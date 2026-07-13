namespace CodexQuota.Helpers;

public static class TimeFormatter
{
    public static string FormatFiveHourReset(DateTimeOffset? resetAt)
    {
        return FormatResetDeadline(resetAt, includeWeekday: false);
    }

    public static string FormatWeeklyReset(DateTimeOffset? resetAt)
    {
        if (!resetAt.HasValue)
        {
            return "截止时间未知";
        }

        var localReset = resetAt.Value.ToLocalTime();
        return $"截止到 {localReset.Month}月{localReset.Day}日";
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

    public static string FormatCompactLogTime(DateTimeOffset? observedAt)
    {
        if (!observedAt.HasValue)
        {
            return "--";
        }

        var localObservedAt = observedAt.Value.ToLocalTime();
        var elapsed = DateTimeOffset.Now - localObservedAt;

        if (elapsed <= TimeSpan.FromMinutes(1))
        {
            return "刚刚";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes))}分钟前";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalHours))}小时前";
        }

        return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalDays))}天前";
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
