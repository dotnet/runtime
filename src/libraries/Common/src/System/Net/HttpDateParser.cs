// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Net
{
    internal static class HttpDateParser
    {
        private static readonly string[] s_dateFormats = new string[] {
            // "r", // RFC 1123, required output format but too strict for input
            "ddd, d MMM yyyy H:m:s 'GMT'", // RFC 1123 (r, except it allows both 1 and 01 for date and time)
            "ddd, d MMM yyyy H:m:s 'UTC'", // RFC 1123, UTC
            "ddd, d MMM yyyy H:m:s", // RFC 1123, no zone - assume GMT
            "d MMM yyyy H:m:s 'GMT'", // RFC 1123, no day-of-week
            "d MMM yyyy H:m:s 'UTC'", // RFC 1123, UTC, no day-of-week
            "d MMM yyyy H:m:s", // RFC 1123, no day-of-week, no zone
            "ddd, d MMM yy H:m:s 'GMT'", // RFC 1123, short year
            "ddd, d MMM yy H:m:s 'UTC'", // RFC 1123, UTC, short year
            "ddd, d MMM yy H:m:s", // RFC 1123, short year, no zone
            "d MMM yy H:m:s 'GMT'", // RFC 1123, no day-of-week, short year
            "d MMM yy H:m:s 'UTC'", // RFC 1123, UTC, no day-of-week, short year
            "d MMM yy H:m:s", // RFC 1123, no day-of-week, short year, no zone

            "dddd, d'-'MMM'-'yy H:m:s 'GMT'", // RFC 850
            "dddd, d'-'MMM'-'yy H:m:s 'UTC'", // RFC 850, UTC
            "dddd, d'-'MMM'-'yy H:m:s zzz", // RFC 850, offset
            "dddd, d'-'MMM'-'yy H:m:s", // RFC 850 no zone
            "ddd MMM d H:m:s yyyy", // ANSI C's asctime() format

            "ddd, d MMM yyyy H:m:s zzz", // RFC 5322
            "ddd, d MMM yyyy H:m:s", // RFC 5322 no zone
            "d MMM yyyy H:m:s zzz", // RFC 5322 no day-of-week
            "d MMM yyyy H:m:s", // RFC 5322 no day-of-week, no zone
        };

        internal static bool TryParse(ReadOnlySpan<char> input, out DateTimeOffset result)
        {
            // None of the relevant patterns have whitespace at the beginning or end, so trim the input of
            // any whitespace.  We can then use strict "r" matching, or if we have to fall back to trying
            // lots of patterns, only allow inner whitespace rather than leading or trailing whitespace.
            input = input.Trim();

            // First try strict parsing for "r" with no options, as it's an order of magnitude faster than general parsing for
            // any individual format in s_dateFormats, allocation-free, and also likely to succeed.  If it doesn't, then
            // fall back to trying each of the various date formats listed earlier, in order, to be accomodating and
            // accept a wide variety of old formats.
            return
                DateTimeOffset.TryParseExact(input, "r", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out result) ||
                DateTimeOffset.TryParseExact(input, s_dateFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowInnerWhite | DateTimeStyles.AssumeUniversal, out result);
        }
    }
}
