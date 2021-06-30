// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string TimeZoneFileName = "tzdata";

        // TODO: Consider restructuring underlying AndroidTimeZones class©.
        // Although it may be easier to work with this way.
        private static TimeZoneInfo GetLocalTimeZoneCore()
        {
            return AndroidTimeZones.Local!;
        }

        private static void PopulateAllSystemTimeZonesCore(CachedData cachedData)
        {
            foreach (string timeZoneId in AndroidTimeZones.GetAvailableIds())
            {
                // cachedData is not in this current context, I think we can push PopulateAllSystemTimeZonesCore back to AllUnix
                // and instead implement how the time zone IDs are obtained in Unix/Android
                TryGetTimeZone(timeZoneId, false, out _, out _, cachedData, alwaysFallbackToLocalMachine: true); // populate the cache
            }
        }

        //TODO: TryGetTimeZoneFromLocalMachine maps to FindSystemTimeZoneByIdCore in mono/mono implementation
        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachineCore(string id, out TimeZoneInfo? value, out Exception? e)
        {
            value = null;
            e = null;

            try
            {
                value = AndroidTimeZones.GetTimeZone(id, id);
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
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, GetTimeZoneDirectory() + TimeZoneFileName), ex);
                return TimeZoneInfoResult.InvalidTimeZoneException;
            }

            if (value == null)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, GetTimeZoneDirectory() + TimeZoneFileName));
                return TimeZoneInfoResult.TimeZoneNotFoundException; // Mono/mono throws TimeZoneNotFoundException, runtime throws InvalidTimeZoneException
            }

            return TimeZoneInfoResult.Success;
        }

        // TODO: Validate you still need these functions / fields.  We should try to isolate the android implementation
        // as much as possible.
        // In other words, mirroring how mono/mono did it is a good first step and then we can walk back what's
        // common with TimeZoneInfo.cs and TimeZoneInfo.AnyUnix.cs
        private static string GetApexTimeDataRoot()
        {
            var ret = Environment.GetEnvironmentVariable("ANDROID_TZDATA_ROOT");
            if (!string.IsNullOrEmpty(ret))
            {
                return ret;
            }

            return "/apex/com.android.tzdata";
        }

        private static string GetApexRuntimeRoot()
        {
            var ret = Environment.GetEnvironmentVariable("ANDROID_RUNTIME_ROOT");
            if (!string.IsNullOrEmpty(ret))
            {
                return ret;
            }

            return "/apex/com.android.runtime";
        }

        internal static readonly string[] Paths = new string[] { GetApexTimeDataRoot() + "/etc/tz/", // Android 10+, TimeData module where the updates land
                                                                 GetApexRuntimeRoot() + "/etc/tz/",  // Android 10+, Fallback location if the above isn't found or corrupted
                                                                 Environment.GetEnvironmentVariable("ANDROID_DATA") + "/misc/zoneinfo/", };

        private static string GetTimeZoneDirectory()
        {
            foreach (var filePath in Paths)
            {
                if (File.Exists(Path.Combine(filePath, TimeZoneFileName)))
                {
                    return filePath;
                }
            }

            return Environment.GetEnvironmentVariable("ANDROID_ROOT") + DefaultTimeZoneDirectory;
        }

        private static class AndroidTimeZones
        {
            private static IAndroidTimeZoneDB? db = GetDefaultTimeZoneDB();

            private static IAndroidTimeZoneDB? GetDefaultTimeZoneDB()
            {
                foreach (var p in AndroidTzData.Paths)
                {
                    if (File.Exists (p))
                    {
                        return new AndroidTzData(AndroidTzData.Paths);
                    }
                }
                //TODO: What should we throw here?
                return null;
            }

            internal static IEnumerable<string> GetAvailableIds()
            {
                return db == null
                    ? Array.Empty<string>()
                    : db.GetAvailableIds();
            }

            private static TimeZoneInfo? _GetTimeZone(string? id, string? name)
            {
                if (db == null)
                {
                    return null;
                }
                byte[] buffer = db.GetTimeZoneData(name);
                if (buffer == null)
                {
                    return null;
                }
                if (string.IsNullOrEmpty(id))
                {
                    return null;
                }

                return GetTimeZoneFromTzData(buffer, id);
            }

            internal static TimeZoneInfo? GetTimeZone(string? id, string? name)
            {
                if (name != null)
                {
                    if (name == "GMT" || name == "UTC")
                    {
                        return new TimeZoneInfo(id!, TimeSpan.FromSeconds(0), id!, name!, name!, null, disableDaylightSavingTime:true);
                    }
                    if (name.StartsWith ("GMT"))
                    {
                        return new TimeZoneInfo (id!,
                                TimeSpan.FromSeconds(ParseNumericZone(name!)),
                                id!, name!, name!, null, disableDaylightSavingTime:true);
                    }
                }

                try
                {
                    return _GetTimeZone(id, name);
                } catch (Exception)
                {
                    return null;
                }
            }

            private static int ParseNumericZone (string? name)
            {
                if (name == null || !name.StartsWith ("GMT") || name.Length <= 3)
                    return 0;

                int sign;
                if (name [3] == '+')
                    sign = 1;
                else if (name [3] == '-')
                    sign = -1;
                else
                    return 0;

                int where;
                int hour = 0;
                bool colon = false;
                for (where = 4; where < name.Length; where++)
                {
                    char c = name [where];

                    if (c == ':')
                    {
                        where++;
                        colon = true;
                        break;
                    }

                    if (c >= '0' && c <= '9')
                        hour = hour * 10 + c - '0';
                    else
                        return 0;
                }

                int min = 0;
                for (; where < name.Length; where++)
                {
                    char c = name [where];

                    if (c >= '0' && c <= '9')
                        min = min * 10 + c - '0';
                    else
                        return 0;
                }

                if (colon)
                    return sign * (hour * 60 + min) * 60;
                else if (hour >= 100)
                    return sign * ((hour / 100) * 60 + (hour % 100)) * 60;
                else
                    return sign * (hour * 60) * 60;
            }

            internal static TimeZoneInfo? Local
            {
                get
                {
                    var id  = GetDefaultTimeZoneName();
                    return GetTimeZone(id, id);
                }
            }

            // TODO: We probably don't need this.  However, if we do, move to Interop
            //
            //[DllImport ("__Internal")]
            //static extern int monodroid_get_system_property (string name, ref IntPtr value);

            // TODO: Move this into Interop
            //
            //[DllImport ("__Internal")]
            //static extern void monodroid_free (IntPtr ptr);

            [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetDefaultTimeZone")]
            internal static extern string GetDefaultTimeZone();

            private static string? GetDefaultTimeZoneName()
            {
                IntPtr value = IntPtr.Zero;
                //int n = 0;
                string? defaultTimeZone  = Environment.GetEnvironmentVariable("__XA_OVERRIDE_TIMEZONE_ID__");

                if (!string.IsNullOrEmpty(defaultTimeZone))
                    return defaultTimeZone;

                // TODO: See how we can test this without hacks
                // Used by the tests
                //if (Environment.GetEnvironmentVariable ("__XA_USE_JAVA_DEFAULT_TIMEZONE_ID__") == null)
                //    n = monodroid_get_system_property ("persist.sys.timezone", ref value);

//                if (n > 0 && value != IntPtr.Zero)
//                {
//                    defaultTimeZone = (Marshal.PtrToStringAnsi(value) ?? String.Empty).Trim();
//                    monodroid_free(value);
//                    if (!String.IsNullOrEmpty(defaultTimeZone))
//                        return defaultTimeZone;
//                }

                // TODO: AndroidPlatform does not exist in runtime.  We need to add an interop call
                //defaultTimeZone = (AndroidPlatform.GetDefaultTimeZone() ?? String.Empty).Trim();
                defaultTimeZone = GetDefaultTimeZone();
                if (!string.IsNullOrEmpty(defaultTimeZone))
                    return defaultTimeZone;

                return null;
            }
        }
    }

    internal interface IAndroidTimeZoneDB
    {
        IEnumerable<string> GetAvailableIds();
        byte[] GetTimeZoneData(string? id);
    }

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    internal unsafe struct AndroidTzDataHeader
    {
        public fixed byte signature [12];
        public int indexOffset;
        public int dataOffset;
        public int zoneTabOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    internal unsafe struct AndroidTzDataEntry
    {
        public fixed byte id [40];
        public int byteOffset;
        public int length;
        public int rawUtcOffset;
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
    internal sealed class AndroidTzData : IAndroidTimeZoneDB
    {

        internal static readonly string[] Paths = new string[] {
            GetApexTimeDataRoot() + "/etc/tz/tzdata", // Android 10+, TimeData module where the updates land
            GetApexRuntimeRoot() + "/etc/tz/tzdata",  // Android 10+, Fallback location if the above isn't found or corrupted
            Environment.GetEnvironmentVariable("ANDROID_DATA") + "/misc/zoneinfo/tzdata",
            Environment.GetEnvironmentVariable("ANDROID_ROOT") + "/usr/share/zoneinfo/tzdata",
        };

        private string tzdataPath;
        private Stream? data;
        private string version = "";
        private string zoneTab = "";

        private string[]? ids;
        private int[]? byteOffsets;
        private int[]? lengths;

        public AndroidTzData (params string[] paths)
        {
            foreach (var path in paths)
            {
                if (LoadData(path))
                {
                    tzdataPath = path;
                    return;
                }
            }

            tzdataPath = "/";
            version = "missing";
            zoneTab = "# Emergency fallback data.\n";
            ids = new[]{ "GMT" };
        }

        public string Version => version;

        public string ZoneTab => zoneTab;

        private static string GetApexTimeDataRoot()
        {
            string? ret = Environment.GetEnvironmentVariable("ANDROID_TZDATA_ROOT");
            if (!string.IsNullOrEmpty (ret!)) {
                return ret!;
            }

            return "/apex/com.android.tzdata";
        }

        private static string GetApexRuntimeRoot()
        {
            string? ret = Environment.GetEnvironmentVariable("ANDROID_RUNTIME_ROOT");
            if (!string.IsNullOrEmpty (ret!))
            {
                return ret!;
            }

            return "/apex/com.android.runtime";
        }

        private bool LoadData(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                data = File.OpenRead(path);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            try
            {
                ReadHeader();
                return true;
            }
            catch
            {
                // log something here instead of the console.
                //Console.Error.WriteLine ("tzdata file \"{0}\" was present but invalid: {1}", path, e);
            }
            return false;
        }

        private unsafe void ReadHeader()
        {
            int size   = Math.Max(Marshal.SizeOf(typeof(AndroidTzDataHeader)), Marshal.SizeOf(typeof(AndroidTzDataEntry)));
            var buffer = new byte[size];
            var header = ReadAt<AndroidTzDataHeader>(0, buffer);

            header.indexOffset = NetworkToHostOrder(header.indexOffset);
            header.dataOffset = NetworkToHostOrder(header.dataOffset);
            header.zoneTabOffset = NetworkToHostOrder(header.zoneTabOffset);

            sbyte* s = (sbyte*)header.signature;
            string magic = new string(s, 0, 6, Encoding.ASCII);

            if (magic != "tzdata" || header.signature[11] != 0)
            {
                var b = new StringBuilder ();
                b.Append ("bad tzdata magic:");
                for (int i = 0; i < 12; ++i) {
                    b.Append(' ').Append(((byte)s[i]).ToString ("x2"));
                }

                //TODO: Put strings in resource file
                throw new InvalidOperationException ("bad tzdata magic: " + b.ToString ());
            }

            version = new string(s, 6, 5, Encoding.ASCII);

            ReadIndex(header.indexOffset, header.dataOffset, buffer);
            ReadZoneTab(header.zoneTabOffset, checked((int)data!.Length) - header.zoneTabOffset);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2087:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of Android TimeZone")]
        private unsafe T ReadAt<T> (long position, byte[] buffer)
            where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            if (buffer.Length < size)
            {
                //TODO: Put strings in resource file
                throw new InvalidOperationException ("Internal error: buffer too small");
            }

            data!.Position = position;
            int r;
            if ((r = data!.Read(buffer, 0, size)) < size)
            {
                //TODO: Put strings in resource file
                throw new InvalidOperationException (
                        string.Format ("Error reading '{0}': read {1} bytes, expected {2}", tzdataPath, r, size));
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

            return
                (((value >> 24) & 0xFF) |
                    ((value >> 08) & 0xFF00) |
                    ((value << 08) & 0xFF0000) |
                    ((value << 24)));
        }

        private unsafe void ReadIndex(int indexOffset, int dataOffset, byte[] buffer)
        {
            int indexSize = dataOffset - indexOffset;
            int entryCount = indexSize / Marshal.SizeOf(typeof(AndroidTzDataEntry));
            int entrySize = Marshal.SizeOf(typeof(AndroidTzDataEntry));

            byteOffsets = new int[entryCount];
            ids = new string[entryCount];
            lengths = new int[entryCount];

            for (int i = 0; i < entryCount; ++i)
            {
                var entry = ReadAt<AndroidTzDataEntry>(indexOffset + (entrySize*i), buffer);
                var p = (sbyte*)entry.id;

                byteOffsets![i] = NetworkToHostOrder(entry.byteOffset) + dataOffset;
                ids![i] = new string(p, 0, GetStringLength(p, 40), Encoding.ASCII);
                lengths![i] = NetworkToHostOrder(entry.length);

                if (lengths![i] < Marshal.SizeOf(typeof(AndroidTzDataHeader)))
                {
                    //TODO: Put strings in resource file
                    throw new InvalidOperationException("Length in index file < sizeof(tzhead)");
                }
            }
        }

        private static unsafe int GetStringLength(sbyte* s, int maxLength)
        {
            int len;
            for (len = 0; len < maxLength; len++, s++)
            {
                if (*s == 0)
                    break;
            }
            return len;
        }

        private unsafe void ReadZoneTab(int zoneTabOffset, int zoneTabSize)
        {
            byte[] ztab = new byte [zoneTabSize];

            data!.Position = zoneTabOffset;

            int r;
            if ((r = data!.Read(ztab, 0, ztab.Length)) < ztab.Length)
            {
                //TODO: Put strings in resource file
                throw new InvalidOperationException(
                        string.Format ("Error reading zonetab: read {0} bytes, expected {1}", r, zoneTabSize));
            }

            zoneTab = Encoding.ASCII.GetString(ztab, 0, ztab.Length);
        }

        public IEnumerable<string> GetAvailableIds()
        {
            return ids!;
        }

        public byte[] GetTimeZoneData(string? id)
        {
            int i = Array.BinarySearch(ids!, id!, StringComparer.Ordinal);
            if (i < 0)
            {
                //TODO: Put strings in resource file
                throw new InvalidOperationException("Error finding the timezone id");
            }

            int offset = byteOffsets![i];
            int length = lengths![i];
            var buffer = new byte[length];

            lock (data!)
            {
                data!.Position = offset;
                int r;
                if ((r = data!.Read(buffer, 0, buffer.Length)) < buffer.Length)
                {
                    //TODO: Put strings in resource file
                    throw new InvalidOperationException(
                            string.Format ("Unable to fully read from file '{0}' at offset {1} length {2}; read {3} bytes expected {4}.",
                                tzdataPath, offset, length, r, buffer.Length));
                }
            }

            return buffer;
        }
    }
}