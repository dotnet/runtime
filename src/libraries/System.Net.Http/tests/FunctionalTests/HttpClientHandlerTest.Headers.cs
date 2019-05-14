// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandlerTest_Headers : HttpClientHandlerTestBase
    {
        public HttpClientHandlerTest_Headers(ITestOutputHelper output) : base(output) { }

        private sealed class DerivedHttpHeaders : HttpHeaders { }

        [Fact]
        public async Task SendAsync_UserAgent_CorrectlyWritten()
        {
            string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.18 Safari/537.36";

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 };
                    message.Headers.TryAddWithoutValidation("User-Agent", userAgent);
                    (await client.SendAsync(message).ConfigureAwait(false)).Dispose();
                }
            },
            async server =>
            {
                HttpRequestData requestData = await server.HandleRequestAsync(HttpStatusCode.OK);

                string agent = requestData.GetSingleHeaderValue("User-Agent");
                Assert.Equal(userAgent, agent);
            });
        }

        [Theory]
        [InlineData("\u05D1\u05F1")]
        [InlineData("jp\u30A5")]
        public async Task SendAsync_InvalidHeader_Throw(string value)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                using (HttpClient client = CreateHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 };
                    Assert.True(request.Headers.TryAddWithoutValidation("bad", value));

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                }

            },
            async server =>
            {
                try
                {
                    // Client should abort at some point so this is going to throw.
                    HttpRequestData requestData = await server.HandleRequestAsync(HttpStatusCode.OK).ConfigureAwait(false);
                }
                catch (IOException) { };
            });
        }

        [Fact]
        public async Task SendAsync_SpecialCharacterHeader_Success()
        {
            string headerValue = "header name with underscore";
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 };
                    message.Headers.TryAddWithoutValidation("x-Special_name", "header name with underscore");
                    (await client.SendAsync(message).ConfigureAwait(false)).Dispose();
                }
            },
            async server =>
            {
                HttpRequestData requestData = await server.HandleRequestAsync(HttpStatusCode.OK);

                string header = requestData.GetSingleHeaderValue("x-Special_name");
                Assert.Equal(header, headerValue);
            });
        }

        [Theory]
        [InlineData("Content-Security-Policy", 4618)]
        [InlineData("RandomCustomHeader", 12345)]
        public async Task GetAsync_LargeHeader_Success(string headerName, int headerValueLength)
        {
            var rand = new Random(42);
            string headerValue = string.Concat(Enumerable.Range(0, headerValueLength).Select(_ => (char)('A' + rand.Next(26))));

            const string ContentString = "hello world";
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                using (HttpResponseMessage resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    Assert.Equal(headerValue, resp.Headers.GetValues(headerName).Single());
                    Assert.Equal(ContentString, await resp.Content.ReadAsStringAsync());
                }
            },
            async server =>
            {
                var headers = new List<HttpHeaderData>();
                headers.Add(new HttpHeaderData(headerName, headerValue));
                await server.HandleRequestAsync(HttpStatusCode.OK, headers: headers, content: ContentString);
            });
        }

        [Fact]
        public async Task GetAsync_EmptyResponseHeader_Success()
        {
            IList<HttpHeaderData> headers = new HttpHeaderData[] {
                                                new HttpHeaderData("x-test", "SendAsync_EmptyHeader_Success"),
                                                new HttpHeaderData("x-empty", ""),
                                                new HttpHeaderData("x-last", "bye") };

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpResponseMessage response = await  client.GetAsync(uri).ConfigureAwait(false);
                    // HTTP/1.1 LoopbackServer adds Connection: close and Date to responses.
                    Assert.Equal(UseHttp2 ?  headers.Count : headers.Count + 2, response.Headers.Count());
                    Assert.NotNull(response.Headers.GetValues("x-empty"));
                }
            },
            async server =>
            {
                HttpRequestData requestData = await server.HandleRequestAsync(HttpStatusCode.OK, headers);
            });
        }

        [Fact]
        public async Task GetAsync_MissingExpires_ReturnNull()
        {
             await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
             {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(uri);
                    Assert.Null(response.Content.Headers.Expires);
                }
            },
            async server =>
            {
                await server.HandleRequestAsync(HttpStatusCode.OK);
            });
        }

        [Theory]
        [InlineData("Thu, 01 Dec 1994 16:00:00 GMT", true)]
        [InlineData("-1", false)]
        [InlineData("0", false)]
        public async Task SendAsync_Expires_Success(string value, bool isValid)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 };
                    HttpResponseMessage response = await client.SendAsync(message);
                    Assert.NotNull(response.Content.Headers.Expires);
                    // Invalid date should be converted to MinValue so everything is expired.
                    Assert.Equal(isValid ? DateTime.Parse(value) : DateTimeOffset.MinValue, response.Content.Headers.Expires);
                }
            },
            async server =>
            {
                IList<HttpHeaderData> headers = new HttpHeaderData[] { new HttpHeaderData("Expires", value) };

                HttpRequestData requestData = await server.HandleRequestAsync(HttpStatusCode.OK, headers);
            });
        }

        [Theory]
        [InlineData("-1", false)]
        [InlineData("Thu, 01 Dec 1994 16:00:00 GMT", true)]
        public void HeadersAdd_CustomExpires_Success(string value, bool isValid)
        {
            var headers = new DerivedHttpHeaders();
            if (!isValid)
            {
                Assert.Throws<FormatException>(() => headers.Add("Expires", value));
            }
            Assert.True(headers.TryAddWithoutValidation("Expires", value));
            Assert.Equal(1, Enumerable.Count(headers.GetValues("Expires")));
            Assert.Equal(value, headers.GetValues("Expires").First());
        }

        [Theory]
        [InlineData("Accept-Encoding", "identity,gzip")]
        public async Task SendAsync_RequestHeaderInResponse_Success(string name, string value)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 };
                    HttpResponseMessage response = await client.SendAsync(message);

                    Assert.Equal(value, response.Headers.GetValues(name).First());
                }
            },
            async server =>
            {
                IList<HttpHeaderData> headers = new HttpHeaderData[] { new HttpHeaderData(name, value) };

                HttpRequestData requestData = await server.HandleRequestAsync(HttpStatusCode.OK, headers);
            });
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_GetWithValidHostHeader_Success(bool withPort)
        {
            var m = new HttpRequestMessage(HttpMethod.Get, Configuration.Http.SecureRemoteEchoServer) { Version = VersionFromUseHttp2 };
            m.Headers.Host = withPort ? Configuration.Http.SecureHost + ":443" : Configuration.Http.SecureHost;

            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.SendAsync(m))
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
                TestHelper.VerifyResponseBody(
                    responseContent,
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_GetWithInvalidHostHeader_ThrowsException()
        {
            if (PlatformDetection.IsNetCore && (!UseSocketsHttpHandler || LoopbackServerFactory.IsHttp2))
            {
                // Only .NET Framework and SocketsHttpHandler with HTTP/1.x use the Host header to influence the SSL auth.
                // Host header is not used for HTTP2
                return;
            }

            var m = new HttpRequestMessage(HttpMethod.Get, Configuration.Http.SecureRemoteEchoServer) { Version = VersionFromUseHttp2 };
            m.Headers.Host = "hostheaderthatdoesnotmatch";

            using (HttpClient client = CreateHttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(m));
            }
        }
    }
}
