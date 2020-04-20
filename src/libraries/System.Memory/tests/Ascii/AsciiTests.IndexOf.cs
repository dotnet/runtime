// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace System.Buffers.Text.Tests
{
    public partial class AsciiUnitTests
    {
        public static IEnumerable<(string text, string value, int firstIndex, int lastIndex, int firstIndexIgnoreCase, int lastIndexIgnoreCase)> IndexOfAsciiCommonTestData()
        {
            yield return ("", "", 0, 0, 0, 0);
            yield return ("\0", "", 0, 1, 0, 1);
            yield return ("", "\0", -1, -1, -1, -1);
            yield return (" Hello ", "l", 3, 4, 3, 4);
            yield return (" Hello ", "h", -1, -1, 1, 1);
            yield return ("xxyzXYZz", "xyz", 1, 1, 1, 4);
        }

        public static IEnumerable<(string text, string value, int firstIndex, int lastIndex, int firstIndexIgnoreCase, int lastIndexIgnoreCase)> IndexOfBytesBytesTestData()
        {
            // Exact byte comparisons for non-ASCII bytes
            yield return ("xx\u00C0yy", "x\u00C0y", 1, 1, 1, 1);
            yield return ("xX\u00C0yY", "x\u00C0Y", -1, -1, 1, 1);
            yield return ("xx\u00C0yy", "x\u00E0y", -1, -1, -1, -1); // U+00C0 and U+00E0 are non-ASCII so shouldn't case-convert
        }

        public static IEnumerable<(string text, string value, int firstIndex, int lastIndex, int firstIndexIgnoreCase, int lastIndexIgnoreCase)> IndexOfBytesCharsTestData()
        {
            // Non-ASCII bytes should never compare as equal to non-ASCII chars
            yield return ("xx\u00C0yy", "\u00C0", -1, -1, -1, -1);
            yield return ("xX\u00C0yY", "\u00C0", -1, -1, -1, -1);
            yield return ("xx\u00C0yy", "\u00E0", -1, -1, -1, -1);

            // Don't normalize non-ASCII before comparisons
            yield return ("\u00C0", "A\u0300", -1, -1, -1, -1); // U+00C0 => U+0041 + U+0300 (decomposed)
            yield return ("\u00C0", "A", -1, -1, -1, -1);
        }

        [Fact]
        public void IndexOf_NullStringParamChecks()
        {
            Assert.Throws<ArgumentNullException>("value", () => Ascii.IndexOf(ReadOnlySpan<byte>.Empty, (string)null));
            Assert.Throws<ArgumentNullException>("value", () => Ascii.IndexOfIgnoreCase(ReadOnlySpan<byte>.Empty, (string)null));
            Assert.Throws<ArgumentNullException>("value", () => Ascii.LastIndexOf(ReadOnlySpan<byte>.Empty, (string)null));
            Assert.Throws<ArgumentNullException>("value", () => Ascii.LastIndexOfIgnoreCase(ReadOnlySpan<byte>.Empty, (string)null));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesBytesTestData))]
        public void IndexOf_ByteByte(string text, string value, [Alias("firstIndex")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);
            byte[] valueBytes = CharsToAsciiBytesChecked(value);

            Assert.Equal(expectedResult, Ascii.IndexOf(textBytes, valueBytes));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesBytesTestData))]
        public void IndexOf_ByteByte_IgnoreCase(string text, string value, [Alias("firstIndexIgnoreCase")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);
            byte[] valueBytes = CharsToAsciiBytesChecked(value);

            Assert.Equal(expectedResult, Ascii.IndexOfIgnoreCase(textBytes, valueBytes));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesCharsTestData))]
        public void IndexOf_ByteChar(string text, string value, [Alias("firstIndex")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);

            Assert.Equal(expectedResult, Ascii.IndexOf(textBytes, value.AsSpan()));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesCharsTestData))]
        public void IndexOf_ByteChar_IgnoreCase(string text, string value, [Alias("firstIndexIgnoreCase")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);

            Assert.Equal(expectedResult, Ascii.IndexOfIgnoreCase(textBytes, value.AsSpan()));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesBytesTestData))]
        public void LastIndexOf_ByteByte(string text, string value, [Alias("lastIndex")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);
            byte[] valueBytes = CharsToAsciiBytesChecked(value);

            Assert.Equal(expectedResult, Ascii.LastIndexOf(textBytes, valueBytes));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesBytesTestData))]
        public void LastIndexOf_ByteByte_IgnoreCase(string text, string value, [Alias("lastIndexIgnoreCase")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);
            byte[] valueBytes = CharsToAsciiBytesChecked(value);

            Assert.Equal(expectedResult, Ascii.LastIndexOfIgnoreCase(textBytes, valueBytes));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesCharsTestData))]
        public void LastIndexOf_ByteChar(string text, string value, [Alias("lastIndex")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);

            Assert.Equal(expectedResult, Ascii.LastIndexOf(textBytes, value.AsSpan()));
        }

        [Theory]
        [TupleMemberData(nameof(IndexOfAsciiCommonTestData))]
        [TupleMemberData(nameof(IndexOfBytesCharsTestData))]
        public void LastIndexOf_ByteChar_IgnoreCase(string text, string value, [Alias("lastIndexIgnoreCase")] int expectedResult)
        {
            byte[] textBytes = CharsToAsciiBytesChecked(text);

            Assert.Equal(expectedResult, Ascii.LastIndexOfIgnoreCase(textBytes, value.AsSpan()));
        }
    }
}
