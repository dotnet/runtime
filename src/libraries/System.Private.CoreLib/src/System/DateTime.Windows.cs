// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public readonly partial struct DateTime
    {
        internal static readonly bool s_systemSupportsLeapSeconds = SystemSupportsLeapSeconds();

        public static unsafe DateTime UtcNow
        {
            get
            {
                ulong fileTimeTmp; // mark only the temp local as address-taken
                s_pfnGetSystemTimeAsFileTime(&fileTimeTmp);
                ulong fileTime = fileTimeTmp;

                if (s_systemSupportsLeapSeconds)
                {
                    // Query the leap second cache first, which avoids expensive calls to GetFileTimeAsSystemTime.

                    LeapSecondCache cacheValue = s_leapSecondCache;
                    ulong ticksSinceStartOfCacheValidityWindow = fileTime - cacheValue.OSFileTimeTicksAtStartOfValidityWindow;
                    if (ticksSinceStartOfCacheValidityWindow < LeapSecondCache.ValidityPeriodInTicks)
                    {
                        return new DateTime(dateData: cacheValue.DotnetDateDataAtStartOfValidityWindow + ticksSinceStartOfCacheValidityWindow);
                    }

                    return UpdateLeapSecondCacheAndReturnUtcNow(); // couldn't use the cache, go down the slow path
                }
                else
                {
                    return new DateTime(dateData: fileTime + (FileTimeOffset | KindUtc));
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe bool IsValidTimeWithLeapSeconds(int year, int month, int day, int hour, int minute, DateTimeKind kind)
        {
            Interop.Kernel32.SYSTEMTIME time;
            time.Year = (ushort)year;
            time.Month = (ushort)month;
            time.DayOfWeek = 0; // ignored by TzSpecificLocalTimeToSystemTime/SystemTimeToFileTime
            time.Day = (ushort)day;
            time.Hour = (ushort)hour;
            time.Minute = (ushort)minute;
            time.Second = 60;
            time.Milliseconds = 0;

            if (kind != DateTimeKind.Utc)
            {
                Interop.Kernel32.SYSTEMTIME st;
                if (Interop.Kernel32.TzSpecificLocalTimeToSystemTime(IntPtr.Zero, &time, &st) != Interop.BOOL.FALSE)
                    return true;
            }

            if (kind != DateTimeKind.Local)
            {
                ulong ft;
                if (Interop.Kernel32.SystemTimeToFileTime(&time, &ft) != Interop.BOOL.FALSE)
                    return true;
            }

            return false;
        }

        private static unsafe DateTime FromFileTimeLeapSecondsAware(ulong fileTime)
        {
            Interop.Kernel32.SYSTEMTIME time;
            if (Interop.Kernel32.FileTimeToSystemTime(&fileTime, &time) == Interop.BOOL.FALSE)
            {
                throw new ArgumentOutOfRangeException(nameof(fileTime), SR.ArgumentOutOfRange_DateTimeBadTicks);
            }
            return CreateDateTimeFromSystemTime(in time, fileTime % TicksPerMillisecond);
        }

        private static unsafe ulong ToFileTimeLeapSecondsAware(long ticks)
        {
            DateTime dt = new(ticks);
            Interop.Kernel32.SYSTEMTIME time;

            dt.GetDate(out int year, out int month, out int day);
            time.Year = (ushort)year;
            time.Month = (ushort)month;
            time.DayOfWeek = 0; // ignored by SystemTimeToFileTime
            time.Day = (ushort)day;

            dt.GetTimePrecise(out int hour, out int minute, out int second, out int tick);
            time.Hour = (ushort)hour;
            time.Minute = (ushort)minute;
            time.Second = (ushort)second;
            time.Milliseconds = 0;

            ulong fileTime;
            if (Interop.Kernel32.SystemTimeToFileTime(&time, &fileTime) == Interop.BOOL.FALSE)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_FileTimeInvalid);
            }

            return fileTime + (uint)tick;
        }

        private static DateTime CreateDateTimeFromSystemTime(in Interop.Kernel32.SYSTEMTIME time, ulong hundredNanoSecond)
        {
            uint year = time.Year;
            uint[] days = IsLeapYear((int)year) ? s_daysToMonth366 : s_daysToMonth365;
            int month = time.Month - 1;
            uint n = DaysToYear(year) + days[month] + time.Day - 1;
            ulong ticks = n * (ulong)TicksPerDay;

            ticks += time.Hour * (ulong)TicksPerHour;
            ticks += time.Minute * (ulong)TicksPerMinute;
            uint second = time.Second;
            if (second <= 59)
            {
                ulong tmp = second * (uint)TicksPerSecond + time.Milliseconds * (uint)TicksPerMillisecond + hundredNanoSecond;
                return new DateTime(ticks + tmp | KindUtc);
            }

            // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
            // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
            ticks += TicksPerMinute - 1 | KindUtc;
            return new DateTime(ticks);
        }

        private static readonly unsafe delegate* unmanaged[SuppressGCTransition]<ulong*, void> s_pfnGetSystemTimeAsFileTime = GetGetSystemTimeAsFileTimeFnPtr();

        private static unsafe delegate* unmanaged[SuppressGCTransition]<ulong*, void> GetGetSystemTimeAsFileTimeFnPtr()
        {
            IntPtr kernel32Lib = Interop.Kernel32.LoadLibraryEx(Interop.Libraries.Kernel32, IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
            Debug.Assert(kernel32Lib != IntPtr.Zero);

            IntPtr pfnGetSystemTime = NativeLibrary.GetExport(kernel32Lib, "GetSystemTimeAsFileTime");

            if (NativeLibrary.TryGetExport(kernel32Lib, "GetSystemTimePreciseAsFileTime", out IntPtr pfnGetSystemTimePrecise))
            {
                // GetSystemTimePreciseAsFileTime exists and we'd like to use it.  However, on
                // misconfigured systems, it's possible for the "precise" time to be inaccurate:
                //     https://github.com/dotnet/runtime/issues/9014
                // If it's inaccurate, though, we expect it to be wildly inaccurate, so as a
                // workaround/heuristic, we get both the "normal" and "precise" times, and as
                // long as they're close, we use the precise one. This workaround can be removed
                // when we better understand what's causing the drift and the issue is no longer
                // a problem or can be better worked around on all targeted OSes.

                // Retry this check several times to reduce chance of false negatives due to thread being rescheduled
                // at wrong time.
                for (int i = 0; i < 10; i++)
                {
                    long systemTimeResult, preciseSystemTimeResult;
                    ((delegate* unmanaged[SuppressGCTransition]<long*, void>)pfnGetSystemTime)(&systemTimeResult);
                    ((delegate* unmanaged[SuppressGCTransition]<long*, void>)pfnGetSystemTimePrecise)(&preciseSystemTimeResult);

                    if (Math.Abs(preciseSystemTimeResult - systemTimeResult) <= 100 * TicksPerMillisecond)
                    {
                        pfnGetSystemTime = pfnGetSystemTimePrecise; // use the precise version
                        break;
                    }
                }
            }

            return (delegate* unmanaged[SuppressGCTransition]<ulong*, void>)pfnGetSystemTime;
        }

        private static unsafe DateTime UpdateLeapSecondCacheAndReturnUtcNow()
        {
            // From conversations with the Windows team, the OS has the ability to update leap second
            // data while applications are running. Leap second data is published on WU well ahead of
            // the actual event. Additionally, the OS's list of leap seconds will only ever expand
            // from the end. There won't be a situation where a leap second will ever be inserted into
            // the middle of the list of all known leap seconds.
            //
            // Normally, this would mean that we could just ask "will a leap second occur in the next
            // 24 hours?" and cache this value. However, it's possible that the current machine may have
            // deferred updates so long that when a leap second is added to the end of the list, it
            // actually occurs in the past (compared to UtcNow). To account for this possibility, we
            // limit our cache's lifetime to just a few minutes (the "validity window"). If a deferred
            // OS update occurs and a past leap second is added, this limits the window in which our
            // cache will return incorrect values.

            Debug.Assert(s_systemSupportsLeapSeconds);
            Debug.Assert(LeapSecondCache.ValidityPeriodInTicks < TicksPerDay - TicksPerSecond, "Leap second cache validity window should be less than 23:59:59.");

            ulong fileTimeNow;
            s_pfnGetSystemTimeAsFileTime(&fileTimeNow);

            // If we reached this point, our leap second cache is stale, and we need to update it.
            // First, convert the FILETIME to a SYSTEMTIME.

            Interop.Kernel32.SYSTEMTIME systemTimeNow;
            ulong hundredNanoSecondNow = fileTimeNow % TicksPerMillisecond;

            // We need the FILETIME and the SYSTEMTIME to reflect each other's values.
            // If FileTimeToSystemTime fails, call GetSystemTime and try again until it succeeds.
            if (Interop.Kernel32.FileTimeToSystemTime(&fileTimeNow, &systemTimeNow) == Interop.BOOL.FALSE)
            {
                return LowGranularityNonCachedFallback();
            }

            // If we're currently within a positive leap second, early-exit since our cache can't handle
            // this situation. Once midnight rolls around the next call to DateTime.UtcNow should update
            // the cache correctly.

            if (systemTimeNow.Second >= 60)
            {
                return CreateDateTimeFromSystemTime(systemTimeNow, hundredNanoSecondNow);
            }

            // Our cache will be valid for some amount of time (the "validity window").
            // Check if a leap second will occur within this window.

            ulong fileTimeAtEndOfValidityPeriod = fileTimeNow + LeapSecondCache.ValidityPeriodInTicks;
            Interop.Kernel32.SYSTEMTIME systemTimeAtEndOfValidityPeriod;
            if (Interop.Kernel32.FileTimeToSystemTime(&fileTimeAtEndOfValidityPeriod, &systemTimeAtEndOfValidityPeriod) == Interop.BOOL.FALSE)
            {
                return LowGranularityNonCachedFallback();
            }

            ulong fileTimeAtStartOfValidityWindow;
            ulong dotnetDateDataAtStartOfValidityWindow;

            // A leap second can only occur at the end of the day, and we can only leap by +/- 1 second
            // at a time. To see if a leap second occurs within the upcoming validity window, we can
            // compare the 'seconds' values at the start and the end of the window.

            if (systemTimeAtEndOfValidityPeriod.Second == systemTimeNow.Second)
            {
                // If we reached this block, a leap second will not occur within the validity window.
                // We can cache the validity window starting at UtcNow.

                fileTimeAtStartOfValidityWindow = fileTimeNow;
                dotnetDateDataAtStartOfValidityWindow = CreateDateTimeFromSystemTime(systemTimeNow, hundredNanoSecondNow)._dateData;
            }
            else
            {
                // If we reached this block, a leap second will occur within the validity window. We cannot
                // allow the cache to cover this entire window, otherwise the cache will start reporting
                // incorrect values once the leap second occurs. To account for this, we slide the validity
                // window back a little bit. The window will have the same duration as before, but instead
                // of beginning now, we'll choose the proper begin time so that it ends at 23:59:59.000.

                Interop.Kernel32.SYSTEMTIME systemTimeAtBeginningOfDay = systemTimeNow;
                systemTimeAtBeginningOfDay.Hour = 0;
                systemTimeAtBeginningOfDay.Minute = 0;
                systemTimeAtBeginningOfDay.Second = 0;
                systemTimeAtBeginningOfDay.Milliseconds = 0;

                ulong fileTimeAtBeginningOfDay;
                if (Interop.Kernel32.SystemTimeToFileTime(&systemTimeAtBeginningOfDay, &fileTimeAtBeginningOfDay) == Interop.BOOL.FALSE)
                {
                    return LowGranularityNonCachedFallback();
                }

                // StartOfValidityWindow = MidnightUtc + 23:59:59 - ValidityPeriod
                fileTimeAtStartOfValidityWindow = fileTimeAtBeginningOfDay + (TicksPerDay - TicksPerSecond) - LeapSecondCache.ValidityPeriodInTicks;
                if (fileTimeNow - fileTimeAtStartOfValidityWindow >= LeapSecondCache.ValidityPeriodInTicks)
                {
                    // If we're inside this block, then we slid the validity window back so far that the current time is no
                    // longer within the window. This can only occur if the current time is 23:59:59.xxx and the next second is a
                    // positive leap second (23:59:60.xxx). For example, if the current time is 23:59:59.123, assuming a
                    // 5-minute validity period, we'll slide the validity window back to [23:54:59.000, 23:59:59.000).
                    //
                    // Depending on how the current process is configured, the OS may report time data in one of two ways. If
                    // the current process is leap-second aware (has the PROCESS_LEAP_SECOND_INFO_FLAG_ENABLE_SIXTY_SECOND flag set),
                    // then a SYSTEMTIME object will report leap seconds by setting the 'wSecond' field to 60. If the current
                    // process is not leap-second aware, the OS will compress the last two seconds of the day as follows.
                    //
                    // Actual time      GetSystemTime returns
                    // ========================================
                    // 23:59:59.000     23:59:59.000
                    // 23:59:59.500     23:59:59.250
                    // 23:59:60.000     23:59:59.500
                    // 23:59:60.500     23:59:59.750
                    // 00:00:00.000     00:00:00.000 (next day)
                    //
                    // In this scenario, we'll skip the caching logic entirely, relying solely on the OS-provided SYSTEMTIME
                    // struct to tell us how to interpret the time information.

                    Debug.Assert(systemTimeNow.Hour == 23 && systemTimeNow.Minute == 59 && systemTimeNow.Second == 59);
                    return CreateDateTimeFromSystemTime(systemTimeNow, hundredNanoSecondNow);
                }

                dotnetDateDataAtStartOfValidityWindow = CreateDateTimeFromSystemTime(systemTimeAtBeginningOfDay, 0)._dateData + (TicksPerDay - TicksPerSecond) - LeapSecondCache.ValidityPeriodInTicks;
            }

            // Finally, update the cache and return UtcNow.

            Debug.Assert(fileTimeNow - fileTimeAtStartOfValidityWindow < LeapSecondCache.ValidityPeriodInTicks, "We should be within the validity window.");
            Volatile.Write(ref s_leapSecondCache, new LeapSecondCache()
            {
                OSFileTimeTicksAtStartOfValidityWindow = fileTimeAtStartOfValidityWindow,
                DotnetDateDataAtStartOfValidityWindow = dotnetDateDataAtStartOfValidityWindow
            });

            return new DateTime(dateData: dotnetDateDataAtStartOfValidityWindow + fileTimeNow - fileTimeAtStartOfValidityWindow);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static DateTime LowGranularityNonCachedFallback()
            {
                // If we reached this point, one of the Win32 APIs FileTimeToSystemTime or SystemTimeToFileTime
                // failed. This should never happen in practice, as this would imply that the Win32 API
                // GetSystemTimeAsFileTime returned an invalid value to us at the start of the calling method.
                // But, just to be safe, if this ever does happen, we'll bypass the caching logic entirely
                // and fall back to GetSystemTime. This results in a loss of granularity (millisecond-only,
                // not rdtsc-based), but at least it means we won't fail.

                Debug.Fail("Our Win32 calls should never fail.");

                Interop.Kernel32.SYSTEMTIME systemTimeNow;
                Interop.Kernel32.GetSystemTime(&systemTimeNow);
                return CreateDateTimeFromSystemTime(systemTimeNow, 0);
            }
        }

        // The leap second cache. May be accessed by multiple threads simultaneously.
        // Writers must not mutate the object's fields after the reference is published.
        // Readers are not required to use volatile semantics.
        private static LeapSecondCache s_leapSecondCache = new LeapSecondCache();

        private sealed class LeapSecondCache
        {
            // The length of the validity window. Must be less than 23:59:59.
            internal const ulong ValidityPeriodInTicks = TicksPerMinute * 5;

            // The FILETIME value at the beginning of the validity window.
            internal ulong OSFileTimeTicksAtStartOfValidityWindow;

            // The DateTime._dateData value at the beginning of the validity window.
            internal ulong DotnetDateDataAtStartOfValidityWindow;
        }
    }
}
