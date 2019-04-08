// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Overflow-safe DateTimeOffset factory.
        /// </summary>
        private static bool TryCreateDateTimeOffset(DateTime dateTime, bool offsetNegative, int offsetHours, int offsetMinutes, out DateTimeOffset value)
        {
            if (((uint)offsetHours) > Utf8Constants.DateTimeMaxUtcOffsetHours)
            {
                value = default;
                return false;
            }

            if (((uint)offsetMinutes) > 59)
            {
                value = default;
                return false;
            }

            if (offsetHours == Utf8Constants.DateTimeMaxUtcOffsetHours && offsetMinutes != 0)
            {
                value = default;
                return false;
            }

            long offsetTicks = (((long)offsetHours) * 3600 + ((long)offsetMinutes) * 60) * TimeSpan.TicksPerSecond;
            if (offsetNegative)
            {
                offsetTicks = -offsetTicks;
            }

            try
            {
                value = new DateTimeOffset(ticks: dateTime.Ticks, offset: new TimeSpan(ticks: offsetTicks));
            }
            catch (ArgumentOutOfRangeException)
            {
                // If we got here, the combination of the DateTime + UTC offset strayed outside the 1..9999 year range. This case seems rare enough
                // that it's better to catch the exception rather than replicate DateTime's range checking (which it's going to do anyway.)
                value = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Overflow-safe DateTimeOffset factory.
        /// </summary>
        private static bool TryCreateDateTimeOffset(int year, int month, int day, int hour, int minute, int second, int fraction, bool offsetNegative, int offsetHours, int offsetMinutes, out DateTimeOffset value)
        {
            if (!TryCreateDateTime(year: year, month: month, day: day, hour: hour, minute: minute, second: second, fraction: fraction, kind: DateTimeKind.Unspecified, out DateTime dateTime))
            {
                value = default;
                return false;
            }

            if (!TryCreateDateTimeOffset(dateTime: dateTime, offsetNegative: offsetNegative, offsetHours: offsetHours, offsetMinutes: offsetMinutes, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Overflow-safe DateTimeOffset/Local time conversion factory.
        /// </summary>
        private static bool TryCreateDateTimeOffsetInterpretingDataAsLocalTime(int year, int month, int day, int hour, int minute, int second, int fraction, out DateTimeOffset value)
        {
            if (!TryCreateDateTime(year: year, month: month, day: day, hour: hour, minute: minute, second: second, fraction: fraction, DateTimeKind.Local, out DateTime dateTime))
            {
                value = default;
                return false;
            }

            try
            {
                value = new DateTimeOffset(dateTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                // If we got here, the combination of the DateTime + UTC offset strayed outside the 1..9999 year range. This case seems rare enough
                // that it's better to catch the exception rather than replicate DateTime's range checking (which it's going to do anyway.)

                value = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Overflow-safe DateTime factory.
        /// </summary>
        private static bool TryCreateDateTime(int year, int month, int day, int hour, int minute, int second, int fraction, DateTimeKind kind, out DateTime value)
        {
            if (year == 0)
            {
                value = default;
                return false;
            }

            Debug.Assert(year <= 9999); // All of our callers to date parse the year from fixed 4-digit fields so this value is trusted.

            if ((((uint)month) - 1) >= 12)
            {
                value = default;
                return false;
            }

            uint dayMinusOne = ((uint)day) - 1;
            if (dayMinusOne >= 28 && dayMinusOne >= DateTime.DaysInMonth(year, month))
            {
                value = default;
                return false;
            }

            if (((uint)hour) > 23)
            {
                value = default;
                return false;
            }

            if (((uint)minute) > 59)
            {
                value = default;
                return false;
            }

            if (((uint)second) > 59)
            {
                value = default;
                return false;
            }

            Debug.Assert(fraction >= 0 && fraction <= Utf8Constants.MaxDateTimeFraction); // All of our callers to date parse the fraction from fixed 7-digit fields so this value is trusted.

            int[] days = DateTime.IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;
            int yearMinusOne = year - 1;
            int totalDays = (yearMinusOne * 365) + (yearMinusOne / 4) - (yearMinusOne / 100) + (yearMinusOne / 400) + days[month - 1] + day - 1;
            long ticks = totalDays * TimeSpan.TicksPerDay;
            int totalSeconds = (hour * 3600) + (minute * 60) + second;
            ticks += totalSeconds * TimeSpan.TicksPerSecond;
            ticks += fraction;
            value = new DateTime(ticks: ticks, kind: kind);
            return true;
        }

        private static readonly int[] s_daysToMonth365 = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        private static readonly int[] s_daysToMonth366 = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };
    }
}
