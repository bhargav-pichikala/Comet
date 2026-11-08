# Comet 0.2: repeated-use investigation

## Evidence boundary

The user's Windows report is that one replacement works and subsequent autocomplete stops. This execution environment is Linux, without Windows or Wine, so that **native failure has not been reproduced here and its exact native root cause is not established**. No Notepad, Chrome, Word, VS Code, Discord, Windows Search or ChatGPT input test is claimed passed. The implementation addresses identifiable lifecycle defects and the hook-thread design risk, and supplies tests/diagnostics for native verification.

## Findings and changes

1. **Hook lifetime depends on the WPF dispatcher in 0.1 — confirmed architecture, suspected cause of the report.** Both low-level hooks were registered on the main WPF thread. Rendering and JSON persistence run there. Windows can silently remove low-level hooks when their thread does not service callbacks within LowLevelHooksTimeout. The source had no separate input pump, so a stall could leave a running tray app with no more callbacks. Whether that timeout occurred on the user's PC is NOT VERIFIED. Version 0.2 registers and disposes the hooks on a dedicated STA thread with its own Dispatcher message loop. Searches, visuals, provider calls and file writes do not block that thread. Mouse hit testing reads an immutable screen rectangle, rather than calling WPF from the hook. No blind periodic re-hook workaround was added.
2. **Full Backspace deletion does not re-arm the trigger — reproduced portable defect.** In 0.1, typing `:rocket`, backspacing all seven characters, then immediately typing `:heart` leaves the parser's boundary flag false, so the second trigger is ignored. Removing the last character of an active candidate now restores the known boundary before that candidate. Backspacing unknown ordinary text still conservatively clears boundary knowledge. A regression tests this exact sequence.
3. **Exact `heart` alias can lose to another emoji's `heart` keyword — reproduced portable defect.** A 100-cycle rocket/heart/fire regression initially failed on cycle 2 because generic exact-keyword matches had the same score as the explicit alias. Exact built-in aliases now outrank generic keyword matches; exact custom shortcuts still take priority. Cache results are also isolated to their settings instance. All matching regressions pass after the change.
4. **Session completion and late UI callbacks need explicit ownership.** A shared input-thread session model now assigns revisions and insertion tokens. Search results, clicks and visibility acknowledgements for an earlier query are discarded. Completion uses a `finally` path and may release only its own pending operation; it never clears a newer candidate. Confirm-key suppression persists across session cancellation until that physical key's matching key-up. Comet-tagged synthetic input is ignored in normal and test modes.
5. **Duplicate accessibility focus notifications can unnecessarily cancel a new candidate.** Notifications identifying the same non-password, still-focused runtime ID/window/focus handle no longer invalidate an otherwise valid decision. Actual changed or unknown fields still invalidate it; password and stale-provider protections remain in place. Native provider behavior is not verified here.
6. **Rapid fade-out/reopen can leave a window transparent — found and corrected during the redesign.** A new show during a pending hide explicitly cancels the fade, restores opacity/hit testing and invalidates the old hide epoch. This was an intermediate 0.2 styling defect, not claimed as the cause of the user's 0.1 report. A 100-reopen state regression covers the animation lifecycle.

## Diagnostics

Tray → **Input diagnostics** shows whether the hook message-pump thread is alive, physical keydown count, callback failures, requested/completed insertions, and the accessibility decision/age. It contains no typed characters, shortcode strings, document contents or clipboard history. A live thread alone does not prove Windows has not removed a hook.

If the Windows failure recurs, record those counters before the first replacement and after the failure, plus Windows/application version, whether it followed Enter/Tab/mouse selection, and whether the physical-key counter still increases. This separates absent callbacks, privacy/provider denial and insertion completion problems without collecting typed text.

7. **Read-only metadata fallback could override an explicit read-only value provider.** The old guard OR-ed in text-pattern support even when ValuePattern reported read-only. Eligibility now honors explicit read-only metadata and rejects unknown text-only mutability. Three portable regressions cover these provider decisions; password/exclusion checks remain independent and required.

## Baseline proof against untouched 0.1 source

`logs/baseline-regressions-0.1.log` was produced by compiling the original core source from commit `ccae683`, outside the updated project. It reproduced an empty query after full Backspace followed by `:heart`, and ranked 🫀 (`anatomical_heart`) first for `heart`. The ordinary original parser Reset/rearm loop also passed 100/100 cycles. That result is evidence **against claiming a deterministic one-use bug in the ordinary pure parser**, and is why the native hook cause remains explicitly unverified rather than being invented.

## Executed regressions

The portable suite includes 100 consecutive rocket/heart/fire replacements into accumulated Unicode text without restarting or resetting the field; 100 cancellation/failure-finally cycles; 100 rapid fade/reopen transitions; stale completion/search/click acknowledgement rejection; own-injection filtering and confirmation key-up/hold recovery; Backspace re-arming; and exact-alias ranking. These exercise shared production state/search/planning code, not the Windows hook APIs or a real application.

The shipped Windows runner adds 100 actual synthetic-key cycles through the running Comet process and a separate WPF text box. It has been built, not executed here. Actual physical input, Notepad and other editors, foreground focus, provider-specific verification and mixed-DPI caret positioning remain pending in WINDOWS-VALIDATION.md.
