// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define NOMINMAX

#include "inlining.h"
#include <iostream>

using std::shared_ptr;
using std::wcout;
using std::endl;
using std::atomic;

shared_ptr<InliningProfiler> InliningProfiler::s_profiler;

#define PROFILER_STUB static void STDMETHODCALLTYPE

#ifndef WIN32
#define UINT_PTR_FORMAT "lx"
#else // WIN32
#define UINT_PTR_FORMAT "llx"
#endif // WIN32

PROFILER_STUB EnterStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();

    InliningProfiler::s_profiler->EnterCallback(functionId, eltInfo);
}

PROFILER_STUB LeaveStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();

    InliningProfiler::s_profiler->LeaveCallback(functionId, eltInfo);
}

PROFILER_STUB TailcallStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();

    InliningProfiler::s_profiler->TailcallCallback(functionId, eltInfo);
}

GUID InliningProfiler::GetClsid()
{
    // {DDADC0CB-21C8-4E53-9A6C-7C65EE5800CE}
    GUID clsid = { 0xDDADC0CB, 0x21C8, 0x4E53, { 0x9A, 0x6C, 0x7C, 0x65, 0xEE, 0x58, 0x00, 0xCE } };
    return clsid;
}

HRESULT InliningProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    InliningProfiler::s_profiler = shared_ptr<InliningProfiler>(this);

    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_ENTERLEAVE
                                                    | COR_PRF_ENABLE_FUNCTION_ARGS
                                                    | COR_PRF_ENABLE_FUNCTION_RETVAL
                                                    | COR_PRF_ENABLE_FRAME_INFO
                                                    | COR_PRF_MONITOR_JIT_COMPILATION,
                                                    0)))
    {
        wcout << L"FAIL: IpCorProfilerInfo::SetEventMask2() failed hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    hr = this->pCorProfilerInfo->SetEnterLeaveFunctionHooks3WithInfo(EnterStub, LeaveStub, TailcallStub);
    if (hr != S_OK)
    {
        wcout << L"SetEnterLeaveFunctionHooks3WithInfo failed with hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    return S_OK;
}

HRESULT InliningProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_failures == 0 && _sawInlineeCall)
    {
        wcout << L"PROFILER TEST PASSES" << endl;
    }
    else
    {
        wcout << L"TEST FAILED _failures=" << _failures.load() 
              << L"_sawInlineeCall=" << _sawInlineeCall << endl;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE InliningProfiler::EnterCallback(FunctionIDOrClientID functionIdOrClientID, COR_PRF_ELT_INFO eltInfo)
{
    String functionName = GetFunctionIDName(functionIdOrClientID.functionID);
    if (functionName == WCHAR("Inlining"))
    {
        _inInlining = true;
    }
    else if (functionName == WCHAR("BlockInlining"))
    {
        _inBlockInlining = true;
    }
    else if (functionName == WCHAR("NoResponse"))
    {
        _inNoResponse = true;
    }
    else if (functionName == WCHAR("Inlinee"))
    {
        if (_inInlining || _inNoResponse)
        {
            _failures++;
            wcout << L"Saw Inlinee as a real method call instead of inlined." << endl;
        }

        if (_inBlockInlining)
        {
            _sawInlineeCall = true;
        }
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE InliningProfiler::LeaveCallback(FunctionIDOrClientID functionIdOrClientID, COR_PRF_ELT_INFO eltInfo)
{
    String functionName = GetFunctionIDName(functionIdOrClientID.functionID);
    if (functionName == WCHAR("Inlining"))
    {
        _inInlining = false;
    }
    else if (functionName == WCHAR("BlockInlining"))
    {
        _inBlockInlining = false;
    }
    else if (functionName == WCHAR("NoResponse"))
    {
        _inNoResponse = false;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE InliningProfiler::TailcallCallback(FunctionIDOrClientID functionIdOrClientID, COR_PRF_ELT_INFO eltInfo)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE InliningProfiler::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline)
{
    String inlineeName = GetFunctionIDName(calleeId);
    if (inlineeName == WCHAR("Inlinee"))
    {
        String inlinerName = GetFunctionIDName(callerId);
        if (inlinerName == WCHAR("Inlining"))
        {
            *pfShouldInline = TRUE;
        }
        else if (inlinerName == WCHAR("BlockInlining"))
        {
            *pfShouldInline = FALSE;
        }
    }

    return S_OK;
}
