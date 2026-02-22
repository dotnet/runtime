// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed partial class SocketAsyncContext
    {
        private const int MultishotAcceptQueueMaxSize = 4096;
        private const int PersistentMultishotRecvDataQueueMaxSize = 64;
        private const int SolSocket = 1;
        private const int SoIncomingCpu = 49;
        private const int SoReusePort = 15;
        private const int IoUringUserDataTagShift = 56;
        private const byte IoUringReservedCompletionTag = 2;
        private const long MultishotAcceptStateDisarmed = 0;
        private const long MultishotAcceptStateArming = 1;
        private Queue<PreAcceptedConnection>? _multishotAcceptQueue;
        private int _migrationState; // 0=unchecked, 1=checking, 2=done
        private long _multishotAcceptState; // 0=disarmed, 1=arming, otherwise encoded reserved-completion user_data
        private ulong _persistentMultishotRecvUserData; // user_data of armed multishot recv SQE
        private int _persistentMultishotRecvArmed; // 0=not armed, 1=armed
        private Queue<BufferedPersistentMultishotRecvData>? _persistentMultishotRecvDataQueue;
        private BufferedPersistentMultishotRecvData _persistentMultishotRecvDataHead;
        private bool _hasPersistentMultishotRecvDataHead;
        private int _persistentMultishotRecvDataHeadOffset;
        private Lock? _multishotAcceptQueueGate;
        private Lock? _persistentMultishotRecvDataGate;
        private Lock? _reusePortShadowListenersGate;
        private ReusePortShadowListenerState[]? _reusePortShadowListeners;

        /// <summary>Tracks a SO_REUSEPORT shadow listener socket armed on a non-primary engine.</summary>
        private struct ReusePortShadowListenerState
        {
            internal SafeSocketHandle Handle;
            internal int EngineIndex;
            internal ulong ArmedUserData;
        }

        private readonly struct BufferedPersistentMultishotRecvData
        {
            internal readonly byte[] Data;
            internal readonly int Length;
            internal readonly bool UsesPooledBuffer;

            internal BufferedPersistentMultishotRecvData(byte[] data, int length, bool usesPooledBuffer)
            {
                Data = data;
                Length = length;
                UsesPooledBuffer = usesPooledBuffer;
            }
        }

        /// <summary>Holds a pre-accepted connection's fd and socket address from a multishot accept CQE.</summary>
        private readonly struct PreAcceptedConnection
        {
            internal readonly IntPtr FileDescriptor;
            internal readonly byte[] SocketAddressData;
            internal readonly int SocketAddressLength;
            internal readonly bool UsesPooledBuffer;

            internal PreAcceptedConnection(IntPtr fileDescriptor, byte[] socketAddressData, int socketAddressLength, bool usesPooledBuffer)
            {
                FileDescriptor = fileDescriptor;
                SocketAddressData = socketAddressData;
                SocketAddressLength = socketAddressLength;
                UsesPooledBuffer = usesPooledBuffer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Lock EnsureMultishotAcceptQueueGate() => EnsureLockInitialized(ref _multishotAcceptQueueGate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Lock EnsurePersistentMultishotRecvDataGate() => EnsureLockInitialized(ref _persistentMultishotRecvDataGate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Lock EnsureReusePortShadowListenersGate() => EnsureLockInitialized(ref _reusePortShadowListenersGate);

        private bool IsPrimarySocketReusePortEnabled()
        {
            Span<byte> value = stackalloc byte[sizeof(int)];
            int valueLength = sizeof(int);
            SocketError error = SocketPal.GetRawSockOpt(_socket, SolSocket, SoReusePort, value, ref valueLength);
            if (error != SocketError.Success || valueLength < sizeof(int))
            {
                return false;
            }

            return BitConverter.ToInt32(value) != 0;
        }

        private void AddReusePortShadowListener(ref ReusePortShadowListenerState state)
        {
            Lock gate = EnsureReusePortShadowListenersGate();
            lock (gate)
            {
                ReusePortShadowListenerState[]? existing = _reusePortShadowListeners;
                if (existing is null)
                {
                    _reusePortShadowListeners = [state];
                    return;
                }

                var updated = new ReusePortShadowListenerState[existing.Length + 1];
                Array.Copy(existing, updated, existing.Length);
                updated[^1] = state;
                _reusePortShadowListeners = updated;
            }
        }

        private void RemoveReusePortShadowListenerByEngineIndex(int engineIndex)
        {
            Lock gate = EnsureReusePortShadowListenersGate();
            lock (gate)
            {
                ReusePortShadowListenerState[]? existing = _reusePortShadowListeners;
                if (existing is null || existing.Length == 0)
                {
                    return;
                }

                int removeIndex = -1;
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i].EngineIndex == engineIndex)
                    {
                        removeIndex = i;
                        break;
                    }
                }

                if (removeIndex < 0)
                {
                    return;
                }

                if (existing.Length == 1)
                {
                    _reusePortShadowListeners = null;
                    return;
                }

                var updated = new ReusePortShadowListenerState[existing.Length - 1];
                if (removeIndex > 0)
                {
                    Array.Copy(existing, 0, updated, 0, removeIndex);
                }

                if (removeIndex < existing.Length - 1)
                {
                    Array.Copy(existing, removeIndex + 1, updated, removeIndex, existing.Length - removeIndex - 1);
                }

                _reusePortShadowListeners = updated;
            }
        }

        private static bool TryGetIncomingCpu(SafeSocketHandle socket, out int cpu)
        {
            cpu = -1;
            Span<byte> value = stackalloc byte[sizeof(int)];
            int valueLength = sizeof(int);
            SocketError error = SocketPal.GetRawSockOpt(socket, SolSocket, SoIncomingCpu, value, ref valueLength);
            if (error != SocketError.Success || valueLength < sizeof(int))
            {
                return false;
            }

            cpu = BitConverter.ToInt32(value);
            return cpu >= 0;
        }

        private void TryMigrateIoUringEngineOnFirstReceiveCompletion()
        {
            if (Interlocked.CompareExchange(ref _migrationState, 1, 0) != 0)
            {
                return;
            }

            try
            {
                SocketAsyncEngine? engine = Volatile.Read(ref _asyncEngine);
                if (engine is null || !engine.IsIoUringCompletionModeEnabled || IsPersistentMultishotRecvArmed())
                {
                    return;
                }

                if (!TryGetIncomingCpu(_socket, out int incomingCpu))
                {
                    return;
                }

                int targetEngineIndex = SocketAsyncEngine.GetEngineIndexForCpu(incomingCpu);
                if (targetEngineIndex < 0 || targetEngineIndex == engine.EngineIndex)
                {
                    return;
                }

                _ = TryMigrateToEngine(targetEngineIndex);
            }
            finally
            {
                Volatile.Write(ref _migrationState, 2);
            }
        }

        private int PersistentMultishotRecvBufferedCount =>
            (_persistentMultishotRecvDataQueue?.Count ?? 0) + (_hasPersistentMultishotRecvDataHead ? 1 : 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Lock EnsureLockInitialized(ref Lock? gate)
        {
            Lock? existing = Volatile.Read(ref gate);
            if (existing is not null)
            {
                return existing;
            }

            Lock created = new Lock();
            Lock? prior = Interlocked.CompareExchange(ref gate, created, null);
            return prior ?? created;
        }

        /// <summary>Returns whether this context's engine is using io_uring completion mode.</summary>
        private bool IsIoUringCompletionModeEnabled()
        {
            SocketAsyncEngine? engine = Volatile.Read(ref _asyncEngine);
            return engine is not null && engine.IsIoUringCompletionModeEnabled;
        }

        /// <summary>Returns the total count of non-pinnable buffer prepare fallbacks across active engines.</summary>
        internal static long GetIoUringNonPinnablePrepareFallbackCount() =>
            SocketAsyncEngine.GetIoUringNonPinnablePrepareFallbackCount();

        /// <summary>Test-only setter for the non-pinnable fallback counter.</summary>
        internal static void SetIoUringNonPinnablePrepareFallbackCountForTest(long value) =>
            SocketAsyncEngine.SetIoUringNonPinnablePrepareFallbackCountForTest(value);

        private static SocketAsyncContext? GetContextForTest(Socket socket)
        {
            try { return socket.SafeHandle.AsyncContext; }
            catch (ObjectDisposedException) { return null; }
        }

        internal static bool TryGetSocketAsyncContextForTest(Socket socket, out SocketAsyncContext? context)
        {
            context = GetContextForTest(socket);
            return context is not null;
        }

        internal static int GetReusePortShadowListenerCountForTest(Socket socket) =>
            GetContextForTest(socket)?._reusePortShadowListeners?.Length ?? 0;

        internal static bool IsMultishotAcceptArmedForTest(Socket socket) =>
            GetContextForTest(socket)?.IsMultishotAcceptArmed ?? false;

        internal static int GetMultishotAcceptQueueCountForTest(Socket socket)
        {
            SocketAsyncContext? context = GetContextForTest(socket);
            if (context is null) return 0;
            Lock gate = context.EnsureMultishotAcceptQueueGate();
            lock (gate) { return context._multishotAcceptQueue?.Count ?? 0; }
        }

        internal static bool TryGetIncomingCpuForTest(Socket socket, out int cpu)
        {
            cpu = -1;
            SocketAsyncContext? context = GetContextForTest(socket);
            return context is not null && TryGetIncomingCpu(context._socket, out cpu);
        }

        internal static bool IsPersistentMultishotRecvArmedForTest(Socket socket) =>
            GetContextForTest(socket)?.IsPersistentMultishotRecvArmed() ?? false;

        internal static ulong GetPersistentMultishotRecvUserDataForTest(Socket socket)
        {
            SocketAsyncContext? context = GetContextForTest(socket);
            return context is not null && context.IsPersistentMultishotRecvArmed()
                ? context.PersistentMultishotRecvUserData : 0;
        }

        internal static int GetPersistentMultishotRecvBufferedCountForTest(Socket socket)
        {
            SocketAsyncContext? context = GetContextForTest(socket);
            if (context is null) return 0;
            Lock gate = context.EnsurePersistentMultishotRecvDataGate();
            lock (gate) { return context.PersistentMultishotRecvBufferedCount; }
        }

        internal int GetPersistentMultishotRecvBufferedCountForDiagnostics()
        {
            Lock gate = EnsurePersistentMultishotRecvDataGate();
            lock (gate)
            {
                return PersistentMultishotRecvBufferedCount;
            }
        }

        /// <summary>Test-only wrapper accepting byte[] to avoid Span reflection limitations.</summary>
        internal bool TryBufferEarlyPersistentMultishotRecvDataForTest(byte[] payload) =>
            TryBufferEarlyPersistentMultishotRecvData(payload);

        /// <summary>Returns whether a multishot accept SQE is currently armed for this context.</summary>
        internal bool IsMultishotAcceptArmed => Volatile.Read(ref _multishotAcceptState) != MultishotAcceptStateDisarmed;

        /// <summary>Returns the user_data payload for the armed multishot accept SQE, if any.</summary>
        internal ulong MultishotAcceptUserData => DecodeMultishotAcceptUserData(Volatile.Read(ref _multishotAcceptState));

        /// <summary>Clears multishot accept armed-state for this context.</summary>
        internal void DisarmMultishotAccept()
        {
            Volatile.Write(ref _multishotAcceptState, MultishotAcceptStateDisarmed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong DecodeMultishotAcceptUserData(long packedState)
        {
            ulong rawState = (ulong)packedState;
            return (byte)(rawState >> IoUringUserDataTagShift) == IoUringReservedCompletionTag
                ? rawState
                : 0;
        }

        /// <summary>Returns whether a persistent multishot recv SQE is currently armed for this context.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsPersistentMultishotRecvArmed() =>
            Volatile.Read(ref _persistentMultishotRecvArmed) != 0;

        /// <summary>Records that a persistent multishot recv SQE has been armed for this context.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetPersistentMultishotRecvArmed(ulong userData)
        {
            Volatile.Write(ref _persistentMultishotRecvUserData, userData);
            Volatile.Write(ref _persistentMultishotRecvArmed, 1);
        }

        /// <summary>Clears this context's armed persistent multishot recv state.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ClearPersistentMultishotRecvArmed()
        {
            Volatile.Write(ref _persistentMultishotRecvUserData, 0);
            Volatile.Write(ref _persistentMultishotRecvArmed, 0);
        }

        /// <summary>Gets the user_data of the armed persistent multishot recv SQE, or 0 if none is armed.</summary>
        internal ulong PersistentMultishotRecvUserData =>
            Volatile.Read(ref _persistentMultishotRecvUserData);

        /// <summary>
        /// Clears persistent multishot recv armed-state and requests ASYNC_CANCEL for
        /// the armed user_data when available.
        /// </summary>
        internal void RequestPersistentMultishotRecvCancel()
        {
            ulong recvUserData = Volatile.Read(ref _persistentMultishotRecvUserData);
            ClearPersistentMultishotRecvArmed();
            if (recvUserData != 0)
            {
                SocketAsyncEngine? engine = Volatile.Read(ref _asyncEngine);
                engine?.TryRequestIoUringCancellation(recvUserData);
            }
        }

        /// <summary>Copies an early multishot-recv payload into the per-socket replay queue.</summary>
        internal bool TryBufferEarlyPersistentMultishotRecvData(ReadOnlySpan<byte> payload)
        {
            if (payload.Length == 0)
            {
                return true;
            }

            EnsurePersistentMultishotRecvDataQueueInitialized();
            Queue<BufferedPersistentMultishotRecvData>? queue = _persistentMultishotRecvDataQueue;
            if (queue is null)
            {
                return false;
            }

            byte[] copy = ArrayPool<byte>.Shared.Rent(payload.Length);
            payload.CopyTo(copy);
            Lock gate = EnsurePersistentMultishotRecvDataGate();
            lock (gate)
            {
                if (PersistentMultishotRecvBufferedCount >= PersistentMultishotRecvDataQueueMaxSize)
                {
                    ArrayPool<byte>.Shared.Return(copy);
                    return false;
                }

                // Publish queue count only after enqueue to avoid teardown observing phantom items.
                queue.Enqueue(new BufferedPersistentMultishotRecvData(copy, payload.Length, usesPooledBuffer: true));
            }

            return true;
        }

        /// <summary>Attempts to drain buffered multishot-recv payload into the caller destination.</summary>
        internal bool TryConsumeBufferedPersistentMultishotRecvData(Memory<byte> destination, out int bytesTransferred)
        {
            bytesTransferred = 0;
            if (destination.Length == 0)
            {
                return false;
            }

            Lock gate = EnsurePersistentMultishotRecvDataGate();
            byte[] sourceBuffer;
            int sourceOffset;
            int toCopy;
            bool releaseHeadAfterCopy;
            BufferedPersistentMultishotRecvData sourceHead;
            lock (gate)
            {
                if (!TryAcquirePersistentMultishotRecvDataHead(out BufferedPersistentMultishotRecvData buffered))
                {
                    return false;
                }

                int headOffset = _persistentMultishotRecvDataHeadOffset;
                int remaining = buffered.Length - headOffset;
                Debug.Assert(remaining > 0);
                if (remaining <= 0)
                {
                    ReleasePersistentMultishotRecvDataHead();
                    return false;
                }

                toCopy = Math.Min(destination.Length, remaining);
                sourceBuffer = buffered.Data;
                sourceOffset = headOffset;
                sourceHead = buffered;
                _persistentMultishotRecvDataHeadOffset = headOffset + toCopy;
                releaseHeadAfterCopy = _persistentMultishotRecvDataHeadOffset >= buffered.Length;
            }

            sourceBuffer.AsSpan(sourceOffset, toCopy).CopyTo(destination.Span);
            bytesTransferred = toCopy;

            if (releaseHeadAfterCopy)
            {
                lock (gate)
                {
                    if (_hasPersistentMultishotRecvDataHead &&
                        _persistentMultishotRecvDataHead.Length == sourceHead.Length &&
                        ReferenceEquals(_persistentMultishotRecvDataHead.Data, sourceHead.Data) &&
                        _persistentMultishotRecvDataHeadOffset >= sourceHead.Length)
                    {
                        ReleasePersistentMultishotRecvDataHead();
                    }
                }
            }

            return true;
        }

        /// <summary>Ensures the pre-accepted connection queue exists.</summary>
        private void EnsureMultishotAcceptQueueInitialized()
        {
            if (_multishotAcceptQueue is null)
            {
                Lock gate = EnsureMultishotAcceptQueueGate();
                lock (gate)
                {
                    _multishotAcceptQueue ??= new Queue<PreAcceptedConnection>();
                }
            }
        }

        /// <summary>
        /// Attempts to enqueue a pre-accepted connection from a multishot accept CQE.
        /// Caller is responsible for closing <paramref name="acceptedFd"/> when this returns false.
        /// </summary>
        internal bool TryEnqueuePreAcceptedConnection(IntPtr acceptedFd, ReadOnlySpan<byte> socketAddressData, int socketAddressLen)
        {
            EnsureMultishotAcceptQueueInitialized();
            Queue<PreAcceptedConnection>? queue = _multishotAcceptQueue;
            if (queue is null)
            {
                return false;
            }

            int length = socketAddressLen;
            if (length < 0)
            {
                length = 0;
            }

            if ((uint)length > (uint)socketAddressData.Length)
            {
                length = socketAddressData.Length;
            }

            Lock gate = EnsureMultishotAcceptQueueGate();
            lock (gate)
            {
                if (queue.Count >= MultishotAcceptQueueMaxSize)
                {
                    return false;
                }

                byte[] copy;
                if (length != 0)
                {
                    copy = ArrayPool<byte>.Shared.Rent(length);
                    socketAddressData.Slice(0, length).CopyTo(copy);
                }
                else
                {
                    copy = Array.Empty<byte>();
                }

                queue.Enqueue(new PreAcceptedConnection(acceptedFd, copy, length, usesPooledBuffer: length != 0));
            }

            return true;
        }

        /// <summary>
        /// Attempts to dequeue a pre-accepted connection from the multishot accept queue.
        /// Returns true if a connection was available, populating the operation fields.
        /// </summary>
        internal bool TryDequeuePreAcceptedConnection(AcceptOperation operation)
        {
            EnsureMultishotAcceptQueueInitialized();
            Queue<PreAcceptedConnection>? queue = _multishotAcceptQueue;
            if (queue is null)
            {
                return false;
            }

            PreAcceptedConnection accepted;
            Lock gate = EnsureMultishotAcceptQueueGate();
            lock (gate)
            {
                if (queue.Count == 0)
                {
                    return false;
                }

                accepted = queue.Dequeue();
            }

            try
            {
                operation.AcceptedFileDescriptor = accepted.FileDescriptor;
                int socketAddressLen = accepted.SocketAddressLength;
                if ((uint)socketAddressLen > (uint)operation.SocketAddress.Length)
                {
                    socketAddressLen = operation.SocketAddress.Length;
                }

                if (socketAddressLen != 0)
                {
                    accepted.SocketAddressData.AsSpan(0, socketAddressLen).CopyTo(operation.SocketAddress.Span);
                }

                operation.AcceptSocketAddressLength = socketAddressLen;
                operation.SocketAddress = operation.SocketAddress.Slice(0, socketAddressLen);
                operation.ErrorCode = SocketError.Success;
                return true;
            }
            finally
            {
                ReturnPooledBufferIfNeeded(accepted.SocketAddressData, accepted.UsesPooledBuffer);
            }
        }

        /// <summary>Records that a shadow listener's multishot accept SQE was armed on the specified engine.</summary>
        internal void RecordReusePortShadowArmed(ulong userData, int engineIndex)
        {
            Lock gate = EnsureReusePortShadowListenersGate();
            lock (gate)
            {
                ReusePortShadowListenerState[]? shadows = _reusePortShadowListeners;
                if (shadows is null)
                {
                    return;
                }

                for (int i = 0; i < shadows.Length; i++)
                {
                    if (shadows[i].EngineIndex == engineIndex)
                    {
                        shadows[i].ArmedUserData = userData;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Creates SO_REUSEPORT shadow listener sockets on non-primary engines to distribute
        /// incoming connections across all io_uring engines. Called after primary multishot accept
        /// is successfully armed.
        /// </summary>
        internal unsafe void TryCreateReusePortShadowListeners(SocketAsyncEngine primaryEngine)
        {
            if (SocketAsyncEngine.IsReusePortAcceptDisabled() || SocketAsyncEngine.EngineCount <= 1)
            {
                return;
            }

            // Get the primary socket's bound address via getsockname.
            byte* sockAddrBuffer = stackalloc byte[128]; // large enough for sockaddr_storage
            int sockAddrLen = 128;
            Interop.Error getSockNameErr = Interop.Sys.GetSockName(_socket, sockAddrBuffer, &sockAddrLen);
            if (getSockNameErr != Interop.Error.SUCCESS || sockAddrLen <= 0)
            {
                return;
            }

            ReadOnlySpan<byte> boundAddress = new ReadOnlySpan<byte>(sockAddrBuffer, sockAddrLen);

            // Determine socket family, type, protocol from the primary socket.
            Interop.Error getTypeErr = Interop.Sys.GetSocketType(
                _socket,
                out AddressFamily addressFamily,
                out SocketType socketType,
                out ProtocolType protocolType,
                out bool _);
            if (getTypeErr != Interop.Error.SUCCESS)
            {
                return;
            }

            // SO_REUSEPORT must be enabled before the primary bind/listen sequence.
            // If the primary wasn't created with REUSEPORT, shadow binds to the same endpoint
            // won't join the reuseport group and will fail with EADDRINUSE.
            if (!IsPrimarySocketReusePortEnabled())
            {
                return;
            }

            SocketAsyncEngine.EnsureFdEngineAffinityTable();

            int engineCount = SocketAsyncEngine.EngineCount;
            for (int i = 0; i < engineCount; i++)
            {
                SocketAsyncEngine targetEngine = SocketAsyncEngine.GetEngineByIndex(i);
                if (targetEngine == primaryEngine)
                {
                    continue;
                }

                // Create shadow socket.
                IntPtr shadowFd;
                Interop.Error socketErr = Interop.Sys.Socket(
                    (int)addressFamily, (int)socketType, (int)protocolType, &shadowFd);
                if (socketErr != Interop.Error.SUCCESS)
                {
                    continue;
                }

                SafeSocketHandle shadowHandle = new SafeSocketHandle();
                Marshal.InitHandle(shadowHandle, shadowFd);

                bool shadowCreated = false;
                try
                {
                    // Set SO_REUSEPORT.
                    int reusePort = 1;
                    Interop.Error setOptErr = Interop.Sys.SetRawSockOpt(
                        shadowHandle, SolSocket, SoReusePort, (byte*)&reusePort, sizeof(int));
                    if (setOptErr != Interop.Error.SUCCESS)
                    {
                        continue;
                    }

                    // Bind to same address.
                    Interop.Error bindErr = Interop.Sys.Bind(shadowHandle, protocolType, boundAddress);
                    if (bindErr != Interop.Error.SUCCESS)
                    {
                        continue;
                    }

                    // Listen.
                    Interop.Error listenErr = Interop.Sys.Listen(shadowHandle, 512);
                    if (listenErr != Interop.Error.SUCCESS)
                    {
                        continue;
                    }

                    // Enqueue setup request to target engine (SQE arming happens on its event loop).
                    ReusePortShadowListenerState state = new ReusePortShadowListenerState
                    {
                        Handle = shadowHandle,
                        EngineIndex = i,
                        ArmedUserData = 0
                    };

                    // Publish the shadow state before enqueuing setup so RecordReusePortShadowArmed
                    // can always resolve and persist armed user_data from the target event loop.
                    AddReusePortShadowListener(ref state);
                    if (targetEngine.TryEnqueueReusePortShadowSetup(shadowHandle, this, primaryEngine))
                    {
                        shadowCreated = true;
                    }
                    else
                    {
                        RemoveReusePortShadowListenerByEngineIndex(i);
                    }
                }
                finally
                {
                    if (!shadowCreated)
                    {
                        shadowHandle.Dispose();
                    }
                }
            }
        }

        /// <summary>Removes a completed io_uring operation from its queue and signals or dispatches its callback.</summary>
        internal bool TryCompleteIoUringOperation(AsyncOperation operation)
        {
            bool removed =
                operation is ReadOperation readOperation ? _receiveQueue.TryRemoveCompletedOperation(this, readOperation) :
                operation is WriteOperation writeOperation ? _sendQueue.TryRemoveCompletedOperation(this, writeOperation) :
                false;
            if (!removed)
            {
                return false;
            }

            ManualResetEventSlim? e = operation.Event;
            if (e is not null)
            {
                e.Set();
                return true;
            }

            operation.CancellationRegistration.Dispose();
            if (ShouldDispatchCompletionCallback(operation))
            {
                if (PreferInlineCompletions)
                {
                    // Inline completion: invoke directly on the event-loop thread,
                    // matching the epoll path (HandleEventsInline). This avoids the
                    // ThreadPool hop for latency-sensitive workloads that opted in
                    // via DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS=1.
                    operation.InvokeCallback(allowPooling: true);
                }
                else
                {
                    operation.QueueIoUringCompletionCallback();
                }
            }

            return true;
        }

        /// <summary>Enqueues an operation for deferred SQE preparation on the event loop thread.</summary>
        private bool TryEnqueueIoUringPreparation(AsyncOperation operation, long prepareSequence)
        {
            SocketAsyncEngine? engine = Volatile.Read(ref _asyncEngine);
            return engine is not null && engine.TryEnqueueIoUringPreparation(operation, prepareSequence);
        }

        /// <summary>Applies cancellation and/or untracking to an operation's io_uring state.</summary>
        private void HandleIoUringCancellationTransition(
            AsyncOperation operation,
            bool requestKernelCancellation,
            bool untrackAndClear)
        {
            SocketAsyncEngine? engine = Volatile.Read(ref _asyncEngine);
            ulong userData = operation.IoUringUserData;
            if (userData == 0)
            {
                return;
            }

            if (requestKernelCancellation)
            {
                engine?.TryRequestIoUringCancellation(userData);
            }

            if (untrackAndClear)
            {
                bool clearAllowed = engine?.TryUntrackIoUringOperation(userData, operation) ?? true;
                if (clearAllowed)
                {
                    operation.ClearIoUringUserData();
                }
            }
        }

        /// <summary>Requests kernel-level ASYNC_CANCEL for an in-flight operation.</summary>
        private void TryRequestIoUringCancellation(AsyncOperation operation)
        {
            HandleIoUringCancellationTransition(
                operation,
                requestKernelCancellation: true,
                untrackAndClear: false);
        }

        /// <summary>Removes an operation from the registry and clears its user_data.</summary>
        internal void TryUntrackIoUringOperation(AsyncOperation operation)
        {
            HandleIoUringCancellationTransition(
                operation,
                requestKernelCancellation: false,
                untrackAndClear: true);
        }

        /// <summary>Stages an operation for io_uring preparation if completion mode is active.</summary>
        static partial void LinuxTryStageIoUringOperation(AsyncOperation operation)
        {
            if (operation.Event is null &&
                operation.AssociatedContext.IsIoUringCompletionModeEnabled() &&
                operation.IoUringUserData == 0 &&
                operation.IsInWaitingState())
            {
                if (!operation.TryQueueIoUringPreparation())
                {
                    operation.EmitReadinessFallbackForQueueOverflow();
                }
            }
        }

        partial void LinuxTryDequeuePreAcceptedConnection(AcceptOperation operation, ref bool dequeued)
        {
            dequeued = TryDequeuePreAcceptedConnection(operation);
        }

        partial void LinuxHasBufferedPersistentMultishotRecvData(ref bool hasBuffered)
        {
            Lock gate = EnsurePersistentMultishotRecvDataGate();
            lock (gate)
            {
                hasBuffered = PersistentMultishotRecvBufferedCount > 0;
            }
        }

        partial void LinuxTryConsumeBufferedPersistentMultishotRecvData(Memory<byte> destination, ref bool consumed, ref int bytesTransferred)
        {
            consumed = TryConsumeBufferedPersistentMultishotRecvData(destination, out bytesTransferred);
        }

        /// <summary>Cleans up multishot-accept state and queued pre-accepted descriptors during abort.</summary>
        partial void LinuxOnStopAndAbort()
        {
            SocketAsyncEngine? engine = Volatile.Read(ref _asyncEngine);
            if (IsPersistentMultishotRecvArmed())
            {
                RequestPersistentMultishotRecvCancel();
            }

            ulong armedUserData = GetArmedMultishotAcceptUserDataForCancellation();
            if (engine is not null && armedUserData != 0)
            {
                engine.TryRequestIoUringCancellation(armedUserData);
            }

            DisarmMultishotAccept();

            // Clean up SO_REUSEPORT shadow listeners.
            ReusePortShadowListenerState[]? shadows;
            Lock shadowGate = EnsureReusePortShadowListenersGate();
            lock (shadowGate)
            {
                shadows = _reusePortShadowListeners;
                _reusePortShadowListeners = null;
            }

            if (shadows is not null)
            {
                for (int i = 0; i < shadows.Length; i++)
                {
                    ref ReusePortShadowListenerState shadow = ref shadows[i];
                    if (shadow.ArmedUserData != 0)
                    {
                        SocketAsyncEngine targetEngine = SocketAsyncEngine.GetEngineByIndex(shadow.EngineIndex);
                        targetEngine.TryRequestIoUringCancellation(shadow.ArmedUserData);
                    }

                    shadow.Handle?.Dispose();
                }
            }

            Queue<PreAcceptedConnection>? multishotAcceptQueue = _multishotAcceptQueue;
            if (multishotAcceptQueue is not null)
            {
                while (true)
                {
                    PreAcceptedConnection accepted;
                    Lock gate = EnsureMultishotAcceptQueueGate();
                    lock (gate)
                    {
                        if (multishotAcceptQueue.Count == 0)
                        {
                            break;
                        }

                        accepted = multishotAcceptQueue.Dequeue();
                    }

                    Interop.Sys.Close(accepted.FileDescriptor);
                    ReturnPooledBufferIfNeeded(accepted.SocketAddressData, accepted.UsesPooledBuffer);
                }
            }

            Lock persistentGate = EnsurePersistentMultishotRecvDataGate();
            lock (persistentGate)
            {
                ReleasePersistentMultishotRecvDataHead();

                Queue<BufferedPersistentMultishotRecvData>? bufferedQueue = _persistentMultishotRecvDataQueue;
                if (bufferedQueue is not null)
                {
                    while (bufferedQueue.Count != 0)
                    {
                        BufferedPersistentMultishotRecvData buffered = bufferedQueue.Dequeue();
                        ReturnPooledBufferIfNeeded(buffered.Data, buffered.UsesPooledBuffer);
                    }
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePersistentMultishotRecvDataQueueInitialized()
        {
            if (_persistentMultishotRecvDataQueue is null)
            {
                Lock gate = EnsurePersistentMultishotRecvDataGate();
                lock (gate)
                {
                    _persistentMultishotRecvDataQueue ??= new Queue<BufferedPersistentMultishotRecvData>();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAcquirePersistentMultishotRecvDataHead(out BufferedPersistentMultishotRecvData buffered)
        {
            if (_hasPersistentMultishotRecvDataHead)
            {
                buffered = _persistentMultishotRecvDataHead;
                return true;
            }

            Queue<BufferedPersistentMultishotRecvData>? queue = _persistentMultishotRecvDataQueue;
            if (queue is null || queue.Count == 0)
            {
                buffered = default;
                return false;
            }

            BufferedPersistentMultishotRecvData dequeued = queue.Dequeue();
            _persistentMultishotRecvDataHead = dequeued;
            _hasPersistentMultishotRecvDataHead = true;
            _persistentMultishotRecvDataHeadOffset = 0;
            buffered = dequeued;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleasePersistentMultishotRecvDataHead()
        {
            if (!_hasPersistentMultishotRecvDataHead)
            {
                return;
            }

            BufferedPersistentMultishotRecvData head = _persistentMultishotRecvDataHead;
            _persistentMultishotRecvDataHead = default;
            _hasPersistentMultishotRecvDataHead = false;
            _persistentMultishotRecvDataHeadOffset = 0;
            ReturnPooledBufferIfNeeded(head.Data, head.UsesPooledBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledBufferIfNeeded(byte[] buffer, bool usesPooledBuffer)
        {
            if (usesPooledBuffer)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private ulong GetArmedMultishotAcceptUserDataForCancellation()
        {
            long packedState = Volatile.Read(ref _multishotAcceptState);
            ulong userData = DecodeMultishotAcceptUserData(packedState);
            if (userData != 0 || packedState == MultishotAcceptStateDisarmed)
            {
                return userData;
            }

            // A transient "arming without published user_data" state can race this read.
            // Bounded spin is best-effort; a miss is benign because later cancellation
            // and teardown paths still unarm/cleanup safely.
            SpinWait spinner = default;
            do
            {
                spinner.SpinOnce();
                packedState = Volatile.Read(ref _multishotAcceptState);
                userData = DecodeMultishotAcceptUserData(packedState);
                if (userData != 0 || packedState == MultishotAcceptStateDisarmed)
                {
                    break;
                }
            } while (!spinner.NextSpinWillYield);

            return userData;
        }

        internal abstract partial class AsyncOperation
        {
            /// <summary>Outcome of processing an io_uring CQE, determining the dispatch action.</summary>
            internal enum IoUringCompletionResult
            {
                Completed = 0,
                Pending = 1,
                Canceled = 2,
                Ignored = 3
            }

            /// <summary>Tri-state result from direct (managed) SQE preparation.</summary>
            internal enum IoUringDirectPrepareResult
            {
                Unsupported = 0,   // Direct path unavailable for this shape; caller keeps operation pending.
                Prepared = 1,      // SQE written
                PrepareFailed = 2, // Direct preparation failed; caller handles retry/fallback without native prepare.
                CompletedFromBuffer = 3  // Operation completed synchronously from early-buffer data; no SQE needed.
            }

            /// <summary>Tracks whether a receive operation prepared as one-shot or multishot.</summary>
            internal enum IoUringReceiveSubmissionMode : byte
            {
                None = 0,
                OneShot = 1,
                Multishot = 2
            }

            private long _ioUringPrepareSequence;
            private int _ioUringPrepareQueued;
            private int _ioUringPreparationReusable;
            private MemoryHandle _ioUringPinnedBuffer;
            private int _ioUringPinnedBufferActive;
            private int _ioUringCompletionSocketAddressLen;
            private int _ioUringCompletionControlBufferLen;
            private int _ioUringReceiveSubmissionMode;
            private int _ioUringSlotExhaustionRetryCount;
            internal ulong IoUringUserData;

            /// <summary>Requests kernel cancellation if the flag is set.</summary>
            partial void LinuxRequestIoUringCancellationIfNeeded(bool requestIoUringCancellation)
            {
                if (requestIoUringCancellation)
                {
                    AssociatedContext.TryRequestIoUringCancellation(this);
                }
            }

            /// <summary>Untracks this operation unless it is in the Canceled state awaiting a terminal CQE.</summary>
            partial void LinuxUntrackIoUringOperation()
            {
                // Canceled operations remain tracked until the terminal CQE arrives so that
                // pinned/user-owned resources are not released while the kernel may still
                // reference them. Dispatch will clear resources on that terminal completion.
                if (_state == State.Canceled)
                {
                    return;
                }

                AssociatedContext.TryUntrackIoUringOperation(this);
            }

            /// <summary>Resets all io_uring preparation state and advances the prepare sequence.</summary>
            partial void ResetIoUringState()
            {
                ReleaseIoUringPreparationResources();
                IoUringUserData = 0;
                Volatile.Write(ref _ioUringPreparationReusable, 0);
                _ioUringCompletionSocketAddressLen = 0;
                _ioUringCompletionControlBufferLen = 0;
                _ioUringReceiveSubmissionMode = (int)IoUringReceiveSubmissionMode.None;
                _ioUringSlotExhaustionRetryCount = 0;
                long nextPrepareSequence = unchecked(_ioUringPrepareSequence + 1);
                // Keep sequence strictly positive so stale queued work from previous resets never matches.
                if (nextPrepareSequence <= 0)
                {
                    nextPrepareSequence = 1;
                }

                Volatile.Write(ref _ioUringPrepareSequence, nextPrepareSequence);
                Volatile.Write(ref _ioUringPrepareQueued, 0);
            }

            /// <summary>Marks this operation as ready for SQE preparation and returns its sequence number.</summary>
            internal long MarkReadyForIoUringPreparation()
            {
                long prepareSequence = Volatile.Read(ref _ioUringPrepareSequence);
                Debug.Assert(prepareSequence > 0);
                Volatile.Write(ref _ioUringPrepareQueued, 1);
                return prepareSequence;
            }

            /// <summary>Cancels a pending preparation if the sequence number still matches.</summary>
            internal void CancelPendingIoUringPreparation(long prepareSequence)
            {
                if (Volatile.Read(ref _ioUringPrepareSequence) == prepareSequence)
                {
                    Volatile.Write(ref _ioUringPrepareQueued, 0);
                }
            }

            /// <summary>Attempts to prepare an SQE for this operation via the managed direct path.</summary>
            internal bool TryPrepareIoUring(SocketAsyncContext context, long prepareSequence)
            {
                long observedPrepareSequence = Volatile.Read(ref _ioUringPrepareSequence);
                bool waiting = _state == State.Waiting;
                if (prepareSequence <= 0 ||
                    observedPrepareSequence != prepareSequence ||
                    !waiting)
                {
                    return false;
                }

                // Consume the queued flag only for a currently valid sequence/state pair.
                // Stale work items must not clear a newer queued prepare request.
                if (Interlocked.CompareExchange(ref _ioUringPrepareQueued, 0, 1) == 0)
                {
                    return false;
                }

                if (Interlocked.Exchange(ref _ioUringPreparationReusable, 0) == 0)
                {
                    ReleaseIoUringPreparationResources();
                }

                SocketAsyncEngine? engine = Volatile.Read(ref context._asyncEngine);
                if (engine is null || !engine.IsIoUringDirectSqeEnabled)
                {
                    // Managed completion mode assumes direct SQE submission.
                    // If direct submission is unavailable, keep operation pending for fallback handling.
                    ErrorCode = SocketError.Success;
                    IoUringUserData = 0;
                    return false;
                }

                IoUringDirectPrepareResult directResult = IoUringPrepareDirect(context, engine, out ulong directUserData);
                if (directResult == IoUringDirectPrepareResult.CompletedFromBuffer)
                {
                    // Operation completed synchronously from early-buffer data during prepare.
                    // Transition to Complete; caller will dispatch the completion callback.
                    _state = State.Complete;
                    IoUringUserData = 0;
                    return false;
                }

                if (directResult == IoUringDirectPrepareResult.Prepared)
                {
                    _ioUringSlotExhaustionRetryCount = 0;
                    IoUringUserData = ErrorCode == SocketError.Success ? directUserData : 0;
                    return true;
                }

                if (directResult == IoUringDirectPrepareResult.PrepareFailed)
                {
                    IoUringUserData = 0;
                    return false;
                }

                // Direct preparation unsupported for this operation shape.
                // Leave operation pending so caller can use completion-path fallback semantics.
                ErrorCode = SocketError.Success;
                IoUringUserData = 0;
                return false;
            }

            /// <summary>Queues this operation for deferred preparation on the event loop thread.</summary>
            internal bool TryQueueIoUringPreparation()
            {
                if (!AssociatedContext.IsIoUringCompletionModeEnabled())
                {
                    return false;
                }

                long prepareSequence = MarkReadyForIoUringPreparation();
                if (AssociatedContext.TryEnqueueIoUringPreparation(this, prepareSequence))
                {
                    return true;
                }

                CancelPendingIoUringPreparation(prepareSequence);
                return false;
            }

            /// <summary>Returns whether this operation is currently in the waiting state.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool IsInWaitingState() => _state == State.Waiting;
            internal bool IsInCompletedState() => _state == State.Complete;

            /// <summary>Increments and returns the slot-exhaustion retry count for this operation.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal int IncrementIoUringSlotExhaustionRetryCount() => ++_ioUringSlotExhaustionRetryCount;

            /// <summary>Resets slot-exhaustion retry tracking for this operation.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void ResetIoUringSlotExhaustionRetryCount() => _ioUringSlotExhaustionRetryCount = 0;

            /// <summary>
            /// Emits a readiness fallback event when io_uring prepare-queue staging fails.
            /// </summary>
            internal void EmitReadinessFallbackForQueueOverflow()
            {
                Interop.Sys.SocketEvents fallbackEvents = GetIoUringFallbackSocketEvents();
                if (fallbackEvents == Interop.Sys.SocketEvents.None)
                {
                    return;
                }

                SocketAsyncContext context = AssociatedContext;
                SocketAsyncEngine? engine = Volatile.Read(ref context._asyncEngine);
                if (engine is null)
                {
                    return;
                }

                // Queue-overflow fallback still needs completion-mode re-prepare semantics:
                // mark the operation so the next readiness-driven EAGAIN path restages an SQE.
                RequestIoUringFallbackReprepare();

                engine.EnqueueReadinessFallbackEvent(
                    context,
                    fallbackEvents,
                    countAsPrepareQueueOverflowFallback: true);
            }

            /// <summary>Processes a CQE result and returns the dispatch action for the completion handler.</summary>
            internal IoUringCompletionResult ProcessIoUringCompletionResult(int result, uint flags, uint auxiliaryData)
            {
                Trace($"Enter, result={result}, flags={flags}, auxiliaryData={auxiliaryData}");

                // Claim ownership of completion processing; if cancellation already won, do not publish completion.
                State oldState = Interlocked.CompareExchange(ref _state, State.Running, State.Waiting);
                if (oldState == State.Canceled)
                {
                    Trace("Exit, previously canceled");
                    return IoUringCompletionResult.Canceled;
                }

                if (oldState != State.Waiting)
                {
                    Trace("Exit, ignored");
                    return IoUringCompletionResult.Ignored;
                }

                if (ProcessIoUringCompletionViaDiscriminator(AssociatedContext, result, auxiliaryData))
                {
                    _state = State.Complete;
                    Trace("Exit, completed");
                    return IoUringCompletionResult.Completed;
                }

                // Incomplete path (e.g. transient retry): mirror TryComplete state transition handling.
                State newState;
                while (true)
                {
                    State state = _state;
                    Debug.Assert(state is State.Running or State.RunningWithPendingCancellation, $"Unexpected operation state: {(State)state}");

                    newState = (state == State.Running ? State.Waiting : State.Canceled);
                    if (state == Interlocked.CompareExchange(ref _state, newState, state))
                    {
                        break;
                    }
                }

                if (newState == State.Canceled)
                {
                    ProcessCancellation();
                    Trace("Exit, canceled while pending");
                    return IoUringCompletionResult.Canceled;
                }

                Trace("Exit, pending");
                return IoUringCompletionResult.Pending;
            }

            /// <summary>Stores recvmsg output lengths from the CQE for post-completion processing.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void SetIoUringCompletionMessageMetadata(int socketAddressLen, int controlBufferLen)
            {
                _ioUringCompletionSocketAddressLen = socketAddressLen;
                _ioUringCompletionControlBufferLen = controlBufferLen;
            }

            /// <summary>Releases preparation resources and resets the user_data to zero.</summary>
            internal void ClearIoUringUserData()
            {
                ReleaseIoUringPreparationResources();
                IoUringUserData = 0;
                Volatile.Write(ref _ioUringPreparationReusable, 0);
                _ioUringCompletionSocketAddressLen = 0;
                _ioUringCompletionControlBufferLen = 0;
                _ioUringReceiveSubmissionMode = (int)IoUringReceiveSubmissionMode.None;
                _ioUringSlotExhaustionRetryCount = 0;
            }

            /// <summary>Clears user_data without releasing preparation resources for pending requeue.</summary>
            internal void ResetIoUringUserDataForRequeue()
            {
                IoUringUserData = 0;
                _ioUringCompletionSocketAddressLen = 0;
                _ioUringCompletionControlBufferLen = 0;
            }

            /// <summary>Records whether the current receive preparation uses one-shot or multishot mode.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void SetIoUringReceiveSubmissionMode(IoUringReceiveSubmissionMode mode)
            {
                Volatile.Write(ref _ioUringReceiveSubmissionMode, (int)mode);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected bool IsIoUringBufferPinned() =>
                Volatile.Read(ref _ioUringPinnedBufferActive) != 0;

            /// <summary>Marks preparation resources as reusable so the next prepare skips re-pinning.</summary>
            internal void MarkIoUringPreparationReusable()
            {
                Volatile.Write(ref _ioUringPreparationReusable, 1);
            }

            /// <summary>Socket address length reported by the kernel in the CQE.</summary>
            protected int IoUringCompletionSocketAddressLen => _ioUringCompletionSocketAddressLen;
            /// <summary>Control buffer length reported by the kernel in the CQE.</summary>
            protected int IoUringCompletionControlBufferLen => _ioUringCompletionControlBufferLen;

            /// <summary>Pins a buffer and returns the raw pointer, recording the handle for later release.</summary>
            protected unsafe byte* PinIoUringBuffer(Memory<byte> buffer)
            {
                ReleasePinnedIoUringBuffer();
                if (buffer.Length == 0)
                {
                    return null;
                }

                _ioUringPinnedBuffer = buffer.Pin();
                Volatile.Write(ref _ioUringPinnedBufferActive, 1);
                return (byte*)_ioUringPinnedBuffer.Pointer;
            }

            /// <summary>Attempts to pin a buffer, falling back to the readiness path if not pinnable.</summary>
            protected unsafe bool TryPinIoUringBuffer(Memory<byte> buffer, out byte* pinnedBuffer)
            {
                if (Volatile.Read(ref _ioUringPinnedBufferActive) != 0)
                {
                    pinnedBuffer = (byte*)_ioUringPinnedBuffer.Pointer;
                    if (buffer.Length > 0 && pinnedBuffer is null)
                    {
                        ReleasePinnedIoUringBuffer();
                        RecordIoUringNonPinnablePrepareFallback();
                        ErrorCode = SocketError.Success;
                        return false;
                    }

                    return true;
                }

                try
                {
                    pinnedBuffer = PinIoUringBuffer(buffer);
                    if (buffer.Length > 0 && pinnedBuffer is null)
                    {
                        ReleasePinnedIoUringBuffer();
                        RecordIoUringNonPinnablePrepareFallback();
                        ErrorCode = SocketError.Success;
                        return false;
                    }

                    return true;
                }
                catch (NotSupportedException)
                {
                    pinnedBuffer = null;
                    RecordIoUringNonPinnablePrepareFallback();
                    ErrorCode = SocketError.Success;
                    return false;
                }
            }

            /// <summary>Transfers ownership of the active pinned buffer to the caller.</summary>
            internal MemoryHandle TransferPinnedBuffer()
            {
                if (Interlocked.Exchange(ref _ioUringPinnedBufferActive, 0) == 0)
                {
                    return default;
                }

                MemoryHandle pinnedBuffer = _ioUringPinnedBuffer;
                _ioUringPinnedBuffer = default;
                return pinnedBuffer;
            }

            /// <summary>
            /// Attempts to pin a socket address buffer, reusing an existing pin when possible.
            /// Caller is responsible for setting operation ErrorCode on failure if needed.
            /// </summary>
            protected static unsafe bool TryPinIoUringSocketAddress(
                Memory<byte> socketAddress,
                ref MemoryHandle pinnedSocketAddress,
                ref int pinnedSocketAddressActive,
                out byte* rawSocketAddress)
            {
                rawSocketAddress = null;
                if (socketAddress.Length == 0)
                {
                    return true;
                }

                if (Volatile.Read(ref pinnedSocketAddressActive) != 0)
                {
                    rawSocketAddress = (byte*)pinnedSocketAddress.Pointer;
                    if (rawSocketAddress is null)
                    {
                        pinnedSocketAddress.Dispose();
                        pinnedSocketAddress = default;
                        Volatile.Write(ref pinnedSocketAddressActive, 0);
                        return false;
                    }

                    return true;
                }

                try
                {
                    pinnedSocketAddress = socketAddress.Pin();
                    Volatile.Write(ref pinnedSocketAddressActive, 1);
                }
                catch (NotSupportedException)
                {
                    rawSocketAddress = null;
                    return false;
                }

                rawSocketAddress = (byte*)pinnedSocketAddress.Pointer;
                if (rawSocketAddress is null)
                {
                    pinnedSocketAddress.Dispose();
                    pinnedSocketAddress = default;
                    Volatile.Write(ref pinnedSocketAddressActive, 0);
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Pins a socket address buffer and normalizes pinning failures to a non-terminal fallback signal.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected unsafe bool TryPinIoUringSocketAddressForPrepare(
                Memory<byte> socketAddress,
                ref MemoryHandle pinnedSocketAddress,
                ref int pinnedSocketAddressActive,
                out byte* rawSocketAddress)
            {
                if (TryPinIoUringSocketAddress(
                    socketAddress,
                    ref pinnedSocketAddress,
                    ref pinnedSocketAddressActive,
                    out rawSocketAddress))
                {
                    return true;
                }

                ErrorCode = SocketError.Success;
                return false;
            }

            /// <summary>Releases an operation-owned pinned socket-address buffer and message-header allocation.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static unsafe void ReleaseIoUringSocketAddressAndMessageHeader(
                ref MemoryHandle pinnedSocketAddress,
                ref int pinnedSocketAddressActive,
                ref IntPtr messageHeader)
            {
                if (Interlocked.Exchange(ref pinnedSocketAddressActive, 0) != 0)
                {
                    pinnedSocketAddress.Dispose();
                    pinnedSocketAddress = default;
                }

                IntPtr header = Interlocked.Exchange(ref messageHeader, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)header);
                }
            }

            /// <summary>Records a telemetry counter for a non-pinnable buffer fallback.</summary>
            private void RecordIoUringNonPinnablePrepareFallback()
            {
                SocketAsyncEngine? engine = Volatile.Read(ref AssociatedContext._asyncEngine);
                if (engine is null || !engine.IsIoUringCompletionModeEnabled)
                {
                    return;
                }

                engine.RecordIoUringNonPinnablePrepareFallback();
            }

            /// <summary>Releases the currently pinned buffer handle if active.</summary>
            private void ReleasePinnedIoUringBuffer()
            {
                if (Interlocked.Exchange(ref _ioUringPinnedBufferActive, 0) != 0)
                {
                    _ioUringPinnedBuffer.Dispose();
                    _ioUringPinnedBuffer = default;
                }
            }

            /// <summary>Releases the pinned buffer when the operation shape (single vs list) changes.</summary>
            protected void ReleaseIoUringPinnedBufferForShapeTransition() =>
                ReleasePinnedIoUringBuffer();

            /// <summary>Releases all preparation resources including the pinned buffer and subclass resources.</summary>
            private void ReleaseIoUringPreparationResources()
            {
                ReleasePinnedIoUringBuffer();
                ReleaseIoUringPreparationResourcesCore();
            }

            /// <summary>Subclass hook to release operation-specific preparation resources.</summary>
            protected virtual void ReleaseIoUringPreparationResourcesCore()
            {
            }

            /// <summary>Frees a set of GCHandles used for buffer list pinning.</summary>
            protected static void ReleasePinnedHandles(GCHandle[] pinnedHandles, int count)
            {
                if (count <= 0)
                {
                    return;
                }

                int releaseCount = count < pinnedHandles.Length ? count : pinnedHandles.Length;
                for (int i = 0; i < releaseCount; i++)
                {
                    if (pinnedHandles[i].IsAllocated)
                    {
                        pinnedHandles[i].Free();
                    }
                }
            }

            /// <summary>Rents an array from the shared pool for temporary io_uring preparation use.</summary>
            private static T[] RentIoUringArray<T>(int minimumLength) =>
                minimumLength == 0 ? Array.Empty<T>() : ArrayPool<T>.Shared.Rent(minimumLength);

            /// <summary>Returns a rented array to the shared pool.</summary>
            private static void ReturnIoUringArray<T>(T[] array, bool clearArray = false)
            {
                if (array.Length != 0)
                {
                    ArrayPool<T>.Shared.Return(array, clearArray);
                }
            }

            /// <summary>Releases pinned handles and returns the iovec array to the pool.</summary>
            protected static void ReleaseIoUringPinnedHandlesAndIovecs(
                ref GCHandle[]? pinnedHandles,
                ref Interop.Sys.IOVector[]? iovecs,
                ref int pinnedHandleCount)
            {
                GCHandle[]? handles = Interlocked.Exchange(ref pinnedHandles, null);
                int handleCount = Interlocked.Exchange(ref pinnedHandleCount, 0);
                if (handles is not null)
                {
                    ReleasePinnedHandles(handles, handleCount);
                    ReturnIoUringArray(handles, clearArray: true);
                }

                Interop.Sys.IOVector[]? vectors = Interlocked.Exchange(ref iovecs, null);
                if (vectors is not null)
                {
                    ReturnIoUringArray(vectors, clearArray: true);
                }
            }

            /// <summary>Pins a list of buffer segments and builds an iovec array for scatter/gather I/O.</summary>
            protected static unsafe bool TryPinBufferListForIoUring(
                IList<ArraySegment<byte>> buffers,
                int startIndex,
                int startOffset,
                out GCHandle[] pinnedHandles,
                out Interop.Sys.IOVector[] iovecs,
                out int iovCount,
                out int pinnedHandleCount,
                out SocketError errorCode)
            {
                iovCount = 0;
                pinnedHandleCount = 0;
                if ((uint)startIndex > (uint)buffers.Count)
                {
                    errorCode = SocketError.InvalidArgument;
                    pinnedHandles = Array.Empty<GCHandle>();
                    iovecs = Array.Empty<Interop.Sys.IOVector>();
                    return false;
                }

                int remainingBufferCount = buffers.Count - startIndex;
                pinnedHandles = RentIoUringArray<GCHandle>(remainingBufferCount);
                iovecs = RentIoUringArray<Interop.Sys.IOVector>(remainingBufferCount);

                int currentOffset = startOffset;
                byte[]? lastPinnedArray = null;
                GCHandle lastPinnedHandle = default;
                try
                {
                    for (int i = 0; i < remainingBufferCount; i++, currentOffset = 0)
                    {
                        ArraySegment<byte> buffer = buffers[startIndex + i];
                        RangeValidationHelpers.ValidateSegment(buffer);

                        if ((uint)currentOffset > (uint)buffer.Count)
                        {
                            ReleasePinnedHandles(pinnedHandles, pinnedHandleCount);
                            ReturnIoUringArray(pinnedHandles, clearArray: true);
                            ReturnIoUringArray(iovecs, clearArray: true);
                            errorCode = SocketError.InvalidArgument;
                            return false;
                        }

                        int bufferCount = buffer.Count - currentOffset;
                        byte* basePtr = null;
                        if (bufferCount != 0)
                        {
                            byte[] array = buffer.Array!;
                            GCHandle handle;
                            if (ReferenceEquals(array, lastPinnedArray))
                            {
                                handle = lastPinnedHandle;
                            }
                            else
                            {
                                handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                                pinnedHandles[pinnedHandleCount] = handle;
                                pinnedHandleCount++;
                                lastPinnedArray = array;
                                lastPinnedHandle = handle;
                            }

                            basePtr = &((byte*)handle.AddrOfPinnedObject())[buffer.Offset + currentOffset];
                        }

                        iovecs[i].Base = basePtr;
                        iovecs[i].Count = (UIntPtr)bufferCount;
                        iovCount++;
                    }
                }
                catch
                {
                    ReleasePinnedHandles(pinnedHandles, pinnedHandleCount);
                    ReturnIoUringArray(pinnedHandles, clearArray: true);
                    ReturnIoUringArray(iovecs, clearArray: true);
                    throw;
                }

                errorCode = SocketError.Success;
                return true;
            }

            /// <summary>Prepares an SQE via the managed direct path. Override in subclasses for direct submission.</summary>
            protected virtual IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                return IoUringDirectPrepareResult.Unsupported;
            }

            /// <summary>
            /// Routes a CQE using an operation-kind discriminator to avoid virtual completion dispatch
            /// on this hot path.
            /// </summary>
            private bool ProcessIoUringCompletionViaDiscriminator(SocketAsyncContext context, int result, uint auxiliaryData)
            {
                IoUringCompletionDispatchKind kind = GetIoUringCompletionDispatchKind();
                if (result >= 0)
                {
                    return kind switch
                    {
                        IoUringCompletionDispatchKind.BufferListSendOperation => ((BufferListSendOperation)this).ProcessIoUringCompletionSuccessBufferListSend(result),
                        IoUringCompletionDispatchKind.BufferMemoryReceiveOperation => ((BufferMemoryReceiveOperation)this).ProcessIoUringCompletionSuccessBufferMemoryReceive(result, auxiliaryData),
                        IoUringCompletionDispatchKind.BufferListReceiveOperation => ((BufferListReceiveOperation)this).ProcessIoUringCompletionSuccessBufferListReceive(result, auxiliaryData),
                        IoUringCompletionDispatchKind.ReceiveMessageFromOperation => ((ReceiveMessageFromOperation)this).ProcessIoUringCompletionSuccessReceiveMessageFrom(result, auxiliaryData),
                        IoUringCompletionDispatchKind.AcceptOperation => ((AcceptOperation)this).ProcessIoUringCompletionSuccessAccept(result, auxiliaryData),
                        IoUringCompletionDispatchKind.ConnectOperation => ((ConnectOperation)this).ProcessIoUringCompletionSuccessConnect(context),
                        IoUringCompletionDispatchKind.SendOperation => ((SendOperation)this).ProcessIoUringCompletionSuccessSend(result),
                        _ => ProcessIoUringCompletionSuccessDefault(result)
                    };
                }

                return kind switch
                {
                    IoUringCompletionDispatchKind.ReceiveMessageFromOperation => ((ReceiveMessageFromOperation)this).ProcessIoUringCompletionErrorReceiveMessageFrom(result),
                    IoUringCompletionDispatchKind.AcceptOperation => ((AcceptOperation)this).ProcessIoUringCompletionErrorAccept(result),
                    IoUringCompletionDispatchKind.ConnectOperation => ((ConnectOperation)this).ProcessIoUringCompletionErrorConnect(context, result),
                    IoUringCompletionDispatchKind.ReadOperation or
                    IoUringCompletionDispatchKind.BufferMemoryReceiveOperation or
                    IoUringCompletionDispatchKind.BufferListReceiveOperation => ((ReadOperation)this).ProcessIoUringCompletionErrorRead(result),
                    IoUringCompletionDispatchKind.WriteOperation or
                    IoUringCompletionDispatchKind.SendOperation or
                    IoUringCompletionDispatchKind.BufferListSendOperation => ((WriteOperation)this).ProcessIoUringCompletionErrorWrite(result),
                    _ => ProcessIoUringCompletionErrorDefault(result)
                };
            }

            /// <summary>Processes a successful (non-negative) io_uring completion result.</summary>
            private bool ProcessIoUringCompletionSuccessDefault(int result)
            {
                Debug.Assert(result >= 0, $"Expected non-negative io_uring result, got {result}");
                ErrorCode = SocketError.Success;
                return true;
            }

            /// <summary>Processes a failed (negative) io_uring completion result.</summary>
            private bool ProcessIoUringCompletionErrorDefault(int result)
            {
                Debug.Assert(result < 0, $"Expected negative io_uring result, got {result}");
                ErrorCode = SocketPal.GetSocketErrorForErrorCode(GetIoUringPalError(result));
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private IoUringCompletionDispatchKind GetIoUringCompletionDispatchKind()
            {
                int dispatchKind = _ioUringCompletionDispatchKind;
                return dispatchKind != 0 ?
                    (IoUringCompletionDispatchKind)dispatchKind :
                    IoUringCompletionDispatchKind.Default;
            }

            /// <summary>Whether preparation resources should be preserved when the operation is requeued.</summary>
            internal virtual bool ShouldReuseIoUringPreparationResourcesOnPending => false;

            /// <summary>Returns whether the negative result represents EAGAIN/EWOULDBLOCK.</summary>
            protected static bool IsIoUringRetryableError(int result)
            {
                if (result >= 0)
                {
                    return false;
                }

                Interop.Error error = GetIoUringPalError(result);
                return error == Interop.Error.EAGAIN || error == Interop.Error.EWOULDBLOCK;
            }

            /// <summary>Converts a negative io_uring result to a SocketError, returning false for retryable errors.</summary>
            protected static bool ProcessIoUringErrorResult(int result, out SocketError errorCode)
            {
                Debug.Assert(result < 0, $"Expected negative io_uring result, got {result}");

                if (IsIoUringRetryableError(result))
                {
                    errorCode = SocketError.Success;
                    return false;
                }

                errorCode = SocketPal.GetSocketErrorForErrorCode(GetIoUringPalError(result));
                return true;
            }

            /// <summary>Converts a negative io_uring CQE result (raw -errno) to PAL error space.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static Interop.Error GetIoUringPalError(int result)
            {
                Debug.Assert(result < 0, $"Expected negative io_uring result, got {result}");
                int platformErrno = -result;
                return Interop.Sys.ConvertErrorPlatformToPal(platformErrno);
            }

            /// <summary>Returns the epoll event mask to use when falling back from io_uring to readiness notification.</summary>
            internal virtual Interop.Sys.SocketEvents GetIoUringFallbackSocketEvents() =>
                Interop.Sys.SocketEvents.None;

            /// <summary>
            /// Copies payload bytes from a provided-buffer ring selection into the operation's target memory.
            /// Returns false when this operation shape does not support provided-buffer payload materialization.
            /// </summary>
            internal virtual unsafe bool TryProcessIoUringProvidedBufferCompletion(
                byte* providedBuffer,
                int providedBufferLength,
                int bytesTransferred,
                ref uint auxiliaryData)
            {
                _ = providedBuffer;
                _ = providedBufferLength;
                _ = bytesTransferred;
                _ = auxiliaryData;
                return false;
            }
        }

        internal abstract partial class ReadOperation
        {
            internal bool ProcessIoUringCompletionErrorRead(int result) =>
                ProcessIoUringErrorResult(result, out ErrorCode);

            /// <inheritdoc />
            // Retained only for defensive fallback paths; regular completion mode avoids readiness fallback.
            internal override Interop.Sys.SocketEvents GetIoUringFallbackSocketEvents() =>
                Interop.Sys.SocketEvents.Read;
        }

        private abstract partial class WriteOperation
        {
            internal bool ProcessIoUringCompletionErrorWrite(int result) =>
                ProcessIoUringErrorResult(result, out ErrorCode);

            /// <inheritdoc />
            // Retained only for defensive fallback paths; regular completion mode avoids readiness fallback.
            internal override Interop.Sys.SocketEvents GetIoUringFallbackSocketEvents() =>
                Interop.Sys.SocketEvents.Write;
        }

        private abstract partial class SendOperation
        {
            internal bool ProcessIoUringCompletionSuccessSend(int result)
            {
                if (result == 0)
                {
                    // A zero-byte completion for a non-empty send payload indicates peer close
                    // on stream sockets; report reset instead of a spurious success/0-byte write.
                    if (Count > 0)
                    {
                        ErrorCode = SocketError.ConnectionReset;
                        return true;
                    }

                    ErrorCode = SocketError.Success;
                    return true;
                }

                Debug.Assert(result > 0, $"Expected positive io_uring send completion size, got {result}");
                Debug.Assert(result <= Count, $"Unexpected io_uring send completion size: result={result}, count={Count}");

                int sent = Math.Min(result, Count);
                BytesTransferred += sent;
                Offset += sent;
                Count -= sent;
                ErrorCode = SocketError.Success;
                return Count == 0;
            }
        }

        private partial class BufferMemorySendOperation
        {
            private IntPtr _ioUringMessageHeader;
            private MemoryHandle _ioUringPinnedSocketAddress;
            private int _ioUringPinnedSocketAddressActive;

            /// <inheritdoc />
            internal override bool ShouldReuseIoUringPreparationResourcesOnPending => true;

            /// <inheritdoc />
            protected override unsafe void ReleaseIoUringPreparationResourcesCore()
            {
                ReleaseIoUringSocketAddressAndMessageHeader(
                    ref _ioUringPinnedSocketAddress,
                    ref _ioUringPinnedSocketAddressActive,
                    ref _ioUringMessageHeader);
            }

            /// <summary>Gets a message header buffer and sets the common sendmsg fields.</summary>
            private unsafe Interop.Sys.MessageHeader* GetOrCreateIoUringSendMessageHeader(byte* rawSocketAddress)
            {
                Interop.Sys.MessageHeader* messageHeader = (Interop.Sys.MessageHeader*)_ioUringMessageHeader;
                if (messageHeader is null)
                {
                    messageHeader = (Interop.Sys.MessageHeader*)NativeMemory.Alloc((nuint)sizeof(Interop.Sys.MessageHeader));
                    _ioUringMessageHeader = (IntPtr)messageHeader;
                }

                messageHeader->SocketAddress = rawSocketAddress;
                messageHeader->SocketAddressLen = SocketAddress.Length;
                messageHeader->ControlBuffer = null;
                messageHeader->ControlBufferLen = 0;
                messageHeader->Flags = SocketFlags.None;
                return messageHeader;
            }

            /// <summary>Configures a message header with zero or one iovec entry.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe void ConfigureSingleIov(
                Interop.Sys.MessageHeader* messageHeader,
                byte* rawBuffer,
                int bufferLength,
                Interop.Sys.IOVector* iov)
            {
                if (bufferLength == 0)
                {
                    messageHeader->IOVectors = null;
                    messageHeader->IOVectorCount = 0;
                    return;
                }

                iov->Base = rawBuffer;
                iov->Count = (UIntPtr)bufferLength;
                messageHeader->IOVectors = iov;
                messageHeader->IOVectorCount = 1;
            }

            /// <summary>Builds a connected send or sendmsg preparation request.</summary>
            private unsafe IoUringDirectPrepareResult IoUringPrepareDirectSendMessage(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                if (!TryPinIoUringSocketAddressForPrepare(
                    SocketAddress,
                    ref _ioUringPinnedSocketAddress,
                    ref _ioUringPinnedSocketAddressActive,
                    out byte* rawSocketAddress))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if (!TryPinIoUringBuffer(Buffer, out byte* rawBuffer))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if (rawBuffer is not null)
                {
                    rawBuffer += Offset;
                }

                Interop.Sys.MessageHeader* messageHeader = GetOrCreateIoUringSendMessageHeader(rawSocketAddress);
                Interop.Sys.IOVector sendIov;
                ConfigureSingleIov(messageHeader, rawBuffer, Count, &sendIov);

                IoUringDirectPrepareResult sendMessagePrepareResult = engine.TryPrepareIoUringDirectSendMessageWithZeroCopyFallback(
                    context._socket,
                    messageHeader,
                    Count,
                    Flags,
                    out userData,
                    out SocketError sendMessageErrorCode);
                ErrorCode = sendMessageErrorCode;
                return sendMessagePrepareResult;
            }

            /// <inheritdoc />
            protected override unsafe IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                if (SocketAddress.Length == 0)
                {
                    if (!TryPinIoUringBuffer(Buffer, out byte* rawBuffer))
                    {
                        return IoUringDirectPrepareResult.PrepareFailed;
                    }

                    if (rawBuffer is not null)
                    {
                        rawBuffer += Offset;
                    }

                    IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectSendWithZeroCopyFallback(
                        context._socket,
                        rawBuffer,
                        Count,
                        Flags,
                        out bool usedZeroCopy,
                        out userData,
                        out SocketError errorCode);
                    ErrorCode = errorCode;
                    if (usedZeroCopy && prepareResult == IoUringDirectPrepareResult.Prepared)
                    {
                        engine.TransferIoUringZeroCopyPinHold(userData, TransferPinnedBuffer());
                    }

                    return prepareResult;
                }

                return IoUringPrepareDirectSendMessage(context, engine, out userData);
            }
        }

        private sealed partial class BufferListSendOperation
        {
            private GCHandle[]? _ioUringPinnedBufferHandles;
            private Interop.Sys.IOVector[]? _ioUringIovecs;
            private int _ioUringPinnedHandleCount;
            private int _ioUringPreparedBufferCount = -1;
            private int _ioUringPreparedStartIndex = -1;
            private int _ioUringPreparedStartOffset = -1;
            private int _ioUringPreparedIovCount;

            /// <inheritdoc />
            internal override bool ShouldReuseIoUringPreparationResourcesOnPending => true;

            /// <inheritdoc />
            protected override void ReleaseIoUringPreparationResourcesCore()
            {
                ReleaseIoUringPinnedHandlesAndIovecs(ref _ioUringPinnedBufferHandles, ref _ioUringIovecs, ref _ioUringPinnedHandleCount);
                _ioUringPreparedBufferCount = -1;
                _ioUringPreparedStartIndex = -1;
                _ioUringPreparedStartOffset = -1;
                _ioUringPreparedIovCount = 0;
            }

            /// <summary>Pins buffer segments starting at BufferIndex/Offset and builds the iovec array.</summary>
            private bool TryPinIoUringBuffers(
                IList<ArraySegment<byte>> buffers,
                int startIndex,
                int startOffset,
                out int iovCount)
            {
                if (_ioUringPinnedBufferHandles is not null &&
                    _ioUringIovecs is not null &&
                    _ioUringPreparedBufferCount == buffers.Count &&
                    _ioUringPreparedStartIndex == startIndex &&
                    _ioUringPreparedStartOffset == startOffset &&
                    _ioUringPreparedIovCount <= _ioUringIovecs.Length)
                {
                    iovCount = _ioUringPreparedIovCount;
                    return true;
                }

                // Release any existing pinned handles and rented arrays before creating new ones.
                // This handles the partial-send case where BufferIndex/Offset advanced, causing the
                // reuse check above to fail while old resources are still held.
                ReleaseIoUringPinnedHandlesAndIovecs(ref _ioUringPinnedBufferHandles, ref _ioUringIovecs, ref _ioUringPinnedHandleCount);

                if (!TryPinBufferListForIoUring(
                        buffers,
                        startIndex,
                        startOffset,
                        out GCHandle[] pinnedHandles,
                        out Interop.Sys.IOVector[] iovecs,
                        out iovCount,
                        out int pinnedHandleCount,
                        out SocketError errorCode))
                {
                    ErrorCode = errorCode;
                    return false;
                }

                _ioUringPinnedBufferHandles = pinnedHandles;
                _ioUringIovecs = iovecs;
                _ioUringPinnedHandleCount = pinnedHandleCount;
                _ioUringPreparedBufferCount = buffers.Count;
                _ioUringPreparedStartIndex = startIndex;
                _ioUringPreparedStartOffset = startOffset;
                _ioUringPreparedIovCount = iovCount;
                return true;
            }

            /// <summary>Advances the buffer position after a partial send, returning true when all data is sent.</summary>
            private bool AdvanceSendBufferPosition(int bytesSent)
            {
                IList<ArraySegment<byte>>? buffers = Buffers;
                if (buffers is null || bytesSent <= 0)
                {
                    return buffers is null || BufferIndex >= buffers.Count;
                }

                int remaining = bytesSent;
                int index = BufferIndex;
                int offset = Offset;

                while (remaining > 0 && index < buffers.Count)
                {
                    int available = buffers[index].Count - offset;
                    Debug.Assert(available >= 0, "Unexpected negative buffer availability during io_uring send completion.");

                    if (available > remaining)
                    {
                        offset += remaining;
                        break;
                    }

                    remaining -= Math.Max(available, 0);
                    index++;
                    offset = 0;
                }

                BufferIndex = index;
                Offset = offset;
                return index >= buffers.Count;
            }

            /// <inheritdoc />
            protected override unsafe IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                if (context.IsPersistentMultishotRecvArmed())
                {
                    context.RequestPersistentMultishotRecvCancel();
                }

                IList<ArraySegment<byte>>? buffers = Buffers;
                if (buffers is null)
                {
                    ErrorCode = SocketError.Success;
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if ((uint)BufferIndex > (uint)buffers.Count)
                {
                    ErrorCode = SocketError.Success;
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if (!TryPinIoUringBuffers(buffers, BufferIndex, Offset, out int iovCount))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                byte* rawSocketAddress = null;
                if (SocketAddress.Length != 0 && !TryPinIoUringBuffer(SocketAddress, out rawSocketAddress))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                Interop.Sys.MessageHeader messageHeader;
                messageHeader.SocketAddress = rawSocketAddress;
                messageHeader.SocketAddressLen = SocketAddress.Length;
                messageHeader.ControlBuffer = null;
                messageHeader.ControlBufferLen = 0;
                messageHeader.Flags = SocketFlags.None;

                Interop.Sys.IOVector[] iovecs = _ioUringIovecs!;
                if (iovCount != 0)
                {
                    fixed (Interop.Sys.IOVector* iovecsPtr = &iovecs[0])
                    {
                        messageHeader.IOVectors = iovecsPtr;
                        messageHeader.IOVectorCount = iovCount;
                        // Buffer-list sends can be many small segments (e.g. 4KB chunks). Use
                        // aggregate payload size for zero-copy eligibility, not per-segment size.
                        long totalPayloadBytes = 0;
                        for (int i = 0; i < iovCount; i++)
                        {
                            totalPayloadBytes += (long)(nuint)iovecs[i].Count;
                            if (totalPayloadBytes >= int.MaxValue)
                            {
                                totalPayloadBytes = int.MaxValue;
                                break;
                            }
                        }

                        IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectSendMessageWithZeroCopyFallback(
                            context._socket,
                            &messageHeader,
                            (int)totalPayloadBytes,
                            Flags,
                            out userData,
                            out SocketError errorCode);
                        ErrorCode = errorCode;
                        return prepareResult;
                    }
                }

                messageHeader.IOVectors = null;
                messageHeader.IOVectorCount = 0;
                IoUringDirectPrepareResult zeroIovPrepareResult = engine.TryPrepareIoUringDirectSendMessageWithZeroCopyFallback(
                    context._socket,
                    &messageHeader,
                    payloadLength: 0,
                    Flags,
                    out userData,
                    out SocketError zeroIovErrorCode);
                ErrorCode = zeroIovErrorCode;
                return zeroIovPrepareResult;
            }

            internal bool ProcessIoUringCompletionSuccessBufferListSend(int result)
            {
                if (result == 0)
                {
                    // Buffer-list sends can represent empty payloads; only treat result=0 as
                    // reset when there are still bytes pending across remaining segments.
                    if (HasPendingBufferListSendBytes())
                    {
                        ErrorCode = SocketError.ConnectionReset;
                        return true;
                    }

                    ErrorCode = SocketError.Success;
                    return true;
                }

                Debug.Assert(result > 0, $"Expected positive io_uring send completion size, got {result}");
                BytesTransferred += result;
                bool complete = AdvanceSendBufferPosition(result);
                ErrorCode = SocketError.Success;
                return complete;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool HasPendingBufferListSendBytes()
            {
                IList<ArraySegment<byte>>? buffers = Buffers;
                if (buffers is null || BufferIndex >= buffers.Count)
                {
                    return false;
                }

                int index = BufferIndex;
                int offset = Offset;
                while (index < buffers.Count)
                {
                    int available = buffers[index].Count - offset;
                    if (available > 0)
                    {
                        return true;
                    }

                    index++;
                    offset = 0;
                }

                return false;
            }
        }

        private sealed partial class BufferMemoryReceiveOperation
        {
            private IntPtr _ioUringMessageHeader;
            private MemoryHandle _ioUringPinnedSocketAddress;
            private int _ioUringPinnedSocketAddressActive;

            /// <inheritdoc />
            internal override bool ShouldReuseIoUringPreparationResourcesOnPending => true;

            /// <inheritdoc />
            protected override unsafe void ReleaseIoUringPreparationResourcesCore()
            {
                ReleaseIoUringSocketAddressAndMessageHeader(
                    ref _ioUringPinnedSocketAddress,
                    ref _ioUringPinnedSocketAddressActive,
                    ref _ioUringMessageHeader);
            }

            /// <summary>Gets a message header buffer and sets the common recvmsg fields.</summary>
            private unsafe Interop.Sys.MessageHeader* GetOrCreateIoUringReceiveMessageHeader(byte* rawSocketAddress)
            {
                Interop.Sys.MessageHeader* messageHeader = (Interop.Sys.MessageHeader*)_ioUringMessageHeader;
                if (messageHeader is null)
                {
                    messageHeader = (Interop.Sys.MessageHeader*)NativeMemory.Alloc((nuint)sizeof(Interop.Sys.MessageHeader));
                    _ioUringMessageHeader = (IntPtr)messageHeader;
                }

                InitializeReceiveMessageHeader(messageHeader, rawSocketAddress);
                return messageHeader;
            }

            /// <summary>Initializes recvmsg header fields shared by direct preparation variants.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void InitializeReceiveMessageHeader(Interop.Sys.MessageHeader* messageHeader, byte* rawSocketAddress)
            {
                messageHeader->SocketAddress = rawSocketAddress;
                messageHeader->SocketAddressLen = SocketAddress.Length;
                messageHeader->ControlBuffer = null;
                messageHeader->ControlBufferLen = 0;
                messageHeader->Flags = SocketFlags.None;
            }

            /// <summary>Configures a message header with a single iovec entry.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe void ConfigureSingleIov(
                Interop.Sys.MessageHeader* messageHeader,
                byte* rawBuffer,
                int bufferLength,
                Interop.Sys.IOVector* iov)
            {
                // Keep a single iovec even for zero-length receives so recvmsg preserves
                // completion-mode readiness probe behavior for zero-byte operations.
                iov->Base = rawBuffer;
                iov->Count = (UIntPtr)bufferLength;
                messageHeader->IOVectors = iov;
                messageHeader->IOVectorCount = 1;
            }

            /// <summary>Builds a connected or receive-from recvmsg operation.</summary>
            private unsafe IoUringDirectPrepareResult IoUringPrepareDirectReceiveMessage(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                if (!TryPinIoUringBuffer(Buffer, out byte* rawBuffer))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if (!TryPinIoUringSocketAddressForPrepare(
                    SocketAddress,
                    ref _ioUringPinnedSocketAddress,
                    ref _ioUringPinnedSocketAddressActive,
                    out byte* rawSocketAddress))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                Interop.Sys.MessageHeader* messageHeader = GetOrCreateIoUringReceiveMessageHeader(rawSocketAddress);
                Interop.Sys.IOVector receiveIov;
                ConfigureSingleIov(messageHeader, rawBuffer, Buffer.Length, &receiveIov);

                IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectReceiveMessage(
                    context._socket,
                    messageHeader,
                    Flags,
                    out userData,
                    out SocketError errorCode);
                ErrorCode = errorCode;
                return prepareResult;
            }

            /// <summary>
            /// Returns whether this operation shape is eligible for multishot recv submission.
            /// Eligible: connected TCP receive (no socket address, no recvmsg flags) with non-empty buffer.
            /// Ineligible: zero-byte probes, recvmsg-based receive paths (SetReceivedFlags/socket address).
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool IsEligibleForIoUringMultishotRecv()
            {
                if (SetReceivedFlags || SocketAddress.Length != 0)
                {
                    return false;
                }

                // Multishot recv uses IORING_OP_RECV (no msg_flags). Message-oriented sockets
                // rely on MSG_TRUNC to report truncation, which is not observable in this path.
                if (SocketPal.GetSockOpt(
                        AssociatedContext._socket,
                        SocketOptionLevel.Socket,
                        SocketOptionName.Type,
                        out int socketTypeValue) != SocketError.Success)
                {
                    // If type probing fails, keep completion correctness by disabling multishot recv.
                    return false;
                }

                SocketType socketType = (SocketType)socketTypeValue;
                if (socketType == SocketType.Dgram ||
                    socketType == SocketType.Raw ||
                    socketType == SocketType.Seqpacket)
                {
                    return false;
                }

                return Buffer.Length != 0;
            }

            /// <inheritdoc />
            protected override unsafe IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                if (SetReceivedFlags || SocketAddress.Length != 0)
                {
                    if (context.IsPersistentMultishotRecvArmed())
                    {
                        context.RequestPersistentMultishotRecvCancel();
                    }

                    SetIoUringReceiveSubmissionMode(IoUringReceiveSubmissionMode.OneShot);
                    IoUringDirectPrepareResult receiveMessagePrepareResult =
                        IoUringPrepareDirectReceiveMessage(context, engine, out userData);
                    if (receiveMessagePrepareResult != IoUringDirectPrepareResult.Prepared || ErrorCode != SocketError.Success)
                    {
                        SetIoUringReceiveSubmissionMode(IoUringReceiveSubmissionMode.None);
                    }

                    return receiveMessagePrepareResult;
                }

                bool allowMultishotRecv = IsEligibleForIoUringMultishotRecv() && engine.SupportsMultishotRecv;
                if (!allowMultishotRecv && context.IsPersistentMultishotRecvArmed())
                {
                    context.RequestPersistentMultishotRecvCancel();
                }

                SetIoUringReceiveSubmissionMode(
                    allowMultishotRecv ? IoUringReceiveSubmissionMode.Multishot : IoUringReceiveSubmissionMode.OneShot);

                // Before piggybacking, check the early-buffer for data that may have arrived
                // between DoTryComplete's check (on ThreadPool) and this prepare (on event loop).
                // Without this check, piggyback would wait for a CQE that never comes while the
                // buffer has unconsumed data—a race between ThreadPool buffer consumption and
                // event loop CQE-driven buffer fill.
                if (allowMultishotRecv && !SetReceivedFlags && SocketAddress.Length == 0 &&
                    context.TryConsumeBufferedPersistentMultishotRecvData(Buffer, out int earlyBufferedBytes))
                {
                    BytesTransferred = earlyBufferedBytes;
                    ReceivedFlags = SocketFlags.None;
                    ErrorCode = SocketError.Success;
                    userData = 0;
                    return IoUringDirectPrepareResult.CompletedFromBuffer;
                }

                // Persistent multishot receive: if one is already armed, attach this operation to
                // that existing user_data instead of submitting a new recv SQE.
                if (allowMultishotRecv && context.IsPersistentMultishotRecvArmed())
                {
                    ulong armedUserData = context.PersistentMultishotRecvUserData;
                    bool replaced = armedUserData != 0 &&
                        engine.TryReplaceIoUringTrackedOperation(armedUserData, this);
                    if (replaced)
                    {
                        userData = armedUserData;
                        ErrorCode = SocketError.Success;
                        return IoUringDirectPrepareResult.Prepared;
                    }

                    // Stale armed-state; clear and submit a fresh SQE below.
                    context.ClearPersistentMultishotRecvArmed();
                }

                bool bufferAlreadyPinned = IsIoUringBufferPinned();
                if (!TryPinIoUringBuffer(Buffer, out byte* rawBuffer))
                {
                    ErrorCode = SocketError.Success;
                    SetIoUringReceiveSubmissionMode(IoUringReceiveSubmissionMode.None);
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectRecv(
                    context._socket,
                    rawBuffer,
                    Buffer.Length,
                    Flags,
                    allowMultishotRecv,
                    bufferAlreadyPinned,
                    out userData,
                    out SocketError errorCode);
                ErrorCode = errorCode;
                if (allowMultishotRecv &&
                    prepareResult == IoUringDirectPrepareResult.Prepared &&
                    errorCode == SocketError.Success)
                {
                    context.SetPersistentMultishotRecvArmed(userData);
                }

                if (prepareResult != IoUringDirectPrepareResult.Prepared || errorCode != SocketError.Success)
                {
                    SetIoUringReceiveSubmissionMode(IoUringReceiveSubmissionMode.None);
                }

                return prepareResult;
            }

            internal bool ProcessIoUringCompletionSuccessBufferMemoryReceive(int result, uint auxiliaryData)
            {
                BytesTransferred = result;
                ReceivedFlags = SetReceivedFlags ? (SocketFlags)(int)auxiliaryData : SocketFlags.None;
                if (result >= 0)
                {
                    AssociatedContext.TryMigrateIoUringEngineOnFirstReceiveCompletion();
                }

                if (SocketAddress.Length != 0)
                {
                    int socketAddressLen = IoUringCompletionSocketAddressLen;
                    if (socketAddressLen < 0)
                    {
                        socketAddressLen = 0;
                    }

                    if ((uint)socketAddressLen > (uint)SocketAddress.Length)
                    {
                        socketAddressLen = SocketAddress.Length;
                    }

                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }
                ErrorCode = SocketError.Success;
                return true;
            }

            /// <inheritdoc />
            internal override unsafe bool TryProcessIoUringProvidedBufferCompletion(
                byte* providedBuffer,
                int providedBufferLength,
                int bytesTransferred,
                ref uint auxiliaryData)
            {
                _ = auxiliaryData;

                if (bytesTransferred <= 0)
                {
                    return true;
                }

                if (SetReceivedFlags || SocketAddress.Length != 0)
                {
                    return false;
                }

                if ((uint)bytesTransferred > (uint)providedBufferLength ||
                    (uint)bytesTransferred > (uint)Buffer.Length)
                {
                    return false;
                }

                new ReadOnlySpan<byte>(providedBuffer, bytesTransferred).CopyTo(Buffer.Span);
                return true;
            }
        }

        private sealed partial class BufferListReceiveOperation
        {
            private GCHandle[]? _ioUringPinnedBufferHandles;
            private Interop.Sys.IOVector[]? _ioUringIovecs;
            private int _ioUringPinnedHandleCount;
            private IntPtr _ioUringMessageHeader;
            private int _ioUringPreparedIovCount;
            private int _ioUringPreparedBufferCount = -1;

            /// <inheritdoc />
            internal override bool ShouldReuseIoUringPreparationResourcesOnPending => true;

            /// <inheritdoc />
            protected override unsafe void ReleaseIoUringPreparationResourcesCore()
            {
                ReleaseIoUringPinnedHandlesAndIovecs(ref _ioUringPinnedBufferHandles, ref _ioUringIovecs, ref _ioUringPinnedHandleCount);
                _ioUringPreparedIovCount = 0;
                _ioUringPreparedBufferCount = -1;

                IntPtr messageHeader = Interlocked.Exchange(ref _ioUringMessageHeader, IntPtr.Zero);
                if (messageHeader != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)messageHeader);
                }
            }

            /// <summary>Pins all buffer segments and builds the iovec array.</summary>
            private bool TryPinIoUringBuffers(IList<ArraySegment<byte>> buffers, out int iovCount)
            {
                if (_ioUringPinnedBufferHandles is not null &&
                    _ioUringIovecs is not null &&
                    _ioUringPreparedIovCount != 0 &&
                    _ioUringPreparedIovCount <= _ioUringIovecs.Length &&
                    _ioUringPreparedBufferCount == buffers.Count)
                {
                    iovCount = _ioUringPreparedIovCount;
                    return true;
                }

                ReleaseIoUringPinnedHandlesAndIovecs(ref _ioUringPinnedBufferHandles, ref _ioUringIovecs, ref _ioUringPinnedHandleCount);

                if (!TryPinBufferListForIoUring(
                        buffers,
                        startIndex: 0,
                        startOffset: 0,
                        out GCHandle[] pinnedHandles,
                        out Interop.Sys.IOVector[] iovecs,
                        out iovCount,
                        out int pinnedHandleCount,
                        out SocketError errorCode))
                {
                    ErrorCode = errorCode;
                    return false;
                }

                _ioUringPinnedBufferHandles = pinnedHandles;
                _ioUringIovecs = iovecs;
                _ioUringPinnedHandleCount = pinnedHandleCount;
                _ioUringPreparedIovCount = iovCount;
                _ioUringPreparedBufferCount = buffers.Count;
                return true;
            }

            /// <inheritdoc />
            protected override unsafe IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                IList<ArraySegment<byte>>? buffers = Buffers;
                if (buffers is null)
                {
                    ErrorCode = SocketError.Success;
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if (!TryPinIoUringBuffers(buffers, out int iovCount))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                byte* rawSocketAddress = null;
                if (SocketAddress.Length != 0 && !TryPinIoUringBuffer(SocketAddress, out rawSocketAddress))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                Interop.Sys.MessageHeader* messageHeader = (Interop.Sys.MessageHeader*)_ioUringMessageHeader;
                if (messageHeader is null)
                {
                    messageHeader = (Interop.Sys.MessageHeader*)NativeMemory.Alloc((nuint)sizeof(Interop.Sys.MessageHeader));
                    _ioUringMessageHeader = (IntPtr)messageHeader;
                }

                messageHeader->SocketAddress = rawSocketAddress;
                messageHeader->SocketAddressLen = SocketAddress.Length;
                messageHeader->ControlBuffer = null;
                messageHeader->ControlBufferLen = 0;
                messageHeader->Flags = SocketFlags.None;

                Interop.Sys.IOVector[] iovecs = _ioUringIovecs!;
                if (iovCount != 0)
                {
                    fixed (Interop.Sys.IOVector* iovecsPtr = &iovecs[0])
                    {
                        messageHeader->IOVectors = iovecsPtr;
                        messageHeader->IOVectorCount = iovCount;
                        IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectReceiveMessage(
                            context._socket,
                            messageHeader,
                            Flags,
                            out userData,
                            out SocketError errorCode);
                        ErrorCode = errorCode;
                        return prepareResult;
                    }
                }

                messageHeader->IOVectors = null;
                messageHeader->IOVectorCount = 0;
                IoUringDirectPrepareResult zeroIovPrepareResult = engine.TryPrepareIoUringDirectReceiveMessage(
                    context._socket,
                    messageHeader,
                    Flags,
                    out userData,
                    out SocketError zeroIovErrorCode);
                ErrorCode = zeroIovErrorCode;
                return zeroIovPrepareResult;
            }

            internal unsafe bool ProcessIoUringCompletionSuccessBufferListReceive(int result, uint auxiliaryData)
            {
                BytesTransferred = result;
                ReceivedFlags = (SocketFlags)(int)auxiliaryData;
                ErrorCode = SocketError.Success;
                if (result >= 0)
                {
                    AssociatedContext.TryMigrateIoUringEngineOnFirstReceiveCompletion();
                }

                if (_ioUringMessageHeader != IntPtr.Zero && SocketAddress.Length != 0)
                {
                    int socketAddressLen = IoUringCompletionSocketAddressLen;
                    if (socketAddressLen < 0)
                    {
                        socketAddressLen = 0;
                    }

                    if ((uint)socketAddressLen > (uint)SocketAddress.Length)
                    {
                        socketAddressLen = SocketAddress.Length;
                    }

                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }

                return true;
            }
        }

        private sealed partial class ReceiveMessageFromOperation
        {
            private GCHandle[]? _ioUringPinnedBufferHandles;
            private Interop.Sys.IOVector[]? _ioUringIovecs;
            private int _ioUringPinnedHandleCount;
            private int _ioUringPreparedIovCount;
            private int _ioUringPreparedBufferListCount = -1;
            private IntPtr _ioUringMessageHeader;
            private IntPtr _ioUringControlBuffer;
            private int _ioUringControlBufferLength;
            private MemoryHandle _ioUringPinnedSocketAddress;
            private int _ioUringPinnedSocketAddressActive;

            /// <inheritdoc />
            internal override bool ShouldReuseIoUringPreparationResourcesOnPending => true;

            /// <inheritdoc />
            protected override unsafe void ReleaseIoUringPreparationResourcesCore()
            {
                ReleaseIoUringPinnedHandlesAndIovecs(ref _ioUringPinnedBufferHandles, ref _ioUringIovecs, ref _ioUringPinnedHandleCount);
                _ioUringPreparedIovCount = 0;
                _ioUringPreparedBufferListCount = -1;

                IntPtr controlBuffer = Interlocked.Exchange(ref _ioUringControlBuffer, IntPtr.Zero);
                if (controlBuffer != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)controlBuffer);
                }
                _ioUringControlBufferLength = 0;

                ReleaseIoUringSocketAddressAndMessageHeader(
                    ref _ioUringPinnedSocketAddress,
                    ref _ioUringPinnedSocketAddressActive,
                    ref _ioUringMessageHeader);
            }

            /// <summary>Pins buffer segments and builds the iovec array for recvmsg.</summary>
            private bool TryPinIoUringBuffers(IList<ArraySegment<byte>> buffers, out int iovCount)
            {
                if (_ioUringPinnedBufferHandles is not null &&
                    _ioUringIovecs is not null &&
                    _ioUringPreparedIovCount <= _ioUringIovecs.Length &&
                    _ioUringPreparedBufferListCount == buffers.Count)
                {
                    iovCount = _ioUringPreparedIovCount;
                    return true;
                }

                ReleaseIoUringPinnedHandlesAndIovecs(ref _ioUringPinnedBufferHandles, ref _ioUringIovecs, ref _ioUringPinnedHandleCount);

                if (!TryPinBufferListForIoUring(
                        buffers,
                        startIndex: 0,
                        startOffset: 0,
                        out GCHandle[] pinnedHandles,
                        out Interop.Sys.IOVector[] iovecs,
                        out iovCount,
                        out int pinnedHandleCount,
                        out SocketError errorCode))
                {
                    ErrorCode = errorCode;
                    return false;
                }

                _ioUringPinnedBufferHandles = pinnedHandles;
                _ioUringIovecs = iovecs;
                _ioUringPinnedHandleCount = pinnedHandleCount;
                _ioUringPreparedIovCount = iovCount;
                _ioUringPreparedBufferListCount = buffers.Count;
                return true;
            }

            /// <inheritdoc />
            protected override unsafe IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                if (context.IsPersistentMultishotRecvArmed())
                {
                    context.RequestPersistentMultishotRecvCancel();
                }

                IList<ArraySegment<byte>>? buffers = Buffers;
                byte* rawBuffer = null;
                int iovCount;
                if (buffers is not null)
                {
                    ReleaseIoUringPinnedBufferForShapeTransition();
                    if (!TryPinIoUringBuffers(buffers, out iovCount))
                    {
                        return IoUringDirectPrepareResult.PrepareFailed;
                    }
                }
                else
                {
                    if (!TryPinIoUringBuffer(Buffer, out rawBuffer))
                    {
                        return IoUringDirectPrepareResult.PrepareFailed;
                    }

                    if (_ioUringPinnedBufferHandles is not null || _ioUringIovecs is not null)
                    {
                        ReleaseIoUringPinnedHandlesAndIovecs(ref _ioUringPinnedBufferHandles, ref _ioUringIovecs, ref _ioUringPinnedHandleCount);
                        _ioUringPreparedIovCount = 0;
                        _ioUringPreparedBufferListCount = -1;
                    }

                    iovCount = 1;
                }

                if (!TryPinIoUringSocketAddressForPrepare(
                    SocketAddress,
                    ref _ioUringPinnedSocketAddress,
                    ref _ioUringPinnedSocketAddressActive,
                    out byte* rawSocketAddress))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                Interop.Sys.MessageHeader* messageHeader = (Interop.Sys.MessageHeader*)_ioUringMessageHeader;
                if (messageHeader is null)
                {
                    messageHeader = (Interop.Sys.MessageHeader*)NativeMemory.Alloc((nuint)sizeof(Interop.Sys.MessageHeader));
                    _ioUringMessageHeader = (IntPtr)messageHeader;
                }

                messageHeader->SocketAddress = rawSocketAddress;
                messageHeader->SocketAddressLen = SocketAddress.Length;
                messageHeader->Flags = SocketFlags.None;

                int controlBufferLen = Interop.Sys.GetControlMessageBufferSize(Convert.ToInt32(IsIPv4), Convert.ToInt32(IsIPv6));
                if (controlBufferLen < 0)
                {
                    ErrorCode = SocketError.Success;
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if (controlBufferLen != 0)
                {
                    if (_ioUringControlBuffer == IntPtr.Zero || _ioUringControlBufferLength != controlBufferLen)
                    {
                        IntPtr controlBuffer = Interlocked.Exchange(ref _ioUringControlBuffer, IntPtr.Zero);
                        if (controlBuffer != IntPtr.Zero)
                        {
                            NativeMemory.Free((void*)controlBuffer);
                        }

                        void* rawControlBuffer = NativeMemory.Alloc((nuint)controlBufferLen);
                        _ioUringControlBuffer = (IntPtr)rawControlBuffer;
                        _ioUringControlBufferLength = controlBufferLen;
                    }

                    messageHeader->ControlBuffer = (byte*)_ioUringControlBuffer;
                    messageHeader->ControlBufferLen = controlBufferLen;
                }
                else
                {
                    IntPtr controlBuffer = Interlocked.Exchange(ref _ioUringControlBuffer, IntPtr.Zero);
                    if (controlBuffer != IntPtr.Zero)
                    {
                        NativeMemory.Free((void*)controlBuffer);
                    }

                    _ioUringControlBufferLength = 0;
                    messageHeader->ControlBuffer = null;
                    messageHeader->ControlBufferLen = 0;
                }

                if (buffers is not null)
                {
                    Interop.Sys.IOVector[] iovecs = _ioUringIovecs!;
                    if (iovCount != 0)
                    {
                        fixed (Interop.Sys.IOVector* iovecsPtr = &iovecs[0])
                        {
                            messageHeader->IOVectors = iovecsPtr;
                            messageHeader->IOVectorCount = iovCount;
                            IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectReceiveMessage(
                                context._socket,
                                messageHeader,
                                Flags,
                                out userData,
                                out SocketError errorCode);
                            ErrorCode = errorCode;
                            return prepareResult;
                        }
                    }

                    messageHeader->IOVectors = null;
                    messageHeader->IOVectorCount = 0;
                    IoUringDirectPrepareResult zeroIovPrepareResult = engine.TryPrepareIoUringDirectReceiveMessage(
                        context._socket,
                        messageHeader,
                        Flags,
                        out userData,
                        out SocketError zeroIovErrorCode);
                    ErrorCode = zeroIovErrorCode;
                    return zeroIovPrepareResult;
                }

                Interop.Sys.IOVector iov;
                iov.Base = rawBuffer;
                iov.Count = (UIntPtr)Buffer.Length;
                messageHeader->IOVectors = &iov;
                messageHeader->IOVectorCount = 1;
                IoUringDirectPrepareResult singleBufferPrepareResult = engine.TryPrepareIoUringDirectReceiveMessage(
                    context._socket,
                    messageHeader,
                    Flags,
                    out userData,
                    out SocketError singleBufferErrorCode);
                ErrorCode = singleBufferErrorCode;
                return singleBufferPrepareResult;
            }

            internal unsafe bool ProcessIoUringCompletionSuccessReceiveMessageFrom(int result, uint auxiliaryData)
            {
                BytesTransferred = result;
                ReceivedFlags = (SocketFlags)(int)auxiliaryData;
                ErrorCode = SocketError.Success;
                IPPacketInformation = default;
                if (result >= 0)
                {
                    AssociatedContext.TryMigrateIoUringEngineOnFirstReceiveCompletion();
                }

                if (_ioUringMessageHeader != IntPtr.Zero)
                {
                    Interop.Sys.MessageHeader* messageHeader = (Interop.Sys.MessageHeader*)_ioUringMessageHeader;
                    int socketAddressCapacity = SocketAddress.Length;
                    int socketAddressLen = IoUringCompletionSocketAddressLen;
                    if (socketAddressLen < 0)
                    {
                        socketAddressLen = 0;
                    }

                    if ((uint)socketAddressLen > (uint)socketAddressCapacity)
                    {
                        socketAddressLen = socketAddressCapacity;
                    }

                    if (socketAddressLen == 0 && socketAddressCapacity != 0)
                    {
                        socketAddressLen = socketAddressCapacity;
                        SocketAddress.Span.Clear();
                    }

                    int controlBufferCapacity = messageHeader->ControlBufferLen;
                    int controlBufferLen = IoUringCompletionControlBufferLen;
                    if (controlBufferLen < 0)
                    {
                        controlBufferLen = 0;
                    }

                    if ((uint)controlBufferLen > (uint)controlBufferCapacity)
                    {
                        controlBufferLen = controlBufferCapacity;
                    }

                    messageHeader->SocketAddressLen = socketAddressLen;
                    messageHeader->ControlBufferLen = controlBufferLen;
                    messageHeader->Flags = ReceivedFlags;

                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);

                    IPPacketInformation = SocketPal.GetIoUringIPPacketInformation(messageHeader, IsIPv4, IsIPv6);
                }

                return true;
            }

            internal bool ProcessIoUringCompletionErrorReceiveMessageFrom(int result)
            {
                if (!ProcessIoUringErrorResult(result, out ErrorCode))
                {
                    return false;
                }

                IPPacketInformation = default;
                return true;
            }
        }

        internal sealed partial class AcceptOperation
        {
            /// <inheritdoc />
            internal override Interop.Sys.SocketEvents GetIoUringFallbackSocketEvents() =>
                Interop.Sys.SocketEvents.Read;

            /// <inheritdoc />
            protected override unsafe IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                AcceptSocketAddressLength = SocketAddress.Length;
                if (!TryPinIoUringBuffer(SocketAddress, out byte* rawSocketAddress))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                if (engine.SupportsMultishotAccept &&
                    Interlocked.CompareExchange(
                        ref context._multishotAcceptState,
                        MultishotAcceptStateArming,
                        MultishotAcceptStateDisarmed) == MultishotAcceptStateDisarmed)
                {
                    context.EnsureMultishotAcceptQueueInitialized();
                    IoUringDirectPrepareResult multishotPrepareResult = engine.TryPrepareIoUringDirectMultishotAccept(
                        context._socket,
                        rawSocketAddress,
                        SocketAddress.Length,
                        out userData,
                        out SocketError multishotErrorCode);
                    if (multishotPrepareResult == IoUringDirectPrepareResult.Prepared)
                    {
                        Debug.Assert(
                            (byte)(userData >> IoUringUserDataTagShift) == IoUringReservedCompletionTag,
                            "Multishot accept user_data must be a reserved-completion token.");
                        Volatile.Write(ref context._multishotAcceptState, unchecked((long)userData));
                        context.TryCreateReusePortShadowListeners(engine);
                        ErrorCode = multishotErrorCode;
                        return multishotPrepareResult;
                    }

                    context.DisarmMultishotAccept();
                }

                IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectAccept(
                    context._socket,
                    rawSocketAddress,
                    SocketAddress.Length,
                    out userData,
                    out SocketError errorCode);
                ErrorCode = errorCode;
                return prepareResult;
            }

            internal bool ProcessIoUringCompletionSuccessAccept(int result, uint auxiliaryData)
            {
                AcceptedFileDescriptor = (IntPtr)result;
                ErrorCode = SocketError.Success;
                // Keep parity with readiness path: always honor reported address length, including 0.
                AcceptSocketAddressLength = auxiliaryData > (uint)SocketAddress.Length ? SocketAddress.Length : (int)auxiliaryData;
                SocketAddress = SocketAddress.Slice(0, AcceptSocketAddressLength);
                return true;
            }

            internal bool ProcessIoUringCompletionErrorAccept(int result)
            {
                AcceptedFileDescriptor = (IntPtr)(-1);
                return ProcessIoUringCompletionErrorRead(result);
            }
        }

        private sealed partial class ConnectOperation
        {
            /// <inheritdoc />
            internal override Interop.Sys.SocketEvents GetIoUringFallbackSocketEvents() =>
                Interop.Sys.SocketEvents.Write;

            /// <inheritdoc />
            protected override unsafe IoUringDirectPrepareResult IoUringPrepareDirect(
                SocketAsyncContext context,
                SocketAsyncEngine engine,
                out ulong userData)
            {
                userData = 0;
                if (!TryPinIoUringBuffer(SocketAddress, out byte* rawSocketAddress))
                {
                    return IoUringDirectPrepareResult.PrepareFailed;
                }

                IoUringDirectPrepareResult prepareResult = engine.TryPrepareIoUringDirectConnect(
                    context._socket,
                    rawSocketAddress,
                    SocketAddress.Length,
                    out userData,
                    out SocketError errorCode);
                ErrorCode = errorCode;
                return prepareResult;
            }

            internal bool ProcessIoUringCompletionErrorConnect(SocketAsyncContext context, int result)
            {
                Interop.Error error = GetIoUringPalError(result);
                if (error == Interop.Error.EINPROGRESS)
                {
                    ErrorCode = SocketError.Success;
                    return false;
                }

                if (!ProcessIoUringCompletionErrorWrite(result))
                {
                    return false;
                }

                context._socket.RegisterConnectResult(ErrorCode);
                return true;
            }

            internal bool ProcessIoUringCompletionSuccessConnect(SocketAsyncContext context)
            {
                ErrorCode = SocketError.Success;
                context._socket.RegisterConnectResult(ErrorCode);

                if (Buffer.Length > 0)
                {
                    Action<int, Memory<byte>, SocketFlags, SocketError>? callback = Callback;
                    Debug.Assert(callback is not null);
                    SocketError error = context.SendToAsync(Buffer, 0, Buffer.Length, SocketFlags.None, default, ref BytesTransferred, callback!, default);
                    if (error == SocketError.IOPending)
                    {
                        // Callback ownership moved to the async send operation.
                        Callback = null;
                        Buffer = default;
                    }
                    else
                    {
                        if (error != SocketError.Success)
                        {
                            ErrorCode = error;
                            context._socket.RegisterConnectResult(ErrorCode);
                        }

                        // Follow-up send completed synchronously (success/error), so invoke
                        // Connect callback from this operation path.
                        Buffer = default;
                    }
                }

                return true;
            }
        }
    }
}
