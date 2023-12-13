// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "gcenv.h"
#include "gcheaputilities.h"
#include "objecthandle.h"

#include "gcenv.ee.h"

#include "PalRedhawkCommon.h"

#include "RedhawkGCInterface.h"

#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"

#include "thread.h"

#include "shash.h"
#include "RuntimeInstance.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#ifndef DACCESS_COMPILE

void GcEnumObject(PTR_PTR_Object ppObj, uint32_t flags, EnumGcRefCallbackFunc* fnGcEnumRef, EnumGcRefScanContext* pSc);

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
            InlinedThreadStaticRoot* pRoot = pThread->GetInlinedThreadStaticList();
            while (pRoot != NULL)
            {
                STRESS_LOG2(LF_GC | LF_GCROOTS, LL_INFO100, "{ Scanning Thread's %p inline thread statics root %p. \n", pThread, pRoot);
                GcEnumObject(&pRoot->m_threadStaticsBase, 0 /*flags*/, fn, sc);
                pRoot = pRoot->m_next;
            }

            STRESS_LOG1(LF_GC | LF_GCROOTS, LL_INFO100, "{ Scanning Thread's %p thread statics root. \n", pThread);
            GcEnumObject(pThread->GetThreadStaticStorage(), 0 /*flags*/, fn, sc);

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
