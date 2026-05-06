// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class GCBasicProfiler : public Profiler
{
public:
    GCBasicProfiler() : Profiler(),
        _gcStarts(0),
        _gcFinishes(0),
        _failures(0)
    {}

	static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished();

private:
    std::atomic<int> _gcStarts;
    std::atomic<int> _gcFinishes;
    std::atomic<int> _failures;
};
