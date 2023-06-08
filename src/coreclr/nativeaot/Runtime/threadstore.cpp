// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "gcrhinterface.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "rhbinder.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "RuntimeInstance.h"
#include "TargetPtrs.h"
#include "yieldprocessornormalized.h"

#include "slist.inl"
#include "GCMemoryHelpers.h"

EXTERN_C volatile uint32_t RhpTrapThreads;
volatile uint32_t RhpTrapThreads = (uint32_t)TrapThreadsFlags::None;

GVAL_IMPL_INIT(PTR_Thread, RhpSuspendingThread, 0);

ThreadStore * GetThreadStore()
{
    return GetRuntimeInstance()->GetThreadStore();
}

ThreadStore::Iterator::Iterator() :
    m_pCurrentPosition(GetThreadStore()->m_ThreadList.GetHead())
{
    // GC threads may access threadstore without locking as
    // the lock taken during suspension effectively held by the entire GC.
    // Others must take a lock.
    ASSERT(GetThreadStore()->m_Lock.OwnedByCurrentThread() ||
        (ThreadStore::GetCurrentThread()->IsGCSpecial() && GCHeapUtilities::IsGCInProgress()));
}

ThreadStore::Iterator::~Iterator()
{
}

PTR_Thread ThreadStore::Iterator::GetNext()
{
    PTR_Thread pResult = m_pCurrentPosition;
    if (NULL != pResult)
        m_pCurrentPosition = pResult->m_pNext;
    return pResult;
}

//static
PTR_Thread ThreadStore::GetSuspendingThread()
{
    return (RhpSuspendingThread);
}

#ifndef DACCESS_COMPILE


ThreadStore::ThreadStore() :
    m_ThreadList(),
    m_Lock(CrstThreadStore)
{
    SaveCurrentThreadOffsetForDAC();
}

ThreadStore::~ThreadStore()
{
    m_Lock.Destroy();
}

// static
ThreadStore * ThreadStore::Create(RuntimeInstance * pRuntimeInstance)
{
    NewHolder<ThreadStore> pNewThreadStore = new (nothrow) ThreadStore();
    if (NULL == pNewThreadStore)
        return NULL;

    if (!PalRegisterHijackCallback(Thread::HijackCallback))
        return NULL;

    pNewThreadStore->m_pRuntimeInstance = pRuntimeInstance;

    pNewThreadStore.SuppressRelease();
    return pNewThreadStore;
}

void ThreadStore::Destroy()
{
    delete this;
}

// static
void ThreadStore::AttachCurrentThread(bool fAcquireThreadStoreLock)
{
    //
    // step 1: ThreadStore::InitCurrentThread
    // step 2: add this thread to the ThreadStore
    //

    // The thread has been constructed, during which some data is initialized (like which RuntimeInstance the
    // thread belongs to), but it hasn't been added to the thread store because doing so takes a lock, which
    // we want to avoid at construction time because the loader lock is held then.
    Thread * pAttachingThread = RawGetCurrentThread();

    // The thread was already initialized, so it is already attached
    if (pAttachingThread->IsInitialized())
    {
        return;
    }

    PalAttachThread(pAttachingThread);

    //
    // Init the thread buffer
    //
    pAttachingThread->Construct();
    ASSERT(pAttachingThread->m_ThreadStateFlags == Thread::TSF_Unknown);

    // fAcquireThreadStoreLock is false when threads are created/attached for GC purpose
    // in such case the lock is already held and GC takes care to ensure safe access to the threadstore

    ThreadStore* pTS = GetThreadStore();
    CrstHolderWithState threadStoreLock(&pTS->m_Lock, fAcquireThreadStoreLock);

    //
    // Set thread state to be attached
    //
    ASSERT(pAttachingThread->m_ThreadStateFlags == Thread::TSF_Unknown);
    pAttachingThread->m_ThreadStateFlags = Thread::TSF_Attached;

    pTS->m_ThreadList.PushHead(pAttachingThread);
}

// static
void ThreadStore::AttachCurrentThread()
{
    AttachCurrentThread(true);
}

void ThreadStore::DetachCurrentThread()
{
    // The thread may not have been initialized because it may never have run managed code before.
    Thread * pDetachingThread = RawGetCurrentThread();

    // The thread was not initialized yet, so it was not attached
    if (!pDetachingThread->IsInitialized())
    {
        return;
    }

    // Unregister from OS notifications
    // This can return false if detach notification is spurious and does not belong to this thread.
    if (!PalDetachThread(pDetachingThread))
    {
        return;
    }

    // Run pre-mortem callbacks while we still can run managed code and not holding locks.
    // NOTE: background GC threads are attached/suspendable threads, but should not run ordinary
    // managed code. Make sure that does not happen here.
    if (g_threadExitCallback != NULL && !pDetachingThread->IsGCSpecial())
    {
        g_threadExitCallback();
    }

    // we will be taking the threadstore lock and need to be in preemptive mode.
    ASSERT(!pDetachingThread->IsCurrentThreadInCooperativeMode());

    // The following makes the thread no longer able to run managed code or participate in GC.
    // We need to hold threadstore lock while doing that.
    {
        ThreadStore* pTS = GetThreadStore();
        // Note that when process is shutting down, the threads may be rudely terminated,
        // possibly while holding the threadstore lock. That is ok, since the process is being torn down.
        CrstHolder threadStoreLock(&pTS->m_Lock);
        ASSERT(rh::std::count(pTS->m_ThreadList.Begin(), pTS->m_ThreadList.End(), pDetachingThread) == 1);
        // remove the thread from the list of managed threads.
        pTS->m_ThreadList.RemoveFirst(pDetachingThread);
        // tidy up GC related stuff (release allocation context, etc..)
        pDetachingThread->Detach();
    }

    // post-mortem clean up.
    pDetachingThread->Destroy();
}

// Used by GC to prevent new threads during a GC and
// to ensure that only one thread performs suspension.
void ThreadStore::LockThreadStore()
{
    // the thread should not be in coop mode when taking the threadstore lock.
    // this is required to avoid deadlocks if suspension is in progress.
    bool wasCooperative = false;
    Thread* pThisThread = GetCurrentThreadIfAvailable();
    if (pThisThread && pThisThread->IsCurrentThreadInCooperativeMode())
    {
        wasCooperative = true;
        pThisThread->EnablePreemptiveMode();
    }

    m_Lock.Enter();

    if (wasCooperative)
    {
        // we just got the lock thus EE can't be suspending, so no waiting here
        pThisThread->DisablePreemptiveMode();
    }
}

void ThreadStore::UnlockThreadStore()
{
    m_Lock.Leave();
}

// exponential spinwait with an approximate time limit for waiting in microsecond range.
// when iteration == -1, only usecLimit is used
void SpinWait(int iteration, int usecLimit)
{
    int64_t startTicks = PalQueryPerformanceCounter();
    int64_t ticksPerSecond = PalQueryPerformanceFrequency();
    int64_t endTicks = startTicks + (usecLimit * ticksPerSecond) / 1000000;

    int l = min((unsigned)iteration, 30);
    for (int i = 0; i < l; i++)
    {
        for (int j = 0; j < (1 << i); j++)
        {
            System_YieldProcessor();
        }

        int64_t currentTicks = PalQueryPerformanceCounter();
        if (currentTicks > endTicks)
        {
            break;
        }
    }
}

void ThreadStore::SuspendAllThreads(bool waitForGCEvent)
{
    Thread * pThisThread = GetCurrentThreadIfAvailable();
    RhpSuspendingThread = pThisThread;

    if (waitForGCEvent)
    {
        GCHeapUtilities::GetGCHeap()->ResetWaitForGCEvent();
    }

    // set the global trap for pinvoke leave and return
    RhpTrapThreads |= (uint32_t)TrapThreadsFlags::TrapThreads;

    // Our lock-free algorithm depends on flushing write buffers of all processors running RH code.  The
    // reason for this is that we essentially implement Dekker's algorithm, which requires write ordering.
    PalFlushProcessWriteBuffers();

    int retries = 0;
    int prevRemaining = 0;
    int remaining = 0;
    bool observeOnly = false;

    while(true)
    {
        prevRemaining = remaining;
        remaining = 0;

        FOREACH_THREAD(pTargetThread)
        {
            if (pTargetThread == pThisThread)
                continue;

            if (!pTargetThread->CacheTransitionFrameForSuspend())
            {
                remaining++;
                if (!observeOnly)
                {
                    pTargetThread->Hijack();
                }
            }
        }
        END_FOREACH_THREAD

        if (!remaining)
            break;

        // if we see progress or have just done a hijacking pass
        // do not hijack in the next iteration
        if (remaining < prevRemaining || !observeOnly)
        {
            // 5 usec delay, then check for more progress
            SpinWait(-1, 5);
            observeOnly = true;
        }
        else
        {
            SpinWait(retries++, 100);
            observeOnly = false;

            // make sure our spining is not starving other threads, but not too often,
            // this can cause a 1-15 msec delay, depending on OS, and that is a lot while
            // very rarely needed, since threads are supposed to be releasing their CPUs
            if ((retries & 127) == 0)
            {
                PalSwitchToThread();
            }
        }
    }

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // Flush the store buffers on all CPUs, to ensure that all changes made so far are seen
    // by the GC threads. This only matters on weak memory ordered processors as
    // the strong memory ordered processors wouldn't have reordered the relevant writes.
    // This is needed to synchronize threads that were running in preemptive mode thus were
    // left alone by suspension to flush their writes that they made before they switched to
    // preemptive mode.
    PalFlushProcessWriteBuffers();
#endif //TARGET_ARM || TARGET_ARM64
}

void ThreadStore::ResumeAllThreads(bool waitForGCEvent)
{
    FOREACH_THREAD(pTargetThread)
    {
        pTargetThread->ResetCachedTransitionFrame();
    }
    END_FOREACH_THREAD

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
        // Flush the store buffers on all CPUs, to ensure that they all see changes made
        // by the GC threads. This only matters on weak memory ordered processors as
        // the strong memory ordered processors wouldn't have reordered the relevant reads.
        // This is needed to synchronize threads that were running in preemptive mode while
        // the runtime was suspended and that will return to cooperative mode after the runtime
        // is restarted.
        PalFlushProcessWriteBuffers();
#endif //TARGET_ARM || TARGET_ARM64

    RhpTrapThreads &= ~(uint32_t)TrapThreadsFlags::TrapThreads;

    RhpSuspendingThread = NULL;
    if (waitForGCEvent)
    {
        GCHeapUtilities::GetGCHeap()->SetWaitForGCEvent();
    }
} // ResumeAllThreads

void ThreadStore::InitiateThreadAbort(Thread* targetThread, Object * threadAbortException, bool doRudeAbort)
{
    SuspendAllThreads(/* waitForGCEvent = */ false);
    // TODO: consider enabling multiple thread aborts running in parallel on different threads
    ASSERT((RhpTrapThreads & (uint32_t)TrapThreadsFlags::AbortInProgress) == 0);
    RhpTrapThreads |= (uint32_t)TrapThreadsFlags::AbortInProgress;

    targetThread->SetThreadAbortException(threadAbortException);

    // TODO: Stage 2: Queue APC to the target thread to break out of possible wait

    bool initiateAbort = false;

    if (!doRudeAbort)
    {
        // TODO: Stage 3: protected regions (finally, catch) handling
        //  If it was in a protected region, set the "throw at protected region end" flag on the native Thread object
        // TODO: Stage 4: reverse PInvoke handling
        //  If there was a reverse Pinvoke frame between the current frame and the funceval frame of the target thread,
        //  find the outermost reverse Pinvoke frame below the funceval frame and set the thread abort flag in its transition frame.
        //  If both of these cases happened at once, find out which one of the outermost frame of the protected region
        //  and the outermost reverse Pinvoke frame is closer to the funceval frame and perform one of the two actions
        //  described above based on the one that's closer.
        initiateAbort = true;
    }
    else
    {
        initiateAbort = true;
    }

    if (initiateAbort)
    {
        PInvokeTransitionFrame* transitionFrame = reinterpret_cast<PInvokeTransitionFrame*>(targetThread->GetTransitionFrame());
        transitionFrame->m_Flags |= PTFF_THREAD_ABORT;
    }

    ResumeAllThreads(/* waitForGCEvent = */ false);
}

void ThreadStore::CancelThreadAbort(Thread* targetThread)
{
    SuspendAllThreads(/* waitForGCEvent = */ false);

    ASSERT((RhpTrapThreads & (uint32_t)TrapThreadsFlags::AbortInProgress) != 0);
    RhpTrapThreads &= ~(uint32_t)TrapThreadsFlags::AbortInProgress;

    PInvokeTransitionFrame* transitionFrame = reinterpret_cast<PInvokeTransitionFrame*>(targetThread->GetTransitionFrame());
    if (transitionFrame != nullptr)
    {
        transitionFrame->m_Flags &= ~PTFF_THREAD_ABORT;
    }

    targetThread->SetThreadAbortException(nullptr);

    ResumeAllThreads(/* waitForGCEvent = */ false);
}

COOP_PINVOKE_HELPER(void *, RhpGetCurrentThread, ())
{
    return ThreadStore::GetCurrentThread();
}

COOP_PINVOKE_HELPER(void, RhpInitiateThreadAbort, (void* thread, Object * threadAbortException, CLR_BOOL doRudeAbort))
{
    GetThreadStore()->InitiateThreadAbort((Thread*)thread, threadAbortException, doRudeAbort);
}

COOP_PINVOKE_HELPER(void, RhpCancelThreadAbort, (void* thread))
{
    GetThreadStore()->CancelThreadAbort((Thread*)thread);
}

C_ASSERT(sizeof(Thread) == sizeof(ThreadBuffer));

#ifndef _MSC_VER
__thread ThreadBuffer tls_CurrentThread;

// the root of inlined threadstatics storage
// there is only one now,
// eventually this will be emitted by ILC and we may have more than one such variable
__thread InlinedThreadStaticRoot tls_InlinedThreadStatics;
#endif

EXTERN_C ThreadBuffer* RhpGetThread()
{
    return &tls_CurrentThread;
}

COOP_PINVOKE_HELPER(Object**, RhGetInlinedThreadStaticStorage, ())
{
    return &tls_InlinedThreadStatics.m_threadStaticsBase;
}

#endif // !DACCESS_COMPILE

#ifdef _WIN32

#ifndef DACCESS_COMPILE

// Keep a global variable in the target process which contains
// the address of _tls_index.  This is the breadcrumb needed
// by DAC to read _tls_index since we don't control the
// declaration of _tls_index directly.

// volatile to prevent the compiler from removing the unused global variable
volatile uint32_t * p_tls_index;
volatile uint32_t SECTIONREL__tls_CurrentThread;

EXTERN_C uint32_t _tls_index;
#if defined(TARGET_ARM64)
// ARM64TODO: Re-enable optimization
#pragma optimize("", off)
#endif
void ThreadStore::SaveCurrentThreadOffsetForDAC()
{
    p_tls_index = &_tls_index;

    uint8_t * pTls = *(uint8_t **)(PalNtCurrentTeb() + OFFSETOF__TEB__ThreadLocalStoragePointer);

    uint8_t * pOurTls = *(uint8_t **)(pTls + (_tls_index * sizeof(void*)));

    SECTIONREL__tls_CurrentThread = (uint32_t)((uint8_t *)&tls_CurrentThread - pOurTls);
}
#if defined(TARGET_ARM64)
#pragma optimize("", on)
#endif
#else // DACCESS_COMPILE

GPTR_IMPL(uint32_t, p_tls_index);
GVAL_IMPL(uint32_t, SECTIONREL__tls_CurrentThread);

//
// This routine supports the !Thread debugger extension routine
//
typedef DPTR(TADDR) PTR_TADDR;
// static
PTR_Thread ThreadStore::GetThreadFromTEB(TADDR pTEB)
{
    if (pTEB == NULL)
        return NULL;

    uint32_t tlsIndex = *p_tls_index;
    TADDR pTls = *(PTR_TADDR)(pTEB + OFFSETOF__TEB__ThreadLocalStoragePointer);
    if (pTls == NULL)
        return NULL;

    TADDR pOurTls = *(PTR_TADDR)(pTls + (tlsIndex * sizeof(void*)));
    if (pOurTls == NULL)
        return NULL;

    return (PTR_Thread)(pOurTls + SECTIONREL__tls_CurrentThread);
}

#endif // DACCESS_COMPILE

#else // _WIN32

void ThreadStore::SaveCurrentThreadOffsetForDAC()
{
}

#endif // _WIN32
