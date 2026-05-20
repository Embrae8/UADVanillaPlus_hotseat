# Current Run Missing Shared Designs

Source: `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log`

Generated from `[AI SharedDesign Gap]` lines in inspected campaign logs. This file is cumulative: it keeps the earlier 1886-campaign snapshot and appends the latest 1898-1900 snapshot below it.

The sortable CSV is `plans/shared-design-missing-designs-current-run.csv`. It currently contains 88 non-zero gap entries: 41 from the latest August 1898-July 1901 gap wave and 47 from the earlier February 1891-November 1893 snapshot.

Latest re-check: the current live log segment is now at July-September 1902, and July 1902, August 1902, and September 1902 all reported `[AI SharedDesign Gaps] ... entries=0`. No new gap rows were added after the July 1901 Italy CL row. The gun-length tech fix still appears active: `gun_mech_35` appears in `SharedDesign gun-length-tech-prune ... removedTechs=...` lines, not as an `exactMissing` tech blocker. Non-gap August 1902 reject signals are still worth watching: several Germany/Japan/Spain BB candidates failed validation after gun-length clamping, with candidate summaries classifying them as unlock/invalid-design rejects rather than `exactMissing` tech blockers. Italy `Ottaviano Augusto` plus Japan `Kankō` still failed internal-weight rescue, but accepted alternatives kept the monthly gap summary at zero.

## How To Read This

- `mixedRejects` means VP did try shared candidates, but the usable set was blocked by one or more real rejection causes. These are the best follow-up targets.
- `onlyTechBlocked` means a shared design existed, but current campaign tech could not use it. These are strong downgrade/prune targets.
- `onlyDuplicate` means a valid shared design existed, but it was rejected as a duplicate of the already represented design. These are mostly Mk II / variant opportunities.
- `targetTonsMax` is the practical design ceiling to aim for. It reflects shipyard and tech-tonnage constraints where the log had them.
- Earlier 1899-1900 DD gaps repeatedly pointed at the same 850-ton ceiling and usually failed on `gun_mech_35` / `$technology_name_gun_mech_34`; current post-fix rows have shifted to future hull/gun/projectile/engine tech and duplicate coverage.

## Earlier Snapshot - High Priority

These were the earlier 1886-campaign entries through November 1893.

| Turn | Country | Type | Target tons | Rejections | Follow-up |
| --- | --- | --- | ---: | --- | --- |
| February 1891 | Austro-Hungarian Empire | BB | 10,669 | tech:1, duplicate:1 | Add an 1891-safe Austria-Hungary BB around 10.7k tons. |
| March 1891 | France | CA | 4,500 | tech:1, duplicate:1 | Add a France CA at or below 4.5k tons with downgradable 1891-safe tech. |
| March 1891 | Spanish Empire | BB | 8,838 | shipyard:6, duplicate:1 | Add a smaller Spain BB below 8.8k tons. |
| June 1891 | British Empire | CA | 4,500 | tech:2, duplicate:1 | Add a Britain CA at or below 4.5k tons with downgradable 1891-safe tech. |
| June 1891 | Italian Empire | CA | 4,000 | tech:1, duplicate:1 | Add an Italy CA at or below 4.0k tons with downgradable 1891-safe tech. |
| August 1891 | German Empire | CL | 3,500 | tech:2, duplicate:1, other:1 | Add a Germany CL at or below 3.5k tons with downgradable 1891-safe tech. |
| January 1892 | Austro-Hungarian Empire | TB | 300 | tech:1, duplicate:1 | Add or adapt an Austria-Hungary TB at or below 300 tons with 1892-safe tech. |
| February 1892 | German Empire | BB | 14,756 | duplicate:2, other:3 | Add a Germany BB around 14.8k tons; inspect candidate details if the `other` rejection repeats. |
| April 1892 | Empire of Japan | BB | 13,756 | shipyard:3, duplicate:1 | Add a Japan BB at or below 13.8k tons. |
| May 1892 | Austro-Hungarian Empire | BB | 12,533 | tech:1, duplicate:1, other:1 | Add an Austria-Hungary BB around 12.5k tons with 1892-safe tech. |
| May 1892 | British Empire | BB | 16,088 | shipyard:2, duplicate:1 | Add a Britain BB at or below 16.1k tons. |
| June 1892 | Empire of Japan | CA | 4,500 | tech:4, duplicate:2 | Add a Japan CA at or below 4.5k tons with 1892-safe tech. |
| June 1892 | France | CA | 5,000 | tech:1, duplicate:1 | Add a France CA at or below 5.0k tons with 1892-safe tech. |
| July 1892 | British Empire | CA | 5,000 | tech:2, duplicate:1 | Add a Britain CA at or below 5.0k tons with 1892-safe tech. |
| August 1892 | Italian Empire | BB | 12,273 | shipyard:2, duplicate:1 | Add an Italy BB at or below 12.3k tons. |
| December 1892 | Italian Empire | CA | 5,000 | tech:2, duplicate:1 | Add an Italy CA at or below 5.0k tons with 1892-safe tech. |
| March 1893 | Austro-Hungarian Empire | TB | 500 | tech:1, duplicate:1 | Add an Austria-Hungary TB at or below 500 tons with 1893-safe tech. |
| April 1893 | Empire of Japan | BB | 13,756 | shipyard:3, duplicate:1 | Repeated Japan BB gap at or below 13.8k tons. |
| April 1893 | German Empire | BB | 14,756 | tech:1, duplicate:2, other:3 | Repeated Germany BB gap around 14.8k tons. |
| May 1893 | Austro-Hungarian Empire | BB | 12,533 | tech:1, duplicate:1, other:1 | Repeated Austria-Hungary BB gap around 12.5k tons. |
| May 1893 | British Empire | BB | 17,016 | shipyard:1, duplicate:1, other:1 | Add a Britain BB at or below 17.0k tons. |
| May 1893 | German Empire | CA | 5,000 | tech:2, duplicate:3 | Add a Germany CA at or below 5.0k tons with 1893-safe tech. |
| July 1893 | Empire of Japan | CA | 5,000 | tech:4, duplicate:2 | Repeated Japan CA gap, now at or below 5.0k tons. |
| July 1893 | Spanish Empire | BB | 12,494 | shipyard:1, tech:5, duplicate:2 | Add a Spain BB at or below 12.5k tons with 1893-safe tech. |
| August 1893 | British Empire | CA | 6,000 | tech:2, duplicate:1 | Add a Britain CA at or below 6.0k tons with 1893-safe tech. |
| August 1893 | Italian Empire | BB | 14,973 | duplicate:1, other:2 | Add an Italy BB at or below 15.0k tons. |
| September 1893 | British Empire | CL | 3,750 | tech:1, duplicate:3 | Add a Britain CL at or below 3.75k tons with 1893-safe tech. |
| November 1893 | Russian Empire | BB | 16,825 | duplicate:3, other:3 | Add a Russia BB at or below 16.8k tons. |

## Earlier Snapshot - Variant / Mk II Opportunities

These were rejected only because the candidate duplicated existing represented coverage:

| Turn | Country | Type | Target tons | Follow-up |
| --- | --- | --- | ---: | --- |
| February 1891 | British Empire | BB | 13,838 | Add a Britain BB Mk II / variant around 13.8k tons. |
| February 1891 | Empire of Japan | BB | 11,506 | Add a Japan BB Mk II / variant around 11.5k tons. |
| April 1891 | British Empire | BB | 13,838 | Same Britain BB variant gap repeated. |
| April 1891 | Italian Empire | BB | 9,838 | Add an Italy BB Mk II / variant around 9.8k tons. |
| August 1891 | Chinese Empire | TB | 300 | Optional China TB variant. |
| August 1891 | Spanish Empire | TB | 275 | Optional Spain TB variant. |
| October 1891 | Russian Empire | TB | 300 | Optional Russia TB variant. |
| December 1891 | German Empire | TB | 300 | Optional Germany TB variant. |
| January 1892 | Empire of Japan | TB | 300 | Optional Japan TB variant. |
| February 1892 | British Empire | TB | 300 | Optional Britain TB variant. |
| February 1892 | Italian Empire | TB | 300 | Optional Italy TB variant. |
| October 1892 | Chinese Empire | TB | 300 | Optional China TB variant; duplicate-only gap repeated. |
| December 1892 | France | TB | 500 | Optional France TB variant. |
| December 1892 | German Empire | TB | 300 | Optional Germany TB variant; duplicate-only gap repeated. |
| December 1892 | Spanish Empire | TB | 300 | Optional Spain TB variant; duplicate-only gap repeated. |
| February 1893 | Empire of Japan | TB | 300 | Optional Japan TB variant; duplicate-only gap repeated. |
| May 1893 | Italian Empire | TB | 300 | Optional Italy TB variant; duplicate-only gap repeated. |
| June 1893 | Russian Empire | TB | 500 | Optional Russia TB variant. |
| November 1893 | British Empire | TB | 500 | Optional Britain TB variant. |

## Earlier Snapshot - Suggested Build Order

1. Spain BB under 8,838 tons, because repeated shipyard rejection means existing BB blueprints are simply too large.
2. Japan/Britain/France/Germany/Italy CA entries, because they are constrained by hard 4,500-6,000 ton tech limits and tech downgrades should help.
3. Spain, Japan, Britain, Italy, Germany, Russia, and Austria-Hungary BB entries for 1892-1893 shipyard ceilings, because the newer gaps are mostly larger BB variants being too large, stale, or duplicated.
4. Britain CL under 3,750 tons and Germany CL under 3,500 tons, because both have tech rejection signals.
5. Austria-Hungary TB entries under 300-500 tons, because they have real tech rejection but are less strategically important than BB/CA/CL coverage.
6. BB Mk II variants for Britain, Japan, Italy, Germany, and Russia.
7. Optional TB variants for China, Spain, Russia, Germany, Japan, Britain, Italy, and France.

## Latest Snapshot - High Priority

These entries were added from the latest inspected August 1898-July 1901 non-zero gap rows. Later checks through September 1902 had no new non-zero shared-design gap entries.

| Turn | Country | Type | Target tons | Rejections | Follow-up |
| --- | --- | --- | ---: | --- | --- |
| August 1898 | Britain | CA | 12,000 | tech:1, duplicate:2, other:1 | Add or adapt a Britain CA at or below 12.0k tons with 1898-safe tech and a non-duplicate fingerprint. |
| November 1898 | German Empire | CA | 12,000 | duplicate:2, other:1 | Add or adapt a Germany CA at or below 12.0k tons; inspect the `other` rejection if it repeats. |
| February 1899 | Chinese Empire | DD | 850 | tech:1 | Add or downgrade a China DD at or below 850 tons with 1899-safe gun/mechanism tech. |
| February 1899 | Empire of Japan | DD | 850 | tech:2 | Add or downgrade a Japan DD at or below 850 tons with 1899-safe gun/mechanism tech. |
| February 1899 | France | DD | 850 | tech:1 | Add or downgrade a France DD at or below 850 tons; this also had a failed `dd_1_france` random fallback in February. |
| February 1899 | German Empire | DD | 850 | tech:1, other:1 | Add or downgrade a Germany DD at or below 850 tons; inspect the `other` rejection if it repeats. |
| February 1899 | Italian Empire | DD | 850 | tech:2 | Add or downgrade an Italy DD at or below 850 tons with 1899-safe gun/mechanism tech. |
| March 1899 | Britain | DD | 850 | tech:1 | Add or downgrade a Britain DD at or below 850 tons with 1899-safe gun/mechanism tech. |
| April 1899 | Austro-Hungarian Empire | DD | 850 | tech:2 | Add or downgrade an Austria-Hungary DD at or below 850 tons. Prefer a lighter 1899-safe variant; the 897.7t candidate was also over the tech tonnage cap. |
| April 1899 | France | DD | 850 | tech:1 | Repeated France DD gap at or below 850 tons; April fell back to a random `Bisson` design after shared rejection. |
| May 1899 | German Empire | CL | 6,500 | tech:1, duplicate:2 | Add or downgrade a Germany CL at or below 6.5k tons with 1899-safe tech and a non-duplicate variant. |
| May 1899 | Soviet Union | DD | 850 | tech:1 | Add or downgrade a Russia/Soviet DD at or below 850 tons with 1899-safe gun/mechanism tech. |
| May 1899 | Spanish Empire | DD | 850 | tech:1 | Add or downgrade a Spain DD at or below 850 tons with 1899-safe gun/mechanism tech. |
| November 1899 | Britain | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a Britain CL at or below 6.5k tons with 1899-safe tech and a non-duplicate variant. |
| November 1899 | Chinese Empire | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a China CL at or below 6.5k tons with 1899-safe tech and a non-duplicate variant. |
| March 1900 | Britain | DD | 850 | tech:1 | Add or downgrade a Britain DD at or below 850 tons with 1900-safe gun/mechanism tech. |
| April 1900 | Empire of Japan | DD | 850 | tech:3 | Add or downgrade a Japan DD at or below 850 tons with 1900-safe gun/mechanism tech. |
| April 1900 | France | DD | 850 | tech:2 | Add or downgrade a France DD at or below 850 tons with 1900-safe gun/mechanism tech. |
| April 1900 | Spanish Empire | CL | 6,500 | tech:2, duplicate:1 | Add or downgrade a Spain CL at or below 6.5k tons with 1900-safe tech and a non-duplicate variant. |
| May 1900 | Chinese Empire | DD | 850 | tech:2 | Add or downgrade a China DD at or below 850 tons with 1900-safe gun/mechanism tech. |
| May 1900 | German Empire | CL | 6,500 | tech:2, duplicate:1 | Add or downgrade a Germany CL at or below 6.5k tons with 1900-safe tech and a non-duplicate variant. |
| May 1900 | Soviet Union | CL | 6,500 | tech:1, duplicate:2 | Add or downgrade a Russia/Soviet CL at or below 6.5k tons with 1900-safe tech and a non-duplicate variant. |
| June 1900 | German Empire | DD | 850 | tech:2, other:1 | Add or downgrade a Germany DD at or below 850 tons with 1900-safe gun/mechanism tech and already-unlocked DD hull/funnel/torpedo parts. |
| July 1900 | Spanish Empire | DD | 850 | tech:1 | Add or downgrade a Spain DD at or below 850 tons with 1900-safe gun/mechanism tech. |
| September 1900 | German Empire | DD | 850 | tech:2, other:1 | Repeated Germany DD gap: stale `gun_mech_35` plus V-2 unlock/part blocks. |
| October 1900 | German Empire | DD | 850 | tech:2, other:1 | Repeated Germany DD gap; October fell back to random `V-8`. |
| November 1900 | Soviet Union | DD | 850 | tech:1 | Add or downgrade a Russia/Soviet DD at or below 850 tons with 1900-safe gun/mechanism tech. |
| January 1901 | Austro-Hungarian Empire | CL | 6,500 | tech:1 | Add or downgrade an Austria-Hungary CL at or below 6.5k tons with 1901-safe hull/gun/engine tech; `gun_mech_35` was pruned, but `Korneuburg` still needs 1902-1904 tech. |
| April 1901 | Chinese Empire | CL | 6,500 | tech:2 | Add or downgrade a China CL at or below 6.5k tons with 1901-safe hull/gun/projectile tech; `gun_mech_35` was pruned. |
| April 1901 | Soviet Union | CA | 12,500 | duplicate:2, other:1 | Add a Russia/Soviet CA variant at or below 12.5k tons; current choices were duplicate or obsolete. |
| April 1901 | Spanish Empire | CL | 6,500 | tech:2 | Add or downgrade a Spain CL at or below 6.5k tons with 1901-safe hull/gun/projectile tech; `gun_mech_35` was pruned. |
| May 1901 | France | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a France CL at or below 6.5k tons with 1901-safe tech and a non-duplicate fingerprint. |
| May 1901 | German Empire | CL | 6,500 | tech:2 | Add or downgrade a Germany CL at or below 6.5k tons with 1901-safe hull/gun/projectile tech. |
| May 1901 | Soviet Union | CL | 6,500 | tech:1, duplicate:1 | Add or downgrade a Russia/Soviet CL at or below 6.5k tons with 1901-safe tech and a non-duplicate fingerprint. |
| July 1901 | Italian Empire | CL | 6,500 | tech:1 | Add or downgrade an Italy CL at or below 6.5k tons with 1901-safe hull/gun/engine/projectile tech. |

## Latest Snapshot - Variant / Mk II Opportunities

These were rejected only because the candidate duplicated existing represented coverage:

| Turn | Country | Type | Target tons | Follow-up |
| --- | --- | --- | ---: | --- |
| November 1898 | Britain | CL | 6,500 | Add a Britain CL Mk II / variant at or below 6.5k tons. The run later created `Dunedin` randomly. |
| March 1899 | Italian Empire | CL | 6,500 | Add an Italy CL Mk II / variant at or below 6.5k tons. The run later created `Giussano` randomly. |
| March 1900 | Austro-Hungarian Empire | CL | 6,500 | Add an Austria-Hungary CL Mk II / variant at or below 6.5k tons. The run later created `Schwechat` randomly at 6,580t. |
| May 1900 | Italian Empire | CL | 6,500 | Add an Italy CL Mk II / variant at or below 6.5k tons. The run later created `Giuseppe Garibaldi` randomly. |
| April 1901 | German Empire | TB | 800 | Add a Germany TB variant at or below 800 tons. `MHMW S-2` exists but was duplicate-skipped. |
| June 1901 | Chinese Empire | TB | 800 | Add a China TB variant at or below 800 tons. `MHMW Y-1` exists but was duplicate-skipped. |

## Latest Snapshot - Random Fallbacks Seen

These designs were created by the random path after shared candidates were attempted but not accepted. They are useful examples for the shared library even when the explicit gap row was only duplicate- or tech-blocked.

| Turn | Country | Type | Random design | Tons | Notes |
| --- | --- | --- | --- | ---: | --- |
| November 1898 | Britain | CL | Dunedin | 3,011.8 | Follow-up to the Britain CL variant gap. |
| February 1899 | German Empire | DD | V-1 | 820.9 | Shared DD candidate failed, then random succeeded. |
| February 1899 | Italian Empire | DD | Granatiere | 817.1 | Shared DD candidate failed, then random succeeded. |
| February 1899 | Empire of Japan | DD | Hagikaze | 809.2 | Shared DD candidate failed, then random succeeded. |
| February 1899 | Chinese Empire | DD | Kangzhuang | 845.1 | Shared DD candidate failed, then random succeeded near the 850-ton cap. |
| March 1899 | Britain | DD | Vanquisher | 824.1 | Shared DD candidate failed, then random succeeded. |
| March 1899 | Italian Empire | CL | Giussano | 4,548 | Follow-up to the Italy CL variant gap. |
| April 1899 | France | DD | Bisson | 841.2 | Shared DD candidate failed, then random succeeded after a `dd_1_france` part failure. |
| April 1899 | Austro-Hungarian Empire | DD | Tatra | 749.3 | Shared DD candidates failed; random had two `dd_1_austria` weight failures before succeeding. |
| May 1899 | German Empire | CL | Pillau | 2,410.3 | Follow-up to the Germany CL mixed gap. |
| May 1899 | Soviet Union | DD | Zante | 693.5 | Follow-up to the Russia/Soviet DD tech-blocked gap. |
| May 1899 | Spanish Empire | DD | Aguila | 762.9 | Follow-up to the Spain DD tech-blocked gap. |
| November 1899 | Britain | CL | Cassandra | 4,774.3 | Follow-up to the Britain CL mixed gap. |
| November 1899 | Chinese Empire | CL | Tieling | 5,898.4 | Follow-up to the China CL mixed gap. |
| March 1900 | Britain | DD | Thanet | 745.7 | Shared DD candidate failed on tech, then random succeeded. |
| March 1900 | Austro-Hungarian Empire | CL | Schwechat | 6,580.0 | Follow-up to the Austria-Hungary CL variant gap; note the random result is just above the 6.5k target. |
| April 1900 | France | DD | Fantassin | 756.7 | Shared DD candidates failed on tech, then random succeeded. |
| April 1900 | Empire of Japan | DD | Susuki | 780.6 | Shared DD candidates failed on tech, then random succeeded. |
| April 1900 | Spanish Empire | CL | Aragon | 6,146.7 | Follow-up to the Spain CL mixed tech/duplicate gap. |
| May 1900 | German Empire | CL | Sperber | 5,704.4 | Follow-up to the Germany CL mixed tech/duplicate gap. |
| May 1900 | Soviet Union | CL | Kuban | 6,452.3 | Follow-up to the Russia/Soviet CL mixed tech/duplicate gap. |
| May 1900 | Italian Empire | CL | Giuseppe Garibaldi | 3,283.7 | Follow-up to the Italy CL variant gap. |
| May 1900 | Chinese Empire | DD | Jinyu | 752.1 | Shared DD candidates failed on tech, then random succeeded. |
| July 1900 | Spanish Empire | DD | Almirante Ferrandiz | 625.9 | Shared DD candidate failed on tech, then random succeeded. |
| October 1900 | German Empire | DD | V-8 | 812.2 | Follow-up to the repeated Germany DD mixed tech/unlock gap. |
| November 1900 | Soviet Union | DD | Statnyi | 840.7 | Shared DD candidate failed on tech, then random succeeded near the 850-ton cap. |
| January 1901 | Austro-Hungarian Empire | CL | Traiskirchen | 5,368.9 | Shared CL candidate failed on non-gun-length tech, then random succeeded. |
| April 1901 | Soviet Union | CA | Amerika | 9,243.8 | Shared CA candidates were duplicate/obsolete, then random succeeded. |
| April 1901 | Spanish Empire | CL | Concordia | 3,963.1 | Shared CL candidates failed on non-gun-length tech, then random succeeded. |
| April 1901 | Chinese Empire | CL | Shaoyang | 3,465.8 | Shared CL candidates failed on non-gun-length tech, then random succeeded. |
| May 1901 | France | CL | Lavoisier | 5,233.2 | Shared CL candidates failed on tech/duplicate, then random succeeded. |
| May 1901 | German Empire | CL | Stuttgart | 3,582.5 | Shared CL candidates failed on non-gun-length tech, then random succeeded. |
| May 1901 | Soviet Union | CL | Admiral Butakov | 5,317.4 | Shared CL candidates failed on tech/duplicate, then random succeeded. |
| July 1901 | Italian Empire | CL | Scipione Africano | 4,467.2 | Shared CL candidate failed on non-gun-length tech, then random succeeded. |

## Latest Snapshot - Suggested Build Order

1. Add 1899-1900 DD coverage first for France, Britain, Germany, Italy, Japan, China, Austria-Hungary, Soviet Union/Russia, and Spain at or below 850 tons. The historical blocker was gun/mechanism tech, but the current April 1901 log shows `gun_mech_35` being pruned successfully.
2. Add Britain and Germany 1898 CA variants at or below 12.0k tons. These are the only current-run CA gaps and both have non-duplicate rejection signals.
3. Add Germany, Britain, China, Italy, France, Spain, Austria-Hungary, and Russia/Soviet CL variants or downgrades at or below 6.5k tons so the builder has non-duplicate fresh-cover options instead of falling back to random designs. The 1901 CL failures are now mostly future hull/gun/projectile/engine tech and oversize candidates, not `gun_mech_35`.
4. If generation failures keep repeating, inspect the vanilla fallback hulls `dd_1_france`, `dd_1_austria`, `dd_1_german`, `ca_4_italy`, `tb_highbow_large`, `cl_1_medium_strbow`, `cl_2_rambow`, `cl_1_straightbow`, and `b3_britain`; those are failures observed in the same log but are less directly tied to shared-library rows than the gap table above.
