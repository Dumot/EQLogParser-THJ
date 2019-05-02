﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
  class DamageLineParser
  {
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public static event EventHandler<DamageProcessedEvent> EventsDamageProcessed;
    public static event EventHandler<ResistProcessedEvent> EventsResistProcessed;
    public static event EventHandler<string> EventsLineProcessed;

    private enum ParseType { HASTAKEN, YOUHAVETAKEN, POINTSOF, UNKNOWN };
    private static readonly DateUtil DateUtil = new DateUtil();
    private static readonly Regex CheckEye = new Regex(@"^Eye of (\w+)", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Dictionary<string, string> SpellTypeCache = new Dictionary<string, string>();

    private static readonly Dictionary<string, byte> HitMap = new Dictionary<string, byte>()
    {
      { "bash", 1 }, { "bit", 1 }, { "backstab", 1 }, { "claw", 1 }, { "crush", 1 }, { "frenzies", 1 },
      { "frenzy", 1 }, { "gore", 1 }, { "hit", 1 }, { "kick", 1 }, { "maul", 1 }, { "punch", 1 },
      { "pierce", 1 }, { "rend", 1 }, { "shoot", 1 }, { "slash", 1 }, { "slam", 1 }, { "slice", 1 },
      { "smash", 1 }, { "sting", 1 }, { "strike", 1 }, { "bashes", 1 }, { "bites", 1 }, { "backstabs", 1 },
      { "claws", 1 }, { "crushes", 1 }, { "gores", 1 }, { "hits", 1 }, { "kicks", 1 }, { "mauls", 1 },
      { "punches", 1 }, { "pierces", 1 }, { "rends", 1 }, { "shoots", 1 }, { "slashes", 1 }, { "slams", 1 },
      { "slices", 1 }, { "smashes", 1 }, { "stings", 1 }, { "strikes", 1 }, { "learn", 1 }, { "learns", 1 },
      { "sweep", 1 }, { "sweeps", 1 }
    };

    private static readonly Dictionary<string, string> HitAdditionalMap = new Dictionary<string, string>()
    {
      { "frenzies", "frenzies on" }, { "frenzy", "frenzy on" }
    };

    public static void Process(string line)
    {
      try
      {
        int index;
        if (line.Length >= 40 && line.IndexOf(" damage", Parsing.ACTIONINDEX + 13, StringComparison.Ordinal) > -1)
        {
          ProcessLine pline = new ProcessLine() { Line = line, ActionPart = line.Substring(Parsing.ACTIONINDEX) };
          pline.TimeString = pline.Line.Substring(1, 24);
          pline.CurrentTime = DateUtil.ParseDate(pline.TimeString, out double precise);

          DamageRecord record = ParseDamage(pline, out bool isPlayerDamage);
          if (record != null)
          {
            DamageProcessedEvent e = new DamageProcessedEvent() { Record = record, IsPlayerDamage = isPlayerDamage, TimeString = pline.TimeString, BeginTime = pline.CurrentTime };
            EventsDamageProcessed(record, e);
          }
        }
        else if (line.Length < 102 && (index = line.IndexOf(" slain ", Parsing.ACTIONINDEX, StringComparison.Ordinal)) > -1)
        {
          ProcessLine pline = new ProcessLine() { Line = line, ActionPart = line.Substring(Parsing.ACTIONINDEX) };
          pline.OptionalIndex = index - Parsing.ACTIONINDEX;
          pline.TimeString = pline.Line.Substring(1, 24);
          pline.CurrentTime = DateUtil.ParseDate(pline.TimeString, out double precise);
          HandleSlain(pline);
        }
        else if (line.Length >= 40 && line.Length < 110 && (index = line.IndexOf(" resisted your ", Parsing.ACTIONINDEX, StringComparison.Ordinal)) > -1)
        {
          ProcessLine pline = new ProcessLine() { Line = line, ActionPart = line.Substring(Parsing.ACTIONINDEX) };
          pline.OptionalIndex = index - Parsing.ACTIONINDEX;
          pline.TimeString = pline.Line.Substring(1, 24);
          pline.CurrentTime = DateUtil.ParseDate(pline.TimeString, out double precise);
          HandleResist(pline);
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

      EventsLineProcessed(line, line);
    }

    private static void HandleSlain(ProcessLine pline)
    {
      string test = null;
      if (pline.ActionPart.Length > 16 && pline.ActionPart.StartsWith("You have slain ", StringComparison.Ordinal) && pline.ActionPart[pline.ActionPart.Length-1] == '!')
      {
        test = pline.ActionPart.Substring(15, pline.ActionPart.Length - 15 - 1);
      }
      else if (pline.OptionalIndex > 9)
      {
        test = pline.ActionPart.Substring(0, pline.OptionalIndex - 9);
      }

      // Gotcharms has been slain by an animated mephit!
      if (test != null && test.Length > 0)
      {
        if (DataManager.Instance.CheckNameForPlayer(test) || DataManager.Instance.CheckNameForPet(test))
        {
          int byIndex = pline.ActionPart.IndexOf(" by ", StringComparison.Ordinal);
          if (byIndex > -1)
          {
            DataManager.Instance.AddPlayerDeath(test, pline.ActionPart.Substring(byIndex + 4), pline.CurrentTime);
          }
        }
        else if (!DataManager.Instance.RemoveActiveNonPlayer(test) && char.IsUpper(test[0]))
        {
          DataManager.Instance.RemoveActiveNonPlayer(char.ToLower(test[0], CultureInfo.CurrentCulture) + test.Substring(1));
        }
      }
    }

    private static void HandleResist(ProcessLine pline)
    {
      // [Mon Feb 11 20:00:28 2019] An inferno flare resisted your Frostreave Strike III!
      string defender = pline.ActionPart.Substring(0, pline.OptionalIndex);
      string spell = pline.ActionPart.Substring(pline.OptionalIndex + 15, pline.ActionPart.Length - pline.OptionalIndex - 15 - 1);

      ResistRecord record = new ResistRecord() { Spell = spell };
      ResistProcessedEvent e = new ResistProcessedEvent() { Record = record, BeginTime = pline.CurrentTime };
      EventsResistProcessed(defender, e);
    }

    private static DamageRecord ParseDamage(ProcessLine pline, out bool isPlayerDamage)
    {
      isPlayerDamage = true;

      DamageRecord record = ParseAllDamage(pline);
      if (record != null)
      {
        if (CheckEye.IsMatch(record.Defender) || record.Defender.EndsWith("chest", StringComparison.Ordinal) || record.Defender.EndsWith("satchel", StringComparison.Ordinal))
        {
          record = null;
        }
        else
        {
          // Needed to replace 'You' and 'you', etc
          record.Attacker = DataManager.Instance.ReplacePlayer(record.Attacker, record.Defender, out bool replaced);
          record.Defender = DataManager.Instance.ReplacePlayer(record.Defender, record.Attacker, out _);

          if (record.Attacker == record.Defender)
          {
            record = null;
          }
          else
          {
            CheckDamageRecordForPet(record, replaced, out bool isDefenderPet, out bool isAttackerPet);
            CheckDamageRecordForPlayer(record, replaced, out bool isDefenderPlayer, out bool isAttackerPlayer);

            if (isDefenderPlayer || isDefenderPet)
            {
              if (!isAttackerPlayer && !isAttackerPet)
              {
                DataManager.Instance.UpdateProbablyNotAPlayer(record.Attacker);
              }

              // main spot where attacker is not a player or pet
              isPlayerDamage = false;
            }

            if ((isAttackerPlayer || isAttackerPet) && !isDefenderPlayer && !isDefenderPet)
            {
              DataManager.Instance.UpdateProbablyNotAPlayer(record.Defender);
            }
            else if (!isAttackerPlayer && !isAttackerPet && !isDefenderPlayer && !isDefenderPet)
            {
              var isDefenderProbablyNotAPlayer = DataManager.Instance.UpdateProbablyNotAPlayer(record.Defender, false);
              var isAttackerProbablyNotAPlayer = DataManager.Instance.UpdateProbablyNotAPlayer(record.Attacker, false);

              if (isDefenderProbablyNotAPlayer && !isAttackerProbablyNotAPlayer)
              {
                DataManager.Instance.UpdateUnVerifiedPetOrPlayer(record.Attacker);
              }
              else if (!isDefenderProbablyNotAPlayer && isAttackerProbablyNotAPlayer)
              {
                DataManager.Instance.UpdateUnVerifiedPetOrPlayer(record.Defender);
                isPlayerDamage = false;
              }
            }
          }
        }
      }

      return record;
    }

    private static void CheckDamageRecordForPet(DamageRecord record, bool replacedAttacker, out bool isDefenderPet, out bool isAttackerPet)
    {
      isAttackerPet = false;

      if (!replacedAttacker)
      {
        if (!string.IsNullOrEmpty(record.AttackerOwner))
        {
          DataManager.Instance.UpdateVerifiedPets(record.Attacker);
          isAttackerPet = true;
        }
        else
        {
          isAttackerPet = DataManager.Instance.CheckNameForPet(record.Attacker);

          if (isAttackerPet)
          {
            record.AttackerOwner = Labels.UNASSIGNED;
          }
        }
      }

      if (!string.IsNullOrEmpty(record.DefenderOwner))
      {
        DataManager.Instance.UpdateVerifiedPets(record.Defender);
        isDefenderPet = true;
      }
      else
      {
        isDefenderPet = DataManager.Instance.CheckNameForPet(record.Defender);
      }
    }

    private static void CheckDamageRecordForPlayer(DamageRecord record, bool replacedAttacker, out bool isDefenderPlayer, out bool isAttackerPlayer)
    {
      isAttackerPlayer = false;

      if (!replacedAttacker)
      {
        if (!string.IsNullOrEmpty(record.AttackerOwner))
        {
          DataManager.Instance.UpdateVerifiedPlayers(record.AttackerOwner);
          isAttackerPlayer = true;
        }

        if (!string.IsNullOrEmpty(record.DefenderOwner))
        {
          DataManager.Instance.UpdateVerifiedPlayers(record.DefenderOwner);
        }
      }

      isDefenderPlayer = string.IsNullOrEmpty(record.DefenderOwner) && DataManager.Instance.CheckNameForPlayer(record.Defender);
    }

    private static DamageRecord ParseAllDamage(ProcessLine pline)
    {
      DamageRecord record = null;
      string part = pline.ActionPart;
      ParseType parseType = ParseType.UNKNOWN;

      int modifiersIndex = -1;
      if (part[part.Length - 1] == ')')
      {
        // using 4 here since the shortest modifier should at least be 3 even in the future. probably.
        modifiersIndex = part.LastIndexOf('(', part.Length - 4);
        if (modifiersIndex > -1)
        {
          part = part.Substring(0, modifiersIndex);
        }
      }

      int pointsIndex = -1;
      int forIndex = -1;
      int fromIndex = -1;
      int byIndex = -1;
      int takenIndex = -1;
      int hitIndex = -1;
      int extraIndex = -1;
      int isAreIndex = -1;
      bool nonMelee = false;

      List<string> nameList = new List<string>();
      StringBuilder builder = new StringBuilder();
      var data = part.Split(' ');

      for (int i=0; i<data.Length; i++)
      {
        switch(data[i])
        {
          case "taken":
            takenIndex = i;

            int test1 = i - 1;
            if (test1 > 0 && data[test1] == "has")
            {
              parseType = ParseType.HASTAKEN;

              int test2 = i + 2;
              if (data.Length > test2 && data[test2] == "extra" && data[test2 - 1] == "an")
              {
                extraIndex = test2;
              }
            }
            else if (test1 >= 1 && data[test1] == "have" && data[test1-1] == "You")
            {
              parseType = ParseType.YOUHAVETAKEN;
            }
            break;
          case "by":
            byIndex = i;
            break;
          case "non-melee":
            nonMelee = true;
            break;
          case "is":
          case "are":
            isAreIndex = i;
            break;
          case "for":
            int next = i + 1;
            if (data.Length > next && data[next].Length > 0 && char.IsNumber(data[next][0]))
            {
              forIndex = i;
            }
            break;
          case "from":
            fromIndex = i;
            break;
          case "points":
            int ofIndex = i + 1;
            if (ofIndex < data.Length && data[ofIndex] == "of")
            {
              parseType = ParseType.POINTSOF;
              pointsIndex = i;
            }
            break;
          default:
            if (HitMap.ContainsKey(data[i]))
            {
              hitIndex = i;
            }
            break;
        }
      }

      if (parseType == ParseType.POINTSOF && forIndex > -1 && forIndex < pointsIndex && hitIndex > -1)
      {
        record = ParsePointsOf(data, nonMelee, forIndex, byIndex, hitIndex, builder, nameList);
      }
      else if (parseType == ParseType.HASTAKEN && takenIndex < fromIndex && fromIndex > -1)
      {
        record = ParseHasTaken(data, takenIndex, fromIndex, byIndex, builder);
      }
      else if (parseType == ParseType.POINTSOF && extraIndex > -1 && takenIndex > -1 && takenIndex < fromIndex)
      {
        record = ParseExtra(data, takenIndex, extraIndex, fromIndex, nameList);
      }
      // there are more messages without a specificied attacker or spell but do these first
      else if (parseType == ParseType.YOUHAVETAKEN && takenIndex > -1 && fromIndex > -1 && byIndex > fromIndex)
      {
        record = ParseYouHaveTaken(data, takenIndex, fromIndex, byIndex, builder);
      }
      else if (parseType == ParseType.POINTSOF && isAreIndex > -1 && byIndex > isAreIndex && forIndex > byIndex)
      {
        record = ParseDS(data, isAreIndex, byIndex, forIndex);
      }

      if (record != null && modifiersIndex > -1)
      {
        record.ModifiersMask = LineModifiersParser.Parse(pline.ActionPart.Substring(modifiersIndex + 1, pline.ActionPart.Length - 1 - modifiersIndex - 1));
      }

      return record;
    }

    private static DamageRecord ParseDS(string[] data, int isAreIndex, int byIndex, int forIndex)
    {
      DamageRecord record = null;

      string defender = string.Join(" ", data, 0, isAreIndex);
      uint damage = StatsUtil.ParseUInt(data[forIndex + 1]);

      string attacker;
      if (data[byIndex + 1] == "YOUR")
      {
        attacker = "you";
      }
      else
      {
        attacker = string.Join(" ", data, byIndex + 1, forIndex - byIndex - 2);
        attacker = attacker.Substring(0, attacker.Length - 2);
      }

      // check for pets
      HasOwner(attacker, out string attackerOwner);
      HasOwner(defender, out string defenderOwner);

      if (attacker != null && defender != null)
      {
        record = BuildRecord(attacker, defender, damage, attackerOwner, defenderOwner, null, Labels.DS);
      }

      return record;
    }

    private static DamageRecord ParseYouHaveTaken(string[] data, int takenIndex, int fromIndex, int byIndex, StringBuilder builder)
    {
      DamageRecord record = null;

      string defender = "you";
      string attacker = ReadStringToPeriod(data, byIndex, builder);
      string spell = string.Join(" ", data, fromIndex + 1, byIndex - fromIndex - 1);
      uint damage = StatsUtil.ParseUInt(data[takenIndex + 1]);

      // check for pets
      HasOwner(attacker, out string attackerOwner);

      if (attacker != null && defender != null)
      {
        record = BuildRecord(attacker, defender, damage, attackerOwner, null, spell, Labels.DD);
      }

      return record;
    }

    private static DamageRecord ParseExtra(string[] data, int takenIndex, int extraIndex, int fromIndex, List<string> nameList)
    {
      DamageRecord record = null;

      uint damage = StatsUtil.ParseUInt(data[extraIndex + 1]);
      string defender = string.Join(" ", data, 0, takenIndex - 1);

      string attacker = null;
      string spell = null;

      if (data.Length > fromIndex + 1)
      {
        int person = fromIndex + 1;
        if (data[person] == "your")
        {
          attacker = "you";
        }
        else
        {
          int len = data[person].Length;
          if (len > 2 && data[person][len-2] == '\'' && data[person][len-1] == 's')
          {
            attacker = data[person].Substring(0, len - 2);
          }
        }

        if (attacker != null)
        {
          nameList.Clear();
          for (int i = person + 1; i < data.Length; i++)
          {
            if (data[i] == "spell.")
            {
              break;
            }
            else
            {
              nameList.Add(data[i]);
            }
          }

          spell = string.Join(" ", nameList);
        }
      }

      // check for pets
      HasOwner(attacker, out string attackerOwner);
      HasOwner(defender, out string defenderOwner);

      if (attacker != null && defender != null)
      {
        record = BuildRecord(attacker, defender, damage, attackerOwner, defenderOwner, spell, Labels.BANE);
      }

      return record;
    }

    private static DamageRecord ParseHasTaken(string[] data, int takenIndex, int fromIndex, int byIndex, StringBuilder builder)
    {
      DamageRecord record = null;

      uint damage = StatsUtil.ParseUInt(data[takenIndex + 1]);
      string defender = string.Join(" ", data, 0, takenIndex - 1);

      string spell = null;
      string attacker = null;
      if (byIndex > -1 && fromIndex < byIndex)
      {
        spell = string.Join(" ", data, fromIndex + 1, byIndex - fromIndex - 1);
        attacker = ReadStringToPeriod(data, byIndex, builder);
      }
      else if (data[fromIndex + 1] == "your")
      {
        spell = ReadStringToPeriod(data, fromIndex + 1, builder);
        attacker = "you";
      }

      if (attacker != null && spell != null)
      {
        // check for pets
        HasOwner(attacker, out string attackerOwner);
        HasOwner(defender, out string defenderOwner);

        if (attacker != null && defender != null)
        {
          record = BuildRecord(attacker, defender, damage, attackerOwner, defenderOwner, spell, GetTypeFromSpell(spell, Labels.DOT));
        }
      }

      return record;
    }

    private static DamageRecord ParsePointsOf(string[] data, bool isNonMelee, int forIndex, int byIndex, int hitIndex, StringBuilder builder, List<string> nameList)
    {
      DamageRecord record = null;
      uint damage = StatsUtil.ParseUInt(data[forIndex + 1]);
      string spell = null;
      string attacker = null;

      if (byIndex > 1)
      {
        spell = ReadStringToPeriod(data, byIndex, builder);
      }

      // before hit
      nameList.Clear();
      for (int i = hitIndex - 1; i >= 0; i--)
      {
        if (data[hitIndex].EndsWith(".", StringComparison.Ordinal))
        {
          break;
        }
        else
        {
          nameList.Insert(0, data[i]);
        }
      }

      if (nameList.Count > 0)
      {
        attacker = string.Join(" ", nameList);
      }

      string hit = data[hitIndex];
      if (HitAdditionalMap.ContainsKey(hit))
      {
        hit = HitAdditionalMap[hit];
        hitIndex++; // multi-word hit value
      }

      string defender = string.Join(" ", data, hitIndex + 1, forIndex - hitIndex - 1);

      // check for pets
      HasOwner(attacker, out string attackerOwner);
      HasOwner(defender, out string defenderOwner);

      // some new special cases
      if (!string.IsNullOrEmpty(spell) && spell.StartsWith("Elemental Conversion", StringComparison.Ordinal))
      {
        DataManager.Instance.UpdateVerifiedPets(defender);
      }
      else if (attacker != null && defender != null)
      {
        if ((isNonMelee || !string.IsNullOrEmpty(spell)) && hit.StartsWith("hit", StringComparison.Ordinal))
        {
          hit = GetTypeFromSpell(spell, Labels.DD);
        }
        else
        {
          hit = char.ToUpper(hit[0], CultureInfo.CurrentCulture) + hit.Substring(1);
        }

        record = BuildRecord(attacker, defender, damage, attackerOwner, defenderOwner, spell, hit);
      }

      return record;
    }

    private static DamageRecord BuildRecord(string attacker, string defender, uint damage, string attackerOwner, string defenderOwner, string spell, string type)
    {
      if (attacker.EndsWith("'s corpse", StringComparison.Ordinal))
      {
        attacker = attacker.Substring(0, attacker.Length - 9);
      }

      DamageRecord record = new DamageRecord()
      {
        Attacker = string.Intern(FixName(attacker)),
        Defender = string.Intern(FixName(defender)),
        Type = string.Intern(type),
        Total = damage,
        ModifiersMask = -1
      };

      if (attackerOwner != null)
      {
        record.AttackerOwner = string.Intern(attackerOwner);
      }

      if (defenderOwner != null)
      {
        record.DefenderOwner = string.Intern(defenderOwner);
      }

      // set sub type if spell is available
      record.SubType = string.IsNullOrEmpty(spell) ? record.Type : string.Intern(spell);
      return record;
    }

    private static string ReadStringToPeriod(string[] data, int byIndex, StringBuilder builder)
    {
      string result = null;

      if (byIndex > 1)
      {
        builder.Clear();
        for (int i = byIndex + 1; i < data.Length; i++)
        {
          int len = data[i].Length;
          builder.Append(data[i]);
          if ((len >= 1 && data[i][len-1] == '.') && !(len >= 3 && data[i][len - 2] == 'k' && data[i][len - 3] == 'R'))
          {
            builder.Remove(builder.Length - 1, 1);
            break;
          }
          else
          {
            builder.Append(" ");
          }
        }

        result = builder.ToString();
      }

      return result;
    }

    private static bool HasOwner(string name, out string owner)
    {
      bool hasOwner = false;
      owner = null;

      if (!string.IsNullOrEmpty(name))
      {
        int posessiveIndex = name.IndexOf("`s ", StringComparison.Ordinal);
        if (posessiveIndex > -1)
        {
          if (IsPetOrMount(name, posessiveIndex + 3, out _))
          {
            if (Helpers.IsPossiblePlayerName(name, posessiveIndex))
            {
              owner = name.Substring(0, posessiveIndex);
              hasOwner = true;
            }
          }
        }
      }

      return hasOwner;
    }

    private static string FixName(string name)
    {
      string result;
      if (name.Length >= 2 && name[0] == 'A' && name[1] == ' ')
      {
        result = "a " + name.Substring(2);
      }
      else if (name.Length >= 3 && name[0] == 'A' && name[1] == 'n' && name[2] == ' ')
      {
        result = "an " + name.Substring(3);
      }
      else
      {
        result = name;
      }

      return result;
    }

    private static string GetTypeFromSpell(string name, string type)
    {
      if (!SpellTypeCache.TryGetValue(name, out string result))
      {
        string spellName = Helpers.AbbreviateSpellName(name);
        SpellData data = DataManager.Instance.GetSpellByAbbrv(spellName);
        result = (data != null && data.IsProc) ? Labels.PROC : type;
        SpellTypeCache[name] = result;
      }

      return result;
    }

    private static bool IsPetOrMount(string part, int start, out int len)
    {
      bool found = false;
      len = -1;

      int end = 2;
      if (part.Length >= (start + ++end) && part.Substring(start, 3) == "pet" ||
        part.Length >= (start + ++end) && part.Substring(start, 4) == "ward" && !(part.Length > (start + 5) && part[start + 5] != 'e') ||
        part.Length >= (start + ++end) && part.Substring(start, 5) == "Mount" ||
        part.Length >= (start + ++end) && part.Substring(start, 6) == "warder")
      {
        found = true;
        len = end;
      }
      return found;
    }
  }
}
