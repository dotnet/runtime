// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class ExceptionProfiler : public Profiler
{
public:
    ExceptionProfiler() : Profiler()
    {}

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
    virtual HRESULT STDMETHODCALLTYPE Shutdown() override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionThrown(ObjectID thrownObjectId) override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionEnter(FunctionID functionId) override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionLeave() override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFilterEnter(FunctionID functionId) override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFilterLeave() override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionSearchCatcherFound(FunctionID functionId) override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter(FunctionID functionId) override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionLeave() override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyEnter(FunctionID functionId) override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyLeave() override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) override;
    virtual HRESULT STDMETHODCALLTYPE ExceptionCatcherLeave() override;
private:

    String expectedSequence;
    String actualSequence;
};
