# Comet for Windows · 0.2

Native .NET 8 WPF emoji autocomplete, updated with repeated-session lifecycle fixes, caret-based positioning and a compact Rocket-style popup based on the supplied screenshot. This is original code and artwork using independently MIT-licensed emoji metadata.

**Windows x64 binaries are cross-built on Linux. Native Windows behavior and the user's original one-use failure remain unverified here. Do not treat this as a production-validated or feature-complete release.** See [the investigation](docs/BUG-INVESTIGATION-0.2.md), [changes](docs/CHANGELOG-0.2.md), [actual test results](docs/TEST-RESULTS.md) and [validation checklist](docs/WINDOWS-VALIDATION.md).

## Download and run

Use `downloads/Comet-0.2-win-x64.zip` for this version. The original `Comet-win-x64.zip` remains the preserved 0.1 build. Extract the entire ZIP into a new folder, quit any running old Comet from its tray, then run `Comet.exe` inside the extracted Comet folder. Keep all runtime files together. No Visual Studio, SDK or separate .NET runtime is needed to run either Comet or its bundled Windows smoke runner.

Type `:rocket`, choose with Up/Down, then Enter or Tab. Escape cancels. The default minimum is two letters. A fully backspaced candidate can immediately be retried. After insertion, the next shortcode can begin without a restart. That state behavior is regression-tested; actual cross-application hook/insertion behavior needs the Windows tests below.

The compact popup shows the current query, one-line emoji suggestions, and **Search for more…**. Select that footer to open the full picker with the query already entered. Picker insertion verifies/replaces the original candidate when opened from autocomplete. The popup stays nonactivating. It tries actual Win32 caret geometry, UI Automation collapsed selection geometry and the MSAA caret object. If none is reliable, inline suggestions stay hidden and Enter is not intercepted for an invisible selection; use **Ctrl+Shift+Space** to open the picker and Copy if needed.

Tray controls provide picker, settings, GIF library, pause/resume, input diagnostics and Quit. Closing a regular window leaves the tray app running. Settings retains custom aliases, multiline/date-time snippets, favorites, recent/frequent emoji, skin tone, exclusions, appearance, hotkey choices, import/export and opt-in startup. GIFs are an authorized local-file library with animation and attachment copying, not an online service.

## Copy to your Windows Desktop with backup

After extracting into a temporary/new folder and quitting Comet, you may explicitly run `Install-To-Desktop.ps1` from that binary folder. It finds the actual Windows Desktop (including a redirected Desktop), backs up an existing `Comet-Windows` folder to a timestamped sibling, and writes the full build, README.txt, TEST_RESULTS.md, KNOWN_LIMITATIONS.md and a ZIP under `Desktop\Comet-Windows`. It never installs an SDK or requires administrator access. This helper is supplied but has not been executed on Windows here.

Alternatively, use File Explorer to rename the old Desktop folder as a backup, create `Comet-Windows`, and extract/copy the entire new Comet folder's contents there. If startup is enabled, save Settings after launching the copy in its permanent location so the startup path is updated.

## Actual Windows tests and screenshot

Quit normal Comet and double-click `Run-Windows-Tests.cmd`. The explicit `--test-input` mode lets the separate bundled smoke runner send synthetic US-layout keys. Click its Run button, keep keyboard/mouse untouched, and wait for **100 consecutive rocket/heart/fire cycles**, plus cancellation/privacy cases. It writes `%TEMP%\Comet-Windows-smoke-results.txt`. Quit test-mode Comet and relaunch normally afterward. This test is compiled, not executed in the Linux build environment. Physical input, other editors, caret placement and focus still require the manual matrix in docs/WINDOWS-VALIDATION.md.

For a real WPF design capture on your Windows PC, run `.\Comet.exe --popup-preview`. It saves `Comet-popup-preview.png` beside the executable. This preview does not install hooks or test caret positioning. The HTML design sheet in docs/visual is labeled as a design reference, not a WPF screenshot. No native screenshot was fabricated.

## Privacy and input limits

Only an in-memory bounded ASCII shortcode candidate is retained. No general typing log, clipboard history, network telemetry or HTTP API exists in the app. Settings are local plaintext in `%LOCALAPPDATA%\Comet\settings.json` and may contain private user snippets. Export them accordingly.

Unknown/password/excluded fields, stale checks, changed focus and detected open IMEs disable processing. Before replacing, Comet verifies the exact bounded preceding candidate and empty selection through UIA TextPattern, then rechecks context before one Unicode SendInput batch. It does not read whole documents or full field values. Unsupported providers fail closed, and protected/elevated/remote fields are not bypassed. A narrow cross-process edit race remains; no universal compatibility claim is made. Multiline snippets send Enter keys and can send messages in chat apps; use them in multiline editors.

## Build and source layout

.NET SDK 8.0.427 was used. From this source root:

```powershell
dotnet run --project tests/Comet.Tests -c Release
dotnet publish src/Comet.Windows -c Release -r win-x64 --self-contained true -o artifacts/Comet-win-x64
dotnet publish tests/Comet.WindowsSmoke -c Release -r win-x64 --self-contained true -o artifacts/WindowsSmoke
```

The core suite exercises production session state, parsing, ranking, settings, replacement planning, navigation, injection filtering and physical-pixel placement geometry. It does not execute Windows APIs. See docs/logs for actual command outputs. The Windows publish is self-contained; do not distribute only the EXE or a Linux apphost.

`src/Comet.Core` contains portable logic and embedded metadata. `src/Comet.Windows` contains the input pump, privacy/caret workers, Win32 insertion, tray and WPF windows. `tests/Comet.WindowsSmoke` is the explicit native integration harness. `docs` includes findings, architecture, parity, investigation, visual comparison and validation. Runtime licenses and emoji metadata provenance are included. No Rocket binary, database, artwork, GIF pack, font or licensing code is redistributed.
