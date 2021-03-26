// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CHAIN.CPP
//

#include <cordb-blocking-obj.h>
#include <cordb-chain.h>
#include <cordb-frame.h>
#include <cordb-thread.h>
#include <cordb.h>

using namespace std;

HRESULT CordbChainEnum::Next(ULONG celt, ICorDebugChain* chains[], ULONG* pceltFetched)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChainEnum - Next - NOT IMPLEMENTED\n"));

    chains[0] = new CordbChain(conn, m_pThread, CHAIN_PROCESS_START, false);
    chains[1] = new CordbChain(conn, m_pThread, CHAIN_ENTER_MANAGED, true);
    chains[0]->AddRef();
    chains[1]->AddRef();
    *pceltFetched = celt;
    return S_OK;
}

CordbChainEnum::CordbChainEnum(Connection* conn, CordbThread* thread) : CordbBaseMono(conn)
{
    this->m_pThread = thread;
}

HRESULT CordbChainEnum::QueryInterface(REFIID id, void** pInterface)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChainEnum - QueryInterface - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbChainEnum::Skip(ULONG celt)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChainEnum - Skip - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChainEnum::Reset(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChainEnum - Reset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChainEnum::Clone(ICorDebugEnum** ppEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChainEnum - Clone - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChainEnum::GetCount(ULONG* pcelt)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChainEnum - GetCount - NOT IMPLEMENTED\n"));

    *pcelt = 2;
    return S_OK;
}

CordbChain::CordbChain(Connection* conn, CordbThread* thread, CorDebugChainReason chain_reason, bool is_managed)
    : CordbBaseMono(conn)
{
    this->m_pThread     = thread;
    this->m_chainReason = chain_reason;
    this->m_isManaged   = is_managed;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetThread(ICorDebugThread** ppThread)
{
    m_pThread->QueryInterface(IID_ICorDebugThread, (void**)ppThread);
    LOG((LF_CORDB, LL_INFO1000000, "CordbChain - GetThread - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetStackRange - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetContext(ICorDebugContext** ppContext)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetContext - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetCaller(ICorDebugChain** ppChain)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetCaller - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetCallee(ICorDebugChain** ppChain)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetCallee - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetPrevious(ICorDebugChain** ppChain)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetPrevious - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetNext(ICorDebugChain** ppChain)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetNext - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::IsManaged(BOOL* pManaged)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbChain - IsManaged - IMPLEMENTED\n"));
    *pManaged = m_isManaged;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbChain::EnumerateFrames(ICorDebugFrameEnum** ppFrames)
{
    CordbFrameEnum* pFrame = new CordbFrameEnum(conn, m_pThread);
    pFrame->AddRef();
    *ppFrames = static_cast<ICorDebugFrameEnum*>(pFrame);
    LOG((LF_CORDB, LL_INFO1000000, "CordbChain - EnumerateFrames - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetActiveFrame(ICorDebugFrame** ppFrame)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetActiveFrame - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetRegisterSet(ICorDebugRegisterSet** ppRegisters)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - GetRegisterSet - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbChain::GetReason(CorDebugChainReason* pReason)
{
    *pReason = m_chainReason;
    LOG((LF_CORDB, LL_INFO1000000, "CordbChain - GetReason - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbChain::QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbChain - QueryInterface - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}
