// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets.Compression;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    /// <summary>A managed implementation of a web socket that sends and receives data via a <see cref="Stream"/>.</summary>
    /// <remarks>
    /// Thread-safety:
    /// - It's acceptable to call ReceiveAsync and SendAsync in parallel.  One of each may run concurrently.
    /// - It's acceptable to have a pending ReceiveAsync while CloseOutputAsync or CloseAsync is called.
    /// - Attempting to invoke any other operations in parallel may corrupt the instance.  Attempting to invoke
    ///   a send operation while another is in progress or a receive operation while another is in progress will
    ///   result in an exception.
    /// </remarks>
    internal sealed partial class ManagedWebSocket : WebSocket
    {
        /// <summary>Encoding for the payload of text messages: UTF-8 encoding that throws if invalid bytes are discovered, per the RFC.</summary>
        private static readonly UTF8Encoding s_textEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        /// <summary>Valid states to be in when calling SendAsync.</summary>
        private static readonly WebSocketState[] s_validSendStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        /// <summary>Valid states to be in when calling ReceiveAsync.</summary>
        private static readonly WebSocketState[] s_validReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent };
        /// <summary>Valid states to be in when calling CloseOutputAsync.</summary>
        private static readonly WebSocketState[] s_validCloseOutputStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        /// <summary>Valid states to be in when calling CloseAsync.</summary>
        private static readonly WebSocketState[] s_validCloseStates = { WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent };

        /// <summary>The maximum size in bytes of a message frame header that includes mask bytes.</summary>
        internal const int MaxMessageHeaderLength = 14;
        /// <summary>The maximum size of a control message payload.</summary>
        private const int MaxControlPayloadLength = 125;
        /// <summary>Length of the mask XOR'd with the payload data.</summary>
        private const int MaskLength = 4;

        /// <summary>The stream used to communicate with the remote server.</summary>
        private readonly Stream _stream;
        /// <summary>
        /// true if this is the server-side of the connection; false if it's client.
        /// This impacts masking behavior: clients always mask payloads they send and
        /// expect to always receive unmasked payloads, whereas servers always send
        /// unmasked payloads and expect to always receive masked payloads.
        /// </summary>
        private readonly bool _isServer;
        /// <summary>The agreed upon subprotocol with the server.</summary>
        private readonly string? _subprotocol;
        /// <summary>Timer used to send periodic pings to the server, at the interval specified</summary>
        private readonly Timer? _keepAliveTimer;
        /// <summary>Buffer used for reading data from the network.</summary>
        private readonly Memory<byte> _receiveBuffer;
        /// <summary>
        /// Tracks the state of the validity of the UTF-8 encoding of text payloads.  Text may be split across fragments.
        /// </summary>
        private readonly Utf8MessageState _utf8TextState = new Utf8MessageState();
        /// <summary>
        /// Mutex used to ensure that calls to SendFrameAsync don't run concurrently.  We don't support multiple concurrent SendAsync calls,
        /// but this is needed to support SendAsync concurrently with keep-alive pings and CloseAsync.
        /// </summary>
        private readonly AsyncMutex _sendMutex = new AsyncMutex();
        /// <summary>
        /// Mutex used to ensure that calls to ReceiveAsyncPrivate don't run concurrently.  We don't support multiple concurrent ReceiveAsync calls,
        /// but this is needed to support SendAsync concurrently with keep-alive pings and CloseAsync.
        /// </summary>
        private readonly AsyncMutex _receiveMutex = new AsyncMutex();

        // We maintain the current WebSocketState in _state.  However, we separately maintain _sentCloseFrame and _receivedCloseFrame
        // as there isn't a strict ordering between CloseSent and CloseReceived.  If we receive a close frame from the server, we need to
        // transition to CloseReceived even if we're currently in CloseSent, and if we send a close frame, we need to transition to
        // CloseSent even if we're currently in CloseReceived.

        /// <summary>The current state of the web socket in the protocol.</summary>
        private WebSocketState _state = WebSocketState.Open;
        /// <summary>true if Dispose has been called; otherwise, false.</summary>
        private bool _disposed;
        /// <summary>Whether we've ever sent a close frame.</summary>
        private bool _sentCloseFrame;
        /// <summary>Whether we've ever received a close frame.</summary>
        private bool _receivedCloseFrame;
        /// <summary>The reason for the close, as sent by the server, or null if not yet closed.</summary>
        private WebSocketCloseStatus? _closeStatus;
        /// <summary>A description of the close reason as sent by the server, or null if not yet closed.</summary>
        private string? _closeStatusDescription;

        /// <summary>
        /// The last header received in a ReceiveAsync.  If ReceiveAsync got a header but then
        /// returned fewer bytes than was indicated in the header, subsequent ReceiveAsync calls
        /// will use the data from the header to construct the subsequent receive results, and
        /// the payload length in this header will be decremented to indicate the number of bytes
        /// remaining to be received for that header.  As a result, between fragments, the payload
        /// length in this header should be 0.
        /// </summary>
        private MessageHeader _lastReceiveHeader = new MessageHeader { Opcode = MessageOpcode.Text, Fin = true, Processed = true };
        /// <summary>The offset of the next available byte in the _receiveBuffer.</summary>
        private int _receiveBufferOffset;
        /// <summary>The number of bytes available in the _receiveBuffer.</summary>
        private int _receiveBufferCount;
        /// <summary>
        /// When dealing with partially read fragments of binary/text messages, a mask previously received may still
        /// apply, and the first new byte received may not correspond to the 0th position in the mask.  This value is
        /// the next offset into the mask that should be applied.
        /// </summary>
        private int _receivedMaskOffsetOffset;
        /// <summary>
        /// Temporary send buffer.  This should be released back to the ArrayPool once it's
        /// no longer needed for the current send operation.  It is stored as an instance
        /// field to minimize needing to pass it around and to avoid it becoming a field on
        /// various async state machine objects.
        /// </summary>
        private byte[]? _sendBuffer;
        /// <summary>
        /// Whether the last SendAsync had endOfMessage==false. We need to track this so that we
        /// can send the subsequent message with a continuation opcode if the last message was a fragment.
        /// </summary>
        private bool _lastSendWasFragment;
        /// <summary>
        /// Whether the last SendAsync had <seealso cref="WebSocketMessageFlags.DisableCompression" /> flag set.
        /// </summary>
        private bool _lastSendHadDisableCompression;

        /// <summary>Lock used to protect update and check-and-update operations on _state.</summary>
        private object StateUpdateLock => _sendMutex;

        private readonly WebSocketInflater? _inflater;
        private readonly WebSocketDeflater? _deflater;

        /// <summary>Initializes the websocket.</summary>
        /// <param name="stream">The connected Stream.</param>
        /// <param name="isServer">true if this is the server-side of the connection; false if this is the client-side of the connection.</param>
        /// <param name="subprotocol">The agreed upon subprotocol for the connection.</param>
        /// <param name="keepAliveInterval">The interval to use for keep-alive pings.</param>
        internal ManagedWebSocket(Stream stream, bool isServer, string? subprotocol, TimeSpan keepAliveInterval)
        {
            Debug.Assert(StateUpdateLock != null, $"Expected {nameof(StateUpdateLock)} to be non-null");
            Debug.Assert(stream != null, $"Expected non-null {nameof(stream)}");
            Debug.Assert(stream.CanRead, $"Expected readable {nameof(stream)}");
            Debug.Assert(stream.CanWrite, $"Expected writeable {nameof(stream)}");
            Debug.Assert(keepAliveInterval == Timeout.InfiniteTimeSpan || keepAliveInterval >= TimeSpan.Zero, $"Invalid {nameof(keepAliveInterval)}: {keepAliveInterval}");

            _stream = stream;
            _isServer = isServer;
            _subprotocol = subprotocol;

            // Create a buffer just large enough to handle received packet headers (at most 14 bytes) and
            // control payloads (at most 125 bytes).  Message payloads are read directly into the buffer
            // supplied to ReceiveAsync.
            const int ReceiveBufferMinLength = MaxControlPayloadLength;
            _receiveBuffer = new byte[ReceiveBufferMinLength];

            // Now that we're opened, initiate the keep alive timer to send periodic pings.
            // We use a weak reference from the timer to the web socket to avoid a cycle
            // that could keep the web socket rooted in erroneous cases.
            if (keepAliveInterval > TimeSpan.Zero)
            {
                _keepAliveTimer = new Timer(static s =>
                {
                    var wr = (WeakReference<ManagedWebSocket>)s!;
                    if (wr.TryGetTarget(out ManagedWebSocket? thisRef))
                    {
                        thisRef.SendKeepAliveFrameAsync();
                    }
                }, new WeakReference<ManagedWebSocket>(this), keepAliveInterval, keepAliveInterval);
            }
        }

        /// <summary>Initializes the websocket.</summary>
        /// <param name="stream">The connected Stream.</param>
        /// <param name="options">The options with which the websocket must be created.</param>
        internal ManagedWebSocket(Stream stream, WebSocketCreationOptions options)
            : this(stream, options.IsServer, options.SubProtocol, options.KeepAliveInterval)
        {
            var deflateOptions = options.DangerousDeflateOptions;

            if (deflateOptions is not null)
            {
                if (options.IsServer)
                {
                    _inflater = new WebSocketInflater(deflateOptions.ClientMaxWindowBits, deflateOptions.ClientContextTakeover);
                    _deflater = new WebSocketDeflater(deflateOptions.ServerMaxWindowBits, deflateOptions.ServerContextTakeover);
                }
                else
                {
                    _inflater = new WebSocketInflater(deflateOptions.ServerMaxWindowBits, deflateOptions.ServerContextTakeover);
                    _deflater = new WebSocketDeflater(deflateOptions.ClientMaxWindowBits, deflateOptions.ClientContextTakeover);
                }
            }
        }

        public override void Dispose()
        {
            lock (StateUpdateLock)
            {
                DisposeCore();
            }
        }

        private void DisposeCore()
        {
            Debug.Assert(Monitor.IsEntered(StateUpdateLock), $"Expected {nameof(StateUpdateLock)} to be held");
            if (!_disposed)
            {
                _disposed = true;
                _keepAliveTimer?.Dispose();
                _stream.Dispose();

                if (_state < WebSocketState.Aborted)
                {
                    _state = WebSocketState.Closed;
                }

                DisposeSafe(_inflater, _receiveMutex);
                DisposeSafe(_deflater, _sendMutex);
            }
        }

        private static void DisposeSafe(IDisposable? resource, AsyncMutex mutex)
        {
            if (resource is not null)
            {
                Task lockTask = mutex.EnterAsync(CancellationToken.None);

                if (lockTask.IsCompleted)
                {
                    resource.Dispose();
                    mutex.Exit();
                }
                else
                {
                    lockTask.GetAwaiter().UnsafeOnCompleted(() =>
                    {
                        resource.Dispose();
                        mutex.Exit();
                    });
                }
            }
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string? CloseStatusDescription => _closeStatusDescription;

        public override WebSocketState State => _state;

        public override string? SubProtocol => _subprotocol;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (messageType != WebSocketMessageType.Text && messageType != WebSocketMessageType.Binary)
            {
                throw new ArgumentException(SR.Format(
                    SR.net_WebSockets_Argument_InvalidMessageType,
                    nameof(WebSocketMessageType.Close), nameof(SendAsync), nameof(WebSocketMessageType.Binary), nameof(WebSocketMessageType.Text), nameof(CloseOutputAsync)),
                    nameof(messageType));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            return SendAsync(buffer, messageType, endOfMessage ? WebSocketMessageFlags.EndOfMessage : default, cancellationToken).AsTask();
        }

        public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            SendAsync(buffer, messageType, endOfMessage ? WebSocketMessageFlags.EndOfMessage : default, cancellationToken);

        public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, WebSocketMessageFlags messageFlags, CancellationToken cancellationToken)
        {
            if (messageType != WebSocketMessageType.Text && messageType != WebSocketMessageType.Binary)
            {
                throw new ArgumentException(SR.Format(
                    SR.net_WebSockets_Argument_InvalidMessageType,
                    nameof(WebSocketMessageType.Close), nameof(SendAsync), nameof(WebSocketMessageType.Binary), nameof(WebSocketMessageType.Text), nameof(CloseOutputAsync)),
                    nameof(messageType));
            }

            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validSendStates);
            }
            catch (Exception exc)
            {
                return new ValueTask(Task.FromException(exc));
            }

            bool endOfMessage = messageFlags.HasFlag(WebSocketMessageFlags.EndOfMessage);
            bool disableCompression = messageFlags.HasFlag(WebSocketMessageFlags.DisableCompression);
            MessageOpcode opcode;

            if (_lastSendWasFragment)
            {
                if (_lastSendHadDisableCompression != disableCompression)
                {
                    throw new ArgumentException(SR.net_WebSockets_Argument_MessageFlagsHasDifferentCompressionOptions, nameof(messageFlags));
                }
                opcode = MessageOpcode.Continuation;
            }
            else
            {
                opcode = messageType == WebSocketMessageType.Binary ? MessageOpcode.Binary : MessageOpcode.Text;
            }

            ValueTask t = SendFrameAsync(opcode, endOfMessage, disableCompression, buffer, cancellationToken);
            _lastSendWasFragment = !endOfMessage;
            _lastSendHadDisableCompression = disableCompression;

            return t;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);

                return ReceiveAsyncPrivate<WebSocketReceiveResult>(buffer, cancellationToken).AsTask();
            }
            catch (Exception exc)
            {
                return Task.FromException<WebSocketReceiveResult>(exc);
            }
        }

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);

                return ReceiveAsyncPrivate<ValueWebSocketReceiveResult>(buffer, cancellationToken);
            }
            catch (Exception exc)
            {
                return ValueTask.FromException<ValueWebSocketReceiveResult>(exc);
            }
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validCloseStates);
            }
            catch (Exception exc)
            {
                return Task.FromException(exc);
            }

            return CloseAsyncPrivate(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            return CloseOutputAsyncCore(closeStatus, statusDescription, cancellationToken);
        }

        private async Task CloseOutputAsyncCore(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validCloseOutputStates);

            await SendCloseFrameAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);

            // If we already received a close frame, since we've now also sent one, we're now closed.
            lock (StateUpdateLock)
            {
                Debug.Assert(_sentCloseFrame);
                if (_receivedCloseFrame)
                {
                    DisposeCore();
                }
            }
        }

        public override void Abort()
        {
            OnAborted();
            Dispose(); // forcibly tear down connection
        }

        private void OnAborted()
        {
            lock (StateUpdateLock)
            {
                WebSocketState state = _state;
                if (state != WebSocketState.Closed && state != WebSocketState.Aborted)
                {
                    _state = state != WebSocketState.None && state != WebSocketState.Connecting ?
                        WebSocketState.Aborted :
                        WebSocketState.Closed;
                }
            }
        }

        /// <summary>Sends a websocket frame to the network.</summary>
        /// <param name="opcode">The opcode for the message.</param>
        /// <param name="endOfMessage">The value of the FIN bit for the message.</param>
        /// <param name="disableCompression">Disables compression for the message.</param>
        /// <param name="payloadBuffer">The buffer containing the payload data from the message.</param>
        /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
        private ValueTask SendFrameAsync(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlyMemory<byte> payloadBuffer, CancellationToken cancellationToken)
        {
            // If a cancelable cancellation token was provided, that would require registering with it, which means more state we have to
            // pass around (the CancellationTokenRegistration), so if it is cancelable, just immediately go to the fallback path.
            // Similarly, it should be rare that there are multiple outstanding calls to SendFrameAsync, but if there are, again
            // fall back to the fallback path.
            Task lockTask = _sendMutex.EnterAsync(cancellationToken);
            return cancellationToken.CanBeCanceled || !lockTask.IsCompletedSuccessfully ?
                SendFrameFallbackAsync(opcode, endOfMessage, disableCompression, payloadBuffer, lockTask, cancellationToken) :
                SendFrameLockAcquiredNonCancelableAsync(opcode, endOfMessage, disableCompression, payloadBuffer);
        }

        /// <summary>Sends a websocket frame to the network. The caller must hold the sending lock.</summary>
        /// <param name="opcode">The opcode for the message.</param>
        /// <param name="endOfMessage">The value of the FIN bit for the message.</param>
        /// <param name="disableCompression">Disables compression for the message.</param>
        /// <param name="payloadBuffer">The buffer containing the payload data fro the message.</param>
        private ValueTask SendFrameLockAcquiredNonCancelableAsync(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlyMemory<byte> payloadBuffer)
        {
            Debug.Assert(_sendMutex.IsHeld, $"Caller should hold the {nameof(_sendMutex)}");

            // If we get here, the cancellation token is not cancelable so we don't have to worry about it,
            // and we own the semaphore, so we don't need to asynchronously wait for it.
            ValueTask writeTask = default;
            bool releaseSendBufferAndSemaphore = true;
            try
            {
                // Write the payload synchronously to the buffer, then write that buffer out to the network.
                int sendBytes = WriteFrameToSendBuffer(opcode, endOfMessage, disableCompression, payloadBuffer.Span);
                writeTask = _stream.WriteAsync(new ReadOnlyMemory<byte>(_sendBuffer, 0, sendBytes));

                // If the operation happens to complete synchronously (or, more specifically, by
                // the time we get from the previous line to here), release the semaphore, return
                // the task, and we're done.
                if (writeTask.IsCompleted)
                {
                    writeTask.GetAwaiter().GetResult();
                    ValueTask flushTask = new ValueTask(_stream.FlushAsync());
                    if (flushTask.IsCompleted)
                    {
                        return flushTask;
                    }
                    else
                    {
                        releaseSendBufferAndSemaphore = false;
                        return WaitForWriteTaskAsync(flushTask, shouldFlush: false);
                    }
                }

                // Up until this point, if an exception occurred (such as when accessing _stream or when
                // calling GetResult), we want to release the semaphore and the send buffer. After this point,
                // both need to be held until writeTask completes.
                releaseSendBufferAndSemaphore = false;
            }
            catch (Exception exc)
            {
                return new ValueTask(Task.FromException(
                    exc is OperationCanceledException ? exc :
                    _state == WebSocketState.Aborted ? CreateOperationCanceledException(exc) :
                    new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc)));
            }
            finally
            {
                if (releaseSendBufferAndSemaphore)
                {
                    ReleaseSendBuffer();
                    _sendMutex.Exit();
                }
            }

            return WaitForWriteTaskAsync(writeTask, shouldFlush: true);
        }

        private async ValueTask WaitForWriteTaskAsync(ValueTask writeTask, bool shouldFlush)
        {
            try
            {
                await writeTask.ConfigureAwait(false);
                if (shouldFlush)
                {
                    await _stream.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception exc) when (!(exc is OperationCanceledException))
            {
                throw _state == WebSocketState.Aborted ?
                    CreateOperationCanceledException(exc) :
                    new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc);
            }
            finally
            {
                ReleaseSendBuffer();
                _sendMutex.Exit();
            }
        }

        private async ValueTask SendFrameFallbackAsync(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlyMemory<byte> payloadBuffer, Task lockTask, CancellationToken cancellationToken)
        {
            await lockTask.ConfigureAwait(false);
            try
            {
                int sendBytes = WriteFrameToSendBuffer(opcode, endOfMessage, disableCompression, payloadBuffer.Span);
                using (cancellationToken.Register(static s => ((ManagedWebSocket)s!).Abort(), this))
                {
                    await _stream.WriteAsync(new ReadOnlyMemory<byte>(_sendBuffer, 0, sendBytes), cancellationToken).ConfigureAwait(false);
                    await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exc) when (!(exc is OperationCanceledException))
            {
                throw _state == WebSocketState.Aborted ?
                    CreateOperationCanceledException(exc, cancellationToken) :
                    new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc);
            }
            finally
            {
                ReleaseSendBuffer();
                _sendMutex.Exit();
            }
        }

        /// <summary>Writes a frame into the send buffer, which can then be sent over the network.</summary>
        private int WriteFrameToSendBuffer(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlySpan<byte> payloadBuffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(WebSocket));

            if (_deflater is not null && !disableCompression)
            {
                payloadBuffer = _deflater.Deflate(payloadBuffer, endOfMessage);
            }
            int payloadLength = payloadBuffer.Length;

            // Ensure we have a _sendBuffer
            AllocateSendBuffer(payloadLength + MaxMessageHeaderLength);
            Debug.Assert(_sendBuffer != null);

            // Write the message header data to the buffer.
            int headerLength;
            int? maskOffset = null;
            if (_isServer)
            {
                // The server doesn't send a mask, so the mask offset returned by WriteHeader
                // is actually the end of the header.
                headerLength = WriteHeader(opcode, _sendBuffer, payloadBuffer, endOfMessage, useMask: false, compressed: _deflater is not null && !disableCompression);
            }
            else
            {
                // We need to know where the mask starts so that we can use the mask to manipulate the payload data,
                // and we need to know the total length for sending it on the wire.
                maskOffset = WriteHeader(opcode, _sendBuffer, payloadBuffer, endOfMessage, useMask: true, compressed: _deflater is not null && !disableCompression);
                headerLength = maskOffset.GetValueOrDefault() + MaskLength;
            }

            // Write the payload
            if (payloadBuffer.Length > 0)
            {
                payloadBuffer.CopyTo(new Span<byte>(_sendBuffer, headerLength, payloadLength));

                // Release the deflater buffer if any, we're not going to need the payloadBuffer anymore.
                _deflater?.ReleaseBuffer();

                // If we added a mask to the header, XOR the payload with the mask.  We do the manipulation in the send buffer so as to avoid
                // changing the data in the caller-supplied payload buffer.
                if (maskOffset.HasValue)
                {
                    ApplyMask(new Span<byte>(_sendBuffer, headerLength, payloadLength), _sendBuffer, maskOffset.Value, 0);
                }
            }

            // Return the number of bytes in the send buffer
            return headerLength + payloadLength;
        }

        private void SendKeepAliveFrameAsync()
        {
            // This exists purely to keep the connection alive; don't wait for the result, and ignore any failures.
            // The call will handle releasing the lock.  We send a pong rather than ping, since it's allowed by
            // the RFC as a unidirectional heartbeat and we're not interested in waiting for a response.
            ValueTask t = SendFrameAsync(MessageOpcode.Pong, endOfMessage: true, disableCompression: true, ReadOnlyMemory<byte>.Empty, CancellationToken.None);
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                // "Observe" any exception, ignoring it to prevent the unobserved exception event from being raised.
                t.AsTask().ContinueWith(static p => { _ = p.Exception; },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        private static int WriteHeader(MessageOpcode opcode, byte[] sendBuffer, ReadOnlySpan<byte> payload, bool endOfMessage, bool useMask, bool compressed)
        {
            // Client header format:
            // 1 bit - FIN - 1 if this is the final fragment in the message (it could be the only fragment), otherwise 0
            // 1 bit - RSV1 - Reserved - 0
            // 1 bit - RSV2 - Reserved - 0
            // 1 bit - RSV3 - Reserved - 0
            // 4 bits - Opcode - How to interpret the payload
            //     - 0x0 - continuation
            //     - 0x1 - text
            //     - 0x2 - binary
            //     - 0x8 - connection close
            //     - 0x9 - ping
            //     - 0xA - pong
            //     - (0x3 to 0x7, 0xB-0xF - reserved)
            // 1 bit - Masked - 1 if the payload is masked, 0 if it's not.  Must be 1 for the client
            // 7 bits, 7+16 bits, or 7+64 bits - Payload length
            //     - For length 0 through 125, 7 bits storing the length
            //     - For lengths 126 through 2^16, 7 bits storing the value 126, followed by 16 bits storing the length
            //     - For lengths 2^16+1 through 2^64, 7 bits storing the value 127, followed by 64 bytes storing the length
            // 0 or 4 bytes - Mask, if Masked is 1 - random value XOR'd with each 4 bytes of the payload, round-robin
            // Length bytes - Payload data

            Debug.Assert(sendBuffer.Length >= MaxMessageHeaderLength, $"Expected {nameof(sendBuffer)} to be at least {MaxMessageHeaderLength}, got {sendBuffer.Length}");

            sendBuffer[0] = (byte)opcode; // 4 bits for the opcode
            if (endOfMessage)
            {
                sendBuffer[0] |= 0x80; // 1 bit for FIN
            }
            if (compressed && opcode != MessageOpcode.Continuation)
            {
                // Per-Message Deflate flag needs to be set only in the first frame
                sendBuffer[0] |= 0b_0100_0000;
            }

            // Store the payload length.
            int maskOffset;
            if (payload.Length <= 125)
            {
                sendBuffer[1] = (byte)payload.Length;
                maskOffset = 2; // no additional payload length
            }
            else if (payload.Length <= ushort.MaxValue)
            {
                sendBuffer[1] = 126;
                sendBuffer[2] = (byte)(payload.Length / 256);
                sendBuffer[3] = unchecked((byte)payload.Length);
                maskOffset = 2 + sizeof(ushort); // additional 2 bytes for 16-bit length
            }
            else
            {
                sendBuffer[1] = 127;
                int length = payload.Length;
                for (int i = 9; i >= 2; i--)
                {
                    sendBuffer[i] = unchecked((byte)length);
                    length /= 256;
                }
                maskOffset = 2 + sizeof(ulong); // additional 8 bytes for 64-bit length
            }

            if (useMask)
            {
                // Generate the mask.
                sendBuffer[1] |= 0x80;
                WriteRandomMask(sendBuffer, maskOffset);
            }

            // Return the position of the mask.
            return maskOffset;
        }

        /// <summary>Writes a 4-byte random mask to the specified buffer at the specified offset.</summary>
        /// <param name="buffer">The buffer to which to write the mask.</param>
        /// <param name="offset">The offset into the buffer at which to write the mask.</param>
        private static void WriteRandomMask(byte[] buffer, int offset) =>
            RandomNumberGenerator.Fill(buffer.AsSpan(offset, MaskLength));

        /// <summary>
        /// Receive the next text, binary, continuation, or close message, returning information about it and
        /// writing its payload into the supplied buffer.  Other control messages may be consumed and processed
        /// as part of this operation, but data about them will not be returned.
        /// </summary>
        /// <param name="payloadBuffer">The buffer into which payload data should be written.</param>
        /// <param name="cancellationToken">The CancellationToken used to cancel the websocket.</param>
        /// <returns>Information about the received message.</returns>
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private async ValueTask<TResult> ReceiveAsyncPrivate<TResult>(Memory<byte> payloadBuffer, CancellationToken cancellationToken)
        {
            // This is a long method.  While splitting it up into pieces would arguably help with readability, doing so would
            // also result in more allocations, as each async method that yields ends up with multiple allocations.  The impact
            // of those allocations is amortized across all of the awaits in the method, and since we generally expect a receive
            // operation to require at most a single yield (while waiting for data to arrive), it's more efficient to have
            // everything in the one method.  We do separate out pieces for handling close and ping/pong messages, as we expect
            // those to be much less frequent (e.g. we should only get one close per websocket), and thus we can afford to pay
            // a bit more for readability and maintainability.

            CancellationTokenRegistration registration = cancellationToken.Register(static s => ((ManagedWebSocket)s!).Abort(), this);
            try
            {
                await _receiveMutex.EnterAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    ObjectDisposedException.ThrowIf(_disposed, typeof(WebSocket));

                    while (true) // in case we get control frames that should be ignored from the user's perspective
                    {
                        // Get the last received header.  If its payload length is non-zero, that means we previously
                        // received the header but were only able to read a part of the fragment, so we should skip
                        // reading another header and just proceed to use that same header and read more data associated
                        // with it.  If instead its payload length is zero, then we've completed the processing of
                        // that message, and we should read the next header.
                        MessageHeader header = _lastReceiveHeader;
                        if (header.Processed)
                        {
                            if (_receiveBufferCount < (_isServer ? MaxMessageHeaderLength : (MaxMessageHeaderLength - MaskLength)))
                            {
                                // Make sure we have the first two bytes, which includes the start of the payload length.
                                if (_receiveBufferCount < 2)
                                {
                                    if (payloadBuffer.IsEmpty)
                                    {
                                        // The caller has issued a zero-byte read.  The only meaningful reason to do that is to
                                        // wait for data to be available without actually consuming any of it. If we just pass down
                                        // our internal buffer, the underlying stream might end up renting and/or pinning a buffer
                                        // for the duration of the operation, which isn't necessary when we don't actually want to
                                        // consume anything. Instead, we issue a zero-byte read against the underlying stream;
                                        // given that the receive buffer currently stores fewer than the minimum number of bytes
                                        // necessary for a header, it's safe to issue a read (if there were at least the minimum
                                        // number of bytes available, we could end up issuing a read that would erroneously wait
                                        // for data that would never arrive). Once that read completes, we can proceed with any
                                        // other reads necessary, and they'll have a reduced chance of pinning the receive buffer.
                                        await _stream.ReadAsync(Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);
                                    }

                                    await EnsureBufferContainsAsync(2, cancellationToken).ConfigureAwait(false);
                                }

                                // Then make sure we have the full header based on the payload length.
                                // If this is the server, we also need room for the received mask.
                                long payloadLength = _receiveBuffer.Span[_receiveBufferOffset + 1] & 0x7F;
                                if (_isServer || payloadLength > 125)
                                {
                                    int minNeeded =
                                        2 +
                                        (_isServer ? MaskLength : 0) +
                                        (payloadLength <= 125 ? 0 : payloadLength == 126 ? sizeof(ushort) : sizeof(ulong)); // additional 2 or 8 bytes for 16-bit or 64-bit length
                                    await EnsureBufferContainsAsync(minNeeded, cancellationToken).ConfigureAwait(false);
                                }
                            }

                            string? headerErrorMessage = TryParseMessageHeaderFromReceiveBuffer(out header);
                            if (headerErrorMessage != null)
                            {
                                await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, headerErrorMessage).ConfigureAwait(false);
                            }
                            _receivedMaskOffsetOffset = 0;

                            if (header.PayloadLength == 0 && header.Compressed)
                            {
                                // In the rare case where we receive a compressed message with no payload
                                // we need to tell the inflater about it, because the receive code bellow will
                                // not try to do anything when PayloadLength == 0.
                                _inflater!.AddBytes(0, endOfMessage: header.Fin);
                            }
                        }

                        // If the header represents a ping or a pong, it's a control message meant
                        // to be transparent to the user, so handle it and then loop around to read again.
                        // Alternatively, if it's a close message, handle it and exit.
                        if (header.Opcode == MessageOpcode.Ping || header.Opcode == MessageOpcode.Pong)
                        {
                            await HandleReceivedPingPongAsync(header, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                        else if (header.Opcode == MessageOpcode.Close)
                        {
                            await HandleReceivedCloseAsync(header, cancellationToken).ConfigureAwait(false);
                            return GetReceiveResult<TResult>(0, WebSocketMessageType.Close, true);
                        }

                        // If this is a continuation, replace the opcode with the one of the message it's continuing
                        if (header.Opcode == MessageOpcode.Continuation)
                        {
                            header.Opcode = _lastReceiveHeader.Opcode;
                            header.Compressed = _lastReceiveHeader.Compressed;
                        }

                        // The message should now be a binary or text message.  Handle it by reading the payload and returning the contents.
                        Debug.Assert(header.Opcode == MessageOpcode.Binary || header.Opcode == MessageOpcode.Text, $"Unexpected opcode {header.Opcode}");

                        // If there's no data to read, return an appropriate result.
                        if (header.Processed || payloadBuffer.Length == 0)
                        {
                            _lastReceiveHeader = header;
                            return GetReceiveResult<TResult>(
                                count: 0,
                                messageType: header.Opcode == MessageOpcode.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                                endOfMessage: header.EndOfMessage);
                        }

                        // Otherwise, read as much of the payload as we can efficiently, and update the header to reflect how much data
                        // remains for future reads.  We first need to copy any data that may be lingering in the receive buffer
                        // into the destination; then to minimize ReceiveAsync calls, we want to read as much as we can, stopping
                        // only when we've either read the whole message or when we've filled the payload buffer.

                        // First copy any data lingering in the receive buffer.
                        int totalBytesReceived = 0;

                        // Only start a new receive if we haven't received the entire frame.
                        if (header.PayloadLength > 0)
                        {
                            if (header.Compressed)
                            {
                                Debug.Assert(_inflater is not null);
                                _inflater.Prepare(header.PayloadLength, payloadBuffer.Length);
                            }

                            // Read directly into the appropriate buffer until we've hit a limit.
                            int limit = (int)Math.Min(header.Compressed ? _inflater!.Span.Length : payloadBuffer.Length, header.PayloadLength);

                            if (_receiveBufferCount > 0)
                            {
                                int receiveBufferBytesToCopy = Math.Min(limit, _receiveBufferCount);
                                Debug.Assert(receiveBufferBytesToCopy > 0);

                                _receiveBuffer.Span.Slice(_receiveBufferOffset, receiveBufferBytesToCopy).CopyTo(
                                    header.Compressed ? _inflater!.Span : payloadBuffer.Span);
                                ConsumeFromBuffer(receiveBufferBytesToCopy);
                                totalBytesReceived += receiveBufferBytesToCopy;
                            }

                            if (totalBytesReceived < limit)
                            {
                                int bytesToRead = limit - totalBytesReceived;
                                Memory<byte> readBuffer = header.Compressed ?
                                    _inflater!.Memory.Slice(totalBytesReceived, bytesToRead) :
                                    payloadBuffer.Slice(totalBytesReceived, bytesToRead);

                                int numBytesRead = await _stream.ReadAtLeastAsync(
                                    readBuffer, bytesToRead, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
                                if (numBytesRead < bytesToRead)
                                {
                                    ThrowEOFUnexpected();
                                }
                                totalBytesReceived += numBytesRead;
                            }

                            if (_isServer)
                            {
                                _receivedMaskOffsetOffset = ApplyMask(header.Compressed ?
                                    _inflater!.Span.Slice(0, totalBytesReceived) :
                                    payloadBuffer.Span.Slice(0, totalBytesReceived), header.Mask, _receivedMaskOffsetOffset);
                            }

                            header.PayloadLength -= totalBytesReceived;

                            if (header.Compressed)
                            {
                                _inflater!.AddBytes(totalBytesReceived, endOfMessage: header.Fin && header.PayloadLength == 0);
                            }
                        }

                        if (header.Compressed)
                        {
                            // In case of compression totalBytesReceived should actually represent how much we've
                            // inflated, rather than how much we've read from the stream.
                            header.Processed = _inflater!.Inflate(payloadBuffer.Span, out totalBytesReceived) && header.PayloadLength == 0;
                        }
                        else
                        {
                            // Without compression the frame is processed as soon as we've received everything
                            header.Processed = header.PayloadLength == 0;
                        }

                        // If this a text message, validate that it contains valid UTF8.
                        if (header.Opcode == MessageOpcode.Text &&
                            !TryValidateUtf8(payloadBuffer.Span.Slice(0, totalBytesReceived), header.EndOfMessage, _utf8TextState))
                        {
                            await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.InvalidPayloadData, WebSocketError.Faulted).ConfigureAwait(false);
                        }

                        _lastReceiveHeader = header;
                        return GetReceiveResult<TResult>(
                            totalBytesReceived,
                            header.Opcode == MessageOpcode.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                            header.EndOfMessage);
                    }
                }
                finally
                {
                    _receiveMutex.Exit();
                }
            }
            catch (Exception exc) when (exc is not OperationCanceledException)
            {
                if (_state == WebSocketState.Aborted)
                {
                    throw new OperationCanceledException(nameof(WebSocketState.Aborted), exc);
                }
                OnAborted();

                if (exc is WebSocketException)
                {
                    throw;
                }

                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc);
            }
            finally
            {
                registration.Dispose();
            }
        }

        /// <summary>
        /// Returns either <see cref="ValueWebSocketReceiveResult"/> or <see cref="WebSocketReceiveResult"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TResult GetReceiveResult<TResult>(int count, WebSocketMessageType messageType, bool endOfMessage)
        {
            if (typeof(TResult) == typeof(ValueWebSocketReceiveResult))
            {
                // Although it might seem that this will incur boxing of the struct,
                // the JIT is smart enough to figure out it is unncessessary and will emit
                // bytecode that returns the ValueWebSocketReceiveResult directly.
                return (TResult)(object)new ValueWebSocketReceiveResult(count, messageType, endOfMessage);
            }

            return (TResult)(object)new WebSocketReceiveResult(count, messageType, endOfMessage, _closeStatus, _closeStatusDescription);
        }

        /// <summary>Processes a received close message.</summary>
        /// <param name="header">The message header.</param>
        /// <param name="cancellationToken">The CancellationToken used to cancel the websocket operation.</param>
        /// <returns>The received result message.</returns>
        private async ValueTask HandleReceivedCloseAsync(MessageHeader header, CancellationToken cancellationToken)
        {
            lock (StateUpdateLock)
            {
                _receivedCloseFrame = true;
                if (_sentCloseFrame && _state < WebSocketState.Closed)
                {
                    _state = WebSocketState.Closed;
                }
                else if (_state < WebSocketState.CloseReceived)
                {
                    _state = WebSocketState.CloseReceived;
                }
            }

            WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure;
            string closeStatusDescription = string.Empty;

            // Handle any payload by parsing it into the close status and description.
            if (header.PayloadLength == 1)
            {
                // The close payload length can be 0 or >= 2, but not 1.
                await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted).ConfigureAwait(false);
            }
            else if (header.PayloadLength >= 2)
            {
                if (_receiveBufferCount < header.PayloadLength)
                {
                    await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(false);
                }

                if (_isServer)
                {
                    ApplyMask(_receiveBuffer.Span.Slice(_receiveBufferOffset, (int)header.PayloadLength), header.Mask, 0);
                }

                closeStatus = (WebSocketCloseStatus)(_receiveBuffer.Span[_receiveBufferOffset] << 8 | _receiveBuffer.Span[_receiveBufferOffset + 1]);
                if (!IsValidCloseStatus(closeStatus))
                {
                    await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted).ConfigureAwait(false);
                }

                if (header.PayloadLength > 2)
                {
                    try
                    {
                        closeStatusDescription = s_textEncoding.GetString(_receiveBuffer.Span.Slice(_receiveBufferOffset + 2, (int)header.PayloadLength - 2));
                    }
                    catch (DecoderFallbackException exc)
                    {
                        await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, innerException: exc).ConfigureAwait(false);
                    }
                }
                ConsumeFromBuffer((int)header.PayloadLength);
            }

            // Store the close status and description onto the instance.
            _closeStatus = closeStatus;
            _closeStatusDescription = closeStatusDescription;

            if (!_isServer && _sentCloseFrame)
            {
                await WaitForServerToCloseConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>Issues a read on the stream to wait for EOF.</summary>
        private async ValueTask WaitForServerToCloseConnectionAsync(CancellationToken cancellationToken)
        {
            // Per RFC 6455 7.1.1, try to let the server close the connection.  We give it up to a second.
            // We simply issue a read and don't care what we get back; we could validate that we don't get
            // additional data, but at this point we're about to close the connection and we're just stalling
            // to try to get the server to close first.
            ValueTask<int> finalReadTask = _stream.ReadAsync(_receiveBuffer, cancellationToken);
            if (finalReadTask.IsCompletedSuccessfully)
            {
                finalReadTask.GetAwaiter().GetResult();
            }
            else
            {
                const int WaitForCloseTimeoutMs = 1_000; // arbitrary amount of time to give the server (same duration as .NET Framework)
                try
                {
#pragma warning disable CA2016 // Token was already provided to the ReadAsync
                    await finalReadTask.AsTask().WaitAsync(TimeSpan.FromMilliseconds(WaitForCloseTimeoutMs)).ConfigureAwait(false);
#pragma warning restore CA2016
                }
                catch
                {
                    Abort();
                    // Eat any resulting exceptions.  We were going to close the connection, anyway.
                }
            }
        }

        /// <summary>Processes a received ping or pong message.</summary>
        /// <param name="header">The message header.</param>
        /// <param name="cancellationToken">The CancellationToken used to cancel the websocket operation.</param>
        private async ValueTask HandleReceivedPingPongAsync(MessageHeader header, CancellationToken cancellationToken)
        {
            // Consume any (optional) payload associated with the ping/pong.
            if (header.PayloadLength > 0 && _receiveBufferCount < header.PayloadLength)
            {
                await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(false);
            }

            // If this was a ping, send back a pong response.
            if (header.Opcode == MessageOpcode.Ping)
            {
                if (_isServer)
                {
                    ApplyMask(_receiveBuffer.Span.Slice(_receiveBufferOffset, (int)header.PayloadLength), header.Mask, 0);
                }

                await SendFrameAsync(
                    MessageOpcode.Pong,
                    endOfMessage: true,
                    disableCompression: true,
                    _receiveBuffer.Slice(_receiveBufferOffset, (int)header.PayloadLength),
                    cancellationToken).ConfigureAwait(false);
            }

            // Regardless of whether it was a ping or pong, we no longer need the payload.
            if (header.PayloadLength > 0)
            {
                ConsumeFromBuffer((int)header.PayloadLength);
            }
        }

        /// <summary>Check whether a close status is valid according to the RFC.</summary>
        /// <param name="closeStatus">The status to validate.</param>
        /// <returns>true if the status if valid; otherwise, false.</returns>
        private static bool IsValidCloseStatus(WebSocketCloseStatus closeStatus)
        {
            // 0-999: "not used"
            // 1000-2999: reserved for the protocol; we need to check individual codes manually
            // 3000-3999: reserved for use by higher-level code
            // 4000-4999: reserved for private use
            // 5000-: not mentioned in RFC

            if (closeStatus < (WebSocketCloseStatus)1000 || closeStatus >= (WebSocketCloseStatus)5000)
            {
                return false;
            }

            if (closeStatus >= (WebSocketCloseStatus)3000)
            {
                return true;
            }

            switch (closeStatus) // check for the 1000-2999 range known codes
            {
                case WebSocketCloseStatus.EndpointUnavailable:
                case WebSocketCloseStatus.InternalServerError:
                case WebSocketCloseStatus.InvalidMessageType:
                case WebSocketCloseStatus.InvalidPayloadData:
                case WebSocketCloseStatus.MandatoryExtension:
                case WebSocketCloseStatus.MessageTooBig:
                case WebSocketCloseStatus.NormalClosure:
                case WebSocketCloseStatus.PolicyViolation:
                case WebSocketCloseStatus.ProtocolError:
                case (WebSocketCloseStatus)1012: // ServiceRestart
                case (WebSocketCloseStatus)1013: // TryAgainLater
                case (WebSocketCloseStatus)1014: // BadGateway
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Send a close message to the server and throw an exception, in response to getting bad data from the server.</summary>
        /// <param name="closeStatus">The close status code to use.</param>
        /// <param name="error">The error reason.</param>
        /// <param name="errorMessage">An optional error message to include in the thrown exception.</param>
        /// <param name="innerException">An optional inner exception to include in the thrown exception.</param>
        private async ValueTask CloseWithReceiveErrorAndThrowAsync(
            WebSocketCloseStatus closeStatus, WebSocketError error, string? errorMessage = null, Exception? innerException = null)
        {
            // Close the connection if it hasn't already been closed
            if (!_sentCloseFrame)
            {
                await CloseOutputAsync(closeStatus, string.Empty, default).ConfigureAwait(false);
            }

            // Dump our receive buffer; we're in a bad state to do any further processing
            _receiveBufferCount = 0;

            // Let the caller know we've failed
            throw errorMessage != null ?
                new WebSocketException(error, errorMessage, innerException) :
                new WebSocketException(error, innerException);
        }

        /// <summary>Parses a message header from the buffer.  This assumes the header is in the buffer.</summary>
        /// <param name="resultHeader">The read header.</param>
        /// <returns>null if a valid header was read; non-null containing the string error message to use if the header was invalid.</returns>
        private string? TryParseMessageHeaderFromReceiveBuffer(out MessageHeader resultHeader)
        {
            Debug.Assert(_receiveBufferCount >= 2, "Expected to at least have the first two bytes of the header.");

            MessageHeader header = default;
            Span<byte> receiveBufferSpan = _receiveBuffer.Span;

            header.Fin = (receiveBufferSpan[_receiveBufferOffset] & 0x80) != 0;
            bool reservedSet = (receiveBufferSpan[_receiveBufferOffset] & 0b_0011_0000) != 0;
            header.Opcode = (MessageOpcode)(receiveBufferSpan[_receiveBufferOffset] & 0xF);
            header.Compressed = (receiveBufferSpan[_receiveBufferOffset] & 0b_0100_0000) != 0;

            bool masked = (receiveBufferSpan[_receiveBufferOffset + 1] & 0x80) != 0;
            header.PayloadLength = receiveBufferSpan[_receiveBufferOffset + 1] & 0x7F;

            ConsumeFromBuffer(2);

            // Read the remainder of the payload length, if necessary
            if (header.PayloadLength == 126)
            {
                Debug.Assert(_receiveBufferCount >= 2, "Expected to have two bytes for the payload length.");
                header.PayloadLength = (receiveBufferSpan[_receiveBufferOffset] << 8) | receiveBufferSpan[_receiveBufferOffset + 1];
                ConsumeFromBuffer(2);
            }
            else if (header.PayloadLength == 127)
            {
                Debug.Assert(_receiveBufferCount >= 8, "Expected to have eight bytes for the payload length.");
                header.PayloadLength = 0;
                for (int i = 0; i < 8; i++)
                {
                    header.PayloadLength = (header.PayloadLength << 8) | receiveBufferSpan[_receiveBufferOffset + i];
                }
                ConsumeFromBuffer(8);
            }

            if (reservedSet)
            {
                resultHeader = default;
                return SR.net_Websockets_ReservedBitsSet;
            }

            if (header.PayloadLength < 0)
            {
                // as per RFC, if payload length is a 64-bit integer, the most significant bit MUST be 0
                // frame-payload-length-63 = %x0000000000000000-7FFFFFFFFFFFFFFF; 64 bits in length
                resultHeader = default;
                return SR.net_Websockets_InvalidPayloadLength;
            }

            if (header.Compressed && _inflater is null)
            {
                resultHeader = default;
                return SR.net_Websockets_PerMessageCompressedFlagWhenNotEnabled;
            }

            if (masked)
            {
                if (!_isServer)
                {
                    resultHeader = default;
                    return SR.net_Websockets_ClientReceivedMaskedFrame;
                }
                header.Mask = CombineMaskBytes(receiveBufferSpan, _receiveBufferOffset);

                // Consume the mask bytes
                ConsumeFromBuffer(4);
            }

            // Do basic validation of the header
            switch (header.Opcode)
            {
                case MessageOpcode.Continuation:
                    if (_lastReceiveHeader.Fin)
                    {
                        // Can't continue from a final message
                        resultHeader = default;
                        return SR.net_Websockets_ContinuationFromFinalFrame;
                    }
                    if (header.Compressed)
                    {
                        // Must not mark continuations as compressed
                        resultHeader = default;
                        return SR.net_Websockets_PerMessageCompressedFlagInContinuation;
                    }

                    // Set the compressed flag from the previous header so the receive procedure can use it
                    // directly without needing to check the previous header in case of continuations.
                    header.Compressed = _lastReceiveHeader.Compressed;
                    break;

                case MessageOpcode.Binary:
                case MessageOpcode.Text:
                    if (!_lastReceiveHeader.Fin)
                    {
                        // Must continue from a non-final message
                        resultHeader = default;
                        return SR.net_Websockets_NonContinuationAfterNonFinalFrame;
                    }
                    break;

                case MessageOpcode.Close:
                case MessageOpcode.Ping:
                case MessageOpcode.Pong:
                    if (header.PayloadLength > MaxControlPayloadLength || !header.Fin)
                    {
                        // Invalid control messgae
                        resultHeader = default;
                        return SR.net_Websockets_InvalidControlMessage;
                    }
                    break;

                default:
                    // Unknown opcode
                    resultHeader = default;
                    return SR.Format(SR.net_Websockets_UnknownOpcode, header.Opcode);
            }

            // Return the read header
            header.Processed = header.PayloadLength == 0 && !header.Compressed;
            resultHeader = header;
            return null;
        }

        /// <summary>Send a close message, then receive until we get a close response message.</summary>
        /// <param name="closeStatus">The close status to send.</param>
        /// <param name="statusDescription">The close status description to send.</param>
        /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
        private async Task CloseAsyncPrivate(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            // Send the close message.  Skip sending a close frame if we're currently in a CloseSent state,
            // for example having just done a CloseOutputAsync.
            if (!_sentCloseFrame)
            {
                await SendCloseFrameAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);
            }

            // We should now either be in a CloseSent case (because we just sent one), or in a Closed state, in case
            // there was a concurrent receive that ended up handling an immediate close frame response from the server.
            // Of course it could also be Aborted if something happened concurrently to cause things to blow up.
            Debug.Assert(
                State == WebSocketState.CloseSent ||
                State == WebSocketState.Closed ||
                State == WebSocketState.Aborted,
                $"Unexpected state {State}.");

            // We only need to wait for a received close frame if we are in the CloseSent State. If we are in the Closed
            // State then it means we already received a close frame. If we are in the Aborted State, then we should not
            // wait for a close frame as per RFC 6455 Section 7.1.7 "Fail the WebSocket Connection".
            if (State == WebSocketState.CloseSent)
            {
                // Wait until we've received a close response
                byte[] closeBuffer = ArrayPool<byte>.Shared.Rent(MaxMessageHeaderLength + MaxControlPayloadLength);
                try
                {
                    // Loop until we've received a close frame.
                    while (!_receivedCloseFrame)
                    {
                        // Enter the receive lock in order to get a consistent view of whether we've received a close
                        // frame.  If we haven't, issue a receive.  Since that receive will try to take the same
                        // non-entrant receive lock, we then exit the lock before waiting for the receive to complete,
                        // as it will always complete asynchronously and only after we've exited the lock.
                        ValueTask<ValueWebSocketReceiveResult> receiveTask = default;
                        try
                        {
                            await _receiveMutex.EnterAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                if (!_receivedCloseFrame)
                                {
                                    receiveTask = ReceiveAsyncPrivate<ValueWebSocketReceiveResult>(closeBuffer, cancellationToken);
                                }
                            }
                            finally
                            {
                                _receiveMutex.Exit();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // If waiting on the receive lock was canceled, abort the connection, as we would do
                            // as part of the receive itself.
                            Abort();
                            throw;
                        }

                        // Wait for the receive to complete if we issued one.
                        await receiveTask.ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(closeBuffer);
                }
            }

            // We're closed.  Close the connection and update the status.
            lock (StateUpdateLock)
            {
                DisposeCore();
            }
        }

        /// <summary>Sends a close message to the server.</summary>
        /// <param name="closeStatus">The close status to send.</param>
        /// <param name="closeStatusDescription">The close status description to send.</param>
        /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
        private async ValueTask SendCloseFrameAsync(WebSocketCloseStatus closeStatus, string? closeStatusDescription, CancellationToken cancellationToken)
        {
            // Close payload is two bytes containing the close status followed by a UTF8-encoding of the status description, if it exists.

            byte[]? buffer = null;
            try
            {
                int count = 2;
                if (string.IsNullOrEmpty(closeStatusDescription))
                {
                    buffer = ArrayPool<byte>.Shared.Rent(count);
                }
                else
                {
                    count += s_textEncoding.GetByteCount(closeStatusDescription);
                    buffer = ArrayPool<byte>.Shared.Rent(count);
                    int encodedLength = s_textEncoding.GetBytes(closeStatusDescription, 0, closeStatusDescription.Length, buffer, 2);
                    Debug.Assert(count - 2 == encodedLength, $"{nameof(s_textEncoding.GetByteCount)} and {nameof(s_textEncoding.GetBytes)} encoded count didn't match");
                }

                ushort closeStatusValue = (ushort)closeStatus;
                buffer[0] = (byte)(closeStatusValue >> 8);
                buffer[1] = (byte)(closeStatusValue & 0xFF);

                await SendFrameAsync(MessageOpcode.Close, endOfMessage: true, disableCompression: true, new Memory<byte>(buffer, 0, count), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            lock (StateUpdateLock)
            {
                _sentCloseFrame = true;
                if (_receivedCloseFrame && _state < WebSocketState.Closed)
                {
                    _state = WebSocketState.Closed;
                }
                else if (_state < WebSocketState.CloseSent)
                {
                    _state = WebSocketState.CloseSent;
                }
            }

            if (!_isServer && _receivedCloseFrame)
            {
                await WaitForServerToCloseConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private void ConsumeFromBuffer(int count)
        {
            Debug.Assert(count >= 0, $"Expected non-negative {nameof(count)}, got {count}");
            Debug.Assert(count <= _receiveBufferCount, $"Trying to consume {count}, which is more than exists {_receiveBufferCount}");
            _receiveBufferCount -= count;
            _receiveBufferOffset += count;
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask EnsureBufferContainsAsync(int minimumRequiredBytes, CancellationToken cancellationToken)
        {
            Debug.Assert(minimumRequiredBytes <= _receiveBuffer.Length, $"Requested number of bytes {minimumRequiredBytes} must not exceed {_receiveBuffer.Length}");

            // If we don't have enough data in the buffer to satisfy the minimum required, read some more.
            if (_receiveBufferCount < minimumRequiredBytes)
            {
                // If there's any data in the buffer, shift it down.
                if (_receiveBufferCount > 0)
                {
                    _receiveBuffer.Span.Slice(_receiveBufferOffset, _receiveBufferCount).CopyTo(_receiveBuffer.Span);
                }
                _receiveBufferOffset = 0;

                // While we don't have enough data, read more.
                if (_receiveBufferCount < minimumRequiredBytes)
                {
                    int bytesToRead = minimumRequiredBytes - _receiveBufferCount;
                    int numRead = await _stream.ReadAtLeastAsync(
                        _receiveBuffer.Slice(_receiveBufferCount), bytesToRead, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
                    _receiveBufferCount += numRead;

                    if (numRead < bytesToRead)
                    {
                        ThrowEOFUnexpected();
                    }
                }
            }
        }

        private void ThrowEOFUnexpected()
        {
            // The connection closed before we were able to read everything we needed.
            // If it was due to us being disposed, fail with the correct exception.
            // Otherwise, it was due to the connection being closed and it wasn't expected.
            ObjectDisposedException.ThrowIf(_disposed, typeof(WebSocket));
            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        }

        /// <summary>Gets a send buffer from the pool.</summary>
        private void AllocateSendBuffer(int minLength)
        {
            Debug.Assert(_sendBuffer == null); // would only fail if had some catastrophic error previously that prevented cleaning up
            _sendBuffer = ArrayPool<byte>.Shared.Rent(minLength);
        }

        /// <summary>Releases the send buffer to the pool.</summary>
        private void ReleaseSendBuffer()
        {
            Debug.Assert(_sendMutex.IsHeld, $"Caller should hold the {nameof(_sendMutex)}");

            if (_sendBuffer is byte[] toReturn)
            {
                _sendBuffer = null;
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }

        private static int CombineMaskBytes(Span<byte> buffer, int maskOffset) =>
            BitConverter.ToInt32(buffer.Slice(maskOffset));

        /// <summary>Applies a mask to a portion of a byte array.</summary>
        /// <param name="toMask">The buffer to which the mask should be applied.</param>
        /// <param name="mask">The array containing the mask to apply.</param>
        /// <param name="maskOffset">The offset into <paramref name="mask"/> of the mask to apply of length <see cref="MaskLength"/>.</param>
        /// <param name="maskOffsetIndex">The next position offset from <paramref name="maskOffset"/> of which by to apply next from the mask.</param>
        /// <returns>The updated maskOffsetOffset value.</returns>
        private static int ApplyMask(Span<byte> toMask, byte[] mask, int maskOffset, int maskOffsetIndex)
        {
            Debug.Assert(maskOffsetIndex < MaskLength, $"Unexpected {nameof(maskOffsetIndex)}: {maskOffsetIndex}");
            Debug.Assert(mask.Length >= MaskLength + maskOffset, $"Unexpected inputs: {mask.Length}, {maskOffset}");
            return ApplyMask(toMask, CombineMaskBytes(mask, maskOffset), maskOffsetIndex);
        }

        /// <summary>Applies a mask to a portion of a byte array.</summary>
        /// <param name="toMask">The buffer to which the mask should be applied.</param>
        /// <param name="mask">The four-byte mask, stored as an Int32.</param>
        /// <param name="maskIndex">The index into the mask.</param>
        /// <returns>The next index into the mask to be used for future applications of the mask.</returns>
        private static unsafe int ApplyMask(Span<byte> toMask, int mask, int maskIndex)
        {
            Debug.Assert(maskIndex < sizeof(int));

            fixed (byte* toMaskBeg = &MemoryMarshal.GetReference(toMask))
            {
                byte* toMaskPtr = toMaskBeg;
                byte* toMaskEnd = toMaskBeg + toMask.Length;

                if (toMaskEnd - toMaskPtr >= sizeof(int))
                {
                    int rolledMask = BitConverter.IsLittleEndian ?
                        (int)BitOperations.RotateRight((uint)mask, maskIndex * 8) :
                        (int)BitOperations.RotateLeft((uint)mask, maskIndex * 8);

                    // Process Vector<byte>.Count bytes at a time.
                    if (Vector.IsHardwareAccelerated && (toMaskEnd - toMaskPtr) >= Vector<byte>.Count)
                    {
                        Vector<byte> maskVector = Vector.AsVectorByte(new Vector<int>(rolledMask));
                        do
                        {
                            *(Vector<byte>*)toMaskPtr ^= maskVector;
                            toMaskPtr += Vector<byte>.Count;
                        }
                        while (toMaskEnd - toMaskPtr >= Vector<byte>.Count);
                    }

                    // Process 4 bytes at a time.
                    while (toMaskEnd - toMaskPtr >= sizeof(int))
                    {
                        *(int*)toMaskPtr ^= rolledMask;
                        toMaskPtr += sizeof(int);
                    }
                }

                // Process 1 byte at a time.
                byte* maskPtr = (byte*)&mask;
                while (toMaskPtr != toMaskEnd)
                {
                    *toMaskPtr++ ^= maskPtr[maskIndex];
                    maskIndex = (maskIndex + 1) & 3;
                }
            }

            return maskIndex;
        }

        /// <summary>Aborts the websocket and throws an exception if an existing operation is in progress.</summary>
        private void ThrowIfOperationInProgress(bool operationCompleted, [CallerMemberName] string? methodName = null)
        {
            if (!operationCompleted)
            {
                Abort();
                ThrowOperationInProgress(methodName);
            }
        }

        private static void ThrowOperationInProgress(string? methodName) => throw new InvalidOperationException(SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, methodName));

        /// <summary>Creates an OperationCanceledException instance, using a default message and the specified inner exception and token.</summary>
        private static OperationCanceledException CreateOperationCanceledException(Exception innerException, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new OperationCanceledException(
                new OperationCanceledException().Message,
                innerException,
                cancellationToken);
        }

        // From https://github.com/aspnet/WebSockets/blob/aa63e27fce2e9202698053620679a9a1059b501e/src/Microsoft.AspNetCore.WebSockets.Protocol/Utilities.cs#L75
        // Performs a stateful validation of UTF-8 bytes.
        // It checks for valid formatting, overlong encodings, surrogates, and value ranges.
        private static bool TryValidateUtf8(Span<byte> span, bool endOfMessage, Utf8MessageState state)
        {
            for (int i = 0; i < span.Length;)
            {
                // Have we started a character sequence yet?
                if (!state.SequenceInProgress)
                {
                    // The first byte tells us how many bytes are in the sequence.
                    state.SequenceInProgress = true;
                    byte b = span[i];
                    i++;
                    if ((b & 0x80) == 0) // 0bbbbbbb, single byte
                    {
                        state.AdditionalBytesExpected = 0;
                        state.CurrentDecodeBits = b & 0x7F;
                        state.ExpectedValueMin = 0;
                    }
                    else if ((b & 0xC0) == 0x80)
                    {
                        // Misplaced 10bbbbbb continuation byte. This cannot be the first byte.
                        return false;
                    }
                    else if ((b & 0xE0) == 0xC0) // 110bbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 1;
                        state.CurrentDecodeBits = b & 0x1F;
                        state.ExpectedValueMin = 0x80;
                    }
                    else if ((b & 0xF0) == 0xE0) // 1110bbbb 10bbbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 2;
                        state.CurrentDecodeBits = b & 0xF;
                        state.ExpectedValueMin = 0x800;
                    }
                    else if ((b & 0xF8) == 0xF0) // 11110bbb 10bbbbbb 10bbbbbb 10bbbbbb
                    {
                        state.AdditionalBytesExpected = 3;
                        state.CurrentDecodeBits = b & 0x7;
                        state.ExpectedValueMin = 0x10000;
                    }
                    else // 111110bb & 1111110b & 11111110 && 11111111 are not valid
                    {
                        return false;
                    }
                }
                while (state.AdditionalBytesExpected > 0 && i < span.Length)
                {
                    byte b = span[i];
                    if ((b & 0xC0) != 0x80)
                    {
                        return false;
                    }

                    i++;
                    state.AdditionalBytesExpected--;

                    // Each continuation byte carries 6 bits of data 0x10bbbbbb.
                    state.CurrentDecodeBits = (state.CurrentDecodeBits << 6) | (b & 0x3F);

                    if (state.AdditionalBytesExpected == 1 && state.CurrentDecodeBits >= 0x360 && state.CurrentDecodeBits <= 0x37F)
                    {
                        // This is going to end up in the range of 0xD800-0xDFFF UTF-16 surrogates that are not allowed in UTF-8;
                        return false;
                    }
                    if (state.AdditionalBytesExpected == 2 && state.CurrentDecodeBits >= 0x110)
                    {
                        // This is going to be out of the upper Unicode bound 0x10FFFF.
                        return false;
                    }
                }
                if (state.AdditionalBytesExpected == 0)
                {
                    state.SequenceInProgress = false;
                    if (state.CurrentDecodeBits < state.ExpectedValueMin)
                    {
                        // Overlong encoding (e.g. using 2 bytes to encode something that only needed 1).
                        return false;
                    }
                }
            }
            if (endOfMessage && state.SequenceInProgress)
            {
                return false;
            }
            return true;
        }

        private sealed class Utf8MessageState
        {
            internal bool SequenceInProgress;
            internal int AdditionalBytesExpected;
            internal int ExpectedValueMin;
            internal int CurrentDecodeBits;
        }

        private enum MessageOpcode : byte
        {
            Continuation = 0x0,
            Text = 0x1,
            Binary = 0x2,
            Close = 0x8,
            Ping = 0x9,
            Pong = 0xA
        }

        [StructLayout(LayoutKind.Auto)]
        private struct MessageHeader
        {
            internal MessageOpcode Opcode;
            internal bool Fin;
            internal long PayloadLength;
            internal bool Compressed;
            internal int Mask;

            /// <summary>
            /// Returns if frame has been received and processed.
            /// </summary>
            internal bool Processed { get; set; }

            /// <summary>
            /// Returns if message has been received and processed.
            /// </summary>
            internal bool EndOfMessage => Fin && Processed && PayloadLength == 0;
        }
    }
}
