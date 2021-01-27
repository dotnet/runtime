// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BLOCKING-OBJ.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-blocking-obj.h>
#include <cordb-breakpoint.h>
#include <cordb-class.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb-type.h>
#include <cordb-value.h>
#include <cordb.h>

using namespace std;

CordbBlockingObjectEnum::CordbBlockingObjectEnum(Connection *conn)
    : CordbBaseMono(conn) {}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Next(
    ULONG celt, CorDebugBlockingObject values[], ULONG *pceltFetched) {
  DEBUG_PRINTF(1, "CordbBlockingObjectEnum - Next - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Skip(ULONG celt) {
  DEBUG_PRINTF(1, "CordbBlockingObjectEnum - Skip - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::Reset(void) {
  DEBUG_PRINTF(1, "CordbBlockingObjectEnum - Reset - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbBlockingObjectEnum::Clone(ICorDebugEnum **ppEnum) {
  DEBUG_PRINTF(1, "CordbBlockingObjectEnum - Clone - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbBlockingObjectEnum::GetCount(ULONG *pcelt) {
  pcelt = 0;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbBlockingObjectEnum::QueryInterface(REFIID riid, void **ppvObject) {
  DEBUG_PRINTF(1,
               "CordbBlockingObjectEnum - QueryInterface - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbBlockingObjectEnum::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbBlockingObjectEnum::Release(void) { return 0; }
