// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "RestrictedCallouts.h"

#include "RhConfig.h"
#include "GcEnum.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "SyncClean.hpp"

#include "interoplibinterface.h"

#include "gctoclreventsink.h"

#include "volatile.h"

#include "daccess.h"

GPTR_IMPL(MethodTable, g_pFreeObjectEEType);

#ifndef DACCESS_COMPILE

void RhEnableFinalization();

void GCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
#ifdef FEATURE_EVENT_TRACE
    ETW::GCLog::ETW_GC_INFO Info;
    Info.SuspendEE.Reason = reason;
    Info.SuspendEE.GcCount = (((reason == SUSPEND_FOR_GC) || (reason == SUSPEND_FOR_GC_PREP)) ?
        (uint32_t)GCHeapUtilities::GetGCHeap()->GetGcCount() : (uint32_t)-1);
#endif // FEATURE_EVENT_TRACE

    FireEtwGCSuspendEEBegin_V1(Info.SuspendEE.Reason, Info.SuspendEE.GcCount, GetClrInstanceId());

    GetThreadStore()->LockThreadStore();
    GCHeapUtilities::GetGCHeap()->SetGCInProgress(TRUE);
    GetThreadStore()->SuspendAllThreads(true);

    FireEtwGCSuspendEEEnd_V1(GetClrInstanceId());
}

void GCToEEInterface::RestartEE(bool /*bFinishedGC*/)
{
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

    SyncClean::CleanUp();

    GetThreadStore()->ResumeAllThreads(true);
    GCHeapUtilities::GetGCHeap()->SetGCInProgress(FALSE);
    GetThreadStore()->UnlockThreadStore();

    FireEtwGCRestartEEEnd_V1(GetClrInstanceId());
}

void GCToEEInterface::GcStartWork(int condemned, int /*max_gen*/)
{
    // Invoke any registered callouts for the start of the collection.
    RestrictedCallouts::InvokeGcCallouts(GCRC_StartCollection, condemned);
}

void GCToEEInterface::BeforeGcScanRoots(int condemned, bool is_bgc, bool is_concurrent)
{
#ifdef FEATURE_OBJCMARSHAL
    if (!is_concurrent)
    {
        ObjCMarshalNative::BeforeRefCountedHandleCallbacks();
    }
#endif
}

void GCToEEInterface::GcScanRoots(ScanFunc* fn, int condemned, int max_gen, ScanContext* sc)
{
    // STRESS_LOG1(LF_GCROOTS, LL_INFO10, "GCScan: Phase = %s\n", sc->promotion ? "promote" : "relocate");

    FOREACH_THREAD(pThread)
    {
        // Skip "GC Special" threads which are really background workers that will never have any roots.
        if (pThread->IsGCSpecial())
            continue;

        if (GCHeapUtilities::GetGCHeap()->IsThreadUsingAllocationContextHeap(pThread->GetAllocContext(), sc->thread_number))
        {
            InlinedThreadStaticRoot* pRoot = pThread->GetInlinedThreadStaticList();
            while (pRoot != NULL)
            {
                STRESS_LOG2(LF_GC | LF_GCROOTS, LL_INFO100, "{ Scanning Thread's %p inline thread statics root %p. \n", pThread, pRoot);
                EnumGcRef(&pRoot->m_threadStaticsBase, GCRK_Object, fn, sc);
                pRoot = pRoot->m_next;
            }

            STRESS_LOG1(LF_GC | LF_GCROOTS, LL_INFO100, "{ Scanning Thread's %p thread statics root. \n", pThread);
            EnumGcRef(pThread->GetThreadStaticStorage(), GCRK_Object, fn, sc);

            STRESS_LOG1(LF_GC | LF_GCROOTS, LL_INFO100, "{ Starting scan of Thread %p\n", pThread);
            sc->thread_under_crawl = pThread;
#if defined(FEATURE_EVENT_TRACE) && !defined(DACCESS_COMPILE)
            sc->dwEtwRootKind = kEtwGCRootKindStack;
#endif
            pThread->GcScanRoots(fn, sc);

#if defined(FEATURE_EVENT_TRACE) && !defined(DACCESS_COMPILE)
            sc->dwEtwRootKind = kEtwGCRootKindOther;
#endif
            STRESS_LOG1(LF_GC | LF_GCROOTS, LL_INFO100, "Ending scan of Thread %p }\n", pThread);
        }
    }
    END_FOREACH_THREAD

    sc->thread_under_crawl = NULL;
}

void GCToEEInterface::GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
{
    FOREACH_THREAD(thread)
    {
        (*fn) (thread->GetAllocContext(), param);
    }
    END_FOREACH_THREAD
}

// EE can perform post stack scanning action, while the user threads are still suspended
void GCToEEInterface::AfterGcScanRoots(int condemned, int /*max_gen*/, ScanContext* sc)
{
    // Invoke any registered callouts for the end of the mark phase.
    RestrictedCallouts::InvokeGcCallouts(GCRC_AfterMarkPhase, condemned);
#ifdef FEATURE_OBJCMARSHAL
    if (!sc->concurrent)
    {
        ObjCMarshalNative::AfterRefCountedHandleCallbacks();
    }
#endif
}

void GCToEEInterface::GcDone(int condemned)
{
    // Invoke any registered callouts for the end of the collection.
    RestrictedCallouts::InvokeGcCallouts(GCRC_EndCollection, condemned);
}

bool GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
#ifdef FEATURE_OBJCMARSHAL
    bool isReferenced = false;
    if (ObjCMarshalNative::IsTrackedReference(pObject, &isReferenced))
        return isReferenced;
#endif // FEATURE_OBJCMARSHAL
    return RestrictedCallouts::InvokeRefCountedHandleCallbacks(pObject);
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

uint32_t GCToEEInterface::GetActiveSyncBlockCount()
{
    return 0;
}

gc_alloc_context * GCToEEInterface::GetAllocContext()
{
    return ThreadStore::GetCurrentThread()->GetAllocContext();
}
#endif // !DACCESS_COMPILE

uint8_t* GCToEEInterface::GetLoaderAllocatorObjectForGC(Object* pObject)
{
    return nullptr;
}

bool GCToEEInterface::IsPreemptiveGCDisabled()
{
    return ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode();
}

bool GCToEEInterface::EnablePreemptiveGC()
{
#ifndef DACCESS_COMPILE
    Thread* pThread = ThreadStore::GetCurrentThread();

    if (pThread->IsCurrentThreadInCooperativeMode())
    {
        pThread->EnablePreemptiveMode();
        return true;
    }
#else
    UNREFERENCED_PARAMETER(pThread);
#endif
    return false;
}

void GCToEEInterface::DisablePreemptiveGC()
{
#ifndef DACCESS_COMPILE
    ThreadStore::GetCurrentThread()->DisablePreemptiveMode();
#else
    UNREFERENCED_PARAMETER(pThread);
#endif
}

Thread* GCToEEInterface::GetThread()
{
#ifndef DACCESS_COMPILE
    return ThreadStore::GetCurrentThreadIfAvailable();
#else
    return NULL;
#endif
}

#ifndef DACCESS_COMPILE

void GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
    UNREFERENCED_PARAMETER(gen);
    UNREFERENCED_PARAMETER(isInduced);
}

void GCToEEInterface::DiagUpdateGenerationBounds()
{
}

void GCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
    UNREFERENCED_PARAMETER(gcContext);
}

void GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
    UNREFERENCED_PARAMETER(index);
    UNREFERENCED_PARAMETER(gen);
    UNREFERENCED_PARAMETER(reason);

#ifdef FEATURE_EVENT_TRACE
    if (!fConcurrent)
    {
        ETW::GCLog::WalkHeap();
    }
#endif // FEATURE_EVENT_TRACE
}

// Note on last parameter: when calling this for bgc, only ETW
// should be sending these events so that existing profapi profilers
// don't get confused.
void WalkMovedReferences(uint8_t* begin, uint8_t* end,
                         ptrdiff_t reloc,
                         void* context,
                         bool fCompacting,
                         bool fBGC)
{
    UNREFERENCED_PARAMETER(begin);
    UNREFERENCED_PARAMETER(end);
    UNREFERENCED_PARAMETER(reloc);
    UNREFERENCED_PARAMETER(context);
    UNREFERENCED_PARAMETER(fCompacting);
    UNREFERENCED_PARAMETER(fBGC);
}

//
// Diagnostics code
//

#ifdef FEATURE_EVENT_TRACE
// Tracks all surviving objects (moved or otherwise).
inline bool ShouldTrackSurvivorsForProfilerOrEtw()
{
    if (ETW::GCLog::ShouldTrackMovementForEtw())
        return true;

    return false;
}
#endif // FEATURE_EVENT_TRACE

void GCToEEInterface::DiagWalkSurvivors(void* gcContext, bool fCompacting)
{
#ifdef FEATURE_EVENT_TRACE
    if (ShouldTrackSurvivorsForProfilerOrEtw())
    {
        size_t context = 0;
        ETW::GCLog::BeginMovedReferences(&context);
        GCHeapUtilities::GetGCHeap()->DiagWalkSurvivorsWithType(gcContext, &WalkMovedReferences, (void*)context, walk_for_gc);
        ETW::GCLog::EndMovedReferences(context);
    }
#else
    UNREFERENCED_PARAMETER(gcContext);
#endif // FEATURE_EVENT_TRACE
}

void GCToEEInterface::DiagWalkUOHSurvivors(void* gcContext, int gen)
{
#ifdef FEATURE_EVENT_TRACE
    if (ShouldTrackSurvivorsForProfilerOrEtw())
    {
        size_t context = 0;
        ETW::GCLog::BeginMovedReferences(&context);
        GCHeapUtilities::GetGCHeap()->DiagWalkSurvivorsWithType(gcContext, &WalkMovedReferences, (void*)context, walk_for_uoh, gen);
        ETW::GCLog::EndMovedReferences(context);
    }
#else
    UNREFERENCED_PARAMETER(gcContext);
#endif // FEATURE_EVENT_TRACE
}

void GCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
#ifdef FEATURE_EVENT_TRACE
    if (ShouldTrackSurvivorsForProfilerOrEtw())
    {
        size_t context = 0;
        ETW::GCLog::BeginMovedReferences(&context);
        GCHeapUtilities::GetGCHeap()->DiagWalkSurvivorsWithType(gcContext, &WalkMovedReferences, (void*)context, walk_for_bgc);
        ETW::GCLog::EndMovedReferences(context);
    }
#else
    UNREFERENCED_PARAMETER(gcContext);
#endif // FEATURE_EVENT_TRACE
}

void GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
    // NativeAOT doesn't patch the write barrier like CoreCLR does, but it
    // still needs to record the changes in the GC heap.

    bool is_runtime_suspended = args->is_runtime_suspended;

    switch (args->operation)
    {
    case WriteBarrierOp::StompResize:
        // StompResize requires a new card table, a new lowest address, and
        // a new highest address
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);

        // We are sensitive to the order of writes here(more comments on this further in the method)
        // In particular g_card_table must be written before writing the heap bounds.
        // For platforms with weak memory ordering we will issue fences, for x64/x86 we are ok
        // as long as compiler does not reorder these writes.
        // That is unlikely since we have method calls in between.
        // Just to be robust agains possible refactoring/inlining we will do a compiler-fenced store here.
        VolatileStore(&g_card_table, args->card_table);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(args->card_bundle_table != nullptr);
        g_card_bundle_table = args->card_bundle_table;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        if (g_sw_ww_enabled_for_gc_heap && (args->write_watch_table != nullptr))
        {
            assert(args->is_runtime_suspended);
            g_write_watch_table = args->write_watch_table;
        }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        // IMPORTANT: managed heap segments may surround unmanaged/stack segments. In such cases adding another managed
        //     heap segment may put a stack/unmanaged write inside the new heap range. However the old card table would
        //     not cover it. Therefore we must ensure that the write barriers see the new table before seeing the new bounds.
        //
        //     On architectures with strong ordering, we only need to prevent compiler reordering.
        //     Otherwise we put a process-wide fence here (so that we could use an ordinary read in the barrier)

#if defined(HOST_ARM64) || defined(HOST_ARM)
        if (!is_runtime_suspended)
        {
            // If runtime is not suspended, force all threads to see the changed table before seeing updated heap boundaries.
            // See: http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/346765
            FlushProcessWriteBuffers();
        }
#endif

        g_lowest_address = args->lowest_address;
        g_highest_address = args->highest_address;

#if defined(HOST_ARM64) || defined(HOST_ARM)
        if (!is_runtime_suspended)
        {
            // If runtime is not suspended, force all threads to see the changed state before observing future allocations.
            FlushProcessWriteBuffers();
        }
#endif
        return;
    case WriteBarrierOp::StompEphemeral:
        // StompEphemeral requires a new ephemeral low and a new ephemeral high
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        g_ephemeral_low = args->ephemeral_low;
        g_ephemeral_high = args->ephemeral_high;
        return;
    case WriteBarrierOp::Initialize:
        // This operation should only be invoked once, upon initialization.
        assert(g_card_table == nullptr);
        assert(g_lowest_address == nullptr);
        assert(g_highest_address == nullptr);
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");

        g_card_table = args->card_table;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(g_card_bundle_table == nullptr);
        g_card_bundle_table = args->card_bundle_table;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        assert(g_write_watch_table == nullptr);
        g_write_watch_table = args->write_watch_table;
#endif

        g_lowest_address = args->lowest_address;
        g_highest_address = args->highest_address;
        g_ephemeral_low = args->ephemeral_low;
        g_ephemeral_high = args->ephemeral_high;
        return;
    case WriteBarrierOp::SwitchToWriteWatch:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
        assert(args->write_watch_table != nullptr);
        g_write_watch_table = args->write_watch_table;
        g_sw_ww_enabled_for_gc_heap = true;
#else
        assert(!"should never be called without FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP");
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        break;

    case WriteBarrierOp::SwitchToNonWriteWatch:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
        g_write_watch_table = nullptr;
        g_sw_ww_enabled_for_gc_heap = false;
#else
        assert(!"should never be called without FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP");
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        return;
    default:
        assert(!"Unknokwn WriteBarrierOp enum");
        return;
    }
}

void GCToEEInterface::EnableFinalization(bool gcHasWorkForFinalizerThread)
{
    if (gcHasWorkForFinalizerThread)
        RhEnableFinalization();
}

void GCToEEInterface::HandleFatalError(unsigned int exitCode)
{
    UNREFERENCED_PARAMETER(exitCode);
    EEPOLICY_HANDLE_FATAL_ERROR(exitCode);
}

bool GCToEEInterface::EagerFinalized(Object* obj)
{
#ifdef FEATURE_OBJCMARSHAL
    if (obj->GetGCSafeMethodTable()->IsTrackedReferenceWithFinalizer())
    {
        ObjCMarshalNative::OnEnteredFinalizerQueue(obj);
        return false;
    }
#endif

    if (!obj->GetGCSafeMethodTable()->HasEagerFinalizer())
        return false;

    // Eager finalization happens while scanning for unmarked finalizable objects
    // after marking strongly reachable and prior to marking dependent and long weak handles.
    // Managed code should not be running.
    ASSERT(GCHeapUtilities::GetGCHeap()->IsGCInProgressHelper());

    // the lowermost 2 bits are reserved for storing additional info about the handle
    // we can use these bits because handle is at least 4 byte aligned
    const uintptr_t HandleTagBits = 3;

    WeakReference* weakRefObj = (WeakReference*)obj;
    OBJECTHANDLE handle = (OBJECTHANDLE)(weakRefObj->m_taggedHandle & ~HandleTagBits);
    HandleType handleType = (weakRefObj->m_taggedHandle & 2) ?
        HandleType::HNDTYPE_STRONG :
        (weakRefObj->m_taggedHandle & 1) ?
        HandleType::HNDTYPE_WEAK_LONG :
        HandleType::HNDTYPE_WEAK_SHORT;
    // keep the bit that indicates whether this reference was tracking resurrection, clear the rest.
    weakRefObj->m_taggedHandle &= (uintptr_t)1;
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, handleType);
    return true;
}

bool GCToEEInterface::IsGCThread()
{
    Thread* pCurrentThread = ThreadStore::RawGetCurrentThread();
    return pCurrentThread->IsGCSpecial() || pCurrentThread == ThreadStore::GetSuspendingThread();
}

bool GCToEEInterface::WasCurrentThreadCreatedByGC()
{
    return ThreadStore::RawGetCurrentThread()->IsGCSpecial();
}

struct ThreadStubArguments
{
    void (*m_pRealStartRoutine)(void*);
    void* m_pRealContext;
    CLREventStatic m_ThreadStartedEvent;
};

static bool CreateNonSuspendableThread(void (*threadStart)(void*), void* arg, const char* name)
{
    UNREFERENCED_PARAMETER(name);

    ThreadStubArguments* threadStubArgs = new (nothrow) ThreadStubArguments();
    if (!threadStubArgs)
        return false;

    threadStubArgs->m_pRealStartRoutine = threadStart;
    threadStubArgs->m_pRealContext = arg;

    // Helper used to wrap the start routine of GC threads so we can do things like initialize the
    // thread state which requires running in the new thread's context.
    auto threadStub = [](void* argument) -> DWORD
        {
            ThreadStore::RawGetCurrentThread()->SetGCSpecial();

            ThreadStubArguments* pStartContext = (ThreadStubArguments*)argument;
            auto realStartRoutine = pStartContext->m_pRealStartRoutine;
            void* realContext = pStartContext->m_pRealContext;
            delete pStartContext;

            STRESS_LOG_RESERVE_MEM(GC_STRESSLOG_MULTIPLY);

            realStartRoutine(realContext);

            return 0;
        };

    if (!PalStartBackgroundGCThread(threadStub, threadStubArgs))
    {
        delete threadStubArgs;
        return false;
    }

    return true;
}

bool GCToEEInterface::CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
{
    UNREFERENCED_PARAMETER(name);

    if (!is_suspendable)
        return CreateNonSuspendableThread(threadStart, arg, name);

    ThreadStubArguments threadStubArgs;
    threadStubArgs.m_pRealStartRoutine = threadStart;
    threadStubArgs.m_pRealContext = arg;

    if (!threadStubArgs.m_ThreadStartedEvent.CreateAutoEventNoThrow(false))
    {
        return false;
    }

    // Helper used to wrap the start routine of background GC threads so we can do things like initialize the
    // thread state which requires running in the new thread's context.
    auto threadStub = [](void* argument) -> DWORD
        {
            ThreadStubArguments* pStartContext = (ThreadStubArguments*)argument;

            // Initialize the Thread for this thread. The false being passed indicates that the thread store lock
            // should not be acquired as part of this operation. This is necessary because this thread is created in
            // the context of a garbage collection and the lock is already held by the GC.
            // This also implies that creation and initialization must proceed sequentially, one thread after another.
            // GCToEEInterface::CreateThread will not return until the thread is done attaching itself.
            ASSERT(GCHeapUtilities::IsGCInProgress());
            ThreadStore::AttachCurrentThread(false);

            ThreadStore::RawGetCurrentThread()->SetGCSpecial();

            auto realStartRoutine = pStartContext->m_pRealStartRoutine;
            void* realContext = pStartContext->m_pRealContext;

            pStartContext->m_ThreadStartedEvent.Set();

            STRESS_LOG_RESERVE_MEM(GC_STRESSLOG_MULTIPLY);

            realStartRoutine(realContext);

            return 0;
        };

    if (!PalStartBackgroundGCThread(threadStub, &threadStubArgs))
    {
        threadStubArgs.m_ThreadStartedEvent.CloseEvent();
        return false;
    }

    uint32_t res = threadStubArgs.m_ThreadStartedEvent.Wait(INFINITE, FALSE);
    threadStubArgs.m_ThreadStartedEvent.CloseEvent();
    ASSERT(res == WAIT_OBJECT_0);

    return true;
}

// NativeAOT does not use async pinned handles
void GCToEEInterface::WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, ScanFunc* callback)
{
    UNREFERENCED_PARAMETER(object);
    UNREFERENCED_PARAMETER(sc);
    UNREFERENCED_PARAMETER(callback);
}

void GCToEEInterface::WalkAsyncPinned(Object* object, void* context, void (*callback)(Object*, Object*, void*))
{
    UNREFERENCED_PARAMETER(object);
    UNREFERENCED_PARAMETER(context);
    UNREFERENCED_PARAMETER(callback);
}

IGCToCLREventSink* GCToEEInterface::EventSink()
{
    return &g_gcToClrEventSink;
}

uint32_t GCToEEInterface::GetTotalNumSizedRefHandles()
{
    return -1;
}

bool GCToEEInterface::AnalyzeSurvivorsRequested(int condemnedGeneration)
{
    return false;
}

void GCToEEInterface::AnalyzeSurvivorsFinished(size_t gcIndex, int condemnedGeneration, uint64_t promoted_bytes, void (*reportGenerationBounds)())
{
}

void GCToEEInterface::VerifySyncTableEntry()
{
}

void GCToEEInterface::UpdateGCEventStatus(int currentPublicLevel, int currentPublicKeywords, int currentPrivateLevel, int currentPrivateKeywords)
{
    UNREFERENCED_PARAMETER(currentPublicLevel);
    UNREFERENCED_PARAMETER(currentPublicKeywords);
    UNREFERENCED_PARAMETER(currentPrivateLevel);
    UNREFERENCED_PARAMETER(currentPrivateKeywords);
    // TODO: Linux LTTng
}

void GCToEEInterface::LogStressMsg(unsigned level, unsigned facility, const StressLogMsg& msg)
{
    // TODO: Implementation
}

uint32_t GCToEEInterface::GetCurrentProcessCpuCount()
{
    return PalGetProcessCpuCount();
}

void GCToEEInterface::DiagAddNewRegion(int generation, uint8_t* rangeStart, uint8_t* rangeEnd, uint8_t* rangeEndReserved)
{
}

MethodTable* GCToEEInterface::GetFreeObjectMethodTable()
{
    assert(g_pFreeObjectEEType != nullptr);
    return (MethodTable*)g_pFreeObjectEEType;
}

bool GCToEEInterface::GetBooleanConfigValue(const char* privateKey, const char* publicKey, bool* value)
{
    if (strcmp(privateKey, "gcConservative") == 0)
    {
        *value = true;
        return true;
    }

    uint64_t uiValue;
    if (g_pRhConfig->ReadConfigValue(privateKey, &uiValue))
    {
        *value = uiValue != 0;
        return true;
    }

    if (publicKey)
    {
        if (g_pRhConfig->ReadKnobBooleanValue(publicKey, value))
        {
            return true;
        }
    }

    return false;
}

extern GCHeapHardLimitInfo g_gcHeapHardLimitInfo;
extern bool g_gcHeapHardLimitInfoSpecified;

bool GCToEEInterface::GetIntConfigValue(const char* privateKey, const char* publicKey, int64_t* value)
{
    if (g_gcHeapHardLimitInfoSpecified)
    {
        if ((g_gcHeapHardLimitInfo.heapHardLimit != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimit") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimit; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitPercent; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitSOH != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitSOH") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitSOH; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitLOH != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitLOH") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitLOH; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitPOH != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitPOH") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitPOH; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitSOHPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitSOHPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitSOHPercent; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitLOHPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitLOHPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitLOHPercent; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitPOHPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitPOHPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitPOHPercent; return true; }
    }

    uint64_t uiValue;
    if (g_pRhConfig->ReadConfigValue(privateKey, &uiValue))
    {
        *value = uiValue;
        return true;
    }

    if (publicKey)
    {
        if (g_pRhConfig->ReadKnobUInt64Value(publicKey, &uiValue))
        {
            *value = uiValue;
            return true;
        }
    }

    return false;
}

void GCToEEInterface::LogErrorToHost(const char *message)
{
}

bool GCToEEInterface::GetStringConfigValue(const char* privateKey, const char* publicKey, const char** value)
{
    UNREFERENCED_PARAMETER(privateKey);
    UNREFERENCED_PARAMETER(publicKey);
    UNREFERENCED_PARAMETER(value);

    return false;
}

void GCToEEInterface::FreeStringConfigValue(const char* value)
{
    delete[] value;
}

#endif // !DACCESS_COMPILE
