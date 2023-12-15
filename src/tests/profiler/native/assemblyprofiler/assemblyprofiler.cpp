// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "assemblyprofiler.h"

GUID AssemblyProfiler::GetClsid()
{
    GUID clsid = { 0x19A49007, 0x9E58, 0x4E31,{ 0xB6, 0x55, 0x83, 0xEC, 0x3B, 0x92, 0x4E, 0x7B } };
	return clsid;
}

HRESULT AssemblyProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    return S_OK;
}

HRESULT AssemblyProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_assemblyUnloadStartedCount != _assemblyUnloadFinishedCount)
    {
        printf("AssemblyProfiler::Shutdown: FAIL: Expected AssemblyUnloadStarted and AssemblyUnloadFinished to be called the same number of times\n");
    }
    else
    {
        printf("PROFILER TEST PASSES\n");
    }

    fflush(stdout);
    return S_OK;
}

HRESULT AssemblyProfiler::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    SHUTDOWNGUARD();

    _assemblyUnloadStartedCount++;
    return S_OK;
}

HRESULT AssemblyProfiler::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    SHUTDOWNGUARD();

    _assemblyUnloadFinishedCount++;
    return S_OK;
}
