// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"
#include <unordered_set>


// This class is intended to be the verification of COR_PRF_HIGH_MONITOR_GC_SKIP_ALLOCATED_BY_CLASS_STATISTIC flag.
class GCSkipObjectsAllocatedByClassCallbackProfiler : public Profiler
{
public:
    GCSkipObjectsAllocatedByClassCallbackProfiler() : Profiler(),
        _gcStarts(0),
        _gcFinishes(0),
        _allocatedByClassCalls(0),
        _failures(0)
    {}

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished();
    virtual HRESULT STDMETHODCALLTYPE ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[]);
private:
    std::atomic<int> _gcStarts;
    std::atomic<int> _gcFinishes;
    std::atomic<int> _allocatedByClassCalls;
    std::atomic<int> _failures;
};
