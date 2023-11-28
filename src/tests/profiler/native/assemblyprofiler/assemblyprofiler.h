// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class AssemblyProfiler : public Profiler
{
public:
    AssemblyProfiler() : Profiler(),
        _assemblyUnloadStartedCount(0),
        _assemblyUnloadFinishedCount(0)
    {}

	static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE AssemblyUnloadStarted(AssemblyID assemblyId);
    virtual HRESULT STDMETHODCALLTYPE AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus);

private:
    std::atomic<int> _assemblyUnloadStartedCount;
    std::atomic<int> _assemblyUnloadFinishedCount;
};
