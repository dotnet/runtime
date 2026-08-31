// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class RuntimeAsyncApisProfiler : public Profiler
{
public:
    RuntimeAsyncApisProfiler();

    static GUID GetClsid();
    HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pCorProfilerInfoUnk) override;
    HRESULT STDMETHODCALLTYPE Shutdown() override;
    HRESULT STDMETHODCALLTYPE JITCompilationStarted(
        FunctionID functionId, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE JITCompilationFinished(
        FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE ExceptionThrown(ObjectID thrownObjectId) override;
    HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionEnter(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) override;

private:
    bool IsTarget(FunctionID functionId);
    ModuleID GetModuleId(FunctionID functionId);

    std::atomic<int> _failures;
    std::atomic<FunctionID> _target;
    std::atomic<int> _jitStarts;
    std::atomic<int> _jitFinishes;
    std::atomic<int> _ipRoundTrips;
    std::atomic<int> _ilMappings;
    std::atomic<int> _exceptionsThrown;
    std::atomic<int> _targetSearches;
    std::atomic<int> _targetUnwinds;
    std::atomic<int> _catchers;
};
