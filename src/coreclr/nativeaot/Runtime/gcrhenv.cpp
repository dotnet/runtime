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
#include "profheapwalkhelper.h"

#include "gcenv.ee.h"

#include "RestrictedCallouts.h"

#include "gcrhinterface.h"

#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"

#include "thread.h"

#include "shash.h"
#include "RWLock.h"
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

#include "GCMemoryHelpers.h"

#include "holder.h"
#include "volatile.h"

#ifdef FEATURE_ETW
    #ifndef _INC_WINDOWS
        typedef void* LPVOID;
        typedef uint32_t UINT;
        typedef void* PVOID;
        typedef uint64_t ULONGLONG;
        typedef uint32_t ULONG;
        typedef int64_t LONGLONG;
        typedef uint8_t BYTE;
        typedef uint16_t UINT16;
    #endif // _INC_WINDOWS

    #include "etwevents.h"
    #include "eventtrace.h"
#else // FEATURE_ETW
    #include "etmdummy.h"
    #define ETW_EVENT_ENABLED(e,f) false
#endif // FEATURE_ETW

GPTR_IMPL(MethodTable, g_pFreeObjectEEType);

#include "gctoclreventsink.h"

#ifndef DACCESS_COMPILE

bool RhInitializeFinalization();
bool RhStartFinalizerThread();
void RhEnableFinalization();

// A few settings are now backed by the cut-down version of Redhawk configuration values.
static RhConfig g_sRhConfig;
RhConfig * g_pRhConfig = &g_sRhConfig;

#ifdef FEATURE_ETW
//
// -----------------------------------------------------------------------------------------------------------
//
// The automatically generated part of the Redhawk ETW infrastructure (EtwEvents.h) calls the following
// function whenever the system enables or disables tracing for this provider.
//

uint32_t EtwCallback(uint32_t IsEnabled, RH_ETW_CONTEXT * pContext)
{
    GCHeapUtilities::RecordEventStateChange(!!(pContext->RegistrationHandle == Microsoft_Windows_Redhawk_GC_PublicHandle),
                                            static_cast<GCEventKeyword>(pContext->MatchAnyKeyword),
                                            static_cast<GCEventLevel>(pContext->Level));

    if (IsEnabled &&
        (pContext->RegistrationHandle == Microsoft_Windows_Redhawk_GC_PrivateHandle) &&
        GCHeapUtilities::IsGCHeapInitialized())
    {
        FireEtwGCSettings(GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(FALSE),
                          GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(TRUE),
                          GCHeapUtilities::IsServerHeap());
        GCHeapUtilities::GetGCHeap()->DiagTraceGCSegments();
    }

    // Special check for the runtime provider's GCHeapCollectKeyword.  Profilers
    // flick this to force a full GC.
    if (IsEnabled &&
        (pContext->RegistrationHandle == Microsoft_Windows_Redhawk_GC_PublicHandle) &&
        GCHeapUtilities::IsGCHeapInitialized() &&
        ((pContext->MatchAnyKeyword & CLR_GCHEAPCOLLECT_KEYWORD) != 0))
    {
        // Profilers may (optionally) specify extra data in the filter parameter
        // to log with the GCStart event.
        LONGLONG l64ClientSequenceNumber = 0;
        if ((pContext->FilterData != NULL) &&
            (pContext->FilterData->Type == 1) &&
            (pContext->FilterData->Size == sizeof(l64ClientSequenceNumber)))
        {
            l64ClientSequenceNumber = *(LONGLONG *) (pContext->FilterData->Ptr);
        }
        ETW::GCLog::ForceGC(l64ClientSequenceNumber);
    }

    return 0;
}
#endif // FEATURE_ETW

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
CrstStatic g_SuspendEELock;
#ifdef _MSC_VER
#pragma warning(disable:4815) // zero-sized array in stack object will have no elements
#endif // _MSC_VER
MethodTable g_FreeObjectEEType;

// static
bool RedhawkGCInterface::InitializeSubsystems()
{
#ifdef FEATURE_ETW
    MICROSOFT_WINDOWS_REDHAWK_GC_PRIVATE_PROVIDER_Context.IsEnabled = FALSE;
    MICROSOFT_WINDOWS_REDHAWK_GC_PUBLIC_PROVIDER_Context.IsEnabled = FALSE;

    // Register the Redhawk event provider with the system.
    RH_ETW_REGISTER_Microsoft_Windows_Redhawk_GC_Private();
    RH_ETW_REGISTER_Microsoft_Windows_Redhawk_GC_Public();

    MICROSOFT_WINDOWS_REDHAWK_GC_PRIVATE_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_Redhawk_GC_PrivateHandle;
    MICROSOFT_WINDOWS_REDHAWK_GC_PUBLIC_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_Redhawk_GC_PublicHandle;
#endif // FEATURE_ETW

    // Initialize the special MethodTable used to mark free list entries in the GC heap.
    g_FreeObjectEEType.InitializeAsGcFreeType();
    g_pFreeObjectEEType = &g_FreeObjectEEType;

    if (!g_SuspendEELock.InitNoThrow(CrstSuspendEE))
        return false;

#ifdef FEATURE_SVR_GC
    g_heap_type = (g_pRhConfig->GetgcServer() && PalGetProcessCpuCount() > 1) ? GC_HEAP_SVR : GC_HEAP_WKS;
#else
    g_heap_type = GC_HEAP_WKS;
#endif

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

    size_t cbSize = pEEType->get_BaseSize();

    if (pEEType->get_ComponentSize() != 0)
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
            uint64_t size64 = (uint64_t)cbSize + ((uint64_t)numElements * (uint64_t)pEEType->get_ComponentSize());
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
            cbSize = cbSize + ((size_t)numElements * (size_t)pEEType->get_ComponentSize());
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
    if (pEEType->get_ComponentSize() != 0)
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
//  pTransitionFrame-  transition frame to make stack crawable
// Returns a pointer to the object allocated or NULL on failure.

COOP_PINVOKE_HELPER(void*, RhpGcAlloc, (MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements, void* pTransitionFrame))
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    pThread->SetCurrentThreadPInvokeTunnelForGcAlloc(pTransitionFrame);

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
    m_usComponentSize = 1;
    m_usFlags = ParameterizedEEType;
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

bool IsOnReadablePortionOfThread(EnumGcRefScanContext * pSc, PTR_VOID pointer)
{
    if (!pSc->thread_under_crawl->IsWithinStackBounds(pointer))
    {
        return false;
    }

    // If the stack_limit is 0, then it wasn't set properly, and the check below will not
    // operate correctly.
    ASSERT(pSc->stack_limit != 0);

    // This ensures that the pointer is not in a currently-unused portion of the stack
    // because the above check is only verifying against the entire stack bounds,
    // but stack_limit is describing the current bound of the stack
    if (PTR_TO_TADDR(pointer) < pSc->stack_limit)
    {
        return false;
    }
    return true;
}

#ifdef HOST_64BIT
#define CONSERVATIVE_REGION_MAGIC_NUMBER 0x87DF7A104F09E0A9ULL
#else
#define CONSERVATIVE_REGION_MAGIC_NUMBER 0x4F09E0A9
#endif

// This is a structure that is created by executing runtime code in order to report a conservative
// region. In managed code if there is a pinned byref pointer to one of this (with the appropriate
// magic number set in it, and a hash that matches up) then the region from regionPointerLow to
// regionPointerHigh will be reported conservatively. This can only be used to report memory regions
// on the current stack and the structure must itself be located on the stack.
struct ConservativelyReportedRegionDesc
{
    // If this is really a ConservativelyReportedRegionDesc then the magic value will be
    // CONSERVATIVE_REGION_MAGIC_NUMBER, and the hash will be the result of CalculateHash
    // across magic, regionPointerLow, and regionPointerHigh
    uintptr_t magic;
    PTR_VOID regionPointerLow;
    PTR_VOID regionPointerHigh;
    uintptr_t hash;

    static uintptr_t CalculateHash(uintptr_t h1, uintptr_t h2, uintptr_t h3)
    {
        uintptr_t hash = h1;
        hash = ((hash << 13) ^ hash) ^ h2;
        hash = ((hash << 13) ^ hash) ^ h3;
        return hash;
    }
};

typedef DPTR(ConservativelyReportedRegionDesc) PTR_ConservativelyReportedRegionDesc;

bool IsPtrAligned(TADDR value)
{
    return (value & (POINTER_SIZE - 1)) == 0;
}

// Logic to actually conservatively report a ConservativelyReportedRegionDesc
// This logic is to be used when attempting to promote a pinned, interior pointer.
// It will attempt to heuristically identify ConservativelyReportedRegionDesc structures
// and if they exist, it will conservatively report a memory region.
static void ReportExplicitConservativeReportedRegionIfValid(EnumGcRefContext * pCtx, PTR_PTR_VOID pObject)
{
    // If the stack_limit isn't set (which can only happen for frames which make a p/invoke call
    // there cannot be a ConservativelyReportedRegionDesc
    if (pCtx->sc->stack_limit == 0)
        return;

    PTR_ConservativelyReportedRegionDesc conservativeRegionDesc = (PTR_ConservativelyReportedRegionDesc)(*pObject);

    // Ensure that conservativeRegionDesc pointer points at a readable memory region
    if (!IsPtrAligned(PTR_TO_TADDR(conservativeRegionDesc)))
    {
        return;
    }

    if (!IsOnReadablePortionOfThread(pCtx->sc, conservativeRegionDesc))
    {
        return;
    }
    if (!IsOnReadablePortionOfThread(pCtx->sc, conservativeRegionDesc + 1))
    {
        return;
    }

    // Now, check to see if what we're pointing at is actually a ConservativeRegionDesc
    // First: check the magic number. If that doesn't match, it cannot be one
    if (conservativeRegionDesc->magic != CONSERVATIVE_REGION_MAGIC_NUMBER)
    {
        return;
    }

    // Second: check to see that the region pointers point at memory which is aligned
    // such that the pointers could be pointers to object references
    if (!IsPtrAligned(PTR_TO_TADDR(conservativeRegionDesc->regionPointerLow)))
    {
        return;
    }
    if (!IsPtrAligned(PTR_TO_TADDR(conservativeRegionDesc->regionPointerHigh)))
    {
        return;
    }

    // Third: check that start is before end.
    if (conservativeRegionDesc->regionPointerLow >= conservativeRegionDesc->regionPointerHigh)
    {
        return;
    }

#ifndef DACCESS_COMPILE
    // This fails for cross-bitness dac compiles and isn't really needed in the DAC anyways.

    // Fourth: Compute a hash of the above numbers. Check to see that the hash matches the hash
    // value stored
    if (ConservativelyReportedRegionDesc::CalculateHash(CONSERVATIVE_REGION_MAGIC_NUMBER,
                                                        (uintptr_t)PTR_TO_TADDR(conservativeRegionDesc->regionPointerLow),
                                                        (uintptr_t)PTR_TO_TADDR(conservativeRegionDesc->regionPointerHigh))
        != conservativeRegionDesc->hash)
    {
        return;
    }
#endif // DACCESS_COMPILE

    // Fifth: Check to see that the region pointed at is within the bounds of the thread
    if (!IsOnReadablePortionOfThread(pCtx->sc, conservativeRegionDesc->regionPointerLow))
    {
        return;
    }
    if (!IsOnReadablePortionOfThread(pCtx->sc, ((PTR_OBJECTREF)conservativeRegionDesc->regionPointerHigh) - 1))
    {
        return;
    }

    // At this point we're most likely working with a ConservativeRegionDesc. We'll assume
    // that's true, and perform conservative reporting. (We've done enough checks to ensure that
    // this conservative reporting won't itself cause an AV, even if our heuristics are wrong
    // with the second and fifth set of checks)
    GcEnumObjectsConservatively((PTR_OBJECTREF)conservativeRegionDesc->regionPointerLow, (PTR_OBJECTREF)conservativeRegionDesc->regionPointerHigh, pCtx->f, pCtx->sc);
}

static void EnumGcRefsCallback(void * hCallback, PTR_PTR_VOID pObject, uint32_t flags)
{
    EnumGcRefContext * pCtx = (EnumGcRefContext *)hCallback;

    GcEnumObject((PTR_OBJECTREF)pObject, flags, pCtx->f, pCtx->sc);

    const uint32_t interiorPinned = GC_CALL_INTERIOR | GC_CALL_PINNED;
    // If this is an interior pinned pointer, check to see if we're working with a ConservativeRegionDesc
    // and if so, report a conservative region. NOTE: do this only during promotion as conservative
    // reporting has no value during other GC phases.
    if (((flags & interiorPinned) == interiorPinned) && (pCtx->sc->promotion))
    {
        ReportExplicitConservativeReportedRegionIfValid(pCtx, pObject);
    }
}

// static
void RedhawkGCInterface::EnumGcRefs(ICodeManager * pCodeManager,
                                    MethodInfo * pMethodInfo,
                                    PTR_VOID safePointAddress,
                                    REGDISPLAY * pRegisterSet,
                                    void * pfnEnumCallback,
                                    void * pvCallbackData)
{
    EnumGcRefContext ctx;
    ctx.pCallback = EnumGcRefsCallback;
    ctx.f  = (EnumGcRefCallbackFunc *)pfnEnumCallback;
    ctx.sc = (EnumGcRefScanContext *)pvCallbackData;
    ctx.sc->stack_limit = pRegisterSet->GetSP();

    pCodeManager->EnumGcRefs(pMethodInfo,
                             safePointAddress,
                             pRegisterSet,
                             &ctx);
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

EXTERN_C UInt32_BOOL g_fGcStressStarted = UInt32_FALSE; // UInt32_BOOL because asm code reads it
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
#ifndef DACCESS_COMPILE
    ScanRootsContext sContext;
    sContext.m_pfnCallback = pfnScanCallback;
    sContext.m_pContext = pContext;

    GetRuntimeInstance()->EnumAllStaticGCRefs(reinterpret_cast<void*>(ScanRootsCallbackWrapper), &sContext);
#else
    UNREFERENCED_PARAMETER(pfnScanCallback);
    UNREFERENCED_PARAMETER(pContext);
#endif // !DACCESS_COMPILE
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

COOP_PINVOKE_HELPER(void, RhpCopyObjectContents, (Object* pobjDest, Object* pobjSrc))
{
    size_t cbDest = pobjDest->GetSize() - sizeof(ObjHeader);
    size_t cbSrc = pobjSrc->GetSize() - sizeof(ObjHeader);
    if (cbSrc != cbDest)
        return;

    ASSERT(pobjDest->get_EEType()->HasReferenceFields() == pobjSrc->get_EEType()->HasReferenceFields());

    if (pobjDest->get_EEType()->HasReferenceFields())
    {
        GCSafeCopyMemoryWithWriteBarrier(pobjDest, pobjSrc, cbDest);
    }
    else
    {
        memcpy(pobjDest, pobjSrc, cbDest);
    }
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhCompareObjectContentsAndPadding, (Object* pObj1, Object* pObj2))
{
    ASSERT(pObj1->get_EEType()->IsEquivalentTo(pObj2->get_EEType()));
    MethodTable * pEEType = pObj1->get_EEType();
    size_t cbFields = pEEType->get_BaseSize() - (sizeof(ObjHeader) + sizeof(MethodTable*));

    uint8_t * pbFields1 = (uint8_t*)pObj1 + sizeof(MethodTable*);
    uint8_t * pbFields2 = (uint8_t*)pObj2 + sizeof(MethodTable*);

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

    g_SuspendEELock.Enter();

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

    g_SuspendEELock.Leave();

    FireEtwGCRestartEEEnd_V1(GetClrInstanceId());
}

void GCToEEInterface::GcStartWork(int condemned, int /*max_gen*/)
{
    // Invoke any registered callouts for the start of the collection.
    RestrictedCallouts::InvokeGcCallouts(GCRC_StartCollection, condemned);
}

void GCToEEInterface::BeforeGcScanRoots(int condemned, bool is_bgc, bool is_concurrent)
{
}

// EE can perform post stack scanning action, while the user threads are still suspended
void GCToEEInterface::AfterGcScanRoots(int condemned, int /*max_gen*/, ScanContext* /*sc*/)
{
    // Invoke any registered callouts for the end of the mark phase.
    RestrictedCallouts::InvokeGcCallouts(GCRC_AfterMarkPhase, condemned);
}

void GCToEEInterface::GcDone(int condemned)
{
    // Invoke any registered callouts for the end of the collection.
    RestrictedCallouts::InvokeGcCallouts(GCRC_EndCollection, condemned);
}

bool GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
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
    return ThreadStore::GetCurrentThread();
#else
    return NULL;
#endif
}

#ifndef DACCESS_COMPILE

#ifdef FEATURE_EVENT_TRACE
void ProfScanRootsHelper(Object** ppObject, ScanContext* pSC, uint32_t dwFlags)
{
    Object* pObj = *ppObject;
    if (dwFlags& GC_CALL_INTERIOR)
    {
        pObj = GCHeapUtilities::GetGCHeap()->GetContainingObject(pObj, true);
        if (pObj == nullptr)
            return;
    }
    ScanRootsHelper(pObj, ppObject, pSC, dwFlags);
}

void GcScanRootsForETW(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    UNREFERENCED_PARAMETER(condemned);
    UNREFERENCED_PARAMETER(max_gen);

    FOREACH_THREAD(pThread)
    {
        if (pThread->IsGCSpecial())
            continue;

        if (GCHeapUtilities::GetGCHeap()->IsThreadUsingAllocationContextHeap(pThread->GetAllocContext(), sc->thread_number))
            continue;

        sc->thread_under_crawl = pThread;
        sc->dwEtwRootKind = kEtwGCRootKindStack;
        pThread->GcScanRoots(reinterpret_cast<void*>(fn), sc);
        sc->dwEtwRootKind = kEtwGCRootKindOther;
    }
    END_FOREACH_THREAD
}

void ScanHandleForETW(Object** pRef, Object* pSec, uint32_t flags, ScanContext* context, bool isDependent)
{
    ProfilingScanContext* pSC = (ProfilingScanContext*)context;

    // Notify ETW of the handle
    if (ETW::GCLog::ShouldWalkHeapRootsForEtw())
    {
        ETW::GCLog::RootReference(
            pRef,
            *pRef,          // object being rooted
            pSec,           // pSecondaryNodeForDependentHandle
            isDependent,
            pSC,
            0,              // dwGCFlags,
            flags);     // ETW handle flags
    }
}

// This is called only if we've determined that either:
//     a) The Profiling API wants to do a walk of the heap, and it has pinned the
//     profiler in place (so it cannot be detached), and it's thus safe to call into the
//     profiler, OR
//     b) ETW infrastructure wants to do a walk of the heap either to log roots,
//     objects, or both.
// This can also be called to do a single walk for BOTH a) and b) simultaneously.  Since
// ETW can ask for roots, but not objects
void GCProfileWalkHeapWorker(BOOL fShouldWalkHeapRootsForEtw, BOOL fShouldWalkHeapObjectsForEtw)
{
    ProfilingScanContext SC(FALSE);
    unsigned max_generation = GCHeapUtilities::GetGCHeap()->GetMaxGeneration();

    // **** Scan roots:  Only scan roots if profiling API wants them or ETW wants them.
    if (fShouldWalkHeapRootsForEtw)
    {
        GcScanRootsForETW(&ProfScanRootsHelper, max_generation, max_generation, &SC);
        SC.dwEtwRootKind = kEtwGCRootKindFinalizer;
        GCHeapUtilities::GetGCHeap()->DiagScanFinalizeQueue(&ProfScanRootsHelper, &SC);

        // Handles are kept independent of wks/svr/concurrent builds
        SC.dwEtwRootKind = kEtwGCRootKindHandle;
        GCHeapUtilities::GetGCHeap()->DiagScanHandles(&ScanHandleForETW, max_generation, &SC);
    }

    // **** Scan dependent handles: only if ETW wants roots
    if (fShouldWalkHeapRootsForEtw)
    {
        // GcScanDependentHandlesForProfiler double-checks
        // CORProfilerTrackConditionalWeakTableElements() before calling into the profiler

        ProfilingScanContext* pSC = &SC;

        // we'll re-use pHeapId (which was either unused (0) or freed by EndRootReferences2
        // (-1)), so reset it to NULL
        _ASSERTE((*((size_t *)(&pSC->pHeapId)) == (size_t)(-1)) ||
                (*((size_t *)(&pSC->pHeapId)) == (size_t)(0)));
        pSC->pHeapId = NULL;

        GCHeapUtilities::GetGCHeap()->DiagScanDependentHandles(&ScanHandleForETW, max_generation, &SC);
    }

    ProfilerWalkHeapContext profilerWalkHeapContext(FALSE, SC.pvEtwContext);

    // **** Walk objects on heap: only if ETW wants them.
    if (fShouldWalkHeapObjectsForEtw)
    {
        GCHeapUtilities::GetGCHeap()->DiagWalkHeap(&HeapWalkHelper, &profilerWalkHeapContext, max_generation, true /* walk the large object heap */);
    }

    #ifdef FEATURE_EVENT_TRACE
    // **** Done! Indicate to ETW helpers that the heap walk is done, so any buffers
    // should be flushed into the ETW stream
    if (fShouldWalkHeapObjectsForEtw || fShouldWalkHeapRootsForEtw)
    {
        ETW::GCLog::EndHeapDump(&profilerWalkHeapContext);
    }
#endif // FEATURE_EVENT_TRACE
}
#endif // defined(FEATURE_EVENT_TRACE)

void GCProfileWalkHeap()
{

#ifdef FEATURE_EVENT_TRACE
    if (ETW::GCLog::ShouldWalkStaticsAndCOMForEtw())
        ETW::GCLog::WalkStaticsAndCOMForETW();

    BOOL fShouldWalkHeapRootsForEtw = ETW::GCLog::ShouldWalkHeapRootsForEtw();
    BOOL fShouldWalkHeapObjectsForEtw = ETW::GCLog::ShouldWalkHeapObjectsForEtw();
#else // !FEATURE_EVENT_TRACE
    BOOL fShouldWalkHeapRootsForEtw = FALSE;
    BOOL fShouldWalkHeapObjectsForEtw = FALSE;
#endif // FEATURE_EVENT_TRACE

#ifdef FEATURE_EVENT_TRACE
    // we need to walk the heap if one of GC_PROFILING or FEATURE_EVENT_TRACE
    // is defined, since both of them make use of the walk heap worker.
    if (fShouldWalkHeapRootsForEtw || fShouldWalkHeapObjectsForEtw)
    {
        GCProfileWalkHeapWorker(fShouldWalkHeapRootsForEtw, fShouldWalkHeapObjectsForEtw);
    }
#endif // defined(FEATURE_EVENT_TRACE)
}


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

    if (!fConcurrent)
    {
        GCProfileWalkHeap();
    }
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

#if defined(FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP) && (!defined(TARGET_ARM64) || !defined(TARGET_UNIX))
#error FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP is only implemented for ARM64 and UNIX
#endif

void GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
    // CoreRT doesn't patch the write barrier like CoreCLR does, but it
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

void GCToEEInterface::EnableFinalization(bool foundFinalizers)
{
    if (foundFinalizers)
        RhEnableFinalization();
}

void GCToEEInterface::HandleFatalError(unsigned int exitCode)
{
    UNREFERENCED_PARAMETER(exitCode);
    EEPOLICY_HANDLE_FATAL_ERROR(exitCode);
}

bool GCToEEInterface::EagerFinalized(Object* obj)
{
    UNREFERENCED_PARAMETER(obj);
    return false;
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

        ThreadStore::RawGetCurrentThread()->SetGCSpecial(true);

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

// CoreRT does not use async pinned handles
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
    // these configuration values are given to us via startup flags.
    if (strcmp(privateKey, "gcServer") == 0)
    {
        *value = g_heap_type == GC_HEAP_SVR;
        return true;
    }

    if (strcmp(privateKey, "gcConservative") == 0)
    {
        *value = true;
        return true;
    }

#ifdef UNICODE
    size_t keyLength = strlen(privateKey) + 1;
    TCHAR* pKey = (TCHAR*)_alloca(sizeof(TCHAR) * keyLength);
    for (size_t i = 0; i < keyLength; i++)
        pKey[i] = privateKey[i];
#else
    const TCHAR* pKey = privateKey;
#endif

    uint32_t uiValue;
    if (!g_pRhConfig->ReadConfigValue(pKey, &uiValue))
        return false;

    *value = uiValue != 0;
    return true;
}

bool GCToEEInterface::GetIntConfigValue(const char* privateKey, const char* publicKey, int64_t* value)
{
#ifdef UNICODE
    size_t keyLength = strlen(privateKey) + 1;
    TCHAR* pKey = (TCHAR*)_alloca(sizeof(TCHAR) * keyLength);
    for (size_t i = 0; i < keyLength; i++)
        pKey[i] = privateKey[i];
#else
    const TCHAR* pKey = privateKey;
#endif

    uint32_t uiValue;
    if (!g_pRhConfig->ReadConfigValue(pKey, &uiValue))
        return false;

    *value = uiValue;
    return true;
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

#if defined(FEATURE_EVENT_TRACE) && !defined(DACCESS_COMPILE)
ProfilingScanContext::ProfilingScanContext(BOOL fProfilerPinnedParam)
    : ScanContext()
{
    pHeapId = NULL;
    fProfilerPinned = fProfilerPinnedParam;
    pvEtwContext = NULL;
    promotion = true;
}
#endif // defined(FEATURE_EVENT_TRACE) && !defined(DACCESS_COMPILE)
