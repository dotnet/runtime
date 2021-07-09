// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BREAKPOINT.H
//

#ifndef __MONO_DEBUGGER_CORDB_BREAKPOINT_H__
#define __MONO_DEBUGGER_CORDB_BREAKPOINT_H__

#include <cordb.h>

class CordbFunctionBreakpoint : public CordbBaseMono, public ICorDebugFunctionBreakpoint
{
    CordbCode* m_pCode;
    ULONG32    m_offset;
    int        m_debuggerId;
    BOOL       m_bActive;

public:
    CordbFunctionBreakpoint(Connection* conn, CordbCode* code, ULONG32 offset);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    ULONG32 GetOffset() const
    {
        return m_offset;
    }
    CordbCode* GetCode() const
    {
        return m_pCode;
    }
    const char* GetClassName()
    {
        return "CordbFunctionBreakpoint";
    }
    ~CordbFunctionBreakpoint();
    HRESULT STDMETHODCALLTYPE GetFunction(ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE GetOffset(ULONG32* pnOffset);
    HRESULT STDMETHODCALLTYPE Activate(BOOL bActive);
    HRESULT STDMETHODCALLTYPE IsActive(BOOL* pbActive);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);
    int     GetDebuggerId() const
    {
        return m_debuggerId;
    }
};

#endif
