// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "RedhawkGCInterface.h"

#include "RhConfig.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "regdisplay.h"

#include "gcdesc.h"
#include "daccess.h"

#ifndef DACCESS_COMPILE

// A few settings are now backed by the cut-down version of Redhawk configuration values.
static RhConfig g_sRhConfig;
RhConfig * g_pRhConfig = &g_sRhConfig;

MethodTable g_FreeObjectEEType;
GPTR_DECL(MethodTable, g_pFreeObjectEEType);

GPTR_IMPL(Thread, g_pFinalizerThread);

bool RhInitializeFinalization();

// Perform any runtime-startup initialization needed by the GC, HandleTable or environmental code in gcrhenv.
// The boolean parameter should be true if a server GC is required and false for workstation. Returns true on
// success or false if a subsystem failed to initialize.

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

    HRESULT hr = GCHeapUtilities::InitializeGC();
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

Object* GcAllocInternal(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, Thread* pThread)
{
    ASSERT(!pThread->IsDoNotTriggerGcSet());
    ASSERT(pThread->IsCurrentThreadInCooperativeMode());

    size_t cbSize = pEEType->GetBaseSize();

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

    if ((cbSize >= RH_LARGE_OBJECT_SIZE) && (cbSize >= GCHeapUtilities::GetGCHeap()->GetLOHThreshold()))
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

void PromoteCarefully(PTR_PTR_Object obj, uint32_t flags, ScanFunc* fnGcEnumRef, ScanContext* pSc)
{
    //
    // Sanity check that the flags contain only these values
    //
    assert((flags & ~(GC_CALL_INTERIOR | GC_CALL_PINNED)) == 0);

    //
    // Sanity check that GC_CALL_INTERIOR FLAG is set
    //
    assert(flags & GC_CALL_INTERIOR);

    // If the object reference points into the stack, we
    // must not promote it, the GC cannot handle these.
    if (pSc->thread_under_crawl->IsWithinStackBounds(*obj))
        return;

    fnGcEnumRef(obj, pSc, flags);
}

// Scan a contiguous range of memory and report everything that looks like it could be a GC reference as a
// pinned interior reference. Pinned in case we are wrong (so the GC won't try to move the object and thus
// corrupt the original memory value by relocating it). Interior since we (a) can't easily tell whether a
// real reference is interior or not and interior is the more conservative choice that will work for both and
// (b) because it might not be a real GC reference at all and in that case falsely listing the reference as
// non-interior will cause the GC to make assumptions and crash quite quickly.
void GcEnumObjectsConservatively(PTR_PTR_Object ppLowerBound, PTR_PTR_Object ppUpperBound, ScanFunc* fnGcEnumRef, ScanContext* pSc)
{
    // Only report potential references in the promotion phase. Since we report everything as pinned there
    // should be no work to do in the relocation phase.
    if (pSc->promotion)
    {
        for (PTR_PTR_Object ppObj = ppLowerBound; ppObj < ppUpperBound; ppObj++)
        {
            // Only report values that lie in the GC heap range. This doesn't conclusively guarantee that the
            // value is a GC heap reference but it's a cheap check that weeds out a lot of spurious values.
            PTR_Object pObj = *ppObj;
            if (((PTR_UInt8)pObj >= g_lowest_address) && ((PTR_UInt8)pObj <= g_highest_address))
                PromoteCarefully(ppObj, GC_CALL_INTERIOR | GC_CALL_PINNED, fnGcEnumRef, pSc);
        }
    }
}

// static
void RedhawkGCInterface::EnumGcRefsInRegionConservatively(PTR_OBJECTREF pLowerBound,
                                                          PTR_OBJECTREF pUpperBound,
                                                          ScanFunc* pfnEnumCallback,
                                                          ScanContext* pvCallbackData)
{
    GcEnumObjectsConservatively(pLowerBound, pUpperBound, pfnEnumCallback, pvCallbackData);
}

void GcEnumObject(PTR_PTR_Object ppObj, uint32_t flags, ScanFunc* fnGcEnumRef, ScanContext* pSc)
{
    //
    // Sanity check that the flags contain only these values
    //
    assert((flags & ~(GC_CALL_INTERIOR | GC_CALL_PINNED)) == 0);

    // for interior pointers, we optimize the case in which
    //  it points into the current threads stack area
    //
    if (flags & GC_CALL_INTERIOR)
        PromoteCarefully(ppObj, flags, fnGcEnumRef, pSc);
    else
        fnGcEnumRef(ppObj, pSc, flags);
}

// static
void RedhawkGCInterface::EnumGcRef(PTR_OBJECTREF pRef, GCRefKind kind, ScanFunc* fnGcEnumRef, ScanContext* pSc)
{
    ASSERT((GCRK_Object == kind) || (GCRK_Byref == kind));

    DWORD flags = 0;

    if (kind == GCRK_Byref)
    {
        flags |= GC_CALL_INTERIOR;
    }

    GcEnumObject(pRef, flags, fnGcEnumRef, pSc);
}

// static
void RedhawkGCInterface::EnumGcRefConservatively(PTR_OBJECTREF pRef, ScanFunc* fnGcEnumRef, ScanContext* pSc)
{
    // Only report potential references in the promotion phase. Since we report everything as pinned there
    // should be no work to do in the relocation phase.
    if (pSc->promotion)
    {
        // Only report values that lie in the GC heap range. This doesn't conclusively guarantee that the
        // value is a GC heap reference but it's a cheap check that weeds out a lot of spurious values.
        PTR_Object pObj = *pRef;
        if (((PTR_UInt8)pObj >= g_lowest_address) && ((PTR_UInt8)pObj <= g_highest_address))
            PromoteCarefully(pRef, GC_CALL_INTERIOR | GC_CALL_PINNED, fnGcEnumRef, pSc);
    }
}

struct EnumGcRefContext : GCEnumContext
{
    ScanFunc* f;
    ScanContext* sc;
};

static void EnumGcRefsCallback(void* hCallback, PTR_PTR_VOID pObject, uint32_t flags)
{
    EnumGcRefContext* pCtx = (EnumGcRefContext*)hCallback;

    GcEnumObject((PTR_OBJECTREF)pObject, flags, pCtx->f, pCtx->sc);
}

// static
void RedhawkGCInterface::EnumGcRefs(ICodeManager* pCodeManager,
    MethodInfo* pMethodInfo,
    PTR_VOID safePointAddress,
    REGDISPLAY* pRegisterSet,
    ScanFunc* pfnEnumCallback,
    ScanContext* pvCallbackData,
    bool   isActiveStackFrame)
{
    EnumGcRefContext ctx;
    ctx.pCallback = EnumGcRefsCallback;
    ctx.f = pfnEnumCallback;
    ctx.sc = pvCallbackData;
    ctx.sc->stack_limit = pRegisterSet->GetSP();

    pCodeManager->EnumGcRefs(pMethodInfo,
        safePointAddress,
        pRegisterSet,
        &ctx,
        isActiveStackFrame);
}

// static
GcSegmentHandle RedhawkGCInterface::RegisterFrozenSegment(void * pSection, size_t allocSize, size_t commitSize, size_t reservedSize)
{
    ASSERT(allocSize <= commitSize);
    ASSERT(commitSize <= reservedSize);

#ifdef FEATURE_BASICFREEZE
    segment_info seginfo;

    seginfo.pvMem           = pSection;
    seginfo.ibFirstObject   = sizeof(ObjHeader);
    seginfo.ibAllocated     = allocSize;
    seginfo.ibCommit        = commitSize;
    seginfo.ibReserved      = reservedSize;

    return (GcSegmentHandle)GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&seginfo);
#else // FEATURE_BASICFREEZE
    return NULL;
#endif // FEATURE_BASICFREEZE
}

// static
void RedhawkGCInterface::UpdateFrozenSegment(GcSegmentHandle seg, uint8_t* allocated, uint8_t* committed)
{
    ASSERT(allocated <= committed);

    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment((segment_handle)seg, allocated, committed);
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
    ASSERT(pObj1->GetMethodTable() == pObj2->GetMethodTable());
    ASSERT(pObj1->GetMethodTable()->IsValueType());

    MethodTable * pEEType = pObj1->GetMethodTable();
    size_t cbFields = pEEType->GetBaseSize() - (sizeof(ObjHeader) + sizeof(MethodTable*));

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

#endif // !DACCESS_COMPILE
