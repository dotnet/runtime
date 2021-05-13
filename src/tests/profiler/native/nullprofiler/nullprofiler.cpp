// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "nullprofiler.h"

#include <iostream>

using std::wcout;
using std::endl;

GUID NullProfiler::GetClsid()
{
    // {9C1A6E14-2DEC-45CE-9061-F31964D8884D}
    GUID clsid = { 0x9C1A6E14, 0x2DEC, 0x45CE,{ 0x90, 0x61, 0xF3, 0x19, 0x64, 0xD8, 0x88, 0x4D } };
    return clsid;
}

HRESULT NullProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    constexpr ULONG bufferSize = 1024;
    ULONG envVarLen = 0;
    WCHAR envVar[bufferSize];
    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("Profiler_Test_Name"),
                                                             bufferSize,
                                                             &envVarLen,
                                                             envVar)))
    {
        wcout << L"Failed to get test name hr=" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    if (wcscmp(envVar, WCHAR("ReverseStartup")) == 0)
    {
        if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("ReverseServerTest_OverwriteMe"),
                                                                 bufferSize,
                                                                 &envVarLen,
                                                                 envVar))
            || wcscmp(envVar, WCHAR("Overwritten")) != 0)
        {
            wcout << L"Failed to get test name hr=" << std::hex << hr << endl;
            _failures++;
            return hr;
        }

        hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("ReverseServerTest_ClearMe"),
                                                            bufferSize,
                                                            &envVarLen,
                                                            envVar);
        if (SUCCEEDED(hr))
        {
            wcout << L"ReverseServerTest_ClearMe was expected to be cleared, but we found it" << std::hex << hr << endl;
            _failures++;
            return E_FAIL;
        }
        // ERROR_ENVVAR_NOT_FOUND hr
        else if (hr != (HRESULT)0x800700CB)
        {
            wcout << L"ReverseServerTest_ClearMe returned an HR other than ENVVAR_NOT_FOUND" << std::hex << hr << endl;
            _failures++;
            return E_FAIL;   
        }
    }
    else
    {
        wcout << L"Unrecognized test name." << endl;
        _failures++;
        return E_FAIL;
    }

    return S_OK;
}

HRESULT NullProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_failures.load() == 0)
    {
        printf("PROFILER TEST PASSES\n");
    }

    fflush(stdout);

    return S_OK;
}
