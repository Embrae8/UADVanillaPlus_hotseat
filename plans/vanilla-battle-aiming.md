# Vanilla Battle Aiming And Hit Chance

Last updated: 2026-05-17.

## Scope

This note summarizes the current local read of vanilla Ultimate Admiral:
Dreadnoughts battle aiming, especially the player-visible behavior where a ship
starts targeting another ship at very low or zero accuracy, the displayed chance
climbs over time, and then can drop after a salvo.

This is a source-grounded mechanics note, not a complete reimplementation of the
formula. The fuller IL for `Ship.AimAndShoot(...)` and `Ship.HitChance(...)` is
large and branchy, so the safest use of this note is as a map of the important
state and control flow before adding diagnostics or balance changes.

Important local sources:

- `E:\Codex\cpp2il_uad_diffable\DiffableCs\Assembly-CSharp\Ship.cs`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\Ship.txt`
- `E:\Codex\cpp2il_uad_diffable\DiffableCs\Assembly-CSharp\Ui.cs`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\Ui.txt`

## Short Version

Vanilla aiming is persistent runtime state, not just a one-shot hit-chance
calculation.

Each ship tracks an `Aim` record per gun group and side. That record stores the
current target, aim progress, manual-targeting state, recent bearing/range
samples, the latest hit chance, progress delta, and whether the current firing
angle is acceptable.

The visible accuracy climb is mainly `Aim.progress` accumulating as the gun
group observes and ranges the target. The post-shot drop appears to be vanilla
reducing or recalculating that progress after firing based on bearing/range
change, rangefinding, lock state, and other dynamic penalties. The final UI hit
chance is broader than aim progress: it combines gun/range curves, design stats,
crew, weather, target size, speed, turning, smoke, damage, and current aim
state.

## Key Runtime State

`Ship.Aim` is the core state object:

- `target`: target ship for this gun group.
- `progress`: current aim/rangefinding progress.
- `manual`: whether this aim came from manual targeting.
- `lastBearing`: previous bearing sample used to penalize target solution
  changes.
- `lastRange`: previous range sample used to penalize target solution changes.
- `progressDelta`: recent progress change.
- `lastHitChance`: latest computed hit chance.
- `isAngleFine`: whether the firing angle is currently acceptable.

Skeleton evidence:

- `Ship.Aim` fields are in `Ship.cs` around line 3114.
- Ship has two aim dictionaries: `enemies` and `enemiesLeftSide`, both mapping
  `PartData` gun groups to `Aim` records.

## Battle Update Flow

The main battle update path calls:

1. `Ship.AILogic(isPaused)`.
2. `Ship.AimAndShoot(isPaused)`.

Inside `AimAndShoot(...)`, vanilla broadly does this:

1. Skips work if paused, dead, disabled, or not allowed to shoot by mission
   rules.
2. Rebuilds weapon data with `CWeap(...)` as needed.
3. Iterates gun groups by range.
4. Checks whether the group is guns or torpedoes, main or secondary, and whether
   the weapon type is allowed to shoot.
5. Calls `HitChanceCalcValue(...)` periodically through `Util.DoRarely(...)`.
6. Calls `InitializeAim(gunPlace, gunGroup)` so each side dictionary has an aim
   record for the active gun group.
7. Chooses the side-specific aim dictionary, finds guns that can rotate and
   shoot, and calls `CommonProgressForSameCaliber(...)`.
8. Chooses or validates a target via `FindEnemyForOtherGuns(...)` or
   `FindEnemyForBowSternGuns(...)`.
9. Computes current hit chance with `Ship.HitChance(...)`.
10. Uses ammo-saving thresholds and normal thresholds to decide whether to wait
    or fire.
11. When firing, calls `GetCalcHitPoint(...)`, `Shell.Create(...)`,
    `Ui.RegisterMadeShot(...)`, and visual effects such as `Effect.GunShoot(...)`.
12. Updates aim progress, last bearing, last range, progress delta, and last hit
    chance as the loop continues.

Important IL locations:

- `Ship.AimAndShoot(System.Boolean isPaused)` in `Ship.txt`, around line 58677.
- `Ship.InitializeAim(...)` in `Ship.txt`, around line 73761.
- `Ship.CommonProgressForSameCaliber(...)` in `Ship.txt`, around line 73287.
- `Ship.HitChance(...)` in `Ship.txt`, around line 171904.
- `Ship.GetCalcHitPoint(...)` in `Ship.txt`, around line 93700.

## Why Accuracy Starts Low And Climbs

New aim records are created with default `Aim()` in `InitializeAim(...)`.
Default `Aim.progress` starts at zero unless restored from battle save state.

The climb comes from the aim update path in `AimAndShoot(...)`. When a valid
target and usable guns exist, vanilla updates `Aim.progress` using:

- gun data, especially the gun aim parameter at `GameData.GunData(...)+52`;
- ship stat effect `"aim"`;
- an additional ship/stat lookup keyed by `"aim"`;
- current range relative to weapon range;
- `aim_close_modifier` and `aim_far_modifier`;
- crew/control penalties such as `crew_control_penalty_aiming`;
- rangefinding/lock conversion params such as `aim_convertion_lock` and
  `aim_convertion_lock_to_unlocked`;
- same-caliber sharing through `CommonProgressForSameCaliber(...)`.

The relevant progress-write pattern appears in `AimAndShoot(...)`:

- existing progress is read from the `Aim` object;
- vanilla computes a decrease/lock/conversion amount;
- vanilla adds progress back using gun data, `"aim"` stat effects, range
  modifiers, and crew/control factors;
- progress is clamped to a bounded range before storing it back.

The practical effect is the familiar "solution building" curve: a gun group
starts with little confidence and becomes more accurate as it keeps a stable
target solution.

## Why Accuracy Can Drop After A Shot

The post-shot drop also appears to be intentional vanilla behavior.

In the same aim update block, vanilla compares current bearing/range information
against `Aim.lastBearing` and `Aim.lastRange`. When the target solution changes,
it applies penalties and decreases progress before adding new progress. The
visible drop after a salvo can therefore come from the firing cycle exposing a
new solution state, the target moving, the firing ship moving or turning, the
gun group losing a clean angle, or the range/bearing samples changing enough to
reduce confidence.

Relevant parameter names observed in the IL:

- `aim_bearing_penalty_max`
- `aim_bearing_change_convert`
- `aim_range_penalty_max`
- `aim_range_change_convert`
- `aim_decrease_factor`
- `aim_decrease_base`
- `aim_convertion_lock`
- `aim_convertion_lock_to_unlocked`

The update also stores:

- `lastBearing` from the current solution bearing;
- `lastRange` from the current measured range;
- `progressDelta` from the difference between new and old progress;
- `lastHitChance` from the latest `HitChance(...)` result.

That matches the observed UI behavior: the value can climb while the solution is
stable, drop when the shot cycle or target geometry changes, then climb again.

## What `HitChance(...)` Adds On Top

`Aim.progress` is only one part of the final displayed accuracy.

`Ship.HitChance(...)` combines many sources, including:

- weapon range and gun data:
  - `Part.WeaponRange(...)`
  - `GunData.HitChanceMaxRange(...)`
  - `GunData.HitChanceMult(...)`
  - `GunData.HitChanceCurver(...)`
- crew and ship design accuracy:
  - crew long-range penalties for main and secondary guns;
  - `Ship.StatEffect("accuracy")`;
  - `Ship.StatEffect("accuracy_long")`;
  - `Ship.TechTurretAccuracy(...)`;
- environment:
  - `BattleManager.GetDaytimeAccuracy(...)`;
  - weather accuracy;
  - wind accuracy;
  - waves accuracy;
  - sun glare handling;
- obstruction and visibility:
  - smoke obstruction;
  - obscured visibility penalty in the firing path;
- target and ship dynamics:
  - target size / `hit_size`;
  - own speed and cruise accuracy effects;
  - target speed effects;
  - own turning and target turning effects;
- damage:
  - conning tower damage accuracy loss;
  - fire control damage accuracy loss;
  - flooding and instability penalties;
- aim/rangefinding:
  - target confidence;
  - `Aim.progress`;
  - `"RangeFinding"` / `"RangeFound"` style UI reasons.

Because of this, the displayed number can move even when aim progress is not the
only thing changing. A target turning, crossing smoke, changing speed, moving in
or out of range bands, or damaging fire-control systems can all affect the same
UI accuracy value.

## UI And Shot Counters

`Ui.RefreshShootInfo(...)` owns the displayed shoot-info panel fields:

- `shootInfoRangeValue`
- `shootInfoAccuracyValue`
- `shootInfoPenetrationValue`
- `shootInfoAccuracy`

`Ui.RegisterMadeShot(Part from, Ship to, int shots)` updates shot counters and
target-specific shot history after a shot is fired. It is not the main aim
progress update, but it is part of the visible shooting feedback loop.

Important UI locations:

- `Ui.RefreshShootInfo(System.Boolean force = false)` in `Ui.txt`, around line
  84243.
- `Ui.RegisterMadeShot(Part from, Ship to, int shots)` in `Ui.txt`, around line
  291827.

## Same-Caliber Sharing

`CommonProgressForSameCaliber(...)` means aim progress is not always isolated to
one individual turret. The method filters gun groups and aim records, finds
same-caliber or related groups, and propagates a common progress value through
matching `Aim` records.

The practical effect is that guns of the same caliber can benefit from shared
rangefinding/progress, which explains why several groups can appear to settle
onto a target together rather than each starting from scratch.

## Save/Load Note

Aim state is serialized into `AimStore`:

- target GUID;
- progress;
- manual flag;
- optional last bearing;
- optional last range;
- optional progress delta;
- optional last hit chance.

This means battle saves can restore existing aim progress instead of every gun
group always starting fresh at zero.

## Modding Implications

For UAD:VP, aiming and hit chance are hot battle paths. Any diagnostics or
changes should be narrow and event-focused.

Safer diagnostic points:

- battle start summaries from ship/gun design data;
- occasional `Aim.progress` snapshots gated to selected ships or first few
  salvos;
- `Ui.RefreshShootInfo(...)` read-only sampling if the goal is to understand
  what the player sees;
- shot-time summaries around `Ui.RegisterMadeShot(...)` or nearby firing
  context, if kept very lightweight.

Riskier points:

- broad hooks inside `Ship.HitChance(...)`;
- broad hooks inside `Ship.AimAndShoot(...)`;
- per-projectile `Shell.Update(...)`;
- repeated reflection or dictionary scans every frame.

If VP needs an aiming diagnostic, prefer compact one-shot or rate-limited logs:

- ship name;
- gun group;
- target name;
- progress;
- progress delta;
- last hit chance;
- displayed hit chance if safely available;
- reason/context such as target changed, shot fired, or range/bearing changed.

Avoid adding normal always-on per-frame logs here. The vanilla loop is already
large, and combat can involve many ships, gun groups, barrels, and projectiles.

## Current Confidence

High confidence:

- Aim is stored per gun group through `Ship.Aim`.
- `Aim.progress` is persistent and saved.
- `AimAndShoot(...)` is the main battle aiming/firing loop.
- `HitChance(...)` combines aim with many static and dynamic modifiers.
- Bearing/range change penalties and aim-decrease parameters exist.
- The observed climb/drop behavior matches the inspected control flow.

Medium confidence:

- The exact post-shot drop is probably the same progress-decrease path reacting
  to changed bearing/range, lock state, firing geometry, or target movement.
  The IL strongly supports this, but the exact branch taken in a live battle
  should be confirmed with a small targeted diagnostic before balancing around
  it.

Open questions:

- Which single branch most often causes the visible drop after a normal salvo?
- How much of the drop is from shot/reload cadence versus target motion or
  own-ship maneuvering?
- Does the UI display one selected gun group's `lastHitChance`, a recomputed
  panel value, or a blended/representative value in all cases?
- Are AI and player ships using identical target confidence and ammo-threshold
  behavior, or do AI shooting modes change the wait/fire threshold materially?

