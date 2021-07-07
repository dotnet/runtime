// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string _timeZoneFileName = "tzdata";

        private static AndroidTzData? s_tzData;
        private static readonly object s_tzDataLock = new object();

        private static AndroidTzData s_atzData
        {
            get
            {
                if (s_tzData == null)
                {
                    lock (s_tzDataLock)
                    {
                        if (s_tzData == null)
                        {
                            s_tzData = new AndroidTzData();
                        }
                    }
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
                byte[] buffer = s_atzData.GetTimeZoneData(name);
                return GetTimeZoneFromTzData(buffer, id);
            }
            catch
            {
                // How should we handle
                return null;
            }
        }

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

            // If we can't find a default time zone, return UTC
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

        private static string GetTimeZoneDirectory()
        {
            // Android 10+, TimeData module where the updates land
            if (File.Exists(Path.Combine(GetApexTimeDataRoot() + "/etc/tz/", _timeZoneFileName)))
            {
                return GetApexTimeDataRoot() + "/etc/tz/";
            }
            // Android 10+, Fallback location if the above isn't found or corrupted
            if (File.Exists(Path.Combine(GetApexRuntimeRoot() + "/etc/tz/", _timeZoneFileName)))
            {
                return GetApexRuntimeRoot() + "/etc/tz/";
            }
            if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("ANDROID_DATA") + "/misc/zoneinfo/", _timeZoneFileName)))
            {
                return Environment.GetEnvironmentVariable("ANDROID_DATA") + "/misc/zoneinfo/";
            }

            return Environment.GetEnvironmentVariable("ANDROID_ROOT") + _defaultTimeZoneDirectory;
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachineCore(string id, out TimeZoneInfo? value, out Exception? e)
        {
            value = null;
            e = null;

            value = id == LocalId ? GetLocalTimeZoneCore() : GetTimeZone(id, id);

            if (value == null)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, GetTimeZoneDirectory() + _timeZoneFileName));
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }

            return TimeZoneInfoResult.Success;
        }

        private static string[] GetTimeZoneIds()
        {
            return s_atzData.GetTimeZoneIds();
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
                public fixed byte signature[12];
                public int indexOffset;
                public int dataOffset;
            }

            [StructLayout(LayoutKind.Sequential, Pack=1)]
            private unsafe struct AndroidTzDataEntry
            {
                public fixed byte id[40];
                public int byteOffset;
                public int length;
                public int rawUtcOffset;
            }

            private string[]? _ids;
            private int[]? _byteOffsets;
            private int[]? _lengths;

            public AndroidTzData()
            {
                ReadHeader(GetTimeZoneDirectory() + _timeZoneFileName);
            }

            private unsafe void ReadHeader(string tzFilePath)
            {
                int size   = Math.Max(Marshal.SizeOf(typeof(AndroidTzDataHeader)), Marshal.SizeOf(typeof(AndroidTzDataEntry)));
                var buffer = new byte[size];
                AndroidTzDataHeader header = ReadAt<AndroidTzDataHeader>(tzFilePath, 0, buffer);

                header.indexOffset = NetworkToHostOrder(header.indexOffset);
                header.dataOffset = NetworkToHostOrder(header.dataOffset);

                sbyte* s = (sbyte*)header.signature;
                string magic = new string(s, 0, 6, Encoding.ASCII);

                if (magic != "tzdata" || header.signature[11] != 0)
                {
                    var b = new StringBuilder();
                    b.Append("bad tzdata magic:");
                    for (int i = 0; i < 12; ++i) {
                        b.Append(' ').Append(((byte)s[i]).ToString("x2"));
                    }

                    //TODO: Put strings in resource file
                    throw new InvalidOperationException("bad tzdata magic: " + b.ToString());
                }
                // What exactly are we considering bad tzdata? Seems like if it doesnt start with "tzdata" or if the signature is filled.
                // How does filling the AndroidTzDataHeader work? Shouldn't signature be filled up, so its always != 0?

                ReadIndex(tzFilePath, header.indexOffset, header.dataOffset, buffer);
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2087:UnrecognizedReflectionPattern",
                Justification = "Implementation detail of Android TimeZone")]
            private unsafe T ReadAt<T>(string tzFilePath, long position, byte[] buffer)
                where T : struct
            {
                int size = Marshal.SizeOf(typeof(T));
                if (buffer.Length < size)
                {
                    //TODO: Put strings in resource file
                    throw new InvalidOperationException("private error: buffer too small");
                }

                using (FileStream fs = File.OpenRead(tzFilePath))
                {
                    fs.Position = position;
                    int numBytesRead;
                    if ((numBytesRead = fs.Read(buffer, 0, size)) < size)
                    {
                        //TODO: Put strings in resource file
                        throw new InvalidOperationException(string.Format("Error reading '{0}': read {1} bytes, expected {2}", tzFilePath, numBytesRead, size));
                    }

                    fixed (byte* b = buffer)
                    {
                        return (T)Marshal.PtrToStructure((IntPtr)b, typeof(T))!; // Is ! the right way to handle Unboxing a possibly null value. Should there be some check instead?
                    }
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

            private unsafe void ReadIndex(string tzFilePath, int indexOffset, int dataOffset, byte[] buffer)
            {
                int indexSize = dataOffset - indexOffset;
                int entrySize = Marshal.SizeOf(typeof(AndroidTzDataEntry));
                int entryCount = indexSize / entrySize;

                _byteOffsets = new int[entryCount];
                _ids = new string[entryCount];
                _lengths = new int[entryCount];

                for (int i = 0; i < entryCount; ++i)
                {
                    AndroidTzDataEntry entry = ReadAt<AndroidTzDataEntry>(tzFilePath, indexOffset + (entrySize*i), buffer);
                    var p = (sbyte*)entry.id;

                    _byteOffsets![i] = NetworkToHostOrder(entry.byteOffset) + dataOffset;
                    _ids![i] = new string(p, 0, GetStringLength(p, 40), Encoding.ASCII);
                    _lengths![i] = NetworkToHostOrder(entry.length);

                    if (_lengths![i] < Marshal.SizeOf(typeof(AndroidTzDataHeader)))
                    {
                        //TODO: Put strings in resource file
                        throw new InvalidOperationException("Length in index file < sizeof(tzhead)");
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
                    //TODO: Put strings in resource file
                    throw new InvalidOperationException("Error finding the timezone id");
                }

                int offset = _byteOffsets![i];
                int length = _lengths![i];
                var buffer = new byte[length];
                // Do we need to lock to prevent multithreading issues like the mono/mono implementation?
                string tzFilePath = GetTimeZoneDirectory() + _timeZoneFileName;
                using (FileStream fs = File.OpenRead(tzFilePath))
                {
                    fs.Position = offset;
                    int numBytesRead;
                    if ((numBytesRead = fs.Read(buffer, 0, buffer.Length)) < buffer.Length)
                    {
                        //TODO: Put strings in resource file
                        throw new InvalidOperationException(string.Format("Unable to fully read from file '{0}' at offset {1} length {2}; read {3} bytes expected {4}.", tzFilePath, offset, length, numBytesRead, buffer.Length));
                    }
                }

                return buffer;
            }
        }
    }
}