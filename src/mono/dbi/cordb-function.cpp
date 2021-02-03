// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-FUNCTION.CPP
//

#include <cordb-code.h>
#include <cordb-function.h>
#include <cordb.h>

using namespace std;

CordbFunction::CordbFunction(Connection *conn, mdToken token, int id,
    ICorDebugModule *module)
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

HRESULT __stdcall CordbFunction::GetModule(ICorDebugModule** ppModule) {
    MdbgProtBuffer localbuf;
    if (!module) {
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, id);
        int cmdId = conn->send_event(MDBGPROT_CMD_SET_METHOD,
            MDBGPROT_CMD_METHOD_ASSEMBLY, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        MdbgProtBuffer* bAnswer = conn->get_answer(cmdId);

        int module_id =
            m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        conn->ppCordb->GetModule(module_id, &module);
    }

    if (!module)
      return S_FALSE;

    *ppModule = module;
    return S_OK;
}

HRESULT __stdcall CordbFunction::GetClass(ICorDebugClass **ppClass) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - GetClass - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetToken(mdMethodDef *pMethodDef) {
  if (this->token == 0) {
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - GetToken - IMPLEMENTED\n"));
    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, id);
    int cmdId = conn->send_event(MDBGPROT_CMD_SET_METHOD,
                                 MDBGPROT_CMD_METHOD_TOKEN, &localbuf);
    m_dbgprot_buffer_free(&localbuf);
    MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);

    this->token =
        m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  }
  *pMethodDef = this->token;
  return S_OK;
}

HRESULT __stdcall CordbFunction::GetILCode(ICorDebugCode **ppCode) {
  if (code == NULL)
    code = new CordbCode(conn, this);
  *ppCode = static_cast<ICorDebugCode *>(code);
  LOG((LF_CORDB, LL_INFO1000000, "CordbFunction - GetILCode - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbFunction::GetNativeCode(ICorDebugCode **ppCode) {
  *ppCode = static_cast<ICorDebugCode *>(code);
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbFunction - GetNativeCode - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbFunction::CreateBreakpoint(
    ICorDebugFunctionBreakpoint **ppBreakpoint) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - CreateBreakpoint - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetLocalVarSigToken(mdSignature *pmdSig) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - GetLocalVarSigToken - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetCurrentVersionNumber(
    ULONG32 *pnCurrentVersion) {
  *pnCurrentVersion = 1;
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbFunction - GetCurrentVersionNumber - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbFunction::SetJMCStatus(BOOL bIsJustMyCode) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - SetJMCStatus - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetJMCStatus(BOOL *pbIsJustMyCode) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - GetJMCStatus - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::EnumerateNativeCode(
    ICorDebugCodeEnum **ppCodeEnum) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - EnumerateNativeCode - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::GetVersionNumber(ULONG32 *pnVersion) {
  *pnVersion = 1;
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbFunction - GetVersionNumber - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbFunction::GetActiveReJitRequestILCode(
    ICorDebugILCode **ppReJitedILCode) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - GetActiveReJitRequestILCode - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbFunction::CreateNativeBreakpoint(
    ICorDebugFunctionBreakpoint **ppBreakpoint) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbFunction - CreateNativeBreakpoint - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}
