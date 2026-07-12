using System.IO;

namespace CodexQuota.Services;

public sealed class StartupService
{
    private const string StartupFileName = "CodexQuota.cmd";

    private static string StartupFolder =>
        Environment.GetFolderPath(Environment.SpecialFolder.Startup);

    private static string StartupFilePath =>
        Path.Combine(StartupFolder, StartupFileName);

    public bool IsEnabled() => File.Exists(StartupFilePath);

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            Enable();
        }
        else
        {
            Disable();
        }
    }

    private static void Enable()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        Directory.CreateDirectory(StartupFolder);
        var script = $"""
            @echo off
            start "" "{executablePath}"
            """;
        File.WriteAllText(StartupFilePath, script);
    }

    private static void Disable()
    {
        if (File.Exists(StartupFilePath))
        {
            File.Delete(StartupFilePath);
        }
    }
}
