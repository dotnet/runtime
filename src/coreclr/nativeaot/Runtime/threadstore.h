// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
class Thread;
class CLREventStatic;
class RuntimeInstance;
class Array;
typedef DPTR(RuntimeInstance) PTR_RuntimeInstance;

enum class TrapThreadsFlags
{
    None = 0,
    AbortInProgress = 1,
    TrapThreads = 2
};

extern "C" void PopulateDebugHeaders();

class ThreadStore
{
    friend void PopulateDebugHeaders();

    SList<Thread>       m_ThreadList;
    PTR_RuntimeInstance m_pRuntimeInstance;
    ReaderWriterLock    m_Lock;

private:
    ThreadStore();

    void                    LockThreadStore();
    void                    UnlockThreadStore();

public:
    class Iterator
    {
        ReaderWriterLock::ReadHolder    m_readHolder;
        PTR_Thread                      m_pCurrentPosition;
    public:
        Iterator();
        ~Iterator();
        PTR_Thread GetNext();
    };

    ~ThreadStore();
    static ThreadStore *    Create(RuntimeInstance * pRuntimeInstance);
    static Thread *         RawGetCurrentThread();
    static Thread *         GetCurrentThread();
    static Thread *         GetCurrentThreadIfAvailable();
    static PTR_Thread       GetSuspendingThread();
    static void             AttachCurrentThread();
    static void             AttachCurrentThread(bool fAcquireThreadStoreLock);
    static void             DetachCurrentThread(bool shutdownStarted);
#ifndef DACCESS_COMPILE
    static void             SaveCurrentThreadOffsetForDAC();
    void                    InitiateThreadAbort(Thread* targetThread, Object * threadAbortException, bool doRudeAbort);
    void                    CancelThreadAbort(Thread* targetThread);
#else
    static PTR_Thread       GetThreadFromTEB(TADDR pvTEB);
#endif
    bool                    GetExceptionsForCurrentThread(Array* pOutputArray, int32_t* pWrittenCountOut);

    void        Destroy();
    void        SuspendAllThreads(bool waitForGCEvent);
    void        ResumeAllThreads(bool waitForGCEvent);

    static bool IsTrapThreadsRequested();
};
typedef DPTR(ThreadStore) PTR_ThreadStore;

ThreadStore * GetThreadStore();

#define FOREACH_THREAD(p_thread_name)                       \
{                                                           \
    ThreadStore::Iterator __threads;                        \
    Thread * p_thread_name;                                 \
    while ((p_thread_name = __threads.GetNext()) != NULL)   \
    {                                                       \

#define END_FOREACH_THREAD  \
    }                       \
}                           \

