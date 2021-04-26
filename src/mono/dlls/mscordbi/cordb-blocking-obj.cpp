// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BLOCKING-OBJ.CPP
//

#include <cordb-blocking-obj.h>
#include <cordb.h>

CordbBlockingObjectEnum::CordbBlockingObjectEnum(Connection* conn) : CordbBaseMono(conn) {}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Next(ULONG                  celt,
                                                        CorDebugBlockingObject values[],
                                                        ULONG*                 pceltFetched)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbBlockingObjectEnum - Next - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Skip(ULONG celt)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbBlockingObjectEnum - Skip - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Reset(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbBlockingObjectEnum - Reset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Clone(ICorDebugEnum** ppEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbBlockingObjectEnum - Clone - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::GetCount(ULONG* pcelt)
{
    pcelt = 0;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::QueryInterface(REFIID id, void** ppInterface)
{
    if (id == IID_ICorDebugBlockingObjectEnum)
        *ppInterface = (ICorDebugBlockingObjectEnum*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorDebugBlockingObjectEnum*)this;
    LOG((LF_CORDB, LL_INFO100000, "CordbBlockingObjectEnum - QueryInterface - IMPLEMENTED\n"));
    AddRef();
    return S_OK;
}
