// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "runtimeasyncapisprofiler.h"

RuntimeAsyncApisProfiler::RuntimeAsyncApisProfiler()
    : Profiler(),
      _failures(0),
      _target(0),
      _jitStarts(0),
      _jitFinishes(0),
      _cacheVetoes(0),
      _unexpectedCachedFunctions(0),
      _ipRoundTrips(0),
      _ilMappings(0),
      _exceptionsThrown(0),
      _targetSearches(0),
      _targetUnwinds(0),
      _catchers(0)
{
}

GUID RuntimeAsyncApisProfiler::GetClsid()
{
    // {9A20F5D8-47B1-4E63-82CD-1F7690AB34E2}
    GUID clsid = {0x9a20f5d8, 0x47b1, 0x4e63, {0x82, 0xcd, 0x1f, 0x76, 0x90, 0xab, 0x34, 0xe2}};
    return clsid;
}

HRESULT RuntimeAsyncApisProfiler::Initialize(IUnknown* pCorProfilerInfoUnk)
{
    HRESULT hr = Profiler::Initialize(pCorProfilerInfoUnk);
    if (FAILED(hr))
    {
        return hr;
    }

    return pCorProfilerInfo->SetEventMask2(
        COR_PRF_MONITOR_JIT_COMPILATION |
        COR_PRF_MONITOR_CACHE_SEARCHES |
        COR_PRF_MONITOR_EXCEPTIONS,
        0);
}

bool RuntimeAsyncApisProfiler::IsTarget(FunctionID functionId)
{
    if (GetFunctionIDName(functionId) != WCHAR("Work"))
    {
        return false;
    }

    std::wstring moduleName = GetModuleIDName(GetModuleId(functionId)).ToWString();
    return moduleName.find(L"runtimeasyncapis") != std::wstring::npos;
}

HRESULT RuntimeAsyncApisProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    SHUTDOWNGUARD();

    if (IsTarget(functionId))
    {
        _jitStarts++;
    }
    return S_OK;
}

HRESULT RuntimeAsyncApisProfiler::JITCompilationFinished(
    FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    SHUTDOWNGUARD();

    if (!IsTarget(functionId))
    {
        return S_OK;
    }

    if (FAILED(hrStatus))
    {
        _failures++;
        return S_OK;
    }

    _target = functionId;
    _jitFinishes++;
    ValidateTargetCode(functionId);
    return S_OK;
}

HRESULT RuntimeAsyncApisProfiler::JITCachedFunctionSearchStarted(
    FunctionID functionId, BOOL* pbUseCachedFunction)
{
    *pbUseCachedFunction = TRUE;
    SHUTDOWNGUARD();

    if (IsTarget(functionId))
    {
        // The APIs validated by this test require JIT-generated native code.
        _cacheVetoes++;
        *pbUseCachedFunction = FALSE;
    }
    return S_OK;
}

HRESULT RuntimeAsyncApisProfiler::JITCachedFunctionSearchFinished(
    FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    SHUTDOWNGUARD();

    if (IsTarget(functionId) && result == COR_PRF_CACHED_FUNCTION_FOUND)
    {
        printf("Cached target function was used after the profiler requested JIT compilation\n");
        _unexpectedCachedFunctions++;
        _failures++;
    }
    return S_OK;
}

void RuntimeAsyncApisProfiler::ValidateTargetCode(FunctionID functionId)
{
    COR_PRF_CODE_INFO codeInfos[16];
    ULONG32 codeInfoCount = 0;
    HRESULT hr = pCorProfilerInfo->GetCodeInfo2(
        functionId, static_cast<ULONG32>(sizeof(codeInfos) / sizeof(codeInfos[0])),
        &codeInfoCount, codeInfos);
    if (FAILED(hr) || codeInfoCount == 0)
    {
        printf("GetCodeInfo2 failed hr=0x%x count=%u\n", hr, codeInfoCount);
        _failures++;
        return;
    }

    FunctionID roundTrip = 0;
    hr = pCorProfilerInfo->GetFunctionFromIP(
        reinterpret_cast<LPCBYTE>(codeInfos[0].startAddress), &roundTrip);
    if (SUCCEEDED(hr) && roundTrip == functionId)
    {
        _ipRoundTrips++;
    }
    else
    {
        _failures++;
    }

    COR_DEBUG_IL_TO_NATIVE_MAP mappings[512];
    ULONG32 mappingCount = 0;
    hr = pCorProfilerInfo->GetILToNativeMapping(
        functionId, static_cast<ULONG32>(sizeof(mappings) / sizeof(mappings[0])),
        &mappingCount, mappings);
    if (SUCCEEDED(hr) && mappingCount > 0)
    {
        _ilMappings++;
    }
    else
    {
        _failures++;
    }
}

HRESULT RuntimeAsyncApisProfiler::ExceptionThrown(ObjectID thrownObjectId)
{
    SHUTDOWNGUARD();

    _exceptionsThrown++;
    return S_OK;
}

HRESULT RuntimeAsyncApisProfiler::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    SHUTDOWNGUARD();

    if (functionId == _target.load())
    {
        _targetSearches++;
    }
    return S_OK;
}

HRESULT RuntimeAsyncApisProfiler::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    SHUTDOWNGUARD();

    if (functionId == _target.load())
    {
        _targetUnwinds++;
    }
    return S_OK;
}

HRESULT RuntimeAsyncApisProfiler::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    SHUTDOWNGUARD();

    _catchers++;
    return S_OK;
}

HRESULT RuntimeAsyncApisProfiler::Shutdown()
{
    HRESULT hr = Profiler::Shutdown();

    printf("RuntimeAsyncApisProfiler: failures=%d jit=%d/%d cacheVetoes=%d unexpectedCached=%d ip=%d il=%d "
           "thrown=%d searches=%d unwinds=%d catchers=%d\n",
           _failures.load(), _jitStarts.load(), _jitFinishes.load(),
           _cacheVetoes.load(), _unexpectedCachedFunctions.load(),
           _ipRoundTrips.load(), _ilMappings.load(), _exceptionsThrown.load(),
           _targetSearches.load(), _targetUnwinds.load(), _catchers.load());

    bool passed =
        SUCCEEDED(hr) &&
        _failures == 0 &&
        _target != 0 &&
        _jitStarts > 0 &&
        _jitFinishes > 0 &&
        _ipRoundTrips > 0 &&
        _ilMappings > 0 &&
        _exceptionsThrown > 0 &&
        (_targetSearches > 0 || _targetUnwinds > 0) &&
        _catchers > 0;

    printf(passed ? "PROFILER TEST PASSES\n" : "PROFILER TEST FAILED\n");
    fflush(stdout);
    return hr;
}

ModuleID RuntimeAsyncApisProfiler::GetModuleId(FunctionID functionId)
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
