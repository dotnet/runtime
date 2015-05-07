// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

// TODO: Remove these extension methods when the actual methods are available on String in System.Runtime.dll
internal static class TemporaryStringSplitExtensions
{
    public static string[] Split(this string value, char separator)
    {
        return value.Split(new[] { separator });
    }

    public static string[] Split(this string value, char separator, StringSplitOptions options)
    {
        return value.Split(new[] { separator }, options);
    }

    public static string[] Split(this string value, char separator, int count, StringSplitOptions options)
    {
        return value.Split(new[] { separator }, count, options);
    }

    public static string[] Split(this string value, string separator)
    {
        return value.Split(new[] { separator }, StringSplitOptions.None);
    }

    public static string[] Split(this string value, string separator, StringSplitOptions options)
    {
        return value.Split(new[] { separator }, options);
    }

    public static string[] Split(this string value, string separator, int count, StringSplitOptions options)
    {
        return value.Split(new[] { separator }, count, options);
    }
}

public static class StringSplitTests
{
    [Fact]
    public static void TestSplitInvalidCount()
    {
        const string value = "a,b";
        const int count = -1;
        const StringSplitOptions options = StringSplitOptions.None;

        Assert.Throws<ArgumentOutOfRangeException>(() => value.Split(',', count, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Split(new[] { ',' }, count));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Split(new[] { ',' }, count, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Split(",", count, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Split(new[] { "," }, count, options));
    }

    [Fact]
    public static void TestSplitInvalidOptions()
    {
        const string value = "a,b";
        const int count = int.MaxValue;
        const StringSplitOptions optionsTooLow = StringSplitOptions.None - 1;
        const StringSplitOptions optionsTooHigh = StringSplitOptions.RemoveEmptyEntries + 1;

        Assert.Throws<ArgumentException>(() => value.Split(',', optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(',', optionsTooHigh));
        Assert.Throws<ArgumentException>(() => value.Split(',', count, optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(',', count, optionsTooHigh));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { ',' }, optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { ',' }, optionsTooHigh));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { ',' }, count, optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { ',' }, count, optionsTooHigh));
        Assert.Throws<ArgumentException>(() => value.Split(",", optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(",", optionsTooHigh));
        Assert.Throws<ArgumentException>(() => value.Split(",", count, optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(",", count, optionsTooHigh));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { "," }, optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { "," }, optionsTooHigh));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { "," }, count, optionsTooLow));
        Assert.Throws<ArgumentException>(() => value.Split(new[] { "," }, count, optionsTooHigh));
    }

    [Fact]
    public static void TestSplitZeroCountEmptyResult()
    {
        const string value = "a,b";
        const int count = 0;
        const StringSplitOptions options = StringSplitOptions.None;

        string[] expected = new string[0];

        Assert.Equal(expected, value.Split(',', count, options));
        Assert.Equal(expected, value.Split(new[] { ',' }, count));
        Assert.Equal(expected, value.Split(new[] { ',' }, count, options));
        Assert.Equal(expected, value.Split(",", count, options));
        Assert.Equal(expected, value.Split(new[] { "," }, count, options));
    }

    [Fact]
    public static void TestSplitEmptyValueWithRemoveEmptyEntriesOptionEmptyResult()
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
    public static void TestSplitOneCountSingleResult()
    {
        const string value = "a,b";
        const int count = 1;
        const StringSplitOptions options = StringSplitOptions.None;

        string[] expected = new[] { value };

        Assert.Equal(expected, value.Split(',', count, options));
        Assert.Equal(expected, value.Split(new[] { ',' }, count));
        Assert.Equal(expected, value.Split(new[] { ',' }, count, options));
        Assert.Equal(expected, value.Split(",", count, options));
        Assert.Equal(expected, value.Split(new[] { "," }, count, options));
    }

    [Fact]
    public static void TestSplitNoMatchSingleResult()
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
    public static void TestSplitCharSeparator(string value, char separator, int count, StringSplitOptions options, string[] expected)
    {
        Assert.Equal(expected, value.Split(separator, count, options));
        Assert.Equal(expected, value.Split(new[] { separator }, count, options));
        Assert.Equal(expected, value.Split(separator.ToString(), count, options));
        Assert.Equal(expected, value.Split(new[] { separator.ToString() }, count, options));
    }

    [Theory]
    [InlineData("a,b,c", null, M, StringSplitOptions.None, new[] { "a,b,c" })]
    [InlineData("a,b,c", "", M, StringSplitOptions.None, new[] { "a,b,c" })]
    [InlineData("aaabaaabaaa", "aa", M, StringSplitOptions.None, new[] { "", "ab", "ab", "a" })]
    [InlineData("aaabaaabaaa", "aa", M, StringSplitOptions.RemoveEmptyEntries, new[] { "ab", "ab", "a" })]
    [InlineData("this, is, a, string, with some spaces", ", ", M, StringSplitOptions.None, new[] { "this", "is", "a", "string", "with some spaces" })]
    public static void TestSplitStringSeparator(string value, string separator, int count, StringSplitOptions options, string[] expected)
    {
        Assert.Equal(expected, value.Split(separator, count, options));
        Assert.Equal(expected, value.Split(new[] { separator }, count, options));
    }

    [Theory]
    [InlineData("a b c", null, M, StringSplitOptions.None, new[] { "a", "b", "c" })]
    [InlineData("a b c", new char[0], M, StringSplitOptions.None, new[] { "a", "b", "c" })]
    [InlineData("a,b,c", null, M, StringSplitOptions.None, new[] { "a,b,c" })]
    [InlineData("a,b,c", new char[0], M, StringSplitOptions.None, new[] { "a,b,c" })]
    [InlineData("this, is, a, string, with some spaces", new[] { ' ', ',' }, M, StringSplitOptions.None, new[] { "this", "", "is", "", "a", "", "string", "", "with", "some", "spaces" })]
    [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ' }, M, StringSplitOptions.None, new[] { "this", "", "is", "", "a", "", "string", "", "with", "some", "spaces" })]
    [InlineData("this, is, a, string, with some spaces", new[] { ' ', ',' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
    [InlineData("this, is, a, string, with some spaces", new[] { ',', ' ' }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
    public static void TestSplitCharArraySeparator(string value, char[] separators, int count, StringSplitOptions options, string[] expected)
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
    [InlineData("this, is, a, string, with some spaces", new[] { " ", ", " }, M, StringSplitOptions.None, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
    [InlineData("this, is, a, string, with some spaces", new[] { ", ", " " }, M, StringSplitOptions.None, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
    [InlineData("this, is, a, string, with some spaces", new[] { " ", ", " }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
    [InlineData("this, is, a, string, with some spaces", new[] { ", ", " " }, M, StringSplitOptions.RemoveEmptyEntries, new[] { "this", "is", "a", "string", "with", "some", "spaces" })]
    public static void TestSplitStringArraySeparator(string value, string[] separators, int count, StringSplitOptions options, string[] expected)
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
