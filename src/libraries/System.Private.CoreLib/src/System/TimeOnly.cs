// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;

namespace System
{
    /// <summary>
    /// Represents a time of day, as would be read from a clock, within the range 00:00:00 to 23:59:59.9999999.
    /// </summary>
    public readonly struct TimeOnly
        : IComparable,
          IComparable<TimeOnly>,
          IEquatable<TimeOnly>,
          ISpanFormattable,
          IComparisonOperators<TimeOnly, TimeOnly>,
          IMinMaxValue<TimeOnly>,
          ISpanParseable<TimeOnly>,
          ISubtractionOperators<TimeOnly, TimeOnly, TimeSpan>
    {
        // represent the number of ticks map to the time of the day. 1 ticks = 100-nanosecond in time measurements.
        private readonly long _ticks;

        // MinTimeTicks is the ticks for the midnight time 00:00:00.000 AM
        private const long MinTimeTicks = 0;

        // MaxTimeTicks is the max tick value for the time in the day. It is calculated using DateTime.Today.AddTicks(-1).TimeOfDay.Ticks.
        private const long MaxTimeTicks = 863_999_999_999;

        /// <summary>
        /// Represents the smallest possible value of TimeOnly.
        /// </summary>
        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static TimeOnly MinValue => new TimeOnly((ulong)MinTimeTicks);

        /// <summary>
        /// Represents the largest possible value of TimeOnly.
        /// </summary>
        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static TimeOnly MaxValue => new TimeOnly((ulong)MaxTimeTicks);

        /// <summary>
        /// Initializes a new instance of the timeOnly structure to the specified hour and the minute.
        /// </summary>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        public TimeOnly(int hour, int minute) : this(DateTime.TimeToTicks(hour, minute, 0, 0)) {}

        /// <summary>
        /// Initializes a new instance of the timeOnly structure to the specified hour, minute, and second.
        /// </summary>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        public TimeOnly(int hour, int minute, int second) : this(DateTime.TimeToTicks(hour, minute, second, 0)) {}

        /// <summary>
        /// Initializes a new instance of the timeOnly structure to the specified hour, minute, second, and millisecond.
        /// </summary>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The millisecond (0 through 999).</param>
        public TimeOnly(int hour, int minute, int second, int millisecond) : this(DateTime.TimeToTicks(hour, minute, second, millisecond)) {}

        /// <summary>
        /// Initializes a new instance of the TimeOnly structure using a specified number of ticks.
        /// </summary>
        /// <param name="ticks">A time of day expressed in the number of 100-nanosecond units since 00:00:00.0000000.</param>
        public TimeOnly(long ticks)
        {
            if ((ulong)ticks > MaxTimeTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks), SR.ArgumentOutOfRange_TimeOnlyBadTicks);
            }

            _ticks = ticks;
        }

        // exist to bypass the check in the public constructor.
        internal TimeOnly(ulong ticks) => _ticks = (long)ticks;

        /// <summary>
        /// Gets the hour component of the time represented by this instance.
        /// </summary>
        public int Hour => new TimeSpan(_ticks).Hours;

        /// <summary>
        /// Gets the minute component of the time represented by this instance.
        /// </summary>
        public int Minute => new TimeSpan(_ticks).Minutes;

        /// <summary>
        /// Gets the second component of the time represented by this instance.
        /// </summary>
        public int Second => new TimeSpan(_ticks).Seconds;

        /// <summary>
        /// Gets the millisecond component of the time represented by this instance.
        /// </summary>
        public int Millisecond => new TimeSpan(_ticks).Milliseconds;

        /// <summary>
        /// Gets the number of ticks that represent the time of this instance.
        /// </summary>
        public long Ticks => _ticks;

        private TimeOnly AddTicks(long ticks) => new TimeOnly((_ticks + TimeSpan.TicksPerDay + (ticks % TimeSpan.TicksPerDay)) % TimeSpan.TicksPerDay);

        private TimeOnly AddTicks(long ticks, out int wrappedDays)
        {
            wrappedDays = (int)(ticks / TimeSpan.TicksPerDay);
            long newTicks = _ticks + ticks % TimeSpan.TicksPerDay;
            if (newTicks < 0)
            {
                wrappedDays--;
                newTicks += TimeSpan.TicksPerDay;
            }
            else
            {
                if (newTicks >= TimeSpan.TicksPerDay)
                {
                    wrappedDays++;
                    newTicks -= TimeSpan.TicksPerDay;
                }
            }

            return new TimeOnly(newTicks);
        }

        /// <summary>
        /// Returns a new TimeOnly that adds the value of the specified TimeSpan to the value of this instance.
        /// </summary>
        /// <param name="value">A positive or negative time interval.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the time interval represented by value.</returns>
        public TimeOnly Add(TimeSpan value) => AddTicks(value.Ticks);

        /// <summary>
        /// Returns a new TimeOnly that adds the value of the specified TimeSpan to the value of this instance.
        /// If the result wraps past the end of the day, this method will return the number of excess days as an out parameter.
        /// </summary>
        /// <param name="value">A positive or negative time interval.</param>
        /// <param name="wrappedDays">When this method returns, contains the number of excess days if any that resulted from wrapping during this addition operation.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the time interval represented by value.</returns>
        public TimeOnly Add(TimeSpan value, out int wrappedDays) => AddTicks(value.Ticks, out wrappedDays);

        /// <summary>
        /// Returns a new TimeOnly that adds the specified number of hours to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional hours. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of hours represented by value.</returns>
        public TimeOnly AddHours(double value) => AddTicks((long)(value * TimeSpan.TicksPerHour));

        /// <summary>
        /// Returns a new TimeOnly that adds the specified number of hours to the value of this instance.
        /// If the result wraps past the end of the day, this method will return the number of excess days as an out parameter.
        /// </summary>
        /// <param name="value">A number of whole and fractional hours. The value parameter can be negative or positive.</param>
        /// <param name="wrappedDays">When this method returns, contains the number of excess days if any that resulted from wrapping during this addition operation.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of hours represented by value.</returns>
        public TimeOnly AddHours(double value, out int wrappedDays) => AddTicks((long)(value * TimeSpan.TicksPerHour), out wrappedDays);

        /// <summary>
        /// Returns a new TimeOnly that adds the specified number of minutes to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional minutes. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of minutes represented by value.</returns>
        public TimeOnly AddMinutes(double value) => AddTicks((long)(value * TimeSpan.TicksPerMinute));

        /// <summary>
        /// Returns a new TimeOnly that adds the specified number of minutes to the value of this instance.
        /// If the result wraps past the end of the day, this method will return the number of excess days as an out parameter.
        /// </summary>
        /// <param name="value">A number of whole and fractional minutes. The value parameter can be negative or positive.</param>
        /// <param name="wrappedDays">When this method returns, contains the number of excess days if any that resulted from wrapping during this addition operation.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of minutes represented by value.</returns>
        public TimeOnly AddMinutes(double value, out int wrappedDays) => AddTicks((long)(value * TimeSpan.TicksPerMinute), out wrappedDays);

        /// <summary>
        /// Determines if a time falls within the range provided.
        /// Supports both "normal" ranges such as 10:00-12:00, and ranges that span midnight such as 23:00-01:00.
        /// </summary>
        /// <param name="start">The starting time of day, inclusive.</param>
        /// <param name="end">The ending time of day, exclusive.</param>
        /// <returns>True, if the time falls within the range, false otherwise.</returns>
        /// <remarks>
        /// If <paramref name="start"/> and <paramref name="end"/> are equal, this method returns false, meaning there is zero elapsed time between the two values.
        /// If you wish to treat such cases as representing one or more whole days, then first check for equality before calling this method.
        /// </remarks>
        public bool IsBetween(TimeOnly start, TimeOnly end)
        {
            long startTicks = start._ticks;
            long endTicks = end._ticks;

            return startTicks <= endTicks
                ? (startTicks <= _ticks && endTicks > _ticks)
                : (startTicks <= _ticks || endTicks > _ticks);
        }

        /// <summary>
        /// Determines whether two specified instances of TimeOnly are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left and right represent the same time; otherwise, false.</returns>
        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(TimeOnly left, TimeOnly right) => left._ticks == right._ticks;

        /// <summary>
        /// Determines whether two specified instances of TimeOnly are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left and right do not represent the same time; otherwise, false.</returns>
        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(TimeOnly left, TimeOnly right) => left._ticks != right._ticks;

        /// <summary>
        /// Determines whether one specified TimeOnly is later than another specified TimeOnly.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is later than right; otherwise, false.</returns>
        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(TimeOnly left, TimeOnly right) => left._ticks > right._ticks;

        /// <summary>
        /// Determines whether one specified TimeOnly represents a time that is the same as or later than another specified TimeOnly.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is the same as or later than right; otherwise, false.</returns>
        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(TimeOnly left, TimeOnly right) => left._ticks >= right._ticks;

        /// <summary>
        /// Determines whether one specified TimeOnly is earlier than another specified TimeOnly.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is earlier than right; otherwise, false.</returns>
        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(TimeOnly left, TimeOnly right) => left._ticks < right._ticks;

        /// <summary>
        /// Determines whether one specified TimeOnly represents a time that is the same as or earlier than another specified TimeOnly.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is the same as or earlier than right; otherwise, false.</returns>
        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(TimeOnly left, TimeOnly right) => left._ticks <= right._ticks;

        /// <summary>
        ///  Gives the elapsed time between two points on a circular clock, which will always be a positive value.
        /// </summary>
        /// <param name="t1">The first TimeOnly instance.</param>
        /// <param name="t2">The second TimeOnly instance..</param>
        /// <returns>The elapsed time between t1 and t2.</returns>
        public static TimeSpan operator -(TimeOnly t1, TimeOnly t2) => new TimeSpan((t1._ticks - t2._ticks + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay);

        /// <summary>
        /// Constructs a TimeOnly object from a TimeSpan representing the time elapsed since midnight.
        /// </summary>
        /// <param name="timeSpan">The time interval measured since midnight. This value has to be positive and not exceeding the time of the day.</param>
        /// <returns>A TimeOnly object representing the time elapsed since midnight using the timeSpan value.</returns>
        public static TimeOnly FromTimeSpan(TimeSpan timeSpan) => new TimeOnly(timeSpan._ticks);

        /// <summary>
        /// Constructs a TimeOnly object from a DateTime representing the time of the day in this DateTime object.
        /// </summary>
        /// <param name="dateTime">The time DateTime object to extract the time of the day from.</param>
        /// <returns>A TimeOnly object representing time of the day specified in the DateTime object.</returns>
        public static TimeOnly FromDateTime(DateTime dateTime) => new TimeOnly(dateTime.TimeOfDay.Ticks);

        /// <summary>
        /// Convert the current TimeOnly instance to a TimeSpan object.
        /// </summary>
        /// <returns>A TimeSpan object spanning to the time specified in the current TimeOnly object.</returns>
        public TimeSpan ToTimeSpan() => new TimeSpan(_ticks);

        internal DateTime ToDateTime() => new DateTime(_ticks);

        /// <summary>
        /// Compares the value of this instance to a specified TimeOnly value and indicates whether this instance is earlier than, the same as, or later than the specified TimeOnly value.
        /// </summary>
        /// <param name="value">The object to compare to the current instance.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and the value parameter.
        /// Less than zero if this instance is earlier than value.
        /// Zero if this instance is the same as value.
        /// Greater than zero if this instance is later than value.
        /// </returns>
        public int CompareTo(TimeOnly value) => _ticks.CompareTo(value._ticks);

        /// <summary>
        /// Compares the value of this instance to a specified object that contains a specified TimeOnly value, and returns an integer that indicates whether this instance is earlier than, the same as, or later than the specified TimeOnly value.
        /// </summary>
        /// <param name="value">A boxed object to compare, or null.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and the value parameter.
        /// Less than zero if this instance is earlier than value.
        /// Zero if this instance is the same as value.
        /// Greater than zero if this instance is later than value.
        /// </returns>
        public int CompareTo(object? value)
        {
            if (value == null) return 1;
            if (value is not TimeOnly timeOnly)
            {
                throw new ArgumentException(SR.Arg_MustBeTimeOnly);
            }

            return CompareTo(timeOnly);
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the value of the specified TimeOnly instance.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns>true if the value parameter equals the value of this instance; otherwise, false.</returns>
        public bool Equals(TimeOnly value) => _ticks == value._ticks;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns>true if value is an instance of TimeOnly and equals the value of this instance; otherwise, false.</returns>
        public override bool Equals([NotNullWhen(true)] object? value) => value is TimeOnly timeOnly && _ticks == timeOnly._ticks;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            long ticks = _ticks;
            return unchecked((int)ticks) ^ (int)(ticks >> 32);
        }

        private const ParseFlags ParseFlagsTimeMask = ParseFlags.HaveYear | ParseFlags.HaveMonth | ParseFlags.HaveDay | ParseFlags.HaveDate | ParseFlags.TimeZoneUsed |
                                                      ParseFlags.TimeZoneUtc | ParseFlags.ParsedMonthName | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern;

        /// <summary>
        /// Converts a memory span that contains string representation of a time to its TimeOnly equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The memory span that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by provider and styles.</returns>
        /// <inheritdoc cref="ISpanParseable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static TimeOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            ParseFailureKind result = TryParseInternal(s, provider, style, out TimeOnly timeOnly);
            if (result != ParseFailureKind.None)
            {
                ThrowOnError(result, s);
            }

            return timeOnly;
        }

        private const string OFormat = "HH':'mm':'ss'.'fffffff";
        private const string RFormat = "HH':'mm':'ss";

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="format">A span containing the characters that represent a format specifier that defines the required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by format, provider, and style.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            ParseFailureKind result = TryParseExactInternal(s, format, provider, style, out TimeOnly timeOnly);
            if (result != ParseFailureKind.None)
            {
                ThrowOnError(result, s);
            }

            return timeOnly;
        }

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by format, provider, and style.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by format, provider, and style.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            ParseFailureKind result = TryParseExactInternal(s, formats, provider, style, out TimeOnly timeOnly);
            if (result != ParseFailureKind.None)
            {
                ThrowOnError(result, s);
            }

            return timeOnly;
        }

        /// <summary>
        /// Converts a string that contains string representation of a time to its TimeOnly equivalent by using the conventions of the current culture.
        /// </summary>
        /// <param name="s">The string that contains the string to parse.</param>
        /// <returns>An object that is equivalent to the time contained in s.</returns>
        public static TimeOnly Parse(string s) => Parse(s, null, DateTimeStyles.None);

        /// <summary>
        /// Converts a string that contains string representation of a time to its TimeOnly equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The string that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about s.</param>
        /// <param name="style">A bitwise combination of the enumeration values that indicates the style elements that can be present in s for the parse operation to succeed, and that defines how to interpret the parsed date. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by provider and styles.</returns>
        public static TimeOnly Parse(string s, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse(s.AsSpan(), provider, style);
        }

        /// <summary>
        /// Converts the specified string representation of a time to its TimeOnly equivalent using the specified format.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="format">A string that represent a format specifier that defines the required format of s.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by format.</returns>
        public static TimeOnly ParseExact(string s, string format) => ParseExact(s, format, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a time to its TimeOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="format">A string containing the characters that represent a format specifier that defines the required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of the enumeration values that provides additional information about s, about style elements that may be present in s, or about the conversion from s to a TimeOnly value. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by format, provider, and style.</returns>
        public static TimeOnly ParseExact(string s, string format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            if (format == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
            return ParseExact(s.AsSpan(), format.AsSpan(), provider, style);
        }

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by format, provider, and style.</returns>
        public static TimeOnly ParseExact(string s, string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a time to its TimeOnly equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the time contained in s, as specified by format, provider, and style.</returns>
        public static TimeOnly ParseExact(string s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return ParseExact(s.AsSpan(), formats, provider, style);
        }

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the time to convert.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out TimeOnly result) => TryParse(s, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        /// <inheritdoc cref="ISpanParseable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result) =>
                            TryParseInternal(s, provider, style, out result) == ParseFailureKind.None;
        private static ParseFailureKind TryParseInternal(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            if ((style & ~DateTimeStyles.AllowWhiteSpaces) != 0)
            {
                result = default;
                return ParseFailureKind.FormatWithParameter;
            }

            DateTimeResult dtResult = default;

            dtResult.Init(s);

            if (!DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), style, ref dtResult))
            {
                result = default;
                return ParseFailureKind.FormatWithOriginalDateTime;
            }

            if ((dtResult.flags & ParseFlagsTimeMask) != 0)
            {
                result = default;
                return ParseFailureKind.WrongParts;
            }

            result = new TimeOnly(dtResult.parsedDate.TimeOfDay.Ticks);

            return ParseFailureKind.None;
        }

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a time that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, out TimeOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of one or more enumeration values that indicate the permitted format of s.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a time that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result) =>
                            TryParseExactInternal(s, format, provider, style, out result) == ParseFailureKind.None;

        private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            if ((style & ~DateTimeStyles.AllowWhiteSpaces) != 0)
            {
                result = default;
                return ParseFailureKind.FormatWithParameter;
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'o':
                    case 'O':
                        format = OFormat;
                        provider = CultureInfo.InvariantCulture.DateTimeFormat;
                        break;

                    case 'r':
                    case 'R':
                        format = RFormat;
                        provider = CultureInfo.InvariantCulture.DateTimeFormat;
                        break;
                }
            }

            DateTimeResult dtResult = default;
            dtResult.Init(s);

            if (!DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, ref dtResult))
            {
                result = default;
                return ParseFailureKind.FormatWithOriginalDateTime;
            }

            if ((dtResult.flags & ParseFlagsTimeMask) != 0)
            {
                result = default;
                return ParseFailureKind.WrongParts;
            }

            result = new TimeOnly(dtResult.parsedDate.TimeOfDay.Ticks);

            return ParseFailureKind.None;
        }

        /// <summary>
        /// Converts the specified char span of a time to its TimeOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)] string?[]? formats, out TimeOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified char span of a time to its TimeOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that defines how to interpret the parsed time. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result) =>
                            TryParseExactInternal(s, formats, provider, style, out result) == ParseFailureKind.None;

        private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            if ((style & ~DateTimeStyles.AllowWhiteSpaces) != 0 || formats == null)
            {
                result = default;
                return ParseFailureKind.FormatWithParameter;
            }

            DateTimeFormatInfo dtfi = DateTimeFormatInfo.GetInstance(provider);

            for (int i = 0; i < formats.Length; i++)
            {
                DateTimeFormatInfo dtfiToUse = dtfi;
                string? format = formats[i];
                if (string.IsNullOrEmpty(format))
                {
                    result = default;
                    return ParseFailureKind.FormatWithFormatSpecifier;
                }

                if (format.Length == 1)
                {
                    switch (format[0])
                    {
                        case 'o':
                        case 'O':
                            format = OFormat;
                            dtfiToUse = CultureInfo.InvariantCulture.DateTimeFormat;
                            break;

                        case 'r':
                        case 'R':
                            format = RFormat;
                            dtfiToUse = CultureInfo.InvariantCulture.DateTimeFormat;
                            break;
                    }
                }

                // Create a new result each time to ensure the runs are independent. Carry through
                // flags from the caller and return the result.
                DateTimeResult dtResult = default;
                dtResult.Init(s);
                if (DateTimeParse.TryParseExact(s, format, dtfiToUse, style, ref dtResult) &&  ((dtResult.flags & ParseFlagsTimeMask) == 0))
                {
                    result = new TimeOnly(dtResult.parsedDate.TimeOfDay.Ticks);
                    return ParseFailureKind.None;
                }
            }

            result = default;
            return ParseFailureKind.FormatWithOriginalDateTime;
        }

        /// <summary>
        /// Converts the specified string representation of a time to its TimeOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing the time to convert.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out TimeOnly result) => TryParse(s, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string representation of a time to its TimeOnly equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParse(s.AsSpan(), provider, style, out result);
        }

        /// <summary>
        /// Converts the specified string representation of a time to its TimeOnly equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a time that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, out TimeOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a time to its TimeOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of one or more enumeration values that indicate the permitted format of s.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a time that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            if (s == null || format == null)
            {
                result = default;
                return false;
            }

            return TryParseExact(s.AsSpan(), format.AsSpan(), provider, style, out result);
        }

        /// <summary>
        /// Converts the specified string of a time to its TimeOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing time to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="result">When this method returns, contains the timeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string?[]? formats, out TimeOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string of a time to its TimeOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing the time to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that defines how to interpret the parsed date. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the TimeOnly value equivalent to the time contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParseExact(s.AsSpan(), formats, provider, style, out result);
        }

        private static void ThrowOnError(ParseFailureKind result, ReadOnlySpan<char> s)
        {
            Debug.Assert(result != ParseFailureKind.None);
            switch (result)
            {
                case ParseFailureKind.FormatWithParameter: throw new ArgumentException(SR.Argument_InvalidDateStyles, "style");
                case ParseFailureKind.FormatWithOriginalDateTime: throw new FormatException(SR.Format(SR.Format_BadTimeOnly, s.ToString()));
                case ParseFailureKind.FormatWithFormatSpecifier: throw new FormatException(SR.Argument_BadFormatSpecifier);
                default:
                    Debug.Assert(result == ParseFailureKind.WrongParts);
                    throw new FormatException(SR.Format(SR.Format_DateTimeOnlyContainsNoneDateParts, s.ToString(), nameof(TimeOnly)));
            }
        }

        /// <summary>
        /// Converts the value of the current TimeOnly object to its equivalent long date string representation.
        /// </summary>
        /// <returns>A string that contains the long time string representation of the current TimeOnly object.</returns>
        public string ToLongTimeString() => ToString("T");

        /// <summary>
        /// Converts the value of the current TimeOnly object to its equivalent short time string representation.
        /// </summary>
        /// <returns>A string that contains the short time string representation of the current TimeOnly object.</returns>
        public string ToShortTimeString() => ToString();

        /// <summary>
        /// Converts the value of the current TimeOnly object to its equivalent string representation using the formatting conventions of the current culture.
        /// The TimeOnly object will be formatted in short form.
        /// </summary>
        /// <returns>A string that contains the short time string representation of the current TimeOnly object.</returns>
        public override string ToString() => ToString("t");

        /// <summary>
        /// Converts the value of the current TimeOnly object to its equivalent string representation using the specified format and the formatting conventions of the current culture.
        /// </summary>
        /// <param name="format">A standard or custom time format string.</param>
        /// <returns>A string representation of value of the current TimeOnly object as specified by format.</returns>
        /// <remarks>The accepted standard formats are 'r', 'R', 'o', 'O', 't' and 'T'. </remarks>
        public string ToString(string? format) => ToString(format, null);

        /// <summary>
        /// Converts the value of the current TimeOnly object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current TimeOnly object as specified by provider.</returns>
        public string ToString(IFormatProvider? provider) => ToString("t", provider);

        /// <summary>
        /// Converts the value of the current TimeOnly object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="format">A standard or custom time format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current TimeOnly object as specified by format and provider.</returns>
        /// <remarks>The accepted standard formats are 'r', 'R', 'o', 'O', 't' and 'T'. </remarks>
        public string ToString(string? format, IFormatProvider? provider)
        {
            if (format == null || format.Length == 0)
            {
                format = "t";
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'o':
                    case 'O':
                        return string.Create(16, this, (destination, value) =>
                        {
                            bool b = DateTimeFormat.TryFormatTimeOnlyO(value.Hour, value.Minute, value.Second, value._ticks % TimeSpan.TicksPerSecond, destination);
                            Debug.Assert(b);
                        });

                    case 'r':
                    case 'R':
                        return string.Create(8, this, (destination, value) =>
                        {
                            bool b = DateTimeFormat.TryFormatTimeOnlyR(value.Hour, value.Minute, value.Second, destination);
                            Debug.Assert(b);
                        });

                    case 't':
                    case 'T':
                        return DateTimeFormat.Format(ToDateTime(), format, provider);

                    default:
                        throw new FormatException(SR.Format_InvalidString);
                }
            }

            DateTimeFormat.IsValidCustomTimeFormat(format.AsSpan(), throwOnError: true);
            return DateTimeFormat.Format(ToDateTime(), format, provider);
        }

        /// <summary>
        /// Tries to format the value of the current TimeOnly instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in destination.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for destination.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for destination.</param>
        /// <returns>true if the formatting was successful; otherwise, false.</returns>
        /// <remarks>The accepted standard formats are 'r', 'R', 'o', 'O', 't' and 'T'. </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
        {
            if (format.Length == 0)
            {
                format = "t";
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'o':
                    case 'O':
                        if (!DateTimeFormat.TryFormatTimeOnlyO(Hour, Minute, Second, _ticks % TimeSpan.TicksPerSecond, destination))
                        {
                            charsWritten = 0;
                            return false;
                        }
                        charsWritten = 16;
                        return true;

                    case 'r':
                    case 'R':
                        if (!DateTimeFormat.TryFormatTimeOnlyR(Hour, Minute, Second, destination))
                        {
                            charsWritten = 0;
                            return false;
                        }
                        charsWritten = 8;
                        return true;

                    case 't':
                    case 'T':
                        return DateTimeFormat.TryFormat(ToDateTime(), destination, out charsWritten, format, provider);

                    default:
                        throw new FormatException(SR.Argument_BadFormatSpecifier);
                }
            }

            if (!DateTimeFormat.IsValidCustomTimeFormat(format, throwOnError: false))
            {
                throw new FormatException(SR.Format(SR.Format_DateTimeOnlyContainsNoneDateParts, format.ToString(), nameof(TimeOnly)));
            }

            return DateTimeFormat.TryFormat(ToDateTime(), destination, out charsWritten, format, provider);
        }

        //
        // IMinMaxValue
        //

        static TimeOnly IMinMaxValue<TimeOnly>.MinValue => MinValue;

        static TimeOnly IMinMaxValue<TimeOnly>.MaxValue => MaxValue;

        //
        // IParseable
        //

        public static TimeOnly Parse(string s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TimeOnly result) => TryParse(s, provider, DateTimeStyles.None, out result);

        //
        // ISpanParseable
        //

        /// <inheritdoc cref="ISpanParseable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static TimeOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        /// <inheritdoc cref="ISpanParseable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TimeOnly result) => TryParse(s, provider, DateTimeStyles.None, out result);
    }
}
