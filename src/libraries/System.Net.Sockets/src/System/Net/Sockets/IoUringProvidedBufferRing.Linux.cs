// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed partial class SocketAsyncEngine
    {
        private const string IoUringAdaptiveBufferSizingSwitchName = "System.Net.Sockets.IoUringAdaptiveBufferSizing";
        private const int IoUringProvidedBufferRingEntries = (int)IoUringConstants.QueueEntries;
        private const int IoUringProvidedBufferSizeDefault = 4096;
        private const ushort IoUringProvidedBufferGroupIdStart = 0x8000;
        private static readonly int s_ioUringProvidedBufferSize = GetConfiguredIoUringProvidedBufferSize();
        private static readonly bool s_ioUringAdaptiveBufferSizingEnabled = IsAdaptiveIoUringProvidedBufferSizingEnabled();
        private static readonly bool s_ioUringRegisterBuffersEnabled = IsIoUringRegisterBuffersEnabled();
        private bool _adaptiveBufferSizingEnabled;
        private ushort _nextIoUringProvidedBufferGroupId = IoUringProvidedBufferGroupIdStart;

        /// <summary>
        /// Initializes a provided-buffer ring and registers it with the kernel when supported.
        /// Failures are non-fatal and leave completion mode enabled without provided buffers.
        /// </summary>
        private void InitializeIoUringProvidedBufferRingIfSupported(int ringFd)
        {
            SetIoUringProvidedBufferCapabilityState(
                supportsProvidedBufferRings: false,
                hasRegisteredBuffers: false);
            _adaptiveBufferSizingEnabled = false;
            _ioUringProvidedBufferGroupId = 0;
            _ioUringProvidedBufferRing = null;
            ushort initialGroupId = AllocateProvidedBufferGroupId();

            if (!IoUringProvidedBufferRing.TryCreate(
                    initialGroupId,
                    IoUringProvidedBufferRingEntries,
                    s_ioUringProvidedBufferSize,
                    s_ioUringAdaptiveBufferSizingEnabled,
                    out IoUringProvidedBufferRing? bufferRing) ||
                bufferRing is null)
            {
                return;
            }

            Interop.Error registerError = bufferRing.Register(ringFd);
            if (registerError != Interop.Error.SUCCESS)
            {
                bufferRing.Dispose();
                return;
            }

            _ioUringProvidedBufferRing = bufferRing;
            _ioUringProvidedBufferGroupId = bufferRing.BufferGroupId;
            _adaptiveBufferSizingEnabled = s_ioUringAdaptiveBufferSizingEnabled;
            SetIoUringProvidedBufferCapabilityState(
                supportsProvidedBufferRings: true,
                hasRegisteredBuffers: TryRegisterProvidedBuffersWithTelemetry(bufferRing, ringFd));

        }

        /// <summary>
        /// Evaluates adaptive buffer-sizing recommendations and hot-swaps the provided-buffer ring when safe.
        /// Must run on the event-loop thread.
        /// </summary>
        private void EvaluateProvidedBufferRingResize()
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "Provided-buffer resize evaluation must run on the io_uring event-loop thread.");
            if (!_adaptiveBufferSizingEnabled || _ringState.RingFd < 0)
            {
                return;
            }

            IoUringProvidedBufferRing? currentRing = _ioUringProvidedBufferRing;
            if (currentRing is null)
            {
                return;
            }

            int currentBufferSize = currentRing.BufferSize;
            int recommendedBufferSize = currentRing.RecommendedBufferSize;
            if (recommendedBufferSize == 0 || recommendedBufferSize == currentBufferSize)
            {
                return;
            }

            if (!IsProvidedBufferResizeQuiescent(currentRing))
            {
                return;
            }

            ushort newGroupId = AllocateProvidedBufferGroupId(_ioUringProvidedBufferGroupId);
            if (!IoUringProvidedBufferRing.TryCreate(
                    newGroupId,
                    IoUringProvidedBufferRingEntries,
                    recommendedBufferSize,
                    adaptiveSizingEnabled: true,
                    out IoUringProvidedBufferRing? replacementRing) ||
                replacementRing is null)
            {
                return;
            }

            AssertProvidedBufferResizeQuiescent(currentRing);

            bool restorePreviousBufferRegistration = _ioUringCapabilities.HasRegisteredBuffers;
            TryUnregisterProvidedBuffersIfRegistered(currentRing, _ringState.RingFd, restorePreviousBufferRegistration);

            if (replacementRing.Register(_ringState.RingFd) != Interop.Error.SUCCESS)
            {
                replacementRing.Dispose();
                if (restorePreviousBufferRegistration)
                {
                    SetIoUringProvidedBufferCapabilityState(
                        supportsProvidedBufferRings: true,
                        hasRegisteredBuffers: TryRegisterProvidedBuffersWithTelemetry(
                            currentRing,
                            _ringState.RingFd));
                }

                return;
            }

            currentRing.Unregister(_ringState.RingFd);
            currentRing.Dispose();

            _ioUringProvidedBufferRing = replacementRing;
            _ioUringProvidedBufferGroupId = replacementRing.BufferGroupId;
            RefreshIoUringMultishotRecvSupport();
            SetIoUringProvidedBufferCapabilityState(
                supportsProvidedBufferRings: true,
                hasRegisteredBuffers: TryRegisterProvidedBuffersWithTelemetry(
                    replacementRing,
                    _ringState.RingFd));

        }

        private bool IsProvidedBufferResizeQuiescent(IoUringProvidedBufferRing currentRing)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "Provided-buffer resize quiescence must be evaluated on the io_uring event-loop thread.");

            if (currentRing.InUseCount != 0)
            {
                return false;
            }

            if (_cqOverflowRecoveryActive)
            {
                return false;
            }

            // Ring swap frees/replaces native buffer-ring memory. Delay swap until all tracked
            // io_uring operations have drained so no in-flight SQE can still reference the old ring.
            return Volatile.Read(ref _trackedIoUringOperationCount) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort AllocateProvidedBufferGroupId(ushort avoidGroupId = 0)
        {
            ushort candidate = _nextIoUringProvidedBufferGroupId;
            for (int attempts = 0; attempts < ushort.MaxValue; attempts++)
            {
                if (candidate != 0 &&
                    candidate != ushort.MaxValue &&
                    candidate != avoidGroupId)
                {
                    _nextIoUringProvidedBufferGroupId = GetNextProvidedBufferGroupId(candidate);
                    return candidate;
                }

                candidate = GetNextProvidedBufferGroupId(candidate);
            }

            Debug.Fail("Unable to allocate an io_uring provided-buffer group id.");
            _nextIoUringProvidedBufferGroupId = IoUringProvidedBufferGroupIdStart;
            return IoUringProvidedBufferGroupIdStart;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort GetNextProvidedBufferGroupId(ushort currentGroupId)
        {
            ushort nextGroupId = unchecked((ushort)(currentGroupId + 1));
            if (nextGroupId < IoUringProvidedBufferGroupIdStart || nextGroupId == ushort.MaxValue)
            {
                nextGroupId = IoUringProvidedBufferGroupIdStart;
            }

            return nextGroupId;
        }

        [Conditional("DEBUG")]
        private void AssertProvidedBufferResizeQuiescent(IoUringProvidedBufferRing currentRing)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "Provided-buffer resize assertions must run on the io_uring event-loop thread.");
            Debug.Assert(
                currentRing.InUseCount == 0,
                "Provided-buffer resize requires no checked-out buffers before ring swap.");
            Debug.Assert(
                !_cqOverflowRecoveryActive,
                "Provided-buffer resize must not run during CQ overflow recovery.");
            Debug.Assert(
                Volatile.Read(ref _trackedIoUringOperationCount) == 0,
                "Provided-buffer resize requires no tracked io_uring operations before old ring disposal.");
        }

        private static int GetConfiguredIoUringProvidedBufferSize()
        {
#if DEBUG
            string? configuredValue = Environment.GetEnvironmentVariable(
                IoUringTestEnvironmentVariables.ProvidedBufferSize);

            if (!string.IsNullOrWhiteSpace(configuredValue))
            {
                return int.TryParse(configuredValue, out int parsedSize) && parsedSize > 0
                    ? parsedSize
                    : IoUringProvidedBufferSizeDefault;
            }
#endif

            return IoUringProvidedBufferSizeDefault;
        }

        private static bool IsAdaptiveIoUringProvidedBufferSizingEnabled()
        {
            bool enabled = AppContext.TryGetSwitch(IoUringAdaptiveBufferSizingSwitchName, out bool configured) && configured;
#if DEBUG
            bool? parsed = TryParseBoolSwitch(
                Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.AdaptiveBufferSizing));
            if (parsed.HasValue) return parsed.Value;
#endif
            return enabled;
        }

        private static bool IsIoUringRegisterBuffersEnabled()
        {
#if DEBUG
            bool? parsed = TryParseBoolSwitch(
                Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.RegisterBuffers));
            if (parsed.HasValue) return parsed.Value;
#endif
            return true;
        }

        private static bool TryRegisterProvidedBuffersWithTelemetry(
            IoUringProvidedBufferRing bufferRing,
            int ringFd)
        {
            if (!s_ioUringRegisterBuffersEnabled || ringFd < 0)
            {
                return false;
            }

            // REGISTER_BUFFERS is orthogonal to provided-buffer selection (RECV + IOSQE_BUFFER_SELECT).
            // Any performance benefit for this path is kernel-dependent and must be validated empirically.
            return bufferRing.TryRegisterBuffersWithKernel(ringFd);
        }

        private void TryUnregisterProvidedBuffersIfRegistered(
            IoUringProvidedBufferRing bufferRing,
            int ringFd,
            bool hasRegisteredBuffers)
        {
            if (!hasRegisteredBuffers || ringFd < 0)
            {
                return;
            }

            bufferRing.TryUnregisterBuffersFromKernel(ringFd);
            SetIoUringProvidedBufferCapabilityState(
                supportsProvidedBufferRings: _ioUringCapabilities.SupportsProvidedBufferRings,
                hasRegisteredBuffers: false);
        }

        /// <summary>Unregisters and disposes the provided-buffer ring.</summary>
        private void FreeIoUringProvidedBufferRing()
        {
            IoUringProvidedBufferRing? bufferRing = _ioUringProvidedBufferRing;
            bool hadRegisteredBuffers = _ioUringCapabilities.HasRegisteredBuffers;
            _ioUringProvidedBufferRing = null;
            // Teardown invariant: clear provided-buffer capabilities immediately so no
            // subsequent receive-prepare path can select provided/fixed-buffer strategies.
            SetIoUringProvidedBufferCapabilityState(
                supportsProvidedBufferRings: false,
                hasRegisteredBuffers: false);
            _adaptiveBufferSizingEnabled = false;
            _ioUringProvidedBufferGroupId = 0;

            if (bufferRing is null)
            {
                return;
            }

            bufferRing.RecycleCheckedOutBuffersForTeardown();

            // Unregister the IORING_REGISTER_BUFFERS iovec array (registered-buffer acceleration).
            TryUnregisterProvidedBuffersIfRegistered(bufferRing, _ringState.RingFd, hadRegisteredBuffers);

            // Unregister the IORING_REGISTER_PBUF_RING provided-buffer ring itself.
            if (_ringState.RingFd >= 0)
            {
                bufferRing.Unregister(_ringState.RingFd);
            }

            bufferRing.Dispose();
        }

        /// <summary>
        /// Owns a managed provided-buffer ring registration: native ring memory, pinned managed
        /// buffers, buffer-id lifecycle, and recycle counters.
        /// Lifetime is process-engine managed and deterministic via <see cref="Dispose"/>; no finalizer is used.
        /// </summary>
        private sealed unsafe class IoUringProvidedBufferRing : IDisposable
        {
            private const int AdaptiveWindowCompletionCount = 256;
            private const int AdaptiveMinBufferSize = 128;
            private const int AdaptiveMaxBufferSize = 65536;
            private const int PreparedReceiveMinimumReserve = 8;
            private const int PreparedReceiveMaximumReserve = 64;
            private const byte BufferStatePosted = 1;
            private const byte BufferStateCheckedOut = 2;
#if DEBUG
            private static int s_testForceCreateOomOnce = -1;
#endif

            private readonly ushort _bufferGroupId;
            private readonly int _bufferSize;
            private readonly uint _ringEntries;
            private readonly uint _ringMask;
            private readonly bool _adaptiveSizingEnabled;
            private readonly byte[][] _buffers;
            private readonly nint[] _bufferAddresses;
            private readonly byte[] _bufferStates;
            private readonly ulong[] _postedBufferStateBits;
            private Interop.Sys.IoUringBuf* _ringBuffers;
            private Interop.Sys.IoUringBufRingHeader* _ringHeader;
            private readonly void* _ringMemory;
            private bool _registered;
            private bool _disposed;
            private int _availableCount;
            private int _inUseCount;
            private long _recycledCount;
            private long _allocationFailureCount;
            private long _totalCompletionBytes;
            private long _totalCompletionCount;
            private long _completionsAboveHighWatermark;
            private long _completionsBelowLowWatermark;
            private int _recommendedBufferSize;
            private uint _nextPreparedReceiveBufferHint;
            private uint _nextPreparedReceivePostedWordHint;
            private bool _deferTailPublish;
            private bool _deferredTailDirty;
            private ushort _deferredTailValue;
            private int _debugOwningThreadId;

            internal ushort BufferGroupId => _bufferGroupId;
            internal int BufferSize => _bufferSize;
            internal int AvailableCount => Volatile.Read(ref _availableCount);
            // Writers are single-threaded via AssertSingleThreadAccess; Volatile.Read keeps
            // diagnostics/resize sampling conservative when observed outside mutation sites.
            internal int InUseCount => Volatile.Read(ref _inUseCount);
            internal long RecycledCount => Volatile.Read(ref _recycledCount);
            internal long AllocationFailureCount => Volatile.Read(ref _allocationFailureCount);
            internal int RecommendedBufferSize => Volatile.Read(ref _recommendedBufferSize);
            internal int TotalBufferCountForTest => _bufferStates.Length;

            private IoUringProvidedBufferRing(ushort bufferGroupId, int ringEntries, int bufferSize, bool adaptiveSizingEnabled)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ringEntries);
                if (!BitOperations.IsPow2((uint)ringEntries) || ringEntries > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(ringEntries));
                }

                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

                _bufferGroupId = bufferGroupId;
                _bufferSize = bufferSize;
                _adaptiveSizingEnabled = adaptiveSizingEnabled;
                _ringEntries = (uint)ringEntries;
                _ringMask = (uint)ringEntries - 1;
                _availableCount = ringEntries;
                _recommendedBufferSize = bufferSize;
                _buffers = new byte[ringEntries][];
                _bufferAddresses = new nint[ringEntries];
                _bufferStates = GC.AllocateUninitializedArray<byte>(ringEntries);
                _postedBufferStateBits = new ulong[(ringEntries + 63) / 64];

                nuint ringByteCount = checked((nuint)ringEntries * (nuint)sizeof(Interop.Sys.IoUringBuf));
                _ringMemory = NativeMemory.AlignedAlloc(ringByteCount, (nuint)Environment.SystemPageSize);
                if (_ringMemory is null)
                {
                    throw new OutOfMemoryException();
                }

                NativeMemory.Clear(_ringMemory, ringByteCount);
                _ringBuffers = (Interop.Sys.IoUringBuf*)_ringMemory;
                _ringHeader = (Interop.Sys.IoUringBufRingHeader*)_ringMemory;

                int initializedCount = 0;
                try
                {
                    for (int i = 0; i < ringEntries; i++)
                    {
                        byte[] buffer = GC.AllocateUninitializedArray<byte>(bufferSize, pinned: true);
                        _buffers[i] = buffer;
                        _bufferAddresses[i] = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(buffer));
                        _bufferStates[i] = BufferStatePosted;
                        SetPostedBufferBit((ushort)i, isPosted: true);

                        WriteBufferDescriptor((uint)i, (ushort)i);
                        initializedCount++;
                    }

                    PublishTail((ushort)initializedCount);
                }
                catch
                {
                    _allocationFailureCount++;
                    Array.Clear(_buffers, 0, initializedCount);
                    Array.Clear(_bufferAddresses, 0, initializedCount);
                    NativeMemory.AlignedFree(_ringMemory);
                    throw;
                }
            }

#if DEBUG
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static bool TryConsumeForcedCreateOutOfMemoryForTest()
            {
                int configured = Volatile.Read(ref s_testForceCreateOomOnce);
                if (configured < 0)
                {
                    configured = string.Equals(
                        Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.ForceProvidedBufferRingOomOnce),
                        "1",
                        StringComparison.Ordinal) ? 1 : 0;
                    Volatile.Write(ref s_testForceCreateOomOnce, configured);
                }

                if (configured == 0)
                {
                    return false;
                }

                return Interlocked.Exchange(ref s_testForceCreateOomOnce, 0) != 0;
            }
#endif

            internal static bool TryCreate(
                ushort bufferGroupId,
                int ringEntries,
                int bufferSize,
                bool adaptiveSizingEnabled,
                out IoUringProvidedBufferRing? bufferRing)
            {
#if DEBUG
                if (TryConsumeForcedCreateOutOfMemoryForTest())
                {
                    bufferRing = null;
                    return false;
                }
#endif

                try
                {
                    bufferRing = new IoUringProvidedBufferRing(bufferGroupId, ringEntries, bufferSize, adaptiveSizingEnabled);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                catch (OutOfMemoryException)
                {
                }

                bufferRing = null;
                return false;
            }

            /// <summary>Records a completion's bytes-transferred for adaptive sizing decisions.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void RecordCompletionUtilization(int bytesTransferred)
            {
                AssertSingleThreadAccess();
                if (!_adaptiveSizingEnabled || bytesTransferred <= 0)
                {
                    return;
                }

                int clampedBytes = Math.Min(bytesTransferred, _bufferSize);
                _totalCompletionBytes += clampedBytes;
                long count = ++_totalCompletionCount;

                int highWatermark = (_bufferSize * 3) / 4;
                int lowWatermark = _bufferSize / 4;
                if (clampedBytes > highWatermark)
                {
                    _completionsAboveHighWatermark++;
                }
                else if (clampedBytes < lowWatermark)
                {
                    _completionsBelowLowWatermark++;
                }

                if ((count & (AdaptiveWindowCompletionCount - 1)) == 0)
                {
                    EvaluateAdaptiveResize();
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void EvaluateAdaptiveResize()
            {
                AssertSingleThreadAccess();
                if (!_adaptiveSizingEnabled)
                {
                    return;
                }

                long windowBytes = _totalCompletionBytes;
                long aboveHigh = _completionsAboveHighWatermark;
                long belowLow = _completionsBelowLowWatermark;
                _totalCompletionBytes = 0;
                _completionsAboveHighWatermark = 0;
                _completionsBelowLowWatermark = 0;

                int currentSize = _bufferSize;
                int recommendedSize = currentSize;
                if (aboveHigh > AdaptiveWindowCompletionCount / 2 ||
                    windowBytes > (long)AdaptiveWindowCompletionCount * ((long)currentSize * 3 / 4))
                {
                    recommendedSize = Math.Min(currentSize * 2, AdaptiveMaxBufferSize);
                }
                else if (belowLow > AdaptiveWindowCompletionCount / 2 ||
                         windowBytes < (long)AdaptiveWindowCompletionCount * ((long)currentSize / 4))
                {
                    recommendedSize = Math.Max(currentSize / 2, AdaptiveMinBufferSize);
                }

                Volatile.Write(ref _recommendedBufferSize, recommendedSize);
            }

            internal Interop.Error Register(int ringFd)
            {
                Debug.Assert(!_disposed);

                if (_registered)
                {
                    return Interop.Error.SUCCESS;
                }

                Interop.Sys.IoUringBufReg registration = default;
                registration.RingAddress = (ulong)(nuint)_ringMemory;
                registration.RingEntries = _ringEntries;
                registration.BufferGroupId = _bufferGroupId;

                int result;
                Interop.Error registerError = Interop.Sys.IoUringShimRegister(
                    ringFd,
                    IoUringConstants.RegisterPbufRing,
                    &registration,
                    1u,
                    &result);
                if (registerError == Interop.Error.SUCCESS)
                {
                    _registered = true;
                }

                return registerError;
            }

            internal Interop.Error Unregister(int ringFd)
            {
                if (!_registered)
                {
                    return Interop.Error.SUCCESS;
                }

                Interop.Sys.IoUringBufReg registration = default;
                registration.BufferGroupId = _bufferGroupId;
                int result;
                Interop.Error unregisterError = Interop.Sys.IoUringShimRegister(
                    ringFd,
                    IoUringConstants.UnregisterPbufRing,
                    &registration,
                    1u,
                    &result);
                if (unregisterError == Interop.Error.SUCCESS)
                {
                    _registered = false;
                }

                return unregisterError;
            }

            /// <summary>
            /// Attempts to register pinned buffer payload pages with the kernel via IORING_REGISTER_BUFFERS.
            /// Failure is non-fatal and callers should gracefully continue with unregistered buffers.
            /// This does not switch recv SQEs to fixed-buffer opcodes; provided-buffer recv stays on
            /// IORING_OP_RECV + IOSQE_BUFFER_SELECT.
            /// </summary>
            internal bool TryRegisterBuffersWithKernel(int ringFd)
            {
                if (_disposed || ringFd < 0 || _buffers.Length == 0)
                {
                    return false;
                }

                nuint allocationSize = checked((nuint)_buffers.Length * (nuint)sizeof(Interop.Sys.IOVector));
                Interop.Sys.IOVector* iovecArray;
                try
                {
                    iovecArray = (Interop.Sys.IOVector*)NativeMemory.Alloc(allocationSize);
                }
                catch (OutOfMemoryException)
                {
                    return false;
                }

                try
                {
                    for (int i = 0; i < _buffers.Length; i++)
                    {
                        nint bufferAddress = _bufferAddresses[i];
                        if (bufferAddress == 0)
                        {
                            return false;
                        }

                        iovecArray[i].Base = (byte*)bufferAddress;
                        iovecArray[i].Count = (UIntPtr)_bufferSize;
                    }

                    int result;
                    Interop.Error registerError = Interop.Sys.IoUringShimRegister(
                        ringFd,
                        IoUringConstants.RegisterBuffers,
                        iovecArray,
                        (uint)_buffers.Length,
                        &result);
                    return registerError == Interop.Error.SUCCESS;
                }
                finally
                {
                    NativeMemory.Free(iovecArray);
                }
            }

            /// <summary>Unregisters previously registered pinned buffers via IORING_UNREGISTER_BUFFERS.</summary>
            internal bool TryUnregisterBuffersFromKernel(int ringFd)
            {
                if (_disposed || ringFd < 0)
                {
                    return false;
                }

                int result;
                Interop.Error unregisterError = Interop.Sys.IoUringShimRegister(
                    ringFd,
                    IoUringConstants.UnregisterBuffers,
                    null,
                    0u,
                    &result);
                return unregisterError == Interop.Error.SUCCESS;
            }

            /// <summary>Acquires a kernel-selected buffer id for completion processing.</summary>
            internal bool TryAcquireBufferForCompletion(ushort bufferId, out byte* buffer, out int bufferLength)
            {
                AssertSingleThreadAccess();
                buffer = null;
                bufferLength = 0;

                if (bufferId >= _ringEntries)
                {
                    _allocationFailureCount++;
                    return false;
                }

                byte state = _bufferStates[bufferId];
                if (state != BufferStatePosted)
                {
                    Debug.Assert(
                        state == BufferStateCheckedOut,
                        $"Unexpected provided-buffer state during acquire: id={bufferId}, state={state}");
                    _allocationFailureCount++;
                    return false;
                }

                _bufferStates[bufferId] = BufferStateCheckedOut;
                SetPostedBufferBit(bufferId, isPosted: false);
                Debug.Assert(_availableCount > 0, "Provided-buffer available count underflow.");
                _availableCount--;
                _inUseCount++;

                nint bufferAddress = _bufferAddresses[bufferId];
                if (bufferAddress == 0)
                {
                    _bufferStates[bufferId] = BufferStatePosted;
                    SetPostedBufferBit(bufferId, isPosted: true);
                    _availableCount++;
                    _inUseCount--;
                    _allocationFailureCount++;
                    return false;
                }

                buffer = (byte*)bufferAddress;
                bufferLength = _bufferSize;
                return true;
            }

            /// <summary>
            /// Acquires any currently posted provided buffer for fixed-recv submission.
            /// The acquired buffer remains checked out until completion recycles it.
            /// </summary>
            internal bool TryAcquireBufferForPreparedReceive(out ushort bufferId, out byte* buffer, out int bufferLength)
            {
                AssertSingleThreadAccess();
                bufferId = 0;
                buffer = null;
                bufferLength = 0;

                // Keep a reserve for kernel-selected (IOSQE_BUFFER_SELECT) receive completions so
                // fixed-recv one-shots don't deplete the provided-buffer pool under sustained load.
                int reserveCount = GetPreparedReceiveReserveCount();
                if (Volatile.Read(ref _availableCount) <= reserveCount)
                {
                    return false;
                }

                uint searchStart = _nextPreparedReceiveBufferHint;
                int maxAttempts = _postedBufferStateBits.Length + 1;
                for (int attempt = 0; attempt < maxAttempts && TryFindPostedBufferId(searchStart, out ushort candidateId); attempt++)
                {
                    if (TryAcquireBufferForCompletion(candidateId, out buffer, out bufferLength))
                    {
                        bufferId = candidateId;
                        uint nextSearchStart = ((uint)candidateId + 1) & _ringMask;
                        _nextPreparedReceiveBufferHint = nextSearchStart;
                        _nextPreparedReceivePostedWordHint = nextSearchStart >> 6;
                        return true;
                    }

                    searchStart = ((uint)candidateId + 1) & _ringMask;
                    _nextPreparedReceiveBufferHint = searchStart;
                    _nextPreparedReceivePostedWordHint = searchStart >> 6;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetPreparedReceiveReserveCount()
            {
                int ringEntryCount = (int)_ringEntries;
                int dynamicReserve = ringEntryCount / 16;
                return Math.Clamp(dynamicReserve, PreparedReceiveMinimumReserve, PreparedReceiveMaximumReserve);
            }

            /// <summary>Returns the pointer/length for a buffer that is already checked out.</summary>
            internal bool TryGetCheckedOutBuffer(ushort bufferId, out byte* buffer, out int bufferLength)
            {
                buffer = null;
                bufferLength = 0;

                if (bufferId >= _ringEntries || _bufferStates[bufferId] != BufferStateCheckedOut)
                {
                    return false;
                }

                nint bufferAddress = _bufferAddresses[bufferId];
                if (bufferAddress == 0)
                {
                    _allocationFailureCount++;
                    return false;
                }

                buffer = (byte*)bufferAddress;
                bufferLength = _bufferSize;
                return true;
            }

            /// <summary>Returns a previously acquired buffer id back to the provided-buffer ring.</summary>
            internal bool TryRecycleBufferFromCompletion(ushort bufferId)
            {
                AssertSingleThreadAccess();
                if (bufferId >= _ringEntries)
                {
                    return false;
                }

                byte state = _bufferStates[bufferId];
                if (state != BufferStateCheckedOut)
                {
                    Debug.Assert(
                        state == BufferStatePosted,
                        $"Unexpected provided-buffer state during recycle: id={bufferId}, state={state}");
                    return false;
                }

                RecycleCheckedOutBuffer(bufferId);
                return true;
            }

            /// <summary>
            /// Recycles any still-checked-out ids back into the ring during teardown.
            /// Returns the number of ids recycled.
            /// </summary>
            internal int RecycleCheckedOutBuffersForTeardown()
            {
                AssertSingleThreadAccess();
                int recycledCount = 0;
                for (ushort bufferId = 0; bufferId < _ringEntries; bufferId++)
                {
                    if (_bufferStates[bufferId] != BufferStateCheckedOut)
                    {
                        continue;
                    }

                    RecycleCheckedOutBuffer(bufferId);
                    recycledCount++;
                }

                return recycledCount;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void BeginDeferredRecyclePublish()
            {
                AssertSingleThreadAccess();
                if (_deferTailPublish)
                {
                    return;
                }

                _deferTailPublish = true;
                _deferredTailDirty = false;
                _deferredTailValue = ReadTail();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void EndDeferredRecyclePublish()
            {
                AssertSingleThreadAccess();
                if (!_deferTailPublish)
                {
                    return;
                }

                _deferTailPublish = false;
                if (_deferredTailDirty)
                {
                    PublishTail(_deferredTailValue);
                    _deferredTailDirty = false;
                }
            }

            /// <summary>
            /// Marks every provided buffer as checked out for deterministic test-only depletion setup.
            /// </summary>
            internal void ForceAllBuffersCheckedOutForTest()
            {
                AssertSingleThreadAccess();
                for (int i = 0; i < _bufferStates.Length; i++)
                {
                    _bufferStates[i] = BufferStateCheckedOut;
                }

                Array.Clear(_postedBufferStateBits);
                _nextPreparedReceivePostedWordHint = 0;
                Volatile.Write(ref _availableCount, 0);
                Volatile.Write(ref _inUseCount, _bufferStates.Length);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void RecycleCheckedOutBuffer(ushort bufferId)
            {
                ushort tail = _deferTailPublish ? _deferredTailValue : ReadTail();
                uint ringIndex = (uint)tail & _ringMask;
                WriteBufferDescriptor(ringIndex, bufferId);
                _bufferStates[bufferId] = BufferStatePosted;
                SetPostedBufferBit(bufferId, isPosted: true);
                _availableCount++;
                Debug.Assert(_inUseCount > 0, "Provided-buffer in-use count underflow.");
                _inUseCount--;
                ushort nextTail = unchecked((ushort)(tail + 1));
                if (_deferTailPublish)
                {
                    _deferredTailValue = nextTail;
                    _deferredTailDirty = true;
                }
                else
                {
                    PublishTail(nextTail);
                }
                _recycledCount++;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetPostedBufferBit(ushort bufferId, bool isPosted)
            {
                int wordIndex = bufferId >> 6;
                ulong bit = 1UL << (bufferId & 63);
                if (isPosted)
                {
                    bool wordWasEmpty = _postedBufferStateBits[wordIndex] == 0;
                    _postedBufferStateBits[wordIndex] |= bit;
                    if (wordWasEmpty)
                    {
                        _nextPreparedReceivePostedWordHint = (uint)wordIndex;
                    }
                }
                else
                {
                    _postedBufferStateBits[wordIndex] &= ~bit;
                }
            }

            private bool TryFindPostedBufferId(uint startIndex, out ushort bufferId)
            {
                int wordCount = _postedBufferStateBits.Length;
                if (wordCount == 0)
                {
                    bufferId = 0;
                    return false;
                }

                int hintWord = (int)(_nextPreparedReceivePostedWordHint % (uint)wordCount);
                if (TryFindBitInWord(hintWord, _postedBufferStateBits[hintWord], out bufferId))
                {
                    _nextPreparedReceivePostedWordHint = (uint)hintWord;
                    return true;
                }

                uint startWord = startIndex >> 6;
                int bitOffset = (int)(startIndex & 63);
                if (startWord >= (uint)wordCount)
                {
                    bufferId = 0;
                    return false;
                }

                if (TryFindBitInWord((int)startWord, _postedBufferStateBits[startWord] & (~0UL << bitOffset), out bufferId))
                {
                    _nextPreparedReceivePostedWordHint = startWord;
                    return true;
                }

                for (int word = (int)startWord + 1; word < wordCount; word++)
                {
                    if (TryFindBitInWord(word, _postedBufferStateBits[word], out bufferId))
                    {
                        _nextPreparedReceivePostedWordHint = (uint)word;
                        return true;
                    }
                }

                for (int word = 0; word < (int)startWord; word++)
                {
                    if (TryFindBitInWord(word, _postedBufferStateBits[word], out bufferId))
                    {
                        _nextPreparedReceivePostedWordHint = (uint)word;
                        return true;
                    }
                }

                bufferId = 0;
                return false;
            }

            private bool TryFindBitInWord(int wordIndex, ulong wordBits, out ushort bufferId)
            {
                while (wordBits != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(wordBits);
                    int candidate = (wordIndex << 6) + bitIndex;
                    if ((uint)candidate < _ringEntries)
                    {
                        bufferId = (ushort)candidate;
                        return true;
                    }

                    wordBits &= wordBits - 1;
                }

                bufferId = 0;
                return false;
            }

            [Conditional("DEBUG")]
            private void AssertSingleThreadAccess()
            {
                int currentThreadId = Environment.CurrentManagedThreadId;
                int ownerThreadId = Volatile.Read(ref _debugOwningThreadId);
                if (ownerThreadId == 0)
                {
                    int prior = Interlocked.CompareExchange(ref _debugOwningThreadId, currentThreadId, comparand: 0);
                    ownerThreadId = prior == 0 ? currentThreadId : prior;
                }

                Debug.Assert(
                    ownerThreadId == currentThreadId,
                    $"IoUringProvidedBufferRing mutable state must be accessed from one thread. Owner={ownerThreadId}, current={currentThreadId}");
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

#if DEBUG
                int checkedOutBufferCount = 0;
                for (int i = 0; i < _bufferStates.Length; i++)
                {
                    if (_bufferStates[i] == BufferStateCheckedOut)
                    {
                        checkedOutBufferCount++;
                    }
                }

                Debug.Assert(
                    checkedOutBufferCount == 0,
                    $"Disposing provided-buffer ring with outstanding checked-out buffers: {checkedOutBufferCount}");
#endif

                Debug.Assert(
                    !_registered,
                    "Provided-buffer ring must be unregistered before disposing native ring memory.");
                if (_registered)
                {
                    return;
                }

                _ringBuffers = null;
                _ringHeader = null;
                NativeMemory.AlignedFree(_ringMemory);
                _disposed = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ushort ReadTail() =>
                Volatile.Read(ref Unsafe.AsRef<ushort>(&_ringHeader->Tail));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void PublishTail(ushort tail) =>
                Volatile.Write(ref Unsafe.AsRef<ushort>(&_ringHeader->Tail), tail);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WriteBufferDescriptor(uint ringIndex, ushort bufferId)
            {
                Debug.Assert(ringIndex < _ringEntries);
                Debug.Assert(bufferId < _ringEntries);
                Debug.Assert(_bufferAddresses[bufferId] != 0);

                Interop.Sys.IoUringBuf* bufferSlot = _ringBuffers + ringIndex;
                bufferSlot->Address = (ulong)(nuint)_bufferAddresses[bufferId];
                bufferSlot->Length = (uint)_bufferSize;
                bufferSlot->BufferId = bufferId;
                // Do NOT write Reserved: at bufs[0] it overlays the ring tail field
                // in the kernel's io_uring_buf_ring union. Writing 0 would corrupt the
                // tail, causing the kernel to miscompute available buffer count.
            }
        }
    }
}
