// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-EVAL.CPP
//

#include <cordb-eval.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-thread.h>
#include <cordb-value.h>
#include <cordb.h>

#include "corerror.h"
#include "metamodel.h"
#include "metamodelpub.h"
#include "rwutil.h"
#include "stdafx.h"

CordbEval::CordbEval(Connection *conn, CordbThread *thread)
    : CordbBaseMono(conn) {
  this->thread = thread;
  ppValue = NULL;
  cmdId = -1;
}

HRESULT STDMETHODCALLTYPE CordbEval::CallParameterizedFunction(
    ICorDebugFunction *pFunction, ULONG32 nTypeArgs,
    ICorDebugType *ppTypeArgs[], ULONG32 nArgs, ICorDebugValue *ppArgs[]) {
  this->thread->ppProcess->Stop(false);
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbEval - CallParameterizedFunction - IMPLEMENTED\n"));

  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, thread->thread_id);
  m_dbgprot_buffer_add_int(&localbuf, 1);
  m_dbgprot_buffer_add_int(&localbuf, ((CordbFunction *)pFunction)->id);
  m_dbgprot_buffer_add_int(&localbuf, nArgs);
  for (int i = 0; i < nArgs; i++) {
    CorElementType ty;
    ppArgs[i]->GetType(&ty);
    CordbContent *cc;
    ppArgs[i]->GetAddress((CORDB_ADDRESS *)&cc);
    m_dbgprot_buffer_add_byte(&localbuf, ty);
    switch (ty) {
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
      m_dbgprot_buffer_add_int(&localbuf, cc->booleanValue);
      break;
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
      m_dbgprot_buffer_add_int(&localbuf, cc->charValue);
      break;
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
      m_dbgprot_buffer_add_int(&localbuf, cc->intValue);
      break;
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
      m_dbgprot_buffer_add_long(&localbuf, cc->longValue);
      break;
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_STRING:
      m_dbgprot_buffer_add_id(&localbuf, cc->intValue);
      break;
    }
  }
  cmdId = conn->send_event(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_INVOKE_METHOD,
                           &localbuf);
  m_dbgprot_buffer_free(&localbuf);
  conn->pending_eval->Append(this);
  return S_OK;
}

void CordbEval::EvalComplete(MdbgProtBuffer *bAnswer) {

  m_dbgprot_decode_byte(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  CordbObjectValue::CreateCordbValue(conn, bAnswer, &ppValue);
  conn->ppCordb->pCallback->EvalComplete(
      static_cast<ICorDebugAppDomain*>(thread->ppProcess->appdomains->Get(0)),
      static_cast<ICorDebugThread *>(thread),
      static_cast<ICorDebugEval *>(this));
}

HRESULT STDMETHODCALLTYPE
CordbEval::CreateValueForType(ICorDebugType *pType, ICorDebugValue **ppValue) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbEval - CreateValueForType - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewParameterizedObject(
    ICorDebugFunction *pConstructor, ULONG32 nTypeArgs,
    ICorDebugType *ppTypeArgs[], ULONG32 nArgs, ICorDebugValue *ppArgs[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbEval - NewParameterizedObject - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewParameterizedObjectNoConstructor(
    ICorDebugClass *pClass, ULONG32 nTypeArgs, ICorDebugType *ppTypeArgs[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbEval - NewParameterizedObjectNoConstructor - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbEval::NewParameterizedArray(ICorDebugType *pElementType, ULONG32 rank,
                                 ULONG32 dims[], ULONG32 lowBounds[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbEval - NewParameterizedArray - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewStringWithLength(LPCWSTR string,
                                                         UINT uiLength) {
  this->thread->ppProcess->Stop(false);
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, thread->thread_id);
  int cmdId = conn->send_event(MDBGPROT_CMD_SET_THREAD,
                               MDBGPROT_CMD_THREAD_GET_APPDOMAIN, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  int domainId = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);

  LPSTR szString;
  UTF8STR(string, szString);

  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, domainId);
  m_dbgprot_buffer_add_string(&localbuf, szString);
  this->cmdId =
      conn->send_event(MDBGPROT_CMD_SET_APPDOMAIN,
                       MDBGPROT_CMD_APPDOMAIN_CREATE_STRING, &localbuf);
  conn->pending_eval->Append(this);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbEval::RudeAbort(void) {
  LOG((LF_CORDB, LL_INFO100000, "CordbEval - RudeAbort - NOT IMPLEMENTED\n"));
  return S_OK;
}

ULONG CordbEval::AddRef(void) { return 1; }

ULONG CordbEval::Release(void) { return 1; }

HRESULT
CordbEval::QueryInterface(REFIID id,
                          _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface) {
  if (id == IID_ICorDebugEval) {
    *pInterface = static_cast<ICorDebugEval *>(this);
  } else if (id == IID_ICorDebugEval2) {
    *pInterface = static_cast<ICorDebugEval2 *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugEval *>(this));
  } else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbEval::CallFunction(ICorDebugFunction *pFunction,
                                                  ULONG32 nArgs,
                                                  ICorDebugValue *ppArgs[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbEval - CallFunction - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewObject(ICorDebugFunction *pConstructor,
                                               ULONG32 nArgs,
                                               ICorDebugValue *ppArgs[]) {
  LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewObject - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbEval::NewObjectNoConstructor(ICorDebugClass *pClass) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbEval - NewObjectNoConstructor - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewString(LPCWSTR string) {
  LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewString - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewArray(CorElementType elementType,
                                              ICorDebugClass *pElementClass,
                                              ULONG32 rank, ULONG32 dims[],
                                              ULONG32 lowBounds[]) {
  LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewArray - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::IsActive(BOOL *pbActive) {
  LOG((LF_CORDB, LL_INFO100000, "CordbEval - IsActive - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::Abort(void) {
  LOG((LF_CORDB, LL_INFO100000, "CordbEval - Abort - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbEval::GetResult(ICorDebugValue **ppResult) {
  *ppResult = ppValue;
  LOG((LF_CORDB, LL_INFO1000000, "CordbEval - GetResult - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbEval::GetThread(ICorDebugThread **ppThread) {
  LOG((LF_CORDB, LL_INFO100000, "CordbEval - GetThread - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::CreateValue(CorElementType elementType,
                                                 ICorDebugClass *pElementClass,
                                                 ICorDebugValue **ppValue) {
  CordbContent content_value;
  content_value.booleanValue = 0;
  CordbValue *value =
      new CordbValue(conn, elementType, content_value,
                     convert_mono_type_2_icordbg_size(elementType));
  LOG((LF_CORDB, LL_INFO1000000, "CordbEval - CreateValue - IMPLEMENTED\n"));
  value->QueryInterface(IID_ICorDebugValue, (void **)ppValue);
  return S_OK;
}
