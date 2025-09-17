// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema.DateAndTime.Helpers;
using System.Xml.Schema.DateAndTime.Specifications;

namespace System.Xml.Schema.DateAndTime.Converters
{
    internal static class DateAndTimeConverter
    {
        /// <summary>
        /// Maximum number of fraction digits.
        /// </summary>
        public const short MaxFractionDigits = 7;

        public static readonly int s_Lz_ = "-".Length;
        public static readonly int s_lz_zz = "-zz".Length;
        public static readonly int s_lz_zz_ = "-zz:".Length;
        public static readonly int s_lz_zz_zz = "-zz:zz".Length;
        public static readonly int s_lzHH = "HH".Length;
        public static readonly int s_lzHH_ = "HH:".Length;
        public static readonly int s_lzHH_mm = "HH:mm".Length;
        public static readonly int s_lzHH_mm_ = "HH:mm:".Length;
        public static readonly int s_lzHH_mm_ss = "HH:mm:ss".Length;
        public static readonly int s_lzyyyy = "yyyy".Length;
        public static readonly int s_lzyyyy_ = "yyyy-".Length;
        public static readonly int s_lzyyyy_MM = "yyyy-MM".Length;
        public static readonly int s_lzyyyy_MM_ = "yyyy-MM-".Length;
        public static readonly int s_lzyyyy_MM_dd = "yyyy-MM-dd".Length;
        private static readonly int s_Lz__ = "--".Length;
        private static readonly int s_Lz___ = "---".Length;
        private static readonly int s_lz___dd = "---dd".Length;
        private static readonly int s_lz__mm = "--MM".Length;
        private static readonly int s_lz__mm_ = "--MM-".Length;
        private static readonly int s_lz__mm__ = "--MM--".Length;
        private static readonly int s_lz__mm_dd = "--MM-dd".Length;
        private static readonly int s_lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;

        private static ReadOnlySpan<int> Power10 => [-1, 10, 100, 1000, 10000, 100000, 1000000];

        public static bool TryParse(string text, XsdDateTimeFlags kinds, out DateAndTimeInfo parsedValue)
        {
            // Skip leading whitespace
            int start = 0;
            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            if (TryParseAsDateTime(text, kinds, start, out parsedValue))
            {
                return true;
            }

            if (TryParseAsDate(text, kinds, start, out parsedValue))
            {
                return true;
            }

            if (TryParseAsTime(text, kinds, start, out parsedValue))
            {
                return true;
            }

            if (TryParseAsXdrTimeNoTz(text, kinds, start, out parsedValue))
            {
                return true;
            }

            if (TryParseAsGYearOrGYearMonth(text, kinds, start, out parsedValue))
            {
                return true;
            }

            if (TryParseAsGMonthOrGMonthDay(text, kinds, start, out parsedValue))
            {
                return true;
            }

            if (TryParseAsGDay(text, kinds, start, out parsedValue))
            {
                return true;
            }

            return false;
        }

        public static bool TryParse(string text, out DateInfo parsedValue)
        {
            int start = 0;
            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            if (TryParseAsDate(text, XsdDateTimeFlags.Date, start, out DateAndTimeInfo rawParsedValue))
            {
                parsedValue = rawParsedValue.Date;
                return true;
            }

            parsedValue = default;
            return false;
        }

        private static bool TryParseAsDate(
            string text,
            XsdDateTimeFlags kinds,
            int start,
            out DateAndTimeInfo parsedValue)
        {
            DateInfo date;
            int? zoneHour, zoneMinute;
            XsdDateTimeKind? kind;

            if (Test(kinds, XsdDateTimeFlags.Date) && TryParseDate(text, start, out date))
            {
                if (TryParseZoneAndWhitespace(text, start + s_lzyyyy_MM_dd, out kind, out zoneHour, out zoneMinute))
                {
                    parsedValue = new DateAndTimeInfo(date, 0, 0, kind.Value, 0, 0, DateTimeTypeCode.Date, zoneHour.Value, zoneMinute.Value);
                    return true;
                }

                if (ParseChar(text, start + s_lzyyyy_MM_dd, 'T')
                    && TryParseTimeAndZoneAndWhitespace(text, start + s_lzyyyy_MM_ddT, out _, out _, out _, out _, out kind, out zoneHour, out zoneMinute))
                {
                    parsedValue = new DateAndTimeInfo(date, 0, 0, kind.Value, 0, 0, DateTimeTypeCode.Date, zoneHour.Value, zoneMinute.Value);
                    return true;
                }
            }

            parsedValue = default;
            return false;
        }

        private static bool TryParseAsDateTime(
            string text,
            XsdDateTimeFlags kinds,
            int start,
            out DateAndTimeInfo parsedValue)
        {
            const XsdDateTimeFlags dateTimeVariants = XsdDateTimeFlags.DateTime
                | XsdDateTimeFlags.XdrDateTime
                | XsdDateTimeFlags.XdrDateTimeNoTz;

            DateInfo date;
            int? hour, minute, second, fraction, zoneHour, zoneMinute;
            XsdDateTimeKind? kind;

            // Choose format starting from the most common and trying not to reparse the same thing too many times
            if (Test(kinds, dateTimeVariants) && TryParseDate(text, start, out date))
            {
                if (Test(kinds, XsdDateTimeFlags.DateTime) &&
                    ParseChar(text, start + s_lzyyyy_MM_dd, 'T') &&
                    TryParseTimeAndZoneAndWhitespace(text, start + s_lzyyyy_MM_ddT, out hour, out minute, out second, out fraction, out kind, out zoneHour, out zoneMinute))
                {
                    parsedValue = new DateAndTimeInfo(date, fraction.Value, hour.Value, kind.Value, minute.Value, second.Value, DateTimeTypeCode.DateTime, zoneHour.Value, zoneMinute.Value);
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.XdrDateTime))
                {
                    if (TryParseZoneAndWhitespace(text, start + s_lzyyyy_MM_dd, out kind, out zoneHour, out zoneMinute))
                    {
                        parsedValue = new DateAndTimeInfo(date, 0, 0, kind.Value, 0, 0, DateTimeTypeCode.XdrDateTime, zoneHour.Value, zoneMinute.Value);
                        return true;
                    }

                    if (ParseChar(text, start + s_lzyyyy_MM_dd, 'T')
                        && TryParseTimeAndZoneAndWhitespace(text, start + s_lzyyyy_MM_ddT, out hour, out minute, out second, out fraction, out kind, out zoneHour, out zoneMinute))
                    {
                        parsedValue = new DateAndTimeInfo(date, fraction.Value, hour.Value, kind.Value, minute.Value, second.Value, DateTimeTypeCode.XdrDateTime, zoneHour.Value, zoneMinute.Value);
                        return true;
                    }
                }

                if (Test(kinds, XsdDateTimeFlags.XdrDateTimeNoTz))
                {
                    if (ParseChar(text, start + s_lzyyyy_MM_dd, 'T'))
                    {
                        if (ParseTimeAndWhitespace(text, start + s_lzyyyy_MM_ddT, out hour, out minute, out second, out fraction))
                        {
                            parsedValue = new DateAndTimeInfo(date, fraction.Value, hour.Value, XsdDateTimeKind.Unspecified, minute.Value, second.Value, DateTimeTypeCode.XdrDateTime, 0, 0);
                            return true;
                        }
                    }
                    else
                    {
                        parsedValue = new DateAndTimeInfo(date, 0, 0, XsdDateTimeKind.Unspecified, 0, 0, DateTimeTypeCode.XdrDateTime, 0, 0);
                        return true;
                    }
                }
            }

            parsedValue = default;
            return false;
        }

        private static bool TryParseAsTime(
            string text,
            XsdDateTimeFlags kinds,
            int start,
            out DateAndTimeInfo parsedValue)
        {
            if (Test(kinds, XsdDateTimeFlags.Time) && TryParseTimeAndZoneAndWhitespace(text, start, out int? hour, out int? minute, out int? second, out int? fraction, out XsdDateTimeKind? kind, out int? zoneHour, out int? zoneMinute))
            {
                parsedValue = new DateAndTimeInfo(DateInfo.DefaultValue, fraction.Value, hour.Value, kind.Value, minute.Value, second.Value, DateTimeTypeCode.Time, zoneHour.Value, zoneMinute.Value);
                return true;
            }

            parsedValue = default;
            return false;
        }

        private static bool TryParseAsXdrTimeNoTz(
            string text,
            XsdDateTimeFlags kinds,
            int start,
            out DateAndTimeInfo parsedValue)
        {
            if (Test(kinds, XsdDateTimeFlags.XdrTimeNoTz) && ParseTimeAndWhitespace(text, start, out int? hour, out int? minute, out int? second, out int? fraction))
            {
                parsedValue = new DateAndTimeInfo(DateInfo.DefaultValue, fraction.Value, hour.Value, XsdDateTimeKind.Unspecified, minute.Value, second.Value, DateTimeTypeCode.Time, default, default);
                return true;
            }

            parsedValue = default;
            return false;
        }

        private static bool TryParseAsGYearOrGYearMonth(
            string text,
            XsdDateTimeFlags kinds,
            int start,
            out DateAndTimeInfo parsedValue)
        {
            int? month, year, zoneHour, zoneMinute;
            DateInfo date;
            XsdDateTimeKind? kind;

            if (Test(kinds, XsdDateTimeFlags.GYearMonth | XsdDateTimeFlags.GYear) && ParseFourDigits(text, start, out year) && 1 <= year)
            {
                if (Test(kinds, XsdDateTimeFlags.GYearMonth) &&
                    ParseChar(text, start + s_lzyyyy, '-') &&
                    ParseTwoDigits(text, start + s_lzyyyy_, out month) &&
                    1 <= month &&
                    month <= 12 &&
                    TryParseZoneAndWhitespace(text, start + s_lzyyyy_MM, out kind, out zoneHour, out zoneMinute))
                {
                    date = new DateInfo(DateInfo.FirstDay, month.Value, year.Value);
                    parsedValue = new DateAndTimeInfo(date, default, default, kind.Value, default, default, DateTimeTypeCode.GYearMonth, zoneHour.Value, zoneMinute.Value);
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.GYear) && TryParseZoneAndWhitespace(text, start + s_lzyyyy, out kind, out zoneHour, out zoneMinute))
                {
                    date = new DateInfo(DateInfo.FirstDay, DateInfo.FirstMonth, year.Value);
                    parsedValue = new DateAndTimeInfo(date, default, default, kind.Value, default, default, DateTimeTypeCode.GYear, zoneHour.Value, zoneMinute.Value);
                    return true;
                }
            }

            parsedValue = new DateAndTimeInfo(default, default, default, XsdDateTimeKind.Unspecified, default, default, default, default, default);
            return false;
        }

        private static bool TryParseAsGMonthOrGMonthDay(
            string text,
            XsdDateTimeFlags kinds,
            int start,
            out DateAndTimeInfo parsedValue)
        {
            int? day, month, zoneHour, zoneMinute;
            DateInfo date;
            XsdDateTimeKind? kind;

            if (Test(kinds, XsdDateTimeFlags.GMonthDay | XsdDateTimeFlags.GMonth) &&
                ParseChar(text, start, '-') &&
                ParseChar(text, start + s_Lz_, '-') &&
                ParseTwoDigits(text, start + s_Lz__, out month) && 1 <= month && month <= 12)
            {
                if (Test(kinds, XsdDateTimeFlags.GMonthDay) &&
                    ParseChar(text, start + s_lz__mm, '-') &&
                    ParseTwoDigits(text, start + s_lz__mm_, out day) && 1 <= day && day <= DateTime.DaysInMonth(DateInfo.LeapYear, month.Value) &&
                    TryParseZoneAndWhitespace(text, start + s_lz__mm_dd, out kind, out zoneHour, out zoneMinute))
                {
                    date = new DateInfo(day.Value, month.Value, DateInfo.LeapYear);
                    parsedValue = new DateAndTimeInfo(date, default, default, kind.Value, default, default, DateTimeTypeCode.GMonthDay, zoneHour.Value, zoneMinute.Value);
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.GMonth) &&
                    (TryParseZoneAndWhitespace(text, start + s_lz__mm, out kind, out zoneHour, out zoneMinute) ||
                        ParseChar(text, start + s_lz__mm, '-') &&
                            ParseChar(text, start + s_lz__mm_, '-') &&
                            TryParseZoneAndWhitespace(text, start + s_lz__mm__, out kind, out zoneHour, out zoneMinute)))
                {
                    date = new DateInfo(DateInfo.FirstDay, month.Value, DateInfo.LeapYear);
                    parsedValue = new DateAndTimeInfo(date, default, default, kind.Value, default, default, DateTimeTypeCode.GMonth, zoneHour.Value, zoneMinute.Value);
                    return true;
                }
            }

            parsedValue = new DateAndTimeInfo();
            return false;
        }

        private static bool TryParseAsGDay(
            string text,
            XsdDateTimeFlags kinds,
            int start,
            out DateAndTimeInfo parsedValue)
        {
            int? day, zoneHour, zoneMinute;
            DateInfo date;
            XsdDateTimeKind? kind;

            if (Test(kinds, XsdDateTimeFlags.GDay) &&
                ParseChar(text, start, '-') &&
                ParseChar(text, start + s_Lz_, '-') &&
                ParseChar(text, start + s_Lz__, '-') &&
                ParseTwoDigits(text, start + s_Lz___, out day) &&
                1 <= day &&
                day <= DateTime.DaysInMonth(DateInfo.LeapYear, DateInfo.FirstMonth) &&
                TryParseZoneAndWhitespace(text, start + s_lz___dd, out kind, out zoneHour, out zoneMinute))
            {
                date = new DateInfo(day.Value, DateInfo.FirstMonth, DateInfo.LeapYear);
                parsedValue = new DateAndTimeInfo(date, default, default, kind.Value, default, default, DateTimeTypeCode.GDay, zoneHour.Value, zoneMinute.Value);

                return true;
            }

            parsedValue = new DateAndTimeInfo();
            return false;
        }

        private static bool ParseTwoDigits(
            string rawValue,
            int start,
            [NotNullWhen(true)] out int? num)
        {
            if (start + 1 < rawValue.Length)
            {
                int d2 = rawValue[start] - '0';
                int d1 = rawValue[start + 1] - '0';
                if (0 <= d2 && d2 < 10 &&
                    0 <= d1 && d1 < 10
                    )
                {
                    num = d2 * 10 + d1;
                    return true;
                }
            }

            num = default;
            return false;
        }

        private static bool ParseFourDigits(
            string rawValue,
            int start,
            [NotNullWhen(true)] out int? num)
        {
            if (start + 3 < rawValue.Length)
            {
                int d4 = rawValue[start] - '0';
                int d3 = rawValue[start + 1] - '0';
                int d2 = rawValue[start + 2] - '0';
                int d1 = rawValue[start + 3] - '0';
                if (0 <= d4 && d4 < 10 &&
                    0 <= d3 && d3 < 10 &&
                    0 <= d2 && d2 < 10 &&
                    0 <= d1 && d1 < 10
                )
                {
                    num = ((d4 * 10 + d3) * 10 + d2) * 10 + d1;
                    return true;
                }
            }

            num = default;
            return false;
        }

        private static bool ParseChar(string rawValue, int start, char ch)
        {
            return start < rawValue.Length && rawValue[start] == ch;
        }

        private static bool Test(XsdDateTimeFlags left, XsdDateTimeFlags right)
        {
            return (left & right) != 0;
        }

        private static bool TryParseDate(
            string rawValue,
            int start,
            out DateInfo parsedDate)
        {
            int? day, month, year;

            if (ParseFourDigits(rawValue, start, out year) &&
                1 <= year &&
                ParseChar(rawValue, start + s_lzyyyy, '-') &&
                ParseTwoDigits(rawValue, start + s_lzyyyy_, out month) &&
                1 <= month && month <= 12 &&
                ParseChar(rawValue, start + s_lzyyyy_MM, '-') &&
                ParseTwoDigits(rawValue, start + s_lzyyyy_MM_, out day) &&
                1 <= day &&
                day <= DateTime.DaysInMonth(year.Value, month.Value)
                )
            {
                parsedDate = new DateInfo(day.Value, month.Value, year.Value);
                return true;
            }

            parsedDate = default;
            return false;
        }

        private static bool TryParseTime(
            string rawValue,
            ref int start,
            [NotNullWhen(true)] out int? hour,
            [NotNullWhen(true)] out int? minute,
            [NotNullWhen(true)] out int? second,
            [NotNullWhen(true)] out int? fraction)
        {
            if (
                ParseTwoDigits(rawValue, start, out hour) && hour < 24 &&
                ParseChar(rawValue, start + s_lzHH, ':') &&
                ParseTwoDigits(rawValue, start + s_lzHH_, out minute) && minute < 60 &&
                ParseChar(rawValue, start + s_lzHH_mm, ':') &&
                ParseTwoDigits(rawValue, start + s_lzHH_mm_, out second) && second < 60
            )
            {
                start += s_lzHH_mm_ss;
                if (ParseChar(rawValue, start, '.'))
                {
                    // Parse factional part of seconds
                    // We allow any number of digits, but keep only first 7
                    fraction = 0;
                    int fractionDigits = 0;
                    int round = 0;
                    while (++start < rawValue.Length)
                    {
                        int d = rawValue[start] - '0';
                        if (9u < unchecked((uint)d))
                        { // d < 0 || 9 < d
                            break;
                        }
                        if (fractionDigits < MaxFractionDigits)
                        {
                            fraction = fraction * 10 + d;
                        }
                        else if (fractionDigits == MaxFractionDigits)
                        {
                            if (5 < d)
                            {
                                round = 1;
                            }
                            else if (d == 5)
                            {
                                round = -1;
                            }
                        }
                        else if (round < 0 && d != 0)
                        {
                            round = 1;
                        }
                        fractionDigits++;
                    }
                    if (fractionDigits < MaxFractionDigits)
                    {
                        if (fractionDigits == 0)
                        {
                            return false; // cannot end with .
                        }
                        fraction *= Power10[MaxFractionDigits - fractionDigits];
                    }
                    else
                    {
                        if (round < 0)
                        {
                            round = fraction.Value & 1;
                        }
                        fraction += round;
                    }
                }
                else
                {
                    fraction = 0;
                }

                return true;
            }

            hour = default;
            minute = default;
            second = default;
            fraction = default;
            return false;
        }

        private static bool ParseTimeAndWhitespace(
            string rawValue,
            int start,
            [NotNullWhen(true)] out int? hour,
            [NotNullWhen(true)] out int? minute,
            [NotNullWhen(true)] out int? second,
            [NotNullWhen(true)] out int? fraction)
        {
            if (TryParseTime(rawValue, ref start, out hour, out minute, out second, out fraction))
            {
                while (start < rawValue.Length)
                {
                    start++;
                }
                return start == rawValue.Length;
            }
            return false;
        }

        private static bool TryParseTimeAndZoneAndWhitespace(
            string rawValue,
            int start,
            [NotNullWhen(true)] out int? hour,
            [NotNullWhen(true)] out int? minute,
            [NotNullWhen(true)] out int? second,
            [NotNullWhen(true)] out int? fraction,
            [NotNullWhen(true)] out XsdDateTimeKind? kind,
            [NotNullWhen(true)] out int? zoneHour,
            [NotNullWhen(true)] out int? zoneMinute)
        {
            if (TryParseTime(rawValue, ref start, out hour, out minute, out second, out fraction))
            {
                if (TryParseZoneAndWhitespace(rawValue, start, out kind, out zoneHour, out zoneMinute))
                {
                    return true;
                }
            }

            kind = default;
            zoneHour = default;
            zoneMinute = default;
            return false;
        }

        private static bool TryParseZoneAndWhitespace(
            string rawValue,
            int start,
            [NotNullWhen(true)] out XsdDateTimeKind? kind,
            [NotNullWhen(true)] out int? zoneHour,
            [NotNullWhen(true)] out int? zoneMinute)
        {
            const int zoneHourOfUnspecified = 0;
            const int zoneHourOfZulu = 0;
            const int zoneMinuteOfUnspecified = 0;
            const int zoneMinuteOfZulu = 0;

            if (start < rawValue.Length)
            {
                char ch = rawValue[start];
                if (ch == 'Z' || ch == 'z')
                {
                    kind = XsdDateTimeKind.Zulu;
                    zoneHour = zoneHourOfZulu;
                    zoneMinute = zoneMinuteOfZulu;
                    start++;
                }
                else if (start + 5 < rawValue.Length &&
                    ParseTwoDigits(rawValue, start + s_Lz_, out zoneHour) &&
                    zoneHour <= 99 &&
                    ParseChar(rawValue, start + s_lz_zz, ':') &&
                    ParseTwoDigits(rawValue, start + s_lz_zz_, out zoneMinute) &&
                    zoneMinute <= 99)
                {
                    switch (ch)
                    {
                        case '-':
                        {
                            kind = XsdDateTimeKind.LocalWestOfZulu;
                            start += s_lz_zz_zz;
                            break;
                        }
                        case '+':
                        {
                            kind = XsdDateTimeKind.LocalEastOfZulu;
                            start += s_lz_zz_zz;
                            break;
                        }
                        default:
                        {
                            kind = default;
                            break;
                        }
                    }
                }
                else
                {
                    kind = XsdDateTimeKind.Unspecified;
                    zoneHour = zoneHourOfUnspecified;
                    zoneMinute = zoneMinuteOfUnspecified;
                }
            }
            else
            {
                kind = XsdDateTimeKind.Unspecified;
                zoneHour = zoneHourOfUnspecified;
                zoneMinute = zoneMinuteOfUnspecified;
            }

            while (start < rawValue.Length && char.IsWhiteSpace(rawValue[start]))
            {
                start++;
            }

            return start == rawValue.Length;
        }
    }
}
