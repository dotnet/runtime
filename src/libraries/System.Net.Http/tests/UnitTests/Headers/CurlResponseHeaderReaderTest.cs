// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace System.Net.Http.Tests
{
    public class CurlResponseParseUtilsTest
    {
        private const string StatusCodeTemplate = "HTTP/1.1 {0} {1}";
        private const string MissingSpaceFormat = "HTTP/1.1 {0}InvalidPhrase";

        private const string StatusCodeVersionFormat = "HTTP/{0}.{1} 200 OK";

        private const string ValidHeader = "Content-Type: text/xml; charset=utf-8";
        private const string HeaderNameWithInvalidChar = "Content{0}Type: text/xml; charset=utf-8";

        private const string invalidChars = "()<>@,;\\\"/[]?={} \t";

        public readonly static IEnumerable<object[]> ValidStatusCodeLines = GetStatusCodeLines(StatusCodeTemplate);
        public readonly static IEnumerable<object[]> InvalidStatusCodeLines = GetStatusCodeLines(MissingSpaceFormat);
        public readonly static IEnumerable<object[]> StatusCodeVersionLines = GetStatusCodeLinesForVersions(1, 10);
        public readonly static IEnumerable<object[]> InvalidHeaderLines = GetInvalidHeaderLines();

        private static IEnumerable<object[]> GetStatusCodeLines(string template)
        {
            const string reasonPhrase = "Test Phrase";
            foreach(int code in Enum.GetValues(typeof(HttpStatusCode)))
            {
                yield return new object[] { string.Format(template, code, reasonPhrase), code, reasonPhrase};
            }
        }

        private static IEnumerable<object[]> GetStatusCodeLinesForVersions(int min, int max)
        {
            for(int major = min; major < max; major++)
            {
                for(int minor = min; minor < max; minor++)
                {
                    yield return new object[] {string.Format(StatusCodeVersionFormat, major, minor), major, minor};
                }
            }
        }

        private static IEnumerable<object[]> GetInvalidHeaderLines()
        {
            foreach(char c in invalidChars)
            {
                yield return new object[] { string.Format(HeaderNameWithInvalidChar, c) };
            }
        }

        #region StatusCode
        [Theory, MemberData(nameof(ValidStatusCodeLines))]
        public unsafe void ReadStatusLine_ValidStatusCode_ResponseMessageValueSet(string statusLine, HttpStatusCode expectedCode, string expectedPhrase)
        {
            byte[] buffer = statusLine.Select(c => checked((byte)c)).ToArray();

            fixed (byte* pBuffer = buffer)
            {
                var reader = new CurlResponseHeaderReader(new IntPtr(pBuffer), checked((ulong)buffer.Length));
                var response = new HttpResponseMessage();
                Assert.True(reader.ReadStatusLine(response));
                Assert.Equal(expectedCode, response.StatusCode);
                Assert.Equal(expectedPhrase, response.ReasonPhrase);
            }
        }

        [Theory, MemberData(nameof(InvalidStatusCodeLines))]
        public unsafe void ReadStatusLine_InvalidStatusCode_ThrowsHttpRequestException(string statusLine, HttpStatusCode expectedCode, string phrase)
        {
            byte[] buffer = statusLine.Select(c => checked((byte)c)).ToArray();

            fixed (byte* pBuffer = buffer)
            {
                var reader = new CurlResponseHeaderReader(new IntPtr(pBuffer), checked((ulong)buffer.Length));
                var response = new HttpResponseMessage();
                Assert.Throws<HttpRequestException>(() => reader.ReadStatusLine(response));
            }
        }

        [Theory, MemberData(nameof(StatusCodeVersionLines))]
        public unsafe void ReadStatusLine_ValidStatusCodeLine_ResponseMessageVersionSet(string statusLine, int major, int minor)
        {
            byte[] buffer = statusLine.Select(c => checked((byte)c)).ToArray();

            fixed (byte* pBuffer = buffer)
            {
                var reader = new CurlResponseHeaderReader(new IntPtr(pBuffer), checked((ulong)buffer.Length));
                var response = new HttpResponseMessage();
                Assert.True(reader.ReadStatusLine(response));
                int expectedMajor = 0;
                int expectedMinor = 0;
                if (major == 1 && (minor == 0 || minor == 1))
                {
                    expectedMajor = 1;
                    expectedMinor = minor;
                }

                Assert.Equal(expectedMajor, response.Version.Major);
                Assert.Equal(expectedMinor, response.Version.Minor);
            }
        }

        #endregion

        #region Headers
        [Theory]
        [InlineData("TestHeader", "Test header value", false)]
        [InlineData("TestHeader", "Test header value", true)]
        [InlineData("TestHeader", "", false)]
        [InlineData("TestHeader", "", true)]
        [InlineData("Server", "IIS", false)]
        [InlineData("Server", "IIS", true)]
        public unsafe void ReadHeader_ValidHeaderLine_HeaderReturned(string expectedHeaderName, string expectedHeaderValue, bool spaceAfterColon)
        {
            string headerLine = $"{expectedHeaderName}:{(spaceAfterColon ? " " : "")}{expectedHeaderValue}";

            byte[] buffer = headerLine.Select(c => checked((byte)c)).ToArray();

            fixed (byte* pBuffer = buffer)
            {
                var reader = new CurlResponseHeaderReader(new IntPtr(pBuffer), checked((ulong)buffer.Length));

                string headerName;
                string headerValue;
                Assert.True(reader.ReadHeader(out headerName, out headerValue));
                Assert.Equal(expectedHeaderName, headerName);
                Assert.Equal(expectedHeaderValue, headerValue);
            }
        }

        [Fact]
        public unsafe void ReadHeader_EmptyBuffer_ReturnsFalse()
        {
            byte[] buffer = new byte[2]; // Non-empty array so we can get a valid pointer using fixed.
            ulong length = 0; // But a length of 0 for empty.

            fixed (byte* pBuffer = buffer)
            {
                var reader = new CurlResponseHeaderReader(new IntPtr(pBuffer), length);

                string headerName;
                string headerValue;
                Assert.False(reader.ReadHeader(out headerName, out headerValue));
                Assert.Null(headerName);
                Assert.Null(headerValue);
            }
        }

        [Theory, MemberData(nameof(InvalidHeaderLines))]
        public unsafe void ReadHeaderName_InvalidHeaderLine_ThrowsHttpRequestException(string headerLine)
        {
            byte[] buffer = headerLine.Select(c => checked((byte)c)).ToArray();

            fixed (byte* pBuffer = buffer)
            {
                var reader = new CurlResponseHeaderReader(new IntPtr(pBuffer), checked((ulong)buffer.Length));

                string headerName;
                string headerValue;
                Assert.Throws<HttpRequestException>(() => reader.ReadHeader(out headerName, out headerValue));
            }
        }
        #endregion
    }
}
