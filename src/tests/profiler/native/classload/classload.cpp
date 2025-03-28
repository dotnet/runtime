// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "classload.h"

GUID ClassLoad::GetClsid()
{
    // {A1B2C3D4-E5F6-7890-1234-56789ABCDEF0}
    GUID clsid = {0xa1b2c3d4, 0xe5f6, 0x7890, {0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0}};
    return clsid;
}

HRESULT ClassLoad::InitializeCommon(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    printf("Setting COR_PRF_MONITOR_CLASS_LOADS mask\n");
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_CLASS_LOADS, 0)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT ClassLoad::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    return InitializeCommon(pICorProfilerInfoUnk);
}

HRESULT ClassLoad::InitializeForAttach(IUnknown* pICorProfilerInfoUnk, void* pvClientData, UINT cbClientData)
{
    return InitializeCommon(pICorProfilerInfoUnk);
}

HRESULT ClassLoad::LoadAsNotificationOnly(BOOL *pbNotificationOnly)
{
    *pbNotificationOnly = TRUE;
    return S_OK;
}

HRESULT ClassLoad::ClassLoadStarted(ClassID classId)
{
    _classLoadStartedCount++;
    return S_OK;
}

HRESULT ClassLoad::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    _classLoadFinishedCount++;
    return S_OK;
}

HRESULT ClassLoad::ClassUnloadStarted(ClassID classId)
{
    _classUnloadStartedCount++;
    wprintf(L"ClassUnloadStarted: %s\n", GetClassIDName(classId).ToCStr());

    return S_OK;
}

HRESULT ClassLoad::ClassUnloadFinished(ClassID classID, HRESULT hrStatus)
{
    _classUnloadFinishedCount++;
    return S_OK;
}


HRESULT ClassLoad::Shutdown()
{
    Profiler::Shutdown();

    if(_failures == 0 
        && (_classLoadStartedCount != 0)
        // Expect unloading of UnloadLibrary.TestClass and
        // List<UnloadLibrary.TestClass> with all its base classes with everything used in List constructor.
        && (_classUnloadStartedCount == 7)
        && (_classLoadStartedCount == _classLoadFinishedCount)
        && (_classUnloadStartedCount == _classUnloadFinishedCount))
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        printf("PROFILER TEST FAILED, failures=%d classLoadStartedCount=%d classLoadFinishedCount=%d classUnloadStartedCount=%d classUnloadFinishedCount=%d\n",
               _failures.load(),
               _classLoadStartedCount.load(),
               _classLoadFinishedCount.load(),
               _classUnloadStartedCount.load(),
               _classUnloadFinishedCount.load());
    }

    fflush(stdout);

    return S_OK;
}
