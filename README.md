# What's a Mirage?

> Track spawners, chests, and wishes in the Mirage league mechanic so nothing gets left behind.

Part of the **WhatsA** plugin family for ExileApi.

## What It Does

- Highlights untriggered spawners with world circles and minimap icons, with a compass arrow pointing to the nearest one
- Color-codes reward chests by tier (Bronze / Silver / Gold) so you can prioritize at a glance
- Shows wish tier ratings (S through D) from PoEDB data mining with a flagging system for your priority picks
- Draws pathfinding routes to astral chain anchors via Radar plugin, with a fallback directional arrow

## Getting Started

1. Download and place in `Plugins/Source/What's a Mirage/`
2. HUD auto-compiles on next launch
3. Enable in plugin list
4. Enter a Mirage encounter -- spawners and chests light up automatically
5. Open settings to customize colors, enable/disable layers, and flag wish priorities

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| **Spawners** | On | Show world circles and minimap dots on untriggered spawners |
| Spawner Color | Cyan | Circle color for spawners |
| **Chests** | On | Show tiered chest highlights |
| Show Tier Label | On | Display Bronze/Silver/Gold text above chests |
| **Monsters** | Off | Show world circles on Mirage monsters |
| **Chain Anchors** | On | Draw path lines to astral chain anchor objectives |
| Fallback Arrow | On | Directional arrow when Radar pathfinding is unavailable |
| **Compass Arrow** | On | Points to nearest untriggered spawner |
| Arrow Max Range | 2000 | Hide arrow when spawner is farther than this |
| **Wish Overlay** | On | Show tier badges on wish selection cards |
| Highlight Flagged | On | Pulsing glow on wishes you've marked as priority |
| Max Draw Distance | 200 | Hide all overlays beyond this grid distance |

<details>
<summary>Technical Details</summary>

### Entity Detection

The plugin filters entities by metadata path:
- Spawners: `Faridun/ZarokhSpawner`
- Chests: `Chests/Faridun` (classified into Bronze/Silver/Gold by chest subtype)
- Monsters: `FaridunLeague`
- Chain Anchors: `FaridunAstralChainAnchor`

### Wish Tier System

Tier ratings are sourced from PoEDB data mining and stored in `data/wish-tiers.json`. Tiers:
- **S** -- Always take
- **A** -- Strong pick
- **B** -- Situational
- **C** -- Chain break
- **D** -- Skip

Flagged wishes persist to `ConfigDirectory/flagged-wishes.json`. Run `/update-wish-tiers` to sync fresh data from PoEDB.

### Inter-Plugin Integration

Uses Radar's PluginBridge for pathfinding to chain anchors. Falls back to directional arrows when Radar is unavailable. Bridge connection status is shown in the settings UI.

### Architecture

- Entity tracking via `EntityAdded`/`EntityRemoved` callbacks with metadata path filters
- State updates cached in `Tick()` (spawner activation, chest open state, monster flags)
- Render-only drawing in `Render()` -- no `GetComponent` calls in the draw path
- Custom ImGui settings UI with 6 tabs, amber/gold theme, live entity counts

### Data Classes

- `MirageSpawnerData` -- spawner position, triggered state
- `MirageChestData` -- chest position, tier, opened state
- `MirageMonsterData` -- monster position, rarity, invulnerability
- `MirageChainAnchorData` -- anchor position, path segments

</details>

## About This Project

These plugins are built with AI-assisted development using Claude Code and the
ExileApiScaffolding (private development workspace) workspace.

The developer works professionally in cybersecurity and high-risk software --
AI compensates for a C# knowledge gap specifically, not engineering judgment.
Plugin data comes from the PoE Wiki and PoEDB data mining.

The focus is on UX: friction points and missing expected features that the
existing plugin ecosystem doesn't address. Every hour spent developing is an
hour not spent on league progression, so feedback is the best way to support
the project.

## WhatsA Plugin Family

| Plugin | Description |
|--------|-------------|
| [What's a Breakpoint?](https://github.com/ParogDev/WhatsABreakpoint) | Kinetic Fusillade attack speed breakpoint visualizer |
| [What's a Crowd Control?](https://github.com/ParogDev/WhatsACrowdControl) | OmniCC-style CC effect overlay with timers |
| **What's a Mirage?** | League mechanic overlay for spawners, chests, and wishes |
| [What's a Tincture?](https://github.com/ParogDev/WhatsATincture) | Automated tincture management with burn stack tracking |
| [What's a Tooltip?](https://github.com/ParogDev/WhatsATooltip) | Shared rich tooltip service for WhatsA plugins |
| [What's an AI Bridge?](https://github.com/ParogDev/WhatsAnAiBridge) | File-based IPC for AI-assisted plugin development |
| [What's an Unbound Avatar?](https://github.com/ParogDev/WhatsAnUnboundAvatar) | Auto-activation for Avatar of the Wilds at 100 fury |

Built with ExileApiScaffolding (private development workspace)
