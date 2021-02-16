// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public readonly partial struct DateTime
    {
        internal static readonly bool s_systemSupportsLeapSeconds = SystemSupportsLeapSeconds();

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

            ulong ticks = GetTicksFromSystemTime(in time, fileTime % TicksPerMillisecond);
            return new DateTime(ticks | KindUtc);
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
        internal static ulong GetTicksFromSystemTime(in Interop.Kernel32.SYSTEMTIME time, ulong hundredNanoSecond)
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
                return ticks + tmp;
            }

            // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
            // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
            return ticks + TicksPerMinute - 1;
        }
    }
}
