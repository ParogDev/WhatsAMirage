# Mirage (Faridun/Zarokh) League Mechanic — AI Reference

## What the Mechanic Does

Mirage is the PoE 2 3.28 league mechanic. Players encounter Zarokh spawners in maps that, when activated, spawn waves of Mirage monsters. After clearing encounters, players can meet **Varashta** (the wish NPC) to select **wishes** — powerful map modifiers that stack.

## Key Entities (ExileApi Paths)

| Entity | Path Contains | Purpose |
|--------|---------------|---------|
| ZarokhSpawner | `Faridun/ZarokhSpawner` | Activatable spawner pillars |
| FaridunInitiatorTEMP | `FaridunInitiatorTEMP` | "Free the Djinn" — T16 spawner equivalent |
| DjinnPortal | `DjinnPortal` | "Enter Mirage" portal |
| Varashta | `Faridun/Kubera/Varashta` | Wish NPC |
| Mirage Chests | `Chests/Faridun/` | Bronze/Silver/Gold reward chests |
| Mirage Monsters | `FaridunLeague/` | League-specific monsters |
| Chain Anchors | `FaridunAstralChainAnchor` | Pathfinding objectives |

## StateMachine States

- **ZarokhSpawner**: `activated` — 1=dormant, 2=triggered/spent
- **FaridunInitiatorTEMP**: `activated`, `warlock`, `chain1`, `chain2`, `chain3`
- **Varashta**: `emerge` — 2=ready (wish available), 3=spent (wish used)

## Knowledge Base (`data/mirage-knowledge.json`)

The plugin loads this JSON at startup and on area change. It contains:
- **Wish tier list** — S/A/B/D tiers with effects and strategy notes
- **Entity paths** — canonical paths for detection
- **Tier colors** — hex colors for UI rendering
- **Detection hints** — how to find zone status and wish panels

### Updating the Knowledge Base
- Edit `data/mirage-knowledge.json` directly
- Bump `version` and `lastUpdated` fields
- Plugin reloads on next area change

### If Mechanic Goes Core
1. Change `status` from `"league"` to `"core"`
2. Update wish list (some wishes may be removed/changed)
3. Update entity paths if GGG renames them
4. Note any spawn rate or availability changes

## What We CAN Detect via ExileApi
- Entity presence/absence and paths
- StateMachine states (activated, emerge, etc.)
- Entity positions (grid + world)
- Chest open/closed state
- Monster rarity, alive/dead, invulnerability
- NPC targetability

## What We CANNOT Detect
- Which wishes are currently offered (wish panel UI is not readable)
- Wish selection history within a session
- Number of wishes remaining
- Exact mirage zone timer/progress
- MapFaridunLeague map stat (not in GameStat enum yet)

## Zone Detection Strategy

Since `MapFaridunLeague` is not in the `GameStat` enum, zone detection uses entity presence:
- If any Faridun entity (spawner, initiator, Varashta, portal) is detected in the current area, `IsMirageZone` is set to true
- This flag resets on area change
- Future: if GGG adds the enum value, switch to direct MapStats lookup
