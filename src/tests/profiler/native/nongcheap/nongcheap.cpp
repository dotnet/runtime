// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "nongcheap.h"

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

HRESULT NonGcHeapProfiler::Shutdown()
{
    if (_failures > 0)
    {
        printf("PROFILER TEST FAILS\n");
    }
    else if (_nonGcHeapObjects == 0)
    {
        printf("PROFILER TEST FAILS: non-GC heap objects were not allocated\n");
    }
    else
    {
        printf("PROFILER TEST PASSES\n");
    }
    printf("Non-GC objects allocated: %d\n", (int)_nonGcHeapObjects);
    printf("PROFILER TEST PASSES\n");
    fflush(stdout);
    return S_OK;
}
