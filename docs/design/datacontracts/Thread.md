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

The contract additionally depends on these data descriptors

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ExceptionInfo` | `PreviousNestedInfo` | Pointer to previous nested exception info |
| `GCAllocContext` | `Pointer` | GC allocation pointer |
| `GCAllocContext` | `Limit` | Allocation limit pointer |
| `IdDispenser` | `HighestId` | Highest possible small thread ID |
| `IdDispenser` | `IdToThread` | Array mapping small thread IDs to thread pointers |
| `RuntimeThreadLocals` | `AllocContext` | GC allocation context for the thread |
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
| `ThreadStore` | `ThreadCount` | Number of threads |
| `ThreadStore` | `FirstThreadLink` | Pointer to first thread in the linked list |
| `ThreadStore` | `UnstartedCount` | Number of unstarted threads |
| `ThreadStore` | `BackgroundCount` | Number of background threads |
| `ThreadStore` | `PendingCount` | Number of pending threads |
| `ThreadStore` | `DeadCount` | Number of dead threads |
``` csharp
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
```
