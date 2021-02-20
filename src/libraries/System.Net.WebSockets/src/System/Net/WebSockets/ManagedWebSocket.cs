// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
    [UnsupportedOSPlatform("browser")]
    internal sealed partial class ManagedWebSocket : WebSocket
    {
        /// <summary>Creates a <see cref="ManagedWebSocket"/> from a <see cref="Stream"/> connected to a websocket endpoint.</summary>
        /// <param name="stream">The connected Stream.</param>
        /// <param name="options">The options with which the websocket must be created.</param>
        /// <returns>The created <see cref="ManagedWebSocket"/> instance.</returns>
        public static ManagedWebSocket CreateFromConnectedStream(Stream stream, WebSocketCreationOptions options)
        {
            return new ManagedWebSocket(stream, options);
        }

        /// <summary>Encoding for the payload of text messages: UTF8 encoding that throws if invalid bytes are discovered, per the RFC.</summary>
        private static readonly UTF8Encoding s_textEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        /// <summary>Valid states to be in when calling SendAsync.</summary>
        private static readonly WebSocketState[] s_validSendStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        /// <summary>Valid states to be in when calling ReceiveAsync.</summary>
        private static readonly WebSocketState[] s_validReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent };
        /// <summary>Valid states to be in when calling CloseOutputAsync.</summary>
        private static readonly WebSocketState[] s_validCloseOutputStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        /// <summary>Valid states to be in when calling CloseAsync.</summary>
        private static readonly WebSocketState[] s_validCloseStates = { WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent };

        /// <summary>Successfully completed task representing a close message.</summary>
        private static readonly Task<WebSocketReceiveResult> s_cachedCloseTask = Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));

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
        /// <summary>CancellationTokenSource used to abort all current and future operations when anything is canceled or any error occurs.</summary>
        private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();

        /// <summary>
        /// Semaphore used to ensure that calls to SendFrameAsync don't run concurrently.
        /// </summary>
        private readonly SemaphoreSlim _sendFrameAsyncLock = new SemaphoreSlim(1, 1);
        private readonly Sender _sender;
        private readonly Receiver _receiver;

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
        /// Tracks the state of the validity of the UTF8 encoding of text payloads.  Text may be split across fragments.
        /// </summary>
        private readonly Utf8MessageState _utf8TextState = new Utf8MessageState();

        /// <summary>
        /// Whether the last SendAsync had endOfMessage==false. We need to track this so that we
        /// can send the subsequent message with a continuation opcode if the last message was a fragment.
        /// </summary>
        private bool _lastSendWasFragment;
        /// <summary>
        /// The task returned from the last ReceiveAsync(ArraySegment, ...) operation to not complete synchronously.
        /// If this is not null and not completed when a subsequent ReceiveAsync is issued, an exception occurs.
        /// </summary>
        private Task _lastReceiveAsync = Task.CompletedTask;

        /// <summary>Lock used to protect update and check-and-update operations on _state.</summary>
        private object StateUpdateLock => _abortSource;
        /// <summary>
        /// We need to coordinate between receives and close operations happening concurrently, as a ReceiveAsync may
        /// be pending while a Close{Output}Async is issued, which itself needs to loop until a close frame is received.
        /// As such, we need thread-safety in the management of <see cref="_lastReceiveAsync"/>.
        /// </summary>
        private object ReceiveAsyncLock => _receiver; // some object, as we're simply lock'ing on it

        private ManagedWebSocket(Stream stream, WebSocketCreationOptions options)
        {
            _sender = new Sender(stream, options);
            _receiver = new Receiver(stream, options);

            Debug.Assert(StateUpdateLock != null, $"Expected {nameof(StateUpdateLock)} to be non-null");
            Debug.Assert(ReceiveAsyncLock != null, $"Expected {nameof(ReceiveAsyncLock)} to be non-null");
            Debug.Assert(StateUpdateLock != ReceiveAsyncLock, "Locks should be different objects");

            Debug.Assert(stream != null, $"Expected non-null stream");
            Debug.Assert(stream.CanRead, $"Expected readable stream");
            Debug.Assert(stream.CanWrite, $"Expected writeable stream");

            _stream = stream;
            _isServer = options.IsServer;
            _subprotocol = options.SubProtocol;

            // Set up the abort source so that if it's triggered, we transition the instance appropriately.
            // There's no need to store the resulting CancellationTokenRegistration, as this instance owns
            // the CancellationTokenSource, and the lifetime of that CTS matches the lifetime of the registration.
            _abortSource.Token.UnsafeRegister(static s =>
            {
                var thisRef = (ManagedWebSocket)s!;

                lock (thisRef.StateUpdateLock)
                {
                    WebSocketState state = thisRef._state;
                    if (state != WebSocketState.Closed && state != WebSocketState.Aborted)
                    {
                        thisRef._state = state != WebSocketState.None && state != WebSocketState.Connecting ?
                            WebSocketState.Aborted :
                            WebSocketState.Closed;
                    }
                }
            }, this);

            // Now that we're opened, initiate the keep alive timer to send periodic pings.
            // We use a weak reference from the timer to the web socket to avoid a cycle
            // that could keep the web socket rooted in erroneous cases.
            if (options.KeepAliveInterval > TimeSpan.Zero)
            {
                _keepAliveTimer = new Timer(static s =>
                {
                    var wr = (WeakReference<ManagedWebSocket>)s!;
                    if (wr.TryGetTarget(out ManagedWebSocket? thisRef))
                    {
                        thisRef.SendKeepAliveFrameAsync();
                    }
                }, new WeakReference<ManagedWebSocket>(this), options.KeepAliveInterval, options.KeepAliveInterval);
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
                _sender.Dispose();
                _receiver.Dispose();

                if (_state < WebSocketState.Aborted)
                {
                    _state = WebSocketState.Closed;
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

            return SendPrivateAsync(buffer, messageType, endOfMessage, cancellationToken).AsTask();
        }

        private ValueTask SendPrivateAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
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

            MessageOpcode opcode =
                _lastSendWasFragment ? MessageOpcode.Continuation :
                messageType == WebSocketMessageType.Binary ? MessageOpcode.Binary :
                MessageOpcode.Text;

            ValueTask t = SendFrameAsync(opcode, endOfMessage, buffer, cancellationToken);
            _lastSendWasFragment = !endOfMessage;
            return t;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);

                Debug.Assert(!Monitor.IsEntered(StateUpdateLock), $"{nameof(StateUpdateLock)} must never be held when acquiring {nameof(ReceiveAsyncLock)}");
                lock (ReceiveAsyncLock) // synchronize with receives in CloseAsync
                {
                    ThrowIfOperationInProgress(_lastReceiveAsync.IsCompleted);
                    Task<WebSocketReceiveResult> t = ReceiveAsyncPrivate<WebSocketReceiveResultGetter, WebSocketReceiveResult>(buffer, cancellationToken).AsTask();
                    _lastReceiveAsync = t;
                    return t;
                }
            }
            catch (Exception exc)
            {
                return Task.FromException<WebSocketReceiveResult>(exc);
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
            _abortSource.Cancel();
            Dispose(); // forcibly tear down connection
        }

        public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return SendPrivateAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);

                Debug.Assert(!Monitor.IsEntered(StateUpdateLock), $"{nameof(StateUpdateLock)} must never be held when acquiring {nameof(ReceiveAsyncLock)}");
                lock (ReceiveAsyncLock) // synchronize with receives in CloseAsync
                {
                    ThrowIfOperationInProgress(_lastReceiveAsync.IsCompleted);

                    ValueTask<ValueWebSocketReceiveResult> receiveValueTask = ReceiveAsyncPrivate<ValueWebSocketReceiveResultGetter, ValueWebSocketReceiveResult>(buffer, cancellationToken);
                    if (receiveValueTask.IsCompletedSuccessfully)
                    {
                        _lastReceiveAsync = receiveValueTask.Result.MessageType == WebSocketMessageType.Close ? s_cachedCloseTask : Task.CompletedTask;
                        return receiveValueTask;
                    }

                    // We need to both store the last receive task and return it, but we can't do that with a ValueTask,
                    // as that could result in consuming it multiple times.  Instead, we use AsTask to consume it just once,
                    // and then store that Task and return a new ValueTask that wraps it. (It would be nice in the future
                    // to avoid this AsTask as well; currently it's used both for error detection and as part of close tracking.)
                    Task<ValueWebSocketReceiveResult> receiveTask = receiveValueTask.AsTask();
                    _lastReceiveAsync = receiveTask;
                    return new ValueTask<ValueWebSocketReceiveResult>(receiveTask);
                }
            }
            catch (Exception exc)
            {
                return ValueTask.FromException<ValueWebSocketReceiveResult>(exc);
            }
        }

        private Task ValidateAndReceiveAsync(Task receiveTask, CancellationToken cancellationToken)
        {
            if (receiveTask.IsCompletedSuccessfully &&
               !(receiveTask is Task<WebSocketReceiveResult> wsrr && wsrr.Result.MessageType == WebSocketMessageType.Close) &&
               !(receiveTask is Task<ValueWebSocketReceiveResult> vwsrr && vwsrr.Result.MessageType == WebSocketMessageType.Close))
            {
                ValueTask<ValueWebSocketReceiveResult> vt = ReceiveAsyncPrivate<ValueWebSocketReceiveResultGetter, ValueWebSocketReceiveResult>(Memory<byte>.Empty, cancellationToken);
                receiveTask =
                    vt.IsCompletedSuccessfully ? (vt.Result.MessageType == WebSocketMessageType.Close ? s_cachedCloseTask : Task.CompletedTask) :
                    vt.AsTask();
            }

            return receiveTask;
        }

        /// <summary><see cref="IWebSocketReceiveResultGetter{TResult}"/> implementation for <see cref="ValueWebSocketReceiveResult"/>.</summary>
        private readonly struct ValueWebSocketReceiveResultGetter : IWebSocketReceiveResultGetter<ValueWebSocketReceiveResult>
        {
            public ValueWebSocketReceiveResult GetResult(int count, WebSocketMessageType messageType, bool endOfMessage, WebSocketCloseStatus? closeStatus, string? closeDescription) =>
                new ValueWebSocketReceiveResult(count, messageType, endOfMessage); // closeStatus/closeDescription are ignored
        }

        /// <summary>Sends a websocket frame to the network.</summary>
        /// <param name="opcode">The opcode for the message.</param>
        /// <param name="endOfMessage">The value of the FIN bit for the message.</param>
        /// <param name="payloadBuffer">The buffer containing the payload data fro the message.</param>
        /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
        private ValueTask SendFrameAsync(MessageOpcode opcode, bool endOfMessage, ReadOnlyMemory<byte> payloadBuffer, CancellationToken cancellationToken)
        {
            // If a cancelable cancellation token was provided, that would require registering with it, which means more state we have to
            // pass around (the CancellationTokenRegistration), so if it is cancelable, just immediately go to the fallback path.
            // Similarly, it should be rare that there are multiple outstanding calls to SendFrameAsync, but if there are, again
            // fall back to the fallback path.
            return cancellationToken.CanBeCanceled || !_sendFrameAsyncLock.Wait(0, default) ?
                SendFrameFallbackAsync(opcode, endOfMessage, payloadBuffer, cancellationToken) :
                SendFrameLockAcquiredNonCancelableAsync(opcode, endOfMessage, payloadBuffer);
        }

        /// <summary>Sends a websocket frame to the network. The caller must hold the sending lock.</summary>
        /// <param name="opcode">The opcode for the message.</param>
        /// <param name="endOfMessage">The value of the FIN bit for the message.</param>
        /// <param name="payloadBuffer">The buffer containing the payload data fro the message.</param>
        private ValueTask SendFrameLockAcquiredNonCancelableAsync(MessageOpcode opcode, bool endOfMessage, ReadOnlyMemory<byte> payloadBuffer)
        {
            Debug.Assert(_sendFrameAsyncLock.CurrentCount == 0, "Caller should hold the _sendFrameAsyncLock");

            // If we get here, the cancellation token is not cancelable so we don't have to worry about it,
            // and we own the semaphore, so we don't need to asynchronously wait for it.
            ValueTask writeTask = default;
            bool releaseSemaphore = true;
            try
            {
                writeTask = _sender.SendAsync(opcode, endOfMessage, payloadBuffer);

                // If the operation happens to complete synchronously (or, more specifically, by
                // the time we get from the previous line to here), release the semaphore, return
                // the task, and we're done.
                if (writeTask.IsCompleted)
                {
                    return writeTask;
                }

                // Up until this point, if an exception occurred (such as when accessing _stream or when
                // calling GetResult), we want to release the semaphore and the send buffer. After this point,
                // both need to be held until writeTask completes.
                releaseSemaphore = false;
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
                if (releaseSemaphore)
                {
                    _sendFrameAsyncLock.Release();
                }
            }

            return WaitForWriteTaskAsync(writeTask);
        }

        private async ValueTask WaitForWriteTaskAsync(ValueTask writeTask)
        {
            try
            {
                await writeTask.ConfigureAwait(false);
            }
            catch (Exception exc) when (!(exc is OperationCanceledException))
            {
                throw _state == WebSocketState.Aborted ?
                    CreateOperationCanceledException(exc) :
                    new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc);
            }
            finally
            {
                _sendFrameAsyncLock.Release();
            }
        }

        private async ValueTask SendFrameFallbackAsync(MessageOpcode opcode, bool endOfMessage, ReadOnlyMemory<byte> payloadBuffer, CancellationToken cancellationToken)
        {
            await _sendFrameAsyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using (cancellationToken.Register(static s => ((ManagedWebSocket)s!).Abort(), this))
                {
                    await _sender.SendAsync(opcode, endOfMessage, payloadBuffer, cancellationToken).ConfigureAwait(false);
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
                _sendFrameAsyncLock.Release();
            }
        }

        private void SendKeepAliveFrameAsync()
        {
            bool acquiredLock = _sendFrameAsyncLock.Wait(0);
            if (acquiredLock)
            {
                // This exists purely to keep the connection alive; don't wait for the result, and ignore any failures.
                // The call will handle releasing the lock.  We send a pong rather than ping, since it's allowed by
                // the RFC as a unidirectional heartbeat and we're not interested in waiting for a response.
                ValueTask t = SendFrameLockAcquiredNonCancelableAsync(MessageOpcode.Pong, true, ReadOnlyMemory<byte>.Empty);
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
            else
            {
                // If the lock is already held, something is already getting sent,
                // so there's no need to send a keep-alive ping.
            }
        }

        /// <summary>
        /// Receive the next text, binary, continuation, or close message, returning information about it and
        /// writing its payload into the supplied buffer.  Other control messages may be consumed and processed
        /// as part of this operation, but data about them will not be returned.
        /// </summary>
        /// <param name="payloadBuffer">The buffer into which payload data should be written.</param>
        /// <param name="cancellationToken">The CancellationToken used to cancel the websocket.</param>
        /// <param name="resultGetter">Used to get the result.  Allows the same method to be used with both WebSocketReceiveResult and ValueWebSocketReceiveResult.</param>
        /// <returns>Information about the received message.</returns>
        private async ValueTask<TWebSocketReceiveResult> ReceiveAsyncPrivate<TWebSocketReceiveResultGetter, TWebSocketReceiveResult>(
            Memory<byte> payloadBuffer,
            CancellationToken cancellationToken,
            TWebSocketReceiveResultGetter resultGetter = default)
            where TWebSocketReceiveResultGetter : struct, IWebSocketReceiveResultGetter<TWebSocketReceiveResult> // constrained to avoid boxing and enable inlining
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
                while (true) // in case we get control frames that should be ignored from the user's perspective
                {
                    ReceiveResult result = await _receiver.ReceiveAsync(payloadBuffer, cancellationToken).ConfigureAwait(false);

                    if (result.ResultType != ReceiveResultType.Message)
                    {
                        if (result.ResultType == ReceiveResultType.ControlMessage)
                        {
                            var messageOrNull = await _receiver.ReceiveControlMessageAsync(cancellationToken).ConfigureAwait(false);
                            if (messageOrNull is null)
                            {
                                ThrowIfEOFUnexpected(true);
                            }
                            ControlMessage message = messageOrNull.GetValueOrDefault();

                            // If the header represents a ping or a pong, it's a control message meant
                            // to be transparent to the user, so handle it and then loop around to read again.
                            // Alternatively, if it's a close message, handle it and exit.
                            if (message.Opcode is MessageOpcode.Ping or MessageOpcode.Pong)
                            {
                                // If this was a ping, send back a pong response.
                                if (message.Opcode == MessageOpcode.Ping)
                                {
                                    await SendFrameAsync(MessageOpcode.Pong, endOfMessage: true, message.Payload, cancellationToken).ConfigureAwait(false);
                                }
                                continue;
                            }
                            else
                            {
                                Debug.Assert(message.Opcode == MessageOpcode.Close);

                                await HandleReceivedCloseAsync(message.Payload, cancellationToken).ConfigureAwait(false);
                                return resultGetter.GetResult(0, WebSocketMessageType.Close, true, _closeStatus, _closeStatusDescription);
                            }
                        }
                        else if (result.ResultType == ReceiveResultType.ConnectionClose)
                        {
                            ThrowIfEOFUnexpected(true);
                        }
                        else
                        {
                            Debug.Assert(result.ResultType == ReceiveResultType.HeaderError);

                            string? error = _receiver.GetHeaderError();
                            await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, error).ConfigureAwait(false);
                        }
                    }

                    // If this a text message, validate that it contains valid UTF8.
                    if (result.MessageType == WebSocketMessageType.Text && result.Count > 0 &&
                        !TryValidateUtf8(payloadBuffer.Span.Slice(0, result.Count), result.EndOfMessage, _utf8TextState))
                    {
                        await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.InvalidPayloadData, WebSocketError.Faulted).ConfigureAwait(false);
                    }

                    return resultGetter.GetResult(
                        count: result.Count,
                        messageType: result.MessageType,
                        endOfMessage: result.EndOfMessage,
                        closeStatus: null, closeDescription: null);
                }
            }
            catch (Exception exc) when (exc is not OperationCanceledException)
            {
                if (_state == WebSocketState.Aborted)
                {
                    throw new OperationCanceledException(nameof(WebSocketState.Aborted), exc);
                }
                _abortSource.Cancel();

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

        /// <summary>Processes a received close message.</summary>
        private async ValueTask HandleReceivedCloseAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
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
            if (payload.Length == 1)
            {
                // The close payload length can be 0 or >= 2, but not 1.
                await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted).ConfigureAwait(false);
            }
            else if (payload.Length >= 2)
            {
                closeStatus = (WebSocketCloseStatus)(payload.Span[0] << 8 | payload.Span[1]);
                if (!IsValidCloseStatus(closeStatus))
                {
                    await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted).ConfigureAwait(false);
                }

                if (payload.Length > 2)
                {
                    try
                    {
                        closeStatusDescription = s_textEncoding.GetString(payload.Span.Slice(2));
                    }
                    catch (DecoderFallbackException exc)
                    {
                        await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, innerException: exc).ConfigureAwait(false);
                    }
                }
            }

            // Store the close status and description onto the instance.
            _closeStatus = closeStatus;
            _closeStatusDescription = closeStatusDescription;

            bool closeOutput = false;

            lock (StateUpdateLock)
            {
                if (!_sentCloseFrame)
                {
                    _sentCloseFrame = true;
                    closeOutput = true;
                }
            }

            if (closeOutput)
            {
                await CloseOutputAsyncCore(closeStatus, closeStatusDescription, cancellationToken).ConfigureAwait(false);
            }
            else if (!_isServer)
            {
                await _receiver.WaitForServerToCloseConnectionAsync(cancellationToken).ConfigureAwait(false);
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

            // Let the caller know we've failed
            throw errorMessage != null ?
                new WebSocketException(error, errorMessage, innerException) :
                new WebSocketException(error, innerException);
        }

        private static bool TryParseMessageHeader(
            ReadOnlySpan<byte> buffer,
            MessageHeader previousHeader,
            bool isServer,
            out MessageHeader header,
            out string? error,
            out int consumedBytes)
        {
            header = default;
            consumedBytes = 0;
            error = null;

            if (buffer.Length < 2)
            {
                return false;
            }
            // Check first for reserved bits that should always be unset
            if ((buffer[0] & 0b0011_0000) != 0)
            {
                return Error(ref error, SR.net_Websockets_ReservedBitsSet);
            }
            header.Fin = (buffer[0] & 0x80) != 0;
            header.Opcode = (MessageOpcode)(buffer[0] & 0xF);
            header.Compressed = (buffer[0] & 0b0100_0000) != 0;

            bool masked = (buffer[1] & 0x80) != 0;
            if (masked && !isServer)
            {
                return Error(ref error, SR.net_Websockets_ClientReceivedMaskedFrame);
            }
            header.PayloadLength = buffer[1] & 0x7F;

            // We've consumed the first 2 bytes
            buffer = buffer.Slice(2);
            consumedBytes += 2;

            // Read the remainder of the payload length, if necessary
            if (header.PayloadLength == 126)
            {
                if (buffer.Length < 2)
                {
                    return false;
                }
                header.PayloadLength = (buffer[0] << 8) | buffer[1];
                buffer = buffer.Slice(2);
                consumedBytes += 2;
            }
            else if (header.PayloadLength == 127)
            {
                if (buffer.Length < 8)
                {
                    return false;
                }
                header.PayloadLength = 0;
                for (int i = 0; i < 8; ++i)
                {
                    header.PayloadLength = (header.PayloadLength << 8) | buffer[i];
                }
                buffer = buffer.Slice(8);
                consumedBytes += 8;
            }

            if (masked)
            {
                if (buffer.Length < MaskLength)
                {
                    return false;
                }
                header.Mask = BitConverter.ToInt32(buffer);
                consumedBytes += MaskLength;
            }

            // Do basic validation of the header
            switch (header.Opcode)
            {
                case MessageOpcode.Continuation:
                    if (previousHeader.Fin)
                    {
                        // Can't continue from a final message
                        return Error(ref error, SR.net_Websockets_ContinuationFromFinalFrame);
                    }
                    if (header.Compressed)
                    {
                        // Per-Message Compressed flag must be set only in the first frame
                        return Error(ref error, SR.net_Websockets_PerMessageCompressedFlagInContinuation);
                    }
                    break;

                case MessageOpcode.Binary:
                case MessageOpcode.Text:
                    if (!previousHeader.Fin)
                    {
                        // Must continue from a non-final message
                        return Error(ref error, SR.net_Websockets_NonContinuationAfterNonFinalFrame);
                    }
                    break;

                case MessageOpcode.Close:
                case MessageOpcode.Ping:
                case MessageOpcode.Pong:
                    if (header.PayloadLength > MaxControlPayloadLength || !header.Fin)
                    {
                        // Invalid control messgae
                        return Error(ref error, SR.net_Websockets_InvalidControlMessage);
                    }
                    break;

                default:
                    // Unknown opcode
                    return Error(ref error, SR.Format(SR.net_Websockets_UnknownOpcode, header.Opcode));
            }

            return true;

            static bool Error(ref string? target, string error)
            {
                target = error;
                return false;
            }
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
                while (!_receivedCloseFrame)
                {
                    Debug.Assert(!Monitor.IsEntered(StateUpdateLock), $"{nameof(StateUpdateLock)} must never be held when acquiring {nameof(ReceiveAsyncLock)}");
                    Task receiveTask;
                    bool usingExistingReceive;
                    lock (ReceiveAsyncLock)
                    {
                        // Now that we're holding the ReceiveAsyncLock, double-check that we've not yet received the close frame.
                        // It could have been received between our check above and now due to a concurrent receive completing.
                        if (_receivedCloseFrame)
                        {
                            break;
                        }

                        // We've not yet processed a received close frame, which means we need to wait for a received close to complete.
                        // There may already be one in flight, in which case we want to just wait for that one rather than kicking off
                        // another (we don't support concurrent receive operations).  We need to kick off a new receive if either we've
                        // never issued a receive or if the last issued receive completed for reasons other than a close frame.  There is
                        // a race condition here, e.g. if there's a in-flight receive that completes after we check, but that's fine: worst
                        // case is we then await it, find that it's not what we need, and try again.
                        receiveTask = _lastReceiveAsync;
                        Task newReceiveTask = ValidateAndReceiveAsync(receiveTask, cancellationToken);
                        usingExistingReceive = ReferenceEquals(receiveTask, newReceiveTask);
                        _lastReceiveAsync = receiveTask = newReceiveTask;
                    }

                    // Wait for whatever receive task we have.  We'll then loop around again to re-check our state.
                    // If this is an existing receive, and if we have a cancelable token, we need to register with that
                    // token while we wait, since it may not be the same one that was given to the receive initially.
                    Debug.Assert(receiveTask != null);
                    using (usingExistingReceive ? cancellationToken.Register(static s => ((ManagedWebSocket)s!).Abort(), this) : default)
                    {
                        await receiveTask.ConfigureAwait(false);
                    }
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
                    Debug.Assert(count - 2 == encodedLength, $"GetByteCount and GetBytes encoded count didn't match");
                }

                ushort closeStatusValue = (ushort)closeStatus;
                buffer[0] = (byte)(closeStatusValue >> 8);
                buffer[1] = (byte)(closeStatusValue & 0xFF);

                await SendFrameAsync(MessageOpcode.Close, true, new Memory<byte>(buffer, 0, count), cancellationToken).ConfigureAwait(false);
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
                await _receiver.WaitForServerToCloseConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private void ThrowIfEOFUnexpected(bool throwOnPrematureClosure)
        {
            // The connection closed before we were able to read everything we needed.
            // If it was due to us being disposed, fail.  If it was due to the connection
            // being closed and it wasn't expected, fail.  If it was due to the connection
            // being closed and that was expected, exit gracefully.
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebSocket));
            }
            if (throwOnPrematureClosure)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
            }
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
                byte* maskPtr = (byte*)&mask;

                if (toMaskEnd - toMaskPtr >= sizeof(int))
                {
                    // align our pointer to sizeof(int)

                    while ((ulong)toMaskPtr % sizeof(int) != 0)
                    {
                        Debug.Assert(toMaskPtr < toMaskEnd);

                        *toMaskPtr++ ^= maskPtr[maskIndex];
                        maskIndex = (maskIndex + 1) & 3;
                    }

                    int rolledMask = (int)BitOperations.RotateRight((uint)mask, maskIndex * 8);

                    // use SIMD if possible.

                    if (Vector.IsHardwareAccelerated && Vector<byte>.Count % sizeof(int) == 0 && (toMaskEnd - toMaskPtr) >= Vector<byte>.Count)
                    {
                        // align our pointer to Vector<byte>.Count

                        while ((ulong)toMaskPtr % (uint)Vector<byte>.Count != 0)
                        {
                            Debug.Assert(toMaskPtr < toMaskEnd);

                            *(int*)toMaskPtr ^= rolledMask;
                            toMaskPtr += sizeof(int);
                        }

                        // use SIMD.

                        if (toMaskEnd - toMaskPtr >= Vector<byte>.Count)
                        {
                            Vector<byte> maskVector = Vector.AsVectorByte(new Vector<int>(rolledMask));

                            do
                            {
                                *(Vector<byte>*)toMaskPtr ^= maskVector;
                                toMaskPtr += Vector<byte>.Count;
                            }
                            while (toMaskEnd - toMaskPtr >= Vector<byte>.Count);
                        }
                    }

                    // process remaining data (or all, if couldn't use SIMD) 4 bytes at a time.

                    while (toMaskEnd - toMaskPtr >= sizeof(int))
                    {
                        *(int*)toMaskPtr ^= rolledMask;
                        toMaskPtr += sizeof(int);
                    }
                }

                // do any remaining data a byte at a time.

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

        private void ThrowOperationInProgress(string? methodName) => throw new InvalidOperationException(SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, methodName));

        /// <summary>Creates an OperationCanceledException instance, using a default message and the specified inner exception and token.</summary>
        private static Exception CreateOperationCanceledException(Exception innerException, CancellationToken cancellationToken = default(CancellationToken))
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
            internal int Mask;
            internal bool Compressed;
        }

        private readonly struct ControlMessage
        {
            internal ControlMessage(MessageOpcode opcode, ReadOnlyMemory<byte> payload)
            {
                Opcode = opcode;
                Payload = payload;
            }
            internal MessageOpcode Opcode { get; }
            internal ReadOnlyMemory<byte> Payload { get; }
        }

        /// <summary>
        /// Interface used by <see cref="ReceiveAsyncPrivate"/> to enable it to return
        /// different result types in an efficient manner.
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        private interface IWebSocketReceiveResultGetter<TResult>
        {
            TResult GetResult(int count, WebSocketMessageType messageType, bool endOfMessage, WebSocketCloseStatus? closeStatus, string? closeDescription);
        }

        /// <summary><see cref="IWebSocketReceiveResultGetter{TResult}"/> implementation for <see cref="WebSocketReceiveResult"/>.</summary>
        private readonly struct WebSocketReceiveResultGetter : IWebSocketReceiveResultGetter<WebSocketReceiveResult>
        {
            public WebSocketReceiveResult GetResult(int count, WebSocketMessageType messageType, bool endOfMessage, WebSocketCloseStatus? closeStatus, string? closeDescription) =>
                new WebSocketReceiveResult(count, messageType, endOfMessage, closeStatus, closeDescription);
        }
    }
}
