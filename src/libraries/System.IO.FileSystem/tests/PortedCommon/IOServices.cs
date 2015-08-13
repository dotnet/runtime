// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    public static PathInfo GetPath(int characterCount, bool extended = false)
    {
        string root = Path.GetTempPath();
        if (extended)
            root = IOInputs.ExtendedPrefix + root;
        return GetPath(root, characterCount);
    }

    public static PathInfo GetExtendedPath(int characterCount)
    {
        return GetPath(characterCount, extended: true);
    }

    public static PathInfo GetPath(string rootPath, int characterCount, int maxComponent = IOInputs.MaxComponent)
    {
        List<string> paths = new List<string>();
        rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        StringBuilder path = new StringBuilder(characterCount);
        path.Append(rootPath);

        while (path.Length < characterCount)
        {
            path.Append(Path.DirectorySeparatorChar);
            if (path.Length == characterCount)
                break;

            // Continue adding guids until the character count is hit
            string guid = Guid.NewGuid().ToString();
            path.Append(guid.Substring(0, Math.Min(characterCount - path.Length, guid.Length)));
            paths.Add(path.ToString());
        }
        Assert.Equal(path.Length, characterCount);

        return new PathInfo(paths.ToArray());
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

    private static string GetDriveFormat(string driveName)
    {
        const int volNameLen = 50;
        StringBuilder volumeName = new StringBuilder(volNameLen);
        const int fileSystemNameLen = 50;
        StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
        int serialNumber, maxFileNameLen, fileSystemFlags;

        bool r = DllImports.GetVolumeInformation(driveName, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
        if (!r)
        {
            throw new IOException("DriveName: " + driveName + " ErrorCode:" + Marshal.GetLastWin32Error());
        }

        return fileSystemName.ToString();
    }

    public static string GetCurrentDrive()
    {
        return Path.GetPathRoot(Directory.GetCurrentDirectory());
    }

    public static bool IsDriveNTFS(string drive)
    {
#if TEST_WINRT
        // we cannot determine filesystem so assume NTFS
        return true;
#else
        return GetDriveFormat(drive) == "NTFS";
#endif
    }

    public static long GetAvailableFreeBytes(string drive)
    {
        long ignored;
        long userBytes;
        if (!DllImports.GetDiskFreeSpaceEx(drive, out userBytes, out ignored, out ignored))
        {
            throw new IOException("DriveName: " + drive + " ErrorCode:" + Marshal.GetLastWin32Error());
        }

        return userBytes;
    }

    private static bool IsReady(string drive)
    {
        const int ERROR_NOT_READY = 0x00000015;

        long ignored;
        if (!DllImports.GetDiskFreeSpaceEx(drive, out ignored, out ignored, out ignored))
        {
            return Marshal.GetLastWin32Error() != ERROR_NOT_READY;
        }

        return true;
    }

    private static bool IsFixed(string drive)
    {
        return DllImports.GetDriveType(drive) == 3;
    }
}

