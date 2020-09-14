// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Security;

using Internal.IO;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string DefaultTimeZoneDirectory = "/usr/share/zoneinfo/";
        private const string ZoneTabFileName = "zone.tab";
        private const string TimeZoneEnvironmentVariable = "TZ";
        private const string TimeZoneDirectoryEnvironmentVariable = "TZDIR";
        private const string FallbackCultureName = "en-US";

        private TimeZoneInfo(byte[] data, string id, bool dstDisabled)
        {
            TZifHead t;
            DateTime[] dts;
            byte[] typeOfLocalTime;
            TZifType[] transitionType;
            string zoneAbbreviations;
            bool[] StandardTime;
            bool[] GmtTime;
            string? futureTransitionsPosixFormat;

            // parse the raw TZif bytes; this method can throw ArgumentException when the data is malformed.
            TZif_ParseRaw(data, out t, out dts, out typeOfLocalTime, out transitionType, out zoneAbbreviations, out StandardTime, out GmtTime, out futureTransitionsPosixFormat);

            _id = id;
            _displayName = LocalId;
            _baseUtcOffset = TimeSpan.Zero;

            // find the best matching baseUtcOffset and display strings based on the current utcNow value.
            // NOTE: read the display strings from the tzfile now in case they can't be loaded later
            // from the globalization data.
            DateTime utcNow = DateTime.UtcNow;
            for (int i = 0; i < dts.Length && dts[i] <= utcNow; i++)
            {
                int type = typeOfLocalTime[i];
                if (!transitionType[type].IsDst)
                {
                    _baseUtcOffset = transitionType[type].UtcOffset;
                    _standardDisplayName = TZif_GetZoneAbbreviation(zoneAbbreviations, transitionType[type].AbbreviationIndex);
                }
                else
                {
                    _daylightDisplayName = TZif_GetZoneAbbreviation(zoneAbbreviations, transitionType[type].AbbreviationIndex);
                }
            }

            if (dts.Length == 0)
            {
                // time zones like Africa/Bujumbura and Etc/GMT* have no transition times but still contain
                // TZifType entries that may contain a baseUtcOffset and display strings
                for (int i = 0; i < transitionType.Length; i++)
                {
                    if (!transitionType[i].IsDst)
                    {
                        _baseUtcOffset = transitionType[i].UtcOffset;
                        _standardDisplayName = TZif_GetZoneAbbreviation(zoneAbbreviations, transitionType[i].AbbreviationIndex);
                    }
                    else
                    {
                        _daylightDisplayName = TZif_GetZoneAbbreviation(zoneAbbreviations, transitionType[i].AbbreviationIndex);
                    }
                }
            }
            _displayName = _standardDisplayName;

            string uiCulture = CultureInfo.CurrentUICulture.Name.Length == 0 ? FallbackCultureName : CultureInfo.CurrentUICulture.Name; // ICU doesn't work nicely with Invariant
            GetDisplayName(Interop.Globalization.TimeZoneDisplayNameType.Generic, uiCulture, ref _displayName);
            GetDisplayName(Interop.Globalization.TimeZoneDisplayNameType.Standard, uiCulture, ref _standardDisplayName);
            GetDisplayName(Interop.Globalization.TimeZoneDisplayNameType.DaylightSavings, uiCulture, ref _daylightDisplayName);

            if (_standardDisplayName == _displayName)
            {
                if (_baseUtcOffset >= TimeSpan.Zero)
                    _displayName = $"(UTC+{_baseUtcOffset:hh\\:mm}) {_standardDisplayName}";
                else
                    _displayName = $"(UTC-{_baseUtcOffset:hh\\:mm}) {_standardDisplayName}";
            }

            // TZif supports seconds-level granularity with offsets but TimeZoneInfo only supports minutes since it aligns
            // with DateTimeOffset, SQL Server, and the W3C XML Specification
            if (_baseUtcOffset.Ticks % TimeSpan.TicksPerMinute != 0)
            {
                _baseUtcOffset = new TimeSpan(_baseUtcOffset.Hours, _baseUtcOffset.Minutes, 0);
            }

            if (!dstDisabled)
            {
                // only create the adjustment rule if DST is enabled
                TZif_GenerateAdjustmentRules(out _adjustmentRules, _baseUtcOffset, dts, typeOfLocalTime, transitionType, StandardTime, GmtTime, futureTransitionsPosixFormat);
            }

            ValidateTimeZoneInfo(_id, _baseUtcOffset, _adjustmentRules, out _supportsDaylightSavingTime);
        }

        /// <summary>
        /// Returns a cloned array of AdjustmentRule objects
        /// </summary>
        public AdjustmentRule[] GetAdjustmentRules()
        {
            if (_adjustmentRules == null)
            {
                return Array.Empty<AdjustmentRule>();
            }

            // The rules we use in Unix care mostly about the start and end dates but don't fill the transition start and end info.
            // as the rules now is public, we should fill it properly so the caller doesn't have to know how we use it internally
            // and can use it as it is used in Windows

            AdjustmentRule[] rules = new AdjustmentRule[_adjustmentRules.Length];

            for (int i = 0; i < _adjustmentRules.Length; i++)
            {
                AdjustmentRule? rule = _adjustmentRules[i];
                DateTime start = rule.DateStart.Kind == DateTimeKind.Utc ?
                            // At the daylight start we didn't start the daylight saving yet then we convert to Local time
                            // by adding the _baseUtcOffset to the UTC time
                            new DateTime(rule.DateStart.Ticks + _baseUtcOffset.Ticks, DateTimeKind.Unspecified) :
                            rule.DateStart;
                DateTime end = rule.DateEnd.Kind == DateTimeKind.Utc ?
                            // At the daylight saving end, the UTC time is mapped to local time which is already shifted by the daylight delta
                            // we calculate the local time by adding _baseUtcOffset + DaylightDelta to the UTC time
                            new DateTime(rule.DateEnd.Ticks + _baseUtcOffset.Ticks + rule.DaylightDelta.Ticks, DateTimeKind.Unspecified) :
                            rule.DateEnd;

                TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, start.Hour, start.Minute, start.Second), start.Month, start.Day);
                TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, end.Hour, end.Minute, end.Second), end.Month, end.Day);

                rules[i] = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(start.Date, end.Date, rule.DaylightDelta, startTransition, endTransition);
            }

            return rules;
        }

        private static void PopulateAllSystemTimeZones(CachedData cachedData)
        {
            Debug.Assert(Monitor.IsEntered(cachedData));

            string timeZoneDirectory = GetTimeZoneDirectory();
            foreach (string timeZoneId in GetTimeZoneIds(timeZoneDirectory))
            {
                TryGetTimeZone(timeZoneId, false, out _, out _, cachedData, alwaysFallbackToLocalMachine: true);  // populate the cache
            }
        }

        /// <summary>
        /// Helper function for retrieving the local system time zone.
        /// May throw COMException, TimeZoneNotFoundException, InvalidTimeZoneException.
        /// Assumes cachedData lock is taken.
        /// </summary>
        /// <returns>A new TimeZoneInfo instance.</returns>
        private static TimeZoneInfo GetLocalTimeZone(CachedData cachedData)
        {
            Debug.Assert(Monitor.IsEntered(cachedData));

            // Without Registry support, create the TimeZoneInfo from a TZ file
            return GetLocalTimeZoneFromTzFile();
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachine(string id, out TimeZoneInfo? value, out Exception? e)
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
        /// Returns a collection of TimeZone Id values from the zone.tab file in the timeZoneDirectory.
        /// </summary>
        /// <remarks>
        /// Lines that start with # are comments and are skipped.
        /// </remarks>
        private static List<string> GetTimeZoneIds(string timeZoneDirectory)
        {
            List<string> timeZoneIds = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(Path.Combine(timeZoneDirectory, ZoneTabFileName), Encoding.UTF8))
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
            byte[]? dirBuffer = null;
            try
            {
                dirBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
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
                if (dirBuffer != null)
                    ArrayPool<byte>.Shared.Return(dirBuffer);
            }
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

        private static bool CompareTimeZoneFile(string filePath, byte[] buffer, byte[] rawData)
        {
            try
            {
                // bufferSize == 1 used to avoid unnecessary buffer in FileStream
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
                {
                    if (stream.Length == rawData.Length)
                    {
                        int index = 0;
                        int count = rawData.Length;

                        while (count > 0)
                        {
                            int n = stream.Read(buffer, index, count);
                            if (n == 0)
                                throw Error.GetEndOfFile();

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

        private static TimeZoneInfo? GetTimeZoneFromTzData(byte[]? rawData, string id)
        {
            if (rawData != null)
            {
                try
                {
                    return new TimeZoneInfo(rawData, id, dstDisabled: false); // create a TimeZoneInfo instance from the TZif data w/ DST support
                }
                catch (ArgumentException) { }
                catch (InvalidTimeZoneException) { }

                try
                {
                    return new TimeZoneInfo(rawData, id, dstDisabled: true); // create a TimeZoneInfo instance from the TZif data w/o DST support
                }
                catch (ArgumentException) { }
                catch (InvalidTimeZoneException) { }
            }
            return null;
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

        /// <summary>
        /// Helper function for retrieving a TimeZoneInfo object by time_zone_name.
        /// This function wraps the logic necessary to keep the private
        /// SystemTimeZones cache in working order
        ///
        /// This function will either return a valid TimeZoneInfo instance or
        /// it will throw 'InvalidTimeZoneException' / 'TimeZoneNotFoundException'.
        /// </summary>
        public static TimeZoneInfo FindSystemTimeZoneById(string id)
        {
            // Special case for Utc as it will not exist in the dictionary with the rest
            // of the system time zones.  There is no need to do this check for Local.Id
            // since Local is a real time zone that exists in the dictionary cache
            if (string.Equals(id, UtcId, StringComparison.OrdinalIgnoreCase))
            {
                return Utc;
            }

            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            else if (id.Length == 0 || id.Contains('\0'))
            {
                throw new TimeZoneNotFoundException(SR.Format(SR.TimeZoneNotFound_MissingData, id));
            }

            TimeZoneInfo? value;
            Exception? e;

            TimeZoneInfoResult result;

            CachedData cachedData = s_cachedData;

            lock (cachedData)
            {
                result = TryGetTimeZone(id, false, out value, out e, cachedData, alwaysFallbackToLocalMachine: true);
            }

            if (result == TimeZoneInfoResult.Success)
            {
                return value!;
            }
            else if (result == TimeZoneInfoResult.InvalidTimeZoneException)
            {
                Debug.Assert(e is InvalidTimeZoneException,
                    "TryGetTimeZone must create an InvalidTimeZoneException when it returns TimeZoneInfoResult.InvalidTimeZoneException");
                throw e;
            }
            else if (result == TimeZoneInfoResult.SecurityException)
            {
                throw new SecurityException(SR.Format(SR.Security_CannotReadFileData, id), e);
            }
            else
            {
                throw new TimeZoneNotFoundException(SR.Format(SR.TimeZoneNotFound_MissingData, id), e);
            }
        }

        // DateTime.Now fast path that avoids allocating an historically accurate TimeZoneInfo.Local and just creates a 1-year (current year) accurate time zone
        internal static TimeSpan GetDateTimeNowUtcOffsetFromUtc(DateTime time, out bool isAmbiguousLocalDst)
        {
            bool isDaylightSavings;
            // Use the standard code path for Unix since there isn't a faster way of handling current-year-only time zones
            return GetUtcOffsetFromUtc(time, Local, out isDaylightSavings, out isAmbiguousLocalDst);
        }

        // TZFILE(5)                   BSD File Formats Manual                  TZFILE(5)
        //
        // NAME
        //      tzfile -- timezone information
        //
        // SYNOPSIS
        //      #include "/usr/src/lib/libc/stdtime/tzfile.h"
        //
        // DESCRIPTION
        //      The time zone information files used by tzset(3) begin with the magic
        //      characters ``TZif'' to identify them as time zone information files, fol-
        //      lowed by sixteen bytes reserved for future use, followed by four four-
        //      byte values written in a ``standard'' byte order (the high-order byte of
        //      the value is written first).  These values are, in order:
        //
        //      tzh_ttisgmtcnt  The number of UTC/local indicators stored in the file.
        //      tzh_ttisstdcnt  The number of standard/wall indicators stored in the
        //                      file.
        //      tzh_leapcnt     The number of leap seconds for which data is stored in
        //                      the file.
        //      tzh_timecnt     The number of ``transition times'' for which data is
        //                      stored in the file.
        //      tzh_typecnt     The number of ``local time types'' for which data is
        //                      stored in the file (must not be zero).
        //      tzh_charcnt     The number of characters of ``time zone abbreviation
        //                      strings'' stored in the file.
        //
        //      The above header is followed by tzh_timecnt four-byte values of type
        //      long, sorted in ascending order.  These values are written in ``stan-
        //      dard'' byte order.  Each is used as a transition time (as returned by
        //      time(3)) at which the rules for computing local time change.  Next come
        //      tzh_timecnt one-byte values of type unsigned char; each one tells which
        //      of the different types of ``local time'' types described in the file is
        //      associated with the same-indexed transition time.  These values serve as
        //      indices into an array of ttinfo structures that appears next in the file;
        //      these structures are defined as follows:
        //
        //            struct ttinfo {
        //                    long    tt_gmtoff;
        //                    int     tt_isdst;
        //                    unsigned int    tt_abbrind;
        //            };
        //
        //      Each structure is written as a four-byte value for tt_gmtoff of type
        //      long, in a standard byte order, followed by a one-byte value for tt_isdst
        //      and a one-byte value for tt_abbrind.  In each structure, tt_gmtoff gives
        //      the number of seconds to be added to UTC, tt_isdst tells whether tm_isdst
        //      should be set by localtime(3) and tt_abbrind serves as an index into the
        //      array of time zone abbreviation characters that follow the ttinfo struc-
        //      ture(s) in the file.
        //
        //      Then there are tzh_leapcnt pairs of four-byte values, written in standard
        //      byte order; the first value of each pair gives the time (as returned by
        //      time(3)) at which a leap second occurs; the second gives the total number
        //      of leap seconds to be applied after the given time.  The pairs of values
        //      are sorted in ascending order by time.b
        //
        //      Then there are tzh_ttisstdcnt standard/wall indicators, each stored as a
        //      one-byte value; they tell whether the transition times associated with
        //      local time types were specified as standard time or wall clock time, and
        //      are used when a time zone file is used in handling POSIX-style time zone
        //      environment variables.
        //
        //      Finally there are tzh_ttisgmtcnt UTC/local indicators, each stored as a
        //      one-byte value; they tell whether the transition times associated with
        //      local time types were specified as UTC or local time, and are used when a
        //      time zone file is used in handling POSIX-style time zone environment
        //      variables.
        //
        //      localtime uses the first standard-time ttinfo structure in the file (or
        //      simply the first ttinfo structure in the absence of a standard-time
        //      structure) if either tzh_timecnt is zero or the time argument is less
        //      than the first transition time recorded in the file.
        //
        // SEE ALSO
        //      ctime(3), time2posix(3), zic(8)
        //
        // BSD                           September 13, 1994                           BSD
        //
        //
        //
        // TIME(3)                  BSD Library Functions Manual                  TIME(3)
        //
        // NAME
        //      time -- get time of day
        //
        // LIBRARY
        //      Standard C Library (libc, -lc)
        //
        // SYNOPSIS
        //      #include <time.h>
        //
        //      time_t
        //      time(time_t *tloc);
        //
        // DESCRIPTION
        //      The time() function returns the value of time in seconds since 0 hours, 0
        //      minutes, 0 seconds, January 1, 1970, Coordinated Universal Time, without
        //      including leap seconds.  If an error occurs, time() returns the value
        //      (time_t)-1.
        //
        //      The return value is also stored in *tloc, provided that tloc is non-null.
        //
        // ERRORS
        //      The time() function may fail for any of the reasons described in
        //      gettimeofday(2).
        //
        // SEE ALSO
        //      gettimeofday(2), ctime(3)
        //
        // STANDARDS
        //      The time function conforms to IEEE Std 1003.1-2001 (``POSIX.1'').
        //
        // BUGS
        //      Neither ISO/IEC 9899:1999 (``ISO C99'') nor IEEE Std 1003.1-2001
        //      (``POSIX.1'') requires time() to set errno on failure; thus, it is impos-
        //      sible for an application to distinguish the valid time value -1 (repre-
        //      senting the last UTC second of 1969) from the error return value.
        //
        //      Systems conforming to earlier versions of the C and POSIX standards
        //      (including older versions of FreeBSD) did not set *tloc in the error
        //      case.
        //
        // HISTORY
        //      A time() function appeared in Version 6 AT&T UNIX.
        //
        // BSD                              July 18, 2003                             BSD
        //
        //
        private static void TZif_GenerateAdjustmentRules(out AdjustmentRule[]? rules, TimeSpan baseUtcOffset, DateTime[] dts, byte[] typeOfLocalTime,
            TZifType[] transitionType, bool[] StandardTime, bool[] GmtTime, string? futureTransitionsPosixFormat)
        {
            rules = null;

            if (dts.Length > 0)
            {
                int index = 0;
                List<AdjustmentRule> rulesList = new List<AdjustmentRule>();

                while (index <= dts.Length)
                {
                    TZif_GenerateAdjustmentRule(ref index, baseUtcOffset, rulesList, dts, typeOfLocalTime, transitionType, StandardTime, GmtTime, futureTransitionsPosixFormat);
                }

                rules = rulesList.ToArray();
                if (rules != null && rules.Length == 0)
                {
                    rules = null;
                }
            }
        }

        private static void TZif_GenerateAdjustmentRule(ref int index, TimeSpan timeZoneBaseUtcOffset, List<AdjustmentRule> rulesList, DateTime[] dts,
            byte[] typeOfLocalTime, TZifType[] transitionTypes, bool[] StandardTime, bool[] GmtTime, string? futureTransitionsPosixFormat)
        {
            // To generate AdjustmentRules, use the following approach:
            // The first AdjustmentRule will go from DateTime.MinValue to the first transition time greater than DateTime.MinValue.
            // Each middle AdjustmentRule wil go from dts[index-1] to dts[index].
            // The last AdjustmentRule will go from dts[dts.Length-1] to Datetime.MaxValue.

            // 0. Skip any DateTime.MinValue transition times. In newer versions of the tzfile, there
            // is a "big bang" transition time, which is before the year 0001. Since any times before year 0001
            // cannot be represented by DateTime, there is no reason to make AdjustmentRules for these unrepresentable time periods.
            // 1. If there are no DateTime.MinValue times, the first AdjustmentRule goes from DateTime.MinValue
            // to the first transition and uses the first standard transitionType (or the first transitionType if none of them are standard)
            // 2. Create an AdjustmentRule for each transition, i.e. from dts[index - 1] to dts[index].
            // This rule uses the transitionType[index - 1] and the whole AdjustmentRule only describes a single offset - either
            // all daylight savings, or all standard time.
            // 3. After all the transitions are filled out, the last AdjustmentRule is created from either:
            //   a. a POSIX-style timezone description ("futureTransitionsPosixFormat"), if there is one or
            //   b. continue the last transition offset until DateTime.Max

            while (index < dts.Length && dts[index] == DateTime.MinValue)
            {
                index++;
            }

            if (rulesList.Count == 0 && index < dts.Length)
            {
                TZifType transitionType = TZif_GetEarlyDateTransitionType(transitionTypes);
                DateTime endTransitionDate = dts[index];

                TimeSpan transitionOffset = TZif_CalculateTransitionOffsetFromBase(transitionType.UtcOffset, timeZoneBaseUtcOffset);
                TimeSpan daylightDelta = transitionType.IsDst ? transitionOffset : TimeSpan.Zero;
                TimeSpan baseUtcDelta = transitionType.IsDst ? TimeSpan.Zero : transitionOffset;

                AdjustmentRule r = AdjustmentRule.CreateAdjustmentRule(
                        DateTime.MinValue,
                        endTransitionDate.AddTicks(-1),
                        daylightDelta,
                        default,
                        default,
                        baseUtcDelta,
                        noDaylightTransitions: true);

                if (!IsValidAdjustmentRuleOffest(timeZoneBaseUtcOffset, r))
                {
                    NormalizeAdjustmentRuleOffset(timeZoneBaseUtcOffset, ref r);
                }

                rulesList.Add(r);
            }
            else if (index < dts.Length)
            {
                DateTime startTransitionDate = dts[index - 1];
                TZifType startTransitionType = transitionTypes[typeOfLocalTime[index - 1]];

                DateTime endTransitionDate = dts[index];

                TimeSpan transitionOffset = TZif_CalculateTransitionOffsetFromBase(startTransitionType.UtcOffset, timeZoneBaseUtcOffset);
                TimeSpan daylightDelta = startTransitionType.IsDst ? transitionOffset : TimeSpan.Zero;
                TimeSpan baseUtcDelta = startTransitionType.IsDst ? TimeSpan.Zero : transitionOffset;

                TransitionTime dstStart;
                if (startTransitionType.IsDst)
                {
                    // the TransitionTime fields are not used when AdjustmentRule.NoDaylightTransitions == true.
                    // However, there are some cases in the past where DST = true, and the daylight savings offset
                    // now equals what the current BaseUtcOffset is.  In that case, the AdjustmentRule.DaylightOffset
                    // is going to be TimeSpan.Zero.  But we still need to return 'true' from AdjustmentRule.HasDaylightSaving.
                    // To ensure we always return true from HasDaylightSaving, make a "special" dstStart that will make the logic
                    // in HasDaylightSaving return true.
                    dstStart = TransitionTime.CreateFixedDateRule(DateTime.MinValue.AddMilliseconds(2), 1, 1);
                }
                else
                {
                    dstStart = default;
                }

                AdjustmentRule r = AdjustmentRule.CreateAdjustmentRule(
                        startTransitionDate,
                        endTransitionDate.AddTicks(-1),
                        daylightDelta,
                        dstStart,
                        default,
                        baseUtcDelta,
                        noDaylightTransitions: true);

                if (!IsValidAdjustmentRuleOffest(timeZoneBaseUtcOffset, r))
                {
                    NormalizeAdjustmentRuleOffset(timeZoneBaseUtcOffset, ref r);
                }

                rulesList.Add(r);
            }
            else
            {
                // create the AdjustmentRule that will be used for all DateTimes after the last transition

                // NOTE: index == dts.Length
                DateTime startTransitionDate = dts[index - 1];

                AdjustmentRule? r = !string.IsNullOrEmpty(futureTransitionsPosixFormat) ?
                    TZif_CreateAdjustmentRuleForPosixFormat(futureTransitionsPosixFormat, startTransitionDate, timeZoneBaseUtcOffset) :
                    null;

                if (r == null)
                {
                    // just use the last transition as the rule which will be used until the end of time

                    TZifType transitionType = transitionTypes[typeOfLocalTime[index - 1]];
                    TimeSpan transitionOffset = TZif_CalculateTransitionOffsetFromBase(transitionType.UtcOffset, timeZoneBaseUtcOffset);
                    TimeSpan daylightDelta = transitionType.IsDst ? transitionOffset : TimeSpan.Zero;
                    TimeSpan baseUtcDelta = transitionType.IsDst ? TimeSpan.Zero : transitionOffset;

                    r = AdjustmentRule.CreateAdjustmentRule(
                        startTransitionDate,
                        DateTime.MaxValue,
                        daylightDelta,
                        default,
                        default,
                        baseUtcDelta,
                        noDaylightTransitions: true);
                }

                if (!IsValidAdjustmentRuleOffest(timeZoneBaseUtcOffset, r))
                {
                    NormalizeAdjustmentRuleOffset(timeZoneBaseUtcOffset, ref r);
                }

                rulesList.Add(r);
            }

            index++;
        }

        private static TimeSpan TZif_CalculateTransitionOffsetFromBase(TimeSpan transitionOffset, TimeSpan timeZoneBaseUtcOffset)
        {
            TimeSpan result = transitionOffset - timeZoneBaseUtcOffset;

            // TZif supports seconds-level granularity with offsets but TimeZoneInfo only supports minutes since it aligns
            // with DateTimeOffset, SQL Server, and the W3C XML Specification
            if (result.Ticks % TimeSpan.TicksPerMinute != 0)
            {
                result = new TimeSpan(result.Hours, result.Minutes, 0);
            }

            return result;
        }

        /// <summary>
        /// Gets the first standard-time transition type, or simply the first transition type
        /// if there are no standard transition types.
        /// </summary>>
        /// <remarks>
        /// from 'man tzfile':
        /// localtime(3)  uses the first standard-time ttinfo structure in the file
        /// (or simply the first ttinfo structure in the absence of a standard-time
        /// structure)  if  either tzh_timecnt is zero or the time argument is less
        /// than the first transition time recorded in the file.
        /// </remarks>
        private static TZifType TZif_GetEarlyDateTransitionType(TZifType[] transitionTypes)
        {
            foreach (TZifType transitionType in transitionTypes)
            {
                if (!transitionType.IsDst)
                {
                    return transitionType;
                }
            }

            if (transitionTypes.Length > 0)
            {
                return transitionTypes[0];
            }

            throw new InvalidTimeZoneException(SR.InvalidTimeZone_NoTTInfoStructures);
        }

        /// <summary>
        /// Creates an AdjustmentRule given the POSIX TZ environment variable string.
        /// </summary>
        /// <remarks>
        /// See http://man7.org/linux/man-pages/man3/tzset.3.html for the format and semantics of this POSX string.
        /// </remarks>
        private static AdjustmentRule? TZif_CreateAdjustmentRuleForPosixFormat(string posixFormat, DateTime startTransitionDate, TimeSpan timeZoneBaseUtcOffset)
        {
            if (TZif_ParsePosixFormat(posixFormat,
                out ReadOnlySpan<char> standardName,
                out ReadOnlySpan<char> standardOffset,
                out ReadOnlySpan<char> daylightSavingsName,
                out ReadOnlySpan<char> daylightSavingsOffset,
                out ReadOnlySpan<char> start,
                out ReadOnlySpan<char> startTime,
                out ReadOnlySpan<char> end,
                out ReadOnlySpan<char> endTime))
            {
                // a valid posixFormat has at least standardName and standardOffset

                TimeSpan? parsedBaseOffset = TZif_ParseOffsetString(standardOffset);
                if (parsedBaseOffset.HasValue)
                {
                    TimeSpan baseOffset = parsedBaseOffset.GetValueOrDefault().Negate(); // offsets are backwards in POSIX notation
                    baseOffset = TZif_CalculateTransitionOffsetFromBase(baseOffset, timeZoneBaseUtcOffset);

                    // having a daylightSavingsName means there is a DST rule
                    if (!daylightSavingsName.IsEmpty)
                    {
                        TimeSpan? parsedDaylightSavings = TZif_ParseOffsetString(daylightSavingsOffset);
                        TimeSpan daylightSavingsTimeSpan;
                        if (!parsedDaylightSavings.HasValue)
                        {
                            // default DST to 1 hour if it isn't specified
                            daylightSavingsTimeSpan = new TimeSpan(1, 0, 0);
                        }
                        else
                        {
                            daylightSavingsTimeSpan = parsedDaylightSavings.GetValueOrDefault().Negate(); // offsets are backwards in POSIX notation
                            daylightSavingsTimeSpan = TZif_CalculateTransitionOffsetFromBase(daylightSavingsTimeSpan, timeZoneBaseUtcOffset);
                            daylightSavingsTimeSpan = TZif_CalculateTransitionOffsetFromBase(daylightSavingsTimeSpan, baseOffset);
                        }

                        TransitionTime? dstStart = TZif_CreateTransitionTimeFromPosixRule(start, startTime);
                        TransitionTime? dstEnd = TZif_CreateTransitionTimeFromPosixRule(end, endTime);

                        if (dstStart == null || dstEnd == null)
                        {
                            return null;
                        }

                        return AdjustmentRule.CreateAdjustmentRule(
                            startTransitionDate,
                            DateTime.MaxValue,
                            daylightSavingsTimeSpan,
                            dstStart.GetValueOrDefault(),
                            dstEnd.GetValueOrDefault(),
                            baseOffset,
                            noDaylightTransitions: false);
                    }
                    else
                    {
                        // if there is no daylightSavingsName, the whole AdjustmentRule should be with no transitions - just the baseOffset
                        return AdjustmentRule.CreateAdjustmentRule(
                               startTransitionDate,
                               DateTime.MaxValue,
                               TimeSpan.Zero,
                               default,
                               default,
                               baseOffset,
                               noDaylightTransitions: true);
                    }
                }
            }

            return null;
        }

        private static TimeSpan? TZif_ParseOffsetString(ReadOnlySpan<char> offset)
        {
            TimeSpan? result = null;

            if (offset.Length > 0)
            {
                bool negative = offset[0] == '-';
                if (negative || offset[0] == '+')
                {
                    offset = offset.Slice(1);
                }

                // Try parsing just hours first.
                // Note, TimeSpan.TryParseExact "%h" can't be used here because some time zones using values
                // like "26" or "144" and TimeSpan parsing would turn that into 26 or 144 *days* instead of hours.
                int hours;
                if (int.TryParse(offset, out hours))
                {
                    result = new TimeSpan(hours, 0, 0);
                }
                else
                {
                    TimeSpan parsedTimeSpan;
                    if (TimeSpan.TryParseExact(offset, "g", CultureInfo.InvariantCulture, out parsedTimeSpan))
                    {
                        result = parsedTimeSpan;
                    }
                }

                if (result.HasValue && negative)
                {
                    result = result.GetValueOrDefault().Negate();
                }
            }

            return result;
        }

        private static DateTime ParseTimeOfDay(ReadOnlySpan<char> time)
        {
            DateTime timeOfDay;
            TimeSpan? timeOffset = TZif_ParseOffsetString(time);
            if (timeOffset.HasValue)
            {
                // This logic isn't correct and can't be corrected until https://github.com/dotnet/runtime/issues/14966 is fixed.
                // Some time zones use time values like, "26", "144", or "-2".
                // This allows the week to sometimes be week 4 and sometimes week 5 in the month.
                // For now, strip off any 'days' in the offset, and just get the time of day correct
                timeOffset = new TimeSpan(timeOffset.GetValueOrDefault().Hours, timeOffset.GetValueOrDefault().Minutes, timeOffset.GetValueOrDefault().Seconds);
                if (timeOffset.GetValueOrDefault() < TimeSpan.Zero)
                {
                    timeOfDay = new DateTime(1, 1, 2, 0, 0, 0);
                }
                else
                {
                    timeOfDay = new DateTime(1, 1, 1, 0, 0, 0);
                }

                timeOfDay += timeOffset.GetValueOrDefault();
            }
            else
            {
                // default to 2AM.
                timeOfDay = new DateTime(1, 1, 1, 2, 0, 0);
            }

            return timeOfDay;
        }

        private static TransitionTime? TZif_CreateTransitionTimeFromPosixRule(ReadOnlySpan<char> date, ReadOnlySpan<char> time)
        {
            if (date.IsEmpty)
            {
                return null;
            }

            if (date[0] == 'M')
            {
                // Mm.w.d
                // This specifies day d of week w of month m. The day d must be between 0(Sunday) and 6.The week w must be between 1 and 5;
                // week 1 is the first week in which day d occurs, and week 5 specifies the last d day in the month. The month m should be between 1 and 12.

                int month;
                int week;
                DayOfWeek day;
                if (!TZif_ParseMDateRule(date, out month, out week, out day))
                {
                    throw new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_UnparseablePosixMDateString, date.ToString()));
                }

                return TransitionTime.CreateFloatingDateRule(ParseTimeOfDay(time), month, week, day);
            }
            else
            {
                if (date[0] != 'J')
                {
                    // should be n Julian day format which we don't support.
                    //
                    // This specifies the Julian day, with n between 0 and 365. February 29 is counted in leap years.
                    //
                    // n would be a relative number from the beginning of the year. which should handle if the
                    // the year is a leap year or not.
                    //
                    // In leap year, n would be counted as:
                    //
                    // 0                30 31              59 60              90      335            365
                    // |-------Jan--------|-------Feb--------|-------Mar--------|....|-------Dec--------|
                    //
                    // while in non leap year we'll have
                    //
                    // 0                30 31              58 59              89      334            364
                    // |-------Jan--------|-------Feb--------|-------Mar--------|....|-------Dec--------|
                    //
                    //
                    // For example if n is specified as 60, this means in leap year the rule will start at Mar 1,
                    // while in non leap year the rule will start at Mar 2.
                    //
                    // If we need to support n format, we'll have to have a floating adjustment rule support this case.

                    // Since we can't support this rule, return null to indicate to skip the POSIX rule.
                    return null;
                }

                // Julian day
                TZif_ParseJulianDay(date, out int month, out int day);
                return TransitionTime.CreateFixedDateRule(ParseTimeOfDay(time), month, day);
            }
        }

        /// <summary>
        /// Parses a string like Jn or n into month and day values.
        /// </summary>
        private static void TZif_ParseJulianDay(ReadOnlySpan<char> date, out int month, out int day)
        {
            // Jn
            // This specifies the Julian day, with n between 1 and 365.February 29 is never counted, even in leap years.
            Debug.Assert(!date.IsEmpty);
            Debug.Assert(date[0] == 'J');
            month = day = 0;

            int index = 1;

            if (index >= date.Length || ((uint)(date[index] - '0') > '9'-'0'))
            {
                throw new InvalidTimeZoneException(SR.InvalidTimeZone_InvalidJulianDay);
            }

            int julianDay = 0;

            do
            {
                julianDay = julianDay * 10 + (int) (date[index] - '0');
                index++;
            } while (index < date.Length && ((uint)(date[index] - '0') <= '9'-'0'));

            int[] days = GregorianCalendarHelper.DaysToMonth365;

            if (julianDay == 0 || julianDay > days[days.Length - 1])
            {
                throw new InvalidTimeZoneException(SR.InvalidTimeZone_InvalidJulianDay);
            }

            int i = 1;
            while (i < days.Length && julianDay > days[i])
            {
                i++;
            }

            Debug.Assert(i > 0 && i < days.Length);

            month = i;
            day = julianDay - days[i - 1];
        }

        /// <summary>
        /// Parses a string like Mm.w.d into month, week and DayOfWeek values.
        /// </summary>
        /// <returns>
        /// true if the parsing succeeded; otherwise, false.
        /// </returns>
        private static bool TZif_ParseMDateRule(ReadOnlySpan<char> dateRule, out int month, out int week, out DayOfWeek dayOfWeek)
        {
            if (dateRule[0] == 'M')
            {
                int monthWeekDotIndex = dateRule.IndexOf('.');
                if (monthWeekDotIndex > 0)
                {
                    ReadOnlySpan<char> weekDaySpan = dateRule.Slice(monthWeekDotIndex + 1);
                    int weekDayDotIndex = weekDaySpan.IndexOf('.');
                    if (weekDayDotIndex > 0)
                    {
                        if (int.TryParse(dateRule.Slice(1, monthWeekDotIndex - 1), out month) &&
                            int.TryParse(weekDaySpan.Slice(0, weekDayDotIndex), out week) &&
                            int.TryParse(weekDaySpan.Slice(weekDayDotIndex + 1), out int day))
                        {
                            dayOfWeek = (DayOfWeek)day;
                            return true;
                        }
                    }
                }
            }

            month = 0;
            week = 0;
            dayOfWeek = default;
            return false;
        }

        private static bool TZif_ParsePosixFormat(
            ReadOnlySpan<char> posixFormat,
            out ReadOnlySpan<char> standardName,
            out ReadOnlySpan<char> standardOffset,
            out ReadOnlySpan<char> daylightSavingsName,
            out ReadOnlySpan<char> daylightSavingsOffset,
            out ReadOnlySpan<char> start,
            out ReadOnlySpan<char> startTime,
            out ReadOnlySpan<char> end,
            out ReadOnlySpan<char> endTime)
        {
            standardName = null;
            standardOffset = null;
            daylightSavingsName = null;
            daylightSavingsOffset = null;
            start = null;
            startTime = null;
            end = null;
            endTime = null;

            int index = 0;
            standardName = TZif_ParsePosixName(posixFormat, ref index);
            standardOffset = TZif_ParsePosixOffset(posixFormat, ref index);

            daylightSavingsName = TZif_ParsePosixName(posixFormat, ref index);
            if (!daylightSavingsName.IsEmpty)
            {
                daylightSavingsOffset = TZif_ParsePosixOffset(posixFormat, ref index);

                if (index < posixFormat.Length && posixFormat[index] == ',')
                {
                    index++;
                    TZif_ParsePosixDateTime(posixFormat, ref index, out start, out startTime);

                    if (index < posixFormat.Length && posixFormat[index] == ',')
                    {
                        index++;
                        TZif_ParsePosixDateTime(posixFormat, ref index, out end, out endTime);
                    }
                }
            }

            return !standardName.IsEmpty && !standardOffset.IsEmpty;
        }

        private static ReadOnlySpan<char> TZif_ParsePosixName(ReadOnlySpan<char> posixFormat, ref int index)
        {
            bool isBracketEnclosed = index < posixFormat.Length && posixFormat[index] == '<';
            if (isBracketEnclosed)
            {
                // move past the opening bracket
                index++;

                ReadOnlySpan<char> result = TZif_ParsePosixString(posixFormat, ref index, c => c == '>');

                // move past the closing bracket
                if (index < posixFormat.Length && posixFormat[index] == '>')
                {
                    index++;
                }

                return result;
            }
            else
            {
                return TZif_ParsePosixString(
                    posixFormat,
                    ref index,
                    c => char.IsDigit(c) || c == '+' || c == '-' || c == ',');
            }
        }

        private static ReadOnlySpan<char> TZif_ParsePosixOffset(ReadOnlySpan<char> posixFormat, ref int index) =>
            TZif_ParsePosixString(posixFormat, ref index, c => !char.IsDigit(c) && c != '+' && c != '-' && c != ':');

        private static void TZif_ParsePosixDateTime(ReadOnlySpan<char> posixFormat, ref int index, out ReadOnlySpan<char> date, out ReadOnlySpan<char> time)
        {
            time = null;

            date = TZif_ParsePosixDate(posixFormat, ref index);
            if (index < posixFormat.Length && posixFormat[index] == '/')
            {
                index++;
                time = TZif_ParsePosixTime(posixFormat, ref index);
            }
        }

        private static ReadOnlySpan<char> TZif_ParsePosixDate(ReadOnlySpan<char> posixFormat, ref int index) =>
            TZif_ParsePosixString(posixFormat, ref index, c => c == '/' || c == ',');

        private static ReadOnlySpan<char> TZif_ParsePosixTime(ReadOnlySpan<char> posixFormat, ref int index) =>
            TZif_ParsePosixString(posixFormat, ref index, c => c == ',');

        private static ReadOnlySpan<char> TZif_ParsePosixString(ReadOnlySpan<char> posixFormat, ref int index, Func<char, bool> breakCondition)
        {
            int startIndex = index;
            for (; index < posixFormat.Length; index++)
            {
                char current = posixFormat[index];
                if (breakCondition(current))
                {
                    break;
                }
            }

            return posixFormat.Slice(startIndex, index - startIndex);
        }

        // Returns the Substring from zoneAbbreviations starting at index and ending at '\0'
        // zoneAbbreviations is expected to be in the form: "PST\0PDT\0PWT\0\PPT"
        private static string TZif_GetZoneAbbreviation(string zoneAbbreviations, int index)
        {
            int lastIndex = zoneAbbreviations.IndexOf('\0', index);
            return lastIndex > 0 ?
                zoneAbbreviations.Substring(index, lastIndex - index) :
                zoneAbbreviations.Substring(index);
        }

        // Converts an array of bytes into an int - always using standard byte order (Big Endian)
        // per TZif file standard
        private static unsafe int TZif_ToInt32(byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
            }
        }

        // Converts an array of bytes into a long - always using standard byte order (Big Endian)
        // per TZif file standard
        private static unsafe long TZif_ToInt64(byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                return (uint)i2 | ((long)i1 << 32);
            }
        }

        private static long TZif_ToUnixTime(byte[] value, int startIndex, TZVersion version) =>
            version != TZVersion.V1 ?
                TZif_ToInt64(value, startIndex) :
                TZif_ToInt32(value, startIndex);

        private static DateTime TZif_UnixTimeToDateTime(long unixTime) =>
            unixTime < DateTimeOffset.UnixMinSeconds ? DateTime.MinValue :
            unixTime > DateTimeOffset.UnixMaxSeconds ? DateTime.MaxValue :
            DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;

        private static void TZif_ParseRaw(byte[] data, out TZifHead t, out DateTime[] dts, out byte[] typeOfLocalTime, out TZifType[] transitionType,
                                          out string zoneAbbreviations, out bool[] StandardTime, out bool[] GmtTime, out string? futureTransitionsPosixFormat)
        {
            // initialize the out parameters in case the TZifHead ctor throws
            dts = null!;
            typeOfLocalTime = null!;
            transitionType = null!;
            zoneAbbreviations = string.Empty;
            StandardTime = null!;
            GmtTime = null!;
            futureTransitionsPosixFormat = null;

            // read in the 44-byte TZ header containing the count/length fields
            //
            int index = 0;
            t = new TZifHead(data, index);
            index += TZifHead.Length;

            int timeValuesLength = 4; // the first version uses 4-bytes to specify times
            if (t.Version != TZVersion.V1)
            {
                // move index past the V1 information to read the V2 information
                index += (int)((timeValuesLength * t.TimeCount) + t.TimeCount + (6 * t.TypeCount) + ((timeValuesLength + 4) * t.LeapCount) + t.IsStdCount + t.IsGmtCount + t.CharCount);

                // read the V2 header
                t = new TZifHead(data, index);
                index += TZifHead.Length;
                timeValuesLength = 8; // the second version uses 8-bytes
            }

            // initialize the containers for the rest of the TZ data
            dts = new DateTime[t.TimeCount];
            typeOfLocalTime = new byte[t.TimeCount];
            transitionType = new TZifType[t.TypeCount];
            zoneAbbreviations = string.Empty;
            StandardTime = new bool[t.TypeCount];
            GmtTime = new bool[t.TypeCount];

            // read in the UTC transition points and convert them to Windows
            //
            for (int i = 0; i < t.TimeCount; i++)
            {
                long unixTime = TZif_ToUnixTime(data, index, t.Version);
                dts[i] = TZif_UnixTimeToDateTime(unixTime);
                index += timeValuesLength;
            }

            // read in the Type Indices; there is a 1:1 mapping of UTC transition points to Type Indices
            // these indices directly map to the array index in the transitionType array below
            //
            for (int i = 0; i < t.TimeCount; i++)
            {
                typeOfLocalTime[i] = data[index];
                index++;
            }

            // read in the Type table.  Each 6-byte entry represents
            // {UtcOffset, IsDst, AbbreviationIndex}
            //
            // each AbbreviationIndex is a character index into the zoneAbbreviations string below
            //
            for (int i = 0; i < t.TypeCount; i++)
            {
                transitionType[i] = new TZifType(data, index);
                index += 6;
            }

            // read in the Abbreviation ASCII string.  This string will be in the form:
            // "PST\0PDT\0PWT\0\PPT"
            //
            Encoding enc = Encoding.UTF8;
            zoneAbbreviations = enc.GetString(data, index, (int)t.CharCount);
            index += (int)t.CharCount;

            // skip ahead of the Leap-Seconds Adjustment data.  In a future release, consider adding
            // support for Leap-Seconds
            //
            index += (int)(t.LeapCount * (timeValuesLength + 4)); // skip the leap second transition times

            // read in the Standard Time table.  There should be a 1:1 mapping between Type-Index and Standard
            // Time table entries.
            //
            // TRUE     =     transition time is standard time
            // FALSE    =     transition time is wall clock time
            // ABSENT   =     transition time is wall clock time
            //
            for (int i = 0; i < t.IsStdCount && i < t.TypeCount && index < data.Length; i++)
            {
                StandardTime[i] = (data[index++] != 0);
            }

            // read in the GMT Time table.  There should be a 1:1 mapping between Type-Index and GMT Time table
            // entries.
            //
            // TRUE     =     transition time is UTC
            // FALSE    =     transition time is local time
            // ABSENT   =     transition time is local time
            //
            for (int i = 0; i < t.IsGmtCount && i < t.TypeCount && index < data.Length; i++)
            {
                GmtTime[i] = (data[index++] != 0);
            }

            if (t.Version != TZVersion.V1)
            {
                // read the POSIX-style format, which should be wrapped in newlines with the last newline at the end of the file
                if (data[index++] == '\n' && data[data.Length - 1] == '\n')
                {
                    futureTransitionsPosixFormat = enc.GetString(data, index, data.Length - index - 1);
                }
            }
        }

        /// <summary>
        /// Normalize adjustment rule offset so that it is within valid range
        /// This method should not be called at all but is here in case something changes in the future
        /// or if really old time zones are present on the OS (no combination is known at the moment)
        /// </summary>
        private static void NormalizeAdjustmentRuleOffset(TimeSpan baseUtcOffset, [NotNull] ref AdjustmentRule adjustmentRule)
        {
            // Certain time zones such as:
            //       Time Zone  start date  end date    offset
            // -----------------------------------------------------
            // America/Yakutat  0001-01-01  1867-10-18   14:41:00
            // America/Yakutat  1867-10-18  1900-08-20   14:41:00
            // America/Sitka    0001-01-01  1867-10-18   14:58:00
            // America/Sitka    1867-10-18  1900-08-20   14:58:00
            // Asia/Manila      0001-01-01  1844-12-31  -15:56:00
            // Pacific/Guam     0001-01-01  1845-01-01  -14:21:00
            // Pacific/Saipan   0001-01-01  1845-01-01  -14:21:00
            //
            // have larger offset than currently supported by framework.
            // If for whatever reason we find that time zone exceeding max
            // offset of 14h this function will truncate it to the max valid offset.
            // Updating max offset may cause problems with interacting with SQL server
            // which uses SQL DATETIMEOFFSET field type which was originally designed to be
            // bit-for-bit compatible with DateTimeOffset.

            TimeSpan utcOffset = GetUtcOffset(baseUtcOffset, adjustmentRule);

            // utc base offset delta increment
            TimeSpan adjustment = TimeSpan.Zero;

            if (utcOffset > MaxOffset)
            {
                adjustment = MaxOffset - utcOffset;
            }
            else if (utcOffset < MinOffset)
            {
                adjustment = MinOffset - utcOffset;
            }

            if (adjustment != TimeSpan.Zero)
            {
                adjustmentRule = AdjustmentRule.CreateAdjustmentRule(
                    adjustmentRule.DateStart,
                    adjustmentRule.DateEnd,
                    adjustmentRule.DaylightDelta,
                    adjustmentRule.DaylightTransitionStart,
                    adjustmentRule.DaylightTransitionEnd,
                    adjustmentRule.BaseUtcOffsetDelta + adjustment,
                    adjustmentRule.NoDaylightTransitions);
            }
        }

        private struct TZifType
        {
            public const int Length = 6;

            public readonly TimeSpan UtcOffset;
            public readonly bool IsDst;
            public readonly byte AbbreviationIndex;

            public TZifType(byte[] data, int index)
            {
                if (data == null || data.Length < index + Length)
                {
                    throw new ArgumentException(SR.Argument_TimeZoneInfoInvalidTZif, nameof(data));
                }
                UtcOffset = new TimeSpan(0, 0, TZif_ToInt32(data, index + 00));
                IsDst = (data[index + 4] != 0);
                AbbreviationIndex = data[index + 5];
            }
        }

        private struct TZifHead
        {
            public const int Length = 44;

            public readonly uint Magic; // TZ_MAGIC "TZif"
            public readonly TZVersion Version; // 1 byte for a \0 or 2 or 3
            // public byte[15] Reserved; // reserved for future use
            public readonly uint IsGmtCount; // number of transition time flags
            public readonly uint IsStdCount; // number of transition time flags
            public readonly uint LeapCount; // number of leap seconds
            public readonly uint TimeCount; // number of transition times
            public readonly uint TypeCount; // number of local time types
            public readonly uint CharCount; // number of abbreviated characters

            public TZifHead(byte[] data, int index)
            {
                if (data == null || data.Length < Length)
                {
                    throw new ArgumentException("bad data", nameof(data));
                }

                Magic = (uint)TZif_ToInt32(data, index + 00);

                if (Magic != 0x545A6966)
                {
                    // 0x545A6966 = {0x54, 0x5A, 0x69, 0x66} = "TZif"
                    throw new ArgumentException(SR.Argument_TimeZoneInfoBadTZif, nameof(data));
                }

                byte version = data[index + 04];
                Version =
                    version == '2' ? TZVersion.V2 :
                    version == '3' ? TZVersion.V3 :
                    TZVersion.V1;  // default/fallback to V1 to guard against future, unsupported version numbers

                // skip the 15 byte reserved field

                // don't use the BitConverter class which parses data
                // based on the Endianess of the machine architecture.
                // this data is expected to always be in "standard byte order",
                // regardless of the machine it is being processed on.

                IsGmtCount = (uint)TZif_ToInt32(data, index + 20);
                IsStdCount = (uint)TZif_ToInt32(data, index + 24);
                LeapCount = (uint)TZif_ToInt32(data, index + 28);
                TimeCount = (uint)TZif_ToInt32(data, index + 32);
                TypeCount = (uint)TZif_ToInt32(data, index + 36);
                CharCount = (uint)TZif_ToInt32(data, index + 40);
            }
        }

        private enum TZVersion : byte
        {
            V1 = 0,
            V2,
            V3,
            // when adding more versions, ensure all the logic using TZVersion is still correct
        }
    }
}
