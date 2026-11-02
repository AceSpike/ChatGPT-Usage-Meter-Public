# Windows Floating v0.4.2

[Download the Windows ZIP](downloads/ChatGPTUsageMeter-v0.4.2-Windows.zip?raw=true) · [中文安装说明](README.zh-CN.md)

This is the latest Windows meter, based on the v0.4.1 skin edition. The historical `windows/` v0.3 source and downloads remain unchanged. The plugin edition is maintained separately and is not installed or changed by this EXE.

## Install and use

1. Download and extract the ZIP from the public repository. The archive contains `ChatGPTUsageMeter.exe`, `README.txt`, `LICENSE` and an EXE checksum. This is unofficial, unsigned experimental software, not an OpenAI product.
2. Close an older meter through its own Exit menu. Launch the extracted EXE. Its file version is **0.4.2.0** and its window title contains **v0.4.2 Skins**.
3. Use an existing official Codex CLI sign-in on this computer. No account, token, CLI runtime or credentials are included. Windows x64 with .NET Framework 4.8 is the tested environment.
4. Right-click the floating window or its notification-area icon. **Tray skin** and **Floating window skin** each offer A–D independently. **语言 / Language** switches Chinese/English. Appearance preferences remain under the existing LocalAppData meter directory.

The unsigned portable build performs no installation, autostart registration or VPN changes. Follow your system's normal security policy. A fixed taskbar shortcut may keep a Windows-cached icon; this package does not claim to refresh such caches.

## CLI discovery fix

When creating a read-only app-server connection, the meter first searches the existing npm native CLI package under roaming ApplicationData. This priority is preserved. If it is absent, it searches `%LOCALAPPDATA%\OpenAI\Codex\bin\<version-directory>\codex.exe`, choosing the executable with the newest **file** UTC write time. Opaque directory names are not sorted as versions. Incomplete directories are skipped. A direct `bin\codex.exe` is a final desktop fallback.

Each new connection performs discovery again, so adding, renaming or removing a version directory does not leave a cached absolute path. An already working connection can continue until reconnect. The meter never starts login or switches away from a found npm CLI just because its account query fails.

Missing CLI, start failure, timeout, rejected RPC and invalid quota responses now have short, bilingual visible reasons. Raw CLI stderr and service error bodies are not shown or saved. Failed refreshes retain explicitly marked in-memory cached official values and their original timestamp; successful recovery clears the error. The existing unverified local-session fallback remains labeled as such.

This reads **Codex quota**, not all ChatGPT web message limits or API billing. Only the existing `account/rateLimits/read` request is used for official quota.

## Source, build and verification

```powershell
.\build.ps1
.\test.ps1
.\test.ps1 -Skins
.\test.ps1 -LiveDesktop
.\test.ps1 -LiveForm
.\package.ps1
```

`-LiveDesktop` deliberately suppresses npm discovery and verifies the installed desktop-bundled CLI's real read-only quota RPC without printing account values. `-LiveForm` checks the actual floating-form connection. Live checks require existing sign-in and service access; they do not log in.

`package.ps1` rebuilds, checks file version 0.4.2.0, regenerates the flat ZIP and both checksum manifests from the actual output. Compare the ZIP with `downloads/SHA256SUMS.txt` and the extracted EXE with `downloads/EXE-SHA256SUMS.txt`. A checksum from an earlier v0.3 repair is not the checksum of this release.

See [VALIDATION.md](VALIDATION.md) for verified scope and remaining limits. Generated binaries, fixture screenshots, preferences and test output stay untracked except for the explicitly reviewed release ZIP.
