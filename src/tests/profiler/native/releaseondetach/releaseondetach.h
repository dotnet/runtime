// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

#include <string>

typedef HRESULT (*GetDispenserFunc) (const CLSID &pClsid, const IID &pIid, void **ppv);

// This test class is very small and doesn't do much. A repeated problem we had was that 
// if an ICorProfilerCallback* interface was added the developer would forget to add
// code to call release on the new interface. This test verifies that it is reclaimed
// after detach. It relies on the fact that the base Profiler class will be updated
// to the new ICorProfilerCallback* interface whenever the interface is added.
//
// If this test fails, it likely means you added an ICorProfilerCallback* interface
// and didn't add the corresponding Release call in ~EEToProfInterfaceImpl.
class ReleaseOnDetach : public Profiler
{
public:
    static ReleaseOnDetach *Instance;

    ReleaseOnDetach();
    virtual ~ReleaseOnDetach();

    virtual GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    virtual HRESULT STDMETHODCALLTYPE ProfilerAttachComplete();
    virtual HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded();

    void SetBoolPtr(void *ptr)
    {
        assert(ptr != NULL);
        _doneFlag = reinterpret_cast<bool *>(ptr);
    }

private:

    HRESULT GetDispenser(IMetaDataDispenserEx **disp);

    IMetaDataDispenserEx* _dispenser;
    std::atomic<int> _failures;
    bool _detachSucceeded;
    volatile bool *_doneFlag;
};
