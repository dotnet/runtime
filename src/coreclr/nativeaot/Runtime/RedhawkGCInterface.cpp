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
// static

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
