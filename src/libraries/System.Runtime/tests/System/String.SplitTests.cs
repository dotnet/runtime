// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Tests
{
    // These tests validate both String.Split and MemoryExtensions.Split, as they have equivalent semantics, with the
    // former creating a new array to store the results and the latter writing the results into a supplied span.

    public static class StringSplitTests
    {
        [Fact]
        public static void SplitInvalidCount()
        {
            const string Value = "a,b";
            const int Count = -1;
            const StringSplitOptions Options = StringSplitOptions.None;

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Value.Split(',', Count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Value.Split(',', Count, Options));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Value.Split(new[] { ',' }, Count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Value.Split(new[] { ',' }, Count, Options));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Value.Split(",", Count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Value.Split(",", Count, Options));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Value.Split(new[] { "," }, Count, Options));
        }

        [Fact]
        public static void SplitInvalidOptions()
        {
            const string Value = "a,b";
            const int Count = 0;

            foreach (StringSplitOptions options in new[] { StringSplitOptions.None - 1, (StringSplitOptions)0x04 })
            {
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(',', options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(',', Count, options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(new[] { ',' }, options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(new[] { ',' }, Count, options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(",", options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(",", Count, options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(new[] { "," }, options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.Split(new[] { "," }, Count, options));

                AssertExtensions.Throws<ArgumentException>("options", () => Value.AsSpan().Split(Span<Range>.Empty, ',', options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.AsSpan().Split(Span<Range>.Empty, ",", options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.AsSpan().SplitAny(Span<Range>.Empty, ",", options));
                AssertExtensions.Throws<ArgumentException>("options", () => Value.AsSpan().SplitAny(Span<Range>.Empty, new[] { "," }, options));
            }
        }

        [Fact]
        public static void SplitZeroCountEmptyResult()
        {
            const string Value = "a,b";
            const int Count = 0;
            const StringSplitOptions Options = StringSplitOptions.None;

            string[] expected = new string[0];

            Assert.Equal(expected, Value.Split(',', Count));
            Assert.Equal(expected, Value.Split(',', Count, Options));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Count));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Count, Options));
            Assert.Equal(expected, Value.Split(",", Count));
            Assert.Equal(expected, Value.Split(",", Count, Options));
            Assert.Equal(expected, Value.Split(new[] { "," }, Count, Options));

            Assert.Equal(0, Value.AsSpan().Split(Span<Range>.Empty, ',', Options));
            Assert.Equal(0, Value.AsSpan().Split(Span<Range>.Empty, ",", Options));
            Assert.Equal(0, Value.AsSpan().SplitAny(Span<Range>.Empty, ",", Options));
        }

        [Fact]
        public static void SplitEmptyValueWithRemoveEmptyEntriesOptionEmptyResult()
        {
            const string Value = "";
            const int Count = int.MaxValue;
            const StringSplitOptions Options = StringSplitOptions.RemoveEmptyEntries;

            string[] expected = Array.Empty<string>();

            Assert.Equal(expected, Value.Split(',', Options));
            Assert.Equal(expected, Value.Split(',', Count, Options));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Options));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Count, Options));
            Assert.Equal(expected, Value.Split(",", Options));
            Assert.Equal(expected, Value.Split(",", Count, Options));
            Assert.Equal(expected, Value.Split(new[] { "," }, Options));
            Assert.Equal(expected, Value.Split(new[] { "," }, Count, Options));

            Range[] ranges = new Range[10];
            Assert.Equal(0, Value.AsSpan().Split(ranges, ',', Options));
            Assert.Equal(0, Value.AsSpan().Split(ranges, ",", Options));
            Assert.Equal(0, Value.AsSpan().SplitAny(ranges, ",", Options));
        }

        [Fact]
        public static void SplitOneCountSingleResult()
        {
            const string Value = "a,b";
            const int Count = 1;
            const StringSplitOptions Options = StringSplitOptions.None;

            string[] expected = new[] { Value };

            Assert.Equal(expected, Value.Split(',', Count));
            Assert.Equal(expected, Value.Split(',', Count, Options));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Count));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Count, Options));
            Assert.Equal(expected, Value.Split(",", Count));
            Assert.Equal(expected, Value.Split(",", Count, Options));
            Assert.Equal(expected, Value.Split(new[] { "," }, Count, Options));

            Range[] ranges = new Range[1];
            Assert.Equal(1, Value.AsSpan().Split(ranges, ',', Options));
            Assert.Equal(0..3, ranges[0]);
            Array.Clear(ranges);

            Assert.Equal(1, Value.AsSpan().Split(ranges, ",", Options));
            Assert.Equal(0..3, ranges[0]);
            Array.Clear(ranges);

            Assert.Equal(1, Value.AsSpan().SplitAny(ranges, ",", Options));
            Assert.Equal(0..3, ranges[0]);
            Array.Clear(ranges);
        }

        [Fact]
        public static void SplitNoMatchSingleResult()
        {
            const string Value = "a b";
            const int Count = int.MaxValue;
            const StringSplitOptions Options = StringSplitOptions.None;

            string[] expected = new[] { Value };

            Assert.Equal(expected, Value.Split(','));
            Assert.Equal(expected, Value.Split(',', Options));
            Assert.Equal(expected, Value.Split(',', Count, Options));
            Assert.Equal(expected, Value.Split(new[] { ',' }));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Options));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Count));
            Assert.Equal(expected, Value.Split(new[] { ',' }, Count, Options));
            Assert.Equal(expected, Value.Split(","));
            Assert.Equal(expected, Value.Split(",", Options));
            Assert.Equal(expected, Value.Split(",", Count, Options));
            Assert.Equal(expected, Value.Split(new[] { "," }, Options));
            Assert.Equal(expected, Value.Split(new[] { "," }, Count, Options));

            Range[] ranges = new Range[10];
            Assert.Equal(1, Value.AsSpan().Split(ranges, ',', Options));
            Assert.Equal(0..3, ranges[0]);
            Array.Clear(ranges);

            Assert.Equal(1, Value.AsSpan().Split(ranges, ",", Options));
            Assert.Equal(0..3, ranges[0]);
            Array.Clear(ranges);

            Assert.Equal(1, Value.AsSpan().SplitAny(ranges, ",", Options));
            Assert.Equal(0..3, ranges[0]);
            Array.Clear(ranges);
        }

        private const int M = int.MaxValue;

        [Theory]
        [InlineData("", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("", ',', 1, StringSplitOptions.None, new[] { "" })]
        [InlineData("", ',', 2, StringSplitOptions.None, new[] { "" })]
        [InlineData("", ',', 3, StringSplitOptions.None, new[] { "" })]
        [InlineData("", ',', 4, StringSplitOptions.None, new[] { "" })]
        [InlineData("", ',', M, StringSplitOptions.None, new[] { "" })]
        [InlineData("", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("", ',', 1, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("", ',', 2, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("", ',', 3, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("", ',', 4, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("", ',', M, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",", ',', 1, StringSplitOptions.None, new[] { "," })]
        [InlineData(",", ',', 2, StringSplitOptions.None, new[] { "", "" })]
        [InlineData(",", ',', 3, StringSplitOptions.None, new[] { "", "" })]
        [InlineData(",", ',', 4, StringSplitOptions.None, new[] { "", "" })]
        [InlineData(",", ',', M, StringSplitOptions.None, new[] { "", "" })]
        [InlineData(",", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "," })]
        [InlineData(",", ',', 2, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",", ',', 3, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",", ',', 4, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",", ',', M, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",,", ',', 1, StringSplitOptions.None, new[] { ",," })]
        [InlineData(",,", ',', 2, StringSplitOptions.None, new[] { "", ",", })]
        [InlineData(",,", ',', 3, StringSplitOptions.None, new[] { "", "", "" })]
        [InlineData(",,", ',', 4, StringSplitOptions.None, new[] { "", "", "" })]
        [InlineData(",,", ',', M, StringSplitOptions.None, new[] { "", "", "" })]
        [InlineData(",,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",," })]
        [InlineData(",,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",,", ',', M, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("ab", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("ab", ',', 1, StringSplitOptions.None, new[] { "ab" })]
        [InlineData("ab", ',', 2, StringSplitOptions.None, new[] { "ab" })]
        [InlineData("ab", ',', 3, StringSplitOptions.None, new[] { "ab" })]
        [InlineData("ab", ',', 4, StringSplitOptions.None, new[] { "ab" })]
        [InlineData("ab", ',', M, StringSplitOptions.None, new[] { "ab" })]
        [InlineData("ab", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("ab", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "ab" })]
        [InlineData("ab", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "ab" })]
        [InlineData("ab", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "ab" })]
        [InlineData("ab", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "ab" })]
        [InlineData("ab", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "ab" })]
        [InlineData("a,b", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("a,b", ',', 1, StringSplitOptions.None, new[] { "a,b" })]
        [InlineData("a,b", ',', 2, StringSplitOptions.None, new[] { "a", "b" })]
        [InlineData("a,b", ',', 3, StringSplitOptions.None, new[] { "a", "b" })]
        [InlineData("a,b", ',', 4, StringSplitOptions.None, new[] { "a", "b" })]
        [InlineData("a,b", ',', M, StringSplitOptions.None, new[] { "a", "b" })]
        [InlineData("a,b", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a,b", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "a,b" })]
        [InlineData("a,b", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,b", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,b", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,b", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("a,", ',', 1, StringSplitOptions.None, new[] { "a," })]
        [InlineData("a,", ',', 2, StringSplitOptions.None, new[] { "a", "" })]
        [InlineData("a,", ',', 3, StringSplitOptions.None, new[] { "a", "" })]
        [InlineData("a,", ',', 4, StringSplitOptions.None, new[] { "a", "" })]
        [InlineData("a,", ',', M, StringSplitOptions.None, new[] { "a", "" })]
        [InlineData("a,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "a," })]
        [InlineData("a,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a" })]
        [InlineData("a,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a" })]
        [InlineData("a,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a" })]
        [InlineData("a,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a" })]
        [InlineData(",b", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",b", ',', 1, StringSplitOptions.None, new[] { ",b" })]
        [InlineData(",b", ',', 2, StringSplitOptions.None, new[] { "", "b" })]
        [InlineData(",b", ',', 3, StringSplitOptions.None, new[] { "", "b" })]
        [InlineData(",b", ',', 4, StringSplitOptions.None, new[] { "", "b" })]
        [InlineData(",b", ',', M, StringSplitOptions.None, new[] { "", "b" })]
        [InlineData(",b", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",b", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",b" })]
        [InlineData(",b", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "b" })]
        [InlineData(",b", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "b" })]
        [InlineData(",b", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "b" })]
        [InlineData(",b", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "b" })]
        [InlineData(",a,b", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",a,b", ',', 1, StringSplitOptions.None, new[] { ",a,b" })]
        [InlineData(",a,b", ',', 2, StringSplitOptions.None, new[] { "", "a,b" })]
        [InlineData(",a,b", ',', 3, StringSplitOptions.None, new[] { "", "a", "b" })]
        [InlineData(",a,b", ',', 4, StringSplitOptions.None, new[] { "", "a", "b" })]
        [InlineData(",a,b", ',', M, StringSplitOptions.None, new[] { "", "a", "b" })]
        [InlineData(",a,b", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",a,b", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",a,b" })]
        [InlineData(",a,b", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData(",a,b", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData(",a,b", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData(",a,b", ',', 5, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,b,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("a,b,", ',', 1, StringSplitOptions.None, new[] { "a,b," })]
        [InlineData("a,b,", ',', 2, StringSplitOptions.None, new[] { "a", "b,", })]
        [InlineData("a,b,", ',', 3, StringSplitOptions.None, new[] { "a", "b", "" })]
        [InlineData("a,b,", ',', 4, StringSplitOptions.None, new[] { "a", "b", "" })]
        [InlineData("a,b,", ',', M, StringSplitOptions.None, new[] { "a", "b", "" })]
        [InlineData("a,b,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a,b,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "a,b," })]
        [InlineData("a,b,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b," })]
        [InlineData("a,b,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,b,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,b,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData("a,b,c", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("a,b,c", ',', 1, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("a,b,c", ',', 2, StringSplitOptions.None, new[] { "a", "b,c" })]
        [InlineData("a,b,c", ',', 3, StringSplitOptions.None, new[] { "a", "b", "c" })]
        [InlineData("a,b,c", ',', 4, StringSplitOptions.None, new[] { "a", "b", "c" })]
        [InlineData("a,b,c", ',', M, StringSplitOptions.None, new[] { "a", "b", "c" })]
        [InlineData("a,b,c", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a,b,c", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "a,b,c" })]
        [InlineData("a,b,c", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b,c", })]
        [InlineData("a,b,c", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData("a,b,c", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData("a,b,c", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData("a,,c", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("a,,c", ',', 1, StringSplitOptions.None, new[] { "a,,c" })]
        [InlineData("a,,c", ',', 2, StringSplitOptions.None, new[] { "a", ",c", })]
        [InlineData("a,,c", ',', 3, StringSplitOptions.None, new[] { "a", "", "c" })]
        [InlineData("a,,c", ',', 4, StringSplitOptions.None, new[] { "a", "", "c" })]
        [InlineData("a,,c", ',', M, StringSplitOptions.None, new[] { "a", "", "c" })]
        [InlineData("a,,c", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a,,c", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "a,,c" })]
        [InlineData("a,,c", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "c", })]
        [InlineData("a,,c", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "c" })]
        [InlineData("a,,c", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "c" })]
        [InlineData("a,,c", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "c" })]
        [InlineData(",a,b,c", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",a,b,c", ',', 1, StringSplitOptions.None, new[] { ",a,b,c" })]
        [InlineData(",a,b,c", ',', 2, StringSplitOptions.None, new[] { "", "a,b,c" })]
        [InlineData(",a,b,c", ',', 3, StringSplitOptions.None, new[] { "", "a", "b,c" })]
        [InlineData(",a,b,c", ',', 4, StringSplitOptions.None, new[] { "", "a", "b", "c" })]
        [InlineData(",a,b,c", ',', M, StringSplitOptions.None, new[] { "", "a", "b", "c" })]
        [InlineData(",a,b,c", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",a,b,c", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",a,b,c" })]
        [InlineData(",a,b,c", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b,c", })]
        [InlineData(",a,b,c", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData(",a,b,c", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData(",a,b,c", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData("a,b,c,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("a,b,c,", ',', 1, StringSplitOptions.None, new[] { "a,b,c," })]
        [InlineData("a,b,c,", ',', 2, StringSplitOptions.None, new[] { "a", "b,c," })]
        [InlineData("a,b,c,", ',', 3, StringSplitOptions.None, new[] { "a", "b", "c,", })]
        [InlineData("a,b,c,", ',', 4, StringSplitOptions.None, new[] { "a", "b", "c", "" })]
        [InlineData("a,b,c,", ',', M, StringSplitOptions.None, new[] { "a", "b", "c", "" })]
        [InlineData("a,b,c,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a,b,c,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "a,b,c," })]
        [InlineData("a,b,c,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b,c,", })]
        [InlineData("a,b,c,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c," })]
        [InlineData("a,b,c,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData("a,b,c,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData(",a,b,c,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",a,b,c,", ',', 1, StringSplitOptions.None, new[] { ",a,b,c," })]
        [InlineData(",a,b,c,", ',', 2, StringSplitOptions.None, new[] { "", "a,b,c," })]
        [InlineData(",a,b,c,", ',', 3, StringSplitOptions.None, new[] { "", "a", "b,c," })]
        [InlineData(",a,b,c,", ',', 4, StringSplitOptions.None, new[] { "", "a", "b", "c," })]
        [InlineData(",a,b,c,", ',', M, StringSplitOptions.None, new[] { "", "a", "b", "c", "" })]
        [InlineData(",a,b,c,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",a,b,c,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",a,b,c," })]
        [InlineData(",a,b,c,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b,c," })]
        [InlineData(",a,b,c,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c," })]
        [InlineData(",a,b,c,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData(",a,b,c,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c" })]
        [InlineData("first,second", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("first,second", ',', 1, StringSplitOptions.None, new[] { "first,second" })]
        [InlineData("first,second", ',', 2, StringSplitOptions.None, new[] { "first", "second" })]
        [InlineData("first,second", ',', 3, StringSplitOptions.None, new[] { "first", "second" })]
        [InlineData("first,second", ',', 4, StringSplitOptions.None, new[] { "first", "second" })]
        [InlineData("first,second", ',', M, StringSplitOptions.None, new[] { "first", "second" })]
        [InlineData("first,second", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("first,second", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "first,second" })]
        [InlineData("first,second", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,second", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,second", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,second", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("first,", ',', 1, StringSplitOptions.None, new[] { "first," })]
        [InlineData("first,", ',', 2, StringSplitOptions.None, new[] { "first", "" })]
        [InlineData("first,", ',', 3, StringSplitOptions.None, new[] { "first", "" })]
        [InlineData("first,", ',', 4, StringSplitOptions.None, new[] { "first", "" })]
        [InlineData("first,", ',', M, StringSplitOptions.None, new[] { "first", "" })]
        [InlineData("first,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("first,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "first," })]
        [InlineData("first,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first" })]
        [InlineData("first,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first" })]
        [InlineData("first,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first" })]
        [InlineData("first,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first" })]
        [InlineData(",second", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",second", ',', 1, StringSplitOptions.None, new[] { ",second" })]
        [InlineData(",second", ',', 2, StringSplitOptions.None, new[] { "", "second" })]
        [InlineData(",second", ',', 3, StringSplitOptions.None, new[] { "", "second" })]
        [InlineData(",second", ',', 4, StringSplitOptions.None, new[] { "", "second" })]
        [InlineData(",second", ',', M, StringSplitOptions.None, new[] { "", "second" })]
        [InlineData(",second", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",second", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",second" })]
        [InlineData(",second", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "second" })]
        [InlineData(",second", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "second" })]
        [InlineData(",second", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "second" })]
        [InlineData(",second", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "second" })]
        [InlineData(",first,second", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",first,second", ',', 1, StringSplitOptions.None, new[] { ",first,second" })]
        [InlineData(",first,second", ',', 2, StringSplitOptions.None, new[] { "", "first,second" })]
        [InlineData(",first,second", ',', 3, StringSplitOptions.None, new[] { "", "first", "second" })]
        [InlineData(",first,second", ',', 4, StringSplitOptions.None, new[] { "", "first", "second" })]
        [InlineData(",first,second", ',', M, StringSplitOptions.None, new[] { "", "first", "second" })]
        [InlineData(",first,second", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",first,second", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",first,second" })]
        [InlineData(",first,second", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData(",first,second", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData(",first,second", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData(",first,second", ',', 5, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,second,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("first,second,", ',', 1, StringSplitOptions.None, new[] { "first,second," })]
        [InlineData("first,second,", ',', 2, StringSplitOptions.None, new[] { "first", "second,", })]
        [InlineData("first,second,", ',', 3, StringSplitOptions.None, new[] { "first", "second", "" })]
        [InlineData("first,second,", ',', 4, StringSplitOptions.None, new[] { "first", "second", "" })]
        [InlineData("first,second,", ',', M, StringSplitOptions.None, new[] { "first", "second", "" })]
        [InlineData("first,second,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("first,second,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "first,second," })]
        [InlineData("first,second,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second," })]
        [InlineData("first,second,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,second,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,second,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second" })]
        [InlineData("first,second,third", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("first,second,third", ',', 1, StringSplitOptions.None, new[] { "first,second,third" })]
        [InlineData("first,second,third", ',', 2, StringSplitOptions.None, new[] { "first", "second,third" })]
        [InlineData("first,second,third", ',', 3, StringSplitOptions.None, new[] { "first", "second", "third" })]
        [InlineData("first,second,third", ',', 4, StringSplitOptions.None, new[] { "first", "second", "third" })]
        [InlineData("first,second,third", ',', M, StringSplitOptions.None, new[] { "first", "second", "third" })]
        [InlineData("first,second,third", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("first,second,third", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "first,second,third" })]
        [InlineData("first,second,third", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second,third", })]
        [InlineData("first,second,third", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData("first,second,third", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData("first,second,third", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData("first,,third", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("first,,third", ',', 1, StringSplitOptions.None, new[] { "first,,third" })]
        [InlineData("first,,third", ',', 2, StringSplitOptions.None, new[] { "first", ",third", })]
        [InlineData("first,,third", ',', 3, StringSplitOptions.None, new[] { "first", "", "third" })]
        [InlineData("first,,third", ',', 4, StringSplitOptions.None, new[] { "first", "", "third" })]
        [InlineData("first,,third", ',', M, StringSplitOptions.None, new[] { "first", "", "third" })]
        [InlineData("first,,third", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("first,,third", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "first,,third" })]
        [InlineData("first,,third", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "third", })]
        [InlineData("first,,third", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "third" })]
        [InlineData("first,,third", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "third" })]
        [InlineData("first,,third", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "third" })]
        [InlineData(",first,second,third", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",first,second,third", ',', 1, StringSplitOptions.None, new[] { ",first,second,third" })]
        [InlineData(",first,second,third", ',', 2, StringSplitOptions.None, new[] { "", "first,second,third" })]
        [InlineData(",first,second,third", ',', 3, StringSplitOptions.None, new[] { "", "first", "second,third" })]
        [InlineData(",first,second,third", ',', 4, StringSplitOptions.None, new[] { "", "first", "second", "third" })]
        [InlineData(",first,second,third", ',', M, StringSplitOptions.None, new[] { "", "first", "second", "third" })]
        [InlineData(",first,second,third", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",first,second,third", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",first,second,third" })]
        [InlineData(",first,second,third", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second,third", })]
        [InlineData(",first,second,third", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData(",first,second,third", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData(",first,second,third", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData("first,second,third,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("first,second,third,", ',', 1, StringSplitOptions.None, new[] { "first,second,third," })]
        [InlineData("first,second,third,", ',', 2, StringSplitOptions.None, new[] { "first", "second,third," })]
        [InlineData("first,second,third,", ',', 3, StringSplitOptions.None, new[] { "first", "second", "third,", })]
        [InlineData("first,second,third,", ',', 4, StringSplitOptions.None, new[] { "first", "second", "third", "" })]
        [InlineData("first,second,third,", ',', M, StringSplitOptions.None, new[] { "first", "second", "third", "" })]
        [InlineData("first,second,third,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("first,second,third,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { "first,second,third," })]
        [InlineData("first,second,third,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second,third,", })]
        [InlineData("first,second,third,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third," })]
        [InlineData("first,second,third,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData("first,second,third,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData(",first,second,third,", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(",first,second,third,", ',', 1, StringSplitOptions.None, new[] { ",first,second,third," })]
        [InlineData(",first,second,third,", ',', 2, StringSplitOptions.None, new[] { "", "first,second,third," })]
        [InlineData(",first,second,third,", ',', 3, StringSplitOptions.None, new[] { "", "first", "second,third," })]
        [InlineData(",first,second,third,", ',', 4, StringSplitOptions.None, new[] { "", "first", "second", "third," })]
        [InlineData(",first,second,third,", ',', M, StringSplitOptions.None, new[] { "", "first", "second", "third", "" })]
        [InlineData(",first,second,third,", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(",first,second,third,", ',', 1, StringSplitOptions.RemoveEmptyEntries, new[] { ",first,second,third," })]
        [InlineData(",first,second,third,", ',', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second,third," })]
        [InlineData(",first,second,third,", ',', 3, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third," })]
        [InlineData(",first,second,third,", ',', 4, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData(",first,second,third,", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first", "second", "third" })]
        [InlineData("first,second,third", ' ', M, StringSplitOptions.None, new[] { "first,second,third" })]
        [InlineData("first,second,third", ' ', M, StringSplitOptions.RemoveEmptyEntries, new[] { "first,second,third" })]
        [InlineData("Foo Bar Baz", ' ', 2, StringSplitOptions.RemoveEmptyEntries, new[] { "Foo", "Bar Baz" })]
        [InlineData("Foo Bar Baz", ' ', M, StringSplitOptions.None, new[] { "Foo", "Bar", "Baz" })]
        [InlineData("a", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData("a", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a", ',', 0, StringSplitOptions.TrimEntries, new string[0])]
        [InlineData("a", ',', 0, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new string[0])]
        [InlineData("a", ',', 1, StringSplitOptions.None, new string[] { "a" })]
        [InlineData("a", ',', 1, StringSplitOptions.RemoveEmptyEntries, new string[] { "a" })]
        [InlineData("a", ',', 1, StringSplitOptions.TrimEntries, new string[] { "a" })]
        [InlineData("a", ',', 1, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new string[] { "a" })]
        [InlineData(" ", ',', 0, StringSplitOptions.None, new string[0])]
        [InlineData(" ", ',', 0, StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData(" ", ',', 0, StringSplitOptions.TrimEntries, new string[0])]
        [InlineData(" ", ',', 0, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new string[0])]
        [InlineData(" ", ',', 1, StringSplitOptions.None, new string[] { " " })]
        [InlineData(" ", ',', 1, StringSplitOptions.RemoveEmptyEntries, new string[] { " " })]
        [InlineData(" ", ',', 1, StringSplitOptions.TrimEntries, new string[] { "" })]
        [InlineData(" ", ',', 1, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new string[0])]
        [InlineData(" a,, b, c ", ',', 2, StringSplitOptions.None, new string[] { " a", ", b, c " })]
        [InlineData(" a,, b, c ", ',', 2, StringSplitOptions.RemoveEmptyEntries, new string[] { " a", " b, c " })]
        [InlineData(" a,, b, c ", ',', 2, StringSplitOptions.TrimEntries, new string[] { "a", ", b, c" })]
        [InlineData(" a,, b, c ", ',', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new string[] { "a", "b, c" })]
        [InlineData(" a,, b, c ", ',', 3, StringSplitOptions.None, new string[] { " a", "", " b, c " })]
        [InlineData(" a,, b, c ", ',', 3, StringSplitOptions.RemoveEmptyEntries, new string[] { " a", " b", " c " })]
        [InlineData(" a,, b, c ", ',', 3, StringSplitOptions.TrimEntries, new string[] { "a", "", "b, c" })]
        [InlineData(" a,, b, c ", ',', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new string[] { "a", "b", "c" })]
        [InlineData("    Monday    ", ',', M, StringSplitOptions.None, new[] { "    Monday    " })]
        [InlineData("    Monday    ", ',', M, StringSplitOptions.TrimEntries, new[] { "Monday" })]
        [InlineData("    Monday    ", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "    Monday    " })]
        [InlineData("    Monday    ", ',', M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "Monday" })]
        [InlineData("              ", ',', M, StringSplitOptions.None, new[] { "              " })]
        [InlineData("              ", ',', M, StringSplitOptions.TrimEntries, new[] { "" })]
        [InlineData("              ", ',', M, StringSplitOptions.RemoveEmptyEntries, new[] { "              " })]
        [InlineData("              ", ',', M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a b ", ' ', 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData(" a b ", ' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        public static void SplitCharSeparator(string value, char separator, int count, StringSplitOptions options, string[] expected)
        {
            Assert.Equal(expected, value.Split(separator, count, options));
            Assert.Equal(expected, value.Split(new[] { separator }, count, options));
            Assert.Equal(expected, value.Split(separator.ToString(), count, options));
            Assert.Equal(expected, value.Split(new[] { separator.ToString() }, count, options));
            if (count == int.MaxValue)
            {
                Assert.Equal(expected, value.Split(separator, options));
                Assert.Equal(expected, value.Split(new[] { separator }, options));
                Assert.Equal(expected, value.Split(separator.ToString(), options));
                Assert.Equal(expected, value.Split(new[] { separator.ToString() }, options));
            }
            if (options == StringSplitOptions.None)
            {
                Assert.Equal(expected, value.Split(separator, count));
                Assert.Equal(expected, value.Split(new[] { separator }, count));
                Assert.Equal(expected, value.Split(separator.ToString(), count));
            }
            if (count == int.MaxValue && options == StringSplitOptions.None)
            {
                Assert.Equal(expected, value.Split(separator));
                Assert.Equal(expected, value.Split(new[] { separator }));
                Assert.Equal(expected, value.Split(separator.ToString()));
            }

            Range[] ranges = new Range[count == int.MaxValue ? value.Length + 1 : count];

            Assert.Equal(expected.Length, value.AsSpan().Split(ranges, separator, options));
            Assert.Equal(expected, ranges.Take(expected.Length).Select(r => value[r]).ToArray());

            Assert.Equal(expected.Length, value.AsSpan().Split(ranges, separator.ToString(), options));
            Assert.Equal(expected, ranges.Take(expected.Length).Select(r => value[r]).ToArray());
        }

        [Theory]
        [InlineData("", null, 0, StringSplitOptions.None, new string[0])]
        [InlineData("", "", 0, StringSplitOptions.None, new string[0])]
        [InlineData("", "separator", 0, StringSplitOptions.None, new string[0])]
        [InlineData("  a ,   b ,c  ", "", M, StringSplitOptions.TrimEntries, new[] { "a ,   b ,c" })]
        [InlineData("       ", "", M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("       ", "", M, StringSplitOptions.TrimEntries, new[] { "" })]
        [InlineData("a,b,c", null, M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("a,b,c", "", M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("aaabaaabaaa", "aa", M, StringSplitOptions.None, new[] { "", "ab", "ab", "a" })]
        [InlineData("aaabaaabaaa", "aa", M, StringSplitOptions.RemoveEmptyEntries, new[] { "ab", "ab", "a" })]
        [InlineData("this, is, a, string, with some spaces", ", ", M, StringSplitOptions.None, new[] { "this", "is", "a", "string", "with some spaces" })]
        [InlineData("Monday, Tuesday, Wednesday, Thursday, Friday", ",", M, StringSplitOptions.TrimEntries, new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" })]
        [InlineData("Monday, Tuesday,\r, Wednesday,\n, Thursday, Friday", ",", M, StringSplitOptions.TrimEntries, new[] { "Monday", "Tuesday", "", "Wednesday", "", "Thursday", "Friday" })]
        [InlineData("Monday, Tuesday,\r, Wednesday,\n, Thursday, Friday", ",", M, StringSplitOptions.RemoveEmptyEntries, new[] { "Monday", " Tuesday", "\r", " Wednesday", "\n", " Thursday", " Friday" })]
        [InlineData("Monday, Tuesday,\r, Wednesday,\n, Thursday, Friday", ",", M, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" })]
        [InlineData("    Monday    ", ",", M, StringSplitOptions.None, new[] { "    Monday    " })]
        [InlineData("    Monday    ", ",", M, StringSplitOptions.TrimEntries, new[] { "Monday" })]
        [InlineData("    Monday    ", ",", M, StringSplitOptions.RemoveEmptyEntries, new[] { "    Monday    " })]
        [InlineData("    Monday    ", ",", M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "Monday" })]
        [InlineData("              ", ",", M, StringSplitOptions.None, new[] { "              " })]
        [InlineData("              ", ",", M, StringSplitOptions.TrimEntries, new[] { "" })]
        [InlineData("              ", ",", M, StringSplitOptions.RemoveEmptyEntries, new[] { "              " })]
        [InlineData("              ", ",", M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a b ", null, 2, StringSplitOptions.TrimEntries, new[] { "a b" })]
        [InlineData("a b ", "", 2, StringSplitOptions.TrimEntries, new[] { "a b" })]
        [InlineData("a b ", " ", 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData(" a b ", null, 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a b" })]
        [InlineData(" a b ", "", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a b" })]
        [InlineData(" a b ", " ", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        public static void SplitStringSeparator(string value, string separator, int count, StringSplitOptions options, string[] expected)
        {
            Assert.Equal(expected, value.Split(separator, count, options));
            Assert.Equal(expected, value.Split(new[] { separator }, count, options));
            if (count == int.MaxValue)
            {
                Assert.Equal(expected, value.Split(separator, options));
                Assert.Equal(expected, value.Split(new[] { separator }, options));
            }
            if (options == StringSplitOptions.None)
            {
                Assert.Equal(expected, value.Split(separator, count));
            }
            if (count == int.MaxValue && options == StringSplitOptions.None)
            {
                Assert.Equal(expected, value.Split(separator));
            }

            Range[] ranges = new Range[count == int.MaxValue ? value.Length + 1 : count];
            Assert.Equal(expected.Length, value.AsSpan().Split(ranges, separator, options));
            Assert.Equal(expected, ranges.Take(expected.Length).Select(r => value[r]).ToArray());
        }

        [Fact]
        public static void SplitNullCharArraySeparator_BindsToCharArrayOverload()
        {
            string value = "a b c";
            string[] expected = new[] { "a", "b", "c" };
            // Ensure Split(null) compiles successfully as a call to Split(char[])
            Assert.Equal(expected, value.Split(null));
        }

        [Theory]
        [InlineData("a b c", null, M, StringSplitOptions.None, new[] { "a", "b", "c" })]
        [InlineData("a b c", new char[0], M, StringSplitOptions.None, new[] { "a", "b", "c" })]
        [InlineData("a,b,c", null, M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("a,b,c", new char[0], M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ' ' }, M, StringSplitOptions.None, new[] { "this,", "is,", "a,", "string,", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ' ', ',' }, M, StringSplitOptions.None, new[] { "this", "", "is", "", "a", "", "string", "", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ' }, M, StringSplitOptions.None, new[] { "this", "", "is", "", "a", "", "string", "", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ', 's' }, M, StringSplitOptions.None, new[] { "thi", "", "", "i", "", "", "a", "", "", "tring", "", "with", "", "ome", "", "pace", "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ', 's', 'a' }, M, StringSplitOptions.None, new[] { "thi", "", "", "i", "", "", "", "", "", "", "tring", "", "with", "", "ome", "", "p", "ce", "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ' ' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this,", "is,", "a,", "string,", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ' ', ',' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ', 's' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "thi", "i", "a", "tring", "with", "ome", "pace" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ', 's', 'a' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "thi", "i", "tring", "with", "ome", "p", "ce" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', 's', 'a' }, M, StringSplitOptions.None, new[] { "thi" /*s*/, "" /*,*/, " i" /*s*/, "" /*,*/, " " /*a*/, "" /*,*/, " " /*s*/, "tring" /*,*/, " with " /*s*/, "ome " /*s*/, "p" /*a*/, "ce" /*s*/, "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', 's', 'a' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "thi", " i", " ", " ", "tring", " with ", "ome ", "p", "ce" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', 's', 'a' }, M, StringSplitOptions.TrimEntries, new[] { "thi", "", "i", "", "", "", "", "tring", "with", "ome", "p", "ce", "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ',', 's', 'a' }, M, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "thi", "i", "tring", "with", "ome", "p", "ce" })]
        [InlineData("this, is, a, very long string, with some spaces, commas and more spaces", new[] { ',', 's' }, M, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "thi", "i", "a", "very long", "tring", "with", "ome", "pace", "comma", "and more", "pace" })]
        [InlineData("    Monday    ", new[] { ',', ':' }, M, StringSplitOptions.None, new[] { "    Monday    " })]
        [InlineData("    Monday    ", new[] { ',', ':' }, M, StringSplitOptions.TrimEntries, new[] { "Monday" })]
        [InlineData("    Monday    ", new[] { ',', ':' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "    Monday    " })]
        [InlineData("    Monday    ", new[] { ',', ':' }, M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "Monday" })]
        [InlineData("              ", new[] { ',', ':' }, M, StringSplitOptions.None, new[] { "              " })]
        [InlineData("              ", new[] { ',', ':' }, M, StringSplitOptions.TrimEntries, new[] { "" })]
        [InlineData("              ", new[] { ',', ':' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "              " })]
        [InlineData("              ", new[] { ',', ':' }, M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a b ", null, 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData("a b ", new char[0], 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData("a b ", new char[] { ' ' }, 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData(" a b ", null, 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData(" a b ", new char[0], 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData(" a b ", new char[] { ' ' }, 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        public static void SplitCharArraySeparator(string value, char[] separators, int count, StringSplitOptions options, string[] expected)
        {
            Assert.Equal(expected, value.Split(separators, count, options));
            Assert.Equal(expected, value.Split(ToStringArray(separators), count, options));

            Range[] ranges = new Range[count == int.MaxValue ? value.Length + 1 : count];
            Assert.Equal(expected.Length, value.AsSpan().SplitAny(ranges, separators, options));
            Assert.Equal(expected, ranges.Take(expected.Length).Select(r => value[r]).ToArray());
        }

        [Theory]
        [InlineData("a b c", null, M, StringSplitOptions.None, new[] { "a", "b", "c" })]
        [InlineData("a b c", new string[0], M, StringSplitOptions.None, new[] { "a", "b", "c" })]
        [InlineData("a,b,c", null, M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("a,b,c", new string[0], M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("a,b,c", new string[] { null }, M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("a,b,c", new string[] { "" }, M, StringSplitOptions.None, new[] { "a,b,c" })]
        [InlineData("this, is, a, string, with some spaces", new[] { " " }, M, StringSplitOptions.None, new[] { "this,", "is,", "a,", "string,", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { " ", ", " }, M, StringSplitOptions.None, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ", ", " " }, M, StringSplitOptions.None, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", " ", "s" }, M, StringSplitOptions.None, new[] { "thi", "", "", "i", "", "", "a", "", "", "tring", "", "with", "", "ome", "", "pace", "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", " ", "s", "a" }, M, StringSplitOptions.None, new[] { "thi", "", "", "i", "", "", "", "", "", "", "tring", "", "with", "", "ome", "", "p", "ce", "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { " " }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this,", "is,", "a,", "string,", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { " ", ", " }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ", ", " " }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", " ", "s" }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "thi", "i", "a", "tring", "with", "ome", "pace" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", " ", "s", "a" }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "thi", "i", "tring", "with", "ome", "p", "ce" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", "s", "a" }, M, StringSplitOptions.None, new[] { "thi" /*s*/, "" /*,*/, " i" /*s*/, "" /*,*/, " " /*a*/, "" /*,*/, " " /*s*/, "tring" /*,*/, " with " /*s*/, "ome " /*s*/, "p" /*a*/, "ce" /*s*/, "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", "s", "a" }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "thi", " i", " ", " ", "tring", " with ", "ome ", "p", "ce" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", "s", "a" }, M, StringSplitOptions.TrimEntries, new[] { "thi", "", "i", "", "", "", "", "tring", "with", "ome", "p", "ce", "" })]
        [InlineData("this, is, a, string, with some spaces", new[] { ",", "s", "a" }, M, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "thi", "i", "tring", "with", "ome", "p", "ce" })]
        [InlineData("this, is, a, string, with some spaces, ", new[] { ",", " s" }, M, StringSplitOptions.None, new[] { "this", " is", " a", "", "tring", " with", "ome", "paces", " " })]
        [InlineData("this, is, a, string, with some spaces, ", new[] { ",", " s" }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", " is", " a", "tring", " with", "ome", "paces", " " })]
        [InlineData("this, is, a, string, with some spaces, ", new[] { ",", " s" }, M, StringSplitOptions.TrimEntries, new[] { "this", "is", "a", "", "tring", "with", "ome", "paces", "" })]
        [InlineData("this, is, a, string, with some spaces, ", new[] { ",", " s" }, M, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "this", "is", "a", "tring", "with", "ome", "paces" })]
        [InlineData("this, is, a, very long string, with some spaces, commas and more spaces", new[] { ",", " s" }, M, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "this", "is", "a", "very long", "tring", "with", "ome", "paces", "commas and more", "paces" })]
        [InlineData("    Monday    ", new[] { ",", ":" }, M, StringSplitOptions.None, new[] { "    Monday    " })]
        [InlineData("    Monday    ", new[] { ",", ":" }, M, StringSplitOptions.TrimEntries, new[] { "Monday" })]
        [InlineData("    Monday    ", new[] { ",", ":" }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "    Monday    " })]
        [InlineData("    Monday    ", new[] { ",", ":" }, M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "Monday" })]
        [InlineData("              ", new[] { ",", ":" }, M, StringSplitOptions.None, new[] { "              " })]
        [InlineData("              ", new[] { ",", ":" }, M, StringSplitOptions.TrimEntries, new[] { "" })]
        [InlineData("              ", new[] { ",", ":" }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "              " })]
        [InlineData("              ", new[] { ",", ":" }, M, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new string[0])]
        [InlineData("a b ", null, 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData("a b ", new string[0], 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData("a b ", new string[] { " " }, 2, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [InlineData(" a b ", null, 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData(" a b ", new string[0], 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        [InlineData(" a b ", new string[] { " " }, 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b" })]
        public static void SplitStringArraySeparator(string value, string[] separators, int count, StringSplitOptions options, string[] expected)
        {
            Assert.Equal(expected, value.Split(separators, count, options));

            Range[] ranges = new Range[count == int.MaxValue ? value.Length + 1 : count];
            Assert.Equal(expected.Length, value.AsSpan().SplitAny(ranges, separators, options));
            Assert.Equal(expected, ranges.Take(expected.Length).Select(r => value[r]).ToArray());
        }

        private static string[] ToStringArray(char[] source)
        {
            if (source == null)
                return null;

            string[] result = new string[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i].ToString();
            }
            return result;
        }
    }
}
