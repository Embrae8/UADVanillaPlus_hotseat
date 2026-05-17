using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UADVanillaPlus.GameData;

namespace UADVanillaPlus.Harmony;

// Patch intent: provide a deterministic research model where campaign year,
// not monthly research spending, owns ordinary technology availability.
[HarmonyPatch(typeof(CampaignController))]
internal static class CampaignHistoricalResearchPatch
{
    private const float ResearchCompleteProgress = 100f;
    private const int MaxSyncPasses = 256;
    private static readonly MethodInfo? TechStartNewResearchsMethod =
        AccessTools.Method(typeof(CampaignController), "TechStartNewResearchs");
    private static readonly HashSet<string> LoggedWarnings = new(StringComparer.Ordinal);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CampaignController.NextTurn))]
    internal static void NextTurnPrefix(CampaignController __instance)
        => ApplyBudgetClampOnly("next turn prefix", __instance);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CampaignController.GetResearchSpeed))]
    internal static bool GetResearchSpeedPrefix(ref float __result)
    {
        if (ModSettings.TechnologySpread != ModSettings.TechnologySpreadMode.Historical)
            return true;

        __result = 0f;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("AiTechBudget")]
    internal static bool AiTechBudgetPrefix(Player player)
    {
        if (ModSettings.TechnologySpread != ModSettings.TechnologySpreadMode.Historical)
            return true;

        int ignored = 0;
        ZeroResearchSpending(player, ref ignored);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnNewTurn")]
    internal static void OnNewTurnPostfix(CampaignController __instance)
        => ApplyCurrentSetting("new turn", __instance);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CampaignController.OnLoadingScreenHide))]
    internal static void OnLoadingScreenHidePostfix(CampaignController __instance)
        => ApplyCurrentSetting("campaign load", __instance);

    internal static void ApplyCurrentSetting(string context = "manual", CampaignController? campaign = null)
    {
        if (ModSettings.TechnologySpread != ModSettings.TechnologySpreadMode.Historical)
            return;

        SynchronizeHistoricalTechs(context, campaign ?? CampaignController.Instance);
    }

    internal static void ApplyBudgetClampOnly(string context = "manual", CampaignController? campaign = null)
    {
        if (ModSettings.TechnologySpread != ModSettings.TechnologySpreadMode.Historical)
            return;

        ClampHistoricalResearchSpending(context, campaign ?? CampaignController.Instance);
    }

    private static void SynchronizeHistoricalTechs(string context, CampaignController? campaign)
    {
        if (campaign?.CampaignData?.PlayersMajor == null)
            return;

        int currentYear = CampaignYear(campaign);
        if (currentYear <= 0)
            return;

        List<Player> players = MajorPlayers(campaign);
        if (players.Count == 0)
            return;

        Dictionary<string, int> completedByPlayer = new(StringComparer.Ordinal);
        List<Technology> completedForMainPlayer = new();
        int completedTotal = 0;
        int syncedPlayers = 0;
        int budgetChanges = 0;
        int pass;

        for (pass = 1; pass <= MaxSyncPasses; pass++)
        {
            int completedThisPass = 0;

            foreach (Player player in players)
            {
                ZeroResearchSpending(player, ref budgetChanges);
                int completed = CompleteDueTechnologies(
                    player,
                    currentYear,
                    IsMainPlayer(player) ? completedForMainPlayer : null);
                if (completed <= 0)
                    continue;

                completedThisPass += completed;
                completedTotal += completed;
                string label = PlayerLabel(player);
                completedByPlayer[label] = completedByPlayer.TryGetValue(label, out int previous)
                    ? previous + completed
                    : completed;
            }

            if (completedThisPass == 0)
                break;

            RefreshVanillaResearchState(campaign);
        }

        foreach (Player player in players)
        {
            ZeroResearchSpending(player, ref budgetChanges);
            syncedPlayers++;
        }

        if (pass > MaxSyncPasses)
        {
            Melon<UADVanillaPlusMod>.Logger.Warning(
                $"UADVP historical research: stopped after {MaxSyncPasses} passes at year {currentYear}; additional due techs may remain.");
        }

        Melon<UADVanillaPlusMod>.Logger.Msg(
            $"UADVP historical research: {currentYear} synced {syncedPlayers} nations, completed {completedTotal} technologies, budgets zeroed, changedBudgets={budgetChanges}, passes={Math.Min(pass, MaxSyncPasses)}, context={context}.");

        ReportPlayerDiscoveries(context, completedForMainPlayer);

        if (completedByPlayer.Count == 0)
            return;

        string samples = string.Join(
            ", ",
            completedByPlayer
                .OrderByDescending(static pair => pair.Value)
                .ThenBy(static pair => pair.Key)
                .Take(8)
                .Select(static pair => $"{pair.Key} +{pair.Value}"));
        Melon<UADVanillaPlusMod>.Logger.Msg($"UADVP historical research samples: {samples}.");
    }

    private static void ClampHistoricalResearchSpending(string context, CampaignController? campaign)
    {
        if (campaign?.CampaignData?.PlayersMajor == null)
            return;

        int changedBudgets = 0;
        int syncedPlayers = 0;
        foreach (Player player in MajorPlayers(campaign))
        {
            ZeroResearchSpending(player, ref changedBudgets);
            syncedPlayers++;
        }

        if (changedBudgets <= 0)
            return;

        Melon<UADVanillaPlusMod>.Logger.Msg(
            $"UADVP historical research: clamped research spending for {syncedPlayers} nations, changedBudgets={changedBudgets}, context={context}.");
    }

    private static int CompleteDueTechnologies(Player player, int currentYear, List<Technology>? completedTechnologies)
    {
        if (player.technologies == null)
            return 0;

        int completed = 0;
        foreach (Technology tech in Technologies(player))
        {
            TechnologyData? data = tech.data;
            if (data == null || data.isEnd || tech.progress >= ResearchCompleteProgress)
                continue;

            int techYear = HistoricalTechYear(data);
            if (techYear > currentYear)
                continue;

            tech.progress = ResearchCompleteProgress;
            completedTechnologies?.Add(tech);
            completed++;
        }

        return completed;
    }

    private static void ReportPlayerDiscoveries(string context, List<Technology> completedTechnologies)
    {
        if (completedTechnologies.Count == 0)
            return;

        Player? mainPlayer = PlayerController.Instance;
        Ui? ui = G.ui;
        if (mainPlayer == null || ui == null)
        {
            WarnOnce(
                $"report-discovery-unavailable:{context}",
                $"UADVP historical research: could not report {completedTechnologies.Count} player discoveries because campaign UI is unavailable, context={context}.");
            return;
        }

        int reported = 0;
        int failed = 0;
        foreach (Technology tech in completedTechnologies)
        {
            try
            {
                ui.ReportDiscovery(mainPlayer, tech);
                reported++;
            }
            catch (Exception ex)
            {
                failed++;
                WarnOnce(
                    $"report-discovery:{TechKey(tech)}:{ex.GetType().Name}",
                    $"UADVP historical research: ReportDiscovery failed for {TechnologyName(tech)}. {ex.GetType().Name}: {ex.Message}");
            }
        }

        string failedText = failed > 0 ? $", failed={failed}" : string.Empty;
        Melon<UADVanillaPlusMod>.Logger.Msg(
            $"UADVP historical research: reported {reported}/{completedTechnologies.Count} player discoveries, context={context}{failedText}.");
    }

    private static int HistoricalTechYear(TechnologyData data)
    {
        try
        {
            return GameManager.GetTechYear(data, true, true);
        }
        catch (Exception ex)
        {
            WarnOnce(
                $"tech-year:{data.Pointer}",
                $"UADVP historical research: failed to read historical year for {TechLabel(data)}. {ex.GetType().Name}: {ex.Message}");
            return int.MaxValue;
        }
    }

    private static void RefreshVanillaResearchState(CampaignController campaign)
    {
        try
        {
            TechStartNewResearchsMethod?.Invoke(campaign, Array.Empty<object>());
        }
        catch (Exception ex)
        {
            WarnOnce(
                "tech-start-new-researchs",
                $"UADVP historical research: TechStartNewResearchs invoke failed. {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void ZeroResearchSpending(Player player, ref int budgetChanges)
    {
        try
        {
            if (Math.Abs(player.techBudget) > 0.0001f)
                budgetChanges++;

            player.techBudget = 0f;
        }
        catch (Exception ex)
        {
            WarnOnce(
                $"tech-budget:{PlayerKey(player)}",
                $"UADVP historical research: failed to zero tech budget for {PlayerLabel(player)}. {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            player.techPriorities?.Clear();
        }
        catch (Exception ex)
        {
            WarnOnce(
                $"tech-priorities:{PlayerKey(player)}",
                $"UADVP historical research: failed to clear tech priorities for {PlayerLabel(player)}. {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static List<Player> MajorPlayers(CampaignController campaign)
    {
        List<Player> players = new();

        foreach (Player player in campaign.CampaignData.PlayersMajor)
        {
            if (player?.technologies != null)
                players.Add(player);
        }

        return players;
    }

    private static List<Technology> Technologies(Player player)
    {
        List<Technology> technologies = new();

        foreach (Technology tech in player.technologies)
        {
            if (tech != null)
                technologies.Add(tech);
        }

        return technologies;
    }

    private static int CampaignYear(CampaignController campaign)
    {
        try
        {
            return campaign.CurrentDate.AsDate().Year;
        }
        catch
        {
            return -1;
        }
    }

    private static string PlayerKey(Player? player)
    {
        if (!string.IsNullOrWhiteSpace(player?.data?.name))
            return player.data.name;

        return player?.GetHashCode().ToString() ?? "unknown";
    }

    private static bool IsMainPlayer(Player? player)
    {
        try
        {
            return player?.isMain == true && player.isAi == false;
        }
        catch
        {
            return false;
        }
    }

    private static string PlayerLabel(Player? player)
    {
        try
        {
            string? name = player?.Name(false);
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(player?.data?.nameUi))
            return player.data.nameUi;

        return player?.data?.name ?? "unknown nation";
    }

    private static string TechLabel(TechnologyData data)
        => !string.IsNullOrWhiteSpace(data.name) ? data.name : data.GetHashCode().ToString();

    private static string TechKey(Technology? tech)
    {
        try
        {
            TechnologyData? data = tech?.data;
            if (data != null && !string.IsNullOrWhiteSpace(data.name))
                return data.name;
        }
        catch
        {
        }

        return tech?.GetHashCode().ToString() ?? "unknown";
    }

    private static string TechnologyName(Technology? tech)
    {
        try
        {
            string? name = tech?.data?.GetName();
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }
        catch
        {
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(tech?.data?.name))
                return tech.data.name;
        }
        catch
        {
        }

        return "unknown technology";
    }

    private static void WarnOnce(string key, string message)
    {
        if (LoggedWarnings.Add(key))
            Melon<UADVanillaPlusMod>.Logger.Warning(message);
    }
}
