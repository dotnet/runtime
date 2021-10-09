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

public static class MountHelper
{
    [DllImport("kernel32.dll", EntryPoint = "GetVolumeNameForVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
    private static extern bool GetVolumeNameForVolumeMountPoint(string volumeName, StringBuilder uniqueVolumeName, int uniqueNameBufferCapacity);
    // unique volume name must be "\\?\Volume{GUID}\"
    [DllImport("kernel32.dll", EntryPoint = "SetVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
    private static extern bool SetVolumeMountPoint(string mountPoint, string uniqueVolumeName);
    [DllImport("kernel32.dll", EntryPoint = "DeleteVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
    private static extern bool DeleteVolumeMountPoint(string mountPoint);

    /// <summary>Creates a symbolic link using command line tools.</summary>
    public static bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
    {
        Process symLinkProcess = new Process();
        if (OperatingSystem.IsWindows())
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
        return symLinkProcess.ExitCode == 0;
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

    ///<summary>
    /// On Windows, mounts a folder to an assigned virtual drive letter using the subst command.
    /// subst is not available in Windows Nano.
    /// </summary>
    public static char CreateVirtualDrive(string targetDir)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        char driveLetter = GetNextAvailableDriveLetter();
        bool success = RunProcess(CreateProcessStartInfo("cmd", "/c", SubstPath, $"{driveLetter}:", targetDir));
        if (!success || !DriveInfo.GetDrives().Any(x => x.Name[0] == driveLetter))
        {
            throw new InvalidOperationException($"Could not create virtual drive {driveLetter}: with subst");
        }
        return driveLetter;

        // Finds the next unused drive letter and returns it.
        char GetNextAvailableDriveLetter()
        {
            List<char> existingDrives = DriveInfo.GetDrives().Select(x => x.Name[0]).ToList();

            // A,B are reserved, C is usually reserved
            IEnumerable<int> range = Enumerable.Range('D', 'Z' - 'D');
            IEnumerable<char> castRange = range.Select(x => Convert.ToChar(x));
            IEnumerable<char> allDrivesLetters = castRange.Except(existingDrives);

            if (!allDrivesLetters.Any())
            {
                throw new ArgumentOutOfRangeException("No drive letters available");
            }

            return allDrivesLetters.First();
        }
    }

    /// <summary>
    /// On Windows, unassigns the specified virtual drive letter from its mounted folder.
    /// </summary>
    public static void DeleteVirtualDrive(char driveLetter)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        bool success = RunProcess(CreateProcessStartInfo("cmd", "/c", SubstPath, "/d", $"{driveLetter}:"));
        if (!success || DriveInfo.GetDrives().Any(x => x.Name[0] == driveLetter))
        {
            throw new InvalidOperationException($"Could not delete virtual drive {driveLetter}: with subst");
        }
    }

    public static void Mount(string volumeName, string mountPoint)
    {
        if (volumeName[volumeName.Length - 1] != Path.DirectorySeparatorChar)
            volumeName += Path.DirectorySeparatorChar;
        if (mountPoint[mountPoint.Length - 1] != Path.DirectorySeparatorChar)
            mountPoint += Path.DirectorySeparatorChar;

        Console.WriteLine(string.Format("Mounting volume {0} at {1}", volumeName, mountPoint));
        bool r;
        StringBuilder sb = new StringBuilder(1024);
        r = GetVolumeNameForVolumeMountPoint(volumeName, sb, sb.Capacity);
        if (!r)
            throw new Exception(string.Format("Win32 error: {0}", Marshal.GetLastPInvokeError()));

        string uniqueName = sb.ToString();
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

    private static string SubstPath
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
            string system32 = Path.Join(systemRoot, "System32");
            return Path.Join(system32, "subst.exe");
        }
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
