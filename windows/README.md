> Historical v0.3 edition. Latest Windows build: [v0.4.2](../windows-v0.4.2/README.md) / [新版中文说明](../windows-v0.4.2/README.zh-CN.md). Historical source and checksums below are preserved.

# Windows Floating v0.3 — historical portable test

[中文说明](README.zh-CN.md) · [Download ZIP](downloads/ChatGPTUsageMeter-Floating-v0.3-Windows-Test.zip?raw=true) · [Validation](VALIDATION.md)

From this public repository, download the historical ZIP, extract it into a normal local folder, and open its only application: **ChatGPTUsageMeter.exe**. Do not launch it inside the ZIP preview. The ZIP contains the EXE, README and checksums; it is a repository download, not a GitHub Release or an installer.

## Required on the test PC

- Windows x64 with .NET Framework 4.8.1. Only one existing Windows environment has been tested; this is not clean-PC certification.
- An already installed **official Codex CLI using its default npm installation layout**, signed in by the user to the intended account. [Official CLI instructions](https://developers.openai.com/codex/cli). The app currently discovers the native CLI under the current user's roaming npm package; PATH-only, Store and custom layouts are not supported by this native reader. The plugin has different discovery logic.
- Network access from that CLI to the official quota service. This ZIP bundles neither Codex, its sign-in nor .NET. The native app does not require an MCP plugin host or npm install in this repository. Building this app requires the Windows .NET Framework C# compiler, already present on the tested PC.

The EXE is **unsigned**. Do not bypass Windows security warnings. A Defender scan found no threats in this exact build; that is not an independent security audit or guarantee.

## Use

- Drag the floating meter's labels/numbers to move it.
- Right-click the meter or its single percentage tray icon. Both share **语言 / Language** → 简体中文 / English, and **Background color** → Dark navy / Charcoal / Forest green / Deep plum. Selected choices have checkmarks.
- Choices take effect immediately and are restored on reopening. Only validated language/background codes are saved in `%LOCALAPPDATA%\CodexUsageMeter\preferences.json`. The old v0.2 `language.txt` can supply the initial language. Failed writes show a notice.
- The same menu provides show, hide, refresh, reposition and exit. Closing hides; **Exit meter** quits. A left click on the tray icon restores the window. Check Windows' hidden tray area if necessary.
- Exit any older meter before switching. A shared single-instance lock prevents simultaneous old/new meters. The app does not install or register autostart.

Numbers are **remaining Codex quota** and hours until its official reset, for the local CLI's account. They are not general ChatGPT message limits or API billing. Queries run every 60 seconds, with a 10-second manual retry floor. Unknown/expired results stay unknown or await refresh. Failures retain marked cached data. Before any official result, the native app may read bounded local Codex session logs for quota fields; fallback account identity is unverified. It never uploads those logs, but they can contain conversations and must not be added to the repository.

## Verify the download

Compare the ZIP with [downloads/SHA256SUMS.txt](downloads/SHA256SUMS.txt), then the extracted EXE with [downloads/EXE-SHA256SUMS.txt](downloads/EXE-SHA256SUMS.txt):

```powershell
Get-FileHash -Algorithm SHA256 .\ChatGPTUsageMeter-Floating-v0.3-Windows-Test.zip
Get-FileHash -Algorithm SHA256 .\ChatGPTUsageMeter.exe
```

Only the EXE hash in that file identifies the reviewed binary; an independent rebuild may have a different PE timestamp/hash.

## Build and test from source

From the repository root on Windows:

```powershell
.\windows\build.ps1
.\windows\test.ps1
.\windows\bin\ChatGPTUsageMeter.exe
```

Use a PowerShell environment allowed by your organization; do not disable execution policy or security controls. `bin/` and `test-output/` are ignored and remain local. Tests use marked mock quota and isolated preference directories; they do not query the account. An optional live check, only when no meter is running, is `windows/bin/BilingualChecks.exe --live` from `windows/test-output`; it queries the signed-in CLI and does not log raw account data. It is not part of the default test run.

The retained legacy dashboard classes are compile-time dependencies, not alternate portable launchers. Only `FloatingProgram` is selected by the build. No old split-indicator EXE is distributed.

Physical mouse/tray interactions, cross-monitor DPI, sleep/wake, long-running stability, and the second PC's installation/sign-in remain manual checks. If the CLI cannot be found or connected, check its installation/sign-in yourself; do not send credentials or raw session files.
