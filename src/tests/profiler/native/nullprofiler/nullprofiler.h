// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class NullProfiler : public Profiler
{
private:
    std::atomic<uint32_t> _failures;

public:
    NullProfiler() : 
        Profiler(),
        _failures(0)
    {
        
    }

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
};
