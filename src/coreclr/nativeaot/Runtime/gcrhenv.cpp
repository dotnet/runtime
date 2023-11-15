// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This module provides data storage and implementations needed by gcrhenv.h to help provide an isolated build
// and runtime environment in which GC and HandleTable code can exist with minimal modifications from the CLR
// mainline. See gcrhenv.h for a more detailed explanation of how this all fits together.
//

#include "common.h"

#include "gcenv.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "gcenv.ee.h"

#include "RestrictedCallouts.h"

#include "gcrhinterface.h"

#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"

#include "thread.h"

#include "shash.h"
#include "TypeManager.h"
#include "RuntimeInstance.h"
#include "objecthandle.h"
#include "MethodTable.inl"
#include "RhConfig.h"

#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "gcdesc.h"
#include "SyncClean.hpp"

#include "daccess.h"

#include "interoplibinterface.h"

#include "holder.h"
#include "volatile.h"

GPTR_IMPL(MethodTable, g_pFreeObjectEEType);

#include "gctoclreventsink.h"

#ifndef DACCESS_COMPILE

bool RhInitializeFinalization();
bool RhStartFinalizerThread();
void RhEnableFinalization();

// A few settings are now backed by the cut-down version of Redhawk configuration values.
static RhConfig g_sRhConfig;
RhConfig * g_pRhConfig = &g_sRhConfig;

//
// -----------------------------------------------------------------------------------------------------------
//
// The rest of Redhawk needs to be able to talk to the GC/HandleTable code (to initialize it, allocate
// objects etc.) without pulling in the entire adaptation layer provided by this file and gcrhenv.h. To this
// end the rest of Redhawk talks to us via a simple interface described in gcrhinterface.h. We provide the
// implementation behind those APIs here.
//

// Perform any runtime-startup initialization needed by the GC, HandleTable or environmental code in gcrhenv.
// The boolean parameter should be true if a server GC is required and false for workstation. Returns true on
// success or false if a subsystem failed to initialize.

#ifndef DACCESS_COMPILE
#ifdef _MSC_VER
#pragma warning(disable:4815) // zero-sized array in stack object will have no elements
#endif // _MSC_VER
MethodTable g_FreeObjectEEType;

// static
bool RedhawkGCInterface::InitializeSubsystems()
{
    // Initialize the special MethodTable used to mark free list entries in the GC heap.
    g_FreeObjectEEType.InitializeAsGcFreeType();
    g_pFreeObjectEEType = &g_FreeObjectEEType;

#ifdef FEATURE_SVR_GC
    g_heap_type = (g_pRhConfig->GetgcServer() && PalGetProcessCpuCount() > 1) ? GC_HEAP_SVR : GC_HEAP_WKS;
#else
    g_heap_type = GC_HEAP_WKS;
#endif

    if (g_pRhConfig->GetgcConservative())
    {
        GetRuntimeInstance()->EnableConservativeStackReporting();
    }

    HRESULT hr = GCHeapUtilities::InitializeDefaultGC();
    if (FAILED(hr))
        return false;

    // Apparently the Windows linker removes global variables if they are never
    // read from, which is a problem for g_gcDacGlobals since it's expected that
    // only the DAC will read from it. This forces the linker to include
    // g_gcDacGlobals.
    volatile void* _dummy = g_gcDacGlobals;

    // Initialize the GC subsystem.
    hr = g_pGCHeap->Initialize();
    if (FAILED(hr))
        return false;

    if (!RhInitializeFinalization())
        return false;

    // Initialize HandleTable.
    if (!GCHandleUtilities::GetGCHandleManager()->Initialize())
        return false;

    return true;
}
#endif // !DACCESS_COMPILE

Object* GcAllocInternal(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, Thread* pThread)
{
    ASSERT(!pThread->IsDoNotTriggerGcSet());
    ASSERT(pThread->IsCurrentThreadInCooperativeMode());

    size_t cbSize = pEEType->get_BaseSize();

    if (pEEType->HasComponentSize())
    {
        // Impose limits on maximum array length to prevent corner case integer overflow bugs
        // Keep in sync with Array.MaxLength in BCL.
        if (pEEType->IsSzArray()) // multi-dimensional arrays are checked up-front
        {
            const int MaxArrayLength = 0x7FFFFFC7;
            if (numElements > MaxArrayLength)
                return NULL;
        }

#ifndef HOST_64BIT
        // if the element count is <= 0x10000, no overflow is possible because the component size is
        // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
        if (numElements > 0x10000)
        {
            // Perform the size computation using 64-bit integeres to detect overflow
            uint64_t size64 = (uint64_t)cbSize + ((uint64_t)numElements * (uint64_t)pEEType->RawGetComponentSize());
            size64 = (size64 + (sizeof(uintptr_t) - 1)) & ~(sizeof(uintptr_t) - 1);

            cbSize = (size_t)size64;
            if (cbSize != size64)
            {
                return NULL;
            }
        }
        else
#endif // !HOST_64BIT
        {
            cbSize = cbSize + ((size_t)numElements * (size_t)pEEType->RawGetComponentSize());
            cbSize = ALIGN_UP(cbSize, sizeof(uintptr_t));
        }
    }
    else
    {
        ASSERT(numElements == 0);
    }

    if (cbSize >= RH_LARGE_OBJECT_SIZE)
    {
        uFlags |= GC_ALLOC_LARGE_OBJECT_HEAP;

#ifdef HOST_64BIT
        const size_t max_object_size = (INT64_MAX - 7 - min_obj_size);
#else
        const size_t max_object_size = (INT32_MAX - 7 - min_obj_size);
#endif

        if (cbSize >= max_object_size)
            return NULL;
    }

    // Save the MethodTable for instrumentation purposes.
    RedhawkGCInterface::SetLastAllocEEType(pEEType);

    Object * pObject = GCHeapUtilities::GetGCHeap()->Alloc(pThread->GetAllocContext(), cbSize, uFlags);
    if (pObject == NULL)
        return NULL;

    pObject->set_EEType(pEEType);
    if (pEEType->HasComponentSize())
    {
        ASSERT(numElements == (uint32_t)numElements);
        ((Array*)pObject)->InitArrayLength((uint32_t)numElements);
    }

    if (uFlags & GC_ALLOC_USER_OLD_HEAP)
        GCHeapUtilities::GetGCHeap()->PublishObject((uint8_t*)pObject);

#ifdef _DEBUG
    // We assume that the allocation quantum is never big enough for LARGE_OBJECT_SIZE.
    gc_alloc_context* acontext = pThread->GetAllocContext();
    ASSERT(acontext->alloc_limit - acontext->alloc_ptr <= RH_LARGE_OBJECT_SIZE);
#endif

    return pObject;
}

// Allocate an object on the GC heap.
//  pEEType         -  type of the object
//  uFlags          -  GC type flags (see gc.h GC_ALLOC_*)
//  numElements     -  number of array elements
//  pTransitionFrame-  transition frame to make stack crawlable
// Returns a pointer to the object allocated or NULL on failure.

COOP_PINVOKE_HELPER(void*, RhpGcAlloc, (MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements, PInvokeTransitionFrame* pTransitionFrame))
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    // The allocation fast path is an asm helper that runs in coop mode and handles most allocation cases.
    // The helper can also be tail-called. That is desirable for the fast path.
    //
    // Here we are on the slow(er) path when we need to call into GC. The fast path pushes a frame and calls here.
    // In extremely rare cases the caller of the asm helper is hijacked and the helper is tail-called.
    // As a result the asm helper may capture a hijacked return address into the transition frame.
    // We do not want to put the burden of preventing such scenario on the fast path. Instead we will
    // check for "hijacked frame" here and un-hijack m_RIP.
    // We do not need to re-hijack when we are done, since m_RIP is discarded in POP_COOP_PINVOKE_FRAME
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    if (Thread::IsHijackTarget(pTransitionFrame->m_RIP))
    {
        ASSERT(pThread->IsHijacked());
        pTransitionFrame->m_RIP = pThread->GetHijackedReturnAddress();
    }
#else

    // NOTE: The x64 fixup above would not be sufficient on ARM64 and similar architectures since
    //       m_RIP is used to restore LR in POP_COOP_PINVOKE_FRAME.
    //       However, this entire scenario is not a problem on architectures where the return address is
    //       in a register as that makes tail-calling methods not hijackable.
    //       (see:GetReturnAddressHijackInfo for detailed reasons in the context of ARM64)
    ASSERT(!Thread::IsHijackTarget(pTransitionFrame->m_RIP));

#endif

    pThread->SetDeferredTransitionFrame(pTransitionFrame);

    return GcAllocInternal(pEEType, uFlags, numElements, pThread);
}

// static
void RedhawkGCInterface::InitAllocContext(gc_alloc_context * pAllocContext)
{
    // NOTE: This method is currently unused because the thread's alloc_context is initialized via
    // static initialization of tls_CurrentThread.  If the initial contents of the alloc_context
    // ever change, then a matching change will need to be made to the tls_CurrentThread static
    // initializer.

    pAllocContext->init();
}

// static
void RedhawkGCInterface::ReleaseAllocContext(gc_alloc_context * pAllocContext)
{
    s_DeadThreadsNonAllocBytes += pAllocContext->alloc_limit - pAllocContext->alloc_ptr;
    GCHeapUtilities::GetGCHeap()->FixAllocContext(pAllocContext, NULL, NULL);
}

// static
void RedhawkGCInterface::WaitForGCCompletion()
{
    GCHeapUtilities::GetGCHeap()->WaitUntilGCComplete();
}

//-------------------------------------------------------------------------------------------------
// Used only by GC initialization, this initializes the MethodTable used to mark free entries in the GC heap. It
// should be an array type with a component size of one (so the GC can easily size it as appropriate) and
// should be marked as not containing any references. The rest of the fields don't matter: the GC does not
// query them and the rest of the runtime will never hold a reference to free object.

void MethodTable::InitializeAsGcFreeType()
{
    m_uFlags = ParameterizedEEType | HasComponentSizeFlag;
    m_usComponentSize = 1;
    m_uBaseSize = sizeof(Array) + SYNC_BLOCK_SKEW;
}

#endif // !DACCESS_COMPILE

extern void GcEnumObject(PTR_OBJECTREF pObj, uint32_t flags, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc);
extern void GcEnumObjectsConservatively(PTR_OBJECTREF pLowerBound, PTR_OBJECTREF pUpperBound, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc);
extern void GcBulkEnumObjects(PTR_OBJECTREF pObjs, DWORD cObjs, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc);

struct EnumGcRefContext : GCEnumContext
{
    EnumGcRefCallbackFunc * f;
    EnumGcRefScanContext * sc;
};

static void EnumGcRefsCallback(void * hCallback, PTR_PTR_VOID pObject, uint32_t flags)
{
    EnumGcRefContext * pCtx = (EnumGcRefContext *)hCallback;

    GcEnumObject((PTR_OBJECTREF)pObject, flags, pCtx->f, pCtx->sc);
}

// static
void RedhawkGCInterface::EnumGcRefs(ICodeManager * pCodeManager,
                                    MethodInfo * pMethodInfo,
                                    PTR_VOID safePointAddress,
                                    REGDISPLAY * pRegisterSet,
                                    void * pfnEnumCallback,
                                    void * pvCallbackData,
                                    bool   isActiveStackFrame)
{
    EnumGcRefContext ctx;
    ctx.pCallback = EnumGcRefsCallback;
    ctx.f  = (EnumGcRefCallbackFunc *)pfnEnumCallback;
    ctx.sc = (EnumGcRefScanContext *)pvCallbackData;
    ctx.sc->stack_limit = pRegisterSet->GetSP();

    pCodeManager->EnumGcRefs(pMethodInfo,
                             safePointAddress,
                             pRegisterSet,
                             &ctx,
                             isActiveStackFrame);
}

// static
void RedhawkGCInterface::EnumGcRefsInRegionConservatively(PTR_RtuObjectRef pLowerBound,
                                                          PTR_RtuObjectRef pUpperBound,
                                                          void * pfnEnumCallback,
                                                          void * pvCallbackData)
{
    GcEnumObjectsConservatively((PTR_OBJECTREF)pLowerBound, (PTR_OBJECTREF)pUpperBound, (EnumGcRefCallbackFunc *)pfnEnumCallback, (EnumGcRefScanContext *)pvCallbackData);
}

// static
void RedhawkGCInterface::EnumGcRef(PTR_RtuObjectRef pRef, GCRefKind kind, void * pfnEnumCallback, void * pvCallbackData)
{
    ASSERT((GCRK_Object == kind) || (GCRK_Byref == kind));

    DWORD flags = 0;

    if (kind == GCRK_Byref)
    {
        flags |= GC_CALL_INTERIOR;
    }

    GcEnumObject((PTR_OBJECTREF)pRef, flags, (EnumGcRefCallbackFunc *)pfnEnumCallback, (EnumGcRefScanContext *)pvCallbackData);
}

// static
void RedhawkGCInterface::EnumGcRefConservatively(PTR_RtuObjectRef pRef, void* pfnEnumCallback, void* pvCallbackData)
{
    GcEnumObject((PTR_OBJECTREF)pRef, GC_CALL_INTERIOR | GC_CALL_PINNED, (EnumGcRefCallbackFunc*)pfnEnumCallback, (EnumGcRefScanContext*)pvCallbackData);
}

#ifndef DACCESS_COMPILE

// static
void RedhawkGCInterface::BulkEnumGcObjRef(PTR_RtuObjectRef pRefs, uint32_t cRefs, void * pfnEnumCallback, void * pvCallbackData)
{
    GcBulkEnumObjects((PTR_OBJECTREF)pRefs, cRefs, (EnumGcRefCallbackFunc *)pfnEnumCallback, (EnumGcRefScanContext *)pvCallbackData);
}

// static
GcSegmentHandle RedhawkGCInterface::RegisterFrozenSegment(void * pSection, size_t SizeSection)
{
#ifdef FEATURE_BASICFREEZE
    segment_info seginfo;

    seginfo.pvMem           = pSection;
    seginfo.ibFirstObject   = sizeof(ObjHeader);
    seginfo.ibAllocated     = SizeSection;
    seginfo.ibCommit        = seginfo.ibAllocated;
    seginfo.ibReserved      = seginfo.ibAllocated;

    return (GcSegmentHandle)GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&seginfo);
#else // FEATURE_BASICFREEZE
    return NULL;
#endif // FEATURE_BASICFREEZE
}

// static
void RedhawkGCInterface::UnregisterFrozenSegment(GcSegmentHandle segment)
{
    GCHeapUtilities::GetGCHeap()->UnregisterFrozenSegment((segment_handle)segment);
}

EXTERN_C UInt32_BOOL g_fGcStressStarted;
UInt32_BOOL g_fGcStressStarted = UInt32_FALSE; // UInt32_BOOL because asm code reads it
#ifdef FEATURE_GC_STRESS
// static
void RedhawkGCInterface::StressGc()
{
    // The GarbageCollect operation below may trash the last win32 error. We save the error here so that it can be
    // restored after the GC operation;
    int32_t lastErrorOnEntry = PalGetLastError();

    if (g_fGcStressStarted && !ThreadStore::GetCurrentThread()->IsSuppressGcStressSet() && !ThreadStore::GetCurrentThread()->IsDoNotTriggerGcSet())
    {
        GCHeapUtilities::GetGCHeap()->GarbageCollect();
    }

    // Restore the saved error
    PalSetLastError(lastErrorOnEntry);
}
#endif // FEATURE_GC_STRESS


#ifdef FEATURE_GC_STRESS
COOP_PINVOKE_HELPER(void, RhpInitializeGcStress, ())
{
    g_fGcStressStarted = UInt32_TRUE;
}
#endif // FEATURE_GC_STRESS

#endif // !DACCESS_COMPILE

//
// Support for scanning the GC heap, objects and roots.
//

// Enumerate every reference field in an object, calling back to the specified function with the given context
// for each such reference found.
// static
void RedhawkGCInterface::ScanObject(void *pObject, GcScanObjectFunction pfnScanCallback, void *pContext)
{
#if !defined(DACCESS_COMPILE) && defined(FEATURE_EVENT_TRACE)
    GCHeapUtilities::GetGCHeap()->DiagWalkObject((Object*)pObject, (walk_fn)pfnScanCallback, pContext);
#else
    UNREFERENCED_PARAMETER(pObject);
    UNREFERENCED_PARAMETER(pfnScanCallback);
    UNREFERENCED_PARAMETER(pContext);
#endif // DACCESS_COMPILE
}

// When scanning for object roots we use existing GC APIs used for object promotion and moving. We use an
// adapter callback to transform the promote function signature used for these methods into something simpler
// that avoids exposing unnecessary implementation details. The pointer to a ScanContext normally passed to
// promotion functions is actually a pointer to the structure below which serves to recall the actual function
// pointer and context for the real context.
struct ScanRootsContext
{
    GcScanRootFunction  m_pfnCallback;
    void *              m_pContext;
};

// Callback with a EnumGcRefCallbackFunc signature that forwards the call to a callback with a GcScanFunction signature
// and its own context.
void ScanRootsCallbackWrapper(Object** pObject, EnumGcRefScanContext* pContext, DWORD dwFlags)
{
    UNREFERENCED_PARAMETER(dwFlags);

    ScanRootsContext * pRealContext = (ScanRootsContext*)pContext;

    (*pRealContext->m_pfnCallback)((void**)&pObject, pRealContext->m_pContext);
}

// Enumerate all the object roots located on the specified thread's stack. It is only safe to call this from
// the context of a GC.
//
// static
void RedhawkGCInterface::ScanStackRoots(Thread *pThread, GcScanRootFunction pfnScanCallback, void *pContext)
{
#ifndef DACCESS_COMPILE
    ScanRootsContext sContext;
    sContext.m_pfnCallback = pfnScanCallback;
    sContext.m_pContext = pContext;

    pThread->GcScanRoots(reinterpret_cast<void*>(ScanRootsCallbackWrapper), &sContext);
#else
    UNREFERENCED_PARAMETER(pThread);
    UNREFERENCED_PARAMETER(pfnScanCallback);
    UNREFERENCED_PARAMETER(pContext);
#endif // !DACCESS_COMPILE
}

// Enumerate all the object roots located in statics. It is only safe to call this from the context of a GC.
//
// static
void RedhawkGCInterface::ScanStaticRoots(GcScanRootFunction pfnScanCallback, void *pContext)
{
    UNREFERENCED_PARAMETER(pfnScanCallback);
    UNREFERENCED_PARAMETER(pContext);
}

// Enumerate all the object roots located in handle tables. It is only safe to call this from the context of a
// GC.
//
// static
void RedhawkGCInterface::ScanHandleTableRoots(GcScanRootFunction pfnScanCallback, void *pContext)
{
#if !defined(DACCESS_COMPILE) && defined(FEATURE_EVENT_TRACE)
    ScanRootsContext sContext;
    sContext.m_pfnCallback = pfnScanCallback;
    sContext.m_pContext = pContext;
    Ref_ScanPointers(2, 2, (EnumGcRefScanContext*)&sContext, ScanRootsCallbackWrapper);
#else
    UNREFERENCED_PARAMETER(pfnScanCallback);
    UNREFERENCED_PARAMETER(pContext);
#endif // !DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

uint32_t RedhawkGCInterface::GetGCDescSize(void * pType)
{
    MethodTable * pMT = (MethodTable *)pType;

    if (!pMT->ContainsPointersOrCollectible())
        return 0;

    return (uint32_t)CGCDesc::GetCGCDescFromMT(pMT)->GetSize();
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhCompareObjectContentsAndPadding, (Object* pObj1, Object* pObj2))
{
    ASSERT(pObj1->get_EEType()->IsEquivalentTo(pObj2->get_EEType()));
    ASSERT(pObj1->get_EEType()->IsValueType());

    MethodTable * pEEType = pObj1->get_EEType();
    size_t cbFields = pEEType->get_BaseSize() - (sizeof(ObjHeader) + sizeof(MethodTable*));

    uint8_t * pbFields1 = (uint8_t*)pObj1 + sizeof(MethodTable*);
    uint8_t * pbFields2 = (uint8_t*)pObj2 + sizeof(MethodTable*);

    // memcmp is ok in a COOP method as we are comparing structs which are typically small.
    FC_RETURN_BOOL(memcmp(pbFields1, pbFields2, cbFields) == 0);
}

// Thread static representing the last allocation.
// This is used to log the type information for each slow allocation.
DECLSPEC_THREAD
MethodTable * RedhawkGCInterface::tls_pLastAllocationEEType = NULL;

// Get the last allocation for this thread.
MethodTable * RedhawkGCInterface::GetLastAllocEEType()
{
    return tls_pLastAllocationEEType;
}

// Set the last allocation for this thread.
void RedhawkGCInterface::SetLastAllocEEType(MethodTable * pEEType)
{
    tls_pLastAllocationEEType = pEEType;
}

uint64_t RedhawkGCInterface::s_DeadThreadsNonAllocBytes = 0;

uint64_t RedhawkGCInterface::GetDeadThreadsNonAllocBytes()
{
#ifdef HOST_64BIT
    return s_DeadThreadsNonAllocBytes;
#else
    // As it could be noticed we read 64bit values that may be concurrently updated.
    // Such reads are not guaranteed to be atomic on 32bit so extra care should be taken.
    return PalInterlockedCompareExchange64((int64_t*)&s_DeadThreadsNonAllocBytes, 0, 0);
#endif
}

void RedhawkGCInterface::DestroyTypedHandle(void * handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfUnknownType((OBJECTHANDLE)handle);
}

void* RedhawkGCInterface::CreateTypedHandle(void* pObject, int type)
{
    return (void*)GCHandleUtilities::GetGCHandleManager()->GetGlobalHandleStore()->CreateHandleOfType((Object*)pObject, (HandleType)type);
}

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
    bool m_isSuspendable;
    CLREventStatic m_ThreadStartedEvent;
};

bool GCToEEInterface::CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
{
    UNREFERENCED_PARAMETER(name);

    ThreadStubArguments threadStubArgs;

    threadStubArgs.m_pRealStartRoutine = threadStart;
    threadStubArgs.m_pRealContext = arg;
    threadStubArgs.m_isSuspendable = is_suspendable;

    if (!threadStubArgs.m_ThreadStartedEvent.CreateAutoEventNoThrow(false))
    {
        return false;
    }

    // Helper used to wrap the start routine of background GC threads so we can do things like initialize the
    // Redhawk thread state which requires running in the new thread's context.
    auto threadStub = [](void* argument) -> DWORD
    {
        ThreadStubArguments* pStartContext = (ThreadStubArguments*)argument;

        if (pStartContext->m_isSuspendable)
        {
            // Initialize the Thread for this thread. The false being passed indicates that the thread store lock
            // should not be acquired as part of this operation. This is necessary because this thread is created in
            // the context of a garbage collection and the lock is already held by the GC.
            ASSERT(GCHeapUtilities::IsGCInProgress());

            ThreadStore::AttachCurrentThread(false);
        }

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
void GCToEEInterface::WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback)
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

// NOTE: this method is not in thread.cpp because it needs access to the layout of alloc_context for DAC to know the
// size, but thread.cpp doesn't generally need to include the GC environment headers for any other reason.
gc_alloc_context * Thread::GetAllocContext()
{
    return dac_cast<DPTR(gc_alloc_context)>(dac_cast<TADDR>(this) + offsetof(Thread, m_rgbAllocContextBuffer));
}

GPTR_IMPL(Thread, g_pFinalizerThread);
GPTR_IMPL(Thread, g_pGcThread);

#ifndef DACCESS_COMPILE

bool __SwitchToThread(uint32_t dwSleepMSec, uint32_t /*dwSwitchCount*/)
{
    if (dwSleepMSec > 0)
    {
        PalSleep(dwSleepMSec);
        return true;
    }
    return !!PalSwitchToThread();
}

#endif // DACCESS_COMPILE

void LogSpewAlways(const char * /*fmt*/, ...)
{
}
