// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

#define NO_TRANSITION ((COR_PRF_TRANSITION_REASON)-1)

// Profiler used by the C++/CLI (IJW) profiler tests. It monitors code
// transitions and validates that every non-null FunctionID reported for a
// transition can be resolved via ICorProfilerInfo::GetFunctionInfo. This is a
// regression guard for https://github.com/dotnet/runtime/issues/120151, where a
// reverse (unmanaged->managed) marshaling stub reported a bogus, non-null
// FunctionID that crashed GetFunctionInfo.
//
// In addition to the "must resolve" check it validates the transition shape of
// the managed method invoked through a native function pointer: the target must
// be reported CALL on the way in and RETURN on the way out, and no nested code
// transitions may surround it. Reverse P/Invoke stubs no longer emit a
// stub-level transition, so the spurious nested callback that used to report a
// bogus FunctionID must not appear at all.
class IjwProfiler : public Profiler
{
public:
    IjwProfiler()
        : Profiler()
        , _failures(0)
        , _transitions(0)
        , _targetUnmanagedToManaged(NO_TRANSITION)
        , _targetManagedToUnmanaged(NO_TRANSITION)
        , _insideTarget(false)
        , _nestedTransitions(0)
    {}
    virtual ~IjwProfiler() = default;

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason);

private:
    std::atomic<int> _failures;
    std::atomic<int> _transitions;

    // Transition reasons recorded for the managed method reached through the
    // native function pointer (see IjwProfileeDll.cpp).
    COR_PRF_TRANSITION_REASON _targetUnmanagedToManaged;
    COR_PRF_TRANSITION_REASON _targetManagedToUnmanaged;

    // Set while executing the managed target so nested transitions can be
    // checked. The profilee is single-threaded around this call.
    std::atomic<bool> _insideTarget;

    // Number of nested code transitions observed while inside the target. Reverse
    // P/Invoke stubs no longer emit a stub-level transition, so this must be 0.
    std::atomic<int> _nestedTransitions;

    void HandleTransition(bool unmanagedToManaged, FunctionID functionID, COR_PRF_TRANSITION_REASON reason);
};
