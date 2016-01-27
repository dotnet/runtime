// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#include <gceesvr.cpp>


int GCHeapCount()
{
    return SVR::gc_heap::n_heaps;
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
    SVR::heap_segment *pHeapSegment =
        __DPtr<SVR::heap_segment>(TO_TADDR(addr));

    // initialize fields by copying from the marshaled segment (note that these are all target addresses)
    pSegment->segmentAddr = addr;
    pSegment->allocated = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->allocated;
    pSegment->committed = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->committed;
    pSegment->reserved = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->reserved;
    pSegment->used = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->used;
    pSegment->mem = (CLRDATA_ADDRESS)(ULONG_PTR) (pHeapSegment->mem);
    pSegment->next = (CLRDATA_ADDRESS)dac_cast<TADDR>(pHeapSegment->next);
    pSegment->gc_heap = (CLRDATA_ADDRESS)(ULONG_PTR) pHeapSegment->heap;

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
    TADDR ptr = SVR::gc_heap::g_heaps.GetAddr();
    ULONG32 bytesRead = 0;    
    
    for (int i=0;i<GCHeapCount();i++)
    {
        
        LPVOID pGCHeapAddr;

        // read the i-th element of g_heaps into pGCHeapAddr
        // @todo Microsoft: Again, if we capture the HRESULT from ReadVirtual, we can print a more explanatory
        // failure message. 
        if (pTarget->ReadVirtual(ptr + i*sizeof(TADDR),
                                 (PBYTE) &pGCHeapAddr, sizeof(TADDR),
                                 &bytesRead) != S_OK)
        {
            return E_FAIL;
        }
        if (bytesRead != sizeof(LPVOID))
        {
            return E_FAIL;
        }

        // store the heap's starting address in our array. 
        pGCHeaps[i] = (CLRDATA_ADDRESS)(ULONG_PTR) pGCHeapAddr;
    }
    return S_OK;
}

#define PTR_CDADDR(ptr)   TO_CDADDR(PTR_TO_TADDR(ptr))
#define HOST_CDADDR(host) TO_CDADDR(PTR_HOST_TO_TADDR(host))

typedef DPTR(class SVR::gc_heap)                        PTR_SVR_gc_heap;

HRESULT ClrDataAccess::GetServerAllocData(unsigned int count, struct DacpGenerationAllocData *data, unsigned int *pNeeded)
{
    unsigned int heaps = (unsigned int)SVR::gc_heap::n_heaps;
    if (pNeeded)
        *pNeeded = heaps;

    if (data)
    {
        if (count > heaps)
            count = heaps;
        
        for (int n=0;n<SVR::gc_heap::n_heaps;n++)
        {
            PTR_SVR_gc_heap pHeap = PTR_SVR_gc_heap(SVR::gc_heap::g_heaps[n]);
            for (int i=0;i<NUMBERGENERATIONS;i++)
            {
                data[n].allocData[i].allocBytes = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->generation_table[i].allocation_context.alloc_bytes;
                data[n].allocData[i].allocBytesLoh = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->generation_table[i].allocation_context.alloc_bytes_loh;
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

    SVR::gc_heap *pHeap = PTR_SVR_gc_heap(TO_TADDR(heapAddr));
    int i;

    //get global information first
    detailsData->heapAddr = heapAddr;
    
    detailsData->lowest_address = PTR_CDADDR(g_lowest_address);
    detailsData->highest_address = PTR_CDADDR(g_highest_address);
    detailsData->card_table = PTR_CDADDR(g_card_table);
    
    // now get information specific to this heap (server mode gives us several heaps; we're getting
    // information about only one of them. 
    detailsData->alloc_allocated = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->alloc_allocated;
    detailsData->ephemeral_heap_segment = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->ephemeral_heap_segment;

    // get bounds for the different generations
    for (i=0; i<NUMBERGENERATIONS; i++)
    {
        detailsData->generation_table[i].start_segment     = (CLRDATA_ADDRESS)dac_cast<TADDR>(pHeap->generation_table[i].start_segment);
        detailsData->generation_table[i].allocation_start   = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->generation_table[i].allocation_start;        
        detailsData->generation_table[i].allocContextPtr    = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->generation_table[i].allocation_context.alloc_ptr;
        detailsData->generation_table[i].allocContextLimit = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->generation_table[i].allocation_context.alloc_limit;
    }

    // since these are all TADDRS, we have to compute the address of the m_FillPointers field explicitly
    TADDR pFillPointerArray = dac_cast<TADDR>(pHeap->finalize_queue) + offsetof(SVR::CFinalize,m_FillPointers);

    for(i=0; i<(NUMBERGENERATIONS+SVR::CFinalize::ExtraSegCount); i++)
    {
        ULONG32 returned = 0;
        size_t pValue;
        HRESULT hr = m_pTarget->ReadVirtual(pFillPointerArray+(i*sizeof(TADDR)),
                                            (PBYTE)&pValue,
                                            sizeof(TADDR),
                                            &returned);
        if (FAILED(hr) || (returned != sizeof(TADDR)))
        {
            return E_FAIL;
        }

        detailsData->finalization_fill_pointers[i] = (CLRDATA_ADDRESS) pValue;
    }

    return S_OK;
}

HRESULT 
ClrDataAccess::ServerOomData(CLRDATA_ADDRESS addr, DacpOomData *oomData)
{
    SVR::gc_heap *pHeap = PTR_SVR_gc_heap(TO_TADDR(addr));

    oom_history* pOOMInfo = (oom_history*)((TADDR)pHeap + offsetof(SVR::gc_heap,oom_info));
    oomData->reason = pOOMInfo->reason;
    oomData->alloc_size = pOOMInfo->alloc_size;
    oomData->available_pagefile_mb = pOOMInfo->available_pagefile_mb;
    oomData->gc_index = pOOMInfo->gc_index;
    oomData->fgm = pOOMInfo->fgm;
    oomData->size = pOOMInfo->size;
    oomData->loh_p = pOOMInfo->loh_p;

    return S_OK;
}

HRESULT 
ClrDataAccess::ServerGCInterestingInfoData(CLRDATA_ADDRESS addr, DacpGCInterestingInfoData *interestingInfoData)
{
#ifdef GC_CONFIG_DRIVEN
    SVR::gc_heap *pHeap = PTR_SVR_gc_heap(TO_TADDR(addr));

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

    SVR::gc_heap *pHeap = PTR_SVR_gc_heap(TO_TADDR(heapAddr));

    analyzeData->heapAddr = heapAddr;
    analyzeData->internal_root_array = (CLRDATA_ADDRESS)(ULONG_PTR) pHeap->internal_root_array;
    analyzeData->internal_root_array_index = (size_t) pHeap->internal_root_array_index;
    analyzeData->heap_analyze_success = (BOOL)pHeap->heap_analyze_success;

    return S_OK;
}

void
ClrDataAccess::EnumSvrGlobalMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    SVR::gc_heap::n_heaps.EnumMem();
    DacEnumMemoryRegion(SVR::gc_heap::g_heaps.GetAddr(),
                    sizeof(TADDR) * SVR::gc_heap::n_heaps);

    SVR::gc_heap::g_heaps.EnumMem();
    
    for (int i=0;i<SVR::gc_heap::n_heaps;i++)
    {
        PTR_SVR_gc_heap pHeap = PTR_SVR_gc_heap(SVR::gc_heap::g_heaps[i]);

        DacEnumMemoryRegion(dac_cast<TADDR>(pHeap), sizeof(SVR::gc_heap));
        DacEnumMemoryRegion(dac_cast<TADDR>(pHeap->finalize_queue), sizeof(SVR::CFinalize));

        // enumerating the generations from max (which is normally gen2) to max+1 gives you
        // the segment list for all the normal segements plus the large heap segment (max+1)
        // this is the convention in the GC so it is repeated here
        for (ULONG i = GCHeap::GetMaxGeneration(); i <= GCHeap::GetMaxGeneration()+1; i++)
        {
            __DPtr<SVR::heap_segment> seg = dac_cast<TADDR>(pHeap->generation_table[i].start_segment);
            while (seg)
            {
                    DacEnumMemoryRegion(PTR_HOST_TO_TADDR(seg), sizeof(SVR::heap_segment));

                    seg = __DPtr<SVR::heap_segment>(dac_cast<TADDR>(seg->next));
            }
        }
    }
}

DWORD DacGetNumHeaps()
{
    if (GCHeap::IsServerHeap())
        return (DWORD)SVR::gc_heap::n_heaps;
        
    // workstation gc
    return 1;
}

HRESULT DacHeapWalker::InitHeapDataSvr(HeapData *&pHeaps, size_t &pCount)
{
    // Scrape basic heap details
    int heaps = SVR::gc_heap::n_heaps;
    pCount = heaps;
    pHeaps = new (nothrow) HeapData[heaps];
    if (pHeaps == NULL)
        return E_OUTOFMEMORY;

    for (int i = 0; i < heaps; ++i)
    {
        // Basic heap info.
        PTR_SVR_gc_heap heap = PTR_SVR_gc_heap(SVR::gc_heap::g_heaps[i]);

        pHeaps[i].YoungestGenPtr = (CORDB_ADDRESS)heap->generation_table[0].allocation_context.alloc_ptr;
        pHeaps[i].YoungestGenLimit = (CORDB_ADDRESS)heap->generation_table[0].allocation_context.alloc_limit;

        pHeaps[i].Gen0Start = (CORDB_ADDRESS)heap->generation_table[0].allocation_start;
        pHeaps[i].Gen0End = (CORDB_ADDRESS)heap->alloc_allocated;
        pHeaps[i].Gen1Start = (CORDB_ADDRESS)heap->generation_table[1].allocation_start;
        
        // Segments
        int count = GetSegmentCount(heap->generation_table[NUMBERGENERATIONS-1].start_segment);
        count += GetSegmentCount(heap->generation_table[NUMBERGENERATIONS-2].start_segment);

        pHeaps[i].SegmentCount = count;
        pHeaps[i].Segments = new (nothrow) SegmentData[count];
        if (pHeaps[i].Segments == NULL)
            return E_OUTOFMEMORY;

        // Small object heap segments
        SVR::PTR_heap_segment seg = heap->generation_table[NUMBERGENERATIONS-2].start_segment;
        int j = 0;
        for (; seg && (j < count); ++j)
        {
            pHeaps[i].Segments[j].Start = (CORDB_ADDRESS)seg->mem;
            if (seg.GetAddr() == TO_TADDR(heap->ephemeral_heap_segment))
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
        seg = heap->generation_table[NUMBERGENERATIONS-1].start_segment;
        for (; seg && (j < count); ++j)
        {
            pHeaps[i].Segments[j].Generation = 3;
            pHeaps[i].Segments[j].Start = (CORDB_ADDRESS)seg->mem;
            pHeaps[i].Segments[j].End = (CORDB_ADDRESS)seg->allocated;
            
            seg = seg->next;
        }
    }

    return S_OK;
}

#endif // defined(FEATURE_SVR_GC)
