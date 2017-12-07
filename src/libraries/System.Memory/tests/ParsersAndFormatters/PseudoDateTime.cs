// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text.Tests
{
    //
    // Used to model DateTime and DateTimeOffsets that have "illegal" values (e.g. Jan 32) This is used to generate a subset of DateTime 
    // and DateTimeOffset ParserTestData objects.
    //
    public sealed class PseudoDateTime
    {
        public PseudoDateTime(int year, int month, int day, int hour, int minute, int second, bool expectSuccess)
            : this(year, month, day, hour, minute, second, fraction: 0, offsetNegative: false, offsetHours: 0, offsetMinutes: 0, expectSuccess: expectSuccess)
        {
        }

        public PseudoDateTime(int year, int month, int day, int hour, int minute, int second, int fraction, bool offsetNegative, int offsetHours, int offsetMinutes, bool expectSuccess)
        {
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Second = second;
            Fraction = fraction;
            OffsetNegative = offsetNegative;
            OffsetHours = offsetHours;
            OffsetMinutes = offsetMinutes;

            ExpectSuccess = expectSuccess;
        }

        public string DefaultString
        {
            get
            {
                if (Fraction != 0)
                    return null;

                return Month.ToString("D2") + "/" + Day.ToString("D2") + "/" + Year.ToString("D4") +
                    " " + Hour.ToString("D2") + ":" + Minute.ToString("D2") + ":" + Second.ToString("D2") +
                    " " + (OffsetNegative ? "-" : "+") + OffsetHours.ToString("D2") + ":" + OffsetMinutes.ToString("D2");
            }
        }

        public string GFormatString
        {
            get
            {
                if (Fraction != 0)
                    return null;
                if (OffsetHours != 0 || OffsetMinutes != 0)
                    return null;

                return Month.ToString("D2") + "/" + Day.ToString("D2") + "/" + Year.ToString("D4") +
                    " " + Hour.ToString("D2") + ":" + Minute.ToString("D2") + ":" + Second.ToString("D2");
            }
        }

        public string RFormatString
        {
            get
            {
                //Tue, 00 Jan 2017 08:08:05 GMT
                if (Fraction != 0)
                    return null;
                if (OffsetHours != 0 || OffsetMinutes != 0)
                    return null;

                string dayAbbreviation;
                if (ExpectSuccess)
                {
                    TimeSpan offset = new TimeSpan(hours: OffsetHours, minutes: OffsetMinutes, seconds: 0);
                    if (OffsetNegative)
                        offset = -offset;
                    DateTimeOffset dto = new DateTimeOffset(year: Year, month: Month, day: Day, hour: Hour, minute: Minute, second: Second, offset: offset);
                    dayAbbreviation = s_dayAbbreviations[(int)(dto.DayOfWeek)];
                }
                else
                {
                    // Pick something legal here as we're expecting code coverage of an error case and we don't want a bad day abbreviation to bypass that.
                    dayAbbreviation = "Sun";
                }

                string monthAbbrevation;
                if (Month >= 1 && Month <= 12)
                {
                    monthAbbrevation = s_monthAbbreviations[Month - 1];
                }
                else
                {
                    // Pick something legal here as we're expecting code coverage of an error case and we don't want a bad day abbreviation to bypass that.
                    monthAbbrevation = "Jan";
                }

                return dayAbbreviation + ", " + Day.ToString("D2") + " " + monthAbbrevation + " " + Year.ToString("D4") + " "
                    + Hour.ToString("D2") + ":" + Minute.ToString("D2") + ":" + Second.ToString("D2") + " "
                    + "GMT";
            }
        }

        public string LFormatString => RFormatString?.ToLowerInvariant();

        public string OFormatStringNoOffset
        {
            get
            {
                if (OffsetHours != 0 || OffsetMinutes != 0)
                    return null;

                return Year.ToString("D4") + "-" + Month.ToString("D2") + "-" + Day.ToString("D2") + "T"
                    + Hour.ToString("D2") + ":" + Minute.ToString("D2") + ":" + Second.ToString("D2")
                    + "." + Fraction.ToString("D7");
            }
        }

        public string OFormatStringZ => (OffsetHours != 0 || OffsetMinutes != 0) ? null : OFormatStringNoOffset + "Z";
        public string OFormatStringOffset
        {
            get
            {
                return Year.ToString("D4") + "-" + Month.ToString("D2") + "-" + Day.ToString("D2") + "T"
                    + Hour.ToString("D2") + ":" + Minute.ToString("D2") + ":" + Second.ToString("D2")
                    + "." + Fraction.ToString("D7")
                    + (OffsetNegative ? "-" : "+") + OffsetHours.ToString("D2") + ":" + OffsetMinutes.ToString("D2");
            }
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public int Hour { get; }
        public int Minute { get; }
        public int Second { get; }
        public int Fraction { get; }
        public bool OffsetNegative { get; }
        public int OffsetHours { get; }
        public int OffsetMinutes { get; }
        public bool ExpectSuccess { get; }

        private static readonly string[] s_dayAbbreviations = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private static readonly string[] s_monthAbbreviations = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    }
}
