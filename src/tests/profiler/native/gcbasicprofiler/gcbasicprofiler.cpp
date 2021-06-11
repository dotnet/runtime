// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcbasicprofiler.h"

GUID GCBasicProfiler::GetClsid()
{
	// {A040B953-EDE7-42D9-9077-AA69BB2BE024}
	GUID clsid = { 0xa040b953, 0xede7, 0x42d9,{ 0x90, 0x77, 0xaa, 0x69, 0xbb, 0x2b, 0xe0, 0x24 } };
	return clsid;
}

HRESULT GCBasicProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(0, 0x10)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT GCBasicProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_gcStarts == 0)
    {
        printf("GCBasicProfiler::Shutdown: FAIL: Expected GarbaseCollectionStarted to be called\n");
    }
    else if (_gcFinishes == 0)
    {
        printf("GCBasicProfiler::Shutdown: FAIL: Expected GarbageCollectionFinished to be called\n");
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

HRESULT GCBasicProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    SHUTDOWNGUARD();

    _gcStarts++;
    if (_gcStarts - _gcFinishes > 2)
    {
        _failures++;
        printf("GCBasicProfiler::GarbageCollectionStarted: FAIL: Expected GCStart <= GCFinish+2. GCStart=%d, GCFinish=%d\n", (int)_gcStarts, (int)_gcFinishes);
        return S_OK;
    }


    if(_gcStarts == 1)
    {
        ULONG nObjectRanges;
        const ULONG cRanges = 32;
        COR_PRF_GC_GENERATION_RANGE objectRanges[cRanges];

        HRESULT hr = pCorProfilerInfo->GetGenerationBounds(cRanges, &nObjectRanges, objectRanges);
        if (FAILED(hr))
        {
            _failures++;
            printf("GCBasicProfiler::GarbageCollectionStarted: FAIL: GetGenerationBounds hr=0x%x\n", hr);
            return S_OK;
        }
        // loop through all ranges
        for (int i = nObjectRanges - 1; i >= 0; i--)
        {
            if (0 > objectRanges[i].generation || objectRanges[i].generation > 4)
            {
                _failures++;
                printf("GCBasicProfiler::GarbageCollectionStarted: FAIL: invalid generation: %d\n",objectRanges[i].generation);
            }
        }
        if (nObjectRanges > 3 && objectRanges[2].generation == 2 && objectRanges[2].rangeLength == 0x18 && objectRanges[2].generation == 1)
        {
            if (objectRanges[3].rangeLength != 0x18)
            {
                _failures++;
                printf("GCBasicProfiler::GarbageCollectionStarted: FAIL: in the first GC for the segment case, gen 1 should have size 0x18");
            }
        }
    }

    return S_OK;
}

HRESULT GCBasicProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();

    _gcFinishes++;
    if (_gcStarts < _gcFinishes)
    {
        _failures++;
        printf("GCBasicProfiler::GarbageCollectionFinished: FAIL: Expected GCStart >= GCFinish. Start=%d, Finish=%d\n", (int)_gcStarts, (int)_gcFinishes);
    }
    return S_OK;
}
