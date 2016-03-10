// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    // TODO:
    // - Plumb status through async APIs to avoid callbacks on synchronous completion
    //     - NOTE: this will require refactoring in the *Async APIs to accommodate the lack
    //             of completion posting
    // - Add support for unregistering + reregistering for events
    //     - This will require a new state for each queue, unregistred, to track whether or
    //       not the queue is currently registered to receive events
    // - Audit Close()-related code for the possibility of file descriptor recycling issues
    //     - It might make sense to change _closeLock to a ReaderWriterLockSlim that is
    //       acquired for read by all public methods before attempting a completion and
    //       acquired for write by Close() and HandlEvents()
    //
    // NOTE: the publicly-exposed asynchronous methods should match the behavior of
    //       Winsock overlapped sockets as closely as possible. Especially important are
    //       completion semantics, as the consuming code relies on the Winsock behavior.
    //
    //       Winsock queues a completion callback for an overlapped operation in two cases:
    //         1. the operation successfully completes synchronously, or
    //         2. the operation completes asynchronously, successfully or otherwise.
    //       In other words, a completion callback is queued iff an operation does not
    //       fail synchronously. The asynchronous methods below (e.g. ReceiveAsync) may
    //       fail synchronously for either of the following reasons:
    //         1. an underlying system call fails synchronously, or
    //         2. an underlying system call returns EAGAIN, but the socket is closed before
    //            the method is able to enqueue its corresponding operation.
    //       In the first case, the async method should return the SocketError that
    //       corresponds to the native error code; in the second, the method should return
    //       SocketError.OperationAborted (which matches what Winsock would return in this
    //       case). The publicly-exposed synchronous methods may also encounter the second
    //       case. In this situation these methods should return SocketError.Interrupted
    //       (which again matches Winsock).
    internal sealed class SocketAsyncContext
    {
        private abstract class AsyncOperation
        {
            private enum State
            {
                Waiting = 0,
                Running = 1,
                Complete = 2,
                Cancelled = 3
            }

            private int _state; // Actually AsyncOperation.State.

#if DEBUG
            private int _callbackQueued; // When non-zero, the callback has been queued.
#endif

            public AsyncOperation Next;
            protected object CallbackOrEvent;
            public SocketError ErrorCode;
            public byte[] SocketAddress;
            public int SocketAddressLen;

            public ManualResetEventSlim Event
            {
                private get { return (ManualResetEventSlim)CallbackOrEvent; }
                set { CallbackOrEvent = value; }
            }

            public AsyncOperation()
            {
                _state = (int)State.Waiting;
                Next = this;
            }

            public void QueueCompletionCallback()
            {
                Debug.Assert(!(CallbackOrEvent is ManualResetEventSlim), $"Unexpected CallbackOrEvent: {CallbackOrEvent}");
                Debug.Assert(_state != (int)State.Cancelled, $"Unexpected _state: {_state}");
#if DEBUG
                Debug.Assert(Interlocked.CompareExchange(ref _callbackQueued, 1, 0) == 0, $"Unexpected _callbackQueued: {_callbackQueued}");
#endif

                ThreadPool.QueueUserWorkItem(o => ((AsyncOperation)o).InvokeCallback(), this);
            }

            public bool TryComplete(SocketAsyncContext context)
            {
                Debug.Assert(_state == (int)State.Waiting, $"Unexpected _state: {_state}");

                return DoTryComplete(context);
            }

            public bool TryCompleteAsync(SocketAsyncContext context)
            {
                return TryCompleteOrAbortAsync(context, abort: false);
            }

            public void AbortAsync()
            {
                bool completed = TryCompleteOrAbortAsync(null, abort: true);
                Debug.Assert(completed, $"Expected TryCompleteOrAbortAsync to return true");
            }

            private bool TryCompleteOrAbortAsync(SocketAsyncContext context, bool abort)
            {
                Debug.Assert(context != null || abort, $"Unexpected values: context={context}, abort={abort}");

                State oldState = (State)Interlocked.CompareExchange(ref _state, (int)State.Running, (int)State.Waiting);
                if (oldState == State.Cancelled)
                {
                    // This operation has been cancelled. The canceller is responsible for
                    // correctly updating any state that would have been handled by
                    // AsyncOperation.Abort.
                    return true;
                }

                Debug.Assert(oldState != State.Complete && oldState != State.Running, $"Unexpected oldState: {oldState}");

                bool completed;
                if (abort)
                {
                    Abort();
                    ErrorCode = SocketError.OperationAborted;
                    completed = true;
                }
                else
                {
                    completed = DoTryComplete(context);
                }

                if (completed)
                {
                    var @event = CallbackOrEvent as ManualResetEventSlim;
                    if (@event != null)
                    {
                        @event.Set();
                    }
                    else
                    {
                        QueueCompletionCallback();
                    }

                    Volatile.Write(ref _state, (int)State.Complete);
                    return true;
                }

                Volatile.Write(ref _state, (int)State.Waiting);
                return false;
            }

            public bool Wait(int timeout)
            {
                if (Event.Wait(timeout))
                {
                    return true;
                }

                var spinWait = new SpinWait();
                for (;;)
                {
                    int state = Interlocked.CompareExchange(ref _state, (int)State.Cancelled, (int)State.Waiting);
                    switch ((State)state)
                    {
                        case State.Running:
                            // A completion attempt is in progress. Keep busy-waiting.
                            spinWait.SpinOnce();
                            break;

                        case State.Complete:
                            // A completion attempt succeeded. Consider this operation as having completed within the timeout.
                            return true;

                        case State.Waiting:
                            // This operation was successfully cancelled.
                            return false;
                    }
                }
            }

            protected abstract void Abort();

            protected abstract bool DoTryComplete(SocketAsyncContext context);

            protected abstract void InvokeCallback();
        }

        private abstract class TransferOperation : AsyncOperation
        {
            public byte[] Buffer;
            public int Offset;
            public int Count;
            public SocketFlags Flags;
            public int BytesTransferred;
            public SocketFlags ReceivedFlags;

            protected sealed override void Abort() { }
        }

        private abstract class SendReceiveOperation : TransferOperation
        {
            public IList<ArraySegment<byte>> Buffers;
            public int BufferIndex;

            public Action<int, byte[], int, SocketFlags, SocketError> Callback
            {
                private get { return (Action<int, byte[], int, SocketFlags, SocketError>)CallbackOrEvent; }
                set { CallbackOrEvent = value; }
            }

            protected sealed override void InvokeCallback()
            {
                Callback(BytesTransferred, SocketAddress, SocketAddressLen, ReceivedFlags, ErrorCode);
            }
        }

        private sealed class SendOperation : SendReceiveOperation
        {
            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                return SocketPal.TryCompleteSendTo(context._socket, Buffer, Buffers, ref BufferIndex, ref Offset, ref Count, Flags, SocketAddress, SocketAddressLen, ref BytesTransferred, out ErrorCode);
            }
        }

        private sealed class ReceiveOperation : SendReceiveOperation
        {
            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                return SocketPal.TryCompleteReceiveFrom(context._socket, Buffer, Buffers, Offset, Count, Flags, SocketAddress, ref SocketAddressLen, out BytesTransferred, out ReceivedFlags, out ErrorCode);
            }
        }

        private sealed class ReceiveMessageFromOperation : TransferOperation
        {
            public bool IsIPv4;
            public bool IsIPv6;
            public IPPacketInformation IPPacketInformation;

            public Action<int, byte[], int, SocketFlags, IPPacketInformation, SocketError> Callback
            {
                private get { return (Action<int, byte[], int, SocketFlags, IPPacketInformation, SocketError>)CallbackOrEvent; }
                set { CallbackOrEvent = value; }
            }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                return SocketPal.TryCompleteReceiveMessageFrom(context._socket, Buffer, Offset, Count, Flags, SocketAddress, ref SocketAddressLen, IsIPv4, IsIPv6, out BytesTransferred, out ReceivedFlags, out IPPacketInformation, out ErrorCode);
            }

            protected override void InvokeCallback()
            {
                Callback(BytesTransferred, SocketAddress, SocketAddressLen, ReceivedFlags, IPPacketInformation, ErrorCode);
            }
        }

        private abstract class AcceptOrConnectOperation : AsyncOperation
        {
        }

        private sealed class AcceptOperation : AcceptOrConnectOperation
        {
            public int AcceptedFileDescriptor;

            public Action<int, byte[], int, SocketError> Callback
            {
                private get { return (Action<int, byte[], int, SocketError>)CallbackOrEvent; }
                set { CallbackOrEvent = value; }
            }

            protected override void Abort()
            {
                AcceptedFileDescriptor = -1;
            }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool completed = SocketPal.TryCompleteAccept(context._socket, SocketAddress, ref SocketAddressLen, out AcceptedFileDescriptor, out ErrorCode);
                Debug.Assert(ErrorCode == SocketError.Success || AcceptedFileDescriptor == -1, $"Unexpected values: ErrorCode={ErrorCode}, AcceptedFileDescriptor={AcceptedFileDescriptor}");
                return completed;
            }

            protected override void InvokeCallback()
            {
                Callback(AcceptedFileDescriptor, SocketAddress, SocketAddressLen, ErrorCode);
            }
        }

        private sealed class ConnectOperation : AcceptOrConnectOperation
        {
            public Action<SocketError> Callback
            {
                private get { return (Action<SocketError>)CallbackOrEvent; }
                set { CallbackOrEvent = value; }
            }

            protected override void Abort() { }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool result = SocketPal.TryCompleteConnect(context._socket, SocketAddressLen, out ErrorCode);
                context.RegisterConnectResult(ErrorCode);
                return result;
            }

            protected override void InvokeCallback()
            {
                Callback(ErrorCode);
            }
        }

        private enum QueueState
        {
            Clear = 0,
            Set = 1,
            Stopped = 2,
        }

        private struct OperationQueue<TOperation>
            where TOperation : AsyncOperation
        {
            private AsyncOperation _tail;

            public QueueState State { get; set; }
            public bool IsStopped { get { return State == QueueState.Stopped; } }
            public bool IsEmpty { get { return _tail == null; } }

            public void Enqueue(TOperation operation)
            {
                Debug.Assert(!IsStopped, "Expected !IsStopped");
                Debug.Assert(operation.Next == operation, "Expected operation.Next == operation");

                if (!IsEmpty)
                {
                    operation.Next = _tail.Next;
                    _tail.Next = operation;
                }

                _tail = operation;
            }

            private bool TryDequeue(out TOperation operation)
            {
                if (_tail == null)
                {
                    operation = null;
                    return false;
                }

                AsyncOperation head = _tail.Next;
                if (head == _tail)
                {
                    _tail = null;
                }
                else
                {
                    _tail.Next = head.Next;
                }

                head.Next = null;
                operation = (TOperation)head;
                return true;
            }

            private void Requeue(TOperation operation)
            {
                // Insert at the head of the queue
                Debug.Assert(!IsStopped, "Expected !IsStopped");
                Debug.Assert(operation.Next == null, "Operation already in queue");

                if (IsEmpty)
                {
                    operation.Next = operation;
                    _tail = operation;
                }
                else
                {
                    operation.Next = _tail.Next;
                    _tail.Next = operation;
                }
            }

            public OperationQueue<TOperation> Stop()
            {
                OperationQueue<TOperation> result = this;
                _tail = null;
                State = QueueState.Stopped;
                return result;
            }

            public void Complete(SocketAsyncContext context)
            {
                if (IsStopped)
                    return;

                State = QueueState.Set;

                TOperation op;
                while (TryDequeue(out op))
                {
                    if (!op.TryCompleteAsync(context))
                    {
                        Requeue(op);
                        return;
                    }
                }
            }

            public void StopAndAbort()
            {
                OperationQueue<TOperation> queue = Stop();

                TOperation op;
                while (queue.TryDequeue(out op))
                {
                    op.AbortAsync();
                }
            }

            public bool AllOfType<TCandidate>() where TCandidate : TOperation
            {
                bool tailIsCandidateType = _tail is TCandidate;
#if DEBUG
                // We assume that all items are of the specified type, or all are not.  Check this invariant.
                if (_tail != null)
                {
                    AsyncOperation op = _tail;
                    do
                    {
                        Debug.Assert((op is TCandidate) == tailIsCandidateType, $"Unexpected values: op={op}, tailIsCandidateType={tailIsCandidateType}");
                        op = op.Next;
                    }
                    while (op != _tail);
                }
#endif
                return tailIsCandidateType;
            }
        }

        private SafeCloseSocket _socket;
        private OperationQueue<TransferOperation> _receiveQueue;
        private OperationQueue<SendOperation> _sendQueue;
        private OperationQueue<AcceptOrConnectOperation> _acceptOrConnectQueue;
        private SocketAsyncEngine.Token _asyncEngineToken;
        private Interop.Sys.SocketEvents _registeredEvents;
        private bool _nonBlockingSet;
        private bool _connectFailed;

        private object _queueLock = new object();

        public SocketAsyncContext(SafeCloseSocket socket)
        {
            _socket = socket;
        }

        private void Register(Interop.Sys.SocketEvents events)
        {
            Debug.Assert(Monitor.IsEntered(_queueLock), "Expected _queueLock to be held");
            Debug.Assert((_registeredEvents & events) == Interop.Sys.SocketEvents.None, $"Unexpected values: _registeredEvents={_registeredEvents}, events={events}");

            if (!_asyncEngineToken.WasAllocated)
            {
                _asyncEngineToken = new SocketAsyncEngine.Token(this);
            }

            events |= _registeredEvents;

            Interop.Error errorCode;
            if (!_asyncEngineToken.TryRegister(_socket, _registeredEvents, events, out errorCode))
            {
                // TODO: throw an appropiate exception
                throw new Exception(string.Format("SocketAsyncContext.Register: {0}", errorCode));
            }

            _registeredEvents = events;
        }

        public void Close()
        {
            lock (_queueLock)
            {
                // Drain queues

                _acceptOrConnectQueue.StopAndAbort();
                _sendQueue.StopAndAbort();
                _receiveQueue.StopAndAbort();

                // Freeing the token will prevent any future event delivery.  This socket will be unregistered
                // from the event port automatically by the OS when it's closed.
                _asyncEngineToken.Free();
            }
        }

        public void SetNonBlocking()
        {
            //
            // Our sockets may start as blocking, and later transition to non-blocking, either because the user
            // explicitly requested non-blocking mode, or because we need non-blocking mode to support async
            // operations.  We never transition back to blocking mode, to avoid problems synchronizing that
            // transition with the async infrastructure.
            //
            // Note that there's no synchronization here, so we may set the non-blocking option multiple times
            // in a race.  This should be fine.
            //
            if (!_nonBlockingSet)
            {
                if (Interop.Sys.Fcntl.SetIsNonBlocking(_socket, 1) != 0)
                {
                    throw new SocketException((int)SocketPal.GetSocketErrorForErrorCode(Interop.Sys.GetLastError()));
                }

                _nonBlockingSet = true;
            }
        }

        public void CheckForPriorConnectFailure()
        {
            if (_connectFailed)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_connect_multiconnect_notsupported);
            }
        }

        public void RegisterConnectResult(SocketError error)
        {
            if (error != SocketError.Success && error != SocketError.WouldBlock)
            {
                _connectFailed = true;
            }
        }

        private bool TryBeginOperation<TOperation>(ref OperationQueue<TOperation> queue, TOperation operation, Interop.Sys.SocketEvents events, out bool isStopped)
            where TOperation : AsyncOperation
        {
            lock (_queueLock)
            {
                switch (queue.State)
                {
                    case QueueState.Stopped:
                        isStopped = true;
                        return false;

                    case QueueState.Clear:
                        break;

                    case QueueState.Set:
                        isStopped = false;
                        queue.State = QueueState.Clear;
                        return false;
                }

                if ((_registeredEvents & events) == Interop.Sys.SocketEvents.None)
                {
                    Register(events);
                }

                queue.Enqueue(operation);
                isStopped = false;
                return true;
            }
        }

        public SocketError Accept(byte[] socketAddress, ref int socketAddressLen, int timeout, out int acceptedFd)
        {
            Debug.Assert(socketAddress != null, "Expected non-null socketAddress");
            Debug.Assert(socketAddressLen > 0, $"Unexpected socketAddressLen: {socketAddressLen}");
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketError errorCode;
            if (SocketPal.TryCompleteAccept(_socket, socketAddress, ref socketAddressLen, out acceptedFd, out errorCode))
            {
                Debug.Assert(errorCode == SocketError.Success || acceptedFd == -1, $"Unexpected values: errorCode={errorCode}, acceptedFd={acceptedFd}");
                return errorCode;
            }

            using (var @event = new ManualResetEventSlim(false, 0))
            {
                var operation = new AcceptOperation {
                    Event = @event,
                    SocketAddress = socketAddress,
                    SocketAddressLen = socketAddressLen
                };

                bool isStopped;
                while (!TryBeginOperation(ref _acceptOrConnectQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
                {
                    if (isStopped)
                    {
                        acceptedFd = -1;
                        return SocketError.Interrupted;
                    }

                    if (operation.TryComplete(this))
                    {
                        socketAddressLen = operation.SocketAddressLen;
                        acceptedFd = operation.AcceptedFileDescriptor;
                        return operation.ErrorCode;
                    }
                }

                if (!operation.Wait(timeout))
                {
                    acceptedFd = -1;
                    return SocketError.TimedOut;
                }

                socketAddressLen = operation.SocketAddressLen;
                acceptedFd = operation.AcceptedFileDescriptor;
                return operation.ErrorCode;
            }
        }

        public SocketError AcceptAsync(byte[] socketAddress, int socketAddressLen, Action<int, byte[], int, SocketError> callback)
        {
            Debug.Assert(socketAddress != null, "Expected non-null socketAddress");
            Debug.Assert(socketAddressLen > 0, $"Unexpected socketAddressLen: {socketAddressLen}");
            Debug.Assert(callback != null, "Expected non-null callback");

            SetNonBlocking();

            int acceptedFd;
            SocketError errorCode;
            if (SocketPal.TryCompleteAccept(_socket, socketAddress, ref socketAddressLen, out acceptedFd, out errorCode))
            {
                Debug.Assert(errorCode == SocketError.Success || acceptedFd == -1, $"Unexpected values: errorCode={errorCode}, acceptedFd={acceptedFd}");

                if (errorCode == SocketError.Success)
                {
                    ThreadPool.QueueUserWorkItem(args =>
                    {
                        var tup = (Tuple<Action<int, byte[], int, SocketError>, int, byte[], int>)args;
                        tup.Item1(tup.Item2, tup.Item3, tup.Item4, SocketError.Success);
                    }, Tuple.Create(callback, acceptedFd, socketAddress, socketAddressLen));
                }
                return errorCode;
            }

            var operation = new AcceptOperation {
                Callback = callback,
                SocketAddress = socketAddress,
                SocketAddressLen = socketAddressLen
            };

            bool isStopped;
            while (!TryBeginOperation(ref _acceptOrConnectQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
            {
                if (isStopped)
                {
                    return SocketError.OperationAborted;
                }

                if (operation.TryComplete(this))
                {
                    operation.QueueCompletionCallback();
                    break;
                }
            }
            return SocketError.IOPending;
        }

        public SocketError Connect(byte[] socketAddress, int socketAddressLen, int timeout)
        {
            Debug.Assert(socketAddress != null, "Expected non-null socketAddress");
            Debug.Assert(socketAddressLen > 0, $"Unexpected socketAddressLen: {socketAddressLen}");
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            CheckForPriorConnectFailure();

            SocketError errorCode;
            if (SocketPal.TryStartConnect(_socket, socketAddress, socketAddressLen, out errorCode))
            {
                RegisterConnectResult(errorCode);
                return errorCode;
            }

            using (var @event = new ManualResetEventSlim(false, 0))
            {
                var operation = new ConnectOperation {
                    Event = @event,
                    SocketAddress = socketAddress,
                    SocketAddressLen = socketAddressLen
                };

                bool isStopped;
                while (!TryBeginOperation(ref _acceptOrConnectQueue, operation, Interop.Sys.SocketEvents.Write, out isStopped))
                {
                    if (isStopped)
                    {
                        return SocketError.Interrupted;
                    }

                    if (operation.TryComplete(this))
                    {
                        return operation.ErrorCode;
                    }
                }

                return operation.Wait(timeout) ? operation.ErrorCode : SocketError.TimedOut;
            }
        }

        public SocketError ConnectAsync(byte[] socketAddress, int socketAddressLen, Action<SocketError> callback)
        {
            Debug.Assert(socketAddress != null, "Expected non-null socketAddress");
            Debug.Assert(socketAddressLen > 0, $"Unexpected socketAddressLen: {socketAddressLen}");
            Debug.Assert(callback != null, "Expected non-null callback");

            CheckForPriorConnectFailure();

            SetNonBlocking();

            SocketError errorCode;
            if (SocketPal.TryStartConnect(_socket, socketAddress, socketAddressLen, out errorCode))
            {
                RegisterConnectResult(errorCode);

                if (errorCode == SocketError.Success)
                {
                    ThreadPool.QueueUserWorkItem(arg => ((Action<SocketError>)arg)(SocketError.Success), callback);
                }

                return errorCode;
            }

            var operation = new ConnectOperation {
                Callback = callback,
                SocketAddress = socketAddress,
                SocketAddressLen = socketAddressLen
            };

            bool isStopped;
            while (!TryBeginOperation(ref _acceptOrConnectQueue, operation, Interop.Sys.SocketEvents.Write, out isStopped))
            {
                if (isStopped)
                {
                    return SocketError.OperationAborted;
                }

                if (operation.TryComplete(this))
                {
                    operation.QueueCompletionCallback();
                    break;
                }
            }
            return SocketError.IOPending;
        }

        public SocketError Receive(byte[] buffer, int offset, int count, ref SocketFlags flags, int timeout, out int bytesReceived)
        {
            int socketAddressLen = 0;
            return ReceiveFrom(buffer, offset, count, ref flags, null, ref socketAddressLen, timeout, out bytesReceived);
        }

        public SocketError ReceiveAsync(byte[] buffer, int offset, int count, SocketFlags flags, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            return ReceiveFromAsync(buffer, offset, count, flags, null, 0, callback);
        }

        public SocketError ReceiveFrom(byte[] buffer, int offset, int count, ref SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, int timeout, out int bytesReceived)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            if (SocketPal.TryCompleteReceiveFrom(_socket, buffer, offset, count, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode))
            {
                flags = receivedFlags;
                return errorCode;
            }

            using (var @event = new ManualResetEventSlim(false, 0))
            {
                var operation = new ReceiveOperation {
                    Event = @event,
                    Buffer = buffer,
                    Offset = offset,
                    Count = count,
                    Flags = flags,
                    SocketAddress = socketAddress,
                    SocketAddressLen = socketAddressLen,
                    BytesTransferred = bytesReceived,
                    ReceivedFlags = receivedFlags
                };

                bool isStopped;
                while (!TryBeginOperation(ref _receiveQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
                {
                    if (isStopped)
                    {
                        flags = operation.ReceivedFlags;
                        bytesReceived = operation.BytesTransferred;
                        return SocketError.Interrupted;
                    }

                    if (operation.TryComplete(this))
                    {
                        socketAddressLen = operation.SocketAddressLen;
                        flags = operation.ReceivedFlags;
                        bytesReceived = operation.BytesTransferred;
                        return operation.ErrorCode;
                    }
                }

                bool signaled = operation.Wait(timeout);
                socketAddressLen = operation.SocketAddressLen;
                flags = operation.ReceivedFlags;
                bytesReceived = operation.BytesTransferred;
                return signaled ? operation.ErrorCode : SocketError.TimedOut;
            }
        }

        public SocketError ReceiveFromAsync(byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            SetNonBlocking();

            int bytesReceived;
            SocketFlags receivedFlags;
            SocketError errorCode;
            if (SocketPal.TryCompleteReceiveFrom(_socket, buffer, offset, count, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode))
            {
                if (errorCode == SocketError.Success)
                {
                    ThreadPool.QueueUserWorkItem(args =>
                    {
                        var tup = (Tuple<Action<int, byte[], int, SocketFlags, SocketError>, int, byte[], int, SocketFlags>)args;
                        tup.Item1(tup.Item2, tup.Item3, tup.Item4, tup.Item5, SocketError.Success);
                    }, Tuple.Create(callback, bytesReceived, socketAddress, socketAddressLen, receivedFlags));
                }
                return errorCode;
            }

            var operation = new ReceiveOperation {
                Callback = callback,
                Buffer = buffer,
                Offset = offset,
                Count = count,
                Flags = flags,
                SocketAddress = socketAddress,
                SocketAddressLen = socketAddressLen,
                BytesTransferred = bytesReceived,
                ReceivedFlags = receivedFlags
            };

            bool isStopped;
            while (!TryBeginOperation(ref _receiveQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
            {
                if (isStopped)
                {
                    return SocketError.OperationAborted;
                }

                if (operation.TryComplete(this))
                {
                    operation.QueueCompletionCallback();
                    break;
                }
            }
            return SocketError.IOPending;
        }

        public SocketError Receive(IList<ArraySegment<byte>> buffers, ref SocketFlags flags, int timeout, out int bytesReceived)
        {
            return ReceiveFrom(buffers, ref flags, null, 0, timeout, out bytesReceived);
        }

        public SocketError ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            return ReceiveFromAsync(buffers, flags, null, 0, callback);
        }

        public SocketError ReceiveFrom(IList<ArraySegment<byte>> buffers, ref SocketFlags flags, byte[] socketAddress, int socketAddressLen, int timeout, out int bytesReceived)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            if (SocketPal.TryCompleteReceiveFrom(_socket, buffers, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode))
            {
                flags = receivedFlags;
                return errorCode;
            }

            using (var @event = new ManualResetEventSlim(false, 0))
            {
                var operation = new ReceiveOperation {
                    Event = @event,
                    Buffers = buffers,
                    Flags = flags,
                    SocketAddress = socketAddress,
                    SocketAddressLen = socketAddressLen,
                    BytesTransferred = bytesReceived,
                    ReceivedFlags = receivedFlags
                };

                bool isStopped;
                while (!TryBeginOperation(ref _receiveQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
                {
                    if (isStopped)
                    {
                        flags = operation.ReceivedFlags;
                        bytesReceived = operation.BytesTransferred;
                        return SocketError.Interrupted;
                    }

                    if (operation.TryComplete(this))
                    {
                        socketAddressLen = operation.SocketAddressLen;
                        flags = operation.ReceivedFlags;
                        bytesReceived = operation.BytesTransferred;
                        return operation.ErrorCode;
                    }
                }

                bool signaled = operation.Wait(timeout);
                socketAddressLen = operation.SocketAddressLen;
                flags = operation.ReceivedFlags;
                bytesReceived = operation.BytesTransferred;
                return signaled ? operation.ErrorCode : SocketError.TimedOut;
            }
        }

        public SocketError ReceiveFromAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, byte[] socketAddress, int socketAddressLen, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            SetNonBlocking();

            int bytesReceived;
            SocketFlags receivedFlags;
            SocketError errorCode;
            if (SocketPal.TryCompleteReceiveFrom(_socket, buffers, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode))
            {
                if (errorCode == SocketError.Success)
                {
                    ThreadPool.QueueUserWorkItem(args =>
                    {
                        var tup = (Tuple<Action<int, byte[], int, SocketFlags, SocketError>, int, byte[], int, SocketFlags>)args;
                        tup.Item1(tup.Item2, tup.Item3, tup.Item4, tup.Item5, SocketError.Success);
                    }, Tuple.Create(callback, bytesReceived, socketAddress, socketAddressLen, receivedFlags));
                }
                return errorCode;
            }

            var operation = new ReceiveOperation {
                Callback = callback,
                Buffers = buffers,
                Flags = flags,
                SocketAddress = socketAddress,
                SocketAddressLen = socketAddressLen,
                BytesTransferred = bytesReceived,
                ReceivedFlags = receivedFlags
            };

            bool isStopped;
            while (!TryBeginOperation(ref _receiveQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
            {
                if (isStopped)
                {
                    return SocketError.OperationAborted;
                }

                if (operation.TryComplete(this))
                {
                    operation.QueueCompletionCallback();
                    break;
                }
            }
            return SocketError.IOPending;
        }

        public SocketError ReceiveMessageFrom(byte[] buffer, int offset, int count, ref SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, bool isIPv4, bool isIPv6, int timeout, out IPPacketInformation ipPacketInformation, out int bytesReceived)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            if (SocketPal.TryCompleteReceiveMessageFrom(_socket, buffer, offset, count, flags, socketAddress, ref socketAddressLen, isIPv4, isIPv6, out bytesReceived, out receivedFlags, out ipPacketInformation, out errorCode))
            {
                flags = receivedFlags;
                return errorCode;
            }

            using (var @event = new ManualResetEventSlim(false, 0))
            {
                var operation = new ReceiveMessageFromOperation {
                    Event = @event,
                    Buffer = buffer,
                    Offset = offset,
                    Count = count,
                    Flags = flags,
                    SocketAddress = socketAddress,
                    SocketAddressLen = socketAddressLen,
                    IsIPv4 = isIPv4,
                    IsIPv6 = isIPv6,
                    BytesTransferred = bytesReceived,
                    ReceivedFlags = receivedFlags,
                    IPPacketInformation = ipPacketInformation,
                };

                bool isStopped;
                while (!TryBeginOperation(ref _receiveQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
                {
                    if (isStopped)
                    {
                        socketAddressLen = operation.SocketAddressLen;
                        flags = operation.ReceivedFlags;
                        ipPacketInformation = operation.IPPacketInformation;
                        bytesReceived = operation.BytesTransferred;
                        return SocketError.Interrupted;
                    }

                    if (operation.TryComplete(this))
                    {
                        socketAddressLen = operation.SocketAddressLen;
                        flags = operation.ReceivedFlags;
                        ipPacketInformation = operation.IPPacketInformation;
                        bytesReceived = operation.BytesTransferred;
                        return operation.ErrorCode;
                    }
                }

                bool signaled = operation.Wait(timeout);
                socketAddressLen = operation.SocketAddressLen;
                flags = operation.ReceivedFlags;
                ipPacketInformation = operation.IPPacketInformation;
                bytesReceived = operation.BytesTransferred;
                return signaled ? operation.ErrorCode : SocketError.TimedOut;
            }
        }

        public SocketError ReceiveMessageFromAsync(byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, bool isIPv4, bool isIPv6, Action<int, byte[], int, SocketFlags, IPPacketInformation, SocketError> callback)
        {
            SetNonBlocking();

            int bytesReceived;
            SocketFlags receivedFlags;
            IPPacketInformation ipPacketInformation;
            SocketError errorCode;
            if (SocketPal.TryCompleteReceiveMessageFrom(_socket, buffer, offset, count, flags, socketAddress, ref socketAddressLen, isIPv4, isIPv6, out bytesReceived, out receivedFlags, out ipPacketInformation, out errorCode))
            {
                if (errorCode == SocketError.Success)
                {
                    ThreadPool.QueueUserWorkItem(args =>
                    {
                        var tup = (Tuple<Action<int, byte[], int, SocketFlags, IPPacketInformation, SocketError>, int, byte[], int, SocketFlags, IPPacketInformation>)args;
                        tup.Item1(tup.Item2, tup.Item3, tup.Item4, tup.Item5, tup.Item6, SocketError.Success);
                    }, Tuple.Create(callback, bytesReceived, socketAddress, socketAddressLen, receivedFlags, ipPacketInformation));
                }
                return errorCode;
            }

            var operation = new ReceiveMessageFromOperation {
                Callback = callback,
                Buffer = buffer,
                Offset = offset,
                Count = count,
                Flags = flags,
                SocketAddress = socketAddress,
                SocketAddressLen = socketAddressLen,
                IsIPv4 = isIPv4,
                IsIPv6 = isIPv6,
                BytesTransferred = bytesReceived,
                ReceivedFlags = receivedFlags,
                IPPacketInformation = ipPacketInformation,
            };

            bool isStopped;
            while (!TryBeginOperation(ref _receiveQueue, operation, Interop.Sys.SocketEvents.Read, out isStopped))
            {
                if (isStopped)
                {
                    return SocketError.OperationAborted;
                }

                if (operation.TryComplete(this))
                {
                    operation.QueueCompletionCallback();
                    break;
                }
            }
            return SocketError.IOPending;
        }

        public SocketError Send(byte[] buffer, int offset, int count, SocketFlags flags, int timeout, out int bytesSent)
        {
            return SendTo(buffer, offset, count, flags, null, 0, timeout, out bytesSent);
        }

        public SocketError SendAsync(byte[] buffer, int offset, int count, SocketFlags flags, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            return SendToAsync(buffer, offset, count, flags, null, 0, callback);
        }

        public SocketError SendTo(byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, int timeout, out int bytesSent)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            SocketError errorCode;
            if (SocketPal.TryCompleteSendTo(_socket, buffer, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode))
            {
                return errorCode;
            }

            using (var @event = new ManualResetEventSlim(false, 0))
            {
                var operation = new SendOperation {
                    Event = @event,
                    Buffer = buffer,
                    Offset = offset,
                    Count = count,
                    Flags = flags,
                    SocketAddress = socketAddress,
                    SocketAddressLen = socketAddressLen,
                    BytesTransferred = bytesSent
                };

                bool isStopped;
                while (!TryBeginOperation(ref _sendQueue, operation, Interop.Sys.SocketEvents.Write, out isStopped))
                {
                    if (isStopped)
                    {
                        bytesSent = operation.BytesTransferred;
                        return SocketError.Interrupted;
                    }

                    if (operation.TryComplete(this))
                    {
                        bytesSent = operation.BytesTransferred;
                        return operation.ErrorCode;
                    }
                }

                bool signaled = operation.Wait(timeout);
                bytesSent = operation.BytesTransferred;
                return signaled ? operation.ErrorCode : SocketError.TimedOut;
            }
        }

        public SocketError SendToAsync(byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            SetNonBlocking();

            int bytesSent = 0;
            SocketError errorCode;
            if (SocketPal.TryCompleteSendTo(_socket, buffer, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode))
            {
                if (errorCode == SocketError.Success)
                {
                    ThreadPool.QueueUserWorkItem(args =>
                    {
                        var tup = (Tuple<Action<int, byte[], int, SocketFlags, SocketError>, int, byte[], int>)args;
                        tup.Item1(tup.Item2, tup.Item3, tup.Item4, 0, SocketError.Success);
                    }, Tuple.Create(callback, bytesSent, socketAddress, socketAddressLen));
                }
                return errorCode;
            }

            var operation = new SendOperation {
                Callback = callback,
                Buffer = buffer,
                Offset = offset,
                Count = count,
                Flags = flags,
                SocketAddress = socketAddress,
                SocketAddressLen = socketAddressLen,
                BytesTransferred = bytesSent
            };

            bool isStopped;
            while (!TryBeginOperation(ref _sendQueue, operation, Interop.Sys.SocketEvents.Write, out isStopped))
            {
                if (isStopped)
                {
                    return SocketError.OperationAborted;
                }

                if (operation.TryComplete(this))
                {
                    operation.QueueCompletionCallback();
                    break;
                }
            }
            return SocketError.IOPending;
        }

        public SocketError Send(IList<ArraySegment<byte>> buffers, SocketFlags flags, int timeout, out int bytesSent)
        {
            return SendTo(buffers, flags, null, 0, timeout, out bytesSent);
        }

        public SocketError SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            return SendToAsync(buffers, flags, null, 0, callback);
        }

        public SocketError SendTo(IList<ArraySegment<byte>> buffers, SocketFlags flags, byte[] socketAddress, int socketAddressLen, int timeout, out int bytesSent)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            int bufferIndex = 0;
            int offset = 0;
            SocketError errorCode;
            if (SocketPal.TryCompleteSendTo(_socket, buffers, ref bufferIndex, ref offset, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode))
            {
                return errorCode;
            }

            using (var @event = new ManualResetEventSlim(false, 0))
            {
                var operation = new SendOperation {
                    Event = @event,
                    Buffers = buffers,
                    BufferIndex = bufferIndex,
                    Offset = offset,
                    Flags = flags,
                    SocketAddress = socketAddress,
                    SocketAddressLen = socketAddressLen,
                    BytesTransferred = bytesSent
                };

                bool isStopped;
                while (!TryBeginOperation(ref _sendQueue, operation, Interop.Sys.SocketEvents.Write, out isStopped))
                {
                    if (isStopped)
                    {
                        bytesSent = operation.BytesTransferred;
                        return SocketError.Interrupted;
                    }

                    if (operation.TryComplete(this))
                    {
                        bytesSent = operation.BytesTransferred;
                        return operation.ErrorCode;
                    }
                }

                bool signaled = operation.Wait(timeout);
                bytesSent = operation.BytesTransferred;
                return signaled ? operation.ErrorCode : SocketError.TimedOut;
            }
        }

        public SocketError SendToAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, byte[] socketAddress, int socketAddressLen, Action<int, byte[], int, SocketFlags, SocketError> callback)
        {
            SetNonBlocking();

            int bufferIndex = 0;
            int offset = 0;
            int bytesSent = 0;
            SocketError errorCode;
            if (SocketPal.TryCompleteSendTo(_socket, buffers, ref bufferIndex, ref offset, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode))
            {
                if (errorCode == SocketError.Success)
                {
                    ThreadPool.QueueUserWorkItem(args =>
                    {
                        var tup = (Tuple<Action<int, byte[], int, SocketFlags, SocketError>, int, byte[], int>)args;
                        tup.Item1(tup.Item2, tup.Item3, tup.Item4, SocketFlags.None, SocketError.Success);
                    }, Tuple.Create(callback, bytesSent, socketAddress, socketAddressLen));
                }
                return errorCode;
            }

            var operation = new SendOperation {
                Callback = callback,
                Buffers = buffers,
                BufferIndex = bufferIndex,
                Offset = offset,
                Flags = flags,
                SocketAddress = socketAddress,
                SocketAddressLen = socketAddressLen,
                BytesTransferred = bytesSent
            };

            bool isStopped;
            while (!TryBeginOperation(ref _sendQueue, operation, Interop.Sys.SocketEvents.Write, out isStopped))
            {
                if (isStopped)
                {
                    return SocketError.OperationAborted;
                }

                if (operation.TryComplete(this))
                {
                    operation.QueueCompletionCallback();
                    break;
                }
            }
            return SocketError.IOPending;
        }

        public unsafe void HandleEvents(Interop.Sys.SocketEvents events)
        {
            lock (_queueLock)
            {
                if ((events & Interop.Sys.SocketEvents.Error) != 0)
                {
                    // Set the Read and Write flags as well; the processing for these events
                    // will pick up the error.
                    events |= Interop.Sys.SocketEvents.Read | Interop.Sys.SocketEvents.Write;
                }

                if ((events & Interop.Sys.SocketEvents.Read) != 0)
                {
                    if (_acceptOrConnectQueue.AllOfType<AcceptOperation>())
                    {
                        _acceptOrConnectQueue.Complete(this);
                    }

                    _receiveQueue.Complete(this);
                }

                if ((events & Interop.Sys.SocketEvents.Write) != 0)
                {
                    if (_acceptOrConnectQueue.AllOfType<ConnectOperation>())
                    {
                        _acceptOrConnectQueue.Complete(this);
                    }

                    _sendQueue.Complete(this);
                }
            }
        }
    }
}
