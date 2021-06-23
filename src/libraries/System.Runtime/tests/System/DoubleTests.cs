// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0, NaN from -NaN

namespace System.Tests
{
    public class DoubleTests
    {
        // NOTE: Consider duplicating any tests added here in SingleTests.cs

        [Theory]
        [InlineData("a")]
        [InlineData(234.0f)]
        public static void CompareTo_ObjectNotDouble_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((double)123).CompareTo(value));
        }

        [Theory]
        [InlineData(234.0, 234.0, 0)]
        [InlineData(234.0, double.MinValue, 1)]
        [InlineData(234.0, -123.0, 1)]
        [InlineData(234.0, 0.0, 1)]
        [InlineData(234.0, 123.0, 1)]
        [InlineData(234.0, 456.0, -1)]
        [InlineData(234.0, double.MaxValue, -1)]
        [InlineData(234.0, double.NaN, 1)]
        [InlineData(double.NaN, double.NaN, 0)]
        [InlineData(double.NaN, 0.0, -1)]
        [InlineData(234.0, null, 1)]
        [InlineData(double.MinValue, double.NegativeInfinity, 1)]
        [InlineData(double.NegativeInfinity, double.MinValue, -1)]
        [InlineData(-0d, double.NegativeInfinity, 1)]
        [InlineData(double.NegativeInfinity, -0d, -1)]
        public static void CompareTo_Other_ReturnsExpected(double d1, object value, int expected)
        {
            if (value is double d2)
            {
                Assert.Equal(expected, Math.Sign(d1.CompareTo(d2)));
                if (double.IsNaN(d1) || double.IsNaN(d2))
                {
                    Assert.False(d1 >= d2);
                    Assert.False(d1 > d2);
                    Assert.False(d1 <= d2);
                    Assert.False(d1 < d2);
                }
                else
                {
                    if (expected >= 0)
                    {
                        Assert.True(d1 >= d2);
                        Assert.False(d1 < d2);
                    }
                    if (expected > 0)
                    {
                        Assert.True(d1 > d2);
                        Assert.False(d1 <= d2);
                    }
                    if (expected <= 0)
                    {
                        Assert.True(d1 <= d2);
                        Assert.False(d1 > d2);
                    }
                    if (expected < 0)
                    {
                        Assert.True(d1 < d2);
                        Assert.False(d1 >= d2);
                    }
                }
            }

            Assert.Equal(expected, Math.Sign(d1.CompareTo(value)));
        }

        [Fact]
        public static void Ctor_Empty()
        {
            var i = new double();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            double d = 41;
            Assert.Equal(41, d);

            d = 41.3;
            Assert.Equal(41.3, d);
        }

        [Fact]
        public static void Epsilon()
        {
            Assert.Equal(4.9406564584124654E-324, double.Epsilon);
            Assert.Equal(0x00000000_00000001u, BitConverter.DoubleToUInt64Bits(double.Epsilon));
        }

        [Theory]
        [InlineData(789.0, 789.0, true)]
        [InlineData(789.0, -789.0, false)]
        [InlineData(789.0, 0.0, false)]
        [InlineData(double.NaN, double.NaN, true)]
        [InlineData(double.NaN, -double.NaN, true)]
        [InlineData(789.0, 789.0f, false)]
        [InlineData(789.0, "789", false)]
        public static void EqualsTest(double d1, object value, bool expected)
        {
            if (value is double d2)
            {
                Assert.Equal(expected, d1.Equals(d2));

                if (double.IsNaN(d1) && double.IsNaN(d2))
                {
                    Assert.Equal(!expected, d1 == d2);
                    Assert.Equal(expected, d1 != d2);
                }
                else
                {
                    Assert.Equal(expected, d1 == d2);
                    Assert.Equal(!expected, d1 != d2);
                }
                Assert.Equal(expected, d1.GetHashCode().Equals(d2.GetHashCode()));
            }
            Assert.Equal(expected, d1.Equals(value));
        }

        [Fact]
        public static void GetTypeCode_Invoke_ReturnsDouble()
        {
            Assert.Equal(TypeCode.Double, 0.0.GetTypeCode());
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, true)]     // Positive Infinity
        public static void IsInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsInfinity(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, true)]                  // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNaN(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNaN(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNegativeInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNegativeInfinity(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, true)]     // Positive Infinity
        public static void IsPositiveInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsPositiveInfinity(d));
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(1.7976931348623157E+308, double.MaxValue);
            Assert.Equal(0x7FEFFFFF_FFFFFFFFu, BitConverter.DoubleToUInt64Bits(double.MaxValue));
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(-1.7976931348623157E+308, double.MinValue);
            Assert.Equal(0xFFEFFFFF_FFFFFFFFu, BitConverter.DoubleToUInt64Bits(double.MinValue));
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal(0.0 / 0.0, double.NaN);
            Assert.Equal(0xFFF80000_00000000u, BitConverter.DoubleToUInt64Bits(double.NaN));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal(-1.0 / 0.0, double.NegativeInfinity);
            Assert.Equal(0xFFF00000_00000000u, BitConverter.DoubleToUInt64Bits(double.NegativeInfinity));
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;

            var dollarSignCommaSeparatorFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = ","
            };

            var decimalSeparatorFormat = new NumberFormatInfo()
            {
                NumberDecimalSeparator = "."
            };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;

            yield return new object[] { "-123", defaultStyle, null, -123.0 };
            yield return new object[] { "0", defaultStyle, null, 0.0 };
            yield return new object[] { "123", defaultStyle, null, 123.0 };
            yield return new object[] { "  123  ", defaultStyle, null, 123.0 };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, 567.89 };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, -567.89 };
            yield return new object[] { "1E23", defaultStyle, null, 1E23 };
            yield return new object[] { "9007199254740997.0", defaultStyle, invariantFormat, 9007199254740996.0 };
            yield return new object[] { "9007199254740997.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 9007199254740996.0 };
            yield return new object[] { "9007199254740997.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 9007199254740996.0 };
            yield return new object[] { "9007199254740997.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 9007199254740998.0 };
            yield return new object[] { "9007199254740997.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 9007199254740998.0 };
            yield return new object[] { "9007199254740997.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 9007199254740998.0 };
            yield return new object[] { "5.005", defaultStyle, invariantFormat, 5.005 };
            yield return new object[] { "5.050", defaultStyle, invariantFormat, 5.05 };
            yield return new object[] { "5.005000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5.005 };
            yield return new object[] { "5.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, 5.0 };
            yield return new object[] { "5.0050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5.005 };

            yield return new object[] { "5005.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5005.0 };
            yield return new object[] { "50050.0", defaultStyle, invariantFormat, 50050.0 };
            yield return new object[] { "5005", defaultStyle, invariantFormat, 5005.0 };
            yield return new object[] { "050050", defaultStyle, invariantFormat, 50050.0 };
            yield return new object[] { "0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 0.0 };
            yield return new object[] { "0.005", defaultStyle, invariantFormat, 0.005 };
            yield return new object[] { "0.0500", defaultStyle, invariantFormat, 0.05 };
            yield return new object[] { "6250000000000000000000000000000000e-12", defaultStyle, invariantFormat, 6.25e21 };
            yield return new object[] { "6250000e0", defaultStyle, invariantFormat, 6.25e6 };
            yield return new object[] { "6250100e-5", defaultStyle, invariantFormat, 62.501 };
            yield return new object[] { "625010.00e-4", defaultStyle, invariantFormat, 62.501 };
            yield return new object[] { "62500e-4", defaultStyle, invariantFormat, 6.25 };
            yield return new object[] { "62500", defaultStyle, invariantFormat, 62500.0 };
            yield return new object[] { "10e-3", defaultStyle, invariantFormat, 0.01 };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, 123.1 };
            yield return new object[] { (1000.0).ToString("N0"), NumberStyles.AllowThousands, null, 1000.0 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, 123.0 };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, 123.567 };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, 123.0 };
            yield return new object[] { "$1,000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0 };
            yield return new object[] { "$1000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0 };
            yield return new object[] { "123.123", NumberStyles.Float, decimalSeparatorFormat, 123.123 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, decimalSeparatorFormat, -123.0 };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, double.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, double.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, double.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, double expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(double.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, double.Parse(value));
                }

                Assert.Equal(expected, double.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(double.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, double.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(double.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, double.Parse(value, style));
                Assert.Equal(expected, double.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        internal static string SplitPairs(string input)
        {
            string[] splitPairs = input.Split('-');
            string ret = "";
            foreach (var pair in splitPairs)
            {
                string reversedPair = Reverse(pair);
                ret += reversedPair;
            }

            return ret;
        }

        internal static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public static void ParsePatterns()
        {
            string path = Directory.GetCurrentDirectory();
            using (FileStream file = new FileStream(Path.Combine(path, "ibm-fpgen.txt"), FileMode.Open))
            {
                using (var streamReader = new StreamReader(file))
                {
                    string line = streamReader.ReadLine();
                    while (line != null)
                    {
                        string[] data = line.Split(' ');
                        string inputHalfBytes = data[0];
                        string inputFloatBytes = data[1];
                        string inputDoubleBytes = data[2];
                        string correctValue = data[3];

                        double doubleValue = double.Parse(correctValue, NumberFormatInfo.InvariantInfo);
                        string doubleBytes = BitConverter.ToString(BitConverter.GetBytes(doubleValue));
                        float floatValue = float.Parse(correctValue, NumberFormatInfo.InvariantInfo);
                        string floatBytes = BitConverter.ToString(BitConverter.GetBytes(floatValue));
                        Half halfValue = Half.Parse(correctValue, NumberFormatInfo.InvariantInfo);
                        string halfBytes = BitConverter.ToString(BitConverter.GetBytes(halfValue));

                        doubleBytes = SplitPairs(doubleBytes);
                        floatBytes = SplitPairs(floatBytes);
                        halfBytes = SplitPairs(halfBytes);

                        if (BitConverter.IsLittleEndian)
                        {
                            doubleBytes = Reverse(doubleBytes);
                            floatBytes = Reverse(floatBytes);
                            halfBytes = Reverse(halfBytes);
                        }

                        Assert.Equal(doubleBytes, inputDoubleBytes);
                        Assert.Equal(floatBytes, inputFloatBytes);
                        Assert.Equal(halfBytes, inputHalfBytes);
                        line = streamReader.ReadLine();
                    }
                }
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float;

            var dollarSignDecimalSeparatorFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                NumberDecimalSeparator = "."
            };

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { (100.0).ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { "1E23", NumberStyles.None, null, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Negative hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(double.TryParse(value, out result));
                    Assert.Equal(default(double), result);

                    Assert.Throws(exceptionType, () => double.Parse(value));
                }

                Assert.Throws(exceptionType, () => double.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(double.TryParse(value, style, provider, out result));
            Assert.Equal(default(double), result);

            Assert.Throws(exceptionType, () => double.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(double.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(double), result);

                Assert.Throws(exceptionType, () => double.Parse(value, style));
                Assert.Throws(exceptionType, () => double.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
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
        public static void PositiveInfinity()
        {
            Assert.Equal(1.0 / 0.0, double.PositiveInfinity);
            Assert.Equal(0x7FF00000_00000000u, BitConverter.DoubleToUInt64Bits(double.PositiveInfinity));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { -4567.0, "G", null, "-4567" };
            yield return new object[] { -4567.89101, "G", null, "-4567.89101" };
            yield return new object[] { 0.0, "G", null, "0" };
            yield return new object[] { 4567.0, "G", null, "4567" };
            yield return new object[] { 4567.89101, "G", null, "4567.89101" };

            yield return new object[] { double.NaN, "G", null, "NaN" };

            yield return new object[] { 2468.0, "N", null, "2,468.00" };

            // Changing the negative pattern doesn't do anything without also passing in a format string
            var customNegativePattern = new NumberFormatInfo() { NumberNegativePattern = 0 };
            yield return new object[] { -6310.0, "G", customNegativePattern, "-6310" };

            var customNegativeSignDecimalGroupSeparator = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*"
            };
            yield return new object[] { -2468.0, "N", customNegativeSignDecimalGroupSeparator, "#2*468~00" };
            yield return new object[] { 2468.0, "N", customNegativeSignDecimalGroupSeparator, "2*468~00" };

            var customNegativeSignGroupSeparatorNegativePattern = new NumberFormatInfo()
            {
                NegativeSign = "xx", // Set to trash to make sure it doesn't show up
                NumberGroupSeparator = "*",
                NumberNegativePattern = 0,
            };
            yield return new object[] { -2468.0, "N", customNegativeSignGroupSeparatorNegativePattern, "(2*468.00)" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { double.NaN, "G", invariantFormat, "NaN" };
            yield return new object[] { double.PositiveInfinity, "G", invariantFormat, "Infinity" };
            yield return new object[] { double.NegativeInfinity, "G", invariantFormat, "-Infinity" };
        }

        public static IEnumerable<object[]> ToString_TestData_NotNetFramework()
        {
            foreach (var testData in ToString_TestData())
            {
                yield return testData;
            }


            yield return new object[] { double.MinValue, "G", null, "-1.7976931348623157E+308" };
            yield return new object[] { double.MaxValue, "G", null, "1.7976931348623157E+308" };

            yield return new object[] { double.Epsilon, "G", null, "5E-324" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { double.Epsilon, "G", invariantFormat, "5E-324" };
            yield return new object[] { 32.5, "C100", invariantFormat, "¤32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5, "P100", invariantFormat, "3,250.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { 32.5, "E100", invariantFormat, "3.2500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { 32.5, "F100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5, "N100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
        }

        [Fact]
        public static void Test_ToString_NotNetFramework()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData_NotNetFramework())
                {
                    ToString((double)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void ToString(double d, string format, IFormatProvider provider, string expected)
        {
            bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(expected, d.ToString());
                    Assert.Equal(expected, d.ToString((IFormatProvider)null));
                }
                Assert.Equal(expected, d.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(expected.Replace('e', 'E'), d.ToString(format.ToUpperInvariant())); // If format is upper case, then exponents are printed in upper case
                Assert.Equal(expected.Replace('E', 'e'), d.ToString(format.ToLowerInvariant())); // If format is lower case, then exponents are printed in upper case
                Assert.Equal(expected.Replace('e', 'E'), d.ToString(format.ToUpperInvariant(), null));
                Assert.Equal(expected.Replace('E', 'e'), d.ToString(format.ToLowerInvariant(), null));
            }
            Assert.Equal(expected.Replace('e', 'E'), d.ToString(format.ToUpperInvariant(), provider));
            Assert.Equal(expected.Replace('E', 'e'), d.ToString(format.ToLowerInvariant(), provider));
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            double d = 123.0;
            Assert.Throws<FormatException>(() => d.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => d.ToString("Y", null)); // Invalid format
            long intMaxPlus1 = (long)int.MaxValue + 1;
            string intMaxPlus1String = intMaxPlus1.ToString();
            Assert.Throws<FormatException>(() => d.ToString("E" + intMaxPlus1String));
        }

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

        [Fact]
        public static void TryFormat()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
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
            }
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

        [Fact]
        public static void TestNegativeNumberParsingWithHyphen()
        {
            // CLDR data for Swedish culture has negative sign U+2212. This test ensure parsing with the hyphen with such cultures will succeed.
            CultureInfo ci = CultureInfo.GetCultureInfo("sv-SE");
            string s = string.Format(ci, "{0}", 158.68);
            Assert.Equal(-158.68, double.Parse("-" + s, NumberStyles.Number, ci));
        }
    }
}
