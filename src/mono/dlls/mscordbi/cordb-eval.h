// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-EVAL.H
//

#ifndef __MONO_DEBUGGER_CORDB_EVAL_H__
#define __MONO_DEBUGGER_CORDB_EVAL_H__

#include <cordb.h>

class CordbEval : public CordbBaseMono, public ICorDebugEval, public ICorDebugEval2
{
    CordbThread*    m_pThread;
    ICorDebugValue* m_pValue;
    int             m_commandId;

public:
    CordbEval(Connection* conn, CordbThread* thread);
    ~CordbEval();
    void EvalComplete(MdbgProtBuffer* pReply);

    HRESULT STDMETHODCALLTYPE CallParameterizedFunction(ICorDebugFunction* pFunction,
                                      ULONG32            nTypeArgs,
                                      ICorDebugType*     ppTypeArgs[],
                                      ULONG32            nArgs,
                                      ICorDebugValue*    ppArgs[]);
    HRESULT STDMETHODCALLTYPE NewParameterizedObject(ICorDebugFunction* pConstructor,
                                   ULONG32            nTypeArgs,
                                   ICorDebugType*     ppTypeArgs[],
                                   ULONG32            nArgs,
                                   ICorDebugValue*    ppArgs[]);
    HRESULT STDMETHODCALLTYPE NewParameterizedObjectNoConstructor(ICorDebugClass* pClass, ULONG32 nTypeArgs, ICorDebugType* ppTypeArgs[]);
    HRESULT STDMETHODCALLTYPE CallFunction(ICorDebugFunction* pFunction, ULONG32 nArgs, ICorDebugValue* ppArgs[]);
    HRESULT STDMETHODCALLTYPE NewObject(ICorDebugFunction* pConstructor, ULONG32 nArgs, ICorDebugValue* ppArgs[]);
    HRESULT STDMETHODCALLTYPE NewObjectNoConstructor(ICorDebugClass* pClass);
    HRESULT STDMETHODCALLTYPE NewString(LPCWSTR string);
    HRESULT STDMETHODCALLTYPE NewArray(
        CorElementType elementType, ICorDebugClass* pElementClass, ULONG32 rank, ULONG32 dims[], ULONG32 lowBounds[]);
    HRESULT STDMETHODCALLTYPE IsActive(BOOL* pbActive);
    HRESULT STDMETHODCALLTYPE Abort(void);
    HRESULT STDMETHODCALLTYPE GetResult(ICorDebugValue** ppResult);
    HRESULT STDMETHODCALLTYPE GetThread(ICorDebugThread** ppThread);
    HRESULT STDMETHODCALLTYPE CreateValue(CorElementType elementType, ICorDebugClass* pElementClass, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE CreateValueForType(ICorDebugType* pType, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE NewParameterizedArray(ICorDebugType* pElementType, ULONG32 rank, ULONG32 dims[], ULONG32 lowBounds[]);
    HRESULT STDMETHODCALLTYPE NewStringWithLength(LPCWSTR string, UINT uiLength);
    HRESULT STDMETHODCALLTYPE RudeAbort(void);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject);
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
        return "CordbEval";
    }
    int GetCommandId() const
    {
        return m_commandId;
    }
};

#endif
