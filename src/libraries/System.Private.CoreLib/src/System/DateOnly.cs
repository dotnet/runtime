// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System
{
    /// <summary>
    /// Represents dates with values ranging from January 1, 0001 Anno Domini (Common Era) through December 31, 9999 A.D. (C.E.) in the Gregorian calendar.
    /// </summary>
    public readonly struct DateOnly : IComparable, IComparable<DateOnly>, IEquatable<DateOnly>, ISpanFormattable
    {
        private readonly int _dayNumber;

        // Maps to Jan 1st year 1
        private const int MinDayNumber = 0;

        // Maps to December 31 year 9999. The value calculated from "new DateTime(9999, 12, 31).Ticks / TimeSpan.TicksPerDay"
        private const int MaxDayNumber = 3_652_058;

        private static int DayNumberFromDateTime(DateTime dt) => (int)(dt.Ticks / TimeSpan.TicksPerDay);

        private DateTime GetEquivalentDateTime() => DateTime.UnsafeCreate(_dayNumber * TimeSpan.TicksPerDay);

        private DateOnly(int dayNumber)
        {
            Debug.Assert((uint)dayNumber <= MaxDayNumber);
            _dayNumber = dayNumber;
        }

        /// <summary>
        /// Gets the earliest possible date that can be created.
        /// </summary>
        public static DateOnly MinValue => new DateOnly(MinDayNumber);

        /// <summary>
        /// Gets the latest possible date that can be created.
        /// </summary>
        public static DateOnly MaxValue => new DateOnly(MaxDayNumber);

        /// <summary>
        /// Creates a new instance of the DateOnly structure to the specified year, month, and day.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month" />).</param>
        public DateOnly(int year, int month, int day) => _dayNumber = DayNumberFromDateTime(new DateTime(year, month, day));

        /// <summary>
        /// Creates a new instance of the DateOnly structure to the specified year, month, and day for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through the number of years in calendar).</param>
        /// <param name="month">The month (1 through the number of months in calendar).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="calendar">The calendar that is used to interpret year, month, and day.<paramref name="month"/>.</param>
        public DateOnly(int year, int month, int day, Calendar calendar) => _dayNumber = DayNumberFromDateTime(new DateTime(year, month, day, calendar));

        /// <summary>
        /// Creates a new instance of the DateOnly structure to the specified number of days.
        /// </summary>
        /// <param name="dayNumber">The number of days since January 1, 0001 in the Proleptic Gregorian calendar.</param>
        public static DateOnly FromDayNumber(int dayNumber)
        {
            if ((uint)dayNumber > MaxDayNumber)
            {
                ThrowHelper.ThrowArgumentOutOfRange_DayNumber(dayNumber);
            }

            return new DateOnly(dayNumber);
        }

        /// <summary>
        /// Gets the year component of the date represented by this instance.
        /// </summary>
        public int Year => GetEquivalentDateTime().Year;

        /// <summary>
        /// Gets the month component of the date represented by this instance.
        /// </summary>
        public int Month  => GetEquivalentDateTime().Month;

        /// <summary>
        /// Gets the day component of the date represented by this instance.
        /// </summary>
        public int Day => GetEquivalentDateTime().Day;

        /// <summary>
        /// Gets the day of the week represented by this instance.
        /// </summary>
        public DayOfWeek DayOfWeek => GetEquivalentDateTime().DayOfWeek;

        /// <summary>
        /// Gets the day of the year represented by this instance.
        /// </summary>
        public int DayOfYear => GetEquivalentDateTime().DayOfYear;

        /// <summary>
        /// Gets the number of days since January 1, 0001 in the Proleptic Gregorian calendar represented by this instance.
        /// </summary>
        public int DayNumber => _dayNumber;

        /// <summary>
        /// Adds the specified number of days to the value of this instance.
        /// </summary>
        /// <param name="value">The number of days to add. To subtract days, specify a negative number.</param>
        /// <returns>An instance whose value is the sum of the date represented by this instance and the number of days represented by value.</returns>
        public DateOnly AddDays(int value)
        {
            int newDayNumber = _dayNumber + value;
            if ((uint)newDayNumber > MaxDayNumber)
            {
                ThrowOutOfRange();
            }

            return new DateOnly(newDayNumber);

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_AddValue);
        }

        /// <summary>
        /// Adds the specified number of months to the value of this instance.
        /// </summary>
        /// <param name="value">A number of months. The months parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date represented by this instance and months.</returns>
        public DateOnly AddMonths(int value) => new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddMonths(value)));

        /// <summary>
        /// Adds the specified number of years to the value of this instance.
        /// </summary>
        /// <param name="value">A number of years. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date represented by this instance and the number of years represented by value.</returns>
        public DateOnly AddYears(int value) => new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddYears(value)));

        /// <summary>
        /// Determines whether two specified instances of DateOnly are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left and right represent the same date; otherwise, false.</returns>
        public static bool operator ==(DateOnly left, DateOnly right) => left._dayNumber == right._dayNumber;

        /// <summary>
        /// Determines whether two specified instances of DateOnly are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left and right do not represent the same date; otherwise, false.</returns>
        public static bool operator !=(DateOnly left, DateOnly right) => left._dayNumber != right._dayNumber;

        /// <summary>
        /// Determines whether one specified DateOnly is later than another specified DateTime.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is later than right; otherwise, false.</returns>
        public static bool operator >(DateOnly left, DateOnly right) => left._dayNumber > right._dayNumber;

        /// <summary>
        /// Determines whether one specified DateOnly represents a date that is the same as or later than another specified DateOnly.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is the same as or later than right; otherwise, false.</returns>
        public static bool operator >=(DateOnly left, DateOnly right) => left._dayNumber >= right._dayNumber;

        /// <summary>
        /// Determines whether one specified DateOnly is earlier than another specified DateOnly.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is earlier than right; otherwise, false.</returns>
        public static bool operator <(DateOnly left, DateOnly right) => left._dayNumber < right._dayNumber;

        /// <summary>
        /// Determines whether one specified DateOnly represents a date that is the same as or earlier than another specified DateOnly.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>true if left is the same as or earlier than right; otherwise, false.</returns>
        public static bool operator <=(DateOnly left, DateOnly right) => left._dayNumber <= right._dayNumber;

        /// <summary>
        /// Returns a DateTime that is set to the date of this DateOnly instance and the time of specified input time.
        /// </summary>
        /// <param name="time">The time of the day.</param>
        /// <returns>The DateTime instance composed of the date of the current DateOnly instance and the time specified by the input time.</returns>
        public DateTime ToDateTime(TimeOnly time) => new DateTime(_dayNumber * TimeSpan.TicksPerDay + time.Ticks);

        /// <summary>
        /// Returns a DateTime instance with the specified input kind that is set to the date of this DateOnly instance and the time of specified input time.
        /// </summary>
        /// <param name="time">The time of the day.</param>
        /// <param name="kind">One of the enumeration values that indicates whether ticks specifies a local time, Coordinated Universal Time (UTC), or neither.</param>
        /// <returns>The DateTime instance composed of the date of the current DateOnly instance and the time specified by the input time.</returns>
        public DateTime ToDateTime(TimeOnly time, DateTimeKind kind) => new DateTime(_dayNumber * TimeSpan.TicksPerDay + time.Ticks, kind);

        /// <summary>
        /// Returns a DateOnly instance that is set to the date part of the specified dateTime.
        /// </summary>
        /// <param name="dateTime">The DateTime instance.</param>
        /// <returns>The DateOnly instance composed of the date part of the specified input time dateTime instance.</returns>
        public static DateOnly FromDateTime(DateTime dateTime) => new DateOnly(DayNumberFromDateTime(dateTime));

        /// <summary>
        /// Compares the value of this instance to a specified DateOnly value and returns an integer that indicates whether this instance is earlier than, the same as, or later than the specified DateTime value.
        /// </summary>
        /// <param name="value">The object to compare to the current instance.</param>
        /// <returns>Less than zero if this instance is earlier than value. Greater than zero if this instance is later than value. Zero if this instance is the same as value.</returns>
        public int CompareTo(DateOnly value) => _dayNumber.CompareTo(value._dayNumber);

        /// <summary>
        /// Compares the value of this instance to a specified object that contains a specified DateOnly value, and returns an integer that indicates whether this instance is earlier than, the same as, or later than the specified DateOnly value.
        /// </summary>
        /// <param name="value">A boxed object to compare, or null.</param>
        /// <returns>Less than zero if this instance is earlier than value. Greater than zero if this instance is later than value. Zero if this instance is the same as value.</returns>
        public int CompareTo(object? value)
        {
            if (value == null) return 1;
            if (value is not DateOnly dateOnly)
            {
                throw new ArgumentException(SR.Arg_MustBeDateOnly);
            }

            return CompareTo(dateOnly);
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the value of the specified DateOnly instance.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns>true if the value parameter equals the value of this instance; otherwise, false.</returns>
        public bool Equals(DateOnly value) => _dayNumber == value._dayNumber;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns>true if value is an instance of DateOnly and equals the value of this instance; otherwise, false.</returns>
        public override bool Equals(object? value) => value is DateOnly dateOnly && _dayNumber == dateOnly._dayNumber;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _dayNumber;

        private const ParseFlags ParseFlagsDateMask = ParseFlags.HaveHour | ParseFlags.HaveMinute | ParseFlags.HaveSecond | ParseFlags.HaveTime | ParseFlags.TimeZoneUsed |
                                                      ParseFlags.TimeZoneUtc | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern;

        /// <summary>
        /// Converts a memory span that contains string representation of a date to its DateOnly equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The memory span that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by provider and styles.</returns>
        public static DateOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            ParseFailureKind result = TryParseInternal(s, provider, style, out DateOnly dateOnly);
            if (result != ParseFailureKind.None)
            {
                ThrowOnError(result, s);
            }

            return dateOnly;
        }

        private const string OFormat = "yyyy'-'MM'-'dd";
        private const string RFormat = "ddd, dd MMM yyyy";

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="format">A span containing the characters that represent a format specifier that defines the required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by format, provider, and style.</returns>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            ParseFailureKind result = TryParseExactInternal(s, format, provider, style, out DateOnly dateOnly);

            if (result != ParseFailureKind.None)
            {
                ThrowOnError(result, s);
            }

            return dateOnly;
        }

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by format, provider, and style.</returns>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by format, provider, and style.</returns>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            ParseFailureKind result = TryParseExactInternal(s, formats, provider, style, out DateOnly dateOnly);
            if (result != ParseFailureKind.None)
            {
                ThrowOnError(result, s);
            }

            return dateOnly;
        }

        /// <summary>
        /// Converts a string that contains string representation of a date to its DateOnly equivalent by using the conventions of the current culture.
        /// </summary>
        /// <param name="s">The string that contains the string to parse.</param>
        /// <returns>An object that is equivalent to the date contained in s.</returns>
        public static DateOnly Parse(string s) => Parse(s, null, DateTimeStyles.None);

        /// <summary>
        /// Converts a string that contains string representation of a date to its DateOnly equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The string that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about s.</param>
        /// <param name="style">A bitwise combination of the enumeration values that indicates the style elements that can be present in s for the parse operation to succeed, and that defines how to interpret the parsed date. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by provider and styles.</returns>
        public static DateOnly Parse(string s, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse(s.AsSpan(), provider, style);
        }

        /// <summary>
        /// Converts the specified string representation of a date to its DateOnly equivalent using the specified format.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="format">A string that represent a format specifier that defines the required format of s.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by format.</returns>
        public static DateOnly ParseExact(string s, string format) => ParseExact(s, format, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a date to its DateOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="format">A string containing the characters that represent a format specifier that defines the required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of the enumeration values that provides additional information about s, about style elements that may be present in s, or about the conversion from s to a DateOnly value. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by format, provider, and style.</returns>
        public static DateOnly ParseExact(string s, string format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            if (format == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
            return ParseExact(s.AsSpan(), format.AsSpan(), provider, style);
        }

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by format, provider, and style.</returns>
        public static DateOnly ParseExact(string s, string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a date to its DateOnly equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <returns>An object that is equivalent to the date contained in s, as specified by format, provider, and style.</returns>
        public static DateOnly ParseExact(string s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return ParseExact(s.AsSpan(), formats, provider, style);
        }

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the date to convert.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out DateOnly result) => TryParse(s, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result) => TryParseInternal(s, provider, style, out result) == ParseFailureKind.None;

        private static ParseFailureKind TryParseInternal(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
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

            if ((dtResult.flags & ParseFlagsDateMask) != 0)
            {
                result = default;
                return ParseFailureKind.WrongParts;
            }

            result = new DateOnly(DayNumberFromDateTime(dtResult.parsedDate));
            return ParseFailureKind.None;
        }

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a date that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, out DateOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of one or more enumeration values that indicate the permitted format of s.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a date that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result) =>
                            TryParseExactInternal(s, format, provider, style, out result) == ParseFailureKind.None;
        private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
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

            if ((dtResult.flags & ParseFlagsDateMask) != 0)
            {
                result = default;
                return ParseFailureKind.WrongParts;
            }

            result = new DateOnly(DayNumberFromDateTime(dtResult.parsedDate));

            return ParseFailureKind.None;
        }

        /// <summary>
        /// Converts the specified char span of a date to its DateOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, string[] formats, out DateOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified char span of a date to its DateOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that defines how to interpret the parsed date. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result) =>
                            TryParseExactInternal(s, formats, provider, style, out result) == ParseFailureKind.None;

        private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
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
                if (DateTimeParse.TryParseExact(s, format, dtfiToUse, style, ref dtResult) && ((dtResult.flags & ParseFlagsDateMask) == 0))
                {
                    result = new DateOnly(DayNumberFromDateTime(dtResult.parsedDate));
                    return ParseFailureKind.None;
                }
            }

            result = default;
            return ParseFailureKind.FormatWithOriginalDateTime;
        }

        /// <summary>
        /// Converts the specified string representation of a date to its DateOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing the date to convert.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string s, out DateOnly result) => TryParse(s, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string representation of a date to its DateOnly equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is empty string, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParse(s.AsSpan(), provider, style, out result);
        }

        /// <summary>
        /// Converts the specified string representation of a date to its DateOnly equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a date that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(string s, string format, out DateOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a date to its DateOnly equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of one or more enumeration values that indicate the permitted format of s.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s is empty string, or does not contain a date that correspond to the pattern specified in format. This parameter is passed uninitialized.</param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(string s, string format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            if (s == null || format == null)
            {
                result = default;
                return false;
            }

            return TryParseExact(s.AsSpan(), format.AsSpan(), provider, style, out result);
        }

        /// <summary>
        /// Converts the specified string of a date to its DateOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing date to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(string s, string[] formats, out DateOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string of a date to its DateOnly equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing the date to parse.</param>
        /// <param name="formats">An array of allowable formats of s.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about s.</param>
        /// <param name="style">A bitwise combination of enumeration values that defines how to interpret the parsed date. A typical value to specify is None.</param>
        /// <param name="result">When this method returns, contains the DateOnly value equivalent to the date contained in s, if the conversion succeeded, or MinValue if the conversion failed. The conversion fails if the s parameter is Empty, or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParseExact(string s, string[] formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
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
                case ParseFailureKind.FormatWithOriginalDateTime: throw new FormatException(SR.Format(SR.Format_BadDateOnly, s.ToString()));
                case ParseFailureKind.FormatWithFormatSpecifier: throw new FormatException(SR.Argument_BadFormatSpecifier);
                default:
                    Debug.Assert(result == ParseFailureKind.WrongParts);
                    throw new FormatException(SR.Format(SR.Format_DateTimeOnlyContainsNoneDateParts, s.ToString(), nameof(DateOnly)));
            }
        }

        /// <summary>
        /// Converts the value of the current DateOnly object to its equivalent long date string representation.
        /// </summary>
        /// <returns>A string that contains the long date string representation of the current DateOnly object.</returns>
        public string ToLongDateString() => ToString("D");

        /// <summary>
        /// Converts the value of the current DateOnly object to its equivalent short date string representation.
        /// </summary>
        /// <returns>A string that contains the short date string representation of the current DateOnly object.</returns>
        public string ToShortDateString() => ToString();

        /// <summary>
        /// Converts the value of the current DateOnly object to its equivalent string representation using the formatting conventions of the current culture.
        /// The DateOnly object will be formatted in short form.
        /// </summary>
        /// <returns>A string that contains the short date string representation of the current DateOnly object.</returns>
        public override string ToString() => ToString("d");

        /// <summary>
        /// Converts the value of the current DateOnly object to its equivalent string representation using the specified format and the formatting conventions of the current culture.
        /// </summary>
        /// <param name="format">A standard or custom date format string.</param>
        /// <returns>A string representation of value of the current DateOnly object as specified by format.</returns>
        public string ToString(string? format) => ToString(format, null);

        /// <summary>
        /// Converts the value of the current DateOnly object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current DateOnly object as specified by provider.</returns>
        public string ToString(IFormatProvider? provider) => ToString("d", provider);

        /// <summary>
        /// Converts the value of the current DateOnly object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="format">A standard or custom date format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current DateOnly object as specified by format and provider.</returns>
        public string ToString(string? format, IFormatProvider? provider)
        {
            if (format == null || format.Length == 0)
            {
                format = "d";
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'o':
                    case 'O':
                        return string.Create(10, this, (destination, value) =>
                        {
                            bool b = DateTimeFormat.TryFormatDateOnlyO(value.Year, value.Month, value.Day, destination);
                            Debug.Assert(b);
                        });

                    case 'r':
                    case 'R':
                        return string.Create(16, this, (destination, value) =>
                        {
                            bool b = DateTimeFormat.TryFormatDateOnlyR(value.DayOfWeek, value.Year, value.Month, value.Day, destination);
                            Debug.Assert(b);
                        });

                    case 'm':
                    case 'M':
                    case 'd':
                    case 'D':
                    case 'y':
                    case 'Y':
                        return DateTimeFormat.Format(GetEquivalentDateTime(), format, provider);

                    default:
                        throw new FormatException(SR.Format_InvalidString);
                }
            }

            DateTimeFormat.IsValidCustomDateFormat(format.AsSpan(), throwOnError: true);
            return DateTimeFormat.Format(GetEquivalentDateTime(), format, provider);
        }

        /// <summary>
        /// Tries to format the value of the current DateOnly instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in destination.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for destination.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for destination.</param>
        /// <returns>true if the formatting was successful; otherwise, false.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
        {
            if (format.Length == 0)
            {
                format = "d";
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'o':
                    case 'O':
                        if (!DateTimeFormat.TryFormatDateOnlyO(Year, Month, Day, destination))
                        {
                            charsWritten = 0;
                            return false;
                        }
                        charsWritten = 10;
                        return true;

                    case 'r':
                    case 'R':

                        if (!DateTimeFormat.TryFormatDateOnlyR(DayOfWeek, Year, Month, Day, destination))
                        {
                            charsWritten = 0;
                            return false;
                        }
                        charsWritten = 16;
                        return true;

                    case 'm':
                    case 'M':
                    case 'd':
                    case 'D':
                    case 'y':
                    case 'Y':
                        return DateTimeFormat.TryFormat(GetEquivalentDateTime(), destination, out charsWritten, format, provider);

                    default:
                        charsWritten = 0;
                        return false;
                }
            }

            if (!DateTimeFormat.IsValidCustomDateFormat(format, throwOnError: false))
            {
                charsWritten = 0;
                return false;
            }

            return DateTimeFormat.TryFormat(GetEquivalentDateTime(), destination, out charsWritten, format, provider);
        }
    }
}
