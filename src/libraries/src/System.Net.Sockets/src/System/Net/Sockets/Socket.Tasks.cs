// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The Task-based APIs are currently wrappers over either the APM APIs (e.g. BeginConnect)
    // or the SocketAsyncEventArgs APIs (e.g. ReceiveAsync(SocketAsyncEventArgs)).  The latter
    // are much more efficient when the SocketAsyncEventArg instances can be reused; as such,
    // at present we use them for ReceiveAsync and SendAsync, caching an instance for each.
    // In the future we could potentially maintain a global cache of instances used for accepts
    // and connects, and potentially separate per-socket instances for Receive{Message}FromAsync,
    // which would need different instances from ReceiveAsync due to having different results
    // and thus different Completed logic.  We also currently fall back to APM implementations
    // when the single cached instance for each of send/receive is otherwise in use; we could
    // potentially also employ a global pool from which to pull in such situations.

    public partial class Socket
    {
        /// <summary>
        /// Sentinel that can be stored into one of the cached fields to indicate that an instance
        /// was previously created but is currently being used by another concurrent operation.
        /// </summary>
        private static readonly Int32TaskSocketAsyncEventArgs s_rentedSentinel = new Int32TaskSocketAsyncEventArgs();
        /// <summary>Cached task with a 0 value.</summary>
        private static readonly Task<int> s_zeroTask = Task.FromResult(0);
        /// <summary>Cached SocketAsyncEventArgs for Task-based ReceiveAsync APIs.</summary>
        private Int32TaskSocketAsyncEventArgs _cachedReceiveEventArgs;
        /// <summary>Cached SocketAsyncEventArgs for Task-based SendAsync APIs.</summary>
        private Int32TaskSocketAsyncEventArgs _cachedSendEventArgs;

        internal Task<Socket> AcceptAsync(Socket acceptSocket)
        {
            var tcs = new TaskCompletionSource<Socket>(this);
            BeginAccept(acceptSocket, 0, iar =>
            {
                var innerTcs = (TaskCompletionSource<Socket>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndAccept(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task ConnectAsync(EndPoint remoteEP)
        {
            var tcs = new TaskCompletionSource<bool>(this);
            BeginConnect(remoteEP, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task ConnectAsync(IPAddress address, int port)
        {
            var tcs = new TaskCompletionSource<bool>(this);
            BeginConnect(address, port, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task ConnectAsync(IPAddress[] addresses, int port)
        {
            var tcs = new TaskCompletionSource<bool>(this);
            BeginConnect(addresses, port, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task ConnectAsync(string host, int port)
        {
            var tcs = new TaskCompletionSource<bool>(this);
            BeginConnect(host, port, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream)
        {
            // Validate the arguments.
            ValidateBuffer(buffer);

            Int32TaskSocketAsyncEventArgs saea = RentSocketAsyncEventArgs(isReceive: true);
            if (saea != null)
            {
                // We got a cached instance. Configure the buffer and initate the operation.
                ConfigureBuffer(saea, buffer, socketFlags, wrapExceptionsInIOExceptions: fromNetworkStream);
                return GetTaskForSendReceive(ReceiveAsync(saea), saea, fromNetworkStream, isReceive: true);
            }
            else
            {
                // We couldn't get a cached instance, due to a concurrent receive operation on the socket.
                // Fall back to wrapping APM.
                return ReceiveAsyncApm(buffer, socketFlags);
            }
        }

        /// <summary>Implements Task-returning ReceiveAsync on top of Begin/EndReceive.</summary>
        private Task<int> ReceiveAsyncApm(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(this);
            BeginReceive(buffer.Array, buffer.Offset, buffer.Count, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndReceive(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task<int> ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            // Validate the arguments.
            ValidateBuffersList(buffers);

            Int32TaskSocketAsyncEventArgs saea = RentSocketAsyncEventArgs(isReceive: true);
            if (saea != null)
            {
                // We got a cached instance. Configure the buffer list and initate the operation.
                ConfigureBufferList(saea, buffers, socketFlags);
                return GetTaskForSendReceive(ReceiveAsync(saea), saea, fromNetworkStream: false, isReceive: true);
            }
            else
            {
                // We couldn't get a cached instance, due to a concurrent receive operation on the socket.
                // Fall back to wrapping APM.
                return ReceiveAsyncApm(buffers, socketFlags);
            }
        }

        /// <summary>Implements Task-returning ReceiveAsync on top of Begin/EndReceive.</summary>
        private Task<int> ReceiveAsyncApm(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(this);
            BeginReceive(buffers, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndReceive(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
        {
            var tcs = new StateTaskCompletionSource<EndPoint, SocketReceiveFromResult>(this) { _field1 = remoteEndPoint };
            BeginReceiveFrom(buffer.Array, buffer.Offset, buffer.Count, socketFlags, ref tcs._field1, iar =>
            {
                var innerTcs = (StateTaskCompletionSource<EndPoint, SocketReceiveFromResult>)iar.AsyncState;
                try
                {
                    int receivedBytes = ((Socket)innerTcs.Task.AsyncState).EndReceiveFrom(iar, ref innerTcs._field1);
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
            BeginReceiveMessageFrom(buffer.Array, buffer.Offset, buffer.Count, socketFlags, ref tcs._field2, iar =>
            {
                var innerTcs = (StateTaskCompletionSource<SocketFlags, EndPoint, SocketReceiveMessageFromResult>)iar.AsyncState;
                try
                {
                    IPPacketInformation ipPacketInformation;
                    int receivedBytes = ((Socket)innerTcs.Task.AsyncState).EndReceiveMessageFrom(iar, ref innerTcs._field1, ref innerTcs._field2, out ipPacketInformation);
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

        internal Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream)
        {
            // Validate the arguments.
            ValidateBuffer(buffer);
            
            Int32TaskSocketAsyncEventArgs saea = RentSocketAsyncEventArgs(isReceive: false);
            if (saea != null)
            {
                // We got a cached instance. Configure the buffer and initate the operation.
                ConfigureBuffer(saea, buffer, socketFlags, wrapExceptionsInIOExceptions: fromNetworkStream);
                return GetTaskForSendReceive(SendAsync(saea), saea, fromNetworkStream, isReceive: false);
            }
            else
            {
                // We couldn't get a cached instance, due to a concurrent send operation on the socket.
                // Fall back to wrapping APM.
                return SendAsyncApm(buffer, socketFlags);
            }
        }

        /// <summary>Implements Task-returning SendAsync on top of Begin/EndSend.</summary>
        private Task<int> SendAsyncApm(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(this);
            BeginSend(buffer.Array, buffer.Offset, buffer.Count, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSend(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task<int> SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            // Validate the arguments.
            ValidateBuffersList(buffers);

            Int32TaskSocketAsyncEventArgs saea = RentSocketAsyncEventArgs(isReceive: false);
            if (saea != null)
            {
                // We got a cached instance. Configure the buffer list and initate the operation.
                ConfigureBufferList(saea, buffers, socketFlags);
                return GetTaskForSendReceive(SendAsync(saea), saea, fromNetworkStream: false, isReceive: false);
            }
            else
            {
                // We couldn't get a cached instance, due to a concurrent send operation on the socket.
                // Fall back to wrapping APM.
                return SendAsyncApm(buffers, socketFlags);
            }
        }

        /// <summary>Implements Task-returning SendAsync on top of Begin/EndSend.</summary>
        private Task<int> SendAsyncApm(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(this);
            BeginSend(buffers, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSend(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        internal Task<int> SendToAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
        {
            var tcs = new TaskCompletionSource<int>(this);
            BeginSendTo(buffer.Array, buffer.Offset, buffer.Count, socketFlags, remoteEP, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSendTo(iar)); }
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
            if (buffer.Offset < 0 || buffer.Offset > buffer.Array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Offset));
            }
            if (buffer.Count < 0 || buffer.Count > buffer.Array.Length - buffer.Offset)
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

        private static void ConfigureBuffer(
            Int32TaskSocketAsyncEventArgs saea, ArraySegment<byte> buffer, SocketFlags socketFlags, bool wrapExceptionsInIOExceptions)
        {
            // Configure the buffer.  We don't clear the buffers when returning the SAEA to the pool,
            // so as to minimize overhead if the same buffer is used for subsequent operations (which is likely).
            // But SAEA doesn't support having both a buffer and a buffer list configured, so clear out a buffer list
            // if there is one before we set the desired buffer.
            if (saea.BufferList != null) saea.BufferList = null;
            saea.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
            saea.SocketFlags = socketFlags;
            saea._wrapExceptionsInIOExceptions = wrapExceptionsInIOExceptions;
        }

        private static void ConfigureBufferList(
            Int32TaskSocketAsyncEventArgs saea, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            // Configure the buffer list.  We don't clear the buffers when returning the SAEA to the pool,
            // so as to minimize overhead if the same buffers are used for subsequent operations (which is likely).
            // But SAEA doesn't support having both a buffer and a buffer list configured, so clear out a buffer
            // if there is one before we set the desired buffer list.
            if (saea.Buffer != null) saea.SetBuffer(null, 0, 0);
            saea.BufferList = buffers;
            saea.SocketFlags = socketFlags;
        }

        /// <summary>Gets a task to represent the operation.</summary>
        /// <param name="pending">true if the operation completes asynchronously; false if it completed synchronously.</param>
        /// <param name="saea">The event args instance used with the operation.</param>
        /// <param name="fromNetworkStream">
        /// true if the request is coming from NetworkStream, which has special semantics for
        /// exceptions and cached tasks; otherwise, false.
        /// </param>
        /// <param name="isReceive">true if this is a receive; false if this is a send.</param>
        private Task<int> GetTaskForSendReceive(
            bool pending, Int32TaskSocketAsyncEventArgs saea,
            bool fromNetworkStream, bool isReceive)
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
                        // Get any cached, successfully-completed cached task that may exist on this SAEA.
                        Task<int> lastTask = saea._successfullyCompletedTask;
                        Debug.Assert(lastTask == null || lastTask.Status == TaskStatus.RanToCompletion);

                        // If there is a task and if it has the desired result, simply reuse it.
                        // Otherwise, create a new one for this result value, and in addition to returning it,
                        // also store it into the SAEA for potential future reuse.
                        t = lastTask != null && lastTask.Result == bytesTransferred ?
                            lastTask :
                            (saea._successfullyCompletedTask = Task.FromResult(bytesTransferred));
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
        private static void CompleteSendReceive(Int32TaskSocketAsyncEventArgs saea, bool isReceive)
        {
            // Pull the relevant state off of the SAEA
            Socket s = (Socket)saea.UserToken;
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
            Exception e = new SocketException((int)error);
            return wrapExceptionsInIOExceptions ?
                new IOException(SR.Format(SR.net_io_readwritefailure, e.Message), e) :
                e;
        }

        /// <summary>Rents a <see cref="Int32TaskSocketAsyncEventArgs"/> for immediate use.</summary>
        /// <param name="isReceive">true if this instance will be used for a receive; false if for sends.</param>
        private Int32TaskSocketAsyncEventArgs RentSocketAsyncEventArgs(bool isReceive)
        {
            // Get any cached SocketAsyncEventArg we may have.
            Int32TaskSocketAsyncEventArgs saea = isReceive ?
                Interlocked.Exchange(ref _cachedReceiveEventArgs, s_rentedSentinel) :
                Interlocked.Exchange(ref _cachedSendEventArgs, s_rentedSentinel);

            if (saea == s_rentedSentinel)
            {
                // An instance was once created (or is currently being created elsewhere), but some other
                // concurrent operation is using it. Since we can store at most one, and since an individual
                // APM operation is less expensive than creating a new SAEA and using it only once, we simply
                // return null, for a caller to fall back to using an APM implementation.
                return null;
            }

            if (saea == null)
            {
                // No instance has been created yet, so create one.
                saea = new Int32TaskSocketAsyncEventArgs();
                var handler = isReceive ? // branch to avoid capturing isReceive on every call
                    new EventHandler<SocketAsyncEventArgs>((_, e) => CompleteSendReceive((Int32TaskSocketAsyncEventArgs)e, isReceive: true)) :
                    new EventHandler<SocketAsyncEventArgs>((_, e) => CompleteSendReceive((Int32TaskSocketAsyncEventArgs)e, isReceive: false));
                saea.Completed += handler;
            }

            // We got an instance. Configure and return it.
            saea.UserToken = this;
            return saea;
        }

        /// <summary>Returns a <see cref="Int32TaskSocketAsyncEventArgs"/> instance for reuse.</summary>
        /// <param name="saea">The instance to return.</param>
        /// <param name="isReceive">true if this instance is used for receives; false if used for sends.</param>
        private void ReturnSocketAsyncEventArgs(Int32TaskSocketAsyncEventArgs saea, bool isReceive)
        {
            Debug.Assert(saea != s_rentedSentinel);

            // Reset state on the SAEA before returning it.  But do not reset buffer state.  That'll be done
            // if necessary by the consumer, but we want to keep the buffers due to likely subsequent reuse
            // and the costs associated with changing them.
            saea.UserToken = null;
            saea._accessed = false;
            saea._builder = default(AsyncTaskMethodBuilder<int>);
            saea._wrapExceptionsInIOExceptions = false;

            // Write this instance back as a cached instance.  It should only ever be overwriting the sentinel,
            // never null or another instance.
            if (isReceive)
            {
                Debug.Assert(_cachedReceiveEventArgs == s_rentedSentinel);
                Volatile.Write(ref _cachedReceiveEventArgs, saea);
            }
            else
            {
                Debug.Assert(_cachedSendEventArgs == s_rentedSentinel);
                Volatile.Write(ref _cachedSendEventArgs, saea);
            }
        }

        /// <summary>Dispose of any cached <see cref="Int32TaskSocketAsyncEventArgs"/> instances.</summary>
        private void DisposeCachedTaskSocketAsyncEventArgs()
        {
            Int32TaskSocketAsyncEventArgs e = Interlocked.Exchange(ref _cachedReceiveEventArgs, s_rentedSentinel);
            if (e != s_rentedSentinel) e?.Dispose();

            e = Interlocked.Exchange(ref _cachedSendEventArgs, s_rentedSentinel);
            if (e != s_rentedSentinel) e?.Dispose();
        }

        /// <summary>A TaskCompletionSource that carries an extra field of strongly-typed state.</summary>
        private class StateTaskCompletionSource<TField1, TResult> : TaskCompletionSource<TResult>
        {
            internal TField1 _field1;
            public StateTaskCompletionSource(object baseState) : base(baseState) { }
        }

        /// <summary>A TaskCompletionSource that carries several extra fields of strongly-typed state.</summary>
        private class StateTaskCompletionSource<TField1, TField2, TResult> : StateTaskCompletionSource<TField1, TResult>
        {
            internal TField2 _field2;
            public StateTaskCompletionSource(object baseState) : base(baseState) { }
        }

        /// <summary>A SocketAsyncEventArgs with an associated async method builder.</summary>
        internal sealed class Int32TaskSocketAsyncEventArgs : SocketAsyncEventArgs
        {
            /// <summary>A cached, successfully completed task.</summary>
            internal Task<int> _successfullyCompletedTask;
            /// <summary>
            /// The builder used to create the Task representing the result of the async operation.
            /// This is a mutable struct.
            /// </summary>
            internal AsyncTaskMethodBuilder<int> _builder;
            /// <summary>Whether exceptions that emerge should be wrapped in IOExceptions.</summary>
            internal bool _wrapExceptionsInIOExceptions;
            /// <summary>
            /// Whether the instance was already accessed as part of the operation.  We expect
            /// at most two accesses: one from the synchronous caller to initiate the operation,
            /// and one from the callback if the operation completes asynchronously.  If it completes
            /// synchronously, then it's the initiator's responsbility to return the instance to
            /// the pool.  If it completes asynchronously, then it's the responsibility of whoever
            /// accesses this second, so we track whether it's already been accessed.
            /// </summary>
            internal bool _accessed = false;

            /// <summary>Gets the builder's task with appropriate synchronization.</summary>
            internal AsyncTaskMethodBuilder<int> GetCompletionResponsibility(out bool responsibleForReturningToPool)
            {
                lock (this)
                {
                    responsibleForReturningToPool = _accessed;
                    _accessed = true;
                    var ignored = _builder.Task; // force initialization under the lock (builder itself lazily initializes w/o synchronization)
                    return _builder;
                }
            }
        }
    }
}
