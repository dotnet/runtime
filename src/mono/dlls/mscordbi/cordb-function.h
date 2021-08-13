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
                      public ICorDebugFunction4
{
    int          m_debuggerId;
    mdToken      m_metadataToken;
    CordbCode*   m_pCode;
    CordbModule* m_pModule;

public:
    CordbFunction(Connection* conn, mdToken token, int id, CordbModule* module);
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
        return "CordbFunction";
    }
    ~CordbFunction();
    int GetDebuggerId() const
    {
        return m_debuggerId;
    }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    HRESULT STDMETHODCALLTYPE GetModule(ICorDebugModule** ppModule);
    HRESULT STDMETHODCALLTYPE GetClass(ICorDebugClass** ppClass);
    HRESULT STDMETHODCALLTYPE GetToken(mdMethodDef* pMethodDef);
    HRESULT STDMETHODCALLTYPE GetILCode(ICorDebugCode** ppCode);
    HRESULT STDMETHODCALLTYPE GetNativeCode(ICorDebugCode** ppCode);
    HRESULT STDMETHODCALLTYPE CreateBreakpoint(ICorDebugFunctionBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE GetLocalVarSigToken(mdSignature* pmdSig);
    HRESULT STDMETHODCALLTYPE GetCurrentVersionNumber(ULONG32* pnCurrentVersion);
    HRESULT STDMETHODCALLTYPE SetJMCStatus(BOOL bIsJustMyCode);
    HRESULT STDMETHODCALLTYPE GetJMCStatus(BOOL* pbIsJustMyCode);
    HRESULT STDMETHODCALLTYPE EnumerateNativeCode(ICorDebugCodeEnum** ppCodeEnum);
    HRESULT STDMETHODCALLTYPE GetVersionNumber(ULONG32* pnVersion);
    HRESULT STDMETHODCALLTYPE GetActiveReJitRequestILCode(ICorDebugILCode** ppReJitedILCode);
    HRESULT STDMETHODCALLTYPE CreateNativeBreakpoint(ICorDebugFunctionBreakpoint** ppBreakpoint);
    mdToken GetMetadataToken() const
    {
        return m_metadataToken;
    }
};

#endif
