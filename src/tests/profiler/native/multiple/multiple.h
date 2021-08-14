// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class MultiplyLoaded : public Profiler
{
public:
    MultiplyLoaded() : Profiler(),
        _exceptionThrownSeenCount(0),
        _detachCount(0),
        _failures(0)
    {}

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pICorProfilerInfoUnk, void* pvClientData, UINT cbClientData);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE LoadAsNotficationOnly(BOOL *pbNotificationOnly);

    virtual HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded();
    virtual HRESULT STDMETHODCALLTYPE ExceptionThrown(ObjectID thrownObjectId);

private:
    std::atomic<int> _exceptionThrownSeenCount;
    std::atomic<int> _detachCount;
    std::atomic<int> _failures;

    HRESULT InitializeCommon(IUnknown* pCorProfilerInfoUnk);
};
