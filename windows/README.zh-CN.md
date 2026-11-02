> Historical v0.3 edition. Latest Windows build: [v0.4.2](../windows-v0.4.2/README.md) / [新版中文说明](../windows-v0.4.2/README.zh-CN.md). Historical source and checksums below are preserved.

# Windows Floating v0.3 历史便携测试版

[下载单 EXE ZIP](downloads/ChatGPTUsageMeter-Floating-v0.3-Windows-Test.zip?raw=true) · [English](README.md) · [验证记录](VALIDATION.md)

从公开仓库下载上述历史 ZIP，**完整解压到普通文件夹**，再双击唯一的 **ChatGPTUsageMeter.exe**。不要直接从压缩包预览运行。包内只有该 EXE、README 和校验值；这是历史仓库下载文件，不是 GitHub Release 或安装器。

## 电脑需要准备什么

1. Windows x64 和 .NET Framework 4.8.1。目前只在一台已有环境的 Windows 电脑验证，不能视为全新电脑安装验收。
2. 已安装并由你自行登录的**官方 Codex CLI**，当前原生程序要求默认的 **npm 安装目录**。按[官方说明](https://developers.openai.com/codex/cli)操作；程序只在当前用户 roaming npm 包内寻找原生 CLI，不支持仅 PATH、Store 或自定义安装路径。插件版的查找逻辑不同。
3. CLI 能联网读取官方额度。ZIP 不捆绑 Codex、登录或 .NET；不需要安装插件宿主，也不需要在本仓库执行 npm install。构建源码使用 Windows 现有 .NET Framework C# 编译器。

EXE **未签名**。请勿绕过系统安全提示。此版本 Defender 扫描未发现威胁，但扫描不等于独立安全审计或安全保证。

## 操作

- 拖动数字或标签移动紧凑浮条。
- **右键浮条或托盘图标**，选择“语言 / Language”中的简体中文或 English；在“背景颜色 / Background color”中选择深海蓝、炭灰、森林绿、暗紫。两处共用菜单，当前选项有勾选。
- 设置立即生效，下次启动恢复。只保存语言、颜色代码到 `%LOCALAPPDATA%\CodexUsageMeter\preferences.json`；旧 v0.2 的 language.txt 可提供初始语言。保存失败会提示，当前选择仍生效。
- 菜单还有显示、隐藏、刷新、重新定位、退出。关闭窗口只是隐藏；“退出浮条 / Exit meter”才会退出。左键托盘图标可恢复，找不到时检查系统托盘隐藏区域。
- 切换版本前先正常退出旧仪表；单实例锁会阻止旧版和新版同时运行。程序不注册开机启动，也没有安装步骤。

显示的是 **Codex CLI 当前账户的 Codex 剩余额度和距官方重置的小时数**，不是普通 ChatGPT 所有模型消息额度或 API 账单。每 60 秒查询，手动刷新最短 10 秒；未知或到期不推算成 100%。失败保留明确标识的缓存；首次官方取数失败时可能读取有限的本机 Codex 会话日志提取额度，此时缓存账户身份未核验。程序不会上传日志，但这些文件可能含聊天内容，不要加入仓库或发送给他人。

## 校验与源码测试

ZIP 的 SHA-256 见 [SHA256SUMS.txt](downloads/SHA256SUMS.txt)，解压后 EXE 的校验值见 [EXE-SHA256SUMS.txt](downloads/EXE-SHA256SUMS.txt)。可使用 PowerShell `Get-FileHash -Algorithm SHA256 文件名` 对比。源码独立重编译可能因 PE 时间戳不同产生不同哈希。

在仓库根目录、组织允许的 PowerShell 环境中执行：

```powershell
.\windows\build.ps1
.\windows\test.ps1
.\windows\bin\ChatGPTUsageMeter.exe
```

不要关闭执行策略或系统安全控制。构建产物在忽略的 windows/bin，模拟测试与隔离偏好在 windows/test-output；默认测试不联网、不读取真实账户。可选真实取数检查见 [English guide](README.md)。保留的旧仪表类是编译依赖，不作为其他启动程序分发。

已验证功能、视觉检查和真实取数范围见[验证记录](VALIDATION.md)。第二台电脑的环境/登录、真实鼠标菜单及托盘操作、多显示器跨屏 DPI、睡眠唤醒和长期稳定性仍需实际测试。
