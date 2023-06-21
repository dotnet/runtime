// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "assemblyprofiler.h"

GUID AssemblyProfiler::GetClsid()
{
    // TODO: Create a new one here I guess
	// {A040B953-EDE7-42D9-9077-AA69BB2BE024}
	GUID clsid = { 0xa040b953, 0xede7, 0x42d9,{ 0x90, 0x77, 0xaa, 0x69, 0xbb, 0x2b, 0xe0, 0x24 } };
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
