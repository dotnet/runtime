// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System;
    using System.Xml;
    using System.Diagnostics;
    using System.Text;

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
        private DateTime _dt;

        // Additional information that DateTime is not preserving
        // Information is stored in the following format:
        // Bits     Info
        // 31-24    DateTimeTypeCode
        // 23-16    XsdDateTimeKind
        // 15-8     Zone Hours
        // 7-0      Zone Minutes
        private uint _extra;


        // Subset of XML Schema types XsdDateTime represents
        private enum DateTimeTypeCode
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
        private enum XsdDateTimeKind
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
        private const int ticksToFractionDivisor = 10000000;

        private static readonly int s_lzyyyy = "yyyy".Length;
        private static readonly int s_lzyyyy_ = "yyyy-".Length;
        private static readonly int s_lzyyyy_MM = "yyyy-MM".Length;
        private static readonly int s_lzyyyy_MM_ = "yyyy-MM-".Length;
        private static readonly int s_lzyyyy_MM_dd = "yyyy-MM-dd".Length;
        private static readonly int s_lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;
        private static readonly int s_lzHH = "HH".Length;
        private static readonly int s_lzHH_ = "HH:".Length;
        private static readonly int s_lzHH_mm = "HH:mm".Length;
        private static readonly int s_lzHH_mm_ = "HH:mm:".Length;
        private static readonly int s_lzHH_mm_ss = "HH:mm:ss".Length;
        private static readonly int s_Lz_ = "-".Length;
        private static readonly int s_lz_zz = "-zz".Length;
        private static readonly int s_lz_zz_ = "-zz:".Length;
        private static readonly int s_lz_zz_zz = "-zz:zz".Length;
        private static readonly int s_Lz__ = "--".Length;
        private static readonly int s_lz__mm = "--MM".Length;
        private static readonly int s_lz__mm_ = "--MM-".Length;
        private static readonly int s_lz__mm__ = "--MM--".Length;
        private static readonly int s_lz__mm_dd = "--MM-dd".Length;
        private static readonly int s_Lz___ = "---".Length;
        private static readonly int s_lz___dd = "---dd".Length;

        // These values were copied from the DateTime class and are
        // needed to convert ticks to year, month and day. See comment
        // for method GetYearMonthDay for rationale.
        // Number of 100ns ticks per time unit
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;

        // Number of days in a non-leap year
        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        private static readonly int[] DaysToMonth365 = {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};
        private static readonly int[] DaysToMonth366 = {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366};

        /// <summary>
        /// Constructs an XsdDateTime from a string using specific format.
        /// </summary>
        public XsdDateTime(string text, XsdDateTimeFlags kinds) : this()
        {
            Parser parser = default;
            if (!parser.Parse(text, kinds))
            {
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, text, kinds));
            }
            InitiateXsdDateTime(parser);
        }

        private XsdDateTime(Parser parser) : this()
        {
            InitiateXsdDateTime(parser);
        }

        private void InitiateXsdDateTime(Parser parser)
        {
            _dt = new DateTime(parser.year, parser.month, parser.day, parser.hour, parser.minute, parser.second);
            if (parser.fraction != 0)
            {
                _dt = _dt.AddTicks(parser.fraction);
            }
            _extra = (uint)(((int)parser.typeCode << TypeShift) | ((int)parser.kind << KindShift) | (parser.zoneHour << ZoneHourShift) | parser.zoneMinute);
        }

        internal static bool TryParse(string text, XsdDateTimeFlags kinds, out XsdDateTime result)
        {
            Parser parser = default;
            if (!parser.Parse(text, kinds))
            {
                result = default;
                return false;
            }
            result = new XsdDateTime(parser);
            return true;
        }

        /// <summary>
        /// Constructs an XsdDateTime from a DateTime.
        /// </summary>
        public XsdDateTime(DateTime dateTime, XsdDateTimeFlags kinds)
        {
            Debug.Assert(Bits.ExactlyOne((uint)kinds), "Only one DateTime type code can be set.");
            _dt = dateTime;

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
                        Debug.Assert(dateTime.Kind == DateTimeKind.Local, $"Unknown DateTimeKind: {dateTime.Kind}");
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

            _extra = (uint)(((int)code << TypeShift) | ((int)kind << KindShift) | (zoneHour << ZoneHourShift) | zoneMinute);
        }

        // Constructs an XsdDateTime from a DateTimeOffset
        public XsdDateTime(DateTimeOffset dateTimeOffset) : this(dateTimeOffset, XsdDateTimeFlags.DateTime)
        {
        }

        public XsdDateTime(DateTimeOffset dateTimeOffset, XsdDateTimeFlags kinds)
        {
            Debug.Assert(Bits.ExactlyOne((uint)kinds), "Only one DateTime type code can be set.");

            _dt = dateTimeOffset.DateTime;

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

            _extra = (uint)(((int)code << TypeShift) | ((int)kind << KindShift) | (zoneOffset.Hours << ZoneHourShift) | zoneOffset.Minutes);
        }

        /// <summary>
        /// Returns auxiliary enumeration of XSD date type
        /// </summary>
        private DateTimeTypeCode InternalTypeCode
        {
            get { return (DateTimeTypeCode)((_extra & TypeMask) >> TypeShift); }
        }

        /// <summary>
        /// Returns geographical "position" of the value
        /// </summary>
        private XsdDateTimeKind InternalKind
        {
            get { return (XsdDateTimeKind)((_extra & KindMask) >> KindShift); }
        }

        /// <summary>
        /// Returns XmlTypeCode of the value being stored
        /// </summary>
        public XmlTypeCode TypeCode
        {
            get { return s_typeCodes[(int)InternalTypeCode]; }
        }

        /// <summary>
        /// Returns the year part of XsdDateTime
        /// The returned value is integer between 1 and 9999
        /// </summary>
        public int Year
        {
            get { return _dt.Year; }
        }

        /// <summary>
        /// Returns the month part of XsdDateTime
        /// The returned value is integer between 1 and 12
        /// </summary>
        public int Month
        {
            get { return _dt.Month; }
        }

        /// <summary>
        /// Returns the day of the month part of XsdDateTime
        /// The returned value is integer between 1 and 31
        /// </summary>
        public int Day
        {
            get { return _dt.Day; }
        }

        /// <summary>
        /// Returns the hour part of XsdDateTime
        /// The returned value is integer between 0 and 23
        /// </summary>
        public int Hour
        {
            get { return _dt.Hour; }
        }

        /// <summary>
        /// Returns the minute part of XsdDateTime
        /// The returned value is integer between 0 and 60
        /// </summary>
        public int Minute
        {
            get { return _dt.Minute; }
        }

        /// <summary>
        /// Returns the second part of XsdDateTime
        /// The returned value is integer between 0 and 60
        /// </summary>
        public int Second
        {
            get { return _dt.Second; }
        }

        /// <summary>
        /// Returns number of ticks in the fraction of the second
        /// The returned value is integer between 0 and 9999999
        /// </summary>
        public int Fraction
        {
            get { return (int)(_dt.Ticks % ticksToFractionDivisor); }
        }

        /// <summary>
        /// Returns the hour part of the time zone
        /// The returned value is integer between -13 and 13
        /// </summary>
        public int ZoneHour
        {
            get
            {
                uint result = (_extra & ZoneHourMask) >> ZoneHourShift;
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
                uint result = (_extra & ZoneMinuteMask);
                return (int)result;
            }
        }

        public DateTime ToZulu() =>
            InternalKind switch
            {
                // set it to UTC
                XsdDateTimeKind.Zulu => new DateTime(_dt.Ticks, DateTimeKind.Utc),

                // Adjust to UTC and then convert to local in the current time zone
                XsdDateTimeKind.LocalEastOfZulu => new DateTime(_dt.Subtract(new TimeSpan(ZoneHour, ZoneMinute, 0)).Ticks, DateTimeKind.Utc),
                XsdDateTimeKind.LocalWestOfZulu => new DateTime(_dt.Add(new TimeSpan(ZoneHour, ZoneMinute, 0)).Ticks, DateTimeKind.Utc),
                _ => _dt,
            };

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
                    result = xdt._dt.Add(addDiff);
                    break;
                default:
                    result = xdt._dt;
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
                    dt = xdt._dt.Add(addDiff);
                    break;
                default:
                    dt = xdt._dt;
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
                    text = new char[s_lzyyyy_MM];
                    IntToCharArray(text, 0, Year, 4);
                    text[s_lzyyyy] = '-';
                    ShortToCharArray(text, s_lzyyyy_, Month);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GYear:
                    text = new char[s_lzyyyy];
                    IntToCharArray(text, 0, Year, 4);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GMonthDay:
                    text = new char[s_lz__mm_dd];
                    text[0] = '-';
                    text[s_Lz_] = '-';
                    ShortToCharArray(text, s_Lz__, Month);
                    text[s_lz__mm] = '-';
                    ShortToCharArray(text, s_lz__mm_, Day);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GDay:
                    text = new char[s_lz___dd];
                    text[0] = '-';
                    text[s_Lz_] = '-';
                    text[s_Lz__] = '-';
                    ShortToCharArray(text, s_Lz___, Day);
                    sb.Append(text);
                    break;
                case DateTimeTypeCode.GMonth:
                    text = new char[s_lz__mm__];
                    text[0] = '-';
                    text[s_Lz_] = '-';
                    ShortToCharArray(text, s_Lz__, Month);
                    text[s_lz__mm] = '-';
                    text[s_lz__mm_] = '-';
                    sb.Append(text);
                    break;
            }
            PrintZone(sb);
            return sb.ToString();
        }

        // Serialize year, month and day
        private void PrintDate(StringBuilder sb)
        {
            char[] text = new char[s_lzyyyy_MM_dd];
            int year, month, day;
            GetYearMonthDay(out year, out month, out day);
            IntToCharArray(text, 0, year, 4);
            text[s_lzyyyy] = '-';
            ShortToCharArray(text, s_lzyyyy_, month);
            text[s_lzyyyy_MM] = '-';
            ShortToCharArray(text, s_lzyyyy_MM_, day);
            sb.Append(text);
        }

        // When printing the date, we need the year, month and the day. When
        // requesting these values from DateTime, it needs to redo the year
        // calculation before it can calculate the month, and it needs to redo
        // the year and month calculation before it can calculate the day. This
        // results in the year being calculated 3 times, the month twice and the
        // day once. As we know that we need all 3 values, by duplicating the
        // logic here we can calculate the number of days and return the intermediate
        // calculations for month and year without the added cost.
        private void GetYearMonthDay(out int year, out int month, out int day)
        {
            long ticks = _dt.Ticks;
            // n = number of days since 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            int y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4)
                y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4)
                y1 = 3;

            year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;

            // n = day number within year
            n -= y1 * DaysPerYear;

            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            int[] days = leapYear ? DaysToMonth366 : DaysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            month = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[month])
                month++;

            day = n - days[month - 1] + 1;
        }

        // Serialize hour, minute, second and fraction
        private void PrintTime(StringBuilder sb)
        {
            char[] text = new char[s_lzHH_mm_ss];
            ShortToCharArray(text, 0, Hour);
            text[s_lzHH] = ':';
            ShortToCharArray(text, s_lzHH_, Minute);
            text[s_lzHH_mm] = ':';
            ShortToCharArray(text, s_lzHH_mm_, Second);
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
                    text = new char[s_lz_zz_zz];
                    text[0] = '-';
                    ShortToCharArray(text, s_Lz_, ZoneHour);
                    text[s_lz_zz] = ':';
                    ShortToCharArray(text, s_lz_zz_, ZoneMinute);
                    sb.Append(text);
                    break;
                case XsdDateTimeKind.LocalEastOfZulu:
                    text = new char[s_lz_zz_zz];
                    text[0] = '+';
                    ShortToCharArray(text, s_Lz_, ZoneHour);
                    text[s_lz_zz] = ':';
                    ShortToCharArray(text, s_lz_zz_, ZoneMinute);
                    sb.Append(text);
                    break;
                default:
                    // do nothing
                    break;
            }
        }

        // Serialize integer into character array starting with index [start].
        // Number of digits is set by [digits]
        private void IntToCharArray(char[] text, int start, int value, int digits)
        {
            while (digits-- != 0)
            {
                text[start + digits] = (char)(value % 10 + '0');
                value /= 10;
            }
        }

        // Serialize two digit integer into character array starting with index [start].
        private void ShortToCharArray(char[] text, int start, int value)
        {
            text[start] = (char)(value / 10 + '0');
            text[start + 1] = (char)(value % 10 + '0');
        }

        private static readonly XmlTypeCode[] s_typeCodes = {
            XmlTypeCode.DateTime,
            XmlTypeCode.Time,
            XmlTypeCode.Date,
            XmlTypeCode.GYearMonth,
            XmlTypeCode.GYear,
            XmlTypeCode.GMonthDay,
            XmlTypeCode.GDay,
            XmlTypeCode.GMonth
        };


        // Parsing string according to XML schema spec
        private struct Parser
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

            private string _text;
            private int _length;

            public bool Parse(string text, XsdDateTimeFlags kinds)
            {
                _text = text;
                _length = text.Length;

                // Skip leading whitespace
                int start = 0;
                while (start < _length && char.IsWhiteSpace(text[start]))
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
                            if (ParseChar(start + s_lzyyyy_MM_dd, 'T') && ParseTimeAndZoneAndWhitespace(start + s_lzyyyy_MM_ddT))
                            {
                                typeCode = DateTimeTypeCode.DateTime;
                                return true;
                            }
                        }
                        if (Test(kinds, XsdDateTimeFlags.Date))
                        {
                            if (ParseZoneAndWhitespace(start + s_lzyyyy_MM_dd))
                            {
                                typeCode = DateTimeTypeCode.Date;
                                return true;
                            }
                        }

                        if (Test(kinds, XsdDateTimeFlags.XdrDateTime))
                        {
                            if (ParseZoneAndWhitespace(start + s_lzyyyy_MM_dd) || (ParseChar(start + s_lzyyyy_MM_dd, 'T') && ParseTimeAndZoneAndWhitespace(start + s_lzyyyy_MM_ddT)))
                            {
                                typeCode = DateTimeTypeCode.XdrDateTime;
                                return true;
                            }
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
                                ParseChar(start + s_lzyyyy, '-') &&
                                Parse2Dig(start + s_lzyyyy_, ref month) && 1 <= month && month <= 12 &&
                                ParseZoneAndWhitespace(start + s_lzyyyy_MM)
                            )
                            {
                                day = firstDay;
                                typeCode = DateTimeTypeCode.GYearMonth;
                                return true;
                            }
                        }
                        if (Test(kinds, XsdDateTimeFlags.GYear))
                        {
                            if (ParseZoneAndWhitespace(start + s_lzyyyy))
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
                        ParseChar(start + s_Lz_, '-') &&
                        Parse2Dig(start + s_Lz__, ref month) && 1 <= month && month <= 12
                    )
                    {
                        if (Test(kinds, XsdDateTimeFlags.GMonthDay) && ParseChar(start + s_lz__mm, '-'))
                        {
                            if (
                                Parse2Dig(start + s_lz__mm_, ref day) && 1 <= day && day <= DateTime.DaysInMonth(leapYear, month) &&
                                ParseZoneAndWhitespace(start + s_lz__mm_dd)
                            )
                            {
                                year = leapYear;
                                typeCode = DateTimeTypeCode.GMonthDay;
                                return true;
                            }
                        }
                        if (Test(kinds, XsdDateTimeFlags.GMonth))
                        {
                            if (ParseZoneAndWhitespace(start + s_lz__mm) || (ParseChar(start + s_lz__mm, '-') && ParseChar(start + s_lz__mm_, '-') && ParseZoneAndWhitespace(start + s_lz__mm__)))
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
                        ParseChar(start + s_Lz_, '-') &&
                        ParseChar(start + s_Lz__, '-') &&
                        Parse2Dig(start + s_Lz___, ref day) && 1 <= day && day <= DateTime.DaysInMonth(leapYear, firstMonth) &&
                        ParseZoneAndWhitespace(start + s_lz___dd)

                    )
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
                    ParseChar(start + s_lzyyyy, '-') &&
                    Parse2Dig(start + s_lzyyyy_, ref month) && 1 <= month && month <= 12 &&
                    ParseChar(start + s_lzyyyy_MM, '-') &&
                    Parse2Dig(start + s_lzyyyy_MM_, ref day) && 1 <= day && day <= DateTime.DaysInMonth(year, month);
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
                    while (start < _length)
                    {//&& char.IsWhiteSpace(text[start])) {
                        start++;
                    }
                    return start == _length;
                }
                return false;
            }

            private static readonly int[] s_power10 = new int[maxFractionDigits] { -1, 10, 100, 1000, 10000, 100000, 1000000 };
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
                            fraction *= s_power10[maxFractionDigits - fractionDigits];
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
                if (start < _length)
                {
                    char ch = _text[start];
                    if (ch == 'Z' || ch == 'z')
                    {
                        kind = XsdDateTimeKind.Zulu;
                        start++;
                    }
                    else if (start + 5 < _length)
                    {
                        if (
                            Parse2Dig(start + s_Lz_, ref zoneHour) && zoneHour <= 99 &&
                            ParseChar(start + s_lz_zz, ':') &&
                            Parse2Dig(start + s_lz_zz_, ref zoneMinute) && zoneMinute <= 99
                        )
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
                }
                while (start < _length && char.IsWhiteSpace(_text[start]))
                {
                    start++;
                }
                return start == _length;
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

            private bool ParseChar(int start, char ch)
            {
                return start < _length && _text[start] == ch;
            }

            private static bool Test(XsdDateTimeFlags left, XsdDateTimeFlags right)
            {
                return (left & right) != 0;
            }
        }
    }
}
