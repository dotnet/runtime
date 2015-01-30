//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


/*
 * GCCOMMON.CPP 
 *
 * Code common to both SVR and WKS gcs
 */

#include "common.h"

#include "gcenv.h"
#include "gc.h"

#ifdef FEATURE_SVR_GC
SVAL_IMPL_INIT(DWORD,GCHeap,gcHeapType,GCHeap::GC_HEAP_INVALID);
#endif // FEATURE_SVR_GC

GPTR_IMPL(GCHeap,g_pGCHeap);

/* global versions of the card table and brick table */ 
GPTR_IMPL(DWORD,g_card_table);

/* absolute bounds of the GC memory */
GPTR_IMPL_INIT(BYTE,g_lowest_address,0);
GPTR_IMPL_INIT(BYTE,g_highest_address,0);

#ifndef DACCESS_COMPILE

BYTE* g_ephemeral_low = (BYTE*)1; 
BYTE* g_ephemeral_high = (BYTE*)~0;

#ifdef WRITE_BARRIER_CHECK
BYTE* g_GCShadow;
BYTE* g_GCShadowEnd;
BYTE* g_shadow_lowest_address = NULL;
#endif

VOLATILE(LONG) m_GCLock = -1;

LONG g_bLowMemoryFromHost = 0;

#ifdef WRITE_BARRIER_CHECK

#define INVALIDGCVALUE (LPVOID)((size_t)0xcccccccd)

    // called by the write barrier to update the shadow heap
void updateGCShadow(Object** ptr, Object* val)
{
    Object** shadow = (Object**) &g_GCShadow[((BYTE*) ptr - g_lowest_address)];
    if ((BYTE*) shadow < g_GCShadowEnd)
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
    BYTE              * start;
    BYTE              * end;
    size_t              gc_index;
    bgc_state           bgc;
    changed_seg_state   changed;
};


const int max_saved_changed_segs = 128;

changed_seg saved_changed_segs[max_saved_changed_segs];
int saved_changed_segs_count = 0;

void record_changed_seg (BYTE* start, BYTE* end, 
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
