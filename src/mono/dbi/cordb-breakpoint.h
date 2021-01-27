// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BREAKPOINT.H
//

#ifndef __MONO_DEBUGGER_CORDB_BREAKPOINT_H__
#define __MONO_DEBUGGER_CORDB_BREAKPOINT_H__

#include <cordb.h>

class CordbFunctionBreakpoint : public CordbBaseMono,
                                public ICorDebugFunctionBreakpoint {
public:
  CordbCode *code;
  ULONG32 offset;
  CordbFunctionBreakpoint(Connection *conn, CordbCode *code, ULONG32 offset);
  HRESULT STDMETHODCALLTYPE
  GetFunction(/* [out] */ ICorDebugFunction **ppFunction);
  HRESULT STDMETHODCALLTYPE GetOffset(/* [out] */ ULONG32 *pnOffset);
  HRESULT STDMETHODCALLTYPE Activate(/* [in] */ BOOL bActive);
  HRESULT STDMETHODCALLTYPE IsActive(/* [out] */ BOOL *pbActive);
  HRESULT STDMETHODCALLTYPE
  QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                 _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
};

#endif
