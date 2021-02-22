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
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbChainEnum";
    }
    HRESULT
    QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    HRESULT Skip(ULONG celt);
    HRESULT Reset(void);
    HRESULT Clone(ICorDebugEnum** ppEnum);
    HRESULT GetCount(ULONG* pcelt);
    HRESULT
    Next(ULONG celt, ICorDebugChain* chains[], ULONG* pceltFetched);
};

class CordbChain : public CordbBaseMono, public ICorDebugChain
{
    CordbThread*        m_pThread;
    CorDebugChainReason m_chainReason;
    bool                m_isManaged;

public:
    CordbChain(Connection* conn, CordbThread* thread, CorDebugChainReason chain_reason, bool is_managed);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbChain";
    }
    HRESULT GetThread(ICorDebugThread** ppThread);
    HRESULT GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd);
    HRESULT
    GetContext(ICorDebugContext** ppContext);
    HRESULT GetCaller(ICorDebugChain** ppChain);
    HRESULT GetCallee(ICorDebugChain** ppChain);
    HRESULT GetPrevious(ICorDebugChain** ppChain);
    HRESULT GetNext(ICorDebugChain** ppChain);
    HRESULT IsManaged(BOOL* pManaged);
    HRESULT
    EnumerateFrames(ICorDebugFrameEnum** ppFrames);
    HRESULT
    GetActiveFrame(ICorDebugFrame** ppFrame);
    HRESULT
    GetRegisterSet(ICorDebugRegisterSet** ppRegisters);
    HRESULT GetReason(CorDebugChainReason* pReason);
    HRESULT
    QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);
};

#endif
