using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CanMoreImplant;

internal sealed class ImplantRow
{
    public BodyPartRecord Part;
    public HediffDef Implant;
    public Hediff Installed;
    public readonly List<RecipeDef> Recipes = new();
    public bool Inferior;
    public bool SimpleWorker;
    public string GroupKey => ImplantGroups.Key(Implant);
    public string GroupLabel => ImplantGroups.Label(Implant);
    private ImplantEffects effects;
    public ImplantEffects Effects => effects ??= ImplantEffects.Build(Implant, Installed);
    public string Label => Installed != null ? Installed.LabelCap.ToString() : Implant.LabelCap.ToString();
}
internal sealed class ImplantSnapshot
{
    public readonly List<ImplantRow> Rows = new();
    public int FailedRecipes;
    public readonly Dictionary<BodyPartRecord, float> PartEfficiencies = new();
}
internal static class ImplantCatalog
{
    private static Pawn cachedPawn;
    private static Game cachedGame;
    private static ImplantSnapshot cached;
    private static float nextRefresh;
    private static readonly HashSet<string> warned = new();
    public static void Invalidate() => cached = null;
    public static ImplantSnapshot Get(Pawn pawn)
    {
        if (cached == null || pawn != cachedPawn || cachedGame != Current.Game || Time.realtimeSinceStartup >= nextRefresh)
        {
            cachedGame = Current.Game;
            cachedPawn = pawn;
            cached = Build(pawn);
            nextRefresh = Time.realtimeSinceStartup + 1f;
        }
        return cached;
    }

    /// <summary>종족별 수술 목록과 Worker의 부위 판정을 사용하며 재고는 필터로 쓰지 않습니다.</summary>
    internal static ImplantSnapshot Build(Pawn pawn)
    {
        var snapshot = new ImplantSnapshot();
        if (pawn?.health?.hediffSet == null) return snapshot;
        var rows = new Dictionary<(BodyPartRecord, HediffDef), ImplantRow>();
        List<RecipeDef> recipes = pawn.def.AllRecipes;
        var explicitlyMapped = new HashSet<HediffDef>(recipes
            .Select(x => x.GetModExtension<ImplantRecipeExtension>())
            .Where(x => x != null && !x.exclude && x.implant != null).Select(x => x.implant));
        var installed = pawn.health.hediffSet.hediffs
            .Where(x => IsInstalledImplant(x) || (x.Visible && explicitlyMapped.Contains(x.def))).ToList();
        foreach (Hediff hediff in installed)
        {
            // 중복 장착된 실제 건강 항목은 보존하고 후보만 중복을 제거합니다.
            var row = new ImplantRow { Part = hediff.Part, Implant = hediff.def, Installed = hediff };
            snapshot.Rows.Add(row);
            rows.TryAdd((row.Part, row.Implant), row);
        }
        foreach (RecipeDef recipe in recipes)
        {
            try
            {
                ImplantRecipeExtension extension = recipe.GetModExtension<ImplantRecipeExtension>();
                if (extension?.exclude == true || !recipe.IsSurgery) continue;
                HediffDef implant = extension?.implant ?? recipe.addsHediff;
                if (extension?.implant == null && !IsImplantDefinition(implant)) continue;
                if (!TargetEligibility.Allows(pawn, recipe, implant)) continue;
                bool simpleWorker = recipe.Worker.GetType() == typeof(Recipe_InstallArtificialBodyPart) ||
                                    extension?.allowAutomaticComparison == true;
                // 열거 중 예외를 던지는 다른 모드도 있으므로 ToList까지 보호합니다.
                var parts = recipe.targetsBodyPart
                    ? recipe.Worker.GetPartsToApplyOn(pawn, recipe).Distinct().ToList()
                    : new List<BodyPartRecord> { null };
                foreach (BodyPartRecord part in parts)
                {
                    // 구울/나이/성별 및 다른 모드의 수술 대상 조건을 실제 수술 메뉴와 동일하게 확인합니다.
                    // AvailableNow(연구 해금)와 재료 재고는 여기서 필터링하지 않습니다.
                    if (!recipe.AvailableOnNow(pawn, part)) continue;
                    var key = (part, implant);
                    if (!rows.TryGetValue(key, out ImplantRow row))
                    {
                        row = new ImplantRow { Part = part, Implant = implant, SimpleWorker = simpleWorker };
                        rows.Add(key, row);
                        snapshot.Rows.Add(row);
                    }
                    else row.SimpleWorker &= simpleWorker;
                    if (!row.Recipes.Contains(recipe)) row.Recipes.Add(recipe);
                }
            }
            catch (Exception error)
            {
                snapshot.FailedRecipes++;
                if (warned.Add(recipe.defName)) Log.Warning("[CanMoreImplant] Skipped surgery " + recipe.defName + ": " + error);
            }
        }
        foreach (ImplantRow row in snapshot.Rows)
        {
            // 비교 기준은 이 실제 부위입니다. 반대쪽 팔이나 미장착 상위 후보를 기준으로 삼지 않습니다.
            if (row.Part == null) continue;
            Hediff replacement = pawn.health.hediffSet.hediffs.FirstOrDefault(x =>
                x.Part == row.Part && x is Hediff_AddedPart);
            if (!snapshot.PartEfficiencies.TryGetValue(row.Part, out float currentEfficiency))
            {
                try { currentEfficiency = PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, row.Part); }
                catch (Exception error)
                {
                    // 사용자 정의 신체/건강 계산 실패 때문에 전체 탭을 닫지 않습니다. 판정만 보류합니다.
                    currentEfficiency = float.NaN;
                    if (warned.Add("efficiency:" + pawn.def.defName + ":" + row.Part.def.defName))
                        Log.Warning("[CanMoreImplant] Cannot compare body-part efficiency: " + error);
                }
                snapshot.PartEfficiencies.Add(row.Part, currentEfficiency);
            }
            // 장착품만 있는 부위도 왼쪽 효율 표시를 위해 계산하되, 장착품 자체는 숨기지 않습니다.
            if (row.Installed != null) continue;
            row.Inferior = ImplantComparison.ShouldHide(row.Implant, replacement?.def, row.Part,
                currentEfficiency, row.SimpleWorker);
            // 효율이 없는 보조 임플란트는 같은 계열의 명시 관계만 별도로 비교합니다.
            if (row.Implant.addedPartProps == null)
                row.Inferior |= installed.Any(x => x.Part == row.Part && row.GroupKey == ImplantGroups.Key(x.def) &&
                    ImplantComparison.IsInferior(row.Implant, x.def, row.Part, row.SimpleWorker));
        }
        var order = pawn.RaceProps.body.AllParts;
        snapshot.Rows.Sort((a, b) =>
        {
            int result = (a.Part == null ? -1 : order.IndexOf(a.Part)).CompareTo(b.Part == null ? -1 : order.IndexOf(b.Part));
            if (result != 0) return result;
            result = (a.Installed == null ? 1 : 0).CompareTo(b.Installed == null ? 1 : 0);
            if (result != 0) return result;
            result = string.Compare(a.GroupKey, b.GroupKey, StringComparison.Ordinal);
            return result != 0 ? result : string.Compare(a.Label, b.Label, StringComparison.CurrentCulture);
        });
        return snapshot;
    }
    internal static bool IsImplantDefinition(HediffDef def) => def != null &&
        (def.countsAsAddedPartOrImplant || def.addedPartProps != null ||
         (def.hediffClass != null && typeof(Hediff_Implant).IsAssignableFrom(def.hediffClass)));
    private static bool IsInstalledImplant(Hediff hediff) => hediff.Visible && IsImplantDefinition(hediff.def);
}
