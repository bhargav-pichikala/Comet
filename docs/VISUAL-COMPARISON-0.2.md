# Rocket-style popup comparison

Reference: the user's attached screenshot showing `roc`, a selected `🚀 rocket` row and `+ Search for more…`. The supplied Rocket-88 DMG and extracted Typeahead NIB resources remain available as static evidence; no macOS window was executed. No proprietary code, fonts or artwork are copied.

Measurements below are visual estimates from the screenshot, not a native side-by-side pixel analysis.

| Element | Reference screenshot | Comet 0.2 adaptation |
|---|---|---|
| Shape | Compact rounded panel with small caret pointer | 260 DIP card, 10 DIP corners, 8 DIP pointer |
| Surface | Warm near-white, very light border | Light #F8F6F6, border #DAD6D6; matching dark palette |
| Header | Bold `roc`, no leading colon, separator below | Query-only 14 DIP semibold text, 48 DIP header, 18 DIP horizontal inset |
| Result | Emoji followed by one bold label on one line | 18 DIP Segoe UI Emoji, 26 DIP icon column, 14 DIP semibold alias |
| Row | Soft gray rounded selection, tight spacing | 36 DIP height, 6 DIP outer inset, 5 DIP corners, #ECEAEA selection |
| Footer | Plus sign and “Search for more…” | Functional 36 DIP footer; keyboard-selectable and clickable |
| Shadow | Subtle soft macOS-style shadow | 14 DIP blur, 2 DIP depth, 16% opacity, transparent margin |
| Extra content | No duplicate subtitle or keyboard-help paragraph | Removed secondary shortcode line and help paragraph from popup |
| Appearance | Small unobtrusive contextual panel | 85 ms fade-in, 65 ms fade-out; rapid reopen cancels old animation |
| Position | Pointer close to text caret | Win32 → UIA selection geometry → MSAA caret, work-area clamp/flip; no mouse fallback |
| Typography | macOS system-style sans serif | Windows Segoe UI; no proprietary macOS font is bundled |
| Total height | Depends on visible results and footer spacing | Content-sized, with scrolling when a small monitor cannot fit all results |

The first styling pass established the header/row/footer proportions and warm neutral palette. The second refined caret-pointer geometry, light/dark contrast, row overflow/scrolling, Unicode-safe previews and fade interruption. These are implementation reviews against the supplied reference, **not a claim of pixel-perfect or observed native visual parity**.

`visual/popup-design-preview.html` is a browser-readable design reference for light/dark geometry. It is not a screenshot of the WPF app and does not test caret placement. A headless Chromium render was attempted in the Linux environment; it did not produce a screenshot, so no native or browser screenshot is claimed.

For a real WPF rendering on Windows, run `.\Comet.exe --popup-preview` from the extracted binary folder. It saves `Comet-popup-preview.png` beside Comet.exe, rendering the same production popup with a `roc`/rocket example. This explicit preview does not install hooks, enter text or test provider positioning. Compare that image and the actual live popup with the original reference; actual Windows capture/export remains unverified here.
