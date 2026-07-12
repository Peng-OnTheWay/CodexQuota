using System.Drawing;
using System.Windows.Forms;

namespace CodexQuota.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public TrayIconService()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示/隐藏", null, (_, _) => ToggleWindowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("立即刷新", null, (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "CodexQuota",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ToggleWindowRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? ExitRequested;

    public void UpdateTooltip(string tooltip)
    {
        _notifyIcon.Text = tooltip.Length <= 63 ? tooltip : tooltip[..63];
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _disposed = true;
    }
}
