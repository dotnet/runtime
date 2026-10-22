// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class GCAllocateProfiler : public Profiler
{
public:
    GCAllocateProfiler() : Profiler(),
        _gcLOHAllocations(0),
        _gcPOHAllocations(0),
        _failures(0)
    {}

	static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE ObjectAllocated(ObjectID objectId, ClassID classId);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

private:
    std::atomic<int> _gcLOHAllocations;
    std::atomic<int> _gcPOHAllocations;
    std::atomic<int> _failures;
};
