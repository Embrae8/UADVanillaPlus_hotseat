using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace UADVanillaPlus.Harmony;

// Vanilla appends global duplicate counters such as " - 2" to refit designs.
// Keep refit names compact and only add a letter when the same class already
// has a refit design for the same campaign year.
[HarmonyPatch(typeof(Ship))]
internal static class DesignRefitNamePatch
{
    private static readonly Regex RefitYearNameRegex = new(
        @"^\s*(?<base>.*?)\s*\((?<year>\d{4})(?<letter>[A-Za-z]*)\)\s*(?:-\s*(?<number>\d+))?\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static bool loggedRule;
    private static bool loggedConflict;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Ship.GetRefitYearNameEnd), typeof(Player), typeof(bool), typeof(bool))]
    private static bool GetRefitYearNameEndPrefix(
        Ship __instance,
        Player tempPlayer,
        bool refitDesignIsRefitDesign,
        ref string __result)
    {
        try
        {
            CampaignController? campaign = CampaignController.Instance;
            if (__instance == null || tempPlayer == null || campaign == null)
                return true;

            string baseName = CleanRefitBaseName(__instance);
            if (string.IsNullOrWhiteSpace(baseName))
                return true;

            int refitYear = campaign.CurrentDate.AsDate().Year;
            int ordinal = NextSameYearOrdinal(tempPlayer, __instance, baseName, refitYear);
            string yearText = $"{refitYear}{ConflictLetterSuffix(ordinal)}";
            string refitSuffix = $" ({yearText})";

            __result = refitDesignIsRefitDesign ? $"{baseName}{refitSuffix}" : refitSuffix;
            LogRuleOnce();
            if (ordinal > 1)
                LogConflictOnce($"{baseName} ({yearText})");

            return false;
        }
        catch (Exception ex)
        {
            Melon<UADVanillaPlusMod>.Logger.Warning($"UADVP design refit names failed; using vanilla name. {ex.GetType().Name}: {ex.Message}");
            return true;
        }
    }

    private static int NextSameYearOrdinal(Player player, Ship currentDesign, string baseName, int refitYear)
    {
        int highestOrdinal = 0;
        var designs = new Il2CppSystem.Collections.Generic.List<Ship>(player.designs);
        foreach (Ship design in designs)
        {
            if (design == null || design.Pointer == currentDesign.Pointer)
                continue;

            if (!TryReadRefitYearName(design.name, design, out string candidateBaseName, out int candidateYear, out int candidateOrdinal))
                continue;

            if (candidateYear != refitYear || !string.Equals(candidateBaseName, baseName, StringComparison.OrdinalIgnoreCase))
                continue;

            highestOrdinal = Math.Max(highestOrdinal, Math.Max(1, candidateOrdinal));
        }

        return highestOrdinal + 1;
    }

    private static string CleanRefitBaseName(Ship? ship)
    {
        string? name = ship?.name;
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        if (TryReadRefitYearName(name, ship, out string baseName, out _, out _))
            return baseName;

        string cleaned = StripLegacyCloneSuffix(name.Trim());
        int yearStart = cleaned.IndexOf('(');
        if (yearStart > 0)
            cleaned = cleaned[..yearStart].TrimEnd();

        return StripLeadingShipTypePrefix(cleaned, ship);
    }

    private static bool TryReadRefitYearName(string? name, Ship? ship, out string baseName, out int year, out int ordinal)
    {
        baseName = string.Empty;
        year = 0;
        ordinal = 1;

        if (string.IsNullOrWhiteSpace(name))
            return false;

        Match match = RefitYearNameRegex.Match(name);
        if (!match.Success || !int.TryParse(match.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out year))
            return false;

        baseName = StripLeadingShipTypePrefix(match.Groups["base"].Value.Trim(), ship);
        if (string.IsNullOrWhiteSpace(baseName))
            return false;

        Group numberGroup = match.Groups["number"];
        if (numberGroup.Success && int.TryParse(numberGroup.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numberOrdinal))
        {
            ordinal = Math.Max(1, numberOrdinal);
            return true;
        }

        ordinal = LetterOrdinal(match.Groups["letter"].Value);
        return true;
    }

    private static string StripLeadingShipTypePrefix(string baseName, Ship? ship)
    {
        string cleaned = baseName.Trim();
        foreach (string typeCode in ShipTypeCodes(ship))
        {
            if (!IsCompactShipTypeCode(typeCode))
                continue;

            string token = typeCode.Trim();
            if (cleaned.Length <= token.Length || !cleaned.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                continue;

            char boundary = cleaned[token.Length];
            if (!char.IsWhiteSpace(boundary) && boundary != '-' && boundary != ':')
                continue;

            string withoutType = cleaned[(token.Length + 1)..].TrimStart();
            while (withoutType.Length > 0 && (withoutType[0] == '-' || withoutType[0] == ':'))
                withoutType = withoutType[1..].TrimStart();

            if (!string.IsNullOrWhiteSpace(withoutType))
                return withoutType;
        }

        return cleaned;
    }

    private static IEnumerable<string> ShipTypeCodes(Ship? ship)
    {
        if (!string.IsNullOrWhiteSpace(ship?.shipType?.name))
            yield return ship.shipType.name;

        if (!string.IsNullOrWhiteSpace(ship?.shipType?.nameUi))
            yield return ship.shipType.nameUi;
    }

    private static bool IsCompactShipTypeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string token = value.Trim();
        if (token.Length is < 1 or > 4)
            return false;

        foreach (char ch in token)
        {
            if (!char.IsLetter(ch))
                return false;
        }

        return true;
    }

    private static string StripLegacyCloneSuffix(string name)
    {
        int end = name.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(name[end]))
            end--;

        int digitEnd = end;
        while (end >= 0 && char.IsDigit(name[end]))
            end--;

        if (end == digitEnd)
            return name;

        while (end >= 0 && char.IsWhiteSpace(name[end]))
            end--;

        if (end <= 0 || name[end] != '-' || !char.IsWhiteSpace(name[end - 1]))
            return name;

        return name[..end].TrimEnd();
    }

    private static string ConflictLetterSuffix(int ordinal)
    {
        if (ordinal <= 1)
            return string.Empty;

        StringBuilder suffix = new();
        int value = ordinal;
        while (value > 0)
        {
            value--;
            suffix.Insert(0, (char)('a' + (value % 26)));
            value /= 26;
        }

        return suffix.ToString();
    }

    private static int LetterOrdinal(string letters)
    {
        if (string.IsNullOrWhiteSpace(letters))
            return 1;

        int value = 0;
        foreach (char letter in letters.Trim().ToLowerInvariant())
        {
            if (letter < 'a' || letter > 'z')
                return 1;

            value = (value * 26) + (letter - 'a' + 1);
        }

        return Math.Max(1, value);
    }

    private static void LogRuleOnce()
    {
        if (loggedRule)
            return;

        loggedRule = true;
        Melon<UADVanillaPlusMod>.Logger.Msg("UADVP design refit names: using class-year naming for player and AI refits.");
    }

    private static void LogConflictOnce(string generatedName)
    {
        if (loggedConflict)
            return;

        loggedConflict = true;
        Melon<UADVanillaPlusMod>.Logger.Msg($"UADVP design refit names: resolved same-year conflict as {generatedName}.");
    }
}
