#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
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
    VarashtaData? Varashta,
    WishTierFile? WishTiers
);

/// <summary>
/// Settings panel: the generated SettingsMenu (see SettingsMenu.cs) draws every setting from its [Menu] label and
/// tooltip; tabs come from the [Submenu] properties and [MenuTab] (Spawners / Chests / Monsters / Paths / Arrow /
/// Wishes). Hand-drawn on top: the live count strip above the tabs, per tab an explanation, live status lines and
/// colour legends, the Radar bridge status, the arrow preview, the zone/Varashta status on Wishes, and a Wish Tiers
/// tab with the searchable tier reference and click-to-flag rows. Amber/gold (#D4A017) accent.
/// </summary>
public sealed class MirageSettingsUi
{
    private readonly SettingsMenu _menu;
    private WhatsAMirageSettings? _settings;
    private MirageUiState? _state;
    private Action? _refreshKnowledgeBase;
    private Func<string, bool>? _isWishFlagged;
    private Action<string>? _toggleWishFlag;
    private string _wishSearchFilter = "";
    private readonly List<KeyValuePair<string, WishTierEntry>> _wishRows = new();

    // ---- palette ----
    private static readonly Vector4 Accent = new(0.831f, 0.627f, 0.090f, 1f);
    private static readonly Vector4 StatusGreen = new(0.2f, 0.85f, 0.3f, 1f);
    private static readonly Vector4 StatusRed = new(0.85f, 0.2f, 0.2f, 1f);
    private static readonly Vector4 StatusDim = new(0.3f, 0.3f, 0.35f, 1f);
    private static readonly Vector4 MirageGold = new(1.0f, 0.84f, 0.0f, 1f);
    private static readonly Vector4 NormalMonster = new(1f, 1f, 0f, 1f);
    private static readonly Vector4 WishTierS = new(0.133f, 0.773f, 0.369f, 1f);
    private static readonly Vector4 WishTierA = new(0.231f, 0.510f, 0.965f, 1f);
    private static readonly Vector4 WishTierB = new(0.655f, 0.545f, 0.980f, 1f);
    private static readonly Vector4 WishTierC = new(0.961f, 0.620f, 0.043f, 1f);
    private static readonly Vector4 WishTierD = new(0.420f, 0.420f, 0.498f, 1f);
    private static readonly string[] TierOrder = { "S", "A", "B", "C", "D", "?" };

    public MirageSettingsUi()
    {
        _menu = new SettingsMenu { Accent = Accent, RootTabName = "Spawners" }
            .Banner(DrawStatusBar)
            .Tab("Spawners", DrawSpawnersIntro, first: true)
            .Tab("Chests", DrawChestsIntro, first: true)
            .Tab("Monsters", DrawMonstersIntro, first: true)
            .Tab("Paths", DrawPathsIntro, first: true)
            .Tab("Arrow", DrawArrowPreview, first: true)
            .Tab("Wishes", DrawWishStatus, first: true)
            .Tab("Wish Tiers", DrawWishReference);
    }

    public void Draw(WhatsAMirageSettings s, MirageUiState state,
        Action? refreshKnowledgeBase = null,
        Func<string, bool>? isWishFlagged = null,
        Action<string>? toggleWishFlag = null)
    {
        _settings = s;
        _state = state;
        _refreshKnowledgeBase = refreshKnowledgeBase;
        _isWishFlagged = isWishFlagged;
        _toggleWishFlag = toggleWishFlag;
        _menu.Draw(s);
    }

    // ---- banner ----

    private void DrawStatusBar()
    {
        var st = _state;
        if (st == null) return;
        CountDot(st.SpawnerCount, "Spawners", first: true);
        CountDot(st.ChestCount, "Chests");
        CountDot(st.MonsterCount, "Monsters");
        CountDot(st.AnchorPathCount, "Paths");
        if (st.IsMirageZone)
        {
            ImGui.SameLine(0, 18f);
            _menu.Badge("Mirage zone", MirageGold);
        }
        ImGui.Spacing();
    }

    private static void CountDot(int count, string label, bool first = false)
    {
        if (!first) ImGui.SameLine(0, 18f);
        Dot(count > 0 ? Accent : StatusDim, $"{count} {label}", count > 0 ? TextCol : StatusDim);
    }

    // ---- tab intros ----

    private void DrawSpawnersIntro()
    {
        InfoBlock("How Spawners Work",
            "Zarokh spawners appear as glowing pillars scattered through the area. Walk near one to trigger it, "
            + "spawning a wave of Mirage monsters.");
        if (_state != null) StatusLine(_state.SpawnerCount, "untriggered spawner", "in range");
        ImGui.Spacing();
    }

    private void DrawChestsIntro()
    {
        var c = _settings?.Chests;
        if (c != null)
        {
            Legend(ToVec(c.BronzeColor.Value), "Bronze", "Common rewards - currency shards, small drops");
            Legend(ToVec(c.SilverColor.Value), "Silver", "Uncommon rewards - mid-tier currency, scarabs");
            Legend(ToVec(c.GoldColor.Value), "Gold", "Rare rewards - divines, high-value uniques");
        }
        if (_state != null) StatusLine(_state.ChestCount, "unopened chest", "nearby");
        ImGui.Spacing();
    }

    private void DrawMonstersIntro()
    {
        var m = _settings?.Monsters;
        Legend(NormalMonster, "Normal", "Standard Mirage league monsters");
        if (m != null)
        {
            Legend(ToVec(m.RareLeaderColor.Value), "Rare Leader", "Pack leaders - shows nearby pack count");
            Legend(ToVec(m.InvulnerableColor.Value), "Invulnerable", "Cannot be damaged - wait for the shield to drop");
        }
        if (_state != null) StatusLine(_state.MonsterCount, "alive monster", "tracked");
        ImGui.Spacing();
    }

    private void DrawPathsIntro()
    {
        var st = _state;
        if (st != null)
        {
            Dot(st.RadarAvailable ? StatusGreen : StatusRed,
                st.RadarAvailable ? "Radar connected - full pathfinding active" : "Radar unavailable - fallback arrows only",
                st.RadarAvailable ? TextCol : DimCol);
            Dot(st.AnchorCount > 0 ? Accent : StatusDim,
                $"{st.AnchorCount} anchor{(st.AnchorCount == 1 ? "" : "s")} tracked, {st.AnchorPathCount} with active path{(st.AnchorPathCount == 1 ? "" : "s")}",
                st.AnchorCount > 0 ? TextCol : StatusDim);
            ImGui.Spacing();
        }
        InfoBlock("Chain Anchors",
            "Astral chain anchors mark objectives in the Mirage encounter. When Radar is connected, full pathfinding "
            + "routes are drawn; without it, a fallback arrow points the way.");
    }

    private void DrawArrowPreview()
    {
        var a = _settings?.Arrow;
        if (a == null) return;
        var dl = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        float w = ImGui.GetContentRegionAvail().X;
        var c = a.Color.Value;
        var col = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        float size = Math.Clamp(a.Size.Value * 0.6f, 12f, 40f);
        var center = new Vector2(origin.X + w * 0.5f, origin.Y + size + 4f);
        var tip = center + new Vector2(0, -size);
        var baseLeft = center + new Vector2(size * 0.33f, size * 0.5f);
        var baseRight = center + new Vector2(-size * 0.33f, size * 0.5f);
        var shadow = new Vector2(2, 2);
        dl.AddTriangleFilled(tip + shadow, baseLeft + shadow, baseRight + shadow, ImGui.GetColorU32(new Vector4(0, 0, 0, 0.3f)));
        dl.AddTriangleFilled(tip, baseLeft, baseRight, ImGui.GetColorU32(col));
        dl.AddTriangle(tip, baseLeft, baseRight, ImGui.GetColorU32(col with { W = col.W * 0.6f }), 1.5f);
        var label = $"Preview: {a.Size.Value}px";
        var ls = ImGui.CalcTextSize(label);
        dl.AddText(new Vector2(center.X - ls.X * 0.5f, center.Y + size * 0.5f + 6f), ImGui.GetColorU32(DimCol), label);
        ImGui.Dummy(new Vector2(1f, size * 2f + ls.Y + 14f));
    }

    private void DrawWishStatus()
    {
        var st = _state;
        if (st == null) return;
        PhaseBanner(st.IsMirageZone ? "MIRAGE ZONE" : "No Mirage", st.IsMirageZone ? MirageGold : StatusDim);
        var v = st.Varashta;
        if (v?.Entity?.IsValid == true)
        {
            var (col, status) = v.IsReady ? (StatusGreen, "Varashta: Ready - Wish Available")
                : v.IsSpent ? (StatusDim, "Varashta: Spent")
                : (Accent, $"Varashta: emerge={v.EmergeState}");
            Dot(col, status, TextCol);
        }
        else Dot(StatusDim, "Varashta: Not Found", StatusDim);
        ImGui.Spacing();
    }

    // ---- wish tier reference: its own tab (a long list), searchable, click a row to flag it ----

    private void DrawWishReference()
    {
        _menu.SectionHeader("Wish Tier Reference", "Tier ratings from data/wish-tiers.json; click a wish to flag it");
        Legend(WishTierS, "S - Always take", "Highest value wishes");
        Legend(WishTierA, "A - Strong pick", "Great in most builds");
        Legend(WishTierB, "B - Situational", "Build/strategy dependent");
        Legend(WishTierC, "C - Chain Break", "Fixed reward from chain completion");
        Legend(WishTierD, "D - Skip", "No meaningful value");
        StarLegend();
        ImGui.Spacing();

        if (_menu.AccentButton("Refresh Wish Tiers")) _refreshKnowledgeBase?.Invoke();
        if (ImGui.IsItemHovered()) SettingsMenu.Tooltip("Reload wish tier data from wish-tiers.json. Run /update-wish-tiers to sync from PoEDB.");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##mr_wish_search", "Search wishes...", ref _wishSearchFilter, 128);

        var kb = _state?.WishTiers;
        if (kb?.Tiers == null || kb.Tiers.Count == 0)
        {
            SettingsMenu.Text(StatusDim, kb == null
                ? "Wish tiers not loaded (data/wish-tiers.json missing or unreadable). Click Refresh above."
                : "Wish tiers loaded but empty: data/wish-tiers.json has no \"tiers\" entries.");
            return;
        }

        var search = _wishSearchFilter.Trim();
        int flagged = 0;
        if (_isWishFlagged != null)
            foreach (var kv in kb.Tiers)
                if (_isWishFlagged(kv.Key)) flagged++;
        if (flagged > 0) StarLine(MirageGold, $"{flagged} wish{(flagged == 1 ? "" : "es")} flagged", MirageGold);

        var dl = ImGui.GetWindowDrawList();
        foreach (var tier in TierOrder)
        {
            // flagged first, then the rest, each in file order (two passes: stable, so flagging never reshuffles a tier)
            _wishRows.Clear();
            for (int pass = 0; pass < 2; pass++)
                foreach (var kv in kb.Tiers)
                {
                    if (TierKey(kv.Value) != tier) continue;
                    bool flaggedRow = _isWishFlagged?.Invoke(kv.Key) == true;
                    if (flaggedRow != (pass == 0)) continue;
                    if (search.Length > 0
                        && !kv.Key.Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !(kv.Value?.Effect?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                        && !(kv.Value?.Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        continue;
                    _wishRows.Add(kv);
                }
            if (_wishRows.Count == 0) continue;

            var tierCol = TierColor(tier);
            ImGui.Spacing();
            var hMin = ImGui.GetCursorScreenPos();
            float rowW = ImGui.GetContentRegionAvail().X;
            dl.AddRectFilled(hMin, hMin + new Vector2(rowW, ImGui.GetTextLineHeight() + 2f), ImGui.GetColorU32(tierCol with { W = 0.08f }));
            ImGui.Indent(6f);
            SettingsMenu.Text(tierCol, tier == "?"
                ? $"Unrated / unknown tier  ({_wishRows.Count})"
                : $"Tier {tier}  ({_wishRows.Count})");
            ImGui.Unindent(6f);

            foreach (var kv in _wishRows) DrawWishRow(dl, kv.Key, kv.Value, tierCol, rowW);
        }
        ImGui.Spacing();
        SettingsMenu.Dim($"v{kb.Version}  - {kb.LastUpdated}  - {kb.Source ?? "manual"}  ({kb.Tiers.Count} wishes)");
    }

    private void DrawWishRow(ImDrawListPtr dl, string name, WishTierEntry wish, Vector4 tierCol, float rowW)
    {
        bool isFlagged = _isWishFlagged?.Invoke(name) == true;
        var rowMin = ImGui.GetCursorScreenPos();
        float lineH = ImGui.GetTextLineHeight();
        Star(dl, new Vector2(rowMin.X + 10f, rowMin.Y + lineH * 0.5f), 5f,
            isFlagged ? MirageGold : TextCol with { W = 0.25f }, isFlagged);
        ImGui.Indent(22f);
        SettingsMenu.Text(isFlagged ? MirageGold : TextCol, name);
        ImGui.PushTextWrapPos(0f);   // wrap at the content edge (left of the scrollbar)
        if (!string.IsNullOrEmpty(wish?.Effect)) SettingsMenu.Dim(wish.Effect);
        if (!string.IsNullOrEmpty(wish?.Notes)) SettingsMenu.Text(tierCol with { W = 0.7f }, wish.Notes);
        ImGui.PopTextWrapPos();
        ImGui.Unindent(22f);
        var after = ImGui.GetCursorScreenPos();
        var rowMax = new Vector2(rowMin.X + rowW, after.Y);

        // one click target over the whole row, laid over the text just drawn
        ImGui.SetCursorScreenPos(rowMin);
        ImGui.PushID(name);
        // InvisibleButton returns true on release over the row, so a press that turns into a scroll drag flips nothing
        bool clicked = ImGui.InvisibleButton("##wish", new Vector2(rowW, Math.Max(1f, rowMax.Y - rowMin.Y)));
        ImGui.PopID();
        if (ImGui.IsItemHovered())
        {
            dl.AddRectFilled(rowMin, rowMax, ImGui.GetColorU32(Accent with { W = 0.08f }), 3f);
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            SettingsMenu.Tooltip(isFlagged ? "Click to remove from your watchlist" : "Click to flag: highlighted with PICK in game");
        }
        if (clicked) _toggleWishFlag?.Invoke(name);
        if (isFlagged)
        {
            dl.AddRectFilled(rowMin, rowMax, ImGui.GetColorU32(MirageGold with { W = 0.06f }), 3f);
            dl.AddRect(rowMin, rowMax, ImGui.GetColorU32(MirageGold with { W = 0.15f }), 3f);
        }
        ImGui.SetCursorScreenPos(after);
        ImGui.Dummy(new Vector2(1f, 2f));
    }

    /// <summary>S/A/B/C/D (any case, trimmed); anything else, missing or empty lands in "?" so no wish is dropped.</summary>
    private static string TierKey(WishTierEntry? e)
    {
        var t = e?.Tier?.Trim().ToUpperInvariant();
        return t is "S" or "A" or "B" or "C" or "D" ? t : "?";
    }

    private static Vector4 TierColor(string tier) => tier switch
    {
        "S" => WishTierS,
        "A" => WishTierA,
        "B" => WishTierB,
        "C" => WishTierC,
        "D" => WishTierD,
        _ => StatusDim,
    };

    // ---- small cursor-flow widgets ----

    private static Vector4 TextCol => ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
    private static Vector4 DimCol => ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];

    private static void Dot(Vector4 dot, string text, Vector4 textColor)
    {
        var p = ImGui.GetCursorScreenPos();
        float h = ImGui.GetTextLineHeight();
        ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(p.X + 4f, p.Y + h * 0.5f), 4f, ImGui.GetColorU32(dot));
        ImGui.Dummy(new Vector2(10f, h));
        ImGui.SameLine(0, 4f);
        SettingsMenu.Text(textColor, text);
    }

    private static void StatusLine(int count, string singular, string suffix)
    {
        string noun = count == 1 ? singular : singular + "s";
        Dot(count > 0 ? StatusGreen : StatusDim, $"{count} {noun} {suffix}", count > 0 ? TextCol : StatusDim);
    }

    private static void Legend(Vector4 color, string name, string desc)
    {
        Dot(color, name, TextCol);
        ImGui.SameLine(0, 6f);
        SettingsMenu.Dim("- " + desc);
    }

    private static void StarLegend()
    {
        StarLine(MirageGold, "Flagged", TextCol);
        ImGui.SameLine(0, 6f);
        SettingsMenu.Dim("- click a row to add/remove it from your watchlist");
    }

    private static void StarLine(Vector4 starColor, string text, Vector4 textColor)
    {
        var p = ImGui.GetCursorScreenPos();
        float h = ImGui.GetTextLineHeight();
        Star(ImGui.GetWindowDrawList(), new Vector2(p.X + 5f, p.Y + h * 0.5f), 5.5f, starColor, true);
        ImGui.Dummy(new Vector2(12f, h));
        ImGui.SameLine(0, 4f);
        SettingsMenu.Text(textColor, text);
    }

    private static void InfoBlock(string title, string body)
    {
        var dl = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        ImGui.Indent(12f);
        SettingsMenu.Text(Accent, title);
        ImGui.PushTextWrapPos(0f);
        SettingsMenu.Dim(body);
        ImGui.PopTextWrapPos();
        ImGui.Unindent(12f);
        var end = ImGui.GetCursorScreenPos();
        dl.AddRectFilled(start, new Vector2(start.X + 3f, end.Y - 2f), ImGui.GetColorU32(Accent with { W = 0.4f }), 1f);
        ImGui.Spacing();
    }

    private static void PhaseBanner(string text, Vector4 color)
    {
        var dl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var max = min + new Vector2(ImGui.GetContentRegionAvail().X, 24f);
        dl.AddRectFilled(min, max, ImGui.GetColorU32(color with { W = 0.15f }), 4f);
        dl.AddRect(min, max, ImGui.GetColorU32(color with { W = 0.4f }), 4f);
        var sz = ImGui.CalcTextSize(text);
        dl.AddText((min + max) * 0.5f - sz * 0.5f, ImGui.GetColorU32(color), text);
        ImGui.Dummy(new Vector2(1f, 28f));
    }

    private static void Star(ImDrawListPtr dl, Vector2 center, float radius, Vector4 color, bool filled)
    {
        const int points = 5;
        float innerR = radius * 0.38f;
        float start = -MathF.PI / 2f;
        uint col = ImGui.GetColorU32(color);
        Vector2 P(float angle, float r) => center + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
        for (int i = 0; i < points; i++)
        {
            float a0 = start + i * 2f * MathF.PI / points;
            float a1 = a0 + MathF.PI / points;
            float a2 = a0 + 2f * MathF.PI / points;
            if (filled)
            {
                dl.AddTriangleFilled(center, P(a0, radius), P(a1, innerR), col);
                dl.AddTriangleFilled(center, P(a1, innerR), P(a2, radius), col);
            }
            else
            {
                dl.AddLine(P(a0, radius), P(a1, innerR), col, 1f);
                dl.AddLine(P(a1, innerR), P(a2, radius), col, 1f);
            }
        }
    }

    private static Vector4 ToVec(SharpDX.Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, 1f);
}
