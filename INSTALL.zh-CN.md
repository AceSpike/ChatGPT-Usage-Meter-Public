> Windows 最新便携版请先看 [v0.4.2 安装与升级说明](windows-v0.4.2/README.zh-CN.md)。本页其余内容为插件版；历史 Windows v0.3 仍保留，不是最新版。

# 插件版环境与安装说明（MIT 预发布候选）

需要独立 Windows 悬浮窗时，请使用 [Floating v0.3 下载与说明](windows/README.zh-CN.md)。下面的宿主安装步骤只适用于插件版。

这不是“下载后双击 EXE 就能用”的 Windows 安装包。它是源代码插件，宿主安装完成后才可打开界面。

## 必需条件

1. **Node.js 24.x**：本机验证版本为24.21.0；请从 nodejs.org 选择24.x官方发行版并按官方说明校验。其他主版本未认证。项目不捆绑Node，也不自动安装它。
2. **官方 Codex CLI 已安装且已登录**：按 https://developers.openai.com/codex/cli 的当前官方说明安装，用户自行在CLI完成登录。只支持服务实际返回的Codex额度，不保证API密钥模式能返回订阅窗口；不需要另建API密钥。
3. **支持本地插件、stdio MCP和MCP Apps的桌面宿主**：账号、工作区策略和宿主版本可能影响支持。侧栏/会话面板支持尚未在此候选中实装验证，不等于可把读数塞进标题栏。
4. **网络正常**：本机CLI能连接OpenAI官方额度服务。宿主在云端时不能借这个本地插件读取另一台电脑的登录。
5. **平台**：仅在一台Windows x64和现有CLI安装环境做过核心测试；Windows ARM64、其他PC及其他操作系统尚未认证。插件不要求.NET；此前独立浮条的.NET依赖不代表此Node插件的依赖。

## 先检查，再安装

在解压后的仓库根目录打开终端：

```text
node --version
node plugins/codex-usage/server/preflight.mjs
npm test
npm run build
```

项目没有npm依赖，无需执行npm install。preflight只检查Node版本和CLI可发现性，不证明账号已登录、网络正常或宿主已支持界面。

可选真实读取：

```text
npm run check:live
```

这条命令会向CLI已有账户只读查询，并在终端输出经过筛选的额度和时间；不要把终端输出放进公开Issue或仓库。项目本身不保存这些数据。

## 宿主安装：必须由用户明确操作/批准

仓库提供`.agents/plugins/marketplace.json`本地目录和`plugins/codex-usage`可移植插件；构建结果也在`dist/codex-usage`。依照宿主官方本地插件说明选择该来源并审核安装。安装会在宿主中登记一个本地stdio服务，所以本任务没有在“保持现有Codex配置不变”的约束下自动安装它。已确认本机CLI有以下安装命令，但尚未执行：

```text
codex plugin marketplace add .
codex plugin add codex-usage@codex-usage-local
```

这不是预览或dry-run命令，会实际新增来源/安装配置，需要先获得用户批准。当前CLI按此市场名只读查询返回installed/available均为空，不代表已安装。安装步骤和宿主中界面的实际可用性仍待验证，不能将本段视作已在所有版本通过的安装器说明。

安装后在新会话调用usage技能或get_usage工具。宿主支持时可从侧栏或会话面板打开横向界面。核对来源为本机CLI、取数时间以及正确账户。启动node server/server.mjs只会运行MCP服务，不会弹出Windows浮窗；双击HTML也不会连接账号。

旧浮条仍是独立的Windows程序，本插件没有安装或替换它，也不修改Codex二进制、不注入进程、不注册自启、不修改系统缩放/安全设置。未签名二进制不在候选包中；不要绕过Windows安全提示。

## 界面语言

在插件面板右上角的“语言 / Language”中选择“简体中文”或“English”。标签、状态提示、无障碍标签和日期格式会立即切换，不会因此重新查询额度。首次默认简体中文；宿主允许浏览器本地存储时，仅保存语言代码，重新打开同一来源的面板会恢复选择，不保存账户或额度数据。若宿主阻止存储，界面会说明语言只在本次打开期间有效，切换本身仍可用。

此设置属于本仓库的MCP Apps插件面板，不会修改另一个独立Windows悬浮窗或托盘程序。

## 数据与故障状态

界面每60秒请求一次，服务端合并重复请求；重置倒计时来自实际时间戳，每秒计算。失败时保留并标记缓存；未知、无额度数据和已过重置时间不会变成虚构数字。已有CLI登录账户可能不同于当前聊天账户。

插件不读取Cookie或凭据文件，不创建凭据。额度结果会返回给你批准的本地MCP宿主；只应在你信任的环境中启用。没有HTTP监听、遥测、外部字体或图片资源。

## 许可证与发布状态

项目源代码按用户确认的MIT许可发布，见LICENSE。署名使用已核实的项目所有者公开GitHub标识AceSpike，不推定其真实姓名或公司。MIT仅覆盖本项目代码，不覆盖或替代Node、Codex、ChatGPT的条款，不表示获得OpenAI背书。

本候选以 MIT 许可作为非官方实验源码公开；版本标识仍为 private-test.2。真实宿主安装与干净 Windows 验证尚未完成，不承诺通用兼容性。手动跨电脑测试参阅 PRIVATE-TEST.md。
