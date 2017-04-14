// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


// sets up vars for GC

#include "gcpriv.h"

#ifndef DACCESS_COMPILE

COUNTER_ONLY(PERF_COUNTER_TIMER_PRECISION g_TotalTimeInGC = 0);
COUNTER_ONLY(PERF_COUNTER_TIMER_PRECISION g_TotalTimeSinceLastGCEnd = 0);

#if defined(ENABLE_PERF_COUNTERS) || defined(FEATURE_EVENT_TRACE)
size_t g_GenerationSizes[NUMBERGENERATIONS];
size_t g_GenerationPromotedSizes[NUMBERGENERATIONS];
#endif // ENABLE_PERF_COUNTERS || FEATURE_EVENT_TRACE

void GCHeap::UpdatePreGCCounters()
{
#if defined(ENABLE_PERF_COUNTERS)
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = 0;
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    size_t allocation_0 = 0;
    size_t allocation_3 = 0; 
    
    // Publish perf stats
    g_TotalTimeInGC = GET_CYCLE_COUNT();

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

    GetPerfCounters().m_GC.cbAlloc += allocation_0;
    GetPerfCounters().m_GC.cbAlloc += allocation_3;
    GetPerfCounters().m_GC.cbLargeAlloc += allocation_3;

#ifdef _PREFAST_
    // prefix complains about us dereferencing hp in wks build even though we only access static members
    // this way. not sure how to shut it up except for this ugly workaround:
    PREFIX_ASSUME( hp != NULL);
#endif //_PREFAST_
    if (hp->settings.reason == reason_induced IN_STRESS_HEAP( && !hp->settings.stress_induced))
    {
        COUNTER_ONLY(GetPerfCounters().m_GC.cInducedGCs++);
    }

    GetPerfCounters().m_Security.timeRTchecks = 0;
    GetPerfCounters().m_Security.timeRTchecksBase = 1; // To avoid divide by zero

#endif //ENABLE_PERF_COUNTERS

#ifdef FEATURE_EVENT_TRACE
#ifdef MULTIPLE_HEAPS
        //take the first heap....
    gc_mechanisms *pSettings = &gc_heap::g_heaps[0]->settings;
#else
    gc_mechanisms *pSettings = &gc_heap::settings;
#endif //MULTIPLE_HEAPS

    ETW::GCLog::ETW_GC_INFO Info;

    Info.GCStart.Count = (uint32_t)pSettings->gc_index;
    Info.GCStart.Depth = (uint32_t)pSettings->condemned_generation;
    Info.GCStart.Reason = (ETW::GCLog::ETW_GC_INFO::GC_REASON)((int)(pSettings->reason));

    Info.GCStart.Type = ETW::GCLog::ETW_GC_INFO::GC_NGC;
    if (pSettings->concurrent)
    {
        Info.GCStart.Type = ETW::GCLog::ETW_GC_INFO::GC_BGC;
    }
#ifdef BACKGROUND_GC
    else if (Info.GCStart.Depth < max_generation)
    {
        if (pSettings->background_p)
            Info.GCStart.Type = ETW::GCLog::ETW_GC_INFO::GC_FGC;
    }
#endif //BACKGROUND_GC

    ETW::GCLog::FireGcStartAndGenerationRanges(&Info);
#endif // FEATURE_EVENT_TRACE
}

void GCHeap::UpdatePostGCCounters()
{
    totalSurvivedSize = gc_heap::get_total_survived_size();

    //
    // The following is for instrumentation.
    //
    // Calculate the common ones for ETW and perf counters.
#if defined(ENABLE_PERF_COUNTERS) || defined(FEATURE_EVENT_TRACE)
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
    uint32_t total_num_sync_blocks = SyncBlockCache::GetSyncBlockCache()->GetActiveCount();

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
#endif //ENABLE_PERF_COUNTERS || FEATURE_EVENT_TRACE

#ifdef FEATURE_EVENT_TRACE
    ETW::GCLog::ETW_GC_INFO Info;

    Info.GCEnd.Depth = condemned_gen;
    Info.GCEnd.Count = (uint32_t)pSettings->gc_index;
    ETW::GCLog::FireGcEndAndGenerationRanges(Info.GCEnd.Count, Info.GCEnd.Depth);

    ETW::GCLog::ETW_GC_INFO HeapInfo;
    ZeroMemory(&HeapInfo, sizeof(HeapInfo));

    for (int gen_index = 0; gen_index <= (max_generation+1); gen_index++)
    {
        HeapInfo.HeapStats.GenInfo[gen_index].GenerationSize = g_GenerationSizes[gen_index];
        HeapInfo.HeapStats.GenInfo[gen_index].TotalPromotedSize = g_GenerationPromotedSizes[gen_index];
    }

#ifdef SIMPLE_DPRINTF
    dprintf (2, ("GC#%d: 0: %Id(%Id); 1: %Id(%Id); 2: %Id(%Id); 3: %Id(%Id)", 
        Info.GCEnd.Count,
        HeapInfo.HeapStats.GenInfo[0].GenerationSize,
        HeapInfo.HeapStats.GenInfo[0].TotalPromotedSize,
        HeapInfo.HeapStats.GenInfo[1].GenerationSize,
        HeapInfo.HeapStats.GenInfo[1].TotalPromotedSize,
        HeapInfo.HeapStats.GenInfo[2].GenerationSize,
        HeapInfo.HeapStats.GenInfo[2].TotalPromotedSize,
        HeapInfo.HeapStats.GenInfo[3].GenerationSize,
        HeapInfo.HeapStats.GenInfo[3].TotalPromotedSize));
#endif //SIMPLE_DPRINTF

    HeapInfo.HeapStats.FinalizationPromotedSize = promoted_finalization_mem;
    HeapInfo.HeapStats.FinalizationPromotedCount = GetFinalizablePromotedCount();
    HeapInfo.HeapStats.PinnedObjectCount = (uint32_t)total_num_pinned_objects;
    HeapInfo.HeapStats.SinkBlockCount =  total_num_sync_blocks;
    HeapInfo.HeapStats.GCHandleCount =  (uint32_t)total_num_gc_handles;

    FireEtwGCHeapStats_V1(HeapInfo.HeapStats.GenInfo[0].GenerationSize, HeapInfo.HeapStats.GenInfo[0].TotalPromotedSize,
                    HeapInfo.HeapStats.GenInfo[1].GenerationSize, HeapInfo.HeapStats.GenInfo[1].TotalPromotedSize,
                    HeapInfo.HeapStats.GenInfo[2].GenerationSize, HeapInfo.HeapStats.GenInfo[2].TotalPromotedSize,
                    HeapInfo.HeapStats.GenInfo[3].GenerationSize, HeapInfo.HeapStats.GenInfo[3].TotalPromotedSize,
                    HeapInfo.HeapStats.FinalizationPromotedSize,
                    HeapInfo.HeapStats.FinalizationPromotedCount,
                    HeapInfo.HeapStats.PinnedObjectCount,
                    HeapInfo.HeapStats.SinkBlockCount,
                    HeapInfo.HeapStats.GCHandleCount, 
                    GetClrInstanceId());
#endif // FEATURE_EVENT_TRACE

#if defined(ENABLE_PERF_COUNTERS)
    for (int gen_index = 0; gen_index <= (max_generation+1); gen_index++)
    {
        _ASSERTE(FitsIn<size_t>(g_GenerationSizes[gen_index]));
        _ASSERTE(FitsIn<size_t>(g_GenerationPromotedSizes[gen_index]));

        if (gen_index == (max_generation+1))
        {
            GetPerfCounters().m_GC.cLrgObjSize = static_cast<size_t>(g_GenerationSizes[gen_index]);
        }
        else
        {
            GetPerfCounters().m_GC.cGenHeapSize[gen_index] = ((gen_index == 0) ? 
                                                                youngest_budget : 
                                                                static_cast<size_t>(g_GenerationSizes[gen_index]));
        }

        // the perf counters only count the promoted size for gen0 and gen1.
        if (gen_index < max_generation)
        {
            GetPerfCounters().m_GC.cbPromotedMem[gen_index] = static_cast<size_t>(g_GenerationPromotedSizes[gen_index]);
        }

        if (gen_index <= max_generation)
        {
            GetPerfCounters().m_GC.cGenCollections[gen_index] =
                dd_collection_count (hp1->dynamic_data_of (gen_index));
        }
    }

    // Committed and reserved memory 
    {
        size_t committed_mem = 0;
        size_t reserved_mem = 0;
#ifdef MULTIPLE_HEAPS
        int hn = 0;
        for (hn = 0; hn < gc_heap::n_heaps; hn++)
        {
            gc_heap* hp = gc_heap::g_heaps [hn];
#else
            gc_heap* hp = pGenGCHeap;
            {
#endif //MULTIPLE_HEAPS
                heap_segment* seg = generation_start_segment (hp->generation_of (max_generation));
                while (seg)
                {
                    committed_mem += heap_segment_committed (seg) - heap_segment_mem (seg);
                    reserved_mem += heap_segment_reserved (seg) - heap_segment_mem (seg);
                    seg = heap_segment_next (seg);
                }
                //same for large segments
                seg = generation_start_segment (hp->generation_of (max_generation + 1));
                while (seg)
                {
                    committed_mem += heap_segment_committed (seg) - 
                        heap_segment_mem (seg);
                    reserved_mem += heap_segment_reserved (seg) - 
                        heap_segment_mem (seg);
                    seg = heap_segment_next (seg);
                }
#ifdef MULTIPLE_HEAPS
            }
#else
        }
#endif //MULTIPLE_HEAPS

        GetPerfCounters().m_GC.cTotalCommittedBytes = committed_mem;
        GetPerfCounters().m_GC.cTotalReservedBytes = reserved_mem;
    }

    _ASSERTE(FitsIn<size_t>(HeapInfo.HeapStats.FinalizationPromotedSize));
    _ASSERTE(FitsIn<size_t>(HeapInfo.HeapStats.FinalizationPromotedCount));
    GetPerfCounters().m_GC.cbPromotedFinalizationMem = static_cast<size_t>(HeapInfo.HeapStats.FinalizationPromotedSize);
    GetPerfCounters().m_GC.cSurviveFinalize = static_cast<size_t>(HeapInfo.HeapStats.FinalizationPromotedCount);
    
    // Compute Time in GC
    PERF_COUNTER_TIMER_PRECISION _currentPerfCounterTimer = GET_CYCLE_COUNT();

    g_TotalTimeInGC = _currentPerfCounterTimer - g_TotalTimeInGC;
    PERF_COUNTER_TIMER_PRECISION _timeInGCBase = (_currentPerfCounterTimer - g_TotalTimeSinceLastGCEnd);

    if (_timeInGCBase < g_TotalTimeInGC)
        g_TotalTimeInGC = 0;        // isn't likely except on some SMP machines-- perhaps make sure that
                                    //  _timeInGCBase >= g_TotalTimeInGC by setting affinity in GET_CYCLE_COUNT
                                    
    while (_timeInGCBase > UINT_MAX) 
    {
        _timeInGCBase = _timeInGCBase >> 8;
        g_TotalTimeInGC = g_TotalTimeInGC >> 8;
    }

    // Update Total Time    
    GetPerfCounters().m_GC.timeInGC = (uint32_t)g_TotalTimeInGC;
    GetPerfCounters().m_GC.timeInGCBase = (uint32_t)_timeInGCBase;

    if (!GetPerfCounters().m_GC.cProcessID)
        GetPerfCounters().m_GC.cProcessID = (size_t)GetCurrentProcessId();
    
    g_TotalTimeSinceLastGCEnd = _currentPerfCounterTimer;

    GetPerfCounters().m_GC.cPinnedObj = total_num_pinned_objects;
    GetPerfCounters().m_GC.cHandles = total_num_gc_handles;
    GetPerfCounters().m_GC.cSinkBlocks = total_num_sync_blocks;
#endif //ENABLE_PERF_COUNTERS
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
    void * typeId = nullptr;
    const WCHAR * name = nullptr;
#ifdef FEATURE_REDHAWK
    typeId = RedhawkGCInterface::GetLastAllocEEType();
#else
    InlineSString<MAX_CLASSNAME_LENGTH> strTypeName;

    EX_TRY
    {
        TypeHandle th = GetThread()->GetTHAllocContextObj();

        if (th != 0)
        {
            th.GetName(strTypeName);
            name = strTypeName.GetUnicode();
            typeId = th.GetMethodTable();
        }
    }
    EX_CATCH {}
    EX_END_CATCH(SwallowAllExceptions)
#endif

    if (typeId != nullptr)
    {
        FireEtwGCAllocationTick_V3((uint32_t)allocation_amount,
                                   ((gen_number == 0) ? ETW::GCLog::ETW_GC_INFO::AllocationSmall : ETW::GCLog::ETW_GC_INFO::AllocationLarge), 
                                   GetClrInstanceId(),
                                   allocation_amount,
                                   typeId, 
                                   name,
                                   heap_number,
                                   object_address
                                   );
    }
}
void gc_heap::fire_etw_pin_object_event (uint8_t* object, uint8_t** ppObject)
{
#ifdef FEATURE_REDHAWK
    UNREFERENCED_PARAMETER(object);
    UNREFERENCED_PARAMETER(ppObject);
#else
    Object* obj = (Object*)object;

    InlineSString<MAX_CLASSNAME_LENGTH> strTypeName; 
   
    EX_TRY
    {
        FAULT_NOT_FATAL();

        TypeHandle th = obj->GetGCSafeTypeHandleIfPossible();
        if(th != NULL)
        {
            th.GetName(strTypeName);
        }

        FireEtwPinObjectAtGCTime(ppObject,
                             object,
                             obj->GetSize(),
                             strTypeName.GetUnicode(),
                             GetClrInstanceId());
    }
    EX_CATCH {}
    EX_END_CATCH(SwallowAllExceptions)
#endif // FEATURE_REDHAWK
}
#endif // FEATURE_EVENT_TRACE

uint32_t gc_heap::user_thread_wait (GCEvent *event, BOOL no_mode_change, int time_out_ms)
{
    Thread* pCurThread = NULL;
    bool mode = false;
    uint32_t dwWaitResult = NOERROR;
    
    if (!no_mode_change)
    {
        pCurThread = GetThread();
        mode = pCurThread ? GCToEEInterface::IsPreemptiveGCDisabled(pCurThread) : false;
        if (mode)
        {
            GCToEEInterface::EnablePreemptiveGC(pCurThread);
        }
    }

    dwWaitResult = event->Wait(time_out_ms, FALSE);

    if (!no_mode_change && mode)
    {
        GCToEEInterface::DisablePreemptiveGC(pCurThread);
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
            ETW::GCLog::ETW_GC_INFO Info;
            Info.GCCreateSegment.Address = (size_t)heap_segment_mem(seg);
            Info.GCCreateSegment.Size = (size_t)(heap_segment_reserved (seg) - heap_segment_mem(seg));
            Info.GCCreateSegment.Type = (heap_segment_read_only_p (seg) ? 
                                         ETW::GCLog::ETW_GC_INFO::READ_ONLY_HEAP :
                                         ETW::GCLog::ETW_GC_INFO::SMALL_OBJECT_HEAP);
            FireEtwGCCreateSegment_V1(Info.GCCreateSegment.Address, Info.GCCreateSegment.Size, Info.GCCreateSegment.Type, GetClrInstanceId());
        }

        // large obj segments
        for (seg = generation_start_segment (h->generation_of (max_generation+1)); seg != 0; seg = heap_segment_next(seg))
        {
            FireEtwGCCreateSegment_V1((size_t)heap_segment_mem(seg), 
                                   (size_t)(heap_segment_reserved (seg) - heap_segment_mem(seg)), 
                                   ETW::GCLog::ETW_GC_INFO::LARGE_OBJECT_HEAP, 
                                   GetClrInstanceId());
        }
    }
#endif // FEATURE_EVENT_TRACE
}

void GCHeap::DiagDescrGenerations (gen_walk_fn fn, void *context)
{
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    pGenGCHeap->descr_generations_to_profiler(fn, context);
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
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

bool GCHeap::RuntimeStructuresValid()
{
    return GCScan::GetGcRuntimeStructuresValid();
}


#endif // !DACCESS_COMPILE


