// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CHAIN.H
//

#ifndef __MONO_DEBUGGER_CORDB_CHAIN_H__
#define __MONO_DEBUGGER_CORDB_CHAIN_H__

#include <cordb.h>

class CordbChainEnum : public CordbBaseMono, public ICorDebugChainEnum
{
    CordbThread* m_pThread;

public:
    CordbChainEnum(Connection* conn, CordbThread* thread);
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
        return "CordbChainEnum";
    }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
    HRESULT STDMETHODCALLTYPE Reset(void);
    HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE GetCount(ULONG* pcelt);
    HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugChain* chains[], ULONG* pceltFetched);
};

class CordbChain : public CordbBaseMono, public ICorDebugChain
{
    CordbThread*        m_pThread;
    CorDebugChainReason m_chainReason;
    bool                m_isManaged;

public:
    CordbChain(Connection* conn, CordbThread* thread, CorDebugChainReason chain_reason, bool is_managed);
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
        return "CordbChain";
    }
    HRESULT STDMETHODCALLTYPE GetThread(ICorDebugThread** ppThread);
    HRESULT STDMETHODCALLTYPE GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd);
    HRESULT STDMETHODCALLTYPE GetContext(ICorDebugContext** ppContext);
    HRESULT STDMETHODCALLTYPE GetCaller(ICorDebugChain** ppChain);
    HRESULT STDMETHODCALLTYPE GetCallee(ICorDebugChain** ppChain);
    HRESULT STDMETHODCALLTYPE GetPrevious(ICorDebugChain** ppChain);
    HRESULT STDMETHODCALLTYPE GetNext(ICorDebugChain** ppChain);
    HRESULT STDMETHODCALLTYPE IsManaged(BOOL* pManaged);
    HRESULT STDMETHODCALLTYPE EnumerateFrames(ICorDebugFrameEnum** ppFrames);
    HRESULT STDMETHODCALLTYPE GetActiveFrame(ICorDebugFrame** ppFrame);
    HRESULT STDMETHODCALLTYPE GetRegisterSet(ICorDebugRegisterSet** ppRegisters);
    HRESULT STDMETHODCALLTYPE GetReason(CorDebugChainReason* pReason);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);
};

#endif
