// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema.DateAndTime.Specifications;

namespace System.Xml.Schema.DateAndTime.Parsers
{
    internal struct XsdDateTimeParser
    {
        /// <summary>
        /// Maximum number of fraction digits.
        /// </summary>
        public const short MaxFractionDigits = 7;
        private const int firstDay = 1;
        private const int firstMonth = 1;
        private const int leapYear = 1904;

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

        public int day;
        public int fraction;
        public int hour;
        public XsdDateTimeKind kind;
        public int minute;
        public int month;
        public int second;
        public DateTimeTypeCode typeCode;
        public int year;
        public int zoneHour;
        public int zoneMinute;

        private int _length;
        private string _text;
        private static ReadOnlySpan<int> Power10 => [-1, 10, 100, 1000, 10000, 100000, 1000000];

        public bool Parse(string text, XsdDateTimeFlags kinds)
        {
            const XsdDateTimeFlags dateVariants = XsdDateTimeFlags.DateTime
                | XsdDateTimeFlags.Date
                | XsdDateTimeFlags.XdrDateTime
                | XsdDateTimeFlags.XdrDateTimeNoTz;

            _text = text;
            _length = text.Length;

            // Skip leading whitespace
            int start = 0;
            while (start < _length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            // Choose format starting from the most common and trying not to reparse the same thing too many times
            if (Test(kinds, dateVariants) && ParseDate(start))
            {
                if (Test(kinds, XsdDateTimeFlags.DateTime) &&
                    ParseChar(start + s_lzyyyy_MM_dd, 'T') &&
                    ParseTimeAndZoneAndWhitespace(start + s_lzyyyy_MM_ddT))
                {
                    typeCode = DateTimeTypeCode.DateTime;
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.Date)
                    && ParseZoneAndWhitespace(start + s_lzyyyy_MM_dd))
                {
                    typeCode = DateTimeTypeCode.Date;
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.XdrDateTime) &&
                    (ParseZoneAndWhitespace(start + s_lzyyyy_MM_dd) ||
                        (ParseChar(start + s_lzyyyy_MM_dd, 'T') && ParseTimeAndZoneAndWhitespace(start + s_lzyyyy_MM_ddT))))
                {
                    typeCode = DateTimeTypeCode.XdrDateTime;
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.XdrDateTimeNoTz))
                {
                    if (ParseChar(start + s_lzyyyy_MM_dd, 'T'))
                    {
                        if (ParseTimeAndWhitespace(start + s_lzyyyy_MM_ddT))
                        {
                            typeCode = DateTimeTypeCode.XdrDateTime;
                            return true;
                        }
                    }
                    else
                    {
                        typeCode = DateTimeTypeCode.XdrDateTime;
                        return true;
                    }
                }
            }

            if (Test(kinds, XsdDateTimeFlags.Time) && ParseTimeAndZoneAndWhitespace(start))
            {
                year = leapYear;
                month = firstMonth;
                day = firstDay;
                typeCode = DateTimeTypeCode.Time;
                return true;
            }

            if (Test(kinds, XsdDateTimeFlags.XdrTimeNoTz) && ParseTimeAndWhitespace(start))
            {
                year = leapYear;
                month = firstMonth;
                day = firstDay;
                typeCode = DateTimeTypeCode.Time;
                return true;
            }

            if (Test(kinds, XsdDateTimeFlags.GYearMonth | XsdDateTimeFlags.GYear) && Parse4Dig(start, ref year) && 1 <= year)
            {
                if (Test(kinds, XsdDateTimeFlags.GYearMonth) &&
                    ParseChar(start + s_lzyyyy, '-') &&
                    Parse2Dig(start + s_lzyyyy_, ref month) && 1 <= month && month <= 12 &&
                    ParseZoneAndWhitespace(start + s_lzyyyy_MM))
                {
                    day = firstDay;
                    typeCode = DateTimeTypeCode.GYearMonth;
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.GYear) && ParseZoneAndWhitespace(start + s_lzyyyy))
                {
                    month = firstMonth;
                    day = firstDay;
                    typeCode = DateTimeTypeCode.GYear;
                    return true;
                }
            }

            if (Test(kinds, XsdDateTimeFlags.GMonthDay | XsdDateTimeFlags.GMonth) &&
                ParseChar(start, '-') &&
                ParseChar(start + s_Lz_, '-') &&
                Parse2Dig(start + s_Lz__, ref month) && 1 <= month && month <= 12)
            {
                if (Test(kinds, XsdDateTimeFlags.GMonthDay) &&
                    ParseChar(start + s_lz__mm, '-') &&
                    Parse2Dig(start + s_lz__mm_, ref day) && 1 <= day && day <= DateTime.DaysInMonth(leapYear, month) &&
                    ParseZoneAndWhitespace(start + s_lz__mm_dd))
                {
                    year = leapYear;
                    typeCode = DateTimeTypeCode.GMonthDay;
                    return true;
                }

                if (Test(kinds, XsdDateTimeFlags.GMonth) &&
                    (ParseZoneAndWhitespace(start + s_lz__mm) ||
                        (ParseChar(start + s_lz__mm, '-') &&
                            ParseChar(start + s_lz__mm_, '-') &&
                            ParseZoneAndWhitespace(start + s_lz__mm__))))
                {
                    year = leapYear;
                    day = firstDay;
                    typeCode = DateTimeTypeCode.GMonth;
                    return true;
                }
            }

            if (Test(kinds, XsdDateTimeFlags.GDay) &&
                ParseChar(start, '-') &&
                ParseChar(start + s_Lz_, '-') &&
                ParseChar(start + s_Lz__, '-') &&
                Parse2Dig(start + s_Lz___, ref day) &&
                1 <= day &&
                day <= DateTime.DaysInMonth(leapYear, firstMonth) &&
                ParseZoneAndWhitespace(start + s_lz___dd))
            {
                year = leapYear;
                month = firstMonth;
                typeCode = DateTimeTypeCode.GDay;
                return true;
            }

            return false;
        }

        private static bool Test(XsdDateTimeFlags left, XsdDateTimeFlags right)
        {
            return (left & right) != 0;
        }

        private bool Parse2Dig(int start, ref int num)
        {
            if (start + 1 < _length)
            {
                int d2 = _text[start] - '0';
                int d1 = _text[start + 1] - '0';
                if (0 <= d2 && d2 < 10 &&
                    0 <= d1 && d1 < 10
                    )
                {
                    num = d2 * 10 + d1;
                    return true;
                }
            }
            return false;
        }

        private bool Parse4Dig(int start, ref int num)
        {
            if (start + 3 < _length)
            {
                int d4 = _text[start] - '0';
                int d3 = _text[start + 1] - '0';
                int d2 = _text[start + 2] - '0';
                int d1 = _text[start + 3] - '0';
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
            return false;
        }

        private bool ParseChar(int start, char ch)
        {
            return start < _length && _text[start] == ch;
        }

        private bool ParseDate(int start)
        {
            return
                Parse4Dig(start, ref year) && 1 <= year &&
                ParseChar(start + s_lzyyyy, '-') &&
                Parse2Dig(start + s_lzyyyy_, ref month) && 1 <= month && month <= 12 &&
                ParseChar(start + s_lzyyyy_MM, '-') &&
                Parse2Dig(start + s_lzyyyy_MM_, ref day) && 1 <= day && day <= DateTime.DaysInMonth(year, month);
        }

        private bool ParseTime(ref int start)
        {
            if (
                Parse2Dig(start, ref hour) && hour < 24 &&
                ParseChar(start + s_lzHH, ':') &&
                Parse2Dig(start + s_lzHH_, ref minute) && minute < 60 &&
                ParseChar(start + s_lzHH_mm, ':') &&
                Parse2Dig(start + s_lzHH_mm_, ref second) && second < 60
            )
            {
                start += s_lzHH_mm_ss;
                if (ParseChar(start, '.'))
                {
                    // Parse factional part of seconds
                    // We allow any number of digits, but keep only first 7
                    this.fraction = 0;
                    int fractionDigits = 0;
                    int round = 0;
                    while (++start < _length)
                    {
                        int d = _text[start] - '0';
                        if (9u < unchecked((uint)d))
                        { // d < 0 || 9 < d
                            break;
                        }
                        if (fractionDigits < MaxFractionDigits)
                        {
                            this.fraction = (this.fraction * 10) + d;
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
                            round = fraction & 1;
                        }
                        fraction += round;
                    }
                }
                return true;
            }
            // cleanup - conflict with gYear
            hour = 0;
            return false;
        }

        private bool ParseTimeAndWhitespace(int start)
        {
            if (ParseTime(ref start))
            {
                while (start < _length)
                {
                    start++;
                }
                return start == _length;
            }
            return false;
        }

        private bool ParseTimeAndZoneAndWhitespace(int start)
        {
            if (ParseTime(ref start) && ParseZoneAndWhitespace(start))
            {
                return true;
            }
            return false;
        }

        private bool ParseZoneAndWhitespace(int start)
        {
            if (start < _length)
            {
                char ch = _text[start];
                if (ch == 'Z' || ch == 'z')
                {
                    kind = XsdDateTimeKind.Zulu;
                    start++;
                }
                else if ((start + 5 < _length) &&
                    Parse2Dig(start + s_Lz_, ref zoneHour) &&
                    zoneHour <= 99 &&
                    ParseChar(start + s_lz_zz, ':') &&
                    Parse2Dig(start + s_lz_zz_, ref zoneMinute) &&
                    zoneMinute <= 99)
                {
                    if (ch == '-')
                    {
                        kind = XsdDateTimeKind.LocalWestOfZulu;
                        start += s_lz_zz_zz;
                    }
                    else if (ch == '+')
                    {
                        kind = XsdDateTimeKind.LocalEastOfZulu;
                        start += s_lz_zz_zz;
                    }
                }
            }
            while (start < _length && char.IsWhiteSpace(_text[start]))
            {
                start++;
            }
            return start == _length;
        }
    }
}
