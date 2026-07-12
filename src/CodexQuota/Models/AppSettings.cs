namespace CodexQuota.Models;

public sealed class AppSettings
{
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double Opacity { get; set; } = 0.92;
    public int RefreshSeconds { get; set; } = 60;
    public bool AlwaysOnTop { get; set; } = true;
    public bool LockPosition { get; set; }
    public bool StartWithWindows { get; set; }
}
