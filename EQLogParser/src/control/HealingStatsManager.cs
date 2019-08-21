﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EQLogParser
{
  class HealingStatsManager : ISummaryBuilder
  {
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    internal static HealingStatsManager Instance = new HealingStatsManager();

    internal event EventHandler<DataPointEvent> EventsUpdateDataPoint;
    internal event EventHandler<StatsGenerationEvent> EventsGenerationStatus;

    internal List<List<ActionBlock>> HealingGroups = new List<List<ActionBlock>>();

    private const int HEAL_OFFSET = 10; // additional # of seconds to count

    private PlayerStats RaidTotals;
    private List<NonPlayer> Selected;
    private string Title;
    private bool UpdatingGroups = false;

    internal HealingStatsManager()
    {
      DataManager.Instance.EventsClearedActiveData += (object sender, bool e) =>
      {
        HealingGroups.Clear();
        RaidTotals = null;
        Selected = null;
        Title = "";
      };
    }

    internal void BuildTotalStats(GenerateStatsOptions options)
    {
      UpdatingGroups = true;

      Selected = options.Npcs;
      Title = options.Name;

      try
      {
        FireNewStatsEvent(options);

        RaidTotals = StatsUtil.CreatePlayerStats(Labels.RAID);
        HealingGroups.Clear();

        Selected.ForEach(npc => StatsUtil.UpdateTimeDiffs(RaidTotals, npc, HEAL_OFFSET));
        RaidTotals.TotalSeconds = RaidTotals.TimeDiffs.Sum();

        if (RaidTotals.BeginTimes.Count > 0 && RaidTotals.BeginTimes.Count == RaidTotals.LastTimes.Count)
        {
          for (int i = 0; i < RaidTotals.BeginTimes.Count; i++)
          {
            HealingGroups.Add(DataManager.Instance.GetHealsDuring(RaidTotals.BeginTimes[i], RaidTotals.LastTimes[i]));
          }

          ComputeHealingStats(options);
        }
        else
        {
          FireNoDataEvent(options);
        }
      }
      catch (ArgumentNullException ne)
      {
        LOG.Error(ne);
      }
      catch (NullReferenceException nr)
      {
        LOG.Error(nr);
      }
      catch (ArgumentOutOfRangeException aor)
      {
        LOG.Error(aor);
      }
      catch (ArgumentException ae)
      {
        LOG.Error(ae);
      }
      finally
      {
        UpdatingGroups = false;
      }
    }

    internal void RebuildTotalStats(GenerateStatsOptions options)
    {
      FireNewStatsEvent(options);
      ComputeHealingStats(options);
    }

    internal void PopulateHealing(List<PlayerStats> playerStats)
    {
      while (UpdatingGroups)
      {
        Thread.Sleep(100);
      }

      Dictionary<string, PlayerStats> individualStats = new Dictionary<string, PlayerStats>();
      Dictionary<string, long> totals = new Dictionary<string, long>();

      HealingGroups.ForEach(group =>
      {
        // keep track of time range as well as the players that have been updated
        Dictionary<string, PlayerSubStats> allStats = new Dictionary<string, PlayerSubStats>();

        group.ForEach(block =>
        {
          block.Actions.ForEach(action =>
          {
            if (action is HealRecord record)
            {
              if (IsValidHeal(record))
              {
                PlayerStats stats = StatsUtil.CreatePlayerStats(individualStats, record.Healed);
                StatsUtil.UpdateStats(stats, record, block.BeginTime);
                allStats[record.Healed] = stats;

                PlayerSubStats subStats = StatsUtil.CreatePlayerSubStats(stats.SubStats, record.Healer, record.Type);
                StatsUtil.UpdateStats(subStats, record, block.BeginTime);
                allStats[record.Healer + "-" + record.Healed] = subStats;

                var spellStatName = record.SubType ?? Labels.SELFHEAL;
                PlayerSubStats spellStats = StatsUtil.CreatePlayerSubStats(stats.SubStats2, spellStatName, record.Type);
                StatsUtil.UpdateStats(spellStats, record, block.BeginTime);
                allStats[stats.Name + "=" + spellStatName] = spellStats;

                long value = 0;
                if (totals.ContainsKey(record.Healed))
                {
                  value = totals[record.Healed];
                }

                totals[record.Healed] = record.Total + value;
              }
            }
          });
        });

        Parallel.ForEach(allStats.Values, stats =>
        {
          stats.TotalSeconds += stats.LastTime - stats.BeginTime + 1;
          stats.BeginTime = double.NaN;
        });
      });

      Parallel.ForEach(playerStats, stat =>
      {
        if (individualStats.ContainsKey(stat.Name))
        {
          if (totals.ContainsKey(stat.Name))
          {
            stat.Extra = totals[stat.Name];
          }

          var indStat = individualStats[stat.Name];
          stat.SubStats2.Add("receivedHealing", indStat);
          StatsUtil.UpdateCalculations(indStat, RaidTotals);

          indStat.SubStats.Values.ToList().ForEach(subStat => StatsUtil.UpdateCalculations(subStat, indStat));
          indStat.SubStats2.Values.ToList().ForEach(subStat => StatsUtil.UpdateCalculations(subStat, indStat));
        }
      });
    }

    internal void FireSelectionEvent(GenerateStatsOptions options, List<PlayerStats> selected)
    {
      FireChartEvent(options, "SELECT", selected);
    }

    internal void FireUpdateEvent(GenerateStatsOptions options, List<PlayerStats> selected = null, Predicate<object> filter = null)
    {
      FireChartEvent(options, "UPDATE", selected, filter);
    }

    internal void FireFilterEvent(GenerateStatsOptions options, Predicate<object> filter)
    {
      FireChartEvent(options, "FILTER", null, filter);
    }

    internal static bool IsValidHeal(HealRecord record)
    {
      bool valid = false;

      if (record != null && !DataManager.Instance.IsProbablyNotAPlayer(record.Healed))
      {
        valid = true;
        SpellData spellData;
        if (record.SubType != null && (spellData = DataManager.Instance.GetSpellByName(record.SubType)) != null)
        {
          if (spellData.Target == (byte)SpellTarget.TARGETAE || spellData.Target == (byte)SpellTarget.NEARBYPLAYERSAE ||
            spellData.Target == (byte)SpellTarget.TARGETRINGAE)
          {
            valid = MainWindow.IsAoEHealingEnabled;
          }
        }
      }

      return valid;
    }

    private void FireCompletedEvent(GenerateStatsOptions options, CombinedStats combined)
    {
      if (options.RequestSummaryData)
      {
        // generating new stats
        EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent()
        {
          Type = Labels.HEALPARSE,
          State = "COMPLETED",
          CombinedStats = combined
        });
      }
    }

    private void FireNewStatsEvent(GenerateStatsOptions options)
    {
      if (options.RequestSummaryData)
      {
        // generating new stats
        EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent() { Type = Labels.HEALPARSE, State = "STARTED" });
      }
    }

    private void FireNoDataEvent(GenerateStatsOptions options)
    {
      if (options.RequestSummaryData)
      {
        // nothing to do
        EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent() { Type = Labels.HEALPARSE, State = "NONPC" });
      }

      FireChartEvent(options, "CLEAR");
    }

    internal void FireChartEvent(GenerateStatsOptions options, string action, List<PlayerStats> selected = null, Predicate<object> filter = null)
    {
      if (options.RequestChartData)
      {
        // send update
        DataPointEvent de = new DataPointEvent() { Action = action, Iterator = new HealGroupCollection(HealingGroups), Filter = filter };

        if (selected != null)
        {
          de.Selected.AddRange(selected);
        }

        EventsUpdateDataPoint?.Invoke(HealingGroups, de);
      }
    }

    private void ComputeHealingStats(GenerateStatsOptions options)
    {
      if (RaidTotals != null)
      {
        CombinedStats combined = null;
        Dictionary<string, PlayerStats> individualStats = new Dictionary<string, PlayerStats>();

        // always start over
        RaidTotals.Total = 0;

        try
        {
          FireUpdateEvent(options);

          if (options.RequestSummaryData)
          {

            HealingGroups.ForEach(group =>
            {
              // keep track of time range as well as the players that have been updated
              Dictionary<string, PlayerSubStats> allStats = new Dictionary<string, PlayerSubStats>();

              group.ForEach(block =>
              {
                block.Actions.ForEach(action =>
                {
                  HealRecord record = action as HealRecord;

                  if (IsValidHeal(record))
                  {
                    RaidTotals.Total += record.Total;
                    PlayerStats stats = StatsUtil.CreatePlayerStats(individualStats, record.Healer);

                    StatsUtil.UpdateStats(stats, record, block.BeginTime);
                    allStats[record.Healer] = stats;

                    var spellStatName = record.SubType ?? Labels.SELFHEAL;
                    PlayerSubStats spellStats = StatsUtil.CreatePlayerSubStats(stats.SubStats, spellStatName, record.Type);
                    StatsUtil.UpdateStats(spellStats, record, block.BeginTime);
                    allStats[stats.Name + "=" + spellStatName] = spellStats;

                    var healedStatName = record.Healed;
                    PlayerSubStats healedStats = StatsUtil.CreatePlayerSubStats(stats.SubStats2, healedStatName, record.Type);
                    StatsUtil.UpdateStats(healedStats, record, block.BeginTime);
                    allStats[stats.Name + "=" + healedStatName] = healedStats;
                  }
                });
              });

              Parallel.ForEach(allStats.Values, stats =>
              {
                stats.TotalSeconds += stats.LastTime - stats.BeginTime + 1;
                stats.BeginTime = double.NaN;
              });
            });

            RaidTotals.DPS = (long)Math.Round(RaidTotals.Total / RaidTotals.TotalSeconds, 2);
            Parallel.ForEach(individualStats.Values, stats => StatsUtil.UpdateCalculations(stats, RaidTotals));

            combined = new CombinedStats
            {
              RaidStats = RaidTotals,
              TargetTitle = (Selected.Count > 1 ? "Combined (" + Selected.Count + "): " : "") + Title,
              TimeTitle = string.Format(CultureInfo.CurrentCulture, StatsUtil.TIME_FORMAT, RaidTotals.TotalSeconds),
              TotalTitle = string.Format(CultureInfo.CurrentCulture, StatsUtil.TOTAL_FORMAT, StatsUtil.FormatTotals(RaidTotals.Total), " Heals ", StatsUtil.FormatTotals(RaidTotals.DPS))
            };

            combined.StatsList.AddRange(individualStats.Values.AsParallel().OrderByDescending(item => item.Total));
            combined.FullTitle = StatsUtil.FormatTitle(combined.TargetTitle, combined.TimeTitle, combined.TotalTitle);
            combined.ShortTitle = StatsUtil.FormatTitle(combined.TargetTitle, combined.TimeTitle, "");

            for (int i = 0; i < combined.StatsList.Count; i++)
            {
              combined.StatsList[i].Rank = Convert.ToUInt16(i + 1);
              combined.UniqueClasses[combined.StatsList[i].ClassName] = 1;
            }
          }
        }
        catch (ArgumentNullException ane)
        {
          LOG.Error(ane);
        }
        catch (NullReferenceException nre)
        {
          LOG.Error(nre);
        }
        catch (ArgumentOutOfRangeException aro)
        {
          LOG.Error(aro);
        }

        FireCompletedEvent(options, combined);
      }

    }

    public StatsSummary BuildSummary(CombinedStats currentStats, List<PlayerStats> selected, bool showTotals, bool rankPlayers)
    {
      List<string> list = new List<string>();

      string title = "";
      string details = "";

      if (currentStats != null)
      {
        if (selected?.Count > 0)
        {
          foreach (PlayerStats stats in selected.OrderByDescending(item => item.Total))
          {
            string playerFormat = rankPlayers ? string.Format(CultureInfo.CurrentCulture, StatsUtil.PLAYER_RANK_FORMAT, stats.Rank, stats.Name) : string.Format(CultureInfo.CurrentCulture, StatsUtil.PLAYER_FORMAT, stats.Name);
            string damageFormat = string.Format(CultureInfo.CurrentCulture, StatsUtil.TOTAL_ONLY_FORMAT, StatsUtil.FormatTotals(stats.Total));
            list.Add(playerFormat + damageFormat + " ");
          }
        }

        details = list.Count > 0 ? ", " + string.Join(" | ", list) : "";
        title = StatsUtil.FormatTitle(currentStats.TargetTitle, currentStats.TimeTitle, showTotals ? currentStats.TotalTitle : "");
      }

      return new StatsSummary() { Title = title, RankedPlayers = details, };
    }
  }
}
