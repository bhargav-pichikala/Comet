# Verified milestone — 2026-11-08

- Supplied DMG extracted with 7-Zip; version, architectures, linked dependencies, datasets, SQL, UI strings and embedded feature evidence inspected. macOS behavior not executed.
- Original native .NET 8 WPF implementation created under the independent name Comet; no Rocket code/data/artwork bundled.
- Main global autocomplete pipeline implemented: privacy guard, keyboard parser, indexed search, nonactivating popup, keyboard/mouse navigation, exact candidate-range verification, batched Unicode replacement and cancellation checks.
- Tray host, picker, custom shortcuts/snippets/date-time variables, favorites/recents/frequency, settings/import/export, exclusions, startup/hotkey controls and local animated GIF library implemented.
- 53 portable automated tests passed on Linux, including Unicode, settings, parser, search, replacement plans and native structure layouts.
- Self-contained Windows x64 publish succeeded. PE x64 apphost inspected. Linux execution rejected the Windows executable with an Exec format error, as expected. No native Windows test pass claimed.
- Optional Windows smoke harness supplied; build outcome in docs/TEST-RESULTS.md. It requires a Windows desktop and opt-in test-input mode.

## Resume from here

1. Run native Windows harness and full application checklist in docs/WINDOWS-VALIDATION.md.
2. Fix real native failures before any production claim, especially provider compatibility, hook suppression, focus safety, mixed-DPI placement and tray/session recovery.
3. Profile resource use and perform an extended idle/typing soak.
4. Update the parity audit using observed outcomes. Remaining intentional gaps include online GIF providers/shortcodes, website-level exclusions, full glyph catalog, French UI, updater, full statistics dashboard and code signing.

The build environment has no Windows runtime/Wine and cannot execute the application. The designated chat output directory /codex/browser/projectless/0001-files-mentioned-by-the-user-uploaded-file-pointe/output is unavailable: /codex does not exist, its creation is denied by the root filesystem, and sudo is absent. Archives are preserved in /workspace/output instead; this is a delivery-path limitation, not a successful download-folder export.
