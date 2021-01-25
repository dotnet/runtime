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
                long fileTime;
                s_pfnGetSystemTimeAsFileTime(&fileTime);

                if (s_systemSupportsLeapSeconds)
                {
                    FullSystemTime time;

                    if (Interop.Kernel32.FileTimeToSystemTime(&fileTime, &time.systemTime) != Interop.BOOL.FALSE)
                    {
                        // to keep the time precision
                        time.hundredNanoSecond = fileTime % 10000; // 10000 is the number of 100-nano seconds per Millisecond
                    }
                    else
                    {
                        Interop.Kernel32.GetSystemTime(&time.systemTime);
                        time.hundredNanoSecond = 0;
                    }

                    if (time.systemTime.Second > 59)
                    {
                        // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
                        // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
                        time.systemTime.Second = 59;
                        time.systemTime.Milliseconds = 999;
                        time.hundredNanoSecond = 9999;
                    }

                    return CreateDateTimeFromSystemTime(in time);
                }
                else
                {
                    return new DateTime(((ulong)(fileTime + FileTimeOffset)) | KindUtc);
                }
            }
        }

        internal static unsafe bool IsValidTimeWithLeapSeconds(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind)
        {
            DateTime dt = new DateTime(year, month, day);
            FullSystemTime time = new FullSystemTime(year, month, dt.DayOfWeek, day, hour, minute, second);

            if (kind != DateTimeKind.Utc)
            {
                Interop.Kernel32.SYSTEMTIME st;
                if (Interop.Kernel32.TzSpecificLocalTimeToSystemTime(IntPtr.Zero, &time.systemTime, &st) != Interop.BOOL.FALSE)
                    return true;
            }

            if (kind != DateTimeKind.Local)
            {
                long ft;
                if (Interop.Kernel32.SystemTimeToFileTime(&time.systemTime, &ft) != Interop.BOOL.FALSE)
                    return true;
            }

            return false;
        }

        private static unsafe DateTime FromFileTimeLeapSecondsAware(long fileTime)
        {
            FullSystemTime time;
            if (Interop.Kernel32.FileTimeToSystemTime(&fileTime, &time.systemTime) == Interop.BOOL.FALSE)
            {
                throw new ArgumentOutOfRangeException(nameof(fileTime), SR.ArgumentOutOfRange_DateTimeBadTicks);
            }

            // to keep the time precision
            time.hundredNanoSecond = fileTime % TicksPerMillisecond;
            if (time.systemTime.Second > 59)
            {
                // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
                // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
                time.systemTime.Second = 59;
                time.systemTime.Milliseconds = 999;
                time.hundredNanoSecond = 9999;
            }
            return CreateDateTimeFromSystemTime(in time);
        }

        private static unsafe long ToFileTimeLeapSecondsAware(long ticks)
        {
            FullSystemTime time = new FullSystemTime(ticks);
            long fileTime;

            if (Interop.Kernel32.SystemTimeToFileTime(&time.systemTime, &fileTime) == Interop.BOOL.FALSE)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_FileTimeInvalid);
            }

            return fileTime + ticks % TicksPerMillisecond;
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

        private static unsafe readonly delegate* unmanaged[SuppressGCTransition]<long*, void> s_pfnGetSystemTimeAsFileTime = GetGetSystemTimeAsFileTimeFnPtr();

        private static unsafe delegate* unmanaged[SuppressGCTransition]<long*, void> GetGetSystemTimeAsFileTimeFnPtr()
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

            return (delegate* unmanaged[SuppressGCTransition]<long*, void>)pfnGetSystemTime;
        }
    }
}
