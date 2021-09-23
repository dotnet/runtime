// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class MultiplyLoaded : public Profiler
{
public:
    MultiplyLoaded() : Profiler()
    {}

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pICorProfilerInfoUnk, void* pvClientData, UINT cbClientData);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE LoadAsNotficationOnly(BOOL *pbNotificationOnly);

    virtual HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded();
    virtual HRESULT STDMETHODCALLTYPE ExceptionThrown(ObjectID thrownObjectId);

private:
    static std::atomic<int> _exceptionThrownSeenCount;
    static std::atomic<int> _detachCount;
    static std::atomic<int> _failures;

    HRESULT InitializeCommon(IUnknown* pCorProfilerInfoUnk);
};
