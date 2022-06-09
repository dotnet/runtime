// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public sealed class Http1CloseResponseStreamConformanceTests : ResponseConnectedStreamConformanceTests
    {
        protected override string GetResponseHeaders() => "HTTP/1.1 200 OK\r\nConnection: close\r\n\r\n";

        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            StreamPair pair = await base.CreateConnectedStreamsAsync();
            Assert.False(pair.Stream2.CanWrite);
            Assert.True(pair.Stream2.CanRead);
            return pair;
        }
    }

    public sealed class Http1RawResponseStreamConformanceTests : ResponseConnectedStreamConformanceTests
    {
        protected override string GetResponseHeaders() => "HTTP/1.1 101 Switching Protocols\r\n\r\n";

        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            StreamPair pair = await base.CreateConnectedStreamsAsync();
            Assert.True(pair.Stream2.CanWrite);
            Assert.True(pair.Stream2.CanRead);
            return pair;
        }
    }

    public sealed class Http1ContentLengthResponseStreamConformanceTests : ResponseStandaloneStreamConformanceTests
    {
        protected override async Task WriteResponseAsync(Stream responseStream, byte[] bodyData)
        {
            await responseStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {bodyData.Length}\r\n\r\n"));
            await responseStream.WriteAsync(bodyData);
        }
    }

    public sealed class Http1SingleChunkResponseStreamConformanceTests : ResponseStandaloneStreamConformanceTests
    {
        protected override async Task WriteResponseAsync(Stream responseStream, byte[] bodyData)
        {
            await responseStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n"));
            if (bodyData.Length > 0)
            {
                // One chunk for the whole response body
                await responseStream.WriteAsync(Encoding.ASCII.GetBytes($"{bodyData.Length:X}\r\n"));
                await responseStream.WriteAsync(bodyData);
                await responseStream.WriteAsync("\r\n"u8.ToArray());
            }
            await responseStream.WriteAsync("0\r\n\r\n"u8.ToArray());
        }
    }

    public sealed class Http1MultiChunkResponseStreamConformanceTests : ResponseStandaloneStreamConformanceTests
    {
        protected override async Task WriteResponseAsync(Stream responseStream, byte[] bodyData)
        {
            await responseStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n"));
            for (int i = 0; i < bodyData.Length; i++)
            {
                // One chunk per byte of the response body
                await responseStream.WriteAsync(Encoding.ASCII.GetBytes($"1\r\n"));
                await responseStream.WriteAsync(bodyData.AsMemory(i, 1));
                await responseStream.WriteAsync("\r\n"u8.ToArray());
            }
            await responseStream.WriteAsync("0\r\n\r\n"u8.ToArray());
        }
    }

    public abstract class ResponseConnectedStreamConformanceTests : ConnectedStreamConformanceTests
    {
        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool BlocksOnZeroByteReads => true;

        protected abstract string GetResponseHeaders();

        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (Stream httpConnection, Stream server) = ConnectedStreams.CreateBidirectional(4096, int.MaxValue);

            using var hc = new HttpClient(new SocketsHttpHandler() { ConnectCallback = delegate { return ValueTask.FromResult(httpConnection); } });
            Task<HttpResponseMessage> clientTask = hc.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://doesntmatter:12345/"), HttpCompletionOption.ResponseHeadersRead);

            await ReadHeadersAsync(server);

            byte[] responseHeader = Encoding.ASCII.GetBytes(GetResponseHeaders());
            await server.WriteAsync(responseHeader);

            return (server, (await clientTask).Content.ReadAsStream());
        }

        internal static async Task ReadHeadersAsync(Stream server)
        {
            var buffer = new byte[256];
            string text = "";
            while (!text.EndsWith("\r\n\r\n", StringComparison.Ordinal))
            {
                int bytesRead = await server.ReadAsync(buffer);
                Assert.InRange(bytesRead, 1, buffer.Length);
                text += Encoding.ASCII.GetString(buffer.AsSpan(0, bytesRead));
            }
        }

        public override Task Disposed_ThrowsObjectDisposedException() =>
            // The HTTP response streams don't throw ObjectDisposedException upon disposal.
            Task.CompletedTask;

        public override async Task ArgumentValidation_ThrowsExpectedException()
        {
            // Only validate the second stream (the first is the server stream that's part of the test).
            using StreamPair streams = await CreateConnectedStreamsAsync();
            await ValidateMisuseExceptionsAsync(streams.Stream2);
        }
    }

    public abstract class ResponseStandaloneStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => false;

        protected abstract Task WriteResponseAsync(Stream responseStream, byte[] bodyData);

        protected override async Task<Stream> CreateReadOnlyStreamCore(byte[] initialData)
        {
            (Stream httpConnection, Stream server) = ConnectedStreams.CreateBidirectional(4096, int.MaxValue);

            using var hc = new HttpClient(new SocketsHttpHandler() { ConnectCallback = delegate { return ValueTask.FromResult(httpConnection); } });
            Task<Stream> clientTask = hc.GetStreamAsync($"http://doesntmatter:12345/");

            await ResponseConnectedStreamConformanceTests.ReadHeadersAsync(server);

            initialData ??= Array.Empty<byte>();
            await WriteResponseAsync(server, initialData);
            server.Dispose();

            return await clientTask;
        }

        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) => Task.FromResult<Stream>(null);
        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) => Task.FromResult<Stream>(null);

        public override Task Disposed_ThrowsObjectDisposedException() =>
            // The HTTP response streams don't throw ObjectDisposedException upon disposal.
            Task.CompletedTask;
    }
}
