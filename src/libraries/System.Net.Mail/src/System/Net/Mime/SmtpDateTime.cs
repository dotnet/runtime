// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Mime
{
    #region RFC2822 date time string format description
    // Format of Date Time string as described by RFC 2822 section 4.3 which obsoletes
    // some field formats that were allowed under RFC 822

    // date-time       =       [ day-of-week "," ] date FWS time [CFWS]
    // day-of-week     =       ([FWS] day-name) / obs-day-of-week
    // day-name        =       "Mon" / "Tue" / "Wed" / "Thu" / "Fri" / "Sat" / "Sun"
    // date            =       day month year
    // year            =       4*DIGIT / obs-year
    // month           =       (FWS month-name FWS) / obs-month
    // month-name      =       "Jan" / "Feb" / "Mar" / "Apr" / "May" / "Jun" / "Jul" / "Aug" /
    //                         "Sep" / "Oct" / "Nov" / "Dec"
    // day             =       ([FWS] 1*2DIGIT) / obs-day
    // time            =       time-of-day FWS zone
    // time-of-day     =       hour ":" minute [ ":" second ]
    // hour            =       2DIGIT / obs-hour
    // minute          =       2DIGIT / obs-minute
    // second          =       2DIGIT / obs-second
    // zone            =       (( "+" / "-" ) 4DIGIT) / obs-zone
    #endregion

    // stores a Date and a Time Zone.  These are parsed and formatted according to the
    // rules in RFC 2822 section 3.3.
    // This class is immutable
    internal sealed class SmtpDateTime
    {
        #region constants

        // use this when a time zone is unknown or is not supplied
        internal const string UnknownTimeZoneDefaultOffset = "-0000";
        internal const string UtcDefaultTimeZoneOffset = "+0000";
        internal const int OffsetLength = 5;

        // range for absolute value of minutes.  it is not necessary to include a max value for hours since
        // the two-digit value that is parsed can't exceed the max value of hours, which is 99
        internal const int MaxMinuteValue = 59;

        // possible valid values for a date string
        // these do NOT include the timezone
        internal const string DateFormatWithDayOfWeek = "ddd, dd MMM yyyy HH:mm:ss";
        internal const string DateFormatWithoutDayOfWeek = "dd MMM yyyy HH:mm:ss";
        internal const string DateFormatWithDayOfWeekAndNoSeconds = "ddd, dd MMM yyyy HH:mm";
        internal const string DateFormatWithoutDayOfWeekAndNoSeconds = "dd MMM yyyy HH:mm";

        #endregion

        #region static fields

        // array of all possible date time values
        // if a string matches any one of these it will be parsed correctly
        internal static readonly string[] s_validDateTimeFormats = new string[]
        {
            DateFormatWithDayOfWeek,
            DateFormatWithoutDayOfWeek,
            DateFormatWithDayOfWeekAndNoSeconds,
            DateFormatWithoutDayOfWeekAndNoSeconds
        };

        internal static readonly char[] s_allowedWhiteSpaceChars = new char[] { ' ', '\t' };

        internal static readonly Dictionary<string, TimeSpan> s_timeZoneOffsetLookup = InitializeShortHandLookups();

        // a TimeSpan must be between these two values in order for it to be within the range allowed
        // by RFC 2822
        internal const long TimeSpanMaxTicks = TimeSpan.TicksPerHour * 99 + TimeSpan.TicksPerMinute * 59;

        // allowed max values for each digit.  min value is always 0
        internal const int OffsetMaxValue = 9959;

        #endregion

        #region static initializers

        internal static Dictionary<string, TimeSpan> InitializeShortHandLookups()
        {
            var tempTimeZoneOffsetLookup = new Dictionary<string, TimeSpan>();

            // all well-known short hand time zone values and their semantic equivalents
            tempTimeZoneOffsetLookup.Add("UT", TimeSpan.Zero); // +0000
            tempTimeZoneOffsetLookup.Add("GMT", TimeSpan.Zero); // +0000
            tempTimeZoneOffsetLookup.Add("EDT", new TimeSpan(-4, 0, 0)); // -0400
            tempTimeZoneOffsetLookup.Add("EST", new TimeSpan(-5, 0, 0)); // -0500
            tempTimeZoneOffsetLookup.Add("CDT", new TimeSpan(-5, 0, 0)); // -0500
            tempTimeZoneOffsetLookup.Add("CST", new TimeSpan(-6, 0, 0)); // -0600
            tempTimeZoneOffsetLookup.Add("MDT", new TimeSpan(-6, 0, 0)); // -0600
            tempTimeZoneOffsetLookup.Add("MST", new TimeSpan(-7, 0, 0)); // -0700
            tempTimeZoneOffsetLookup.Add("PDT", new TimeSpan(-7, 0, 0)); // -0700
            tempTimeZoneOffsetLookup.Add("PST", new TimeSpan(-8, 0, 0)); // -0800
            return tempTimeZoneOffsetLookup;
        }

        #endregion

        #region private fields

        private readonly DateTime _date;
        private readonly TimeSpan _timeZone;

        // true if the time zone is unspecified i.e. -0000
        // the time zone will usually be specified
        private readonly bool _unknownTimeZone;

        #endregion

        #region constructors

        internal SmtpDateTime(DateTime value)
        {
            _date = value;

            switch (value.Kind)
            {
                case DateTimeKind.Local:
                    // GetUtcOffset takes local time zone information into account e.g. daylight savings time
                    TimeSpan localTimeZone = TimeZoneInfo.Local.GetUtcOffset(value);
                    _timeZone = ValidateAndGetSanitizedTimeSpan(localTimeZone);
                    break;

                case DateTimeKind.Unspecified:
                    _unknownTimeZone = true;
                    break;

                case DateTimeKind.Utc:
                    _timeZone = TimeSpan.Zero;
                    break;
            }
        }

        internal SmtpDateTime(string value)
        {
            string timeZoneOffset;
            _date = ParseValue(value, out timeZoneOffset);

            if (!TryParseTimeZoneString(timeZoneOffset, out _timeZone))
            {
                // time zone is unknown
                _unknownTimeZone = true;
            }
        }

        #endregion

        #region internal properties

        internal DateTime Date
        {
            get
            {
                if (_unknownTimeZone)
                {
                    return DateTime.SpecifyKind(_date, DateTimeKind.Unspecified);
                }
                else
                {
                    // DateTimeOffset will convert the value of this.date to the time as
                    // specified in this.timeZone
                    DateTimeOffset offset = new DateTimeOffset(_date, _timeZone);
                    return offset.LocalDateTime;
                }
            }
        }

#if DEBUG
        // this method is only called by test code
        internal string TimeZone => _unknownTimeZone ? UnknownTimeZoneDefaultOffset : TimeSpanToOffset(_timeZone);
#endif

        #endregion

        #region internals

        // outputs the RFC 2822 formatted date string including time zone
        public override string ToString() =>
            FormatDate(_date) + " " + (_unknownTimeZone ? UnknownTimeZoneDefaultOffset : TimeSpanToOffset(_timeZone));

        // returns true if the offset is of the form [+|-]dddd and
        // within the range 0000 to 9959
        internal void ValidateAndGetTimeZoneOffsetValues(string offset, out bool positive, out int hours, out int minutes)
        {
            Debug.Assert(!string.IsNullOrEmpty(offset), "violation of precondition: offset must not be null or empty");
            Debug.Assert(offset != UnknownTimeZoneDefaultOffset, "Violation of precondition: do not pass an unknown offset");
            Debug.Assert(offset.StartsWith('-') || offset.StartsWith('+'), "offset initial character was not a + or -");

            if (offset.Length != OffsetLength)
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }

            positive = offset.StartsWith('+');

            // TryParse will parse in base 10 by default.  do not allow any styles of input beyond the default
            // which is numeric values only
            if (!int.TryParse(offset.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out hours))
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }

            if (!int.TryParse(offset.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out minutes))
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }

            // we only explicitly validate the minutes.  they must be below 59
            // the hours are implicitly validated as a number formed from a string of length
            // 2 can only be <= 99
            if (minutes > MaxMinuteValue)
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }
        }

        // returns true if the time zone short hand is all alphabetical characters
        internal void ValidateTimeZoneShortHandValue(string value)
        {
            // time zones can't be empty
            Debug.Assert(!string.IsNullOrEmpty(value), "violation of precondition: offset must not be null or empty");

            // time zones must all be alphabetical characters
            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsLetter(value, i))
                {
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, value));
                }
            }
        }

        // formats a date only.  Does not include time zone
        internal string FormatDate(DateTime value) => value.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        // parses the date and time zone
        // postconditions:
        // return value is valid DateTime representation of the Date portion of data
        // timeZone is the portion of data which should contain the time zone data
        // timeZone is NOT evaluated by ParseValue
        internal DateTime ParseValue(string data, out string timeZone)
        {
            // check that there is something to parse
            if (string.IsNullOrEmpty(data))
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }

            // find the first occurrence of ':'
            // this tells us where the separator between hour and minute are
            int indexOfHourSeparator = data.IndexOf(':');

            // no ':' means invalid value
            if (indexOfHourSeparator == -1)
            {
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data));
            }

            // now we know where hours and minutes are separated.  The first whitespace after
            // that MUST be the separator between the time portion and the timezone portion
            // timezone may have additional spaces, characters, or comments after it but
            // this is ok since we'll parse that whole section later
            int indexOfTimeZoneSeparator = data.IndexOfAny(s_allowedWhiteSpaceChars, indexOfHourSeparator);

            if (indexOfTimeZoneSeparator == -1)
            {
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data));
            }

            // extract the time portion and remove all leading and trailing whitespace
            string date = data.AsSpan(0, indexOfTimeZoneSeparator).Trim().ToString();

            // attempt to parse the DateTime component.
            DateTime dateValue;
            if (!DateTime.TryParseExact(date, s_validDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateValue))
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }

            // kind property will be Unspecified since no timezone info was in the date string
            Debug.Assert(dateValue.Kind == DateTimeKind.Unspecified);

            // extract the second half of the string. This will start with at least one whitespace character.
            // Trim the string to remove these characters.
            string timeZoneString = data.AsSpan(indexOfTimeZoneSeparator).Trim().ToString();

            // find, if any, the first whitespace character after the timezone.
            // These will be CFWS and must be ignored. Remove them.
            int endOfTimeZoneOffset = timeZoneString.IndexOfAny(s_allowedWhiteSpaceChars);

            if (endOfTimeZoneOffset != -1)
            {
                timeZoneString = timeZoneString.Substring(0, endOfTimeZoneOffset);
            }

            if (string.IsNullOrEmpty(timeZoneString))
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }

            timeZone = timeZoneString;

            return dateValue;
        }

        // if this returns true, timeZone is the correct TimeSpan representation of the input
        // if it returns false then the time zone is unknown and so timeZone must be ignored
        internal bool TryParseTimeZoneString(string timeZoneString, out TimeSpan timeZone)
        {
            // see if the zone is the special unspecified case, a numeric offset, or a shorthand string
            if (timeZoneString == UnknownTimeZoneDefaultOffset)
            {
                // The inputed time zone is the special value "unknown", -0000
                timeZone = TimeSpan.Zero;
                return false;
            }
            else if ((timeZoneString[0] == '+' || timeZoneString[0] == '-'))
            {
                bool positive;
                int hours;
                int minutes;

                ValidateAndGetTimeZoneOffsetValues(timeZoneString, out positive, out hours, out minutes);

                // Apply the negative sign, if applicable, to whichever of hours or minutes is NOT 0.
                if (!positive)
                {
                    if (hours != 0)
                    {
                        hours *= -1;
                    }
                    else if (minutes != 0)
                    {
                        minutes *= -1;
                    }
                }

                timeZone = new TimeSpan(hours, minutes, 0);
                return true;
            }
            else
            {
                // not an offset so ensure that it contains no invalid characters
                ValidateTimeZoneShortHandValue(timeZoneString);

                // check if the shorthand value has a semantically equivalent offset
                return s_timeZoneOffsetLookup.TryGetValue(timeZoneString, out timeZone);
            }
        }

        internal TimeSpan ValidateAndGetSanitizedTimeSpan(TimeSpan span)
        {
            // sanitize the time span by removing the seconds and milliseconds.  Days are not handled here
            TimeSpan sanitizedTimeSpan = new TimeSpan(span.Days, span.Hours, span.Minutes, 0, 0);

            // validate range of time span
            if (Math.Abs(sanitizedTimeSpan.Ticks) > TimeSpanMaxTicks)
            {
                throw new FormatException(SR.MailDateInvalidFormat);
            }

            return sanitizedTimeSpan;
        }

        // precondition:  span must be sanitized and within a valid range
        internal string TimeSpanToOffset(TimeSpan span)
        {
            Debug.Assert(span.Seconds == 0, "Span had seconds value");
            Debug.Assert(span.Milliseconds == 0, "Span had milliseconds value");

            if (span.Ticks == 0)
            {
                return UtcDefaultTimeZoneOffset;
            }
            else
            {
                // get the total number of hours since TimeSpan.Hours won't go beyond 24
                // ensure that it's a whole number since the fractional part represents minutes
                uint hours = (uint)Math.Abs(Math.Floor(span.TotalHours));
                uint minutes = (uint)Math.Abs(span.Minutes);

                Debug.Assert((hours != 0) || (minutes != 0), "Input validation ensures hours or minutes isn't zero");

                string output = span.Ticks > 0 ? "+" : "-";

                // hours and minutes must be two digits
                if (hours < 10)
                {
                    output += "0";
                }

                output += hours.ToString();

                if (minutes < 10)
                {
                    output += "0";
                }

                output += minutes.ToString();

                return output;
            }
        }

        #endregion
    }
}
