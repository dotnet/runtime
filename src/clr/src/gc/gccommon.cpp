// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*
 * GCCOMMON.CPP 
 *
 * Code common to both SVR and WKS gcs
 */

#include "common.h"

#include "gcenv.h"
#include "gc.h"

#ifdef FEATURE_SVR_GC
SVAL_IMPL_INIT(uint32_t,IGCHeap,gcHeapType,IGCHeap::GC_HEAP_INVALID);
#endif // FEATURE_SVR_GC

SVAL_IMPL_INIT(uint32_t,IGCHeap,maxGeneration,2);

IGCHeapInternal* g_theGCHeap;

#ifdef FEATURE_STANDALONE_GC
IGCToCLR* g_theGCToCLR;
#endif // FEATURE_STANDALONE_GC

#ifdef GC_CONFIG_DRIVEN
GARY_IMPL(size_t, gc_global_mechanisms, MAX_GLOBAL_GC_MECHANISMS_COUNT);
#endif //GC_CONFIG_DRIVEN

#ifndef DACCESS_COMPILE

#ifdef WRITE_BARRIER_CHECK
uint8_t* g_GCShadow;
uint8_t* g_GCShadowEnd;
uint8_t* g_shadow_lowest_address = NULL;
#endif

uint32_t* g_gc_card_table;
uint8_t* g_gc_lowest_address  = 0;
uint8_t* g_gc_highest_address = 0;

VOLATILE(int32_t) m_GCLock = -1;

#ifdef GC_CONFIG_DRIVEN
void record_global_mechanism (int mech_index)
{
	(gc_global_mechanisms[mech_index])++;
}
#endif //GC_CONFIG_DRIVEN

int32_t g_bLowMemoryFromHost = 0;

#ifdef WRITE_BARRIER_CHECK

#define INVALIDGCVALUE (void *)((size_t)0xcccccccd)

    // called by the write barrier to update the shadow heap
void updateGCShadow(Object** ptr, Object* val)
{
    Object** shadow = (Object**) &g_GCShadow[((uint8_t*) ptr - g_lowest_address)];
    if ((uint8_t*) shadow < g_GCShadowEnd)
    {
        *shadow = val;

        // Ensure that the write to the shadow heap occurs before the read from
        // the GC heap so that race conditions are caught by INVALIDGCVALUE.
        MemoryBarrier();

        if(*ptr!=val)
            *shadow = (Object *) INVALIDGCVALUE;
    }
}

#endif // WRITE_BARRIER_CHECK


struct changed_seg
{
    uint8_t           * start;
    uint8_t           * end;
    size_t              gc_index;
    bgc_state           bgc;
    changed_seg_state   changed;
};


const int max_saved_changed_segs = 128;

changed_seg saved_changed_segs[max_saved_changed_segs];
int saved_changed_segs_count = 0;

void record_changed_seg (uint8_t* start, uint8_t* end,
                         size_t current_gc_index,
                         bgc_state current_bgc_state,
                         changed_seg_state changed_state)
{
    if (saved_changed_segs_count < max_saved_changed_segs)
    {
        saved_changed_segs[saved_changed_segs_count].start = start;
        saved_changed_segs[saved_changed_segs_count].end = end;
        saved_changed_segs[saved_changed_segs_count].gc_index = current_gc_index;
        saved_changed_segs[saved_changed_segs_count].bgc = current_bgc_state;
        saved_changed_segs[saved_changed_segs_count].changed = changed_state;
        saved_changed_segs_count++;
    }
    else
    {
        saved_changed_segs_count = 0;
    }
}

// The runtime needs to know whether we're using workstation or server GC 
// long before the GCHeap is created.
void InitializeHeapType(bool bServerHeap)
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_SVR_GC
    IGCHeap::gcHeapType = bServerHeap ? IGCHeap::GC_HEAP_SVR : IGCHeap::GC_HEAP_WKS;
#ifdef WRITE_BARRIER_CHECK
    if (IGCHeap::gcHeapType == IGCHeap::GC_HEAP_SVR)
    {
        g_GCShadow = 0;
        g_GCShadowEnd = 0;
    }
#endif // WRITE_BARRIER_CHECK
#else // FEATURE_SVR_GC
    UNREFERENCED_PARAMETER(bServerHeap);
    CONSISTENCY_CHECK(bServerHeap == false);
#endif // FEATURE_SVR_GC
}

IGCHeap* InitializeGarbageCollector(IGCToCLR* clrToGC)
{
    LIMITED_METHOD_CONTRACT;

    IGCHeapInternal* heap;
#ifdef FEATURE_SVR_GC
    assert(IGCHeap::gcHeapType != IGCHeap::GC_HEAP_INVALID);
    heap = IGCHeap::gcHeapType == IGCHeap::GC_HEAP_SVR ? SVR::CreateGCHeap() : WKS::CreateGCHeap();
#else
    heap = WKS::CreateGCHeap();
#endif

    g_theGCHeap = heap;

#ifdef FEATURE_STANDALONE_GC
    assert(clrToGC != nullptr);
    g_theGCToCLR = clrToGC;
#else
    UNREFERENCED_PARAMETER(clrToGC);
    assert(clrToGC == nullptr);
#endif

    return heap;
}

#endif // !DACCESS_COMPILE
