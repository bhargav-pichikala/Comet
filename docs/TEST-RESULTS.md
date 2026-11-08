# Actual build and test results

Date: 2026-11-08. Environment: Debian GNU/Linux 13 x86-64. SDK: .NET 8.0.427. Windows runtime dependencies in publish: .NET and WindowsDesktop 8.0.33.

## Executed successfully

| Check | Actual outcome |
|---|---|
| DMG extraction | 7-Zip 25.01 extracted the UDIF/HFS+ bundle successfully |
| Static identification | Plist, JSON, localization, SQL, strings, universal Mach-O headers/load commands read |
| Portable automated suite | **53 passed, 0 failed** |
| Windows x64 self-contained publish | Succeeded; Windows PE x86-64 Comet.exe generated |
| Optional Windows smoke harness cross-build | Succeeded, 0 warnings / 0 errors; not executed |
| Native interop structure layout | INPUT=40, KEYBDINPUT=24, GUITHREADINFO=72 bytes on x64; portable assertions passed |
| Runtime inclusion | Runtime config declares included .NETCore.App and WindowsDesktop.App 8.0.33; hostfxr.dll, coreclr.dll, PresentationFramework.dll and Windows Forms assembly present |

The app publish emits **WFAC010**, advising use of Windows Forms DPI APIs instead of manifest DPI declarations. Comet's UI is WPF; the per-monitor manifest is intentional and Windows Forms is used for the tray. This is a build warning, not a native DPI validation pass.

The final portable run indexed 3,949 emoji entries in 187 ms. Across ten uncached representative queries, median search time was 1.02 ms and maximum 10.03 ms. The Linux console process working set was 97 MB. These numbers include test-process/JIT/index effects and are not Windows idle-memory/CPU/hook-latency measurements. Search figures are observations, not enforced performance guarantees.

The suite covers word boundaries, single/double/custom triggers, Backspace, cancellation revisions, rapid parser updates and candidate bounds; exact/partial/case-insensitive/alias/fuzzy search; tone/gender/ZWJ/selectors; custom alias priority and duplicates; multiline snippets and date/time variables; surrounding-text replacement planning and randomized context; settings validation/roundtrip/corruption/version checks; keyboard navigation; exclusions; usage/favorites/recents/categories/cache invalidation; and Win32 structure layouts. Portable tests do not call Windows APIs or prove native hooks/insertion.

## Attempted but unavailable

Executing the published Windows EXE on Linux returned:

```text
cannot execute binary file: Exec format error
```

No Windows runtime or Wine is available. Therefore actual native launch, tray, global hook input, popup location, focus behavior, Unicode insertion, clipboard attachments, startup, settings UI, exit, Windows password-field behavior, real keyboard layouts and application compatibility are **NOT VERIFIED**. The synthetic Windows harness is compiled only and has no pass count here. The checklist in WINDOWS-VALIDATION.md is pending in its entirety.

## Included evidence

`logs/core-tests.log`, `logs/windows-publish.log`, `logs/windows-smoke-build.log`, and `logs/windows-launch-attempt.log` preserve command outputs. PE/dependency/hash/archive verification is recorded in `PACKAGE-VERIFICATION.txt` alongside the output archives.

## Delivery-path limitation

The specified chat artifact folder under `/codex/browser/projectless/.../output` could not be created: the filesystem denied creation of `/codex`, and `sudo` is not installed. The completed archives and readable reports are saved in `/workspace/output`. No successful export to the designated chat download folder is claimed.
