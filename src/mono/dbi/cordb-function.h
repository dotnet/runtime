// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-FUNCTION.H
//

#ifndef __MONO_DEBUGGER_CORDB_FUNCTION_H__
#define __MONO_DEBUGGER_CORDB_FUNCTION_H__

#include <cordb-assembly.h>
#include <cordb.h>

class CordbFunction : public CordbBaseMono,
                      public ICorDebugFunction,
                      public ICorDebugFunction2,
                      public ICorDebugFunction3,
                      public ICorDebugFunction4 {
public:
  int id;
  mdToken token;
  CordbCode *code;
  CordbModule *module;

  CordbFunction(Connection *conn, mdToken token, int id, CordbModule *module);
  HRESULT STDMETHODCALLTYPE
  QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                 _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
  HRESULT STDMETHODCALLTYPE GetModule(/* [out] */ ICorDebugModule **ppModule);
  HRESULT STDMETHODCALLTYPE GetClass(/* [out] */ ICorDebugClass **ppClass);
  HRESULT STDMETHODCALLTYPE GetToken(/* [out] */ mdMethodDef *pMethodDef);
  HRESULT STDMETHODCALLTYPE GetILCode(/* [out] */ ICorDebugCode **ppCode);
  HRESULT STDMETHODCALLTYPE GetNativeCode(/* [out] */ ICorDebugCode **ppCode);
  HRESULT STDMETHODCALLTYPE
  CreateBreakpoint(/* [out] */ ICorDebugFunctionBreakpoint **ppBreakpoint);
  HRESULT STDMETHODCALLTYPE
  GetLocalVarSigToken(/* [out] */ mdSignature *pmdSig);
  HRESULT STDMETHODCALLTYPE
  GetCurrentVersionNumber(/* [out] */ ULONG32 *pnCurrentVersion);
  HRESULT STDMETHODCALLTYPE SetJMCStatus(/* [in] */ BOOL bIsJustMyCode);
  HRESULT STDMETHODCALLTYPE GetJMCStatus(/* [out] */ BOOL *pbIsJustMyCode);
  HRESULT STDMETHODCALLTYPE
  EnumerateNativeCode(/* [out] */ ICorDebugCodeEnum **ppCodeEnum);
  HRESULT STDMETHODCALLTYPE GetVersionNumber(/* [out] */ ULONG32 *pnVersion);
  HRESULT STDMETHODCALLTYPE
  GetActiveReJitRequestILCode(ICorDebugILCode **ppReJitedILCode);
  HRESULT STDMETHODCALLTYPE
  CreateNativeBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint);
};

#endif
