// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "exceptionprofiler.h"

GUID ExceptionProfiler::GetClsid()
{
    GUID clsid = { 0xE3C1F87D, 0x1D20, 0x4F1C,{ 0xA3, 0x5E, 0x2C, 0x2D, 0x2E, 0x2B, 0x8F, 0x5D } };
	return clsid;
}

HRESULT ExceptionProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_EXCEPTIONS | COR_PRF_DISABLE_INLINING, 0)))
    {
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    constexpr ULONG bufferSize = 1024;
    ULONG envVarLen = 0;
    WCHAR envVar[bufferSize];
    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("Exception_Expected_Sequence"), bufferSize, &envVarLen, envVar)))
    {
        return E_FAIL;
    }
    expectedSequence = envVar;

    return S_OK;
}

HRESULT ExceptionProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (expectedSequence == actualSequence)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        std::wcout << L"ExceptionProfiler::Shutdown: FAIL: Expected and actual exception sequences do not match" << std::endl;
        std::wcout << L"Expected Sequence" << std::endl << expectedSequence << std::endl;
        std::wcout << L"Actual Sequence" << std::endl << actualSequence << std::endl;
    }

    fflush(stdout);
    return S_OK;
}

HRESULT ExceptionProfiler::ExceptionThrown(ObjectID thrownObjectId)
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionThrown"));
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionSearchFunctionEnter: "));
    actualSequence += GetFunctionIDName(functionId);
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionSearchFunctionLeave()
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionSearchFunctionLeave"));
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionSearchFilterEnter(FunctionID functionId)
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionSearchFilterEnter: "));
    actualSequence += GetFunctionIDName(functionId);
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionSearchFilterLeave()
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionSearchFilterLeave"));
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionSearchCatcherFound(FunctionID functionId)
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionSearchCatcherFound: "));
    actualSequence += GetFunctionIDName(functionId);
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionUnwindFunctionEnter: "));
    actualSequence += GetFunctionIDName(functionId);
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionUnwindFunctionLeave()
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionUnwindFunctionLeave"));
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionUnwindFinallyEnter: "));
    actualSequence += GetFunctionIDName(functionId);
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionUnwindFinallyLeave()
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionUnwindFinallyLeave"));
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionCatcherEnter: "));
    actualSequence += GetFunctionIDName(functionId);
    actualSequence += WCHAR("\n");

    return S_OK;
}
HRESULT ExceptionProfiler::ExceptionCatcherLeave()
{
    SHUTDOWNGUARD();

    actualSequence += String(WCHAR("ExceptionCatcherLeave"));
    actualSequence += WCHAR("\n");

    return S_OK;
}
