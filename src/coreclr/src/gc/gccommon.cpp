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

IGCHeapInternal* g_theGCHeap;
IGCHandleManager* g_theGCHandleManager;

#ifdef BUILD_AS_STANDALONE
IGCToCLR* g_theGCToCLR;
#endif // BUILD_AS_STANDALONE

#ifdef GC_CONFIG_DRIVEN
size_t gc_global_mechanisms[MAX_GLOBAL_GC_MECHANISMS_COUNT];
#endif //GC_CONFIG_DRIVEN

#ifndef DACCESS_COMPILE

#ifdef WRITE_BARRIER_CHECK
uint8_t* g_GCShadow;
uint8_t* g_GCShadowEnd;
uint8_t* g_shadow_lowest_address = NULL;
#endif

uint32_t* g_gc_card_table;

VOLATILE(int32_t) g_fSuspensionPending = 0;

uint32_t g_yieldProcessorScalingFactor = 1;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
uint32_t* g_gc_card_bundle_table;
#endif

uint8_t* g_gc_lowest_address  = 0;
uint8_t* g_gc_highest_address = 0;
GCHeapType g_gc_heap_type = GC_HEAP_INVALID;
uint32_t g_max_generation = max_generation;
MethodTable* g_gc_pFreeObjectMethodTable = nullptr;
uint32_t g_num_processors = 0;

#ifdef GC_CONFIG_DRIVEN
void record_global_mechanism (int mech_index)
{
    (gc_global_mechanisms[mech_index])++;
}
#endif //GC_CONFIG_DRIVEN

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

#endif // !DACCESS_COMPILE
