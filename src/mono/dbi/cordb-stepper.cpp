// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-STEPPER.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-frame.h>
#include <cordb-process.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb.h>

using namespace std;

CordbStepper::CordbStepper(Connection *conn, CordbThread *thread)
    : CordbBaseMono(conn) {
  this->thread = thread;
  hasStepped = false;
  isComplete = false;
}

HRESULT STDMETHODCALLTYPE CordbStepper::IsActive(BOOL *pbActive) {
  DEBUG_PRINTF(1, "CordbStepper - IsActive - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::Deactivate(void) {
  DEBUG_PRINTF(1, "CordbStepper - Deactivate - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbStepper::SetInterceptMask(CorDebugIntercept mask) {
  DEBUG_PRINTF(1, "CordbStepper - SetInterceptMask - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbStepper::SetUnmappedStopMask(CorDebugUnmappedStop mask) {
  DEBUG_PRINTF(1, "CordbStepper - SetUnmappedStopMask - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::Step(BOOL bStepIn) {
  DEBUG_PRINTF(1, "CordbStepper - Step - NOT IMPLEMENTED\n");
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
  m_dbgprot_buffer_add_int(&sendbuf, bStepIn ? MDBGPROT_STEP_DEPTH_INTO : MDBGPROT_STEP_DEPTH_OVER);
  m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_FILTER_NONE);

  conn->send_event(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
  m_dbgprot_buffer_free(&sendbuf);

  DEBUG_PRINTF(1, "CordbStepper - StepRange - IMPLEMENTED\n");
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

  conn->send_event(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
  m_dbgprot_buffer_free(&sendbuf);

  DEBUG_PRINTF(1, "CordbStepper - StepOut - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetRangeIL(BOOL bIL) {
  DEBUG_PRINTF(1, "CordbStepper - SetRangeIL - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::QueryInterface(REFIID riid,
                                                       void **ppvObject) {
  DEBUG_PRINTF(1, "CordbStepper - QueryInterface - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbStepper::AddRef(void) {
  DEBUG_PRINTF(1, "CordbStepper - AddRef - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbStepper::Release(void) {
  DEBUG_PRINTF(1, "CordbStepper - Release - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetJMC(BOOL fIsJMCStepper) {
  DEBUG_PRINTF(1, "CordbStepper - SetJMC - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}
