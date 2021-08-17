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

        // This is specifically request streams, not control streams
        private readonly Dictionary<int, Http3LoopbackStream> _openStreams = new Dictionary<int, Http3LoopbackStream>();
        private Http3LoopbackStream _currentStream;

        private Http3LoopbackStream _inboundControlStream;      // Inbound control stream from client
        private Http3LoopbackStream _outboundControlStream;     // Our outbound control stream

        public Http3LoopbackConnection(QuicConnection connection)
        {
            _connection = connection;
        }

        public override void Dispose()
        {
            // Close any remaining request streams (but NOT control streams, as these should not be closed while the connection is open)
            foreach (Http3LoopbackStream stream in _openStreams.Values)
            {
                stream.Dispose();
            }

// We don't dispose the connection currently, because this causes races when the server connection is closed before
// the client has received and handled all response data.
// See discussion in https://github.com/dotnet/runtime/pull/57223#discussion_r687447832
#if false
            // Dispose the connection
            // If we already waited for graceful shutdown from the client, then the connection is already closed and this will simply release the handle.
            // If not, then this will silently abort the connection.
            _connection.Dispose();

            // Dispose control streams so that we release their handles too.
            _inboundControlStream?.Dispose();
            _outboundControlStream?.Dispose();
#endif
        }

        public async Task CloseAsync(long errorCode)
        {
            await _connection.CloseAsync(errorCode).ConfigureAwait(false);
        }

        public Http3LoopbackStream OpenUnidirectionalStream()
        {
            return new Http3LoopbackStream(_connection.OpenUnidirectionalStream());
        }

        public Http3LoopbackStream OpenBidirectionalStream()
        {
            return new Http3LoopbackStream(_connection.OpenBidirectionalStream());
        }

        public static int GetRequestId(QuicStream stream)
        {
            Debug.Assert(stream.CanRead && stream.CanWrite, "Stream must be a request stream.");

            // TODO: QUIC streams can have IDs larger than int.MaxValue; update all our tests to use long rather than int.
            return checked((int)stream.StreamId + 1);
        }

        public Http3LoopbackStream GetOpenRequest(int requestId = 0)
        {
            return requestId == 0 ? _currentStream : _openStreams[requestId - 1];
        }

        public override Task InitializeConnectionAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<Http3LoopbackStream> AcceptStreamAsync()
        {
            QuicStream quicStream = await _connection.AcceptStreamAsync().ConfigureAwait(false);
            var stream = new Http3LoopbackStream(quicStream);

            if (quicStream.CanWrite)
            {
                _openStreams.Add(checked((int)quicStream.StreamId), stream);
                _currentStream = stream;
            }

            return stream;
        }

        private async Task HandleControlStreamAsync(Http3LoopbackStream controlStream)
        {
            if (_inboundControlStream is not null)
            {
                throw new Exception("Received second control stream from client???");
            }

            long? streamType = await controlStream.ReadIntegerAsync();
            Assert.Equal(Http3LoopbackStream.ControlStream, streamType);

            List<(long settingId, long settingValue)> settings = await controlStream.ReadSettingsAsync();
            (long settingId, long settingValue) = Assert.Single(settings);

            Assert.Equal(Http3LoopbackStream.MaxHeaderListSize, settingId);

            _inboundControlStream = controlStream;
        }

        // This will automatically handle the control stream, including validating its contents
        public async Task<Http3LoopbackStream> AcceptRequestStreamAsync()
        {
            Http3LoopbackStream stream;

            while (true)
            {
                stream = await AcceptStreamAsync().ConfigureAwait(false);

                if (stream.CanWrite)
                {
                    return stream;
                }

                // Must be the control stream
                await HandleControlStreamAsync(stream);
            }
        }

        public async Task<(Http3LoopbackStream clientControlStream, Http3LoopbackStream requestStream)> AcceptControlAndRequestStreamAsync()
        {
            Http3LoopbackStream streamA = null, streamB = null;

            try
            {
                streamA = await AcceptStreamAsync();
                streamB = await AcceptStreamAsync();

                return (streamA.CanWrite, streamB.CanWrite) switch
                {
                    (false, true) => (streamA, streamB),
                    (true, false) => (streamB, streamA),
                    _ => throw new Exception("Expected one unidirectional and one bidirectional stream; received something else.")
                };
            }
            catch
            {
                streamA?.Dispose();
                streamB?.Dispose();
                throw;
            }
        }

        public async Task EstablishControlStreamAsync()
        {
            _outboundControlStream = OpenUnidirectionalStream();
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

        public override Task SendResponseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "", bool isFinal = true, int requestId = 0)
        {
            return GetOpenRequest(requestId).SendResponseAsync(statusCode, headers, content, isFinal);
        }

        public override Task SendResponseBodyAsync(byte[] content, bool isFinal = true, int requestId = 0)
        {
            return GetOpenRequest(requestId).SendResponseBodyAsync(content, isFinal);
        }

        public override Task SendResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, int requestId = 0)
        {
            return GetOpenRequest(requestId).SendResponseHeadersAsync(statusCode, headers);
        }

        public override Task SendPartialResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, int requestId = 0)
        {
            return GetOpenRequest(requestId).SendPartialResponseHeadersAsync(statusCode, headers);
        }

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            Http3LoopbackStream stream = await AcceptRequestStreamAsync().ConfigureAwait(false);

            HttpRequestData request = await stream.ReadRequestDataAsync().ConfigureAwait(false);

            // We are about to close the connection, after we send the response.
            // So, send a GOAWAY frame now so the client won't inadvertantly try to reuse the connection.
            await _outboundControlStream.SendGoAwayFrameAsync(stream.StreamId + 4);

            await stream.SendResponseAsync(statusCode, headers, content).ConfigureAwait(false);

            await WaitForClientDisconnectAsync();

            return request;
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
                catch (QuicConnectionAbortedException abortException) when (abortException.ErrorCode == H3_NO_ERROR)
                {
                    break;
                }

                using (stream)
                {
                    await stream.AbortAndWaitForShutdownAsync(H3_REQUEST_REJECTED);
                }
            }

            // The client's control stream should throw QuicConnectionAbortedException, indicating that it was
            // aborted because the connection was closed (and was not explicitly closed or aborted prior to the connection being closed)
            await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () => await _inboundControlStream.ReadFrameAsync());

            await CloseAsync(H3_NO_ERROR);
        }

        public override async Task WaitForCancellationAsync(bool ignoreIncomingData = true, int requestId = 0)
        {
            await GetOpenRequest(requestId).WaitForCancellationAsync(ignoreIncomingData).ConfigureAwait(false);
        }
    }

}
