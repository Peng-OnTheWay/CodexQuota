namespace CodexQuota.Models;

public sealed class QuotaReadResult
{
    private QuotaReadResult(QuotaReadStatus status, QuotaSnapshot? snapshot, string message, string? sourceFile)
    {
        Status = status;
        Snapshot = snapshot;
        Message = message;
        SourceFile = sourceFile;
    }

    public QuotaReadStatus Status { get; }
    public QuotaSnapshot? Snapshot { get; }
    public string Message { get; }
    public string? SourceFile { get; }

    public static QuotaReadResult Success(QuotaSnapshot snapshot, string sourceFile) =>
        new(QuotaReadStatus.Success, snapshot, "本地", sourceFile);

    public static QuotaReadResult MissingSessions(string sessionsPath) =>
        new(QuotaReadStatus.MissingSessions, null, $"未找到 Codex 本地数据：{sessionsPath}", null);

    public static QuotaReadResult NoJsonlFiles(string sessionsPath) =>
        new(QuotaReadStatus.NoJsonlFiles, null, $"等待 Codex 产生会话日志：{sessionsPath}", null);

    public static QuotaReadResult NoQuotaFound() =>
        new(QuotaReadStatus.NoQuotaFound, null, "等待 Codex 产生额度记录", null);
}

public enum QuotaReadStatus
{
    Success,
    MissingSessions,
    NoJsonlFiles,
    NoQuotaFound
}
