# Battle AI Control Regression Handoff

Context: thinker-session handoff for the dedicated VP builder session. Do not
treat this doc as an implemented fix.

## Current Symptoms

- The `AI` button is visible again, but it is placed at the far right of the
  main battle command strip, after unrelated controls such as shell/targeting
  and speed controls.
- The expected location is the small division mode selector cluster with
  `Sail`, `Screen`, `Scout`, `Follow`, and `Retreat`.
- After clicking the button, the division did not visibly show as AI controlled
  in the usual UI state.

## Evidence From Latest.log

The current live log shows the click path did fire:

```text
UADVP battle AI control: added AI control button to battle division orders.
UADVP battle AI control: AI button ready for 1 selected division(s); root=baseOrders/Orders, rootActive=True, parent=Orders, parentActive=True, buttonActive=True.
UADVP battle AI control: enabled AI control for 1 selected division(s), 4 ship(s) via button.
UADVP battle AI control: cleared 1 division toggle(s) during LeaveBattle.
```

So the immediate problem is probably not that `onClick` is disconnected. It is
more likely one or both of these:

- placement is using the broad `baseOrders/Orders` parent instead of the
  narrower division-mode button parent;
- the UI status is being suppressed by the `UIDivision.RefreshUI` postfix that
  rewrites friendly VP AI labels away from vanilla's `$Ui_Skirmish_AI` display.

If the ships also ignore AI behavior after the click, then there is a separate
runtime behavior bug despite the toggle log. Verify this in battle before only
changing labels.

## Relevant Code

File:

```text
UADVanillaPlus/Harmony/BattleDivisionAiControlPatch.cs
```

The current context resolver is too broad:

```csharp
if (sourceName != "baseOrders" || !root.activeInHierarchy)
    continue;
```

The placement helper then scans all direct button siblings under the resolved
parent and moves the clone after the rightmost one. On `baseOrders`, that parent
contains more than the division mode selector, so the button lands at the far
right of the whole command strip.

The label suppression is here:

```csharp
internal static void RefreshDivisionCardLabel(UIDivision divisionUi)
{
    ...
    if (division == null ||
        divisionUi?.Name == null ||
        !IsDivisionAiControlledByVp(division) ||
        !IsFriendlyDivision(division))
    {
        return;
    }

    string? labelKey = FriendlyDivisionCommandLabelIgnoringVpAi(division);
    divisionUi.Name.text = string.IsNullOrEmpty(labelKey)
        ? string.Empty
        : LocalizeManager.Localize(labelKey);
}
```

That postfix was added to prevent unwanted friendly division-card `AI` labels
after the global `Ship.isAiControlled` getter patch. It may now conflict with
the desired user-visible confirmation that a division has been handed to AI.

## Suggested Builder Fix Direction

1. Read `plans/ui-layout-guide.md` before editing. This is a UI placement
   change.
2. Keep the global duplicate cleanup across candidate roots.
3. Do not use all of `baseOrders` as the final placement parent. Resolve the
   nearest shared parent of the actual division mode buttons (`Sail`, `Screen`,
   `Scout`, `Follow`, `Retreat`) and set the AI clone sibling index relative to
   `Retreat` or the rightmost button in that specific cluster.
4. Prefer cloning one of the real division mode buttons from that cluster, not a
   generic command-strip button.
5. Revisit `RefreshDivisionCardLabel`. Decide whether VP AI control should:
   - show vanilla `$Ui_Skirmish_AI` for friendly VP-controlled divisions;
   - show a VP-specific visible state somewhere else; or
   - keep hiding the label but change the button visual strongly enough that the
     player can tell AI control is active.
6. Verify both UI and behavior:
   - select a friendly division;
   - confirm `AI` appears in the division mode selector only;
   - click `AI`;
   - confirm `Latest.log` reports `enabled AI control ... via button`;
   - confirm the player-visible state changes as intended;
   - confirm the division actually behaves as AI-controlled;
   - right-click a manual order and confirm VP clears AI control.

## Build/Publish Reminders

- Builder session only should modify source, bump versions, build, copy DLLs,
  commit, push, or publish.
- Before building, bump `ModInfo.cs`, `Properties/AssemblyInfo.cs`, and README
  current version together per `AGENTS.md`.
- Use the workspace-local build command from `AGENTS.md`, including
  `/p:RestoreConfigFile=E:\Codex\UADVanillaPlus\NuGet.Config`.
- After a successful build, copy directly to the game `Mods` folder and report
  a lock if the game has the DLL open.
