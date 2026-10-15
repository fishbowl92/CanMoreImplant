using System.Linq;
using Verse;

namespace CanMoreImplant;

/// <summary>마우스 설명은 이름·효과·출처만 포함합니다. 수술 설명이나 건강 상태 원문을 붙이지 않습니다.</summary>
internal static class ImplantTooltip
{
    internal static string Build(ImplantRow row) =>
        row.Label + "\n" + row.Effects.Details + "\n[" + Source(row) + "]";

    internal static string Source(ImplantRow row)
    {
        // 수술을 추가한 패치 모드보다 임플란트 정의의 원래 출처를 우선합니다.
        if (row.Implant.modContentPack != null) return EnglishSourceName(row.Implant.modContentPack);
        string[] sources = row.Recipes.Where(x => x.modContentPack != null)
            .Select(x => EnglishSourceName(x.modContentPack)).Distinct().ToArray();
        return sources.Length == 0 ? "Unknown" : string.Join(", ", sources);
    }

    internal static string EnglishSourceName(ModContentPack source)
    {
        if (source == null) return "Unknown";
        // DLC의 표시 이름은 번역 모드가 바꿀 수 있으므로 고정 식별자로 영어 이름을 선택합니다.
        switch (source.PackageId?.ToLowerInvariant())
        {
            case "ludeon.rimworld": return "Core";
            case "ludeon.rimworld.royalty": return "Royalty";
            case "ludeon.rimworld.ideology": return "Ideology";
            case "ludeon.rimworld.biotech": return "Biotech";
            case "ludeon.rimworld.anomaly": return "Anomaly";
            case "ludeon.rimworld.odyssey": return "Odyssey";
        }
        // 외부 모드에 영어 이름이 없으면 임의 번역하지 않고 영문 패키지 식별자로 표시합니다.
        string name = source.Name;
        if (!string.IsNullOrWhiteSpace(name) && name.All(c => c >= ' ' && c <= '~')) return name;
        return string.IsNullOrWhiteSpace(source.PackageId) ? "Unknown" : source.PackageId;
    }
}
