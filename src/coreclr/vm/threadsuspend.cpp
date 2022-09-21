// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// threadsuspend.CPP
//
// This file contains the implementation of thread suspension. The implementation of thread suspension
// used to be spread through multiple places. That is why, many methods still live in their own homes
// (class Thread, class ThreadStore, etc.). They should be eventually refactored into class ThreadSuspend.
//

#include "common.h"

#include "threadsuspend.h"

#include "finalizerthread.h"
#include "dbginterface.h"

// from ntstatus.h
#define STATUS_SUSPEND_COUNT_EXCEEDED    ((NTSTATUS)0xC000004AL)

#define HIJACK_NONINTERRUPTIBLE_THREADS

bool ThreadSuspend::s_fSuspendRuntimeInProgress = false;

bool ThreadSuspend::s_fSuspended = false;

CLREvent* ThreadSuspend::g_pGCSuspendEvent = NULL;

ThreadSuspend::SUSPEND_REASON ThreadSuspend::m_suspendReason;

#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
void* ThreadSuspend::g_returnAddressHijackTarget = NULL;
#endif

// If you add any thread redirection function, make sure the debugger can 1) recognize the redirection
// function, and 2) retrieve the original CONTEXT.  See code:Debugger.InitializeHijackFunctionAddress and
// code:DacDbiInterfaceImpl.RetrieveHijackedContext.
extern "C" void             RedirectedHandledJITCaseForGCThreadControl_Stub(void);
extern "C" void             RedirectedHandledJITCaseForDbgThreadControl_Stub(void);
extern "C" void             RedirectedHandledJITCaseForUserSuspend_Stub(void);
#ifdef FEATURE_SPECIAL_USER_MODE_APC
extern "C" void NTAPI       ApcActivationCallbackStub(ULONG_PTR Parameter);
#endif

#define GetRedirectHandlerForGCThreadControl()      \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForGCThreadControl_Stub))
#define GetRedirectHandlerForDbgThreadControl()     \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForDbgThreadControl_Stub))
#define GetRedirectHandlerForUserSuspend()          \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForUserSuspend_Stub))
#ifdef FEATURE_SPECIAL_USER_MODE_APC
#define GetRedirectHandlerForApcActivation()        \
                ((PAPCFUNC) GetEEFuncEntryPoint(ApcActivationCallbackStub))
#endif

#if defined(TARGET_AMD64) || defined(TARGET_ARM) || defined(TARGET_ARM64)
#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
extern "C" void             RedirectedHandledJITCaseForGCStress_Stub(void);
#define GetRedirectHandlerForGCStress()             \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForGCStress_Stub))
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS
#endif // TARGET_AMD64 || TARGET_ARM

// Every PING_JIT_TIMEOUT ms, check to see if a thread in JITted code has wandered
// into some fully interruptible code (or should have a different hijack to improve
// our chances of snagging it at a safe spot).
#define PING_JIT_TIMEOUT        1

// When we find a thread in a spot that's not safe to abort -- how long to wait before
// we try again.
#define ABORT_POLL_TIMEOUT      10
#ifdef _DEBUG
#define ABORT_FAIL_TIMEOUT      40000
#endif // _DEBUG

//
// CANNOT USE IsBad*Ptr() methods here.  They are *banned* APIs because of various
// reasons (see http://winweb/wincet/bannedapis.htm).
//
#define IS_VALID_WRITE_PTR(addr, size)      _ASSERTE((addr) != NULL)
#define IS_VALID_CODE_PTR(addr)             _ASSERTE((addr) != NULL)


void ThreadSuspend::SetSuspendRuntimeInProgress()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(ThreadStore::HoldingThreadStore() || IsAtProcessExit());
    _ASSERTE(!s_fSuspendRuntimeInProgress || IsAtProcessExit());
    s_fSuspendRuntimeInProgress = true;
}

void ThreadSuspend::ResetSuspendRuntimeInProgress()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(ThreadStore::HoldingThreadStore() || IsAtProcessExit());
    _ASSERTE(s_fSuspendRuntimeInProgress || IsAtProcessExit());
    s_fSuspendRuntimeInProgress = false;
}


// When SuspendThread returns, target thread may still be executing user code.
// We can not access data, e.g. m_fPreemptiveGCDisabled, changed by target thread.
// But our code depends on reading these data.  To make this operation safe, we
// call GetThreadContext which returns only after target thread does not execute
// any user code.

// Message from David Cutler
/*
    After SuspendThread returns, can the suspended thread continue to execute code in user mode?

    [David Cutler] The suspended thread cannot execute any more user code, but it might be currently "running"
    on a logical processor whose other logical processor is currently actually executing another thread.
    In this case the target thread will not suspend until the hardware switches back to executing instructions
    on its logical processor. In this case even the memory barrier would not necessarily work - a better solution
    would be to use interlocked operations on the variable itself.

    After SuspendThread returns, does the store buffer of the CPU for the suspended thread still need to drain?

    Historically, we've assumed that the answer to both questions is No.  But on one 4/8 hyper-threaded machine
    running Win2K3 SP1 build 1421, we've seen two stress failures where SuspendThread returns while writes seem to still be in flight.

    Usually after we suspend a thread, we then call GetThreadContext.  This seems to guarantee consistency.
    But there are places we would like to avoid GetThreadContext, if it's safe and legal.

    [David Cutler] Get context delivers a APC to the target thread and waits on an event that will be set
    when the target thread has delivered its context.

    Chris.
*/

// Message from Neill Clift
/*
    What SuspendThread does is insert an APC block into a target thread and request an inter-processor interrupt to
    do the APC interrupt. It doesn't wait till the thread actually enters some state or the interrupt has been serviced.

    I took a quick look at the APIC spec in the Intel manuals this morning. Writing to the APIC posts a message on a bus.
    Processors accept messages and presumably queue the s/w interrupts at this time. We don't wait for this acceptance
    when we send the IPI so at least on APIC machines when you suspend a thread it continues to execute code for some short time
    after the routine returns. We use other mechanisms for IPI and so it could work differently on different h/w.

*/
BOOL EnsureThreadIsSuspended (HANDLE hThread, Thread* pThread)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    WRAPPER_NO_CONTRACT;

    CONTEXT ctx;
    ctx.ContextFlags = CONTEXT_INTEGER;

    BOOL ret;
    ret = ::GetThreadContext(hThread, &ctx);
    return ret;
}

FORCEINLINE VOID MyEnterLogLock()
{
    EnterLogLock();
}
FORCEINLINE VOID MyLeaveLogLock()
{
    LeaveLogLock();
}

// On non-Windows CORECLR platforms remove Thread::SuspendThread support
#ifndef DISABLE_THREADSUSPEND
// SuspendThread
//   Attempts to OS-suspend the thread, whichever GC mode it is in.
// Arguments:
//   fOneTryOnly - If TRUE, report failure if the thread has its
//     m_dwForbidSuspendThread flag set.  If FALSE, retry.
//   pdwSuspendCount - If non-NULL, will contain the return code
//     of the underlying OS SuspendThread call on success,
//     undefined on any kind of failure.
// Return value:
//   A SuspendThreadResult value indicating success or failure.
Thread::SuspendThreadResult Thread::SuspendThread(BOOL fOneTryOnly, DWORD *pdwSuspendCount)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef STRESS_LOG
    if (StressLog::StressLogOn((unsigned int)-1, 0))
    {
        // Make sure to create the stress log for the current thread
        // (if needed) before we suspend the target thread.  The target
        // thread may be holding the stress log lock when we suspend it,
        // which could cause a deadlock.
        if (StressLog::CreateThreadStressLog() == NULL)
        {
            return STR_NoStressLog;
        }
    }
#endif

    HANDLE hThread;
    SuspendThreadResult str = (SuspendThreadResult) -1;
    DWORD dwSuspendCount = 0;
    DWORD tries = 1;
#if defined(_DEBUG)
    int nCnt = 0;
    bool bDiagSuspend = g_pConfig->GetDiagnosticSuspend();
    ULONGLONG i64TimestampStart = CLRGetTickCount64();
    ULONGLONG i64TimestampCur = i64TimestampStart;
    ULONGLONG i64TimestampPrev = i64TimestampStart;

    // This is the max allowed timestamp ticks to transpire from beginning of
    // our attempt to suspend the thread, before we'll assert (implying we believe
    // there might be a deadlock) - (default = 2000).
    ULONGLONG i64TimestampTicksMax = g_pConfig->SuspendThreadDeadlockTimeoutMs();
#endif // _DEBUG

#if defined(_DEBUG)
    // Stop the stress log from allocating any new memory while in this function
    // as that can lead to deadlocks
    CantAllocHolder hldrCantAlloc;
#endif

    DWORD dwSwitchCount = 0;

    while (TRUE) {
        StateHolder<MyEnterLogLock, MyLeaveLogLock> LogLockHolder(FALSE);
        CounterHolder handleHolder(&m_dwThreadHandleBeingUsed);

        hThread = GetThreadHandle();
        if (hThread == INVALID_HANDLE_VALUE) {
            str = STR_UnstartedOrDead;
            break;
        }

        {
            // We do not want to suspend the target thread while it is holding the log lock.
            // By acquiring the lock ourselves, we know that this is not the case.
            // Note: LogLock is a noop when not logging, do not assume the rest is running under a lock.
            LogLockHolder.Acquire();

            // It is important to avoid two threads suspending each other.
            // Before a thread suspends another, it increments its own m_dwForbidSuspendThread count first,
            // then it checks the target thread's m_dwForbidSuspendThread.
            ForbidSuspendThreadHolder forbidSuspend;

            // We need a total order between write/read of our/their m_dwForbidSuspendThread here, thus a full fence.
            // Note - this is just to prevent mutual suspension of two threads executing this same sequence.
            // The other thread may still set its m_dwForbidSuspendThread for other reasons asynchronously.
            // We will check on that once the thread is suspended.
            if (InterlockedOr(&m_dwForbidSuspendThread, 0))
            {
#if defined(_DEBUG)
                // Enable the diagnostic ClrSuspendThread() if the
                //     DiagnosticSuspend config setting is set.
                // This will interfere with the mutual suspend race but it's
                //     here only for diagnostic purposes anyway
                if (!bDiagSuspend)
#endif // _DEBUG
                    goto retry;
            }

            dwSuspendCount = ClrSuspendThread(hThread);

            //
            // Since SuspendThread is asynchronous, we now must wait for the thread to
            // actually be suspended before decrementing our own m_dwForbidSuspendThread count.
            // Otherwise there would still be a chance for the "suspended" thread to suspend us
            // before it really stops running.
            //
            if ((int)dwSuspendCount >= 0)
            {
                if (!EnsureThreadIsSuspended(hThread, this))
                {
                    ClrResumeThread(hThread);
                    str = STR_Failure;
                    break;
                }
            }
        }

        if ((int)dwSuspendCount >= 0)
        {
            // read m_dwForbidSuspendThread again after suspension, it may have changed.
            if (m_dwForbidSuspendThread.LoadWithoutBarrier() != 0)
            {
#if defined(_DEBUG)
                // Log diagnostic below 8 times during the i64TimestampTicksMax period
                if (i64TimestampCur-i64TimestampStart >= nCnt*(i64TimestampTicksMax>>3) )
                {
                    CONTEXT ctx;
                    SetIP(&ctx, -1);
                    ctx.ContextFlags = CONTEXT_CONTROL;
                    this->GetThreadContext(&ctx);
                    STRESS_LOG7(LF_SYNC, LL_INFO1000,
                        "Thread::SuspendThread[%p]:  EIP=%p. nCnt=%d. result=%d.\n"
                        "\t\t\t\t\t\t\t\t\t     forbidSuspend=%d. coop=%d. state=%x.\n",
                        this, GetIP(&ctx), nCnt, dwSuspendCount,
                        (LONG)this->m_dwForbidSuspendThread, (ULONG)this->m_fPreemptiveGCDisabled, this->GetSnapshotState());

                    // Enable a preemptive assert in diagnostic mode: before we
                    // resume the target thread to get its current state in the debugger
                    if (bDiagSuspend)
                    {
                        // triggered after 6 * 250msec
                        _ASSERTE(nCnt < 6 && "Timing out in Thread::SuspendThread");
                    }

                    ++nCnt;
                }
#endif // _DEBUG
                ClrResumeThread(hThread);

#if defined(_DEBUG)
                // If the suspend diagnostics are enabled we need to spin here in order to avoid
                // the case where we Suspend/Resume the target thread without giving it a chance to run.
                if ((!fOneTryOnly) && bDiagSuspend)
                {
                    while ( m_dwForbidSuspendThread != 0 &&
                        CLRGetTickCount64()-i64TimestampStart < nCnt*(i64TimestampTicksMax>>3) )
                    {
                        if (g_SystemInfo.dwNumberOfProcessors > 1)
                        {
                            if ((tries++) % 20 != 0) {
                                YieldProcessorNormalized(); // play nice on hyperthreaded CPUs
                            } else {
                                __SwitchToThread(0, ++dwSwitchCount);
                            }
                        }
                        else
                        {
                            __SwitchToThread(0, ++dwSwitchCount); // don't spin on uniproc machines
                        }
                    }
                }
#endif // _DEBUG
                goto retry;
            }
            // We suspend the right thread
#ifdef _DEBUG
            Thread * pCurThread = GetThreadNULLOk();
            if (pCurThread != NULL)
            {
                pCurThread->dbg_m_cSuspendedThreads ++;
                _ASSERTE(pCurThread->dbg_m_cSuspendedThreads > 0);
            }
#endif
            IncCantAllocCount();

            m_ThreadHandleForResume = hThread;
            str = STR_Success;
            break;
        }
        else
        {
            // SuspendThread failed
            if ((int)dwSuspendCount != -1)
            {
                STRESS_LOG1(LF_SYNC, LL_INFO1000, "In Thread::SuspendThread ::SuspendThread returned %x\n", dwSuspendCount);
            }

            // Our callers generally expect that STR_Failure means that
            // the thread has exited.
#ifndef TARGET_UNIX
            _ASSERTE(NtCurrentTeb()->LastStatusValue != STATUS_SUSPEND_COUNT_EXCEEDED);
#endif // !TARGET_UNIX

            str = STR_Failure;
            break;
        }

retry:
        handleHolder.Release();
        LogLockHolder.Release();

        if (fOneTryOnly)
        {
            str = STR_Forbidden;
            break;
        }

#if defined(_DEBUG)
        i64TimestampPrev = i64TimestampCur;
        i64TimestampCur = CLRGetTickCount64();
        // CLRGetTickCount64() is global per machine (not per CPU, like getTimeStamp()).
        // Next ASSERT states that CLRGetTickCount64() is increasing, or has wrapped.
        // If it wrapped, the last iteration should have executed faster then 0.5 seconds.
        _ASSERTE(i64TimestampCur >= i64TimestampPrev || i64TimestampCur <= 500);

        if (i64TimestampCur - i64TimestampStart >= i64TimestampTicksMax)
        {
            dwSuspendCount = ClrSuspendThread(hThread);
            _ASSERTE(!"It takes too long to suspend a thread");
            if ((int)dwSuspendCount >= 0)
                ClrResumeThread(hThread);
        }
#endif // _DEBUG

        // Allow the target thread to run in order to make some progress.
        // On multi processor machines we saw the suspending thread resuming immediately after the __SwitchToThread()
        // because it has another few processors available.  As a consequence the target thread was being Resumed and
        // Suspended right away, w/o a real chance to make any progress.
        if (g_SystemInfo.dwNumberOfProcessors > 1 && (tries++) % 20 != 0)
        {
            YieldProcessorNormalized(); // play nice on hyperthreaded CPUs
        }
        else
        {
            __SwitchToThread(0, ++dwSwitchCount); // don't spin on uniproc machines
        }
    }

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
        if (str == STR_Success)
        {
            (&g_profControlBlock)->RuntimeThreadSuspended((ThreadID)this);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    if (pdwSuspendCount != NULL)
    {
        *pdwSuspendCount = dwSuspendCount;
    }
    _ASSERTE(str != (SuspendThreadResult) -1);
    return str;

}
#endif // DISABLE_THREADSUSPEND

// On non-Windows CORECLR platforms remove Thread::ResumeThread support
#ifndef DISABLE_THREADSUSPEND
DWORD Thread::ResumeThread()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE (m_ThreadHandleForResume != INVALID_HANDLE_VALUE);

    //DWORD res = ClrResumeThread(GetThreadHandle());
    DWORD res = ClrResumeThread(m_ThreadHandleForResume);
    _ASSERTE (res != 0 && "Thread is not previously suspended");
#ifdef _DEBUG_IMPL
    _ASSERTE (!m_Creator.IsCurrentThread());
    if ((res != (DWORD)-1) && (res != 0))
    {
        Thread * pCurThread = GetThreadNULLOk();
        if (pCurThread != NULL)
        {
            _ASSERTE(pCurThread->dbg_m_cSuspendedThreads > 0);
            pCurThread->dbg_m_cSuspendedThreads --;
            _ASSERTE(pCurThread->dbg_m_cSuspendedThreadsWithoutOSLock <= pCurThread->dbg_m_cSuspendedThreads);
        }
    }
#endif
    if (res != (DWORD) -1 && res != 0)
    {
        DecCantAllocCount();
    }
#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
        if ((res != 0) && (res != (DWORD)-1))
        {
            (&g_profControlBlock)->RuntimeThreadResumed((ThreadID)this);
        }
        END_PROFILER_CALLBACK();
    }
#endif
    return res;

}
#endif // DISABLE_THREADSUSPEND

#ifdef _DEBUG
void* forceStackA;

// CheckSuspended
//   Checks whether the given thread is currently suspended.
//   Note that if we cannot determine the true suspension status
//   of the thread, we succeed.  Intended to be used in asserts
//   in operations that require the target thread to be suspended.
// Arguments:
//   pThread - The thread to examine.
// Return value:
//   FALSE, if the thread is definitely not suspended.
//   TRUE, otherwise.
static inline BOOL CheckSuspended(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() != pThread);
    _ASSERTE(CheckPointer(pThread));

#ifndef DISABLE_THREADSUSPEND
    DWORD dwSuspendCount;
    Thread::SuspendThreadResult str = pThread->SuspendThread(FALSE, &dwSuspendCount);
    forceStackA = &dwSuspendCount;
    if (str == Thread::STR_Success)
    {
        pThread->ResumeThread();
        return dwSuspendCount >= 1;
    }
#endif // !DISABLE_THREADSUSPEND
    return TRUE;
}
#endif //_DEBUG

BOOL EEGetThreadContext(Thread *pThread, CONTEXT *pContext)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(CheckSuspended(pThread));

    BOOL ret =  pThread->GetThreadContext(pContext);

    STRESS_LOG6(LF_SYNC, LL_INFO1000, "Got thread context ret = %d EIP = %p ESP = %p EBP = %p, pThread = %p, ContextFlags = 0x%x\n",
        ret, GetIP(pContext), GetSP(pContext), GetFP(pContext), pThread, pContext->ContextFlags);

    return ret;

}

BOOL EESetThreadContext(Thread *pThread, const CONTEXT *pContext)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef TARGET_X86
    _ASSERTE(CheckSuspended(pThread));
#endif

    BOOL ret = pThread->SetThreadContext(pContext);

    STRESS_LOG6(LF_SYNC, LL_INFO1000, "Set thread context ret = %d EIP = %p ESP = %p EBP = %p, pThread = %p, ContextFlags = 0x%x\n",
        ret, GetIP((CONTEXT*)pContext), GetSP((CONTEXT*)pContext), GetFP((CONTEXT*)pContext), pThread, pContext->ContextFlags);

    return ret;
}

// Context passed down through a stack crawl (see code below).
struct StackCrawlContext
{
    enum SCCType
    {
        SCC_CheckWithinEH   = 0x00000001,
        SCC_CheckWithinCer  = 0x00000002,
    };
    Thread* pAbortee;
    int         eType;
    BOOL        fUnprotectedCode;
    BOOL        fWithinEHClause;
    BOOL        fWithinCer;
    BOOL        fHasManagedCodeOnStack;
    BOOL        fWriteToStressLog;

    BOOL        fHaveLatchedCF;
    CrawlFrame  LatchedCF;
};

// Crawl the stack looking for Thread Abort related information (whether we're executing inside a CER or an error handling clauses
// of some sort).
static StackWalkAction TAStackCrawlCallBackWorker(CrawlFrame* pCf, StackCrawlContext *pData)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(pData->eType & (StackCrawlContext::SCC_CheckWithinCer | StackCrawlContext::SCC_CheckWithinEH));

    if(pCf->IsFrameless())
    {
        IJitManager* pJitManager = pCf->GetJitManager();
        _ASSERTE(pJitManager);
        if (pJitManager && !pData->fHasManagedCodeOnStack)
        {
            pData->fHasManagedCodeOnStack = TRUE;
        }
    }

    // Get the method for this frame if it exists (might not be a managed method, so check the explicit frame if that's what we're
    // looking at).
    MethodDesc *pMD = pCf->GetFunction();
    Frame *pFrame = pCf->GetFrame();
    if (pMD == NULL && pFrame != NULL)
        pMD = pFrame->GetFunction();

    // Non-method frames don't interest us.
    if (pMD == NULL)
        return SWA_CONTINUE;

    #if defined(_DEBUG)
    #define METHODNAME(pFunc) (pFunc?pFunc->m_pszDebugMethodName:"<n/a>")
    #else
    #define METHODNAME(pFunc) "<n/a>"
    #endif
    if (pData->fWriteToStressLog)
    {
        STRESS_LOG5(LF_EH, LL_INFO100, "TAStackCrawlCallBack: STACKCRAWL method:%pM ('%s'), offset %x, Frame:%p, FrameVtable = %pV\n",
            pMD, METHODNAME(pMD), pCf->IsFrameless()?pCf->GetRelOffset():0, pFrame, pCf->IsFrameless()?0:(*(void**)pFrame));
    }
    #undef METHODNAME


    // If we weren't asked about EH clauses then we can return now (stop the stack trace if we have a definitive answer on the CER
    // question, move to the next frame otherwise).
    if ((pData->eType & StackCrawlContext::SCC_CheckWithinEH) == 0)
        return ((pData->fWithinCer || pData->fUnprotectedCode) && pData->fHasManagedCodeOnStack) ? SWA_ABORT : SWA_CONTINUE;

    // If we already discovered we're within an EH clause but are still processing (presumably to determine whether we're within a
    // CER), then we can just skip to the next frame straight away. Also terminate here if the current frame is not frameless since
    // there isn't any useful EH information for non-managed frames.
    if (pData->fWithinEHClause || !pCf->IsFrameless())
        return SWA_CONTINUE;

    IJitManager* pJitManager = pCf->GetJitManager();
    _ASSERTE(pJitManager);

    EH_CLAUSE_ENUMERATOR pEnumState;
    unsigned EHCount = pJitManager->InitializeEHEnumeration(pCf->GetMethodToken(), &pEnumState);
    if (EHCount == 0)
        // We do not have finally clause here.
        return SWA_CONTINUE;

    DWORD offs = (DWORD)pCf->GetRelOffset();

    if (!pCf->IsActiveFrame())
    {
        // If we aren't the topmost method, then our IP is a return address and
        // we can't use it to directly compare against the EH ranges because we
        // may be in an cloned finally which has a call as the last instruction.

        offs--;
    }

    if (pData->fWriteToStressLog)
    {
        STRESS_LOG1(LF_EH, LL_INFO100, "TAStackCrawlCallBack: STACKCRAWL Offset 0x%x V\n", offs);
    }
    EE_ILEXCEPTION_CLAUSE EHClause;

    StackWalkAction action = SWA_CONTINUE;
#ifndef FEATURE_EH_FUNCLETS
    // On X86, the EH encoding for catch clause is completely mess.
    // If catch clause is in its own basic block, the end of catch includes everything in the basic block.
    // For nested catch, the end of catch may include several jmp instructions after JIT_EndCatch call.
    // To better decide if we are inside a nested catch, we check if offs-1 is in more than one catch clause.
    DWORD countInCatch = 0;
    BOOL fAtJitEndCatch = FALSE;
    if (pData->pAbortee == GetThread() &&
        pData->pAbortee->ThrewControlForThread() == Thread::InducedThreadRedirectAtEndOfCatch &&
        GetControlPC(pCf->GetRegisterSet()) == (PCODE)GetIP(pData->pAbortee->GetAbortContext()))
    {
        fAtJitEndCatch = TRUE;
        offs -= 1;
    }
#endif  // !FEATURE_EH_FUNCLETS

    for(ULONG i=0; i < EHCount; i++)
    {
        pJitManager->GetNextEHClause(&pEnumState, &EHClause);
        _ASSERTE(IsValidClause(&EHClause));

        // !!! If this function is called on Aborter thread, we should check for finally only.

        // !!! If this function is called on Aborter thread, we should check for finally only.
        // !!! Catch and filter clause are skipped.  In UserAbort, the first thing after ReadyForAbort
        // !!! is to check if the target thread is processing exception.
        // !!! If exception is in flight, we don't induce ThreadAbort.  Instead at the end of Jit_EndCatch
        // !!! we will handle abort.
        if (pData->pAbortee != GetThreadNULLOk() && !IsFaultOrFinally(&EHClause))
        {
            continue;
        }
        if (offs >= EHClause.HandlerStartPC &&
            offs < EHClause.HandlerEndPC)
        {
#ifndef FEATURE_EH_FUNCLETS
            if (fAtJitEndCatch)
            {
                // On X86, JIT's EH info may include the instruction after JIT_EndCatch inside the same catch
                // clause if it is in the same basic block.
                // So for this case, the offs is in at least one catch handler, but since we are at the end of
                // catch, this one should not be counted.
                countInCatch ++;
                if (countInCatch == 1)
                {
                    continue;
                }
            }
#endif // !FEATURE_EH_FUNCLETS
            pData->fWithinEHClause = true;
            // We're within an EH clause. If we're asking about CERs too then stop the stack walk if we've reached a conclusive
            // result or continue looking otherwise. Else we can stop the stackwalk now.
            if (pData->eType & StackCrawlContext::SCC_CheckWithinCer)
            {
                action = (pData->fWithinCer || pData->fUnprotectedCode) ? SWA_ABORT : SWA_CONTINUE;
            }
            else
            {
                action = SWA_ABORT;
        }
            break;
    }
    }

#ifndef FEATURE_EH_FUNCLETS
#ifdef _DEBUG
    if (fAtJitEndCatch)
    {
        _ASSERTE (countInCatch > 0);
    }
#endif   // _DEBUG
#endif   // !FEATURE_EH_FUNCLETS
    return action;
}

// Wrapper around code:TAStackCrawlCallBackWorker that abstracts away the differences between the reporting order
// of x86 and 64-bit stackwalker implementations, and also deals with interop calls that have an implicit reliability
// contract. If a P/Invoke or CLR->COM call returns SafeHandle or CriticalHandle, the IL stub could be aborted
// before having a chance to store the native handle into the Safe/CriticalHandle object. Therefore such calls are
// treated as unbreakable by convention.
StackWalkAction TAStackCrawlCallBack(CrawlFrame* pCf, void* data)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    StackCrawlContext *pData = (StackCrawlContext *)data;

    // We have the current frame in pCf and possibly one latched frame in pData->LatchedCF. This enumeration
    // describes which of these should be passed to code:TAStackCrawlCallBackWorker and in what order.
    enum LatchedFrameAction
    {
        DiscardLatchedFrame,    // forget the latched frame, report the current one
        DiscardCurrentFrame,    // ignore the current frame, report the latched one
        ProcessLatchedInOrder,  // report the latched frame, then report the current frame
        ProcessLatchedReversed, // report the current frame, then report the latched frame
        LatchCurrentFrame       // latch the current frame, don't report anything
    }
    frameAction = DiscardLatchedFrame;

#ifdef TARGET_X86
    // On X86 the IL stub method is reported to us before the frame with the actual interop method. We need to
    // swap the order because if the worker saw the IL stub - which is a CER root - first, it would terminate the
    // stack walk and wouldn't allow the thread to be aborted, regardless of how the interop method is annotated.
    if (pData->fHaveLatchedCF)
    {
        // Does the current and latched frame represent the same call?
        if (pCf->pFrame == pData->LatchedCF.pFrame)
        {
            // Report the interop method (current frame) which may be annotated, then the IL stub.
            frameAction = ProcessLatchedReversed;
        }
        else
        {
            // The two frames are unrelated - process them in order.
            frameAction = ProcessLatchedInOrder;
        }
        pData->fHaveLatchedCF = FALSE;
    }
    else
    {
        MethodDesc *pMD = pCf->GetFunction();
        if (pMD != NULL && pMD->IsILStub() && InlinedCallFrame::FrameHasActiveCall(pCf->pFrame))
        {
            // This may be IL stub for an interesting interop call - latch it.
            frameAction = LatchCurrentFrame;
        }
    }
#else // TARGET_X86
    // On 64-bit the IL stub method is reported after the actual interop method so we don't have to swap them.
    // However, we still want to discard the interop method frame if the call is unbreakable by convention.
    if (pData->fHaveLatchedCF)
    {
        frameAction = ProcessLatchedInOrder;
        pData->fHaveLatchedCF = FALSE;
    }
    else
    {
        MethodDesc *pMD = pCf->GetFunction();
        if (pCf->GetFrame() != NULL && pMD != NULL && (pMD->IsNDirect() || pMD->IsComPlusCall()))
        {
            // This may be interop method of an interesting interop call - latch it.
            frameAction = LatchCurrentFrame;
        }
    }
#endif // TARGET_X86

    // Execute the "frame action".
    StackWalkAction action;
    switch (frameAction)
    {
        case DiscardLatchedFrame:
                action = TAStackCrawlCallBackWorker(pCf, pData);
                break;

        case DiscardCurrentFrame:
                action = TAStackCrawlCallBackWorker(&pData->LatchedCF, pData);
                break;

        case ProcessLatchedInOrder:
                action = TAStackCrawlCallBackWorker(&pData->LatchedCF, pData);
                if (action == SWA_CONTINUE)
                    action = TAStackCrawlCallBackWorker(pCf, pData);
                break;

        case ProcessLatchedReversed:
                action = TAStackCrawlCallBackWorker(pCf, pData);
                if (action == SWA_CONTINUE)
                    action = TAStackCrawlCallBackWorker(&pData->LatchedCF, pData);
                break;

        case LatchCurrentFrame:
                pData->LatchedCF = *pCf;
                pData->fHaveLatchedCF = TRUE;
                action = SWA_CONTINUE;
                break;

        default:
            UNREACHABLE();
    }
    return action;
}

// Is the current thread currently executing within a constrained execution region?
BOOL Thread::IsExecutingWithinCer()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!g_fEEStarted)
        return FALSE;

    Thread *pThread = GetThread();
    StackCrawlContext sContext = { pThread,
                                   StackCrawlContext::SCC_CheckWithinCer,
        FALSE,
        FALSE,
        FALSE,
        FALSE,
        FALSE,
        FALSE};

    pThread->StackWalkFrames(TAStackCrawlCallBack, &sContext);

#ifdef STRESS_LOG
    if (sContext.fWithinCer && StressLog::StressLogOn(~0u, 0))
    {
        // If stress log is on, write info to stress log
        StackCrawlContext sContext1 = { pThread,
                                        StackCrawlContext::SCC_CheckWithinCer,
            FALSE,
            FALSE,
            FALSE,
            FALSE,
            TRUE,
            FALSE};

        pThread->StackWalkFrames(TAStackCrawlCallBack, &sContext1);
    }
#endif

    return sContext.fWithinCer;
}

#if defined(TARGET_AMD64) && defined(FEATURE_HIJACK)
BOOL Thread::IsSafeToInjectThreadAbort(PTR_CONTEXT pContextToCheck)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pContextToCheck != NULL);
    }
    CONTRACTL_END;

    EECodeInfo codeInfo(GetIP(pContextToCheck));
    _ASSERTE(codeInfo.IsValid());

    // Check if the method uses a frame register. If it does not, then RSP will be used by the OS as the frame register
    // and returned as the EstablisherFrame. This is fine at any instruction in the method (including epilog) since there is always a
    // difference of stackslot size between the callerSP and the callee SP due to return address having been pushed on the stack.
    if (!codeInfo.HasFrameRegister())
    {
        return TRUE;
    }

    BOOL fSafeToInjectThreadAbort = TRUE;

    if (IsIPInEpilog(pContextToCheck, &codeInfo, &fSafeToInjectThreadAbort))
    {
        return fSafeToInjectThreadAbort;
    }
    else
    {
        return TRUE;
    }
}
#endif // defined(TARGET_AMD64) && defined(FEATURE_HIJACK)

#ifdef TARGET_AMD64
// CONTEXT_CONTROL does not include any nonvolatile registers that might be the frame pointer.
#define CONTEXT_MIN_STACKWALK (CONTEXT_CONTROL | CONTEXT_INTEGER)
#else
#define CONTEXT_MIN_STACKWALK (CONTEXT_CONTROL)
#endif


BOOL Thread::ReadyForAsyncException()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!IsAbortRequested())
    {
        return FALSE;
    }

    if (GetThreadNULLOk() == this && HasThreadStateNC (TSNC_PreparingAbort) && !IsRudeAbort() )
    {
        STRESS_LOG0(LF_APPDOMAIN, LL_INFO10, "in Thread::ReadyForAbort  PreparingAbort\n");
        // Avoid recursive call
        return FALSE;
    }

    // The thread requests not to be aborted.  Honor this for safe abort.
    if (!IsRudeAbort() && IsAsyncPrevented())
    {
        STRESS_LOG0(LF_APPDOMAIN, LL_INFO10, "in Thread::ReadyForAbort  AsyncPrevented\n");
        return FALSE;
    }

    REGDISPLAY rd;

    Frame *pStartFrame = NULL;
    if (ThrewControlForThread() == Thread::InducedThreadRedirect ||
        ThrewControlForThread() == Thread::InducedThreadRedirectAtEndOfCatch)
    {
        _ASSERTE(GetThread() == this);
        _ASSERTE(ExecutionManager::IsManagedCode(GetIP(m_OSContext)));
        FillRegDisplay(&rd, m_OSContext);

        if (ThrewControlForThread() == Thread::InducedThreadRedirectAtEndOfCatch)
        {
            // On 64bit, this function may be called from COMPlusCheckForAbort when
            // stack has not unwound, but m_OSContext points to the place after unwind.
            //
            TADDR sp = GetSP(m_OSContext);
            Frame *pFrameAddr = m_pFrame;
            while (pFrameAddr < (LPVOID)sp)
            {
                pFrameAddr = pFrameAddr->Next();
            }
            if (pFrameAddr != m_pFrame)
            {
                pStartFrame = pFrameAddr;
            }
        }
#if defined(TARGET_AMD64) && defined(FEATURE_HIJACK)
        else if (ThrewControlForThread() == Thread::InducedThreadRedirect)
        {
            if (!IsSafeToInjectThreadAbort(m_OSContext))
            {
                STRESS_LOG0(LF_EH, LL_INFO10, "Thread::ReadyForAbort: Not injecting abort since we are at an unsafe instruction.\n");
                return FALSE;
            }
        }
#endif // defined(TARGET_AMD64) && defined(FEATURE_HIJACK)
    }
    else
    {
        if (GetFilterContext())
        {
            FillRegDisplay(&rd, GetFilterContext());
        }
        else
        {
             CONTEXT ctx;
             SetIP(&ctx, 0);
             SetSP(&ctx, 0);
             FillRegDisplay(&rd, &ctx);
        }
    }

#ifdef STRESS_LOG
    REGDISPLAY rd1;
    if (StressLog::StressLogOn(~0u, 0))
    {
        CONTEXT ctx1;
        CopyRegDisplay(&rd, &rd1, &ctx1);
    }
#endif

    // Walk the stack to determine if we are running in Constrained Execution Region or finally EH clause (in the non-rude abort
    // case). We cannot initiate an abort in these circumstances.
    StackCrawlContext TAContext =
    {
        this,
        StackCrawlContext::SCC_CheckWithinCer | (IsRudeAbort() ? 0 : StackCrawlContext::SCC_CheckWithinEH),
        FALSE,
        FALSE,
        FALSE,
        FALSE,
        FALSE
    };

    StackWalkFramesEx(&rd, TAStackCrawlCallBack, &TAContext, QUICKUNWIND, pStartFrame);

    _ASSERTE(TAContext.fHasManagedCodeOnStack || !IsAbortInitiated() || (GetThreadNULLOk() != this));

    if (TAContext.fWithinCer)
    {
        STRESS_LOG0(LF_APPDOMAIN, LL_INFO10, "in Thread::ReadyForAbort  RunningCer\n");
        return FALSE;
    }

#ifdef STRESS_LOG
    if (StressLog::StressLogOn(~0u, 0) &&
        (IsRudeAbort() || !TAContext.fWithinEHClause))
    {
        //Save into stresslog.
        StackCrawlContext TAContext1 =
        {
            this,
            StackCrawlContext::SCC_CheckWithinCer | (IsRudeAbort() ? 0 : StackCrawlContext::SCC_CheckWithinEH),
            FALSE,
            FALSE,
            FALSE,
            FALSE,
            TRUE
        };

        StackWalkFramesEx(&rd1, TAStackCrawlCallBack, &TAContext1, QUICKUNWIND, pStartFrame);
    }
#endif

    if (IsRudeAbort()) {
        // If it is rude abort, there is no additional restriction on abort.
        STRESS_LOG0(LF_APPDOMAIN, LL_INFO10, "in Thread::ReadyForAbort  RudeAbort\n");
        return TRUE;
    }

    if (TAContext.fWithinEHClause)
    {
        STRESS_LOG0(LF_APPDOMAIN, LL_INFO10, "in Thread::ReadyForAbort  RunningEHClause\n");
    }

    // If we are running finally, we can not abort for Safe Abort.
    return !TAContext.fWithinEHClause;
}

BOOL Thread::IsRudeAbort()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return (IsAbortRequested() && (m_AbortType == EEPolicy::TA_Rude));
}

//
// If the OS is down in kernel mode when we do a GetThreadContext,any
// updates we make to the context will not take effect if we try to do
// a SetThreadContext.  As a result, newer OSes expose the idea of
// "trap frame reporting" which will tell us if it is unsafe to modify
// the context and pass it along to SetThreadContext.
//
// On OSes that support trap frame reporting, we will return FALSE if
// we can determine that the OS is not in user mode.  Otherwise, we
// return TRUE.
//
BOOL Thread::IsContextSafeToRedirect(const CONTEXT* pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL isSafeToRedirect = TRUE;

#ifndef TARGET_UNIX

#if !defined(TARGET_X86)
    // In some cases (x86 WOW64, ARM32 on ARM64) Windows will not set the CONTEXT_EXCEPTION_REPORTING flag
    // if the thread is executing in kernel mode (i.e. in the middle of a syscall or exception handling).
    // Therefore, we should treat the absence of the CONTEXT_EXCEPTION_REPORTING flag as an indication that
    // it is not safe to manipulate with the current state of the thread context.
    // Note: the x86 WOW64 case is already handled in GetSafelyRedirectableThreadContext; in addition, this
    // flag is never set on Windows7 x86 WOW64. So this check is valid for non-x86 architectures only.
    isSafeToRedirect = (pContext->ContextFlags & CONTEXT_EXCEPTION_REPORTING) != 0;
#endif // !defined(TARGET_X86)

    if (pContext->ContextFlags & CONTEXT_EXCEPTION_REPORTING)
    {
        if (pContext->ContextFlags & (CONTEXT_SERVICE_ACTIVE|CONTEXT_EXCEPTION_ACTIVE))
        {
            // cannot process exception
            LOG((LF_ALWAYS, LL_WARNING, "thread [os id=0x08%x id=0x08%x] redirect failed due to ContextFlags of 0x%08x\n", (DWORD)m_OSThreadId, m_ThreadId, pContext->ContextFlags));
            isSafeToRedirect = FALSE;
        }
    }

#endif // !TARGET_UNIX

    return isSafeToRedirect;
}

void Thread::SetAbortEndTime(ULONGLONG endTime, BOOL fRudeAbort)
{
    LIMITED_METHOD_CONTRACT;

    {
        AbortRequestLockHolder lh(this);
        if (fRudeAbort)
        {
            if (endTime < m_RudeAbortEndTime)
            {
                m_RudeAbortEndTime = endTime;
            }
        }
        else
        {
            if (endTime < m_AbortEndTime)
            {
                m_AbortEndTime = endTime;
            }
        }
    }

}

bool UseActivationInjection()
{
#ifdef TARGET_UNIX
    return true;
#else
    return Thread::UseSpecialUserModeApc();
#endif
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT
Thread::UserAbort(EEPolicy::ThreadAbortTypes abortType, DWORD timeout)
{
    CONTRACTL
    {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    STRESS_LOG2(LF_SYNC | LF_APPDOMAIN, LL_INFO100, "UserAbort Thread %p Thread Id = %x\n", this, GetThreadId());

    BOOL fHoldingThreadStoreLock = ThreadStore::HoldingThreadStore();

    AbortControlHolder AbortController(this);

    // Swap in timeout
    if (timeout != INFINITE)
    {
        ULONG64 curTime = CLRGetTickCount64();
        ULONG64 newEndTime = curTime + timeout;

        SetAbortEndTime(newEndTime, abortType == EEPolicy::TA_Rude);
    }

    MarkThreadForAbort(abortType);

    Thread *pCurThread = GetThreadNULLOk();

    // If aborting self
    if (this == pCurThread)
    {
        SetAbortInitiated();
#ifdef _DEBUG
        m_dwAbortPoint = 1;
#endif

        GCX_COOP();

        OBJECTREF exceptObj;

        if (IsRudeAbort())
        {
            exceptObj = CLRException::GetBestThreadAbortException();
        }
        else
        {
            EEException eeExcept(kThreadAbortException);
            exceptObj = CLRException::GetThrowableFromException(&eeExcept);
        }

        RaiseTheExceptionInternalOnly(exceptObj, FALSE);
    }

    _ASSERTE(this != pCurThread);      // Aborting another thread.

#ifdef _DEBUG
    DWORD elapsed_time = 0;
#endif

    // We do not want this thread to be alerted.
    ThreadPreventAsyncHolder preventAsync(pCurThread != NULL);

#ifdef _DEBUG
    // If UserAbort times out, put up msgbox once.
    BOOL fAlreadyAssert = FALSE;
#endif

#if !defined(DISABLE_THREADSUSPEND)
    DWORD dwSwitchCount = 0;
#endif // !defined(DISABLE_THREADSUSPEND)

    while (true)
    {
        // Lock the thread store
        LOG((LF_SYNC, INFO3, "UserAbort obtain lock\n"));

        ULONGLONG abortEndTime = GetAbortEndTime();
        if (abortEndTime != MAXULONGLONG)
        {
            ULONGLONG now_time = CLRGetTickCount64();

            if (now_time >= abortEndTime)
            {
                // timeout, but no action on timeout.
                // Debugger can call this function to abort func-eval with a timeout
                return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
            }
        }

        // Thread abort needs to walk stack to decide if thread abort can proceed.
        // It is unsafe to crawl a stack of thread if the thread is OS-suspended which we do during
        // thread abort.  For example, Thread T1 aborts thread T2.  T2 is suspended by T1. Inside SQL
        // this means that no thread sharing the same scheduler with T2 can run.  If T1 needs a lock which
        // is owned by one thread on the scheduler, T1 will wait forever.
        // Our solution is to move T2 to a safe point, resume it, and then do stack crawl.

        // We need to make sure that ThreadStoreLock is released after CheckForAbort.  This makes sure
        // that ThreadAbort does not race against GC.
        class CheckForAbort
        {
        private:
            Thread *m_pThread;
            BOOL m_fHoldingThreadStoreLock;
            BOOL m_NeedRelease;
        public:
            CheckForAbort(Thread *pThread, BOOL fHoldingThreadStoreLock)
            : m_pThread(pThread),
            m_fHoldingThreadStoreLock(fHoldingThreadStoreLock),
            m_NeedRelease(FALSE)
            {
            }
            void Activate()
            {
                m_NeedRelease = TRUE;
                if (!m_fHoldingThreadStoreLock)
                {
                    ThreadSuspend::LockThreadStore(ThreadSuspend::SUSPEND_OTHER);
                }
                ThreadStore::ResetStackCrawlEvent();

                // The thread being aborted may clear the TS_AbortRequested bit and the matching increment
                // of g_TrapReturningThreads behind our back. Increment g_TrapReturningThreads here
                // to ensure that we stop for the stack crawl even if the TS_AbortRequested bit is cleared.
                ThreadStore::TrapReturningThreads(TRUE);
            }
            void NeedStackCrawl()
            {
                m_pThread->SetThreadState(Thread::TS_StackCrawlNeeded);
            }
            ~CheckForAbort()
            {
                Release();
            }
            void Release()
            {
                if (m_NeedRelease)
                {
                    m_NeedRelease = FALSE;
                    ThreadStore::TrapReturningThreads(FALSE);
                    ThreadStore::SetStackCrawlEvent();
                    m_pThread->ResetThreadState(TS_StackCrawlNeeded);
                    if (!m_fHoldingThreadStoreLock)
                    {
                        ThreadSuspend::UnlockThreadStore();
                    }
                }
            }
        };
        CheckForAbort checkForAbort(this, fHoldingThreadStoreLock);
        if (!UseActivationInjection())
        {
            checkForAbort.Activate();
        }

        // We own TS lock.  The state of the Thread can not be changed.
        if (m_State & TS_Unstarted)
        {
            // This thread is not yet started.
#ifdef _DEBUG
            m_dwAbortPoint = 2;
#endif

            return S_OK;
        }

        if (GetThreadHandle() == INVALID_HANDLE_VALUE &&
            (m_State & TS_Unstarted) == 0)
        {
            // The thread is going to die or is already dead.
            UnmarkThreadForAbort();
#ifdef _DEBUG
            m_dwAbortPoint = 3;
#endif

            return S_OK;
        }

        // What if someone else has this thread suspended already?   It'll depend where the
        // thread got suspended.
        //
        // User Suspend:
        //     We'll just set the abort bit and hope for the best on the resume.
        //
        // GC Suspend:
        //    If it's suspended in jitted code, we'll hijack the IP.
        //    <REVISIT_TODO> Consider race w/ GC suspension</REVISIT_TODO>
        //    If it's suspended but not in jitted code, we'll get suspended for GC, the GC
        //    will complete, and then we'll abort the target thread.
        //

        // It's possible that the thread has completed the abort already.
        //
        if (!(m_State & TS_AbortRequested))
        {
#ifdef _DEBUG
            m_dwAbortPoint = 4;
#endif

            return S_OK;
        }

        // If a thread is Dead or Detached, abort is a NOP.
        //
        if (m_State & (TS_Dead | TS_Detached | TS_TaskReset))
        {
            UnmarkThreadForAbort();

#ifdef _DEBUG
            m_dwAbortPoint = 5;
#endif
            return S_OK;
        }

        // It's possible that some stub notices the AbortRequested bit -- even though we
        // haven't done any real magic yet.  If the thread has already started it's abort, we're
        // done.
        //
        // Two more cases can be folded in here as well.  If the thread is unstarted, it'll
        // abort when we start it.
        //
        // If the thread is user suspended (SyncSuspended) -- we're out of luck.  Set the bit and
        // hope for the best on resume.
        //
        if ((m_State & TS_AbortInitiated) && !IsRudeAbort())
        {
#ifdef _DEBUG
            m_dwAbortPoint = 6;
#endif
            break;
        }

#ifdef FEATURE_THREAD_ACTIVATION
        if (UseActivationInjection())
        {
            InjectActivation(ActivationReason::ThreadAbort);
        }
        else
#endif // FEATURE_THREAD_ACTIVATION
        {
            BOOL fOutOfRuntime = FALSE;
            BOOL fNeedStackCrawl = FALSE;

#ifdef DISABLE_THREADSUSPEND
            // On platforms that do not support safe thread suspension we have to
            // rely on the GCPOLL mechanism; the mechanism is activated above by
            // TrapReturningThreads.  However when reading shared state we need
            // to erect appropriate memory barriers. So the interlocked operation
            // below ensures that any future reads on this thread will happen after
            // any earlier writes on a different thread have taken effect.
            InterlockedOr((LONG*)&m_State, 0);

#else // DISABLE_THREADSUSPEND

            // Win32 suspend the thread, so it isn't moving under us.
            SuspendThreadResult str = SuspendThread();
            switch (str)
            {
            case STR_Success:
                break;

            case STR_Failure:
            case STR_UnstartedOrDead:
            case STR_NoStressLog:
                checkForAbort.Release();
                __SwitchToThread(0, ++dwSwitchCount);
                continue;

            default:
                UNREACHABLE();
            }

            _ASSERTE(str == STR_Success);

#endif // DISABLE_THREADSUSPEND

            // It's possible that the thread has completed the abort already.
            //
            if (!(m_State & TS_AbortRequested))
            {
#ifndef DISABLE_THREADSUSPEND
                ResumeThread();
#endif

#ifdef _DEBUG
                m_dwAbortPoint = 63;
#endif
                return S_OK;
            }

            // Check whether some stub noticed the AbortRequested bit in-between our test above
            // and us suspending the thread.
            if ((m_State & TS_AbortInitiated) && !IsRudeAbort())
            {
#ifndef DISABLE_THREADSUSPEND
                ResumeThread();
#endif
#ifdef _DEBUG
                m_dwAbortPoint = 65;
#endif
                break;
            }

            // If Threads is stopped under a managed debugger, it will have both
            // TS_DebugSuspendPending and TS_SyncSuspended, regardless of whether
            // the thread is actually suspended or not.
            if (m_State & TS_SyncSuspended)
            {
#ifndef DISABLE_THREADSUSPEND
                ResumeThread();
#endif
                checkForAbort.Release();
#ifdef _DEBUG
                m_dwAbortPoint = 7;
#endif

                //
                // If it's stopped by the debugger, we don't want to throw an exception.
                // Debugger suspension is to have no effect of the runtime behaviour.
                //
                if (m_State & TS_DebugSuspendPending)
                {
                    return S_OK;
                }

                COMPlusThrow(kThreadStateException, IDS_EE_THREAD_ABORT_WHILE_SUSPEND);
            }

            // If the thread has no managed code on it's call stack, abort is a NOP.  We're about
            // to touch the unmanaged thread's stack -- for this to be safe, we can't be
            // Dead/Detached/Unstarted.
            //
            _ASSERTE(!(m_State & (  TS_Dead
                                | TS_Detached
                                | TS_Unstarted)));

#if defined(TARGET_X86) && !defined(FEATURE_EH_FUNCLETS)
            // TODO WIN64: consider this if there is a way to detect of managed code on stack.
            if ((m_pFrame == FRAME_TOP)
                && (GetFirstCOMPlusSEHRecord(this) == EXCEPTION_CHAIN_END)
            )
            {
#ifndef DISABLE_THREADSUSPEND
                ResumeThread();
#endif
#ifdef _DEBUG
                m_dwAbortPoint = 8;
#endif

                return S_OK;
            }
#endif // TARGET_X86


            if (!m_fPreemptiveGCDisabled)
            {
                if ((m_pFrame != FRAME_TOP) && m_pFrame->IsTransitionToNativeFrame()
#if defined(TARGET_X86) && !defined(FEATURE_EH_FUNCLETS)
                    && ((size_t) GetFirstCOMPlusSEHRecord(this) > ((size_t) m_pFrame) - 20)
#endif // TARGET_X86
                    )
                {
                    fOutOfRuntime = TRUE;
                }
            }

            checkForAbort.NeedStackCrawl();
            if (!m_fPreemptiveGCDisabled)
            {
                fNeedStackCrawl = TRUE;
            }
#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
            else
            {
                HandleJITCaseForAbort();
            }
#endif // FEATURE_HIJACK && !TARGET_UNIX

#ifndef DISABLE_THREADSUSPEND
            // The thread is not suspended now.
            ResumeThread();
#endif

            if (!fNeedStackCrawl)
            {
                goto LPrepareRetry;
            }

            if (!ReadyForAbort()) {
                goto LPrepareRetry;
            }

            // !!! Check for Exception in flight should happen before induced thread abort.
            // !!! ReadyForAbort skips catch and filter clause.

            // If an exception is currently being thrown, one of two things will happen.  Either, we'll
            // catch, and notice the abort request in our end-catch, or we'll not catch [in which case
            // we're leaving managed code anyway.  The top-most handler is responsible for resetting
            // the bit.
            //
            if (HasException() &&
                // For rude abort, we will initiated abort
                !IsRudeAbort())
            {
#ifdef _DEBUG
                m_dwAbortPoint = 9;
#endif
                break;
            }

            // If the thread is in sleep, wait, or join interrupt it
            // However, we do NOT want to interrupt if the thread is already processing an exception
            if (m_State & TS_Interruptible)
            {
                UserInterrupt(TI_Abort);        // if the user wakes up because of this, it will read the
                                                // abort requested bit and initiate the abort
#ifdef _DEBUG
                m_dwAbortPoint = 10;
#endif
                goto LPrepareRetry;
            }

            if (fOutOfRuntime)
            {
                // If the thread is running outside the EE, and is behind a stub that's going
                // to catch...
#ifdef _DEBUG
                m_dwAbortPoint = 11;
#endif
                break;
            }

            // Ok.  It's not in managed code, nor safely out behind a stub that's going to catch
            // it on the way in.  We have to poll.

LPrepareRetry:

            checkForAbort.Release();
        }

        // Don't do a Sleep.  It's possible that the thread we are trying to abort is
        // stuck in unmanaged code trying to get into the apartment that we are supposed
        // to be pumping!  Instead, ping the current thread's handle.  Obviously this
        // will time out, but it will pump if we need it to.
        if (pCurThread)
        {
            pCurThread->Join(ABORT_POLL_TIMEOUT, TRUE);
        }
        else
        {
            ClrSleepEx(ABORT_POLL_TIMEOUT, FALSE);
        }


#ifdef _DEBUG
        elapsed_time += ABORT_POLL_TIMEOUT;
        if (g_pConfig->GetGCStressLevel() == 0 && !fAlreadyAssert)
        {
            _ASSERTE(elapsed_time < ABORT_FAIL_TIMEOUT);
            fAlreadyAssert = TRUE;
        }
#endif

    } // while (true)

    if ((GetAbortEndTime() != MAXULONGLONG)  && IsAbortRequested())
    {
        while (TRUE)
        {
            if (!IsAbortRequested())
            {
                return S_OK;
            }

            ULONGLONG curTime = CLRGetTickCount64();
            if (curTime >= GetAbortEndTime())
            {
                break;
            }

            if (pCurThread)
            {
                pCurThread->Join(100, TRUE);
            }
            else
            {
                ClrSleepEx(100, FALSE);
            }
        }

        return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
    }

    return S_OK;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void Thread::SetRudeAbortEndTimeFromEEPolicy()
{
    LIMITED_METHOD_CONTRACT;
    SetAbortEndTime(MAXULONGLONG, TRUE);
}

ULONGLONG Thread::s_NextSelfAbortEndTime = MAXULONGLONG;

void Thread::LockAbortRequest(Thread* pThread)
{
    WRAPPER_NO_CONTRACT;

    DWORD dwSwitchCount = 0;

    while (TRUE) {
        for (unsigned i = 0; i < 10000; i ++) {
            if (VolatileLoad(&(pThread->m_AbortRequestLock)) == 0) {
                break;
            }
            YieldProcessorNormalized(); // indicate to the processor that we are spinning
        }
        if (InterlockedCompareExchange(&(pThread->m_AbortRequestLock),1,0) == 0) {
            return;
        }
        __SwitchToThread(0, ++dwSwitchCount);
    }
}

void Thread::UnlockAbortRequest(Thread *pThread)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE (pThread->m_AbortRequestLock == 1);
    InterlockedExchange(&pThread->m_AbortRequestLock, 0);
}

void Thread::MarkThreadForAbort(EEPolicy::ThreadAbortTypes abortType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    AbortRequestLockHolder lh(this);

#ifdef _DEBUG
    if (abortType == EEPolicy::TA_Rude)
    {
        m_fRudeAborted = TRUE;
    }
#endif

    if (m_AbortType >= (DWORD)abortType)
    {
        // another thread is aborting at a higher level
        return;
    }

    m_AbortType = abortType;

    if (!IsAbortRequested())
    {
        // We must set this before we start flipping thread bits to avoid races where
        // trap returning threads is already high due to other reasons.

        // The thread is asked for abort the first time
        SetAbortRequestBit();
    }
    STRESS_LOG3(LF_APPDOMAIN, LL_ALWAYS, "Mark Thread %p Thread Id = %x for abort (type %d)\n", this, GetThreadId(), abortType);
}

void Thread::SetAbortRequestBit()
{
    WRAPPER_NO_CONTRACT;
    while (TRUE)
    {
        LONG curValue = (LONG)m_State;
        if ((curValue & TS_AbortRequested) != 0)
        {
            break;
        }
        if (InterlockedCompareExchange((LONG*)&m_State, curValue|TS_AbortRequested, curValue) == curValue)
        {
            ThreadStore::TrapReturningThreads(TRUE);

            break;
        }
    }
}

void Thread::RemoveAbortRequestBit()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#ifdef _DEBUG
    // There's a race between removing the TS_AbortRequested bit and decrementing g_TrapReturningThreads
    // We may remove the bit, but before we have a chance to call ThreadStore::TrapReturningThreads(FALSE)
    // DbgFindThread() may execute, and find too few threads with the bit set.
    // To ensure the assert in DbgFindThread does not fire under such a race we set the ChgInFlight before hand.
    CounterHolder trtHolder(&g_trtChgInFlight);
#endif
    while (TRUE)
    {
        LONG curValue = (LONG)m_State;
        if ((curValue & TS_AbortRequested) == 0)
        {
            break;
        }
        if (InterlockedCompareExchange((LONG*)&m_State, curValue&(~TS_AbortRequested), curValue) == curValue)
        {
            ThreadStore::TrapReturningThreads(FALSE);

            break;
        }
    }
}

// Make sure that when AbortRequest bit is cleared, we also dec TrapReturningThreads count.
void Thread::UnmarkThreadForAbort(EEPolicy::ThreadAbortTypes abortType /* = EEPolicy::TA_Rude */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    AbortRequestLockHolder lh(this);

    if (m_AbortType > (DWORD)abortType)
    {
        // Aborting at a higher level
        return;
    }

    m_AbortType = EEPolicy::TA_None;
    m_AbortEndTime = MAXULONGLONG;
    m_RudeAbortEndTime = MAXULONGLONG;

    if (IsAbortRequested())
    {
        RemoveAbortRequestBit();
        ResetThreadState(TS_AbortInitiated);
        m_fRudeAbortInitiated = FALSE;
        ResetUserInterrupted();
    }

    STRESS_LOG2(LF_APPDOMAIN, LL_ALWAYS, "Unmark Thread %p Thread Id = %x for abort \n", this, GetThreadId());
}

void Thread::ResetAbort()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(this == GetThread());
    _ASSERTE(!IsDead());

    UnmarkThreadForAbort();
}


void ThreadSuspend::LockThreadStore(ThreadSuspend::SUSPEND_REASON reason)
{
    CONTRACTL {
        NOTHROW;
    // any thread entering with `PreemptiveGCDisabled` should be prepared to switch mode, thus GC_TRIGGERS
        if ((GetThreadNULLOk() != NULL) && GetThread()->PreemptiveGCDisabled()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    // There's a nasty problem here.  Once we start shutting down because of a
    // process detach notification, threads are disappearing from under us.  There
    // are a surprising number of cases where the dying thread holds the ThreadStore
    // lock.  For example, the finalizer thread holds this during startup in about
    // 10 of our COM BVTs.
    if (!IsAtProcessExit())
    {
        BOOL gcOnTransitions;

        Thread *pCurThread = GetThreadNULLOk();

        gcOnTransitions = GC_ON_TRANSITIONS(FALSE);                // dont do GC for GCStress 3

        // We may be blocked while acquiring s_pThreadStore
        // We should be in preemptive mode when blocked or we could cause suspension in progress
        // to loop forever because it needs to see all threads in preemptive mode.
        BOOL toggleGC = (pCurThread != NULL && pCurThread->PreemptiveGCDisabled());
        if (toggleGC)
            pCurThread->EnablePreemptiveGC();

        LOG((LF_SYNC, INFO3, "Locking thread store\n"));

        // Any thread that holds the thread store lock cannot be stopped by unmanaged breakpoints and exceptions when
        // we're doing managed/unmanaged debugging. Calling SetDebugCantStop(true) on the current thread helps us
        // remember that.
        if (pCurThread)
            IncCantStopCount();

        // This is shutdown aware. If we're in shutdown, and not helper/finalizer/shutdown
        // then this will not take the lock and just block forever.
        ThreadStore::s_pThreadStore->Enter();

        _ASSERTE(ThreadStore::s_pThreadStore->m_holderthreadid.IsUnknown());
        ThreadStore::s_pThreadStore->m_holderthreadid.SetToCurrentThread();

        LOG((LF_SYNC, INFO3, "Locked thread store\n"));

        // Established after we obtain the lock, so only useful for synchronous tests.
        // A thread attempting to suspend us asynchronously already holds this lock.
        ThreadStore::s_pThreadStore->m_HoldingThread = pCurThread;

        if (toggleGC)
            pCurThread->DisablePreemptiveGC();

        GC_ON_TRANSITIONS(gcOnTransitions);
    }
#ifdef _DEBUG
    else
        LOG((LF_SYNC, INFO3, "Locking thread store skipped upon detach\n"));
#endif
}

void ThreadSuspend::UnlockThreadStore(BOOL bThreadDestroyed, ThreadSuspend::SUSPEND_REASON reason)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // There's a nasty problem here.  Once we start shutting down because of a
    // process detach notification, threads are disappearing from under us.  There
    // are a surprising number of cases where the dying thread holds the ThreadStore
    // lock.  For example, the finalizer thread holds this during startup in about
    // 10 of our COM BVTs.
    if (!IsAtProcessExit())
    {
        Thread *pCurThread = GetThreadNULLOk();

        LOG((LF_SYNC, INFO3, "Unlocking thread store\n"));
        _ASSERTE(pCurThread == NULL || ThreadStore::s_pThreadStore->m_HoldingThread == pCurThread);

#ifdef _DEBUG
        // If Thread object has been destroyed, we need to reset the ownership info in Crst.
        _ASSERTE(!bThreadDestroyed || pCurThread == NULL);
        if (bThreadDestroyed) {
            ThreadStore::s_pThreadStore->m_Crst.m_holderthreadid.SetToCurrentThread();
        }
#endif

        ThreadStore::s_pThreadStore->m_HoldingThread = NULL;
        ThreadStore::s_pThreadStore->m_holderthreadid.Clear();
        ThreadStore::s_pThreadStore->Leave();
        LOG((LF_SYNC, INFO3, "Unlocked thread store\n"));

        // We're out of the critical area for managed/unmanaged debugging.
        if (!bThreadDestroyed && pCurThread)
            DecCantStopCount();
    }
#ifdef _DEBUG
    else
        LOG((LF_SYNC, INFO3, "Unlocking thread store skipped upon detach\n"));
#endif
}

#ifdef TARGET_X86
#define CONTEXT_COMPLETE (CONTEXT_FULL | CONTEXT_FLOATING_POINT |       \
                          CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS)
#else
#define CONTEXT_COMPLETE (CONTEXT_FULL | CONTEXT_DEBUG_REGISTERS)
#endif

CONTEXT* AllocateOSContextHelper(BYTE** contextBuffer)
{
    CONTEXT* pOSContext = NULL;

#if !defined(TARGET_UNIX) && (defined(TARGET_X86) || defined(TARGET_AMD64))
    DWORD context = CONTEXT_COMPLETE;

    // Determine if the processor supports AVX so we could
    // retrieve extended registers
    DWORD64 FeatureMask = GetEnabledXStateFeatures();
    if ((FeatureMask & XSTATE_MASK_AVX) != 0)
    {
        context = context | CONTEXT_XSTATE;
    }

    // Retrieve contextSize by passing NULL for Buffer
    DWORD contextSize = 0;
    ULONG64 xStateCompactionMask = XSTATE_MASK_LEGACY | XSTATE_MASK_AVX;
    // The initialize call should fail but return contextSize
    BOOL success = g_pfnInitializeContext2 ?
        g_pfnInitializeContext2(NULL, context, NULL, &contextSize, xStateCompactionMask) :
        InitializeContext(NULL, context, NULL, &contextSize);

    // Spec mentions that we may get a different error (it was observed on Windows7).
    // In such case the contextSize is undefined.
    if (success || GetLastError() != ERROR_INSUFFICIENT_BUFFER)
    {
        STRESS_LOG2(LF_SYNC, LL_INFO1000, "AllocateOSContextHelper: Unexpected result from InitializeContext (success: %d, error: %d).\n",
            success, GetLastError());
        return NULL;
    }

    // So now allocate a buffer of that size and call InitializeContext again
    BYTE* buffer = new (nothrow)BYTE[contextSize];
    if (buffer != NULL)
    {
        success = g_pfnInitializeContext2 ?
            g_pfnInitializeContext2(buffer, context, &pOSContext, &contextSize, xStateCompactionMask):
            InitializeContext(buffer, context, &pOSContext, &contextSize);

        if (!success)
        {
            delete[] buffer;
            buffer = NULL;
        }
    }

    if (!success)
    {
        pOSContext = NULL;
    }

    *contextBuffer = buffer;

#else
    pOSContext = new (nothrow) CONTEXT;
    pOSContext->ContextFlags = CONTEXT_COMPLETE;
    *contextBuffer = NULL;
#endif

    return pOSContext;
}

void ThreadStore::AllocateOSContext()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(HoldingThreadStore());

    if (s_pOSContext == NULL)
    {
        s_pOSContext = AllocateOSContextHelper(&s_pOSContextBuffer);
    }
}

CONTEXT *ThreadStore::GrabOSContext(BYTE** contextBuffer)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(HoldingThreadStore());

    CONTEXT *pContext = s_pOSContext;
    *contextBuffer = s_pOSContextBuffer;
    s_pOSContext = NULL;
    s_pOSContextBuffer = NULL;
    return pContext;
}

extern void WaitForEndOfShutdown();

//----------------------------------------------------------------------------
//
// Suspending threads, rendezvousing with threads that reach safe places, etc.
//
//----------------------------------------------------------------------------

// A note on SUSPENSIONS.
//
// We must not suspend a thread while it is holding the ThreadStore lock, or
// the lock on the thread.  Why?  Because we need those locks to resume the
// thread (and to perform a GC, use the debugger, spawn or kill threads, etc.)
//
// There are two types of suspension we must consider to enforce the above
// rule.  Synchronous suspensions are where we persuade the thread to suspend
// itself.  This is CommonTripThread and its cousins.  In other words, the
// thread toggles the GC mode, or it hits a hijack, or certain opcodes in the
// interpreter, etc.  In these cases, the thread can simply check whether it
// is holding these locks before it suspends itself.
//
// The other style is an asynchronous suspension.  This is where another
// thread looks to see where we are.  If we are in a fully interruptible region
// of JIT code, we will be left suspended.  In this case, the thread performing
// the suspension must hold the locks on the thread and the threadstore.  This
// ensures that we aren't suspended while we are holding these locks.
//
// Note that in the asynchronous case it's not enough to just inspect the thread
// to see if it's holding these locks.  Since the thread must be in preemptive
// mode to block to acquire these locks, and since there will be a few inst-
// ructions between acquiring the lock and noting in our state that we've
// acquired it, then there would be a window where we would seem eligible for
// suspension -- but in fact would not be.

//----------------------------------------------------------------------------

// We can't leave preemptive mode and enter cooperative mode, if a GC is
// currently in progress.  This is the situation when returning back into
// the EE from outside.  See the comments in DisablePreemptiveGC() to understand
// why we Enable GC here!
void Thread::RareDisablePreemptiveGC()
{
    BEGIN_PRESERVE_LAST_ERROR;

    CONTRACTL {
        NOTHROW;
        DISABLED(GC_TRIGGERS);  // I think this is actually wrong: prevents a p->c->p mode switch inside a NOTRIGGER region.
    }
    CONTRACTL_END;

    if (IsAtProcessExit())
    {
        goto Exit;
    }

    // This should NEVER be called if the TSNC_UnsafeSkipEnterCooperative bit is set!
    _ASSERTE(!(m_StateNC & TSNC_UnsafeSkipEnterCooperative) && "DisablePreemptiveGC called while the TSNC_UnsafeSkipEnterCooperative bit is set");

    // Holding a spin lock in preemp mode and switch to coop mode could cause other threads spinning
    // waiting for GC
    _ASSERTE ((m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

    _ASSERTE(!MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread() || IsInForbidSuspendForDebuggerRegion());

    if (!GCHeapUtilities::IsGCHeapInitialized())
    {
        goto Exit;
    }

    if (ThreadStore::HoldingThreadStore(this))
    {
        goto Exit;
    }

    // Note IsGCInProgress is also true for say Pause (anywhere SuspendEE happens) and GCThread is the
    // thread that did the Pause. While in Pause if another thread attempts Rev/Pinvoke it should get inside the following and
    // block until resume
    if ((GCHeapUtilities::IsGCInProgress() && (this != ThreadSuspend::GetSuspensionThread())) ||
        ((m_State & TS_DebugSuspendPending) && !IsInForbidSuspendForDebuggerRegion()) ||
        (m_State & TS_StackCrawlNeeded))
    {
        STRESS_LOG1(LF_SYNC, LL_INFO1000, "RareDisablePreemptiveGC: entering. Thread state = %x\n", m_State.Load());

        DWORD dwSwitchCount = 0;

        while (true)
        {
            EnablePreemptiveGC();

            // Cannot use GCX_PREEMP_NO_DTOR here because we're inside of the thread
            // PREEMP->COOP switch mechanism and GCX_PREEMP's assert's will fire.
            // Instead we use BEGIN_GCX_ASSERT_PREEMP to inform Scan of the mode
            // change here.
            BEGIN_GCX_ASSERT_PREEMP;

            // just wait until the GC is over.
            if (this != ThreadSuspend::GetSuspensionThread())
            {
#ifdef PROFILING_SUPPORTED
                // If profiler desires GC events, notify it that this thread is waiting until the GC is over
                // Do not send suspend notifications for debugger suspensions
                {
                    BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
                    if (!(m_State & TS_DebugSuspendPending))
                    {
                        (&g_profControlBlock)->RuntimeThreadSuspended((ThreadID)this);
                    }
                    END_PROFILER_CALLBACK();
                }
#endif // PROFILING_SUPPORTED

                DWORD status = GCHeapUtilities::GetGCHeap()->WaitUntilGCComplete();
                if (status != S_OK)
                {
                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Waiting for GC completion failed"));
                }

                if (!GCHeapUtilities::IsGCInProgress())
                {
                    if (HasThreadState(TS_StackCrawlNeeded))
                    {
                        ThreadStore::WaitForStackCrawlEvent();
                    }
                }

#ifdef PROFILING_SUPPORTED
                // Let the profiler know that this thread is resuming
                {
                    BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
                    (&g_profControlBlock)->RuntimeThreadResumed((ThreadID)this);
                    END_PROFILER_CALLBACK();
                }
#endif // PROFILING_SUPPORTED
            }

            END_GCX_ASSERT_PREEMP;

            // disable preemptive gc.
            InterlockedOr((LONG*)&m_fPreemptiveGCDisabled, 1);

            // The fact that we check whether 'this' is the GC thread may seem
            // strange.  After all, we determined this before entering the method.
            // However, it is possible for the current thread to become the GC
            // thread while in this loop.  This happens if you use the COM+
            // debugger to suspend this thread and then release it.
            if (! ((GCHeapUtilities::IsGCInProgress() && (this != ThreadSuspend::GetSuspensionThread())) ||
                    ((m_State & TS_DebugSuspendPending) && !IsInForbidSuspendForDebuggerRegion()) ||
                    (m_State & TS_StackCrawlNeeded)) )
            {
                break;
            }

            __SwitchToThread(0, ++dwSwitchCount);
        }
        STRESS_LOG0(LF_SYNC, LL_INFO1000, "RareDisablePreemptiveGC: leaving\n");
    }

Exit: ;
    END_PRESERVE_LAST_ERROR;
}

void Thread::HandleThreadAbort ()
{
    BEGIN_PRESERVE_LAST_ERROR;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // @TODO: we should consider treating this function as an FCALL or HCALL and use FCThrow instead of COMPlusThrow

    // Sometimes we call this without any CLR SEH in place.  An example is UMThunkStubRareDisableWorker.
    // That's okay since COMPlusThrow will eventually erect SEH around the RaiseException. It prevents
    // us from stating CONTRACT here.

    if (ReadyForAbort())
    {
        ResetThreadState ((ThreadState)(TS_Interrupted | TS_Interruptible));
        // We are going to abort.  Abort satisfies Thread.Interrupt requirement.
        InterlockedExchange (&m_UserInterrupt, 0);

        // generate either a ThreadAbort exception
        STRESS_LOG1(LF_APPDOMAIN, LL_INFO100, "Thread::HandleThreadAbort throwing abort for %x\n", GetThreadId());

        GCX_COOP_NO_DTOR();

        // Can not use holder.  GCX_COOP forces the thread back to the original state during
        // exception unwinding, which may put the thread back to cooperative mode.
        // GCX_COOP();

        if (!IsAbortInitiated() ||
            (IsRudeAbort() && !IsRudeAbortInitiated()))
        {
            PreWorkForThreadAbort();
        }

        PreparingAbortHolder paHolder;

        OBJECTREF exceptObj;

        if (IsRudeAbort())
        {
            exceptObj = CLRException::GetBestThreadAbortException();
        }
        else
        {
            EEException eeExcept(kThreadAbortException);
            exceptObj = CLRException::GetThrowableFromException(&eeExcept);
        }

        RaiseTheExceptionInternalOnly(exceptObj, FALSE);
    }

    END_PRESERVE_LAST_ERROR;
}

void Thread::PreWorkForThreadAbort()
{
    WRAPPER_NO_CONTRACT;

    SetAbortInitiated();
    // if an abort and interrupt happen at the same time (e.g. on a sleeping thread),
    // the abort is favored. But we do need to reset the interrupt bits.
    ResetThreadState((ThreadState)(TS_Interruptible | TS_Interrupted));
    ResetUserInterrupted();
}

#if defined(STRESS_HEAP) && defined(_DEBUG)

// This function is for GC stress testing.  Before we enable preemptive GC, let us do a GC
// because GC may happen while the thread is in preemptive GC mode.
void Thread::PerformPreemptiveGC()
{
    CONTRACTL {
        NOTHROW;
        DISABLED(GC_TRIGGERS);  // I think this is actually wrong: prevents a p->c->p mode switch inside a NOTRIGGER region.
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    if (IsAtProcessExit())
        return;

    if (!GCStressPolicy::IsEnabled() || !GCStress<cfg_transition>::IsEnabled())
        return;

    if (!GCHeapUtilities::IsGCHeapInitialized())
        return;

    if (!m_GCOnTransitionsOK
#ifdef ENABLE_CONTRACTS
        || RawGCNoTrigger()
#endif
        || g_fEEShutDown
        || GCHeapUtilities::IsGCInProgress(TRUE)
        || GCHeapUtilities::GetGCHeap()->GetGcCount() == 0    // Need something that works for isolated heap.
        || ThreadStore::HoldingThreadStore())
        return;

#ifdef DEBUGGING_SUPPORTED
    // Don't collect if the debugger is attach and either 1) there
    // are any threads held at unsafe places or 2) this thread is
    // under the control of the debugger's dispatch logic (as
    // evidenced by having a non-NULL filter context.)
    if ((CORDebuggerAttached() &&
        (g_pDebugInterface->ThreadsAtUnsafePlaces() ||
        (GetFilterContext() != NULL))))
        return;
#endif // DEBUGGING_SUPPORTED

    _ASSERTE(m_fPreemptiveGCDisabled.Load() == 0);     // we are in preemptive mode when we call this

    m_GCOnTransitionsOK = FALSE;
    {
        GCX_COOP();
        m_bGCStressing = TRUE;

        // BUG(github #10318) - when not using allocation contexts, the alloc lock
        // must be acquired here. Until fixed, this assert prevents random heap corruption.
        _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());
        GCHeapUtilities::GetGCHeap()->StressHeap(GetThread()->GetAllocContext());
        m_bGCStressing = FALSE;
    }
    m_GCOnTransitionsOK = TRUE;
}
#endif  // STRESS_HEAP && DEBUG

// To leave cooperative mode and enter preemptive mode, if a GC is in progress, we
// no longer care to suspend this thread.  But if we are trying to suspend the thread
// for other reasons (e.g. Thread.Suspend()), now is a good time.
//
// Note that it is possible for an N/Direct call to leave the EE without explicitly
// enabling preemptive GC.
void Thread::RareEnablePreemptiveGC()
{
    CONTRACTL {
        NOTHROW;
        DISABLED(GC_TRIGGERS); // I think this is actually wrong: prevents a p->c->p mode switch inside a NOTRIGGER region.
    }
    CONTRACTL_END;

    // @todo -  Needs a hard SO probe
    CONTRACT_VIOLATION(GCViolation|FaultViolation);

    // If we have already received our PROCESS_DETACH during shutdown, there is only one thread in the
    // process and no coordination is necessary.
    if (IsAtProcessExit())
        return;

    _ASSERTE (!m_fPreemptiveGCDisabled);

    // holding a spin lock in coop mode and transit to preemp mode will cause deadlock on GC
    _ASSERTE ((m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

    _ASSERTE(!MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread() || IsInForbidSuspendForDebuggerRegion());

#if defined(STRESS_HEAP) && defined(_DEBUG)
    if (!IsDetached())
        PerformPreemptiveGC();
#endif

    STRESS_LOG1(LF_SYNC, LL_INFO100000, "RareEnablePreemptiveGC: entering. Thread state = %x\n", m_State.Load());
    if (!ThreadStore::HoldingThreadStore(this))
    {
#ifdef FEATURE_HIJACK
        // Remove any hijacks we might have.
        UnhijackThread();
#endif // FEATURE_HIJACK

        // EnablePreemptiveGC already set us to preemptive mode before triggering the Rare path.
        // the Rare path implies that someone else is observing us (e.g. SuspendRuntime).
        // we have changed to preemptive mode, so signal that there was a suspension progress.
        ThreadSuspend::g_pGCSuspendEvent->Set();

        // for GC, the fact that we are leaving the EE means that it no longer needs to
        // suspend us.  But if we are doing a non-GC suspend, we need to block now.
        // Give the debugger precedence over user suspensions:
        while ((m_State & TS_DebugSuspendPending) && !IsInForbidSuspendForDebuggerRegion())
        {

#ifdef DEBUGGING_SUPPORTED
            // We don't notify the debugger that this thread is now suspended. We'll just
            // let the debugger's helper thread sweep and pick it up.
            // We also never take the TSL in here either.
            // Life's much simpler this way...


#endif // DEBUGGING_SUPPORTED

#ifdef LOGGING
            {
                LOG((LF_CORDB, LL_INFO1000, "[0x%x] SUSPEND: suspended while enabling gc.\n", GetThreadId()));
            }
#endif

            WaitSuspendEvents(); // sets bits, too

        }
    }
    STRESS_LOG0(LF_SYNC, LL_INFO100000, " RareEnablePreemptiveGC: leaving.\n");
}

// Called when we are passing through a safe point in CommonTripThread or
// HandleSuspensionForInterruptedThread. Do the right thing with this thread,
// which can either mean waiting for the GC to complete, or performing a
// pending suspension.
void Thread::PulseGCMode()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(this == GetThread());

    if (PreemptiveGCDisabled() && CatchAtSafePoint())
    {
        EnablePreemptiveGC();
        DisablePreemptiveGC();
    }
}

// Indicate whether threads should be trapped when returning to the EE (i.e. disabling
// preemptive GC mode)
Volatile<LONG> g_fTrapReturningThreadsLock;
void ThreadStore::TrapReturningThreads(BOOL yes)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // make sure that a thread doesn't get suspended holding g_fTrapReturningThreadsLock
    // if a suspended thread held this lock and then the suspending thread called in
    // here (which it does) the suspending thread would deadlock causing the suspension
    // as a whole to deadlock
    ForbidSuspendThreadHolder suspend;

    DWORD dwSwitchCount = 0;
    while (1 == InterlockedExchange(&g_fTrapReturningThreadsLock, 1))
    {
        // we can't forbid suspension while we are sleeping and don't hold the lock
        // this will trigger an assert on SQLCLR but is a general issue
        suspend.Release();
        __SwitchToThread(0, ++dwSwitchCount);
        suspend.Acquire();
    }

    if (yes)
    {
#ifdef _DEBUG
        CounterHolder trtHolder(&g_trtChgInFlight);
        InterlockedIncrement(&g_trtChgStamp);
#endif

        GCHeapUtilities::GetGCHeap()->SetSuspensionPending(true);
        InterlockedIncrement ((LONG *)&g_TrapReturningThreads);
        _ASSERTE(g_TrapReturningThreads > 0);

#ifdef _DEBUG
        trtHolder.Release();
#endif
    }
    else
    {
        InterlockedDecrement ((LONG *)&g_TrapReturningThreads);
        GCHeapUtilities::GetGCHeap()->SetSuspensionPending(false);
        _ASSERTE(g_TrapReturningThreads >= 0);
    }

    g_fTrapReturningThreadsLock = 0;
}

#ifdef FEATURE_HIJACK

void RedirectedThreadFrame::ExceptionUnwind()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    STRESS_LOG1(LF_SYNC, LL_INFO1000, "In RedirectedThreadFrame::ExceptionUnwind pFrame = %p\n", this);

    Thread* pThread = GetThread();

    // Allow future use to avoid repeatedly new'ing
    if (pThread->UnmarkRedirectContextInUse(m_Regs))
    {
        m_Regs = NULL;
    }
}

#ifndef TARGET_UNIX

#ifdef TARGET_X86

//****************************************************************************************
// This will check who caused the exception.  If it was caused by the redirect function,
// the reason is to resume the thread back at the point it was redirected in the first
// place.  If the exception was not caused by the function, then it was caused by the call
// out to the I[GC|Debugger]ThreadControl client and we need to determine if it's an
// exception that we can just eat and let the runtime resume the thread, or if it's an
// uncatchable exception that we need to pass on to the runtime.
//
int RedirectedHandledJITCaseExceptionFilter(
    PEXCEPTION_POINTERS pExcepPtrs,     // Exception data
    RedirectedThreadFrame *pFrame,      // Frame on stack
    CONTEXT *pCtx,                      // Saved context
    DWORD dwLastError)                  // saved last error
{
    // !!! Do not use a non-static contract here.
    // !!! Contract may insert an exception handling record.
    // !!! This function assumes that GetCurrentSEHRecord() returns the exception record set up in
    // !!! Thread::RestoreContextSimulated
    //
    // !!! Do not use an object with dtor, since it injects a fs:0 entry.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    if (pExcepPtrs->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Get the thread handle
    Thread *pThread = GetThread();
    STRESS_LOG1(LF_SYNC, LL_INFO100, "In RedirectedHandledJITCaseExceptionFilter pFrame = %p\n", pFrame);
    _ASSERTE(pExcepPtrs->ExceptionRecord->ExceptionCode == EXCEPTION_HIJACK);

    // Unlink the frame in preparation for resuming in managed code
    pFrame->Pop();

    // Copy everything in the saved context record into the EH context.
    // Historically the EH context has enough space for every enabled context feature.
    // That may not hold for the future features beyond AVX, but this codepath is
    // supposed to be used only on OSes that do not have RtlRestoreContext.
    CONTEXT* pTarget = pExcepPtrs->ContextRecord;
    if (!CopyContext(pTarget, pCtx->ContextFlags, pCtx))
    {
        STRESS_LOG1(LF_SYNC, LL_ERROR, "ERROR: Could not set context record, lastError = 0x%x\n", GetLastError());
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }

    DWORD espValue = pCtx->Esp;

    // Allow future use to avoid repeatedly new'ing
    pThread->UnmarkRedirectContextInUse(pCtx);

    /////////////////////////////////////////////////////////////////////////////
    // NOTE: Ugly, ugly workaround.
    // We need to resume the thread into the managed code where it was redirected,
    // and the corresponding ESP is below the current one.  But C++ expects that
    // on an EXCEPTION_CONTINUE_EXECUTION that the ESP will be above where it has
    // installed the SEH handler.  To solve this, we need to remove all handlers
    // that reside above the resumed ESP, but we must leave the OS-installed
    // handler at the top, so we grab the top SEH handler, call
    // PopSEHRecords which will remove all SEH handlers above the target ESP and
    // then link the OS handler back in with SetCurrentSEHRecord.

    // Get the special OS handler and save it until PopSEHRecords is done
    EXCEPTION_REGISTRATION_RECORD *pCurSEH = GetCurrentSEHRecord();

    // Unlink all records above the target resume ESP
    PopSEHRecords((LPVOID)(size_t)espValue);

    // Link the special OS handler back in to the top
    pCurSEH->Next = GetCurrentSEHRecord();

    // Register the special OS handler as the top handler with the OS
    SetCurrentSEHRecord(pCurSEH);

    // restore last error
    SetLastError(dwLastError);

    // Resume execution at point where thread was originally redirected
    return (EXCEPTION_CONTINUE_EXECUTION);
}
#endif // TARGET_X86

void NotifyHostOnGCSuspension()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

}

// This function is called from the assembly functions used to redirect a thread.  It must not cause
// an exception (except SO).
extern "C" PCONTEXT __stdcall GetCurrentSavedRedirectContext()
{
    LIMITED_METHOD_CONTRACT;

    PCONTEXT pContext;

    BEGIN_PRESERVE_LAST_ERROR;
    pContext = GetThread()->GetSavedRedirectContext();
    END_PRESERVE_LAST_ERROR;

    return pContext;
}

#ifdef TARGET_X86

void Thread::RestoreContextSimulated(Thread* pThread, CONTEXT* pCtx, void* pFrame, DWORD dwLastError)
{
    pThread->HandleThreadAbort();        // Might throw an exception.

    // A counter to avoid a nasty case where an
    // up-stack filter throws another exception
    // causing our filter to be run again for
    // some unrelated exception.
    int filter_count = 0;

    __try
    {
        // Save the instruction pointer where we redirected last.  This does not race with the check
        // against this variable in HandledJitCase because the GC will not attempt to redirect the
        // thread until the instruction pointer of this thread is back in managed code.
        pThread->m_LastRedirectIP = GetIP(pCtx);
        pThread->m_SpinCount = 0;

        RaiseException(EXCEPTION_HIJACK, 0, 0, NULL);
    }
    __except (++filter_count == 1
            ? RedirectedHandledJITCaseExceptionFilter(GetExceptionInformation(), (RedirectedThreadFrame*)pFrame, pCtx, dwLastError)
            : EXCEPTION_CONTINUE_SEARCH)
    {
        _ASSERTE(!"Reached body of __except in Thread::RedirectedHandledJITCase");
    }
}

#endif // TARGET_X86

void __stdcall Thread::RedirectedHandledJITCase(RedirectReason reason)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // We must preserve this in case we've interrupted an IL pinvoke stub before it
    // was able to save the error.
    DWORD dwLastError = GetLastError(); // BEGIN_PRESERVE_LAST_ERROR

    Thread *pThread = GetThread();

    // Get the saved context
    CONTEXT *pCtx = pThread->GetSavedRedirectContext();
    _ASSERTE(pCtx);

    INDEBUG(Thread::ObjectRefFlush(pThread));

    // Create a frame on the stack
    FrameWithCookie<RedirectedThreadFrame> frame(pCtx);

    STRESS_LOG5(LF_SYNC, LL_INFO1000, "In RedirectedHandledJITcase reason 0x%x pFrame = %p pc = %p sp = %p fp = %p", reason, &frame, GetIP(pCtx), GetSP(pCtx), GetFP(pCtx));

    // Make sure this thread doesn't reuse the context memory.
    pThread->MarkRedirectContextInUse(pCtx);

    // Link in the frame
    frame.Push();

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
    if (Thread::UseRedirectForGcStress() && (reason == RedirectReason_GCStress))
    {
        _ASSERTE(pThread->PreemptiveGCDisabledOther());
        DoGcStress(frame.GetContext(), NULL);
    }
    else
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS
    {
        _ASSERTE(reason == RedirectReason_GCSuspension ||
                    reason == RedirectReason_DebugSuspension ||
                    reason == RedirectReason_UserSuspension);

        // Actual self-suspension.
        // Leave and reenter COOP mode to be trapped on the way back.
        GCX_PREEMP_NO_DTOR();
        GCX_PREEMP_NO_DTOR_END();
    }

    // Once we get here the suspension is over!
    // We will restore the state as it was at the point of redirection
    // and continue normal execution.

#ifdef TARGET_X86
    if (!g_pfnRtlRestoreContext)
    {
        RestoreContextSimulated(pThread, pCtx, &frame, dwLastError);

        // we never return to the caller.
        __UNREACHABLE();
    }
#endif // TARGET_X86

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
    //
    // If GCStress interrupts an IL stub or inlined p/invoke while it's running in preemptive mode, it switches the mode to
    // cooperative - but we will resume to preemptive below.  We should not trigger an abort in that case, as it will fail
    // due to the GC mode.
    //
    if (!Thread::UseRedirectForGcStress() || !pThread->m_fPreemptiveGCDisabledForGCStress)
#endif
    {

        UINT_PTR uAbortAddr;
        UINT_PTR uResumePC = (UINT_PTR)GetIP(pCtx);
        CopyOSContext(pThread->m_OSContext, pCtx);
        uAbortAddr = (UINT_PTR)COMPlusCheckForAbort();
        if (uAbortAddr)
        {
            LOG((LF_EH, LL_INFO100, "thread abort in progress, resuming thread under control... (handled jit case)\n"));

            CONSISTENCY_CHECK(CheckPointer(pCtx));

            STRESS_LOG1(LF_EH, LL_INFO10, "resume under control: ip: %p (handled jit case)\n", uResumePC);

            SetIP(pThread->m_OSContext, uResumePC);

#if defined(TARGET_ARM)
            // Save the original resume PC in Lr
            pCtx->Lr = uResumePC;

            // Since we have set a new IP, we have to clear conditional execution flags too.
            ClearITState(pThread->m_OSContext);
#endif // TARGET_ARM

            SetIP(pCtx, uAbortAddr);
        }
    }

    // Unlink the frame in preparation for resuming in managed code
    frame.Pop();

    // Allow future use of the context
    pThread->UnmarkRedirectContextInUse(pCtx);

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
    if (Thread::UseRedirectForGcStress() && pThread->m_fPreemptiveGCDisabledForGCStress)
    {
        pThread->EnablePreemptiveGC();
        pThread->m_fPreemptiveGCDisabledForGCStress = false;
    }
#endif

    LOG((LF_SYNC, LL_INFO1000, "Resuming execution with RtlRestoreContext\n"));
    SetLastError(dwLastError); // END_PRESERVE_LAST_ERROR

#ifdef TARGET_X86
    g_pfnRtlRestoreContext(pCtx, NULL);
#else
    RtlRestoreContext(pCtx, NULL);
#endif

    // we never return to the caller.
    __UNREACHABLE();
}

//****************************************************************************************
// This helper is called when a thread suspended in managed code at a sequence point while
// suspending the runtime and there is a client interested in re-assigning the thread to
// do interesting work while the runtime is suspended.  This will call into the client
// notifying it that the thread will be suspended for a runtime suspension.
//
void __stdcall Thread::RedirectedHandledJITCaseForDbgThreadControl()
{
    WRAPPER_NO_CONTRACT;
    RedirectedHandledJITCase(RedirectReason_DebugSuspension);
}

//****************************************************************************************
// This helper is called when a thread suspended in managed code at a sequence point when
// suspending the runtime.
//
// We do this because the obvious code sequence:
//
//      SuspendThread(t1);
//      GetContext(t1, &ctx);
//      ctx.Ecx = <some new value>;
//      SetContext(t1, &ctx);
//      ResumeThread(t1);
//
// simply does not work due to a nasty race with exception handling in the OS.  If the
// thread that is suspended has just faulted, then the update can disappear without ever
// modifying the real thread ... and there is no way to tell.
//
// Updating the EIP may not work ... but when it doesn't, we're ok ... an exception ends
// up getting dispatched anyway.
//
// If the host is interested in getting control, then we give control to the host.  If the
// host is not interested in getting control, then we call out to the host.  After that,
// we raise an exception and will end up waiting for the GC to finish inside the filter.
//
void __stdcall Thread::RedirectedHandledJITCaseForGCThreadControl()
{
    WRAPPER_NO_CONTRACT;
    RedirectedHandledJITCase(RedirectReason_GCSuspension);
}

//***********************
// Like the above, but called for a UserSuspend.
//
void __stdcall Thread::RedirectedHandledJITCaseForUserSuspend()
{
    WRAPPER_NO_CONTRACT;
    RedirectedHandledJITCase(RedirectReason_UserSuspension);
}

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER

//***********************
// Like the above, but called for GC stress.
//
void __stdcall Thread::RedirectedHandledJITCaseForGCStress()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(Thread::UseRedirectForGcStress());
    RedirectedHandledJITCase(RedirectReason_GCStress);
}

#endif // HAVE_GCCOVER && _DEBUG && USE_REDIRECT_FOR_GCSTRESS

//****************************************************************************************
// This will take a thread that's been suspended in managed code at a sequence point and
// will Redirect the thread. It will save all register information, build a frame on the
// thread's stack, put a pointer to the frame at the top of the stack and set the IP of
// the thread to pTgt.  pTgt is then responsible for unlinking the thread,
//
// NOTE: Cannot play with a suspended thread's stack memory, since the OS will use the
// top of the stack to store information.  The thread must be resumed and play with it's
// own stack.
//

BOOL Thread::RedirectThreadAtHandledJITCase(PFN_REDIRECTTARGET pTgt)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(HandledJITCase());
    _ASSERTE(GetThreadNULLOk() != this);
    _ASSERTE(ThreadStore::HoldingThreadStore());

    ////////////////////////////////////////////////////////////////
    // Acquire a context structure to save the thread state into

    // All callers, including suspension, operate while holding the ThreadStore
    //   lock.  This means that we can pre-allocate a context structure
    //   globally in the ThreadStore and use it in this function.

    // Check whether we have a SavedRedirectContext we can reuse:
    CONTEXT *pCtx = GetSavedRedirectContext();

    // If we've never assigned a context for this thread, do so now
    if (!pCtx)
    {
        pCtx = m_pSavedRedirectContext = ThreadStore::GrabOSContext(&m_pOSContextBuffer);
    }

    // We may not have a preallocated context. Could be short on memory when we tried to preallocate.
    // We cannot allocate here since we have a thread stopped in a random place, possibly holding locks
    // that we would need while allocating.
    // Other ways and attempts at suspending may yet succeed, but this redirection cannot continue.
    if (!pCtx)
        return (FALSE);

    //////////////////////////////////////
    // Get and save the thread's context
    BOOL bRes = true;

    // Always get complete context, pCtx->ContextFlags are set during Initialization

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // Scenarios like GC stress may indirectly disable XState features in the pCtx
    // depending on the state at the time of GC stress interrupt.
    //
    // Make sure that AVX feature mask is set, if supported.
    //
    // This should not normally fail.
    // The system silently ignores any feature specified in the FeatureMask
    // which is not enabled on the processor.
    SetXStateFeaturesMask(pCtx, XSTATE_MASK_AVX);
#endif //defined(TARGET_X86) || defined(TARGET_AMD64)

    // Make sure we specify CONTEXT_EXCEPTION_REQUEST to detect "trap frame reporting".
    pCtx->ContextFlags |= CONTEXT_EXCEPTION_REQUEST;
    bRes &= EEGetThreadContext(this, pCtx);
    _ASSERTE(bRes && "Failed to GetThreadContext in RedirectThreadAtHandledJITCase - aborting redirect.");

    if (!bRes)
        return (FALSE);

    if (!IsContextSafeToRedirect(pCtx))
        return (FALSE);

    ////////////////////////////////////////////////////
    // Now redirect the thread to the helper function

    // Temporarily set the IP of the context to the target for SetThreadContext
    PCODE dwOrigEip = GetIP(pCtx);
#ifdef TARGET_ARM
    // Redirection can be required when in IT Block.
    // In that case must reset the IT state before redirection.
    DWORD dwOrigCpsr = pCtx->Cpsr;
    ClearITState(pCtx);
#endif
    _ASSERTE(ExecutionManager::IsManagedCode(dwOrigEip));
    SetIP(pCtx, (PCODE)pTgt);


    STRESS_LOG4(LF_SYNC, LL_INFO10000, "Redirecting thread %p(tid=%x) from address 0x%p to address 0x%p\n",
        this, this->GetThreadId(), dwOrigEip, pTgt);

    bRes = EESetThreadContext(this, pCtx);
    if (!bRes)
    {
#ifdef _DEBUG
        // In some rare cases the stack pointer may be outside the stack limits.
        // SetThreadContext would fail assuming that we are trying to bypass CFG.
        //
        // NB: the check here is slightly more strict than what OS requires,
        //     but it is simple and uses only documented parts of TEB
        auto pTeb = this->GetTEB();
        void* stackPointer = (void*)GetSP(pCtx);
        if ((stackPointer < pTeb->StackLimit) || (stackPointer > pTeb->StackBase))
        {
            return (FALSE);
        }

        _ASSERTE(!"Failed to SetThreadContext in RedirectThreadAtHandledJITCase - aborting redirect.");
#endif

        return FALSE;
    }

    // Restore original IP
    SetIP(pCtx, dwOrigEip);
#ifdef TARGET_ARM
    // restore IT State in the context
    pCtx->Cpsr = dwOrigCpsr;
#endif


    //////////////////////////////////////////////////
    // Indicate whether or not the redirect succeeded

    return (bRes);
}

BOOL Thread::CheckForAndDoRedirect(PFN_REDIRECTTARGET pRedirectTarget)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(this != GetThreadNULLOk());
    _ASSERTE(PreemptiveGCDisabledOther());
    _ASSERTE(IsAddrOfRedirectFunc(pRedirectTarget));

    BOOL fRes = FALSE;
    fRes = RedirectThreadAtHandledJITCase(pRedirectTarget);
    LOG((LF_GC, LL_INFO1000, "RedirectThreadAtHandledJITCase %s.\n", fRes ? "SUCCEEDED" : "FAILED"));

    return (fRes);
}

BOOL Thread::RedirectCurrentThreadAtHandledJITCase(PFN_REDIRECTTARGET pTgt, CONTEXT *pCurrentThreadCtx)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(GetThread() == this);
    _ASSERTE(PreemptiveGCDisabledOther());
    _ASSERTE(IsAddrOfRedirectFunc(pTgt));
    _ASSERTE(pCurrentThreadCtx);
    _ASSERTE((pCurrentThreadCtx->ContextFlags & CONTEXT_FULL) == CONTEXT_FULL);
    _ASSERTE(ExecutionManager::IsManagedCode(GetIP(pCurrentThreadCtx)));

    ////////////////////////////////////////////////////////////////
    // Allocate a context structure to save the thread state into

    // Check to see if we've already got memory allocated for this purpose.
    CONTEXT *pCtx = GetSavedRedirectContext();

    // If we've never assigned a context for this thread, do so now
    if (!pCtx)
    {
        pCtx = m_pSavedRedirectContext = AllocateOSContextHelper(&m_pOSContextBuffer);
        _ASSERTE(GetSavedRedirectContext() != NULL);
    }

    //////////////////////////////////////
    // Get and save the thread's context
    BOOL success = TRUE;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // This method is called for GC stress interrupts in managed code.
    // The current context may have various XState features, depending on what is used/dirty,
    // but only AVX feature may contain live data. (that could change with new features in JIT)
    // Besides pCtx may not have space to store other features.
    // So we will mask out everything but AVX.
    DWORD64 srcFeatures = 0;
    success = GetXStateFeaturesMask(pCurrentThreadCtx, &srcFeatures);
    _ASSERTE(success);
    if (!success)
        return FALSE;

    // Get may return 0 if no XState is set, which Set would not accept.
    if (srcFeatures != 0)
    {
        success = SetXStateFeaturesMask(pCurrentThreadCtx, srcFeatures & XSTATE_MASK_AVX);
        _ASSERTE(success);
        if (!success)
            return FALSE;
    }

#endif //defined(TARGET_X86) || defined(TARGET_AMD64)

    success = CopyContext(pCtx, pCtx->ContextFlags, pCurrentThreadCtx);
    _ASSERTE(success);
    if (!success)
        return FALSE;

    ////////////////////////////////////////////////////
    // Now redirect the thread to the helper function

    SetIP(pCurrentThreadCtx, (PCODE)pTgt);

#ifdef TARGET_ARM
    // Redirection can be required when in IT Block
    // Clear the IT State before redirecting
    ClearITState(pCurrentThreadCtx);
#endif

    //////////////////////////////////////////////////
    // Indicate whether or not the redirect succeeded

    return TRUE;
}

//************************************************************************
// Exception handling needs to special case the redirection. So provide
// a helper to identify redirection targets and keep the exception
// checks in sync with the redirection here.
// See CPFH_AdjustContextForThreadSuspensionRace for details.
BOOL Thread::IsAddrOfRedirectFunc(void * pFuncAddr)
{
    WRAPPER_NO_CONTRACT;

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
    if (Thread::UseRedirectForGcStress() && (pFuncAddr == GetRedirectHandlerForGCStress()))
        return TRUE;
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS

    if ((pFuncAddr == GetRedirectHandlerForGCThreadControl()) ||
        (pFuncAddr == GetRedirectHandlerForDbgThreadControl()) ||
        (pFuncAddr == GetRedirectHandlerForUserSuspend()))
    {
        return TRUE;
    }

#ifdef FEATURE_SPECIAL_USER_MODE_APC
    if (pFuncAddr == GetRedirectHandlerForApcActivation())
    {
        return TRUE;
    }
#endif

    return FALSE;
}

//************************************************************************
// Redirect thread at a GC suspension.
BOOL Thread::CheckForAndDoRedirectForGC()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_GC, LL_INFO1000, "Redirecting thread %08x for GCThreadSuspension", GetThreadId()));
    return CheckForAndDoRedirect(GetRedirectHandlerForGCThreadControl());
}

//************************************************************************
// Redirect thread at a debug suspension.
BOOL Thread::CheckForAndDoRedirectForDbg()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "Redirecting thread %08x for DebugSuspension", GetThreadId()));
    return CheckForAndDoRedirect(GetRedirectHandlerForDbgThreadControl());
}

//*************************************************************************
// Redirect thread at a user suspend.
BOOL Thread::CheckForAndDoRedirectForUserSuspend()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_SYNC, LL_INFO1000, "Redirecting thread %08x for UserSuspension", GetThreadId()));
    return CheckForAndDoRedirect(GetRedirectHandlerForUserSuspend());
}

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
//*************************************************************************
// Redirect thread at a GC stress point.
BOOL Thread::CheckForAndDoRedirectForGCStress (CONTEXT *pCurrentThreadCtx)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(Thread::UseRedirectForGcStress());

    LOG((LF_CORDB, LL_INFO1000, "Redirecting thread %08x for GCStress", GetThreadId()));

    m_fPreemptiveGCDisabledForGCStress = !PreemptiveGCDisabled();
    GCX_COOP_NO_DTOR();

    BOOL fSuccess = RedirectCurrentThreadAtHandledJITCase(GetRedirectHandlerForGCStress(), pCurrentThreadCtx);

    if (!fSuccess)
    {
        GCX_COOP_NO_DTOR_END();
        m_fPreemptiveGCDisabledForGCStress = false;
    }

    return fSuccess;
}
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS

#endif // !TARGET_UNIX
#endif // FEATURE_HIJACK


#ifdef PROFILING_SUPPORTED
// Simple helper to convert the GC's SUSPEND_REASON enum to the profiling API's public
// COR_PRF_SUSPEND_REASON enum.  Used in code:Thread::SuspendRuntime to help with
// sending the suspension event to the profiler.
COR_PRF_SUSPEND_REASON GCSuspendReasonToProfSuspendReason(ThreadSuspend::SUSPEND_REASON gcReason)
{
    LIMITED_METHOD_CONTRACT;

    switch(gcReason)
    {
    default:
        return COR_PRF_SUSPEND_OTHER;
    case ThreadSuspend::SUSPEND_FOR_GC:
        return COR_PRF_SUSPEND_FOR_GC;
    case ThreadSuspend::SUSPEND_FOR_APPDOMAIN_SHUTDOWN:
        return COR_PRF_SUSPEND_FOR_APPDOMAIN_SHUTDOWN;
    case ThreadSuspend::SUSPEND_FOR_REJIT:
        return COR_PRF_SUSPEND_FOR_REJIT;
    case ThreadSuspend::SUSPEND_FOR_SHUTDOWN:
        return COR_PRF_SUSPEND_FOR_SHUTDOWN;
    case ThreadSuspend::SUSPEND_FOR_DEBUGGER:
        return COR_PRF_SUSPEND_FOR_INPROC_DEBUGGER;
    case ThreadSuspend::SUSPEND_FOR_GC_PREP:
        return COR_PRF_SUSPEND_FOR_GC_PREP;
    case ThreadSuspend::SUSPEND_FOR_PROFILER:
        return COR_PRF_SUSPEND_FOR_PROFILER;
    }
}
#endif // PROFILING_SUPPORTED

//************************************************************************************
//
// SuspendRuntime is responsible for ensuring that all managed threads reach a
// "safe point."  It returns when all threads are known to be in "preemptive" mode.
// This is *only* called by ThreadSuspend::SuspendEE; these two methods should really
// be refactored into a separate "managed execution lock."
//
// Note that we use this method for more than just GC suspension.  We also suspend
// for debugging, etc.
//
// The basic algorithm is this:
//
//    while there are threads in cooperative mode:
//        for each thread in cooprative mode:
//           suspend the native thread.
//           if it's still in cooperative mode, and it's running JIT'd code:
//               Redirect/hijack the thread
//
// Redirection vs. Hijacking:
//
// JIT'd code does not generally poll to see if a GC wants to run.  Instead, the JIT
// records "GC info" describing where the "safe points" are in the code.  While we
// have a native thread suspended in JIT'd code, we can see which instruction it
// is currently executing.  If that instruction is a safe point, then the GC may proceed.
// Returning from a managed method is *always* a safe point, so if the thread is not
// currently at a safe point we can "hijack" its return address.  Once that it done,
// if/when the method tried to return the thread will be sent to a hijack routine
// that will leave cooperative mode and wait for the GC to complete.
//
// If the thread is already at a safe point, you might think we could simply leave it
// suspended and proceed with the GC.  In principle, this should be what we do.
// However, various historical OS bugs prevent this from working.  The problem is that
// we are not guaranteed to capture an accurate CONTEXT (register state) for a suspended
// thread.  So instead, we "redirect" the thread, by overwriting its instruction pointer.
// We then resume the thread, and it immediately starts executing our "redirect" routine,
// which leaves cooperative mode and waits for the GC to complete.
//
// See code:Thread#SuspendingTheRuntime for more
void ThreadSuspend::SuspendRuntime(ThreadSuspend::SUSPEND_REASON reason)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk())
        {
            GC_TRIGGERS;            // CLREvent::Wait is GC_TRIGGERS
        }
        else
        {
            DISABLED(GC_TRIGGERS);
        }
    }
    CONTRACTL_END;

    // This thread
    Thread  *pCurThread = GetThreadNULLOk();

    // Caller is expected to be holding the ThreadStore lock.  Also, caller must
    // have set GcInProgress before coming here, or things will break;
    _ASSERTE(ThreadStore::HoldingThreadStore() || IsAtProcessExit());
    _ASSERTE(GCHeapUtilities::IsGCInProgress() );

    STRESS_LOG1(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime(reason=0x%x)\n", reason);


#ifdef PROFILING_SUPPORTED
    // If the profiler desires information about GCs, then let it know that one
    // is starting.
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
        _ASSERTE(reason != ThreadSuspend::SUSPEND_FOR_DEBUGGER);
        _ASSERTE(reason != ThreadSuspend::SUSPEND_FOR_DEBUGGER_SWEEP);

        {
            (&g_profControlBlock)->RuntimeSuspendStarted(
                GCSuspendReasonToProfSuspendReason(reason));
        }
        if (pCurThread)
        {
            // Notify the profiler that the thread that is actually doing the GC is 'suspended',
            // meaning that it is doing stuff other than run the managed code it was before the
            // GC started.
            (&g_profControlBlock)->RuntimeThreadSuspended((ThreadID)pCurThread);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    //
    // If this thread is running at low priority, boost its priority.  We remember the old
    // priority so that we can restore it in ResumeRuntime.
    //
    if (pCurThread)     // concurrent GC occurs on threads we don't know about
    {
        _ASSERTE(pCurThread->m_Priority == INVALID_THREAD_PRIORITY);
        int priority = pCurThread->GetThreadPriority();
        if (priority < THREAD_PRIORITY_NORMAL)
        {
            pCurThread->m_Priority = priority;
            pCurThread->SetThreadPriority(THREAD_PRIORITY_NORMAL);
        }
    }

    // From this point until the end of the function, consider all active thread
    // suspension to be in progress.  This is mainly to give the profiler API a hint
    // that trying to suspend a thread (in order to walk its stack) could delay the
    // overall EE suspension.  So the profiler API would early-abort the stackwalk
    // in such a case.
    SuspendRuntimeInProgressHolder hldSuspendRuntimeInProgress;

    // Flush the store buffers on all CPUs, to ensure two things:
    // - we get a reliable reading of the threads' m_fPreemptiveGCDisabled state
    // - other threads see that g_TrapReturningThreads is set
    // See VSW 475315 and 488918 for details.
    ::FlushProcessWriteBuffers();

    //
    // Make a pass through all threads.  We do a couple of things here:
    // 1) we count the number of threads that are observed to be in cooperative mode.
    // 2) for threads currently running managed code, we try to redirect/jihack them.

    // counts of cooperative threads
    int previousCount = 0;
    int countThreads = 0;

    // we will iterate over threads and check which are left in coop mode
    // while checking, we also will try suspending and hijacking
    // unless we have just done that, then we can observeOnly and see if situation improves
    // we do not on uniprocessor though (spin-checking is pointless on uniprocessor)
    bool observeOnly = false;

    _ASSERTE(!pCurThread || !pCurThread->HasThreadState(Thread::TS_GCSuspendFlags));
#ifdef _DEBUG
    DWORD dbgStartTimeout = GetTickCount();
#endif

    while (true)
    {
        Thread* thread = NULL;
        while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
        {
            if (thread == pCurThread)
                continue;

            // on the first iteration check m_fPreemptiveGCDisabled unconditionally and
            // mark interesting threads as TS_GCSuspendPending
            if (previousCount == 0)
            {
                STRESS_LOG3(LF_SYNC, LL_INFO10000, "    Inspecting thread 0x%x ID 0x%x coop mode = %d\n",
                    thread, thread->GetThreadId(), thread->m_fPreemptiveGCDisabled.LoadWithoutBarrier());


                // ::FlushProcessWriteBuffers above guarantees that the state that we see here
                // is after the trap flag is visible to the other thread.
                //
                // In other words: any threads seen in preemptive mode are no longer interesting to us.
                // if they try switch to cooperative, they would see the flag set.
#ifdef FEATURE_PERFTRACING
                // Mark that the thread is currently in managed code.
                thread->SaveGCModeOnSuspension();
#endif // FEATURE_PERFTRACING
                if (!thread->m_fPreemptiveGCDisabled.LoadWithoutBarrier())
                {
                    _ASSERTE(!thread->HasThreadState(Thread::TS_GCSuspendFlags));
                    continue;
                }
                else
                {
                    countThreads++;
                    thread->SetThreadState(Thread::TS_GCSuspendPending);
                }
            }

            if (!thread->HasThreadStateOpportunistic(Thread::TS_GCSuspendPending))
            {
                continue;
            }

            if (!thread->m_fPreemptiveGCDisabled.LoadWithoutBarrier())
            {
                STRESS_LOG1(LF_SYNC, LL_INFO1000, "    Thread %x went preemptive it is at a GC safe point\n", thread);
                countThreads--;
                thread->ResetThreadState(Thread::TS_GCSuspendFlags);
                continue;
            }

            if (observeOnly)
            {
                continue;
            }

            if (thread->IsGCSpecial())
            {
                // GC threads can not be forced to run preemptively, so we will not try.
                continue;
            }

            // this is an interesting thread in cooperative mode, let's guide it to preemptive

            if (!Thread::UseContextBasedThreadRedirection())
            {
                // On platforms that do not support safe thread suspension, we do one of the following:
                //
                //     - If we're on a Unix platform where hijacking is enabled, we attempt
                //       to inject an activation which will try to redirect or hijack the
                //       thread to get it to a safe point.
                //
                //     - Similarly to above, if we're on a Windows platform where the special
                //       user-mode APC is available, that is used if redirection is necessary.
                //
                //     - Otherwise, we rely on the GCPOLL mechanism enabled by
                //       TrapReturningThreads.

#ifdef FEATURE_THREAD_ACTIVATION
                bool success = thread->InjectActivation(Thread::ActivationReason::SuspendForGC);
                if (!success)
                {
                    STRESS_LOG1(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime() -   Failed to inject an activation for thread %p.\n", thread);
                }
#endif // FEATURE_THREAD_ACTIVATION

                continue;
            }

#ifndef DISABLE_THREADSUSPEND

            if (thread->HasThreadStateOpportunistic(Thread::TS_GCSuspendRedirected))
            {
                // We have seen this thead before and have redirected it.
                // No point in suspending it again. It will not run hijackable code until it parks itself.
                continue;
            }

#ifdef TIME_SUSPEND
            DWORD startSuspend = g_SuspendStatistics.GetTime();
#endif

            //
            // Suspend the native thread.
            //

            // We can not allocate memory after we suspend a thread.
            // Otherwise, we may deadlock the process, because the thread we just suspended
            // might hold locks we would need to acquire while allocating.
            ThreadStore::AllocateOSContext();
            Thread::SuspendThreadResult str = thread->SuspendThread(/*fOneTryOnly*/ TRUE);

            // We should just always build with this TIME_SUSPEND stuff, and report the results via ETW.
#ifdef TIME_SUSPEND
            g_SuspendStatistics.osSuspend.Accumulate(
                    SuspendStatistics::GetElapsed(startSuspend,
                                                    g_SuspendStatistics.GetTime()));

            if (str == Thread::STR_Success)
                g_SuspendStatistics.cntOSSuspendResume++;
            else
                g_SuspendStatistics.cntFailedSuspends++;
#endif

            switch (str)
            {
            case Thread::STR_Success:
                // let's check the state of this one.
                break;

            case Thread::STR_Forbidden:
                STRESS_LOG1(LF_SYNC, LL_INFO1000, "    Suspending thread 0x%x forbidden\n", thread);
                continue;

            case Thread::STR_NoStressLog:
                STRESS_LOG2(LF_SYNC, LL_ERROR, "    ERROR: Could not suspend thread 0x%x, result = %d\n", thread, str);
                continue;

            case Thread::STR_UnstartedOrDead:
            case Thread::STR_Failure:
                 STRESS_LOG3(LF_SYNC, LL_ERROR, "    ERROR: Could not suspend thread 0x%x, result = %d, lastError = 0x%x\n", thread, str, GetLastError());
                 continue;
            }

            // the thread is suspended here, we can hijack, if platform supports.

            if (!thread->m_fPreemptiveGCDisabled.LoadWithoutBarrier())
            {
                // actually, we are done with this one
                STRESS_LOG1(LF_SYNC, LL_INFO1000, "    Thread %x went preemptive while suspending it is at a GC safe point\n", thread);
                countThreads--;
                thread->ResetThreadState(Thread::TS_GCSuspendFlags);
                thread->ResumeThread();
                continue;
            }

            // We now know for sure that the thread is still in cooperative mode.  If it's in JIT'd code, here
            // is where we try to hijack/redirect the thread.  If it's in VM code, we have to just let the VM
            // finish what it's doing.

#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
            {
                Thread::WorkingOnThreadContextHolder workingOnThreadContext(thread);

                // Note that thread->HandledJITCase is not a simple predicate - it actually will hijack the thread if that's possible.
                // So HandledJITCase can do one of these:
                //   - Return TRUE, in which case it's our responsibility to redirect the thread
                //   - Return FALSE after hijacking the thread - we shouldn't try to redirect
                //   - Return FALSE but not hijack the thread - there's nothing we can do either
                if (workingOnThreadContext.Acquired() && thread->HandledJITCase())
                {
                    // Thread is in cooperative state and stopped in interruptible code.
                    // Redirect thread so we can capture a good thread context
                    // (GetThreadContext is not sufficient, due to an OS bug).
                    if (!thread->CheckForAndDoRedirectForGC())
                    {
#ifdef TIME_SUSPEND
                        g_SuspendStatistics.cntFailedRedirections++;
#endif
                        STRESS_LOG1(LF_SYNC, LL_INFO1000, "Failed to CheckForAndDoRedirectForGC(). Thread %p\n", thread);
                    }
                    else
                    {
#ifdef TIME_SUSPEND
                        g_SuspendStatistics.cntRedirections++;
#endif
                        thread->SetThreadState(Thread::TS_GCSuspendRedirected);
                        STRESS_LOG1(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime() -   Thread %p redirected().\n", thread);
                    }
                }
            }
#endif // FEATURE_HIJACK && !TARGET_UNIX

            thread->ResumeThread();
            STRESS_LOG1(LF_SYNC, LL_INFO1000, "    Thread 0x%x is in cooperative needs to rendezvous\n", thread);
#endif // !DISABLE_THREADSUSPEND
        }

        if (countThreads == 0)
        {
            // SUCCESS!!
            break;
        }

        bool hasProgress = previousCount != countThreads;
        previousCount = countThreads;

        // If we have just updated hijacks/redirects, then do a pass while only observing.
        // Repeat observing only as long as we see progress. Most threads react to hijack/redirect very fast and
        // typically we can avoid waiting on an event. (except on uniprocessor where we do not spin)
        //
        // Otherwise redo hijacks, but check g_pGCSuspendEvent event on the way.
        // Re-hijacking unconditionally is likely to execute exactly the same hijacks,
        // while not letting the other threads to run much.
        // Thus we require either PING_JIT_TIMEOUT or some progress between active suspension attempts.
        if (g_SystemInfo.dwNumberOfProcessors > 1 && (hasProgress || !observeOnly))
        {
            // small pause
            YieldProcessorNormalized();

            STRESS_LOG1(LF_SYNC, LL_INFO1000, "Spinning, %d threads remaining\n", countThreads);
            observeOnly = true;
            continue;
        }

#ifdef TIME_SUSPEND
        DWORD startWait = g_SuspendStatistics.GetTime();
#endif

        // Wait for at least one thread to tell us it's left cooperative mode.
        // we do this by waiting on g_pGCSuspendEvent.  We cannot simply wait forever, because we
        // might have done return-address hijacking on a thread, and that thread might not
        // return from the method we hijacked (maybe it calls into some other managed code that
        // executes a long loop, for example).  We wait with a timeout, and retry hijacking/redirection.
        //
        // This is unfortunate, because it means that in some cases we wait for PING_JIT_TIMEOUT
        // milliseconds, causing long GC pause times.

        STRESS_LOG1(LF_SYNC, LL_INFO1000, "Waiting for suspend event %d threads remaining\n", countThreads);
        DWORD res = g_pGCSuspendEvent->Wait(PING_JIT_TIMEOUT, FALSE);

#ifdef TIME_SUSPEND
        g_SuspendStatistics.wait.Accumulate(
                SuspendStatistics::GetElapsed(startWait,
                                                g_SuspendStatistics.GetTime()));

        g_SuspendStatistics.cntWaits++;
        if (res == WAIT_TIMEOUT)
            g_SuspendStatistics.cntWaitTimeouts++;
#endif

        if (res == WAIT_TIMEOUT || res == WAIT_IO_COMPLETION)
        {
            STRESS_LOG1(LF_SYNC, LL_INFO1000, "    Timed out waiting for rendezvous event %d threads remaining\n", countThreads);
#ifdef _DEBUG
            DWORD dbgEndTimeout = GetTickCount();

            if ((dbgEndTimeout > dbgStartTimeout) &&
                (dbgEndTimeout - dbgStartTimeout > g_pConfig->SuspendDeadlockTimeout()))
            {
                // Do not change this to _ASSERTE.
                // We want to catch the state of the machine at the
                // time when we can not suspend some threads.
                // It takes too long for _ASSERTE to stop the process.
                DebugBreak();
                _ASSERTE(!"Timed out trying to suspend EE due to thread");
                char message[256];

                Thread* thread = NULL;
                while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
                {
                    if (thread == pCurThread)
                        continue;

                    if ((thread->m_State & Thread::TS_GCSuspendPending) == 0)
                        continue;

                    if (thread->m_fPreemptiveGCDisabled)
                    {
                        DWORD id = (DWORD) thread->m_OSThreadId;
                        if (id == 0xbaadf00d)
                        {
                            sprintf_s (message, ARRAY_SIZE(message), "Thread CLR ID=%x cannot be suspended",
                                        thread->GetThreadId());
                        }
                        else
                        {
                            sprintf_s (message, ARRAY_SIZE(message), "Thread OS ID=%x cannot be suspended",
                                        id);
                        }
                        DbgAssertDialog(__FILE__, __LINE__, message);
                    }
                }
                // if we continue from the assert we'll reset the time
                dbgStartTimeout = GetTickCount();
            }
#endif
       }
        else if (res == WAIT_OBJECT_0)
        {
        }
        else
        {
            // No WAIT_FAILED, WAIT_ABANDONED, etc.
            _ASSERTE(!"unexpected wait termination during gc suspension");
        }

        observeOnly = false;
        g_pGCSuspendEvent->Reset();
    }

#ifdef PROFILING_SUPPORTED
    // If a profiler is keeping track of GC events, notify it
    {
    BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
    (&g_profControlBlock)->RuntimeSuspendFinished();
    END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

#ifdef _DEBUG
    if (reason == ThreadSuspend::SUSPEND_FOR_GC)
    {
        Thread* thread = NULL;
        while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
        {
            thread->DisableStressHeap();
            _ASSERTE(!thread->HasThreadState(Thread::TS_GCSuspendPending));
        }
    }
#endif

    // We know all threads are in preemptive mode, so go ahead and reset the event.
    g_pGCSuspendEvent->Reset();

#ifdef HAVE_GCCOVER
    //
    // Now that the EE has been suspended, let's see if any oustanding
    // gcstress instruction updates need to occur.  Each thread can
    // have only one pending at a time.
    //
    Thread* thread = NULL;
    while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
    {
        thread->CommitGCStressInstructionUpdate();
    }
#endif // HAVE_GCCOVER

    STRESS_LOG0(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime() - Success\n");
}

#ifdef HAVE_GCCOVER

void Thread::CommitGCStressInstructionUpdate()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BYTE* pbDestCode = NULL;
    BYTE* pbSrcCode = NULL;

    if (TryClearGCStressInstructionUpdate(&pbDestCode, &pbSrcCode))
    {
        assert(pbDestCode != NULL);
        assert(pbSrcCode != NULL);

        ExecutableWriterHolder<BYTE> destCodeWriterHolder(pbDestCode, sizeof(DWORD));

#if defined(TARGET_X86) || defined(TARGET_AMD64)

        *destCodeWriterHolder.GetRW() = *pbSrcCode;

#elif defined(TARGET_ARM)

        if (GetARMInstructionLength(pbDestCode) == 2)
            *(WORD*)destCodeWriterHolder.GetRW()  = *(WORD*)pbSrcCode;
        else
            *(DWORD*)destCodeWriterHolder.GetRW() = *(DWORD*)pbSrcCode;

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

        *(DWORD*)destCodeWriterHolder.GetRW() = *(DWORD*)pbSrcCode;

#else

        *destCodeWriterHolder.GetRW() = *pbSrcCode;

#endif

        FlushInstructionCache(GetCurrentProcess(), (LPCVOID)pbDestCode, 4);
    }
}

#endif // HAVE_GCCOVER


#ifdef _DEBUG
void EnableStressHeapHelper()
{
    WRAPPER_NO_CONTRACT;
    ENABLESTRESSHEAP();
}
#endif

// We're done with our GC.  Let all the threads run again.
// By this point we've already unblocked most threads.  This just releases the ThreadStore lock.
void ThreadSuspend::ResumeRuntime(BOOL bFinishedGC, BOOL SuspendSucceeded)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    Thread  *pCurThread = GetThreadNULLOk();

    // Caller is expected to be holding the ThreadStore lock.  But they must have
    // reset GcInProgress, or threads will continue to suspend themselves and won't
    // be resumed until the next GC.
    _ASSERTE(IsGCSpecialThread() || ThreadStore::HoldingThreadStore());
    _ASSERTE(!GCHeapUtilities::IsGCInProgress() );

    STRESS_LOG2(LF_SYNC, LL_INFO1000, "Thread::ResumeRuntime(finishedGC=%d, SuspendSucceeded=%d) - Start\n", bFinishedGC, SuspendSucceeded);

    //
    // Notify everyone who cares, that this suspension is over, and this thread is going to go do other things.
    //


#ifdef PROFILING_SUPPORTED
    // Need to give resume event for the GC thread
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
        if (pCurThread)
        {
            (&g_profControlBlock)->RuntimeThreadResumed((ThreadID)pCurThread);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

#ifdef TIME_SUSPEND
    DWORD startRelease = g_SuspendStatistics.GetTime();
#endif

    //
    // Unlock the thread store.  At this point, all threads should be allowed to run.
    //
    ThreadSuspend::UnlockThreadStore();

#ifdef TIME_SUSPEND
    g_SuspendStatistics.releaseTSL.Accumulate(SuspendStatistics::GetElapsed(startRelease,
                                                                            g_SuspendStatistics.GetTime()));
#endif

#ifdef PROFILING_SUPPORTED
    //
    // This thread is logically "resuming" from a GC now.  Tell the profiler.
    //
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
        GCX_PREEMP();
        (&g_profControlBlock)->RuntimeResumeFinished();
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    //
    // If we raised this thread's priority in SuspendRuntime, we restore it here.
    //
    if (pCurThread)
    {
        if (pCurThread->m_Priority != INVALID_THREAD_PRIORITY)
        {
            pCurThread->SetThreadPriority(pCurThread->m_Priority);
            pCurThread->m_Priority = INVALID_THREAD_PRIORITY;
        }
    }

    STRESS_LOG0(LF_SYNC, LL_INFO1000, "Thread::ResumeRuntime() - End\n");
}

#ifndef TARGET_UNIX
#ifdef TARGET_X86
//****************************************************************************************
// This will resume the thread at the location of redirection.
//
int RedirectedThrowControlExceptionFilter(
    PEXCEPTION_POINTERS pExcepPtrs     // Exception data
    )
{
    // !!! Do not use a non-static contract here.
    // !!! Contract may insert an exception handling record.
    // !!! This function assumes that GetCurrentSEHRecord() returns the exception record set up in
    // !!! ThrowControlForThread
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    if (pExcepPtrs->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Get the thread handle
    Thread *pThread = GetThread();

    STRESS_LOG0(LF_SYNC, LL_INFO100, "In RedirectedThrowControlExceptionFilter\n");

    // If we get here via COM+ exception, gc-mode is unknown.  We need it to
    // be cooperative for this function.
    _ASSERTE (pThread->PreemptiveGCDisabled());

    _ASSERTE(pExcepPtrs->ExceptionRecord->ExceptionCode == BOOTUP_EXCEPTION_COMPLUS);

    // Copy the saved context record into the EH context;
    CONTEXT *pCtx = pThread->m_OSContext;
    ReplaceExceptionContextRecord(pExcepPtrs->ContextRecord, pCtx);

    /////////////////////////////////////////////////////////////////////////////
    // NOTE: Ugly, ugly workaround.
    // We need to resume the thread into the managed code where it was redirected,
    // and the corresponding ESP is below the current one.  But C++ expects that
    // on an EXCEPTION_CONTINUE_EXECUTION that the ESP will be above where it has
    // installed the SEH handler.  To solve this, we need to remove all handlers
    // that reside above the resumed ESP, but we must leave the OS-installed
    // handler at the top, so we grab the top SEH handler, call
    // PopSEHRecords which will remove all SEH handlers above the target ESP and
    // then link the OS handler back in with SetCurrentSEHRecord.

    // Get the special OS handler and save it until PopSEHRecords is done
    EXCEPTION_REGISTRATION_RECORD *pCurSEH = GetCurrentSEHRecord();

    // Unlink all records above the target resume ESP
    PopSEHRecords((LPVOID)(size_t)pCtx->Esp);

    // Link the special OS handler back in to the top
    pCurSEH->Next = GetCurrentSEHRecord();

    // Register the special OS handler as the top handler with the OS
    SetCurrentSEHRecord(pCurSEH);

    // Resume execution at point where thread was originally redirected
    return (EXCEPTION_CONTINUE_EXECUTION);
}
#endif
#endif // !TARGET_UNIX

// Resume a thread at this location, to persuade it to throw a ThreadStop.  The
// exception handler needs a reasonable idea of how large this method is, so don't
// add lots of arbitrary code here.
void
ThrowControlForThread(
#ifdef FEATURE_EH_FUNCLETS
        FaultingExceptionFrame *pfef
#endif // FEATURE_EH_FUNCLETS
        )
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

    Thread *pThread = GetThread();
    _ASSERTE(pThread->m_OSContext);

    _ASSERTE(pThread->PreemptiveGCDisabled());

    // Check if we can start abort
    // We use InducedThreadRedirect as a marker to tell stackwalker that a thread is redirected from JIT code.
    // This is to distinguish a thread is in Preemptive mode and in JIT code.
    // After stackcrawl, we change to InducedThreadStop.
    if (pThread->ThrewControlForThread() == Thread::InducedThreadRedirect ||
        pThread->ThrewControlForThread() == Thread::InducedThreadRedirectAtEndOfCatch)
    {
        _ASSERTE((pThread->m_OSContext->ContextFlags & CONTEXT_ALL) == CONTEXT_ALL);
        if (!pThread->ReadyForAbort())
        {
            STRESS_LOG0(LF_SYNC, LL_INFO100, "ThrowControlForThread resume\n");
            pThread->ResetThrowControlForThread();
            // Thread abort is not allowed at this point
#ifndef FEATURE_EH_FUNCLETS
            __try{
                RaiseException(BOOTUP_EXCEPTION_COMPLUS,0,0,NULL);
            }
            __except(RedirectedThrowControlExceptionFilter(GetExceptionInformation()))
            {
                _ASSERTE(!"Should not reach here");
            }
#else // FEATURE_EH_FUNCLETS
            RtlRestoreContext(pThread->m_OSContext, NULL);
#endif // !FEATURE_EH_FUNCLETS
            _ASSERTE(!"Should not reach here");
        }
        pThread->SetThrowControlForThread(Thread::InducedThreadStop);
    }

#if defined(FEATURE_EH_FUNCLETS)
    *(TADDR*)pfef = FaultingExceptionFrame::GetMethodFrameVPtr();
    *pfef->GetGSCookiePtr() = GetProcessGSCookie();
#else // FEATURE_EH_FUNCLETS
    FrameWithCookie<FaultingExceptionFrame> fef;
    FaultingExceptionFrame *pfef = &fef;
#endif // FEATURE_EH_FUNCLETS
    pfef->InitAndLink(pThread->m_OSContext);

    // !!! Can not assert here.  Sometimes our EHInfo for catch clause extends beyond
    // !!! Jit_EndCatch.  Not sure if we have guarantee on catch clause.
    //_ASSERTE (pThread->ReadyForAbort());

    STRESS_LOG0(LF_SYNC, LL_INFO100, "ThrowControlForThread Aborting\n");

    // Here we raise an exception.
    RaiseComPlusException();
}

#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
// This function is called by UserAbort.
// It forces a thread to abort if allowed and the thread is running managed code.
BOOL Thread::HandleJITCaseForAbort()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(ThreadStore::HoldingThreadStore());

    WorkingOnThreadContextHolder workingOnThreadContext(this);
    if (!workingOnThreadContext.Acquired())
    {
        return FALSE;
    }

    _ASSERTE (m_fPreemptiveGCDisabled);

    CONTEXT ctx;
    ctx.ContextFlags = CONTEXT_CONTROL | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXCEPTION_REQUEST;
    BOOL success     = EEGetThreadContext(this, &ctx);
    _ASSERTE(success && "Thread::HandleJITCaseForAbort : Failed to get thread context");

    if (success)
    {
        success = IsContextSafeToRedirect(&ctx);
    }

    if (success)
    {
        PCODE curIP = GetIP(&ctx);

        // check if this is code managed by the code manager (ie. in the code heap)
        if (ExecutionManager::IsManagedCode(curIP))
        {
            return ResumeUnderControl(&ctx);
        }
    }

    return FALSE;
}

// Threads suspended by ClrSuspendThread() are resumed in two ways.  If we
// suspended them in error, they are resumed via ClrResumeThread().  But if
// this is the HandledJIT() case and the thread is in fully interruptible code, we
// can resume them under special control.  ResumeRuntime and UserResume are cases
// of this.
//
// The suspension has done its work (e.g. GC or user thread suspension).  But during
// the resumption we may have more that we want to do with this thread.  For example,
// there may be a pending ThreadAbort request.  Instead of resuming the thread at its
// current EIP, we tweak its resumption point via the thread context.  Then it starts
// executing at a new spot where we can have our way with it.

BOOL Thread::ResumeUnderControl(CONTEXT *pCtx)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL fSuccess = FALSE;

    LOG((LF_APPDOMAIN, LL_INFO100, "ResumeUnderControl %x\n", GetThreadId()));

    BOOL fSucceeded;

    m_OSContext->ContextFlags = CONTEXT_ALL | CONTEXT_EXCEPTION_REQUEST;
    fSucceeded = EEGetThreadContext(this, m_OSContext);

    if (fSucceeded)
    {
        if (GetIP(pCtx) != GetIP(m_OSContext))
        {
            return FALSE;
        }
        fSucceeded = IsContextSafeToRedirect(m_OSContext);
    }

    if (fSucceeded)
    {
        PCODE resumePC = GetIP(m_OSContext);
        SetIP(m_OSContext, GetEEFuncEntryPoint(THROW_CONTROL_FOR_THREAD_FUNCTION));
        SetThrowControlForThread(InducedThreadRedirect);
        STRESS_LOG1(LF_SYNC, LL_INFO100, "ResumeUnderControl for Thread %p\n", this);

#ifdef TARGET_AMD64
        // We need to establish the return value on the stack in the redirection stub, to
        // achieve crawlability.  We use 'rcx' as the way to communicate the return value.
        // However, we are going to crawl in ReadyForAbort and we are going to resume in
        // ThrowControlForThread using m_OSContext.  It's vital that the original correct
        // Rcx is present at those times, or we will have corrupted Rcx at the point of
        // resumption.
        UINT_PTR    keepRcx = m_OSContext->Rcx;

        m_OSContext->Rcx = (UINT_PTR)resumePC;
#endif // TARGET_AMD64

#if defined(TARGET_ARM)
        // We save the original ControlPC in LR on ARM.
        UINT_PTR originalLR = m_OSContext->Lr;
        m_OSContext->Lr = (UINT_PTR)resumePC;

        // Since we have set a new IP, we have to clear conditional execution flags too.
        UINT_PTR originalCpsr = m_OSContext->Cpsr;
        ClearITState(m_OSContext);
#endif // TARGET_ARM

        EESetThreadContext(this, m_OSContext);

#ifdef TARGET_ARM
        // Restore the original LR now that the OS context has been updated to resume @ redirection function.
        m_OSContext->Lr = originalLR;
        m_OSContext->Cpsr = originalCpsr;
#endif // TARGET_ARM

#ifdef TARGET_AMD64
        // and restore.
        m_OSContext->Rcx = keepRcx;
#endif // TARGET_AMD64

        SetIP(m_OSContext, resumePC);

        fSuccess = TRUE;
    }
#if _DEBUG
    else
        _ASSERTE(!"Couldn't obtain thread context -- StopRequest delayed");
#endif
    return fSuccess;
}

#endif // FEATURE_HIJACK && !TARGET_UNIX


PCONTEXT Thread::GetAbortContext ()
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_EH, LL_INFO100, "Returning abort context: %p\n", m_OSContext));
    return m_OSContext;
}


//****************************************************************************
// Return true if we've Suspended the runtime,
// False if we still need to sweep.
//****************************************************************************
bool Thread::SysStartSuspendForDebug(AppDomain *pAppDomain)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread  *pCurThread = GetThreadNULLOk();
    Thread  *thread = NULL;

    if (IsAtProcessExit())
    {
        LOG((LF_CORDB, LL_INFO1000,
             "SUSPEND: skipping suspend due to process detach.\n"));
        return true;
    }

    LOG((LF_CORDB, LL_INFO1000, "[0x%x] SUSPEND: starting suspend.  Trap count: %d\n",
         pCurThread ? pCurThread->GetThreadId() : (DWORD) -1, g_TrapReturningThreads.Load()));

    // Caller is expected to be holding the ThreadStore lock
    _ASSERTE(ThreadStore::HoldingThreadStore() || IsAtProcessExit());


    // NOTE::NOTE::NOTE::NOTE::NOTE
    // This function has parallel logic in SuspendRuntime.  Please make
    // sure to make appropriate changes there as well.

    _ASSERTE(m_DebugWillSyncCount == -1);
    m_DebugWillSyncCount++;

    // From this point until the end of the function, consider all active thread
    // suspension to be in progress.  This is mainly to give the profiler API a hint
    // that trying to suspend a thread (in order to walk its stack) could delay the
    // overall EE suspension.  So the profiler API would early-abort the stackwalk
    // in such a case.
    ThreadSuspend::SuspendRuntimeInProgressHolder hldSuspendRuntimeInProgress;

    while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
    {
#if 0
//<REVISIT_TODO>  @todo APPD This needs to be finished, replaced, or yanked --MiPanitz</REVISIT_TODO>
        if (m_DebugAppDomainTarget != NULL &&
            thread->GetDomain() != m_DebugAppDomainTarget)
        {
            continue;
        }
#endif

        // Don't try to suspend threads that you've left suspended.
        if (thread->m_StateNC & TSNC_DebuggerUserSuspend)
            continue;

        if (thread == pCurThread)
        {
            LOG((LF_CORDB, LL_INFO1000,
                 "[0x%x] SUSPEND: marking current thread.\n",
                 thread->GetThreadId()));

            _ASSERTE(!thread->m_fPreemptiveGCDisabled);

            // Mark this thread so it trips when it tries to re-enter
            // after completing this call.
            thread->SetupForSuspension(TS_DebugSuspendPending);
            thread->MarkForSuspension(TS_DebugSuspendPending);
            continue;
        }

        thread->SetupForSuspension(TS_DebugSuspendPending);

        // Threads can be in Preemptive or Cooperative GC mode.
        // Threads cannot switch to Cooperative mode without special
        // treatment when a GC is happening.  But they can certainly
        // switch back and forth during a debug suspension -- until we
        // can get their Pending bit set.

#if !defined(DISABLE_THREADSUSPEND) && defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
        DWORD dwSwitchCount = 0;
    RetrySuspension:
#endif // !DISABLE_THREADSUSPEND && FEATURE_HIJACK && !TARGET_UNIX

        SuspendThreadResult str = STR_Success;
        if (!UseContextBasedThreadRedirection())
        {
            // On platforms that do not support safe thread suspension we either
            // rely on the GCPOLL mechanism mechanism enabled by TrapReturningThreads,
            // or we try to hijack/redirect the thread using a thread activation.

            // When we do not suspend the target thread, when reading shared state
            // we need to erect appropriate memory barriers. So the interlocked
            // operation below ensures that any future reads on this thread will
            // happen after any earlier writes on a different thread.
            InterlockedOr((LONG*)&thread->m_fPreemptiveGCDisabled, 0);
        }
        else
        {
#ifndef DISABLE_THREADSUSPEND
            // We can not allocate memory after we suspend a thread.
            // Otherwise, we may deadlock if suspended thread holds allocator locks.
            ThreadStore::AllocateOSContext();
            str = thread->SuspendThread();
#endif // !DISABLE_THREADSUSPEND
        }

        if (thread->m_fPreemptiveGCDisabled && str == STR_Success)
        {
#if !defined(DISABLE_THREADSUSPEND) && defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
            if (UseContextBasedThreadRedirection())
            {
                WorkingOnThreadContextHolder workingOnThreadContext(thread);
                if (workingOnThreadContext.Acquired() && thread->HandledJITCase())
                {
                    // Redirect thread so we can capture a good thread context
                    // (GetThreadContext is not sufficient, due to an OS bug).
                    // If we don't succeed (should only happen on Win9X, due to
                    // a different OS bug), we must resume the thread and try
                    // again.
                    if (!thread->CheckForAndDoRedirectForDbg())
                    {
                        thread->ResumeThread();
                        __SwitchToThread(0, ++dwSwitchCount);
                        goto RetrySuspension;
                    }
                }
            }
#endif // !DISABLE_THREADSUSPEND && FEATURE_HIJACK && !TARGET_UNIX

            // Remember that this thread will be running to a safe point
            InterlockedIncrement(&m_DebugWillSyncCount);

            // When the thread reaches a safe place, it will wait
            // on the DebugSuspendEvent which clients can set when they
            // want to release us.
            thread->MarkForSuspension(TS_DebugSuspendPending |
                                       TS_DebugWillSync
                      );

            if (!UseContextBasedThreadRedirection())
            {
                // There'a a race above between the moment we first check m_fPreemptiveGCDisabled
                // and the moment we enable TrapReturningThreads in MarkForSuspension.  However,
                // nothing bad happens if the thread has transitioned to preemptive before marking
                // the thread for suspension; the thread will later be identified as Synced in
                // SysSweepThreadsForDebug.
                //
                // If the thread transitions to preemptive mode and into a forbid-suspend-for-debugger
                // region, SysSweepThreadsForDebug would similarly identify the thread as synced
                // after it leaves the forbid region.

#if defined(FEATURE_THREAD_ACTIVATION) && defined(TARGET_WINDOWS)
                // Inject an activation that will interrupt the thread and try to bring it to a safe point
                thread->InjectActivation(Thread::ActivationReason::SuspendForDebugger);
#endif // FEATURE_THREAD_ACTIVATION && TARGET_WINDOWS
            }
            else
            {
#ifndef DISABLE_THREADSUSPEND
                // Resume the thread and let it run to a safe point
                thread->ResumeThread();
#endif // !DISABLE_THREADSUSPEND
            }

            LOG((LF_CORDB, LL_INFO1000,
                 "[0x%x] SUSPEND: gc disabled - will sync.\n",
                 thread->GetThreadId()));
        }
        else if (!thread->m_fPreemptiveGCDisabled)
        {
            // Mark threads that are outside the Runtime so that if
            // they attempt to re-enter they will trip.
            thread->MarkForSuspension(TS_DebugSuspendPending);

            if (
                // There'a a race above between the moment we first check m_fPreemptiveGCDisabled
                // and the moment we enable TrapReturningThreads in MarkForSuspension.  To account
                // for that we check whether the thread moved into cooperative mode, and if it had
                // we mark it as a DebugWillSync thread, that will be handled later in
                // SysSweepThreadsForDebug.
                (!UseContextBasedThreadRedirection() && thread->m_fPreemptiveGCDisabled) ||

                // The thread may have been suspended in a forbid-suspend-for-debugger region, or
                // before the state change to set TS_DebugSuspendPending is made visible to other
                // threads, the thread may have transitioned into a forbid region. In either case,
                // flag the thread as TS_DebugWillSync and let SysSweepThreadsForDebug later
                // identify the thread as synced after the thread leaves the forbid region.
                thread->IsInForbidSuspendForDebuggerRegion())
            {
                // Remember that this thread will be running to a safe point
                InterlockedIncrement(&m_DebugWillSyncCount);
                thread->SetThreadState(TS_DebugWillSync);
            }

#ifndef DISABLE_THREADSUSPEND
            if (str == STR_Success && UseContextBasedThreadRedirection()) {
                thread->ResumeThread();
            }
#endif // !DISABLE_THREADSUSPEND

            LOG((LF_CORDB, LL_INFO1000,
                 "[0x%x] SUSPEND: gc enabled.\n", thread->GetThreadId()));
        }
    }

    //
    // Return true if all threads are synchronized now, otherwise the
    // debugge must wait for the SuspendComplete, called from the last
    // thread to sync.
    //

    if (InterlockedDecrement(&m_DebugWillSyncCount) < 0)
    {
        LOG((LF_CORDB, LL_INFO1000,
             "SUSPEND: all threads sync before return.\n"));
        return true;
    }
    else
        return false;
}

//
// This method is called by the debugger helper thread when it times out waiting for a set of threads to
// synchronize. Its used to chase down threads that are not synchronizing quickly. It returns true if all the threads are
// now synchronized. This also means that we own the thread store lock.
//
// This can be safely called if we're already suspended.
bool Thread::SysSweepThreadsForDebug(bool forceSync)
{
    CONTRACT(bool) {
        NOTHROW;
        DISABLED(GC_TRIGGERS); // WaitUntilConcurrentGCComplete toggle GC mode, disabled because called by unmanaged thread

        // We assume that only the "real" helper thread ever calls this (not somebody doing helper thread duty).
        PRECONDITION(ThreadStore::HoldingThreadStore());
        PRECONDITION(IsDbgHelperSpecialThread());
        PRECONDITION(GetThreadNULLOk() == NULL);

        // Iff we return true, then we have the TSL (or the aux lock used in workarounds).
        POSTCONDITION(ThreadStore::HoldingThreadStore());
    }
    CONTRACT_END;

    _ASSERTE(!forceSync); // deprecated parameter

    Thread *thread = NULL;

    // NOTE::NOTE::NOTE::NOTE::NOTE
    // This function has parallel logic in SuspendRuntime.  Please make
    // sure to make appropriate changes there as well.

    // From this point until the end of the function, consider all active thread
    // suspension to be in progress.  This is mainly to give the profiler API a hint
    // that trying to suspend a thread (in order to walk its stack) could delay the
    // overall EE suspension.  So the profiler API would early-abort the stackwalk
    // in such a case.
    ThreadSuspend::SuspendRuntimeInProgressHolder hldSuspendRuntimeInProgress;

    // Loop over the threads...
    while (((thread = ThreadStore::GetThreadList(thread)) != NULL) && (m_DebugWillSyncCount >= 0))
    {
        // Skip threads that we aren't waiting for to sync.
        if ((thread->m_State & TS_DebugWillSync) == 0)
            continue;

        if (!UseContextBasedThreadRedirection())
        {
            // On platforms that do not support safe thread suspension we either
            // rely on the GCPOLL mechanism mechanism enabled by TrapReturningThreads,
            // or we try to hijack/redirect the thread using a thread activation.

            // When we do not suspend the target thread, when reading shared state
            // we need to erect appropriate memory barriers. So the interlocked
            // operation below ensures that any future reads on this thread will
            // happen after any earlier writes on a different thread.
            InterlockedOr((LONG*)&thread->m_fPreemptiveGCDisabled, 0);
            if (!thread->m_fPreemptiveGCDisabled)
            {
                if (thread->IsInForbidSuspendForDebuggerRegion())
                {
                    continue;
                }

                // If the thread toggled to preemptive mode and is not in a
                // forbid-suspend-for-debugger region, then it's synced.
                goto Label_MarkThreadAsSynced;
            }

#if defined(FEATURE_THREAD_ACTIVATION) && defined(TARGET_WINDOWS)
            // Inject an activation that will interrupt the thread and try to bring it to a safe point
            thread->InjectActivation(Thread::ActivationReason::SuspendForDebugger);
#endif // FEATURE_THREAD_ACTIVATION && TARGET_WINDOWS

            continue;
        }

#ifndef DISABLE_THREADSUSPEND
        // Suspend the thread

#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
        DWORD dwSwitchCount = 0;
#endif

RetrySuspension:
        // We can not allocate memory after we suspend a thread.
        // Otherwise, we may deadlock if the suspended thread holds allocator locks.
        ThreadStore::AllocateOSContext();
        SuspendThreadResult str = thread->SuspendThread();

        if (str == STR_Failure || str == STR_UnstartedOrDead)
        {
            // The thread cannot actually be unstarted - if it was, we would not
            // have marked it with TS_DebugWillSync in the first phase.
            _ASSERTE(!(thread->m_State & TS_Unstarted));

            // If the thread has gone, we can't wait on it.
            goto Label_MarkThreadAsSynced;
        }
        else if (str == STR_NoStressLog)
        {
            goto RetrySuspension;
        }
        else if (!thread->m_fPreemptiveGCDisabled)
        {
            // If the thread toggled to preemptive mode and is not in a
            // forbid-suspend-for-debugger region, then it's synced.

            // We can safely resume the thread here b/c it's in PreemptiveMode and the
            // EE will trap anybody trying to re-enter cooperative. So letting it run free
            // won't hurt the runtime.
            //
            // If the thread is in a forbid-suspend-for-debugger region, the thread needs to
            // be resumed to give it a chance to leave the forbid region. The EE will also
            // trap the thread if it tries to re-enter a forbid region.
            _ASSERTE(str == STR_Success);
            thread->ResumeThread();

            if (!thread->IsInForbidSuspendForDebuggerRegion())
            {
                goto Label_MarkThreadAsSynced;
            }
            else
            {
                continue;
            }
        }
#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
        // If the thread is in jitted code, HandledJitCase will try to hijack it; and the hijack
        // will toggle the GC.
        else
        {
            _ASSERTE(str == STR_Success);
            WorkingOnThreadContextHolder workingOnThreadContext(thread);
            if (workingOnThreadContext.Acquired() && thread->HandledJITCase())
            {
                // Redirect thread so we can capture a good thread context
                // (GetThreadContext is not sufficient, due to an OS bug).
                // If we don't succeed (should only happen on Win9X, due to
                // a different OS bug), we must resume the thread and try
                // again.
                if (!thread->CheckForAndDoRedirectForDbg())
                {
                    thread->ResumeThread();
                    __SwitchToThread(0, ++dwSwitchCount);
                    goto RetrySuspension;
                }

                // The hijack will toggle our GC mode, and thus we could wait for the next sweep,
                // and the GC-mode check above would catch and sync us. But there's no reason to wait,
                // if the thread is redirected, it's as good as synced, so mark it now.
                thread->ResumeThread();
                goto Label_MarkThreadAsSynced;
            }
        }
#endif // FEATURE_HIJACK && !TARGET_UNIX

        // If we didn't take the thread out of the set, then resume it and give it another chance to reach a safe
        // point.
        thread->ResumeThread();
        continue;

#endif // !DISABLE_THREADSUSPEND

        // The thread is synced. Remove the sync bits and dec the sync count.
Label_MarkThreadAsSynced:
        thread->ResetThreadState(TS_DebugWillSync);
        if (InterlockedDecrement(&m_DebugWillSyncCount) < 0)
        {
            // If that was the last thread, then the CLR is synced.
            // We return while own the thread store lock. We return true now, which indicates this to the caller.
            RETURN true;
        }
        continue;

    } // end looping through Thread Store

    if (m_DebugWillSyncCount < 0)
    {
        RETURN true;
    }

    // The CLR is not yet synced. We release the threadstore lock and return false.
    hldSuspendRuntimeInProgress.Release();

    RETURN false;
}

void Thread::SysResumeFromDebug(AppDomain *pAppDomain)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread  *thread = NULL;

    if (IsAtProcessExit())
    {
        LOG((LF_CORDB, LL_INFO1000,
             "RESUME: skipping resume due to process detach.\n"));
        return;
    }

    LOG((LF_CORDB, LL_INFO1000, "RESUME: starting resume AD:0x%x.\n", pAppDomain));


    // Make sure we completed the previous sync
    _ASSERTE(m_DebugWillSyncCount == -1);

    // Caller is expected to be holding the ThreadStore lock
    _ASSERTE(ThreadStore::HoldingThreadStore() || IsAtProcessExit());

    while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
    {
        // Only consider resuming threads if they're in the correct appdomain
        if (pAppDomain != NULL && thread->GetDomain() != pAppDomain)
        {
            LOG((LF_CORDB, LL_INFO1000, "RESUME: Not resuming thread 0x%x, since it's "
                "in appdomain 0x%x.\n", thread, pAppDomain));
            continue;
        }

        // If the user wants to keep the thread suspended, then
        // don't release the thread.
        if (!(thread->m_StateNC & TSNC_DebuggerUserSuspend))
        {
            // If we are still trying to suspend this thread, forget about it.
            if (thread->m_State & TS_DebugSuspendPending)
            {
                LOG((LF_CORDB, LL_INFO1000,
                     "[0x%x] RESUME: TS_DebugSuspendPending was set, but will be removed\n",
                     thread->GetThreadId()));

#ifdef FEATURE_EMULATE_SINGLESTEP
                if (thread->IsSingleStepEnabled())
                {
                    if (ISREDIRECTEDTHREAD(thread))
                        thread->ApplySingleStep(GETREDIRECTEDCONTEXT(thread));
                }
#endif
                // Note: we unmark for suspension _then_ set the suspend event.
                thread->ReleaseFromSuspension(TS_DebugSuspendPending);
            }

        }
        else
        {
            // Thread will remain suspended due to a request from the debugger.

            LOG((LF_CORDB,LL_INFO10000,"Didn't unsuspend thread 0x%x"
                "(ID:0x%x)\n", thread, thread->GetThreadId()));
            LOG((LF_CORDB,LL_INFO10000,"Suspending:0x%x\n",
                thread->m_State & TS_DebugSuspendPending));
            _ASSERTE((thread->m_State & TS_DebugWillSync) == 0);

        }
    }

    LOG((LF_CORDB, LL_INFO1000, "RESUME: resume complete. Trap count: %d\n", g_TrapReturningThreads.Load()));
}

/*
 *
 * WaitSuspendEventsHelper
 *
 * This function is a simple helper function for WaitSuspendEvents.  It is needed
 * because of the EX_TRY macro.  This macro does an alloca(), which allocates space
 * off the stack, not free'ing it.  Thus, doing a EX_TRY in a loop can easily result
 * in a stack overflow error.  By factoring out the EX_TRY into a separate function,
 * we recover that stack space.
 *
 * Parameters:
 *   None.
 *
 * Return:
 *   true if meant to continue, else false.
 *
 */
BOOL Thread::WaitSuspendEventsHelper(void)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    DWORD result = WAIT_FAILED;

    EX_TRY {

        if (m_State & TS_DebugSuspendPending) {

            ThreadState oldState = m_State;

            while (oldState & TS_DebugSuspendPending) {

                ThreadState newState = (ThreadState)(oldState | TS_SyncSuspended);
                if (InterlockedCompareExchange((LONG *)&m_State, newState, oldState) == (LONG)oldState)
                {
                    result = m_DebugSuspendEvent.Wait(INFINITE,FALSE);
#if _DEBUG
                    newState = m_State;
                    _ASSERTE(!(newState & TS_SyncSuspended));
#endif
                    break;
                }

                oldState = m_State;
            }
        }
    }
    EX_CATCH {
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result != WAIT_OBJECT_0;
}


// There's a bit of a workaround here
void Thread::WaitSuspendEvents(BOOL fDoWait)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    _ASSERTE(!PreemptiveGCDisabled());
    _ASSERTE((m_State & TS_SyncSuspended) == 0);

    // Let us do some useful work before suspending ourselves.

    // If we're required to perform a wait, do so.  Typically, this is
    // skipped if this thread is a Debugger Special Thread.
    if (fDoWait)
    {
        while (TRUE)
        {
            WaitSuspendEventsHelper();

            ThreadState oldState = m_State;

            //
            // If all reasons to suspend are off, we think we can exit
            // this loop, but we need to check atomically.
            //
            if ((oldState & TS_DebugSuspendPending) == 0)
            {
                //
                // Construct the destination state we desire - all suspension bits turned off.
                //
                ThreadState newState = (ThreadState)(oldState & ~(TS_DebugSuspendPending | TS_SyncSuspended));

                if (InterlockedCompareExchange((LONG *)&m_State, newState, oldState) == (LONG)oldState)
                {
                    //
                    // We are done.
                    //
                    break;
                }
            }
        }
    }
}

#ifdef FEATURE_HIJACK
//                      Hijacking JITted calls
//                      ======================

// State of execution when we suspend a thread
struct ExecutionState
{
    BOOL            m_FirstPass;
    BOOL            m_IsJIT;            // are we executing JITted code?
    MethodDesc     *m_pFD;              // current function/method we're executing
    VOID          **m_ppvRetAddrPtr;    // pointer to return address in frame
    DWORD           m_RelOffset;        // relative offset at which we're currently executing in this fcn
    IJitManager    *m_pJitManager;
    METHODTOKEN     m_MethodToken;
    BOOL            m_IsInterruptible;  // is this code interruptible?

    ExecutionState() : m_FirstPass(TRUE) {LIMITED_METHOD_CONTRACT;  }
};

// Client is responsible for suspending the thread before calling
void Thread::HijackThread(ReturnKind returnKind, ExecutionState *esb)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidReturnKind(returnKind));

    VOID *pvHijackAddr = reinterpret_cast<VOID *>(OnHijackTripThread);

#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
    void* returnAddressHijackTarget = ThreadSuspend::GetReturnAddressHijackTarget();
    if (returnAddressHijackTarget != NULL)
    {
        pvHijackAddr = returnAddressHijackTarget;
    }
#endif // TARGET_WINDOWS && TARGET_AMD64

#ifdef TARGET_X86
    if (returnKind == RT_Float)
    {
        pvHijackAddr = reinterpret_cast<VOID *>(OnHijackFPTripThread);
    }
#endif // TARGET_X86

    // Don't hijack if are in the first level of running a filter/finally/catch.
    // This is because they share ebp with their containing function further down the
    // stack and we will hijack their containing function incorrectly
    if (IsInFirstFrameOfHandler(this, esb->m_pJitManager, esb->m_MethodToken, esb->m_RelOffset))
    {
        STRESS_LOG3(LF_SYNC, LL_INFO100, "Thread::HijackThread(%p to %p): Early out - IsInFirstFrameOfHandler. State=%x.\n", this, pvHijackAddr, (ThreadState)m_State);
        return;
    }

    // Don't hijack if a profiler stackwalk is in progress
    HijackLockHolder hijackLockHolder(this);
    if (!hijackLockHolder.Acquired())
    {
        STRESS_LOG3(LF_SYNC, LL_INFO100, "Thread::HijackThread(%p to %p): Early out - !hijackLockHolder.Acquired. State=%x.\n", this, pvHijackAddr, (ThreadState)m_State);
        return;
    }

    SetHijackReturnKind(returnKind);

    if (m_State & TS_Hijacked)
        UnhijackThread();

    // Make sure that the location of the return address is on the stack
    _ASSERTE(IsAddressInStack(esb->m_ppvRetAddrPtr));

    // Obtain the location of the return address in the currently executing stack frame
    m_ppvHJRetAddrPtr = esb->m_ppvRetAddrPtr;

    // Remember the place that the return would have gone
    m_pvHJRetAddr = *esb->m_ppvRetAddrPtr;

    IS_VALID_CODE_PTR((FARPROC) (TADDR)m_pvHJRetAddr);
    // TODO [DAVBR]: For the full fix for VsWhidbey 450273, the below
    // may be uncommented once isLegalManagedCodeCaller works properly
    // with non-return address inputs, and with non-DEBUG builds
    //_ASSERTE(isLegalManagedCodeCaller((TADDR)m_pvHJRetAddr));
    STRESS_LOG2(LF_SYNC, LL_INFO100, "Hijacking return address 0x%p for thread %p\n", m_pvHJRetAddr, this);

    // Remember the method we're executing
    m_HijackedFunction = esb->m_pFD;

    // Bash the stack to return to one of our stubs
    *esb->m_ppvRetAddrPtr = pvHijackAddr;
    SetThreadState(TS_Hijacked);
}

// If we are unhijacking another thread (not the current thread), then the caller is responsible for
// suspending that thread.
// It's legal to unhijack the current thread without special treatment.
void Thread::UnhijackThread()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (m_State & TS_Hijacked)
    {
        IS_VALID_WRITE_PTR(m_ppvHJRetAddrPtr, sizeof(void*));
        IS_VALID_CODE_PTR((FARPROC) m_pvHJRetAddr);

        // Can't make the following assertion, because sometimes we unhijack after
        // the hijack has tripped (i.e. in the case we actually got some value from
        // it.
//       _ASSERTE(*m_ppvHJRetAddrPtr == OnHijackTripThread);

        STRESS_LOG2(LF_SYNC, LL_INFO100, "Unhijacking return address 0x%p for thread %p\n", m_pvHJRetAddr, this);
        // restore the return address and clear the flag
        *m_ppvHJRetAddrPtr = m_pvHJRetAddr;
        ResetThreadState(TS_Hijacked);

        // But don't touch m_pvHJRetAddr.  We may need that to resume a thread that
        // is currently hijacked!
    }
}

// Get the ExecutionState for the specified *SUSPENDED* thread.  Note that this is
// a 'StackWalk' call back (PSTACKWALKFRAMESCALLBACK).
StackWalkAction SWCB_GetExecutionState(CrawlFrame *pCF, VOID *pData)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ExecutionState  *pES = (ExecutionState *) pData;
    StackWalkAction  action = SWA_ABORT;

    if (pES->m_FirstPass)
    {
        // This will help factor out some repeated code.
        bool notJittedCase = false;

        // If we're jitted code at the top of the stack, grab everything
        if (pCF->IsFrameless() && pCF->IsActiveFunc())
        {
            pES->m_IsJIT = TRUE;
            pES->m_pFD = pCF->GetFunction();
            pES->m_MethodToken = pCF->GetMethodToken();
            pES->m_ppvRetAddrPtr = 0;
            pES->m_IsInterruptible = pCF->IsGcSafe();
            pES->m_RelOffset = pCF->GetRelOffset();
            pES->m_pJitManager = pCF->GetJitManager();

            STRESS_LOG3(LF_SYNC, LL_INFO1000, "Stopped in Jitted code at pc = %p sp = %p fullyInt=%d\n",
                GetControlPC(pCF->GetRegisterSet()), GetRegdisplaySP(pCF->GetRegisterSet()), pES->m_IsInterruptible);

#if defined(FEATURE_CONSERVATIVE_GC) && !defined(USE_GC_INFO_DECODER)
            if (g_pConfig->GetGCConservative())
            {
                // Conservative GC enabled; behave as if HIJACK_NONINTERRUPTIBLE_THREADS had not been
                // set above:
                //
                notJittedCase = true;
            }
            else
#endif // FEATURE_CONSERVATIVE_GC
            {
#ifndef HIJACK_NONINTERRUPTIBLE_THREADS
                if (!pES->m_IsInterruptible)
                {
                    notJittedCase = true;
                }
#else // HIJACK_NONINTERRUPTIBLE_THREADS
                // if we're not interruptible right here, we need to determine the
                // return address for hijacking.
                if (!pES->m_IsInterruptible)
                {
#ifdef FEATURE_EH_FUNCLETS
                    PREGDISPLAY pRDT = pCF->GetRegisterSet();
                    _ASSERTE(pRDT != NULL);

                    // For simplicity, don't hijack in funclets
                    bool fIsFunclet = pCF->IsFunclet();
                    if (fIsFunclet)
                    {
                        notJittedCase = true;
                    }
                    else
                    {
                         // We already have the caller context available at this point
                        _ASSERTE(pRDT->IsCallerContextValid);
#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

                        // Why do we use CallerContextPointers below?
                        //
                        // Assume the following callstack, growing from left->right:
                        //
                        // C -> B -> A
                        //
                        // Assuming A is non-interruptible function and pushes LR on stack,
                        // when we get the stackwalk callback for A, the CallerContext would
                        // contain non-volatile register state for B and CallerContextPtrs would
                        // contain the location where the caller's (B's) non-volatiles where restored
                        // from. This would be the stack location in A where they were pushed. Thus,
                        // CallerContextPtrs->Lr would contain the stack location in A where LR (representing an address in B)
                        // was pushed and thus, contains the return address in B.

                        // Note that the JIT always pushes LR even for leaf methods to make hijacking
                        // work for them. See comment in code:Compiler::genPushCalleeSavedRegisters.

#if defined(TARGET_LOONGARCH64)
                        if (pRDT->pCallerContextPointers->Ra == &pRDT->pContext->Ra)
#else
                        if(pRDT->pCallerContextPointers->Lr == &pRDT->pContext->Lr)
#endif
                        {
                            // This is the case when we are either:
                            //
                            // 1) In a leaf method that does not push LR on stack, OR
                            // 2) In the prolog/epilog of a non-leaf method that has not yet pushed LR on stack
                            //    or has LR already popped off.
                            //
                            // The remaining case of non-leaf method is that of IP being in the body of the
                            // function. In such a case, LR would be have been pushed on the stack and thus,
                            // we wouldnt be here but in the "else" clause below.
                            //
                            // For (1) we can use CallerContext->ControlPC to be used as the return address
                            // since we know that leaf frames will return back to their caller.
                            // For this, we may need JIT support to do so.
                            notJittedCase = true;
                        }
                        else if (pCF->HasTailCalls())
                        {
                            // Do not hijack functions that have tail calls, since there are two problems:
                            // 1. When a function that tail calls another one is hijacked, the LR may be
                            //    stored at a different location in the stack frame of the tail call target.
                            //    So just by performing tail call, the hijacked location becomes invalid and
                            //    unhijacking would corrupt stack by writing to that location.
                            // 2. There is a small window after the caller pops LR from the stack in its
                            //    epilog and before the tail called function pushes LR in its prolog when
                            //    the hijacked return address would not be not on the stack and so we would
                            //    not be able to unhijack.
                            notJittedCase = true;
                        }
                        else
                        {
                            // This is the case of IP being inside the method body and LR is
                            // pushed on the stack. We get it to determine the return address
                            // in the caller of the current non-interruptible frame.
#if defined(TARGET_LOONGARCH64)
                            pES->m_ppvRetAddrPtr = (void **) pRDT->pCallerContextPointers->Ra;
#else
                            pES->m_ppvRetAddrPtr = (void **) pRDT->pCallerContextPointers->Lr;
#endif
                        }
#elif defined(TARGET_X86) || defined(TARGET_AMD64)
                        pES->m_ppvRetAddrPtr = (void **) (EECodeManager::GetCallerSp(pRDT) - sizeof(void*));
#else // TARGET_X86 || TARGET_AMD64
                        PORTABILITY_ASSERT("Platform NYI");
#endif // _TARGET_???_
                    }
#else // FEATURE_EH_FUNCLETS
                    // peel off the next frame to expose the return address on the stack
                    pES->m_FirstPass = FALSE;
                    action = SWA_CONTINUE;
#endif // !FEATURE_EH_FUNCLETS
                }
#endif // HIJACK_NONINTERRUPTIBLE_THREADS
            }
            // else we are successfully out of here with SWA_ABORT
        }
        else
        {
#ifdef TARGET_X86
            STRESS_LOG2(LF_SYNC, LL_INFO1000, "Not in Jitted code at EIP = %p, &EIP = %p\n", GetControlPC(pCF->GetRegisterSet()), pCF->GetRegisterSet()->PCTAddr);
#else
            STRESS_LOG1(LF_SYNC, LL_INFO1000, "Not in Jitted code at pc = %p\n", GetControlPC(pCF->GetRegisterSet()));
#endif
            notJittedCase = true;
        }

        // Cases above may have set "notJITtedCase", which we handle as follows:
        if (notJittedCase)
        {
            pES->m_IsJIT = FALSE;
#ifdef _DEBUG
            pES->m_pFD = (MethodDesc *)POISONC;
            pES->m_ppvRetAddrPtr = (void **)POISONC;
            pES->m_IsInterruptible = FALSE;
#endif
        }
    }
    else
    {
#if defined(TARGET_X86) && !defined(FEATURE_EH_FUNCLETS)
        // Second pass, looking for the address of the return address so we can
        // hijack:

        PREGDISPLAY     pRDT = pCF->GetRegisterSet();

        if (pRDT != NULL)
        {
            // pPC points to the return address sitting on the stack, as our
            // current EIP for the penultimate stack frame.
            pES->m_ppvRetAddrPtr = (void **) pRDT->PCTAddr;

            STRESS_LOG2(LF_SYNC, LL_INFO1000, "Partially Int case hijack address = 0x%x val = 0x%x\n", pES->m_ppvRetAddrPtr, *pES->m_ppvRetAddrPtr);
        }
#else
        PORTABILITY_ASSERT("Platform NYI");
#endif
    }

    return action;
}

HijackFrame::HijackFrame(LPVOID returnAddress, Thread *thread, HijackArgs *args)
           : m_ReturnAddress((TADDR)returnAddress),
             m_Thread(thread),
             m_Args(args)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_Thread == GetThread());

    m_Next = m_Thread->GetFrame();
    m_Thread->SetFrame(this);
}

void STDCALL OnHijackWorker(HijackArgs * pArgs)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifdef HIJACK_NONINTERRUPTIBLE_THREADS
    BEGIN_PRESERVE_LAST_ERROR;

    Thread         *thread = GetThread();

    thread->ResetThreadState(Thread::TS_Hijacked);

    // Fix up our caller's stack, so it can resume from the hijack correctly
    pArgs->ReturnAddress = (size_t)thread->m_pvHJRetAddr;

    // Build a frame so that stack crawling can proceed from here back to where
    // we will resume execution.
    FrameWithCookie<HijackFrame> frame((void *)pArgs->ReturnAddress, thread, pArgs);

#ifdef _DEBUG
    BOOL GCOnTransition = FALSE;
    if (g_pConfig->FastGCStressLevel()) {
        GCOnTransition = GC_ON_TRANSITIONS(FALSE);
    }
#endif // _DEBUG

#ifdef TIME_SUSPEND
    g_SuspendStatistics.cntHijackTrap++;
#endif // TIME_SUSPEND

    CommonTripThread();

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GC_ON_TRANSITIONS(GCOnTransition);
    }
#endif // _DEBUG

    frame.Pop();

    END_PRESERVE_LAST_ERROR;
#else
    PORTABILITY_ASSERT("OnHijackWorker not implemented on this platform.");
#endif // HIJACK_NONINTERRUPTIBLE_THREADS
}

static bool GetReturnAddressHijackInfo(EECodeInfo *pCodeInfo, ReturnKind *pReturnKind)
{
    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();
    return pCodeInfo->GetCodeManager()->GetReturnAddressHijackInfo(gcInfoToken, pReturnKind);
}

#ifndef TARGET_UNIX

//
// The function below, ThreadCaughtInKernelModeExceptionHandling, exists to detect and work around a very subtle
// race that we have when we suspend a thread while that thread is in the kernel handling an exception.
//
// When a user-mode thread takes an exception, the OS must get involved to handle that exception before user-mode
// exception handling takes place. The exception causes the thread to enter kernel-mode. To handle the exception,
// the kernel does the following: 1) pushes a CONTEXT, then an EXCEPTION_RECORD, and finally an EXCEPTION_POINTERS
// struct onto the thread's user-mode stack. 2) the Esp value in the thread's user-mode context is updated to
// reflect the fact that these structures have just been pushed. 3) some segment registers in the user-mode context
// are modified. 4) the Eip value in the user-mode context is changed to point to the user-mode exception dispatch
// routine. 5) the kernel resumes user-mode execution with the altered context.
//
// Note that during this entire process: 1) the thread can be suspeded by another user-mode thread, and 2)
// Get/SetThreadContext all operate on the user-mode context.
//
// There are two important races to consider here: a race with attempting to hijack the thread in HandledJITCase,
// and a race attempting to trace the thread's stack in HandledJITCase.
//
//
// Race #1: failure to hijack a thread in HandledJITCase.
//
// In HandledJITCase, if we see that a thread's Eip is in managed code at an interruptible point, we will attempt
// to move the thread to a hijack in order to stop it's execution for a variety of reasons (GC, debugger, user-mode
// supension, etc.) We do this by suspending the thread, inspecting Eip, changing Eip to the address of the hijack
// routine, and resuming the thread.
//
// The problem here is that in step #4 above, the kernel is going to change Eip in the thread's context to point to
// the user-mode exception dispatch routine. If we suspend a thread when it has taken an exception in managed code,
// we may see Eip pointing to managed code and attempt to hijack the thread. When we resume the thread, step #4
// will eventually execute and the thread will go to the user-mode exception dispatch routine instead of to our
// hijack.
//
// We tollerate this by recgonizing that this has happened when we arrive in our exception handler
// (COMPlusFrameHandler), and we fix up the IP in the context passed to the handler.
//
//
// Race #2: inability to trace a managed call stack
//
// If we suspend a thread after step #2 above, but before step #4, then we will see an Eip pointing to managed
// code, but an Esp that points to the newly pushed exception structures. If we are in a managed function that does
// not have an Ebp frame, the return address will be relative to Esp and we will not be able to resolve the return
// address properly. Since we will attempt to place a return address hijack (as part of our heroic efforts to trap
// the thread quickly), we may end up writing over random memory with our hijack. This is obviously extremely
// bad. Realistically, any attempt to trace a thread's stack in this case is suspect, even if the mangaed function
// has a EBP frame.
//
// The solution is to attempt to detect this race and abandon the hijack attempt. We have developed the following
// heuristic to detect this case. Basically, we look to see if Esp points to an EXCEPTION_POINTERS structure, and
// that this structure points to valid EXCEPTION_RECORD and CONTEXT structures. They must be ordered on the stack,
// and the faulting address in the EXCEPTION_RECORD should be the thread's current Eip, and the Eip in the CONTEXT
// should be the thread's current Eip.
//
// This is the heuristic codified. Given Eip and Esp from the thread's current context:
//
// 1. if Eip points to a managed function, and...
// 2. the pointer at Esp is equal to Esp + sizeof(EXCEPTION_POINTERS), and...
// 3. the faulting address in the EXCEPTION_RECORD at that location is equal to the current Eip, and...
// 4. the NumberParameters field in the EXCEPTION_RECORD is valid (between 0 and EXCEPTION_MAXIMUM_PARAMETERS), and...
// 5. the pointer at Esp + 4 is equal to Esp + sizeof(EXCEPTION_POINTERS) + the dynamic size of the EXCEPTION_RECORD, and...
// 6. the Eip value of the CONTEXT at that location is equal to the current Eip, then we have recgonized the race.
//
// The validation of Eip in both places, combined with ensuring that the pointer values are on the thread's stack
// make this a safe heuristic to evaluate. Even if one could end up in a function with the stack looking exactly
// like this, and even if we are trying to suspend such a thread and we catch it at the Eip that matches the values
// at the proper places on the stack, then the worst that will happen is we won't attempt to hijack the thread at
// that point. We'll resume it and try again later. There will be at least one other instruction in the function
// that is not at the Eip value on the stack, and we'll be able to trace the thread's stack from that instruction
// and place the return address hijack.
//
// As races go, race #1 above is very, very easy to hit. We hit it in the wild before we shipped V1, and a simple
// test program with one thread constantly AV'ing and another thread attempting to suspend the first thread every
// half second hit's the race almost instantly.
//
// Race #2 is extremely rare in comparison. The same program properly instrumented only hits the race about 5 times
// every 2000 attempts or so. We did not hit this even in very stressful exception tests and
// it's never been seen in the wild.
//
// Note: a new feature has been added in recent OS's that allows us to detect both of these races with a simple
// call to GetThreadContext. This feature exists on all Win64 platforms, so this change is only for 32-bit
// platforms. We've asked for this fix to be applied to future 32-bit OS's, so we can remove this on those
// platforms when that happens. Furthermore, once we stop supporting the older 32-bit OS versions that don't have
// the new feature, we can remove these altogether.
//
// WARNING: Interrupts (int 3) immediately increment the IP whereas traps (AVs) do not.
// So this heuristic only works for trap, but not for interrupts. As a result, the race
// is still a problem for interrupts. This means that the race can cause a process crash
// if the managed debugger puts an "int 3" in order to do a stepping operation,
// and GC or a sampling profiler tries to suspend the thread. This can be handled
// by modifying the code below to additionally check if the instruction just before
// the IP is an "int 3".
//

#ifdef TARGET_X86

#ifndef TARGET_UNIX
#define WORKAROUND_RACES_WITH_KERNEL_MODE_EXCEPTION_HANDLING
#endif // !TARGET_UNIX

#ifdef WORKAROUND_RACES_WITH_KERNEL_MODE_EXCEPTION_HANDLING
BOOL ThreadCaughtInKernelModeExceptionHandling(Thread *pThread, CONTEXT *ctx)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pThread != NULL);
        PRECONDITION(ctx != NULL);
    }
    CONTRACTL_END;

    // Validate that Esp plus all of our maximum structure sizes is on the thread's stack. We use the cached bounds
    // on the Thread object. If we're that close to the top of the thread's stack, then we can't possibly have hit
    // the race. If we pass this test, we can assume all memory accesses below are legal, since they're all on the
    // thread's stack.
    if ((ctx->Esp + sizeof(EXCEPTION_POINTERS) + sizeof(EXCEPTION_RECORD) + sizeof(CONTEXT)) >=
        (UINT_PTR)pThread->GetCachedStackBase())
    {
        return FALSE;
    }

    // The calculations below assume that a DWORD is the same size as a pointer. Since this is only needed on
    // 32-bit platforms, this should be fine.
    _ASSERTE(sizeof(DWORD) == sizeof(void*));

    // There are cases where the ESP is just decremented but the page is not touched, thus the page is not committed or
    // still has page guard bit set. We can't hit the race in such case so we just leave. Besides, we can't access the
    // memory with page guard flag or not committed.
    MEMORY_BASIC_INFORMATION mbi;
    if (VirtualQuery((LPCVOID)(UINT_PTR)ctx->Esp, &mbi, sizeof(mbi)) == sizeof(mbi))
    {
        if (!(mbi.State & MEM_COMMIT) || (mbi.Protect & PAGE_GUARD))
            return FALSE;
    }
    else
    {
        STRESS_LOG0 (LF_SYNC, ERROR, "VirtualQuery failed!");
    }

    // The first two values on the stack should be a pointer to the EXCEPTION_RECORD and a pointer to the CONTEXT.
    UINT_PTR Esp = (UINT_PTR)ctx->Esp;
    UINT_PTR ER = *((UINT_PTR*)Esp);
    UINT_PTR CTX = *((UINT_PTR*)(Esp + sizeof(EXCEPTION_RECORD*)));

    // The EXCEPTION_RECORD should be at Esp + sizeof(EXCEPTION_POINTERS)... if it's not, then we haven't hit the race.
    if (ER != (Esp + sizeof(EXCEPTION_POINTERS)))
    {
        return FALSE;
    }

    // Assume we have an EXCEPTION_RECORD at Esp + sizeof(EXCEPTION_POINTERS) and look at values within that.
    EXCEPTION_RECORD *pER = (EXCEPTION_RECORD*)ER;

    // Make sure the faulting address in the EXCEPTION_RECORD matches the thread's current Eip.
    if ((UINT_PTR)pER->ExceptionAddress != ctx->Eip)
    {
        return FALSE;
    }

    // Validate the number of exception parameters.
    if ((pER->NumberParameters > EXCEPTION_MAXIMUM_PARAMETERS))
    {
        return FALSE;
    }

    // We have a plausable number of exception parameters, so compute the exact size of this exception
    // record. Remember, an EXCEPTION_RECORD has a variable sized array of optional information at the end called
    // the ExceptionInformation. It's an array of pointers up to EXCEPTION_MAXIMUM_PARAMETERS in length.
    DWORD exceptionRecordSize = sizeof(EXCEPTION_RECORD) -
        ((EXCEPTION_MAXIMUM_PARAMETERS - pER->NumberParameters) * sizeof(pER->ExceptionInformation[0]));

    // On Vista WOW on X64, the OS pushes the maximum number of parameters onto the stack.
    DWORD exceptionRecordMaxSize = sizeof(EXCEPTION_RECORD);

    // The CONTEXT pointer should be pointing right after the EXCEPTION_RECORD.
    if ((CTX != (ER + exceptionRecordSize)) &&
        (CTX != (ER + exceptionRecordMaxSize)))
    {
        return FALSE;
    }

    // Assume we have a CONTEXT at Esp + 8 + exceptionRecordSize and look at values within that.
    CONTEXT *pCTX = (CONTEXT*)CTX;

    // Make sure the Eip in the CONTEXT on the stack matches the current Eip value.
    if (pCTX->Eip != ctx->Eip)
    {
        return FALSE;
    }

    // If all the tests above fail, then it means that we've hit race #2 described in the text before this function.
    STRESS_LOG3(LF_SYNC, LL_INFO100,
                "ThreadCaughtInKernelModeExceptionHandling returning TRUE. Eip=%p, Esp=%p, ExceptionCode=%p\n",
                ctx->Eip, ctx->Esp, pER->ExceptionCode);

    return TRUE;
}
#endif //WORKAROUND_RACES_WITH_KERNEL_MODE_EXCEPTION_HANDLING
#endif //TARGET_X86

//---------------------------------------------------------------------------------------
//
// Helper used by HandledJITCase and others (like the profiling API) who need an
// absolutely reliable register context.
//
// Arguments:
//     * dwOptions - [in] Combination of flags from enum
//         GetSafelyRedirectableThreadContextOptions to customize the checks performed by
//         this function.
//     * pCtx - [out] This Thread's current context. Callers may rely on this only if nonzero
//         is returned
//     * pRD - [out] Matching REGDISPLAY filled from the pCtx found by this function.
//         Callers may rely on this only if nonzero is returned
//
// Return Value:
//      Nonzero iff all requested checks have succeeded, which would imply that it is
//      a reliable time to use this Thread's context.
//
BOOL Thread::GetSafelyRedirectableThreadContext(DWORD dwOptions, CONTEXT * pCtx, REGDISPLAY * pRD)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(pCtx != NULL);
    _ASSERTE(pRD != NULL);

    // We are never in interruptible code if there if a filter context put in place by the debugger.
    if (GetFilterContext() != NULL)
        return FALSE;

#ifdef DEBUGGING_SUPPORTED
    if ((dwOptions & kCheckDebuggerBreakpoints) != 0)
    {
        // If we are running under the control of a managed debugger that may have placed breakpoints in the code stream,
        // then there is a special case that we need to check. See the comments in debugger.cpp for more information.
        if (CORDebuggerAttached() && (g_pDebugInterface->IsThreadContextInvalid(this, NULL)))
            return FALSE;
    }
#endif // DEBUGGING_SUPPORTED

    _ASSERTE(GetFilterContext() == NULL);
    ZeroMemory(pCtx, sizeof(*pCtx));

    // Make sure we specify CONTEXT_EXCEPTION_REQUEST to detect "trap frame reporting".
    pCtx->ContextFlags = CONTEXT_FULL | CONTEXT_EXCEPTION_REQUEST;
    if (!EEGetThreadContext(this, pCtx))
    {
        return FALSE;
    }

    //
    // workaround around WOW64 problems.  Only do this workaround if a) this is x86, and b) the OS does not support trap frame reporting,
    // If the OS *does* support trap frame reporting, then the call to IsContextSafeToRedirect below will return FALSE if we run
    // into this race.
    //
#ifdef TARGET_X86
    if (!(pCtx->ContextFlags & CONTEXT_EXCEPTION_REPORTING) &&
        ((dwOptions & kPerfomLastRedirectIPCheck) != 0))
    {
        // This code fixes a race between GetThreadContext and NtContinue.  If we redirect managed code
        // at the same place twice in a row, we run the risk of reading a bogus CONTEXT when we redirect
        // the second time.  This leads to access violations on x86 machines.  To fix the problem, we
        // never redirect at the same instruction pointer that we redirected at on the previous GC.
        if (GetIP(pCtx) == m_LastRedirectIP)
        {
            // We need to test for an infinite loop in assembly, as this will break the heuristic we
            // are using.
            const BYTE short_jmp = 0xeb;    // Machine code for a short jump.
            const BYTE self = 0xfe;         // -2.  Short jumps are calculated as [ip]+2+[second_byte].

            // If we find that we are in an infinite loop, we'll set the last redirected IP to 0 so that we will
            // redirect the next time we attempt it.  Delaying one interation allows us to narrow the window of
            // the race we are working around in this corner case.
            BYTE *ip = (BYTE *)m_LastRedirectIP;
            if (ip[0] == short_jmp && ip[1] == self)
                m_LastRedirectIP = 0;

            // We set a hard limit of 5 times we will spin on this to avoid any tricky race which we have not
            // accounted for.
            m_SpinCount++;
            if (m_SpinCount >= 5)
                m_LastRedirectIP = 0;

            STRESS_LOG0(LF_GC, LL_INFO10000, "GetSafelyRedirectableThreadContext() - Cannot redirect at the same IP as the last redirection.\n");
            return FALSE;
        }
    }
#endif

    if (!IsContextSafeToRedirect(pCtx))
    {
        STRESS_LOG0(LF_GC, LL_INFO10000, "GetSafelyRedirectableThreadContext() - trap frame reporting an invalid CONTEXT\n");
        return FALSE;
    }

    ZeroMemory(pRD, sizeof(*pRD));
    if (!InitRegDisplay(pRD, pCtx, true))
        return FALSE;

    return TRUE;
}

// Called while the thread is suspended.  If we aren't in JITted code, this isn't
// a JITCase and we return FALSE.  If it is a JIT case and we are in interruptible
// code, then we are handled.  Our caller has found a good spot and can keep us
// suspended.  If we aren't in interruptible code, then we aren't handled.  So we
// pick a spot to hijack the return address and our caller will wait to get us
// somewhere safe.
BOOL Thread::HandledJITCase()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL            ret = FALSE;
    ExecutionState  esb;
    StackWalkAction action;

    CONTEXT ctx;
    REGDISPLAY rd;
    if (!GetSafelyRedirectableThreadContext(
        kPerfomLastRedirectIPCheck | kCheckDebuggerBreakpoints,
        &ctx,
        &rd))
    {
        STRESS_LOG0(LF_GC, LL_INFO10000, "HandledJITCase() - GetSafelyRedirectableThreadContext() returned FALSE\n");
        return FALSE;
    }

    PCODE ip = GetIP(&ctx);
    if (!ExecutionManager::IsManagedCode(ip))
    {
        return FALSE;
    }

#ifdef WORKAROUND_RACES_WITH_KERNEL_MODE_EXCEPTION_HANDLING
    if (ThreadCaughtInKernelModeExceptionHandling(this, &ctx))
    {
        return FALSE;
    }
#endif //WORKAROUND_RACES_WITH_KERNEL_MODE_EXCEPTION_HANDLING

#ifdef _DEBUG
    // We know IP is in managed code, mark current thread as safe for calls into host
    Thread * pCurThread = GetThreadNULLOk();
    if (pCurThread != NULL)
    {
        pCurThread->dbg_m_cSuspendedThreadsWithoutOSLock ++;
        _ASSERTE(pCurThread->dbg_m_cSuspendedThreadsWithoutOSLock <= pCurThread->dbg_m_cSuspendedThreads);
    }
#endif //_DEBUG

#ifdef TIME_SUSPEND
    DWORD startCrawl = g_SuspendStatistics.GetTime();
#endif
    action = StackWalkFramesEx(&rd,SWCB_GetExecutionState, &esb,
                                QUICKUNWIND | DISABLE_MISSING_FRAME_DETECTION |
                                THREAD_IS_SUSPENDED | ALLOW_ASYNC_STACK_WALK, NULL);

#ifdef TIME_SUSPEND
    g_SuspendStatistics.crawl.Accumulate(
            SuspendStatistics::GetElapsed(startCrawl,
                                            g_SuspendStatistics.GetTime()));

    g_SuspendStatistics.cntHijackCrawl++;
#endif

    //
    // action should either be SWA_ABORT, in which case we properly walked
    // the stack frame and found out whether this is a JIT case, or
    // SWA_FAILED, in which case the walk couldn't even be started because
    // there are no stack frames, which also isn't a JIT case.
    //
    if (action == SWA_ABORT && esb.m_IsJIT)
    {
        // If we are interruptible and we are in cooperative mode, our caller can
        // just leave us suspended.
        if (esb.m_IsInterruptible && m_fPreemptiveGCDisabled)
        {
            _ASSERTE(!ThreadStore::HoldingThreadStore(this));
            ret = TRUE;
        }
        else
        if (esb.m_ppvRetAddrPtr)
        {
            // we need to hijack the return address.  Base this on whether or not
            // the method returns an object reference, so we know whether to protect
            // it or not.
            EECodeInfo codeInfo(ip);

            ReturnKind returnKind;

            if (GetReturnAddressHijackInfo(&codeInfo, &returnKind))
            {
                HijackThread(returnKind, &esb);
            }
        }
    }
    // else it's not even a JIT case

#ifdef _DEBUG
    // Restore back the number of threads without OS lock
    if (pCurThread != NULL)
    {
        pCurThread->dbg_m_cSuspendedThreadsWithoutOSLock--;
    }
#endif //_DEBUG

    STRESS_LOG1(LF_SYNC, LL_INFO10000, "    HandledJitCase returning %d\n", ret);
    return ret;
}

#endif // !TARGET_UNIX

#endif // FEATURE_HIJACK

// Some simple helpers to keep track of the threads we are waiting for
void Thread::MarkForSuspension(ULONG bit)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // CoreCLR does not support user-requested thread suspension
    _ASSERTE(bit == TS_DebugSuspendPending ||
             bit == (TS_DebugSuspendPending | TS_DebugWillSync));

    _ASSERTE(IsAtProcessExit() || ThreadStore::HoldingThreadStore());

    _ASSERTE((m_State & bit) == 0);

    InterlockedOr((LONG*)&m_State, bit);
    ThreadStore::TrapReturningThreads(TRUE);
}

void Thread::UnmarkForSuspension(ULONG mask)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // CoreCLR does not support user-requested thread suspension
    _ASSERTE(mask == ~TS_DebugSuspendPending);

    _ASSERTE(IsAtProcessExit() || ThreadStore::HoldingThreadStore());

    _ASSERTE((m_State & ~mask) != 0);

    // we decrement the global first to be able to satisfy the assert from DbgFindThread
    ThreadStore::TrapReturningThreads(FALSE);
    InterlockedAnd((LONG*)&m_State, mask);
}

//----------------------------------------------------------------------------

void ThreadSuspend::RestartEE(BOOL bFinishedGC, BOOL SuspendSucceeded)
{
    ThreadSuspend::s_fSuspended = false;
#ifdef TIME_SUSPEND
    g_SuspendStatistics.StartRestart();
#endif //TIME_SUSPEND

    FireEtwGCRestartEEBegin_V1(GetClrInstanceId());

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // Flush the store buffers on all CPUs, to ensure that they all see changes made
    // by the GC threads. This only matters on weak memory ordered processors as
    // the strong memory ordered processors wouldn't have reordered the relevant reads.
    // This is needed to synchronize threads that were running in preemptive mode while
    // the runtime was suspended and that will return to cooperative mode after the runtime
    // is restarted.
    ::FlushProcessWriteBuffers();
#endif //TARGET_ARM || TARGET_ARM64

    //
    // SyncClean holds a list of things to be cleaned up when it's possible.
    // SyncClean uses the GC mode to synchronize access to this list.  Threads must be
    // in COOP mode to add things to the list, and the list can only be cleaned up
    // while no threads are adding things.
    // Since we know that no threads are in COOP mode at this point (because the EE is
    // suspended), we clean up the list here.
    //
    SyncClean::CleanUp();

#ifdef PROFILING_SUPPORTED
    // If a profiler is keeping track suspend events, notify it.  This notification
    // must happen before we set TrapReturning threads to FALSE because as soon as
    // we remove the return trap threads can start "running" managed code again as
    // they return from unmanaged.  (Whidbey Bug #7505)
    // Also must notify before setting GcInProgress = FALSE.
    //
    // It's very odd that we do this here, in ThreadSuspend::RestartEE, while the
    // corresponding call to RuntimeSuspendStarted is done at a lower architectural layer,
    // in ThreadSuspend::SuspendRuntime.
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackSuspends());
        (&g_profControlBlock)->RuntimeResumeStarted();
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    //
    // Unhijack all threads, and reset their "suspend pending" flags.  Why isn't this in
    // Thread::ResumeRuntime?
    //
    Thread  *thread = NULL;
    while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
    {
        thread->PrepareForEERestart(SuspendSucceeded);
    }

    //
    // Revert to being a normal thread
    //
    ClrFlsClearThreadType (ThreadType_DynamicSuspendEE);
    GCHeapUtilities::GetGCHeap()->SetGCInProgress(false);

    //
    // Allow threads to enter COOP mode (though we still need to wake the ones
    // that we hijacked).
    //
    // Note: this is the last barrier that keeps managed threads
    // from entering cooperative mode. If the sequence changes,
    // you may have to change routine GCHeapUtilities::SafeToRestartManagedThreads
    // as well.
    //
    ThreadStore::TrapReturningThreads(FALSE);
    g_pSuspensionThread    = 0;

    //
    // Any threads that are waiting in WaitUntilGCComplete will continue now.
    //
    GCHeapUtilities::GetGCHeap()->SetWaitForGCEvent();
    _ASSERTE(IsGCSpecialThread() || ThreadStore::HoldingThreadStore());

    ResumeRuntime(bFinishedGC, SuspendSucceeded);

    FireEtwGCRestartEEEnd_V1(GetClrInstanceId());

#ifdef TIME_SUSPEND
    g_SuspendStatistics.EndRestart();
#endif //TIME_SUSPEND
}

// The contract between GC and the EE, for starting and finishing a GC is as follows:
//
//  SuspendEE:
//      LockThreadStore
//      SetGCInProgress
//      SuspendRuntime
//
//      ... perform the GC ...
//
// RestartEE:
//      SetGCDone
//      ResumeRuntime
//         calls UnlockThreadStore
//
// Note that this is intentionally *not* symmetrical.  The EE will assert that the
// GC does most of this stuff in the correct sequence.

//
// This is the only way to call ThreadSuspend::SuspendRuntime, and that method is
// so tightly coupled to this one, with intermingled responsibilities, that we don't
// understand why we have a separation at all.  At some point we should refactor all of
// the suspension code into a separate abstraction, which we would like to call the
// "managed execution lock."  The current "layering" of this stuff has it mixed
// randomly into the Thread and GC code, and split into almost completely arbitrary
// layers.
//
void ThreadSuspend::SuspendEE(SUSPEND_REASON reason)
{
#ifdef TIME_SUSPEND
    g_SuspendStatistics.StartSuspend();
#endif //TIME_SUSPEND

    BOOL gcOnTransitions;

    ETW::GCLog::ETW_GC_INFO Info;
    Info.SuspendEE.Reason = reason;
    Info.SuspendEE.GcCount = (((reason == SUSPEND_FOR_GC) || (reason == SUSPEND_FOR_GC_PREP)) ?
        (ULONG)GCHeapUtilities::GetGCHeap()->GetGcCount() : (ULONG)-1);

    FireEtwGCSuspendEEBegin_V1(Info.SuspendEE.Reason, Info.SuspendEE.GcCount, GetClrInstanceId());

    LOG((LF_SYNC, INFO3, "Suspending the runtime for reason %d\n", reason));

    gcOnTransitions = GC_ON_TRANSITIONS(FALSE);        // dont do GC for GCStress 3

    Thread* pCurThread = GetThreadNULLOk();

    DWORD dwSwitchCount = 0;

retry_for_debugger:

#ifdef TIME_SUSPEND
    DWORD startAcquire = g_SuspendStatistics.GetTime();
#endif

    //
    // Acquire the TSL.  We will hold this until the we restart the EE.
    //
    ThreadSuspend::LockThreadStore(reason);

#ifdef TIME_SUSPEND
    g_SuspendStatistics.acquireTSL.Accumulate(SuspendStatistics::GetElapsed(startAcquire,
        g_SuspendStatistics.GetTime()));
#endif

    //
    // Now we're going to acquire an exclusive lock on managed code execution (including
    // "manually managed" code in GCX_COOP regions).
    //
    // First, we reset the event that we're about to tell other threads to wait for.
    //
    GCHeapUtilities::GetGCHeap()->ResetWaitForGCEvent();

    //
    // Remember that we're the one suspending the EE
    //
    g_pSuspensionThread = pCurThread;

    //
    // Tell all threads, globally, to wait for WaitForGCEvent.
    //
    ThreadStore::TrapReturningThreads(TRUE);

    //
    // Remember why we're doing this.
    //
    m_suspendReason = reason;

    //
    // There's a GC in progress.  (again, not necessarily - we suspend the EE for other reasons.
    // I wonder how much confusion this has caused....)
    // It seems like much of the above is redundant. We should investigate reducing the number
    // of mechanisms we use to indicate that a suspension is in progress.
    GCHeapUtilities::GetGCHeap()->SetGCInProgress(true);

    // set tls flags for compat with SOS
    ClrFlsSetThreadType(ThreadType_DynamicSuspendEE);

    _ASSERTE(ThreadStore::HoldingThreadStore() || g_fProcessDetach);

    //
    // Now that we've instructed all threads to please stop,
    // go interrupt the ones that are running managed code and force them to stop.
    // This does not return until all threads have acknowledged that they
    // will not run managed code.
    //
    SuspendRuntime(reason);

#ifdef DEBUGGING_SUPPORTED
    // If the debugging services are attached, then its possible
    // that there is a thread which appears to be stopped at a gc
    // safe point, but which really is not. If that is the case,
    // back off and try again.
    if ((CORDebuggerAttached() && g_pDebugInterface->ThreadsAtUnsafePlaces()))
    {
        // In this case, the debugger has stopped at least one
        // thread at an unsafe place.  The debugger will usually
        // have already requested that we stop.  If not, it will
        // usually either do so shortly, or resume the thread that is
        // at the unsafe place. Either way, we have to wait for the
        // debugger to decide what it wants to do.
        //
        // In some rare cases, the end-user debugger may have frozen
        // a thread at a gc-unsafe place, and so we'll loop forever
        // here and never resolve the deadlock.  Unfortunately we can't
        // easily abort a GC
        // and so for now we just wait for the debugger to timeout and
        // hopefully thaw that thread.  Maybe instead we should try to
        // detect this situation sooner (when thread abort is possible)
        // and notify the debugger with NotifyOfCrossThreadDependency, giving
        // it the chance to thaw other threads or abort us before getting
        // wedged in the GC.
        //
        // Note: we've still got the ThreadStore lock held.
        //
        LOG((LF_GCROOTS | LF_GC | LF_CORDB,
            LL_INFO10,
            "***** Giving up on current GC suspension due to debugger *****\n"));

        // Mark that we're done with the gc, so that the debugger can proceed.
        RestartEE(FALSE, FALSE);

        LOG((LF_GCROOTS | LF_GC | LF_CORDB, LL_INFO10, "The EE is free now...\n"));

        // If someone's trying to suspent *this* thread, this is a good opportunity.
        if (pCurThread && pCurThread->CatchAtSafePointOpportunistic())
        {
            pCurThread->PulseGCMode();  // Go suspend myself.
        }
        else
        {
            // otherwise, just yield so the debugger can finish what it's doing.
            __SwitchToThread(0, ++dwSwitchCount);
        }

        goto retry_for_debugger;
    }
#endif // DEBUGGING_SUPPORTED

    GC_ON_TRANSITIONS(gcOnTransitions);

    FireEtwGCSuspendEEEnd_V1(GetClrInstanceId());

#ifdef TIME_SUSPEND
    g_SuspendStatistics.EndSuspend(reason == SUSPEND_FOR_GC || reason == SUSPEND_FOR_GC_PREP);
#endif //TIME_SUSPEND
    ThreadSuspend::s_fSuspended = true;

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // Flush the store buffers on all CPUs, to ensure that all changes made so far are seen
    // by the GC threads. This only matters on weak memory ordered processors as
    // the strong memory ordered processors wouldn't have reordered the relevant writes.
    // This is needed to synchronize threads that were running in preemptive mode thus were
    // left alone by suspension to flush their writes that they made before they switched to
    // preemptive mode.
    ::FlushProcessWriteBuffers();
#endif //TARGET_ARM || TARGET_ARM64
}

#ifdef FEATURE_THREAD_ACTIVATION

// This function is called by a thread activation to check if the specified instruction pointer
// is in a function where we can safely handle an activation.
BOOL CheckActivationSafePoint(SIZE_T ip, BOOL checkingCurrentThread)
{
    Thread *pThread = GetThreadNULLOk();
    // It is safe to call the ExecutionManager::IsManagedCode only if we are making the check for
    // a thread different from the current one or if the current thread is in the cooperative mode.
    // Otherwise ExecutionManager::IsManagedCode could deadlock if the activation happened when the
    // thread was holding the ExecutionManager's writer lock.
    // When the thread is in preemptive mode, we know for sure that it is not executing managed code.
    BOOL checkForManagedCode = !checkingCurrentThread || (pThread != NULL && pThread->PreemptiveGCDisabled());
    return checkForManagedCode && ExecutionManager::IsManagedCode(ip);
}

// This function is called when thread suspension is pending. It tries to ensure that the current
// thread is taken to a GC-safe place as quickly as possible. It does this by doing
// one of the following:
//
//     - If the thread is in native code or preemptive GC is not disabled, there's
//       nothing to do, so we return.
//
//     - If the thread is in interruptible managed code, we will push a frame that
//       has information about the context that was interrupted and then switch to
//       preemptive GC mode so that the pending GC can proceed, and then switch back.
//
//     - If the thread is in uninterruptible managed code, we will patch the return
//       address to take the thread to the appropriate stub (based on the return
//       type of the method) which will then handle preparing the thread for GC.
//
void HandleSuspensionForInterruptedThread(CONTEXT *interruptedContext)
{
    Thread *pThread = GetThread();

    if (pThread->PreemptiveGCDisabled() != TRUE)
        return;

    PCODE ip = GetIP(interruptedContext);

    // This function can only be called when the interrupted thread is in
    // an activation safe point.
    _ASSERTE(CheckActivationSafePoint(ip, /* checkingCurrentThread */ TRUE));

    Thread::WorkingOnThreadContextHolder workingOnThreadContext(pThread);
    if (!workingOnThreadContext.Acquired())
        return;

#if defined(DEBUGGING_SUPPORTED) && defined(TARGET_WINDOWS)
    // If we are running under the control of a managed debugger that may have placed breakpoints in the code stream,
    // then there is a special case that we need to check. See the comments in debugger.cpp for more information.
    if (CORDebuggerAttached() && g_pDebugInterface->IsThreadContextInvalid(pThread, interruptedContext))
        return;
#endif // DEBUGGING_SUPPORTED && TARGET_WINDOWS

    EECodeInfo codeInfo(ip);
    if (!codeInfo.IsValid())
        return;

    DWORD addrOffset = codeInfo.GetRelOffset();

    ICodeManager *pEECM = codeInfo.GetCodeManager();
    _ASSERTE(pEECM != NULL);

    bool isAtSafePoint = pEECM->IsGcSafe(&codeInfo, addrOffset);
    if (isAtSafePoint)
    {
        // If the thread is at a GC safe point, push a RedirectedThreadFrame with
        // the interrupted context and pulse the GC mode so that GC can proceed.
        FrameWithCookie<RedirectedThreadFrame> frame(interruptedContext);

        frame.Push(pThread);

        pThread->PulseGCMode();

        INSTALL_MANAGED_EXCEPTION_DISPATCHER;
        INSTALL_UNWIND_AND_CONTINUE_HANDLER;

        pThread->HandleThreadAbort();

        UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

        frame.Pop(pThread);
    }
    else
    {
        // The thread is in non-interruptible code.
        ExecutionState executionState;
        StackWalkAction action;
        REGDISPLAY regDisplay;
        pThread->InitRegDisplay(&regDisplay, interruptedContext, true /* validContext */);

        BOOL unused;

        if (IsIPInEpilog(interruptedContext, &codeInfo, &unused))
            return;

        // Use StackWalkFramesEx to find the location of the return address. This will locate the
        // return address by checking relative to the caller frame's SP, which is preferable to
        // checking next to the current RBP because we may have interrupted the function prior to
        // the point where RBP is updated.
        action = pThread->StackWalkFramesEx(
            &regDisplay,
            SWCB_GetExecutionState,
            &executionState,
            QUICKUNWIND | DISABLE_MISSING_FRAME_DETECTION | ALLOW_ASYNC_STACK_WALK);

        if (action != SWA_ABORT || !executionState.m_IsJIT)
            return;

        if (executionState.m_ppvRetAddrPtr == NULL)
            return;

        ReturnKind returnKind;

        if (!GetReturnAddressHijackInfo(&codeInfo, &returnKind))
        {
            return;
        }

        // Calling this turns off the GC_TRIGGERS/THROWS/INJECT_FAULT contract in LoadTypeHandle.
        // We should not trigger any loads for unresolved types.
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

        // Mark that we are performing a stackwalker like operation on the current thread.
        // This is necessary to allow the signature parsing functions to work without triggering any loads.
        StackWalkerWalkingThreadHolder threadStackWalking(pThread);

        // Hijack the return address to point to the appropriate routine based on the method's return type.
        pThread->HijackThread(returnKind, &executionState);
    }
}

#ifdef FEATURE_SPECIAL_USER_MODE_APC
void Thread::ApcActivationCallback(ULONG_PTR Parameter)
{
    // Cannot use contracts here because the thread may be interrupted at any point

    _ASSERTE(UseSpecialUserModeApc());
    _ASSERTE(Parameter != 0);

    CLONE_PAPC_CALLBACK_DATA pData = (CLONE_PAPC_CALLBACK_DATA)Parameter;
    ActivationReason reason = (ActivationReason)pData->Parameter;
    PCONTEXT pContext = pData->ContextRecord;

    struct AutoClearPendingThreadActivation
    {
        ~AutoClearPendingThreadActivation()
        {
            GetThread()->m_hasPendingActivation = false;
        }
    } autoClearPendingThreadActivation;

    if (!CheckActivationSafePoint(GetIP(pContext), true /* checkingCurrentThread */))
    {
        return;
    }

    switch (reason)
    {
        case ActivationReason::SuspendForGC:
        case ActivationReason::SuspendForDebugger:
        case ActivationReason::ThreadAbort:
            HandleSuspensionForInterruptedThread(pContext);
            break;

        default:
            UNREACHABLE_MSG("Unexpected ActivationReason");
    }
}
#endif // FEATURE_SPECIAL_USER_MODE_APC

bool Thread::InjectActivation(ActivationReason reason)
{
#ifdef FEATURE_SPECIAL_USER_MODE_APC
    _ASSERTE(UseSpecialUserModeApc());

    if (m_hasPendingActivation)
    {
        // Try to avoid nesting special user-mode APCs, as they can interrupt one another
        return true;
    }

    HANDLE hThread = GetThreadHandle();
    if (hThread == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    m_hasPendingActivation = true;
    BOOL success =
        (*s_pfnQueueUserAPC2Proc)(
            GetRedirectHandlerForApcActivation(),
            hThread,
            (ULONG_PTR)reason,
            SpecialUserModeApcWithContextFlags);
    _ASSERTE(success);
    return true;
#elif defined(TARGET_UNIX)
    _ASSERTE((reason == ActivationReason::SuspendForGC) || (reason == ActivationReason::ThreadAbort));

    static ConfigDWORD injectionEnabled;
    if (injectionEnabled.val(CLRConfig::INTERNAL_ThreadSuspendInjection) == 0)
        return false;

    HANDLE hThread = GetThreadHandle();
    if (hThread != INVALID_HANDLE_VALUE)
        return ::PAL_InjectActivation(hThread);

    return false;
#else
#error Unknown platform.
#endif // FEATURE_SPECIAL_USER_MODE_APC || TARGET_UNIX
}

#endif // FEATURE_THREAD_ACTIVATION

// Initialize thread suspension support
void ThreadSuspend::Initialize()
{
#ifdef FEATURE_HIJACK
#if defined(TARGET_UNIX)
    ::PAL_SetActivationFunction(HandleSuspensionForInterruptedThread, CheckActivationSafePoint);
#elif defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
    // Only versions of Windows that have the special user mode APC have a correct implementation of the return address hijack handling
    if (Thread::UseSpecialUserModeApc())
    {
        HMODULE hModNtdll = WszLoadLibrary(W("ntdll.dll"));
        if (hModNtdll != NULL)
        {
            typedef ULONG_PTR (NTAPI *PFN_RtlGetReturnAddressHijackTarget)();
            PFN_RtlGetReturnAddressHijackTarget pfnRtlGetReturnAddressHijackTarget = (PFN_RtlGetReturnAddressHijackTarget)GetProcAddress(hModNtdll, "RtlGetReturnAddressHijackTarget");
            if (pfnRtlGetReturnAddressHijackTarget != NULL)
            {
                g_returnAddressHijackTarget = (void*)pfnRtlGetReturnAddressHijackTarget();
            }
        }
    }
#endif // TARGET_WINDOWS && TARGET_AMD64
#endif // FEATURE_HIJACK
}

#ifdef _DEBUG
BOOL Debug_IsLockedViaThreadSuspension()
{
    LIMITED_METHOD_CONTRACT;
    return GCHeapUtilities::IsGCInProgress() &&
                    (dbgOnly_IsSpecialEEThread() ||
                    IsGCSpecialThread() ||
                    GetThreadNULLOk() == ThreadSuspend::GetSuspensionThread());
}
#endif

#if defined(TIME_SUSPEND) || defined(GC_STATS)

DWORD StatisticsBase::secondsToDisplay = 0;

DWORD StatisticsBase::GetTime()
{
    LIMITED_METHOD_CONTRACT;
    LARGE_INTEGER large;

    if (divisor == 0)
    {
        if (QueryPerformanceFrequency(&large) && (large.QuadPart != 0))
            divisor = (DWORD)(large.QuadPart / (1000 * 1000));        // microseconds
        else
            divisor = 1;
    }

    if (QueryPerformanceCounter(&large))
        return (DWORD) (large.QuadPart / divisor);
    else
        return 0;
}

DWORD StatisticsBase::GetElapsed(DWORD start, DWORD stop)
{
    LIMITED_METHOD_CONTRACT;
    if (stop > start)
        return stop - start;

    INT64 bigStop = stop;
    bigStop += 0x100000000ULL;
    bigStop -= start;

    // The assert below was seen firing in stress, so comment it out for now
    //_ASSERTE(((INT64)(DWORD)bigStop) == bigStop);

    if (((INT64)(DWORD)bigStop) == bigStop)
        return (DWORD) bigStop;
    else
        return 0;
}

void StatisticsBase::RollOverIfNeeded()
{
    LIMITED_METHOD_CONTRACT;

    // Our counters are 32 bits and can count to 4 GB in microseconds or 4K in seconds.
    // Reset when we get close to overflowing
    const DWORD RolloverInterval = 3900;

    // every so often, print a summary of our statistics
    DWORD ticksNow = GetTickCount();

    if (secondsToDisplay == 0)
    {
        secondsToDisplay = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_StatsUpdatePeriod);
        if (secondsToDisplay == 0)
            secondsToDisplay = 1;
        else if (secondsToDisplay > RolloverInterval)
            secondsToDisplay = RolloverInterval;
    }

    if (ticksNow - startTick > secondsToDisplay * 1000)
    {
        DisplayAndUpdate();

        startTick = GetTickCount();

        // Our counters are 32 bits and can count to 4 GB in microseconds or 4K in seconds.
        // Reset when we get close to overflowing
        if (++cntDisplay >= (int)(RolloverInterval / secondsToDisplay))
            Initialize();
    }
}

#endif // defined(TIME_SUSPEND) || defined(GC_STATS)


#ifdef TIME_SUSPEND

// There is a current and a prior copy of the statistics.  This allows us to display deltas per reporting
// interval, as well as running totals.  The 'min' and 'max' values require special treatment.  They are
// Reset (zeroed) in the current statistics when we begin a new interval and they are updated via a
// comparison with the global min/max.
SuspendStatistics g_SuspendStatistics;
SuspendStatistics g_LastSuspendStatistics;

WCHAR* SuspendStatistics::logFileName = NULL;

// Called whenever our timers start to overflow
void SuspendStatistics::Initialize()
{
    LIMITED_METHOD_CONTRACT;
    // for efficiency sake we're taking a dependency on the layout of a C++ object
    // with a vtable. protect against violations of our premise:
    static_assert(offsetof(SuspendStatistics, cntDisplay) == sizeof(void*),
            "The first field of SuspendStatistics follows the pointer sized vtable");

    int podOffs = offsetof(SuspendStatistics, cntDisplay);  // offset of the first POD field
    memset((BYTE*)(&g_SuspendStatistics)+podOffs, 0, sizeof(g_SuspendStatistics)-podOffs);
    memset((BYTE*)(&g_LastSuspendStatistics)+podOffs, 0, sizeof(g_LastSuspendStatistics)-podOffs);
}

// Top of SuspendEE
void SuspendStatistics::StartSuspend()
{
    LIMITED_METHOD_CONTRACT;
    startSuspend = GetTime();
}

// Bottom of SuspendEE
void SuspendStatistics::EndSuspend(BOOL bForGC)
{
    LIMITED_METHOD_CONTRACT;
    DWORD time = GetElapsed(startSuspend, GetTime());

    suspend.Accumulate(time);
    cntSuspends++;
    // details on suspends...
    if (!bForGC)
        cntNonGCSuspends++;
    if (GCHeapUtilities::GetGCHeap()->IsConcurrentGCInProgress())
    {
        cntSuspendsInBGC++;
        if (!bForGC)
            cntNonGCSuspendsInBGC++;
    }
}

// Time spent in the current suspend (for pro-active debugging)
DWORD SuspendStatistics::CurrentSuspend()
{
    LIMITED_METHOD_CONTRACT;
    return GetElapsed(startSuspend, GetTime());
}

// Top of RestartEE
void SuspendStatistics::StartRestart()
{
    LIMITED_METHOD_CONTRACT;
    startRestart = GetTime();
}

// Bottom of RestartEE
void SuspendStatistics::EndRestart()
{
    LIMITED_METHOD_CONTRACT;
    DWORD timeNow = GetTime();

    restart.Accumulate(GetElapsed(startRestart, timeNow));
    cntRestarts++;

    paused.Accumulate(SuspendStatistics::GetElapsed(startSuspend, timeNow));

    RollOverIfNeeded();
}

// Time spent in the current restart
DWORD SuspendStatistics::CurrentRestart()
{
    LIMITED_METHOD_CONTRACT;
    return GetElapsed(startRestart, GetTime());
}

void SuspendStatistics::DisplayAndUpdate()
{
    LIMITED_METHOD_CONTRACT;

    // TODO: this fires at times...
    // _ASSERTE(cntSuspends == cntRestarts);

    if (logFileName == NULL)
    {
        logFileName = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_SuspendTimeLog);
    }

    FILE* logFile;

    if (logFileName != NULL && (logFile = _wfopen((LPCWSTR)logFileName, W("a"))) != NULL)
    {
    if (cntDisplay == 0)
        fprintf(logFile, "\nSUSP **** Initialize *****\n\n");

    fprintf(logFile, "SUSP **** Summary ***** %d\n", cntDisplay);

    paused.DisplayAndUpdate    (logFile, "Paused ", &g_LastSuspendStatistics.paused,     cntSuspends, g_LastSuspendStatistics.cntSuspends);
    suspend.DisplayAndUpdate   (logFile, "Suspend", &g_LastSuspendStatistics.suspend,    cntSuspends, g_LastSuspendStatistics.cntSuspends);
    restart.DisplayAndUpdate   (logFile, "Restart", &g_LastSuspendStatistics.restart,    cntRestarts, g_LastSuspendStatistics.cntSuspends);
    acquireTSL.DisplayAndUpdate(logFile, "LockTSL", &g_LastSuspendStatistics.acquireTSL, cntSuspends, g_LastSuspendStatistics.cntSuspends);
    releaseTSL.DisplayAndUpdate(logFile, "Unlock ", &g_LastSuspendStatistics.releaseTSL, cntSuspends, g_LastSuspendStatistics.cntSuspends);
    osSuspend.DisplayAndUpdate (logFile, "OS Susp", &g_LastSuspendStatistics.osSuspend,  cntOSSuspendResume, g_LastSuspendStatistics.cntOSSuspendResume);
    crawl.DisplayAndUpdate     (logFile, "Crawl",   &g_LastSuspendStatistics.crawl,      cntHijackCrawl, g_LastSuspendStatistics.cntHijackCrawl);
    wait.DisplayAndUpdate      (logFile, "Wait",    &g_LastSuspendStatistics.wait,       cntWaits,    g_LastSuspendStatistics.cntWaits);

    fprintf(logFile, "OS Suspend Failures %d (%d), Wait Timeouts %d (%d), Hijack traps %d (%d)\n",
           cntFailedSuspends - g_LastSuspendStatistics.cntFailedSuspends, cntFailedSuspends,
           cntWaitTimeouts - g_LastSuspendStatistics.cntWaitTimeouts, cntWaitTimeouts,
           cntHijackTrap - g_LastSuspendStatistics.cntHijackTrap, cntHijackTrap);

    fprintf(logFile, "Redirected EIP Failures %d (%d)\n",
           cntFailedRedirections - g_LastSuspendStatistics.cntFailedRedirections, cntFailedRedirections);

    fprintf(logFile, "Suspend: All %d (%d). NonGC: %d (%d). InBGC: %d (%d). NonGCInBGC: %d (%d)\n\n",
            cntSuspends - g_LastSuspendStatistics.cntSuspends, cntSuspends,
            cntNonGCSuspends - g_LastSuspendStatistics.cntNonGCSuspends, cntNonGCSuspends,
            cntSuspendsInBGC - g_LastSuspendStatistics.cntSuspendsInBGC, cntSuspendsInBGC,
            cntNonGCSuspendsInBGC - g_LastSuspendStatistics.cntNonGCSuspendsInBGC, cntNonGCSuspendsInBGC);

    // close the log file...
    fclose(logFile);
    }

    memcpy(&g_LastSuspendStatistics, this, sizeof(g_LastSuspendStatistics));

    suspend.Reset();
    restart.Reset();
    paused.Reset();
    acquireTSL.Reset();
    releaseTSL.Reset();
    osSuspend.Reset();
    crawl.Reset();
    wait.Reset();
}

#endif // TIME_SUSPEND

#if defined(TIME_SUSPEND) || defined(GC_STATS)

const char* const str_timeUnit[]   = { "usec", "msec", "sec" };
const int         timeUnitFactor[] = { 1, 1000, 1000000 };

void MinMaxTot::DisplayAndUpdate(FILE* logFile, _In_z_ const char *pName, MinMaxTot *pLastOne, int fullCount, int priorCount, timeUnit unit /* = usec */)
{
    LIMITED_METHOD_CONTRACT;

    int tuf = timeUnitFactor[unit];
    int delta = fullCount - priorCount;

    fprintf(logFile, "%s  %u (%u) times for %u (%u) %s. Min %u (%u), Max %u (%u), Avg %u (%u)\n",
           pName,
           delta, fullCount,
           (totVal - pLastOne->totVal) / tuf, totVal / tuf,
           str_timeUnit[(int)unit],
           minVal / tuf, pLastOne->minVal / tuf,
           maxVal / tuf, pLastOne->maxVal / tuf,
           (delta == 0 ? 0 : (totVal - pLastOne->totVal) / delta) / tuf,
           (fullCount == 0 ? 0 : totVal / fullCount) / tuf);

    if (minVal > pLastOne->minVal && pLastOne->minVal != 0)
        minVal = pLastOne->minVal;

    if (maxVal < pLastOne->maxVal)
        maxVal = pLastOne->maxVal;
}

#endif // defined(TIME_SUSPEND) || defined(GC_STATS)
