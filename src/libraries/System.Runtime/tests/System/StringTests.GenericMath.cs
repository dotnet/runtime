// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class StringTests_GenericMath
    {
        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            yield return new object[] { "Hello" };
            yield return new object[] { "hello" };
            yield return new object[] { "HELLO" };
            yield return new object[] { "hElLo" };
            yield return new object[] { "  Hello  " };
            yield return new object[] { "Hello\0" };
            yield return new object[] { " \0 \0  Hello   \0 " };
            yield return new object[] { "World" };
            yield return new object[] { "world" };
            yield return new object[] { "WORLD" };
            yield return new object[] { "wOrLd" };
            yield return new object[] { "World  " };
            yield return new object[] { "World\0" };
            yield return new object[] { "  World \0\0\0  " };
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            yield return new object[] { null, typeof(ArgumentNullException) };
            // We cannot easily test inputs that exceed `string.MaxLength` without risk of OOM
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[0] };
            }

            yield return new object[] { " \0 \0  Hello, World!   \0 ", 6, 5, "Hello" };
            yield return new object[] { " \0 \0  Hello, World!   \0 ", 13, 5, "World" };
            yield return new object[] { " \0 \0  Hello, World!   \0 ", 6, 13, "Hello, World!" };
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(StringTests_GenericMath.Parse_Valid_TestData), MemberType = typeof(StringTests_GenericMath))]
        public static void ParseValidStringTest(string value)
        {
            string result;
            string expected = value;

            // Default
            Assert.True(ParsableHelper<string>.TryParse(value, provider: null, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, ParsableHelper<string>.Parse(value, provider: null));

            // Current Culture
            Assert.True(ParsableHelper<string>.TryParse(value, provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, ParsableHelper<string>.Parse(value, provider: CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(StringTests_GenericMath.Parse_Invalid_TestData), MemberType = typeof(StringTests_GenericMath))]
        public static void ParseInvalidStringTest(string value, Type exceptionType)
        {
            string result;

            // Default
            Assert.False(ParsableHelper<string>.TryParse(value, provider: null, out result));
            Assert.Equal(default(string), result);
            Assert.Throws(exceptionType, () => ParsableHelper<string>.Parse(value, provider: null));

            // Current Culture
            Assert.False(ParsableHelper<string>.TryParse(value, provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(default(string), result);
            Assert.Throws(exceptionType, () => ParsableHelper<string>.Parse(value, provider: CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(StringTests_GenericMath.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(StringTests_GenericMath))]
        public static void ParseValidSpanTest(string value, int offset, int count, string expected)
        {
            string result;

            // Default
            Assert.True(SpanParsableHelper<string>.TryParse(value.AsSpan(offset, count), provider: null, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, SpanParsableHelper<string>.Parse(value.AsSpan(offset, count), provider: null));

            // Current Culture
            Assert.True(SpanParsableHelper<string>.TryParse(value.AsSpan(offset, count), provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, SpanParsableHelper<string>.Parse(value.AsSpan(offset, count), provider: CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(StringTests_GenericMath.Parse_Invalid_TestData), MemberType = typeof(StringTests_GenericMath))]
        public static void ParseInvalidSpanTest(string value, Type exceptionType)
        {
            if (value is null)
            {
                // null and empty span are treated the same
                return;
            }

            string result;

            // Default
            Assert.False(SpanParsableHelper<string>.TryParse(value.AsSpan(), provider: null, out result));
            Assert.Equal(default(string), result);
            Assert.Throws(exceptionType, () => SpanParsableHelper<string>.Parse(value.AsSpan(), provider: null));

            // Current Culture
            Assert.False(SpanParsableHelper<string>.TryParse(value.AsSpan(), provider: CultureInfo.CurrentCulture, out result));
            Assert.Equal(default(string), result);
            Assert.Throws(exceptionType, () => SpanParsableHelper<string>.Parse(value.AsSpan(), provider: CultureInfo.CurrentCulture));
        }
    }
}
