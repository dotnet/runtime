// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "hotreloadprofiler.h"
#include <cstring>
#include <iostream>

using std::wcout;
using std::endl;

GUID HotReloadProfiler::GetClsid()
{
    // {C4A3B8E2-6F1D-4B7A-9E05-3D2F8A61B7C9}
    GUID clsid = { 0xC4A3B8E2, 0x6F1D, 0x4B7A, { 0x9E, 0x05, 0x3D, 0x2F, 0x8A, 0x61, 0xB7, 0xC9 } };
    return clsid;
}

HRESULT HotReloadProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    constexpr ULONG bufferSize = 1024;
    ULONG envVarLen = 0;
    WCHAR envVar[bufferSize];
    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("Profiler_Test_Name"), bufferSize, &envVarLen, envVar)))
    {
        wcout << L"FAIL: could not read the test name hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }
    _setBody = wcscmp(envVar, WCHAR("HotReloadGetILFunctionBodyAfterSetBody")) == 0;

    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_JIT_COMPILATION, 0)))
    {
        wcout << L"FAIL: SetEventMask2 failed hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    return S_OK;
}

HRESULT HotReloadProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_failures == 0 && _sawAfterUpdate)
    {
        wcout << L"PROFILER TEST PASSES" << endl;
    }
    else
    {
        wcout << L"TEST FAILED _failures=" << _failures.load() << L" _sawAfterUpdate=" << _sawAfterUpdate << endl;
    }

    return S_OK;
}

HRESULT HotReloadProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    SHUTDOWNGUARD();

    String name = GetFunctionIDName(functionId);
    if (name == WCHAR("Value") && _editedToken == mdMethodDefNil)
    {
        ModuleID moduleId;
        mdMethodDef token;
        if (FAILED(pCorProfilerInfo->GetFunctionInfo(functionId, NULL, &moduleId, &token)))
        {
            wcout << L"FAIL: GetFunctionInfo failed for Value" << endl;
            _failures++;
            return S_OK;
        }
        RecordOriginalBody(moduleId, token);
    }
    else if (name == WCHAR("AfterUpdate"))
    {
        _sawAfterUpdate = true;
        CheckEditedBody();
    }

    return S_OK;
}

void HotReloadProfiler::RecordOriginalBody(ModuleID moduleId, mdMethodDef token)
{
    LPCBYTE body = NULL;
    ULONG size = 0;
    HRESULT hr = pCorProfilerInfo->GetILFunctionBody(moduleId, token, &body, &size);
    if (FAILED(hr) || body == NULL || size == 0)
    {
        wcout << L"FAIL: GetILFunctionBody before the update hr=0x" << std::hex << hr << endl;
        _failures++;
        return;
    }

    _editedModule = moduleId;
    _editedToken = token;
    _originalBody.assign(body, body + size);

    if (_setBody)
    {
        // Give the method a profiler-set body (an unchanged copy) that the edit must take precedence over.
        COMPtrHolder<IMethodMalloc> malloc;
        if (FAILED(hr = pCorProfilerInfo->GetILFunctionBodyAllocator(moduleId, &malloc)))
        {
            wcout << L"FAIL: GetILFunctionBodyAllocator hr=0x" << std::hex << hr << endl;
            _failures++;
            return;
        }
        LPBYTE copy = (LPBYTE)malloc->Alloc(size);
        memcpy(copy, body, size);
        if (FAILED(hr = pCorProfilerInfo->SetILFunctionBody(moduleId, token, copy)))
        {
            wcout << L"FAIL: SetILFunctionBody hr=0x" << std::hex << hr << endl;
            _failures++;
        }
    }
}

void HotReloadProfiler::CheckEditedBody()
{
    if (_editedToken == mdMethodDefNil)
    {
        wcout << L"FAIL: Value was not compiled before the update" << endl;
        _failures++;
        return;
    }

    LPCBYTE body = NULL;
    ULONG size = 0;
    HRESULT hr = pCorProfilerInfo->GetILFunctionBody(_editedModule, _editedToken, &body, &size);
    if (FAILED(hr) || body == NULL)
    {
        wcout << L"FAIL: GetILFunctionBody after the update hr=0x" << std::hex << hr << endl;
        _failures++;
        return;
    }

    // The update changes `=> 1` to `=> 2`: the body must differ from the original in exactly one
    // byte, ldc.i4.1 (0x17) becoming ldc.i4.2 (0x18).
    int changed = 0;
    bool expectedChange = size == _originalBody.size();
    for (ULONG i = 0; expectedChange && i < size; i++)
    {
        if (body[i] != _originalBody[i])
        {
            changed++;
            expectedChange = _originalBody[i] == 0x17 && body[i] == 0x18;
        }
    }

    if (!expectedChange || changed != 1)
    {
        wcout << L"FAIL: GetILFunctionBody after the update did not return the edit (size " << std::dec << size
              << L", original size " << _originalBody.size() << L", bytes changed " << changed << L")" << endl;
        _failures++;
    }
}
