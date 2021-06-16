// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Net.Quic;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Security;

namespace System.Net.Http
{
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    internal sealed class Http3Connection : HttpConnectionBase, IDisposable
    {
        // TODO: once HTTP/3 is standardized, create APIs for this.
        public static readonly SslApplicationProtocol Http3ApplicationProtocol29 = new SslApplicationProtocol("h3-29");
        public static readonly SslApplicationProtocol Http3ApplicationProtocol30 = new SslApplicationProtocol("h3-30");
        public static readonly SslApplicationProtocol Http3ApplicationProtocol31 = new SslApplicationProtocol("h3-31");

        private readonly HttpConnectionPool _pool;
        private readonly HttpAuthority? _origin;
        private readonly HttpAuthority _authority;
        private readonly byte[] _altUsedEncodedHeader;
        private QuicConnection? _connection;
        private Task? _connectionClosedTask;

        // Keep a collection of requests around so we can process GOAWAY.
        private readonly Dictionary<QuicStream, Http3RequestStream> _activeRequests = new Dictionary<QuicStream, Http3RequestStream>();

        // Set when GOAWAY is being processed, when aborting, or when disposing.
        private long _lastProcessedStreamId = -1;

        // Our control stream.
        private QuicStream? _clientControl;

        // Current SETTINGS from the server.
        private int _maximumHeadersLength = int.MaxValue; // TODO: this is not yet observed by Http3Stream when buffering headers.

        // Once the server's streams are received, these are set to 1. Further receipt of these streams results in a connection error.
        private int _haveServerControlStream;
        private int _haveServerQpackDecodeStream;
        private int _haveServerQpackEncodeStream;

        // A connection-level error will abort any future operations.
        private Exception? _abortException;

        public HttpAuthority Authority => _authority;
        public HttpConnectionPool Pool => _pool;
        public int MaximumRequestHeadersLength => _maximumHeadersLength;
        public byte[] AltUsedEncodedHeaderBytes => _altUsedEncodedHeader;
        public Exception? AbortException => Volatile.Read(ref _abortException);
        private object SyncObj => _activeRequests;

        /// <summary>
        /// If true, we've received GOAWAY, are aborting due to a connection-level error, or are disposing due to pool limits.
        /// </summary>
        private bool ShuttingDown
        {
            get
            {
                Debug.Assert(Monitor.IsEntered(SyncObj));
                return _lastProcessedStreamId != -1;
            }
        }

        public Http3Connection(HttpConnectionPool pool, HttpAuthority? origin, HttpAuthority authority, QuicConnection connection)
        {
            _pool = pool;
            _origin = origin;
            _authority = authority;
            _connection = connection;

            bool altUsedDefaultPort = pool.Kind == HttpConnectionKind.Http && authority.Port == HttpConnectionPool.DefaultHttpPort || pool.Kind == HttpConnectionKind.Https && authority.Port == HttpConnectionPool.DefaultHttpsPort;
            string altUsedValue = altUsedDefaultPort ? authority.IdnHost : authority.IdnHost + ":" + authority.Port.ToString(Globalization.CultureInfo.InvariantCulture);
            _altUsedEncodedHeader = QPack.QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReferenceToArray(KnownHeaders.AltUsed.Name, altUsedValue);

            // Errors are observed via Abort().
            _ = SendSettingsAsync();

            // This process is cleaned up when _connection is disposed, and errors are observed via Abort().
            _ = AcceptStreamsAsync();
        }

        /// <summary>
        /// Starts shutting down the <see cref="Http3Connection"/>. Final cleanup will happen when there are no more active requests.
        /// </summary>
        public void Dispose()
        {
            lock (SyncObj)
            {
                if (_lastProcessedStreamId == -1)
                {
                    _lastProcessedStreamId = long.MaxValue;
                    CheckForShutdown();
                }
            }
        }

        /// <summary>
        /// Called when shutting down, this checks for when shutdown is complete (no more active requests) and does actual disposal.
        /// </summary>
        /// <remarks>Requires <see cref="SyncObj"/> to be locked.</remarks>
        private void CheckForShutdown()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(ShuttingDown);

            if (_activeRequests.Count != 0)
            {
                return;
            }

            if (_clientControl != null)
            {
                _clientControl.Dispose();
                _clientControl = null;
            }

            if (_connection != null)
            {
                // Close the QuicConnection in the background.

                if (_connectionClosedTask == null)
                {
                    _connectionClosedTask = _connection.CloseAsync((long)Http3ErrorCode.NoError).AsTask();
                }

                QuicConnection connection = _connection;
                _connection = null;

                _ = _connectionClosedTask.ContinueWith(closeTask =>
                {
                    if (closeTask.IsFaulted && NetEventSource.Log.IsEnabled())
                    {
                        Trace($"{nameof(QuicConnection)} failed to close: {closeTask.Exception!.InnerException}");
                    }

                    try
                    {
                        connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Trace($"{nameof(QuicConnection)} failed to dispose: {ex}");
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(async);

            // Allocate an active request
            QuicStream? quicStream = null;
            Http3RequestStream? requestStream = null;
            ValueTask waitTask = default;

            try
            {
                while (true)
                {
                    lock (SyncObj)
                    {
                        if (_connection == null)
                        {
                            break;
                        }

                        if (_connection.GetRemoteAvailableBidirectionalStreamCount() > 0)
                        {
                            quicStream = _connection.OpenBidirectionalStream();
                            requestStream = new Http3RequestStream(request, this, quicStream);
                            _activeRequests.Add(quicStream, requestStream);
                            break;
                        }
                        waitTask = _connection.WaitForAvailableBidirectionalStreamsAsync(cancellationToken);
                    }

                    // Wait for an available stream (based on QUIC MAX_STREAMS) if there isn't one available yet.
                    await waitTask.ConfigureAwait(false);
                }

                if (quicStream == null)
                {
                    throw new HttpRequestException(SR.net_http_request_aborted, null, RequestRetryType.RetryOnConnectionFailure);
                }

                requestStream!.StreamId = quicStream.StreamId;

                bool goAway;
                lock (SyncObj)
                {
                    goAway = _lastProcessedStreamId != -1 && requestStream.StreamId > _lastProcessedStreamId;
                }

                if (goAway)
                {
                    throw new HttpRequestException(SR.net_http_request_aborted, null, RequestRetryType.RetryOnConnectionFailure);
                }

                Task<HttpResponseMessage> responseTask = requestStream.SendAsync(cancellationToken);

                // null out requestStream to avoid disposing in finally block. It is now in charge of disposing itself.
                requestStream = null;

                return await responseTask.ConfigureAwait(false);
            }
            catch (QuicConnectionAbortedException ex)
            {
                // This will happen if we aborted _connection somewhere.
                Abort(ex);
                throw new HttpRequestException(SR.Format(SR.net_http_http3_connection_error, ex.ErrorCode), ex, RequestRetryType.RetryOnConnectionFailure);
            }
            finally
            {
                requestStream?.Dispose();
            }
        }

        /// <summary>
        /// Aborts the connection with an error.
        /// </summary>
        /// <remarks>
        /// Used for e.g. I/O or connection-level frame parsing errors.
        /// </remarks>
        internal Exception Abort(Exception abortException)
        {
            // Only observe the first exception we get.
            Exception? firstException = Interlocked.CompareExchange(ref _abortException, abortException, null);

            if (firstException != null)
            {
                if (NetEventSource.Log.IsEnabled() && !ReferenceEquals(firstException, abortException))
                {
                    // Lost the race to set the field to another exception, so just trace this one.
                    Trace($"{nameof(abortException)}=={abortException}");
                }

                return firstException;
            }

            // Stop sending requests to this connection.
            _pool.InvalidateHttp3Connection(this);

            Http3ErrorCode connectionResetErrorCode = (abortException as Http3ProtocolException)?.ErrorCode ?? Http3ErrorCode.InternalError;

            lock (SyncObj)
            {
                // Set _lastProcessedStreamId != -1 to make ShuttingDown = true.
                // It's possible GOAWAY is already being processed, in which case this would already be != -1.
                if (_lastProcessedStreamId == -1)
                {
                    _lastProcessedStreamId = long.MaxValue;
                }

                // Abort the connection. This will cause all of our streams to abort on their next I/O.
                if (_connection != null && _connectionClosedTask == null)
                {
                    _connectionClosedTask = _connection.CloseAsync((long)connectionResetErrorCode).AsTask();
                }

                CheckForShutdown();
            }

            return abortException;
        }

        private void OnServerGoAway(long lastProcessedStreamId)
        {
            // Stop sending requests to this connection.
            _pool.InvalidateHttp3Connection(this);

            var streamsToGoAway = new List<Http3RequestStream>();

            lock (SyncObj)
            {
                if (lastProcessedStreamId > _lastProcessedStreamId)
                {
                    // Server can send multiple GOAWAY frames.
                    // Spec says a server MUST NOT increase the stream ID in subsequent GOAWAYs,
                    // but doesn't specify what client should do if that is violated. Ignore for now.
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace("HTTP/3 server sent GOAWAY with increasing stream ID. Retried requests may have been double-processed by server.");
                    }
                    return;
                }

                _lastProcessedStreamId = lastProcessedStreamId;

                foreach (KeyValuePair<QuicStream, Http3RequestStream> request in _activeRequests)
                {
                    if (request.Value.StreamId > lastProcessedStreamId)
                    {
                        streamsToGoAway.Add(request.Value);
                    }
                }

                CheckForShutdown();
            }

            // GOAWAY each stream outside of the lock, so they can acquire the lock to remove themselves from _activeRequests.
            foreach (Http3RequestStream stream in streamsToGoAway)
            {
                stream.GoAway();
            }
        }

        public void RemoveStream(QuicStream stream)
        {
            lock (SyncObj)
            {
                bool removed = _activeRequests.Remove(stream);
                Debug.Assert(removed == true);

                if (ShuttingDown)
                {
                    CheckForShutdown();
                }
            }
        }

        public override void Trace(string message, [CallerMemberName] string? memberName = null) =>
            Trace(0, message, memberName);

        internal void Trace(long streamId, string message, [CallerMemberName] string? memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                _pool?.GetHashCode() ?? 0,    // pool ID
                GetHashCode(),                // connection ID
                (int)streamId,                // stream ID
                memberName,                   // method name
                message);                     // message

        private async Task SendSettingsAsync()
        {
            try
            {
                _clientControl = _connection!.OpenUnidirectionalStream();
                await _clientControl.WriteAsync(_pool.Settings.Http3SettingsFrame, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Abort(ex);
            }
        }

        public static byte[] BuildSettingsFrame(HttpConnectionSettings settings)
        {
            Span<byte> buffer = stackalloc byte[4 + VariableLengthIntegerHelper.MaximumEncodedLength];

            int integerLength = VariableLengthIntegerHelper.WriteInteger(buffer.Slice(4), settings._maxResponseHeadersLength * 1024L);
            int payloadLength = 1 + integerLength; // includes the setting ID and the integer value.
            Debug.Assert(payloadLength <= VariableLengthIntegerHelper.OneByteLimit);

            buffer[0] = (byte)Http3StreamType.Control;
            buffer[1] = (byte)Http3FrameType.Settings;
            buffer[2] = (byte)payloadLength;
            buffer[3] = (byte)Http3SettingType.MaxHeaderListSize;

            return buffer.Slice(0, 4 + integerLength).ToArray();
        }

        /// <summary>
        /// Accepts unidirectional streams (control, QPack, ...) from the server.
        /// </summary>
        private async Task AcceptStreamsAsync()
        {
            try
            {
                while (true)
                {
                    ValueTask<QuicStream> streamTask;

                    lock (SyncObj)
                    {
                        if (ShuttingDown)
                        {
                            return;
                        }

                        // No cancellation token is needed here; we expect the operation to cancel itself when _connection is disposed.
                        streamTask = _connection!.AcceptStreamAsync(CancellationToken.None);
                    }

                    QuicStream stream = await streamTask.ConfigureAwait(false);

                    // This process is cleaned up when _connection is disposed, and errors are observed via Abort().
                    _ = ProcessServerStreamAsync(stream);
                }
            }
            catch (QuicOperationAbortedException)
            {
                // Shutdown initiated by us, no need to abort.
            }
            catch (Exception ex)
            {
                Abort(ex);
            }
        }

        /// <summary>
        /// Routes a stream to an appropriate stream-type-specific processor
        /// </summary>
        private async Task ProcessServerStreamAsync(QuicStream stream)
        {
            ArrayBuffer buffer = default;

            try
            {
                await using (stream.ConfigureAwait(false))
                {
                    if (stream.CanWrite)
                    {
                        // Server initiated bidirectional streams are either push streams or extensions, and we support neither.
                        throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
                    }

                    buffer = new ArrayBuffer(initialSize: 32, usePool: true);

                    int bytesRead;

                    try
                    {
                        bytesRead = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (QuicStreamAbortedException)
                    {
                        // Treat identical to receiving 0. See below comment.
                        bytesRead = 0;
                    }

                    if (bytesRead == 0)
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#name-unidirectional-streams
                        // A sender can close or reset a unidirectional stream unless otherwise specified. A receiver MUST
                        // tolerate unidirectional streams being closed or reset prior to the reception of the unidirectional
                        // stream header.
                        return;
                    }

                    buffer.Commit(bytesRead);

                    // Stream type is a variable-length integer, but we only check the first byte. There is no known type requiring more than 1 byte.
                    switch (buffer.ActiveSpan[0])
                    {
                        case (byte)Http3StreamType.Control:
                            if (Interlocked.Exchange(ref _haveServerControlStream, 1) != 0)
                            {
                                // A second control stream has been received.
                                throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
                            }

                            // Discard the stream type header.
                            buffer.Discard(1);

                            // Ownership of buffer is transferred to ProcessServerControlStreamAsync.
                            ArrayBuffer bufferCopy = buffer;
                            buffer = default;

                            await ProcessServerControlStreamAsync(stream, bufferCopy).ConfigureAwait(false);
                            return;
                        case (byte)Http3StreamType.QPackDecoder:
                            if (Interlocked.Exchange(ref _haveServerQpackDecodeStream, 1) != 0)
                            {
                                // A second QPack decode stream has been received.
                                throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
                            }

                            // The stream must not be closed, but we aren't using QPACK right now -- ignore.
                            buffer.Dispose();
                            await stream.CopyToAsync(Stream.Null).ConfigureAwait(false);
                            return;
                        case (byte)Http3StreamType.QPackEncoder:
                            if (Interlocked.Exchange(ref _haveServerQpackEncodeStream, 1) != 0)
                            {
                                // A second QPack encode stream has been received.
                                throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
                            }

                            // We haven't enabled QPack in our SETTINGS frame, so we shouldn't receive any meaningful data here.
                            // However, the standard says the stream must not be closed for the lifetime of the connection. Just ignore any data.
                            buffer.Dispose();
                            await stream.CopyToAsync(Stream.Null).ConfigureAwait(false);
                            return;
                        case (byte)Http3StreamType.Push:
                            // We don't support push streams.
                            // Because no maximum push stream ID was negotiated via a MAX_PUSH_ID frame, server should not have sent this. Abort the connection with H3_ID_ERROR.
                            throw new Http3ConnectionException(Http3ErrorCode.IdError);
                        default:
                            // Unknown stream type. Per spec, these must be ignored and aborted but not be considered a connection-level error.

                            if (NetEventSource.Log.IsEnabled())
                            {
                                // Read the rest of the integer, which might be more than 1 byte, so we can log it.

                                long unknownStreamType;
                                while (!VariableLengthIntegerHelper.TryRead(buffer.ActiveSpan, out unknownStreamType, out _))
                                {
                                    buffer.EnsureAvailableSpace(VariableLengthIntegerHelper.MaximumEncodedLength);
                                    bytesRead = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(false);

                                    if (bytesRead == 0)
                                    {
                                        unknownStreamType = -1;
                                        break;
                                    }

                                    buffer.Commit(bytesRead);
                                }

                                NetEventSource.Info(this, $"Ignoring server-initiated stream of unknown type {unknownStreamType}.");
                            }

                            stream.AbortWrite((long)Http3ErrorCode.StreamCreationError);
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Abort(ex);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        /// <summary>
        /// Reads the server's control stream.
        /// </summary>
        private async Task ProcessServerControlStreamAsync(QuicStream stream, ArrayBuffer buffer)
        {
            using (buffer)
            {
                // Read the first frame of the control stream. Per spec:
                // A SETTINGS frame MUST be sent as the first frame of each control stream.

                (Http3FrameType? frameType, long payloadLength) = await ReadFrameEnvelopeAsync().ConfigureAwait(false);

                if (frameType == null)
                {
                    // Connection closed prematurely, expected SETTINGS frame.
                    throw new Http3ConnectionException(Http3ErrorCode.ClosedCriticalStream);
                }

                if (frameType != Http3FrameType.Settings)
                {
                    throw new Http3ConnectionException(Http3ErrorCode.MissingSettings);
                }

                await ProcessSettingsFrameAsync(payloadLength).ConfigureAwait(false);

                // Read subsequent frames.

                while (true)
                {
                    (frameType, payloadLength) = await ReadFrameEnvelopeAsync().ConfigureAwait(false);

                    switch (frameType)
                    {
                        case Http3FrameType.GoAway:
                            await ProcessGoAwayFameAsync(payloadLength).ConfigureAwait(false);
                            break;
                        case Http3FrameType.Settings:
                            // If an endpoint receives a second SETTINGS frame on the control stream, the endpoint MUST respond with a connection error of type H3_FRAME_UNEXPECTED.
                            throw new Http3ConnectionException(Http3ErrorCode.UnexpectedFrame);
                        case Http3FrameType.Headers: // Servers should not send these frames to a control stream.
                        case Http3FrameType.Data:
                        case Http3FrameType.MaxPushId:
                        case Http3FrameType.ReservedHttp2Priority: // These frames are explicitly reserved and must never be sent.
                        case Http3FrameType.ReservedHttp2Ping:
                        case Http3FrameType.ReservedHttp2WindowUpdate:
                        case Http3FrameType.ReservedHttp2Continuation:
                            throw new Http3ConnectionException(Http3ErrorCode.UnexpectedFrame);
                        case Http3FrameType.PushPromise:
                        case Http3FrameType.CancelPush:
                            // Because we haven't sent any MAX_PUSH_ID frame, it is invalid to receive any push-related frames as they will all reference a too-large ID.
                            throw new Http3ConnectionException(Http3ErrorCode.IdError);
                        case null:
                            // End of stream reached. If we're shutting down, stop looping. Otherwise, this is an error (this stream should not be closed for life of connection).
                            bool shuttingDown;
                            lock (SyncObj)
                            {
                                shuttingDown = ShuttingDown;
                            }
                            if (!shuttingDown)
                            {
                                throw new Http3ConnectionException(Http3ErrorCode.ClosedCriticalStream);
                            }
                            return;
                        default:
                            await SkipUnknownPayloadAsync(frameType.GetValueOrDefault(), payloadLength).ConfigureAwait(false);
                            break;
                    }
                }
            }

            async ValueTask<(Http3FrameType? frameType, long payloadLength)> ReadFrameEnvelopeAsync()
            {
                long frameType, payloadLength;
                int bytesRead;

                while (!Http3Frame.TryReadIntegerPair(buffer.ActiveSpan, out frameType, out payloadLength, out bytesRead))
                {
                    buffer.EnsureAvailableSpace(VariableLengthIntegerHelper.MaximumEncodedLength * 2);
                    bytesRead = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(false);

                    if (bytesRead != 0)
                    {
                        buffer.Commit(bytesRead);
                    }
                    else if (buffer.ActiveLength == 0)
                    {
                        // End of stream.
                        return (null, 0);
                    }
                    else
                    {
                        // Our buffer has partial frame data in it but not enough to complete the read: bail out.
                        throw new Http3ConnectionException(Http3ErrorCode.FrameError);
                    }
                }

                buffer.Discard(bytesRead);

                return ((Http3FrameType)frameType, payloadLength);
            }

            async ValueTask ProcessSettingsFrameAsync(long settingsPayloadLength)
            {
                while (settingsPayloadLength != 0)
                {
                    long settingId, settingValue;
                    int bytesRead;

                    while (!Http3Frame.TryReadIntegerPair(buffer.ActiveSpan, out settingId, out settingValue, out bytesRead))
                    {
                        buffer.EnsureAvailableSpace(VariableLengthIntegerHelper.MaximumEncodedLength * 2);
                        bytesRead = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(false);

                        if (bytesRead != 0)
                        {
                            buffer.Commit(bytesRead);
                        }
                        else
                        {
                            // Our buffer has partial frame data in it but not enough to complete the read: bail out.
                            throw new Http3ConnectionException(Http3ErrorCode.FrameError);
                        }
                    }

                    settingsPayloadLength -= bytesRead;

                    if (settingsPayloadLength < 0)
                    {
                        // An integer was encoded past the payload length.
                        // A frame payload that contains additional bytes after the identified fields or a frame payload that terminates before the end of the identified fields MUST be treated as a connection error of type H3_FRAME_ERROR.
                        throw new Http3ConnectionException(Http3ErrorCode.FrameError);
                    }

                    buffer.Discard(bytesRead);

                    switch ((Http3SettingType)settingId)
                    {
                        case Http3SettingType.MaxHeaderListSize:
                            _maximumHeadersLength = (int)Math.Min(settingValue, int.MaxValue);
                            break;
                        case Http3SettingType.ReservedHttp2EnablePush:
                        case Http3SettingType.ReservedHttp2MaxConcurrentStreams:
                        case Http3SettingType.ReservedHttp2InitialWindowSize:
                        case Http3SettingType.ReservedHttp2MaxFrameSize:
                            // Per https://tools.ietf.org/html/draft-ietf-quic-http-31#section-7.2.4.1
                            // these settings IDs are reserved and must never be sent.
                            throw new Http3ConnectionException(Http3ErrorCode.SettingsError);
                    }
                }
            }

            async ValueTask ProcessGoAwayFameAsync(long goawayPayloadLength)
            {
                long lastStreamId;
                int bytesRead;

                while (!VariableLengthIntegerHelper.TryRead(buffer.AvailableSpan, out lastStreamId, out bytesRead))
                {
                    buffer.EnsureAvailableSpace(VariableLengthIntegerHelper.MaximumEncodedLength);
                    bytesRead = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(false);

                    if (bytesRead != 0)
                    {
                        buffer.Commit(bytesRead);
                    }
                    else
                    {
                        // Our buffer has partial frame data in it but not enough to complete the read: bail out.
                        throw new Http3ConnectionException(Http3ErrorCode.FrameError);
                    }
                }

                buffer.Discard(bytesRead);
                if (bytesRead != goawayPayloadLength)
                {
                    // Frame contains unknown extra data after the integer.
                    throw new Http3ConnectionException(Http3ErrorCode.FrameError);
                }

                OnServerGoAway(lastStreamId);
            }

            async ValueTask SkipUnknownPayloadAsync(Http3FrameType frameType, long payloadLength)
            {
                while (payloadLength != 0)
                {
                    if (buffer.ActiveLength == 0)
                    {
                        int bytesRead = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(false);

                        if (bytesRead != 0)
                        {
                            buffer.Commit(bytesRead);
                        }
                        else
                        {
                            // Our buffer has partial frame data in it but not enough to complete the read: bail out.
                            throw new Http3ConnectionException(Http3ErrorCode.FrameError);
                        }
                    }

                    long readLength = Math.Min(payloadLength, buffer.ActiveLength);
                    buffer.Discard((int)readLength);
                    payloadLength -= readLength;
                }
            }
        }
    }
}
