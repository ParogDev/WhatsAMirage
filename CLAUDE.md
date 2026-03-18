# CLAUDE.md — WhatsAMirage

## Plugin Context

WhatsAMirage is an overlay plugin for the Mirage (Faridun/Zarokh) league mechanic. It tracks spawners, tiered chests, league monsters, and chain-anchor pathing, drawing world circles, minimap icons, tier labels, pack count overlays, and a directional compass arrow to the nearest untriggered spawner.

**Main class**: `WhatsAMirage` inherits `BaseSettingsPlugin<WhatsAMirageSettings>`
**Settings class**: `WhatsAMirageSettings` implements `ISettings`
**Key classes**: MirageSpawnerData, MirageChestData, MirageChainAnchorData, MirageMonsterData, ChestTier (enum), MirageSettingsUi

### Architecture

Entity tracking via `EntityAdded` filters by metadata path (Faridun/ZarokhSpawner, Chests/Faridun, FaridunLeague, FaridunAstralChainAnchor). `Tick()` updates spawner activation state (StateMachine "activated"), chest open state, monster rare/invulnerable flags, chain anchor path requests (via Radar PluginBridge), and arrow target selection. `Render()` draws world circles, minimap dots, tier labels, pack counts, path lines, and direction arrows. `DrawSettings()` delegates to `MirageSettingsUi` for a custom tabbed ImGui panel.

### Lifecycle Methods Used

| Method | Purpose |
|---|---|
| `AreaChange` | Clears all entity dictionaries, cancels path CTS, reloads terrain height data |
| `EntityAdded` | Filters entities by path and populates spawner/chest/monster/anchor dictionaries |
| `Tick` | Updates entity state, requests Radar paths, resolves arrow target, caches minimap state |
| `Render` | Draws world circles, minimap icons, tier labels, pack counts, path lines, direction arrows |
| `DrawSettings` | Custom tabbed settings UI via MirageSettingsUi |

### Current Settings

> **Note**: This settings table is a snapshot from generation time. If settings are added/removed later, re-run `/setup-plugin-project` to regenerate, or update CLAUDE.md manually.

| Setting | Type | Default | Parent |
|---|---|---|---|
| Enable | ToggleNode | true | root |
| Spawners.Show | ToggleNode | true | SpawnerSettings |
| Spawners.WorldCircleColor | ColorNode | Cyan (0,255,255) | SpawnerSettings |
| Spawners.WorldCircleRadius | RangeNode\<int\> | 80 (30-200) | SpawnerSettings |
| Spawners.MinimapIcon | ToggleNode | true | SpawnerSettings |
| Spawners.MinimapIconSize | RangeNode\<int\> | 15 (5-30) | SpawnerSettings |
| Chests.Show | ToggleNode | true | ChestSettings |
| Chests.BronzeColor | ColorNode | #CD7F32 | ChestSettings |
| Chests.SilverColor | ColorNode | #C0C0C0 | ChestSettings |
| Chests.GoldColor | ColorNode | #FFD700 | ChestSettings |
| Chests.WorldCircleRadius | RangeNode\<int\> | 60 (20-150) | ChestSettings |
| Chests.MinimapIcon | ToggleNode | true | ChestSettings |
| Chests.ShowTierLabel | ToggleNode | true | ChestSettings |
| Monsters.Show | ToggleNode | true | MonsterSettings |
| Monsters.RareLeaderColor | ColorNode | Orange | MonsterSettings |
| Monsters.InvulnerableColor | ColorNode | Gray | MonsterSettings |
| Monsters.PackCountOverlay | ToggleNode | true | MonsterSettings |
| Monsters.MinimapIcon | ToggleNode | true | MonsterSettings |
| ChainAnchors.Show | ToggleNode | true | ChainAnchorSettings |
| ChainAnchors.PathColor | ColorNode | #FF64FF | ChainAnchorSettings |
| ChainAnchors.PathThickness | RangeNode\<int\> | 2 (1-5) | ChainAnchorSettings |
| ChainAnchors.DrawEveryNthSegment | RangeNode\<int\> | 3 (1-10) | ChainAnchorSettings |
| ChainAnchors.ShowOnMinimap | ToggleNode | true | ChainAnchorSettings |
| ChainAnchors.ShowWorldCircle | ToggleNode | true | ChainAnchorSettings |
| ChainAnchors.WorldCircleRadius | RangeNode\<int\> | 100 (30-300) | ChainAnchorSettings |
| ChainAnchors.FallbackArrow | ToggleNode | true | ChainAnchorSettings |
| Arrow.Show | ToggleNode | true | ArrowSettings |
| Arrow.Color | ColorNode | Cyan (0,255,255) | ArrowSettings |
| Arrow.Size | RangeNode\<int\> | 40 (20-80) | ArrowSettings |
| Arrow.DistanceFromCenter | RangeNode\<int\> | 120 (60-300) | ArrowSettings |
| Arrow.MaxRange | RangeNode\<int\> | 500 (100-2000) | ArrowSettings |
| General.MaxDrawDistance | RangeNode\<int\> | 200 (50-500) | GeneralSettings |

## Project Setup

- This is an ExileApi plugin (game HUD overlay framework for Path of Exile)
- Do not edit anything outside this directory
- Target framework: net10.0-windows, OutputType: Library

### Namespace → DLL Mapping
| DLL | Key Namespaces |
|---|---|
| `ExileCore.dll` | `ExileCore` (BaseSettingsPlugin, GameController, Graphics, Input), `ExileCore.Shared` (Nodes, Enums, Interfaces, Attributes, Helpers), `ExileCore.PoEMemory` (Components, MemoryObjects) |
| `GameOffsets.dll` | `GameOffsets` (offsets structs), `GameOffsets.Native` (Vector2i, NativeStringU) |

## Build & Run

- NO manual build command — Loader.exe auto-compiles from Plugins/Source/
- HUD installation path: resolved from .csproj HintPath (parent dir of ExileCore.dll)
- For IDE support set env var: `exapiPackage` = `<HUD installation path>`

## API Reference

- **Default**: HUD installation (from .csproj HintPath) — compiled DLLs with intellisense
- **Enhanced**: If `.claude/override-path` exists, read it for a path to expanded
  API reference with full type definitions and source. Use that path for deep lookups
  when the compiled DLLs don't provide enough detail about a type, method, or pattern.

## Plugin Anatomy

Every plugin is a C# class library. The main class inherits `BaseSettingsPlugin<TSettings>` and the settings class implements `ISettings`.

Minimal `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <OutputType>Library</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>latest</LangVersion>
    <DebugType>embedded</DebugType>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="ExileCore">
      <HintPath>$(exapiPackage)\ExileCore.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="GameOffsets">
      <HintPath>$(exapiPackage)\GameOffsets.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="ImGui.NET" Version="1.90.0.1" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="SharpDX.Mathematics" Version="4.2.0" />
  </ItemGroup>
</Project>
```

### Plugin Lifecycle

| Method | When called | Notes |
|---|---|---|
| `Initialise()` | Once on load | Register hotkeys, wire up `OnPressed`/`OnValueChanged`, return `true` on success |
| `OnLoad()` | After Initialise | Load textures: `Graphics.InitImage("file.png")` |
| `AreaChange(AreaInstance area)` | Zone change | Clear cached entity lists here |
| `Tick()` | Every frame | Return `null` (no async job needed) or a `Job` for background work |
| `Render()` | Every frame | Draw overlays; check `Settings.Enable` and `GameController.InGame` |
| `EntityAdded(Entity entity)` | Entity enters range | Filter and cache relevant entities here |
| `EntityRemoved(Entity entity)` | Entity leaves range | Remove from caches |
| `DrawSettings()` | Settings panel open | Call `base.DrawSettings()` unless fully custom |

## GameController API

`GameController` is the main access point available in all plugin methods:

```csharp
// State checks
GameController.InGame
GameController.Game.IsInGameState

// Player
GameController.Player                          // local player Entity
GameController.Player.GetComponent<Positioned>().GridPosNum

// Area
GameController.Area.CurrentArea.IsPeaceful
GameController.Area.CurrentArea.Area.RawName
GameController.Area.CurrentArea.IsTown
GameController.Area.CurrentArea.IsHideout

// Entities — prefer ValidEntitiesByType over Entities for filtered access
GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
GameController.Entities   // all entities, more expensive

// Ingame UI / state
GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible
GameController.Game.IngameState.IngameUi.FullscreenPanels
GameController.Game.IngameState.IngameUi.OpenRightPanel
GameController.Game.IngameState.Camera        // Camera, WorldToScreen
GameController.Game.IngameState.Data          // terrain, server data, area dimensions

// Game files (static data)
GameController.Files.BaseItemTypes.Translate(entity.Path)
GameController.Files.GemEffects.GetById(id)

// Window
GameController.Window.GetWindowRectangleTimeCache   // cached, use this not GetWindowRectangle()

// Inter-plugin
GameController.PluginBridge.SaveMethod("MyPlugin.Method", delegate)
GameController.PluginBridge.GetMethod<TDelegate>("OtherPlugin.Method")

// Timing
GameController.DeltaTime  // double, seconds since last frame
```

## Entity API

```csharp
entity.Path         // "Metadata/Monsters/..."
entity.Metadata     // same as Path for most entities
entity.Id           // uint, unique per session
entity.IsValid      // always check before extensive use
entity.IsAlive
entity.GridPosNum   // Vector2, 2D grid position
entity.PosNum       // Vector3, 3D world position
entity.Distance(otherEntity)
entity.DistancePlayer  // float, distance to local player
entity.Type         // EntityType enum
entity.GetComponent<Positioned>()     // null if not present
entity.GetComponent<Render>()
entity.GetComponent<Stats>()
entity.GetComponent<ObjectMagicProperties>()?.Mods
```

## Component Quick Reference

### Life
```csharp
var life = entity.GetComponent<Life>();
life.CurHP / life.MaxHP      // health
life.CurMana / life.MaxMana  // mana
life.CurES / life.MaxES      // energy shield
life.HPPercentage             // 0-100
```

### Buffs
```csharp
var buffs = entity.GetComponent<Buffs>();
buffs.BuffsList               // List<Buff>
buff.Name                     // internal string ID (e.g., "frozen", "shocked")
buff.DisplayName              // human-readable
buff.Timer                    // seconds remaining
buff.MaxTime                  // original duration
buff.Charges                  // stack count
```

### Positioned & Render
```csharp
var pos = entity.GetComponent<Positioned>();
pos.GridPosNum                // Vector2 grid coords

var render = entity.GetComponent<Render>();
render.Name                   // display name
render.Bounds                 // bounding box
```

### ObjectMagicProperties
```csharp
var omp = entity.GetComponent<ObjectMagicProperties>();
omp.Rarity                    // MonsterRarity enum
omp.Mods                      // List<string> mod names
```

### Stats
```csharp
var stats = entity.GetComponent<Stats>();
stats.StatDictionary          // Dictionary<GameStat, int>
```

### Other Common Components
- `Monster` — monster-specific data
- `Player` — player-specific data
- `Chest` — chest state (IsOpened, IsStrongbox)
- `Portal` — portal destination
- `WorldItem` — ground item info
- `Targetable` — whether entity can be targeted (isTargetable)
- `Base` — item base type info (Name, ItemBaseName)
- `Mods` — item mods (ItemMods, ImplicitMods, ItemRarity)
- `Sockets` — socket links, colors, count
- `StateMachine` — state machine states (used for spawner activation detection)

## Settings Node Types

```csharp
public class MySettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public RangeNode<int> SomeRange { get; set; } = new RangeNode<int>(10, 1, 100);
    public RangeNode<float> SomeFloat { get; set; } = new RangeNode<float>(1.5f, 0f, 10f);
    public ColorNode SomeColor { get; set; } = new ColorNode(Color.White);
    public HotkeyNodeV2 SomeKey { get; set; } = Keys.F5;
    // Methods:
    //   .IsPressed()    → bool, true while key is held down
    //   .PressedOnce()  → bool, true only on first frame of press (edge-triggered)
    //   .Value          → Keys enum value
    // Wire in Initialise():
    //   Input.RegisterKey(Settings.SomeKey.Value);
    //   Settings.SomeKey.OnValueChanged += () => Input.RegisterKey(Settings.SomeKey.Value);
    public ButtonNode SomeButton { get; set; }   // wire OnPressed in constructor
    public TextNode SomeText { get; set; } = new TextNode("default");
    public EmptyNode SomeHeader { get; set; }     // visual separator in settings

    [Menu("Display Name")]
    public ToggleNode WithLabel { get; set; } = new ToggleNode(true);

    [Submenu]
    public class NestedSection
    {
        public ToggleNode SubOption { get; set; } = new ToggleNode(false);
    }
}
```

## Graphics API

```csharp
// Rectangles — use System.Numerics.Vector2 for positions
Graphics.DrawBox(topLeft, bottomRight, color, rounding);
Graphics.DrawFrame(topLeft, bottomRight, color, borderWidth, segments, rounding);

// Text
Graphics.DrawText("text", position, color);
Graphics.DrawTextWithBackground("text", position, textColor, alignment, bgColor);
var size = Graphics.MeasureText("text");
using (Graphics.SetTextScale(1.5f)) { /* draw at scale */ }

// World / map lines
Graphics.DrawLineInWorld(gridPos1, gridPos2, width, color);
Graphics.DrawLineOnLargeMap(gridPos1, gridPos2, width, color);

// Circles
Graphics.DrawFilledCircleInWorld(worldPos, radius, color);
Graphics.DrawCircleInWorld(worldPos, radius, color, thickness, segments, filled);

// Textures (load in OnLoad, draw in Render)
Graphics.InitImage("myimage.png");            // from plugin directory
Graphics.InitImage("key", fullPath);          // with explicit path
Graphics.DrawImage("key", rectF, color);      // RectangleF from SharpDX
var texId = Graphics.GetTextureId("key");     // IntPtr for ImGui
Graphics.HasImage("key");                     // check if loaded

// Camera projection
var screenPos = GameController.Game.IngameState.Camera.WorldToScreen(entity.PosNum);
```

## Input API

```csharp
// Mouse position — DIFFERENT coordinate spaces!
Input.MousePositionNum          // Vector2, WINDOW-relative (client area coords)
Input.ForceMousePositionNum     // Vector2, ABSOLUTE screen coords (set cursor)
Input.SetCursorPos(Vector2)     // expects ABSOLUTE screen coords

// Keyboard
Input.GetKeyState(Keys.X)       // true while held
Input.IsKeyDown(Keys.X)         // true while held (alias)
Input.RegisterKey(Keys.X)       // required before polling custom keys
Input.KeyDown(Keys.X)           // simulate key press (use sparingly — anti-cheat risk)
Input.KeyUp(Keys.X)             // simulate key release
```

**WARNING**: `MousePositionNum` is window-relative. To convert to screen coords for `SetCursorPos`, add the window position from `GameController.Window.GetWindowRectangleTimeCache`.

## Performance Rules

ExileApi plugins run every frame (60+ fps). The game process is memory-mapped, so every property access that isn't cached may read from game memory.

- **NEVER** call `GetComponent<T>()` in `Render()` — do it in `Tick()`, store in fields
- **NEVER** iterate `GameController.Entities` — use `EntityListWrapper.ValidEntitiesByType`
- **NEVER** call `entity.Path` per-frame — cache in `EntityAdded()` via `SetHudComponent`
- Use `EntityAdded`/`EntityRemoved` to maintain filtered entity lists
- Use `TimeCache<T>` for expensive operations that don't need per-frame freshness
- Clear state in `AreaChange()`, not per-tick
- Use cached accessors (`GetWindowRectangleTimeCache`, `GetClientRectCache`)
- Check `entity.IsValid` and `entity.IsAlive` before processing
- Check `screenPos == Vector2.Zero` after `WorldToScreen()`
- Load textures in `OnLoad()`, never in `Render()`
- Separate data reads (`Tick`) from drawing (`Render`) — never GetComponent in Render
- Use `_canRender` flags to skip `Render()` entirely when nothing to draw
- Guard early in `Tick()`: InGame, IsAlive, area checks (town/hideout) before work

## C# Best Practices for Plugin Development

- Use file-scoped namespaces
- Seal leaf classes for virtual dispatch optimization
- Object-pool reusable instances to avoid per-frame GC pressure
- Use Dictionary/HashSet lookups instead of per-frame string comparisons
- Use switch expressions for tier/type mappings
- Use readonly fields for immutable configuration
- Any plugin with 4+ settings should use a dedicated `<PluginName>SettingsUi` class — see "Custom ImGui Settings UI" section for the full widget library

## Coordinate Systems

- **Grid** (`GridPosNum`, `Vector2`): tile-based map coordinates. Used for minimap drawing.
- **World** (`PosNum`, `Vector3`): 3D world coordinates. Convert to screen with `Camera.WorldToScreen`.
- **Screen** (`Vector2`): pixel coordinates. Used for ImGui and `Graphics.DrawText/DrawFrame`.
- GridToWorld multiplier: `250f / 23f`

## Common Patterns

### Entity Tracking
```csharp
// In EntityAdded — filter and cache
public override void EntityAdded(Entity entity)
{
    if (entity.Type == EntityType.Monster && entity.IsAlive)
        _trackedMonsters[entity.Id] = entity;
}

// In EntityRemoved — clean up
public override void EntityRemoved(Entity entity)
{
    _trackedMonsters.Remove(entity.Id);
}

// In AreaChange — clear all
public override void AreaChange(AreaInstance area)
{
    _trackedMonsters.Clear();
}
```

### Buff Monitoring
```csharp
var buffs = player.GetComponent<Buffs>()?.BuffsList;
if (buffs != null)
{
    for (int i = 0; i < buffs.Count; i++)
    {
        var buff = buffs[i];
        if (buff?.Name == "target_buff" && buff.Timer > 0)
        {
            // buff is active
        }
    }
}
```

### Rate-Limited Updates
```csharp
// Option 1: TimeCache — auto-invalidates after ms
private readonly TimeCache<List<MyData>> _cache;
_cache = new TimeCache<List<MyData>>(BuildData, 200); // refresh every 200ms

// Option 2: Stopwatch throttle
private readonly Stopwatch _timer = Stopwatch.StartNew();
if (_timer.ElapsedMilliseconds < 200) return null;
_timer.Restart();
```

### Edge Detection (Key Press / Release)
```csharp
private bool _wasKeyPressed;

// In Tick():
var pressed = Settings.SomeKey.IsPressed();
if (pressed && !_wasKeyPressed) { /* on press edge */ }
else if (!pressed && _wasKeyPressed) { /* on release edge */ }
_wasKeyPressed = pressed;
```

### Inter-Plugin Communication
```csharp
// Expose a method
GameController.PluginBridge.SaveMethod("MyPlugin.GetData", (Func<MyData>)GetData);

// Consume another plugin's method
var getData = GameController.PluginBridge.GetMethod<Func<OtherData>>("OtherPlugin.GetData");
if (getData != null) { var data = getData(); }
```

### Custom ImGui Settings UI

This plugin uses a dedicated `MirageSettingsUi` class with amber/gold accent, 5 tabs (Spawners, Chests, Monsters, Paths, Arrow), and self-contained PillToggle/SliderInt/ColorPicker/SectionHeader widget primitives. See `MirageSettingsUi.cs` for the full implementation.

### UI-First Feature Development

When adding a new feature, always design the settings UI experience in the same pass — never leave bare controls without domain context.

**Required UI enrichment for each feature type:**

| What you're adding | What the SettingsUi needs |
|---|---|
| New entity type or data source | Live count in a status bar + status line in the relevant tab |
| New color setting | Legend item showing a colored dot + what that color means in context |
| Bridge/integration (e.g., Radar) | Connection status indicator (green/red dot + status text) |
| Visual feature (arrows, circles, paths) | Preview widget showing what the in-game overlay will look like |
| New mechanic | Info block explaining how the mechanic works for users unfamiliar with it |

**Live runtime data pattern** — define a record to pass live state from `DrawSettings()`:
```csharp
public record MyPluginUiState(int EntityCount, bool BridgeConnected, ...);

// In DrawSettings():
_settingsUi.Draw(Settings, new MyPluginUiState(
    _tracked.Count(x => x.IsValid),
    _bridge != null
));
```

**Domain-specific widget examples:**
- **Status bar** — compact strip below tab bar: `● 3 Spawners  ● 7 Chests  ● 12 Monsters` (accent when >0, dim when 0)
- **Tier legend** — colored dot + name + description: `● Bronze — Common rewards`
- **Bridge status** — green/red dot: `● Radar connected` / `● Radar unavailable — fallback mode`
- **Info block** — left accent bar + title + description text explaining a mechanic
- **Preview** — draw a miniature version of the overlay element using configured color/size

A feature without its UI counterpart feels incomplete. The goal is that a user opening settings immediately understands what the plugin does, what it's tracking, and whether everything is working.

## Reference Plugins

Study other plugins in `Plugins/Source/` for pattern reference. Look at:
- Simple buff-tracking plugins for entity/buff patterns
- Minimap overlay plugins for coordinate system usage
- Settings-heavy plugins for ImGui custom UI patterns
- WhatsACrowdControl / WhatsATincture for custom settings UI patterns
