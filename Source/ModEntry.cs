using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CanMoreImplant;

/// <summary>환경 설정만 저장하며 건강 상태나 수술 데이터는 변경하지 않습니다.</summary>
public sealed class ImplantSettings : ModSettings
{
    public bool hideInferior = true;
    public bool useExplicitRules = true;
    public override void ExposeData()
    {
        Scribe_Values.Look(ref hideInferior, "hideInferior", true);
        Scribe_Values.Look(ref useExplicitRules, "useExplicitRules", true);
        base.ExposeData();
    }
}

public sealed class CanMoreImplantMod : Mod
{
    public static ImplantSettings Settings;
    public CanMoreImplantMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<ImplantSettings>();
        // 게임 정의 로딩 중이므로 화면 연결 패치만 설치합니다.
        try
        {
            new Harmony("canmoreimplant.healthtab").PatchAll();
            Log.Message("[CanMoreImplant] Health-tab hooks loaded (RimWorld 1.7).");
        }
        catch (Exception e) { Log.Error("[CanMoreImplant] Cannot attach implant tab: " + e); }
    }
    public override string SettingsCategory() => "Can More Implant";
    public override void DoSettingsWindowContents(Rect rect)
    {
        var listing = new Listing_Standard();
        listing.Begin(rect);
        listing.CheckboxLabeled("CMI.HideInferior".Translate(), ref Settings.hideInferior);
        listing.CheckboxLabeled("CMI.ExplicitRules".Translate(), ref Settings.useExplicitRules);
        listing.Gap();
        listing.Label("CMI.SettingsHelp".Translate());
        listing.End();
        ImplantCatalog.Invalidate();
    }
}
