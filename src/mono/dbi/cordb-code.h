// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CODE.H
//

#ifndef __MONO_DEBUGGER_CORDB_CODE_H__
#define __MONO_DEBUGGER_CORDB_CODE_H__

#include <cordb.h>

class CordbCode : public CordbBaseMono, public ICorDebugCode {
public:
  CordbFunction *func;
  CordbCode(Connection *conn, CordbFunction *func);
  HRESULT STDMETHODCALLTYPE IsIL(/* [out] */ BOOL *pbIL);
  HRESULT STDMETHODCALLTYPE
  GetFunction(/* [out] */ ICorDebugFunction **ppFunction);
  HRESULT STDMETHODCALLTYPE GetAddress(/* [out] */ CORDB_ADDRESS *pStart);
  HRESULT STDMETHODCALLTYPE GetSize(/* [out] */ ULONG32 *pcBytes);
  HRESULT STDMETHODCALLTYPE
  CreateBreakpoint(/* [in] */ ULONG32 offset, /* [out] */
                   ICorDebugFunctionBreakpoint **ppBreakpoint);
  HRESULT STDMETHODCALLTYPE GetCode(
      /* [in] */ ULONG32 startOffset, /* [in] */ ULONG32 endOffset, /* [in] */
      ULONG32 cBufferAlloc, /* [length_is][size_is][out] */ BYTE buffer[],
      /* [out] */ ULONG32 *pcBufferSize);
  HRESULT STDMETHODCALLTYPE GetVersionNumber(/* [out] */ ULONG32 *nVersion);
  HRESULT STDMETHODCALLTYPE
  GetILToNativeMapping(/* [in] */ ULONG32 cMap, /* [out] */ ULONG32 *pcMap,
                       /* [length_is][size_is][out] */
                       COR_DEBUG_IL_TO_NATIVE_MAP map[]);
  HRESULT STDMETHODCALLTYPE
  GetEnCRemapSequencePoints(/* [in] */ ULONG32 cMap, /* [out] */ ULONG32 *pcMap,
                            /* [length_is][size_is][out] */ ULONG32 offsets[]);
  HRESULT STDMETHODCALLTYPE
  QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                 _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
};

#endif
