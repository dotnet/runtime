// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        /// <summary>
        /// Maps the SQ ring, CQ ring, and SQE array into managed address space and derives
        /// all ring pointers from the kernel-reported offsets. On failure, unmaps any
        /// partially-mapped regions and closes the ring fd.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe bool TryMmapRings(ref IoUringSetupResult setup)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsOffsetInRange(ulong offset, ulong size, ulong mappedSize) =>
                offset <= mappedSize && size <= mappedSize - offset;

            ref Interop.Sys.IoUringParams p = ref setup.Params;
            bool usesNoSqArray = (setup.NegotiatedFlags & IoUringConstants.SetupNoSqArray) != 0;
            bool usesSqe128 = (setup.NegotiatedFlags & IoUringConstants.SetupSqe128) != 0;
            uint negotiatedSqeSize = usesSqe128 ? 128u : (uint)sizeof(IoUringSqe);
            if (negotiatedSqeSize != (uint)sizeof(IoUringSqe))
            {
                // Managed SQE writers currently mirror the 64-byte io_uring_sqe layout.
                Interop.Sys.IoUringShimCloseFd(setup.RingFd);
                return false;
            }

            // Compute ring sizes.
            ulong sqRingSize = p.SqOff.Array;
            if (!usesNoSqArray)
            {
                sqRingSize += p.SqEntries * (uint)sizeof(uint);
            }
            ulong cqRingSize = p.CqOff.Cqes + p.CqEntries * (uint)sizeof(Interop.Sys.IoUringCqe);
            ulong sqesSize = p.SqEntries * negotiatedSqeSize;

            // mmap SQ ring (and possibly CQ ring if SINGLE_MMAP).
            bool usesSingleMmap = (p.Features & IoUringConstants.FeatureSingleMmap) != 0;

            byte* sqRingPtr;
            byte* cqRingPtr;

            if (usesSingleMmap)
            {
                ulong ringSize = sqRingSize > cqRingSize ? sqRingSize : cqRingSize;
                void* ptr;
                Interop.Error err = Interop.Sys.IoUringShimMmap(setup.RingFd, ringSize, IoUringConstants.OffSqRing, &ptr);
                if (err != Interop.Error.SUCCESS)
                {
                    Interop.Sys.IoUringShimCloseFd(setup.RingFd);
                    return false;
                }
                sqRingPtr = (byte*)ptr;
                cqRingPtr = (byte*)ptr;
                sqRingSize = ringSize;
                cqRingSize = ringSize;
            }
            else
            {
                void* sqPtr;
                Interop.Error err = Interop.Sys.IoUringShimMmap(setup.RingFd, sqRingSize, IoUringConstants.OffSqRing, &sqPtr);
                if (err != Interop.Error.SUCCESS)
                {
                    Interop.Sys.IoUringShimCloseFd(setup.RingFd);
                    return false;
                }
                sqRingPtr = (byte*)sqPtr;

                void* cqPtr;
                err = Interop.Sys.IoUringShimMmap(setup.RingFd, cqRingSize, IoUringConstants.OffCqRing, &cqPtr);
                if (err != Interop.Error.SUCCESS)
                {
                    Interop.Sys.IoUringShimMunmap(sqRingPtr, sqRingSize);
                    Interop.Sys.IoUringShimCloseFd(setup.RingFd);
                    return false;
                }
                cqRingPtr = (byte*)cqPtr;
            }

            if (!IsOffsetInRange(p.SqOff.Head, sizeof(uint), sqRingSize) ||
                !IsOffsetInRange(p.SqOff.Tail, sizeof(uint), sqRingSize) ||
                !IsOffsetInRange(p.SqOff.RingMask, sizeof(uint), sqRingSize) ||
                !IsOffsetInRange(p.SqOff.RingEntries, sizeof(uint), sqRingSize) ||
                !IsOffsetInRange(p.SqOff.Flags, sizeof(uint), sqRingSize) ||
                (!usesNoSqArray && !IsOffsetInRange(p.SqOff.Array, p.SqEntries * (uint)sizeof(uint), sqRingSize)) ||
                !IsOffsetInRange(p.CqOff.Head, sizeof(uint), cqRingSize) ||
                !IsOffsetInRange(p.CqOff.Tail, sizeof(uint), cqRingSize) ||
                !IsOffsetInRange(p.CqOff.RingMask, sizeof(uint), cqRingSize) ||
                !IsOffsetInRange(p.CqOff.RingEntries, sizeof(uint), cqRingSize) ||
                !IsOffsetInRange(p.CqOff.Overflow, sizeof(uint), cqRingSize) ||
                !IsOffsetInRange(p.CqOff.Cqes, p.CqEntries * (uint)sizeof(Interop.Sys.IoUringCqe), cqRingSize))
            {
                if (!usesSingleMmap)
                {
                    Interop.Sys.IoUringShimMunmap(cqRingPtr, cqRingSize);
                }

                Interop.Sys.IoUringShimMunmap(sqRingPtr, sqRingSize);
                Interop.Sys.IoUringShimCloseFd(setup.RingFd);
                return false;
            }

            // mmap SQE array.
            void* sqePtr;
            {
                Interop.Error err = Interop.Sys.IoUringShimMmap(setup.RingFd, sqesSize, IoUringConstants.OffSqes, &sqePtr);
                if (err != Interop.Error.SUCCESS)
                {
                    if (!usesSingleMmap)
                        Interop.Sys.IoUringShimMunmap(cqRingPtr, cqRingSize);
                    Interop.Sys.IoUringShimMunmap(sqRingPtr, sqRingSize);
                    Interop.Sys.IoUringShimCloseFd(setup.RingFd);
                    return false;
                }
            }

            // Derive SQ pointers and populate existing _ioUringSqRingInfo for compatibility.
            _ioUringSqRingInfo.SqeBase = (IntPtr)sqePtr;
            _ioUringSqRingInfo.SqTailPtr = (IntPtr)(sqRingPtr + p.SqOff.Tail);
            _ioUringSqRingInfo.SqHeadPtr = (IntPtr)(sqRingPtr + p.SqOff.Head);
            _ioUringSqRingInfo.SqMask = *(uint*)(sqRingPtr + p.SqOff.RingMask);
            _ioUringSqRingInfo.SqEntries = *(uint*)(sqRingPtr + p.SqOff.RingEntries);
            _ioUringSqRingInfo.SqeSize = negotiatedSqeSize;
            _ioUringSqRingInfo.UsesNoSqArray = usesNoSqArray ? (byte)1 : (byte)0;
            _ioUringSqRingInfo.RingFd = setup.RingFd;
            _ioUringSqRingInfo.UsesEnterExtArg = setup.UsesExtArg ? (byte)1 : (byte)0;
            _managedSqFlagsPtr = (uint*)(sqRingPtr + p.SqOff.Flags);

            // Initialize SQ array identity mapping if NO_SQARRAY is not active.
            if (!usesNoSqArray)
            {
                uint* sqArray = (uint*)(sqRingPtr + p.SqOff.Array);
                for (uint i = 0; i < p.SqEntries; i++)
                {
                    sqArray[i] = i;
                }
            }

            // Derive CQ pointers.
            _managedCqeBase = (Interop.Sys.IoUringCqe*)(cqRingPtr + p.CqOff.Cqes);
            _managedCqTailPtr = (uint*)(cqRingPtr + p.CqOff.Tail);
            _managedCqHeadPtr = (uint*)(cqRingPtr + p.CqOff.Head);
            _managedCqMask = *(uint*)(cqRingPtr + p.CqOff.RingMask);
            _managedCqEntries = *(uint*)(cqRingPtr + p.CqOff.RingEntries);
            _managedCqOverflowPtr = (uint*)(cqRingPtr + p.CqOff.Overflow);

            Debug.Assert(
                BitOperations.IsPow2(_ioUringSqRingInfo.SqEntries),
                $"Kernel-reported SQ entries must be power-of-two. sq_entries={_ioUringSqRingInfo.SqEntries}");
            Debug.Assert(
                BitOperations.IsPow2(_managedCqEntries),
                $"Kernel-reported CQ entries must be power-of-two. cq_entries={_managedCqEntries}");
            Debug.Assert(
                _ioUringSqRingInfo.SqMask == _ioUringSqRingInfo.SqEntries - 1,
                $"Unexpected SQ mask/entries contract: sq_mask={_ioUringSqRingInfo.SqMask}, sq_entries={_ioUringSqRingInfo.SqEntries}");
            Debug.Assert(
                _managedCqMask == _managedCqEntries - 1,
                $"Unexpected CQ mask/entries contract: cq_mask={_managedCqMask}, cq_entries={_managedCqEntries}");

            _managedObservedCqOverflow = Volatile.Read(ref *_managedCqOverflowPtr);
            _cqOverflowRecoveryActive = false;
            _cqOverflowRecoveryBranch = default;

            // Store ring region info for teardown.
            _managedSqRingPtr = sqRingPtr;
            _managedCqRingPtr = cqRingPtr;
            _managedSqRingSize = sqRingSize;
            _managedCqRingSize = cqRingSize;
            _managedSqesSize = sqesSize;
            _managedUsesSingleMmap = usesSingleMmap;
            _managedRingFd = setup.RingFd;
            _managedUsesExtArg = setup.UsesExtArg;
            _managedUsesNoSqArray = usesNoSqArray;
            _managedNegotiatedFlags = setup.NegotiatedFlags;
            _managedSqeInvariantsValidated = ValidateManagedSqeInitializationInvariants();
            if (!_managedSqeInvariantsValidated)
            {
                CleanupManagedRings();
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void CleanupManagedRings()
        {
            _managedCqDrainEnabled = false;

            byte* sqRingPtr = _managedSqRingPtr;
            byte* cqRingPtr = _managedCqRingPtr;
            ulong sqRingSize = _managedSqRingSize;
            ulong cqRingSize = _managedCqRingSize;
            ulong sqesSize = _managedSqesSize;
            bool usesSingleMmap = _managedUsesSingleMmap;
            void* sqeBase = _ioUringSqRingInfo.SqeBase.ToPointer();

            // Clear all mmap-derived pointers before unmapping so any late reads fail safely.
            _managedSqFlagsPtr = null;
            _managedCqeBase = null;
            _managedCqTailPtr = null;
            _managedCqHeadPtr = null;
            _managedCqOverflowPtr = null;
            _managedSqRingPtr = null;
            _managedCqRingPtr = null;
            _managedSqRingSize = 0;
            _managedCqRingSize = 0;
            _managedSqesSize = 0;
            _managedCqMask = 0;
            _managedCqEntries = 0;
            _managedCachedCqHead = 0;
            _managedObservedCqOverflow = 0;
            _ioUringSqRingInfo = default;
            _managedSqeInvariantsValidated = false;

            if (sqRingPtr != null)
            {
                // Unmap SQEs first
                if (sqesSize > 0 && sqeBase != null)
                {
                    Interop.Sys.IoUringShimMunmap(sqeBase, sqesSize);
                }
                // Unmap CQ ring (only if separate from SQ ring)
                if (!usesSingleMmap && cqRingPtr != null && cqRingPtr != sqRingPtr)
                {
                    Interop.Sys.IoUringShimMunmap(cqRingPtr, cqRingSize);
                }
                // Unmap SQ ring
                Interop.Sys.IoUringShimMunmap(sqRingPtr, sqRingSize);
            }
            if (_managedRingFd >= 0)
            {
                Interop.Sys.IoUringShimCloseFd(_managedRingFd);
                _managedRingFd = -1;
            }
        }

        /// <summary>Unmaps rings and closes the ring fd.</summary>
        partial void LinuxFreeIoUringResources()
        {
            // Managed io_uring teardown: release resources allocated during TryInitializeManagedIoUring.
            // This must run BEFORE the common slot/buffer cleanup below because kernel
            // unregister operations need the ring fd to still be open.
            if (_ioUringInitialized)
            {
                // 0. Unregister/dispose provided buffer ring while the main ring fd is still open.
                FreeIoUringProvidedBufferRing();

                // 1. The registered ring fd is implicitly released when the ring fd is closed.
                //    Just mark it as inactive so no subsequent code attempts to use it.
                _ioUringSqRingInfo.RegisteredRingFd = -1;

                // 2. Close the wakeup eventfd.
                if (_managedWakeupEventFd >= 0)
                {
                    Interop.Sys.IoUringShimCloseFd(_managedWakeupEventFd);
                    _managedWakeupEventFd = -1;
                }

                // 3. Unmap SQ/CQ rings, SQEs and close the ring fd.
                //    Closing the ring fd also terminates any kernel SQPOLL thread for this ring.
                CleanupManagedRings();

                // 4. Disable managed flags to prevent any late operations.
                _ioUringInitialized = false;
                _managedCqDrainEnabled = false;
            }

            bool portClosedForTeardown = Volatile.Read(ref _ioUringPortClosedForTeardown) != 0;
            if (!portClosedForTeardown)
            {
                PollIoUringDiagnosticsIfNeeded(force: true);
            }

            // Second drain intentionally catches any items enqueued after LinuxBeforeFreeNativeResources
            // published teardown but before native port closure became globally visible.
            DrainQueuedIoUringOperationsForTeardown();

            if (_completionSlots is not null)
            {
                DrainTrackedIoUringOperationsForTeardown(portClosedForTeardown);
                Debug.Assert(IsIoUringTrackingEmpty(), $"Leaked tracked io_uring operations: {Volatile.Read(ref _trackedIoUringOperationCount)}");

                // Free any native memory still held by completion slots
                for (int i = 0; i < _completionSlots.Length; i++)
                {
                    ref IoUringCompletionSlot slot = ref _completionSlots[i];
                    ref IoUringCompletionSlotStorage slotStorage = ref _completionSlotStorage![i];
                    if (slot.IsZeroCopySend && slot.ZeroCopyNotificationPending)
                    {
                        // Ring teardown can drop in-flight NOTIF CQEs; clear pending SEND_ZC state
                        // so teardown cannot leave slots/pin-holds logically waiting forever.
                        slot.IsZeroCopySend = false;
                        slot.ZeroCopyNotificationPending = false;
                    }

                    ReleaseZeroCopyPinHold(i);
                    if (slot.Kind == IoUringCompletionOperationKind.Message)
                    {
                        FreeMessageStorage(i);
                    }
                    else if (slot.Kind == IoUringCompletionOperationKind.Accept && slotStorage.NativeSocketAddressLengthPtr != null)
                    {
                        *slotStorage.NativeSocketAddressLengthPtr = 0;
                    }

                    // Clear all pointers that alias _completionSlotNativeStorage before freeing it.
                    slotStorage.NativeInlineStorage = null;
                    slotStorage.NativeSocketAddressLengthPtr = null;
                    slotStorage.NativeMsgHdrPtr = IntPtr.Zero;
                    slotStorage.MessageIsReceive = false;
                    slotStorage.NativeIOVectors = null;
                    slotStorage.NativeSocketAddress = null;
                    slotStorage.NativeControlBuffer = null;
                    slotStorage.ReceiveOutputSocketAddress = null;
                    slotStorage.ReceiveOutputControlBuffer = null;
                    slotStorage.ReceiveSocketAddressCapacity = 0;
                    slotStorage.ReceiveControlBufferCapacity = 0;
                }

                _completionSlots = null;
                _trackedOperations = null;
                _completionSlotStorage = null;
                _trackedIoUringOperationCount = 0;
                _zeroCopyPinHolds = null;
                _completionSlotFreeListHead = -1;
                _completionSlotsInUse = 0;
                _liveAcceptCompletionSlotCount = 0;

                _ioUringSlotCapacity = 0;
                _cqOverflowRecoveryActive = false;
                _cqOverflowRecoveryBranch = default;
                _ioUringManagedPendingSubmissions = 0;
                _ioUringManagedSqTail = 0;
                _ioUringManagedSqTailLoaded = false;
                _ioUringSqRingInfo = default;
                _ioUringDirectSqeEnabled = false;
                _sqPollEnabled = false;

            }

            if (_completionSlotNativeStorage != null)
            {
                NativeMemory.Free(_completionSlotNativeStorage);
                _completionSlotNativeStorage = null;
                _completionSlotNativeStorageStride = 0;
            }

            ResetIoUringPrepareQueueDepthTelemetry();

            // Final flush of managed io_uring deltas in case teardown modified counters
            // after the forced diagnostics poll and no further event-loop iteration runs.
            PublishIoUringManagedDiagnosticsDelta();
        }
    }
}
