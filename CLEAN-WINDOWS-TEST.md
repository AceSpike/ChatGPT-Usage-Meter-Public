# Required clean-Windows acceptance test (not yet completed)

Use a provided disposable Windows11 x64 test machine/VM and a consenting tester's account. Do not copy authentication or user profiles from the development PC. Do not disable OS/browser security protections.

1. Record OS/architecture and absence/presence of Node, CLI and compatible desktop host. Do not record usernames, tokens, account screenshots or quota values in public reports.
2. With no runtime, confirm the documented missing-Node condition; this candidate is not an EXE installer.
3. Install official Node24.x manually, then run npm test and npm run build without npm install. Confirm tests pass and CLI preflight reports missing CLI honestly.
4. Install official Codex CLI using its documentation. Before login, verify quota is unavailable, not a fabricated number. Complete login manually in the official client only.
5. Explicitly approve installation of the local plugin/stdio service in the compatible desktop host. This modifies that test host's plugin configuration; it is not performed automatically by build/tests.
6. Verify get_usage, actual sidebar/thread launch, readable horizontal UI, local account scope, UTC/local timestamps, 60-second refresh and manual refresh. Compare privately against the official usage screen.
7. Exercise unavailable/cached/expired states with a controlled test provider, keeping test data labeled and never packaging a fake provider as production. The unit tests already cover arithmetic; they do not replace live install verification.
8. Test host shutdown/restart, normal plugin removal, and absence of autostart or leftover listener. A tester controls cleanup of their disposable machine.

Pass/fail and versions may be reported publicly. Keep actual account details and usage private. These checks remain outstanding even though the project is publicly available as experimental software. Public distribution is not a clean-PC or real-host validation pass.
