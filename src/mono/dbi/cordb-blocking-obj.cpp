// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BLOCKING-OBJ.CPP
//

#include <cordb.h>
#include <cordb-blocking-obj.h>

CordbBlockingObjectEnum::CordbBlockingObjectEnum(Connection *conn)
    : CordbBaseMono(conn) {}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Next(
    ULONG celt, CorDebugBlockingObject values[], ULONG *pceltFetched) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbBlockingObjectEnum - Next - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Skip(ULONG celt) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbBlockingObjectEnum - Skip - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Reset(void) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbBlockingObjectEnum - Reset - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbBlockingObjectEnum::Clone(ICorDebugEnum **ppEnum) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbBlockingObjectEnum - Clone - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::GetCount(ULONG *pcelt) {
  pcelt = 0;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbBlockingObjectEnum::QueryInterface(REFIID riid, void **ppvObject) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbBlockingObjectEnum - QueryInterface - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbBlockingObjectEnum::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbBlockingObjectEnum::Release(void) { return 0; }
