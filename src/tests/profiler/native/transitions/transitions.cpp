
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "transitions.h"

Transitions::Transitions() :
    _failures(0),
    _sawEnter(false),
    _sawLeave(false)
{

}

GUID Transitions::GetClsid()
{
    // {027AD7BB-578E-4921-B29F-B540363D83EC}
    GUID clsid = { 0x027AD7BB, 0x578E, 0x4921, { 0xB2, 0x9F, 0xB5, 0x40, 0x36, 0x3D, 0x83, 0xEC } };
    return clsid;
}

HRESULT Transitions::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_CODE_TRANSITIONS | COR_PRF_DISABLE_INLINING, 0)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT Transitions::Shutdown()
{
    Profiler::Shutdown();

    if (_failures == 0 && _sawEnter && _sawLeave)
    {
        // If we're here, that means we were Released enough to trigger the destructor
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        auto boolFmt = [](bool b) { return b ? "true" : "false"; };
        printf("Test failed _failures=%d _sawEnter=%s _sawLeave=%s\n", 
                _failures.load(), boolFmt(_sawEnter), boolFmt(_sawLeave));
    }

    return S_OK;
}

extern "C" EXPORT void STDMETHODCALLTYPE DoPInvoke(int i)
{
    printf("PInvoke received i=%d\n", i);
}

HRESULT Transitions::UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    if (FunctionIsTargetFunction(functionID))
    {
        _sawEnter = true;
    }

    return S_OK;
}

HRESULT Transitions::ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    if (FunctionIsTargetFunction(functionID))
    {
        _sawLeave = true;
    }

    return S_OK;
}

bool Transitions::FunctionIsTargetFunction(FunctionID functionID)
{
    String name = GetFunctionIDName(functionID);
    return name == WCHAR("DoPInvoke");
}