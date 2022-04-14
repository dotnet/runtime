// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

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
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly partial struct DateTime
        : IComparable,
          ISpanFormattable,
          IConvertible,
          IComparable<DateTime>,
          IEquatable<DateTime>,
          ISerializable,
          IAdditionOperators<DateTime, TimeSpan, DateTime>,
          IAdditiveIdentity<DateTime, TimeSpan>,
          IComparisonOperators<DateTime, DateTime>,
          IMinMaxValue<DateTime>,
          ISpanParsable<DateTime>,
          ISubtractionOperators<DateTime, TimeSpan, DateTime>,
          ISubtractionOperators<DateTime, DateTime, TimeSpan>
    {
        // Number of 100ns ticks per time unit
        internal const int MicrosecondsPerMillisecond = 1000;
        private const long TicksPerMicrosecond = 10;
        private const long TicksPerMillisecond = TicksPerMicrosecond * MicrosecondsPerMillisecond;

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
        private const long MaxMicroseconds = MaxMillis * MicrosecondsPerMillisecond;

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

        private static readonly uint[] s_daysToMonth365 = {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        private static readonly uint[] s_daysToMonth366 = {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

        private static ReadOnlySpan<byte> DaysInMonth365 => new byte[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private static ReadOnlySpan<byte> DaysInMonth366 => new byte[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public static readonly DateTime MinValue;
        public static readonly DateTime MaxValue = new DateTime(MaxTicks, DateTimeKind.Unspecified);
        public static readonly DateTime UnixEpoch = new DateTime(UnixEpochTicks, DateTimeKind.Utc);

        private const ulong TicksMask = 0x3FFFFFFFFFFFFFFF;
        private const ulong FlagsMask = 0xC000000000000000;
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
            if ((ulong)ticks > MaxTicks) ThrowTicksOutOfRange();
            _dateData = (ulong)ticks;
        }

        private DateTime(ulong dateData)
        {
            this._dateData = dateData;
        }

        internal static DateTime UnsafeCreate(long ticks) => new DateTime((ulong) ticks);

        public DateTime(long ticks, DateTimeKind kind)
        {
            if ((ulong)ticks > MaxTicks) ThrowTicksOutOfRange();
            if ((uint)kind > (uint)DateTimeKind.Local) ThrowInvalidKind();
            _dateData = (ulong)ticks | ((ulong)(uint)kind << KindShift);
        }

        internal DateTime(long ticks, DateTimeKind kind, bool isAmbiguousDst)
        {
            if ((ulong)ticks > MaxTicks) ThrowTicksOutOfRange();
            Debug.Assert(kind == DateTimeKind.Local, "Internal Constructor is for local times only");
            _dateData = ((ulong)ticks | (isAmbiguousDst ? KindLocalAmbiguousDst : KindLocal));
        }

        private static void ThrowTicksOutOfRange() => throw new ArgumentOutOfRangeException("ticks", SR.ArgumentOutOfRange_DateTimeBadTicks);
        private static void ThrowInvalidKind() => throw new ArgumentException(SR.Argument_InvalidDateTimeKind, "kind");
        private static void ThrowMillisecondOutOfRange() => throw new ArgumentOutOfRangeException("millisecond", SR.Format(SR.ArgumentOutOfRange_Range, 0, MillisPerSecond - 1));
        private static void ThrowMicrosecondOutOfRange() => throw new ArgumentOutOfRangeException("microsecond", SR.Format(SR.ArgumentOutOfRange_Range, 0, MicrosecondsPerMillisecond - 1));
        private static void ThrowDateArithmetic(int param) => throw new ArgumentOutOfRangeException(param switch { 0 => "value", 1 => "t", _ => "months" }, SR.ArgumentOutOfRange_DateArithmetic);

        // Constructs a DateTime from a given year, month, and day. The
        // time-of-day of the resulting DateTime is always midnight.
        //
        public DateTime(int year, int month, int day)
        {
            _dateData = DateToTicks(year, month, day);
        }

        // Constructs a DateTime from a given year, month, and day for
        // the specified calendar. The
        // time-of-day of the resulting DateTime is always midnight.
        //
        public DateTime(int year, int month, int day, Calendar calendar)
            : this(year, month, day, 0, 0, 0, calendar)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through the number of years in <paramref name="calendar"/>).</param>
        /// <param name="month">The month (1 through the number of months in <paramref name="calendar"/>).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="calendar">The calendar that is used to interpret <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/>.</param>
        /// <param name="kind">
        /// One of the enumeration values that indicates whether <paramref name="year"/>, <paramref name="month"/>, <paramref name="day"/>,
        /// <paramref name="hour"/>, <paramref name="minute"/>, <paramref name="second"/>, and <paramref name="millisecond"/>
        /// specify a local time, Coordinated Universal Time (UTC), or neither.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="calendar"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is not in the range supported by <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than the number of months in <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="kind"/> is not one of the <see cref="DateTimeKind"/> values.
        /// </exception>
        /// <remarks>
        /// The allowable values for <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/> parameters
        /// depend on the <paramref name="calendar"/> parameter. An exception is thrown if the specified date and time cannot
        /// be expressed using <paramref name="calendar"/>.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar!!, DateTimeKind kind)
        {
            if ((uint)millisecond >= MillisPerSecond) ThrowMillisecondOutOfRange();
            if ((uint)kind > (uint)DateTimeKind.Local) ThrowInvalidKind();

            if (second != 60 || !s_systemSupportsLeapSeconds)
            {
                ulong ticks = calendar.ToDateTime(year, month, day, hour, minute, second, millisecond).UTicks;
                _dateData = ticks | ((ulong)kind << KindShift);
            }
            else
            {
                // if we have a leap second, then we adjust it to 59 so that DateTime will consider it the last in the specified minute.
                this = new DateTime(year, month, day, hour, minute, 59, millisecond, calendar, kind);
                ValidateLeapSecond();
            }
        }

        // Constructs a DateTime from a given year, month, day, hour,
        // minute, and second.
        //
        public DateTime(int year, int month, int day, int hour, int minute, int second)
        {
            if (second != 60 || !s_systemSupportsLeapSeconds)
            {
                _dateData = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
            }
            else
            {
                // if we have a leap second, then we adjust it to 59 so that DateTime will consider it the last in the specified minute.
                this = new DateTime(year, month, day, hour, minute, 59);
                ValidateLeapSecond();
            }
        }

        public DateTime(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind)
        {
            if ((uint)kind > (uint)DateTimeKind.Local) ThrowInvalidKind();

            if (second != 60 || !s_systemSupportsLeapSeconds)
            {
                ulong ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
                _dateData = ticks | ((ulong)kind << KindShift);
            }
            else
            {
                // if we have a leap second, then we adjust it to 59 so that DateTime will consider it the last in the specified minute.
                this = new DateTime(year, month, day, hour, minute, 59, kind);
                ValidateLeapSecond();
            }
        }

        // Constructs a DateTime from a given year, month, day, hour,
        // minute, and second for the specified calendar.
        //
        public DateTime(int year, int month, int day, int hour, int minute, int second, Calendar calendar!!)
        {
            if (second != 60 || !s_systemSupportsLeapSeconds)
            {
                _dateData = calendar.ToDateTime(year, month, day, hour, minute, second, 0).UTicks;
            }
            else
            {
                // if we have a leap second, then we adjust it to 59 so that DateTime will consider it the last in the specified minute.
                this = new DateTime(year, month, day, hour, minute, 59, calendar);
                ValidateLeapSecond();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is less than 1 or greater than 9999.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than 12.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        /// </exception>
        /// <remarks>
        /// This constructor interprets <paramref name="year"/>, <paramref name="month"/> and <paramref name="day"/> as a year, month and day
        /// in the Gregorian calendar. To instantiate a <see cref="DateTime"/> value by using the year, month and day in another calendar, call
        /// the <see cref="DateTime(int, int, int, int, int, int, int, int, Calendar)"/> constructor.
        ///
        /// The <see cref="Kind"/> property is initialized to <see cref="DateTimeKind.Unspecified"/>.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) =>
            _dateData = Init(year, month, day, hour, minute, second, millisecond);

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="kind">
        /// One of the enumeration values that indicates whether <paramref name="year"/>, <paramref name="month"/>, <paramref name="day"/>,
        /// <paramref name="hour"/>, <paramref name="minute"/>, <paramref name="second"/>, and <paramref name="millisecond"/>
        /// specify a local time, Coordinated Universal Time (UTC), or neither.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is less than 1 or greater than 9999.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than 12.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="kind"/> is not one of the <see cref="DateTimeKind"/> values.
        /// </exception>
        /// <remarks>
        /// This constructor interprets <paramref name="year"/>, <paramref name="month"/> and <paramref name="day"/> as a year, month and day
        /// in the Gregorian calendar. To instantiate a <see cref="DateTime"/> value by using the year, month and day in another calendar, call
        /// the <see cref="DateTime(int, int, int, int, int, int, int, int, Calendar, DateTimeKind)"/> constructor.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind) =>
            _dateData = Init(year, month, day, hour, minute, second, millisecond, kind);

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through the number of years in <paramref name="calendar"/>).</param>
        /// <param name="month">The month (1 through the number of months in <paramref name="calendar"/>).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="calendar">The calendar that is used to interpret <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="calendar"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is not in the range supported by <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than the number of months in <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        /// </exception>
        /// <remarks>
        /// The allowable values for <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/> parameters
        /// depend on the <paramref name="calendar"/> parameter. An exception is thrown if the specified date and time cannot
        /// be expressed using <paramref name="calendar"/>.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar!!)
        {
            if (second != 60 || !s_systemSupportsLeapSeconds)
            {
                _dateData = calendar.ToDateTime(year, month, day, hour, minute, second, millisecond).UTicks;
            }
            else
            {
                // if we have a leap second, then we adjust it to 59 so that DateTime will consider it the last in the specified minute.
                this = new DateTime(year, month, day, hour, minute, 59, millisecond, calendar);
                ValidateLeapSecond();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="microsecond">The milliseconds (0 through 900).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is less than 1 or greater than 9999.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than 12.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        ///
        /// -or-
        ///
        /// <paramref name="microsecond"/> is less than 0 or greater than 900.
        /// </exception>
        /// <remarks>
        /// This constructor interprets <paramref name="year"/>, <paramref name="month"/> and <paramref name="day"/> as a year, month and day
        /// in the Gregorian calendar. To instantiate a <see cref="DateTime"/> value by using the year, month and day in another calendar, call
        /// the <see cref="DateTime(int, int, int, int, int, int, int, int, Calendar)"/> constructor.
        ///
        /// The <see cref="Kind"/> property is initialized to <see cref="DateTimeKind.Unspecified"/>.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond)
            : this(year, month, day, hour, minute, second, millisecond, microsecond, DateTimeKind.Unspecified)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="microsecond">The milliseconds (0 through 900).</param>
        /// <param name="kind">
        /// One of the enumeration values that indicates whether <paramref name="year"/>, <paramref name="month"/>, <paramref name="day"/>,
        /// <paramref name="hour"/>, <paramref name="minute"/>, <paramref name="second"/>, and <paramref name="millisecond"/>
        /// specify a local time, Coordinated Universal Time (UTC), or neither.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is less than 1 or greater than 9999.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than 12.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        ///
        /// -or-
        ///
        /// <paramref name="microsecond"/> is less than 0 or greater than 900.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="kind"/> is not one of the <see cref="DateTimeKind"/> values.
        /// </exception>
        /// <remarks>
        /// This constructor interprets <paramref name="year"/>, <paramref name="month"/> and <paramref name="day"/> as a year, month and day
        /// in the Gregorian calendar. To instantiate a <see cref="DateTime"/> value by using the year, month and day in another calendar, call
        /// the <see cref="DateTime(int, int, int, int, int, int, int, int, Calendar, DateTimeKind)"/> constructor.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, DateTimeKind kind)
        {
            ulong ticks = Init(year, month, day, hour, minute, second, millisecond, kind);
            if ((uint)microsecond >= MicrosecondsPerMillisecond) ThrowMicrosecondOutOfRange();

            ulong newTicks = (ticks & TicksMask) + (ulong)(microsecond * TicksPerMicrosecond);

            Debug.Assert(newTicks <= MaxTicks);
            _dateData = newTicks | (ticks & FlagsMask);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through the number of years in <paramref name="calendar"/>).</param>
        /// <param name="month">The month (1 through the number of months in <paramref name="calendar"/>).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="microsecond">The milliseconds (0 through 900).</param>
        /// <param name="calendar">The calendar that is used to interpret <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="calendar"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is not in the range supported by <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than the number of months in <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        ///
        /// -or-
        ///
        /// <paramref name="microsecond"/> is less than 0 or greater than 900.
        /// </exception>
        /// <remarks>
        /// The allowable values for <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/> parameters
        /// depend on the <paramref name="calendar"/> parameter. An exception is thrown if the specified date and time cannot
        /// be expressed using <paramref name="calendar"/>.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, Calendar calendar!!)
            : this(year, month, day, hour, minute, second, millisecond, microsecond, calendar, DateTimeKind.Unspecified)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTime"/> structure to the specified year, month, day, hour, minute, second,
        /// millisecond, and Coordinated Universal Time (UTC) or local time for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through the number of years in <paramref name="calendar"/>).</param>
        /// <param name="month">The month (1 through the number of months in <paramref name="calendar"/>).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="microsecond">The milliseconds (0 through 900).</param>
        /// <param name="calendar">The calendar that is used to interpret <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/>.</param>
        /// <param name="kind">
        /// One of the enumeration values that indicates whether <paramref name="year"/>, <paramref name="month"/>, <paramref name="day"/>,
        /// <paramref name="hour"/>, <paramref name="minute"/>, <paramref name="second"/>, and <paramref name="millisecond"/>
        /// specify a local time, Coordinated Universal Time (UTC), or neither.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="calendar"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is not in the range supported by <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="month"/> is less than 1 or greater than the number of months in <paramref name="calendar"/>.
        ///
        /// -or-
        ///
        /// <paramref name="day"/> is less than 1 or greater than the number of days in <paramref name="month"/>.
        ///
        /// -or-
        ///
        /// <paramref name="hour"/> is less than 0 or greater than 23.
        ///
        /// -or-
        ///
        /// <paramref name="minute"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="second"/> is less than 0 or greater than 59.
        ///
        /// -or-
        ///
        /// <paramref name="millisecond"/> is less than 0 or greater than 999.
        ///
        /// -or-
        ///
        /// <paramref name="microsecond"/> is less than 0 or greater than 900.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="kind"/> is not one of the <see cref="DateTimeKind"/> values.
        /// </exception>
        /// <remarks>
        /// The allowable values for <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/> parameters
        /// depend on the <paramref name="calendar"/> parameter. An exception is thrown if the specified date and time cannot
        /// be expressed using <paramref name="calendar"/>.
        ///
        /// For applications in which portability of date and time data or a limited degree of time zone awareness is important,
        /// you can use the corresponding <see cref="DateTimeOffset"/> constructor.
        /// </remarks>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, Calendar calendar!!, DateTimeKind kind)
            : this(year, month, day, hour, minute, second, millisecond, calendar, kind)
        {
            if ((uint)microsecond >= MicrosecondsPerMillisecond)
            {
                ThrowMicrosecondOutOfRange();
            }
            _dateData = new DateTime(_dateData).AddMicroseconds(microsecond)._dateData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Init(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind = DateTimeKind.Unspecified)
        {
            if ((uint)millisecond >= MillisPerSecond) ThrowMillisecondOutOfRange();
            if ((uint)kind > (uint)DateTimeKind.Local) ThrowInvalidKind();

            if (second != 60 || !s_systemSupportsLeapSeconds)
            {
                ulong ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
                ticks += (uint)millisecond * (uint)TicksPerMillisecond;
                Debug.Assert(ticks <= MaxTicks, "Input parameters validated already");
                return ticks | ((ulong)kind << KindShift);
            }

            // if we have a leap second, then we adjust it to 59 so that DateTime will consider it the last in the specified minute.
            DateTime dt = new(year, month, day, hour, minute, 59, millisecond, kind);

            if (!IsValidTimeWithLeapSeconds(year, month, day, hour, 59, kind))
            {
                ThrowHelper.ThrowArgumentOutOfRange_BadHourMinuteSecond();
            }

            return dt._dateData;
        }

        private void ValidateLeapSecond()
        {
            if (!IsValidTimeWithLeapSeconds(Year, Month, Day, Hour, Minute, Kind))
            {
                ThrowHelper.ThrowArgumentOutOfRange_BadHourMinuteSecond();
            }
        }

        private DateTime(SerializationInfo info, StreamingContext context)
        {
            if (info == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);

            bool foundTicks = false;

            // Get the data
            SerializationInfoEnumerator enumerator = info.GetEnumerator();
            while (enumerator.MoveNext())
            {
                switch (enumerator.Name)
                {
                    case TicksField:
                        _dateData = (ulong)Convert.ToInt64(enumerator.Value, CultureInfo.InvariantCulture);
                        foundTicks = true;
                        continue;
                    case DateDataField:
                        _dateData = Convert.ToUInt64(enumerator.Value, CultureInfo.InvariantCulture);
                        goto foundData;
                }
            }
            if (!foundTicks)
            {
                throw new SerializationException(SR.Serialization_MissingDateTimeData);
            }
        foundData:
            if (UTicks > MaxTicks)
            {
                throw new SerializationException(SR.Serialization_DateTimeTicksOutOfRange);
            }
        }

        private ulong UTicks => _dateData & TicksMask;

        private ulong InternalKind => _dateData & FlagsMask;

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
            double millis_double = value * scale + (value >= 0 ? 0.5 : -0.5);
            if (millis_double <= -MaxMillis || millis_double >= MaxMillis) ThrowOutOfRange();
            return AddTicks((long)millis_double * TicksPerMillisecond);

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_AddValue);
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

        /// <summary>
        /// Returns a new <see cref="DateTime"/> that adds the specified number of microseconds to the value of this instance.
        /// </summary>
        /// <param name="value">
        /// A number of whole and fractional microseconds.
        /// The <paramref name="value"/> parameter can be negative or positive.
        /// Note that this value is rounded to the nearest integer.
        /// </param>
        /// <returns>
        /// An object whose value is the sum of the date and time represented
        /// by this instance and the number of microseconds represented by <paramref name="value"/>.
        /// </returns>
        /// <remarks>
        /// This method does not change the value of this <see cref="DateTime"/>. Instead, it returns a new <see cref="DateTime"/>
        /// whose value is the result of this operation.
        ///
        /// The fractional part of value is the fractional part of a microsecond.
        /// For example, 4.5 is equivalent to 4 microseconds and 50 ticks, where one microseconds = 10 ticks.
        ///
        /// The value parameter is rounded to the nearest integer.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The resulting <see cref="DateTime"/> is less than <see cref="MinValue"/> or greater than <see cref="MaxValue"/>.
        /// </exception>
        public DateTime AddMicroseconds(double value)
        {
            if (value < -MaxMicroseconds || value > MaxMicroseconds)
            {
                ThrowOutOfRange();
            }

            return AddTicks((long)(value * TicksPerMicrosecond));

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_AddValue);
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
            GetDate(out int year, out int month, out int day);
            int y = year, d = day;
            int m = month + months;
            int q = m > 0 ? (int)((uint)(m - 1) / 12) : m / 12 - 1;
            y += q;
            m -= q * 12;
            if (y < 1 || y > 9999) ThrowDateArithmetic(2);
            uint[] daysTo = IsLeapYear(y) ? s_daysToMonth366 : s_daysToMonth365;
            uint daysToMonth = daysTo[m - 1];
            int days = (int)(daysTo[m] - daysToMonth);
            if (d > days) d = days;
            uint n = DaysToYear((uint)y) + daysToMonth + (uint)d - 1;
            return new DateTime(n * (ulong)TicksPerDay + UTicks % TicksPerDay | InternalKind);
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
            ulong ticks = (ulong)(Ticks + value);
            if (ticks > MaxTicks) ThrowDateArithmetic(0);
            return new DateTime(ticks | InternalKind);
        }

        // TryAddTicks is exact as AddTicks except it doesn't throw
        internal bool TryAddTicks(long value, out DateTime result)
        {
            ulong ticks = (ulong)(Ticks + value);
            if (ticks > MaxTicks)
            {
                result = default;
                return false;
            }
            result = new DateTime(ticks | InternalKind);
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
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_DateTimeBadYears);
            }
            GetDate(out int year, out int month, out int day);
            int y = year + value;
            if (y < 1 || y > 9999) ThrowDateArithmetic(0);
            uint n = DaysToYear((uint)y);

            int m = month - 1, d = day - 1;
            if (IsLeapYear(y))
            {
                n += s_daysToMonth366[m];
            }
            else
            {
                if (d == 28 && m == 1) d--;
                n += s_daysToMonth365[m];
            }
            n += (uint)d;
            return new DateTime(n * (ulong)TicksPerDay + UTicks % TicksPerDay | InternalKind);
        }

        // Compares two DateTime values, returning an integer that indicates
        // their relationship.
        //
        public static int Compare(DateTime t1, DateTime t2)
        {
            long ticks1 = t1.Ticks;
            long ticks2 = t2.Ticks;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong DateToTicks(int year, int month, int day)
        {
            if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            {
                ThrowHelper.ThrowArgumentOutOfRange_BadYearMonthDay();
            }

            uint[] days = IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;
            if ((uint)day > days[month] - days[month - 1])
            {
                ThrowHelper.ThrowArgumentOutOfRange_BadYearMonthDay();
            }

            uint n = DaysToYear((uint)year) + days[month - 1] + (uint)day - 1;
            return n * (ulong)TicksPerDay;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint DaysToYear(uint year)
        {
            uint y = year - 1;
            uint cent = y / 100;
            return y * (365 * 4 + 1) / 4 - cent + cent / 4;
        }

        // Return the tick count corresponding to the given hour, minute, second.
        // Will check the if the parameters are valid.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong TimeToTicks(int hour, int minute, int second)
        {
            if ((uint)hour >= 24 || (uint)minute >= 60 || (uint)second >= 60)
            {
                ThrowHelper.ThrowArgumentOutOfRange_BadHourMinuteSecond();
            }

            int totalSeconds = hour * 3600 + minute * 60 + second;
            return (uint)totalSeconds * (ulong)TicksPerSecond;
        }

        internal static ulong TimeToTicks(int hour, int minute, int second, int millisecond)
        {
            ulong ticks = TimeToTicks(hour, minute, second);

            if ((uint)millisecond >= MillisPerSecond) ThrowMillisecondOutOfRange();

            ticks += (uint)millisecond * (uint)TicksPerMillisecond;

            Debug.Assert(ticks <= MaxTicks, "Input parameters validated already");

            return ticks;
        }

        internal static ulong TimeToTicks(int hour, int minute, int second, int millisecond, int microsecond)
        {
            ulong ticks = TimeToTicks(hour, minute, second, millisecond);

            if ((uint)microsecond >= MicrosecondsPerMillisecond) ThrowMicrosecondOutOfRange();

            ticks += (uint)microsecond * (uint)TicksPerMicrosecond;

            Debug.Assert(ticks <= MaxTicks, "Input parameters validated already");

            return ticks;
        }

        // Returns the number of days in the month given by the year and
        // month arguments.
        //
        public static int DaysInMonth(int year, int month)
        {
            if (month < 1 || month > 12) ThrowHelper.ThrowArgumentOutOfRange_Month(month);
            // IsLeapYear checks the year argument
            return (IsLeapYear(year) ? DaysInMonth366 : DaysInMonth365)[month - 1];
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
        public override bool Equals([NotNullWhen(true)] object? value)
        {
            if (value is DateTime)
            {
                return Ticks == ((DateTime)value).Ticks;
            }
            return false;
        }

        public bool Equals(DateTime value)
        {
            return this == value;
        }

        // Compares two DateTime values for equality. Returns true if
        // the two DateTime values are equal, or false if they are
        // not equal.
        //
        public static bool Equals(DateTime t1, DateTime t2)
        {
            return t1 == t2;
        }

        public static DateTime FromBinary(long dateData)
        {
            if (((ulong)dateData & KindLocal) != 0)
            {
                // Local times need to be adjusted as you move from one time zone to another,
                // just as they are when serializing in text. As such the format for local times
                // changes to store the ticks of the UTC time, but with flags that look like a
                // local date.
                long ticks = dateData & (unchecked((long)TicksMask));
                // Negative ticks are stored in the top part of the range and should be converted back into a negative number
                if (ticks > TicksCeiling - TicksPerDay)
                {
                    ticks -= TicksCeiling;
                }
                // Convert the ticks back to local. If the UTC ticks are out of range, we need to default to
                // the UTC offset from MinValue and MaxValue to be consistent with Parse.
                bool isAmbiguousLocalDst = false;
                long offsetTicks;
                if (ticks < MinTicks)
                {
                    offsetTicks = TimeZoneInfo.GetLocalUtcOffset(MinValue, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
                }
                else if (ticks > MaxTicks)
                {
                    offsetTicks = TimeZoneInfo.GetLocalUtcOffset(MaxValue, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
                }
                else
                {
                    // Because the ticks conversion between UTC and local is lossy, we need to capture whether the
                    // time is in a repeated hour so that it can be passed to the DateTime constructor.
                    DateTime utcDt = new DateTime(ticks, DateTimeKind.Utc);
                    offsetTicks = TimeZoneInfo.GetUtcOffsetFromUtc(utcDt, TimeZoneInfo.Local, out _, out isAmbiguousLocalDst).Ticks;
                }
                ticks += offsetTicks;
                // Another behaviour of parsing is to cause small times to wrap around, so that they can be used
                // to compare times of day
                if (ticks < 0)
                {
                    ticks += TicksPerDay;
                }
                if ((ulong)ticks > MaxTicks)
                {
                    throw new ArgumentException(SR.Argument_DateTimeBadBinaryData, nameof(dateData));
                }
                return new DateTime(ticks, DateTimeKind.Local, isAmbiguousLocalDst);
            }
            else
            {
                if (((ulong)dateData & TicksMask) > MaxTicks)
                    throw new ArgumentException(SR.Argument_DateTimeBadBinaryData, nameof(dateData));
                return new DateTime((ulong)dateData);
            }
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
            if ((ulong)fileTime > MaxTicks - FileTimeOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(fileTime), SR.ArgumentOutOfRange_FileTimeInvalid);
            }

#pragma warning disable 162 // Unrechable code on Unix
            if (s_systemSupportsLeapSeconds)
            {
                return FromFileTimeLeapSecondsAware((ulong)fileTime);
            }
#pragma warning restore 162

            // This is the ticks in Universal time for this fileTime.
            ulong universalTicks = (ulong)fileTime + FileTimeOffset;
            return new DateTime(universalTicks | KindUtc);
        }

        // Creates a DateTime from an OLE Automation Date.
        //
        public static DateTime FromOADate(double d)
        {
            return new DateTime(DoubleDateToTicks(d), DateTimeKind.Unspecified);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);

            // Serialize both the old and the new format
            info.AddValue(TicksField, Ticks);
            info.AddValue(DateDataField, _dateData);
        }

        public bool IsDaylightSavingTime()
        {
            if (InternalKind == KindUtc)
            {
                return false;
            }
            return TimeZoneInfo.Local.IsDaylightSavingTime(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
        }

        public static DateTime SpecifyKind(DateTime value, DateTimeKind kind)
        {
            if ((uint)kind > (uint)DateTimeKind.Local) ThrowInvalidKind();
            return new DateTime(value.UTicks | ((ulong)kind << KindShift));
        }

        public long ToBinary()
        {
            if ((_dateData & KindLocal) != 0)
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
                return storedTicks | (unchecked((long)KindLocal));
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
                ulong ticks = UTicks;
                return new DateTime((ticks - ticks % TicksPerDay) | InternalKind);
            }
        }

        // Returns a given date part of this DateTime. This method is used
        // to compute the year, day-of-year, month, or day part.
        private int GetDatePart(int part)
        {
            // n = number of days since 1/1/0001
            uint n = (uint)(UTicks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            uint y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            uint y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            uint y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            uint y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;
            // If year was requested, compute and return it
            if (part == DatePartYear)
            {
                return (int)(y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1);
            }
            // n = day number within year
            n -= y1 * DaysPerYear;
            // If day-of-year was requested, return it
            if (part == DatePartDayOfYear) return (int)n + 1;
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            uint[] days = y1 == 3 && (y4 != 24 || y100 == 3) ? s_daysToMonth366 : s_daysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            uint m = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // If month was requested, return it
            if (part == DatePartMonth) return (int)m;
            // Return 1-based day-of-month
            return (int)(n - days[m - 1] + 1);
        }

        // Exactly the same as GetDatePart, except computing all of
        // year/month/day rather than just one of them. Used when all three
        // are needed rather than redoing the computations for each.
        internal void GetDate(out int year, out int month, out int day)
        {
            // n = number of days since 1/1/0001
            uint n = (uint)(UTicks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            uint y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            uint y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            uint y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            uint y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;
            // compute year
            year = (int)(y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1);
            // n = day number within year
            n -= y1 * DaysPerYear;
            // dayOfYear = n + 1;
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            uint[] days = y1 == 3 && (y4 != 24 || y100 == 3) ? s_daysToMonth366 : s_daysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            uint m = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // compute month and day
            month = (int)m;
            day = (int)(n - days[m - 1] + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void GetTime(out int hour, out int minute, out int second)
        {
            ulong seconds = UTicks / TicksPerSecond;
            ulong minutes = seconds / 60;
            second = (int)(seconds - (minutes * 60));
            ulong hours = minutes / 60;
            minute = (int)(minutes - (hours * 60));
            hour = (int)((uint)hours % 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void GetTime(out int hour, out int minute, out int second, out int millisecond)
        {
            ulong milliseconds = UTicks / TicksPerMillisecond;
            ulong seconds = milliseconds / 1000;
            millisecond = (int)(milliseconds - (seconds * 1000));
            ulong minutes = seconds / 60;
            second = (int)(seconds - (minutes * 60));
            ulong hours = minutes / 60;
            minute = (int)(minutes - (hours * 60));
            hour = (int)((uint)hours % 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void GetTimePrecise(out int hour, out int minute, out int second, out int tick)
        {
            ulong ticks = UTicks;
            ulong seconds = ticks / TicksPerSecond;
            tick = (int)(ticks - (seconds * TicksPerSecond));
            ulong minutes = seconds / 60;
            second = (int)(seconds - (minutes * 60));
            ulong hours = minutes / 60;
            minute = (int)(minutes - (hours * 60));
            hour = (int)((uint)hours % 24);
        }

        // Returns the day-of-month part of this DateTime. The returned
        // value is an integer between 1 and 31.
        //
        public int Day => GetDatePart(DatePartDay);

        // Returns the day-of-week part of this DateTime. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //
        public DayOfWeek DayOfWeek => (DayOfWeek)(((uint)(UTicks / TicksPerDay) + 1) % 7);

        // Returns the day-of-year part of this DateTime. The returned value
        // is an integer between 1 and 366.
        //
        public int DayOfYear => GetDatePart(DatePartDayOfYear);

        // Returns the hash code for this DateTime.
        //
        public override int GetHashCode()
        {
            long ticks = Ticks;
            return unchecked((int)ticks) ^ (int)(ticks >> 32);
        }

        // Returns the hour part of this DateTime. The returned value is an
        // integer between 0 and 23.
        //
        public int Hour => (int)((uint)(UTicks / TicksPerHour) % 24);

        internal bool IsAmbiguousDaylightSavingTime() =>
            InternalKind == KindLocalAmbiguousDst;

        public DateTimeKind Kind
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalKind switch
            {
                KindUnspecified => DateTimeKind.Unspecified,
                KindUtc => DateTimeKind.Utc,
                _ => DateTimeKind.Local,
            };
        }

        // Returns the millisecond part of this DateTime. The returned value
        // is an integer between 0 and 999.
        //
        public int Millisecond => (int)((UTicks / TicksPerMillisecond) % 1000);

        /// <summary>
        /// The microseconds component, expressed as a value between 0 and 999.
        /// </summary>
        public int Microsecond => (int)((UTicks / TicksPerMicrosecond) % 1000);

        /// <summary>
        /// The nanoseconds component, expressed as a value between 0 and 900.
        /// </summary>
        public int Nanosecond => (int)(UTicks % TicksPerMicrosecond) * 100;

        // Returns the minute part of this DateTime. The returned value is
        // an integer between 0 and 59.
        //
        public int Minute => (int)((UTicks / TicksPerMinute) % 60);

        // Returns the month part of this DateTime. The returned value is an
        // integer between 1 and 12.
        //
        public int Month => GetDatePart(DatePartMonth);

        // Returns a DateTime representing the current date and time. The
        // resolution of the returned value depends on the system timer.
        public static DateTime Now
        {
            get
            {
                DateTime utc = UtcNow;
                long offset = TimeZoneInfo.GetDateTimeNowUtcOffsetFromUtc(utc, out bool isAmbiguousLocalDst).Ticks;
                long tick = utc.Ticks + offset;
                if ((ulong)tick <= MaxTicks)
                {
                    if (!isAmbiguousLocalDst)
                    {
                        return new DateTime((ulong)tick | KindLocal);
                    }
                    return new DateTime((ulong)tick | KindLocalAmbiguousDst);
                }
                return new DateTime(tick < 0 ? KindLocal : MaxTicks | KindLocal);
            }
        }

        // Returns the second part of this DateTime. The returned value is
        // an integer between 0 and 59.
        //
        public int Second => (int)((UTicks / TicksPerSecond) % 60);

        // Returns the tick count for this DateTime. The returned value is
        // the number of 100-nanosecond intervals that have elapsed since 1/1/0001
        // 12:00am.
        //
        public long Ticks => (long)(_dateData & TicksMask);

        // Returns the time-of-day part of this DateTime. The returned value
        // is a TimeSpan that indicates the time elapsed since midnight.
        //
        public TimeSpan TimeOfDay => new TimeSpan((long)(UTicks % TicksPerDay));

        // Returns a DateTime representing the current date. The date part
        // of the returned value is the current date, and the time-of-day part of
        // the returned value is zero (midnight).
        //
        public static DateTime Today => Now.Date;

        // Returns the year part of this DateTime. The returned value is an
        // integer between 1 and 9999.
        //
        public int Year => GetDatePart(DatePartYear);

        // Checks whether a given year is a leap year. This method returns true if
        // year is a leap year, or false if not.
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeapYear(int year)
        {
            if (year < 1 || year > 9999)
            {
                ThrowHelper.ThrowArgumentOutOfRange_Year();
            }
            if ((year & 3) != 0) return false;
            if ((year & 15) == 0) return true;
            // return true/false not the test result https://github.com/dotnet/runtime/issues/4207
            return (uint)year % 25 != 0 ? true : false;
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return DateTimeParse.Parse(s, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None);
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), DateTimeStyles.None);
        }

        public static DateTime Parse(string s, IFormatProvider? provider, DateTimeStyles styles)
        {
            DateTimeFormatInfo.ValidateStyles(styles, styles: true);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), styles);
        }

        public static DateTime Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null, DateTimeStyles styles = DateTimeStyles.None)
        {
            DateTimeFormatInfo.ValidateStyles(styles, styles: true);
            return DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), styles);
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime ParseExact(string s, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            if (format == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
            return DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), DateTimeStyles.None);
        }

        // Constructs a DateTime from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTime ParseExact(string s, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, IFormatProvider? provider, DateTimeStyles style)
        {
            DateTimeFormatInfo.ValidateStyles(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            if (format == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
            return DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style);
        }

        public static DateTime ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            DateTimeFormatInfo.ValidateStyles(style);
            return DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style);
        }

        public static DateTime ParseExact(string s, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style)
        {
            DateTimeFormatInfo.ValidateStyles(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return DateTimeParse.ParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style);
        }

        public static DateTime ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            DateTimeFormatInfo.ValidateStyles(style);
            return DateTimeParse.ParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style);
        }

        public TimeSpan Subtract(DateTime value)
        {
            return new TimeSpan(Ticks - value.Ticks);
        }

        public DateTime Subtract(TimeSpan value)
        {
            ulong ticks = (ulong)(Ticks - value._ticks);
            if (ticks > MaxTicks) ThrowDateArithmetic(0);
            return new DateTime(ticks | InternalKind);
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
            return TicksToOADate(Ticks);
        }

        public long ToFileTime()
        {
            // Treats the input as local if it is not specified
            return ToUniversalTime().ToFileTimeUtc();
        }

        public long ToFileTimeUtc()
        {
            // Treats the input as universal if it is not specified
            long ticks = ((_dateData & KindLocal) != 0) ? ToUniversalTime().Ticks : Ticks;

#pragma warning disable 162 // Unrechable code on Unix
            if (s_systemSupportsLeapSeconds)
            {
                return (long)ToFileTimeLeapSecondsAware(ticks);
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
            if ((_dateData & KindLocal) != 0)
            {
                return this;
            }
            long offset = TimeZoneInfo.GetUtcOffsetFromUtc(this, TimeZoneInfo.Local, out _, out bool isAmbiguousLocalDst).Ticks;
            long tick = Ticks + offset;
            if ((ulong)tick <= MaxTicks)
            {
                if (!isAmbiguousLocalDst)
                {
                    return new DateTime((ulong)tick | KindLocal);
                }
                return new DateTime((ulong)tick | KindLocalAmbiguousDst);
            }
            return new DateTime(tick < 0 ? KindLocal : MaxTicks | KindLocal);
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

        public string ToString([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string? format)
        {
            return DateTimeFormat.Format(this, format, null);
        }

        public string ToString(IFormatProvider? provider)
        {
            return DateTimeFormat.Format(this, null, provider);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string? format, IFormatProvider? provider)
        {
            return DateTimeFormat.Format(this, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            DateTimeFormat.TryFormat(this, destination, out charsWritten, format, provider);

        public DateTime ToUniversalTime()
        {
            return TimeZoneInfo.ConvertTimeToUtc(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out DateTime result)
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

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles styles, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(styles, styles: true);

            if (s == null)
            {
                result = default;
                return false;
            }

            return DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), styles, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles styles, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(styles, styles: true);
            return DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), styles, out result);
        }

        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string? format, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style);

            if (s == null || format == null)
            {
                result = default;
                return false;
            }

            return DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style);
            return DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style);

            if (s == null)
            {
                result = default;
                return false;
            }

            return DateTimeParse.TryParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
        {
            DateTimeFormatInfo.ValidateStyles(style);
            return DateTimeParse.TryParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style, out result);
        }

        public static DateTime operator +(DateTime d, TimeSpan t)
        {
            ulong ticks = (ulong)(d.Ticks + t._ticks);
            if (ticks > MaxTicks) ThrowDateArithmetic(1);
            return new DateTime(ticks | d.InternalKind);
        }

        public static DateTime operator -(DateTime d, TimeSpan t)
        {
            ulong ticks = (ulong)(d.Ticks - t._ticks);
            if (ticks > MaxTicks) ThrowDateArithmetic(1);
            return new DateTime(ticks | d.InternalKind);
        }

        public static TimeSpan operator -(DateTime d1, DateTime d2) => new TimeSpan(d1.Ticks - d2.Ticks);

        public static bool operator ==(DateTime d1, DateTime d2) => ((d1._dateData ^ d2._dateData) << 2) == 0;

        public static bool operator !=(DateTime d1, DateTime d2) => !(d1 == d2);

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(DateTime t1, DateTime t2) => t1.Ticks < t2.Ticks;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(DateTime t1, DateTime t2) => t1.Ticks <= t2.Ticks;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(DateTime t1, DateTime t2) => t1.Ticks > t2.Ticks;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(DateTime t1, DateTime t2) => t1.Ticks >= t2.Ticks;

        // Returns a string array containing all of the known date and time options for the
        // current culture.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats()
        {
            return GetDateTimeFormats(CultureInfo.CurrentCulture);
        }

        // Returns a string array containing all of the known date and time options for the
        // using the information provided by IFormatProvider.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats(IFormatProvider? provider)
        {
            return DateTimeFormat.GetAllDateTimes(this, DateTimeFormatInfo.GetInstance(provider));
        }

        // Returns a string array containing all of the date and time options for the
        // given format format and current culture.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats(char format)
        {
            return GetDateTimeFormats(format, CultureInfo.CurrentCulture);
        }

        // Returns a string array containing all of the date and time options for the
        // given format format and given culture.  The strings returned are properly formatted date and
        // time strings for the current instance of DateTime.
        public string[] GetDateTimeFormats(char format, IFormatProvider? provider)
        {
            return DateTimeFormat.GetAllDateTimes(this, format, DateTimeFormatInfo.GetInstance(provider));
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode() => TypeCode.DateTime;

        bool IConvertible.ToBoolean(IFormatProvider? provider) => throw InvalidCast(nameof(Boolean));
        char IConvertible.ToChar(IFormatProvider? provider) => throw InvalidCast(nameof(Char));
        sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw InvalidCast(nameof(SByte));
        byte IConvertible.ToByte(IFormatProvider? provider) => throw InvalidCast(nameof(Byte));
        short IConvertible.ToInt16(IFormatProvider? provider) => throw InvalidCast(nameof(Int16));
        ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw InvalidCast(nameof(UInt16));
        int IConvertible.ToInt32(IFormatProvider? provider) => throw InvalidCast(nameof(Int32));
        uint IConvertible.ToUInt32(IFormatProvider? provider) => throw InvalidCast(nameof(UInt32));
        long IConvertible.ToInt64(IFormatProvider? provider) => throw InvalidCast(nameof(Int64));
        ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw InvalidCast(nameof(UInt64));
        float IConvertible.ToSingle(IFormatProvider? provider) => throw InvalidCast(nameof(Single));
        double IConvertible.ToDouble(IFormatProvider? provider) => throw InvalidCast(nameof(Double));
        decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw InvalidCast(nameof(Decimal));

        private static Exception InvalidCast(string to) => new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, nameof(DateTime), to));

        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => this;

        object IConvertible.ToType(Type type, IFormatProvider? provider) => Convert.DefaultToType(this, type, provider);

        // Tries to construct a DateTime from a given year, month, day, hour,
        // minute, second and millisecond.
        //
        internal static bool TryCreate(int year, int month, int day, int hour, int minute, int second, int millisecond, out DateTime result)
        {
            result = default;
            if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
            {
                return false;
            }
            if ((uint)hour >= 24 || (uint)minute >= 60 || (uint)millisecond >= MillisPerSecond)
            {
                return false;
            }

            uint[] days = IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;
            if ((uint)day > days[month] - days[month - 1])
            {
                return false;
            }
            ulong ticks = (DaysToYear((uint)year) + days[month - 1] + (uint)day - 1) * (ulong)TicksPerDay;

            if ((uint)second < 60)
            {
                ticks += TimeToTicks(hour, minute, second) + (uint)millisecond * (uint)TicksPerMillisecond;
            }
            else if (second == 60 && s_systemSupportsLeapSeconds && IsValidTimeWithLeapSeconds(year, month, day, hour, minute, DateTimeKind.Unspecified))
            {
                // if we have leap second (second = 60) then we'll need to check if it is valid time.
                // if it is valid, then we adjust the second to 59 so DateTime will consider this second is last second
                // of this minute.
                // if it is not valid time, we'll eventually throw.
                // although this is unspecified datetime kind, we'll assume the passed time is UTC to check the leap seconds.
                ticks += TimeToTicks(hour, minute, 59) + 999 * TicksPerMillisecond;
            }
            else
            {
                return false;
            }

            Debug.Assert(ticks <= MaxTicks, "Input parameters validated already");
            result = new DateTime(ticks);
            return true;
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static DateTime IAdditionOperators<DateTime, TimeSpan, DateTime>.operator checked +(DateTime left, TimeSpan right) => left + right;

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static TimeSpan IAdditiveIdentity<DateTime, TimeSpan>.AdditiveIdentity => default;

        //
        // IMinMaxValue
        //

        static DateTime IMinMaxValue<DateTime>.MinValue => MinValue;

        static DateTime IMinMaxValue<DateTime>.MaxValue => MaxValue;

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out DateTime result) => TryParse(s, provider, DateTimeStyles.None, out result);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static DateTime Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DateTime result) => TryParse(s, provider, DateTimeStyles.None, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static DateTime ISubtractionOperators<DateTime, TimeSpan, DateTime>.operator checked -(DateTime left, TimeSpan right) => left - right;

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static TimeSpan ISubtractionOperators<DateTime, DateTime, TimeSpan>.operator checked -(DateTime left, DateTime right) => left - right;
    }
}
