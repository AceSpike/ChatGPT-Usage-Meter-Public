# Windows 用量仪表 v0.4.2 安装说明

[下载最新版 ZIP](downloads/ChatGPTUsageMeter-v0.4.2-Windows.zip?raw=true) · [英文说明](README.md)

本版以 v0.4.1 的四套皮肤为基础，补上另一台电脑上已确认的 Codex 桌面 CLI 识别问题。旧 `windows/` v0.3 源码、下载和插件均保留。

1. 从公开仓库下载 ZIP，先解压。压缩包包含 `ChatGPTUsageMeter.exe`、`README.txt`、`LICENSE`、`SHA256SUMS.txt`。这是非官方实验项目，与 OpenAI 无隶属或背书关系。
2. 从旧仪表右键菜单选择退出，再运行解压后的 EXE。文件属性版本应是 **0.4.2.0**，窗口标题应包含 **v0.4.2 Skins**。不要再运行旧 ZIP 解压出来的同名程序。
3. 本机需有已登录的官方 Codex CLI。已测试 Windows x64、.NET Framework 4.8。包内没有 CLI、账号、密码、令牌或登录数据。
4. 右键悬浮窗或通知区图标，可独立选择“托盘皮肤”A–D 和“悬浮窗皮肤”A–D；“语言 / Language”切换中英文。沿用原有 LocalAppData 外观设置。

这是未签名的便携版，不安装插件、不设置开机启动、不改 VPN。请遵循系统原有安全策略。Windows 固定快捷方式的图标可能有缓存，本版不声称刷新该缓存。

## Codex 识别规则

保持 **npm 版优先**。npm 原生程序不存在时，再查找：

```text
%LOCALAPPDATA%\OpenAI\Codex\bin\<版本目录>\codex.exe
```

按程序文件的 UTC 更新时间选最新版本，不按目录名排序；空的更新目录会跳过。每次重新连接都会重新查找，支持版本目录新增、改名和删除。已正常连接的进程可继续使用，重连时再发现新位置。发现 npm 程序但查询失败时，不会自动换用另一份 CLI 或启动登录。

未找到 CLI、启动失败、超时、RPC 拒绝和无效额度数据都会显示简短错误原因。不会显示或保存 CLI 原始错误内容。失败时仍保留有标记的内存缓存及原始时间，成功后清除错误；本地日志回退仍明确标为未验证数据。

仪表读取的是 **Codex 用量额度**，不是所有 ChatGPT 网页消息上限或 API 账单；官方取数只使用既有的 `account/rateLimits/read`。

## 校验与构建

下载 ZIP 后，对照 `downloads/SHA256SUMS.txt`；解压后的 EXE 对照 `downloads/EXE-SHA256SUMS.txt`。旧 v0.3 修复版的哈希不能用于本版。本版清单由实际生成的 EXE 和 ZIP 重新计算。

在本目录运行 `build.ps1` 构建；`test.ps1` 运行离线测试，`test.ps1 -Skins` 检查四套皮肤，`test.ps1 -LiveDesktop` 强制使用桌面附带的 CLI 做真实只读取数，`test.ps1 -LiveForm` 检查实际浮窗连接。`package.ps1` 重建并生成 ZIP 与新校验清单。

另一台电脑曾验证旧 v0.3 定位修复可取数；本 v0.4.2 在那台电脑上仍需实际打开确认，不能把旧 EXE 的成功当作新版完整验收。
