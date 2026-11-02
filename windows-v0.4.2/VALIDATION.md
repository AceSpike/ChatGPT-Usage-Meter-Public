# Windows v0.4.2 validation — 2026-11-02 UTC

The release is built from the latest v0.4.1 skin source, with a separate v0.4.2 source tree. Historical v0.3 source/downloads and the existing plugin are retained.

Previously recorded validation on one development PC (not rerun during public-package preparation), Windows x64, .NET Framework compiler v4.0.30319:

- 117 discovery/error assertions: npm priority; desktop-only installation; executable write-time ordering instead of opaque directory ordering; incomplete directories; new, renamed and removed version directories; missing CLI; launch failure; mocked RPC rejection, invalid result and timeout; raw error redaction; visible failure reasons across all four skins in both languages; cache retention and recovery.
- 192 existing tray assertions passed, including 16/20/24/32 px, 0/100/unknown/failure, freshness, complete bilingual tooltips and HICON cleanup.
- 1,421 skin assertions passed, including all independent tray/window combinations, persistence/reopen, invalid and blocked storage, data states, both languages and 100/150/200% synthetic font/layout scaling.
- Existing bilingual, background, migration, contrast, bounds and hide/restore/exit regression checks passed.
- **Real desktop-only CLI read passed:** npm discovery was deliberately suppressed, the installed desktop-bundled CLI was selected, and `account/rateLimits/read` returned an official quota snapshot. The test printed no account values, credentials or personal paths.
- **Real floating-form read passed:** the new form reached fresh official quota state through its normal discovery path and exited cleanly.
- `package.ps1` rebuilt file version 0.4.2.0 and regenerated the EXE and ZIP SHA256 manifests from that output. Repository delivery tests independently inspect ZIP members, inner/outer checksums, PE version marker and icon resolutions.

Only the reviewed four-file portable ZIP is published. Preferences, logs, fixture screenshots, live output, credentials, Codex itself and test executables are excluded. The repository artifact audit explicitly checks the latest ZIP and reviewed icon while retaining the historical ZIP check.

Limits: the current v0.4.2 binary has not been directly tested on the other computer. That computer's successful earlier v0.3 discovery repair is supporting evidence for the layout, not proof of this release's complete UI behavior there. Windows ARM64, physical multi-monitor DPI transitions, fixed-shortcut cache refresh and native glass blur remain unverified. This unsigned experimental build does not install a runtime or change account/VPN settings.

## Public-package preparation — 2026-11-02

On Linux, the existing executable was preserved byte-for-byte; only ZIP documentation and MIT LICENSE inclusion changed. The ZIP checksum was recalculated. All 16 Node tests and the source-only build passed, including ZIP member, EXE hash/version and icon checks. No Windows compilation, Defender scan, real CLI query, desktop-host install or Windows GUI test was rerun during this preparation. Earlier results above are historical records, not independent reproduction.
