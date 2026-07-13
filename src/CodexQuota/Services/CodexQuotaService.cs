using System.IO;
using CodexQuota.Models;

namespace CodexQuota.Services;

public sealed class CodexQuotaService
{
    private readonly CodexLogLocator _locator;
    private readonly CodexQuotaParser _parser;

    public CodexQuotaService(CodexLogLocator locator, CodexQuotaParser parser)
    {
        _locator = locator;
        _parser = parser;
    }

    public Task<QuotaReadResult> ReadLatestAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ReadLatest(cancellationToken), cancellationToken);
    }

    private QuotaReadResult ReadLatest(CancellationToken cancellationToken)
    {
        if (!_locator.SessionsDirectoryExists())
        {
            return QuotaReadResult.MissingSessions(_locator.SessionsPath);
        }

        var recentFiles = _locator.GetRecentJsonlFiles(maxFiles: 20);
        if (recentFiles.Count == 0)
        {
            return QuotaReadResult.NoJsonlFiles(_locator.SessionsPath);
        }

        var merged = new QuotaSnapshot();
        string? firstSourceFile = null;

        foreach (var file in recentFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<string> lines;
            try
            {
                lines = new List<string>(capacity: 64);
                using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                while (reader.ReadLine() is { } line)
                {
                    lines.Add(line);
                }
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            var snapshot = _parser.TryParseLatestSnapshot(lines);
            if (snapshot?.HasAnyQuota == true)
            {
                firstSourceFile ??= file.FullName;
                MergeMissingQuotaWindows(merged, snapshot);
                if (merged.HasWeeklyQuota)
                {
                    return QuotaReadResult.Success(merged, firstSourceFile);
                }
            }
        }

        return merged.HasAnyQuota && firstSourceFile is not null
            ? QuotaReadResult.Success(merged, firstSourceFile)
            : QuotaReadResult.NoQuotaFound();
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
}
