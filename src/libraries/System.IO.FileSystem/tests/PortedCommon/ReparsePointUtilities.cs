// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**
This is meant to contain useful utilities for IO related work in ReparsePoints
 - MountVolume
 - Encryption
**/
#define TRACE
#define DEBUG

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
public static class MountHelper
{
    [DllImport("kernel32.dll", EntryPoint = "GetVolumeNameForVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
    private static extern bool GetVolumeNameForVolumeMountPoint(String volumeName, StringBuilder uniqueVolumeName, int uniqueNameBufferCapacity);
    // unique volume name must be "\\?\Volume{GUID}\"
    [DllImport("kernel32.dll", EntryPoint = "SetVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
    private static extern bool SetVolumeMountPoint(String mountPoint, String uniqueVolumeName);
    [DllImport("kernel32.dll", EntryPoint = "DeleteVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
    private static extern bool DeleteVolumeMountPoint(String mountPoint);

    /// <summary>Creates a symbolic link using command line tools</summary>
    /// <param name="linkPath">The existing file</param>
    /// <param name="targetPath"></param>
    public static bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
    {
        Process symLinkProcess = new Process();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

        if (symLinkProcess != null)
        {
            symLinkProcess.WaitForExit();
            return (0 == symLinkProcess.ExitCode);
        }
        else
        {
            return false;
        }
    }

    public static void Mount(String volumeName, String mountPoint)
    {

        if (volumeName[volumeName.Length - 1] != Path.DirectorySeparatorChar)
            volumeName += Path.DirectorySeparatorChar;
        if (mountPoint[mountPoint.Length - 1] != Path.DirectorySeparatorChar)
            mountPoint += Path.DirectorySeparatorChar;

        Console.WriteLine(String.Format("Mounting volume {0} at {1}", volumeName, mountPoint));
        bool r;
        StringBuilder sb = new StringBuilder(1024);
        r = GetVolumeNameForVolumeMountPoint(volumeName, sb, sb.Capacity);
        if (!r)
            throw new Exception(String.Format("Win32 error: {0}", Marshal.GetLastWin32Error()));

        String uniqueName = sb.ToString();
        Console.WriteLine(String.Format("uniqueName: <{0}>", uniqueName));
        r = SetVolumeMountPoint(mountPoint, uniqueName);
        if (!r)
            throw new Exception(String.Format("Win32 error: {0}", Marshal.GetLastWin32Error()));
        Task.Delay(100).Wait(); // adding sleep for the file system to settle down so that reparse point mounting works
    }

    public static void Unmount(String mountPoint)
    {
        if (mountPoint[mountPoint.Length - 1] != Path.DirectorySeparatorChar)
            mountPoint += Path.DirectorySeparatorChar;
        Console.WriteLine(String.Format("Unmounting the volume at {0}", mountPoint));

        bool r = DeleteVolumeMountPoint(mountPoint);
        if (!r)
            throw new Exception(String.Format("Win32 error: {0}", Marshal.GetLastWin32Error()));
    }

    /// For standalone debugging help. Change Main0 to Main
     public static void Main0(String[] args)
    {
	 	try
        {
			if(args[0]=="-m")
				Mount(args[1], args[2]);
			if(args[0]=="-u")
				Unmount(args[1]);
 		}
        catch(Exception ex)
        {
	 		Console.WriteLine(ex);
		}
    }	

}
