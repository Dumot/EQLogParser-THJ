﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EQLogParser
{
  class StatsBuilder
  {
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private const int DEATH_TIME_OFFSET = 10; // seconds forward
    private const string RAID_PLAYER = "Totals";
    private const string TIME_FORMAT = "in {0}s";
    private const string DAMAGE_FORMAT = "{0} @{1}";
    private const string PLAYER_FORMAT = "{0} = ";
    private const string PLAYER_RANK_FORMAT = "{0}. {1} = ";
    private static DictionaryAddHelper<long, int> LongIntAddHelper = new DictionaryAddHelper<long, int>();
    private static DictionaryAddHelper<string, int> StringIntAddHelper = new DictionaryAddHelper<string, int>();

    internal static string BuildTitle(CombinedStats currentStats, bool showTotals = true)
    {
      string result;
      if (showTotals)
      {
        result = FormatTitle(currentStats.TargetTitle, currentStats.TimeTitle, currentStats.DamageTitle);
      }
      else
      {
        result = FormatTitle(currentStats.TargetTitle, currentStats.TimeTitle);
      }
      return result;
    }

    internal static StatsSummary BuildSummary(CombinedStats currentStats, List<PlayerStats> selected, bool showTotals, bool rankPlayers)
    {
      List<string> list = new List<string>();

      string title = "";
      string details = "";
      string shortTitle = "";

      if (currentStats != null)
      {
        if (selected != null)
        {
          foreach (PlayerStats stats in selected.OrderByDescending(item => item.TotalDamage))
          {
            string playerFormat = rankPlayers ? String.Format(PLAYER_RANK_FORMAT, stats.Rank, stats.Name) : String.Format(PLAYER_FORMAT, stats.Name);
            string damageFormat = String.Format(DAMAGE_FORMAT, Helpers.FormatDamage(stats.TotalDamage), Helpers.FormatDamage(stats.DPS));
            string timeFormat = String.Format(TIME_FORMAT, stats.TotalSeconds);
            list.Add(playerFormat + damageFormat + " " + timeFormat);
          }
        }

        details = list.Count > 0 ? ", " + string.Join(", ", list) : "";
        title = BuildTitle(currentStats, showTotals);
        shortTitle = BuildTitle(currentStats, false);
      }

      return new StatsSummary() { Title = title, RankedPlayers = details, ShortTitle = shortTitle };
    }

    internal static CombinedStats BuildTotalStats(List<NonPlayer> selected)
    {
      CombinedStats combined = new CombinedStats();
      ConcurrentDictionary<string, PlayerStats> individualStats = new ConcurrentDictionary<string, PlayerStats>();
      PlayerStats raidTotals = CreatePlayerStats(RAID_PLAYER);

      Dictionary<string, List<string>> needAggregate = new Dictionary<string, List<string>>();
      Dictionary<string, List<NonPlayer>> aggregateNpcStats = new Dictionary<string, List<NonPlayer>>();
      DictionaryListHelper<string, string> needAggregateHelper = new DictionaryListHelper<string, string>();
      DictionaryListHelper<string, NonPlayer> aggregateNpcStatsHelper = new DictionaryListHelper<string, NonPlayer>();
      DictionaryListHelper<string, PlayerStats> statsHelper = new DictionaryListHelper<string, PlayerStats>();
      Dictionary<string, byte> uniqueClasses = new Dictionary<string, byte>();
      ConcurrentDictionary<string, byte> uniquePlayers = new ConcurrentDictionary<string, byte>();
      Dictionary<string, int> ResistMap = new Dictionary<string, int>();

      try
      {
        string title = selected.First().Name;

        foreach (NonPlayer npc in selected.OrderBy(item => item.ID))
        {
          if (npc.BeginTimeString == NonPlayer.BREAK_TIME)
          {
            continue;
          }

          foreach (var key in npc.DamageMap.Keys)
          {
            if (!DataManager.Instance.IsProbablyNotAPlayer(key))
            {
              PlayerStats playerTotals;
              DamageStats npcStats = npc.DamageMap[key];

              if (!individualStats.ContainsKey(key))
              {
                playerTotals = CreatePlayerStats(key);
                individualStats[key] = playerTotals;
                if (playerTotals.ClassName != "")
                {
                  lock (uniqueClasses)
                  {
                    uniqueClasses[playerTotals.ClassName] = 1;
                  }
                }
              }
              else
              {
                playerTotals = individualStats[key];
              }

              // track player names
              uniquePlayers[key] = 1;

              // see if there's a pet mapping, check this first
              string parent = DataManager.Instance.GetPlayerFromPet(key);
              if (parent != null)
              {
                needAggregateHelper.AddToList(needAggregate, parent, key);
              }
              else if (npcStats.Owner != "" && npcStats.IsPet)
              {
                needAggregateHelper.AddToList(needAggregate, npcStats.Owner, key);
              }

              aggregateNpcStatsHelper.AddToList(aggregateNpcStats, key, npc);
              UpdateTotals(playerTotals, npcStats);
              UpdateTotals(raidTotals, npcStats);

              // only the current player will see resist messages
              if (DataManager.Instance.PlayerName == key && npc.ResistMap.ContainsKey(key))
              {
                Parallel.ForEach(npc.ResistMap[key], keyvalue =>
                {
                  StringIntAddHelper.Add(ResistMap, keyvalue.Key, keyvalue.Value);
                });
              }
            }
          }
        }

        raidTotals.TotalSeconds = raidTotals.TimeDiffs.Sum();
        raidTotals.DPS = (long)Math.Round(raidTotals.TotalDamage / raidTotals.TotalSeconds, 2);

        combined.RaidStats = raidTotals;
        combined.TargetTitle = (selected.Count > 1 ? "Combined (" + selected.Count + "): " : "") + title;
        combined.TimeTitle = string.Format(TIME_FORMAT, combined.RaidStats.TotalSeconds);
        combined.DamageTitle = string.Format(DAMAGE_FORMAT, Helpers.FormatDamage(raidTotals.TotalDamage), Helpers.FormatDamage(raidTotals.DPS));
        combined.UniqueClasses = uniqueClasses;

        // get death counts
        Dictionary<string, int> deathCounts = GetPlayerDeaths(raidTotals);

        // save them all before child code removes
        var allStatValues = individualStats.Values.ToList();

        combined.Children = new Dictionary<string, List<PlayerStats>>();
        if (needAggregate.Count > 0)
        {
          Parallel.ForEach(needAggregate, pair =>
          {
            string aggregateName = (pair.Key == DataManager.UNASSIGNED_PET_OWNER) ? pair.Key : pair.Key + " +Pets";
            PlayerStats aggregatePlayerStats = CreatePlayerStats(aggregateName, pair.Key);
            List<string> all = pair.Value.ToList();
            all.Add(pair.Key);

            int deaths = 0;
            List<DamageStats> allDamageStats = new List<DamageStats>();
            foreach (string child in all)
            {
              lock (aggregateNpcStats)
              {
                if (aggregateNpcStats.ContainsKey(child) && individualStats.ContainsKey(child))
                {
                  // only for children of aggregates, all original stats updated below
                  if (deathCounts.ContainsKey(individualStats[child].Name))
                  {
                    deaths += deathCounts[individualStats[child].Name];
                  }

                  statsHelper.AddToList(combined.Children, aggregateName, individualStats[child]);

                  PlayerStats removed;
                  individualStats.TryRemove(child, out removed);

                  foreach (NonPlayer npc in aggregateNpcStats[child])
                  {
                    allDamageStats.Add(npc.DamageMap[child]);
                  }
                }
              }
            }

            // update total death count
            aggregatePlayerStats.Deaths = deaths;

            allDamageStats.OrderBy(dStats => dStats.BeginTime).ToList().ForEach(dStats =>
            {
              UpdateTotals(aggregatePlayerStats, dStats);
            });

            aggregatePlayerStats.TotalSeconds = aggregatePlayerStats.TimeDiffs.Sum();
            aggregatePlayerStats.DPS = (long) Math.Round(aggregatePlayerStats.TotalDamage / aggregatePlayerStats.TotalSeconds, 2);
            aggregatePlayerStats.SDPS = (long) Math.Round(aggregatePlayerStats.TotalDamage / combined.RaidStats.TotalSeconds, 2);

            individualStats[aggregateName] = aggregatePlayerStats;

            // figure out percents
            lock (combined.Children)
            {
              if (combined.Children.ContainsKey(aggregateName))
              {
                foreach (PlayerStats childStat in combined.Children[aggregateName])
                {
                  if (aggregatePlayerStats.TotalDamage > 0)
                  {
                    childStat.Percent = Math.Round(((decimal)childStat.TotalDamage / aggregatePlayerStats.TotalDamage) * 100, 2);
                  }

                  if (combined.RaidStats.TotalDamage > 0)
                  {
                    childStat.PercentOfRaid = Math.Round((decimal)childStat.TotalDamage / combined.RaidStats.TotalDamage * 100, 2);
                  }
                }
              }
            }
          });
        }

        PlayerStats myStats = null;
        Parallel.ForEach(allStatValues, (stat) =>
        {
          stat.TotalSeconds = stat.TimeDiffs.Sum();
          stat.DPS = (long) Math.Round(stat.TotalDamage / stat.TotalSeconds, 2);
          stat.SDPS = (long) Math.Round(stat.TotalDamage / combined.RaidStats.TotalSeconds, 2);

          foreach (var subStat in stat.SubStats.Values.OrderByDescending(item => item.TotalDamage))
          {
            subStat.TotalSeconds = subStat.TimeDiffs.Sum();
            subStat.DPS = (long) Math.Round(subStat.TotalDamage / subStat.TotalSeconds, 2);
            subStat.SDPS = (long) Math.Round(subStat.TotalDamage / combined.RaidStats.TotalSeconds, 2);

            if (stat.TotalDamage > 0)
            {
              subStat.Percent = Math.Round(stat.Percent / 100 * ((decimal)subStat.TotalDamage / stat.TotalDamage) * 100, 2);
            }

            if (stat.Name == DataManager.Instance.PlayerName)
            {
              myStats = stat;

              if (ResistMap.ContainsKey(subStat.Name))
              {
                lock (ResistMap)
                {
                  subStat.Resists = ResistMap[subStat.Name];
                  ResistMap.Remove(subStat.Name);
                }

                int hits = (subStat.Hits + subStat.Resists);
                if (hits > 0)
                {
                  subStat.ResistRate = Math.Round(Convert.ToDecimal(subStat.Resists) / hits * 100, 2);
                }
              }
            }
          }

          // update death count
          if (deathCounts.ContainsKey(stat.Name))
          {
            stat.Deaths = deathCounts[stat.Name];
          }
        });

        // left over spells that were resisted but never showed up in the damage map
        if (ResistMap.Count > 0 && myStats != null)
        {
          foreach (var keyvalue in ResistMap)
          {
            string spellName = Helpers.AbbreviateSpellName(keyvalue.Key);
            var spellData = DataManager.Instance.GetSpellByAbbrv(spellName);
            if (spellData != null && spellData.Damaging)
            {
              var resisted = new PlayerSubStats()
              {
                ClassName = "",
                Name = keyvalue.Key,
                Type = "Resisted",
                CritFreqValues = new Dictionary<long, int>(),
                NonCritFreqValues = new Dictionary<long, int>(),
                ResistRate = 100,
                Resists = keyvalue.Value
              };

              if (!myStats.SubStats.ContainsKey(resisted.Name))
              {
                myStats.SubStats.Add(resisted.Name, resisted);
              }
            }
          }
        }

        int lastRank = 0;
        combined.StatsList = individualStats.Values.OrderByDescending(item => item.TotalDamage).ToList();
        for (int i = 0; i < combined.StatsList.Count; i++)
        {
          combined.StatsList[i].Rank = i + 1;
          lastRank = combined.StatsList[i].Rank;
          if (combined.Children.ContainsKey(combined.StatsList[i].Name))
          {
            combined.Children[combined.StatsList[i].Name] = combined.Children[combined.StatsList[i].Name].OrderByDescending(item => item.TotalDamage).ToList();
          }

          // total percents
          combined.StatsList[i].PercentOfRaid = Math.Round((decimal) combined.StatsList[i].TotalDamage / combined.RaidStats.TotalDamage * 100, 2);
        }

        // look for people casting during this time frame who did not do any damage and append them
        List<string> casters = SpellCountBuilder.GetPlayersCastingDuring(combined.RaidStats);
        if (casters.Count > 0)
        {
          foreach (var caster in casters.AsParallel().Where(caster => !uniquePlayers.ContainsKey(caster) && DataManager.Instance.CheckNameForPlayer(caster)))
          {
            var zeroStats = CreatePlayerStats(caster);
            zeroStats.Rank = ++lastRank;
            combined.StatsList.Add(zeroStats);
            combined.UniqueClasses[zeroStats.ClassName] = 1;
          }
        }
      }
      catch (Exception e)
      {
        LOG.Error(e);
      }

      return combined;
    }

    internal static Dictionary<string, List<HitFreqChartData>> GetHitFreqValues(CombinedStats combined, PlayerStats playerStats)
    {
      Dictionary<string, List<HitFreqChartData>> results = new Dictionary<string, List<HitFreqChartData>>();

      // get chart data for player and pets if available
      List<PlayerStats> list = new List<PlayerStats>();
      if (combined.Children.ContainsKey(playerStats.Name))
      {
        list.AddRange(combined.Children[playerStats.Name]);
      }
      else
      {
        list.Add(playerStats);
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

      return results;
    }

    internal static ChartData GetDPSValues(CombinedStats combined, List<PlayerStats> stats, NpcDamageManager damageManager)
    {
      List<string> labels = new List<string>();
      DictionaryAddHelper<string, long> addHelper = new DictionaryAddHelper<string, long>();
      Dictionary<string, List<long>> playerValues = new Dictionary<string, List<long>>();

      if (stats.Count > 0)
      {
        try
        {
          // sort stats
          stats = stats.OrderBy(item => item.Name).ToList();
          int interval = combined.RaidStats.TotalSeconds < 12 ? 1 : (int)combined.RaidStats.TotalSeconds / 12;

          foreach (var timeIndex in Enumerable.Range(0, combined.RaidStats.BeginTimes.Count))
          {
            DateTime firstTime = combined.RaidStats.BeginTimes[timeIndex];
            DateTime lastTime = combined.RaidStats.LastTimes[timeIndex];
            DateTime currentTime = firstTime;

            int index = 0;
            var damages = damageManager.GetDamageStartingAt(firstTime);

            Dictionary<string, long> playerTotals = new Dictionary<string, long>();
            labels.Add(Helpers.FormatDateTime(firstTime));
            while (currentTime <= lastTime)
            {
              while (index < damages.Count && damages[index].CurrentTime <= currentTime)
              {
                Dictionary<string, long> playerDamage = damages[index].PlayerDamage;
                foreach (var stat in stats)
                {
                  if (combined.Children.ContainsKey(stat.Name))
                  {
                    combined.Children[stat.Name].ForEach(child =>
                    {
                      if (playerDamage.ContainsKey(child.Name))
                      {
                        addHelper.Add(playerTotals, stat.Name, playerDamage[child.Name]);
                      }
                    });
                  }
                  else if (playerDamage.ContainsKey(stat.Name))
                  {
                    addHelper.Add(playerTotals, stat.Name, playerDamage[stat.Name]);
                  }
                }

                index++;
              }

              foreach (var stat in stats)
              {
                if (!playerValues.ContainsKey(stat.Name))
                {
                  playerValues[stat.Name] = new List<long>();
                }

                long dps = 0;
                if (playerTotals.ContainsKey(stat.Name))
                {
                  dps = (long) Math.Round(playerTotals[stat.Name] / ((currentTime - firstTime).TotalSeconds + 1), 2);
                }

                playerValues[stat.Name].Add(dps);
              }

              labels.Add(Helpers.FormatDateTime(currentTime));

              if (currentTime == lastTime)
              {
                break;
              }
              else
              {
                currentTime = currentTime.AddSeconds(interval);
                if (currentTime > lastTime)
                {
                  currentTime = lastTime;
                }
              }
            }
          }

          // scale down if too many points
          int scale = -1;
          var firstPlayer = playerValues.Values.First();
          if (firstPlayer.Count > 20)
          {
            scale = firstPlayer.Count / 20;
          }

          if (scale > 1)
          {
            Dictionary<string, List<long>> newPlayerValues = new Dictionary<string, List<long>>();
            Parallel.ForEach(playerValues.Keys, (player) =>
            {
              var newList = playerValues[player].Where((item, i) => i % scale == 0).ToList();

              lock (newPlayerValues)
              {
                newPlayerValues[player] = newList;
              }
            });

            playerValues = newPlayerValues;
            labels = labels.Where((item, i) => i % scale == 0).ToList();
          }
        }
        catch (Exception ex)
        {
          LOG.Error(ex);
        }
      }

      return new ChartData() { Values = playerValues, XAxisLabels = labels };
    }

    internal static List<PlayerStats> GetSelectedPlayerStatsByClass(string classString, ItemCollection items)
    {
      DataManager.SpellClasses type = (DataManager.SpellClasses) Enum.Parse(typeof(DataManager.SpellClasses), classString);
      string className = DataManager.Instance.GetClassName(type);

      List<PlayerStats> selectedStats = new List<PlayerStats>();
      foreach (var item in items)
      {
        PlayerStats stats = item as PlayerStats;
        if (stats.ClassName == className)
        {
          selectedStats.Add(stats);
        }
      }

      return selectedStats;
    }

    private static Dictionary<string, int> GetPlayerDeaths(PlayerStats raidStats)
    {
      Dictionary<string, int> deathCounts = new Dictionary<string, int>();

      if (raidStats.BeginTimes.Count > 0 && raidStats.LastTimes.Count > 0)
      {
        DateTime beginTime = raidStats.BeginTimes.First();
        DateTime endTime = raidStats.LastTimes.Last().AddSeconds(DEATH_TIME_OFFSET); ;

        Parallel.ForEach(DataManager.Instance.GetPlayerDeathsDuring(beginTime, endTime), death =>
        {
          StringIntAddHelper.Add(deathCounts, death.Player, 1);
        });
      }

      return deathCounts;
    }

    private static string FormatTitle(string targetTitle, string timeTitle, string damageTitle = "")
    {
      string result;
      result = targetTitle + " " + timeTitle;
      if (damageTitle != "")
      {
        result += ", " + damageTitle;
      }
      return result;
    }

    private static void UpdateTotals(PlayerStats playerTotals, DamageStats npcStats)
    {
      playerTotals.TotalDamage += npcStats.TotalDamage;
      playerTotals.TotalCritDamage += npcStats.TotalCritDamage;
      playerTotals.TotalLuckyDamage += npcStats.TotalLuckyDamage;
      playerTotals.BaneHits += npcStats.BaneCount;
      playerTotals.Hits += npcStats.Count;
      playerTotals.CritHits += npcStats.CritCount;
      playerTotals.LuckyHits += npcStats.LuckyCount;
      playerTotals.TwincastHits += npcStats.TwincastCount;
      playerTotals.Max = (playerTotals.Max < npcStats.Max) ? npcStats.Max : playerTotals.Max;

      UpdateTimeDiffs(playerTotals, npcStats);

      if (playerTotals.Hits > 0)
      {
        playerTotals.Avg = (long)Math.Round(Convert.ToDecimal(playerTotals.TotalDamage) / playerTotals.Hits, 2);
        playerTotals.CritRate = Math.Round(Convert.ToDecimal(playerTotals.CritHits) / playerTotals.Hits * 100, 2);
        playerTotals.LuckRate = Math.Round(Convert.ToDecimal(playerTotals.LuckyHits) / playerTotals.Hits * 100, 2);
      }

      if ((playerTotals.CritHits - playerTotals.LuckyHits) > 0)
      {
        playerTotals.AvgCrit = (long) Math.Round(Convert.ToDecimal(playerTotals.TotalCritDamage) / (playerTotals.CritHits - playerTotals.LuckyHits), 2);
      }
      if (playerTotals.LuckyHits > 0)
      {
        playerTotals.AvgLucky = (long) Math.Round(Convert.ToDecimal(playerTotals.TotalLuckyDamage) / playerTotals.LuckyHits, 2);
      }

      Parallel.ForEach(npcStats.HitMap, keyvalue => UpdateHitMap(playerTotals, keyvalue));
      Parallel.ForEach(npcStats.SpellDoTMap, keyvalue =>
      {
        UpdateHitMap(playerTotals, keyvalue, "DoT");
      });
      Parallel.ForEach(npcStats.SpellDDMap, keyvalue =>
      {
        UpdateHitMap(playerTotals, keyvalue, "DD");
      });
      Parallel.ForEach(npcStats.SpellProcMap, keyvalue =>
      {
        UpdateHitMap(playerTotals, keyvalue, "Proc");
      });
    }

    private static void UpdateTimeDiffs(PlayerSubStats subStats, Hit npcStats)
    {
      int currentIndex = subStats.BeginTimes.Count - 1;
      if (currentIndex == -1)
      {
        subStats.BeginTimes.Add(npcStats.BeginTime);
        subStats.LastTimes.Add(npcStats.LastTime);
        subStats.TimeDiffs.Add(0); // update afterward
        currentIndex = 0;
      }
      else if (subStats.LastTimes[currentIndex] >= npcStats.BeginTime)
      {
        if (npcStats.LastTime > subStats.LastTimes[currentIndex])
        {
          subStats.LastTimes[currentIndex] = npcStats.LastTime;
        }
      }
      else
      {
        subStats.BeginTimes.Add(npcStats.BeginTime);
        subStats.LastTimes.Add(npcStats.LastTime);
        subStats.TimeDiffs.Add(0); // update afterward
        currentIndex++;
      }

      subStats.TimeDiffs[currentIndex] = subStats.LastTimes[currentIndex].Subtract(subStats.BeginTimes[currentIndex]).TotalSeconds + 1;
    }

    private static void UpdateHitMap(PlayerStats playerTotals, KeyValuePair<string, Hit> keyvalue, string type = null)
    {
      PlayerSubStats subStats = null;
      lock (playerTotals)
      {
        if (!playerTotals.SubStats.ContainsKey(keyvalue.Key))
        {
          subStats = new PlayerSubStats()
          {
            ClassName = "",
            Name = keyvalue.Key,
            Type = type,
            CritFreqValues = new Dictionary<long, int>(),
            NonCritFreqValues = new Dictionary<long, int>(),
            BeginTimes = new List<DateTime>(),
            LastTimes = new List<DateTime>(),
            TimeDiffs = new List<double>()
          };

          playerTotals.SubStats[keyvalue.Key] = subStats;
        }
        else
        {
          subStats = playerTotals.SubStats[keyvalue.Key];
        }
      }

      Hit hitMap = keyvalue.Value;
      subStats.TotalDamage += hitMap.TotalDamage;
      subStats.TotalCritDamage += hitMap.TotalCritDamage;
      subStats.TotalLuckyDamage += hitMap.TotalLuckyDamage;
      subStats.BaneHits += hitMap.BaneCount;
      subStats.Hits += hitMap.Count;
      subStats.CritHits += hitMap.CritCount;
      subStats.LuckyHits += hitMap.LuckyCount;
      subStats.TwincastHits += hitMap.TwincastCount;
      subStats.Max = (subStats.Max < hitMap.Max) ? hitMap.Max : subStats.Max;

      UpdateTimeDiffs(subStats, hitMap);
      //var diff = (hitMap.LastTime - hitMap.BeginTime).TotalSeconds;
      //subStats.TotalSeconds += diff == 0 ? 1 : diff;

      subStats.Avg = (long) Math.Round(Convert.ToDecimal(subStats.TotalDamage) / subStats.Hits, 2);
      subStats.CritRate = Math.Round(Convert.ToDecimal(subStats.CritHits) / subStats.Hits * 100, 2);
      subStats.LuckRate = Math.Round(Convert.ToDecimal(subStats.LuckyHits) / subStats.Hits * 100, 2);
      subStats.TwincastRate = Math.Round(Convert.ToDecimal(subStats.TwincastHits) / subStats.Hits * 100, 2);
      hitMap.CritFreqValues.Keys.ToList().ForEach(k => LongIntAddHelper.Add(subStats.CritFreqValues, k, hitMap.CritFreqValues[k]));
      hitMap.NonCritFreqValues.Keys.ToList().ForEach(k => LongIntAddHelper.Add(subStats.NonCritFreqValues, k, hitMap.NonCritFreqValues[k]));

      if ((subStats.CritHits - subStats.LuckyHits) > 0)
      {
        subStats.AvgCrit = (long) Math.Round(Convert.ToDecimal(subStats.TotalCritDamage) / (subStats.CritHits - subStats.LuckyHits), 2);
      }
      if (subStats.LuckyHits > 0)
      {
        subStats.AvgLucky = (long) Math.Round(Convert.ToDecimal(subStats.TotalLuckyDamage) / subStats.LuckyHits, 2);
      }
    }

    private static PlayerStats CreatePlayerStats(string name, string origName = null)
    {
      string className = "";
      origName = origName == null ? name : origName;

      if (!DataManager.Instance.CheckNameForPet(origName))
      {
        className = DataManager.Instance.GetPlayerClass(origName);
      }

      return new PlayerStats()
      {
        Name = name,
        ClassName = className,
        OrigName = origName,
        Percent = 100, // until something says otherwise
        BeginTimes = new List<DateTime>(),
        LastTimes = new List<DateTime>(),
        SubStats = new Dictionary<string, PlayerSubStats>(),
        TimeDiffs = new List<double>()
      };
    }
  }
}

