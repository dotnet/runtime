// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Tests.System.Net.Http
{
    public static class HttpDateParserTests
    {
        public static IEnumerable<object[]> TryStringToDate_Data()
        {
            DateTimeOffset zeroOffset = new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero);

            // RFC1123 date/time value
            yield return new object[] { "Sun, 06 Nov 1994 08:49:37 GMT", zeroOffset };
            yield return new object[] { "Sun, 06 Nov 1994 08:49:37 UTC", zeroOffset };
            yield return new object[] { "Sun, 06 Nov 1994 08:49:37", zeroOffset };
            yield return new object[] { "6 Nov 1994 8:49:37 GMT", zeroOffset };
            yield return new object[] { "6 Nov 1994 8:49:37 UTC", zeroOffset };
            yield return new object[] { "6 Nov 1994 8:49:37", zeroOffset };
            yield return new object[] { "Sun, 06 Nov 94 08:49:37", zeroOffset };
            yield return new object[] { "6 Nov 94 8:49:37", zeroOffset };

            // RFC850 date/time value
            yield return new object[] { "Sunday, 06-Nov-94 08:49:37 GMT", zeroOffset };
            yield return new object[] { "Sunday, 06-Nov-94 08:49:37 UTC", zeroOffset };
            yield return new object[] { "Sunday, 6-Nov-94 8:49:37", zeroOffset };

            // ANSI C's asctime() format
            yield return new object[] { "Sun Nov  06 08:49:37 1994", zeroOffset };
            yield return new object[] { "Sun Nov  6 8:49:37 1994", zeroOffset };

            // RFC5322 date/time
            DateTimeOffset offset = new DateTimeOffset(1997, 11, 8, 9, 55, 6, new TimeSpan(-6, 0, 0));
            yield return new object[] { "Sat, 08 Nov 1997 09:55:06 -0600", new DateTimeOffset(1997, 11, 8, 9, 55, 6, new TimeSpan(-6, 0, 0)) };
            yield return new object[] { "8 Nov 1997 9:55:6", new DateTimeOffset(1997, 11, 8, 9, 55, 6, TimeSpan.Zero) };
            yield return new object[] { "Sat, 8 Nov 1997 9:55:6 +0200", new DateTimeOffset(1997, 11, 8, 9, 55, 6, new TimeSpan(2, 0, 0)) };
        }

        [Theory]
        [MemberData(nameof(TryStringToDate_Data))]
        // We don't need extensive tests, since we let DateTimeOffset do the parsing. This test is just
        // to validate that we use the correct parameters when calling into DateTimeOffset.ToString().
        public static void TryStringToDate_UseOfValidDateTimeStringsInDifferentFormats_ParsedCorrectly(string input, DateTimeOffset expected)
        {
            Assert.True(HttpDateParser.TryStringToDate(input, out var result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Sun, 06 Nov 1994 08:49:37 GMT invalid")]
        [InlineData("Sun, 06 Nov 1994 08:49:37 GMT,")]
        [InlineData(",Sun, 06 Nov 1994 08:49:37 GMT")]
        public static void TryStringToDate_UseInvalidDateTimeString(string invalid)
        {
            Assert.False(HttpDateParser.TryStringToDate(",Sun, 06 Nov 1994 08:49:37 GMT", out var result));
        }

        [Fact]
        public static void DateToString_UseRfcSampleTimestamp_FormattedAccordingToRfc1123()
        {
            // We don't need extensive tests, since we let DateTimeOffset do the formatting. This test is just
            // to validate that we use the correct parameters when calling into DateTimeOffset.ToString().
            DateTimeOffset dateTime = new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero);
            Assert.Equal("Sun, 06 Nov 1994 08:49:37 GMT", HttpDateParser.DateToString(dateTime));
        }
    }
}
