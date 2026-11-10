// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcskipobjectsallocatedbyclasscallbackprofiler.h"

GUID GCSkipObjectsAllocatedByClassCallbackProfiler::GetClsid()
{
    // {0FC54C4F-6BD3-494D-A11A-B32DE3FBCB76}

    GUID clsid = { 0x0FC54C4F, 0x6BD3, 0x494D, { 0xA1, 0x1A, 0xB3, 0x2D, 0xE3, 0xFB, 0xCB, 0x76 } };
    return clsid;
}

HRESULT GCSkipObjectsAllocatedByClassCallbackProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_GC, COR_PRF_HIGH_MONITOR_GC_SKIP_ALLOCATED_BY_CLASS_STATISTIC)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT GCSkipObjectsAllocatedByClassCallbackProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_gcStarts == 0)
    {
        printf("GCSkipObjectsAllocatedByClassCallbackProfiler::Shutdown: FAIL: Expected GarbageCollectionStarted to be called\n");
    }
    else if (_gcFinishes == 0)
    {
        printf("GCSkipObjectsAllocatedByClassCallbackProfiler::Shutdown: FAIL: Expected GarbageCollectionFinished to be called\n");
    }
    else if (_allocatedByClassCalls != 0)
    {
        printf("GCSkipObjectsAllocatedByClassCallbackProfiler::Shutdown: FAIL: Expected ObjectsAllocatedByClass to be not called\n");
    }
    else if(_failures == 0)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        // failures were printed earlier when _failures was incremented
    }
    fflush(stdout);

    return S_OK;
}

HRESULT GCSkipObjectsAllocatedByClassCallbackProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    SHUTDOWNGUARD();

    _gcStarts++;
    if (_gcStarts - _gcFinishes > 2)
    {
        _failures++;
        printf("GCSkipObjectsAllocatedByClassCallbackProfiler::GarbageCollectionStarted: FAIL: Expected GCStart <= GCFinish+2. GCStart=%d, GCFinish=%d\n", (int)_gcStarts, (int)_gcFinishes);
    }

    return S_OK;
}

HRESULT GCSkipObjectsAllocatedByClassCallbackProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();

    _gcFinishes++;
    if (_gcStarts < _gcFinishes)
    {
        _failures++;
        printf("GCSkipObjectsAllocatedByClassCallbackProfiler::GarbageCollectionFinished: FAIL: Expected GCStart >= GCFinish. Start=%d, Finish=%d\n", (int)_gcStarts, (int)_gcFinishes);
    }

    return S_OK;
}

HRESULT GCSkipObjectsAllocatedByClassCallbackProfiler::ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[])
{
    SHUTDOWNGUARD();

    ++_allocatedByClassCalls;
    ++_failures;
    printf("GCSkipObjectsAllocatedByClassCallbackProfiler::ObjectsAllocatedByClass: FAIL: Expected ObjectsAllocatedByClass Calls == 0. AllocatedByClassCalls=%d\n", (int)_allocatedByClassCalls);


    return S_OK;
}
