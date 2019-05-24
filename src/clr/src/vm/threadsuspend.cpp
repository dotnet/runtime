// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

CLREvent* ThreadSuspend::g_pGCSuspendEvent = NULL;

ThreadSuspend::SUSPEND_REASON ThreadSuspend::m_suspendReason;
Thread* ThreadSuspend::m_pThreadAttemptingSuspendForGC;

CLREventBase * ThreadSuspend::s_hAbortEvt = NULL;
CLREventBase * ThreadSuspend::s_hAbortEvtCache = NULL;

// If you add any thread redirection function, make sure the debugger can 1) recognize the redirection 
// function, and 2) retrieve the original CONTEXT.  See code:Debugger.InitializeHijackFunctionAddress and
// code:DacDbiInterfaceImpl.RetrieveHijackedContext. 
extern "C" void             RedirectedHandledJITCaseForGCThreadControl_Stub(void);
extern "C" void             RedirectedHandledJITCaseForDbgThreadControl_Stub(void);
extern "C" void             RedirectedHandledJITCaseForUserSuspend_Stub(void);

#define GetRedirectHandlerForGCThreadControl()      \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForGCThreadControl_Stub))
#define GetRedirectHandlerForDbgThreadControl()     \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForDbgThreadControl_Stub))
#define GetRedirectHandlerForUserSuspend()          \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForUserSuspend_Stub))

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
extern "C" void             RedirectedHandledJITCaseForGCStress_Stub(void);
#define GetRedirectHandlerForGCStress()             \
                ((PFN_REDIRECTTARGET) GetEEFuncEntryPoint(RedirectedHandledJITCaseForGCStress_Stub))
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS
#endif // _TARGET_AMD64_ || _TARGET_ARM_


// Every PING_JIT_TIMEOUT ms, check to see if a thread in JITted code has wandered
// into some fully interruptible code (or should have a different hijack to improve
// our chances of snagging it at a safe spot).
#define PING_JIT_TIMEOUT        10

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
#define IS_VALID_WRITE_PTR(addr, size)      _ASSERTE(addr != NULL)
#define IS_VALID_CODE_PTR(addr)             _ASSERTE(addr != NULL)


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

    Volatile<HANDLE> hThread;
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

        // Whether or not "goto retry" should YieldProcessor and __SwitchToThread
        BOOL doSwitchToThread = TRUE;

        hThread = GetThreadHandle();
        if (hThread == INVALID_HANDLE_VALUE) {
            str = STR_UnstartedOrDead;
            break;
        }
        else if (hThread == SWITCHOUT_HANDLE_VALUE) {
            str = STR_SwitchedOut;
            break;
        }

        {
            // We do not want to suspend the target thread while it is holding the log lock.
            // By acquiring the lock ourselves, we know that this is not the case.
            LogLockHolder.Acquire();
            
            // It is important to avoid two threads suspending each other.
            // Before a thread suspends another, it increments its own m_dwForbidSuspendThread count first,
            // then it checks the target thread's m_dwForbidSuspendThread.
            ForbidSuspendThreadHolder forbidSuspend;
            if ((m_dwForbidSuspendThread != 0))
            {
#if defined(_DEBUG)
                // Enable the diagnostic ::SuspendThread() if the 
                //     DiagnosticSuspend config setting is set.
                // This will interfere with the mutual suspend race but it's
                //     here only for diagnostic purposes anyway
                if (!bDiagSuspend)
#endif // _DEBUG
                    goto retry;
            }

            dwSuspendCount = ::SuspendThread(hThread);

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
                    ::ResumeThread(hThread);
                    str = STR_Failure;
                    break;
                }
            }
        }
        if ((int)dwSuspendCount >= 0)
        {
            if (hThread == GetThreadHandle())
            {
                if (m_dwForbidSuspendThread != 0)
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
                    ::ResumeThread(hThread);

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
                Thread * pCurThread = GetThread();
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
                // A thread was switch out but in again.
                // We suspend a wrong thread.
                ::ResumeThread(hThread);
                doSwitchToThread = FALSE;
                goto retry;
            }
        }
        else {
            // We can get here either SuspendThread fails
            // Or the fiber thread dies after this fiber switched out.

            if ((int)dwSuspendCount != -1) {
                STRESS_LOG1(LF_SYNC, LL_INFO1000, "In Thread::SuspendThread ::SuspendThread returned %x\n", dwSuspendCount);
            }
            if (GetThreadHandle() == SWITCHOUT_HANDLE_VALUE) {
                str = STR_SwitchedOut;
                break;
            }
            else {
                // Our callers generally expect that STR_Failure means that
                // the thread has exited.
#ifndef FEATURE_PAL                
                _ASSERTE(NtCurrentTeb()->LastStatusValue != STATUS_SUSPEND_COUNT_EXCEEDED);
#endif // !FEATURE_PAL                
                str = STR_Failure;
                break;
            }
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
            dwSuspendCount = ::SuspendThread(hThread);
            _ASSERTE(!"It takes too long to suspend a thread");
            if ((int)dwSuspendCount >= 0)
                ::ResumeThread(hThread);
        }
#endif // _DEBUG

        if (doSwitchToThread)
        {
            // When looking for deadlocks we need to allow the target thread to run in order to make some progress.
            // On multi processor machines we saw the suspending thread resuming immediately after the __SwitchToThread()
            // because it has another few processors available.  As a consequence the target thread was being Resumed and
            // Suspended right away, w/o a real chance to make any progress.
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

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
        if (str == STR_Success)
        {
            g_profControlBlock.pProfInterface->RuntimeThreadSuspended((ThreadID)this);
        }
        END_PIN_PROFILER();
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

    _ASSERTE (GetThreadHandle() != SWITCHOUT_HANDLE_VALUE);

    //DWORD res = ::ResumeThread(GetThreadHandle());
    DWORD res = ::ResumeThread(m_ThreadHandleForResume);
    _ASSERTE (res != 0 && "Thread is not previously suspended");
#ifdef _DEBUG_IMPL
    _ASSERTE (!m_Creater.IsCurrentThread());
    if ((res != (DWORD)-1) && (res != 0))
    {
        Thread * pCurThread = GetThread();
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
        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
        if ((res != 0) && (res != (DWORD)-1))
        {
            g_profControlBlock.pProfInterface->RuntimeThreadResumed((ThreadID)this);
        }
        END_PIN_PROFILER();
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

    _ASSERTE(GetThread() != pThread);
    _ASSERTE(CheckPointer(pThread));

#ifndef DISABLE_THREADSUSPEND
    // Only perform this test if we're allowed to call back into the host.
    // Thread::SuspendThread contains several potential calls into the host.
    if (CanThisThreadCallIntoHost())
    {
        DWORD dwSuspendCount;
        Thread::SuspendThreadResult str = pThread->SuspendThread(FALSE, &dwSuspendCount);
        forceStackA = &dwSuspendCount;
        if (str == Thread::STR_Success)
        {
            pThread->ResumeThread();
            return dwSuspendCount >= 1;
        }
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

#ifdef _TARGET_X86_
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
#ifndef WIN64EXCEPTIONS
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
#endif  // !WIN64EXCEPTIONS

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
        if (pData->pAbortee != GetThread() && !IsFaultOrFinally(&EHClause))
        {
            continue;
        }
        if (offs >= EHClause.HandlerStartPC &&
            offs < EHClause.HandlerEndPC)
        {
#ifndef WIN64EXCEPTIONS
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
#endif // !WIN64EXCEPTIONS
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

#ifndef WIN64EXCEPTIONS
#ifdef _DEBUG
    if (fAtJitEndCatch)
    {
        _ASSERTE (countInCatch > 0);
    }
#endif   // _DEBUG
#endif   // !WIN64EXCEPTIONS_
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

#ifdef _TARGET_X86_
    // On X86 the IL stub method is reported to us before the frame with the actual interop method. We need to
    // swap the order because if the worker saw the IL stub - which is a CER root - first, it would terminate the
    // stack walk and wouldn't allow the thread to be aborted, regardless of how the interop method is annotated.
    if (pData->fHaveLatchedCF)
    {
        // Does the current and latched frame represent the same call?
        if (pCf->pFrame == pData->LatchedCF.pFrame)
        {
            if (pData->LatchedCF.GetFunction()->AsDynamicMethodDesc()->IsUnbreakable())
            {
                // Report only the latched IL stub frame which is a CER root.
                frameAction = DiscardCurrentFrame;
            }
            else
            {
                // Report the interop method (current frame) which may be annotated, then the IL stub.
                frameAction = ProcessLatchedReversed;
            }
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
#else // _TARGET_X86_
    // On 64-bit the IL stub method is reported after the actual interop method so we don't have to swap them.
    // However, we still want to discard the interop method frame if the call is unbreakable by convention.
    if (pData->fHaveLatchedCF)
    {
        MethodDesc *pMD = pCf->GetFunction();
        if (pMD != NULL && pMD->IsILStub() &&
            pData->LatchedCF.GetFrame()->GetReturnAddress() == GetControlPC(pCf->GetRegisterSet()) &&
            pMD->AsDynamicMethodDesc()->IsUnbreakable())
        {
            // The current and latched frame represent the same call and the IL stub is marked as unbreakable.
            // We will discard the interop method and report only the IL stub which is a CER root.
            frameAction = DiscardLatchedFrame;
        }
        else
        {
            // Otherwise process the two frames in order.
            frameAction = ProcessLatchedInOrder;
        }
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
#endif // _TARGET_X86_

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
    _ASSERTE (pThread);
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


// Context structure used during stack walks to determine whether a given method is executing within a CER.
struct CerStackCrawlContext
{
    MethodDesc *m_pStartMethod;         // First method we crawl (here for debug purposes)
    bool        m_fFirstFrame;          // True for first callback only
    bool        m_fWithinCer;           // The result
};


// Determine whether the method at the given depth in the thread's execution stack is executing within a CER.
BOOL Thread::IsWithinCer(CrawlFrame *pCf)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return FALSE;
}

#if defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)
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
#endif // defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)

#ifdef _TARGET_AMD64_
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

    if (IsAbortRequested() && HasThreadStateNC(TSNC_SOWorkNeeded))
    {
        return TRUE;
    }

    if (GetThread() == this && HasThreadStateNC (TSNC_PreparingAbort) && !IsRudeAbort() )
    {
        STRESS_LOG0(LF_APPDOMAIN, LL_INFO10, "in Thread::ReadyForAbort  PreparingAbort\n");
        // Avoid recursive call
        return FALSE;
    }

    if (IsAbortPrevented())
    {
        //
        // If the thread is marked to have a FuncEval abort request, then allow that to go through
        // since we dont want to block funcEval aborts. Such requests are initiated by the
        // right-side when the thread is doing funcEval and the exception would be caught in the 
        // left-side's funcEval implementation that will then clear the funcEval-abort-state from the thread.
        //
        // If another thread also marked this one for a non-FuncEval abort, then the left-side will 
        // proceed to [re]throw that exception post funcEval abort. When we come here next, we would follow 
        // the usual rules to raise the exception and if raised, to prevent the abort if applicable.
        //
        if (!IsFuncEvalAbort())
        {
            STRESS_LOG0(LF_APPDOMAIN, LL_INFO10, "in Thread::ReadyForAbort  prevent abort\n");
            return FALSE;
        }
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
#if defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)
        else if (ThrewControlForThread() == Thread::InducedThreadRedirect)
        {
            if (!IsSafeToInjectThreadAbort(m_OSContext))
            {
                STRESS_LOG0(LF_EH, LL_INFO10, "Thread::ReadyForAbort: Not injecting abort since we are at an unsafe instruction.\n");
                return FALSE;
            }
        }
#endif // defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)
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

    if (!TAContext.fHasManagedCodeOnStack && IsAbortInitiated() && GetThread() == this)
    {
        EEResetAbort(TAR_Thread);
        return FALSE;
    }

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

BOOL Thread::IsFuncEvalAbort()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return (IsAbortRequested() && (m_AbortInfo & TAI_AnyFuncEvalAbort));
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
BOOL Thread::IsContextSafeToRedirect(CONTEXT* pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL isSafeToRedirect = TRUE;

#ifndef FEATURE_PAL
    
#if !defined(_TARGET_X86_)
    // In some cases (x86 WOW64, ARM32 on ARM64) Windows will not set the CONTEXT_EXCEPTION_REPORTING flag
    // if the thread is executing in kernel mode (i.e. in the middle of a syscall or exception handling).
    // Therefore, we should treat the absence of the CONTEXT_EXCEPTION_REPORTING flag as an indication that
    // it is not safe to manipulate with the current state of the thread context.
    // Note: the x86 WOW64 case is already handled in GetSafelyRedirectableThreadContext; in addition, this
    // flag is never set on Windows7 x86 WOW64. So this check is valid for non-x86 architectures only.
    isSafeToRedirect = (pContext->ContextFlags & CONTEXT_EXCEPTION_REPORTING) != 0;
#endif // !defined(_TARGET_X86_)

    if (pContext->ContextFlags & CONTEXT_EXCEPTION_REPORTING)
    {
        if (pContext->ContextFlags & (CONTEXT_SERVICE_ACTIVE|CONTEXT_EXCEPTION_ACTIVE))
        {
            // cannot process exception
            LOG((LF_ALWAYS, LL_WARNING, "thread [os id=0x08%x id=0x08%x] redirect failed due to ContextFlags of 0x%08x\n", m_OSThreadId, m_ThreadId, pContext->ContextFlags));
            isSafeToRedirect = FALSE;
        }
    }

#endif // !FEATURE_PAL

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

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT
Thread::UserAbort(ThreadAbortRequester requester,
                  EEPolicy::ThreadAbortTypes abortType,
                  DWORD timeout,
                  UserAbort_Client client
                 )
{
    CONTRACTL
    {
        THROWS;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    STRESS_LOG2(LF_SYNC | LF_APPDOMAIN, LL_INFO100, "UserAbort Thread %p Thread Id = %x\n", this, GetThreadId());

    BOOL fHoldingThreadStoreLock = ThreadStore::HoldingThreadStore();

    // For SafeAbort from FuncEval abort, we do not apply escalation policy.  Debugger
    // tries SafeAbort first with a short timeout.  The thread will return to debugger.
    // After some break, the thread is going to do RudeAbort if abort has not finished.
    EClrOperation operation;
    if (abortType == EEPolicy::TA_Rude)
    {
        if (HasLockInCurrentDomain())
        {
            operation = OPR_ThreadRudeAbortInCriticalRegion;
        }
        else
        {
            operation = OPR_ThreadRudeAbortInNonCriticalRegion;
        }
    }
    else
    {
        operation = OPR_ThreadAbort;
    }

    // Debugger func-eval aborts (both rude + normal) don't have any escalation policy. They are invoked
    // by the debugger and the debugger handles the consequences. 
    // Furthermore, in interop-debugging, threads will be hard-suspened in preemptive mode while we try to abort them.
    // So any abort strategy that relies on a timeout and the target thread slipping is dangerous. Escalation policy would let a 
    // host circumvent the timeout and thus we may wait forever for the target thread to slip. We'd deadlock here. Since the escalation
    // policy doesn't let the host break this deadlock (and certianly doesn't let the debugger break the deadlock), it's unsafe
    // to have an escalation policy for func-eval aborts at all.
    BOOL fEscalation = (requester != TAR_FuncEval);
    if (fEscalation)
    {
        EPolicyAction action = GetEEPolicy()->GetDefaultAction(operation, this);
        switch (action)
        {
        case eAbortThread:
            GetEEPolicy()->NotifyHostOnDefaultAction(operation,action);
            break;
        case eRudeAbortThread:
            if (abortType != EEPolicy::TA_Rude)
            {
                abortType = EEPolicy::TA_Rude;
            }
            GetEEPolicy()->NotifyHostOnDefaultAction(operation,action);
            break;
        case eUnloadAppDomain:
        case eRudeUnloadAppDomain:
            // AD unload does not abort finalizer thread.
            if (this != FinalizerThread::GetFinalizerThread())
            {
                if (this == GetThread())
                {
                    Join(INFINITE,TRUE);
                }
                return S_OK;
            }
            break;
        case eExitProcess:
        case eFastExitProcess:
        case eRudeExitProcess:
            GetEEPolicy()->NotifyHostOnDefaultAction(operation,action);
            EEPolicy::HandleExitProcessFromEscalation(action, HOST_E_EXITPROCESS_THREADABORT);
            _ASSERTE (!"Should not reach here");
            break;
        default:
            _ASSERTE (!"unknown policy for thread abort");
        }

        DWORD timeoutFromPolicy;
        if (abortType != EEPolicy::TA_Rude)
        {
            timeoutFromPolicy = GetEEPolicy()->GetTimeout(OPR_ThreadAbort);
        }
        else if (!HasLockInCurrentDomain())
        {
            timeoutFromPolicy = GetEEPolicy()->GetTimeout(OPR_ThreadRudeAbortInNonCriticalRegion);
        }
        else
        {
            timeoutFromPolicy = GetEEPolicy()->GetTimeout(OPR_ThreadRudeAbortInCriticalRegion);
        }
        if (timeout > timeoutFromPolicy)
        {
            timeout = timeoutFromPolicy;
        }
    }

    AbortControlHolder AbortController(this);

    // Swap in timeout
    if (timeout != INFINITE)
    {
        ULONG64 curTime = CLRGetTickCount64();
        ULONG64 newEndTime = curTime + timeout;

        SetAbortEndTime(newEndTime, abortType == EEPolicy::TA_Rude);
    }

    MarkThreadForAbort(requester, abortType);

    Thread *pCurThread = GetThread();

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
            exceptObj = CLRException::GetPreallocatedRudeThreadAbortException();
        }
        else
        {
            EEException eeExcept(kThreadAbortException);
            exceptObj = CLRException::GetThrowableFromException(&eeExcept);
        }

        RaiseTheExceptionInternalOnly(exceptObj, FALSE);
    }

    _ASSERTE(this != pCurThread);      // Aborting another thread.

    if (client == UAC_Host)
    {
        // A host may call ICLRTask::Abort on a critical thread.  We don't want to
        // block this thread.
        return S_OK;
    }

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

LRetry:
    for (;;)
    {
        // Lock the thread store
        LOG((LF_SYNC, INFO3, "UserAbort obtain lock\n"));

        ULONGLONG abortEndTime = GetAbortEndTime();
        if (abortEndTime != MAXULONGLONG)
        {
            ULONGLONG now_time = CLRGetTickCount64();

            if (now_time >= abortEndTime)
            {
                EPolicyAction action1 = eNoAction;
                DWORD timeout1 = INFINITE;
                if (fEscalation)
                {
                    if (!IsRudeAbort())
                    {
                        action1 = GetEEPolicy()->GetActionOnTimeout(OPR_ThreadAbort, this);
                        timeout1 = GetEEPolicy()->GetTimeout(OPR_ThreadAbort);
                    }
                    else if (HasLockInCurrentDomain())
                    {
                        action1 = GetEEPolicy()->GetActionOnTimeout(OPR_ThreadRudeAbortInCriticalRegion, this);
                        timeout1 = GetEEPolicy()->GetTimeout(OPR_ThreadRudeAbortInCriticalRegion);
                    }
                    else
                    {
                        action1 = GetEEPolicy()->GetActionOnTimeout(OPR_ThreadRudeAbortInNonCriticalRegion, this);
                        timeout1 = GetEEPolicy()->GetTimeout(OPR_ThreadRudeAbortInNonCriticalRegion);
                    }
                }
                if (action1 == eNoAction)
                {
                    // timeout, but no action on timeout.
                    // Debugger can call this function to about func-eval with a timeout
                    return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
                }
                if (timeout1 != INFINITE)
                {
                    break;
                }
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
              m_NeedRelease(TRUE)
            {
                if (!fHoldingThreadStoreLock)
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

        // We own TS lock.  The state of the Thread can not be changed.
        if (m_State & TS_Unstarted)
        {
            // This thread is not yet started.
#ifdef _DEBUG
            m_dwAbortPoint = 2;
#endif
            if(requester == Thread::TAR_Thread)
                SetAborted();
            return S_OK;
        }

        if (GetThreadHandle() == INVALID_HANDLE_VALUE &&
            (m_State & TS_Unstarted) == 0)
        {
            // The thread is going to die or is already dead.
            UnmarkThreadForAbort(Thread::TAR_ALL);
#ifdef _DEBUG
            m_dwAbortPoint = 3;
#endif
            if(requester == Thread::TAR_Thread)
                SetAborted();
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
            if(requester == Thread::TAR_Thread)
                SetAborted();
            return S_OK;
        }

        // If a thread is Dead or Detached, abort is a NOP.
        //
        if (m_State & (TS_Dead | TS_Detached | TS_TaskReset))
        {
            UnmarkThreadForAbort(Thread::TAR_ALL);
            if(requester == Thread::TAR_Thread)
                SetAborted();
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

        BOOL fOutOfRuntime = FALSE;
        BOOL fNeedStackCrawl = FALSE;

#ifdef DISABLE_THREADSUSPEND
        // On platforms that do not support safe thread suspension we have to 
        // rely on the GCPOLL mechanism; the mechanism is activated above by 
        // TrapReturningThreads.  However when reading shared state we need 
        // to erect appropriate memory barriers. So the interlocked operation 
        // below ensures that any future reads on this thread will happen after
        // any earlier writes on a different thread have taken effect.
        FastInterlockOr((DWORD*)&m_State, 0);

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

        case STR_SwitchedOut:
            // If the thread is in preemptive gc mode, we can erect a barrier to block the
            // thread to return to cooperative mode.  Then we can do stack crawl and make decision.
            if (!m_fPreemptiveGCDisabled)
            {
                checkForAbort.NeedStackCrawl();
                if (GetThreadHandle() != SWITCHOUT_HANDLE_VALUE || m_fPreemptiveGCDisabled)
                {
                    checkForAbort.Release();
                    __SwitchToThread(0, ++dwSwitchCount);
                    continue;
                }
                else
                {
                    goto LStackCrawl;
                }
            }
            else
            {
                goto LPrepareRetry;
            }

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
            if(requester == Thread::TAR_Thread)
                SetAborted();
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
        // If it's suspended w/o the debugger (eg, by via Thread.Suspend), it will
        // also have TS_UserSuspendPending set.
        if (m_State & TS_SyncSuspended)
        {
#ifndef DISABLE_THREADSUSPEND
            ResumeThread();
#endif
            checkForAbort.Release();
#ifdef _DEBUG
            m_dwAbortPoint = 7;
#endif

            // CoreCLR does not support user-requested thread suspension
            _ASSERTE(!(m_State & TS_UserSuspendPending));

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

#if defined(_TARGET_X86_) && !defined(WIN64EXCEPTIONS)
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

            if(requester == Thread::TAR_Thread)
                SetAborted();
            return S_OK;
        }
#endif // _TARGET_X86_


        if (!m_fPreemptiveGCDisabled)
        {
            if ((m_pFrame != FRAME_TOP) && m_pFrame->IsTransitionToNativeFrame()
#if defined(_TARGET_X86_) && !defined(WIN64EXCEPTIONS)
                && ((size_t) GetFirstCOMPlusSEHRecord(this) > ((size_t) m_pFrame) - 20)
#endif // _TARGET_X86_
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
#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
        else
        {
            HandleJITCaseForAbort();
        }
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

#ifndef DISABLE_THREADSUSPEND
        // The thread is not suspended now.
        ResumeThread();
#endif

        if (!fNeedStackCrawl)
        {
            goto LPrepareRetry;
        }

#ifndef DISABLE_THREADSUSPEND
LStackCrawl:
#endif // DISABLE_THREADSUSPEND

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

    } // for(;;)

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

        if (IsAbortRequested() && fEscalation)
        {
            EPolicyAction action1;
            EClrOperation operation1;
            if (!IsRudeAbort())
            {
                operation1 = OPR_ThreadAbort;
            }
            else if (HasLockInCurrentDomain())
            {
                operation1 = OPR_ThreadRudeAbortInCriticalRegion;
            }
            else
            {
                operation1 = OPR_ThreadRudeAbortInNonCriticalRegion;
            }
            action1 = GetEEPolicy()->GetActionOnTimeout(operation1, this);
            switch (action1)
            {
            case eRudeAbortThread:
                GetEEPolicy()->NotifyHostOnTimeout(operation1, action1);
                MarkThreadForAbort(requester, EEPolicy::TA_Rude);
                SetRudeAbortEndTimeFromEEPolicy();
                goto LRetry;
            case eUnloadAppDomain:
                // AD unload does not abort finalizer thread.
                if (this == FinalizerThread::GetFinalizerThread())
                {
                    GetEEPolicy()->NotifyHostOnTimeout(operation1, action1);
                    MarkThreadForAbort(requester, EEPolicy::TA_Rude);
                    SetRudeAbortEndTimeFromEEPolicy();
                    goto LRetry;
                }
                else
                {
                    if (this == GetThread())
                    {
                        Join(INFINITE,TRUE);
                    }
                    return S_OK;
                }
                break;
            case eRudeUnloadAppDomain:
                // AD unload does not abort finalizer thread.
                if (this == FinalizerThread::GetFinalizerThread())
                {
                    MarkThreadForAbort(requester, EEPolicy::TA_Rude);
                    SetRudeAbortEndTimeFromEEPolicy();
                    goto LRetry;
                }
                else
                {
                    if (this == GetThread())
                    {
                        Join(INFINITE,TRUE);
                    }
                    return S_OK;
                }
                break;
            case eExitProcess:
            case eFastExitProcess:
            case eRudeExitProcess:
                GetEEPolicy()->NotifyHostOnTimeout(operation1, action1);
                EEPolicy::HandleExitProcessFromEscalation(action1, HOST_E_EXITPROCESS_TIMEOUT);
                _ASSERTE (!"Should not reach here");
                break;
            default:
            break;
            }
        }

        return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
    }

    if(requester == Thread::TAR_Thread)
        SetAborted();
    return S_OK;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void Thread::SetRudeAbortEndTimeFromEEPolicy()
{
    LIMITED_METHOD_CONTRACT;

    DWORD timeout = GetEEPolicy()->GetTimeout(OPR_ThreadRudeAbortInCriticalRegion);

    ULONGLONG newEndTime;
    if (timeout == INFINITE)
    {
        newEndTime = MAXULONGLONG;
    }
    else
    {
        newEndTime = CLRGetTickCount64() + timeout;
    }

    SetAbortEndTime(newEndTime, TRUE);
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
        if (FastInterlockCompareExchange(&(pThread->m_AbortRequestLock),1,0) == 0) {
            return;
        }
        __SwitchToThread(0, ++dwSwitchCount);
    }
}

void Thread::UnlockAbortRequest(Thread *pThread)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE (pThread->m_AbortRequestLock == 1);
    FastInterlockExchange(&pThread->m_AbortRequestLock, 0);
}

void Thread::MarkThreadForAbort(ThreadAbortRequester requester, EEPolicy::ThreadAbortTypes abortType)
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

    DWORD abortInfo = 0;

    if (requester & TAR_Thread)
    {
        if (abortType == EEPolicy::TA_Safe)
        {
            abortInfo |= TAI_ThreadAbort;
        }
        else if (abortType == EEPolicy::TA_Rude)
        {
            abortInfo |= TAI_ThreadRudeAbort;
        }
    }

    if (requester & TAR_FuncEval)
    {
        if (abortType == EEPolicy::TA_Safe)
        {
            abortInfo |= TAI_FuncEvalAbort;
        }
        else if (abortType == EEPolicy::TA_Rude)
        {
            abortInfo |= TAI_FuncEvalRudeAbort;
        }
    }

    if (abortInfo == 0)
    {
        ASSERT(!"Invalid abort information");
        return;
    }

    if (requester == TAR_Thread)
    {
        DWORD timeoutFromPolicy;
        if (abortType != EEPolicy::TA_Rude)
        {
            timeoutFromPolicy = GetEEPolicy()->GetTimeout(OPR_ThreadAbort);
        }
        else if (!HasLockInCurrentDomain())
        {
            timeoutFromPolicy = GetEEPolicy()->GetTimeout(OPR_ThreadRudeAbortInNonCriticalRegion);
        }
        else
        {
            timeoutFromPolicy = GetEEPolicy()->GetTimeout(OPR_ThreadRudeAbortInCriticalRegion);
        }
        if (timeoutFromPolicy != INFINITE)
        {
            ULONGLONG endTime = CLRGetTickCount64() + timeoutFromPolicy;
            if (abortType != EEPolicy::TA_Rude)
            {
                if (endTime < m_AbortEndTime)
                {
                    m_AbortEndTime = endTime;
                }
            }
            else if (endTime < m_RudeAbortEndTime)
            {
                m_RudeAbortEndTime = endTime;
            }
        }
    }

    if (abortInfo == (m_AbortInfo & abortInfo))
    {
        //
        // We are already doing this kind of abort.
        //
        return;
    }

    m_AbortInfo |= abortInfo;

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
    STRESS_LOG4(LF_APPDOMAIN, LL_ALWAYS, "Mark Thread %p Thread Id = %x for abort from requester %d (type %d)\n", this, GetThreadId(), requester, abortType);
}

void Thread::SetAbortRequestBit()
{
    WRAPPER_NO_CONTRACT;
    while (TRUE)
    {
        Volatile<LONG> curValue = (LONG)m_State;
        if ((curValue & TS_AbortRequested) != 0)
        {
            break;
        }
        if (FastInterlockCompareExchange((LONG*)&m_State, curValue|TS_AbortRequested, curValue) == curValue)
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
        Volatile<LONG> curValue = (LONG)m_State;
        if ((curValue & TS_AbortRequested) == 0)
        {
            break;
        }
        if (FastInterlockCompareExchange((LONG*)&m_State, curValue&(~TS_AbortRequested), curValue) == curValue)
        {
            ThreadStore::TrapReturningThreads(FALSE);

            break;
        }
    }
}

// Make sure that when AbortRequest bit is cleared, we also dec TrapReturningThreads count.
void Thread::UnmarkThreadForAbort(ThreadAbortRequester requester, BOOL fForce)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Switch to COOP (for ClearAbortReason) before acquiring AbortRequestLock 
    GCX_COOP();

    AbortRequestLockHolder lh(this);

    //
    // Unmark the bits that are being turned off
    //
    if (requester & TAR_Thread)
    {
        if ((m_AbortInfo != TAI_ThreadRudeAbort) || fForce)
        {
            m_AbortInfo &= ~(TAI_ThreadAbort   |
                             TAI_ThreadRudeAbort );
        }
    }

    if (requester & TAR_FuncEval)
    {
        m_AbortInfo &= ~(TAI_FuncEvalAbort   |
                         TAI_FuncEvalRudeAbort);
    }

    //
    // Decide which type of abort to do based on the new bit field.
    //
    if (m_AbortInfo & TAI_AnyRudeAbort)
    {
        m_AbortType = EEPolicy::TA_Rude;
    }
    else if (m_AbortInfo & TAI_AnySafeAbort)
    {
        m_AbortType = EEPolicy::TA_Safe;
    }
    else
    {
        m_AbortType = EEPolicy::TA_None;
    }

    //
    // If still aborting, do nothing
    //
    if (m_AbortType != EEPolicy::TA_None)
    {
        return;
    }

    m_AbortEndTime = MAXULONGLONG;
    m_RudeAbortEndTime = MAXULONGLONG;

    if (IsAbortRequested())
    {
        RemoveAbortRequestBit();
        FastInterlockAnd((DWORD*)&m_State,~(TS_AbortInitiated));
        m_fRudeAbortInitiated = FALSE;
        ResetUserInterrupted();
    }

    STRESS_LOG3(LF_APPDOMAIN, LL_ALWAYS, "Unmark Thread %p Thread Id = %x for abort from requester %d\n", this, GetThreadId(), requester);
}

void Thread::InternalResetAbort(ThreadAbortRequester requester, BOOL fResetRudeAbort)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(this == GetThread());
    _ASSERTE(!IsDead());

    // managed code can not reset Rude thread abort
    UnmarkThreadForAbort(requester, fResetRudeAbort);
}


void ThreadSuspend::LockThreadStore(ThreadSuspend::SUSPEND_REASON reason)
{
    CONTRACTL {
        NOTHROW;
        if ((GetThread() != NULL) && GetThread()->PreemptiveGCDisabled()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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

        Thread *pCurThread = GetThread();

        gcOnTransitions = GC_ON_TRANSITIONS(FALSE);                // dont do GC for GCStress 3

        BOOL toggleGC = (   pCurThread != NULL
                         && pCurThread->PreemptiveGCDisabled()
                         && reason != ThreadSuspend::SUSPEND_FOR_GC);

        // Note: there is logic in gc.cpp surrounding suspending all
        // runtime threads for a GC that depends on the fact that we
        // do an EnablePreemptiveGC and a DisablePreemptiveGC around
        // taking this lock.
        if (toggleGC)
            pCurThread->EnablePreemptiveGC();

        LOG((LF_SYNC, INFO3, "Locking thread store\n"));

        // Any thread that holds the thread store lock cannot be stopped by unmanaged breakpoints and exceptions when
        // we're doing managed/unmanaged debugging. Calling SetDebugCantStop(true) on the current thread helps us
        // remember that.
        if (pCurThread)
            pCurThread->SetDebugCantStop(true);

        // This is used to avoid thread starvation if non-GC threads are competing for
        // the thread store lock when there is a real GC-thread waiting to get in.
        // This is initialized lazily when the first non-GC thread backs out because of
        // a waiting GC thread.
        if (s_hAbortEvt != NULL &&
            !(reason == ThreadSuspend::SUSPEND_FOR_GC || 
              reason == ThreadSuspend::SUSPEND_FOR_GC_PREP ||
              reason == ThreadSuspend::SUSPEND_FOR_DEBUGGER_SWEEP) &&
            m_pThreadAttemptingSuspendForGC != NULL &&
            m_pThreadAttemptingSuspendForGC != pCurThread)
        {
            CLREventBase * hAbortEvt = s_hAbortEvt;

            if (hAbortEvt != NULL)
            {
                LOG((LF_SYNC, INFO3, "Performing suspend abort wait.\n"));
                hAbortEvt->Wait(INFINITE, FALSE);
                LOG((LF_SYNC, INFO3, "Release from suspend abort wait.\n"));
            }
        }

        // This is shutdown aware. If we're in shutdown, and not helper/finalizer/shutdown
        // then this will not take the lock and just block forever.
        ThreadStore::s_pThreadStore->Enter();


        _ASSERTE(ThreadStore::s_pThreadStore->m_holderthreadid.IsUnknown());
        ThreadStore::s_pThreadStore->m_holderthreadid.SetToCurrentThread();

        LOG((LF_SYNC, INFO3, "Locked thread store\n"));

        // Established after we obtain the lock, so only useful for synchronous tests.
        // A thread attempting to suspend us asynchronously already holds this lock.
        ThreadStore::s_pThreadStore->m_HoldingThread = pCurThread;

#ifndef _PREFAST_
        if (toggleGC)
            pCurThread->DisablePreemptiveGC();
#endif

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
        Thread *pCurThread = GetThread();

        LOG((LF_SYNC, INFO3, "Unlocking thread store\n"));
        _ASSERTE(GetThread() == NULL || ThreadStore::s_pThreadStore->m_HoldingThread == GetThread());

#ifdef _DEBUG
        // If Thread object has been destroyed, we need to reset the ownership info in Crst.
        _ASSERTE(!bThreadDestroyed || GetThread() == NULL);
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
            pCurThread->SetDebugCantStop(false);
    }
#ifdef _DEBUG
    else
        LOG((LF_SYNC, INFO3, "Unlocking thread store skipped upon detach\n"));
#endif
}


void ThreadStore::AllocateOSContext()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(HoldingThreadStore());
    if (s_pOSContext == NULL
#ifdef _DEBUG
        || s_pOSContext == (CONTEXT*)0x1
#endif
       )
    {
        s_pOSContext = new (nothrow) CONTEXT();
    }
#ifdef _DEBUG
    if (s_pOSContext == NULL)
    {
        s_pOSContext = (CONTEXT*)0x1;
    }
#endif
}

CONTEXT *ThreadStore::GrabOSContext()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(HoldingThreadStore());
    CONTEXT *pContext = s_pOSContext;
    s_pOSContext = NULL;
#ifdef _DEBUG
    if (pContext == (CONTEXT*)0x1)
    {
        pContext = NULL;
    }
#endif
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

    if (!GCHeapUtilities::IsGCHeapInitialized())
    {
        goto Exit;
    }

    // CoreCLR does not support user-requested thread suspension
    _ASSERTE(!(m_State & TS_UserSuspendPending));

    // Note IsGCInProgress is also true for say Pause (anywhere SuspendEE happens) and GCThread is the 
    // thread that did the Pause. While in Pause if another thread attempts Rev/Pinvoke it should get inside the following and 
    // block until resume
    if ((GCHeapUtilities::IsGCInProgress()  && (this != ThreadSuspend::GetSuspensionThread())) ||
        (m_State & (TS_UserSuspendPending | TS_DebugSuspendPending | TS_StackCrawlNeeded)))
    {
        if (!ThreadStore::HoldingThreadStore(this))
        {
            STRESS_LOG1(LF_SYNC, LL_INFO1000, "RareDisablePreemptiveGC: entering. Thread state = %x\n", m_State.Load());

            DWORD dwSwitchCount = 0;

            do
            {
                // CoreCLR does not support user-requested thread suspension
                _ASSERTE(!(m_State & TS_UserSuspendPending));

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
                        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
                        if (!(m_State & TS_DebugSuspendPending))
                        {
                            g_profControlBlock.pProfInterface->RuntimeThreadSuspended((ThreadID)this);
                        }
                        END_PIN_PROFILER();
                    }
#endif // PROFILING_SUPPORTED



                    DWORD status = S_OK;
                    SetThreadStateNC(TSNC_WaitUntilGCFinished);
                    status = GCHeapUtilities::GetGCHeap()->WaitUntilGCComplete();
                    ResetThreadStateNC(TSNC_WaitUntilGCFinished);

                    if (status == (DWORD)COR_E_STACKOVERFLOW)
                    {
                        // One of two things can happen here:
                        // 1. GC is suspending the process.  GC needs to wait.
                        // 2. GC is proceeding after suspension.  The current thread needs to spin.
                        SetThreadState(TS_BlockGCForSO);
                        while (GCHeapUtilities::IsGCInProgress() && m_fPreemptiveGCDisabled.Load() == 0)
                        {
#undef Sleep
                            // We can not go to a host for blocking operation due ot lack of stack.
                            // Instead we will spin here until
                            // 1. GC is finished; Or
                            // 2. GC lets this thread to run and will wait for it
                            Sleep(10);
#define Sleep(a) Dont_Use_Sleep(a)
                        }
                        ResetThreadState(TS_BlockGCForSO);
                        if (m_fPreemptiveGCDisabled.Load() == 1)
                        {
                            // GC suspension has allowed this thread to switch back to cooperative mode.
                            break;
                        }
                    }
                    if (!GCHeapUtilities::IsGCInProgress())
                    {
                        if (HasThreadState(TS_StackCrawlNeeded))
                        {
                            SetThreadStateNC(TSNC_WaitUntilGCFinished);
                            ThreadStore::WaitForStackCrawlEvent();
                            ResetThreadStateNC(TSNC_WaitUntilGCFinished);
                        }
                        else
                        {
                            __SwitchToThread(0, ++dwSwitchCount);
                        }
                    }

#ifdef PROFILING_SUPPORTED
                    // Let the profiler know that this thread is resuming
                    {
                        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
                        g_profControlBlock.pProfInterface->RuntimeThreadResumed((ThreadID)this);
                        END_PIN_PROFILER();
                    }
#endif // PROFILING_SUPPORTED
                }

                END_GCX_ASSERT_PREEMP;

                // disable preemptive gc.
                FastInterlockOr(&m_fPreemptiveGCDisabled, 1);

                // The fact that we check whether 'this' is the GC thread may seem
                // strange.  After all, we determined this before entering the method.
                // However, it is possible for the current thread to become the GC
                // thread while in this loop.  This happens if you use the COM+
                // debugger to suspend this thread and then release it.

            } while ((GCHeapUtilities::IsGCInProgress()  && (this != ThreadSuspend::GetSuspensionThread())) ||
                     (m_State & (TS_UserSuspendPending | TS_DebugSuspendPending | TS_StackCrawlNeeded)));
        }
        STRESS_LOG0(LF_SYNC, LL_INFO1000, "RareDisablePreemptiveGC: leaving\n");
    }

Exit: ;
    END_PRESERVE_LAST_ERROR;
}

void Thread::HandleThreadAbortTimeout()
{
    WRAPPER_NO_CONTRACT;

    EPolicyAction action = eNoAction;
    EClrOperation operation = OPR_ThreadRudeAbortInNonCriticalRegion;

    if (IsFuncEvalAbort())
    {   
        // There can't be escalation policy for FuncEvalAbort timeout.
        // The debugger should retain control of the policy.  For example, if a RudeAbort times out, it's
        // probably because the debugger had some other thread frozen.  When the thread is thawed, things might
        // be fine, so we don't want to escelate the FuncEvalRudeAbort (which will be swalled by FuncEvalHijackWorker)
        // into a user RudeThreadAbort (which will at least rip the entire thread).
        return;
    }        

    if (!IsRudeAbort())
    {
        operation = OPR_ThreadAbort;
    }
    else if (HasLockInCurrentDomain())
    {
        operation = OPR_ThreadRudeAbortInCriticalRegion;
    }
    else
    {
        operation = OPR_ThreadRudeAbortInNonCriticalRegion;
    }
    action = GetEEPolicy()->GetActionOnTimeout(operation, this);
    // We only support escalation to rude abort

    EX_TRY {
        switch (action)
        {
        case eRudeAbortThread:
            GetEEPolicy()->NotifyHostOnTimeout(operation,action);
            MarkThreadForAbort(TAR_Thread, EEPolicy::TA_Rude);
            break;
        case eExitProcess:
        case eFastExitProcess:
        case eRudeExitProcess:
            GetEEPolicy()->NotifyHostOnTimeout(operation,action);
            EEPolicy::HandleExitProcessFromEscalation(action, HOST_E_EXITPROCESS_THREADABORT);
            _ASSERTE (!"Should not reach here");
            break;
        case eNoAction:
            break;
        default:
            _ASSERTE (!"unknown policy for thread abort");
        }
    }
    EX_CATCH {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void Thread::HandleThreadAbort ()
{
    BEGIN_PRESERVE_LAST_ERROR;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // It's possible we could go through here if we hit a hard SO and MC++ has called back
    // into the runtime on this thread

    FinishSOWork();
  
    if (IsAbortRequested() && GetAbortEndTime() < CLRGetTickCount64())
    {
        HandleThreadAbortTimeout();
    }

    // @TODO: we should consider treating this function as an FCALL or HCALL and use FCThrow instead of COMPlusThrow

    // Sometimes we call this without any CLR SEH in place.  An example is UMThunkStubRareDisableWorker.
    // That's okay since COMPlusThrow will eventually erect SEH around the RaiseException. It prevents
    // us from stating CONTRACT here.

    if (ReadyForAbort())
    {
        ResetThreadState ((ThreadState)(TS_Interrupted | TS_Interruptible));
        // We are going to abort.  Abort satisfies Thread.Interrupt requirement.
        FastInterlockExchange (&m_UserInterrupt, 0);

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
            exceptObj = CLRException::GetPreallocatedRudeThreadAbortException();
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
    FastInterlockAnd((ULONG *) &m_State, ~(TS_Interruptible | TS_Interrupted));
    ResetUserInterrupted();

    if (IsRudeAbort()) {
        if (HasLockInCurrentDomain()) {
            AppDomain *pDomain = GetAppDomain();
            // Cannot enable the following assertion.
            // We may take the lock, but the lock will be released during exception backout.
            //_ASSERTE(!pDomain->IsDefaultDomain());
            EPolicyAction action = GetEEPolicy()->GetDefaultAction(OPR_ThreadRudeAbortInCriticalRegion, this);
            switch (action)
            {
            case eExitProcess:
            case eFastExitProcess:
            case eRudeExitProcess:
                    {
                GetEEPolicy()->NotifyHostOnDefaultAction(OPR_ThreadRudeAbortInCriticalRegion,action);
                GetEEPolicy()->HandleExitProcessFromEscalation(action,HOST_E_EXITPROCESS_ADUNLOAD);
                    }
                break;
            default:
                break;
            }
        }
    }
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

    if (Thread::ThreadsAtUnsafePlaces())
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

    // EnablePreemptiveGC already set us to preemptive mode before triggering the Rare path.
    // Force other threads to see this update, since the Rare path implies that someone else
    // is observing us (e.g. SuspendRuntime).

    _ASSERTE (!m_fPreemptiveGCDisabled);

    // holding a spin lock in coop mode and transit to preemp mode will cause deadlock on GC
    _ASSERTE ((m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

    FastInterlockOr (&m_fPreemptiveGCDisabled, 0);

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

        // wake up any threads waiting to suspend us, like the GC thread.
        ThreadSuspend::g_pGCSuspendEvent->Set();

        // for GC, the fact that we are leaving the EE means that it no longer needs to
        // suspend us.  But if we are doing a non-GC suspend, we need to block now.
        // Give the debugger precedence over user suspensions:
        while (m_State & (TS_DebugSuspendPending | TS_UserSuspendPending))
        {
            // CoreCLR does not support user-requested thread suspension
            _ASSERTE(!(m_State & TS_UserSuspendPending));

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
// HandleGCSuspensionForInterruptedThread. Do the right thing with this thread,
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
    while (1 == FastInterlockExchange(&g_fTrapReturningThreadsLock, 1))
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
        FastInterlockIncrement(&g_trtChgStamp);
#endif

        GCHeapUtilities::GetGCHeap()->SetSuspensionPending(true);
        FastInterlockIncrement (&g_TrapReturningThreads);
#ifdef ENABLE_FAST_GCPOLL_HELPER
        EnableJitGCPoll();
#endif
        _ASSERTE(g_TrapReturningThreads > 0);

#ifdef _DEBUG
        trtHolder.Release();
#endif
    }
    else
    {
        FastInterlockDecrement (&g_TrapReturningThreads);
        GCHeapUtilities::GetGCHeap()->SetSuspensionPending(false);

#ifdef ENABLE_FAST_GCPOLL_HELPER
        if (0 == g_TrapReturningThreads)
        {
            DisableJitGCPoll();
        }
#endif

        _ASSERTE(g_TrapReturningThreads >= 0);
    }
#ifdef ENABLE_FAST_GCPOLL_HELPER
    //Ensure that we flush the cache line containing the GC Poll Helper.
    MemoryBarrier();
#endif //ENABLE_FAST_GCPOLL_HELPER
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

    if (pThread->GetSavedRedirectContext())
    {
        delete m_Regs;
    }
    else
    {
        // Save it for future use to avoid repeatedly new'ing
        pThread->SetSavedRedirectContext(m_Regs);
    }

    m_Regs = NULL;
}

#ifndef PLATFORM_UNIX

#ifdef _TARGET_X86_
//****************************************************************************************
// This will check who caused the exception.  If it was caused by the the redirect function,
// the reason is to resume the thread back at the point it was redirected in the first
// place.  If the exception was not caused by the function, then it was caused by the call
// out to the I[GC|Debugger]ThreadControl client and we need to determine if it's an
// exception that we can just eat and let the runtime resume the thread, or if it's an
// uncatchable exception that we need to pass on to the runtime.
//
int RedirectedHandledJITCaseExceptionFilter(
    PEXCEPTION_POINTERS pExcepPtrs,     // Exception data
    RedirectedThreadFrame *pFrame,      // Frame on stack
    BOOL fDone,                         // Whether redirect completed without exception
    CONTEXT *pCtx)                      // Saved context
{
    // !!! Do not use a non-static contract here.
    // !!! Contract may insert an exception handling record.
    // !!! This function assumes that GetCurrentSEHRecord() returns the exception record set up in
    // !!! Thread::RedirectedHandledJITCase
    //
    // !!! Do not use an object with dtor, since it injects a fs:0 entry.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    if (pExcepPtrs->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Get the thread handle
    Thread *pThread = GetThread();
    _ASSERTE(pThread);


    STRESS_LOG2(LF_SYNC, LL_INFO100, "In RedirectedHandledJITCaseExceptionFilter fDone = %d pFrame = %p\n", fDone, pFrame);

    // If we get here via COM+ exception, gc-mode is unknown.  We need it to
    // be cooperative for this function.
    GCX_COOP_NO_DTOR();

    // If the exception was due to the called client, then we need to figure out if it
    // is an exception that can be eaten or if it needs to be handled elsewhere.
    if (!fDone)
    {
        if (pExcepPtrs->ExceptionRecord->ExceptionFlags & EXCEPTION_NONCONTINUABLE)
        {
            return (EXCEPTION_CONTINUE_SEARCH);
        }

        // Get the latest thrown object
        OBJECTREF throwable = CLRException::GetThrowableFromExceptionRecord(pExcepPtrs->ExceptionRecord);

        // If this is an uncatchable exception, then let the exception be handled elsewhere
        if (IsUncatchable(&throwable))
        {
            pThread->EnablePreemptiveGC();
            return (EXCEPTION_CONTINUE_SEARCH);
        }
    }
#ifdef _DEBUG
    else
    {
        _ASSERTE(pExcepPtrs->ExceptionRecord->ExceptionCode == EXCEPTION_HIJACK);
    }
#endif

    // Unlink the frame in preparation for resuming in managed code
    pFrame->Pop();

    // Copy the saved context record into the EH context;
    ReplaceExceptionContextRecord(pExcepPtrs->ContextRecord, pCtx);

    DWORD espValue = pCtx->Esp;
    if (pThread->GetSavedRedirectContext())
    {
        delete pCtx;
    }
    else
    {
        // Save it for future use to avoid repeatedly new'ing
        pThread->SetSavedRedirectContext(pCtx);
    }

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

    // Resume execution at point where thread was originally redirected
    return (EXCEPTION_CONTINUE_EXECUTION);
}
#endif // _TARGET_X86_

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

    DWORD dwLastError = GetLastError();
    PCONTEXT pContext = GetThread()->GetSavedRedirectContext();
    SetLastError(dwLastError);

    return pContext;
}

void __stdcall Thread::RedirectedHandledJITCase(RedirectReason reason)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // We must preserve this in case we've interrupted an IL pinvoke stub before it
    // was able to save the error.
    DWORD dwLastError = GetLastError();

    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    // Get the saved context
    CONTEXT *pCtx = pThread->GetSavedRedirectContext();
    _ASSERTE(pCtx);

    INDEBUG(Thread::ObjectRefFlush(pThread));

    // Create a frame on the stack
    FrameWithCookie<RedirectedThreadFrame> frame(pCtx);

    STRESS_LOG5(LF_SYNC, LL_INFO1000, "In RedirectedHandledJITcase reason 0x%x pFrame = %p pc = %p sp = %p fp = %p", reason, &frame, GetIP(pCtx), GetSP(pCtx), GetFP(pCtx));

#ifdef _TARGET_X86_
    // This will indicate to the exception filter whether or not the exception is caused
    // by us or the client.
    BOOL fDone = FALSE;
    int filter_count = 0;       // A counter to avoid a nasty case where an
                                // up-stack filter throws another exception
                                // causing our filter to be run again for
                                // some unrelated exception.

    __try
#endif // _TARGET_X86_
    {
        // Make sure this thread doesn't reuse the context memory in re-entrancy cases
        _ASSERTE(pThread->GetSavedRedirectContext() != NULL);
        pThread->SetSavedRedirectContext(NULL);

        // Link in the frame
        frame.Push();

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
        if (reason == RedirectReason_GCStress)
        {
            _ASSERTE(pThread->PreemptiveGCDisabledOther());
            DoGcStress(frame.GetContext(), NULL);
        }
        else
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS
        {
            // Enable PGC before calling out to the client to allow runtime suspend to finish
            GCX_PREEMP_NO_DTOR();

            // Notify the interface of the pending suspension
            switch (reason) {
            case RedirectReason_GCSuspension:
                break;
            case RedirectReason_DebugSuspension:
                break;
            case RedirectReason_UserSuspension:
                // Do nothing;
                break;
            default:
                _ASSERTE(!"Invalid redirect reason");
                break;
            }

            // Disable preemptive GC so we can unlink the frame
            GCX_PREEMP_NO_DTOR_END();
        }

#ifdef _TARGET_X86_
        pThread->HandleThreadAbort();        // Might throw an exception.

        // Indicate that the call to the service went without an exception, and that
        // we're raising our own exception to resume the thread to where it was
        // redirected from
        fDone = TRUE;

        // Save the instruction pointer where we redirected last.  This does not race with the check
        // against this variable in HandledJitCase because the GC will not attempt to redirect the
        // thread until the instruction pointer of this thread is back in managed code.
        pThread->m_LastRedirectIP = GetIP(pCtx);
        pThread->m_SpinCount = 0;

        RaiseException(EXCEPTION_HIJACK, 0, 0, NULL);

#else // _TARGET_X86_

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
        //
        // If GCStress interrupts an IL stub or inlined p/invoke while it's running in preemptive mode, it switches the mode to 
        // cooperative - but we will resume to preemptive below.  We should not trigger an abort in that case, as it will fail 
        // due to the GC mode.
        //
        if (!pThread->m_fPreemptiveGCDisabledForGCStress)
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

#if defined(_TARGET_ARM_)
                // Save the original resume PC in Lr
                pCtx->Lr = uResumePC;

                // Since we have set a new IP, we have to clear conditional execution flags too.
                ClearITState(pThread->m_OSContext);
#endif // _TARGET_ARM_

                SetIP(pCtx, uAbortAddr);
            }
        }

        // Unlink the frame in preparation for resuming in managed code
        frame.Pop();

        {
            // Free the context struct if we already have one cached
            if (pThread->GetSavedRedirectContext())
            {
                CONTEXT* pCtxTemp = (CONTEXT*)_alloca(sizeof(CONTEXT));
                memcpy(pCtxTemp, pCtx, sizeof(CONTEXT));
                delete pCtx;
                pCtx = pCtxTemp;
            }
            else
            {
                // Save it for future use to avoid repeatedly new'ing
                pThread->SetSavedRedirectContext(pCtx);
            }

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
            if (pThread->m_fPreemptiveGCDisabledForGCStress)
            {
                pThread->EnablePreemptiveGC();
                pThread->m_fPreemptiveGCDisabledForGCStress = false;
            }
#endif

            LOG((LF_SYNC, LL_INFO1000, "Resuming execution with RtlRestoreContext\n"));

            SetLastError(dwLastError);

            RtlRestoreContext(pCtx, NULL);
        }
#endif // _TARGET_X86_
    }
#ifdef _TARGET_X86_
    __except (++filter_count == 1
        ? RedirectedHandledJITCaseExceptionFilter(GetExceptionInformation(), &frame, fDone, pCtx)
        : EXCEPTION_CONTINUE_SEARCH)
    {
        _ASSERTE(!"Reached body of __except in Thread::RedirectedHandledJITCase");
    }

#endif // _TARGET_X86_
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

#ifdef _TARGET_X86_
#define CONTEXT_COMPLETE (CONTEXT_FULL | CONTEXT_FLOATING_POINT |       \
                          CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS | CONTEXT_EXCEPTION_REQUEST)
#else
#define CONTEXT_COMPLETE (CONTEXT_FULL | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXCEPTION_REQUEST)
#endif

BOOL Thread::RedirectThreadAtHandledJITCase(PFN_REDIRECTTARGET pTgt)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(HandledJITCase());
    _ASSERTE(GetThread() != this);

    ////////////////////////////////////////////////////////////////
    // Acquire a context structure to save the thread state into

    // We need to distinguish between two types of callers:
    // - Most callers, including GC, operate while holding the ThreadStore
    //   lock.  This means that we can pre-allocate a context structure
    //   globally in the ThreadStore and use it in this function.
    // - Some callers (currently only YieldTask) cannot take the ThreadStore
    //   lock.  Therefore we always allocate a SavedRedirectContext in the
    //   Thread constructor.  (Since YieldTask currently is the only caller
    //   that does not hold the ThreadStore lock, we only do this when
    //   we're hosted.)

    // Check whether we have a SavedRedirectContext we can reuse:
    CONTEXT *pCtx = GetSavedRedirectContext();

    // If we've never allocated a context for this thread, do so now
    if (!pCtx)
    {
        // If our caller took the ThreadStore lock, then it pre-allocated
        // a context in the ThreadStore:
        if (ThreadStore::HoldingThreadStore())
        {
            pCtx = ThreadStore::GrabOSContext();
        }

        if (!pCtx)
        {
            // Even when our caller is YieldTask, we can find a NULL
            // SavedRedirectContext in this function:  Consider the scenario
            // where GC is in progress and has already redirected a thread.
            // That thread will set its SavedRedirectContext to NULL to enable
            // reentrancy.  Now assume that the host calls YieldTask for the
            // redirected thread.  In this case, this function will simply
            // fail, but that is fine:  The redirected thread will check,
            // before it resumes execution, whether it should yield.
            return (FALSE);
        }

        // Save the pointer for the redirect function
        _ASSERTE(GetSavedRedirectContext() == NULL);
        SetSavedRedirectContext(pCtx);
    }

    //////////////////////////////////////
    // Get and save the thread's context

    // Always get complete context
    pCtx->ContextFlags = CONTEXT_COMPLETE;
    BOOL bRes = EEGetThreadContext(this, pCtx);
    _ASSERTE(bRes && "Failed to GetThreadContext in RedirectThreadAtHandledJITCase - aborting redirect.");

    if (!bRes)
        return (FALSE);

    if (!IsContextSafeToRedirect(pCtx))
        return (FALSE);

    ////////////////////////////////////////////////////
    // Now redirect the thread to the helper function

    // Temporarily set the IP of the context to the target for SetThreadContext
    PCODE dwOrigEip = GetIP(pCtx);
#ifdef _TARGET_ARM_
    // Redirection can be required when in IT Block.
    // In that case must reset the IT state before redirection.
    DWORD dwOrigCpsr = pCtx->Cpsr;
    ClearITState(pCtx);
#endif
    _ASSERTE(ExecutionManager::IsManagedCode(dwOrigEip));
    SetIP(pCtx, (PCODE)pTgt);


    STRESS_LOG4(LF_SYNC, LL_INFO10000, "Redirecting thread %p(tid=%x) from address 0x%08x to address 0x%p\n",
        this, this->GetThreadId(), dwOrigEip, pTgt);

    bRes = EESetThreadContext(this, pCtx);
    _ASSERTE(bRes && "Failed to SetThreadContext in RedirectThreadAtHandledJITCase - aborting redirect.");

    // Restore original IP
    SetIP(pCtx, dwOrigEip);
#ifdef _TARGET_ARM_
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

    _ASSERTE(this != GetThread());
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

    // REVISIT_TODO need equivalent of this for the current thread
    //_ASSERTE(HandledJITCase());

    _ASSERTE(GetThread() == this);
    _ASSERTE(PreemptiveGCDisabledOther());
    _ASSERTE(IsAddrOfRedirectFunc(pTgt));
    _ASSERTE(pCurrentThreadCtx);
    _ASSERTE((pCurrentThreadCtx->ContextFlags & (CONTEXT_COMPLETE - CONTEXT_EXCEPTION_REQUEST))
                                             == (CONTEXT_COMPLETE - CONTEXT_EXCEPTION_REQUEST));
    _ASSERTE(ExecutionManager::IsManagedCode(GetIP(pCurrentThreadCtx)));

    ////////////////////////////////////////////////////////////////
    // Allocate a context structure to save the thread state into

    // Check to see if we've already got memory allocated for this purpose.
    CONTEXT *pCtx = GetSavedRedirectContext();

    // If we've never allocated a context for this thread, do so now
    if (!pCtx)
    {
        pCtx = new (nothrow) CONTEXT();

        if (!pCtx)
            return (FALSE);

        // Save the pointer for the redirect function
        _ASSERTE(GetSavedRedirectContext() == NULL);
        SetSavedRedirectContext(pCtx);
    }

    //////////////////////////////////////
    // Get and save the thread's context

    CopyMemory(pCtx, pCurrentThreadCtx, sizeof(CONTEXT));

    // Clear any new bits we don't understand (like XSAVE) in case we pass
    // this context to RtlRestoreContext (like for gcstress)
    pCtx->ContextFlags &= CONTEXT_ALL;

    // Ensure that this flag is set for the next time through the normal path,
    // RedirectThreadAtHandledJITCase.
    pCtx->ContextFlags |= CONTEXT_EXCEPTION_REQUEST;

    ////////////////////////////////////////////////////
    // Now redirect the thread to the helper function

    SetIP(pCurrentThreadCtx, (PCODE)pTgt);

#ifdef _TARGET_ARM_
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
    if (pFuncAddr == GetRedirectHandlerForGCStress())
        return TRUE;
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS

    return
        (pFuncAddr == GetRedirectHandlerForGCThreadControl()) ||
        (pFuncAddr == GetRedirectHandlerForDbgThreadControl()) ||
        (pFuncAddr == GetRedirectHandlerForUserSuspend());
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

#endif // !PLATFORM_UNIX
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
HRESULT ThreadSuspend::SuspendRuntime(ThreadSuspend::SUSPEND_REASON reason)
{
    CONTRACTL {
        NOTHROW;
        if (GetThread())
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
    Thread  *pCurThread = GetThread();

    // The thread we're working on (suspending, etc.) right now.
    Thread  *thread = NULL;

    // The number of threads we found in COOP mode.
    LONG     countThreads = 0;

    DWORD    res;

    // Caller is expected to be holding the ThreadStore lock.  Also, caller must
    // have set GcInProgress before coming here, or things will break;
    _ASSERTE(ThreadStore::HoldingThreadStore() || IsAtProcessExit());
    _ASSERTE(GCHeapUtilities::IsGCInProgress() );

    STRESS_LOG1(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime(reason=0x%x)\n", reason);


#ifdef PROFILING_SUPPORTED
    // If the profiler desires information about GCs, then let it know that one
    // is starting.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
        _ASSERTE(reason != ThreadSuspend::SUSPEND_FOR_DEBUGGER);
        _ASSERTE(reason != ThreadSuspend::SUSPEND_FOR_DEBUGGER_SWEEP);

        {
            g_profControlBlock.pProfInterface->RuntimeSuspendStarted(
                GCSuspendReasonToProfSuspendReason(reason));
        }
        if (pCurThread)
        {
            // Notify the profiler that the thread that is actually doing the GC is 'suspended',
            // meaning that it is doing stuff other than run the managed code it was before the
            // GC started.
            g_profControlBlock.pProfInterface->RuntimeThreadSuspended((ThreadID)pCurThread);
        }
        END_PIN_PROFILER();
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
    //
    // Later we will make more passes where we do roughly the same thing.  We should combine the two loops.
    //
    
    while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
    {
        if (thread->HasThreadState(Thread::TS_GCSuspendPending))
        {
            thread->ResetThreadState(Thread::TS_GCSuspendPending);
        }

        if (thread == pCurThread)
            continue;

        STRESS_LOG3(LF_SYNC, LL_INFO10000, "    Inspecting thread 0x%x ID 0x%x coop mode = %d\n",
            thread, thread->GetThreadId(), thread->m_fPreemptiveGCDisabled.Load());

        // Nothing confusing left over from last time.
        _ASSERTE((thread->m_State & Thread::TS_GCSuspendPending) == 0);

        // Threads can be in Preemptive or Cooperative GC mode.  Threads cannot switch
        // to Cooperative mode without special treatment when a GC is happening.
        if (thread->m_fPreemptiveGCDisabled)
        {
            // Check a little more carefully.  Threads might sneak out without telling
            // us, because of inlined PInvoke which doesn't go through RareEnablePreemptiveGC.

#ifdef DISABLE_THREADSUSPEND
            // On platforms that do not support safe thread suspension, we do one of two things:
            //
            //     - If we're on a Unix platform where hijacking is enabled, we attempt
            //       to inject a GC suspension which will try to redirect or hijack the
            //       thread to get it to a safe point.
            //
            //     - Otherwise, we rely on the GCPOLL mechanism enabled by 
            //       TrapReturningThreads.

            // When reading shared state we need to erect appropriate memory barriers.
            // The interlocked operation below ensures that any future reads on this
            // thread will happen after any earlier writes on a different thread.
            //
            // <TODO> Need more careful review of this </TODO>
            //
            FastInterlockOr(&thread->m_fPreemptiveGCDisabled, 0);

            if (thread->m_fPreemptiveGCDisabled)
            {
                FastInterlockOr((ULONG *) &thread->m_State, Thread::TS_GCSuspendPending);
                countThreads++;

#if defined(FEATURE_HIJACK) && defined(PLATFORM_UNIX)
                bool gcSuspensionSignalSuccess = thread->InjectGcSuspension();
                if (!gcSuspensionSignalSuccess)
                {
                    STRESS_LOG1(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime() -   Failed to raise GC suspension signal for thread %p.\n", thread);
                }
#endif // FEATURE_HIJACK && PLATFORM_UNIX
            }

#else // DISABLE_THREADSUSPEND

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
            DWORD dwSwitchCount = 0;
    RetrySuspension:
#endif

            // We can not allocate memory after we suspend a thread.
            // Otherwise, we may deadlock the process, because the thread we just suspended
            // might hold locks we would need to acquire while allocating.
            ThreadStore::AllocateOSContext();

#ifdef TIME_SUSPEND
            DWORD startSuspend = g_SuspendStatistics.GetTime();
#endif

            //
            // Suspend the native thread.
            //
            Thread::SuspendThreadResult str = thread->SuspendThread();

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

            if (str == Thread::STR_NoStressLog)
            {
                STRESS_LOG2(LF_SYNC, LL_ERROR, "    ERROR: Could not suspend thread 0x%x, result = %d\n", thread, str);
            }
            else
            if (thread->m_fPreemptiveGCDisabled)
            {
                // We now know for sure that the thread is still in cooperative mode.  If it's in JIT'd code, here
                // is where we try to hijack/redirect the thread.  If it's in VM code, we have to just let the VM
                // finish what it's doing.

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
                // Only check for HandledJITCase if we actually suspended the thread.
                if (str == Thread::STR_Success)
                {
                    Thread::WorkingOnThreadContextHolder workingOnThreadContext(thread);

                    //
                    // Note that thread->HandledJITCase is not a simple predicate - it actually will hijack the thread if that's possible.
                    // So HandledJITCase can do one of these:
                    //
                    //   - Return TRUE, in which case it's our responsibility to redirect the thread
                    //   - Return FALSE after hijacking the thread - we shouldn't try to redirect
                    //   - Return FALSE but not hijack the thread - there's nothing we can do either
                    //
                    // Here is another great opportunity for refactoring :)
                    //
                    if (workingOnThreadContext.Acquired() && thread->HandledJITCase())
                    {
                        // Redirect thread so we can capture a good thread context
                        // (GetThreadContext is not sufficient, due to an OS bug).
                        if (!thread->CheckForAndDoRedirectForGC())
                        {
#ifdef TIME_SUSPEND
                            g_SuspendStatistics.cntFailedRedirections++;
#endif
                            STRESS_LOG1(LF_SYNC, LL_INFO1000, "Failed to CheckForAndDoRedirectForGC(). Retry suspension for thread %p\n", thread);
                            thread->ResumeThread();
                            __SwitchToThread(0, ++dwSwitchCount);
                            goto RetrySuspension;
                        }
#ifdef TIME_SUSPEND
                        else
                            g_SuspendStatistics.cntRedirections++;
#endif
                        STRESS_LOG1(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime() -   Thread %p redirected().\n", thread);
                    }
                }
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

                FastInterlockOr((ULONG *) &thread->m_State, Thread::TS_GCSuspendPending);

                countThreads++;

                // Only resume if we actually suspended the thread above.
                if (str == Thread::STR_Success)
                    thread->ResumeThread();

                STRESS_LOG1(LF_SYNC, LL_INFO1000, "    Thread 0x%x is in cooperative needs to rendezvous\n", thread);
            }
            else
            if (str == Thread::STR_Success)
            {
                STRESS_LOG1(LF_SYNC, LL_WARNING, "    Inspecting thread 0x%x was in cooperative, but now is not\n", thread);
                // Oops.
                thread->ResumeThread();
            }
            else
            if (str == Thread::STR_SwitchedOut) {
                STRESS_LOG1(LF_SYNC, LL_WARNING, "    Inspecting thread 0x%x was in cooperative, but now is switched out\n", thread);
            }
            else {
                _ASSERTE(str == Thread::STR_Failure || str == Thread::STR_UnstartedOrDead);
                STRESS_LOG3(LF_SYNC, LL_ERROR, "    ERROR: Could not suspend thread 0x%x, result = %d, lastError = 0x%x\n", thread, str, GetLastError());
            }

#endif // DISABLE_THREADSUSPEND

        }
    }

#ifdef _DEBUG

    {
        int     countCheck = 0;
        Thread *InnerThread = NULL;

        while ((InnerThread = ThreadStore::GetThreadList(InnerThread)) != NULL)
        {
            if (InnerThread != pCurThread &&
                (InnerThread->m_State & Thread::TS_GCSuspendPending) != 0)
            {
                countCheck++;
            }
        }
        _ASSERTE(countCheck == countThreads);
    }

#endif

    //
    // Now we keep retrying until we find that no threads are in cooperative mode.  This should be merged into 
    // the first loop.
    //
    while (countThreads)
    {
        _ASSERTE (thread == NULL);
        STRESS_LOG1(LF_SYNC, LL_INFO1000, "    A total of %d threads need to rendezvous\n", countThreads);
        while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
        {
            if (thread == pCurThread)
                continue;

            if (thread->HasThreadState(Thread::TS_BlockGCForSO))
            {
                // The thread is trying to block for GC.  But we don't have enough stack to do
                // this operation.
                // We will let the thread switch back to cooperative mode, and continue running.
                if (thread->m_fPreemptiveGCDisabled.Load() == 0)
                {
                    if (!thread->HasThreadState(Thread::TS_GCSuspendPending))
                    {
                        thread->SetThreadState(Thread::TS_GCSuspendPending);
                        countThreads ++;
                    }
                    thread->ResetThreadState(Thread::TS_BlockGCForSO);
                    FastInterlockOr (&thread->m_fPreemptiveGCDisabled, 1);
                }
                continue;
            }
            if ((thread->m_State & Thread::TS_GCSuspendPending) == 0)
                continue;

            if (!thread->m_fPreemptiveGCDisabled)
            {
                // Inlined N/Direct can sneak out to preemptive without actually checking.
                // If we find one, we can consider it suspended (since it can't get back in).
                STRESS_LOG1(LF_SYNC, LL_INFO1000, "    Thread %x went preemptive it is at a GC safe point\n", thread);
                countThreads--;
                thread->ResetThreadState(Thread::TS_GCSuspendPending);
            }
        }

        if (countThreads == 0)
        {
            break;
        }

#ifdef _DEBUG
        DWORD dbgStartTimeout = GetTickCount();
#endif

        // If another thread is trying to do a GC, there is a chance of deadlock
        // because this thread holds the threadstore lock and the GC thread is stuck
        // trying to get it, so this thread must bail and do a retry after the GC completes.
        //
        // <REVISIT> Shouldn't we do this only if *this* thread isn't attempting a GC?  We're mostly 
        //  done suspending the EE at this point - why give up just because another thread wants
        //  to do exactly the same thing?  Note that GetGCThreadAttemptingSuspend will never (AFAIK)
        //  return the current thread here, because we NULL it out after obtaining the thread store lock. </REVISIT>
        //
        if (m_pThreadAttemptingSuspendForGC != NULL && m_pThreadAttemptingSuspendForGC != pCurThread)
        {
#ifdef PROFILING_SUPPORTED
            // Must let the profiler know that this thread is aborting its attempt at suspending
            {
                BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
                g_profControlBlock.pProfInterface->RuntimeSuspendAborted();
                END_PIN_PROFILER();
            }
#endif // PROFILING_SUPPORTED

            STRESS_LOG0(LF_SYNC, LL_ALWAYS, "Thread::SuspendRuntime() - Timing out.\n");
            return (ERROR_TIMEOUT);
        }

#ifdef TIME_SUSPEND
        DWORD startWait = g_SuspendStatistics.GetTime();
#endif

        // 
        // Wait for at least one thread to tell us it's left cooperative mode.  
        // we do this by waiting on g_pGCSuspendEvent.  We cannot simply wait forever, because we
        // might have done return-address hijacking on a thread, and that thread might not
        // return from the method we hijacked (maybe it calls into some other managed code that
        // executes a long loop, for example).  We we wait with a timeout, and retry hijacking/redirection.
        //
        // This is unfortunate, because it means that in some cases we wait for PING_JIT_TIMEOUT
        // milliseconds, causing long GC pause times.
        //
        // We should fix this, by calling SwitchToThread/Sleep(0) a few times before waiting on the event.
        // This will not fix it 100% of the time (we may still have to wait on the event), but 
        // the event is needed to work around limitations of SwitchToThread/Sleep(0).  
        //
        // For now, we simply wait.
        //

        res = g_pGCSuspendEvent->Wait(PING_JIT_TIMEOUT, FALSE);


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
                _ASSERTE (thread == NULL);
                while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
                {
                    if (thread == pCurThread)
                        continue;

                    if ((thread->m_State & Thread::TS_GCSuspendPending) == 0)
                        continue;

                    if (thread->m_fPreemptiveGCDisabled)
                    {
                        DWORD id = thread->m_OSThreadId;
                        if (id == 0xbaadf00d)
                        {
                            sprintf_s (message, COUNTOF(message), "Thread CLR ID=%x cannot be suspended",
                                     thread->GetThreadId());
                        }
                        else
                        {
                            sprintf_s (message, COUNTOF(message), "Thread OS ID=%x cannot be suspended",
                                     id);
                        }
                        DbgAssertDialog(__FILE__, __LINE__, message);
                    }
                }
                // if we continue from the assert we'll reset the time
                dbgStartTimeout = GetTickCount();
            }
#endif

#if defined(FEATURE_HIJACK) && defined(PLATFORM_UNIX)
            _ASSERTE (thread == NULL);
            while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
            {
                if (thread == pCurThread)
                    continue;

                if ((thread->m_State & Thread::TS_GCSuspendPending) == 0)
                    continue;

                if (!thread->m_fPreemptiveGCDisabled)
                    continue;

                // When we tried to inject the suspension before, we may have been in a place
                // where it wasn't possible. Try one more time.
                bool gcSuspensionSignalSuccess = thread->InjectGcSuspension();
                if (!gcSuspensionSignalSuccess)
                {
                    // If we failed to raise the signal for some reason, just log it and move on.
                    STRESS_LOG1(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime() -   Failed to raise GC suspension signal for thread %p.\n", thread);
                }
            }
#endif

#ifndef DISABLE_THREADSUSPEND
            // all these threads should be in cooperative mode unless they have
            // set their SafeEvent on the way out.  But there's a race between
            // when we time out and when they toggle their mode, so sometimes
            // we will suspend a thread that has just left.
            _ASSERTE (thread == NULL);
            while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
            {
                if (thread == pCurThread)
                    continue;

                if ((thread->m_State & Thread::TS_GCSuspendPending) == 0)
                    continue;

                if (!thread->m_fPreemptiveGCDisabled)
                    continue;

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
            RetrySuspension2:
#endif
                // We can not allocate memory after we suspend a thread.
                // Otherwise, we may deadlock the process when CLR is hosted.
                ThreadStore::AllocateOSContext();

#ifdef TIME_SUSPEND
                DWORD startSuspend = g_SuspendStatistics.GetTime();
#endif

                Thread::SuspendThreadResult str = thread->SuspendThread();

#ifdef TIME_SUSPEND
                g_SuspendStatistics.osSuspend.Accumulate(
                    SuspendStatistics::GetElapsed(startSuspend,
                                                  g_SuspendStatistics.GetTime()));

                if (str == Thread::STR_Success)
                    g_SuspendStatistics.cntOSSuspendResume++;
                else
                    g_SuspendStatistics.cntFailedSuspends++;
#endif

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
                // Only check HandledJITCase if we actually suspended the thread, and
                // the thread is in cooperative mode.
                // See comment at the previous invocation of HandledJITCase - it does
                // more than you think!
                if (str == Thread::STR_Success && thread->m_fPreemptiveGCDisabled)
                {
                    Thread::WorkingOnThreadContextHolder workingOnThreadContext(thread);
                    if (workingOnThreadContext.Acquired() && thread->HandledJITCase())
                    {
                        // Redirect thread so we can capture a good thread context
                        // (GetThreadContext is not sufficient, due to an OS bug).
                        if (!thread->CheckForAndDoRedirectForGC())
                        {
#ifdef TIME_SUSPEND
                            g_SuspendStatistics.cntFailedRedirections++;
#endif
                            STRESS_LOG1(LF_SYNC, LL_INFO1000, "Failed to CheckForAndDoRedirectForGC(). Retry suspension 2 for thread %p\n", thread);
                            thread->ResumeThread();
                            goto RetrySuspension2;
                        }
#ifdef TIME_SUSPEND
                        else
                            g_SuspendStatistics.cntRedirections++;
#endif
                    }
                }
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

                if (str == Thread::STR_Success)
                    thread->ResumeThread();
            }
#endif // DISABLE_THREADSUSPEND
        }
        else
        if (res == WAIT_OBJECT_0)
        {
            g_pGCSuspendEvent->Reset();
            continue;
        }
        else
        {
            // No WAIT_FAILED, WAIT_ABANDONED, etc.
            _ASSERTE(!"unexpected wait termination during gc suspension");
        }
    }

#ifdef PROFILING_SUPPORTED
    // If a profiler is keeping track of GC events, notify it
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
        g_profControlBlock.pProfInterface->RuntimeSuspendFinished();
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED

#ifdef _DEBUG
    if (reason == ThreadSuspend::SUSPEND_FOR_GC) {
        thread = NULL;
        while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
        {
            thread->DisableStressHeap();
            _ASSERTE (!thread->HasThreadState(Thread::TS_GCSuspendPending));
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
    thread = NULL;
    while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
    {
        thread->CommitGCStressInstructionUpdate();
    }
#endif // HAVE_GCCOVER

    STRESS_LOG0(LF_SYNC, LL_INFO1000, "Thread::SuspendRuntime() - Success\n");
    return S_OK;
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

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        *pbDestCode = *pbSrcCode;

#elif defined(_TARGET_ARM_)

        if (GetARMInstructionLength(pbDestCode) == 2)
            *(WORD*)pbDestCode  = *(WORD*)pbSrcCode;
        else
            *(DWORD*)pbDestCode = *(DWORD*)pbSrcCode;

#elif defined(_TARGET_ARM64_)

        *(DWORD*)pbDestCode = *(DWORD*)pbSrcCode;

#else

        *pbDestCode = *pbSrcCode;

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
void ThreadSuspend::ResumeRuntime(BOOL bFinishedGC, BOOL SuspendSucceded)
{
    CONTRACTL {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    Thread  *pCurThread = GetThread();

    // Caller is expected to be holding the ThreadStore lock.  But they must have
    // reset GcInProgress, or threads will continue to suspend themselves and won't
    // be resumed until the next GC.
    _ASSERTE(IsGCSpecialThread() || ThreadStore::HoldingThreadStore());
    _ASSERTE(!GCHeapUtilities::IsGCInProgress() );

    STRESS_LOG2(LF_SYNC, LL_INFO1000, "Thread::ResumeRuntime(finishedGC=%d, SuspendSucceeded=%d) - Start\n", bFinishedGC, SuspendSucceded);

    //
    // Notify everyone who cares, that this suspension is over, and this thread is going to go do other things.
    //


#ifdef PROFILING_SUPPORTED
    // Need to give resume event for the GC thread
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
        if (pCurThread)
        {
            g_profControlBlock.pProfInterface->RuntimeThreadResumed((ThreadID)pCurThread);
        }
        END_PIN_PROFILER();
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
        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->RuntimeResumeFinished();
        END_PIN_PROFILER();
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

#ifndef FEATURE_PAL
#ifdef _TARGET_X86_
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
    _ASSERTE(pThread);


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
#endif // !FEATURE_PAL

// Resume a thread at this location, to persuade it to throw a ThreadStop.  The
// exception handler needs a reasonable idea of how large this method is, so don't
// add lots of arbitrary code here.
void
ThrowControlForThread(
#ifdef WIN64EXCEPTIONS
        FaultingExceptionFrame *pfef
#endif // WIN64EXCEPTIONS
        )
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

    Thread *pThread = GetThread();
    _ASSERTE(pThread);
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
#ifndef WIN64EXCEPTIONS
            __try{
                RaiseException(BOOTUP_EXCEPTION_COMPLUS,0,0,NULL);
            }
            __except(RedirectedThrowControlExceptionFilter(GetExceptionInformation()))
            {
                _ASSERTE(!"Should not reach here");
            }
#else // WIN64EXCEPTIONS
            RtlRestoreContext(pThread->m_OSContext, NULL);
#endif // !WIN64EXCEPTIONS
            _ASSERTE(!"Should not reach here");
        }
        pThread->SetThrowControlForThread(Thread::InducedThreadStop);
    }

#if defined(WIN64EXCEPTIONS)
    *(TADDR*)pfef = FaultingExceptionFrame::GetMethodFrameVPtr();
    *pfef->GetGSCookiePtr() = GetProcessGSCookie();
#else // WIN64EXCEPTIONS
    FrameWithCookie<FaultingExceptionFrame> fef;
    FaultingExceptionFrame *pfef = &fef;
#endif // WIN64EXCEPTIONS
    pfef->InitAndLink(pThread->m_OSContext);

    // !!! Can not assert here.  Sometimes our EHInfo for catch clause extends beyond
    // !!! Jit_EndCatch.  Not sure if we have guarantee on catch clause.
    //_ASSERTE (pThread->ReadyForAbort());

    STRESS_LOG0(LF_SYNC, LL_INFO100, "ThrowControlForThread Aborting\n");

    // Here we raise an exception.
    RaiseComPlusException();
}

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
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

// Threads suspended by the Win32 ::SuspendThread() are resumed in two ways.  If we
// suspended them in error, they are resumed via the Win32 ::ResumeThread().  But if
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

#ifdef _TARGET_AMD64_
        // We need to establish the return value on the stack in the redirection stub, to
        // achieve crawlability.  We use 'rcx' as the way to communicate the return value.
        // However, we are going to crawl in ReadyForAbort and we are going to resume in
        // ThrowControlForThread using m_OSContext.  It's vital that the original correct
        // Rcx is present at those times, or we will have corrupted Rcx at the point of
        // resumption.
        UINT_PTR    keepRcx = m_OSContext->Rcx;

        m_OSContext->Rcx = (UINT_PTR)resumePC;
#endif // _TARGET_AMD64_

#if defined(_TARGET_ARM_)
        // We save the original ControlPC in LR on ARM.
        UINT_PTR originalLR = m_OSContext->Lr;
        m_OSContext->Lr = (UINT_PTR)resumePC;

        // Since we have set a new IP, we have to clear conditional execution flags too.
        UINT_PTR originalCpsr = m_OSContext->Cpsr;
        ClearITState(m_OSContext);
#endif // _TARGET_ARM_

        EESetThreadContext(this, m_OSContext);

#ifdef _TARGET_ARM_
        // Restore the original LR now that the OS context has been updated to resume @ redirection function.
        m_OSContext->Lr = originalLR;
        m_OSContext->Cpsr = originalCpsr;
#endif // _TARGET_ARM_

#ifdef _TARGET_AMD64_
        // and restore.
        m_OSContext->Rcx = keepRcx;
#endif // _TARGET_AMD64_

        SetIP(m_OSContext, resumePC);

        fSuccess = TRUE;
    }
#if _DEBUG
    else
        _ASSERTE(!"Couldn't obtain thread context -- StopRequest delayed");
#endif
    return fSuccess;
}

#endif // FEATURE_HIJACK && !PLATFORM_UNIX


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

    Thread  *pCurThread = GetThread();
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

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
        DWORD dwSwitchCount = 0;
    RetrySuspension:
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

        // We can not allocate memory after we suspend a thread.
        // Otherwise, we may deadlock the process when CLR is hosted.
        ThreadStore::AllocateOSContext();

#ifdef DISABLE_THREADSUSPEND
        // On platforms that do not support safe thread suspension we have
        // to rely on the GCPOLL mechanism.
        
        // When we do not suspend the target thread we rely on the GCPOLL
        // mechanism enabled by TrapReturningThreads.  However when reading
        // shared state we need to erect appropriate memory barriers. So
        // the interlocked operation below ensures that any future reads on 
        // this thread will happen after any earlier writes on a different 
        // thread.
        SuspendThreadResult str = STR_Success;
        FastInterlockOr(&thread->m_fPreemptiveGCDisabled, 0);
#else
        SuspendThreadResult str = thread->SuspendThread();
#endif // DISABLE_THREADSUSPEND

        if (thread->m_fPreemptiveGCDisabled && str == STR_Success)
        {

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
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
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

            // Remember that this thread will be running to a safe point
            FastInterlockIncrement(&m_DebugWillSyncCount);

            // When the thread reaches a safe place, it will wait
            // on the DebugSuspendEvent which clients can set when they
            // want to release us.
            thread->MarkForSuspension(TS_DebugSuspendPending |
                                       TS_DebugWillSync
                      );

#ifdef DISABLE_THREADSUSPEND
            // There'a a race above between the moment we first check m_fPreemptiveGCDisabled
            // and the moment we enable TrapReturningThreads in MarkForSuspension.  However, 
            // nothing bad happens if the thread has transitioned to preemptive before marking 
            // the thread for suspension; the thread will later be identified as Synced in
            // SysSweepThreadsForDebug
#else  // DISABLE_THREADSUSPEND
            // Resume the thread and let it run to a safe point
            thread->ResumeThread();
#endif // DISABLE_THREADSUSPEND

            LOG((LF_CORDB, LL_INFO1000,
                 "[0x%x] SUSPEND: gc disabled - will sync.\n",
                 thread->GetThreadId()));
        }
        else if (!thread->m_fPreemptiveGCDisabled)
        {
            // Mark threads that are outside the Runtime so that if
            // they attempt to re-enter they will trip.
            thread->MarkForSuspension(TS_DebugSuspendPending);

#ifdef DISABLE_THREADSUSPEND
            // There'a a race above between the moment we first check m_fPreemptiveGCDisabled
            // and the moment we enable TrapReturningThreads in MarkForSuspension.  To account 
            // for that we check whether the thread moved into cooperative mode, and if it had
            // we mark it as a DebugWillSync thread, that will be handled later in 
            // SysSweepThreadsForDebug
            if (thread->m_fPreemptiveGCDisabled)
            {
                // Remember that this thread will be running to a safe point
                FastInterlockIncrement(&m_DebugWillSyncCount);
                thread->SetThreadState(TS_DebugWillSync);
            }
#else  // DISABLE_THREADSUSPEND
            if (str == STR_Success) {
                thread->ResumeThread();
            }
#endif // DISABLE_THREADSUSPEND

            LOG((LF_CORDB, LL_INFO1000,
                 "[0x%x] SUSPEND: gc enabled.\n", thread->GetThreadId()));
        }
    }

    //
    // Return true if all threads are synchronized now, otherwise the
    // debugge must wait for the SuspendComplete, called from the last
    // thread to sync.
    //

    if (FastInterlockDecrement(&m_DebugWillSyncCount) < 0)
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
// synchronize. Its used to chase down threads that are not syncronizing quickly. It returns true if all the threads are
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
        PRECONDITION(GetThread() == NULL);

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

#ifdef DISABLE_THREADSUSPEND

        // On platforms that do not support safe thread suspension we have
        // to rely on the GCPOLL mechanism.
        
        // When we do not suspend the target thread we rely on the GCPOLL
        // mechanism enabled by TrapReturningThreads.  However when reading
        // shared state we need to erect appropriate memory barriers. So
        // the interlocked operation below ensures that any future reads on 
        // this thread will happen after any earlier writes on a different 
        // thread.
        FastInterlockOr(&thread->m_fPreemptiveGCDisabled, 0);
        if (!thread->m_fPreemptiveGCDisabled)
        {
            // If the thread toggled to preemptive mode, then it's synced.
            goto Label_MarkThreadAsSynced;
        }
        else
        {
            continue;
        }

#else // DISABLE_THREADSUSPEND
        // Suspend the thread

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
        DWORD dwSwitchCount = 0;
#endif

RetrySuspension:
        // We can not allocate memory after we suspend a thread.
        // Otherwise, we may deadlock the process when CLR is hosted.
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
        else if (str == STR_SwitchedOut)
        {
            // The thread was switched b/c of fiber-mode stuff.
            if (!thread->m_fPreemptiveGCDisabled)
            {
                goto Label_MarkThreadAsSynced;
            }
            else
            {
                goto RetrySuspension;
            }
        }
        else if (str == STR_NoStressLog)
        {
            goto RetrySuspension;
        }
        else if (!thread->m_fPreemptiveGCDisabled)
        {
            // If the thread toggled to preemptive mode, then it's synced.

            // We can safely resume the thread here b/c it's in PreemptiveMode and the
            // EE will trap anybody trying to re-enter cooperative. So letting it run free
            // won't hurt the runtime.
            _ASSERTE(str == STR_Success);
            thread->ResumeThread();

            goto Label_MarkThreadAsSynced;
        }
#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
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
                // if the thread is hijacked, it's as good as synced, so mark it now.
                thread->ResumeThread();
                goto Label_MarkThreadAsSynced;
            }
        }
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

        // If we didn't take the thread out of the set, then resume it and give it another chance to reach a safe
        // point.
        thread->ResumeThread();
        continue;

#endif // DISABLE_THREADSUSPEND

        // The thread is synced. Remove the sync bits and dec the sync count.
Label_MarkThreadAsSynced:
        FastInterlockAnd((ULONG *) &thread->m_State, ~TS_DebugWillSync);
        if (FastInterlockDecrement(&m_DebugWillSyncCount) < 0)
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

#ifdef _TARGET_ARM_
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

        // CoreCLR does not support user-requested thread suspension
        _ASSERTE(!(m_State & TS_UserSuspendPending));

        if (m_State & TS_DebugSuspendPending) {

            ThreadState oldState = m_State;

            while (oldState & TS_DebugSuspendPending) {

                ThreadState newState = (ThreadState)(oldState | TS_SyncSuspended);
                if (FastInterlockCompareExchange((LONG *)&m_State, newState, oldState) == (LONG)oldState)
                {
                    result = m_DebugSuspendEvent.Wait(INFINITE,FALSE);
#if _DEBUG
                    newState = m_State;
                    _ASSERTE(!(newState & TS_SyncSuspended) || (newState & TS_UserSuspendPending));
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

            // CoreCLR does not support user-requested thread suspension
            _ASSERTE(!(oldState & TS_UserSuspendPending));

            //
            // If all reasons to suspend are off, we think we can exit
            // this loop, but we need to check atomically.
            //
            if ((oldState & (TS_UserSuspendPending | TS_DebugSuspendPending)) == 0)
            {
                //
                // Construct the destination state we desire - all suspension bits turned off.
                //
                ThreadState newState = (ThreadState)(oldState & ~(TS_UserSuspendPending |
                                                                  TS_DebugSuspendPending |
                                                                  TS_SyncSuspended));

                if (FastInterlockCompareExchange((LONG *)&m_State, newState, oldState) == (LONG)oldState)
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
void Thread::HijackThread(VOID *pvHijackAddr, ExecutionState *esb)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

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

    IS_VALID_CODE_PTR((FARPROC) pvHijackAddr);

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
    FastInterlockOr((ULONG *) &m_State, TS_Hijacked);
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
        FastInterlockAnd((ULONG *) &m_State, ~TS_Hijacked);

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
#ifdef WIN64EXCEPTIONS
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
#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)

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

                        if(pRDT->pCallerContextPointers->Lr == &pRDT->pContext->Lr)
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
                            pES->m_ppvRetAddrPtr = (void **) pRDT->pCallerContextPointers->Lr;
                        }
#elif defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
                        pES->m_ppvRetAddrPtr = (void **) (EECodeManager::GetCallerSp(pRDT) - sizeof(void*));
#else // _TARGET_X86_ || _TARGET_AMD64_
                        PORTABILITY_ASSERT("Platform NYI");
#endif // _TARGET_???_
                    }
#else // WIN64EXCEPTIONS
                    // peel off the next frame to expose the return address on the stack
                    pES->m_FirstPass = FALSE;
                    action = SWA_CONTINUE;
#endif // !WIN64EXCEPTIONS
                }
#endif // HIJACK_NONINTERRUPTIBLE_THREADS
            }
            // else we are successfully out of here with SWA_ABORT
        }
        else
        {
#ifdef _TARGET_X86_
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
#if defined(_TARGET_X86_) && !defined(WIN64EXCEPTIONS)
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
#else
    PORTABILITY_ASSERT("OnHijackWorker not implemented on this platform.");
#endif // HIJACK_NONINTERRUPTIBLE_THREADS
}

ReturnKind GetReturnKindFromMethodTable(Thread *pThread, EECodeInfo *codeInfo)
{
#ifdef _WIN64
    // For simplicity, we don't hijack in funclets, but if you ever change that, 
    // be sure to choose the OnHijack... callback type to match that of the FUNCLET
    // not the main method (it would probably be Scalar).
#endif // _WIN64

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();
    // Mark that we are performing a stackwalker like operation on the current thread.
    // This is necessary to allow the signature parsing functions to work without triggering any loads
    ClrFlsValueSwitch threadStackWalking(TlsIdx_StackWalkerWalkingThread, pThread);

    MethodDesc *methodDesc = codeInfo->GetMethodDesc();
    _ASSERTE(methodDesc != nullptr);

#ifdef _TARGET_X86_
    MetaSig msig(methodDesc);
    if (msig.HasFPReturn())
    {
        // Figuring out whether the function returns FP or not is hard to do
        // on-the-fly, so we use a different callback helper on x86 where this
        // piece of information is needed in order to perform the right save &
        // restore of the return value around the call to OnHijackScalarWorker.
        return RT_Float;
    }
#endif // _TARGET_X86_

    MethodTable* pMT = NULL;
    MetaSig::RETURNTYPE type = methodDesc->ReturnsObject(INDEBUG_COMMA(false) &pMT);
    if (type == MetaSig::RETOBJ)
    {
        return RT_Object;
    }

    if (type == MetaSig::RETBYREF)
    {
        return RT_ByRef;
    }

#ifdef UNIX_AMD64_ABI
    // The Multi-reg return case using the classhandle is only implemented for AMD64 SystemV ABI.
    // On other platforms, multi-reg return is not supported with GcInfo v1.
    // So, the relevant information must be obtained from the GcInfo tables (which requires version2).
    if (type == MetaSig::RETVALUETYPE)
    {
        EEClass *eeClass = pMT->GetClass();
        ReturnKind regKinds[2] = { RT_Unset, RT_Unset };
        int orefCount = 0;
        for (int i = 0; i < 2; i++)
        {
            if (eeClass->GetEightByteClassification(i) == SystemVClassificationTypeIntegerReference)
            {
                regKinds[i] = RT_Object;
            }
            else if (eeClass->GetEightByteClassification(i) == SystemVClassificationTypeIntegerByRef)
            {
                regKinds[i] = RT_ByRef;
            }
            else
            {
                regKinds[i] = RT_Scalar;
            }
        }
        ReturnKind structReturnKind = GetStructReturnKind(regKinds[0], regKinds[1]);
        return structReturnKind;
    }
#endif // UNIX_AMD64_ABI

    return RT_Scalar;
}

ReturnKind GetReturnKind(Thread *pThread, EECodeInfo *codeInfo)
{
    GCInfoToken gcInfoToken = codeInfo->GetGCInfoToken();
    ReturnKind returnKind = codeInfo->GetCodeManager()->GetReturnKind(gcInfoToken);

    if (!IsValidReturnKind(returnKind))
    {
        returnKind = GetReturnKindFromMethodTable(pThread, codeInfo);
    }
    else
    {
#if !defined(FEATURE_MULTIREG_RETURN) || defined(UNIX_AMD64_ABI)
         // For ARM64 struct-return, GetReturnKindFromMethodTable() is not supported
        _ASSERTE(returnKind == GetReturnKindFromMethodTable(pThread, codeInfo));
#endif // !FEATURE_MULTIREG_RETURN || UNIX_AMD64_ABI
    }

    _ASSERTE(IsValidReturnKind(returnKind));
    return returnKind;
}

VOID * GetHijackAddr(Thread *pThread, EECodeInfo *codeInfo)
{
    ReturnKind returnKind = GetReturnKind(pThread, codeInfo);
    pThread->SetHijackReturnKind(returnKind);

#ifdef _TARGET_X86_
    if (returnKind == RT_Float)
    {
        return reinterpret_cast<VOID *>(OnHijackFPTripThread);
    }
#endif // _TARGET_X86_

    return reinterpret_cast<VOID *>(OnHijackTripThread);
}

#ifndef PLATFORM_UNIX

// Get the ExecutionState for the specified SwitchIn thread.  Note that this is
// a 'StackWalk' call back (PSTACKWALKFRAMESCALLBACK).
StackWalkAction SWCB_GetExecutionStateForSwitchIn(CrawlFrame *pCF, VOID *pData)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ExecutionState  *pES = (ExecutionState *) pData;
    StackWalkAction  action = SWA_CONTINUE;

    if (pES->m_FirstPass) {
        if (pCF->IsFrameless()) {
#ifdef _TARGET_X86_
            pES->m_FirstPass = FALSE;
#else
            _ASSERTE(!"Platform NYI");
#endif

            pES->m_IsJIT = TRUE;
            pES->m_pFD = pCF->GetFunction();
            pES->m_MethodToken = pCF->GetMethodToken();
            // We do not care if the code is interruptible
            pES->m_IsInterruptible = FALSE;
            pES->m_RelOffset = pCF->GetRelOffset();
            pES->m_pJitManager = pCF->GetJitManager();
        }
    }
    else {
#ifdef _TARGET_X86_
        if (pCF->IsFrameless()) {
            PREGDISPLAY     pRDT = pCF->GetRegisterSet();
            if (pRDT) {
                // pPC points to the return address sitting on the stack, as our
                // current EIP for the penultimate stack frame.
                pES->m_ppvRetAddrPtr = (void **) pRDT->PCTAddr;
                action = SWA_ABORT;
            }
        }
#else
        _ASSERTE(!"Platform NYI");
#endif
    }
    return action;
}

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
// In HandledJITCase, if we see that a thread's Eip is in managed code at an interruptable point, we will attempt
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

#ifdef _TARGET_X86_

#ifndef FEATURE_PAL
#define WORKAROUND_RACES_WITH_KERNEL_MODE_EXCEPTION_HANDLING
#endif // !FEATURE_PAL

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

    // There are cases where the ESP is just decremented but the page is not touched, thus the page is not commited or
    // still has page guard bit set. We can't hit the race in such case so we just leave. Besides, we can't access the
    // memory with page guard flag or not committed.
    MEMORY_BASIC_INFORMATION mbi;
#undef VirtualQuery
    // This code can run below YieldTask, which means that it must not call back into the host.
    // The reason is that YieldTask is invoked by the host, and the host needs not be reentrant.
    if (VirtualQuery((LPCVOID)(UINT_PTR)ctx->Esp, &mbi, sizeof(mbi)) == sizeof(mbi))
    {
        if (!(mbi.State & MEM_COMMIT) || (mbi.Protect & PAGE_GUARD))
            return FALSE;
    }
    else
        STRESS_LOG0 (LF_SYNC, ERROR, "VirtualQuery failed!");
#define VirtualQuery(lpAddress, lpBuffer, dwLength) Dont_Use_VirtualQuery(lpAddress, lpBuffer, dwLength)

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
#endif //_TARGET_X86_

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
        if (CORDebuggerAttached() && (g_pDebugInterface->IsThreadContextInvalid(this)))
            return FALSE;
    }
#endif // DEBUGGING_SUPPORTED

    // Make sure we specify CONTEXT_EXCEPTION_REQUEST to detect "trap frame reporting".
    _ASSERTE(GetFilterContext() == NULL);

    ZeroMemory(pCtx, sizeof(*pCtx));
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
#ifdef _TARGET_X86_
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
BOOL Thread::HandledJITCase(BOOL ForTaskSwitchIn)
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
    Thread * pCurThread = GetThread();
    if (pCurThread != NULL)
    {
        pCurThread->dbg_m_cSuspendedThreadsWithoutOSLock ++;
        _ASSERTE(pCurThread->dbg_m_cSuspendedThreadsWithoutOSLock <= pCurThread->dbg_m_cSuspendedThreads);
    }
#endif //_DEBUG
    
    // Walk one or two frames of the stack...
    if (ForTaskSwitchIn) {
        action = StackWalkFramesEx(&rd,SWCB_GetExecutionStateForSwitchIn, &esb, QUICKUNWIND | DISABLE_MISSING_FRAME_DETECTION | THREAD_IS_SUSPENDED | ALLOW_ASYNC_STACK_WALK, NULL);
    }
    else {
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
    }

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
            VOID *pvHijackAddr = GetHijackAddr(this, &codeInfo);

#ifdef FEATURE_ENABLE_GCPOLL
            // On platforms that support both hijacking and GC polling
            // decide whether to hijack based on a configuration value.  
            // COMPlus_GCPollType = 1 is the setting that enables hijacking
            // in GCPOLL enabled builds.
            EEConfig::GCPollType pollType = g_pConfig->GetGCPollType();
            if (EEConfig::GCPOLL_TYPE_HIJACK == pollType || EEConfig::GCPOLL_TYPE_DEFAULT == pollType)
#endif // FEATURE_ENABLE_GCPOLL
            {
                HijackThread(pvHijackAddr, &esb);
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

#endif // !PLATFORM_UNIX

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

    FastInterlockOr((ULONG *) &m_State, bit);
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
    FastInterlockAnd((ULONG *) &m_State, mask);
}

//----------------------------------------------------------------------------

void ThreadSuspend::RestartEE(BOOL bFinishedGC, BOOL SuspendSucceded)
{
#ifdef TIME_SUSPEND
    g_SuspendStatistics.StartRestart();
#endif //TIME_SUSPEND

    FireEtwGCRestartEEBegin_V1(GetClrInstanceId());

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
        BEGIN_PIN_PROFILER(CORProfilerTrackSuspends());
        g_profControlBlock.pProfInterface->RuntimeResumeStarted();
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED

    //
    // Unhijack all threads, and reset their "suspend pending" flags.  Why isn't this in
    // Thread::ResumeRuntime?
    //
    Thread  *thread = NULL;
    while ((thread = ThreadStore::GetThreadList(thread)) != NULL)
    {
        thread->PrepareForEERestart(SuspendSucceded);
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

    ResumeRuntime(bFinishedGC, SuspendSucceded);

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

    Thread* pCurThread = GetThread();

    DWORD dwSwitchCount = 0;

    // Note: we need to make sure to re-set m_pThreadAttemptingSuspendForGC when we retry
    // due to the debugger case below!
retry_for_debugger:

    //
    // Set variable to indicate that this thread is preforming a true GC
    // This gives this thread priority over other threads that are trying to acquire the ThreadStore Lock
    // for other reasons.
    //
    if (reason == ThreadSuspend::SUSPEND_FOR_GC || reason == ThreadSuspend::SUSPEND_FOR_GC_PREP)
    {
        m_pThreadAttemptingSuspendForGC = pCurThread;

        //
        // also unblock any thread waiting around for this thread to suspend. This prevents us from completely
        // starving other suspension clients, such as the debugger, which we otherwise would do because of
        // the priority we just established.
        //
        g_pGCSuspendEvent->Set();
    }

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
    // If we've blocked other threads that are waiting for the ThreadStore lock, unblock them now
    // (since we already got it).  This allows them to get the TSL after we release it.
    //
    if ( s_hAbortEvtCache != NULL &&
        (reason == ThreadSuspend::SUSPEND_FOR_GC || reason == ThreadSuspend::SUSPEND_FOR_GC_PREP))
    {
        LOG((LF_SYNC, INFO3, "GC thread is backing out the suspend abort event.\n"));
        s_hAbortEvt = NULL;

        LOG((LF_SYNC, INFO3, "GC thread is signalling the suspend abort event.\n"));
        s_hAbortEvtCache->Set();
    }

    //
    // Also null-out m_pThreadAttemptingSuspendForGC since it should only matter if s_hAbortEvt is 
    // in play.
    //
    if (reason == ThreadSuspend::SUSPEND_FOR_GC || reason == ThreadSuspend::SUSPEND_FOR_GC_PREP)
    {
        m_pThreadAttemptingSuspendForGC = NULL;
    }

    {
        //
        // Now we're going to acquire an exclusive lock on managed code execution (including
        // "maunally managed" code in GCX_COOP regions).
        //
        // First, we reset the event that we're about to tell other threads to wait for.
        //
        GCHeapUtilities::GetGCHeap()->ResetWaitForGCEvent();

        //
        // Remember that we're the one doing the GC.  Actually, maybe we're not doing a GC -
        // what this really indicates is that we are trying to acquire the "managed execution lock."
        //
        {
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
            // It seems like much of the above is redundant.  We should investigate reducing the number
            // of mechanisms we use to indicate that a suspension is in progress.
            //
            GCHeapUtilities::GetGCHeap()->SetGCInProgress(true);

            //
            // Gratuitous memory barrier.  (may be needed - but I'm not sure why.)
            //
            MemoryBarrier();

            ClrFlsSetThreadType (ThreadType_DynamicSuspendEE);
        }

        HRESULT hr;
        {
            _ASSERTE(ThreadStore::HoldingThreadStore() || g_fProcessDetach);

            //
            // Now that we've instructed all threads to please stop, 
            // go interrupt the ones that are running managed code and force them to stop.
            // This does not return successfully until all threads have acknowledged that they
            // will not run managed code.
            //
            hr = SuspendRuntime(reason);
            ASSERT( hr == S_OK || hr == ERROR_TIMEOUT);

#ifdef TIME_SUSPEND
            if (hr == ERROR_TIMEOUT)
                g_SuspendStatistics.cntCollideRetry++;
#endif
        }

        if (hr == ERROR_TIMEOUT)
            STRESS_LOG0(LF_SYNC, LL_INFO1000, "SysSuspension colission");

        // If the debugging services are attached, then its possible
        // that there is a thread which appears to be stopped at a gc
        // safe point, but which really is not. If that is the case,
        // back off and try again.

        // If this is not the GC thread and another thread has triggered
        // a GC, then we may have bailed out of SuspendRuntime, so we
        // must resume all of the threads and tell the GC that we are
        // at a safepoint - since this is the exact same behaviour
        // that the debugger needs, just use it's code.
        if ((hr == ERROR_TIMEOUT)
            || Thread::ThreadsAtUnsafePlaces()
#ifdef DEBUGGING_SUPPORTED  // seriously?  When would we want to disable debugging support? :)
             || (CORDebuggerAttached() && 
            // When the debugger is synchronizing, trying to perform a GC could deadlock. The GC has the
            // threadstore lock and synchronization cannot complete until the debugger can get the 
            // threadstore lock. However the GC can not complete until it sends the BeforeGarbageCollection
            // event, and the event can not be sent until the debugger is synchronized. In order to break
            // this deadlock cycle the GC must give up the threadstore lock, allow the debugger to synchronize, 
            // then try again.
                 (g_pDebugInterface->ThreadsAtUnsafePlaces() || g_pDebugInterface->IsSynchronizing()))
#endif // DEBUGGING_SUPPORTED
            )
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
            // <REVISIT>The below manipulation of two static variables (s_hAbortEvtCache and s_hAbortEvt)
            // is protected by the ThreadStore lock, which we are still holding.  But we access these
            // in ThreadSuspend::LockThreadStore, prior to obtaining the lock. </REVISIT>
            //
            LOG((LF_GCROOTS | LF_GC | LF_CORDB,
                 LL_INFO10,
                 "***** Giving up on current GC suspension due "
                 "to debugger or timeout *****\n"));            

            if (s_hAbortEvtCache == NULL)
            {
                LOG((LF_SYNC, INFO3, "Creating suspend abort event.\n"));

                CLREvent * pEvent = NULL;

                EX_TRY 
                {
                    pEvent = new CLREvent();
                    pEvent->CreateManualEvent(FALSE);
                    s_hAbortEvtCache = pEvent;
                }
                EX_CATCH
                {
                    // Couldn't init the abort event. Its a shame, but not fatal. We'll simply not use it
                    // on this iteration and try again next time.
                    if (pEvent) {
                        _ASSERTE(!pEvent->IsValid());
                        pEvent->CloseEvent();
                        delete pEvent;
                    }
                }
                EX_END_CATCH(SwallowAllExceptions)
            }

            if (s_hAbortEvtCache != NULL)
            {
                LOG((LF_SYNC, INFO3, "Using suspend abort event.\n"));
                s_hAbortEvt = s_hAbortEvtCache;
                s_hAbortEvt->Reset();
            }
            
            // Mark that we're done with the gc, so that the debugger can proceed.
            RestartEE(FALSE, FALSE);            
            
            LOG((LF_GCROOTS | LF_GC | LF_CORDB,
                 LL_INFO10, "The EE is free now...\n"));
            
            // If someone's trying to suspent *this* thread, this is a good opportunity.
            // <REVIST>This call to CatchAtSafePoint is redundant - PulseGCMode already checks this.</REVISIT>
            if (pCurThread && pCurThread->CatchAtSafePoint())
            {
                //  <REVISIT> This assert is fired on BGC thread 'cause we
                // got timeout.</REVISIT>
                //_ASSERTE((pCurThread->PreemptiveGCDisabled()) || IsGCSpecialThread());
                pCurThread->PulseGCMode();  // Go suspend myself.
            }
            else
            {
                // otherwise, just yield so the debugger can finish what it's doing.
                __SwitchToThread (0, ++dwSwitchCount); 
            }

            goto retry_for_debugger;
        }
    }
    GC_ON_TRANSITIONS(gcOnTransitions);

    FireEtwGCSuspendEEEnd_V1(GetClrInstanceId());

#ifdef TIME_SUSPEND
    g_SuspendStatistics.EndSuspend(reason == SUSPEND_FOR_GC || reason == SUSPEND_FOR_GC_PREP);
#endif //TIME_SUSPEND
}

#if defined(FEATURE_HIJACK) && defined(PLATFORM_UNIX)

// This function is called by PAL to check if the specified instruction pointer
// is in a function where we can safely inject activation. 
BOOL CheckActivationSafePoint(SIZE_T ip, BOOL checkingCurrentThread)
{
    Thread *pThread = GetThread();
    // It is safe to call the ExecutionManager::IsManagedCode only if we are making the check for
    // a thread different from the current one or if the current thread is in the cooperative mode.
    // Otherwise ExecutionManager::IsManagedCode could deadlock if the activation happened when the
    // thread was holding the ExecutionManager's writer lock.
    // When the thread is in preemptive mode, we know for sure that it is not executing managed code.
    BOOL checkForManagedCode = !checkingCurrentThread || (pThread != NULL && pThread->PreemptiveGCDisabled());
    return checkForManagedCode && ExecutionManager::IsManagedCode(ip);
}

// This function is called when a GC is pending. It tries to ensure that the current
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
void HandleGCSuspensionForInterruptedThread(CONTEXT *interruptedContext)
{
    Thread *pThread = GetThread();

    if (pThread->PreemptiveGCDisabled() != TRUE)
        return;

#ifdef FEATURE_PERFTRACING
    // Mark that the thread is currently in managed code.
    pThread->SaveGCModeOnSuspension();
#endif // FEATURE_PERFTRACING

    PCODE ip = GetIP(interruptedContext);

    // This function can only be called when the interrupted thread is in 
    // an activation safe point.
    _ASSERTE(CheckActivationSafePoint(ip, /* checkingCurrentThread */ TRUE));

    Thread::WorkingOnThreadContextHolder workingOnThreadContext(pThread);
    if (!workingOnThreadContext.Acquired())
        return;

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
        pThread->SetSavedRedirectContext(NULL);

        frame.Push(pThread);

        pThread->PulseGCMode();

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


        // Calling this turns off the GC_TRIGGERS/THROWS/INJECT_FAULT contract in LoadTypeHandle.
        // We should not trigger any loads for unresolved types.
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

        // Mark that we are performing a stackwalker like operation on the current thread.
        // This is necessary to allow the signature parsing functions to work without triggering any loads.
        ClrFlsValueSwitch threadStackWalking(TlsIdx_StackWalkerWalkingThread, pThread);

        // Hijack the return address to point to the appropriate routine based on the method's return type.
        void *pvHijackAddr = GetHijackAddr(pThread, &codeInfo);
        pThread->HijackThread(pvHijackAddr, &executionState);
    }
}

bool Thread::InjectGcSuspension()
{
    static ConfigDWORD injectionEnabled;
    if (injectionEnabled.val(CLRConfig::INTERNAL_ThreadSuspendInjection) == 0)
        return false;

    Volatile<HANDLE> hThread;
    hThread = GetThreadHandle();
    if (hThread != INVALID_HANDLE_VALUE && hThread != SWITCHOUT_HANDLE_VALUE)
    {
        ::PAL_InjectActivation(hThread);
        return true;
    }

    return false;
}

#endif // FEATURE_HIJACK && PLATFORM_UNIX

// Initialize thread suspension support
void ThreadSuspend::Initialize()
{
#if defined(FEATURE_HIJACK) && defined(PLATFORM_UNIX)
    ::PAL_SetActivationFunction(HandleGCSuspensionForInterruptedThread, CheckActivationSafePoint);
#endif
}

#ifdef _DEBUG
BOOL Debug_IsLockedViaThreadSuspension()
{
    LIMITED_METHOD_CONTRACT;
    return GCHeapUtilities::IsGCInProgress() && 
                    (dbgOnly_IsSpecialEEThread() || 
                    IsGCSpecialThread() || 
                    GetThread() == ThreadSuspend::GetSuspensionThread());
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

    fprintf(logFile, "Redirected EIP Failures %d (%d), Collided GC/Debugger %d (%d)\n",
           cntFailedRedirections - g_LastSuspendStatistics.cntFailedRedirections, cntFailedRedirections,
           cntCollideRetry - g_LastSuspendStatistics.cntCollideRetry, cntCollideRetry);

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

void MinMaxTot::DisplayAndUpdate(FILE* logFile, __in_z const char *pName, MinMaxTot *pLastOne, int fullCount, int priorCount, timeUnit unit /* = usec */)
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
