// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class DecimalTests
    {
        [Theory]
        [InlineData(MidpointRounding.ToEven - 1)]
        [InlineData(MidpointRounding.ToPositiveInfinity + 1)]
        public void Round_InvalidMidpointRounding_ThrowsArgumentException(MidpointRounding mode)
        {
            AssertExtensions.Throws<ArgumentException>("mode", () => decimal.Round(1, 2, mode));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "-123", 1, 3, NumberStyles.Number, null, 123m };
            yield return new object[] { "-123", 0, 3, NumberStyles.Number, null, -12m };
            yield return new object[] { 1000.ToString("N0"), 0, 4, NumberStyles.AllowThousands, null, 100m };
            yield return new object[] { 1000.ToString("N0"), 2, 3, NumberStyles.AllowThousands, null, 0m };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, 123m };
            yield return new object[] { "1234567890123456789012345.678456", 1, 4, NumberStyles.Number, new NumberFormatInfo() { NumberDecimalSeparator = "." }, 2345m };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, decimal expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            decimal result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(decimal.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, decimal.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, decimal.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, decimal.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(decimal.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => decimal.Parse(value.AsSpan(), style, provider));

                Assert.False(decimal.TryParse(value.AsSpan(), style, provider, out decimal result));
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void TryFormat()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                foreach (var testdata in ToString_TestData())
                {
                    decimal localI = (decimal)testdata[0];
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

                        if (localFormat != null)
                        {
                            // Upper localFormat
                            actual = new char[localExpected.Length];
                            Assert.True(localI.TryFormat(actual.AsSpan(), out charsWritten, localFormat.ToUpperInvariant(), localProvider));
                            Assert.Equal(localExpected.Length, charsWritten);
                            Assert.Equal(localExpected.ToUpperInvariant(), new string(actual));

                            // Lower format
                            actual = new char[localExpected.Length];
                            Assert.True(localI.TryFormat(actual.AsSpan(), out charsWritten, localFormat.ToLowerInvariant(), localProvider));
                            Assert.Equal(localExpected.Length, charsWritten);
                            Assert.Equal(localExpected.ToLowerInvariant(), new string(actual));
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
    }
}
