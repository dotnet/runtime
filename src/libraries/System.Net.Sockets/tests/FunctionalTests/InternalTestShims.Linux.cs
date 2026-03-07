// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace System.Net.Sockets
{
    /// <summary>
    /// Linux test-only shim that mirrors internal SocketAsyncEngine test hooks through reflection.
    /// </summary>
    internal sealed class SocketAsyncEngine
    {
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Keep shim type initialization inert: all reflection is resolved lazily per call.
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Net.Sockets.SocketAsyncEngine", "System.Net.Sockets")]
        static SocketAsyncEngine()
        {
        }

        private readonly object _inner;

        private SocketAsyncEngine(object inner)
        {
            _inner = inner;
        }

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

        internal static IoUringNonPinnableFallbackPublicationState GetIoUringNonPinnableFallbackPublicationStateForTest()
        {
            object state = InvokeStatic("GetIoUringNonPinnableFallbackPublicationStateForTest")!;
            return new IoUringNonPinnableFallbackPublicationState(
                ReadProperty<long>(state, "PublishedCount"),
                ReadProperty<int>(state, "PublishingGate"),
                ReadProperty<long>(state, "FallbackCount"));
        }

        internal static void SetIoUringNonPinnableFallbackPublicationStateForTest(IoUringNonPinnableFallbackPublicationState state)
        {
            MethodInfo setter = GetRequiredMethod(GetEngineType(), "SetIoUringNonPinnableFallbackPublicationStateForTest", StaticFlags);
            Type stateType = setter.GetParameters()[0].ParameterType;
            ConstructorInfo constructor = stateType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(long), typeof(int), typeof(long) },
                modifiers: null) ?? throw new MissingMethodException(stateType.FullName, ".ctor(long,int,long)");

            object rawState = constructor.Invoke(new object[] { state.PublishedCount, state.PublishingGate, state.FallbackCount });
            _ = setter.Invoke(null, new object[] { rawState });
        }

        internal static long GetIoUringNonPinnablePrepareFallbackDeltaForTest() => (long)InvokeStatic("GetIoUringNonPinnablePrepareFallbackDeltaForTest")!;
        internal static bool IsIoUringEnabledForTest() => (bool)InvokeStatic("IsIoUringEnabledForTest")!;
        internal static bool IsSqPollRequestedForTest() => (bool)InvokeStatic("IsSqPollRequestedForTest")!;
        internal static bool IsIoUringDirectSqeDisabledForTest() => (bool)InvokeStatic("IsIoUringDirectSqeDisabledForTest")!;
        internal static bool IsZeroCopySendOptedInForTest() => (bool)InvokeStatic("IsZeroCopySendOptedInForTest")!;
        internal static bool IsIoUringRegisterBuffersEnabledForTest() => (bool)InvokeStatic("IsIoUringRegisterBuffersEnabledForTest")!;
        internal static bool IsNativeMsghdrLayoutSupportedForIoUringForTest(int pointerSize, int nativeMsghdrSize) =>
            (bool)InvokeStatic("IsNativeMsghdrLayoutSupportedForIoUringForTest", new object?[] { pointerSize, nativeMsghdrSize })!;
        internal static long GetIoUringPendingRetryQueuedToPrepareQueueCountForTest() => (long)InvokeStatic("GetIoUringPendingRetryQueuedToPrepareQueueCountForTest")!;
        internal static int GetIoUringCancellationQueueCapacityForTest() => (int)InvokeStatic("GetIoUringCancellationQueueCapacityForTest")!;
        internal static bool IsIoUringMultishotRecvSupportedForTest() => (bool)InvokeStatic("IsIoUringMultishotRecvSupportedForTest")!;
        internal static bool IsIoUringMultishotAcceptSupportedForTest() => (bool)InvokeStatic("IsIoUringMultishotAcceptSupportedForTest")!;
        internal static bool HasActiveIoUringEngineWithInitializedCqStateForTest() => (bool)InvokeStatic("HasActiveIoUringEngineWithInitializedCqStateForTest")!;
        internal static int GetIoUringCompletionSlotsInUseForTest() => (int)InvokeStatic("GetIoUringCompletionSlotsInUseForTest")!;
        internal static int GetIoUringTrackedOperationCountForTest() => (int)InvokeStatic("GetIoUringTrackedOperationCountForTest")!;
        internal static bool IsAnyIoUringSqPollEngineNeedingWakeupForTest() => (bool)InvokeStatic("IsAnyIoUringSqPollEngineNeedingWakeupForTest")!;
        internal static bool ValidateIoUringProvidedBufferTeardownOrderingForTest() => (bool)InvokeStatic("ValidateIoUringProvidedBufferTeardownOrderingForTest")!;
        internal static ulong EncodeCompletionSlotUserDataForTest(int slotIndex, ulong generation) =>
            (ulong)InvokeStatic("EncodeCompletionSlotUserDataForTest", new object?[] { slotIndex, generation })!;
        internal static ulong IncrementCompletionSlotGenerationForTest(ulong generation) =>
            (ulong)InvokeStatic("IncrementCompletionSlotGenerationForTest", new object?[] { generation })!;

        internal static bool IsTrackedIoUringUserDataForTest(ulong userData) =>
            (bool)InvokeStatic("IsTrackedIoUringUserDataForTest", new object?[] { userData })!;

        internal static bool TryDecodeCompletionSlotUserDataForTest(ulong userData, out int slotIndex, out ulong generation)
        {
            object?[] args = new object?[] { userData, 0, 0UL };
            bool result = (bool)InvokeStatic("TryDecodeCompletionSlotUserDataForTest", args)!;
            slotIndex = (int)args[1]!;
            generation = (ulong)args[2]!;
            return result;
        }

        internal static IoUringNativeMsghdrLayoutSnapshotForTest GetIoUringNativeMsghdrLayoutForTest()
        {
            object snapshot = InvokeStatic("GetIoUringNativeMsghdrLayoutForTest")!;
            return new IoUringNativeMsghdrLayoutSnapshotForTest(
                ReadProperty<int>(snapshot, "Size"),
                ReadProperty<int>(snapshot, "MsgNameOffset"),
                ReadProperty<int>(snapshot, "MsgNameLengthOffset"),
                ReadProperty<int>(snapshot, "MsgIovOffset"),
                ReadProperty<int>(snapshot, "MsgIovLengthOffset"),
                ReadProperty<int>(snapshot, "MsgControlOffset"),
                ReadProperty<int>(snapshot, "MsgControlLengthOffset"),
                ReadProperty<int>(snapshot, "MsgFlagsOffset"));
        }

        internal static IoUringCompletionSlotLayoutSnapshotForTest GetIoUringCompletionSlotLayoutForTest()
        {
            object snapshot = InvokeStatic("GetIoUringCompletionSlotLayoutForTest")!;
            return new IoUringCompletionSlotLayoutSnapshotForTest(
                ReadProperty<int>(snapshot, "Size"),
                ReadProperty<int>(snapshot, "GenerationOffset"),
                ReadProperty<int>(snapshot, "FreeListNextOffset"),
                ReadProperty<int>(snapshot, "PackedStateOffset"),
                ReadProperty<int>(snapshot, "FixedRecvBufferIdOffset"),
                ReadProperty<int>(snapshot, "TestForcedResultOffset"));
        }

        internal static IoUringProvidedBufferSnapshotForTest GetIoUringProvidedBufferSnapshotForTest()
        {
            object snapshot = InvokeStatic("GetIoUringProvidedBufferSnapshotForTest")!;
            return new IoUringProvidedBufferSnapshotForTest(
                ReadProperty<bool>(snapshot, "HasIoUringPort"),
                ReadProperty<bool>(snapshot, "SupportsProvidedBufferRings"),
                ReadProperty<bool>(snapshot, "HasProvidedBufferRing"),
                ReadProperty<bool>(snapshot, "HasRegisteredBuffers"),
                ReadProperty<bool>(snapshot, "AdaptiveBufferSizingEnabled"),
                ReadProperty<int>(snapshot, "AvailableCount"),
                ReadProperty<int>(snapshot, "InUseCount"),
                ReadProperty<int>(snapshot, "TotalBufferCount"),
                ReadProperty<int>(snapshot, "BufferSize"),
                ReadProperty<int>(snapshot, "RecommendedBufferSize"),
                ReadProperty<long>(snapshot, "RecycledCount"),
                ReadProperty<long>(snapshot, "AllocationFailureCount"));
        }

        internal static IoUringZeroCopySendSnapshotForTest GetIoUringZeroCopySendSnapshotForTest()
        {
            object snapshot = InvokeStatic("GetIoUringZeroCopySendSnapshotForTest")!;
            return new IoUringZeroCopySendSnapshotForTest(
                ReadProperty<bool>(snapshot, "HasIoUringPort"),
                ReadProperty<bool>(snapshot, "SupportsSendZc"),
                ReadProperty<bool>(snapshot, "SupportsSendMsgZc"),
                ReadProperty<bool>(snapshot, "ZeroCopySendEnabled"));
        }

        internal static IoUringFixedRecvSnapshotForTest GetIoUringFixedRecvSnapshotForTest()
        {
            object snapshot = InvokeStatic("GetIoUringFixedRecvSnapshotForTest")!;
            return new IoUringFixedRecvSnapshotForTest(
                ReadProperty<bool>(snapshot, "HasIoUringPort"),
                ReadProperty<bool>(snapshot, "SupportsReadFixed"),
                ReadProperty<bool>(snapshot, "HasRegisteredBuffers"));
        }

        internal static IoUringSqPollSnapshotForTest GetIoUringSqPollSnapshotForTest()
        {
            object snapshot = InvokeStatic("GetIoUringSqPollSnapshotForTest")!;
            return new IoUringSqPollSnapshotForTest(
                ReadProperty<bool>(snapshot, "HasIoUringPort"),
                ReadProperty<bool>(snapshot, "SqPollEnabled"),
                ReadProperty<bool>(snapshot, "DeferTaskrunEnabled"));
        }

        internal static IoUringZeroCopyPinHoldSnapshotForTest GetIoUringZeroCopyPinHoldSnapshotForTest()
        {
            object snapshot = InvokeStatic("GetIoUringZeroCopyPinHoldSnapshotForTest")!;
            return new IoUringZeroCopyPinHoldSnapshotForTest(
                ReadProperty<bool>(snapshot, "HasIoUringPort"),
                ReadProperty<int>(snapshot, "ActivePinHolds"),
                ReadProperty<int>(snapshot, "PendingNotificationCount"));
        }

        internal static bool TryInjectIoUringCqOverflowForTest(uint delta, out int injectedEngineCount)
        {
            object?[] args = new object?[] { delta, 0 };
            bool result = (bool)InvokeStatic("TryInjectIoUringCqOverflowForTest", args)!;
            injectedEngineCount = (int)args[1]!;
            return result;
        }

        internal static bool TryGetIoUringRingFdForTest(out int ringFd)
        {
            object?[] args = new object?[] { -1 };
            bool result = (bool)InvokeStatic("TryGetIoUringRingFdForTest", args)!;
            ringFd = (int)args[0]!;
            return result;
        }

        internal static bool TryGetIoUringWakeupEventFdForTest(out int eventFd)
        {
            object?[] args = new object?[] { -1 };
            bool result = (bool)InvokeStatic("TryGetIoUringWakeupEventFdForTest", args)!;
            eventFd = (int)args[0]!;
            return result;
        }

        internal static bool TryValidateSqNeedWakeupMatchesRawSqFlagBitForTest(out bool matches)
        {
            object?[] args = new object?[] { false };
            bool result = (bool)InvokeStatic("TryValidateSqNeedWakeupMatchesRawSqFlagBitForTest", args)!;
            matches = (bool)args[0]!;
            return result;
        }

        internal static bool TryForceIoUringProvidedBufferRingExhaustionForTest(out int forcedBufferCount)
        {
            object?[] args = new object?[] { 0 };
            bool result = (bool)InvokeStatic("TryForceIoUringProvidedBufferRingExhaustionForTest", args)!;
            forcedBufferCount = (int)args[0]!;
            return result;
        }

        internal static bool TryRecycleForcedIoUringProvidedBufferRingForTest(out int recycledBufferCount)
        {
            object?[] args = new object?[] { 0 };
            bool result = (bool)InvokeStatic("TryRecycleForcedIoUringProvidedBufferRingForTest", args)!;
            recycledBufferCount = (int)args[0]!;
            return result;
        }

        internal static bool TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine)
        {
            object?[] args = new object?[] { null };
            bool result = (bool)InvokeStatic("TryGetFirstIoUringEngineForTest", args)!;
            if (!result || args[0] is null)
            {
                ioUringEngine = null;
                return false;
            }

            ioUringEngine = new SocketAsyncEngine(args[0]);
            return true;
        }

        internal static SocketAsyncEngine[] GetActiveIoUringEnginesForTest()
        {
            Array engines = (Array)InvokeStatic("GetActiveIoUringEnginesForTest")!;
            var wrappers = new SocketAsyncEngine[engines.Length];
            for (int i = 0; i < engines.Length; i++)
            {
                wrappers[i] = new SocketAsyncEngine(engines.GetValue(i)!);
            }

            return wrappers;
        }

        internal static int[] GetEnginePinnedCpuIndicesForTest() =>
            (int[])InvokeStatic("GetEnginePinnedCpuIndicesForTest")!;

        internal static int GetEngineIndexForCpuForTest(int cpuIndex) =>
            (int)InvokeStatic("GetEngineIndexForCpuForTest", cpuIndex)!;

        internal static bool TrySetCurrentThreadAffinityForTest(int cpuIndex) =>
            (bool)InvokeStatic("TrySetCurrentThreadAffinityForTest", cpuIndex)!;

        internal bool SupportsMultishotAcceptForTest
        {
            get => GetInstanceProperty<bool>("SupportsMultishotAcceptForTest");
            set => SetInstanceProperty("SupportsMultishotAcceptForTest", value);
        }

        internal bool SupportsOpSendZcForTest
        {
            get => GetInstanceProperty<bool>("SupportsOpSendZcForTest");
            set => SetInstanceProperty("SupportsOpSendZcForTest", value);
        }

        internal bool ZeroCopySendEnabledForTest
        {
            get => GetInstanceProperty<bool>("ZeroCopySendEnabledForTest");
            set => SetInstanceProperty("ZeroCopySendEnabledForTest", value);
        }

        internal long IoUringCancelQueueLengthForTest
        {
            get => GetInstanceProperty<long>("IoUringCancelQueueLengthForTest");
            set => SetInstanceProperty("IoUringCancelQueueLengthForTest", value);
        }

        internal long IoUringCancelQueueOverflowCountForTest => GetInstanceProperty<long>("IoUringCancelQueueOverflowCountForTest");
        internal long IoUringCancelQueueWakeRetryCountForTest => GetInstanceProperty<long>("IoUringCancelQueueWakeRetryCountForTest");

        internal int IoUringWakeupRequestedForTest
        {
            get => GetInstanceProperty<int>("IoUringWakeupRequestedForTest");
            set => SetInstanceProperty("IoUringWakeupRequestedForTest", value);
        }

        internal bool TryEnqueueIoUringCancellationForTest(ulong userData)
            => (bool)InvokeInstance("TryEnqueueIoUringCancellationForTest", userData)!;

        internal int SubmitIoUringOperationsNormalizedForTest()
            => Convert.ToInt32(InvokeInstance("SubmitIoUringOperationsNormalizedForTest"));

        internal static int EngineCount
        {
            get
            {
                PropertyInfo property = GetRequiredProperty(GetEngineType(), "EngineCount", StaticFlags);
                return (int)property.GetValue(null)!;
            }
        }

        private static object? InvokeStatic(string methodName, params object?[]? args)
        {
            MethodInfo method = GetRequiredMethod(GetEngineType(), methodName, StaticFlags);
            return method.Invoke(null, args);
        }

        private object? InvokeInstance(string methodName, params object?[]? args)
        {
            MethodInfo method = GetRequiredMethod(GetEngineType(), methodName, InstanceFlags);
            return method.Invoke(_inner, args);
        }

        private T GetInstanceProperty<T>(string propertyName)
        {
            PropertyInfo property = GetRequiredProperty(GetEngineType(), propertyName, InstanceFlags);
            return (T)property.GetValue(_inner)!;
        }

        private void SetInstanceProperty(string propertyName, object? value)
        {
            PropertyInfo property = GetRequiredProperty(GetEngineType(), propertyName, InstanceFlags);
            property.SetValue(_inner, value);
        }

        private static T ReadProperty<T>(object instance, string propertyName)
        {
            PropertyInfo property = GetRequiredProperty(instance.GetType(), propertyName, InstanceFlags);
            return (T)property.GetValue(instance)!;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private static Type GetEngineType()
        {
            return typeof(Socket).Assembly.GetType("System.Net.Sockets.SocketAsyncEngine", throwOnError: true, ignoreCase: false)!;
        }

        private static MethodInfo GetRequiredMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string methodName, BindingFlags flags)
        {
            return type.GetMethod(methodName, flags) ?? throw new MissingMethodException(type.FullName, methodName);
        }

        private static PropertyInfo GetRequiredProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string propertyName, BindingFlags flags)
        {
            return type.GetProperty(propertyName, flags) ?? throw new MissingMemberException(type.FullName, propertyName);
        }
    }

    /// <summary>
    /// Linux test-only shim that forwards SocketAsyncContext test hooks through reflection.
    /// </summary>
    internal sealed class SocketAsyncContext
    {
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly object _inner;

        private SocketAsyncContext(object inner)
        {
            _inner = inner;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Net.Sockets.SocketAsyncContext", "System.Net.Sockets")]
        internal static bool IsMultishotAcceptArmedForTest(Socket socket)
            => (bool)GetRequiredMethod(GetContextType(), "IsMultishotAcceptArmedForTest", StaticFlags).Invoke(null, new object[] { socket })!;

        internal static int GetMultishotAcceptQueueCountForTest(Socket socket)
            => (int)GetRequiredMethod(GetContextType(), "GetMultishotAcceptQueueCountForTest", StaticFlags).Invoke(null, new object[] { socket })!;

        internal static bool TryGetIncomingCpuForTest(Socket socket, out int cpu)
        {
            object?[] args = new object?[] { socket, 0 };
            bool result = (bool)GetRequiredMethod(GetContextType(), "TryGetIncomingCpuForTest", StaticFlags).Invoke(null, args)!;
            cpu = (int)args[1]!;
            return result;
        }

        internal static bool IsPersistentMultishotRecvArmedForTest(Socket socket)
            => (bool)GetRequiredMethod(GetContextType(), "IsPersistentMultishotRecvArmedForTest", StaticFlags).Invoke(null, new object[] { socket })!;

        internal static ulong GetPersistentMultishotRecvUserDataForTest(Socket socket)
            => (ulong)GetRequiredMethod(GetContextType(), "GetPersistentMultishotRecvUserDataForTest", StaticFlags).Invoke(null, new object[] { socket })!;

        internal static int GetPersistentMultishotRecvBufferedCountForTest(Socket socket)
            => (int)GetRequiredMethod(GetContextType(), "GetPersistentMultishotRecvBufferedCountForTest", StaticFlags).Invoke(null, new object[] { socket })!;

        internal static int GetReusePortShadowListenerCountForTest(Socket socket)
            => (int)GetRequiredMethod(GetContextType(), "GetReusePortShadowListenerCountForTest", StaticFlags).Invoke(null, new object[] { socket })!;

        internal static bool TryGetSocketAsyncContextForTest(Socket socket, out SocketAsyncContext? context)
        {
            object?[] args = new object?[] { socket, null };
            bool result = (bool)GetRequiredMethod(GetContextType(), "TryGetSocketAsyncContextForTest", StaticFlags).Invoke(null, args)!;
            if (!result || args[1] is null)
            {
                context = null;
                return false;
            }

            context = new SocketAsyncContext(args[1]);
            return true;
        }

        internal bool TryBufferEarlyPersistentMultishotRecvData(byte[] payload)
        {
            MethodInfo method = GetRequiredMethod(GetContextType(), "TryBufferEarlyPersistentMultishotRecvDataForTest", InstanceFlags);
            return (bool)method.Invoke(_inner, new object[] { payload })!;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private static Type GetContextType()
        {
            return typeof(Socket).Assembly.GetType("System.Net.Sockets.SocketAsyncContext", throwOnError: true, ignoreCase: false)!;
        }

        private static MethodInfo GetRequiredMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string methodName, BindingFlags flags)
        {
            return type.GetMethod(methodName, flags) ?? throw new MissingMethodException(type.FullName, methodName);
        }
    }


}
