// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-THREAD.H
//

#ifndef __MONO_DEBUGGER_CORDB_THREAD_H__
#define __MONO_DEBUGGER_CORDB_THREAD_H__

#include <cordb.h>

class CordbThread : public CordbBaseMono,
                    public ICorDebugThread,
                    public ICorDebugThread2,
                    public ICorDebugThread3,
                    public ICorDebugThread4
{
    long                     m_threadId;
    CordbProcess*            m_pProcess;
    CordbStepper*            m_pStepper;
    CordbRegisterSet*        m_pRegisterSet;
    CordbNativeFrame*        m_pCurrentFrame;
    CordbBlockingObjectEnum* m_pBlockingObject;

public:
    CordbThread(Connection* conn, CordbProcess* ppProcess, long thread_id);
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
        return "CordbThread";
    }
    ~CordbThread();
    void    SetRegisterSet(CordbRegisterSet* rs);
    HRESULT STDMETHODCALLTYPE HasUnhandledException(void);
    HRESULT STDMETHODCALLTYPE GetBlockingObjects(ICorDebugBlockingObjectEnum** ppBlockingObjectEnum);
    HRESULT STDMETHODCALLTYPE GetCurrentCustomDebuggerNotification(ICorDebugValue** ppNotificationObject);
    HRESULT STDMETHODCALLTYPE CreateStackWalk(ICorDebugStackWalk** ppStackWalk);
    HRESULT STDMETHODCALLTYPE GetActiveInternalFrames(ULONG32                  cInternalFrames,
                            ULONG32*                 pcInternalFrames,
                            ICorDebugInternalFrame2* ppInternalFrames[]);
    HRESULT STDMETHODCALLTYPE GetActiveFunctions(ULONG32 cFunctions, ULONG32* pcFunctions, COR_ACTIVE_FUNCTION pFunctions[]);
    HRESULT STDMETHODCALLTYPE GetConnectionID(CONNID* pdwConnectionId);
    HRESULT STDMETHODCALLTYPE GetTaskID(TASKID* pTaskId);
    HRESULT STDMETHODCALLTYPE GetVolatileOSThreadID(DWORD* pdwTid);
    HRESULT STDMETHODCALLTYPE InterceptCurrentException(ICorDebugFrame* pFrame);
    HRESULT STDMETHODCALLTYPE GetProcess(ICorDebugProcess** ppProcess);
    HRESULT STDMETHODCALLTYPE GetID(DWORD* pdwThreadId);
    HRESULT STDMETHODCALLTYPE GetHandle(HTHREAD* phThreadHandle);
    HRESULT STDMETHODCALLTYPE GetAppDomain(ICorDebugAppDomain** ppAppDomain);
    HRESULT STDMETHODCALLTYPE SetDebugState(CorDebugThreadState state);
    HRESULT STDMETHODCALLTYPE GetDebugState(CorDebugThreadState* pState);
    HRESULT STDMETHODCALLTYPE GetUserState(CorDebugUserState* pState);
    HRESULT STDMETHODCALLTYPE GetCurrentException(ICorDebugValue** ppExceptionObject);
    HRESULT STDMETHODCALLTYPE ClearCurrentException(void);
    HRESULT STDMETHODCALLTYPE CreateStepper(ICorDebugStepper** ppStepper);
    HRESULT STDMETHODCALLTYPE EnumerateChains(ICorDebugChainEnum** ppChains);
    HRESULT STDMETHODCALLTYPE GetActiveChain(ICorDebugChain** ppChain);
    HRESULT STDMETHODCALLTYPE GetActiveFrame(ICorDebugFrame** ppFrame);
    HRESULT STDMETHODCALLTYPE GetRegisterSet(ICorDebugRegisterSet** ppRegisters);
    HRESULT STDMETHODCALLTYPE CreateEval(ICorDebugEval** ppEval);
    HRESULT STDMETHODCALLTYPE GetObject(ICorDebugValue** ppObject);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    long GetThreadId() const
    {
        return m_threadId;
    }
};

class CordbThreadEnum : public CordbBaseMono, public ICorDebugThreadEnum
{
public:
    CordbThreadEnum(Connection* conn);
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
        return "CordbThreadEnum";
    }
    HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugThread* values[], ULONG* pceltFetched);
    HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
    HRESULT STDMETHODCALLTYPE Reset(void);
    HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE GetCount(ULONG* pcelt);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
};

#endif
