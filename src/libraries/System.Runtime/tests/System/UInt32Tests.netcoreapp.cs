// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class UInt32Tests
    {
        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "123", 0, 2, NumberStyles.Integer, null, (uint)12 };
            yield return new object[] { "123", 1, 2, NumberStyles.Integer, null, (uint)23 };
            yield return new object[] { "4294967295", 0, 1, NumberStyles.Integer, null, 4 };
            yield return new object[] { "4294967295", 9, 1, NumberStyles.Integer, null, 5 };
            yield return new object[] { "12", 0, 1, NumberStyles.HexNumber, null, (uint)0x1 };
            yield return new object[] { "12", 1, 1, NumberStyles.HexNumber, null, (uint)0x2 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (uint)10 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, uint expected)
        {
            uint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(uint.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, uint.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(uint.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                uint result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(uint.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0u, result);
                }

                Assert.Throws(exceptionType, () => uint.Parse(value.AsSpan(), style, provider));

                Assert.False(uint.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(uint i, string format, IFormatProvider provider, string expected)
        {
            char[] actual;
            int charsWritten;

            // Just right
            actual = new char[expected.Length];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual));

            // Longer than needed
            actual = new char[expected.Length + 1];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual, 0, charsWritten));

            // Too short
            if (expected.Length > 0)
            {
                actual = new char[expected.Length - 1];
                Assert.False(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                Assert.Equal(0, charsWritten);
            }

            if (format != null)
            {
                // Upper format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToUpperInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToUpperInvariant(), new string(actual));

                // Lower format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToLowerInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToLowerInvariant(), new string(actual));
            }
        }
    }
}
