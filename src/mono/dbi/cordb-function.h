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
  int m_debuggerId;
  mdToken m_metadataToken;
  CordbCode *m_pCode;
  CordbModule *m_pModule;

public:
  CordbFunction(Connection *conn, mdToken token, int id, CordbModule *module);
  ULONG AddRef(void) { return (BaseAddRef()); }
  ULONG Release(void) { return (BaseRelease()); }
  const char *GetClassName() { return "CordbFunction"; }
  ~CordbFunction();
  int GetDebuggerId() const { return m_debuggerId; }
  HRESULT
  QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);

  HRESULT GetModule(ICorDebugModule **ppModule);
  HRESULT GetClass(ICorDebugClass **ppClass);
  HRESULT GetToken(mdMethodDef *pMethodDef);
  HRESULT GetILCode(ICorDebugCode **ppCode);
  HRESULT GetNativeCode(ICorDebugCode **ppCode);
  HRESULT
  CreateBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint);
  HRESULT
  GetLocalVarSigToken(mdSignature *pmdSig);
  HRESULT
  GetCurrentVersionNumber(ULONG32 *pnCurrentVersion);
  HRESULT SetJMCStatus(BOOL bIsJustMyCode);
  HRESULT GetJMCStatus(BOOL *pbIsJustMyCode);
  HRESULT
  EnumerateNativeCode(ICorDebugCodeEnum **ppCodeEnum);
  HRESULT GetVersionNumber(ULONG32 *pnVersion);
  HRESULT
  GetActiveReJitRequestILCode(ICorDebugILCode **ppReJitedILCode);
  HRESULT
  CreateNativeBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint);
  mdToken GetMetadataToken() const { return m_metadataToken; }
};

#endif
