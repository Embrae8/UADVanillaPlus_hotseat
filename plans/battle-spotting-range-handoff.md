# Battle Spotting Range Handoff

Context: thinker-session handoff for the dedicated VP builder session. Do not
treat this doc as an implemented fix.

## Copy/Paste Handoff

````markdown
# Battle Spotting Range Builder Handoff

Please implement a VP battle option that buffs spotter spotting range for both
player and AI ships.

Requested menu options:

- `Vanilla`
- `3x` default
- `5x`
- `10x`

This should be symmetric. Do not gate it to player ships or the main player.

Vanilla evidence from `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\Ship.txt`:

- `Ship.Update()` periodically runs the `detect_ships` pass.
- That pass checks enemies with `Ship.DoesDetect(spotter, target)`.
- `DoesDetect(...)` compares horizontal distance against `GetDetectionRange(spotter, target)`.
- `GetDetectionRange(...)` effectively uses:

```text
(target.GetVisibilityRange() + spotter.GetSpottingRange())
    * weather/day/wind/waves detection modifier
    * smoke modifier
```

The requested lever is `spotter.GetSpottingRange()`. Vanilla
`GetSpottingRange()` sums ship `spot` + `tspot`; towers carry most of the useful
`tspot` values. This should not modify target visibility, weather detection,
smoke, or firing reveal behavior.

Suggested implementation:

- Add a typed setting to `UADVanillaPlus/GameData/ModSettings.cs`.
- Use key `uadvp_battle_spotting_range_mode`.
- Add enum values `Vanilla = 1`, `X3 = 3`, `X5 = 5`, `X10 = 10`.
- Default load should be `X3`.
- Add helper text/multiplier methods and include the selected mode in `CurrentSettingsText()`.
- Add a `Battle Spotting` segmented option under the Battle tab in
  `UADVanillaPlus/Harmony/InGameOptionsMenuPatch.cs`.
- Labels should be `Vanilla`, `3x`, `5x`, `10x`.

Patch target:

```csharp
[HarmonyPatch(typeof(Ship), nameof(Ship.GetSpottingRange))]
internal static class BattleSpottingRangePatch
{
    [HarmonyPostfix]
    private static void Postfix(ref float __result)
    {
        float multiplier = ModSettings.BattleSpottingRangeMultiplier(ModSettings.BattleSpottingRangeMode);
        if (Math.Abs(multiplier - 1f) > 0.001f)
            __result *= multiplier;
    }
}
```

Important nuance: this multiplies only the spotter contribution, so the final
range becomes:

```text
target visibility + (spotter spotting * multiplier)
```

That matches the request. If playtesting wants an even stronger effect later,
the follow-up would be a separate total-detection-range multiplier around
`GetDetectionRange(...)`.

Verification:

- Option appears under `UAD:VP Options -> Battle`.
- Clean/default profile selects `3x`.
- `Vanilla`, `3x`, `5x`, and `10x` persist through menu reopen/restart.
- Startup/settings log includes Battle Spotting mode.
- Player ships spot enemies sooner than vanilla at `3x`.
- AI ships also spot player ships sooner than vanilla at `3x`.
- If the option remains live-changeable in battle, later detection passes use the new value.
- `Vanilla` returns the original `GetSpottingRange()` result.
- Smoke still reduces detection through vanilla `behind_smoke_detection`.
- Firing reveal through `CheckSpotting()` still works.

Build reminders:

- Builder session owns source edits, version bump, build, DLL copy, commit, push, and release.
- Before building, bump `ModInfo.cs`, `Properties/AssemblyInfo.cs`, and README current version together.
- Build with the workspace-local command from `AGENTS.md`.
- Copy the DLL directly after build per `AGENTS.md`.
- Update README Battle features if this ships.
````

## Requested Feature

Add a configurable VP battle option that buffs spotter spotting range for both
player and AI ships.

Menu choices requested:

- `Vanilla`
- `3x` default
- `5x`
- `10x`

This should be symmetric. Do not gate it to the main player or human ships.

## Vanilla Spotting Evidence

The relevant surface-battle detection path is in vanilla `Ship`:

- `Ship.Update()` periodically runs the `detect_ships` pass.
- That pass iterates actual battle ships and calls `Ship.DoesDetect(spotter, target)`.
- On success, the target gets `ShowForOthers(true)` and the spotter reports
  `$Ui_Battle_0spotted1`.

The detection check is effectively:

```text
horizontal distance < GetDetectionRange(spotter, target)
```

`GetDetectionRange(spotter, target)` builds range from:

```text
(target.GetVisibilityRange() + spotter.GetSpottingRange())
    * weather/day/wind/waves detection modifier
    * smoke modifier
```

The important user-requested lever is `spotter.GetSpottingRange()`, not the
target's own visibility. Vanilla `GetSpottingRange()` sums the ship's `spot`
and `tspot` stat values. Hulls usually have low or zero `spot`; towers carry
most of the useful `tspot` values.

Weather detection values are cached in `BattleManager.WeatherAllDetection` via
`BattleManager.UpdateWeatherDetectionValues()`. VP's existing Always Sunny
patch already calls that refresh after forcing battle weather, so this feature
does not need to touch the weather cache.

Smoke still applies through vanilla `behind_smoke_detection`, default `0.6`.
Firing can temporarily reveal an unspotted ship via `CheckSpotting()` and
`RevealedUntil`; keep that separate from this feature unless later testing says
the reveal behavior feels wrong.

Useful source anchors:

```text
E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\Ship.txt
  Method: System.Void Update()
  Method: System.Boolean DoesDetect(Ship spotter, Ship whom)
  Method: System.Single GetDetectionRange(Ship spotter, Ship whom)
  Method: System.Single GetVisibilityRange()
  Method: System.Single GetSpottingRange()
  Method: System.Void CheckSpotting()

E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\BattleManager.txt
  Method: System.Void UpdateWeatherDetectionValues()
  Method: System.Single GetDaytimeDetection()
  Method: System.Single GetWeatherDetection()
  Method: System.Single GetWindDetection()
  Method: System.Single GetWavesDetection()

E:\Codex\UADRealismDIP\TweaksAndFixes\Default_Files\UAD_Files\params.csv
  reveal_position,5,chance to become revelead after a shot if the ship is out of spotting range
  behind_smoke_detection,0.6,penalty of detection when behind smoke
  obscured_visibility_penalty,0.5,penalty when ship is not directly visible but become spotted when it fires
```

## Suggested Builder Implementation

Use the existing VP option patterns:

- `UADVanillaPlus/GameData/ModSettings.cs`
- `UADVanillaPlus/Harmony/InGameOptionsMenuPatch.cs`
- existing enum-style option: `AccuracyPenaltyMode`
- existing segmented Battle menu options: `Battle Weather`, `Accuracy Penalties`

Suggested setting shape:

```csharp
private const string BattleSpottingRangeModeKey = "uadvp_battle_spotting_range_mode";

internal enum BattleSpottingRangeMode
{
    Vanilla = 1,
    X3 = 3,
    X5 = 5,
    X10 = 10,
}
```

Default load should be `X3`:

```csharp
PlayerPrefs.GetInt(BattleSpottingRangeModeKey, (int)BattleSpottingRangeMode.X3)
```

Add helpers similar to the accuracy-penalty helpers:

```csharp
internal static BattleSpottingRangeMode BattleSpottingRangeMode { get; set; }
internal static float BattleSpottingRangeMultiplier(BattleSpottingRangeMode mode)
internal static string BattleSpottingRangeModeText(BattleSpottingRangeMode mode)
```

Add this setting to `CurrentSettingsText()` so `LogCurrentSettings(...)`
includes it after changes.

Add a Battle-section segmented option in `InGameOptionsMenuPatch.BuildSectionPane`:

```text
Label: Battle Spotting
Tooltip: Increases ship spotting range for both player and AI ships. Vanilla uses the game's original spot/tower spotting values.
Segments: Vanilla, 3x, 5x, 10x
```

Keep it changeable during battle unless testing shows cached UI state becomes
misleading. If implemented as a postfix on `GetSpottingRange()`, the next
vanilla detection pass should pick up the new multiplier.

## Patch Target

Prefer a narrow postfix on the public method:

```csharp
[HarmonyPatch(typeof(Ship), nameof(Ship.GetSpottingRange))]
internal static class BattleSpottingRangePatch
{
    [HarmonyPostfix]
    private static void Postfix(ref float __result)
    {
        float multiplier = ModSettings.BattleSpottingRangeMultiplier(ModSettings.BattleSpottingRangeMode);
        if (Math.Abs(multiplier - 1f) > 0.001f)
            __result *= multiplier;
    }
}
```

Reasons:

- It multiplies exactly the requested lever: spotter `spot + tspot`.
- It applies equally to player and AI because it does not inspect owner/player.
- It avoids replacing `Ship.GetDetectionRange(...)`, which is private static
  and mixes visibility, weather, and smoke.
- Runtime cost should be tiny if the postfix only reads cached ModSettings and
  multiplies a float.

Important nuance: this does not multiply total detection range. It multiplies
the spotter contribution before vanilla adds target visibility:

```text
new range = target visibility + (spotter spotting * multiplier)
```

That matches the user request. If later playtesting wants more dramatic spotting
than that, the follow-up would be a separate option to multiply total detection
range in `GetDetectionRange(...)`.

## Naming Notes

Recommended user-facing name:

```text
Battle Spotting
```

Recommended mode labels:

```text
Vanilla / 3x / 5x / 10x
```

Avoid implying this is player-only or radar-only. It affects the ship stat path
that includes towers and hull spotting values.

## Verification Checklist

Build-session verification should include:

- New option appears under UAD:VP Options -> Battle.
- Default selection on a clean profile is `3x`.
- Toggling to `Vanilla`, `5x`, and `10x` persists through menu reopen and game restart.
- Startup/settings log includes the selected Battle Spotting mode.
- In battle, player ships spot enemies sooner than vanilla at `3x`.
- In battle, AI ships also spot player ships sooner than vanilla at `3x`.
- Changing the option mid-battle affects subsequent detections without requiring battle reload, if the builder keeps it live-changeable.
- `Vanilla` mode behaves like no VP patch by returning the original `GetSpottingRange()` result.
- Smoke still reduces detection through vanilla `behind_smoke_detection`.
- Firing reveal behavior through `CheckSpotting()` still works and does not become noisy in logs.

Useful optional diagnostic while validating:

```text
Log a few first detections with spotter name, target name, mode, base spotting,
multiplied spotting, target visibility, and final distance/range comparison.
Remove or strongly gate this before leaving the feature in a normal build.
```

## Build/Publish Reminders

- Builder session only should modify source, bump versions, build, copy DLLs,
  commit, push, or publish.
- Before building, bump `ModInfo.cs`, `Properties/AssemblyInfo.cs`, and README
  current version together per `AGENTS.md`.
- Use the workspace-local build command from `AGENTS.md`, including
  `/p:RestoreConfigFile=E:\Codex\UADVanillaPlus\NuGet.Config`.
- After a successful build, copy directly to the game `Mods` folder and report
  a lock if the game has the DLL open.
- If this ships as a player-facing feature, update README's Battle feature list
  with concise wording and the default `3x` behavior.
