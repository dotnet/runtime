// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema.DateAndTime.Specifications
{
    /// <summary>
    /// This enum specifies what format should be used when converting string to Date and Time.
    /// </summary>
    [Flags]
    internal enum XsdDateAndTimeFlags
    {
        DateTime = 0b_0000_0000_0001,
        Time = 0b_0000_0000_0010,
        Date = 0b_0000_0000_0100,
        GYearMonth = 0b_0000_0000_1000,
        GYear = 0b_0000_0001_0000,
        GMonthDay = 0b_0000_0010_0000,
        GDay = 0b_0000_0100_0000,
        GMonth = 0b_0000_1000_0000,

        XdrDateTimeNoTz = 0b_0001_0000_0000,
        XdrDateTime = 0b_0010_0000_0000,
        XdrTimeNoTz = 0b_0100_0000_0000,  //XDRTime with tz is the same as xsd:time
        AllXsd = DateTime | Time | Date | GYearMonth | GYear | GMonthDay | GDay | GMonth
    }
}
