// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;
using Xunit;

namespace System.Tests
{
    public class HalfTests_GenericMath
    {
        [Theory]
        [MemberData(nameof(HalfTests.Parse_Valid_TestData), MemberType = typeof(HalfTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, Half expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Half result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(NumberHelper<Half>.TryParse(value, null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, NumberHelper<Half>.Parse(value, null));
                }

                Assert.Equal(expected, NumberHelper<Half>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(NumberHelper<Half>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, NumberHelper<Half>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(NumberHelper<Half>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, NumberHelper<Half>.Parse(value, style, null));
                Assert.Equal(expected, NumberHelper<Half>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(HalfTests.Parse_Invalid_TestData), MemberType = typeof(HalfTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Half result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(NumberHelper<Half>.TryParse(value, null, out result));
                    Assert.Equal(default(Half), result);

                    Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, null));
                }

                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(NumberHelper<Half>.TryParse(value, style, provider, out result));
            Assert.Equal(default(Half), result);

            Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(NumberHelper<Half>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(Half), result);

                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, style, null));
                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(HalfTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(HalfTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Half expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Half result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(NumberHelper<Half>.TryParse(value.AsSpan(offset, count), null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, NumberHelper<Half>.Parse(value.AsSpan(offset, count), null));
                }

                Assert.Equal(expected, NumberHelper<Half>.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, NumberHelper<Half>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<Half>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(HalfTests.Parse_Invalid_TestData), MemberType = typeof(HalfTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value.AsSpan(), style, provider));

                Assert.False(NumberHelper<Half>.TryParse(value.AsSpan(), style, provider, out Half result));
                Assert.Equal((Half)0, result);
            }
        }
    }
}
