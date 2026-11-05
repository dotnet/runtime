// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

// Replaces a method's instrumented IL map on every JIT of one generic method while other threads
// read their mappings back, and checks that every IL offset read back comes from the map.
class InstrumentedILMapProfiler : public Profiler
{
public:
    InstrumentedILMapProfiler();

    static GUID GetClsid();
    HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pCorProfilerInfoUnk) override;
    HRESULT STDMETHODCALLTYPE Shutdown() override;
    HRESULT STDMETHODCALLTYPE JITCompilationStarted(
        FunctionID functionId, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE JITCompilationFinished(
        FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchStarted(
        FunctionID functionId, BOOL* pbUseCachedFunction) override;

private:
    bool IsTarget(FunctionID functionId);

    std::atomic<int> _failures;
    std::atomic<int> _mapsSet;
    std::atomic<int> _mappingsRead;
    std::atomic<int> _badOffsets;
};
