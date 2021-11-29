// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

#include <unordered_set>
#include <map>
#include <memory>
#include <string>

// TODO: right now this test exercises only one test, it should be generalized in the future.
// It currently hooks JitCompilationFinished looking for a trigger method, after
// the trigger method is seen it calls ReJITWithInliners on Inline.Inlinee and
// verifies that 3 rejits are seen (this is the expected number due to how the code is
// constructed). Then it reverts all three and verifies the reverts go through.
//
// In general it would be better to have a pinvoke in the profiler that can be accessed through
// C#, controlling behavior like the following.
//      ReJitMethod("Inline.exe", "Foo.Bar.Inlinee");
//      Inliner() // assert it has new behavior
//      RevertMethod("Inline.exe", "Foo.Bar.Inlinee");
//      Inliner() // assert it has original behavior

class ReJITProfiler : public Profiler
{
public:
    ReJITProfiler();
    virtual ~ReJITProfiler() = default;

	static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    virtual HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock);
    virtual HRESULT STDMETHODCALLTYPE JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock);
    virtual HRESULT STDMETHODCALLTYPE JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline);
    virtual HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result);

    virtual HRESULT STDMETHODCALLTYPE ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock);
    virtual HRESULT STDMETHODCALLTYPE GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl);
    virtual HRESULT STDMETHODCALLTYPE ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock);
    virtual HRESULT STDMETHODCALLTYPE ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus);

private:
    void AddInlining(FunctionID inliner, FunctionID inlinee);

    bool FunctionSeen(FunctionID func);

    FunctionID GetFunctionIDFromToken(ModuleID module, mdMethodDef token);
    mdMethodDef GetMethodDefForFunction(FunctionID functionId);
    ModuleID GetModuleIDForFunction(FunctionID functionId);

    ICorProfilerInfo10 *_profInfo10;
    std::atomic<int> _failures;
    std::atomic<int> _rejits;
    std::atomic<int> _reverts;
    std::map<FunctionID, std::shared_ptr<std::unordered_set<FunctionID>>> _inlinings;
    FunctionID _triggerFuncId;
    FunctionID _targetFuncId;
    ModuleID _targetModuleId;
    mdMethodDef _targetMethodDef;

    const String ReJITTriggerMethodName = WCHAR("TriggerReJIT");
    const String RevertTriggerMethodName = WCHAR("TriggerRevert");
    const String TargetMethodName = WCHAR("InlineeTarget");
    const String TargetModuleName = WCHAR("rejit.dll");
};
