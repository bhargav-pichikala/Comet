# Static inspection of Rocket-88.dmg

Inspection date: 2026-11-08. Input SHA-256: `a6050156f5cf583fdd8d7f2714967d0d06f9547f91ef9d3ba094f076e0a58c34`. Size: 11,775,530 bytes.

## Method and evidence limits

7-Zip 25.01 decoded the UDIF compressed disk image and traversed its HFS+ filesystem without mounting or executing it. Extraction reported success. Python plistlib and JSON readers inspected metadata and datasets. Mach-O universal headers and load commands were parsed directly. `strings` exposed selectors, imported symbols, class names, preference keys, and snippet-variable identifiers. English and Base localization text was read with its declared UTF-8/UTF-16 encoding. SQL resource schemas and query semantics were examined as static evidence.

No macOS runtime was available. No original window was displayed. Exact geometry, latency, animation, focus behavior, clipboard restoration ordering, exclusion defaults, and crash/lifecycle behavior were not behaviorally observed. A resource or symbol proves its presence, not that the associated feature always executes or works. License/activation controls were neither bypassed nor exercised. No protected implementation or assets were copied to Comet.

## Bundle and architecture: verified static facts

- HFS+ volume `Rocket`, application `Rocket/Rocket.app/Contents`.
- `Info.plist`: Rocket **1.9.5**, build **88**; identifier `net.matthewpalmer.Rocket`; executable `MacOS/Rocket`; minimum macOS **13.5**.
- `LSUIElement=true` indicates agent/menu-bar style lifecycle; `NSMainNibFile=MainMenu`, `NSPrincipalClass=NSApplication`.
- Universal Mach-O: x86_64 slice 1,377,152 bytes; arm64 slice 882,720 bytes. Swift and Objective-C runtime references are present.
- Bundled frameworks: **FMDB**, **MASShortcut**, **Sparkle**. Load commands also reference AppKit, Foundation, Carbon, ServiceManagement, ApplicationServices, CoreGraphics, QuartzCore, CoreImage, IOKit, Security, SQLite, libc++, and Swift libraries.
- Sparkle metadata enables automatic checks and declares an appcast URL. No update request was made.
- Code-signature directories, app icon, compiled asset catalog, MainMenu and view NIBs, localized strings, JSON datasets, SQL migrations, and sample GIFs are present.
- English and French resources exist. Some internal names/category data use the `rocket.internal` language identifier.

## Functionality evidence

| Area | Verified evidence in bundle | What remains inference or unverified |
|---|---|---|
| Global typing | `KeyDownEventListener`, CGEvent tap creation/enabling, event type and post symbols; accessibility onboarding assets | Exact input event policy and keyboard-layout/IME behavior |
| Autocomplete | Typeahead controller, ordinary/wide row NIBs, phrase/items fields; demo says type `:roc`, choose rocket with Return/Tab | Exact minimum query length, cancellation rules and timing |
| Text insertion | CGEvent posting and pasteboard string/image/file-URL symbols | Exact deletion/insertion sequence and focus guarantees |
| Clipboard | General pasteboard selectors, generated-content type, leave-on-clipboard preference, copied notification | Prior clipboard preservation and application-specific formats |
| Popup | Typeahead view-size key, table rows, wide-text rows | Rendered appearance, caret anchoring and DPI behavior |
| Global shortcut | MASShortcut framework/binder and browse-and-search shortcut preference | Default key combination and collision behavior |
| Background lifecycle | LSUIElement, MainMenu NIB, startup preference, ServiceManagement linkage | Resident resource usage, launch agent behavior and exit ordering |
| Emoji | `Base.json`: 5,225 content rows, all marked emoji/bundled; localized English: 1,967 entries, French dataset; legacy `emoji.json`: 1,300 rows | Current Unicode version completeness; not all base entries have localized names |
| Metadata | Names, short_names, keywords, categories; Unicode versions and variant query resources | Which metadata is exposed in each UI mode |
| Search | SQL uses phrase LIKE matching, language or user-defined filtering, grouping, usage-count ordering, skin/gender modifiers | Actual query wildcard construction; no proof of typo-tolerant edit-distance matching |
| Variations | Preferred skin tone, gender/skin flags; query accounts for ZWJ, gender-neutral bases, directional variants | Runtime selection behavior and font support |
| Browsing | Browser coordinator, category/content lists, details, add-custom-emoji UI and copy strings | Precise navigation and insertion semantics |
| Custom shortcuts | Add/edit shortcut fields; migration 7 tracks user-defined aliases across languages | Duplicate resolution and validation rules |
| Snippets | AddSnippet controller, wide-text rows, snippet content type/category and variable identifiers | Complete parser grammar and runtime formatting |
| Snippet variables | date_short/medium/long/full, time_short/medium/long/full, date_time_iso, best_date, best_time, time_24h strings | Exact locale-dependent formatting of each token |
| GIFs/images | Eight reaction GIF files plus an accessibility-onboarding GIF, add-custom-GIF UI with file chooser/filename-shortcut hint, file/image pasteboard symbols, GIF content classes | Online search provider not established; exact sharing formats and app support unverified |
| Preferences | Trigger, double trigger, skin tone, login startup, language, clipboard retention, typeahead size, browse shortcut, VoiceOver-related selectors | Default values and interactions |
| App/site exceptions | Apple Events purpose text mentions Finder, Safari, Chrome and avoiding websites with existing emoji pickers; browser identifiers and suppression phrase fields | Exact allow/deny lists and website detection logic |
| Persistence | SQLite schema migrations 0–7: content, short_name, usage_count, names, keywords, languages, categories, Unicode versions, import/change logs, user_defined flag; preference key strings | Runtime database path, storage encryption, transaction and recovery policy |
| Statistics | Stats coordinator/model/view and strings for most-used categories, total and different emoji | Exact statistics calculations |
| Licensing | License activation/info NIBs and upgrade text label some GIF/search/customization/snippet features as Pro | Entitlements were not activated or tested |

## Adaptation decisions

Comet uses an original name/icon, WPF UI, local JSON settings, Win32 hooks, UI Automation, and Unicode SendInput. It uses independently fetched MIT emoji metadata (3,949 entries including tone variants), not Rocket's bundled data. It does not bundle Rocket GIFs, fonts, images, frameworks, NIBs, or executable code. GIF functionality is an authorized user-supplied local library, consistent with the verified custom-file UI; no third-party paid service is impersonated. English metadata is provided. Date/time snippet variables use documented .NET/culture-based approximations. Website-specific exceptions, macOS VoiceOver options, Sparkle updates, licensing/upgrade UI, and a copy of Rocket's statistics dashboard are not implemented.
