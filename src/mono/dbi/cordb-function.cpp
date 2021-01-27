// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-FUNCTION.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-code.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb.h>

using namespace std;

CordbFunction::CordbFunction(Connection *conn, mdToken token, int id,
                             CordbModule *module)
    : CordbBaseMono(conn) {
  this->token = token;
  this->id = id;
  code = NULL;
  this->module = module;
}

HRESULT __stdcall CordbFunction::QueryInterface(REFIID id, void **pInterface) {
  if (id == IID_ICorDebugFunction) {
    *pInterface = static_cast<ICorDebugFunction *>(this);
  } else if (id == IID_ICorDebugFunction2) {
    *pInterface = static_cast<ICorDebugFunction2 *>(this);
  } else if (id == IID_ICorDebugFunction3) {
    *pInterface = static_cast<ICorDebugFunction3 *>(this);
  } else if (id == IID_ICorDebugFunction4) {
    *pInterface = static_cast<ICorDebugFunction4 *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugFunction *>(this));
  } else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

ULONG __stdcall CordbFunction::AddRef(void) { return 0; }

ULONG __stdcall CordbFunction::Release(void) { return 0; }

HRESULT __stdcall CordbFunction::GetModule(ICorDebugModule **ppModule) {
  if (module == NULL) {
    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, id);
    int cmdId =
        conn->send_event(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_ASSEMBLY, &localbuf);
    m_dbgprot_buffer_free(&localbuf);

    DEBUG_PRINTF(
        1, "CordbFunction - GetModule - IMPLEMENTED - ENTREI NO 0.1 - %d\n",
        id);

    MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);

    int module_id = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);

    DEBUG_PRINTF(
        1, "CordbFunction - GetModule - IMPLEMENTED - ENTREI NO 0.2 - %d\n",
        module_id);

    module = (CordbModule *)g_hash_table_lookup(conn->ppCordb->modules,
                                                GINT_TO_POINTER(module_id));
  }

  *ppModule = static_cast<ICorDebugModule *>(this->module);
  DEBUG_PRINTF(1, "CordbFunction - GetModule - IMPLEMENTED - %p\n",
               this->module);

  if (!*ppModule)
    return S_FALSE;
  return S_OK;
}

HRESULT __stdcall CordbFunction::GetClass(ICorDebugClass **ppClass) {
  DEBUG_PRINTF(1, "CordbFunction - GetClass - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetToken(mdMethodDef *pMethodDef) {
  if (this->token == 0) {
    DEBUG_PRINTF(
        1, "CordbFunction - GetToken - IMPLEMENTED - ENTREI NO 0 - %d\n", id);
    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, id);
    int cmdId = conn->send_event(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_TOKEN, &localbuf);
    m_dbgprot_buffer_free(&localbuf);

    DEBUG_PRINTF(
        1, "CordbFunction - GetToken - IMPLEMENTED - ENTREI NO 0.1 - %d\n", id);

    MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);

    this->token = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  }
  *pMethodDef = this->token;
  DEBUG_PRINTF(1, "CordbFunction - GetToken - IMPLEMENTED - %d\n", *pMethodDef);
  return S_OK;
}

HRESULT __stdcall CordbFunction::GetILCode(ICorDebugCode **ppCode) {
  if (code == NULL)
    code = new CordbCode(conn, this);
  *ppCode = static_cast<ICorDebugCode *>(code);
  DEBUG_PRINTF(1, "CordbFunction - GetILCode - IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbFunction::GetNativeCode(ICorDebugCode **ppCode) {
  *ppCode = static_cast<ICorDebugCode *>(code);
  DEBUG_PRINTF(1, "CordbFunction - GetNativeCode - IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbFunction::CreateBreakpoint(
    ICorDebugFunctionBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1, "CordbFunction - CreateBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetLocalVarSigToken(mdSignature *pmdSig) {
  DEBUG_PRINTF(1, "CordbFunction - GetLocalVarSigToken - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetCurrentVersionNumber(
    ULONG32 *pnCurrentVersion) {
  *pnCurrentVersion = 1;
  DEBUG_PRINTF(1, "CordbFunction - GetCurrentVersionNumber - IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbFunction::SetJMCStatus(BOOL bIsJustMyCode) {
  DEBUG_PRINTF(1, "CordbFunction - SetJMCStatus - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetJMCStatus(BOOL *pbIsJustMyCode) {
  DEBUG_PRINTF(1, "CordbFunction - GetJMCStatus - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::EnumerateNativeCode(
    ICorDebugCodeEnum **ppCodeEnum) {
  DEBUG_PRINTF(1, "CordbFunction - EnumerateNativeCode - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetVersionNumber(ULONG32 *pnVersion) {
  *pnVersion = 1;
  DEBUG_PRINTF(1, "CordbFunction - GetVersionNumber - IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbFunction::GetActiveReJitRequestILCode(
    ICorDebugILCode **ppReJitedILCode) {
  DEBUG_PRINTF(
      1, "CordbFunction - GetActiveReJitRequestILCode - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::CreateNativeBreakpoint(
    ICorDebugFunctionBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1, "CordbFunction - CreateNativeBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}
