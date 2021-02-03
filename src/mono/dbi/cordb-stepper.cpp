// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-STEPPER.CPP
//

#include <cordb-frame.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb.h>

using namespace std;

CordbStepper::CordbStepper(Connection *conn, CordbThread *thread)
    : CordbBaseMono(conn) {
  this->thread = thread;
  hasStepped = false;
  isComplete = false;
  eventId = -1;
}

HRESULT STDMETHODCALLTYPE CordbStepper::IsActive(BOOL *pbActive) {
  LOG((LF_CORDB, LL_INFO100000, "CordbStepper - IsActive - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::Deactivate(void) {
  LOG((LF_CORDB, LL_INFO1000000, "CordbStepper - Deactivate - IMPLEMENTED\n"));
  MdbgProtBuffer sendbuf;
  int buflen = 128;
  m_dbgprot_buffer_init(&sendbuf, buflen);
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_EVENT_KIND_STEP);
  m_dbgprot_buffer_add_int(&sendbuf, eventId);
  conn->send_event(MDBGPROT_CMD_SET_EVENT_REQUEST,
                   MDBGPROT_CMD_EVENT_REQUEST_CLEAR, &sendbuf);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbStepper::SetInterceptMask(CorDebugIntercept mask) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbStepper - SetInterceptMask - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbStepper::SetUnmappedStopMask(CorDebugUnmappedStop mask) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbStepper - SetUnmappedStopMask - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::Step(BOOL bStepIn) {
  LOG((LF_CORDB, LL_INFO100000, "CordbStepper - Step - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::StepRange(BOOL bStepIn,
                                                  COR_DEBUG_STEP_RANGE ranges[],
                                                  ULONG32 cRangeCount) {
  isComplete = false;
  hasStepped = true;
  MdbgProtBuffer sendbuf;
  int buflen = 128;
  m_dbgprot_buffer_init(&sendbuf, buflen);
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_EVENT_KIND_STEP);
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_SUSPEND_POLICY_ALL);
  m_dbgprot_buffer_add_byte(&sendbuf, 1); // modifiers
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_MOD_KIND_STEP);

  m_dbgprot_buffer_add_id(&sendbuf, thread->thread_id);
  m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_SIZE_MIN);
  m_dbgprot_buffer_add_int(&sendbuf, bStepIn ? MDBGPROT_STEP_DEPTH_INTO
                                             : MDBGPROT_STEP_DEPTH_OVER);
  m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_FILTER_NONE);

  int cmdId = conn->send_event(MDBGPROT_CMD_SET_EVENT_REQUEST,
                               MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
  m_dbgprot_buffer_free(&sendbuf);
  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  eventId = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);

  LOG((LF_CORDB, LL_INFO1000000, "CordbStepper - StepRange - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::StepOut(void) {
  isComplete = false;
  hasStepped = true;
  MdbgProtBuffer sendbuf;
  int buflen = 128;
  m_dbgprot_buffer_init(&sendbuf, buflen);
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_EVENT_KIND_STEP);
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_SUSPEND_POLICY_ALL);
  m_dbgprot_buffer_add_byte(&sendbuf, 1); // modifiers
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_MOD_KIND_STEP);

  m_dbgprot_buffer_add_id(&sendbuf, thread->thread_id);
  m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_SIZE_MIN);
  m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_DEPTH_OUT);
  m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_FILTER_NONE);

  int cmdId = conn->send_event(MDBGPROT_CMD_SET_EVENT_REQUEST,
                               MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
  m_dbgprot_buffer_free(&sendbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);

  LOG((LF_CORDB, LL_INFO1000000, "CordbStepper - StepOut - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetRangeIL(BOOL bIL) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbStepper - SetRangeIL - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::QueryInterface(REFIID riid,
                                                       void **ppvObject) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbStepper - QueryInterface - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbStepper::AddRef(void) {
  LOG((LF_CORDB, LL_INFO100000, "CordbStepper - AddRef - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbStepper::Release(void) {
  LOG((LF_CORDB, LL_INFO100000, "CordbStepper - Release - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetJMC(BOOL fIsJMCStepper) {
  LOG((LF_CORDB, LL_INFO100000, "CordbStepper - SetJMC - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}
