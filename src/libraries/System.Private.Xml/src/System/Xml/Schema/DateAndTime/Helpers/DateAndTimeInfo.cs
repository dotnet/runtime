// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema.DateAndTime.Specifications;

namespace System.Xml.Schema.DateAndTime.Helpers
{
    internal struct DateAndTimeInfo
    {
        public DateInfo Date { get; }
        public int Fraction { get; }
        public int Hour { get; }
        public XsdDateTimeKind Kind { get; }
        public int Minute { get; }
        public int Second { get; }
        public DateTimeTypeCode TypeCode { get; }
        public int ZoneHour { get; }
        public int ZoneMinute { get; }

        public DateAndTimeInfo(
            DateInfo date,
            int fraction,
            int hour,
            XsdDateTimeKind kind,
            int minute,
            int second,
            DateTimeTypeCode typeCode,
            int zoneHour,
            int zoneMinute)
        {
            Date = date;
            Fraction = fraction;
            Hour = hour;
            Kind = kind;
            Minute = minute;
            Second = second;
            TypeCode = typeCode;
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
                  0,
                  0)
        {
        }
    }
}
