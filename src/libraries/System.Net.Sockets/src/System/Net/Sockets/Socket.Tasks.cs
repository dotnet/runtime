// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        /// <summary>Cached task with a 0 value.</summary>
        private static readonly Task<int> s_zeroTask = Task.FromResult(0);

        /// <summary>Cached instance for accept operations.</summary>
        private TaskSocketAsyncEventArgs<Socket>? _acceptEventArgs;

        /// <summary>Cached instance for receive operations that return <see cref="ValueTask{Int32}"/>. Also used for ConnectAsync operations.</summary>
        private AwaitableSocketAsyncEventArgs? _singleBufferReceiveEventArgs;
        /// <summary>Cached instance for send operations that return <see cref="ValueTask{Int32}"/>.</summary>
        private AwaitableSocketAsyncEventArgs? _singleBufferSendEventArgs;

        /// <summary>Cached instance for receive operations that return <see cref="Task{Int32}"/>.</summary>
        private TaskSocketAsyncEventArgs<int>? _multiBufferReceiveEventArgs;
        /// <summary>Cached instance for send operations that return <see cref="Task{Int32}"/>.</summary>
        private TaskSocketAsyncEventArgs<int>? _multiBufferSendEventArgs;

        internal Task<Socket> AcceptAsync(Socket? acceptSocket)
        {
            // Get any cached SocketAsyncEventArg we may have.
            TaskSocketAsyncEventArgs<Socket>? saea = Interlocked.Exchange(ref _acceptEventArgs, null);
            if (saea is null)
            {
                saea = new TaskSocketAsyncEventArgs<Socket>();
                saea.Completed += (s, e) => CompleteAccept((Socket)s!, (TaskSocketAsyncEventArgs<Socket>)e);
            }

            // Configure the SAEA.
            saea.AcceptSocket = acceptSocket;

            // Initiate the accept operation.
            Task<Socket> t;
            if (AcceptAsync(saea))
            {
                // The operation is completing asynchronously (it may have already completed).
                // Get the task for the operation, with appropriate synchronization to coordinate
                // with the async callback that'll be completing the task.
                bool responsibleForReturningToPool;
                t = saea.GetCompletionResponsibility(out responsibleForReturningToPool).Task;
                if (responsibleForReturningToPool)
                {
                    // We're responsible for returning it only if the callback has already been invoked
                    // and gotten what it needs from the SAEA; otherwise, the callback will return it.
                    ReturnSocketAsyncEventArgs(saea);
                }
            }
            else
            {
                // The operation completed synchronously.  Get a task for it.
                t = saea.SocketError == SocketError.Success ?
                    Task.FromResult(saea.AcceptSocket!) :
                    Task.FromException<Socket>(GetException(saea.SocketError));

                // There won't be a callback, and we're done with the SAEA, so return it to the pool.
                ReturnSocketAsyncEventArgs(saea);
            }

            return t;
        }

        internal Task ConnectAsync(EndPoint remoteEP)
        {
            // Use _singleBufferReceiveEventArgs so the AwaitableSocketAsyncEventArgs can be re-used later for receives.
            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

            saea.RemoteEndPoint = remoteEP;
            return saea.ConnectAsync(this).AsTask();
        }

        internal Task ConnectAsync(IPAddress address, int port) => ConnectAsync(new IPEndPoint(address, port));

        internal Task ConnectAsync(IPAddress[] addresses, int port)
        {
            if (addresses == null)
            {
                throw new ArgumentNullException(nameof(addresses));
            }
            if (addresses.Length == 0)
            {
                throw new ArgumentException(SR.net_invalidAddressList, nameof(addresses));
            }

            return DoConnectAsync(addresses, port);
        }

        private async Task DoConnectAsync(IPAddress[] addresses, int port)
        {
            Exception? lastException = null;
            foreach (IPAddress address in addresses)
            {
                try
                {
                    await ConnectAsync(address, port).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            Debug.Assert(lastException != null);
            ExceptionDispatchInfo.Throw(lastException);
        }

        internal Task ConnectAsync(string host, int port)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            EndPoint ep = IPAddress.TryParse(host, out IPAddress? parsedAddress) ? (EndPoint)
                new IPEndPoint(parsedAddress, port) :
                new DnsEndPoint(host, port);
            return ConnectAsync(ep);
        }

        internal Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream)
        {
            ValidateBuffer(buffer);
            return ReceiveAsync(buffer, socketFlags, fromNetworkStream, default).AsTask();
        }

        internal ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(buffer);
            saea.SocketFlags = socketFlags;
            saea.WrapExceptionsInNetworkExceptions = fromNetworkStream;
            return saea.ReceiveAsync(this, cancellationToken);
        }

        internal Task<int> ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            ValidateBuffersList(buffers);

            TaskSocketAsyncEventArgs<int>? saea = Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null);
            if (saea is null)
            {
                saea = new TaskSocketAsyncEventArgs<int>();
                saea.Completed += (s, e) => CompleteSendReceive((Socket)s!, (TaskSocketAsyncEventArgs<int>)e, isReceive: true);
            }

            saea.BufferList = buffers;
            saea.SocketFlags = socketFlags;
            return GetTaskForSendReceive(ReceiveAsync(saea), saea, fromNetworkStream: false, isReceive: true);
        }

        internal Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
        {
            var tcs = new StateTaskCompletionSource<EndPoint, SocketReceiveFromResult>(this) { _field1 = remoteEndPoint };
            BeginReceiveFrom(buffer.Array!, buffer.Offset, buffer.Count, socketFlags, ref tcs._field1, iar =>
            {
                var innerTcs = (StateTaskCompletionSource<EndPoint, SocketReceiveFromResult>)iar.AsyncState!;
                try
                {
                    int receivedBytes = ((Socket)innerTcs.Task.AsyncState!).EndReceiveFrom(iar, ref innerTcs._field1);
                    innerTcs.TrySetResult(new SocketReceiveFromResult
                    {
                        ReceivedBytes = receivedBytes,
                        RemoteEndPoint = innerTcs._field1
                    });
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
        {
            var tcs = new StateTaskCompletionSource<SocketFlags, EndPoint, SocketReceiveMessageFromResult>(this) { _field1 = socketFlags, _field2 = remoteEndPoint };
            BeginReceiveMessageFrom(buffer.Array!, buffer.Offset, buffer.Count, socketFlags, ref tcs._field2, iar =>
            {
                var innerTcs = (StateTaskCompletionSource<SocketFlags, EndPoint, SocketReceiveMessageFromResult>)iar.AsyncState!;
                try
                {
                    IPPacketInformation ipPacketInformation;
                    int receivedBytes = ((Socket)innerTcs.Task.AsyncState!).EndReceiveMessageFrom(iar, ref innerTcs._field1, ref innerTcs._field2, out ipPacketInformation);
                    innerTcs.TrySetResult(new SocketReceiveMessageFromResult
                    {
                        ReceivedBytes = receivedBytes,
                        RemoteEndPoint = innerTcs._field2,
                        SocketFlags = innerTcs._field1,
                        PacketInformation = ipPacketInformation
                    });
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            ValidateBuffer(buffer);
            return SendAsync(buffer, socketFlags, default).AsTask();
        }

        internal ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
            saea.SocketFlags = socketFlags;
            saea.WrapExceptionsInNetworkExceptions = false;
            return saea.SendAsync(this, cancellationToken);
        }

        internal ValueTask SendAsyncForNetworkStream(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
            saea.SocketFlags = socketFlags;
            saea.WrapExceptionsInNetworkExceptions = true;
            return saea.SendAsyncForNetworkStream(this, cancellationToken);
        }

        internal Task<int> SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            ValidateBuffersList(buffers);

            TaskSocketAsyncEventArgs<int>? saea = Interlocked.Exchange(ref _multiBufferSendEventArgs, null);
            if (saea is null)
            {
                saea = new TaskSocketAsyncEventArgs<int>();
                saea.Completed += (s, e) => CompleteSendReceive((Socket)s!, (TaskSocketAsyncEventArgs<int>)e, isReceive: false);
            }

            saea.BufferList = buffers;
            saea.SocketFlags = socketFlags;
            return GetTaskForSendReceive(SendAsync(saea), saea, fromNetworkStream: false, isReceive: false);
        }

        internal Task<int> SendToAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
        {
            var tcs = new TaskCompletionSource<int>(this);
            BeginSendTo(buffer.Array!, buffer.Offset, buffer.Count, socketFlags, remoteEP, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState!;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState!).EndSendTo(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        /// <summary>Validates the supplied array segment, throwing if its array or indices are null or out-of-bounds, respectively.</summary>
        private static void ValidateBuffer(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
            {
                throw new ArgumentNullException(nameof(buffer.Array));
            }
            if ((uint)buffer.Offset > buffer.Array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Offset));
            }
            if ((uint)buffer.Count > buffer.Array.Length - buffer.Offset)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Count));
            }
        }

        /// <summary>Validates the supplied buffer list, throwing if it's null or empty.</summary>
        private static void ValidateBuffersList(IList<ArraySegment<byte>> buffers)
        {
            if (buffers == null)
            {
                throw new ArgumentNullException(nameof(buffers));
            }
            if (buffers.Count == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_sockets_zerolist, nameof(buffers)), nameof(buffers));
            }
        }

        /// <summary>Gets a task to represent the operation.</summary>
        /// <param name="pending">true if the operation completes asynchronously; false if it completed synchronously.</param>
        /// <param name="saea">The event args instance used with the operation.</param>
        /// <param name="fromNetworkStream">
        /// true if the request is coming from NetworkStream, which has special semantics for
        /// exceptions and cached tasks; otherwise, false.
        /// </param>
        /// <param name="isReceive">true if this is a receive; false if this is a send.</param>
        private Task<int> GetTaskForSendReceive(bool pending, TaskSocketAsyncEventArgs<int> saea, bool fromNetworkStream, bool isReceive)
        {
            Task<int> t;

            if (pending)
            {
                // The operation is completing asynchronously (it may have already completed).
                // Get the task for the operation, with appropriate synchronization to coordinate
                // with the async callback that'll be completing the task.
                bool responsibleForReturningToPool;
                t = saea.GetCompletionResponsibility(out responsibleForReturningToPool).Task;
                if (responsibleForReturningToPool)
                {
                    // We're responsible for returning it only if the callback has already been invoked
                    // and gotten what it needs from the SAEA; otherwise, the callback will return it.
                    ReturnSocketAsyncEventArgs(saea, isReceive);
                }
            }
            else
            {
                // The operation completed synchronously.  Get a task for it.
                if (saea.SocketError == SocketError.Success)
                {
                    // Get the number of bytes successfully received/sent.
                    int bytesTransferred = saea.BytesTransferred;

                    // For zero bytes transferred, we can return our cached 0 task.
                    // We can also do so if the request came from network stream and is a send,
                    // as for that we can return any value because it returns a non-generic Task.
                    if (bytesTransferred == 0 || (fromNetworkStream & !isReceive))
                    {
                        t = s_zeroTask;
                    }
                    else
                    {
                        // Otherwise, create a new task for this result value.
                        t = Task.FromResult(bytesTransferred);
                    }
                }
                else
                {
                    t = Task.FromException<int>(GetException(saea.SocketError, wrapExceptionsInIOExceptions: fromNetworkStream));
                }

                // There won't be a callback, and we're done with the SAEA, so return it to the pool.
                ReturnSocketAsyncEventArgs(saea, isReceive);
            }

            return t;
        }

        /// <summary>Completes the SocketAsyncEventArg's Task with the result of the send or receive, and returns it to the specified pool.</summary>
        private static void CompleteAccept(Socket s, TaskSocketAsyncEventArgs<Socket> saea)
        {
            // Pull the relevant state off of the SAEA
            SocketError error = saea.SocketError;
            Socket? acceptSocket = saea.AcceptSocket;

            // Synchronize with the initiating thread. If the synchronous caller already got what
            // it needs from the SAEA, then we can return it to the pool now. Otherwise, it'll be
            // responsible for returning it once it's gotten what it needs from it.
            bool responsibleForReturningToPool;
            AsyncTaskMethodBuilder<Socket> builder = saea.GetCompletionResponsibility(out responsibleForReturningToPool);
            if (responsibleForReturningToPool)
            {
                s.ReturnSocketAsyncEventArgs(saea);
            }

            // Complete the builder/task with the results.
            if (error == SocketError.Success)
            {
                builder.SetResult(acceptSocket!);
            }
            else
            {
                builder.SetException(GetException(error));
            }
        }

        /// <summary>Completes the SocketAsyncEventArg's Task with the result of the send or receive, and returns it to the specified pool.</summary>
        private static void CompleteSendReceive(Socket s, TaskSocketAsyncEventArgs<int> saea, bool isReceive)
        {
            // Pull the relevant state off of the SAEA
            SocketError error = saea.SocketError;
            int bytesTransferred = saea.BytesTransferred;
            bool wrapExceptionsInIOExceptions = saea._wrapExceptionsInIOExceptions;

            // Synchronize with the initiating thread. If the synchronous caller already got what
            // it needs from the SAEA, then we can return it to the pool now. Otherwise, it'll be
            // responsible for returning it once it's gotten what it needs from it.
            bool responsibleForReturningToPool;
            AsyncTaskMethodBuilder<int> builder = saea.GetCompletionResponsibility(out responsibleForReturningToPool);
            if (responsibleForReturningToPool)
            {
                s.ReturnSocketAsyncEventArgs(saea, isReceive);
            }

            // Complete the builder/task with the results.
            if (error == SocketError.Success)
            {
                builder.SetResult(bytesTransferred);
            }
            else
            {
                builder.SetException(GetException(error, wrapExceptionsInIOExceptions));
            }
        }

        /// <summary>Gets a SocketException or an IOException wrapping a SocketException for the specified error.</summary>
        private static Exception GetException(SocketError error, bool wrapExceptionsInIOExceptions = false)
        {
            Exception e = ExceptionDispatchInfo.SetCurrentStackTrace(new SocketException((int)error));
            return wrapExceptionsInIOExceptions ?
                new IOException(SR.Format(SR.net_io_readwritefailure, e.Message), e) :
                e;
        }

        /// <summary>Returns a <see cref="TaskSocketAsyncEventArgs{TResult}"/> instance for reuse.</summary>
        /// <param name="saea">The instance to return.</param>
        /// <param name="isReceive">true if this instance is used for receives; false if used for sends.</param>
        private void ReturnSocketAsyncEventArgs(TaskSocketAsyncEventArgs<int> saea, bool isReceive)
        {
            // Reset state on the SAEA before returning it.  But do not reset buffer state.  That'll be done
            // if necessary by the consumer, but we want to keep the buffers due to likely subsequent reuse
            // and the costs associated with changing them.
            saea._accessed = false;
            saea._builder = default;
            saea._wrapExceptionsInIOExceptions = false;

            // Write this instance back as a cached instance, only if there isn't currently one cached.
            ref TaskSocketAsyncEventArgs<int>? cache = ref isReceive ? ref _multiBufferReceiveEventArgs : ref _multiBufferSendEventArgs;
            if (Interlocked.CompareExchange(ref cache, saea, null) != null)
            {
                saea.Dispose();
            }
        }

        /// <summary>Returns a <see cref="TaskSocketAsyncEventArgs{TResult}"/> instance for reuse.</summary>
        /// <param name="saea">The instance to return.</param>
        private void ReturnSocketAsyncEventArgs(TaskSocketAsyncEventArgs<Socket> saea)
        {
            // Reset state on the SAEA before returning it.  But do not reset buffer state.  That'll be done
            // if necessary by the consumer, but we want to keep the buffers due to likely subsequent reuse
            // and the costs associated with changing them.
            saea.AcceptSocket = null;
            saea._accessed = false;
            saea._builder = default;

            // Write this instance back as a cached instance, only if there isn't currently one cached.
            if (Interlocked.CompareExchange(ref _acceptEventArgs, saea, null) != null)
            {
                // Couldn't return it, so dispose it.
                saea.Dispose();
            }
        }

        /// <summary>Dispose of any cached <see cref="TaskSocketAsyncEventArgs{TResult}"/> instances.</summary>
        private void DisposeCachedTaskSocketAsyncEventArgs()
        {
            Interlocked.Exchange(ref _acceptEventArgs, null)?.Dispose();
            Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null)?.Dispose();
            Interlocked.Exchange(ref _multiBufferSendEventArgs, null)?.Dispose();
            Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null)?.Dispose();
            Interlocked.Exchange(ref _singleBufferSendEventArgs, null)?.Dispose();
        }

        /// <summary>A TaskCompletionSource that carries an extra field of strongly-typed state.</summary>
        private sealed class StateTaskCompletionSource<TField1, TResult> : TaskCompletionSource<TResult>
        {
            internal TField1 _field1 = default!; // always set on construction
            public StateTaskCompletionSource(object baseState) : base(baseState) { }
        }

        /// <summary>A TaskCompletionSource that carries several extra fields of strongly-typed state.</summary>
        private sealed class StateTaskCompletionSource<TField1, TField2, TResult> : TaskCompletionSource<TResult>
        {
            internal TField1 _field1 = default!; // always set on construction
            internal TField2 _field2 = default!; // always set on construction
            public StateTaskCompletionSource(object baseState) : base(baseState) { }
        }

        /// <summary>A SocketAsyncEventArgs with an associated async method builder.</summary>
        private sealed class TaskSocketAsyncEventArgs<TResult> : SocketAsyncEventArgs
        {
            /// <summary>
            /// The builder used to create the Task representing the result of the async operation.
            /// This is a mutable struct.
            /// </summary>
            internal AsyncTaskMethodBuilder<TResult> _builder;
            /// <summary>
            /// Whether the instance was already accessed as part of the operation.  We expect
            /// at most two accesses: one from the synchronous caller to initiate the operation,
            /// and one from the callback if the operation completes asynchronously.  If it completes
            /// synchronously, then it's the initiator's responsbility to return the instance to
            /// the pool.  If it completes asynchronously, then it's the responsibility of whoever
            /// accesses this second, so we track whether it's already been accessed.
            /// </summary>
            internal bool _accessed;
            /// <summary>Whether exceptions that emerge should be wrapped in IOExceptions.</summary>
            internal bool _wrapExceptionsInIOExceptions;

            internal TaskSocketAsyncEventArgs() :
                base(unsafeSuppressExecutionContextFlow: true) // avoid flowing context at lower layers as we only expose Task, which handles it
            {
            }

            /// <summary>Gets the builder's task with appropriate synchronization.</summary>
            internal AsyncTaskMethodBuilder<TResult> GetCompletionResponsibility(out bool responsibleForReturningToPool)
            {
                lock (this)
                {
                    responsibleForReturningToPool = _accessed;
                    _accessed = true;
                    _ = _builder.Task; // force initialization under the lock (builder itself lazily initializes w/o synchronization)
                    return _builder;
                }
            }
        }

        /// <summary>A SocketAsyncEventArgs that can be awaited to get the result of an operation.</summary>
        internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource, IValueTaskSource<int>
        {
            private static readonly Action<object?> s_completedSentinel = new Action<object?>(state => throw new InvalidOperationException(SR.Format(SR.net_sockets_valuetaskmisuse, nameof(s_completedSentinel))));
            /// <summary>The owning socket.</summary>
            private readonly Socket _owner;
            /// <summary>Whether this should be cached as a read or a write on the <see cref="_owner"/></summary>
            private bool _isReadForCaching;
            /// <summary>
            /// <see cref="s_completedSentinel"/> if it has completed. Another delegate if OnCompleted was called before the operation could complete,
            /// in which case it's the delegate to invoke when the operation does complete.
            /// </summary>
            private Action<object?>? _continuation;
            private ExecutionContext? _executionContext;
            private object? _scheduler;
            /// <summary>Current token value given to a ValueTask and then verified against the value it passes back to us.</summary>
            /// <remarks>
            /// This is not meant to be a completely reliable mechanism, doesn't require additional synchronization, etc.
            /// It's purely a best effort attempt to catch misuse, including awaiting for a value task twice and after
            /// it's already being reused by someone else.
            /// </remarks>
            private short _token;
            /// <summary>The cancellation token used for the current operation.</summary>
            private CancellationToken _cancellationToken;

            /// <summary>Initializes the event args.</summary>
            public AwaitableSocketAsyncEventArgs(Socket owner, bool isReceiveForCaching) :
                base(unsafeSuppressExecutionContextFlow: true) // avoid flowing context at lower layers as we only expose ValueTask, which handles it
            {
                _owner = owner;
                _isReadForCaching = isReceiveForCaching;
            }

            public bool WrapExceptionsInNetworkExceptions { get; set; }

            private void Release()
            {
                _cancellationToken = default;
                _token++;
                _continuation = null;

                ref AwaitableSocketAsyncEventArgs? cache = ref _isReadForCaching ? ref _owner._singleBufferReceiveEventArgs : ref _owner._singleBufferSendEventArgs;
                if (Interlocked.CompareExchange(ref cache, this, null) != null)
                {
                    Dispose();
                }
            }

            protected override void OnCompleted(SocketAsyncEventArgs _)
            {
                // When the operation completes, see if OnCompleted was already called to hook up a continuation.
                // If it was, invoke the continuation.
                Action<object?>? c = _continuation;
                if (c != null || (c = Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null)) != null)
                {
                    Debug.Assert(c != s_completedSentinel, "The delegate should not have been the completed sentinel.");

                    object? continuationState = UserToken;
                    UserToken = null;
                    _continuation = s_completedSentinel; // in case someone's polling IsCompleted

                    ExecutionContext? ec = _executionContext;
                    if (ec == null)
                    {
                        InvokeContinuation(c, continuationState, forceAsync: false, requiresExecutionContextFlow: false);
                    }
                    else
                    {
                        // This case should be relatively rare, as the async Task/ValueTask method builders
                        // use the awaiter's UnsafeOnCompleted, so this will only happen with code that
                        // explicitly uses the awaiter's OnCompleted instead.
                        _executionContext = null;
                        ExecutionContext.Run(ec, runState =>
                        {
                            var t = (Tuple<AwaitableSocketAsyncEventArgs, Action<object?>, object>)runState!;
                            t.Item1.InvokeContinuation(t.Item2, t.Item3, forceAsync: false, requiresExecutionContextFlow: false);
                        }, Tuple.Create(this, c, continuationState));
                    }
                }
            }

            /// <summary>Initiates a receive operation on the associated socket.</summary>
            /// <returns>This instance.</returns>
            public ValueTask<int> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
            {
                Debug.Assert(Volatile.Read(ref _continuation) == null, $"Expected null continuation to indicate reserved for use");

                if (socket.ReceiveAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask<int>(this, _token);
                }

                int bytesTransferred = BytesTransferred;
                SocketError error = SocketError;

                Release();

                return error == SocketError.Success ?
                    new ValueTask<int>(bytesTransferred) :
                    ValueTask.FromException<int>(CreateException(error));
            }

            /// <summary>Initiates a send operation on the associated socket.</summary>
            /// <returns>This instance.</returns>
            public ValueTask<int> SendAsync(Socket socket, CancellationToken cancellationToken)
            {
                Debug.Assert(Volatile.Read(ref _continuation) == null, $"Expected null continuation to indicate reserved for use");

                if (socket.SendAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask<int>(this, _token);
                }

                int bytesTransferred = BytesTransferred;
                SocketError error = SocketError;

                Release();

                return error == SocketError.Success ?
                    new ValueTask<int>(bytesTransferred) :
                    ValueTask.FromException<int>(CreateException(error));
            }

            public ValueTask SendAsyncForNetworkStream(Socket socket, CancellationToken cancellationToken)
            {
                Debug.Assert(Volatile.Read(ref _continuation) == null, $"Expected null continuation to indicate reserved for use");

                if (socket.SendAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask(this, _token);
                }

                SocketError error = SocketError;

                Release();

                return error == SocketError.Success ?
                    default :
                    ValueTask.FromException(CreateException(error));
            }

            public ValueTask ConnectAsync(Socket socket)
            {
                Debug.Assert(Volatile.Read(ref _continuation) == null, $"Expected null continuation to indicate reserved for use");

                try
                {
                    if (socket.ConnectAsync(this))
                    {
                        return new ValueTask(this, _token);
                    }
                }
                catch
                {
                    Release();
                    throw;
                }

                SocketError error = SocketError;

                Release();

                return error == SocketError.Success ?
                    default :
                    ValueTask.FromException(CreateException(error));
            }

            /// <summary>Gets the status of the operation.</summary>
            public ValueTaskSourceStatus GetStatus(short token)
            {
                if (token != _token)
                {
                    ThrowIncorrectTokenException();
                }

                return
                    !ReferenceEquals(_continuation, s_completedSentinel) ? ValueTaskSourceStatus.Pending :
                    SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                    ValueTaskSourceStatus.Faulted;
            }

            /// <summary>Queues the provided continuation to be executed once the operation has completed.</summary>
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                if (token != _token)
                {
                    ThrowIncorrectTokenException();
                }

                if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
                {
                    _executionContext = ExecutionContext.Capture();
                }

                if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
                {
                    SynchronizationContext? sc = SynchronizationContext.Current;
                    if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                    {
                        _scheduler = sc;
                    }
                    else
                    {
                        TaskScheduler ts = TaskScheduler.Current;
                        if (ts != TaskScheduler.Default)
                        {
                            _scheduler = ts;
                        }
                    }
                }

                UserToken = state; // Use UserToken to carry the continuation state around
                Action<object>? prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
                if (ReferenceEquals(prevContinuation, s_completedSentinel))
                {
                    // Lost the race condition and the operation has now already completed.
                    // We need to invoke the continuation, but it must be asynchronously to
                    // avoid a stack dive.  However, since all of the queueing mechanisms flow
                    // ExecutionContext, and since we're still in the same context where we
                    // captured it, we can just ignore the one we captured.
                    bool requiresExecutionContextFlow = _executionContext != null;
                    _executionContext = null;
                    UserToken = null; // we have the state in "state"; no need for the one in UserToken
                    InvokeContinuation(continuation, state, forceAsync: true, requiresExecutionContextFlow);
                }
                else if (prevContinuation != null)
                {
                    // Flag errors with the continuation being hooked up multiple times.
                    // This is purely to help alert a developer to a bug they need to fix.
                    ThrowMultipleContinuationsException();
                }
            }

            private void InvokeContinuation(Action<object?> continuation, object? state, bool forceAsync, bool requiresExecutionContextFlow)
            {
                object? scheduler = _scheduler;
                _scheduler = null;

                if (scheduler != null)
                {
                    if (scheduler is SynchronizationContext sc)
                    {
                        sc.Post(s =>
                        {
                            var t = (Tuple<Action<object>, object>)s!;
                            t.Item1(t.Item2);
                        }, Tuple.Create(continuation, state));
                    }
                    else
                    {
                        Debug.Assert(scheduler is TaskScheduler, $"Expected TaskScheduler, got {scheduler}");
                        Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, (TaskScheduler)scheduler);
                    }
                }
                else if (forceAsync)
                {
                    if (requiresExecutionContextFlow)
                    {
                        ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
                    }
                }
                else
                {
                    continuation(state);
                }
            }

            /// <summary>Gets the result of the completion operation.</summary>
            /// <returns>Number of bytes transferred.</returns>
            /// <remarks>
            /// Unlike TaskAwaiter's GetResult, this does not block until the operation completes: it must only
            /// be used once the operation has completed.  This is handled implicitly by await.
            /// </remarks>
            public int GetResult(short token)
            {
                if (token != _token)
                {
                    ThrowIncorrectTokenException();
                }

                SocketError error = SocketError;
                int bytes = BytesTransferred;
                CancellationToken cancellationToken = _cancellationToken;

                Release();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }
                return bytes;
            }

            void IValueTaskSource.GetResult(short token)
            {
                if (token != _token)
                {
                    ThrowIncorrectTokenException();
                }

                SocketError error = SocketError;
                CancellationToken cancellationToken = _cancellationToken;

                Release();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }
            }

            private void ThrowIncorrectTokenException() => throw new InvalidOperationException(SR.InvalidOperation_IncorrectToken);

            private void ThrowMultipleContinuationsException() => throw new InvalidOperationException(SR.InvalidOperation_MultipleContinuations);

            private void ThrowException(SocketError error, CancellationToken cancellationToken)
            {
                if (error == SocketError.OperationAborted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw CreateException(error, forAsyncThrow: false);
            }

            private Exception CreateException(SocketError error, bool forAsyncThrow = true)
            {
                Exception e = new SocketException((int)error);

                if (forAsyncThrow)
                {
                    e = ExceptionDispatchInfo.SetCurrentStackTrace(e);
                }

                return WrapExceptionsInNetworkExceptions ?
                    NetworkErrorHelper.MapSocketException((SocketException)e) :
                    e;
            }
        }
    }
}
