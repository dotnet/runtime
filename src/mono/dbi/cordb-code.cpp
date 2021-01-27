// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CODE.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-blocking-obj.h>
#include <cordb-breakpoint.h>
#include <cordb-chain.h>
#include <cordb-class.h>
#include <cordb-code.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb-type.h>
#include <cordb-value.h>
#include <cordb.h>

using namespace std;

CordbCode::CordbCode(Connection *conn, CordbFunction *func)
    : CordbBaseMono(conn) {
  this->func = func;
}

HRESULT __stdcall CordbCode::IsIL(BOOL *pbIL) {
  DEBUG_PRINTF(1, "CordbCode - IsIL - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetFunction(ICorDebugFunction **ppFunction) {
  DEBUG_PRINTF(1, "CordbCode - GetFunction - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetAddress(CORDB_ADDRESS *pStart) {
  DEBUG_PRINTF(1, "CordbCode - GetAddress - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetSize(ULONG32 *pcBytes) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);

  m_dbgprot_buffer_add_id(&localbuf, this->func->id);
  int cmdId = conn->send_event(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_GET_BODY, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  int code_size = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  *pcBytes = code_size;
  DEBUG_PRINTF(1, "CordbCode - GetSize - IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbCode::CreateBreakpoint(
    ULONG32 offset, ICorDebugFunctionBreakpoint **ppBreakpoint) {
  // add it in a list to not recreate a already created breakpoint
  CordbFunctionBreakpoint *bp = new CordbFunctionBreakpoint(conn, this, offset);
  *ppBreakpoint = static_cast<ICorDebugFunctionBreakpoint *>(bp);
  g_ptr_array_add(this->func->module->pProcess->cordb->breakpoints, bp);
  DEBUG_PRINTF(1, "CordbCode - CreateBreakpoint - IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbCode::GetCode(ULONG32 startOffset, ULONG32 endOffset,
                                     ULONG32 cBufferAlloc, BYTE buffer[],
                                     ULONG32 *pcBufferSize) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);

  m_dbgprot_buffer_add_id(&localbuf, this->func->id);
  int cmdId = conn->send_event(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_GET_BODY, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  guint8 *code = m_dbgprot_decode_byte_array(bAnswer->buf, &bAnswer->buf,
                                   bAnswer->end, pcBufferSize);

  memcpy(buffer, code, *pcBufferSize);
  DEBUG_PRINTF(1, "CordbCode - GetCode - IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbCode::GetVersionNumber(ULONG32 *nVersion) {
  *nVersion = 1;
  DEBUG_PRINTF(1, "CordbCode - GetVersionNumber - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT __stdcall CordbCode::GetILToNativeMapping(
    ULONG32 cMap, ULONG32 *pcMap, COR_DEBUG_IL_TO_NATIVE_MAP map[]) {
  DEBUG_PRINTF(1, "CordbCode - GetILToNativeMapping - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetEnCRemapSequencePoints(ULONG32 cMap,
                                                       ULONG32 *pcMap,
                                                       ULONG32 offsets[]) {
  DEBUG_PRINTF(1, "CordbCode - GetEnCRemapSequencePoints - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::QueryInterface(REFIID id, void **pInterface) {
  if (id == IID_ICorDebugCode) {
    *pInterface = static_cast<ICorDebugCode *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugCode *>(this));
  } else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }
  return S_OK;
}

ULONG __stdcall CordbCode::AddRef(void) { return 0; }

ULONG __stdcall CordbCode::Release(void) { return 0; }
