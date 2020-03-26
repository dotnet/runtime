// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
