// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class Base64EncodingTest
    {
        [Theory]
        [InlineData("some test header to base64 encode")]
        [InlineData("some test h\xE9ader to base64asdf\xE9\xE5")]
        public void Base64Stream_WithBasicAsciiString_ShouldEncodeAndDecode(string testHeader)
        {
            var s = new Base64Stream(new Base64WriteStateInfo());
            var testHeaderBytes = Encoding.UTF8.GetBytes(testHeader);
            s.EncodeBytes(testHeaderBytes, 0, testHeaderBytes.Length);

            string encodedString = s.GetEncodedString();
            for (int i = 0; i < encodedString.Length; i++)
            {
                Assert.InRange((byte)encodedString[i], 0, 127);
            }

            byte[] stringToDecode = Encoding.ASCII.GetBytes(encodedString);
            int result = s.DecodeBytes(stringToDecode, 0, encodedString.Length);
            Assert.Equal(testHeader, Encoding.UTF8.GetString(stringToDecode, 0, result));
        }

        [Fact]
        public void Base64Stream_WithVerySmallBuffer_ShouldTriggerBufferResize_AndShouldEncodeProperly()
        {
            var s = new Base64Stream(new Base64WriteStateInfo(10, new byte[0], new byte[0], 70, 0));

            const string TestString = "0123456789abcdef";

            byte[] buffer = Encoding.UTF8.GetBytes(TestString);
            s.EncodeBytes(buffer, 0, buffer.Length);
            string encodedString = s.GetEncodedString();

            Assert.Equal("MDEyMzQ1Njc4OWFiY2RlZg==", encodedString);

            byte[] stringToDecode = Encoding.ASCII.GetBytes(encodedString);
            int result = s.DecodeBytes(stringToDecode, 0, encodedString.Length);

            Assert.Equal(TestString, Encoding.UTF8.GetString(stringToDecode, 0, result));
        }

        [Fact]
        public void Base64Stream_WithVeryLongString_ShouldEncodeProperly()
        {
            var writeStateInfo = new Base64WriteStateInfo(10, new byte[0], new byte[0], 70, 0);
            var s = new Base64Stream(writeStateInfo);

            byte[] buffer = Encoding.UTF8.GetBytes(LongString);
            s.EncodeBytes(buffer, 0, buffer.Length);
            string encodedString = s.GetEncodedString();

            byte[] stringToDecode = Encoding.ASCII.GetBytes(encodedString);
            int result = s.DecodeBytes(stringToDecode, 0, encodedString.Length);

            Assert.Equal(LongString, Encoding.UTF8.GetString(stringToDecode, 0, result));
        }

        private const string LongString =
@"01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567
01234567
11234567
21234567
31234567
41234567
51234567
61234567
71234567
81234567
91234567";
    }
}
