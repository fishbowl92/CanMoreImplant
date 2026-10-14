using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    /// <summary>기존 건강 창 내부에 부위/항목 두 열만 그립니다. 수술 예약 기능은 원래 수술 탭에 둡니다.</summary>
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
            DrawRows(content, pawn, snapshot.Rows.Where(x => showLower || !x.Inferior).ToList());
        }
        finally
        {
            GUI.color = savedColor;
            Text.Font = savedFont;
            Text.Anchor = savedAnchor;
            Text.WordWrap = savedWrap;
        }
    }

    private static void DrawRows(Rect viewport, Pawn pawn, List<ImplantRow> rows)
    {
        if (rows.Count == 0)
        {
            Widgets.Label(viewport, "CMI.Empty".Translate());
            return;
        }
        float width = viewport.width - 18f;
        float partWidth = width * 0.36f;
        float nameWidth = width - partWidth - 28f;
        float[] heights = rows.Select(x => Mathf.Max(26f,
            Mathf.Max(Text.CalcHeight(x.Label, nameWidth), Text.CalcHeight(PartLabel(x.Part), partWidth - 8f)) + 4f)).ToArray();
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
                if (newPart) Widgets.Label(new Rect(4, y + 2f, partWidth - 8f, h), PartLabel(row.Part));
                GUI.color = row.Installed != null ? row.Installed.LabelColor : candidateColor;
                Widgets.Label(new Rect(partWidth, y + 2f, nameWidth, h), row.Label);
                GUI.color = Color.white;
                if (row.Installed != null)
                    Widgets.InfoCardButton(new Rect(width - 24f, y, 24f, 24f), row.Installed);
                else Widgets.InfoCardButton(width - 24f, y, row.Implant);
                // 설명은 실제로 마우스를 올릴 때만 계산해 재료 검색 비용을 줄입니다.
                TooltipHandler.TipRegion(new Rect(0, y, width - 28f, h),
                    new TipSignal(() => Tooltip(pawn, row), 196401 + index));
                y += h;
                previous = row.Part;
            }
        }
        finally { Widgets.EndScrollView(); }
    }

    private static string PartLabel(BodyPartRecord part) => part == null ? "WholeBody".Translate().ToString() : part.LabelCap.ToString();

    private static string Tooltip(Pawn pawn, ImplantRow row)
    {
        if (row.Installed != null) return row.Installed.GetTooltip(pawn, false);
        var text = new StringBuilder();
        text.AppendLine(row.Label + " — " + PartLabel(row.Part));
        text.AppendLine("CMI.Candidate".Translate());
        if (row.Inferior) text.AppendLine("CMI.InferiorTip".Translate());
        text.AppendLine(row.Implant.description);
        text.AppendLine();
        foreach (RecipeDef recipe in row.Recipes)
        {
            text.AppendLine(recipe.LabelCap + " [" + (recipe.modContentPack?.Name ?? "?") + "]");
            try
            {
                AcceptanceReport report = recipe.Worker.AvailableReport(pawn, row.Part);
                if (pawn.Dead) text.AppendLine("CMI.Dead".Translate());
                else if (!recipe.AvailableNow) text.AppendLine("CMI.ResearchLocked".Translate());
                else if (!report.Accepted || !recipe.AvailableOnNow(pawn, row.Part))
                    text.AppendLine("CMI.Blocked".Translate(report.Reason ?? "CMI.SpecialCondition".Translate().ToString()));
                else text.AppendLine("CMI.BodyCompatible".Translate());
                if (pawn.MapHeld != null)
                {
                    var missing = recipe.PotentiallyMissingIngredients(null, pawn.MapHeld).ToList();
                    if (missing.Count > 0)
                        text.AppendLine("CMI.Missing".Translate(string.Join(", ", missing.Select(x => x.LabelCap))));
                }
                if (!recipe.skillRequirements.NullOrEmpty())
                    text.AppendLine("CMI.Skills".Translate(string.Join(", ", recipe.skillRequirements.Select(x => x.skill.LabelCap + " " + x.minLevel))));
            }
            catch (Exception)
            {
                text.AppendLine("CMI.ConditionUnknown".Translate());
            }
            text.AppendLine();
        }
        text.AppendLine("CMI.NoGuarantee".Translate());
        return text.ToString().TrimEnd();
    }
}
