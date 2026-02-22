// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    /// <summary>Linux socket engine coordinating epoll and io_uring work for process sockets.</summary>
    /// <remarks>
    /// io_uring completion mode currently uses one active engine/event-loop instance per process.
    /// This keeps ownership and teardown semantics simple, but the single submit/drain loop can
    /// become a bottleneck at high core/socket densities.
    /// Future work may evaluate multi-engine sharding (for example by socket affinity) when high-core
    /// throughput data justifies the additional complexity.
    /// </remarks>
    internal sealed unsafe partial class SocketAsyncEngine
    {
        /// <summary>Indicates which io_uring dispatch mode is active for this engine instance.</summary>
        private enum IoUringMode : byte
        {
            Disabled = 0,
            Completion = 1
        }

        /// <summary>Distinguishes cancellation requests issued during normal runtime from those during engine teardown.</summary>
        private enum IoUringCancellationOrigin : byte
        {
            Runtime = 0,
            Teardown = 1
        }

        /// <summary>Identifies which CQ-overflow recovery branch is active for logging/telemetry correlation.</summary>
        private enum IoUringCqOverflowRecoveryBranch : byte
        {
            MultishotAcceptArming = 0,
            Teardown = 1,
            // Steady-state branch: normal runtime overflow recovery outside teardown/accept-arm handoff.
            DualWave = 2
        }

        /// <summary>Tracks the lifecycle of an io_uring operation for debug assertions on valid state transitions.</summary>
        private enum IoUringOperationLifecycleState : byte
        {
            Queued = 0,
            Prepared = 1,
            Submitted = 2,
            Completed = 3,
            Canceled = 4,
            Detached = 5
        }

        /// <summary>Result of attempting to remove a tracked operation by user_data.</summary>
        private enum IoUringTrackedOperationRemoveResult : byte
        {
            Removed = 0,
            NotFound = 1,
            Mismatch = 2
        }

        /// <summary>Immutable snapshot of negotiated io_uring capabilities for this engine instance.</summary>
        private readonly struct LinuxIoUringCapabilities
        {
            private const uint FlagIsIoUringPort = 1u << 0;
            private const uint FlagSupportsMultishotRecv = 1u << 1;
            private const uint FlagSupportsMultishotAccept = 1u << 2;
            private const uint FlagSupportsZeroCopySend = 1u << 3;
            private const uint FlagSqPollEnabled = 1u << 4;
            private const uint FlagSupportsProvidedBufferRings = 1u << 5;
            private const uint FlagHasRegisteredBuffers = 1u << 6;

            private readonly uint _flags;

            /// <summary>The active io_uring dispatch mode.</summary>
            internal IoUringMode Mode { get; }

            /// <summary>Whether the engine's port was created as an io_uring instance.</summary>
            internal bool IsIoUringPort => (_flags & FlagIsIoUringPort) != 0;
            /// <summary>Whether multishot recv can be used by this engine instance.</summary>
            internal bool SupportsMultishotRecv => (_flags & FlagSupportsMultishotRecv) != 0;
            /// <summary>Whether multishot accept can be used by this engine instance.</summary>
            internal bool SupportsMultishotAccept => (_flags & FlagSupportsMultishotAccept) != 0;
            /// <summary>Whether zero-copy send is enabled for this engine instance.</summary>
            internal bool SupportsZeroCopySend => (_flags & FlagSupportsZeroCopySend) != 0;
            /// <summary>Whether SQPOLL mode is enabled for this engine instance.</summary>
            internal bool SqPollEnabled => (_flags & FlagSqPollEnabled) != 0;
            /// <summary>Whether provided-buffer rings are active for this engine instance.</summary>
            internal bool SupportsProvidedBufferRings => (_flags & FlagSupportsProvidedBufferRings) != 0;
            /// <summary>Whether provided buffers are currently registered with the kernel.</summary>
            internal bool HasRegisteredBuffers => (_flags & FlagHasRegisteredBuffers) != 0;

            /// <summary>Whether the engine is operating in full completion mode.</summary>
            internal bool IsCompletionMode =>
                Mode == IoUringMode.Completion;

            private LinuxIoUringCapabilities(IoUringMode mode, uint flags)
            {
                Mode = mode;
                _flags = flags;
            }

            internal LinuxIoUringCapabilities WithMode(IoUringMode mode) =>
                new LinuxIoUringCapabilities(mode, _flags);

            internal LinuxIoUringCapabilities WithIsIoUringPort(bool value) =>
                WithFlag(FlagIsIoUringPort, value);

            internal LinuxIoUringCapabilities WithSupportsMultishotRecv(bool value) =>
                WithFlag(FlagSupportsMultishotRecv, value);

            internal LinuxIoUringCapabilities WithSupportsMultishotAccept(bool value) =>
                WithFlag(FlagSupportsMultishotAccept, value);

            internal LinuxIoUringCapabilities WithSupportsZeroCopySend(bool value) =>
                WithFlag(FlagSupportsZeroCopySend, value);

            internal LinuxIoUringCapabilities WithSqPollEnabled(bool value) =>
                WithFlag(FlagSqPollEnabled, value);

            internal LinuxIoUringCapabilities WithSupportsProvidedBufferRings(bool value) =>
                WithFlag(FlagSupportsProvidedBufferRings, value);

            internal LinuxIoUringCapabilities WithHasRegisteredBuffers(bool value) =>
                WithFlag(FlagHasRegisteredBuffers, value);

            private LinuxIoUringCapabilities WithFlag(uint flag, bool value)
            {
                uint flags = value ? (_flags | flag) : (_flags & ~flag);
                return new LinuxIoUringCapabilities(Mode, flags);
            }
        }

        [Flags]
        private enum IoUringConfigurationWarningFlags : byte
        {
            None = 0,
            SqPollRequestedWithoutIoUring = 1 << 0,
            DirectSqeDisabledWithoutIoUring = 1 << 1,
            ZeroCopyOptInWithoutIoUring = 1 << 2
        }

        /// <summary>Immutable process-wide snapshot of resolved io_uring configuration inputs.</summary>
        private readonly struct IoUringResolvedConfiguration
        {
            internal bool IoUringEnabled { get; }
            internal bool SqPollRequested { get; }
            internal bool DirectSqeDisabled { get; }
            internal bool ZeroCopySendOptedIn { get; }
            internal bool RegisterBuffersEnabled { get; }
            internal bool AdaptiveProvidedBufferSizingEnabled { get; }
            internal int ProvidedBufferSize { get; }
            internal int PrepareQueueCapacity { get; }
            internal int CancellationQueueCapacity { get; }
            private readonly IoUringConfigurationWarningFlags _warningFlags;

            internal IoUringResolvedConfiguration(
                bool ioUringEnabled,
                bool sqPollRequested,
                bool directSqeDisabled,
                bool zeroCopySendOptedIn,
                bool registerBuffersEnabled,
                bool adaptiveProvidedBufferSizingEnabled,
                int providedBufferSize,
                int prepareQueueCapacity,
                int cancellationQueueCapacity)
            {
                IoUringEnabled = ioUringEnabled;
                SqPollRequested = sqPollRequested;
                DirectSqeDisabled = directSqeDisabled;
                ZeroCopySendOptedIn = zeroCopySendOptedIn;
                RegisterBuffersEnabled = registerBuffersEnabled;
                AdaptiveProvidedBufferSizingEnabled = adaptiveProvidedBufferSizingEnabled;
                ProvidedBufferSize = providedBufferSize;
                PrepareQueueCapacity = prepareQueueCapacity;
                CancellationQueueCapacity = cancellationQueueCapacity;
                _warningFlags = ComputeWarningFlags(
                    ioUringEnabled,
                    sqPollRequested,
                    directSqeDisabled,
                    zeroCopySendOptedIn);
            }

            internal string ToLogString() =>
                $"enabled={IoUringEnabled}, sqpollRequested={SqPollRequested}, directSqeDisabled={DirectSqeDisabled}, zeroCopySendOptedIn={ZeroCopySendOptedIn}, registerBuffersEnabled={RegisterBuffersEnabled}, adaptiveProvidedBufferSizingEnabled={AdaptiveProvidedBufferSizingEnabled}, providedBufferSize={ProvidedBufferSize}, prepareQueueCapacity={PrepareQueueCapacity}, cancellationQueueCapacity={CancellationQueueCapacity}";

            internal bool TryGetValidationWarnings([NotNullWhen(true)] out string? warnings)
            {
                if (_warningFlags == IoUringConfigurationWarningFlags.None)
                {
                    warnings = null;
                    return false;
                }

                warnings = BuildWarningMessage(_warningFlags);
                return true;
            }

            private static IoUringConfigurationWarningFlags ComputeWarningFlags(
                bool ioUringEnabled,
                bool sqPollRequested,
                bool directSqeDisabled,
                bool zeroCopySendOptedIn)
            {
                IoUringConfigurationWarningFlags warnings = IoUringConfigurationWarningFlags.None;
                if (!ioUringEnabled && sqPollRequested)
                {
                    warnings |= IoUringConfigurationWarningFlags.SqPollRequestedWithoutIoUring;
                }

                if (!ioUringEnabled && directSqeDisabled)
                {
                    warnings |= IoUringConfigurationWarningFlags.DirectSqeDisabledWithoutIoUring;
                }

                if (!ioUringEnabled && zeroCopySendOptedIn)
                {
                    warnings |= IoUringConfigurationWarningFlags.ZeroCopyOptInWithoutIoUring;
                }

                return warnings;
            }

            private static string BuildWarningMessage(IoUringConfigurationWarningFlags warnings)
            {
                var parts = new List<string>(3);
                if ((warnings & IoUringConfigurationWarningFlags.SqPollRequestedWithoutIoUring) != 0)
                {
                    parts.Add("SQPOLL requested while io_uring is disabled");
                }

                if ((warnings & IoUringConfigurationWarningFlags.DirectSqeDisabledWithoutIoUring) != 0)
                {
                    parts.Add("direct SQE disabled while io_uring is disabled");
                }

                if ((warnings & IoUringConfigurationWarningFlags.ZeroCopyOptInWithoutIoUring) != 0)
                {
                    parts.Add("zero-copy send opted-in while io_uring is disabled");
                }

                return string.Join("; ", parts);
            }
        }

        /// <summary>Mirrors kernel <c>struct io_uring_sqe</c> (64 bytes), written to the SQ ring for submission.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 64)]
        internal struct IoUringSqe
        {
            [FieldOffset(0)]
            internal byte Opcode;
            [FieldOffset(1)]
            internal byte Flags;
            [FieldOffset(2)]
            internal ushort Ioprio;
            [FieldOffset(4)]
            internal int Fd;
            [FieldOffset(8)]
            internal ulong Off;
            [FieldOffset(16)]
            internal ulong Addr;
            [FieldOffset(24)]
            internal uint Len;
            [FieldOffset(28)]
            internal uint RwFlags;
            [FieldOffset(32)]
            internal ulong UserData;
            [FieldOffset(40)]
            internal ushort BufIndex;
            [FieldOffset(42)]
            internal ushort Personality;
            [FieldOffset(44)]
            internal int SpliceFdIn;
            [FieldOffset(48)]
            internal ulong Addr3;
        }

        /// <summary>Mirrors kernel <c>struct io_uring_probe_op</c> (8 bytes per entry in the probe ops array).</summary>
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct IoUringProbeOp
        {
            [FieldOffset(0)] internal byte Op;
            [FieldOffset(1)] internal byte Resv;
            [FieldOffset(2)] internal ushort Flags;
            // 4 bytes reserved at offset 4
        }

        /// <summary>Mirrors kernel <c>struct io_uring_probe</c> (16-byte header preceding the variable-length ops array).</summary>
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct IoUringProbeHeader
        {
            [FieldOffset(0)] internal byte LastOp;
            [FieldOffset(1)] internal byte OpsLen;
            // 14 bytes reserved at offset 2
        }

        /// <summary>
        /// Kernel ABI opcode constants as a static class (not an enum) to avoid byte-cast noise
        /// at every SQE write site, since the SQE Opcode field is typed as byte.
        /// </summary>
        private static class IoUringOpcodes
        {
            internal const byte ReadFixed = 4;
            internal const byte Send = 26;
            internal const byte Recv = 27;
            internal const byte SendMsg = 9;
            internal const byte RecvMsg = 10;
            internal const byte Accept = 13;
            internal const byte Connect = 16;
            internal const byte SendZc = 53;
            internal const byte SendMsgZc = 54;
            internal const byte AsyncCancel = 14;
            internal const byte PollAdd = 6;
        }

        /// <summary>
        /// Centralizes io_uring ABI constants that mirror the native definitions in pal_io_uring.c.
        /// These are used by managed code that directly interacts with the io_uring submission
        /// and completion rings (e.g., direct SQE writes via mmap'd ring access).
        /// </summary>
        private static class IoUringConstants
        {
            // Setup flags (io_uring_setup params.flags)
            internal const uint SetupCqSize       = 1u << 3;
            internal const uint SetupSqPoll       = 1u << 5;
            internal const uint SetupSubmitAll    = 1u << 7;
            internal const uint SetupCoopTaskrun  = 1u << 8;
            internal const uint SetupSqe128       = 1u << 10;
            internal const uint SetupSingleIssuer = 1u << 12;
            internal const uint SetupDeferTaskrun = 1u << 13;
            internal const uint SetupRDisabled    = 1u << 6;
            internal const uint SetupNoSqArray    = 1u << 16;
            internal const uint SetupCloexec      = 1u << 19;

            // Feature flags (io_uring_params.features)
            internal const uint FeatureSingleMmap = 1u << 0;
            internal const uint FeatureExtArg = 1u << 8;

            // Enter flags (io_uring_enter flags parameter)
            internal const uint EnterGetevents      = 1u << 0;
            internal const uint EnterSqWakeup       = 1u << 1;
            internal const uint EnterExtArg         = 1u << 3;
            internal const uint EnterRegisteredRing = 1u << 4;

            // SQ ring flags (sq_ring->flags)
            internal const uint SqNeedWakeup = 1u << 0;

            // Register opcodes
            internal const uint RegisterEnableRings      = 17;
            internal const uint RegisterBuffers          = 0;
            internal const uint UnregisterBuffers        = 1;
            internal const uint RegisterProbe            = 8;
            internal const uint RegisterRingFds          = 20;
            internal const uint UnregisterRingFds        = 21;
            internal const uint RegisterPbufRing         = 22;
            internal const uint UnregisterPbufRing       = 23;

            // Register helper values
            internal const uint RegisterOffsetAuto = 0xFFFFFFFFU;

            // Probe op flags
            internal const uint ProbeOpFlagSupported = 1u << 0;

            // Poll flags
            internal const uint PollAddFlagMulti = 1u << 0;
            internal const uint PollIn = 0x0001;

            // CQE flags
            internal const uint CqeFBuffer = 1u << 0; // IORING_CQE_F_BUFFER (buffer id in upper bits)
            internal const uint CqeFMore = 1u << 1; // IORING_CQE_F_MORE (multishot)
            internal const uint CqeFSockNonEmpty = 1u << 2; // IORING_CQE_F_SOCK_NONEMPTY (more data pending after recv)
            internal const uint CqeFNotif = 1u << 3; // IORING_CQE_F_NOTIF (zero-copy notification)
            internal const int CqeBufferShift = 16; // IORING_CQE_BUFFER_SHIFT

            // Recv ioprio flags
            internal const ushort RecvMultishot = 1 << 1; // IORING_RECV_MULTISHOT
            // Accept ioprio flags
            internal const ushort AcceptMultishot = 1 << 0; // IORING_ACCEPT_MULTISHOT

            // SQE flags
            internal const byte SqeBufferSelect = 1 << 5; // IOSQE_BUFFER_SELECT

            // Sizing
            internal const uint QueueEntries = 1024;
            // Keep CQ capacity at 4x SQ entries to absorb completion bursts during short GC pauses
            // without immediately tripping overflow recovery on busy rings.
            internal const uint CqEntriesFactor = 4;
            internal const uint MaxCqeDrainBatch = 128;
            // Bounded wait trades wake latency for starvation resilience:
            // if an eventfd wake is missed or deferred, the event loop still polls at least once
            // every 50ms (worst-case deferred wake latency).
            internal const long BoundedWaitTimeoutNanos = 50L * 1000 * 1000; // 50ms
            // Circuit-breaker bounded wait used after repeated eventfd wake failures.
            internal const long WakeFailureFallbackWaitTimeoutNanos = 1L * 1000 * 1000; // 1ms

            // Completion operation pool sizing
            internal const int CompletionOperationPoolCapacityFactor = 2;

            // mmap offsets (from kernel UAPI: IORING_OFF_SQ_RING, IORING_OFF_CQ_RING, IORING_OFF_SQES)
            internal const ulong OffSqRing = 0;
            internal const ulong OffCqRing = 0x8000000;
            internal const ulong OffSqes   = 0x10000000;

            // Minimum kernel version for io_uring engine.
            // SEND_ZC deferred-completion logic relies on NOTIF CQE sequencing behavior stabilized in Linux 6.1.0.
            internal const int MinKernelMajor = 6;
            internal const int MinKernelMinor = 1;

            // Zero-copy send size threshold (payloads below this use regular send).
            internal const int ZeroCopySendThreshold = 16384; // 16KB

            // User data tag values (encoded in upper bits of user_data)
            internal const byte TagNone               = 0;
            internal const byte TagReservedCompletion = 2;
            internal const byte TagWakeupSignal       = 3;

            // Accept-time flags for accepted socket descriptors: SOCK_CLOEXEC | SOCK_NONBLOCK.
            internal const uint AcceptFlags = 0x80800;

            // Message inline capacities (avoid heap allocation on common small payloads)
            internal const int MessageInlineIovCount = 4;
            internal const int MessageInlineSocketAddressCapacity = 128; // sizeof(sockaddr_storage)
            internal const int MessageInlineControlBufferCapacity = 128;

            // Internal discriminator for io_uring vs epoll fallback detection
            internal const int NotSocketEventPort = int.MinValue + 1;

            // Completion slot encoding
            // Slot index is encoded into 16 bits of user_data payload => max 65536 slot IDs per engine.
            internal const int SlotIndexBits = 16;
            internal const ulong SlotIndexMask = (1UL << SlotIndexBits) - 1UL;
            internal const int GenerationBits = 56 - SlotIndexBits;
            // 40-bit generation space gives each slot ~1.1 trillion incarnations before wrap.
            // Generation zero remains reserved as "uninitialized", so wrap remaps 2^40-1 -> 1.
            internal const ulong GenerationMask = (1UL << GenerationBits) - 1UL;

            // Test hook opcode masks (mirrors IoUringTestOpcodeMask in pal_io_uring.c)
            internal const byte TestOpcodeMaskNone = 0;
            internal const byte TestOpcodeMaskSend = 1 << 0;
            internal const byte TestOpcodeMaskRecv = 1 << 1;
            internal const byte TestOpcodeMaskSendMsg = 1 << 2;
            internal const byte TestOpcodeMaskRecvMsg = 1 << 3;
            internal const byte TestOpcodeMaskAccept = 1 << 4;
            internal const byte TestOpcodeMaskConnect = 1 << 5;
            internal const byte TestOpcodeMaskSendZc = 1 << 6;
            internal const byte TestOpcodeMaskSendMsgZc = 1 << 7;
        }

        /// <summary>Captures the results of <c>io_uring_setup(2)</c> including ring fd, negotiated params, and feature flags.</summary>
        private struct IoUringSetupResult
        {
            internal int RingFd;
            internal Interop.Sys.IoUringParams Params;
            internal uint NegotiatedFlags;
            internal bool UsesExtArg;
            internal bool SqPollNegotiated;
        }

        /// <summary>Discriminates completion slot metadata shape for operation-specific post-completion processing.</summary>
        private enum IoUringCompletionOperationKind : byte
        {
            None = 0,
            Accept = 1,
            Message = 2,
            ReusePortAccept = 3,
        }

        /// <summary>
        /// Hot per-slot metadata used on every CQE dispatch.
        /// Keep this minimal; native pointer-heavy state is kept in <see cref="IoUringCompletionSlotStorage"/>.
        /// Explicit 24-byte layout keeps generation/free-list state and hot flags in one compact block.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct IoUringCompletionSlot
        {
            // 0..7
            [FieldOffset(0)]
            public ulong Generation;
            // 8..11 (-1 = end of free list)
            [FieldOffset(8)]
            public int FreeListNext;
            // 12..15 (operation kind + hot state flags)
            [FieldOffset(12)]
            private uint _packedState;
            // 16..17
            [FieldOffset(16)]
            public ushort FixedRecvBufferId;
#if DEBUG
            // 20..23 debug-only forced completion result payload.
            [FieldOffset(20)]
            public int TestForcedResult;
#endif

            private const uint KindMask = 0xFFu;
            private const uint FlagIsZeroCopySend = 1u << 8;
            private const uint FlagZeroCopyNotificationPending = 1u << 9;
            private const uint FlagUsesFixedRecvBuffer = 1u << 10;
#if DEBUG
            private const uint FlagHasTestForcedResult = 1u << 11;
#endif

            public IoUringCompletionOperationKind Kind
            {
                get => (IoUringCompletionOperationKind)(_packedState & KindMask);
                set => _packedState = (_packedState & ~KindMask) | ((uint)value & KindMask);
            }

            public bool IsZeroCopySend
            {
                get => (_packedState & FlagIsZeroCopySend) != 0;
                set => SetFlag(FlagIsZeroCopySend, value);
            }

            public bool ZeroCopyNotificationPending
            {
                get => (_packedState & FlagZeroCopyNotificationPending) != 0;
                set => SetFlag(FlagZeroCopyNotificationPending, value);
            }

            public bool UsesFixedRecvBuffer
            {
                get => (_packedState & FlagUsesFixedRecvBuffer) != 0;
                set => SetFlag(FlagUsesFixedRecvBuffer, value);
            }

#if DEBUG
            public bool HasTestForcedResult
            {
                get => (_packedState & FlagHasTestForcedResult) != 0;
                set => SetFlag(FlagHasTestForcedResult, value);
            }
#endif

            private void SetFlag(uint mask, bool value)
            {
                if (value)
                {
                    _packedState |= mask;
                }
                else
                {
                    _packedState &= ~mask;
                }
            }
        }

        /// <summary>
        /// Hot tracked-operation ownership state used on completion and cancellation paths.
        /// Kept separate from native slot storage to improve cache locality in CQE dispatch.
        /// </summary>
        private struct IoUringTrackedOperationState
        {
            public SocketAsyncContext.AsyncOperation? TrackedOperation;
            public ulong TrackedOperationGeneration;
        }

        /// <summary>
        /// Cold per-slot native metadata: pointers and message writeback state needed only for
        /// operation-specific completion processing.
        /// </summary>
        private struct IoUringCompletionSlotStorage
        {
            // Hold a DangerousAddRef lease for the socket fd until this slot is fully retired.
            public SafeSocketHandle? DangerousRefSocketHandle;
            // Per-slot pre-allocated native slab backing accept socklen_t and message inline storage.
            public unsafe byte* NativeInlineStorage;
            // Accept metadata
            public unsafe int* NativeSocketAddressLengthPtr; // socklen_t* in NativeInlineStorage
            // Message metadata (pointers to native-alloc'd msghdr/iovec)
            public IntPtr NativeMsgHdrPtr;
            public bool MessageIsReceive;
            // Message metadata - deep-copied native msghdr constituents (point into NativeInlineStorage).
            public unsafe Interop.Sys.IOVector* NativeIOVectors;
            public unsafe byte* NativeSocketAddress;
            public unsafe byte* NativeControlBuffer;
            // RecvMsg output capture - pointers back to managed MessageHeader buffers for writeback
            public unsafe byte* ReceiveOutputSocketAddress;
            public unsafe byte* ReceiveOutputControlBuffer;
            public int ReceiveSocketAddressCapacity;
            public int ReceiveControlBufferCapacity;
            // ReusePortAccept metadata - cross-engine references for shadow listener accept forwarding
            public SocketAsyncContext? ReusePortPrimaryContext;
            public SocketAsyncEngine? ReusePortPrimaryEngine;
        }

        /// <summary>
        /// Mirrors the kernel's <c>struct msghdr</c> layout for direct SQE submission.
        /// Used by <see cref="TryPrepareInlineMessageStorage"/> to build a native msghdr that
        /// io_uring sendmsg/recvmsg opcodes can consume directly.
        /// Must only be used on 64-bit Linux where sizeof(msghdr) == 56.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct NativeMsghdr
        {
            [FieldOffset(0)]
            public void* MsgName;
            [FieldOffset(8)]
            public uint MsgNameLen;
            [FieldOffset(16)]
            public Interop.Sys.IOVector* MsgIov;
            [FieldOffset(24)]
            public nuint MsgIovLen;
            [FieldOffset(32)]
            public void* MsgControl;
            [FieldOffset(40)]
            public nuint MsgControlLen;
            [FieldOffset(48)]
            public int MsgFlags;
        }

        /// <summary>
        /// Grouped managed ring mmap state.
        /// Keeping these fields in a single struct reduces top-level instance-field sprawl.
        /// </summary>
        private unsafe struct ManagedRingState
        {
            public Interop.Sys.IoUringCqe* CqeBase;
            public uint* CqTailPtr;
            public uint* CqHeadPtr;
            public uint CqMask;
            public uint CqEntries;
            public uint* CqOverflowPtr;
            public uint ObservedCqOverflow;
            public byte* SqRingPtr;
            public byte* CqRingPtr;
            public uint* SqFlagsPtr;
            public ulong SqRingSize;
            public ulong CqRingSize;
            public ulong SqesSize;
            public bool UsesSingleMmap;
            public int RingFd;
            public bool UsesExtArg;
            public bool UsesNoSqArray;
            public uint NegotiatedFlags;
            public uint CachedCqHead;
            public bool CqDrainEnabled;
            public int WakeupEventFd;

            public static ManagedRingState CreateDefault()
            {
                ManagedRingState state = default;
                state.RingFd = -1;
                state.WakeupEventFd = -1;
                return state;
            }
        }

        private const int IoUringDiagnosticsPollInterval = 64;
        private const int MinIoUringPrepareQueueDrainPerSubmit = 256;
        private const int MaxIoUringPrepareQueueDrainPerSubmit = 8192;
        private const int MinIoUringCancelQueueDrainPerSubmit = 256;
        private const int MaxIoUringCancelQueueDrainPerSubmit = 2048;
        private const int MaxIoUringSqeAcquireSubmitAttempts = 16;
        private const int CqOverflowTrackedSweepDelayMilliseconds = 250;
        private const int CqOverflowTrackedSweepMaxRearms = 8;
        private const int IoUringWakeFailureCircuitBreakerThreshold = 8;
        private const string IoUringEnvironmentVariable = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING";
        private const string IoUringSqPollEnvironmentVariable = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_SQPOLL";
        private const string IoUringDisableMultishotAcceptEnvironmentVariable = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_DISABLE_MULTISHOT_ACCEPT";
        private const string IoUringDisableReusePortAcceptEnvironmentVariable = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_DISABLE_REUSEPORT_ACCEPT";
        private const string UseIoUringAppContextSwitch = "System.Net.Sockets.UseIoUring";
        private const string UseIoUringSqPollAppContextSwitch = "System.Net.Sockets.UseIoUringSqPoll";
        // Configuration matrix (7 surfaces):
        // 1) DOTNET_SYSTEM_NET_SOCKETS_IO_URING
        // 2) AppContext: System.Net.Sockets.UseIoUring
        // 3) DOTNET_SYSTEM_NET_SOCKETS_IO_URING_SQPOLL
        // 4) AppContext: System.Net.Sockets.UseIoUringSqPoll
        // 5) DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_DIRECT_SQE (DEBUG)
        // 6) DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_ZERO_COPY_SEND (DEBUG)
        // 7) DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_REGISTER_BUFFERS (DEBUG, in IoUringProvidedBufferRing)
        //
        // Precedence (same pattern for both gates):
        // - env var overrides AppContext switch; AppContext is used only when env is unset.
        // All inputs are read once per process and cached in s_cachedConfigInputs.
        private static readonly object s_affinityGrowLock = new object();

        internal static void SetFdEngineAffinity(int fd, int engineIndex)
        {
            int[]? affinity = s_fdEngineAffinity;
            if (affinity is null) return;
            if ((uint)fd >= (uint)affinity.Length)
                affinity = GrowAffinityTable(fd);
            Volatile.Write(ref affinity[fd], engineIndex + 1);

            // If the table was concurrently grown after we captured 'affinity', mirror the write
            // into the current canonical table as well.
            int[]? current = s_fdEngineAffinity;
            if (!ReferenceEquals(current, affinity) && current is not null && (uint)fd < (uint)current.Length)
            {
                Volatile.Write(ref current[fd], engineIndex + 1);
            }
        }

        internal static void ClearFdEngineAffinity(int fd)
        {
            int[]? affinity = s_fdEngineAffinity;
            if (affinity is not null && (uint)fd < (uint)affinity.Length)
                Volatile.Write(ref affinity[fd], 0);
        }

        internal static void EnsureFdEngineAffinityTable()
        {
            if (s_fdEngineAffinity is null)
                Interlocked.CompareExchange(ref s_fdEngineAffinity, new int[4096], null);
        }

        private static int[] GrowAffinityTable(int fd)
        {
            lock (s_affinityGrowLock)
            {
                int[]? current = s_fdEngineAffinity;
                if (current is not null && fd < current.Length) return current;
                int newSize = Math.Max(fd + 1, (current?.Length ?? 0) * 2);
                int[] grown = new int[newSize];
                if (current is not null) Array.Copy(current, grown, current.Length);
                s_fdEngineAffinity = grown;
                return grown;
            }
        }

        private const ulong IoUringUserDataPayloadMask = 0x00FF_FFFF_FFFF_FFFFUL;
        private const int IoUringUserDataTagShift = 56;
        private static readonly int s_ioUringPrepareQueueCapacity = GetIoUringPrepareQueueCapacity();
        private static readonly int s_ioUringCancellationQueueCapacity = s_ioUringPrepareQueueCapacity;
        private int _ioUringResolvedConfigurationLogged;
        private long _ioUringPendingRetryQueuedToPrepareQueueCount;
        private long _ioUringNonPinnablePrepareFallbackCount;
        private long _ioUringPublishedNonPinnablePrepareFallbackCount;
        private MpscQueue<IoUringPrepareWorkItem>? _ioUringPrepareQueue;
        private MpscQueue<ulong>? _ioUringCancelQueue;
        private long _ioUringPrepareQueueLength;
        private long _ioUringCancelQueueLength;
        private long _ioUringPrepareQueueOverflowCount;
        private long _ioUringCancelQueueOverflowCount;
        private long _ioUringPrepareQueueOverflowFallbackCount;
        private long _ioUringCompletionSlotExhaustionCount;
        private long _ioUringCompletionSlotDrainRecoveryCount;
        private long _ioUringPublishedPrepareQueueLength;
        private long _ioUringBenignLateCompletionCount;
        private long _ioUringCompletionRequeueFailureCount;
        private long _ioUringUntrackMismatchCount;
        private long _ioUringPublishedPrepareQueueOverflowCount;
        private long _ioUringPublishedPrepareQueueOverflowFallbackCount;
        private long _ioUringPublishedCompletionRequeueFailureCount;
        private long _ioUringPublishedCompletionSlotExhaustionCount;
        private long _ioUringPublishedCompletionSlotDrainRecoveryCount;
        private int _ioUringDiagnosticsPollCountdown;
        private int _ioUringWakeFailureConsecutiveCount;
        private uint _ioUringWakeupGeneration;
        private int _ioUringPortClosedForTeardown;
        // Release-published teardown gate. Readers use Volatile.Read in enqueue/wakeup paths
        // to prevent new io_uring work from being published after teardown begins.
        private int _ioUringTeardownInitiated;
        private int _ioUringSlotCapacity;
        private bool _completionSlotDrainInProgress;
        private bool _cqOverflowRecoveryActive;
        private IoUringCqOverflowRecoveryBranch _cqOverflowRecoveryBranch;
        private long _cqOverflowTrackedSweepDeadlineTicks;
        private int _cqOverflowTrackedSweepRearmCount;
        private uint _ioUringManagedPendingSubmissions;
        private uint _ioUringManagedSqTail;
        private bool _ioUringManagedSqTailLoaded;
        private Interop.Sys.IoUringSqRingInfo _ioUringSqRingInfo;
        private bool _managedSqeInvariantsValidated;
        private bool _ioUringDirectSqeEnabled;
        private ManagedRingState _ringState = ManagedRingState.CreateDefault();

        // Per-opcode support flags, populated by ProbeIoUringOpcodeSupport.
        private bool _supportsOpSend;
        private bool _supportsOpReadFixed;
        private bool _supportsOpRecv;
        private bool _supportsOpSendMsg;
        private bool _supportsOpRecvMsg;
        private bool _supportsOpAccept;
        private bool _supportsOpConnect;
        private bool _supportsOpSendZc;
        private bool _supportsOpSendMsgZc;
        private bool _supportsOpAsyncCancel;
        private bool _supportsMultishotRecv;
        private bool _supportsMultishotAccept;
        private bool _zeroCopySendEnabled;

        private bool _sqPollEnabled;
        private bool _ioUringInitialized;
        private int _ioUringDrainTelemetryBatchActive;
        private long _ioUringDrainBatchProvidedBufferDepletionCount;
        private long _ioUringDrainBatchProvidedBufferRecycleCount;
        private long _ioUringDrainBatchPersistentMultishotRecvEarlyDataCount;
        private IoUringProvidedBufferRing? _ioUringProvidedBufferRing;
        private ushort _ioUringProvidedBufferGroupId;
        // SoA split: hot completion slot state and cold native storage/tracking metadata.
        private IoUringCompletionSlot[]? _completionSlots;
        private IoUringTrackedOperationState[]? _trackedOperations;
        private IoUringCompletionSlotStorage[]? _completionSlotStorage;
        private unsafe byte* _completionSlotNativeStorage;
        private nuint _completionSlotNativeStorageStride;
        private int _trackedIoUringOperationCount;
        private System.Buffers.MemoryHandle[]? _zeroCopyPinHolds;
        private int _completionSlotFreeListHead = -1;
        private int _completionSlotsInUse;
        private int _completionSlotsHighWaterMark;
        private int _liveAcceptCompletionSlotCount;

#if DEBUG
        // Test hook state: forced completion result injection (mirrors native pal_io_uring.c test hooks).
        private byte _testForceEagainOnceMask;
        private byte _testForceEcanceledOnceMask;
        private int _testForceSubmitEpermOnce;
        // Test-only observability for cancel-queue full retry path.
        private long _testCancelQueueWakeRetryCount;
#endif
        static partial void ResetDebugTestForcedResult(ref IoUringCompletionSlot slot);
        static partial void ResolveDebugTestForcedResult(ref IoUringCompletionSlot slot, ref int result);
        partial void ApplyDebugTestForcedResult(ref IoUringCompletionSlot slot, byte opcode);
        partial void RestoreDebugTestForcedResultIfNeeded(int slotIndex, byte opcode);
        partial void InitializeDebugTestHooksFromEnvironment();

        private LinuxIoUringCapabilities _ioUringCapabilities;

        // Managed ring state accessors (backed by _ringState).
        private unsafe Interop.Sys.IoUringCqe* _managedCqeBase
        {
            get => _ringState.CqeBase;
            set => _ringState.CqeBase = value;
        }

        private unsafe uint* _managedCqTailPtr
        {
            get => _ringState.CqTailPtr;
            set => _ringState.CqTailPtr = value;
        }

        private unsafe uint* _managedCqHeadPtr
        {
            get => _ringState.CqHeadPtr;
            set => _ringState.CqHeadPtr = value;
        }

        private uint _managedCqMask
        {
            get => _ringState.CqMask;
            set => _ringState.CqMask = value;
        }

        private uint _managedCqEntries
        {
            get => _ringState.CqEntries;
            set => _ringState.CqEntries = value;
        }

        private unsafe uint* _managedCqOverflowPtr
        {
            get => _ringState.CqOverflowPtr;
            set => _ringState.CqOverflowPtr = value;
        }

        private uint _managedObservedCqOverflow
        {
            get => _ringState.ObservedCqOverflow;
            set => _ringState.ObservedCqOverflow = value;
        }

        private unsafe byte* _managedSqRingPtr
        {
            get => _ringState.SqRingPtr;
            set => _ringState.SqRingPtr = value;
        }

        private unsafe byte* _managedCqRingPtr
        {
            get => _ringState.CqRingPtr;
            set => _ringState.CqRingPtr = value;
        }

        private unsafe uint* _managedSqFlagsPtr
        {
            get => _ringState.SqFlagsPtr;
            set => _ringState.SqFlagsPtr = value;
        }

        private ulong _managedSqRingSize
        {
            get => _ringState.SqRingSize;
            set => _ringState.SqRingSize = value;
        }

        private ulong _managedCqRingSize
        {
            get => _ringState.CqRingSize;
            set => _ringState.CqRingSize = value;
        }

        private ulong _managedSqesSize
        {
            get => _ringState.SqesSize;
            set => _ringState.SqesSize = value;
        }

        private bool _managedUsesSingleMmap
        {
            get => _ringState.UsesSingleMmap;
            set => _ringState.UsesSingleMmap = value;
        }

        private int _managedRingFd
        {
            get => _ringState.RingFd;
            set => _ringState.RingFd = value;
        }

        private bool _managedUsesExtArg
        {
            get => _ringState.UsesExtArg;
            set => _ringState.UsesExtArg = value;
        }

        private bool _managedUsesNoSqArray
        {
            get => _ringState.UsesNoSqArray;
            set => _ringState.UsesNoSqArray = value;
        }

        private uint _managedNegotiatedFlags
        {
            get => _ringState.NegotiatedFlags;
            set => _ringState.NegotiatedFlags = value;
        }

        private uint _managedCachedCqHead
        {
            get => _ringState.CachedCqHead;
            set => _ringState.CachedCqHead = value;
        }

        private bool _managedCqDrainEnabled
        {
            get => _ringState.CqDrainEnabled;
            set => _ringState.CqDrainEnabled = value;
        }

        private int _managedWakeupEventFd
        {
            get => _ringState.WakeupEventFd;
            set => _ringState.WakeupEventFd = value;
        }

        /// <summary>Whether this engine instance is using io_uring completion mode.</summary>
        internal bool IsIoUringCompletionModeEnabled => _ioUringCapabilities.IsCompletionMode;
        /// <summary>Whether managed direct SQE submission is enabled.</summary>
        internal bool IsIoUringDirectSqeEnabled => _ioUringDirectSqeEnabled;
        /// <summary>Whether a connected send payload is eligible for the SEND_ZC path.</summary>
        internal bool ShouldTryIoUringDirectSendZeroCopy(int payloadLength) =>
            IsIoUringZeroCopySendEligible(payloadLength, requiresSendMessageOpcode: false);
        /// <summary>Whether a message-based send payload is eligible for the SENDMSG_ZC path.</summary>
        internal bool ShouldTryIoUringDirectSendMessageZeroCopy(int payloadLength) =>
            IsIoUringZeroCopySendEligible(payloadLength, requiresSendMessageOpcode: true);

        /// <summary>
        /// Centralized zero-copy policy:
        /// 1) process-level opt-in, 2) opcode support, 3) payload threshold.
        /// The threshold is based on total payload bytes so buffer-list workloads (e.g. 4KB segments)
        /// are eligible once the aggregate payload crosses the cutoff.
        /// </summary>
        private bool IsIoUringZeroCopySendEligible(int payloadLength, bool requiresSendMessageOpcode)
        {
            if (!_zeroCopySendEnabled || payloadLength < IoUringConstants.ZeroCopySendThreshold)
            {
                return false;
            }

            return requiresSendMessageOpcode ? _supportsOpSendMsgZc : _supportsOpSendZc;
        }

        /// <summary>
        /// Reads the total count of pending completions that had to requeue through prepare queues
        /// after inline completion-mode re-prepare was not used.
        /// </summary>
        internal static long GetIoUringPendingRetryQueuedToPrepareQueueCount()
        {
            long total = 0;
            foreach (SocketAsyncEngine engine in s_engines)
            {
                total += Interlocked.Read(ref engine._ioUringPendingRetryQueuedToPrepareQueueCount);
            }

            return total;
        }

        internal static long GetIoUringNonPinnablePrepareFallbackCount()
        {
            long total = 0;
            foreach (SocketAsyncEngine engine in s_engines)
            {
                total += Interlocked.Read(ref engine._ioUringNonPinnablePrepareFallbackCount);
            }

            return total;
        }

        internal static void SetIoUringNonPinnablePrepareFallbackCountForTest(long value)
        {
#if DEBUG
            bool assigned = false;
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled)
                {
                    continue;
                }

                long engineValue = assigned ? 0 : value;
                Interlocked.Exchange(ref engine._ioUringNonPinnablePrepareFallbackCount, engineValue);
                Interlocked.Exchange(ref engine._ioUringPublishedNonPinnablePrepareFallbackCount, 0);
                assigned = true;
            }
#else
            _ = value;
#endif
        }

        private void LogIoUringResolvedConfigurationIfNeeded(in IoUringResolvedConfiguration resolvedConfiguration)
        {
            if (Interlocked.Exchange(ref _ioUringResolvedConfigurationLogged, 1) != 0)
            {
                return;
            }

            string configuration = resolvedConfiguration.ToLogString();
            SocketsTelemetry.Log.ReportIoUringResolvedConfiguration(configuration);
        }

        private static int GetIoUringPrepareQueueCapacity()
        {
#if DEBUG
            if (Environment.GetEnvironmentVariable(
                    IoUringTestEnvironmentVariables.PrepareQueueCapacity) is string configuredValue &&
                int.TryParse(configuredValue, out int configuredCapacity) &&
                configuredCapacity > 0)
            {
                return configuredCapacity;
            }
#endif

            // Raised default to reduce fallback frequency under bursty load.
            int scaledCapacity = s_eventBufferCount >= 32 ? checked(s_eventBufferCount * 4) : 512;
            return Math.Max(scaledCapacity, 512);
        }

        private static uint GetIoUringQueueEntries()
        {
#if DEBUG
            if (Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.QueueEntries) is string configuredValue &&
                int.TryParse(configuredValue, out int configuredEntries) &&
                configuredEntries >= 2 &&
                configuredEntries <= IoUringConstants.QueueEntries &&
                (configuredEntries & (configuredEntries - 1)) == 0)
            {
                return (uint)configuredEntries;
            }
#endif

            return IoUringConstants.QueueEntries;
        }

        /// <summary>Creates a capabilities snapshot based on whether the port is io_uring.</summary>
        private static LinuxIoUringCapabilities ResolveLinuxIoUringCapabilities(bool isIoUringPort) =>
            default(LinuxIoUringCapabilities)
                .WithIsIoUringPort(isIoUringPort)
                .WithMode(isIoUringPort ? IoUringMode.Completion : IoUringMode.Disabled);

        private void SetIoUringProvidedBufferCapabilityState(bool supportsProvidedBufferRings, bool hasRegisteredBuffers)
        {
            _ioUringCapabilities = _ioUringCapabilities
                .WithSupportsProvidedBufferRings(supportsProvidedBufferRings)
                .WithHasRegisteredBuffers(hasRegisteredBuffers);
        }

        /// <summary>Encodes a tag byte and payload into a 64-bit user_data value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong EncodeIoUringUserData(byte tag, ulong payload) =>
            ((ulong)tag << IoUringUserDataTagShift) | (payload & IoUringUserDataPayloadMask);

        /// <summary>Reads the next CQE from the completion ring without advancing the head.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool TryPeekNextCqe(out Interop.Sys.IoUringCqe* cqe, int eventLoopThreadId)
        {
            Debug.Assert(eventLoopThreadId == Environment.CurrentManagedThreadId,
                "TryPeekNextCqe must only be called from the event loop thread (SINGLE_ISSUER contract).");
            cqe = null;
            uint cqTail = Volatile.Read(ref *_managedCqTailPtr);
            if (_managedCachedCqHead == cqTail) return false;
            uint index = _managedCachedCqHead & _managedCqMask;
            cqe = _managedCqeBase + index;
            return true;
        }

        /// <summary>Advances the CQ head pointer by the given count, making slots available to the kernel.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void AdvanceCqHead(uint count, int eventLoopThreadId)
        {
            Debug.Assert(eventLoopThreadId == Environment.CurrentManagedThreadId,
                "AdvanceCqHead must only be called from the event loop thread (SINGLE_ISSUER contract).");
            _managedCachedCqHead += count;
            Volatile.Write(ref *_managedCqHeadPtr, _managedCachedCqHead);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginIoUringDrainTelemetryBatch()
        {
            _ioUringDrainBatchProvidedBufferDepletionCount = 0;
            _ioUringDrainBatchProvidedBufferRecycleCount = 0;
            _ioUringDrainBatchPersistentMultishotRecvEarlyDataCount = 0;
            Volatile.Write(ref _ioUringDrainTelemetryBatchActive, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushIoUringDrainTelemetryBatch()
        {
            Volatile.Write(ref _ioUringDrainTelemetryBatchActive, 0);

            long depletionCount = _ioUringDrainBatchProvidedBufferDepletionCount;
            if (depletionCount != 0)
            {
                SocketsTelemetry.Log.IoUringProvidedBufferDepletion(depletionCount);
            }

            long recycleCount = _ioUringDrainBatchProvidedBufferRecycleCount;
            if (recycleCount != 0)
            {
                SocketsTelemetry.Log.IoUringProvidedBufferRecycle(recycleCount);
            }

            long earlyDataCount = _ioUringDrainBatchPersistentMultishotRecvEarlyDataCount;
            if (earlyDataCount != 0)
            {
                SocketsTelemetry.Log.IoUringPersistentMultishotRecvEarlyData(earlyDataCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordIoUringProvidedBufferDepletionForDrainBatch(long count = 1)
        {
            if (Volatile.Read(ref _ioUringDrainTelemetryBatchActive) != 0)
            {
                _ioUringDrainBatchProvidedBufferDepletionCount += count;
                return;
            }

            SocketsTelemetry.Log.IoUringProvidedBufferDepletion(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordIoUringProvidedBufferRecycleForDrainBatch(long count = 1)
        {
            if (Volatile.Read(ref _ioUringDrainTelemetryBatchActive) != 0)
            {
                _ioUringDrainBatchProvidedBufferRecycleCount += count;
                return;
            }

            SocketsTelemetry.Log.IoUringProvidedBufferRecycle(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordIoUringPersistentMultishotRecvEarlyDataForDrainBatch(long count = 1)
        {
            if (Volatile.Read(ref _ioUringDrainTelemetryBatchActive) != 0)
            {
                _ioUringDrainBatchPersistentMultishotRecvEarlyDataCount += count;
                return;
            }

            SocketsTelemetry.Log.IoUringPersistentMultishotRecvEarlyData(count);
        }

        /// <summary>
        /// Drains up to <see cref="IoUringConstants.MaxCqeDrainBatch"/> CQEs from the mmap'd
        /// completion ring and dispatches each based on the user_data tag.
        /// Tag=2 (reserved completion) entries are dispatched directly through
        /// <see cref="SocketEventHandler.DispatchSingleIoUringCompletion"/>.
        /// Tag=3 (wakeup signal) entries are handled inline.
        /// Returns true when at least one CQE was drained.
        /// </summary>
        private unsafe bool DrainCqeRingBatch(SocketEventHandler handler)
        {
            int eventLoopThreadId = Volatile.Read(ref _eventLoopManagedThreadId);
            Debug.Assert(eventLoopThreadId == Environment.CurrentManagedThreadId,
                "DrainCqeRingBatch must only be called from the event loop thread (SINGLE_ISSUER contract).");
            ObserveManagedCqOverflowCounter();
            int drained = 0;
            bool drainedAnyCqe = false;
            bool enqueuedFallbackEvent = false;
            uint deferredCqHeadAdvance = 0;
            IoUringProvidedBufferRing? providedBufferRing = _ioUringProvidedBufferRing;
            providedBufferRing?.BeginDeferredRecyclePublish();
            BeginIoUringDrainTelemetryBatch();

            try
            {
                while (drained < (int)IoUringConstants.MaxCqeDrainBatch
                    && TryPeekNextCqe(out Interop.Sys.IoUringCqe* cqe, eventLoopThreadId))
                {
                    drainedAnyCqe = true;
                    ulong userData = cqe->UserData;
                    int result = cqe->Result;
                    uint flags = cqe->Flags;

                    if (_cqOverflowRecoveryActive)
                    {
                        // During overflow recovery, publish head movement per CQE so the kernel can
                        // reclaim CQ ring space immediately and avoid extending overflow pressure.
                        AdvanceCqHead(1, eventLoopThreadId);
                    }
                    else
                    {
                        _managedCachedCqHead++;
                        deferredCqHeadAdvance++;
                    }

                    byte tag = (byte)(userData >> IoUringUserDataTagShift);
                    ulong payload = userData & IoUringUserDataPayloadMask;

                    if (tag == IoUringConstants.TagReservedCompletion)
                    {
                        if ((flags & IoUringConstants.CqeFNotif) != 0)
                        {
                            if (HandleZeroCopyNotification(payload))
                            {
                                handler.DispatchZeroCopyIoUringNotification(payload);
                                // Free the slot AFTER dispatch has taken the tracked operation.
                                FreeCompletionSlot(DecodeCompletionSlotIndex(payload));
                                drained++;
                                continue;
                            }
                        }

                        bool isMultishotCompletion = false;
                        bool isReusePortAccept = false;
                        if ((flags & IoUringConstants.CqeFMore) != 0)
                        {
                            IoUringCompletionSlot[]? completionEntries = _completionSlots;
                            int slotIndex = DecodeCompletionSlotIndex(payload);
                            if (completionEntries is not null &&
                                (uint)slotIndex < (uint)completionEntries.Length)
                            {
                                ref IoUringCompletionSlot classSlot = ref completionEntries[slotIndex];
                                // SEND_ZC/SENDMSG_ZC result CQEs have CQE_F_MORE because a NOTIF
                                // CQE follows -- this is NOT a multishot indicator. Exclude ZC slots.
                                if (!classSlot.IsZeroCopySend)
                                {
                                    IoUringCompletionOperationKind kind = classSlot.Kind;
                                    isReusePortAccept = kind == IoUringCompletionOperationKind.ReusePortAccept;
                                    isMultishotCompletion = isReusePortAccept ||
                                        (kind == IoUringCompletionOperationKind.Message && _ioUringCapabilities.SupportsMultishotRecv) ||
                                        (kind == IoUringCompletionOperationKind.Accept && _ioUringCapabilities.SupportsMultishotAccept);
                                }
                            }
                        }
                        else
                        {
                            // Terminal CQE (no CQE_F_MORE). Check if this is a ReusePortAccept slot
                            // so we can route it for graceful cleanup without tracked-operation lookup.
                            IoUringCompletionSlot[]? completionEntries = _completionSlots;
                            int slotIndex = DecodeCompletionSlotIndex(payload);
                            if (completionEntries is not null &&
                                (uint)slotIndex < (uint)completionEntries.Length)
                            {
                                isReusePortAccept = completionEntries[slotIndex].Kind == IoUringCompletionOperationKind.ReusePortAccept;
                            }
                        }
                        ResolveReservedCompletionSlotMetadata(
                            payload,
                            isMultishotCompletion,
                            ref result,
                            out int completionSocketAddressLen,
                            out int completionControlBufferLen,
                            out uint completionAuxiliaryData,
                            out bool hasFixedRecvBuffer,
                            out ushort fixedRecvBufferId,
                            out bool shouldFreeSlot);

                        if (isReusePortAccept)
                        {
                            handler.DispatchReusePortAcceptIoUringCompletion(
                                userData,
                                result,
                                flags,
                                completionSocketAddressLen,
                                completionAuxiliaryData);
                            // Terminal CQE (no MORE flag): free the slot.
                            if (!isMultishotCompletion)
                            {
                                shouldFreeSlot = true;
                            }
                        }
                        else if (isMultishotCompletion)
                        {
                            // Dispatch expects full tagged user_data so tracked-ownership decode can validate tag+generation.
                            handler.DispatchMultishotIoUringCompletion(
                                userData,
                                result,
                                flags,
                                completionSocketAddressLen,
                                completionControlBufferLen,
                                completionAuxiliaryData,
                                hasFixedRecvBuffer,
                                fixedRecvBufferId,
                                ref enqueuedFallbackEvent);
                        }
                        else
                        {
                            // Dispatch expects full tagged user_data so tracked-ownership decode can validate tag+generation.
                            handler.DispatchSingleIoUringCompletion(
                                userData,
                                result,
                                flags,
                                completionSocketAddressLen,
                                completionControlBufferLen,
                                completionAuxiliaryData,
                                hasFixedRecvBuffer,
                                fixedRecvBufferId,
                                ref enqueuedFallbackEvent);
                        }

                        // Free the completion slot AFTER dispatch has taken the tracked
                        // operation. FreeCompletionSlot nulls TrackedOperation, so it must
                        // run after TryTakeTrackedIoUringOperation in the dispatch methods.
                        if (shouldFreeSlot)
                        {
                            FreeCompletionSlot(DecodeCompletionSlotIndex(payload));
                        }
                    }
                    else if (tag == IoUringConstants.TagWakeupSignal)
                    {
                        HandleManagedWakeupSignal(result);
                        if ((flags & IoUringConstants.CqeFMore) == 0 &&
                            Volatile.Read(ref _ioUringTeardownInitiated) == 0 &&
                            !QueueManagedWakeupPollAdd())
                        {
                        }
                    }
                    else if (tag != IoUringConstants.TagNone)
                    {
                        Debug.Fail($"Unknown io_uring CQE user_data tag: {tag}.");
                    }

                    drained++;
                }
            }
            finally
            {
                providedBufferRing?.EndDeferredRecyclePublish();
                FlushIoUringDrainTelemetryBatch();
                if (deferredCqHeadAdvance != 0 && _managedCqHeadPtr is not null)
                {
                    Volatile.Write(ref *_managedCqHeadPtr, _managedCachedCqHead);
                }
            }

            if (enqueuedFallbackEvent)
            {
                EnsureWorkerScheduled();
            }

            TryCompleteManagedCqOverflowRecovery();
            AssertCompletionSlotUsageBounded();

            return drainedAnyCqe;
        }

        /// <summary>
        /// Resolves metadata for a reserved completion by applying forced test results and
        /// copying operation-specific completion outputs (accept/recvmsg) from native storage.
        /// </summary>
        private void ResolveReservedCompletionSlotMetadata(
            ulong payload,
            bool isMultishotCompletion,
            ref int result,
            out int completionSocketAddressLen,
            out int completionControlBufferLen,
            out uint completionAuxiliaryData,
            out bool hasFixedRecvBuffer,
            out ushort fixedRecvBufferId,
            out bool shouldFreeSlot)
        {
            completionSocketAddressLen = 0;
            completionControlBufferLen = 0;
            completionAuxiliaryData = 0;
            hasFixedRecvBuffer = false;
            fixedRecvBufferId = 0;
            shouldFreeSlot = false;

            int slotIndex = DecodeCompletionSlotIndex(payload);
            if ((uint)slotIndex >= (uint)_completionSlots!.Length)
            {
                return;
            }

            ref IoUringCompletionSlot slot = ref _completionSlots[slotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![slotIndex];
            ulong completionGeneration = (payload >> IoUringConstants.SlotIndexBits) & IoUringConstants.GenerationMask;
            if (completionGeneration != slot.Generation)
            {
                // Stale CQE for a recycled slot; ignore without mutating current slot state.
                return;
            }

            ResolveDebugTestForcedResult(ref slot, ref result);

            if (slot.UsesFixedRecvBuffer)
            {
                hasFixedRecvBuffer = true;
                fixedRecvBufferId = slot.FixedRecvBufferId;
                slot.UsesFixedRecvBuffer = false;
                slot.FixedRecvBufferId = 0;
                Debug.Assert(!isMultishotCompletion, "Fixed-buffer receive completions are expected to be one-shot.");
            }

            if (slot.Kind == IoUringCompletionOperationKind.Accept &&
                slotStorage.NativeSocketAddressLengthPtr is not null)
            {
                int nativeSocketAddressLength = *slotStorage.NativeSocketAddressLengthPtr;
                completionAuxiliaryData = nativeSocketAddressLength >= 0 ? (uint)nativeSocketAddressLength : 0u;
                if (isMultishotCompletion)
                {
                    int socketAddressCapacity = slotStorage.ReceiveSocketAddressCapacity;
                    if (socketAddressCapacity > 0 && slotStorage.NativeSocketAddress is not null)
                    {
                        Unsafe.InitBlockUnaligned(slotStorage.NativeSocketAddress, 0, (uint)socketAddressCapacity);
                    }

                    *slotStorage.NativeSocketAddressLengthPtr = socketAddressCapacity >= 0 ? socketAddressCapacity : 0;
                }
            }
            else if (slot.Kind == IoUringCompletionOperationKind.Message)
            {
                CopyMessageCompletionOutputs(
                    slotIndex,
                    out completionSocketAddressLen,
                    out completionControlBufferLen,
                    out completionAuxiliaryData);
            }

            if (!isMultishotCompletion)
            {
                if (!slot.IsZeroCopySend)
                {
                    shouldFreeSlot = true;
                }
                else if (result < 0)
                {
                    // Error completion path may not produce a NOTIF CQE.
                    shouldFreeSlot = true;
                }
                else if (!slot.ZeroCopyNotificationPending)
                {
                    // First CQE for zero-copy send: keep slot alive until NOTIF CQE arrives.
                    slot.ZeroCopyNotificationPending = true;
                    AssertZeroCopyNotificationPendingForPayload(payload);
                }
            }
        }

        /// <summary>
        /// Handles NOTIF CQEs for zero-copy sends: validates and clears ZC pending state.
        /// The caller must free the completion slot after dispatch takes the tracked operation.
        /// </summary>
        private bool HandleZeroCopyNotification(ulong payload)
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return false;
            }

            int slotIndex = DecodeCompletionSlotIndex(payload);
            if ((uint)slotIndex >= (uint)completionEntries.Length)
            {
                return false;
            }

            ref IoUringCompletionSlot slot = ref completionEntries[slotIndex];
            ulong completionGeneration = (payload >> IoUringConstants.SlotIndexBits) & IoUringConstants.GenerationMask;
            if (slot.Generation != completionGeneration)
            {
                return false;
            }

            if (!slot.IsZeroCopySend || !slot.ZeroCopyNotificationPending)
            {
                return false;
            }

            slot.IsZeroCopySend = false;
            slot.ZeroCopyNotificationPending = false;
            return true;
        }

        /// <summary>Returns true when the completion slot for <paramref name="userData"/> is waiting on SEND_ZC NOTIF.</summary>
        private bool IsZeroCopyNotificationPending(ulong userData)
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return false;
            }

            int slotIndex = DecodeCompletionSlotIndex(userData & IoUringUserDataPayloadMask);
            if ((uint)slotIndex >= (uint)completionEntries.Length)
            {
                return false;
            }

            ref IoUringCompletionSlot slot = ref completionEntries[slotIndex];
            return slot.IsZeroCopySend && slot.ZeroCopyNotificationPending;
        }

        /// <summary>
        /// Releases a deferred SEND_ZC completion slot when dispatch cannot reattach ownership.
        /// </summary>
        private bool TryCleanupDeferredZeroCopyCompletionSlot(ulong userData)
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return false;
            }

            int slotIndex = DecodeCompletionSlotIndex(userData & IoUringUserDataPayloadMask);
            if ((uint)slotIndex >= (uint)completionEntries.Length)
            {
                return false;
            }

            ref IoUringCompletionSlot slot = ref completionEntries[slotIndex];
            ulong completionGeneration = ((userData & IoUringUserDataPayloadMask) >> IoUringConstants.SlotIndexBits) & IoUringConstants.GenerationMask;
            if (slot.Generation != completionGeneration)
            {
                return false;
            }

            if (!slot.IsZeroCopySend || !slot.ZeroCopyNotificationPending)
            {
                return false;
            }

            slot.IsZeroCopySend = false;
            slot.ZeroCopyNotificationPending = false;
            FreeCompletionSlot(slotIndex);
            return true;
        }

        /// <summary>Debug assertion that a reserved completion payload remains armed for SEND_ZC NOTIF.</summary>
        [Conditional("DEBUG")]
        private void AssertZeroCopyNotificationPendingForPayload(ulong payload)
        {
            ulong userData = EncodeIoUringUserData(IoUringConstants.TagReservedCompletion, payload);
            Debug.Assert(
                IsZeroCopyNotificationPending(userData),
                "SEND_ZC first CQE must leave the completion slot pending until NOTIF CQE arrives.");
        }

        /// <summary>Debug assertion that SEND_ZC completion dispatch is deferred until NOTIF arrives.</summary>
        [Conditional("DEBUG")]
        private void AssertZeroCopyDeferredCompletionState(ulong userData, SocketAsyncContext.AsyncOperation operation)
        {
            Debug.Assert(
                operation.IoUringUserData == userData,
                "Deferred SEND_ZC completion must retain the original user_data until NOTIF CQE dispatch.");
            Debug.Assert(
                IsZeroCopyNotificationPending(userData),
                "Deferred SEND_ZC completion requires an armed NOTIF state.");
        }

        /// <summary>Observes kernel CQ overflow count deltas and emits telemetry/logs.</summary>
        private unsafe void ObserveManagedCqOverflowCounter()
        {
            if (_managedCqOverflowPtr is null)
            {
                return;
            }

            uint observedOverflow = Volatile.Read(ref *_managedCqOverflowPtr);
            uint previousOverflow = _managedObservedCqOverflow;
            // The kernel counter is uint32 and wraps; compare via wrapped delta instead of monotonic ordering.
            uint delta = unchecked(observedOverflow - previousOverflow);
            if (delta == 0)
            {
                return;
            }

            _managedObservedCqOverflow = observedOverflow;
            SocketsTelemetry.Log.IoUringCqOverflow(delta);
            // Defer stale-tracked sweep scheduling until recovery completes.
            Volatile.Write(ref _cqOverflowTrackedSweepDeadlineTicks, 0);
            _cqOverflowTrackedSweepRearmCount = 0;

            IoUringCqOverflowRecoveryBranch branch = _cqOverflowRecoveryActive ?
                IoUringCqOverflowRecoveryBranch.DualWave :
                DetermineCqOverflowRecoveryBranchAtEntry();
            _cqOverflowRecoveryActive = true;
            _cqOverflowRecoveryBranch = branch;
            AssertLiveAcceptSlotsRemainTrackedDuringRecovery(branch);

        }

        /// <summary>Determines the initial recovery branch discriminator for a newly observed CQ overflow.</summary>
        private IoUringCqOverflowRecoveryBranch DetermineCqOverflowRecoveryBranchAtEntry()
        {
            if (Volatile.Read(ref _ioUringTeardownInitiated) != 0)
            {
                return IoUringCqOverflowRecoveryBranch.Teardown;
            }

            if (_ioUringCapabilities.SupportsMultishotAccept &&
                HasLiveAcceptCompletionSlot())
            {
                return IoUringCqOverflowRecoveryBranch.MultishotAcceptArming;
            }

            return IoUringCqOverflowRecoveryBranch.DualWave;
        }

        /// <summary>Returns true when at least one active completion slot is currently tracking accept metadata.</summary>
        private bool HasLiveAcceptCompletionSlot()
        {
            // Keep this O(1): CQ-overflow branch selection can run frequently on the event loop hot path.
            int liveAcceptCount = Volatile.Read(ref _liveAcceptCompletionSlotCount);
            Debug.Assert(liveAcceptCount >= 0);
            return liveAcceptCount != 0;
        }

        /// <summary>
        /// Completes CQ-overflow recovery once the ring is drained and no additional overflow increments are observed.
        /// Recovery is best-effort: dropped CQEs cannot be reconstructed, so this only restores steady-state draining.
        /// </summary>
        private unsafe void TryCompleteManagedCqOverflowRecovery()
        {
            if (!_cqOverflowRecoveryActive ||
                _managedCqOverflowPtr is null ||
                _managedCqTailPtr is null)
            {
                return;
            }

            uint cqTail = Volatile.Read(ref *_managedCqTailPtr);
            if (_managedCachedCqHead != cqTail)
            {
                return;
            }

            if (Volatile.Read(ref _ioUringTeardownInitiated) != 0)
            {
                _cqOverflowRecoveryBranch = IoUringCqOverflowRecoveryBranch.Teardown;
            }

            uint observedOverflow = Volatile.Read(ref *_managedCqOverflowPtr);
            // The kernel counter is uint32 and wraps; compare via wrapped subtraction.
            uint delta = unchecked(observedOverflow - _managedObservedCqOverflow);
            if (delta > 0)
            {
                _managedObservedCqOverflow = observedOverflow;
                if (_cqOverflowRecoveryBranch != IoUringCqOverflowRecoveryBranch.Teardown)
                {
                    _cqOverflowRecoveryBranch = IoUringCqOverflowRecoveryBranch.DualWave;
                }
                SocketsTelemetry.Log.IoUringCqOverflow(delta);

                return;
            }

            _cqOverflowRecoveryActive = false;
            _cqOverflowTrackedSweepRearmCount = 0;
            Volatile.Write(
                ref _cqOverflowTrackedSweepDeadlineTicks,
                Environment.TickCount64 + CqOverflowTrackedSweepDelayMilliseconds);
            SocketsTelemetry.Log.IoUringCqOverflowRecovery(1);
            if (_cqOverflowRecoveryBranch == IoUringCqOverflowRecoveryBranch.MultishotAcceptArming)
            {
                // Phase 1 spec branch (a): if CQ overflow occurs while multishot accept is live,
                // defer re-arm nudges until after drain completes instead of discarding active state.
                TryQueueDeferredMultishotAcceptRearmAfterRecovery();
            }
            AssertCompletionSlotPoolConsistency();

        }

        /// <summary>
        /// After CQ-overflow recovery completes, performs a delayed sweep to retire tracked operations
        /// that remain attached despite already transitioning out of the waiting state.
        /// </summary>
        private void TrySweepStaleTrackedIoUringOperationsAfterCqOverflowRecovery()
        {
            if (!_ioUringCapabilities.IsCompletionMode ||
                _cqOverflowRecoveryActive ||
                !IsCurrentThreadEventLoopThread())
            {
                return;
            }

            long deadline = Volatile.Read(ref _cqOverflowTrackedSweepDeadlineTicks);
            if (deadline == 0 ||
                unchecked(Environment.TickCount64 - deadline) < 0)
            {
                return;
            }

            // Consume the deadline before the sweep; follow-up work can re-arm it.
            Volatile.Write(ref _cqOverflowTrackedSweepDeadlineTicks, 0);

            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            IoUringTrackedOperationState[]? trackedOperations = _trackedOperations;
            if (completionEntries is null ||
                trackedOperations is null ||
                trackedOperations.Length != completionEntries.Length ||
                IsIoUringTrackingEmpty())
            {
                return;
            }

            int detachedCount = 0;
            int canceledWaitingCount = 0;

            for (int slotIndex = 0; slotIndex < trackedOperations.Length; slotIndex++)
            {
                ref IoUringTrackedOperationState trackedState = ref trackedOperations[slotIndex];
                SocketAsyncContext.AsyncOperation? operation = Volatile.Read(ref trackedState.TrackedOperation);
                if (operation is null)
                {
                    continue;
                }

                ulong generation = Volatile.Read(ref trackedState.TrackedOperationGeneration);
                if (generation == 0)
                {
                    continue;
                }

                ulong payload = EncodeCompletionSlotUserData(slotIndex, generation);
                ulong userData = EncodeIoUringUserData(IoUringConstants.TagReservedCompletion, payload);
                if (operation.IoUringUserData != userData)
                {
                    continue;
                }

                IoUringCompletionOperationKind kind = completionEntries[slotIndex].Kind;
                if (ShouldSkipCqOverflowTrackedSweep(operation, userData, kind))
                {
                    continue;
                }

                if (operation.IsInWaitingState())
                {
                    if (operation.TryCancel())
                    {
                        canceledWaitingCount++;
                    }

                    continue;
                }

                if (TryUntrackTrackedIoUringOperation(userData, operation, out SocketAsyncContext.AsyncOperation? removedOperation) != IoUringTrackedOperationRemoveResult.Removed ||
                    removedOperation is null)
                {
                    continue;
                }

                removedOperation.ClearIoUringUserData();
                FreeCompletionSlot(slotIndex);
                detachedCount++;
            }

            // Sweep for orphaned SEND_ZC completion slots whose NOTIF CQE was lost to CQ overflow.
            int zeroCopyOrphanCount = SweepOrphanedZeroCopyNotificationSlots(completionEntries, trackedOperations);

            int totalDrainRecovery = detachedCount + zeroCopyOrphanCount;
            if (totalDrainRecovery != 0)
            {
                SocketsTelemetry.Log.IoUringCompletionSlotDrainRecovery(totalDrainRecovery);
            }

            if (canceledWaitingCount != 0)
            {
                if (_cqOverflowTrackedSweepRearmCount < CqOverflowTrackedSweepMaxRearms)
                {
                    _cqOverflowTrackedSweepRearmCount++;
                    Volatile.Write(
                        ref _cqOverflowTrackedSweepDeadlineTicks,
                        Environment.TickCount64 + CqOverflowTrackedSweepDelayMilliseconds);
                }
            }
            else
            {
                _cqOverflowTrackedSweepRearmCount = 0;
            }
        }

        /// <summary>
        /// Scans completion slots for SEND_ZC entries stuck in ZeroCopyNotificationPending state
        /// with no corresponding tracked operation, indicating a lost NOTIF CQE from CQ overflow.
        /// </summary>
        private int SweepOrphanedZeroCopyNotificationSlots(
            IoUringCompletionSlot[] completionEntries,
            IoUringTrackedOperationState[] trackedOperations)
        {
            int freedCount = 0;
            for (int slotIndex = 0; slotIndex < completionEntries.Length; slotIndex++)
            {
                ref IoUringCompletionSlot slot = ref completionEntries[slotIndex];
                if (!slot.IsZeroCopySend || !slot.ZeroCopyNotificationPending)
                {
                    continue;
                }

                // The slot is waiting for a NOTIF CQE. Check whether any tracked operation
                // still references this slot. If not, the first CQE was already processed and
                // the operation was completed/dispatched, meaning the NOTIF CQE is the only
                // thing keeping this slot alive -- and it was lost to CQ overflow.
                ref IoUringTrackedOperationState trackedState = ref trackedOperations[slotIndex];
                if (Volatile.Read(ref trackedState.TrackedOperation) is not null)
                {
                    continue;
                }

                // Orphaned: NOTIF-pending with no tracked operation. Force-free the slot.
                slot.IsZeroCopySend = false;
                slot.ZeroCopyNotificationPending = false;
                FreeCompletionSlot(slotIndex);
                freedCount++;
            }

            return freedCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldSkipCqOverflowTrackedSweep(
            SocketAsyncContext.AsyncOperation operation,
            ulong userData,
            IoUringCompletionOperationKind kind)
        {
            SocketAsyncContext context = operation.AssociatedContext;

            if (kind == IoUringCompletionOperationKind.Accept &&
                context.IsMultishotAcceptArmed &&
                context.MultishotAcceptUserData == userData)
            {
                // Active multishot accept slots are intentionally long-lived.
                return true;
            }

            if (kind == IoUringCompletionOperationKind.Message &&
                context.IsPersistentMultishotRecvArmed() &&
                context.PersistentMultishotRecvUserData == userData)
            {
                // Persistent multishot recv slots are intentionally long-lived.
                return true;
            }

            return false;
        }

        /// <summary>Debug assertion for Phase-1 branch (a): live multishot-accept slots must remain tracked during recovery.</summary>
        [Conditional("DEBUG")]
        private void AssertLiveAcceptSlotsRemainTrackedDuringRecovery(IoUringCqOverflowRecoveryBranch branch)
        {
            if (branch != IoUringCqOverflowRecoveryBranch.MultishotAcceptArming)
            {
                return;
            }

            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return;
            }

            bool foundTrackedAccept = false;
            for (int i = 0; i < completionEntries.Length; i++)
            {
                if (completionEntries[i].Kind != IoUringCompletionOperationKind.Accept)
                {
                    continue;
                }

                ulong payload = EncodeCompletionSlotUserData(i, completionEntries[i].Generation);
                ulong userData = EncodeIoUringUserData(IoUringConstants.TagReservedCompletion, payload);
                if (ContainsTrackedIoUringOperation(userData))
                {
                    foundTrackedAccept = true;
                    break;
                }
            }

            Debug.Assert(
                foundTrackedAccept,
                "CQ-overflow recovery branch (a) requires at least one live tracked multishot-accept slot.");
        }

        /// <summary>
        /// After overflow recovery completes, nudges accept contexts with live multishot accept state
        /// so the managed accept pipeline can resume dequeue/prepare flow.
        /// </summary>
        private void TryQueueDeferredMultishotAcceptRearmAfterRecovery()
        {
            if (!_ioUringCapabilities.SupportsMultishotAccept ||
                Volatile.Read(ref _ioUringTeardownInitiated) != 0)
            {
                return;
            }

            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return;
            }

            for (int slotIndex = 0; slotIndex < completionEntries.Length; slotIndex++)
            {
                if (completionEntries[slotIndex].Kind != IoUringCompletionOperationKind.Accept)
                {
                    continue;
                }

                ulong payload = EncodeCompletionSlotUserData(slotIndex, completionEntries[slotIndex].Generation);
                ulong userData = EncodeIoUringUserData(IoUringConstants.TagReservedCompletion, payload);
                if (!TryGetTrackedIoUringOperation(userData, out SocketAsyncContext.AsyncOperation? operation) ||
                    operation is not SocketAsyncContext.AcceptOperation acceptOperation)
                {
                    continue;
                }

                SocketAsyncContext context = acceptOperation.AssociatedContext;
                if (!context.IsMultishotAcceptArmed ||
                    context.MultishotAcceptUserData != userData)
                {
                    continue;
                }

                EnqueueReadinessFallbackEvent(context, Interop.Sys.SocketEvents.Read);
            }
        }

        /// <summary>
        /// Handles a wakeup signal CQE by consuming the eventfd counter.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void HandleManagedWakeupSignal(int cqeResult)
        {
            if (cqeResult >= 0 && _managedWakeupEventFd >= 0)
            {
                ulong value;
                _ = Interop.Sys.IoUringShimReadEventFd(_managedWakeupEventFd, &value);
            }
        }

        private const int FdCloexec = 1;
        /// <summary>io_uring completion mode does not use socket event registration updates.</summary>
        partial void LinuxTryChangeSocketEventRegistration(
            IntPtr socketHandle,
            Interop.Sys.SocketEvents currentEvents,
            Interop.Sys.SocketEvents newEvents,
            int data,
            ref Interop.Error error,
            ref bool handled)
        {
            if (!Volatile.Read(ref _ioUringInitialized))
            {
                return;
            }

            handled = true;
            error = Interop.Error.SUCCESS;
        }

        private static bool TrySetFdCloseOnExec(int fd)
        {
            int currentFlags = Interop.Sys.Fcntl.GetFD((IntPtr)fd);
            if (currentFlags < 0)
            {
                return false;
            }

            int updatedFlags = currentFlags | FdCloexec;
            if (updatedFlags == currentFlags)
            {
                return true;
            }

            if (Interop.Sys.Fcntl.SetFD((IntPtr)fd, updatedFlags) == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Probes the kernel for supported io_uring opcodes using IORING_REGISTER_PROBE and
        /// populates the per-opcode <c>_supportsOp*</c> capability flags.
        /// When the probe syscall is unavailable (older kernels), all flags remain at their
        /// default value (<see langword="false"/>).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void ProbeIoUringOpcodeSupport(int ringFd)
        {
            // Probe buffer: 16-byte header + 256 * 8-byte ops = 2064 bytes.
            const int maxOps = 256;
            const int probeSize = 16 + maxOps * 8;
            byte* probeBuffer = stackalloc byte[probeSize];
            new Span<byte>(probeBuffer, probeSize).Clear();

            int result;
            Interop.Error err = Interop.Sys.IoUringShimRegister(
                ringFd, IoUringConstants.RegisterProbe, probeBuffer, (uint)maxOps, &result);

            if (err != Interop.Error.SUCCESS)
            {
                // Probe not supported (for example older kernels): per-opcode flags remain false.
                // Direct SQE prep does not gate on these flags; this mainly affects optional feature light-up.
                return;
            }

            // Parse: ops start at offset 16, each is 8 bytes.
            IoUringProbeOp* ops = (IoUringProbeOp*)(probeBuffer + 16);
            IoUringProbeHeader* header = (IoUringProbeHeader*)probeBuffer;
            int opsCount = Math.Min((int)header->OpsLen, maxOps);

            _supportsOpReadFixed = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.ReadFixed);
            _supportsOpSend = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.Send);
            _supportsOpRecv = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.Recv);
            _supportsOpSendMsg = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.SendMsg);
            _supportsOpRecvMsg = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.RecvMsg);
            _supportsOpAccept = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.Accept);
            _supportsOpConnect = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.Connect);
            _supportsOpSendZc = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.SendZc);
            _supportsOpSendMsgZc = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.SendMsgZc);
            _zeroCopySendEnabled = _supportsOpSendZc && IsZeroCopySendOptedIn();
            _supportsOpAsyncCancel = IsOpcodeSupported(ops, opsCount, IoUringOpcodes.AsyncCancel);
            _supportsMultishotAccept = _supportsOpAccept && !IsMultishotAcceptDisabled();
            RefreshIoUringMultishotRecvSupport();
        }

        /// <summary>Checks whether a specific opcode is supported by the kernel's io_uring probe result.</summary>
        private static unsafe bool IsOpcodeSupported(IoUringProbeOp* ops, int opsCount, byte opcode)
        {
            if (opcode >= opsCount) return false;
            return (ops[opcode].Flags & IoUringConstants.ProbeOpFlagSupported) != 0;
        }

        /// <summary>Publishes the managed SQ tail pointer to make queued SQEs visible to the kernel.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void PublishManagedSqeTail()
        {
            if (!_ioUringManagedSqTailLoaded || _ioUringSqRingInfo.SqTailPtr == IntPtr.Zero)
            {
                return;
            }

            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "PublishManagedSqeTail must only be called from the event loop thread (SINGLE_ISSUER contract).");
            ref uint sqTailRef = ref Unsafe.AsRef<uint>((void*)_ioUringSqRingInfo.SqTailPtr);
            Volatile.Write(ref sqTailRef, _ioUringManagedSqTail);
            _ioUringManagedSqTailLoaded = false;
        }

        /// <summary>
        /// Returns true when the SQPOLL kernel thread has gone idle and needs an explicit wakeup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool SqNeedWakeup()
        {
            Debug.Assert(_sqPollEnabled, "SqNeedWakeup should only be checked in SQPOLL mode.");
            if (_managedSqFlagsPtr == null)
            {
                return true;
            }

            return (Volatile.Read(ref *_managedSqFlagsPtr) & IoUringConstants.SqNeedWakeup) != 0;
        }

        /// <summary>Allocates the next available SQE slot from the submission ring.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool TryGetNextManagedSqe(out IoUringSqe* sqe)
        {
            sqe = null;
            if (!_ioUringDirectSqeEnabled)
            {
                return false;
            }

            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "TryGetNextManagedSqe must only be called from the event loop thread (SINGLE_ISSUER contract).");
            if (!_managedSqeInvariantsValidated)
            {
                return false;
            }

            ref Interop.Sys.IoUringSqRingInfo ringInfo = ref _ioUringSqRingInfo;
            Debug.Assert(ringInfo.SqeBase != IntPtr.Zero);
            Debug.Assert(ringInfo.SqHeadPtr != IntPtr.Zero);
            Debug.Assert(ringInfo.SqTailPtr != IntPtr.Zero);
            Debug.Assert(ringInfo.SqEntries != 0);
            Debug.Assert(ringInfo.SqeSize == (uint)sizeof(IoUringSqe));

            ref uint sqHeadRef = ref Unsafe.AsRef<uint>((void*)ringInfo.SqHeadPtr);
            uint sqHead = Volatile.Read(ref sqHeadRef);
            if (!_ioUringManagedSqTailLoaded)
            {
                ref uint sqTailRef = ref Unsafe.AsRef<uint>((void*)ringInfo.SqTailPtr);
                _ioUringManagedSqTail = Volatile.Read(ref sqTailRef);
                _ioUringManagedSqTailLoaded = true;
            }

            uint sqTail = _ioUringManagedSqTail;
            if (sqTail - sqHead >= ringInfo.SqEntries)
            {
                return false;
            }

            uint index = sqTail & ringInfo.SqMask;
            nint sqeOffset = checked((nint)((nuint)index * ringInfo.SqeSize));
            sqe = (IoUringSqe*)((byte*)ringInfo.SqeBase + sqeOffset);
            // Managed direct-SQE preparation in System.Net.Sockets is socket-opcode-only. Clearing the full
            // SQE is safe because every opcode we emit initializes all fields it relies on.
            Unsafe.WriteUnaligned(sqe, default(IoUringSqe));
            _ioUringManagedSqTail = sqTail + 1;
            _ioUringManagedPendingSubmissions++;
            return true;
        }

        /// <summary>Validates immutable SQ ring invariants once at initialization.</summary>
        private bool ValidateManagedSqeInitializationInvariants()
        {
            ref Interop.Sys.IoUringSqRingInfo ringInfo = ref _ioUringSqRingInfo;
            if (ringInfo.SqeBase == IntPtr.Zero ||
                ringInfo.SqHeadPtr == IntPtr.Zero ||
                ringInfo.SqTailPtr == IntPtr.Zero ||
                ringInfo.SqEntries == 0)
            {
                return false;
            }

            if (ringInfo.SqeSize != (uint)sizeof(IoUringSqe))
            {
                Debug.Fail($"Unexpected io_uring SQE size. Expected {sizeof(IoUringSqe)}, got {ringInfo.SqeSize}.");
                return false;
            }

            return true;
        }

        /// <summary>Attempts to acquire an SQE, retrying with intermediate submits on ring full.</summary>
        private unsafe bool TryAcquireManagedSqeWithRetry(out IoUringSqe* sqe, out Interop.Error submitError)
        {
            sqe = null;
            submitError = Interop.Error.SUCCESS;
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "TryAcquireManagedSqeWithRetry must only be called from the event loop thread (SINGLE_ISSUER contract).");
            SocketEventHandler drainHandler = default;
            bool drainHandlerInitialized = false;

            for (int attempt = 0; attempt < MaxIoUringSqeAcquireSubmitAttempts; attempt++)
            {
                if (TryGetNextManagedSqe(out sqe))
                {
                    return true;
                }

                // Before retrying submission, run a CQ drain pass so completions can release
                // slots and unblock kernel forward progress. The overflow counter is observed
                // during drain; do not assume a single pass fully clears overflow pressure.
                if (_managedCqDrainEnabled &&
                    _managedCqOverflowPtr is not null &&
                    _completionSlotsInUse != 0)
                {
                    if (!drainHandlerInitialized)
                    {
                        drainHandler = new SocketEventHandler(this);
                        drainHandlerInitialized = true;
                    }
                    _ = DrainCqeRingBatch(drainHandler);

                    if (TryGetNextManagedSqe(out sqe))
                    {
                        return true;
                    }
                }

                submitError = SubmitIoUringOperationsNormalized();
                if (submitError != Interop.Error.SUCCESS)
                {
                    return false;
                }
            }

            submitError = Interop.Error.EAGAIN;
            return false;
        }

        /// <summary>
        /// Common setup for direct SQE preparation: allocates a completion slot, encodes user data,
        /// resolves the socket fd/flags, applies test hooks, and acquires an SQE. On failure,
        /// restores test state and frees the slot.
        /// </summary>
        private unsafe struct IoUringDirectSqeSetupResult
        {
            public SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult PrepareResult;
            public int SlotIndex;
            public ulong UserData;
            public int SqeFd;
            public byte SqeFlags;
            public IoUringSqe* Sqe;
            public SocketError ErrorCode;
        }

        /// <summary>
        /// Prepares a direct SQE and returns all setup data as a single struct to avoid large
        /// out-parameter callsites in per-opcode prepare paths.
        /// </summary>
        /// <returns>
        /// <see cref="SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared"/> if the SQE was acquired
        /// (caller must write the SQE and return Prepared),
        /// or a terminal result (Unsupported/PrepareFailed) that the caller should return directly.
        /// </returns>
        private unsafe IoUringDirectSqeSetupResult TrySetupDirectSqe(
            SafeSocketHandle socket,
            byte opcode)
        {
            IoUringDirectSqeSetupResult setup = default;
            setup.SlotIndex = -1;
            setup.PrepareResult = SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            setup.ErrorCode = SocketError.Success;

            if (!_ioUringDirectSqeEnabled)
            {
                return setup;
            }

            int slotIndex = AllocateCompletionSlot();
            if (slotIndex < 0)
            {
                RecordIoUringCompletionSlotExhaustion();

                if (!_completionSlotDrainInProgress)
                {
                    _completionSlotDrainInProgress = true;
                    try
                    {
                        SocketEventHandler handler = new SocketEventHandler(this);
                        if (DrainCqeRingBatch(handler))
                        {
                            slotIndex = AllocateCompletionSlot();
                        }
                    }
                    finally
                    {
                        _completionSlotDrainInProgress = false;
                    }
                }

                if (slotIndex < 0)
                {
                    return setup;
                }

                RecordIoUringCompletionSlotDrainRecovery();
            }

            setup.SlotIndex = slotIndex;
            ref IoUringCompletionSlot slot = ref _completionSlots![slotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![slotIndex];
            setup.UserData = EncodeCompletionSlotUserData(slotIndex, slot.Generation);

            bool addedSocketRef = false;
            try
            {
                // Keep the fd alive from SQE prep through CQE retirement to avoid fd-reuse races after close.
                socket.DangerousAddRef(ref addedSocketRef);
            }
            catch (ObjectDisposedException)
            {
                FreeCompletionSlot(slotIndex);
                setup.SlotIndex = -1;
                setup.ErrorCode = SocketError.OperationAborted;
                setup.PrepareResult = SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.PrepareFailed;
                return setup;
            }

            if (!addedSocketRef)
            {
                FreeCompletionSlot(slotIndex);
                setup.SlotIndex = -1;
                setup.ErrorCode = SocketError.OperationAborted;
                setup.PrepareResult = SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.PrepareFailed;
                return setup;
            }

            slotStorage.DangerousRefSocketHandle = socket;
            // GC/rooting contract for fd lifetime:
            // Engine -> _completionSlotStorage[slotIndex].DangerousRefSocketHandle -> SafeSocketHandle.
            // Keep this chain alive across SQE submission through CQE retirement to avoid fd reuse races.
            SafeSocketHandle? operation = slotStorage.DangerousRefSocketHandle;
            Debug.Assert(operation != null);
            int socketFd = (int)(nint)operation!.DangerousGetHandle();
            ConfigureSocketSqeFdAndFlags(socketFd, out setup.SqeFd, out setup.SqeFlags);
            ApplyDebugTestForcedResult(ref slot, opcode);

            if (!TryAcquireManagedSqeWithRetry(out IoUringSqe* sqe, out Interop.Error submitError))
            {
                RestoreDebugTestForcedResultIfNeeded(slotIndex, opcode);
                FreeCompletionSlot(slotIndex);
                setup.SlotIndex = -1;

                if (submitError == Interop.Error.SUCCESS ||
                    submitError == Interop.Error.EAGAIN ||
                    submitError == Interop.Error.EWOULDBLOCK)
                {
                    return setup;
                }

                setup.ErrorCode = SocketPal.GetSocketErrorForErrorCode(submitError);
                setup.PrepareResult = SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.PrepareFailed;
                return setup;
            }

            setup.Sqe = sqe;
            setup.PrepareResult = SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
            return setup;
        }

        /// <summary>Prepares a send SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectSend(
            SafeSocketHandle socket,
            byte* buffer,
            int bufferLen,
            SocketFlags flags,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;

            if (!TryConvertIoUringPrepareSocketFlags(flags, out uint rwFlags))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.Send);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            WriteSendLikeSqe(setup.Sqe, IoUringOpcodes.Send, setup.SqeFd, setup.SqeFlags, setup.UserData, buffer, (uint)bufferLen, rwFlags);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>
        /// Prepares a send SQE, preferring SEND_ZC when eligible and falling back to SEND when unavailable.
        /// </summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectSendWithZeroCopyFallback(
            SafeSocketHandle socket,
            byte* buffer,
            int bufferLen,
            SocketFlags flags,
            out bool usedZeroCopy,
            out ulong userData,
            out SocketError errorCode)
        {
            usedZeroCopy = false;
            if (ShouldTryIoUringDirectSendZeroCopy(bufferLen))
            {
                SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult zeroCopyResult = TryPrepareIoUringDirectSendZc(
                    socket,
                    buffer,
                    bufferLen,
                    flags,
                    out userData,
                    out errorCode);
                if (zeroCopyResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported)
                {
                    usedZeroCopy = zeroCopyResult == SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
                    return zeroCopyResult;
                }
            }

            return TryPrepareIoUringDirectSend(
                socket,
                buffer,
                bufferLen,
                flags,
                out userData,
                out errorCode);
        }

        /// <summary>Prepares a zero-copy send SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectSendZc(
            SafeSocketHandle socket,
            byte* buffer,
            int bufferLen,
            SocketFlags flags,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;

            if (!ShouldTryIoUringDirectSendZeroCopy(bufferLen))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            if (!TryConvertIoUringPrepareSocketFlags(flags, out uint rwFlags))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.SendZc);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            ref IoUringCompletionSlot slot = ref _completionSlots![setup.SlotIndex];
            slot.IsZeroCopySend = true;
            slot.ZeroCopyNotificationPending = false;

            WriteSendLikeSqe(setup.Sqe, IoUringOpcodes.SendZc, setup.SqeFd, setup.SqeFlags, setup.UserData, buffer, (uint)bufferLen, rwFlags);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>Prepares a recv SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectRecv(
            SafeSocketHandle socket,
            byte* buffer,
            int bufferLen,
            SocketFlags flags,
            bool allowMultishotRecv,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;

            if (!TryConvertIoUringPrepareSocketFlags(flags, out uint rwFlags))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.Recv);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            if (ShouldTryIoUringDirectFixedRecv(flags, allowMultishotRecv, bufferLen) &&
                TryPrepareIoUringDirectRecvFixed(setup.SlotIndex, setup.Sqe, setup.SqeFd, setup.SqeFlags, setup.UserData, bufferLen))
            {
                SocketsTelemetry.Log.IoUringFixedRecvSelected();
                userData = setup.UserData;
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
            }

            if (allowMultishotRecv &&
                bufferLen > 0 &&
                TryGetIoUringMultishotRecvBufferGroupId(out ushort multishotBufferGroupId))
            {
                SetCompletionSlotKind(ref _completionSlots![setup.SlotIndex], IoUringCompletionOperationKind.Message);
                WriteMultishotRecvSqe(setup.Sqe, setup.SqeFd, setup.SqeFlags, setup.UserData, multishotBufferGroupId);
            }
            else if (bufferLen > 0 &&
                     TryGetIoUringProvidedBufferGroupId(out ushort providedBufferGroupId))
            {
                WriteProvidedBufferRecvSqe(
                    setup.Sqe,
                    setup.SqeFd,
                    setup.SqeFlags,
                    setup.UserData,
                    (uint)bufferLen,
                    rwFlags,
                    providedBufferGroupId);
            }
            else
            {
                WriteRecvSqe(setup.Sqe, setup.SqeFd, setup.SqeFlags, setup.UserData, buffer, (uint)bufferLen, rwFlags);
            }
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        private bool ShouldTryIoUringDirectFixedRecv(SocketFlags flags, bool allowMultishotRecv, int bufferLen)
        {
            if (!_supportsOpReadFixed || !_ioUringCapabilities.HasRegisteredBuffers)
            {
                return false;
            }

            if (allowMultishotRecv || bufferLen <= 0)
            {
                return false;
            }

            // READ_FIXED does not provide recvmsg/socket flags semantics.
            return flags == SocketFlags.None;
        }

        private unsafe bool TryPrepareIoUringDirectRecvFixed(
            int slotIndex,
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            int requestedLength)
        {
            IoUringProvidedBufferRing? providedBufferRing = _ioUringProvidedBufferRing;
            if (providedBufferRing is null)
            {
                SocketsTelemetry.Log.IoUringFixedRecvFallback();
                return false;
            }

            if (!providedBufferRing.TryAcquireBufferForPreparedReceive(
                    out ushort bufferId,
                    out byte* fixedBuffer,
                    out int fixedBufferLength))
            {
                // Under transient provided-buffer pressure, fall back to normal receive preparation.
                SocketsTelemetry.Log.IoUringFixedRecvFallback();
                return false;
            }

            Debug.Assert(_completionSlots is not null);
            ref IoUringCompletionSlot slot = ref _completionSlots![slotIndex];
            slot.UsesFixedRecvBuffer = true;
            slot.FixedRecvBufferId = bufferId;

            int receiveLength = Math.Min(requestedLength, fixedBufferLength);
            WriteReadFixedSqe(
                sqe,
                sqeFd,
                sqeFlags,
                userData,
                fixedBuffer,
                (uint)receiveLength,
                bufferId);
            return true;
        }

        /// <summary>Prepares an accept SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectAccept(
            SafeSocketHandle socket,
            byte* socketAddress,
            int socketAddressLen,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;
            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.Accept);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            ref IoUringCompletionSlot slot = ref _completionSlots![setup.SlotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![setup.SlotIndex];
            SetCompletionSlotKind(ref slot, IoUringCompletionOperationKind.Accept);
            Debug.Assert(slotStorage.NativeSocketAddressLengthPtr is not null);
            *slotStorage.NativeSocketAddressLengthPtr = socketAddressLen;

            WriteAcceptSqe(setup.Sqe, setup.SqeFd, setup.SqeFlags, setup.UserData, socketAddress, (IntPtr)slotStorage.NativeSocketAddressLengthPtr);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>Prepares a multishot accept SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectMultishotAccept(
            SafeSocketHandle socket,
            byte* socketAddress,
            int socketAddressLen,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;
            if (!_supportsMultishotAccept)
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.Accept);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }
            _ = socketAddress;
            _ = socketAddressLen;

            ref IoUringCompletionSlot slot = ref _completionSlots![setup.SlotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![setup.SlotIndex];
            SetCompletionSlotKind(ref slot, IoUringCompletionOperationKind.Accept);
            Debug.Assert(slotStorage.NativeSocketAddressLengthPtr is not null);
            // Security hardening: multishot accept reuses a single SQE across shots, so sharing one sockaddr
            // writeback buffer can race and surface mismatched peer addresses under bursty delivery.
            // Transitional multishot accept only needs accepted fds, so request no sockaddr writeback.
            *slotStorage.NativeSocketAddressLengthPtr = 0;
            slotStorage.ReceiveSocketAddressCapacity = 0;

            WriteMultishotAcceptSqe(
                setup.Sqe,
                setup.SqeFd,
                setup.SqeFlags,
                setup.UserData,
                socketAddress: null,
                socketAddressLengthPtr: IntPtr.Zero);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>
        /// Prepares a multishot accept SQE for a SO_REUSEPORT shadow listener.
        /// The slot uses <see cref="IoUringCompletionOperationKind.ReusePortAccept"/> so CQE dispatch
        /// forwards accepted fds to the primary listener's pre-accept queue without tracked-operation lookup.
        /// Must be called on this engine's event-loop thread.
        /// </summary>
        internal unsafe bool TryPrepareReusePortMultishotAccept(
            SafeSocketHandle shadowSocket,
            SocketAsyncContext primaryContext,
            SocketAsyncEngine primaryEngine,
            out ulong userData)
        {
            userData = 0;
            if (!_supportsMultishotAccept)
            {
                return false;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(shadowSocket, IoUringOpcodes.Accept);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                return false;
            }

            ref IoUringCompletionSlot slot = ref _completionSlots![setup.SlotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![setup.SlotIndex];
            SetCompletionSlotKind(ref slot, IoUringCompletionOperationKind.ReusePortAccept);
            slotStorage.ReusePortPrimaryContext = primaryContext;
            slotStorage.ReusePortPrimaryEngine = primaryEngine;

            WriteMultishotAcceptSqe(
                setup.Sqe,
                setup.SqeFd,
                setup.SqeFlags,
                setup.UserData,
                socketAddress: null,
                socketAddressLengthPtr: IntPtr.Zero);
            userData = setup.UserData;
            return true;
        }

        /// <summary>Prepares a connect SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectConnect(
            SafeSocketHandle socket,
            byte* socketAddress,
            int socketAddressLen,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;
            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.Connect);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            WriteConnectSqe(setup.Sqe, setup.SqeFd, setup.SqeFlags, setup.UserData, socketAddress, socketAddressLen);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>Prepares a sendmsg SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectSendMessage(
            SafeSocketHandle socket,
            Interop.Sys.MessageHeader* messageHeader,
            SocketFlags flags,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;

            if (!TryConvertIoUringPrepareSocketFlags(flags, out uint rwFlags))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.SendMsg);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            ref IoUringCompletionSlot slot = ref _completionSlots![setup.SlotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![setup.SlotIndex];
            SetCompletionSlotKind(ref slot, IoUringCompletionOperationKind.Message);
            if (!TryPrepareInlineMessageStorage(setup.SlotIndex, messageHeader, isReceive: false))
            {
                FreeCompletionSlot(setup.SlotIndex);
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            WriteSendMsgLikeSqe(setup.Sqe, IoUringOpcodes.SendMsg, setup.SqeFd, setup.SqeFlags, setup.UserData, slotStorage.NativeMsgHdrPtr, rwFlags);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>
        /// Prepares a sendmsg SQE, preferring SENDMSG_ZC when eligible and falling back to SENDMSG otherwise.
        /// </summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectSendMessageWithZeroCopyFallback(
            SafeSocketHandle socket,
            Interop.Sys.MessageHeader* messageHeader,
            int payloadLength,
            SocketFlags flags,
            out ulong userData,
            out SocketError errorCode)
        {
            if (ShouldTryIoUringDirectSendMessageZeroCopy(payloadLength))
            {
                SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult zeroCopyResult = TryPrepareIoUringDirectSendMessageZc(
                    socket,
                    messageHeader,
                    payloadLength,
                    flags,
                    out userData,
                    out errorCode);
                if (zeroCopyResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported)
                {
                    return zeroCopyResult;
                }
            }

            return TryPrepareIoUringDirectSendMessage(
                socket,
                messageHeader,
                flags,
                out userData,
                out errorCode);
        }

        /// <summary>Prepares a sendmsg_zc SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectSendMessageZc(
            SafeSocketHandle socket,
            Interop.Sys.MessageHeader* messageHeader,
            int payloadLength,
            SocketFlags flags,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;

            if (!ShouldTryIoUringDirectSendMessageZeroCopy(payloadLength))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            if (!TryConvertIoUringPrepareSocketFlags(flags, out uint rwFlags))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.SendMsgZc);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            ref IoUringCompletionSlot slot = ref _completionSlots![setup.SlotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![setup.SlotIndex];
            SetCompletionSlotKind(ref slot, IoUringCompletionOperationKind.Message);
            slotStorage.MessageIsReceive = false;
            // Mirror SEND_ZC semantics: first CQE is not final managed completion; operation
            // completes only after NOTIF CQE confirms kernel/NIC no longer references payload.
            slot.IsZeroCopySend = true;
            slot.ZeroCopyNotificationPending = false;
            if (!TryPrepareInlineMessageStorage(setup.SlotIndex, messageHeader, isReceive: false))
            {
                // Per-slot inline native slabs avoid hot-path allocations; overflow shapes
                // fall back by returning Unsupported so upper layers can use alternate paths.
                FreeCompletionSlot(setup.SlotIndex);
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            WriteSendMsgLikeSqe(setup.Sqe, IoUringOpcodes.SendMsgZc, setup.SqeFd, setup.SqeFlags, setup.UserData, slotStorage.NativeMsgHdrPtr, rwFlags);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>Prepares a recvmsg SQE via the managed direct path.</summary>
        internal unsafe SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult TryPrepareIoUringDirectReceiveMessage(
            SafeSocketHandle socket,
            Interop.Sys.MessageHeader* messageHeader,
            SocketFlags flags,
            out ulong userData,
            out SocketError errorCode)
        {
            userData = 0;
            errorCode = SocketError.Success;

            if (!TryConvertIoUringPrepareSocketFlags(flags, out uint rwFlags))
            {
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            IoUringDirectSqeSetupResult setup = TrySetupDirectSqe(socket, IoUringOpcodes.RecvMsg);
            if (setup.PrepareResult != SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared)
            {
                errorCode = setup.ErrorCode;
                return setup.PrepareResult;
            }

            ref IoUringCompletionSlot slot = ref _completionSlots![setup.SlotIndex];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![setup.SlotIndex];
            SetCompletionSlotKind(ref slot, IoUringCompletionOperationKind.Message);
            if (!TryPrepareInlineMessageStorage(setup.SlotIndex, messageHeader, isReceive: true))
            {
                FreeCompletionSlot(setup.SlotIndex);
                return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Unsupported;
            }

            WriteRecvMsgSqe(setup.Sqe, setup.SqeFd, setup.SqeFlags, setup.UserData, slotStorage.NativeMsgHdrPtr, rwFlags);
            userData = setup.UserData;
            return SocketAsyncContext.AsyncOperation.IoUringDirectPrepareResult.Prepared;
        }

        /// <summary>Debug-only assertion that validates a state machine transition.</summary>
        [Conditional("DEBUG")]
        private static void AssertIoUringLifecycleTransition(
            IoUringOperationLifecycleState from,
            IoUringOperationLifecycleState to)
        {
            bool isValid =
                from == IoUringOperationLifecycleState.Queued && to == IoUringOperationLifecycleState.Prepared ||
                from == IoUringOperationLifecycleState.Prepared && to == IoUringOperationLifecycleState.Submitted ||
                from == IoUringOperationLifecycleState.Prepared && to == IoUringOperationLifecycleState.Detached ||
                from == IoUringOperationLifecycleState.Submitted &&
                    (to == IoUringOperationLifecycleState.Queued ||
                     to == IoUringOperationLifecycleState.Completed ||
                     to == IoUringOperationLifecycleState.Canceled ||
                     to == IoUringOperationLifecycleState.Detached);

            Debug.Assert(isValid, $"Invalid io_uring lifecycle transition: {from} -> {to}");
        }

        /// <summary>Checks whether the kernel version meets the minimum for io_uring support.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsIoUringKernelVersionSupported()
        {
#if DEBUG
            if (string.Equals(
                    Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.ForceKernelVersionUnsupported),
                    "1",
                    StringComparison.Ordinal))
            {
                return false;
            }
#endif

            return OperatingSystem.IsOSPlatformVersionAtLeast(
                "Linux",
                IoUringConstants.MinKernelMajor,
                IoUringConstants.MinKernelMinor);
        }

        /// <summary>
        /// Recomputes whether multishot recv can be used by this engine instance.
        /// Requires opcode support and active provided-buffer ring support.
        /// </summary>
        private bool RefreshIoUringMultishotRecvSupport()
        {
            _supportsMultishotRecv =
                _supportsOpRecv &&
                _ioUringCapabilities.SupportsProvidedBufferRings;
            return _supportsMultishotRecv;
        }

        /// <summary>
        /// Returns the provided-buffer group id used for buffer-select receive submissions.
        /// </summary>
        private bool TryGetIoUringProvidedBufferGroupId(out ushort bufferGroupId)
        {
            if (_ioUringCapabilities.SupportsProvidedBufferRings && _ioUringProvidedBufferRing is not null)
            {
                bufferGroupId = _ioUringProvidedBufferGroupId;
                return true;
            }

            bufferGroupId = default;
            return false;
        }

        /// <summary>
        /// Returns the provided-buffer group id used for multishot recv submissions.
        /// Multishot recv remains disabled unless both the opcode probe and provided-ring
        /// registration succeeded for this engine instance.
        /// </summary>
        private bool TryGetIoUringMultishotRecvBufferGroupId(out ushort bufferGroupId)
        {
            if (_supportsMultishotRecv && TryGetIoUringProvidedBufferGroupId(out bufferGroupId))
            {
                return true;
            }

            bufferGroupId = default;
            return false;
        }

        internal bool SupportsMultishotRecv => _ioUringCapabilities.SupportsMultishotRecv;
        internal bool SupportsMultishotAccept => _ioUringCapabilities.SupportsMultishotAccept;

        /// <summary>Calls io_uring_setup and negotiates feature flags.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe bool TrySetupIoUring(bool sqPollRequested, out IoUringSetupResult setupResult)
        {
            setupResult = default;
            uint queueEntries = GetIoUringQueueEntries();

            uint flags = IoUringConstants.SetupCqSize | IoUringConstants.SetupSubmitAll
                       | IoUringConstants.SetupCoopTaskrun | IoUringConstants.SetupSingleIssuer
                       | IoUringConstants.SetupNoSqArray | IoUringConstants.SetupCloexec;

            if (sqPollRequested)
            {
                // SQPOLL and DEFER_TASKRUN are mutually exclusive in practice.
                flags |= IoUringConstants.SetupSqPoll;
            }
            else
            {
                // DEFER_TASKRUN defers task work to io_uring_enter only, reducing event-loop
                // CPU vs COOP_TASKRUN (which runs task work at every syscall boundary).
                // Requires SINGLE_ISSUER and submitter_task == current on every io_uring_enter.
                // io_uring_setup runs on the event loop thread (deferred from constructor) so
                // submitter_task is set correctly for the event loop's io_uring_enter calls.
                flags |= IoUringConstants.SetupDeferTaskrun;
            }

            Interop.Sys.IoUringParams ioParams = default;
            ioParams.Flags = flags;
            ioParams.CqEntries = queueEntries * IoUringConstants.CqEntriesFactor;

            int ringFd;
            Interop.Error err = Interop.Sys.IoUringShimSetup(queueEntries, &ioParams, &ringFd);

            // IORING_SETUP_NO_SQARRAY was introduced in Linux 6.6.
            // IORING_SETUP_CLOEXEC was introduced in Linux 5.19.
            // Peel unsupported setup flags on EINVAL and retry.
            if (err == Interop.Error.EINVAL &&
                (flags & IoUringConstants.SetupNoSqArray) != 0)
            {
                flags &= ~IoUringConstants.SetupNoSqArray;
                ioParams = default;
                ioParams.Flags = flags;
                ioParams.CqEntries = queueEntries * IoUringConstants.CqEntriesFactor;

                err = Interop.Sys.IoUringShimSetup(queueEntries, &ioParams, &ringFd);
            }

            if (err == Interop.Error.EINVAL &&
                (flags & IoUringConstants.SetupCloexec) != 0)
            {
                flags &= ~IoUringConstants.SetupCloexec;
                ioParams = default;
                ioParams.Flags = flags;
                ioParams.CqEntries = queueEntries * IoUringConstants.CqEntriesFactor;

                err = Interop.Sys.IoUringShimSetup(queueEntries, &ioParams, &ringFd);
            }

            if (err != Interop.Error.SUCCESS)
            {
                return false;
            }

            // IORING_SETUP_CLOEXEC removes the fork/exec inheritance window on supporting kernels.
            // Keep FD_CLOEXEC as a fallback for peeled/older setups.
            if (!TrySetFdCloseOnExec(ringFd))
            {
                // Ensure ring fd is not inherited across fork/exec; inherited ring fds can corrupt ownership.
                Interop.Sys.IoUringShimCloseFd(ringFd);
                return false;
            }

            setupResult.RingFd = ringFd;
            setupResult.Params = ioParams;
            setupResult.NegotiatedFlags = flags;
            setupResult.UsesExtArg = (ioParams.Features & IoUringConstants.FeatureExtArg) != 0;
            setupResult.SqPollNegotiated = (flags & IoUringConstants.SetupSqPoll) != 0;
            if (setupResult.SqPollNegotiated)
            {
                SocketsTelemetry.Log.ReportIoUringSqPollNegotiatedWarning();
            }
            return true;
        }


        /// <summary>Queues a POLL_ADD SQE on the wakeup eventfd for cross-thread signaling.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe bool QueueManagedWakeupPollAdd()
        {
            if (_managedWakeupEventFd < 0)
                return false;

            if (!TryGetNextManagedSqe(out IoUringSqe* sqe))
                return false;

            sqe->Opcode = IoUringOpcodes.PollAdd;
            sqe->Fd = _managedWakeupEventFd;
            sqe->Len = IoUringConstants.PollAddFlagMulti; // IORING_POLL_ADD_MULTI
            sqe->RwFlags = IoUringConstants.PollIn;
            sqe->UserData = EncodeIoUringUserData(IoUringConstants.TagWakeupSignal, 0);
            return true;
        }

        /// <summary>Attempts to register the ring fd for fixed-fd submission.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe bool TryRegisterRingFd(int ringFd, out int registeredRingFd)
        {
            registeredRingFd = -1;

            // io_uring_rsrc_update: { uint32 offset, uint32 resv, uint64 data }
            uint* update = stackalloc uint[4]; // 16 bytes
            update[0] = IoUringConstants.RegisterOffsetAuto; // offset = auto-assign
            update[1] = 0; // resv
            *(ulong*)(update + 2) = (ulong)ringFd; // data = ring fd

            int result;
            Interop.Error err = Interop.Sys.IoUringShimRegister(
                ringFd, IoUringConstants.RegisterRingFds, update, 1u, &result);

            if (err != Interop.Error.SUCCESS || result <= 0)
                return false;

            registeredRingFd = (int)update[0]; // kernel wrote assigned index back
            return true;
        }

        /// <summary>
        /// Configures the SQE fd and flags for a socket operation.
        /// Completion mode uses direct socket file descriptors.
        /// </summary>
        private static void ConfigureSocketSqeFdAndFlags(int socketFd, out int sqeFd, out byte sqeFlags)
        {
            sqeFd = socketFd;
            sqeFlags = 0;
        }


        /// <summary>
        /// Orchestrates complete managed io_uring initialization: kernel version check,
        /// ring setup with flag negotiation, mmap, opcode probe, eventfd creation,
        /// ring fd registration, and initial wakeup poll queue.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe bool TryInitializeManagedIoUring(in IoUringResolvedConfiguration resolvedConfiguration)
        {
            if (!IsIoUringKernelVersionSupported())
                return false;

            bool sqPollRequested = resolvedConfiguration.SqPollRequested;
            if (!TrySetupIoUring(sqPollRequested, out IoUringSetupResult setupResult))
                return false;

            if (!TryMmapRings(ref setupResult))
                return false;

            _sqPollEnabled = setupResult.SqPollNegotiated;

            // Probe opcode support.
            ProbeIoUringOpcodeSupport(setupResult.RingFd);

            // Create wakeup eventfd.
            int eventFd;
            Interop.Error err = Interop.Sys.IoUringShimCreateEventFd(&eventFd);
            if (err != Interop.Error.SUCCESS)
            {
                // Cleanup: unmap and close
                CleanupManagedRings();
                return false;
            }

            if (!TrySetFdCloseOnExec(eventFd))
            {
                // Eventfd wake channel must remain process-local across exec to prevent stale cross-process signaling.
                Interop.Sys.IoUringShimCloseFd(eventFd);
                CleanupManagedRings();
                return false;
            }

            _managedWakeupEventFd = eventFd;

            // Try to register the ring fd for faster enter syscalls.
            if (TryRegisterRingFd(setupResult.RingFd, out int registeredRingFd))
            {
                _ioUringSqRingInfo.RegisteredRingFd = registeredRingFd;
            }

            // Queue the initial wakeup POLL_ADD.
            // Direct SQE must be enabled for QueueManagedWakeupPollAdd to work.
            _ioUringDirectSqeEnabled = true;
            if (!QueueManagedWakeupPollAdd())
            {
                _ioUringDirectSqeEnabled = false;
                Interop.Sys.IoUringShimCloseFd(eventFd);
                _managedWakeupEventFd = -1;
                CleanupManagedRings();
                return false;
            }

            // Respect process-level direct SQE toggle after the required wakeup POLL_ADD is armed.
            if (resolvedConfiguration.DirectSqeDisabled)
            {
                _ioUringDirectSqeEnabled = false;
            }

            InitializeIoUringProvidedBufferRingIfSupported(setupResult.RingFd);
            RefreshIoUringMultishotRecvSupport();
            // _ioUringInitialized is set by the caller (LinuxDetectAndInitializeIoUring)
            // after the memory barrier + capabilities publication, so cross-thread readers
            // never observe _ioUringInitialized == true before ring state is fully visible.

            InitializeDebugTestHooksFromEnvironment();

            return true;
        }

        /// <summary>Validates the managed NativeMsghdr layout contract for direct io_uring message operations.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsNativeMsghdrLayoutSupportedForIoUring()
        {
            if (IsNativeMsghdrLayoutSupportedForIoUring(IntPtr.Size, sizeof(NativeMsghdr)))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNativeMsghdrLayoutSupportedForIoUring(int pointerSize, int nativeMsghdrSize) =>
            pointerSize == 8 && nativeMsghdrSize == 56;

        /// <summary>Detects io_uring support and initializes the managed submission/completion paths.</summary>
        partial void LinuxDetectAndInitializeIoUring()
        {
            IoUringResolvedConfiguration resolvedConfiguration = ResolveIoUringResolvedConfiguration();
            LogIoUringResolvedConfigurationIfNeeded(in resolvedConfiguration);
            if (!resolvedConfiguration.IoUringEnabled || !IsNativeMsghdrLayoutSupportedForIoUring() || !TryInitializeManagedIoUring(in resolvedConfiguration))
            {
                _ioUringCapabilities = ResolveLinuxIoUringCapabilities(isIoUringPort: false);
                SocketsTelemetry.Log.ReportSocketEngineBackendSelected(
                    isIoUringPort: false,
                    isCompletionMode: false,
                    sqPollEnabled: false);

                return;
            }

            // Initialize managed-side state before publishing capabilities.
            // Capabilities must be the last write: cross-thread readers gate on
            // IsIoUringPort / IsCompletionMode and then access queues and slot pools.
            InitializeLinuxIoUringDiagnosticsState();

            _ioUringSlotCapacity = (int)Math.Max(_managedCqEntries, IoUringConstants.QueueEntries);
            // Slot pool capacity is 2x slot capacity (currently 8192 with default cq sizing).
            // Multishot operations retain slots for their full lifetime, so this bounds
            // concurrent long-lived multishot receives before backpressure/exhaustion.
            _ioUringPrepareQueue = new MpscQueue<IoUringPrepareWorkItem>();
            _ioUringCancelQueue = new MpscQueue<ulong>();
            _reusePortShadowSetupQueue = new MpscQueue<ReusePortShadowSetupRequest>();
            int completionSlotCapacity = _ioUringSlotCapacity * IoUringConstants.CompletionOperationPoolCapacityFactor;
            InitializeCompletionSlotPool(completionSlotCapacity);

            _managedCqDrainEnabled = true;

            // Ensure all init state (ring mappings, queues, slot pools) is globally visible
            // before capabilities are published to cross-thread readers on ARM64.
            Thread.MemoryBarrier();

            _ioUringCapabilities = default(LinuxIoUringCapabilities)
                .WithIsIoUringPort(true)
                .WithMode(IoUringMode.Completion)
                .WithSupportsMultishotRecv(_supportsMultishotRecv)
                .WithSupportsMultishotAccept(_supportsMultishotAccept)
                .WithSupportsZeroCopySend(_zeroCopySendEnabled)
                .WithSqPollEnabled(_sqPollEnabled)
                .WithSupportsProvidedBufferRings(_ioUringProvidedBufferRing is not null)
                .WithHasRegisteredBuffers(_ioUringCapabilities.HasRegisteredBuffers);

            _ioUringInitialized = true;

            SocketsTelemetry.Log.ReportSocketEngineBackendSelected(
                isIoUringPort: true,
                isCompletionMode: true,
                sqPollEnabled: _ioUringCapabilities.SqPollEnabled);

        }

        /// <summary>Tears down io_uring state before native resource cleanup.</summary>
        partial void LinuxBeforeFreeNativeResources(ref bool closeSocketEventPort)
        {
            if (!_ioUringCapabilities.IsIoUringPort || _port == (IntPtr)(-1))
            {
                return;
            }

            // Publish teardown before draining queues/closing the native port so concurrent
            // producer paths observe shutdown via acquire reads and stop queueing new work.
            Volatile.Write(ref _ioUringTeardownInitiated, 1);
            DrainQueuedIoUringOperationsForTeardown();

            Interop.Error closeError = Interop.Sys.CloseSocketEventPort(_port);
            if (closeError == Interop.Error.SUCCESS)
            {
                closeSocketEventPort = false;
                Volatile.Write(ref _ioUringPortClosedForTeardown, 1);
            }
        }

        /// <summary>Submits pending SQEs before entering the wait.</summary>
        partial void LinuxEventLoopBeforeWait()
        {
            Interop.Error submitError = SubmitIoUringBatch();
            if (submitError != Interop.Error.SUCCESS)
            {
                // FailFast site: the event-loop submit step cannot degrade safely once
                // io_uring completion mode is active; losing submit progress would orphan tracked ops.
                ThrowInternalException(submitError);
            }
        }

        /// <summary>Attempts a managed completion wait using io_uring_enter with timeout.</summary>
        partial void LinuxEventLoopTryCompletionWait(SocketEventHandler handler, ref int numEvents, ref int numCompletions, ref Interop.Error err, ref bool waitHandled)
        {
            if (!_ioUringCapabilities.IsCompletionMode)
            {
                return;
            }

            // Managed CQE drain path: read CQEs directly from mmap'd ring.
            // First, try a non-blocking drain of any already-available CQEs.
            bool hadCqes = DrainCqeRingBatch(handler);
            bool deferTaskrunEnabled =
                (_managedNegotiatedFlags & IoUringConstants.SetupDeferTaskrun) != 0;
            bool forceDeferredTaskWorkEnter = hadCqes && deferTaskrunEnabled;
            if (hadCqes && !forceDeferredTaskWorkEnter)
            {
                numCompletions = 1;
                numEvents = 0;
                waitHandled = true;
                err = Interop.Error.SUCCESS;
                return;
            }

            // If CQEs were already drained and DEFER_TASKRUN is active, perform a non-blocking
            // io_uring_enter(GETEVENTS, minComplete=0) to flush deferred task work.
            // Otherwise perform the regular bounded wait for at least one CQE.
            uint minComplete = forceDeferredTaskWorkEnter ? 0u : 1u;
            uint enterFlags = IoUringConstants.EnterGetevents;
            int ringFd = _managedRingFd;
            if (_ioUringSqRingInfo.RegisteredRingFd >= 0)
            {
                enterFlags |= IoUringConstants.EnterRegisteredRing;
                ringFd = _ioUringSqRingInfo.RegisteredRingFd;
            }

            uint submitCount = _sqPollEnabled ? 0u : _ioUringManagedPendingSubmissions;
            if (_sqPollEnabled &&
                _ioUringManagedPendingSubmissions != 0 &&
                SqNeedWakeup())
            {
                enterFlags |= IoUringConstants.EnterSqWakeup;
            }

            if (_managedUsesExtArg)
            {
                // Bounded wait via EXT_ARG; timeout shortens when wake circuit-breaker is active.
                uint waitEnterFlags = enterFlags | IoUringConstants.EnterExtArg;
                int waitRingFd = ringFd;
                Interop.Sys.IoUringKernelTimespec timeout = default;
                timeout.TvNsec = forceDeferredTaskWorkEnter ? 0 : GetManagedCompletionWaitTimeoutNanos();
                Interop.Sys.IoUringGeteventsArg extArg = default;
                extArg.Ts = (ulong)(nuint)(&timeout);

                int result;
                err = Interop.Sys.IoUringShimEnterExt(
                    waitRingFd, submitCount, minComplete, waitEnterFlags, &extArg, &result);
                if (err == Interop.Error.EINVAL && (waitEnterFlags & IoUringConstants.EnterRegisteredRing) != 0)
                {
                    DisableRegisteredRingFd();
                    waitEnterFlags &= ~IoUringConstants.EnterRegisteredRing;
                    waitRingFd = _managedRingFd;
                    err = Interop.Sys.IoUringShimEnterExt(
                        waitRingFd, submitCount, minComplete, waitEnterFlags, &extArg, &result);
                }

                if (err == Interop.Error.SUCCESS)
                {
                    UpdateManagedPendingSubmissionCountAfterEnter(submitCount, result);
                }
            }
            else
            {
                Debug.Assert(
                    false,
                    "Non-EXT_ARG io_uring wait fallback is unexpected on supported kernels (>= 6.1).");
                uint waitEnterFlags = enterFlags;
                int waitRingFd = ringFd;
                int result;
                err = Interop.Sys.IoUringShimEnter(
                    waitRingFd, submitCount, minComplete, waitEnterFlags, &result);
                if (err == Interop.Error.EINVAL && (waitEnterFlags & IoUringConstants.EnterRegisteredRing) != 0)
                {
                    DisableRegisteredRingFd();
                    waitEnterFlags &= ~IoUringConstants.EnterRegisteredRing;
                    waitRingFd = _managedRingFd;
                    err = Interop.Sys.IoUringShimEnter(
                        waitRingFd, submitCount, minComplete, waitEnterFlags, &result);
                }

                if (err == Interop.Error.SUCCESS)
                {
                    UpdateManagedPendingSubmissionCountAfterEnter(submitCount, result);
                }
            }

            // Drain after waking. If a producer signalled during the drain, re-drain once
            // to pick up any work enqueued between the CQE read and the generation check.
            // Re-snapshot per iteration to avoid unbounded spin when producers are active.
            hadCqes = false;
            uint wakeGenCurrent;
            do
            {
                wakeGenCurrent = Volatile.Read(ref _ioUringWakeupGeneration);
                hadCqes |= DrainCqeRingBatch(handler);
            }
            while (Volatile.Read(ref _ioUringWakeupGeneration) != wakeGenCurrent);
            // CQE dispatch can inline-prepare follow-up SQEs (for example partial send/recv
            // resubmissions). Submit them immediately to avoid an extra event-loop turn before
            // they reach the kernel, which can otherwise amplify backpressure latency.
            if (_ioUringManagedPendingSubmissions != 0)
            {
                Interop.Error inlineSubmitError = SubmitIoUringOperationsNormalized();
                if (inlineSubmitError != Interop.Error.SUCCESS)
                {
                    ThrowInternalException(inlineSubmitError);
                }
            }

            numCompletions = hadCqes ? 1 : 0;
            numEvents = 0;
            waitHandled = true;
            err = Interop.Error.SUCCESS;
        }

        /// <summary>Polls diagnostics after each event loop iteration.</summary>
        partial void LinuxEventLoopAfterIteration()
        {
            PollIoUringDiagnosticsIfNeeded(force: false);
            TrySweepStaleTrackedIoUringOperationsAfterCqOverflowRecovery();
        }

        /// <summary>Queued request to arm a multishot accept SQE for a SO_REUSEPORT shadow listener on this engine.</summary>
        private readonly struct ReusePortShadowSetupRequest
        {
            public readonly SafeSocketHandle ShadowSocket;
            public readonly SocketAsyncContext PrimaryContext;
            public readonly SocketAsyncEngine PrimaryEngine;

            public ReusePortShadowSetupRequest(SafeSocketHandle shadowSocket, SocketAsyncContext primaryContext, SocketAsyncEngine primaryEngine)
            {
                ShadowSocket = shadowSocket;
                PrimaryContext = primaryContext;
                PrimaryEngine = primaryEngine;
            }
        }

        private MpscQueue<ReusePortShadowSetupRequest>? _reusePortShadowSetupQueue;

        /// <summary>
        /// Enqueues a shadow listener setup request for deferred processing on this engine's event loop.
        /// The shadow accept SQE will be armed during the next <see cref="SubmitIoUringBatch"/> cycle.
        /// </summary>
        internal bool TryEnqueueReusePortShadowSetup(SafeSocketHandle shadowSocket, SocketAsyncContext primaryContext, SocketAsyncEngine primaryEngine)
        {
            if (!_ioUringCapabilities.IsCompletionMode || Volatile.Read(ref _ioUringTeardownInitiated) != 0)
            {
                return false;
            }

            MpscQueue<ReusePortShadowSetupRequest>? queue = _reusePortShadowSetupQueue;
            if (queue is null)
            {
                return false;
            }

            if (!queue.TryEnqueue(new ReusePortShadowSetupRequest(shadowSocket, primaryContext, primaryEngine)))
            {
                return false;
            }

            WakeEventLoop();
            return true;
        }

        /// <summary>Queued work item pairing an operation with its prepare sequence number for deferred SQE preparation.</summary>
        private readonly struct IoUringPrepareWorkItem
        {
            /// <summary>The operation to prepare.</summary>
            public readonly SocketAsyncContext.AsyncOperation Operation;
            /// <summary>The sequence number that must match for the preparation to proceed.</summary>
            public readonly long PrepareSequence;

            /// <summary>Creates a work item pairing an operation with its prepare sequence number.</summary>
            public IoUringPrepareWorkItem(SocketAsyncContext.AsyncOperation operation, long prepareSequence)
            {
                Operation = operation;
                PrepareSequence = prepareSequence;
            }
        }

        /// <summary>Enqueues an operation for deferred SQE preparation on the event loop thread.</summary>
        internal bool TryEnqueueIoUringPreparation(SocketAsyncContext.AsyncOperation operation, long prepareSequence)
        {
            if (!_ioUringCapabilities.IsCompletionMode || Volatile.Read(ref _ioUringTeardownInitiated) != 0)
            {
                return false;
            }

            MpscQueue<IoUringPrepareWorkItem>? prepareQueue = _ioUringPrepareQueue;
            if (prepareQueue is null)
            {
                return false;
            }

            long queueLength = Interlocked.Increment(ref _ioUringPrepareQueueLength);
            if (queueLength > s_ioUringPrepareQueueCapacity)
            {
                Interlocked.Decrement(ref _ioUringPrepareQueueLength);
                Interlocked.Increment(ref _ioUringPrepareQueueOverflowCount);

                return false;
            }

            if (!prepareQueue.TryEnqueue(new IoUringPrepareWorkItem(operation, prepareSequence)))
            {
                Interlocked.Decrement(ref _ioUringPrepareQueueLength);
                Interlocked.Increment(ref _ioUringPrepareQueueOverflowCount);

                return false;
            }

            WakeEventLoop();
            return true;
        }

        /// <summary>Extracts completion-slot index and generation from tracked reserved-completion user_data.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryDecodeTrackedIoUringUserData(ulong userData, out int slotIndex, out ulong generation)
        {
            generation = 0;
            slotIndex = 0;
            if (userData == 0)
            {
                return false;
            }

            if ((byte)(userData >> IoUringUserDataTagShift) != IoUringConstants.TagReservedCompletion)
            {
                return false;
            }

            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return false;
            }

            ulong payload = userData & IoUringUserDataPayloadMask;
            slotIndex = DecodeCompletionSlotIndex(payload);
            if ((uint)slotIndex >= (uint)completionEntries.Length)
            {
                return false;
            }

            generation = (payload >> IoUringConstants.SlotIndexBits) & IoUringConstants.GenerationMask;
            return true;
        }

        /// <summary>Atomically removes and returns the tracked operation matching the user_data and generation.</summary>
        private bool TryTakeTrackedIoUringOperation(ulong userData, out SocketAsyncContext.AsyncOperation? operation)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "TryTakeTrackedIoUringOperation must run on the event-loop thread.");
            operation = null;
            if (!TryDecodeTrackedIoUringUserData(userData, out int slotIndex, out ulong generation))
            {
                return false;
            }

            ref IoUringTrackedOperationState entry = ref _trackedOperations![slotIndex];
            while (true)
            {
                SocketAsyncContext.AsyncOperation? currentOperation = Volatile.Read(ref entry.TrackedOperation);
                if (currentOperation is null)
                {
                    return false;
                }

                // Writers publish generation before operation; if operation is visible here,
                // generation must match unless this CQE belongs to an older slot incarnation.
                if (Volatile.Read(ref entry.TrackedOperationGeneration) != generation)
                {
                    return false;
                }

                // Single-owner handoff: exactly one completion-side CAS can null out TrackedOperation
                // for this slot incarnation. A racing replace may swap references, but cannot create
                // two winners for the same user_data token.
                if (Interlocked.CompareExchange(ref entry.TrackedOperation, null, currentOperation) != currentOperation)
                {
                    continue;
                }

                // Reset generation to zero so TryReattachTrackedIoUringOperation (used by
                // SEND_ZC to re-register while awaiting the NOTIF CQE) can CAS from 0 to
                // the new generation. Volatile.Write ensures visibility on ARM64 before the
                // count decrement below, preventing a concurrent TryTrack from observing
                // TrackedOperation == null with a stale non-zero generation.
                Volatile.Write(ref entry.TrackedOperationGeneration, 0UL);
                DecrementTrackedIoUringOperationCountOnEventLoop();
                operation = currentOperation;
                return true;
            }
        }

        /// <summary>Returns the tracked operation for the given user_data without untracking it.</summary>
        private bool TryGetTrackedIoUringOperation(ulong userData, out SocketAsyncContext.AsyncOperation? operation)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "TryGetTrackedIoUringOperation must run on the event-loop thread.");
            operation = null;
            if (!TryDecodeTrackedIoUringUserData(userData, out int slotIndex, out ulong generation))
            {
                return false;
            }

            ref IoUringTrackedOperationState entry = ref _trackedOperations![slotIndex];
            SocketAsyncContext.AsyncOperation? currentOperation = Volatile.Read(ref entry.TrackedOperation);
            if (currentOperation is null)
            {
                return false;
            }

            if (Volatile.Read(ref entry.TrackedOperationGeneration) != generation)
            {
                return false;
            }

            operation = currentOperation;
            return true;
        }

        /// <summary>Returns whether an operation with the given user_data and generation is currently tracked.</summary>
        private bool ContainsTrackedIoUringOperation(ulong userData)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "ContainsTrackedIoUringOperation must run on the event-loop thread.");
            return TryGetTrackedIoUringOperation(userData, out _);
        }

        /// <summary>Re-attaches a completion owner after dispatch-side deferral (for example SEND_ZC waiting on NOTIF CQE).</summary>
        private bool TryReattachTrackedIoUringOperation(ulong userData, SocketAsyncContext.AsyncOperation operation)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "TryReattachTrackedIoUringOperation must run on the event-loop thread.");
            if (!TryDecodeTrackedIoUringUserData(userData, out int slotIndex, out ulong generation))
            {
                return false;
            }

            // Verify the completion slot is still in the expected SEND_ZC NOTIF-pending state
            // before attempting to reattach. If the slot was freed and reallocated between the
            // first CQE dispatch and this reattach call, the slot's state will not match.
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null || (uint)slotIndex >= (uint)completionEntries.Length)
            {
                return false;
            }

            ref IoUringCompletionSlot slot = ref completionEntries[slotIndex];
            if (!slot.IsZeroCopySend || !slot.ZeroCopyNotificationPending || slot.Generation != generation)
            {
                // Slot was freed and possibly reallocated. The NOTIF CQE was either already
                // processed or will be discarded by HandleZeroCopyNotification's generation check.
                return false;
            }

            ref IoUringTrackedOperationState entry = ref _trackedOperations![slotIndex];
            if (Interlocked.CompareExchange(ref entry.TrackedOperationGeneration, generation, 0) != 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref entry.TrackedOperation, operation, null) is not null)
            {
                Volatile.Write(ref entry.TrackedOperationGeneration, 0);
                return false;
            }

            IncrementTrackedIoUringOperationCountOnEventLoop();
            return true;
        }

        /// <summary>Atomically replaces the tracked operation for the given user_data.</summary>
        private bool TryReplaceTrackedIoUringOperation(ulong userData, SocketAsyncContext.AsyncOperation newOperation)
        {
            if (!TryDecodeTrackedIoUringUserData(userData, out int slotIndex, out ulong generation))
            {
                return false;
            }

            ref IoUringTrackedOperationState entry = ref _trackedOperations![slotIndex];
            while (true)
            {
                SocketAsyncContext.AsyncOperation? currentOperation = Volatile.Read(ref entry.TrackedOperation);
                if (currentOperation is null)
                {
                    return false;
                }

                if (Volatile.Read(ref entry.TrackedOperationGeneration) != generation)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref entry.TrackedOperation, newOperation, currentOperation) == currentOperation)
                {
                    return true;
                }
            }
        }

        /// <summary>Removes a tracked operation, optionally verifying it matches an expected reference.</summary>
        private IoUringTrackedOperationRemoveResult TryUntrackTrackedIoUringOperation(
            ulong userData,
            SocketAsyncContext.AsyncOperation? expectedOperation,
            out SocketAsyncContext.AsyncOperation? removedOperation)
        {
            removedOperation = null;
            if (!TryDecodeTrackedIoUringUserData(userData, out int slotIndex, out ulong generation))
            {
                return IoUringTrackedOperationRemoveResult.NotFound;
            }

            ref IoUringTrackedOperationState entry = ref _trackedOperations![slotIndex];
            while (true)
            {
                SocketAsyncContext.AsyncOperation? currentOperation = Volatile.Read(ref entry.TrackedOperation);
                if (currentOperation is null)
                {
                    return IoUringTrackedOperationRemoveResult.NotFound;
                }

                if (Volatile.Read(ref entry.TrackedOperationGeneration) != generation)
                {
                    return IoUringTrackedOperationRemoveResult.NotFound;
                }

                if (expectedOperation is not null && !ReferenceEquals(currentOperation, expectedOperation))
                {
                    return IoUringTrackedOperationRemoveResult.Mismatch;
                }

                if (Interlocked.CompareExchange(ref entry.TrackedOperation, null, currentOperation) != currentOperation)
                {
                    continue;
                }

                // Volatile.Write ensures the generation reset is visible on ARM64 before
                // the count decrement. This method runs from worker threads (cancellation),
                // and a plain store could reorder past Interlocked.Decrement, leaving a
                // window where the event loop sees TrackedOperation == null but generation != 0.
                Volatile.Write(ref entry.TrackedOperationGeneration, 0UL);
                Interlocked.Decrement(ref _trackedIoUringOperationCount);
                removedOperation = currentOperation;
                AssertIoUringLifecycleTransition(
                    IoUringOperationLifecycleState.Submitted,
                    IoUringOperationLifecycleState.Canceled);
                return IoUringTrackedOperationRemoveResult.Removed;
            }
        }

        /// <summary>Returns true when no io_uring operations are currently tracked.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsIoUringTrackingEmpty() =>
            Volatile.Read(ref _trackedIoUringOperationCount) == 0;

        private void IncrementTrackedIoUringOperationCountOnEventLoop()
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "Tracked-operation increments must run on the event-loop thread.");
            int nextCount = _trackedIoUringOperationCount + 1;
            Volatile.Write(ref _trackedIoUringOperationCount, nextCount);
        }

        private void DecrementTrackedIoUringOperationCountOnEventLoop()
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "Tracked-operation decrements must run on the event-loop thread.");
            int nextCount = _trackedIoUringOperationCount - 1;
            Debug.Assert(nextCount >= 0, "Tracked-operation count underflow.");
            Volatile.Write(ref _trackedIoUringOperationCount, nextCount);
        }

        /// <summary>Removes an operation from completion-slot tracking, logging on mismatch.</summary>
        internal bool TryUntrackIoUringOperation(ulong userData, SocketAsyncContext.AsyncOperation? expectedOperation = null)
        {
            IoUringTrackedOperationRemoveResult removeResult = TryUntrackTrackedIoUringOperation(userData, expectedOperation, out _);
            if (removeResult == IoUringTrackedOperationRemoveResult.Mismatch)
            {
                Debug.Fail("io_uring tracked operation mismatch while untracking user_data.");
                Interlocked.Increment(ref _ioUringUntrackMismatchCount);

                return false;
            }

            return true;
        }

        /// <summary>Attempts to replace the currently tracked operation for an existing user_data slot.</summary>
        internal bool TryReplaceIoUringTrackedOperation(ulong userData, SocketAsyncContext.AsyncOperation newOperation)
        {
            // Replacement keeps the same slot+generation token; completion ownership is still
            // resolved by the CompareExchange gate in TryTakeTrackedIoUringOperation.
            return TryReplaceTrackedIoUringOperation(userData, newOperation);
        }

        /// <summary>Enqueues a user_data for ASYNC_CANCEL on the event loop thread.</summary>
        private bool TryEnqueueIoUringCancellation(ulong userData)
        {
            if (!_ioUringCapabilities.IsCompletionMode || userData == 0 || Volatile.Read(ref _ioUringTeardownInitiated) != 0)
            {
                return false;
            }

            MpscQueue<ulong>? cancelQueue = _ioUringCancelQueue;
            if (cancelQueue is null)
            {
                return false;
            }

            // First attempt: enqueue directly.
            long queueLength = Interlocked.Increment(ref _ioUringCancelQueueLength);
            if (queueLength <= s_ioUringCancellationQueueCapacity)
            {
                if (cancelQueue.TryEnqueue(userData))
                {
                    return true;
                }

                Interlocked.Decrement(ref _ioUringCancelQueueLength);
            }
            else
            {
                Interlocked.Decrement(ref _ioUringCancelQueueLength);
            }

            // Queue-full can be transient under cancellation bursts. Wake the event loop and retry once.
#if DEBUG
            // Keep a dedicated test counter so functional tests can verify the wake-and-retry path.
            Interlocked.Increment(ref _testCancelQueueWakeRetryCount);
#endif
            WakeEventLoop();
            // Retry while SpinWait remains in active-spin mode; once it would yield, take slow-path accounting.
            SpinWait retryBackoff = default;
            do
            {
                retryBackoff.SpinOnce();

                queueLength = Interlocked.Increment(ref _ioUringCancelQueueLength);
                if (queueLength <= s_ioUringCancellationQueueCapacity)
                {
                    if (cancelQueue.TryEnqueue(userData))
                    {
                        return true;
                    }

                    Interlocked.Decrement(ref _ioUringCancelQueueLength);
                    continue;
                }

                Interlocked.Decrement(ref _ioUringCancelQueueLength);
            } while (!retryBackoff.NextSpinWillYield);

            Interlocked.Increment(ref _ioUringCancelQueueOverflowCount);
            SocketsTelemetry.Log.IoUringCancellationQueueOverflow();

            return false;
        }

        /// <summary>Writes an ASYNC_CANCEL SQE directly if the engine is on the event loop thread.</summary>
        private bool TryQueueIoUringAsyncCancel(ulong userData)
        {
            if (!_ioUringCapabilities.IsIoUringPort || userData == 0)
            {
                return false;
            }

            if (!TryAcquireManagedSqeWithRetry(out IoUringSqe* sqe, out _))
            {
                return false;
            }

            WriteAsyncCancelSqe(sqe, userData);
            return true;
        }

        /// <summary>Writes to the eventfd to wake the event loop from a blocking wait.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Interop.Error ManagedWakeEventLoop()
        {
            return Interop.Sys.IoUringShimWriteEventFd(_managedWakeupEventFd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetManagedCompletionWaitTimeoutNanos()
        {
            return Volatile.Read(ref _ioUringWakeFailureConsecutiveCount) >= IoUringWakeFailureCircuitBreakerThreshold
                ? IoUringConstants.WakeFailureFallbackWaitTimeoutNanos
                : IoUringConstants.BoundedWaitTimeoutNanos;
        }

        /// <summary>Sends a wake signal to the event loop thread.</summary>
        /// <remarks>
        /// Write coalescing is handled by the kernel's eventfd mechanism: multiple write(1)
        /// calls accumulate in the counter, read() drains it, and the multishot POLL_ADD on
        /// the eventfd fires once per 0-to-nonzero transition. No application-level
        /// coalescing is needed.
        /// </remarks>
        private void WakeEventLoop()
        {
            if (!_ioUringCapabilities.IsCompletionMode || Volatile.Read(ref _ioUringTeardownInitiated) != 0)
            {
                return;
            }

            // Advance the wakeup generation so the event loop's post-wake drain loop
            // detects work enqueued during the blocking syscall.
            Interlocked.Increment(ref _ioUringWakeupGeneration);

            Interop.Error error = ManagedWakeEventLoop();
            if (error == Interop.Error.SUCCESS)
            {
                Interlocked.Exchange(ref _ioUringWakeFailureConsecutiveCount, 0);

                return;
            }

            Interlocked.Increment(ref _ioUringWakeFailureConsecutiveCount);
        }

        /// <summary>
        /// Wakes the io_uring event loop to process deferred cancel CQEs produced by
        /// shutdown/disconnect during SafeSocketHandle.CloseAsIs. With DEFER_TASKRUN,
        /// these CQEs are queued as task work and only processed during io_uring_enter.
        /// </summary>
        partial void LinuxWakeIoUringEventLoopForSocketClose()
        {
            WakeEventLoop();
        }

        /// <summary>Enqueues a cancellation request and wakes the event loop.</summary>
        internal void TryRequestIoUringCancellation(ulong userData)
        {
            if (!TryEnqueueIoUringCancellation(userData))
            {
                return;
            }

            WakeEventLoop();
        }

        /// <summary>
        /// Enqueues a readiness fallback event when io_uring submission is congested.
        /// </summary>
        internal void EnqueueReadinessFallbackEvent(
            SocketAsyncContext context,
            Interop.Sys.SocketEvents events,
            bool countAsPrepareQueueOverflowFallback = false)
        {
            if (events == Interop.Sys.SocketEvents.None)
            {
                return;
            }

            _eventQueue.Enqueue(new SocketIOEvent(context, events));
            if (countAsPrepareQueueOverflowFallback)
            {
                RecordIoUringPrepareQueueOverflowFallback();
            }
            EnsureWorkerScheduled();
        }

        /// <summary>Drains queued cancellation requests into ASYNC_CANCEL SQEs.</summary>
        private bool DrainIoUringCancellationQueue()
        {
            MpscQueue<ulong>? cancelQueue = _ioUringCancelQueue;
            if (cancelQueue is null)
            {
                return false;
            }

            int cancelDrainBudget = GetAdaptiveIoUringCancelQueueDrainBudget();
            bool preparedSqe = false;
            for (int drained = 0; drained < cancelDrainBudget &&
                cancelQueue.TryDequeue(out ulong userData); drained++)
            {
                long remainingLength = Interlocked.Decrement(ref _ioUringCancelQueueLength);
                Debug.Assert(remainingLength >= 0);

                // Cancellation requests can race with terminal completion/untracking.
                // Skip stale requests to avoid issuing known -ENOENT async-cancel SQEs.
                if (!IsTrackedIoUringOperation(userData))
                {
                    continue;
                }

                if (TryQueueIoUringAsyncCancel(userData))
                {
                    preparedSqe = true;
                }
            }
            return preparedSqe;
        }

        /// <summary>Drains queued SO_REUSEPORT shadow listener setup requests, arming multishot accept SQEs.</summary>
        private bool DrainReusePortShadowSetupQueue()
        {
            MpscQueue<ReusePortShadowSetupRequest>? queue = _reusePortShadowSetupQueue;
            if (queue is null)
            {
                return false;
            }

            bool preparedSqe = false;
            while (queue.TryDequeue(out ReusePortShadowSetupRequest request))
            {
                if (TryPrepareReusePortMultishotAccept(
                    request.ShadowSocket,
                    request.PrimaryContext,
                    request.PrimaryEngine,
                    out ulong userData))
                {
                    preparedSqe = true;
                    request.PrimaryContext.RecordReusePortShadowArmed(userData, _engineIndex);
                }
            }
            return preparedSqe;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetAdaptiveIoUringPrepareQueueDrainBudget()
        {
            long observedLength = Interlocked.Read(ref _ioUringPrepareQueueLength);
            return ComputeAdaptiveIoUringDrainBudget(
                observedLength,
                MinIoUringPrepareQueueDrainPerSubmit,
                MaxIoUringPrepareQueueDrainPerSubmit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetAdaptiveIoUringCancelQueueDrainBudget()
        {
            long observedLength = Interlocked.Read(ref _ioUringCancelQueueLength);
            return ComputeAdaptiveIoUringDrainBudget(
                observedLength,
                MinIoUringCancelQueueDrainPerSubmit,
                MaxIoUringCancelQueueDrainPerSubmit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeAdaptiveIoUringDrainBudget(long observedLength, int minBudget, int maxBudget)
        {
            if (observedLength <= minBudget)
            {
                return minBudget;
            }

            if (observedLength >= maxBudget)
            {
                return maxBudget;
            }

            return (int)observedLength;
        }

        /// <summary>Drains both prepare and cancel queues, then submits all pending SQEs.</summary>
        private Interop.Error SubmitIoUringBatch()
        {
            if (!_ioUringCapabilities.IsIoUringPort)
            {
                return Interop.Error.SUCCESS;
            }

            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "SubmitIoUringBatch must only be called from the event loop thread (SINGLE_ISSUER contract).");
            bool preparedSqe = false;
            if (_ioUringCapabilities.IsCompletionMode)
            {
                preparedSqe |= DrainIoUringCancellationQueue();
                preparedSqe |= DrainReusePortShadowSetupQueue();

                MpscQueue<IoUringPrepareWorkItem>? prepareQueue = _ioUringPrepareQueue;
                if (prepareQueue is null)
                {
                    ThrowInternalException("io_uring invariant violation: prepare queue is null while engine is in completion mode");
                }

                int prepareDrainBudget = GetAdaptiveIoUringPrepareQueueDrainBudget();
                for (int drained = 0; drained < prepareDrainBudget &&
                    prepareQueue.TryDequeue(out IoUringPrepareWorkItem workItem); drained++)
                {
                    long remainingLength = Interlocked.Decrement(ref _ioUringPrepareQueueLength);
                    Debug.Assert(remainingLength >= 0);
                    Interop.Error prepareError = TryPrepareAndTrackIoUringOperation(
                        workItem.Operation,
                        workItem.PrepareSequence,
                        out bool preparedOperation);
                    if (prepareError != Interop.Error.SUCCESS)
                    {
                        return prepareError;
                    }

                    preparedSqe |= preparedOperation;

                    if (!preparedOperation && workItem.Operation.IsInCompletedState())
                    {
                        // Operation completed from early-buffer data during prepare (no SQE needed).
                        // Dispatch the completion callback now.
                        workItem.Operation.AssociatedContext.TryCompleteIoUringOperation(workItem.Operation);
                        continue;
                    }

                    if (!preparedOperation && workItem.Operation.IsInWaitingState())
                    {
                        // In completion mode, keep retries in the io_uring prepare queue.
                        // Synthetic readiness fallback can self-amplify into hot loops that
                        // do not produce kernel send/recv progress.
                        bool requeued = workItem.Operation.TryQueueIoUringPreparation();
                        if (requeued)
                        {
                            continue;
                        }

                        workItem.Operation.ResetIoUringSlotExhaustionRetryCount();
                        WakeEventLoop();
                        continue;
                    }
                }

            }

            if (!preparedSqe)
            {
                // Inline re-prepare paths can write SQEs outside queue drains; ensure they are submitted.
                if (_ioUringManagedPendingSubmissions != 0)
                {
                    return SubmitIoUringOperationsNormalized();
                }

                if ((_ioUringCancelQueue?.IsEmpty == false) || (_ioUringPrepareQueue?.IsEmpty == false))
                {
                    WakeEventLoop();
                }

                return Interop.Error.SUCCESS;
            }

            return SubmitIoUringOperationsNormalized();
        }

        /// <summary>
        /// Prepares an operation for io_uring submission and tracks it in completion-slot metadata.
        /// On non-prepared paths, clears operation user_data and releases preparation resources.
        /// </summary>
        private Interop.Error TryPrepareAndTrackIoUringOperation(
            SocketAsyncContext.AsyncOperation operation,
            long prepareSequence,
            out bool preparedSqe)
        {
            preparedSqe = false;

            bool prepared = operation.TryPrepareIoUring(operation.AssociatedContext, prepareSequence);
            if (prepared)
            {
                AssertIoUringLifecycleTransition(
                    IoUringOperationLifecycleState.Queued,
                    IoUringOperationLifecycleState.Prepared);
            }

            if (prepared && operation.ErrorCode == SocketError.Success)
            {
                preparedSqe = true;
                if (!TryTrackPreparedIoUringOperation(operation))
                {
                    // Invariant violation: tracking collision after prepare.
                    // A prepared SQE may now complete without a managed owner; do not attempt best-effort recovery.
                    operation.ClearIoUringUserData();
                    ThrowInternalException("io_uring tracking collision: prepared SQE could not be tracked by user_data");
                }

                return Interop.Error.SUCCESS;
            }

            if (prepared)
            {
                AssertIoUringLifecycleTransition(
                    IoUringOperationLifecycleState.Prepared,
                    IoUringOperationLifecycleState.Detached);
            }

            if (!TryUntrackIoUringOperation(operation.IoUringUserData, operation))
            {
                // Mismatch indicates token ownership confusion; avoid releasing
                // resources that may still be associated with another tracked op.
                ThrowInternalException("io_uring untrack mismatch: token ownership confusion during prepare cleanup");
            }

            operation.ClearIoUringUserData();
            return Interop.Error.SUCCESS;
        }

        /// <summary>
        /// Falls back to readiness notification for an operation that remained waiting after a failed prepare attempt.
        /// </summary>
        private void EmitReadinessFallbackForUnpreparedOperation(SocketAsyncContext.AsyncOperation operation)
        {
            operation.ClearIoUringUserData();
            Interop.Sys.SocketEvents fallbackEvents = operation.GetIoUringFallbackSocketEvents();
            if (fallbackEvents == Interop.Sys.SocketEvents.None)
            {
                return;
            }

            operation.RequestIoUringFallbackReprepare();
            EnqueueReadinessFallbackEvent(operation.AssociatedContext, fallbackEvents);
        }

        /// <summary>Registers a prepared operation in completion-slot metadata.</summary>
        private bool TryTrackPreparedIoUringOperation(SocketAsyncContext.AsyncOperation operation)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "TryTrackPreparedIoUringOperation must run on the event-loop thread.");
            if (!TryDecodeTrackedIoUringUserData(operation.IoUringUserData, out int slotIndex, out ulong generation))
            {
                return false;
            }

            ref IoUringTrackedOperationState entry = ref _trackedOperations![slotIndex];
            if (Volatile.Read(ref entry.TrackedOperationGeneration) == 0 &&
                Volatile.Read(ref entry.TrackedOperation) is null)
            {
                // Publish generation before operation so readers never observe a new
                // operation paired with a stale generation on weakly-ordered CPUs.
                Volatile.Write(ref entry.TrackedOperationGeneration, generation);
                Volatile.Write(ref entry.TrackedOperation, operation);
                IncrementTrackedIoUringOperationCountOnEventLoop();
                AssertIoUringLifecycleTransition(
                    IoUringOperationLifecycleState.Prepared,
                    IoUringOperationLifecycleState.Submitted);
                return true;
            }

            if (Volatile.Read(ref entry.TrackedOperation) is null &&
                Volatile.Read(ref entry.TrackedOperationGeneration) == generation)
            {
                Volatile.Write(ref entry.TrackedOperationGeneration, 0);
            }

            // Persistent multishot receive can rebind an existing tracked user_data to a new
            // managed operation before this call. In that case, tracking is already satisfied.
            return operation.IoUringUserData != 0 &&
                TryGetTrackedIoUringOperation(operation.IoUringUserData, out SocketAsyncContext.AsyncOperation? trackedOperation) &&
                ReferenceEquals(trackedOperation, operation);
        }

        /// <summary>Returns whether the given user_data is currently tracked.</summary>
        private bool IsTrackedIoUringOperation(ulong userData)
        {
            return ContainsTrackedIoUringOperation(userData);
        }

        /// <summary>Returns whether current completion-slot usage indicates likely slot exhaustion pressure.</summary>
        private bool IsPotentialCompletionSlotExhaustion()
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null || completionEntries.Length == 0)
            {
                return false;
            }

            int threshold = Math.Max(0, completionEntries.Length - 16);
            return _completionSlotsInUse >= threshold;
        }

        /// <summary>Debug assertion that tracked completion-slot usage never exceeds pool bounds.</summary>
        [Conditional("DEBUG")]
        private void AssertCompletionSlotUsageBounded()
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                Debug.Assert(
                    _completionSlotsInUse == 0,
                    "Completion slot usage must be zero when the slot pool is not allocated.");
                return;
            }

            Debug.Assert(
                _completionSlotsInUse >= 0 && _completionSlotsInUse <= completionEntries.Length,
                $"Completion slot usage out of bounds: inUse={_completionSlotsInUse}, capacity={completionEntries.Length}.");
        }

        /// <summary>Debug assertion that completion-slot free-list topology matches <see cref="_completionSlotsInUse"/>.</summary>
        [Conditional("DEBUG")]
        private void AssertCompletionSlotPoolConsistency()
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                Debug.Assert(_completionSlotsInUse == 0, "Completion slot usage must be zero when slots are not allocated.");
                Debug.Assert(_completionSlotFreeListHead == -1, "Free-list head must be reset when slots are not allocated.");
                return;
            }

            bool[] visited = new bool[completionEntries.Length];
            int freeCount = 0;
            int current = _completionSlotFreeListHead;
            while (current >= 0)
            {
                Debug.Assert(
                    (uint)current < (uint)completionEntries.Length,
                    $"Completion-slot free-list index out of range: {current}.");
                if ((uint)current >= (uint)completionEntries.Length || visited[current])
                {
                    break;
                }

                visited[current] = true;
                freeCount++;
                current = completionEntries[current].FreeListNext;
            }

            int expectedInUse = completionEntries.Length - freeCount;
            Debug.Assert(
                expectedInUse == _completionSlotsInUse,
                $"Completion-slot accounting mismatch: expected in-use={expectedInUse}, actual in-use={_completionSlotsInUse}, free={freeCount}, capacity={completionEntries.Length}.");
        }

        /// <summary>Returns whether the calling thread is the event loop thread.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCurrentThreadEventLoopThread() =>
            Volatile.Read(ref _eventLoopManagedThreadId) == Environment.CurrentManagedThreadId;

        /// <summary>Disables the registered ring fd after an EINVAL and falls back to the raw ring fd.</summary>
        private void DisableRegisteredRingFd()
        {
            _ioUringSqRingInfo.RegisteredRingFd = -1;
        }

        /// <summary>
        /// Completes rejected-but-published SQEs as failed completions so ignored submit
        /// errors do not re-queue the same work indefinitely.
        /// </summary>
        private unsafe void DrainRejectedManagedSqesAsFailedCompletions(uint rejectedSubmitCount, Interop.Error submitError)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "DrainRejectedManagedSqesAsFailedCompletions must run on the event-loop thread.");
            if (rejectedSubmitCount == 0)
            {
                return;
            }

            ref Interop.Sys.IoUringSqRingInfo ringInfo = ref _ioUringSqRingInfo;
            if (ringInfo.SqeBase == IntPtr.Zero || ringInfo.SqEntries == 0 || ringInfo.SqeSize < (uint)sizeof(IoUringSqe))
            {
                return;
            }

            int completionResult = -Interop.Sys.ConvertErrorPalToPlatform(submitError);
            uint firstRejectedSqTail = _ioUringManagedSqTail - rejectedSubmitCount;
            SocketEventHandler handler = new SocketEventHandler(this);
            bool enqueuedFallbackEvent = false;

            for (uint i = 0; i < rejectedSubmitCount; i++)
            {
                uint sqTail = firstRejectedSqTail + i;
                uint ringIndex = sqTail & ringInfo.SqMask;
                nint sqeOffset = checked((nint)((nuint)ringIndex * ringInfo.SqeSize));
                IoUringSqe* sqe = (IoUringSqe*)((byte*)ringInfo.SqeBase + sqeOffset);
                ulong sqeUserData = sqe->UserData;
                byte tag = (byte)(sqeUserData >> IoUringUserDataTagShift);

                if (tag == IoUringConstants.TagReservedCompletion)
                {
                    int sqeCompletionResult = completionResult;
                    ulong payload = sqeUserData & IoUringUserDataPayloadMask;
                    ResolveReservedCompletionSlotMetadata(
                        payload,
                        isMultishotCompletion: false,
                        ref sqeCompletionResult,
                        out int completionSocketAddressLen,
                        out int completionControlBufferLen,
                        out uint completionAuxiliaryData,
                        out bool hasFixedRecvBuffer,
                        out ushort fixedRecvBufferId,
                        out bool shouldFreeSlot);

                    handler.DispatchSingleIoUringCompletion(
                        sqeUserData,
                        sqeCompletionResult,
                        flags: 0,
                        socketAddressLen: completionSocketAddressLen,
                        controlBufferLen: completionControlBufferLen,
                        auxiliaryData: completionAuxiliaryData,
                        hasFixedRecvBuffer: hasFixedRecvBuffer,
                        fixedRecvBufferId: fixedRecvBufferId,
                        ref enqueuedFallbackEvent);

                    // Mirror the normal CQE dispatch ownership model: free slot only
                    // after dispatch has taken/reconciled tracked operation ownership.
                    if (shouldFreeSlot)
                    {
                        FreeCompletionSlot(DecodeCompletionSlotIndex(payload));
                    }
                }
                else if (tag != IoUringConstants.TagNone && tag != IoUringConstants.TagWakeupSignal)
                {
                    Debug.Fail($"Unexpected io_uring SQE user_data tag on rejected submit drain: {tag}.");
                }
            }

            if (enqueuedFallbackEvent)
            {
                EnsureWorkerScheduled();
            }
        }

        /// <summary>Returns the accepted SQE count from an io_uring_enter result, clamped to the requested submit count.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ComputeAcceptedSubmissionCount(uint requestedSubmitCount, int enterResult)
        {
            if (requestedSubmitCount == 0 || enterResult <= 0)
            {
                return 0;
            }

            uint acceptedSubmitCount = (uint)enterResult;
            return acceptedSubmitCount <= requestedSubmitCount ? acceptedSubmitCount : requestedSubmitCount;
        }

        /// <summary>Updates pending-submission accounting after an io_uring_enter wait call.</summary>
        private void UpdateManagedPendingSubmissionCountAfterEnter(uint requestedSubmitCount, int enterResult)
        {
            if (_sqPollEnabled)
            {
                // SQPOLL consumes published SQEs asynchronously after wakeup.
                _ioUringManagedPendingSubmissions = 0;
                return;
            }

            uint acceptedSubmitCount = ComputeAcceptedSubmissionCount(requestedSubmitCount, enterResult);
            uint rejectedSubmitCount = requestedSubmitCount - acceptedSubmitCount;
            Debug.Assert(
                acceptedSubmitCount + rejectedSubmitCount == requestedSubmitCount,
                "Partial-submit accounting mismatch in io_uring wait path.");
            _ioUringManagedPendingSubmissions = rejectedSubmitCount;
        }

        /// <summary>Submits the specified number of pending SQEs via io_uring_enter.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe Interop.Error ManagedSubmitPendingEntries(
            uint toSubmit,
            out uint acceptedSubmitCount)
        {
            acceptedSubmitCount = 0;
            if (toSubmit == 0)
            {
                return Interop.Error.SUCCESS;
            }

            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "ManagedSubmitPendingEntries must only be called from the event loop thread (SINGLE_ISSUER contract).");
            if (TryConsumeDebugForcedSubmitError(out Interop.Error forcedSubmitError))
            {
                return forcedSubmitError;
            }

            if (_sqPollEnabled)
            {
                if (!SqNeedWakeup())
                {
                    SocketsTelemetry.Log.IoUringSqPollSubmissionSkipped(toSubmit);
                    acceptedSubmitCount = toSubmit;
                    return Interop.Error.SUCCESS;
                }

                uint wakeupFlags = IoUringConstants.EnterSqWakeup;
                int wakeupRingFd = _managedRingFd;
                if (_ioUringSqRingInfo.RegisteredRingFd >= 0)
                {
                    wakeupFlags |= IoUringConstants.EnterRegisteredRing;
                    wakeupRingFd = _ioUringSqRingInfo.RegisteredRingFd;
                }

                // Wakeup accounting is intentionally optimistic: this counter tracks wake requests
                // issued by managed code, not guaranteed kernel-side SQ consumption.
                SocketsTelemetry.Log.IoUringSqPollWakeup();
                int wakeupResult;
                Interop.Error wakeupError = Interop.Sys.IoUringShimEnter(wakeupRingFd, 0, 0, wakeupFlags, &wakeupResult);
                if (wakeupError == Interop.Error.EINVAL && (wakeupFlags & IoUringConstants.EnterRegisteredRing) != 0)
                {
                    DisableRegisteredRingFd();
                    wakeupFlags &= ~IoUringConstants.EnterRegisteredRing;
                    wakeupRingFd = _managedRingFd;
                    wakeupError = Interop.Sys.IoUringShimEnter(wakeupRingFd, 0, 0, wakeupFlags, &wakeupResult);
                }

                if (wakeupError == Interop.Error.SUCCESS)
                {
                    acceptedSubmitCount = toSubmit;
                }

                return wakeupError;
            }

            uint enterFlags = 0;
            int ringFd = _managedRingFd;
            if (_ioUringSqRingInfo.RegisteredRingFd >= 0)
            {
                enterFlags |= IoUringConstants.EnterRegisteredRing;
                ringFd = _ioUringSqRingInfo.RegisteredRingFd;
            }

            while (toSubmit > 0)
            {
                int result;
                Interop.Error err = Interop.Sys.IoUringShimEnter(ringFd, toSubmit, 0, enterFlags, &result);
                if (err == Interop.Error.EINVAL && (enterFlags & IoUringConstants.EnterRegisteredRing) != 0)
                {
                    DisableRegisteredRingFd();
                    enterFlags &= ~IoUringConstants.EnterRegisteredRing;
                    ringFd = _managedRingFd;
                    err = Interop.Sys.IoUringShimEnter(ringFd, toSubmit, 0, enterFlags, &result);
                }

                if (err != Interop.Error.SUCCESS)
                    return err;

                uint acceptedThisCall = ComputeAcceptedSubmissionCount(toSubmit, result);
                if (acceptedThisCall == 0)
                {
                    return Interop.Error.EAGAIN;
                }

                acceptedSubmitCount += acceptedThisCall;
                toSubmit -= acceptedThisCall;
            }
            return Interop.Error.SUCCESS;
        }

        /// <summary>Computes pending submissions and calls ManagedSubmitPendingEntries.</summary>
        private Interop.Error SubmitIoUringOperationsNormalized()
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "SubmitIoUringOperationsNormalized must only be called from the event loop thread (SINGLE_ISSUER contract).");
            PublishManagedSqeTail();
            uint managedPending = _ioUringManagedPendingSubmissions;
            _ioUringManagedPendingSubmissions = 0;

            Interop.Error error = ManagedSubmitPendingEntries(managedPending, out uint acceptedSubmitCount);
            uint rejectedSubmitCount = managedPending - acceptedSubmitCount;
            Debug.Assert(
                acceptedSubmitCount + rejectedSubmitCount == managedPending,
                "Partial-submit accounting mismatch in io_uring submit path.");

            // EFAULT indicates corrupted SQ ring memory; propagate to FailFast.
            // All other errors drain rejected SQEs as failed completions so individual
            // operations receive error callbacks and the engine survives.
            bool fatalSubmitError = error == Interop.Error.EFAULT;
            if (error != Interop.Error.SUCCESS && rejectedSubmitCount != 0 && !fatalSubmitError)
            {
                DrainRejectedManagedSqesAsFailedCompletions(rejectedSubmitCount, error);
            }

            return fatalSubmitError ? error : Interop.Error.SUCCESS;
        }

        /// <summary>Cancels all queued-but-not-submitted operations during teardown.</summary>
        private void DrainQueuedIoUringOperationsForTeardown()
        {
            MpscQueue<IoUringPrepareWorkItem>? prepareQueue = _ioUringPrepareQueue;
            if (prepareQueue is not null)
            {
                while (prepareQueue.TryDequeue(out IoUringPrepareWorkItem workItem))
                {
                    long remainingLength = Interlocked.Decrement(ref _ioUringPrepareQueueLength);
                    Debug.Assert(remainingLength >= 0);

                    SocketAsyncContext.AsyncOperation operation = workItem.Operation;
                    operation.CancelPendingIoUringPreparation(workItem.PrepareSequence);
                    operation.TryCancelForTeardown();
                    operation.ClearIoUringUserData();
                }
            }

            MpscQueue<ulong>? cancelQueue = _ioUringCancelQueue;
            if (cancelQueue is not null)
            {
                while (cancelQueue.TryDequeue(out _))
                {
                    long remainingLength = Interlocked.Decrement(ref _ioUringCancelQueueLength);
                    Debug.Assert(remainingLength >= 0);
                }
            }

            // No reset needed for generation counter; teardown does not re-enter the wait loop.
        }

        /// <summary>
        /// Cancels all tracked in-flight operations during teardown.
        /// This includes any future long-lived operations (for example multishot recv).
        /// </summary>
        private void DrainTrackedIoUringOperationsForTeardown(bool portClosedForTeardown)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "DrainTrackedIoUringOperationsForTeardown must run on the event-loop thread.");
            if (_completionSlots is null || IsIoUringTrackingEmpty())
            {
                return;
            }

            if (_cqOverflowRecoveryActive)
            {
                // Phase 1 spec branch (b): teardown preempts overflow-recovery ownership;
                // tracked-operation drain/cancel paths become the single shutdown owner.
                _cqOverflowRecoveryBranch = IoUringCqOverflowRecoveryBranch.Teardown;
                _cqOverflowRecoveryActive = false;
            }

            bool queuedAsyncCancel = false;
            bool canPrepareTeardownCancels = !portClosedForTeardown && IsCurrentThreadEventLoopThread();
            IoUringTrackedOperationState[]? trackedOperations = _trackedOperations;
            if (trackedOperations is null)
            {
                return;
            }

            // Teardown uses an explicit array walk to avoid iterator state-machine allocations.
            for (int i = 0; i < trackedOperations.Length; i++)
            {
                SocketAsyncContext.AsyncOperation? operation = Interlocked.Exchange(ref trackedOperations[i].TrackedOperation, null);
                if (operation is null)
                {
                    continue;
                }

                Volatile.Write(ref trackedOperations[i].TrackedOperationGeneration, 0UL);
                DecrementTrackedIoUringOperationCountOnEventLoop();
                AssertIoUringLifecycleTransition(
                    IoUringOperationLifecycleState.Submitted,
                    IoUringOperationLifecycleState.Detached);

                ulong userData = operation.IoUringUserData;
                if (canPrepareTeardownCancels &&
                    TryQueueIoUringAsyncCancel(userData))
                {
                    queuedAsyncCancel = true;
                }

                // Teardown policy: if the port was already closed, native ownership has been
                // detached and it is now safe to release operation-owned resources eagerly.
                // Otherwise, queue best-effort async cancel before releasing resources.
                operation.TryCancelForTeardown();
                operation.ClearIoUringUserData();
            }

            if (canPrepareTeardownCancels && queuedAsyncCancel)
            {
                _ = SubmitIoUringOperationsNormalized();
            }
        }

        /// <summary>Increments the late-completion counter and samples to the log.</summary>
        private void RecordBenignLateIoUringCompletion()
        {
            Interlocked.Increment(ref _ioUringBenignLateCompletionCount);
        }

        /// <summary>Increments the diagnostic counter tracking pending completion retries that queued prepare work.</summary>
        private void RecordIoUringPendingRetryQueuedToPrepareQueue()
        {
            Interlocked.Increment(ref _ioUringPendingRetryQueuedToPrepareQueueCount);
        }

        /// <summary>Increments the non-pinnable prepare fallback counter for this engine instance.</summary>
        internal void RecordIoUringNonPinnablePrepareFallback()
        {
            Interlocked.Increment(ref _ioUringNonPinnablePrepareFallbackCount);
        }

        /// <summary>Increments the completion-slot exhaustion counter.</summary>
        private void RecordIoUringCompletionSlotExhaustion()
        {
            Interlocked.Increment(ref _ioUringCompletionSlotExhaustionCount);
        }

        /// <summary>Increments the completion-slot drain-recovery counter.</summary>
        private void RecordIoUringCompletionSlotDrainRecovery()
        {
            Interlocked.Increment(ref _ioUringCompletionSlotDrainRecoveryCount);
        }

        /// <summary>Increments the prepare-queue overflow fallback counter.</summary>
        private void RecordIoUringPrepareQueueOverflowFallback()
        {
            Interlocked.Increment(ref _ioUringPrepareQueueOverflowFallbackCount);
        }

        /// <summary>Increments the requeue-failure counter and samples to the log.</summary>
        private void RecordIoUringCompletionRequeueFailure()
        {
            Interlocked.Increment(ref _ioUringCompletionRequeueFailureCount);
        }


    }
}
