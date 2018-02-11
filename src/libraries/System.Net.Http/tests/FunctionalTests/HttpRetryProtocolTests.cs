// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpRetryProtocolTests : HttpClientTestBase
    {
        private static readonly string s_simpleContent = "Hello World\r\n";
        private static readonly string s_simpleResponse =
            $"HTTP/1.1 200 OK\r\n" +
            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
            $"Content-Length: {s_simpleContent.Length}\r\n" +
            "\r\n" +
            s_simpleContent;

        // Retry logic is supported by SocketsHttpHandler, CurlHandler, uap, and netfx.  Only WinHttp does not support. 
        private bool IsRetrySupported => !IsWinHttpHandler;

        [Fact]
        [ActiveIssue(26770, TargetFrameworkMonikers.NetFramework)]
        public async Task GetAsync_RetryOnConnectionClosed_Success()
        {
            if (!IsRetrySupported)
            {
                return;
            }

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Send initial request and receive response so connection is established
                    HttpResponseMessage response1 = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(s_simpleContent, await response1.Content.ReadAsStringAsync());

                    // Send second request.  Should reuse same connection.  
                    // The server will close the connection, but HttpClient should retry the request.
                    HttpResponseMessage response2 = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(s_simpleContent, await response1.Content.ReadAsStringAsync());
                }
            },
            async server =>
            {
                // Accept first connection
                await server.AcceptConnectionAsync(async connection =>
                {
                    // Initial response
                    await connection.ReadRequestHeaderAndSendResponseAsync(s_simpleResponse);

                    // Second response: Read request headers, then close connection
                    await connection.ReadRequestHeaderAsync();
                });

                // Client should reconnect.  Accept that connection and send response.
                await server.AcceptConnectionSendResponseAndCloseAsync(s_simpleResponse);
            });
        }

        [Fact]
        public async Task PostAsyncExpect100Continue_RetryOnConnectionClosed_Success()
        {
            if (!IsRetrySupported)
            {
                return;
            }

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Send initial request and receive response so connection is established
                    HttpResponseMessage response1 = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(s_simpleContent, await response1.Content.ReadAsStringAsync());

                    // Send second request.  Should reuse same connection.  
                    // The server will close the connection, but HttpClient should retry the request.
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.ExpectContinue = true;
                    var content = new CustomContent();
                    request.Content = content;

                    HttpResponseMessage response2 = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(s_simpleContent, await response1.Content.ReadAsStringAsync());

                    Assert.Equal(1, content.SerializeCount);
                }
            },
            async server =>
            {
                // Accept first connection
                await server.AcceptConnectionAsync(async connection =>
                {
                    // Initial response
                    await connection.ReadRequestHeaderAndSendResponseAsync(s_simpleResponse);

                    // Second response: Read request headers, then close connection
                    List<string> lines = await connection.ReadRequestHeaderAsync();
                    Assert.Contains("Expect: 100-continue", lines);
                });

                // Client should reconnect.  Accept that connection and send response.
                await server.AcceptConnectionAsync(async connection =>
                {
                    List<string> lines = await connection.ReadRequestHeaderAsync();
                    Assert.Contains("Expect: 100-continue", lines);

                    await connection.Writer.WriteAsync("HTTP/1.1 100 Continue\r\n\r\n");

                    string contentLine = await connection.Reader.ReadLineAsync();
                    Assert.Equal(s_simpleContent, contentLine + "\r\n");

                    await connection.Writer.WriteAsync(s_simpleResponse);
                });
            });
        }

        class CustomContent : HttpContent
        {
            private int _serializeCount;

            public CustomContent()
            {
                _serializeCount = 0;
            }

            public int SerializeCount => _serializeCount;

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                _serializeCount++;

                await stream.WriteAsync(Encoding.UTF8.GetBytes(s_simpleContent));
            }

            protected override bool TryComputeLength(out long length)
            {
                length = s_simpleContent.Length;
                return true;
            }
        }
    }
}
