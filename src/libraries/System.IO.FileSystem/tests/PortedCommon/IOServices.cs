// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

internal class IOServices
{
    public static IEnumerable<string> GetReadyDrives()
    {
        foreach (string drive in GetLogicalDrives())
        {
            if (IsReady(drive))
                yield return drive;
        }
    }

    public static string GetNotReadyDrive()
    {
        string[] drives = GetLogicalDrives();
        foreach (string drive in drives)
        {
            if (!IsReady(drive))
                return drive;
        }

        return null;
    }

    public static string GetNonExistentDrive()
    {
        string[] availableDrives = GetLogicalDrives();

        for (char drive = 'A'; drive <= 'Z'; drive++)
        {
            if (!availableDrives.Contains(drive + @":\"))
                return drive + @":\";
        }

        return null;
    }

    public static string GetNtfsDriveOtherThanCurrent()
    {
        return GetNtfsDriveOtherThan(GetCurrentDrive());
    }

    public static string GetNtfsDriveOtherThan(string drive)
    {
        foreach (string otherDrive in GetLogicalDrives())
        {
            if (string.Equals(drive, otherDrive, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsFixed(otherDrive))
                continue;

            if (!IsReady(otherDrive))
                continue;

            if (IsDriveNTFS(otherDrive))
                return otherDrive;
        }

        return null;
    }

    public static string GetNonNtfsDriveOtherThanCurrent()
    {
        return GetNonNtfsDriveOtherThan(GetCurrentDrive());
    }

    public static string GetNonNtfsDriveOtherThan(string drive)
    {
        foreach (string otherDrive in GetLogicalDrives())
        {
            if (string.Equals(drive, otherDrive, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsReady(otherDrive))
                continue;

            if (!IsDriveNTFS(otherDrive))
                return otherDrive;
        }

        return null;
    }

    public static string GetPath(string rootPath, int characterCount, bool extended)
    {
        if (extended)
            rootPath = IOInputs.ExtendedPrefix + rootPath;
        return GetPath(rootPath, characterCount);
    }

    public static string GetPath(string rootPath, int characterCount)
    {
        if (rootPath.Length > characterCount)
        {
            // don't return immediately as the path might be ending with directory separator now
            rootPath = rootPath.Substring(0, characterCount);
        }

        rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        StringBuilder path = new StringBuilder(characterCount);
        path.Append(rootPath);

        while (path.Length < characterCount)
        {
            // Add directory separator after each dir but not at the end of the path
            path.Append(Path.DirectorySeparatorChar);

            // Continue adding unique path segments until the character count is hit
            int remainingChars = characterCount - path.Length;
            string guid = Guid.NewGuid().ToString("N"); // No dashes
            if (remainingChars < guid.Length)
            {
                path.Append(guid.Substring(0, remainingChars));
            }
            else
            {
                // Long paths can be over 32K characters. Given that a guid is just 36 chars, this
                // can lead to 800+ recursive call depths. We'll create large segments to
                // make tests more manageable.

                path.Append(guid);
                remainingChars = characterCount - path.Length;
                path.Append('g', Math.Min(remainingChars, 200));
            }

            if (path.Length + 1 == characterCount)
            {
                // If only one character is missing add a k!
                path.Append('k');
            }
        }

        Assert.Equal(characterCount, path.Length);

        return path.ToString();
    }

    public static IEnumerable<string> CreateDirectories(string rootPath, params string[] names)
    {
        List<string> paths = new List<string>();

        foreach (string name in names)
        {
            string path = Path.Combine(rootPath, name);

            Directory.CreateDirectory(path);

            paths.Add(path);
        }

        return paths;
    }

    public static IEnumerable<string> CreateFiles(string rootPath, params string[] names)
    {
        List<string> paths = new List<string>();

        foreach (string name in names)
        {
            string path = Path.Combine(rootPath, name);

            FileStream stream = File.Create(path);
            stream.Dispose();

            paths.Add(path);
        }

        return paths;
    }

    public static string AddTrailingSlashIfNeeded(string path)
    {
        if (path.Length > 0 && path[path.Length - 1] != Path.DirectorySeparatorChar && path[path.Length - 1] != Path.AltDirectorySeparatorChar)
        {
            path = path + Path.DirectorySeparatorChar;
        }

        return path;
    }

    public static string RemoveTrailingSlash(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string[] GetLogicalDrives()
    {   // From .NET Framework's Directory.GetLogicalDrives
        int drives = DllImports.GetLogicalDrives();
        if (drives == 0)
            throw new InvalidOperationException();

        uint d = (uint)drives;
        int count = 0;
        while (d != 0)
        {
            if (((int)d & 1) != 0) count++;
            d >>= 1;
        }
        string[] result = new string[count];
        char[] root = new char[] { 'A', ':', '\\' };
        d = (uint)drives;
        count = 0;
        while (d != 0)
        {
            if (((int)d & 1) != 0)
            {
                result[count++] = new string(root);
            }
            d >>= 1;
            root[0]++;
        }

        return result;
    }

    public static string GetCurrentDrive()
    {
        return Path.GetPathRoot(Directory.GetCurrentDirectory());
    }

    public static bool IsDriveNTFS(string drive)
    {
        if (PlatformDetection.IsInAppContainer)
        {
            // we cannot determine filesystem so assume NTFS
            return true;
        }

        var di = new DriveInfo(drive);

        return string.Equals(di.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
    }

    public static long GetAvailableFreeBytes(string drive)
    {
        long ignored;
        long userBytes;
        if (!DllImports.GetDiskFreeSpaceEx(drive, out userBytes, out ignored, out ignored))
        {
            throw new IOException("DriveName: " + drive + " ErrorCode:" + Marshal.GetLastPInvokeError());
        }

        return userBytes;
    }

    private static bool IsReady(string drive)
    {
        const int ERROR_NOT_READY = 0x00000015;

        long ignored;
        if (!DllImports.GetDiskFreeSpaceEx(drive, out ignored, out ignored, out ignored))
        {
            return Marshal.GetLastPInvokeError() != ERROR_NOT_READY;
        }

        return true;
    }

    private static bool IsFixed(string drive)
    {
        return DllImports.GetDriveType(drive) == 3;
    }
}
