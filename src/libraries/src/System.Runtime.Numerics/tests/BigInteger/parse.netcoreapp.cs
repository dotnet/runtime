// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public partial class parseTest
    {
        [Theory]
        [InlineData("123456789", 0, 9, "123456789")]
        [InlineData("123456789", 0, 1, "1")]
        [InlineData("123456789", 1, 3, "234")]
        [InlineData("123456789", 8, 1, "9")]
        [InlineData("123456789abc", 8, 1, "9")]
        [InlineData("1\03456789", 0, 1, "1")]
        [InlineData("1\03456789", 0, 2, "1")]
        [InlineData("123456789\0", 0, 10, "123456789")]
        public void Parse_Subspan_Success(string input, int offset, int length, string expected)
        {
            Eval(BigInteger.Parse(input.AsSpan(offset, length)), expected);
            Assert.True(BigInteger.TryParse(input.AsSpan(offset, length), out BigInteger test));
            Eval(test, expected);
        }

        [Fact]
        public void Parse_EmptySubspan_Fails()
        {
            Assert.False(BigInteger.TryParse("12345".AsSpan(0, 0), out BigInteger result));
            Assert.Equal(0, result);
        }

        static partial void VerifyParseSpanToString(string num1, NumberStyles ns, bool failureNotExpected, string expected)
        {
            if (failureNotExpected)
            {
                Eval(BigInteger.Parse(num1.AsSpan(), ns), expected);

                Assert.True(BigInteger.TryParse(num1.AsSpan(), ns, provider: null, out BigInteger test));
                Eval(test, expected);

                if (ns == NumberStyles.Integer)
                {
                    Assert.True(BigInteger.TryParse(num1.AsSpan(), out test));
                    Eval(test, expected);
                }
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsSpan(), ns); });

                Assert.False(BigInteger.TryParse(num1.AsSpan(), ns, provider: null, out BigInteger test));

                if (ns == NumberStyles.Integer)
                {
                    Assert.False(BigInteger.TryParse(num1.AsSpan(), out test));
                }
            }
        }

        static partial void VerifySimpleFormatParseSpan(string num1, NumberFormatInfo nfi, BigInteger expected, bool failureExpected)
        {
            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1.AsSpan(), provider: nfi));
                Assert.True(BigInteger.TryParse(num1.AsSpan(), NumberStyles.Any, nfi, out BigInteger test));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsSpan(), provider: nfi); });
                Assert.False(BigInteger.TryParse(num1.AsSpan(), NumberStyles.Any, nfi, out BigInteger test), string.Format("Expected TryParse to fail on {0}", num1));
            }
        }

        static partial void VerifyFormatParseSpan(string num1, NumberStyles ns, NumberFormatInfo nfi, BigInteger expected, bool failureExpected)
        {
            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1.AsSpan(), ns, nfi));
                Assert.True(BigInteger.TryParse(num1.AsSpan(), NumberStyles.Any, nfi, out BigInteger test));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsSpan(), ns, nfi); });
                Assert.False(BigInteger.TryParse(num1.AsSpan(), ns, nfi, out BigInteger test), string.Format("Expected TryParse to fail on {0}", num1));
            }
        }
    }
}
