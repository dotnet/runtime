// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"
#include <memory>

class InliningProfiler : public Profiler
{
public:
    static std::shared_ptr<InliningProfiler> s_profiler;

    InliningProfiler() : 
    	Profiler(),
    	_failures(0),
    	_inInlining(false),
    	_inBlockInlining(false),
    	_inNoResponse(false)
    {
    }

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline);

    HRESULT STDMETHODCALLTYPE EnterCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT STDMETHODCALLTYPE LeaveCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT STDMETHODCALLTYPE TailcallCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);

private:
    std::atomic<int> _failures;
    bool _inInlining;
    bool _inBlockInlining;
    bool _inNoResponse;
    bool _sawInlineeCall;
};
