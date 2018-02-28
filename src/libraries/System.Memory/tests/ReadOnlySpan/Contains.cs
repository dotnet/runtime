// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthContains_StringComparison()
        {
            var a = new char[3];

            var span = new ReadOnlySpan<char>(a);
            var slice = new ReadOnlySpan<char>(a, 2, 0);
            Assert.True(span.Contains(slice, StringComparison.Ordinal));

            Assert.True(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.OrdinalIgnoreCase));

            span = ReadOnlySpan<char>.Empty;
            Assert.True(span.Contains(slice, StringComparison.Ordinal));

            Assert.True(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameSpanContains_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            Assert.True(span.Contains(span, StringComparison.Ordinal));

            Assert.True(span.Contains(span, StringComparison.CurrentCulture));
            Assert.True(span.Contains(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(span, StringComparison.InvariantCulture));
            Assert.True(span.Contains(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void LengthMismatchContains_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 2);
            var slice = new ReadOnlySpan<char>(a, 0, 3);
            Assert.False(span.Contains(slice, StringComparison.Ordinal));

            Assert.False(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.False(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.False(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(span.Contains(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void ContainsMatch_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(a, 0, 2);
            Assert.True(span.Contains(slice, StringComparison.Ordinal));

            Assert.True(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void ContainsMatchDifferentSpans_StringComparison()
        {
            char[] a = { '4', '5', '6', '7' };
            char[] b = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(b, 0, 3);
            Assert.True(span.Contains(slice, StringComparison.Ordinal));

            Assert.True(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void ContainsNoMatch_StringComparison()
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
                    Assert.False(firstSpan.Contains(secondSpan, StringComparison.Ordinal));

                    Assert.False(firstSpan.Contains(secondSpan, StringComparison.OrdinalIgnoreCase));
                    
                    // Different behavior depending on OS
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.Contains(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.Contains(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.Contains(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.Contains(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void MakeSureNoContainsChecksGoOutOfRange_StringComparison()
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
                Assert.True(span1.Contains(span2, StringComparison.Ordinal));

                Assert.True(span1.Contains(span2, StringComparison.CurrentCulture));
                Assert.True(span1.Contains(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(span1.Contains(span2, StringComparison.InvariantCulture));
                Assert.True(span1.Contains(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(span1.Contains(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void ContainsUnknownComparisonType_StringComparison()
        {
            char[] a = { '4', '5', '6' };
            var span = new ReadOnlySpan<char>(a);
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.Contains(_span, StringComparison.CurrentCulture - 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.Contains(_span, StringComparison.OrdinalIgnoreCase + 1));
            TestHelpers.AssertThrows<ArgumentException, char>(span, (_span) => _span.Contains(_span, (StringComparison)6));
        }

        [Theory]
        [InlineData("Hello", "ello", true)]
        [InlineData("Hello", "ELL", false)]
        [InlineData("Hello", "Larger Hello", false)]
        [InlineData("Hello", "Goodbye", false)]
        [InlineData("", "", true)]
        [InlineData("", "hello", false)]
        [InlineData("Hello", "", true)]
        public static void Contains(string s, string value, bool expected)
        {
            Assert.Equal(expected, s.AsSpan().Contains(value.AsSpan(), StringComparison.Ordinal));
        }
    }
}
