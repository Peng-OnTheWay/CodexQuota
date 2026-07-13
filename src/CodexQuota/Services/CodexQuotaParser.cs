using System.Text.Json;
using CodexQuota.Models;

namespace CodexQuota.Services;

public sealed class CodexQuotaParser
{
    private const int FiveHourWindowMinutes = 300;
    private const int WeeklyWindowMinutes = 10080;

    public QuotaSnapshot? TryParseLatestSnapshot(IEnumerable<string> jsonlLines)
    {
        var lines = jsonlLines as IList<string> ?? jsonlLines.ToList();
        var merged = new QuotaSnapshot();

        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("token_count", StringComparison.Ordinal))
            {
                continue;
            }

            var snapshot = TryParseLine(line);
            if (snapshot?.HasAnyQuota == true)
            {
                MergeMissingQuotaWindows(merged, snapshot);
                if (merged.HasBothQuotaWindows)
                {
                    return merged;
                }
            }
        }

        return merged.HasAnyQuota ? merged : null;
    }

    public QuotaSnapshot? TryParseLine(string jsonLine)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;

            if (!IsTokenCountEvent(root, out var payload))
            {
                return null;
            }

            if (!payload.TryGetProperty("rate_limits", out var rateLimits) ||
                rateLimits.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var snapshot = new QuotaSnapshot
            {
                ObservedAt = ReadObservedAt(root)
            };

            foreach (var rateLimitProperty in rateLimits.EnumerateObject())
            {
                var rateLimit = rateLimitProperty.Value;
                if (rateLimit.ValueKind != JsonValueKind.Object ||
                    !TryReadInt(rateLimit, "window_minutes", out var windowMinutes))
                {
                    continue;
                }

                TryReadDouble(rateLimit, "used_percent", out var usedPercent);
                var resetAt = ReadResetAt(rateLimit);

                if (windowMinutes == FiveHourWindowMinutes)
                {
                    snapshot.FiveHourUsedPercent = usedPercent;
                    snapshot.FiveHourResetAt = resetAt;
                    snapshot.FiveHourObservedAt = snapshot.ObservedAt;
                }
                else if (windowMinutes == WeeklyWindowMinutes)
                {
                    snapshot.WeeklyUsedPercent = usedPercent;
                    snapshot.WeeklyResetAt = resetAt;
                    snapshot.WeeklyObservedAt = snapshot.ObservedAt;
                }
            }

            return snapshot.HasAnyQuota ? snapshot : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void MergeMissingQuotaWindows(QuotaSnapshot target, QuotaSnapshot source)
    {
        target.ObservedAt = NewerOf(target.ObservedAt, source.ObservedAt);

        if (!target.HasFiveHourQuota && source.HasFiveHourQuota)
        {
            target.FiveHourUsedPercent = source.FiveHourUsedPercent;
            target.FiveHourResetAt = source.FiveHourResetAt;
            target.FiveHourObservedAt = source.FiveHourObservedAt ?? source.ObservedAt;
        }

        if (!target.HasWeeklyQuota && source.HasWeeklyQuota)
        {
            target.WeeklyUsedPercent = source.WeeklyUsedPercent;
            target.WeeklyResetAt = source.WeeklyResetAt;
            target.WeeklyObservedAt = source.WeeklyObservedAt ?? source.ObservedAt;
        }
    }

    private static DateTimeOffset? NewerOf(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return left.Value >= right.Value ? left : right;
    }

    private static bool IsTokenCountEvent(JsonElement root, out JsonElement payload)
    {
        payload = default;
        if (!root.TryGetProperty("type", out var type) ||
            type.GetString() != "event_msg" ||
            !root.TryGetProperty("payload", out payload) ||
            payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("type", out var payloadType))
        {
            return false;
        }

        return payloadType.GetString() == "token_count";
    }

    private static DateTimeOffset? ReadObservedAt(JsonElement root)
    {
        foreach (var propertyName in new[] { "timestamp", "time", "created_at" })
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), out var parsed))
            {
                return parsed.ToLocalTime();
            }

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out var timestamp))
            {
                return timestamp > 10_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToLocalTime()
                    : DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime();
            }
        }

        return null;
    }

    private static DateTimeOffset? ReadResetAt(JsonElement rateLimit)
    {
        if (!TryReadLong(rateLimit, "resets_at", out var resetTimestamp))
        {
            return null;
        }

        return resetTimestamp > 10_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(resetTimestamp).ToLocalTime()
            : DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).ToLocalTime();
    }

    private static bool TryReadInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value);
    }

    private static bool TryReadLong(JsonElement element, string propertyName, out long value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt64(out value);
    }

    private static bool TryReadDouble(JsonElement element, string propertyName, out double? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetDouble(out var parsed))
        {
            return false;
        }

        value = Math.Clamp(parsed, 0d, 100d);
        return true;
    }
}
