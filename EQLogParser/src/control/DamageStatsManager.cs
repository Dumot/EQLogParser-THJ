﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    private readonly Dictionary<int, byte> DamageGroupIds = new Dictionary<int, byte>();
    private readonly ConcurrentDictionary<string, TimeRange> PlayerTimeRanges = new ConcurrentDictionary<string, TimeRange>();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TimeRange>> PlayerSubTimeRanges = new ConcurrentDictionary<string, ConcurrentDictionary<string, TimeRange>>();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> PlayerPets = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>();
    private readonly ConcurrentDictionary<string, string> PetToPlayer = new ConcurrentDictionary<string, string>();
    private readonly List<IAction> Resists = new List<IAction>();
    private List<List<ActionBlock>> AllDamageGroups;
    private List<List<ActionBlock>> DamageGroups = new List<List<ActionBlock>>();
    private PlayerStats RaidTotals;
    private List<Fight> Selected;
    private string Title;

    private static long OverlayDeadTotalDamage;
    private static Dictionary<string, string> OverlayPetOwners;
    private static Dictionary<string, OverlayPlayerTotal> OverlayDeadPlayerTotals;
    private static TimeRange OverlayTimeSegments;
    private static string OverlayFightName;
    private static double OverlayUpdateTime;
    private static int OverlayDeadFightCount;

    internal static CombinedStats ComputeOverlayStats(bool reset, int mode, int maxRows, string selectedClass)
    {
      CombinedStats combined = null;

      if (reset)
      {
        OverlayDeadTotalDamage = 0;
        OverlayUpdateTime = 0;
        OverlayPetOwners = new Dictionary<string, string>();
        OverlayDeadPlayerTotals = new Dictionary<string, OverlayPlayerTotal>();
        OverlayTimeSegments = new TimeRange();
        OverlayFightName = null;
        OverlayDeadFightCount = 0;
      }

      var allDamage = OverlayDeadTotalDamage;
      var allTime = OverlayTimeSegments.TimeSegments.Count > 0 ? new TimeRange(OverlayTimeSegments.TimeSegments) : new TimeRange();
      var playerTotals = new Dictionary<string, OverlayPlayerTotal>();
      var playerHasPet = new Dictionary<string, bool>();
      var fightCount = OverlayDeadFightCount;

      // check incase pet mappings was updated while overlay is running
      foreach (var keypair in OverlayPetOwners)
      {
        UpdateOverlayHasPet(keypair.Key, keypair.Value, playerHasPet, OverlayDeadPlayerTotals);
      }

      // copy values from dead fights
      foreach (var keypair in OverlayDeadPlayerTotals)
      {
        playerTotals[keypair.Key] = new OverlayPlayerTotal
        {
          Damage = keypair.Value.Damage,
          UpdateTime = keypair.Value.UpdateTime,
          Name = keypair.Value.Name,
          Range = new TimeRange(keypair.Value.Range.TimeSegments)
        };
      }

      var oldestTime = OverlayUpdateTime;
      Fight oldestFight = null;
      var baneEnabled = MainWindow.IsBaneDamageEnabled;

      // clear out anything pending in the queue
      DamageLineParser.CheckSlainQueue(DateUtil.ToDouble(DateTime.Now.AddSeconds(-3)));

      foreach (var fightinfo in DataManager.Instance.GetOverlayFights())
      {
        var fight = fightinfo.Value;
        fightCount++;

        if (!fight.Dead || mode > 0)
        {
          foreach (var keypair in fight.PlayerTotals)
          {
            var player = UpdateOverlayHasPet(keypair.Key, keypair.Value.PetOwner, playerHasPet, playerTotals);

            // save current state and remove dead fight at the end
            if (fight.Dead)
            {
              OverlayDeadTotalDamage += keypair.Value.Damage;
              OverlayTimeSegments.Add(new TimeSegment(keypair.Value.BeginTime, keypair.Value.UpdateTime));
            }

            // always update so +Pets can be added before fight is dead
            OverlayPetOwners[player] = keypair.Value.PetOwner;
            allDamage += keypair.Value.Damage;
            allTime.Add(new TimeSegment(keypair.Value.BeginTime, keypair.Value.UpdateTime));

            if (OverlayUpdateTime == 0)
            {
              OverlayUpdateTime = keypair.Value.UpdateTime;
              oldestTime = keypair.Value.UpdateTime;
              oldestFight = fight;
            }
            else
            {
              OverlayUpdateTime = Math.Max(OverlayUpdateTime, keypair.Value.UpdateTime);
              if (oldestTime > keypair.Value.UpdateTime)
              {
                oldestTime = keypair.Value.UpdateTime;
                oldestFight = fight;
              }
            }

            if (fight.Dead)
            {
              UpdateOverlayPlayerTotals(player, OverlayDeadPlayerTotals, keypair.Value);
            }

            UpdateOverlayPlayerTotals(player, playerTotals, keypair.Value);
          }
        }

        if (fight.Dead)
        {
          DataManager.Instance.RemoveOverlayFight(fightinfo.Key);
          OverlayDeadFightCount++;
        }
      }

      if (OverlayFightName == null && oldestFight != null)
      {
        OverlayFightName = oldestFight.Name;
      }

      var timeout = mode == 0 ? DataManager.FIGHTTIMEOUT : mode;
      var totalSeconds = allTime.GetTotal();
      var diff = (DateTime.Now - DateTime.MinValue.AddSeconds(OverlayUpdateTime)).TotalSeconds;
      // added >= 0 check because this broke while testing when clocks moved an hour back in the fall
      if (OverlayFightName != null && totalSeconds > 0 && allDamage > 0 && diff >= 0 && diff <= timeout)
      {
        int rank = 1;
        var list = new List<PlayerStats>();
        var totalDps = (long)Math.Round(allDamage / totalSeconds, 2);
        int myIndex = -1;

        foreach (var total in playerTotals.Values.OrderByDescending(total => total.Damage))
        {
          var time = total.Range.GetTotal();
          if (time > 0 && (DateTime.Now - DateTime.MinValue.AddSeconds(total.UpdateTime)).TotalSeconds <= DataManager.MAXTIMEOUT)
          {
            PlayerStats playerStats = new PlayerStats()
            {
              Name = playerHasPet.ContainsKey(total.Name) ? total.Name + " +Pets" : total.Name,
              Total = total.Damage,
              DPS = (long)Math.Round(total.Damage / time, 2),
              TotalSeconds = time,
              Rank = (ushort)rank++,
              ClassName = PlayerManager.Instance.GetPlayerClass(total.Name),
              OrigName = total.Name
            };

            if (playerStats.Name.StartsWith(ConfigUtil.PlayerName, StringComparison.Ordinal))
            {
              myIndex = list.Count;
            }

            if (myIndex == list.Count || selectedClass == EQLogParser.Resource.ANY_CLASS || selectedClass == playerStats.ClassName)
            {
              list.Add(playerStats);
            }
          }
        }

        if (myIndex > (maxRows - 1))
        {
          var me = list[myIndex];
          list = list.Take(maxRows - 1).ToList();
          list.Add(me);
        }
        else
        {
          list = list.Take(maxRows).ToList();
        }

        combined = new CombinedStats();
        combined.StatsList.AddRange(list);
        combined.RaidStats = new PlayerStats { Total = allDamage, DPS = totalDps, TotalSeconds = totalSeconds };
        combined.TargetTitle = (fightCount > 1 ? "C(" + fightCount + "): " : "") + OverlayFightName;

        // these are here to support copy/paste of the parse
        combined.TimeTitle = string.Format(StatsUtil.TIME_FORMAT, combined.RaidStats.TotalSeconds);
        combined.TotalTitle = string.Format(StatsUtil.TOTAL_FORMAT, StatsUtil.FormatTotals(combined.RaidStats.Total),
          " Damage ", StatsUtil.FormatTotals(combined.RaidStats.DPS));
      }

      return combined;
    }

    private static void UpdateOverlayPlayerTotals(string player, Dictionary<string, OverlayPlayerTotal> playerTotals, FightTotalDamage fightDmg)
    {
      if (playerTotals.TryGetValue(player, out OverlayPlayerTotal total))
      {
        total.Damage += fightDmg.Damage;
        total.Range.Add(new TimeSegment(fightDmg.BeginTime, fightDmg.UpdateTime));
        total.UpdateTime = Math.Max(total.UpdateTime, fightDmg.UpdateTime);
      }
      else
      {
        playerTotals[player] = new OverlayPlayerTotal
        {
          Name = player,
          Damage = fightDmg.Damage,
          Range = new TimeRange(new TimeSegment(fightDmg.BeginTime, fightDmg.UpdateTime)),
          UpdateTime = fightDmg.UpdateTime
        };
      }
    }

    private static string UpdateOverlayHasPet(string player, string petOwner, Dictionary<string, bool> playerHasPet, Dictionary<string, OverlayPlayerTotal> totals)
    {
      if (!string.IsNullOrEmpty(petOwner))
      {
        playerHasPet[petOwner] = true;

        if (totals.TryGetValue(player, out OverlayPlayerTotal value))
        {
          totals.Remove(player);
          totals[petOwner] = value;
          value.Name = petOwner;
        }

        player = petOwner;
      }
      else if (PlayerManager.Instance.GetPlayerFromPet(player) is string owner && owner != Labels.UNASSIGNED)
      {
        playerHasPet[owner] = true;

        if (totals.TryGetValue(player, out OverlayPlayerTotal value))
        {
          totals.Remove(player);
          totals[owner] = value;
          value.Name = owner;
        }

        player = owner;
      }

      return player;
    }

    internal DamageStatsManager()
    {
      DataManager.Instance.EventsClearedActiveData += (object sender, bool e) =>
      {
        lock (DamageGroupIds)
        {
          Reset();
        }
      };
    }

    internal int GetGroupCount()
    {
      lock (DamageGroupIds)
      {
        return DamageGroups.Count;
      }
    }

    internal void RebuildTotalStats(GenerateStatsOptions options)
    {
      lock (DamageGroupIds)
      {
        if (DamageGroups.Count > 0)
        {
          FireNewStatsEvent();
          ComputeDamageStats(options);
        }
      }
    }

    internal void BuildTotalStats(GenerateStatsOptions options)
    {
      lock (DamageGroupIds)
      {
        try
        {
          FireNewStatsEvent();
          Reset();

          Selected = options.Npcs.OrderBy(sel => sel.Id).ToList();
          Title = options.Npcs?.FirstOrDefault()?.Name;
          var damageBlocks = new List<ActionBlock>();

          Selected.ForEach(fight =>
          {
            damageBlocks.AddRange(fight.DamageBlocks);

            if (fight.GroupId > -1)
            {
              DamageGroupIds[fight.GroupId] = 1;
            }

            RaidTotals.Ranges.Add(new TimeSegment(fight.BeginDamageTime, fight.LastDamageTime));
            StatsUtil.UpdateRaidTimeRanges(fight, PlayerTimeRanges, PlayerSubTimeRanges);
          });

          damageBlocks.Sort((a, b) => a.BeginTime.CompareTo(b.BeginTime));

          if (damageBlocks.Count > 0)
          {
            RaidTotals.TotalSeconds = RaidTotals.Ranges.GetTotal();
            RaidTotals.MaxTime = RaidTotals.TotalSeconds;

            int rangeIndex = 0;
            double lastTime = 0;
            var newBlock = new List<ActionBlock>();
            damageBlocks.ForEach(block =>
            {
              if (RaidTotals.Ranges.TimeSegments.Count > rangeIndex && block.BeginTime > RaidTotals.Ranges.TimeSegments[rangeIndex].EndTime)
              {
                rangeIndex++;

                if (newBlock.Count > 0)
                {
                  DamageGroups.Add(newBlock);
                }

                newBlock = new List<ActionBlock>();
              }

              if (lastTime != block.BeginTime)
              {
                var copy = new ActionBlock();
                copy.Actions.AddRange(block.Actions);
                copy.BeginTime = block.BeginTime;
                newBlock.Add(copy);
              }
              else
              {
                newBlock.Last().Actions.AddRange(block.Actions);
              }

              // update pet mapping
              block.Actions.ForEach(action => UpdatePetMapping(action as DamageRecord));
              lastTime = block.BeginTime;
            });

            DamageGroups.Add(newBlock);
            RaidTotals.Ranges.TimeSegments.ForEach(segment => DataManager.Instance.GetResistsDuring(segment.BeginTime, segment.EndTime).ForEach(block =>
              Resists.AddRange(block.Actions)));
            ComputeDamageStats(options);
          }
          else if (Selected == null || Selected.Count == 0)
          {
            FireNoDataEvent(options, "NONPC");
          }
          else
          {
            FireNoDataEvent(options, "NODATA");
          }
        }
        catch (Exception ex)
        {
          LOG.Error(ex);
        }
      }
    }

    private void ComputeDamageStats(GenerateStatsOptions options)
    {
      lock (DamageGroupIds)
      {
        if (RaidTotals != null)
        {
          CombinedStats combined = null;
          var childrenStats = new ConcurrentDictionary<string, Dictionary<string, PlayerStats>>();
          var topLevelStats = new ConcurrentDictionary<string, PlayerStats>();
          var damageValidator = new DamageValidator();
          var individualStats = new Dictionary<string, PlayerStats>();

          // always start over
          RaidTotals.Total = 0;
          double startTime = -1;
          double stopTime = -1;

          try
          {
            if ((options.MaxSeconds > -1 && options.MaxSeconds < RaidTotals.MaxTime && options.MaxSeconds != RaidTotals.TotalSeconds) ||
              (options.MinSeconds > 0 && options.MinSeconds <= RaidTotals.MaxTime && options.MinSeconds != RaidTotals.MinTime))
            {
              var removeFromEnd = RaidTotals.MaxTime - options.MaxSeconds;
              if (removeFromEnd > 0)
              {
                var reverse = RaidTotals.Ranges.TimeSegments.ToList();
                reverse.Reverse();
                foreach (var range in reverse)
                {
                  if (range.Total >= removeFromEnd)
                  {
                    stopTime = range.EndTime - removeFromEnd;
                    break;
                  }
                  else
                  {
                    removeFromEnd -= range.Total;
                  }
                }
              }

              var removeFromStart = (double)options.MinSeconds;
              if (removeFromStart > 0)
              {
                foreach (var range in RaidTotals.Ranges.TimeSegments)
                {
                  if (range.Total >= removeFromStart)
                  {
                    startTime = range.BeginTime + removeFromStart;
                    break;
                  }
                  else
                  {
                    removeFromStart -= range.Total;
                  }
                }
              }

              var filteredGroups = new List<List<ActionBlock>>();
              AllDamageGroups.ForEach(group =>
              {
                var filteredBlocks = new List<ActionBlock>();
                group.ForEach(block =>
                {
                  if ((startTime == -1 || block.BeginTime >= startTime) && (stopTime == -1 || block.BeginTime <= stopTime))
                  {
                    filteredBlocks.Add(block);
                  }
                });

                if (filteredBlocks.Count > 0)
                {
                  filteredGroups.Add(filteredBlocks);
                }
              });

              DamageGroups = filteredGroups;
              RaidTotals.TotalSeconds = options.MaxSeconds - options.MinSeconds;
              RaidTotals.MinTime = options.MinSeconds;
            }
            else
            {
              DamageGroups = AllDamageGroups;
              RaidTotals.MinTime = 0;
              RaidTotals.TotalSeconds = RaidTotals.MaxTime;
            }

            Dictionary<string, double> prevPlayerTimes = new Dictionary<string, double>();
            DamageGroups.ForEach(group =>
            {
              group.ForEach(block =>
              {
                block.Actions.ForEach(action =>
                {
                  if (action is DamageRecord record)
                  {
                    var isValid = damageValidator.IsValid(record);
                    var stats = StatsUtil.CreatePlayerStats(individualStats, record.Attacker);

                    if (record.Type == Labels.BANE && !isValid)
                    {
                      stats.BaneHits++;

                      if (individualStats.TryGetValue(stats.OrigName + " +Pets", out PlayerStats temp))
                      {
                        temp.BaneHits++;
                      }
                    }
                    else if (isValid)
                    {
                      bool isAttackerPet = PlayerManager.Instance.IsVerifiedPet(record.Attacker);
                      bool isNewFrame = checkNewFrame(prevPlayerTimes, stats.Name, block.BeginTime);

                      RaidTotals.Total += record.Total;
                      StatsUtil.UpdateStats(stats, record, isNewFrame, isAttackerPet);

                      if ((!PetToPlayer.TryGetValue(record.Attacker, out string player) && !PlayerPets.ContainsKey(record.Attacker))
                      || player == Labels.UNASSIGNED)
                      {
                        topLevelStats[record.Attacker] = stats;
                        stats.IsTopLevel = true;
                      }
                      else
                      {
                        var origName = player ?? record.Attacker;
                        var aggregateName = origName + " +Pets";
                        isNewFrame = checkNewFrame(prevPlayerTimes, aggregateName, block.BeginTime);

                        var aggregatePlayerStats = StatsUtil.CreatePlayerStats(individualStats, aggregateName, origName);
                        StatsUtil.UpdateStats(aggregatePlayerStats, record, isNewFrame, isAttackerPet);
                        topLevelStats[aggregateName] = aggregatePlayerStats;

                        if (!childrenStats.TryGetValue(aggregateName, out Dictionary<string, PlayerStats> children))
                        {
                          childrenStats[aggregateName] = new Dictionary<string, PlayerStats>();
                        }

                        childrenStats[aggregateName][stats.Name] = stats;
                        stats.IsTopLevel = false;
                      }

                      var subStats = StatsUtil.CreatePlayerSubStats(stats.SubStats, record.SubType, record.Type);
                      uint critHits = subStats.CritHits;
                      StatsUtil.UpdateStats(subStats, record, false, isAttackerPet);

                      // dont count misses/dodges or where no damage was done
                      if (record.Total > 0)
                      {
                        Dictionary<long, int> values = subStats.CritHits > critHits ? subStats.CritFreqValues : subStats.NonCritFreqValues;
                        Helpers.LongIntAddHelper.Add(values, record.Total, 1);
                      }
                    }
                  }
                });
              });
            });

            RaidTotals.DPS = (long)Math.Round(RaidTotals.Total / RaidTotals.TotalSeconds, 2);
            StatsUtil.PopulateSpecials(RaidTotals);

            var resistCounts = Resists.Cast<ResistRecord>().GroupBy(x => x.Spell).ToDictionary(g => g.Key, g => g.ToList().Count);
            var expandedStats = new ConcurrentBag<PlayerStats>();

            Parallel.ForEach(individualStats.Values, stats =>
            {
              if (topLevelStats.ContainsKey(stats.Name))
              {
                if (childrenStats.TryGetValue(stats.Name, out Dictionary<string, PlayerStats> children))
                {
                  var timeRange = new TimeRange();
                  foreach (var child in children.Values)
                  {
                    if (PlayerTimeRanges.TryGetValue(child.Name, out TimeRange range))
                    {
                      StatsUtil.UpdateAllStatsTimeRanges(child, PlayerTimeRanges, PlayerSubTimeRanges, startTime, stopTime);
                      timeRange.Add(range.TimeSegments);
                    }

                    expandedStats.Add(child);
                    StatsUtil.UpdateCalculations(child, RaidTotals, resistCounts);

                    if (stats.Total > 0)
                    {
                      child.Percent = (float)Math.Round(Convert.ToDouble(child.Total) / stats.Total * 100, 2);
                    }

                    if (RaidTotals.Specials.TryGetValue(child.Name, out string special1))
                    {
                      child.Special = special1;
                    }
                  }

                  var filteredTimeRange = StatsUtil.FilterTimeRange(timeRange, startTime, stopTime);
                  stats.TotalSeconds = filteredTimeRange.GetTotal();
                }
                else
                {
                  expandedStats.Add(stats);
                  StatsUtil.UpdateAllStatsTimeRanges(stats, PlayerTimeRanges, PlayerSubTimeRanges, startTime, stopTime);
                }

                StatsUtil.UpdateCalculations(stats, RaidTotals, resistCounts);

                if (RaidTotals.Specials.TryGetValue(stats.OrigName, out string special2))
                {
                  stats.Special = special2;
                }
              }
            });

            combined = new CombinedStats
            {
              RaidStats = RaidTotals,
              TargetTitle = (Selected.Count > 1 ? "Combined (" + Selected.Count + "): " : "") + Title,
              TimeTitle = string.Format(StatsUtil.TIME_FORMAT, RaidTotals.TotalSeconds),
              TotalTitle = string.Format(StatsUtil.TOTAL_FORMAT, StatsUtil.FormatTotals(RaidTotals.Total), " Damage ", StatsUtil.FormatTotals(RaidTotals.DPS))
            };

            combined.StatsList.AddRange(topLevelStats.Values.AsParallel().OrderByDescending(item => item.Total));
            combined.FullTitle = StatsUtil.FormatTitle(combined.TargetTitle, combined.TimeTitle, combined.TotalTitle);
            combined.ShortTitle = StatsUtil.FormatTitle(combined.TargetTitle, combined.TimeTitle, "");
            combined.ExpandedStatsList.AddRange(expandedStats.AsParallel().OrderByDescending(item => item.Total));

            for (int i = 0; i < combined.ExpandedStatsList.Count; i++)
            {
              combined.ExpandedStatsList[i].Rank = Convert.ToUInt16(i + 1);
              if (combined.StatsList.Count > i)
              {
                combined.StatsList[i].Rank = Convert.ToUInt16(i + 1);
                combined.UniqueClasses[combined.StatsList[i].ClassName] = 1;

                if (childrenStats.TryGetValue(combined.StatsList[i].Name, out Dictionary<string, PlayerStats> children))
                {
                  combined.Children.Add(combined.StatsList[i].Name, children.Values.OrderByDescending(stats => stats.Total).ToList());
                }
              }
            }

            // generating new stats
            var genEvent = new StatsGenerationEvent
            {
              Type = Labels.DAMAGEPARSE,
              State = "COMPLETED",
              CombinedStats = combined,
              Limited = damageValidator.IsDamageLimited()
            };

            genEvent.Groups.AddRange(DamageGroups);
            genEvent.UniqueGroupCount = DamageGroupIds.Count;
            EventsGenerationStatus?.Invoke(this, genEvent);

            FireChartEvent(options, "UPDATE");
          }
          catch (Exception ex)
          {
            LOG.Error(ex);
          }
        }
      }
    }

    private bool checkNewFrame(Dictionary<string, double> prevPlayerTimes, string name, double beginTime)
    {
      if (!prevPlayerTimes.TryGetValue(name, out double prevTime))
      {
        prevPlayerTimes[name] = beginTime;
        prevTime = beginTime;
      }

      var newFrame = (beginTime > prevTime);

      if (newFrame)
      {
        prevPlayerTimes[name] = beginTime;
      }

      return newFrame;
    }

    private void FireNewStatsEvent()
    {
      // generating new stats
      EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent { Type = Labels.DAMAGEPARSE, State = "STARTED" });
    }

    private void FireNoDataEvent(GenerateStatsOptions options, string state)
    {
      // nothing to do
      EventsGenerationStatus?.Invoke(this, new StatsGenerationEvent { Type = Labels.DAMAGEPARSE, State = state });
      FireChartEvent(options, "CLEAR");
    }

    internal void FireChartEvent(GenerateStatsOptions options, string action, List<PlayerStats> selected = null)
    {
      lock (DamageGroupIds)
      {
        // reset groups
        if (options.MaxSeconds == long.MinValue && AllDamageGroups != null)
        {
          DamageGroups = AllDamageGroups;
        }

        // send update
        DataPointEvent de = new DataPointEvent { Action = action, Iterator = new DamageGroupCollection(DamageGroups) };

        if (selected != null)
        {
          de.Selected.AddRange(selected);
        }

        EventsUpdateDataPoint?.Invoke(DamageGroups, de);
      }
    }

    private void Reset()
    {
      AllDamageGroups = DamageGroups;
      DamageGroups.Clear();
      DamageGroupIds.Clear();
      RaidTotals = StatsUtil.CreatePlayerStats(Labels.RAIDTOTALS);
      PlayerPets.Clear();
      PetToPlayer.Clear();
      Resists.Clear();
      PlayerTimeRanges.Clear();
      PlayerSubTimeRanges.Clear();
      Selected = null;
      Title = "";
    }

    private void UpdatePetMapping(DamageRecord damage)
    {
      string pname = PlayerManager.Instance.GetPlayerFromPet(damage.Attacker);
      if ((!string.IsNullOrEmpty(pname) && pname != Labels.UNASSIGNED) || !string.IsNullOrEmpty(pname = damage.AttackerOwner))
      {
        if (!PlayerPets.TryGetValue(pname, out ConcurrentDictionary<string, byte> mapping))
        {
          mapping = new ConcurrentDictionary<string, byte>();
          PlayerPets[pname] = mapping;
        }

        mapping[damage.Attacker] = 1;
        PetToPlayer[damage.Attacker] = pname;
      }
    }

    public StatsSummary BuildSummary(string type, CombinedStats currentStats, List<PlayerStats> selected, bool showPetLabel, bool showDPS,
      bool showTotals, bool rankPlayers, bool showSpecial, bool showTime, string customTitle)
    {
      List<string> list = new List<string>();

      string title = "";
      string details = "";

      if (currentStats != null && type == Labels.DAMAGEPARSE)
      {
        if (selected?.Count > 0)
        {
          foreach (PlayerStats stats in selected.OrderByDescending(item => item.Total))
          {
            string name = showPetLabel ? stats.Name : stats.Name.Replace(" +Pets", "");
            string playerFormat = rankPlayers ? string.Format(StatsUtil.PLAYER_RANK_FORMAT, stats.Rank, name) : string.Format(StatsUtil.PLAYER_FORMAT, name);
            string damageFormat = showDPS ? string.Format(StatsUtil.TOTAL_FORMAT, StatsUtil.FormatTotals(stats.Total), "", StatsUtil.FormatTotals(stats.DPS)) :
              string.Format(StatsUtil.TOTAL_ONLY_FORMAT, StatsUtil.FormatTotals(stats.Total));
            string timeFormat = string.Format(StatsUtil.TIME_FORMAT, stats.TotalSeconds);

            var dps = playerFormat + damageFormat;

            if (showTime)
            {
              dps += " " + timeFormat;
            }

            if (showSpecial && !string.IsNullOrEmpty(stats.Special))
            {
              dps = string.Format(StatsUtil.SPECIAL_FORMAT, dps, stats.Special);
            }

            list.Add(dps);
          }
        }

        details = list.Count > 0 ? ", " + string.Join(" | ", list) : "";
        var timeTitle = showTime ? currentStats.TimeTitle : "";
        var totals = showDPS ? currentStats.TotalTitle : currentStats.TotalTitle.Split(new string[] { " @" }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
        title = StatsUtil.FormatTitle(customTitle ?? currentStats.TargetTitle, timeTitle, showTotals ? totals : "");
      }

      return new StatsSummary { Title = title, RankedPlayers = details };
    }
  }
}

