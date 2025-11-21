// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class DynamicJitOptimizations : public Profiler
{
public:
    DynamicJitOptimizations() = default;
    virtual ~DynamicJitOptimizations() = default;

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
    virtual HRESULT STDMETHODCALLTYPE JITInlining(
        FunctionID callerId,
        FunctionID calleeId,
        BOOL      *pfShouldInline) override;
    virtual HRESULT STDMETHODCALLTYPE Shutdown() override;
};
