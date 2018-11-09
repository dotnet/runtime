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

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            const NumberStyles DefaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            yield return new object[] { "-123", 1, 3, DefaultStyle, null, (float)123 };
            yield return new object[] { "-123", 0, 3, DefaultStyle, null, (float)-12 };
            yield return new object[] { "1E23", 0, 3, DefaultStyle, null, (float)1E2 };
            yield return new object[] { "123", 0, 2, NumberStyles.Float, new NumberFormatInfo(), (float)12 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$", CurrencyGroupSeparator = "," }, (float)10 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, (float)123 };
            yield return new object[] { "-Infinity", 1, 8, NumberStyles.Any, NumberFormatInfo.InvariantInfo, float.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, float expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            float result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(float.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, float.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, float.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, float.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(float.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => float.Parse(value.AsSpan(), style, provider));

                Assert.False(float.TryParse(value.AsSpan(), style, provider, out float result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [InlineData("0.0", 0x00000000)]
        // Verify small and large exactly representable integers:
        [InlineData("1", 0x3f800000)]
        [InlineData("2", 0x40000000)]
        [InlineData("3", 0x40400000)]
        [InlineData("4", 0x40800000)]
        [InlineData("5", 0x40A00000)]
        [InlineData("6", 0x40C00000)]
        [InlineData("7", 0x40E00000)]
        [InlineData("8", 0x41000000)]
        [InlineData("16777208", 0x4b7ffff8)]
        [InlineData("16777209", 0x4b7ffff9)]
        [InlineData("16777210", 0x4b7ffffa)]
        [InlineData("16777211", 0x4b7ffffb)]
        [InlineData("16777212", 0x4b7ffffc)]
        [InlineData("16777213", 0x4b7ffffd)]
        [InlineData("16777214", 0x4b7ffffe)]
        [InlineData("16777215", 0x4b7fffff)] // 2^24 - 1
        // Verify the smallest and largest denormal values:
        [InlineData("1.4012984643248170e-45", 0x00000001)]
        [InlineData("2.8025969286496340e-45", 0x00000002)]
        [InlineData("4.2038953929744510e-45", 0x00000003)]
        [InlineData("5.6051938572992680e-45", 0x00000004)]
        [InlineData("7.0064923216240850e-45", 0x00000005)]
        [InlineData("8.4077907859489020e-45", 0x00000006)]
        [InlineData("9.8090892502737200e-45", 0x00000007)]
        [InlineData("1.1210387714598537e-44", 0x00000008)]
        [InlineData("1.2611686178923354e-44", 0x00000009)]
        [InlineData("1.4012984643248170e-44", 0x0000000a)]
        [InlineData("1.5414283107572988e-44", 0x0000000b)]
        [InlineData("1.6815581571897805e-44", 0x0000000c)]
        [InlineData("1.8216880036222622e-44", 0x0000000d)]
        [InlineData("1.9618178500547440e-44", 0x0000000e)]
        [InlineData("2.1019476964872256e-44", 0x0000000f)]
        [InlineData("1.1754921087447446e-38", 0x007ffff0)]
        [InlineData("1.1754922488745910e-38", 0x007ffff1)]
        [InlineData("1.1754923890044375e-38", 0x007ffff2)]
        [InlineData("1.1754925291342839e-38", 0x007ffff3)]
        [InlineData("1.1754926692641303e-38", 0x007ffff4)]
        [InlineData("1.1754928093939768e-38", 0x007ffff5)]
        [InlineData("1.1754929495238232e-38", 0x007ffff6)]
        [InlineData("1.1754930896536696e-38", 0x007ffff7)]
        [InlineData("1.1754932297835160e-38", 0x007ffff8)]
        [InlineData("1.1754933699133625e-38", 0x007ffff9)]
        [InlineData("1.1754935100432089e-38", 0x007ffffa)]
        [InlineData("1.1754936501730553e-38", 0x007ffffb)]
        [InlineData("1.1754937903029018e-38", 0x007ffffc)]
        [InlineData("1.1754939304327482e-38", 0x007ffffd)]
        [InlineData("1.1754940705625946e-38", 0x007ffffe)]
        [InlineData("1.1754942106924411e-38", 0x007fffff)]
        // This number is exactly representable and should not be rounded in any
        // mode:
        // 0.1111111111111111111111100
        //                          ^
        [InlineData("0.99999988079071044921875", 0x3f7ffffe)]
        // This number is below the halfway point between two representable values
        // so it should round down in nearest mode:
        // 0.11111111111111111111111001
        //                          ^
        [InlineData("0.99999989569187164306640625", 0x3f7ffffe)]
        // This number is exactly halfway between two representable values, so it
        // should round to even in nearest mode:
        // 0.1111111111111111111111101
        //                          ^
        [InlineData("0.9999999105930328369140625", 0x3f7ffffe)]
        // This number is above the halfway point between two representable values
        // so it should round up in nearest mode:
        // 0.11111111111111111111111011
        //                          ^
        [InlineData("0.99999992549419403076171875", 0x3f7fffff)]
        public static void Parse_TestSpecific(string s, uint expectedBits)
        {
            CheckOneSingle(s, expectedBits);
        }

        [Theory]
        // Verify the smallest denormals:
        [InlineData(0x00000001, 0x00000100)]
        // Verify the largest denormals and the smallest normals:
        [InlineData(0x007fff00, 0x00800100)]
        // Verify the largest normals:
        [InlineData(0x7f7fff00, 0x7f800000)]
        public static void Parse_TestSpecificRanges(uint start, uint end)
        {
            for (uint i = start; i != end; i++)
            {
                TestRoundTripSingle(i);
            }

            TestRoundTripSingle((float)int.MaxValue);
            TestRoundTripSingle((float)uint.MaxValue);
        }

        [Theory]
        // Verify all representable powers of two and nearby values:
        [InlineData(2, -1022, 1024)]
        // Verify all representable powers of ten and nearby values:
        [InlineData(10, -50, 41)]
        public static void Parse_TestSpecificPowers(int b, int start, int end)
        {
            for (int i = start; i != end; ++i)
            {
                float f = MathF.Pow(b, i);
                uint bits = (uint)BitConverter.SingleToInt32Bits(f);

                TestRoundTripSingle(bits - 1);
                TestRoundTripSingle(bits);
                TestRoundTripSingle(bits + 1);
            }
        }

        private static void CheckOneSingle(string s, uint expectedBits)
        {
            CheckOneSingle(s, BitConverter.Int32BitsToSingle((int)(expectedBits)));
        }

        private static void CheckOneSingle(string s, float expected)
        {
            if (!float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float actual))
            {
                // If we fail to parse, set actual to NaN to ensure the comparison below will fail
                actual = float.NaN;
            }

            Assert.True(actual.Equals(expected), $"Expected {InvariantToString(expected)}, Actual {InvariantToString(actual)}");
        }

        private static string InvariantToString(object o)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:G9}", o);
        }

        private static void TestRoundTripSingle(float d)
        {
            string s = InvariantToString(d);
            CheckOneSingle(s, d);
        }

        private static void TestRoundTripSingle(uint bits)
        {
            float d = BitConverter.Int32BitsToSingle((int)(bits));

            if (float.IsFinite(d))
            {
                string s = InvariantToString(d);
                CheckOneSingle(s, bits);
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
