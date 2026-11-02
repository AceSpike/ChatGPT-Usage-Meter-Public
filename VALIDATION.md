Latest Windows v0.4.2 checks are recorded separately in [windows-v0.4.2/VALIDATION.md](windows-v0.4.2/VALIDATION.md). Existing plugin and historical validation records below are retained.

# Local validation record — not a release approval

Original validation environment: Windows x64, Node.js 24.21.0. No new runtime/package was installed. These historical results are not fresh cross-PC or real-host certification.

Passed:
- Six Node tests covering quota math and windows, expiry/missing/range checks, 60-second coalescing, error cache/retry/recovery, MCP initialization/tools/resources, invalid requests, UI syntax, standalone unavailable state, foreign postMessage rejection, and a separate process with CLI/user-home environment removed.
- Full Draft 2020-12 JSON-schema validation of plugin.json and mcp.json against the official Agent Plugins 1.0.0 schemas fetched in memory. The validator was already available locally; it is not a runtime dependency.
- Source and built-plugin heuristic scans for user paths, credential-shaped strings, usage snapshots and binary/image/log artifacts. Only source files and synthetic tests are packaged. This is not an independent security audit.
- A new readonly quota-adapter check reached the official service and returned fresh status. No real quota values, account identity, raw errors or fetchedAt snapshots were stored in the repository or release archive.
- Preflight can identify Node version, OS/architecture and CLI presence without outputting the installation path. This does not prove host installation or sign-in on another computer.

Not passed / not attempted:
- Real desktop plugin installation and MCP Apps/sidebar/thread UI launch.
- A clean Windows PC with no Node or Codex, another CLI install layout, ARM64, other OS, or Node major versions other than the local version.
- New login, expired-login recovery, live network outages, signing or SmartScreen reputation checks.
- Public plugin-directory submission. Public source distribution is separate from host installation validation.

The existing installed Windows floating utility was not rebuilt, replaced, stopped or configured by this packaging task. The source-only plugin is a separate implementation of the same quota display concept, not a repackaged signed EXE.

The isolated-process test proves honest failure when dependencies/auth environment are absent; it is not a clean Windows OS or desktop-host installation test. No available Windows Sandbox executable, Docker/Podman command, or common VirtualBox/VMware executable was found in the checked environment. No virtualization feature or software was enabled/installed. WSL presence does not establish a clean Windows desktop test.

Real-host installation remains unattempted because it registers a new local plugin/MCP service in the existing host configuration. Current constraints keep that configuration unchanged. This is a validation gap, not a claimed automatic-approval rejection. A user-approved/manual test installation and an appropriate Windows test environment are needed before claiming real-host compatibility.

## Historical packaging checks

Source packaging and official manifest schema checks passed in a scrubbed Linux environment under Node.js 24.22.1. These are not Windows, authenticated CLI or real-host end-to-end tests.

Known unresolved plugin findings: a null MCP input can crash the server; non-Windows preflight may report CLI presence with an empty PATH; repeated builds can retain obsolete dist files. A similar null-response failure in the CLI handler was identified by static inspection, not reproduced. Passing the existing tests does not close these findings.

## Language selection — 0.1.0-private-test.2

- The MCP Apps panel now offers Simplified Chinese and English. Labels, status messages, accessibility labels and local date formatting change immediately. Simplified Chinese is the initial default. This does not add a Windows floating window or tray feature.
- Only the `codex-usage.language` preference is stored, and only when the host allows browser storage. No quota, account or authentication data is persisted. If storage access fails, the UI keeps the choice in memory and explains that it will not survive reopening. Switching language does not query the account or reset cached quota.
- All 13 Node tests passed under Node.js 24.22.1 with PATH and HOME scrubbed. Tests cover both languages, reload persistence, invalid saved values, blocked reads/writes, accessibility labels, date display, missing/expired/cached/unavailable states, query failure and recovery, repeated refresh/language changes, foreign-message rejection and consistent version metadata. The source-only build and plugin/MCP schema validation also passed.
- Independent source/test review found no remaining language-feature defect after synchronizing the package, plugin, UI, MCP server and CLI client version strings. Backend quota-reading logic is unchanged.
- Actual Chromium rendering was attempted but could not launch because of the execution environment's socket and runner restrictions. No visual or real desktop-host compatibility pass is claimed. Narrow-window layout, real-host language persistence and the clean Windows end-to-end flow still need manual verification. Previously documented backend/build robustness findings remain unresolved; public distribution does not resolve these testing gaps.
