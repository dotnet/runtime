// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "rejitprofiler.h"
#include "ilrewriter.h"
#include <iostream>
#include <utility>

using std::map;
using std::unordered_set;
using std::make_pair;
using std::shared_ptr;
using std::make_shared;

#ifndef __FUNCTION_NAME__
    #ifdef WIN32   //WINDOWS
        #define __FUNCTION_NAME__   __FUNCTION__
    #else          //*NIX
        #define __FUNCTION_NAME__   __func__
    #endif
#endif

#define _MESSAGE(LVL, MSG) std::wcout << "**" << LVL << "** " << __FUNCTION_NAME__ << " - " << MSG << std::endl;
#define INFO(MSG) _MESSAGE("INFO", MSG)
#define FAIL(MSG) _MESSAGE("FAIL", MSG)

#ifdef __clang__
#pragma clang diagnostic ignored "-Wnull-arithmetic"
#endif // __clang__

ReJITProfiler::ReJITProfiler() : Profiler(),
    _profInfo10(nullptr),
    _failures(0),
    _rejits(0),
    _reverts(0),
    _inlinings(),
    _triggerFuncId(0),
    _targetFuncId(0),
    _targetModuleId(0),
    _targetMethodDef(mdTokenNil)
{

}

GUID ReJITProfiler::GetClsid()
{
    // {66F7A9DF-8858-4A32-9CFF-3AD0787E0186}
    GUID clsid = { 0x66F7A9DF, 0x8858, 0x4A32, {0x9C, 0xFF, 0x3A, 0xD0, 0x78, 0x7E, 0x01, 0x86 } };
    return clsid;
}

HRESULT ReJITProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    HRESULT hr = Profiler::Initialize(pICorProfilerInfoUnk);
    if (FAILED(hr))
    {
        _failures++;
        FAIL(L"Profiler::Initialize failed with hr=" << std::hex << hr);
        return hr;
    }

    hr = pICorProfilerInfoUnk->QueryInterface(IID_ICorProfilerInfo10, (void **)&_profInfo10);
    if (FAILED(hr))
    {
        _failures++;
        FAIL(L"Could not QI for ICorProfilerInfo10");
        return hr;
    }

    INFO(L"Initialize started");

    DWORD eventMaskLow = COR_PRF_ENABLE_REJIT | COR_PRF_MONITOR_JIT_COMPILATION;
    DWORD eventMaskHigh = 0x0;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(eventMaskLow, eventMaskHigh)))
    {
        _failures++;
        FAIL(L"ICorProfilerInfo::SetEventMask2() failed hr=0x" << std::hex << hr);
        return hr;
    }

    return S_OK;
}

HRESULT ReJITProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_profInfo10 != nullptr)
    {
        _profInfo10->Release();
        _profInfo10 = nullptr;
    }

    int expectedRejitCount = -1;
    auto it = _inlinings.find(_targetFuncId);
    if (it != _inlinings.end())
    {
        // The number of inliners are expected to ReJIT, plus the method itself
        expectedRejitCount = (int)((*it).second->size() + 1);
    }

    INFO(L" rejit count=" << _rejits << L" expected rejit count=" << expectedRejitCount);

    if(_failures == 0 && _rejits == expectedRejitCount)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        FAIL(L"Test failed number of failures=" << _failures);
    }
    fflush(stdout);

    return S_OK;
}

HRESULT STDMETHODCALLTYPE ReJITProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    ModuleID moduleId = GetModuleIDForFunction(functionId);
    String moduleName = GetModuleIDName(moduleId);

    String funcName = GetFunctionIDName(functionId);
    INFO(L"jitting started for " << funcName << L" in module " << moduleName);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE ReJITProfiler::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    String functionName = GetFunctionIDName(functionId);
    ModuleID moduleId = GetModuleIDForFunction(functionId);
    String moduleName = GetModuleIDName(moduleId);

    if (functionName == TargetMethodName && EndsWith(moduleName, TargetModuleName))
    {
        INFO(L"Found function id for target method");
        _targetFuncId = functionId;
        _targetModuleId = GetModuleIDForFunction(_targetFuncId);
        _targetMethodDef = GetMethodDefForFunction(_targetFuncId);
    }
    else if (functionName == ReJITTriggerMethodName && EndsWith(moduleName, TargetModuleName))
    {
        INFO(L"ReJIT Trigger method jitting finished: " << functionName);

        _triggerFuncId = functionId;

        INFO(L"Requesting rejit with inliners for method " << GetFunctionIDName(_targetFuncId));
        INFO(L"ModuleID=" << std::hex << _targetModuleId << L" and MethodDef=" << std::hex << _targetMethodDef);

        _profInfo10->RequestReJITWithInliners(COR_PRF_REJIT_BLOCK_INLINING | COR_PRF_REJIT_INLINING_CALLBACKS, 1, &_targetModuleId, &_targetMethodDef);
    }
    else if (functionName == RevertTriggerMethodName && EndsWith(moduleName, TargetModuleName))
    {
        INFO(L"Revert trigger method jitting finished: " << functionName);

        INFO(L"Requesting revert for method " << GetFunctionIDName(_targetFuncId));
        INFO(L"ModuleID=" << std::hex << _targetModuleId << L" and MethodDef=" << std::hex << _targetMethodDef);
        _profInfo10->RequestRevert(1, &_targetModuleId, &_targetMethodDef, nullptr);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE ReJITProfiler::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline)
{
    AddInlining(callerId, calleeId);
    *pfShouldInline = TRUE;

    String calleeName = GetFunctionIDName(calleeId);
    String moduleName = GetModuleIDName(GetModuleIDForFunction(calleeId));
    INFO(L"Inlining occurred! Inliner=" << GetFunctionIDName(callerId) << L" Inlinee=" << calleeName << L" Inlinee module name=" << moduleName);

    return S_OK;
}

HRESULT STDMETHODCALLTYPE ReJITProfiler::ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock)
{
    INFO(L"Saw a ReJIT for function " << GetFunctionIDName(functionId));
    _rejits++;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE ReJITProfiler::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl)
{
    INFO(L"Starting to build IL for methodDef=" << std::hex << methodId);
    COMPtrHolder<IUnknown> pUnk;
    HRESULT hr = _profInfo10->GetModuleMetaData(moduleId, ofWrite, IID_IMetaDataEmit2, &pUnk);
    if (FAILED(hr))
    {
        _failures++;
        FAIL(L"GetModuleMetaData failed for IID_IMetaDataEmit2 in ModuleID '" << std::hex << moduleId);
        return hr;
    }

    COMPtrHolder<IMetaDataEmit2> pTargetEmit;
    hr = pUnk->QueryInterface(IID_IMetaDataEmit2, (void **)&pTargetEmit);
    if (FAILED(hr))
    {
        _failures++;
        FAIL(L"Unable to QI for IMetaDataEmit2");
        return hr;
    }


    const WCHAR *wszNewUserDefinedString = WCHAR("Hello from profiler rejit!");
    mdString tokmdsUserDefined = mdTokenNil;
    hr = pTargetEmit->DefineUserString(wszNewUserDefinedString,
                                       (ULONG)wcslen(wszNewUserDefinedString),
                                       &tokmdsUserDefined);
    if (FAILED(hr))
    {
        _failures++;
        FAIL(L"DefineUserString failed");
        return S_OK;
    }

    ILRewriter rewriter(pCorProfilerInfo, pFunctionControl, moduleId, methodId);
    hr = rewriter.Import();
    if (FAILED(hr))
    {
        _failures++;
        FAIL(L"IL import failed");
        return hr;
    }

    ILInstr * pInstr = rewriter.GetILList();

    while (true)
    {
        if (pInstr->m_opcode == CEE_LDSTR)
        {
            INFO(L"Replaced function string with new one.");
            pInstr->m_Arg32 = tokmdsUserDefined;
        }

        pInstr = pInstr->m_pNext;
        if (pInstr == nullptr || pInstr == rewriter.GetILList())
        {
            break;
        }
    }

    hr = rewriter.Export();
    if (FAILED(hr))
    {
        _failures++;
        FAIL(L"IL export failed");
        return hr;
    }

    INFO(L"IL build sucessful for methodDef=" << std::hex << methodId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE ReJITProfiler::ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE ReJITProfiler::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    _failures++;

    FAIL(L"ReJIT error reported hr=" << std::hex << hrStatus);

    return S_OK;
}


void ReJITProfiler::AddInlining(FunctionID inliner, FunctionID inlinee)
{
    shared_ptr<unordered_set<FunctionID>> inliners;
    auto result = _inlinings.find(inlinee);
    if (result == _inlinings.end())
    {
        auto p = make_pair(inlinee, make_shared<unordered_set<FunctionID>>());
        inliners = p.second;
        _inlinings.insert(p);
    }
    else
    {
        inliners = (*result).second;
    }

    auto it = inliners->find(inliner);
    if (it == inliners->end())
    {
        inliners->insert(inliner);
    }
}

mdMethodDef ReJITProfiler::GetMethodDefForFunction(FunctionID functionId)
{
    ClassID classId = NULL;
    ModuleID moduleId = NULL;
    mdToken token = NULL;
    ULONG32 nTypeArgs = NULL;
    ClassID typeArgs[SHORT_LENGTH];
    COR_PRF_FRAME_INFO frameInfo = NULL;

    HRESULT hr = S_OK;
    hr = pCorProfilerInfo->GetFunctionInfo2(functionId,
                                            frameInfo,
                                            &classId,
                                            &moduleId,
                                            &token,
                                            SHORT_LENGTH,
                                            &nTypeArgs,
                                            typeArgs);
    return token;
}

ModuleID ReJITProfiler::GetModuleIDForFunction(FunctionID functionId)
{
    ClassID classId = NULL;
    ModuleID moduleId = NULL;
    mdToken token = NULL;
    ULONG32 nTypeArgs = NULL;
    ClassID typeArgs[SHORT_LENGTH];
    COR_PRF_FRAME_INFO frameInfo = NULL;

    HRESULT hr = S_OK;
    hr = pCorProfilerInfo->GetFunctionInfo2(functionId,
                                            frameInfo,
                                            &classId,
                                            &moduleId,
                                            &token,
                                            SHORT_LENGTH,
                                            &nTypeArgs,
                                            typeArgs);
    return moduleId;
}

bool ReJITProfiler::EndsWith(const String &lhs, const String &rhs)
{
    if (lhs.Size() < rhs.Size())
    {
        return false;
    }

    size_t lhsPos = lhs.Size() - rhs.Size();
    size_t rhsPos = 0;

    while (rhsPos < rhs.Size())
    {
        if (std::tolower(lhs[lhsPos]) != std::tolower(rhs[rhsPos]))
        {
            return false;
        }

        ++lhsPos;
        ++rhsPos;
    }

    return true;
}
