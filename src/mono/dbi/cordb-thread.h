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
                    public ICorDebugThread4 {
  long m_threadId;
  CordbProcess *m_pProcess;
  CordbStepper *m_pStepper;
  CordbRegisterSet *m_pRegisterSet;
  CordbNativeFrame *m_pCurrentFrame;
  CordbBlockingObjectEnum *m_pBlockingObject;

public:
  CordbThread(Connection *conn, CordbProcess *ppProcess, long thread_id);
  ULONG AddRef(void) { return (BaseAddRef()); }
  ULONG Release(void) { return (BaseRelease()); }
  const char *GetClassName() { return "CordbThread"; }
  ~CordbThread();
  void SetRegisterSet(CordbRegisterSet *rs);
  HRESULT HasUnhandledException(void);
  HRESULT
  GetBlockingObjects(ICorDebugBlockingObjectEnum **ppBlockingObjectEnum);
  HRESULT
  GetCurrentCustomDebuggerNotification(ICorDebugValue **ppNotificationObject);
  HRESULT
  CreateStackWalk(ICorDebugStackWalk **ppStackWalk);
  HRESULT
  GetActiveInternalFrames(ULONG32 cInternalFrames, ULONG32 *pcInternalFrames,
                          ICorDebugInternalFrame2 *ppInternalFrames[]);
  HRESULT GetActiveFunctions(ULONG32 cFunctions, ULONG32 *pcFunctions,
                             COR_ACTIVE_FUNCTION pFunctions[]);
  HRESULT
  GetConnectionID(CONNID *pdwConnectionId);
  HRESULT GetTaskID(TASKID *pTaskId);
  HRESULT GetVolatileOSThreadID(DWORD *pdwTid);
  HRESULT
  InterceptCurrentException(ICorDebugFrame *pFrame);
  HRESULT
  GetProcess(ICorDebugProcess **ppProcess);
  HRESULT GetID(DWORD *pdwThreadId);
  HRESULT GetHandle(HTHREAD *phThreadHandle);
  HRESULT
  GetAppDomain(ICorDebugAppDomain **ppAppDomain);
  HRESULT SetDebugState(CorDebugThreadState state);
  HRESULT
  GetDebugState(CorDebugThreadState *pState);
  HRESULT GetUserState(CorDebugUserState *pState);
  HRESULT
  GetCurrentException(ICorDebugValue **ppExceptionObject);
  HRESULT ClearCurrentException(void);
  HRESULT
  CreateStepper(ICorDebugStepper **ppStepper);
  HRESULT
  EnumerateChains(ICorDebugChainEnum **ppChains);
  HRESULT
  GetActiveChain(ICorDebugChain **ppChain);
  HRESULT
  GetActiveFrame(ICorDebugFrame **ppFrame);
  HRESULT
  GetRegisterSet(ICorDebugRegisterSet **ppRegisters);
  HRESULT CreateEval(ICorDebugEval **ppEval);
  HRESULT GetObject(ICorDebugValue **ppObject);
  HRESULT
  QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);

  long GetThreadId() const { return m_threadId; }
  CordbStepper *GetStepper() const { return m_pStepper; }
};

#endif
