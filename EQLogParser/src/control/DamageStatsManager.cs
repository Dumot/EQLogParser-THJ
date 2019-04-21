﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace EQLogParser
{
  class DamageStatsManager : ISummaryBuilder
  {
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    internal static DamageStatsManager Instance = new DamageStatsManager();

    internal event EventHandler<DataPointEvent> EventsUpdateDataPoint;
    internal event EventHandler<StatsGenerationEvent> EventsGenerationStatus;

    internal List<List<ActionBlock>> DamageGroups = new List<List<ActionBlock>>();
    internal Dictionary<string, byte> NpcNames = new Dictionary<string, byte>();

    private PlayerStats RaidTotals;
    private readonly ConcurrentDictionary<string, byte> PlayerHasPet = new ConcurrentDictionary<string, byte>();
    private readonly ConcurrentDictionary<string, string> PetToPlayer = new ConcurrentDictionary<string, string>();
    private readonly List<IAction> Resists = new List<IAction>();
    private List<NonPlayer> Selected;
    private bool IsBaneAvailable = false;
    private string Title;

    internal DamageStatsManager()
    {
      DataManager.Instance.EventsClearedActiveData += (object sender, bool e) =>
      {
        DamageGroups.Clear();
        RaidTotals = null;
        PlayerHasPet.Clear();
        PetToPlayer.Clear();
        Resists.Clear();
        Selected = null;
        Title = "";
      };
    }

    internal void BuildTotalStats(DamageStatsOptions options)
    {
      Selected = options.Npcs;
      Title = options.Name;

      try
      {
        FireNewStatsEvent(options);

        RaidTotals = StatsUtil.CreatePlayerStats(Labels.RAID);
        DamageGroups.Clear();
        NpcNames.Clear();
        PlayerHasPet.Clear();
        PetToPlayer.Clear();
        Resists.Clear();

        Selected.ForEach(npc =>
        {
          StatsUtil.UpdateTimeDiffs(RaidTotals, npc);
          NpcNames[npc.Name] = 1;
        });

        RaidTotals.TotalSeconds = RaidTotals.TimeDiffs.Sum();

        if (RaidTotals.BeginTimes.Count > 0 && RaidTotals.BeginTimes.Count == RaidTotals.LastTimes.Count)
        {
          for (int i = 0; i < RaidTotals.BeginTimes.Count; i++)
          {
            DamageGroups.Add(DataManager.Instance.GetAttackDamageDuring(RaidTotals.BeginTimes[i], RaidTotals.LastTimes[i]));

            var group = DataManager.Instance.GetResistsDuring(RaidTotals.BeginTimes[i], RaidTotals.LastTimes[i]);
            group.ForEach(block => Resists.AddRange(block.Actions));
          }

          Parallel.ForEach(DamageGroups, group =>
          {
            // look for pets that need to be combined first
            Parallel.ForEach(group, block =>
            {
              block.Actions.ForEach(action =>
              {
                DamageRecord record = action as DamageRecord;
                // see if there's a pet mapping, check this first
                string pname = DataManager.Instance.GetPlayerFromPet(record.Attacker);
                if (pname != null || (pname = record.AttackerOwner).Length != 0)
                {
                  PlayerHasPet[pname] = 1;
                  PetToPlayer[record.Attacker] = pname;
                }
              });
            });
          });

          ComputeDamageStats(options);
        }
        else
        {
          FireNoDataEvent(options);
        }
      }
      catch (Exception ex)
      {
        LOG.Error(ex);
      }
    }

    internal void RebuildTotalStats(DamageStatsOptions options)
    {
      FireNewStatsEvent(options);
      ComputeDamageStats(options);
    }

    internal OverlayDamageStats ComputeOverlayDamageStats(DamageRecord record, double beginTime, bool showBane, OverlayDamageStats overlayStats = null)
    {
      PlayerStats raidStats;
      if (overlayStats == null)
      {
        overlayStats = new OverlayDamageStats
        {
          TopLevelStats = new Dictionary<string, PlayerStats>(),
          AggregateStats = new Dictionary<string, PlayerStats>(),
          IndividualStats = new Dictionary<string, PlayerStats>(),
          UniqueNpcs = new Dictionary<string, byte>(),
          RaidStats = raidStats = new PlayerStats()
        };

        raidStats.BeginTime = beginTime;
      }
      else
      {
        raidStats = overlayStats.RaidStats;
      }

      if (beginTime - raidStats.LastTime > NpcDamageManager.NPC_DEATH_TIME)
      {
        raidStats.Total = 0;
        raidStats.BeginTime = beginTime;
        overlayStats.UniqueNpcs.Clear();
        overlayStats.TopLevelStats.Clear();
        overlayStats.AggregateStats.Clear();
        overlayStats.IndividualStats.Clear();
      }

      raidStats.LastTime = beginTime;
      raidStats.TotalSeconds = raidStats.LastTime - raidStats.BeginTime + 1;

      if (!DataManager.Instance.IsProbablyNotAPlayer(record.Attacker) && (showBane || record.Type != Labels.BANE))
      {
        overlayStats.UniqueNpcs[record.Defender] = 1;
        raidStats.Total += record.Total;

        // see if there's a pet mapping, check this first
        string pname = DataManager.Instance.GetPlayerFromPet(record.Attacker);
        if (pname != null || (pname = record.AttackerOwner).Length != 0)
        {
          PlayerHasPet[pname] = 1;
          PetToPlayer[record.Attacker] = pname;
        }

        bool isPet = PetToPlayer.TryGetValue(record.Attacker, out string player);
        bool needAggregate = isPet || (!isPet && PlayerHasPet.ContainsKey(record.Attacker) && overlayStats.TopLevelStats.ContainsKey(record.Attacker + " +Pets"));

        if (!needAggregate || player == Labels.UNASSIGNED)
        {
          // not a pet
          PlayerStats stats = StatsUtil.CreatePlayerStats(overlayStats.IndividualStats, record.Attacker);
          StatsUtil.UpdateStats(stats, record, beginTime);
          overlayStats.TopLevelStats[record.Attacker] = stats;
          stats.TotalSeconds = stats.LastTime - stats.BeginTime + 1;
        }
        else
        {
          string origName = player ?? record.Attacker;
          string aggregateName = origName + " +Pets";

          PlayerStats aggregatePlayerStats;
          aggregatePlayerStats = StatsUtil.CreatePlayerStats(overlayStats.IndividualStats, aggregateName, origName);
          overlayStats.TopLevelStats[aggregateName] = aggregatePlayerStats;

          if (overlayStats.TopLevelStats.ContainsKey(origName))
          {
            var origPlayer = overlayStats.TopLevelStats[origName];
            StatsUtil.MergeStats(aggregatePlayerStats, origPlayer);
            overlayStats.TopLevelStats.Remove(origName);
            overlayStats.IndividualStats.Remove(origName);
          }

          if (record.Attacker != origName && overlayStats.TopLevelStats.ContainsKey(record.Attacker))
          {
            var origPet = overlayStats.TopLevelStats[record.Attacker];
            StatsUtil.MergeStats(aggregatePlayerStats, origPet);
            overlayStats.TopLevelStats.Remove(record.Attacker);
            overlayStats.IndividualStats.Remove(record.Attacker);
          }

          StatsUtil.UpdateStats(aggregatePlayerStats, record, beginTime);
          aggregatePlayerStats.TotalSeconds = aggregatePlayerStats.LastTime - aggregatePlayerStats.BeginTime + 1;
        }

        raidStats.DPS = (long) Math.Round(raidStats.Total / raidStats.TotalSeconds, 2);

        var list = overlayStats.TopLevelStats.Values.AsParallel().OrderByDescending(item => item.Total).ToList();
        int found = list.FindIndex(stats => stats.Name.StartsWith(DataManager.Instance.PlayerName, StringComparison.Ordinal));

        int renumber;
        if (found > 4)
        {
          var you = list[found];
          you.Rank = Convert.ToUInt16(found + 1);
          overlayStats.StatsList = list.Take(4).ToList();
          overlayStats.StatsList.Add(you);
          renumber = overlayStats.StatsList.Count - 1;
        }
        else
        {
          overlayStats.StatsList = list.Take(5).ToList();
          renumber = overlayStats.StatsList.Count;
        }

        for (int i = 0; i < renumber; i++)
        {
          overlayStats.StatsList[i].Rank = Convert.ToUInt16(i + 1);
        }

        // only calculate the top few
        Parallel.ForEach(overlayStats.StatsList, top => StatsUtil.UpdateCalculations(top, raidStats));
        overlayStats.TargetTitle = (overlayStats.UniqueNpcs.Count > 1 ? "Combined (" + overlayStats.UniqueNpcs.Count + "): " : "") + record.Defender;
      }

      return overlayStats;
    }

    internal Dictionary<string, List<HitFreqChartData>> GetHitFreqValues(PlayerStats selected, CombinedDamageStats damageStats)
    {
      Dictionary<string, List<HitFreqChartData>> results = new Dictionary<string, List<HitFreqChartData>>();

      if (damageStats != null)
      {
        // get chart data for player and pets if available
        List<PlayerStats> list = new List<PlayerStats>();
        if (damageStats.Children.ContainsKey(selected.Name))
        {
          list.AddRange(damageStats.Children[selected.Name]);
        }
        else
        {
          list.Add(selected);
        }

        list.ForEach(stat =>
        {
          results[stat.Name] = new List<HitFreqChartData>();
          foreach (string type in stat.SubStats.Keys)
          {
            List<int> critFreqs = new List<int>();
            List<int> nonCritFreqs = new List<int>();
            HitFreqChartData chartData = new HitFreqChartData() { HitType = type };

            // add crits
            var critDamages = stat.SubStats[type].CritFreqValues.Keys.OrderBy(key => key).ToList();
            critDamages.ForEach(damage => critFreqs.Add(stat.SubStats[type].CritFreqValues[damage]));
            chartData.CritYValues = critFreqs;
            chartData.CritXValues = critDamages;

            // add non crits
            var nonCritDamages = stat.SubStats[type].NonCritFreqValues.Keys.OrderBy(key => key).ToList();
            nonCritDamages.ForEach(damage => nonCritFreqs.Add(stat.SubStats[type].NonCritFreqValues[damage]));
            chartData.NonCritYValues = nonCritFreqs;
            chartData.NonCritXValues = nonCritDamages;
            results[stat.Name].Add(chartData);
          }
        });
      }

      return results;
    }

    internal bool IsValidDamage(DamageRecord record)
    {
      bool valid = false;

      if (record != null && NpcNames.ContainsKey(record.Defender) && !DataManager.Instance.IsProbablyNotAPlayer(record.Attacker))
      {
        if (record.Type == Labels.BANE)
        {
          IsBaneAvailable = true;
        }

        valid = true;
      }

      return valid;
    }

    internal void FireSelectionEvent(DamageStatsOptions options, List<PlayerStats> selected)
    {
      if (options.RequestChartData)
      {
        // send update
        DataPointEvent de = new DataPointEvent() { Action = "SELECT", Selected = selected };
        EventsUpdateDataPoint?.Invoke(DamageGroups, de);
      }
    }

    internal void FireUpdateEvent(DamageStatsOptions options, List<PlayerStats> selected = null)
    {
      if (options.RequestChartData)
      {
        // send update
        DataPointEvent de = new DataPointEvent() { Action = "UPDATE", Selected = selected, Iterator = new DamageGroupIterator(DamageGroups, options.IsBaneEanbled) };
        EventsUpdateDataPoint?.Invoke(DamageGroups, de);
      }
    }

    private void FireCompletedEvent(DamageStatsOptions options, CombinedDamageStats combined)
    {
      if (options.RequestSummaryData)
      {
        // generating new stats
        EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent()
        {
          Type = Labels.DAMAGEPARSE,
          State = "COMPLETED",
          CombinedStats = combined,
          IsBaneAvailable = IsBaneAvailable
        });
      }
    }

    private void FireNewStatsEvent(DamageStatsOptions options)
    {
      if (options.RequestSummaryData)
      {
        // generating new stats
        EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent() { Type = Labels.DAMAGEPARSE, State = "STARTED" });
      }
    }

    private void FireNoDataEvent(DamageStatsOptions options)
    {
      if (options.RequestSummaryData)
      {
        // nothing to do
        EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent() { Type = Labels.DAMAGEPARSE, State = "NONPC" });
      }

      if (options.RequestChartData)
      {
        // send update
        DataPointEvent de = new DataPointEvent() { Action = "CLEAR" };
        EventsUpdateDataPoint?.Invoke(DamageGroups, de);
      }
    }

    private void ComputeDamageStats(DamageStatsOptions options)
    {
      CombinedDamageStats combined = null;
      ConcurrentDictionary<string, Dictionary<string, PlayerStats>> childrenStats = new ConcurrentDictionary<string, Dictionary<string, PlayerStats>>();
      ConcurrentDictionary<string, PlayerStats> topLevelStats = new ConcurrentDictionary<string, PlayerStats>();
      ConcurrentDictionary<string, PlayerStats> aggregateStats = new ConcurrentDictionary<string, PlayerStats>();
      Dictionary<string, PlayerStats> individualStats = new Dictionary<string, PlayerStats>();

      // always start over
      RaidTotals.Total = 0;
      IsBaneAvailable = false;

      try
      {
        FireUpdateEvent(options);

        // special cast
        if (options.RequestSummaryData)
        {
          DamageGroups.ForEach(group =>
          {
            // keep track of time range as well as the players that have been updated
            Dictionary<string, PlayerSubStats> allStats = new Dictionary<string, PlayerSubStats>();

            group.ForEach(block =>
            {
              block.Actions.ForEach(action =>
              {
                DamageRecord record = action as DamageRecord;
                if (IsValidDamage(record))
                {
                  PlayerStats stats = StatsUtil.CreatePlayerStats(individualStats, record.Attacker);

                  if (!options.IsBaneEanbled && record.Type == Labels.BANE)
                  {
                    stats.BaneHits++;

                    if (individualStats.TryGetValue(stats.OrigName + " +Pets", out PlayerStats temp))
                    {
                      temp.BaneHits++;
                    }
                  }
                  else
                  {
                    RaidTotals.Total += record.Total;
                    StatsUtil.UpdateStats(stats, record, block.BeginTime);
                    allStats[record.Attacker] = stats;

                    if (!PetToPlayer.TryGetValue(record.Attacker, out string player) && !PlayerHasPet.ContainsKey(record.Attacker))
                    {
                      // not a pet
                      topLevelStats[record.Attacker] = stats;
                    }
                    else
                    {
                      string origName = player ?? record.Attacker;
                      string aggregateName = (player == Labels.UNASSIGNED) ? origName : origName + " +Pets";

                      PlayerStats aggregatePlayerStats = StatsUtil.CreatePlayerStats(individualStats, aggregateName, origName);
                      StatsUtil.UpdateStats(aggregatePlayerStats, record, block.BeginTime);
                      allStats[aggregateName] = aggregatePlayerStats;
                      topLevelStats[aggregateName] = aggregatePlayerStats;

                      if (!childrenStats.TryGetValue(aggregateName, out Dictionary<string, PlayerStats> children))
                      {
                        childrenStats[aggregateName] = new Dictionary<string, PlayerStats>();
                      }

                      childrenStats[aggregateName][stats.Name] = stats;
                    }

                    PlayerSubStats subStats = StatsUtil.CreatePlayerSubStats(stats.SubStats, record.SubType, record.Type);
                    UpdateSubStats(subStats, record, block.BeginTime);
                    allStats[stats.Name + "=" + record.SubType] = subStats;
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

          RaidTotals.DPS = (long) Math.Round(RaidTotals.Total / RaidTotals.TotalSeconds, 2);

          // add up resists
          Dictionary<string, uint> resistCounts = new Dictionary<string, uint>();
          Parallel.ForEach(Resists, resist =>
          {
            ResistRecord record = resist as ResistRecord;
            Helpers.StringUIntAddHelper.Add(resistCounts, record.Spell, 1);
          });

          // get death counts
          ConcurrentDictionary<string, uint> deathCounts = StatsUtil.GetPlayerDeaths(RaidTotals);

          Parallel.ForEach(individualStats.Values, stats =>
          {
            if (topLevelStats.TryGetValue(stats.Name, out PlayerStats topLevel))
            {
              uint totalDeaths = 0;
              if (childrenStats.TryGetValue(stats.Name, out Dictionary<string, PlayerStats> children))
              {
                foreach (var child in children.Values)
                {
                  StatsUtil.UpdateCalculations(child, RaidTotals, resistCounts);

                  if (stats.Total > 0)
                  {
                    child.Percent = Math.Round(Convert.ToDouble(child.Total) / stats.Total * 100, 2);
                  }

                  if (deathCounts.TryGetValue(child.Name, out uint count))
                  {
                    child.Deaths = count;
                    totalDeaths += child.Deaths;
                  }
                }
              }

              StatsUtil.UpdateCalculations(stats, RaidTotals, resistCounts);
              stats.Deaths = totalDeaths;
            }
            else if (!PetToPlayer.ContainsKey(stats.Name))
            {
              StatsUtil.UpdateCalculations(stats, RaidTotals, resistCounts);

              if (deathCounts.TryGetValue(stats.Name, out uint count))
              {
                stats.Deaths = count;
              }
            }
          });

          combined = new CombinedDamageStats
          {
            RaidStats = RaidTotals,
            UniqueClasses = new Dictionary<string, byte>(),
            Children = new Dictionary<string, List<PlayerStats>>(),
            StatsList = topLevelStats.Values.AsParallel().OrderByDescending(item => item.Total).ToList(),
            TargetTitle = (Selected.Count > 1 ? "Combined (" + Selected.Count + "): " : "") + Title,
            TimeTitle = string.Format(CultureInfo.CurrentCulture, StatsUtil.TIME_FORMAT, RaidTotals.TotalSeconds),
            TotalTitle = string.Format(CultureInfo.CurrentCulture, StatsUtil.TOTAL_FORMAT, StatsUtil.FormatTotals(RaidTotals.Total), " Damage ", StatsUtil.FormatTotals(RaidTotals.DPS))
          };

          combined.FullTitle = StatsUtil.FormatTitle(combined.TargetTitle, combined.TimeTitle, combined.TotalTitle);
          combined.ShortTitle = StatsUtil.FormatTitle(combined.TargetTitle, combined.TimeTitle, "");

          for (int i = 0; i < combined.StatsList.Count; i++)
          {
            combined.StatsList[i].Rank = Convert.ToUInt16(i + 1);
            combined.UniqueClasses[combined.StatsList[i].ClassName] = 1;

            if (childrenStats.TryGetValue(combined.StatsList[i].Name, out Dictionary<string, PlayerStats> children))
            {
              combined.Children.Add(combined.StatsList[i].Name, children.Values.OrderByDescending(stats => stats.Total).ToList());
            }
          }
        }
      }
      catch (ArgumentNullException anx)
      {
        LOG.Error(anx);
      }
      catch (AggregateException agx)
      {
        LOG.Error(agx);
      }

      FireCompletedEvent(options, combined);
    }

    public StatsSummary BuildSummary(CombinedStats currentStats, List<PlayerStats> selected, bool showTotals, bool rankPlayers)
    {
      List<string> list = new List<string>();

      string title = "";
      string details = "";

      if (currentStats != null)
      {
        if (selected != null)
        {
          foreach (PlayerStats stats in selected.OrderByDescending(item => item.Total))
          {
            string playerFormat = rankPlayers ? string.Format(CultureInfo.CurrentCulture, StatsUtil.PLAYER_RANK_FORMAT, stats.Rank, stats.Name) : string.Format(CultureInfo.CurrentCulture, StatsUtil.PLAYER_FORMAT, stats.Name);
            string damageFormat = string.Format(CultureInfo.CurrentCulture, StatsUtil.TOTAL_FORMAT, StatsUtil.FormatTotals(stats.Total), "", StatsUtil.FormatTotals(stats.DPS));
            string timeFormat = string.Format(CultureInfo.CurrentCulture, StatsUtil.TIME_FORMAT, stats.TotalSeconds);
            list.Add(playerFormat + damageFormat + " " + timeFormat);
          }
        }

        details = list.Count > 0 ? ", " + string.Join(", ", list) : "";
        title = StatsUtil.FormatTitle(currentStats.TargetTitle, currentStats.TimeTitle, showTotals ? currentStats.TotalTitle : "");
      }

      return new StatsSummary() { Title = title, RankedPlayers = details };
    }

    private static void UpdateSubStats(PlayerSubStats subStats, DamageRecord record, double beginTime)
    {
      uint critHits = subStats.CritHits;
      StatsUtil.UpdateStats(subStats, record, beginTime);

      Dictionary<long, int> values = subStats.CritHits > critHits ? subStats.CritFreqValues : subStats.NonCritFreqValues;
      Helpers.LongIntAddHelper.Add(values, record.Total, 1);
    }
  }
}

