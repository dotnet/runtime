// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal abstract class WebSocketBase : WebSocket, IDisposable
    {
        private readonly OutstandingOperationHelper _closeOutstandingOperationHelper;
        private readonly OutstandingOperationHelper _closeOutputOutstandingOperationHelper;
        private readonly OutstandingOperationHelper _receiveOutstandingOperationHelper;
        private readonly OutstandingOperationHelper _sendOutstandingOperationHelper;
        private readonly Stream _innerStream;
        private readonly IWebSocketStream? _innerStreamAsWebSocketStream;
        private readonly string? _subProtocol;

        // We are not calling Dispose method on this object in Cleanup method to avoid a race condition while one thread is calling disposing on
        // this object and another one is still using WaitAsync. According to Dev11 358715, this should be fine as long as we are not accessing the
        // AvailableWaitHandle on this SemaphoreSlim object.
        private readonly SemaphoreSlim _sendFrameThrottle;
        // locking _ThisLock protects access to
        // - State
        // - _closeAsyncStartedReceive
        // - _closeReceivedTaskCompletionSource
        // - _closeNetworkConnectionTask
        private readonly object _thisLock;
        private readonly WebSocketBuffer _internalBuffer;
        private readonly KeepAliveTracker _keepAliveTracker;
        private volatile bool _cleanedUp;
        private volatile TaskCompletionSource? _closeReceivedTaskCompletionSource;
        private volatile Task? _closeOutputTask;
        private volatile bool _isDisposed;
        private volatile Task? _closeNetworkConnectionTask;
        private volatile bool _closeAsyncStartedReceive;
        private volatile WebSocketState _state;
        private volatile Task? _keepAliveTask;
        private volatile WebSocketOperation.ReceiveOperation? _receiveOperation;
        private volatile WebSocketOperation.SendOperation? _sendOperation;
        private volatile WebSocketOperation.SendOperation? _keepAliveOperation;
        private volatile WebSocketOperation.CloseOutputOperation? _closeOutputOperation;
        private Nullable<WebSocketCloseStatus> _closeStatus;
        private string? _closeStatusDescription;
        private int _receiveState;
        private Exception? _pendingException;

        protected WebSocketBase(Stream innerStream,
            string? subProtocol,
            TimeSpan keepAliveInterval,
            WebSocketBuffer internalBuffer)
        {
            Debug.Assert(internalBuffer != null, "'internalBuffer' MUST NOT be NULL.");
            HttpWebSocket.ValidateInnerStream(innerStream);
            HttpWebSocket.ValidateOptions(subProtocol, internalBuffer.ReceiveBufferSize,
                internalBuffer.SendBufferSize, keepAliveInterval);

            _thisLock = new object();

            _innerStream = innerStream;
            _internalBuffer = internalBuffer;
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Associate(this, _innerStream);
                NetEventSource.Associate(this, _internalBuffer);
            }

            _closeOutstandingOperationHelper = new OutstandingOperationHelper();
            _closeOutputOutstandingOperationHelper = new OutstandingOperationHelper();
            _receiveOutstandingOperationHelper = new OutstandingOperationHelper();
            _sendOutstandingOperationHelper = new OutstandingOperationHelper();
            _state = WebSocketState.Open;
            _subProtocol = subProtocol;
            _sendFrameThrottle = new SemaphoreSlim(1, 1);
            _closeStatus = null;
            _closeStatusDescription = null;
            _innerStreamAsWebSocketStream = innerStream as IWebSocketStream;
            _innerStreamAsWebSocketStream?.SwitchToOpaqueMode(this);
            _keepAliveTracker = KeepAliveTracker.Create(keepAliveInterval);
        }

        public override WebSocketState State
        {
            get
            {
                Debug.Assert(_state != WebSocketState.None, "'_state' MUST NOT be 'WebSocketState.None'.");
                return _state;
            }
        }

        public override string? SubProtocol
        {
            get
            {
                return _subProtocol;
            }
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                return _closeStatus;
            }
        }

        public override string? CloseStatusDescription
        {
            get
            {
                return _closeStatusDescription;
            }
        }

        internal WebSocketBuffer InternalBuffer
        {
            get
            {
                Debug.Assert(_internalBuffer != null, "'_internalBuffer' MUST NOT be NULL.");
                return _internalBuffer;
            }
        }

        protected void StartKeepAliveTimer()
        {
            _keepAliveTracker.StartTimer(this);
        }

        // locking SessionHandle protects access to
        // - WSPC (WebSocketProtocolComponent)
        // - _KeepAliveTask
        // - _closeOutputTask
        // - _LastSendActivity
        internal abstract SafeHandle SessionHandle { get; }

        // MultiThreading: ThreadSafe; At most one outstanding call to ReceiveAsync is allowed
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));
            return ReceiveAsyncCore(buffer, cancellationToken);
        }

        private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            WebSocketReceiveResult receiveResult;

            ThrowIfPendingException();
            ThrowIfDisposed();
            ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseSent);

            bool ownsCancellationTokenSource = false;
            CancellationToken linkedCancellationToken = CancellationToken.None;
            try
            {
                ownsCancellationTokenSource = _receiveOutstandingOperationHelper.TryStartOperation(cancellationToken,
                    out linkedCancellationToken);
                if (!ownsCancellationTokenSource)
                {
                    lock (_thisLock)
                    {
                        if (_closeAsyncStartedReceive)
                        {
                            throw new InvalidOperationException(
                                SR.Format(SR.net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync, nameof(CloseAsync), nameof(CloseOutputAsync)));
                        }

                        throw new InvalidOperationException(
                            SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, nameof(ReceiveAsync)));
                    }
                }

                EnsureReceiveOperation();
                receiveResult = (await _receiveOperation!.Process(buffer, linkedCancellationToken).SuppressContextFlow())!;

                if (NetEventSource.Log.IsEnabled() && receiveResult.Count > 0)
                {
                    NetEventSource.DumpBuffer(this, buffer.Array!, buffer.Offset, receiveResult.Count);
                }
            }
            catch (Exception exception)
            {
                bool aborted = linkedCancellationToken.IsCancellationRequested;
                Abort();
                ThrowIfConvertibleException(nameof(ReceiveAsync), exception, cancellationToken, aborted);
                throw;
            }
            finally
            {
                _receiveOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
            }

            return receiveResult;
        }

        // MultiThreading: ThreadSafe; At most one outstanding call to SendAsync is allowed
        public override Task SendAsync(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            if (messageType != WebSocketMessageType.Binary &&
                    messageType != WebSocketMessageType.Text)
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_Argument_InvalidMessageType,
                    messageType,
                    nameof(SendAsync),
                    WebSocketMessageType.Binary,
                    WebSocketMessageType.Text,
                    nameof(CloseOutputAsync)),
                    nameof(messageType));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
        }

        private async Task SendAsyncCore(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            Debug.Assert(messageType == WebSocketMessageType.Binary || messageType == WebSocketMessageType.Text,
                "'messageType' MUST be either 'WebSocketMessageType.Binary' or 'WebSocketMessageType.Text'.");
            Debug.Assert(buffer.Array != null);

            ThrowIfPendingException();
            ThrowIfDisposed();
            ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
            bool ownsCancellationTokenSource = false;
            CancellationToken linkedCancellationToken = CancellationToken.None;

            try
            {
                while (!(ownsCancellationTokenSource = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken)))
                {
                    Task? keepAliveTask;

                    lock (SessionHandle)
                    {
                        keepAliveTask = _keepAliveTask;

                        if (keepAliveTask == null)
                        {
                            // Check whether there is still another outstanding send operation
                            // Potentially the keepAlive operation has completed before this thread
                            // was able to enter the SessionHandle-lock.
                            _sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                            if (ownsCancellationTokenSource = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken))
                            {
                                break;
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, nameof(SendAsync)));
                            }
                        }
                    }

                    await keepAliveTask.SuppressContextFlow();
                    ThrowIfPendingException();

                    _sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                }

                if (NetEventSource.Log.IsEnabled() && buffer.Count > 0)
                {
                    NetEventSource.DumpBuffer(this, buffer.Array, buffer.Offset, buffer.Count);
                }

                EnsureSendOperation();
                _sendOperation!.BufferType = GetBufferType(messageType, endOfMessage);
                await _sendOperation.Process(buffer, linkedCancellationToken).SuppressContextFlow();
            }
            catch (Exception exception)
            {
                bool aborted = linkedCancellationToken.IsCancellationRequested;
                Abort();
                ThrowIfConvertibleException(nameof(SendAsync), exception, cancellationToken, aborted);
                throw;
            }
            finally
            {
                _sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
            }
        }

        private async Task SendFrameAsync(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
        {
            bool sendFrameLockTaken = false;
            try
            {
                await _sendFrameThrottle.WaitAsync(cancellationToken).SuppressContextFlow();
                sendFrameLockTaken = true;

                if (sendBuffers.Count > 1 &&
                    _innerStreamAsWebSocketStream != null &&
                    _innerStreamAsWebSocketStream.SupportsMultipleWrite)
                {
                    await _innerStreamAsWebSocketStream.MultipleWriteAsync(sendBuffers,
                        cancellationToken).SuppressContextFlow();
                }
                else
                {
                    foreach (ArraySegment<byte> buffer in sendBuffers)
                    {
                        await _innerStream.WriteAsync(buffer.Array!,
                            buffer.Offset,
                            buffer.Count,
                            cancellationToken).SuppressContextFlow();
                    }
                }
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, objectDisposedException);
            }
            catch (NotSupportedException notSupportedException)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, notSupportedException);
            }
            finally
            {
                if (sendFrameLockTaken)
                {
                    _sendFrameThrottle.Release();
                }
            }
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override void Abort()
        {
            bool thisLockTaken = false;
            bool sessionHandleLockTaken = false;
            try
            {
                if (IsStateTerminal(State))
                {
                    return;
                }

                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                if (IsStateTerminal(State))
                {
                    return;
                }

                _state = WebSocketState.Aborted;

                // Abort any outstanding IO operations.
                if (SessionHandle != null && !SessionHandle.IsClosed && !SessionHandle.IsInvalid)
                {
                    WebSocketProtocolComponent.WebSocketAbortHandle(SessionHandle);
                }

                _receiveOutstandingOperationHelper.CancelIO();
                _sendOutstandingOperationHelper.CancelIO();
                _closeOutputOutstandingOperationHelper.CancelIO();
                _closeOutstandingOperationHelper.CancelIO();
                _innerStreamAsWebSocketStream?.Abort();
                CleanUp();
            }
            finally
            {
                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
            }
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            return CloseOutputAsyncCore(closeStatus, statusDescription!, cancellationToken);
        }

        private async Task CloseOutputAsyncCore(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfPendingException();
            if (IsStateTerminal(State))
            {
                return;
            }
            ThrowIfDisposed();

            bool thisLockTaken = false;
            bool sessionHandleLockTaken = false;
            bool needToCompleteSendOperation = false;
            bool ownsCloseOutputCancellationTokenSource = false;
            bool ownsSendCancellationTokenSource = false;
            CancellationToken linkedCancellationToken = CancellationToken.None;
            try
            {
                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                ThrowIfPendingException();
                ThrowIfDisposed();

                if (IsStateTerminal(State))
                {
                    return;
                }

                ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
                ownsCloseOutputCancellationTokenSource = _closeOutputOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                if (!ownsCloseOutputCancellationTokenSource)
                {
                    Task? closeOutputTask = _closeOutputTask;

                    if (closeOutputTask != null)
                    {
                        ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                        await closeOutputTask.SuppressContextFlow();
                        TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                    }
                }
                else
                {
                    needToCompleteSendOperation = true;
                    while (!(ownsSendCancellationTokenSource =
                        _sendOutstandingOperationHelper.TryStartOperation(cancellationToken,
                            out linkedCancellationToken)))
                    {
                        if (_keepAliveTask != null)
                        {
                            Task keepAliveTask = _keepAliveTask;

                            ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                            await keepAliveTask.SuppressContextFlow();
                            TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);

                            ThrowIfPendingException();
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, nameof(SendAsync)));
                        }

                        _sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
                    }

                    EnsureCloseOutputOperation();
                    _closeOutputOperation!.CloseStatus = closeStatus;
                    _closeOutputOperation!.CloseReason = statusDescription;
                    _closeOutputTask = _closeOutputOperation!.Process(null, linkedCancellationToken);

                    ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                    await _closeOutputTask.SuppressContextFlow();
                    TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);

                    if (OnCloseOutputCompleted())
                    {
                        bool callCompleteOnCloseCompleted = false;

                        try
                        {
                            callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                thisLockTaken, sessionHandleLockTaken, linkedCancellationToken).SuppressContextFlow();
                        }
                        catch (Exception)
                        {
                            // If an exception is thrown we know that the locks have been released,
                            // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                            ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                            throw;
                        }

                        if (callCompleteOnCloseCompleted)
                        {
                            ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                            FinishOnCloseCompleted();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                bool aborted = linkedCancellationToken.IsCancellationRequested;
                Abort();
                ThrowIfConvertibleException(nameof(CloseOutputAsync), exception, cancellationToken, aborted);
                throw;
            }
            finally
            {
                _closeOutputOutstandingOperationHelper.CompleteOperation(ownsCloseOutputCancellationTokenSource);

                if (needToCompleteSendOperation)
                {
                    _sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
                }

                _closeOutputTask = null;
                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
            }
        }

        // returns TRUE if the caller should also call StartOnCloseCompleted
        private bool OnCloseOutputCompleted()
        {
            if (IsStateTerminal(State))
            {
                return false;
            }

            switch (State)
            {
                case WebSocketState.Open:
                    _state = WebSocketState.CloseSent;
                    return false;
                case WebSocketState.CloseReceived:
                    return true;
                default:
                    return false;
            }
        }

        // MultiThreading: This method has to be called under a _ThisLock-lock
        // ReturnValue: This method returns true only if CompleteOnCloseCompleted needs to be called
        // If this method returns true all locks were released before starting the IO operation
        // and they have to be retaken by the caller before calling CompleteOnCloseCompleted
        // Exception handling: If an exception is thrown from await StartOnCloseCompleted
        // it always means the locks have been released already - so the caller has to retake the
        // locks in the catch-block.
        // This is ensured by enforcing a Task.Yield for IWebSocketStream.CloseNetowrkConnectionAsync
        private async Task<bool> StartOnCloseCompleted(bool thisLockTakenSnapshot,
            bool sessionHandleLockTakenSnapshot,
            CancellationToken cancellationToken)
        {
            Debug.Assert(thisLockTakenSnapshot, "'thisLockTakenSnapshot' MUST be 'true' at this point.");

            if (IsStateTerminal(_state))
            {
                return false;
            }

            _state = WebSocketState.Closed;

            if (_innerStreamAsWebSocketStream != null)
            {
                bool thisLockTaken = thisLockTakenSnapshot;
                bool sessionHandleLockTaken = sessionHandleLockTakenSnapshot;

                try
                {
                    _closeNetworkConnectionTask ??=
                        _innerStreamAsWebSocketStream.CloseNetworkConnectionAsync(cancellationToken);

                    if (thisLockTaken && sessionHandleLockTaken)
                    {
                        ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                    }
                    else if (thisLockTaken)
                    {
                        ReleaseLock(_thisLock, ref thisLockTaken);
                    }

                    await _closeNetworkConnectionTask.SuppressContextFlow();
                }
                catch (Exception closeNetworkConnectionTaskException)
                {
                    if (!CanHandleExceptionDuringClose(closeNetworkConnectionTaskException))
                    {
                        ThrowIfConvertibleException(nameof(StartOnCloseCompleted),
                            closeNetworkConnectionTaskException,
                            cancellationToken,
                            cancellationToken.IsCancellationRequested);
                        throw;
                    }
                }
            }

            return true;
        }

        // MultiThreading: This method has to be called under a thisLock-lock
        private void FinishOnCloseCompleted()
        {
            CleanUp();
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override Task CloseAsync(WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            return CloseAsyncCore(closeStatus, statusDescription, cancellationToken);
        }

        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfPendingException();
            if (IsStateTerminal(State))
            {
                return;
            }
            ThrowIfDisposed();

            bool lockTaken = false;
            Monitor.Enter(_thisLock, ref lockTaken);
            bool ownsCloseCancellationTokenSource = false;
            CancellationToken linkedCancellationToken = CancellationToken.None;
            try
            {
                ThrowIfPendingException();
                if (IsStateTerminal(State))
                {
                    return;
                }
                ThrowIfDisposed();
                ThrowOnInvalidState(State,
                    WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent);

                Task? closeOutputTask;
                ownsCloseCancellationTokenSource = _closeOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                if (ownsCloseCancellationTokenSource)
                {
                    closeOutputTask = _closeOutputTask;
                    if (closeOutputTask == null && State != WebSocketState.CloseSent)
                    {
                        _closeReceivedTaskCompletionSource ??= new TaskCompletionSource();

                        closeOutputTask = CloseOutputAsync(closeStatus,
                            statusDescription,
                            linkedCancellationToken);
                    }
                }
                else
                {
                    Debug.Assert(_closeReceivedTaskCompletionSource != null, "'_closeReceivedTaskCompletionSource' MUST NOT be NULL.");
                    closeOutputTask = _closeReceivedTaskCompletionSource.Task;
                }

                if (closeOutputTask != null)
                {
                    ReleaseLock(_thisLock, ref lockTaken);
                    try
                    {
                        await closeOutputTask.SuppressContextFlow();
                    }
                    catch (Exception closeOutputError)
                    {
                        Monitor.Enter(_thisLock, ref lockTaken);

                        if (!CanHandleExceptionDuringClose(closeOutputError))
                        {
                            ThrowIfConvertibleException(nameof(CloseOutputAsync),
                                closeOutputError,
                                cancellationToken,
                                linkedCancellationToken.IsCancellationRequested);
                            throw;
                        }
                    }

                    // When closeOutputTask != null  and an exception thrown from await closeOutputTask is handled,
                    // the lock will be taken in the catch-block. So the logic here avoids taking the lock twice.
                    if (!lockTaken)
                    {
                        Monitor.Enter(_thisLock, ref lockTaken);
                    }
                }

                if (OnCloseOutputCompleted())
                {
                    bool callCompleteOnCloseCompleted = false;

                    try
                    {
                        // linkedCancellationToken can be CancellationToken.None if ownsCloseCancellationTokenSource==false
                        // This is still ok because OnCloseOutputCompleted won't start any IO operation in this case
                        callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                            lockTaken, false, linkedCancellationToken).SuppressContextFlow();
                    }
                    catch (Exception)
                    {
                        // If an exception is thrown we know that the locks have been released,
                        // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                        ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                        throw;
                    }

                    if (callCompleteOnCloseCompleted)
                    {
                        ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                        FinishOnCloseCompleted();
                    }
                }

                if (IsStateTerminal(State))
                {
                    return;
                }

                linkedCancellationToken = CancellationToken.None;

                bool ownsReceiveCancellationTokenSource = _receiveOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                if (ownsReceiveCancellationTokenSource)
                {
                    _closeAsyncStartedReceive = true;
                    ArraySegment<byte> closeMessageBuffer =
                        new ArraySegment<byte>(new byte[HttpWebSocket.MinReceiveBufferSize]);
                    EnsureReceiveOperation();
                    Task<WebSocketReceiveResult?> receiveAsyncTask = _receiveOperation!.Process(closeMessageBuffer,
                        linkedCancellationToken);
                    ReleaseLock(_thisLock, ref lockTaken);

                    WebSocketReceiveResult? receiveResult = null;
                    try
                    {
                        receiveResult = await receiveAsyncTask.SuppressContextFlow();
                    }
                    catch (Exception receiveException)
                    {
                        Monitor.Enter(_thisLock, ref lockTaken);

                        if (!CanHandleExceptionDuringClose(receiveException))
                        {
                            ThrowIfConvertibleException(nameof(CloseAsync),
                                receiveException,
                                cancellationToken,
                                linkedCancellationToken.IsCancellationRequested);
                            throw;
                        }
                    }

                    // receiveResult is NEVER NULL if WebSocketBase.ReceiveOperation.Process completes successfully
                    // - but in the close code path we handle some exception if another thread was able to tranistion
                    // the state into Closed successfully. In this case receiveResult can be NULL and it is safe to
                    // skip the statements in the if-block.
                    if (receiveResult != null)
                    {
                        if (NetEventSource.Log.IsEnabled() && receiveResult.Count > 0)
                        {
                            NetEventSource.DumpBuffer(this, closeMessageBuffer.Array!, closeMessageBuffer.Offset, receiveResult.Count);
                        }

                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                        {
                            throw new WebSocketException(WebSocketError.InvalidMessageType,
                                SR.Format(SR.net_WebSockets_InvalidMessageType,
                                    nameof(WebSocket) + "." + nameof(CloseAsync),
                                    nameof(WebSocket) + "." + nameof(CloseOutputAsync),
                                    receiveResult.MessageType));
                        }
                    }
                }
                else
                {
                    _receiveOutstandingOperationHelper.CompleteOperation(ownsReceiveCancellationTokenSource);
                    ReleaseLock(_thisLock, ref lockTaken);
                    await _closeReceivedTaskCompletionSource!.Task.SuppressContextFlow();
                }

                // When ownsReceiveCancellationTokenSource is true and an exception is thrown, the lock will be taken.
                // So this logic here is to avoid taking the lock twice.
                if (!lockTaken)
                {
                    Monitor.Enter(_thisLock, ref lockTaken);
                }

                if (!IsStateTerminal(State))
                {
                    bool ownsSendCancellationSource = false;
                    try
                    {
                        // We know that the CloseFrame has been sent at this point. So no Send-operation is allowed anymore and we
                        // can hijack the _SendOutstandingOperationHelper to create a linkedCancellationToken
                        ownsSendCancellationSource = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                        Debug.Assert(ownsSendCancellationSource, "'ownsSendCancellationSource' MUST be 'true' at this point.");

                        bool callCompleteOnCloseCompleted = false;

                        try
                        {
                            // linkedCancellationToken can be CancellationToken.None if ownsCloseCancellationTokenSource==false
                            // This is still ok because OnCloseOutputCompleted won't start any IO operation in this case
                            callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                lockTaken, false, linkedCancellationToken).SuppressContextFlow();
                        }
                        catch (Exception)
                        {
                            // If an exception is thrown we know that the locks have been released,
                            // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                            ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                            throw;
                        }

                        if (callCompleteOnCloseCompleted)
                        {
                            ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                            FinishOnCloseCompleted();
                        }
                    }
                    finally
                    {
                        _sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationSource);
                    }
                }
            }
            catch (Exception exception)
            {
                bool aborted = linkedCancellationToken.IsCancellationRequested;
                Abort();
                ThrowIfConvertibleException(nameof(CloseAsync), exception, cancellationToken, aborted);
                throw;
            }
            finally
            {
                _closeOutstandingOperationHelper.CompleteOperation(ownsCloseCancellationTokenSource);
                ReleaseLock(_thisLock, ref lockTaken);
            }
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            bool thisLockTaken = false;
            bool sessionHandleLockTaken = false;

            try
            {
                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);

                if (_isDisposed)
                {
                    return;
                }

                if (!IsStateTerminal(State))
                {
                    Abort();
                }
                else
                {
                    CleanUp();
                }

                _isDisposed = true;
            }
            finally
            {
                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
            }
        }

        private static void ResetFlagAndTakeLock(object lockObject, ref bool thisLockTaken)
        {
            Debug.Assert(lockObject != null, "'lockObject' MUST NOT be NULL.");
            thisLockTaken = false;
            Monitor.Enter(lockObject, ref thisLockTaken);
        }

        private void ResetFlagsAndTakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            thisLockTaken = false;
            sessionHandleLockTaken = false;
            TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
        }

        private void TakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            Debug.Assert(_thisLock != null, "'_thisLock' MUST NOT be NULL.");
            Debug.Assert(SessionHandle != null, "'SessionHandle' MUST NOT be NULL.");

            Monitor.Enter(SessionHandle, ref sessionHandleLockTaken);
            Monitor.Enter(_thisLock, ref thisLockTaken);
        }

        private void ReleaseLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            Debug.Assert(_thisLock != null, "'_thisLock' MUST NOT be NULL.");
            Debug.Assert(SessionHandle != null, "'SessionHandle' MUST NOT be NULL.");

            if (thisLockTaken)
            {
                Monitor.Exit(_thisLock);
                thisLockTaken = false;
            }

            if (sessionHandleLockTaken)
            {
                Monitor.Exit(SessionHandle);
                sessionHandleLockTaken = false;
            }
        }

        private void EnsureReceiveOperation()
        {
            if (_receiveOperation == null)
            {
                lock (_thisLock)
                {
                    _receiveOperation ??= new WebSocketOperation.ReceiveOperation(this);
                }
            }
        }

        private void EnsureSendOperation()
        {
            if (_sendOperation == null)
            {
                lock (_thisLock)
                {
                    _sendOperation ??= new WebSocketOperation.SendOperation(this);
                }
            }
        }

        private void EnsureKeepAliveOperation()
        {
            if (_keepAliveOperation == null)
            {
                lock (_thisLock)
                {
                    if (_keepAliveOperation == null)
                    {
                        WebSocketOperation.SendOperation keepAliveOperation = new WebSocketOperation.SendOperation(this);
                        keepAliveOperation.BufferType = WebSocketProtocolComponent.BufferType.UnsolicitedPong;
                        _keepAliveOperation = keepAliveOperation;
                    }
                }
            }
        }

        private void EnsureCloseOutputOperation()
        {
            if (_closeOutputOperation == null)
            {
                lock (_thisLock)
                {
                    _closeOutputOperation ??= new WebSocketOperation.CloseOutputOperation(this);
                }
            }
        }

        private static void ReleaseLock(object lockObject, ref bool lockTaken)
        {
            Debug.Assert(lockObject != null, "'lockObject' MUST NOT be NULL.");
            if (lockTaken)
            {
                Monitor.Exit(lockObject);
                lockTaken = false;
            }
        }

        private static WebSocketProtocolComponent.BufferType GetBufferType(WebSocketMessageType messageType,
            bool endOfMessage)
        {
            Debug.Assert(messageType == WebSocketMessageType.Binary || messageType == WebSocketMessageType.Text,
                $"The value of 'messageType' ({messageType}) is invalid. Valid message types: '{WebSocketMessageType.Binary}, {WebSocketMessageType.Text}'");

            if (messageType == WebSocketMessageType.Text)
            {
                if (endOfMessage)
                {
                    return WebSocketProtocolComponent.BufferType.UTF8Message;
                }

                return WebSocketProtocolComponent.BufferType.UTF8Fragment;
            }
            else
            {
                if (endOfMessage)
                {
                    return WebSocketProtocolComponent.BufferType.BinaryMessage;
                }

                return WebSocketProtocolComponent.BufferType.BinaryFragment;
            }
        }

        private static WebSocketMessageType GetMessageType(WebSocketProtocolComponent.BufferType bufferType)
        {
            switch (bufferType)
            {
                case WebSocketProtocolComponent.BufferType.Close:
                    return WebSocketMessageType.Close;
                case WebSocketProtocolComponent.BufferType.BinaryFragment:
                case WebSocketProtocolComponent.BufferType.BinaryMessage:
                    return WebSocketMessageType.Binary;
                case WebSocketProtocolComponent.BufferType.UTF8Fragment:
                case WebSocketProtocolComponent.BufferType.UTF8Message:
                    return WebSocketMessageType.Text;
                default:
                    // This indicates a contract violation of the websocket protocol component,
                    // because we currently don't support any WebSocket extensions and would
                    // not accept a Websocket handshake requesting extensions
                    Debug.Fail($"The value of 'bufferType' ({bufferType}) is invalid.");
                    throw new WebSocketException(WebSocketError.NativeError,
                        SR.Format(SR.net_WebSockets_InvalidBufferType,
                            bufferType,
                            WebSocketProtocolComponent.BufferType.Close,
                            WebSocketProtocolComponent.BufferType.BinaryFragment,
                            WebSocketProtocolComponent.BufferType.BinaryMessage,
                            WebSocketProtocolComponent.BufferType.UTF8Fragment,
                            WebSocketProtocolComponent.BufferType.UTF8Message));
            }
        }

        internal void ValidateNativeBuffers(WebSocketProtocolComponent.Action action,
            WebSocketProtocolComponent.BufferType bufferType,
            Interop.WebSocket.Buffer[] dataBuffers,
            uint dataBufferCount)
        {
            _internalBuffer.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);
        }

        private void ThrowIfAborted(bool aborted, Exception innerException)
        {
            if (aborted)
            {
                throw new WebSocketException(WebSocketError.InvalidState,
                    SR.Format(SR.net_WebSockets_InvalidState_ClosedOrAborted, GetType().FullName, WebSocketState.Aborted),
                    innerException);
            }
        }

        private bool CanHandleExceptionDuringClose(Exception error)
        {
            Debug.Assert(error != null, "'error' MUST NOT be NULL.");

            if (State != WebSocketState.Closed)
            {
                return false;
            }

            return error is OperationCanceledException ||
                error is WebSocketException ||
                error is SocketException ||
                error is HttpListenerException ||
                error is IOException;
        }

        // We only want to throw an OperationCanceledException if the CancellationToken passed
        // down from the caller is canceled - not when Abort is called on another thread and
        // the linkedCancellationToken is canceled.
        private void ThrowIfConvertibleException(string? methodName,
            Exception exception,
            CancellationToken cancellationToken,
            bool aborted)
        {
            Debug.Assert(exception != null, "'exception' MUST NOT be NULL.");

            if (NetEventSource.Log.IsEnabled() && !string.IsNullOrEmpty(methodName))
            {
                NetEventSource.Error(this, $"methodName: {methodName}, exception: {exception}");
            }

            OperationCanceledException? operationCanceledException = exception as OperationCanceledException;
            if (operationCanceledException != null)
            {
                if (cancellationToken.IsCancellationRequested ||
                    !aborted)
                {
                    return;
                }
                ThrowIfAborted(aborted, exception);
            }

            WebSocketException? convertedException = exception as WebSocketException;
            if (convertedException != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfAborted(aborted, convertedException);
                return;
            }

            SocketException? socketException = exception as SocketException;
            if (socketException != null)
            {
                convertedException = new WebSocketException(socketException.NativeErrorCode, socketException);
            }

            HttpListenerException? httpListenerException = exception as HttpListenerException;
            if (httpListenerException != null)
            {
                convertedException = new WebSocketException(httpListenerException.ErrorCode, httpListenerException);
            }

            IOException? ioException = exception as IOException;
            if (ioException != null)
            {
                socketException = exception.InnerException as SocketException;
                if (socketException != null)
                {
                    convertedException = new WebSocketException(socketException.NativeErrorCode, ioException);
                }
            }

            if (convertedException != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfAborted(aborted, convertedException);
                throw convertedException;
            }

            AggregateException? aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                // Collapse possibly nested graph into a flat list.
                // Empty inner exception list is unlikely but possible via public api.
                ReadOnlyCollection<Exception> unwrappedExceptions = aggregateException.Flatten().InnerExceptions;
                if (unwrappedExceptions.Count == 0)
                {
                    return;
                }

                foreach (Exception unwrappedException in unwrappedExceptions)
                {
                    ThrowIfConvertibleException(null, unwrappedException, cancellationToken, aborted);
                }
            }
        }

        private void CleanUp()
        {
            // Multithreading: This method is always called under the _ThisLock lock
            if (_cleanedUp)
            {
                return;
            }

            _cleanedUp = true;

            SessionHandle?.Dispose();
            _internalBuffer?.Dispose(this.State);
            _receiveOutstandingOperationHelper?.Dispose();
            _sendOutstandingOperationHelper?.Dispose();
            _closeOutputOutstandingOperationHelper?.Dispose();
            _closeOutstandingOperationHelper?.Dispose();

            if (_innerStream != null)
            {
                try
                {
                    _innerStream.Close();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (IOException)
                {
                }
                catch (SocketException)
                {
                }
                catch (HttpListenerException)
                {
                }
            }

            _keepAliveTracker.Dispose();
        }

        private void OnBackgroundTaskException(Exception exception)
        {
            if (Interlocked.CompareExchange<Exception>(ref _pendingException!, exception, null!) == null)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(this, exception.ToString());
                }
                Abort();
            }
        }

        private void ThrowIfPendingException()
        {
            Exception pendingException = Interlocked.Exchange<Exception>(ref _pendingException!, null!);
            if (pendingException != null)
            {
                throw new WebSocketException(WebSocketError.Faulted, pendingException);
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }

        private void UpdateReceiveState(int newReceiveState, int expectedReceiveState)
        {
            int receiveState;
            if ((receiveState = Interlocked.Exchange(ref _receiveState, newReceiveState)) != expectedReceiveState)
            {
                Debug.Fail($"'_receiveState' had an invalid value '{receiveState}'. The expected value was '{expectedReceiveState}'.");
            }
        }

        private bool StartOnCloseReceived(ref bool thisLockTaken)
        {
            ThrowIfDisposed();

            if (IsStateTerminal(State) || State == WebSocketState.CloseReceived)
            {
                return false;
            }

            Monitor.Enter(_thisLock, ref thisLockTaken);
            if (IsStateTerminal(State) || State == WebSocketState.CloseReceived)
            {
                return false;
            }

            if (State == WebSocketState.Open)
            {
                _state = WebSocketState.CloseReceived;
                _closeReceivedTaskCompletionSource ??= new TaskCompletionSource();

                return false;
            }

            return true;
        }

        private void FinishOnCloseReceived(WebSocketCloseStatus closeStatus,
            string? closeStatusDescription)
        {
            _closeReceivedTaskCompletionSource?.TrySetResult();

            _closeStatus = closeStatus;
            _closeStatusDescription = closeStatusDescription;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"closeStatus: {closeStatus}, closeStatusDescription: {closeStatusDescription}, _State: {_state}");
        }

        private static async void OnKeepAlive(object? sender)
        {
            Debug.Assert(sender != null, "'sender' MUST NOT be NULL.");
            Debug.Assert((sender as WebSocketBase) != null, "'sender as WebSocketBase' MUST NOT be NULL.");

            WebSocketBase? thisPtr = (sender as WebSocketBase)!;
            bool lockTaken = false;

            CancellationToken linkedCancellationToken = CancellationToken.None;
            try
            {
                Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);

                if (thisPtr._isDisposed ||
                    thisPtr._state != WebSocketState.Open ||
                    thisPtr._closeOutputTask != null)
                {
                    return;
                }

                if (thisPtr._keepAliveTracker.ShouldSendKeepAlive())
                {
                    bool ownsCancellationTokenSource = false;
                    try
                    {
                        ownsCancellationTokenSource = thisPtr._sendOutstandingOperationHelper.TryStartOperation(CancellationToken.None, out linkedCancellationToken);
                        if (ownsCancellationTokenSource)
                        {
                            thisPtr.EnsureKeepAliveOperation();
                            thisPtr._keepAliveTask = thisPtr._keepAliveOperation!.Process(null, linkedCancellationToken);
                            ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
                            await thisPtr._keepAliveTask!.SuppressContextFlow();
                        }
                    }
                    finally
                    {
                        if (!lockTaken)
                        {
                            Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);
                        }
                        thisPtr._sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                        thisPtr._keepAliveTask = null;
                    }

                    thisPtr._keepAliveTracker.ResetTimer();
                }
            }
            catch (Exception exception)
            {
                try
                {
                    thisPtr.ThrowIfConvertibleException(nameof(OnKeepAlive),
                        exception,
                        CancellationToken.None,
                        linkedCancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception backgroundException)
                {
                    thisPtr.OnBackgroundTaskException(backgroundException);
                }
            }
            finally
            {
                ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
            }
        }

        private abstract class WebSocketOperation
        {
            protected bool AsyncOperationCompleted { get; set; }
            private readonly WebSocketBase _webSocket;

            internal WebSocketOperation(WebSocketBase webSocket)
            {
                Debug.Assert(webSocket != null, "'webSocket' MUST NOT be NULL.");
                _webSocket = webSocket;
                AsyncOperationCompleted = false;
            }

            public WebSocketReceiveResult? ReceiveResult { get; protected set; }
            protected abstract int BufferCount { get; }
            protected abstract WebSocketProtocolComponent.ActionQueue ActionQueue { get; }
            protected abstract void Initialize(Nullable<ArraySegment<byte>> buffer, CancellationToken cancellationToken);
            protected abstract bool ShouldContinue(CancellationToken cancellationToken);

            // Multi-Threading: This method has to be called under a SessionHandle-lock. It returns true if a
            // close frame was received. Handling the received close frame might involve IO - to make the locking
            // strategy easier and reduce one level in the await-hierarchy the IO is kicked off by the caller.
            protected abstract bool ProcessAction_NoAction();

            protected virtual void ProcessAction_IndicateReceiveComplete(
                Nullable<ArraySegment<byte>> buffer,
                WebSocketProtocolComponent.BufferType bufferType,
                WebSocketProtocolComponent.Action action,
                Interop.WebSocket.Buffer[] dataBuffers,
                uint dataBufferCount,
                IntPtr actionContext)
            {
                throw new NotImplementedException();
            }

            protected abstract void Cleanup();

            internal async Task<WebSocketReceiveResult?> Process(Nullable<ArraySegment<byte>> buffer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(BufferCount >= 1 && BufferCount <= 2, "'bufferCount' MUST ONLY BE '1' or '2'.");

                bool sessionHandleLockTaken = false;
                AsyncOperationCompleted = false;
                ReceiveResult = null;
                try
                {
                    Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                    _webSocket.ThrowIfPendingException();
                    Initialize(buffer, cancellationToken);

                    while (ShouldContinue(cancellationToken))
                    {
                        WebSocketProtocolComponent.Action action;
                        WebSocketProtocolComponent.BufferType bufferType;

                        bool completed = false;

                        while (!completed)
                        {
                            Interop.WebSocket.Buffer[] dataBuffers =
                                new Interop.WebSocket.Buffer[BufferCount];
                            uint dataBufferCount = (uint)BufferCount;
                            IntPtr actionContext;

                            _webSocket.ThrowIfDisposed();
                            WebSocketProtocolComponent.WebSocketGetAction(_webSocket,
                                ActionQueue,
                                dataBuffers,
                                ref dataBufferCount,
                                out action,
                                out bufferType,
                                out actionContext);

                            switch (action)
                            {
                                case WebSocketProtocolComponent.Action.NoAction:
                                    if (ProcessAction_NoAction())
                                    {
                                        // A close frame was received

                                        Debug.Assert(ReceiveResult!.Count == 0, "'receiveResult.Count' MUST be 0.");
                                        Debug.Assert(ReceiveResult.CloseStatus != null, "'receiveResult.CloseStatus' MUST NOT be NULL for message type 'Close'.");
                                        bool thisLockTaken = false;
                                        try
                                        {
                                            if (_webSocket.StartOnCloseReceived(ref thisLockTaken))
                                            {
                                                // If StartOnCloseReceived returns true the WebSocket close handshake has been completed
                                                // so there is no need to retake the SessionHandle-lock.
                                                // _ThisLock lock is guaranteed to be taken by StartOnCloseReceived when returning true
                                                ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                                bool callCompleteOnCloseCompleted = false;

                                                try
                                                {
                                                    callCompleteOnCloseCompleted = await _webSocket.StartOnCloseCompleted(
                                                        thisLockTaken, sessionHandleLockTaken, cancellationToken).SuppressContextFlow();
                                                }
                                                catch (Exception)
                                                {
                                                    // If an exception is thrown we know that the locks have been released,
                                                    // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                                                    ResetFlagAndTakeLock(_webSocket._thisLock, ref thisLockTaken);
                                                    throw;
                                                }

                                                if (callCompleteOnCloseCompleted)
                                                {
                                                    ResetFlagAndTakeLock(_webSocket._thisLock, ref thisLockTaken);
                                                    _webSocket.FinishOnCloseCompleted();
                                                }
                                            }
                                            _webSocket.FinishOnCloseReceived(ReceiveResult.CloseStatus.Value, ReceiveResult.CloseStatusDescription);
                                        }
                                        finally
                                        {
                                            if (thisLockTaken)
                                            {
                                                ReleaseLock(_webSocket._thisLock, ref thisLockTaken);
                                            }
                                        }
                                    }
                                    completed = true;
                                    break;
                                case WebSocketProtocolComponent.Action.IndicateReceiveComplete:
                                    ProcessAction_IndicateReceiveComplete(buffer,
                                        bufferType,
                                        action,
                                        dataBuffers,
                                        dataBufferCount,
                                        actionContext);
                                    break;
                                case WebSocketProtocolComponent.Action.ReceiveFromNetwork:
                                    int count = 0;
                                    try
                                    {
                                        ArraySegment<byte> payload = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);

                                        ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                        HttpWebSocket.ThrowIfConnectionAborted(_webSocket._innerStream, true);
                                        try
                                        {
                                            Task<int> readTask = _webSocket._innerStream.ReadAsync(payload.Array!,
                                                payload.Offset,
                                                payload.Count,
                                                cancellationToken);
                                            count = await readTask.SuppressContextFlow();
                                            _webSocket._keepAliveTracker.OnDataReceived();
                                        }
                                        catch (ObjectDisposedException objectDisposedException)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, objectDisposedException);
                                        }
                                        catch (NotSupportedException notSupportedException)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, notSupportedException);
                                        }
                                        Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                        _webSocket.ThrowIfPendingException();
                                        // If the client unexpectedly closed the socket we throw an exception as we didn't get any close message
                                        if (count == 0)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                                        }
                                    }
                                    finally
                                    {
                                        WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                                            actionContext,
                                            count);
                                    }
                                    break;
                                case WebSocketProtocolComponent.Action.IndicateSendComplete:
                                    WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket, actionContext, 0);
                                    AsyncOperationCompleted = true;
                                    ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                    await _webSocket._innerStream.FlushAsync(cancellationToken).SuppressContextFlow();
                                    Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                    break;
                                case WebSocketProtocolComponent.Action.SendToNetwork:
                                    int bytesSent = 0;
                                    try
                                    {
                                        if (_webSocket.State != WebSocketState.CloseSent ||
                                            (bufferType != WebSocketProtocolComponent.BufferType.PingPong &&
                                            bufferType != WebSocketProtocolComponent.BufferType.UnsolicitedPong))
                                        {
                                            if (dataBufferCount == 0)
                                            {
                                                break;
                                            }

                                            List<ArraySegment<byte>> sendBuffers = new List<ArraySegment<byte>>((int)dataBufferCount);
                                            int sendBufferSize = 0;
                                            ArraySegment<byte> framingBuffer = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);
                                            sendBuffers.Add(framingBuffer);
                                            sendBufferSize += framingBuffer.Count;

                                            // There can be at most 2 dataBuffers
                                            // - one for the framing header and one for the payload
                                            if (dataBufferCount == 2)
                                            {
                                                ArraySegment<byte> payload;

                                                // The second buffer might be from the pinned send payload buffer (1) or from the
                                                // internal native buffer (2).  In the case of a PONG response being generated, the buffer
                                                // would be from (2).  Even if the payload is from a WebSocketSend operation, the buffer
                                                // might be (1) only if no buffer copies were needed (in the case of no masking, for example).
                                                // Or it might be (2).  So, we need to check.
                                                if (_webSocket._internalBuffer.IsPinnedSendPayloadBuffer(dataBuffers[1], bufferType))
                                                {
                                                    payload = _webSocket._internalBuffer.ConvertPinnedSendPayloadFromNative(dataBuffers[1], bufferType);
                                                }
                                                else
                                                {
                                                    payload = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[1], bufferType);
                                                }

                                                sendBuffers.Add(payload);
                                                sendBufferSize += payload.Count;
                                            }

                                            ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                            HttpWebSocket.ThrowIfConnectionAborted(_webSocket._innerStream, false);
                                            await _webSocket.SendFrameAsync(sendBuffers, cancellationToken).SuppressContextFlow();
                                            Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                            _webSocket.ThrowIfPendingException();
                                            bytesSent += sendBufferSize;
                                            _webSocket._keepAliveTracker.OnDataSent();
                                        }
                                    }
                                    finally
                                    {
                                        WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                                            actionContext,
                                            bytesSent);
                                    }

                                    break;
                                default:
                                    Debug.Fail($"Invalid action '{action}' returned from WebSocketGetAction.");
                                    throw new InvalidOperationException();
                            }
                        }

                        // WebSocketGetAction has returned NO_ACTION. In general, WebSocketGetAction will return
                        // NO_ACTION if there is no work item available to process at the current moment. But
                        // there could be work items on the queue still.  Those work items can't be returned back
                        // until the current work item (being done by another thread) is complete.
                        //
                        // It's possible that another thread might be finishing up an async operation and needs
                        // to call WebSocketCompleteAction. Once that happens, calling WebSocketGetAction on this
                        // thread might return something else to do. This happens, for example, if the RECEIVE thread
                        // ends up having to begin sending out a PONG response (due to it receiving a PING) and the
                        // current SEND thread has posted a WebSocketSend but it can't be processed yet until the
                        // RECEIVE thread has finished sending out the PONG response.
                        //
                        // So, we need to release the lock briefly to give the other thread a chance to finish
                        // processing.  We won't actually exit this outer loop and return from this async method
                        // until the caller's async operation has been fully completed.
                        ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                        Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                    }
                }
                finally
                {
                    Cleanup();
                    ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                }

                return ReceiveResult;
            }

            public sealed class ReceiveOperation : WebSocketOperation
            {
                private int _receiveState;
                private bool _pongReceived;
                private bool _receiveCompleted;

                public ReceiveOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                }

                protected override WebSocketProtocolComponent.ActionQueue ActionQueue
                {
                    get { return WebSocketProtocolComponent.ActionQueue.Receive; }
                }

                protected override int BufferCount
                {
                    get { return 1; }
                }

                protected override void Initialize(Nullable<ArraySegment<byte>> buffer, CancellationToken cancellationToken)
                {
                    Debug.Assert(buffer != null, "'buffer' MUST NOT be NULL.");
                    _pongReceived = false;
                    _receiveCompleted = false;
                    _webSocket.ThrowIfDisposed();

                    int originalReceiveState = Interlocked.CompareExchange(ref _webSocket._receiveState,
                        ReceiveState.Application, ReceiveState.Idle);

                    switch (originalReceiveState)
                    {
                        case ReceiveState.Idle:
                            _receiveState = ReceiveState.Application;
                            break;
                        case ReceiveState.Application:
                            Debug.Fail("'originalReceiveState' MUST NEVER be ReceiveState.Application at this point.");
                            break;
                        case ReceiveState.PayloadAvailable:
                            WebSocketReceiveResult receiveResult;
                            if (!_webSocket._internalBuffer.ReceiveFromBufferedPayload(buffer.Value, out receiveResult))
                            {
                                _webSocket.UpdateReceiveState(ReceiveState.Idle, ReceiveState.PayloadAvailable);
                            }
                            ReceiveResult = receiveResult;
                            _receiveCompleted = true;
                            break;
                        default:
                            Debug.Fail($"Invalid ReceiveState '{originalReceiveState}'.");
                            break;
                    }
                }

                protected override void Cleanup()
                {
                }

                protected override bool ShouldContinue(CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (_receiveCompleted)
                    {
                        return false;
                    }

                    _webSocket.ThrowIfDisposed();
                    _webSocket.ThrowIfPendingException();
                    WebSocketProtocolComponent.WebSocketReceive(_webSocket);

                    return true;
                }

                protected override bool ProcessAction_NoAction()
                {
                    if (_pongReceived)
                    {
                        _receiveCompleted = false;
                        _pongReceived = false;
                        return false;
                    }

                    Debug.Assert(ReceiveResult != null,
                        "'ReceiveResult' MUST NOT be NULL.");
                    _receiveCompleted = true;

                    if (ReceiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return true;
                    }

                    return false;
                }

                protected override void ProcessAction_IndicateReceiveComplete(
                    Nullable<ArraySegment<byte>> buffer,
                    WebSocketProtocolComponent.BufferType bufferType,
                    WebSocketProtocolComponent.Action action,
                    Interop.WebSocket.Buffer[] dataBuffers,
                    uint dataBufferCount,
                    IntPtr actionContext)
                {
                    Debug.Assert(buffer != null, "'buffer MUST NOT be NULL.");

                    int bytesTransferred = 0;
                    _pongReceived = false;

                    if (bufferType == WebSocketProtocolComponent.BufferType.PingPong)
                    {
                        // ignoring received pong frame
                        _pongReceived = true;
                        WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                            actionContext,
                            bytesTransferred);
                        return;
                    }

                    WebSocketReceiveResult receiveResult;
                    try
                    {
                        ArraySegment<byte> payload;
                        WebSocketMessageType messageType = GetMessageType(bufferType);
                        int newReceiveState = ReceiveState.Idle;

                        if (bufferType == WebSocketProtocolComponent.BufferType.Close)
                        {
                            payload = ArraySegment<byte>.Empty;
                            _webSocket._internalBuffer.ConvertCloseBuffer(action, dataBuffers[0], out WebSocketCloseStatus closeStatus, out string? reason);

                            receiveResult = new WebSocketReceiveResult(bytesTransferred,
                                messageType, true, closeStatus, reason);
                        }
                        else
                        {
                            payload = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);

                            bool endOfMessage = bufferType ==
                                WebSocketProtocolComponent.BufferType.BinaryMessage ||
                                bufferType == WebSocketProtocolComponent.BufferType.UTF8Message ||
                                bufferType == WebSocketProtocolComponent.BufferType.Close;

                            if (payload.Count > buffer.Value.Count)
                            {
                                _webSocket._internalBuffer.BufferPayload(payload, buffer.Value.Count, messageType, endOfMessage);
                                newReceiveState = ReceiveState.PayloadAvailable;
                                endOfMessage = false;
                            }

                            bytesTransferred = Math.Min(payload.Count, (int)buffer.Value.Count);
                            if (bytesTransferred > 0)
                            {
                                Buffer.BlockCopy(payload.Array!,
                                    payload.Offset,
                                    buffer.Value.Array!,
                                    buffer.Value.Offset,
                                    bytesTransferred);
                            }

                            receiveResult = new WebSocketReceiveResult(bytesTransferred, messageType, endOfMessage);
                        }

                        _webSocket.UpdateReceiveState(newReceiveState, _receiveState);
                    }
                    finally
                    {
                        WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                            actionContext,
                            bytesTransferred);
                    }

                    ReceiveResult = receiveResult;
                }
            }

            public class SendOperation : WebSocketOperation
            {
                protected bool _BufferHasBeenPinned;

                public SendOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                }

                protected override WebSocketProtocolComponent.ActionQueue ActionQueue
                {
                    get { return WebSocketProtocolComponent.ActionQueue.Send; }
                }

                protected override int BufferCount
                {
                    get { return 2; }
                }

                protected virtual Nullable<Interop.WebSocket.Buffer> CreateBuffer(Nullable<ArraySegment<byte>> buffer)
                {
                    if (buffer == null)
                    {
                        return null;
                    }

                    Interop.WebSocket.Buffer payloadBuffer;
                    payloadBuffer = default;
                    _webSocket._internalBuffer.PinSendBuffer(buffer.Value, out _BufferHasBeenPinned);
                    payloadBuffer.Data.BufferData = _webSocket._internalBuffer.ConvertPinnedSendPayloadToNative(buffer.Value);
                    payloadBuffer.Data.BufferLength = (uint)buffer.Value.Count;
                    return payloadBuffer;
                }

                protected override bool ProcessAction_NoAction()
                {
                    return false;
                }

                protected override void Cleanup()
                {
                    if (_BufferHasBeenPinned)
                    {
                        _BufferHasBeenPinned = false;
                        _webSocket._internalBuffer.ReleasePinnedSendBuffer();
                    }
                }

                internal WebSocketProtocolComponent.BufferType BufferType { get; set; }

                protected override void Initialize(Nullable<ArraySegment<byte>> buffer,
                    CancellationToken cancellationToken)
                {
                    Debug.Assert(!_BufferHasBeenPinned, "'_BufferHasBeenPinned' MUST NOT be pinned at this point.");
                    _webSocket.ThrowIfDisposed();
                    _webSocket.ThrowIfPendingException();

                    Nullable<Interop.WebSocket.Buffer> payloadBuffer = CreateBuffer(buffer);
                    if (payloadBuffer != null)
                    {
                        WebSocketProtocolComponent.WebSocketSend(_webSocket, BufferType, payloadBuffer.Value);
                    }
                    else
                    {
                        WebSocketProtocolComponent.WebSocketSendWithoutBody(_webSocket, BufferType);
                    }
                }

                protected override bool ShouldContinue(CancellationToken cancellationToken)
                {
                    Debug.Assert(ReceiveResult == null, "'ReceiveResult' MUST be NULL.");
                    if (AsyncOperationCompleted)
                    {
                        return false;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return true;
                }
            }

            public sealed class CloseOutputOperation : SendOperation
            {
                public CloseOutputOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                    BufferType = WebSocketProtocolComponent.BufferType.Close;
                }

                internal WebSocketCloseStatus CloseStatus { get; set; }
                internal string? CloseReason { get; set; }

                protected override Nullable<Interop.WebSocket.Buffer> CreateBuffer(Nullable<ArraySegment<byte>> buffer)
                {
                    Debug.Assert(buffer == null, "'buffer' MUST BE NULL.");
                    _webSocket.ThrowIfDisposed();
                    _webSocket.ThrowIfPendingException();

                    if (CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        return null;
                    }

                    Interop.WebSocket.Buffer payloadBuffer = default;
                    if (CloseReason != null)
                    {
                        byte[] blob = Encoding.UTF8.GetBytes(CloseReason);
                        Debug.Assert(blob.Length <= WebSocketValidate.MaxControlFramePayloadLength,
                            "The close reason is too long.");
                        ArraySegment<byte> closeBuffer = new ArraySegment<byte>(blob, 0, Math.Min(WebSocketValidate.MaxControlFramePayloadLength, blob.Length));
                        _webSocket._internalBuffer.PinSendBuffer(closeBuffer, out _BufferHasBeenPinned);
                        payloadBuffer.CloseStatus.ReasonData = _webSocket._internalBuffer.ConvertPinnedSendPayloadToNative(closeBuffer);
                        payloadBuffer.CloseStatus.ReasonLength = (uint)closeBuffer.Count;
                    }

                    payloadBuffer.CloseStatus.CloseStatus = (ushort)CloseStatus;
                    return payloadBuffer;
                }
            }
        }

        private abstract class KeepAliveTracker : IDisposable
        {
            // Multi-Threading: only one thread at a time is allowed to call OnDataReceived or OnDataSent
            // - but both methods can be called from different threads at the same time.
            public abstract void OnDataReceived();
            public abstract void OnDataSent();
            public abstract void Dispose();
            public abstract void StartTimer(WebSocketBase webSocket);
            public abstract void ResetTimer();
            public abstract bool ShouldSendKeepAlive();

            public static KeepAliveTracker Create(TimeSpan keepAliveInterval)
            {
                if ((int)keepAliveInterval.TotalMilliseconds > 0)
                {
                    return new DefaultKeepAliveTracker(keepAliveInterval);
                }

                return new DisabledKeepAliveTracker();
            }

            private sealed class DisabledKeepAliveTracker : KeepAliveTracker
            {
                public override void OnDataReceived()
                {
                }

                public override void OnDataSent()
                {
                }

                public override void ResetTimer()
                {
                }

                public override void StartTimer(WebSocketBase webSocket)
                {
                }

                public override bool ShouldSendKeepAlive()
                {
                    return false;
                }

                public override void Dispose()
                {
                }
            }

            private sealed class DefaultKeepAliveTracker : KeepAliveTracker
            {
                private static readonly TimerCallback s_KeepAliveTimerElapsedCallback = new TimerCallback(OnKeepAlive);
                private readonly TimeSpan _keepAliveInterval;
                private long _lastSendActivityTimestamp;
                private long _lastReceiveActivityTimestamp;
                private Timer? _keepAliveTimer;

                public DefaultKeepAliveTracker(TimeSpan keepAliveInterval)
                {
                    _keepAliveInterval = keepAliveInterval;
                }

                public override void OnDataReceived()
                {
                    _lastReceiveActivityTimestamp = Stopwatch.GetTimestamp();
                }

                public override void OnDataSent()
                {
                    _lastSendActivityTimestamp = Stopwatch.GetTimestamp();
                }

                public override void ResetTimer()
                {
                    ResetTimer((int)_keepAliveInterval.TotalMilliseconds);
                }

                public override void StartTimer(WebSocketBase webSocket)
                {
                    Debug.Assert(webSocket != null, "'webSocket' MUST NOT be NULL.");
                    Debug.Assert(webSocket._keepAliveTracker != null,
                        "'webSocket._KeepAliveTracker' MUST NOT be NULL at this point.");
                    int keepAliveIntervalMilliseconds = (int)_keepAliveInterval.TotalMilliseconds;
                    Debug.Assert(keepAliveIntervalMilliseconds > 0, "'keepAliveIntervalMilliseconds' MUST be POSITIVE.");

                    // The correct pattern is to first initialize the Timer object, assign it to the member variable
                    // and only afterwards enable the Timer. This is required because the constructor, together with
                    // the assignment are not guaranteed to be an atomic operation, which creates a race between the
                    // assignment and the Timer callback.
                    _keepAliveTimer = new Timer(s_KeepAliveTimerElapsedCallback, webSocket, Timeout.Infinite,
                        Timeout.Infinite);

                    _keepAliveTimer.Change(keepAliveIntervalMilliseconds, Timeout.Infinite);
                }

                public override bool ShouldSendKeepAlive()
                {
                    TimeSpan idleTime = GetIdleTime();
                    if (idleTime >= _keepAliveInterval)
                    {
                        return true;
                    }

                    ResetTimer((int)(_keepAliveInterval - idleTime).TotalMilliseconds);
                    return false;
                }

                public override void Dispose()
                {
                    _keepAliveTimer!.Dispose();
                }

                private void ResetTimer(int dueInMilliseconds)
                {
                    _keepAliveTimer!.Change(dueInMilliseconds, Timeout.Infinite);
                }

                private TimeSpan GetIdleTime()
                {
                    TimeSpan sinceLastSendActivity = GetTimeElapsed(_lastSendActivityTimestamp);
                    TimeSpan sinceLastReceiveActivity = GetTimeElapsed(_lastReceiveActivityTimestamp);

                    if (sinceLastReceiveActivity < sinceLastSendActivity)
                    {
                        return sinceLastReceiveActivity;
                    }

                    return sinceLastSendActivity;
                }

                private TimeSpan GetTimeElapsed(long timestamp) => timestamp != 0 ?
                    Stopwatch.GetElapsedTime(timestamp) :
                    _keepAliveInterval;
            }
        }

        private sealed class OutstandingOperationHelper : IDisposable
        {
            private volatile int _operationsOutstanding;
            private volatile CancellationTokenSource? _cancellationTokenSource;
            private volatile bool _isDisposed;
            private readonly object _thisLock = new object();

            public bool TryStartOperation(CancellationToken userCancellationToken, out CancellationToken linkedCancellationToken)
            {
                linkedCancellationToken = CancellationToken.None;
                ThrowIfDisposed();

                lock (_thisLock)
                {
                    int operationsOutstanding = ++_operationsOutstanding;

                    if (operationsOutstanding == 1)
                    {
                        linkedCancellationToken = CreateLinkedCancellationToken(userCancellationToken);
                        return true;
                    }

                    Debug.Assert(operationsOutstanding >= 1, "'operationsOutstanding' must never be smaller than 1.");
                    return false;
                }
            }

            public void CompleteOperation(bool ownsCancellationTokenSource)
            {
                if (_isDisposed)
                {
                    // no-op if the WebSocket is already aborted
                    return;
                }

                CancellationTokenSource? snapshot = null;

                lock (_thisLock)
                {
                    --_operationsOutstanding;
                    Debug.Assert(_operationsOutstanding >= 0, "'_OperationsOutstanding' must never be smaller than 0.");

                    if (ownsCancellationTokenSource)
                    {
                        snapshot = _cancellationTokenSource;
                        _cancellationTokenSource = null;
                    }
                }

                snapshot?.Dispose();
            }

            // Has to be called under _ThisLock lock
            private CancellationToken CreateLinkedCancellationToken(CancellationToken cancellationToken)
            {
                var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Debug.Assert(_cancellationTokenSource == null, "'_cancellationTokenSource' MUST be NULL.");
                _cancellationTokenSource = linkedCancellationTokenSource;

                return linkedCancellationTokenSource.Token;
            }

            public void CancelIO()
            {
                CancellationTokenSource? cancellationTokenSourceSnapshot = null;

                lock (_thisLock)
                {
                    if (_operationsOutstanding == 0)
                    {
                        return;
                    }

                    cancellationTokenSourceSnapshot = _cancellationTokenSource;
                }

                if (cancellationTokenSourceSnapshot != null)
                {
                    try
                    {
                        cancellationTokenSourceSnapshot.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Simply ignore this exception - There is apparently a rare race condition
                        // where the cancellationTokensource is disposed before the Cancel method call completed.
                    }
                }
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                CancellationTokenSource? snapshot = null;
                lock (_thisLock)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    _isDisposed = true;
                    snapshot = _cancellationTokenSource;
                    _cancellationTokenSource = null;
                }

                snapshot?.Dispose();
            }

            private void ThrowIfDisposed()
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
            }
        }

        internal interface IWebSocketStream
        {
            // Switching to opaque mode will change the behavior to use the knowledge that the WebSocketBase class
            // is pinning all payloads already and that we will have at most one outstanding send and receive at any
            // given time. This allows us to avoid creation of OverlappedData and pinning for each operation.

            void SwitchToOpaqueMode(WebSocketBase webSocket);
            void Abort();
            bool SupportsMultipleWrite { get; }
            Task MultipleWriteAsync(IList<ArraySegment<byte>> buffers, CancellationToken cancellationToken);

            // Any implementation has to guarantee that no exception is thrown synchronously
            // for example by enforcing a Task.Yield at the beginning of the method
            // This is necessary to enforce an API contract (for WebSocketBase.StartOnCloseCompleted) that ensures
            // that all locks have been released whenever an exception is thrown from it.
            Task CloseNetworkConnectionAsync(CancellationToken cancellationToken);
        }

        private static class ReceiveState
        {
            internal const int SendOperation = -1;
            internal const int Idle = 0;
            internal const int Application = 1;
            internal const int PayloadAvailable = 2;
        }
    }
}
