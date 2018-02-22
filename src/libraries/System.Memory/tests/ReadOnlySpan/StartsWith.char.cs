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
        private const string SoftHyphen = "\u00AD";

        [Fact]
        public static void ZeroLengthStartsWith_StringComparison()
        {
            var a = new char[3];

            var span = new ReadOnlySpan<char>(a);
            var slice = new ReadOnlySpan<char>(a, 2, 0);
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));

            span = ReadOnlySpan<char>.Empty;
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameSpanStartsWith_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            Assert.True(span.StartsWith(span, StringComparison.Ordinal));

            Assert.True(span.StartsWith(span, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(span, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void LengthMismatchStartsWith_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 2);
            var slice = new ReadOnlySpan<char>(a, 0, 3);
            Assert.False(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.False(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.False(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.False(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void StartsWithMatch_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(a, 0, 2);
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void StartsWithMatchDifferentSpans_StringComparison()
        {
            char[] a = { '4', '5', '6', '7' };
            char[] b = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(b, 0, 3);
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void StartsWithNoMatch_StringComparison()
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
                    Assert.False(firstSpan.StartsWith(secondSpan, StringComparison.Ordinal));

                    Assert.False(firstSpan.StartsWith(secondSpan, StringComparison.OrdinalIgnoreCase));
                    
                    // Different behavior depending on OS
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.StartsWith(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        firstSpan.StartsWith(secondSpan, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.StartsWith(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.StartsWith(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void MakeSureNoStartsWithChecksGoOutOfRange_StringComparison()
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
                Assert.True(span1.StartsWith(span2, StringComparison.Ordinal));

                Assert.True(span1.StartsWith(span2, StringComparison.CurrentCulture));
                Assert.True(span1.StartsWith(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(span1.StartsWith(span2, StringComparison.InvariantCulture));
                Assert.True(span1.StartsWith(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(span1.StartsWith(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void StartsWithUnknownComparisonType_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.StartsWith(_span, StringComparison.CurrentCulture - 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.StartsWith(_span, StringComparison.OrdinalIgnoreCase + 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.StartsWith(_span, (StringComparison)6));
        }

        [Fact]
        public static void StartsWithMatchNonOrdinal_StringComparison()
        {
            ReadOnlySpan<char> span = new char[] { 'a', 'b', 'c', 'd' };
            ReadOnlySpan<char> value = new char[] { 'a', 'B', 'c' };
            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
            Assert.True(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("el-GR");

            span = new char[] { '\u03b1', '\u03b2', '\u03b3', '\u03b4' };  // αβγδ
            value = new char[] { '\u03b1', '\u03b2', '\u03b3' }; // αβγ

            Assert.True(span.StartsWith(value, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));

            value = new char[] { '\u03b1', '\u0392', '\u03b3' }; // αΒγ
            Assert.False(span.StartsWith(value, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;

            span = new char[] { '\u0069', '\u00df', '\u0049', '\u03b4' }; // ißIδ
            value = new char[] { '\u0069', '\u0073', '\u0073', '\u0049' }; // issI

            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
             // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().StartsWith(value.ToString(), StringComparison.InvariantCulture),
                span.StartsWith(value, StringComparison.InvariantCulture));
            Assert.Equal(
                span.ToString().StartsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));

            value = new char[] { '\u0049', '\u0073', '\u0073', '\u0049' }; // IssI
            Assert.False(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCulture));
             // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().StartsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public static void StartsWithNoMatchNonOrdinal_StringComparison()
        {
            ReadOnlySpan<char> span = new char[] { 'a', 'b', 'c', 'd' };
            ReadOnlySpan<char> value = new char[] { 'a', 'D', 'c' };
            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
            Assert.False(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("el-GR");

            span = new char[] { '\u03b1', '\u03b2', '\u03b3', '\u03b4' }; // αβγδ
            value = new char[] { '\u03b1', '\u03b4', '\u03b3' }; // αδγ

            Assert.False(span.StartsWith(value, StringComparison.CurrentCulture));
            Assert.False(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));

            value = new char[] { '\u03b1', '\u0394', '\u03b3' }; // αΔγ
            Assert.False(span.StartsWith(value, StringComparison.CurrentCulture));
            Assert.False(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;

            span = new char[] { '\u0069', '\u00df', '\u0049', '\u03b4' }; // ißIδ
            value = new char[] { '\u0069', '\u03b4', '\u03b4', '\u0049' };  // iδδI

            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCulture));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));

            value = new char[] { '\u0049', '\u03b4', '\u03b4', '\u0049' }; // IδδI
            Assert.False(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCulture));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));
        }

        [Theory]
        // CurrentCulture
        [InlineData("Hello", "Hel", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.CurrentCulture, true)]
        [InlineData("", "", StringComparison.CurrentCulture, true)]
        [InlineData("", "hello", StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", "Hel", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "HEL", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.CurrentCultureIgnoreCase, false)]
        // InvariantCulture
        [InlineData("Hello", "Hel", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.InvariantCulture, true)]
        [InlineData("", "", StringComparison.InvariantCulture, true)]
        [InlineData("", "hello", StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", "Hel", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "HEL", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.InvariantCultureIgnoreCase, false)]
        // Ordinal
        [InlineData("Hello", "H", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hel", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hello", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hello Larger", StringComparison.Ordinal, false)]
        [InlineData("Hello", "", StringComparison.Ordinal, true)]
        [InlineData("Hello", "HEL", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Abc", StringComparison.Ordinal, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.Ordinal, false)]
        [InlineData("", "", StringComparison.Ordinal, true)]
        [InlineData("", "hello", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwx", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklm", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "ab_defghijklmnopqrstu", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdef_hijklmn", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghij_lmn", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "a", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyza", StringComparison.Ordinal, false)]
        // OrdinalIgnoreCase
        [InlineData("Hello", "Hel", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Hello Larger", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "HEL", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.OrdinalIgnoreCase, false)]
        public static void StartsWith(string s, string value, StringComparison comparisonType, bool expected)
        {
            Assert.Equal(expected, s.AsSpan().StartsWith(value.AsSpan(), comparisonType));
        }

        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public static void StartsWith_NullInStrings(StringComparison comparison)
        {
            Assert.False("\0test".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().StartsWith("te\0s".AsSpan(), comparison));
            Assert.True("test\0".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().StartsWith("te\0".AsSpan(), comparison));
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
        public static void StartsWith_NullInStrings_NonOrdinal(StringComparison comparison)
        {
            Assert.False("\0test".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().StartsWith("te\0s".AsSpan(), comparison));
            Assert.True("test\0".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().StartsWith("te\0".AsSpan(), comparison));
        }
    }
}
