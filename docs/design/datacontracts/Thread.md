# Contract Thread

This contract is for reading and iterating the threads of the process.

## APIs of contract

``` csharp
record struct ThreadStoreData (
    int ThreadCount,
    TargetPointer FirstThread,
    TargetPointer FinalizerThread,
    TargetPointer GcThread);

record struct ThreadStoreCounts (
    int UnstartedThreadCount,
    int BackgroundThreadCount,
    int PendingThreadCount,
    int DeadThreadCount);

enum ThreadState
{
    Unknown             = 0x00000000,    // threads are initialized this way
    Hijacked            = 0x00000080,    // Return address has been hijacked
    Background          = 0x00000200,    // Thread is a background thread
    Unstarted           = 0x00000400,    // Thread has never been started
    Dead                = 0x00000800,    // Thread is dead
    ThreadPoolWorker    = 0x01000000,    // is this a threadpool worker thread?
}

record struct ThreadData (
    uint Id;
    TargetNUInt OSId;
    ThreadState State;
    bool PreemptiveGCDisabled
    TargetPointer AllocContextPointer;
    TargetPointer AllocContextLimit;
    TargetPointer Frame;
    TargetPointer FirstNestedException;
    TargetPointer TEB;
    TargetPointer LastThrownObjectHandle;
    TargetPointer NextThread;
);
```

``` csharp
ThreadStoreData GetThreadStoreData();
ThreadStoreCounts GetThreadCounts();
ThreadData GetThreadData(TargetPointer threadPointer);
TargetPointer IdToThread(uint id);
TargetPointer GetThreadLocalStaticBase(TargetPointer threadPointer, int indexOffset, int indexType);
```

## Version 1

The contract depends on the following globals

| Global name | Type | Meaning |
| --- | --- |
| `AppDomain` | TargetPointer | A pointer to the address of the one AppDomain
| `ThreadStore` | TargetPointer | A pointer to the address of the ThreadStore
| `FeatureEHFunclets` | TargetPointer | 1 if EH funclets are enabled, 0 otherwise
| `FinalizerThread` | TargetPointer | A pointer to the finalizer thread
| `GCThread` | TargetPointer | A pointer to the GC thread
| `ThinLockThreadIdDispenser` | TargetPointer | Dispenser of thinlock IDs for locking objects
| `NumberOfTlsOffsetsNotUsedInNoncollectibleArray` | byte | Number of unused slots in noncollectible TLS array
| `PtrArrayOffsetToDataArray` | TargetPointer | Offset from PtrArray class address to start of enclosed data array
| `SizeOfGenericModeBlock` | uint32 | Size of GenericModeBlock struct

The contract additionally depends on these data descriptors

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Exception` | `WatsonBuckets` | Pointer to exception Watson buckets |
| `ExceptionInfo` | `PreviousNestedInfo` | Pointer to previous nested exception info |
| `ExceptionInfo` | `ThrownObjectHandle` | Pointer to exception object handle |
| `ExceptionInfo` | `ExceptionWatsonBucketTrackerBuckets` | Pointer to Watson unhandled buckets on non-Unix |
| `GCAllocContext` | `Pointer` | GC allocation pointer |
| `GCAllocContext` | `Limit` | Allocation limit pointer |
| `IdDispenser` | `HighestId` | Highest possible small thread ID |
| `IdDispenser` | `IdToThread` | Array mapping small thread IDs to thread pointers |
| `InflightTLSData` | `Next` | Pointer to next in-flight TLS data entry |
| `InflightTLSData` | `TlsIndex` | TLS index for the in-flight static field |
| `InflightTLSData` | `TLSData` | Object handle to the TLS data for the static field |
| `ObjectHandle` | `Object` | Pointer to the managed object |
| `RuntimeThreadLocals` | `AllocContext` | GC allocation context for the thread |
| `TLSIndex` | `IndexOffset` | Offset index for thread local storage |
| `TLSIndex` | `IndexType` | Type of thread local storage index |
| `TLSIndex` | `IsAllocated` | Whether TLS storage has been allocated |
| `TLSIndex` | `TLSIndexRawIndex` | Raw index value containing type and offset |
| `Thread` | `Id` | Thread identifier |
| `Thread` | `OSId` | Operating system thread identifier |
| `Thread` | `State` | Thread state flags |
| `Thread` | `PreemptiveGCDisabled` | Flag indicating if preemptive GC is disabled |
| `Thread` | `Frame` | Pointer to current frame |
| `Thread` | `TEB` | Thread Environment Block pointer |
| `Thread` | `LastThrownObject` | Handle to last thrown exception object |
| `Thread` | `LinkNext` | Pointer to get next thread |
| `Thread` | `ExceptionTracker` | Pointer to exception tracking information |
| `Thread` | `RuntimeThreadLocals` | Pointer to some thread-local storage |
| `Thread` | `ThreadLocalDataPtr` | Pointer to thread local data structure |
| `Thread` | `UEWatsonBucketTrackerBuckets` | Pointer to thread Watson buckets data |
| `ThreadLocalData` | `NonCollectibleTlsData` | Count of non-collectible TLS data entries |
| `ThreadLocalData` | `NonCollectibleTlsArrayData` | Pointer to non-collectible TLS array data |
| `ThreadLocalData` | `CollectibleTlsData` | Count of collectible TLS data entries |
| `ThreadLocalData` | `CollectibleTlsArrayData` | Pointer to collectible TLS array data |
| `ThreadLocalData` | `InFlightData` | Pointer to in-flight TLS data for fields being initialized |
| `ThreadStore` | `ThreadCount` | Number of threads |
| `ThreadStore` | `FirstThreadLink` | Pointer to first thread in the linked list |
| `ThreadStore` | `UnstartedCount` | Number of unstarted threads |
| `ThreadStore` | `BackgroundCount` | Number of background threads |
| `ThreadStore` | `PendingCount` | Number of pending threads |
| `ThreadStore` | `DeadCount` | Number of dead threads |

The contract depends on the following other contracts

| Contract |
| --- |
| Object |

``` csharp
enum TLSIndexType
{
    NonCollectible = 0,
    Collectible = 1,
    DirectOnThreadLocalData = 2,
};


ThreadStoreData GetThreadStoreData()
{
    TargetPointer threadStore = target.ReadGlobalPointer("ThreadStore");

    ulong threadLinkoffset = ... // offset from Thread data descriptor
    return new ThreadStoreData(
        ThreadCount: target.Read<int>(threadStore + /* ThreadStore::ThreadCount offset */),
        FirstThread: target.ReadPointer(threadStore + /* ThreadStore::FirstThreadLink offset */ - threadLinkoffset),
        FinalizerThread: target.ReadGlobalPointer("FinalizerThread"),
        GCThread: target.ReadGlobalPointer("GCThread"));
}

DacThreadStoreCounts GetThreadCounts()
{
    TargetPointer threadStore = target.ReadGlobalPointer("ThreadStore");

    return new ThreadStoreCounts(
        UnstartedThreadCount: target.Read<int>(threadStore + /* ThreadStore::UnstartedCount offset */),
        BackgroundThreadCount: target.Read<int>(threadStore + /* ThreadStore::BackgroundCount offset */),,
        PendingThreadCount: target.Read<int>(threadStore + /* ThreadStore::PendingCount offset */),,
        DeadThreadCount: target.Read<int>(threadStore + /* ThreadStore::DeadCount offset */),,
}

ThreadData GetThreadData(TargetPointer address)
{
    var runtimeThread = new Thread(target, threadPointer);

    // Exception tracker is a pointer when EH funclets are enabled
    TargetPointer exceptionTrackerAddr = target.ReadGlobal<byte>("FeatureEHFunclets") != 0
        ? target.ReadPointer(address + /* Thread::ExceptionTracker offset */)
        : address + /* Thread::ExceptionTracker offset */;
    TargetPointer firstNestedException = exceptionTrackerAddr != TargetPointer.Null
        ? target.ReadPointer(exceptionTrackerAddr + /* ExceptionInfo::PreviousNestedInfo offset*/)
        : TargetPointer.Null;

    TargetPointer allocContextPointer = TargetPointer.Null;
    TargetPointer allocContextLimit = TargetPointer.Null;
    TargetPointer threadLocals = target.ReadPointer(address + /* Thread::RuntimeThreadLocals offset */);
    if (threadLocals != TargetPointer.Null)
    {
        allocContextPointer = target.ReadPointer(threadLocals + /* RuntimeThreadLocals::AllocContext offset */ + /* GCAllocContext::Pointer offset */);
        allocContextLimit = target.ReadPointer(threadLocals + /* RuntimeThreadLocals::AllocContext offset */ + /* GCAllocContext::Limit offset */);
    }

    ulong threadLinkoffset = ... // offset from Thread data descriptor
    return new ThreadData(
        Id: target.Read<uint>(address + /* Thread::Id offset */),
        OSId: target.ReadNUInt(address + /* Thread::OSId offset */),
        State: target.Read<uint>(address + /* Thread::State offset */),
        PreemptiveGCDisabled: (target.Read<uint>(address + /* Thread::PreemptiveGCDisabled offset */) & 0x1) != 0,
        AllocContextPointer: allocContextPointer,
        AllocContextLimit: allocContextLimit,
        Frame: target.ReadPointer(address + /* Thread::Frame offset */),
        TEB : /* Has Thread::TEB offset */ ? target.ReadPointer(address + /* Thread::TEB offset */) : TargetPointer.Null,
        LastThrownObjectHandle : target.ReadPointer(address + /* Thread::LastThrownObject offset */),
        FirstNestedException : firstNestedException,
        NextThread: target.ReadPointer(address + /* Thread::LinkNext offset */) - threadLinkOffset;
    );
}

TargetPointer IThread.IdToThread(uint id)
{
    TargetPointer idDispenserPointer = target.ReadGlobalPointer(Constants.Globals.ThinlockThreadIdDispenser);
    TargetPointer idDispenser = target.ReadPointer(idDispenserPointer);
    uint HighestId = target.ReadPointer(idDispenser + /* IdDispenser::HighestId offset */);
    TargetPointer threadPtr = TargetPointer.Null;
    if (id < HighestId)
        threadPtr = target.ReadPointer(idDispenser + /* IdDispenser::IdToThread offset + (index into IdToThread array * size of array elements (== size of target pointer)) */);
    return threadPtr;
}

TargetPointer IThread.GetThreadLocalStaticBase(TargetPointer threadPointer, TargetPointer tlsIndexPtr)
{
    // Get the thread's TLS base address
    TargetPointer threadLocalDataPtr = target.ReadPointer(threadPointer + /* Thread::ThreadLocalDataPtr offset */);
    if (threadLocalDataPtr == TargetPointer.Null)
        return TargetPointer.Null;

    Data.TLSIndex tlsIndex = new Data.TLSIndex(tlsIndexPtr);
    if (!tlsIndex.IsAllocated)
        return TargetPointer.Null;

    TargetPointer threadLocalStaticBase = default;
    int indexType = tlsIndex.IndexType;
    int indexOffset = tlsIndex.IndexOffset;
    switch ((TLSIndexType)indexType)
    {
        case TLSIndexType.NonCollectible:
            int nonCollectibleCount = target.ReadPointer(threadLocalDataPtr + /* ThreadLocalData::NonCollectibleTlsDataCount offset */);
            // bounds check
            if (nonCollectibleCount > indexOffset)
            {
                TargetPointer nonCollectibleArray = target.ReadPointer(threadLocalDataPtr + /* ThreadLocalData::NonCollectibleTlsArrayData offset */);
                int arrayIndex = indexOffset - target.ReadGlobal<byte>("NumberOfTlsOffsetsNotUsedInNoncollectibleArray");
                TargetPointer arrayStartAddress = nonCollectibleArray + target.ReadGlobalPointer("PtrArrayOffsetToDataArray");
                threadLocalStaticBase = target.ReadPointer(arrayStartAddress + (ulong)(arrayIndex * target.PointerSize));
            }
            break;
        case TLSIndexType.Collectible:
            int collectibleCount = target.ReadPointer(threadLocalDataPtr + /* ThreadLocalData::CollectibleTlsDataCount offset */);
            if (collectibleCount > indexOffset)
            {
                TargetPointer collectibleArray = target.ReadPointer(threadLocalDataPtr + /* ThreadLocalData::CollectibleTlsArrayData offset */);
                threadLocalStaticBase = target.ReadPointer(collectibleArray + (ulong)(indexOffset * target.PointerSize));
            }
            break;
        case TLSIndexType.DirectOnThreadLocalData:
            threadLocalStaticBase = threadLocalDataPtr;
            break;
    }
    if (threadLocalStaticBase == TargetPointer.Null)
    {
        TargetPointer inFlightData = target.ReadPointer(threadLocalDataPtr + /* ThreadLocalData::inFlightData offset */);
        while (inFlightData != TargetPointer.Null)
        {
            TargetPointer tlsIndexInFlightPtr = target.ReadPointer(inFlightData + /* InflightTLSData::TlsIndex offset */);
            Data.TLSIndex tlsIndexInFlight = new Data.TLSIndex(tlsIndexInFlightPtr);
            if (tlsIndexInFlight.TLSIndexRawIndex == tlsIndex.TLSIndexRawIndex)
            {
                threadLocalStaticBase = target.ReadPointer(tlsIndexInFlightPtr + /* InflightTLSData::TLSData offset */);
                break;
            }
            inFlightData = target.ReadPointer(inFlightData + /* InflightTLSData::Next offset */);
        }
    }
    return threadLocalStaticBase;
}

byte[] IThread.GetWatsonBuckets(TargetPointer threadPointer)
{
    TargetPointer readFrom;
    TargetPointer exceptionTrackerPtr = _target.ReadPointer(threadPointer + /*Thread::ExceptionTracker offset */);
    if (exceptionTrackerPtr == TargetPointer.Null)
        return Array.Empty<byte>();
    TargetPointer thrownObjectHandle = target.ReadPointer(exceptionTrackerPtr + /* ExceptionInfo::ThrownObjectHandle offset */);
    TargetPointer throwableObjectPtr = target.ReadPointer(thrownObjectHandle);
    if (throwableObjectPtr != TargetPointer.Null)
    {
        TargetPointer watsonBuckets = target.ReadPointer(throwableObjectPtr + /* Exception::WatsonBuckets offset */);
        if (watsonBuckets != TargetPointer.Null)
        {
            readFrom = _target.Contracts.Object.GetArrayData(watsonBuckets, out _, out _, out _);
        }
        else
        {
            readFrom = target.ReadPointer(threadPointer + /* Thread::UEWatsonBucketTrackerBuckets offset */);
            if (readFrom == TargetPointer.Null)
            {
                readFrom = target.ReadPointer(exceptionTrackerPtr + /* ExceptionInfo::ExceptionWatsonBucketTrackerBuckets offset */);
            }
            else
            {
                return Array.Empty<byte>();
            }
        }
    }
    else
    {
        readFrom = target.ReadPointer(threadPointer + /* Thread::UEWatsonBucketTrackerBuckets offset */);
    }

    Span<byte> span = new byte[_target.ReadGlobal<uint>("SizeOfGenericModeBlock")];
    if (readFrom == TargetPointer.Null)
        return Array.Empty<byte>();
    
    _target.ReadBuffer(readFrom, span);
    return span.ToArray();
}

```
