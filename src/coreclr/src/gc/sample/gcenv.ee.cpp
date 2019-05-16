// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

#include "windows.h"

#include "gcenv.h"
#include "gc.h"

MethodTable * g_pFreeObjectMethodTable;

EEConfig * g_pConfig;

bool CLREventStatic::CreateManualEventNoThrow(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, TRUE, bInitialState, NULL);
    m_fInitialized = true;

    return IsValid();
}

bool CLREventStatic::CreateAutoEventNoThrow(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, FALSE, bInitialState, NULL);
    m_fInitialized = true;

    return IsValid();
}

bool CLREventStatic::CreateOSManualEventNoThrow(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, TRUE, bInitialState, NULL);
    m_fInitialized = true;

    return IsValid();
}

bool CLREventStatic::CreateOSAutoEventNoThrow(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, FALSE, bInitialState, NULL);
    m_fInitialized = true;

    return IsValid();
}

void CLREventStatic::CloseEvent()
{
    if (m_fInitialized && m_hEvent != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hEvent);
        m_hEvent = INVALID_HANDLE_VALUE;
    }
}

bool CLREventStatic::IsValid() const
{
    return m_fInitialized && m_hEvent != INVALID_HANDLE_VALUE;
}

bool CLREventStatic::Set()
{
    if (!m_fInitialized)
        return false;
    return !!SetEvent(m_hEvent);
}

bool CLREventStatic::Reset()
{
    if (!m_fInitialized)
        return false;
    return !!ResetEvent(m_hEvent);
}

uint32_t CLREventStatic::Wait(uint32_t dwMilliseconds, bool bAlertable)
{
    DWORD result = WAIT_FAILED;

    if (m_fInitialized)
    {
        bool        disablePreemptive = false;
        Thread *    pCurThread = GetThread();

        if (NULL != pCurThread)
        {
            disablePreemptive = GCToEEInterface::EnablePreemptiveGC();
        }

        result = WaitForSingleObjectEx(m_hEvent, dwMilliseconds, bAlertable);

        if (disablePreemptive)
        {
            GCToEEInterface::DisablePreemptiveGC();
        }
    }

    return result;
}

#ifndef __GNUC__
__declspec(thread) Thread * pCurrentThread;
#else // !__GNUC__
thread_local Thread * pCurrentThread;
#endif // !__GNUC__

Thread * GetThread()
{
    return pCurrentThread;
}

Thread * g_pThreadList = NULL;

Thread * ThreadStore::GetThreadList(Thread * pThread)
{
    if (pThread == NULL)
        return g_pThreadList;

    return pThread->m_pNext;
}

void ThreadStore::AttachCurrentThread()
{
    // TODO: Locks

    Thread * pThread = new Thread();
    pThread->GetAllocContext()->init();
    pCurrentThread = pThread;

    pThread->m_pNext = g_pThreadList;
    g_pThreadList = pThread;
}

void GCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
    g_theGCHeap->SetGCInProgress(true);

    // TODO: Implement
}

void GCToEEInterface::RestartEE(bool bFinishedGC)
{
    // TODO: Implement

    g_theGCHeap->SetGCInProgress(false);
}

void GCToEEInterface::GcScanRoots(promote_func* fn,  int condemned, int max_gen, ScanContext* sc)
{
    // TODO: Implement - Scan stack roots on given thread
}

void GCToEEInterface::GcStartWork(int condemned, int max_gen)
{
}

void GCToEEInterface::AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
{
}

void GCToEEInterface::GcBeforeBGCSweepWork()
{
}

void GCToEEInterface::GcDone(int condemned)
{
}

bool GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
    return false;
}

bool GCToEEInterface::IsPreemptiveGCDisabled()
{
    Thread* pThread = ::GetThread();
    return pThread->PreemptiveGCDisabled();
}

bool GCToEEInterface::EnablePreemptiveGC()
{
    bool bToggleGC = false;
    Thread* pThread = ::GetThread();

    if (pThread)
    {
        bToggleGC = !!pThread->PreemptiveGCDisabled();
        if (bToggleGC)
        {
            pThread->EnablePreemptiveGC();
        }
    }

    return bToggleGC;
}

void GCToEEInterface::DisablePreemptiveGC()
{
    Thread* pThread = ::GetThread();
    pThread->DisablePreemptiveGC();
}

Thread* GCToEEInterface::GetThread()
{
    return ::GetThread();
}

gc_alloc_context * GCToEEInterface::GetAllocContext()
{
    Thread* pThread = ::GetThread();
    return pThread->GetAllocContext();
}

void GCToEEInterface::GcEnumAllocContexts (enum_alloc_context_func* fn, void* param)
{
    Thread * pThread = NULL;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        fn(pThread->GetAllocContext(), param);
    }
}

uint8_t* GCToEEInterface::GetLoaderAllocatorObjectForGC(Object* pObject)
{
    return NULL;
}

void GCToEEInterface::SyncBlockCacheWeakPtrScan(HANDLESCANPROC /*scanProc*/, uintptr_t /*lp1*/, uintptr_t /*lp2*/)
{
}

void GCToEEInterface::SyncBlockCacheDemote(int /*max_gen*/)
{
}

void GCToEEInterface::SyncBlockCachePromotionsGranted(int /*max_gen*/)
{
}

void GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
}

void GCToEEInterface::DiagUpdateGenerationBounds()
{
}

void GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
}

void GCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
}

void GCToEEInterface::DiagWalkSurvivors(void* gcContext, bool fCompacting)
{
}

void GCToEEInterface::DiagWalkLOHSurvivors(void* gcContext)
{
}

void GCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
}

void GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
}

void GCToEEInterface::EnableFinalization(bool foundFinalizers)
{
    // Signal to finalizer thread that there are objects to finalize
    // TODO: Implement for finalization
}

void GCToEEInterface::HandleFatalError(unsigned int exitCode)
{
    abort();
}

bool GCToEEInterface::EagerFinalized(Object* obj)
{
    // The sample does not finalize anything eagerly.
    return false;
}

bool GCToEEInterface::GetBooleanConfigValue(const char* key, bool* value)
{
    return false;
}

bool GCToEEInterface::GetIntConfigValue(const char* key, int64_t* value)
{
    return false;
}

bool GCToEEInterface::GetStringConfigValue(const char* key, const char** value)
{
    return false;
}

void GCToEEInterface::FreeStringConfigValue(const char *value)
{

}

bool GCToEEInterface::IsGCThread()
{
    return false;
}

bool GCToEEInterface::WasCurrentThreadCreatedByGC()
{
    return false;
}

static MethodTable freeObjectMT;

MethodTable* GCToEEInterface::GetFreeObjectMethodTable()
{
    // 
    // Initialize free object methodtable. The GC uses a special array-like methodtable as placeholder
    // for collected free space.
    //
    freeObjectMT.InitializeFreeObject();
    return &freeObjectMT;
}

bool GCToEEInterface::CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
{
    return false;
}

void GCToEEInterface::WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback)
{
}

void GCToEEInterface::WalkAsyncPinned(Object* object, void* context, void (*callback)(Object*, Object*, void*))
{
}

uint32_t GCToEEInterface::GetTotalNumSizedRefHandles()
{
    return -1;
}

inline bool GCToEEInterface::AnalyzeSurvivorsRequested(int condemnedGeneration)
{
    return false;
}

inline void GCToEEInterface::AnalyzeSurvivorsFinished(int condemnedGeneration)
{
    
}
