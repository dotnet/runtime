// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public readonly partial struct DateTime
    {
        private static LeapSecondCache? s_leapSecondCache;

        // Question: Can this be [Stdcall, SuppressGCTransition]?
        private static unsafe delegate* unmanaged[Stdcall]<ulong*, void> s_pfnGetSystemTimeAsFileTime = GetGetSystemTimeAsFileTimeFnPtr();

        private static unsafe delegate* unmanaged[Stdcall]<ulong*, void> GetGetSystemTimeAsFileTimeFnPtr()
        {
            IntPtr kernel32Lib = NativeLibrary.Load("kernel32.dll", typeof(DateTime).Assembly, DllImportSearchPath.System32);
            IntPtr pfnGetSystemTime = NativeLibrary.GetExport(kernel32Lib, "GetSystemTimeAsFileTime"); // will never fail

            if (NativeLibrary.TryGetExport(kernel32Lib, "GetSystemTimePreciseAsFileTime", out IntPtr pfnGetSystemTimePrecise))
            {
                // If GetSystemTimePreciseAsFileTime exists, we'd like to use it.  However, on
                // misconfigured systems, it's possible for the "precise" time to be inaccurate:
                //     https://github.com/dotnet/runtime/issues/9014
                // If it's inaccurate, though, we expect it to be wildly inaccurate, so as a
                // workaround/heuristic, we get both the "normal" and "precise" times, and as
                // long as they're close, we use the precise one. This workaround can be removed
                // when we better understand what's causing the drift and the issue is no longer
                // a problem or can be better worked around on all targeted OSes.

                ulong filetimeStd, filetimePrecise;
                ((delegate* unmanaged[Stdcall]<ulong*, void>)pfnGetSystemTime)(&filetimeStd);
                ((delegate* unmanaged[Stdcall]<ulong*, void>)pfnGetSystemTimePrecise)(&filetimePrecise);

                if (Math.Abs((long)(filetimeStd - filetimePrecise)) <= 100 * TicksPerMillisecond)
                {
                    pfnGetSystemTime = pfnGetSystemTimePrecise; // use the precise version
                }
            }

            return (delegate* unmanaged[Stdcall]<ulong*, void>)pfnGetSystemTime;
        }

        public static unsafe DateTime UtcNow
        {
            get
            {
                // The OS tick count and .NET's tick count are slightly different. The OS tick
                // count is the *absolute* number of 100-ns intervals which have elapsed since
                // January 1, 1601 (UTC). Due to leap second handling, the number of ticks per
                // day is variable. Dec. 30, 2016 had 864,000,000,000 ticks (a standard 24-hour
                // day), but Dec. 31, 2016 had 864,010,000,000 ticks due to leap second insertion.
                // In .NET, *every* day is assumed to have exactly 24 hours (864,000,000,000 ticks).
                // This means that per the OS, midnight Dec. 31, 2016 + 864 bn ticks = Dec. 31, 2016 23:59:60,
                // but per .NET, midnight Dec. 31, 2016 + 864 bn ticks = Jan. 1, 2017 00:00:00.
                //
                // We can query the OS and have it deconstruct the tick count into (yyyy-mm-dd hh:mm:ss),
                // constructing a new DateTime object from these components, but this is slow.
                // So instead we'll rely on the fact that leap seconds only ever adjust the day
                // by +1 or -1 second, and only at the very end of the day. That is, time rolls
                // 23:59:58 -> 00:00:00 (negative leap second) or 23:59:59 -> 23:59:60 (positive leap
                // second). Thus we assume that each day has at least 23 hr 59 min 59 sec, or
                // 863,990,000,000 ticks.
                //
                // We take advantage of this by caching what the OS believes the tick count is at
                // the beginning of the day vs. what .NET believes the tick count is at the beginning
                // of the day. When the OS returns a tick count to us, if it's within 23:59:59 of
                // what midnight was on the current day, then we know that there's no way for a leap
                // second to have been inserted or removed, and we can short-circuit the leap second
                // handling logic by performing a quick addition and returning immediately. If the
                // OS-provided tick count is outside of our cached range, we'll update the cache.
                // On the off-chance the API is called on the very last second (or two) of the day,
                // we'll go down the slow path without updating the cache, and once another second
                // elapses we'll be able to update the cache again.

                const long MinTicksPerDay = TicksPerDay - TicksPerSecond; // a day is at least 23:59:59 long

                ulong osTicks;
                s_pfnGetSystemTimeAsFileTime(&osTicks);

                // If the OS doesn't support leap second handling, short-circuit everything.

                if (!s_systemSupportsLeapSeconds)
                {
                    return new DateTime((osTicks + FileTimeOffset) | KindUtc);
                }

                // If it's between 00:00:00 (inclusive) and 23:59:59 (exclusive) on the same day
                // as the previous call to UtcNow, we can use the cached value. We can't remove
                // the "is it before midnight?" check below since the system clock may have been
                // moved backward.

                if (s_leapSecondCache is LeapSecondCache cache)
                {
                    if (osTicks >= cache.WindowsTicksAsOfMidnight && osTicks < (cache.WindowsTicksAsOfMidnight + MinTicksPerDay))
                    {
                        return new DateTime((osTicks - cache.WindowsTicksAsOfMidnight + cache.DateTimeTicksAsOfMidnight) | KindUtc);
                    }
                }

                // If we reached this point, one of the following is true:
                //   a) the cache hasn't yet been initialized; or
                //   b) the day has changed since the last call to UtcNow; or
                //   c) the current time is 23:59:59 or 23:59:60.
                //
                // In cases (a) and (b), we'll update the cache. In case (c), we
                // pessimistically assume we might be inside a leap second, so we
                // won't update the cache.

                DateTime dateTime = FromFileTimeLeapSecondsAware((long)osTicks);
                ulong ticksIntoDay = (ulong)dateTime.Ticks % (ulong)TicksPerDay;
                if (ticksIntoDay < MinTicksPerDay)
                {
                    // It's not yet 23:59:59, so update the cache. It's ok for multiple
                    // threads to do this concurrently as long as the write to the static
                    // is published *after* the cache object's fields have been populated.

                    Volatile.Write(ref s_leapSecondCache, new LeapSecondCache
                    {
                        WindowsTicksAsOfMidnight = osTicks - ticksIntoDay,
                        DateTimeTicksAsOfMidnight = (ulong)dateTime.Ticks - ticksIntoDay
                    });
                }

                return dateTime;
            }
        }

        private sealed class LeapSecondCache
        {
            internal ulong WindowsTicksAsOfMidnight;
            internal ulong DateTimeTicksAsOfMidnight;
        }

        internal static readonly bool s_systemSupportsLeapSeconds = SystemSupportsLeapSeconds();

        internal static unsafe bool IsValidTimeWithLeapSeconds(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind)
        {
            DateTime dt = new DateTime(year, month, day);
            FullSystemTime time = new FullSystemTime(year, month, dt.DayOfWeek, day, hour, minute, second);

            return kind switch
            {
                DateTimeKind.Local => ValidateSystemTime(&time.systemTime, localTime: true),
                DateTimeKind.Utc => ValidateSystemTime(&time.systemTime, localTime: false),
                _ => ValidateSystemTime(&time.systemTime, localTime: true) || ValidateSystemTime(&time.systemTime, localTime: false),
            };
        }

        private static unsafe DateTime FromFileTimeLeapSecondsAware(long fileTime)
        {
            FullSystemTime time;
            if (FileTimeToSystemTime(fileTime, &time))
            {
                return CreateDateTimeFromSystemTime(in time);
            }

            throw new ArgumentOutOfRangeException(nameof(fileTime), SR.ArgumentOutOfRange_DateTimeBadTicks);
        }

        private static unsafe long ToFileTimeLeapSecondsAware(long ticks)
        {
            FullSystemTime time = new FullSystemTime(ticks);
            long fileTime;

            if (SystemTimeToFileTime(&time.systemTime, &fileTime))
            {
                return fileTime + ticks % TicksPerMillisecond;
            }

            throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_FileTimeInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime CreateDateTimeFromSystemTime(in FullSystemTime time)
        {
            long ticks = DateToTicks(time.systemTime.Year, time.systemTime.Month, time.systemTime.Day);
            ticks += TimeToTicks(time.systemTime.Hour, time.systemTime.Minute, time.systemTime.Second);
            ticks += time.systemTime.Milliseconds * TicksPerMillisecond;
            ticks += time.hundredNanoSecond;
            return new DateTime(((ulong)(ticks)) | KindUtc);
        }

        // FullSystemTime struct is the SYSTEMTIME struct with extra hundredNanoSecond field to store more precise time.
        [StructLayout(LayoutKind.Sequential)]
        private struct FullSystemTime
        {
            internal Interop.Kernel32.SYSTEMTIME systemTime;
            internal long hundredNanoSecond;

            internal FullSystemTime(int year, int month, DayOfWeek dayOfWeek, int day, int hour, int minute, int second)
            {
                systemTime.Year = (ushort)year;
                systemTime.Month = (ushort)month;
                systemTime.DayOfWeek = (ushort)dayOfWeek;
                systemTime.Day = (ushort)day;
                systemTime.Hour = (ushort)hour;
                systemTime.Minute = (ushort)minute;
                systemTime.Second = (ushort)second;
                systemTime.Milliseconds = 0;
                hundredNanoSecond = 0;
            }

            internal FullSystemTime(long ticks)
            {
                DateTime dt = new DateTime(ticks);

                dt.GetDate(out int year, out int month, out int day);
                dt.GetTime(out int hour, out int minute, out int second, out int millisecond);

                systemTime.Year = (ushort)year;
                systemTime.Month = (ushort)month;
                systemTime.DayOfWeek = (ushort)dt.DayOfWeek;
                systemTime.Day = (ushort)day;
                systemTime.Hour = (ushort)hour;
                systemTime.Minute = (ushort)minute;
                systemTime.Second = (ushort)second;
                systemTime.Milliseconds = (ushort)millisecond;
                hundredNanoSecond = 0;
            }
        }

#if !CORECLR
        internal static readonly bool s_systemSupportsPreciseSystemTime = SystemSupportsPreciseSystemTime();

        private static unsafe bool SystemSupportsPreciseSystemTime()
        {
            if (Environment.IsWindows8OrAbove)
            {
                // GetSystemTimePreciseAsFileTime exists and we'd like to use it.  However, on
                // misconfigured systems, it's possible for the "precise" time to be inaccurate:
                //     https://github.com/dotnet/runtime/issues/9014
                // If it's inaccurate, though, we expect it to be wildly inaccurate, so as a
                // workaround/heuristic, we get both the "normal" and "precise" times, and as
                // long as they're close, we use the precise one. This workaround can be removed
                // when we better understand what's causing the drift and the issue is no longer
                // a problem or can be better worked around on all targeted OSes.

                long systemTimeResult;
                Interop.Kernel32.GetSystemTimeAsFileTime(&systemTimeResult);

                long preciseSystemTimeResult;
                Interop.Kernel32.GetSystemTimePreciseAsFileTime(&preciseSystemTimeResult);

                return Math.Abs(preciseSystemTimeResult - systemTimeResult) <= 100 * TicksPerMillisecond;
            }

            return false;
        }

        private static unsafe bool ValidateSystemTime(Interop.Kernel32.SYSTEMTIME* time, bool localTime)
        {
            if (localTime)
            {
                Interop.Kernel32.SYSTEMTIME st;
                return Interop.Kernel32.TzSpecificLocalTimeToSystemTime(IntPtr.Zero, time, &st) != Interop.BOOL.FALSE;
            }
            else
            {
                long timestamp;
                return Interop.Kernel32.SystemTimeToFileTime(time, &timestamp) != Interop.BOOL.FALSE;
            }
        }

        private static unsafe bool FileTimeToSystemTime(long fileTime, FullSystemTime* time)
        {
            if (Interop.Kernel32.FileTimeToSystemTime(&fileTime, &time->systemTime) != Interop.BOOL.FALSE)
            {
                // to keep the time precision
                time->hundredNanoSecond = fileTime % TicksPerMillisecond;
                if (time->systemTime.Second > 59)
                {
                    // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
                    // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
                    time->systemTime.Second = 59;
                    time->systemTime.Milliseconds = 999;
                    time->hundredNanoSecond = 9999;
                }
                return true;
            }
            return false;
        }

        private static unsafe void GetSystemTimeWithLeapSecondsHandling(FullSystemTime* time)
        {
            if (!FileTimeToSystemTime(GetSystemTimeAsFileTime(), time))
            {
                Interop.Kernel32.GetSystemTime(&time->systemTime);
                time->hundredNanoSecond = 0;
                if (time->systemTime.Second > 59)
                {
                    // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
                    // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
                    time->systemTime.Second = 59;
                    time->systemTime.Milliseconds = 999;
                    time->hundredNanoSecond = 9999;
                }
            }
        }

        private static unsafe bool SystemTimeToFileTime(Interop.Kernel32.SYSTEMTIME* time, long* fileTime)
        {
            return Interop.Kernel32.SystemTimeToFileTime(time, fileTime) != Interop.BOOL.FALSE;
        }

        private static unsafe long GetSystemTimeAsFileTime()
        {
            long timestamp;

            if (s_systemSupportsPreciseSystemTime)
            {
                Interop.Kernel32.GetSystemTimePreciseAsFileTime(&timestamp);
            }
            else
            {
                Interop.Kernel32.GetSystemTimeAsFileTime(&timestamp);
            }

            return timestamp;
        }
#endif
    }
}
