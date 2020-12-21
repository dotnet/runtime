// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// File: request.cpp
//
// CorDataAccess::Request implementation.
//
//*****************************************************************************

#include "stdafx.h"
#include "dacdbiinterface.h"
#include "dacdbiimpl.h"

#if defined(FEATURE_SVR_GC)

#include <sigformat.h>
#include <win32threadpool.h>
#include "request_common.h"

int GCHeapCount()
{
    if (g_gcDacGlobals->n_heaps == nullptr)
        return 0;
    return *g_gcDacGlobals->n_heaps;
}

HRESULT GetServerHeapData(CLRDATA_ADDRESS addr, DacpHeapSegmentData *pSegment)
{
    // get field values (target addresses) for the heap segment at addr
    if (!addr)
    {
        // PREfix.
        return E_INVALIDARG;
    }

    // marshal the segment from target to host
    dac_heap_segment *pHeapSegment = __DPtr<dac_heap_segment>(TO_TADDR(addr));

    // initialize fields by copying from the marshaled segment (note that these are all target addresses)
    pSegment->segmentAddr = addr;
    pSegment->allocated = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->allocated;
    pSegment->committed = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->committed;
    pSegment->reserved = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->reserved;
    pSegment->used = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->used;
    pSegment->mem = (CLRDATA_ADDRESS)(ULONG_PTR) (pHeapSegment->mem);
    pSegment->next = (CLRDATA_ADDRESS)dac_cast<TADDR>(pHeapSegment->next);
    pSegment->gc_heap = (CLRDATA_ADDRESS)pHeapSegment->heap;

    return S_OK;
}

HRESULT GetServerHeaps(CLRDATA_ADDRESS pGCHeaps[], ICorDebugDataTarget * pTarget)
{
    // @todo Microsoft: It would be good to have an assert here to ensure pGCHeaps is large enough to
    // hold all the addresses. Currently we check that in the only caller, but if we were to call this from
    // somewhere else in the future, we could have a buffer overrun.

    // The runtime declares its own global array of gc heap addresses for multiple heap scenarios. We need to get
    // its starting address. This expression is a little tricky to parse, but in DAC builds, g_heaps is
    // a DAC global (__GlobalPtr). The __GlobalPtr<...>::GetAddr() function gets the starting address of that global, but
    // be sure to note this is a target address. We'll use this as our source for getting our local list of
    // heap addresses.
    for (int i = 0; i < GCHeapCount(); i++)
    {
        pGCHeaps[i] = (CLRDATA_ADDRESS)HeapTableIndex(g_gcDacGlobals->g_heaps, i).GetAddr();
    }

    return S_OK;
}

#define PTR_CDADDR(ptr)   TO_CDADDR(PTR_TO_TADDR(ptr))
#define HOST_CDADDR(host) TO_CDADDR(PTR_HOST_TO_TADDR(host))

HRESULT ClrDataAccess::GetServerAllocData(unsigned int count, struct DacpGenerationAllocData *data, unsigned int *pNeeded)
{
    unsigned int heaps = (unsigned int)GCHeapCount();
    if (pNeeded)
        *pNeeded = heaps;

    if (data)
    {
        if (count > heaps)
            count = heaps;

        for (unsigned int n=0; n < heaps; n++)
        {
            DPTR(dac_gc_heap) pHeap = HeapTableIndex(g_gcDacGlobals->g_heaps, n);
            for (int i=0;i<NUMBERGENERATIONS;i++)
            {
                dac_generation generation = *ServerGenerationTableIndex(pHeap, i);
                data[n].allocData[i].allocBytes = (CLRDATA_ADDRESS)(ULONG_PTR) generation.allocation_context.alloc_bytes;
                data[n].allocData[i].allocBytesLoh = (CLRDATA_ADDRESS)(ULONG_PTR) generation.allocation_context.alloc_bytes_uoh;
            }
        }
    }

    return S_OK;
}

HRESULT ClrDataAccess::ServerGCHeapDetails(CLRDATA_ADDRESS heapAddr, DacpGcHeapDetails *detailsData)
{
    if (!heapAddr)
    {
        // PREfix.
        return E_INVALIDARG;
    }

    DPTR(dac_gc_heap) pHeap = __DPtr<dac_gc_heap>(TO_TADDR(heapAddr));
    int i;

    //get global information first
    detailsData->heapAddr = heapAddr;

    detailsData->lowest_address = PTR_CDADDR(g_lowest_address);
    detailsData->highest_address = PTR_CDADDR(g_highest_address);
    detailsData->card_table = PTR_CDADDR(g_card_table);

    // now get information specific to this heap (server mode gives us several heaps; we're getting
    // information about only one of them.
    detailsData->alloc_allocated = (CLRDATA_ADDRESS)pHeap->alloc_allocated;
    detailsData->ephemeral_heap_segment = (CLRDATA_ADDRESS)dac_cast<TADDR>(pHeap->ephemeral_heap_segment);

    // get bounds for the different generations
    for (i=0; i<NUMBERGENERATIONS; i++)
    {
        DPTR(dac_generation) generation = ServerGenerationTableIndex(pHeap, i);
        detailsData->generation_table[i].start_segment     = (CLRDATA_ADDRESS)dac_cast<TADDR>(generation->start_segment);
        detailsData->generation_table[i].allocation_start   = (CLRDATA_ADDRESS)(ULONG_PTR)generation->allocation_start;
        DPTR(gc_alloc_context) alloc_context = dac_cast<TADDR>(generation) + offsetof(dac_generation, allocation_context);
        detailsData->generation_table[i].allocContextPtr    = (CLRDATA_ADDRESS)(ULONG_PTR) alloc_context->alloc_ptr;
        detailsData->generation_table[i].allocContextLimit = (CLRDATA_ADDRESS)(ULONG_PTR) alloc_context->alloc_limit;
    }

    DPTR(dac_finalize_queue) fq = pHeap->finalize_queue;
    DPTR(uint8_t*) pFillPointerArray= dac_cast<TADDR>(fq) + offsetof(dac_finalize_queue, m_FillPointers);
    for(i=0; i<(NUMBERGENERATIONS+dac_finalize_queue::ExtraSegCount); i++)
    {
        detailsData->finalization_fill_pointers[i] = (CLRDATA_ADDRESS) pFillPointerArray[i];
    }

    return S_OK;
}

HRESULT
ClrDataAccess::ServerOomData(CLRDATA_ADDRESS addr, DacpOomData *oomData)
{
    DPTR(dac_gc_heap) pHeap = __DPtr<dac_gc_heap>(TO_TADDR(addr));

    oom_history pOOMInfo = pHeap->oom_info;
    oomData->reason = pOOMInfo.reason;
    oomData->alloc_size = pOOMInfo.alloc_size;
    oomData->available_pagefile_mb = pOOMInfo.available_pagefile_mb;
    oomData->gc_index = pOOMInfo.gc_index;
    oomData->fgm = pOOMInfo.fgm;
    oomData->size = pOOMInfo.size;
    oomData->loh_p = pOOMInfo.loh_p;

    return S_OK;
}

HRESULT
ClrDataAccess::ServerGCInterestingInfoData(CLRDATA_ADDRESS addr, DacpGCInterestingInfoData *interestingInfoData)
{
#ifdef GC_CONFIG_DRIVEN
    dac_gc_heap *pHeap = __DPtr<dac_gc_heap>(TO_TADDR(addr));

    size_t* dataPoints = (size_t*)&(pHeap->interesting_data_per_heap);
    for (int i = 0; i < NUM_GC_DATA_POINTS; i++)
        interestingInfoData->interestingDataPoints[i] = dataPoints[i];
    size_t* mechanisms = (size_t*)&(pHeap->compact_reasons_per_heap);
    for (int i = 0; i < MAX_COMPACT_REASONS_COUNT; i++)
        interestingInfoData->compactReasons[i] = mechanisms[i];
    mechanisms = (size_t*)&(pHeap->expand_mechanisms_per_heap);
    for (int i = 0; i < MAX_EXPAND_MECHANISMS_COUNT; i++)
        interestingInfoData->expandMechanisms[i] = mechanisms[i];
    mechanisms = (size_t*)&(pHeap->interesting_mechanism_bits_per_heap);
    for (int i = 0; i < MAX_GC_MECHANISM_BITS_COUNT; i++)
        interestingInfoData->bitMechanisms[i] = mechanisms[i];

    return S_OK;
#else
    return E_NOTIMPL;
#endif //GC_CONFIG_DRIVEN
}

HRESULT ClrDataAccess::ServerGCHeapAnalyzeData(CLRDATA_ADDRESS heapAddr, DacpGcHeapAnalyzeData *analyzeData)
{
    if (!heapAddr)
    {
        // PREfix.
        return E_INVALIDARG;
    }

    DPTR(dac_gc_heap) pHeap = __DPtr<dac_gc_heap>(TO_TADDR(heapAddr));

    analyzeData->heapAddr = heapAddr;
    analyzeData->internal_root_array = (CLRDATA_ADDRESS)pHeap->internal_root_array;
    analyzeData->internal_root_array_index = (size_t)pHeap->internal_root_array_index;
    analyzeData->heap_analyze_success = (BOOL)pHeap->heap_analyze_success;

    return S_OK;
}

void
ClrDataAccess::EnumSvrGlobalMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    if (g_gcDacGlobals->n_heaps == nullptr || g_gcDacGlobals->g_heaps == nullptr)
        return;

    g_gcDacGlobals->n_heaps.EnumMem();

    int heaps = *g_gcDacGlobals->n_heaps;
    DacEnumMemoryRegion(g_gcDacGlobals->g_heaps.GetAddr(), sizeof(TADDR) * heaps);

    g_gcDacGlobals->gc_structures_invalid_cnt.EnumMem();
    g_gcDacGlobals->g_heaps.EnumMem();

    for (int i = 0; i < heaps; i++)
    {
        DPTR(dac_gc_heap) pHeap = HeapTableIndex(g_gcDacGlobals->g_heaps, i);

        size_t gen_table_size = g_gcDacGlobals->generation_size * (*g_gcDacGlobals->max_gen + 2);
        DacEnumMemoryRegion(dac_cast<TADDR>(pHeap), sizeof(dac_gc_heap));
        DacEnumMemoryRegion(dac_cast<TADDR>(pHeap->finalize_queue), sizeof(dac_finalize_queue));

        TADDR taddrTable = dac_cast<TADDR>(pHeap) + offsetof(dac_gc_heap, generation_table);
        DacEnumMemoryRegion(taddrTable, gen_table_size);

        // enumerating the generations from max (which is normally gen2) to max+1 gives you
        // the segment list for all the normal segements plus the large heap segment (max+1)
        // this is the convention in the GC so it is repeated here
        for (ULONG i = *g_gcDacGlobals->max_gen; i <= *g_gcDacGlobals->max_gen +1; i++)
        {
            DPTR(dac_heap_segment) seg = ServerGenerationTableIndex(pHeap, i)->start_segment;
            while (seg)
            {
                    DacEnumMemoryRegion(PTR_HOST_TO_TADDR(seg), sizeof(dac_heap_segment));

                    seg = seg->next;
            }
        }
    }
}

DWORD DacGetNumHeaps()
{
    if (g_heap_type == GC_HEAP_SVR)
        return (DWORD)*g_gcDacGlobals->n_heaps;

    // workstation gc
    return 1;
}

HRESULT DacHeapWalker::InitHeapDataSvr(HeapData *&pHeaps, size_t &pCount)
{
    if (g_gcDacGlobals->n_heaps == nullptr || g_gcDacGlobals->g_heaps == nullptr)
        return S_OK;

    // Scrape basic heap details
    int heaps = *g_gcDacGlobals->n_heaps;
    pCount = heaps;
    pHeaps = new (nothrow) HeapData[heaps];
    if (pHeaps == NULL)
        return E_OUTOFMEMORY;

    for (int i = 0; i < heaps; ++i)
    {
        // Basic heap info.
        DPTR(dac_gc_heap) heap = HeapTableIndex(g_gcDacGlobals->g_heaps, i);
        dac_generation gen0 = *ServerGenerationTableIndex(heap, 0);
        dac_generation gen1 = *ServerGenerationTableIndex(heap, 1);
        dac_generation gen2 = *ServerGenerationTableIndex(heap, 2);
        dac_generation loh  = *ServerGenerationTableIndex(heap, 3);
        dac_generation poh  = *ServerGenerationTableIndex(heap, 4);

        pHeaps[i].YoungestGenPtr = (CORDB_ADDRESS)gen0.allocation_context.alloc_ptr;
        pHeaps[i].YoungestGenLimit = (CORDB_ADDRESS)gen0.allocation_context.alloc_limit;

        pHeaps[i].Gen0Start = (CORDB_ADDRESS)gen0.allocation_start;
        pHeaps[i].Gen0End = (CORDB_ADDRESS)heap->alloc_allocated;
        pHeaps[i].Gen1Start = (CORDB_ADDRESS)gen1.allocation_start;

        // Segments
        int count = GetSegmentCount(loh.start_segment);
        count += GetSegmentCount(poh.start_segment);
        count += GetSegmentCount(gen2.start_segment);

        pHeaps[i].SegmentCount = count;
        pHeaps[i].Segments = new (nothrow) SegmentData[count];
        if (pHeaps[i].Segments == NULL)
            return E_OUTOFMEMORY;

        // Small object heap segments
        DPTR(dac_heap_segment) seg = gen2.start_segment;
        int j = 0;
        for (; seg && (j < count); ++j)
        {
            pHeaps[i].Segments[j].Start = (CORDB_ADDRESS)seg->mem;
            if (seg.GetAddr() == heap->ephemeral_heap_segment.GetAddr())
            {
                pHeaps[i].Segments[j].End = (CORDB_ADDRESS)heap->alloc_allocated;
                pHeaps[i].EphemeralSegment = j;
                pHeaps[i].Segments[j].Generation = 1;
            }
            else
            {
                pHeaps[i].Segments[j].End = (CORDB_ADDRESS)seg->allocated;
                pHeaps[i].Segments[j].Generation = 2;
            }

            seg = seg->next;
        }

        // Large object heap segments
        seg = loh.start_segment;
        for (; seg && (j < count); ++j)
        {
            pHeaps[i].Segments[j].Generation = 3;
            pHeaps[i].Segments[j].Start = (CORDB_ADDRESS)seg->mem;
            pHeaps[i].Segments[j].End = (CORDB_ADDRESS)seg->allocated;

            seg = seg->next;
        }

        // Pinned object heap segments
        seg = poh.start_segment;
        for (; seg && (j < count); ++j)
        {
            pHeaps[i].Segments[j].Generation = 4;
            pHeaps[i].Segments[j].Start = (CORDB_ADDRESS)seg->mem;
            pHeaps[i].Segments[j].End = (CORDB_ADDRESS)seg->allocated;

            seg = seg->next;
        }
    }

    return S_OK;
}

#endif // defined(FEATURE_SVR_GC)
