// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema.DateAndTime.Specifications;

namespace System.Xml.Schema.DateAndTime.Helpers
{
    internal struct DateAndTimeInfo
    {
        public DateInfo Date { get; }
        public XsdDateTimeKind Kind { get; }
        public TimeInfo Time { get; }
        public DateTimeTypeCode TypeCode { get; }
        public int ZoneHour { get; }
        public int ZoneMinute { get; }

        public DateAndTimeInfo(
            DateInfo date,
            XsdDateTimeKind kind,
            TimeInfo time,
            DateTimeTypeCode typeCode,
            int zoneHour,
            int zoneMinute)
        {
            Date = date;
            Kind = kind;
            Time = time;
            TypeCode = typeCode;
            ZoneHour = zoneHour;
            ZoneMinute = zoneMinute;
        }

        public DateAndTimeInfo()
            : this(
                  default,
                  XsdDateTimeKind.Unspecified,
                  default,
                  default,
                  0,
                  0)
        {
        }
    }
}
