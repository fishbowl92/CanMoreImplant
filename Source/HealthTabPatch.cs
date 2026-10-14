using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CanMoreImplant;

internal static class ImplantTabState
{
    public static bool Active;
    private static readonly FieldInfo operationTab = AccessTools.Field(typeof(HealthCardUtility), "onOperationTab");
    public static void SelectVanilla(bool operations)
    {
        Active = false;
        operationTab.SetValue(null, operations);
    }
    public static void SelectImplants()
    {
        Active = true;
        ImplantCatalog.Invalidate();
    }
}

/// <summary>
/// 개요와 수술은 원본 렌더링을 그대로 사용합니다. 탭을 그리는 호출 한 곳만 감싸 세 번째 탭을 삽입합니다.
/// 다른 화면에서 사용하는 TabDrawer 전체를 전역 패치하지 않습니다.
/// </summary>
[HarmonyPatch(typeof(HealthCardUtility), nameof(HealthCardUtility.DrawHealthSummary))]
internal static class AddImplantTabPatch
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(TabDrawer) &&
                method.Name == "DrawTabs" && method.IsGenericMethod && method.GetParameters().Length == 3)
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(AddImplantTabPatch), nameof(DrawTabs));
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) Log.Error("[CanMoreImplant] Expected one health tab draw call, found " + replaced + ". Check game version/UI mods.");
    }

    public static TabRecord DrawTabs(Rect rect, List<TabRecord> tabs, float maxTabWidth)
    {
        tabs.Add(new TabRecord("CMI.Tab".Translate(), ImplantTabState.SelectImplants, false));
        // 원래 두 탭의 너비를 유지하면서 오른쪽으로 새 탭 하나만 늘립니다.
        rect.width *= 1.5f;
        return TabDrawer.DrawTabs(rect, tabs, maxTabWidth);
    }
}

[HarmonyPatch(typeof(HealthCardUtility), nameof(HealthCardUtility.DrawPawnHealthCard))]
internal static class ImplantCardPatch
{
    internal static bool Prefix(Rect outRect, Pawn pawn, bool allowOperations)
    {
        if (!ImplantTabState.Active || pawn?.health == null) return true;
        try
        {
            ImplantTabView.Draw(outRect, pawn, allowOperations && !pawn.Dead);
            return false;
        }
        catch (Exception error)
        {
            // 한 모드의 후보가 잘못되어도 기존 건강 창으로 돌아갈 수 있게 복구합니다.
            ImplantTabState.Active = false;
            Log.ErrorOnce("[CanMoreImplant] Restored original health view after error: " + error, 192648041);
            return true;
        }
    }
}
