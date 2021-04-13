// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-REGISTER.CPP
//

#include <cordb-assembly.h>
#include <cordb-register.h>
#include <cordb.h>

using namespace std;

HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG64* pAvailable)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbRegisterSet - GetRegistersAvailable - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

CordbRegisterSet::CordbRegisterSet(Connection* conn, uint8_t* ctx, uint32_t ctx_len) : CordbBaseMono(conn)
{
    this->m_pCtx   = ctx;
    this->m_ctxLen = ctx_len;
}

HRESULT CordbRegisterSet::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugRegisterSet)
    {
        *pInterface = static_cast<ICorDebugRegisterSet*>(static_cast<ICorDebugRegisterSet*>(this));
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugRegisterSet*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT CordbRegisterSet::GetRegisters(ULONG64 mask, ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbRegisterSet - GetRegisters - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbRegisterSet::SetRegisters(ULONG64 mask, ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbRegisterSet - SetRegisters - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbRegisterSet::GetThreadContext(ULONG32 contextSize, BYTE context[])
{
    if (m_ctxLen < contextSize)
        memcpy(context+POS_RAX, m_pCtx, m_ctxLen);
    LOG((LF_CORDB, LL_INFO100000, "CordbRegisterSet - GetThreadContext - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbRegisterSet::SetThreadContext(ULONG32 contextSize, BYTE context[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbRegisterSet - SetThreadContext - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}
