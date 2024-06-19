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

``` csharp
ThreadStoreData GetThreadStoreData();
ThreadStoreCounts GetThreadCounts();
ThreadData GetThreadData(TargetPointer threadPointer);
TargetPointer GetNestedExceptionInfo(TargetPointer nestedExceptionPointer, out TargetPointer nextNestedException);
TargetPointer GetManagedThreadObject(TargetPointer threadPointer);
```

## Version 1



``` csharp
SListReader ThreadListReader = Contracts.SList.GetReader("Thread");

ThreadStoreData GetThreadStoreData()
{
    TargetPointer threadStore = Target.ReadGlobalPointer("s_pThreadStore");
    var runtimeThreadStore = new ThreadStore(Target, threadStore);

    TargetPointer firstThread = ThreadListReader.GetHead(runtimeThreadStore.SList.Pointer);

    return new ThreadStoreData(
        ThreadCount : runtimeThreadStore.m_ThreadCount,
        FirstThread: firstThread,
        FinalizerThread: Target.ReadGlobalPointer("g_pFinalizerThread"),
        GCThread: Target.ReadGlobalPointer("g_pSuspensionThread"));
}

DacThreadStoreCounts GetThreadCounts()
{
    TargetPointer threadStore = Target.ReadGlobalPointer("s_pThreadStore");
    var runtimeThreadStore = new ThreadStore(Target, threadStore);

    return new ThreadStoreCounts(
        ThreadCount : runtimeThreadStore.m_ThreadCount,
        UnstartedThreadCount : runtimeThreadStore.m_UnstartedThreadCount,
        BackgroundThreadCount : runtimeThreadStore.m_BackgroundThreadCount,
        PendingThreadCount : runtimeThreadStore.m_PendingThreadCount,
        DeadThreadCount: runtimeThreadStore.m_DeadThreadCount,
}

ThreadData GetThreadData(TargetPointer threadPointer)
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

    return new ThreadData(
        ThreadId : runtimeThread.m_ThreadId,
        OsThreadId : (OsThreadId)runtimeThread.m_OSThreadId,
        State : (ThreadState)runtimeThread.m_State,
        PreemptiveGCDisabled : thread.m_fPreemptiveGCDisabled != 0,
        AllocContextPointer : thread.m_alloc_context.alloc_ptr,
        AllocContextLimit : thread.m_alloc_context.alloc_limit,
        Frame : thread.m_pFrame,
        TEB : thread.Has_m_pTEB ? thread.m_pTEB : TargetPointer.Null,
        LastThrownObjectHandle : new DacGCHandle(thread.m_LastThrownObjectHandle),
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
