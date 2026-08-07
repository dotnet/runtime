// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "runtimeasynceltprofiler.h"

namespace
{
const WCHAR* const TargetNames[] = {WCHAR("Work"), WCHAR("WorkVoid"), WCHAR("WorkDouble")};
const char* const TargetDisplayNames[] = {"Work", "WorkVoid", "WorkDouble"};
const int ExpectedCallbackCounts[] = {25, 3, 3};

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
      _sequenceFailures(0)
{
    static_assert(sizeof(TargetNames) / sizeof(TargetNames[0]) == TargetCount);
    static_assert(sizeof(TargetDisplayNames) / sizeof(TargetDisplayNames[0]) == TargetCount);
    static_assert(sizeof(ExpectedCallbackCounts) / sizeof(ExpectedCallbackCounts[0]) == TargetCount);

    for (int i = 0; i < TargetCount; i++)
    {
        _targets[i] = 0;
        _enters[i] = 0;
        _leaves[i] = 0;
        _depth[i] = 0;
    }
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

    if (FAILED(hrStatus))
    {
        return S_OK;
    }

    String functionName = GetFunctionIDName(functionId);
    int targetIndex = -1;
    for (int i = 0; i < TargetCount; i++)
    {
        if (functionName == TargetNames[i])
        {
            targetIndex = i;
            break;
        }
    }
    if (targetIndex < 0)
    {
        return S_OK;
    }

    std::wstring moduleName = GetModuleIDName(GetModuleId(functionId)).ToWString();
    if (moduleName.find(L"runtimeasyncelt") != std::wstring::npos)
    {
        FunctionID expected = 0;
        _targets[targetIndex].compare_exchange_strong(expected, functionId);
    }

    return S_OK;
}

void RuntimeAsyncELTProfiler::Enter(FunctionID functionId)
{
    int targetIndex = GetTargetIndex(functionId);
    if (targetIndex < 0)
    {
        return;
    }

    _enters[targetIndex]++;
    if (_depth[targetIndex].fetch_add(1) != 0)
    {
        _sequenceFailures++;
    }
}

void RuntimeAsyncELTProfiler::Leave(FunctionID functionId)
{
    int targetIndex = GetTargetIndex(functionId);
    if (targetIndex < 0)
    {
        return;
    }

    _leaves[targetIndex]++;
    if (_depth[targetIndex].fetch_sub(1) != 1)
    {
        _sequenceFailures++;
    }
}

HRESULT RuntimeAsyncELTProfiler::Shutdown()
{
    HRESULT hr = Profiler::Shutdown();
    s_instance = nullptr;

    bool passed = SUCCEEDED(hr) && _sequenceFailures == 0;
    for (int i = 0; i < TargetCount; i++)
    {
        printf("RuntimeAsyncELTProfiler: %s enters=%d leaves=%d depth=%d\n",
               TargetDisplayNames[i], _enters[i].load(), _leaves[i].load(), _depth[i].load());
        passed &= _targets[i].load() != 0 &&
                  _enters[i].load() == ExpectedCallbackCounts[i] &&
                  _leaves[i].load() == ExpectedCallbackCounts[i] &&
                  _depth[i].load() == 0;
    }
    printf("RuntimeAsyncELTProfiler: sequenceFailures=%d\n", _sequenceFailures.load());
    printf(passed ? "PROFILER TEST PASSES\n" : "PROFILER TEST FAILED\n");

    fflush(stdout);
    return hr;
}

int RuntimeAsyncELTProfiler::GetTargetIndex(FunctionID functionId)
{
    for (int i = 0; i < TargetCount; i++)
    {
        if (_targets[i].load() == functionId)
        {
            return i;
        }
    }
    return -1;
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
