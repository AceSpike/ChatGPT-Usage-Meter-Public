# CHATGPT USAGE METER — local plugin

MIT licensed; see LICENSE. This is a source-only Codex-quota plugin, not a standalone Windows executable.

Requires Node.js 24.x, an already-installed official Codex CLI signed in to the intended account, network access to its quota service, and a local desktop host supporting portable plugins, stdio MCP and MCP Apps. Other hosts/accounts/architectures are not certified. No npm packages or new API key are needed.

Install through the host's reviewed local-plugin workflow. mcp.json launches `node server/server.mjs` in this plugin's directory. That command is a stdio service, not a visible program; double-clicking it or the HTML does not install anything. `node server/preflight.mjs` checks runtime/CLI presence. Optional `node server/check.mjs` reads the existing account's quota; keep its output private.

The readonly get_usage tool returns remaining percent, reset time, fetched timestamp and cache state. The optional UI requests updates every60 seconds and computes countdown locally. No data is inferred as 100% after expiry. UI sidebar/thread entrypoints depend on host support and have not yet been installed/tested in the real desktop host. There is no title-bar injection or overlay in this package.

The existing CLI owns its authentication. The plugin does not open credential files or browser cookies, create accounts, start model turns, scan conversations, enable autostart or change settings. It returns filtered quota data to its approved MCP host and keeps only an in-memory cache. No generated image/font asset, usage snapshot or Windows binary is distributed.

Pre-release validation gaps: real host installation/UI, clean Windows desktop, account recovery and other platforms. Do not advertise this package as download-and-double-click ready.

Interface language: use the panel's **语言 / Language** selector for Simplified Chinese or English. The default is Simplified Chinese; the selected language code alone is saved in browser storage when allowed. A blocked-storage notice explains the current-view-only fallback. Switching translates UI text, accessibility labels and date formatting without querying quota. This does not change the separate Windows floating/tray utility.
