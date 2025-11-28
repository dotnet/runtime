// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class ClassLoad : public Profiler
{
public:

    ClassLoad() : 
        Profiler(),
        _classLoadStartedCount(0),
        _classLoadFinishedCount(0),
        _classUnloadStartedCount(0),
        _classUnloadFinishedCount(0),
        _failures(0)
    {
    }

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    virtual HRESULT STDMETHODCALLTYPE ClassLoadStarted(ClassID classId);
    virtual HRESULT STDMETHODCALLTYPE ClassLoadFinished(ClassID classId, HRESULT hrStatus);
    virtual HRESULT STDMETHODCALLTYPE ClassUnloadStarted(ClassID classId);
    virtual HRESULT STDMETHODCALLTYPE ClassUnloadFinished(ClassID classId, HRESULT hrStatus);

private:
    std::atomic<int> _classLoadStartedCount;
    std::atomic<int> _classLoadFinishedCount;
    std::atomic<int> _classUnloadStartedCount;
    std::atomic<int> _classUnloadFinishedCount;
    std::atomic<int> _failures;
};
