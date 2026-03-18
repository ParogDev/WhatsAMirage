using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhatsAMirage;

public class WhatsAMirageSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);

    public SpawnerSettings Spawners { get; set; } = new();
    public ChestSettings Chests { get; set; } = new();
    public MonsterSettings Monsters { get; set; } = new();
    public ChainAnchorSettings ChainAnchors { get; set; } = new();
    public ArrowSettings Arrow { get; set; } = new();
    public GeneralSettings General { get; set; } = new();
    public VarashtaSettings Varashta { get; set; } = new();
    public PortalSettings Portal { get; set; } = new();
    public WishAlertSettings WishAlert { get; set; } = new();
}

[Submenu(CollapsedByDefault = true)]
public class SpawnerSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ColorNode WorldCircleColor { get; set; } = new Color(0, 255, 255, 255);
    public RangeNode<int> WorldCircleRadius { get; set; } = new(80, 30, 200);
    public ToggleNode MinimapIcon { get; set; } = new(true);
    public RangeNode<int> MinimapIconSize { get; set; } = new(15, 5, 30);
}

[Submenu(CollapsedByDefault = true)]
public class ChestSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ColorNode BronzeColor { get; set; } = new Color(205, 127, 50, 255);
    public ColorNode SilverColor { get; set; } = new Color(192, 192, 192, 255);
    public ColorNode GoldColor { get; set; } = new Color(255, 215, 0, 255);
    public RangeNode<int> WorldCircleRadius { get; set; } = new(60, 20, 150);
    public ToggleNode MinimapIcon { get; set; } = new(true);
    public ToggleNode ShowTierLabel { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class MonsterSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ColorNode RareLeaderColor { get; set; } = Color.Orange;
    public ColorNode InvulnerableColor { get; set; } = Color.Gray;
    public ToggleNode PackCountOverlay { get; set; } = new(true);
    public ToggleNode MinimapIcon { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class ChainAnchorSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ColorNode PathColor { get; set; } = new Color(255, 100, 255, 255);
    public RangeNode<int> PathThickness { get; set; } = new(2, 1, 5);
    public RangeNode<int> DrawEveryNthSegment { get; set; } = new(3, 1, 10);
    public ToggleNode ShowOnMinimap { get; set; } = new(true);
    public ToggleNode ShowWorldCircle { get; set; } = new(true);
    public RangeNode<int> WorldCircleRadius { get; set; } = new(100, 30, 300);
    public ToggleNode FallbackArrow { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class ArrowSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ColorNode Color { get; set; } = new Color(0, 255, 255, 255);
    public RangeNode<int> Size { get; set; } = new(40, 20, 80);
    public RangeNode<int> DistanceFromCenter { get; set; } = new(120, 60, 300);
    public RangeNode<int> MaxRange { get; set; } = new(500, 100, 2000);
}

[Submenu(CollapsedByDefault = true)]
public class GeneralSettings
{
    public RangeNode<int> MaxDrawDistance { get; set; } = new(200, 50, 500);
}

[Submenu(CollapsedByDefault = true)]
public class VarashtaSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ColorNode ReadyColor { get; set; } = new Color(255, 215, 0, 255);
    public ColorNode SpentColor { get; set; } = new Color(128, 128, 128, 255);
    public RangeNode<int> WorldCircleRadius { get; set; } = new(80, 30, 200);
    public ToggleNode MinimapIcon { get; set; } = new(true);
    public RangeNode<int> MinimapIconSize { get; set; } = new(18, 5, 30);
}

[Submenu(CollapsedByDefault = true)]
public class PortalSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public ColorNode Color { get; set; } = new Color(138, 43, 226, 255);
    public RangeNode<int> WorldCircleRadius { get; set; } = new(80, 30, 200);
    public ToggleNode MinimapIcon { get; set; } = new(true);
    public RangeNode<int> MinimapIconSize { get; set; } = new(15, 5, 30);
}

[Submenu(CollapsedByDefault = true)]
public class WishAlertSettings
{
    public ToggleNode ShowAlert { get; set; } = new(true);
    public ColorNode AlertColor { get; set; } = new Color(255, 215, 0, 255);
    public ToggleNode ShowWishTierOverlay { get; set; } = new(true);
    public ToggleNode ShowWishTooltip { get; set; } = new(true);
    public ToggleNode HighlightFlaggedWishes { get; set; } = new(true);
    public ColorNode FlaggedWishColor { get; set; } = new Color(255, 215, 0, 255);
}
