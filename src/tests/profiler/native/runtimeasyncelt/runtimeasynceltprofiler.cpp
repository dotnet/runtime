// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "runtimeasynceltprofiler.h"

namespace
{
void STDMETHODCALLTYPE EnterStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO)
{
    SHUTDOWNGUARD_RETVOID();
    RuntimeAsyncELTProfiler::Instance()->Enter(functionId.functionID);
}

void STDMETHODCALLTYPE LeaveStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO)
{
    SHUTDOWNGUARD_RETVOID();
    RuntimeAsyncELTProfiler::Instance()->Leave(functionId.functionID);
}

void STDMETHODCALLTYPE TailcallStub(FunctionIDOrClientID, COR_PRF_ELT_INFO)
{
    SHUTDOWNGUARD_RETVOID();
}
}

RuntimeAsyncELTProfiler* RuntimeAsyncELTProfiler::s_instance = nullptr;

RuntimeAsyncELTProfiler::RuntimeAsyncELTProfiler()
    : Profiler(),
      _target(0),
      _enters(0),
      _leaves(0),
      _depth(0),
      _sequenceFailures(0)
{
}

GUID RuntimeAsyncELTProfiler::GetClsid()
{
    // {D1C7A5B2-6E04-4D7F-9A31-2B84C6F0E915}
    GUID clsid = {0xd1c7a5b2, 0x6e04, 0x4d7f, {0x9a, 0x31, 0x2b, 0x84, 0xc6, 0xf0, 0xe9, 0x15}};
    return clsid;
}

HRESULT RuntimeAsyncELTProfiler::Initialize(IUnknown* pCorProfilerInfoUnk)
{
    HRESULT hr = Profiler::Initialize(pCorProfilerInfoUnk);
    if (FAILED(hr))
    {
        return hr;
    }

    s_instance = this;

    hr = pCorProfilerInfo->SetEventMask2(
        COR_PRF_MONITOR_ENTERLEAVE | COR_PRF_MONITOR_JIT_COMPILATION | COR_PRF_ENABLE_FRAME_INFO,
        0);
    if (FAILED(hr))
    {
        return hr;
    }

    return pCorProfilerInfo->SetEnterLeaveFunctionHooks3WithInfo(EnterStub, LeaveStub, TailcallStub);
}

HRESULT RuntimeAsyncELTProfiler::JITCompilationFinished(
    FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    SHUTDOWNGUARD();

    if (FAILED(hrStatus) || GetFunctionIDName(functionId) != WCHAR("Work"))
    {
        return S_OK;
    }

    std::wstring moduleName = GetModuleIDName(GetModuleId(functionId)).ToWString();
    if (moduleName.find(L"runtimeasyncelt") != std::wstring::npos)
    {
        FunctionID expected = 0;
        _target.compare_exchange_strong(expected, functionId);
    }

    return S_OK;
}

void RuntimeAsyncELTProfiler::Enter(FunctionID functionId)
{
    if (functionId != _target.load())
    {
        return;
    }

    _enters++;
    if (_depth.fetch_add(1) != 0)
    {
        _sequenceFailures++;
    }
}

void RuntimeAsyncELTProfiler::Leave(FunctionID functionId)
{
    if (functionId != _target.load())
    {
        return;
    }

    _leaves++;
    if (_depth.fetch_sub(1) != 1)
    {
        _sequenceFailures++;
    }
}

HRESULT RuntimeAsyncELTProfiler::Shutdown()
{
    HRESULT hr = Profiler::Shutdown();
    s_instance = nullptr;

    printf("RuntimeAsyncELTProfiler: enters=%d leaves=%d depth=%d sequenceFailures=%d\n",
           _enters.load(), _leaves.load(), _depth.load(), _sequenceFailures.load());

    if (SUCCEEDED(hr) && _target != 0 && _enters == 25 && _leaves == 25 &&
        _depth == 0 && _sequenceFailures == 0)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        printf("PROFILER TEST FAILED\n");
    }

    fflush(stdout);
    return hr;
}

ModuleID RuntimeAsyncELTProfiler::GetModuleId(FunctionID functionId)
{
    ClassID classId = 0;
    ModuleID moduleId = 0;
    mdToken token = 0;
    ULONG32 typeArgumentCount = 0;
    ClassID typeArguments[SHORT_LENGTH];
    COR_PRF_FRAME_INFO frameInfo = 0;

    HRESULT hr = pCorProfilerInfo->GetFunctionInfo2(
        functionId, frameInfo, &classId, &moduleId, &token,
        SHORT_LENGTH, &typeArgumentCount, typeArguments);
    return SUCCEEDED(hr) ? moduleId : 0;
}
