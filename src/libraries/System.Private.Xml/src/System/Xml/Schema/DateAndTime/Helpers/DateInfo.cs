// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema.DateAndTime.Helpers
{
    internal struct DateInfo
    {
        public static readonly DateInfo DefaultValue = new DateInfo(FirstDay, FirstMonth, LeapYear);

        public const int FirstDay = 1;
        public const int FirstMonth = 1;
        public const int LeapYear = 1904;

        public int Day { get; }
        public int Month { get; }
        public int Year { get; }

        public DateInfo(int day, int month, int year)
        {
            Day = day;
            Month = month;
            Year = year;
        }
    }
}
