// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "instrumentedilmap.h"
#include <vector>

// Every entry is (0, 0), so every ordinary IL offset translates to 0, and each such translation scans the
// whole map while other threads replace it. Reading a freed copy may fault or yield invalid offsets.
static const ULONG32 MapEntries = 50000;

// The special IL offsets GetILToNativeMapping may report (CorDebugIlToNativeMappingTypes).
static const ULONG32 NoMappingOffset = 0xFFFFFFFF;
static const ULONG32 PrologOffset = 0xFFFFFFFE;
static const ULONG32 EpilogOffset = 0xFFFFFFFD;

InstrumentedILMapProfiler::InstrumentedILMapProfiler()
    : Profiler(),
      _failures(0),
      _mapsSet(0),
      _mappingsRead(0),
      _badOffsets(0)
{
}

GUID InstrumentedILMapProfiler::GetClsid()
{
    // {4F7C2B1E-8D3A-4E59-B6A1-2C9E5D7F3A10}
    GUID clsid = {0x4f7c2b1e, 0x8d3a, 0x4e59, {0xb6, 0xa1, 0x2c, 0x9e, 0x5d, 0x7f, 0x3a, 0x10}};
    return clsid;
}

HRESULT InstrumentedILMapProfiler::Initialize(IUnknown* pCorProfilerInfoUnk)
{
    HRESULT hr = Profiler::Initialize(pCorProfilerInfoUnk);
    if (FAILED(hr))
    {
        return hr;
    }

    return pCorProfilerInfo->SetEventMask2(
        COR_PRF_MONITOR_JIT_COMPILATION | COR_PRF_MONITOR_CACHE_SEARCHES, 0);
}

bool InstrumentedILMapProfiler::IsTarget(FunctionID functionId)
{
    // Compare the method definition's name. GetFunctionIDName appends the type arguments
    // ("Target<W<...>>"), which differ for every instantiation this test compiles.
    ClassID classId = 0;
    ModuleID moduleId = 0;
    mdToken token = 0;
    ULONG32 typeArgumentCount = 0;
    ClassID typeArguments[SHORT_LENGTH];
    HRESULT hr = pCorProfilerInfo->GetFunctionInfo2(
        functionId, 0, &classId, &moduleId, &token, SHORT_LENGTH, &typeArgumentCount, typeArguments);
    if (FAILED(hr))
    {
        return false;
    }

    COMPtrHolder<IMetaDataImport> pIMDImport;
    hr = pCorProfilerInfo->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport, (IUnknown **)&pIMDImport);
    if (FAILED(hr))
    {
        return false;
    }

    WCHAR methodName[STR_LENGTH];
    hr = pIMDImport->GetMethodProps(token, NULL, methodName, STR_LENGTH, NULL, NULL, NULL, NULL, NULL, NULL);
    if (FAILED(hr))
    {
        return false;
    }

    String name;
    name += methodName;
    if (name != WCHAR("Target"))
    {
        return false;
    }

    std::wstring moduleName = GetModuleIDName(moduleId).ToWString();
    return moduleName.find(L"instrumentedilmap") != std::wstring::npos;
}

HRESULT InstrumentedILMapProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    SHUTDOWNGUARD();

    if (!IsTarget(functionId))
    {
        return S_OK;
    }

    // Every value-type instantiation of Target shares one method token, so each call after the first
    // replaces the map the runtime holds for that token.
    // The runtime copies the entries, so the profiler keeps ownership of this buffer.
    std::vector<COR_IL_MAP> map(MapEntries);
    for (ULONG32 i = 0; i < MapEntries; i++)
    {
        map[i].oldOffset = 0;
        map[i].newOffset = 0;
        map[i].fAccurate = TRUE;
    }

    HRESULT hr = pCorProfilerInfo->SetILInstrumentedCodeMap(functionId, TRUE, MapEntries, map.data());
    if (FAILED(hr))
    {
        printf("SetILInstrumentedCodeMap failed hr=0x%x\n", hr);
        _failures++;
        return S_OK;
    }

    _mapsSet++;
    return S_OK;
}

HRESULT InstrumentedILMapProfiler::JITCompilationFinished(
    FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    SHUTDOWNGUARD();

    if (!IsTarget(functionId) || FAILED(hrStatus))
    {
        return S_OK;
    }

    COR_DEBUG_IL_TO_NATIVE_MAP mappings[512];
    ULONG32 mappingCount = 0;
    HRESULT hr = pCorProfilerInfo->GetILToNativeMapping(
        functionId, static_cast<ULONG32>(sizeof(mappings) / sizeof(mappings[0])), &mappingCount, mappings);
    if (FAILED(hr))
    {
        printf("GetILToNativeMapping failed hr=0x%x\n", hr);
        _failures++;
        return S_OK;
    }

    if (mappingCount == 0 || mappingCount > static_cast<ULONG32>(sizeof(mappings) / sizeof(mappings[0])))
    {
        printf("GetILToNativeMapping returned %u entries\n", mappingCount);
        _failures++;
        return S_OK;
    }

    _mappingsRead++;
    for (ULONG32 i = 0; i < mappingCount; i++)
    {
        ULONG32 ilOffset = mappings[i].ilOffset;
        bool special = ilOffset == NoMappingOffset || ilOffset == PrologOffset || ilOffset == EpilogOffset;
        if (!special && ilOffset != 0)
        {
            printf("IL offset 0x%x does not come from the instrumented map\n", ilOffset);
            _badOffsets++;
            _failures++;
            break;
        }
    }
    return S_OK;
}

HRESULT InstrumentedILMapProfiler::JITCachedFunctionSearchStarted(
    FunctionID functionId, BOOL* pbUseCachedFunction)
{
    *pbUseCachedFunction = TRUE;
    SHUTDOWNGUARD();

    if (IsTarget(functionId))
    {
        // The test needs every instantiation JIT-compiled so the map is replaced each time.
        *pbUseCachedFunction = FALSE;
    }
    return S_OK;
}

HRESULT InstrumentedILMapProfiler::Shutdown()
{
    HRESULT hr = Profiler::Shutdown();

    printf("InstrumentedILMapProfiler: failures=%d mapsSet=%d mappingsRead=%d badOffsets=%d\n",
           _failures.load(), _mapsSet.load(), _mappingsRead.load(), _badOffsets.load());

    bool passed = SUCCEEDED(hr) && _failures == 0 && _mapsSet > 1 && _mappingsRead > 1;

    printf(passed ? "PROFILER TEST PASSES\n" : "PROFILER TEST FAILED\n");
    fflush(stdout);
    return hr;
}
