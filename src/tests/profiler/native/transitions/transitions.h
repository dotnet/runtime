// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

#define NO_TRANSITION ((COR_PRF_TRANSITION_REASON)-1)

class Transitions : public Profiler
{
public:
    Transitions() = default;
    virtual ~Transitions() = default;

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason);

private:
    std::atomic<int> _failures;

    struct TransitionInstance
    {
        TransitionInstance()
            : UnmanagedToManaged{ NO_TRANSITION }
            , ManagedToUnmanaged{ NO_TRANSITION }
        { }

        COR_PRF_TRANSITION_REASON UnmanagedToManaged;
        COR_PRF_TRANSITION_REASON ManagedToUnmanaged;
    };

    TransitionInstance _pinvoke;
    TransitionInstance _reversePinvoke;
    String expectedPinvokeName;
    String expectedReversePInvokeName;

    bool FunctionIsTargetFunction(FunctionID functionID, TransitionInstance** inst);
};
