// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string TimeZoneFileName = "zone.tab";
        private const string TimeZoneDirectoryEnvironmentVariable = "TZDIR";
        private const string TimeZoneEnvironmentVariable = "TZ";

        private static TimeZoneInfo GetLocalTimeZoneCore()
        {
            // Without Registry support, create the TimeZoneInfo from a TZ file
            return GetLocalTimeZoneFromTzFile();
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachineCore(string id, out TimeZoneInfo? value, out Exception? e)
        {
            value = null;
            e = null;

            string timeZoneDirectory = GetTimeZoneDirectory();
            string timeZoneFilePath = Path.Combine(timeZoneDirectory, id);
            byte[] rawData;
            try
            {
                rawData = File.ReadAllBytes(timeZoneFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                e = ex;
                return TimeZoneInfoResult.SecurityException;
            }
            catch (FileNotFoundException ex)
            {
                e = ex;
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }
            catch (DirectoryNotFoundException ex)
            {
                e = ex;
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }
            catch (IOException ex)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, timeZoneFilePath), ex);
                return TimeZoneInfoResult.InvalidTimeZoneException;
            }

            value = GetTimeZoneFromTzData(rawData, id);

            if (value == null)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, timeZoneFilePath));
                return TimeZoneInfoResult.InvalidTimeZoneException;
            }

            return TimeZoneInfoResult.Success;
        }

        /// <summary>
        /// Returns a collection of TimeZone Id values from the time zone file in the timeZoneDirectory.
        /// </summary>
        /// <remarks>
        /// Lines that start with # are comments and are skipped.
        /// </remarks>
        private static List<string> GetTimeZoneIds()
        {
            List<string> timeZoneIds = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(Path.Combine(GetTimeZoneDirectory(), TimeZoneFileName), Encoding.UTF8))
                {
                    string? zoneTabFileLine;
                    while ((zoneTabFileLine = sr.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(zoneTabFileLine) && zoneTabFileLine[0] != '#')
                        {
                            // the format of the line is "country-code \t coordinates \t TimeZone Id \t comments"

                            int firstTabIndex = zoneTabFileLine.IndexOf('\t');
                            if (firstTabIndex != -1)
                            {
                                int secondTabIndex = zoneTabFileLine.IndexOf('\t', firstTabIndex + 1);
                                if (secondTabIndex != -1)
                                {
                                    string timeZoneId;
                                    int startIndex = secondTabIndex + 1;
                                    int thirdTabIndex = zoneTabFileLine.IndexOf('\t', startIndex);
                                    if (thirdTabIndex != -1)
                                    {
                                        int length = thirdTabIndex - startIndex;
                                        timeZoneId = zoneTabFileLine.Substring(startIndex, length);
                                    }
                                    else
                                    {
                                        timeZoneId = zoneTabFileLine.Substring(startIndex);
                                    }

                                    if (!string.IsNullOrEmpty(timeZoneId))
                                    {
                                        timeZoneIds.Add(timeZoneId);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return timeZoneIds;
        }

        private static string? GetTzEnvironmentVariable()
        {
            string? result = Environment.GetEnvironmentVariable(TimeZoneEnvironmentVariable);
            if (!string.IsNullOrEmpty(result))
            {
                if (result[0] == ':')
                {
                    // strip off the ':' prefix
                    result = result.Substring(1);
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the time zone id by using 'readlink' on the path to see if tzFilePath is
        /// a symlink to a file.
        /// </summary>
        private static string? FindTimeZoneIdUsingReadLink(string tzFilePath)
        {
            string? id = null;

            string? symlinkPath = Interop.Sys.ReadLink(tzFilePath);
            if (symlinkPath != null)
            {
                // symlinkPath can be relative path, use Path to get the full absolute path.
                symlinkPath = Path.GetFullPath(symlinkPath, Path.GetDirectoryName(tzFilePath)!);

                string timeZoneDirectory = GetTimeZoneDirectory();
                if (symlinkPath.StartsWith(timeZoneDirectory, StringComparison.Ordinal))
                {
                    id = symlinkPath.Substring(timeZoneDirectory.Length);
                }
            }

            return id;
        }

        private static string? GetDirectoryEntryFullPath(ref Interop.Sys.DirectoryEntry dirent, string currentPath)
        {
            ReadOnlySpan<char> direntName = dirent.GetName(stackalloc char[Interop.Sys.DirectoryEntry.NameBufferSize]);

            if ((direntName.Length == 1 && direntName[0] == '.') ||
                (direntName.Length == 2 && direntName[0] == '.' && direntName[1] == '.'))
                return null;

            return Path.Join(currentPath.AsSpan(), direntName);
        }

        /// <summary>
        /// Enumerate files
        /// </summary>
        private static unsafe void EnumerateFilesRecursively(string path, Predicate<string> condition)
        {
            List<string>? toExplore = null; // List used as a stack

            int bufferSize = Interop.Sys.GetReadDirRBufferSize();
            byte[] dirBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                string currentPath = path;

                fixed (byte* dirBufferPtr = dirBuffer)
                {
                    while (true)
                    {
                        IntPtr dirHandle = Interop.Sys.OpenDir(currentPath);
                        if (dirHandle == IntPtr.Zero)
                        {
                            throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), currentPath, isDirectory: true);
                        }

                        try
                        {
                            // Read each entry from the enumerator
                            Interop.Sys.DirectoryEntry dirent;
                            while (Interop.Sys.ReadDirR(dirHandle, dirBufferPtr, bufferSize, out dirent) == 0)
                            {
                                string? fullPath = GetDirectoryEntryFullPath(ref dirent, currentPath);
                                if (fullPath == null)
                                    continue;

                                // Get from the dir entry whether the entry is a file or directory.
                                // We classify everything as a file unless we know it to be a directory.
                                bool isDir;
                                if (dirent.InodeType == Interop.Sys.NodeType.DT_DIR)
                                {
                                    // We know it's a directory.
                                    isDir = true;
                                }
                                else if (dirent.InodeType == Interop.Sys.NodeType.DT_LNK || dirent.InodeType == Interop.Sys.NodeType.DT_UNKNOWN)
                                {
                                    // It's a symlink or unknown: stat to it to see if we can resolve it to a directory.
                                    // If we can't (e.g. symlink to a file, broken symlink, etc.), we'll just treat it as a file.

                                    Interop.Sys.FileStatus fileinfo;
                                    if (Interop.Sys.Stat(fullPath, out fileinfo) >= 0)
                                    {
                                        isDir = (fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;
                                    }
                                    else
                                    {
                                        isDir = false;
                                    }
                                }
                                else
                                {
                                    // Otherwise, treat it as a file.  This includes regular files, FIFOs, etc.
                                    isDir = false;
                                }

                                // Yield the result if the user has asked for it.  In the case of directories,
                                // always explore it by pushing it onto the stack, regardless of whether
                                // we're returning directories.
                                if (isDir)
                                {
                                    toExplore ??= new List<string>();
                                    toExplore.Add(fullPath);
                                }
                                else if (condition(fullPath))
                                {
                                    return;
                                }
                            }
                        }
                        finally
                        {
                            if (dirHandle != IntPtr.Zero)
                                Interop.Sys.CloseDir(dirHandle);
                        }

                        if (toExplore == null || toExplore.Count == 0)
                            break;

                        currentPath = toExplore[toExplore.Count - 1];
                        toExplore.RemoveAt(toExplore.Count - 1);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(dirBuffer);
            }
        }

        private static bool CompareTimeZoneFile(string filePath, byte[] buffer, byte[] rawData)
        {
            try
            {
                // bufferSize == 1 used to avoid unnecessary buffer in FileStream
                using (SafeFileHandle sfh = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long fileLength = RandomAccess.GetLength(sfh);
                    if (fileLength == rawData.Length)
                    {
                        int index = 0;
                        int count = rawData.Length;

                        while (count > 0)
                        {
                            int n = RandomAccess.Read(sfh, buffer.AsSpan(index, count), index);
                            if (n == 0)
                                ThrowHelper.ThrowEndOfFileException();

                            int end = index + n;
                            for (; index < end; index++)
                            {
                                if (buffer[index] != rawData[index])
                                {
                                    return false;
                                }
                            }

                            count -= n;
                        }

                        return true;
                    }
                }
            }
            catch (IOException) { }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }

        /// <summary>
        /// Find the time zone id by searching all the tzfiles for the one that matches rawData
        /// and return its file name.
        /// </summary>
        private static string FindTimeZoneId(byte[] rawData)
        {
            // default to "Local" if we can't find the right tzfile
            string id = LocalId;
            string timeZoneDirectory = GetTimeZoneDirectory();
            string localtimeFilePath = Path.Combine(timeZoneDirectory, "localtime");
            string posixrulesFilePath = Path.Combine(timeZoneDirectory, "posixrules");
            byte[] buffer = new byte[rawData.Length];

            try
            {
                EnumerateFilesRecursively(timeZoneDirectory, (string filePath) =>
                {
                    // skip the localtime and posixrules file, since they won't give us the correct id
                    if (!string.Equals(filePath, localtimeFilePath, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(filePath, posixrulesFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (CompareTimeZoneFile(filePath, buffer, rawData))
                        {
                            // if all bytes are the same, this must be the right tz file
                            id = filePath;

                            // strip off the root time zone directory
                            if (id.StartsWith(timeZoneDirectory, StringComparison.Ordinal))
                            {
                                id = id.Substring(timeZoneDirectory.Length);
                            }
                            return true;
                        }
                    }
                    return false;
                });
            }
            catch (IOException) { }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }

            return id;
        }

        private static bool TryLoadTzFile(string tzFilePath, [NotNullWhen(true)] ref byte[]? rawData, [NotNullWhen(true)] ref string? id)
        {
            if (File.Exists(tzFilePath))
            {
                try
                {
                    rawData = File.ReadAllBytes(tzFilePath);
                    if (string.IsNullOrEmpty(id))
                    {
                        id = FindTimeZoneIdUsingReadLink(tzFilePath);

                        if (string.IsNullOrEmpty(id))
                        {
                            id = FindTimeZoneId(rawData);
                        }
                    }
                    return true;
                }
                catch (IOException) { }
                catch (SecurityException) { }
                catch (UnauthorizedAccessException) { }
            }
            return false;
        }

        /// <summary>
        /// Gets the tzfile raw data for the current 'local' time zone using the following rules.
        /// 1. Read the TZ environment variable.  If it is set, use it.
        /// 2. Look for the data in /etc/localtime.
        /// 3. Look for the data in GetTimeZoneDirectory()/localtime.
        /// 4. Use UTC if all else fails.
        /// </summary>
        private static bool TryGetLocalTzFile([NotNullWhen(true)] out byte[]? rawData, [NotNullWhen(true)] out string? id)
        {
            rawData = null;
            id = null;
            string? tzVariable = GetTzEnvironmentVariable();

            // If the env var is null, use the localtime file
            if (tzVariable == null)
            {
                return
                    TryLoadTzFile("/etc/localtime", ref rawData, ref id) ||
                    TryLoadTzFile(Path.Combine(GetTimeZoneDirectory(), "localtime"), ref rawData, ref id);
            }

            // If it's empty, use UTC (TryGetLocalTzFile() should return false).
            if (tzVariable.Length == 0)
            {
                return false;
            }

            // Otherwise, use the path from the env var.  If it's not absolute, make it relative
            // to the system timezone directory
            string tzFilePath;
            if (tzVariable[0] != '/')
            {
                id = tzVariable;
                tzFilePath = Path.Combine(GetTimeZoneDirectory(), tzVariable);
            }
            else
            {
                tzFilePath = tzVariable;
            }
            return TryLoadTzFile(tzFilePath, ref rawData, ref id);
        }

        /// <summary>
        /// Helper function used by 'GetLocalTimeZone()' - this function wraps the call
        /// for loading time zone data from computers without Registry support.
        ///
        /// The TryGetLocalTzFile() call returns a Byte[] containing the compiled tzfile.
        /// </summary>
        private static TimeZoneInfo GetLocalTimeZoneFromTzFile()
        {
            byte[]? rawData;
            string? id;
            if (TryGetLocalTzFile(out rawData, out id))
            {
                TimeZoneInfo? result = GetTimeZoneFromTzData(rawData, id);
                if (result != null)
                {
                    return result;
                }
            }

            // if we can't find a local time zone, return UTC
            return Utc;
        }

        private static string GetTimeZoneDirectory()
        {
            string? tzDirectory = Environment.GetEnvironmentVariable(TimeZoneDirectoryEnvironmentVariable);

            if (tzDirectory == null)
            {
                tzDirectory = DefaultTimeZoneDirectory;
            }
            else if (!tzDirectory.EndsWith(Path.DirectorySeparatorChar))
            {
                tzDirectory += PathInternal.DirectorySeparatorCharAsString;
            }

            return tzDirectory;
        }
    }
}
