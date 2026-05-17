using Il2Cpp;
using MelonLoader;

namespace UADVanillaPlus.GameData;

// This is campaign save-state, not a VP PlayerPrefs option. The active
// controller value is written into CampaignController.Store on the next save.
internal static class CampaignSharedDesignUsageSettings
{
    internal static bool HasActiveCampaign
        => CampaignController.Instance?.CampaignData != null;

    internal static CampaignController.SharedDesignUsage CurrentMode
        => CampaignController.Instance?.SharedDesignsUsage ?? CampaignController.SharedDesignUsage.Off;

    internal static string CurrentModeText()
        => HasActiveCampaign ? ModeText(CurrentMode) : "No Campaign";

    internal static bool TrySetMode(CampaignController.SharedDesignUsage mode)
    {
        CampaignController? campaign = CampaignController.Instance;
        if (campaign?.CampaignData == null)
        {
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP option: Shared Designs cannot be changed without a loaded campaign.");
            return false;
        }

        CampaignController.SharedDesignUsage oldMode = campaign.SharedDesignsUsage;
        if (oldMode == mode)
            return false;

        campaign.SharedDesignsUsage = mode;
        Melon<UADVanillaPlusMod>.Logger.Msg($"UADVP option: Shared Designs mode {ModeText(oldMode)} -> {ModeText(mode)}.");
        ModSettings.LogCurrentSettings("after Shared Designs change");
        return true;
    }

    internal static string ModeText(CampaignController.SharedDesignUsage mode)
        => mode switch
        {
            CampaignController.SharedDesignUsage.Selective => "Selective",
            CampaignController.SharedDesignUsage.Always => "Always",
            _ => "Off",
        };
}
