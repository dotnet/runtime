// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


// sets up vars for GC

#include "gcpriv.h"

#ifndef DACCESS_COMPILE

uint64_t g_TotalTimeInGC = 0;
uint64_t g_TotalTimeSinceLastGCEnd = 0;

uint32_t g_percentTimeInGCSinceLastGC = 0;

size_t g_GenerationSizes[NUMBERGENERATIONS];
size_t g_GenerationPromotedSizes[NUMBERGENERATIONS];

void GCHeap::UpdatePreGCCounters()
{
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = 0;
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    size_t allocation_0 = 0;
    size_t allocation_3 = 0; 
    
    // Publish perf stats
    g_TotalTimeInGC = GCToOSInterface::QueryPerformanceCounter();

#ifdef MULTIPLE_HEAPS
    int hn = 0;
    for (hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        hp = gc_heap::g_heaps [hn];
            
        allocation_0 += 
            dd_desired_allocation (hp->dynamic_data_of (0))-
            dd_new_allocation (hp->dynamic_data_of (0));
        allocation_3 += 
            dd_desired_allocation (hp->dynamic_data_of (max_generation+1))-
            dd_new_allocation (hp->dynamic_data_of (max_generation+1));
    }
#else
    allocation_0 = 
        dd_desired_allocation (hp->dynamic_data_of (0))-
        dd_new_allocation (hp->dynamic_data_of (0));
    allocation_3 = 
        dd_desired_allocation (hp->dynamic_data_of (max_generation+1))-
        dd_new_allocation (hp->dynamic_data_of (max_generation+1));
        
#endif //MULTIPLE_HEAPS

#ifdef MULTIPLE_HEAPS
        //take the first heap....
    gc_mechanisms *pSettings = &gc_heap::g_heaps[0]->settings;
#else
    gc_mechanisms *pSettings = &gc_heap::settings;
#endif //MULTIPLE_HEAPS

    uint32_t count = (uint32_t)pSettings->gc_index;
    uint32_t depth = (uint32_t)pSettings->condemned_generation;
    uint32_t reason = (uint32_t)pSettings->reason;
    gc_etw_type type = gc_etw_type_ngc;
    if (pSettings->concurrent)
    {
        type = gc_etw_type_bgc;
    }
#ifdef BACKGROUND_GC
    else if (depth < max_generation && pSettings->background_p)
    {
        type = gc_etw_type_fgc;
    }
#endif // BACKGROUND_GC

    FIRE_EVENT(GCStart_V2, count, depth, reason, static_cast<uint32_t>(type));
    g_theGCHeap->DiagDescrGenerations([](void*, int generation, uint8_t* rangeStart, uint8_t* rangeEnd, uint8_t* rangeEndReserved)
    {
        uint64_t range = static_cast<uint64_t>(rangeEnd - rangeStart);
        uint64_t rangeReserved = static_cast<uint64_t>(rangeEndReserved - rangeStart);
        FIRE_EVENT(GCGenerationRange, generation, rangeStart, range, rangeReserved);
    }, nullptr);
}

void GCHeap::UpdatePostGCCounters()
{
    totalSurvivedSize = gc_heap::get_total_survived_size();

    //
    // The following is for instrumentation.
    //
    // Calculate the common ones for ETW and perf counters.
#if defined(FEATURE_EVENT_TRACE)
#ifdef MULTIPLE_HEAPS
    //take the first heap....
    gc_heap* hp1 = gc_heap::g_heaps[0];
    gc_mechanisms *pSettings = &hp1->settings;
#else
    gc_heap* hp1 = pGenGCHeap;
    gc_mechanisms *pSettings = &gc_heap::settings;
#endif //MULTIPLE_HEAPS

    int condemned_gen = pSettings->condemned_generation;

    memset (g_GenerationSizes, 0, sizeof (g_GenerationSizes));
    memset (g_GenerationPromotedSizes, 0, sizeof (g_GenerationPromotedSizes));
    
    size_t total_num_gc_handles = g_dwHandles;
    uint32_t total_num_sync_blocks = GCToEEInterface::GetActiveSyncBlockCount();

    // Note this is however for perf counter only, for legacy reasons. What we showed 
    // in perf counters for "gen0 size" was really the gen0 budget which made
    // sense (somewhat) at the time. For backward compatibility we are keeping
    // this calculated the same way. For ETW we use the true gen0 size (and 
    // gen0 budget is also reported in an event).
    size_t youngest_budget = 0;

    size_t promoted_finalization_mem = 0;
    size_t total_num_pinned_objects = gc_heap::get_total_pinned_objects();

#ifndef FEATURE_REDHAWK
    // if a max gen garbage collection was performed, resync the GC Handle counter; 
    // if threads are currently suspended, we do not need to obtain a lock on each handle table
    if (condemned_gen == max_generation)
        total_num_gc_handles = HndCountAllHandles(!IsGCInProgress());
#endif //FEATURE_REDHAWK

    // per generation calculation.
    for (int gen_index = 0; gen_index <= (max_generation+1); gen_index++)
    {
#ifdef MULTIPLE_HEAPS
        int hn = 0;
        for (hn = 0; hn < gc_heap::n_heaps; hn++)
        {
            gc_heap* hp = gc_heap::g_heaps[hn];
#else
            gc_heap* hp = pGenGCHeap;
            {
#endif //MULTIPLE_HEAPS
                dynamic_data* dd = hp->dynamic_data_of (gen_index);

                if (gen_index == 0)
                {
                    youngest_budget += dd_desired_allocation (hp->dynamic_data_of (gen_index));
                }

                g_GenerationSizes[gen_index] += hp->generation_size (gen_index);

                if (gen_index <= condemned_gen)
                {
                    g_GenerationPromotedSizes[gen_index] += dd_promoted_size (dd);
                }

                if ((gen_index == (max_generation+1)) && (condemned_gen == max_generation))
                {
                    g_GenerationPromotedSizes[gen_index] += dd_promoted_size (dd);
                }

                if (gen_index == 0)
                {
                    promoted_finalization_mem +=  dd_freach_previous_promotion (dd);
                }
#ifdef MULTIPLE_HEAPS
            }
#else
        }
#endif //MULTIPLE_HEAPS
    }
#endif //FEATURE_EVENT_TRACE

#ifdef FEATURE_EVENT_TRACE
    g_theGCHeap->DiagDescrGenerations([](void*, int generation, uint8_t* rangeStart, uint8_t* rangeEnd, uint8_t* rangeEndReserved)
    {
        uint64_t range = static_cast<uint64_t>(rangeEnd - rangeStart);
        uint64_t rangeReserved = static_cast<uint64_t>(rangeEndReserved - rangeStart);
        FIRE_EVENT(GCGenerationRange, generation, rangeStart, range, rangeReserved);
    }, nullptr);

    FIRE_EVENT(GCEnd_V1, static_cast<uint32_t>(pSettings->gc_index), condemned_gen);

#ifdef SIMPLE_DPRINTF
    dprintf (2, ("GC#%d: 0: %Id(%Id); 1: %Id(%Id); 2: %Id(%Id); 3: %Id(%Id)", 
        (size_t)pSettings->gc_index,
        g_GenerationSizes[0], g_GenerationPromotedSizes[0],
        g_GenerationSizes[1], g_GenerationPromotedSizes[1],
        g_GenerationSizes[2], g_GenerationPromotedSizes[2],
        g_GenerationSizes[3], g_GenerationPromotedSizes[3]));
#endif //SIMPLE_DPRINTF

    FIRE_EVENT(GCHeapStats_V1,
        g_GenerationSizes[0], g_GenerationPromotedSizes[0],
        g_GenerationSizes[1], g_GenerationPromotedSizes[1],
        g_GenerationSizes[2], g_GenerationPromotedSizes[2],
        g_GenerationSizes[3], g_GenerationPromotedSizes[3],
        promoted_finalization_mem,
        GetFinalizablePromotedCount(),
        static_cast<uint32_t>(total_num_pinned_objects),
        total_num_sync_blocks,
        static_cast<uint32_t>(total_num_gc_handles));

#endif // FEATURE_EVENT_TRACE

    // Compute Time in GC
    uint64_t _currentPerfCounterTimer = GCToOSInterface::QueryPerformanceCounter();

    g_TotalTimeInGC = _currentPerfCounterTimer - g_TotalTimeInGC;
    uint64_t _timeInGCBase = (_currentPerfCounterTimer - g_TotalTimeSinceLastGCEnd);

    if (_timeInGCBase < g_TotalTimeInGC)
        g_TotalTimeInGC = 0;        // isn't likely except on some SMP machines-- perhaps make sure that
                                    //  _timeInGCBase >= g_TotalTimeInGC by setting affinity in GET_CYCLE_COUNT

    while (_timeInGCBase > UINT32_MAX) 
    {
        _timeInGCBase = _timeInGCBase >> 8;
        g_TotalTimeInGC = g_TotalTimeInGC >> 8;
    }

    // Update percent time spent in GC
    g_percentTimeInGCSinceLastGC = (int)(g_TotalTimeInGC * 100 / _timeInGCBase);

    g_TotalTimeSinceLastGCEnd = _currentPerfCounterTimer;
}

int GCHeap::GetLastGCPercentTimeInGC()
{
    return (int)(g_percentTimeInGCSinceLastGC);
}

size_t GCHeap::GetLastGCGenerationSize(int gen)
{
    return g_GenerationSizes[gen];
}

size_t GCHeap::GetCurrentObjSize()
{
    return (totalSurvivedSize + gc_heap::get_total_allocated());
}

size_t GCHeap::GetLastGCStartTime(int generation)
{
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = gc_heap::g_heaps[0];
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    return dd_time_clock (hp->dynamic_data_of (generation));
}

size_t GCHeap::GetLastGCDuration(int generation)
{
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = gc_heap::g_heaps[0];
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    return dd_gc_elapsed_time (hp->dynamic_data_of (generation));
}

size_t GetHighPrecisionTimeStamp();

size_t GCHeap::GetNow()
{
    return GetHighPrecisionTimeStamp();
}

bool GCHeap::IsGCInProgressHelper (bool bConsiderGCStart)
{
    return GcInProgress || (bConsiderGCStart? VolatileLoad(&gc_heap::gc_started) : FALSE);
}

uint32_t GCHeap::WaitUntilGCComplete(bool bConsiderGCStart)
{
    if (bConsiderGCStart)
    {
        if (gc_heap::gc_started)
        {
            gc_heap::wait_for_gc_done();
        }
    }

    uint32_t dwWaitResult = NOERROR;

    if (GcInProgress) 
    {
        ASSERT( WaitForGCEvent->IsValid() );

#ifdef DETECT_DEADLOCK
        // wait for GC to complete
BlockAgain:
        dwWaitResult = WaitForGCEvent->Wait(DETECT_DEADLOCK_TIMEOUT, FALSE );

        if (dwWaitResult == WAIT_TIMEOUT) {
            //  Even in retail, stop in the debugger if available.
            GCToOSInterface::DebugBreak();
            goto BlockAgain;
        }

#else  //DETECT_DEADLOCK
        
        dwWaitResult = WaitForGCEvent->Wait(INFINITE, FALSE );
        
#endif //DETECT_DEADLOCK
    }

    return dwWaitResult;
}

void GCHeap::SetGCInProgress(bool fInProgress)
{
    GcInProgress = fInProgress;
}

void GCHeap::SetWaitForGCEvent()
{
    WaitForGCEvent->Set();
}

void GCHeap::ResetWaitForGCEvent()
{
    WaitForGCEvent->Reset();
}

void GCHeap::WaitUntilConcurrentGCComplete()
{
#ifdef BACKGROUND_GC
    if (pGenGCHeap->settings.concurrent)
        pGenGCHeap->background_gc_wait();
#endif //BACKGROUND_GC
}

bool GCHeap::IsConcurrentGCInProgress()
{
#ifdef BACKGROUND_GC
    return !!pGenGCHeap->settings.concurrent;
#else
    return false;
#endif //BACKGROUND_GC
}

#ifdef FEATURE_EVENT_TRACE
void gc_heap::fire_etw_allocation_event (size_t allocation_amount, int gen_number, uint8_t* object_address)
{
    gc_etw_alloc_kind kind = gen_number == 0 ? gc_etw_alloc_soh : gc_etw_alloc_loh;
    FIRE_EVENT(GCAllocationTick_V3, static_cast<uint64_t>(allocation_amount), kind, heap_number, object_address);
}

void gc_heap::fire_etw_pin_object_event (uint8_t* object, uint8_t** ppObject)
{
    FIRE_EVENT(PinObjectAtGCTime, object, ppObject);
}
#endif // FEATURE_EVENT_TRACE

uint32_t gc_heap::user_thread_wait (GCEvent *event, BOOL no_mode_change, int time_out_ms)
{
    Thread* pCurThread = NULL;
    bool bToggleGC = false;
    uint32_t dwWaitResult = NOERROR;
    
    if (!no_mode_change)
    {
        bToggleGC = GCToEEInterface::EnablePreemptiveGC();
    }

    dwWaitResult = event->Wait(time_out_ms, FALSE);

    if (bToggleGC)
    {
        GCToEEInterface::DisablePreemptiveGC();
    }

    return dwWaitResult;
}

#ifdef BACKGROUND_GC
// Wait for background gc to finish
uint32_t gc_heap::background_gc_wait (alloc_wait_reason awr, int time_out_ms)
{
    dprintf(2, ("Waiting end of background gc"));
    assert (background_gc_done_event.IsValid());
    fire_alloc_wait_event_begin (awr);
    uint32_t dwRet = user_thread_wait (&background_gc_done_event, FALSE, time_out_ms);
    fire_alloc_wait_event_end (awr);
    dprintf(2, ("Waiting end of background gc is done"));

    return dwRet;
}

// Wait for background gc to finish sweeping large objects
void gc_heap::background_gc_wait_lh (alloc_wait_reason awr)
{
    dprintf(2, ("Waiting end of background large sweep"));
    assert (gc_lh_block_event.IsValid());
    fire_alloc_wait_event_begin (awr);
    user_thread_wait (&gc_lh_block_event, FALSE);
    fire_alloc_wait_event_end (awr);
    dprintf(2, ("Waiting end of background large sweep is done"));
}

#endif //BACKGROUND_GC


/******************************************************************************/
IGCHeapInternal* CreateGCHeap() {
    return new(nothrow) GCHeap();   // we return wks or svr 
}

void GCHeap::DiagTraceGCSegments()
{
#ifdef FEATURE_EVENT_TRACE
    heap_segment* seg = 0;
#ifdef MULTIPLE_HEAPS
    // walk segments in each heap
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* h = gc_heap::g_heaps [i];
#else
    {
        gc_heap* h = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        for (seg = generation_start_segment (h->generation_of (max_generation)); seg != 0; seg = heap_segment_next(seg))
        {
            uint8_t* address = heap_segment_mem (seg);
            size_t size = heap_segment_reserved (seg) - heap_segment_mem (seg);
            gc_etw_segment_type type = heap_segment_read_only_p (seg) ? gc_etw_segment_read_only_heap : gc_etw_segment_small_object_heap;
            FIRE_EVENT(GCCreateSegment_V1, address, size, static_cast<uint32_t>(type));
        }

        // large obj segments
        for (seg = generation_start_segment (h->generation_of (max_generation+1)); seg != 0; seg = heap_segment_next(seg))
        {
            uint8_t* address = heap_segment_mem (seg);
            size_t size = heap_segment_reserved (seg) - heap_segment_mem (seg);
            FIRE_EVENT(GCCreateSegment_V1, address, size, static_cast<uint32_t>(gc_etw_segment_large_object_heap));
        }
    }
#endif // FEATURE_EVENT_TRACE
}

void GCHeap::DiagDescrGenerations (gen_walk_fn fn, void *context)
{
    pGenGCHeap->descr_generations_to_profiler(fn, context);
}

segment_handle GCHeap::RegisterFrozenSegment(segment_info *pseginfo)
{
#ifdef FEATURE_BASICFREEZE
    heap_segment * seg = new (nothrow) heap_segment;
    if (!seg)
    {
        return NULL;
    }

    uint8_t* base_mem = (uint8_t*)pseginfo->pvMem;
    heap_segment_mem(seg) = base_mem + pseginfo->ibFirstObject;
    heap_segment_allocated(seg) = base_mem + pseginfo->ibAllocated;
    heap_segment_committed(seg) = base_mem + pseginfo->ibCommit;
    heap_segment_reserved(seg) = base_mem + pseginfo->ibReserved;
    heap_segment_next(seg) = 0;
    heap_segment_used(seg) = heap_segment_allocated(seg);
    heap_segment_plan_allocated(seg) = 0;
    seg->flags = heap_segment_flags_readonly;

#if defined (MULTIPLE_HEAPS) && !defined (ISOLATED_HEAPS)
    gc_heap* heap = gc_heap::g_heaps[0];
    heap_segment_heap(seg) = heap;
#else
    gc_heap* heap = pGenGCHeap;
#endif //MULTIPLE_HEAPS && !ISOLATED_HEAPS

    if (heap->insert_ro_segment(seg) == FALSE)
    {
        delete seg;
        return NULL;
    }

    return reinterpret_cast< segment_handle >(seg);
#else
    assert(!"Should not call GCHeap::RegisterFrozenSegment without FEATURE_BASICFREEZE defined!");
    return NULL;
#endif // FEATURE_BASICFREEZE
}

void GCHeap::UnregisterFrozenSegment(segment_handle seg)
{
#ifdef FEATURE_BASICFREEZE
#if defined (MULTIPLE_HEAPS) && !defined (ISOLATED_HEAPS)
    gc_heap* heap = gc_heap::g_heaps[0];
#else
    gc_heap* heap = pGenGCHeap;
#endif //MULTIPLE_HEAPS && !ISOLATED_HEAPS

    heap->remove_ro_segment(reinterpret_cast<heap_segment*>(seg));
#else
    assert(!"Should not call GCHeap::UnregisterFrozenSegment without FEATURE_BASICFREEZE defined!");
#endif // FEATURE_BASICFREEZE
}

bool GCHeap::IsInFrozenSegment(Object *object)
{
#ifdef FEATURE_BASICFREEZE
    uint8_t* o = (uint8_t*)object;
    heap_segment * hs = gc_heap::find_segment (o, FALSE);
    //We create a frozen object for each frozen segment before the segment is inserted
    //to segment list; during ngen, we could also create frozen objects in segments which
    //don't belong to current GC heap.
    //So we return true if hs is NULL. It might create a hole about detecting invalidate 
    //object. But given all other checks present, the hole should be very small
    return !hs || heap_segment_read_only_p (hs);
#else // FEATURE_BASICFREEZE
    return false;
#endif
}

bool GCHeap::RuntimeStructuresValid()
{
    return GCScan::GetGcRuntimeStructuresValid();
}

void GCHeap::SetSuspensionPending(bool fSuspensionPending)
{
    if (fSuspensionPending)
    {
        Interlocked::Increment(&g_fSuspensionPending);
    }
    else
    {
        Interlocked::Decrement(&g_fSuspensionPending);
    }
}

void GCHeap::ControlEvents(GCEventKeyword keyword, GCEventLevel level)
{
    GCEventStatus::Set(GCEventProvider_Default, keyword, level);
}

void GCHeap::ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level)
{
    GCEventStatus::Set(GCEventProvider_Private, keyword, level);
}

#endif // !DACCESS_COMPILE


