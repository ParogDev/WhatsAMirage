using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets.Native;
using ImGuiNET;
using Newtonsoft.Json;
using Color = SharpDX.Color;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace WhatsAMirage;

public class WhatsAMirage : BaseSettingsPlugin<WhatsAMirageSettings>
{
    private readonly Dictionary<uint, MirageSpawnerData> _spawners = new();
    private readonly Dictionary<uint, MirageChestData> _chests = new();
    private readonly Dictionary<uint, MirageMonsterData> _monsters = new();
    private readonly Dictionary<uint, MirageChainAnchorData> _chainAnchors = new();
    private readonly Dictionary<uint, FaridunInitiatorData> _initiators = new();
    private readonly Dictionary<uint, DjinnPortalData> _portals = new();
    private VarashtaData _varashta;
    private bool _isMirageZone;

    private MirageKnowledgeBase _knowledgeBase;

    private MirageSpawnerData _arrowTarget;
    private MirageSettingsUi _settingsUi;

    // Wish panel state
    private bool _wishPanelVisible;
    private List<Element> _cachedWishElements;

    // Radar pathfinding bridge
    private Func<Vector2, Action<List<Vector2i>>, CancellationToken, System.Threading.Tasks.Task> _radarLookForRoute;
    private bool _radarChecked;
    private float[][] _heightData;
    private const float GridToWorldMultiplier = 250f / 23f;

    // Minimap state
    private bool? _largeMap;
    private float _mapScale;
    private Vector2 _mapCenter;

    private const float CameraAngle = 38.7f * MathF.PI / 180;
    private static readonly float CameraAngleCos = MathF.Cos(CameraAngle);
    private static readonly float CameraAngleSin = MathF.Sin(CameraAngle);

    private Camera Camera => GameController.IngameState.Camera;

    public override bool Initialise()
    {
        LoadKnowledgeBase();
        return true;
    }

    private void LoadKnowledgeBase()
    {
        try
        {
            // DirectoryFullName = DLL's directory. The csproj copies data/ to output.
            // Also check source root as fallback (walk up from assembly location).
            var asmDir = Path.GetDirectoryName(GetType().Assembly.Location) ?? DirectoryFullName;
            var candidates = new List<string>
            {
                Path.Combine(DirectoryFullName, "data", "mirage-knowledge.json"),
                Path.Combine(asmDir, "data", "mirage-knowledge.json"),
            };
            // Walk up from assembly dir looking for source root with data/
            var dir = asmDir;
            for (int i = 0; i < 6 && dir != null; i++)
            {
                dir = Path.GetDirectoryName(dir);
                if (dir != null)
                    candidates.Add(Path.Combine(dir, "data", "mirage-knowledge.json"));
            }

            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                {
                    _knowledgeBase = JsonConvert.DeserializeObject<MirageKnowledgeBase>(File.ReadAllText(full));
                    LogMsg($"Loaded mirage knowledge base from {full}");
                    return;
                }
            }

            LogError("mirage-knowledge.json not found in any expected location");
        }
        catch (Exception ex)
        {
            LogError($"Failed to load mirage knowledge base: {ex.Message}");
        }
    }

    public void RefreshKnowledgeBase()
    {
        LoadKnowledgeBase();
    }

    public override void AreaChange(AreaInstance area)
    {
        _spawners.Clear();
        _chests.Clear();
        _monsters.Clear();
        _initiators.Clear();
        _portals.Clear();
        _varashta = null;
        _isMirageZone = false;
        _arrowTarget = null;
        _cachedWishElements?.Clear();

        foreach (var anchor in _chainAnchors.Values)
            anchor.PathCts?.Cancel();
        _chainAnchors.Clear();
        _radarChecked = false;
        _heightData = GameController.IngameState.Data.RawTerrainHeightData;

        LoadKnowledgeBase();
    }

    public override void EntityAdded(Entity entity)
    {
        var path = entity.Path;
        if (string.IsNullOrEmpty(path))
            return;

        if (path.Contains("Faridun/ZarokhSpawner", StringComparison.Ordinal))
        {
            _isMirageZone = true;
            if (_spawners.TryGetValue(entity.Id, out var existing))
            {
                existing.Entity = entity;
                existing.LastKnownGridPos = entity.GridPosNum;
            }
            else
            {
                _spawners[entity.Id] = new MirageSpawnerData(entity);
            }
        }
        else if (path.Contains("FaridunInitiatorTEMP", StringComparison.Ordinal))
        {
            _isMirageZone = true;
            if (_initiators.TryGetValue(entity.Id, out var existing))
            {
                existing.Entity = entity;
                existing.LastKnownGridPos = entity.GridPosNum;
            }
            else
            {
                _initiators[entity.Id] = new FaridunInitiatorData(entity);
            }
        }
        else if (path.Contains("Faridun/Kubera/Varashta", StringComparison.Ordinal))
        {
            _isMirageZone = true;
            if (_varashta != null)
                _varashta.Entity = entity;
            else
                _varashta = new VarashtaData(entity);
        }
        else if (path.Contains("DjinnPortal", StringComparison.Ordinal))
        {
            _isMirageZone = true;
            _portals[entity.Id] = new DjinnPortalData(entity);
        }
        else if (path.Contains("Chests/Faridun/", StringComparison.Ordinal))
        {
            _isMirageZone = true;
            _chests[entity.Id] = new MirageChestData(entity);
        }
        else if (path.Contains("FaridunLeague/", StringComparison.Ordinal))
        {
            _monsters[entity.Id] = new MirageMonsterData(entity);
        }
        else if (path.Contains("FaridunAstralChainAnchor", StringComparison.Ordinal))
        {
            _isMirageZone = true;
            var anchor = new MirageChainAnchorData(entity);
            _chainAnchors[entity.Id] = anchor;
            RequestPathForAnchor(anchor);
        }
    }

    public override Job Tick()
    {
        // Lazy-load Radar PluginBridge (once per area)
        if (!_radarChecked)
        {
            _radarLookForRoute = GameController.PluginBridge
                .GetMethod<Func<Vector2, Action<List<Vector2i>>, CancellationToken, System.Threading.Tasks.Task>>("Radar.LookForRoute");
            _radarChecked = true;
        }

        // Update chain anchors — don't remove on invalid, entity may return in range
        foreach (var (id, data) in _chainAnchors)
        {
            if (!data.Entity.IsValid)
            {
                // Out of range — cancel path to save resources, but keep tracking
                if (data.PathCts != null)
                {
                    data.PathCts.Cancel();
                    data.PathCts = null;
                    data.Path = null;
                }
            }
            else if (data.Path == null && data.PathCts == null && _radarLookForRoute != null)
            {
                // Back in range (or newly available) — re-request path
                RequestPathForAnchor(data);
            }
        }

        // Update spawners — check StateMachine.activated to detect triggered state
        // Don't remove on invalid — entity may return in range; keep tracking for arrow
        foreach (var (id, data) in _spawners)
        {
            if (!data.Entity.IsValid) continue;

            // Cache position while entity is readable
            data.LastKnownGridPos = data.Entity.GridPosNum;

            // StateMachine "activated" state: 1 = dormant, 2 = triggered/spent
            var sm = data.Entity.GetComponent<StateMachine>();
            if (sm?.States != null)
            {
                foreach (var state in sm.States)
                {
                    if (state.Name == "activated")
                    {
                        data.IsActivated = state.Value != 1;
                        break;
                    }
                }
            }
        }

        // Update chests
        var deadChestIds = new List<uint>();
        foreach (var (id, data) in _chests)
        {
            if (!data.Entity.IsValid)
            {
                deadChestIds.Add(id);
                continue;
            }

            var chest = data.Entity.GetComponent<Chest>();
            if (chest != null)
                data.IsOpened = chest.IsOpened;
        }
        foreach (var id in deadChestIds) _chests.Remove(id);

        // Update monsters
        var deadMonsterIds = new List<uint>();
        foreach (var (id, data) in _monsters)
        {
            if (!data.Entity.IsValid || data.Entity.IsDead)
            {
                deadMonsterIds.Add(id);
                continue;
            }

            data.IsRareLeader = data.Entity.Rarity == MonsterRarity.Rare;

            var stats = data.Entity.GetComponent<Stats>();
            if (stats?.StatDictionary != null)
                data.IsInvulnerable = stats.StatDictionary.TryGetValue(GameStat.CannotBeDamaged, out var val) && val == 1;
            else
                data.IsInvulnerable = false;
        }
        foreach (var id in deadMonsterIds) _monsters.Remove(id);

        // Update initiators (same pattern as spawners)
        foreach (var (id, data) in _initiators)
        {
            if (!data.Entity.IsValid) continue;
            data.LastKnownGridPos = data.Entity.GridPosNum;

            var sm = data.Entity.GetComponent<StateMachine>();
            if (sm?.States != null)
            {
                foreach (var state in sm.States)
                {
                    if (state.Name == "activated")
                    {
                        data.IsActivated = state.Value != 1;
                        break;
                    }
                }
            }
        }

        // Update Varashta emerge state
        if (_varashta?.Entity?.IsValid == true)
        {
            var sm = _varashta.Entity.GetComponent<StateMachine>();
            if (sm?.States != null)
            {
                foreach (var state in sm.States)
                {
                    if (state.Name == "emerge")
                    {
                        _varashta.EmergeState = state.Value;
                        break;
                    }
                }
            }
        }

        // Find arrow target: nearest untriggered spawner or initiator
        _arrowTarget = null;
        var playerPos = GameController.Player?.GridPosNum ?? Vector2.Zero;
        if (playerPos != Vector2.Zero)
        {
            float bestDist = float.MaxValue;
            foreach (var data in _spawners.Values)
            {
                if (data.IsActivated) continue;
                var dist = Vector2.Distance(playerPos, data.LastKnownGridPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    _arrowTarget = data;
                }
            }

            // Respect max range setting
            if (_arrowTarget != null && bestDist > Settings.Arrow.MaxRange.Value)
                _arrowTarget = null;
        }

        // Update minimap state
        var ingameUi = GameController.IngameState.IngameUi;
        var smallMiniMap = ingameUi.Map.SmallMiniMap;
        if (smallMiniMap.IsValid && smallMiniMap.IsVisibleLocal)
        {
            var mapRect = smallMiniMap.GetClientRectCache;
            _mapCenter = mapRect.Center.ToVector2Num();
            _largeMap = false;
            _mapScale = smallMiniMap.MapScale;
        }
        else if (ingameUi.Map.LargeMap.IsVisibleLocal)
        {
            var largeMapWindow = GameController.IngameState.IngameUi.Map.LargeMap;
            _mapCenter = largeMapWindow.MapCenter;
            _largeMap = true;
            _mapScale = largeMapWindow.MapScale;
        }
        else
        {
            _largeMap = null;
        }

        return null;
    }

    public override void Render()
    {
        if (!GameController.InGame) return;

        var player = GameController.Player;
        if (player == null) return;

        // Guard: skip during spawn immunity
        if (player.GetComponent<Buffs>()?.HasBuff("grace_period") == true) return;

        // Detect wish panel visibility before UI guards (wish overlay draws ON the popup)
        var ingameUi = GameController.Game.IngameState.IngameUi;
        _wishPanelVisible = false;
        _cachedWishElements?.Clear();
        if (Settings.WishAlert.ShowWishTierOverlay)
        {
            try
            {
                var popUp = GameController.IngameState.IngameUi.PopUpWindow;
                if (popUp?.IsVisible == true)
                {
                    _cachedWishElements ??= new List<Element>();
                    popUp.GetAllElementsRecursive(
                        e => e.Text != null && e.Text.StartsWith("Wish for", StringComparison.Ordinal),
                        _cachedWishElements);
                    _wishPanelVisible = _cachedWishElements.Count > 0;
                }
            }
            catch { /* PopUpWindow access can throw if UI state is transitioning */ }
        }

        // Guard: skip world overlays when fullscreen/large UI panels are open
        // (but still allow wish tier overlay since it draws on the UI itself)
        if (ingameUi.FullscreenPanels.Any(x => x.IsVisible) ||
            ingameUi.LargePanels.Any(x => x.IsVisible))
        {
            if (_wishPanelVisible && _knowledgeBase?.Wishes != null && Settings.WishAlert.ShowWishTierOverlay)
                DrawWishTierOverlay();
            return;
        }

        var playerRender = player.GetComponent<Render>();
        if (playerRender == null) return;

        var playerGridPos = playerRender.PosNum.WorldToGrid();
        var playerWorldPos = playerRender.PosNum;
        var maxDist = Settings.General.MaxDrawDistance.Value;

        if (!_wishPanelVisible)
        {
        // Draw spawners
        if (Settings.Spawners.Show)
        {
            foreach (var data in _spawners.Values)
            {
                if (data.IsActivated || !data.Entity.IsValid) continue;

                var entityGridPos = data.Entity.GridPosNum;
                var gridDist = Vector2.Distance(playerGridPos, entityGridPos);
                if (gridDist > maxDist) continue;

                // World circle
                var worldPos = data.Entity.PosNum;
                Graphics.DrawCircleInWorld(worldPos, Settings.Spawners.WorldCircleRadius.Value,
                    Settings.Spawners.WorldCircleColor.Value, 2, 24, true);

                // Minimap icon
                if (Settings.Spawners.MinimapIcon && _largeMap != null)
                {
                    DrawMinimapDot(entityGridPos, playerGridPos, playerRender,
                        Settings.Spawners.MinimapIconSize.Value, Settings.Spawners.WorldCircleColor.Value);
                }
            }
        }

        // Draw chests
        if (Settings.Chests.Show)
        {
            foreach (var data in _chests.Values)
            {
                if (data.IsOpened || !data.Entity.IsValid) continue;

                var entityGridPos = data.Entity.GridPosNum;
                var gridDist = Vector2.Distance(playerGridPos, entityGridPos);
                if (gridDist > maxDist) continue;

                var color = data.Tier switch
                {
                    ChestTier.Bronze => Settings.Chests.BronzeColor.Value,
                    ChestTier.Silver => Settings.Chests.SilverColor.Value,
                    ChestTier.Gold => Settings.Chests.GoldColor.Value,
                    _ => Color.White
                };

                // World circle
                var worldPos = data.Entity.PosNum;
                Graphics.DrawCircleInWorld(worldPos, Settings.Chests.WorldCircleRadius.Value,
                    color, 2, 24, true);

                // Tier label
                if (Settings.Chests.ShowTierLabel && data.Tier != ChestTier.Unknown)
                {
                    var screenPos = Camera.WorldToScreen(worldPos);
                    if (IsOnScreen(screenPos))
                    {
                        var label = data.Tier.ToString();
                        Graphics.DrawText(label, screenPos - new Vector2(0, 20), color, FontAlign.Center);
                    }
                }

                // Minimap icon
                if (Settings.Chests.MinimapIcon && _largeMap != null)
                {
                    DrawMinimapDot(entityGridPos, playerGridPos, playerRender,
                        10, color);
                }
            }
        }

        // Draw monsters
        if (Settings.Monsters.Show)
        {
            var aliveMonsters = _monsters.Values
                .Where(m => m.Entity.IsValid && !m.Entity.IsDead)
                .ToList();

            // Count pack near each rare leader for overlay
            var rareLeaders = aliveMonsters.Where(m => m.IsRareLeader).ToList();

            foreach (var data in aliveMonsters)
            {
                var entityGridPos = data.Entity.GridPosNum;
                var gridDist = Vector2.Distance(playerGridPos, entityGridPos);
                if (gridDist > maxDist) continue;

                Color color;
                if (data.IsInvulnerable)
                    color = Settings.Monsters.InvulnerableColor.Value;
                else if (data.IsRareLeader)
                    color = Settings.Monsters.RareLeaderColor.Value;
                else
                    color = Color.Yellow;

                // World circle
                var worldPos = data.Entity.PosNum;
                Graphics.DrawCircleInWorld(worldPos, 40, color, 2, 16, true);

                // Minimap icon
                if (Settings.Monsters.MinimapIcon && _largeMap != null)
                {
                    DrawMinimapDot(entityGridPos, playerGridPos, playerRender,
                        8, color);
                }
            }

            // Pack count overlay on rare leaders
            if (Settings.Monsters.PackCountOverlay)
            {
                foreach (var leader in rareLeaders)
                {
                    if (!leader.Entity.IsValid || leader.Entity.IsDead) continue;
                    var leaderGrid = leader.Entity.GridPosNum;
                    var gridDist = Vector2.Distance(playerGridPos, leaderGrid);
                    if (gridDist > maxDist) continue;

                    var packCount = aliveMonsters.Count(m =>
                        Vector2.Distance(leaderGrid, m.Entity.GridPosNum) < 60);

                    var screenPos = Camera.WorldToScreen(leader.Entity.PosNum);
                    if (IsOnScreen(screenPos))
                    {
                        Graphics.DrawText($"x{packCount}",
                            screenPos - new Vector2(0, 30),
                            Settings.Monsters.RareLeaderColor.Value,
                            FontAlign.Center);
                    }
                }
            }
        }

        // Draw chain anchor paths
        if (Settings.ChainAnchors.Show && _chainAnchors.Count > 0)
        {
            DrawChainAnchorPaths(playerWorldPos, playerGridPos, playerRender);
        }

        // Draw initiators (same visuals as spawners)
        if (Settings.Spawners.Show)
        {
            foreach (var data in _initiators.Values)
            {
                if (data.IsActivated || !data.Entity.IsValid) continue;

                var entityGridPos = data.Entity.GridPosNum;
                var gridDist = Vector2.Distance(playerGridPos, entityGridPos);
                if (gridDist > maxDist) continue;

                var worldPos = data.Entity.PosNum;
                Graphics.DrawCircleInWorld(worldPos, Settings.Spawners.WorldCircleRadius.Value,
                    Settings.Spawners.WorldCircleColor.Value, 2, 24, true);

                if (Settings.Spawners.MinimapIcon && _largeMap != null)
                {
                    DrawMinimapDot(entityGridPos, playerGridPos, playerRender,
                        Settings.Spawners.MinimapIconSize.Value, Settings.Spawners.WorldCircleColor.Value);
                }
            }
        }

        // Draw Varashta
        if (Settings.Varashta.Show && _varashta?.Entity?.IsValid == true)
        {
            var entityGridPos = _varashta.Entity.GridPosNum;
            var gridDist = Vector2.Distance(playerGridPos, entityGridPos);
            if (gridDist <= maxDist)
            {
                var color = _varashta.IsReady
                    ? Settings.Varashta.ReadyColor.Value
                    : Settings.Varashta.SpentColor.Value;

                var worldPos = _varashta.Entity.PosNum;
                Graphics.DrawCircleInWorld(worldPos, Settings.Varashta.WorldCircleRadius.Value,
                    color, 2, 24, true);

                // Status label
                var screenPos = Camera.WorldToScreen(worldPos);
                if (IsOnScreen(screenPos))
                {
                    var label = _varashta.IsReady ? "Varashta [READY]" :
                                _varashta.IsSpent ? "Varashta [SPENT]" : "Varashta";
                    Graphics.DrawText(label, screenPos - new Vector2(0, 25), color, FontAlign.Center);
                }

                if (Settings.Varashta.MinimapIcon && _largeMap != null)
                {
                    DrawMinimapDot(entityGridPos, playerGridPos, playerRender,
                        Settings.Varashta.MinimapIconSize.Value, color);
                }
            }
        }

        // Draw DjinnPortals
        if (Settings.Portal.Show)
        {
            foreach (var data in _portals.Values)
            {
                if (!data.Entity.IsValid) continue;

                var entityGridPos = data.Entity.GridPosNum;
                var gridDist = Vector2.Distance(playerGridPos, entityGridPos);
                if (gridDist > maxDist) continue;

                var worldPos = data.Entity.PosNum;
                Graphics.DrawCircleInWorld(worldPos, Settings.Portal.WorldCircleRadius.Value,
                    Settings.Portal.Color.Value, 2, 24, true);

                var screenPos = Camera.WorldToScreen(worldPos);
                if (IsOnScreen(screenPos))
                    Graphics.DrawText("Enter Mirage", screenPos - new Vector2(0, 25),
                        Settings.Portal.Color.Value, FontAlign.Center);

                if (Settings.Portal.MinimapIcon && _largeMap != null)
                {
                    DrawMinimapDot(entityGridPos, playerGridPos, playerRender,
                        Settings.Portal.MinimapIconSize.Value, Settings.Portal.Color.Value);
                }
            }
        }

        // Wish available alert
        if (Settings.WishAlert.ShowAlert && _varashta is { IsReady: true, Entity.IsValid: true })
        {
            var screenSize = new Vector2(Camera.Width, Camera.Height);
            var alertPos = new Vector2(screenSize.X / 2, 80);
            Graphics.DrawText("Wish Available", alertPos,
                Settings.WishAlert.AlertColor.Value, FontAlign.Center);
        }

        // Draw TomTom arrow
        if (Settings.Arrow.Show && _arrowTarget != null)
        {
            DrawDirectionArrow(playerWorldPos, playerGridPos);
        }
        } // end if (!_wishPanelVisible)

        // Wish tier overlay on the wish selection panel
        if (_wishPanelVisible && _knowledgeBase?.Wishes != null && Settings.WishAlert.ShowWishTierOverlay)
        {
            DrawWishTierOverlay();
        }
    }

    private static readonly Dictionary<string, Color> TierColors = new()
    {
        ["S"] = new Color(34, 197, 94),    // green
        ["A"] = new Color(59, 130, 246),   // blue
        ["B"] = new Color(167, 139, 250),  // purple
        ["C"] = new Color(245, 158, 11),   // amber
        ["D"] = new Color(107, 114, 128),  // gray
    };

    private void DrawWishTierOverlay()
    {
        if (_cachedWishElements == null || _cachedWishElements.Count == 0) return;

        foreach (var elem in _cachedWishElements)
        {
            var wishName = elem.Text;
            _knowledgeBase.Wishes.TryGetValue(wishName, out var wishData);

            var rect = elem.GetClientRectCache;
            if (rect.Width <= 0 || rect.Height <= 0) continue;

            var tierText = wishData?.Tier ?? "?";
            var tierColor = TierColors.GetValueOrDefault(tierText, new Color(128, 128, 128));

            // Large tier badge, right-aligned inside the wish name bar
            using (Graphics.SetTextScale(1.8f))
            {
                var textSize = Graphics.MeasureText(tierText);
                var padding = new Vector2(10, 6);
                var badgeCenter = new Vector2(
                    rect.X + rect.Width - textSize.X / 2 - padding.X - 8,
                    rect.Y + rect.Height / 2);

                var pillMin = badgeCenter - textSize / 2 - padding;
                var pillMax = badgeCenter + textSize / 2 + padding;

                // Dark background + tier-colored frame
                Graphics.DrawBox(pillMin, pillMax, new Color(10, 10, 15, 220));
                Graphics.DrawFrame(pillMin, pillMax, tierColor, 2);

                // Tier letter
                Graphics.DrawText(tierText, badgeCenter, tierColor, FontAlign.Center);
            }
        }

        // Tooltip on hover
        if (Settings.WishAlert.ShowWishTooltip)
        {
            var mousePos = Input.MousePositionNum;
            foreach (var elem in _cachedWishElements)
            {
                var rect = elem.GetClientRectCache;
                if (rect.Width <= 0) continue;

                // Hit test
                if (mousePos.X < rect.X || mousePos.X > rect.X + rect.Width ||
                    mousePos.Y < rect.Y || mousePos.Y > rect.Y + rect.Height)
                    continue;

                var wishName = elem.Text;
                _knowledgeBase.Wishes.TryGetValue(wishName, out var wishData);

                var tierText = wishData?.Tier ?? "?";
                var tierColor4 = TierColors.TryGetValue(tierText, out var tc)
                    ? new Vector4(tc.R / 255f, tc.G / 255f, tc.B / 255f, 1f)
                    : new Vector4(0.5f, 0.5f, 0.5f, 1f);

                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.05f, 0.05f, 0.07f, 0.95f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 8));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
                ImGui.BeginTooltip();

                ImGui.TextColored(tierColor4, $"[{tierText}] {wishName}");
                ImGui.Separator();

                if (wishData != null)
                {
                    ImGui.TextColored(new Vector4(0.88f, 0.88f, 0.88f, 1f), wishData.Effect);
                    if (!string.IsNullOrEmpty(wishData.Category))
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.55f, 1f), $"Category: {wishData.Category}");
                    if (!string.IsNullOrEmpty(wishData.Notes))
                    {
                        ImGui.Spacing();
                        ImGui.TextColored(new Vector4(0.83f, 0.63f, 0.09f, 0.9f), wishData.Notes);
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Unknown wish - not in knowledge base");
                }

                ImGui.EndTooltip();
                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor();
                break; // one tooltip at a time
            }
        }
    }

    private void RequestPathForAnchor(MirageChainAnchorData anchor)
    {
        if (_radarLookForRoute == null) return;

        anchor.PathCts?.Cancel();
        anchor.PathCts = new CancellationTokenSource();

        var gridPos = anchor.Entity.GridPosNum;
        var target = new Vector2(gridPos.X, gridPos.Y);

        _radarLookForRoute(target, path =>
        {
            anchor.Path = path;
        }, anchor.PathCts.Token);
    }

    private void DrawChainAnchorPaths(Vector3 playerWorldPos, Vector2 playerGridPos, Render playerRender)
    {
        var pathColor = Settings.ChainAnchors.PathColor.Value;
        var thickness = Settings.ChainAnchors.PathThickness.Value;
        var nth = Settings.ChainAnchors.DrawEveryNthSegment.Value;

        foreach (var data in _chainAnchors.Values)
        {
            if (data.IsCompleted || !data.Entity.IsValid) continue;

            // World circle at anchor position
            if (Settings.ChainAnchors.ShowWorldCircle)
            {
                Graphics.DrawCircleInWorld(data.Entity.PosNum,
                    Settings.ChainAnchors.WorldCircleRadius.Value, pathColor, 2, 24, true);
            }

            // Draw path segments (world space)
            var path = data.Path;
            if (path is { Count: > 1 })
            {
                var p0 = Camera.WorldToScreen(playerWorldPos);
                var i = 0;
                foreach (var elem in path)
                {
                    float height = 0;
                    if (_heightData != null && elem.Y >= 0 && elem.X >= 0
                        && elem.Y < _heightData.Length && elem.X < _heightData[elem.Y].Length)
                        height = _heightData[elem.Y][elem.X];

                    var p1 = Camera.WorldToScreen(
                        new Vector3(elem.X * GridToWorldMultiplier, elem.Y * GridToWorldMultiplier, height));

                    if (++i % nth == 0)
                    {
                        if (IsOnScreen(p0) || IsOnScreen(p1))
                            Graphics.DrawLine(p0, p1, thickness, pathColor);
                        else
                            break;
                    }
                    p0 = p1;
                }
            }

            // Minimap path
            if (Settings.ChainAnchors.ShowOnMinimap && _largeMap != null && path is { Count: > 1 })
            {
                DrawMinimapPath(path, playerGridPos, playerRender, pathColor);
            }

            // Fallback arrow when no path available and Radar not loaded
            if (path == null && Settings.ChainAnchors.FallbackArrow && _radarLookForRoute == null)
            {
                DrawFallbackArrow(data.Entity, playerWorldPos, playerGridPos);
            }
        }
    }

    private void DrawMinimapPath(List<Vector2i> path, Vector2 playerGridPos, Render playerRender, Color color)
    {
        var playerHeight = -playerRender.UnclampedHeight;
        var ithElement = 0;

        foreach (var elem in path)
        {
            if (++ithElement % 5 != 0) continue;

            float height = 0;
            if (_heightData != null && elem.Y >= 0 && elem.X >= 0
                && elem.Y < _heightData.Length && elem.X < _heightData[elem.Y].Length)
                height = _heightData[elem.Y][elem.X];

            var delta = new Vector2(elem.X, elem.Y) - playerGridPos;
            var deltaZ = (playerHeight + height) * PoeMapExtension.WorldToGridConversion;
            var mapPos = _mapCenter + DeltaInWorldToMinimapDelta(delta, deltaZ);

            Graphics.DrawCircleFilled(mapPos, 2, color, 4);
        }
    }

    private void DrawFallbackArrow(Entity anchorEntity, Vector3 playerWorldPos, Vector2 playerGridPos)
    {
        if (!anchorEntity.IsValid) return;

        var targetWorldPos = anchorEntity.PosNum;
        var playerScreenPos = Camera.WorldToScreen(playerWorldPos);
        var targetScreenPos = Camera.WorldToScreen(targetWorldPos);

        var screenSize = new Vector2(Camera.Width, Camera.Height);
        var arrowSize = Settings.Arrow.Size.Value * 0.7f;
        var arrowDist = Settings.Arrow.DistanceFromCenter.Value;
        var arrowColor = Settings.ChainAnchors.PathColor.Value;

        Vector2 direction;
        Vector2 arrowCenter;

        if (IsOnScreen(targetScreenPos))
        {
            direction = Vector2.Normalize(targetScreenPos - playerScreenPos);
            arrowCenter = playerScreenPos + direction * arrowDist;
        }
        else
        {
            var targetGridPos = anchorEntity.GridPosNum;
            var dx = targetGridPos.X - playerGridPos.X;
            var dy = targetGridPos.Y - playerGridPos.Y;
            var isoX = dx - dy;
            var isoY = -(dx + dy);
            direction = Vector2.Normalize(new Vector2(isoX, isoY));

            var center = screenSize / 2;
            arrowCenter = center + direction * arrowDist;

            var margin = arrowSize + 10;
            arrowCenter = Vector2.Clamp(arrowCenter,
                new Vector2(margin, margin),
                screenSize - new Vector2(margin, margin));
        }

        var perpendicular = new Vector2(-direction.Y, direction.X);
        var tip = arrowCenter + direction * arrowSize;
        var baseLeft = arrowCenter - direction * (arrowSize * 0.5f) + perpendicular * (arrowSize * 0.33f);
        var baseRight = arrowCenter - direction * (arrowSize * 0.5f) - perpendicular * (arrowSize * 0.33f);

        Graphics.DrawConvexPolyFilled(new[] { tip, baseLeft, baseRight }, arrowColor);

        var dist = Vector2.Distance(playerGridPos, anchorEntity.GridPosNum);
        Graphics.DrawText($"{(int)dist}", arrowCenter + direction * (arrowSize + 10), arrowColor, FontAlign.Center);
    }

    private void DrawDirectionArrow(Vector3 playerWorldPos, Vector2 playerGridPos)
    {
        var target = _arrowTarget;
        if (target == null) return;

        var screenSize = new Vector2(Camera.Width, Camera.Height);
        var arrowSize = Settings.Arrow.Size.Value;
        var arrowDist = Settings.Arrow.DistanceFromCenter.Value;
        var arrowColor = Settings.Arrow.Color.Value;

        Vector2 direction;
        Vector2 arrowCenter;

        // If entity is in range, try on-screen arrow; otherwise always use grid-based direction
        if (target.Entity.IsValid)
        {
            var targetWorldPos = target.Entity.PosNum;
            var playerScreenPos = Camera.WorldToScreen(playerWorldPos);
            var targetScreenPos = Camera.WorldToScreen(targetWorldPos);

            if (IsOnScreen(targetScreenPos))
            {
                direction = Vector2.Normalize(targetScreenPos - playerScreenPos);
                arrowCenter = playerScreenPos + direction * arrowDist;
                goto drawArrow;
            }
        }

        // Off-screen or out of range: use cached grid position for direction
        {
            var targetGridPos = target.LastKnownGridPos;
            var dx = targetGridPos.X - playerGridPos.X;
            var dy = targetGridPos.Y - playerGridPos.Y;

            // Grid angle adjusted by isometric camera rotation
            // The minimap formula uses (X-Y, -(X+Y)) transform, so replicate that for direction
            var isoX = dx - dy;
            var isoY = -(dx + dy);
            direction = Vector2.Normalize(new Vector2(isoX, isoY));

            // Clamp arrow to screen edge with padding
            var center = screenSize / 2;
            arrowCenter = center + direction * arrowDist;

            // Clamp to screen bounds with margin
            var margin = arrowSize + 10;
            arrowCenter = Vector2.Clamp(arrowCenter,
                new Vector2(margin, margin),
                screenSize - new Vector2(margin, margin));
        }

        drawArrow:
        // Draw isosceles triangle (compass needle shape)
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var tip = arrowCenter + direction * arrowSize;
        var baseLeft = arrowCenter - direction * (arrowSize * 0.5f) + perpendicular * (arrowSize * 0.33f);
        var baseRight = arrowCenter - direction * (arrowSize * 0.5f) - perpendicular * (arrowSize * 0.33f);

        Graphics.DrawConvexPolyFilled(new[] { tip, baseLeft, baseRight }, arrowColor);

        // Draw distance text near arrow (use cached position — works even when out of range)
        var dist = Vector2.Distance(playerGridPos, target.LastKnownGridPos);
        Graphics.DrawText($"{(int)dist}", arrowCenter + direction * (arrowSize + 10), arrowColor, FontAlign.Center);
    }

    private void DrawMinimapDot(Vector2 entityGridPos, Vector2 playerGridPos, Render playerRender, float size, Color color)
    {
        var delta = entityGridPos - playerGridPos;
        var playerHeight = -playerRender.UnclampedHeight;
        var terrainHeight = GameController.IngameState.Data.GetTerrainHeightAt(entityGridPos);
        var deltaZ = (playerHeight + terrainHeight) * PoeMapExtension.WorldToGridConversion;

        var mapPos = _mapCenter + DeltaInWorldToMinimapDelta(delta, deltaZ);
        var halfSize = size / 2f;

        // Skip if off small minimap
        if (_largeMap == false)
        {
            var ingameUi = GameController.IngameState.IngameUi;
            var mapRect = ingameUi.Map.SmallMiniMap.GetClientRectCache;
            var drawRect = new SharpDX.RectangleF(mapPos.X - halfSize, mapPos.Y - halfSize, size, size);
            if (!mapRect.Contains(drawRect))
                return;
        }

        Graphics.DrawCircleFilled(mapPos, halfSize, color, 8);
    }

    private Vector2 DeltaInWorldToMinimapDelta(Vector2 delta, float deltaZ)
    {
        return _mapScale * Vector2.Multiply(
            new Vector2(delta.X - delta.Y, deltaZ - (delta.X + delta.Y)),
            new Vector2(CameraAngleCos, CameraAngleSin));
    }

    private bool IsOnScreen(Vector2 pos)
    {
        return pos.X > 0 && pos.Y > 0 && pos.X < Camera.Width && pos.Y < Camera.Height;
    }

    public override void DrawSettings()
    {
        _settingsUi ??= new MirageSettingsUi();
        _settingsUi.Draw(Settings, new MirageUiState(
            _spawners.Count(kv => !kv.Value.IsActivated) + _initiators.Count(kv => !kv.Value.IsActivated),
            _chests.Count(kv => !kv.Value.IsOpened && kv.Value.Entity.IsValid),
            _monsters.Count(kv => kv.Value.Entity.IsValid && !kv.Value.Entity.IsDead),
            _chainAnchors.Count,
            _chainAnchors.Count(kv => kv.Value.Path != null),
            _radarLookForRoute != null,
            _isMirageZone,
            _varashta,
            _knowledgeBase
        ), RefreshKnowledgeBase);
    }
}
