// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
   public abstract class HttpClientHandler_ResponseDrain_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_ResponseDrain_Test(ITestOutputHelper output) : base(output) { }

        protected virtual void SetResponseDrainTimeout(HttpClientHandler handler, TimeSpan time) { }

        [OuterLoop]
        [Theory]
        [InlineData(LoopbackServer.ContentMode.ContentLength)]
        [InlineData(LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsync_DisposeBeforeReadingToEnd_DrainsRequestsAndReusesConnection(LoopbackServer.ContentMode mode)
        {
            const string simpleContent = "Hello world!";

            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    using (var client = CreateHttpClient())
                    {
                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, simpleContent.Length, mode);

                        // Read up to exactly 1 byte before the end of the response
                        Stream responseStream = await response1.Content.ReadAsStreamAsync();
                        byte[] bytes = await ReadToByteCount(responseStream, simpleContent.Length - 1);
                        Assert.Equal(simpleContent.Substring(0, simpleContent.Length - 1), Encoding.ASCII.GetString(bytes));

                        // Introduce a short delay to try to ensure that when we dispose the response,
                        // all response data is available and we can drain synchronously and reuse the connection.
                        await Task.Delay(100);

                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on the same connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, simpleContent.Length, mode);
                        Assert.Equal(simpleContent, await response2.Content.ReadAsStringAsync());
                    }
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        server.ListenSocket.Close(); // Shut down the listen socket so attempts at additional connections would fail on the client

                        string response = LoopbackServer.GetContentModeResponse(mode, simpleContent);
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                    });
                });
        }

        // The actual amount of drain that's supported is handler and timing dependent, apparently.
        // These cases are an attempt to provide a "min bar" for draining behavior.

        [OuterLoop]
        [Theory]
        [InlineData(0, 0, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(0, 0, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(1, 0, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(1, 0, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(1, 0, LoopbackServer.ContentMode.BytePerChunk)]
        [InlineData(2, 1, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(2, 1, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(2, 1, LoopbackServer.ContentMode.BytePerChunk)]
        [InlineData(10, 1, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(10, 1, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(10, 1, LoopbackServer.ContentMode.BytePerChunk)]
        [InlineData(100, 10, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(100, 10, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(100, 10, LoopbackServer.ContentMode.BytePerChunk)]
        [InlineData(1000, 950, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(1000, 950, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(1000, 950, LoopbackServer.ContentMode.BytePerChunk)]
        [InlineData(10000, 9500, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(10000, 9500, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(10000, 9500, LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeBeforeReadingToEnd_DrainsRequestsAndReusesConnection(int totalSize, int readSize, LoopbackServer.ContentMode mode)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();
                    SetResponseDrainTimeout(handler, Timeout.InfiniteTimeSpan);

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (var client = new HttpClient(handler))
                    { 
                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, totalSize, mode);

                        // Read part but not all of response
                        Stream responseStream = await response1.Content.ReadAsStreamAsync();
                        await ReadToByteCount(responseStream, readSize);

                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on the same connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, totalSize, mode);
                        Assert.Equal(totalSize, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', totalSize);
                    string response = LoopbackServer.GetContentModeResponse(mode, content);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        // Process the first request, with some introduced delays in the response to
                        // stress the draining.
                        await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                        foreach (char c in response)
                        {
                            await connection.Writer.WriteAsync(c);
                        }

                        // Process the second request.
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                    });
                });
        }

        [OuterLoop]
        [Theory]
        [InlineData(100000, 1, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(100000, 1, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(100000, 1, LoopbackServer.ContentMode.BytePerChunk)]
        [InlineData(800000, 1, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(800000, 1, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(1024 * 1024, 1, LoopbackServer.ContentMode.ContentLength)]
        public async Task GetAsyncLargeRequestWithMaxConnections_DisposeBeforeReadingToEnd_DrainsRequestsAndReusesConnection(int totalSize, int readSize, LoopbackServer.ContentMode mode)
        {
            await GetAsyncWithMaxConnections_DisposeBeforeReadingToEnd_DrainsRequestsAndReusesConnection(totalSize, readSize, mode);
            return;
        }

        // Similar to above, these are semi-extreme cases where the response should never drain for any handler.

        [OuterLoop]
        [Theory]
        [InlineData(2000000, 0, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(2000000, 0, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(2000000, 0, LoopbackServer.ContentMode.BytePerChunk)]
        [InlineData(4000000, 1000000, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(4000000, 1000000, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(4000000, 1000000, LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeBeforeReadingToEnd_KillsConnection(int totalSize, int readSize, LoopbackServer.ContentMode mode)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();
                    SetResponseDrainTimeout(handler, Timeout.InfiniteTimeSpan);

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (var client = new HttpClient(handler))
                    {
                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, totalSize, mode);

                        // Read part but not all of response
                        Stream responseStream = await response1.Content.ReadAsStreamAsync();
                        await ReadToByteCount(responseStream, readSize);

                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on a new connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, totalSize, mode);
                        Assert.Equal(totalSize, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', totalSize);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAsync();
                        try
                        {
                            await connection.Writer.WriteAsync(LoopbackServer.GetContentModeResponse(mode, content, connectionClose: false));
                        }
                        catch (Exception) { }     // Eat errors from client disconnect.

                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(LoopbackServer.GetContentModeResponse(mode, content, connectionClose: true));
                    });
                });
        }

        protected static void ValidateResponseHeaders(HttpResponseMessage response, int contentLength, LoopbackServer.ContentMode mode)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            switch (mode)
            {
                case LoopbackServer.ContentMode.ContentLength:
                    Assert.Equal(contentLength, response.Content.Headers.ContentLength);
                    break;

                case LoopbackServer.ContentMode.SingleChunk:
                case LoopbackServer.ContentMode.BytePerChunk:
                    Assert.True(response.Headers.TransferEncodingChunked);
                    break;
            }
        }

        protected static async Task<byte[]> ReadToByteCount(Stream stream, int byteCount)
        {
            byte[] buffer = new byte[byteCount];
            int totalBytesRead = 0;

            while (totalBytesRead < byteCount)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, (byteCount - totalBytesRead));
                if (bytesRead == 0)
                {
                    throw new Exception("Unexpected EOF");
                }

                totalBytesRead += bytesRead;
                if (totalBytesRead > byteCount)
                {
                    throw new Exception("Read more bytes than requested???");
                }
            }

            return buffer;
        }
    }
}
