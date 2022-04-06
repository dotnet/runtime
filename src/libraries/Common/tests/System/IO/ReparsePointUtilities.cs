// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
This is meant to contain useful utilities for IO related work in ReparsePoints
 - MountVolume
 - Encryption
**/
#define TRACE
#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public static partial class MountHelper
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetVolumeNameForVolumeMountPointW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetVolumeNameForVolumeMountPoint(string volumeName, char[] uniqueVolumeName, int uniqueNameBufferCapacity);
    // unique volume name must be "\\?\Volume{GUID}\"
    [LibraryImport("kernel32.dll", EntryPoint = "SetVolumeMountPointW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetVolumeMountPoint(string mountPoint, string uniqueVolumeName);
    [LibraryImport("kernel32.dll", EntryPoint = "DeleteVolumeMountPointW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteVolumeMountPoint(string mountPoint);

    // Helper for ConditionalClass attributes
    internal static bool IsSubstAvailable => PlatformDetection.IsSubstAvailable;

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

#if NETFRAMEWORK
        bool isWindows = true;
#else
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsBrowser()) // OSes that don't support Process.Start()
        {
            return false;
        }

        bool isWindows = OperatingSystem.IsWindows();
#endif
        Process symLinkProcess = new Process();
        if (isWindows)
        {
            symLinkProcess.StartInfo.FileName = "cmd";
            symLinkProcess.StartInfo.Arguments = string.Format("/c mklink{0} \"{1}\" \"{2}\"", isDirectory ? " /D" : "", linkPath, targetPath);
        }
        else
        {
            symLinkProcess.StartInfo.FileName = "/bin/ln";
            symLinkProcess.StartInfo.Arguments = string.Format("-s \"{0}\" \"{1}\"", targetPath, linkPath);
        }
        symLinkProcess.StartInfo.UseShellExecute = false;
        symLinkProcess.StartInfo.RedirectStandardOutput = true;

        symLinkProcess.Start();

        symLinkProcess.WaitForExit();

        return (symLinkProcess.ExitCode == 0);
    }

    /// <summary>On Windows, creates a junction using command line tools.</summary>
    public static bool CreateJunction(string junctionPath, string targetPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        return RunProcess(CreateProcessStartInfo("cmd", "/c", "mklink", "/J", junctionPath, targetPath));
    }

    public static void Mount(string volumeName, string mountPoint)
    {
        if (volumeName[volumeName.Length - 1] != Path.DirectorySeparatorChar)
            volumeName += Path.DirectorySeparatorChar;
        if (mountPoint[mountPoint.Length - 1] != Path.DirectorySeparatorChar)
            mountPoint += Path.DirectorySeparatorChar;

        Console.WriteLine(string.Format("Mounting volume {0} at {1}", volumeName, mountPoint));
        bool r;
        char[] sb = new char[1024];
        r = GetVolumeNameForVolumeMountPoint(volumeName, sb, sb.Length);
        if (!r)
            throw new Exception(string.Format("Win32 error: {0}", Marshal.GetLastPInvokeError()));

        string uniqueName = new string(sb);
        Console.WriteLine(string.Format("uniqueName: <{0}>", uniqueName));
        r = SetVolumeMountPoint(mountPoint, uniqueName);
        if (!r)
            throw new Exception(string.Format("Win32 error: {0}", Marshal.GetLastPInvokeError()));
        Task.Delay(100).Wait(); // adding sleep for the file system to settle down so that reparse point mounting works
    }

    public static void Unmount(string mountPoint)
    {
        if (mountPoint[mountPoint.Length - 1] != Path.DirectorySeparatorChar)
            mountPoint += Path.DirectorySeparatorChar;
        Console.WriteLine(string.Format("Unmounting the volume at {0}", mountPoint));

        bool r = DeleteVolumeMountPoint(mountPoint);
        if (!r)
            throw new Exception(string.Format("Win32 error: {0}", Marshal.GetLastPInvokeError()));
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, params string[] arguments)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return info;
    }

    private static bool RunProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo);
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    /// For standalone debugging help. Change Main0 to Main
    public static void Main0(string[] args)
    {
         try
        {
            if (args[0]=="-m")
                Mount(args[1], args[2]);
            if (args[0]=="-u")
                Unmount(args[1]);
         }
        catch (Exception ex)
        {
             Console.WriteLine(ex);
        }
    }

}
