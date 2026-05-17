using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UADVanillaPlus.GameData;

namespace UADVanillaPlus.Harmony;

// Patch intent: make major combatants feel less like torpedo boats by removing
// torpedo launchers from the human designer part list for CA and larger designs.
// AI/random generation remains unrestricted during shipgen because some early
// CA hulls require placeholder torpedo parts; successful AI designs are
// sanitized afterward by CampaignGeneratedDesignSanitizer.
[HarmonyPatch(typeof(Ship))]
internal static class DesignTorpedoRestrictionPatch
{
    private static readonly HashSet<string> LoggedShipTypes = new(StringComparer.OrdinalIgnoreCase);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ship.IsPartAvailable), typeof(PartData), typeof(Player), typeof(ShipType), typeof(Ship))]
    internal static void IsPartAvailablePostfix(PartData part, Player player, ShipType shipType, Ship ship, ref bool __result)
    {
        if (!__result ||
            !ModSettings.MajorShipTorpedoesRestricted ||
            !IsHumanDesigner(player, ship) ||
            !IsTorpedo(part) ||
            !IsMajorShipType(shipType))
        {
            return;
        }

        __result = false;
        LogFirstBlock(part, shipType);
    }

    private static bool IsHumanDesigner(Player? player, Ship? ship)
    {
        Player? owner = player ?? ship?.player;
        if (owner == null)
            return false;

        try
        {
            return owner.isMain && !owner.isAi;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTorpedo(PartData? part)
        => part?.isTorpedo == true
           || IsText(part?.type, "torpedo")
           || IsText(part?.name, "torpedo");

    private static bool IsMajorShipType(ShipType? shipType)
    {
        string label = ShipTypeLabel(shipType);
        return label is "CA" or "BC" or "BB"
               || label.Contains("HEAVY CRUISER", StringComparison.OrdinalIgnoreCase)
               || label.Contains("BATTLECRUISER", StringComparison.OrdinalIgnoreCase)
               || label.Contains("BATTLESHIP", StringComparison.OrdinalIgnoreCase);
    }

    private static string ShipTypeLabel(ShipType? shipType)
    {
        string? label = shipType?.nameUi;
        if (string.IsNullOrWhiteSpace(label))
            label = shipType?.name;
        if (string.IsNullOrWhiteSpace(label))
            label = shipType?.nameFull;

        return string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim().ToUpperInvariant();
    }

    private static bool IsText(string? value, string needle)
        => !string.IsNullOrWhiteSpace(value) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    private static void LogFirstBlock(PartData part, ShipType shipType)
    {
        string label = ShipTypeLabel(shipType);
        if (!LoggedShipTypes.Add(label))
            return;

        string partName = string.IsNullOrWhiteSpace(part.nameUi) ? part.name : part.nameUi;
        Melon<UADVanillaPlusMod>.Logger.Msg($"UADVP design balance: hiding torpedo parts for player {label} designs. First blocked part: {partName}.");
    }
}
