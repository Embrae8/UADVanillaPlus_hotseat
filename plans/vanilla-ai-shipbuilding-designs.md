# Vanilla AI Shipbuilding And Design Generation

## Scope

This note is a vanilla-focused reference for how Ultimate Admiral: Dreadnoughts appears to decide:

- whether an AI nation should build new ships,
- which ship type/design it tries to build,
- where the design candidates come from,
- how new random designs are created when no suitable design exists.

The goal is a baseline for future VP ideas. This is not an implementation plan yet, and it deliberately separates confirmed source behavior from likely mod hook points.

## Short Take

AI shipbuilding is not a single "fleet planner" that first calculates a perfect target navy and then designs toward it.

The monthly campaign path is more incremental:

1. `AiManageFleet(...)` runs for an AI player.
2. It scraps old ships, sets ship roles, and deletes stale designs.
3. Depending on the campaign design mode, it either tries to generate/borrow designs or uses predefined campaign designs.
4. It checks the player's cash/income, crew pool, existing under-construction tonnage, fleet/GDP ratio, and current type mix.
5. It chooses one eligible design of the most underrepresented ship type, then calls `PlayerController.BuildShipsFromDesign(...)`.
6. It repeats this only a small number of times per call: 5 attempts for surface ships, 2 attempts for submarines.

The important split:

- `GenerateRandomDesigns(...)` decides what designs are available.
- `BuildNewShips(...)` and `BuildNewSubmarines(...)` decide what to order from those available designs.
- `Ship.GenerateRandomShip(...)` and `Ship.AddRandomPartsNew(...)` decide the actual design layout, components, tonnage, armor, speed, and part placement.

The emerging strategic gap is that the inspected construction path looks mostly self-referential. It asks whether the AI can afford ships, whether it has crew and yard capacity, what its own type mix looks like, and what its personality ratios prefer. It does not appear to first ask what its current enemies or likely rivals have built.

## Evidence Sources

Code inspection used the locally decompiled vanilla sources:

- `E:\Codex\cpp2il_uad_diffable\DiffableCs\Assembly-CSharp\CampaignController.cs`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType__AiManageFleet_d__201.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType__GenerateRandomDesigns_d__202.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType___c__DisplayClass198_0.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType___c__DisplayClass198_1.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType___c__DisplayClass198_2.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType___c__DisplayClass202_0.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\PlayerController.txt`
- `E:\Codex\cpp2il_uad_diffable\DiffableCs\Assembly-CSharp\Player.cs`
- `E:\Codex\cpp2il_uad_diffable\DiffableCs\Assembly-CSharp\Ship.cs`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\Ship_NestedType__GenerateRandomShip_d__573.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\Ship_NestedType__AddRandomPartsNew_d__591.txt`
- `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\AiPersonalities.txt`

Data inspection used the local captured vanilla reference data in:

- `E:\Codex\UADRealismDIP\TweaksAndFixes\Default_Files\UAD_Files\params.csv`
- `E:\Codex\UADRealismDIP\TweaksAndFixes\Default_Files\UAD_Files\AiPersonalities.csv`
- `E:\Codex\UADRealismDIP\TweaksAndFixes\Default_Files\UAD_Files\randParts.csv`
- `E:\Codex\UADRealismDIP\TweaksAndFixes\Default_Files\UAD_Files\randPartsRefit.csv`
- `E:\Codex\UADRealismDIP\TweaksAndFixes\Default_Files\UAD_Files\shipTypes.csv`
- `E:\Codex\UADRealismDIP\TweaksAndFixes\Default_Files\UAD_Files\parts.csv`

## Main Monthly Flow

The central campaign AI fleet routine is `CampaignController.AiManageFleet(Player player, bool prewarming = false)`.

Its state-machine `MoveNext()` shows this order:

1. Skip if the player is disabled.
2. `ScrapOldAiShips(player)`
3. If not prewarming:
   - `SetAiShipRoles(player)`
   - `DeleteOldDesigns(player)`
4. Choose a design source:
   - If design mode permits generated designs, start `GenerateRandomDesigns(player, prewarming)` and yield while it runs.
   - Otherwise call `GetPredefinedDesign(player, prewarming)`.
5. After design work finishes:
   - compute `player.CashAndIncome()`,
   - call `BuildNewShips(player, tempPlayerCash)`,
   - call `BuildNewSubmarines(player, tempPlayerCash)`.

This means new construction is downstream of design maintenance. If the AI has no valid buildable design for a desired type, build selection does not itself design a ship; it only picks from what design maintenance left available.

## Campaign Design Modes

`CampaignController.DesignsUsage` is:

- `FullGenerated = 0`
- `Mixed = 1`
- `FullPredefined = 2`
- `Count = 3`

`AiManageFleet(...)` branches on this mode:

- Generated/mixed paths use `GenerateRandomDesigns(...)`.
- Full predefined paths use `GetPredefinedDesign(...)`.

`TryTakeSharedDesign(...)` and `GetSharedDesign(...)` are also gated by campaign/shared-design settings. When allowed, the generator can import a shared/predefined design instead of making a fresh random one.

## Initial Fleet Generation

New-game setup has a separate prewarm/simulation path in `CampaignController+<UpdateLoadingNewGame>d__240`.

It uses the params:

- `fleet_generation_years`
- `fleet_generation_index_chance`
- `fleet_generation_chance`
- `fleet_generation_money_min`
- `fleet_generation_money_max`

The setup loop simulates a number of earlier months/years, probabilistically calling `AiManageFleet(player, prewarming: true)`. During prewarming, the AI uses the same fleet manager but with some reporting and cleanup behavior suppressed or altered.

This matters because a campaign can start with AI designs and ships produced by the same logic, not just hardcoded starting fleet files.

## Build Gates For Surface Ships

`BuildNewShips(Player player, float tempPlayerCash)` has two broad modes.

### Existing Campaign Mode

When the player is already into the campaign, the method checks:

- `Player.Revenue()`
- `Player.GetAiPersonality().aiShipbuilding`
- `tempPlayerCash`
- `player.crewPool`
- `build_ships_min_crew_pool_amount`
- `Player.ShipTonnageUnderConstruction()`
- `Player.ShipbuildingCapacityLimit()`
- `override_shipybuilding_limit`
- `Player.StateBudget()`
- current fleet tonnage
- `ai_ship_gdp_ratio`

The money gate is effectively:

`aiShipbuilding * revenue <= tempPlayerCash`

If this fails, the method exits without attempting a build. This means the personality's `aiShipbuilding` field is not just flavor; it directly shifts how readily the AI starts new construction.

The crew gate requires enough crew pool:

`crewPool >= build_ships_min_crew_pool_amount`

The dock-work gate stops new orders if under-construction tonnage is already above:

`ShipbuildingCapacityLimit() * override_shipybuilding_limit`

The fleet/GDP gate compares state budget against current fleet tonnage times `ai_ship_gdp_ratio`. If the AI already has too much fleet tonnage for its economic scale, it stops.

### Early Fleet Generation Mode

When generating early/start fleets, the method uses:

- `Player.Income()`
- `Player.Revenue()`
- `fleet_generation_money_max`
- `fleet_generation_money_min`
- `GetBaseYearMultiplier(...)`

This gives a year-scaled money threshold. If the country's finances are below that threshold, the generated fleet pass stops.

## Surface Ship Type Selection

After gates pass, `BuildNewShips(...)` selects a ship type from globally buildable ship types.

The relevant logic is spread across compiler-generated lambdas:

- `<BuildNewShips>b__1` / `<BuildNewShips>b__3`: ship type must have an existing design for the player.
- `<BuildNewShips>b__4`: type is considered desirable if the player's current fleet mix is below the AI's target ratio for that type.
- `<BuildNewShips>b__2`: target ratio is `Player.GetShipBuildRatio(shipType.name, shipType.weightOrRatioBase)`.
- `Player.GetShipBuildRatio(...)`: gets the active `AiPersonalities` object and multiplies the base ratio by `AiPersonalities.GetBuildRatio(shipType)`.

The ratio check is the core "which class should I build?" decision.

The method compares the type's desired build ratio against how many ships of that type exist in the player's fleet. If the fleet is empty, it only needs the type to have a positive desired ratio. If the fleet is non-empty, it compares:

`desired type ratio / total desired ratio`

against:

`current count of this type / total fleet count`

If the desired share is greater than or equal to the current share, that type remains a candidate.

From the remaining candidate types, the code chooses one via `Util.Random(...)`. So the AI does not deterministically choose the largest deficit; it filters to underrepresented types and then randomly picks among them.

### Observed Vanilla Surface Ratios

Runtime diagnostics from a VP-only campaign load showed these loaded base surface `buildRatio` values:

| Type | Base Ratio | Target Count Share |
|---|---:|---:|
| `BB` | 10 | 7.4% |
| `BC` | 5 | 3.7% |
| `CA` | 23 | 17.0% |
| `CL` | 29 | 21.5% |
| `DD` | 34 | 25.2% |
| `TB` | 34 | 25.2% |

These values are count-share pressure, not tonnage-share pressure. A 2,000-ton torpedo boat and a 30,000-ton battleship each count as one ship for the ratio comparison. That helps explain why the vanilla-like campaign behavior can feel light-ship-heavy: the base ratios give `CL`/`DD`/`TB` about 72% of desired surface count share, while `BB`/`BC` together get about 11%.

This does not prove the data is wrong. It may be trying to approximate historical fleet counts. But UAD's campaign combat emphasis makes the result feel underweighted toward battle-line and core cruiser forces.

## Surface Design Selection

Once it picks a ship type, `BuildNewShips(...)` asks `<BuildNewShips>b__0(ShipType stype)` to find a design.

That selector:

1. Iterates `player.designs`.
2. Filters out non-buildable designs.
3. Filters by exact ship type.
4. Calls `PlayerController.CanBuildShipsFromDesign(...)`.
5. Keeps the buildable design with the highest `dateCreated`/year-like value.

So the AI tends to build the newest buildable design of the chosen type, not the cheapest, strongest, fastest, or best-scored design.

After selecting the design, it calls:

`PlayerController.BuildShipsFromDesign(design, amount: 1, force: false, overridePlayer: null)`

The AI build amount for surface ships is fixed at 1 per successful attempt. The outer loop allows up to 5 successful or attempted surface build passes in one `BuildNewShips(...)` call.

## Ship Value And Design Scoring

Vanilla does have ship and fleet value calculations, but they do not appear to be used by the campaign build chooser when selecting among designs of an already-chosen type.

Relevant value functions include:

- `Ship.EstimatePower(IEnumerable<Ship> ships, bool useDamageEstimation = true)`: an absolute fleet/battle power estimate. It factors the ship type's `power`, armor, cost, crew/HP-like values, speed, firepower, ammo state, and damage estimation when requested.
- `Ship.PowerProjection(bool force = false)`: a campaign strategic-presence value. It factors ship type `power_projection`, displacement, operating range, speed, firepower, and current role/status modifiers.
- `Ship.GetFirepower()`: a lower-level firepower metric used by the power estimates.
- `Player.TotalPowerProjection()`: sums ship/submarine campaign projection for a player.
- `Player.NavyPowerRating()`: uses `TotalPowerProjection()` plus a year-scaled campaign multiplier/divisor to produce the campaign UI power rating.

These are useful absolute-value signals. A 1930 battleship should usually score much higher than an 1890 battleship because its real stats are much larger: more displacement, better guns, better armor, higher cost, better speed, and so on.

The important limitation is that these functions do not appear to be a "good for this year" design score. They do not obviously normalize a ship against current-year expectations for its class, penalize old designs by age, ask whether a 1930 `BB` is competitive against other 1930 `BB`s, or compare value per cost/build time. `Player.NavyPowerRating()` has explicit year scaling, but that scaling is applied at the aggregate national-rating layer, not as a campaign construction design picker.

For AI construction, vanilla likely understands absolute power, but not era-adjusted design quality. A modded design selector would probably need separate metrics:

- `absolutePower`: raw battle/campaign usefulness from existing vanilla stats.
- `eraAdjustedPower`: how competitive this design is for its type and current campaign year.
- `costEfficiency`: useful power per dollar, ton, build month, or crew.
- `strategicFit`: whether the design answers the current pressure, such as capital duel, cruiser coverage, screens, or commerce war.

The first diagnostic step should be to log those vanilla power values for every buildable same-type candidate and compare them to the design vanilla actually picks by newest `dateCreated`.

## Post-Build Role Behavior

After `BuildShipsFromDesign(...)`, the method sets campaign-role state on the newly created ships depending on campaign state:

- If the player is not at war, newly ordered ships get a default `CurrentRole` assignment.
- If the player is at war, there is a personality-weighted random chance to change that role assignment.
- If no special branch applies, there is also a small hardcoded random chance path.

The exact `CampaignRole` value mapping should be rechecked before modifying this area. The important practical point is that vanilla does extra post-processing after ordering ships; `BuildShipsFromDesign(...)` sets the vessel to `Building`, then `BuildNewShips(...)` still assigns campaign-role intent to the ordered ship.

## Build Gates For Submarines

`BuildNewSubmarines(Player player, float tempPlayerCash)` mirrors much of the surface build logic but with important differences:

- It only loops 2 times, not 5.
- It filters `ShipType` values to names containing `ss`.
- It also reads `G.data.submarineTypes`.
- It combines surface-type ratio pressure with submarine-type ratio pressure.
- It calls `PlayerController.BuildSubmarines(type, amount: 1, owner: player)`.

The same broad budget gates are used:

- cash vs revenue/personality,
- crew pool vs `build_ships_min_crew_pool_amount`,
- fleet/GDP pressure through `ai_ship_gdp_ratio`,
- early fleet generation thresholds.

The submarine path has a simpler build validation surface:

- `PlayerController.CanBuildSubmarineForType(...)`
- `Player.IsSubmarineUnlocked(...)`
- `Player.IsSubmarineObsolete(...)`

## Design Generation Flow

`GenerateRandomDesigns(Player player, bool prewarming)` is the main design maintenance path.

Its state-machine fields show the intended loop:

- `i`
- `shipType`
- `tries`
- `ship`
- `j`

The observed flow:

1. If an existing generated ship is valid, report it as a new design when not prewarming.
2. If allowed, try to find a shared design of the desired type/year through `GetSharedDesign(...)`.
3. If a shared design exists and passes checks, use it.
4. Otherwise build a temporary constructor ship:
   - choose an eligible buildable `ShipType`,
   - find a hull with `Ship.GetHull(...)`,
   - create a `Ship`,
   - enter constructor mode.
5. Call `Ship.GenerateRandomShip(...)`.
6. If generation succeeds and the resulting ship is valid, leave it as a campaign design.
7. If generation fails, leave constructor mode and erase the temporary vessel, then retry.

The generator tries to keep design coverage by type. It groups existing player designs by `ShipType`, compares them to currently buildable types, and prefers types missing from the player's design list. If the player already has too many designs, it can prune old/obsolete ones through `DeleteOldDesigns(...)`.

## Shared And Predefined Designs

`TryTakeSharedDesign(...)` is only active when `designsUsage == FullPredefined` in the inspected path.

It chooses the relevant year:

- prewarming uses the campaign start year,
- live campaign uses the current campaign date's year.

Then it calls:

`GetSharedDesign(player, shipType, year, checkTech: true, isEarlySavedShip: false)`

`GetSharedDesign(...)` reads `G.data.sharedDesignsPerNation`, filters by the player's nation key, deserializes a stored design into a temporary `Ship`, and validates it. When `checkTech` is true, it routes through `PlayerController.CanBuildShipsFromDesign(...)`, so shared designs still have to pass tonnage, shipyard, hull unlock, obsolete, and design-validity checks.

This is a major mod implication: changing `CanBuildShipsFromDesign(...)` can affect both real build orders and shared/predefined design adoption.

### Shared-Design Store Data Model

Shared designs are serialized `Ship.Store` objects indexed in `G.GameData.sharedDesignsPerNation`, whose value type is a dictionary from nation key to a list of `(Ship.Store, string)` tuples. The string tuple member appears to be associated file/source metadata, while the `Ship.Store` is the actual design payload.

The serialized store is rich enough to understand most of the blueprint before creating a live ship:

- identity and dates: `id`, `designId`, `vesselName`, `playerName`, `dateCreated`, `dateFinished`, `dateCreatedRefit`,
- frame and hull: `shipType`, `hullName`, `tonnage`, `beam`, `draught`, hull size and hull placement fields,
- components: `components` as string key/value pairs,
- serialized tech snapshot: `techs` as `List<string>`,
- protection and ammunition: `armor`, `turretArmors`, `turretCalibers`, `AmmoTotal`, `Ammo`,
- placed parts: `parts` as `List<Part.Store>`,
- design/campaign flags: `isSharedDesign`, `isRefitSimple`, `refitDesignName`, `currentRole`, and related refit/sale fields.

`Part.Store` itself is small: `name`, `Id`, `position`, and `rotation`. The `name` is the useful key for blueprint analysis because `Ship.FromStore(...)` resolves it back to live `PartData` and `Part` objects. For a quick static answer to "what equipment is on this shared design", inspect `store.parts[*].name`. For a more reliable answer, deserialize into a temporary `Ship` and inspect `ship.parts[*].data`, because the live part data exposes tags, component type, gun/torpedo/mines/submarine-related metadata, and availability checks.

Techs have a store/live split:

- `store.techs` is a serialized string list and is useful as a broad hint.
- `ship.techsActual` is a live inherited `VesselEntity` list recomputed after `Ship.FromStore(...)` / `Ship.RecalcTechsActual(...)`.

The live `ship.techsActual` collection is the authoritative dependency list for vanilla tech matching. `CampaignController.TechMatch(...)` reads the design-side collection at offset `0x58`, which corresponds to `VesselEntity.<techsActual>k__BackingField`. This means store-only tech comparisons can be misleading: they may show broad or stale-looking entries such as `submarine_exp_4` even when the exact live dependency list would be narrower or different.

For future blueprint adaptation, this model is promising:

1. Inspect the store to identify obvious option conflicts cheaply.
2. Deserialize to a temporary `Ship` when precise part/tech data is needed.
3. Modify only live parts that correspond to narrow optional VP-rule conflicts.
4. Recalculate the live ship.
5. Re-run vanilla validation and `TechMatch(...)`.

This avoids a full redesign loop while still treating shared designs as adaptable blueprints rather than exact binary accept/reject payloads.

### Shared-Design Tech Reject Diagnostics

Recent VP diagnostics confirmed an important distinction:

- `CampaignController.TechMatch(ship, player)` returning `false` is authoritative. Vanilla really rejected that shared-design candidate on tech compatibility.
- Raw serialized `Ship.Store.techs` differences are only a fallback hint. They can include broad-looking entries such as `submarine_exp_4`, `surv_internal_9`, or `gun_sec_13`, and should not be treated as the exact missing dependency unless the live ship tech collection is readable.

The current safer log wording should keep this distinction visible:

`exactMissing=unavailable reason=shipTechsUnavailable storeMissingHint=[...]`

The next diagnostic problem is to crack the exact missing-tech read. Decompiled/diffable output points at the inherited `VesselEntity` tech collections:

- `VesselEntity.<techs>k__BackingField`, offset `0x50`,
- `VesselEntity.<techsActual>k__BackingField`, offset `0x58`,
- `VesselEntity.<techsList>k__BackingField`, offset `0x60`.

`CampaignController.TechMatch(...)` reads the design-side collection at offset `0x58`, so the exact match source is almost certainly `VesselEntity.techsActual`. The diagnostic should try direct reads of `ship.techsActual` before falling back to broad reflection. If that works, missing-tech logging should compare by `TechnologyData.Pointer` where possible, with tech name as a fallback/readability label, and should mirror vanilla:

- `tech.isEnd == true`: compare against `player.technologiesResearchedAll`,
- otherwise: compare against `player.technologiesResearchedActual`.

Target successful reject log shape:

`tech:false exactMissing=[armor_quality_5|Krupp Armor V|type=armor|component=armor_quality|end=false] shipTechs=42 playerActual=183 playerAll=208`

If exact reading still fails, keep `exactMissing=unavailable` and only show `storeMissingHint=[...]`.

Follow-up diagnostics showed that many shared-design tech rejects are not truly unknown techs. The key distinction is:

- `actual=false, all=false`: the adopting nation does not know the tech; this should remain a hard reject unless a later blueprint sanitizer can remove or downgrade the dependency.
- `actual=false, all=true`: the adopting nation knows the tech historically, but it is not in `technologiesResearchedActual`; vanilla `TechMatch(...)` still rejects it because non-end techs are compared only against `technologiesResearchedActual`.

This second case is too strict for shared-design adoption. Examples from France attempting `BB MHMW Richelieu` in 1902 include known/basic component dependencies such as `engine_boiler_1` / `fuel_coal`, `engine_special_21` / `rudder_1`, starting shell/ammunition components, and other early equipment. These had `all=true`, often `start=true` or `defaultComponent=true`, and `used=true`, but `actual=false`. That does not mean France cannot build coal boilers or basic rudders; it means the imported design's live dependency list still references older component techs that are no longer part of France's current actual tech set.

Practical fix direction: for AI shared-design matching only, treat `player.technologiesResearchedAll` as sufficient for non-end techs that are already known, while still rejecting `all=false` techs. This is narrower than full blueprint adaptation and should unblock stale/basic component dependencies without allowing genuinely unknown future equipment.

Later logs exposed a second category: unknown global unlock techs that are not directly used by the imported ship. For example, `engine_boiler_32` is `Advanced Destroyer Funnels I`, year 1902, with effect `unlockpart(earlyDDfunnels_level_2)`, no component, and diagnostics reported it as `used=false|class=global` while rejecting a German BB shared design. Its presence in a BB candidate strongly suggests `ship.techsActual` can contain broad/global unlock-state dependencies from the source design context, not only the components physically mounted on that ship. This should be treated separately from component-used techs before deciding whether `all=false` global unlocks should remain hard rejects.

Likely practical interpretation: shared-design creation may serialize or recompute all techs available to the source design year/context, not only techs needed by the mounted parts. For adoption, VP should sanitize the imported design's tech dependency list before tech matching: preserve techs that correspond to mounted components/parts or otherwise verifiable ship-specific requirements, but remove clear source-context baggage from the candidate dependency list. This should be done before the shared-design tech check and followed by normal build validation, so genuine unavailable parts still fail through `CanBuildShipsFromDesign(...)`.

Important caveat: do not remove techs just because `component=<empty>`. Some meaningful ship-design techs are not literal placed parts. Shell, AP/HE cap, propellant, explosive, ammunition, armor, fire-control, torpedo, and similar choices can be design settings or global stat unlocks rather than physical components. The first sanitizer should preserve anything mapped to `ship.components`, `ship.parts`, or `Ship.Store.components`, and should keep ambiguous combat-performance categories unless there is proof they are source-context-only. A safe first removal target is narrower: unknown, unused, empty-component `unlockpart(...)` style global unlocks that do not match any mounted part, store component, hull, gun, shell/ammo setting, torpedo setting, or other known design-setting category. Example: `engine_boiler_32` / `Advanced Destroyer Funnels I` was reported on a German BB candidate even though it unlocks destroyer funnel parts and was `used=false`; that looks like removable source-context baggage. In contrast, shell-cap and shell-option techs are often required for selected design settings and should remain rejects for now unless a future blueprint adapter deliberately downgrades them to the nearest available option.

Shared-design candidate ordering should also be VP-owned rather than inherited from file/library order. Logs from 1902 showed `+3` future candidates can be overly optimistic: a 1905 BB considered in 1902 failed on many genuinely used 1903-1905 technologies, then fell back to slow random BB generation. A better exact-copy first pass is to consider designs in a window of `currentYear - 7` through `currentYear + 3`, sorted by `YearCreated` descending. This tries the newest plausible blueprint first while giving the AI more older, buildable fallbacks before it pays the cost of random generation. The diagnostic `yearMatch` count and reject-detail order should use the same `-7/+3` window and year-descending order so logs tell the same story as selection.

### Shared Designs As Blueprints

Longer term, VP should probably treat shared designs as blueprints rather than exact copies. The exact-copy path is brittle because VP options can make otherwise good shared designs illegal: CA+ torpedoes may be disallowed, mine warfare may be disabled, submarine/ASW gear may be disabled, or a small optional component may be just beyond the adopting nation's tech.

The performant version should not run a full redesign loop. It should be a single deterministic compatibility pass:

1. Deserialize the shared design into a temporary `Ship`.
2. Run vanilla validation and tech checks.
3. If validation fails only for known optional/VP-rule conflicts, apply small blueprint sanitizers.
4. Recalculate the design.
5. Re-run vanilla validation and tech checks.
6. Accept only if the design still resembles the blueprint and passes normal build checks.

Good first sanitizer candidates:

- remove CA+ torpedoes when the VP `CA+ Torpedoes` option disallows them,
- remove mine gear when mine warfare is disabled,
- remove offensive/ASW-only submarine-warfare gear when submarine warfare is disabled,
- possibly remove or downgrade small optional systems only after exact missing-tech diagnostics prove they are safe.

The option-based sanitizer should operate on the blueprint's equipment first, not by blindly deleting techs. For CA+ torpedoes, only apply to `CA`, `BC`, and `BB`: remove mounted torpedo launcher parts and then remove torpedo launcher settings such as `torpedo_prop`, `torpedo_size`, and `ammo_torp` if no torpedo tubes remain. Do not remove `torpedo_belt` or generic torpedo-detection tower stats. For disabled mine warfare, remove mine and minesweeping parts/components using the existing `MineWarfareDetector` logic. For disabled submarine warfare, remove offensive/ASW-only equipment such as depth charges and dedicated anti-sub weapons; keep `sonar`/`hydro` because those sensors remain useful for surface-ship torpedo spotting even when submarines are disabled. Do not strip generic spotting/radio/tower stats merely because they improve submarine or torpedo detection. After equipment cleanup, recalculate or reserialize/re-import the ship so `ship.techsActual` reflects the adapted blueprint, then run the existing sanitized tech match and `CanBuildShipsFromDesign(...)`. Only techs tied to removed equipment should be pruned; combat-defining missing techs such as gun marks, armor quality, engines, rangefinders, and hull/gun layout remain hard rejects.

Bad first sanitizer candidates:

- hull substitutions,
- main-gun redesigns,
- engine/speed redesigns,
- armor scheme rewrites,
- tower/funnel swaps,
- broad component availability overrides.

Those identity-defining changes are closer to random redesign and should remain rejects until there is stronger evidence.

This should probably be a gated behavior, for example `Shared Design Handling: Exact / Blueprint Adaptation`, and it needs compact logs that explain what was changed:

`UADVP shared-design blueprint nation=Italy type=BB source=MHMW Vittorio Emanuele action=removed_ca_torpedoes,removed_mines before=techReject after=accepted power=...`

Exact missing-tech diagnostics are the prerequisite. Without them, blueprint adaptation would be guessing whether a reject came from optional equipment or from a genuinely unreachable design.

### Shared-Design Duplicate Import Finding

The May 2026 save/load repro showed that duplicate `[S]` rows are not merely a design-tab display issue and not only a post-load mutation. They are real duplicate shared blueprint rows entering the campaign design book during initial shared-design prewarm. Save/load makes the pollution persistent and obvious, but the earliest log evidence happens before the later design-tab inspection.

Fresh repro evidence from `Latest.log`:

- France imported the same `BB MHMW Provence` shared design twice during initial prewarm: first `attempt`/`success` at lines `110`/`123`, then another `attempt`/`success` at lines `149`/`160`.
- Immediately afterward, the January 1886 France snapshot reported `duplicateFingerprintGroups=1` and `duplicateFingerprintRows=2`.
- Other nations then showed full duplicate shared design books, for example February 1886 United States with `totalDesigns=8`, `counts BB=2 CA=2 CL=2 TB=2`, `duplicateFingerprintGroups=4`, and `duplicateFingerprintRows=8`.
- The `BuildShipsFromDesign(...)` sanitizer still matters, but it is not the primary explanation for this duplicate-blueprint repro. Created ships briefly inherit `isSharedDesign=true` from shared source designs, then the sanitizer clears the built ship back to `isShared=false`.
- A separate save/load hazard remains: some imported shared source designs have empty identity fields. Examples include `MHMW Kansas`, `MHMW Derbent`, and `MHMW Falcon`, where the source design logged `store.id=00000000-0000-0000-0000-000000000000` and `designId=00000000-0000-0000-0000-000000000000`. Ships built from those designs can then carry an empty `designId`, even after `isSharedDesign` is cleared.

Practical fix direction:

1. Add duplicate guarding before a shared candidate is returned/adopted into `player.designs`.
2. Prefer an ID match when the candidate/shared store has a non-empty `id`.
3. Fall back to a stable blueprint fingerprint when IDs are empty or unreliable. The fingerprint should include enough structure to distinguish real variants: normalized type, name, hull identity, mounted parts/components/weapons, tonnage band, speed band, and range band.
4. Log duplicate skips explicitly, for example `duplicate-skip nation=... type=... candidate=... existing=... reason=id|fingerprint`.
5. Normalize adopted shared-design identity before it becomes a campaign design row. If an imported blueprint has an empty `id`, assign a real campaign-local `Guid`. Keep `designId` empty for blueprint rows unless vanilla requires otherwise, but built ships should point at a non-empty source design ID whenever possible.
6. Keep shared blueprint rows marked shared for the `[S]` marker; built ships should remain non-shared after the existing build sanitizer.

This finding also tightens the future scheduled-refresh idea below: any repeated shared-only refresh must include duplicate detection from the first version, not as a later cleanup.

### Future Idea: Scheduled Shared-Only Refresh

Another possible layer, not planned for immediate implementation, is a cheap scheduled shared-design refresh pass. The motivation is that shared-design checks are much cheaper than full random generation, and vanilla can leave AI design books stale or sparse for years. In the August 1906 test, Britain reached the design/build step with no `BB` or `BC` designs, failed two shared `BC` candidates, then spent roughly a minute in random `BC` generation before creating and ordering `Incomparable`. A regular shared-only refresh might have filled some of those design-book gaps earlier without paying the full random-generation cost at the point of construction.

Possible cadence:

- January / April / July / October: check `BB` and `BC`,
- February / May / August / November: check `CA` and `CL`,
- March / June / September / December: check `DD` and `TB`.

This pass should run only after the same cheap capacity preflight used for expensive design generation: cash, crew, queue, vanilla fleet/GDP cap, type unlock, and basic shipyard fit. If a nation cannot currently place orders, it should not maintain the design book that month. The pass should also be shared-only: if no shared candidate works, log the reject reason and stop. It must not fall through to `Ship.GenerateRandomShip(...)`, because avoiding that expensive path is the point.

Duplicate control is the main design risk. After sanitizing, downgrading, and recalculating a shared blueprint, VP should derive a stable "blueprint fingerprint" before committing it to the design book. The fingerprint could include the normalized type, hull, main battery layout, torpedo launcher presence, armor scheme or armor-quality tier, major components, tonnage band, speed band, and range band. If an existing design has the same fingerprint, skip import. If the blueprint is meaningfully improved because the nation's tech has changed, import it as a new version using a clear generated name such as `MHMW Defence (1907)` or `MHMW Defence Mk II`, rather than creating exact duplicate class names.

The refresh should probably import only when it improves the design book:

- no buildable design exists for that type,
- the newest buildable design is stale by a configurable age threshold,
- the shared candidate has a meaningful adjusted-power improvement,
- or the current buildable design is below an active arms-race threshold.

This idea is attractive because it keeps the AI more current while preserving performance discipline: use cheap shared blueprints opportunistically, avoid random generation, and avoid growing the design book with near-duplicates.

## Random Ship Layout Generation

The actual design layout work is under `Ship.GenerateRandomShip(...)`.

The public signature shows the controls it accepts:

`GenerateRandomShip(onDone, needWait, error, adjustTonnage, adjustBeam, adjustDraught, adjustDiameter, adjustLength, customTonnageRatio, customSpeed, customRange, customSurv, customArmor, limitCaliber, limitArmor, limitSpeed, fromUi, isSimpleRefit, checkMainGunsCount, useSmallAmountTries, info)`

For the AI campaign path inspected in `GenerateRandomDesigns(...)`, most of those are passed as default/true values, with `info` supplied through a `StringBuilder`.

Inside the state machine, important calls include:

- `GameManager.StartAutodesign(...)`
- `Ship.IsValidWeightOffset(...)`
- `Ship.IsValidCostReqParts(...)`
- `Ship.IsValidCostWeightBarbette(...)`
- `Ship.RemoveAllParts(...)`
- `Ship.AddRandomPartsNew(...)`
- `GameManager.EndAutodesign(...)`

The generator retries with different tonnage/beam/draught/armor/speed/component choices until validation passes or the try budget is exhausted. Failure logging includes:

`failed to generate random ship of type '{0}', reason '{1}', hull '{2}', reqs '{3}', parts '{4}', try {5}`

The layout itself is mostly data-driven by `randParts.csv` and `randPartsRefit.csv`.

## Random Parts Data

`Ship.AddRandomPartsNew(...)` uses:

- `GameData.randParts`
- `GameData.randPartsRefit`
- `Ship.GetParts(RandPart randPart, limitCaliber)`
- mount checks,
- part availability checks,
- placement checks such as `Part.CanPlaceSoft(...)` and `Part.CanPlaceSoftLight(...)`.

The state-machine fields show the core placement bookkeeping:

- `partDataForGroup`
- `mainTowerPlaced`
- `secTowerPlaced`
- `secTowerNeedForShip`
- `funnelsInstalled`
- `maxFunnels`
- `desiredAmount`
- `chooseFromParts`
- `firstPairCreated`
- `offsetX`
- `offsetZ`

The `randParts.csv` columns define most of the design recipe:

- ship types,
- chance,
- min/max count,
- part type,
- paired placement,
- grouping rules,
- required effect,
- center/side placement,
- Z range,
- conditions such as `tag(...)`, `!tag(...)`, `zero(...)`, `mount(...)`, and boolean `and/or`.

This is where national flavor by layout/hull family most naturally enters. The monthly build chooser only says "I need a CL" or "I need a BB"; `randParts` and part/hull availability determine what that CL or BB physically becomes.

## AI Personalities

`AiPersonalities.csv` controls several build-related values:

- `aiShipbuilding`
- `aiRefit`
- `aiShipyardMod`
- `aiTechMod`
- `aiParams`

The `aiParams` string can include tokens such as:

- `buildRatio(bb;2.45)`
- `buildRatio(cl;2.6)`
- `buildRatio(ss;0.85)`
- `TechMod(gun_main;2)`

`AiPersonalities.GetBuildRatio(shipType)` returns 1 by default and only changes if the active personality has a matching build-ratio entry. `Player.GetShipBuildRatio(...)` multiplies that personality ratio by the base ship-type ratio.

Therefore, personality influences:

- when the AI is willing to spend on new ships, through `aiShipbuilding`,
- what type mix it drifts toward, through `buildRatio(...)`,
- what technology areas it prioritizes, through `TechMod(...)`.

It does not appear to directly score individual designs in `BuildNewShips(...)`.

## PlayerController Build Validation

`PlayerController.CanBuildShipsFromDesign(...)` is a shared choke point.

It checks:

- design has an owner/player,
- tonnage is allowed by tech: `Player.IsTonnageAllowedByTech(...)`,
- design weight fits `Player.MaxShipyard(...)`,
- hull is unlocked: `Player.IsHullUnlocked(...)`,
- obsolete hull behavior: `Player.IsHullObsolete(...)`,
- funds in some custom battle/shared-design paths,
- `Ship.IsValid(...)`,
- custom-battle ship type restrictions when relevant.

It returns reason strings including:

- `tonnage`
- `shipyard`
- `unlock`
- `obsolete`
- `funds`
- `valid`
- `shiptype`

Because both build selection and shared-design import use this method, broad changes here can have a wide blast radius.

`PlayerController.BuildShipsFromDesign(...)` then:

1. Rechecks `CanBuildShipsFromDesign(...)` unless forced.
2. Clones the design via `CloneShipRaw(...)`.
3. Sets the new ship status to `Building`.
4. Deducts crew from `player.crewPool`.
5. Randomizes/sets crew training values.
6. Recalculates weight/cost.
7. Adds the new ship to the returned list.

## What Does Not Seem To Happen

Based on this pass:

- The monthly build chooser does not appear to rank designs by combat score.
- It does not appear to compare armor, firepower, speed, range, or cost among multiple designs of the same type.
- It does not appear to use `Ship.EstimatePower(...)`, `Ship.PowerProjection(...)`, `Ship.GetFirepower()`, or cost efficiency when choosing which same-type design to build.
- It does not appear to score designs relative to their era or against same-year expected standards.
- It does not appear to compute a grand strategic target fleet from `NeededTonnage(...)` before ordering ships.
- It does not appear to compare the AI player's fleet against current war opponents before choosing ship types.
- It does not appear to compare the AI player's fleet against likely peacetime rivals before choosing ship types.
- It does not appear to ask whether the AI is behind in capital ships, cruiser strength, screens, or total battle-line tonnage relative to rival navies.
- It does not appear to build multiple surface ships of the same design in one call; each successful surface attempt orders one ship.
- It does not create a missing design inside `BuildNewShips(...)`; missing designs have to be addressed earlier by design maintenance.

## Relation To Needed Tonnage

`CampaignController.NeededTonnage(...)` is still important for AI behavior, but it appears tied to area/task-force pressure rather than direct ship construction.

The inspected call site sums area deficits by comparing:

- `AreaCurrentTonnage(area, player)`
- `NeededTonnage(area, player, forShips)`

That result feeds later campaign movement/task-force logic. It is not called inside `BuildNewShips(...)` or `GenerateRandomDesigns(...)` in the inspected path.

So if the future idea is "AI should build more ships because it has too little coverage in an area," vanilla does not seem to wire that directly into ship orders. A mod would need to bridge strategic area deficits into either build-ratio pressure or build gating.

## Opposition Awareness Gap

Historically, major naval construction was relational. Navies did not build only toward an internal preferred ratio; they watched active enemies, likely rivals, alliance systems, regional theaters, and visible capital-ship programs. A country behind its most important rival in battle-line strength should feel pressure to respond, especially in `BB`, `BC`, and `CA` construction.

The inspected vanilla build path does not appear to have that layer. The likely current model is closer to:

1. Can this AI afford to build?
2. Does it have crew and yard capacity?
3. Is its own fleet under its economy-scaled limit?
4. Which own ship-type count share is below the personality/base target share?
5. Pick a random eligible underrepresented type.
6. Build the newest valid design of that type.

The missing model is:

1. Who should this AI be building against?
2. Is it strategically behind those opponents in capital ships, cruisers, screens, submarines, or total tonnage?
3. Which deficit matters most right now?
4. Can the AI afford an appropriate response, or should it choose a cheaper substitute?
5. How should that external pressure override or bend personality ratios?

## Threat-Aware Build Pressure Idea

A future VP behavior change could add a naval threat assessment layer above the existing type-ratio filter. The goal would not be to replace economy, crew, shipyard, design availability, or personality logic. Those still matter. The goal is to stop the AI from making construction decisions in isolation when rival fleet data should clearly redirect it.

### Opponent Selection

At war:

- Active war opponents should receive the highest weight.
- Multiple war opponents can be combined into a weighted rival fleet, but avoid letting a tiny secondary opponent dominate the assessment.
- Theater overlap should matter if the data is accessible: a hostile navy with ships in the same sea areas should count more than a distant opponent with little operational contact.

At peace:

- Likely rivals should be selected from low relations, recent wars, high tension, shared sea regions, competing great-power status, and naval/economic rank.
- Existing diplomacy and relation data should be used rather than inventing a separate rival list.
- Minor or irrelevant countries should be ignored unless they are geographically close, directly hostile, or unusually strong in the same theater.

### Comparison Metrics

The comparison should prefer strength groups and tonnage over raw ship count:

- Capital force: `BB` + `BC`, with `CA` either separate or as a partial capital substitute for poorer navies.
- Cruiser force: `CA` + `CL`, useful for trade protection, colonial coverage, and medium-power competition.
- Screens: `CL` + `DD` + `TB`, compared both as absolute screen strength and relative to capital ships.
- Submarines: `SS`, probably separate from surface-fleet pressure.
- Total naval tonnage and under-construction tonnage by type.

Ships under construction should count at partial weight so the AI can react to visible programs without double-counting future strength as already present. Obsolete, damaged, or low-readiness ships could eventually be discounted, but that should be a later refinement after basic diagnostics prove useful.

### Pressure Outputs

The assessment should produce simple pressure values that can be logged and later fed into build scoring:

- `capitalPressure`: high when rivals have materially stronger `BB`/`BC` battle-line strength.
- `heavyCruiserPressure`: high when the AI cannot afford or build enough capital ships but needs core surface strength.
- `screenPressure`: high when the AI has too few screens for its own capital fleet or compared with rivals.
- `subPressure`: high when submarine warfare or commerce pressure should be emphasized.
- `rebuildPressure`: high when the AI fleet has collapsed below a viable national baseline.

The first useful version can be coarse. For example, a country with 1 `BB` facing a weighted rival pool of 8 `BB`/`BC` equivalents should generate strong capital pressure even if its own personality currently likes light ships.

### Blending With Vanilla Logic

The least disruptive behavioral target is the ship-type decision inside `BuildNewShips(...)`, after vanilla gates have already passed and before a final type is selected.

Possible blending rules:

- Keep vanilla `buildRatio(...)` and base ship-type ratios as personality flavor.
- Add threat pressure as a multiplier or additive score per ship type.
- Let severe strategic deficits override personality, especially for `BB`/`BC`/`CA`.
- Do not let light-ship preference satisfy a capital-force deficit.
- Use affordability fallbacks: if the AI needs `BB`/`BC` but cannot build or afford them, pressure can flow to `CA`; if it cannot sustain `CA`, it can flow to `CL` rather than endless `TB`/`DD` spam.
- Keep design validation unchanged initially. If the AI has no valid design for the strategically preferred type, diagnostics should show that as a design-generation or design-retention problem rather than silently picking an unrelated type.

This should be treated as a behavior plan only after diagnostics. The first step is to log the gap, not to change shipbuilding.

## Near-Term Configurable Ratio Profiles

A pragmatic first VP step is to make the surface build-ratio baseline configurable, while keeping the rest of vanilla AI construction intact.

This is intentionally simpler than the threat-aware plan above. It would not yet know about era, war opponents, likely rivals, or theater needs. It would only change the base desired type mix that vanilla already uses.

Proposed campaign option:

- `AI Fleet Mix`
- `Vanilla`: keep the loaded game-data ratios.
- `Balanced`: set `BB`, `BC`, `CA`, `CL`, `DD`, and `TB` to equal weight.
- `Heavy`: favor battle-line and core cruiser construction.

Proposed profile values:

| Mode | `BB` | `BC` | `CA` | `CL` | `DD` | `TB` | Intent |
|---|---:|---:|---:|---:|---:|---:|---|
| `Vanilla` | loaded original | loaded original | loaded original | loaded original | loaded original | loaded original | Preserve base-game behavior. |
| `Balanced` | 10 | 10 | 10 | 10 | 10 | 10 | Equal count-share pressure across all surface types. |
| `Heavy` | 30 | 15 | 30 | 30 | 10 | 10 | Push AI toward stronger `BB`/`BC`/`CA`/`CL` fleets and away from `DD`/`TB` overproduction. |

`Heavy` should be the default if the goal is to immediately address campaigns where AI navies overindex on light ships and lack credible core fleets.

This profile should initially affect only surface ships. Submarines use a separate path and should stay vanilla until there is a specific submarine balance goal.

Implementation shape:

- Cache original loaded `ShipType.buildRatio` values before changing them.
- Apply the selected profile to `BB`, `BC`, `CA`, `CL`, `DD`, and `TB` once game data exists.
- Reapply safely on campaign load/start and when the option changes.
- Restore cached originals for `Vanilla` instead of hardcoding the currently observed vanilla values.
- Log the active profile and resulting ratios in diagnostics so campaign logs can explain what the AI was optimizing toward.

This is a good first experiment because it works with vanilla's existing ratio machinery. If it produces better campaign fleets, the later smarter system can treat these profiles as fallback baselines.

### Why This Is Not The Final AI Model

Static ratios cannot answer several important historical and gameplay questions:

- Era: a 1900 navy should not necessarily use the same `BB`/`BC`/`CA`/`CL` pressure as a 1935 navy.
- Technology: battlecruiser pressure should depend on whether battlecruisers are unlocked and practical.
- Economy: poorer powers may need `CA`/`CL` fallback pressure when they cannot sustain modern `BB` construction.
- War state: a country fighting a major battle fleet should respond differently from a country fighting a commerce-raiding or regional opponent.
- Arms race: if a rival is laying down many capital ships, the AI should be able to detect that and respond before the gap becomes permanent.
- Screens: heavier capital construction should eventually imply enough screen pressure to escort and protect the battle fleet, not just fewer light ships forever.

So the profile option should be considered a controlled baseline experiment. The smarter follow-up would be an era-aware and opponent-aware pressure layer that bends these ratios dynamically.

## Candidate Mod Hook Points

### Low Risk

- Adjust `AiPersonalities.csv` `buildRatio(...)` and `aiShipbuilding`.
- Add a configurable VP profile that changes loaded surface `ShipType.buildRatio` values, with `Vanilla` restoring cached originals.
- Adjust `params.csv` thresholds:
  - `ai_ship_gdp_ratio`
  - `build_ships_min_crew_pool_amount`
  - `override_shipybuilding_limit`
  - `fleet_generation_money_min`
  - `fleet_generation_money_max`
- Add reporting/logging around `BuildNewShips(...)`, `GenerateRandomDesigns(...)`, and `CanBuildShipsFromDesign(...)` before changing behavior.

These change broad tendencies without touching random design layout internals.

### Medium Risk

- Patch the type-candidate ratio filter in `BuildNewShips(...)`.
- Patch design selection so the AI chooses among same-type designs by a custom score instead of newest date.
- Add era-adjusted design scoring that blends vanilla absolute power, current-year expectations, cost/build-time efficiency, and strategic need.
- Patch `DeleteOldDesigns(...)` retention so useful older designs remain available longer.
- Patch `GenerateRandomDesigns(...)` to force design coverage for missing ship types.
- Add a threat-aware type-pressure layer that adjusts `BB`/`BC`/`CA`/`CL`/`DD`/`TB` candidacy based on current war opponents or likely peacetime rivals.
- Add a gated shared-design blueprint adaptation pass that removes narrowly optional equipment blocked by VP options, then revalidates through vanilla checks.

These are behaviorally direct, but they risk making AI queues weird if validation or design retention gets out of sync.

### High Risk

- Patch `Ship.GenerateRandomShip(...)`.
- Patch `Ship.AddRandomPartsNew(...)`.
- Patch `Ship.GetParts(...)` or rand-part placement eligibility.
- Patch `PlayerController.CanBuildShipsFromDesign(...)` globally.

These can affect AI, player designer behavior, shared designs, custom battle, refits, and validation. VP already has prior evidence that bad design validation can cause later campaign failures, so any changes here need tight player/AI/context gates and log-backed verification.

## Research Questions For Follow-Up

1. What exact `CampaignRole` values are assigned after AI build orders, and do those roles affect later task-force allocation or readiness?
2. Does `DeleteOldDesigns(...)` remove designs that are still strategically useful, especially for lower-tech or cash-strapped AI nations?
3. How often does `GenerateRandomDesigns(...)` fail by nation/type/year in a real campaign log?
4. Do AI nations stall because build gates fail, because no buildable design exists, or because their type ratios consider them satisfied?
5. Could strategic area deficits from `NeededTonnage(...)` safely feed into ship-type build pressure?
6. Are shared/predefined designs frequently rejected by `CanBuildShipsFromDesign(...)`, and if so, which reason dominates?
7. Does the "newest design wins" rule cause AI to build expensive or invalidly specialized ships over more practical older designs?
8. Which opponents should count as an AI nation's likely peacetime rivals, and which vanilla relation/tension/theater fields are safest to use?
9. During wars, does the AI's selected build type correlate at all with enemy fleet composition, or is it fully explained by own-fleet ratios and design availability?
10. How often does a country with a clear capital-ship deficit still select `CL`, `DD`, or `TB` because those types remain under personality/count-share targets?
11. Does counting ships by tonnage instead of raw count change the diagnosis of "underrepresented" ship types for AI nations that overbuild light ships?
12. Should build-ratio profiles vary by era, and if so what year/tech thresholds should shift pressure between `CA`, `BC`, and `BB`?
13. Can an arms-race signal safely include ships under construction, so AI nations respond to rival capital programs before those ships enter service?
14. How well do `Ship.EstimatePower(...)` and `Ship.PowerProjection(...)` correlate with actually good campaign designs by type/year?
15. What should an era-adjusted design score use as its baseline: same-type peer designs, current tech limits, campaign year, tonnage bands, or opponent fleet capabilities?
16. Does the newest-design rule cause the AI to build a lower-power or worse-value design when an older same-type design is stronger, cheaper, or better suited to current strategic pressure?
17. Can shared-design tech rejects log exact missing `VesselEntity.techsActual` entries instead of falling back to serialized `Ship.Store.techs` hints?
18. Which exact missing-tech categories are safe for blueprint adaptation, and which should remain hard rejects because they define the design's identity?

## Suggested First Instrumentation

Before changing AI construction behavior, add a diagnostic-only pass that logs one compact line when `BuildNewShips(...)` exits early or builds:

- player nation,
- turn/year,
- cash/income/revenue,
- `aiShipbuilding`,
- crew pool,
- fleet tonnage,
- under-construction tonnage,
- candidate ship types,
- selected ship type,
- selected design,
- `CanBuildShipsFromDesign(...)` failure reason if no design is buildable.

For design generation, log:

- desired ship type,
- whether shared/predefined was tried,
- whether random generation was used,
- hull chosen,
- retry count,
- failure reason from `Ship.GenerateRandomShip(...)`.

For same-type design selection, log every buildable candidate of the chosen type:

- design name,
- date/year created,
- tonnage,
- cost,
- build time if available,
- `Ship.GetFirepower()`,
- `Ship.PowerProjection(...)`,
- `Ship.EstimatePower(...)` for that single-design list,
- whether it is the newest design,
- whether vanilla selected it.

This would let us separate "AI cannot afford ships", "AI does not want that type", "AI cannot generate a valid design", and "AI has designs but build validation rejects them."

### Candidate Gate No-Order Diagnostic

Recent campaign logs exposed one remaining blind spot: an AI nation can pass the broad build gates, have one or more candidate ship types, and still exit `BuildNewShips(...)` without placing an order. For example, China in November 1903 had cash, crew, queue, and fleet-GDP gates passing, with `CA`, `CL`, and `TB` candidates, but no order. In the next turn, under similar broad conditions, it did order `CL` and `TB`.

The existing `UADVP ai-build ... none` line identifies this as `likelyGate=candidate`, but it does not explain what happened inside vanilla's build loop after candidate eligibility was established. The next diagnostic layer should instrument the inner build attempts:

- log when each surface build pass starts and ends,
- log the type returned by the weighted picker/vanilla predicate for that pass,
- log whether `BuildShipsFromDesign(...)` was called,
- log whether it returned/created a new ship,
- log if the pass was skipped because the selected type became share-satisfied after an earlier pass,
- log if the pass was skipped because selected type/design validation changed after the prefix diagnostics,
- log remaining cash, temp cash, crew, under-construction tonnage, and queue ratio after each successful order,
- log an explicit `no-order-reason` summary when broad gates passed and candidates existed but no ships were added.

The important distinction is between "AI chose not to attempt construction", "AI attempted but `BuildShipsFromDesign(...)` silently failed", and "our diagnostic/picker view diverged from vanilla's actual inner-loop state." This should remain diagnostic-only.

### Design Power Snapshot Diagnostic

A useful near-term diagnostic is to log each AI nation's current design book immediately after design maintenance and before construction orders. The safest hook is probably the existing `BuildNewShips(...)` diagnostic prefix: in vanilla `AiManageFleet(...)`, that point runs after `GenerateRandomDesigns(...)` or `GetPredefinedDesign(...)` has finished, so it acts like an "end of design turn" snapshot without directly patching the coroutine.

For each AI nation, log a compact header:

- nation,
- turn/year,
- campaign phase,
- AI personality,
- active `AI Fleet Mix`,
- total design count,
- design count by surface type.

Then log one compact line per current design:

- type,
- design name,
- created year/date,
- refit year/date if relevant,
- tonnage,
- cost,
- `BuildingCostPerMonth()` if stable for designs,
- `Ship.GetFirepower()`,
- `Ship.PowerProjection(force: true)`,
- `Ship.EstimatePower(single-design-list, useDamageEstimation: false)`,
- `CanBuildShipsFromDesign(...)` result and reason.

Use `useDamageEstimation: false` for initial diagnostics, because these are designs rather than active battle ships and damage state should not matter. If `EstimatePower(...)` returns zero or throws for design objects, the log should still record the failure and keep the rest of the design row.

This snapshot should be gated or easy to remove because it can be very verbose during campaign generation. The first version can log once per AI player per campaign turn and include only `BB`, `BC`, `CA`, `CL`, `DD`, and `TB` designs.

### Opposition Snapshot Extension

The next diagnostic iteration should add one compact opposition snapshot per traced AI build decision. It should not change behavior yet.

Log:

- selected comparison opponents and weights,
- whether each opponent was selected because of war, low relations, recent conflict, shared theater, naval rank, or another reason,
- own fleet mix by count, tonnage, and under-construction tonnage,
- weighted rival fleet mix by count, tonnage, and under-construction tonnage,
- own-vs-rival capital force: `BB`, `BC`, and possibly `CA`,
- own-vs-rival cruiser force: `CA` and `CL`,
- own-vs-rival screens: `CL`, `DD`, and `TB`,
- resulting diagnostic-only pressure values such as `capitalPressure`, `screenPressure`, and `rebuildPressure`,
- vanilla selected type and selected design, when an order happens,
- the hypothetical threat-preferred type, without using it to change the order.

Example target log shape:

```text
UADVP ai-build threat nation=Germany turn=Jan 1912 rivals=Britain:war:1.00,France:hostile:0.60 ownCore=BB:5/145000t,BC:0/0t,CA:16/128000t rivalCore=BB:9.4/310000t,BC:2.1/76000t,CA:11.7/102000t pressure=capital:high,screen:ok,rebuild:low vanillaType=CL threatType=BB
```

This log would make the core question visible: did vanilla pick a light ship because the AI is truly strategically safe, because it lacks a buildable capital design, because capital ships are blocked by budget/yard limits, or because the build chooser never considered the rival fleet at all?

## Current VP Findings From 1895 Campaign Runs

The latest VP instrumentation confirms several practical lessons for future AI design/build work.

### Design Generation Gates

Vanilla design maintenance is much more conservative than the campaign pace needs. A nation may have all buildable surface types represented and still wait for the replacement/staleness path. The observed vanilla replacement path is constrained by:

- a design-count gate around the old `<= 6`/`7` design-book behavior,
- a freshness/staleness check that is too slow for VP's faster design expectations,
- cadence/random rolls that can skip even when the design book is missing important buildable types,
- one-design-per-pass behavior, so recovering missing `BB`, `CA`, `CL`, and `TB` coverage can take several successful cadence passes.

The VP replacement-count bypass works for stale represented types in logs, but it does not help when a nation has fallen into missing-design recovery. Italy demonstrated this clearly: it lost most design-book entries, skipped multiple turns by cadence while missing `CA` and `TB`, then eventually imported only one shared `CA` design. The next useful diagnostic/fix is to separate "stale replacement" from "missing buildable type recovery." Missing buildable surface types should probably bypass cadence and be tried in priority order, likely `BB -> BC -> CA -> CL -> DD -> TB`, while stale replacement can remain throttled.

### Design Book Pruning And Retention

Campaign logs show active fleets can still contain ships using old designs that are no longer present in `player.designs`. This matters because buildability and design generation reason from the design book, not merely from active ships. Italy's active fleet still contained old `BB`, `CA`, `CL`, and `TB` designs, but the design snapshot eventually showed only `CA` and `CL` in the book. Future retention work should distinguish:

- old designs still useful for building,
- old designs only referenced by active ships,
- shared-design imported blueprints,
- random-generated replacements,
- refit designs that should not become normal build templates unless intentionally allowed.

### Build Decisions Versus Design Availability

The build-side diagnostics now separate several common causes that look similar in the UI:

- no buildable design for the desired type,
- type ratio already satisfied by count share,
- cash, crew, queue, or fleet-GDP gates blocking construction,
- candidate types existing but vanilla not entering the actual build call,
- a valid design existing but not selected because another type won the build picker.

Italy in November 1895 had a valid new shared `CA:MHMW Vittoria` at about 89% of benchmark, but construction was still blocked by both cash and crew. That was not a shared-design problem. Japan, by contrast, had buildable random `BB`/`CA` designs and repeatedly built from them because the designs remained valid and the weighted picker's growth fallback selected a capital ship after ordinary candidates were share-satisfied.

### Shared Designs As Blueprints

Exact-copy shared designs are too brittle. The data model exposes a design's stored part/component state and serialized tech lists, but `Ship.Store.techs` can be broader than the actual materialized design requirements. `VesselEntity.techsActual` is the more useful path when available. Diagnostics should keep distinguishing exact missing techs from broad serialized-store hints.

The long-term direction is to treat shared designs as blueprints:

- try newest acceptable candidates first, within a year window such as current year +3 / -7,
- sanitize equipment disabled by VP options before validation,
- downgrade safe optional components before rejecting,
- then run vanilla build validation.

Safe downgrade/adaptation categories identified so far:

- armor quality one step down,
- torpedo size one step down,
- radio to previous or none,
- rangefinder to previous or none,
- auxiliary engine / steering gear / drive shaft to previous or none,
- torpedo belt/protection to previous or none,
- minor range/speed tonnage rescue after downgrades.

Categories to avoid or treat as hard rejects for now:

- hull unlocks,
- main/secondary gun techs and mounts,
- gun mechanics where the exact effect is unclear,
- broad hull-series globals unless proven unused,
- sonar/hydrophone, because those remain useful for torpedo spotting and should not be removed just because submarine warfare is disabled.

### VP Option Sanitizers

Shared-design option sanitizers are working in the observed logs: Japanese shared `BB` and `CA` candidates had CA+ torpedo launchers removed before validation. Those candidates still failed for tech, unlock, duplicate, or invalid-design reasons, so vanilla fell back to random generation.

The CA+ Torpedoes option currently has one important gap: random-generated AI `BB`/`BC`/`CA` designs can still include torpedo launchers. Japan demonstrated this:

- `BB:Yamashio` was `source=random` and later logged `torpTubes=5`.
- `CA:Nokogiri` was `source=random` and later logged `torpTubes=4`.

This is not a shared-design sanitizer bug. `DesignTorpedoRestrictionPatch` only hides torpedo parts from the human designer, and the shared sanitizer only runs on shared imports. The likely fix is post-generation sanitation: let vanilla random generation complete, then strip torpedo launcher parts and torpedo support components from successful AI random `BB`/`BC`/`CA` designs, recalculate, validate, and log the result. Avoid pre-blocking torpedo parts inside `Ship.GenerateRandomShip(...)`, because that generation path is a fragile Il2Cpp boundary and some early hulls may rely on placeholder parts during construction.

### Power And Competitiveness

`Ship.EstimatePower(...)` plus VP's effective-power adjustment is good enough for diagnostics and coarse gates, but it should remain advisory until more sanity checks are done. The current logs show reasonable broad ordering, but power values can be skewed by random-generated torpedo-heavy major ships, and the option sanitizer gap above can inflate `BB`/`CA` torpedo threat until fixed.

Arms-race competitiveness should stay configurable and probably default below the early experimental 75% threshold. The 60% range seemed more practical in campaign observation, while disabled remains useful during design-generation debugging.

### Current Open Fix Candidates

Near-term, evidence-backed work items:

1. Add a post-generation AI random-design sanitizer for CA+ torpedoes on `BB`/`BC`/`CA`.
2. Add missing-design recovery that bypasses cadence when a buildable surface type has no current design.
3. Keep stale replacement throttled, but retain the replacement-count bypass for stale represented types.
4. Keep shared-design blueprint adaptation focused on safe optional downgrades and option-driven equipment removal.
5. Continue logging `source=random` versus `source=shared`, design power, buildability, and explicit build gates so later runs can distinguish design problems from economy/crew problems.
