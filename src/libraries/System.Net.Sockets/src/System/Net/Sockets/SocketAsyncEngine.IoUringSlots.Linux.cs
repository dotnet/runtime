// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nuint GetCompletionSlotNativeStorageStride()
        {
            nuint iovSize = (nuint)IoUringConstants.MessageInlineIovCount * (nuint)sizeof(Interop.Sys.IOVector);
            return (nuint)sizeof(NativeMsghdr) +
                iovSize +
                (nuint)IoUringConstants.MessageInlineSocketAddressCapacity +
                (nuint)IoUringConstants.MessageInlineControlBufferCapacity +
                (nuint)sizeof(int);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void InitializeCompletionSlotNativeStorage(
            ref IoUringCompletionSlotStorage slotStorage,
            byte* slotStorageBase)
        {
            slotStorage.NativeInlineStorage = slotStorageBase;
            slotStorage.NativeMsgHdrPtr = (IntPtr)slotStorageBase;

            byte* cursor = slotStorageBase + sizeof(NativeMsghdr);
            slotStorage.NativeIOVectors = (Interop.Sys.IOVector*)cursor;
            cursor += IoUringConstants.MessageInlineIovCount * sizeof(Interop.Sys.IOVector);
            slotStorage.NativeSocketAddress = cursor;
            cursor += IoUringConstants.MessageInlineSocketAddressCapacity;
            slotStorage.NativeControlBuffer = cursor;
            cursor += IoUringConstants.MessageInlineControlBufferCapacity;
            slotStorage.NativeSocketAddressLengthPtr = (int*)cursor;

            slotStorage.MessageIsReceive = false;
            slotStorage.ReceiveOutputSocketAddress = null;
            slotStorage.ReceiveOutputControlBuffer = null;
            slotStorage.ReceiveSocketAddressCapacity = 0;
            slotStorage.ReceiveControlBufferCapacity = 0;
        }

        /// <summary>Allocates SoA completion slot arrays and initializes the free list.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeCompletionSlotPool(int capacity)
        {
            Debug.Assert(
                (ulong)capacity <= IoUringConstants.SlotIndexMask + 1UL,
                $"Completion slot capacity {capacity} exceeds encodable slot index range {IoUringConstants.SlotIndexMask + 1UL}.");
            Debug.Assert(
                Unsafe.SizeOf<IoUringCompletionSlot>() == 24,
                $"IoUringCompletionSlot size drifted: expected 24, got {Unsafe.SizeOf<IoUringCompletionSlot>()}.");
            _completionSlots = new IoUringCompletionSlot[capacity];
            _trackedOperations = new IoUringTrackedOperationState[capacity];
            _completionSlotStorage = new IoUringCompletionSlotStorage[capacity];
            _zeroCopyPinHolds = new System.Buffers.MemoryHandle[capacity];
            _completionSlotNativeStorageStride = GetCompletionSlotNativeStorageStride();
            Debug.Assert(
                _completionSlotNativeStorageStride <= int.MaxValue,
                $"Completion slot native storage stride overflow: {_completionSlotNativeStorageStride}.");
            if (_completionSlotNativeStorageStride > int.MaxValue)
            {
                // FailFast-adjacent site: impossible stride overflow indicates corrupted
                // layout assumptions during engine initialization, so keep the hard failure.
                ThrowInternalException(Interop.Error.EOVERFLOW);
            }

            _completionSlotNativeStorage = (byte*)NativeMemory.AllocZeroed((nuint)capacity * _completionSlotNativeStorageStride);
            // Build free list linking all slots
            for (int i = 0; i < capacity - 1; i++)
            {
                _completionSlots[i].Generation = 1;
                _completionSlots[i].FreeListNext = i + 1;
                InitializeCompletionSlotNativeStorage(
                    ref _completionSlotStorage[i],
                    _completionSlotNativeStorage + ((nuint)i * _completionSlotNativeStorageStride));
            }
            _completionSlots[capacity - 1].Generation = 1;
            _completionSlots[capacity - 1].FreeListNext = -1;
            InitializeCompletionSlotNativeStorage(
                ref _completionSlotStorage[capacity - 1],
                _completionSlotNativeStorage + ((nuint)(capacity - 1) * _completionSlotNativeStorageStride));
            _completionSlotFreeListHead = 0;
            _completionSlotsInUse = 0;
            _completionSlotsHighWaterMark = 0;
            _liveAcceptCompletionSlotCount = 0;
            _trackedIoUringOperationCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCompletionSlotKind(ref IoUringCompletionSlot slot, IoUringCompletionOperationKind kind)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "SetCompletionSlotKind must run on the event-loop thread.");
            IoUringCompletionOperationKind previousKind = slot.Kind;
            if (previousKind == kind)
            {
                return;
            }

            slot.Kind = kind;
            bool previousIsAcceptLike = previousKind == IoUringCompletionOperationKind.Accept ||
                previousKind == IoUringCompletionOperationKind.ReusePortAccept;
            bool currentIsAcceptLike = kind == IoUringCompletionOperationKind.Accept ||
                kind == IoUringCompletionOperationKind.ReusePortAccept;
            if (previousIsAcceptLike || currentIsAcceptLike)
            {
                int liveAcceptCount = _liveAcceptCompletionSlotCount;
                if (previousIsAcceptLike)
                {
                    liveAcceptCount--;
                }

                if (currentIsAcceptLike)
                {
                    liveAcceptCount++;
                }

                Debug.Assert(liveAcceptCount >= 0);
                Volatile.Write(ref _liveAcceptCompletionSlotCount, liveAcceptCount);
            }
        }

        /// <summary>
        /// Allocates a completion slot from the free list. Returns the slot index,
        /// or -1 if the pool is exhausted (backpressure signal).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AllocateCompletionSlot()
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "AllocateCompletionSlot must run on the event-loop thread.");
            Debug.Assert(_completionSlots is not null);
            int index = _completionSlotFreeListHead;
            if (index < 0)
                return -1; // Pool exhausted

            ref IoUringCompletionSlot slot = ref _completionSlots![index];
            // Slot state is reset in FreeCompletionSlot; keep allocation to free-list bookkeeping only.
            _completionSlotFreeListHead = slot.FreeListNext;
            slot.FreeListNext = -1;
            int inUse = ++_completionSlotsInUse;
            if (inUse > _completionSlotsHighWaterMark)
            {
                _completionSlotsHighWaterMark = inUse;
                SocketsTelemetry.Log.IoUringCompletionSlotHighWaterMark(inUse);
            }
            return index;
        }

        /// <summary>
        /// Returns a completion slot to the free list, incrementing its generation
        /// to invalidate any stale user_data references.
        /// </summary>
        private unsafe void FreeCompletionSlot(int index)
        {
            Debug.Assert(IsCurrentThreadEventLoopThread(),
                "FreeCompletionSlot must run on the event-loop thread.");
            Debug.Assert(index >= 0 && index < _completionSlots!.Length);

            ReleaseZeroCopyPinHold(index);
            ref IoUringCompletionSlot slot = ref _completionSlots![index];
            ref IoUringTrackedOperationState trackedState = ref _trackedOperations![index];
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![index];
            Debug.Assert(
                Volatile.Read(ref trackedState.TrackedOperation) is null,
                "Completion slot should not be freed while a tracked io_uring operation is still attached.");

            SafeSocketHandle? dangerousRefSocketHandle = slotStorage.DangerousRefSocketHandle;
            ExceptionDispatchInfo? dangerousReleaseException = null;
            try
            {
                if (dangerousRefSocketHandle is not null)
                {
                    slotStorage.DangerousRefSocketHandle = null;
                    dangerousRefSocketHandle.DangerousRelease();
                }
            }
            catch (Exception ex)
            {
                dangerousReleaseException = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                if (slot.UsesFixedRecvBuffer)
                {
                    IoUringProvidedBufferRing? providedBufferRing = _ioUringProvidedBufferRing;
                    if (providedBufferRing is not null)
                    {
                        providedBufferRing.TryRecycleBufferFromCompletion(slot.FixedRecvBufferId);
                    }
                }

                // Free any native message storage
                if (slot.Kind == IoUringCompletionOperationKind.Message)
                {
                    FreeMessageStorage(index);
                }
                else if (slot.Kind == IoUringCompletionOperationKind.Accept)
                {
                    if (slotStorage.NativeSocketAddressLengthPtr != null)
                    {
                        *slotStorage.NativeSocketAddressLengthPtr = 0;
                    }
                }
                else if (slot.Kind == IoUringCompletionOperationKind.ReusePortAccept)
                {
                    slotStorage.ReusePortPrimaryContext = null;
                    slotStorage.ReusePortPrimaryEngine = null;
                }

                slot.Generation = (slot.Generation + 1UL) & IoUringConstants.GenerationMask;
                if (slot.Generation == 0)
                {
                    slot.Generation = 1;
                }
                SetCompletionSlotKind(ref slot, IoUringCompletionOperationKind.None);
                ResetDebugTestForcedResult(ref slot);
                slot.ClearZeroCopyState();
                slot.UsesFixedRecvBuffer = false;
                slot.FixedRecvBufferId = 0;
                Volatile.Write(ref trackedState.TrackedOperation, null);
                trackedState.TrackedOperationGeneration = 0;
                slot.FreeListNext = _completionSlotFreeListHead;
                _completionSlotFreeListHead = index;
                _completionSlotsInUse--;
            }

            dangerousReleaseException?.Throw();
        }

        /// <summary>Disposes a retained zero-copy pin-hold for the specified completion slot.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseZeroCopyPinHold(int slotIndex)
        {
            System.Buffers.MemoryHandle[]? pinHolds = _zeroCopyPinHolds;
            if (pinHolds is null || (uint)slotIndex >= (uint)pinHolds.Length)
            {
                return;
            }

            pinHolds[slotIndex].Dispose();
            pinHolds[slotIndex] = default;
        }

        /// <summary>Transfers operation-owned pin state into the engine's zero-copy pin-hold table.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TransferIoUringZeroCopyPinHold(ulong userData, System.Buffers.MemoryHandle pinHold)
        {
            System.Buffers.MemoryHandle[]? pinHolds = _zeroCopyPinHolds;
            if (pinHolds is null)
            {
                pinHold.Dispose();
                Debug.Fail("Zero-copy pin-hold table is unavailable while transferring pin ownership.");
                return;
            }

            int slotIndex = DecodeCompletionSlotIndex(userData & IoUringUserDataPayloadMask);
            if ((uint)slotIndex >= (uint)pinHolds.Length)
            {
                pinHold.Dispose();
                Debug.Fail($"Invalid completion slot index while transferring zero-copy pin hold: {slotIndex}.");
                return;
            }

            Debug.Assert(_completionSlots is not null);
            ref IoUringCompletionSlot slot = ref _completionSlots![slotIndex];
            if (!slot.IsZeroCopySend)
            {
                pinHold.Dispose();
                Debug.Fail("Zero-copy pin hold transfer requested for a non-zero-copy completion slot.");
                return;
            }

            pinHolds[slotIndex].Dispose();
            pinHolds[slotIndex] = pinHold;
        }

        /// <summary>
        /// Prepares pre-allocated per-slot native message storage for sendmsg/recvmsg.
        /// Returns false when header shape exceeds inline capacities so callers can fall back.
        /// </summary>
        private unsafe bool TryPrepareInlineMessageStorage(int slotIndex, Interop.Sys.MessageHeader* messageHeader, bool isReceive)
        {
            Debug.Assert(sizeof(NativeMsghdr) == NativeMsghdr.ExpectedSize, $"NativeMsghdr size mismatch with kernel struct msghdr: expected {NativeMsghdr.ExpectedSize}, got {sizeof(NativeMsghdr)}");
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![slotIndex];

            int iovCount = messageHeader->IOVectorCount;
            int sockAddrLen = messageHeader->SocketAddressLen;
            int controlBufLen = messageHeader->ControlBufferLen;
            Debug.Assert(iovCount >= 0, $"Expected non-negative iovCount, got {iovCount}");
            Debug.Assert(sockAddrLen >= 0, $"Expected non-negative socket address length, got {sockAddrLen}");
            Debug.Assert(controlBufLen >= 0, $"Expected non-negative control buffer length, got {controlBufLen}");

            if ((uint)iovCount > IoUringConstants.MessageInlineIovCount ||
                (uint)sockAddrLen > IoUringConstants.MessageInlineSocketAddressCapacity ||
                (uint)controlBufLen > IoUringConstants.MessageInlineControlBufferCapacity)
            {
                return false;
            }

            if (slotStorage.NativeInlineStorage == null)
            {
                return false;
            }

            if ((iovCount > 0 && messageHeader->IOVectors == null) ||
                (sockAddrLen > 0 && messageHeader->SocketAddress == null) ||
                (controlBufLen > 0 && messageHeader->ControlBuffer == null))
            {
                return false;
            }

            // Most of the inline slab is overwritten immediately; clear only msghdr header state.
            new Span<byte>(slotStorage.NativeMsgHdrPtr.ToPointer(), sizeof(NativeMsghdr)).Clear();

            NativeMsghdr* hdr = (NativeMsghdr*)slotStorage.NativeMsgHdrPtr;
            Interop.Sys.IOVector* iovDst = slotStorage.NativeIOVectors;
            byte* sockAddrDst = slotStorage.NativeSocketAddress;
            byte* controlBufDst = slotStorage.NativeControlBuffer;

            if (iovCount > 0)
            {
                nuint iovBytes = (nuint)iovCount * (nuint)sizeof(Interop.Sys.IOVector);
                Buffer.MemoryCopy(
                    messageHeader->IOVectors,
                    iovDst,
                    (nuint)IoUringConstants.MessageInlineIovCount * (nuint)sizeof(Interop.Sys.IOVector),
                    iovBytes);
            }

            if (!isReceive)
            {
                if (sockAddrLen > 0)
                {
                    Buffer.MemoryCopy(
                        messageHeader->SocketAddress,
                        sockAddrDst,
                        (nuint)IoUringConstants.MessageInlineSocketAddressCapacity,
                        (nuint)sockAddrLen);
                }

                if (controlBufLen > 0)
                {
                    Buffer.MemoryCopy(
                        messageHeader->ControlBuffer,
                        controlBufDst,
                        (nuint)IoUringConstants.MessageInlineControlBufferCapacity,
                        (nuint)controlBufLen);
                }
            }

            hdr->MsgName = sockAddrLen > 0 ? sockAddrDst : null;
            hdr->MsgNameLen = (uint)sockAddrLen;
            hdr->MsgIov = iovCount > 0 ? iovDst : null;
            hdr->MsgIovLen = (nuint)iovCount;
            hdr->MsgControl = controlBufLen > 0 ? controlBufDst : null;
            hdr->MsgControlLen = (nuint)controlBufLen;
            hdr->MsgFlags = 0;

            if (isReceive)
            {
                slotStorage.ReceiveOutputSocketAddress = messageHeader->SocketAddress;
                slotStorage.ReceiveOutputControlBuffer = messageHeader->ControlBuffer;
                slotStorage.ReceiveSocketAddressCapacity = sockAddrLen;
                slotStorage.ReceiveControlBufferCapacity = controlBufLen;
            }
            else
            {
                slotStorage.ReceiveOutputSocketAddress = null;
                slotStorage.ReceiveOutputControlBuffer = null;
                slotStorage.ReceiveSocketAddressCapacity = 0;
                slotStorage.ReceiveControlBufferCapacity = 0;
            }

            slotStorage.MessageIsReceive = isReceive;
            return true;
        }

        /// <summary>
        /// Resets inline message metadata on the completion slot.
        /// </summary>
        private unsafe void FreeMessageStorage(int slotIndex)
        {
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![slotIndex];
            // Slot inline storage is cleared on prepare before each reuse; avoid a second full memset on free.

            slotStorage.ReceiveOutputSocketAddress = null;
            slotStorage.ReceiveOutputControlBuffer = null;
            slotStorage.ReceiveSocketAddressCapacity = 0;
            slotStorage.ReceiveControlBufferCapacity = 0;
            slotStorage.MessageIsReceive = false;
        }

        /// <summary>
        /// After a recvmsg CQE completes, copies the kernel-written socket address and
        /// control buffer data from the native msghdr back to the managed MessageHeader's
        /// output buffers. For sendmsg completions this is a no-op.
        /// Returns the actual socket address length, control buffer length, and msg_flags written by the kernel.
        /// </summary>
        private unsafe void CopyMessageCompletionOutputs(
            int slotIndex,
            out int socketAddressLen,
            out int controlBufferLen,
            out uint messageFlags)
        {
            ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![slotIndex];
            socketAddressLen = 0;
            controlBufferLen = 0;
            messageFlags = 0;

            if (!slotStorage.MessageIsReceive)
                return;

            NativeMsghdr* hdr = (NativeMsghdr*)slotStorage.NativeMsgHdrPtr;
            if (hdr == null)
                return;

            socketAddressLen = (int)hdr->MsgNameLen;
            controlBufferLen = (int)hdr->MsgControlLen;
            messageFlags = (uint)hdr->MsgFlags;

            // Copy socket address from native buffer back to managed output buffer
            if (slotStorage.ReceiveOutputSocketAddress != null && slotStorage.NativeSocketAddress != null &&
                slotStorage.ReceiveSocketAddressCapacity > 0 && socketAddressLen > 0)
            {
                int copyLen = Math.Min(slotStorage.ReceiveSocketAddressCapacity, socketAddressLen);
                Buffer.MemoryCopy(slotStorage.NativeSocketAddress, slotStorage.ReceiveOutputSocketAddress, copyLen, copyLen);
            }

            // Copy control buffer from native buffer back to managed output buffer
            if (slotStorage.ReceiveOutputControlBuffer != null && slotStorage.NativeControlBuffer != null &&
                slotStorage.ReceiveControlBufferCapacity > 0 && controlBufferLen > 0)
            {
                int copyLen = Math.Min(slotStorage.ReceiveControlBufferCapacity, controlBufferLen);
                Buffer.MemoryCopy(slotStorage.NativeControlBuffer, slotStorage.ReceiveOutputControlBuffer, copyLen, copyLen);
            }
        }

        /// <summary>
        /// Decodes a completion slot index from a user_data payload value.
        /// The slot index is encoded in the lower <see cref="IoUringConstants.SlotIndexBits"/> bits of the payload.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DecodeCompletionSlotIndex(ulong payload)
        {
            return (int)(payload & IoUringConstants.SlotIndexMask);
        }

        /// <summary>
        /// Encodes a completion slot index and generation into a user_data value
        /// with the ReservedCompletion tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong EncodeCompletionSlotUserData(int slotIndex, ulong generation)
        {
            ulong payload = ((ulong)(generation & IoUringConstants.GenerationMask) << IoUringConstants.SlotIndexBits) | ((ulong)slotIndex & IoUringConstants.SlotIndexMask);
            return EncodeIoUringUserData(IoUringConstants.TagReservedCompletion, payload);
        }
    }
}
