// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// threadsuspend.h

#ifndef _THREAD_SUSPEND_H_
#define _THREAD_SUSPEND_H_

#if defined(TIME_SUSPEND) || defined(GC_STATS)

enum timeUnit { usec, msec, sec };

// running aggregations
struct MinMaxTot
{
    DWORD minVal, maxVal, totVal;

    void Accumulate(DWORD time)
    {
        LIMITED_METHOD_CONTRACT;
        if (time < minVal || minVal == 0)
            minVal = time;

        if (time > maxVal)
            maxVal = time;

        // We are supposed to anticipate overflow and clear our totals
        // However we still see this assert and for now, let's ignore it...
        // _ASSERTE(((DWORD) (totVal + time)) > ((DWORD) totVal));
        if (((DWORD) (totVal + time)) > ((DWORD) totVal))
            totVal += time;
    }

    void Reset()
    {
        LIMITED_METHOD_CONTRACT;
        minVal = maxVal = 0;
    }

    void DisplayAndUpdate(FILE* logFile, __in_z const char *pName, MinMaxTot *pLastOne, int fullCount, int priorCount, timeUnit=usec);
};

// A note about timings.  We use QueryPerformanceCounter to measure all timings in units.  During
// Initialization, we compute a divisor to convert those timings into microseconds.  This means
// that we can accumulate about 4,000 seconds (over one hour) of GC time into 32-bit quantities
// before we must reinitialize.

// A note about performance: derived classes have taken a dependency on cntDisplay being the first
// field of this class, following the vtable*. When this is violated a compile time assert will fire.
struct StatisticsBase
{
    // display the statistics every so many seconds.
    static DWORD secondsToDisplay;

    // we must re-initialize after an hour of GC time, to avoid overflow.  It's more convenient to
    // re-initialize after an hour of wall-clock time, instead
    int cntDisplay;

    // convert all timings into microseconds
    DWORD divisor;
    DWORD GetTime();
    static DWORD GetElapsed(DWORD start, DWORD stop);

    // we want to print statistics every 10 seconds - this is to remember the start of the 10 sec interval.
    DWORD startTick;

    // derived classes must call this regularly (from a logical "end of a cycle")
    void RollOverIfNeeded();

    virtual void Initialize() = 0;
    virtual void DisplayAndUpdate() = 0;
};

#endif // defined(TIME_SUSPEND) || defined(GC_STATS)

#ifdef TIME_SUSPEND

struct SuspendStatistics
    : public StatisticsBase
{
    static WCHAR* logFileName;

    // number of times we call SuspendEE, RestartEE
    int cntSuspends, cntRestarts;

    int cntSuspendsInBGC, cntNonGCSuspends, cntNonGCSuspendsInBGC;

    // Times for current suspension & restart
    DWORD startSuspend, startRestart;

    // min, max and total time spent performing a Suspend, a Restart, or Paused from the start of
    // a Suspend to the end of a Restart.  We can compute 'avg' using 'cnt' and 'tot' values.
    MinMaxTot suspend, restart, paused;

    // We know there can be contention on acquiring the ThreadStoreLock.
    MinMaxTot acquireTSL, releaseTSL;

    // And if we OS suspend a thread that is blocking or perhaps throwing an exception and is therefore
    // stuck in the kernel, it could take approximately a second.  So track the time taken for OS
    // suspends
    MinMaxTot osSuspend;

    // And if we place a hijack, we need to crawl a stack to do so.
    MinMaxTot crawl;

    // And waiting can be a significant part of the total suspension time.
    MinMaxTot wait;

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // There are some interesting events that are worth counting, because they show where the time is going:

    // number of times we waited on g_pGCSuspendEvent while trying to suspend the EE
    int cntWaits;

    // and the number of times those Waits timed out rather than being signalled by a cooperating thread
    int cntWaitTimeouts;

    // number of times we did an OS (or hosted) suspend or resume on a thread
    int cntOSSuspendResume;

    // number of times we crawled a stack for a hijack
    int cntHijackCrawl;

    // and the number of times the hijack actually trapped a thread for us
    int cntHijackTrap;

    // the number of times we redirected a thread in fully interruptible code, by rewriting its EIP
    // so it will throw to a blocking point
    int cntRedirections;

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // And there are some "failure" cases that should never or almost never occur.

    // number of times the OS or Host was unable to ::SuspendThread a thread for us.  This count should be
    // approximately 0.
    int cntFailedSuspends;

    // number of times we were unable to redirect a thread by rewriting its register state in a
    // suspended context.  This count should be approximately 0.
    int cntFailedRedirections;

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Internal mechanism:

    virtual void Initialize();
    virtual void DisplayAndUpdate();

    // Public API

    void StartSuspend();
    void EndSuspend(BOOL bForGC);
    DWORD CurrentSuspend();

    void StartRestart();
    void EndRestart();
    DWORD CurrentRestart();
};

extern SuspendStatistics g_SuspendStatistics;
extern SuspendStatistics g_LastSuspendStatistics;

#endif // TIME_SUSPEND

BOOL EEGetThreadContext(Thread *pThread, CONTEXT *pContext);
BOOL EnsureThreadIsSuspended(HANDLE hThread, Thread* pThread);

class ThreadSuspend
{
    friend class Thread;
    friend class ThreadStore;

public:
    typedef enum
    {
        SUSPEND_OTHER                   = 0,
        SUSPEND_FOR_GC                  = 1,
        SUSPEND_FOR_APPDOMAIN_SHUTDOWN  = 2,
        SUSPEND_FOR_REJIT               = 3,
        SUSPEND_FOR_SHUTDOWN            = 4,
        SUSPEND_FOR_DEBUGGER            = 5,
        SUSPEND_FOR_GC_PREP             = 6,
        SUSPEND_FOR_DEBUGGER_SWEEP      = 7,     // This must only be used in Thread::SysSweepThreadsForDebug
        SUSPEND_FOR_PROFILER            = 8
    } SUSPEND_REASON;

private:
    static SUSPEND_REASON    m_suspendReason;    // This contains the reason why the runtime is suspended

    static void SuspendRuntime(ThreadSuspend::SUSPEND_REASON reason);
    static void ResumeRuntime(BOOL bFinishedGC, BOOL SuspendSucceded);
public:
    // Initialize thread suspension support
    static void Initialize();

private:
    static CLREvent * g_pGCSuspendEvent;

    // This is true iff we're currently in the process of suspending threads.  Once the
    // threads have been suspended, this is false.  This is set via an instance of
    // SuspendRuntimeInProgressHolder placed in SuspendRuntime, SysStartSuspendForDebug,
    // and SysSweepThreadsForDebug.  Code outside Thread reads this via
    // Thread::SysIsSuspendInProgress.
    //
    // *** THERE IS NO SYNCHRONIZATION AROUND SETTING OR READING THIS ***
    // This value is only useful for code that can be more efficient if it has a good guess
    // as to whether we're suspending the runtime.  This is NOT to be used by code that
    // *requires* this knowledge with 100% accuracy in order to behave correctly, unless
    // you add synchronization yourself.  An example of where Thread::SysIsSuspendInProgress
    // is used is by the profiler API, in ProfToEEInterfaceImpl::DoStackSnapshot.  The profiler
    // API needs to suspend the target thread whose stack it is about to walk.  But the profiler
    // API should avoid this if the runtime is being suspended.  Otherwise, the thread trying to
    // suspend the runtime (thread A) might get stuck when it tries to suspend the thread
    // executing ProfToEEInterfaceImpl::DoStackSnapshot (thread B), since thread B will be
    // busy trying to suspend the target of the stack walk (thread C).  Bad luck with timing
    // could cause A to try to suspend B over and over again while B is busy suspending C, and
    // then suspending D, etc., assuming the profiler does a lot of stack walks.  This, in turn,
    // could cause the deadlock detection assert in Thread::SuspendThread to fire.  So the
    // moral here is that, if B realizes the runtime is being suspended, it can just fail the stackwalk
    // immediately without trying to do the suspend.  But if B occasionally gets false positives or
    // false negatives from calling Thread::SysIsSuspendInProgress, the worst is we might
    // delay the EE suspension a little bit, or we might too eagerly fail from ProfToEEInterfaceImpl::DoStackSnapshot.
    // But there won't be any corruption or AV.  More details on the profiler API scenario in VsWhidbey bug 454936.
    static bool     s_fSuspendRuntimeInProgress;

    static bool     s_fSuspended;

    static void SetSuspendRuntimeInProgress();
    static void ResetSuspendRuntimeInProgress();

    typedef StateHolder<ThreadSuspend::SetSuspendRuntimeInProgress, ThreadSuspend::ResetSuspendRuntimeInProgress> SuspendRuntimeInProgressHolder;

public:
    static bool SysIsSuspendInProgress() { return s_fSuspendRuntimeInProgress; }
    static bool SysIsSuspended() { return s_fSuspended; }

public:
    //suspend all threads
    static void SuspendEE(SUSPEND_REASON reason);
    static void RestartEE(BOOL bFinishedGC, BOOL SuspendSucceded); //resume threads.

    static void LockThreadStore(ThreadSuspend::SUSPEND_REASON reason);
    static void UnlockThreadStore(BOOL bThreadDestroyed = FALSE,
                                  ThreadSuspend::SUSPEND_REASON reason = ThreadSuspend::SUSPEND_OTHER);

    static Thread * GetSuspensionThread()
    {
        LIMITED_METHOD_CONTRACT;
        return g_pSuspensionThread;
    }

private:
    static LONG m_DebugWillSyncCount;
};

class ThreadStoreLockHolderWithSuspendReason
{
public:
    ThreadStoreLockHolderWithSuspendReason(ThreadSuspend::SUSPEND_REASON reason)
    {
        ThreadSuspend::LockThreadStore(reason);
    }
    ~ThreadStoreLockHolderWithSuspendReason()
    {
        ThreadSuspend::UnlockThreadStore();
    }
private:
    ThreadSuspend::SUSPEND_REASON m_reason;
};

#endif // _THREAD_SUSPEND_H_
