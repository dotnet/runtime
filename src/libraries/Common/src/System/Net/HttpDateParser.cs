// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        // Try the various date formats in the order listed above.
        // We should accept a wide variety of common formats, but only output RFC 1123 style dates.
        internal static bool TryStringToDate(ReadOnlySpan<char> input, out DateTimeOffset result) =>
             DateTimeOffset.TryParseExact(
                 input,
                 s_dateFormats,
                 DateTimeFormatInfo.InvariantInfo,
                 DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                 out result);

        // Format according to RFC1123; 'r' uses invariant info (DateTimeFormatInfo.InvariantInfo).
        internal static string DateToString(DateTimeOffset dateTime) =>
            dateTime.ToUniversalTime().ToString("r");
    }
}
