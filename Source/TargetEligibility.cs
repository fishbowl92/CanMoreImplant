using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CanMoreImplant;

internal static class TargetEligibility
{
    private static HashSet<HediffDef> mechanitorImplants;

    /// <summary>재고/연구와 무관한 대상 종류 제한입니다. 부위 열거 전에 검사합니다.</summary>
    internal static bool Allows(Pawn pawn, RecipeDef recipe, HediffDef implant)
    {
        MutantDef mutant = pawn.IsMutant ? pawn.mutant.Def : null;
        if (recipe.mutantPrerequisite != null && !recipe.mutantPrerequisite.Contains(mutant)) return false;
        if (mutant != null && recipe.mutantBlacklist?.Contains(mutant) == true) return false;
        if (recipe.humanlikeOnly && !pawn.RaceProps.Humanlike) return false;
        if (recipe.GetModExtension<ImplantRecipeExtension>()?.requiresMechanitor == true ||
            MechanitorImplants.Contains(implant))
        {
            // 메카링크는 메카니터가 되는 수단이므로 이것 자체를 전용 후보로 제한하지 않습니다.
            return pawn.health.hediffSet.hediffs.Any(x => x.def.defName == "MechlinkImplant");
        }
        return true;
    }

    private static HashSet<HediffDef> MechanitorImplants
    {
        get
        {
            if (mechanitorImplants != null) return mechanitorImplants;
            mechanitorImplants = new HashSet<HediffDef>();
            // 이름에 '메카'가 들어가는지 추측하지 않고 아이템 정의의 실제 선행 건강 상태를 읽습니다.
            foreach (ThingDef item in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (item.comps == null || !item.comps.OfType<CompProperties_Usable>()
                    .Any(x => x.userMustHaveHediff?.defName == "MechlinkImplant")) continue;
                foreach (var effect in item.comps.OfType<CompProperties_UseEffectInstallImplant>())
                    if (effect.hediffDef != null) mechanitorImplants.Add(effect.hediffDef);
            }
            return mechanitorImplants;
        }
    }

    internal static void Invalidate() => mechanitorImplants = null;
}
