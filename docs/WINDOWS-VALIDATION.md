# Comet 0.2 Windows validation — not executed here

All items below are pending. Do not infer a pass from compilation or portable tests.

## Opt-in synthetic integration harness

Requirements: native Windows 10/11 x64 desktop, default Comet settings (trigger `:`, minimum 2, autocomplete enabled), US-English keyboard layout, and no development tools when using the bundled runner. Building the test harness from source requires the .NET 8 SDK.

1. Quit any running Comet instance. Start the extracted published binary with `.\Comet.exe --test-input`. This opt-in mode allows externally injected test keystrokes into the hook. Its own tagged insertion events remain ignored.
2. Close Settings, then run the bundled `Comet.WindowsSmoke.exe`. Alternatively, use `Run-Windows-Tests.cmd` to start explicit test mode and the runner. From source, `dotnet run --project tests/Comet.WindowsSmoke -c Release -r win-x64` is also available.
3. Click **Run smoke tests**, then leave keyboard and mouse untouched. The harness tests a separate-process WPF text box: 100 consecutive rocket/heart/fire replacements with no reset/restart between cycles, surrounding text, Escape, Backspace, clicking outside, password exclusion and cursor movement. Allow about two minutes; do not type or move the mouse. It writes `%TEMP%\Comet-Windows-smoke-results.txt`.
4. Record actual outcomes. Quit test-mode Comet afterward and relaunch normally. The harness tests synthetic virtual keys and a WPF provider, not physical input timing or other applications. It must not be used as evidence of broad compatibility.

## Required manual matrix

Record Windows build, Comet build hash, application version, accessibility settings, keyboard layout, monitor scaling, and actual result for each case. Use ordinary unelevated apps first.

| Target | Native launch | Suggestions on :rocket | Enter/Tab replacement | Surrounding text intact | ZWJ/tone | Password/unsupported field safe | Status |
|---|---|---|---|---|---|---|---|
| Notepad | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |
| Edge / Chrome / Firefox ordinary textarea | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |
| Microsoft Word document | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |
| VS Code editor | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |
| Discord draft field | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |
| Other messaging app draft field | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |
| Classic Win32 Edit / WPF TextBox | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |
| Elevated Notepad (expect rejection or disable) | Pending | Pending | Pending | Pending | Pending | Pending | NOT VERIFIED |

## Functional acceptance checklist

- Launch from extracted ZIP without development tools/.NET installed; confirm tray, first-run Settings, subsequent background start, single instance, hotkey and Quit. Check hidden-icons tray area.
- Type `:rocket`, choose another result with arrows, confirm with Enter and Tab; click a result without focus loss; dismiss with Escape; verify held-key repeats and keyup suppression.
- Type normal punctuation, URLs, email addresses, colon mid-word, double trigger and a custom punctuation trigger. Confirm unrelated typing and Ctrl/Alt/Windows shortcuts remain unaffected.
- Verify Backspace, Delete, Home/End/arrows, mouse click, scroll, text selection, focus switch, tab switch, programmatic caret movement, rapid typing and repeated candidates. Do not permit deletion of surrounding text or nonempty selection.
- Verify modifier release during closing-trigger confirmation; test dead keys, Caps Lock, non-US layouts, AltGr and IME composition. Unsupported composition should cancel safely.
- Test Unicode skin-tone, gender, family ZWJ, flag and variation-selector output. Compare exact code points, not only screenshots.
- Confirm candidate verification fails closed where TextPattern is unavailable; clipboard Copy remains explicit. Confirm UIPI/elevated/protected/password fields are not bypassed.
- Add/edit/delete/filter custom emoji and a multiline snippet; reject duplicate aliases. Test recognized date/time variables and literal unknown variables. Use a multiline editor before trying any chat draft.
- Persist settings/favorites/recent usage across restart. Import valid settings for review, reject corrupt/oversized/unknown-schema files and verify startup/hotkey side effects only after Save.
- Toggle pause and process exclusions; verify status labels. Try each global shortcut, including collision handling. Save startup after installation, sign out/in, and verify one background instance.
- Move across monitors with 100%, 125%, 150%, 200% scaling; check all screen edges, caret fallback, light/dark themes, keyboard selection visibility, contrast and long labels.
- Add small/large/animated GIFs, preview, search, remove, then paste the file clipboard into supported apps. Test missing GIF paths and app exit while clipboard holds an attachment.
- Test install/uninstall scripts, preserving personal data. Confirm no unintended files/registry entries are changed. Confirm portable mode works without scripts.
- Restart Explorer, lock/unlock, sleep/resume, switch keyboard layouts, disconnect monitors, log off/shut down, and run a long idle/typing soak. Measure hook callback latency, CPU, working set, handle count and growth.

Keep failures and unsupported applications in this document and the parity audit; do not mark a case COMPLETE until actually observed.


## Specific 0.2 manual checks

1. In Notepad, insert `:rocket`, immediately `:heart`, then `:fire`; continue for at least 100 cycles without restarting. Check every resulting Unicode sequence. Test Enter, Tab, clicked selection, held Enter/autorepeat and shifted closing colon. Record actual results, not assumptions.
2. Erase every character of `:rocket` with Backspace, immediately type `:heart`, and confirm it works. Repeat Escape and outside-click cancellation followed by a fresh trigger. Exercise rapid type → Escape → type before the old fade completes; the next popup must regain full opacity and mouse hit testing.
3. When the popup is open, confirm the original field retains the caret. Continue typing and verify the header/results update without flicker. Use arrow keys to select **Search for more…**, open the picker and select an emoji: the original candidate must be replaced, not left before the output.
4. In each editor/address/search field, move typing between lines, blank/end-of-document locations and screen edges. Confirm anchoring to the real caret, flip above near the bottom, clamp at both horizontal edges, and correct behavior on negative-coordinate/mixed-DPI monitors. Unavailable geometry should produce no misplaced inline popup; the explicit picker remains available.
5. Run `.\Comet.exe --popup-preview` to export the actual WPF design as `Comet-popup-preview.png` beside the EXE. Compare it and the live popup to the supplied Rocket screenshot: width, query header, row/emoji/font sizes, highlight, inset, corner radius, border, pointer, shadow and light/dark contrast. No native comparison was performed in Linux.
6. If repeated autocomplete fails, open tray **Input diagnostics** before restarting and record counters/field reason. No typed text is logged by that view. A stopped physical-key count versus an advancing count helps distinguish hook loss from eligibility/insertion problems. Preserve the Windows runner log and actual application versions.
7. Test the backup-first Desktop helper and verify the old folder is preserved under its timestamped backup name. Confirm `Desktop\Comet-Windows\Comet.exe`, ZIP, README.txt, TEST_RESULTS.md and KNOWN_LIMITATIONS.md exist; launch that copy, update startup path if enabled, and verify clean Quit/restart/settings persistence.
