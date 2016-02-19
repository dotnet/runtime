// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

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

        public CurlResponseParseUtilsTest()
        {
        }

        #region StatusCode
        [Theory, MemberData(nameof(ValidStatusCodeLines))]
        public void ReadStatusLine_ValidStatusCode_ResponseMessageValueSet(string statusLine, HttpStatusCode expectedCode, string expectedPhrase)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            CurlResponseParseUtils.ReadStatusLine(response, statusLine);
            Assert.Equal<HttpStatusCode>(expectedCode, response.StatusCode);
            Assert.Equal<string>(expectedPhrase, response.ReasonPhrase);
        }

        [Theory, MemberData(nameof(InvalidStatusCodeLines))]
        public void ReadStatusLine_InvalidStatusCode_ThrowsHttpRequestException(string statusLine, HttpStatusCode expectedCode, string phrase)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            Assert.Throws<HttpRequestException>(() => CurlResponseParseUtils.ReadStatusLine(response, statusLine));
        }

        [Theory, MemberData(nameof(StatusCodeVersionLines))]
        public void ReadStatusLine_ValidStatusCodeLine_ResponseMessageVersionSet(string statusLine, int major, int minor)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            CurlResponseParseUtils.ReadStatusLine(response, statusLine);
            int expectedMajor = 0;
            int expectedMinor = 0;
            if (major == 1 && (minor == 0 || minor == 1))
            {
                expectedMajor = 1;
                expectedMinor = minor;
            }

            Assert.Equal<int>(expectedMajor, response.Version.Major);
            Assert.Equal<int>(expectedMinor, response.Version.Minor);
        }

        #endregion

        #region Headers
        [Fact]
        public void ReadHeaderName_ValidHeaderLine_HeaderReturnedAndIndexSet()
        {
            string headerName = "TestHeader";
            string headerValue = "Test header value";
            string headerLine = string.Format("{0}:{1}", headerName, headerValue);
            int index;
            Assert.Equal<string>(headerName, CurlResponseParseUtils.ReadHeaderName(headerLine, out index));
            Assert.Equal<string>(headerValue, headerLine.Substring(index));
        }

        [Fact]
        public void ReadHeaderName_ValidLineWithEmptyHeaderValue_HeaderReturnedAndIndexSet()
        {
            string headerName = "TestHeader";
            string headerLine = string.Format("{0}:", headerName);
            int index;
            Assert.Equal<string>(headerName, CurlResponseParseUtils.ReadHeaderName(headerLine, out index));
            Assert.Equal<string>(string.Empty, headerLine.Substring(index));
        }

        [Fact]
        public void ReadHeaderName_EmptyString_ReturnsNull()
        {
            int index;
            Assert.Null(CurlResponseParseUtils.ReadHeaderName(string.Empty, out index));
        }

        [Theory, MemberData(nameof(InvalidHeaderLines))]
        public void ReadHeaderName_InvalidHeaderLine_ThrowsHttpRequestException(string headerLine)
        {
            int index;
            Assert.Throws<HttpRequestException>(() => CurlResponseParseUtils.ReadHeaderName(headerLine, out index));
        }
        #endregion
    }
}
