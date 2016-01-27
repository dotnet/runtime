// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Xml.Schema
{
    /// <summary>
    /// This enum specifies what format should be used when converting string to XsdDateTime
    /// </summary>
    [Flags]
    internal enum XsdDateTimeFlags
    {
        DateTime = 0x01,
        Time = 0x02,
        Date = 0x04,
        GYearMonth = 0x08,
        GYear = 0x10,
        GMonthDay = 0x20,
        GDay = 0x40,
        GMonth = 0x80,
        XdrDateTimeNoTz = 0x100,
        XdrDateTime = 0x200,
        XdrTimeNoTz = 0x400,  //XDRTime with tz is the same as xsd:time  
        AllXsd = 0xFF //All still does not include the XDR formats
    }

    /// <summary>
    /// This structure extends System.DateTime to support timeInTicks zone and Gregorian types components of an Xsd Duration.  It is used internally to support Xsd durations without loss
    /// of fidelity.  XsdDuration structures are immutable once they've been created.
    /// </summary>
    internal struct XsdDateTime
    {
        // DateTime is being used as an internal representation only
        // Casting XsdDateTime to DateTime might return a different value
        private DateTime dt;

        // Additional information that DateTime is not preserving
        // Information is stored in the following format:
        // Bits     Info
        // 31-24    DateTimeTypeCode 
        // 23-16    XsdDateTimeKind
        // 15-8     Zone Hours
        // 7-0      Zone Minutes
        private uint extra;


        // Subset of XML Schema types XsdDateTime represents
        enum DateTimeTypeCode
        {
            DateTime,
            Time,
            Date,
            GYearMonth,
            GYear,
            GMonthDay,
            GDay,
            GMonth,
            XdrDateTime,
        }

        // Internal representation of DateTimeKind
        enum XsdDateTimeKind
        {
            Unspecified,
            Zulu,
            LocalWestOfZulu,    // GMT-1..14, N..Y
            LocalEastOfZulu     // GMT+1..14, A..M
        }

        // Masks and shifts used for packing and unpacking extra 
        private const uint TypeMask = 0xFF000000;
        private const uint KindMask = 0x00FF0000;
        private const uint ZoneHourMask = 0x0000FF00;
        private const uint ZoneMinuteMask = 0x000000FF;
        private const int TypeShift = 24;
        private const int KindShift = 16;
        private const int ZoneHourShift = 8;

        // Maximum number of fraction digits;
        private const short maxFractionDigits = 7;

        static readonly int Lzyyyy = "yyyy".Length;
        static readonly int Lzyyyy_ = "yyyy-".Length;
        static readonly int Lzyyyy_MM = "yyyy-MM".Length;
        static readonly int Lzyyyy_MM_ = "yyyy-MM-".Length;
        static readonly int Lzyyyy_MM_dd = "yyyy-MM-dd".Length;
        static readonly int Lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;
        static readonly int LzHH = "HH".Length;
        static readonly int LzHH_ = "HH:".Length;
        static readonly int LzHH_mm = "HH:mm".Length;
        static readonly int LzHH_mm_ = "HH:mm:".Length;
        static readonly int LzHH_mm_ss = "HH:mm:ss".Length;
        static readonly int Lz_ = "-".Length;
        static readonly int Lz_zz = "-zz".Length;
        static readonly int Lz_zz_ = "-zz:".Length;
        static readonly int Lz_zz_zz = "-zz:zz".Length;
        static readonly int Lz__ = "--".Length;
        static readonly int Lz__mm = "--MM".Length;
        static readonly int Lz__mm_ = "--MM-".Length;
        static readonly int Lz__mm__ = "--MM--".Length;
        static readonly int Lz__mm_dd = "--MM-dd".Length;
        static readonly int Lz___ = "---".Length;
        static readonly int Lz___dd = "---dd".Length;


        /// <summary>
        /// Constructs an XsdDateTime from a string using specific format.
        /// </summary>
        public XsdDateTime(string text, XsdDateTimeFlags kinds) : this()
        {
            Parser parser = new Parser();
            if (!parser.Parse(text, kinds))
            {
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, text, kinds));
            }
            InitiateXsdDateTime(parser);
        }

        private void InitiateXsdDateTime(Parser parser)
        {
            dt = new DateTime(parser.year, parser.month, parser.day, parser.hour, parser.minute, parser.second);
            if (parser.fraction != 0)
            {
                dt = dt.AddTicks(parser.fraction);
            }
            extra = (uint)(((int)parser.typeCode << TypeShift) | ((int)parser.kind << KindShift) | (parser.zoneHour << ZoneHourShift) | parser.zoneMinute);
        }

        /// <summary>
        /// Constructs an XsdDateTime from a DateTime.
        /// </summary>
        public XsdDateTime(DateTime dateTime, XsdDateTimeFlags kinds)
        {
            Debug.Assert(Bits.ExactlyOne((uint)kinds), "Only one DateTime type code can be set.");
            dt = dateTime;

            DateTimeTypeCode code = (DateTimeTypeCode)(Bits.LeastPosition((uint)kinds) - 1);
            int zoneHour = 0;
            int zoneMinute = 0;
            XsdDateTimeKind kind;

            switch (dateTime.Kind)
            {
                case DateTimeKind.Unspecified: kind = XsdDateTimeKind.Unspecified; break;
                case DateTimeKind.Utc: kind = XsdDateTimeKind.Zulu; break;

                default:
                    {
                        Debug.Assert(dateTime.Kind == DateTimeKind.Local, "Unknown DateTimeKind: " + dateTime.Kind);
                        TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(dateTime);

                        if (utcOffset.Ticks < 0)
                        {
                            kind = XsdDateTimeKind.LocalWestOfZulu;
                            zoneHour = -utcOffset.Hours;
                            zoneMinute = -utcOffset.Minutes;
                        }
                        else
                        {
                            kind = XsdDateTimeKind.LocalEastOfZulu;
                            zoneHour = utcOffset.Hours;
                            zoneMinute = utcOffset.Minutes;
                        }
                        break;
                    }
            }

            extra = (uint)(((int)code << TypeShift) | ((int)kind << KindShift) | (zoneHour << ZoneHourShift) | zoneMinute);
        }

        public XsdDateTime(DateTimeOffset dateTimeOffset, XsdDateTimeFlags kinds)
        {
            Debug.Assert(Bits.ExactlyOne((uint)kinds), "Only one DateTime type code can be set.");

            dt = dateTimeOffset.DateTime;

            TimeSpan zoneOffset = dateTimeOffset.Offset;
            DateTimeTypeCode code = (DateTimeTypeCode)(Bits.LeastPosition((uint)kinds) - 1);
            XsdDateTimeKind kind;
            if (zoneOffset.TotalMinutes < 0)
            {
                zoneOffset = zoneOffset.Negate();
                kind = XsdDateTimeKind.LocalWestOfZulu;
            }
            else if (zoneOffset.TotalMinutes > 0)
            {
                kind = XsdDateTimeKind.LocalEastOfZulu;
            }
            else
            {
                kind = XsdDateTimeKind.Zulu;
            }

            extra = (uint)(((int)code << TypeShift) | ((int)kind << KindShift) | (zoneOffset.Hours << ZoneHourShift) | zoneOffset.Minutes);
        }

        /// <summary>
        /// Returns auxiliary enumeration of XSD date type
        /// </summary>
        private DateTimeTypeCode InternalTypeCode
        {
            get { return (DateTimeTypeCode)((extra & TypeMask) >> TypeShift); }
        }

        /// <summary>
        /// Returns geographical "position" of the value
        /// </summary>
        private XsdDateTimeKind InternalKind
        {
            get { return (XsdDateTimeKind)((extra & KindMask) >> KindShift); }
        }

        /// <summary>
        /// Returns the year part of XsdDateTime
        /// The returned value is integer between 1 and 9999
        /// </summary>
        public int Year
        {
            get { return dt.Year; }
        }

        /// <summary>
        /// Returns the month part of XsdDateTime
        /// The returned value is integer between 1 and 12
        /// </summary>
        public int Month
        {
            get { return dt.Month; }
        }

        /// <summary>
        /// Returns the day of the month part of XsdDateTime
        /// The returned value is integer between 1 and 31
        /// </summary>
        public int Day
        {
            get { return dt.Day; }
        }

        /// <summary>
        /// Returns the hour part of XsdDateTime
        /// The returned value is integer between 0 and 23
        /// </summary>
        public int Hour
        {
            get { return dt.Hour; }
        }

        /// <summary>
        /// Returns the minute part of XsdDateTime
        /// The returned value is integer between 0 and 60
        /// </summary>
        public int Minute
        {
            get { return dt.Minute; }
        }

        /// <summary>
        /// Returns the second part of XsdDateTime
        /// The returned value is integer between 0 and 60
        /// </summary>
        public int Second
        {
            get { return dt.Second; }
        }

        /// <summary>
        /// Returns number of ticks in the fraction of the second
        /// The returned value is integer between 0 and 9999999
        /// </summary>
        public int Fraction
        {
            get { return (int)(dt.Ticks - new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).Ticks); }
        }

        /// <summary>
        /// Returns the hour part of the time zone
        /// The returned value is integer between -13 and 13
        /// </summary>
        public int ZoneHour
        {
            get
            {
                uint result = (extra & ZoneHourMask) >> ZoneHourShift;
                return (int)result;
            }
        }

        /// <summary>
        /// Returns the minute part of the time zone
        /// The returned value is integer between 0 and 60
        /// </summary>
        public int ZoneMinute
        {
            get
            {
                uint result = (extra & ZoneMinuteMask);
                return (int)result;
            }
        }

        /// <summary>
        /// Cast to DateTime
        /// The following table describes the behaviors of getting the default value
        /// when a certain year/month/day values are missing.
        /// 
        /// An "X" means that the value exists.  And "--" means that value is missing.
        /// 
        /// Year    Month   Day =>  ResultYear  ResultMonth     ResultDay       Note
        /// 
        /// X       X       X       Parsed year Parsed month    Parsed day
        /// X       X       --      Parsed Year Parsed month    First day       If we have year and month, assume the first day of that month.
        /// X       --      X       Parsed year First month     Parsed day      If the month is missing, assume first month of that year.
        /// X       --      --      Parsed year First month     First day       If we have only the year, assume the first day of that year.
        /// 
        /// --      X       X       CurrentYear Parsed month    Parsed day      If the year is missing, assume the current year.
        /// --      X       --      CurrentYear Parsed month    First day       If we have only a month value, assume the current year and current day.
        /// --      --      X       CurrentYear First month     Parsed day      If we have only a day value, assume current year and first month.
        /// --      --      --      CurrentYear Current month   Current day     So this means that if the date string only contains time, you will get current date.
        /// </summary>
        public static implicit operator DateTime(XsdDateTime xdt)
        {
            DateTime result;
            switch (xdt.InternalTypeCode)
            {
                case DateTimeTypeCode.GMonth:
                case DateTimeTypeCode.GDay:
                    result = new DateTime(DateTime.Now.Year, xdt.Month, xdt.Day);
                    break;
                case DateTimeTypeCode.Time:
                    //back to DateTime.Now 
                    DateTime currentDateTime = DateTime.Now;
                    TimeSpan addDiff = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day) - new DateTime(xdt.Year, xdt.Month, xdt.Day);
                    result = xdt.dt.Add(addDiff);
                    break;
                default:
                    result = xdt.dt;
                    break;
            }

            long ticks;
            switch (xdt.InternalKind)
            {
                case XsdDateTimeKind.Zulu:
                    // set it to UTC
                    result = new DateTime(result.Ticks, DateTimeKind.Utc);
                    break;
                case XsdDateTimeKind.LocalEastOfZulu:
                    // Adjust to UTC and then convert to local in the current time zone
                    ticks = result.Ticks - new TimeSpan(xdt.ZoneHour, xdt.ZoneMinute, 0).Ticks;
                    if (ticks < DateTime.MinValue.Ticks)
                    {
                        // Underflow. Return the DateTime as local time directly
                        ticks += TimeZoneInfo.Local.GetUtcOffset(result).Ticks;
                        if (ticks < DateTime.MinValue.Ticks)
                            ticks = DateTime.MinValue.Ticks;
                        return new DateTime(ticks, DateTimeKind.Local);
                    }
                    result = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
                    break;
                case XsdDateTimeKind.LocalWestOfZulu:
                    // Adjust to UTC and then convert to local in the current time zone
                    ticks = result.Ticks + new TimeSpan(xdt.ZoneHour, xdt.ZoneMinute, 0).Ticks;
                    if (ticks > DateTime.MaxValue.Ticks)
                    {
                        // Overflow. Return the DateTime as local time directly
                        ticks += TimeZoneInfo.Local.GetUtcOffset(result).Ticks;
                        if (ticks > DateTime.MaxValue.Ticks)
                            ticks = DateTime.MaxValue.Ticks;
                        return new DateTime(ticks, DateTimeKind.Local);
                    }
                    result = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
                    break;
                default:
                    break;
            }
            return result;
        }

        public static implicit operator DateTimeOffset(XsdDateTime xdt)
        {
            DateTime dt;

            switch (xdt.InternalTypeCode)
            {
                case DateTimeTypeCode.GMonth:
                case DateTimeTypeCode.GDay:
                    dt = new DateTime(DateTime.Now.Year, xdt.Month, xdt.Day);
                    break;
                case DateTimeTypeCode.Time:
                    //back to DateTime.Now 
                    DateTime currentDateTime = DateTime.Now;
                    TimeSpan addDiff = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day) - new DateTime(xdt.Year, xdt.Month, xdt.Day);
                    dt = xdt.dt.Add(addDiff);
                    break;
                default:
                    dt = xdt.dt;
                    break;
            }

            DateTimeOffset result;
            switch (xdt.InternalKind)
            {
                case XsdDateTimeKind.LocalEastOfZulu:
                    result = new DateTimeOffset(dt, new TimeSpan(xdt.ZoneHour, xdt.ZoneMinute, 0));
                    break;
                case XsdDateTimeKind.LocalWestOfZulu:
                    result = new DateTimeOffset(dt, new TimeSpan(-xdt.ZoneHour, -xdt.ZoneMinute, 0));
                    break;
                case XsdDateTimeKind.Zulu:
                    result = new DateTimeOffset(dt, new TimeSpan(0));
                    break;
                case XsdDateTimeKind.Unspecified:
                default:
                    result = new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
                    break;
            }

            return result;
        }

        /// <summary>
        /// Serialization to a string
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(64);
            char[] text;
            switch (InternalTypeCode)
            {
                case DateTimeTypeCode.DateTime:
                    PrintDate(sb);
                    sb.Append('T');
                    PrintTime(sb);
                    break;
                case DateTimeTypeCode.Time:
                    PrintTime(sb);
                    break;
                case DateTimeTypeCode.Date:
                    PrintDate(sb);
                    break;
                case DateTimeTypeCode.GYearMonth:
                    text = new char[Lzyyyy_MM];
                    IntToCharArray(text, 0, Year, 4);
                    text[Lzyyyy] = '-';
                    ShortToCharArray(text, Lzyyyy_, Month);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GYear:
                    text = new char[Lzyyyy];
                    IntToCharArray(text, 0, Year, 4);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GMonthDay:
                    text = new char[Lz__mm_dd];
                    text[0] = '-';
                    text[Lz_] = '-';
                    ShortToCharArray(text, Lz__, Month);
                    text[Lz__mm] = '-';
                    ShortToCharArray(text, Lz__mm_, Day);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GDay:
                    text = new char[Lz___dd];
                    text[0] = '-';
                    text[Lz_] = '-';
                    text[Lz__] = '-';
                    ShortToCharArray(text, Lz___, Day);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GMonth:
                    text = new char[Lz__mm__];
                    text[0] = '-';
                    text[Lz_] = '-';
                    ShortToCharArray(text, Lz__, Month);
                    text[Lz__mm] = '-';
                    text[Lz__mm_] = '-';
                    sb.Append(text);
                    break;
            }
            PrintZone(sb);
            return sb.ToString();
        }

        // Serialize year, month and day
        private void PrintDate(StringBuilder sb)
        {
            char[] text = new char[Lzyyyy_MM_dd];
            IntToCharArray(text, 0, Year, 4);
            text[Lzyyyy] = '-';
            ShortToCharArray(text, Lzyyyy_, Month);
            text[Lzyyyy_MM] = '-';
            ShortToCharArray(text, Lzyyyy_MM_, Day);
            sb.Append(text);
        }

        // Serialize hour, minute, second and fraction
        private void PrintTime(StringBuilder sb)
        {
            char[] text = new char[LzHH_mm_ss];
            ShortToCharArray(text, 0, Hour);
            text[LzHH] = ':';
            ShortToCharArray(text, LzHH_, Minute);
            text[LzHH_mm] = ':';
            ShortToCharArray(text, LzHH_mm_, Second);
            sb.Append(text);
            int fraction = Fraction;
            if (fraction != 0)
            {
                int fractionDigits = maxFractionDigits;
                while (fraction % 10 == 0)
                {
                    fractionDigits--;
                    fraction /= 10;
                }
                text = new char[fractionDigits + 1];
                text[0] = '.';
                IntToCharArray(text, 1, fraction, fractionDigits);
                sb.Append(text);
            }
        }

        // Serialize time zone
        private void PrintZone(StringBuilder sb)
        {
            char[] text;
            switch (InternalKind)
            {
                case XsdDateTimeKind.Zulu:
                    sb.Append('Z');
                    break;
                case XsdDateTimeKind.LocalWestOfZulu:
                    text = new char[Lz_zz_zz];
                    text[0] = '-';
                    ShortToCharArray(text, Lz_, ZoneHour);
                    text[Lz_zz] = ':';
                    ShortToCharArray(text, Lz_zz_, ZoneMinute);
                    sb.Append(text);
                    break;
                case XsdDateTimeKind.LocalEastOfZulu:
                    text = new char[Lz_zz_zz];
                    text[0] = '+';
                    ShortToCharArray(text, Lz_, ZoneHour);
                    text[Lz_zz] = ':';
                    ShortToCharArray(text, Lz_zz_, ZoneMinute);
                    sb.Append(text);
                    break;
                default:
                    // do nothing
                    break;
            }
        }

        // Serialize integer into character array starting with index [start]. 
        // Number of digits is set by [digits]
        private static void IntToCharArray(char[] text, int start, int value, int digits)
        {
            while (digits-- != 0)
            {
                text[start + digits] = (char)(value % 10 + '0');
                value /= 10;
            }
        }

        // Serialize two digit integer into character array starting with index [start].
        private static void ShortToCharArray(char[] text, int start, int value)
        {
            text[start] = (char)(value / 10 + '0');
            text[start + 1] = (char)(value % 10 + '0');
        }

        // Parsing string according to XML schema spec
        struct Parser
        {
            private const int leapYear = 1904;
            private const int firstMonth = 1;
            private const int firstDay = 1;

            public DateTimeTypeCode typeCode;
            public int year;
            public int month;
            public int day;
            public int hour;
            public int minute;
            public int second;
            public int fraction;
            public XsdDateTimeKind kind;
            public int zoneHour;
            public int zoneMinute;

            private string text;
            private int length;

            public bool Parse(string text, XsdDateTimeFlags kinds)
            {
                this.text = text;
                this.length = text.Length;

                // Skip leading whitespace
                int start = 0;
                while (start < length && char.IsWhiteSpace(text[start]))
                {
                    start++;
                }
                // Choose format starting from the most common and trying not to reparse the same thing too many times

                if (Test(kinds, XsdDateTimeFlags.DateTime | XsdDateTimeFlags.Date | XsdDateTimeFlags.XdrDateTime | XsdDateTimeFlags.XdrDateTimeNoTz))
                {
                    if (ParseDate(start))
                    {
                        if (Test(kinds, XsdDateTimeFlags.DateTime))
                        {
                            if (ParseChar(start + Lzyyyy_MM_dd, 'T') && ParseTimeAndZoneAndWhitespace(start + Lzyyyy_MM_ddT))
                            {
                                typeCode = DateTimeTypeCode.DateTime;
                                return true;
                            }
                        }

                        if (Test(kinds, XsdDateTimeFlags.Date))
                        {
                            if (ParseZoneAndWhitespace(start + Lzyyyy_MM_dd))
                            {
                                typeCode = DateTimeTypeCode.Date;
                                return true;
                            }
                        }
                        if (Test(kinds, XsdDateTimeFlags.XdrDateTime))
                        {
                            if (ParseZoneAndWhitespace(start + Lzyyyy_MM_dd) || (ParseChar(start + Lzyyyy_MM_dd, 'T') && ParseTimeAndZoneAndWhitespace(start + Lzyyyy_MM_ddT)))
                            {
                                typeCode = DateTimeTypeCode.XdrDateTime;
                                return true;
                            }
                        }
                        if (Test(kinds, XsdDateTimeFlags.XdrDateTimeNoTz))
                        {
                            if (ParseChar(start + Lzyyyy_MM_dd, 'T'))
                            {
                                if (ParseTimeAndWhitespace(start + Lzyyyy_MM_ddT))
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
                }

                if (Test(kinds, XsdDateTimeFlags.Time))
                {
                    if (ParseTimeAndZoneAndWhitespace(start))
                    { //Equivalent to NoCurrentDateDefault on DateTimeStyles while parsing xs:time
                        year = leapYear;
                        month = firstMonth;
                        day = firstDay;
                        typeCode = DateTimeTypeCode.Time;
                        return true;
                    }
                }

                if (Test(kinds, XsdDateTimeFlags.XdrTimeNoTz))
                {
                    if (ParseTimeAndWhitespace(start))
                    { //Equivalent to NoCurrentDateDefault on DateTimeStyles while parsing xs:time
                        year = leapYear;
                        month = firstMonth;
                        day = firstDay;
                        typeCode = DateTimeTypeCode.Time;
                        return true;
                    }
                }

                if (Test(kinds, XsdDateTimeFlags.GYearMonth | XsdDateTimeFlags.GYear))
                {
                    if (Parse4Dig(start, ref year) && 1 <= year)
                    {
                        if (Test(kinds, XsdDateTimeFlags.GYearMonth))
                        {
                            if (
                                ParseChar(start + Lzyyyy, '-') &&
                                Parse2Dig(start + Lzyyyy_, ref month) && 1 <= month && month <= 12 &&
                                ParseZoneAndWhitespace(start + Lzyyyy_MM)
                            )
                            {
                                day = firstDay;
                                typeCode = DateTimeTypeCode.GYearMonth;
                                return true;
                            }
                        }
                        if (Test(kinds, XsdDateTimeFlags.GYear))
                        {
                            if (ParseZoneAndWhitespace(start + Lzyyyy))
                            {
                                month = firstMonth;
                                day = firstDay;
                                typeCode = DateTimeTypeCode.GYear;
                                return true;
                            }
                        }
                    }
                }
                if (Test(kinds, XsdDateTimeFlags.GMonthDay | XsdDateTimeFlags.GMonth))
                {
                    if (
                        ParseChar(start, '-') &&
                        ParseChar(start + Lz_, '-') &&
                        Parse2Dig(start + Lz__, ref month) && 1 <= month && month <= 12
                    )
                    {
                        if (Test(kinds, XsdDateTimeFlags.GMonthDay) && ParseChar(start + Lz__mm, '-'))
                        {
                            if (
                                Parse2Dig(start + Lz__mm_, ref day) && 1 <= day && day <= DateTime.DaysInMonth(leapYear, month) &&
                                ParseZoneAndWhitespace(start + Lz__mm_dd)
                            )
                            {
                                year = leapYear;
                                typeCode = DateTimeTypeCode.GMonthDay;
                                return true;
                            }
                        }
                        if (Test(kinds, XsdDateTimeFlags.GMonth))
                        {
                            if (ParseZoneAndWhitespace(start + Lz__mm) || (ParseChar(start + Lz__mm, '-') && ParseChar(start + Lz__mm_, '-') && ParseZoneAndWhitespace(start + Lz__mm__)))
                            {
                                year = leapYear;
                                day = firstDay;
                                typeCode = DateTimeTypeCode.GMonth;
                                return true;
                            }
                        }
                    }
                }

                if (Test(kinds, XsdDateTimeFlags.GDay))
                {
                    if (
                        ParseChar(start, '-') &&
                        ParseChar(start + Lz_, '-') &&
                        ParseChar(start + Lz__, '-') &&
                        Parse2Dig(start + Lz___, ref day) && 1 <= day && day <= DateTime.DaysInMonth(leapYear, firstMonth) &&
                        ParseZoneAndWhitespace(start + Lz___dd))
                    {
                        year = leapYear;
                        month = firstMonth;
                        typeCode = DateTimeTypeCode.GDay;
                        return true;
                    }
                }
                return false;
            }


            private bool ParseDate(int start)
            {
                return
                    Parse4Dig(start, ref year) && 1 <= year &&
                    ParseChar(start + Lzyyyy, '-') &&
                    Parse2Dig(start + Lzyyyy_, ref month) && 1 <= month && month <= 12 &&
                    ParseChar(start + Lzyyyy_MM, '-') &&
                    Parse2Dig(start + Lzyyyy_MM_, ref day) && 1 <= day && day <= DateTime.DaysInMonth(year, month);
            }

            private bool ParseTimeAndZoneAndWhitespace(int start)
            {
                if (ParseTime(ref start))
                {
                    if (ParseZoneAndWhitespace(start))
                    {
                        return true;
                    }
                }
                return false;
            }

            private bool ParseTimeAndWhitespace(int start)
            {
                if (ParseTime(ref start))
                {
                    while (start < length)
                    {
                        start++;
                    }
                    return start == length;
                }
                return false;
            }

            static int[] Power10 = new int[maxFractionDigits] { -1, 10, 100, 1000, 10000, 100000, 1000000 };
            private bool ParseTime(ref int start)
            {
                if (
                    Parse2Dig(start, ref hour) && hour < 24 &&
                    ParseChar(start + LzHH, ':') &&
                    Parse2Dig(start + LzHH_, ref minute) && minute < 60 &&
                    ParseChar(start + LzHH_mm, ':') &&
                    Parse2Dig(start + LzHH_mm_, ref second) && second < 60
                )
                {
                    start += LzHH_mm_ss;
                    if (ParseChar(start, '.'))
                    {
                        // Parse factional part of seconds
                        // We allow any number of digits, but keep only first 7
                        this.fraction = 0;
                        int fractionDigits = 0;
                        int round = 0;
                        while (++start < length)
                        {
                            int d = text[start] - '0';
                            if (9u < (uint)d)
                            { // d < 0 || 9 < d
                                break;
                            }
                            if (fractionDigits < maxFractionDigits)
                            {
                                this.fraction = (this.fraction * 10) + d;
                            }
                            else if (fractionDigits == maxFractionDigits)
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
                        if (fractionDigits < maxFractionDigits)
                        {
                            if (fractionDigits == 0)
                            {
                                return false; // cannot end with .
                            }
                            fraction *= Power10[maxFractionDigits - fractionDigits];
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

            private bool ParseZoneAndWhitespace(int start)
            {
                if (start < length)
                {
                    char ch = text[start];
                    if (ch == 'Z' || ch == 'z')
                    {
                        kind = XsdDateTimeKind.Zulu;
                        start++;
                    }
                    else if (start + 5 < length)
                    {
                        if (
                            Parse2Dig(start + Lz_, ref zoneHour) && zoneHour <= 99 &&
                            ParseChar(start + Lz_zz, ':') &&
                            Parse2Dig(start + Lz_zz_, ref zoneMinute) && zoneMinute <= 99
                        )
                        {
                            if (ch == '-')
                            {
                                kind = XsdDateTimeKind.LocalWestOfZulu;
                                start += Lz_zz_zz;
                            }
                            else if (ch == '+')
                            {
                                kind = XsdDateTimeKind.LocalEastOfZulu;
                                start += Lz_zz_zz;
                            }
                        }
                    }
                }
                while (start < length && char.IsWhiteSpace(text[start]))
                {
                    start++;
                }
                return start == length;
            }


            private bool Parse4Dig(int start, ref int num)
            {
                if (start + 3 < length)
                {
                    int d4 = text[start] - '0';
                    int d3 = text[start + 1] - '0';
                    int d2 = text[start + 2] - '0';
                    int d1 = text[start + 3] - '0';
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

            private bool Parse2Dig(int start, ref int num)
            {
                if (start + 1 < length)
                {
                    int d2 = text[start] - '0';
                    int d1 = text[start + 1] - '0';
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

            private bool ParseChar(int start, char ch)
            {
                return start < length && text[start] == ch;
            }

            private static bool Test(XsdDateTimeFlags left, XsdDateTimeFlags right)
            {
                return (left & right) != 0;
            }
        }
    }
}
