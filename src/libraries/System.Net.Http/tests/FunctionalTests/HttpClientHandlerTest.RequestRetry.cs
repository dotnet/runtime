// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_RequestRetry : HttpClientHandlerTestBase
    {
        private const string SimpleContent = "Hello World\r\n";

        public HttpClientHandlerTest_RequestRetry(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task GetAsyncOnNewConnection_RetryOnConnectionClosed_Success()
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Send request. The server will close the first connection after it is successfully established, but SocketsHttpHandler should retry the request.
                    HttpResponseMessage response = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(SimpleContent, await response.Content.ReadAsStringAsync());
                }
            },
            async server =>
            {
                // Accept first connection
                await server.AcceptConnectionAsync(async connection =>
                {
                    // Read request headers, then close connection
                    await connection.ReadRequestHeaderAsync();
                });

                // Client should reconnect.  Accept that connection and send response.
                await server.AcceptConnectionSendResponseAndCloseAsync(content: SimpleContent);
            });
        }

        [Fact]
        public async Task GetAsyncOnExistingConnection_RetryOnConnectionClosed_Success()
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Send initial request and receive response so connection is established
                    HttpResponseMessage response1 = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(SimpleContent, await response1.Content.ReadAsStringAsync());

                    // Send second request.  Should reuse same connection.
                    // The server will close the connection, but HttpClient should retry the request.
                    HttpResponseMessage response2 = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
                    Assert.Equal(SimpleContent, await response2.Content.ReadAsStringAsync());
                }
            },
            async server =>
            {
                // Accept first connection
                await server.AcceptConnectionAsync(async connection =>
                {
                    // Initial response
                    await connection.ReadRequestHeaderAndSendResponseAsync(content: SimpleContent);

                    // Second response: Read request headers, then close connection
                    await connection.ReadRequestHeaderAsync();
                });

                // Client should reconnect.  Accept that connection and send response.
                await server.AcceptConnectionSendResponseAndCloseAsync(content: SimpleContent);
            });
        }

        [Fact]
        public async Task GetAsync_RetryUntilLimitExceeded_ThrowsHttpRequestException()
        {
            const int MaxRetries = 3;

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Send request. The server will keeping closing the connection after it is successfully established.
                    // SocketsHttpHandler should retry the request up to the retry limit, then fail.
                    await Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync(url));
                }
            },
            async server =>
            {
                // Note, total attempts will be MaxRetries + 1 for the original attempt.
                for (int i = 0; i <= MaxRetries; i++)
                {
                    // Establish connection and then close it before sending a response
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAsync();
                    });
                }

                // Client should not attempt any further connections.
            });
        }

        [Fact]
        public async Task PostAsyncExpect100Continue_FailsAfterContentSendStarted_Throws()
        {
            var contentSending = new TaskCompletionSource<bool>();
            var connectionClosed = new TaskCompletionSource<bool>();

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Send initial request and receive response so connection is established
                    HttpResponseMessage response1 = await client.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                    Assert.Equal(SimpleContent, await response1.Content.ReadAsStringAsync());

                    // Send second request on same connection.  When the Expect: 100-continue timeout
                    // expires, the content will start to be serialized and will signal the server to
                    // close the connection; then once the connection is closed, the send will be allowed
                    // to continue and will fail.
                    var request = new HttpRequestMessage(HttpMethod.Post, url) { Version = UseVersion };
                    request.Headers.ExpectContinue = true;
                    request.Content = new SynchronizedSendContent(contentSending, connectionClosed.Task);
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(TestAsync, request));
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
                    await connection.ReadRequestHeaderAndSendResponseAsync(content: SimpleContent);

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

            // The content needs to be large enough to force Expect: 100-Continue behavior in SocketsHttpHandler.
            private readonly string _longContent = new String('a', 1025);

            public SynchronizedSendContent(TaskCompletionSource<bool> sendingContent, Task connectionClosed)
            {
                _connectionClosed = connectionClosed;
                _sendingContent = sendingContent;
            }

#if NETCOREAPP
            protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
                SerializeToStreamAsync(stream, context).GetAwaiter().GetResult();
#endif

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
