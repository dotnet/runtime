
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "transitions.h"

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

    constexpr ULONG bufferSize = 1024;
    ULONG envVarLen = 0;
    WCHAR envVar[bufferSize];
    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("PInvoke_Transition_Expected_Name"), bufferSize, &envVarLen, envVar)))
    {
        return E_FAIL;
    }
    expectedPinvokeName = envVar;
    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("ReversePInvoke_Transition_Expected_Name"), bufferSize, &envVarLen, envVar)))
    {
        return E_FAIL;
    }
    expectedReversePInvokeName = envVar;

    return S_OK;
}

HRESULT Transitions::Shutdown()
{
    Profiler::Shutdown();

    bool successPinvoke = _pinvoke.ManagedToUnmanaged == COR_PRF_TRANSITION_CALL
                    && _pinvoke.UnmanagedToManaged == COR_PRF_TRANSITION_RETURN;

    bool successReversePinvoke = _reversePinvoke.ManagedToUnmanaged == COR_PRF_TRANSITION_RETURN
                    && _reversePinvoke.UnmanagedToManaged == COR_PRF_TRANSITION_CALL;

    if (_failures == 0 && successPinvoke && successReversePinvoke)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        auto boolFmt = [](bool b) { return b ? "true" : "false"; };
        printf("Test failed _failures=%d _pinvoke=%s _reversePinvoke=%s\n",
                _failures.load(), boolFmt(successPinvoke), boolFmt(successReversePinvoke));
    }

    return S_OK;
}

extern "C" EXPORT void STDMETHODCALLTYPE DoPInvoke(int(*callback)(int), int i)
{
    printf("PInvoke received i=%d\n", callback(i));
}


HRESULT Transitions::UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    SHUTDOWNGUARD();

    TransitionInstance* inst;
    if (FunctionIsTargetFunction(functionID, &inst))
    {
        if (inst->UnmanagedToManaged != NO_TRANSITION)
        {
            // Report a failure for duplicate transitions.
            _failures++;
        }
        inst->UnmanagedToManaged = reason;
    }

    return S_OK;
}

HRESULT Transitions::ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    SHUTDOWNGUARD();

    TransitionInstance* inst;
    if (FunctionIsTargetFunction(functionID, &inst))
    {
        if (inst->ManagedToUnmanaged != NO_TRANSITION)
        {
            // Report a failure for duplicate transitions.
            _failures++;
        }
        inst->ManagedToUnmanaged = reason;
    }

    return S_OK;
}

bool Transitions::FunctionIsTargetFunction(FunctionID functionID, TransitionInstance** inst)
{
    String name = GetFunctionIDName(functionID);

    if (name == expectedPinvokeName)
    {
        *inst = &_pinvoke;
    }
    else if (name == expectedReversePInvokeName)
    {
        *inst = &_reversePinvoke;
    }
    else
    {
        return false;
    }

    return true;
}