// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                Interlocked.CompareExchange(ref s_tzData, new AndroidTzData(), null);

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

                if (c >= '0' && c <= '9')
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

                if (c >= '0' && c <= '9')
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

        // On Android, time zone data is found in tzdata
        // Based on https://github.com/mono/mono/blob/main/mcs/class/corlib/System/TimeZoneInfo.Android.cs
        // Also follows the locations found at the bottom of https://github.com/aosp-mirror/platform_bionic/blob/master/libc/tzcode/bionic.cpp
        private static string GetTimeZoneDirectory()
        {
            string apexTzDataFileDir = GetApexTimeDataRoot() + "/etc/tz/";
            // Android 10+, TimeData module where the updates land
            if (File.Exists(Path.Combine(apexTzDataFileDir, TimeZoneFileName)))
            {
                return apexTzDataFileDir;
            }
            string apexRuntimeFileDir = GetApexRuntimeRoot() + "/etc/tz/";
            // Android 10+, Fallback location if the above isn't found or corrupted
            if (File.Exists(Path.Combine(apexRuntimeFileDir, TimeZoneFileName)))
            {
                return apexRuntimeFileDir;
            }
            string androidDataFileDir = Environment.GetEnvironmentVariable("ANDROID_DATA") + "/misc/zoneinfo/";
            if (File.Exists(Path.Combine(androidDataFileDir, TimeZoneFileName)))
            {
                return androidDataFileDir;
            }

            return Environment.GetEnvironmentVariable("ANDROID_ROOT") + DefaultTimeZoneDirectory;
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachineCore(string id, out TimeZoneInfo? value, out Exception? e)
        {
            value = id == LocalId ? GetLocalTimeZoneCore() : GetTimeZone(id, id);

            if (value == null)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, GetTimeZoneDirectory() + TimeZoneFileName));
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
        * This is a C# port of libcore.util.ZoneInfoDB:
        *
        *    https://android.googlesource.com/platform/libcore/+/master/luni/src/main/java/libcore/util/ZoneInfoDB.java
        *
        * This is needed in order to read Android v4.3 tzdata files.
        *
        * Android 10+ moved the up-to-date tzdata location to a module updatable via the Google Play Store and the
        * database location changed (https://source.android.com/devices/architecture/modular-system/runtime#time-zone-data-interactions)
        * The older locations still exist (at least the `/system/usr/share/zoneinfo` one) but they won't be updated.
        */
        private sealed class AndroidTzData
        {
            [StructLayout(LayoutKind.Sequential, Pack=1)]
            private unsafe struct AndroidTzDataHeader
            {
                public fixed byte signature[12]; // "tzdata2012f\0"
                public int indexOffset;
                public int dataOffset;
                public int finalOffset;
            }

            [StructLayout(LayoutKind.Sequential, Pack=1)]
            private unsafe struct AndroidTzDataEntry
            {
                public fixed byte id[40];
                public int byteOffset;
                public int length;
                public int unused; // Was raw GMT offset; always 0 since tzdata2014f (L).
            }

            private string[] _ids;
            private int[] _byteOffsets;
            private int[] _lengths;

            public AndroidTzData()
            {
                string tzFilePath = GetTimeZoneDirectory() + TimeZoneFileName;
                using (FileStream fs = File.OpenRead(tzFilePath))
                {
                    ReadHeader(tzFilePath, fs);
                }
            }

            [MemberNotNull(nameof(_ids))]
            [MemberNotNull(nameof(_byteOffsets))]
            [MemberNotNull(nameof(_lengths))]
            private unsafe void ReadHeader(string tzFilePath, Stream fs)
            {
                int size = Math.Max(sizeof(AndroidTzDataHeader)), sizeof(AndroidTzDataEntry)));
                Span<byte> buffer = stackalloc byte[size];
                AndroidTzDataHeader header = ReadAt<AndroidTzDataHeader>(tzFilePath, fs, 0, buffer);

                header.indexOffset = NetworkToHostOrder(header.indexOffset);
                header.dataOffset = NetworkToHostOrder(header.dataOffset);

                // tzdata files are expected to start with the form of "tzdata2012f\0" depending on the year of the tzdata used which is 2012 in this example
                if (header.signature[0] != (byte)'t' || header.signature[1] != (byte)'z' || header.signature[2] != (byte)'d' || header.signature[3] != (byte)'a' || header.signature[4] != (byte)'t' || header.signature[5] != (byte)'a' || header.signature[11] != 0)
                {
                    var b = new StringBuilder(buffer.Length);
                    for (int i = 0; i < 12; ++i) {
                        b.Append(' ').Append(HexConverter.ToCharLower(buffer[i]));
                    }

                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadTZHeader, tzFilePath, b.ToString()));
                }

                ReadIndex(tzFilePath, fs, header.indexOffset, header.dataOffset, buffer);
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2087:UnrecognizedReflectionPattern",
                Justification = "Implementation detail of Android TimeZone")]
            private unsafe T ReadAt<T>(string tzFilePath, Stream fs, long position, Span<byte> buffer)
                where T : struct
            {
                int size = sizeof(T));
                Debug.Assert(buffer.Length >= size);

                fs.Position = position;
                int numBytesRead;
                if ((numBytesRead = fs.Read(buffer)) < size)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ReadTZError, tzFilePath, position, size, numBytesRead, size));
                }

                fixed (byte* b = buffer)
                {
                    return (T)Marshal.PtrToStructure((IntPtr)b, typeof(T))!;
                }
            }

            private static int NetworkToHostOrder(int value)
            {
                if (!BitConverter.IsLittleEndian)
                    return value;

                return (((value >> 24) & 0xFF) | ((value >> 08) & 0xFF00) | ((value << 08) & 0xFF0000) | ((value << 24)));
            }

            private static unsafe int GetStringLength(sbyte* s, int maxLength)
            {
                int len;
                for (len = 0; len < maxLength; len++, s++)
                {
                    if (*s == 0)
                    {
                        break;
                    }
                }
                return len;
            }

            [MemberNotNull(nameof(_ids))]
            [MemberNotNull(nameof(_byteOffsets))]
            [MemberNotNull(nameof(_lengths))]
            private unsafe void ReadIndex(string tzFilePath, Stream fs, int indexOffset, int dataOffset, Span<byte> buffer)
            {
                int indexSize = dataOffset - indexOffset;
                int entrySize = sizeof(AndroidTzDataEntry));
                int entryCount = indexSize / entrySize;

                _byteOffsets = new int[entryCount];
                _ids = new string[entryCount];
                _lengths = new int[entryCount];

                for (int i = 0; i < entryCount; ++i)
                {
                    AndroidTzDataEntry entry = ReadAt<AndroidTzDataEntry>(tzFilePath, fs, indexOffset + (entrySize*i), buffer);
                    var p = (sbyte*)entry.id;

                    _byteOffsets![i] = NetworkToHostOrder(entry.byteOffset) + dataOffset;
                    _ids![i] = new string(p, 0, GetStringLength(p, 40), Encoding.ASCII);
                    _lengths![i] = NetworkToHostOrder(entry.length);

                    if (_lengths![i] < sizeof(AndroidTzDataHeader)))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_BadIndexLength);
                    }
                }
            }

            public string[] GetTimeZoneIds()
            {
                return _ids!;
            }

            public byte[] GetTimeZoneData(string id)
            {
                int i = Array.BinarySearch(_ids!, id, StringComparer.Ordinal);
                if (i < 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.TimeZoneNotFound_MissingData, id));
                }

                int offset = _byteOffsets![i];
                int length = _lengths![i];
                var buffer = new byte[length];
                string tzFilePath = GetTimeZoneDirectory() + TimeZoneFileName;
                using (FileStream fs = File.OpenRead(tzFilePath))
                {
                    fs.Position = offset;
                    int numBytesRead;
                    if ((numBytesRead = fs.Read(buffer)) < buffer.Length)
                    {
                        throw new InvalidOperationException(string.Format(SR.InvalidOperation_ReadTZError, tzFilePath, offset, length, numBytesRead, buffer.Length));
                    }
                }

                return buffer;
            }
        }
    }
}