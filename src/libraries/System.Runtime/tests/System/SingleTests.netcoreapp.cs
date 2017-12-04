// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class SingleTests
    {
        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, true)]              // Min Negative Normal
        [InlineData(-1.17549435E-38f, true)]            // Max Negative Normal
        [InlineData(-1.17549421E-38f, true)]            // Min Negative Subnormal
        [InlineData(-1.401298E-45, true)]               // Max Negative Subnormal
        [InlineData(-0.0f, true)]                       // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, true)]                        // Positive Zero
        [InlineData(1.401298E-45, true)]                // Min Positive Subnormal
        [InlineData(1.17549421E-38f, true)]             // Max Positive Subnormal
        [InlineData(1.17549435E-38f, true)]             // Min Positive Normal
        [InlineData(float.MaxValue, true)]              // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsFinite(float d, bool expected)
        {
            Assert.Equal(expected, float.IsFinite(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, true)]      // Negative Infinity
        [InlineData(float.MinValue, true)]              // Min Negative Normal
        [InlineData(-1.17549435E-38f, true)]            // Max Negative Normal
        [InlineData(-1.17549421E-38f, true)]            // Min Negative Subnormal
        [InlineData(-1.401298E-45, true)]               // Max Negative Subnormal
        [InlineData(-0.0f, true)]                       // Negative Zero
        [InlineData(float.NaN, true)]                   // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNegative(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNegative(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, true)]              // Min Negative Normal
        [InlineData(-1.17549435E-38f, true)]            // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-1.401298E-45, false)]              // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, true)]             // Min Positive Normal
        [InlineData(float.MaxValue, true)]              // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNormal(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNormal(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, true)]            // Min Negative Subnormal
        [InlineData(-1.401298E-45, true)]               // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, true)]                // Min Positive Subnormal
        [InlineData(1.17549421E-38f, true)]             // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsSubnormal(float d, bool expected)
        {
            Assert.Equal(expected, float.IsSubnormal(d));
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Span_Valid(string value, NumberStyles style, IFormatProvider provider, float expected)
        {
            Assert.Equal(expected, float.Parse(value.AsReadOnlySpan(), style, provider));

            Assert.True(float.TryParse(value.AsReadOnlySpan(), style, provider, out float result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => float.Parse(value.AsReadOnlySpan(), style, provider));

                Assert.False(float.TryParse(value.AsReadOnlySpan(), style, provider, out float result));
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void TryFormat()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                foreach (var testdata in ToString_TestData())
                {
                    float localI = (float)testdata[0];
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

                return SuccessExitCode;
            }).Dispose();
        }
    }
}
