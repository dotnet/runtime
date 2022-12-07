// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public static class TrimTests
    {
        [Fact]
        public static void EmptyInput()
        {
            Assert.Equal(default(Range), Ascii.Trim(ReadOnlySpan<byte>.Empty));
            Assert.Equal(default(Range), Ascii.Trim(ReadOnlySpan<char>.Empty));
            Assert.Equal(default(Range), Ascii.TrimStart(ReadOnlySpan<byte>.Empty));
            Assert.Equal(default(Range), Ascii.TrimStart(ReadOnlySpan<char>.Empty));
            Assert.Equal(default(Range), Ascii.TrimEnd(ReadOnlySpan<byte>.Empty));
            Assert.Equal(default(Range), Ascii.TrimEnd(ReadOnlySpan<char>.Empty));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("abc")]
        [InlineData("a\tb c\rd\ne")]
        public static void NothingToTrimNonEmptyInput(string text)
        {
            ReadOnlySpan<byte> bytes = Encoding.ASCII.GetBytes(text);

            Range expected = 0..text.Length;
            Assert.Equal(expected, Ascii.Trim(bytes));
            Assert.Equal(expected, Ascii.Trim(text));
            Assert.Equal(expected, Ascii.TrimStart(bytes));
            Assert.Equal(expected, Ascii.TrimStart(text));
            Assert.Equal(expected, Ascii.TrimEnd(bytes));
            Assert.Equal(expected, Ascii.TrimEnd(text));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData(" \t\r\n ")]
        [InlineData("\n \t \r")]
        public static void OnlyWhitespaces(string text)
        {
            ReadOnlySpan<byte> bytes = Encoding.ASCII.GetBytes(text);

            Assert.Equal(text.Length..text.Length, Ascii.Trim(bytes));
            Assert.Equal(text.Length..text.Length, Ascii.Trim(text));
            Assert.Equal(text.Length..text.Length, Ascii.TrimStart(bytes));
            Assert.Equal(text.Length..text.Length, Ascii.TrimStart(text));
            // Special-case when the input contains all-whitespace data, since we want to
            // return a zero-length slice at the *beginning* of the span, not the end of the span
            Assert.Equal(0..0, Ascii.TrimEnd(bytes));
            Assert.Equal(0..0, Ascii.TrimEnd(text));
        }

        [Theory]
        [InlineData(" a", 1)]
        [InlineData("\tb", 1)]
        [InlineData("\rc", 1)]
        [InlineData("\nd", 1)]
        [InlineData(" \t\r\ne", 4)]
        [InlineData(" \t\r\n\n\r\t f", 8)]
        public static void StartingWithWhitespace(string text, int leadingWhitespaceCount)
        {
            ReadOnlySpan<byte> bytes = Encoding.ASCII.GetBytes(text);

            Assert.Equal(leadingWhitespaceCount..text.Length, Ascii.TrimStart(bytes));
            Assert.Equal(leadingWhitespaceCount..text.Length, Ascii.TrimStart(text));
            Assert.Equal(leadingWhitespaceCount..text.Length, Ascii.Trim(bytes));
            Assert.Equal(leadingWhitespaceCount..text.Length, Ascii.Trim(text));
            Assert.Equal(0..text.Length, Ascii.TrimEnd(bytes));
            Assert.Equal(0..text.Length, Ascii.TrimEnd(text));
        }

        [Theory]
        [InlineData("a ", 1)]
        [InlineData("b\t", 1)]
        [InlineData("c\r", 1)]
        [InlineData("d\n", 1)]
        [InlineData("e \t\r\n", 4)]
        [InlineData("f \t\r\n\n\r\t ", 8)]
        public static void EndingWithWhitespace(string text, int trailingWhitespaceCount)
        {
            ReadOnlySpan<byte> bytes = Encoding.ASCII.GetBytes(text);

            Assert.Equal(0..(text.Length - trailingWhitespaceCount), Ascii.TrimEnd(bytes));
            Assert.Equal(0..(text.Length - trailingWhitespaceCount), Ascii.TrimEnd(text));
            Assert.Equal(0..(text.Length - trailingWhitespaceCount), Ascii.Trim(bytes));
            Assert.Equal(0..(text.Length - trailingWhitespaceCount), Ascii.Trim(text));
            Assert.Equal(0..text.Length, Ascii.TrimStart(bytes));
            Assert.Equal(0..text.Length, Ascii.TrimStart(text));
        }

        [Theory]
        [InlineData(" a ", 1, 1)]
        [InlineData("\tb\t", 1, 1)]
        [InlineData("\rc\r", 1, 1)]
        [InlineData("\nd\n", 1, 1)]
        [InlineData(" \t\r\ne \t\r\n", 4, 4)]
        [InlineData(" \t\r\n\n\r\t f \t\r\n\n\r\t ", 8, 8)]
        public static void StartingAndEndingWithWhitespace(string text, int leadingWhitespaceCount, int trailingWhitespaceCount)
        {
            ReadOnlySpan<byte> bytes = Encoding.ASCII.GetBytes(text);

            Assert.Equal(leadingWhitespaceCount..text.Length, Ascii.TrimStart(bytes));
            Assert.Equal(leadingWhitespaceCount..text.Length, Ascii.TrimStart(text));
            Assert.Equal(leadingWhitespaceCount..(text.Length - trailingWhitespaceCount), Ascii.Trim(bytes));
            Assert.Equal(leadingWhitespaceCount..(text.Length - trailingWhitespaceCount), Ascii.Trim(text));
            Assert.Equal(0..(text.Length - trailingWhitespaceCount), Ascii.TrimEnd(bytes));
            Assert.Equal(0..(text.Length - trailingWhitespaceCount), Ascii.TrimEnd(text));
        }
    }
}
