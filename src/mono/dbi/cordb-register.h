// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-REGISTER.H
//

#ifndef __MONO_DEBUGGER_CORDB_REGISTER_H__
#define __MONO_DEBUGGER_CORDB_REGISTER_H__

#include <cordb.h>

class CordbRegisteSet : public CordbBaseMono, public ICorDebugRegisterSet {
  guint8 *ctx;
  guint32 ctx_len;

public:
  CordbRegisteSet(Connection *conn, guint8 *ctx, guint32 ctx_len);
  HRESULT STDMETHODCALLTYPE
  QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                 _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
  HRESULT STDMETHODCALLTYPE
  GetRegistersAvailable(/* [out] */ ULONG64 *pAvailable);
  HRESULT STDMETHODCALLTYPE
  GetRegisters(/* [in] */ ULONG64 mask, /* [in] */ ULONG32 regCount,
               /* [length_is][size_is][out] */ CORDB_REGISTER regBuffer[]);
  HRESULT STDMETHODCALLTYPE SetRegisters(
      /* [in] */ ULONG64 mask, /* [in] */ ULONG32 regCount, /* [size_is][in] */
      CORDB_REGISTER regBuffer[]);
  HRESULT STDMETHODCALLTYPE GetThreadContext(
      /* [in] */ ULONG32 contextSize, /* [size_is][length_is][out][in] */
      BYTE context[]);
  HRESULT STDMETHODCALLTYPE SetThreadContext(
      /* [in] */ ULONG32 contextSize, /* [size_is][length_is][in] */
      BYTE context[]);
};

#endif
