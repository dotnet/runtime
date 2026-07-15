// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Thread_1 : IThread
{
    private readonly Target _target;
    private readonly TargetPointer _threadStoreAddr;
    private readonly Target.TypeInfo _threadTypeInfo;

    [Flags]
    private enum TLSIndexType
    {
        NonCollectible = 0, // IndexOffset for this form of TLSIndex is scaled by sizeof(OBJECTREF) and used as an index into the array at ThreadLocalData::pNonCollectibleTlsArrayData to get the final address
        Collectible = 1, // IndexOffset for this form of TLSIndex is scaled by sizeof(void*) and then added to ThreadLocalData::pCollectibleTlsArrayData to get the final address
        DirectOnThreadLocalData = 2, // IndexOffset for this form of TLS index is an offset into the ThreadLocalData structure itself. This is used for very high performance scenarios, and scenario where the runtime native code needs to hold a TLS pointer to a managed TLS slot. Each one of these is hand-opted into this model.
    };

    [Flags]
    private enum ThreadState_1
    {
        SuspensionTrapped = 0x2,
        GCSuspendRedirected = 0x4,
        DebugSuspendPending = 0x8,
        Hijacked = 0x80,
        Background = 0x200,
        Unstarted = 0x400,
        CoInitialized = 0x2000,
        InSTA = 0x4000,
        InMTA = 0x8000,
        Stopped = 0x10000,
        DebugSyncSuspended = 0x80000,
        DebugWillSync = 0x100000,
        ThreadPoolWorker = 0x1000000,
        WaitSleepJoin = 0x2000000,
        Detached = unchecked((int)0x80000000)
    }

    [Flags]
    private enum ExceptionFlags
    {
        DebuggerInterceptInfo = 0x00000200,
        IsUnhandled = 0x00000800,
    }

    internal Thread_1(Target target)
    {
        _target = target;
        _threadStoreAddr = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.ThreadStore));
        _threadTypeInfo = target.GetTypeInfo(DataType.Thread);
    }

    void IThread.SetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state)
    {
        Data.Thread t = _target.ProcessedData.GetOrAdd<Data.Thread>(thread);
        t.WriteDebuggerControlledThreadState(t.DebuggerControlledThreadState | (uint)state);
    }

    void IThread.ResetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state)
    {
        Data.Thread t = _target.ProcessedData.GetOrAdd<Data.Thread>(thread);
        t.WriteDebuggerControlledThreadState(t.DebuggerControlledThreadState & ~(uint)state);
    }

    ThreadStoreData IThread.GetThreadStoreData()
    {
        Data.ThreadStore threadStore = _target.ProcessedData.GetOrAdd<Data.ThreadStore>(_threadStoreAddr);
        return new ThreadStoreData(
            threadStore.ThreadCount,
            threadStore.FirstThreadLink,
            _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.FinalizerThread)),
            _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCThread)));
    }

    ThreadStoreCounts IThread.GetThreadCounts()
    {
        Data.ThreadStore threadStore = _target.ProcessedData.GetOrAdd<Data.ThreadStore>(_threadStoreAddr);
        return new ThreadStoreCounts(
            threadStore.UnstartedCount,
            threadStore.BackgroundCount,
            threadStore.PendingCount,
            threadStore.DeadCount);
    }

    private static Contracts.ThreadState GetThreadState(ThreadState_1 state)
    {
        Contracts.ThreadState result = Contracts.ThreadState.Unknown;
        if (state.HasFlag(ThreadState_1.SuspensionTrapped))
            result |= Contracts.ThreadState.SuspensionTrapped;
        if (state.HasFlag(ThreadState_1.GCSuspendRedirected))
            result |= Contracts.ThreadState.GCSuspendRedirected;
        if (state.HasFlag(ThreadState_1.DebugSuspendPending))
            result |= Contracts.ThreadState.DebugSuspendPending;
        if (state.HasFlag(ThreadState_1.Hijacked))
            result |= Contracts.ThreadState.Hijacked;
        if (state.HasFlag(ThreadState_1.Background))
            result |= Contracts.ThreadState.Background;
        if (state.HasFlag(ThreadState_1.Unstarted))
            result |= Contracts.ThreadState.Unstarted;
        if (state.HasFlag(ThreadState_1.CoInitialized))
            result |= Contracts.ThreadState.CoInitialized;
        if (state.HasFlag(ThreadState_1.InSTA))
            result |= Contracts.ThreadState.InSTA;
        if (state.HasFlag(ThreadState_1.InMTA))
            result |= Contracts.ThreadState.InMTA;
        if (state.HasFlag(ThreadState_1.Stopped))
            result |= Contracts.ThreadState.Stopped;
        if (state.HasFlag(ThreadState_1.DebugSyncSuspended))
            result |= Contracts.ThreadState.DebugSyncSuspended;
        if (state.HasFlag(ThreadState_1.DebugWillSync))
            result |= Contracts.ThreadState.DebugWillSync;
        if (state.HasFlag(ThreadState_1.ThreadPoolWorker))
            result |= Contracts.ThreadState.ThreadPoolWorker;
        if (state.HasFlag(ThreadState_1.WaitSleepJoin))
            result |= Contracts.ThreadState.WaitSleepJoin;
        if (state.HasFlag(ThreadState_1.Detached))
            result |= Contracts.ThreadState.Detached;
        return result;
    }

    ThreadData IThread.GetThreadData(TargetPointer threadPointer)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);

        TargetPointer address = _target.ReadPointer(thread.ExceptionTracker);
        TargetPointer firstNestedException = TargetPointer.Null;
        bool hasUnhandledException = false;
        Data.ExceptionInfo? exceptionInfo = null;
        if (address != TargetPointer.Null)
        {
            exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(address);
            firstNestedException = exceptionInfo.PreviousNestedInfo;

            if (exceptionInfo.ThrownObject != TargetPointer.Null)
            {
                uint exceptionFlags = exceptionInfo.ExceptionFlags;
                hasUnhandledException = (exceptionFlags & (uint)ExceptionFlags.IsUnhandled) != 0
                    && (exceptionFlags & (uint)ExceptionFlags.DebuggerInterceptInfo) == 0;
            }
        }

        if (thread.LastThrownObjectIsUnhandled != 0)
            hasUnhandledException = true;

        // Prefer the active exception from ExInfo (pseudo-handle to m_exception field).
        // After the removal of SetThrowable/m_hThrowable, m_LastThrownObjectHandle is only
        // updated after exception dispatch completes, so during active dispatch it may be
        // stale.  The pseudo-handle has the same dereference semantics as a real GC handle.
        TargetPointer lastThrownObjectHandle = GetActiveExceptionPseudoHandle(exceptionInfo, address);
        if (lastThrownObjectHandle == TargetPointer.Null)
        {
            lastThrownObjectHandle = thread.LastThrownObject.Handle;
        }

        return new ThreadData(
            threadPointer,
            thread.Id,
            thread.OSId,
            GetThreadState((ThreadState_1)thread.State),
            (thread.PreemptiveGCDisabled & 0x1) != 0,
            thread.RuntimeThreadLocals?.AllocContext.GCAllocationContext.Pointer ?? TargetPointer.Null,
            thread.RuntimeThreadLocals?.AllocContext.GCAllocationContext.Limit ?? TargetPointer.Null,
            thread.Frame,
            firstNestedException,
            thread.ExposedObject.Handle,
            lastThrownObjectHandle,
            thread.CurrentCustomDebuggerNotification.Handle,
            thread.LastThrownObjectIsUnhandled != 0,
            hasUnhandledException,
            thread.LinkNext,
            thread.ThreadHandle,
            thread.InteropDebuggingHijacked != 0,
            thread.DebuggerFilterContext,
            thread.GCFrame,
            address != TargetPointer.Null,
            exceptionInfo?.ExceptionRecord ?? TargetPointer.Null,
            exceptionInfo?.ContextRecord ?? TargetPointer.Null);
    }

    void IThread.GetThreadAllocContext(TargetPointer threadPointer, out long allocBytes, out long allocBytesLoh)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);

        allocBytes = thread.RuntimeThreadLocals?.AllocContext.GCAllocationContext.AllocBytes ?? 0;
        allocBytesLoh = thread.RuntimeThreadLocals?.AllocContext.GCAllocationContext.AllocBytesLoh ?? 0;
    }

    void IThread.GetStackLimitData(TargetPointer threadPointer, out TargetPointer stackBase, out TargetPointer stackLimit, out TargetPointer frameAddress)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);

        stackBase = thread.CachedStackBase;
        stackLimit = thread.CachedStackLimit;
        frameAddress = threadPointer + (ulong)_threadTypeInfo.Fields[nameof(Data.Thread.Frame)].Offset;
    }

    // happens inside critical section
    TargetPointer IThread.IdToThread(uint id)
    {
        TargetPointer idDispenserPtr = _target.ReadGlobalPointer(Constants.Globals.ThinlockThreadIdDispenser);
        TargetPointer idDispenser = _target.ReadPointer(idDispenserPtr);
        Data.IdDispenser idDispenserObj = _target.ProcessedData.GetOrAdd<Data.IdDispenser>(idDispenser);
        TargetPointer threadPtr = TargetPointer.Null;
        if (id < idDispenserObj.HighestId)
            threadPtr = _target.ReadPointer(idDispenserObj.IdToThread + (ulong)(id * _target.PointerSize));
        return threadPtr;
    }

    TargetPointer IThread.GetThreadLocalStaticBase(TargetPointer threadPointer, TargetPointer tlsIndexPtr)
    {
        // Get the thread's TLS base address
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);
        TargetPointer threadLocalDataPtr = thread.ThreadLocalDataPtr;
        if (threadLocalDataPtr == TargetPointer.Null)
            return TargetPointer.Null;

        Data.TLSIndex tlsIndex = _target.ProcessedData.GetOrAdd<Data.TLSIndex>(tlsIndexPtr);
        if (!tlsIndex.IsAllocated)
            return TargetPointer.Null;

        TargetPointer threadLocalStaticBase = default;
        Data.ThreadLocalData threadLocalData = _target.ProcessedData.GetOrAdd<Data.ThreadLocalData>(threadLocalDataPtr);
        int indexOffset = tlsIndex.IndexOffset;
        int indexType = tlsIndex.IndexType;
        switch ((TLSIndexType)indexType)
        {
            case TLSIndexType.NonCollectible:
                int nonCollectibleCount = threadLocalData.NonCollectibleTlsDataCount;
                // bounds check
                if (nonCollectibleCount > indexOffset)
                {
                    TargetPointer nonCollectibleArray = threadLocalData.NonCollectibleTlsArrayData;
                    int arrayIndex = indexOffset - _target.ReadGlobal<byte>(Constants.Globals.NumberOfTlsOffsetsNotUsedInNoncollectibleArray);
                    TargetPointer arrayStartAddress = nonCollectibleArray + _target.ReadGlobalPointer(Constants.Globals.PtrArrayOffsetToDataArray);
                    threadLocalStaticBase = _target.ReadPointer(arrayStartAddress + (ulong)(arrayIndex * _target.PointerSize));
                }
                break;
            case TLSIndexType.Collectible:
                int collectibleCount = threadLocalData.CollectibleTlsDataCount;
                if (collectibleCount > indexOffset)
                {
                    TargetPointer collectibleArray = threadLocalData.CollectibleTlsArrayData;
                    TargetPointer handleSlotAddress = collectibleArray + (ulong)(indexOffset * _target.PointerSize);
                    threadLocalStaticBase = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(handleSlotAddress).Object;
                }
                break;
            case TLSIndexType.DirectOnThreadLocalData:
                threadLocalStaticBase = threadLocalDataPtr + (ulong)indexOffset;
                break;
        }
        if (threadLocalStaticBase == TargetPointer.Null)
        {
            TargetPointer inFlightData = threadLocalData.InFlightData;
            while (inFlightData != TargetPointer.Null)
            {
                Data.InflightTLSData inFlightTLSData = _target.ProcessedData.GetOrAdd<Data.InflightTLSData>(inFlightData);
                if (inFlightTLSData.TlsIndex.TLSIndexRawIndex == tlsIndex.TLSIndexRawIndex)
                {
                    threadLocalStaticBase = inFlightTLSData.TLSData.Object;
                    break;
                }
                inFlightData = inFlightTLSData.Next;
            }
        }
        return threadLocalStaticBase;
    }

    private (Data.Thread thread, Data.ExceptionInfo? exceptionInfo, TargetPointer exceptionTrackerAddr) GetThreadExceptionInfo(TargetPointer threadPointer)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);
        TargetPointer exceptionTrackerPtr = _target.ReadPointer(thread.ExceptionTracker);
        Data.ExceptionInfo? exceptionInfo = (exceptionTrackerPtr == TargetPointer.Null) ? null : _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exceptionTrackerPtr);
        return (thread, exceptionInfo, exceptionTrackerPtr);
    }

    /// <summary>
    /// Returns the target address of the ExInfo::m_exception field as a pseudo-handle
    /// if there is an active exception tracker with a non-null thrown object.
    /// Callers dereference this address to read the exception Object*, just like a real
    /// GC handle.  Returns TargetPointer.Null when no active exception is present.
    /// </summary>
    private TargetPointer GetActiveExceptionPseudoHandle(Data.ExceptionInfo? exceptionInfo, TargetPointer exceptionTrackerAddr)
    {
        if (exceptionInfo is null || exceptionInfo.ThrownObject == TargetPointer.Null)
            return TargetPointer.Null;

        Target.TypeInfo type = _target.GetTypeInfo(DataType.ExceptionInfo);
        return exceptionTrackerAddr + (ulong)type.Fields[nameof(Data.ExceptionInfo.ThrownObject)].Offset;
    }

    TargetPointer IThread.GetCurrentExceptionHandle(TargetPointer threadPointer)
    {
        var (_, exceptionInfo, exceptionTrackerAddr) = GetThreadExceptionInfo(threadPointer);
        return GetActiveExceptionPseudoHandle(exceptionInfo, exceptionTrackerAddr);
    }
}
