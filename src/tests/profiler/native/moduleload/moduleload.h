// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class ModuleLoad : public Profiler
{
public:

    ModuleLoad() : 
        Profiler(),
        _assemblyLoadStartedCount(0),
        _assemblyLoadFinishedCount(0),
        _moduleLoadStartedCount(0),
        _moduleLoadFinishedCount(0),
        _failures(0)
    {

    }

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pICorProfilerInfoUnk, void* pvClientData, UINT cbClientData);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE LoadAsNotificationOnly(BOOL *pbNotificationOnly);

    virtual HRESULT STDMETHODCALLTYPE AssemblyLoadStarted(AssemblyID assemblyId);
    virtual HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus);
    virtual HRESULT STDMETHODCALLTYPE ModuleLoadStarted(ModuleID moduleId);
    virtual HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus);

private:
    std::atomic<int> _assemblyLoadStartedCount;
    std::atomic<int> _assemblyLoadFinishedCount;
    std::atomic<int> _moduleLoadStartedCount;
    std::atomic<int> _moduleLoadFinishedCount;
    std::atomic<int> _failures;

    HRESULT InitializeCommon(IUnknown* pCorProfilerInfoUnk);
};
