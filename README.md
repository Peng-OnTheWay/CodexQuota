# CodexQuota

一个极简 Windows 桌面悬浮卡片，显示本机 Codex 的 **5 小时** 和 **本周** 剩余额度。

```
         ● 5小时
          78%
      截止到 14:35
      ──────────
          本周
          45%
    截止到 周六 08:00
       Codex 11:06
```

- 纯本地，不联网
- 不读取登录凭证（`auth.json`）
- 定时 + 文件监听自动刷新
- 系统托盘常驻

## 快速上手

### 方式一：下载预构建版本

前往 [Releases](https://github.com/Peng-OnTheWay/CodexQuota/releases) 下载最新的 `CodexQuota.exe`，双击即可运行。无需安装 .NET 运行时。

### 方式二：从源码构建

需要 Windows + .NET 8 SDK。

```powershell
# 运行
dotnet run --project .\src\CodexQuota\CodexQuota.csproj

# 测试
dotnet run --project .\tests\CodexQuota.Tests\CodexQuota.Tests.csproj

# 发布
dotnet publish .\src\CodexQuota\CodexQuota.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\app
```

## 使用指南

### 前提条件

你的电脑上必须有 Codex，且正常使用过（产生了额度消耗记录）。Codex 的会话日志默认位置：

```text
%USERPROFILE%\.codex\sessions\
```

如果你刚安装 Codex 还没用过，启动后卡片会显示"等待 Codex 额度…"。用 Codex 触发一次对话，日志生成后卡片会自动刷新。

### 界面说明

| 区域 | 说明 |
|---|---|
| **● 状态点** | 绿色：近 12 小时内数据正常 |
| **5小时** | 过去 5 小时的滑动窗口剩余百分比 |
| **本周** | 本周的滑动窗口剩余百分比 |
| **截止到 xx:xx** | Codex 报告的下次额度重置时间（精确到几点几分） |
| **Codex 日志时间** | 显示最后一次读取到 Codex 会话日志的时间 |

剩余百分比会**严格绑定** Codex 日志中的真实数据，不会虚假倒数。

### 右键菜单

在卡片上右键可操作：

| 菜单项 | 说明 |
|---|---|
| 立即刷新 | 立即重新读取最新日志 |
| 始终置顶 | 开关卡片始终显示在最前端 |
| 锁定位置 | 锁定后不可拖动 |
| 开机启动 | 在启动文件夹创建启动脚本 |
| 打开日志目录 | 在资源管理器中打开 Codex sessions 文件夹 |
| 关于 | 显示版本信息 |
| 退出 | 完全退出程序 |

### 系统托盘

程序启动后会在系统托盘显示图标，如果没有看到卡片，可能是被隐藏了。

| 操作 | 效果 |
|---|---|
| 双击托盘图标 | 显示/隐藏卡片 |
| 右键托盘图标 | 弹出菜单（显示/隐藏、刷新、退出） |

### 关闭窗口 ≠ 退出

点击窗口的 X 按钮只是**隐藏**卡片到托盘，程序仍在后台运行。彻底退出请在右键菜单或托盘菜单中选"退出"。

## 数据来源

读取 `%USERPROFILE%\.codex\sessions\` 下最新的 `token_count` 事件，解析 `rate_limits` 中两条记录：

| window_minutes | 含义 |
|---|---|
| 300 | 5 小时窗口 |
| 10080 | 1 周窗口 |

取 `used_percent` 反算剩余百分比，取 `resets_at` 作为截止时间。

## 项目结构

```text
CodexQuota.sln
src/CodexQuota/               WPF 悬浮窗程序
tests/CodexQuota.Tests/       解析器 smoke tests
```

## 安全审计要点

| 功能 | 入口文件 |
|---|---|
| 额度读取 | `Services/CodexLogLocator.cs`、`CodexQuotaParser.cs`、`CodexQuotaService.cs` |
| 设置写入 | `Services/SettingsService.cs`、`StartupService.cs` |

- 不访问网络（无 `HttpClient` / `WebClient` / `Socket`）
- 不上传日志，不做账号登录，不做遥测
- 日志读取仅限 `%USERPROFILE%\.codex\sessions\*.jsonl`
- 设置仅写入 `%LOCALAPPDATA%\CodexQuota\settings.json`

