using System.IO;

namespace CodexQuota.Services;

public sealed class CodexLogLocator
{
    public string SessionsPath { get; }

    public CodexLogLocator()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        SessionsPath = Path.Combine(userProfile, ".codex", "sessions");
    }

    public bool SessionsDirectoryExists() => Directory.Exists(SessionsPath);

    public IReadOnlyList<FileInfo> GetRecentJsonlFiles(int maxFiles = 20)
    {
        if (!SessionsDirectoryExists())
        {
            return Array.Empty<FileInfo>();
        }

        try
        {
            return new DirectoryInfo(SessionsPath)
                .EnumerateFiles("*.jsonl", SearchOption.AllDirectories)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(maxFiles)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<FileInfo>();
        }
    }
}
