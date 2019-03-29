// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class DoubleTests
    {
        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, true)]             // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, true)]    // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, true)]    // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, true)]      // Max Negative Subnormal
        [InlineData(-0.0, true)]                        // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, true)]                         // Positive Zero
        [InlineData(4.94065645841247E-324, true)]       // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, true)]     // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, true)]     // Min Positive Normal
        [InlineData(double.MaxValue, true)]             // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsFinite(double d, bool expected)
        {
            Assert.Equal(expected, double.IsFinite(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, true)]             // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, true)]    // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, true)]    // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, true)]      // Max Negative Subnormal
        [InlineData(-0.0, true)]                        // Negative Zero
        [InlineData(double.NaN, true)]                  // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNegative(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNegative(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, true)]             // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, true)]    // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, false)]     // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, true)]     // Min Positive Normal
        [InlineData(double.MaxValue, true)]             // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNormal(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNormal(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, true)]    // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, true)]      // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, true)]       // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, true)]     // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsSubnormal(double d, bool expected)
        {
            Assert.Equal(expected, double.IsSubnormal(d));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            const NumberStyles DefaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;
            yield return new object[] { "-123", 0, 3, DefaultStyle, null, (double)-12 };
            yield return new object[] { "-123", 1, 3, DefaultStyle, null, (double)123 };
            yield return new object[] { "1E23", 0, 3, DefaultStyle, null, 1E2 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, 123 };
            yield return new object[] { "-Infinity", 1, 8, NumberStyles.Any, NumberFormatInfo.InvariantInfo, double.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, double expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(double.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, double.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, double.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, double.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(double.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => double.Parse(value.AsSpan(), style, provider));

                Assert.False(double.TryParse(value.AsSpan(), style, provider, out double result));
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void TryFormat()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                foreach (var testdata in ToString_TestData_NotNetFramework())
                {
                    double localI = (double)testdata[0];
                    string localFormat = (string)testdata[1];
                    IFormatProvider localProvider = (IFormatProvider)testdata[2];
                    string localExpected = (string)testdata[3];

                    try
                    {
                        char[] actual;
                        int charsWritten;

                        // Just right
                        actual = new char[localExpected.Length];
                        Assert.True(localI.TryFormat(actual.AsSpan(), out charsWritten, localFormat, localProvider));
                        Assert.Equal(localExpected.Length, charsWritten);
                        Assert.Equal(localExpected, new string(actual));

                        // Longer than needed
                        actual = new char[localExpected.Length + 1];
                        Assert.True(localI.TryFormat(actual.AsSpan(), out charsWritten, localFormat, localProvider));
                        Assert.Equal(localExpected.Length, charsWritten);
                        Assert.Equal(localExpected, new string(actual, 0, charsWritten));

                        // Too short
                        if (localExpected.Length > 0)
                        {
                            actual = new char[localExpected.Length - 1];
                            Assert.False(localI.TryFormat(actual.AsSpan(), out charsWritten, localFormat, localProvider));
                            Assert.Equal(0, charsWritten);
                        }
                    }
                    catch (Exception exc)
                    {
                        throw new Exception($"Failed on `{localI}`, `{localFormat}`, `{localProvider}`, `{localExpected}`. {exc}");
                    }
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        public static IEnumerable<object[]> ToStringRoundtrip_TestData()
        {
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.MinValue };
            yield return new object[] { -Math.PI };
            yield return new object[] { -Math.E };
            yield return new object[] { -double.Epsilon };
            yield return new object[] { -0.84551240822557006 };
            yield return new object[] { -0.0 };
            yield return new object[] { double.NaN };
            yield return new object[] { 0.0 };
            yield return new object[] { 0.84551240822557006 };
            yield return new object[] { double.Epsilon };
            yield return new object[] { Math.E };
            yield return new object[] { Math.PI };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip(double value)
        {
            double result = double.Parse(value.ToString());
            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(result));
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip_R(double value)
        {
            double result = double.Parse(value.ToString("R"));
            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(result));
        }
    }
}
