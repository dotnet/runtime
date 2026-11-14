// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"
#include <atomic>
#include <vector>

class HotReloadProfiler : public Profiler
{
public:
    HotReloadProfiler() :
        Profiler(),
        _failures(0),
        _setBody(false),
        _sawAfterUpdate(false),
        _editedModule(0),
        _editedToken(mdMethodDefNil)
    {
    }

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock);

private:
    void RecordOriginalBody(ModuleID moduleId, mdMethodDef token);
    void CheckEditedBody();

    std::atomic<int> _failures;
    bool _setBody;
    bool _sawAfterUpdate;
    ModuleID _editedModule;
    mdMethodDef _editedToken;
    std::vector<BYTE> _originalBody;
};
