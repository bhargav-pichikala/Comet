# Comet 0.2 changes

- Move low-level keyboard/mouse hooks to an independent input message pump, isolating them from WPF rendering and settings I/O.
- Make search/selection/visibility/insertion ownership explicit; ignore stale work and always release matching completion state.
- Restore trigger recognition after fully backspacing a candidate; fix exact `heart` alias ranking and settings-specific search-cache isolation.
- Handle confirmation key-up/autorepeat across cancellation; keep Comet's tagged synthetic input out of the parser.
- Honor explicit read-only field metadata instead of overriding it with a text-pattern fallback; unknown mutability fails closed.
- Keep duplicate same-field focus notifications from unnecessarily invalidating the current eligibility check. Changed/password/unknown fields still fail closed.
- Replace pointer fallback with actual Win32, UIA TextPattern and MSAA caret geometry. Refresh while active; clamp/flip within the target monitor; skip inline display when the caret is unavailable.
- Restyle the popup around the supplied Rocket screenshot: compact query header, single-line emoji rows, soft highlight, thin separator/border, rounded corners, pointer and shadow; light/dark palettes and short fades.
- Add functional Search for more… with keyboard navigation. Picker selection verifies and replaces the original shortcode instead of appending to it.
- Protect rapid reopen from an old fade completion; trim previews by Unicode text elements.
- Add privacy-preserving tray diagnostics, an explicit native WPF PNG preview export, a 100-cycle Windows smoke runner requiring no SDK, and a backup-first Desktop copy helper.

The old Windows/source ZIPs remain unchanged in downloads/. Versioned 0.2 downloads are separate. The reported 0.1 native one-use failure's exact root cause remains unverified without Windows execution; see BUG-INVESTIGATION-0.2.md.
