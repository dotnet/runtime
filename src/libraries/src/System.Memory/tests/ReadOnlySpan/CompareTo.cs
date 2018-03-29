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
        public static void ZeroLengthCompareTo_StringComparison()
        {
            char[] a = { '4', '5', '6' };

            var span = new ReadOnlySpan<char>(a);
            var slice = new ReadOnlySpan<char>(a, 2, 0);
            Assert.True(0 < span.CompareTo(slice, StringComparison.Ordinal));

            Assert.True(0 < span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.True(0 < span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(0 < span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.True(0 < span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(0 < span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));

            span = new ReadOnlySpan<char>(a, 1, 0);
            Assert.Equal(0, span.CompareTo(slice, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameSpanCompareTo_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            Assert.Equal(0, span.CompareTo(span, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(span, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(span, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void LengthMismatchCompareTo_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 2);
            var slice = new ReadOnlySpan<char>(a, 0, 3);
            Assert.True(0 > span.CompareTo(slice, StringComparison.Ordinal));

            Assert.True(0 > span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.True(0 > span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(0 > span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.True(0 > span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(0 > span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void CompareToOverlappingMatch_StringComparison()
        {
            char[] a = { '4', '5', '6', '5', '6', '5' };
            var span = new ReadOnlySpan<char>(a, 1, 3);
            var slice = new ReadOnlySpan<char>(a, 3, 3);
            Assert.Equal(0, span.CompareTo(slice, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void CompareToMatchDifferentSpans_StringComparison()
        {
            char[] a = { '4', '5', '6', '7' };
            char[] b = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(b, 0, 3);
            Assert.Equal(0, span.CompareTo(slice, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void CompareToNoMatch_StringComparison()
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
                    Assert.True(0 > firstSpan.CompareTo(secondSpan, StringComparison.Ordinal));

                    // Due to differences in the implementation, the exact result of CompareTo will not necessarily match with string.Compare.
                    // However, the sign will match, which is what defines correctness.
                    Assert.Equal(
                        Math.Sign(string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.OrdinalIgnoreCase)),
                        Math.Sign(firstSpan.CompareTo(secondSpan, StringComparison.OrdinalIgnoreCase)));

                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.CompareTo(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        firstSpan.CompareTo(secondSpan, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.CompareTo(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.CompareTo(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void MakeSureNoCompareToChecksGoOutOfRange_StringComparison()
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
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.Ordinal));

                Assert.Equal(0, span1.CompareTo(span2, StringComparison.CurrentCulture));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.InvariantCulture));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void CompareToUnknownComparisonType_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.CompareTo(_span, StringComparison.CurrentCulture - 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.CompareTo(_span, StringComparison.OrdinalIgnoreCase + 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.CompareTo(_span, (StringComparison)6));
        }

        [Theory]
        // CurrentCulture
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.CurrentCulture, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.CurrentCulture, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.CurrentCulture, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCulture, 1)]
        [InlineData("hello", 2, "HELLO", 2, 3, StringComparison.CurrentCulture, -1)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.CurrentCulture, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCulture, -1)]
        [InlineData("A", 0, "B", 0, 1, StringComparison.CurrentCulture, -1)]
        [InlineData("B", 0, "A", 0, 1, StringComparison.CurrentCulture, 1)]
        // CurrentCultureIgnoreCase
        [InlineData("HELLO", 0, "hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Yellow", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.CurrentCultureIgnoreCase, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCultureIgnoreCase, -1)]
        // InvariantCulture
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.InvariantCulture, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.InvariantCulture, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.InvariantCulture, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.InvariantCulture, 1)]
        [InlineData("hello", 2, "HELLO", 2, 3, StringComparison.InvariantCulture, -1)]
        // InvariantCultureIgnoreCase
        [InlineData("HELLO", 0, "hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Yellow", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.InvariantCultureIgnoreCase, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.InvariantCultureIgnoreCase, -1)]
        // Ordinal
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.Ordinal, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.Ordinal, 0)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 0, "Hello", 0, 0, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "Hello", 0, 3, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "He" + SoftHyphen + "llo", 0, 5, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 0, "-=<Hello>=-", 3, 5, StringComparison.Ordinal, 0)]
        [InlineData("\uD83D\uDD53Hello\uD83D\uDD50", 1, "\uD83D\uDD53Hello\uD83D\uDD54", 1, 7, StringComparison.Ordinal, 0)] // Surrogate split
        [InlineData("Hello", 0, "Hello123", 0, int.MaxValue, StringComparison.Ordinal, -1)]           // Recalculated length, second string longer
        [InlineData("Hello123", 0, "Hello", 0, int.MaxValue, StringComparison.Ordinal, 1)]            // Recalculated length, first string longer
        [InlineData("---aaaaaaaaaaa", 3, "+++aaaaaaaaaaa", 3, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 2, equal compare
        [InlineData("aaaaaaaaaaaaaa", 3, "aaaxaaaaaaaaaa", 3, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 2, different compare at n=1
        [InlineData("-aaaaaaaaaaaaa", 1, "+aaaaaaaaaaaaa", 1, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 6, equal compare
        [InlineData("aaaaaaaaaaaaaa", 1, "axaaaaaaaaaaaa", 1, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 6, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 4, equal compare
        [InlineData("aaaaaaaaaaaaaa", 0, "xaaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 4, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 0, "axaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 4, different compare at n=2
        [InlineData("--aaaaaaaaaaaa", 2, "++aaaaaaaaaaaa", 2, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 0, equal compare
        [InlineData("aaaaaaaaaaaaaa", 2, "aaxaaaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaxaaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=2
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaaxaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=3
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaaaxaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=4
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaaaaxaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=5
        [InlineData("aaaaaaaaaaaaaa", 0, "+aaaaaaaaaaaaa", 1, 13, StringComparison.Ordinal, 0)]       // Different int alignment, equal compare
        [InlineData("aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaax", 1, 100, StringComparison.Ordinal, -1)]     // Different int alignment
        [InlineData("aaaaaaaaaaaaaa", 1, "aaaxaaaaaaaaaa", 3, 100, StringComparison.Ordinal, -1)]     // Different long alignment, abs of 4, one of them is 2, different at n=1
        [InlineData("-aaaaaaaaaaaaa", 1, "++++aaaaaaaaaa", 4, 10, StringComparison.Ordinal, 0)]       // Different long alignment, equal compare
        [InlineData("aaaaaaaaaaaaaa", 1, "aaaaaaaaaaaaax", 4, 100, StringComparison.Ordinal, -1)]     // Different long alignment
        [InlineData("\0", 0, "", 0, 1, StringComparison.Ordinal, 1)]                                  // Same memory layout, except for m_stringLength (m_firstChars are both 0)
        [InlineData("\0\0", 0, "", 0, 2, StringComparison.Ordinal, 1)]                                // Same as above, except m_stringLength for one is 2
        [InlineData("", 0, "\0b", 0, 2, StringComparison.Ordinal, -1)]                                // strA's second char != strB's second char codepath
        [InlineData("", 0, "b", 0, 1, StringComparison.Ordinal, -1)]                                  // Should hit strA.m_firstChar != strB.m_firstChar codepath
        [InlineData("abcxxxxxxxxxxxxxxxxxxxxxx", 0, "abdxxxxxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 64-bit: first long compare is different
        [InlineData("abcdefgxxxxxxxxxxxxxxxxxx", 0, "abcdefhxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 64-bit: second long compare is different
        [InlineData("abcdefghijkxxxxxxxxxxxxxx", 0, "abcdefghijlxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 64-bit: third long compare is different
        [InlineData("abcdexxxxxxxxxxxxxxxxxxxx", 0, "abcdfxxxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 32-bit: second int compare is different
        [InlineData("abcdefghixxxxxxxxxxxxxxxx", 0, "abcdefghjxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 32-bit: fourth int compare is different
        // OrdinalIgnoreCase
        [InlineData("HELLO", 0, "hello", 0, 5, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 2, "Yellow", 2, 3, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("A", 0, "x", 0, 1, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("a", 0, "X", 0, 1, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("[", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("[", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("\\", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("\\", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("]", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("]", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("^", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("^", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("_", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("_", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("`", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("`", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        public static void Compare(string strA, int indexA, string strB, int indexB, int length, StringComparison comparisonType, int expected)
        {
            ReadOnlySpan<char> span = length <= (strA.Length - indexA) ? strA.AsSpan(indexA, length) : strA.AsSpan(indexA);
            ReadOnlySpan<char> value = length <= (strB.Length - indexB) ? strB.AsSpan(indexB, length) : strB.AsSpan(indexB);
            Assert.Equal(expected, Math.Sign(span.CompareTo(value, comparisonType)));
        }
    }
}
