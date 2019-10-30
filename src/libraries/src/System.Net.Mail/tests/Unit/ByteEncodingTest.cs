// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class ByteEncodingTest
    {
        [Theory]
        [InlineData("some test header")]
        [InlineData("some test header that is really long some test header that is really long some test header that is really long some test header that is really long some test header")]
        public void EncodeHeader_WithNoUnicode_ShouldNotEncode(string testHeader)
        {
            string result = MimeBasePart.EncodeHeaderValue(testHeader, Encoding.UTF8, true);
            Assert.StartsWith("some test", result, StringComparison.Ordinal);
            Assert.EndsWith("header", result, StringComparison.Ordinal);

            foreach (char c in result)
            {
                Assert.InRange((byte)c, 0, 128);
            }

            Assert.Equal(testHeader, MimeBasePart.DecodeHeaderValue(result));
        }

        [Theory]
        [InlineData("some test h\xE9ader to base64asdf\xE9\xE5", 1)]
        [InlineData("some test header to base64 \xE5 \xF8\xEE asdf\xE9encode that contains some unicodeasdf\xE9\xE5 and is really really long and stuff ", 3)]
        public void EncoderAndDecoder_ShouldEncodeAndDecode(string testHeader, int expectedFoldedCount)
        {
            string result = MimeBasePart.EncodeHeaderValue(testHeader, Encoding.UTF8, true);
            Assert.StartsWith("=?utf-8?B?", result, StringComparison.Ordinal);
            Assert.EndsWith("?=", result, StringComparison.Ordinal);

            string[] foldedHeaders = result.Split('\r');
            Assert.Equal(expectedFoldedCount, foldedHeaders.Length);
            foreach (string foldedHeader in foldedHeaders)
            {
                Assert.InRange(foldedHeader.Length, 0, 76);
            }

            Assert.Equal(testHeader, MimeBasePart.DecodeHeaderValue(result));
        }

        [Theory]
        [InlineData("some test header to base64", 1)]
        [InlineData("some test header to base64asdf \xE9\xE5 encode that contains some unicode \xE5 \xF8\xEE asdf\xE9\xE5 and is really really long and stuff ", 3)]
        public void EncoderAndDecoder_WithQEncodedString_AndNoUnicode_AndShortHeader_ShouldEncodeAndDecode(
            string testHeader, int expectedFoldedCount)
        {
            string result = MimeBasePart.EncodeHeaderValue(testHeader, Encoding.UTF8, false);

            string[] foldedHeaders = result.Split(new string[] { "\r\n " }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(expectedFoldedCount, foldedHeaders.Length);
            foreach (string foldedHeader in foldedHeaders)
            {
                Assert.InRange(foldedHeader.Length, 0, 76);
            }

            Assert.Equal(testHeader, MimeBasePart.DecodeHeaderValue(result));
        }
    }
}
