// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "profiler.h"
#include <atomic>

class ClassFactory : public IClassFactory
{
private:
    std::atomic<int> refCount;
	GUID clsid;
public:
    ClassFactory(REFCLSID clsid);
    virtual ~ClassFactory();
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject) override;
    ULONG   STDMETHODCALLTYPE AddRef(void) override;
    ULONG   STDMETHODCALLTYPE Release(void) override;
    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown *pUnkOuter, REFIID riid, void **ppvObject) override;
    HRESULT STDMETHODCALLTYPE LockServer(BOOL fLock) override;
};
