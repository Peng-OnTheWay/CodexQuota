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
                return QuotaReadResult.Success(snapshot, file.FullName);
            }
        }

        return QuotaReadResult.NoQuotaFound();
    }
}
