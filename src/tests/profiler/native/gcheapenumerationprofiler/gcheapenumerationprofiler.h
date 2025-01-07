// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>
#include "../profiler.h"

class GCHeapEnumerationProfiler : public Profiler
{
public:
    GCHeapEnumerationProfiler() : Profiler(),
        _objectsCount(0),
        _customGCHeapObjectTypesCount(0),
        _failures(0),
        _expectedExceptions(0),
        _threadList(),
        _gcStartSleeping(false)
    {}

    // Profiler callbacks override
	static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished();
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    // Helper methods
    bool IsGCStartSleeping();
    void IncrementFailures();
    HRESULT ValidateEnumerateGCHeapObjects(HRESULT expected);

private:
    std::atomic<int> _objectsCount;
    std::atomic<int> _customGCHeapObjectTypesCount;
    std::atomic<int> _failures;
    std::atomic<int> _expectedExceptions;
    std::vector<std::thread> _threadList;
    bool _gcStartSleeping;

    HRESULT EnumerateGCHeapObjects();
};
