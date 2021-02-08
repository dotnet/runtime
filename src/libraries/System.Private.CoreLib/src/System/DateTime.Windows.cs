// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public readonly partial struct DateTime
    {
        internal static readonly bool s_systemSupportsLeapSeconds = SystemSupportsLeapSeconds();

        public static unsafe DateTime UtcNow
        {
            get
            {
                ulong fileTime;
                s_pfnGetSystemTimeAsFileTime(&fileTime);

                if (s_systemSupportsLeapSeconds)
                {
                    Interop.Kernel32.SYSTEMTIME time;
                    ulong hundredNanoSecond;

                    if (Interop.Kernel32.FileTimeToSystemTime(&fileTime, &time) != Interop.BOOL.FALSE)
                    {
                        // to keep the time precision
                        ulong tmp = fileTime; // temp. variable avoids double read from memory
                        hundredNanoSecond = tmp % TicksPerMillisecond;
                    }
                    else
                    {
                        Interop.Kernel32.GetSystemTime(&time);
                        hundredNanoSecond = 0;
                    }

                    return CreateDateTimeFromSystemTime(in time, hundredNanoSecond);
                }
                else
                {
                    return new DateTime(fileTime + FileTimeOffset | KindUtc);
                }
            }
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private static unsafe readonly delegate* unmanaged[SuppressGCTransition]<ulong*, void> s_pfnGetSystemTimeAsFileTime = GetGetSystemTimeAsFileTimeFnPtr();

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
    }
}
