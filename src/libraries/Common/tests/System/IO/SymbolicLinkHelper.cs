using System;
using System.Diagnostics;
using System.IO;
using Xunit;

public static class SymbolicLinkHelper
{
    /// <summary>
    /// In some cases (such as when running without elevated privileges),
    /// the symbolic link may fail to create. Only run this test if it creates
    /// links successfully.
    /// </summary>
    internal static bool CanCreateSymbolicLinks => s_canCreateSymbolicLinks.Value;

    private static readonly Lazy<bool> s_canCreateSymbolicLinks = new Lazy<bool>(() =>
    {
        bool success = true;

        // Verify file symlink creation
        string path = Path.GetTempFileName();
        string linkPath = path + ".link";
        success = CreateSymbolicLink(linkPath: linkPath, targetPath: path, isDirectory: false);
        try { File.Delete(path); } catch { }
        try { File.Delete(linkPath); } catch { }

        // Verify directory symlink creation
        path = Path.GetTempFileName();
        linkPath = path + ".link";
        success = success && CreateSymbolicLink(linkPath: linkPath, targetPath: path, isDirectory: true);
        try { Directory.Delete(path); } catch { }
        try { Directory.Delete(linkPath); } catch { }

        // Reduce the risk we accidentally stop running these altogether
        // on Windows, due to a bug in CreateSymbolicLink
        if (!success && PlatformDetection.IsWindows)
            Assert.True(!PlatformDetection.IsWindowsAndElevated);

        return success;
    });

    /// <summary>Creates a symbolic link using command line tools.</summary>
    public static bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
    {
        // It's easy to get the parameters backwards.
        Assert.EndsWith(".link", linkPath);
        if (linkPath != targetPath) // testing loop
            Assert.False(targetPath.EndsWith(".link"), $"{targetPath} should not end with .link");

        if (PlatformDetection.IsiOS || PlatformDetection.IstvOS || PlatformDetection.IsMacCatalyst || PlatformDetection.IsBrowser)
        {
            return false;
        }

#if !NETFRAMEWORK
        try
        {
            FileSystemInfo linkInfo = isDirectory ?
                Directory.CreateSymbolicLink(linkPath, targetPath) : File.CreateSymbolicLink(linkPath, targetPath);

            // Detect silent failures.
            Assert.True(linkInfo.Exists);
            return true;
        }
        catch
        {
            return false;
        }
#else
        Process symLinkProcess = new Process();

        symLinkProcess.StartInfo.FileName = "cmd";
        symLinkProcess.StartInfo.Arguments = string.Format("/c mklink{0} \"{1}\" \"{2}\"", isDirectory ? " /D" : "", linkPath, targetPath);
        symLinkProcess.StartInfo.UseShellExecute = false;
        symLinkProcess.StartInfo.RedirectStandardOutput = true;

        symLinkProcess.Start();

        symLinkProcess.WaitForExit();

        return (symLinkProcess.ExitCode == 0);
#endif
    }
}
