# Contract Thread

This contract is for reading and iterating the threads of the process.

## APIs of contract

``` csharp
[Flags]
enum ThreadContextSource
{
    None = 0,
    Debugger = 1,
}

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
    SuspensionTrapped   = 0x00000002,    // Thread is trapped waiting for suspension to complete (was in managed code)
    GCSuspendRedirected = 0x00000004,    // Thread has been redirected to suspension routine
    DebugSuspendPending = 0x00000008,    // Debugger requested this thread to be suspended
    Hijacked            = 0x00000080,    // Return address has been hijacked
    Background          = 0x00000200,    // Thread is a background thread
    Unstarted           = 0x00000400,    // Thread has never been started
    CoInitialized       = 0x00002000,    // CoInitialize has been called for this thread
    InSTA               = 0x00004000,    // Thread hosts an STA
    InMTA               = 0x00008000,    // Thread is part of the MTA
    Stopped             = 0x00010000,    // Thread has started to shut down
    DebugSyncSuspended  = 0x00080000,    // Thread has suspended itself at a safe point in response to a debugger suspend request
    DebugWillSync       = 0x00100000,    // Debugger will wait for this thread to sync
    ThreadPoolWorker    = 0x01000000,    // is this a threadpool worker thread?
    WaitSleepJoin       = 0x02000000,    // Thread is in a Sleep(), Wait(), Join()
    Detached            = unchecked((int)0x80000000), // Thread was detached
}

record struct ThreadData (
    TargetPointer ThreadAddress,
    uint Id;
    TargetNUInt OSId;
    ThreadState State;
    bool PreemptiveGCDisabled
    TargetPointer AllocContextPointer;
    TargetPointer AllocContextLimit;
    TargetPointer Frame;
    TargetPointer FirstNestedException;
    TargetPointer ExposedObjectHandle;
    TargetPointer LastThrownObjectHandle;
    TargetPointer CurrentCustomDebuggerNotificationHandle;
    bool LastThrownObjectIsUnhandled;
    bool HasUnhandledException;
    TargetPointer NextThread;
    TargetPointer ThreadHandle;
    bool IsInteropDebuggingHijacked;
    TargetPointer DebuggerFilterContext;
    TargetPointer GCFrame;
    bool IsExceptionInProgress;
    TargetPointer OSExceptionRecord;
    TargetPointer OSExceptionContextRecord;
);
```

``` csharp
[Flags]
enum DebuggerControlledThreadState
{
    None                        = 0x00000000, // Threads are initialized this way
    UserSuspend                 = 0x00000001, // Marked "suspended" by the debugger
}
```

``` csharp
ThreadStoreData GetThreadStoreData();
ThreadStoreCounts GetThreadCounts();
ThreadData GetThreadData(TargetPointer threadPointer);
void SetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state);
void ResetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state);
void GetStackLimitData(TargetPointer threadPointer, out TargetPointer stackBase, out TargetPointer stackLimit, out TargetPointer frameAddress);
TargetPointer IdToThread(uint id);
TargetPointer GetThreadLocalStaticBase(TargetPointer threadPointer, TargetPointer tlsIndexPtr);
```

## Version 1

<!-- BEGIN GENERATED: usage contract=Thread version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `EEAllocContext` | `GCAllocationContext` | `GCAllocContext` | Embedded GC allocation context for the thread |
| `ExceptionInfo` | `ContextRecord` | `pointer` | Pointer to the OS `CONTEXT` the OS dispatcher pushed for this exception |
| `ExceptionInfo` | `ExceptionFlags` | `uint32` | Exception state flags |
| `ExceptionInfo` | `ExceptionRecord` | `pointer` | Pointer to the OS `EXCEPTION_RECORD` the OS dispatcher pushed for this exception |
| `ExceptionInfo` | `PreviousNestedInfo` | `pointer` | Pointer to previous nested exception info |
| `ExceptionInfo` | `ThrownObject` | `pointer` | Handle to the thrown exception object |
| `GCAllocContext` | `AllocBytes` | `int64` | Number of bytes allocated on SOH by this context |
| `GCAllocContext` | `AllocBytesLoh` | `int64` | Number of bytes allocated not on SOH by this context |
| `GCAllocContext` | `Limit` | `pointer` | Allocation limit pointer |
| `GCAllocContext` | `Pointer` | `pointer` | GC allocation pointer |
| `IdDispenser` | `HighestId` | `uint32` | Highest possible small thread ID |
| `IdDispenser` | `IdToThread` | `pointer` | Array mapping small thread IDs to thread pointers |
| `InFlightTLSData` | `Next` | `pointer` | Pointer to next in-flight TLS data entry |
| `InFlightTLSData` | `TLSData` | `ObjectHandle` | Object handle to the TLS data for the static field |
| `InFlightTLSData` | `TlsIndex` | `TLSIndex` | TLS index for the in-flight static field |
| `RuntimeThreadLocals` | `AllocContext` | `EEAllocContext` | GC allocation context for the thread |
| `Thread` | `CachedStackBase` | `pointer` | Pointer to the base of the stack |
| `Thread` | `CachedStackLimit` | `pointer` | Pointer to the limit of the stack |
| `Thread` | `CurrentCustomDebuggerNotification` | `ObjectHandle` | Handle to the current custom debugger notification object |
| `Thread` | `DebuggerControlledThreadState` | `uint32` | Thread state flags controlled by the debugger |
| `Thread` | `DebuggerFilterContext` | `pointer` | Pointer to the debugger filter context for the thread |
| `Thread` | `ExceptionTracker` | `pointer` | Pointer to exception tracking information |
| `Thread` | `ExposedObject` | `ObjectHandle` | Handle to the managed `Thread` object exposed to the debugger |
| `Thread` | `Frame` | `pointer` | Pointer to current frame |
| `Thread` | `GCFrame` | `pointer` | Pointer to the head of the thread's GCFrame chain. |
| `Thread` | `Id` | `uint32` | Thread identifier |
| `Thread` | `InteropDebuggingHijacked` | `uint32` | Whether the thread has been hijacked for interop debugging |
| `Thread` | `LastThrownObject` | `ObjectHandle` | Handle to last thrown exception object |
| `Thread` | `LastThrownObjectIsUnhandled` | `uint32` | Whether `LastThrownObject` should be treated as unhandled |
| `Thread` | `LinkNext` | `pointer` | Pointer to get next thread |
| `Thread` | `OSId` | `nuint` | Operating system thread identifier |
| `Thread` | `PreemptiveGCDisabled` | `uint32` | Flag indicating if preemptive GC is disabled |
| `Thread` | `RuntimeThreadLocals` | `pointer` | Pointer to some thread-local storage |
| `Thread` | `State` | `uint32` | Thread state flags |
| `Thread` | `ThreadHandle` | `pointer` | OS thread handle (optional, Windows only; readers should expect `TargetPointer.Null` on non-Windows targets) |
| `Thread` | `ThreadLocalDataPtr` | `pointer` | Pointer to thread local data structure |
| `ThreadLocalData` | `CollectibleTlsArrayData` | `pointer` | Pointer to collectible TLS array data |
| `ThreadLocalData` | `CollectibleTlsDataCount` | `int32` | Count of collectible TLS data entries |
| `ThreadLocalData` | `InFlightData` | `pointer` | Pointer to in-flight TLS data for fields being initialized |
| `ThreadLocalData` | `NonCollectibleTlsArrayData` | `pointer` | Pointer to non-collectible TLS array data |
| `ThreadLocalData` | `NonCollectibleTlsDataCount` | `int32` | Count of non-collectible TLS data entries |
| `ThreadStore` | `BackgroundCount` | `int32` | Number of background threads |
| `ThreadStore` | `DeadCount` | `int32` | Number of dead threads |
| `ThreadStore` | `FirstThreadLink` | `pointer` | Pointer to first thread in the linked list |
| `ThreadStore` | `PendingCount` | `int32` | Number of pending threads |
| `ThreadStore` | `ThreadCount` | `int32` | Number of threads |
| `ThreadStore` | `UnstartedCount` | `int32` | Number of unstarted threads |
| `TLSIndex` | `TLSIndexRawIndex` | `uint32` | Raw index value containing type and offset |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `FinalizerThread` | `pointer` | Pointer to the finalizer thread |
| `GCThread` | `pointer` | Pointer to the GC thread |
| `NumberOfTlsOffsetsNotUsedInNoncollectibleArray` | `uint8` | Number of unused slots in the non-collectible TLS array |
| `PtrArrayOffsetToDataArray` | `pointer` | Offset from a pointer-array object to its enclosed data array |
| `ThinlockThreadIdDispenser` | `pointer` | Pointer to the dispenser of thin-lock thread IDs |
| `ThreadStore` | `pointer` | Pointer to the runtime thread store |

### Contracts used

_None._
<!-- END GENERATED: usage contract=Thread version=c1 -->

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

    // Prefer the active exception from ExInfo (pseudo-handle to m_exception field).
    // After the removal of SetThrowable/m_hThrowable, m_LastThrownObjectHandle is only
    // updated after exception dispatch completes, so during active dispatch it may be stale.
    TargetPointer lastThrownObjectHandle = TargetPointer.Null;
    if (exceptionTrackerAddr != TargetPointer.Null)
    {
        TargetPointer thrownObject = target.ReadPointer(exceptionTrackerAddr + /* ExceptionInfo::ThrownObject offset */);
        if (thrownObject != TargetPointer.Null)
        {
            lastThrownObjectHandle = exceptionTrackerAddr + /* ExceptionInfo::ThrownObject field offset */;
        }
    }
    if (lastThrownObjectHandle == TargetPointer.Null)
    {
        lastThrownObjectHandle = target.ReadPointer(address + /* Thread::LastThrownObject offset */);
    }

    ulong threadLinkoffset = ... // offset from Thread data descriptor

    // The OS-pushed EXCEPTION_RECORD / CONTEXT are reachable through the current
    // exception tracker (ExInfo). When there is no exception in progress the tracker
    // pointer is null.
    bool isExceptionInProgress = exceptionTrackerAddr != TargetPointer.Null;
    TargetPointer osExceptionRecord = isExceptionInProgress
        ? target.ReadPointer(exceptionTrackerAddr + /* ExceptionInfo::ExceptionRecord offset */)
        : TargetPointer.Null;
    TargetPointer osExceptionContextRecord = isExceptionInProgress
        ? target.ReadPointer(exceptionTrackerAddr + /* ExceptionInfo::ContextRecord offset */)
        : TargetPointer.Null;

    return new ThreadData(
        Id: target.Read<uint>(address + /* Thread::Id offset */),
        OSId: target.ReadNUInt(address + /* Thread::OSId offset */),
        State: (ThreadState)(target.Read<uint>(address + /* Thread::State offset */) & /* mask of wrapped ThreadState bits */),
        PreemptiveGCDisabled: (target.Read<uint>(address + /* Thread::PreemptiveGCDisabled offset */) & 0x1) != 0,
        AllocContextPointer: allocContextPointer,
        AllocContextLimit: allocContextLimit,
        Frame: target.ReadPointer(address + /* Thread::Frame offset */),
        LastThrownObjectHandle : lastThrownObjectHandle,
        FirstNestedException : firstNestedException,
        NextThread: target.ReadPointer(address + /* Thread::LinkNext offset */) - threadLinkOffset;
        GCFrame: target.ReadPointer(address + /* Thread::GCFrame offset */),
        IsExceptionInProgress: isExceptionInProgress,
        OSExceptionRecord: osExceptionRecord,
        OSExceptionContextRecord: osExceptionContextRecord,
    );
}

void IThread.GetStackLimitData(TargetPointer threadPointer, out TargetPointer stackBase, out TargetPointer stackLimit, out TargetPointer frameAddress)
{
    stackBase = target.ReadPointer(threadPointer + /* Thread::CachedStackBase offset */);
    stackLimit = target.ReadPointer(threadPointer + /* Thread::CachedStackLimit offset */);
    frameAddress = threadPointer + /* Thread::Frame offset */;
}

void SetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state)
{
    uint current = target.Read<uint>(thread + /* Thread::DebuggerControlledThreadState offset */);
    target.Write<uint>(thread + /* Thread::DebuggerControlledThreadState offset */, current | (uint)state);
}

void ResetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state)
{
    uint current = target.Read<uint>(thread + /* Thread::DebuggerControlledThreadState offset */);
    target.Write<uint>(thread + /* Thread::DebuggerControlledThreadState offset */, current & ~(uint)state);
}

TargetPointer IThread.IdToThread(uint id)
{
    TargetPointer idDispenserPointer = target.ReadGlobalPointer(Constants.Globals.ThinlockThreadIdDispenser);
    TargetPointer idDispenser = target.ReadPointer(idDispenserPointer);
    uint HighestId = target.Read<uint>(idDispenser + /* IdDispenser::HighestId offset */);
    TargetPointer threadPtr = TargetPointer.Null;
    if (id <= HighestId)
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
                // The collectible TLS array slot holds an OBJECTHANDLE; dereference the handle to the object
                TargetPointer handleSlotAddress = collectibleArray + (ulong)(indexOffset * target.PointerSize);
                TargetPointer handle = target.ReadPointer(handleSlotAddress);
                if (handle != TargetPointer.Null && target.TryReadPointer(handle, out TargetPointer obj))
                    threadLocalStaticBase = obj;
            }
            break;
        case TLSIndexType.DirectOnThreadLocalData:
            threadLocalStaticBase = threadLocalDataPtr + indexOffset;
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

TargetPointer IThread.GetCurrentExceptionHandle(TargetPointer threadPointer)
{
    TargetPointer exceptionTrackerPtr = target.ReadPointer(threadPointer + /*Thread::ExceptionTracker offset */);
    if (exceptionTrackerPtr == TargetPointer.Null)
        return TargetPointer.Null;
    TargetPointer thrownObject = target.ReadPointer(exceptionTrackerPtr + /* ExceptionInfo::ThrownObject offset */);

    if (thrownObject == TargetPointer.Null)
        return TargetPointer.Null;

    // Return the address of the ThrownObject field as a pseudo-handle.
    // Callers dereference this address to read the exception Object*.
    return exceptionTrackerPtr + /* ExceptionInfo::ThrownObject field offset */;
}

```
