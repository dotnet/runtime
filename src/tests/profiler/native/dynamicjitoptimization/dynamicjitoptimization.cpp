
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dynamicjitoptimization.h"

static DynamicJitOptimizations* gInstance = nullptr;
static bool gDisableOptimizations = false;
static bool gDisableInlining = false;
static int gInlineCount = 0;

GUID DynamicJitOptimizations::GetClsid()
{
    // {C26D02FE-9E4C-484E-8984-F86724AA98B5}
    GUID clsid = { 0xc26d02fe, 0x9e4c, 0x484e, { 0x89, 0x84, 0xf8, 0x67, 0x24, 0xaa, 0x98, 0xb5 } };
    return clsid;
}

int SetEventMask()
{
    if (!gInstance)
    {
        return -1;
    }
    DWORD mask = COR_PRF_MONITOR_JIT_COMPILATION;
    if (gDisableOptimizations)
    {
        mask |= COR_PRF_DISABLE_OPTIMIZATIONS;
    }
    if (gDisableInlining)
    {
        mask |= COR_PRF_DISABLE_INLINING;
    }
    return gInstance->pCorProfilerInfo->SetEventMask2(mask, 0);
}

HRESULT DynamicJitOptimizations::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    gInstance = this;
    return SetEventMask();
}

HRESULT DynamicJitOptimizations::JITInlining(
    FunctionID callerId,
    FunctionID calleeId,
    BOOL      *pfShouldInline)
{
    SHUTDOWNGUARD();

    if (*pfShouldInline)
    {
        // filter for testee module
        ClassID classId = 0;
        ModuleID moduleId = 0;
        mdToken token = 0;
        ULONG32 nTypeArgs = 0;
        ClassID typeArgs[SHORT_LENGTH];
        COR_PRF_FRAME_INFO frameInfo = 0;

        HRESULT hr = S_OK;
        hr = pCorProfilerInfo->GetFunctionInfo2(callerId,
                                                frameInfo,
                                                &classId,
                                                &moduleId,
                                                &token,
                                                SHORT_LENGTH,
                                                &nTypeArgs,
                                                typeArgs);
        if (FAILED(hr))
        {
            printf("FAIL: GetFunctionInfo2 call failed with hr=0x%x\n", hr);
            return hr;
        }
        auto moduleName = GetModuleIDName(moduleId);
        if (EndsWith(moduleName, WCHAR("DynamicOptimizationTestLib.dll")))
        {
            gInlineCount++;
        }
    }
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
    gDisableOptimizations = disable;
    return SetEventMask();
}


extern "C" EXPORT int STDMETHODCALLTYPE SwitchInlining(bool disable)
{
    gDisableInlining = disable;
    return SetEventMask();
}

extern "C" EXPORT int STDMETHODCALLTYPE GetInlineCount(){
    return gInlineCount;
}
