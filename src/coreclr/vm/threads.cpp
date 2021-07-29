// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// THREADS.CPP
//

//
//


#include "common.h"

#include "frames.h"
#include "threads.h"
#include "stackwalk.h"
#include "excep.h"
#include "comsynchronizable.h"
#include "log.h"
#include "gcheaputilities.h"
#include "mscoree.h"
#include "dbginterface.h"
#include "corprof.h"                // profiling
#include "eeprofinterfaces.h"
#include "eeconfig.h"
#include "corhost.h"
#include "win32threadpool.h"
#include "jitinterface.h"
#include "eventtrace.h"
#include "comutilnative.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#include "wrappers.h"

#include "nativeoverlapped.h"

#include "appdomain.inl"
#include "vmholder.h"
#include "exceptmacros.h"
#include "win32threadpool.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "interoputil.h"
#include "interoputil.inl"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#include "roapi.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef FEATURE_SPECIAL_USER_MODE_APC
#include "asmconstants.h"
#endif

static const PortableTailCallFrame g_sentinelTailCallFrame = { NULL, NULL };

TailCallTls::TailCallTls()
    // A new frame will always be allocated before the frame is modified,
    // so casting away const is ok here.
    : m_frame(const_cast<PortableTailCallFrame*>(&g_sentinelTailCallFrame))
    , m_argBuffer(NULL)
{
}

Thread* STDCALL GetThreadHelper()
{
    return GetThreadNULLOk();
}

TailCallArgBuffer* TailCallTls::AllocArgBuffer(int size, void* gcDesc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    _ASSERTE(size >= (int)offsetof(TailCallArgBuffer, Args));

    if (m_argBuffer != NULL && m_argBuffer->Size < size)
    {
        FreeArgBuffer();
    }

    if (m_argBuffer == NULL)
    {
        m_argBuffer = (TailCallArgBuffer*)new (nothrow) BYTE[size];
        if (m_argBuffer == NULL)
            return NULL;
        m_argBuffer->Size = size;
    }

    m_argBuffer->State = TAILCALLARGBUFFER_ACTIVE;

    m_argBuffer->GCDesc = gcDesc;
    if (gcDesc != NULL)
    {
        memset(m_argBuffer->Args, 0, size - offsetof(TailCallArgBuffer, Args));
    }

    return m_argBuffer;
}

#if defined (_DEBUG_IMPL) || defined(_PREFAST_)
thread_local int t_ForbidGCLoaderUseCount;
#endif

uint64_t Thread::dead_threads_non_alloc_bytes = 0;

SPTR_IMPL(ThreadStore, ThreadStore, s_pThreadStore);

CONTEXT* ThreadStore::s_pOSContext = NULL;
BYTE* ThreadStore::s_pOSContextBuffer = NULL;

CLREvent *ThreadStore::s_pWaitForStackCrawlEvent;

PTR_ThreadLocalModule ThreadLocalBlock::GetTLMIfExists(ModuleIndex index)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (index.m_dwIndex >= m_TLMTableSize)
        return NULL;

    return m_pTLMTable[index.m_dwIndex].pTLM;
}

PTR_ThreadLocalModule ThreadLocalBlock::GetTLMIfExists(MethodTable* pMT)
{
    WRAPPER_NO_CONTRACT;
    ModuleIndex index = pMT->GetModuleForStatics()->GetModuleIndex();
    return GetTLMIfExists(index);
}

#ifndef DACCESS_COMPILE

BOOL Thread::s_fCleanFinalizedThread = FALSE;

UINT64 Thread::s_workerThreadPoolCompletionCountOverflow = 0;
UINT64 Thread::s_ioThreadPoolCompletionCountOverflow = 0;
UINT64 Thread::s_monitorLockContentionCountOverflow = 0;

CrstStatic g_DeadlockAwareCrst;

//
// A transient thread value that indicates this thread is currently walking its stack
// or the stack of another thread. This value is useful to help short-circuit
// some problematic checks in the loader, guarantee that types & assemblies
// encountered during the walk must already be loaded, and provide information to control
// assembly loading behavior during stack walks.
//
// This value is set around the main portions of the stack walk (as those portions may
// enter the type & assembly loaders). This is also explicitly cleared while the
// walking thread calls the stackwalker callback or needs to execute managed code, as
// such calls may execute arbitrary code unrelated to the actual stack walking, and
// may never return, in the case of exception stackwalk callbacks.
//
thread_local Thread* t_pStackWalkerWalkingThread;

#if defined(_DEBUG)
BOOL MatchThreadHandleToOsId ( HANDLE h, DWORD osId )
{
#ifndef TARGET_UNIX
    LIMITED_METHOD_CONTRACT;

    DWORD id = GetThreadId(h);

    // OS call GetThreadId may fail, and return 0.  In this case we can not
    // make a decision if the two match or not.  Instead, we ignore this check.
    return id == 0 || id == osId;
#else // !TARGET_UNIX
    return TRUE;
#endif // !TARGET_UNIX
}
#endif // _DEBUG


#ifdef _DEBUG_IMPL
template<> AutoCleanupGCAssert<TRUE>::AutoCleanupGCAssert()
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_MODE_COOPERATIVE;
}

template<> AutoCleanupGCAssert<FALSE>::AutoCleanupGCAssert()
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
}

template<> void GCAssert<TRUE>::BeginGCAssert()
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_MODE_COOPERATIVE;
}

template<> void GCAssert<FALSE>::BeginGCAssert()
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
}
#endif

// #define     NEW_TLS     1

#ifdef _DEBUG
void  Thread::SetFrame(Frame *pFrame)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
        MODE_COOPERATIVE;
        // It only makes sense for a Thread to call SetFrame on itself.
        PRECONDITION(this == GetThread());
        PRECONDITION(CheckPointer(pFrame));
    }
    CONTRACTL_END;

    if (g_pConfig->fAssertOnFailFast())
    {
        Frame *pWalk = m_pFrame;
        BOOL fExist = FALSE;
        while (pWalk != (Frame*) -1)
        {
            if (pWalk == pFrame)
            {
                fExist = TRUE;
                break;
            }
            pWalk = pWalk->m_Next;
        }
        pWalk = m_pFrame;
        while (fExist && pWalk != pFrame && pWalk != (Frame*)-1)
        {
            pWalk = pWalk->m_Next;
        }
    }

    m_pFrame = pFrame;

    // If stack overrun corruptions are expected, then skip this check
    // as the Frame chain may have been corrupted.
    if (g_pConfig->fAssertOnFailFast() == false)
        return;

    Frame* espVal = (Frame*)GetCurrentSP();

    while (pFrame != (Frame*) -1)
    {
        static Frame* stopFrame = 0;
        if (pFrame == stopFrame)
            _ASSERTE(!"SetFrame frame == stopFrame");

        _ASSERTE(IsExecutingOnAltStack() || espVal < pFrame);
        _ASSERTE(IsExecutingOnAltStack() || pFrame < m_CacheStackBase);
        _ASSERTE(pFrame->GetFrameType() < Frame::TYPE_COUNT);

        pFrame = pFrame->m_Next;
    }
}

#endif // _DEBUG

//************************************************************************
// PRIVATE GLOBALS
//************************************************************************

extern unsigned __int64 getTimeStamp();

extern unsigned __int64 getTickFrequency();

unsigned __int64 tgetFrequency() {
    static unsigned __int64 cachedFreq = (unsigned __int64) -1;

    if (cachedFreq != (unsigned __int64) -1)
        return cachedFreq;
    else {
        cachedFreq = getTickFrequency();
        return cachedFreq;
    }
}

#endif // #ifndef DACCESS_COMPILE

static StackWalkAction DetectHandleILStubsForDebugger_StackWalkCallback(CrawlFrame *pCF, VOID *pData)
{
    WRAPPER_NO_CONTRACT;
    // It suffices to wait for the first CrawlFrame with non-NULL function
    MethodDesc *pMD = pCF->GetFunction();
    if (pMD != NULL)
    {
        *(bool *)pData = pMD->IsILStub();
        return SWA_ABORT;
    }

    return SWA_CONTINUE;
}

// This is really just a heuristic to detect if we are executing in an M2U IL stub or
// one of the marshaling methods it calls.  It doesn't deal with U2M IL stubs.
// We loop through the frame chain looking for an uninitialized TransitionFrame.
// If there is one, then we are executing in an M2U IL stub or one of the methods it calls.
// On the other hand, if there is an initialized TransitionFrame, then we are not.
// Also, if there is an HMF on the stack, then we stop.  This could be the case where
// an IL stub calls an FCALL which ends up in a managed method, and the debugger wants to
// stop in those cases.  Some examples are COMException..ctor and custom marshalers.
//
// X86 IL stubs use InlinedCallFrame and are indistinguishable from ordinary methods with
// inlined P/Invoke when judging just from the frame chain. We use stack walk to decide
// this case.
bool Thread::DetectHandleILStubsForDebugger()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Frame* pFrame = GetFrame();

    if (pFrame != NULL)
    {
        while (pFrame != FRAME_TOP)
        {
            // Check for HMF's.  See the comment at the beginning of this function.
            if (pFrame->GetVTablePtr() == HelperMethodFrame::GetMethodFrameVPtr())
            {
                break;
            }
            // If there is an entry frame (i.e. U2M managed), we should break.
            else if (pFrame->GetFrameType() == Frame::TYPE_ENTRY)
            {
                break;
            }
            // Check for M2U transition frames.  See the comment at the beginning of this function.
            else if (pFrame->GetFrameType() == Frame::TYPE_EXIT)
            {
                if (pFrame->GetReturnAddress() == NULL)
                {
                    // If the return address is NULL, then the frame has not been initialized yet.
                    // We may see InlinedCallFrame in ordinary methods as well. Have to do
                    // stack walk to find out if this is really an IL stub.
                    bool fInILStub = false;

                    StackWalkFrames(&DetectHandleILStubsForDebugger_StackWalkCallback,
                                    &fInILStub,
                                    QUICKUNWIND,
                                    dac_cast<PTR_Frame>(pFrame));

                    if (fInILStub) return true;
                }
                else
                {
                    // The frame is fully initialized.
                    return false;
                }
            }
            pFrame = pFrame->Next();
        }
    }
    return false;
}

#ifndef _MSC_VER
__thread ThreadLocalInfo gCurrentThreadInfo;
#endif

#ifndef DACCESS_COMPILE

void SetThread(Thread* t)
{
    LIMITED_METHOD_CONTRACT

    gCurrentThreadInfo.m_pThread = t;
}

void SetAppDomain(AppDomain* ad)
{
    LIMITED_METHOD_CONTRACT

    gCurrentThreadInfo.m_pAppDomain = ad;
}

BOOL Thread::Alert ()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL fRetVal = FALSE;
    {
        HANDLE handle = GetThreadHandle();
        if (handle != INVALID_HANDLE_VALUE)
        {
            fRetVal = ::QueueUserAPC(UserInterruptAPC, handle, APC_Code);
        }
    }

    return fRetVal;
}


DWORD Thread::Join(DWORD timeout, BOOL alertable)
{
    WRAPPER_NO_CONTRACT;
    return JoinEx(timeout,alertable?WaitMode_Alertable:WaitMode_None);
}
DWORD Thread::JoinEx(DWORD timeout, WaitMode mode)
{
    CONTRACTL {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    BOOL alertable = (mode & WaitMode_Alertable)?TRUE:FALSE;

    Thread *pCurThread = GetThreadNULLOk();
    _ASSERTE(pCurThread || dbgOnly_IsSpecialEEThread());

    {
        // We're not hosted, so WaitMode_InDeadlock is irrelevant.  Clear it, so that this wait can be
        // forwarded to a SynchronizationContext if needed.
        mode = (WaitMode)(mode & ~WaitMode_InDeadlock);

        HANDLE handle = GetThreadHandle();
        if (handle == INVALID_HANDLE_VALUE) {
            return WAIT_FAILED;
        }
        if (pCurThread) {
            return pCurThread->DoAppropriateWait(1, &handle, FALSE, timeout, mode);
        }
        else {
            return WaitForSingleObjectEx(handle,timeout,alertable);
        }
    }
}

extern INT32 MapFromNTPriority(INT32 NTPriority);

BOOL Thread::SetThreadPriority(
    int nPriority   // thread priority level
)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL fRet;
    {
        if (GetThreadHandle() == INVALID_HANDLE_VALUE) {
            // When the thread starts running, we will set the thread priority.
            fRet =  TRUE;
        }
        else
            fRet = ::SetThreadPriority(GetThreadHandle(), nPriority);
    }

    if (fRet)
    {
        GCX_COOP();
        THREADBASEREF pObject = (THREADBASEREF)ObjectFromHandle(m_ExposedObject);
        if (pObject != NULL)
        {
            // TODO: managed ThreadPriority only supports up to 4.
            pObject->SetPriority (MapFromNTPriority(nPriority));
        }
    }
    return fRet;
}

int Thread::GetThreadPriority()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    int nRetVal = -1;
    if (GetThreadHandle() == INVALID_HANDLE_VALUE) {
        nRetVal = FALSE;
    }
    else
        nRetVal = ::GetThreadPriority(GetThreadHandle());

    return nRetVal;
}

void Thread::ChooseThreadCPUGroupAffinity()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    if (!CPUGroupInfo::CanEnableGCCPUGroups() ||
        !CPUGroupInfo::CanEnableThreadUseAllCpuGroups() ||
        !CPUGroupInfo::CanAssignCpuGroupsToThreads())
    {
        return;
    }

    //Borrow the ThreadStore Lock here: Lock ThreadStore before distributing threads
    ThreadStoreLockHolder TSLockHolder(TRUE);

    // this thread already has CPU group affinity set
    if (m_pAffinityMask != 0)
        return;

    if (GetThreadHandle() == INVALID_HANDLE_VALUE)
        return;

    GROUP_AFFINITY groupAffinity;
    CPUGroupInfo::ChooseCPUGroupAffinity(&groupAffinity);
    CPUGroupInfo::SetThreadGroupAffinity(GetThreadHandle(), &groupAffinity, NULL);
    m_wCPUGroup = groupAffinity.Group;
    m_pAffinityMask = groupAffinity.Mask;
#endif // !TARGET_UNIX
}

void Thread::ClearThreadCPUGroupAffinity()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    if (!CPUGroupInfo::CanEnableGCCPUGroups() ||
        !CPUGroupInfo::CanEnableThreadUseAllCpuGroups() ||
        !CPUGroupInfo::CanAssignCpuGroupsToThreads())
    {
        return;
    }

    ThreadStoreLockHolder TSLockHolder(TRUE);

    // this thread does not have CPU group affinity set
    if (m_pAffinityMask == 0)
        return;

    GROUP_AFFINITY groupAffinity;
    groupAffinity.Group = m_wCPUGroup;
    groupAffinity.Mask = m_pAffinityMask;
    CPUGroupInfo::ClearCPUGroupAffinity(&groupAffinity);

    m_wCPUGroup = 0;
    m_pAffinityMask = 0;
#endif // !TARGET_UNIX
}

DWORD Thread::StartThread()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    _ASSERTE (m_Creator.IsCurrentThread());
    m_Creator.Clear();
#endif

    _ASSERTE (GetThreadHandle() != INVALID_HANDLE_VALUE);
    DWORD dwRetVal = ::ResumeThread(GetThreadHandle());
    return dwRetVal;
}

// Class static data:
LONG    Thread::m_DebugWillSyncCount = -1;
LONG    Thread::m_DetachCount = 0;
LONG    Thread::m_ActiveDetachCount = 0;

static void DeleteThread(Thread* pThread)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    //_ASSERTE (pThread == GetThread());
    SetThread(NULL);
    SetAppDomain(NULL);

    if (pThread->HasThreadStateNC(Thread::TSNC_ExistInThreadStore))
    {
        pThread->DetachThread(FALSE);
    }
    else
    {
#ifdef FEATURE_COMINTEROP
        pThread->RevokeApartmentSpy();
#endif // FEATURE_COMINTEROP

        FastInterlockOr((ULONG *)&pThread->m_State, Thread::TS_Dead);

        // ~Thread() calls SafeSetThrowables which has a conditional contract
        // which says that if you call it with a NULL throwable then it is
        // MODE_ANY, otherwise MODE_COOPERATIVE. Scan doesn't understand that
        // and assumes that we're violating the MODE_COOPERATIVE.
        CONTRACT_VIOLATION(ModeViolation);

        delete pThread;
    }
}

static void EnsurePreemptive()
{
    WRAPPER_NO_CONTRACT;
    Thread *pThread = GetThreadNULLOk();
    if (pThread && pThread->PreemptiveGCDisabled())
    {
        pThread->EnablePreemptiveGC();
    }
}

typedef StateHolder<DoNothing, EnsurePreemptive> EnsurePreemptiveModeIfException;

Thread* SetupThread()
{
    CONTRACTL {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    Thread* pThread;
    if ((pThread = GetThreadNULLOk()) != NULL)
        return pThread;

    // For interop debugging, we must mark that we're in a can't-stop region
    // b.c we may take Crsts here that may block the helper thread.
    // We're especially fragile here b/c we don't have a Thread object yet
    CantStopHolder hCantStop;

    EnsurePreemptiveModeIfException ensurePreemptive;

#ifdef _DEBUG
    CHECK chk;
    if (g_pConfig->SuppressChecks())
    {
        // EnterAssert will suppress any checks
        chk.EnterAssert();
    }
#endif

    // Normally, HasStarted is called from the thread's entrypoint to introduce it to
    // the runtime.  But sometimes that thread is used for DLL_THREAD_ATTACH notifications
    // that call into managed code.  In that case, a call to SetupThread here must
    // find the correct Thread object and install it into TLS.

    if (ThreadStore::s_pThreadStore->GetPendingThreadCount() != 0)
    {
        DWORD  ourOSThreadId = ::GetCurrentThreadId();
        {
            ThreadStoreLockHolder TSLockHolder;
            _ASSERTE(pThread == NULL);
            while ((pThread = ThreadStore::s_pThreadStore->GetAllThreadList(pThread, Thread::TS_Unstarted | Thread::TS_FailStarted, Thread::TS_Unstarted)) != NULL)
            {
                if (pThread->GetOSThreadId() == ourOSThreadId)
                {
                    break;
                }
            }

            if (pThread != NULL)
            {
                STRESS_LOG2(LF_SYNC, LL_INFO1000, "T::ST - recycling thread 0x%p (state: 0x%x)\n", pThread, pThread->m_State.Load());
            }
        }

        // It's perfectly reasonable to not find the thread.  It's just an unrelated
        // thread spinning up.
        if (pThread)
        {
            if (IsThreadPoolWorkerSpecialThread())
            {
                FastInterlockOr((ULONG *) &pThread->m_State, Thread::TS_TPWorkerThread);
                pThread->SetBackground(TRUE);
            }
            else if (IsThreadPoolIOCompletionSpecialThread())
            {
                FastInterlockOr ((ULONG *) &pThread->m_State, Thread::TS_CompletionPortThread);
                pThread->SetBackground(TRUE);
            }
            else if (IsTimerSpecialThread() || IsWaitSpecialThread())
            {
                FastInterlockOr((ULONG *) &pThread->m_State, Thread::TS_TPWorkerThread);
                pThread->SetBackground(TRUE);
            }

            BOOL fStatus = pThread->HasStarted();
            ensurePreemptive.SuppressRelease();
            return fStatus ? pThread : NULL;
        }
    }

    // First time we've seen this thread in the runtime:
    pThread = new Thread();

// What state are we in here? COOP???

    Holder<Thread*,DoNothing<Thread*>,DeleteThread> threadHolder(pThread);

    SetupTLSForThread();
    pThread->InitThread();
    pThread->PrepareApartmentAndContext();

    // reset any unstarted bits on the thread object
    FastInterlockAnd((ULONG *) &pThread->m_State, ~Thread::TS_Unstarted);
    FastInterlockOr((ULONG *) &pThread->m_State, Thread::TS_LegalToJoin);

    ThreadStore::AddThread(pThread);

    SetThread(pThread);
    SetAppDomain(pThread->GetDomain());

#ifdef FEATURE_INTEROP_DEBUGGING
    // Ensure that debugger word slot is allocated
    TlsSetValue(g_debuggerWordTLSIndex, 0);
#endif

    // We now have a Thread object visable to the RS. unmark special status.
    hCantStop.Release();

    threadHolder.SuppressRelease();

    FastInterlockOr((ULONG *) &pThread->m_State, Thread::TS_FullyInitialized);

#ifdef DEBUGGING_SUPPORTED
    //
    // If we're debugging, let the debugger know that this
    // thread is up and running now.
    //
    if (CORDebuggerAttached())
    {
        g_pDebugInterface->ThreadCreated(pThread);
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "ThreadCreated() not called due to CORDebuggerAttached() being FALSE for thread 0x%x\n", pThread->GetThreadId()));
    }
#endif // DEBUGGING_SUPPORTED

#ifdef PROFILING_SUPPORTED
    // If a profiler is present, then notify the profiler that a
    // thread has been created.
    if (!IsGCSpecialThread())
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackThreads());
        {
            GCX_PREEMP();
            (&g_profControlBlock)->ThreadCreated(
                (ThreadID)pThread);
        }

        DWORD osThreadId = ::GetCurrentThreadId();
        (&g_profControlBlock)->ThreadAssignedToOSThread(
            (ThreadID)pThread, osThreadId);
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    _ASSERTE(!pThread->IsBackground()); // doesn't matter, but worth checking
    pThread->SetBackground(TRUE);

    ensurePreemptive.SuppressRelease();

    if (IsThreadPoolWorkerSpecialThread())
    {
        FastInterlockOr((ULONG *) &pThread->m_State, Thread::TS_TPWorkerThread);
    }
    else if (IsThreadPoolIOCompletionSpecialThread())
    {
        FastInterlockOr ((ULONG *) &pThread->m_State, Thread::TS_CompletionPortThread);
    }
    else if (IsTimerSpecialThread() || IsWaitSpecialThread())
    {
        FastInterlockOr((ULONG *) &pThread->m_State, Thread::TS_TPWorkerThread);
    }

#ifdef FEATURE_EVENT_TRACE
    ETW::ThreadLog::FireThreadCreated(pThread);
#endif // FEATURE_EVENT_TRACE

    return pThread;
}

//-------------------------------------------------------------------------
// Public function: SetupThreadNoThrow()
// Creates Thread for current thread if not previously created.
// Returns NULL for failure (usually due to out-of-memory.)
//-------------------------------------------------------------------------
Thread* SetupThreadNoThrow(HRESULT *pHR)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    Thread *pThread = GetThreadNULLOk();
    if (pThread != NULL)
    {
        return pThread;
    }

    EX_TRY
    {
        pThread = SetupThread();
    }
    EX_CATCH
    {
        // We failed SetupThread.  GET_EXCEPTION() may depend on Thread object.
        if (__pException == NULL)
        {
            hr = E_OUTOFMEMORY;
        }
        else
        {
        hr = GET_EXCEPTION()->GetHR();
    }
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (pHR)
    {
        *pHR = hr;
    }

    return pThread;
}

//-------------------------------------------------------------------------
// Public function: SetupUnstartedThread()
// This sets up a Thread object for an exposed System.Thread that
// has not been started yet.  This allows us to properly enumerate all threads
// in the ThreadStore, so we can report on even unstarted threads.  Clearly
// there is no physical thread to match, yet.
//
// When there is, complete the setup with code:Thread::HasStarted()
//-------------------------------------------------------------------------
Thread* SetupUnstartedThread(SetupUnstartedThreadFlags flags)
{
    CONTRACTL {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    Thread* pThread = new Thread();

    if (flags & SUTF_ThreadStoreLockAlreadyTaken)
    {
        _ASSERTE(ThreadStore::HoldingThreadStore());
        pThread->SetThreadStateNC(Thread::TSNC_TSLTakenForStartup);
    }

    FastInterlockOr((ULONG *) &pThread->m_State,
                    (Thread::TS_Unstarted | Thread::TS_WeOwn));

    ThreadStore::AddThread(pThread);

    return pThread;
}

//-------------------------------------------------------------------------
// Public function: DestroyThread()
// Destroys the specified Thread object, for a thread which is about to die.
//-------------------------------------------------------------------------
void DestroyThread(Thread *th)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE (th == GetThread());

    GCX_PREEMP_NO_DTOR();

    if (th->IsAbortRequested()) {
        // Reset trapping count.
        th->UnmarkThreadForAbort();
    }

    // Clear any outstanding stale EH state that maybe still active on the thread.
#ifdef FEATURE_EH_FUNCLETS
    ExceptionTracker::PopTrackers((void*)-1);
#else // !FEATURE_EH_FUNCLETS
#ifdef TARGET_X86
    PTR_ThreadExceptionState pExState = th->GetExceptionState();
    if (pExState->IsExceptionInProgress())
    {
        GCX_COOP();
        pExState->GetCurrentExceptionTracker()->UnwindExInfo((void *)-1);
    }
#else // !TARGET_X86
#error Unsupported platform
#endif // TARGET_X86
#endif // FEATURE_EH_FUNCLETS

    if (g_fEEShutDown == 0)
    {
        th->SetThreadState(Thread::TS_ReportDead);
        th->OnThreadTerminate(FALSE);
    }
}

//-------------------------------------------------------------------------
// Public function: DetachThread()
// Marks the thread as needing to be destroyed, but doesn't destroy it yet.
//-------------------------------------------------------------------------
HRESULT Thread::DetachThread(BOOL fDLLThreadDetach)
{
    // !!! Can not use contract here.
    // !!! Contract depends on Thread object for GC_TRIGGERS.
    // !!! At the end of this function, we call InternalSwitchOut,
    // !!! and then GetThread()=NULL, and dtor of contract does not work any more.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    // Clear any outstanding stale EH state that maybe still active on the thread.
#ifdef FEATURE_EH_FUNCLETS
    ExceptionTracker::PopTrackers((void*)-1);
#else // !FEATURE_EH_FUNCLETS
#ifdef TARGET_X86
    PTR_ThreadExceptionState pExState = GetExceptionState();
    if (pExState->IsExceptionInProgress())
    {
        GCX_COOP();
        pExState->GetCurrentExceptionTracker()->UnwindExInfo((void *)-1);
    }
#else // !TARGET_X86
#error Unsupported platform
#endif // TARGET_X86
#endif // FEATURE_EH_FUNCLETS

#ifdef FEATURE_COMINTEROP
    IErrorInfo *pErrorInfo;
    // Avoid calling GetErrorInfo() if ole32 has already executed the DLL_THREAD_DETACH,
    // otherwise we'll cause ole32 to re-allocate and leak its TLS data (SOleTlsData).
    if (ClrTeb::GetOleReservedPtr() != NULL && GetErrorInfo(0, &pErrorInfo) == S_OK)
    {
        // if this is our IErrorInfo, release it now - we don't want ole32 to do it later as
        // part of its DLL_THREAD_DETACH as we won't be able to handle the call at that point
        if (!ComInterfaceSlotIs(pErrorInfo, 2, Unknown_ReleaseSpecial_IErrorInfo))
        {
            // if it's not our IErrorInfo, put it back
            SetErrorInfo(0, pErrorInfo);
        }
        pErrorInfo->Release();
    }

    // Revoke our IInitializeSpy registration only if we are not in DLL_THREAD_DETACH
    // (COM will do it or may have already done it automatically in that case).
    if (!fDLLThreadDetach)
    {
        RevokeApartmentSpy();
    }
#endif // FEATURE_COMINTEROP

    _ASSERTE(!PreemptiveGCDisabled());

    _ASSERTE ((m_State & Thread::TS_Detached) == 0);

    _ASSERTE (this == GetThread());

    FastInterlockIncrement(&Thread::m_DetachCount);

    if (IsAbortRequested()) {
        // Reset trapping count.
        UnmarkThreadForAbort();
    }

    if (!IsBackground())
    {
        FastInterlockIncrement(&Thread::m_ActiveDetachCount);
        ThreadStore::CheckForEEShutdown();
    }

    HANDLE hThread = GetThreadHandle();
    SetThreadHandle (INVALID_HANDLE_VALUE);
    while (m_dwThreadHandleBeingUsed > 0)
    {
        // Another thread is using the handle now.
#undef Sleep
        // We can not call __SwitchToThread since we can not go back to host.
        ::Sleep(10);
#define Sleep(a) Dont_Use_Sleep(a)
    }
    if (m_WeOwnThreadHandle && m_ThreadHandleForClose == INVALID_HANDLE_VALUE)
    {
        m_ThreadHandleForClose = hThread;
    }

    // We need to make sure that TLS are touched last here.
    SetThread(NULL);
    SetAppDomain(NULL);

    FastInterlockOr((ULONG*)&m_State, (int) (Thread::TS_Detached | Thread::TS_ReportDead));
    // Do not touch Thread object any more.  It may be destroyed.

    // These detached threads will be cleaned up by finalizer thread.  But if the process uses
    // little managed heap, it will be a while before GC happens, and finalizer thread starts
    // working on detached thread.  So we wake up finalizer thread to clean up resources.
    //
    // (It's possible that this is the startup thread, and startup failed, and so the finalization
    //  machinery isn't fully initialized.  Hence this check.)
    if (g_fEEStarted)
        FinalizerThread::EnableFinalization();

    return S_OK;
}

DWORD GetRuntimeId()
{
    LIMITED_METHOD_CONTRACT;

#ifdef HOST_WINDOWS
    return _tls_index;
#else
    return 0;
#endif
}

//---------------------------------------------------------------------------
// Creates new Thread for reverse p-invoke calls.
//---------------------------------------------------------------------------
Thread* WINAPI CreateThreadBlockThrow()
{

    WRAPPER_NO_CONTRACT;

    // This is a workaround to disable our check for throwing exception in SetupThread.
    // We want to throw an exception for reverse p-invoke, and our assertion may fire if
    // a unmanaged caller does not setup an exception handler.
    CONTRACT_VIOLATION(ThrowsViolation); // WON'T FIX - This enables catastrophic failure exception in reverse P/Invoke - the only way we can communicate an error to legacy code.
    Thread* pThread = NULL;
    BEGIN_ENTRYPOINT_THROWS;

    HRESULT hr = S_OK;
    pThread = SetupThreadNoThrow(&hr);
    if (pThread == NULL)
    {
        // Creating Thread failed, and we need to throw an exception to report status.
        // It is misleading to use our COM+ exception code, since this is not a managed exception.
        ULONG_PTR arg = hr;
        RaiseException(EXCEPTION_EXX, 0, 1, &arg);
    }
    END_ENTRYPOINT_THROWS;

    return pThread;
}

#ifdef _DEBUG
DWORD_PTR Thread::OBJREF_HASH = OBJREF_TABSIZE;
#endif

extern "C" void STDCALL JIT_PatchedCodeStart();
extern "C" void STDCALL JIT_PatchedCodeLast();

static void* s_barrierCopy = NULL;

BYTE* GetWriteBarrierCodeLocation(VOID* barrier)
{
    if (IsWriteBarrierCopyEnabled())
    {
        return (BYTE*)PINSTRToPCODE((TADDR)s_barrierCopy + ((TADDR)barrier - (TADDR)JIT_PatchedCodeStart));
    }
    else
    {
        return (BYTE*)barrier;
    }
}

BOOL IsIPInWriteBarrierCodeCopy(PCODE controlPc)
{
    if (IsWriteBarrierCopyEnabled())
    {
        return (s_barrierCopy <= (void*)controlPc && (void*)controlPc < ((BYTE*)s_barrierCopy + ((BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart)));
    }
    else
    {
        return FALSE;
    }
}

PCODE AdjustWriteBarrierIP(PCODE controlPc)
{
    _ASSERTE(IsIPInWriteBarrierCodeCopy(controlPc));

    // Pretend we were executing the barrier function at its original location so that the unwinder can unwind the frame
    return (PCODE)JIT_PatchedCodeStart + (controlPc - (PCODE)s_barrierCopy);
}

#ifdef TARGET_X86
extern "C" void *JIT_WriteBarrierEAX_Loc;
#else
extern "C" void *JIT_WriteBarrier_Loc;
#endif

#ifdef TARGET_ARM64
extern "C" void (*JIT_WriteBarrier_Table)();
extern "C" void *JIT_WriteBarrier_Loc = 0;
extern "C" void *JIT_WriteBarrier_Table_Loc = 0;
#endif // TARGET_ARM64

#ifdef TARGET_ARM
extern "C" void *JIT_WriteBarrier_Loc = 0;
#endif // TARGET_ARM

#ifndef TARGET_UNIX
// g_TlsIndex is only used by the DAC. Disable optimizations around it to prevent it from getting optimized out.
#pragma optimize("", off)
static void SetIlsIndex(DWORD tlsIndex)
{
    g_TlsIndex = tlsIndex;
}
#pragma optimize("", on)
#endif

//---------------------------------------------------------------------------
// One-time initialization. Called during Dll initialization. So
// be careful what you do in here!
//---------------------------------------------------------------------------
void InitThreadManager()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // All patched helpers should fit into one page.
    // If you hit this assert on retail build, there is most likely problem with BBT script.
    _ASSERTE_ALL_BUILDS("clr/src/VM/threads.cpp", (BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart > (ptrdiff_t)0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/threads.cpp", (BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart < (ptrdiff_t)GetOsPageSize());

    if (IsWriteBarrierCopyEnabled())
    {
        s_barrierCopy = ExecutableAllocator::Instance()->Reserve(g_SystemInfo.dwAllocationGranularity);
        ExecutableAllocator::Instance()->Commit(s_barrierCopy, g_SystemInfo.dwAllocationGranularity, true);
        if (s_barrierCopy == NULL)
        {
            _ASSERTE(!"Allocation of GC barrier code page failed");
            COMPlusThrowWin32();
        }

        {
            size_t writeBarrierSize = (BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart;
            ExecutableWriterHolder<void> barrierWriterHolder(s_barrierCopy, writeBarrierSize);
            memcpy(barrierWriterHolder.GetRW(), (BYTE*)JIT_PatchedCodeStart, writeBarrierSize);
        }

        // Store the JIT_WriteBarrier copy location to a global variable so that helpers
        // can jump to it.
#ifdef TARGET_X86
        JIT_WriteBarrierEAX_Loc = GetWriteBarrierCodeLocation((void*)JIT_WriteBarrierEAX);

#define X86_WRITE_BARRIER_REGISTER(reg) \
    SetJitHelperFunction(CORINFO_HELP_ASSIGN_REF_##reg, GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier##reg)); \
    ETW::MethodLog::StubInitialized((ULONGLONG)GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier##reg), W("@WriteBarrier" #reg));

        ENUM_X86_WRITE_BARRIER_REGISTERS()

#undef X86_WRITE_BARRIER_REGISTER

#else // TARGET_X86
        JIT_WriteBarrier_Loc = GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier);
#endif // TARGET_X86
        SetJitHelperFunction(CORINFO_HELP_ASSIGN_REF, GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier));
        ETW::MethodLog::StubInitialized((ULONGLONG)GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier), W("@WriteBarrier"));

#ifdef TARGET_ARM64
        // Store the JIT_WriteBarrier_Table copy location to a global variable so that it can be updated.
        JIT_WriteBarrier_Table_Loc = GetWriteBarrierCodeLocation((void*)&JIT_WriteBarrier_Table);
#endif // TARGET_ARM64

#if defined(TARGET_ARM64) || defined(TARGET_ARM)
        SetJitHelperFunction(CORINFO_HELP_CHECKED_ASSIGN_REF, GetWriteBarrierCodeLocation((void*)JIT_CheckedWriteBarrier));
        ETW::MethodLog::StubInitialized((ULONGLONG)GetWriteBarrierCodeLocation((void*)JIT_CheckedWriteBarrier), W("@CheckedWriteBarrier"));
        SetJitHelperFunction(CORINFO_HELP_ASSIGN_BYREF, GetWriteBarrierCodeLocation((void*)JIT_ByRefWriteBarrier));
        ETW::MethodLog::StubInitialized((ULONGLONG)GetWriteBarrierCodeLocation((void*)JIT_ByRefWriteBarrier), W("@ByRefWriteBarrier"));
#endif // TARGET_ARM64 || TARGET_ARM

    }
    else
    {
        // I am using virtual protect to cover the entire range that this code falls in.
        //

        // We could reset it to non-writeable inbetween GCs and such, but then we'd have to keep on re-writing back and forth,
        // so instead we'll leave it writable from here forward.

        DWORD oldProt;
        if (!ClrVirtualProtect((void *)JIT_PatchedCodeStart, (BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart,
                            PAGE_EXECUTE_READWRITE, &oldProt))
        {
            _ASSERTE(!"ClrVirtualProtect of code page failed");
            COMPlusThrowWin32();
        }

#ifdef TARGET_X86
        JIT_WriteBarrierEAX_Loc = (void*)JIT_WriteBarrierEAX;
#else
        JIT_WriteBarrier_Loc = (void*)JIT_WriteBarrier;
#endif
#ifdef TARGET_ARM64
        // Store the JIT_WriteBarrier_Table copy location to a global variable so that it can be updated.
        JIT_WriteBarrier_Table_Loc = (void*)&JIT_WriteBarrier_Table;
#endif // TARGET_ARM64
    }

#ifndef TARGET_UNIX
    _ASSERTE(GetThreadNULLOk() == NULL);

    size_t offsetOfCurrentThreadInfo = Thread::GetOffsetOfThreadStatic(&gCurrentThreadInfo);

    _ASSERTE(offsetOfCurrentThreadInfo < 0x8000);
    _ASSERTE(_tls_index < 0x10000);

    // Save gCurrentThreadInfo location for debugger
    SetIlsIndex((DWORD)(_tls_index + (offsetOfCurrentThreadInfo << 16) + 0x80000000));

    _ASSERTE(g_TrapReturningThreads == 0);
#endif // !TARGET_UNIX

#ifdef FEATURE_INTEROP_DEBUGGING
    g_debuggerWordTLSIndex = TlsAlloc();
    if (g_debuggerWordTLSIndex == TLS_OUT_OF_INDEXES)
        COMPlusThrowWin32();
#endif

    IfFailThrow(Thread::CLRSetThreadStackGuarantee(Thread::STSGuarantee_Force));

    ThreadStore::InitThreadStore();

    // NOTE: CRST_UNSAFE_ANYMODE prevents a GC mode switch when entering this crst.
    // If you remove this flag, we will switch to preemptive mode when entering
    // g_DeadlockAwareCrst, which means all functions that enter it will become
    // GC_TRIGGERS.  (This includes all uses of CrstHolder.)  So be sure
    // to update the contracts if you remove this flag.
    g_DeadlockAwareCrst.Init(CrstDeadlockDetection, CRST_UNSAFE_ANYMODE);

#ifdef _DEBUG
    // Randomize OBJREF_HASH to handle hash collision.
    Thread::OBJREF_HASH = OBJREF_TABSIZE - (DbgGetEXETimeStamp()%10);
#endif // _DEBUG

    ThreadSuspend::Initialize();
}


//************************************************************************
// Thread members
//************************************************************************


#if defined(_DEBUG) && defined(TRACK_SYNC)

// One outstanding synchronization held by this thread:
struct Dbg_TrackSyncEntry
{
    UINT_PTR     m_caller;
    AwareLock   *m_pAwareLock;

    BOOL        Equiv      (UINT_PTR caller, void *pAwareLock)
    {
        LIMITED_METHOD_CONTRACT;

        return (m_caller == caller) && (m_pAwareLock == pAwareLock);
    }

    BOOL        Equiv      (void *pAwareLock)
    {
        LIMITED_METHOD_CONTRACT;

        return (m_pAwareLock == pAwareLock);
    }
};

// Each thread has a stack that tracks all enter and leave requests
struct Dbg_TrackSyncStack : public Dbg_TrackSync
{
    enum
    {
        MAX_TRACK_SYNC  = 20,       // adjust stack depth as necessary
    };

    void    EnterSync  (UINT_PTR caller, void *pAwareLock);
    void    LeaveSync  (UINT_PTR caller, void *pAwareLock);

    Dbg_TrackSyncEntry  m_Stack [MAX_TRACK_SYNC];
    UINT_PTR            m_StackPointer;
    BOOL                m_Active;

    Dbg_TrackSyncStack() : m_StackPointer(0),
                           m_Active(TRUE)
    {
        LIMITED_METHOD_CONTRACT;
    }
};

void Dbg_TrackSyncStack::EnterSync(UINT_PTR caller, void *pAwareLock)
{
    LIMITED_METHOD_CONTRACT;

    STRESS_LOG4(LF_SYNC, LL_INFO100, "Dbg_TrackSyncStack::EnterSync, IP=%p, Recursion=%u, LockState=%x, HoldingThread=%p.\n",
                    caller,
                    ((AwareLock*)pAwareLock)->GetRecursionLevel(),
                    ((AwareLock*)pAwareLock)->GetLockState(),
                    ((AwareLock*)pAwareLock)->GetHoldingThread());

    if (m_Active)
    {
        if (m_StackPointer >= MAX_TRACK_SYNC)
        {
            _ASSERTE(!"Overflowed synchronization stack checking.  Disabling");
            m_Active = FALSE;
            return;
        }
    }
    m_Stack[m_StackPointer].m_caller = caller;
    m_Stack[m_StackPointer].m_pAwareLock = (AwareLock *) pAwareLock;

    m_StackPointer++;

}

void Dbg_TrackSyncStack::LeaveSync(UINT_PTR caller, void *pAwareLock)
{
    WRAPPER_NO_CONTRACT;

    STRESS_LOG4(LF_SYNC, LL_INFO100, "Dbg_TrackSyncStack::LeaveSync, IP=%p, Recursion=%u, LockState=%x, HoldingThread=%p.\n",
                    caller,
                    ((AwareLock*)pAwareLock)->GetRecursionLevel(),
                    ((AwareLock*)pAwareLock)->GetLockState(),
                    ((AwareLock*)pAwareLock)->GetHoldingThread());

    if (m_Active)
    {
        if (m_StackPointer == 0)
            _ASSERTE(!"Underflow in leaving synchronization");
        else
        if (m_Stack[m_StackPointer - 1].Equiv(pAwareLock))
        {
            m_StackPointer--;
        }
        else
        {
            for (int i=m_StackPointer - 2; i>=0; i--)
            {
                if (m_Stack[i].Equiv(pAwareLock))
                {
                    _ASSERTE(!"Locks are released out of order.  This might be okay...");
                    memcpy(&m_Stack[i], &m_Stack[i+1],
                           sizeof(m_Stack[0]) * (m_StackPointer - i - 1));

                    return;
                }
            }
            _ASSERTE(!"Trying to release a synchronization lock which isn't held");
        }
    }
}

#endif  // TRACK_SYNC


static  DWORD dwHashCodeSeed = 123456789;

//--------------------------------------------------------------------
// Thread construction
//--------------------------------------------------------------------
Thread::Thread()
{
    CONTRACTL {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    m_pFrame                = FRAME_TOP;
    m_pGCFrame              = NULL;

    m_fPreemptiveGCDisabled = 0;

#ifdef _DEBUG
    m_ulForbidTypeLoad      = 0;
    m_GCOnTransitionsOK     = TRUE;
#endif

#ifdef ENABLE_CONTRACTS
    m_ulEnablePreemptiveGCCount  = 0;
#endif

#ifdef _DEBUG
    dbg_m_cSuspendedThreads = 0;
    dbg_m_cSuspendedThreadsWithoutOSLock = 0;
    m_Creator.Clear();
    m_dwUnbreakableLockCount = 0;
#endif

    m_dwForbidSuspendThread = 0;

    // Initialize lock state
    m_pHead = &m_embeddedEntry;
    m_embeddedEntry.pNext = m_pHead;
    m_embeddedEntry.pPrev = m_pHead;
    m_embeddedEntry.dwLLockID = 0;
    m_embeddedEntry.dwULockID = 0;
    m_embeddedEntry.wReaderLevel = 0;

    m_pBlockingLock = NULL;

    m_alloc_context.init();
    m_thAllocContextObj = 0;

    m_UserInterrupt = 0;
    m_WaitEventLink.m_Next = NULL;
    m_WaitEventLink.m_LinkSB.m_pNext = NULL;
    m_ThreadHandle = INVALID_HANDLE_VALUE;
    m_ThreadHandleForClose = INVALID_HANDLE_VALUE;
    m_ThreadHandleForResume = INVALID_HANDLE_VALUE;
    m_WeOwnThreadHandle = FALSE;

#ifdef _DEBUG
    m_ThreadId = UNINITIALIZED_THREADID;
#endif //_DEBUG

    // Initialize this variable to a very different start value for each thread
    // Using linear congruential generator from Knuth Vol. 2, p. 102, line 24
    dwHashCodeSeed = dwHashCodeSeed * 1566083941 + 1;
    m_dwHashCodeSeed = dwHashCodeSeed;

    m_hijackLock = FALSE;

    m_OSThreadId = 0;
    m_Priority = INVALID_THREAD_PRIORITY;
    m_ExternalRefCount = 1;
    m_State = TS_Unstarted;
    m_StateNC = TSNC_Unknown;

    // It can't be a LongWeakHandle because we zero stuff out of the exposed
    // object as it is finalized.  At that point, calls to GetCurrentThread()
    // had better get a new one,!
    m_ExposedObject = CreateGlobalShortWeakHandle(NULL);

    GlobalShortWeakHandleHolder exposedObjectHolder(m_ExposedObject);

    m_StrongHndToExposedObject = CreateGlobalStrongHandle(NULL);
    GlobalStrongHandleHolder strongHndToExposedObjectHolder(m_StrongHndToExposedObject);

    m_LastThrownObjectHandle = NULL;
    m_ltoIsUnhandled = FALSE;

    m_debuggerFilterContext = NULL;
    m_fInteropDebuggingHijacked = FALSE;
    m_profilerCallbackState = 0;

    m_pProfilerFilterContext = NULL;

    m_CacheStackBase = 0;
    m_CacheStackLimit = 0;
    m_CacheStackSufficientExecutionLimit = 0;
    m_CacheStackStackAllocNonRiskyExecutionLimit = 0;

#ifdef _DEBUG
    m_pCleanedStackBase = NULL;
#endif

#ifdef STACK_GUARDS_DEBUG
    m_pCurrentStackGuard = NULL;
#endif

#ifdef FEATURE_HIJACK
    m_ppvHJRetAddrPtr = (VOID**) 0xCCCCCCCCCCCCCCCC;
    m_pvHJRetAddr = (VOID*) 0xCCCCCCCCCCCCCCCC;

#ifndef TARGET_UNIX
    X86_ONLY(m_LastRedirectIP = 0);
    X86_ONLY(m_SpinCount = 0);
#endif // TARGET_UNIX
#endif // FEATURE_HIJACK

#if defined(_DEBUG) && defined(TRACK_SYNC)
    m_pTrackSync = new Dbg_TrackSyncStack;
    NewHolder<Dbg_TrackSyncStack> trackSyncHolder(static_cast<Dbg_TrackSyncStack*>(m_pTrackSync));
#endif  // TRACK_SYNC

    m_PreventAsync = 0;
#ifdef FEATURE_COMINTEROP
    m_fDisableComObjectEagerCleanup = false;
#endif //FEATURE_COMINTEROP
    m_fHasDeadThreadBeenConsideredForGCTrigger = false;
    m_TraceCallCount = 0;
    m_ThrewControlForThread = 0;
    m_ThreadTasks = (ThreadTasks)0;
    m_pLoadLimiter= NULL;

    // The state and the tasks must be 32-bit aligned for atomicity to be guaranteed.
    _ASSERTE((((size_t) &m_State) & 3) == 0);
    _ASSERTE((((size_t) &m_ThreadTasks) & 3) == 0);

    // On all callbacks, call the trap code, which we now have
    // wired to cause a GC.  Thus we will do a GC on all Transition Frame Transitions (and more).
   if (GCStress<cfg_transition>::IsEnabled())
   {
        m_State = (ThreadState) (m_State | TS_GCOnTransitions);
   }

    m_AbortType = EEPolicy::TA_None;
    m_AbortEndTime = MAXULONGLONG;
    m_RudeAbortEndTime = MAXULONGLONG;
    m_AbortController = 0;
    m_AbortRequestLock = 0;
    m_fRudeAbortInitiated = FALSE;

    m_pIOCompletionContext = NULL;

#ifdef _DEBUG
    m_fRudeAborted = FALSE;
    m_dwAbortPoint = 0;
#endif

    m_OSContext = new CONTEXT();
    NewHolder<CONTEXT> contextHolder(m_OSContext);

    m_pSavedRedirectContext = NULL;
    m_pOSContextBuffer = NULL;

#ifdef _DEBUG
    m_RedirectContextInUse = false;
#endif

#ifdef FEATURE_COMINTEROP
    m_pRCWStack = new RCWStackHeader();
#endif

#ifdef _DEBUG
    m_bGCStressing = FALSE;
    m_bUniqueStacking = FALSE;
#endif

    m_pPendingTypeLoad = NULL;

    m_pIBCInfo = NULL;

    m_dwAVInRuntimeImplOkayCount = 0;

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) && !defined(TARGET_UNIX) // GCCOVER
    m_fPreemptiveGCDisabledForGCStress = false;
#endif

#ifdef _DEBUG
    m_pHelperMethodFrameCallerList = (HelperMethodFrameCallerList*)-1;
#endif

    m_pExceptionDuringStartup = NULL;

#ifdef HAVE_GCCOVER
    m_pbDestCode = NULL;
    m_pbSrcCode = NULL;
#if defined(GCCOVER_TOLERATE_SPURIOUS_AV)
    m_pLastAVAddress = NULL;
#endif // defined(GCCOVER_TOLERATE_SPURIOUS_AV)
#endif // HAVE_GCCOVER

    m_debuggerActivePatchSkipper = NULL;
    m_dwThreadHandleBeingUsed = 0;
    SetProfilerCallbacksAllowed(TRUE);

    m_pCreatingThrowableForException = NULL;

#ifdef FEATURE_EH_FUNCLETS
    m_dwIndexClauseForCatch = 0;
    m_sfEstablisherOfActualHandlerFrame.Clear();
#endif // FEATURE_EH_FUNCLETS

    m_workerThreadPoolCompletionCount = 0;
    m_ioThreadPoolCompletionCount = 0;
    m_monitorLockContentionCount = 0;

    m_pDomain = SystemDomain::System()->DefaultDomain();

    // Do not expose thread until it is fully constructed
    g_pThinLockThreadIdDispenser->NewId(this, this->m_ThreadId);

    //
    // DO NOT ADD ADDITIONAL CONSTRUCTION AFTER THIS POINT.
    // NewId() allows this Thread instance to be accessed via a Thread Id.  Do not
    // add additional construction after this point to prevent the race condition
    // of accessing a partially constructed Thread via Thread Id lookup.
    //

    exposedObjectHolder.SuppressRelease();
    strongHndToExposedObjectHolder.SuppressRelease();
#if defined(_DEBUG) && defined(TRACK_SYNC)
    trackSyncHolder.SuppressRelease();
#endif
    contextHolder.SuppressRelease();

#ifdef FEATURE_COMINTEROP
    m_uliInitializeSpyCookie.QuadPart = 0ul;
    m_fInitializeSpyRegistered = false;
    m_pLastSTACtxCookie = NULL;
#endif // FEATURE_COMINTEROP

    m_fGCSpecial = FALSE;

#ifndef TARGET_UNIX
    m_wCPUGroup = 0;
    m_pAffinityMask = 0;
#endif // !TARGET_UNIX

    m_pAllLoggedTypes = NULL;

#ifdef FEATURE_PERFTRACING
    memset(&m_activityId, 0, sizeof(m_activityId));
#endif // FEATURE_PERFTRACING
    m_HijackReturnKind = RT_Illegal;

    m_currentPrepareCodeConfig = nullptr;
    m_isInForbidSuspendForDebuggerRegion = false;
    m_hasPendingActivation = false;

#ifdef _DEBUG
    memset(dangerousObjRefs, 0, sizeof(dangerousObjRefs));
#endif // _DEBUG
}

//--------------------------------------------------------------------
// Failable initialization occurs here.
//--------------------------------------------------------------------
void Thread::InitThread()
{
    CONTRACTL {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;


    HANDLE  hDup = INVALID_HANDLE_VALUE;
    BOOL    ret = TRUE;

        // This message actually serves a purpose (which is why it is always run)
        // The Stress log is run during hijacking, when other threads can be suspended
        // at arbitrary locations (including when holding a lock that NT uses to serialize
        // all memory allocations).  By sending a message now, we insure that the stress
        // log will not allocate memory at these critical times an avoid deadlock.
    STRESS_LOG2(LF_ALWAYS, LL_ALWAYS, "SetupThread  managed Thread %p Thread Id = %x\n", this, GetThreadId());

#ifndef TARGET_UNIX
    // workaround: Remove this when we flow impersonation token to host.
    BOOL    reverted = FALSE;
    HANDLE  threadToken = INVALID_HANDLE_VALUE;
#endif // !TARGET_UNIX

    if (m_ThreadHandle == INVALID_HANDLE_VALUE)
    {
        // For WinCE, all clients have the same handle for a thread.  Duplication is
        // not possible.  We make sure we never close this handle unless we created
        // the thread (TS_WeOwn).
        //
        // For Win32, each client has its own handle.  This is achieved by duplicating
        // the pseudo-handle from ::GetCurrentThread().  Unlike WinCE, this service
        // returns a pseudo-handle which is only useful for duplication.  In this case
        // each client is responsible for closing its own (duplicated) handle.
        //
        // We don't bother duplicating if WeOwn, because we created the handle in the
        // first place.
        // Thread is created when or after the physical thread started running
        HANDLE curProcess = ::GetCurrentProcess();

#ifndef TARGET_UNIX

        // If we're impersonating on NT, then DuplicateHandle(GetCurrentThread()) is going to give us a handle with only
        // THREAD_TERMINATE, THREAD_QUERY_INFORMATION, and THREAD_SET_INFORMATION. This doesn't include
        // THREAD_SUSPEND_RESUME nor THREAD_GET_CONTEXT. We need to be able to suspend the thread, and we need to be
        // able to get its context. Therefore, if we're impersonating, we revert to self, dup the handle, then
        // re-impersonate before we leave this routine.
        if (!RevertIfImpersonated(&reverted, &threadToken))
        {
            COMPlusThrowWin32();
        }

        class EnsureResetThreadToken
        {
        private:
            BOOL m_NeedReset;
            HANDLE m_threadToken;
        public:
            EnsureResetThreadToken(HANDLE threadToken, BOOL reverted)
            {
                m_threadToken = threadToken;
                m_NeedReset = reverted;
            }
            ~EnsureResetThreadToken()
            {
                UndoRevert(m_NeedReset, m_threadToken);
                if (m_threadToken != INVALID_HANDLE_VALUE)
                {
                    CloseHandle(m_threadToken);
                }
            }
        };

        EnsureResetThreadToken resetToken(threadToken, reverted);

#endif // !TARGET_UNIX

        if (::DuplicateHandle(curProcess, ::GetCurrentThread(), curProcess, &hDup,
                              0 /*ignored*/, FALSE /*inherit*/, DUPLICATE_SAME_ACCESS))
        {
            _ASSERTE(hDup != INVALID_HANDLE_VALUE);

            SetThreadHandle(hDup);
            m_WeOwnThreadHandle = TRUE;
        }
        else
        {
            COMPlusThrowWin32();
        }
    }

    if ((m_State & TS_WeOwn) == 0)
    {
        if (!AllocHandles())
        {
            ThrowOutOfMemory();
        }
    }

    _ASSERTE(HasValidThreadHandle());

    m_random.Init();

    // Set floating point mode to round to nearest
#ifndef TARGET_UNIX
    (void) _controlfp_s( NULL, _RC_NEAR, _RC_CHOP|_RC_UP|_RC_DOWN|_RC_NEAR );

    m_pTEB = (struct _NT_TIB*)NtCurrentTeb();

#endif // !TARGET_UNIX

    if (m_CacheStackBase == 0)
    {
        _ASSERTE(m_CacheStackLimit == 0);
        ret = SetStackLimits(fAll);
        if (ret == FALSE)
        {
            ThrowOutOfMemory();
        }
    }

    ret = Thread::AllocateIOCompletionContext();
    if (!ret)
    {
        ThrowOutOfMemory();
    }
}

// Allocate all the handles.  When we are kicking of a new thread, we can call
// here before the thread starts running.
BOOL Thread::AllocHandles()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(!m_DebugSuspendEvent.IsValid());
    _ASSERTE(!m_EventWait.IsValid());

    BOOL fOK = TRUE;
    EX_TRY {
        // create a manual reset event for getting the thread to a safe point
        m_DebugSuspendEvent.CreateManualEvent(FALSE);
        m_EventWait.CreateManualEvent(TRUE);
    }
    EX_CATCH {
        fOK = FALSE;

        if (!m_DebugSuspendEvent.IsValid()) {
            m_DebugSuspendEvent.CloseEvent();
        }

        if (!m_EventWait.IsValid()) {
            m_EventWait.CloseEvent();
        }
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    return fOK;
}

//--------------------------------------------------------------------
// This is the alternate path to SetupThread/InitThread.  If we created
// an unstarted thread, we have SetupUnstartedThread/HasStarted.
//--------------------------------------------------------------------
BOOL Thread::HasStarted()
{
    CONTRACTL {
        NOTHROW;
        DISABLED(GC_NOTRIGGER);
    }
    CONTRACTL_END;

    _ASSERTE(!m_fPreemptiveGCDisabled);     // can't use PreemptiveGCDisabled() here

    // This is cheating a little.  There is a pathway here from SetupThread, but only
    // via IJW SystemDomain::RunDllMain.  Normally SetupThread returns a thread in
    // preemptive mode, ready for a transition.  But in the IJW case, it can return a
    // cooperative mode thread.  RunDllMain handles this "surprise" correctly.
    m_fPreemptiveGCDisabled = TRUE;

    // Normally, HasStarted is called from the thread's entrypoint to introduce it to
    // the runtime.  But sometimes that thread is used for DLL_THREAD_ATTACH notifications
    // that call into managed code.  In that case, the second HasStarted call is
    // redundant and should be ignored.
    if (GetThreadNULLOk() == this)
        return TRUE;

    _ASSERTE(GetThreadNULLOk() == 0);
    _ASSERTE(HasValidThreadHandle());

    BOOL    fCanCleanupCOMState = FALSE;
    BOOL    res = TRUE;

    res = SetStackLimits(fAll);
    if (res == FALSE)
    {
        m_pExceptionDuringStartup = Exception::GetOOMException();
        goto FAILURE;
    }

    // If any exception happens during HasStarted, we will cache the exception in Thread::m_pExceptionDuringStartup
    // which will be thrown in Thread.Start as an internal exception
    EX_TRY
    {
        SetupTLSForThread();

        InitThread();

        fCanCleanupCOMState = TRUE;
        // Preparing the COM apartment and context may attempt
        // to transition to Preemptive mode. At this point in
        // the thread's lifetime this can be a bad thing if a GC
        // is triggered (e.g. GCStress). Do the preparation prior
        // to the thread being set so the Preemptive mode transition
        // is a no-op.
        PrepareApartmentAndContext();

        SetThread(this);
        SetAppDomain(m_pDomain);

        ThreadStore::TransferStartedThread(this);

#ifdef FEATURE_EVENT_TRACE
        ETW::ThreadLog::FireThreadCreated(this);
#endif // FEATURE_EVENT_TRACE
    }
    EX_CATCH
    {
        if (__pException != NULL)
        {
            __pException.SuppressRelease();
            m_pExceptionDuringStartup = __pException;
        }
        res = FALSE;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (res == FALSE)
        goto FAILURE;

    FastInterlockOr((ULONG *) &m_State, TS_FullyInitialized);

#ifdef DEBUGGING_SUPPORTED
    //
    // If we're debugging, let the debugger know that this
    // thread is up and running now.
    //
    if (CORDebuggerAttached())
    {
        g_pDebugInterface->ThreadCreated(this);
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "ThreadCreated() not called due to CORDebuggerAttached() being FALSE for thread 0x%x\n", GetThreadId()));
    }

#endif // DEBUGGING_SUPPORTED

#ifdef PROFILING_SUPPORTED
    // If a profiler is running, let them know about the new thread.
    //
    // The call to IsGCSpecial is crucial to avoid a deadlock.  See code:Thread::m_fGCSpecial for more
    // information
    if (!IsGCSpecial())
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackThreads());
        BOOL gcOnTransition = GC_ON_TRANSITIONS(FALSE);     // disable GCStress 2 to avoid the profiler receiving a RuntimeThreadSuspended notification even before the ThreadCreated notification
        
        {
            GCX_PREEMP();
            (&g_profControlBlock)->ThreadCreated((ThreadID) this);
        }

        GC_ON_TRANSITIONS(gcOnTransition);

        DWORD osThreadId = ::GetCurrentThreadId();
        (&g_profControlBlock)->ThreadAssignedToOSThread(
            (ThreadID) this, osThreadId);
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    // Reset the ThreadStoreLock state flag since the thread
    // has now been started.
    ResetThreadStateNC(Thread::TSNC_TSLTakenForStartup);
    return TRUE;

FAILURE:
    if (m_fPreemptiveGCDisabled)
    {
        m_fPreemptiveGCDisabled = FALSE;
    }
    _ASSERTE (HasThreadState(TS_Unstarted));

    SetThreadState(TS_FailStarted);

    if (GetThreadNULLOk() != NULL && IsAbortRequested())
        UnmarkThreadForAbort();

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    //
    // Undo the platform context initialization, so we don't leak a CoInitialize.
    //
    if (fCanCleanupCOMState)
    {
        // The thread pointer in TLS may not be set yet, if we had a failure before we set it.
        // So we'll set it up here (we'll unset it a few lines down).
        SetThread(this);
        CleanupCOMState();
    }
#endif
    FastInterlockDecrement(&ThreadStore::s_pThreadStore->m_PendingThreadCount);
    // One of the components of OtherThreadsComplete() has changed, so check whether
    // we should now exit the EE.
    ThreadStore::CheckForEEShutdown();
    DecExternalCount(/*holdingLock*/ HasThreadStateNC(Thread::TSNC_TSLTakenForStartup));
    SetThread(NULL);
    SetAppDomain(NULL);
    return FALSE;
}

BOOL Thread::AllocateIOCompletionContext()
{
    WRAPPER_NO_CONTRACT;
    PIOCompletionContext pIOC = new (nothrow) IOCompletionContext;

    if(pIOC != NULL)
    {
        pIOC->lpOverlapped = NULL;
        m_pIOCompletionContext = pIOC;
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

VOID Thread::FreeIOCompletionContext()
{
    WRAPPER_NO_CONTRACT;
    if (m_pIOCompletionContext != NULL)
    {
        PIOCompletionContext pIOC = (PIOCompletionContext) m_pIOCompletionContext;
        delete pIOC;
        m_pIOCompletionContext = NULL;
    }
}

void Thread::HandleThreadStartupFailure()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() != NULL);

    struct ProtectArgs
    {
        OBJECTREF pThrowable;
        OBJECTREF pReason;
    } args;
    memset(&args, 0, sizeof(ProtectArgs));

    GCPROTECT_BEGIN(args);

    MethodTable *pMT = CoreLibBinder::GetException(kThreadStartException);
    args.pThrowable = AllocateObject(pMT);

    MethodDescCallSite exceptionCtor(METHOD__THREAD_START_EXCEPTION__EX_CTOR);

    if (m_pExceptionDuringStartup)
    {
        args.pReason = CLRException::GetThrowableFromException(m_pExceptionDuringStartup);
        Exception::Delete(m_pExceptionDuringStartup);
        m_pExceptionDuringStartup = NULL;
    }

    ARG_SLOT args1[] = {
        ObjToArgSlot(args.pThrowable),
        ObjToArgSlot(args.pReason),
    };
    exceptionCtor.Call(args1);

    GCPROTECT_END(); //Prot

    RaiseTheExceptionInternalOnly(args.pThrowable, FALSE);
}

#ifndef TARGET_UNIX
BOOL RevertIfImpersonated(BOOL *bReverted, HANDLE *phToken)
{
    WRAPPER_NO_CONTRACT;

    BOOL bImpersonated = OpenThreadToken(GetCurrentThread(),    // we are assuming that if this call fails,
                                         TOKEN_IMPERSONATE,     // we are not impersonating. There is no win32
                                         TRUE,                  // api to figure this out. The only alternative
                                         phToken);              // is to use NtCurrentTeb->IsImpersonating().
    if (bImpersonated)
    {
        *bReverted = RevertToSelf();
        return *bReverted;

    }
    return TRUE;
}

void UndoRevert(BOOL bReverted, HANDLE hToken)
{
    if (bReverted)
    {
        if (!SetThreadToken(NULL, hToken))
        {
           _ASSERT("Undo Revert -> SetThreadToken failed");
           STRESS_LOG1(LF_EH, LL_INFO100, "UndoRevert/SetThreadToken failed for hToken = %d\n",hToken);
           EEPOLICY_HANDLE_FATAL_ERROR(COR_E_SECURITY);
        }
    }
    return;
}
#endif // !TARGET_UNIX


// We don't want ::CreateThread() calls scattered throughout the source.  So gather
// them all here.

BOOL Thread::CreateNewThread(SIZE_T stackSize, LPTHREAD_START_ROUTINE start, void *args, LPCWSTR pName)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    BOOL bRet;

    //This assert is here to prevent a bug in the future
    //  CreateTask currently takes a DWORD and we will downcast
    //  if that interface changes to take a SIZE_T this Assert needs to be removed.
    //
    _ASSERTE(stackSize <= 0xFFFFFFFF);

#ifndef TARGET_UNIX
    HandleHolder token;
    BOOL bReverted = FALSE;
    bRet = RevertIfImpersonated(&bReverted, &token);
    if (bRet != TRUE)
        return bRet;
#endif // !TARGET_UNIX

    m_StateNC = (ThreadStateNoConcurrency)((ULONG)m_StateNC | TSNC_CLRCreatedThread);
    bRet = CreateNewOSThread(stackSize, start, args);
#ifndef TARGET_UNIX
    UndoRevert(bReverted, token);
#endif // !TARGET_UNIX
    if (pName != NULL)
        SetThreadName(m_ThreadHandle, pName);

    return bRet;
}

void Thread::InitializationForManagedThreadInNative(_In_ Thread* pThread)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
        PRECONDITION(pThread != NULL);
    }
    CONTRACTL_END;

#ifdef FEATURE_OBJCMARSHAL
    {
        GCX_COOP_THREAD_EXISTS(pThread);
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__AUTORELEASEPOOL__CREATEAUTORELEASEPOOL);
        DECLARE_ARGHOLDER_ARRAY(args, 0);
        CALL_MANAGED_METHOD_NORET(args);
    }
#endif // FEATURE_OBJCMARSHAL
}

void Thread::CleanUpForManagedThreadInNative(_In_ Thread* pThread)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
        PRECONDITION(pThread != NULL);
    }
    CONTRACTL_END;

#ifdef FEATURE_OBJCMARSHAL
    {
        GCX_COOP_THREAD_EXISTS(pThread);
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__AUTORELEASEPOOL__DRAINAUTORELEASEPOOL);
        DECLARE_ARGHOLDER_ARRAY(args, 0);
        CALL_MANAGED_METHOD_NORET(args);
    }
#endif // FEATURE_OBJCMARSHAL
}

HANDLE Thread::CreateUtilityThread(Thread::StackSizeBucket stackSizeBucket, LPTHREAD_START_ROUTINE start, void *args, LPCWSTR pName, DWORD flags, DWORD* pThreadId)
{
    LIMITED_METHOD_CONTRACT;

    // TODO: we should always use small stacks for most of these threads.  For CLR 4, we're being conservative
    // here because this is a last-minute fix.

    SIZE_T stackSize;

    switch (stackSizeBucket)
    {
    case StackSize_Small:
        stackSize = 256 * 1024;
        break;

    case StackSize_Medium:
        stackSize = 512 * 1024;
        break;

    default:
        _ASSERTE(!"Bad stack size bucket");
        break;
    case StackSize_Large:
        stackSize = 1024 * 1024;
        break;
    }

    flags |= STACK_SIZE_PARAM_IS_A_RESERVATION;

    DWORD threadId;
    HANDLE hThread = CreateThread(NULL, stackSize, start, args, flags, &threadId);

    SetThreadName(hThread, pName);


    if (pThreadId)
        *pThreadId = threadId;

    return hThread;
}

// Represent the value of DEFAULT_STACK_SIZE as passed in the property bag to the host during construction
static unsigned long s_defaultStackSizeProperty = 0;

void ParseDefaultStackSize(LPCWSTR valueStr)
{
    if (valueStr)
    {
        LPWSTR end;
        errno = 0;
        unsigned long value = wcstoul(valueStr, &end, 16); // Base 16 without a prefix

        if ((errno == ERANGE)     // Parsed value doesn't fit in an unsigned long
            || (valueStr == end)  // No characters parsed
            || (end == nullptr)   // Unexpected condition (should never happen)
            || (end[0] != 0))     // Unprocessed terminal characters
        {
            ThrowHR(E_INVALIDARG);
        }
        else
        {
            s_defaultStackSizeProperty = value;
        }
    }
}

SIZE_T GetDefaultStackSizeSetting()
{
    static DWORD s_defaultStackSizeEnv = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DefaultStackSize);

    uint64_t value = s_defaultStackSizeEnv ? s_defaultStackSizeEnv : s_defaultStackSizeProperty;

    SIZE_T minStack = 0x10000;     // 64K - Somewhat arbitrary minimum thread stack size
    SIZE_T maxStack = 0x80000000;  //  2G - Somewhat arbitrary maximum thread stack size

    if ((value >= maxStack) || ((value != 0) && (value < minStack)))
    {
        ThrowHR(E_INVALIDARG);
    }

    return (SIZE_T) value;
}

BOOL Thread::GetProcessDefaultStackSize(SIZE_T* reserveSize, SIZE_T* commitSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //
    // Let's get the stack sizes from the PE file that started process.
    //
    static SIZE_T ExeSizeOfStackReserve = 0;
    static SIZE_T ExeSizeOfStackCommit = 0;

    static BOOL fSizesGot = FALSE;

    if (!fSizesGot)
    {
        SIZE_T defaultStackSizeSetting = GetDefaultStackSizeSetting();

        if (defaultStackSizeSetting != 0)
        {
            ExeSizeOfStackReserve = defaultStackSizeSetting;
            ExeSizeOfStackCommit = defaultStackSizeSetting;
            fSizesGot = TRUE;
        }
    }

#ifndef TARGET_UNIX
    if (!fSizesGot)
    {
        HINSTANCE hInst = WszGetModuleHandle(NULL);
        _ASSERTE(hInst);  // WszGetModuleHandle should never fail on the module that started the process.
        EX_TRY
        {
            PEDecoder pe(hInst);
            pe.GetEXEStackSizes(&ExeSizeOfStackReserve, &ExeSizeOfStackCommit);
            fSizesGot = TRUE;
        }
        EX_CATCH
        {
            fSizesGot = FALSE;
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
#endif // !TARGET_UNIX

    if (!fSizesGot) {
        //return some somewhat-reasonable numbers
        if (NULL != reserveSize) *reserveSize = 256*1024;
        if (NULL != commitSize) *commitSize = 256*1024;
        return FALSE;
    }

    if (NULL != reserveSize) *reserveSize = ExeSizeOfStackReserve;
    if (NULL != commitSize) *commitSize = ExeSizeOfStackCommit;
    return TRUE;
}

BOOL Thread::CreateNewOSThread(SIZE_T sizeToCommitOrReserve, LPTHREAD_START_ROUTINE start, void *args)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifdef TARGET_UNIX
    SIZE_T  ourId = 0;
#else
    DWORD   ourId = 0;
#endif
    HANDLE  h = NULL;
    DWORD dwCreationFlags = CREATE_SUSPENDED;

    dwCreationFlags |= STACK_SIZE_PARAM_IS_A_RESERVATION;

    if (sizeToCommitOrReserve == 0)
    {
        sizeToCommitOrReserve = GetDefaultStackSizeSetting();
    }

#ifndef TARGET_UNIX // the PAL does its own adjustments as necessary
    if (sizeToCommitOrReserve != 0 && sizeToCommitOrReserve <= GetOsPageSize())
    {
        // On Windows, passing a value that is <= one page size bizarrely causes the OS to use the default stack size instead of
        // a minimum, which is undesirable. This adjustment fixes that issue to use a minimum stack size (typically 64 KB).
        sizeToCommitOrReserve = GetOsPageSize() + 1;
    }
#endif // !TARGET_UNIX

    // Make sure we have all our handles, in case someone tries to suspend us
    // as we are starting up.
    if (!AllocHandles())
    {
        // OS is out of handles/memory?
        return FALSE;
    }

#ifdef TARGET_UNIX
    h = ::PAL_CreateThread64(NULL     /*=SECURITY_ATTRIBUTES*/,
#else
    h = ::CreateThread(      NULL     /*=SECURITY_ATTRIBUTES*/,
#endif
                             sizeToCommitOrReserve,
                             start,
                             args,
                             dwCreationFlags,
                             &ourId);

    if (h == NULL)
        return FALSE;

    _ASSERTE(!m_fPreemptiveGCDisabled);     // leave in preemptive until HasStarted.

    SetThreadHandle(h);
    m_WeOwnThreadHandle = TRUE;

    // Before we do the resume, we need to take note of the new ThreadId.  This
    // is necessary because -- before the thread starts executing at KickofThread --
    // it may perform some DllMain DLL_THREAD_ATTACH notifications.  These could
    // call into managed code.  During the consequent SetupThread, we need to
    // perform the Thread::HasStarted call instead of going through the normal
    // 'new thread' pathway.
    _ASSERTE(GetOSThreadId() == 0);
    _ASSERTE(ourId != 0);

    m_OSThreadId = ourId;

    FastInterlockIncrement(&ThreadStore::s_pThreadStore->m_PendingThreadCount);

#ifdef _DEBUG
    m_Creator.SetToCurrentThread();
#endif

    return TRUE;
}

//
// #threadDestruction
//
// General comments on thread destruction.
//
// The C++ Thread object can survive beyond the time when the Win32 thread has died.
// This is important if an exposed object has been created for this thread.  The
// exposed object will survive until it is GC'ed.
//
// A client like an exposed object can place an external reference count on that
// object.  We also place a reference count on it when we construct it, and we lose
// that count when the thread finishes doing useful work (OnThreadTerminate).
//
// One way OnThreadTerminate() is called is when the thread finishes doing useful
// work.  This case always happens on the correct thread.
//
// The other way OnThreadTerminate()  is called is during product shutdown.  We do
// a "best effort" to eliminate all threads except the Main thread before shutdown
// happens.  But there may be some background threads or external threads still
// running.
//
// When the final reference count disappears, we destruct.  Until then, the thread
// remains in the ThreadStore, but is marked as "Dead".
//<TODO>
// @TODO cwb: for a typical shutdown, only background threads are still around.
// Should we interrupt them?  What about the non-typical shutdown?</TODO>

int Thread::IncExternalCount()
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    Thread *pCurThread = GetThreadNULLOk();

    _ASSERTE(m_ExternalRefCount > 0);
    int retVal = FastInterlockIncrement((LONG*)&m_ExternalRefCount);
    // If we have an exposed object and the refcount is greater than one
    // we must make sure to keep a strong handle to the exposed object
    // so that we keep it alive even if nobody has a reference to it.
    if (pCurThread && ((*((void**)m_ExposedObject)) != NULL))
    {
        // The exposed object exists and needs a strong handle so check
        // to see if it has one.
        // Only a managed thread can setup StrongHnd.
        if ((*((void**)m_StrongHndToExposedObject)) == NULL)
        {
            GCX_COOP();
            // Store the object in the strong handle.
            StoreObjectInHandle(m_StrongHndToExposedObject, ObjectFromHandle(m_ExposedObject));
        }
    }

    return retVal;
}

int Thread::DecExternalCount(BOOL holdingLock)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    // Note that it's possible to get here with a NULL current thread (during
    // shutdown of the thread manager).
    Thread *pCurThread = GetThreadNULLOk();
    _ASSERTE (pCurThread == NULL || IsAtProcessExit()
              || (!holdingLock && !ThreadStore::HoldingThreadStore(pCurThread))
              || (holdingLock && ThreadStore::HoldingThreadStore(pCurThread)));

    BOOL ToggleGC = FALSE;
    BOOL SelfDelete = FALSE;

    int retVal;

    // Must synchronize count and exposed object handle manipulation. We use the
    // thread lock for this, which implies that we must be in pre-emptive mode
    // to begin with and avoid any activity that would invoke a GC (this
    // acquires the thread store lock).
    if (pCurThread)
    {
        // TODO: we would prefer to use a GC Holder here, however it is hard
        //       to get the case where we're deleting this thread correct given
        //       the current macros. We want to supress the release of the holder
        //       here which puts us in Preemptive mode, and also the switch to
        //       Cooperative mode below, but since both holders will be named
        //       the same thing (due to the generic nature of the macro) we can
        //       not use GCX_*_SUPRESS_RELEASE() for 2 holders in the same scope
        //       b/c they will both apply simply to the most narrowly scoped
        //       holder.

        ToggleGC = pCurThread->PreemptiveGCDisabled();
        if (ToggleGC)
            pCurThread->EnablePreemptiveGC();
    }

    GCX_ASSERT_PREEMP();

    ThreadStoreLockHolder tsLock(!holdingLock);

    _ASSERTE(m_ExternalRefCount >= 1);
    _ASSERTE(!holdingLock ||
             ThreadStore::s_pThreadStore->m_Crst.GetEnterCount() > 0 ||
             IsAtProcessExit());

    retVal = FastInterlockDecrement((LONG*)&m_ExternalRefCount);

    if (retVal == 0)
    {
        HANDLE h = GetThreadHandle();
        if (h == INVALID_HANDLE_VALUE)
        {
            h = m_ThreadHandleForClose;
            m_ThreadHandleForClose = INVALID_HANDLE_VALUE;
        }
        // Can not assert like this.  We have already removed the Unstarted bit.
        //_ASSERTE (IsUnstarted() || h != INVALID_HANDLE_VALUE);
        if (h != INVALID_HANDLE_VALUE && m_WeOwnThreadHandle)
        {
            ::CloseHandle(h);
            SetThreadHandle(INVALID_HANDLE_VALUE);
        }
        // Switch back to cooperative mode to manipulate the thread.
        if (pCurThread)
        {
            // TODO: we would prefer to use GCX_COOP here, see comment above.
            pCurThread->DisablePreemptiveGC();
        }

        GCX_ASSERT_COOP();

        // during process detach the thread might still be in the thread list
        // if it hasn't seen its DLL_THREAD_DETACH yet.  Use the following
        // tweak to decide if the thread has terminated yet.
        if (!HasValidThreadHandle())
        {
            SelfDelete = this == pCurThread;
            m_ExceptionState.FreeAllStackTraces();
            if (SelfDelete) {
                SetThread(NULL);
            }
            delete this;
        }

        tsLock.Release();

        // It only makes sense to restore the GC mode if we didn't just destroy
        // our own thread object.
        if (pCurThread && !SelfDelete && !ToggleGC)
        {
            pCurThread->EnablePreemptiveGC();
        }

        // Cannot use this here b/c it creates a holder named the same as GCX_ASSERT_COOP
        // in the same scope above...
        //
        // GCX_ASSERT_PREEMP()

        return retVal;
    }
    else if (pCurThread == NULL)
    {
        // We're in shutdown, too late to be worrying about having a strong
        // handle to the exposed thread object, we've already performed our
        // final GC.
        tsLock.Release();

        return retVal;
    }
    else
    {
        // Check to see if the external ref count reaches exactly one. If this
        // is the case and we have an exposed object then it is that exposed object
        // that is holding a reference to us. To make sure that we are not the
        // ones keeping the exposed object alive we need to remove the strong
        // reference we have to it.
        if ((retVal == 1) && ((*((void**)m_StrongHndToExposedObject)) != NULL))
        {
            // Switch back to cooperative mode to manipulate the object.

            // Don't want to switch back to COOP until we let go of the lock
            // however we are allowed to call StoreObjectInHandle here in preemptive
            // mode because we are setting the value to NULL.
            CONTRACT_VIOLATION(ModeViolation);

            // Clear the handle and leave the lock.
            // We do not have to to DisablePreemptiveGC here, because
            // we just want to put NULL into a handle.
            StoreObjectInHandle(m_StrongHndToExposedObject, NULL);

            tsLock.Release();

            // Switch back to the initial GC mode.
            if (ToggleGC)
            {
                pCurThread->DisablePreemptiveGC();
            }

            GCX_ASSERT_COOP();

            return retVal;
        }
    }

    tsLock.Release();

    // Switch back to the initial GC mode.
    if (ToggleGC)
    {
        pCurThread->DisablePreemptiveGC();
    }

    return retVal;
}



//--------------------------------------------------------------------
// Destruction. This occurs after the associated native thread
// has died.
//--------------------------------------------------------------------
Thread::~Thread()
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    // TODO: enable this
    //_ASSERTE(GetThread() != this);
    _ASSERTE(m_ThrewControlForThread == 0);

    // AbortRequest is coupled with TrapReturningThread.
    // We should have unmarked the thread for abort.
    // !!! Can not assert here.  If a thread has no managed code on stack
    // !!! we leave the g_TrapReturningThread set so that the thread will be
    // !!! aborted if it enters managed code.
    //_ASSERTE(!IsAbortRequested());

    // We should not have the Thread marked for abort.  But if we have
    // we need to unmark it so that g_TrapReturningThreads is decremented.
    if (IsAbortRequested())
    {
        UnmarkThreadForAbort();
    }

#if defined(_DEBUG) && defined(TRACK_SYNC)
    _ASSERTE(IsAtProcessExit() || ((Dbg_TrackSyncStack *) m_pTrackSync)->m_StackPointer == 0);
    delete m_pTrackSync;
#endif // TRACK_SYNC

    _ASSERTE(IsDead() || IsUnstarted() || IsAtProcessExit());

    if (m_WaitEventLink.m_Next != NULL && !IsAtProcessExit())
    {
        WaitEventLink *walk = &m_WaitEventLink;
        while (walk->m_Next) {
            ThreadQueue::RemoveThread(this, (SyncBlock*)((DWORD_PTR)walk->m_Next->m_WaitSB & ~1));
            StoreEventToEventStore (walk->m_Next->m_EventWait);
        }
        m_WaitEventLink.m_Next = NULL;
    }

    if (m_StateNC & TSNC_ExistInThreadStore) {
        BOOL ret;
        ret = ThreadStore::RemoveThread(this);
        _ASSERTE(ret);
    }

#ifdef _DEBUG
    m_pFrame = (Frame *)POISONC;
#endif

    // Normally we shouldn't get here with a valid thread handle; however if SetupThread
    // failed (due to an OOM for example) then we need to CloseHandle the thread
    // handle if we own it.
    if (m_WeOwnThreadHandle && (GetThreadHandle() != INVALID_HANDLE_VALUE))
    {
        CloseHandle(GetThreadHandle());
    }

    if (m_DebugSuspendEvent.IsValid())
    {
        m_DebugSuspendEvent.CloseEvent();
    }
    if (m_EventWait.IsValid())
    {
        m_EventWait.CloseEvent();
    }

    FreeIOCompletionContext();

    if (m_OSContext)
        delete m_OSContext;

    if (m_pOSContextBuffer)
    {
        delete[] m_pOSContextBuffer;
        m_pOSContextBuffer = NULL;
    }
    else if (m_pSavedRedirectContext)
    {
        delete m_pSavedRedirectContext;
    }

    MarkRedirectContextInUse(m_pSavedRedirectContext);
    m_pSavedRedirectContext = NULL;

#ifdef FEATURE_COMINTEROP
    if (m_pRCWStack)
        delete m_pRCWStack;
#endif

    if (m_pExceptionDuringStartup)
    {
        Exception::Delete (m_pExceptionDuringStartup);
    }

    ClearContext();

    if (!IsAtProcessExit())
    {
        // Destroy any handles that we're using to hold onto exception objects
        SafeSetThrowables(NULL);

        DestroyShortWeakHandle(m_ExposedObject);
        DestroyStrongHandle(m_StrongHndToExposedObject);
    }

    g_pThinLockThreadIdDispenser->DisposeId(GetThreadId());

    if (m_pIBCInfo) {
        delete m_pIBCInfo;
    }

    m_tailCallTls.FreeArgBuffer();

#ifdef FEATURE_EVENT_TRACE
    // Destruct the thread local type cache for allocation sampling
    if(m_pAllLoggedTypes) {
        ETW::TypeSystemLog::DeleteTypeHashNoLock(&m_pAllLoggedTypes);
    }
#endif // FEATURE_EVENT_TRACE

    // Wait for another thread to leave its loop in DeadlockAwareLock::TryBeginEnterLock
    CrstHolder lock(&g_DeadlockAwareCrst);
}

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT

void Thread::BaseCoUninitialize()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    _ASSERTE(GetThread() == this);

    ::CoUninitialize();
}// BaseCoUninitialize

#ifdef FEATURE_COMINTEROP
void Thread::BaseWinRTUninitialize()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    _ASSERTE(WinRTSupported());
    _ASSERTE(GetThread() == this);
    _ASSERTE(IsWinRTInitialized());

    RoUninitialize();
}
#endif // FEATURE_COMINTEROP

void Thread::CoUninitialize()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // Running threads might have performed a CoInitialize which must
    // now be balanced.
    BOOL needsUninitialize = IsCoInitialized()
#ifdef FEATURE_COMINTEROP
        || IsWinRTInitialized()
#endif // FEATURE_COMINTEROP
        ;

    if (!IsAtProcessExit() && needsUninitialize)
    {
        GCX_PREEMP();
        CONTRACT_VIOLATION(ThrowsViolation);

        if (IsCoInitialized())
        {
            BaseCoUninitialize();
            FastInterlockAnd((ULONG *)&m_State, ~TS_CoInitialized);
        }

#ifdef FEATURE_COMINTEROP
        if (IsWinRTInitialized())
        {
            _ASSERTE(WinRTSupported());
            BaseWinRTUninitialize();
            ResetWinRTInitialized();
        }
#endif // FEATURE_COMNITEROP
    }
}
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

void Thread::CleanupDetachedThreads()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadStore::HoldingThreadStore());

    ThreadStoreLockHolder threadStoreLockHolder;

    Thread *thread = ThreadStore::GetAllThreadList(NULL, 0, 0);

    STRESS_LOG0(LF_SYNC, LL_INFO1000, "T::CDT called\n");

    while (thread != NULL)
    {
        Thread *next = ThreadStore::GetAllThreadList(thread, 0, 0);

        if (thread->IsDetached())
        {
            STRESS_LOG1(LF_SYNC, LL_INFO1000, "T::CDT - detaching thread 0x%p\n", thread);

            // Unmark that the thread is detached while we have the
            // thread store lock. This will ensure that no other
            // thread will race in here and try to delete it, too.
            FastInterlockAnd((ULONG*)&(thread->m_State), ~TS_Detached);
            FastInterlockDecrement(&m_DetachCount);
            if (!thread->IsBackground())
                FastInterlockDecrement(&m_ActiveDetachCount);

            // If the debugger is attached, then we need to unlock the
            // thread store before calling OnThreadTerminate. That
            // way, we won't be holding the thread store lock if we
            // need to block sending a detach thread event.
            BOOL debuggerAttached =
#ifdef DEBUGGING_SUPPORTED
                CORDebuggerAttached();
#else // !DEBUGGING_SUPPORTED
                FALSE;
#endif // !DEBUGGING_SUPPORTED

            if (debuggerAttached)
                ThreadStore::UnlockThreadStore();

            thread->OnThreadTerminate(debuggerAttached ? FALSE : TRUE);

#ifdef DEBUGGING_SUPPORTED
            if (debuggerAttached)
            {
                ThreadSuspend::LockThreadStore(ThreadSuspend::SUSPEND_OTHER);

                // We remember the next Thread in the thread store
                // list before deleting the current one. But we can't
                // use that Thread pointer now that we release the
                // thread store lock in the middle of the loop. We
                // have to start from the beginning of the list every
                // time. If two threads T1 and T2 race into
                // CleanupDetachedThreads, then T1 will grab the first
                // Thread on the list marked for deletion and release
                // the lock. T2 will grab the second one on the
                // list. T2 may complete destruction of its Thread,
                // then T1 might re-acquire the thread store lock and
                // try to use the next Thread in the thread store. But
                // T2 just deleted that next Thread.
                thread = ThreadStore::GetAllThreadList(NULL, 0, 0);
            }
            else
#endif // DEBUGGING_SUPPORTED
            {
                thread = next;
            }
        }
        else if (thread->HasThreadState(TS_Finalized))
        {
            STRESS_LOG1(LF_SYNC, LL_INFO1000, "T::CDT - finalized thread 0x%p\n", thread);

            thread->ResetThreadState(TS_Finalized);
            // We have finalized the managed Thread object.  Now it is time to clean up the unmanaged part
            thread->DecExternalCount(TRUE);
            thread = next;
        }
        else
        {
            thread = next;
        }
    }

    s_fCleanFinalizedThread = FALSE;
}

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT

void Thread::CleanupCOMState()
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    if (GetFinalApartment() == Thread::AS_InSTA)
        ReleaseRCWsInCachesNoThrow(GetCurrentCtxCookie());
#endif // FEATURE_COMINTEROP

    // Running threads might have performed a CoInitialize which must
    // now be balanced. However only the thread that called COInitialize can
    // call CoUninitialize.

    BOOL needsUninitialize = IsCoInitialized()
#ifdef FEATURE_COMINTEROP
        || IsWinRTInitialized()
#endif // FEATURE_COMINTEROP
        ;

    if (needsUninitialize)
    {
        GCX_PREEMP();
        CONTRACT_VIOLATION(ThrowsViolation);

        if (IsCoInitialized())
        {
            BaseCoUninitialize();
            ResetCoInitialized();
        }

#ifdef FEATURE_COMINTEROP
        if (IsWinRTInitialized())
        {
            _ASSERTE(WinRTSupported());
            BaseWinRTUninitialize();
            ResetWinRTInitialized();
        }
#endif // FEATURE_COMINTEROP
    }
}
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

// See general comments on thread destruction (code:#threadDestruction) above.
void Thread::OnThreadTerminate(BOOL holdingLock)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    // #ReportDeadOnThreadTerminate
    // Caller should have put the TS_ReportDead bit on by now.
    // We don't want any windows after the exit event but before the thread is marked dead.
    // If a debugger attached during such a window (or even took a dump at the exit event),
    // then it may not realize the thread is dead.
    // So ensure we mark the thread as dead before we send the tool notifications.
    // The TS_ReportDead bit will cause the debugger to view this as TS_Dead.
    _ASSERTE(HasThreadState(TS_ReportDead));

    // Should not use OSThreadId:
    // OSThreadId may change for the current thread is the thread is blocked and rescheduled
    // by host.
    Thread *pCurrentThread = GetThreadNULLOk();
    DWORD CurrentThreadID = pCurrentThread?pCurrentThread->GetThreadId():0;
    DWORD ThisThreadID = GetThreadId();

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // If the currently running thread is the thread that died and it is an STA thread, then we
    // need to release all the RCW's in the current context. However, we cannot do this if we
    // are in the middle of process detach.
    if (!IsAtProcessExit() && this == GetThreadNULLOk())
    {
        CleanupCOMState();
    }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    if (g_fEEShutDown != 0)
    {
        // We have started shutdown.  Not safe to touch CLR state.
        return;
    }

    // We took a count during construction, and we rely on the count being
    // non-zero as we terminate the thread here.
    _ASSERTE(m_ExternalRefCount > 0);

    // The thread is no longer running.  It's important that we zero any general OBJECTHANDLE's
    // on this Thread object.  That's because we need the managed Thread object to be subject to
    // GC and yet any HANDLE is opaque to the GC when it comes to collecting cycles. When the
    // thread is executing, nothing can be collected anyway.  But now that we stop running the
    // cycle concerns us.
    //
    // It's important that we only use OBJECTHANDLE's that are retrievable while the thread is
    // still running.  That's what allows us to zero them here with impunity:
    {
        // No handles to clean up in the m_ExceptionState
        _ASSERTE(!m_ExceptionState.IsExceptionInProgress());

        GCX_COOP();

        // Destroy the LastThrown handle (and anything that violates the above assert).
        SafeSetThrowables(NULL);

        // Free all structures related to thread statics for this thread
        DeleteThreadStaticData();

    }

    if  (GCHeapUtilities::IsGCHeapInitialized())
    {
        // Guaranteed to NOT be a shutdown case, because we tear down the heap before
        // we tear down any threads during shutdown.
        if (ThisThreadID == CurrentThreadID)
        {
            GCX_COOP();
            // GetTotalAllocatedBytes reads dead_threads_non_alloc_bytes, but will suspend EE, being in COOP mode we cannot race with that
            // however, there could be other threads terminating and doing the same Add.
            FastInterlockExchangeAddLong((LONG64*)&dead_threads_non_alloc_bytes, m_alloc_context.alloc_limit - m_alloc_context.alloc_ptr);
            GCHeapUtilities::GetGCHeap()->FixAllocContext(&m_alloc_context, NULL, NULL);
            m_alloc_context.init();
        }
    }

    // We switch a thread to dead when it has finished doing useful work.  But it
    // remains in the thread store so long as someone keeps it alive.  An exposed
    // object will do this (it releases the refcount in its finalizer).  If the
    // thread is never released, we have another look during product shutdown and
    // account for the unreleased refcount of the uncollected exposed object:
    if (IsDead())
    {
        GCX_COOP();

        _ASSERTE(IsAtProcessExit());
        ClearContext();
        if (m_ExposedObject != NULL)
            DecExternalCount(holdingLock);             // may destruct now
    }
    else
    {
#ifdef DEBUGGING_SUPPORTED
        //
        // If we're debugging, let the debugger know that this thread is
        // gone.
        //
        // There is a race here where the debugger could have attached after
        // we checked (and thus didn't release the lock).  In this case,
        // we can't call out to the debugger or we risk a deadlock.
        //
        if (!holdingLock && CORDebuggerAttached())
        {
            g_pDebugInterface->DetachThread(this);
        }
#endif // DEBUGGING_SUPPORTED

#ifdef PROFILING_SUPPORTED
        // If a profiler is present, then notify the profiler of thread destroy
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackThreads());
            GCX_PREEMP();
            (&g_profControlBlock)->ThreadDestroyed((ThreadID) this);
            END_PROFILER_CALLBACK();
        }
#endif // PROFILING_SUPPORTED

        if (!holdingLock)
        {
            LOG((LF_SYNC, INFO3, "OnThreadTerminate obtain lock\n"));
            ThreadSuspend::LockThreadStore(ThreadSuspend::SUSPEND_OTHER);

        }

        if  (GCHeapUtilities::IsGCHeapInitialized() && ThisThreadID != CurrentThreadID)
        {
            // We must be holding the ThreadStore lock in order to clean up alloc context.
            // We should never call FixAllocContext during GC.
            dead_threads_non_alloc_bytes += m_alloc_context.alloc_limit - m_alloc_context.alloc_ptr;
            GCHeapUtilities::GetGCHeap()->FixAllocContext(&m_alloc_context, NULL, NULL);
            m_alloc_context.init();
        }

        FastInterlockOr((ULONG *) &m_State, TS_Dead);
        ThreadStore::s_pThreadStore->m_DeadThreadCount++;
        ThreadStore::s_pThreadStore->IncrementDeadThreadCountForGCTrigger();

        if (IsUnstarted())
            ThreadStore::s_pThreadStore->m_UnstartedThreadCount--;
        else
        {
            if (IsBackground())
                ThreadStore::s_pThreadStore->m_BackgroundThreadCount--;
        }

        FastInterlockAnd((ULONG *) &m_State, ~(TS_Unstarted | TS_Background));

        //
        // If this thread was told to trip for debugging between the
        // sending of the detach event above and the locking of the
        // thread store lock, then remove the flag and decrement the
        // global trap returning threads count.
        //
        if (!IsAtProcessExit())
        {
            // A thread can't die during a GCPending, because the thread store's
            // lock is held by the GC thread.
            if (m_State & TS_DebugSuspendPending)
                UnmarkForSuspension(~TS_DebugSuspendPending);

            if (CurrentThreadID == ThisThreadID && IsAbortRequested())
            {
                UnmarkThreadForAbort();
            }
        }

        if (GetThreadHandle() != INVALID_HANDLE_VALUE)
        {
            if (m_ThreadHandleForClose == INVALID_HANDLE_VALUE)
            {
                m_ThreadHandleForClose = GetThreadHandle();
            }
            SetThreadHandle (INVALID_HANDLE_VALUE);
        }

        m_OSThreadId = 0;

        // If nobody else is holding onto the thread, we may destruct it here:
        ULONG   oldCount = DecExternalCount(TRUE);
        // If we are shutting down the process, we only have one thread active in the
        // system.  So we can disregard all the reasons that hold this thread alive --
        // TLS is about to be reclaimed anyway.
        if (IsAtProcessExit())
            while (oldCount > 0)
            {
                oldCount = DecExternalCount(TRUE);
            }

        // ASSUME THAT THE THREAD IS DELETED, FROM HERE ON

        _ASSERTE(ThreadStore::s_pThreadStore->m_ThreadCount >= 0);
        _ASSERTE(ThreadStore::s_pThreadStore->m_BackgroundThreadCount >= 0);
        _ASSERTE(ThreadStore::s_pThreadStore->m_ThreadCount >=
                 ThreadStore::s_pThreadStore->m_BackgroundThreadCount);
        _ASSERTE(ThreadStore::s_pThreadStore->m_ThreadCount >=
                 ThreadStore::s_pThreadStore->m_UnstartedThreadCount);
        _ASSERTE(ThreadStore::s_pThreadStore->m_ThreadCount >=
                 ThreadStore::s_pThreadStore->m_DeadThreadCount);

        // One of the components of OtherThreadsComplete() has changed, so check whether
        // we should now exit the EE.
        ThreadStore::CheckForEEShutdown();

        if (ThisThreadID == CurrentThreadID)
        {
            // NULL out the thread block  in the tls.  We can't do this if we aren't on the
            // right thread.  But this will only happen during a shutdown.  And we've made
            // a "best effort" to reduce to a single thread before we begin the shutdown.
            SetThread(NULL);
            SetAppDomain(NULL);
        }

        if (!holdingLock)
        {
            LOG((LF_SYNC, INFO3, "OnThreadTerminate releasing lock\n"));
            ThreadSuspend::UnlockThreadStore(ThisThreadID == CurrentThreadID);
        }
    }
}

// Helper functions to check for duplicate handles. we only do this check if
// a waitfor multiple fails.
int __cdecl compareHandles( const void *arg1, const void *arg2 )
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HANDLE h1 = *(HANDLE*)arg1;
    HANDLE h2 = *(HANDLE*)arg2;
    return  (h1 == h2) ? 0 : ((h1 < h2) ? -1 : 1);
}

BOOL CheckForDuplicateHandles(int countHandles, HANDLE *handles)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    qsort(handles,countHandles,sizeof(HANDLE),compareHandles);
    for (int i=1; i < countHandles; i++)
    {
        if (handles[i-1] == handles[i])
            return TRUE;
    }
    return FALSE;
}
//--------------------------------------------------------------------
// Based on whether this thread has a message pump, do the appropriate
// style of Wait.
//--------------------------------------------------------------------
DWORD Thread::DoAppropriateWait(int countHandles, HANDLE *handles, BOOL waitAll,
                                DWORD millis, WaitMode mode, PendingSync *syncState)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    INDEBUG(BOOL alertable = (mode & WaitMode_Alertable) != 0;);
    _ASSERTE(alertable || syncState == 0);

    struct Param
    {
        Thread *pThis;
        int countHandles;
        HANDLE *handles;
        BOOL waitAll;
        DWORD millis;
        WaitMode mode;
        DWORD dwRet;
    } param;
    param.pThis = this;
    param.countHandles = countHandles;
    param.handles = handles;
    param.waitAll = waitAll;
    param.millis = millis;
    param.mode = mode;
    param.dwRet = (DWORD) -1;

    EE_TRY_FOR_FINALLY(Param *, pParam, &param) {
        pParam->dwRet = pParam->pThis->DoAppropriateWaitWorker(pParam->countHandles, pParam->handles, pParam->waitAll, pParam->millis, pParam->mode);
    }
    EE_FINALLY {
        if (syncState) {
            if (!GOT_EXCEPTION() &&
                param.dwRet >= WAIT_OBJECT_0 && param.dwRet < (DWORD)(WAIT_OBJECT_0 + countHandles)) {
                // This thread has been removed from syncblk waiting list by the signalling thread
                syncState->Restore(FALSE);
            }
            else
                syncState->Restore(TRUE);
        }

        _ASSERTE (param.dwRet != WAIT_IO_COMPLETION);
    }
    EE_END_FINALLY;

    return(param.dwRet);
}

DWORD Thread::DoAppropriateWait(AppropriateWaitFunc func, void *args,
                                DWORD millis, WaitMode mode,
                                PendingSync *syncState)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    INDEBUG(BOOL alertable = (mode & WaitMode_Alertable) != 0;);
    _ASSERTE(alertable || syncState == 0);

    struct Param
    {
        Thread *pThis;
        AppropriateWaitFunc func;
        void *args;
        DWORD millis;
        WaitMode mode;
        DWORD dwRet;
    } param;
    param.pThis = this;
    param.func = func;
    param.args = args;
    param.millis = millis;
    param.mode = mode;
    param.dwRet = (DWORD) -1;

    EE_TRY_FOR_FINALLY(Param *, pParam, &param) {
        pParam->dwRet = pParam->pThis->DoAppropriateWaitWorker(pParam->func, pParam->args, pParam->millis, pParam->mode);
    }
    EE_FINALLY {
        if (syncState) {
            if (!GOT_EXCEPTION() && WAIT_OBJECT_0 == param.dwRet) {
                // This thread has been removed from syncblk waiting list by the signalling thread
                syncState->Restore(FALSE);
            }
            else
                syncState->Restore(TRUE);
        }

        _ASSERTE (WAIT_IO_COMPLETION != param.dwRet);
    }
    EE_END_FINALLY;

    return(param.dwRet);
}

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT

//--------------------------------------------------------------------
// helper to do message wait
//--------------------------------------------------------------------
DWORD MsgWaitHelper(int numWaiters, HANDLE* phEvent, BOOL bWaitAll, DWORD millis, BOOL bAlertable)
{
    STANDARD_VM_CONTRACT;

    DWORD flags = 0;
    DWORD dwReturn=WAIT_ABANDONED;

    // If we're going to pump, we cannot use WAIT_ALL.  That's because the wait would
    // only be satisfied if a message arrives while the handles are signalled.  If we
    // want true WAIT_ALL, we need to fire up a different thread in the MTA and wait
    // on its result.  This isn't implemented yet.
    //
    // A change was added to WaitHandleNative::CorWaitMultipleNative to disable WaitAll
    // in an STA with more than one handle.
    if (bWaitAll)
    {
        if (numWaiters == 1)
            bWaitAll = FALSE;

        // The check that's supposed to prevent this condition from occuring, in WaitHandleNative::CorWaitMultipleNative,
        // is unfortunately behind FEATURE_COMINTEROP instead of FEATURE_COMINTEROP_APARTMENT_SUPPORT.
        // So on CoreCLR (where FEATURE_COMINTEROP is not currently defined) we can actually reach this point.
        // We can't fix this, because it's a breaking change, so we just won't assert here.
        // The result is that WaitAll on an STA thread in CoreCLR will behave stragely, as described above.
    }

    if (bWaitAll)
        flags |= COWAIT_WAITALL;

    if (bAlertable)
        flags |= COWAIT_ALERTABLE;

    // CoWaitForMultipleHandles does not support more than 63 handles. It returns RPC_S_CALLPENDING for more than 63 handles
    // that is impossible to differentiate from timeout.
    if (numWaiters > 63)
        COMPlusThrow(kNotSupportedException, W("NotSupported_MaxWaitHandles_STA"));

    HRESULT hr = CoWaitForMultipleHandles(flags, millis, numWaiters, phEvent, &dwReturn);

    if (hr == RPC_S_CALLPENDING)
    {
        dwReturn = WAIT_TIMEOUT;
    }
    else if (FAILED(hr))
    {
        // The service behaves differently on an STA vs. MTA in how much
        // error information it propagates back, and in which form.  We currently
        // only get here in the STA case, so bias this logic that way.
        dwReturn = WAIT_FAILED;
    }
    else
    {
        dwReturn += WAIT_OBJECT_0;  // success -- bias back
    }

    return dwReturn;
}

#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

//--------------------------------------------------------------------
// Do appropriate wait based on apartment state (STA or MTA)
DWORD Thread::DoAppropriateAptStateWait(int numWaiters, HANDLE* pHandles, BOOL bWaitAll,
                                         DWORD timeout, WaitMode mode)
{
    STANDARD_VM_CONTRACT;

    BOOL alertable = (mode & WaitMode_Alertable) != 0;

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    if (alertable && !GetDomain()->MustForceTrivialWaitOperations())
    {
        ApartmentState as = GetFinalApartment();
        if (AS_InMTA != as)
        {
            return MsgWaitHelper(numWaiters, pHandles, bWaitAll, timeout, alertable);
        }
    }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    return WaitForMultipleObjectsEx(numWaiters, pHandles, bWaitAll, timeout, alertable);
}

// A helper called by our two flavors of DoAppropriateWaitWorker
void Thread::DoAppropriateWaitWorkerAlertableHelper(WaitMode mode)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // A word about ordering for Interrupt.  If someone tries to interrupt a thread
    // that's in the interruptible state, we queue an APC.  But if they try to interrupt
    // a thread that's not in the interruptible state, we just record that fact.  So
    // we have to set TS_Interruptible before we test to see whether someone wants to
    // interrupt us or else we have a race condition that causes us to skip the APC.
    FastInterlockOr((ULONG *) &m_State, TS_Interruptible);

    if (HasThreadStateNC(TSNC_InRestoringSyncBlock))
    {
        // The thread is restoring SyncBlock for Object.Wait.
        ResetThreadStateNC(TSNC_InRestoringSyncBlock);
    }
    else
    {
        HandleThreadInterrupt();

        // Safe to clear the interrupted state, no APC could have fired since we
        // reset m_UserInterrupt (which inhibits our APC callback from doing
        // anything).
        FastInterlockAnd((ULONG *) &m_State, ~TS_Interrupted);
    }
}

void MarkOSAlertableWait()
{
    LIMITED_METHOD_CONTRACT;
    GetThread()->SetThreadStateNC (Thread::TSNC_OSAlertableWait);
}

void UnMarkOSAlertableWait()
{
    LIMITED_METHOD_CONTRACT;
    GetThread()->ResetThreadStateNC (Thread::TSNC_OSAlertableWait);
}

//--------------------------------------------------------------------
// Based on whether this thread has a message pump, do the appropriate
// style of Wait.
//--------------------------------------------------------------------
DWORD Thread::DoAppropriateWaitWorker(int countHandles, HANDLE *handles, BOOL waitAll,
                                      DWORD millis, WaitMode mode)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    DWORD ret = 0;

    BOOL alertable = (mode & WaitMode_Alertable) != 0;
    // Waits from SynchronizationContext.WaitHelper are always just WaitMode_IgnoreSyncCtx.
    // So if we defer to a sync ctx, we will lose any extra bits.  We must therefore not
    // defer to a sync ctx if doing any non-default wait.
    // If you're doing a default wait, but want to ignore sync ctx, specify WaitMode_IgnoreSyncCtx
    // which will make mode != WaitMode_Alertable.
    BOOL ignoreSyncCtx = (mode != WaitMode_Alertable);

    if (GetDomain()->MustForceTrivialWaitOperations())
        ignoreSyncCtx = TRUE;

    // Unless the ignoreSyncCtx flag is set, first check to see if there is a synchronization
    // context on the current thread and if there is, dispatch to it to do the wait.
    // If  the wait is non alertable we cannot forward the call to the sync context
    // since fundamental parts of the system (such as the GC) rely on non alertable
    // waits not running any managed code. Also if we are past the point in shutdown were we
    // are allowed to run managed code then we can't forward the call to the sync context.
    if (!ignoreSyncCtx
        && alertable
        && !HasThreadStateNC(Thread::TSNC_BlockedForShutdown))
    {
        GCX_COOP();

        BOOL fSyncCtxPresent = FALSE;
        OBJECTREF SyncCtxObj = NULL;
        GCPROTECT_BEGIN(SyncCtxObj)
        {
            GetSynchronizationContext(&SyncCtxObj);
            if (SyncCtxObj != NULL)
            {
                SYNCHRONIZATIONCONTEXTREF syncRef = (SYNCHRONIZATIONCONTEXTREF)SyncCtxObj;
                if (syncRef->IsWaitNotificationRequired())
                {
                    fSyncCtxPresent = TRUE;
                    ret = DoSyncContextWait(&SyncCtxObj, countHandles, handles, waitAll, millis);
                }
            }
        }
        GCPROTECT_END();

        if (fSyncCtxPresent)
            return ret;
    }

    // Before going to pre-emptive mode the thread needs to be flagged as waiting for
    // the debugger. This used to be accomplished by the TS_Interruptible flag but that
    // doesn't work reliably, see DevDiv Bugs 699245. Some methods call in here already in
    // COOP mode so we set the bit before the transition. For the calls that are already
    // in pre-emptive mode those are still buggy. This is only a partial fix.
    BOOL isCoop = PreemptiveGCDisabled();
    ThreadStateNCStackHolder tsNC(isCoop && alertable, TSNC_DebuggerSleepWaitJoin);

    GCX_PREEMP();

    if (alertable)
    {
        DoAppropriateWaitWorkerAlertableHelper(mode);
    }

    StateHolder<MarkOSAlertableWait,UnMarkOSAlertableWait> OSAlertableWait(alertable);

    ThreadStateHolder tsh(alertable, TS_Interruptible | TS_Interrupted);

    ULONGLONG dwStart = 0, dwEnd;
retry:
    if (millis != INFINITE)
    {
        dwStart = CLRGetTickCount64();
    }

    ret = DoAppropriateAptStateWait(countHandles, handles, waitAll, millis, mode);

    if (ret == WAIT_IO_COMPLETION)
    {
        _ASSERTE (alertable);

        if (m_State & TS_Interrupted)
        {
            HandleThreadInterrupt();
        }
        // We could be woken by some spurious APC or an EE APC queued to
        // interrupt us. In the latter case the TS_Interrupted bit will be set
        // in the thread state bits. Otherwise we just go back to sleep again.
        if (millis != INFINITE)
        {
            dwEnd = CLRGetTickCount64();
            if (dwEnd >= dwStart + millis)
            {
                ret = WAIT_TIMEOUT;
                goto WaitCompleted;
            }
            else
            {
                millis -= (DWORD)(dwEnd - dwStart);
            }
        }
        goto retry;
    }
    _ASSERTE((ret >= WAIT_OBJECT_0  && ret < (WAIT_OBJECT_0  + (DWORD)countHandles)) ||
             (ret >= WAIT_ABANDONED && ret < (WAIT_ABANDONED + (DWORD)countHandles)) ||
             (ret == WAIT_TIMEOUT) || (ret == WAIT_FAILED));
    // countHandles is used as an unsigned -- it should never be negative.
    _ASSERTE(countHandles >= 0);

    // We support precisely one WAIT_FAILED case, where we attempt to wait on a
    // thread handle and the thread is in the process of dying we might get a
    // invalid handle substatus. Turn this into a successful wait.
    // There are three cases to consider:
    //  1)  Only waiting on one handle: return success right away.
    //  2)  Waiting for all handles to be signalled: retry the wait without the
    //      affected handle.
    //  3)  Waiting for one of multiple handles to be signalled: return with the
    //      first handle that is either signalled or has become invalid.
    if (ret == WAIT_FAILED)
    {
        DWORD errorCode = ::GetLastError();
        if (errorCode == ERROR_INVALID_PARAMETER)
        {
            if (CheckForDuplicateHandles(countHandles, handles))
                COMPlusThrow(kDuplicateWaitObjectException);
            else
                COMPlusThrowHR(HRESULT_FROM_WIN32(errorCode));
        }
        else if (errorCode == ERROR_ACCESS_DENIED)
        {
            // A Win32 ACL could prevent us from waiting on the handle.
            COMPlusThrow(kUnauthorizedAccessException);
        }
        else if (errorCode == ERROR_NOT_ENOUGH_MEMORY)
        {
            ThrowOutOfMemory();
        }
#ifdef TARGET_UNIX
        else if (errorCode == ERROR_NOT_SUPPORTED)
        {
            // "Wait for any" and "wait for all" operations on multiple wait handles are not supported when a cross-process sync
            // object is included in the array
            COMPlusThrow(kPlatformNotSupportedException, W("PlatformNotSupported_NamedSyncObjectWaitAnyWaitAll"));
        }
#endif
        else if (errorCode != ERROR_INVALID_HANDLE)
        {
            ThrowWin32(errorCode);
        }

        if (countHandles == 1)
            ret = WAIT_OBJECT_0;
        else if (waitAll)
        {
            // Probe all handles with a timeout of zero. When we find one that's
            // invalid, move it out of the list and retry the wait.
            for (int i = 0; i < countHandles; i++)
            {
                // WaitForSingleObject won't pump memssage; we already probe enough space
                // before calling this function and we don't want to fail here, so we don't
                // do a transition to tolerant code here
                DWORD subRet = WaitForSingleObject (handles[i], 0);
                if (subRet != WAIT_FAILED)
                    continue;
                _ASSERTE(::GetLastError() == ERROR_INVALID_HANDLE);
                if ((countHandles - i - 1) > 0)
                    memmove(&handles[i], &handles[i+1], (countHandles - i - 1) * sizeof(HANDLE));
                countHandles--;
                break;
            }

            // Compute the new timeout value by assume that the timeout
            // is not large enough for more than one wrap
            dwEnd = CLRGetTickCount64();
            if (millis != INFINITE)
            {
                if (dwEnd >= dwStart + millis)
                {
                    ret = WAIT_TIMEOUT;
                    goto WaitCompleted;
                }
                else
                {
                    millis -= (DWORD)(dwEnd - dwStart);
                }
            }
            goto retry;
        }
        else
        {
            // Probe all handles with a timeout as zero, succeed with the first
            // handle that doesn't timeout.
            ret = WAIT_OBJECT_0;
            int i;
            for (i = 0; i < countHandles; i++)
            {
            TryAgain:
                // WaitForSingleObject won't pump memssage; we already probe enough space
                // before calling this function and we don't want to fail here, so we don't
                // do a transition to tolerant code here
                DWORD subRet = WaitForSingleObject (handles[i], 0);
                if ((subRet == WAIT_OBJECT_0) || (subRet == WAIT_FAILED))
                    break;
                if (subRet == WAIT_ABANDONED)
                {
                    ret = (ret - WAIT_OBJECT_0) + WAIT_ABANDONED;
                    break;
                }
                // If we get alerted it just masks the real state of the current
                // handle, so retry the wait.
                if (subRet == WAIT_IO_COMPLETION)
                    goto TryAgain;
                _ASSERTE(subRet == WAIT_TIMEOUT);
                ret++;
            }
        }
    }

WaitCompleted:

    _ASSERTE((ret != WAIT_TIMEOUT) || (millis != INFINITE));

    return ret;
}


DWORD Thread::DoAppropriateWaitWorker(AppropriateWaitFunc func, void *args,
                                      DWORD millis, WaitMode mode)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    BOOL alertable = (mode & WaitMode_Alertable)!=0;

    // Before going to pre-emptive mode the thread needs to be flagged as waiting for
    // the debugger. This used to be accomplished by the TS_Interruptible flag but that
    // doesn't work reliably, see DevDiv Bugs 699245. Some methods call in here already in
    // COOP mode so we set the bit before the transition. For the calls that are already
    // in pre-emptive mode those are still buggy. This is only a partial fix.
    BOOL isCoop = PreemptiveGCDisabled();
    ThreadStateNCStackHolder tsNC(isCoop && alertable, TSNC_DebuggerSleepWaitJoin);
    GCX_PREEMP();

    // <TODO>
    // @TODO cwb: we don't know whether a thread has a message pump or
    // how to pump its messages, currently.
    // @TODO cwb: WinCE isn't going to support Thread.Interrupt() correctly until
    // we get alertable waits on that platform.</TODO>
    DWORD ret;
    if(alertable)
    {
        DoAppropriateWaitWorkerAlertableHelper(mode);
    }

    DWORD option;
    if (alertable)
    {
        option = WAIT_ALERTABLE;
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
        ApartmentState as = GetFinalApartment();
        if ((AS_InMTA != as) && !GetDomain()->MustForceTrivialWaitOperations())
        {
            option |= WAIT_MSGPUMP;
        }
#endif  // FEATURE_COMINTEROP_APARTMENT_SUPPORT
    }
    else
    {
        option = 0;
    }

    ThreadStateHolder tsh(alertable, TS_Interruptible | TS_Interrupted);

    ULONGLONG dwStart = 0;
    ULONGLONG dwEnd;

retry:
    if (millis != INFINITE)
    {
        dwStart = CLRGetTickCount64();
    }
    ret = func(args, millis, option);

    if (ret == WAIT_IO_COMPLETION)
    {
        _ASSERTE (alertable);

        if ((m_State & TS_Interrupted))
        {
            HandleThreadInterrupt();
        }
        if (millis != INFINITE)
        {
            dwEnd = CLRGetTickCount64();
            if (dwEnd >= dwStart + millis)
            {
                ret = WAIT_TIMEOUT;
                goto WaitCompleted;
            }
            else
            {
                millis -= (DWORD)(dwEnd - dwStart);
            }
        }
        goto retry;
    }

WaitCompleted:
    _ASSERTE(ret == WAIT_OBJECT_0 ||
             ret == WAIT_ABANDONED ||
             ret == WAIT_TIMEOUT ||
             ret == WAIT_FAILED);

    _ASSERTE((ret != WAIT_TIMEOUT) || (millis != INFINITE));

    return ret;
}

//--------------------------------------------------------------------
// Only one style of wait for DoSignalAndWait since we don't support this on STA Threads
//--------------------------------------------------------------------
DWORD Thread::DoSignalAndWait(HANDLE *handles, DWORD millis, BOOL alertable, PendingSync *syncState)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    _ASSERTE(alertable || syncState == 0);

    struct Param
    {
        Thread *pThis;
        HANDLE *handles;
        DWORD millis;
        BOOL alertable;
        DWORD dwRet;
    } param;
    param.pThis = this;
    param.handles = handles;
    param.millis = millis;
    param.alertable = alertable;
    param.dwRet = (DWORD) -1;

    EE_TRY_FOR_FINALLY(Param *, pParam, &param) {
        pParam->dwRet = pParam->pThis->DoSignalAndWaitWorker(pParam->handles, pParam->millis, pParam->alertable);
    }
    EE_FINALLY {
        if (syncState) {
            if (!GOT_EXCEPTION() && WAIT_OBJECT_0 == param.dwRet) {
                // This thread has been removed from syncblk waiting list by the signalling thread
                syncState->Restore(FALSE);
            }
            else
                syncState->Restore(TRUE);
        }

        _ASSERTE (WAIT_IO_COMPLETION != param.dwRet);
    }
    EE_END_FINALLY;

    return(param.dwRet);
}


DWORD Thread::DoSignalAndWaitWorker(HANDLE* pHandles, DWORD millis,BOOL alertable)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    DWORD ret = 0;

    GCX_PREEMP();

    if(alertable)
    {
        DoAppropriateWaitWorkerAlertableHelper(WaitMode_None);
    }

    StateHolder<MarkOSAlertableWait,UnMarkOSAlertableWait> OSAlertableWait(alertable);

    ThreadStateHolder tsh(alertable, TS_Interruptible | TS_Interrupted);

    ULONGLONG dwStart = 0, dwEnd;

    if (INFINITE != millis)
    {
        dwStart = CLRGetTickCount64();
    }

    ret = SignalObjectAndWait(pHandles[0], pHandles[1], millis, alertable);

retry:

    if (WAIT_IO_COMPLETION == ret)
    {
        _ASSERTE (alertable);
        // We could be woken by some spurious APC or an EE APC queued to
        // interrupt us. In the latter case the TS_Interrupted bit will be set
        // in the thread state bits. Otherwise we just go back to sleep again.
        if ((m_State & TS_Interrupted))
        {
            HandleThreadInterrupt();
        }
        if (INFINITE != millis)
        {
            dwEnd = CLRGetTickCount64();
            if (dwStart + millis <= dwEnd)
            {
                ret = WAIT_TIMEOUT;
                goto WaitCompleted;
            }
            else
            {
                millis -= (DWORD)(dwEnd - dwStart);
            }
            dwStart = CLRGetTickCount64();
        }
        //Retry case we don't want to signal again so only do the wait...
        ret = WaitForSingleObjectEx(pHandles[1],millis,TRUE);
        goto retry;
    }

    if (WAIT_FAILED == ret)
    {
        DWORD errorCode = ::GetLastError();
        //If the handle to signal is a mutex and
        //   the calling thread is not the owner, errorCode is ERROR_NOT_OWNER

        switch(errorCode)
        {
            case ERROR_INVALID_HANDLE:
            case ERROR_NOT_OWNER:
            case ERROR_ACCESS_DENIED:
                COMPlusThrowWin32();
                break;

            case ERROR_TOO_MANY_POSTS:
                ret = ERROR_TOO_MANY_POSTS;
                break;

            default:
                CONSISTENCY_CHECK_MSGF(0, ("This errorCode is not understood '(%d)''\n", errorCode));
                COMPlusThrowWin32();
                break;
        }
    }

WaitCompleted:

    //Check that the return state is valid
    _ASSERTE(WAIT_OBJECT_0 == ret  ||
         WAIT_ABANDONED == ret ||
         WAIT_TIMEOUT == ret ||
         WAIT_FAILED == ret  ||
         ERROR_TOO_MANY_POSTS == ret);

    //Wrong to time out if the wait was infinite
    _ASSERTE((WAIT_TIMEOUT != ret) || (INFINITE != millis));

    return ret;
}

DWORD Thread::DoSyncContextWait(OBJECTREF *pSyncCtxObj, int countHandles, HANDLE *handles, BOOL waitAll, DWORD millis)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(handles));
        PRECONDITION(IsProtectedByGCFrame (pSyncCtxObj));
    }
    CONTRACTL_END;
    MethodDescCallSite invokeWaitMethodHelper(METHOD__SYNCHRONIZATION_CONTEXT__INVOKE_WAIT_METHOD_HELPER);

    BASEARRAYREF handleArrayObj = (BASEARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_I, countHandles);
    memcpyNoGCRefs(handleArrayObj->GetDataPtr(), handles, countHandles * sizeof(HANDLE));

    ARG_SLOT args[6] =
    {
        ObjToArgSlot(*pSyncCtxObj),
        ObjToArgSlot(handleArrayObj),
        BoolToArgSlot(waitAll),
        (ARG_SLOT)millis,
    };

    // Needed by TriggerGCForMDAInternal to avoid infinite recursion
    ThreadStateNCStackHolder holder(TRUE, TSNC_InsideSyncContextWait);

    return invokeWaitMethodHelper.Call_RetI4(args);
}

// Called out of SyncBlock::Wait() to block this thread until the Notify occurs.
BOOL Thread::Block(INT32 timeOut, PendingSync *syncState)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(this == GetThread());

    // Before calling Block, the SyncBlock queued us onto it's list of waiting threads.
    // However, before calling Block the SyncBlock temporarily left the synchronized
    // region.  This allowed threads to enter the region and call Notify, in which
    // case we may have been signalled before we entered the Wait.  So we aren't in the
    // m_WaitSB list any longer.  Not a problem: the following Wait will return
    // immediately.  But it means we cannot enforce the following assertion:
//    _ASSERTE(m_WaitSB != NULL);

    return (Wait(syncState->m_WaitEventLink->m_Next->m_EventWait, timeOut, syncState) != WAIT_OBJECT_0);
}


// Return whether or not a timeout occurred.  TRUE=>we waited successfully
DWORD Thread::Wait(HANDLE *objs, int cntObjs, INT32 timeOut, PendingSync *syncInfo)
{
    WRAPPER_NO_CONTRACT;

    DWORD   dwResult;
    DWORD   dwTimeOut32;

    _ASSERTE(timeOut >= 0 || timeOut == INFINITE_TIMEOUT);

    dwTimeOut32 = (timeOut == INFINITE_TIMEOUT
                   ? INFINITE
                   : (DWORD) timeOut);

    dwResult = DoAppropriateWait(cntObjs, objs, FALSE /*=waitAll*/, dwTimeOut32,
                                 WaitMode_Alertable /*alertable*/,
                                 syncInfo);

    // Either we succeeded in the wait, or we timed out
    _ASSERTE((dwResult >= WAIT_OBJECT_0 && dwResult < (DWORD)(WAIT_OBJECT_0 + cntObjs)) ||
             (dwResult == WAIT_TIMEOUT));

    return dwResult;
}

// Return whether or not a timeout occurred.  TRUE=>we waited successfully
DWORD Thread::Wait(CLREvent *pEvent, INT32 timeOut, PendingSync *syncInfo)
{
    WRAPPER_NO_CONTRACT;

    DWORD   dwResult;
    DWORD   dwTimeOut32;

    _ASSERTE(timeOut >= 0 || timeOut == INFINITE_TIMEOUT);

    dwTimeOut32 = (timeOut == INFINITE_TIMEOUT
                   ? INFINITE
                   : (DWORD) timeOut);

    dwResult = pEvent->Wait(dwTimeOut32, TRUE /*alertable*/, syncInfo);

    // Either we succeeded in the wait, or we timed out
    _ASSERTE((dwResult == WAIT_OBJECT_0) ||
             (dwResult == WAIT_TIMEOUT));

    return dwResult;
}

void Thread::Wake(SyncBlock *psb)
{
    WRAPPER_NO_CONTRACT;

    CLREvent* hEvent = NULL;
    WaitEventLink *walk = &m_WaitEventLink;
    while (walk->m_Next) {
        if (walk->m_Next->m_WaitSB == psb) {
            hEvent = walk->m_Next->m_EventWait;
            // We are guaranteed that only one thread can change walk->m_Next->m_WaitSB
            // since the thread is helding the syncblock.
            walk->m_Next->m_WaitSB = (SyncBlock*)((DWORD_PTR)walk->m_Next->m_WaitSB | 1);
            break;
        }
#ifdef _DEBUG
        else if ((SyncBlock*)((DWORD_PTR)walk->m_Next & ~1) == psb) {
            _ASSERTE (!"Can not wake a thread on the same SyncBlock more than once");
        }
#endif
    }
    PREFIX_ASSUME (hEvent != NULL);
    hEvent->Set();
}

#define WAIT_INTERRUPT_THREADABORT 0x1
#define WAIT_INTERRUPT_INTERRUPT 0x2
#define WAIT_INTERRUPT_OTHEREXCEPTION 0x4

// When we restore
DWORD EnterMonitorForRestore(SyncBlock *pSB)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    DWORD state = 0;
    EX_TRY
    {
        pSB->EnterMonitor();
    }
    EX_CATCH
    {
        // Assume it is a normal exception unless proven.
        state = WAIT_INTERRUPT_OTHEREXCEPTION;
        Thread *pThread = GetThread();
        if (pThread->IsAbortInitiated())
        {
            state = WAIT_INTERRUPT_THREADABORT;
        }
        else if (__pException != NULL)
        {
            if (__pException->GetHR() == COR_E_THREADINTERRUPTED)
            {
                state = WAIT_INTERRUPT_INTERRUPT;
            }
        }
    }
    EX_END_CATCH(SwallowAllExceptions);

    return state;
}

// This is the service that backs us out of a wait that we interrupted.  We must
// re-enter the monitor to the same extent the SyncBlock would, if we returned
// through it (instead of throwing through it).  And we need to cancel the wait,
// if it didn't get notified away while we are processing the interrupt.
void PendingSync::Restore(BOOL bRemoveFromSB)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(m_EnterCount);

    Thread      *pCurThread = GetThread();

    _ASSERTE (pCurThread == m_OwnerThread);

    WaitEventLink *pRealWaitEventLink = m_WaitEventLink->m_Next;

    pRealWaitEventLink->m_RefCount --;
    if (pRealWaitEventLink->m_RefCount == 0)
    {
        if (bRemoveFromSB) {
            ThreadQueue::RemoveThread(pCurThread, pRealWaitEventLink->m_WaitSB);
        }
        if (pRealWaitEventLink->m_EventWait != &pCurThread->m_EventWait) {
            // Put the event back to the pool.
            StoreEventToEventStore(pRealWaitEventLink->m_EventWait);
        }
        // Remove from the link.
        m_WaitEventLink->m_Next = m_WaitEventLink->m_Next->m_Next;
    }

    // Someone up the stack is responsible for keeping the syncblock alive by protecting
    // the object that owns it.  But this relies on assertions that EnterMonitor is only
    // called in cooperative mode.  Even though we are safe in preemptive, do the
    // switch.
    GCX_COOP_THREAD_EXISTS(pCurThread);
    // We need to make sure that EnterMonitor succeeds.  We may have code like
    // lock (a)
    // {
    // a.Wait
    // }
    // We need to make sure that the finally from lock is excuted with the lock owned.
    DWORD state = 0;
    SyncBlock *psb = (SyncBlock*)((DWORD_PTR)pRealWaitEventLink->m_WaitSB & ~1);
    for (LONG i=0; i < m_EnterCount;)
    {
        if ((state & (WAIT_INTERRUPT_THREADABORT | WAIT_INTERRUPT_INTERRUPT)) != 0)
        {
            // If the thread has been interrupted by Thread.Interrupt or Thread.Abort,
            // disable the check at the beginning of DoAppropriateWait
            pCurThread->SetThreadStateNC(Thread::TSNC_InRestoringSyncBlock);
        }
        DWORD result = EnterMonitorForRestore(psb);
        if (result == 0)
        {
            i++;
        }
        else
        {
            // We block the thread until the thread acquires the lock.
            // This is to make sure that when catch/finally is executed, the thread has the lock.
            // We do not want thread to run its catch/finally if the lock is not taken.
            state |= result;

            // If the thread is being rudely aborted, and the thread has
            // no Cer on stack, we will not run managed code to release the
            // lock, so we can terminate the loop.
            if (pCurThread->IsRudeAbortInitiated() &&
                !pCurThread->IsExecutingWithinCer())
            {
                break;
            }
        }
    }

    pCurThread->ResetThreadStateNC(Thread::TSNC_InRestoringSyncBlock);

    if ((state & WAIT_INTERRUPT_THREADABORT) != 0)
    {
        pCurThread->HandleThreadAbort();
    }
    else if ((state & WAIT_INTERRUPT_INTERRUPT) != 0)
    {
        COMPlusThrow(kThreadInterruptedException);
    }
}



// This is the callback from the OS, when we queue an APC to interrupt a waiting thread.
// The callback occurs on the thread we wish to interrupt.  It is a STATIC method.
void WINAPI Thread::UserInterruptAPC(ULONG_PTR data)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(data == APC_Code);

    Thread *pCurThread = GetThreadNULLOk();
    if (pCurThread)
    {
        // We should only take action if an interrupt is currently being
        // requested (our synchronization does not guarantee that we won't fire
        // spuriously). It's safe to check the m_UserInterrupt field and then
        // set TS_Interrupted in a non-atomic fashion because m_UserInterrupt is
        // only cleared in this thread's context (though it may be set from any
        // context).
        if (pCurThread->IsUserInterrupted())
        {
            // Set bit to indicate this routine was called (as opposed to other
            // generic APCs).
            FastInterlockOr((ULONG *) &pCurThread->m_State, TS_Interrupted);
        }
    }
}

// This is the workhorse for Thread.Interrupt().
void Thread::UserInterrupt(ThreadInterruptMode mode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    FastInterlockOr((DWORD*)&m_UserInterrupt, mode);

    if (HasValidThreadHandle() &&
        HasThreadState (TS_Interruptible))
    {
        Alert();
    }
}

// Implementation of Thread.Sleep().
void Thread::UserSleep(INT32 time)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    INCONTRACT(_ASSERTE(!GetThread()->GCNoTrigger()));

    DWORD   res;

    // Before going to pre-emptive mode the thread needs to be flagged as waiting for
    // the debugger. This used to be accomplished by the TS_Interruptible flag but that
    // doesn't work reliably, see DevDiv Bugs 699245.
    ThreadStateNCStackHolder tsNC(TRUE, TSNC_DebuggerSleepWaitJoin);
    GCX_PREEMP();

    // A word about ordering for Interrupt.  If someone tries to interrupt a thread
    // that's in the interruptible state, we queue an APC.  But if they try to interrupt
    // a thread that's not in the interruptible state, we just record that fact.  So
    // we have to set TS_Interruptible before we test to see whether someone wants to
    // interrupt us or else we have a race condition that causes us to skip the APC.
    FastInterlockOr((ULONG *) &m_State, TS_Interruptible);

    // If someone has interrupted us, we should not enter the wait.
    if (IsUserInterrupted())
    {
        HandleThreadInterrupt();
    }

    ThreadStateHolder tsh(TRUE, TS_Interruptible | TS_Interrupted);

    FastInterlockAnd((ULONG *) &m_State, ~TS_Interrupted);

    DWORD dwTime = (DWORD)time;
retry:

    ULONGLONG start = CLRGetTickCount64();

    res = ClrSleepEx (dwTime, TRUE);

    if (res == WAIT_IO_COMPLETION)
    {
        // We could be woken by some spurious APC or an EE APC queued to
        // interrupt us. In the latter case the TS_Interrupted bit will be set
        // in the thread state bits. Otherwise we just go back to sleep again.
        if ((m_State & TS_Interrupted))
        {
            HandleThreadInterrupt();
        }

        if (dwTime == INFINITE)
        {
            goto retry;
        }
        else
        {
            ULONGLONG actDuration = CLRGetTickCount64() - start;

            if (dwTime > actDuration)
            {
                dwTime -= (DWORD)actDuration;
                goto retry;
            }
            else
            {
                res = WAIT_TIMEOUT;
            }
        }
    }
    _ASSERTE(res == WAIT_TIMEOUT || res == WAIT_OBJECT_0);
}


// Correspondence between an EE Thread and an exposed System.Thread:
OBJECTREF Thread::GetExposedObject()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    TRIGGERSGC();

    Thread *pCurThread = GetThreadNULLOk();
    _ASSERTE (!(pCurThread == NULL || IsAtProcessExit()));

    _ASSERTE(pCurThread->PreemptiveGCDisabled());

    if (ObjectFromHandle(m_ExposedObject) == NULL)
    {
        // Allocate the exposed thread object.
        THREADBASEREF attempt = (THREADBASEREF) AllocateObject(g_pThreadClass);
        GCPROTECT_BEGIN(attempt);

        // The exposed object keeps us alive until it is GC'ed.  This
        // doesn't mean the physical thread continues to run, of course.
        // We have to set this outside of the ThreadStore lock, because this might trigger a GC.
        attempt->SetInternal(this);

        BOOL fNeedThreadStore = (! ThreadStore::HoldingThreadStore(pCurThread));
        // Take a lock to make sure that only one thread creates the object.
        ThreadStoreLockHolder tsHolder(fNeedThreadStore);

        // Check to see if another thread has not already created the exposed object.
        if (ObjectFromHandle(m_ExposedObject) == NULL)
        {
            // Keep a weak reference to the exposed object.
            StoreObjectInHandle(m_ExposedObject, (OBJECTREF) attempt);

            ObjectInHandleHolder exposedHolder(m_ExposedObject);

            // Increase the external ref count. We can't call IncExternalCount because we
            // already hold the thread lock and IncExternalCount won't be able to take it.
            ULONG retVal = FastInterlockIncrement ((LONG*)&m_ExternalRefCount);

            // Check to see if we need to store a strong pointer to the object.
            if (retVal > 1)
                StoreObjectInHandle(m_StrongHndToExposedObject, (OBJECTREF) attempt);

            ObjectInHandleHolder strongHolder(m_StrongHndToExposedObject);


            attempt->SetManagedThreadId(GetThreadId());


            // Note that we are NOT calling the constructor on the Thread.  That's
            // because this is an internal create where we don't want a Start
            // address.  And we don't want to expose such a constructor for our
            // customers to accidentally call.  The following is in lieu of a true
            // constructor:
            attempt->InitExisting();

            exposedHolder.SuppressRelease();
            strongHolder.SuppressRelease();
        }
        else
        {
            attempt->ClearInternal();
        }

        GCPROTECT_END();
    }
    return ObjectFromHandle(m_ExposedObject);
}


// We only set non NULL exposed objects for unstarted threads that haven't exited
// their constructor yet.  So there are no race conditions.
void Thread::SetExposedObject(OBJECTREF exposed)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    if (exposed != NULL)
    {
        _ASSERTE (GetThreadNULLOk() != this);
        _ASSERTE(IsUnstarted());
        _ASSERTE(ObjectFromHandle(m_ExposedObject) == NULL);
        // The exposed object keeps us alive until it is GC'ed.  This doesn't mean the
        // physical thread continues to run, of course.
        StoreObjectInHandle(m_ExposedObject, exposed);
        // This makes sure the contexts on the backing thread
        // and the managed thread start off in sync with each other.
        // BEWARE: the IncExternalCount call below may cause GC to happen.

        // IncExternalCount will store exposed in m_StrongHndToExposedObject which is in default domain.
        // If the creating thread is killed before the target thread is killed in Thread.Start, Thread object
        // will be kept alive forever.
        // Instead, IncExternalCount should be called after the target thread has been started in Thread.Start.
        // IncExternalCount();
    }
    else
    {
        // Simply set both of the handles to NULL. The GC of the old exposed thread
        // object will take care of decrementing the external ref count.
        StoreObjectInHandle(m_ExposedObject, NULL);
        StoreObjectInHandle(m_StrongHndToExposedObject, NULL);
    }
}

void Thread::SetLastThrownObject(OBJECTREF throwable, BOOL isUnhandled)
{
    CONTRACTL
    {
        if ((throwable == NULL) || CLRException::IsPreallocatedExceptionObject(throwable)) NOTHROW; else THROWS; // From CreateHandle
        GC_NOTRIGGER;
        if (throwable == NULL) MODE_ANY; else MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    STRESS_LOG_COND1(LF_EH, LL_INFO100, OBJECTREFToObject(throwable) != NULL, "in Thread::SetLastThrownObject: obj = %p\n", OBJECTREFToObject(throwable));

    // you can't have a NULL unhandled exception
    _ASSERTE(!(throwable == NULL && isUnhandled));

    if (m_LastThrownObjectHandle != NULL)
    {
        // We'll somtimes use a handle for a preallocated exception object. We should never, ever destroy one of
        // these handles... they'll be destroyed when the Runtime shuts down.
        if (!CLRException::IsPreallocatedExceptionHandle(m_LastThrownObjectHandle))
        {
            DestroyHandle(m_LastThrownObjectHandle);
        }

        m_LastThrownObjectHandle = NULL; // Make sure to set this to NULL here just in case we throw trying to make
                                         // a new handle below.
    }

    if (throwable != NULL)
    {
        _ASSERTE(this == GetThread());

        // Non-compliant exceptions are always wrapped.
        // The use of the ExceptionNative:: helper here (rather than the global ::IsException helper)
        // is hokey, but we need a GC_NOTRIGGER version and it's only for an ASSERT.
        _ASSERTE(IsException(throwable->GetMethodTable()));

        // If we're tracking one of the preallocated exception objects, then just use the global handle that
        // matches it rather than creating a new one.
        if (CLRException::IsPreallocatedExceptionObject(throwable))
        {
            m_LastThrownObjectHandle = CLRException::GetPreallocatedHandleForObject(throwable);
        }
        else
        {
            m_LastThrownObjectHandle = GetDomain()->CreateHandle(throwable);
        }

        _ASSERTE(m_LastThrownObjectHandle != NULL);
        m_ltoIsUnhandled = isUnhandled;
    }
    else
    {
        m_ltoIsUnhandled = FALSE;
    }
}

void Thread::SetSOForLastThrownObject()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;


    // If we are saving stack overflow exception, we can just null out the current handle.
    // The current domain is going to be unloaded or the process is going to be killed, so
    // we will not leak a handle.
    m_LastThrownObjectHandle = CLRException::GetPreallocatedStackOverflowExceptionHandle();
}

//
// This is a nice wrapper for SetLastThrownObject which catches any exceptions caused by not being able to create
// the handle for the throwable, and setting the last thrown object to the preallocated out of memory exception
// instead.
//
OBJECTREF Thread::SafeSetLastThrownObject(OBJECTREF throwable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        if (throwable == NULL) MODE_ANY; else MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // We return the original throwable if nothing goes wrong.
    OBJECTREF ret = throwable;

    EX_TRY
    {
        // Try to set the throwable.
        SetLastThrownObject(throwable);
    }
    EX_CATCH
    {
        // If it didn't work, then set the last thrown object to the preallocated OOM exception, and return that
        // object instead of the original throwable.
        ret = CLRException::GetPreallocatedOutOfMemoryException();
        SetLastThrownObject(ret);
    }
    EX_END_CATCH(SwallowAllExceptions);

    return ret;
}

//
// This is a nice wrapper for SetThrowable and SetLastThrownObject, which catches any exceptions caused by not
// being able to create the handle for the throwable, and sets the throwable to the preallocated out of memory
// exception instead. It also updates the last thrown object, which is always updated when the throwable is
// updated.
//
OBJECTREF Thread::SafeSetThrowables(OBJECTREF throwable DEBUG_ARG(ThreadExceptionState::SetThrowableErrorChecking stecFlags),
                                    BOOL isUnhandled)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        if (throwable == NULL) MODE_ANY; else MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // We return the original throwable if nothing goes wrong.
    OBJECTREF ret = throwable;

    EX_TRY
    {
        // Try to set the throwable.
        SetThrowable(throwable DEBUG_ARG(stecFlags));

        // Now, if the last thrown object is different, go ahead and update it. This makes sure that we re-throw
        // the right object when we rethrow.
        if (LastThrownObject() != throwable)
        {
            SetLastThrownObject(throwable);
        }

        if (isUnhandled)
        {
            MarkLastThrownObjectUnhandled();
        }
    }
    EX_CATCH
    {
        // If either set didn't work, then set both throwables to the preallocated OOM exception, and return that
        // object instead of the original throwable.
        ret = CLRException::GetPreallocatedOutOfMemoryException();

        // Neither of these will throw because we're setting with a preallocated exception.
        SetThrowable(ret DEBUG_ARG(stecFlags));
        SetLastThrownObject(ret, isUnhandled);
    }
    EX_END_CATCH(SwallowAllExceptions);


    return ret;
}

// This method will sync the managed exception state to be in sync with the topmost active exception
// for a given thread
void Thread::SyncManagedExceptionState(bool fIsDebuggerThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    {
        GCX_COOP();

        // Syncup the LastThrownObject on the managed thread
        SafeUpdateLastThrownObject();
    }
}

void Thread::SetLastThrownObjectHandle(OBJECTHANDLE h)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (m_LastThrownObjectHandle != NULL &&
        !CLRException::IsPreallocatedExceptionHandle(m_LastThrownObjectHandle))
    {
        DestroyHandle(m_LastThrownObjectHandle);
    }

    m_LastThrownObjectHandle = h;
}

//
// Create a duplicate handle of the current throwable and set the last thrown object to that. This ensures that the
// last thrown object and the current throwable have handles that are in the same app domain.
//
void Thread::SafeUpdateLastThrownObject(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTHANDLE hThrowable = GetThrowableAsHandle();

    if (hThrowable != NULL)
    {
        EX_TRY
        {
            IGCHandleManager *pHandleTable = GCHandleUtilities::GetGCHandleManager();

            // Creating a duplicate handle here ensures that the AD of the last thrown object
            // matches the domain of the current throwable.
            OBJECTHANDLE duplicateHandle = pHandleTable->CreateDuplicateHandle(hThrowable);
            SetLastThrownObjectHandle(duplicateHandle);
        }
        EX_CATCH
        {
            // If we can't create a duplicate handle, we set both throwables to the preallocated OOM exception.
            SafeSetThrowables(CLRException::GetPreallocatedOutOfMemoryException());
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
}

// Background threads must be counted, because the EE should shut down when the
// last non-background thread terminates.  But we only count running ones.
void Thread::SetBackground(BOOL isBack)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // booleanize IsBackground() which just returns bits
    if (isBack == !!IsBackground())
        return;

    BOOL lockHeld = HasThreadStateNC(Thread::TSNC_TSLTakenForStartup);
    _ASSERTE(!lockHeld || (lockHeld && ThreadStore::HoldingThreadStore()));

    LOG((LF_SYNC, INFO3, "SetBackground obtain lock\n"));
    ThreadStoreLockHolder TSLockHolder(!lockHeld);

    if (IsDead())
    {
        // This can only happen in a race condition, where the correct thing to do
        // is ignore it.  If it happens without the race condition, we throw an
        // exception.
    }
    else
    if (isBack)
    {
        if (!IsBackground())
        {
            FastInterlockOr((ULONG *) &m_State, TS_Background);

            // unstarted threads don't contribute to the background count
            if (!IsUnstarted())
                ThreadStore::s_pThreadStore->m_BackgroundThreadCount++;

            // If we put the main thread into a wait, until only background threads exist,
            // then we make that
            // main thread a background thread.  This cleanly handles the case where it
            // may or may not be one as it enters the wait.

            // One of the components of OtherThreadsComplete() has changed, so check whether
            // we should now exit the EE.
            ThreadStore::CheckForEEShutdown();
        }
    }
    else
    {
        if (IsBackground())
        {
            FastInterlockAnd((ULONG *) &m_State, ~TS_Background);

            // unstarted threads don't contribute to the background count
            if (!IsUnstarted())
                ThreadStore::s_pThreadStore->m_BackgroundThreadCount--;

            _ASSERTE(ThreadStore::s_pThreadStore->m_BackgroundThreadCount >= 0);
            _ASSERTE(ThreadStore::s_pThreadStore->m_BackgroundThreadCount <=
                     ThreadStore::s_pThreadStore->m_ThreadCount);
        }
    }
}

#ifdef FEATURE_COMINTEROP
class ApartmentSpyImpl : public IUnknownCommon<IInitializeSpy, IID_IInitializeSpy>
{

public:
    HRESULT STDMETHODCALLTYPE PreInitialize(DWORD dwCoInit, DWORD dwCurThreadAptRefs)
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE PostInitialize(HRESULT hrCoInit, DWORD dwCoInit, DWORD dwNewThreadAptRefs)
    {
        LIMITED_METHOD_CONTRACT;
        return hrCoInit; // this HRESULT will be returned from CoInitialize(Ex)
    }

    HRESULT STDMETHODCALLTYPE PreUninitialize(DWORD dwCurThreadAptRefs)
    {
        // Don't assume that Thread exists and do not create it.
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_MODE_PREEMPTIVE;

        HRESULT hr = S_OK;

        if (dwCurThreadAptRefs == 1 && !g_fEEShutDown)
        {
            // This is the last CoUninitialize on this thread and the CLR is still running. If it's an STA
            // we take the opportunity to perform COM/WinRT cleanup now, when the apartment is still alive.

            Thread *pThread = GetThreadNULLOk();
            if (pThread != NULL)
            {
                BEGIN_EXTERNAL_ENTRYPOINT(&hr)
                {
                    if (pThread->GetFinalApartment() == Thread::AS_InSTA)
                    {
                        // This will release RCWs and purge the WinRT factory cache on all AppDomains. It
                        // will also synchronize with the finalizer thread which ensures that the RCWs
                        // that were already in the global RCW cleanup list will be cleaned up as well.
                        //
                        ReleaseRCWsInCachesNoThrow(GetCurrentCtxCookie());
                    }
                }
                END_EXTERNAL_ENTRYPOINT;
            }
        }
        return hr;
    }

    HRESULT STDMETHODCALLTYPE PostUninitialize(DWORD dwNewThreadAptRefs)
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }
};
#endif // FEATURE_COMINTEROP

void Thread::PrepareApartmentAndContext()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifdef TARGET_UNIX
    m_OSThreadId = ::PAL_GetCurrentOSThreadId();
#else
    m_OSThreadId = ::GetCurrentThreadId();
#endif

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // Be very careful in here because we haven't set up e.g. TLS yet.

    if (m_State & (TS_InSTA | TS_InMTA))
    {
        // Make sure TS_InSTA and TS_InMTA aren't both set.
        _ASSERTE(!((m_State & TS_InSTA) && (m_State & TS_InMTA)));

        // Determine the apartment state to set based on the requested state.
        ApartmentState aState = m_State & TS_InSTA ? AS_InSTA : AS_InMTA;

        // Clear the requested apartment state from the thread. This is requested since
        // the thread might actually be a fiber that has already been initialized to
        // a different apartment state than the requested one. If we didn't clear
        // the requested apartment state, then we could end up with both TS_InSTA and
        // TS_InMTA set at the same time.
        FastInterlockAnd ((ULONG *) &m_State, ~TS_InSTA & ~TS_InMTA);

        // Attempt to set the requested apartment state.
        SetApartment(aState);
    }

    // In the case where we own the thread and we have switched it to a different
    // starting context, it is the responsibility of the caller (KickOffThread())
    // to notice that the context changed, and to adjust the delegate that it will
    // dispatch on, as appropriate.
#endif //FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef FEATURE_COMINTEROP
    // Our IInitializeSpy will be registered in classic processes
    // only if the internal config switch is on.
    if (g_pConfig->EnableRCWCleanupOnSTAShutdown())
    {
        NewHolder<ApartmentSpyImpl> pSpyImpl = new ApartmentSpyImpl();

        IfFailThrow(CoRegisterInitializeSpy(pSpyImpl, &m_uliInitializeSpyCookie));
        pSpyImpl.SuppressRelease();

        m_fInitializeSpyRegistered = true;
    }
#endif // FEATURE_COMINTEROP
}

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT

// TS_InSTA (0x00004000) -> AS_InSTA (0)
// TS_InMTA (0x00008000) -> AS_InMTA (1)
#define TS_TO_AS(ts)                                    \
    (Thread::ApartmentState)((((DWORD)ts) >> 14) - 1)   \

// Retrieve the apartment state of the current thread. There are three possible
// states: thread hosts an STA, thread is part of the MTA or thread state is
// undecided. The last state may indicate that the apartment has not been set at
// all (nobody has called CoInitializeEx) or that the EE does not know the
// current state (EE has not called CoInitializeEx).
Thread::ApartmentState Thread::GetApartment()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ApartmentState as = AS_Unknown;
    ThreadState maskedTs = (ThreadState)(((DWORD)m_State) & (TS_InSTA|TS_InMTA));
    if (maskedTs)
    {
        _ASSERTE((maskedTs == TS_InSTA) || (maskedTs == TS_InMTA));
        static_assert_no_msg(TS_TO_AS(TS_InSTA) == AS_InSTA);
        static_assert_no_msg(TS_TO_AS(TS_InMTA) == AS_InMTA);

        as = TS_TO_AS(maskedTs);
    }

    if (as != AS_Unknown)
    {
        return as;
    }

    return GetApartmentRare(as);
}

Thread::ApartmentState Thread::GetApartmentRare(Thread::ApartmentState as)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (this == GetThreadNULLOk())
    {
        THDTYPE type;
        HRESULT hr = S_OK;

        if (as == AS_Unknown)
        {
            hr = GetCurrentThreadTypeNT5(&type);
            if (hr == S_OK)
            {
                as = (type == THDTYPE_PROCESSMESSAGES) ? AS_InSTA : AS_InMTA;

                // If we get back THDTYPE_PROCESSMESSAGES, we are guaranteed to
                // be an STA thread. If not, we are an MTA thread, however
                // we can't know if the thread has been explicitly set to MTA
                // (via a call to CoInitializeEx) or if it has been implicitly
                // made MTA (if it hasn't been CoInitializeEx'd but CoInitialize
                // has already been called on some other thread in the process.
                if (as == AS_InSTA)
                    FastInterlockOr((ULONG *) &m_State, AS_InSTA);
            }
        }
    }

    return as;
}

// Retrieve the explicit apartment state of the current thread. There are three possible
// states: thread hosts an STA, thread is part of the MTA or thread state is
// undecided. The last state may indicate that the apartment has not been set at
// all (nobody has called CoInitializeEx), the EE does not know the
// current state (EE has not called CoInitializeEx), or the thread is implicitly in
// the MTA.
Thread::ApartmentState Thread::GetExplicitApartment()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!((m_State & TS_InSTA) && (m_State & TS_InMTA)));

    // Initialize m_State by calling GetApartment.
    GetApartment();

    ApartmentState as = (m_State & TS_InSTA) ? AS_InSTA :
                        (m_State & TS_InMTA) ? AS_InMTA :
                        AS_Unknown;

    return as;
}

Thread::ApartmentState Thread::GetFinalApartment()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(this == GetThread());

    ApartmentState as = AS_Unknown;
    if (g_fEEShutDown)
    {
        // On shutdown, do not use cached value.  Someone might have called
        // CoUninitialize.
        FastInterlockAnd ((ULONG *) &m_State, ~TS_InSTA & ~TS_InMTA);
    }

    as = GetApartment();
    if (as == AS_Unknown)
    {
        // On Win2k and above, GetApartment will only return AS_Unknown if CoInitialize
        // hasn't been called in the process. In that case we can simply assume MTA. However we
        // cannot cache this value in the Thread because if a CoInitialize does occur, then the
        // thread state might change.
        as = AS_InMTA;
    }

    return as;
}

// when we get apartment tear-down notification,
// we want reset the apartment state we cache on the thread
VOID Thread::ResetApartment()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // reset the TS_InSTA bit and TS_InMTA bit
    ThreadState t_State = (ThreadState)(~(TS_InSTA | TS_InMTA));
    FastInterlockAnd((ULONG *) &m_State, t_State);
}

// Attempt to set current thread's apartment state. The actual apartment state
// achieved is returned and may differ from the input state if someone managed
// to call CoInitializeEx on this thread first (note that calls to SetApartment
// made before the thread has started are guaranteed to succeed).
Thread::ApartmentState Thread::SetApartment(ApartmentState state)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Reset any bits that request for CoInitialize
    ResetRequiresCoInitialize();

    // Setting the state to AS_Unknown indicates we should CoUninitialize
    // the thread.
    if (state == AS_Unknown)
    {
        BOOL needUninitialize = (m_State & TS_CoInitialized)
#ifdef FEATURE_COMINTEROP
            || IsWinRTInitialized()
#endif // FEATURE_COMINTEROP
            ;

        if (needUninitialize)
        {
            GCX_PREEMP();

            // If we haven't CoInitialized the thread, then we don't have anything to do.
            if (m_State & TS_CoInitialized)
            {
                // We should never be attempting to CoUninitialize another thread than
                // the currently running thread.
#ifdef TARGET_UNIX
                _ASSERTE(m_OSThreadId == ::PAL_GetCurrentOSThreadId());
#else
                _ASSERTE(m_OSThreadId == ::GetCurrentThreadId());
#endif

                // CoUninitialize the thread and reset the STA/MTA/CoInitialized state bits.
                ::CoUninitialize();

                ThreadState uninitialized = static_cast<ThreadState>(TS_InSTA | TS_InMTA | TS_CoInitialized);
                FastInterlockAnd((ULONG *) &m_State, ~uninitialized);
            }

#ifdef FEATURE_COMINTEROP
            if (IsWinRTInitialized())
            {
                _ASSERTE(WinRTSupported());
                BaseWinRTUninitialize();
                ResetWinRTInitialized();
            }
#endif // FEATURE_COMINTEROP
        }
        return GetApartment();
    }

    // Call GetApartment to initialize the current apartment state.
    //
    // Important note: For Win2k and above this can return AS_InMTA even if the current
    // thread has never been CoInitialized. Because of this we MUST NOT look at the
    // return value of GetApartment here. We can however look at the m_State flags
    // since these will only be set to TS_InMTA if we know for a fact the the
    // current thread has explicitly been made MTA (via a call to CoInitializeEx).
    GetApartment();

    // If the current thread is STA, then it is impossible to change it to
    // MTA.
    if (m_State & TS_InSTA)
    {
        return AS_InSTA;
    }

    // If the current thread is EXPLICITLY MTA, then it is impossible to change it to
    // STA.
    if (m_State & TS_InMTA)
    {
        return AS_InMTA;
    }

    // If the thread isn't even started yet, we mark the state bits without
    // calling CoInitializeEx (since we're obviously not in the correct thread
    // context yet). We'll retry this call when the thread is started.
    // Don't use the TS_Unstarted state bit to check for this, it's cleared far
    // too late in the day for us. Instead check whether we're in the correct
    // thread context.
#ifdef TARGET_UNIX
    if (m_OSThreadId != ::PAL_GetCurrentOSThreadId())
#else
    if (m_OSThreadId != ::GetCurrentThreadId())
#endif
    {
        FastInterlockOr((ULONG *) &m_State, (state == AS_InSTA) ? TS_InSTA : TS_InMTA);
        return state;
    }

    HRESULT hr;
    {
        GCX_PREEMP();

        // Attempt to set apartment by calling CoInitializeEx. This may fail if
        // another caller (outside EE) beat us to it.
        //
        // Important note: When calling CoInitializeEx(COINIT_MULTITHREADED) on a
        // thread that has never been CoInitialized, the return value will always
        // be S_OK, even if another thread in the process has already been
        // CoInitialized to MTA. However if the current thread has already been
        // CoInitialized to MTA, then S_FALSE will be returned.
        hr = ::CoInitializeEx(NULL, (state == AS_InSTA) ?
                              COINIT_APARTMENTTHREADED : COINIT_MULTITHREADED);
    }

    if (SUCCEEDED(hr))
    {
        ThreadState t_State = (state == AS_InSTA) ? TS_InSTA : TS_InMTA;

        if (hr == S_OK)
        {
            // The thread has never been CoInitialized.
            t_State = (ThreadState)(t_State | TS_CoInitialized);
        }
        else
        {
            _ASSERTE(hr == S_FALSE);

            // If the thread has already been CoInitialized to the proper mode, then
            // we don't want to leave an outstanding CoInit so we CoUninit.
            {
                GCX_PREEMP();
                ::CoUninitialize();
            }
        }

        // We succeeded in setting the apartment state to the requested state.
        FastInterlockOr((ULONG *) &m_State, t_State);
    }
    else if (hr == RPC_E_CHANGED_MODE)
    {
        // We didn't manage to enforce the requested apartment state, but at least
        // we can work out what the state is now.  No need to actually do the CoInit --
        // obviously someone else already took care of that.
        FastInterlockOr((ULONG *) &m_State, ((state == AS_InSTA) ? TS_InMTA : TS_InSTA));
    }
    else if (hr == E_OUTOFMEMORY)
    {
        COMPlusThrowOM();
    }
    else if (hr == E_NOTIMPL)
    {
        COMPlusThrow(kPlatformNotSupportedException, IDS_EE_THREAD_APARTMENT_NOT_SUPPORTED, (state == AS_InSTA) ? W("STA") : W("MTA"));
    }
    else
    {
        _ASSERTE(!"Unexpected HRESULT returned from CoInitializeEx!");
    }

    // If WinRT is supported on this OS, also initialize it at the same time.  Since WinRT sits on top of COM
    // we need to make sure that it is initialized in the same threading mode as we just started COM itself
    // with (or that we detected COM had already been started with).
    if (WinRTSupported() && !IsWinRTInitialized())
    {
        GCX_PREEMP();

        BOOL isSTA = m_State & TS_InSTA;
        _ASSERTE(isSTA || (m_State & TS_InMTA));

        HRESULT hrWinRT = RoInitialize(isSTA ? RO_INIT_SINGLETHREADED : RO_INIT_MULTITHREADED);

        if (SUCCEEDED(hrWinRT))
        {
            if (hrWinRT == S_OK)
            {
                SetThreadStateNC(TSNC_WinRTInitialized);
            }
            else
            {
                _ASSERTE(hrWinRT == S_FALSE);

                // If the thread has already been initialized, back it out. We may not
                // always be able to call RoUninitialize on shutdown so if there's
                // a way to avoid having to, we should take advantage of that.
                RoUninitialize();
            }
        }
        else if (hrWinRT == E_OUTOFMEMORY)
        {
            COMPlusThrowOM();
        }
        else
        {
            // We don't check for RPC_E_CHANGEDMODE, since we're using the mode that was read in by
            // initializing COM above.  COM and WinRT need to always be in the same mode, so we should never
            // see that return code at this point.
            _ASSERTE(!"Unexpected HRESULT From RoInitialize");
        }
    }

    // Since we've just called CoInitialize, COM has effectively been started up.
    // To ensure the CLR is aware of this, we need to call EnsureComStarted.
    EnsureComStarted(FALSE);

    return GetApartment();
}
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT


//----------------------------------------------------------------------------
//
//    ThreadStore Implementation
//
//----------------------------------------------------------------------------

ThreadStore::ThreadStore()
           : m_Crst(CrstThreadStore, (CrstFlags) (CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD)),
             m_ThreadCount(0),
             m_MaxThreadCount(0),
             m_UnstartedThreadCount(0),
             m_BackgroundThreadCount(0),
             m_PendingThreadCount(0),
             m_DeadThreadCount(0),
             m_DeadThreadCountForGCTrigger(0),
             m_TriggerGCForDeadThreads(false),
             m_HoldingThread(0)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_TerminationEvent.CreateManualEvent(FALSE);
    _ASSERTE(m_TerminationEvent.IsValid());
}


void ThreadStore::InitThreadStore()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    s_pThreadStore = new ThreadStore;

    g_pThinLockThreadIdDispenser = new IdDispenser();

    ThreadSuspend::g_pGCSuspendEvent = new CLREvent();
    ThreadSuspend::g_pGCSuspendEvent->CreateManualEvent(FALSE);

    s_pWaitForStackCrawlEvent = new CLREvent();
    s_pWaitForStackCrawlEvent->CreateManualEvent(FALSE);

    s_DeadThreadCountThresholdForGCTrigger =
        static_cast<LONG>(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Thread_DeadThreadCountThresholdForGCTrigger));
    if (s_DeadThreadCountThresholdForGCTrigger < 0)
    {
        s_DeadThreadCountThresholdForGCTrigger = 0;
    }
    s_DeadThreadGCTriggerPeriodMilliseconds =
        CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Thread_DeadThreadGCTriggerPeriodMilliseconds);
    s_DeadThreadGenerationCounts = nullptr;
}

// Enter and leave the critical section around the thread store.  Clients should
// use LockThreadStore and UnlockThreadStore because ThreadStore lock has
// additional semantics well beyond a normal lock.
DEBUG_NOINLINE void ThreadStore::Enter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // we must be in preemptive mode while taking this lock
        // if suspension is in progress, the lock is taken, and there is no way to suspend us once we block
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
    CHECK_ONE_STORE();
    m_Crst.Enter();
}

DEBUG_NOINLINE void ThreadStore::Leave()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
    CHECK_ONE_STORE();
    m_Crst.Leave();
}

void ThreadStore::LockThreadStore()
{
    WRAPPER_NO_CONTRACT;

    // The actual implementation is in ThreadSuspend class since it is coupled
    // with thread suspension logic
    ThreadSuspend::LockThreadStore(ThreadSuspend::SUSPEND_OTHER);
}

void ThreadStore::UnlockThreadStore()
{
    WRAPPER_NO_CONTRACT;

    // The actual implementation is in ThreadSuspend class since it is coupled
    // with thread suspension logic
    ThreadSuspend::UnlockThreadStore(FALSE, ThreadSuspend::SUSPEND_OTHER);
}

// AddThread adds 'newThread' to m_ThreadList
void ThreadStore::AddThread(Thread *newThread)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    LOG((LF_SYNC, INFO3, "AddThread obtain lock\n"));

    BOOL lockHeld = newThread->HasThreadStateNC(Thread::TSNC_TSLTakenForStartup);
    _ASSERTE(!lockHeld || (lockHeld && ThreadStore::HoldingThreadStore()));

    ThreadStoreLockHolder TSLockHolder(!lockHeld);

    s_pThreadStore->m_ThreadList.InsertTail(newThread);

    s_pThreadStore->m_ThreadCount++;
    if (s_pThreadStore->m_MaxThreadCount < s_pThreadStore->m_ThreadCount)
        s_pThreadStore->m_MaxThreadCount = s_pThreadStore->m_ThreadCount;

    if (newThread->IsUnstarted())
        s_pThreadStore->m_UnstartedThreadCount++;

    newThread->SetThreadStateNC(Thread::TSNC_ExistInThreadStore);

    _ASSERTE(!newThread->IsBackground());
    _ASSERTE(!newThread->IsDead());
}

// this function is just desgined to avoid deadlocks during abnormal process termination, and should not be used for any other purpose
BOOL ThreadStore::CanAcquireLock()
{
    WRAPPER_NO_CONTRACT;
    {
        return (s_pThreadStore->m_Crst.m_criticalsection.LockCount == -1 || (size_t)s_pThreadStore->m_Crst.m_criticalsection.OwningThread == (size_t)GetCurrentThreadId());
    }
}

// Whenever one of the components of OtherThreadsComplete() has changed in the
// correct direction, see whether we can now shutdown the EE because only background
// threads are running.
void ThreadStore::CheckForEEShutdown()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (g_fWeControlLifetime &&
        s_pThreadStore->OtherThreadsComplete())
    {
        BOOL bRet;
        bRet = s_pThreadStore->m_TerminationEvent.Set();
        _ASSERTE(bRet);
    }
}


BOOL ThreadStore::RemoveThread(Thread *target)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL    found;
    Thread *ret;

#if 0 // This assert is not valid when failing to create background GC thread.
      // Main GC thread holds the TS lock.
    _ASSERTE (ThreadStore::HoldingThreadStore());
#endif

    _ASSERTE(s_pThreadStore->m_Crst.GetEnterCount() > 0 ||
             IsAtProcessExit());
    _ASSERTE(s_pThreadStore->DbgFindThread(target));
    ret = s_pThreadStore->m_ThreadList.FindAndRemove(target);
    _ASSERTE(ret && ret == target);
    found = (ret != NULL);

    if (found)
    {
        target->ResetThreadStateNC(Thread::TSNC_ExistInThreadStore);

        s_pThreadStore->m_ThreadCount--;

        if (target->IsDead())
        {
            s_pThreadStore->m_DeadThreadCount--;
            s_pThreadStore->DecrementDeadThreadCountForGCTrigger();
        }

        // Unstarted threads are not in the Background count:
        if (target->IsUnstarted())
            s_pThreadStore->m_UnstartedThreadCount--;
        else
        if (target->IsBackground())
            s_pThreadStore->m_BackgroundThreadCount--;

        FastInterlockExchangeAddLong(
            (LONGLONG *)&Thread::s_workerThreadPoolCompletionCountOverflow,
            target->m_workerThreadPoolCompletionCount);
        FastInterlockExchangeAddLong(
            (LONGLONG *)&Thread::s_ioThreadPoolCompletionCountOverflow,
            target->m_ioThreadPoolCompletionCount);
        FastInterlockExchangeAddLong(
            (LONGLONG *)&Thread::s_monitorLockContentionCountOverflow,
            target->m_monitorLockContentionCount);

        _ASSERTE(s_pThreadStore->m_ThreadCount >= 0);
        _ASSERTE(s_pThreadStore->m_BackgroundThreadCount >= 0);
        _ASSERTE(s_pThreadStore->m_ThreadCount >=
                 s_pThreadStore->m_BackgroundThreadCount);
        _ASSERTE(s_pThreadStore->m_ThreadCount >=
                 s_pThreadStore->m_UnstartedThreadCount);
        _ASSERTE(s_pThreadStore->m_ThreadCount >=
                 s_pThreadStore->m_DeadThreadCount);

        // One of the components of OtherThreadsComplete() has changed, so check whether
        // we should now exit the EE.
        CheckForEEShutdown();
    }
    return found;
}

// When a thread is created as unstarted.  Later it may get started, in which case
// someone calls Thread::HasStarted() on that physical thread.  This completes
// the Setup and calls here.
void ThreadStore::TransferStartedThread(Thread *thread)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(thread != NULL);
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() == thread);

    BOOL lockHeld = thread->HasThreadStateNC(Thread::TSNC_TSLTakenForStartup);

    // This ASSERT is correct for one of the following reasons.
    //  - The lock is not currently held which means it will be taken below.
    //  - The thread was created in an Unstarted state and the lock is
    //    being held by the creator thread. The only thing we know for sure
    //    is that the lock is held and not by this thread.
    _ASSERTE(!lockHeld
        || (lockHeld
            && !s_pThreadStore->m_holderthreadid.IsUnknown()
            && ((s_pThreadStore->m_HoldingThread != NULL) || IsGCSpecialThread())
            && !ThreadStore::HoldingThreadStore()));

    LOG((LF_SYNC, INFO3, "TransferStartedThread obtain lock\n"));
    ThreadStoreLockHolder TSLockHolder(!lockHeld);

    _ASSERTE(s_pThreadStore->DbgFindThread(thread));
    _ASSERTE(thread->HasValidThreadHandle());
    _ASSERTE(thread->m_State & Thread::TS_WeOwn);
    _ASSERTE(thread->IsUnstarted());
    _ASSERTE(!thread->IsDead());

    // Of course, m_ThreadCount is already correct since it includes started and
    // unstarted threads.
    s_pThreadStore->m_UnstartedThreadCount--;

    // We only count background threads that have been started
    if (thread->IsBackground())
        s_pThreadStore->m_BackgroundThreadCount++;

    _ASSERTE(s_pThreadStore->m_PendingThreadCount > 0);
    FastInterlockDecrement(&s_pThreadStore->m_PendingThreadCount);

    // As soon as we erase this bit, the thread becomes eligible for suspension,
    // stopping, interruption, etc.
    FastInterlockAnd((ULONG *) &thread->m_State, ~Thread::TS_Unstarted);
    FastInterlockOr((ULONG *) &thread->m_State, Thread::TS_LegalToJoin);

    // One of the components of OtherThreadsComplete() has changed, so check whether
    // we should now exit the EE.
    CheckForEEShutdown();
}

LONG ThreadStore::s_DeadThreadCountThresholdForGCTrigger = 0;
DWORD ThreadStore::s_DeadThreadGCTriggerPeriodMilliseconds = 0;
SIZE_T *ThreadStore::s_DeadThreadGenerationCounts = nullptr;

void ThreadStore::IncrementDeadThreadCountForGCTrigger()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Although all increments and decrements are usually done inside a lock, that is not sufficient to synchronize with a
    // background GC thread resetting this value, hence the interlocked operation. Ignore overflow; overflow would likely never
    // occur, the count is treated as unsigned, and nothing bad would happen if it were to overflow.
    SIZE_T count = static_cast<SIZE_T>(FastInterlockIncrement(&m_DeadThreadCountForGCTrigger));

    SIZE_T countThreshold = static_cast<SIZE_T>(s_DeadThreadCountThresholdForGCTrigger);
    if (count < countThreshold || countThreshold == 0)
    {
        return;
    }

    IGCHeap *gcHeap = GCHeapUtilities::GetGCHeap();
    if (gcHeap == nullptr)
    {
        return;
    }

    SIZE_T gcLastMilliseconds = gcHeap->GetLastGCStartTime(gcHeap->GetMaxGeneration());
    SIZE_T gcNowMilliseconds = gcHeap->GetNow();
    if (gcNowMilliseconds - gcLastMilliseconds < s_DeadThreadGCTriggerPeriodMilliseconds)
    {
        return;
    }

    if (!g_fEEStarted) // required for FinalizerThread::EnableFinalization() below
    {
        return;
    }

    // The GC is triggered on the finalizer thread since it's not safe to trigger it on DLL_THREAD_DETACH.
    // TriggerGCForDeadThreadsIfNecessary() will determine which generation of GC to trigger, and may not actually trigger a GC.
    // If a GC is triggered, since there would be a delay before the dead thread count is updated, clear the count and wait for
    // it to reach the threshold again. If a GC would not be triggered, the count is still cleared here to prevent waking up the
    // finalizer thread to do the work in TriggerGCForDeadThreadsIfNecessary() for every dead thread.
    m_DeadThreadCountForGCTrigger = 0;
    m_TriggerGCForDeadThreads = true;
    FinalizerThread::EnableFinalization();
}

void ThreadStore::DecrementDeadThreadCountForGCTrigger()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Although all increments and decrements are usually done inside a lock, that is not sufficient to synchronize with a
    // background GC thread resetting this value, hence the interlocked operation.
    if (FastInterlockDecrement(&m_DeadThreadCountForGCTrigger) < 0)
    {
        m_DeadThreadCountForGCTrigger = 0;
    }
}

void ThreadStore::OnMaxGenerationGCStarted()
{
    LIMITED_METHOD_CONTRACT;

    // A dead thread may contribute to triggering a GC at most once. After a max-generation GC occurs, if some dead thread
    // objects are still reachable due to references to the thread objects, they will not contribute to triggering a GC again.
    // Synchronize the store with increment/decrement operations occurring on different threads, and make the change visible to
    // other threads in order to prevent unnecessary GC triggers.
    FastInterlockExchange(&m_DeadThreadCountForGCTrigger, 0);
}

bool ThreadStore::ShouldTriggerGCForDeadThreads()
{
    LIMITED_METHOD_CONTRACT;

    return m_TriggerGCForDeadThreads;
}

void ThreadStore::TriggerGCForDeadThreadsIfNecessary()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (!m_TriggerGCForDeadThreads)
    {
        return;
    }
    m_TriggerGCForDeadThreads = false;

    if (g_fEEShutDown)
    {
        // Not safe to touch CLR state
        return;
    }

    unsigned gcGenerationToTrigger = 0;
    IGCHeap *gcHeap = GCHeapUtilities::GetGCHeap();
    _ASSERTE(gcHeap != nullptr);
    SIZE_T generationCountThreshold = static_cast<SIZE_T>(s_DeadThreadCountThresholdForGCTrigger) / 2;
    unsigned maxGeneration = gcHeap->GetMaxGeneration();
    if (!s_DeadThreadGenerationCounts)
    {
        // initialize this field on first use with an entry for every table.
        s_DeadThreadGenerationCounts = new (nothrow) SIZE_T[maxGeneration + 1];
        if (!s_DeadThreadGenerationCounts)
        {
            return;
        }
    }

    memset(s_DeadThreadGenerationCounts, 0, sizeof(SIZE_T) * (maxGeneration + 1));
    {
        ThreadStoreLockHolder threadStoreLockHolder;
        GCX_COOP();

        // Determine the generation for which to trigger a GC. Iterate over all dead threads that have not yet been considered
        // for triggering a GC and see how many are in which generations.
        for (Thread *thread = ThreadStore::GetAllThreadList(NULL, Thread::TS_Dead, Thread::TS_Dead);
            thread != nullptr;
            thread = ThreadStore::GetAllThreadList(thread, Thread::TS_Dead, Thread::TS_Dead))
        {
            if (thread->HasDeadThreadBeenConsideredForGCTrigger())
            {
                continue;
            }

            Object *exposedObject = OBJECTREFToObject(thread->GetExposedObjectRaw());
            if (exposedObject == nullptr)
            {
                continue;
            }

            unsigned exposedObjectGeneration = gcHeap->WhichGeneration(exposedObject);
            SIZE_T newDeadThreadGenerationCount = ++s_DeadThreadGenerationCounts[exposedObjectGeneration];
            if (exposedObjectGeneration > gcGenerationToTrigger && newDeadThreadGenerationCount >= generationCountThreshold)
            {
                gcGenerationToTrigger = exposedObjectGeneration;
                if (gcGenerationToTrigger >= maxGeneration)
                {
                    break;
                }
            }
        }

        // Make sure that enough time has elapsed since the last GC of the desired generation. We don't want to trigger GCs
        // based on this heuristic too often. Give it some time to let the memory pressure trigger GCs automatically, and only
        // if it doesn't in the given time, this heuristic may kick in to trigger a GC.
        SIZE_T gcLastMilliseconds = gcHeap->GetLastGCStartTime(gcGenerationToTrigger);
        SIZE_T gcNowMilliseconds = gcHeap->GetNow();
        if (gcNowMilliseconds - gcLastMilliseconds < s_DeadThreadGCTriggerPeriodMilliseconds)
        {
            return;
        }

        // For threads whose exposed objects are in the generation of GC that will be triggered or in a lower GC generation,
        // mark them as having contributed to a GC trigger to prevent redundant GC triggers
        for (Thread *thread = ThreadStore::GetAllThreadList(NULL, Thread::TS_Dead, Thread::TS_Dead);
            thread != nullptr;
            thread = ThreadStore::GetAllThreadList(thread, Thread::TS_Dead, Thread::TS_Dead))
        {
            if (thread->HasDeadThreadBeenConsideredForGCTrigger())
            {
                continue;
            }

            Object *exposedObject = OBJECTREFToObject(thread->GetExposedObjectRaw());
            if (exposedObject == nullptr)
            {
                continue;
            }

            if (gcGenerationToTrigger < maxGeneration &&
                gcHeap->WhichGeneration(exposedObject) > gcGenerationToTrigger)
            {
                continue;
            }

            thread->SetHasDeadThreadBeenConsideredForGCTrigger();
        }
    } // ThreadStoreLockHolder, GCX_COOP()

    GCHeapUtilities::GetGCHeap()->GarbageCollect(gcGenerationToTrigger, FALSE, collection_non_blocking);
}

#endif // #ifndef DACCESS_COMPILE


// Access the list of threads.  You must be inside a critical section, otherwise
// the "cursor" thread might disappear underneath you.  Pass in NULL for the
// cursor to begin at the start of the list.
Thread *ThreadStore::GetAllThreadList(Thread *cursor, ULONG mask, ULONG bits)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    SUPPORTS_DAC;

#ifndef DACCESS_COMPILE
    _ASSERTE((s_pThreadStore->m_Crst.GetEnterCount() > 0) || IsAtProcessExit());
#endif

    while (TRUE)
    {
        cursor = (cursor
                  ? s_pThreadStore->m_ThreadList.GetNext(cursor)
                  : s_pThreadStore->m_ThreadList.GetHead());

        if (cursor == NULL)
            break;

        if ((cursor->m_State & mask) == bits)
            return cursor;
    }
    return NULL;
}

// Iterate over the threads that have been started
Thread *ThreadStore::GetThreadList(Thread *cursor)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    SUPPORTS_DAC;

    return GetAllThreadList(cursor, (Thread::TS_Unstarted | Thread::TS_Dead), 0);
}

//---------------------------------------------------------------------------------------
//
// Grab a consistent snapshot of the thread's state, for reporting purposes only.
//
// Return Value:
//    the current state of the thread
//

Thread::ThreadState Thread::GetSnapshotState()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    ThreadState res = m_State;

    if (res & TS_ReportDead)
    {
        res = (ThreadState) (res | TS_Dead);
    }

    return res;
}

#ifndef DACCESS_COMPILE

BOOL CLREventWaitWithTry(CLREventBase *pEvent, DWORD timeout, BOOL fAlertable, DWORD *pStatus)
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    BOOL fLoop = TRUE;
    EX_TRY
    {
        *pStatus = pEvent->Wait(timeout, fAlertable);
        fLoop = FALSE;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return fLoop;
}

// We shut down the EE only when all the non-background threads have terminated
// (unless this is an exceptional termination).  So the main thread calls here to
// wait before tearing down the EE.
void ThreadStore::WaitForOtherThreads()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    CHECK_ONE_STORE();

    Thread      *pCurThread = GetThread();

    // Regardless of whether the main thread is a background thread or not, force
    // it to be one.  This simplifies our rules for counting non-background threads.
    pCurThread->SetBackground(TRUE);

    LOG((LF_SYNC, INFO3, "WaitForOtherThreads obtain lock\n"));
    ThreadStoreLockHolder TSLockHolder(TRUE);
    if (!OtherThreadsComplete())
    {
        TSLockHolder.Release();

        FastInterlockOr((ULONG *) &pCurThread->m_State, Thread::TS_ReportDead);

        DWORD ret = WAIT_OBJECT_0;
        while (CLREventWaitWithTry(&m_TerminationEvent, INFINITE, TRUE, &ret))
        {
        }
        _ASSERTE(ret == WAIT_OBJECT_0);
    }
}

#ifdef _DEBUG
BOOL ThreadStore::DbgFindThread(Thread *target)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CHECK_ONE_STORE();

    // Cache the current change stamp for g_TrapReturningThreads
    LONG chgStamp = g_trtChgStamp;
    STRESS_LOG3(LF_STORE, LL_INFO100, "ThreadStore::DbgFindThread - [thread=%p]. trt=%d. chgStamp=%d\n", GetThreadNULLOk(), g_TrapReturningThreads.Load(), chgStamp);

#if 0 // g_TrapReturningThreads debug code.
        int             iRetry = 0;
Retry:
#endif // g_TrapReturningThreads debug code.
    BOOL    found = FALSE;
    Thread *cur = NULL;
    LONG    cnt = 0;
    LONG    cntBack = 0;
    LONG    cntUnstart = 0;
    LONG    cntDead = 0;
    LONG    cntReturn = 0;

    while ((cur = GetAllThreadList(cur, 0, 0)) != NULL)
    {
        cnt++;

        if (cur->IsDead())
            cntDead++;

        // Unstarted threads do not contribute to the count of background threads
        if (cur->IsUnstarted())
            cntUnstart++;
        else
        if (cur->IsBackground())
            cntBack++;

        if (cur == target)
            found = TRUE;

        // Note that (DebugSuspendPending | SuspendPending) implies a count of 2.
        // We don't count GCPending because a single trap is held for the entire
        // GC, instead of counting each interesting thread.
        if (cur->m_State & Thread::TS_DebugSuspendPending)
            cntReturn++;

        if (cur->m_TraceCallCount > 0)
            cntReturn++;

        if (cur->IsAbortRequested())
            cntReturn++;
    }

    _ASSERTE(cnt == m_ThreadCount);
    _ASSERTE(cntUnstart == m_UnstartedThreadCount);
    _ASSERTE(cntBack == m_BackgroundThreadCount);
    _ASSERTE(cntDead == m_DeadThreadCount);
    _ASSERTE(0 <= m_PendingThreadCount);

#if 0 // g_TrapReturningThreads debug code.
    if (cntReturn != g_TrapReturningThreads /*&& !g_fEEShutDown*/)
    {       // If count is off, try again, to account for multiple threads.
        if (iRetry < 4)
        {
            //              printf("Retry %d.  cntReturn:%d, gReturn:%d\n", iRetry, cntReturn, g_TrapReturningThreads);
            ++iRetry;
            goto Retry;
        }
        printf("cnt:%d, Un:%d, Back:%d, Dead:%d, cntReturn:%d, TrapReturn:%d, eeShutdown:%d, threadShutdown:%d\n",
               cnt,cntUnstart,cntBack,cntDead,cntReturn,g_TrapReturningThreads, g_fEEShutDown, Thread::IsAtProcessExit());
        LOG((LF_CORDB, LL_INFO1000,
             "SUSPEND: cnt:%d, Un:%d, Back:%d, Dead:%d, cntReturn:%d, TrapReturn:%d, eeShutdown:%d, threadShutdown:%d\n",
             cnt,cntUnstart,cntBack,cntDead,cntReturn,g_TrapReturningThreads, g_fEEShutDown, Thread::IsAtProcessExit()) );

        //_ASSERTE(cntReturn + 2 >= g_TrapReturningThreads);
    }
    if (iRetry > 0 && iRetry < 4)
    {
        printf("%d retries to re-sync counted TrapReturn with global TrapReturn.\n", iRetry);
    }
#endif // g_TrapReturningThreads debug code.

    STRESS_LOG4(LF_STORE, LL_INFO100, "ThreadStore::DbgFindThread - [thread=%p]. trt=%d. chg=%d. cnt=%d\n", GetThreadNULLOk(), g_TrapReturningThreads.Load(), g_trtChgStamp.Load(), cntReturn);

    // Because of race conditions and the fact that the GC places its
    // own count, I can't assert this precisely.  But I do want to be
    // sure that this count isn't wandering ever higher -- with a
    // nasty impact on the performance of GC mode changes and method
    // call chaining!
    //
    // We don't bother asserting this during process exit, because
    // during a shutdown we will quietly terminate threads that are
    // being waited on.  (If we aren't shutting down, we carefully
    // decrement our counts and alert anyone waiting for us to
    // return).
    //
    // Note: we don't actually assert this if
    // ThreadStore::TrapReturningThreads() updated g_TrapReturningThreads
    // between the beginning of this function and the moment of the assert.
    // *** The order of evaluation in the if condition is important ***
    _ASSERTE(
             (g_trtChgInFlight != 0 || (cntReturn + 2 >= g_TrapReturningThreads) || chgStamp != g_trtChgStamp) ||
             g_fEEShutDown);

    return found;
}

#endif // _DEBUG

void Thread::HandleThreadInterrupt ()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // If we're waiting for shutdown, we don't want to abort/interrupt this thread
    if (HasThreadStateNC(Thread::TSNC_BlockedForShutdown))
        return;

    if ((m_UserInterrupt & TI_Abort) != 0)
    {
        HandleThreadAbort();
    }
    if ((m_UserInterrupt & TI_Interrupt) != 0)
    {
        ResetThreadState ((ThreadState)(TS_Interrupted | TS_Interruptible));
        FastInterlockAnd ((DWORD*)&m_UserInterrupt, ~TI_Interrupt);

        COMPlusThrow(kThreadInterruptedException);
    }
}

#ifdef _DEBUG
#define MAXSTACKBYTES (2 * GetOsPageSize())
void CleanStackForFastGCStress ()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PVOID StackLimit = ClrTeb::GetStackLimit();
    size_t nBytes = (size_t)&nBytes - (size_t)StackLimit;
    nBytes &= ~sizeof (size_t);
    if (nBytes > MAXSTACKBYTES) {
        nBytes = MAXSTACKBYTES;
    }
    size_t* buffer = (size_t*) _alloca (nBytes);
    memset(buffer, 0, nBytes);
    GetThread()->m_pCleanedStackBase = &nBytes;
}

void Thread::ObjectRefFlush(Thread* thread)
{
    // this is debug only code, so no need to validate
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_ENTRY_POINT;

    _ASSERTE(thread->PreemptiveGCDisabled());  // Should have been in managed code
    memset(thread->dangerousObjRefs, 0, sizeof(thread->dangerousObjRefs));
    thread->m_allObjRefEntriesBad = FALSE;
    CLEANSTACKFORFASTGCSTRESS ();
}
#endif

#if defined(STRESS_HEAP)

PtrHashMap *g_pUniqueStackMap = NULL;
Crst *g_pUniqueStackCrst = NULL;

#define UniqueStackDepth 8

BOOL StackCompare (UPTR val1, UPTR val2)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    size_t *p1 = (size_t *)(val1 << 1);
    size_t *p2 = (size_t *)val2;
    if (p1[0] != p2[0]) {
        return FALSE;
    }
    size_t nElem = p1[0];
    if (nElem >= UniqueStackDepth) {
        nElem = UniqueStackDepth;
    }
    p1 ++;
    p2 ++;

    for (size_t n = 0; n < nElem; n ++) {
        if (p1[n] != p2[n]) {
            return FALSE;
        }
    }

    return TRUE;
}

void UniqueStackSetupMap()
{
    WRAPPER_NO_CONTRACT;

    if (g_pUniqueStackCrst == NULL)
    {
        Crst *Attempt = new Crst (
                                     CrstUniqueStack,
                                     CrstFlags(CRST_REENTRANCY | CRST_UNSAFE_ANYMODE));

        if (FastInterlockCompareExchangePointer(&g_pUniqueStackCrst,
                                                Attempt,
                                                NULL) != NULL)
        {
            // We lost the race
            delete Attempt;
        }
    }

    // Now we have a Crst we can use to synchronize the remainder of the init.
    if (g_pUniqueStackMap == NULL)
    {
        CrstHolder ch(g_pUniqueStackCrst);

        if (g_pUniqueStackMap == NULL)
        {
            PtrHashMap *map = new (SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()) PtrHashMap ();
            LockOwner lock = {g_pUniqueStackCrst, IsOwnerOfCrst};
            map->Init (256, StackCompare, TRUE, &lock);
            g_pUniqueStackMap = map;
        }
    }
}

BOOL StartUniqueStackMapHelper()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL fOK = TRUE;
    EX_TRY
    {
        if (g_pUniqueStackMap == NULL)
        {
            UniqueStackSetupMap();
        }
    }
    EX_CATCH
    {
        fOK = FALSE;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return fOK;
}

BOOL StartUniqueStackMap ()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return StartUniqueStackMapHelper();
}

#ifndef TARGET_UNIX

size_t UpdateStackHash(size_t hash, size_t retAddr)
{
    return ((hash << 3) + hash) ^ retAddr;
}

/***********************************************************************/
size_t getStackHash(size_t* stackTrace, size_t* stackTop, size_t* stackStop, size_t stackBase, size_t stackLimit)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // return a hash of every return address found between 'stackTop' (the lowest address)
    // and 'stackStop' (the highest address)

    size_t hash = 0;
    int    idx  = 0;

#ifdef TARGET_X86

    static size_t moduleBase = (size_t) -1;
    static size_t moduleTop = (size_t) -1;
    if (moduleTop == (size_t) -1)
    {
        MEMORY_BASIC_INFORMATION mbi;

        if (ClrVirtualQuery(getStackHash, &mbi, sizeof(mbi)))
        {
            moduleBase = (size_t)mbi.AllocationBase;
            moduleTop = (size_t)mbi.BaseAddress + mbi.RegionSize;
        }
        else
        {
            // way bad error, probably just assert and exit
            _ASSERTE (!"ClrVirtualQuery failed");
            moduleBase = 0;
            moduleTop = 0;
        }
    }

    while (stackTop < stackStop)
    {
        // Clean out things that point to stack, as those can't be return addresses
        if (*stackTop > moduleBase && *stackTop < moduleTop)
        {
            TADDR dummy;

            if (isRetAddr((TADDR)*stackTop, &dummy))
            {
                hash = UpdateStackHash(hash, *stackTop);

                // If there is no jitted code on the stack, then just use the
                // top 16 frames as the context.
                idx++;
                if (idx <= UniqueStackDepth)
                {
                    stackTrace [idx] = *stackTop;
                }
            }
        }
        stackTop++;
    }

#else // TARGET_X86

    CONTEXT ctx;
    ClrCaptureContext(&ctx);

    UINT_PTR            uControlPc = (UINT_PTR)GetIP(&ctx);
    UINT_PTR            uImageBase;

    UINT_PTR uPrevControlPc = uControlPc;

    for (;;)
    {
        RtlLookupFunctionEntry(uControlPc,
                               ARM_ONLY((DWORD*))(&uImageBase),
                               NULL
                               );

        if (((UINT_PTR)GetClrModuleBase()) != uImageBase)
        {
            break;
        }

        uControlPc = Thread::VirtualUnwindCallFrame(&ctx);

        UINT_PTR uRetAddrForHash = uControlPc;

        if (uPrevControlPc == uControlPc)
        {
            // This is a special case when we fail to acquire the loader lock
            // in RtlLookupFunctionEntry(), which then returns false.  The end
            // result is that we cannot go any further on the stack and
            // we will loop infinitely (because the owner of the loader lock
            // is blocked on us).
            hash = 0;
            break;
        }
        else
        {
            uPrevControlPc = uControlPc;
        }

        hash = UpdateStackHash(hash, uRetAddrForHash);

        // If there is no jitted code on the stack, then just use the
        // top 16 frames as the context.
        idx++;
        if (idx <= UniqueStackDepth)
        {
            stackTrace [idx] = uRetAddrForHash;
        }
    }
#endif // TARGET_X86

    stackTrace [0] = idx;

    return(hash);
}

void UniqueStackHelper(size_t stackTraceHash, size_t *stackTrace)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    EX_TRY {
        size_t nElem = stackTrace[0];
        if (nElem >= UniqueStackDepth) {
            nElem = UniqueStackDepth;
        }
        AllocMemHolder<size_t> stackTraceInMap = SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(size_t *)) * (S_SIZE_T(nElem) + S_SIZE_T(1)));
        memcpy (stackTraceInMap, stackTrace, sizeof(size_t *) * (nElem + 1));
        g_pUniqueStackMap->InsertValue(stackTraceHash, stackTraceInMap);
        stackTraceInMap.SuppressRelease();
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

/***********************************************************************/
/* returns true if this stack has not been seen before, useful for
   running tests only once per stack trace.  */

BOOL Thread::UniqueStack(void* stackStart)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

        // If we where not told where to start, start at the caller of UniqueStack
    if (stackStart == 0)
    {
        stackStart = &stackStart;
    }

    if (g_pUniqueStackMap == NULL)
    {
        if (!StartUniqueStackMap ())
        {
            // We fail to initialize unique stack map due to OOM.
            // Let's say the stack is unique.
            return TRUE;
    }
    }

    size_t stackTrace[UniqueStackDepth+1] = {0};

        // stackTraceHash represents a hash of entire stack at the time we make the call,
        // We insure at least GC per unique stackTrace.  What information is contained in
        // 'stackTrace' is somewhat arbitrary.  We choose it to mean all functions live
        // on the stack up to the first jitted function.

    size_t stackTraceHash;
    Thread* pThread = GetThread();


    void* stopPoint = pThread->m_CacheStackBase;

#ifdef TARGET_X86
    // Find the stop point (most jitted function)
    Frame* pFrame = pThread->GetFrame();
    for(;;)
    {
        // skip GC frames
        if (pFrame == 0 || pFrame == (Frame*) -1)
            break;

        pFrame->GetFunction();      // This insures that helper frames are inited

        if (pFrame->GetReturnAddress() != 0)
        {
            stopPoint = pFrame;
            break;
        }
        pFrame = pFrame->Next();
    }
#endif // TARGET_X86

    // Get hash of all return addresses between here an the top most jitted function
    stackTraceHash = getStackHash (stackTrace, (size_t*) stackStart, (size_t*) stopPoint,
        size_t(pThread->m_CacheStackBase), size_t(pThread->m_CacheStackLimit));

    if (stackTraceHash == 0 ||
        g_pUniqueStackMap->LookupValue (stackTraceHash, stackTrace) != (LPVOID)INVALIDENTRY)
    {
        return FALSE;
    }
    BOOL fUnique = FALSE;

    {
        CrstHolder ch(g_pUniqueStackCrst);
#ifdef _DEBUG
        if (GetThreadNULLOk())
            GetThread()->m_bUniqueStacking = TRUE;
#endif
        if (g_pUniqueStackMap->LookupValue (stackTraceHash, stackTrace) != (LPVOID)INVALIDENTRY)
        {
            fUnique = FALSE;
        }
        else
        {
            fUnique = TRUE;
            FAULT_NOT_FATAL();
            UniqueStackHelper(stackTraceHash, stackTrace);
        }
#ifdef _DEBUG
        if (GetThreadNULLOk())
            GetThread()->m_bUniqueStacking = FALSE;
#endif
    }

#ifdef _DEBUG
    static int fCheckStack = -1;
    if (fCheckStack == -1)
    {
        fCheckStack = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_FastGCCheckStack);
    }
    if (fCheckStack && pThread->m_pCleanedStackBase > stackTrace
        && pThread->m_pCleanedStackBase - stackTrace > (int) MAXSTACKBYTES)
    {
        _ASSERTE (!"Garbage on stack");
    }
#endif
    return fUnique;
}

#else // !TARGET_UNIX

BOOL Thread::UniqueStack(void* stackStart)
{
    return FALSE;
}

#endif // !TARGET_UNIX

#endif // STRESS_HEAP


/*
 * GetStackLowerBound
 *
 * Returns the lower bound of the stack space.  Note -- the practical bound is some number of pages greater than
 * this value -- those pages are reserved for a stack overflow exception processing.
 *
 * Parameters:
 *  None
 *
 * Returns:
 *  address of the lower bound of the threads's stack.
 */
void * Thread::GetStackLowerBound()
{
    // Called during fiber switch.  Can not have non-static contract.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

 #ifndef TARGET_UNIX
   MEMORY_BASIC_INFORMATION lowerBoundMemInfo;
    SIZE_T dwRes;

    dwRes = ClrVirtualQuery((const void *)&lowerBoundMemInfo, &lowerBoundMemInfo, sizeof(MEMORY_BASIC_INFORMATION));

    if (sizeof(MEMORY_BASIC_INFORMATION) == dwRes)
    {
        return (void *)(lowerBoundMemInfo.AllocationBase);
    }
    else
    {
        return NULL;
    }
#else // !TARGET_UNIX
    return PAL_GetStackLimit();
#endif // !TARGET_UNIX
}

/*
 * GetStackUpperBound
 *
 * Return the upper bound of the thread's stack space.
 *
 * Parameters:
 *  None
 *
 * Returns:
 *  address of the base of the threads's stack.
 */
void *Thread::GetStackUpperBound()
{
    // Called during fiber switch.  Can not have non-static contract.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    return ClrTeb::GetStackBase();
}

BOOL Thread::SetStackLimits(SetStackLimitScope scope)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (scope == fAll)
    {
        m_CacheStackBase  = GetStackUpperBound();
        m_CacheStackLimit = GetStackLowerBound();
        if (m_CacheStackLimit == NULL)
        {
            _ASSERTE(!"Failed to set stack limits");
            return FALSE;
        }

        // Compute the limit used by EnsureSufficientExecutionStack and cache it on the thread. This minimum stack size should
        // be sufficient to allow a typical non-recursive call chain to execute, including potential exception handling and
        // garbage collection. Used for probing for available stack space through RuntimeImports.EnsureSufficientExecutionStack,
        // among other things.
#ifdef HOST_64BIT
        const UINT_PTR MinExecutionStackSize = 128 * 1024;
#else // !HOST_64BIT
        const UINT_PTR MinExecutionStackSize = 64 * 1024;
#endif // HOST_64BIT
        _ASSERTE(m_CacheStackBase >= m_CacheStackLimit);
        if ((reinterpret_cast<UINT_PTR>(m_CacheStackBase) - reinterpret_cast<UINT_PTR>(m_CacheStackLimit)) >
            MinExecutionStackSize)
        {
            m_CacheStackSufficientExecutionLimit = reinterpret_cast<UINT_PTR>(m_CacheStackLimit) + MinExecutionStackSize;
        }
        else
        {
            m_CacheStackSufficientExecutionLimit = reinterpret_cast<UINT_PTR>(m_CacheStackBase);
        }

        // Compute the limit used by CheckCanUseStackAllocand cache it on the thread. This minimum stack size should
        // be sufficient to avoid all significant risk of a moderate size stack alloc interfering with application behavior
        const UINT_PTR StackAllocNonRiskyExecutionStackSize = 512 * 1024;
        _ASSERTE(m_CacheStackBase >= m_CacheStackLimit);
        if ((reinterpret_cast<UINT_PTR>(m_CacheStackBase) - reinterpret_cast<UINT_PTR>(m_CacheStackLimit)) >
            StackAllocNonRiskyExecutionStackSize)
        {
            m_CacheStackStackAllocNonRiskyExecutionLimit = reinterpret_cast<UINT_PTR>(m_CacheStackLimit) + StackAllocNonRiskyExecutionStackSize;
        }
        else
        {
            m_CacheStackStackAllocNonRiskyExecutionLimit = reinterpret_cast<UINT_PTR>(m_CacheStackBase);
        }
    }

    // Ensure that we've setup the stack guarantee properly before we cache the stack limits
    // as they depend upon the stack guarantee.
    if (FAILED(CLRSetThreadStackGuarantee()))
        return FALSE;

    return TRUE;
}

//---------------------------------------------------------------------------------------------
// Routines we use to managed a thread's stack, for fiber switching or stack overflow purposes.
//---------------------------------------------------------------------------------------------

HRESULT Thread::CLRSetThreadStackGuarantee(SetThreadStackGuaranteeScope fScope)
{
    CONTRACTL
    {
        WRAPPER(NOTHROW);
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    // TODO: we need to measure what the stack usage needs are at the limits in the hosted scenario for host callbacks

    if (Thread::IsSetThreadStackGuaranteeInUse(fScope))
    {
        // <TODO> Tune this as needed </TODO>
        ULONG uGuardSize = SIZEOF_DEFAULT_STACK_GUARANTEE;
        int   EXTRA_PAGES = 0;
#if defined(HOST_64BIT)
        // Free Build EH Stack Stats:
        // --------------------------------
        // currently the maximum stack usage we'll face while handling a SO includes:
        //      4.3k for the OS (kernel32!RaiseException, Rtl EH dispatch code, RtlUnwindEx [second pass])
        //      1.2k for the CLR EH setup (NakedThrowHelper*)
        //      4.5k for other heavy CLR stack creations (2x CONTEXT, 1x REGDISPLAY)
        //     ~1.0k for other misc CLR stack allocations
        //     -----
        //     11.0k --> ~2.75 pages for CLR SO EH dispatch
        //
        // -plus we might need some more for debugger EH dispatch, Watson, etc...
        // -also need to take into account that we can lose up to 1 page of the guard region
        // -additionally, we need to provide some region to hosts to allow for lock acquisition in a hosted scenario
        //
        EXTRA_PAGES = 3;
        INDEBUG(EXTRA_PAGES += 1);

        int ThreadGuardPages = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ThreadGuardPages);
        if (ThreadGuardPages == 0)
        {
            uGuardSize += (EXTRA_PAGES * GetOsPageSize());
        }
        else
        {
            uGuardSize += (ThreadGuardPages * GetOsPageSize());
        }

#else // HOST_64BIT
#ifdef _DEBUG
        uGuardSize += (1 * GetOsPageSize());    // one extra page for debug infrastructure
#endif // _DEBUG
#endif // HOST_64BIT

        LOG((LF_EH, LL_INFO10000, "STACKOVERFLOW: setting thread stack guarantee to 0x%x\n", uGuardSize));

        if (!::SetThreadStackGuarantee(&uGuardSize))
        {
            return HRESULT_FROM_GetLastErrorNA();
        }
    }

#endif // !TARGET_UNIX

    return S_OK;
}


/*
 * GetLastNormalStackAddress
 *
 * GetLastNormalStackAddress returns the last stack address before the guard
 * region of a thread. This is the last address that one could write to before
 * a stack overflow occurs.
 *
 * Parameters:
 *  StackLimit - the base of the stack allocation
 *
 * Returns:
 *  Address of the first page of the guard region.
 */
UINT_PTR Thread::GetLastNormalStackAddress(UINT_PTR StackLimit)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    UINT_PTR cbStackGuarantee = GetStackGuarantee();

    // Here we take the "hard guard region size", the "stack guarantee" and the "fault page" and add them
    // all together.  Note that the "fault page" is the reason for the extra GetOsPageSize() below.  The OS
    // will guarantee us a certain amount of stack remaining after a stack overflow.  This is called the
    // "stack guarantee".  But to do this, it has to fault on the page before that region as the app is
    // allowed to fault at the very end of that page.  So, as a result, the last normal stack address is
    // one page sooner.
    return StackLimit + (cbStackGuarantee
#ifndef TARGET_UNIX
            + GetOsPageSize()
#endif // !TARGET_UNIX
            + HARD_GUARD_REGION_SIZE);
}

#ifdef _DEBUG

static void DebugLogMBIFlags(UINT uState, UINT uProtect)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX

#define LOG_FLAG(flags, name)  \
    if (flags & name) \
    { \
        LOG((LF_EH, LL_INFO1000, "" #name " ")); \
    } \

    if (uState)
    {
        LOG((LF_EH, LL_INFO1000, "State: "));

        LOG_FLAG(uState, MEM_COMMIT);
        LOG_FLAG(uState, MEM_RESERVE);
        LOG_FLAG(uState, MEM_DECOMMIT);
        LOG_FLAG(uState, MEM_RELEASE);
        LOG_FLAG(uState, MEM_FREE);
        LOG_FLAG(uState, MEM_PRIVATE);
        LOG_FLAG(uState, MEM_MAPPED);
        LOG_FLAG(uState, MEM_RESET);
        LOG_FLAG(uState, MEM_TOP_DOWN);
        LOG_FLAG(uState, MEM_WRITE_WATCH);
        LOG_FLAG(uState, MEM_PHYSICAL);
        LOG_FLAG(uState, MEM_LARGE_PAGES);
        LOG_FLAG(uState, MEM_4MB_PAGES);
    }

    if (uProtect)
    {
        LOG((LF_EH, LL_INFO1000, "Protect: "));

        LOG_FLAG(uProtect, PAGE_NOACCESS);
        LOG_FLAG(uProtect, PAGE_READONLY);
        LOG_FLAG(uProtect, PAGE_READWRITE);
        LOG_FLAG(uProtect, PAGE_WRITECOPY);
        LOG_FLAG(uProtect, PAGE_EXECUTE);
        LOG_FLAG(uProtect, PAGE_EXECUTE_READ);
        LOG_FLAG(uProtect, PAGE_EXECUTE_READWRITE);
        LOG_FLAG(uProtect, PAGE_EXECUTE_WRITECOPY);
        LOG_FLAG(uProtect, PAGE_GUARD);
        LOG_FLAG(uProtect, PAGE_NOCACHE);
        LOG_FLAG(uProtect, PAGE_WRITECOMBINE);
    }

#undef LOG_FLAG
#endif // !TARGET_UNIX
}


static void DebugLogStackRegionMBIs(UINT_PTR uLowAddress, UINT_PTR uHighAddress)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    MEMORY_BASIC_INFORMATION meminfo;
    UINT_PTR uStartOfThisRegion = uLowAddress;

    LOG((LF_EH, LL_INFO1000, "----------------------------------------------------------------------\n"));

    while (uStartOfThisRegion < uHighAddress)
    {
        SIZE_T res = ClrVirtualQuery((const void *)uStartOfThisRegion, &meminfo, sizeof(meminfo));

        if (sizeof(meminfo) != res)
        {
            LOG((LF_EH, LL_INFO1000, "VirtualQuery failed on %p\n", uStartOfThisRegion));
            break;
        }

        UINT_PTR uStartOfNextRegion = uStartOfThisRegion + meminfo.RegionSize;

        if (uStartOfNextRegion > uHighAddress)
        {
            uStartOfNextRegion = uHighAddress;
        }

        UINT_PTR uRegionSize = uStartOfNextRegion - uStartOfThisRegion;

        LOG((LF_EH, LL_INFO1000, "0x%p -> 0x%p (%d pg)  ", uStartOfThisRegion, uStartOfNextRegion - 1, uRegionSize / GetOsPageSize()));
        DebugLogMBIFlags(meminfo.State, meminfo.Protect);
        LOG((LF_EH, LL_INFO1000, "\n"));

        uStartOfThisRegion = uStartOfNextRegion;
    }

    LOG((LF_EH, LL_INFO1000, "----------------------------------------------------------------------\n"));
}

// static
void Thread::DebugLogStackMBIs()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    Thread* pThread = GetThreadNULLOk();  // N.B. this can be NULL!

    UINT_PTR uStackLimit        = (UINT_PTR)GetStackLowerBound();
    UINT_PTR uStackBase         = (UINT_PTR)GetStackUpperBound();
    if (pThread)
    {
        uStackLimit        = (UINT_PTR)pThread->GetCachedStackLimit();
        uStackBase         = (UINT_PTR)pThread->GetCachedStackBase();
    }
    else
    {
        uStackLimit        = (UINT_PTR)GetStackLowerBound();
        uStackBase         = (UINT_PTR)GetStackUpperBound();
    }
    UINT_PTR uStackSize         = uStackBase - uStackLimit;

    LOG((LF_EH, LL_INFO1000, "----------------------------------------------------------------------\n"));
    LOG((LF_EH, LL_INFO1000, "Stack Snapshot 0x%p -> 0x%p (%d pg)\n", uStackLimit, uStackBase, uStackSize / GetOsPageSize()));
    if (pThread)
    {
        LOG((LF_EH, LL_INFO1000, "Last normal addr: 0x%p\n", pThread->GetLastNormalStackAddress()));
    }

    DebugLogStackRegionMBIs(uStackLimit, uStackBase);
}
#endif // _DEBUG

NOINLINE void AllocateSomeStack(){
    LIMITED_METHOD_CONTRACT;
#ifdef TARGET_X86
    const size_t size = 0x200;
#else   //TARGET_X86
    const size_t size = 0x400;
#endif  //TARGET_X86

    INT8* mem = (INT8*)_alloca(size);
    // Actually touch the memory we just allocated so the compiler can't
    // optimize it away completely.
    // NOTE: this assumes the stack grows down (towards 0).
    VolatileStore<INT8>(mem, 0);
}

#ifndef TARGET_UNIX

// static // private
BOOL Thread::DoesRegionContainGuardPage(UINT_PTR uLowAddress, UINT_PTR uHighAddress)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    SIZE_T dwRes;
    MEMORY_BASIC_INFORMATION meminfo;
    UINT_PTR uStartOfCurrentRegion = uLowAddress;

    while (uStartOfCurrentRegion < uHighAddress)
    {
#undef VirtualQuery
        // This code can run below YieldTask, which means that it must not call back into the host.
        // The reason is that YieldTask is invoked by the host, and the host needs not be reentrant.
        dwRes = VirtualQuery((const void *)uStartOfCurrentRegion, &meminfo, sizeof(meminfo));
#define VirtualQuery(lpAddress, lpBuffer, dwLength) Dont_Use_VirtualQuery(lpAddress, lpBuffer, dwLength)

        // If the query fails then assume we have no guard page.
        if (sizeof(meminfo) != dwRes)
        {
            return FALSE;
        }

        if (meminfo.Protect & PAGE_GUARD)
        {
            return TRUE;
        }

        uStartOfCurrentRegion += meminfo.RegionSize;
    }

    return FALSE;
}

#endif // !TARGET_UNIX

/*
 * DetermineIfGuardPagePresent
 *
 * DetermineIfGuardPagePresent returns TRUE if the thread's stack contains a proper guard page. This function makes
 * a physical check of the stack, rather than relying on whether or not the CLR is currently processing a stack
 * overflow exception.
 *
 * It seems reasonable to want to check just the 3rd page for !MEM_COMMIT or PAGE_GUARD, but that's no good in a
 * world where a) one can extend the guard region arbitrarily with SetThreadStackGuarantee(), b) a thread's stack
 * could be pre-committed, and c) another lib might reset the guard page very high up on the stack, much as we
 * do. In that world, we have to do VirtualQuery from the lower bound up until we find a region with PAGE_GUARD on
 * it. If we've never SO'd, then that's two calls to VirtualQuery.
 *
 * Parameters:
 *  None
 *
 * Returns:
 *  TRUE if the thread has a guard page, FALSE otherwise.
 */
BOOL Thread::DetermineIfGuardPagePresent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    BOOL bStackGuarded = FALSE;
    UINT_PTR uStackBase = (UINT_PTR)GetCachedStackBase();
    UINT_PTR uStackLimit = (UINT_PTR)GetCachedStackLimit();

    // Note: we start our queries after the hard guard page (one page up from the base of the stack.) We know the
    // very last region of the stack is never the guard page (its always the uncomitted "hard" guard page) so there's
    // no need to waste a query on it.
    bStackGuarded = DoesRegionContainGuardPage(uStackLimit + HARD_GUARD_REGION_SIZE,
                                                uStackBase);

    LOG((LF_EH, LL_INFO10000, "Thread::DetermineIfGuardPagePresent: stack guard page: %s\n", bStackGuarded ? "PRESENT" : "MISSING"));

    return bStackGuarded;
#else // !TARGET_UNIX
    return TRUE;
#endif // !TARGET_UNIX
}

/*
 * GetLastNormalStackAddress
 *
 * GetLastNormalStackAddress returns the last stack address before the guard
 * region of this thread. This is the last address that one could write to
 * before a stack overflow occurs.
 *
 * Parameters:
 *  None
 *
 * Returns:
 *  Address of the first page of the guard region.
 */
UINT_PTR Thread::GetLastNormalStackAddress()
{
    WRAPPER_NO_CONTRACT;

    return GetLastNormalStackAddress((UINT_PTR)m_CacheStackLimit);
}


/*
 * GetStackGuarantee
 *
 * Returns the amount of stack guaranteed after an SO but before the OS rips the process.
 *
 * Parameters:
 *  none
 *
 * Returns:
 *  The stack guarantee in OS pages.
 */
UINT_PTR Thread::GetStackGuarantee()
{
    WRAPPER_NO_CONTRACT;

#ifndef TARGET_UNIX
    // There is a new API available on new OS's called SetThreadStackGuarantee. It allows you to change the size of
    // the guard region on a per-thread basis. If we're running on an OS that supports the API, then we must query
    // it to see if someone has changed the size of the guard region for this thread.
    if (!IsSetThreadStackGuaranteeInUse())
    {
        return SIZEOF_DEFAULT_STACK_GUARANTEE;
    }

    ULONG cbNewStackGuarantee = 0;
    // Passing in a value of 0 means that we're querying, and the value is changed with the new guard region
    // size.
    if (::SetThreadStackGuarantee(&cbNewStackGuarantee) &&
        (cbNewStackGuarantee != 0))
    {
        return cbNewStackGuarantee;
    }
#endif // TARGET_UNIX

    return SIZEOF_DEFAULT_STACK_GUARANTEE;
}

#ifndef TARGET_UNIX

//
// MarkPageAsGuard
//
// Given a page base address, try to turn it into a guard page and then requery to determine success.
//
// static // private
BOOL Thread::MarkPageAsGuard(UINT_PTR uGuardPageBase)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    DWORD flOldProtect;

    ClrVirtualProtect((LPVOID)uGuardPageBase, 1,
                      (PAGE_READWRITE | PAGE_GUARD), &flOldProtect);

    // Intentionally ignore return value -- if it failed, we'll find out below
    // and keep moving up the stack until we either succeed or we hit the guard
    // region.  If we don't succeed before we hit the guard region, we'll end up
    // with a fatal error.

    // Now, make sure the guard page is really there. If its not, then VirtualProtect most likely failed
    // because our stack had grown onto the page we were trying to protect by the time we made it into
    // VirtualProtect. So try the next page down.
    MEMORY_BASIC_INFORMATION meminfo;
    SIZE_T dwRes;

    dwRes = ClrVirtualQuery((const void *)uGuardPageBase, &meminfo, sizeof(meminfo));

    return ((sizeof(meminfo) == dwRes) && (meminfo.Protect & PAGE_GUARD));
}


/*
 * RestoreGuardPage
 *
 * RestoreGuardPage will replace the guard page on this thread's stack. The assumption is that it was removed by
 * the OS due to a stack overflow exception. This function requires that you know that you have enough stack space
 * to restore the guard page, so make sure you know what you're doing when you decide to call this.
 *
 * Parameters:
 *  None
 *
 * Returns:
 *  Nothing
 */
VOID Thread::RestoreGuardPage()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    BOOL bStackGuarded = DetermineIfGuardPagePresent();

    // If the guard page is still there, then just return.
    if (bStackGuarded)
    {
        LOG((LF_EH, LL_INFO100, "Thread::RestoreGuardPage: no need to restore... guard page is already there.\n"));
        return;
    }

    UINT_PTR approxStackPointer;
    UINT_PTR guardPageBase;
    UINT_PTR guardRegionThreshold;
    BOOL     pageMissing;

    if (!bStackGuarded)
    {
    // The normal guard page is the 3rd page from the base. The first page is the "hard" guard, the second one is
    // reserve, and the 3rd one is marked as a guard page. However, since there is now an API (on some platforms)
    // to change the size of the guard region, we'll just go ahead and protect the next page down from where we are
    // now. The guard page will get pushed forward again, just like normal, until the next stack overflow.
        approxStackPointer   = (UINT_PTR)GetCurrentSP();
        guardPageBase        = (UINT_PTR)ALIGN_DOWN(approxStackPointer, GetOsPageSize()) - GetOsPageSize();

        // OS uses soft guard page to update the stack info in TEB.  If our guard page is not beyond the current stack, the TEB
        // will not be updated, and then OS's check of stack during exception will fail.
        if (approxStackPointer >= guardPageBase)
        {
            guardPageBase -= GetOsPageSize();
        }
    // If we're currently "too close" to the page we want to mark as a guard then the call to VirtualProtect to set
    // PAGE_GUARD will fail, but it won't return an error. Therefore, we protect the page, then query it to make
    // sure it worked. If it didn't, we try the next page down. We'll either find a page to protect, or run into
    // the guard region and rip the process down with EEPOLICY_HANDLE_FATAL_ERROR below.
        guardRegionThreshold = GetLastNormalStackAddress();
        pageMissing          = TRUE;

        while (pageMissing)
        {
            LOG((LF_EH, LL_INFO10000,
                 "Thread::RestoreGuardPage: restoring guard page @ 0x%p, approxStackPointer=0x%p, "
                 "last normal stack address=0x%p\n",
                     guardPageBase, approxStackPointer, guardRegionThreshold));

            // Make sure we set the guard page above the guard region.
            if (guardPageBase < guardRegionThreshold)
            {
                goto lFatalError;
            }

            if (MarkPageAsGuard(guardPageBase))
            {
                // The current GuardPage should be beyond the current SP.
                _ASSERTE (guardPageBase < approxStackPointer);
                pageMissing = FALSE;
            }
            else
            {
                guardPageBase -= GetOsPageSize();
            }
        }
    }

    INDEBUG(DebugLogStackMBIs());

    return;

lFatalError:
    STRESS_LOG2(LF_EH, LL_ALWAYS,
                "Thread::RestoreGuardPage: too close to the guard region (0x%p) to restore guard page @0x%p\n",
                guardRegionThreshold, guardPageBase);
    _ASSERTE(!"Too close to the guard page to reset it!");
    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_STACKOVERFLOW);
}

#endif // !TARGET_UNIX

#endif // #ifndef DACCESS_COMPILE

//
// InitRegDisplay: initializes a REGDISPLAY for a thread. If validContext
// is false, pRD is filled from the current context of the thread. The
// thread's current context is also filled in pctx. If validContext is true,
// pctx should point to a valid context and pRD is filled from that.
//
bool Thread::InitRegDisplay(const PREGDISPLAY pRD, PT_CONTEXT pctx, bool validContext)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!validContext)
    {
        if (GetFilterContext()!= NULL)
        {
            pctx = GetFilterContext();
        }
        else
        {
#ifdef DACCESS_COMPILE
            DacNotImpl();
#else
            pctx->ContextFlags = CONTEXT_FULL;

            _ASSERTE(this != GetThreadNULLOk());  // do not call GetThreadContext on the active thread

            BOOL ret = EEGetThreadContext(this, pctx);
            if (!ret)
            {
                SetIP(pctx, 0);
#ifdef TARGET_X86
                pRD->ControlPC = pctx->Eip;
                pRD->PCTAddr = (TADDR)&(pctx->Eip);
#elif defined(TARGET_AMD64)
                // nothing more to do here, on Win64 setting the IP to 0 is enough.
#elif defined(TARGET_ARM)
                // nothing more to do here, on Win64 setting the IP to 0 is enough.
#else
                PORTABILITY_ASSERT("NYI for platform Thread::InitRegDisplay");
#endif

                return false;
            }
#endif // DACCESS_COMPILE
        }
    }

    FillRegDisplay( pRD, pctx );

    return true;
}


void Thread::FillRegDisplay(const PREGDISPLAY pRD, PT_CONTEXT pctx)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    ::FillRegDisplay(pRD, pctx);

#if defined(DEBUG_REGDISPLAY) && !defined(TARGET_X86)
    CONSISTENCY_CHECK(!pRD->_pThread || pRD->_pThread == this);
    pRD->_pThread = this;

    CheckRegDisplaySP(pRD);
#endif // defined(DEBUG_REGDISPLAY) && !defined(TARGET_X86)
}


#ifdef DEBUG_REGDISPLAY

void CheckRegDisplaySP (REGDISPLAY *pRD)
{
    if (pRD->SP && pRD->_pThread)
    {
#ifndef NO_FIXED_STACK_LIMIT
        _ASSERTE(pRD->_pThread->IsExecutingOnAltStack() || PTR_VOID(pRD->SP) >= pRD->_pThread->GetCachedStackLimit());
#endif // NO_FIXED_STACK_LIMIT
        _ASSERTE(pRD->_pThread->IsExecutingOnAltStack() || PTR_VOID(pRD->SP) <  pRD->_pThread->GetCachedStackBase());
    }
}

#endif // DEBUG_REGDISPLAY

//                      Trip Functions
//                      ==============
// When a thread reaches a safe place, it will rendezvous back with us, via one of
// the following trip functions:

void CommonTripThread()
{
#ifndef DACCESS_COMPILE
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    Thread  *thread = GetThread();
    thread->HandleThreadAbort ();

    if (thread->CatchAtSafePoint())
    {
        _ASSERTE(!ThreadStore::HoldingThreadStore(thread));
#ifdef FEATURE_HIJACK
        thread->UnhijackThread();
#endif // FEATURE_HIJACK

        // Trap
        thread->PulseGCMode();
    }
#else
    DacNotImpl();
#endif // #ifndef DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

void Thread::SetFilterContext(CONTEXT *pContext)
{
    // SetFilterContext is like pushing a Frame onto the Frame chain.
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE; // Absolutely must be in coop to coordinate w/ Runtime suspension.
        PRECONDITION(GetThread() == this); // must be on current thread.
    } CONTRACTL_END;

    m_debuggerFilterContext = pContext;
}

#endif // #ifndef DACCESS_COMPILE

T_CONTEXT *Thread::GetFilterContext(void)
{
    LIMITED_METHOD_DAC_CONTRACT;

   return m_debuggerFilterContext;
}

#ifndef DACCESS_COMPILE

void Thread::ClearContext()
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    if (!m_pDomain)
        return;

    // must set exposed context to null first otherwise object verification
    // checks will fail AV when m_Context is null
    m_pDomain = NULL;
#ifdef FEATURE_COMINTEROP
    m_fDisableComObjectEagerCleanup = false;
#endif //FEATURE_COMINTEROP
}

BOOL Thread::HaveExtraWorkForFinalizer()
{
    LIMITED_METHOD_CONTRACT;

    return RequireSyncBlockCleanup()
        || ThreadpoolMgr::HaveTimerInfosToFlush()
        || Thread::CleanupNeededForFinalizedThread()
        || (m_DetachCount > 0)
        || SystemDomain::System()->RequireAppDomainCleanup()
        || YieldProcessorNormalization::IsMeasurementScheduled()
        || ThreadStore::s_pThreadStore->ShouldTriggerGCForDeadThreads();
}

void Thread::DoExtraWorkForFinalizer()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(GetThread() == this);
    _ASSERTE(this == FinalizerThread::GetFinalizerThread());

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    if (RequiresCoInitialize())
    {
        SetApartment(AS_InMTA);
    }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    if (RequireSyncBlockCleanup())
    {
#ifndef TARGET_UNIX
        InteropSyncBlockInfo::FlushStandbyList();
#endif // !TARGET_UNIX

#ifdef FEATURE_COMINTEROP
        RCW::FlushStandbyList();
#endif // FEATURE_COMINTEROP

        SyncBlockCache::GetSyncBlockCache()->CleanupSyncBlocks();
    }
    if (SystemDomain::System()->RequireAppDomainCleanup())
    {
        SystemDomain::System()->ProcessDelayedUnloadLoaderAllocators();
    }

    if(m_DetachCount > 0 || Thread::CleanupNeededForFinalizedThread())
    {
        Thread::CleanupDetachedThreads();
    }

    // If there were any TimerInfos waiting to be released, they'll get flushed now
    ThreadpoolMgr::FlushQueueOfTimerInfos();

    if (YieldProcessorNormalization::IsMeasurementScheduled())
    {
        GCX_PREEMP();
        YieldProcessorNormalization::PerformMeasurement();
    }

    ThreadStore::s_pThreadStore->TriggerGCForDeadThreadsIfNecessary();
}


// HELPERS FOR THE BASE OF A MANAGED THREAD, INCLUDING AD TRANSITION SUPPORT

// We have numerous places where we start up a managed thread.  This includes several places in the
// ThreadPool, the 'new Thread(...).Start()' case, and the Finalizer.  Try to factor the code so our
// base exception handling behavior is consistent across those places.  The resulting code is convoluted,
// but it's better than the prior situation of each thread being on a different plan.

// We need Middle & Outer methods for the usual problem of combining C++ & SEH.

/* The effect of all this is that we get:

                Base of thread -- OS unhandled exception filter that we hook

                SEH handler from DispatchOuter
                C++ handler from DispatchMiddle

                User code that obviously can throw.

*/


struct ManagedThreadCallState
{
    ADCallBackFcnType   pTarget;
    LPVOID                       args;
    UnhandledExceptionLocation   filterType;

    ManagedThreadCallState(ADCallBackFcnType Target,LPVOID Args,
                        UnhandledExceptionLocation   FilterType):
          pTarget(Target),
          args(Args),
          filterType(FilterType)
    {
        LIMITED_METHOD_CONTRACT;
    };
};

// The following static helpers are outside of the ManagedThreadBase struct because I
// don't want to change threads.h whenever I change the mechanism for how unhandled
// exceptions works.  The ManagedThreadBase struct is for the public exposure of the
// API only.

static void ManagedThreadBase_DispatchOuter(ManagedThreadCallState *pCallState);

static void ManagedThreadBase_DispatchInner(ManagedThreadCallState *pCallState)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Go ahead and dispatch the call.
    (*pCallState->pTarget) (pCallState->args);
}

static void ManagedThreadBase_DispatchMiddle(ManagedThreadCallState *pCallState)
{
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    EX_TRY_CPP_ONLY
    {
        // During an unwind, we have some cleanup:
        //
        // 1)  We should no longer suppress any unhandled exception reporting at the base
        //     of the thread, because any handler that contained the exception to the AppDomain
        //     where it occurred is now being removed from the stack.
        //
        // 2)  We need to unwind the Frame chain.  We cannot do it when we get to the __except clause
        //     because at this point we are in the 2nd phase and the stack has been popped.  Any
        //     stack crawling from another thread will see a frame chain in a popped region of stack.
        //     Nor can we pop it in a filter, since this would destroy all the stack-walking information
        //     we need to perform the 2nd pass.  So doing it in a C++ destructor will ensure it happens
        //     during the 2nd pass but before the stack is actually popped.
        class Cleanup
        {
            Frame     *m_pEntryFrame;
            Thread    *m_pThread;

        public:
            Cleanup(Thread* pThread)
            {
                m_pThread = pThread;
                m_pEntryFrame = pThread->m_pFrame;
            }

            ~Cleanup()
            {
                GCX_COOP();
                m_pThread->SetFrame(m_pEntryFrame);
            }
        };

        Cleanup cleanup(GetThread());

        ManagedThreadBase_DispatchInner(pCallState);
    }
    EX_CATCH_CPP_ONLY
    {
        GCX_COOP();
        Exception *pException = GET_EXCEPTION();

        // RudeThreadAbort is a pre-allocated instance of ThreadAbort. So the following is sufficient.
        // For Whidbey, by default only swallow certain exceptions.  If reverting back to Everett's
        // behavior (swallowing all unhandled exception), then swallow all unhandled exception.
        //
        if (IsExceptionOfType(kThreadAbortException, pException))
        {
            // Do nothing to swallow the exception
        }
        else
        {
            // Setting up the unwind_and_continue_handler ensures that C++ exceptions do not leak out.
            //
            // Without unwind_and_continue_handler below, the exception will fly up the stack to
            // this point, where it will be rethrown and thus leak out.
            INSTALL_UNWIND_AND_CONTINUE_HANDLER;

            EX_RETHROW;

            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);
}

/*
typedef struct Param
{
    ManagedThreadCallState * m_pCallState;
    Frame                  * m_pFrame;
    Param(ManagedThreadCallState * pCallState, Frame * pFrame): m_pCallState(pCallState), m_pFrame(pFrame) {}
} TryParam;
*/
typedef struct Param: public NotifyOfCHFFilterWrapperParam
{
    ManagedThreadCallState * m_pCallState;
    Param(ManagedThreadCallState * pCallState): m_pCallState(pCallState) {}
} TryParam;

// Dispatch to the appropriate filter, based on the active CallState.
static LONG ThreadBaseRedirectingFilter(PEXCEPTION_POINTERS pExceptionInfo, LPVOID pParam)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    TryParam * pRealParam = reinterpret_cast<TryParam *>(pParam);
    ManagedThreadCallState * _pCallState = pRealParam->m_pCallState;

    LONG ret = -1;

    // This will invoke the swallowing filter. If that returns EXCEPTION_CONTINUE_SEARCH,
    // it will trigger unhandled exception processing.
    // WARNING - ThreadBaseExceptionAppDomainFilter may not return
    // This occurs when the debugger decides to intercept an exception and catch it in a frame closer
    // to the leaf than the one executing this filter
    ret = ThreadBaseExceptionAppDomainFilter(pExceptionInfo, _pCallState);

    // Although EXCEPTION_EXECUTE_HANDLER can also be returned in cases corresponding to
    // unhandled exceptions, all of those cases have already notified the debugger of an unhandled
    // exception which prevents a second notification indicating the exception was caught
    if (ret == EXCEPTION_EXECUTE_HANDLER)
    {

        // WARNING - NotifyOfCHFFilterWrapper may not return
        // This occurs when the debugger decides to intercept an exception and catch it in a frame closer
        // to the leaf than the one executing this filter
        NotifyOfCHFFilterWrapper(pExceptionInfo, pRealParam);
    }

    // Get the reference to the current thread..
    Thread *pCurThread = GetThread();

    //
    // In the default domain, when an exception goes unhandled on a managed thread whose threadbase is in the VM (e.g. explicitly spawned threads,
    //    ThreadPool threads, finalizer thread, etc), CLR can end up in the unhandled exception processing path twice.
    //
    // The first attempt to perform UE processing happens at the managed thread base (via this function). When it completes,
    // we will set TSNC_ProcessedUnhandledException state against the thread to indicate that we have perform the unhandled exception processing.
    //
    // On CoreSys CoreCLR, the host can ask CoreCLR to run all code in the default domain. As a result, when we return from the first attempt to perform UE
    // processing, the call could return back with EXCEPTION_EXECUTE_HANDLER since, like desktop CoreCLR is instructed by SL host to swallow all unhandled exceptions,
    // CoreSys CoreCLR can also be instructed by its Phone host to swallow all unhandled exceptions. As a result, the exception dispatch will never continue to go upstack
    // to the native threadbase in the OS kernel and thus, there will never be a second attempt to perform UE processing. Hence, we dont, and shouldnt, need to set
    // TSNC_ProcessedUnhandledException state against the thread if we are in SingleAppDomain mode and have been asked to swallow the exception.
    //
    // If we continue to set TSNC_ProcessedUnhandledException and a ThreadPool Thread A has an exception go unhandled, we will swallow it correctly for the first time.
    // The next time Thread A has an exception go unhandled, our UEF will see TSNC_ProcessedUnhandledException set and assume (incorrectly) UE processing has happened and
    // will fail to honor the host policy (e.g. swallow unhandled exception). Thus, the 2nd unhandled exception may end up crashing the app when it should not.
    //
    if (ret != EXCEPTION_EXECUTE_HANDLER)
    {
        LOG((LF_EH, LL_INFO100, "ThreadBaseRedirectingFilter: setting TSNC_ProcessedUnhandledException\n"));

        // Since we have already done unhandled exception processing for it, we dont want it
        // to happen again if our UEF gets invoked upon returning back to the OS.
        //
        // Set the flag to indicate so.
        pCurThread->SetThreadStateNC(Thread::TSNC_ProcessedUnhandledException);
    }

    return ret;
}

static void ManagedThreadBase_DispatchOuter(ManagedThreadCallState *pCallState)
{
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // HasStarted() must have already been performed by our caller
    _ASSERTE(GetThreadNULLOk() != NULL);

    Thread *pThread = GetThread();
#ifdef FEATURE_EH_FUNCLETS
    Frame  *pFrame = pThread->m_pFrame;
#endif // FEATURE_EH_FUNCLETS

    // The sole purpose of having this frame is to tell the debugger that we have a catch handler here
    // which may swallow managed exceptions.  The debugger needs this in order to send a
    // CatchHandlerFound (CHF) notification.
    FrameWithCookie<DebuggerU2MCatchHandlerFrame> catchFrame;

    TryParam param(pCallState);
    param.pFrame = &catchFrame;

    struct TryArgs
    {
        TryParam *pTryParam;
        Thread *pThread;

        BOOL *pfHadException;

#ifdef FEATURE_EH_FUNCLETS
        Frame *pFrame;
#endif // FEATURE_EH_FUNCLETS
    }args;

    args.pTryParam = &param;
    args.pThread = pThread;

    BOOL fHadException = TRUE;
    args.pfHadException = &fHadException;

#ifdef FEATURE_EH_FUNCLETS
    args.pFrame = pFrame;
#endif // FEATURE_EH_FUNCLETS

    PAL_TRY(TryArgs *, pArgs, &args)
    {
        PAL_TRY(TryParam *, pParam, pArgs->pTryParam)
        {
            ManagedThreadBase_DispatchMiddle(pParam->m_pCallState);
        }
        PAL_EXCEPT_FILTER(ThreadBaseRedirectingFilter)
        {
            // Note: one of our C++ exceptions will never reach this filter because they're always caught by
            // the EX_CATCH in ManagedThreadBase_DispatchMiddle().
            //
            // If eCLRDeterminedPolicy, we only swallow for TA, RTA, and ADU exception.
            // For eHostDeterminedPolicy, we will swallow all the managed exception.
    #ifdef FEATURE_EH_FUNCLETS
            // this must be done after the second pass has run, it does not
            // reference anything on the stack, so it is safe to run in an
            // SEH __except clause as well as a C++ catch clause.
            ExceptionTracker::PopTrackers(pArgs->pFrame);
    #endif // FEATURE_EH_FUNCLETS

            _ASSERTE(!pArgs->pThread->IsAbortRequested());
        }
        PAL_ENDTRY;

        *(pArgs->pfHadException) = FALSE;
    }
    PAL_FINALLY
    {
        catchFrame.Pop();
    }
    PAL_ENDTRY;
}


// For the implementation, there are three variants of work possible:

// 1.  Establish the base of a managed thread, and switch to the correct AppDomain.
static void ManagedThreadBase_FullTransition(ADCallBackFcnType pTarget,
                                                   LPVOID args,
                                                   UnhandledExceptionLocation filterType)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    ManagedThreadCallState CallState(pTarget, args, filterType);
    ManagedThreadBase_DispatchOuter(&CallState);
}

// 2.  Establish the base of a managed thread, but the AppDomain transition must be
//     deferred until later.
void ManagedThreadBase_NoADTransition(ADCallBackFcnType pTarget,
                                             UnhandledExceptionLocation filterType)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    AppDomain *pAppDomain = GetAppDomain();

    ManagedThreadCallState CallState(pTarget, NULL, filterType);

    // self-describing, to create a pTurnAround data for eventual delivery to a subsequent AppDomain
    // transition.
    CallState.args = &CallState;

    ManagedThreadBase_DispatchOuter(&CallState);
}



// And here are the various exposed entrypoints for base thread behavior

// The 'new Thread(...).Start()' case from COMSynchronizable kickoff thread worker
void ManagedThreadBase::KickOff(ADCallBackFcnType pTarget, LPVOID args)
{
    WRAPPER_NO_CONTRACT;
    ManagedThreadBase_FullTransition(pTarget, args, ManagedThread);
}

// The IOCompletion, QueueUserWorkItem, AddTimer, RegisterWaitForSingleObject cases in the ThreadPool
void ManagedThreadBase::ThreadPool(ADCallBackFcnType pTarget, LPVOID args)
{
    WRAPPER_NO_CONTRACT;
    ManagedThreadBase_FullTransition(pTarget, args, ThreadPoolThread);
}

// The Finalizer thread establishes exception handling at its base, but defers all the AppDomain
// transitions.
void ManagedThreadBase::FinalizerBase(ADCallBackFcnType pTarget)
{
    WRAPPER_NO_CONTRACT;
    ManagedThreadBase_NoADTransition(pTarget, FinalizerThread);
}

//+----------------------------------------------------------------------------
//
//  Method:     Thread::GetStaticFieldAddress   private
//
//  Synopsis:   Get the address of the field relative to the current thread.
//              If an address has not been assigned yet then create one.
//
//+----------------------------------------------------------------------------

LPVOID Thread::GetStaticFieldAddress(FieldDesc *pFD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(pFD != NULL);
    _ASSERTE(pFD->IsThreadStatic());
    _ASSERTE(!pFD->IsRVA());

    // for static field the MethodTable is exact even for generic classes
    MethodTable *pMT = pFD->GetEnclosingMethodTable();

    // We need to make sure that the class has been allocated, however
    // we should not call the class constructor
    ThreadStatics::GetTLM(pMT)->EnsureClassAllocated(pMT);

    PTR_BYTE base = NULL;

    if (pFD->GetFieldType() == ELEMENT_TYPE_CLASS ||
        pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
    {
        base = pMT->GetGCThreadStaticsBasePointer();
    }
    else
    {
        base = pMT->GetNonGCThreadStaticsBasePointer();
    }

    _ASSERTE(base != NULL);

    DWORD offset = pFD->GetOffset();
    _ASSERTE(offset <= FIELD_OFFSET_LAST_REAL_OFFSET);

    LPVOID result = (LPVOID)((PTR_BYTE)base + (DWORD)offset);

    // For value classes, the handle points at an OBJECTREF
    // which holds the boxed value class, so derefernce and unbox.
    if (pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
    {
        OBJECTREF obj = ObjectToOBJECTREF(*(Object**) result);
        result = obj->GetData();
    }

    return result;
}

#endif // #ifndef DACCESS_COMPILE

 //+----------------------------------------------------------------------------
//
//  Method:     Thread::GetStaticFieldAddrNoCreate   private
//
//  Synopsis:   Get the address of the field relative to the thread.
//              If an address has not been assigned, return NULL.
//              No creating is allowed.
//
//+----------------------------------------------------------------------------

TADDR Thread::GetStaticFieldAddrNoCreate(FieldDesc *pFD)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(pFD != NULL);
    _ASSERTE(pFD->IsThreadStatic());

    // for static field the MethodTable is exact even for generic classes
    PTR_MethodTable pMT = pFD->GetEnclosingMethodTable();

    PTR_BYTE base = NULL;

    if (pFD->GetFieldType() == ELEMENT_TYPE_CLASS ||
        pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
    {
        base = pMT->GetGCThreadStaticsBasePointer(dac_cast<PTR_Thread>(this));
    }
    else
    {
        base = pMT->GetNonGCThreadStaticsBasePointer(dac_cast<PTR_Thread>(this));
    }

    if (base == NULL)
        return NULL;

    DWORD offset = pFD->GetOffset();
    _ASSERTE(offset <= FIELD_OFFSET_LAST_REAL_OFFSET);

    TADDR result = dac_cast<TADDR>(base) + (DWORD)offset;

    // For value classes, the handle points at an OBJECTREF
    // which holds the boxed value class, so derefernce and unbox.
    if (pFD->IsByValue())
    {
        _ASSERTE(result != NULL);
        PTR_Object obj = *PTR_UNCHECKED_OBJECTREF(result);
        if (obj == NULL)
            return NULL;
        result = dac_cast<TADDR>(obj->GetData());
    }

    return result;
}

#ifndef DACCESS_COMPILE

//
// NotifyFrameChainOfExceptionUnwind
// -----------------------------------------------------------
// This method will walk the Frame chain from pStartFrame to
// the last frame that is below pvLimitSP and will call each
// frame's ExceptionUnwind method.  It will return the first
// Frame that is above pvLimitSP.
//
Frame * Thread::NotifyFrameChainOfExceptionUnwind(Frame* pStartFrame, LPVOID pvLimitSP)
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS);  // due to UnwindFrameChain from NOTRIGGER areas
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pStartFrame));
        PRECONDITION(CheckPointer(pvLimitSP));
    }
    CONTRACTL_END;

    Frame * pFrame;

#ifdef _DEBUG
    //
    // assert that the specified Thread's Frame chain actually
    // contains the start Frame.
    //
    pFrame = m_pFrame;
    while ((pFrame != pStartFrame) &&
           (pFrame != FRAME_TOP))
    {
        pFrame = pFrame->Next();
    }
    CONSISTENCY_CHECK_MSG(pFrame == pStartFrame, "pStartFrame is not on pThread's Frame chain!");
#endif // _DEBUG

    pFrame = pStartFrame;
    while (pFrame < pvLimitSP)
    {
        CONSISTENCY_CHECK(pFrame != PTR_NULL);
        CONSISTENCY_CHECK((pFrame) > static_cast<Frame *>((LPVOID)GetCurrentSP()));
        pFrame->ExceptionUnwind();
        pFrame = pFrame->Next();
    }

    // return the frame after the last one notified of the unwind
    return pFrame;
}

//+----------------------------------------------------------------------------
//
//  Method:     Thread::DeleteThreadStaticData   private
//
//  Synopsis:   Delete the static data for each appdomain that this thread
//              visited.
//
//
//+----------------------------------------------------------------------------

void Thread::DeleteThreadStaticData()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_ThreadLocalBlock.FreeTable();
}

//+----------------------------------------------------------------------------
//
//  Method:     Thread::DeleteThreadStaticData   public
//
//  Synopsis:   Delete the static data for the given module. This is called
//              when the AssemblyLoadContext unloads.
//
//
//+----------------------------------------------------------------------------

void Thread::DeleteThreadStaticData(ModuleIndex index)
{
    m_ThreadLocalBlock.FreeTLM(index.m_dwIndex, FALSE /* isThreadShuttingDown */);
}

OBJECTREF Thread::GetCulture(BOOL bUICulture)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // This is the case when we're building CoreLib and haven't yet created
    // the system assembly.
    if (SystemDomain::System()->SystemAssembly()==NULL || g_fForbidEnterEE) {
        return NULL;
    }

    OBJECTREF pCurrentCulture;
    MethodDescCallSite propGet(bUICulture ? METHOD__CULTURE_INFO__GET_CURRENT_UI_CULTURE : METHOD__CULTURE_INFO__GET_CURRENT_CULTURE);
    ARG_SLOT retVal = propGet.Call_RetArgSlot(NULL);
    pCurrentCulture = ArgSlotToObj(retVal);
    return pCurrentCulture;
}

void Thread::SetCulture(OBJECTREF *CultureObj, BOOL bUICulture)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    MethodDescCallSite propSet(bUICulture
        ? METHOD__CULTURE_INFO__SET_CURRENT_UI_CULTURE
        : METHOD__CULTURE_INFO__SET_CURRENT_CULTURE);

    // Set up the Stack.
    ARG_SLOT pNewArgs[] = {
        ObjToArgSlot(*CultureObj)
    };

    // Make the actual call.
    propSet.Call_RetArgSlot(pNewArgs);
}

BOOL ThreadStore::HoldingThreadStore(Thread *pThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (pThread)
    {
        return (pThread == s_pThreadStore->m_HoldingThread);
    }
    else
    {
        return (s_pThreadStore->m_holderthreadid.IsCurrentThread());
    }
}

NOINLINE void Thread::OnIncrementCountOverflow(UINT32 *threadLocalCount, UINT64 *overflowCount)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(threadLocalCount != nullptr);
    _ASSERTE(overflowCount != nullptr);

    // Increment overflow, accumulate the count for this increment into the overflow count and reset the thread-local count

    // The thread store lock, in coordination with other places that read these values, ensures that both changes
    // below become visible together
    ThreadStoreLockHolder tsl;

    *threadLocalCount = 0;
    InterlockedExchangeAdd64((LONGLONG *)overflowCount, (LONGLONG)UINT32_MAX + 1);
}

UINT64 Thread::GetTotalCount(SIZE_T threadLocalCountOffset, UINT64 *overflowCount)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(overflowCount != nullptr);

    // enumerate all threads, summing their local counts.
    ThreadStoreLockHolder tsl;

    UINT64 total = GetOverflowCount(overflowCount);

    Thread *pThread = NULL;
    while ((pThread = ThreadStore::GetAllThreadList(pThread, 0, 0)) != NULL)
    {
        total += *GetThreadLocalCountRef(pThread, threadLocalCountOffset);
    }

    return total;
}

UINT64 Thread::GetTotalThreadPoolCompletionCount()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    bool usePortableThreadPool = ThreadpoolMgr::UsePortableThreadPool();

    // enumerate all threads, summing their local counts.
    ThreadStoreLockHolder tsl;

    UINT64 total = GetIOThreadPoolCompletionCountOverflow();
    if (!usePortableThreadPool)
    {
        total += GetWorkerThreadPoolCompletionCountOverflow();
    }

    Thread *pThread = NULL;
    while ((pThread = ThreadStore::GetAllThreadList(pThread, 0, 0)) != NULL)
    {
        if (!usePortableThreadPool)
        {
            total += pThread->m_workerThreadPoolCompletionCount;
        }
        total += pThread->m_ioThreadPoolCompletionCount;
    }

    return total;
}

INT32 Thread::ResetManagedThreadObject(INT32 nPriority)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    GCX_COOP();
    return ResetManagedThreadObjectInCoopMode(nPriority);
}

INT32 Thread::ResetManagedThreadObjectInCoopMode(INT32 nPriority)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    THREADBASEREF pObject = (THREADBASEREF)ObjectFromHandle(m_ExposedObject);
    if (pObject != NULL)
    {
        pObject->ResetName();
        nPriority = pObject->GetPriority();
    }

    return nPriority;
}

BOOL Thread::IsRealThreadPoolResetNeeded()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if(!IsBackground())
        return TRUE;

    THREADBASEREF pObject = (THREADBASEREF)ObjectFromHandle(m_ExposedObject);

    if(pObject != NULL)
    {
        INT32 nPriority = pObject->GetPriority();

        if(nPriority != ThreadNative::PRIORITY_NORMAL)
            return TRUE;
    }

    return FALSE;
}

void Thread::InternalReset(BOOL fNotFinalizerThread, BOOL fThreadObjectResetNeeded, BOOL fResetAbort)
{
    CONTRACTL {
        NOTHROW;
        if(!fNotFinalizerThread || fThreadObjectResetNeeded) {GC_TRIGGERS;} else {GC_NOTRIGGER;}
    }
    CONTRACTL_END;

    _ASSERTE (this == GetThread());

    INT32 nPriority = ThreadNative::PRIORITY_NORMAL;

    if (!fNotFinalizerThread && this == FinalizerThread::GetFinalizerThread())
    {
        nPriority = ThreadNative::PRIORITY_HIGHEST;
    }

    if(fThreadObjectResetNeeded)
    {
        nPriority = ResetManagedThreadObject(nPriority);
    }

    if (fResetAbort && IsAbortRequested()) {
        UnmarkThreadForAbort();
    }

    if (IsThreadPoolThread() && fThreadObjectResetNeeded)
    {
        SetBackground(TRUE);
        if (nPriority != ThreadNative::PRIORITY_NORMAL)
        {
            SetThreadPriority(THREAD_PRIORITY_NORMAL);
        }
    }
    else if (!fNotFinalizerThread && this == FinalizerThread::GetFinalizerThread())
    {
        SetBackground(TRUE);
        if (nPriority != ThreadNative::PRIORITY_HIGHEST)
        {
            SetThreadPriority(THREAD_PRIORITY_HIGHEST);
        }
    }
}

DeadlockAwareLock::DeadlockAwareLock(const char *description)
  : m_pHoldingThread(NULL)
#ifdef _DEBUG
    , m_description(description)
#endif
{
    LIMITED_METHOD_CONTRACT;
}

DeadlockAwareLock::~DeadlockAwareLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Wait for another thread to leave its loop in DeadlockAwareLock::TryBeginEnterLock
    CrstHolder lock(&g_DeadlockAwareCrst);
}

CHECK DeadlockAwareLock::CheckDeadlock(Thread *pThread)
{
    CONTRACTL
    {
        PRECONDITION(g_DeadlockAwareCrst.OwnedByCurrentThread());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Note that this check is recursive in order to produce descriptive check failure messages.
    Thread *pHoldingThread = m_pHoldingThread.Load();
    if (pThread == pHoldingThread)
    {
        CHECK_FAILF(("Lock %p (%s) is held by thread %d", this, m_description, pThread));
    }

    if (pHoldingThread != NULL)
    {
        DeadlockAwareLock *pBlockingLock = pHoldingThread->m_pBlockingLock.Load();
        if (pBlockingLock != NULL)
        {
            CHECK_MSGF(pBlockingLock->CheckDeadlock(pThread),
                       ("Deadlock: Lock %p (%s) is held by thread %d", this, m_description, pHoldingThread));
        }
    }

    CHECK_OK;
}

BOOL DeadlockAwareLock::CanEnterLock()
{
    Thread * pThread = GetThread();
    CONSISTENCY_CHECK_MSG(pThread->m_pBlockingLock.Load() == NULL,
                          "Cannot block on two locks at once");

    {
        CrstHolder lock(&g_DeadlockAwareCrst);

        // Look for deadlocks
        DeadlockAwareLock *pLock = this;

        while (TRUE)
        {
            Thread * holdingThread = pLock->m_pHoldingThread;

            if (holdingThread == pThread)
            {
                // Deadlock!
                return FALSE;
            }
            if (holdingThread == NULL)
            {
                // Lock is unheld
                break;
            }

            pLock = holdingThread->m_pBlockingLock;

            if (pLock == NULL)
            {
                // Thread is running free
                break;
            }
        }

        return TRUE;
    }
}

BOOL DeadlockAwareLock::TryBeginEnterLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread * pThread = GetThread();
    CONSISTENCY_CHECK_MSG(pThread->m_pBlockingLock.Load() == NULL,
                          "Cannot block on two locks at once");

    {
        CrstHolder lock(&g_DeadlockAwareCrst);

        // Look for deadlocks
        DeadlockAwareLock *pLock = this;

        while (TRUE)
        {
            Thread * holdingThread = pLock->m_pHoldingThread;

            if (holdingThread == pThread)
            {
                // Deadlock!
                return FALSE;
            }
            if (holdingThread == NULL)
            {
                // Lock is unheld
                break;
            }

            pLock = holdingThread->m_pBlockingLock;

            if (pLock == NULL)
            {
                // Thread is running free
                break;
            }
        }

        pThread->m_pBlockingLock = this;
    }

    return TRUE;
};

void DeadlockAwareLock::BeginEnterLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread * pThread = GetThread();
    CONSISTENCY_CHECK_MSG(pThread->m_pBlockingLock.Load() == NULL,
                          "Cannot block on two locks at once");

    {
        CrstHolder lock(&g_DeadlockAwareCrst);

        // Look for deadlock loop
        CONSISTENCY_CHECK_MSG(CheckDeadlock(pThread), "Deadlock detected!");

        pThread->m_pBlockingLock = this;
    }
};

void DeadlockAwareLock::EndEnterLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread * pThread = GetThread();

    CONSISTENCY_CHECK(m_pHoldingThread.Load() == NULL || m_pHoldingThread.Load() == pThread);
    CONSISTENCY_CHECK(pThread->m_pBlockingLock.Load() == this);

    // No need to take a lock when going from blocking to holding.  This
    // transition implies the lack of a deadlock that other threads can see.
    // (If they would see a deadlock after the transition, they would see
    // one before as well.)

    m_pHoldingThread = pThread;
}

void DeadlockAwareLock::LeaveLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CONSISTENCY_CHECK(m_pHoldingThread == GetThread());
    CONSISTENCY_CHECK(GetThread()->m_pBlockingLock.Load() == NULL);

    m_pHoldingThread = NULL;
}


#ifdef _DEBUG

// Normally, any thread we operate on has a Thread block in its TLS.  But there are
// a few special threads we don't normally execute managed code on.
//
// There is a scenario where we run managed code on such a thread, which is when the
// DLL_THREAD_ATTACH notification of an (IJW?) module calls into managed code.  This
// is incredibly dangerous.  If a GC is provoked, the system may have trouble performing
// the GC because its threads aren't available yet.
static DWORD SpecialEEThreads[10];
static LONG  cnt_SpecialEEThreads = 0;

void dbgOnly_IdentifySpecialEEThread()
{
    WRAPPER_NO_CONTRACT;

    LONG  ourCount = FastInterlockIncrement(&cnt_SpecialEEThreads);

    _ASSERTE(ourCount < (LONG) NumItems(SpecialEEThreads));
    SpecialEEThreads[ourCount-1] = ::GetCurrentThreadId();
}

BOOL dbgOnly_IsSpecialEEThread()
{
    WRAPPER_NO_CONTRACT;

    DWORD   ourId = ::GetCurrentThreadId();

    for (LONG i=0; i<cnt_SpecialEEThreads; i++)
        if (ourId == SpecialEEThreads[i])
            return TRUE;

    // If we have an EE thread doing helper thread duty, then it is temporarily
    // 'special' too.
    #ifdef DEBUGGING_SUPPORTED
    if (g_pDebugInterface)
    {
        //<TODO>We probably should use Thread::GetThreadId</TODO>
        DWORD helperID = g_pDebugInterface->GetHelperThreadID();
        if (helperID == ourId)
            return TRUE;
    }
    #endif

    //<TODO>Clean this up</TODO>
    if (GetThreadNULLOk() == NULL)
        return TRUE;


    return FALSE;
}

#endif // _DEBUG

void Thread::StaticInitialize()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_SPECIAL_USER_MODE_APC
    InitializeSpecialUserModeApc();

    // When CET shadow stacks are enabled, support for special user-mode APCs with the necessary functionality is required
    _ASSERTE_ALL_BUILDS(__FILE__, !AreCetShadowStacksEnabled() || UseSpecialUserModeApc());
#endif
}

#ifdef FEATURE_SPECIAL_USER_MODE_APC

QueueUserAPC2Proc Thread::s_pfnQueueUserAPC2Proc;

static void NTAPI EmptyApcCallback(ULONG_PTR Parameter)
{
    LIMITED_METHOD_CONTRACT;
}

void Thread::InitializeSpecialUserModeApc()
{
    WRAPPER_NO_CONTRACT;
    static_assert_no_msg(OFFSETOF__APC_CALLBACK_DATA__ContextRecord == offsetof(CLONE_APC_CALLBACK_DATA, ContextRecord));

    HMODULE hKernel32 = WszLoadLibraryEx(WINDOWS_KERNEL32_DLLNAME_W, NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);

    // See if QueueUserAPC2 exists
    QueueUserAPC2Proc pfnQueueUserAPC2Proc = (QueueUserAPC2Proc)GetProcAddress(hKernel32, "QueueUserAPC2");
    if (pfnQueueUserAPC2Proc == nullptr)
    {
        return;
    }

    // See if QueueUserAPC2 supports the special user-mode APC with a callback that includes the interrupted CONTEXT. A special
    // user-mode APC can interrupt a thread that is in user mode and not in a non-alertable wait.
    if (!(*pfnQueueUserAPC2Proc)(EmptyApcCallback, GetCurrentThread(), 0, SpecialUserModeApcWithContextFlags))
    {
        return;
    }

    // In the future, once code paths using the special user-mode APC get some bake time, it should be used regardless of
    // whether CET shadow stacks are enabled
    if (AreCetShadowStacksEnabled())
    {
        s_pfnQueueUserAPC2Proc = pfnQueueUserAPC2Proc;
    }
}

#endif // FEATURE_SPECIAL_USER_MODE_APC
#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
STATIC_DATA::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    DAC_ENUM_STHIS(STATIC_DATA);
}

void
Thread::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    DAC_ENUM_DTHIS();
    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        if (m_pDomain.IsValid())
        {
            m_pDomain->EnumMemoryRegions(flags, true);
        }
    }

    if (m_debuggerFilterContext.IsValid())
    {
        m_debuggerFilterContext.EnumMem();
    }

    OBJECTHANDLE_EnumMemoryRegions(m_LastThrownObjectHandle);

    m_ExceptionState.EnumChainMemoryRegions(flags);

    m_ThreadLocalBlock.EnumMemoryRegions(flags);

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {

        //
        // Allow all of the frames on the stack to enumerate
        // their memory.
        //

        PTR_Frame frame = m_pFrame;
        while (frame.IsValid() &&
               frame.GetAddr() != dac_cast<TADDR>(FRAME_TOP))
        {
            frame->EnumMemoryRegions(flags);
            frame = frame->m_Next;
        }
    }

    //
    // Try and do a stack trace and save information
    // for each part of the stack.  This is very vulnerable
    // to memory problems so ignore all exceptions here.
    //

    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED
    (
        EnumMemoryRegionsWorker(flags);
    );
}

void
Thread::EnumMemoryRegionsWorker(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    if (IsUnstarted())
    {
        return;
    }

    T_CONTEXT context;
    BOOL DacGetThreadContext(Thread* thread, T_CONTEXT* context);
    REGDISPLAY regDisp;
    StackFrameIterator frameIter;

    TADDR previousSP = 0; //start at zero; this allows first check to always succeed.
    TADDR currentSP;

    // Init value.  The Limit itself is not legal, so move one target pointer size to the smallest-magnitude
    // legal address.
    currentSP = dac_cast<TADDR>(m_CacheStackLimit) + sizeof(TADDR);

    if (GetFilterContext())
    {
        context = *GetFilterContext();
    }
    else
    {
        DacGetThreadContext(this, &context);
    }

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        AppDomain::GetCurrentDomain()->EnumMemoryRegions(flags, true);
    }

    FillRegDisplay(&regDisp, &context);
    frameIter.Init(this, NULL, &regDisp, 0);
    while (frameIter.IsValid())
    {
        //
        // There are identical stack pointer checking semantics in code:ClrDataAccess::EnumMemWalkStackHelper
        // You ***MUST*** maintain identical semantics for both checks!
        //

        // Before we continue, we should check to be sure we have a valid
        // stack pointer.  This is to prevent stacks that are not walked
        // properly due to
        //   a) stack corruption bugs
        //   b) bad stack walks
        // from continuing on indefinitely.
        //
        // We will force SP to strictly increase.
        //   this check can only happen for real stack frames (i.e. not for explicit frames that don't update the RegDisplay)
        //   for ia64, SP may be equal, but in this case BSP must strictly decrease.
        // We will force SP to be properly aligned.
        // We will force SP to be in the correct range.
        //
        if (frameIter.GetFrameState() == StackFrameIterator::SFITER_FRAMELESS_METHOD)
        {
            // This check cannot be applied to explicit frames; they may not move the SP at all.
            // Also, a single function can push several on the stack at a time with no guarantees about
            // ordering so we can't check that the addresses of the explicit frames are monotonically increasing.
            // There is the potential that the walk will not terminate if a set of explicit frames reference
            // each other circularly.  While we could choose a limit for the number of explicit frames allowed
            // in a row like the total stack size/pointer size, we have no known problems with this scenario.
            // Thus for now we ignore it.
            currentSP = (TADDR)GetRegdisplaySP(&regDisp);

            if (currentSP <= previousSP)
            {
                _ASSERTE(!"Target stack has been corrupted, SP for current frame must be larger than previous frame.");
                break;
            }
        }

        // On windows desktop, the stack pointer should be a multiple
        // of pointer-size-aligned in the target address space
        if (currentSP % sizeof(TADDR) != 0)
        {
            _ASSERTE(!"Target stack has been corrupted, SP must be aligned.");
            break;
        }

        if (!IsAddressInStack(currentSP))
        {
            _ASSERTE(!"Target stack has been corrupted, SP must in in the stack range.");
            break;
        }

        // Enumerate the code around the call site to help debugger stack walking heuristics
        PCODE callEnd = GetControlPC(&regDisp);
        DacEnumCodeForStackwalk(callEnd);

        // To stackwalk through funceval frames, we need to be sure to preserve the
        // DebuggerModule's m_pRuntimeDomainFile.  This is the only case that doesn't use the current
        // vmDomainFile in code:DacDbiInterfaceImpl::EnumerateInternalFrames.  The following
        // code mimics that function.
        // Allow failure, since we want to continue attempting to walk the stack regardless of the outcome.
        EX_TRY
        {
            if ((frameIter.GetFrameState() == StackFrameIterator::SFITER_FRAME_FUNCTION) ||
                (frameIter.GetFrameState() == StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION))
            {
                Frame * pFrame = frameIter.m_crawl.GetFrame();
                g_pDebugInterface->EnumMemoryRegionsIfFuncEvalFrame(flags, pFrame);
            }
        }
        EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

        MethodDesc* pMD = frameIter.m_crawl.GetFunction();
        if (pMD != NULL)
        {
            pMD->EnumMemoryRegions(flags);
#if defined(FEATURE_EH_FUNCLETS) && defined(FEATURE_PREJIT)
            // Enumerate unwind info
            // Note that we don't do this based on the MethodDesc because in theory there isn't a 1:1 correspondence
            // between MethodDesc and code (and so unwind info, and even debug info).  Eg., EnC creates new versions
            // of the code, but the MethodDesc always points at the latest version (which isn't necessarily
            // the one on the stack).  In practice this is unlikely to be a problem since wanting a minidump
            // and making EnC edits are usually mutually exclusive.
            if (frameIter.m_crawl.IsFrameless())
            {
                frameIter.m_crawl.GetJitManager()->EnumMemoryRegionsForMethodUnwindInfo(flags, frameIter.m_crawl.GetCodeInfo());
            }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(FEATURE_PREJIT)
        }

        previousSP = currentSP;

        if (frameIter.Next() != SWA_CONTINUE)
        {
            break;
        }
    }
}

void
ThreadStore::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;

    // This will write out the context of the s_pThreadStore. ie
    // just the pointer
    //
    s_pThreadStore.EnumMem();
    if (s_pThreadStore.IsValid())
    {
        // write out the whole ThreadStore structure
        DacEnumHostDPtrMem(s_pThreadStore);

        // The thread list may be corrupt, so just
        // ignore exceptions during enumeration.
        EX_TRY
        {
            Thread* thread       = s_pThreadStore->m_ThreadList.GetHead();
            LONG    dwNumThreads = s_pThreadStore->m_ThreadCount;

            for (LONG i = 0; (i < dwNumThreads) && (thread != NULL); i++)
            {
                // Even if this thread is totally broken and we can't enum it, struggle on.
                // If we do not, we will leave this loop and not enum stack memory for any further threads.
                CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED(
                    thread->EnumMemoryRegions(flags);
                );
                thread = s_pThreadStore->m_ThreadList.GetNext(thread);
            }
        }
        EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED
    }
}

#endif // #ifdef DACCESS_COMPILE
