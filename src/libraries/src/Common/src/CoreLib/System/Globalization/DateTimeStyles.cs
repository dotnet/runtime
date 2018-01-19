// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Contains valid formats for DateTime recognized by
** the DateTime class' parsing code.
**
**
===========================================================*/

namespace System.Globalization
{
    [Flags]
    public enum DateTimeStyles
    {
        // Bit flag indicating that leading whitespace is allowed. Character values
        // 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, and 0x0020 are considered to be
        // whitespace.


        None = 0x00000000,

        AllowLeadingWhite = 0x00000001,

        AllowTrailingWhite = 0x00000002, //Bitflag indicating trailing whitespace is allowed.

        AllowInnerWhite = 0x00000004,

        AllowWhiteSpaces = AllowLeadingWhite | AllowInnerWhite | AllowTrailingWhite,
        // When parsing a date/time string, if all year/month/day are missing, set the default date
        // to 0001/1/1, instead of the current year/month/day.

        NoCurrentDateDefault = 0x00000008,
        // When parsing a date/time string, if a timezone specifier ("GMT","Z","+xxxx", "-xxxx" exists), we will
        // ajdust the parsed time based to GMT.

        AdjustToUniversal = 0x00000010,

        AssumeLocal = 0x00000020,

        AssumeUniversal = 0x00000040,
        // Attempt to preserve whether the input is unspecified, local or UTC
        RoundtripKind = 0x00000080,
    }
}
