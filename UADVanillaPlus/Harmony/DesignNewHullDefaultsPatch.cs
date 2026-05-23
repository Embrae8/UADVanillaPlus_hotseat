using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace UADVanillaPlus.Harmony;

// Human-designer hull picks start from practical defaults instead of vanilla's
// randomized hull-change state. This is deliberately scoped to Ui.ChoosePart's
// constructor hull path so Auto Design, AI generation, imports, and save loads
// keep their own initialization behavior.
[HarmonyPatch]
internal static class DesignNewHullDefaultsPatch
{
    private const string LogPrefix = "UADVP designer hull defaults";
    private const float KnotsToMetersPerSecond = 0.514444444f;
    private const float MaxReasonableDesignerSpeedKnots = 80f;
    private const float MaxReasonableDesignerTonnage = 500000f;
    private const int MaxDeferredEquipmentAttempts = 5;

    private static readonly MethodInfo? RefreshHullStatsMethod =
        AccessTools.Method(typeof(Ship), "RefreshHullStats", Type.EmptyTypes);
    private static readonly MethodInfo? OnConShipChangedMethod =
        AccessTools.Method(typeof(Ui), "OnConShipChanged", new[] { typeof(bool) });
    private static readonly MethodInfo? RefreshPartsMethod =
        AccessTools.Method(typeof(Ui), "RefreshParts", Type.EmptyTypes);

    private static int uiHullChooseDepth;
    private static Ship? pendingArmamentDefaultsShip;
    private static string pendingArmamentDefaultsContext = string.Empty;
    private static Ship? pendingEquipmentDefaultsShip;
    private static string pendingEquipmentDefaultsContext = string.Empty;
    private static int pendingEquipmentDefaultsAttempts;
    private static Ship? pendingTorpedoDefaultsShip;
    private static string pendingTorpedoDefaultsContext = string.Empty;
    private static readonly Dictionary<long, long> ArmamentDefaultsAppliedHullByShip = new();
    private static readonly Dictionary<long, long> EquipmentDefaultsAppliedHullByShip = new();
    private static readonly Dictionary<long, long> TorpedoDefaultsAppliedHullByShip = new();
    private static bool loggedApply;
    private static bool loggedChoosePartTarget;
    private static bool loggedChangeHullTarget;
    private static bool loggedAddPartTarget;
    private static bool loggedUiUpdateTarget;
    private static bool loggedRefreshWarning;
    private static bool loggedSanityWarning;
    private static bool applyingDefaults;
    private static bool applyingArmamentDefaults;
    private static bool applyingEquipmentDefaults;
    private static bool applyingTorpedoDefaults;

    internal static MethodBase? ChoosePartTarget()
        => AccessTools.Method(typeof(Ui), "ChoosePart", new[] { typeof(PartData), typeof(bool) });

    internal static MethodBase? ChangeHullTarget()
        => AccessTools.Method(typeof(Ship), nameof(Ship.ChangeHull), new[] { typeof(PartData), typeof(Ship.Store), typeof(ShipType), typeof(bool) });

    internal static MethodBase? AddPartTarget()
        => AccessTools.Method(typeof(Ship), nameof(Ship.AddPart), new[] { typeof(Part) });

    internal static MethodBase? UiUpdateTarget()
        => AccessTools.Method(typeof(Ui), "Update", Type.EmptyTypes);

    internal static string ApplySmartRefitComponentDefaults(Ship ship)
    {
        ComponentInstallSummary components = ApplyBestComponents(ship);
        ArmamentInstallSummary armament = HasGun(ship)
            ? ApplyArmamentComponents(ship)
            : ArmamentInstallSummary.Empty;
        EquipmentInstallSummary equipment = HasMainTower(ship)
            ? ApplyEquipmentComponents(ship)
            : EquipmentInstallSummary.Empty;
        TorpedoInstallSummary torpedoes = HasTorpedoLauncher(ship)
            ? ApplyTorpedoComponents(ship)
            : TorpedoInstallSummary.Empty;

        try { ship.CalcWeightAndCost(true, true); }
        catch { }

        return
            $"components={components.Installed}/{components.Attempted} " +
            $"armament={armament.Installed}/{armament.Attempted} " +
            $"equipment={equipment.Installed}/{equipment.Attempted} " +
            $"torpedoes={torpedoes.Installed}/{torpedoes.Attempted}";
    }

    internal static void EnterUiChoosePart(PartData? part)
    {
        if (!IsHull(part) || !IsConstructor())
            return;

        uiHullChooseDepth++;
    }

    internal static void LeaveUiChoosePart(PartData? part)
    {
        if (!IsHull(part) || !IsConstructor())
            return;

        uiHullChooseDepth = Math.Max(0, uiHullChooseDepth - 1);
    }

    internal static void ApplyAfterChangeHull(Ship? ship, PartData? hullData, bool byHuman)
    {
        if (ship == null || !IsHull(hullData) || !IsConstructor())
            return;

        bool fromUiHullPick = uiHullChooseDepth > 0;
        if (!fromUiHullPick && !byHuman)
            return;

        if (!IsHumanMainDesignerShip(ship))
            return;
        if (applyingDefaults)
            return;

        try
        {
            applyingDefaults = true;
            string beforeName = ShipLabel(ship);
            ship.SetRandomName();
            string afterName = ShipLabel(ship);

            float speed = ApplyOptimalSpeed(ship, hullData!);
            float tons = ApplyMaxTonnage(ship, hullData!);
            ApplyRangeCrewAndQuarters(ship);
            ComponentInstallSummary components = ApplyBestComponents(ship);
            ResetArmamentDefaultsFor(ship);
            ResetEquipmentDefaultsFor(ship);
            ResetTorpedoDefaultsFor(ship);
            TryApplyArmamentDefaults(ship, "hull-defaults", refreshUi: false);
            TryApplyEquipmentDefaults(ship, "hull-defaults", refreshUi: false);
            TryApplyTorpedoDefaults(ship, "hull-defaults", refreshUi: false);

            try { ship.CalcWeightAndCost(true, true); }
            catch { }

            try { RefreshHullStatsMethod?.Invoke(ship, Array.Empty<object>()); }
            catch { }

            RefreshConstructorUi();

            if (!loggedApply)
            {
                loggedApply = true;
                Melon<UADVanillaPlusMod>.Logger.Msg(
                    $"{LogPrefix}: applied player hull defaults hull={Token(hullData)} name={LogToken(beforeName)}->{LogToken(afterName)} speed={Fmt(speed)}kn tons={Fmt(tons)} range=VeryHigh crew=100 quarters=Spacious components={components.Installed}/{components.Attempted} rudder={components.RudderLabel}.");
            }
        }
        catch (Exception ex)
        {
            Melon<UADVanillaPlusMod>.Logger.Warning(
                $"{LogPrefix}: failed to apply hull defaults. {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            applyingDefaults = false;
        }
    }

    internal static void QueueArmamentDefaultsAfterAddPart(Ship? ship, Part? part)
    {
        if (ship == null || !IsConstructor())
            return;
        if (!IsHumanMainDesignerShip(ship) || !IsActiveConstructorShip(ship))
            return;
        if (applyingDefaults || applyingArmamentDefaults || applyingEquipmentDefaults || applyingTorpedoDefaults)
            return;

        if (IsGunPart(part))
        {
            pendingArmamentDefaultsShip = ship;
            pendingArmamentDefaultsContext = "first-gun";
        }

        if (IsMainTowerPart(part))
        {
            pendingEquipmentDefaultsShip = ship;
            pendingEquipmentDefaultsContext = "first-main-tower";
            pendingEquipmentDefaultsAttempts = 0;
        }

        if (IsTorpedoPart(part))
        {
            pendingTorpedoDefaultsShip = ship;
            pendingTorpedoDefaultsContext = "first-torpedo";
        }
    }

    internal static void ProcessPendingArmamentDefaults(Ui? ui)
    {
        ProcessPendingArmamentDefaultsOnly(ui);
        ProcessPendingEquipmentDefaults(ui);
        ProcessPendingTorpedoDefaults(ui);
    }

    private static void ProcessPendingArmamentDefaultsOnly(Ui? ui)
    {
        Ship? ship = pendingArmamentDefaultsShip;
        if (ship == null)
            return;

        pendingArmamentDefaultsShip = null;
        string context = string.IsNullOrWhiteSpace(pendingArmamentDefaultsContext) ? "deferred" : pendingArmamentDefaultsContext;
        pendingArmamentDefaultsContext = string.Empty;

        if (!IsConstructor() || !IsHumanMainDesignerShip(ship) || !IsActiveConstructorShip(ship, ui) || !HasGun(ship))
            return;

        TryApplyArmamentDefaults(ship, context, refreshUi: true);
    }

    private static void ProcessPendingEquipmentDefaults(Ui? ui)
    {
        Ship? ship = pendingEquipmentDefaultsShip;
        if (ship == null)
            return;

        string context = string.IsNullOrWhiteSpace(pendingEquipmentDefaultsContext) ? "deferred" : pendingEquipmentDefaultsContext;

        if (!IsConstructor() || !IsHumanMainDesignerShip(ship) || !IsActiveConstructorShip(ship, ui) || !HasMainTower(ship))
        {
            pendingEquipmentDefaultsAttempts++;
            if (pendingEquipmentDefaultsAttempts >= MaxDeferredEquipmentAttempts)
            {
                pendingEquipmentDefaultsShip = null;
                pendingEquipmentDefaultsContext = string.Empty;
                pendingEquipmentDefaultsAttempts = 0;
                Melon<UADVanillaPlusMod>.Logger.Msg(
                    $"{LogPrefix}: equipment defaults skipped context={context} ship={LogToken(ShipLabel(ship))} hasMainTower={HasMainTower(ship)} reason=not-ready-after-defer.");
            }

            return;
        }

        pendingEquipmentDefaultsShip = null;
        pendingEquipmentDefaultsContext = string.Empty;
        pendingEquipmentDefaultsAttempts = 0;

        TryApplyEquipmentDefaults(ship, context, refreshUi: true);
    }

    private static void ProcessPendingTorpedoDefaults(Ui? ui)
    {
        Ship? ship = pendingTorpedoDefaultsShip;
        if (ship == null)
            return;

        pendingTorpedoDefaultsShip = null;
        string context = string.IsNullOrWhiteSpace(pendingTorpedoDefaultsContext) ? "deferred" : pendingTorpedoDefaultsContext;
        pendingTorpedoDefaultsContext = string.Empty;

        if (!IsConstructor() || !IsHumanMainDesignerShip(ship) || !IsActiveConstructorShip(ship, ui) || !HasTorpedoLauncher(ship))
            return;

        TryApplyTorpedoDefaults(ship, context, refreshUi: true);
    }

    internal static void LogChoosePartTarget()
    {
        if (loggedChoosePartTarget)
            return;

        loggedChoosePartTarget = true;
        Melon<UADVanillaPlusMod>.Logger.Msg($"{LogPrefix}: attached Ui.ChoosePart hull marker.");
    }

    internal static void LogChangeHullTarget()
    {
        if (loggedChangeHullTarget)
            return;

        loggedChangeHullTarget = true;
        Melon<UADVanillaPlusMod>.Logger.Msg($"{LogPrefix}: attached Ship.ChangeHull defaults hook.");
    }

    internal static void LogAddPartTarget()
    {
        if (loggedAddPartTarget)
            return;

        loggedAddPartTarget = true;
        Melon<UADVanillaPlusMod>.Logger.Msg($"{LogPrefix}: attached Ship.AddPart armament/equipment/torpedo defaults hook.");
    }

    internal static void LogUiUpdateTarget()
    {
        if (loggedUiUpdateTarget)
            return;

        loggedUiUpdateTarget = true;
        Melon<UADVanillaPlusMod>.Logger.Msg($"{LogPrefix}: attached Ui.Update deferred armament/equipment/torpedo defaults hook.");
    }

    private static float ApplyOptimalSpeed(Ship ship, PartData hullData)
    {
        float rawKnots = Safe(() => hullData.speedLimiter, 0f);
        if (rawKnots <= 0f)
            rawKnots = Safe(() => Ship.speedLimiter(hullData), 0f);
        if (!IsSanePositive(rawKnots, MaxReasonableDesignerSpeedKnots))
        {
            WarnSanityOnce($"skipped speed default rawKnots={Fmt(rawKnots)} hull={Token(hullData)}.");
            return Safe(() => ship.speedMax / KnotsToMetersPerSecond, 0f);
        }

        float minKnots = Safe(() => ship.shipType.speedMin, 0f);
        float maxKnots = Safe(() => ship.shipType.speedMax, MaxReasonableDesignerSpeedKnots);
        if (!IsSanePositive(maxKnots, MaxReasonableDesignerSpeedKnots))
            maxKnots = MaxReasonableDesignerSpeedKnots;
        if (minKnots < 0f || minKnots > maxKnots)
            minKnots = 0f;

        float clampedKnots = Math.Clamp(rawKnots, minKnots, maxKnots);
        float speedKnots = MathF.Floor(clampedKnots + 0.0001f);
        if (speedKnots < minKnots)
            speedKnots = minKnots;
        if (speedKnots > maxKnots)
            speedKnots = maxKnots;

        float speedMetersPerSecond = speedKnots * KnotsToMetersPerSecond;
        ship.SetSpeedMax(speedMetersPerSecond);
        ship.SetEngineCustomSpeed(speedMetersPerSecond);
        return speedKnots;
    }

    private static float ApplyMaxTonnage(Ship ship, PartData hullData)
    {
        float hullMax = Safe(() => hullData.tonnageMax, 0f);
        float shipMax = Safe(() => ship.TonnageMax(), 0f);
        float target = shipMax > 0f ? shipMax : hullMax;
        if (hullMax > 0f && target > 0f)
            target = Math.Min(target, hullMax);

        Player? player = Safe(() => ship.player, null);
        float limit = Safe(() => player?.TonnageLimit(ship.shipType) ?? -1f, -1f);
        if (limit > 0f && target > 0f)
            target = Math.Min(target, limit);
        else if (target <= 0f && limit > 0f)
            target = limit;

        if (IsSanePositive(target, MaxReasonableDesignerTonnage))
            ship.SetTonnage(target);
        else
            WarnSanityOnce($"skipped tonnage default target={Fmt(target)} hullMax={Fmt(hullMax)} shipMax={Fmt(shipMax)} limit={Fmt(limit)} hull={Token(hullData)}.");

        return Safe(() => ship.Tonnage(), target);
    }

    private static void ApplyRangeCrewAndQuarters(Ship ship)
    {
        ship.SetOpRange(VesselEntity.OpRange.VeryHigh, true);
        ship.CurrentCrewQuarters = Ship.CrewQuarters.Spacious;
        ship.CrewTrainingAmount = 100f;
    }

    private static ComponentInstallSummary ApplyBestComponents(Ship ship)
    {
        List<ComponentData> components = AllComponents();
        string[] slots =
        {
            "engine",
            "fuel",
            "boilers",
            "aux_eng",
            "shaft",
            "steering",
            "rudder",
            "armor",
            "barbette",
            "torpedo_belt",
            "multi_bottom",
            "bulkheads",
            "antiflooding",
            "citadel"
        };

        int attempted = 0;
        int installed = 0;
        string rudderLabel = "unchanged";
        foreach (string slot in slots)
        {
            ComponentData? candidate = string.Equals(slot, "rudder", StringComparison.OrdinalIgnoreCase)
                ? BestBalancedRudder(ship, components)
                : BestAvailableComponent(ship, components, slot);
            if (candidate == null)
                continue;

            attempted++;
            if (!Safe(() => ship.InstallComponent(candidate, true), false))
                continue;

            installed++;
            if (string.Equals(slot, "rudder", StringComparison.OrdinalIgnoreCase))
                rudderLabel = Token(candidate);
        }

        return new ComponentInstallSummary(attempted, installed, rudderLabel);
    }

    private static ArmamentInstallSummary TryApplyArmamentDefaults(Ship ship, string context, bool refreshUi)
    {
        if (!IsHumanMainDesignerShip(ship) || applyingArmamentDefaults || !HasGun(ship))
            return ArmamentInstallSummary.Empty;

        long shipKey = ShipKey(ship);
        long hullKey = HullKey(ship);
        if (shipKey == 0L || hullKey == 0L)
            return ArmamentInstallSummary.Empty;
        if (ArmamentDefaultsAppliedHullByShip.TryGetValue(shipKey, out long appliedHullKey) && appliedHullKey == hullKey)
            return ArmamentInstallSummary.Empty;

        try
        {
            applyingArmamentDefaults = true;
            ArmamentInstallSummary summary = ApplyArmamentComponents(ship);
            if (summary.Attempted <= 0)
                return summary;

            ArmamentDefaultsAppliedHullByShip[shipKey] = hullKey;
            if (summary.Installed > 0)
            {
                try { ship.CalcWeightAndCost(true, true); }
                catch { }

                if (refreshUi)
                    RefreshConstructorUi();
            }

            Melon<UADVanillaPlusMod>.Logger.Msg(
                $"{LogPrefix}: applied armament defaults context={context} ship={LogToken(ShipLabel(ship))} installed={summary.Installed}/{summary.Attempted} components={summary.ComponentsText}.");
            return summary;
        }
        catch (Exception ex)
        {
            Melon<UADVanillaPlusMod>.Logger.Warning(
                $"{LogPrefix}: failed to apply armament defaults during {context}. {ex.GetType().Name}: {ex.Message}");
            return ArmamentInstallSummary.Empty;
        }
        finally
        {
            applyingArmamentDefaults = false;
        }
    }

    private static EquipmentInstallSummary TryApplyEquipmentDefaults(Ship ship, string context, bool refreshUi)
    {
        if (!IsHumanMainDesignerShip(ship) || applyingEquipmentDefaults || !HasMainTower(ship))
            return EquipmentInstallSummary.Empty;

        long shipKey = ShipKey(ship);
        long hullKey = HullKey(ship);
        if (shipKey == 0L || hullKey == 0L)
            return EquipmentInstallSummary.Empty;
        if (EquipmentDefaultsAppliedHullByShip.TryGetValue(shipKey, out long appliedHullKey) && appliedHullKey == hullKey)
            return EquipmentInstallSummary.Empty;

        try
        {
            applyingEquipmentDefaults = true;
            EquipmentInstallSummary summary = ApplyEquipmentComponents(ship);
            if (summary.Attempted <= 0)
            {
                Melon<UADVanillaPlusMod>.Logger.Msg(
                    $"{LogPrefix}: equipment defaults skipped context={context} ship={LogToken(ShipLabel(ship))} hasMainTower={HasMainTower(ship)} reason=no-available-components-or-slots.");
                return summary;
            }

            EquipmentDefaultsAppliedHullByShip[shipKey] = hullKey;
            if (summary.Installed > 0)
            {
                try { ship.CalcWeightAndCost(true, true); }
                catch { }

                if (refreshUi)
                    RefreshConstructorUi();
            }

            Melon<UADVanillaPlusMod>.Logger.Msg(
                $"{LogPrefix}: applied equipment defaults context={context} ship={LogToken(ShipLabel(ship))} installed={summary.Installed}/{summary.Attempted} components={summary.ComponentsText}.");
            return summary;
        }
        catch (Exception ex)
        {
            Melon<UADVanillaPlusMod>.Logger.Warning(
                $"{LogPrefix}: failed to apply equipment defaults during {context}. {ex.GetType().Name}: {ex.Message}");
            return EquipmentInstallSummary.Empty;
        }
        finally
        {
            applyingEquipmentDefaults = false;
        }
    }

    private static TorpedoInstallSummary TryApplyTorpedoDefaults(Ship ship, string context, bool refreshUi)
    {
        if (!IsHumanMainDesignerShip(ship) || applyingTorpedoDefaults || !HasTorpedoLauncher(ship))
            return TorpedoInstallSummary.Empty;

        long shipKey = ShipKey(ship);
        long hullKey = HullKey(ship);
        if (shipKey == 0L || hullKey == 0L)
            return TorpedoInstallSummary.Empty;
        if (TorpedoDefaultsAppliedHullByShip.TryGetValue(shipKey, out long appliedHullKey) && appliedHullKey == hullKey)
            return TorpedoInstallSummary.Empty;

        try
        {
            applyingTorpedoDefaults = true;
            TorpedoInstallSummary summary = ApplyTorpedoComponents(ship);
            if (summary.Attempted <= 0)
                return summary;

            TorpedoDefaultsAppliedHullByShip[shipKey] = hullKey;
            if (summary.Installed > 0)
            {
                try { ship.CalcWeightAndCost(true, true); }
                catch { }

                if (refreshUi)
                    RefreshConstructorUi();
            }

            Melon<UADVanillaPlusMod>.Logger.Msg(
                $"{LogPrefix}: applied torpedo defaults context={context} ship={LogToken(ShipLabel(ship))} component={summary.ComponentText}.");
            return summary;
        }
        catch (Exception ex)
        {
            Melon<UADVanillaPlusMod>.Logger.Warning(
                $"{LogPrefix}: failed to apply torpedo defaults during {context}. {ex.GetType().Name}: {ex.Message}");
            return TorpedoInstallSummary.Empty;
        }
        finally
        {
            applyingTorpedoDefaults = false;
        }
    }

    private static ArmamentInstallSummary ApplyArmamentComponents(Ship ship)
    {
        string[][] rankedFamilies =
        {
            new[] { "shell_ratio_main_0" },
            new[] { "shell_ratio_sec_0" },
            new[] { "he_3", "he_2", "he_0", "he_1", "he_4", "he_5" },
            new[] { "ap_5", "ap_2", "ap_1", "ap_0", "ap_4", "ap_3" },
            new[] { "propellant_8", "propellant_7", "propellant_5", "propellant_1", "propellant_0" },
            new[] { "explosive_9", "explosive_8", "explosive_6", "explosive_3", "explosive_1", "explosive_0" },
            new[] { "turret_traverse_5", "turret_traverse_4", "turret_traverse_3", "turret_traverse_2", "turret_traverse_1", "turret_traverse_0" },
            new[] { "gun_reload_4", "gun_reload_3", "gun_reload_2", "gun_reload_1", "gun_reload_0" }
        };

        int attempted = 0;
        int installed = 0;
        List<string> labels = new();
        foreach (string[] rankedKeys in rankedFamilies)
        {
            ComponentData? component = FirstAvailableComponentByKey(ship, rankedKeys, requireExistingShipSlot: false);
            if (component == null)
                continue;

            attempted++;
            if (CurrentComponentMatches(ship, component))
                continue;

            if (!Safe(() => ship.InstallComponent(component, true), false))
                continue;

            installed++;
            labels.Add(Token(component));
        }

        string text = labels.Count == 0 ? "already-set" : string.Join(",", labels.Select(LogToken));
        return new ArmamentInstallSummary(attempted, installed, text);
    }

    private static EquipmentInstallSummary ApplyEquipmentComponents(Ship ship)
    {
        string[][] rankedFamilies =
        {
            new[] { "rangefinder_coinc_5", "rangefinder_coinc_4", "rangefinder_coinc_3", "rangefinder_coinc_2", "rangefinder_coinc_1" },
            new[] { "sonar_3", "sonar_2", "sonar_1", "hydro_3", "hydro_2", "hydro_1" },
            new[] { "radio_2", "radio_1" }
        };

        int attempted = 0;
        int installed = 0;
        List<string> labels = new();
        foreach (string[] rankedKeys in rankedFamilies)
        {
            ComponentData? component = FirstAvailableComponentByKey(ship, rankedKeys, requireExistingShipSlot: false);
            if (component == null)
                continue;

            attempted++;
            if (CurrentComponentMatches(ship, component))
                continue;

            if (!Safe(() => ship.InstallComponent(component, true), false))
                continue;

            installed++;
            labels.Add(Token(component));
        }

        string text = labels.Count == 0 ? "already-set" : string.Join(",", labels.Select(LogToken));
        return new EquipmentInstallSummary(attempted, installed, text);
    }

    private static TorpedoInstallSummary ApplyTorpedoComponents(Ship ship)
    {
        string[] rankedKeys =
        {
            "torpedo_diameter_9",
            "torpedo_diameter_8",
            "torpedo_diameter_7",
            "torpedo_diameter_6",
            "torpedo_diameter_5",
            "torpedo_diameter_4",
            "torpedo_diameter_3",
            "torpedo_diameter_2",
            "torpedo_diameter_1",
            "torpedo_diameter_0"
        };

        ComponentData? component = FirstAvailableComponentByKey(ship, rankedKeys);
        if (component == null)
            return TorpedoInstallSummary.Empty;

        if (CurrentComponentMatches(ship, component))
            return new TorpedoInstallSummary(1, 0, "already-set");

        if (!Safe(() => ship.InstallComponent(component, true), false))
            return new TorpedoInstallSummary(1, 0, "install-failed");

        return new TorpedoInstallSummary(1, 1, LogToken(Token(component)));
    }

    private static ComponentData? FirstAvailableComponentByKey(Ship ship, IEnumerable<string> rankedKeys, bool requireExistingShipSlot = true)
    {
        var components = G.GameData?.components;
        var shipComponents = Safe(() => ship.components, null);
        if (components == null)
            return null;
        if (requireExistingShipSlot && shipComponents == null)
            return null;

        foreach (string key in rankedKeys)
        {
            if (!components.TryGetValue(key, out ComponentData component) || component == null)
                continue;

            CompType? slot = Safe(() => component.typex, null);
            if (slot == null)
                continue;
            if (requireExistingShipSlot && !Safe(() => shipComponents != null && shipComponents.ContainsKey(slot), false))
                continue;
            if (!IsComponentAvailable(ship, component))
                continue;

            return component;
        }

        return null;
    }

    private static bool CurrentComponentMatches(Ship ship, ComponentData component)
    {
        try
        {
            var components = ship.components;
            if (components == null)
                return false;

            CompType slot = component.typex;
            if (!components.TryGetValue(slot, out ComponentData current) || current == null)
                return false;

            return SameComponent(current, component);
        }
        catch
        {
            return false;
        }
    }

    private static bool SameComponent(ComponentData? a, ComponentData? b)
    {
        if (a == null || b == null)
            return false;

        long aPointer = Safe(() => a.Pointer.ToInt64(), 0L);
        long bPointer = Safe(() => b.Pointer.ToInt64(), 0L);
        if (aPointer != 0L && aPointer == bPointer)
            return true;

        return string.Equals(Token(a), Token(b), StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasGun(Ship ship)
    {
        try
        {
            var parts = ship.parts;
            if (parts == null)
                return false;

            foreach (Part part in parts)
            {
                if (IsGunPart(part))
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool HasMainTower(Ship ship)
    {
        try
        {
            var parts = ship.parts;
            if (parts == null)
                return false;

            foreach (Part part in parts)
            {
                if (IsMainTowerPart(part))
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool HasTorpedoLauncher(Ship ship)
    {
        try
        {
            var parts = ship.parts;
            if (parts == null)
                return false;

            foreach (Part part in parts)
            {
                if (IsTorpedoPart(part))
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static ComponentData? BestAvailableComponent(Ship ship, IEnumerable<ComponentData> components, string slot)
        => components
            .Where(component => ComponentMatchesSlot(component, slot))
            .Where(component => IsComponentAvailable(ship, component))
            .OrderByDescending(component => Safe(() => component.order, 0f))
            .ThenByDescending(component => Safe(() => component.tech?.year ?? 0, 0))
            .ThenBy(component => Token(component), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

    private static ComponentData? BestBalancedRudder(Ship ship, IEnumerable<ComponentData> components)
    {
        List<ComponentData> available = components
            .Where(component => ComponentMatchesSlot(component, "rudder"))
            .Where(component => IsComponentAvailable(ship, component))
            .ToList();

        ComponentData? explicitBalanced = available
            .Where(IsBalancedRudder)
            .OrderByDescending(component => Safe(() => component.order, 0f))
            .ThenByDescending(component => Safe(() => component.tech?.year ?? 0, 0))
            .ThenBy(component => Token(component), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return explicitBalanced;
    }

    private static bool IsBalancedRudder(ComponentData component)
    {
        string name = SafeString(() => component.name);
        if (string.Equals(name, "rudder_0", StringComparison.OrdinalIgnoreCase))
            return true;

        string label = $"{SafeString(() => component.nameShort)} {SafeString(() => component.nameUi)}";
        return label.Contains("balanced", StringComparison.OrdinalIgnoreCase) &&
            !label.Contains("semi", StringComparison.OrdinalIgnoreCase) &&
            !label.Contains("unbalanced", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsComponentAvailable(Ship ship, ComponentData component)
    {
        try
        {
            string reason;
            return component.enabled && ship.IsComponentAvailable(component, out reason);
        }
        catch
        {
            return false;
        }
    }

    private static bool ComponentMatchesSlot(ComponentData? component, string slot)
    {
        if (component == null)
            return false;

        string name = SafeString(() => component.name);
        string type = SafeString(() => component.type);
        return slot switch
        {
            "engine" => name.StartsWith("main_engine_", StringComparison.OrdinalIgnoreCase) || Same(type, "engine"),
            "fuel" => name.StartsWith("fuel_", StringComparison.OrdinalIgnoreCase) || Same(type, "fuel"),
            "boilers" => name.StartsWith("boiler_", StringComparison.OrdinalIgnoreCase) || Same(type, "boilers") || Same(type, "boiler"),
            "aux_eng" => name.StartsWith("aux_engine_", StringComparison.OrdinalIgnoreCase) || Same(type, "aux_eng"),
            "shaft" => name.StartsWith("drive_shaft_", StringComparison.OrdinalIgnoreCase) || Same(type, "shaft"),
            "steering" => name.StartsWith("steering_gear_", StringComparison.OrdinalIgnoreCase) || Same(type, "steering"),
            "rudder" => name.StartsWith("rudder_", StringComparison.OrdinalIgnoreCase) || Same(type, "rudder"),
            "armor" => name.StartsWith("armor_", StringComparison.OrdinalIgnoreCase) || Same(type, "armor"),
            "barbette" => name.StartsWith("barbette_thickness_", StringComparison.OrdinalIgnoreCase) || Same(type, "barbette"),
            "torpedo_belt" => name.StartsWith("torpedo_belt_", StringComparison.OrdinalIgnoreCase) || Same(type, "torpedo_belt"),
            "multi_bottom" => name.StartsWith("multi_bottom_", StringComparison.OrdinalIgnoreCase) || Same(type, "multi_bottom"),
            "bulkheads" => name.StartsWith("buklheads_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("bulkheads_", StringComparison.OrdinalIgnoreCase) || Same(type, "bulkheads"),
            "antiflooding" => name.StartsWith("Anti_Flooding_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("anti_flooding_", StringComparison.OrdinalIgnoreCase) || Same(type, "antiflooding"),
            "citadel" => name.StartsWith("Citadel_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("citadel_", StringComparison.OrdinalIgnoreCase) || Same(type, "citadel"),
            "rangefinder" => name.StartsWith("rangefinder_", StringComparison.OrdinalIgnoreCase) || Same(type, "rangefinder"),
            "sonar" => name.StartsWith("hydro_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("sonar_", StringComparison.OrdinalIgnoreCase) || Same(type, "sonar"),
            "radio" => name.StartsWith("radio_", StringComparison.OrdinalIgnoreCase) || Same(type, "radio"),
            _ => false
        };
    }

    private static List<ComponentData> AllComponents()
    {
        List<ComponentData> result = new();
        try
        {
            var components = G.GameData?.components;
            if (components == null)
                return result;

            foreach (var pair in components)
            {
                if (pair.Value != null)
                    result.Add(pair.Value);
            }
        }
        catch
        {
        }

        return result;
    }

    private static void ResetArmamentDefaultsFor(Ship ship)
    {
        long key = ShipKey(ship);
        if (key != 0L)
            ArmamentDefaultsAppliedHullByShip.Remove(key);
    }

    private static void ResetEquipmentDefaultsFor(Ship ship)
    {
        long key = ShipKey(ship);
        if (key != 0L)
            EquipmentDefaultsAppliedHullByShip.Remove(key);
    }

    private static void ResetTorpedoDefaultsFor(Ship ship)
    {
        long key = ShipKey(ship);
        if (key != 0L)
            TorpedoDefaultsAppliedHullByShip.Remove(key);
    }

    private static void RefreshConstructorUi()
    {
        try
        {
            Ui? ui = G.ui;
            if (ui == null)
                return;

            try { OnConShipChangedMethod?.Invoke(ui, new object[] { false }); }
            catch (Exception ex) { WarnRefreshOnce("OnConShipChanged", ex); }

            try { ui.RefreshConstructorInfo(); }
            catch (Exception ex) { WarnRefreshOnce("RefreshConstructorInfo", ex); }

            try { RefreshPartsMethod?.Invoke(ui, Array.Empty<object>()); }
            catch (Exception ex) { WarnRefreshOnce("RefreshParts", ex); }
        }
        catch (Exception ex)
        {
            WarnRefreshOnce("refresh", ex);
        }
    }

    private static void WarnRefreshOnce(string phase, Exception ex)
    {
        if (loggedRefreshWarning)
            return;

        loggedRefreshWarning = true;
        Melon<UADVanillaPlusMod>.Logger.Warning(
            $"{LogPrefix}: designer UI refresh failed at {phase}. {ex.GetType().Name}: {ex.Message}");
    }

    private static bool IsHumanMainDesignerShip(Ship ship)
    {
        Player? owner = Safe(() => ship.player, null) ?? PlayerController.Instance;
        if (owner == null)
            return false;

        return Safe(() => owner.isMain && !owner.isAi, false);
    }

    private static bool IsActiveConstructorShip(Ship ship, Ui? ui = null)
    {
        Ui? effectiveUi = ui ?? Safe(() => G.ui, null);
        Ship? active = Safe(() => effectiveUi?.mainShip, null);
        if (active == null)
            return false;

        long activeKey = ShipKey(active);
        long shipKey = ShipKey(ship);
        return activeKey != 0L && activeKey == shipKey;
    }

    private static bool IsConstructor()
        => Safe(() => GameManager.IsConstructor, false);

    private static bool IsHull(PartData? part)
        => Safe(() => part?.isHull == true, false);

    private static bool IsGunPart(Part? part)
        => Safe(() => part?.data?.isGun == true, false);

    private static bool IsTorpedoPart(Part? part)
        => Safe(() => part?.data?.isTorpedo == true, false);

    private static bool IsMainTowerPart(Part? part)
    {
        PartData? data = Safe(() => part?.data, null);
        if (data == null)
            return false;

        if (Safe(() => data.isTowerMain, false))
            return true;

        string name = SafeString(() => data.name);
        string type = SafeString(() => data.type);
        return name.Contains("tower_main", StringComparison.OrdinalIgnoreCase)
            || type.Contains("tower_main", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSanePositive(float value, float maxInclusive)
        => value > 0f && value <= maxInclusive && !float.IsNaN(value) && !float.IsInfinity(value);

    private static void WarnSanityOnce(string detail)
    {
        if (loggedSanityWarning)
            return;

        loggedSanityWarning = true;
        Melon<UADVanillaPlusMod>.Logger.Warning($"{LogPrefix}: {detail}");
    }

    private static bool Same(string? value, string expected)
        => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static string Token(BaseData? data)
        => string.IsNullOrWhiteSpace(SafeString(() => data?.name)) ? "<unknown>" : SafeString(() => data?.name);

    private static string ShipLabel(Ship ship)
    {
        string name = SafeString(() => ship.Name(false, false, false, false, true));
        return string.IsNullOrWhiteSpace(name) ? "<unnamed>" : name;
    }

    private static long ShipKey(Ship ship)
        => Safe(() => ship.Pointer.ToInt64(), 0L);

    private static long HullKey(Ship ship)
        => Safe(() => ship.hull?.data?.Pointer.ToInt64() ?? 0L, 0L);

    private static string LogToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "<empty>";

        return value.Trim().Replace(' ', '_');
    }

    private static string Fmt(float value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string SafeString(Func<string?> getter, string fallback = "")
    {
        try
        {
            return getter() ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static T Safe<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter();
        }
        catch
        {
            return fallback;
        }
    }

    private readonly record struct ComponentInstallSummary(int Attempted, int Installed, string RudderLabel);
    private readonly record struct ArmamentInstallSummary(int Attempted, int Installed, string ComponentsText)
    {
        internal static ArmamentInstallSummary Empty { get; } = new(0, 0, "none");
    }

    private readonly record struct EquipmentInstallSummary(int Attempted, int Installed, string ComponentsText)
    {
        internal static EquipmentInstallSummary Empty { get; } = new(0, 0, "none");
    }

    private readonly record struct TorpedoInstallSummary(int Attempted, int Installed, string ComponentText)
    {
        internal static TorpedoInstallSummary Empty { get; } = new(0, 0, "none");
    }
}

[HarmonyPatch]
internal static class DesignNewHullDefaultsChoosePartPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
        => DesignNewHullDefaultsPatch.ChoosePartTarget();

    [HarmonyPrepare]
    private static bool Prepare()
    {
        bool found = TargetMethod() != null;
        if (found)
            DesignNewHullDefaultsPatch.LogChoosePartTarget();
        else
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP designer hull defaults: Ui.ChoosePart target not found; hull-pick scoping disabled.");

        return found;
    }

    [HarmonyPrefix]
    private static void Prefix(PartData part)
        => DesignNewHullDefaultsPatch.EnterUiChoosePart(part);

    [HarmonyFinalizer]
    private static void Finalizer(PartData part)
        => DesignNewHullDefaultsPatch.LeaveUiChoosePart(part);
}

[HarmonyPatch]
internal static class DesignNewHullDefaultsChangeHullPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
        => DesignNewHullDefaultsPatch.ChangeHullTarget();

    [HarmonyPrepare]
    private static bool Prepare()
    {
        bool found = TargetMethod() != null;
        if (found)
            DesignNewHullDefaultsPatch.LogChangeHullTarget();
        else
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP designer hull defaults: Ship.ChangeHull target not found; defaults disabled.");

        return found;
    }

    [HarmonyPostfix]
    private static void Postfix(Ship __instance, PartData data, bool byHuman)
        => DesignNewHullDefaultsPatch.ApplyAfterChangeHull(__instance, data, byHuman);
}

[HarmonyPatch]
internal static class DesignNewHullDefaultsAddPartPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
        => DesignNewHullDefaultsPatch.AddPartTarget();

    [HarmonyPrepare]
    private static bool Prepare()
    {
        bool found = TargetMethod() != null;
        if (found)
            DesignNewHullDefaultsPatch.LogAddPartTarget();
        else
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP designer hull defaults: Ship.AddPart target not found; first-gun armament defaults disabled.");

        return found;
    }

    [HarmonyPostfix]
    private static void Postfix(Ship __instance, Part part)
        => DesignNewHullDefaultsPatch.QueueArmamentDefaultsAfterAddPart(__instance, part);
}

[HarmonyPatch]
internal static class DesignNewHullDefaultsUiUpdatePatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
        => DesignNewHullDefaultsPatch.UiUpdateTarget();

    [HarmonyPrepare]
    private static bool Prepare()
    {
        bool found = TargetMethod() != null;
        if (found)
            DesignNewHullDefaultsPatch.LogUiUpdateTarget();
        else
            Melon<UADVanillaPlusMod>.Logger.Warning("UADVP designer hull defaults: Ui.Update target not found; deferred armament defaults disabled.");

        return found;
    }

    [HarmonyPostfix]
    private static void Postfix(Ui __instance)
        => DesignNewHullDefaultsPatch.ProcessPendingArmamentDefaults(__instance);
}
