// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "enumthreadsprofiler.h"

GUID EnumThreadsProfiler::GetClsid()
{
    // {0742962D-2ED3-44B0-BA84-06B1EF0A0A0B}
    GUID clsid = { 0x0742962d, 0x2ed3, 0x44b0,{ 0xba, 0x84, 0x06, 0xb1, 0xef, 0x0a, 0x0a, 0x0b } };
	return clsid;
}

HRESULT EnumThreadsProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);
    printf("EnumThreadsProfiler::Initialize\n");

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_GC | COR_PRF_MONITOR_SUSPENDS, COR_PRF_HIGH_MONITOR_NONE)))
    {
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        IncrementFailures();
    }

    return hr;
}

HRESULT STDMETHODCALLTYPE EnumThreadsProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    SHUTDOWNGUARD();

    printf("EnumThreadsProfiler::GarbageCollectionStarted\n");
    _gcStarts.fetch_add(1, std::memory_order_relaxed);
    if (_gcStarts < _gcFinishes)
    {
        IncrementFailures();
        printf("EnumThreadsProfiler::GarbageCollectionStarted: FAIL: Expected GCStart >= GCFinish. Start=%d, Finish=%d\n", (int)_gcStarts, (int)_gcFinishes);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE EnumThreadsProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();

    printf("EnumThreadsProfiler::GarbageCollectionFinished\n");
    _gcFinishes.fetch_add(1, std::memory_order_relaxed);
    if (_gcStarts < _gcFinishes)
    {
        IncrementFailures();
        printf("EnumThreadsProfiler::GarbageCollectionFinished: FAIL: Expected GCStart >= GCFinish. Start=%d, Finish=%d\n", (int)_gcStarts, (int)_gcFinishes);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE EnumThreadsProfiler::RuntimeSuspendFinished()
{
    SHUTDOWNGUARD();

    printf("EnumThreadsProfiler::RuntimeSuspendFinished\n");

    ICorProfilerThreadEnum* threadEnum = nullptr;
    HRESULT enumThreadsHR = pCorProfilerInfo->EnumThreads(&threadEnum);
    printf("Finished enumerating threads\n");
    _profilerEnumThreadsCompleted.fetch_add(1, std::memory_order_relaxed);
    threadEnum->Release();
    return enumThreadsHR;
}

HRESULT EnumThreadsProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_gcStarts == 0)
    {
        printf("EnumThreadsProfiler::Shutdown: FAIL: Expected GarbageCollectionStarted to be called\n");
    }
    else if (_gcFinishes == 0)
    {
        printf("EnumThreadsProfiler::Shutdown: FAIL: Expected GarbageCollectionFinished to be called\n");
    }
    else if (_profilerEnumThreadsCompleted == 0)
    {
        printf("EnumThreadsProfiler::Shutdown: FAIL: Expected RuntimeSuspendFinished to be called and EnumThreads completed\n");
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

void EnumThreadsProfiler::IncrementFailures()
{
    _failures.fetch_add(1, std::memory_order_relaxed);
}
