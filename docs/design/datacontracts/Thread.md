# Contract Thread

This contract is for reading and iterating the threads of the process.

## Data structures defined by contract
``` csharp
record struct DacThreadStoreData (
    int ThreadCount,
    TargetPointer FirstThread,
    TargetPointer FinalizerThread,
    TargetPointer GcThread);

record struct DacThreadStoreCounts (
    int UnstartedThreadCount,
    int BackgroundThreadCount,
    int PendingThreadCount,
    int DeadThreadCount);

enum ThreadState
{
    TS_Unknown                = 0x00000000,    // threads are initialized this way

    TS_AbortRequested         = 0x00000001,    // Abort the thread

    TS_GCSuspendPending       = 0x00000002,    // ThreadSuspend::SuspendRuntime watches this thread to leave coop mode.
    TS_GCSuspendRedirected    = 0x00000004,    // ThreadSuspend::SuspendRuntime has redirected the thread to suspention routine.
    TS_GCSuspendFlags         = TS_GCSuspendPending | TS_GCSuspendRedirected, // used to track suspension progress. Only SuspendRuntime writes/resets these.

    TS_DebugSuspendPending    = 0x00000008,    // Is the debugger suspending threads?
    TS_GCOnTransitions        = 0x00000010,    // Force a GC on stub transitions (GCStress only)

    TS_LegalToJoin            = 0x00000020,    // Is it now legal to attempt a Join()

    TS_ExecutingOnAltStack    = 0x00000040,    // Runtime is executing on an alternate stack located anywhere in the memory

    TS_Hijacked               = 0x00000080,    // Return address has been hijacked

    // unused                 = 0x00000100,
    TS_Background             = 0x00000200,    // Thread is a background thread
    TS_Unstarted              = 0x00000400,    // Thread has never been started
    TS_Dead                   = 0x00000800,    // Thread is dead

    TS_WeOwn                  = 0x00001000,    // Exposed object initiated this thread
    TS_CoInitialized          = 0x00002000,    // CoInitialize has been called for this thread

    TS_InSTA                  = 0x00004000,    // Thread hosts an STA
    TS_InMTA                  = 0x00008000,    // Thread is part of the MTA

    // Some bits that only have meaning for reporting the state to clients.
    TS_ReportDead             = 0x00010000,    // in WaitForOtherThreads()
    TS_FullyInitialized       = 0x00020000,    // Thread is fully initialized and we are ready to broadcast its existence to external clients

    TS_TaskReset              = 0x00040000,    // The task is reset

    TS_SyncSuspended          = 0x00080000,    // Suspended via WaitSuspendEvent
    TS_DebugWillSync          = 0x00100000,    // Debugger will wait for this thread to sync

    TS_StackCrawlNeeded       = 0x00200000,    // A stackcrawl is needed on this thread, such as for thread abort
                                                // See comment for s_pWaitForStackCrawlEvent for reason.

    // unused                 = 0x00400000,

    // unused                 = 0x00800000,
    TS_TPWorkerThread         = 0x01000000,    // is this a threadpool worker thread?

    TS_Interruptible          = 0x02000000,    // sitting in a Sleep(), Wait(), Join()
    TS_Interrupted            = 0x04000000,    // was awakened by an interrupt APC. !!! This can be moved to TSNC

    TS_CompletionPortThread   = 0x08000000,    // Completion port thread

    TS_AbortInitiated         = 0x10000000,    // set when abort is begun

    TS_Finalized              = 0x20000000,    // The associated managed Thread object has been finalized.
                                                // We can clean up the unmanaged part now.

    TS_FailStarted            = 0x40000000,    // The thread fails during startup.
    TS_Detached               = 0x80000000,    // Thread was detached by DllMain
}

record struct DacThreadData (
    uint ThreadId;
    TargetNUint OsThreadId;
    ThreadState State;
    bool PreemptiveGCDisabled
    TargetPointer AllocContextPointer;
    TargetPointer AllocContextLimit;
    TargetPointer Frame;
    TargetPointer FirstNestedException;
    TargetPointer TEB;
    DacGCHandle LastThrownObjectHandle;
    TargetPointer NextThread;
);
```

## Apis of contract
``` csharp
DacThreadStoreData GetThreadStoreData();
DacThreadStoreCounts GetThreadCounts();
DacThreadData GetThreadData(TargetPointer threadPointer);
TargetPointer GetNestedExceptionInfo(TargetPointer nestedExceptionPointer, out TargetPointer nextNestedException);
TargetPointer GetManagedThreadObject(TargetPointer threadPointer);
```

## Version 1



``` csharp
SListReader ThreadListReader = Contracts.SList.GetReader("Thread");

DacThreadStoreData GetThreadStoreData()
{
    TargetPointer threadStore = Target.ReadGlobalTargetPointer("s_pThreadStore");
    var runtimeThreadStore = new ThreadStore(Target, threadStore);

    TargetPointer firstThread = ThreadListReader.GetHead(runtimeThreadStore.SList.Pointer);

    return new DacThreadStoreData(
        ThreadCount : runtimeThreadStore.m_ThreadCount,
        FirstThread: firstThread,
        FinalizerThread: Target.ReadGlobalTargetPointer("g_pFinalizerThread"),
        GcThread: Target.ReadGlobalTargetPointer("g_pSuspensionThread"));
}

DacThreadStoreCounts GetThreadCounts()
{
    TargetPointer threadStore = Target.ReadGlobalTargetPointer("s_pThreadStore");
    var runtimeThreadStore = new ThreadStore(Target, threadStore);

    return new DacThreadStoreCounts(
        ThreadCount : runtimeThreadStore.m_ThreadCount,
        UnstartedThreadCount : runtimeThreadStore.m_UnstartedThreadCount,
        BackgroundThreadCount : runtimeThreadStore.m_BackgroundThreadCount,
        PendingThreadCount : runtimeThreadStore.m_PendingThreadCount,
        DeadThreadCount: runtimeThreadStore.m_DeadThreadCount,
}

DacThreadData GetThreadData(TargetPointer threadPointer)
{
    var runtimeThread = new Thread(Target, threadPointer);

    TargetPointer firstNestedException = TargetPointer.Null;
    if (Target.ReadGlobalInt32("FEATURE_EH_FUNCLETS"))
    {
        if (runtimeThread.m_ExceptionState.m_pCurrentTracker != TargetPointer.Null)
        {
            firstNestedException = new ExceptionTrackerBase(Target, runtimeThread.m_ExceptionState.m_pCurrentTracker).m_pPrevNestedInfo;
        }
    }
    else
    {
        firstNestedException = runtimeThread.m_ExceptionState.m_currentExInfo.m_pPrevNestedInfo;
    }

    return new DacThread(
        ThreadId : runtimeThread.m_ThreadId,
        OsThreadId : (OsThreadId)runtimeThread.m_OSThreadId,
        State : (ThreadState)runtimeThread.m_State,
        PreemptiveGCDisabled : thread.m_fPreemptiveGCDisabled != 0,
        AllocContextPointer : thread.m_alloc_context.alloc_ptr,
        AllocContextLimit : thread.m_alloc_context.alloc_limit,
        Frame : thread.m_pFrame,
        TEB : thread.Has_m_pTEB ? thread.m_pTEB : TargetPointer.Null,
        LastThreadObjectHandle : new DacGCHandle(thread.m_LastThrownObjectHandle),
        FirstNestedException : firstNestedException,
        NextThread : ThreadListReader.GetHead.GetNext(threadPointer)
    );
}

TargetPointer GetNestedExceptionInfo(TargetPointer nestedExceptionPointer, out TargetPointer nextNestedException)
{
    if (nestedExceptionPointer == TargetPointer.Null)
    {
        throw new InvalidArgumentException();
    }
    if (Target.ReadGlobalInt32("FEATURE_EH_FUNCLETS"))
    {
        var exData = new ExceptionTrackerBase(Target, nestedExceptionPointer);
        nextNestedException = exData.m_pPrevNestedInfo;
        return Contracts.GCHandle.GetObject(exData.m_hThrowable);
    }
    else
    {
        var exData = new ExInfo(Target, nestedExceptionPointer);
        nextNestedException = exData.m_pPrevNestedInfo;
        return Contracts.GCHandle.GetObject(exData.m_hThrowable);
    }
}

TargetPointer GetManagedThreadObject(TargetPointer threadPointer)
{
    var runtimeThread = new Thread(Target, threadPointer);
    return Contracts.GCHandle.GetObject(new DacGCHandle(runtimeThread.m_ExposedObject));
}
```
