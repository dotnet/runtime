// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "moduleload.h"

GUID ModuleLoad::GetClsid()
{
    // {1774B2E5-028B-4FA8-9DE5-26218CBCBBAC    }
    GUID clsid = {0x1774b2e5, 0x028b, 0x4fa8, {0x9d, 0xe5, 0x26, 0x21, 0x8c, 0xbc, 0xbb, 0xac}};
    return clsid;
}

HRESULT ModuleLoad::InitializeCommon(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    printf("Setting exception mask\n");
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_MODULE_LOADS | COR_PRF_MONITOR_ASSEMBLY_LOADS, 0)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT ModuleLoad::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    return InitializeCommon(pICorProfilerInfoUnk);
}

HRESULT ModuleLoad::InitializeForAttach(IUnknown* pICorProfilerInfoUnk, void* pvClientData, UINT cbClientData)
{
    return InitializeCommon(pICorProfilerInfoUnk);
}

HRESULT ModuleLoad::LoadAsNotificationOnly(BOOL *pbNotificationOnly)
{
    *pbNotificationOnly = TRUE;
    return S_OK;
}

HRESULT ModuleLoad::AssemblyLoadStarted(AssemblyID assemblyId)
{
    _assemblyLoadStartedCount++;
    return S_OK;
}

HRESULT ModuleLoad::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    _assemblyLoadFinishedCount++;
    return S_OK;
}

HRESULT ModuleLoad::ModuleLoadStarted(ModuleID moduleId)
{
    _moduleLoadStartedCount++;
    return S_OK;
}

HRESULT ModuleLoad::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    _moduleLoadFinishedCount++;
    return S_OK;
}


HRESULT ModuleLoad::Shutdown()
{
    Profiler::Shutdown();

    if(_failures == 0 
        && (_moduleLoadStartedCount != 0)
        && (_assemblyLoadStartedCount != 0)
        && (_moduleLoadStartedCount == _moduleLoadFinishedCount)
        && (_assemblyLoadStartedCount == _assemblyLoadFinishedCount))
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        printf("PROFILER TEST FAILED, failures=%d moduleLoadStarted=%d moduleLoadFinished=%d assemblyLoadStarted=%d assemblyLoadFinished=%d\n",
               _failures.load(),
               _moduleLoadStartedCount.load(),
               _moduleLoadFinishedCount.load(),
               _assemblyLoadStartedCount.load(),
               _assemblyLoadFinishedCount.load());
    }

    fflush(stdout);

    return S_OK;
}
