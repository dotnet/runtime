// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
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
    // DateTimeOffset is a value type that consists of a DateTime and a time zone offset,
    // ie. how far away the time is from GMT. The DateTime is stored whole, and the offset
    // is stored as an Int16 internally to save space, but presented as a TimeSpan.
    //
    // The range is constrained so that both the represented clock time and the represented
    // UTC time fit within the boundaries of MaxValue. This gives it the same range as DateTime
    // for actual UTC times, and a slightly constrained range on one end when an offset is
    // present.
    //
    // This class should be substitutable for date time in most cases; so most operations
    // effectively work on the clock time. However, the underlying UTC time is what counts
    // for the purposes of identity, sorting and subtracting two instances.
    //
    //
    // There are theoretically two date times stored, the UTC and the relative local representation
    // or the 'clock' time. It actually does not matter which is stored in m_dateTime, so it is desirable
    // for most methods to go through the helpers UtcDateTime and ClockDateTime both to abstract this
    // out and for internal readability.

    [StructLayout(LayoutKind.Auto)]
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly partial struct DateTimeOffset
        : IComparable,
          ISpanFormattable,
          IComparable<DateTimeOffset>,
          IEquatable<DateTimeOffset>,
          ISerializable,
          IDeserializationCallback,
          ISpanParsable<DateTimeOffset>,
          IUtf8SpanFormattable
    {
        // Constants
        private const int MaxOffsetMinutes = 14 * 60;
        private const int MinOffsetMinutes = -MaxOffsetMinutes;
        internal const long MaxOffset = MaxOffsetMinutes * TimeSpan.TicksPerMinute;
        internal const long MinOffset = -MaxOffset;

        private const long UnixEpochSeconds = DateTime.UnixEpochTicks / TimeSpan.TicksPerSecond; // 62,135,596,800
        private const long UnixEpochMilliseconds = DateTime.UnixEpochTicks / TimeSpan.TicksPerMillisecond; // 62,135,596,800,000

        internal const long UnixMinSeconds = DateTime.MinTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;
        internal const long UnixMaxSeconds = DateTime.MaxTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;

        // Static Fields
        public static readonly DateTimeOffset MinValue;
        public static readonly DateTimeOffset MaxValue = new DateTimeOffset(0, DateTime.CreateUnchecked(DateTime.MaxTicks));
        public static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(0, DateTime.CreateUnchecked(DateTime.UnixEpochTicks));

        // Instance Fields
        private readonly DateTime _dateTime;
        private readonly int _offsetMinutes;

        // Constructors

        private DateTimeOffset(int validOffsetMinutes, DateTime validDateTime)
        {
            Debug.Assert(validOffsetMinutes is >= MinOffsetMinutes and <= MaxOffsetMinutes);
            Debug.Assert(validDateTime.Kind == DateTimeKind.Unspecified);
            Debug.Assert((ulong)(validDateTime.Ticks + validOffsetMinutes * TimeSpan.TicksPerMinute) <= DateTime.MaxTicks);
            _dateTime = validDateTime;
            _offsetMinutes = validOffsetMinutes;
        }

        // Constructs a DateTimeOffset from a tick count and offset
        public DateTimeOffset(long ticks, TimeSpan offset) : this(ValidateOffset(offset), ValidateDate(new DateTime(ticks), offset))
        {
        }

        private static DateTimeOffset CreateValidateOffset(DateTime dateTime, TimeSpan offset) => new DateTimeOffset(ValidateOffset(offset), ValidateDate(dateTime, offset));

        // Constructs a DateTimeOffset from a DateTime. For Local and Unspecified kinds,
        // extracts the local offset. For UTC, creates a UTC instance with a zero offset.
        public DateTimeOffset(DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                // Local and Unspecified are both treated as Local
                TimeSpan offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                _offsetMinutes = ValidateOffset(offset);
                _dateTime = ValidateDate(dateTime, offset);
            }
            else
            {
                _offsetMinutes = 0;
                _dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            }
        }

        // Constructs a DateTimeOffset from a DateTime. And an offset. Always makes the clock time
        // consistent with the DateTime. For Utc ensures the offset is zero. For local, ensures that
        // the offset corresponds to the local.
        public DateTimeOffset(DateTime dateTime, TimeSpan offset)
        {
            if (dateTime.Kind == DateTimeKind.Local)
            {
                if (offset != TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime))
                {
                    throw new ArgumentException(SR.Argument_OffsetLocalMismatch, nameof(offset));
                }
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                if (offset.Ticks != 0)
                {
                    throw new ArgumentException(SR.Argument_OffsetUtcMismatch, nameof(offset));
                }
            }
            _offsetMinutes = ValidateOffset(offset);
            _dateTime = ValidateDate(dateTime, offset);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffset"/> structure by <paramref name="date"/>, <paramref name="time"/> and <paramref name="offset"/>.
        /// </summary>
        /// <param name="date">The date part</param>
        /// <param name="time">The time part</param>
        /// <param name="offset">The time's offset from Coordinated Universal Time (UTC).</param>
        public DateTimeOffset(DateOnly date, TimeOnly time, TimeSpan offset)
            : this(new DateTime(date, time), offset)
        {
        }

        // Constructs a DateTimeOffset from a given year, month, day, hour,
        // minute, second and offset.
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, TimeSpan offset)
        {
            _offsetMinutes = ValidateOffset(offset);

            if (second != 60 || !DateTime.SystemSupportsLeapSeconds)
            {
                _dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second), offset);
            }
            else
            {
                _dateTime = WithLeapSecond(year, month, day, hour, minute, offset);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DateTime WithLeapSecond(int year, int month, int day, int hour, int minute, TimeSpan offset)
        {
            // Reset the leap second to 59 for now and then we'll validate it after getting the final UTC time.
            DateTimeOffset value = new(year, month, day, hour, minute, 59, offset);
            DateTime.ValidateLeapSecond(value.UtcDateTime);
            return value._dateTime;
        }

        // Constructs a DateTimeOffset from a given year, month, day, hour,
        // minute, second, millisecond and offset
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan offset)
            : this(year, month, day, hour, minute, second, offset)
        {
            if ((uint)millisecond >= TimeSpan.MillisecondsPerSecond) DateTime.ThrowMillisecondOutOfRange();
            _dateTime = DateTime.CreateUnchecked(UtcTicks + (uint)millisecond * (uint)TimeSpan.TicksPerMillisecond);
        }

        // Constructs a DateTimeOffset from a given year, month, day, hour,
        // minute, second, millisecond, Calendar and offset.
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar, TimeSpan offset)
        {
            ArgumentNullException.ThrowIfNull(calendar);
            _offsetMinutes = ValidateOffset(offset);

            if (second != 60 || !DateTime.SystemSupportsLeapSeconds)
            {
                _dateTime = ValidateDate(calendar.ToDateTime(year, month, day, hour, minute, second, millisecond), offset);
            }
            else
            {
                _dateTime = WithLeapSecond(calendar, year, month, day, hour, minute, millisecond, offset);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DateTime WithLeapSecond(Calendar calendar, int year, int month, int day, int hour, int minute, int millisecond, TimeSpan offset)
        {
            // Reset the leap second to 59 for now and then we'll validate it after getting the final UTC time.
            DateTimeOffset value = new DateTimeOffset(year, month, day, hour, minute, 59, millisecond, calendar, offset);
            DateTime.ValidateLeapSecond(value.UtcDateTime);
            return value._dateTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffset"/> structure using the
        /// specified <paramref name="year"/>, <paramref name="month"/>, <paramref name="day"/>, <paramref name="hour"/>, <paramref name="minute"/>,
        /// <paramref name="second"/>, <paramref name="millisecond"/>, <paramref name="microsecond"/> and <paramref name="offset"/>.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="microsecond">The microseconds (0 through 999).</param>
        /// <param name="offset">The time's offset from Coordinated Universal Time (UTC).</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="offset"/> does not represent whole minutes.
        /// </exception>
        /// <remarks>
        /// This constructor interprets <paramref name="year"/>, <paramref name="month"/> and <paramref name="day"/> as a year, month and day
        /// in the Gregorian calendar. To instantiate a <see cref="DateTimeOffset"/> value by using the year, month and day in another calendar, call
        /// the <see cref="DateTimeOffset(int, int, int, int, int, int, int, int, Calendar, TimeSpan)"/> constructor.
        /// </remarks>
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
        /// -or-
        ///
        /// <paramref name="microsecond"/> is less than 0 or greater than 999.
        /// </exception>
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, TimeSpan offset)
            : this(year, month, day, hour, minute, second, millisecond, offset)
        {
            if ((uint)microsecond >= TimeSpan.MicrosecondsPerMillisecond) DateTime.ThrowMicrosecondOutOfRange();
            _dateTime = DateTime.CreateUnchecked(UtcTicks + (uint)microsecond * (uint)TimeSpan.TicksPerMicrosecond);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffset"/> structure using the
        /// specified <paramref name="year"/>, <paramref name="month"/>, <paramref name="day"/>, <paramref name="hour"/>, <paramref name="minute"/>,
        /// <paramref name="second"/>, <paramref name="millisecond"/>, <paramref name="microsecond"/> and <paramref name="offset"/>.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The milliseconds (0 through 999).</param>
        /// <param name="microsecond">The microseconds (0 through 999).</param>
        /// <param name="calendar">The calendar that is used to interpret <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/>.</param>
        /// <param name="offset">The time's offset from Coordinated Universal Time (UTC).</param>
        /// <remarks>
        /// This constructor interprets <paramref name="year"/>, <paramref name="month"/> and <paramref name="day"/> as a year, month and day
        /// in the Gregorian calendar. To instantiate a <see cref="DateTimeOffset"/> value by using the year, month and day in another calendar, call
        /// the <see cref="DateTimeOffset(int, int, int, int, int, int, int, int, Calendar, TimeSpan)"/> constructor.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="offset"/> does not represent whole minutes.
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
        /// <paramref name="microsecond"/> is less than 0 or greater than 999.
        ///
        /// -or-
        ///
        /// <paramref name="offset"/> is less than -14 hours or greater than 14 hours.
        ///
        /// -or-
        ///
        /// The <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/> parameters
        /// cannot be represented as a date and time value.
        ///
        /// -or-
        ///
        /// The <see cref="UtcDateTime"/> property is earlier than <see cref="MinValue"/> or later than <see cref="MaxValue"/>.
        /// </exception>
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, Calendar calendar, TimeSpan offset)
            : this(year, month, day, hour, minute, second, millisecond, calendar, offset)
        {
            if ((uint)microsecond >= TimeSpan.MicrosecondsPerMillisecond) DateTime.ThrowMicrosecondOutOfRange();
            _dateTime = DateTime.CreateUnchecked(UtcTicks + (uint)microsecond * (uint)TimeSpan.TicksPerMicrosecond);
        }

        public static DateTimeOffset UtcNow => new DateTimeOffset(0, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified));

        public DateTime DateTime => ClockDateTime;

        public DateTime UtcDateTime => DateTime.CreateUnchecked((long)(_dateTime._dateData | DateTime.KindUtc));

        public DateTime LocalDateTime => UtcDateTime.ToLocalTime();

        // Adjust to a given offset with the same UTC time.  Can throw ArgumentException
        //
        public DateTimeOffset ToOffset(TimeSpan offset) => CreateValidateOffset(_dateTime + offset, offset);

        // Instance Properties

        // The clock or visible time represented. This is just a wrapper around the internal date because this is
        // the chosen storage mechanism. Going through this helper is good for readability and maintainability.
        // This should be used for display but not identity.
        private DateTime ClockDateTime => DateTime.CreateUnchecked(UtcTicks + _offsetMinutes * TimeSpan.TicksPerMinute);

        // Returns the date part of this DateTimeOffset. The resulting value
        // corresponds to this DateTimeOffset with the time-of-day part set to
        // zero (midnight).
        //
        public DateTime Date => ClockDateTime.Date;

        // Returns the day-of-month part of this DateTimeOffset. The returned
        // value is an integer between 1 and 31.
        //
        public int Day => ClockDateTime.Day;

        // Returns the day-of-week part of this DateTimeOffset. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //
        public DayOfWeek DayOfWeek => ClockDateTime.DayOfWeek;

        // Returns the day-of-year part of this DateTimeOffset. The returned value
        // is an integer between 1 and 366.
        //
        public int DayOfYear => ClockDateTime.DayOfYear;

        // Returns the hour part of this DateTimeOffset. The returned value is an
        // integer between 0 and 23.
        //
        public int Hour => ClockDateTime.Hour;

        // Returns the millisecond part of this DateTimeOffset. The returned value
        // is an integer between 0 and 999.
        //
        public int Millisecond => UtcDateTime.Millisecond;

        /// <summary>
        /// Gets the microsecond component of the time represented by the current <see cref="DateTimeOffset"/> object.
        /// </summary>
        /// <remarks>
        /// If you rely on properties such as <see cref="Now"/> or <see cref="UtcNow"/> to accurately track the number of elapsed microseconds,
        /// the precision of the time's microseconds component depends on the resolution of the system clock.
        /// On Windows NT 3.5 and later, and Windows Vista operating systems, the clock's resolution is approximately 10000-15000 microseconds.
        /// </remarks>
        public int Microsecond => UtcDateTime.Microsecond;

        /// <summary>
        /// Gets the nanosecond component of the time represented by the current <see cref="DateTimeOffset"/> object.
        /// </summary>
        /// <remarks>
        /// If you rely on properties such as <see cref="Now"/> or <see cref="UtcNow"/> to accurately track the number of elapsed nanosecond,
        /// the precision of the time's nanosecond component depends on the resolution of the system clock.
        /// On Windows NT 3.5 and later, and Windows Vista operating systems, the clock's resolution is approximately 10000000-15000000 nanoseconds.
        /// </remarks>
        public int Nanosecond => UtcDateTime.Nanosecond;

        // Returns the minute part of this DateTimeOffset. The returned value is
        // an integer between 0 and 59.
        //
        public int Minute => ClockDateTime.Minute;

        // Returns the month part of this DateTimeOffset. The returned value is an
        // integer between 1 and 12.
        //
        public int Month => ClockDateTime.Month;

        public TimeSpan Offset => new TimeSpan(_offsetMinutes * TimeSpan.TicksPerMinute);

        /// <summary>
        /// Gets the total number of minutes representing the time's offset from Coordinated Universal Time (UTC).
        /// </summary>
        public int TotalOffsetMinutes => _offsetMinutes;

        // Returns the second part of this DateTimeOffset. The returned value is
        // an integer between 0 and 59.
        //
        public int Second => UtcDateTime.Second;

        // Returns the tick count for this DateTimeOffset. The returned value is
        // the number of 100-nanosecond intervals that have elapsed since 1/1/0001
        // 12:00am.
        //
        public long Ticks => ClockDateTime.Ticks;

        public long UtcTicks => (long)_dateTime._dateData;

        // Returns the time-of-day part of this DateTimeOffset. The returned value
        // is a TimeSpan that indicates the time elapsed since midnight.
        //
        public TimeSpan TimeOfDay => ClockDateTime.TimeOfDay;

        // Returns the year part of this DateTimeOffset. The returned value is an
        // integer between 1 and 9999.
        //
        public int Year => ClockDateTime.Year;

        // Returns the DateTimeOffset resulting from adding the given
        // TimeSpan to this DateTimeOffset.
        //
        public DateTimeOffset Add(TimeSpan timeSpan) => Add(ClockDateTime.Add(timeSpan));

        // Returns the DateTimeOffset resulting from adding a fractional number of
        // days to this DateTimeOffset. The result is computed by rounding the
        // fractional number of days given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddDays(double days) => Add(ClockDateTime.AddDays(days));

        // Returns the DateTimeOffset resulting from adding a fractional number of
        // hours to this DateTimeOffset. The result is computed by rounding the
        // fractional number of hours given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddHours(double hours) => Add(ClockDateTime.AddHours(hours));

        // Returns the DateTimeOffset resulting from the given number of
        // milliseconds to this DateTimeOffset. The result is computed by rounding
        // the number of milliseconds given by value to the nearest integer,
        // and adding that interval to this DateTimeOffset. The value
        // argument is permitted to be negative.
        //
        public DateTimeOffset AddMilliseconds(double milliseconds) => Add(ClockDateTime.AddMilliseconds(milliseconds));

        /// <summary>
        /// Returns a new <see cref="DateTimeOffset"/> object that adds a specified number of microseconds to the value of this instance.
        /// </summary>
        /// <param name="microseconds">A number of whole and fractional microseconds. The number can be negative or positive.</param>
        /// <returns>
        /// An object whose value is the sum of the date and time represented by the current <see cref="DateTimeOffset"/> object and the number
        /// of whole microseconds represented by <paramref name="microseconds"/>.
        /// </returns>
        /// <remarks>
        /// The fractional part of value is the fractional part of a microsecond.
        /// For example, 4.5 is equivalent to 4 microseconds and 50 ticks, where one microseconds = 10 ticks.
        /// However, <paramref name="microseconds"/> is rounded to the nearest microsecond; all values of .5 or greater are rounded up.
        ///
        /// Because a <see cref="DateTimeOffset"/> object does not represent the date and time in a specific time zone,
        /// the <see cref="AddMicroseconds"/> method does not consider a particular time zone's adjustment rules
        /// when it performs date and time arithmetic.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The resulting <see cref="DateTimeOffset"/> value is less than <see cref="MinValue"/>
        ///
        /// -or-
        ///
        /// The resulting <see cref="DateTimeOffset"/> value is greater than <see cref="MaxValue"/>
        /// </exception>
        public DateTimeOffset AddMicroseconds(double microseconds) => Add(ClockDateTime.AddMicroseconds(microseconds));

        // Returns the DateTimeOffset resulting from adding a fractional number of
        // minutes to this DateTimeOffset. The result is computed by rounding the
        // fractional number of minutes given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddMinutes(double minutes) => Add(ClockDateTime.AddMinutes(minutes));

        public DateTimeOffset AddMonths(int months) => Add(ClockDateTime.AddMonths(months));

        // Returns the DateTimeOffset resulting from adding a fractional number of
        // seconds to this DateTimeOffset. The result is computed by rounding the
        // fractional number of seconds given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddSeconds(double seconds) => Add(ClockDateTime.AddSeconds(seconds));

        // Returns the DateTimeOffset resulting from adding the given number of
        // 100-nanosecond ticks to this DateTimeOffset. The value argument
        // is permitted to be negative.
        //
        public DateTimeOffset AddTicks(long ticks) => Add(ClockDateTime.AddTicks(ticks));

        // Returns the DateTimeOffset resulting from adding the given number of
        // years to this DateTimeOffset. The result is computed by incrementing
        // (or decrementing) the year part of this DateTimeOffset by value
        // years. If the month and day of this DateTimeOffset is 2/29, and if the
        // resulting year is not a leap year, the month and day of the resulting
        // DateTimeOffset becomes 2/28. Otherwise, the month, day, and time-of-day
        // parts of the result are the same as those of this DateTimeOffset.
        //
        public DateTimeOffset AddYears(int years) => Add(ClockDateTime.AddYears(years));

        private DateTimeOffset Add(DateTime dateTime) => new DateTimeOffset(_offsetMinutes, ValidateDate(dateTime, Offset));

        // Compares two DateTimeOffset values, returning an integer that indicates
        // their relationship.
        //
        public static int Compare(DateTimeOffset first, DateTimeOffset second) =>
            first.UtcTicks.CompareTo(second.UtcTicks);

        // Compares this DateTimeOffset to a given object. This method provides an
        // implementation of the IComparable interface. The object
        // argument must be another DateTimeOffset, or otherwise an exception
        // occurs.  Null is considered less than any instance.
        //
        int IComparable.CompareTo(object? obj)
        {
            if (obj == null) return 1;
            if (obj is not DateTimeOffset other)
            {
                throw new ArgumentException(SR.Arg_MustBeDateTimeOffset);
            }

            return UtcTicks.CompareTo(other.UtcTicks);
        }

        public int CompareTo(DateTimeOffset other) =>
            UtcTicks.CompareTo(other.UtcTicks);

        // Checks if this DateTimeOffset is equal to a given object. Returns
        // true if the given object is a boxed DateTimeOffset and its value
        // is equal to the value of this DateTimeOffset. Returns false
        // otherwise.
        //
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is DateTimeOffset && UtcTicks == ((DateTimeOffset)obj).UtcTicks;

        public bool Equals(DateTimeOffset other) => UtcTicks == other.UtcTicks;

        // returns true when the ClockDateTime, Kind, and Offset match
        public bool EqualsExact(DateTimeOffset other) => UtcTicks == other.UtcTicks && _offsetMinutes == other._offsetMinutes;

        // Compares two DateTimeOffset values for equality. Returns true if
        // the two DateTimeOffset values are equal, or false if they are
        // not equal.
        //
        public static bool Equals(DateTimeOffset first, DateTimeOffset second) => first.UtcTicks == second.UtcTicks;

        // Creates a DateTimeOffset from a Windows filetime. A Windows filetime is
        // a long representing the date and time as the number of
        // 100-nanosecond intervals that have elapsed since 1/1/1601 12:00am.
        //
        public static DateTimeOffset FromFileTime(long fileTime) =>
            ToLocalTime(DateTime.FromFileTimeUtc(fileTime), true);

        public static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            if (seconds < UnixMinSeconds || seconds > UnixMaxSeconds)
            {
                ThrowHelper.ThrowArgumentOutOfRange_Range(nameof(seconds), seconds, UnixMinSeconds, UnixMaxSeconds);
            }

            long ticks = seconds * TimeSpan.TicksPerSecond + DateTime.UnixEpochTicks;
            return new DateTimeOffset(0, DateTime.CreateUnchecked(ticks));
        }

        public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds)
        {
            const long MinMilliseconds = DateTime.MinTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;
            const long MaxMilliseconds = DateTime.MaxTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;

            if (milliseconds < MinMilliseconds || milliseconds > MaxMilliseconds)
            {
                ThrowHelper.ThrowArgumentOutOfRange_Range(nameof(milliseconds), milliseconds, MinMilliseconds, MaxMilliseconds);
            }

            long ticks = milliseconds * TimeSpan.TicksPerMillisecond + DateTime.UnixEpochTicks;
            return new DateTimeOffset(0, DateTime.CreateUnchecked(ticks));
        }

        // ----- SECTION: private serialization instance methods  ----------------*

        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            try
            {
                ValidateOffset(Offset);
                ValidateDate(ClockDateTime, Offset);
            }
            catch (ArgumentException e)
            {
                throw new SerializationException(SR.Serialization_InvalidData, e);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            info.AddValue("DateTime", _dateTime); // Do not rename (binary serialization)
            info.AddValue("OffsetMinutes", (short)_offsetMinutes); // Do not rename (binary serialization)
        }

        private DateTimeOffset(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            _dateTime = (DateTime)info.GetValue("DateTime", typeof(DateTime))!; // Do not rename (binary serialization)
            _offsetMinutes = (short)info.GetValue("OffsetMinutes", typeof(short))!; // Do not rename (binary serialization)
        }

        // Returns the hash code for this DateTimeOffset.
        //
        public override int GetHashCode() => UtcTicks.GetHashCode();

        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTimeOffset Parse(string input)
        {
            if (input == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);

            DateTime dateResult = DateTimeParse.Parse(input,
                                                      DateTimeFormatInfo.CurrentInfo,
                                                      DateTimeStyles.None,
                                                      out TimeSpan offset);
            return CreateValidateOffset(dateResult, offset);
        }

        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTimeOffset Parse(string input, IFormatProvider? formatProvider)
            => Parse(input, formatProvider, DateTimeStyles.None);

        public static DateTimeOffset Parse(string input, IFormatProvider? formatProvider, DateTimeStyles styles)
        {
            styles = ValidateStyles(styles);
            if (input == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);

            DateTime dateResult = DateTimeParse.Parse(input,
                                                      DateTimeFormatInfo.GetInstance(formatProvider),
                                                      styles,
                                                      out TimeSpan offset);
            return CreateValidateOffset(dateResult, offset);
        }

        public static DateTimeOffset Parse(ReadOnlySpan<char> input, IFormatProvider? formatProvider = null, DateTimeStyles styles = DateTimeStyles.None)
        {
            styles = ValidateStyles(styles);
            DateTime dateResult = DateTimeParse.Parse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out TimeSpan offset);
            return CreateValidateOffset(dateResult, offset);
        }

        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTimeOffset ParseExact(string input, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, IFormatProvider? formatProvider)
            => ParseExact(input, format, formatProvider, DateTimeStyles.None);

        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        //
        public static DateTimeOffset ParseExact(string input, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, IFormatProvider? formatProvider, DateTimeStyles styles)
        {
            styles = ValidateStyles(styles);
            if (input == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            if (format == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);

            DateTime dateResult = DateTimeParse.ParseExact(input,
                                                           format,
                                                           DateTimeFormatInfo.GetInstance(formatProvider),
                                                           styles,
                                                           out TimeSpan offset);
            return CreateValidateOffset(dateResult, offset);
        }

        public static DateTimeOffset ParseExact(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] ReadOnlySpan<char> format, IFormatProvider? formatProvider, DateTimeStyles styles = DateTimeStyles.None)
        {
            styles = ValidateStyles(styles);
            DateTime dateResult = DateTimeParse.ParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out TimeSpan offset);
            return CreateValidateOffset(dateResult, offset);
        }

        public static DateTimeOffset ParseExact(string input, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string[] formats, IFormatProvider? formatProvider, DateTimeStyles styles)
        {
            styles = ValidateStyles(styles);
            if (input == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);

            DateTime dateResult = DateTimeParse.ParseExactMultiple(input,
                                                                   formats,
                                                                   DateTimeFormatInfo.GetInstance(formatProvider),
                                                                   styles,
                                                                   out TimeSpan offset);
            return CreateValidateOffset(dateResult, offset);
        }

        public static DateTimeOffset ParseExact(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string[] formats, IFormatProvider? formatProvider, DateTimeStyles styles = DateTimeStyles.None)
        {
            styles = ValidateStyles(styles);
            DateTime dateResult = DateTimeParse.ParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out TimeSpan offset);
            return CreateValidateOffset(dateResult, offset);
        }

        public TimeSpan Subtract(DateTimeOffset value) => new TimeSpan(UtcTicks - value.UtcTicks);

        public DateTimeOffset Subtract(TimeSpan value) => Add(ClockDateTime.Subtract(value));

        public long ToFileTime() => UtcDateTime.ToFileTimeUtc();

        public long ToUnixTimeSeconds()
        {
            // Truncate sub-second precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times.
            //
            // For example, consider the DateTimeOffset 12/31/1969 12:59:59.001 +0
            //   ticks            = 621355967990010000
            //   ticksFromEpoch   = ticks - DateTime.UnixEpochTicks          = -9990000
            //   secondsFromEpoch = ticksFromEpoch / TimeSpan.TicksPerSecond = 0
            //
            // Notice that secondsFromEpoch is rounded *up* by the truncation induced by integer division,
            // whereas we actually always want to round *down* when converting to Unix time. This happens
            // automatically for positive Unix time values. Now the example becomes:
            //   seconds          = ticks / TimeSpan.TicksPerSecond = 62135596799
            //   secondsFromEpoch = seconds - UnixEpochSeconds      = -1
            //
            // In other words, we want to consistently round toward the time 1/1/0001 00:00:00,
            // rather than toward the Unix Epoch (1/1/1970 00:00:00).
            long seconds = (long)((ulong)UtcTicks / TimeSpan.TicksPerSecond);
            return seconds - UnixEpochSeconds;
        }

        public long ToUnixTimeMilliseconds()
        {
            // Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times
            long milliseconds = (long)((ulong)UtcTicks / TimeSpan.TicksPerMillisecond);
            return milliseconds - UnixEpochMilliseconds;
        }

        public DateTimeOffset ToLocalTime() => ToLocalTime(UtcDateTime, false);

        private static DateTimeOffset ToLocalTime(DateTime utcDateTime, bool throwOnOverflow)
        {
            TimeSpan offset = TimeZoneInfo.GetLocalUtcOffset(utcDateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
            long localTicks = utcDateTime.Ticks + offset.Ticks;
            if ((ulong)localTicks > DateTime.MaxTicks)
            {
                if (throwOnOverflow)
                    throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);

                localTicks = localTicks < DateTime.MinTicks ? DateTime.MinTicks : DateTime.MaxTicks;
            }

            return CreateValidateOffset(DateTime.CreateUnchecked(localTicks), offset);
        }

        public override string ToString() =>
            DateTimeFormat.Format(ClockDateTime, null, null, Offset);

        public string ToString([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string? format) =>
            DateTimeFormat.Format(ClockDateTime, format, null, Offset);

        public string ToString(IFormatProvider? formatProvider) =>
            DateTimeFormat.Format(ClockDateTime, null, formatProvider, Offset);

        public string ToString([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string? format, IFormatProvider? formatProvider) =>
            DateTimeFormat.Format(ClockDateTime, format, formatProvider, Offset);

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null) =>
            DateTimeFormat.TryFormat(ClockDateTime, destination, out charsWritten, format, formatProvider, Offset);

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null) =>
            DateTimeFormat.TryFormat(ClockDateTime, utf8Destination, out bytesWritten, format, formatProvider, Offset);

        public DateTimeOffset ToUniversalTime() => new DateTimeOffset(0, _dateTime);

        public static bool TryParse([NotNullWhen(true)] string? input, out DateTimeOffset result)
        {
            bool parsed = DateTimeParse.TryParse(input,
                                                    DateTimeFormatInfo.CurrentInfo,
                                                    DateTimeStyles.None,
                                                    out DateTime dateResult,
                                                    out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        public static bool TryParse(ReadOnlySpan<char> input, out DateTimeOffset result)
        {
            bool parsed = DateTimeParse.TryParse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out DateTime dateResult, out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        public static bool TryParse([NotNullWhen(true)] string? input, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
        {
            styles = ValidateStyles(styles);
            if (input == null)
            {
                result = default;
                return false;
            }

            bool parsed = DateTimeParse.TryParse(input,
                                                    DateTimeFormatInfo.GetInstance(formatProvider),
                                                    styles,
                                                    out DateTime dateResult,
                                                    out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        public static bool TryParse(ReadOnlySpan<char> input, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
        {
            styles = ValidateStyles(styles);
            bool parsed = DateTimeParse.TryParse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out DateTime dateResult, out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string? format, IFormatProvider? formatProvider, DateTimeStyles styles,
                                            out DateTimeOffset result)
        {
            styles = ValidateStyles(styles);
            if (input == null || format == null)
            {
                result = default;
                return false;
            }

            bool parsed = DateTimeParse.TryParseExact(input,
                                                         format,
                                                         DateTimeFormatInfo.GetInstance(formatProvider),
                                                         styles,
                                                         out DateTime dateResult,
                                                         out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        public static bool TryParseExact(
            ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] ReadOnlySpan<char> format, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
        {
            styles = ValidateStyles(styles);
            bool parsed = DateTimeParse.TryParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out DateTime dateResult, out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string?[]? formats, IFormatProvider? formatProvider, DateTimeStyles styles,
                                            out DateTimeOffset result)
        {
            styles = ValidateStyles(styles);
            if (input == null)
            {
                result = default;
                return false;
            }

            bool parsed = DateTimeParse.TryParseExactMultiple(input,
                                                                 formats,
                                                                 DateTimeFormatInfo.GetInstance(formatProvider),
                                                                 styles,
                                                                 out DateTime dateResult,
                                                                 out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        public static bool TryParseExact(
            ReadOnlySpan<char> input, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string?[]? formats, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
        {
            styles = ValidateStyles(styles);
            bool parsed = DateTimeParse.TryParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out DateTime dateResult, out TimeSpan offset);
            result = CreateValidateOffset(dateResult, offset);
            return parsed;
        }

        // Ensures the TimeSpan is valid to go in a DateTimeOffset.
        private static int ValidateOffset(TimeSpan offset)
        {
            long minutes = offset.Ticks / TimeSpan.TicksPerMinute;
            if (offset.Ticks != minutes * TimeSpan.TicksPerMinute)
            {
                ThrowOffsetPrecision();
                static void ThrowOffsetPrecision() => throw new ArgumentException(SR.Argument_OffsetPrecision, nameof(offset));
            }
            if (minutes < MinOffsetMinutes || minutes > MaxOffsetMinutes)
            {
                ThrowOffsetOutOfRange();
                static void ThrowOffsetOutOfRange() => throw new ArgumentOutOfRangeException(nameof(offset), SR.Argument_OffsetOutOfRange);
            }
            return (int)minutes;
        }

        // Ensures that the time and offset are in range.
        private static DateTime ValidateDate(DateTime dateTime, TimeSpan offset)
        {
            // The key validation is that both the UTC and clock times fit. The clock time is validated
            // by the DateTime constructor.
            Debug.Assert(offset.Ticks >= MinOffset && offset.Ticks <= MaxOffset, "Offset not validated.");

            // This operation cannot overflow because offset should have already been validated to be within
            // 14 hours and the DateTime instance is more than that distance from the boundaries of long.
            long utcTicks = dateTime.Ticks - offset.Ticks;
            if ((ulong)utcTicks > DateTime.MaxTicks)
            {
                ThrowOutOfRange();
                static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(offset), SR.Argument_UTCOutOfRange);
            }
            // make sure the Kind is set to Unspecified
            return DateTime.CreateUnchecked(utcTicks);
        }

        private static DateTimeStyles ValidateStyles(DateTimeStyles styles)
        {
            const DateTimeStyles localUniversal = DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal;

            if ((styles & (DateTimeFormatInfo.InvalidDateTimeStyles | DateTimeStyles.NoCurrentDateDefault)) != 0
                || (styles & localUniversal) == localUniversal)
            {
                ThrowInvalid(styles);
            }

            // RoundtripKind does not make sense for DateTimeOffset; ignore this flag for backward compatibility with DateTime
            // AssumeLocal is also ignored as that is what we do by default with DateTimeOffset.Parse
            return styles & (~DateTimeStyles.RoundtripKind & ~DateTimeStyles.AssumeLocal);

            static void ThrowInvalid(DateTimeStyles styles)
            {
                string message = (styles & DateTimeFormatInfo.InvalidDateTimeStyles) != 0 ? SR.Argument_InvalidDateTimeStyles
                    : (styles & localUniversal) == localUniversal ? SR.Argument_ConflictingDateTimeStyles
                    : SR.Argument_DateTimeOffsetInvalidDateTimeStyles;
                throw new ArgumentException(message, nameof(styles));
            }
        }

        // Operators

        public static implicit operator DateTimeOffset(DateTime dateTime) =>
            new DateTimeOffset(dateTime);

        public static DateTimeOffset operator +(DateTimeOffset dateTimeOffset, TimeSpan timeSpan) =>
            dateTimeOffset.Add(dateTimeOffset.ClockDateTime + timeSpan);

        public static DateTimeOffset operator -(DateTimeOffset dateTimeOffset, TimeSpan timeSpan) =>
            dateTimeOffset.Add(dateTimeOffset.ClockDateTime - timeSpan);

        public static TimeSpan operator -(DateTimeOffset left, DateTimeOffset right) =>
            new TimeSpan(left.UtcTicks - right.UtcTicks);

        public static bool operator ==(DateTimeOffset left, DateTimeOffset right) =>
            left.UtcTicks == right.UtcTicks;

        public static bool operator !=(DateTimeOffset left, DateTimeOffset right) =>
            left.UtcTicks != right.UtcTicks;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(DateTimeOffset left, DateTimeOffset right) =>
            left.UtcTicks < right.UtcTicks;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(DateTimeOffset left, DateTimeOffset right) =>
            left.UtcTicks <= right.UtcTicks;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(DateTimeOffset left, DateTimeOffset right) =>
            left.UtcTicks > right.UtcTicks;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(DateTimeOffset left, DateTimeOffset right) =>
            left.UtcTicks >= right.UtcTicks;

        /// <summary>
        /// Deconstructs <see cref="DateTimeOffset"/> into <see cref="DateOnly"/>, <see cref="TimeOnly"/> and <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="date">
        /// Deconstructed <see cref="DateOnly"/>.
        /// </param>
        /// <param name="time">
        /// Deconstructed <see cref="TimeOnly"/>
        /// </param>
        /// <param name="offset">
        /// Deconstructed parameter for <see cref="Offset"/>.
        /// </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out DateOnly date, out TimeOnly time, out TimeSpan offset)
        {
            (date, time) = ClockDateTime;
            offset = Offset;
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out DateTimeOffset result) => TryParse(s, provider, DateTimeStyles.None, out result);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static DateTimeOffset Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DateTimeOffset result) => TryParse(s, provider, DateTimeStyles.None, out result);
    }
}
