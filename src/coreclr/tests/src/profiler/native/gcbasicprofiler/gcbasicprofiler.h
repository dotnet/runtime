// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

	virtual GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished();

private:
    std::atomic<int> _gcStarts;
    std::atomic<int> _gcFinishes;
    std::atomic<int> _failures;
};