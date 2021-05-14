// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http.Functional.Tests;
using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    public class BidirectionStreamingTest : HttpClientHandlerTestBase
    {
        public BidirectionStreamingTest(ITestOutputHelper output) : base(output)
        { }

        // Build number suggested by the WinHttp team.
        // It can be reduced if bidirectional streaming is backported.
        public static bool OsSupportsWinHttpBidirectionalStreaming => Environment.OSVersion.Version >= new Version(10, 0, 22357, 0);

        public static bool TestsEnabled => OsSupportsWinHttpBidirectionalStreaming && PlatformDetection.SupportsAlpn;

        public static bool TestsBackwardsCompatibilityEnabled => !OsSupportsWinHttpBidirectionalStreaming && PlatformDetection.SupportsAlpn;

        protected override Version UseVersion => new Version(2, 0);

        protected static byte[] DataBytes = Encoding.ASCII.GetBytes("data");

        protected static Frame MakeDataFrame(int streamId, byte[] data, bool endStream = false) =>
            new DataFrame(data, (endStream ? FrameFlags.EndStream : FrameFlags.None), 0, streamId);

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task WriteRequestAfterReadResponse()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, server.Address);
                message.Version = new Version(2, 0);
                message.Content = new StreamingContent(async s =>
                {
                    await s.WriteAsync(new byte[50]);

                    await tcs.Task;

                    await s.WriteAsync(new byte[50]);
                }, length: null);

                Task<HttpResponseMessage> sendTask = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync(expectEndOfStream: false);

                Frame frame = await connection.ReadDataFrameAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: false));

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                using Stream responseStream = await response.Content.ReadAsStreamAsync();

                // Read response data.
                byte[] buffer = new byte[1024];
                int readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(DataBytes.Length, readCount);

                // Finish sending request data.
                tcs.SetResult(null);

                frame = await connection.ReadDataFrameAsync();

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: true));

                // Finish reading response data.
                readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(DataBytes.Length, readCount);

                readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(0, readCount);
            }
        }

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task AfterReadResponseServerError_ClientWrite()
        {
            TaskCompletionSource<Stream> requestStreamTcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object> completeStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, server.Address);
                message.Version = new Version(2, 0);
                message.Content = new StreamingContent(async s =>
                {
                    requestStreamTcs.SetResult(s);

                    await completeStreamTcs.Task;
                });

                Task<HttpResponseMessage> sendTask = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                Stream requestStream = await requestStreamTcs.Task;
                await requestStream.WriteAsync(new byte[50]);

                int streamId = await connection.ReadRequestHeaderAsync(expectEndOfStream: false);

                Frame frame = await connection.ReadDataFrameAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: false));

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                Stream responseStream = await response.Content.ReadAsStreamAsync();

                // Read response data.
                byte[] buffer = new byte[1024];
                int readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(DataBytes.Length, readCount);

                // Server sends RST_STREAM.
                await connection.WriteFrameAsync(new RstStreamFrame(FrameFlags.EndStream, 0, streamId));

                await Assert.ThrowsAsync<IOException>(() => requestStream.WriteAsync(new byte[50]).AsTask());
            }
        }

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task AfterReadResponseServerError_ClientRead()
        {
            TaskCompletionSource<Stream> requestStreamTcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object> completeStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, server.Address);
                message.Version = new Version(2, 0);
                message.Content = new StreamingContent(async s =>
                {
                    requestStreamTcs.SetResult(s);

                    await completeStreamTcs.Task;
                });

                Task<HttpResponseMessage> sendTask = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                Stream requestStream = await requestStreamTcs.Task;
                await requestStream.WriteAsync(new byte[50]);

                int streamId = await connection.ReadRequestHeaderAsync(expectEndOfStream: false);

                Frame frame = await connection.ReadDataFrameAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: false));

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                Stream responseStream = await response.Content.ReadAsStreamAsync();

                // Read response data.
                byte[] buffer = new byte[1024];
                int readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(DataBytes.Length, readCount);

                // Server sends RST_STREAM.
                await connection.WriteFrameAsync(new RstStreamFrame(FrameFlags.EndStream, 0, streamId));

                await Assert.ThrowsAsync<IOException>(() => responseStream.ReadAsync(buffer, 0, buffer.Length));
            }
        }

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task AfterReadResponseCompleteClient_ServerGetsEndStream()
        {
            TaskCompletionSource<Stream> requestStreamTcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object> completeStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, server.Address);
                message.Version = new Version(2, 0);
                message.Content = new StreamingContent(async s =>
                {
                    requestStreamTcs.SetResult(s);

                    await completeStreamTcs.Task;
                });

                Task<HttpResponseMessage> sendTask = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                Stream requestStream = await requestStreamTcs.Task;
                await requestStream.WriteAsync(new byte[50]);

                int streamId = await connection.ReadRequestHeaderAsync(expectEndOfStream: false);

                Frame frame = await connection.ReadDataFrameAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: false));

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                Stream responseStream = await response.Content.ReadAsStreamAsync();

                // Read response data.
                byte[] buffer = new byte[1024];
                int readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(DataBytes.Length, readCount);

                // Client sends DATA with END_STREAM
                completeStreamTcs.SetResult(null);

                // Server reads DATA with END_STREAM
                frame = await connection.ReadDataFrameAsync();
                Assert.True(frame.EndStreamFlag);

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: true));

                // Finish reading response data.
                readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(DataBytes.Length, readCount);

                readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(0, readCount);
            }
        }

        [ConditionalFact(nameof(TestsEnabled))]
        public async Task ReadAndWriteAfterServerHasSentEndStream_Success()
        {
            TaskCompletionSource<Stream> requestStreamTcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object> completeStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, server.Address);
                message.Version = new Version(2, 0);
                message.Content = new StreamingContent(async s =>
                {
                    await s.WriteAsync(new byte[50]);

                    requestStreamTcs.SetResult(s);

                    await completeStreamTcs.Task;
                });

                Task serverActions = RunServer();

                HttpResponseMessage response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                await serverActions;

                Stream requestStream = await requestStreamTcs.Task;
                Stream responseStream = await response.Content.ReadAsStreamAsync();

                // Successfully because endstream hasn't been read yet.
                await requestStream.WriteAsync(new byte[50]);

                byte[] buffer = new byte[50];
                int readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(DataBytes.Length, readCount);

                readCount = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(0, readCount);

                async Task RunServer()
                {
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                    int streamId = await connection.ReadRequestHeaderAsync(expectEndOfStream: false);
                    await connection.SendDefaultResponseHeadersAsync(streamId, endStream: false);
                    await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: true));
                };
            }
        }

        [ConditionalFact(nameof(TestsBackwardsCompatibilityEnabled))]
        public async Task BackwardsCompatibility_DowngradeToHttp11()
        {
            TaskCompletionSource<object> completeStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, server.Address);
                message.Version = new Version(2, 0);
                message.Content = new StreamingContent(async s =>
                {
                    await completeStreamTcs.Task;
                });

                Task<HttpResponseMessage> sendTask = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                // If WinHTTP doesn't support streaming a request without a length then it will fallback
                // to HTTP/1.1. This is pretty weird behavior but we keep it for backwards compatibility.
                Exception ex = await Assert.ThrowsAsync<Exception>(async () => await server.EstablishConnectionAsync());
                Assert.Equal("HTTP/1.1 request sent to HTTP/2 connection.", ex.Message);

                completeStreamTcs.SetResult(null);
            }
        }

        private class StreamingContent : HttpContent
        {
            private readonly Func<Stream, Task> _writeFunc;
            private readonly long? _length;

            public StreamingContent(Func<Stream, Task> writeFunc, long? length = null)
            {
                _writeFunc = writeFunc;
                _length = length;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return _writeFunc(stream);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _length.GetValueOrDefault();
                return _length.HasValue;
            }
        }
    }
}
