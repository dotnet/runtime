// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema.DateAndTime.Specifications;

namespace System.Xml.Schema.DateAndTime.Helpers
{
    internal struct DateAndTimeInfo
    {
        public int Day { get; }
        public int Fraction { get; }
        public int Hour { get; }
        public XsdDateTimeKind Kind { get; }
        public int Minute { get; }
        public int Month { get; }
        public int Second { get; }
        public DateTimeTypeCode TypeCode { get; }
        public int Year { get; }
        public int ZoneHour { get; }
        public int ZoneMinute { get; }

        public DateAndTimeInfo(
            int day,
            int fraction,
            int hour,
            XsdDateTimeKind kind,
            int minute,
            int month,
            int second,
            DateTimeTypeCode typeCode,
            int year,
            int zoneHour,
            int zoneMinute)
        {
            Day = day;
            Fraction = fraction;
            Hour = hour;
            Kind = kind;
            Minute = minute;
            Month = month;
            Second = second;
            TypeCode = typeCode;
            Year = year;
            ZoneHour = zoneHour;
            ZoneMinute = zoneMinute;
        }

        public DateAndTimeInfo()
            : this(
                  default,
                  default,
                  default,
                  XsdDateTimeKind.Unspecified,
                  default,
                  default,
                  default,
                  default,
                  default,
                  0,
                  0)
        {
        }
    }
}
