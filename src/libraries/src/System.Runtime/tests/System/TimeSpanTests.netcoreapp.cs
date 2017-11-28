// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static partial class TimeSpanTests
    {
        private static IEnumerable<object[]> MultiplicationTestData()
        {
            yield return new object[] {new TimeSpan(2, 30, 0), 2.0, new TimeSpan(5, 0, 0)};
            yield return new object[] {new TimeSpan(14, 2, 30, 0), 192.0, TimeSpan.FromDays(2708)};
            yield return new object[] {TimeSpan.FromDays(366), Math.PI, new TimeSpan(993446995288779)};
            yield return new object[] {TimeSpan.FromDays(366), -Math.E, new TimeSpan(-859585952922633)};
            yield return new object[] {TimeSpan.FromDays(29.530587981), 13.0, TimeSpan.FromDays(383.897643819444)};
            yield return new object[] {TimeSpan.FromDays(-29.530587981), -12.0, TimeSpan.FromDays(354.367055833333)};
            yield return new object[] {TimeSpan.FromDays(-29.530587981), 0.0, TimeSpan.Zero};
            yield return new object[] {TimeSpan.MaxValue, 0.5, TimeSpan.FromTicks((long)(long.MaxValue * 0.5))};
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void Multiplication(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(expected, timeSpan * factor);
            Assert.Equal(expected, factor * timeSpan);
        }

        [Fact]
        public static void OverflowingMultiplication()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue * 1.000000001);
            Assert.Throws<OverflowException>(() => -1.000000001 * TimeSpan.MaxValue);
        }

        [Fact]
        public static void NaNMultiplication()
        {
            AssertExtensions.Throws<ArgumentException>("factor", () => TimeSpan.FromDays(1) * double.NaN);
            AssertExtensions.Throws<ArgumentException>("factor", () => double.NaN * TimeSpan.FromDays(1));
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void Division(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(factor, expected / timeSpan, 14);
            double divisor = 1.0 / factor;
            Assert.Equal(expected, timeSpan / divisor);
        }

        [Fact]
        public static void DivideByZero()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(1) / 0);
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(-1) / 0);
            Assert.Throws<OverflowException>(() => TimeSpan.Zero / 0);
            Assert.Equal(double.PositiveInfinity, TimeSpan.FromDays(1) / TimeSpan.Zero);
            Assert.Equal(double.NegativeInfinity, TimeSpan.FromDays(-1) / TimeSpan.Zero);
            Assert.True(double.IsNaN(TimeSpan.Zero / TimeSpan.Zero));
        }

        [Fact]
        public static void NaNDivision()
        {
            AssertExtensions.Throws<ArgumentException>("divisor", () => TimeSpan.FromDays(1) / double.NaN);
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void NamedMultiplication(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(expected, timeSpan.Multiply(factor));
        }

        [Fact]
        public static void NamedOverflowingMultiplication()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue.Multiply(1.000000001));
        }

        [Fact]
        public static void NamedNaNMultiplication()
        {
            AssertExtensions.Throws<ArgumentException>("factor", () => TimeSpan.FromDays(1).Multiply(double.NaN));
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void NamedDivision(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(factor, expected.Divide(timeSpan), 14);
            double divisor = 1.0 / factor;
            Assert.Equal(expected, timeSpan.Divide(divisor));
        }

        [Fact]
        public static void NamedDivideByZero()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(1).Divide(0));
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(-1).Divide(0));
            Assert.Throws<OverflowException>(() => TimeSpan.Zero.Divide(0));
            Assert.Equal(double.PositiveInfinity, TimeSpan.FromDays(1).Divide(TimeSpan.Zero));
            Assert.Equal(double.NegativeInfinity, TimeSpan.FromDays(-1).Divide(TimeSpan.Zero));
            Assert.True(double.IsNaN(TimeSpan.Zero.Divide(TimeSpan.Zero)));
        }

        [Fact]
        public static void NamedNaNDivision()
        {
            AssertExtensions.Throws<ArgumentException>("divisor", () => TimeSpan.FromDays(1).Divide(double.NaN));
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Span(string inputString, IFormatProvider provider, TimeSpan expected)
        {
            ReadOnlySpan<char> input = inputString.AsReadOnlySpan();
            TimeSpan result;

            Assert.Equal(expected, TimeSpan.Parse(input, provider));
            Assert.True(TimeSpan.TryParse(input, provider, out result));
            Assert.Equal(expected, result);

            // Also negate
            if (!char.IsWhiteSpace(input[0]))
            {
                input = ("-" + inputString).AsReadOnlySpan();
                expected = -expected;

                Assert.Equal(expected, TimeSpan.Parse(input, provider));
                Assert.True(TimeSpan.TryParse(input, provider, out result));
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string inputString, IFormatProvider provider, Type exceptionType)
        {
            if (inputString != null)
            {
                Assert.Throws(exceptionType, () => TimeSpan.Parse(inputString.AsReadOnlySpan(), provider));
                Assert.False(TimeSpan.TryParse(inputString.AsReadOnlySpan(), provider, out TimeSpan result));
                Assert.Equal(TimeSpan.Zero, result);
            }
        }



        [Theory]
        [MemberData(nameof(ParseExact_Valid_TestData))]
        public static void ParseExact_Span_Valid(string inputString, string format, TimeSpan expected)
        {
            ReadOnlySpan<char> input = inputString.AsReadOnlySpan();

            TimeSpan result;
            Assert.Equal(expected, TimeSpan.ParseExact(input, format, new CultureInfo("en-US")));
            Assert.Equal(expected, TimeSpan.ParseExact(input, new[] { format }, new CultureInfo("en-US")));

            Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), out result));
            Assert.Equal(expected, result);

            Assert.True(TimeSpan.TryParseExact(input, new[] { format }, new CultureInfo("en-US"), out result));
            Assert.Equal(expected, result);

            if (format != "c" && format != "t" && format != "T" && format != "g" && format != "G")
            {
                // TimeSpanStyles is interpreted only for custom formats
                Assert.Equal(expected.Negate(), TimeSpan.ParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative));

                Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative, out result));
                Assert.Equal(expected.Negate(), result);
            }
            else
            {
                // Inputs that can be parsed in standard formats with ParseExact should also be parsable with Parse
                Assert.Equal(expected, TimeSpan.Parse(input, CultureInfo.InvariantCulture));

                Assert.True(TimeSpan.TryParse(input, CultureInfo.InvariantCulture, out result));
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_Invalid_TestData))]
        public static void ParseExactTest_Span_Invalid(string inputString, string format, Type exceptionType)
        {
            if (inputString != null && format != null)
            {
                Assert.Throws(exceptionType, () => TimeSpan.ParseExact(inputString.AsReadOnlySpan(), format, new CultureInfo("en-US")));

                TimeSpan result;
                Assert.False(TimeSpan.TryParseExact(inputString.AsReadOnlySpan(), format, new CultureInfo("en-US"), out result));
                Assert.Equal(TimeSpan.Zero, result);

                Assert.False(TimeSpan.TryParseExact(inputString.AsReadOnlySpan(), new[] { format }, new CultureInfo("en-US"), out result));
                Assert.Equal(TimeSpan.Zero, result);
            }
        }

        [Fact]
        public static void ParseExactMultiple_Span_InvalidNullEmptyFormats()
        {
            TimeSpan result;

            AssertExtensions.Throws<ArgumentNullException>("formats", () => TimeSpan.ParseExact("12:34:56".AsReadOnlySpan(), (string[])null, null));
            Assert.False(TimeSpan.TryParseExact("12:34:56".AsReadOnlySpan(), (string[])null, null, out result));

            Assert.Throws<FormatException>(() => TimeSpan.ParseExact("12:34:56".AsReadOnlySpan(), new string[0], null));
            Assert.False(TimeSpan.TryParseExact("12:34:56".AsReadOnlySpan(), new string[0], null, out result));
        }

        [Theory]
        [MemberData(nameof(ToString_MemberData))]
        public static void TryFormat_Valid(TimeSpan input, string format, string expected)
        {
            int charsWritten;
            Span<char> dst;

            dst = new char[expected.Length - 1];
            Assert.False(input.TryFormat(dst, out charsWritten, format, CultureInfo.InvariantCulture));
            Assert.Equal(0, charsWritten);

            dst = new char[expected.Length];
            Assert.True(input.TryFormat(dst, out charsWritten, format, CultureInfo.InvariantCulture));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(dst));

            dst = new char[expected.Length + 1];
            Assert.True(input.TryFormat(dst, out charsWritten, format, CultureInfo.InvariantCulture));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(dst.Slice(0, dst.Length - 1)));
            Assert.Equal(0, dst[dst.Length - 1]);
        }
    }
}
