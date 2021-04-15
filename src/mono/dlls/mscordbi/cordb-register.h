// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-REGISTER.H
//

#ifndef __MONO_DEBUGGER_CORDB_REGISTER_H__
#define __MONO_DEBUGGER_CORDB_REGISTER_H__

#include <cordb.h>

class CordbRegisterSet : public CordbBaseMono, public ICorDebugRegisterSet
{
    uint8_t* m_pCtx;
    uint32_t m_ctxLen;

public:
    CordbRegisterSet(Connection* conn, uint8_t* ctx, uint32_t ctx_len);
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
        return "CordbRegisterSet";
    }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    HRESULT STDMETHODCALLTYPE GetRegistersAvailable(ULONG64* pAvailable);
    HRESULT STDMETHODCALLTYPE GetRegisters(ULONG64 mask, ULONG32 regCount, CORDB_REGISTER regBuffer[]);
    HRESULT STDMETHODCALLTYPE SetRegisters(ULONG64 mask, ULONG32 regCount, CORDB_REGISTER regBuffer[]);
    HRESULT STDMETHODCALLTYPE GetThreadContext(ULONG32 contextSize, BYTE context[]);
    HRESULT STDMETHODCALLTYPE SetThreadContext(ULONG32 contextSize, BYTE context[]);
};

#endif
