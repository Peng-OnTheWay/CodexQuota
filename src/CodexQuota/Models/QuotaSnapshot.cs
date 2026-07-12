namespace CodexQuota.Models;

public sealed class QuotaSnapshot
{
    public double? FiveHourUsedPercent { get; set; }
    public double? WeeklyUsedPercent { get; set; }

    public DateTimeOffset? FiveHourResetAt { get; set; }
    public DateTimeOffset? WeeklyResetAt { get; set; }

    public DateTimeOffset? ObservedAt { get; set; }

    public double? FiveHourRemainingPercent => RemainingFromUsed(FiveHourUsedPercent);
    public double? WeeklyRemainingPercent => RemainingFromUsed(WeeklyUsedPercent);

    public bool HasAnyQuota =>
        FiveHourUsedPercent.HasValue ||
        WeeklyUsedPercent.HasValue ||
        FiveHourResetAt.HasValue ||
        WeeklyResetAt.HasValue;

    private static double? RemainingFromUsed(double? usedPercent)
    {
        if (!usedPercent.HasValue)
        {
            return null;
        }

        return Math.Clamp(100d - usedPercent.Value, 0d, 100d);
    }
}
