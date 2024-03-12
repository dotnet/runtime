// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"

#include "GcEnum.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "regdisplay.h"

static void PromoteCarefully(PTR_PTR_Object obj, uint32_t flags, ScanFunc* fnGcEnumRef, ScanContext* pSc)
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
static void GcEnumObjectsConservatively(PTR_PTR_Object ppLowerBound, PTR_PTR_Object ppUpperBound, ScanFunc* fnGcEnumRef, ScanContext* pSc)
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
            if (((PTR_uint8_t)pObj >= g_lowest_address) && ((PTR_uint8_t)pObj <= g_highest_address))
                PromoteCarefully(ppObj, GC_CALL_INTERIOR | GC_CALL_PINNED, fnGcEnumRef, pSc);
        }
    }
}

void EnumGcRefsInRegionConservatively(PTR_OBJECTREF pLowerBound,
                                      PTR_OBJECTREF pUpperBound,
                                      ScanFunc* pfnEnumCallback,
                                      ScanContext* pvCallbackData)
{
    GcEnumObjectsConservatively(pLowerBound, pUpperBound, pfnEnumCallback, pvCallbackData);
}

static void GcEnumObject(PTR_PTR_Object ppObj, uint32_t flags, ScanFunc* fnGcEnumRef, ScanContext* pSc)
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

void EnumGcRef(PTR_OBJECTREF pRef, GCRefKind kind, ScanFunc* fnGcEnumRef, ScanContext* pSc)
{
    ASSERT((GCRK_Object == kind) || (GCRK_Byref == kind));

    DWORD flags = 0;

    if (kind == GCRK_Byref)
    {
        flags |= GC_CALL_INTERIOR;
    }

    GcEnumObject(pRef, flags, fnGcEnumRef, pSc);
}

void EnumGcRefConservatively(PTR_OBJECTREF pRef, ScanFunc* fnGcEnumRef, ScanContext* pSc)
{
    // Only report potential references in the promotion phase. Since we report everything as pinned there
    // should be no work to do in the relocation phase.
    if (pSc->promotion)
    {
        // Only report values that lie in the GC heap range. This doesn't conclusively guarantee that the
        // value is a GC heap reference but it's a cheap check that weeds out a lot of spurious values.
        PTR_Object pObj = *pRef;
        if (((PTR_uint8_t)pObj >= g_lowest_address) && ((PTR_uint8_t)pObj <= g_highest_address))
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

void EnumGcRefs(ICodeManager* pCodeManager,
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
