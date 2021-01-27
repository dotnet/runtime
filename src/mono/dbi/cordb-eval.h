// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-EVAL.H
//

#ifndef __MONO_DEBUGGER_CORDB_EVAL_H__
#define __MONO_DEBUGGER_CORDB_EVAL_H__

#include <cordb.h>

class CordbEval : public CordbBaseMono,
                  public ICorDebugEval,
                  public ICorDebugEval2 {
  CordbThread *thread;
  ICorDebugValue *ppValue;

public:
  int cmdId;
  CordbEval(Connection *conn, CordbThread *thread);
  void EvalComplete(MdbgProtBuffer *bAnswer);

  virtual HRESULT STDMETHODCALLTYPE CallParameterizedFunction(
      ICorDebugFunction *pFunction, ULONG32 nTypeArgs,
      ICorDebugType *ppTypeArgs[], ULONG32 nArgs, ICorDebugValue *ppArgs[]);
  virtual HRESULT STDMETHODCALLTYPE NewParameterizedObject(
      ICorDebugFunction *pConstructor, ULONG32 nTypeArgs,
      ICorDebugType *ppTypeArgs[], ULONG32 nArgs, ICorDebugValue *ppArgs[]);
  virtual HRESULT STDMETHODCALLTYPE NewParameterizedObjectNoConstructor(
      ICorDebugClass *pClass, ULONG32 nTypeArgs, ICorDebugType *ppTypeArgs[]);
  virtual HRESULT STDMETHODCALLTYPE CallFunction(ICorDebugFunction *pFunction,
                                                 ULONG32 nArgs,
                                                 ICorDebugValue *ppArgs[]);
  virtual HRESULT STDMETHODCALLTYPE NewObject(ICorDebugFunction *pConstructor,
                                              ULONG32 nArgs,
                                              ICorDebugValue *ppArgs[]);
  virtual HRESULT STDMETHODCALLTYPE
  NewObjectNoConstructor(ICorDebugClass *pClass);
  virtual HRESULT STDMETHODCALLTYPE NewString(LPCWSTR string);
  virtual HRESULT STDMETHODCALLTYPE NewArray(CorElementType elementType,
                                             ICorDebugClass *pElementClass,
                                             ULONG32 rank, ULONG32 dims[],
                                             ULONG32 lowBounds[]);
  virtual HRESULT STDMETHODCALLTYPE IsActive(BOOL *pbActive);
  virtual HRESULT STDMETHODCALLTYPE Abort(void);
  virtual HRESULT STDMETHODCALLTYPE GetResult(ICorDebugValue **ppResult);
  virtual HRESULT STDMETHODCALLTYPE GetThread(ICorDebugThread **ppThread);
  virtual HRESULT STDMETHODCALLTYPE CreateValue(CorElementType elementType,
                                                ICorDebugClass *pElementClass,
                                                ICorDebugValue **ppValue);
  virtual HRESULT STDMETHODCALLTYPE
  CreateValueForType(ICorDebugType *pType, ICorDebugValue **ppValue);
  virtual HRESULT STDMETHODCALLTYPE
  NewParameterizedArray(ICorDebugType *pElementType, ULONG32 rank,
                        ULONG32 dims[], ULONG32 lowBounds[]);
  virtual HRESULT STDMETHODCALLTYPE NewStringWithLength(LPCWSTR string,
                                                        UINT uiLength);
  virtual HRESULT STDMETHODCALLTYPE RudeAbort(void);
  HRESULT QueryInterface(REFIID riid,
                         _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject);
  ULONG AddRef(void);
  ULONG Release(void);
};

#endif
