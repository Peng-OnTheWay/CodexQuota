using System.Diagnostics;

namespace CodexQuota.Services;

public sealed class ChatGptProcessService
{
    public bool IsRunning()
    {
        try
        {
            return Process.GetProcessesByName("ChatGPT").Length > 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
