// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-FRAME.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-code.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-register.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb-value.h>

using namespace std;

CordbFrameEnum::CordbFrameEnum(Connection *conn, CordbThread *thread)
    : CordbBaseMono(conn) {
  this->thread = thread;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Next(ULONG celt,
                                               ICorDebugFrame *frames[],
                                               ULONG *pceltFetched) {
  for (int i = 0; i < nframes; i++) {
    frames[i] = this->frames[i];
  }
  DEBUG_PRINTF(1, "CordbFrameEnum - Next - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Skip(ULONG celt) {
  DEBUG_PRINTF(1, "CordbFrameEnum - Skip - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Reset(void) {
  DEBUG_PRINTF(1, "CordbFrameEnum - Reset - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Clone(ICorDebugEnum **ppEnum) {
  DEBUG_PRINTF(1, "CordbFrameEnum - Clone - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::GetCount(ULONG *pcelt) {
  DEBUG_PRINTF(1, "CordbFrameEnum - GetCount - IMPLEMENTED\n");
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, thread->thread_id);
  m_dbgprot_buffer_add_int(&localbuf, 0);
  m_dbgprot_buffer_add_int(&localbuf, -1);

  int cmdId =
      conn->send_event(MDBGPROT_CMD_SET_THREAD, MDBGPROT_CMD_THREAD_GET_FRAME_INFO, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  nframes = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  frames = (CordbNativeFrame **)malloc(sizeof(CordbNativeFrame *) * nframes);

  for (int i = 0; i < nframes; i++) {
    int frameid = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    int methodId = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    int il_offset = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    int flags = m_dbgprot_decode_byte(bAnswer->buf, &bAnswer->buf, bAnswer->end);

    CordbNativeFrame *frame =
        new CordbNativeFrame(conn, frameid, methodId, il_offset, flags, thread);
    frames[i] = frame;
  }

  if (!thread->registerset)
    thread->registerset = new CordbRegisteSet(conn, 0, 0);

  *pcelt = nframes;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::QueryInterface(REFIID riid,
                                                         void **ppvObject) {
  DEBUG_PRINTF(1, "CordbFrameEnum - QueryInterface - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbFrameEnum::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbFrameEnum::Release(void) { return 0; }

CordbJITILFrame::CordbJITILFrame(Connection *conn, int frameid, int methodId,
                                 int il_offset, int flags, CordbThread *thread)
    : CordbBaseMono(conn) {
  this->frameid = frameid;
  this->methodId = methodId;
  this->il_offset = il_offset;
  this->flags = flags;
  this->thread = thread;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetChain(
    /* [out] */ ICorDebugChain **ppChain) {
  DEBUG_PRINTF(1, "CordbFrame - GetChain - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCode(
    /* [out] */ ICorDebugCode **ppCode) {
  DEBUG_PRINTF(1, "CordbFrame - GetCode - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetFunction(
    /* [out] */ ICorDebugFunction **ppFunction) {
  CordbFunction *func = thread->ppProcess->cordb->findFunction(methodId);
  if (!func) {
    func = new CordbFunction(conn, 0, methodId, NULL);
    g_ptr_array_add(thread->ppProcess->cordb->functions, func);
  }

  *ppFunction = static_cast<ICorDebugFunction *>(func);
  DEBUG_PRINTF(1, "CordbFrame - GetFunction\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetFunctionToken(
    /* [out] */ mdMethodDef *pToken) {
  DEBUG_PRINTF(1, "CordbFrame - GetFunctionToken - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetStackRange(
    /* [out] */ CORDB_ADDRESS *pStart,
    /* [out] */ CORDB_ADDRESS *pEnd) {
  *pStart = 4096;
  *pEnd = 8192;
  DEBUG_PRINTF(1,
               "CordbFrame - GetStackRange - NOT IMPLEMENTED - we need id?\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCaller(
    /* [out] */ ICorDebugFrame **ppFrame) {
  DEBUG_PRINTF(1, "CordbFrame - GetCaller - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCallee(
    /* [out] */ ICorDebugFrame **ppFrame) {
  DEBUG_PRINTF(1, "CordbFrame - GetCallee - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::CreateStepper(
    /* [out] */ ICorDebugStepper **ppStepper) {
  DEBUG_PRINTF(1, "CordbFrame - CreateStepper - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::QueryInterface(
    /* [in] */ REFIID id,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface) {
  if (id == IID_ICorDebugILFrame) {
    *pInterface = static_cast<ICorDebugILFrame *>(this);
  } else if (id == IID_ICorDebugILFrame2) {
    *pInterface = static_cast<ICorDebugILFrame2 *>(this);
  } else if (id == IID_ICorDebugILFrame3) {
    *pInterface = static_cast<ICorDebugILFrame3 *>(this);
  } else if (id == IID_ICorDebugILFrame4) {
    *pInterface = static_cast<ICorDebugILFrame4 *>(this);
  } else {
    *pInterface = NULL;

    DEBUG_PRINTF(1, "CordbFrame - QueryInterface - E_NOTIMPL\n");

    return E_NOINTERFACE;
  }
  DEBUG_PRINTF(1, "CordbFrame - QueryInterface - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::RemapFunction(ULONG32 newILOffset) {
  DEBUG_PRINTF(1, "CordbFrame - RemapFunction - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbJITILFrame::EnumerateTypeParameters(ICorDebugTypeEnum **ppTyParEnum) {
  DEBUG_PRINTF(1, "CordbFrame - EnumerateTypeParameters - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetReturnValueForILOffset(
    ULONG32 ILoffset, ICorDebugValue **ppReturnValue) {
  DEBUG_PRINTF(1, "CordbFrame - GetReturnValueForILOffset - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::EnumerateLocalVariablesEx(
    ILCodeKind flags, ICorDebugValueEnum **ppValueEnum) {
  DEBUG_PRINTF(1, "CordbFrame - EnumerateLocalVariablesEx - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetLocalVariableEx(
    ILCodeKind flags, DWORD dwIndex, ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbFrame - GetLocalVariableEx - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCodeEx(ILCodeKind flags,
                                                     ICorDebugCode **ppCode) {
  if (flags == ILCODE_REJIT_IL) {
    *ppCode = NULL;
  } else {
    CordbCode *code = new CordbCode(conn, NULL);
    *ppCode = static_cast<ICorDebugCode *>(code);
  }
  DEBUG_PRINTF(1, "CordbJITILFrame - GetCodeEx - IMPLEMENTED\n");
  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbJITILFrame::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbJITILFrame::Release(void) { return 0; }

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetIP(
    /* [out] */ ULONG32 *pnOffset,
    /* [out] */ CorDebugMappingResult *pMappingResult) {
  *pnOffset = il_offset;
  *pMappingResult = MAPPING_EXACT;
  DEBUG_PRINTF(1, "CordbFrame - GetIP - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::SetIP(
    /* [in] */ ULONG32 nOffset) {
  DEBUG_PRINTF(1, "CordbFrame - SetIP - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::EnumerateLocalVariables(
    /* [out] */ ICorDebugValueEnum **ppValueEnum) {
  DEBUG_PRINTF(1, "CordbFrame - EnumerateLocalVariables - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetLocalVariable(
    /* [in] */ DWORD dwIndex,
    /* [out] */ ICorDebugValue **ppValue) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, thread->thread_id);
  m_dbgprot_buffer_add_id(&localbuf, frameid);
  m_dbgprot_buffer_add_int(&localbuf, 1);
  m_dbgprot_buffer_add_int(&localbuf, dwIndex);

  int cmdId = conn->send_event(MDBGPROT_CMD_SET_STACK_FRAME, MDBGPROT_CMD_STACK_FRAME_GET_VALUES,
                               &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  return CordbObjectValue::CreateCordbValue(conn, bAnswer, ppValue);
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::EnumerateArguments(
    /* [out] */ ICorDebugValueEnum **ppValueEnum) {
  DEBUG_PRINTF(1, "CordbFrame - EnumerateArguments - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetArgument(
    /* [in] */ DWORD dwIndex,
    /* [out] */ ICorDebugValue **ppValue) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, thread->thread_id);
  m_dbgprot_buffer_add_id(&localbuf, frameid);

  m_dbgprot_buffer_add_int(&localbuf, dwIndex);
  int cmdId = conn->send_event(MDBGPROT_CMD_SET_STACK_FRAME,
                               MDBGPROT_CMD_STACK_FRAME_GET_ARGUMENT, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  DEBUG_PRINTF(1, "CordbFrame - GetArgument - IMPLEMENTED - dwIndex - %d\n",
               dwIndex);
  return CordbObjectValue::CreateCordbValue(conn, bAnswer, ppValue);
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetStackDepth(
    /* [out] */ ULONG32 *pDepth) {
  DEBUG_PRINTF(1, "CordbFrame - GetStackDepth - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetStackValue(
    /* [in] */ DWORD dwIndex,
    /* [out] */ ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbFrame - GetStackValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::CanSetIP(
    /* [in] */ ULONG32 nOffset) {
  DEBUG_PRINTF(1, "CordbFrame - CanSetIP - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

CordbNativeFrame::CordbNativeFrame(Connection *conn, int frameid, int methodId,
                                   int il_offset, int flags,
                                   CordbThread *thread)
    : CordbBaseMono(conn) {
  m_JITILFrame =
      new CordbJITILFrame(conn, frameid, methodId, il_offset, flags, thread);
  this->thread = thread;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetIP(ULONG32 *pnOffset) {
  DEBUG_PRINTF(1, "CordbNativeFrame - GetIP - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::SetIP(ULONG32 nOffset) {
  DEBUG_PRINTF(1, "CordbNativeFrame - SetIP - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbNativeFrame::GetRegisterSet(ICorDebugRegisterSet **ppRegisters) {
  return thread->GetRegisterSet(ppRegisters);
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalRegisterValue(
    CorDebugRegister reg, ULONG cbSigBlob, PCCOR_SIGNATURE pvSigBlob,
    ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1,
               "CordbNativeFrame - GetLocalRegisterValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalDoubleRegisterValue(
    CorDebugRegister highWordReg, CorDebugRegister lowWordReg, ULONG cbSigBlob,
    PCCOR_SIGNATURE pvSigBlob, ICorDebugValue **ppValue) {
  DEBUG_PRINTF(
      1, "CordbNativeFrame - GetLocalDoubleRegisterValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalMemoryValue(
    CORDB_ADDRESS address, ULONG cbSigBlob, PCCOR_SIGNATURE pvSigBlob,
    ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbNativeFrame - GetLocalMemoryValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalRegisterMemoryValue(
    CorDebugRegister highWordReg, CORDB_ADDRESS lowWordAddress, ULONG cbSigBlob,
    PCCOR_SIGNATURE pvSigBlob, ICorDebugValue **ppValue) {
  DEBUG_PRINTF(
      1, "CordbNativeFrame - GetLocalRegisterMemoryValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalMemoryRegisterValue(
    CORDB_ADDRESS highWordAddress, CorDebugRegister lowWordRegister,
    ULONG cbSigBlob, PCCOR_SIGNATURE pvSigBlob, ICorDebugValue **ppValue) {
  DEBUG_PRINTF(
      1, "CordbNativeFrame - GetLocalMemoryRegisterValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::CanSetIP(ULONG32 nOffset) {
  DEBUG_PRINTF(1, "CordbNativeFrame - CanSetIP - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetChain(ICorDebugChain **ppChain) {
  DEBUG_PRINTF(1, "CordbNativeFrame - GetChain - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetCode(ICorDebugCode **ppCode) {
  CordbCode *code = new CordbCode(conn, NULL);
  *ppCode = static_cast<ICorDebugCode *>(code);
  DEBUG_PRINTF(1, "CordbJITILFrame - GetCodeEx - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbNativeFrame::GetFunction(ICorDebugFunction **ppFunction) {
  return m_JITILFrame->GetFunction(ppFunction);
}

HRESULT STDMETHODCALLTYPE
CordbNativeFrame::GetFunctionToken(mdMethodDef *pToken) {
  DEBUG_PRINTF(1, "CordbNativeFrame - GetFunctionToken - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetStackRange(CORDB_ADDRESS *pStart,
                                                          CORDB_ADDRESS *pEnd) {
  return m_JITILFrame->GetStackRange(pStart, pEnd);
}

HRESULT STDMETHODCALLTYPE
CordbNativeFrame::GetCaller(ICorDebugFrame **ppFrame) {
  DEBUG_PRINTF(1, "CordbNativeFrame - GetCaller - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbNativeFrame::GetCallee(ICorDebugFrame **ppFrame) {
  DEBUG_PRINTF(1, "CordbNativeFrame - GetCallee - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbNativeFrame::CreateStepper(ICorDebugStepper **ppStepper) {
  DEBUG_PRINTF(1, "CordbNativeFrame - CreateStepper - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::QueryInterface(REFIID id,
                                                           void **pInterface) {
  if (id == IID_ICorDebugFrame) {
    *pInterface = static_cast<ICorDebugFrame *>(
        static_cast<ICorDebugNativeFrame *>(this));
  } else if (id == IID_ICorDebugNativeFrame) {
    *pInterface = static_cast<ICorDebugNativeFrame *>(this);
  } else if (id == IID_ICorDebugNativeFrame2) {
    *pInterface = static_cast<ICorDebugNativeFrame2 *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugNativeFrame *>(this));
  } else {
    // might be searching for an IL Frame. delegate that search to the
    // JITILFrame
    if (m_JITILFrame != NULL) {
      return m_JITILFrame->QueryInterface(id, pInterface);
    } else {
      *pInterface = NULL;
      return E_NOINTERFACE;
    }
  }

  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbNativeFrame::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbNativeFrame::Release(void) { return 0; }

HRESULT STDMETHODCALLTYPE CordbNativeFrame::IsChild(BOOL *pIsChild) {
  DEBUG_PRINTF(1, "CordbNativeFrame - IsChild - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::IsMatchingParentFrame(
    ICorDebugNativeFrame2 *pPotentialParentFrame, BOOL *pIsParent) {
  DEBUG_PRINTF(1,
               "CordbNativeFrame - IsMatchingParentFrame - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbNativeFrame::GetStackParameterSize(ULONG32 *pSize) {
  DEBUG_PRINTF(1,
               "CordbNativeFrame - GetStackParameterSize - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}
