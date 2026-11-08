# Comet for Windows · 0.1

Independent native Windows emoji autocomplete utility, implemented in original C#/.NET 8 WPF code after static inspection of the supplied Rocket macOS disk image. **Built for Windows x64; Windows execution and application compatibility remain unverified. This is not a production-ready or feature-complete Rocket replacement.**

## Run

1. Extract the **entire** `Comet-win-x64.zip` into a permanent writable folder on Windows 10/11 x64. Keep the EXE and all dependencies together.
2. Run `Comet.exe`. Development tools and a separately installed .NET runtime are not required. The first launch opens Settings. Thereafter, Comet lives in the system tray (possibly under the hidden-icons arrow).
3. In an accessible standard text editor, type `:rocket`, wait for suggestions, then press Enter or Tab. Use Up/Down to choose and Escape to dismiss. The default minimum is two letters. A shortcode must begin at a boundary, such as after a space or opening parenthesis.
4. Press **Ctrl+Shift+Space** for the full picker, or open it from the tray. Enter/double-click inserts into the preceding supported field; Copy is available when insertion is unsupported. Right-click or use Favorite to save an item.
5. Open Settings from the tray to add custom emoji shortcuts and multiline snippets, change the trigger, exclusions, popup size, hotkey, appearance, or startup behavior. Click **Save** to apply settings. Expansions use names without the trigger, such as `signature`.

A closed Settings or picker window does not exit Comet. Choose **Quit Comet** from the tray.

If Windows displays a warning for this unsigned application, review the source/build provenance before deciding whether to run it. The binary is not code-signed.

## Optional installation

The ZIP is portable. The included `install.ps1` optionally copies it to `%LOCALAPPDATA%\Programs\Comet` and creates a Start menu shortcut. Run that script from the extracted binary folder using PowerShell. It requires no administrator access. Quit Comet before upgrading. Enable launch at sign-in in Settings **after** moving/installing the app, so the registered path points to its permanent location.

`uninstall.ps1` removes that per-user installation, its Start menu shortcut, and its startup entry, while retaining personal data. Both scripts are supplied but could not be executed in the Linux build environment. To remove personal data too, delete `%LOCALAPPDATA%\Comet` after quitting, if desired.

## GIFs

Open **GIF library**, add GIF files you own or may lawfully share, search their filenames, preview animation, and choose **Copy GIF file**. Paste into a destination that accepts file attachments. There is no online GIF provider, GIF shortcode expansion, or bundled proprietary reaction library. Sharing support depends on the destination. Removed library entries leave the managed GIF files intact so copied attachments and exported settings do not unexpectedly break.

## Input and privacy limits

Autocomplete only operates when Windows accessibility identifies a focused, enabled, non-password editable field. Unknown/inaccessible fields, active IMEs, excluded applications, and stale privacy checks disable processing. Before deletion, Comet verifies an empty selection and the exact preceding shortcode via `TextPattern`. If unavailable or timed out, it cancels replacement and reports the reason. The picker can insert without deletion into a supported field, or copy explicitly.

Only a bounded ASCII shortcode candidate is buffered while active; ordinary text is not retained. The accessibility verification reads only the bounded preceding candidate range, not a whole document or field value. No typed-text logs, network traffic, analytics, or telemetry are implemented. Settings store user-created snippets, usage IDs/counts, favorites, exclusions, and managed GIF paths locally. Treat exported settings as private.

Multiline snippets send Enter key events. Use them in multiline editors: in chat apps Enter can send a message. Surrogate pairs, variation selectors, and ZWJ sequences are preserved as UTF-16 Unicode input. Applications decide how they render and accept them.

See `docs/KNOWN-LIMITATIONS.md` and `docs/WINDOWS-VALIDATION.md` before depending on it.

## Build and tests

Install the .NET 8 SDK (8.0.427 used here), then run from the source root:

```powershell
dotnet run --project tests/Comet.Tests -c Release
dotnet publish src/Comet.Windows -c Release -r win-x64 --self-contained true -o artifacts/Comet-win-x64
```

Or run `tools/build.ps1` on Windows to test, publish, and ZIP. Linux can cross-build the WPF app with `EnableWindowsTargeting=true`, as set in the project. A normal `dotnet build` on Linux creates a Linux apphost that cannot run WPF: distribute only the **win-x64 publish** output.

For an interactive synthetic Windows input smoke test, build `tests/Comet.WindowsSmoke` and follow `docs/WINDOWS-VALIDATION.md`. Do not treat successful compilation as proof of native behavior.

## Contents

- `src/Comet.Core`: parser, indexed search, Unicode emoji metadata, models, replacement plans, settings.
- `src/Comet.Windows`: tray host, Win32 keyboard/mouse hooks, accessibility guard, exact-range verification, SendInput insertion, popup, picker, settings and GIF library.
- `tests/Comet.Tests`: portable automated tests.
- `tests/Comet.WindowsSmoke`: opt-in interactive Windows integration harness.
- `docs`: static DMG findings, architecture, parity audit, actual test results, limitations and validation checklist.

Metadata uses MIT-licensed `iamcal/emoji-data`; see license and provenance in `docs`. No Rocket binaries, databases, artwork, GIFs, or licensing components are redistributed. The icon is original geometric artwork.
