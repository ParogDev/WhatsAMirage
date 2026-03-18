using System;
using System.Collections.Generic;
using System.Numerics;
using ExileCore.Shared.Nodes;
using ImGuiNET;

namespace WhatsAMirage;

/// <summary>
/// Live runtime data passed from WhatsAMirage.DrawSettings().
/// </summary>
public record MirageUiState(
    int SpawnerCount,
    int ChestCount,
    int MonsterCount,
    int AnchorCount,
    int AnchorPathCount,
    bool RadarAvailable,
    bool IsMirageZone,
    VarashtaData Varashta,
    WishTierFile WishTiers
);

/// <summary>
/// Self-contained neon-styled settings panel for WhatsAMirage.
/// Amber/gold (#D4A017) accent - Zarokh/Maraketh desert theme.
/// Custom DrawList widgets, tabbed layout, 5 content tabs.
/// </summary>
public sealed class MirageSettingsUi
{
    private int _activeTab;
    private readonly Dictionary<string, float> _anims = new();

    private static readonly string[] Tabs = { "Spawners", "Chests", "Monsters", "Paths", "Arrow", "Wishes" };

    // ── Palette ─────────────────────────────────────────────────────
    private static uint Accent => Col(0.831f, 0.627f, 0.090f);
    private static uint AccentDim => Col(0.55f, 0.42f, 0.06f);
    private static uint Label => Col(0.88f, 0.88f, 0.88f);
    private static uint Desc => Col(0.38f, 0.40f, 0.44f);
    private static uint TabOff => Col(0.48f, 0.48f, 0.52f);
    private static uint CardBg => Col(0.05f, 0.05f, 0.07f, 1f);

    // Status colors
    private static uint StatusGreen => Col(0.2f, 0.85f, 0.3f);
    private static uint StatusRed => Col(0.85f, 0.2f, 0.2f);
    private static uint StatusDim => Col(0.3f, 0.3f, 0.35f);

    // Tier accent colors (for legend dots)
    private static uint TierBronze => Col(0.80f, 0.50f, 0.20f);
    private static uint TierSilver => Col(0.75f, 0.75f, 0.75f);
    private static uint TierGold => Col(1.0f, 0.84f, 0.0f);

    // Wish tier colors
    private static uint WishTierS => Col(0.133f, 0.773f, 0.369f);
    private static uint WishTierA => Col(0.231f, 0.510f, 0.965f);
    private static uint WishTierB => Col(0.655f, 0.545f, 0.980f);
    private static uint WishTierC => Col(0.961f, 0.620f, 0.043f);
    private static uint WishTierD => Col(0.420f, 0.420f, 0.498f);
    private static uint MirageGold => Col(1.0f, 0.84f, 0.0f);

    private const float Row = 34f;

    // ── Entry point ─────────────────────────────────────────────────

    private Action _refreshKnowledgeBase;
    private Func<string, bool> _isWishFlagged;
    private Action<string> _toggleWishFlag;
    private string _wishSearchFilter = "";

    public void Draw(WhatsAMirageSettings s, MirageUiState state,
        Action refreshKnowledgeBase = null,
        Func<string, bool> isWishFlagged = null,
        Action<string> toggleWishFlag = null)
    {
        _refreshKnowledgeBase = refreshKnowledgeBase;
        _isWishFlagged = isWishFlagged;
        _toggleWishFlag = toggleWishFlag;
        var contentMin = ImGui.GetCursorScreenPos();
        float contentW = ImGui.GetContentRegionAvail().X;
        var dl = ImGui.GetWindowDrawList();

        // Tab bar
        float tabH = 26f;
        float tabW = contentW / Tabs.Length;

        for (int i = 0; i < Tabs.Length; i++)
        {
            var tMin = new Vector2(contentMin.X + i * tabW, contentMin.Y);
            var tMax = new Vector2(contentMin.X + (i + 1) * tabW, contentMin.Y + tabH);
            bool active = i == _activeTab;

            if (active)
                dl.AddRectFilled(tMin, tMax, WithAlpha(Accent, 0.12f));

            ImGui.SetCursorScreenPos(tMin);
            ImGui.InvisibleButton($"##mr_tab_{i}", tMax - tMin);
            bool hov = ImGui.IsItemHovered();
            if (ImGui.IsItemClicked()) _activeTab = i;

            if (hov && !active)
                dl.AddRectFilled(tMin, tMax, WithAlpha(Accent, 0.06f));
            if (active)
                dl.AddLine(tMin with { Y = tMax.Y - 1 } + new Vector2(3, 0),
                    tMax - new Vector2(3, 1), Accent, 2f);

            uint tc = active ? Accent : (hov ? AccentDim : TabOff);
            CenterText(dl, Tabs[i], (tMin + tMax) * 0.5f, tc);
        }

        float sepY = contentMin.Y + tabH + 1;
        dl.AddLine(new Vector2(contentMin.X, sepY),
            new Vector2(contentMin.X + contentW, sepY), WithAlpha(Accent, 0.10f), 1f);

        // ── Status bar ──────────────────────────────────────────────
        float statusY = sepY + 4;
        DrawStatusBar(dl, contentMin.X, contentW, statusY, state);
        float statusH = 22f;

        // Content area
        ImGui.SetCursorScreenPos(new Vector2(contentMin.X, sepY + statusH + 6));
        var avail = new Vector2(contentW, ImGui.GetContentRegionAvail().Y);
        ImGui.BeginChild("##mr_content", avail, ImGuiChildFlags.None, ImGuiWindowFlags.None);

        var cdl = ImGui.GetWindowDrawList();
        var cMin = ImGui.GetWindowPos();
        var cSz = ImGui.GetWindowSize();

        cdl.AddRectFilled(cMin, cMin + cSz, CardBg, 3f);
        float pulse = (float)(0.4 + 0.3 * Math.Sin(ImGui.GetTime() * 1.8));
        cdl.AddRect(cMin, cMin + cSz, WithAlpha(Accent, pulse * 0.18f), 3f, ImDrawFlags.None, 1f);

        float scrollY = ImGui.GetScrollY();
        float y = cMin.Y + 10 - scrollY;
        float x = cMin.X + 12;
        float cx = cMin.X + cSz.X * 0.50f;
        float sw = cSz.X * 0.40f;

        switch (_activeTab)
        {
            case 0: TabSpawners(cdl, s.Spawners, x, cx, ref y, sw, state); break;
            case 1: TabChests(cdl, s.Chests, x, cx, ref y, sw, state); break;
            case 2: TabMonsters(cdl, s.Monsters, x, cx, ref y, sw, state); break;
            case 3: TabPaths(cdl, s.ChainAnchors, x, cx, ref y, sw, state); break;
            case 4: TabArrow(cdl, s.Arrow, s.General, x, cx, ref y, sw, cSz.X); break;
            case 5: TabWishes(cdl, s, x, cx, ref y, sw, cSz.X, state); break;
        }

        ImGui.SetCursorScreenPos(new Vector2(x, y));
        ImGui.Dummy(new Vector2(1, 4));
        ImGui.EndChild();
    }

    // ── Status bar ──────────────────────────────────────────────────

    private static void DrawStatusBar(ImDrawListPtr dl, float startX, float width, float y, MirageUiState state)
    {
        var items = new (string label, int count)[]
        {
            ("Spawners", state.SpawnerCount),
            ("Chests", state.ChestCount),
            ("Monsters", state.MonsterCount),
            ("Paths", state.AnchorPathCount),
        };

        float x = startX + 12;
        foreach (var (label, count) in items)
        {
            uint dotCol = count > 0 ? Accent : StatusDim;
            dl.AddCircleFilled(new Vector2(x + 4, y + 8), 4f, dotCol);
            string text = $"{count} {label}";
            uint textCol = count > 0 ? Label : StatusDim;
            dl.AddText(new Vector2(x + 12, y + 1), textCol, text);
            x += ImGui.CalcTextSize(text).X + 26;
        }
    }

    // ── Tabs ────────────────────────────────────────────────────────

    private void TabSpawners(ImDrawListPtr dl, SpawnerSettings s,
        float x, float cx, ref float y, float sw, MirageUiState state)
    {
        // Info block
        DrawInfoBlock(dl, x, ref y,
            "How Spawners Work",
            "Zarokh spawners appear as glowing pillars scattered through the area.\n" +
            "Walk near one to trigger it - spawning a wave of Mirage monsters.");
        DrawStatusLine(dl, x, ref y, state.SpawnerCount, "untriggered spawner", "in range");

        SectionHeader(dl, x, ref y, "Visibility");
        Toggle("mr_sp_show", s.Show, dl, x, cx, ref y,
            "Show Spawners", "Render world circles on untriggered Zarokh spawners");
        Toggle("mr_sp_mmap", s.MinimapIcon, dl, x, cx, ref y,
            "Minimap Icon", "Show spawner dots on the minimap");

        SectionHeader(dl, x, ref y, "Appearance");
        ColorPicker("mr_sp_col", s.WorldCircleColor, dl, x, cx, ref y,
            "Circle Color", "Color of the world circle around spawners");
        IntSlider("mr_sp_rad", s.WorldCircleRadius, dl, x, cx, ref y, sw,
            "Circle Radius", "World circle radius in units");
        IntSlider("mr_sp_msz", s.MinimapIconSize, dl, x, cx, ref y, sw,
            "Minimap Size", "Size of minimap dot in pixels");
    }

    private void TabChests(ImDrawListPtr dl, ChestSettings s,
        float x, float cx, ref float y, float sw, MirageUiState state)
    {
        // Tier legend
        SectionHeader(dl, x, ref y, "Tier Legend");
        DrawTierLegendItem(dl, x, ref y, TierBronze, "Bronze", "Common rewards - currency shards, small drops");
        DrawTierLegendItem(dl, x, ref y, TierSilver, "Silver", "Uncommon rewards - mid-tier currency, scarabs");
        DrawTierLegendItem(dl, x, ref y, TierGold, "Gold", "Rare rewards - divines, high-value uniques");
        DrawStatusLine(dl, x, ref y, state.ChestCount, "unopened chest", "nearby");

        SectionHeader(dl, x, ref y, "Visibility");
        Toggle("mr_ch_show", s.Show, dl, x, cx, ref y,
            "Show Chests", "Render world circles on unopened Mirage chests");
        Toggle("mr_ch_mmap", s.MinimapIcon, dl, x, cx, ref y,
            "Minimap Icon", "Show chest dots on the minimap");
        Toggle("mr_ch_tier", s.ShowTierLabel, dl, x, cx, ref y,
            "Tier Labels", "Display Bronze/Silver/Gold text above chests");

        SectionHeader(dl, x, ref y, "Tier Colors");
        ColorPicker("mr_ch_bz", s.BronzeColor, dl, x, cx, ref y,
            "Bronze", "Color for bronze-tier chests");
        ColorPicker("mr_ch_sv", s.SilverColor, dl, x, cx, ref y,
            "Silver", "Color for silver-tier chests");
        ColorPicker("mr_ch_gd", s.GoldColor, dl, x, cx, ref y,
            "Gold", "Color for gold-tier chests");

        SectionHeader(dl, x, ref y, "Size");
        IntSlider("mr_ch_rad", s.WorldCircleRadius, dl, x, cx, ref y, sw,
            "Circle Radius", "World circle radius for chests");
    }

    private void TabMonsters(ImDrawListPtr dl, MonsterSettings s,
        float x, float cx, ref float y, float sw, MirageUiState state)
    {
        // Color legend
        SectionHeader(dl, x, ref y, "Monster Types");
        DrawTierLegendItem(dl, x, ref y, Col(1f, 1f, 0f), "Normal", "Standard Mirage league monsters");
        DrawTierLegendItem(dl, x, ref y, ColFromNode(s.RareLeaderColor), "Rare Leader", "Pack leaders - shows nearby pack count");
        DrawTierLegendItem(dl, x, ref y, ColFromNode(s.InvulnerableColor), "Invulnerable", "Cannot be damaged - wait for shield to drop");
        DrawStatusLine(dl, x, ref y, state.MonsterCount, "alive monster", "tracked");

        SectionHeader(dl, x, ref y, "Visibility");
        Toggle("mr_mo_show", s.Show, dl, x, cx, ref y,
            "Show Monsters", "Render world circles on Mirage league monsters");
        Toggle("mr_mo_mmap", s.MinimapIcon, dl, x, cx, ref y,
            "Minimap Icon", "Show monster dots on the minimap");
        Toggle("mr_mo_pack", s.PackCountOverlay, dl, x, cx, ref y,
            "Pack Count", "Show nearby pack count on rare leaders");

        SectionHeader(dl, x, ref y, "Colors");
        ColorPicker("mr_mo_rare", s.RareLeaderColor, dl, x, cx, ref y,
            "Rare Leader", "Color for rare monster leaders");
        ColorPicker("mr_mo_inv", s.InvulnerableColor, dl, x, cx, ref y,
            "Invulnerable", "Color for invulnerable monsters");
    }

    private void TabPaths(ImDrawListPtr dl, ChainAnchorSettings s,
        float x, float cx, ref float y, float sw, MirageUiState state)
    {
        // Radar bridge status
        SectionHeader(dl, x, ref y, "Radar Bridge");
        DrawBridgeStatus(dl, x, ref y, state.RadarAvailable);
        DrawAnchorStatus(dl, x, ref y, state.AnchorCount, state.AnchorPathCount);

        DrawInfoBlock(dl, x, ref y,
            "Chain Anchors",
            "Astral chain anchors mark objectives in the Mirage encounter.\n" +
            "When Radar is connected, full pathfinding routes are drawn.\n" +
            "Without Radar, a fallback directional arrow points the way.");

        SectionHeader(dl, x, ref y, "Visibility");
        Toggle("mr_pa_show", s.Show, dl, x, cx, ref y,
            "Show Paths", "Render chain anchor path lines via Radar bridge");
        Toggle("mr_pa_mmap", s.ShowOnMinimap, dl, x, cx, ref y,
            "Minimap Path", "Draw path dots on the minimap");
        Toggle("mr_pa_wc", s.ShowWorldCircle, dl, x, cx, ref y,
            "World Circle", "Draw circle at chain anchor position");
        Toggle("mr_pa_fb", s.FallbackArrow, dl, x, cx, ref y,
            "Fallback Arrow", "Show directional arrow when Radar is unavailable");

        SectionHeader(dl, x, ref y, "Appearance");
        ColorPicker("mr_pa_col", s.PathColor, dl, x, cx, ref y,
            "Path Color", "Color of path lines and minimap dots");
        IntSlider("mr_pa_th", s.PathThickness, dl, x, cx, ref y, sw,
            "Thickness", "Path line thickness in pixels");
        IntSlider("mr_pa_nth", s.DrawEveryNthSegment, dl, x, cx, ref y, sw,
            "Draw Every Nth", "Skip segments for performance (1 = all)");
        IntSlider("mr_pa_rad", s.WorldCircleRadius, dl, x, cx, ref y, sw,
            "Circle Radius", "World circle radius at anchor position");
    }

    private void TabArrow(ImDrawListPtr dl, ArrowSettings a, GeneralSettings g,
        float x, float cx, ref float y, float sw, float cardW)
    {
        // Arrow preview
        SectionHeader(dl, x, ref y, "Arrow Preview");
        DrawArrowPreview(dl, x, ref y, a, cardW);

        SectionHeader(dl, x, ref y, "Direction Arrow");
        Toggle("mr_ar_show", a.Show, dl, x, cx, ref y,
            "Show Arrow", "Compass arrow pointing to nearest untriggered spawner");
        ColorPicker("mr_ar_col", a.Color, dl, x, cx, ref y,
            "Arrow Color", "Color of the directional arrow");
        IntSlider("mr_ar_sz", a.Size, dl, x, cx, ref y, sw,
            "Arrow Size", "Size of the arrow triangle in pixels");
        IntSlider("mr_ar_dist", a.DistanceFromCenter, dl, x, cx, ref y, sw,
            "Distance", "Arrow distance from player screen position");
        IntSlider("mr_ar_rng", a.MaxRange, dl, x, cx, ref y, sw,
            "Max Range", "Hide arrow when spawner is beyond this grid distance");

        SectionHeader(dl, x, ref y, "General");
        IntSlider("mr_ge_mdd", g.MaxDrawDistance, dl, x, cx, ref y, sw,
            "Max Draw Distance", "Hide all overlays beyond this grid distance");
    }

    private void TabWishes(ImDrawListPtr dl, WhatsAMirageSettings s,
        float x, float cx, ref float y, float sw, float cardW, MirageUiState state)
    {
        // Zone status banner
        SectionHeader(dl, x, ref y, "Zone Status");
        {
            var bannerMin = new Vector2(x + 2, y);
            var bannerMax = new Vector2(x + cardW - 24, y + 24);
            if (state.IsMirageZone)
            {
                dl.AddRectFilled(bannerMin, bannerMax, WithAlpha(MirageGold, 0.15f), 4f);
                dl.AddRect(bannerMin, bannerMax, WithAlpha(MirageGold, 0.4f), 4f, ImDrawFlags.None, 1f);
                CenterText(dl, "MIRAGE ZONE", (bannerMin + bannerMax) * 0.5f, MirageGold);
            }
            else
            {
                dl.AddRectFilled(bannerMin, bannerMax, WithAlpha(StatusDim, 0.10f), 4f);
                dl.AddRect(bannerMin, bannerMax, WithAlpha(StatusDim, 0.3f), 4f, ImDrawFlags.None, 1f);
                CenterText(dl, "No Mirage", (bannerMin + bannerMax) * 0.5f, StatusDim);
            }
            y += 30f;
        }

        // Varashta status
        SectionHeader(dl, x, ref y, "Varashta");
        {
            var v = state.Varashta;
            if (v?.Entity?.IsValid == true)
            {
                uint col;
                string status;
                if (v.IsReady)      { col = StatusGreen; status = "Ready - Wish Available"; }
                else if (v.IsSpent) { col = StatusDim;   status = "Spent"; }
                else                { col = Accent;      status = $"emerge={v.EmergeState}"; }

                dl.AddCircleFilled(new Vector2(x + 8, y + 7), 5f, col);
                dl.AddText(new Vector2(x + 20, y), Label, status);
                y += 18f;

                var playerPos = ImGui.GetIO().MousePos; // distance calculated elsewhere
                dl.AddText(new Vector2(x + 20, y), Desc, "Varashta, the Winter Sekhema");
                y += 18f;
            }
            else
            {
                dl.AddCircleFilled(new Vector2(x + 8, y + 7), 5f, StatusDim);
                dl.AddText(new Vector2(x + 20, y), StatusDim, "Not Found");
                y += 18f;
            }
        }

        // Wish alert toggle
        SectionHeader(dl, x, ref y, "Wish Alert");
        Toggle("mr_wa_show", s.WishAlert.ShowAlert, dl, x, cx, ref y,
            "Show Alert", "Display 'Wish Available' on screen when Varashta is ready");
        ColorPicker("mr_wa_col", s.WishAlert.AlertColor, dl, x, cx, ref y,
            "Alert Color", "Color of the on-screen wish alert text");
        Toggle("mr_wa_tier", s.WishAlert.ShowWishTierOverlay, dl, x, cx, ref y,
            "Show Tier Badge", "Display tier indicator on wish selection cards");
        Toggle("mr_wa_tip", s.WishAlert.ShowWishTooltip, dl, x, cx, ref y,
            "Show Tooltip", "Show details on hover over wish cards");

        SectionHeader(dl, x, ref y, "Flagged Wishes");
        Toggle("mr_wa_flag", s.WishAlert.HighlightFlaggedWishes, dl, x, cx, ref y,
            "Highlight Flagged", "Pulsing glow + PICK label on flagged wishes in-game");
        ColorPicker("mr_wa_fcol", s.WishAlert.FlaggedWishColor, dl, x, cx, ref y,
            "Flag Color", "Highlight color for flagged wishes");

        // Quick legend
        SectionHeader(dl, x, ref y, "Quick Legend");
        DrawTierLegendItem(dl, x, ref y, WishTierS, "S - Always take", "Highest value wishes");
        DrawTierLegendItem(dl, x, ref y, WishTierA, "A - Strong pick", "Great in most builds");
        DrawTierLegendItem(dl, x, ref y, WishTierB, "B - Situational", "Build/strategy dependent");
        DrawTierLegendItem(dl, x, ref y, WishTierC, "C - Chain Break", "Fixed reward from chain completion");
        DrawTierLegendItem(dl, x, ref y, WishTierD, "D - Skip", "No meaningful value");
        DrawTierLegendItem(dl, x, ref y, MirageGold, "* Flagged", "Your priority picks  - highlighted in-game");
        y += 4f;

        // Wish tier reference list from wish-tiers.json
        // Toolbar: Refresh + Search
        {
            ImGui.SetCursorScreenPos(new Vector2(x + 6, y));
            if (ImGui.Button("Refresh Wish Tiers"))
                _refreshKnowledgeBase?.Invoke();
            HelpMarker("Reload wish tier data from wish-tiers.json. Run /update-wish-tiers to sync from PoEDB.");
            y += 28f;

            // Search filter
            ImGui.SetCursorScreenPos(new Vector2(x + 6, y));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(0.07f, 0.07f, 0.09f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Border, new System.Numerics.Vector4(0.55f, 0.42f, 0.06f, 0.4f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
            ImGui.SetNextItemWidth(cardW - 36);
            ImGui.InputTextWithHint("##mr_wish_search", "Search wishes...", ref _wishSearchFilter, 128);
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);
            y += 28f;
        }

        var kb = state.WishTiers;
        if (kb?.Tiers == null || kb.Tiers.Count == 0)
        {
            dl.AddText(new Vector2(x + 6, y), StatusDim, "Wish tiers not loaded. Click Refresh above.");
            y += 20f;
            return;
        }

        var hasSearch = !string.IsNullOrWhiteSpace(_wishSearchFilter);
        var searchLower = _wishSearchFilter?.ToLowerInvariant() ?? "";

        // Count flagged
        int flaggedCount = 0;
        if (_isWishFlagged != null)
            foreach (var kv in kb.Tiers)
                if (_isWishFlagged(kv.Key)) flaggedCount++;

        if (flaggedCount > 0)
        {
            dl.AddText(new Vector2(x + 6, y), MirageGold, $"* {flaggedCount} wish{(flaggedCount == 1 ? "" : "es")} flagged");
            y += 18f;
        }

        SectionHeader(dl, x, ref y, "Wish Tier Reference");

        // Group by tier  - show flagged first if not searching
        var tierOrder = new[] { "S", "A", "B", "C", "D", "?" };
        foreach (var tier in tierOrder)
        {
            var wishes = new List<KeyValuePair<string, WishTierEntry>>();
            foreach (var kv in kb.Tiers)
            {
                if (kv.Value.Tier != tier) continue;
                if (hasSearch)
                {
                    var matchName = kv.Key.ToLowerInvariant().Contains(searchLower);
                    var matchEffect = kv.Value.Effect?.ToLowerInvariant().Contains(searchLower) == true;
                    var matchNotes = kv.Value.Notes?.ToLowerInvariant().Contains(searchLower) == true;
                    if (!matchName && !matchEffect && !matchNotes) continue;
                }
                wishes.Add(kv);
            }

            if (wishes.Count == 0) continue;

            // Sort: flagged first within each tier
            if (_isWishFlagged != null)
                wishes.Sort((a, b) =>
                {
                    var af = _isWishFlagged(a.Key) ? 0 : 1;
                    var bf = _isWishFlagged(b.Key) ? 0 : 1;
                    return af.CompareTo(bf);
                });

            uint tierCol = GetWishTierColor(tier);

            // Tier header
            dl.AddRectFilled(new Vector2(x + 2, y), new Vector2(x + cardW - 24, y + 18), WithAlpha(tierCol, 0.08f));
            dl.AddText(new Vector2(x + 8, y + 1), tierCol, $"Tier {tier}  ({wishes.Count})");
            y += 20f;

            foreach (var kv in wishes)
            {
                var wish = kv.Value;
                var isFlagged = _isWishFlagged?.Invoke(kv.Key) == true;
                float rowStartY = y;

                // Flag star button (clickable)
                {
                    var starPos = new Vector2(x + 4, y + 1);
                    ImGui.SetCursorScreenPos(starPos);
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0, 0, 0, 0));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.2f, 0.2f, 0.1f, 0.5f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.3f, 0.3f, 0.1f, 0.5f));
                    if (ImGui.SmallButton($"{(isFlagged ? "*" : "o")}##flag_{kv.Key}"))
                        _toggleWishFlag?.Invoke(kv.Key);
                    ImGui.PopStyleColor(3);
                }

                // Name (shifted right for star)
                dl.AddText(new Vector2(x + 22, y), isFlagged ? MirageGold : Label, kv.Key);
                y += 15f;

                // Effect
                if (!string.IsNullOrEmpty(wish.Effect))
                {
                    dl.AddText(new Vector2(x + 26, y), Desc, wish.Effect);
                    y += 15f;
                }

                // Notes
                if (!string.IsNullOrEmpty(wish.Notes))
                {
                    dl.AddText(new Vector2(x + 26, y), WithAlpha(tierCol, 0.7f), wish.Notes);
                    y += 15f;
                }

                // Flagged row highlight
                if (isFlagged)
                {
                    dl.AddRectFilled(
                        new Vector2(x + 2, rowStartY - 2),
                        new Vector2(x + cardW - 24, y + 2),
                        WithAlpha(MirageGold, 0.06f), 3f);
                    dl.AddRect(
                        new Vector2(x + 2, rowStartY - 2),
                        new Vector2(x + cardW - 24, y + 2),
                        WithAlpha(MirageGold, 0.15f), 3f, ImDrawFlags.None, 1f);
                }

                y += 2f;
            }
            y += 4f;
        }

        // Version info
        y += 4f;
        dl.AddText(new Vector2(x + 6, y), Desc, $"v{kb.Version}  - {kb.LastUpdated}  - {kb.Source ?? "manual"}  ({kb.Tiers.Count} wishes)");
        y += 18f;
    }

    private static uint GetWishTierColor(string tier) => tier switch
    {
        "S" => WishTierS,
        "A" => WishTierA,
        "B" => WishTierB,
        "C" => WishTierC,
        "D" => WishTierD,
        _ => StatusDim
    };

    // ── Domain content helpers ──────────────────────────────────────

    private static void DrawInfoBlock(ImDrawListPtr dl, float x, ref float y, string title, string body)
    {
        float boxX = x + 2;
        float textX = boxX + 10;
        float startY = y;

        // Title
        dl.AddText(new Vector2(textX, y + 2), Accent, title);
        y += 18f;

        // Body lines
        foreach (var line in body.Split('\n'))
        {
            dl.AddText(new Vector2(textX, y), Desc, line);
            y += 15f;
        }
        y += 4f;

        // Left accent bar
        dl.AddRectFilled(new Vector2(boxX, startY), new Vector2(boxX + 3, y), WithAlpha(Accent, 0.4f), 1f);
        y += 4f;
    }

    private static void DrawStatusLine(ImDrawListPtr dl, float x, ref float y, int count, string singular, string suffix)
    {
        uint dotCol = count > 0 ? StatusGreen : StatusDim;
        dl.AddCircleFilled(new Vector2(x + 8, y + 7), 4f, dotCol);
        string noun = count == 1 ? singular : singular + "s";
        string text = $"{count} {noun} {suffix}";
        dl.AddText(new Vector2(x + 18, y), count > 0 ? Label : StatusDim, text);
        y += 20f;
    }

    private static void DrawTierLegendItem(ImDrawListPtr dl, float x, ref float y, uint dotColor, string name, string desc)
    {
        dl.AddCircleFilled(new Vector2(x + 8, y + 7), 5f, dotColor);
        dl.AddText(new Vector2(x + 20, y), Label, name);
        float nameW = ImGui.CalcTextSize(name).X;
        dl.AddText(new Vector2(x + 24 + nameW, y), Desc, $"- {desc}");
        y += 18f;
    }

    private static void DrawBridgeStatus(ImDrawListPtr dl, float x, ref float y, bool available)
    {
        uint dotCol = available ? StatusGreen : StatusRed;
        string text = available ? "Radar connected - full pathfinding active" : "Radar unavailable - fallback arrows only";
        dl.AddCircleFilled(new Vector2(x + 8, y + 7), 5f, dotCol);
        dl.AddText(new Vector2(x + 20, y), available ? Label : Desc, text);
        y += 20f;
    }

    private static void DrawAnchorStatus(ImDrawListPtr dl, float x, ref float y, int anchorCount, int pathCount)
    {
        string text = $"{anchorCount} anchor{(anchorCount == 1 ? "" : "s")} tracked, {pathCount} with active path{(pathCount == 1 ? "" : "s")}";
        uint col = anchorCount > 0 ? Label : StatusDim;
        dl.AddCircleFilled(new Vector2(x + 8, y + 7), 4f, anchorCount > 0 ? Accent : StatusDim);
        dl.AddText(new Vector2(x + 20, y), col, text);
        y += 20f;
    }

    private static void DrawArrowPreview(ImDrawListPtr dl, float x, ref float y, ArrowSettings a, float cardW)
    {
        var c = a.Color.Value;
        uint arrowCol = ImGui.GetColorU32(new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));
        float size = Math.Clamp(a.Size.Value * 0.6f, 12f, 40f);

        // Center the preview arrow in the card
        float centerX = x + (cardW - 24) * 0.5f;
        float centerY = y + 24;

        // Draw upward-pointing arrow (direction = up)
        var direction = new Vector2(0, -1);
        var perpendicular = new Vector2(1, 0);
        var tip = new Vector2(centerX, centerY) + direction * size;
        var baseLeft = new Vector2(centerX, centerY) - direction * (size * 0.5f) + perpendicular * (size * 0.33f);
        var baseRight = new Vector2(centerX, centerY) - direction * (size * 0.5f) - perpendicular * (size * 0.33f);

        // Shadow
        var offset = new Vector2(2, 2);
        dl.AddTriangleFilled(tip + offset, baseLeft + offset, baseRight + offset, WithAlpha(0x000000FF, 0.3f));

        // Arrow
        dl.AddTriangleFilled(tip, baseLeft, baseRight, arrowCol);
        dl.AddTriangle(tip, baseLeft, baseRight, WithAlpha(arrowCol, 0.6f), 1.5f);

        // Label
        string label = $"{a.Size.Value}px";
        var labelSz = ImGui.CalcTextSize(label);
        dl.AddText(new Vector2(centerX - labelSz.X * 0.5f, centerY + size * 0.6f + 4), Desc, label);

        y += size * 2 + 20;
    }

    // ── Widget primitives ───────────────────────────────────────────

    private static void HelpMarker(string desc)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    private void SectionHeader(ImDrawListPtr dl, float x, ref float y, string title)
    {
        y += 4f;
        dl.AddText(new Vector2(x, y), Accent, title);
        y += 18f;
        dl.AddLine(new Vector2(x, y - 4), new Vector2(x + ImGui.CalcTextSize(title).X + 40, y - 4),
            WithAlpha(Accent, 0.25f), 1f);
    }

    private void Toggle(string key, ToggleNode node, ImDrawListPtr dl,
        float x, float cx, ref float y, string label, string desc)
    {
        dl.AddText(new Vector2(x + 6, y + 1), Label, label);
        dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);
        ImGui.SetCursorScreenPos(new Vector2(cx, y + 5));
        var v = node.Value;
        _anims.TryGetValue(key, out float a);
        if (PillToggle($"##{key}", ref v, ref a))
            node.Value = v;
        _anims[key] = a;
        y += Row;
    }

    private void IntSlider(string key, RangeNode<int> node, ImDrawListPtr dl,
        float x, float cx, ref float y, float sw, string label, string desc)
    {
        dl.AddText(new Vector2(x + 6, y + 1), Label, label);
        dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);
        ImGui.SetCursorScreenPos(new Vector2(cx, y + 7));
        var v = node.Value;
        if (SliderInt($"##{key}", ref v, node.Min, node.Max, sw))
            node.Value = v;
        dl.AddText(new Vector2(cx + sw + 6, y + 7), Accent, v.ToString());
        y += Row;
    }

    private void ColorPicker(string key, ColorNode node, ImDrawListPtr dl,
        float x, float cx, ref float y, string label, string desc)
    {
        dl.AddText(new Vector2(x + 6, y + 1), Label, label);
        dl.AddText(new Vector2(x + 6, y + 16), Desc, desc);

        var c = node.Value;
        ImGui.SetCursorScreenPos(new Vector2(cx, y + 3));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.07f, 0.07f, 0.09f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.55f, 0.42f, 0.06f, 0.6f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        var colorVec = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        ImGui.SetNextItemWidth(120);
        if (ImGui.ColorEdit4($"##{key}", ref colorVec,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel))
        {
            node.Value = new SharpDX.Color(
                (int)(colorVec.X * 255), (int)(colorVec.Y * 255),
                (int)(colorVec.Z * 255), (int)(colorVec.W * 255));
        }
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);

        y += Row;
    }

    // ── Self-contained drawing helpers ──────────────────────────────

    private static bool PillToggle(string id, ref bool value, ref float animState)
    {
        var dl = ImGui.GetWindowDrawList();
        var cur = ImGui.GetCursorScreenPos();
        const float w = 40f, h = 20f, r = h * 0.5f;

        ImGui.InvisibleButton(id, new Vector2(w, h));
        bool changed = ImGui.IsItemClicked();
        if (changed) value = !value;

        float dt = ImGui.GetIO().DeltaTime;
        float target = value ? 1f : 0f;
        animState = Math.Clamp(animState + (target - animState) * Math.Min(dt * 10f, 1f), 0f, 1f);

        uint trackOff = ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1f));
        uint trackColor = LerpCol(trackOff, Accent, animState);
        dl.AddRectFilled(cur, cur + new Vector2(w, h), trackColor, r);

        float knobX = cur.X + r + animState * (w - h);
        uint knobCol = ImGui.GetColorU32(new Vector4(
            0.5f + 0.5f * animState, 0.5f + 0.5f * animState,
            0.5f + 0.5f * animState, 1f));
        dl.AddCircleFilled(new Vector2(knobX, cur.Y + r), r - 2f, knobCol);

        return changed;
    }

    private static bool SliderInt(string id, ref int value, int min, int max, float width)
    {
        var dl = ImGui.GetWindowDrawList();
        var cur = ImGui.GetCursorScreenPos();
        const float h = 16f, th = 4f, tr = 7f;

        ImGui.InvisibleButton(id, new Vector2(width, h));
        bool changed = false;
        if (ImGui.IsItemActive())
        {
            float frac = Math.Clamp((ImGui.GetMousePos().X - cur.X) / width, 0f, 1f);
            int nv = min + (int)(frac * (max - min));
            if (nv != value) { value = nv; changed = true; }
        }

        float trackY = cur.Y + (h - th) * 0.5f;
        dl.AddRectFilled(cur with { Y = trackY }, new Vector2(cur.X + width, trackY + th),
            ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 1f)), 2f);

        float f = (max > min) ? (float)(value - min) / (max - min) : 0f;
        float fw = f * width;
        dl.AddRectFilled(cur with { Y = trackY }, new Vector2(cur.X + fw, trackY + th), Accent, 2f);

        float tx = cur.X + fw, ty = cur.Y + h * 0.5f;
        dl.AddCircleFilled(new Vector2(tx, ty), tr, Accent);
        dl.AddCircleFilled(new Vector2(tx, ty), tr - 2f,
            ImGui.GetColorU32(new Vector4(0.95f, 0.95f, 0.95f, 1f)));

        return changed;
    }

    // ── Color utilities ─────────────────────────────────────────────

    private static uint Col(float r, float g, float b, float a = 1f)
        => ImGui.GetColorU32(new Vector4(r, g, b, a));

    private static uint ColFromNode(ColorNode node)
    {
        var c = node.Value;
        return ImGui.GetColorU32(new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));
    }

    private static uint WithAlpha(uint color, float alpha)
    {
        var v = ImGui.ColorConvertU32ToFloat4(color);
        v.W = alpha;
        return ImGui.GetColorU32(v);
    }

    private static uint LerpCol(uint a, uint b, float t)
    {
        var va = ImGui.ColorConvertU32ToFloat4(a);
        var vb = ImGui.ColorConvertU32ToFloat4(b);
        return ImGui.GetColorU32(new Vector4(
            va.X + (vb.X - va.X) * t, va.Y + (vb.Y - va.Y) * t,
            va.Z + (vb.Z - va.Z) * t, va.W + (vb.W - va.W) * t));
    }

    private static void CenterText(ImDrawListPtr dl, string text, Vector2 center, uint color)
    {
        var sz = ImGui.CalcTextSize(text);
        dl.AddText(center - sz * 0.5f, color, text);
    }
}
