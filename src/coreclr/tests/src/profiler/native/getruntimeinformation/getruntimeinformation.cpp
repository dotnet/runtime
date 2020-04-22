// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "getruntimeinformation.h"
#include <string>
#include <assert.h>
#include <inttypes.h>
#include <sstream>


// Prints a lot to the console for easier tracking
#define DEBUG_OUT false

GetRuntimeInformation::GetRuntimeInformation() :
    failures(0)
{

}

GetRuntimeInformation::~GetRuntimeInformation()
{

}

GUID GetRuntimeInformation::GetClsid()
{
    // 4CF56B6D-F8FB-4056-AF4A-F6413DD738B1
    GUID clsid = { 0x4CF56B6D, 0xF8FB, 0x4056, { 0xAF, 0x4A, 0xF6, 0x41, 0x3D, 0xD7, 0x38, 0xB1 } };
    return clsid;
}

HRESULT GetRuntimeInformation::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    HRESULT hr = Profiler::Initialize(pICorProfilerInfoUnk);
    if (FAILED(hr))
    {
        failures++;
        printf("Profiler::Initialize failed with hr=0x%x\n", hr);
        return hr;
    }

    printf("GetRuntimeInformation::Initialize\n");

    USHORT clrInstanceId;
    COR_PRF_RUNTIME_TYPE runtimeType;
    USHORT majorVersion;
    USHORT minorVersion;
    USHORT buildNumber;
    USHORT qfeVersion;
    ULONG versionStringLength;

    hr = pCorProfilerInfo->GetRuntimeInformation(&clrInstanceId,
                                                 &runtimeType,
                                                 &majorVersion,
                                                 &minorVersion,
                                                 &buildNumber,
                                                 &qfeVersion,
                                                 0,
                                                 &versionStringLength,
                                                 NULL);
    if (FAILED(hr) && hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        printf("GetRuntimeInformation failed with hr=0x%x\n", hr);
        return E_FAIL;
    }
    
    // We should return the version even if the profiler doesn't want the version string
    if (runtimeType != COR_PRF_CORE_CLR
        || majorVersion == 0
        || minorVersion == 0
        || buildNumber == 0)
    {
        printf("First call to GetRuntimeInformation didn't return valid information. runtimeType=%u majorVersion=%u minorVersion=%u buildNumber=%u\n",
            runtimeType, majorVersion, minorVersion, buildNumber);
        failures++;
    }

    NewArrayHolder<WCHAR> versionString(new WCHAR[versionStringLength]);
    hr = pCorProfilerInfo->GetRuntimeInformation(&clrInstanceId,
                                                 &runtimeType,
                                                 &majorVersion,
                                                 &minorVersion,
                                                 &buildNumber,
                                                 &qfeVersion,
                                                 versionStringLength,
                                                 &versionStringLength,
                                                 (WCHAR *)versionString);
    if (FAILED(hr))
    {
        printf("GetRuntimeInformation failed with hr=0x%x\n", hr);
        return E_FAIL;
    }

    printf("clrInstanceId=%u\n", clrInstanceId);
    printf("runtimeType=%u\n", runtimeType);
    printf("majorVersion=%u\n", majorVersion);
    printf("minorVersion=%u\n", minorVersion);
    printf("buildNumber=%u\n", buildNumber);
    printf("qfeVersion=%u\n", qfeVersion);
    printf("versionStringLength=%u\n", versionStringLength);
    wprintf(L"versionString=%s\n", (WCHAR *)versionString);

    // NOTE: this is hardcoded by the test since the tests run under corerun, so it won't
    // change over time. It would be awesome to validate that it parses correctly over
    // time, but it's not currently possible with our infrastructure.
    if (runtimeType != COR_PRF_CORE_CLR
        || majorVersion != 5
        || minorVersion != 1
        || buildNumber != 306
        || versionStringLength == 0
        || versionString == NULL)
    {
        wprintf(L"Error parsing version string. runtimeType=%u majorVersion=%u minorVersion=%u buildNumber=%u versionString=%s\n",
            runtimeType, majorVersion, minorVersion, buildNumber, String(versionString).ToWString().c_str());
        failures++;
    }

    return S_OK;
}

HRESULT GetRuntimeInformation::Shutdown()
{
    Profiler::Shutdown();

    if(failures == 0)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        printf("Test failed number of failures=%d\n", failures.load());
    }
    fflush(stdout);

    return S_OK;
}
