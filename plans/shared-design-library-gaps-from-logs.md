# Shared Design Library Gaps From Logs

This note lists AI-created ship designs and shared-design rejection signals seen in recent MelonLoader logs. The goal is to identify missing shared-design-library coverage so the campaign can reuse predefined designs instead of spending turn time on random generation.

## Method

- Primary source: `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log`.
- Supplemental source: the newest archived logs under `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Logs`.
- Strongest missing-library signal: `[AI SharedDesign Gap]` rows with `mixedRejects` or `onlyTechBlocked`.
- Useful successful-manual signal: `[AI DesignGen] result ... source=random` after `sharedAttempted=1 sharedAccepted=0`.
- Caveat: current diagnostics still do not print the final hull ID for successful generated designs. Failed generation attempts do print hull IDs, so those are listed separately when useful.

## Current Latest.log Shared-Design Gaps

The current-run sortable details are in `plans/shared-design-missing-designs-current-run.csv`, and the human-readable action list is in `plans/shared-design-missing-designs-current-run.md`. The latest live log segment is now at July-September 1902, and July 1902, August 1902, and September 1902 all reported `entries=0`; the last non-zero rows remain the May-July 1901 gaps. The gun-length tech fix remains active in this log: `gun_mech_35` appears in `SharedDesign gun-length-tech-prune ... removedTechs=...` lines, not as an `exactMissing` tech blocker. August 1902 did have non-gap reject signals after pruning/clamping, but accepted fallback shared candidates kept the summary at zero.

High-priority current gaps:

| Turn | Country | Class | Target tons | Rejection summary | Library gap to consider |
| --- | --- | --- | ---: | --- | --- |
| August 1898 | Britain | CA | 12,000 | tech:1, duplicate:2, other:1 | Add or adapt a Britain 1898 CA at or below 12.0k tons with safer tech and a non-duplicate fingerprint. |
| November 1898 | German Empire | CA | 12,000 | duplicate:2, other:1 | Add or adapt a Germany 1898 CA at or below 12.0k tons; inspect the `other` rejection if it repeats. |
| February 1899 | Chinese Empire | DD | 850 | tech:1 | Add or downgrade a China 1899 DD at or below 850 tons. |
| February 1899 | Empire of Japan | DD | 850 | tech:2 | Add or downgrade a Japan 1899 DD at or below 850 tons. |
| February 1899 | France | DD | 850 | tech:1 | Add or downgrade a France 1899 DD at or below 850 tons. |
| February 1899 | German Empire | DD | 850 | tech:1, other:1 | Add or downgrade a Germany 1899 DD at or below 850 tons. |
| February 1899 | Italian Empire | DD | 850 | tech:2 | Add or downgrade an Italy 1899 DD at or below 850 tons. |
| March 1899 | Britain | DD | 850 | tech:1 | Add or downgrade a Britain 1899 DD at or below 850 tons. |
| April 1899 | Austro-Hungarian Empire | DD | 850 | tech:2 | Add or downgrade an Austria-Hungary 1899 DD at or below 850 tons. |
| April 1899 | France | DD | 850 | tech:1 | Repeated France DD gap at or below 850 tons. |
| May 1899 | German Empire | CL | 6,500 | tech:1, duplicate:2 | Add or downgrade a Germany 1899 CL at or below 6.5k tons with safer tech and a non-duplicate fingerprint. |
| May 1899 | Soviet Union | DD | 850 | tech:1 | Add or downgrade a Russia/Soviet 1899 DD at or below 850 tons. |
| May 1899 | Spanish Empire | DD | 850 | tech:1 | Add or downgrade a Spain 1899 DD at or below 850 tons. |
| November 1899 | Britain | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a Britain 1899 CL at or below 6.5k tons with safer tech and a non-duplicate fingerprint. |
| November 1899 | Chinese Empire | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a China 1899 CL at or below 6.5k tons with safer tech and a non-duplicate fingerprint. |
| March 1900 | Britain | DD | 850 | tech:1 | Add or downgrade a Britain 1900 DD at or below 850 tons without `gun_mech_35`. |
| April 1900 | Empire of Japan | DD | 850 | tech:3 | Add or downgrade a Japan 1900 DD at or below 850 tons without `gun_mech_35`. |
| April 1900 | France | DD | 850 | tech:2 | Add or downgrade a France 1900 DD at or below 850 tons without `gun_mech_35`. |
| April 1900 | Spanish Empire | CL | 6,500 | tech:2, duplicate:1 | Add or downgrade a Spain 1900 CL at or below 6.5k tons with safer tech and a non-duplicate fingerprint. |
| May 1900 | Chinese Empire | DD | 850 | tech:2 | Add or downgrade a China 1900 DD at or below 850 tons without `gun_mech_35`. |
| May 1900 | German Empire | CL | 6,500 | tech:2, duplicate:1 | Add or downgrade a Germany 1900 CL at or below 6.5k tons with safer tech and a non-duplicate fingerprint. |
| May 1900 | Soviet Union | CL | 6,500 | tech:1, duplicate:2 | Add or downgrade a Russia/Soviet 1900 CL at or below 6.5k tons with safer tech and a non-duplicate fingerprint. |
| June 1900 | German Empire | DD | 850 | tech:2, other:1 | Add or downgrade a Germany 1900 DD at or below 850 tons without `gun_mech_35`, and avoid the V-2 unlock/part blockers. |
| July 1900 | Spanish Empire | DD | 850 | tech:1 | Add or downgrade a Spain 1900 DD at or below 850 tons without `gun_mech_35`. |
| September 1900 | German Empire | DD | 850 | tech:2, other:1 | Repeated Germany 1900 DD gap; V-1 variants still need stale gun-length tech, while V-2 is unlock/part blocked. |
| October 1900 | German Empire | DD | 850 | tech:2, other:1 | Repeated Germany 1900 DD gap before random `V-8` succeeded at 812.2t. |
| November 1900 | Soviet Union | DD | 850 | tech:1 | Add or downgrade a Russia/Soviet 1900 DD at or below 850 tons without `gun_mech_35`. |
| January 1901 | Austro-Hungarian Empire | CL | 6,500 | tech:1 | Add or downgrade an Austria-Hungary 1901 CL at or below 6.5k tons with 1901-safe hull/gun/engine tech; `gun_mech_35` was pruned successfully. |
| April 1901 | Chinese Empire | CL | 6,500 | tech:2 | Add or downgrade a China 1901 CL at or below 6.5k tons with 1901-safe hull/gun/projectile tech; `gun_mech_35` was pruned successfully. |
| April 1901 | Soviet Union | CA | 12,500 | duplicate:2, other:1 | Add a Russia/Soviet 1901 CA variant; shared candidates were duplicate or obsolete. |
| April 1901 | Spanish Empire | CL | 6,500 | tech:2 | Add or downgrade a Spain 1901 CL at or below 6.5k tons with 1901-safe hull/gun/projectile tech; `gun_mech_35` was pruned successfully. |
| May 1901 | France | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a France 1901 CL at or below 6.5k tons with 1901-safe tech and a non-duplicate fingerprint. |
| May 1901 | German Empire | CL | 6,500 | tech:2 | Add or downgrade a Germany 1901 CL at or below 6.5k tons with 1901-safe hull/gun/projectile tech. |
| May 1901 | Soviet Union | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a Russia/Soviet 1901 CL at or below 6.5k tons with 1901-safe tech and a non-duplicate fingerprint. |
| July 1901 | Italian Empire | CL | 6,500 | tech:1 | Add or downgrade an Italy 1901 CL at or below 6.5k tons with 1901-safe hull/gun/engine/projectile tech. |

Variant opportunities:

| Turn | Country | Class | Target tons | Library gap to consider |
| --- | --- | --- | ---: | --- |
| November 1898 | Britain | CL | 6,500 | Add a Britain CL Mk II / variant so the fresh-cover path does not fall back to random generation. |
| March 1899 | Italian Empire | CL | 6,500 | Add an Italy CL Mk II / variant so the fresh-cover path does not fall back to random generation. |
| March 1900 | Austro-Hungarian Empire | CL | 6,500 | Add an Austria-Hungary CL Mk II / variant so the fresh-cover path does not fall back to random generation. |
| May 1900 | Italian Empire | CL | 6,500 | Add an Italy CL Mk II / variant so the fresh-cover path does not fall back to random generation. |
| April 1901 | German Empire | TB | 800 | Add a Germany TB variant so the fresh-cover path does not duplicate `MHMW S-2`. |
| June 1901 | Chinese Empire | TB | 800 | Add a China TB variant so the fresh-cover path does not duplicate `MHMW Y-1`. |

## Recent Turn Shared-Design Drilldown

The recent 1900-1901 turns now add missing-library rows starting in March 1900. Shared successes in January/February 1900 were healthy, March-May show the recurring tech/duplicate failure modes that drive random generation, June-November add a sharper Germany DD unlock/part signal, and the 1901 rows show the gun-length fix working while other future tech and duplicate/obsolete gaps remain.

| Turn | Country | Class | Outcome | Why it matters |
| --- | --- | --- | --- | --- |
| January 1900 | France | TB | Shared accepted `MHMW Fauconneau` at 765.7t. | Clean success after pruning disabled mine tech; no rejection signal. |
| January 1900 | Soviet Union | BB | Shared accepted `MHMW Georgii Pobedonosets Mk II` at 15,191.3t. | `MHMW Navarin` was skipped as a duplicate, but two other BBs were accepted. |
| January 1900 | Spanish Empire | CA | Shared accepted `MHMW Nuestra Senora del Pilar Mk II` at 9,636.8t. | Candidate pool was healthy: three CA candidates accepted, no rejects. |
| January 1900 | Chinese Empire | TB | Shared accepted `MHMW Y-1` at 738.2t. | Clean success, no rejection signal. |
| February 1900 | France | none | No design created; shared was not attempted. | `gateReason=replacement-count-blocked` with all main surface classes already represented, so this is not a shared-design failure. |
| February 1900 | Soviet Union | CA | Shared accepted `MHMW Bayan` at 10,195.7t. | `MHMW Derbent` was duplicate-skipped and `MHMW Severnaya Zvezda` was obsolete, but `Bayan` remained valid. |
| February 1900 | Austro-Hungarian Empire | CA | Shared accepted `MHMW Steyr` at 9,672.1t. | `MHMW Kaiser Franz Joseph I` was tech-rejected on 1901/1902 hull/global tech plus `gun_mech_35` and `gun_main_12`; older CA options still covered the request. |
| March 1900 | Britain | DD | Shared rejected `MHMW Lydiard`; random created `Thanet` at 745.7t. | The only DD candidate was under the 850t cap but tech-blocked on `gun_mech_35`. |
| March 1900 | Austro-Hungarian Empire | CL | Shared rejected `MHMW Maros`; random created `Schwechat` at 6,580t. | `Maros` duplicated existing coverage; add a non-duplicate CL variant at or below 6.5k tons. |
| April 1900 | Empire of Japan | DD | Shared rejected `MHMW Nire`, `Matsukaze`, and `MHMW Oboro`; random created `Susuki` at 780.6t. | All three DD candidates were blocked by `gun_mech_35`. |
| April 1900 | France | DD | Shared rejected `MHMW Fantassin` and `Voltigeur`; random created `Fantassin` at 756.7t. | Both DD candidates were blocked by `gun_mech_35`. |
| April 1900 | Spanish Empire | CL | Shared rejected `MHMW Berenguela`, `MHMW Libertad`, and `MHMW Navas de Tolosa`; random created `Aragon` at 6,146.7t. | `Berenguela`/`Libertad` used 1901-1903 hull/gun tech including `gun_mech_35`, while `Navas de Tolosa` duplicated existing coverage. |
| May 1900 | German Empire | CL | Shared rejected `MHMW Berlin`, `MHMW Amazone`, and `MHMW Hamburg`; random created `Sperber` at 5,704.4t. | `Berlin`/`Amazone` used 1901-1903 hull/gun tech including `gun_mech_35`; `Hamburg` duplicated existing coverage. |
| May 1900 | Soviet Union | CL | Shared rejected `MHMW Rion`, `MHMW Bogatyr`, and `MHMW Diana`; random created `Kuban` at 6,452.3t. | `Rion` used 1901-1902 hull/gun tech including `gun_mech_35` and `gun_mech_51`; `Bogatyr`/`Diana` duplicated existing coverage. |
| May 1900 | Italian Empire | CL | Shared rejected `MHMW Confienza`; random created `Giuseppe Garibaldi` at 3,283.7t. | The only CL candidate was a duplicate, so this is a pure variant gap. |
| May 1900 | Chinese Empire | DD | Shared rejected `MHMW Fuxin` and `Xiejiawan`; random created `Jinyu` at 752.1t. | Both DD candidates were blocked by `gun_mech_35`. |
| June 1900 | German Empire | DD | Shared rejected `MHMW V-2`, `MHMW V-1`, and `V-1`; random generation failed on `dd_1_german`. | V-1 variants were blocked by stale `gun_mech_35`; V-2 was blocked by unlock/part availability (`dd_1_german_large`, `small_dd_funnel_1`, `torpedo_x2`). |
| July 1900 | Spanish Empire | DD | Shared rejected `MHMW Huesca`; random created `Almirante Ferrandiz` at 625.9t. | Huesca only failed on stale `gun_mech_35`. |
| September 1900 | German Empire | DD | Same Germany DD candidate pattern repeated; random generation again failed on `dd_1_german`. | This confirms the Germany issue is not just missing a template: it also has an unlock/part block. |
| October 1900 | German Empire | DD | Same Germany DD candidate pattern repeated; random created `V-8` at 812.2t. | Add/fix Germany DD template, but also inspect V-2 unlock/part requirements. |
| November 1900 | Soviet Union | DD | Shared rejected `MHMW Rodislav`; random created `Statnyi` at 840.7t. | Rodislav only failed on stale `gun_mech_35`. |
| January 1901 | Austro-Hungarian Empire | CL | Shared rejected `MHMW Korneuburg`; random created `Traiskirchen` at 5,368.9t. | `gun_mech_35`/`gun_mech_36` were pruned first, but Korneuburg still needs 1902-1904 hull/gun/engine tech and is 8,032.4t against a 6,500t target. |
| April 1901 | Chinese Empire | CL | Shared rejected `MHMW Cangzhou` and `MHMW Zhoushan`; random created `Shaoyang` at 3,465.8t. | `gun_mech_35` was pruned first; remaining blockers are 1902-1903 hull/gun/projectile tech. |
| April 1901 | German Empire | TB | Shared rejected `MHMW S-2`; no random fallback was needed for this row. | Pure duplicate-only gap. Add a Germany TB variant at or below 800t if fresh coverage is desired. |
| April 1901 | Soviet Union | CA | Shared rejected `MHMW Bayan`, `MHMW Derbent`, and `MHMW Severnaya Zvezda`; random created `Amerika` at 9,243.8t. | Bayan/Derbent duplicated existing coverage, while Severnaya Zvezda was obsolete. This is a variant/obsolete gap, not a gun-length issue. |
| April 1901 | Spanish Empire | CL | Shared rejected `MHMW Berenguela` and `MHMW Libertad`; random created `Concordia` at 3,963.1t. | `gun_mech_35` was pruned first; remaining blockers are 1902-1903 hull/gun/projectile tech. |
| May 1901 | France | CL | Shared rejected `MHMW Isly` and `MHMW Duguay-Trouin`; random created `Lavoisier` at 5,233.2t. | Isly needs 1902-1903 hull/gun/projectile tech after gun-length pruning; Duguay-Trouin duplicated existing coverage. |
| May 1901 | German Empire | CL | Shared rejected `MHMW Berlin` and `MHMW Amazone`; random created `Stuttgart` at 3,582.5t. | Both candidates need 1902-1903 hull/gun/projectile tech after gun-length pruning. |
| May 1901 | Soviet Union | CL | Shared rejected `MHMW Rion` and `MHMW Bogatyr`; random created `Admiral Butakov` at 5,317.4t. | Rion needs 1902 hull/gun/projectile tech after gun-length pruning; Bogatyr duplicated existing coverage. |
| June 1901 | Chinese Empire | TB | Shared rejected `MHMW Y-1`; no random fallback was needed for this row. | Pure duplicate-only gap. Add a China TB variant at or below 800t if fresh coverage is desired. |
| July 1901 | Italian Empire | CL | Shared rejected `MHMW Calabria`; random created `Scipione Africano` at 4,467.2t. | Calabria needs 1902-1904 hull/gun/engine/projectile tech after gun-length pruning and is 8,032.4t against a 6,500t target. |

Torpedo cleanup note: the February 1900 Soviet `Bayan` and Austria-Hungary `Steyr` CA accepts logged `torpedo-audit ... result=cache-leak` after cleanup, and later July/October/November repeats hit `Bayan`/`Steyr` again. Live/store launchers were zeroed, but the cache still reported four launchers. That did not block shared acceptance or building in this log, but it is worth watching if torpedo cleanup becomes a runtime issue.

## Current Latest.log Random Fallbacks

These designs were created by the random path after shared candidates were attempted but not accepted. They are good shared-library examples, especially for the 1899-1900 DD/CL wave.

| Turn | Country | Class | Generated design | Tons | Monthly build cost | Power projection | Library gap to consider |
| --- | --- | --- | --- | ---: | ---: | ---: | --- |
| November 1898 | Britain | CL | Dunedin | 3011.8 | 689132.6 | 920.6 | Add a Britain 1898-1899 CL variant; explicit gap row was duplicate-only at 6.5k tons. |
| February 1899 | German Empire | DD | V-1 | 820.9 | 480247.3 | 413.7 | Add a Germany 1899 DD around 821 tons. |
| February 1899 | Italian Empire | DD | Granatiere | 817.1 | 381485.7 | 343.6 | Add an Italy 1899 DD around 817 tons. |
| February 1899 | Empire of Japan | DD | Hagikaze | 809.2 | 541659.6 | 732.4 | Add a Japan 1899 DD around 809 tons. |
| February 1899 | Chinese Empire | DD | Kangzhuang | 845.1 | 473644.3 | 595.2 | Add a China 1899 DD just below the 850-ton cap. |
| March 1899 | Britain | DD | Vanquisher | 824.1 | 458308.3 | 637 | Add a Britain 1899 DD around 824 tons. |
| March 1899 | Italian Empire | CL | Giussano | 4548 | 893665.8 | 1225.6 | Add an Italy 1899 CL variant around 4.5k tons; explicit gap row was duplicate-only at 6.5k tons. |
| April 1899 | France | DD | Bisson | 841.2 | 464232 | 519.7 | Add a France 1899 DD around 841 tons. |
| April 1899 | Austro-Hungarian Empire | DD | Tatra | 749.3 | 449300.3 | 449.5 | Add an Austria-Hungary 1899 DD around 750 tons; this is safer than the logged 897.7t tech-blocked candidate. |
| May 1899 | German Empire | CL | Pillau | 2410.3 | 606293.2 | 731.3 | Add a Germany 1899 CL variant around 2.4k tons; explicit gap row had tech and duplicate rejection. |
| May 1899 | Soviet Union | DD | Zante | 693.5 | 464489.8 | 346.1 | Add a Russia/Soviet 1899 DD around 694 tons. |
| May 1899 | Spanish Empire | DD | Aguila | 762.9 | 382961.7 | 568.3 | Add a Spain 1899 DD around 763 tons. |
| November 1899 | Britain | CL | Cassandra | 4774.3 | 1389102 | 1290.4 | Add a Britain 1899 CL variant around 4.8k tons; explicit gap row had tech and duplicate rejection. |
| November 1899 | Chinese Empire | CL | Tieling | 5898.4 | 1247492 | 1644.3 | Add a China 1899 CL variant around 5.9k tons; explicit gap row had tech and duplicate rejection. |
| March 1900 | Britain | DD | Thanet | 745.7 | 485270.1 | 650.7 | Add a Britain 1900 DD around 746 tons without `gun_mech_35`. |
| March 1900 | Austro-Hungarian Empire | CL | Schwechat | 6580 | 1382766 | 2015.9 | Add an Austria-Hungary 1900 CL variant at or below 6.5k tons; random result is slightly over cap. |
| April 1900 | France | DD | Fantassin | 756.7 | 402684.9 | 415.9 | Add a France 1900 DD around 757 tons without `gun_mech_35`. |
| April 1900 | Empire of Japan | DD | Susuki | 780.6 | 567707.5 | 737.9 | Add a Japan 1900 DD around 781 tons without `gun_mech_35`. |
| April 1900 | Spanish Empire | CL | Aragon | 6146.7 | 1464959 | 1984.4 | Add a Spain 1900 CL around 6.1k tons with safer tech. |
| May 1900 | German Empire | CL | Sperber | 5704.4 | 1483094 | 1676.7 | Add a Germany 1900 CL around 5.7k tons with safer tech. |
| May 1900 | Soviet Union | CL | Kuban | 6452.3 | 1300214 | 2352.7 | Add a Russia/Soviet 1900 CL around 6.45k tons with safer tech. |
| May 1900 | Italian Empire | CL | Giuseppe Garibaldi | 3283.7 | 916523.8 | 1172 | Add an Italy 1900 CL variant around 3.3k tons. |
| May 1900 | Chinese Empire | DD | Jinyu | 752.1 | 522082.7 | 400.3 | Add a China 1900 DD around 752 tons without `gun_mech_35`. |
| July 1900 | Spanish Empire | DD | Almirante Ferrandiz | 625.9 | 420618.2 | 456.6 | Add a Spain 1900 DD around 626 tons without `gun_mech_35`. |
| October 1900 | German Empire | DD | V-8 | 812.2 | 506923.2 | 392.7 | Add a Germany 1900 DD around 812 tons with safe/unlocked parts. |
| November 1900 | Soviet Union | DD | Statnyi | 840.7 | 601758.7 | 437.5 | Add a Russia/Soviet 1900 DD near the 850-ton cap without `gun_mech_35`. |
| January 1901 | Austro-Hungarian Empire | CL | Traiskirchen | 5368.9 | 1838568 | 1616.5 | Add an Austria-Hungary 1901 CL around 5.4k tons with safe non-gun-length tech. |
| April 1901 | Soviet Union | CA | Amerika | 9243.8 | 2140410 | 1872.4 | Add a Russia/Soviet 1901 CA variant around 9.2k tons. |
| April 1901 | Spanish Empire | CL | Concordia | 3963.1 | 1044571 | 1511.8 | Add a Spain 1901 CL around 4.0k tons with safe non-gun-length tech. |
| April 1901 | Chinese Empire | CL | Shaoyang | 3465.8 | 834834.7 | 938.5 | Add a China 1901 CL around 3.5k tons with safe non-gun-length tech. |
| May 1901 | France | CL | Lavoisier | 5233.2 | 941656.4 | 1014.7 | Add a France 1901 CL around 5.2k tons with safe non-gun-length tech. |
| May 1901 | German Empire | CL | Stuttgart | 3582.5 | 1069992 | 1116.3 | Add a Germany 1901 CL around 3.6k tons with safe non-gun-length tech. |
| May 1901 | Soviet Union | CL | Admiral Butakov | 5317.4 | 994164.6 | 1321.8 | Add a Russia/Soviet 1901 CL around 5.3k tons with safe non-gun-length tech. |
| July 1901 | Italian Empire | CL | Scipione Africano | 4467.2 | 1004263 | 1525.9 | Add an Italy 1901 CL around 4.5k tons with safe non-gun-length tech. |

## Current Latest.log Failed Generation Attempts

These are not all shared-design-library failures, but they are useful context when choosing which templates to add or which vanilla hulls to watch.

| Approx. turn context | Inferred country | Class | Hull ID | Evidence | Library gap to consider |
| --- | --- | --- | --- | --- | --- |
| October 1898 nearby refit/generation context | Unclear, near Italy refit and Britain turn start | CL | `cl_1_medium_strbow` | Failed for `parts`, listing `armouredcruiser_tower_main_5_small: available`, then failed in 1 try. | Treat as lower confidence. Watch for repeat before assigning to a shared-library target. |
| February 1899 | France | DD | `dd_1_france` | France DD shared candidate was tech-blocked on `gun_mech_35`, then random generation failed after 4 tries. | Add a France 1899 DD shared fallback so the run avoids this random path. |
| March 1899 nearby context | Britain likely, but failure is before the DD shared attempt | BB | `b3_britain` | Failed for `parts`, listing `agamemnon_funnel_4: available`, then failed in 1 try. | Lower confidence for shared-library work unless this repeats; current explicit Britain gap is DD/CL, not BB. |
| April 1899 | France | DD | `dd_1_france` | Shared candidate failed on tech; random path logged `torpedo_x1: part` but later created `Bisson`. | Add a France 1899 DD fallback and keep an eye on torpedo part selection. |
| April 1899 | Austro-Hungarian Empire | DD | `dd_1_austria` | Shared candidates failed on tech; random path hit weight failures before creating `Tatra` at 749.3t. | Add a lighter Austria-Hungary 1899 DD fallback around 750 tons. |
| May 1900 | Soviet Union | CL | `cl_2_rambow` | Before `Kuban` succeeded, random CL generation hit one weight failure and one `gun_4_x1: shoot` parts failure. | Add a Russia/Soviet 1900 CL fallback around 6.45k tons to avoid this random path. |
| May 1900 | Italian Empire | CL | `cl_1_straightbow` | Before `Giuseppe Garibaldi` succeeded, random CL generation hit a weight failure. | Add an Italy 1900 CL variant around 3.3k tons. |
| June 1900 | German Empire | DD | `dd_1_german` | Shared Germany DD candidates failed first; random generation then failed `dd_1_german` in 4 tries. | Fix stale `gun_mech_35`, then inspect Germany DD random hull/part setup if this repeats. |
| September 1900 | German Empire | DD | `dd_1_german` | Same `dd_1_german` 4-try failure repeated before October's successful `V-8`. | Stronger signal that Germany DD hull/part selection needs follow-up. |
| October 1900 nearby context | Italian Empire likely nearby | CA | `ca_4_italy` | Failed for `parts`; all listed late CA tower/funnel parts were reported `available`. | Lower confidence shared-library issue, but worth a generator parts-placement check if it repeats. |
| November 1900 nearby context | Britain likely nearby | TB | `tb_highbow_large` | Failed for weight at 802.6t, over a TB target envelope. | Watch for repeat; likely fallback hull/template weight tuning, not shared-design table. |
| April 1901 | Soviet Union | CA | `ca_maine_threemast` | Random CA generation hit one weight-offset failure before creating `Amerika`. | Add a Russia/Soviet 1901 CA shared fallback so the run avoids this random path. |
| April 1901 | Chinese Empire | CL | `cl_1_rambow_3mast_ver2` | Random CL generation hit one weight-offset failure before creating `Shaoyang`. | Add a China 1901 CL shared fallback so the run avoids this random path. |
| May 1901 | France | CL | `cl_1_rambow_3mast_early` | Random CL generation hit one weight-offset failure and one `gun_4_x1: shoot` parts failure before creating `Lavoisier`. | Add a France 1901 CL shared fallback so the run avoids this random path. |
| May 1901 | Soviet Union | CL | `cl_1_rambow_3mast_early` | Random CL generation hit one weight-offset failure before creating `Admiral Butakov`. | Add a Russia/Soviet 1901 CL shared fallback so the run avoids this random path. |

## Other Reject Reasons To Inspect

- Current post-fix validation: `gun_mech_35` no longer appears as an `exactMissing` reject. It only appears in `SharedDesign gun-length-tech-prune ... removedTechs=...` lines, followed by normal clamp/validation behavior.
- Germany DD `build:unlock` was the most actionable non-`gun_mech_35` reject in the November 1900 log. `MHMW V-2` needed `dd_1_german_large` but the log said `noMaterializedHullPart`; it also saw `small_dd_funnel_1` blocked by `earlyDDfunnels_level_2`, and `torpedo_x2` was unlocked but still `partAvailable=false`.
- Germany random DD fallback failed twice on `dd_1_german` in 4 tries before October finally produced `V-8`.
- The latest 1901 `onlyTechBlocked` rows are now real future-tech/template-age issues: Austria-Hungary `Korneuburg`, China `Cangzhou`/`Zhoushan`, and Spain `Berenguela`/`Libertad` still require 1902-1904 hull/gun/projectile/engine tech after gun-length tech pruning.
- The newest May-July 1901 rows extend that same CL pattern to France `Isly`, Germany `Berlin`/`Amazone`, Soviet `Rion`, and Italy `Calabria`; none are blocked by `gun_mech_35` after pruning.
- Latest August 1902 non-gap BB rejections to watch: Germany `MHMW Helgoland`/`MHMW Kaiser Wilhelm der Grosse`, Japan `MHMW Aki`/`Kankō`, and Spain `MHMW Bahama`/`MHMW Carmen`/`MHMW Gallardo`/`MHMW Santa Barbara`/`Campeón` failed validation after gun-length clamping. Candidate summaries classify most of these as `build:unlock` hull/tower/funnel availability misses, while `Kankō`/`Campeón` are invalid-design rejects; they are not `exactMissing` tech blockers. Because Germany/Japan/Spain each still accepted other BB candidates, these did not create `[AI SharedDesign Gap]` rows.
- Latest internal-weight rescue rejects: Italy `Ottaviano Augusto` stayed invalid at roughly 6,516t after rescue, and Japan `Kankō` stayed invalid around 18,500t. Italy accepted `Montebello`; Japan accepted `Kumano`/`MHMW Yamashiro`, so these are cleanup targets rather than missing-library blockers.
- Soviet `Amerika` also confirms the random-design sanitizer path is active: the log removed 16 torpedo parts/components from the generated CA before it was added.
- `ca_4_italy` failed for `parts` even though the listed late CA tower/funnel options were all reported `available`; that looks more like placement/selection trouble than a direct missing shared design.
- `tb_highbow_large` failed for weight at 802.6t. Treat that as lower priority unless it repeats, because it is likely a vanilla random hull/template weight envelope issue.
- `torpedo-audit ... result=cache-leak` repeated for shared CAs (`Bayan`/`Steyr`), but did not block acceptance or building in this log.

## Older Current-Run Entries

The previous `Latest.log` snapshot was an 1891-1895 run. Keep these as backlog signal, not as the current save's latest state:

| Turn | Country | Class | Generated design | Tons | Note |
| --- | --- | --- | --- | ---: | --- |
| June 1894 | Austro-Hungarian Empire | BB | Ernst | 10068.5 | Add an Austria-Hungary 1894 BB/pre-dreadnought-era template. |
| December 1894 | Austro-Hungarian Empire | TB | Alk | 398.3 | Add an Austria-Hungary 1894 TB template. |
| November 1894 | British Empire | BB | Royal Oak | 15460.6 | Add a Britain 1894 BB template around 15.5k tons. |
| February 1895 | British Empire | CL | Retribution | 2764.4 | Add a Britain 1895 CL template around 2.8k tons. |
| February 1894 | France | CL | Jean Bart | 3935.2 | Add a France 1894 CL template around 3.9k tons. |
| February 1895 | France | CA | Tonnant | 6061.6 | Add a France 1895 CA template around 6.1k tons. |
| February 1895 | Empire of Japan | CL | Tenryu | 3054.8 | Add a Japan 1895 CL template around 3.1k tons. |
| February 1895 | Empire of Japan | BB | Yamashio | 11406.8 | Shared BB candidates were sanitized but rejected, then random generated a BB with torpedo launchers. |
| April 1895 | Empire of Japan | CA | Nokogiri | 4603.6 | Shared CA candidates were sanitized but rejected, then random generated a CA with torpedo launchers. |
| February 1895 | Chinese Empire | TB | Y-3 | 329.2 | Add a China 1895 TB template around 330 tons. |

Older failed-generation backlog:

| Approx. turn context | Inferred country | Class | Hull ID | In-game hull name | Evidence | Library gap to consider |
| --- | --- | --- | --- | --- | --- | --- |
| February 1895 | German Empire | BB | `b2_friedrich` | Battleship III | Failed for `weight` and then failed after 4 tries. | Add a German 1895 BB template for the `Battleship III` / `B_Friedrich` family, or prevent this hull from being selected when the generator cannot stay in weight. |
| February 1895 | Empire of Japan | CL | `cl_1_3mast_armored` | Belted 3-Mast Cruiser | Failed for missing/invalid `gun_4_x1` parts. | Add a Japan 1895 CL template for this early belted 3-mast cruiser family, or fix the 4-inch gun placement candidate. |
| May 1893 nearby context | British Empire likely, but not proven by hull country alone | CA | `ca_1_small` | Armored Cruiser I | Failed because small armored-cruiser towers were reported as unavailable/part-blocked despite being named. | Add an 1893-ish small armored-cruiser fallback for nations using `ca_1_small`, especially Britain if this repeats. |
| October 1890 nearby context | Unclear from log; hull supports China, Italy, Portugal | BB | `b1_maine_varsides` | Experimental Turret Ship | Failed because `pre-dreadnought_tower_main_7` was part-blocked. | Add or repair an early 1890 experimental turret-ship fallback if China/Italy/Portugal can hit this hull in campaigns. |
| Separate late-campaign context | Unclear from log; hull supports Russia/China/Austria | BB | `bb_4` | Dreadnought IV | Failed for missing `tower_sec` after a 1921 next-turn block. | Only act if this repeats; add late dreadnought templates for Russia/China/Austria or improve secondary tower requirements. |

## Recent Archive Scan

The newest archived logs show many more manual generations outside the current 1898-1899 run. Treat this as backlog signal, not a precise current-save requirement.

| Year band | Signal |
| --- | --- |
| 1891-1896 | Repeated hand-built BB/CA/CL/TB coverage gaps across Britain, France, Germany, Japan, China, Russia, Spain, Italy, and Austria-Hungary. |
| 1898-1900 | Current live run emphasizes 1899-1900 DD coverage across major nations, especially China, Japan, France, Germany, Italy, Britain, Austria-Hungary, Russia/Soviet, and Spain, plus Britain/Germany CA and Germany/Britain/China/Italy/Spain/Austria-Hungary/Russia-Soviet CL variants or downgrades. |
| 1916 | Very broad gap: BB, BC, CA, CL, and DD designs were generated for almost every major country in archived logs. If a 1916 start is important, shared templates are needed for all five main surface classes across majors. |
| 1920-1922 | Several late-start gaps: China BB/CL, France BB/BC/CA/CL/DD, Austria-Hungary BB/BC/CA/DD, Britain BB/CL, Soviet Union BB/CL/DD, Spain BB/BC/DD, Germany CL/DD, United States BC/CA/DD. |

## Recommended Next Instrumentation

For successful designs, add or keep a temporary log line around `GenerateRandomDesigns` or immediately after the returned design is selected:

- country
- turn date
- requested class
- selected hull ID
- hull display name
- generated design name
- success/failure
- elapsed milliseconds

That would let the shared-library update target exact hull families instead of only country/year/class/tonnage envelopes.

Also log whether a successful generated design came from `source=random` or `source=shared` and whether VP option sanitizers changed it. The old Japan `Yamashio` and `Nokogiri` cases showed that shared-design candidates can be sanitized correctly, fail validation, and then fall back to vanilla random generation that reintroduces CA+ torpedo launchers.
