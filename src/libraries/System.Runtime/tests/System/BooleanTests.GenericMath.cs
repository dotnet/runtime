// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class BooleanTests_GenericMath
    {
        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(BooleanTests.Parse_Valid_TestData), MemberType = typeof(BooleanTests))]
        public static void ParseValidStringTest(string value, bool expected)
        {
            bool result;

            // Default
            Assert.True(ParsableHelper<bool>.TryParse(value, provider: null, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, ParsableHelper<bool>.Parse(value, provider: null));

            // Current Culture
            Assert.True(ParsableHelper<bool>.TryParse(value, provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, ParsableHelper<bool>.Parse(value, provider: CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(BooleanTests.Parse_Invalid_TestData), MemberType = typeof(BooleanTests))]
        public static void ParseInvalidStringTest(string value, Type exceptionType)
        {
            bool result;

            // Default
            Assert.False(ParsableHelper<bool>.TryParse(value, provider: null, out result));
            Assert.Equal(default(bool), result);
            Assert.Throws(exceptionType, () => ParsableHelper<bool>.Parse(value, provider: null));

            // Current Culture
            Assert.False(ParsableHelper<bool>.TryParse(value, provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(default(bool), result);
            Assert.Throws(exceptionType, () => ParsableHelper<bool>.Parse(value, provider: CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(BooleanTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(BooleanTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, bool expected)
        {
            bool result;

            // Default
            Assert.True(SpanParsableHelper<bool>.TryParse(value.AsSpan(offset, count), provider: null, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, SpanParsableHelper<bool>.Parse(value.AsSpan(offset, count), provider: null));

            // Current Culture
            Assert.True(SpanParsableHelper<bool>.TryParse(value.AsSpan(offset, count), provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, SpanParsableHelper<bool>.Parse(value.AsSpan(offset, count), provider: CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(BooleanTests.Parse_Invalid_TestData), MemberType = typeof(BooleanTests))]
        public static void ParseInvalidSpanTest(string value, Type exceptionType)
        {
            if (value is null)
            {
                // null and empty span are treated the same
                return;
            }

            bool result;

            // Default
            Assert.False(SpanParsableHelper<bool>.TryParse(value.AsSpan(), provider: null, out result));
            Assert.Equal(default(bool), result);
            Assert.Throws(exceptionType, () => SpanParsableHelper<bool>.Parse(value.AsSpan(), provider: null));

            // Current Culture
            Assert.False(SpanParsableHelper<bool>.TryParse(value.AsSpan(), provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(default(bool), result);
            Assert.Throws(exceptionType, () => SpanParsableHelper<bool>.Parse(value.AsSpan(), provider: CultureInfo.CurrentCulture));
        }
    }
}
