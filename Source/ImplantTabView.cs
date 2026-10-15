using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CanMoreImplant;

internal static class ImplantTabView
{
    private static Vector2 scroll;
    private static Pawn lastPawn;
    private static Game lastGame;
    private static readonly Color candidateColor = new Color(0.56f, 0.56f, 0.56f);

    /// <summary>부위·현재 효율 / 임플란트 / 주요 효과의 세 열을 표시합니다. 수술 예약은 원래 탭에 둡니다.</summary>
    public static void Draw(Rect outer, Pawn pawn, bool allowOperations)
    {
        Color savedColor = GUI.color;
        GameFont savedFont = Text.Font;
        TextAnchor savedAnchor = Text.Anchor;
        bool savedWrap = Text.WordWrap;
        try
        {
            if (pawn != lastPawn || Current.Game != lastGame)
            {
                scroll = Vector2.zero;
                lastPawn = pawn;
                lastGame = Current.Game;
            }
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            Rect panel = new Rect(outer.x, outer.y + 31f, outer.width, outer.height - 31f);
            Widgets.DrawMenuSection(panel);
            var tabs = new List<TabRecord>
            {
                new TabRecord("HealthOverview".Translate(), () => ImplantTabState.SelectVanilla(false), false)
            };
            if (allowOperations)
                tabs.Add(new TabRecord("MedicalOperationsShort".Translate(), () => ImplantTabState.SelectVanilla(true), false));
            tabs.Add(new TabRecord("CMI.Tab".Translate(), ImplantTabState.SelectImplants, true));
            Rect tabRect = panel;
            tabRect.width = outer.width * 0.375f * 1.5f;
            TabDrawer.DrawTabs(tabRect, tabs);
            ImplantSnapshot snapshot = ImplantCatalog.Get(pawn);
            Rect content = panel.ContractedBy(9f);
            var footer = new Rect(content.x, content.yMax - 28f, content.width, 28f);
            bool showLower = !CanMoreImplantMod.Settings.hideInferior;
            bool oldShow = showLower;
            int hidden = snapshot.Rows.Count(x => x.Inferior);
            Widgets.CheckboxLabeled(footer, "CMI.ShowInferior".Translate(hidden), ref showLower);
            if (oldShow != showLower)
            {
                CanMoreImplantMod.Settings.hideInferior = !showLower;
                CanMoreImplantMod.Settings.Write();
            }
            TooltipHandler.TipRegion(footer, "CMI.Legend".Translate());
            content.height -= 32f;
            if (snapshot.FailedRecipes > 0)
            {
                var warning = new Rect(content.x, content.y, content.width, 28f);
                GUI.color = Color.yellow;
                Widgets.Label(warning, "CMI.Skipped".Translate(snapshot.FailedRecipes));
                GUI.color = Color.white;
                content.yMin += 30f;
            }
            DrawRows(content, snapshot.Rows.Where(x => showLower || !x.Inferior).ToList(), snapshot.PartEfficiencies);
        }
        finally
        {
            GUI.color = savedColor;
            Text.Font = savedFont;
            Text.Anchor = savedAnchor;
            Text.WordWrap = savedWrap;
        }
    }

    private static void DrawRows(Rect viewport, List<ImplantRow> rows,
        IReadOnlyDictionary<BodyPartRecord, float> efficiencies)
    {
        if (rows.Count == 0)
        {
            Widgets.Label(viewport, "CMI.Empty".Translate());
            return;
        }
        float width = viewport.width - 18f;
        ColumnWidths(width, out float partWidth, out float nameWidth, out float effectWidth);
        float effectX = partWidth + nameWidth;
        string[] headings = { "CMI.PartColumn".Translate(), "CMI.ImplantColumn".Translate(), "CMI.EffectColumn".Translate() };
        float headerHeight = Mathf.Max(Text.CalcHeight(headings[0], partWidth - 8f),
            Mathf.Max(Text.CalcHeight(headings[1], nameWidth - 8f), Text.CalcHeight(headings[2], effectWidth - 8f))) + 8f;
        GUI.color = Color.white;
        Widgets.Label(new Rect(viewport.x + 4, viewport.y, partWidth - 8f, headerHeight), headings[0]);
        Widgets.Label(new Rect(viewport.x + partWidth, viewport.y, nameWidth - 8f, headerHeight), headings[1]);
        Widgets.Label(new Rect(viewport.x + effectX, viewport.y, effectWidth - 8f, headerHeight), headings[2]);
        viewport.yMin += headerHeight;
        string[] parts = rows.Select(x => PartWithEfficiency(x.Part, efficiencies)).ToArray();
        string[] effects = rows.Select(x => x.Effects.Summary).ToArray();
        float[] heights = rows.Select((x, i) => Mathf.Max(26f,
            Mathf.Max(Text.CalcHeight(x.Label, nameWidth - 8f),
                Mathf.Max(Text.CalcHeight(effects[i], effectWidth - 8f),
                    i == 0 || x.Part != rows[i - 1].Part ? Text.CalcHeight(parts[i], partWidth - 8f) : 0f)) + 4f)).ToArray();
        float height = heights.Sum() + 8f * rows.Select(x => x.Part).Distinct().Count();
        Widgets.BeginScrollView(viewport, ref scroll, new Rect(0, 0, width, height));
        try
        {
            float y = 0;
            BodyPartRecord previous = null;
            for (int index = 0; index < rows.Count; index++)
            {
                ImplantRow row = rows[index];
                bool newPart = index == 0 || row.Part != previous;
                if (newPart && index > 0) y += 8f;
                float h = heights[index];
                Rect whole = new Rect(0, y, width, h);
                if ((index & 1) == 0) Widgets.DrawLightHighlight(whole);
                Widgets.DrawHighlightIfMouseover(whole);
                GUI.color = Color.white;
                if (newPart) Widgets.Label(new Rect(4, y + 2f, partWidth - 8f, h), parts[index]);
                GUI.color = row.Installed != null ? row.Installed.LabelColor : candidateColor;
                Widgets.Label(new Rect(partWidth, y + 2f, nameWidth - 8f, h), row.Label);
                Widgets.Label(new Rect(effectX, y + 2f, effectWidth - 8f, h), effects[index]);
                GUI.color = Color.white;
                if (row.Installed != null)
                    Widgets.InfoCardButton(new Rect(width - 24f, y, 24f, 24f), row.Installed);
                else Widgets.InfoCardButton(width - 24f, y, row.Implant);
                // 간단한 효과 목록과 출처만 표시합니다. 자세한 내용은 정보 버튼에 남겨 둡니다.
                TooltipHandler.TipRegion(new Rect(0, y, width - 28f, h),
                    new TipSignal(() => ImplantTooltip.Build(row), 196401 + index));
                y += h;
                previous = row.Part;
            }
        }
        finally { Widgets.EndScrollView(); }
    }

    private static string PartLabel(BodyPartRecord part) => part == null ? "WholeBody".Translate().ToString() : part.LabelCap.ToString();

    internal static void ColumnWidths(float width, out float part, out float implant, out float effect)
    {
        // 오른쪽 정보 버튼(28px)을 제외한 공간을 세 열에 배분합니다.
        float usable = Mathf.Max(0f, width - 28f);
        part = usable * 0.25f;
        implant = usable * 0.40f;
        effect = usable - part - implant;
    }

    internal static string PartWithEfficiency(BodyPartRecord part, IReadOnlyDictionary<BodyPartRecord, float> efficiencies)
    {
        if (part == null) return PartLabel(null);
        string value = efficiencies.TryGetValue(part, out float efficiency) && float.IsFinite(efficiency)
            ? efficiency.ToStringPercent() : "?";
        return PartLabel(part) + "\n" + "CMI.CurrentEfficiency".Translate(value);
    }
}
