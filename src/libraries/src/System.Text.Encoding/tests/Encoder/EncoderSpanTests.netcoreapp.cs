// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Encodings.Tests
{
    public partial class EncoderTests
    {
        [Fact]
        public static void GetByteCount_Span_MatchesEncodingByteCount()
        {
            const string TextString = "hello world";
            Encoding e = Encoding.UTF8;
            Assert.Equal(e.GetByteCount(TextString), e.GetEncoder().GetByteCount(TextString.AsReadOnlySpan(), flush: true));
        }

        [Fact]
        public static void GetBytes_Span_MatchesEncodingGetBytes()
        {
            const string TextString = "hello world";
            Encoding e = Encoding.UTF8;

            byte[] bytes = new byte[e.GetByteCount(TextString)];
            Assert.Equal(bytes.Length, e.GetEncoder().GetBytes(TextString.AsReadOnlySpan(), bytes, flush: true));
            Assert.Equal(e.GetBytes(TextString), bytes);
        }

        [Fact]
        public static void Convert_Span_MatchesGetBytes()
        {
            const string TextString = "hello world";
            Encoding encoding = Encoding.UTF8;
            Encoder encoder = encoding.GetEncoder();
            byte[] bytes;

            bytes = new byte[encoding.GetByteCount(TextString)];
            encoder.Convert(TextString.AsReadOnlySpan(), bytes.AsSpan().Slice(0, 2), true, out int charsUsed, out int bytesUsed, out bool completed);
            Assert.Equal(encoding.GetBytes(TextString).AsSpan().Slice(0, 2).ToArray(), bytes.AsSpan().Slice(0, 2).ToArray());
            Assert.Equal(2, charsUsed);
            Assert.Equal(2, bytesUsed);
            Assert.False(completed);

            bytes = new byte[encoding.GetByteCount(TextString)];
            encoder.Convert(TextString.AsReadOnlySpan(), bytes, true, out charsUsed, out bytesUsed, out completed);
            Assert.Equal(encoding.GetBytes(TextString), bytes);
            Assert.Equal(TextString.Length, charsUsed);
            Assert.Equal(bytes.Length, bytesUsed);
            Assert.True(completed);
        }
    }
}
