﻿using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace EQLogParser
{
  public enum SpellClasses
  {
    WAR = 1, CLR = 2, PAL = 4, RNG = 8, SHD = 16, DRU = 32, MNK = 64, BRD = 128, ROG = 256,
    SHM = 512, NEC = 1024, WIZ = 2048, MAG = 4096, ENC = 8192, BST = 16384, BER = 32768
  }

  public enum SpellTarget
  {
    LOS = 1, CASTER_AE = 2, CASTER_GROUP = 3, CASTER_PB = 4, SINGLE = 5, SELF = 6, TARGET_AE = 8,
    NEARBY_PLAYERS_AE = 40, DIRECTION_AE = 42, TARGET_RING_AE = 45
  }

  public static class Labels
  {
    public const string DD_NAME = "Direct Damage";
    public const string DOT_NAME = "DoT Tick";
    public const string DS_NAME = "Damage Shield";
    public const string BANE_NAME = "Bane Damage";
    public const string PROC_NAME = "Proc";
    public const string RESIST_NAME = "Resisted Spells";
    public const string HOT_NAME = "HoT Tick";
    public const string HEAL_NAME = "Heal";
    public const string NO_DATA = "No Data Available";
    public const string UNASSIGNED_PET_OWNER = "Unknown Pet Owner";
    public const string UNKNOWN_SPELL = "Unknown Spell";
    public const string UNKNOWN_PLAYER = "Unknown Player";
    public const string RAID_PLAYER = "Totals";
    public const string HEAL_PARSE = "Healing";
    public const string DAMAGE_PARSE = "Damage";
  }

  public static class ChatChannels
  {
    public const string AUCTION = "Auction";
    public const string SAY = "Say";
    public const string GUILD = "Guild";
    public const string FELLOWSHIP = "Fellowship";
    public const string TELL = "Tell";
    public const string SHOUT = "Shout";
    public const string GROUP = "Group";
    public const string RAID = "Raid";
    public const string OOC = "OOC";
  }

  public interface ISummaryBuilder
  {
    StatsSummary BuildSummary(CombinedStats currentStats, List<PlayerStats> selected, bool showTotals, bool rankPlayers);
  }

  public interface IAction { }

  public class ColorItem
  {
    public SolidColorBrush Brush { get; set; }
    public string Name { get; set; }
  }

  public class CheckedItem
  {
    public string Text { get; set; }
    public string SelectedText { get; set; }
    public bool IsChecked { get; set; }
  }

  public class ChatType
  {
    public string Channel { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public bool SenderIsYou { get; set; }
    public bool ReceiverIsYou { get; set; }
    public string Line { get; set; }
    public int KeywordStart { get; set; }
  }

  public class ParseData
  {
    public CombinedStats CombinedStats { get; set; }
    public ISummaryBuilder Builder { get; set; }
    public List<PlayerStats> Selected { get; set; }
  }

  public class TimedAction : IAction
  {
    public double BeginTime { get; set; }
  }

  public class FullTimedAction : TimedAction
  {
    public double LastTime { get; set; }
  }

  public class PlayerStatsSelectionChangedEvent : EventArgs
  {
    public List<PlayerStats> Selected { get; set; }
  }

  public class DamageProcessedEvent : TimedAction
  {
    public DamageRecord Record { get; set; }
    public string TimeString { get; set; }
    public bool IsPlayerDamage { get; set; }
  }

  public class HealProcessedEvent : TimedAction
  {
    public HealRecord Record { get; set; }
  }

  public class ResistProcessedEvent : TimedAction
  {
    public ResistRecord Record { get; set; }
  }

  public class DataPointEvent
  {
    public string Action { get; set; }
    public RecordGroupIterator Iterator { get; set; }
    public List<PlayerStats> Selected { get; set; }
  }

  public class DamageStatsOptions
  {
    public string Name { get; set; }
    public List<NonPlayer> Npcs { get; set; }
    public bool IsBaneEanbled { get; set; }
    public bool RequestChartData { get; set; }
    public bool RequestSummaryData { get; set; }
  }

  public class HealingStatsOptions
  {
    public string Name { get; set; }
    public List<NonPlayer> Npcs { get; set; }
    public bool IsAEHealingEanbled { get; set; }
    public bool RequestChartData { get; set; }
    public bool RequestSummaryData { get; set; }
  }

  public class StatsGenerationEvent
  {
    public string Type { get; set; }
    public string State { get; set; }
    public CombinedStats CombinedStats { get; set; }
    public bool IsBaneAvailable { get; set; }
    public bool IsAEHealingAvailable { get; set; }
  }

  public class ChatLine : TimedAction
  {
    public string Line { get; set; }
  }

  public class ProcessLine
  {
    public string Line { get; set; }
    public double CurrentTime { get; set; }
    public string TimeString { get; set; }
    public string ActionPart { get; set; }
    public int OptionalIndex { get; set; }
    public string OptionalData { get; set; }
  }

  public class ResistRecord : IAction
  {
    public string Spell { get; set; }
  }

  public class HitRecord : IAction
  {
    public uint Total { get; set; }
    public uint OverTotal { get; set; }
    public string Type { get; set; }
    public string SubType { get; set; }
    public int ModifiersMask { get; set; }
  }

  public class HealRecord : HitRecord
  {
    public string Healer { get; set; }
    public string Healed { get; set; }
  }

  public class DamageRecord : HitRecord
  {
    public string Attacker { get; set; }
    public string AttackerOwner { get; set; }
    public string Defender { get; set; }
    public string DefenderOwner { get; set; }
  }

  public class ActionBlock : TimedAction
  {
    public List<IAction> Actions { get; set; }
  }

  public class Hit : FullTimedAction
  {
    public uint Max { get; set; }
    public uint BaneHits{ get; set; }
    public uint Hits { get; set; }
    public uint CritHits { get; set; }
    public uint LuckyHits { get; set; }
    public uint TwincastHits { get; set; }
    public long Total { get; set; }
    public long TotalCrit { get; set; }
    public long TotalLucky { get; set; }
    public Dictionary<long, int> CritFreqValues { get; set; }
    public Dictionary<long, int> NonCritFreqValues { get; set; }
  }

  public class NonPlayer : FullTimedAction
  {
    public const string BREAK_TIME = "Break Time";
    public string BeginTimeString { get; set; }
    public string Name { get; set; }
    public int ID { get; set; }
    public string CorrectMapKey {get; set;}
    public int GroupID { get; set; }
  }

  public class PetMapping
  {
    public string Owner { get; set; }
    public string Pet { get; set; }
  }

  public class SortableName
  {
    public string Name { get; set; }
  }

  public class PlayerDeath : IAction
  {
    public string Player { get; set; }
    public string Npc { get; set; }
  }

  public class ReceivedSpell : IAction
  {
    public string Receiver { get; set; }
    public SpellData SpellData { get; set; }
  }

  public class SpellCast : ReceivedSpell
  {
    public string Spell { get; set; }
    public string Caster { get; set; }
  }

  public class SpellData
  {
    public string ID { get; set; }
    public string Spell { get; set; }
    public string SpellAbbrv { get; set; }
    public bool Beneficial { get; set; }
    public byte Target { get; set; }
    public ushort ClassMask { get; set; }
    public string LandsOnYou { get; set; }
    public string LandsOnOther { get; set; }
    public bool Damaging { get; set; }
    public bool IsProc { get; set; }
  }

  public class SpellCountData
  {
    public Dictionary<string, Dictionary<string, uint>> PlayerCastCounts { get; set; }
    public Dictionary<string, Dictionary<string, uint>> PlayerReceivedCounts { get; set; }
    public Dictionary<string, uint> MaxCastCounts { get; set; }
    public Dictionary<string, uint> MaxReceivedCounts { get; set; }
    public Dictionary<string, SpellData> UniqueSpells { get; set; }
  }

  public class SpellCountRow
  {
    public string Spell { get; set; }
    public double[] Values { get; set; }
    public bool IsReceived { get; set; }
  }

  public class CombinedStats
  {
    public string TargetTitle { get; set; }
    public string TimeTitle { get; set; }
    public string TotalTitle { get; set; }
    public string FullTitle { get; set; }
    public string ShortTitle { get; set; }
    public List<PlayerStats> StatsList { get; set; }
    public PlayerStats RaidStats { get; set; }
  }

  public class CombinedDamageStats : CombinedStats
  {
    public Dictionary<string, List<PlayerStats>> Children { get; set; }
    public Dictionary<string, byte> UniqueClasses { get; set; }
  }

  public class CombinedHealStats : CombinedStats
  {
    public Dictionary<string, byte> UniqueClasses { get; set; }
  }

  public class OverlayDamageStats : CombinedStats
  {
    public Dictionary<string, PlayerStats> TopLevelStats { get; set; }
    public Dictionary<string, PlayerStats> AggregateStats { get; set; }
    public Dictionary<string, PlayerStats> IndividualStats { get; set; }
    public Dictionary<string, byte> UniqueNpcs { get; set; }
  }

  public class DataPoint
  {
    public long Avg { get; set; }
    public string Name { get; set; }
    public int ModifiersMask { get; set; }
    public long Total { get; set; }
    public long RollingTotal { get; set; }
    public uint RollingHits { get; set; }
    public uint RollingCritHits { get; set; }
    public long VPS { get; set; }
    public double CritRate { get; set; }
    public double BeginTime { get; set; }
    public double CurrentTime { get; set; }
  }

  public class HitFreqChartData
  {
    public string HitType { get; set; }
    public List<int> CritYValues { get; set; }
    public List<long> CritXValues { get; set; }
    public List<int> NonCritYValues { get; set; }
    public List<long> NonCritXValues { get; set; }
  }

  public class StatsSummary
  {
    public string Title { get; set; }
    public string RankedPlayers { get; set; }
  }

  public class PlayerSubStats : Hit
  {
    public ushort Rank { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public double TotalSeconds { get; set; }
    public long DPS { get; set; }
    public long SDPS { get; set; }
    public long Extra { get; set; }
    public uint Resists { get; set; }
    public long Avg { get; set; }
    public long AvgCrit { get; set; }
    public long AvgLucky { get; set; }
    public double CritRate { get; set; }
    public double ExtraRate { get; set; }
    public double LuckRate { get; set; }
    public double TwincastRate { get; set; }
    public double ResistRate { get; set; }
    public double Percent { get; set; }
    public double PercentOfRaid { get; set; }
    public uint Deaths { get; set; }
    public string ClassName { get; set; }
    public List<double> BeginTimes { get; set; }
    public List<double> LastTimes { get; set; }
    public List<double> TimeDiffs { get; set; }
  }

  public class PlayerStats : PlayerSubStats
  {
    public Dictionary<string, PlayerSubStats> SubStats { get; set; }
    public Dictionary<string, PlayerSubStats> SubStats2 { get; set; }
    public string OrigName { get; set; }
  }
}
