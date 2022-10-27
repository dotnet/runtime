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
#include "RWLock.h"
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
    m_readHolder(&GetThreadStore()->m_Lock),
    m_pCurrentPosition(GetThreadStore()->m_ThreadList.GetHead())
{
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
    m_Lock(true /* writers (i.e. attaching/detaching threads) should wait on GC event */)
{
    SaveCurrentThreadOffsetForDAC();
}

ThreadStore::~ThreadStore()
{
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

    // The runtime holds the thread store lock for the duration of thread suspension for GC, so let's check to
    // see if that's going on and, if so, use a proper wait instead of the RWL's spinning.  NOTE: when we are
    // called with fAcquireThreadStoreLock==false, we are being called in a situation where the GC is trying to
    // init a GC thread, so we must honor the flag to mean "do not block on GC" or else we will deadlock.
    if (fAcquireThreadStoreLock && (RhpTrapThreads != (uint32_t)TrapThreadsFlags::None))
        RedhawkGCInterface::WaitForGCCompletion();

    ThreadStore* pTS = GetThreadStore();
    ReaderWriterLock::WriteHolder write(&pTS->m_Lock, fAcquireThreadStoreLock);

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

void ThreadStore::DetachCurrentThread(bool shutdownStarted)
{
    // The thread may not have been initialized because it may never have run managed code before.
    Thread * pDetachingThread = RawGetCurrentThread();

    // The thread was not initialized yet, so it was not attached
    if (!pDetachingThread->IsInitialized())
    {
        return;
    }

    if (!PalDetachThread(pDetachingThread))
    {
        return;
    }

    {
        // remove the thread from the list.
        // this must be done even when shutdown is in progress.
        // departing threads will be releasing their TLS storage and if not removed,
        // will corrupt the thread list, while some prior to shutdown detach might still be in progress.
        ThreadStore* pTS = GetThreadStore();
        ReaderWriterLock::WriteHolder write(&pTS->m_Lock);
        ASSERT(rh::std::count(pTS->m_ThreadList.Begin(), pTS->m_ThreadList.End(), pDetachingThread) == 1);
        pTS->m_ThreadList.RemoveFirst(pDetachingThread);
        pDetachingThread->Detach();
    }

    // the rest of cleanup is not necessary if we are shutting down.
    if (shutdownStarted)
    {
        return;
    }

    pDetachingThread->Destroy();
}

// Used by GC to prevent new threads during a GC.  New threads must take a write lock to
// modify the list, but they won't be allowed to until all outstanding read locks are
// released.  This way, the GC always enumerates a consistent set of threads each time
// it enumerates threads between SuspendAllThreads and ResumeAllThreads.
//
// @TODO: Investigate if this requirement is actually necessary.  Threads already may
// not enter managed code during GC, so if new threads are added to the thread store,
// but haven't yet entered managed code, is that really a problem?
//
// @TODO: Investigate the suspend/resume algorithm's dependence on this lock's side-
// effect of being a memory barrier.
void ThreadStore::LockThreadStore()
{
    m_Lock.AcquireReadLock();
}

void ThreadStore::UnlockThreadStore()
{
    m_Lock.ReleaseReadLock();
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

    LockThreadStore();

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
    UnlockThreadStore();
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

EXTERN_C DECLSPEC_THREAD ThreadBuffer tls_CurrentThread;
DECLSPEC_THREAD ThreadBuffer tls_CurrentThread =
{
    { 0 },                              // m_rgbAllocContextBuffer
    Thread::TSF_Unknown,                // m_ThreadStateFlags
    TOP_OF_STACK_MARKER,                // m_pTransitionFrame
    TOP_OF_STACK_MARKER,                // m_pDeferredTransitionFrame
    0,                                  // m_pCachedTransitionFrame
    0,                                  // m_pNext
    INVALID_HANDLE_VALUE,               // m_hPalThread
    0,                                  // m_ppvHijackedReturnAddressLocation
    0,                                  // m_pvHijackedReturnAddress
    0,                                  // all other fields are initialized by zeroes
};

EXTERN_C ThreadBuffer* RhpGetThread()
{
    return &tls_CurrentThread;
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


#ifndef DACCESS_COMPILE

// internal static extern unsafe bool RhGetExceptionsForCurrentThread(Exception[] outputArray, out int writtenCountOut);
COOP_PINVOKE_HELPER(FC_BOOL_RET, RhGetExceptionsForCurrentThread, (Array* pOutputArray, int32_t* pWrittenCountOut))
{
    FC_RETURN_BOOL(GetThreadStore()->GetExceptionsForCurrentThread(pOutputArray, pWrittenCountOut));
}

bool ThreadStore::GetExceptionsForCurrentThread(Array* pOutputArray, int32_t* pWrittenCountOut)
{
    int32_t countWritten = 0;
    Object** pArrayElements;
    Thread * pThread = GetCurrentThread();

    for (PTR_ExInfo pInfo = pThread->m_pExInfoStackHead; pInfo != NULL; pInfo = pInfo->m_pPrevExInfo)
    {
        if (pInfo->m_exception == NULL)
            continue;

        countWritten++;
    }

    // No input array provided, or it was of the wrong kind.  We'll fill out the count and return false.
    if ((pOutputArray == NULL) || (pOutputArray->get_EEType()->RawGetComponentSize() != POINTER_SIZE))
        goto Error;

    // Input array was not big enough.  We don't even partially fill it.
    if (pOutputArray->GetArrayLength() < (uint32_t)countWritten)
        goto Error;

    *pWrittenCountOut = countWritten;

    // Success, but nothing to report.
    if (countWritten == 0)
        return true;

    pArrayElements = (Object**)pOutputArray->GetArrayData();
    for (PTR_ExInfo pInfo = pThread->m_pExInfoStackHead; pInfo != NULL; pInfo = pInfo->m_pPrevExInfo)
    {
        if (pInfo->m_exception == NULL)
            continue;

        *pArrayElements = pInfo->m_exception;
        pArrayElements++;
    }

    RhpBulkWriteBarrier(pArrayElements, countWritten * POINTER_SIZE);
    return true;

Error:
    *pWrittenCountOut = countWritten;
    return false;
}
#endif // DACCESS_COMPILE
