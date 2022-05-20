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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
