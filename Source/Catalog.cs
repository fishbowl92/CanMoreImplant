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
    public string Label => Installed != null ? Installed.LabelCap.ToString() : Implant.LabelCap.ToString();
}
internal sealed class ImplantSnapshot
{
    public readonly List<ImplantRow> Rows = new();
    public int FailedRecipes;
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
                bool simpleWorker = recipe.Worker.GetType() == typeof(Recipe_InstallArtificialBodyPart) ||
                                    extension?.allowAutomaticComparison == true;
                // 열거 중 예외를 던지는 다른 모드도 있으므로 ToList까지 보호합니다.
                var parts = recipe.targetsBodyPart
                    ? recipe.Worker.GetPartsToApplyOn(pawn, recipe).Distinct().ToList()
                    : new List<BodyPartRecord> { null };
                foreach (BodyPartRecord part in parts)
                {
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
        foreach (ImplantRow row in snapshot.Rows.Where(x => x.Installed == null))
        {
            // 미장착 상위 후보나 반대쪽 팔의 장착품 때문에 숨기지 않습니다.
            row.Inferior = installed.Any(x => x.Part == row.Part &&
                ImplantComparison.IsInferior(row.Implant, x.def, row.Part, row.SimpleWorker));
        }
        var order = pawn.RaceProps.body.AllParts;
        snapshot.Rows.Sort((a, b) =>
        {
            int result = (a.Part == null ? -1 : order.IndexOf(a.Part)).CompareTo(b.Part == null ? -1 : order.IndexOf(b.Part));
            if (result != 0) return result;
            result = (a.Installed == null ? 1 : 0).CompareTo(b.Installed == null ? 1 : 0);
            return result != 0 ? result : string.Compare(a.Label, b.Label, StringComparison.CurrentCulture);
        });
        return snapshot;
    }
    internal static bool IsImplantDefinition(HediffDef def) => def != null &&
        (def.countsAsAddedPartOrImplant || def.addedPartProps != null ||
         (def.hediffClass != null && typeof(Hediff_Implant).IsAssignableFrom(def.hediffClass)));
    private static bool IsInstalledImplant(Hediff hediff) => hediff.Visible && IsImplantDefinition(hediff.def);
}
