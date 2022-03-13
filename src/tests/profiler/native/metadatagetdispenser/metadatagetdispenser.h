// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

#include <unordered_set>
#include <map>
#include <memory>
#include <string>

typedef HRESULT (*GetDispenserFunc) (const CLSID &pClsid, const IID &pIid, void **ppv);

class MetaDataGetDispenser : public Profiler
{
public:
    MetaDataGetDispenser();
    virtual ~MetaDataGetDispenser();

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    virtual HRESULT STDMETHODCALLTYPE ModuleLoadStarted(ModuleID moduleId);

private:

    HRESULT GetDispenser(IMetaDataDispenserEx **disp);

    IMetaDataDispenserEx* _dispenser;
    std::atomic<int> _failures;
};
