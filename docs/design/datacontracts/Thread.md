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

The contract depends on the following globals

| Global name | Type | Meaning |
| --- | --- | --- |
| `AppDomain` | TargetPointer | A pointer to the address of the one AppDomain |
| `ThreadStore` | TargetPointer | A pointer to the address of the ThreadStore |
| `FeatureEHFunclets` | TargetPointer | 1 if EH funclets are enabled, 0 otherwise |
| `FinalizerThread` | TargetPointer | A pointer to the finalizer thread |
| `GCThread` | TargetPointer | A pointer to the GC thread |
| `ThinLockThreadIdDispenser` | TargetPointer | Dispenser of thinlock IDs for locking objects |
| `NumberOfTlsOffsetsNotUsedInNoncollectibleArray` | byte | Number of unused slots in noncollectible TLS array |
| `PtrArrayOffsetToDataArray` | TargetPointer | Offset from PtrArray class address to start of enclosed data array |

The contract additionally depends on these data descriptors

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ExceptionInfo` | `PreviousNestedInfo` | Pointer to previous nested exception info |
| `ExceptionInfo` | `ThrownObjectHandle` | Pointer to exception object handle |
| `ExceptionInfo` | `ExceptionRecord` | Pointer to the OS `EXCEPTION_RECORD` the OS dispatcher pushed for this exception |
| `ExceptionInfo` | `ContextRecord` | Pointer to the OS `CONTEXT` the OS dispatcher pushed for this exception |
| `GCAllocContext` | `Pointer` | GC allocation pointer |
| `GCAllocContext` | `Limit` | Allocation limit pointer |
| `GCAllocContext` | `AllocBytes` | Number of bytes allocated on SOH by this context |
| `GCAllocContext` | `AllocBytesLoh` | Number of bytes allocated not on SOH by this context |
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
| `Thread` | `DebuggerControlledThreadState` | Thread state flags controlled by the debugger |
| `Thread` | `PreemptiveGCDisabled` | Flag indicating if preemptive GC is disabled |
| `Thread` | `Frame` | Pointer to current frame |
| `Thread` | `GCFrame` | Pointer to the head of the thread's GCFrame chain. |
| `Thread` | `CachedStackBase` | Pointer to the base of the stack |
| `Thread` | `CachedStackLimit` | Pointer to the limit of the stack |
| `Thread` | `ExposedObject` | Handle to the managed `Thread` object exposed to the debugger |
| `Thread` | `LastThrownObject` | Handle to last thrown exception object |
| `Thread` | `LastThrownObjectIsUnhandled` | Whether `LastThrownObject` should be treated as unhandled |
| `Thread` | `CurrentCustomDebuggerNotification` | Handle to the current custom debugger notification object |
| `Thread` | `LinkNext` | Pointer to get next thread |
| `Thread` | `ExceptionTracker` | Pointer to exception tracking information |
| `Thread` | `DebuggerFilterContext` | Pointer to the debugger filter context for the thread |
| `Thread` | `InteropDebuggingHijacked` | Whether the thread has been hijacked for interop debugging |
| `Thread` | `RuntimeThreadLocals` | Pointer to some thread-local storage |
| `Thread` | `ThreadLocalDataPtr` | Pointer to thread local data structure |
| `Thread` | `ThreadHandle` | OS thread handle (optional, Windows only; readers should expect `TargetPointer.Null` on non-Windows targets) |
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
