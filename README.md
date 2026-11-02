# CHATGPT USAGE METER

**Unofficial community project; not affiliated with or endorsed by OpenAI.** Public experimental software under the MIT license, without warranty.

Two local **Codex quota** interfaces: a portable Windows floating meter and a source-only Codex/ChatGPT desktop plugin candidate. The project name does not mean this reads all ChatGPT message limits or API billing.

## Latest Windows edition: v0.4.2

[Download Windows v0.4.2 ZIP](windows-v0.4.2/downloads/ChatGPTUsageMeter-v0.4.2-Windows.zip?raw=true) · [中文安装与升级](windows-v0.4.2/README.zh-CN.md) · [English guide](windows-v0.4.2/README.md) · [Validation](windows-v0.4.2/VALIDATION.md)

This version preserves the v0.4.1 independent A–D tray/window skins and fixes discovery of the Codex desktop application's versioned CLI. It keeps npm priority, displays safe bilingual read errors, and regenerates checksums from the actual EXE/ZIP. Extract and run the new EXE; its file version is **0.4.2.0** and its title contains **v0.4.2 Skins**. Old v0.3 downloads below are retained as history. The existing plugin and its version are unchanged.

## Choose an edition / 选择版本

**For a new Windows install, choose v0.4.2. v0.3 is retained only for history.**
**Windows 新安装请选择 v0.4.2；v0.3 仅作历史保留。**

| Edition / 版本 | Start here / 下载与说明 | Runtime / 运行要求 |
|---|---|---|
| **Windows Floating v0.4.2 — latest, recommended / 最新推荐** | [Download v0.4.2 ZIP / 下载最新版](windows-v0.4.2/downloads/ChatGPTUsageMeter-v0.4.2-Windows.zip?raw=true) · [中文安装说明](windows-v0.4.2/README.zh-CN.md) · [English guide](windows-v0.4.2/README.md) | Signed-in official Codex CLI (npm or desktop-bundled) + .NET Framework 4.8; no plugin host / 已登录的官方 CLI（npm 或桌面附带）+ .NET Framework 4.8，无需插件宿主 |
| **Plugin private-test.2** | [Plugin installation](INSTALL.zh-CN.md) | Node 24, local MCP Apps host and signed-in Codex CLI |
| **Windows Floating v0.3 — historical / 历史保留** | [Historical v0.3 ZIP / 历史版下载](windows/downloads/ChatGPTUsageMeter-Floating-v0.3-Windows-Test.zip?raw=true) · [中文使用说明](windows/README.zh-CN.md) · [English](windows/README.md) | Existing official npm-installed Codex CLI sign-in and .NET Framework; no plugin host needed |

The Windows ZIP is an **unsigned experimental portable build**, not an installer. The public repository download does not require repository membership. Extract it before launching the only executable, `ChatGPTUsageMeter.exe`. Language and background colors are in its right-click menu. See the Windows guide for dependencies and untested second-PC scenarios. Neither edition includes credentials or logs you in.

## Plugin edition

**Known limitations:** the experimental plugin has unresolved input-handling/preflight/build robustness findings (including a null MCP input crash). See [validation details](VALIDATION.md). Use only in a trusted local test environment; the Windows executable is a separate implementation.

**MIT-licensed experimental plugin, version 0.1.0-private-test.2.** The existing version identifier is retained; public availability does not certify production readiness. This clean source distribution includes the local marketplace catalog and `.gitignore`, without earlier repository history. Real desktop-host installation remains unverified. Attribution uses the project owner's GitHub handle, AceSpike. See [cross-PC test instructions](PRIVATE-TEST.md).

[中文环境与安装说明](INSTALL.zh-CN.md). MIT covers this project's code, not Node.js, Codex or ChatGPT, and implies no endorsement.

## What it actually does

- Reads the installed Codex CLI's documented `account/rateLimits/read` through its stdio app server using the existing CLI sign-in.
- Projects only quota windows, percentages and reset times; never returns account email, tokens, raw errors, conversation text or billing details.
- Remaining = 100 minus used percent. Reset hours derive from the actual Unix reset timestamp. Missing/invalid data stays unknown; after reset, wait for a new service result instead of inferring 100%.
- A visible MCP App panel requests updates every 60 seconds. The server coalesces requests, caches for 60 seconds and rate-limits retries to 10 seconds. Failed refreshes retain an explicitly marked in-memory cache with the original fetched timestamp. Quota records stay in memory and are never saved to disk. Only the UI language preference may be saved in the host's browser storage.
- Provides a readonly MCP tool even in hosts that do not display UI.

The compact horizontal interface is an adaptation of the Windows meter. It runs in a supported MCP Apps iframe, optionally exposed through global sidebar and thread panel entrypoints. **It is not a native title-bar widget, an overlay or an always-visible taskbar meter.** The Windows edition is maintained separately under [`windows/`](windows/README.md) and does not use the plugin host. No claim is made that a desktop host will enable these entrypoints until installation testing is performed.

## Interface language

Use the **语言 / Language** selector in the plugin panel to choose **简体中文** or **English**. Labels, statuses, accessibility labels and date formatting switch immediately without a quota query. The initial default is Simplified Chinese. Only the selected language code is saved locally when the host allows browser storage, so it is restored when reopening the same view/origin. If storage is blocked, switching still works for the current view and a notice explains that it cannot be saved. No quota or account data is saved with this preference.

This selector applies to the MCP Apps panel. Windows Floating v0.3 has its own persistent language and background settings in the floating window/tray right-click menu.

## Requirements — not download-and-double-click

| Item | Plugin candidate |
|---|---|
| Host | Local Codex/ChatGPT desktop with portable plugins, stdio MCP and MCP Apps support; deployment policies may limit availability |
| Runtime | Node.js 24.x, tested with 24.21.0; other major versions are not certified |
| Data source | Official Codex CLI already installed and signed in to the desired account |
| Account | CLI sign-in can differ from the current chat/browser account; API-key-only accounts may not return subscription quota |
| Network | CLI must be able to reach the official quota service |
| Packages | No npm dependencies, package downloads or API key required |
| Windows | Tested only on one Windows x64 environment; ARM64/other PCs/Linux/macOS not certified |
| Executable | No Windows EXE, runtime installer or signed binary is included |

Downloading the ZIP or double-clicking HTML does **not** install a plugin or connect an account. A standalone HTML page shows “host unavailable”, never sample quota. This package does not bundle Node, Codex, sign-in or an entire desktop host. `node plugins/codex-usage/server/preflight.mjs` checks runtime and CLI presence without disclosing a local path; it does not prove sign-in or host support. Do not bypass SmartScreen or OS security notices. A clean-PC installation and signed Windows release remain separate unverified work.

## Local review and installation (user-controlled)

1. Install [Node.js 24.x](https://nodejs.org/en/download) and the [official Codex CLI](https://developers.openai.com/codex/cli) if needed. Do not use third-party repackaged binaries. Sign in through the official CLI yourself. This plugin never starts login or creates credentials.
2. Review the source and `npm test`; run `npm run build`. No `npm install` is necessary.
3. Optional: `npm run check:live` reads the existing CLI account and prints a filtered result to the terminal. Do not upload that output. It is not saved by the project.
4. Review `plugins/codex-usage/plugin.json` and `mcp.json`. The server command is `node server/server.mjs` with plugin-relative `cwd: ./`.
5. For a supported local repo-marketplace workflow, the provided `.agents/plugins/marketplace.json` points to `./plugins/codex-usage`. Open the repo using your host's documented plugin workflow, review the connection/installation prompt, and install explicitly. Restart the host only if the official workflow requires it. **No installation or settings changes are performed by the build.**
6. In a new chat, invoke the `usage` skill or `get_usage` tool. If supported, open the sidebar/thread UI. Confirm the local CLI account, source and fetched timestamp against the official quota view.

Windows detection supports the existing npm-installed native Codex binary or a `codex.exe` on PATH. Other layouts may require the user to set `CODEX_CLI_PATH` to an already-installed executable. This variable is a local path, not a credential; do not commit local overrides. Cloud/remote hosts cannot read your computer's CLI sign-in and will correctly report unavailable.

## Security and permissions

The plugin starts only its own read-only Codex app-server child on demand. It does not start model turns, read credential files or browser cookies, scan sessions, modify configuration, install software, change permissions, register autostart or expose an HTTP listener. MCP transport is stdin/stdout. Raw stderr and raw service errors are not exposed. The host may receive the returned quota data; approve installation only in the local environment you intend to trust.

The plugin package includes no screenshots, user usage snapshots, account records, binaries, generated image assets or personal filesystem paths. The documented Windows ZIPs and the latest application's icon resource are explicitly allowlisted and checksum-verified; historical downloads are retained. UI markup and styles are code-native. No external image/font/CDN resources are loaded. Do not add screenshots containing personal context to a public repository.

## Development

```text
npm test
npm run build
node plugins/codex-usage/server/server.mjs
```

The last command is an MCP stdio server, not a visible desktop program; the host normally launches it. `dist/codex-usage` is a clean source plugin package. Build/tests use Node's standard library only and never install or publish anything.

## Verified and remaining checks

Unit tests cover remaining percentages, both windows, unknown/invalid values, exact reset/expiry, coalescing, cache retention and recovery. MCP tests cover initialization, readonly metadata, tool calls, resources and invalid requests. Build performs a plugin-content scan for paths/credential-like strings and disallows snapshot/binary files.

Actual CLI access is separately tested locally without storing a quota snapshot. This does **not** prove another PC is ready, that UI bridges work in every host, that marketplace installation succeeds, or that the software is production-ready. Public availability is separate from those checks. The full host UI install, clean machine, architecture, sign-in recovery and signing/reputation checks remain unverified. See `RELEASE-CHECKLIST.md`.

## Official references

- [Portable plugin packaging](https://developers.openai.com/plugins/build/plugins)
- [MCP Apps UI](https://developers.openai.com/plugins/build/chatgpt-ui)
- [Sidebar and thread extensions](https://developers.openai.com/plugins/build/extensions)
- [Official Codex quota interface](https://learn.chatgpt.com/docs/app-server)
- [GitHub naming rules](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)

The requested display name is **CHATGPT USAGE METER**. The clean public repository is **AceSpike/ChatGPT-Usage-Meter-Public**. It is not a fork and does not include prior commit history.
