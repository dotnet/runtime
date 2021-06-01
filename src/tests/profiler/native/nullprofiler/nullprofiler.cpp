// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "nullprofiler.h"

GUID NullProfiler::GetClsid()
{
    // {9C1A6E14-2DEC-45CE-9061-F31964D8884D}
    GUID clsid = { 0x9C1A6E14, 0x2DEC, 0x45CE,{ 0x90, 0x61, 0xF3, 0x19, 0x64, 0xD8, 0x88, 0x4D } };
    return clsid;
}

HRESULT NullProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    // This profiler does nothing, and passes if it is loaded at all
    printf("PROFILER TEST PASSES\n");

    return S_OK;
}

HRESULT NullProfiler::Shutdown()
{
    Profiler::Shutdown();

    fflush(stdout);

    return S_OK;
}
