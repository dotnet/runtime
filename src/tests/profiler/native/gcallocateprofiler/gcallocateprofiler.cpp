// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcallocateprofiler.h"

GUID GCAllocateProfiler::GetClsid()
{
    // {55b9554d-6115-45a2-be1e-c80f7fa35369}
	GUID clsid = { 0x55b9554d, 0x6115, 0x45a2,{ 0xbe, 0x1e, 0xc8, 0x0f, 0x7f, 0xa3, 0x53, 0x69 } };
	return clsid;
}

HRESULT GCAllocateProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_ENABLE_OBJECT_ALLOCATED, COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED | COR_PRF_HIGH_MONITOR_PINNEDOBJECT_ALLOCATED)))
    {
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE GCAllocateProfiler::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    COR_PRF_GC_GENERATION_RANGE gen;
    HRESULT hr = pCorProfilerInfo->GetObjectGeneration(objectId, &gen);
    if (FAILED(hr))
    {
        printf("GetObjectGeneration failed hr=0x%x\n", hr);
    }
    else if (gen.generation == COR_PRF_GC_PINNED_OBJECT_HEAP)
    {
        _gcPOHAllocations++;
    }
    else if (gen.generation == COR_PRF_GC_LARGE_OBJECT_HEAP)
    {
        _gcLOHAllocations++;
    }

    return S_OK;
}

HRESULT GCAllocateProfiler::Shutdown()
{
    Profiler::Shutdown();
    assert(_gcPOHAllocations > 0);
    assert(_gcLOHAllocations > 0);
    printf("PROFILER TEST PASSES. PinnedObjectAllocations=%d, LargeObjectAllocations=%d.\n", (int)_gcPOHAllocations, (int)_gcLOHAllocations);
    fflush(stdout);

    return S_OK;
}
