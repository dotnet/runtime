// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class RuntimeAsyncELTProfiler : public Profiler
{
public:
    RuntimeAsyncELTProfiler();

    static GUID GetClsid();
    HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pCorProfilerInfoUnk) override;
    HRESULT STDMETHODCALLTYPE Shutdown() override;
    HRESULT STDMETHODCALLTYPE JITCompilationFinished(
        FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;

    void Enter(FunctionID functionId);
    void Leave(FunctionID functionId);

    static RuntimeAsyncELTProfiler* Instance() { return s_instance; }

private:
    static const int TargetCount = 3;

    int GetTargetIndex(FunctionID functionId);
    ModuleID GetModuleId(FunctionID functionId);

    static RuntimeAsyncELTProfiler* s_instance;
    std::atomic<FunctionID> _targets[TargetCount];
    std::atomic<int> _enters[TargetCount];
    std::atomic<int> _leaves[TargetCount];
    std::atomic<int> _depth[TargetCount];
    std::atomic<int> _sequenceFailures;
};
