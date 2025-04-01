// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Xml.Schema.DateAndTime.Converters;
using System.Xml.Schema.DateAndTime.Helpers;
using System.Xml.Schema.DateAndTime.Specifications;

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
        private DateTime _dt;

        // Additional information that DateTime is not preserving
        // Information is stored in the following format:
        // Bits     Info
        // 31-24    DateTimeTypeCode
        // 23-16    XsdDateTimeKind
        // 15-8     Zone Hours
        // 7-0      Zone Minutes
        private uint _extra;

        // Masks and shifts used for packing and unpacking extra
        private const uint TypeMask = 0xFF000000;
        private const uint KindMask = 0x00FF0000;
        private const uint ZoneHourMask = 0x0000FF00;
        private const uint ZoneMinuteMask = 0x000000FF;
        private const int TypeShift = 24;
        private const int KindShift = 16;
        private const int ZoneHourShift = 8;

        private const int TicksToFractionDivisor = 10000000;

        // Number of days in a non-leap year
        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        private static ReadOnlySpan<int> DaysToMonth365 => [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365];
        private static ReadOnlySpan<int> DaysToMonth366 => [0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366];

        private const int CharStackBufferSize = 64;

        /// <summary>
        /// Constructs an XsdDateTime from a string using specific format.
        /// </summary>
        public XsdDateTime(string text, XsdDateTimeFlags kinds) : this()
        {
            if (!DateAndTimeConverter.TryParse(text, kinds, out DateAndTimeInfo parsedValue))
            {
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, text, kinds));
            }

            InitiateXsdDateTime(parsedValue);
        }

        private XsdDateTime(DateAndTimeInfo parsedValue) : this()
        {
            InitiateXsdDateTime(parsedValue);
        }

        private void InitiateXsdDateTime(DateAndTimeInfo parsedValue)
        {
            _dt = new DateTime(parsedValue.Year, parsedValue.Month, parsedValue.Day, parsedValue.Hour, parsedValue.Minute, parsedValue.Second);
            if (parsedValue.Fraction != 0)
            {
                _dt = _dt.AddTicks(parsedValue.Fraction);
            }
            _extra = (uint)(((int)parsedValue.TypeCode << TypeShift) | ((int)parsedValue.Kind << KindShift) | (parsedValue.ZoneHour << ZoneHourShift) | parsedValue.ZoneMinute);
        }

        internal static bool TryParse(string text, XsdDateTimeFlags kinds, out XsdDateTime result)
        {
            if (!DateAndTimeConverter.TryParse(text, kinds, out DateAndTimeInfo parsedValue))
            {
                result = default;
                return false;
            }

            result = new XsdDateTime(parsedValue);
            return true;
        }

        /// <summary>
        /// Constructs an XsdDateTime from a DateTime.
        /// </summary>
        public XsdDateTime(DateTime dateTime, XsdDateTimeFlags kinds)
        {
            Debug.Assert(BitOperations.IsPow2((uint)kinds), "One and only one DateTime type code can be set.");
            _dt = dateTime;

            DateTimeTypeCode code = (DateTimeTypeCode)BitOperations.TrailingZeroCount((uint)kinds);
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
            Debug.Assert(BitOperations.IsPow2((uint)kinds), "Only one DateTime type code can be set.");

            _dt = dateTimeOffset.DateTime;

            TimeSpan zoneOffset = dateTimeOffset.Offset;
            DateTimeTypeCode code = (DateTimeTypeCode)BitOperations.TrailingZeroCount((uint)kinds);
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
            get { return (int)(_dt.Ticks % TicksToFractionDivisor); }
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
                    // codeql[cs/leap-year/unsafe-date-construction-from-two-elements] - The XML specification does not explicitly define this behavior for parsing in a non-leap year. We intentionally throw here. Altering this behavior to be more resilient, producing dates like 2/28 or 3/1, could introduce unintended consequences and may not be desirable for user.
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
            Span<char> destination = stackalloc char[CharStackBufferSize];
            bool success = TryFormat(destination, out int charsWritten);
            Debug.Assert(success);

            return destination.Slice(0, charsWritten).ToString();
        }

        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            var vsb = new ValueStringBuilder(destination);

            switch (InternalTypeCode)
            {
                case DateTimeTypeCode.DateTime:
                    PrintDate(ref vsb);
                    vsb.Append('T');
                    PrintTime(ref vsb);
                    break;
                case DateTimeTypeCode.Time:
                    PrintTime(ref vsb);
                    break;
                case DateTimeTypeCode.Date:
                    PrintDate(ref vsb);
                    break;
                case DateTimeTypeCode.GYearMonth:
                    vsb.AppendSpanFormattable(Year, format: "D4", provider: null);
                    vsb.Append('-');
                    vsb.AppendSpanFormattable(Month, format: "D2", provider: null);
                    break;
                case DateTimeTypeCode.GYear:
                    vsb.AppendSpanFormattable(Year, format: "D4", provider: null);
                    break;
                case DateTimeTypeCode.GMonthDay:
                    vsb.Append("--");
                    vsb.AppendSpanFormattable(Month, format: "D2", provider: null);
                    vsb.Append('-');
                    vsb.AppendSpanFormattable(Day, format: "D2", provider: null);
                    break;
                case DateTimeTypeCode.GDay:
                    vsb.Append("---");
                    vsb.AppendSpanFormattable(Day, format: "D2", provider: null);
                    break;
                case DateTimeTypeCode.GMonth:
                    vsb.Append("--");
                    vsb.AppendSpanFormattable(Month, format: "D2", provider: null);
                    vsb.Append("--");
                    break;
            }
            PrintZone(ref vsb);

            charsWritten = vsb.Length;
            return destination.Length >= vsb.Length;
        }

        // Serialize year, month and day
        private void PrintDate(ref ValueStringBuilder vsb)
        {
            Span<char> text = vsb.AppendSpan(DateAndTimeConverter.s_lzyyyy_MM_dd);
            int year, month, day;
            GetYearMonthDay(out year, out month, out day);
            WriteXDigits(text, 0, year, 4);
            text[DateAndTimeConverter.s_lzyyyy] = '-';
            Write2Digits(text, DateAndTimeConverter.s_lzyyyy_, month);
            text[DateAndTimeConverter.s_lzyyyy_MM] = '-';
            Write2Digits(text, DateAndTimeConverter.s_lzyyyy_MM_, day);
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
            int n = (int)(ticks / TimeSpan.TicksPerDay);
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
            ReadOnlySpan<int> days = leapYear ? DaysToMonth366 : DaysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            month = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[month])
                month++;

            day = n - days[month - 1] + 1;
        }

        // Serialize hour, minute, second and fraction
        private void PrintTime(ref ValueStringBuilder vsb)
        {
            Span<char> text = vsb.AppendSpan(DateAndTimeConverter.s_lzHH_mm_ss);
            Write2Digits(text, 0, Hour);
            text[DateAndTimeConverter.s_lzHH] = ':';
            Write2Digits(text, DateAndTimeConverter.s_lzHH_, Minute);
            text[DateAndTimeConverter.s_lzHH_mm] = ':';
            Write2Digits(text, DateAndTimeConverter.s_lzHH_mm_, Second);
            int fraction = Fraction;
            if (fraction != 0)
            {
                int fractionDigits = DateAndTimeConverter.MaxFractionDigits;
                while (fraction % 10 == 0)
                {
                    fractionDigits--;
                    fraction /= 10;
                }

                text = vsb.AppendSpan(fractionDigits + 1);
                text[0] = '.';
                WriteXDigits(text, 1, fraction, fractionDigits);
            }
        }

        // Serialize time zone
        private void PrintZone(ref ValueStringBuilder vsb)
        {
            Span<char> text;
            switch (InternalKind)
            {
                case XsdDateTimeKind.Zulu:
                    vsb.Append('Z');
                    break;
                case XsdDateTimeKind.LocalWestOfZulu:
                    text = vsb.AppendSpan(DateAndTimeConverter.s_lz_zz_zz);
                    text[0] = '-';
                    Write2Digits(text, DateAndTimeConverter.s_Lz_, ZoneHour);
                    text[DateAndTimeConverter.s_lz_zz] = ':';
                    Write2Digits(text, DateAndTimeConverter.s_lz_zz_, ZoneMinute);
                    break;
                case XsdDateTimeKind.LocalEastOfZulu:
                    text = vsb.AppendSpan(DateAndTimeConverter.s_lz_zz_zz);
                    text[0] = '+';
                    Write2Digits(text, DateAndTimeConverter.s_Lz_, ZoneHour);
                    text[DateAndTimeConverter.s_lz_zz] = ':';
                    Write2Digits(text, DateAndTimeConverter.s_lz_zz_, ZoneMinute);
                    break;
                default:
                    // do nothing
                    break;
            }
        }

        // Serialize integer into character Span starting with index [start].
        // Number of digits is set by [digits]
        private static void WriteXDigits(Span<char> text, int start, int value, int digits)
        {
            while (digits-- != 0)
            {
                text[start + digits] = (char)(value % 10 + '0');
                value /= 10;
            }
        }

        // Serialize two digit integer into character Span starting with index [start].
        private static void Write2Digits(Span<char> text, int start, int value)
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
    }
}
