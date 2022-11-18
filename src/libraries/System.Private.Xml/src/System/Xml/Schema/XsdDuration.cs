// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System;
using System.Diagnostics;
using System.Text;

namespace System.Xml.Schema
{
    /// <summary>
    /// This structure holds components of an Xsd Duration.  It is used internally to support Xsd durations without loss
    /// of fidelity.  XsdDuration structures are immutable once they've been created.
    /// </summary>
    internal struct XsdDuration
    {
        private int _years;
        private int _months;
        private int _days;
        private int _hours;
        private int _minutes;
        private int _seconds;
        private uint _nanoseconds;       // High bit is used to indicate whether duration is negative

        private const uint NegativeBit = 0x80000000;

        private enum Parts
        {
            HasNone = 0,
            HasYears = 1,
            HasMonths = 2,
            HasDays = 4,
            HasHours = 8,
            HasMinutes = 16,
            HasSeconds = 32,
        }

        public enum DurationType
        {
            Duration,
            YearMonthDuration,
            DayTimeDuration,
        }

        /// <summary>
        /// Construct an XsdDuration from component parts.
        /// </summary>
        public XsdDuration(bool isNegative, int years, int months, int days, int hours, int minutes, int seconds, int nanoseconds)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (months < 0) throw new ArgumentOutOfRangeException(nameof(months));
            if (days < 0) throw new ArgumentOutOfRangeException(nameof(days));
            if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours));
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (nanoseconds < 0 || nanoseconds > 999999999) throw new ArgumentOutOfRangeException(nameof(nanoseconds));

            _years = years;
            _months = months;
            _days = days;
            _hours = hours;
            _minutes = minutes;
            _seconds = seconds;
            _nanoseconds = (uint)nanoseconds;

            if (isNegative)
                _nanoseconds |= NegativeBit;
        }

        /// <summary>
        /// Construct an XsdDuration from a TimeSpan value.
        /// </summary>
        public XsdDuration(TimeSpan timeSpan) : this(timeSpan, DurationType.Duration)
        {
        }

        /// <summary>
        /// Construct an XsdDuration from a TimeSpan value that represents an xsd:duration, an xdt:dayTimeDuration, or
        /// an xdt:yearMonthDuration.
        /// </summary>
        public XsdDuration(TimeSpan timeSpan, DurationType durationType)
        {
            long ticks = timeSpan.Ticks;
            ulong ticksPos;
            bool isNegative;

            if (ticks < 0)
            {
                // Note that (ulong) -Int64.MinValue = Int64.MaxValue + 1, which is what we want for that special case
                isNegative = true;
                ticksPos = unchecked((ulong)-ticks);
            }
            else
            {
                isNegative = false;
                ticksPos = (ulong)ticks;
            }

            if (durationType == DurationType.YearMonthDuration)
            {
                int years = (int)(ticksPos / ((ulong)TimeSpan.TicksPerDay * 365));
                int months = (int)((ticksPos % ((ulong)TimeSpan.TicksPerDay * 365)) / ((ulong)TimeSpan.TicksPerDay * 30));

                if (months == 12)
                {
                    // If remaining days >= 360 and < 365, then round off to year
                    years++;
                    months = 0;
                }

                this = new XsdDuration(isNegative, years, months, 0, 0, 0, 0, 0);
            }
            else
            {
                Debug.Assert(durationType == DurationType.Duration || durationType == DurationType.DayTimeDuration);

                // Tick count is expressed in 100 nanosecond intervals
                _nanoseconds = (uint)(ticksPos % 10000000) * 100;
                if (isNegative)
                    _nanoseconds |= NegativeBit;

                _years = 0;
                _months = 0;
                _days = (int)(ticksPos / (ulong)TimeSpan.TicksPerDay);
                _hours = (int)((ticksPos / (ulong)TimeSpan.TicksPerHour) % 24);
                _minutes = (int)((ticksPos / (ulong)TimeSpan.TicksPerMinute) % 60);
                _seconds = (int)((ticksPos / (ulong)TimeSpan.TicksPerSecond) % 60);
            }
        }

        /// <summary>
        /// Constructs an XsdDuration from a string in the xsd:duration format.  Components are stored with loss
        /// of fidelity (except in the case of overflow).
        /// </summary>
        public XsdDuration(string s) : this(s, DurationType.Duration)
        {
        }

        /// <summary>
        /// Constructs an XsdDuration from a string in the xsd:duration format.  Components are stored without loss
        /// of fidelity (except in the case of overflow).
        /// </summary>
        public XsdDuration(string s, DurationType durationType)
        {
            XsdDuration result;
            Exception? exception = TryParse(s, durationType, out result);
            if (exception != null)
            {
                throw exception;
            }

            _years = result.Years;
            _months = result.Months;
            _days = result.Days;
            _hours = result.Hours;
            _minutes = result.Minutes;
            _seconds = result.Seconds;
            _nanoseconds = (uint)result.Nanoseconds;
            if (result.IsNegative)
            {
                _nanoseconds |= NegativeBit;
            }

            return;
        }

        /// <summary>
        /// Return true if this duration is negative.
        /// </summary>
        public bool IsNegative
        {
            get { return (_nanoseconds & NegativeBit) != 0; }
        }

        /// <summary>
        /// Return number of years in this duration (stored in 31 bits).
        /// </summary>
        public int Years
        {
            get { return _years; }
        }

        /// <summary>
        /// Return number of months in this duration (stored in 31 bits).
        /// </summary>
        public int Months
        {
            get { return _months; }
        }

        /// <summary>
        /// Return number of days in this duration (stored in 31 bits).
        /// </summary>
        public int Days
        {
            get { return _days; }
        }

        /// <summary>
        /// Return number of hours in this duration (stored in 31 bits).
        /// </summary>
        public int Hours
        {
            get { return _hours; }
        }

        /// <summary>
        /// Return number of minutes in this duration (stored in 31 bits).
        /// </summary>
        public int Minutes
        {
            get { return _minutes; }
        }

        /// <summary>
        /// Return number of seconds in this duration (stored in 31 bits).
        /// </summary>
        public int Seconds
        {
            get { return _seconds; }
        }

        /// <summary>
        /// Return number of nanoseconds in this duration.
        /// </summary>
        public int Nanoseconds
        {
            get { return (int)(_nanoseconds & ~NegativeBit); }
        }

        /// <summary>
        /// Internal helper method that converts an Xsd duration to a TimeSpan value.  This code uses the estimate
        /// that there are 365 days in the year and 30 days in a month.
        /// </summary>
        public TimeSpan ToTimeSpan()
        {
            return ToTimeSpan(DurationType.Duration);
        }

        /// <summary>
        /// Internal helper method that converts an Xsd duration to a TimeSpan value.  This code uses the estimate
        /// that there are 365 days in the year and 30 days in a month.
        /// </summary>
        public TimeSpan ToTimeSpan(DurationType durationType)
        {
            TimeSpan result;
            Exception? exception = TryToTimeSpan(durationType, out result);
            if (exception != null)
            {
                throw exception;
            }

            return result;
        }

        internal Exception? TryToTimeSpan(out TimeSpan result)
        {
            return TryToTimeSpan(DurationType.Duration, out result);
        }

        internal Exception? TryToTimeSpan(DurationType durationType, out TimeSpan result)
        {
            Exception? exception;
            ulong ticks = 0;

            // Throw error if result cannot fit into a long
            try
            {
                checked
                {
                    // Discard year and month parts if constructing TimeSpan for DayTimeDuration
                    if (durationType != DurationType.DayTimeDuration)
                    {
                        ticks += ((ulong)_years + (ulong)_months / 12) * 365;
                        ticks += ((ulong)_months % 12) * 30;
                    }

                    // Discard day and time parts if constructing TimeSpan for YearMonthDuration
                    if (durationType != DurationType.YearMonthDuration)
                    {
                        ticks += (ulong)_days;

                        ticks *= 24;
                        ticks += (ulong)_hours;

                        ticks *= 60;
                        ticks += (ulong)_minutes;

                        ticks *= 60;
                        ticks += (ulong)_seconds;

                        // Tick count interval is in 100 nanosecond intervals (7 digits)
                        ticks *= (ulong)TimeSpan.TicksPerSecond;
                        ticks += (ulong)Nanoseconds / 100;
                    }
                    else
                    {
                        // Multiply YearMonth duration by number of ticks per day
                        ticks *= (ulong)TimeSpan.TicksPerDay;
                    }

                    if (IsNegative)
                    {
                        // Handle special case of Int64.MaxValue + 1 before negation, since it would otherwise overflow
                        if (ticks == (ulong)long.MaxValue + 1)
                        {
                            result = new TimeSpan(long.MinValue);
                        }
                        else
                        {
                            result = new TimeSpan(-((long)ticks));
                        }
                    }
                    else
                    {
                        result = new TimeSpan((long)ticks);
                    }
                    return null;
                }
            }
            catch (OverflowException)
            {
                result = TimeSpan.MinValue;
                exception = new OverflowException(SR.Format(SR.XmlConvert_Overflow, durationType, "TimeSpan"));
            }
            return exception;
        }

        /// <summary>
        /// Return the string representation of this Xsd duration.
        /// </summary>
        public override string ToString()
        {
            return ToString(DurationType.Duration);
        }

        /// <summary>
        /// Return the string representation according to xsd:duration rules, xdt:dayTimeDuration rules, or
        /// xdt:yearMonthDuration rules.
        /// </summary>
        internal string ToString(DurationType durationType)
        {
            var vsb = new ValueStringBuilder(stackalloc char[20]);
            int nanoseconds, digit, zeroIdx, len;

            if (IsNegative)
                vsb.Append('-');

            vsb.Append('P');

            if (durationType != DurationType.DayTimeDuration)
            {
                if (_years != 0)
                {
                    vsb.AppendSpanFormattable(_years, null, CultureInfo.InvariantCulture);
                    vsb.Append('Y');
                }

                if (_months != 0)
                {
                    vsb.AppendSpanFormattable(_months, null, CultureInfo.InvariantCulture);
                    vsb.Append('M');
                }
            }

            if (durationType != DurationType.YearMonthDuration)
            {
                if (_days != 0)
                {
                    vsb.AppendSpanFormattable(_days, null, CultureInfo.InvariantCulture);
                    vsb.Append('D');
                }

                if (_hours != 0 || _minutes != 0 || _seconds != 0 || Nanoseconds != 0)
                {
                    vsb.Append('T');
                    if (_hours != 0)
                    {
                        vsb.AppendSpanFormattable(_hours, null, CultureInfo.InvariantCulture);
                        vsb.Append('H');
                    }

                    if (_minutes != 0)
                    {
                        vsb.AppendSpanFormattable(_minutes, null, CultureInfo.InvariantCulture);
                        vsb.Append('M');
                    }

                    nanoseconds = Nanoseconds;
                    if (_seconds != 0 || nanoseconds != 0)
                    {
                        vsb.AppendSpanFormattable(_seconds, null, CultureInfo.InvariantCulture);
                        if (nanoseconds != 0)
                        {
                            vsb.Append('.');

                            len = vsb.Length;
                            Span<char> tmpSpan = stackalloc char[9];
                            zeroIdx = len + 8;

                            for (int idx = zeroIdx; idx >= len; idx--)
                            {
                                digit = nanoseconds % 10;
                                tmpSpan[idx - len] = (char)(digit + '0');

                                if (zeroIdx == idx && digit == 0)
                                    zeroIdx--;

                                nanoseconds /= 10;
                            }

                            vsb.EnsureCapacity(zeroIdx + 1);
                            vsb.Append(tmpSpan.Slice(0, zeroIdx - len + 1));
                        }
                        vsb.Append('S');
                    }
                }

                // Zero is represented as "PT0S"
                if (vsb[vsb.Length - 1] == 'P')
                    vsb.Append("T0S");
            }
            else
            {
                // Zero is represented as "T0M"
                if (vsb[vsb.Length - 1] == 'P')
                    vsb.Append("0M");
            }

            return vsb.ToString();
        }

        internal static Exception? TryParse(string s, out XsdDuration result)
        {
            return TryParse(s, DurationType.Duration, out result);
        }

        internal static Exception? TryParse(string s, DurationType durationType, out XsdDuration result)
        {
            string? errorCode;
            int length;
            int value, pos, numDigits;
            Parts parts = Parts.HasNone;

            result = default;

            s = s.Trim();
            length = s.Length;

            pos = 0;

            if (pos >= length) goto InvalidFormat;

            if (s[pos] == '-')
            {
                pos++;
                result._nanoseconds = NegativeBit;
            }
            else
            {
                result._nanoseconds = 0;
            }

            if (pos >= length) goto InvalidFormat;

            if (s[pos++] != 'P') goto InvalidFormat;

            errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
            if (errorCode != null) goto Error;

            if (pos >= length) goto InvalidFormat;

            if (s[pos] == 'Y')
            {
                if (numDigits == 0) goto InvalidFormat;

                parts |= Parts.HasYears;
                result._years = value;
                if (++pos == length) goto Done;

                errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'M')
            {
                if (numDigits == 0) goto InvalidFormat;

                parts |= Parts.HasMonths;
                result._months = value;
                if (++pos == length) goto Done;

                errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'D')
            {
                if (numDigits == 0) goto InvalidFormat;

                parts |= Parts.HasDays;
                result._days = value;
                if (++pos == length) goto Done;

                errorCode = TryParseDigits(s, ref pos, false, out _, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'T')
            {
                if (numDigits != 0) goto InvalidFormat;

                pos++;
                errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;

                if (s[pos] == 'H')
                {
                    if (numDigits == 0) goto InvalidFormat;

                    parts |= Parts.HasHours;
                    result._hours = value;
                    if (++pos == length) goto Done;

                    errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                    if (errorCode != null) goto Error;

                    if (pos >= length) goto InvalidFormat;
                }

                if (s[pos] == 'M')
                {
                    if (numDigits == 0) goto InvalidFormat;

                    parts |= Parts.HasMinutes;
                    result._minutes = value;
                    if (++pos == length) goto Done;

                    errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                    if (errorCode != null) goto Error;

                    if (pos >= length) goto InvalidFormat;
                }

                if (s[pos] == '.')
                {
                    pos++;

                    parts |= Parts.HasSeconds;
                    result._seconds = value;

                    errorCode = TryParseDigits(s, ref pos, true, out value, out numDigits);
                    if (errorCode != null) goto Error;

                    if (numDigits == 0)
                    { //If there are no digits after the decimal point, assume 0
                        value = 0;
                    }
                    // Normalize to nanosecond intervals
                    for (; numDigits > 9; numDigits--)
                        value /= 10;

                    for (; numDigits < 9; numDigits++)
                        value *= 10;

                    result._nanoseconds |= (uint)value;

                    if (pos >= length) goto InvalidFormat;

                    if (s[pos] != 'S') goto InvalidFormat;
                    if (++pos == length) goto Done;
                }
                else if (s[pos] == 'S')
                {
                    if (numDigits == 0) goto InvalidFormat;

                    parts |= Parts.HasSeconds;
                    result._seconds = value;
                    if (++pos == length) goto Done;
                }
            }

            // Duration cannot end with digits
            if (numDigits != 0) goto InvalidFormat;

            // No further characters are allowed
            if (pos != length) goto InvalidFormat;

            Done:
            // At least one part must be defined
            if (parts == Parts.HasNone) goto InvalidFormat;

            if (durationType == DurationType.DayTimeDuration)
            {
                if ((parts & (Parts.HasYears | Parts.HasMonths)) != 0)
                    goto InvalidFormat;
            }
            else if (durationType == DurationType.YearMonthDuration)
            {
                if ((parts & ~(XsdDuration.Parts.HasYears | XsdDuration.Parts.HasMonths)) != 0)
                    goto InvalidFormat;
            }

            return null;

        InvalidFormat:
            return new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, durationType));

        Error:
            return new OverflowException(SR.Format(SR.XmlConvert_Overflow, s, durationType));
        }

        /// Helper method that constructs an integer from leading digits starting at s[offset].  "offset" is
        /// updated to contain an offset just beyond the last digit.  The number of digits consumed is returned in
        /// cntDigits.  The integer is returned (0 if no digits).  If the digits cannot fit into an Int32:
        ///   1. If eatDigits is true, then additional digits will be silently discarded (don't count towards numDigits)
        ///   2. If eatDigits is false, an overflow exception is thrown
        private static string? TryParseDigits(string s, ref int offset, bool eatDigits, out int result, out int numDigits)
        {
            int offsetStart = offset;
            int offsetEnd = s.Length;
            int digit;

            result = 0;
            numDigits = 0;

            while (offset < offsetEnd && char.IsAsciiDigit(s[offset]))
            {
                digit = s[offset] - '0';

                if (result > (int.MaxValue - digit) / 10)
                {
                    if (!eatDigits)
                    {
                        return SR.XmlConvert_Overflow;
                    }

                    // Skip past any remaining digits
                    numDigits = offset - offsetStart;

                    while (offset < offsetEnd && char.IsAsciiDigit(s[offset]))
                    {
                        offset++;
                    }

                    return null;
                }

                result = result * 10 + digit;
                offset++;
            }

            numDigits = offset - offsetStart;
            return null;
        }
    }
}
