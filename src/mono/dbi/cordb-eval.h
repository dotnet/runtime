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
  CordbThread *m_pThread;
  ICorDebugValue *m_pValue;
  int m_commandId;

public:
  CordbEval(Connection *conn, CordbThread *thread);
  ~CordbEval();
  void EvalComplete(MdbgProtBuffer *pReply);

  HRESULT CallParameterizedFunction(ICorDebugFunction *pFunction,
                                    ULONG32 nTypeArgs,
                                    ICorDebugType *ppTypeArgs[], ULONG32 nArgs,
                                    ICorDebugValue *ppArgs[]);
  HRESULT NewParameterizedObject(ICorDebugFunction *pConstructor,
                                 ULONG32 nTypeArgs, ICorDebugType *ppTypeArgs[],
                                 ULONG32 nArgs, ICorDebugValue *ppArgs[]);
  HRESULT NewParameterizedObjectNoConstructor(ICorDebugClass *pClass,
                                              ULONG32 nTypeArgs,
                                              ICorDebugType *ppTypeArgs[]);
  HRESULT CallFunction(ICorDebugFunction *pFunction, ULONG32 nArgs,
                       ICorDebugValue *ppArgs[]);
  HRESULT NewObject(ICorDebugFunction *pConstructor, ULONG32 nArgs,
                    ICorDebugValue *ppArgs[]);
  HRESULT
  NewObjectNoConstructor(ICorDebugClass *pClass);
  HRESULT NewString(LPCWSTR string);
  HRESULT NewArray(CorElementType elementType, ICorDebugClass *pElementClass,
                   ULONG32 rank, ULONG32 dims[], ULONG32 lowBounds[]);
  HRESULT IsActive(BOOL *pbActive);
  HRESULT Abort(void);
  HRESULT GetResult(ICorDebugValue **ppResult);
  HRESULT GetThread(ICorDebugThread **ppThread);
  HRESULT CreateValue(CorElementType elementType, ICorDebugClass *pElementClass,
                      ICorDebugValue **ppValue);
  HRESULT
  CreateValueForType(ICorDebugType *pType, ICorDebugValue **ppValue);
  HRESULT
  NewParameterizedArray(ICorDebugType *pElementType, ULONG32 rank,
                        ULONG32 dims[], ULONG32 lowBounds[]);
  HRESULT NewStringWithLength(LPCWSTR string, UINT uiLength);
  HRESULT RudeAbort(void);
  HRESULT QueryInterface(REFIID riid,
                         _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject);
  ULONG AddRef(void) { return (BaseAddRef()); }
  ULONG Release(void) { return (BaseRelease()); }
  const char *GetClassName() { return "CordbEval"; }
  int GetCommandId() const { return m_commandId; }
};

#endif
