// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthEndsWith_StringComparison()
        {
            var a = new char[3];

            var span = new ReadOnlySpan<char>(a);
            var slice = new ReadOnlySpan<char>(a, 2, 0);
            Assert.True(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));

            span = ReadOnlySpan<char>.Empty;
            Assert.True(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameSpanEndsWith_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            Assert.True(span.EndsWith(span, StringComparison.Ordinal));

            Assert.True(span.EndsWith(span, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(span, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void LengthMismatchEndsWith_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 2);
            var slice = new ReadOnlySpan<char>(a, 0, 3);
            Assert.False(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.False(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.False(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.False(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void EndsWithMatch_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(a, 1, 2);
            Assert.True(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void EndsWithMatchDifferentSpans_StringComparison()
        {
            char[] a = { '7', '4', '5', '6' };
            char[] b = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 1, 3);
            var slice = new ReadOnlySpan<char>(b, 0, 3);
            Assert.True(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void EndsWithNoMatch_StringComparison()
        {
            for (int length = 1; length < 150; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    var firstSpan = new ReadOnlySpan<char>(first);
                    var secondSpan = new ReadOnlySpan<char>(second);
                    Assert.False(firstSpan.EndsWith(secondSpan, StringComparison.Ordinal));

                    Assert.False(firstSpan.EndsWith(secondSpan, StringComparison.OrdinalIgnoreCase));

                    // Different behavior depending on OS
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.EndsWith(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        firstSpan.EndsWith(secondSpan, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.EndsWith(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.EndsWith(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void MakeSureNoEndsWithChecksGoOutOfRange_StringComparison()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = (char)99;
                first[length + 1] = (char)99;
                var second = new char[length + 2];
                second[0] = (char)100;
                second[length + 1] = (char)100;
                var span1 = new ReadOnlySpan<char>(first, 1, length);
                var span2 = new ReadOnlySpan<char>(second, 1, length);
                Assert.True(span1.EndsWith(span2, StringComparison.Ordinal));

                Assert.True(span1.EndsWith(span2, StringComparison.CurrentCulture));
                Assert.True(span1.EndsWith(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(span1.EndsWith(span2, StringComparison.InvariantCulture));
                Assert.True(span1.EndsWith(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(span1.EndsWith(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void EndsWithUnknownComparisonType_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.EndsWith(_span, StringComparison.CurrentCulture - 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.EndsWith(_span, StringComparison.OrdinalIgnoreCase + 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.EndsWith(_span, (StringComparison)6));
        }

        [Fact]
        public static void EndsWithMatchNonOrdinal_StringComparison()
        {
            ReadOnlySpan<char> span = new char[] { 'd', 'a', 'b', 'c' };
            ReadOnlySpan<char> value = new char[] { 'a', 'B', 'c' };
            Assert.False(span.EndsWith(value, StringComparison.Ordinal));
            Assert.True(span.EndsWith(value, StringComparison.OrdinalIgnoreCase));

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("el-GR");

            span = new char[] { '\u03b4', '\u03b1', '\u03b2', '\u03b3' }; // δαβγ
            value = new char[] { '\u03b1', '\u03b2', '\u03b3' }; // αβγ

            Assert.True(span.EndsWith(value, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

            value = new char[] { '\u03b1', '\u0392', '\u03b3' }; // αΒγ
            Assert.False(span.EndsWith(value, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;

            span = new char[] { '\u03b4', '\u0069', '\u00df', '\u0049' }; // δißI
            value = new char[] { '\u0069', '\u0073', '\u0073', '\u0049' }; // issI

            Assert.False(span.EndsWith(value, StringComparison.Ordinal));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().EndsWith(value.ToString(), StringComparison.InvariantCulture),
                span.EndsWith(value, StringComparison.InvariantCulture));
            Assert.Equal(
                span.ToString().EndsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));

            value = new char[] { '\u0049', '\u0073', '\u0073', '\u0049' }; // IssI
            Assert.False(span.EndsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.EndsWith(value, StringComparison.InvariantCulture));
             // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().EndsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public static void EndsWithNoMatchNonOrdinal_StringComparison()
        {
            ReadOnlySpan<char> span = new char[] { 'd', 'a', 'b', 'c' };
            ReadOnlySpan<char> value = new char[] { 'a', 'D', 'c' };
            Assert.False(span.EndsWith(value, StringComparison.Ordinal));
            Assert.False(span.EndsWith(value, StringComparison.OrdinalIgnoreCase));

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("el-GR");

            span = new char[] { '\u03b4', '\u03b1', '\u03b2', '\u03b3' }; // δαβγ
            value = new char[] { '\u03b1', '\u03b4', '\u03b3' }; // αδγ

            Assert.False(span.EndsWith(value, StringComparison.CurrentCulture));
            Assert.False(span.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

            value = new char[] { '\u03b1', '\u0394', '\u03b3' }; // αΔγ
            Assert.False(span.EndsWith(value, StringComparison.CurrentCulture));
            Assert.False(span.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;

            span = new char[] { '\u03b4', '\u0069', '\u00df', '\u0049' }; // δißI
            value = new char[] { '\u0069', '\u03b4', '\u03b4', '\u0049' }; // iδδI

            Assert.False(span.EndsWith(value, StringComparison.Ordinal));
            Assert.False(span.EndsWith(value, StringComparison.InvariantCulture));
            Assert.False(span.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));

            value = new char[] { '\u0049', '\u03b4', '\u03b4', '\u0049' }; // IδδI
            Assert.False(span.EndsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.EndsWith(value, StringComparison.InvariantCulture));
            Assert.False(span.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));
        }

        [Theory]
        // CurrentCulture
        [InlineData("", "Foo", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "llo", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.CurrentCulture, true)]
        [InlineData("", "", StringComparison.CurrentCulture, true)]
        [InlineData("", "a", StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", "llo", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "LLO", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "a", StringComparison.CurrentCultureIgnoreCase, false)]
        // InvariantCulture
        [InlineData("", "Foo", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "llo", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.InvariantCulture, true)]
        [InlineData("", "", StringComparison.InvariantCulture, true)]
        [InlineData("", "a", StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", "llo", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "LLO", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "a", StringComparison.InvariantCultureIgnoreCase, false)]
        // Ordinal
        [InlineData("Hello", "o", StringComparison.Ordinal, true)]
        [InlineData("Hello", "llo", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hello", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Larger Hello", StringComparison.Ordinal, false)]
        [InlineData("Hello", "", StringComparison.Ordinal, true)]
        [InlineData("Hello", "LLO", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Abc", StringComparison.Ordinal, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.Ordinal, false)]
        [InlineData("", "", StringComparison.Ordinal, true)]
        [InlineData("", "a", StringComparison.Ordinal, false)]
        // OrdinalIgnoreCase
        [InlineData("Hello", "llo", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Larger Hello", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "LLO", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("", "a", StringComparison.OrdinalIgnoreCase, false)]
        public static void EndsWith(string s, string value, StringComparison comparisonType, bool expected)
        {
            Assert.Equal(expected, s.AsSpan().EndsWith(value.AsSpan(), comparisonType));
        }

        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public static void EndsWith_NullInStrings(StringComparison comparison)
        {
            Assert.True("\0test".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().EndsWith("e\0st".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test\0".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().EndsWith("\0st".AsSpan(), comparison));
        }

        // NOTE: This is by design. Unix ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between Windows and Unix (https://github.com/dotnet/coreclr/issues/2051).
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        public static void EndsWith_NullInStrings_NonOrdinal(StringComparison comparison)
        {
            Assert.True("\0test".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().EndsWith("e\0st".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test\0".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().EndsWith("\0st".AsSpan(), comparison));
        }
    }
}
