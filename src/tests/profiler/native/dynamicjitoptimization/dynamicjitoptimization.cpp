
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dynamicjitoptimization.h"

static DynamicJitOptimizations* gInstance = nullptr;

GUID DynamicJitOptimizations::GetClsid()
{
    // {C26D02FE-9E4C-484E-8984-F86724AA98B5}
    GUID clsid = { 0xc26d02fe, 0x9e4c, 0x484e, { 0x89, 0x84, 0xf8, 0x67, 0x24, 0xaa, 0x98, 0xb5 } };
    return clsid;
}

HRESULT DynamicJitOptimizations::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    gInstance = this;
    return S_OK;
}

HRESULT DynamicJitOptimizations::Shutdown()
{
    Profiler::Shutdown();

    gInstance = nullptr;
    return S_OK;
}

extern "C" EXPORT int STDMETHODCALLTYPE SwitchJitOptimization(bool disable)
{
    if(!gInstance){
        return -1;
    }
    return gInstance->pCorProfilerInfo->SetEventMask2(disable ? COR_PRF_DISABLE_OPTIMIZATIONS : 0, 0);
}
