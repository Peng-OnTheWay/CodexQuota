using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CodexQuota.Helpers;
using CodexQuota.Models;
using CodexQuota.Services;

namespace CodexQuota;

public partial class MainWindow : Window
{
    private readonly CodexLogLocator _locator = new();
    private readonly SettingsService _settingsService = new();
    private readonly StartupService _startupService = new();
    private readonly CodexQuotaService _quotaService;
    private readonly DispatcherTimer _refreshTimer = new();
    private readonly DispatcherTimer _fileChangeDebounceTimer = new();
    private readonly TrayIconService _trayIconService = new();

    private FileSystemWatcher? _watcher;
    private AppSettings _settings;
    private QuotaSnapshot? _currentSnapshot;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        _settings = _settingsService.Load();
        _quotaService = new CodexQuotaService(_locator, new CodexQuotaParser());

        ApplySettings();
        ConfigureTimers();
        ConfigureTrayIcon();
        ConfigureFileWatcher();
    }

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            await RefreshNowAsync();
        }
        catch
        {
            // 启动时即使读日志失败也不崩溃，等定时器重试
            RenderEmptyState("等待 Codex 额度…");
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        SaveWindowPlacement();
        _watcher?.Dispose();
        _trayIconService.Dispose();
        base.OnClosing(e);
    }

    private void ApplySettings()
    {
        Opacity = Math.Clamp(_settings.Opacity, 0.35, 1.0);
        Topmost = _settings.AlwaysOnTop;
        AlwaysOnTopMenuItem.IsChecked = _settings.AlwaysOnTop;
        LockPositionMenuItem.IsChecked = _settings.LockPosition;
        StartWithWindowsMenuItem.IsChecked = _startupService.IsEnabled();

        if (_settings.WindowLeft.HasValue && _settings.WindowTop.HasValue)
        {
            Left = _settings.WindowLeft.Value;
            Top = _settings.WindowTop.Value;
        }
        else
        {
            Left = SystemParameters.WorkArea.Right - Width - 24;
            Top = SystemParameters.WorkArea.Top + 40;
        }
    }

    private void ConfigureTimers()
    {
        _refreshTimer.Interval = TimeSpan.FromSeconds(Math.Max(10, _settings.RefreshSeconds));
        _refreshTimer.Tick += async (_, _) => await RefreshNowAsync();
        _refreshTimer.Start();

        _fileChangeDebounceTimer.Interval = TimeSpan.FromSeconds(2);
        _fileChangeDebounceTimer.Tick += async (_, _) =>
        {
            _fileChangeDebounceTimer.Stop();
            await RefreshNowAsync();
        };
    }

    private void ConfigureTrayIcon()
    {
        _trayIconService.ToggleWindowRequested += (_, _) => ToggleVisibility();
        _trayIconService.RefreshRequested += async (_, _) => await RefreshNowAsync();
        _trayIconService.ExitRequested += (_, _) => ExitApplication();
    }

    private void ConfigureFileWatcher()
    {
        if (!_locator.SessionsDirectoryExists())
        {
            return;
        }

        try
        {
            _watcher = new FileSystemWatcher(_locator.SessionsPath, "*.jsonl")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                InternalBufferSize = 65536
            };
            _watcher.Changed += (_, _) => ScheduleDebouncedRefresh();
            _watcher.Created += (_, _) => ScheduleDebouncedRefresh();
            _watcher.Renamed += (_, _) => ScheduleDebouncedRefresh();
            _watcher.Error += (_, _) =>
            {
                // 缓冲区溢出等错误时静默忽略，定时器会兜底刷新
            };
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception)
        {
            // 文件监视器设置失败不影响主流程，定时器兜底
        }
    }

    private void ScheduleDebouncedRefresh()
    {
        Dispatcher.Invoke(() =>
        {
            _fileChangeDebounceTimer.Stop();
            _fileChangeDebounceTimer.Start();
        });
    }

    private async Task RefreshNowAsync()
    {
        try
        {
            var result = await _quotaService.ReadLatestAsync();
            if (result.Status == QuotaReadStatus.Success && result.Snapshot is not null)
            {
                _currentSnapshot = result.Snapshot;
                RenderSnapshot(result.Snapshot, result.SourceFile);
                return;
            }

            RenderEmptyState(result.Message);
        }
        catch (OperationCanceledException)
        {
            // 取消不处理
        }
        catch (Exception)
        {
            RenderEmptyState("读取异常，稍后重试");
        }
    }

    private void RenderSnapshot(QuotaSnapshot snapshot, string? sourceFile)
    {
        var isStale = TimeFormatter.IsStale(snapshot.ObservedAt, TimeSpan.FromHours(12));
        StatusTextBlock.Foreground = isStale
            ? PercentageHelper.BrushForRemaining(40)
            : PercentageHelper.BrushForRemaining(90);

        RenderQuota(
            FiveHourPercentTextBlock,
            FiveHourPercentPill,
            FiveHourResetTextBlock,
            snapshot.FiveHourRemainingPercent,
            snapshot.FiveHourResetAt,
            TimeFormatter.FormatFiveHourReset(snapshot.FiveHourResetAt));

        RenderQuota(
            WeeklyPercentTextBlock,
            WeeklyPercentPill,
            WeeklyResetTextBlock,
            snapshot.WeeklyRemainingPercent,
            snapshot.WeeklyResetAt,
            TimeFormatter.FormatWeeklyReset(snapshot.WeeklyResetAt));

        ObservedTextBlock.Text = TimeFormatter.FormatObservedAge(snapshot.ObservedAt);
        _trayIconService.UpdateTooltip($"CodexQuota - 5小时 {PercentageHelper.FormatRemaining(snapshot.FiveHourRemainingPercent)} / 本周 {PercentageHelper.FormatRemaining(snapshot.WeeklyRemainingPercent)}");
    }

    private static void RenderQuota(
        System.Windows.Controls.TextBlock percentTextBlock,
        System.Windows.Controls.Border percentPill,
        System.Windows.Controls.TextBlock resetTextBlock,
        double? remainingPercent,
        DateTimeOffset? resetAt,
        string resetText)
    {
        percentTextBlock.Text = PercentageHelper.FormatRemaining(remainingPercent);
        percentTextBlock.Foreground = PercentageHelper.BrushForRemaining(remainingPercent);
        percentPill.Background = PercentageHelper.PillBackgroundForRemaining(remainingPercent);

        if (!resetAt.HasValue)
        {
            resetTextBlock.Text = "截止时间未知";
            return;
        }

        var localReset = resetAt.Value.ToLocalTime();
        if (localReset <= DateTimeOffset.Now)
        {
            resetTextBlock.Text = $"{resetText}（待刷新）";
            return;
        }

        resetTextBlock.Text = resetText;
    }

    private void RenderEmptyState(string message)
    {
        StatusTextBlock.Foreground = PercentageHelper.BrushForRemaining(null);
        FiveHourPercentTextBlock.Text = "--";
        FiveHourPercentTextBlock.Foreground = PercentageHelper.BrushForRemaining(null);
        FiveHourPercentPill.Background = System.Windows.Media.Brushes.Transparent;
        FiveHourResetTextBlock.Text = message;
        WeeklyPercentTextBlock.Text = "--";
        WeeklyPercentTextBlock.Foreground = PercentageHelper.BrushForRemaining(null);
        WeeklyPercentPill.Background = System.Windows.Media.Brushes.Transparent;
        WeeklyResetTextBlock.Text = "暂无 Codex 额度";
        ObservedTextBlock.Text = "未绑定";
        _trayIconService.UpdateTooltip("CodexQuota - 等待额度记录");
    }

    private void ToggleVisibility()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            Activate();
        }
    }

    private void SaveWindowPlacement()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.AlwaysOnTop = Topmost;
        _settings.LockPosition = LockPositionMenuItem.IsChecked;
        _settings.StartWithWindows = _startupService.IsEnabled();
        _settingsService.Save(_settings);
    }

    private void ExitApplication()
    {
        _isExiting = true;
        SaveWindowPlacement();
        System.Windows.Application.Current.Shutdown();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (LockPositionMenuItem.IsChecked)
        {
            return;
        }

        DragMove();
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        SaveWindowPlacement();
    }

    private async void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await RefreshNowAsync();
    }

    private void AlwaysOnTopMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Topmost = AlwaysOnTopMenuItem.IsChecked;
        SaveWindowPlacement();
    }

    private void LockPositionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SaveWindowPlacement();
    }

    private void StartWithWindowsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _startupService.SetEnabled(StartWithWindowsMenuItem.IsChecked);
        SaveWindowPlacement();
    }

    private void OpenLogsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(_locator.SessionsPath))
        {
            System.Windows.MessageBox.Show(
                "还没有找到 Codex sessions 目录。",
                "CodexQuota",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = _locator.SessionsPath,
            UseShellExecute = true
        });
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "CodexQuota\n\n纯本地读取 Codex sessions 中的 token_count.rate_limits。\n不读取 auth.json，不联网，不上传日志。",
            "关于 CodexQuota",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExitApplication();
    }
}
