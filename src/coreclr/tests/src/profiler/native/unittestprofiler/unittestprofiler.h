// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "../profiler.h"

#include <unordered_set>
#include <map>
#include <memory>
#include <string>

typedef HRESULT (*GetDispenserFunc) (const CLSID &pClsid, const IID &pIid, void **ppv);

class UnitTestProfiler : public Profiler
{
public:
    UnitTestProfiler();
    virtual ~UnitTestProfiler();

    virtual GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    virtual HRESULT STDMETHODCALLTYPE ModuleLoadStarted(ModuleID moduleId);

private:

    HRESULT GetDispenser(IMetaDataDispenserEx **disp);

    IMetaDataDispenserEx* _dispenser;
    std::atomic<int> _failures;
};
