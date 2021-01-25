// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Encodings.Tests
{
    public class DecoderTests
    {
        [Fact]
        public static void GetCharCount_Span_MatchesEncodingCharCount()
        {
            const string TextString = "hello world";
            Encoding e = Encoding.UTF8;
            byte[] textBytes = e.GetBytes(TextString);

            Assert.Equal(TextString.Length, e.GetDecoder().GetCharCount(textBytes.AsSpan(), flush: true));
        }

        [Fact]
        public static void GetChars_Span_MatchesEncodingGetChars()
        {
            const string TextString = "hello world";
            Encoding e = Encoding.UTF8;
            byte[] textBytes = e.GetBytes(TextString);

            char[] chars = new char[TextString.Length];
            Assert.Equal(chars.Length, e.GetDecoder().GetChars(textBytes, chars.AsSpan(), flush: true));
            Assert.Equal(TextString, new string(chars));
        }

        [Fact]
        public static void Convert_Span_MatchesGetChars()
        {
            const string TextString = "hello world";
            Encoding e = Encoding.UTF8;
            Decoder decoder = e.GetDecoder();
            byte[] textBytes = e.GetBytes(TextString);
            char[] chars;

            chars = new char[TextString.Length];
            decoder.Convert(textBytes.AsSpan(), chars.AsSpan(0, 2), true, out int bytesUsed, out int charsUsed, out bool completed);
            Assert.Equal("he", new string(chars, 0, 2));
            Assert.Equal(2, bytesUsed);
            Assert.Equal(2, charsUsed);
            Assert.False(completed);

            chars = new char[TextString.Length];
            decoder.Convert(textBytes.AsSpan(), chars.AsSpan(), true, out bytesUsed, out charsUsed, out completed);
            Assert.Equal(TextString, new string(chars));
            Assert.Equal(textBytes.Length, bytesUsed);
            Assert.Equal(TextString.Length, charsUsed);
            Assert.True(completed);
        }
    }
}
