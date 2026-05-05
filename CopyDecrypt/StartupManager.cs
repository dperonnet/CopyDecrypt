using Microsoft.Win32;

namespace CopyDecrypt;

internal static class StartupManager
{
    private const string RunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CopyDecrypt";

    internal static void Apply(bool enabled)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            return;

        using var key = Registry.CurrentUser.OpenSubKey(RunSubKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunSubKey);

        if (key is null)
            return;

        if (enabled)
            key.SetValue(ValueName, '"' + exe + '"');
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
