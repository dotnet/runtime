// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Tests
{
    internal class JsonDateTimeTestData
    {
        // Test string, Argument to DateTime(Offset).Parse(Exact)
        public static IEnumerable<object[]> ValidISO8601Tests()
        {
            yield return new object[] { "\"0997-07-16\"", "0997-07-16" };
            yield return new object[] { "\"1997-07-16\"", "1997-07-16" };
            yield return new object[] { "\"1997-07-16T19:20\"", "1997-07-16T19:20" };
            yield return new object[] { "\"1997-07-16T19:20:30\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.45\"", "1997-07-16T19:20:30.45" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555\"", "1997-07-16T19:20:30.4555555" };

            // Test fractions.
            yield return new object[] { "\"1997-07-16T19:20:30.0\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.000\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.0000000\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.01\"", "1997-07-16T19:20:30.01" };
            yield return new object[] { "\"1997-07-16T19:20:30.0001\"", "1997-07-16T19:20:30.0001" };
            yield return new object[] { "\"1997-07-16T19:20:30.0000001\"", "1997-07-16T19:20:30.0000001" };
            yield return new object[] { "\"1997-07-16T19:20:30.0000323\"", "1997-07-16T19:20:30.0000323" };
            yield return new object[] { "\"1997-07-16T19:20:30.1\"", "1997-07-16T19:20:30.1" };
            yield return new object[] { "\"1997-07-16T19:20:30.22\"", "1997-07-16T19:20:30.22" };
            yield return new object[] { "\"1997-07-16T19:20:30.333\"", "1997-07-16T19:20:30.333" };
            yield return new object[] { "\"1997-07-16T19:20:30.4444\"", "1997-07-16T19:20:30.4444" };
            yield return new object[] { "\"1997-07-16T19:20:30.55555\"", "1997-07-16T19:20:30.55555" };
            yield return new object[] { "\"1997-07-16T19:20:30.666666\"", "1997-07-16T19:20:30.666666" };
            yield return new object[] { "\"1997-07-16T19:20:30.7777777\"", "1997-07-16T19:20:30.7777777" };
            yield return new object[] { "\"1997-07-16T19:20:30.1000000\"", "1997-07-16T19:20:30.1" };
            yield return new object[] { "\"1997-07-16T19:20:30.2200000\"", "1997-07-16T19:20:30.22" };
            yield return new object[] { "\"1997-07-16T19:20:30.3330000\"", "1997-07-16T19:20:30.333" };
            yield return new object[] { "\"1997-07-16T19:20:30.4444000\"", "1997-07-16T19:20:30.4444" };
            yield return new object[] { "\"1997-07-16T19:20:30.5555500\"", "1997-07-16T19:20:30.55555" };
            yield return new object[] { "\"1997-07-16T19:20:30.6666660\"", "1997-07-16T19:20:30.666666" };

            // Test fraction truncation.
            yield return new object[] { "\"1997-07-16T19:20:30.00000001\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.000000001\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.77777770\"", "1997-07-16T19:20:30.7777777" };
            yield return new object[] { "\"1997-07-16T19:20:30.777777700\"", "1997-07-16T19:20:30.7777777" };
            yield return new object[] { "\"1997-07-16T19:20:30.45555554\"", "1997-07-16T19:20:30.45555554" };
            // We expect the parser to truncate. `DateTime(Offset).Parse` will round up to 7dp in these cases,
            // so we pass a string representing the Datetime(Offset) we expect to the `Parse` method.
            yield return new object[] { "\"1997-07-16T19:20:30.45555555\"", "1997-07-16T19:20:30.4555555" };
            yield return new object[] { "\"1997-07-16T19:20:30.455555555\"", "1997-07-16T19:20:30.4555555" };
            yield return new object[] { "\"1997-07-16T19:20:30.0000000000000\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.00000000000001\"", "1997-07-16T19:20:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555555\"", "1997-07-16T19:20:30.4555555" };
            yield return new object[] { "\"1997-07-16T19:20:30.45555555555\"", "1997-07-16T19:20:30.4555555" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555555555555\"", "1997-07-16T19:20:30.4555555" };

            // Test Non-UTC timezone designator (TZD).
            yield return new object[] { "\"1997-07-16T19:20+01:00\"", "1997-07-16T19:20+01:00" };
            yield return new object[] { "\"1997-07-16T19:20-01:00\"", "1997-07-16T19:20-01:00" };
            yield return new object[] { "\"1997-07-16T19:20:30+01:00\"", "1997-07-16T19:20:30+01:00" };
            yield return new object[] { "\"1997-07-16T19:20:30-01:00\"", "1997-07-16T19:20:30-01:00" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+01:00\"", "1997-07-16T19:20:30.4555555+01:00" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-01:00\"", "1997-07-16T19:20:30.4555555-01:00" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+04:30\"", "1997-07-16T19:20:30.4555555+04:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-04:30\"", "1997-07-16T19:20:30.4555555-04:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555555555555+01:30\"", "1997-07-16T19:20:30.4555555+01:30" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555555555555+01\"", "1997-07-16T19:20:30.4555555+01:00" };
            // Test Non-UTC TZD without minute.
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+01\"", "1997-07-16T19:20:30.4555555+01:00" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-01\"", "1997-07-16T19:20:30.4555555-01:00" };
            // Test Non-UTC TZD with max UTC offset hour.
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+14:00\"", "1997-07-16T19:20:30.4555555+14:00" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-14:00\"", "1997-07-16T19:20:30.4555555-14:00" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+14\"", "1997-07-16T19:20:30.4555555+14:00" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-14\"", "1997-07-16T19:20:30.4555555-14:00" };
            // Test February 29 on a leap year
            yield return new object[] { "\"2020-02-29T19:20:30.4555555+10:00\"", "2020-02-29T19:20:30.4555555+10:00" };

            // Hex string
            yield return new object[] { "\"\\u0031\\u0039\\u0039\\u0037\\u002d\\u0030\\u0037\\u002d\\u0031\\u0036\\u0054\\u0031\\u0039\\u003a\\u0032\\u0030\\u003a\\u0033\\u0030\\u002e\\u0034\\u0035\\u0035\\u0035\\u0035\\u0035\\u0035\"", "1997-07-16T19:20:30.4555555" };

            // Mix of hex and plain string
            yield return new object[] { "\"1997\\u002d07\\u002d16T19:20:30\\u002e4555\\u003555+14\"", "1997-07-16T19:20:30.4555555+14:00" };
        }

        // UTC TZD tests are separate because `DateTime.Parse` for strings with `Z` TZD will return
        // a `DateTime` with `DateTimeKind.Local` i.e `+00:00` which does not equal our expected result,
        // a `DateTime` with `DateTimeKind.Utc` i.e `Z`.
        // Instead, we need to use `DateTime.ParseExact` which returns a DateTime Utc `DateTimeKind`.
        //
        // Test string, Argument to DateTime(Offset).Parse(Exact)
        public static IEnumerable<object[]> ValidISO8601TestsWithUtcOffset()
        {
            yield return new object[] { "\"1997-07-16T19:20Z\"", "1997-07-16T19:20:00.0000000Z" };
            yield return new object[] { "\"1997-07-16T19:20:30Z\"", "1997-07-16T19:20:30.0000000Z" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555Z\"", "1997-07-16T19:20:30.4555555Z" };
            yield return new object[] { "\"1997-07-16T19:20:30.455555555Z\"", "1997-07-16T19:20:30.4555555Z" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555555555555Z\"", "1997-07-16T19:20:30.4555555Z" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555555000000Z\"", "1997-07-16T19:20:30.4555555Z" };
            // Hex string
            yield return new object[] { "\"1\\u003997-07-16T19:2\\u0030:30.4555555Z\"", "1997-07-16T19:20:30.4555555Z" };
        }

        public static IEnumerable<object[]> InvalidISO8601Tests()
        {
            // Too short
            yield return new object[] { "\"1997-07\"" };
            yield return new object[] { "\"1996\"" };
            yield return new object[] { "\"997-07-16\"" };
            yield return new object[] { "\"1997-07-6\"" };
            yield return new object[] { "\"1997-7-06\"" };

            // Invalid YYYY-MM-DD
            yield return new object[] { "\"0997 07-16\"" };
            yield return new object[] { "\"0997-0a-16\"" };
            yield return new object[] { "\"0997-07 16\"" };
            yield return new object[] { "\"0997-07-160997-07-16\"" };
            yield return new object[] { "\"0997-07-16abc\"" };
            yield return new object[] { "\"0997-07-16 \"" };
            yield return new object[] { "\"0997-07-16,0997-07-16\"" };
            yield return new object[] { "\"1997-07-16T19:20abc\"" };
            yield return new object[] { "\"1997-07-16T19:20, 123\"" };
            yield return new object[] { "\"1997-07-16T\"" };
            yield return new object[] { "\"1997-07-16*\"" };
            yield return new object[] { "\"1997-07-6T01\"" };
            yield return new object[] { "\"1997-07-16Z\"" };
            yield return new object[] { "\"1997-07-16+01:00\"" };
            yield return new object[] { "\"19970716\"" };
            yield return new object[] { "\"0997-07-166\"" };
            yield return new object[] { "\"1997-07-1sdsad\"" };
            yield return new object[] { "\"1997-07-16T19200\"" };

            // Invalid YYYY-MM-DDThh:mm
            yield return new object[] { "\"1997-07-16T1\"" };
            yield return new object[] { "\"1997-07-16Ta0:00\"" };
            yield return new object[] { "\"1997-07-16T.9:20\"" };
            yield return new object[] { "\"1997-07-16T19: 20:30\"" };
            yield return new object[] { "\"1997-07-16 19:20:30\"" };
            yield return new object[] { "\"1997-07-16T19:2030\"" };
            // 24:00 is not supported per ISO 8601-1:2019
            yield return new object[] { "\"1997-07-16T24:00\"" };

            // Invalid YYYY-MM-DDThh:mm:ss
            yield return new object[] { "\"1997-07-16T19:20a\"" };
            yield return new object[] { "\"1997-07-16T19:20:\"" };
            yield return new object[] { "\"1997-07-16T19:20:a\"" };
            yield return new object[] { "\"1997-07-16T19:20:3a\"" };
            yield return new object[] { "\"1997-07-16T19:20:30a\"" };
            yield return new object[] { "\"1997-07-16T19:20:3045\"" };
            yield return new object[] { "\"1997-07-16T19:20:304555555\"" };

            // Invalid fractions.
            yield return new object[] { "\"1997-07-16T19.45\"" };
            yield return new object[] { "\"1997-07-16T19:20.45\"" };
            yield return new object[] { "\"1997-07-16T19:20:30,45\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.a\"" };
            yield return new object[] { "\"1997-07-16T19:20:30 .\"" };
            yield return new object[] { "\"abc1997-07-16T19:20:30.000\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+\"" };

            // Invalid TZD.
            yield return new object[] { "\"1997-07-16T19:20:30.4555555a\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+-Z\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555Z \"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+01Z\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+01:\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555 +01:00\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555- 01:00\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+04 :30\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-04: 30\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+0100 \"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-010\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+430\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555--0430\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+0\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-01005\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+14:00a\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-14:0\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-14:00 \"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+14 00\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+0100\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-0100\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+0430\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-0430\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555555555555-0100\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+1400\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-1400\"" };


            // Proper format but invalid calendar date, time, or time zone designator fields
            yield return new object[] { "\"1997-00-16T19:20:30.4555555\"" };
            yield return new object[] { "\"1997-07-16T25:20:30.4555555\"" };
            yield return new object[] { "\"1997-00-16T19:20:30.4555555Z\"" };
            yield return new object[] { "\"1997-07-16T25:20:30.4555555Z\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+14:30\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-14:30\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+15:00\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555-15:00\"" };
            yield return new object[] { "\"1997-00-16T19:20:30.4555555+10:00\"" };
            yield return new object[] { "\"1997-07-16T25:20:30.4555555+10:00\"" };
            yield return new object[] { "\"1997-07-16T19:60:30.4555555+10:00\"" };
            yield return new object[] { "\"1997-07-16T19:20:60.4555555+10:00\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.4555555+10:60\"" };
            yield return new object[] { "\"0000-07-16T19:20:30.4555555+10:00\"" };
            yield return new object[] { "\"2019-02-29T19:20:30.4555555+10:00\"" };
            yield return new object[] { "\"9999-12-31T23:59:59.9999999-01:00\"" }; // This date spills over to year 10_000.

            // Too long
            yield return new object[] { "\"1997-07-16T19:20:30.00000000000000000\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.00000000000000001\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.000000000000000001\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.45555555550000000\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.45555555555555555\"" };
            yield return new object[] { "\"1997-07-16T19:20:30.45555555555555555555\"" };

            // Hex strings

            // Invalid YYYY-MM-DD 0997-07-16
            yield return new object[] { "\"0997\\u002007-16\"" };
            // Invalid YYYY-MM-DDThh:mm:ss 1997-07-16T19:20:3045
            yield return new object[] { "\"\\u0031\\u0039\\u0039\\u0037\\u002d\\u0030\\u0037\\u002d\\u0031\\u0036\\u0054\\u0031\\u0039\\u003a\\u0032\\u0030\\u003a\\u0033\\u0030\\u0034\\u0035\"" };
            // Invalid fraction 1997-07-16T19:20:30,45
            yield return new object[] { "\"1997-07-16T19:20:30\\u002c45\"" };
            // Invalid TZD 1997-07-16T19:20:30.4555555+-Z
            yield return new object[] { "\"199\\u0037-07\\u002d16T1\\u0039:20:30.4555555+\\u002dZ\"" };
            // Proper format but invalid calendar date, time, or time zone designator fields 1997-00-16
            yield return new object[] { "\"\\u0031\\u0039\\u0039\\u0037\\u002d\\u0030\\u0030\\u002d\\u0031\\u0036\"" };

            // High byte expansion - parsing fails early at 254 characters.
            yield return new object[] { "\"" + new string('\u20AC', 250) + "\"" };
            yield return new object[] { "\"" + new string('\u20AC', 260) + "\"" };
        }

        public static IEnumerable<object[]> DateTimeFractionTrimBaseTests()
        {
            yield return new object[] { "2019-04-24T14:50:17.0000000", "2019-04-24T14:50:17" };
            yield return new object[] { "2019-04-24T14:50:17.1000000", "2019-04-24T14:50:17.1" };
            yield return new object[] { "2019-04-24T14:50:17.1100000", "2019-04-24T14:50:17.11" };
            yield return new object[] { "2019-04-24T14:50:17.1110000", "2019-04-24T14:50:17.111" };
            yield return new object[] { "2019-04-24T14:50:17.1111000", "2019-04-24T14:50:17.1111" };
            yield return new object[] { "2019-04-24T14:50:17.1111100", "2019-04-24T14:50:17.11111" };
            yield return new object[] { "2019-04-24T14:50:17.1111110", "2019-04-24T14:50:17.111111" };
            yield return new object[] { "2019-04-24T14:50:17.1111111", "2019-04-24T14:50:17.1111111" };
            yield return new object[] { "2019-04-24T14:50:17.0000001", "2019-04-24T14:50:17.0000001" };
            yield return new object[] { "2019-04-24T14:50:17.0000010", "2019-04-24T14:50:17.000001" };
            yield return new object[] { "2019-04-24T14:50:17.0000100", "2019-04-24T14:50:17.00001" };
            yield return new object[] { "2019-04-24T14:50:17.0001000", "2019-04-24T14:50:17.0001" };
            yield return new object[] { "2019-04-24T14:50:17.0010000", "2019-04-24T14:50:17.001" };
            yield return new object[] { "2019-04-24T14:50:17.0100000", "2019-04-24T14:50:17.01" };
        }

        public static IEnumerable<object[]> DateTimeFractionTrimUtcOffsetTests()
        {
            foreach (object[] test in DateTimeFractionTrimBaseTests())
            {
                yield return new object[] { $"{(string)(test[0])}Z", $"{(string)(test[1])}Z" };
            }
        }

        public static IEnumerable<object[]> DateTimeOffsetFractionTrimTests()
        {
            foreach (object[] test in DateTimeFractionTrimBaseTests())
            {
                yield return new object[] { $"{(string)(test[0])}+01:00", $"{(string)(test[1])}+01:00" };
            }
        }
    }
}
