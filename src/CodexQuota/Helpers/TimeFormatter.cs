namespace CodexQuota.Helpers;

public static class TimeFormatter
{
    public static string FormatFiveHourReset(DateTimeOffset? resetAt)
    {
        return FormatResetDeadline(resetAt, includeWeekday: false);
    }

    public static string FormatWeeklyReset(DateTimeOffset? resetAt)
    {
        return FormatResetDeadline(resetAt, includeWeekday: true);
    }

    public static string FormatObservedAge(DateTimeOffset? observedAt)
    {
        if (!observedAt.HasValue)
        {
            return "日志时间未知";
        }

        var localObservedAt = observedAt.Value.ToLocalTime();
        return localObservedAt.Date == DateTimeOffset.Now.Date
            ? $"日志 {localObservedAt:HH:mm}"
            : $"日志 {localObservedAt:MM-dd HH:mm}";
    }

    public static bool IsStale(DateTimeOffset? observedAt, TimeSpan staleAfter)
    {
        return observedAt.HasValue && DateTimeOffset.Now - observedAt.Value > staleAfter;
    }

    private static string FormatResetDeadline(DateTimeOffset? resetAt, bool includeWeekday)
    {
        if (!resetAt.HasValue)
        {
            return "截止时间未知";
        }

        var localReset = resetAt.Value.ToLocalTime();

        if (includeWeekday)
        {
            var day = localReset.ToString("ddd", new System.Globalization.CultureInfo("zh-CN"));
            return $"截止到 {day} {localReset:HH:mm}";
        }

        if (localReset.Date == DateTimeOffset.Now.Date)
        {
            return $"截止到 {localReset:HH:mm}";
        }

        return $"截止到 {localReset:MM-dd HH:mm}";
    }
}
