// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "elttransitions.h"
#include <iostream>

using std::wcout;
using std::endl;

std::shared_ptr<EltTransitions> EltTransitions::s_profiler;

#define PROFILER_STUB static void STDMETHODCALLTYPE

PROFILER_STUB EnterStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();
    EltTransitions::s_profiler->EnterCallback(functionId, eltInfo);
}

PROFILER_STUB LeaveStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();
    EltTransitions::s_profiler->LeaveCallback(functionId, eltInfo);
}

PROFILER_STUB TailcallStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();
    EltTransitions::s_profiler->TailcallCallback(functionId, eltInfo);
}

GUID EltTransitions::GetClsid()
{
    // {C7A0B5D1-9E3F-4A21-8B77-1C2D3E4F5061}
    GUID clsid = { 0xC7A0B5D1, 0x9E3F, 0x4A21, { 0x8B, 0x77, 0x1C, 0x2D, 0x3E, 0x4F, 0x50, 0x61 } };
    return clsid;
}

HRESULT EltTransitions::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    EltTransitions::s_profiler = std::shared_ptr<EltTransitions>(this);

    HRESULT hr = S_OK;
    constexpr ULONG bufferSize = 1024;
    ULONG envVarLen = 0;
    WCHAR envVar[bufferSize];
    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("PInvoke_Transition_Expected_Name"), bufferSize, &envVarLen, envVar)))
    {
        _failures++;
        printf("FAIL: could not read PInvoke_Transition_Expected_Name hr=0x%x\n", hr);
        return E_FAIL;
    }
    _expectedPinvokeName = envVar;

    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("ReversePInvoke_Transition_Expected_Name"), bufferSize, &envVarLen, envVar)))
    {
        _failures++;
        printf("FAIL: could not read ReversePInvoke_Transition_Expected_Name hr=0x%x\n", hr);
        return E_FAIL;
    }
    _expectedReversePInvokeName = envVar;

    // The slow-path WithInfo hooks require the FUNCTION_ARGS/FUNCTION_RETVAL/FRAME_INFO flags.
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_ENTERLEAVE
                                                    | COR_PRF_ENABLE_FUNCTION_ARGS
                                                    | COR_PRF_ENABLE_FUNCTION_RETVAL
                                                    | COR_PRF_ENABLE_FRAME_INFO
                                                    | COR_PRF_MONITOR_CODE_TRANSITIONS
                                                    | COR_PRF_DISABLE_INLINING,
                                                    0)))
    {
        _failures++;
        printf("FAIL: SetEventMask2() failed hr=0x%x\n", hr);
        return hr;
    }

    if (FAILED(hr = pCorProfilerInfo->SetEnterLeaveFunctionHooks3WithInfo(EnterStub, LeaveStub, TailcallStub)))
    {
        _failures++;
        printf("FAIL: SetEnterLeaveFunctionHooks3WithInfo() failed hr=0x%x\n", hr);
        return hr;
    }

    return S_OK;
}

HRESULT EltTransitions::Shutdown()
{
    Profiler::Shutdown();

    // The forward P/Invoke must have transitioned managed->unmanaged (CALL) then back (RETURN).
    bool pinvokeTransitioned = _pinvokeManagedToUnmanaged == COR_PRF_TRANSITION_CALL
                            && _pinvokeUnmanagedToManaged == COR_PRF_TRANSITION_RETURN;

    // The regression: the forward P/Invoke must never be reported via ELT.
    bool noSpuriousElt = _pinvokeEltCount == 0;

    // Guard against an over-broad fix: the reverse P/Invoke target is a managed method and
    // must still receive ELT.
    bool reversePinvokeGotElt = _reversePinvokeEltCount > 0;

    if (_failures == 0 && pinvokeTransitioned && noSpuriousElt && reversePinvokeGotElt)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        auto boolFmt = [](bool b) { return b ? "true" : "false"; };
        printf("Test failed _failures=%d pinvokeTransitioned=%s noSpuriousElt=%s (pinvokeEltCount=%d) reversePinvokeGotElt=%s\n",
               _failures.load(), boolFmt(pinvokeTransitioned), boolFmt(noSpuriousElt),
               _pinvokeEltCount.load(), boolFmt(reversePinvokeGotElt));
    }
    fflush(stdout);
    return S_OK;
}

void EltTransitions::EnterCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    String name = GetFunctionIDName(functionId.functionID);
    if (name == _expectedPinvokeName)
    {
        _pinvokeEltCount++;
    }
    else if (name == _expectedReversePInvokeName)
    {
        _reversePinvokeEltCount++;
    }
}

void EltTransitions::LeaveCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    String name = GetFunctionIDName(functionId.functionID);
    if (name == _expectedPinvokeName)
    {
        _pinvokeEltCount++;
    }
}

void EltTransitions::TailcallCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    String name = GetFunctionIDName(functionId.functionID);
    if (name == _expectedPinvokeName)
    {
        _pinvokeEltCount++;
    }
}

HRESULT EltTransitions::ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    SHUTDOWNGUARD();

    String name = GetFunctionIDName(functionID);
    if (name == _expectedPinvokeName)
    {
        _pinvokeManagedToUnmanaged = reason;
    }

    return S_OK;
}

HRESULT EltTransitions::UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    SHUTDOWNGUARD();

    String name = GetFunctionIDName(functionID);
    if (name == _expectedPinvokeName)
    {
        _pinvokeUnmanagedToManaged = reason;
    }

    return S_OK;
}
