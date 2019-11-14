// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class WinHttpResponseHeaderReaderTest
    {
        private static readonly KeyValuePair<string, string>[] s_emptyHeaders = Array.Empty<KeyValuePair<string, string>>();

        [Fact]
        public void ReadHeader_WithStatusLine_CanSkipStatusLineAndReadHeader()
        {
            char[] array = "HTTP/1.0 200 OK\r\nServer: Apache".ToCharArray();
            var reader = new WinHttpResponseHeaderReader(array, 0, array.Length);

            Assert.True(reader.ReadLine());

            string name;
            string value;
            Assert.True(reader.ReadHeader(out name, out value));
            Assert.Equal("Server", name);
            Assert.Equal("Apache", value);
        }

        [Fact]
        public void ReadHeader_KnownHeaderName_SameKnownHeaderNameObjectReturned()
        {
            char[] array = "Server: Apache".ToCharArray();
            var reader = new WinHttpResponseHeaderReader(array, 0, array.Length);

            string name;
            string value;
            Assert.True(reader.ReadHeader(out name, out value));
            Assert.Same(HttpKnownHeaderNames.Server, name);
        }

        [Theory]
        [InlineData("Content-Encoding: gzip")]
        [InlineData("Content-Encoding: deflate")]
        public void ReadHeader_KnownHeaderValue_SameHeaderValueObjectReturned(string header)
        {
            char[] array = (header + "\r\n" + header).ToCharArray();
            var reader = new WinHttpResponseHeaderReader(array, 0, array.Length);

            string name1;
            string value1;
            Assert.True(reader.ReadHeader(out name1, out value1));

            string name2;
            string value2;
            Assert.True(reader.ReadHeader(out name2, out value2));

            Assert.Same(value1, value2);
        }

        [Theory]
        [MemberData(nameof(HeaderData))]
        public void ReadHeader_VariousInputs_MatchesExpectedBehavior(string raw, KeyValuePair<string, string>[] expectedHeaders)
        {
            char[] array = raw.ToCharArray();
            var reader = new WinHttpResponseHeaderReader(array, 0, array.Length);

            string name;
            string value;

            for (int i = 0; i < expectedHeaders.Length; i++)
            {
                string expectedName = expectedHeaders[i].Key;
                string expectedValue = expectedHeaders[i].Value;

                Assert.True(reader.ReadHeader(out name, out value));
                Assert.Equal(expectedName, name);
                Assert.Equal(expectedValue, value);
            }

            Assert.False(reader.ReadHeader(out name, out value));
            Assert.Null(name);
            Assert.Null(value);
        }

        public static object[][] HeaderData =
        {
            new object[] { "", s_emptyHeaders },
            new object[] { "    ", s_emptyHeaders },
            new object[] { "\t", s_emptyHeaders },
            new object[] { "\r\n", s_emptyHeaders },
            new object[] { "\r\n\r\n", s_emptyHeaders },
            new object[] { "\r\n\r\n\r\n", s_emptyHeaders },

            new object[] { "Content-Length: 50", new[] { CreateHeader("Content-Length", "50") } },

            new object[] { "X-Custom-Header: Foo", new[] { CreateHeader("X-Custom-Header", "Foo") } },

            new object[]
            {
                "Content-Length: 50\r\n" +
                "Content-Encoding: gzip\r\n" +
                "X-Powered-By: .NET",
                new[]
                {
                    CreateHeader("Content-Length", "50"),
                    CreateHeader("Content-Encoding", "gzip"),
                    CreateHeader("X-Powered-By", ".NET")
                }
            },

            // No colon in a line, should be skipped
            new object[]
            {
                "Content-Length: 50\r\n" +
                "no colon, should be skipped\r\n" +
                "X-Powered-By: .NET",
                new[]
                {
                    CreateHeader("Content-Length", "50"),
                    CreateHeader("X-Powered-By", ".NET")
                }
            },

            // Empty lines (middle line) should be skipped
            new object[]
            {
                "Content-Length: 50\r\n" +
                "\r\n" +
                "X-Powered-By: .NET",
                new[]
                {
                    CreateHeader("Content-Length", "50"),
                    CreateHeader("X-Powered-By", ".NET")
                }
            },

            new object[]
            {
                "Content-Length: 50\r\n" +
                "" +
                "X-Powered-By: .NET",
                new[]
                {
                    CreateHeader("Content-Length", "50"),
                    CreateHeader("X-Powered-By", ".NET")
                }
            },

            // Empty lines (last line) should be skipped
            new object[]
            {
                "Content-Length: 50\r\n" +
                "Content-Encoding: deflate\r\n" +
                "X-Powered-By: .NET\r\n",
                new[]
                {
                    CreateHeader("Content-Length", "50"),
                    CreateHeader("Content-Encoding", "deflate"),
                    CreateHeader("X-Powered-By", ".NET")
                }
            },

            // Values should be trimmed
            new object[]
            {
                "Content-Length:   50    \r\n" +
                "Content-Encoding:    brotli    \r\n" +
                "X-Powered-By:    .NET    ",
                new[]
                {
                    CreateHeader("Content-Length", "50"),
                    CreateHeader("Content-Encoding", "brotli"),
                    CreateHeader("X-Powered-By", ".NET")
                }
            }
        };

        private static KeyValuePair<string, string> CreateHeader(string name, string value)
        {
            return new KeyValuePair<string, string>(name, value);
        }
    }
}
