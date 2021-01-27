// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CHAIN.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-blocking-obj.h>
#include <cordb-breakpoint.h>
#include <cordb-chain.h>
#include <cordb-class.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb-type.h>
#include <cordb-value.h>
#include <cordb.h>

using namespace std;

HRESULT __stdcall CordbChainEnum::Next(ULONG celt, ICorDebugChain *chains[],
                                       ULONG *pceltFetched) {
  DEBUG_PRINTF(1, "CordbChainEnum - Next - IMPLEMENTED\n");

  chains[0] = new CordbChain(conn, thread, CHAIN_PROCESS_START, false);
  chains[1] = new CordbChain(conn, thread, CHAIN_ENTER_MANAGED, true);
  *pceltFetched = celt;
  return S_OK;
}

CordbChainEnum::CordbChainEnum(Connection *conn, CordbThread *thread)
    : CordbBaseMono(conn) {
  this->thread = thread;
}

HRESULT __stdcall CordbChainEnum::QueryInterface(REFIID id, void **pInterface) {
  DEBUG_PRINTF(1, "CordbChainEnum - QueryInterface - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbChainEnum::Skip(ULONG celt) {
  DEBUG_PRINTF(1, "CordbChainEnum - Skip - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbChainEnum::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbChainEnum::Release(void) { return 0; }

HRESULT STDMETHODCALLTYPE CordbChainEnum::Reset(void) {
  DEBUG_PRINTF(1, "CordbChainEnum - Reset - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChainEnum::Clone(
    /* [out] */ ICorDebugEnum **ppEnum) {
  DEBUG_PRINTF(1, "CordbChainEnum - Clone - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChainEnum::GetCount(
    /* [out] */ ULONG *pcelt) {
  DEBUG_PRINTF(1, "CordbChainEnum - GetCount - IMPLEMENTED\n");

  *pcelt = 2;
  return S_OK;
}

CordbChain::CordbChain(Connection *conn, CordbThread *thread,
                       CorDebugChainReason chain_reason, bool is_managed)
    : CordbBaseMono(conn) {
  this->thread = thread;
  this->chain_reason = chain_reason;
  this->is_managed = is_managed;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetThread(/* [out] */ ICorDebugThread **ppThread) {
  *ppThread = static_cast<ICorDebugThread *>(thread);
  DEBUG_PRINTF(1, "CordbChain - GetThread - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetStackRange(
    /* [out] */ CORDB_ADDRESS *pStart, /* [out] */ CORDB_ADDRESS *pEnd) {
  DEBUG_PRINTF(1, "CordbChain - GetStackRange - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetContext(/* [out] */ ICorDebugContext **ppContext) {
  DEBUG_PRINTF(1, "CordbChain - GetContext - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetCaller(/* [out] */ ICorDebugChain **ppChain) {
  DEBUG_PRINTF(1, "CordbChain - GetCaller - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetCallee(/* [out] */ ICorDebugChain **ppChain) {
  DEBUG_PRINTF(1, "CordbChain - GetCallee - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetPrevious(/* [out] */ ICorDebugChain **ppChain) {
  DEBUG_PRINTF(1, "CordbChain - GetPrevious - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetNext(/* [out] */ ICorDebugChain **ppChain) {
  DEBUG_PRINTF(1, "CordbChain - GetNext - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::IsManaged(/* [out] */ BOOL *pManaged) {
  DEBUG_PRINTF(1, "CordbChain - IsManaged - IMPLEMENTED\n");
  *pManaged = is_managed;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbChain::EnumerateFrames(/* [out] */ ICorDebugFrameEnum **ppFrames) {
  *ppFrames = new CordbFrameEnum(conn, thread);
  DEBUG_PRINTF(1, "CordbChain - EnumerateFrames - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetActiveFrame(/* [out] */ ICorDebugFrame **ppFrame) {
  DEBUG_PRINTF(1, "CordbChain - GetActiveFrame - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetRegisterSet(/* [out] */ ICorDebugRegisterSet **ppRegisters) {
  DEBUG_PRINTF(1, "CordbChain - GetRegisterSet - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbChain::GetReason(/* [out] */ CorDebugChainReason *pReason) {
  *pReason = chain_reason;
  DEBUG_PRINTF(1, "CordbChain - GetReason - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbChain::QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                           _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface) {
  DEBUG_PRINTF(1, "CordbChain - QueryInterface - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbChain::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbChain::Release(void) { return 0; }
