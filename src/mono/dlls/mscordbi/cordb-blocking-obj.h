// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BLOCKING-OBJ.H
//

#ifndef __MONO_DEBUGGER_CORDB_BLOCKING_OBJ_H__
#define __MONO_DEBUGGER_CORDB_BLOCKING_OBJ_H__

#include <cordb.h>

class CordbBlockingObjectEnum : public CordbBaseMono, public ICorDebugBlockingObjectEnum
{
public:
    CordbBlockingObjectEnum(Connection* conn);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbBlockingObjectEnum";
    }
    HRESULT STDMETHODCALLTYPE Next(ULONG celt, CorDebugBlockingObject values[], ULONG* pceltFetched);
    HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
    HRESULT STDMETHODCALLTYPE Reset(void);
    HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE GetCount(ULONG* pcelt);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
};

#endif
