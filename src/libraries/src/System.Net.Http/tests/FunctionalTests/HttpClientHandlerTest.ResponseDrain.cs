// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandler_ResponseDrain_Test : HttpClientTestBase
    {
        public enum ContentMode
        {
            ContentLength,
            SingleChunk,
            BytePerChunk
        }

        private static string GetResponseForContentMode(string content, ContentMode mode)
        {
            switch (mode)
            {
                case ContentMode.ContentLength:
                    return LoopbackServer.GetHttpResponse(content: content);
                case ContentMode.SingleChunk:
                    return LoopbackServer.GetSingleChunkHttpResponse(content: content);
                case ContentMode.BytePerChunk:
                    return LoopbackServer.GetBytePerChunkHttpResponse(content: content);
                default:
                    Assert.True(false);
                    return null;
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(ContentMode.ContentLength)]
        [InlineData(ContentMode.SingleChunk)]
        [InlineData(ContentMode.BytePerChunk)]
        public async Task GetAsync_DisposeBeforeReadingToEnd_DrainsRequestsAndReusesConnection(ContentMode mode)
        {
            if (IsWinHttpHandler && mode == ContentMode.BytePerChunk)
            {
                // WinHttpHandler seems to only do a limited amount of draining when chunking is used;
                // I *think* it's not draining anything beyond the current chunk being processed.
                // So, these cases won't pass.
                return;
            }

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
                        string response = GetResponseForContentMode(simpleContent, mode);
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                    });
                });
        }

        // The actual amount of drain that's supported is handler and timing dependent, apparently.
        // These cases are an attempt to provide a "min bar" for draining behavior.

        [OuterLoop]
        [Theory]
        [InlineData(0, 0, ContentMode.ContentLength)]
        [InlineData(0, 0, ContentMode.SingleChunk)]
        [InlineData(1, 0, ContentMode.ContentLength)]
        [InlineData(1, 0, ContentMode.SingleChunk)]
        [InlineData(1, 0, ContentMode.BytePerChunk)]
        [InlineData(2, 1, ContentMode.ContentLength)]
        [InlineData(2, 1, ContentMode.SingleChunk)]
        [InlineData(2, 1, ContentMode.BytePerChunk)]
        [InlineData(10, 1, ContentMode.ContentLength)]
        [InlineData(10, 1, ContentMode.SingleChunk)]
        [InlineData(10, 1, ContentMode.BytePerChunk)]
        [InlineData(100, 10, ContentMode.ContentLength)]
        [InlineData(100, 10, ContentMode.SingleChunk)]
        [InlineData(100, 10, ContentMode.BytePerChunk)]
        [InlineData(1000, 950, ContentMode.ContentLength)]
        [InlineData(1000, 950, ContentMode.SingleChunk)]
        [InlineData(1000, 950, ContentMode.BytePerChunk)]
        [InlineData(10000, 9500, ContentMode.ContentLength)]
        [InlineData(10000, 9500, ContentMode.SingleChunk)]
        [InlineData(10000, 9500, ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeBeforeReadingToEnd_DrainsRequestsAndReusesConnection(int totalSize, int readSize, ContentMode mode)
        {
            if (IsWinHttpHandler && 
                ((mode == ContentMode.SingleChunk && readSize == 0) ||
                 (mode == ContentMode.BytePerChunk)))
            {
                // WinHttpHandler seems to only do a limited amount of draining when TE is used;
                // I *think* it's not draining anything beyond the current chunk being processed.
                // So, these cases won't pass.
                return;
            }

            if (IsCurlHandler)
            {
                // CurlHandler drain behavior is very inconsistent, so just skip these tests.
                return;
            }

            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();

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
                    string response = GetResponseForContentMode(content, mode);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                    });
                });
        }

        // Similar to above, these are semi-extreme cases where the response should never drain for any handler.

        [OuterLoop]
        [Theory]
        [InlineData(20000, 0, ContentMode.ContentLength)]
        [InlineData(20000, 0, ContentMode.SingleChunk)]
        [InlineData(20000, 0, ContentMode.BytePerChunk)]
        [InlineData(40000, 10000, ContentMode.ContentLength)]
        [InlineData(40000, 10000, ContentMode.SingleChunk)]
        [InlineData(40000, 10000, ContentMode.BytePerChunk)]
        [InlineData(100000, 50000, ContentMode.ContentLength)]
        [InlineData(100000, 50000, ContentMode.SingleChunk)]
        [InlineData(100000, 50000, ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeBeforeReadingToEnd_KillsConnection(int totalSize, int readSize, ContentMode mode)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();

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
                    string response = GetResponseForContentMode(content, mode);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAsync();
                        try
                        {
                            await connection.Writer.WriteAsync(response);
                        }
                        catch (Exception) { }     // Eat errors from client disconnect.
                    });
                    await server.AcceptConnectionSendCustomResponseAndCloseAsync(response);
                });
        }

        private static void ValidateResponseHeaders(HttpResponseMessage response, int contentLength, ContentMode mode)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            switch (mode)
            {
                case ContentMode.ContentLength:
                    Assert.Equal(contentLength, response.Content.Headers.ContentLength);
                    break;

                case ContentMode.SingleChunk:
                case ContentMode.BytePerChunk:
                    Assert.True(response.Headers.TransferEncodingChunked);
                    break;
            }
        }

        private static async Task<byte[]> ReadToByteCount(Stream stream, int byteCount)
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
