// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"
#include <memory>

#define NO_TRANSITION ((COR_PRF_TRANSITION_REASON)-1)

// Regression test for https://github.com/dotnet/runtime/issues/130242.
//
// Enables BOTH Enter/Leave/Tailcall (ELT) hooks and code-transition monitoring, then
// verifies that a forward P/Invoke is surfaced only through the ManagedToUnmanaged/
// UnmanagedToManaged transition callbacks and never through ELT. Before the fix, forward
// P/Invokes (JIT-compiled from transient IL on the P/Invoke MethodDesc) incorrectly passed
// the ELT gate in GetCompileFlags and fired spurious enter/leave callbacks that corrupted
// the profiler shadow stack. The reverse P/Invoke target is a managed method and must still
// receive ELT, which guards against an over-broad fix.
class EltTransitions : public Profiler
{
public:
    static std::shared_ptr<EltTransitions> s_profiler;

    EltTransitions() = default;
    virtual ~EltTransitions() = default;

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    virtual HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason);

    void EnterCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    void LeaveCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    void TailcallCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);

private:
    std::atomic<int> _failures{0};

    // Forward P/Invoke must NOT receive ELT (this count must stay 0).
    std::atomic<int> _pinvokeEltCount{0};
    // Reverse P/Invoke target (a managed method) must still receive ELT.
    std::atomic<int> _reversePinvokeEltCount{0};

    COR_PRF_TRANSITION_REASON _pinvokeManagedToUnmanaged{NO_TRANSITION};
    COR_PRF_TRANSITION_REASON _pinvokeUnmanagedToManaged{NO_TRANSITION};

    String _expectedPinvokeName;
    String _expectedReversePInvokeName;
};
