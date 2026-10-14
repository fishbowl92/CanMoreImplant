using Verse;

namespace CanMoreImplant;

/// <summary>HediffDef에 지정합니다. 한 모드에 서로 다른 용도의 의체 계열이 있을 때 분리합니다.</summary>
public sealed class ImplantGroupExtension : DefModExtension
{
    public string comparisonGroup;
    public string groupLabel;
}

internal static class ImplantGroups
{
    internal const string Vanilla = "vanilla";

    // 출처 없는 정의를 바닐라로 오인하지 않습니다. 런타임 생성 정의는 각자 분리합니다.
    internal static string Key(HediffDef implant)
    {
        string explicitGroup = implant.GetModExtension<ImplantGroupExtension>()?.comparisonGroup;
        if (!string.IsNullOrWhiteSpace(explicitGroup)) return "custom:" + explicitGroup.Trim();
        ModContentPack source = implant.modContentPack;
        if (source == null) return "unknown:" + implant.defName;
        return source.IsCoreMod || source.IsOfficialMod ? Vanilla : "mod:" + source.PackageId;
    }

    internal static string Label(HediffDef implant)
    {
        var extension = implant.GetModExtension<ImplantGroupExtension>();
        if (!string.IsNullOrWhiteSpace(extension?.comparisonGroup))
            return string.IsNullOrWhiteSpace(extension.groupLabel) ? extension.comparisonGroup : extension.groupLabel;
        if (Key(implant) == Vanilla) return "CMI.VanillaGroup".Translate();
        return implant.modContentPack?.Name ?? "CMI.UnknownGroup".Translate().ToString();
    }
}
