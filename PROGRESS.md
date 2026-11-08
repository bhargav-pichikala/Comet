# Current milestone — Comet 0.2

Existing 0.1 source and binary ZIPs were recovered and preserved. The source was updated in place for independent input-thread hooks, revision/token-owned autocomplete sessions, full-Backspace recovery, exact-alias ranking, caret-only placement and the reference-style popup. See docs/BUG-INVESTIGATION-0.2.md for verified defects versus the unverified native report.

Portable test outcome and current build outputs are in docs/TEST-RESULTS.md and docs/logs. The native smoke runner now includes 100 consecutive input/insertion cycles and is supplied in the Windows binary package without requiring an SDK. Native Windows execution, live visual comparison and cross-application validation remain pending.

Resume with docs/WINDOWS-VALIDATION.md on a native Windows desktop. Record real results, use tray Input diagnostics if repeated autocomplete fails, fix native failures and update the audit before any production-complete claim. This session has no Windows drive/USERPROFILE Desktop access; a backup-first explicit Desktop copy script is included instead of claiming a local Desktop deployment.
