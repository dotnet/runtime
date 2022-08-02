// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Functional.Tests;
using Xunit;
using System.Threading;

namespace System.Net.Test.Common
{
    internal sealed class Http3LoopbackConnection : GenericLoopbackConnection
    {
        public const long H3_NO_ERROR = 0x100;
        public const long H3_GENERAL_PROTOCOL_ERROR = 0x101;
        public const long H3_INTERNAL_ERROR = 0x102;
        public const long H3_STREAM_CREATION_ERROR = 0x103;
        public const long H3_CLOSED_CRITICAL_STREAM = 0x104;
        public const long H3_FRAME_UNEXPECTED = 0x105;
        public const long H3_FRAME_ERROR = 0x106;
        public const long H3_EXCESSIVE_LOAD = 0x107;
        public const long H3_ID_ERROR = 0x108;
        public const long H3_SETTINGS_ERROR = 0x109;
        public const long H3_MISSING_SETTINGS = 0x10a;
        public const long H3_REQUEST_REJECTED = 0x10b;
        public const long H3_REQUEST_CANCELLED = 0x10c;
        public const long H3_REQUEST_INCOMPLETE = 0x10d;
        public const long H3_CONNECT_ERROR = 0x10f;
        public const long H3_VERSION_FALLBACK = 0x110;

        private readonly QuicConnection _connection;

        // Queue for holding streams we accepted before we managed to accept the control stream
        private readonly Queue<QuicStream> _delayedStreams = new Queue<QuicStream>();

        // This is specifically request streams, not control streams
        private readonly Dictionary<int, Http3LoopbackStream> _openStreams = new Dictionary<int, Http3LoopbackStream>();

        private Http3LoopbackStream _currentStream;
        // We can't retrieve the stream ID after the stream is disposed, so store it separately
        // Initialize it to -4 so that the firstInvalidStreamId calculation will work even if we never process a request
        private long _currentStreamId = -4;

        private Http3LoopbackStream _inboundControlStream;      // Inbound control stream from client
        private Http3LoopbackStream _outboundControlStream;     // Our outbound control stream

        public Http3LoopbackConnection(QuicConnection connection)
        {
            _connection = connection;
        }

        public long MaxHeaderListSize { get; private set; } = -1;

        public override async ValueTask DisposeAsync()
        {
            // Close any remaining request streams (but NOT control streams, as these should not be closed while the connection is open)
            foreach (Http3LoopbackStream stream in _openStreams.Values)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            foreach (QuicStream stream in _delayedStreams)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            // Dispose the connection
            // If we already waited for graceful shutdown from the client, then the connection is already closed and this will simply release the handle.
            // If not, then this will silently abort the connection.
            await _connection.DisposeAsync();

            // Dispose control streams so that we release their handles too.
            if (_inboundControlStream is not null)
            {
                await _inboundControlStream.DisposeAsync().ConfigureAwait(false);
            }
            if (_outboundControlStream is not null)
            {
                await _outboundControlStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task CloseAsync(long errorCode) => _connection.CloseAsync(errorCode).AsTask();

        public async ValueTask<Http3LoopbackStream> OpenUnidirectionalStreamAsync()
        {
            return new Http3LoopbackStream(await _connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional));
        }

        public async ValueTask<Http3LoopbackStream> OpenBidirectionalStreamAsync()
        {
            return new Http3LoopbackStream(await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional));
        }

        public static int GetRequestId(QuicStream stream)
        {
            Debug.Assert(stream.CanRead && stream.CanWrite, "Stream must be a request stream.");

            // TODO: QUIC streams can have IDs larger than int.MaxValue; update all our tests to use long rather than int.
            return checked((int)stream.Id + 1);
        }

        public Http3LoopbackStream GetOpenRequest(int requestId = 0)
        {
            return requestId == 0 ? _currentStream : _openStreams[requestId - 1];
        }

        public override Task InitializeConnectionAsync()
        {
            throw new NotImplementedException();
        }

        private Task EnsureControlStreamAcceptedAsync()
        {
            if (_inboundControlStream != null)
            {
                return Task.CompletedTask;
            }

            return EnsureControlStreamAcceptedInternalAsync();
            async Task EnsureControlStreamAcceptedInternalAsync()
            {
                Http3LoopbackStream controlStream;

                while (true)
                {
                    QuicStream quicStream = await _connection.AcceptInboundStreamAsync().ConfigureAwait(false);

                    if (!quicStream.CanWrite)
                    {
                        // control stream accepted
                        controlStream = new Http3LoopbackStream(quicStream);
                        break;
                    }

                    // control streams are unidirectional, so this must be a request stream
                    // keep it for later and wait for another stream
                    _delayedStreams.Enqueue(quicStream);
                }

                long? streamType = await controlStream.ReadIntegerAsync();
                Assert.Equal(Http3LoopbackStream.ControlStream, streamType);

                List<(long settingId, long settingValue)> settings = await controlStream.ReadSettingsAsync();
                (long settingId, long settingValue) = Assert.Single(settings);

                Assert.Equal(Http3LoopbackStream.MaxHeaderListSize, settingId);
                MaxHeaderListSize = settingValue;

                _inboundControlStream = controlStream;
            }
        }

        // This will automatically handle the control stream, including validating its contents
        public async Task<Http3LoopbackStream> AcceptRequestStreamAsync()
        {
            await EnsureControlStreamAcceptedAsync().ConfigureAwait(false);

            if (!_delayedStreams.TryDequeue(out QuicStream quicStream))
            {
                quicStream = await _connection.AcceptInboundStreamAsync().ConfigureAwait(false);
            }

            var stream = new Http3LoopbackStream(quicStream);

            Assert.True(quicStream.CanWrite, "Expected writeable stream.");

            _openStreams.Add(checked((int)quicStream.Id), stream);
            _currentStream = stream;
            _currentStreamId = quicStream.Id;

            return stream;
        }

        public async Task<(Http3LoopbackStream clientControlStream, Http3LoopbackStream requestStream)> AcceptControlAndRequestStreamAsync()
        {
            Http3LoopbackStream requestStream = await AcceptRequestStreamAsync();
            Http3LoopbackStream controlStream = _inboundControlStream;

            return (controlStream, requestStream);
        }

        public async Task EstablishControlStreamAsync()
        {
            _outboundControlStream = await OpenUnidirectionalStreamAsync();
            await _outboundControlStream.SendUnidirectionalStreamTypeAsync(Http3LoopbackStream.ControlStream);
            await _outboundControlStream.SendSettingsFrameAsync();
        }

        public override async Task<byte[]> ReadRequestBodyAsync()
        {
            return await _currentStream.ReadRequestBodyAsync().ConfigureAwait(false);
        }

        public override async Task<HttpRequestData> ReadRequestDataAsync(bool readBody = true)
        {
            Http3LoopbackStream stream = await AcceptRequestStreamAsync().ConfigureAwait(false);
            return await stream.ReadRequestDataAsync(readBody).ConfigureAwait(false);
        }

        public override Task SendResponseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "", bool isFinal = true)
        {
            return GetOpenRequest().SendResponseAsync(statusCode, headers, content, isFinal);
        }

        public override Task SendResponseBodyAsync(byte[] content, bool isFinal = true)
        {
            return GetOpenRequest().SendResponseBodyAsync(content, isFinal);
        }

        public override Task SendResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null)
        {
            return GetOpenRequest().SendResponseHeadersAsync(statusCode, headers);
        }

        public override Task SendPartialResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null)
        {
            return GetOpenRequest().SendPartialResponseHeadersAsync(statusCode, headers);
        }

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            Http3LoopbackStream stream = await AcceptRequestStreamAsync().ConfigureAwait(false);

            HttpRequestData request = await stream.ReadRequestDataAsync().ConfigureAwait(false);

            // We are about to close the connection, after we send the response.
            // So, send a GOAWAY frame now so the client won't inadvertantly try to reuse the connection.
            // Note that in HTTP3 (unlike HTTP2) there is no strict ordering between the GOAWAY and the response below;
            // so the client may race in processing them and we need to handle this.
            await _outboundControlStream.SendGoAwayFrameAsync(stream.StreamId + 4);

            await stream.SendResponseAsync(statusCode, headers, content).ConfigureAwait(false);

            await WaitForClientDisconnectAsync();

            return request;
        }

        public async Task ShutdownAsync(bool failCurrentRequest = false)
        {
            try
            {
                long firstInvalidStreamId = failCurrentRequest ? _currentStreamId : _currentStreamId + 4;
                await _outboundControlStream.SendGoAwayFrameAsync(firstInvalidStreamId);
            }
            catch (QuicException abortException) when (abortException.QuicError == QuicError.ConnectionAborted && abortException.ApplicationErrorCode == H3_NO_ERROR)
            {
                // Client must have closed the connection already because the HttpClientHandler instance was disposed.
                // So nothing to do.
                return;
            }
            catch (OperationCanceledException)
            {
                // If the client is closing the connection at the same time we are trying to send the GOAWAY above,
                // this can result in OperationCanceledException from QuicStream.WriteAsync.
                // See https://github.com/dotnet/runtime/issues/58078
                // I saw this consistently with GetAsync_EmptyResponseHeader_Success.
                // To work around this, just eat the exception for now.
                // Also, be aware of this issue as it will do weird things with OperationCanceledException and can
                // make debugging this very confusing: https://github.com/dotnet/runtime/issues/58081
                return;
            }

            await WaitForClientDisconnectAsync();
        }

        // Wait for the client to close the connection, e.g. after we send a GOAWAY, or after the HttpClient is disposed.
        public async Task WaitForClientDisconnectAsync(bool refuseNewRequests = true)
        {
            while (true)
            {
                Http3LoopbackStream stream;

                try
                {
                    stream = await AcceptRequestStreamAsync().ConfigureAwait(false);

                    if (!refuseNewRequests)
                    {
                        throw new Exception("Unexpected request stream received while waiting for client disconnect");
                    }
                }
                catch (QuicException abortException) when (abortException.QuicError == QuicError.ConnectionAborted && abortException.ApplicationErrorCode == H3_NO_ERROR)
                {
                    break;
                }

                await using (stream)
                {
                    stream.Abort(H3_REQUEST_REJECTED);
                }
            }

            // The client's control stream should throw QuicConnectionAbortedException, indicating that it was
            // aborted because the connection was closed (and was not explicitly closed or aborted prior to the connection being closed)
            QuicException ex = await Assert.ThrowsAsync<QuicException>(async () => await _inboundControlStream.ReadFrameAsync());
            Assert.Equal(QuicError.ConnectionAborted, ex.QuicError);

            await CloseAsync(H3_NO_ERROR);
        }

        public override async Task WaitForCancellationAsync(bool ignoreIncomingData = true)
        {
            await GetOpenRequest().WaitForCancellationAsync(ignoreIncomingData).ConfigureAwait(false);
        }

        public override Task WaitForCloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

}
