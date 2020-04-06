// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "../profiler.h"

class EventPipeProfiler : public Profiler
{
public:
    EventPipeProfiler() : Profiler(),
        _failures(0),
        _provider(0),
        _allTypesEvent(0),
        _emptyEvent(0),
        _simpleEvent(0)
    {}

    virtual GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock);

private:
    std::atomic<int> _failures;
    ICorProfilerInfo12 *_pCorProfilerInfo12;
    EVENTPIPE_PROVIDER _provider;
    EVENTPIPE_EVENT _allTypesEvent;
    EVENTPIPE_EVENT _emptyEvent;
    EVENTPIPE_EVENT _simpleEvent;
};