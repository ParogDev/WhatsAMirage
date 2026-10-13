using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhatsAMirage;

public class WhatsAMirageSettings : ISettings
{
    [Menu("Enable", "Turn the whole plugin on or off")]
    public ToggleNode Enable { get; set; } = new(true);

    [Menu("Spawners", "Markers on untriggered Zarokh spawners")]
    public SpawnerSettings Spawners { get; set; } = new();

    [Menu("Chests", "Markers on unopened Mirage chests, colored by tier")]
    public ChestSettings Chests { get; set; } = new();

    [Menu("Monsters", "Markers on Mirage league monsters and rare pack leaders")]
    public MonsterSettings Monsters { get; set; } = new();

    [Menu("Chain Paths", "Paths to chain anchors (via the Radar bridge) and the fallback arrow")]
    public ChainAnchorSettings ChainAnchors { get; set; } = new();

    [Menu("Arrow", "Compass arrow pointing to the nearest untriggered spawner")]
    public ArrowSettings Arrow { get; set; } = new();

    [Menu("General", "Settings shared by all overlays")]
    public GeneralSettings General { get; set; } = new();

    [Menu("Varashta", "Marker on Varashta, ready or spent")]
    public VarashtaSettings Varashta { get; set; } = new();

    [Menu("Djinn Portals", "Markers on Djinn portals into the Mirage")]
    public PortalSettings Portal { get; set; } = new();

    [Menu("Wishes", "Wish-ready alert and wish card tier badges, tooltips and highlights")]
    public WishAlertSettings WishAlert { get; set; } = new();
}

[Submenu(CollapsedByDefault = true)]
public class SpawnerSettings
{
    [Menu("Show Spawners", "Draw world circles on untriggered Zarokh spawners")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu("Circle Color", "Color of the world circle around spawners")]
    public ColorNode WorldCircleColor { get; set; } = new Color(0, 255, 255, 255);

    [Menu("Circle Radius", "World circle radius, in world units (30-200)")]
    public RangeNode<int> WorldCircleRadius { get; set; } = new(80, 30, 200);

    [Menu("Map Icon", "Show spawner dots on the large map")]
    public ToggleNode MinimapIcon { get; set; } = new(true);

    [Menu("Map Icon Size", "Size of the spawner map dot, in px (5-30)")]
    public RangeNode<int> MinimapIconSize { get; set; } = new(15, 5, 30);
}

[Submenu(CollapsedByDefault = true)]
public class ChestSettings
{
    [Menu("Show Chests", "Draw world circles on unopened Mirage chests")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu("Bronze Color", "Color for bronze-tier chests")]
    public ColorNode BronzeColor { get; set; } = new Color(205, 127, 50, 255);

    [Menu("Silver Color", "Color for silver-tier chests")]
    public ColorNode SilverColor { get; set; } = new Color(192, 192, 192, 255);

    [Menu("Gold Color", "Color for gold-tier chests")]
    public ColorNode GoldColor { get; set; } = new Color(255, 215, 0, 255);

    [Menu("Circle Radius", "World circle radius for chests, in world units (20-150)")]
    public RangeNode<int> WorldCircleRadius { get; set; } = new(60, 20, 150);

    [Menu("Map Icon", "Show chest dots on the large map")]
    public ToggleNode MinimapIcon { get; set; } = new(true);

    [Menu("Tier Labels", "Show Bronze/Silver/Gold text above chests")]
    public ToggleNode ShowTierLabel { get; set; } = new(false);
}

[Submenu(CollapsedByDefault = true)]
public class MonsterSettings
{
    [Menu("Show Monsters", "Draw world circles on Mirage league monsters")]
    public ToggleNode Show { get; set; } = new(false);

    [Menu("Rare Leader Color", "Color for rare pack leaders and their pack count")]
    public ColorNode RareLeaderColor { get; set; } = Color.Orange;

    [Menu("Invulnerable Color", "Color for monsters that cannot be damaged yet")]
    public ColorNode InvulnerableColor { get; set; } = Color.Gray;

    [Menu("Pack Count", "Show how many pack monsters are still alive near each rare leader")]
    public ToggleNode PackCountOverlay { get; set; } = new(false);

    [Menu("Map Icon", "Show monster dots on the large map")]
    public ToggleNode MinimapIcon { get; set; } = new(false);
}

[Submenu(CollapsedByDefault = true)]
public class ChainAnchorSettings
{
    [Menu("Show Paths", "Draw chain anchor path lines (paths come from the Radar bridge)")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu("Path Color", "Color of path lines, map dots and the fallback arrow")]
    public ColorNode PathColor { get; set; } = new Color(255, 100, 255, 255);

    [Menu("Thickness", "Path line thickness, in px (1-5)")]
    public RangeNode<int> PathThickness { get; set; } = new(2, 1, 5);

    [Menu("Draw Every Nth", "Draw only every Nth path segment to save time (1 = all, up to 10)")]
    public RangeNode<int> DrawEveryNthSegment { get; set; } = new(3, 1, 10);

    [Menu("Map Path", "Draw path dots on the large map")]
    public ToggleNode ShowOnMinimap { get; set; } = new(true);

    [Menu("World Circle", "Draw a circle at each chain anchor position")]
    public ToggleNode ShowWorldCircle { get; set; } = new(false);

    [Menu("Circle Radius", "Anchor circle radius, in world units (30-300)")]
    public RangeNode<int> WorldCircleRadius { get; set; } = new(100, 30, 300);

    [Menu("Fallback Arrow", "Point an arrow at the anchor when Radar is unavailable")]
    public ToggleNode FallbackArrow { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class ArrowSettings
{
    [Menu("Show Arrow", "Compass arrow pointing to the nearest untriggered spawner")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu("Arrow Color", "Color of the spawner arrow")]
    public ColorNode Color { get; set; } = new Color(0, 255, 255, 255);

    [Menu("Arrow Size", "Size of the arrow triangle, in px (20-80)")]
    public RangeNode<int> Size { get; set; } = new(40, 20, 80);

    [Menu("Distance", "Arrow distance from the player's screen position, in px (60-300)")]
    public RangeNode<int> DistanceFromCenter { get; set; } = new(120, 60, 300);

    [Menu("Max Range", "Hide the arrow when the nearest spawner is farther than this, in grid units (100-2000)")]
    public RangeNode<int> MaxRange { get; set; } = new(500, 100, 2000);
}

[Submenu(CollapsedByDefault = true)]
public class GeneralSettings
{
    [Menu("Max Draw Distance", "Hide overlays beyond this distance from the player, in grid units (50-500)")]
    public RangeNode<int> MaxDrawDistance { get; set; } = new(200, 50, 500);
}

[Submenu(CollapsedByDefault = true)]
public class VarashtaSettings
{
    [Menu("Show Varashta", "Draw a world circle on Varashta")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu("Ready Color", "Color while Varashta has a wish ready")]
    public ColorNode ReadyColor { get; set; } = new Color(255, 215, 0, 255);

    [Menu("Spent Color", "Color once the wish is used")]
    public ColorNode SpentColor { get; set; } = new Color(128, 128, 128, 255);

    [Menu("Circle Radius", "World circle radius, in world units (30-200)")]
    public RangeNode<int> WorldCircleRadius { get; set; } = new(80, 30, 200);

    [Menu("Map Icon", "Show a Varashta dot on the large map")]
    public ToggleNode MinimapIcon { get; set; } = new(true);

    [Menu("Map Icon Size", "Size of the Varashta map dot, in px (5-30)")]
    public RangeNode<int> MinimapIconSize { get; set; } = new(18, 5, 30);
}

[Submenu(CollapsedByDefault = true)]
public class PortalSettings
{
    [Menu("Show Portals", "Draw a world circle and \"Enter Mirage\" label on Djinn portals")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu("Portal Color", "Color of the portal circle, label and map dot")]
    public ColorNode Color { get; set; } = new Color(138, 43, 226, 255);

    [Menu("Circle Radius", "World circle radius, in world units (30-200)")]
    public RangeNode<int> WorldCircleRadius { get; set; } = new(80, 30, 200);

    [Menu("Map Icon", "Show portal dots on the large map")]
    public ToggleNode MinimapIcon { get; set; } = new(true);

    [Menu("Map Icon Size", "Size of the portal map dot, in px (5-30)")]
    public RangeNode<int> MinimapIconSize { get; set; } = new(15, 5, 30);
}

[Submenu(CollapsedByDefault = true)]
public class WishAlertSettings
{
    [Menu("Show Alert", "Show 'Wish Available' on screen when Varashta is ready")]
    public ToggleNode ShowAlert { get; set; } = new(true);

    [Menu("Alert Color", "Color of the on-screen wish alert text")]
    public ColorNode AlertColor { get; set; } = new Color(255, 215, 0, 255);

    [Menu("Show Tier Badge", "Show an S-D tier badge on each wish selection card")]
    public ToggleNode ShowWishTierOverlay { get; set; } = new(true);

    [Menu("Show Tooltip", "Show wish details when hovering a wish card")]
    public ToggleNode ShowWishTooltip { get; set; } = new(true);

    [Menu("Highlight Flagged", "Pulsing glow and PICK label on wishes you flagged")]
    public ToggleNode HighlightFlaggedWishes { get; set; } = new(true);

    [Menu("Flag Color", "Highlight color for flagged wishes")]
    public ColorNode FlaggedWishColor { get; set; } = new Color(255, 215, 0, 255);
}
