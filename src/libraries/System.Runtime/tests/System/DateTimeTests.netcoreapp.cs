// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static partial class DateTimeTests
    {
        [Theory]
        [MemberData(nameof(StandardFormatSpecifiers))]
        public static void TryFormat_MatchesToString(string format)
        {
            DateTime dt = DateTime.UtcNow;
            string expected = dt.ToString(format);

            // Just the right length, succeeds
            Span<char> dest = new char[expected.Length];
            Assert.True(dt.TryFormat(dest, out int charsWritten, format));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal<char>(expected.ToCharArray(), dest.ToArray());

            // Too short, fails
            dest = new char[expected.Length - 1];
            Assert.False(dt.TryFormat(dest, out charsWritten, format));
            Assert.Equal(0, charsWritten);

            // Longer than needed, succeeds
            dest = new char[expected.Length + 1];
            Assert.True(dt.TryFormat(dest, out charsWritten, format));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal<char>(expected.ToCharArray(), dest.Slice(0, expected.Length).ToArray());
            Assert.Equal(0, dest[dest.Length - 1]);
        }

        [Theory]
        [MemberData(nameof(Parse_ValidInput_Suceeds_MemberData))]
        public static void Parse_Span_ValidInput_Suceeds(string input, CultureInfo culture, DateTime? expected)
        {
            Assert.Equal(expected, DateTime.Parse(input.AsReadOnlySpan(), culture));
        }

        [Theory]
        [MemberData(nameof(ParseExact_ValidInput_Succeeds_MemberData))]
        public static void ParseExact_Span_ValidInput_Succeeds(string input, string format, CultureInfo culture, DateTimeStyles style, DateTime? expected)
        {
            DateTime result1 = DateTime.ParseExact(input.AsReadOnlySpan(), format, culture, style);
            DateTime result2 = DateTime.ParseExact(input.AsReadOnlySpan(), new[] { format }, culture, style);

            Assert.True(DateTime.TryParseExact(input.AsReadOnlySpan(), format, culture, style, out DateTime result3));
            Assert.True(DateTime.TryParseExact(input.AsReadOnlySpan(), new[] { format }, culture, style, out DateTime result4));

            Assert.Equal(result1, result2);
            Assert.Equal(result1, result3);
            Assert.Equal(result1, result4);

            if (expected != null) // some inputs don't roundtrip well
            {
                // Normalize values to make comparison easier
                if (expected.Value.Kind != DateTimeKind.Utc)
                {
                    expected = expected.Value.ToUniversalTime();
                }
                if (result1.Kind != DateTimeKind.Utc)
                {
                    result1 = result1.ToUniversalTime();
                }

                Assert.Equal(expected, result1);
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_InvalidInputs_Fail_MemberData))]
        public static void ParseExact_Span_InvalidInputs_Fail(string input, string format, CultureInfo culture, DateTimeStyles style)
        {
            Assert.Throws<FormatException>(() => DateTime.ParseExact(input.AsReadOnlySpan(), format, culture, style));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(input.AsReadOnlySpan(), new[] { format }, culture, style));

            Assert.False(DateTime.TryParseExact(input.AsReadOnlySpan(), format, culture, style, out DateTime result));
            Assert.False(DateTime.TryParseExact(input.AsReadOnlySpan(), new[] { format }, culture, style, out result));
        }

        [Fact]
        public static void UnixEpoch()
        {
            VerifyDateTime(DateTime.UnixEpoch, 1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
