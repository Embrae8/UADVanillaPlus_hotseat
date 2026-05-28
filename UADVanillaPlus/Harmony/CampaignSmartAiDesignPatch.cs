using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UADVanillaPlus.Services;

namespace UADVanillaPlus.Harmony;

[HarmonyPatch]
internal static class CampaignSmartAiDesignTryTakePatch
{
    private static MethodBase? TargetMethod()
        => AccessTools.Method(typeof(CampaignController), "TryTakeSharedDesign", new[] { typeof(Player), typeof(ShipType), typeof(bool) });

    [HarmonyPrepare]
    private static bool Prepare()
    {
        bool available = TargetMethod() != null;
        if (!available)
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP smart AI designs: TryTakeSharedDesign target not found; smart fallback disabled.");

        return available;
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(CampaignController __instance, Player player, ShipType shipType, bool prewarming, ref bool __result)
    {
        if (__result)
            return;

        SmartAiDesignAttemptResult result = SmartAiDesignService.TryReplaceRandomFallback(__instance, player, shipType, prewarming);
        if (result.Attempted && result.SuppressVanillaFallback)
            __result = true;
    }
}

[HarmonyPatch]
internal static class CampaignSmartAiDesignBuildNewShipsPatch
{
    private static MethodBase? TargetMethod()
        => AccessTools.Method(typeof(CampaignController), "BuildNewShips", new[] { typeof(Player), typeof(float) });

    [HarmonyPrepare]
    private static bool Prepare()
    {
        bool available = TargetMethod() != null;
        if (!available)
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP smart AI designs: BuildNewShips target not found; smart design summaries will flush later.");

        return available;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Low)]
    private static void Prefix(Player player)
        => SmartAiDesignService.FlushSummary(player);
}

[HarmonyPatch]
internal static class CampaignSmartAiDesignRandomShipMoveNextPatch
{
    private static MethodBase? TargetMethod()
        => DesignRandomShipSingleAttemptPatch.GenerateRandomShipMoveNextTarget();

    [HarmonyPrepare]
    private static bool Prepare()
    {
        bool available = TargetMethod() != null;
        if (available)
            SmartAiDesignService.LogSmartRandomShipHookAttached();
        else
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP smart AI designs: Ship.GenerateRandomShip MoveNext target not found; smart generator diagnostics disabled.");

        return available;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static void Prefix(object __instance)
        => SmartAiDesignService.SmartAiRandomShipMoveNextPrefix(__instance);

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    private static void Postfix(object __instance, bool __result)
        => SmartAiDesignService.SmartAiRandomShipMoveNextPostfix(__instance, __result);

    [HarmonyFinalizer]
    private static void Finalizer(object __instance, Exception? __exception)
        => SmartAiDesignService.SmartAiRandomShipMoveNextFinalizer(__instance, __exception);
}
