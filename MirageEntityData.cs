using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using ExileCore.PoEMemory.MemoryObjects;
using GameOffsets.Native;
using Newtonsoft.Json;

namespace WhatsAMirage;

public enum ChestTier
{
    Unknown,
    Bronze,
    Silver,
    Gold
}

public class MirageSpawnerData
{
    public Entity Entity { get; set; }

    /// <summary>
    /// From StateMachine "activated" state: 1 = dormant, anything else = triggered/spent.
    /// </summary>
    public bool IsActivated { get; set; }

    /// <summary>
    /// Cached grid position so arrow targeting works when entity is out of range.
    /// Updated every tick while the entity is valid.
    /// </summary>
    public System.Numerics.Vector2 LastKnownGridPos { get; set; }

    public MirageSpawnerData(Entity entity)
    {
        Entity = entity;
        IsActivated = false;
        LastKnownGridPos = entity.GridPosNum;
    }
}

public class MirageChestData
{
    private static readonly Regex TierRegex = new(@"MinorCache(Bronze|Silver|Gold)(\w+)", RegexOptions.Compiled);

    public Entity Entity { get; }
    public ChestTier Tier { get; }
    public string ItemType { get; }
    public bool IsOpened { get; set; }

    public MirageChestData(Entity entity)
    {
        Entity = entity;
        IsOpened = false;

        var match = TierRegex.Match(entity.Path);
        if (match.Success)
        {
            Tier = match.Groups[1].Value switch
            {
                "Bronze" => ChestTier.Bronze,
                "Silver" => ChestTier.Silver,
                "Gold" => ChestTier.Gold,
                _ => ChestTier.Unknown
            };
            ItemType = match.Groups[2].Value;
        }
        else
        {
            Tier = ChestTier.Unknown;
            ItemType = "Unknown";
        }
    }
}

public class MirageChainAnchorData
{
    public Entity Entity { get; }
    public bool IsCompleted { get; set; }
    public List<Vector2i> Path { get; set; }
    public CancellationTokenSource PathCts { get; set; }

    public MirageChainAnchorData(Entity entity)
    {
        Entity = entity;
        IsCompleted = false;
    }
}

public class MirageMonsterData
{
    public Entity Entity { get; }
    public bool IsInvulnerable { get; set; }
    public bool IsRareLeader { get; set; }

    public MirageMonsterData(Entity entity)
    {
        Entity = entity;
        IsInvulnerable = false;
        IsRareLeader = entity.Rarity == ExileCore.Shared.Enums.MonsterRarity.Rare;
    }
}

public class VarashtaData
{
    public Entity Entity { get; set; }
    public long EmergeState { get; set; }
    public bool IsReady => EmergeState == 2;
    public bool IsSpent => EmergeState == 3;

    public VarashtaData(Entity entity)
    {
        Entity = entity;
    }
}

public class DjinnPortalData
{
    public Entity Entity { get; set; }

    public DjinnPortalData(Entity entity)
    {
        Entity = entity;
    }
}

public class FaridunInitiatorData
{
    public Entity Entity { get; set; }
    public bool IsActivated { get; set; }
    public System.Numerics.Vector2 LastKnownGridPos { get; set; }

    public FaridunInitiatorData(Entity entity)
    {
        Entity = entity;
        LastKnownGridPos = entity.GridPosNum;
    }
}

// Knowledge base DTOs
public class MirageKnowledgeBase
{
    [JsonProperty("version")] public string Version { get; set; }
    [JsonProperty("lastUpdated")] public string LastUpdated { get; set; }
    [JsonProperty("status")] public string Status { get; set; }
    [JsonProperty("wishes")] public Dictionary<string, WishData> Wishes { get; set; }
    [JsonProperty("tierColors")] public Dictionary<string, string> TierColors { get; set; }
}

public class WishData
{
    [JsonProperty("tier")] public string Tier { get; set; }
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("effect")] public string Effect { get; set; }
    [JsonProperty("notes")] public string Notes { get; set; }
}
