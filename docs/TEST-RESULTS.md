# Comet 0.2 — actual test results

2026-11-08 · Debian Linux x86-64 · .NET SDK 8.0.427 · Windows runtime dependencies 8.0.33.

## Executed

- **84 portable tests passed, 0 failed**, including the original 53 tests.
- **100 consecutive rocket/heart/fire cycles** passed in shared production session/search/replacement-planning logic, without restarting or resetting the simulated field. Own-tagged synthetic-input filtering and physical confirm key-up are exercised in each cycle.
- **100 cancellation/failure-completion cycles** and **100 rapid popup fade/reopen state cycles** passed.
- Regression coverage also includes full-Backspace rearming, stale search/visibility/completion rejection, newer-query preservation, invisible-selection protection, Search for more navigation/context, exact heart/custom alias priority, settings-cache isolation and physical-pixel placement at bottom/right edges, negative-coordinate monitors and 100/125/150/200% scales.
- Self-contained Windows x64 app publish succeeded. PE x64 apphost and included runtime files were inspected.
- Self-contained Windows smoke runner publish succeeded. It includes 100 actual synthetic-input/insertion cycles when run on Windows; **it was not executed here**.
- Whitespace/diff checks passed. Archives are checked for integrity and per-file hashes, and original 0.1 ZIP hashes are checked unchanged.

The final portable run observed median uncached search 0.90 ms and maximum 3.65 ms over ten representative queries, with a 154 MB Linux test-runner working set. These are not Windows idle resource or hook-latency measurements.

The new repeated-cycle regression initially failed at its second cycle: `heart` could rank a different heart emoji before the explicit ❤️ alias. The ranking defect was fixed and the full suite rerun. Fully erasing a candidate also exposed a reproducible boundary rearming defect; its exact sequence is covered. See BUG-INVESTIGATION-0.2.md for distinctions from the unverified native one-use report.

The publish has one intentional WFAC010 analyzer warning about manifest DPI settings: WPF owns the UI; Windows Forms supplies only the tray. Keeping the per-monitor manifest is not proof that native DPI behavior passed.

## Not executed / not verified

No native Windows launch, tray, hook delivery/suppression, physical input, caret/provider/DPI placement, focus continuity, real-app replacement, startup, UI persistence/restart, Desktop copy, native screenshot export or clean-exit pass is claimed. No 100-cycle native test pass is claimed. No source macOS runtime was executed. A headless Linux Chromium design-sheet render was attempted but produced no screenshot; the design HTML is labeled accordingly. Windows executable launch on Linux is unsupported and reports Exec format error.

The shared code regressions demonstrate state/planning behavior. They do not establish the precise cause of the user's native failure or replace the pending native checklist in WINDOWS-VALIDATION.md.

## Evidence

- `logs/core-tests-0.2.log`
- `logs/baseline-regressions-0.1.log`
- `logs/windows-publish-0.2.log`
- `logs/windows-smoke-publish-0.2.log`
- `logs/windows-launch-0.2.log`
- `../downloads/Comet-0.2-SHA256SUMS.txt` and packaged `SHA256SUMS.txt`

The Windows binary package includes the test runner and `Run-Windows-Tests.cmd`; no SDK is needed to perform those synthetic tests locally. Its log is `%TEMP%\Comet-Windows-smoke-results.txt`.
