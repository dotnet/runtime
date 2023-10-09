// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "nongcheap.h"
#include <vector>

GUID NonGcHeapProfiler::GetClsid()
{
    // {EF0D191C-3FC7-4311-88AF-E474CBEB2859}
    GUID clsid = { 0xef0d191c, 0x3fc7, 0x4311, { 0x88, 0xaf, 0xe4, 0x74, 0xcb, 0xeb, 0x28, 0x59 } };
    return clsid;
}

HRESULT NonGcHeapProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(
        COR_PRF_ENABLE_OBJECT_ALLOCATED | COR_PRF_MONITOR_OBJECT_ALLOCATED,
        COR_PRF_HIGH_BASIC_GC)))
    {
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE NonGcHeapProfiler::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    SHUTDOWNGUARD();
    
    COR_PRF_GC_GENERATION_RANGE gen;
    HRESULT hr = pCorProfilerInfo->GetObjectGeneration(objectId, &gen);

    // non-GC objects (same for GC.GetGeneration() API) have generation = -1
    if (gen.generation == (COR_PRF_GC_GENERATION)INT32_MAX)
    {
        if (!FAILED(hr))
        {
            // We expect GetObjectGeneration to return an error (CORPROF_E_NOT_GC_OBJECT)
            // for non-GC objects.
            _failures++;
        }
        _nonGcHeapObjects++;
        if (gen.rangeLength != 0 || gen.rangeLengthReserved != 0 || gen.rangeStart != 0)
        {
            _failures++;
        }
    }
    else if (FAILED(hr))
    {
        _failures++;
    }
    return S_OK;
}

HRESULT NonGcHeapProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();

    _garbageCollections++;

    std::vector<uint64_t> segment_starts;
    std::vector<uint64_t> segment_ends;
    const int MAX_SEGMENTS = 16;
    COR_PRF_NONGC_HEAP_RANGE nongc_segments[MAX_SEGMENTS];
    COR_PRF_GC_GENERATION_RANGE gc_segments[MAX_SEGMENTS];
    ULONG segCount;
    ObjectID firstObj = 0;
    HRESULT hr = pCorProfilerInfo->GetNonGCHeapBounds(MAX_SEGMENTS, &segCount, nongc_segments);
    if (FAILED(hr))
    {
        printf("FAIL: GetNonGCHeapBounds returned an error\n!");
        _failures++;
    }
    else if (segCount == 0 || segCount > MAX_SEGMENTS)
    {
        printf("FAIL: GetNonGCHeapBounds: invalid segCount (%u)\n!", (uint32_t)segCount);
        _failures++;
    }
    else
    {
        // Save very first object ID to compare with EnumerateNonGCObjects
        firstObj = nongc_segments[0].rangeStart;

        printf("\nGetNonGCHeapBounds (segCount = %u):\n", (uint32_t)segCount);
        for (uint32_t i = 0; i < (uint32_t)segCount; i++)
        {
            printf("\tseg#%u, rangeStart=%p, rangeLength=%u, rangeLengthReserved=%u\n",
                i, (void*)nongc_segments[i].rangeStart, (uint32_t)nongc_segments[i].rangeLength, (uint32_t)nongc_segments[i].rangeLengthReserved);

            if (nongc_segments[i].rangeLength > nongc_segments[i].rangeLengthReserved)
            {
                printf("FAIL: GetNonGCHeapBounds: rangeLength > rangeLengthReserved");
                _failures++;
            }

            if (!nongc_segments[i].rangeStart)
            {
                printf("FAIL: GetNonGCHeapBounds: rangeStart is null");
                _failures++;
            }
            segment_starts.push_back(nongc_segments[i].rangeStart);
            segment_ends.push_back(nongc_segments[i].rangeStart + nongc_segments[i].rangeLengthReserved);
        }
        printf("\n");
    }
    hr = pCorProfilerInfo->GetGenerationBounds(MAX_SEGMENTS, &segCount, gc_segments);
    if (FAILED(hr))
    {
        printf("FAIL: GetGenerationBounds returned an error\n!");
        _failures++;
    }
    else if (segCount == 0 || segCount > MAX_SEGMENTS)
    {
        printf("FAIL: GetGenerationBounds: invalid segCount (%u)\n!", (uint32_t)segCount);
        _failures++;
    }
    else
    {
        printf("\nGetGenerationBounds (segCount = %u):\n", (uint32_t)segCount);
        for (uint64_t i = 0; i < segCount; i++)
        {
            printf("\tseg#%u, rangeStart=%p, rangeLength=%u, rangeLengthReserved=%u\n",
                (uint32_t)i, (void*)gc_segments[i].rangeStart, (uint32_t)gc_segments[i].rangeLength, (uint32_t)gc_segments[i].rangeLengthReserved);

            if (gc_segments[i].rangeLength > gc_segments[i].rangeLengthReserved)
            {
                printf("FAIL: GetGenerationBounds: rangeLength > rangeLengthReserved");
                _failures++;
            }

            if (!gc_segments[i].rangeStart)
            {
                printf("FAIL: GetGenerationBounds: rangeStart is null");
                _failures++;
            }
            segment_starts.push_back(gc_segments[i].rangeStart);
            segment_ends.push_back(gc_segments[i].rangeStart + gc_segments[i].rangeLengthReserved);
        }
        printf("\n");
    }
    sort(segment_starts.begin(), segment_starts.end());
    sort(segment_ends.begin(), segment_ends.end());

    // Do segments overlap?
    if (segment_starts.size() > 1)
    {
        for (size_t i = 0; i < segment_starts.size() - 1; i++)
        {
            printf("inspecting segment %d [rangeStart=%p rangeEnd=%p]\n", (int)i, (void*)segment_starts[i], (void*)segment_ends[i]);

            if (segment_starts[i] == segment_starts[i+1])
            {
                printf("FAIL: Duplicated segment starts");
                _failures++;
            }
            if (segment_ends[i] == segment_ends[i+1])
            {
                printf("FAIL: Duplicated segment ends");
                _failures++;
            }
            if (segment_ends[i] > segment_starts[i+1])
            {
                printf("FAIL: Overlapping segments\n");
                _failures++;
            }
        }
    }

    // Let's make sure we got the same number of objects as we got from the callback
    // by testing the EnumerateNonGCObjects API.
    ICorProfilerObjectEnum* pEnum = NULL;
    hr = pCorProfilerInfo->EnumerateNonGCObjects(&pEnum);
    if (FAILED(hr))
    {
        printf("FAIL: EnumerateNonGCObjects returned an error\n!");
        _failures++;
    }
    else
    {
        int nonGcObjectsEnumerated = 0;
        ObjectID obj;
        bool isFirstObj = true;
        while (pEnum->Next(1, &obj, NULL) == S_OK)
        {
            if (isFirstObj)
            {
                if (firstObj != obj)
                {
                    printf("FAIL: EnumerateNonGCObjects: firstObj != obj\n!");
                    _failures++;
                }
            }

            // Add test coverage for IsFrozenObject API, currently, it is expected to return true
            // for objects from non-GC heap (it might also return true for frozen segments we don't track)
            BOOL isFrozen;
            hr = pCorProfilerInfo->IsFrozenObject(obj, &isFrozen);
            if (FAILED(hr) || !isFrozen)
            {
                printf("FAIL: EnumerateNonGCObjects: IsFrozenObject failed\n!");
                _failures++;
            }

            isFirstObj = false;
            nonGcObjectsEnumerated++;
        }

        if (nonGcObjectsEnumerated != _nonGcHeapObjects)
        {
            printf("FAIL: objectAllocated(%d) != _nonGcHeapObjects(%d)\n!", nonGcObjectsEnumerated, (int)_nonGcHeapObjects);
            _failures++;
        }
    }

    return S_OK;
}

HRESULT NonGcHeapProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_garbageCollections == 0)
    {
        printf("PROFILER TEST FAILS: no garbage collections were triggered\n");
        _failures++;
    }

    if (_nonGcHeapObjects == 0)
    {
        printf("PROFILER TEST FAILS: non-GC heap objects were not allocated\n");
        _failures++;
    }

    if (_failures > 0)
    {
        printf("PROFILER TEST FAILS\n");
    }
    else
    {
        printf("PROFILER TEST PASSES\n");
    }
    printf("Non-GC objects allocated: %d\n", (int)_nonGcHeapObjects);
    fflush(stdout);
    return S_OK;
}
