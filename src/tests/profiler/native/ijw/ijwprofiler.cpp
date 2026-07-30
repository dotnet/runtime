// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ijwprofiler.h"

GUID IjwProfiler::GetClsid()
{
    // {D6973314-9E66-4EAD-8129-9B1D3AD7CB85}
    GUID clsid = { 0xd6973314, 0x9e66, 0x4ead, { 0x81, 0x29, 0x9b, 0x1d, 0x3a, 0xd7, 0xcb, 0x85 } };
    return clsid;
}

HRESULT IjwProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_CODE_TRANSITIONS | COR_PRF_DISABLE_INLINING, 0)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x\n", hr);
        return hr;
    }

    return S_OK;
}

HRESULT IjwProfiler::Shutdown()
{
    Profiler::Shutdown();

    // The managed target reached through the native function pointer must have
    // been reported CALL on the way in and RETURN on the way out. This also
    // ensures the by-pointer scenario actually ran.
    bool targetOk = _targetUnmanagedToManaged == COR_PRF_TRANSITION_CALL
                 && _targetManagedToUnmanaged == COR_PRF_TRANSITION_RETURN;

    if (_failures == 0 && _transitions > 0 && targetOk)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        printf("Test failed _failures=%d _transitions=%d targetU2M=%d targetM2U=%d\n",
               _failures.load(), _transitions.load(),
               (int)_targetUnmanagedToManaged, (int)_targetManagedToUnmanaged);
    }
    fflush(stdout);
    return S_OK;
}

void IjwProfiler::HandleTransition(bool unmanagedToManaged, FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    _transitions++;

    const char* which = unmanagedToManaged ? "UnmanagedToManagedTransition" : "ManagedToUnmanagedTransition";

    // A non-null FunctionID must be a real MethodDesc that GetFunctionInfo can
    // resolve. Before the fix for https://github.com/dotnet/runtime/issues/120151
    // a reverse marshaling stub reported a bogus pointer here, and this call
    // would fail or crash.
    bool isTarget = false;
    if (functionID != 0)
    {
        ClassID classId = 0;
        ModuleID moduleId = 0;
        mdToken token = 0;
        HRESULT hr = pCorProfilerInfo->GetFunctionInfo(functionID, &classId, &moduleId, &token);
        if (FAILED(hr))
        {
            _failures++;
            printf("FAIL: %s reported FunctionID=0x%p (reason=%d) that GetFunctionInfo could not resolve hr=0x%x\n",
                   which, (void*)functionID, (int)reason, hr);
            fflush(stdout);
        }
        else
        {
            isTarget = GetFunctionIDName(functionID) == WCHAR("ManagedByPointerTarget");
        }
    }

    if (isTarget)
    {
        // Record the reason for each direction and open/close the window in which
        // the nested reverse-stub transitions are checked. The target should be
        // seen exactly once per direction.
        if (unmanagedToManaged)
        {
            if (_targetUnmanagedToManaged != NO_TRANSITION)
            {
                _failures++;
            }
            _targetUnmanagedToManaged = reason;
            if (reason == COR_PRF_TRANSITION_CALL)
            {
                _insideTarget = true;
            }
        }
        else
        {
            if (_targetManagedToUnmanaged != NO_TRANSITION)
            {
                _failures++;
            }
            _targetManagedToUnmanaged = reason;
            if (reason == COR_PRF_TRANSITION_RETURN)
            {
                _insideTarget = false;
            }
        }
        return;
    }

    // Any non-target transition seen while executing the managed target is a
    // nested reverse (unmanaged->managed) marshaling stub. Post-fix these report
    // a NULL FunctionID; a non-null value here is exactly the 120151 bug (a
    // resolvable-but-wrong pointer would slip past the check above).
    if (_insideTarget.load() && functionID != 0)
    {
        _failures++;
        printf("FAIL: nested %s inside target reported non-null FunctionID=0x%p (reason=%d)\n",
               which, (void*)functionID, (int)reason);
        fflush(stdout);
    }
}

HRESULT IjwProfiler::UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    SHUTDOWNGUARD();
    HandleTransition(/* unmanagedToManaged */ true, functionID, reason);
    return S_OK;
}

HRESULT IjwProfiler::ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason)
{
    SHUTDOWNGUARD();
    HandleTransition(/* unmanagedToManaged */ false, functionID, reason);
    return S_OK;
}
