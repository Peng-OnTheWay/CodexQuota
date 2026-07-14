# CodexQuota

CodexQuota 是一个极简 Windows 桌面悬浮卡片，用来显示 Codex 本地日志里记录的 **本周剩余额度**。

新版 UI 已移除历史上的 5 小时额度展示，主视觉只保留一个大数字：本周余量。
右上角红绿点显示本机 ChatGPT 桌面程序是否正在运行。

## 给普通用户：怎么使用？

如果你只是想使用这个小工具，不需要看源码，也不需要打开 Visual Studio。

### 第 1 步：下载 exe

请到本仓库右侧或顶部的 **Releases** 页面下载最新版：

```text
CodexQuota.exe
```

如果 Releases 里还没有 exe，说明作者还没有发布预构建版本。此时请看下方“给开发者：从源码构建”。

### 第 2 步：双击运行

双击 `CodexQuota.exe` 后，桌面右上角会出现一个小卡片。

如果 Windows 弹出安全提示，选择“更多信息” → “仍要运行”。这是因为个人小工具通常没有代码签名。

### 第 3 步：确保 ChatGPT / Codex 正在运行

右上角状态点表示本机 ChatGPT 桌面程序是否运行：

| 状态 | 含义 |
|---|---|
| 绿色 `● ChatGPT 在线` | 检测到本机 ChatGPT 桌面程序正在运行 |
| 红色 `● ChatGPT 离线` | 没有检测到 ChatGPT 桌面程序 |

如果从离线变成在线，CodexQuota 会自动重新读取本地日志并同步余量。

### 第 4 步：如果显示“等待 Codex 写入周额度”

这通常表示本机还没有可读取的 Codex 额度记录。

请先正常使用一次 ChatGPT / Codex，让它产生本地日志。CodexQuota 会自动刷新。

### 如何退出？

右键悬浮卡片或系统托盘图标，选择：

```text
退出
```

直接关闭窗口通常只是隐藏到托盘，程序仍会在后台运行。

## 设计原则

- 只显示 Codex 本地日志真实写入的数据。
- 不根据时间流逝虚假倒数，不伪造实时额度。
- 如果额度截止时间已过但 Codex 还没有写入新的确认记录，显示“待确认”。
- 不读取登录凭证，不访问网络，不上传日志。

## 界面

卡片显示内容：

```text
Codex Quota        ● ChatGPT 在线

本周余量              67%
截止到7月20日

██████████░░░░░
只显示 Codex 写入的真实周额度      3分钟前更新
```

说明：

| 区域 | 说明 |
|---|---|
| 右上角红绿点 | 绿色：本机 ChatGPT 桌面程序正在运行；红色：未检测到运行 |
| 本周余量 | `100 - used_percent`，来源于 Codex 日志中的周额度窗口 |
| 截止到 x月x日 | 来源于 `resets_at`，只显示日期 |
| x分钟前更新 | 来源于对应 `token_count` 日志的 `timestamp` |

程序每 5 秒检测一次本机 `ChatGPT` 进程。如果 ChatGPT 从关闭变为开启，CodexQuota 会立即重新读取本地 Codex 日志并同步余量。

## 数据来源

Codex 会话日志默认位于：

```text
%USERPROFILE%\.codex\sessions\
```

程序读取最新的 `.jsonl` 日志，寻找：

```json
{
  "type": "event_msg",
  "payload": {
    "type": "token_count",
    "rate_limits": {
      "primary": {
        "used_percent": 33,
        "window_minutes": 10080,
        "resets_at": 1784355627
      }
    }
  }
}
```

当前界面只使用：

| 字段 | 用途 |
|---|---|
| `window_minutes = 10080` | 识别本周额度窗口 |
| `used_percent` | 计算剩余额度 |
| `resets_at` | 显示截止日期 |
| `timestamp` | 显示“多久前更新” |

解析器仍保留对旧 5 小时窗口的兼容读取能力，但 UI 不再展示它。

## 安全边界

CodexQuota 的第一原则是可审计、纯本地：

- 只读取 `%USERPROFILE%\.codex\sessions\*.jsonl`
- 只写入 `%LOCALAPPDATA%\CodexQuota\settings.json`
- 可选开机启动只写入当前用户启动文件夹
- 不读取 `%USERPROFILE%\.codex\auth.json`
- 不使用 `HttpClient` / `WebClient` / `Socket`
- 不上传日志，不做账号登录，不做遥测

## 项目结构

```text
CodexQuota.sln
src/CodexQuota/               WPF 悬浮窗程序
tests/CodexQuota.Tests/       零依赖解析器 smoke tests
LICENSE
THIRD-PARTY-NOTICES.md
```

仓库不提交编译产物。以下目录/文件会被 `.gitignore` 忽略：

```text
bin/
obj/
publish/
publish-new/
app/
CodexQuota.exe
*.dll runtime copies
```

## 从源码运行

需要 Windows + .NET 8 SDK。

```powershell
dotnet run --project .\src\CodexQuota\CodexQuota.csproj
```

## 测试

```powershell
dotnet run --project .\tests\CodexQuota.Tests\CodexQuota.Tests.csproj
```

期望输出：

```text
All CodexQuota parser smoke tests passed.
```

## 发布本地 exe

发布到本地 `publish` 目录：

```powershell
dotnet publish .\src\CodexQuota\CodexQuota.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o .\publish
```

生成文件：

```text
publish\CodexQuota.exe
```

如果正在运行旧版 exe，Windows 会锁住文件。请先右键托盘图标，选择“退出”，再重新发布。

## 给开发者：上传给别人下载

仓库不会提交 `CodexQuota.exe`，因为 exe 和运行时 DLL 是构建产物。

如果你想让普通用户直接下载使用，推荐流程是：

1. 本地运行 `dotnet publish` 生成 `publish\CodexQuota.exe`
2. 打开 GitHub 仓库的 **Releases**
3. 创建一个新 Release
4. 上传 `publish\CodexQuota.exe`
5. 在 Release 说明里写明：下载 exe 后双击运行

## 常见问题

### 为什么不是每分钟变化？

程序每分钟会重新读取本地日志，也会监听日志文件变化。

但 Codex 额度本身只有在 Codex 写入新的 `token_count.rate_limits` 时才会变化。没有新日志时，CodexQuota 不会编造新的额度。

### 右上角在线/离线检测的是什么？

检测的是本机是否存在名为 `ChatGPT` 的桌面程序进程。它只检查本地进程列表，不联网，也不登录任何账号。

### 为什么显示“待确认”？

本地日志里的 `resets_at` 已经过期，但 Codex 还没有写入新的额度记录。为了避免把旧额度伪装成当前额度，程序会显示“待确认”。

### 为什么不读取在线接口？

第一版刻意不联网、不读取 `auth.json`，这样用户可以很容易审计它的安全边界。
