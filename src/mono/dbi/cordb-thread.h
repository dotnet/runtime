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
public:
  long thread_id;
  CordbProcess *ppProcess;
  CordbStepper *stepper;
  CordbRegisteSet *registerset;
  CordbThread(Connection *conn, CordbProcess *ppProcess, long thread_id);
  HRESULT STDMETHODCALLTYPE HasUnhandledException(void);
  HRESULT STDMETHODCALLTYPE GetBlockingObjects(
      /* [out] */ ICorDebugBlockingObjectEnum **ppBlockingObjectEnum);
  HRESULT STDMETHODCALLTYPE GetCurrentCustomDebuggerNotification(
      /* [out] */ ICorDebugValue **ppNotificationObject);
  HRESULT STDMETHODCALLTYPE
  CreateStackWalk(/* [out] */ ICorDebugStackWalk **ppStackWalk);
  HRESULT STDMETHODCALLTYPE
  GetActiveInternalFrames(/* [in] */ ULONG32 cInternalFrames, /* [out] */
                          ULONG32 *pcInternalFrames,
                          /* [length_is][size_is][out][in] */
                          ICorDebugInternalFrame2 *ppInternalFrames[]);
  HRESULT STDMETHODCALLTYPE GetActiveFunctions(
      /* [in] */ ULONG32 cFunctions, /* [out] */ ULONG32 *pcFunctions,
      /* [length_is][size_is][out][in] */
      COR_ACTIVE_FUNCTION pFunctions[]);
  HRESULT STDMETHODCALLTYPE
  GetConnectionID(/* [out] */ CONNID *pdwConnectionId);
  HRESULT STDMETHODCALLTYPE GetTaskID(/* [out] */ TASKID *pTaskId);
  HRESULT STDMETHODCALLTYPE GetVolatileOSThreadID(/* [out] */ DWORD *pdwTid);
  HRESULT STDMETHODCALLTYPE
  InterceptCurrentException(/* [in] */ ICorDebugFrame *pFrame);
  HRESULT STDMETHODCALLTYPE
  GetProcess(/* [out] */ ICorDebugProcess **ppProcess);
  HRESULT STDMETHODCALLTYPE GetID(/* [out] */ DWORD *pdwThreadId);
  HRESULT STDMETHODCALLTYPE GetHandle(/* [out] */ HTHREAD *phThreadHandle);
  HRESULT STDMETHODCALLTYPE
  GetAppDomain(/* [out] */ ICorDebugAppDomain **ppAppDomain);
  HRESULT STDMETHODCALLTYPE SetDebugState(/* [in] */ CorDebugThreadState state);
  HRESULT STDMETHODCALLTYPE
  GetDebugState(/* [out] */ CorDebugThreadState *pState);
  HRESULT STDMETHODCALLTYPE GetUserState(/* [out] */ CorDebugUserState *pState);
  HRESULT STDMETHODCALLTYPE
  GetCurrentException(/* [out] */ ICorDebugValue **ppExceptionObject);
  HRESULT STDMETHODCALLTYPE ClearCurrentException(void);
  HRESULT STDMETHODCALLTYPE
  CreateStepper(/* [out] */ ICorDebugStepper **ppStepper);
  HRESULT STDMETHODCALLTYPE
  EnumerateChains(/* [out] */ ICorDebugChainEnum **ppChains);
  HRESULT STDMETHODCALLTYPE
  GetActiveChain(/* [out] */ ICorDebugChain **ppChain);
  HRESULT STDMETHODCALLTYPE
  GetActiveFrame(/* [out] */ ICorDebugFrame **ppFrame);
  HRESULT STDMETHODCALLTYPE
  GetRegisterSet(/* [out] */ ICorDebugRegisterSet **ppRegisters);
  HRESULT STDMETHODCALLTYPE CreateEval(/* [out] */ ICorDebugEval **ppEval);
  HRESULT STDMETHODCALLTYPE GetObject(/* [out] */ ICorDebugValue **ppObject);
  HRESULT STDMETHODCALLTYPE
  QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                 _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
};

#endif
