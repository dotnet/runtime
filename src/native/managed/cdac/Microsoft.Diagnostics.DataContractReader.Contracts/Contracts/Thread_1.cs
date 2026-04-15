// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Thread_1 : IThread
{
    private readonly Target _target;
    private readonly TargetPointer _threadStoreAddr;
    private readonly ulong _threadLinkOffset;

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
        Hijacked = 0x80,
        Background = 0x200,
        Unstarted = 0x400,
        Stopped = 0x10000,
        ThreadPoolWorker = 0x1000000
    }

    internal Thread_1(Target target)
    {
        _target = target;
        _threadStoreAddr = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.ThreadStore));

        // Get the offset into Thread of the SLink. We use this to find the actual
        // first thread from the linked list node contained by the first thread.
        Target.TypeInfo type = _target.GetTypeInfo(DataType.Thread);
        _threadLinkOffset = (ulong)type.Fields[nameof(Data.Thread.LinkNext)].Offset;
    }

    ThreadStoreData IThread.GetThreadStoreData()
    {
        Data.ThreadStore threadStore = _target.ProcessedData.GetOrAdd<Data.ThreadStore>(_threadStoreAddr);
        return new ThreadStoreData(
            threadStore.ThreadCount,
            GetThreadFromLink(threadStore.FirstThreadLink),
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
        if (state.HasFlag(ThreadState_1.Hijacked))
            result |= Contracts.ThreadState.Hijacked;
        if (state.HasFlag(ThreadState_1.Background))
            result |= Contracts.ThreadState.Background;
        if (state.HasFlag(ThreadState_1.Unstarted))
            result |= Contracts.ThreadState.Unstarted;
        if (state.HasFlag(ThreadState_1.Stopped))
            result |= Contracts.ThreadState.Stopped;
        if (state.HasFlag(ThreadState_1.ThreadPoolWorker))
            result |= Contracts.ThreadState.ThreadPoolWorker;
        return result;
    }

    ThreadData IThread.GetThreadData(TargetPointer threadPointer)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);

        TargetPointer address = _target.ReadPointer(thread.ExceptionTracker);
        TargetPointer firstNestedException = TargetPointer.Null;
        if (address != TargetPointer.Null)
        {
            Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(address);
            firstNestedException = exceptionInfo.PreviousNestedInfo;
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
            thread.LastThrownObject.Handle,
            GetThreadFromLink(thread.LinkNext));
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
        Target.TypeInfo type = _target.GetTypeInfo(DataType.Thread);

        stackBase = thread.CachedStackBase;
        stackLimit = thread.CachedStackLimit;
        frameAddress = threadPointer + (ulong)type.Fields[nameof(Data.Thread.Frame)].Offset;
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

    private TargetPointer GetThreadFromLink(TargetPointer threadLink)
    {
        if (threadLink == TargetPointer.Null)
            return TargetPointer.Null;

        // Get the address of the thread containing the link
        return new TargetPointer(threadLink - _threadLinkOffset);
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
                    threadLocalStaticBase = _target.ReadPointer(collectibleArray + (ulong)(indexOffset * _target.PointerSize));
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

    private (Data.Thread thread, Data.ExceptionInfo? exceptionInfo) GetThreadExceptionInfo(TargetPointer threadPointer)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);
        TargetPointer exceptionTrackerPtr = _target.ReadPointer(thread.ExceptionTracker);
        Data.ExceptionInfo? exceptionInfo = (exceptionTrackerPtr == TargetPointer.Null) ? null : _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exceptionTrackerPtr);
        return (thread, exceptionInfo);
    }

    TargetPointer IThread.GetCurrentExceptionHandle(TargetPointer threadPointer)
    {
        var (_, exceptionInfo) = GetThreadExceptionInfo(threadPointer);

        if (exceptionInfo == null)
            return TargetPointer.Null;

        if (exceptionInfo.ThrownObjectHandle == TargetPointer.Null || _target.ReadPointer(exceptionInfo.ThrownObjectHandle) == TargetPointer.Null)
            return TargetPointer.Null;

        return exceptionInfo.ThrownObjectHandle;
    }

    byte[] IThread.GetWatsonBuckets(TargetPointer threadPointer)
    {
        TargetPointer readFrom;
        var (thread, exceptionInfo) = GetThreadExceptionInfo(threadPointer);
        if (exceptionInfo == null)
            return Array.Empty<byte>();
        Data.ObjectHandle throwableObject = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(exceptionInfo.ThrownObjectHandle);
        if (throwableObject.Object != TargetPointer.Null)
        {
            Data.Exception exception = _target.ProcessedData.GetOrAdd<Data.Exception>(throwableObject.Object);
            if (exception.WatsonBuckets != TargetPointer.Null)
            {
                readFrom = _target.Contracts.Object.GetArrayData(exception.WatsonBuckets, out _, out _, out _);
            }
            else
            {
                readFrom = thread.UEWatsonBucketTrackerBuckets;
                if (readFrom == TargetPointer.Null)
                {
                    readFrom = exceptionInfo.ExceptionWatsonBucketTrackerBuckets;
                }
                else
                {
                    return Array.Empty<byte>();
                }
            }
        }
        else
        {
            readFrom = thread.UEWatsonBucketTrackerBuckets;
        }

        if (readFrom == TargetPointer.Null)
            return Array.Empty<byte>();

        byte[] rval = new byte[_target.ReadGlobal<uint>(Constants.Globals.SizeOfGenericModeBlock)];
        _target.ReadBuffer(readFrom, rval);
        return rval;
    }
}
