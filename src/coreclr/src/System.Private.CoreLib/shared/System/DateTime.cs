// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;
using CultureInfo = System.Globalization.CultureInfo;
using Calendar = System.Globalization.Calendar;

namespace System
{
    // This value type represents a date and time.  Every DateTime
    // object has a private field (Ticks) of type Int64 that stores the
    // date and time as the number of 100 nanosecond intervals since
    // 12:00 AM January 1, year 1 A.D. in the proleptic Gregorian Calendar.
    //
    // Starting from V2.0, DateTime also stored some context about its time
    // zone in the form of a 3-state value representing Unspecified, Utc or
    // Local. This is stored in the two top bits of the 64-bit numeric value
    // with the remainder of the bits storing the tick count. This information
    // is only used during time zone conversions and is not part of the
    // identity of the DateTime. Thus, operations like Compare and Equals
    // ignore this state. This is to stay compatible with earlier behavior
    // and performance characteristics and to avoid forcing  people into dealing
    // with the effects of daylight savings. Note, that this has little effect
    // on how the DateTime works except in a context where its specific time
    // zone is needed, such as during conversions and some parsing and formatting
    // cases.
    //
    // There is also 4th state stored that is a special type of Local value that
    // is used to avoid data loss when round-tripping between local and UTC time.
    // See below for more information on this 4th state, although it is
    // effectively hidden from most users, who just see the 3-state DateTimeKind
    // enumeration.
    //
    // For compatibility, DateTime does not serialize the Kind data when used in
    // binary serialization.
    //
    // For a description of various calendar issues, look at
    //
    //
    [StructLayout(LayoutKind.Auto)]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly partial struct DateTime : IComparable, IFormattable, IConvertible, IComparable<DateTime>, IEquatable<DateTime>, ISerializable, ISpanFormattable
    {
        // Number of 100ns ticks per time unit
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;

        // Number of milliseconds per time unit
        private const int MillisPerSecond = 1000;
        private const int MillisPerMinute = MillisPerSecond * 60;
        private const int MillisPerHour = MillisPerMinute * 60;
        private const int MillisPerDay = MillisPerHour * 24;

        // Number of days in a non-leap year
        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        // Number of days from 1/1/0001 to 12/31/1600
        private const int DaysTo1601 = DaysPer400Years * 4;          // 584388
        // Number of days from 1/1/0001 to 12/30/1899
        private const int DaysTo1899 = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;
        // Number of days from 1/1/0001 to 12/31/1969
        internal const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear; // 719,162
        // Number of days from 1/1/0001 to 12/31/9999
        private const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059

        internal const long MinTicks = 0;
        internal const long MaxTicks = DaysTo10000 * TicksPerDay - 1;
        private const long MaxMillis = (long)DaysTo10000 * MillisPerDay;

        internal const long UnixEpochTicks = DaysTo1970 * TicksPerDay;
        private const long FileTimeOffset = DaysTo1601 * TicksPerDay;
        private const long DoubleDateOffset = DaysTo1899 * TicksPerDay;
        // The minimum OA date is 0100/01/01 (Note it's year 100).
        // The maximum OA date is 9999/12/31
        private const long OADateMinAsTicks = (DaysPer100Years - DaysPerYear) * TicksPerDay;
        // All OA dates must be greater than (not >=) OADateMinAsDouble
        private const double OADateMinAsDouble = -657435.0;
        // All OA dates must be less than (not <=) OADateMaxAsDouble
        private const double OADateMaxAsDouble = 2958466.0;

        private const int DatePartYear = 0;
        private const int DatePartDayOfYear = 1;
        private const int DatePartMonth = 2;
        private const int DatePartDay = 3;

        private static readonly int[] s_daysToMonth365 = {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};
        private static readonly int[] s_daysToMonth366 = {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366};

        public static readonly DateTime MinValue = new DateTime(MinTicks, DateTimeKind.Unspecified);
        public static readonly DateTime MaxValue = new DateTime(MaxTicks, DateTimeKind.Unspecified);
        public static readonly DateTime UnixEpoch = new DateTime(UnixEpochTicks, DateTimeKind.Utc);

        private const ulong TicksMask = 0x3FFFFFFFFFFFFFFF;
        private const ulong FlagsMask = 0xC000000000000000;
        private const ulong LocalMask = 0x8000000000000000;
        private const long TicksCeiling = 0x4000000000000000;
        private const ulong KindUnspecified = 0x0000000000000000;
        private const ulong KindUtc = 0x4000000000000000;
        private const ulong KindLocal = 0x8000000000000000;
        private const ulong KindLocalAmbiguousDst = 0xC000000000000000;
        private const int KindShift = 62;

        private const string TicksField = "ticks"; // Do not rename (binary serialization)
        private const string DateDataField = "dateData"; // Do not rename (binary serialization)

        // The data is stored as an unsigned 64-bit integer
        //   Bits 01-62: The value of 100-nanosecond ticks where 0 represents 1/1/0001 12:00am, up until the value
        //               12/31/9999 23:59:59.9999999
        //   Bits 63-64: A four-state value that describes the DateTimeKind value of the date time, with a 2nd
        //               value for the rare case where the date time is local, but is in an overlapped daylight
        //               savings time hour and it is in daylight savings time. This allows distinction of these
        //               otherwise ambiguous local times and prevents data loss when round tripping from Local to
        //               UTC time.
        private readonly ulong _dateData;

        // Constructs a DateTime from a tick count. The ticks
        // argument specifies the date as the number of 100-nanosecond intervals
        // that have elapsed since 1/1/0001 12:00am.
        //
        public DateTime(long ticks)
        {
            if (ticks < MinTicks || ticks > MaxTicks)
                throw new ArgumentOutOfRangeException(nameof(ticks), SR.ArgumentOutOfRange_DateTimeBadTicks);
            _dateData = (ulong)ticks;
        }

        private DateTime(ulong dateData)
        {
            this._dateData = dateData;
        }

        public DateTime(long ticks, DateTimeKind kind)
        {
            if (ticks < MinTicks || ticks > MaxTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks), SR.ArgumentOutOfRange_DateTimeBadTicks);
            }
            if (kind < DateTimeKind.Unspecified || kind > DateTimeKind.Local)
            {
                throw new ArgumentException(SR.Argument_InvalidDateTimeKind, nameof(kind));
            }
            _dateData = ((ulong)ticks | ((ulong)kind << KindShift));
        }

        internal DateTime(long ticks, DateTimeKind kind, bool isAmbiguousDst)
        {
            if (ticks < MinTicks || ticks > MaxTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks), SR.ArgumentOutOfRange_DateTimeBadTicks);
            }
            Debug.Assert(kind == DateTimeKind.Local, "Internal Constructor is for local times only");
            _dateData = ((ulong)ticks | (isAmbiguousDst ? KindLocalAmbiguousDst : KindLocal));
        }

        // Constructs a DateTime from a given year, month, and day. The
        // time-of-day of the resulting DateTime is always midnight.
        //
        public DateTime(int year, int month, int day)
        {
            _dateData = (ulong)DateToTicks(year, month, day);
        }

        // Constructs a DateTime from a given year, month, and day for
        // the specified calendar. The
        // time-of-day of the resulting DateTime is always midnight.
        //
        public DateTime(int year, int month, int day, Calendar calendar)
            : this(year, month, day, 0, 0, 0, calendar)
        {
        }

        // Constructs a DateTime from a given year, month, day, hour,
        // minute, and second.
        //
        public DateTime(int year, int month, int day, int hour, int minute, int second)
        {
            if (second == 60 && s_systemSupportsLeapSeconds && IsValidTimeWithLeapSeconds(year, month, day, hour, minute, second, DateTimeKind.Unspecified))
            {
                // if we have leap second (second = 60) then we'll need to check if it is valid time.
                // if it is valid, then we adjust the second to 59 so DateTime will consider this second is last second
                // in the specified minute.
                // if it is not valid time, we'll eventually throw.
                second = 59;
            }
            _dateData = (ulong)(DateToTicks(year, month, day) + TimeToTicks(hour, minute, second));
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind)
        {
            if (kind < DateTimeKind.Unspecified || kind > DateTimeKind.Local)
            {
                throw new ArgumentException(SR.Argument_InvalidDateTimeKind, nameof(kind));
            }

            if (second == 60 && s_systemSupportsLeapSeconds && IsValidTimeWithLeapSeconds(year, month, day, hour, minute, second, kind))
            {
                // if we have leap second (second = 60) then we'll need to check if it is valid time.
                // if it is valid, then we adjust the second to 59 so DateTime will consider this second is last second
                // in the specified minute.
                // if it is not valid time, we'll eventually throw.
                second = 59;
            }

            long ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
            _dateData = ((ulong)ticks | ((ulong)kind << KindShift));
        }

        // Constructs a DateTime from a given year, month, day, hour,
        // minute, and second for the specified calendar.
        //
        public DateTime(int year, int month, int day, int hour, int minute, int second, Calendar calendar)
        {
            if (calendar == null)
                throw new ArgumentNullException(nameof(calendar));

            int originalSecond = second;
            if (second == 60 && s_systemSupportsLeapSeconds)
            {
                // Reset the second value now and then we'll validate it later when we get the final Gregorian date.
                second = 59;
            }

            _dateData = (ulong)calendar.ToDateTime(year, month, day, hour, minute, second, 0).Ticks;

            if (originalSecond == 60)
            {
                DateTime dt = new DateTime(_dateData);
                if (!IsValidTimeWithLeapSeconds(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 60, DateTimeKind.Unspecified))
                {
                    throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
                }
            }
        }

        // Constructs a DateTime from a given year, month, day, hour,
        // minute, and second.
        //
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecond), SR.Format(SR.ArgumentOutOfRange_Range, 0, MillisPerSecond - 1));
            }

            if (second == 60 && s_systemSupportsLeapSeconds && IsValidTimeWithLeapSeconds(year, month, day, hour, minute, second, DateTimeKind.Unspecified))
            {
                // if we have leap second (second = 60) then we'll need to check if it is valid time.
                // if it is valid, then we adjust the second to 59 so DateTime will consider this second is last second
                // in the specified minute.
                // if it is not valid time, we'll eventually throw.
                second = 59;
            }

            long ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
            ticks += millisecond * TicksPerMillisecond;
            if (ticks < MinTicks || ticks > MaxTicks)
                throw new ArgumentException(SR.Arg_DateTimeRange);
            _dateData = (ulong)ticks;
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind)
        {
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecond), SR.Format(SR.ArgumentOutOfRange_Range, 0, MillisPerSecond - 1));
            }
            if (kind < DateTimeKind.Unspecified || kind > DateTimeKind.Local)
            {
                throw new ArgumentException(SR.Argument_InvalidDateTimeKind, nameof(kind));
            }

            if (second == 60 && s_systemSupportsLeapSeconds && IsValidTimeWithLeapSeconds(year, month, day, hour, minute, second, kind))
            {
                // if we have leap second (second = 60) then we'll need to check if it is valid time.
                // if it is valid, then we adjust the second to 59 so DateTime will consider this second is last second
                // in the specified minute.
                // if it is not valid time, we'll eventually throw.
                second = 59;
            }

            long ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
            ticks += millisecond * TicksPerMillisecond;
            if (ticks < MinTicks || ticks > MaxTicks)
                throw new ArgumentException(SR.Arg_DateTimeRange);
            _dateData = ((ulong)ticks | ((ulong)kind << KindShift));
        }

        // Constructs a DateTime from a given year, month, day, hour,
        // minute, and second for the specified calendar.
        //
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar)
        {
            if (calendar == null)
                throw new ArgumentNullException(nameof(calendar));
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecond), SR.Format(SR.ArgumentOutOfRange_Range, 0, MillisPerSecond - 1));
            }

            int originalSecond = second;
            if (second == 60 && s_systemSupportsLeapSeconds)
            {
                // Reset the second value now and then we'll validate it later when we get the final Gregorian date.
                second = 59;
            }

            long ticks = calendar.ToDateTime(year, month, day, hour, minute, second, 0).Ticks;
            ticks += millisecond * TicksPerMillisecond;
            if (ticks < MinTicks || ticks > MaxTicks)
                throw new ArgumentException(SR.Arg_DateTimeRange);
            _dateData = (ulong)ticks;

            if (originalSecond == 60)
            {
                DateTime dt = new DateTime(_dateData);
                if (!IsValidTimeWithLeapSeconds(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 60, DateTimeKind.Unspecified))
                {
                    throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
                }
            }
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar, DateTimeKind kind)
        {
            if (calendar == null)
                throw new ArgumentNullException(nameof(calendar));
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecond), SR.Format(SR.ArgumentOutOfRange_Range, 0, MillisPerSecond - 1));
            }
            if (kind < DateTimeKind.Unspecified || kind > DateTimeKind.Local)
            {
                throw new ArgumentException(SR.Argument_InvalidDateTimeKind, nameof(kind));
            }

            int originalSecond = second;
            if (second == 60 && s_systemSupportsLeapSeconds)
            {
                // Reset the second value now and then we'll validate it later when we get the final Gregorian date.
                second = 59;
            }

            long ticks = calendar.ToDateTime(year, month, day, hour, minute, second, 0).Ticks;
            ticks += millisecond * TicksPerMillisecond;
            if (ticks < MinTicks || ticks > MaxTicks)
                throw new ArgumentException(SR.Arg_DateTimeRange);
            _dateData = ((ulong)ticks | ((ulong)kind << KindShift));

            if (originalSecond == 60)
            {
                DateTime dt = new DateTime(_dateData);
                if (!IsValidTimeWithLeapSeconds(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 60, kind))
                {
                    throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
                }
            }
        }

        private DateTime(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            bool foundTicks = false;
            bool foundDateData = false;
            long serializedTicks = 0;
            ulong serializedDateData = 0;


            // Get the data
            SerializationInfoEnumerator enumerator = info.GetEnumerator();
            while (enumerator.MoveNext())
            {
                switch (enumerator.Name)
                {
                    case TicksField:
                        serializedTicks = Convert.ToInt64(enumerator.Value, CultureInfo.InvariantCulture);
                        foundTicks = true;
                        break;
                    case DateDataField:
                        serializedDateData = Convert.ToUInt64(enumerator.Value, CultureInfo.InvariantCulture);
                        foundDateData = true;
                        break;
                    default:
                        // Ignore other fields for forward compatibility.
                        break;
                }
            }
            if (foundDateData)
            {
                _dateData = serializedDateData;
            }
            else if (foundTicks)
            {
                _dateData = (ulong)serializedTicks;
            }
            else
            {
                throw new SerializationException(SR.Serialization_MissingDateTimeData);
            }
            long ticks = InternalTicks;
            if (ticks < MinTicks || ticks > MaxTicks)
            {
                throw new SerializationException(SR.Serialization_DateTimeTicksOutOfRange);
            }
        }

        internal long InternalTicks
        {
            get
            {
                return (long)(_dateData & TicksMask);
            }
        }

        private ulong InternalKind
        {
            get
            {
                return (_dateData & FlagsMask);
            }
        }

        // Returns the DateTime resulting from adding the given
        // TimeSpan to this DateTime.
        //
        public DateTime Add(TimeSpan value)
        {
            return AddTicks(value._ticks);
        }

        // Returns the DateTime resulting from adding a fractional number of
        // time units to this DateTime.
        private DateTime Add(double value, int scale)
        {
            double millis_double = value * (double)scale + (value >= 0 ? 0.5 : -0.5);

            if (millis_double <= (double)-MaxMillis || millis_double >= (double)MaxMillis)
                 throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_AddValue);

            return AddTicks((long)millis_double * TicksPerMillisecond);
        }

        // Returns the DateTime resulting from adding a fractional number of
        // days to this DateTime. The result is computed by rounding the
        // fractional number of days given by value to the nearest
        // millisecond, and adding that interval to this DateTime. The
        // value argument is permitted to be negative.
        //
        public DateTime AddDays(double value)
        {
            return Add(value, MillisPerDay);
        }

        // Returns the DateTime resulting from adding a fractional number of
        // hours to this DateTime. The result is computed by rounding the
        // fractional number of hours given by value to the nearest
        // millisecond, and adding that interval to this DateTime. The
        // value argument is permitted to be negative.
        //
        public DateTime AddHours(double value)
        {
            return Add(value, MillisPerHour);
        }

        // Returns the DateTime resulting from the given number of
        // milliseconds to this DateTime. The result is computed by rounding
        // the number of milliseconds given by value to the nearest integer,
        // and adding that interval to this DateTime. The value
        // argument is permitted to be negative.
        //
        public DateTime AddMilliseconds(double value)
        {
            return Add(value, 1);
        }

        // Returns the DateTime resulting from adding a fractional number of
        // minutes to this DateTime. The result is computed by rounding the
        // fractional number of minutes given by value to the nearest
        // millisecond, and adding that interval to this DateTime. The
        // value argument is permitted to be negative.
        //
        public DateTime AddMinutes(double value)
        {
            return Add(value, MillisPerMinute);
        }

        // Returns the DateTime resulting from adding the given number of
        // months to this DateTime. The result is computed by incrementing
        // (or decrementing) the year and month parts of this DateTime by
        // months months, and, if required, adjusting the day part of the
        // resulting date downwards to the last day of the resulting month in the
        // resulting year. The time-of-day part of the result is the same as the
        // time-of-day part of this DateTime.
        //
        // In more precise terms, considering this DateTime to be of the
        // form y / m / d + t, where y is the
        // year, m is the month, d is the day, and t is the
        // time-of-day, the result is y1 / m1 / d1 + t,
        // where y1 and m1 are computed by adding months months
        // to y and m, and d1 is the largest value less than
        // or equal to d that denotes a valid day in month m1 of year
        // y1.
        //
        public DateTime AddMonths(int months)
        {
            if (months < -120000 || months > 120000) throw new ArgumentOutOfRangeException(nameof(months), SR.ArgumentOutOfRange_DateTimeBadMonths);
            GetDatePart(out int y, out int m, out int d);
            int i = m - 1 + months;
            if (i >= 0)
            {
                m = i % 12 + 1;
                y = y + i / 12;
            }
            else
            {
                m = 12 + (i + 1) % 12;
                y = y + (i - 11) / 12;
            }
            if (y < 1 || y > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(months), SR.ArgumentOutOfRange_DateArithmetic);
            }
            int days = DaysInMonth(y, m);
            if (d > days) d = days;
            return new DateTime((ulong)(DateToTicks(y, m, d) + InternalTicks % TicksPerDay) | InternalKind);
        }

        // Returns the DateTime resulting from adding a fractional number of
        // seconds to this DateTime. The result is computed by rounding the
        // fractional number of seconds given by value to the nearest
        // millisecond, and adding that interval to this DateTime. The
        // value argument is permitted to be negative.
        //
        public DateTime AddSeconds(double value)
        {
            return Add(value, MillisPerSecond);
        }

        // Returns the DateTime resulting from adding the given number of
        // 100-nanosecond ticks to this DateTime. The value argument
        // is permitted to be negative.
        //
        public DateTime AddTicks(long value)
        {
            long ticks = InternalTicks;
            if (value > MaxTicks - ticks || value < MinTicks - ticks)
            {
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_DateArithmetic);
            }
            return new DateTime((ulong)(ticks + value) | InternalKind);
        }

        // TryAddTicks is exact as AddTicks except it doesn't throw
        internal bool TryAddTicks(long value, out DateTime result)
        {
            long ticks = InternalTicks;
            if (value > MaxTicks - ticks || value < MinTicks - ticks)
            {
                result = default(DateTime);
                return false;
            }
            result = new DateTime((ulong)(ticks + value) | InternalKind);
            return true;
        }

        // Returns the DateTime resulting from adding the given number of
        // years to this DateTime. The result is computed by incrementing
        // (or decrementing) the year part of this DateTime by value
        // years. If the month and day of this DateTime is 2/29, and if the
        // resulting year is not a leap year, the month and day of the resulting
        // DateTime becomes 2/28. Otherwise, the month, day, and time-of-day
        // parts of the result are the same as those of this DateTime.
        //
        public DateTime AddYears(int value)
        {
            if (value < -10000 || value > 10000)
            {
                // DateTimeOffset.AddYears(int years) is implemented on top of DateTime.AddYears(int value). Use the more appropriate
                // parameter name out of the two for the exception.
                throw new ArgumentOutOfRangeException("years", SR.ArgumentOutOfRange_DateTimeBadYears);
            }
            return AddMonths(value * 12);
        }

        // Compares two DateTime values, returning an integer that indicates
        // their relationship.
        //
        public static int Compare(DateTime t1, DateTime t2)
        {
            long ticks1 = t1.InternalTicks;
            long ticks2 = t2.InternalTicks;
            if (ticks1 > ticks2) return 1;
            if (ticks1 < ticks2) return -1;
            return 0;
        }

        // Compares this DateTime to a given object. This method provides an
        // implementation of the IComparable interface. The object
        // argument must be another DateTime, or otherwise an exception
        // occurs.  Null is considered less than any instance.
        //
        // Returns a value less than zero if this  object
        public int CompareTo(object? value)
        {
            if (value == null) return 1;
            if (!(value is DateTime))
            {
                throw new ArgumentException(SR.Arg_MustBeDateTime);
            }

            return Compare(this, (DateTime)value);
        }

        public int CompareTo(DateTime value)
        {
            return Compare(this, value);
        }

        // Returns the tick count corresponding to the given year, month, and day.
        // Will check the if the parameters are valid.
        private static long DateToTicks(int year, int month, int day)
        {
            if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
            {
                int[] days = IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;
                if (day >= 1 && day <= days[month] - days[month - 1])
                {
                    int y = year - 1;
                    int n = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
                    return n * TicksPerDay;
                }
            }
            throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
        }

        // Return the tick count corresponding to the given hour, minute, second.
        // Will check the if the parameters are valid.
        private static long TimeToTicks(int hour, int minute, int second)
        {
            //TimeSpan.TimeToTicks is a family access function which does no error checking, so
            //we need to put some error checking out here.
            if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >= 0 && second < 60)
            {
                return (TimeSpan.TimeToTicks(hour, minute, second));
            }
            throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
        }

        // Returns the number of days in the month given by the year and
        // month arguments.
        //
        public static int DaysInMonth(int year, int month)
        {
            if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month), SR.ArgumentOutOfRange_Month);
            // IsLeapYear checks the year argument
            int[] days = IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;
            return days[month] - days[month - 1];
        }

        // Converts an OLE Date to a tick count.
        // This function is duplicated in COMDateTime.cpp
        internal static long DoubleDateToTicks(double value)
        {
            // The check done this way will take care of NaN
            if (!(value < OADateMaxAsDouble) || !(value > OADateMinAsDouble))
                throw new ArgumentException(SR.Arg_OleAutDateInvalid);

            // Conversion to long will not cause an overflow here, as at this point the "value" is in between OADateMinAsDouble and OADateMaxAsDouble
            long millis = (long)(value * MillisPerDay + (value >= 0 ? 0.5 : -0.5));
            // The interesting thing here is when you have a value like 12.5 it all positive 12 days and 12 hours from 01/01/1899
            // However if you a value of -12.25 it is minus 12 days but still positive 6 hours, almost as though you meant -11.75 all negative
            // This line below fixes up the millis in the negative case
            if (millis < 0)
            {
                millis -= (millis % MillisPerDay) * 2;
            }

            millis += DoubleDateOffset / TicksPerMillisecond;

            if (millis < 0 || millis >= MaxMillis) throw new ArgumentException(SR.Arg_OleAutDateScale);
            return millis * TicksPerMillisecond;
        }

        // Checks if this DateTime is equal to a given object. Returns
        // true if the given object is a boxed DateTime and its value
        // is equal to the value of this DateTime. Returns false
        // otherwise.
        //
        public override bool Equals(object? value)
        {
            if (value is DateTime)
            {
                return InternalTicks == ((DateTime)value).InternalTicks;
            }
            return false;
        }

        public bool Equals(DateTime value)
        {
            return InternalTicks == value.InternalTicks;
        }

        // Compares two DateTime values for equality. Returns true if
        // the two DateTime values are equal, or false if they are
        // not equal.
        //
        public static bool Equals(DateTime t1, DateTime t2)
        {
            return t1.InternalTicks == t2.InternalTicks;
        }

        public static DateTime FromBinary(long dateData)
        {
            if ((dateData & (unchecked((long)LocalMask))) != 0)
            {
                // Local times need to be adjusted as you move from one time zone to another,
                // just as they are when serializing in text. As such the format for local times
                // changes to store the ticks of the UTC time, but with flags that look like a
                // local date.
                long ticks = dateData & (unchecked((long)TicksMask));
                // Negative ticks are stored in the top part of the range and should be converted back into a negative number
                if (ticks > TicksCeiling - TicksPerDay)
                {
                    ticks = ticks - TicksCeiling;
                }
                // Convert the ticks back to local. If the UTC ticks are out of range, we need to default to
                // the UTC offset from MinValue and MaxValue to be consistent with Parse.
                bool isAmbiguousLocalDst = false;
                long offsetTicks;
                if (ticks < MinTicks)
                {
                    offsetTicks = TimeZoneInfo.GetLocalUtcOffset(DateTime.MinValue, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
                }
                else if (ticks > MaxTicks)
                {
                    offsetTicks = TimeZoneInfo.GetLocalUtcOffset(DateTime.MaxValue, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
                }
                else
                {
                    // Because the ticks conversion between UTC and local is lossy, we need to capture whether the
                    // time is in a repeated hour so that it can be passed to the DateTime constructor.
                    DateTime utcDt = new DateTime(ticks, DateTimeKind.Utc);
                    bool isDaylightSavings = false;
                    offsetTicks = TimeZoneInfo.GetUtcOffsetFromUtc(utcDt, TimeZoneInfo.Local, out isDaylightSavings, out isAmbiguousLocalDst).Ticks;
                }
                ticks += offsetTicks;
                // Another behaviour of parsing is to cause small times to wrap around, so that they can be used
                // to compare times of day
                if (ticks < 0)
                {
                    ticks += TicksPerDay;
                }
                if (ticks < MinTicks || ticks > MaxTicks)
                {
                    throw new ArgumentException(SR.Argument_DateTimeBadBinaryData, nameof(dateData));
                }
                return new DateTime(ticks, DateTimeKind.Local, isAmbiguousLocalDst);
            }
            else
            {
                return DateTime.FromBinaryRaw(dateData);
            }
        }

        // A version of ToBinary that uses the real representation and does not adjust local times. This is needed for
        // scenarios where the serialized data must maintain compatibility
        internal static DateTime FromBinaryRaw(long dateData)
        {
            long ticks = dateData & (long)TicksMask;
            if (ticks < MinTicks || ticks > MaxTicks)
                throw new ArgumentException(SR.Argument_DateTimeBadBinaryData, nameof(dateData));
            return new DateTime((ulong)dateData);
        }

        // Creates a DateTime from a Windows filetime. A Windows filetime is
        // a long representing the date and time as the number of
        // 100-nanosecond intervals that have elapsed since 1/1/1601 12:00am.
        //
        public static DateTime FromFileTime(long fileTime)
        {
            return FromFileTimeUtc(fileTime).ToLocalTime();
        }

        public static DateTime FromFileTimeUtc(long fileTime)
        {
            if (fileTime < 0 || fileTime > MaxTicks - FileTimeOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(fileTime), SR.ArgumentOutOfRange_FileTimeInvalid);
            }

#pragma warning disable 162 // Unrechable code on Unix
            if (s_systemSupportsLeapSeconds)
            {
                return FromFileTimeLeapSecondsAware(fileTime);
            }
#pragma warning restore 162

            // This is the ticks in Universal time for this fileTime.
            long universalTicks = fileTime + FileTimeOffset;
            return new DateTime(universalTicks, DateTimeKind.Utc);
        }

        // Creates a DateTime from an OLE Automation Date.
        //
        public static DateTime FromOADate(double d)
        {
            return new DateTime(DoubleDateToTicks(d), DateTimeKind.Unspecified);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            // Serialize both the old and the new format
            info.AddValue(TicksField, InternalTicks);
            info.AddValue(DateDataField, _dateData);
        }

        public bool IsDaylightSavingTime()
        {
            if (Kind == DateTimeKind.Utc)
            {
                return false;
            }
            return TimeZoneInfo.Local.IsDaylightSavingTime(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
        }

        public static DateTime SpecifyKind(DateTime value, DateTimeKind kind)
        {
            return new DateTime(value.InternalTicks, kind);
        }

        public long ToBinary()
        {
            if (Kind == DateTimeKind.Local)
            {
                // Local times need to be adjusted as you move from one time zone to another,
                // just as they are when serializing in text. As such the format for local times
                // changes to store the ticks of the UTC time, but with flags that look like a
                // local date.

                // To match serialization in text we need to be able to handle cases where
                // the UTC value would be out of range. Unused parts of the ticks range are
                // used for this, so that values just past max value are stored just past the
                // end of the maximum range, and values just below minimum value are stored
                // at the end of the ticks area, just below 2^62.
                TimeSpan offset = TimeZoneInfo.GetLocalUtcOffset(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                long ticks = Ticks;
                long storedTicks = ticks - offset.Ticks;
                if (storedTicks < 0)
                {
                    storedTicks = TicksCeiling + storedTicks;
                }
                return storedTicks | (unchecked((long)LocalMask));
            }
            else
            {
                return (long)_dateData;
            }
        }

        // Returns the date part of this DateTime. The resulting value
        // corresponds to this DateTime with the time-of-day part set to
        // zero (midnight).
        //
        public DateTime Date
        {
            get
            {
                long ticks = InternalTicks;
                return new DateTime((ulong)(ticks - ticks % TicksPerDay) | InternalKind);
            }
        }

        // Returns a given date part of this DateTime. This method is used
        // to compute the year, day-of-year, month, or day part.
        private int GetDatePart(int part)
        {
            long ticks = InternalTicks;
            // n = number of days since 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            int y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;
            // If year was requested, compute and return it
            if (part == DatePartYear)
            {
                return y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            }
            // n = day number within year
            n -= y1 * DaysPerYear;
            // If day-of-year was requested, return it
            if (part == DatePartDayOfYear) return n + 1;
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            int[] days = leapYear ? s_daysToMonth366 : s_daysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            int m = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // If month was requested, return it
            if (part == DatePartMonth) return m;
            // Return 1-based day-of-month
            return n - days[m - 1] + 1;
        }

        // Exactly the same as GetDatePart(int part), except computing all of
        // year/month/day rather than just one of them.  Used when all three
        // are needed rather than redoing the computations for each.
        internal void GetDatePart(out int year, out int month, out int day)
        {
            long ticks = InternalTicks;
            // n = number of days since 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            int y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;
            // compute year
            year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            // n = day number within year
            n -= y1 * DaysPerYear;
            // dayOfYear = n + 1;
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            int[] days = leapYear ? s_daysToMonth366 : s_daysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            int m = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // compute month and day
            month = m;
            day = n - days[m - 1] + 1;
        }

        // Returns the day-of-month part of this DateTime. The returned
        // value is an integer between 1 and 31.
        //
        public int Day
        {
            get
            {
                return GetDatePart(DatePartDay);
            }
        }

        // Returns the day-of-week part of this DateTime. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //
        public DayOfWeek DayOfWeek
        {
            get
            {
                return (DayOfWeek)((InternalTicks / TicksPerDay + 1) % 7);
            }
        }

        // Returns the day-of-year part of this DateTime. The returned value
        // is an integer between 1 and 366.
        //
        public int DayOfYear
        {
            get
            {
                return GetDatePart(DatePartDayOfYear);
            }
        }

        // Returns the hash code for this DateTime.
        //
        public override int GetHashCode()
        {
            long ticks = InternalTicks;
            return unchecked((int)ticks) ^ (int)(ticks >> 32);
        }

        // Returns the hour part of this DateTime. The returned value is an
        // integer between 0 and 23.
        //
        public int Hour
        {
            get
            {
                return (int)((InternalTicks / TicksPerHour) % 24);
            }
        }

        internal bool IsAmbiguousDaylightSavingTime()
        {
            return (InternalKind == KindLocalAmbiguousDst);
        }

        public DateTimeKind Kind
        {
            get
            {
                switch (InternalKind)
                {
                    case KindUnspecified:
                        return DateTimeKind.Unspecified;
                    case KindUtc:
                        return DateTimeKind.Utc;
                    default:
                        return DateTimeKind.Local;
                }
            }
        }

        // Returns the millisecond part of this DateTime. The returned value
        // is an integer between 0 and 999.
        //
        public int Millisecond
        {
            get
            {
                return (int)((InternalTicks / TicksPerMillisecond) % 1000);
            }
        }

        // Returns the minute part of this DateTime. The returned value is
        // an integer between 0 and 59.
        //
        public int Minute
        {
            get
            {
                return (int)((InternalTicks / TicksPerMinute) % 60);
            }
        }

        // Returns the month part of this DateTime. The returned value is an
        // integer between 1 and 12.
        //
        public int Month
        {
            get
            {
                return GetDatePart(DatePartMonth);
            }
        }

        // Returns a DateTime representing the current date and time. The
        // resolution of the returned value depends on the system timer.
        public static DateTime Now
        {
            get
            {
                DateTime utc = UtcNow;
                bool isAmbiguousLocalDst = false;
                long offset = TimeZoneInfo.GetDateTimeNowUtcOffsetFromUtc(utc, out isAmbiguousLocalDst).Ticks;
                long tick = utc.Ticks + offset;
                if (tick > DateTime.MaxTicks)
                {
                    return new DateTime(DateTime.MaxTicks, DateTimeKind.Local);
                }
                if (tick < DateTime.MinTicks)
                {
                    return new DateTime(DateTime.MinTicks, DateTimeKind.Local);
                }
                return new DateTime(tick, DateTimeKind.Local, isAmbiguousLocalDst);
            }
        }

        // Returns the second part of this DateTime. The returned value is
        // an integer between 0 and 59.
        //
        public int Second
        {
            get
            {
                return (int)((InternalTicks / TicksPerSecond) % 60);
            }
        }

        // Returns the tick count for this DateTime. The returned value is
        // the number of 100-nanosecond intervals that have elapsed since 1/1/0001
        // 12:00am.
        //
        public long Ticks
        {
            get
            {
                return InternalTicks;
            }
        }

        // Returns the time-of-day part of this DateTime. The returned value
        // is a TimeSpan that indicates the time elapsed since midnight.
        //
        public TimeSpan TimeOfDay
        {
            get
            {
                return new TimeSpan(InternalTicks % TicksPerDay);
            }
        }

        // Returns a DateTime representing the current date. The date part
        // of the returned value is the current date, and the time-of-day part of
        // the returned value is zero (midnight).
        //
        public static DateTime Today
        {
            get
            {
                return DateTime.Now.Date;
            }
        }

        // Returns the year part of this DateTime. The returned value is an
        // integer between 1 and 9999.
        //
        public int Year
        {
            get
            {
                return GetDatePart(DatePartYear);
            }
        }

        // Checks whether a given year is a leap year. This method returns true if
        // year is a leap year, or false if not.
        //
        public static bool IsLeapYear(int year)
        {
            if (year < 1 || year > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(year), SR.ArgumentOutOfRange_Year);
            }
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return (DateTimeParse.Parse(s, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None));
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return (DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), DateTimeStyles.None));
        }

        public static DateTime Parse(string s, IFormatProvider? provider, DateTimeStyles styles)
        {
            DateTimeFormatInfo.ValidateStyles(styles, nameof(styles));
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return (DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), styles));
        }

        public static DateTime Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null, DateTimeStyles styles = DateTimeStyles.None)
        {
            DateTimeFormatInfo.ValidateStyles(styles, nameof(styles));
            return DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), styles);
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime ParseExact(string s, string format, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            if (format == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
            return (DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), DateTimeStyles.None));
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime ParseExact(string s, string format, IFormatProvider? provider, DateTimeStyles style)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            if (format == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
            return (DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style));
        }

        public static DateTime ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));
            return DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style);
        }

        public static DateTime ParseExact(string s, string[] formats, IFormatProvider? provider, DateTimeStyles style)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return DateTimeParse.ParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style);
        }

        public static DateTime ParseExact(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));
            return DateTimeParse.ParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style);
        }

        public TimeSpan Subtract(DateTime value)
        {
            return new TimeSpan(InternalTicks - value.InternalTicks);
        }

        public DateTime Subtract(TimeSpan value)
        {
            long ticks = InternalTicks;
            long valueTicks = value._ticks;
            if (ticks - MinTicks < valueTicks || ticks - MaxTicks > valueTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_DateArithmetic);
            }
            return new DateTime((ulong)(ticks - valueTicks) | InternalKind);
        }

        // This function is duplicated in COMDateTime.cpp
        private static double TicksToOADate(long value)
        {
            if (value == 0)
                return 0.0;  // Returns OleAut's zero'ed date value.
            if (value < TicksPerDay) // This is a fix for VB. They want the default day to be 1/1/0001 rathar then 12/30/1899.
                value += DoubleDateOffset; // We could have moved this fix down but we would like to keep the bounds check.
            if (value < OADateMinAsTicks)
                throw new OverflowException(SR.Arg_OleAutDateInvalid);
            // Currently, our max date == OA's max date (12/31/9999), so we don't
            // need an overflow check in that direction.
            long millis = (value - DoubleDateOffset) / TicksPerMillisecond;
            if (millis < 0)
            {
                long frac = millis % MillisPerDay;
                if (frac != 0) millis -= (MillisPerDay + frac) * 2;
            }
            return (double)millis / MillisPerDay;
        }

        // Converts the DateTime instance into an OLE Automation compatible
        // double date.
        public double ToOADate()
        {
            return TicksToOADate(InternalTicks);
        }

        public long ToFileTime()
        {
            // Treats the input as local if it is not specified
            return ToUniversalTime().ToFileTimeUtc();
        }

        public long ToFileTimeUtc()
        {
            // Treats the input as universal if it is not specified
            long ticks = ((InternalKind & LocalMask) != 0) ? ToUniversalTime().InternalTicks : this.InternalTicks;

#pragma warning disable 162 // Unrechable code on Unix
            if (s_systemSupportsLeapSeconds)
            {
                return ToFileTimeLeapSecondsAware(ticks);
            }
#pragma warning restore 162

            ticks -= FileTimeOffset;
            if (ticks < 0)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_FileTimeInvalid);
            }

            return ticks;
        }

        public DateTime ToLocalTime()
        {
            return ToLocalTime(false);
        }

        internal DateTime ToLocalTime(bool throwOnOverflow)
        {
            if (Kind == DateTimeKind.Local)
            {
                return this;
            }
            bool isDaylightSavings = false;
            bool isAmbiguousLocalDst = false;
            long offset = TimeZoneInfo.GetUtcOffsetFromUtc(this, TimeZoneInfo.Local, out isDaylightSavings, out isAmbiguousLocalDst).Ticks;
            long tick = Ticks + offset;
            if (tick > DateTime.MaxTicks)
            {
                if (throwOnOverflow)
                    throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);
                else
                    return new DateTime(DateTime.MaxTicks, DateTimeKind.Local);
            }
            if (tick < DateTime.MinTicks)
            {
                if (throwOnOverflow)
                    throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);
                else
                    return new DateTime(DateTime.MinTicks, DateTimeKind.Local);
            }
            return new DateTime(tick, DateTimeKind.Local, isAmbiguousLocalDst);
        }

        public string ToLongDateString()
        {
            return DateTimeFormat.Format(this, "D", null);
        }

        public string ToLongTimeString()
        {
            return DateTimeFormat.Format(this, "T", null);
        }

        public string ToShortDateString()
        {
            return DateTimeFormat.Format(this, "d", null);
        }

        public string ToShortTimeString()
        {
            return DateTimeFormat.Format(this, "t", null);
        }

        public override string ToString()
        {
            return DateTimeFormat.Format(this, null, null);
        }

        public string ToString(string? format)
        {
            return DateTimeFormat.Format(this, format, null);
        }

        public string ToString(IFormatProvider? provider)
        {
            return DateTimeFormat.Format(this, null, provider);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return DateTimeFormat.Format(this, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            DateTimeFormat.TryFormat(this, destination, out charsWritten, format, provider);

        public DateTime ToUniversalTime()
        {
            return TimeZoneInfo.ConvertTimeToUtc(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
        }

        public static bool TryParse(string? s, out DateTime result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }
            return DateTimeParse.TryParse(s, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out DateTime result)
        {
            return DateTimeParse.TryParse(s, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out result);
        }

        public static bool TryParse(string? s, IFormatProvider? provider, DateTimeStyles styles, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(styles, nameof(styles));

            if (s == null)
            {
                result = default;
                return false;
            }

            return DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), styles, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles styles, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(styles, nameof(styles));
            return DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), styles, out result);
        }

        public static bool TryParseExact(string? s, string? format, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));

            if (s == null || format == null)
            {
                result = default;
                return false;
            }

            return DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));
            return DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static bool TryParseExact(string? s, string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));

            if (s == null)
            {
                result = default;
                return false;
            }

            return DateTimeParse.TryParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static bool TryParseExact(ReadOnlySpan<char> s, string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style, nameof(style));
            return DateTimeParse.TryParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static DateTime operator +(DateTime d, TimeSpan t)
        {
            long ticks = d.InternalTicks;
            long valueTicks = t._ticks;
            if (valueTicks > MaxTicks - ticks || valueTicks < MinTicks - ticks)
            {
                throw new ArgumentOutOfRangeException(nameof(t), SR.ArgumentOutOfRange_DateArithmetic);
            }
            return new DateTime((ulong)(ticks + valueTicks) | d.InternalKind);
        }

        public static DateTime operator -(DateTime d, TimeSpan t)
        {
            long ticks = d.InternalTicks;
            long valueTicks = t._ticks;
            if (ticks - MinTicks < valueTicks || ticks - MaxTicks > valueTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(t), SR.ArgumentOutOfRange_DateArithmetic);
            }
            return new DateTime((ulong)(ticks - valueTicks) | d.InternalKind);
        }

        public static TimeSpan operator -(DateTime d1, DateTime d2)
        {
            return new TimeSpan(d1.InternalTicks - d2.InternalTicks);
        }

        public static bool operator ==(DateTime d1, DateTime d2)
        {
            return d1.InternalTicks == d2.InternalTicks;
        }

        public static bool operator !=(DateTime d1, DateTime d2)
        {
            return d1.InternalTicks != d2.InternalTicks;
        }

        public static bool operator <(DateTime t1, DateTime t2)
        {
            return t1.InternalTicks < t2.InternalTicks;
        }

        public static bool operator <=(DateTime t1, DateTime t2)
        {
            return t1.InternalTicks <= t2.InternalTicks;
        }

        public static bool operator >(DateTime t1, DateTime t2)
        {
            return t1.InternalTicks > t2.InternalTicks;
        }

        public static bool operator >=(DateTime t1, DateTime t2)
        {
            return t1.InternalTicks >= t2.InternalTicks;
        }


        // Returns a string array containing all of the known date and time options for the
        // current culture.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats()
        {
            return (GetDateTimeFormats(CultureInfo.CurrentCulture));
        }

        // Returns a string array containing all of the known date and time options for the
        // using the information provided by IFormatProvider.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats(IFormatProvider? provider)
        {
            return (DateTimeFormat.GetAllDateTimes(this, DateTimeFormatInfo.GetInstance(provider)));
        }


        // Returns a string array containing all of the date and time options for the
        // given format format and current culture.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats(char format)
        {
            return (GetDateTimeFormats(format, CultureInfo.CurrentCulture));
        }

        // Returns a string array containing all of the date and time options for the
        // given format format and given culture.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats(char format, IFormatProvider? provider)
        {
            return (DateTimeFormat.GetAllDateTimes(this, format, DateTimeFormatInfo.GetInstance(provider)));
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.DateTime;
        }


        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Boolean"));
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "SByte"));
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Byte"));
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Int16"));
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "UInt16"));
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Int32"));
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "UInt32"));
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Int64"));
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "UInt64"));
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Single"));
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Double"));
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", "Decimal"));
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            return this;
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        // Tries to construct a DateTime from a given year, month, day, hour,
        // minute, second and millisecond.
        //
        internal static bool TryCreate(int year, int month, int day, int hour, int minute, int second, int millisecond, out DateTime result)
        {
            result = DateTime.MinValue;
            if (year < 1 || year > 9999 || month < 1 || month > 12)
            {
                return false;
            }
            int[] days = IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;
            if (day < 1 || day > days[month] - days[month - 1])
            {
                return false;
            }
            if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60 || second < 0 || second > 60)
            {
                return false;
            }
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                return false;
            }

            if (second == 60)
            {
                if (s_systemSupportsLeapSeconds && IsValidTimeWithLeapSeconds(year, month, day, hour, minute, second, DateTimeKind.Unspecified))
                {
                    // if we have leap second (second = 60) then we'll need to check if it is valid time.
                    // if it is valid, then we adjust the second to 59 so DateTime will consider this second is last second
                    // of this minute.
                    // if it is not valid time, we'll eventually throw.
                    // although this is unspecified datetime kind, we'll assume the passed time is UTC to check the leap seconds.
                    second = 59;
                }
                else
                {
                    return false;
                }
            }

            long ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);

            ticks += millisecond * TicksPerMillisecond;
            if (ticks < MinTicks || ticks > MaxTicks)
            {
                return false;
            }
            result = new DateTime(ticks, DateTimeKind.Unspecified);
            return true;
        }
    }
}
