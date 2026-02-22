// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        internal readonly struct IoUringNonPinnableFallbackPublicationState
        {
            internal IoUringNonPinnableFallbackPublicationState(long publishedCount, int publishingGate, long fallbackCount)
            {
                PublishedCount = publishedCount;
                PublishingGate = publishingGate;
                FallbackCount = fallbackCount;
            }

            internal long PublishedCount { get; }
            internal int PublishingGate { get; }
            internal long FallbackCount { get; }
        }

        internal readonly struct IoUringProvidedBufferSnapshotForTest
        {
            internal IoUringProvidedBufferSnapshotForTest(
                bool hasIoUringPort,
                bool supportsProvidedBufferRings,
                bool hasProvidedBufferRing,
                bool hasRegisteredBuffers,
                bool adaptiveBufferSizingEnabled,
                int availableCount,
                int inUseCount,
                int totalBufferCount,
                int bufferSize,
                int recommendedBufferSize,
                long recycledCount,
                long allocationFailureCount)
            {
                HasIoUringPort = hasIoUringPort;
                SupportsProvidedBufferRings = supportsProvidedBufferRings;
                HasProvidedBufferRing = hasProvidedBufferRing;
                HasRegisteredBuffers = hasRegisteredBuffers;
                AdaptiveBufferSizingEnabled = adaptiveBufferSizingEnabled;
                AvailableCount = availableCount;
                InUseCount = inUseCount;
                TotalBufferCount = totalBufferCount;
                BufferSize = bufferSize;
                RecommendedBufferSize = recommendedBufferSize;
                RecycledCount = recycledCount;
                AllocationFailureCount = allocationFailureCount;
            }

            internal bool HasIoUringPort { get; }
            internal bool SupportsProvidedBufferRings { get; }
            internal bool HasProvidedBufferRing { get; }
            internal bool HasRegisteredBuffers { get; }
            internal bool AdaptiveBufferSizingEnabled { get; }
            internal int AvailableCount { get; }
            internal int InUseCount { get; }
            internal int TotalBufferCount { get; }
            internal int BufferSize { get; }
            internal int RecommendedBufferSize { get; }
            internal long RecycledCount { get; }
            internal long AllocationFailureCount { get; }
        }

        internal readonly struct IoUringZeroCopySendSnapshotForTest
        {
            internal IoUringZeroCopySendSnapshotForTest(
                bool hasIoUringPort,
                bool supportsSendZc,
                bool supportsSendMsgZc,
                bool zeroCopySendEnabled)
            {
                HasIoUringPort = hasIoUringPort;
                SupportsSendZc = supportsSendZc;
                SupportsSendMsgZc = supportsSendMsgZc;
                ZeroCopySendEnabled = zeroCopySendEnabled;
            }

            internal bool HasIoUringPort { get; }
            internal bool SupportsSendZc { get; }
            internal bool SupportsSendMsgZc { get; }
            internal bool ZeroCopySendEnabled { get; }
        }

        internal readonly struct IoUringFixedRecvSnapshotForTest
        {
            internal IoUringFixedRecvSnapshotForTest(
                bool hasIoUringPort,
                bool supportsReadFixed,
                bool hasRegisteredBuffers)
            {
                HasIoUringPort = hasIoUringPort;
                SupportsReadFixed = supportsReadFixed;
                HasRegisteredBuffers = hasRegisteredBuffers;
            }

            internal bool HasIoUringPort { get; }
            internal bool SupportsReadFixed { get; }
            internal bool HasRegisteredBuffers { get; }
        }

        internal readonly struct IoUringSqPollSnapshotForTest
        {
            internal IoUringSqPollSnapshotForTest(bool hasIoUringPort, bool sqPollEnabled, bool deferTaskrunEnabled)
            {
                HasIoUringPort = hasIoUringPort;
                SqPollEnabled = sqPollEnabled;
                DeferTaskrunEnabled = deferTaskrunEnabled;
            }

            internal bool HasIoUringPort { get; }
            internal bool SqPollEnabled { get; }
            internal bool DeferTaskrunEnabled { get; }
        }

        internal readonly struct IoUringZeroCopyPinHoldSnapshotForTest
        {
            internal IoUringZeroCopyPinHoldSnapshotForTest(
                bool hasIoUringPort,
                int activePinHolds,
                int pendingNotificationCount)
            {
                HasIoUringPort = hasIoUringPort;
                ActivePinHolds = activePinHolds;
                PendingNotificationCount = pendingNotificationCount;
            }

            internal bool HasIoUringPort { get; }
            internal int ActivePinHolds { get; }
            internal int PendingNotificationCount { get; }
        }

        internal readonly struct IoUringNativeMsghdrLayoutSnapshotForTest
        {
            internal IoUringNativeMsghdrLayoutSnapshotForTest(
                int size,
                int msgNameOffset,
                int msgNameLengthOffset,
                int msgIovOffset,
                int msgIovLengthOffset,
                int msgControlOffset,
                int msgControlLengthOffset,
                int msgFlagsOffset)
            {
                Size = size;
                MsgNameOffset = msgNameOffset;
                MsgNameLengthOffset = msgNameLengthOffset;
                MsgIovOffset = msgIovOffset;
                MsgIovLengthOffset = msgIovLengthOffset;
                MsgControlOffset = msgControlOffset;
                MsgControlLengthOffset = msgControlLengthOffset;
                MsgFlagsOffset = msgFlagsOffset;
            }

            internal int Size { get; }
            internal int MsgNameOffset { get; }
            internal int MsgNameLengthOffset { get; }
            internal int MsgIovOffset { get; }
            internal int MsgIovLengthOffset { get; }
            internal int MsgControlOffset { get; }
            internal int MsgControlLengthOffset { get; }
            internal int MsgFlagsOffset { get; }
        }

        internal readonly struct IoUringCompletionSlotLayoutSnapshotForTest
        {
            internal IoUringCompletionSlotLayoutSnapshotForTest(
                int size,
                int generationOffset,
                int freeListNextOffset,
                int packedStateOffset,
                int fixedRecvBufferIdOffset,
                int testForcedResultOffset)
            {
                Size = size;
                GenerationOffset = generationOffset;
                FreeListNextOffset = freeListNextOffset;
                PackedStateOffset = packedStateOffset;
                FixedRecvBufferIdOffset = fixedRecvBufferIdOffset;
                TestForcedResultOffset = testForcedResultOffset;
            }

            internal int Size { get; }
            internal int GenerationOffset { get; }
            internal int FreeListNextOffset { get; }
            internal int PackedStateOffset { get; }
            internal int FixedRecvBufferIdOffset { get; }
            internal int TestForcedResultOffset { get; }
        }

        internal static IoUringNonPinnableFallbackPublicationState GetIoUringNonPinnableFallbackPublicationStateForTest() =>
            new IoUringNonPinnableFallbackPublicationState(
                GetPrimaryIoUringEnginePublishedNonPinnableFallbackCountForTest(),
                publishingGate: 0,
                SocketAsyncContext.GetIoUringNonPinnablePrepareFallbackCount());

        internal static int[] GetEnginePinnedCpuIndicesForTest()
        {
            SocketAsyncEngine[] engines = s_engines;
            int[] pinnedCpus = new int[engines.Length];
            for (int i = 0; i < engines.Length; i++)
            {
                pinnedCpus[i] = engines[i].PinnedCpuIndex;
            }

            return pinnedCpus;
        }

        internal static int GetEngineIndexForCpuForTest(int cpuIndex) =>
            GetEngineIndexForCpu(cpuIndex);

        internal static bool TrySetCurrentThreadAffinityForTest(int cpuIndex)
        {
            if (cpuIndex < 0 || cpuIndex >= IntPtr.Size * 8)
            {
                return false;
            }

            IntPtr mask = (IntPtr)unchecked((nint)(1UL << cpuIndex));
            return Interop.Sys.SchedSetAffinity(0, ref mask) == 0;
        }

        internal static void SetIoUringNonPinnableFallbackPublicationStateForTest(IoUringNonPinnableFallbackPublicationState state)
        {
#if DEBUG
            // Test-only publication-state control keeps concurrent publisher tests deterministic.
            SetPrimaryIoUringEnginePublishedNonPinnableFallbackCountForTest(state.PublishedCount);
            SocketAsyncContext.SetIoUringNonPinnablePrepareFallbackCountForTest(state.FallbackCount);
#else
            _ = state;
#endif
        }

        internal static long GetIoUringNonPinnablePrepareFallbackDeltaForTest()
        {
            if (TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine) &&
                ioUringEngine is not null &&
                ioUringEngine.TryPublishIoUringNonPinnablePrepareFallbackDelta(out long delta))
            {
                return delta;
            }

            return 0;
        }

        private static long GetPrimaryIoUringEnginePublishedNonPinnableFallbackCountForTest()
        {
            if (TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine) &&
                ioUringEngine is not null)
            {
                return Interlocked.Read(ref ioUringEngine._ioUringPublishedNonPinnablePrepareFallbackCount);
            }

            return 0;
        }

        private static void SetPrimaryIoUringEnginePublishedNonPinnableFallbackCountForTest(long value)
        {
            if (TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine) &&
                ioUringEngine is not null)
            {
                Interlocked.Exchange(ref ioUringEngine._ioUringPublishedNonPinnablePrepareFallbackCount, value);
            }
        }

        internal static bool IsIoUringEnabledForTest() => IsIoUringEnabled();
        internal static bool IsSqPollRequestedForTest() => IsSqPollRequested();
        internal static bool IsIoUringDirectSqeDisabledForTest() => IsIoUringDirectSqeDisabled();
        internal static bool IsZeroCopySendOptedInForTest() => IsZeroCopySendOptedIn();
        internal static bool IsIoUringRegisterBuffersEnabledForTest() => IsIoUringRegisterBuffersEnabled();
        internal static bool IsNativeMsghdrLayoutSupportedForIoUringForTest(int pointerSize, int nativeMsghdrSize) =>
            IsNativeMsghdrLayoutSupportedForIoUring(pointerSize, nativeMsghdrSize);
        internal static long GetIoUringPendingRetryQueuedToPrepareQueueCountForTest() => GetIoUringPendingRetryQueuedToPrepareQueueCount();
        internal static int GetIoUringCancellationQueueCapacityForTest() => s_ioUringCancellationQueueCapacity;

        internal static SocketAsyncEngine[] GetActiveIoUringEnginesForTest()
        {
            var engines = new List<SocketAsyncEngine>(s_engines.Length);
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled)
                {
                    engines.Add(engine);
                }
            }

            return engines.ToArray();
        }

        internal bool SupportsMultishotRecvForTest => _supportsMultishotRecv;
        internal bool SupportsMultishotAcceptForTest
        {
            get => _supportsMultishotAccept;
            set
            {
#if DEBUG
                _supportsMultishotAccept = value;
#else
                _ = value;
#endif
            }
        }

        internal bool SupportsProvidedBufferRingsForTest => _ioUringCapabilities.SupportsProvidedBufferRings;
        internal bool HasProvidedBufferRingForTest => _ioUringProvidedBufferRing is not null;
        internal bool IoUringBuffersRegisteredForTest => _ioUringCapabilities.HasRegisteredBuffers;
        internal bool AdaptiveBufferSizingEnabledForTest => _adaptiveBufferSizingEnabled;
        internal bool SupportsOpSendZcForTest
        {
            get => _supportsOpSendZc;
            set
            {
#if DEBUG
                _supportsOpSendZc = value;
#else
                _ = value;
#endif
            }
        }

        internal bool SupportsOpSendMsgZcForTest => _supportsOpSendMsgZc;
        internal bool ZeroCopySendEnabledForTest
        {
            get => _zeroCopySendEnabled;
            set
            {
#if DEBUG
                _zeroCopySendEnabled = value;
#else
                _ = value;
#endif
            }
        }

        internal bool SupportsOpReadFixedForTest => _supportsOpReadFixed;
        internal bool SqPollEnabledForTest => _sqPollEnabled;
        internal IntPtr PortForTest => _port;
        internal long IoUringCancelQueueLengthForTest
        {
            get => Interlocked.Read(ref _ioUringCancelQueueLength);
            set
            {
#if DEBUG
                Interlocked.Exchange(ref _ioUringCancelQueueLength, value);
#else
                _ = value;
#endif
            }
        }

        internal long IoUringCancelQueueOverflowCountForTest => Interlocked.Read(ref _ioUringCancelQueueOverflowCount);
        internal long IoUringCancelQueueWakeRetryCountForTest
        {
            get
            {
#if DEBUG
                return Interlocked.Read(ref _testCancelQueueWakeRetryCount);
#else
                _ = _ioUringInitialized;
                return 0;
#endif
            }
        }
        internal int IoUringWakeupRequestedForTest
        {
            get => unchecked((int)Volatile.Read(ref _ioUringWakeupGeneration));
            set
            {
#if DEBUG
                Volatile.Write(ref _ioUringWakeupGeneration, unchecked((uint)value));
#else
                _ = value;
#endif
            }
        }

        internal bool TryEnqueueIoUringCancellationForTest(ulong userData) => TryEnqueueIoUringCancellation(userData);
        internal Interop.Error SubmitIoUringOperationsNormalizedForTest() => SubmitIoUringOperationsNormalized();

        internal bool SqNeedWakeupForTest() => SqNeedWakeup();

        internal unsafe uint* GetManagedSqFlagsPointerForTest() => _managedSqFlagsPtr;

        internal static bool IsIoUringMultishotRecvSupportedForTest()
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled && engine._supportsMultishotRecv)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsIoUringMultishotAcceptSupportedForTest()
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled && engine._supportsMultishotAccept)
                {
                    return true;
                }
            }

            return false;
        }

        internal static IoUringProvidedBufferSnapshotForTest GetIoUringProvidedBufferSnapshotForTest()
        {
            bool hasIoUringPort = false;
            bool supportsProvidedBufferRings = false;
            bool hasProvidedBufferRing = false;
            bool hasRegisteredBuffers = false;
            bool adaptiveBufferSizingEnabled = false;
            int availableCount = 0;
            int inUseCount = 0;
            int totalBufferCount = 0;
            int bufferSize = 0;
            int recommendedBufferSize = 0;
            long recycledCount = 0;
            long allocationFailureCount = 0;

            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled)
                {
                    continue;
                }

                hasIoUringPort = true;
                if (!engine._ioUringCapabilities.SupportsProvidedBufferRings)
                {
                    continue;
                }

                supportsProvidedBufferRings = true;
                adaptiveBufferSizingEnabled |= engine._adaptiveBufferSizingEnabled;
                hasRegisteredBuffers |= engine._ioUringCapabilities.HasRegisteredBuffers;

                IoUringProvidedBufferRing? providedBufferRing = engine._ioUringProvidedBufferRing;
                if (providedBufferRing is null)
                {
                    continue;
                }

                hasProvidedBufferRing = true;
                availableCount += providedBufferRing.AvailableCount;
                inUseCount += providedBufferRing.InUseCount;
                recycledCount += providedBufferRing.RecycledCount;
                allocationFailureCount += providedBufferRing.AllocationFailureCount;
                bufferSize = Math.Max(bufferSize, providedBufferRing.BufferSize);
                recommendedBufferSize = Math.Max(recommendedBufferSize, providedBufferRing.RecommendedBufferSize);
                totalBufferCount += providedBufferRing.TotalBufferCountForTest;
            }

            return new IoUringProvidedBufferSnapshotForTest(
                hasIoUringPort,
                supportsProvidedBufferRings,
                hasProvidedBufferRing,
                hasRegisteredBuffers,
                adaptiveBufferSizingEnabled,
                availableCount,
                inUseCount,
                totalBufferCount,
                bufferSize,
                recommendedBufferSize,
                recycledCount,
                allocationFailureCount);
        }

        internal static IoUringZeroCopySendSnapshotForTest GetIoUringZeroCopySendSnapshotForTest()
        {
            bool hasIoUringPort = false;
            bool supportsSendZc = false;
            bool supportsSendMsgZc = false;
            bool zeroCopySendEnabled = false;

            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled)
                {
                    continue;
                }

                hasIoUringPort = true;
                supportsSendZc |= engine._supportsOpSendZc;
                supportsSendMsgZc |= engine._supportsOpSendMsgZc;
                zeroCopySendEnabled |= engine._zeroCopySendEnabled;
            }

            return new IoUringZeroCopySendSnapshotForTest(
                hasIoUringPort,
                supportsSendZc,
                supportsSendMsgZc,
                zeroCopySendEnabled);
        }

        internal static IoUringFixedRecvSnapshotForTest GetIoUringFixedRecvSnapshotForTest()
        {
            bool hasIoUringPort = false;
            bool supportsReadFixed = false;
            bool hasRegisteredBuffers = false;

            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled)
                {
                    continue;
                }

                hasIoUringPort = true;
                supportsReadFixed |= engine._supportsOpReadFixed;
                hasRegisteredBuffers |= engine._ioUringCapabilities.HasRegisteredBuffers;
            }

            return new IoUringFixedRecvSnapshotForTest(
                hasIoUringPort,
                supportsReadFixed,
                hasRegisteredBuffers);
        }

        internal static IoUringSqPollSnapshotForTest GetIoUringSqPollSnapshotForTest()
        {
            bool hasIoUringPort = false;
            bool sqPollEnabled = false;
            bool deferTaskrunEnabled = false;

            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled)
                {
                    continue;
                }

                hasIoUringPort = true;
                sqPollEnabled |= engine._sqPollEnabled;
                deferTaskrunEnabled |= (engine._managedNegotiatedFlags & IoUringConstants.SetupDeferTaskrun) != 0;
            }

            return new IoUringSqPollSnapshotForTest(hasIoUringPort, sqPollEnabled, deferTaskrunEnabled);
        }

        internal static bool IsAnyIoUringSqPollEngineNeedingWakeupForTest()
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled &&
                    engine._sqPollEnabled &&
                    engine.SqNeedWakeup())
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryValidateSqNeedWakeupMatchesRawSqFlagBitForTest(out bool matches)
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled || !engine._sqPollEnabled)
                {
                    continue;
                }

                bool methodValue = engine.SqNeedWakeup();
                if (engine._managedSqFlagsPtr == null)
                {
                    matches = methodValue;
                    return true;
                }

                bool rawValue = (Volatile.Read(ref *engine._managedSqFlagsPtr) & IoUringConstants.SqNeedWakeup) != 0;
                matches = rawValue == methodValue;
                return true;
            }

            matches = true;
            return false;
        }

        internal static IoUringZeroCopyPinHoldSnapshotForTest GetIoUringZeroCopyPinHoldSnapshotForTest()
        {
            bool hasIoUringPort = false;
            int activePinHolds = 0;
            int pendingNotificationCount = 0;

            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled)
                {
                    continue;
                }

                hasIoUringPort = true;
                MemoryHandle[]? pinHolds = engine._zeroCopyPinHolds;
                if (pinHolds is not null)
                {
                    for (int i = 0; i < pinHolds.Length; i++)
                    {
                        if (!pinHolds[i].Equals(default(MemoryHandle)))
                        {
                            activePinHolds++;
                        }
                    }
                }

                pendingNotificationCount += engine.CountZeroCopyNotificationPendingSlotsForTest();
            }

            return new IoUringZeroCopyPinHoldSnapshotForTest(
                hasIoUringPort,
                activePinHolds,
                pendingNotificationCount);
        }

        /// <summary>Counts completion slots currently waiting for SEND_ZC NOTIF CQEs (test-only).</summary>
        private int CountZeroCopyNotificationPendingSlotsForTest()
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return 0;
            }

            int pendingNotificationSlots = 0;
            for (int i = 0; i < completionEntries.Length; i++)
            {
                ref IoUringCompletionSlot slot = ref completionEntries[i];
                if (slot.IsZeroCopySend && slot.ZeroCopyNotificationPending)
                {
                    pendingNotificationSlots++;
                }
            }

            return pendingNotificationSlots;
        }

        internal static bool TryForceIoUringProvidedBufferRingExhaustionForTest(out int forcedBufferCount)
        {
#if DEBUG
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (TryForceIoUringProvidedBufferRingExhaustionForCurrentEngineForTest(engine, out forcedBufferCount))
                {
                    return true;
                }
            }

            forcedBufferCount = 0;
            return false;
#else
            forcedBufferCount = 0;
            return false;
#endif
        }

        internal static bool TryRecycleForcedIoUringProvidedBufferRingForTest(out int recycledBufferCount)
        {
#if DEBUG
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (TryRecycleForcedIoUringProvidedBufferRingForCurrentEngineForTest(engine, out recycledBufferCount))
                {
                    return true;
                }
            }

            recycledBufferCount = 0;
            return false;
#else
            recycledBufferCount = 0;
            return false;
#endif
        }

        private static bool TryForceIoUringProvidedBufferRingExhaustionForCurrentEngineForTest(
            SocketAsyncEngine engine,
            out int forcedBufferCount)
        {
#if DEBUG
            IoUringProvidedBufferRing? providedBufferRing = engine._ioUringProvidedBufferRing;
            if (!engine.IsIoUringCompletionModeEnabled ||
                !engine._ioUringCapabilities.SupportsProvidedBufferRings ||
                providedBufferRing is null)
            {
                forcedBufferCount = 0;
                return false;
            }

            // Test-only deterministic exhaustion setup to force provided-buffer depletion paths.
            providedBufferRing.ForceAllBuffersCheckedOutForTest();
            forcedBufferCount = providedBufferRing.TotalBufferCountForTest;
            return true;
#else
            forcedBufferCount = 0;
            return false;
#endif
        }

        private static bool TryRecycleForcedIoUringProvidedBufferRingForCurrentEngineForTest(
            SocketAsyncEngine engine,
            out int recycledBufferCount)
        {
#if DEBUG
            IoUringProvidedBufferRing? providedBufferRing = engine._ioUringProvidedBufferRing;
            if (!engine.IsIoUringCompletionModeEnabled ||
                !engine._ioUringCapabilities.SupportsProvidedBufferRings ||
                providedBufferRing is null)
            {
                recycledBufferCount = 0;
                return false;
            }

            recycledBufferCount = providedBufferRing.RecycleCheckedOutBuffersForTeardown();
            return true;
#else
            recycledBufferCount = 0;
            return false;
#endif
        }

        internal static IoUringNativeMsghdrLayoutSnapshotForTest GetIoUringNativeMsghdrLayoutForTest()
        {
            NativeMsghdr layout = default;
            byte* basePtr = (byte*)&layout;

            return new IoUringNativeMsghdrLayoutSnapshotForTest(
                size: sizeof(NativeMsghdr),
                msgNameOffset: (int)((byte*)&layout.MsgName - basePtr),
                msgNameLengthOffset: (int)((byte*)&layout.MsgNameLen - basePtr),
                msgIovOffset: (int)((byte*)&layout.MsgIov - basePtr),
                msgIovLengthOffset: (int)((byte*)&layout.MsgIovLen - basePtr),
                msgControlOffset: (int)((byte*)&layout.MsgControl - basePtr),
                msgControlLengthOffset: (int)((byte*)&layout.MsgControlLen - basePtr),
                msgFlagsOffset: (int)((byte*)&layout.MsgFlags - basePtr));
        }

        internal static IoUringCompletionSlotLayoutSnapshotForTest GetIoUringCompletionSlotLayoutForTest()
        {
            IoUringCompletionSlot slot = default;
            byte* basePtr = (byte*)&slot;

            return new IoUringCompletionSlotLayoutSnapshotForTest(
                size: sizeof(IoUringCompletionSlot),
                generationOffset: (int)((byte*)&slot.Generation - basePtr),
                freeListNextOffset: (int)((byte*)&slot.FreeListNext - basePtr),
                packedStateOffset: (int)Marshal.OffsetOf(typeof(IoUringCompletionSlot), "_packedState"),
                fixedRecvBufferIdOffset: (int)((byte*)&slot.FixedRecvBufferId - basePtr),
#if DEBUG
                testForcedResultOffset: (int)((byte*)&slot.TestForcedResult - basePtr)
#else
                testForcedResultOffset: -1
#endif
                );
        }

        internal static ulong EncodeCompletionSlotUserDataForTest(int slotIndex, ulong generation) =>
            EncodeCompletionSlotUserData(slotIndex, generation);

        internal static bool TryDecodeCompletionSlotUserDataForTest(ulong userData, out int slotIndex, out ulong generation)
        {
            slotIndex = 0;
            generation = 0;

            if ((byte)(userData >> IoUringUserDataTagShift) != IoUringConstants.TagReservedCompletion)
            {
                return false;
            }

            ulong payload = userData & IoUringUserDataPayloadMask;
            slotIndex = DecodeCompletionSlotIndex(payload);
            generation = (payload >> IoUringConstants.SlotIndexBits) & IoUringConstants.GenerationMask;
            return true;
        }

        internal static ulong IncrementCompletionSlotGenerationForTest(ulong generation)
        {
            ulong nextGeneration = (generation + 1UL) & IoUringConstants.GenerationMask;
            return nextGeneration == 0 ? 1UL : nextGeneration;
        }

        internal static bool IsTrackedIoUringUserDataForTest(ulong userData)
        {
            if (!TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine) ||
                ioUringEngine is null ||
                !ioUringEngine.IsIoUringCompletionModeEnabled ||
                ioUringEngine._trackedOperations is null)
            {
                return false;
            }

            if (!TryDecodeCompletionSlotUserDataForTest(userData, out int slotIndex, out ulong generation))
            {
                return false;
            }

            IoUringTrackedOperationState[] trackedOperations = ioUringEngine._trackedOperations;
            if ((uint)slotIndex >= (uint)trackedOperations.Length)
            {
                return false;
            }

            ref IoUringTrackedOperationState trackedState = ref trackedOperations[slotIndex];
            return Volatile.Read(ref trackedState.TrackedOperationGeneration) == generation &&
                Volatile.Read(ref trackedState.TrackedOperation) is not null;
        }

        [RequiresUnreferencedCode("Uses MethodBase.GetMethodBody() for test-only IL ordering validation.")]
        internal static bool ValidateIoUringProvidedBufferTeardownOrderingForTest()
        {
            MethodInfo teardownMethod = typeof(SocketAsyncEngine).GetMethod("LinuxFreeIoUringResources", BindingFlags.NonPublic | BindingFlags.Instance)!;
            MethodInfo freeProvidedBufferRingMethod = typeof(SocketAsyncEngine).GetMethod("FreeIoUringProvidedBufferRing", BindingFlags.NonPublic | BindingFlags.Instance)!;
            MethodInfo cleanupManagedRingsMethod = typeof(SocketAsyncEngine).GetMethod("CleanupManagedRings", BindingFlags.NonPublic | BindingFlags.Instance)!;

#if DEBUG
            byte[] ilBytes = teardownMethod.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>();
            if (ilBytes.Length == 0)
            {
                return false;
            }

            ReadOnlySpan<byte> il = ilBytes;
            int freeProvidedBufferRingOffset = FindCallMethodTokenOffset(il, freeProvidedBufferRingMethod.MetadataToken);
            int cleanupManagedRingsOffset = FindCallMethodTokenOffset(il, cleanupManagedRingsMethod.MetadataToken);

            return freeProvidedBufferRingOffset >= 0 &&
                cleanupManagedRingsOffset >= 0 &&
                freeProvidedBufferRingOffset < cleanupManagedRingsOffset;
#else
            return true;
#endif
        }

        private static int FindCallMethodTokenOffset(ReadOnlySpan<byte> il, int targetMetadataToken)
        {
            Span<byte> tokenBytes = stackalloc byte[sizeof(int)];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(tokenBytes, targetMetadataToken);

            for (int i = 1; i <= il.Length - sizeof(int); i++)
            {
                // call (0x28) and callvirt (0x6F) are followed by a 4-byte method token.
                byte opcode = il[i - 1];
                if (opcode != 0x28 && opcode != 0x6F)
                {
                    continue;
                }

                if (il.Slice(i, sizeof(int)).SequenceEqual(tokenBytes))
                {
                    return i - 1;
                }
            }

            return -1;
        }

        internal static bool TryInjectIoUringCqOverflowForTest(uint delta, out int injectedEngineCount)
        {
#if DEBUG
            injectedEngineCount = 0;
            if (delta == 0)
            {
                return false;
            }

            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled || engine._managedCqOverflowPtr == null)
                {
                    continue;
                }

                Volatile.Write(
                    ref *engine._managedCqOverflowPtr,
                    unchecked(Volatile.Read(ref *engine._managedCqOverflowPtr) + delta));
                injectedEngineCount++;
            }

            return injectedEngineCount != 0;
#else
            injectedEngineCount = 0;
            return false;
#endif
        }

        internal static bool HasActiveIoUringEngineWithInitializedCqStateForTest()
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled)
                {
                    continue;
                }

                return engine._managedCqRingPtr != null &&
                    engine._managedCqOverflowPtr != null &&
                    engine._completionSlots is not null &&
                    engine._trackedOperations is not null &&
                    engine._completionSlotStorage is not null;
            }

            return false;
        }

        internal static int GetIoUringCompletionSlotsInUseForTest()
        {
            int totalInUse = 0;
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled)
                {
                    totalInUse += Volatile.Read(ref engine._completionSlotsInUse);
                }
            }

            return totalInUse;
        }

        internal static int GetIoUringTrackedOperationCountForTest()
        {
            int totalTracked = 0;
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (!engine.IsIoUringCompletionModeEnabled || engine._trackedOperations is null)
                {
                    continue;
                }

                IoUringTrackedOperationState[] storage = engine._trackedOperations;
                for (int i = 0; i < storage.Length; i++)
                {
                    if (Volatile.Read(ref storage[i].TrackedOperation) is not null)
                    {
                        totalTracked++;
                    }
                }
            }

            return totalTracked;
        }

        internal static bool TryGetIoUringRingFdForTest(out int ringFd)
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled && engine._managedRingFd >= 0)
                {
                    ringFd = engine._managedRingFd;
                    return true;
                }
            }

            ringFd = -1;
            return false;
        }

        internal static bool TryGetIoUringWakeupEventFdForTest(out int eventFd)
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled && engine._managedWakeupEventFd >= 0)
                {
                    eventFd = engine._managedWakeupEventFd;
                    return true;
                }
            }

            eventFd = -1;
            return false;
        }

        internal static bool TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine)
        {
            foreach (SocketAsyncEngine engine in s_engines)
            {
                if (engine.IsIoUringCompletionModeEnabled)
                {
                    ioUringEngine = engine;
                    return true;
                }
            }

            ioUringEngine = null;
            return false;
        }
    }
}
