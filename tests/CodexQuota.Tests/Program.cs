using CodexQuota.Services;

var parser = new CodexQuotaParser();
var root = AppContext.BaseDirectory;

Run("normal.jsonl", snapshot =>
{
    Assert(snapshot is not null, "normal should parse");
    Assert(snapshot!.FiveHourUsedPercent == 51, "normal 5h used should be 51");
    Assert(snapshot.WeeklyUsedPercent == 8, "normal weekly used should be 8");
    Assert(snapshot.FiveHourRemainingPercent == 49, "normal 5h remaining should be 49");
    Assert(snapshot.WeeklyRemainingPercent == 92, "normal weekly remaining should be 92");
});

Run("missing-week.jsonl", snapshot =>
{
    Assert(snapshot is not null, "missing-week should parse");
    Assert(snapshot!.FiveHourUsedPercent == 51, "missing-week 5h used should be 51");
    Assert(snapshot.WeeklyUsedPercent is null, "missing-week weekly should be null");
});

Run("invalid-line.jsonl", snapshot =>
{
    Assert(snapshot is not null, "invalid-line should skip broken JSON");
    Assert(snapshot!.FiveHourUsedPercent == 52, "invalid-line should parse latest valid line");
});

Run("swapped-windows.jsonl", snapshot =>
{
    Assert(snapshot is not null, "swapped-windows should parse");
    Assert(snapshot!.FiveHourUsedPercent == 60, "5h should be detected by window_minutes=300");
    Assert(snapshot.WeeklyUsedPercent == 10, "weekly should be detected by window_minutes=10080");
});

Run("multi-record.jsonl", snapshot =>
{
    Assert(snapshot is not null, "multi-record should parse");
    Assert(snapshot!.FiveHourUsedPercent == 20, "multi-record should use the newest valid token_count line");
});

Console.WriteLine("All CodexQuota parser smoke tests passed.");

void Run(string fileName, Action<CodexQuota.Models.QuotaSnapshot?> assertion)
{
    var path = Path.Combine(root, "TestData", fileName);
    if (!File.Exists(path))
    {
        path = Path.Combine(FindRepositoryRoot(), "tests", "CodexQuota.Tests", "TestData", fileName);
    }

    var lines = File.ReadAllLines(path);
    assertion(parser.TryParseLatestSnapshot(lines));
}

string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "CodexQuota.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not find CodexQuota.sln.");
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
