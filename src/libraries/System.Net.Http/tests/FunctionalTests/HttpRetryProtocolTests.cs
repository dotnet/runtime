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
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpRetryProtocolTests : HttpClientHandlerTestBase
    {
        private static readonly string s_simpleContent = "Hello World\r\n";

        // Retry logic is supported by SocketsHttpHandler, CurlHandler, uap, and netfx.  Only WinHttp does not support. 
        private bool IsRetrySupported => !IsWinHttpHandler;

        public HttpRetryProtocolTests(ITestOutputHelper output) : base(output) { }

        [Fact]
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
                    await connection.ReadRequestHeaderAndSendResponseAsync(content: s_simpleContent);

                    // Second response: Read request headers, then close connection
                    await connection.ReadRequestHeaderAsync();
                });

                // Client should reconnect.  Accept that connection and send response.
                await server.AcceptConnectionSendResponseAndCloseAsync(content: s_simpleContent);
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WinRT HTTP stack doesn't support Expect: 100-continue")]
        [Fact]
        public async Task PostAsyncExpect100Continue_FailsAfterContentSendStarted_Throws()
        {
            if (!UseSocketsHttpHandler)
            {
                // WinHttpHandler does not support Expect: 100-continue.
                // And the test is expecting specific behaviors of how SocketsHttpHandler does pooling;
                // it generally works on CurlHandler, but not always.
                return;
            }

            var contentSending = new TaskCompletionSource<bool>();
            var connectionClosed = new TaskCompletionSource<bool>();

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Send initial request and receive response so connection is established
                    HttpResponseMessage response1 = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(s_simpleContent, await response1.Content.ReadAsStringAsync());

                    // Send second request on same connection.  When the Expect: 100-continue timeout
                    // expires, the content will start to be serialized and will signal the server to
                    // close the connection; then once the connection is closed, the send will be allowed
                    // to continue and will fail.
                    var request = new HttpRequestMessage(HttpMethod.Post, url) { Version = VersionFromUseHttp2 };
                    request.Headers.ExpectContinue = true;
                    request.Content = new SynchronizedSendContent(contentSending, connectionClosed.Task);
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                }
            },
            async server =>
            {
                // Accept connection
                await server.AcceptConnectionAsync(async connection =>
                {
                    // Shut down the listen socket so no additional connections can happen
                    server.ListenSocket.Close();

                    // Initial response
                    await connection.ReadRequestHeaderAndSendResponseAsync(content: s_simpleContent);

                    // Second response: Read request headers, then close connection
                    List<string> lines = await connection.ReadRequestHeaderAsync();
                    Assert.Contains("Expect: 100-continue", lines);
                    await contentSending.Task;
                });
                connectionClosed.SetResult(true);
            });
        }

        private sealed class SynchronizedSendContent : HttpContent
        {
            private readonly Task _connectionClosed;
            private readonly TaskCompletionSource<bool> _sendingContent;

            // The content needs to be large enough to force Expect: 100-Continue behavior in libcurl.
            private readonly string _longContent = new String('a', 1025);

            public SynchronizedSendContent(TaskCompletionSource<bool> sendingContent, Task connectionClosed)
            {
                _connectionClosed = connectionClosed;
                _sendingContent = sendingContent;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                _sendingContent.SetResult(true);
                await _connectionClosed;
                await stream.WriteAsync(Encoding.UTF8.GetBytes(_longContent));
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _longContent.Length;
                return true;
            }
        }
    }
}
