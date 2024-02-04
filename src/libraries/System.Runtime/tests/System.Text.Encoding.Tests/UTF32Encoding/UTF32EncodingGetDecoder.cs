// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UTF32EncodingGetDecoder
    {
        [Fact]
        public void GetDecoder()
        {
            char[] sourceChars = "abc\u1234\uD800\uDC00defg".ToCharArray();
            char[] destinationChars = new char[10];
            byte[] bytes = new UTF32Encoding().GetBytes(sourceChars);
            int bytesUsed;
            int charsUsed;
            bool completed;
            Decoder decoder = new UTF32Encoding().GetDecoder();
            decoder.Convert(bytes, 0, 36, destinationChars, 0, 10, true, out bytesUsed, out charsUsed, out completed);
            if (completed)
            {
                Assert.Equal(sourceChars, destinationChars);
            }
        }

        [Fact]
        public void GetDecoder_NegativeTests()
        {
            char[] sourceChars = "\uD800\uDC00".ToCharArray();
            char[] destinationChars = [];
            byte[] bytes = new UTF32Encoding().GetBytes(sourceChars);
            int bytesUsed;
            int charsUsed;
            bool completed;
            Decoder decoder = new UTF32Encoding().GetDecoder();
            Assert.Throws<ArgumentException>("chars", () => decoder.Convert(bytes.AsSpan(), destinationChars.AsSpan(), true, out bytesUsed, out charsUsed, out completed));
        }
    }
}
