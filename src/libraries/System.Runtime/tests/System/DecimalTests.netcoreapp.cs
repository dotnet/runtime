// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class DecimalTests
    {
        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Span_Valid(string value, NumberStyles style, IFormatProvider provider, decimal expected)
        {
            Assert.Equal(expected, decimal.Parse(value.AsReadOnlySpan(), style, provider));

            Assert.True(decimal.TryParse(value.AsReadOnlySpan(), style, provider, out decimal result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => decimal.Parse(value.AsReadOnlySpan(), style, provider));

                Assert.False(decimal.TryParse(value.AsReadOnlySpan(), style, provider, out decimal result));
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

                return SuccessExitCode;
            }).Dispose();
        }
    }
}
