using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CanMoreImplant;

/// <summary>표시 전용 요약입니다. 건강 상태를 만들거나 장착 후 종합 능력을 예측하지 않습니다.</summary>
internal sealed class ImplantEffects
{
    internal readonly List<string> Entries = new();
    internal string Summary => Summarize(Entries);
    internal string Details => string.Join("\n", Entries);

    internal static string Summarize(IReadOnlyList<string> entries)
    {
        if (entries.Count == 0) return "CMI.SpecialEffect".Translate();
        return entries[0] + (entries.Count > 1 ? "\n" + "CMI.MoreEffects".Translate(entries.Count - 1) : "");
    }

    internal static ImplantEffects Build(HediffDef def, Hediff installed,
        Func<HediffStage, Hediff, IEnumerable<StatDrawEntry>> readStats = null)
    {
        var result = new ImplantEffects();
        try
        {
            // 교체품은 부위 효율을 우선합니다. 현재 부위의 손상은 왼쪽 열에서 별도로 표시합니다.
            string efficiencyLabel = "PartEfficiency".Translate().ToString().CapitalizeFirst();
            string efficiencyValue = null;
            if (def.addedPartProps != null && float.IsFinite(def.addedPartProps.partEfficiency))
            {
                efficiencyValue = def.addedPartProps.partEfficiency.ToStringPercent();
                result.Entries.Add(efficiencyLabel + " " + efficiencyValue);
            }
            HediffStage stage = SelectStage(def, installed);
            if (HasUnresolvedScaling(stage, installed))
            {
                // 대상 능력/중첩 정도에 따라 바뀌는 값을 고정 보너스처럼 제시하지 않습니다.
                result.Entries.Add("CMI.ConditionalEffect".Translate());
            }
            else
            {
                // 게임 정보창의 단위/부호/번역을 재사용합니다. 방어력 %, 대역폭 +정수 등을 구별합니다.
                // 공급자 인자는 게임 설정을 초기화하지 않는 독립 테스트에도 사용합니다.
                var stats = (readStats ?? HediffStatsUtility.SpecialDisplayStats)(stage, installed)
                    .OrderByDescending(x => x.DisplayPriorityWithinCategory);
                foreach (var entry in stats)
                {
                    if (entry.LabelCap == efficiencyLabel && entry.ValueString == efficiencyValue) continue;
                    string line = entry.LabelCap + " " + entry.ValueString;
                    if (!result.Entries.Contains(line)) result.Entries.Add(line);
                }
            }
        }
        catch (Exception)
        {
            // 사용자 정의 효과/포맷터가 실패해도 탭 전체를 닫지 않고 정보 카드로 안내합니다.
            result.Entries.Add("CMI.SpecialEffect".Translate());
        }
        if (result.Entries.Count == 0) result.Entries.Add("CMI.SpecialEffect".Translate());
        return result;
    }

    internal static HediffStage SelectStage(HediffDef def, Hediff installed)
    {
        if (installed != null) return installed.CurStage;
        if (def.stages.NullOrEmpty()) return null;
        int index = def.StageAtSeverity(def.initialSeverity);
        return index >= 0 && index < def.stages.Count ? def.stages[index] : null;
    }

    internal static bool HasUnresolvedScaling(HediffStage stage, Hediff installed) => stage != null &&
        (stage.statOffsetEffectMultiplier != null || stage.statFactorEffectMultiplier != null ||
         stage.capacityFactorEffectMultiplier != null || (installed == null &&
            (stage.multiplyStatChangesBySeverity || !stage.statOffsetsBySeverity.NullOrEmpty() ||
             !stage.statFactorsBySeverity.NullOrEmpty() || stage.capMods?.Any(x => x.SetMaxDefined) == true)));
}
