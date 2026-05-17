# Problematic AI Design Generation

This note captures the slow design-generation evidence from the latest observed slow next-turn pass.

Source log:

- `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log`
- Slow turn summary: `[NextTurn]: Date: 01/01/1895`
- Timing bucket: `ManageFleet: 49938ms` out of `Total: 51242ms`
- The AI-build diagnostics around that turn report `turn=February 1895`; the vanilla next-turn timing block prints the prior date label.

## Problematic Entries

| Turn | Inferred country | Class | Hull tried | In-game hull name | Hull context | Evidence | Timing signal |
| --- | --- | --- | --- | --- | --- | --- | --- |
| February 1895 | German Empire | BB / battleship | `b2_friedrich` | Battleship III | German `B_Friedrich` hull, model `friedrich_hull_a`, tonnage 12,000-17,000t, speed 18.3 kn, tech unlock year 1895 | Failed random BB generation for `weight` on `try '2'`, `current_weight 15015.87`; then `failed to generate ship of type 'bb', hull b2_friedrich in 4 tries`. | France AI-build finished at `21:19:21.353`; Germany AI-build started at `21:19:46.091`. The visible failed-hull window ran from `21:19:33.585` to `21:19:45.257`, about 11.7s, inside a 24.8s country gap. |
| February 1895 | Empire of Japan | CL / light cruiser | `cl_1_3mast_armored` | Belted 3-Mast Cruiser | Japan/Greece/Portugal `CL_Chiyoda` hull, model `emden_hull_var`, tonnage 2,750-5,500t, speed 19 kn, unlocked by early hull tech and obsolete around 1895 cruiser tech | Failed random CL generation for `parts`: `gun_4_x1: part, gun_4_x1: part`, `try 1`. | Austria-Hungary AI-build started at `21:19:46.189`; Japan AI-build started at `21:19:57.307`. The failed CL line is at `21:19:52.359`, inside an 11.1s country gap. |

## Country Inference

- `b2_friedrich` is Germany-only in the live `parts.csv`, and the failed BB generation appears immediately before the German Empire AI-build marker.
- `cl_1_3mast_armored` supports Japan/Greece/Portugal in the live `parts.csv`; only Japan is present as a campaign major in this trace, and the failed CL generation appears immediately before the Empire of Japan AI-build marker.

## Nearby Designs Not Proven Slow

These designs were newly created or present in the same turn's AI-build snapshots, but the log does not prove that they individually caused the long wall time:

- British Empire: `CL Retribution`, created February 1895.
- France: `CA Tonnant`, created February 1895.
- Empire of Japan: `CL Tenryu`, created February 1895. This is near the `cl_1_3mast_armored` failure, but the diagnostic does not print the final hull on the generated design.
- Chinese Empire: `TB Y-3`, created February 1895.

## Suggested Follow-Up

- Add a temporary stopwatch around AI `GenerateRandomDesigns` with country, requested class, selected hull id, display name, attempt count, and success/failure result.
- If the German case repeats, inspect why `b2_friedrich` can select a weight envelope that repeatedly lands overweight around 15,016t.
- If the Japan case repeats, inspect `cl_1_3mast_armored` gun slot requirements against the 4-inch single gun candidate (`gun_4_x1`) and any stale/obsolete part availability at the 1895 boundary.
