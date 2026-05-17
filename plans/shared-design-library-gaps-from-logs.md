# Shared Design Library Gaps From Logs

This note lists AI-created ship designs seen in the recent MelonLoader logs. The goal is to identify missing shared-design-library coverage so the campaign can reuse predefined designs instead of spending turn time on random generation.

## Method

- Primary source: `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log`.
- Supplemental source: the newest archived logs under `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Logs`.
- Heuristic for successful manual generation: `UADVP ai-build ... design-power ... created=<same month/year as turn>`.
- Existing shared-library style designs in this run usually appear as `MHMW ...` with older creation dates such as `created=January 1890`.
- Caveat: current diagnostics do not print the final hull ID for successful generated designs. Failed generation attempts do print the hull ID, so those are listed with exact hull display names.

## Current Latest.log Successful Manual Designs

These are the current-run successful AI-created designs in `Latest.log` where the design creation date matches the AI-build turn.

| Turn | Country | Class | Generated design | Tons | Monthly build cost | Power projection | Library gap to consider |
| --- | --- | --- | --- | ---: | ---: | ---: | --- |
| June 1894 | Austro-Hungarian Empire | BB | Ernst | 10068.5 | 2557270 | 2107.8 | Add an Austria-Hungary 1894 BB/pre-dreadnought-era template. |
| December 1894 | Austro-Hungarian Empire | TB | Alk | 398.3 | 202109.8 | 187 | Add an Austria-Hungary 1894 TB template. |
| November 1894 | British Empire | BB | Royal Oak | 15460.6 | 3369984 | 3844 | Add a Britain 1894 BB template around 15.5k tons. |
| February 1895 | British Empire | CL | Retribution | 2764.4 | 555201.9 | 721 | Add a Britain 1895 CL template around 2.8k tons. |
| February 1894 | France | CL | Jean Bart | 3935.2 | 777064.8 | 439.5 | Add a France 1894 CL template around 3.9k tons. |
| February 1895 | France | CA | Tonnant | 6061.6 | 1549906 | 1894.8 | Add a France 1895 CA template around 6.1k tons. |
| February 1895 | Empire of Japan | CL | Tenryu | 3054.8 | 706003.6 | 693.3 | Add a Japan 1895 CL template around 3.1k tons. |
| February 1895 | Empire of Japan | BB | Yamashio | 11406.8 | 2855638 | 3267.7 | Shared BB candidates were sanitized for CA+ torpedo restrictions but rejected, then vanilla random generated this BB with torpedo launchers. Add/repair Japan 1895 BB shared coverage or sanitize successful random designs after generation. |
| April 1895 | Empire of Japan | CA | Nokogiri | 4603.6 | 1085588 | 1283.6 | Shared CA candidates were sanitized but rejected, then vanilla random generated this CA with torpedo launchers. Add/repair Japan 1895 CA shared coverage or sanitize successful random designs after generation. |
| February 1895 | Chinese Empire | TB | Y-3 | 329.2 | 170642.2 | 140 | Add a China 1895 TB template around 330 tons. |

Separate late-campaign entry from the same `Latest.log`:

| Turn | Country | Class | Generated design | Tons | Monthly build cost | Power projection | Note |
| --- | --- | --- | --- | ---: | ---: | ---: | --- |
| November 1921 | France | BB | Massena | 70703.6 | 61129780 | 18650.7 | This appears to be from a different/late campaign context in the same log, not the 1890s slow-turn run. |

## Current Latest.log Failed Generation Attempts

These attempts are strong shared-library candidates because they either consumed visible wall time or generated failure spam.

| Approx. turn context | Inferred country | Class | Hull ID | In-game hull name | Evidence | Library gap to consider |
| --- | --- | --- | --- | --- | --- | --- |
| February 1895 | German Empire | BB | `b2_friedrich` | Battleship III | Failed for `weight` and then failed after 4 tries. This was inside the largest visible country gap before Germany AI-build. | Add a German 1895 BB template for the `Battleship III` / `B_Friedrich` family, or prevent this hull from being selected when the generator cannot stay in weight. |
| February 1895 | Empire of Japan | CL | `cl_1_3mast_armored` | Belted 3-Mast Cruiser | Failed for missing/invalid `gun_4_x1` parts. This was inside the gap before Japan AI-build. | Add a Japan 1895 CL template for this early belted 3-mast cruiser family, or fix the 4-inch gun placement candidate. |
| May 1893 nearby context | British Empire likely, but not proven by hull country alone | CA | `ca_1_small` | Armored Cruiser I | Failed because `armouredcruiser_tower_main_5_small` and `armouredcruiser_tower_sec_5_small` were reported as unavailable/part-blocked despite being named. The hull supports Britain among other countries. | Add an 1893-ish small armored-cruiser fallback for nations using `ca_1_small`, especially Britain if this repeats. |
| October 1890 nearby context | Unclear from log; hull supports China, Italy, Portugal | BB | `b1_maine_varsides` | Experimental Turret Ship | Failed because `pre-dreadnought_tower_main_7` was part-blocked. The adjacent AI marker is Britain, but the hull itself does not support Britain, so do not assign this one without better instrumentation. | Add or repair an early 1890 experimental turret-ship fallback if China/Italy/Portugal can hit this hull in campaigns. |
| Separate late-campaign context | Unclear from log; hull supports Russia/China/Austria | BB | `bb_4` | Dreadnought IV | Failed for missing `tower_sec` after a 1921 next-turn block. No adjacent AI-build marker was close enough to assign the country safely. | Only act if this repeats; add late dreadnought templates for Russia/China/Austria or improve secondary tower requirements. |

## Recent Archive Scan

The newest archived logs show many more manual generations outside the current 1890s slow-turn run. Treat this as backlog signal, not a precise current-save requirement.

High-level successful manual-generation counts from the latest archives:

| Year band | Signal |
| --- | --- |
| 1891-1896 | Repeated hand-built BB/CA/CL/TB coverage gaps across Britain, France, Germany, Japan, China, Russia, Spain, Italy, and Austria-Hungary. The current `Latest.log` entries above are the actionable subset from this live run. |
| 1916 | Very broad gap: BB, BC, CA, CL, and DD designs were generated for almost every major country in archived logs. If a 1916 start is important, shared templates are needed for all five main surface classes across majors. |
| 1920-1922 | Several late-start gaps: China BB/CL, France BB/BC/CA/CL/DD, Austria-Hungary BB/BC/CA/DD, Britain BB/CL, Soviet Union BB/CL/DD, Spain BB/BC/DD, Germany CL/DD, United States BC/CA/DD. |

## Recommended Next Instrumentation

For successful designs, add a temporary log line around `GenerateRandomDesigns` or immediately after the returned design is selected:

- country
- turn date
- requested class
- selected hull ID
- hull display name
- generated design name
- success/failure
- elapsed milliseconds

That would let the shared-library update target exact hull families instead of only country/year/class/tonnage envelopes.

Also log whether a successful generated design came from `source=random` or `source=shared` and whether VP option sanitizers changed it. The Japan `Yamashio` and `Nokogiri` cases showed that shared-design candidates can be sanitized correctly, fail validation, and then fall back to vanilla random generation that reintroduces CA+ torpedo launchers.
