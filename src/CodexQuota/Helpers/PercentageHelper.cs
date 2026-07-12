namespace CodexQuota.Helpers;

public static class PercentageHelper
{
    public static string FormatRemaining(double? remainingPercent)
    {
        return remainingPercent.HasValue
            ? $"{Math.Round(remainingPercent.Value):0}%"
            : "暂无数据";
    }

    public static double ProgressValue(double? remainingPercent)
    {
        return remainingPercent.HasValue
            ? Math.Clamp(remainingPercent.Value, 0d, 100d)
            : 0d;
    }

    public static System.Windows.Media.Brush BrushForRemaining(double? remainingPercent)
    {
        if (!remainingPercent.HasValue)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
        }

        return remainingPercent.Value switch
        {
            >= 60 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
            >= 30 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 179, 8)),
            _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68))
        };
    }

    public static System.Windows.Media.Brush PillBackgroundForRemaining(double? remainingPercent)
    {
        if (!remainingPercent.HasValue || remainingPercent.Value >= 30)
        {
            return System.Windows.Media.Brushes.Transparent;
        }

        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(38, 239, 68, 68));
    }
}
