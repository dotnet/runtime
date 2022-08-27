// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class StringSplitTests
    {
        [Fact]
        public static void SplitInvalidCount()
        {
            const string value = "a,b";
            const int count = -1;
            const StringSplitOptions options = StringSplitOptions.None;

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => value.Split(',', count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => value.Split(',', count, options));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => value.Split(new[] { ',' }, count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => value.Split(new[] { ',' }, count, options));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => value.Split(",", count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => value.Split(",", count, options));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => value.Split(new[] { "," }, count, options));
        }

        [Fact]
        public static void SplitInvalidOptions()
        {
            const string value = "a,b";
            const int count = int.MaxValue;
            const StringSplitOptions optionsTooLow = StringSplitOptions.None - 1;
            const StringSplitOptions optionsTooHigh = (StringSplitOptions)0x04;

            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(',', optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(',', optionsTooHigh));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(',', count, optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(',', count, optionsTooHigh));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { ',' }, optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { ',' }, optionsTooHigh));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { ',' }, count, optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { ',' }, count, optionsTooHigh));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(",", optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(",", optionsTooHigh));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(",", count, optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(",", count, optionsTooHigh));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { "," }, optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { "," }, optionsTooHigh));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { "," }, count, optionsTooLow));
            AssertExtensions.Throws<ArgumentException>("options", () => value.Split(new[] { "," }, count, optionsTooHigh));
        }

        [Fact]
        public static void SplitZeroCountEmptyResult()
        {
            const string value = "a,b";
            const int count = 0;
            const StringSplitOptions options = StringSplitOptions.None;

            string[] expected = new string[0];

            Assert.Equal(expected, value.Split(',', count));
            Assert.Equal(expected, value.Split(',', count, options));
            Assert.Equal(expected, value.Split(new[] { ',' }, count));
            Assert.Equal(expected, value.Split(new[] { ',' }, count, options));
            Assert.Equal(expected, value.Split(",", count));
            Assert.Equal(expected, value.Split(",", count, options));
            Assert.Equal(expected, value.Split(new[] { "," }, count, options));
        }

        [Fact]
        public static void SplitEmptyValueWithRemoveEmptyEntriesOptionEmptyResult()
        {
            string value = string.Empty;
            const int count = int.MaxValue;
            const StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries;

            string[] expected = new string[0];

            Assert.Equal(expected, value.Split(',', options));
            Assert.Equal(expected, value.Split(',', count, options));
            Assert.Equal(expected, value.Split(new[] { ',' }, options));
            Assert.Equal(expected, value.Split(new[] { ',' }, count, options));
            Assert.Equal(expected, value.Split(",", options));
            Assert.Equal(expected, value.Split(",", count, options));
            Assert.Equal(expected, value.Split(new[] { "," }, options));
            Assert.Equal(expected, value.Split(new[] { "," }, count, options));
        }

        [Fact]
        public static void SplitOneCountSingleResult()
        {
            const string value = "a,b";
            const int count = 1;
            const StringSplitOptions options = StringSplitOptions.None;

            string[] expected = new[] { value };

            Assert.Equal(expected, value.Split(',', count));
            Assert.Equal(expected, value.Split(',', count, options));
            Assert.Equal(expected, value.Split(new[] { ',' }, count));
            Assert.Equal(expected, value.Split(new[] { ',' }, count, options));
            Assert.Equal(expected, value.Split(",", count));
            Assert.Equal(expected, value.Split(",", count, options));
            Assert.Equal(expected, value.Split(new[] { "," }, count, options));
        }

        [Fact]
        public static void SplitNoMatchSingleResult()
        {
            const string value = "a b";
            const int count = int.MaxValue;
            const StringSplitOptions options = StringSplitOptions.None;

            string[] expected = new[] { value };

            Assert.Equal(expected, value.Split(','));
            Assert.Equal(expected, value.Split(',', options));
            Assert.Equal(expected, value.Split(',', count, options));
            Assert.Equal(expected, value.Split(new[] { ',' }));
            Assert.Equal(expected, value.Split(new[] { ',' }, options));
            Assert.Equal(expected, value.Split(new[] { ',' }, count));
            Assert.Equal(expected, value.Split(new[] { ',' }, count, options));
            Assert.Equal(expected, value.Split(","));
            Assert.Equal(expected, value.Split(",", options));
            Assert.Equal(expected, value.Split(",", count, options));
            Assert.Equal(expected, value.Split(new[] { "," }, options));
            Assert.Equal(expected, value.Split(new[] { "," }, count, options));
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
        }

        [Theory]
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
        public static void SplitCharArraySeparator(string value, char[] separators, int count, StringSplitOptions options, string[] expected)
        {
            Assert.Equal(expected, value.Split(separators, count, options));
            Assert.Equal(expected, value.Split(ToStringArray(separators), count, options));
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
        public static void SplitStringArraySeparator(string value, string[] separators, int count, StringSplitOptions options, string[] expected)
        {
            Assert.Equal(expected, value.Split(separators, count, options));
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
