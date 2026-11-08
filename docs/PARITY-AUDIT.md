# Feature parity audit

2026-11-08. Original macOS app was **not executed**. “Static” below means bundle metadata, resources, strings or SQL support the finding; it does not establish observed runtime behavior. No source app license was bypassed. Comet is not feature-complete or production-verified.

Status meanings: COMPLETE = implemented and portable behavior tested; PARTIAL = implemented with material scope limits; NOT IMPLEMENTED = absent; NOT VERIFIED = code exists but native runtime evidence is unavailable. Test status uses the same four labels. No Windows interaction is marked COMPLETE.

| Feature | Rocket macOS evidence | Comet Windows behavior | Implementation status | Test status | Known limitations |
|---|---|---|---|---|---|
| Global autocomplete | Static event-tap and typeahead components; `:roc` demo | Global hook, bounded parser, async search, suggestions | PARTIAL | PARTIAL | Core parser/search passed; actual hook and app coverage unverified |
| Trigger customization | Static trigger and double-trigger controls | ASCII punctuation, single/double trigger, min query | COMPLETE | COMPLETE | ASCII shortcodes only; UI itself unverified |
| Native floating popup | Static typeahead NIBs | Noactivate topmost compact rows, names, selection | NOT VERIFIED | NOT VERIFIED | No Windows screenshots/runtime observation |
| Enter/Tab/arrows/Escape | Static demo documents Return/Tab | Cached selection, wrapping navigation, optional Tab, dismissal | PARTIAL | PARTIAL | Portable navigation tested; hook suppression unverified |
| Mouse selection | Typeahead row resources | Click to confirm without activation | NOT VERIFIED | NOT VERIFIED | Native focus behavior unverified |
| Caret positioning | Runtime not observed | Win32 caret anchor, mouse fallback, work-area clamp | NOT VERIFIED | NOT VERIFIED | UIA caret-only controls fall back to mouse |
| Multi-monitor / DPI | Runtime not observed | Per-monitor manifest and monitor work-area placement | NOT VERIFIED | NOT VERIFIED | Mixed scaling needs native tests |
| Exact shortcode replacement | Static pasteboard/CGEvent symbols | Verify preceding range, then batched deletion/Unicode input | PARTIAL | PARTIAL | Planning tested; provider support and narrow cross-process race remain |
| Unicode / ZWJ / selectors | Static Unicode variant resources | Full UTF-16 sequences preserved | PARTIAL | PARTIAL | Data and planning tested; app rendering/injection unverified |
| Emoji metadata | Static JSON names, aliases, keywords, categories | Independent MIT dataset, 3,949 entries with variants | COMPLETE | COMPLETE | Different metadata/source coverage than Rocket |
| Search | Static LIKE and usage ordering SQL | Prefix, substring, keywords, limited fuzzy edits/subsequences | COMPLETE | COMPLETE | Fuzzy candidates bounded to top 500 bigram hits; not arbitrary typo guarantee |
| Skin tones | Static preference and variant queries | Picker preference filters variants, gender variants searchable | PARTIAL | PARTIAL | Core single-tone behavior tested; mixed-tone combinations not independently selectable |
| Recents / frequency | Static usage-count/statistics evidence | Recent list, frequent category and ranking | COMPLETE | COMPLETE | Persistence/core tested; native picker unverified |
| Favorites | Original behavior not established | Toggle, browse, remove in Settings | PARTIAL | PARTIAL | Core filtering tested; UI unverified |
| Full picker | Static browser/category/content controllers | Searchable grid, categories, tone, recents, favorites, keyboard | NOT VERIFIED | NOT VERIFIED | Results capped at 400; refine query for more |
| Custom emoji shortcuts | Static add/edit UI and user_defined schema | Add/edit/delete/filter with duplicate validation | PARTIAL | PARTIAL | Core validated; editor interactions unverified |
| Multiline snippets | Static AddSnippet UI, snippets category | Add/edit/delete/search; Unicode and multiline expansion | PARTIAL | PARTIAL | Core passed; line breaks emit Enter, possibly sending chat messages |
| Date/time snippet variables | Static recognized token strings | Original culture-based token expander | PARTIAL | PARTIAL | Core tests passed; format is an approximation, not macOS-identical |
| GIF import/search | Static GIF files and custom-file chooser | Authorized local file library, name search | PARTIAL | NOT VERIFIED | No remote provider, proprietary bundled pack or GIF shortcuts |
| GIF animation / sharing | Static GIF/image/file pasteboard types | Animated local preview and file-drop clipboard | NOT VERIFIED | NOT VERIFIED | Target must accept file attachments; clipboard persistence unverified |
| Tray lifecycle | LSUIElement and menu resources | Tray picker/settings/library/pause/quit, one instance | NOT VERIFIED | NOT VERIFIED | Clean shutdown, session and Explorer restart behavior unverified |
| Launch at login | Static startup preference/ServiceManagement | Opt-in per-user Run key | NOT VERIFIED | NOT VERIFIED | Move/install before enabling; not tested on Windows |
| Global picker hotkey | Static MASShortcut/browse key | Three configurable combinations with conflict detection | NOT VERIFIED | NOT VERIFIED | No arbitrary key chord recorder |
| Pause / exclusions | Static suppression fields/browser purpose text | Tray pause and exact executable-name exclusions | PARTIAL | PARTIAL | Core exclusions tested; process detection unverified |
| Website exclusions | Safari/Chrome purpose text | No per-website detection | NOT IMPLEMENTED | NOT IMPLEMENTED | Exclude entire browser instead |
| Password/privacy guard | Accessibility-related static evidence | Cached non-password editable metadata; fail closed; no text log | NOT VERIFIED | NOT VERIFIED | Custom providers can misreport; secure desktop/UIPI boundaries respected |
| IME/layout handling | Runtime not observed | Layout translation, cancel modifiers/dead keys, disable open IME | PARTIAL | NOT VERIFIED | No TSF composition integration; AltGr and complex layouts unsupported |
| Clipboard policy | Static leave-on-clipboard preference | Default Unicode input avoids clipboard; explicit/optional copy | NOT VERIFIED | NOT VERIFIED | No clipboard insertion adapter or rollback support |
| Settings/import/export | Static preferences/SQLite storage | Validated local JSON, atomic file replacement, import/export | PARTIAL | PARTIAL | Persistence core tested; UI/startup side effects unverified |
| Light/dark appearance | Rendered original UI unobserved | System/Light/Dark WPF styling | NOT VERIFIED | NOT VERIFIED | Existing regular windows keep their theme until reopened |
| Localization | Static English/French resources | English UI and metadata | PARTIAL | NOT VERIFIED | No French UI or localized metadata |
| Statistics dashboard | Static Stats controller/model/labels | Frequent ranking only | PARTIAL | PARTIAL | No dedicated dashboard or category usage chart |
| ASCII/glyph catalog | Static categories include symbols/ASCII | Unicode emoji metadata only | NOT IMPLEMENTED | NOT IMPLEMENTED | Custom snippets can add individual glyphs |
| Automatic updates | Sparkle metadata | No updater | NOT IMPLEMENTED | NOT IMPLEMENTED | Replace portable/install directory manually after quitting |
| Code signing / MSI | Original signing files present | Unsigned portable self-contained ZIP, optional PowerShell installer | PARTIAL | PARTIAL | EXE/PE/publish inspected; no signed MSI/installer execution |
| Cross-app validation | Original runtime unobserved | Checklist and synthetic Windows harness supplied | NOT VERIFIED | NOT VERIFIED | Notepad/browser/Word/VS Code/Discord not tested here |
