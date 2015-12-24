//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 

// 


// sets up vars for GC

#include "gcpriv.h"

#ifndef DACCESS_COMPILE

COUNTER_ONLY(PERF_COUNTER_TIMER_PRECISION g_TotalTimeInGC = 0);
COUNTER_ONLY(PERF_COUNTER_TIMER_PRECISION g_TotalTimeSinceLastGCEnd = 0);

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
    GetPerfCounters().m_GC.cPinnedObj = 0;

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
#ifdef FEATURE_EVENT_TRACE
    // Use of temporary variables to avoid rotor build warnings
    ETW::GCLog::ETW_GC_INFO Info;
#ifdef MULTIPLE_HEAPS
    //take the first heap....
    gc_mechanisms *pSettings = &gc_heap::g_heaps[0]->settings;
#else
    gc_mechanisms *pSettings = &gc_heap::settings;
#endif //MULTIPLE_HEAPS

    int condemned_gen = pSettings->condemned_generation;
    Info.GCEnd.Depth = condemned_gen;
    Info.GCEnd.Count = (uint32_t)pSettings->gc_index;
    ETW::GCLog::FireGcEndAndGenerationRanges(Info.GCEnd.Count, Info.GCEnd.Depth);

    int xGen;
    ETW::GCLog::ETW_GC_INFO HeapInfo;
    ZeroMemory(&HeapInfo, sizeof(HeapInfo));
    size_t youngest_gen_size = 0;
    
#ifdef MULTIPLE_HEAPS
    //take the first heap....
    gc_heap* hp1 = gc_heap::g_heaps[0];
#else
    gc_heap* hp1 = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    size_t promoted_finalization_mem = 0;

    totalSurvivedSize = gc_heap::get_total_survived_size();

    for (xGen = 0; xGen <= (max_generation+1); xGen++)
    {
        size_t gensize = 0;
        size_t promoted_mem = 0; 

#ifdef MULTIPLE_HEAPS
        int hn = 0;

        for (hn = 0; hn < gc_heap::n_heaps; hn++)
        {
            gc_heap* hp2 = gc_heap::g_heaps [hn];
            dynamic_data* dd2 = hp2->dynamic_data_of (xGen);

            // Generation 0 is empty (if there isn't demotion) so its size is 0
            // It is more interesting to report the desired size before next collection.
            // Gen 1 is also more accurate if desired is reported due to sampling intervals.
            if (xGen == 0)
            {
                youngest_gen_size += dd_desired_allocation (hp2->dynamic_data_of (xGen));
            }

            gensize += hp2->generation_size(xGen);          

            if (xGen <= condemned_gen)
            {
                promoted_mem += dd_promoted_size (dd2);
            }

            if ((xGen == (max_generation+1)) && (condemned_gen == max_generation))
            {
                promoted_mem += dd_promoted_size (dd2);
            }

            if (xGen == 0)
            {
                promoted_finalization_mem +=  dd_freach_previous_promotion (dd2);
            }
        }
#else
        if (xGen == 0)
        {
            youngest_gen_size = dd_desired_allocation (hp1->dynamic_data_of (xGen));
        }

        gensize = hp1->generation_size(xGen);
        if (xGen <= condemned_gen)
        {
            promoted_mem = dd_promoted_size (hp1->dynamic_data_of (xGen));
        }

        if ((xGen == (max_generation+1)) && (condemned_gen == max_generation))
        {
            promoted_mem = dd_promoted_size (hp1->dynamic_data_of (max_generation+1));
        }

        if (xGen == 0)
        {
            promoted_finalization_mem =  dd_freach_previous_promotion (hp1->dynamic_data_of (xGen));
        }

#endif //MULTIPLE_HEAPS

        HeapInfo.HeapStats.GenInfo[xGen].GenerationSize = gensize;
        HeapInfo.HeapStats.GenInfo[xGen].TotalPromotedSize = promoted_mem;
    }

    {
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
    }

    HeapInfo.HeapStats.FinalizationPromotedSize = promoted_finalization_mem;
    HeapInfo.HeapStats.FinalizationPromotedCount = GetFinalizablePromotedCount();

#if defined(ENABLE_PERF_COUNTERS)
    
    // if a max gen garbage collection was performed, resync the GC Handle counter; 
    // if threads are currently suspended, we do not need to obtain a lock on each handle table
    if (condemned_gen == max_generation)
        GetPerfCounters().m_GC.cHandles = HndCountAllHandles(!GCHeap::IsGCInProgress());

    for (xGen = 0; xGen <= (max_generation+1); xGen++)
    {
        _ASSERTE(FitsIn<size_t>(HeapInfo.HeapStats.GenInfo[xGen].GenerationSize));
        _ASSERTE(FitsIn<size_t>(HeapInfo.HeapStats.GenInfo[xGen].TotalPromotedSize));

        if (xGen == (max_generation+1))
        {
            GetPerfCounters().m_GC.cLrgObjSize = static_cast<size_t>(HeapInfo.HeapStats.GenInfo[xGen].GenerationSize);
        }
        else
        {
            GetPerfCounters().m_GC.cGenHeapSize[xGen] = ((xGen == 0) ? 
                                                                youngest_gen_size : 
                                                                static_cast<size_t>(HeapInfo.HeapStats.GenInfo[xGen].GenerationSize));
        }

        // the perf counters only count the promoted size for gen0 and gen1.
        if (xGen < max_generation)
        {
            GetPerfCounters().m_GC.cbPromotedMem[xGen] = static_cast<size_t>(HeapInfo.HeapStats.GenInfo[xGen].TotalPromotedSize);
        }

        if (xGen <= max_generation)
        {
            GetPerfCounters().m_GC.cGenCollections[xGen] =
                dd_collection_count (hp1->dynamic_data_of (xGen));
        }
    }

    //Committed memory 
    {
        size_t committed_mem = 0;
        size_t reserved_mem = 0;
#ifdef MULTIPLE_HEAPS
        int hn = 0;
        for (hn = 0; hn < gc_heap::n_heaps; hn++)
        {
            gc_heap* hp2 = gc_heap::g_heaps [hn];
#else
            gc_heap* hp2 = hp1;
            {
#endif //MULTIPLE_HEAPS
                heap_segment* seg = 
                    generation_start_segment (hp2->generation_of (max_generation));
                while (seg)
                {
                    committed_mem += heap_segment_committed (seg) - 
                        heap_segment_mem (seg);
                    reserved_mem += heap_segment_reserved (seg) - 
                        heap_segment_mem (seg);
                    seg = heap_segment_next (seg);
                }
                //same for large segments
                seg = 
                    generation_start_segment (hp2->generation_of (max_generation + 1));
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

        GetPerfCounters().m_GC.cTotalCommittedBytes = 
            committed_mem;
        GetPerfCounters().m_GC.cTotalReservedBytes = 
            reserved_mem;
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

    HeapInfo.HeapStats.PinnedObjectCount = (uint32_t)(GetPerfCounters().m_GC.cPinnedObj);
    HeapInfo.HeapStats.SinkBlockCount =  (uint32_t)(GetPerfCounters().m_GC.cSinkBlocks);
    HeapInfo.HeapStats.GCHandleCount =  (uint32_t)(GetPerfCounters().m_GC.cHandles);
#endif //ENABLE_PERF_COUNTERS

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

#if defined(GC_PROFILING) //UNIXTODO: Enable this for FEATURE_EVENT_TRACE
void ProfScanRootsHelper(Object** ppObject, ScanContext *pSC, uint32_t dwFlags)
{
#if  defined(FEATURE_EVENT_TRACE)
    Object *pObj = *ppObject;
#ifdef INTERIOR_POINTERS
    if (dwFlags & GC_CALL_INTERIOR)
    {
        uint8_t *o = (uint8_t*)pObj;
        gc_heap* hp = gc_heap::heap_of (o);

        if ((o < hp->gc_low) || (o >= hp->gc_high))
        {
            return;
        }
        pObj = (Object*) hp->find_object(o, hp->gc_low);
    }
#endif //INTERIOR_POINTERS
    ScanRootsHelper(&pObj, pSC, dwFlags);
#endif //  defined(FEATURE_EVENT_TRACE)
}

// This is called only if we've determined that either:
//     a) The Profiling API wants to do a walk of the heap, and it has pinned the
//     profiler in place (so it cannot be detached), and it's thus safe to call into the
//     profiler, OR
//     b) ETW infrastructure wants to do a walk of the heap either to log roots,
//     objects, or both.
// This can also be called to do a single walk for BOTH a) and b) simultaneously.  Since
// ETW can ask for roots, but not objects
void GCProfileWalkHeapWorker(BOOL fProfilerPinned, BOOL fShouldWalkHeapRootsForEtw, BOOL fShouldWalkHeapObjectsForEtw)
{
    {
        ProfilingScanContext SC(fProfilerPinned);

        // **** Scan roots:  Only scan roots if profiling API wants them or ETW wants them.
        if (fProfilerPinned || fShouldWalkHeapRootsForEtw)
        {
#ifdef MULTIPLE_HEAPS
            int hn;

            // Must emulate each GC thread number so we can hit each
            // heap for enumerating the roots.
            for (hn = 0; hn < gc_heap::n_heaps; hn++)
            {
                // Ask the vm to go over all of the roots for this specific
                // heap.
                gc_heap* hp = gc_heap::g_heaps [hn];
                SC.thread_number = hn;
                GCScan::GcScanRoots(&ProfScanRootsHelper, max_generation, max_generation, &SC);

                // The finalizer queue is also a source of roots
                SC.dwEtwRootKind = kEtwGCRootKindFinalizer;
                hp->finalize_queue->GcScanRoots(&ScanRootsHelper, hn, &SC);
            }
#else
            // Ask the vm to go over all of the roots
            GCScan::GcScanRoots(&ProfScanRootsHelper, max_generation, max_generation, &SC);

            // The finalizer queue is also a source of roots
            SC.dwEtwRootKind = kEtwGCRootKindFinalizer;
            pGenGCHeap->finalize_queue->GcScanRoots(&ScanRootsHelper, 0, &SC);

#endif // MULTIPLE_HEAPS
            // Handles are kept independent of wks/svr/concurrent builds
            SC.dwEtwRootKind = kEtwGCRootKindHandle;
            GCScan::GcScanHandlesForProfilerAndETW(max_generation, &SC);

            // indicate that regular handle scanning is over, so we can flush the buffered roots
            // to the profiler.  (This is for profapi only.  ETW will flush after the
            // entire heap was is complete, via ETW::GCLog::EndHeapDump.)
#if defined (GC_PROFILING)
            if (fProfilerPinned)
            {
                g_profControlBlock.pProfInterface->EndRootReferences2(&SC.pHeapId);
            }
#endif // defined (GC_PROFILING)
        }

        // **** Scan dependent handles: only if the profiler supports it or ETW wants roots
        if ((fProfilerPinned && CORProfilerTrackConditionalWeakTableElements()) ||
            fShouldWalkHeapRootsForEtw)
        {
            // GcScanDependentHandlesForProfiler double-checks
            // CORProfilerTrackConditionalWeakTableElements() before calling into the profiler

            GCScan::GcScanDependentHandlesForProfilerAndETW(max_generation, &SC);

            // indicate that dependent handle scanning is over, so we can flush the buffered roots
            // to the profiler.  (This is for profapi only.  ETW will flush after the
            // entire heap was is complete, via ETW::GCLog::EndHeapDump.)
            if (fProfilerPinned && CORProfilerTrackConditionalWeakTableElements())
            {
                g_profControlBlock.pProfInterface->EndConditionalWeakTableElementReferences(&SC.pHeapId);
            }
        }

        ProfilerWalkHeapContext profilerWalkHeapContext(fProfilerPinned, SC.pvEtwContext);

        // **** Walk objects on heap: only if profiling API wants them or ETW wants them.
        if (fProfilerPinned || fShouldWalkHeapObjectsForEtw)
        {
#ifdef MULTIPLE_HEAPS
            int hn;

            // Walk the heap and provide the objref to the profiler
            for (hn = 0; hn < gc_heap::n_heaps; hn++)
            {
                gc_heap* hp = gc_heap::g_heaps [hn];         
                hp->walk_heap(&HeapWalkHelper, &profilerWalkHeapContext, max_generation, TRUE /* walk the large object heap */);
            }
#else
            gc_heap::walk_heap(&HeapWalkHelper, &profilerWalkHeapContext, max_generation, TRUE);
#endif //MULTIPLE_HEAPS
        }

        // **** Done! Indicate to ETW helpers that the heap walk is done, so any buffers
        // should be flushed into the ETW stream
        if (fShouldWalkHeapObjectsForEtw || fShouldWalkHeapRootsForEtw)
        {
            ETW::GCLog::EndHeapDump(&profilerWalkHeapContext);
        }
    }
}
#endif // defined(GC_PROFILING)

void GCProfileWalkHeap()
{
    BOOL fWalkedHeapForProfiler = FALSE;

#ifdef FEATURE_EVENT_TRACE
    if (ETW::GCLog::ShouldWalkStaticsAndCOMForEtw())
        ETW::GCLog::WalkStaticsAndCOMForETW();
    
    BOOL fShouldWalkHeapRootsForEtw = ETW::GCLog::ShouldWalkHeapRootsForEtw();
    BOOL fShouldWalkHeapObjectsForEtw = ETW::GCLog::ShouldWalkHeapObjectsForEtw();
#else // !FEATURE_EVENT_TRACE
    BOOL fShouldWalkHeapRootsForEtw = FALSE;
    BOOL fShouldWalkHeapObjectsForEtw = FALSE;
#endif // FEATURE_EVENT_TRACE

#if defined (GC_PROFILING)
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        GCProfileWalkHeapWorker(TRUE /* fProfilerPinned */, fShouldWalkHeapRootsForEtw, fShouldWalkHeapObjectsForEtw);
        fWalkedHeapForProfiler = TRUE;
        END_PIN_PROFILER();
    }
#endif // defined (GC_PROFILING)

#if  defined (GC_PROFILING)//UNIXTODO: Enable this for FEATURE_EVENT_TRACE
    // If the profiling API didn't want us to walk the heap but ETW does, then do the
    // walk here
    if (!fWalkedHeapForProfiler && 
        (fShouldWalkHeapRootsForEtw || fShouldWalkHeapObjectsForEtw))
    {
        GCProfileWalkHeapWorker(FALSE /* fProfilerPinned */, fShouldWalkHeapRootsForEtw, fShouldWalkHeapObjectsForEtw);
    }
#endif // FEATURE_EVENT_TRACE
}

BOOL GCHeap::IsGCInProgressHelper (BOOL bConsiderGCStart)
{
    return GcInProgress || (bConsiderGCStart? VolatileLoad(&gc_heap::gc_started) : FALSE);
}

uint32_t GCHeap::WaitUntilGCComplete(BOOL bConsiderGCStart)
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
            //  Even in retail, stop in the debugger if available.  Ideally, the
            //  following would use DebugBreak, but debspew.h makes this a null
            //  macro in retail.  Note that in debug, we don't use the debspew.h
            //  macros because these take a critical section that may have been
            //  taken by a suspended thread.
            FreeBuildDebugBreak();
            goto BlockAgain;
        }

#else  //DETECT_DEADLOCK
        
        dwWaitResult = WaitForGCEvent->Wait(INFINITE, FALSE );
        
#endif //DETECT_DEADLOCK
    }

    return dwWaitResult;
}

void GCHeap::SetGCInProgress(BOOL fInProgress)
{
    GcInProgress = fInProgress;
}

CLREvent * GCHeap::GetWaitForGCEvent()
{
    return WaitForGCEvent;
}

void GCHeap::WaitUntilConcurrentGCComplete()
{
#ifdef BACKGROUND_GC
    if (pGenGCHeap->settings.concurrent)
        pGenGCHeap->background_gc_wait();
#endif //BACKGROUND_GC
}

BOOL GCHeap::IsConcurrentGCInProgress()
{
#ifdef BACKGROUND_GC
    return pGenGCHeap->settings.concurrent;
#else
    return FALSE;
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
    TypeHandle th = GetThread()->GetTHAllocContextObj();
    if (th != 0)
    {
        InlineSString<MAX_CLASSNAME_LENGTH> strTypeName;
        th.GetName(strTypeName);
        typeId = th.GetMethodTable();
        name = strTypeName.GetUnicode();
    }
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

uint32_t gc_heap::user_thread_wait (CLREvent *event, BOOL no_mode_change, int time_out_ms)
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
::GCHeap* CreateGCHeap() {
    return new(nothrow) GCHeap();   // we return wks or svr 
}

void GCHeap::TraceGCSegments()
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

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
void GCHeap::DescrGenerationsToProfiler (gen_walk_fn fn, void *context)
{
    pGenGCHeap->descr_generations_to_profiler(fn, context);
}
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

#if defined(BACKGROUND_GC) && defined(FEATURE_REDHAWK)

// Helper used to wrap the start routine of background GC threads so we can do things like initialize the
// Redhawk thread state which requires running in the new thread's context.
uint32_t WINAPI gc_heap::rh_bgc_thread_stub(void * pContext)
{
    rh_bgc_thread_ctx * pStartContext = (rh_bgc_thread_ctx*)pContext;

    // Initialize the Thread for this thread. The false being passed indicates that the thread store lock
    // should not be acquired as part of this operation. This is necessary because this thread is created in
    // the context of a garbage collection and the lock is already held by the GC.
    ASSERT(GCHeap::GetGCHeap()->IsGCInProgress());
    GCToEEInterface::AttachCurrentThread();

    // Inform the GC which Thread* we are.
    pStartContext->m_pRealContext->bgc_thread = GetThread();

    // Run the real start procedure and capture its return code on exit.
    return pStartContext->m_pRealStartRoutine(pStartContext->m_pRealContext);
}

#endif // BACKGROUND_GC && FEATURE_REDHAWK

#ifdef FEATURE_BASICFREEZE
segment_handle GCHeap::RegisterFrozenSegment(segment_info *pseginfo)
{
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
}

void GCHeap::UnregisterFrozenSegment(segment_handle seg)
{
#if defined (MULTIPLE_HEAPS) && !defined (ISOLATED_HEAPS)
    gc_heap* heap = gc_heap::g_heaps[0];
#else
    gc_heap* heap = pGenGCHeap;
#endif //MULTIPLE_HEAPS && !ISOLATED_HEAPS

    heap->remove_ro_segment(reinterpret_cast<heap_segment*>(seg));
}
#endif // FEATURE_BASICFREEZE


#endif // !DACCESS_COMPILE


