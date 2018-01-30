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

        // Retry logic is supported by managed handler, CurlHandler, uap, and netfx.  Only WinHttp does not support. 
        private bool IsRetrySupported =>
            UseManagedHandler || !PlatformDetection.IsWindows || PlatformDetection.IsUap || PlatformDetection.IsFullFramework;

        public static Task CreateServerAndClientAsync(Func<Uri, Task> clientFunc, Func<Socket, Task> serverFunc)
        {
            IPEndPoint ignored;
            return LoopbackServer.CreateServerAsync(async (server, uri) =>
            {
                Task clientTask = clientFunc(uri);
                Task serverTask = serverFunc(server);

                await TestHelper.WhenAllCompletedOrAnyFailed(clientTask, serverTask);

            }, out ignored);
        }

        [Fact]
        public async Task GetAsync_RetryOnConnectionClosed_Success()
        {
            if (!IsRetrySupported)
            {
                return;
            }

            await CreateServerAndClientAsync(async url =>
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
                await LoopbackServer.AcceptSocketAsync(server, async (s, stream, reader, writer) =>
                {
                    // Initial response
                    await LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer, s_simpleResponse);

                    // Second response: Read request headers, then close connection
                    await LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer, "");
                    s.Close();

                    // Client should reconnect.  Accept that connection and send response.
                    await LoopbackServer.ReadRequestAndSendResponseAsync(server, s_simpleResponse);

                    return null;
                });
            });
        }

        [Fact]
        public async Task PostAsyncExpect100Continue_RetryOnConnectionClosed_Success()
        {
            if (!IsRetrySupported)
            {
                return;
            }

            await CreateServerAndClientAsync(async url =>
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
                await LoopbackServer.AcceptSocketAsync(server, async (s, stream, reader, writer) =>
                {
                    // Initial response
                    await LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer, s_simpleResponse);

                    // Second response: Read request headers, then close connection
                    List<string> lines = await LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer, "");
                    Assert.Contains("Expect: 100-continue", lines);
                    s.Close();

                    // Client should reconnect.  Accept that connection and send response.
                    await LoopbackServer.AcceptSocketAsync(server, async (s2, stream2, reader2, writer2) =>
                    {
                        List<string> lines2 = await LoopbackServer.ReadWriteAcceptedAsync(s2, reader2, writer2, "");
                        Assert.Contains("Expect: 100-continue", lines2);

                        await writer2.WriteAsync("HTTP/1.1 100 Continue\r\n\r\n");

                        string contentLine = await reader2.ReadLineAsync();
                        Assert.Equal(s_simpleContent, contentLine + "\r\n");

                        await writer2.WriteAsync(s_simpleResponse);

                        return null;
                    });

                    return null;
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
