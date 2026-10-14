using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace CanMoreImplant;

/// <summary>모드 제작자가 보증하는 명시적 관계. 같은 실제 신체 부위 안에서만 적용합니다.</summary>
public sealed class ImplantComparisonDef : Def
{
    public HediffDef lower;
    public HediffDef higher;
    public BodyPartDef bodyPart;
    public bool enabled = true;
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (lower == null || higher == null) yield return "lower and higher must be valid HediffDefs.";
        if (lower != null && lower == higher) yield return "An implant cannot outrank itself.";
    }
}

/// <summary>특수 수술을 목록에 연결할 수 있는 RecipeDef용 확장 데이터.</summary>
public sealed class ImplantRecipeExtension : DefModExtension
{
    public HediffDef implant;
    public bool exclude;
    public bool allowAutomaticComparison;
}

internal static class ImplantComparison
{
    private static readonly Dictionary<(HediffDef, HediffDef), bool> simpleCache = new();
    private static readonly HashSet<string> cosmeticFields = new()
    {
        "descriptionShort", "debugLabelExtra", "spawnThingOnRemoved", "priceImpact", "priceOffset",
        "defaultLabelColor", "labelNoun", "battleStateLabel", "labelNounPretty", "targetPrefix",
        "overrideTooltip", "extraTooltip", "inspectString", "scenarioCanAdd", "addedPartProps"
    };

    public static bool IsInferior(HediffDef candidate, HediffDef installed, BodyPartRecord part, bool simpleWorker)
    {
        if (candidate == null || installed == null || candidate == installed) return false;
        if (CanMoreImplantMod.Settings.useExplicitRules)
        {
            bool forward = ExplicitPath(candidate, installed, part?.def);
            bool reverse = ExplicitPath(installed, candidate, part?.def);
            // 모드끼리 반대되는 관계를 선언했다면 둘 다 숨기지 않습니다.
            if (forward || reverse) return forward && !reverse;
        }
        if (!simpleWorker) return false;
        var key = (candidate, installed);
        if (!simpleCache.TryGetValue(key, out bool result))
        {
            result = SimpleDominates(installed, candidate);
            simpleCache[key] = result;
        }
        return result;
    }

    private static bool ExplicitPath(HediffDef from, HediffDef to, BodyPartDef part)
    {
        var seen = new HashSet<HediffDef> { from };
        var queue = new Queue<HediffDef>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            HediffDef current = queue.Dequeue();
            foreach (ImplantComparisonDef rule in DefDatabase<ImplantComparisonDef>.AllDefsListForReading)
            {
                if (!rule.enabled || rule.lower != current || rule.higher == null ||
                    (rule.bodyPart != null && rule.bodyPart != part)) continue;
                if (rule.higher == to) return true;
                if (seen.Add(rule.higher)) queue.Enqueue(rule.higher);
            }
        }
        return false;
    }

    /// <summary>효율 외의 실제 작동 데이터가 같은 단순 교체품만 자동 비교합니다.</summary>
    internal static bool SimpleDominates(HediffDef upper, HediffDef lower)
    {
        if (!IsSimple(upper) || !IsSimple(lower)) return false;
        if (!(upper.addedPartProps.partEfficiency > lower.addedPartProps.partEfficiency + 0.0001f)) return false;
        foreach (FieldInfo field in typeof(HediffDef).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (cosmeticFields.Contains(field.Name)) continue;
            if (!SameValue(field.GetValue(upper), field.GetValue(lower), 0)) return false;
        }
        foreach (FieldInfo field in typeof(AddedBodyPartProps).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.Name == "partEfficiency" || field.Name == "betterThanNatural") continue;
            if (!SameValue(field.GetValue(upper.addedPartProps), field.GetValue(lower.addedPartProps), 0)) return false;
        }
        return true;
    }
    private static bool IsSimple(HediffDef def) => def != null && def.GetType() == typeof(HediffDef) &&
        def.hediffClass == typeof(Hediff_AddedPart) && def.addedPartProps != null &&
        def.comps.NullOrEmpty() && def.stages.NullOrEmpty() && def.modExtensions.NullOrEmpty() &&
        def.hediffGivers.NullOrEmpty() && def.abilities.NullOrEmpty();

    private static bool SameValue(object a, object b, int depth)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null)
        {
            // 미설정 목록과 빈 목록은 동등하지만 비어 있지 않은 데이터는 추정하지 않습니다.
            object value = a ?? b;
            return value is ICollection collection && collection.Count == 0;
        }
        if (a.GetType() != b.GetType() || depth > 8) return false;
        Type type = a.GetType();
        if (a is Def || a is Type) return a.Equals(b);
        if (type.IsValueType || a is string) return a.Equals(b);
        if (a is SimpleCurve ac && b is SimpleCurve bc)
            return SameValue(ac.Points, bc.Points, depth + 1);
        if (a is IEnumerable first && b is IEnumerable second)
        {
            object[] xs = first.Cast<object>().ToArray();
            object[] ys = second.Cast<object>().ToArray();
            return xs.Length == ys.Length && xs.Zip(ys, (x, y) => SameValue(x, y, depth + 1)).All(x => x);
        }
        // 알 수 없는 사용자 정의 객체의 동작은 비교하지 않습니다.
        return false;
    }
}
