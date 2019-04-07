// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

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
        /// <summary>Handler for completed AcceptAsync operations.</summary>
        private static readonly EventHandler<SocketAsyncEventArgs> AcceptCompletedHandler = (s, e) => CompleteAccept((Socket)s, (TaskSocketAsyncEventArgs<Socket>)e);
        /// <summary>Handler for completed ReceiveAsync operations.</summary>
        private static readonly EventHandler<SocketAsyncEventArgs> ReceiveCompletedHandler = (s, e) => CompleteSendReceive((Socket)s, (Int32TaskSocketAsyncEventArgs)e, isReceive: true);
        /// <summary>Handler for completed SendAsync operations.</summary>
        private static readonly EventHandler<SocketAsyncEventArgs> SendCompletedHandler = (s, e) => CompleteSendReceive((Socket)s, (Int32TaskSocketAsyncEventArgs)e, isReceive: false);
        /// <summary>
        /// Sentinel that can be stored into one of the Socket cached fields to indicate that an instance
        /// was previously created but is currently being used by another concurrent operation.
        /// </summary>
        private static readonly TaskSocketAsyncEventArgs<Socket> s_rentedSocketSentinel = new TaskSocketAsyncEventArgs<Socket>();
        /// <summary>
        /// Sentinel that can be stored into one of the Int32 fields to indicate that an instance
        /// was previously created but is currently being used by another concurrent operation.
        /// </summary>
        private static readonly Int32TaskSocketAsyncEventArgs s_rentedInt32Sentinel = new Int32TaskSocketAsyncEventArgs();
        /// <summary>Cached task with a 0 value.</summary>
        private static readonly Task<int> s_zeroTask = Task.FromResult(0);

        /// <summary>Cached event args used with Task-based async operations.</summary>
        private CachedEventArgs _cachedTaskEventArgs;

        internal Task<Socket> AcceptAsync(Socket acceptSocket)
        {
            // Get any cached SocketAsyncEventArg we may have.
            TaskSocketAsyncEventArgs<Socket> saea = Interlocked.Exchange(ref LazyInitializer.EnsureInitialized(ref _cachedTaskEventArgs).TaskAccept, s_rentedSocketSentinel);
            if (saea == s_rentedSocketSentinel)
            {
                // An instance was once created (or is currently being created elsewhere), but some other
                // concurrent operation is using it. Since we can store at most one, and since an individual
                // APM operation is less expensive than creating a new SAEA and using it only once, we simply
                // fall back to using an APM implementation.
                return AcceptAsyncApm(acceptSocket);
            }
            else if (saea == null)
            {
                // No instance has been created yet, so create one.
                saea = new TaskSocketAsyncEventArgs<Socket>();
                saea.Completed += AcceptCompletedHandler;
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
                    Task.FromResult(saea.AcceptSocket) :
                    Task.FromException<Socket>(GetException(saea.SocketError));

                // There won't be a callback, and we're done with the SAEA, so return it to the pool.
                ReturnSocketAsyncEventArgs(saea);
            }

            return t;
        }

        /// <summary>Implements Task-returning AcceptAsync on top of Begin/EndAsync.</summary>
        private Task<Socket> AcceptAsyncApm(Socket acceptSocket)
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
            ValidateBuffer(buffer);
            return ReceiveAsync((Memory<byte>)buffer, socketFlags, fromNetworkStream, default).AsTask();
        }

        // TODO https://github.com/dotnet/corefx/issues/24430:
        // Fully plumb cancellation down into socket operations.

        internal ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
            }

            AwaitableSocketAsyncEventArgs saea = LazyInitializer.EnsureInitialized(ref LazyInitializer.EnsureInitialized(ref _cachedTaskEventArgs).ValueTaskReceive);
            if (saea.Reserve())
            {
                Debug.Assert(saea.BufferList == null);
                saea.SetBuffer(buffer);
                saea.SocketFlags = socketFlags;
                saea.WrapExceptionsInIOExceptions = fromNetworkStream;
                return saea.ReceiveAsync(this, cancellationToken);
            }
            else
            {
                // We couldn't get a cached instance, due to a concurrent receive operation on the socket.
                // Fall back to wrapping APM.
                return new ValueTask<int>(ReceiveAsyncApm(buffer, socketFlags));
            }
        }

        /// <summary>Implements Task-returning ReceiveAsync on top of Begin/EndReceive.</summary>
        private Task<int> ReceiveAsyncApm(Memory<byte> buffer, SocketFlags socketFlags)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> bufferArray))
            {
                // We were able to extract the underlying byte[] from the Memory<byte>. Use it.
                var tcs = new TaskCompletionSource<int>(this);
                BeginReceive(bufferArray.Array, bufferArray.Offset, bufferArray.Count, socketFlags, iar =>
                {
                    var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                    try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndReceive(iar)); }
                    catch (Exception e) { innerTcs.TrySetException(e); }
                }, tcs);
                return tcs.Task;
            }
            else
            {
                // We weren't able to extract an underlying byte[] from the Memory<byte>.
                // Instead read into an ArrayPool array, then copy from that into the memory.
                byte[] poolArray = ArrayPool<byte>.Shared.Rent(buffer.Length);
                var tcs = new TaskCompletionSource<int>(this);
                BeginReceive(poolArray, 0, buffer.Length, socketFlags, iar =>
                {
                    var state = (Tuple<TaskCompletionSource<int>, Memory<byte>, byte[]>)iar.AsyncState;
                    try
                    {
                        int bytesCopied = ((Socket)state.Item1.Task.AsyncState).EndReceive(iar);
                        new ReadOnlyMemory<byte>(state.Item3, 0, bytesCopied).Span.CopyTo(state.Item2.Span);
                        state.Item1.TrySetResult(bytesCopied);
                    }
                    catch (Exception e)
                    {
                        state.Item1.TrySetException(e);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(state.Item3);
                    }
                }, Tuple.Create(tcs, buffer, poolArray));
                return tcs.Task;
            }
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

        internal Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            ValidateBuffer(buffer);
            return SendAsync((ReadOnlyMemory<byte>)buffer, socketFlags, default).AsTask();
        }

        internal ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
            }

            AwaitableSocketAsyncEventArgs saea = LazyInitializer.EnsureInitialized(ref LazyInitializer.EnsureInitialized(ref _cachedTaskEventArgs).ValueTaskSend);
            if (saea.Reserve())
            {
                Debug.Assert(saea.BufferList == null);
                saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
                saea.SocketFlags = socketFlags;
                saea.WrapExceptionsInIOExceptions = false;
                return saea.SendAsync(this, cancellationToken);
            }
            else
            {
                // We couldn't get a cached instance, due to a concurrent send operation on the socket.
                // Fall back to wrapping APM.
                return new ValueTask<int>(SendAsyncApm(buffer, socketFlags));
            }
        }

        internal ValueTask SendAsyncForNetworkStream(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask(Task.FromCanceled(cancellationToken));
            }

            AwaitableSocketAsyncEventArgs saea = LazyInitializer.EnsureInitialized(ref LazyInitializer.EnsureInitialized(ref _cachedTaskEventArgs).ValueTaskSend);
            if (saea.Reserve())
            {
                Debug.Assert(saea.BufferList == null);
                saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
                saea.SocketFlags = socketFlags;
                saea.WrapExceptionsInIOExceptions = true;
                return saea.SendAsyncForNetworkStream(this);
            }
            else
            {
                // We couldn't get a cached instance, due to a concurrent send operation on the socket.
                // Fall back to wrapping APM.
                return new ValueTask(SendAsyncApm(buffer, socketFlags));
            }
        }

        /// <summary>Implements Task-returning SendAsync on top of Begin/EndSend.</summary>
        private Task<int> SendAsyncApm(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> bufferArray))
            {
                var tcs = new TaskCompletionSource<int>(this);
                BeginSend(bufferArray.Array, bufferArray.Offset, bufferArray.Count, socketFlags, iar =>
                {
                    var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                    try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSend(iar)); }
                    catch (Exception e) { innerTcs.TrySetException(e); }
                }, tcs);
                return tcs.Task;
            }
            else
            {
                // We weren't able to extract an underlying byte[] from the Memory<byte>.
                // Instead read into an ArrayPool array, then copy from that into the memory.
                byte[] poolArray = ArrayPool<byte>.Shared.Rent(buffer.Length);
                buffer.Span.CopyTo(poolArray);
                var tcs = new TaskCompletionSource<int>(this);
                BeginSend(poolArray, 0, buffer.Length, socketFlags, iar =>
                {
                    var state = (Tuple<TaskCompletionSource<int>, byte[]>)iar.AsyncState;
                    try
                    {
                        state.Item1.TrySetResult(((Socket)state.Item1.Task.AsyncState).EndSend(iar));
                    }
                    catch (Exception e)
                    {
                        state.Item1.TrySetException(e);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(state.Item2);
                    }
                }, Tuple.Create(tcs, poolArray));
                return tcs.Task;
            }
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

        private static void ConfigureBufferList(
            Int32TaskSocketAsyncEventArgs saea, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            // Configure the buffer list.  We don't clear the buffers when returning the SAEA to the pool,
            // so as to minimize overhead if the same buffers are used for subsequent operations (which is likely).
            // But SAEA doesn't support having both a buffer and a buffer list configured, so clear out a buffer
            // if there is one before we set the desired buffer list.
            if (!saea.MemoryBuffer.Equals(default)) saea.SetBuffer(default);
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
            Socket acceptSocket = saea.AcceptSocket;

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
                builder.SetResult(acceptSocket);
            }
            else
            {
                builder.SetException(GetException(error));
            }
        }

        /// <summary>Completes the SocketAsyncEventArg's Task with the result of the send or receive, and returns it to the specified pool.</summary>
        private static void CompleteSendReceive(Socket s, Int32TaskSocketAsyncEventArgs saea, bool isReceive)
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
            CachedEventArgs cea = LazyInitializer.EnsureInitialized(ref _cachedTaskEventArgs);
            Int32TaskSocketAsyncEventArgs saea = isReceive ?
                Interlocked.Exchange(ref cea.TaskReceive, s_rentedInt32Sentinel) :
                Interlocked.Exchange(ref cea.TaskSend, s_rentedInt32Sentinel);

            if (saea == s_rentedInt32Sentinel)
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
                saea.Completed += isReceive ? ReceiveCompletedHandler : SendCompletedHandler;
            }

            return saea;
        }

        /// <summary>Returns a <see cref="Int32TaskSocketAsyncEventArgs"/> instance for reuse.</summary>
        /// <param name="saea">The instance to return.</param>
        /// <param name="isReceive">true if this instance is used for receives; false if used for sends.</param>
        private void ReturnSocketAsyncEventArgs(Int32TaskSocketAsyncEventArgs saea, bool isReceive)
        {
            Debug.Assert(_cachedTaskEventArgs != null, "Should have been initialized when renting");
            Debug.Assert(saea != s_rentedInt32Sentinel);

            // Reset state on the SAEA before returning it.  But do not reset buffer state.  That'll be done
            // if necessary by the consumer, but we want to keep the buffers due to likely subsequent reuse
            // and the costs associated with changing them.
            saea._accessed = false;
            saea._builder = default(AsyncTaskMethodBuilder<int>);
            saea._wrapExceptionsInIOExceptions = false;

            // Write this instance back as a cached instance.  It should only ever be overwriting the sentinel,
            // never null or another instance.
            if (isReceive)
            {
                Debug.Assert(_cachedTaskEventArgs.TaskReceive == s_rentedInt32Sentinel);
                Volatile.Write(ref _cachedTaskEventArgs.TaskReceive, saea);
            }
            else
            {
                Debug.Assert(_cachedTaskEventArgs.TaskSend == s_rentedInt32Sentinel);
                Volatile.Write(ref _cachedTaskEventArgs.TaskSend, saea);
            }
        }

        /// <summary>Returns a <see cref="Int32TaskSocketAsyncEventArgs"/> instance for reuse.</summary>
        /// <param name="saea">The instance to return.</param>
        private void ReturnSocketAsyncEventArgs(TaskSocketAsyncEventArgs<Socket> saea)
        {
            Debug.Assert(_cachedTaskEventArgs != null, "Should have been initialized when renting");
            Debug.Assert(saea != s_rentedSocketSentinel);

            // Reset state on the SAEA before returning it.  But do not reset buffer state.  That'll be done
            // if necessary by the consumer, but we want to keep the buffers due to likely subsequent reuse
            // and the costs associated with changing them.
            saea.AcceptSocket = null;
            saea._accessed = false;
            saea._builder = default(AsyncTaskMethodBuilder<Socket>);

            // Write this instance back as a cached instance.  It should only ever be overwriting the sentinel,
            // never null or another instance.
            Debug.Assert(_cachedTaskEventArgs.TaskAccept == s_rentedSocketSentinel);
            Volatile.Write(ref _cachedTaskEventArgs.TaskAccept, saea);
        }

        /// <summary>Dispose of any cached <see cref="Int32TaskSocketAsyncEventArgs"/> instances.</summary>
        private void DisposeCachedTaskSocketAsyncEventArgs()
        {
            CachedEventArgs cea = _cachedTaskEventArgs;
            if (cea != null)
            {
                Interlocked.Exchange(ref cea.TaskAccept, s_rentedSocketSentinel)?.Dispose();
                Interlocked.Exchange(ref cea.TaskReceive, s_rentedInt32Sentinel)?.Dispose();
                Interlocked.Exchange(ref cea.TaskSend, s_rentedInt32Sentinel)?.Dispose();
                Interlocked.Exchange(ref cea.ValueTaskReceive, AwaitableSocketAsyncEventArgs.Reserved)?.Dispose();
                Interlocked.Exchange(ref cea.ValueTaskSend, AwaitableSocketAsyncEventArgs.Reserved)?.Dispose();
            }
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

        /// <summary>Cached event args used with Task-based async operations.</summary>
        private sealed class CachedEventArgs
        {
            /// <summary>Cached instance for accept operations.</summary>
            public TaskSocketAsyncEventArgs<Socket> TaskAccept;
            /// <summary>Cached instance for receive operations that return <see cref="Task{Int32}"/>.</summary>
            public Int32TaskSocketAsyncEventArgs TaskReceive;
            /// <summary>Cached instance for send operations that return <see cref="Task{Int32}"/>.</summary>
            public Int32TaskSocketAsyncEventArgs TaskSend;
            /// <summary>Cached instance for receive operations that return <see cref="ValueTask{Int32}"/>.</summary>
            public AwaitableSocketAsyncEventArgs ValueTaskReceive;
            /// <summary>Cached instance for send operations that return <see cref="ValueTask{Int32}"/>.</summary>
            public AwaitableSocketAsyncEventArgs ValueTaskSend;
        }

        /// <summary>A SocketAsyncEventArgs with an associated async method builder.</summary>
        private class TaskSocketAsyncEventArgs<TResult> : SocketAsyncEventArgs
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
            internal bool _accessed = false;

            internal TaskSocketAsyncEventArgs() :
                base(flowExecutionContext: false) // avoid flowing context at lower layers as we only expose Task, which handles it
            {
            }

            /// <summary>Gets the builder's task with appropriate synchronization.</summary>
            internal AsyncTaskMethodBuilder<TResult> GetCompletionResponsibility(out bool responsibleForReturningToPool)
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

        /// <summary>A SocketAsyncEventArgs with an associated async method builder.</summary>
        private sealed class Int32TaskSocketAsyncEventArgs : TaskSocketAsyncEventArgs<int>
        {
            /// <summary>Whether exceptions that emerge should be wrapped in IOExceptions.</summary>
            internal bool _wrapExceptionsInIOExceptions;
        }

        /// <summary>A SocketAsyncEventArgs that can be awaited to get the result of an operation.</summary>
        internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource, IValueTaskSource<int>
        {
            internal static readonly AwaitableSocketAsyncEventArgs Reserved = new AwaitableSocketAsyncEventArgs() { _continuation = null };
            /// <summary>Sentinel object used to indicate that the operation has completed prior to OnCompleted being called.</summary>
            private static readonly Action<object> s_completedSentinel = new Action<object>(state => throw new Exception(nameof(s_completedSentinel)));
            /// <summary>Sentinel object used to indicate that the instance is available for use.</summary>
            private static readonly Action<object> s_availableSentinel = new Action<object>(state => throw new Exception(nameof(s_availableSentinel)));
            /// <summary>
            /// <see cref="s_availableSentinel"/> if the object is available for use, after GetResult has been called on a previous use.
            /// null if the operation has not completed.
            /// <see cref="s_completedSentinel"/> if it has completed.
            /// Another delegate if OnCompleted was called before the operation could complete, in which case it's the delegate to invoke
            /// when the operation does complete.
            /// </summary>
            private Action<object> _continuation = s_availableSentinel;
            private ExecutionContext _executionContext;
            private object _scheduler;
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
            public AwaitableSocketAsyncEventArgs() :
                base(flowExecutionContext: false) // avoid flowing context at lower layers as we only expose ValueTask, which handles it
            {
            }

            public bool WrapExceptionsInIOExceptions { get; set; }

            public bool Reserve() =>
                ReferenceEquals(Interlocked.CompareExchange(ref _continuation, null, s_availableSentinel), s_availableSentinel);

            private void Release()
            {
                _cancellationToken = default;
                _token++;
                Volatile.Write(ref _continuation, s_availableSentinel);
            }

            protected override void OnCompleted(SocketAsyncEventArgs _)
            {
                // When the operation completes, see if OnCompleted was already called to hook up a continuation.
                // If it was, invoke the continuation.
                Action<object> c = _continuation;
                if (c != null || (c = Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null)) != null)
                {
                    Debug.Assert(c != s_availableSentinel, "The delegate should not have been the available sentinel.");
                    Debug.Assert(c != s_completedSentinel, "The delegate should not have been the completed sentinel.");

                    object continuationState = UserToken;
                    UserToken = null;
                    _continuation = s_completedSentinel; // in case someone's polling IsCompleted

                    ExecutionContext ec = _executionContext;
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
                            var t = (Tuple<AwaitableSocketAsyncEventArgs, Action<object>, object>)runState;
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
                    new ValueTask<int>(Task.FromException<int>(CreateException(error)));
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
                    new ValueTask<int>(Task.FromException<int>(CreateException(error)));
            }

            public ValueTask SendAsyncForNetworkStream(Socket socket)
            {
                Debug.Assert(Volatile.Read(ref _continuation) == null, $"Expected null continuation to indicate reserved for use");

                if (socket.SendAsync(this))
                {
                    return new ValueTask(this, _token);
                }

                SocketError error = SocketError;

                Release();

                return error == SocketError.Success ?
                    default :
                    new ValueTask(Task.FromException(CreateException(error)));
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
                    base.SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                    ValueTaskSourceStatus.Faulted;
            }

            /// <summary>Queues the provided continuation to be executed once the operation has completed.</summary>
            public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
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
                    SynchronizationContext sc = SynchronizationContext.Current;
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
                Action<object> prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
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

            private void InvokeContinuation(Action<object> continuation, object state, bool forceAsync, bool requiresExecutionContextFlow)
            {
                object scheduler = _scheduler;
                _scheduler = null;

                if (scheduler != null)
                {
                    if (scheduler is SynchronizationContext sc)
                    {
                        sc.Post(s =>
                        {
                            var t = (Tuple<Action<object>, object>)s;
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

                throw CreateException(error);
            }

            private Exception CreateException(SocketError error)
            {
                var se = new SocketException((int)error);
                return WrapExceptionsInIOExceptions ? (Exception)
                    new IOException(SR.Format(SR.net_io_readfailure, se.Message), se) :
                    se;
            }
        }
    }
}
