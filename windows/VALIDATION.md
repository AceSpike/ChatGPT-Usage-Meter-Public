# Floating v0.3 verification scope

The owner confirmed the menu-based v0.3 behavior on the development PC. This directory preserves the reviewed source and the exact previously scanned portable ZIP, not a claim of general Windows certification.

## Completed

- Existing Windows .NET Framework compiler built the native app and component tests. Default source/test code and quota logic match the owner-confirmed v0.3; repository scripts place generated files under ignored bin/test-output directories.
- Native WinForms component tests cover bilingual labels, context menu/status/unknown/expired/cache states, language/background persistence and reopen, invalid/blocked storage, legacy language migration, shared floating/tray menu, checkmarks, absence of a dashboard language toolbar, hide/restore/reposition/clamp/close-to-hide/exit.
- Recursive control bounds/label sizing passed for Chinese and English at simulated 100/150/200%. Both languages and all four backgrounds were rendered and inspected using native WinForms controls; alphanumeric/Chinese label contrast was tested at least 4.5:1. Mock preview values are explicitly labeled test data and are not bundled with the repository.
- The actual new floating form reached a fresh official account/rateLimits/read state through the installed CLI and exited cleanly. No raw account response or account identity was saved in the repository.
- Launched app's responding process, intended path and version 0.3.0.0 were checked on the development PC. The owner subsequently confirmed the result.
- Windows Defender custom scan of the exact portable EXE returned exit 0 and no threats. EXE is unsigned. This is a point-in-time scan, not an independent security audit or guarantee.
- Before repository upload, source/docs and every ZIP entry are checked for credential-like content and personal paths. The ZIP has exactly ChatGPTUsageMeter.exe, README.txt and SHA256SUMS.txt. ZIP and EXE hashes are recorded in downloads/. Root packaging tests verify the archive and extracted EXE hashes; plugin build still produces only the plugin package.

## Not established

Clean second-PC installation, other CLI layouts or architectures, physical cross-monitor DPI transitions, sleep/wake and long-running stability are not certified. Automated tests exercise native components and offscreen rendering; actual desktop mouse/tray input acceptance was not automated. Owner confirmation does not replace testing the next PC's environment and CLI account.

Source contains synthetic quota fixtures for tests only. Session logs, preferences, local paths, real usage snapshots and credentials are not repository deliverables. The native app's bounded local-log fallback differs from the plugin's no-session-scan policy; see SECURITY.md.
