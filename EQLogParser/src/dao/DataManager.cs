﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EQLogParser
{
  internal enum SpellClass
  {
    WAR = 1, CLR = 2, PAL = 4, RNG = 8, SHD = 16, DRU = 32, MNK = 64, BRD = 128, ROG = 256,
    SHM = 512, NEC = 1024, WIZ = 2048, MAG = 4096, ENC = 8192, BST = 16384, BER = 32768, PET = 65536
  }

  internal enum SpellTarget
  {
    LOS = 1, CASTERAE = 2, CASTERGROUP = 3, CASTERPB = 4, SINGLETARGET = 5, SELF = 6, TARGETAE = 8,
    NEARBYPLAYERSAE = 40, DIRECTIONAE = 42, TARGETRINGAE = 45
  }

  class DataManager
  {
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    internal static DataManager Instance = new DataManager();
    internal event EventHandler<Fight> EventsNewInactiveFight;
    internal event EventHandler<string> EventsRemovedFight;
    internal event EventHandler<Fight> EventsNewFight;
    internal event EventHandler<Fight> EventsRefreshFight;
    internal event EventHandler<bool> EventsClearedActiveData;

    internal const int FIGHT_TIMEOUT = 24;
    internal const double BUFFS_OFFSET = 90;

    private static readonly SpellAbbrvComparer AbbrvComparer = new SpellAbbrvComparer();
    private static readonly TimedActionComparer TAComparer = new TimedActionComparer();

    private readonly List<ActionBlock> AllMiscBlocks = new List<ActionBlock>();
    private readonly List<ActionBlock> AllDeathBlocks = new List<ActionBlock>();
    private readonly List<ActionBlock> AllHealBlocks = new List<ActionBlock>();
    private readonly List<ActionBlock> AllSpellCastBlocks = new List<ActionBlock>();
    private readonly List<ActionBlock> AllReceivedSpellBlocks = new List<ActionBlock>();
    private readonly List<ActionBlock> AllResistBlocks = new List<ActionBlock>();
    private readonly List<ActionBlock> AllLootBlocks = new List<ActionBlock>();
    private readonly List<TimedAction> AllSpecialActions = new List<TimedAction>();

    private readonly Dictionary<string, byte> AllNpcs = new Dictionary<string, byte>();
    private readonly Dictionary<string, List<SpellData>> PosessiveLandsOnOthers = new Dictionary<string, List<SpellData>>();
    private readonly Dictionary<string, List<SpellData>> NonPosessiveLandsOnOthers = new Dictionary<string, List<SpellData>>();
    private readonly Dictionary<string, List<SpellData>> LandsOnYou = new Dictionary<string, List<SpellData>>();
    private readonly Dictionary<string, SpellData> SpellsNameDB = new Dictionary<string, SpellData>();
    private readonly Dictionary<string, SpellData> SpellsAbbrvDB = new Dictionary<string, SpellData>();
    private readonly Dictionary<string, SpellClass> SpellsToClass = new Dictionary<string, SpellClass>();

    private readonly ConcurrentDictionary<string, Fight> ActiveFights = new ConcurrentDictionary<string, Fight>();
    private readonly ConcurrentDictionary<string, byte> LifetimeFights = new ConcurrentDictionary<string, byte>();
    private int LastSpellIndex = -1;

    private DataManager()
    {
      DictionaryListHelper<string, SpellData> helper = new DictionaryListHelper<string, SpellData>();
      var spellList = new List<SpellData>();

      ConfigUtil.ReadList(@"data\spells.txt").ForEach(line =>
      {
        try
        {
          var spellData = TextFormatUtils.ParseCustomSpellData(line);
          if (spellData != null)
          {
            spellList.Add(spellData);
            SpellsNameDB[spellData.Name] = spellData;

            if (!SpellsAbbrvDB.ContainsKey(spellData.NameAbbrv))
            {
              SpellsAbbrvDB[spellData.NameAbbrv] = spellData;
            }
            else if (string.Compare(SpellsAbbrvDB[spellData.NameAbbrv].Name, spellData.Name, true, CultureInfo.CurrentCulture) < 0)
            {
              // try to keep the newest version
              SpellsAbbrvDB[spellData.NameAbbrv] = spellData;
            }

            if (spellData.LandsOnOther.StartsWith("'s ", StringComparison.Ordinal))
            {
              spellData.LandsOnOther = spellData.LandsOnOther.Substring(3);
              helper.AddToList(PosessiveLandsOnOthers, spellData.LandsOnOther, spellData);
            }
            else if (spellData.LandsOnOther.Length > 1)
            {
              spellData.LandsOnOther = spellData.LandsOnOther.Substring(1);
              helper.AddToList(NonPosessiveLandsOnOthers, spellData.LandsOnOther, spellData);
            }

            if (spellData.LandsOnYou.Length > 0) // just do stuff in common
            {
              helper.AddToList(LandsOnYou, spellData.LandsOnYou, spellData);
            }
          }
        }
        catch (OverflowException ex)
        {
          LOG.Error("Error reading spell data", ex);
        }
      });

      // sort by duration for the timeline to pick better options
      foreach (var key in NonPosessiveLandsOnOthers.Keys)
      {
        NonPosessiveLandsOnOthers[key].Sort((a, b) =>
        {
          int result = b.Duration.CompareTo(a.Duration);
          return result != 0 ? result : string.Compare(b.ID, a.ID, true, CultureInfo.CurrentCulture);
        });
      }

      foreach (var key in PosessiveLandsOnOthers.Keys)
      {
        PosessiveLandsOnOthers[key].Sort((a, b) =>
        {
          int result = b.Duration.CompareTo(a.Duration);
          return result != 0 ? result : string.Compare(b.ID, a.ID, true, CultureInfo.CurrentCulture);
        });
      }

      foreach (var key in LandsOnYou.Keys)
      {
        LandsOnYou[key].Sort((a, b) =>
        {
          int result = b.Duration.CompareTo(a.Duration);
          return result != 0 ? result : string.Compare(b.ID, a.ID, true, CultureInfo.CurrentCulture);
        });
      }

      Dictionary<string, byte> keepOut = new Dictionary<string, byte>();
      var classEnums = Enum.GetValues(typeof(SpellClass)).Cast<SpellClass>().ToList();
      spellList.ForEach(spell =>
      {
        // exact match meaning class-only spell that are of certain target types
        var tgt = (SpellTarget)spell.Target;
        if ((tgt == SpellTarget.SELF || (spell.Level <= 250 && (tgt == SpellTarget.SINGLETARGET || tgt == SpellTarget.LOS))) && classEnums.Contains((SpellClass)spell.ClassMask))
        {
          // these need to be unique and keep track if a conflict is found
          if (SpellsToClass.ContainsKey(spell.Name))
          {
            SpellsToClass.Remove(spell.Name);
            keepOut[spell.Name] = 1;
          }
          else if (!keepOut.ContainsKey(spell.Name))
          {
            SpellsToClass[spell.Name] = (SpellClass)spell.ClassMask;
          }
        }
      });

      // load NPCs
      ConfigUtil.ReadList(@"data\npcs.txt").ForEach(line => AllNpcs[line.Trim()] = 1);

      PlayerManager.Instance.EventsNewTakenPetOrPlayerAction += (sender, name) => RemoveFight(name);
      PlayerManager.Instance.EventsNewVerifiedPlayer += (sender, name) => RemoveFight(name);
      PlayerManager.Instance.EventsNewVerifiedPet += (sender, name) => RemoveFight(name);
    }

    internal void AddDeathRecord(DeathRecord record, double beginTime) => Helpers.AddAction(AllDeathBlocks, record, beginTime);

    internal void AddLootRecord(LootRecord record, double beginTime) => Helpers.AddAction(AllLootBlocks, record, beginTime);

    internal void AddMiscRecord(IAction action, double beginTime) => Helpers.AddAction(AllMiscBlocks, action, beginTime);

    internal void AddResistRecord(ResistRecord record, double beginTime) => Helpers.AddAction(AllResistBlocks, record, beginTime);

    internal void AddReceivedSpell(ReceivedSpell received, double beginTime) => Helpers.AddAction(AllReceivedSpellBlocks, received, beginTime);

    internal List<ActionBlock> GetAllLoot() => AllLootBlocks.ToList();

    internal List<ActionBlock> GetCastsDuring(double beginTime, double endTime) => SearchActions(AllSpellCastBlocks, beginTime, endTime);

    internal List<ActionBlock> GetDeathsDuring(double beginTime, double endTime) => SearchActions(AllDeathBlocks, beginTime, endTime);

    internal List<ActionBlock> GetHealsDuring(double beginTime, double endTime) => SearchActions(AllHealBlocks, beginTime, endTime);

    internal List<ActionBlock> GetMiscDuring(double beginTime, double endTime) => SearchActions(AllMiscBlocks, beginTime, endTime);

    internal List<ActionBlock> GetResistsDuring(double beginTime, double endTime) => SearchActions(AllResistBlocks, beginTime, endTime);

    internal List<ActionBlock> GetReceivedSpellsDuring(double beginTime, double endTime) => SearchActions(AllReceivedSpellBlocks, beginTime, endTime);

    internal List<Fight> GetActiveFights() => ActiveFights.Values.ToList();

    internal void CheckExpireFights(double currentTime)
    {
      ActiveFights.Values.Where(fight => (currentTime - fight.LastTime) > FIGHT_TIMEOUT).ToList().ForEach(fight => RemoveActiveFight(fight.CorrectMapKey));
    }

    internal Fight GetFight(string name)
    {
      Fight result = null;
      if (!string.IsNullOrEmpty(name))
      {
        if (char.IsUpper(name[0]))
        {
          if (!ActiveFights.TryGetValue(name, out result))
          {
            ActiveFights.TryGetValue(Helpers.ToLower(name), out result);
          }
        }
        else
        {
          if (!ActiveFights.TryGetValue(name, out result))
          {
            ActiveFights.TryGetValue(Helpers.ToUpper(name), out result);
          }
        }
      }

      return result;
    }

    internal bool IsLifetimeNpc(string name)
    {
      bool result = false;
      if (!string.IsNullOrEmpty(name))
      {
        if (char.IsUpper(name[0]))
        {
          result = LifetimeFights.ContainsKey(name) || LifetimeFights.ContainsKey(Helpers.ToLower(name));
        }
        else
        {
          result = LifetimeFights.ContainsKey(name) || LifetimeFights.ContainsKey(Helpers.ToUpper(name));
        }
      }

      return result;
    }

    internal void AddSpecial(TimedAction action)
    {
      lock (AllSpecialActions)
      {
        AllSpecialActions.Add(action);
      }
    }

    internal void AddHealRecord(HealRecord record, double beginTime)
    {
      record.Healer = PlayerManager.Instance.ReplacePlayer(record.Healer, record.Healed);
      record.Healed = PlayerManager.Instance.ReplacePlayer(record.Healed, record.Healer);
      Helpers.AddAction(AllHealBlocks, record, beginTime);
    }

    internal void HandleSpellInterrupt(string player, string spell, double beginTime)
    {
      for (int i = AllSpellCastBlocks.Count - 1; i >= 0 && beginTime - AllSpellCastBlocks[i].BeginTime <= 5; i--)
      {
        int index = AllSpellCastBlocks[i].Actions.FindLastIndex(action => ((SpellCast)action).Spell == spell && ((SpellCast)action).Caster == player);
        if (index > -1)
        {
          AllSpellCastBlocks[i].Actions.RemoveAt(index);
          break;
        }
      }
    }

    internal void AddSpellCast(SpellCast cast, double beginTime)
    {
      if (SpellsNameDB.ContainsKey(cast.Spell))
      {
        Helpers.AddAction(AllSpellCastBlocks, cast, beginTime);
        LastSpellIndex = AllSpellCastBlocks.Count - 1;

        if (SpellsToClass.TryGetValue(cast.Spell, out SpellClass theClass))
        {
          PlayerManager.Instance.UpdatePlayerClassFromSpell(cast, theClass);
        }
      }
    }

    internal List<TimedAction> GetSpecials()
    {
      List<TimedAction> sorted;
      lock(AllSpecialActions)
      {
        sorted = AllSpecialActions.OrderBy(special => special.BeginTime).ToList();
      }
      return sorted;
    }

    internal SpellData GetSpellByAbbrv(string abbrv)
    {
      SpellData result = null;
      if (abbrv.Length > 0 && abbrv != Labels.UNKSPELL && SpellsAbbrvDB.ContainsKey(abbrv))
      {
        result = SpellsAbbrvDB[abbrv];
      }
      return result;
    }

    internal SpellData GetSpellByName(string name)
    {
      SpellData result = null;
      if (name.Length > 0 && name != Labels.UNKSPELL && SpellsNameDB.ContainsKey(name))
      {
        result = SpellsNameDB[name];
      }
      return result;
    }

    internal bool IsPlayerSpell(string name)
    {
      var spellData = GetSpellByName(name);
      return spellData?.ClassMask > 0;
    }

    internal List<SpellData> GetNonPosessiveLandsOnOther(string player, string value, out List<SpellData> output)
    {
      List<SpellData> result = null;
      if (NonPosessiveLandsOnOthers.TryGetValue(value, out output))
      {
        result = FindByLandsOn(player, output);
      }
      return result;
    }

    internal List<SpellData> GetPosessiveLandsOnOther(string player, string value, out List<SpellData> output)
    {
      List<SpellData> result = null;
      if (PosessiveLandsOnOthers.TryGetValue(value, out output))
      {
        result = FindByLandsOn(player, output);
      }
      return result;
    }

    internal List<SpellData> GetLandsOnYou(string player, string value, out List<SpellData> output)
    {
      List<SpellData> result = null;
      if (LandsOnYou.TryGetValue(value, out output))
      {
        result = FindByLandsOn(player, output);
      }
      return result;
    }

    internal bool IsKnownNpc(string npc)
    {
      bool found = false;
      if (!string.IsNullOrEmpty(npc))
      {
        found = AllNpcs.ContainsKey(npc.ToLower(CultureInfo.CurrentCulture));
      }
      return found; 
    }

    private SpellData FindPreviousCast(string player, List<SpellData> output)
    {
      if (LastSpellIndex > -1)
      {
        var endTime = AllSpellCastBlocks[LastSpellIndex].BeginTime - 5;
        for (int i = LastSpellIndex; i >= 0 && AllSpellCastBlocks[i].BeginTime >= endTime; i--)
        {
          for (int j = AllSpellCastBlocks[i].Actions.Count - 1; j >= 0; j--)
          {         
            if (AllSpellCastBlocks[i].Actions[j] is SpellCast cast && output.Find(spellData => spellData == cast.SpellData) is SpellData found)
            {
              if (found.Target != (int)SpellTarget.SELF || cast.Caster == player)
              {
                return cast.SpellData;
              }
            }
          }
        }
      }

      return null;
    }

    private List<SpellData> FindByLandsOn(string player, List<SpellData> output)
    {
      List<SpellData> result = null;

      if (output.Count == 1)
      {
        result = output;
      }
      else if (output.Count > 1)
      {
        var foundSpellData = FindPreviousCast(player, output);
        if (foundSpellData == null)
        {
          // one more thing, if all the abbrviations look the same then we know the spell
          // even if the version is wrong. grab the newest
          if (output.Distinct(AbbrvComparer).Count() == 1)
          {
            result = new List<SpellData> { output.First() };
          }
          else
          {
            result = output;
          }
        }
        else
        {
          result = new List<SpellData> { foundSpellData };
        }
      }

      return result;
    }

    internal bool RemoveActiveFight(string name)
    {
      bool removed = ActiveFights.TryRemove(name, out Fight fight);

      if (removed)
      {
        EventsNewInactiveFight?.Invoke(this, fight);
      }

      return removed;
    }

    internal void UpdateIfNewFightMap(string name, Fight fight)
    {
      if (!LifetimeFights.ContainsKey(name))
      {
        LifetimeFights[name] = 1;
      }

      if (!ActiveFights.ContainsKey(name))
      {
        ActiveFights[name] = fight;
        EventsNewFight?.Invoke(this, fight);
      }
      else
      {
        EventsRefreshFight?.Invoke(this, fight);
      }
    }

    internal void Clear()
    {
      lock (this)
      {
        LastSpellIndex = -1;
        ActiveFights.Clear();
        LifetimeFights.Clear();
        AllDeathBlocks.Clear();
        AllMiscBlocks.Clear();
        AllSpellCastBlocks.Clear();
        AllReceivedSpellBlocks.Clear();
        AllResistBlocks.Clear();
        AllHealBlocks.Clear();
        AllLootBlocks.Clear();
        AllSpecialActions.Clear();
        EventsClearedActiveData?.Invoke(this, true);
      }
    }

    internal static bool ResolveSpellAmbiguity(ReceivedSpell spell, out SpellData replaced)
    {
      replaced = null;

      if (spell.Ambiguity.Count < 30)
      {
        int spellClass = (int)PlayerManager.Instance.GetPlayerClassEnum(spell.Receiver);
        var subset = spell.Ambiguity.FindAll(test => test.Target == (int)SpellTarget.SELF && spellClass != 0 && (test.ClassMask & spellClass) == spellClass);
        var distinct = subset.Distinct(AbbrvComparer).ToList();
        replaced = distinct.Count == 1 ? distinct.First() : spell.Ambiguity.First();
      }

      return replaced != null;
    }

    private void RemoveFight(string name)
    {
      bool removed = ActiveFights.TryRemove(name, out Fight npc);
      removed = LifetimeFights.TryRemove(name, out byte bnpc) || removed;

      if (removed)
      {
        EventsRemovedFight?.Invoke(this, name);
      }
    }

    private static List<ActionBlock> SearchActions(List<ActionBlock> allActions, double beginTime, double endTime)
    {
      ActionBlock startBlock = new ActionBlock() { BeginTime = beginTime };
      ActionBlock endBlock = new ActionBlock() { BeginTime = endTime + 1 };

      int startIndex = allActions.BinarySearch(startBlock, TAComparer);
      if (startIndex < 0)
      {
        startIndex = Math.Abs(startIndex) - 1;
      }

      int endIndex = allActions.BinarySearch(endBlock, TAComparer);
      if (endIndex < 0)
      {
        endIndex = Math.Abs(endIndex) - 1;
      }

      int last = endIndex - startIndex;
      return last > 0 ? allActions.GetRange(startIndex, last) : new List<ActionBlock>();
    }

    private class SpellAbbrvComparer : IEqualityComparer<SpellData>
    {
      public bool Equals(SpellData x, SpellData y)
      {
        return x.NameAbbrv == y.NameAbbrv;
      }

      public int GetHashCode(SpellData obj)
      {
        return obj.NameAbbrv.GetHashCode();
      }
    }

    private class TimedActionComparer : IComparer<TimedAction>
    {
      public int Compare(TimedAction x, TimedAction y)
      {
        return x.BeginTime.CompareTo(y.BeginTime);
      }
    }
  }
}