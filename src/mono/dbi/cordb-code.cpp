// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CODE.CPP
//

#include <cordb-blocking-obj.h>
#include <cordb-breakpoint.h>
#include <cordb-code.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb.h>

using namespace std;

CordbCode::CordbCode(Connection *conn, CordbFunction *func)
    : CordbBaseMono(conn) {
  this->m_pFunction = func;
}

HRESULT __stdcall CordbCode::IsIL(BOOL *pbIL) {
  LOG((LF_CORDB, LL_INFO100000, "CordbCode - IsIL - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetFunction(ICorDebugFunction **ppFunction) {
  LOG((LF_CORDB, LL_INFO100000, "CordbCode - GetFunction - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetAddress(CORDB_ADDRESS *pStart) {
  LOG((LF_CORDB, LL_INFO100000, "CordbCode - GetAddress - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetSize(ULONG32 *pcBytes) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);

  m_dbgprot_buffer_add_id(&localbuf, this->GetFunction()->GetDebuggerId());
  int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD,
                              MDBGPROT_CMD_METHOD_GET_BODY, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  ReceivedReplyPacket *received_reply_packet = conn->GetReplyWithError(cmdId);
  CHECK_ERROR_RETURN_FALSE(received_reply_packet);
  MdbgProtBuffer *pReply = received_reply_packet->Buffer();

  int code_size = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
  *pcBytes = code_size;
  LOG((LF_CORDB, LL_INFO1000000, "CordbCode - GetSize - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbCode::CreateBreakpoint(
    ULONG32 offset, ICorDebugFunctionBreakpoint **ppBreakpoint) {
  // add it in a list to not recreate a already created breakpoint
  CordbFunctionBreakpoint *bp = new CordbFunctionBreakpoint(conn, this, offset);
  bp->QueryInterface(IID_ICorDebugFunctionBreakpoint, (void **)ppBreakpoint);
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbCode - CreateBreakpoint - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbCode::GetCode(ULONG32 startOffset, ULONG32 endOffset,
                                     ULONG32 cBufferAlloc, BYTE buffer[],
                                     ULONG32 *pcBufferSize) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);

  m_dbgprot_buffer_add_id(&localbuf, this->GetFunction()->GetDebuggerId());
  int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD,
                              MDBGPROT_CMD_METHOD_GET_BODY, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  ReceivedReplyPacket *received_reply_packet = conn->GetReplyWithError(cmdId);
  CHECK_ERROR_RETURN_FALSE(received_reply_packet);
  MdbgProtBuffer *pReply = received_reply_packet->Buffer();

  uint8_t *code = m_dbgprot_decode_byte_array(
      pReply->p, &pReply->p, pReply->end, (int32_t *)pcBufferSize);

  memcpy(buffer, code, *pcBufferSize);
  free(code);
  LOG((LF_CORDB, LL_INFO1000000, "CordbCode - GetCode - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbCode::GetVersionNumber(ULONG32 *nVersion) {
  *nVersion = 1;
  LOG((LF_CORDB, LL_INFO100000,
       "CordbCode - GetVersionNumber - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT __stdcall CordbCode::GetILToNativeMapping(
    ULONG32 cMap, ULONG32 *pcMap, COR_DEBUG_IL_TO_NATIVE_MAP map[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbCode - GetILToNativeMapping - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT __stdcall CordbCode::GetEnCRemapSequencePoints(ULONG32 cMap,
                                                       ULONG32 *pcMap,
                                                       ULONG32 offsets[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbCode - GetEnCRemapSequencePoints - NOT IMPLEMENTED\n"));
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
  AddRef();
  return S_OK;
}
