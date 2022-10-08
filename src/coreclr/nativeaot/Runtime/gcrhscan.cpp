// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "gcenv.h"
#include "gcheaputilities.h"
#include "objecthandle.h"

#include "gcenv.ee.h"

#include "PalRedhawkCommon.h"

#include "gcrhinterface.h"

#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"

#include "thread.h"

#include "shash.h"
#include "RWLock.h"
#include "RuntimeInstance.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#ifndef DACCESS_COMPILE

void GcEnumObjectsConservatively(PTR_PTR_Object ppLowerBound, PTR_PTR_Object ppUpperBound, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc);

/*
 * Scan all stack and statics roots
 */

void GCToEEInterface::GcScanRoots(EnumGcRefCallbackFunc * fn,  int condemned, int max_gen, EnumGcRefScanContext * sc)
{
    // STRESS_LOG1(LF_GCROOTS, LL_INFO10, "GCScan: Phase = %s\n", sc->promotion ? "promote" : "relocate");

    FOREACH_THREAD(pThread)
    {
        // Skip "GC Special" threads which are really background workers that will never have any roots.
        if (pThread->IsGCSpecial())
            continue;

#if !defined (ISOLATED_HEAPS)
        if (!GCHeapUtilities::GetGCHeap()->IsThreadUsingAllocationContextHeap(pThread->GetAllocContext(),
                                                                     sc->thread_number))
        {
            // STRESS_LOG2(LF_GC|LF_GCROOTS, LL_INFO100, "{ Scan of Thread %p (ID = %x) declined by this heap\n",
            //             pThread, pThread->GetThreadId());
        }
        else
#endif
        {
            STRESS_LOG1(LF_GC|LF_GCROOTS, LL_INFO100, "{ Starting scan of Thread %p\n", pThread);
            sc->thread_under_crawl = pThread;
#if defined(FEATURE_EVENT_TRACE) && !defined(DACCESS_COMPILE)
            sc->dwEtwRootKind = kEtwGCRootKindStack;
#endif
            pThread->GcScanRoots(reinterpret_cast<void*>(fn), sc);

#if defined(FEATURE_EVENT_TRACE) && !defined(DACCESS_COMPILE)
            sc->dwEtwRootKind = kEtwGCRootKindOther;
#endif
            STRESS_LOG1(LF_GC|LF_GCROOTS, LL_INFO100, "Ending scan of Thread %p }\n", pThread);
        }
    }
    END_FOREACH_THREAD

    sc->thread_under_crawl = NULL;
}

void GCToEEInterface::GcEnumAllocContexts (enum_alloc_context_func* fn, void* param)
{
    FOREACH_THREAD(thread)
    {
        (*fn) (thread->GetAllocContext(), param);
    }
    END_FOREACH_THREAD
}

#endif //!DACCESS_COMPILE

void PromoteCarefully(PTR_PTR_Object obj, uint32_t flags, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc)
{
    //
    // Sanity check that the flags contain only these values
    //
    assert((flags & ~(GC_CALL_INTERIOR|GC_CALL_PINNED)) == 0);

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

void GcEnumObject(PTR_PTR_Object ppObj, uint32_t flags, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc)
{
    //
    // Sanity check that the flags contain only these values
    //
    assert((flags & ~(GC_CALL_INTERIOR|GC_CALL_PINNED)) == 0);

    // for interior pointers, we optimize the case in which
    //  it points into the current threads stack area
    //
    if (flags & GC_CALL_INTERIOR)
        PromoteCarefully (ppObj, flags, fnGcEnumRef, pSc);
    else
        fnGcEnumRef(ppObj, pSc, flags);
}

void GcBulkEnumObjects(PTR_PTR_Object pObjs, uint32_t cObjs, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc)
{
    PTR_PTR_Object ppObj = pObjs;

    for (uint32_t i = 0; i < cObjs; i++)
        fnGcEnumRef(ppObj++, pSc, 0);
}

// Scan a contiguous range of memory and report everything that looks like it could be a GC reference as a
// pinned interior reference. Pinned in case we are wrong (so the GC won't try to move the object and thus
// corrupt the original memory value by relocating it). Interior since we (a) can't easily tell whether a
// real reference is interior or not and interior is the more conservative choice that will work for both and
// (b) because it might not be a real GC reference at all and in that case falsely listing the reference as
// non-interior will cause the GC to make assumptions and crash quite quickly.
void GcEnumObjectsConservatively(PTR_PTR_Object ppLowerBound, PTR_PTR_Object ppUpperBound, EnumGcRefCallbackFunc * fnGcEnumRef, EnumGcRefScanContext * pSc)
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
                fnGcEnumRef(ppObj, pSc, GC_CALL_INTERIOR|GC_CALL_PINNED);
        }
    }
}
