// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string TimeZoneFileName = "tzdata";

        private static AndroidTzData? s_tzData;

        private static AndroidTzData AndroidTzDataInstance
        {
            get
            {
                if (s_tzData == null)
                {
                    Interlocked.CompareExchange(ref s_tzData, new AndroidTzData(), null);
                }

                return s_tzData;
            }
        }

        // This should be called when name begins with GMT
        private static int ParseGMTNumericZone(string name)
        {
            int sign;
            if (name[3] == '+')
            {
                sign = 1;
            }
            else if (name[3] == '-')
            {
                sign = -1;
            }
            else
            {
                return 0;
            }

            int where;
            int hour = 0;
            bool colon = false;
            for (where = 4; where < name.Length; where++)
            {
                char c = name[where];

                if (c == ':')
                {
                    where++;
                    colon = true;
                    break;
                }

                if (char.IsAsciiDigit(c))
                {
                    hour = hour * 10 + c - '0';
                }
                else
                {
                    return 0;
                }
            }

            int min = 0;
            for (; where < name.Length; where++)
            {
                char c = name [where];

                if (char.IsAsciiDigit(c))
                {
                    min = min * 10 + c - '0';
                }
                else
                {
                    return 0;
                }
            }

            if (colon)
            {
                return sign * (hour * 60 + min) * 60;
            }
            else if (hour >= 100)
            {
                return sign * ((hour / 100) * 60 + (hour % 100)) * 60;
            }
            else
            {
                return sign * (hour * 60) * 60;
            }
        }

        private static TimeZoneInfo? GetTimeZone(string id, string name)
        {
            if (name == "GMT" || name == "UTC")
            {
                return new TimeZoneInfo(id, TimeSpan.FromSeconds(0), id, name, name, null, disableDaylightSavingTime:true);
            }
            if (name.StartsWith("GMT", StringComparison.Ordinal))
            {
                return new TimeZoneInfo(id, TimeSpan.FromSeconds(ParseGMTNumericZone(name)), id, name, name, null, disableDaylightSavingTime:true);
            }

            try
            {
                byte[] buffer = AndroidTzDataInstance.GetTimeZoneData(name);
                return GetTimeZoneFromTzData(buffer, id);
            }
            catch
            {
                return null;
            }
        }

        // Core logic to retrieve the local system time zone.
        // Obtains Android's system local time zone id to get the corresponding time zone
        // Defaults to Utc if local time zone cannot be found
        private static TimeZoneInfo GetLocalTimeZoneCore()
        {
            string? id = Interop.Sys.GetDefaultTimeZone();
            if (!string.IsNullOrEmpty(id))
            {
                TimeZoneInfo? defaultTimeZone = GetTimeZone(id, id);

                if (defaultTimeZone != null)
                {
                    return defaultTimeZone;
                }
            }

            return Utc;
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachineCore(string id, out TimeZoneInfo? value, out Exception? e)
        {

            value = id == LocalId ? GetLocalTimeZoneCore() : GetTimeZone(id, id);

            if (value == null)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, AndroidTzDataInstance.GetTimeZoneDirectory() + TimeZoneFileName));
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }

            e = null;
            return TimeZoneInfoResult.Success;
        }

        private static string[] GetTimeZoneIds()
        {
            return AndroidTzDataInstance.GetTimeZoneIds();
        }

        /*
        * Android v4.3 Timezone support infrastructure.
        *
        * Android tzdata files are found in the format of
        * Header <Beginning of Entry Index> Entry Entry Entry ... Entry <Beginning of Data Index> <TZDATA>
        *
        * https://github.com/aosp-mirror/platform_bionic/blob/master/libc/tzcode/bionic.cpp
        *
        * The header (24 bytes) contains the following information
        * signature - 12 bytes of the form "tzdata2012f\0" where 2012f is subject to change
        * index offset - 4 bytes that denotes the offset at which the index of the tzdata file starts
        * data offset - 4 bytes that denotes the offset at which the data of the tzdata file starts
        * final offset - 4 bytes that used to denote the final offset, which we don't use but will note.
        *
        * Each Data Entry (52 bytes) can be used to generate a TimeZoneInfo and contain the following information
        * id - 40 bytes that contain the id of the time zone data entry timezone<id>
        * byte offset - 4 bytes that denote the offset from the data offset timezone<id> data can be found
        * length - 4 bytes that denote the length of the data for timezone<id>
        * unused - 4 bytes that used to be raw GMT offset, but now is always 0 since tzdata2014f (L).
        *
        * This is needed in order to read Android v4.3 tzdata files.
        *
        * Android 10+ moved the up-to-date tzdata location to a module updatable via the Google Play Store and the
        * database location changed (https://source.android.com/devices/architecture/modular-system/runtime#time-zone-data-interactions)
        * The older locations still exist (at least the `/system/usr/share/zoneinfo` one) but they won't be updated.
        */
        private sealed class AndroidTzData
        {
            private string[] _ids;
            private int[] _byteOffsets;
            private int[] _lengths;
            private bool[] _isBackwards;
            private string _tzFileDir;
            private string _tzFilePath;

            private static string GetApexTimeDataRoot()
            {
                string? ret = Environment.GetEnvironmentVariable("ANDROID_TZDATA_ROOT");
                if (!string.IsNullOrEmpty(ret))
                {
                    return ret;
                }

                return "/apex/com.android.tzdata";
            }

            private static string GetApexRuntimeRoot()
            {
                string? ret = Environment.GetEnvironmentVariable("ANDROID_RUNTIME_ROOT");
                if (!string.IsNullOrEmpty(ret))
                {
                    return ret;
                }

                return "/apex/com.android.runtime";
            }

            public AndroidTzData()
            {
                // On Android, time zone data is found in tzdata
                // Based on https://github.com/mono/mono/blob/main/mcs/class/corlib/System/TimeZoneInfo.Android.cs
                // Also follows the locations found at the bottom of https://github.com/aosp-mirror/platform_bionic/blob/master/libc/tzcode/bionic.cpp
                string[] tzFileDirList = new string[] {GetApexTimeDataRoot() + "/etc/tz/", // Android 10+, TimeData module where the updates land
                                                       GetApexRuntimeRoot() + "/etc/tz/", // Android 10+, Fallback location if the above isn't found or corrupted
                                                       Environment.GetEnvironmentVariable("ANDROID_DATA") + "/misc/zoneinfo/",
                                                       Environment.GetEnvironmentVariable("ANDROID_ROOT") + DefaultTimeZoneDirectory};
                foreach (var tzFileDir in tzFileDirList)
                {
                    string tzFilePath = Path.Combine(tzFileDir, TimeZoneFileName);
                    if (LoadData(tzFileDir, tzFilePath))
                    {
                        _tzFileDir = tzFileDir;
                        _tzFilePath = tzFilePath;
                        return;
                    }
                }

                throw new TimeZoneNotFoundException(SR.TimeZoneNotFound_ValidTimeZoneFileMissing);
            }

            // On some versions of Android, the tzdata file may still contain backward timezone ids.
            // We attempt to use tzlookup.xml, which is available on some versions of Android to help
            // validate non-backward timezone ids
            // tzlookup.xml is an autogenerated file that contains timezone ids in this form:
            //
            // <timezones ianaversion="2019b">
            //   <countryzones>
            //     <country code="au" default="Australia/Sydney" everutc="n">
            //       <id alts="Australia/ACT,Australia/Canberra,Australia/NSW">Australia/Sydney</id>
            //       ...
            //       ...
            //       <id>Australia/Eucla</id>
            //     </country>
            //     <country ...>
            //       ...
            //       ...
            //       ...
            //     </country>
            //   </countryzones>
            // </timezones>
            //
            // Once the timezone cache is populated with the IDs, we reference tzlookup id tags
            // to determine if an id is backwards and label it as such if they are.
            private static void FilterBackwardIDs(string tzFileDir, out HashSet<string> tzLookupIDs)
            {
                tzLookupIDs = new HashSet<string>();
                try
                {
                    using (StreamReader sr = File.OpenText(Path.Combine(tzFileDir, "tzlookup.xml")))
                    {
                        string? tzLookupLine;
                        while (sr.Peek() >= 0)
                        {
                            if (!(tzLookupLine = sr.ReadLine())!.AsSpan().TrimStart().StartsWith("<id", StringComparison.Ordinal))
                                continue;

                            int idStart = tzLookupLine!.IndexOf('>') + 1;
                            int idLength = tzLookupLine.LastIndexOf("</", StringComparison.Ordinal) - idStart;
                            if (idStart <= 0 || idLength < 0)
                            {
                                // Either the start tag <id ... > or the end tag </id> are not found
                                continue;
                            }
                            string id = tzLookupLine.Substring(idStart, idLength);
                            tzLookupIDs.Add(id);
                        }
                    }
                }
                catch {}
            }

            [MemberNotNullWhen(true, nameof(_ids))]
            [MemberNotNullWhen(true, nameof(_byteOffsets))]
            [MemberNotNullWhen(true, nameof(_lengths))]
            [MemberNotNullWhen(true, nameof(_isBackwards))]
            private bool LoadData(string tzFileDir, string path)
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                try
                {
                    using (FileStream fs = File.OpenRead(path))
                    {
                        LoadTzFile(tzFileDir, fs);
                    }
                    return true;
                }
                catch {}

                return false;
            }

            [MemberNotNull(nameof(_ids))]
            [MemberNotNull(nameof(_byteOffsets))]
            [MemberNotNull(nameof(_lengths))]
            [MemberNotNull(nameof(_isBackwards))]
            private void LoadTzFile(string tzFileDir, Stream fs)
            {
                const int HeaderSize = 24;
                Span<byte> buffer = stackalloc byte[HeaderSize];

                ReadTzDataIntoBuffer(fs, 0, buffer);

                LoadHeader(buffer, out int indexOffset, out int dataOffset);
                ReadIndex(tzFileDir, fs, indexOffset, dataOffset);
            }

            private static void LoadHeader(Span<byte> buffer, out int indexOffset, out int dataOffset)
            {
                // tzdata files are expected to start with the form of "tzdata2012f\0" depending on the year of the tzdata used which is 2012 in this example
                // since we're not differentiating on year, check for tzdata and the ending \0
                var tz = (ushort)TZif_ToInt16(buffer.Slice(0, 2));
                var data = (uint)TZif_ToInt32(buffer.Slice(2, 4));

                if (tz != 0x747A || data != 0x64617461 || buffer[11] != 0)
                {
                    // 0x747A  0x64617461 = {0x74, 0x7A} {0x64, 0x61, 0x74, 0x61} = "tz" "data"
                    var b = new StringBuilder(buffer.Length);
                    for (int i = 0; i < 12; ++i)
                    {
                        b.Append(' ').Append(HexConverter.ToCharLower(buffer[i]));
                    }

                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadTZHeader, TimeZoneFileName, b.ToString()));
                }

                indexOffset = TZif_ToInt32(buffer.Slice(12, 4));
                dataOffset = TZif_ToInt32(buffer.Slice(16, 4));
            }

            [MemberNotNull(nameof(_ids))]
            [MemberNotNull(nameof(_byteOffsets))]
            [MemberNotNull(nameof(_lengths))]
            [MemberNotNull(nameof(_isBackwards))]
            private void ReadIndex(string tzFileDir, Stream fs, int indexOffset, int dataOffset)
            {
                int indexSize = dataOffset - indexOffset;
                const int entrySize = 52; // Data entry size
                int entryCount = indexSize / entrySize;
                _byteOffsets = new int[entryCount];
                _ids = new string[entryCount];
                _lengths = new int[entryCount];
                _isBackwards = new bool[entryCount];
                FilterBackwardIDs(tzFileDir, out HashSet<string> tzLookupIDs);
                for (int i = 0; i < entryCount; ++i)
                {
                    LoadEntryAt(fs, indexOffset + (entrySize*i), out string id, out int byteOffset, out int length);

                    _byteOffsets[i] = byteOffset + dataOffset;
                    _ids[i] = id;
                    _lengths[i] = length;
                    _isBackwards[i] = !tzLookupIDs.Contains(id);

                    if (length < 24) // Header Size
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_BadIndexLength);
                    }
                }
            }

            private void ReadTzDataIntoBuffer(Stream fs, long position, Span<byte> buffer)
            {
                fs.Position = position;

                int bytesRead = 0;
                int bytesLeft = buffer.Length;

                while (bytesLeft > 0)
                {
                    int b = fs.Read(buffer.Slice(bytesRead));
                    if (b == 0)
                    {
                        break;
                    }

                    bytesRead += b;
                    bytesLeft -= b;
                }

                if (bytesLeft != 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ReadTZError, _tzFilePath, position, buffer.Length, bytesRead, buffer.Length));
                }
            }

            private void LoadEntryAt(Stream fs, long position, out string id, out int byteOffset, out int length)
            {
                const int size = 52; // data entry size
                Span<byte> entryBuffer = stackalloc byte[size];

                ReadTzDataIntoBuffer(fs, position, entryBuffer);

                int index = 0;
                while (entryBuffer[index] != 0 && index < 40)
                {
                    index += 1;
                }
                id = Encoding.UTF8.GetString(entryBuffer.Slice(0, index));
                byteOffset = TZif_ToInt32(entryBuffer.Slice(40, 4));
                length = TZif_ToInt32(entryBuffer.Slice(44, 4));
            }

            public string[] GetTimeZoneIds()
            {
                int numTimeZoneIDs = 0;
                for (int i = 0; i < _ids.Length; i++)
                {
                    if (!_isBackwards[i])
                    {
                        numTimeZoneIDs++;
                    }
                }
                string[] nonBackwardsTZIDs = new string[numTimeZoneIDs];
                var index = 0;
                for (int i = 0; i < _ids.Length; i++)
                {
                    if (!_isBackwards[i])
                    {
                        nonBackwardsTZIDs[index] = _ids[i];
                        index++;
                    }
                }
                return nonBackwardsTZIDs;
            }

            public string GetTimeZoneDirectory()
            {
                return _tzFilePath;
            }

            public byte[] GetTimeZoneData(string id)
            {
                int i = Array.BinarySearch(_ids, id, StringComparer.Ordinal);
                if (i < 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.TimeZoneNotFound_MissingData, id));
                }

                int offset = _byteOffsets[i];
                int length = _lengths[i];
                byte[] buffer = new byte[length];

                using (FileStream fs = File.OpenRead(_tzFilePath))
                {
                    ReadTzDataIntoBuffer(fs, offset, buffer);
                }

                return buffer;
            }
        }
    }
}
