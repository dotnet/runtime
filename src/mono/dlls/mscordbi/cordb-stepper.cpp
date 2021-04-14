// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-STEPPER.CPP
//

#include <cordb-frame.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb-process.h>
#include <cordb.h>

using namespace std;

CordbStepper::CordbStepper(Connection* conn, CordbThread* thread) : CordbBaseMono(conn)
{
    m_pThread   = thread;
    m_debuggerId = -1;
    conn->GetProcess()->AddStepper(this);
    m_bIsActive = false;
}

CordbStepper::~CordbStepper() {}

HRESULT STDMETHODCALLTYPE CordbStepper::IsActive(BOOL* pbActive)
{
    *pbActive = m_bIsActive;
    LOG((LF_CORDB, LL_INFO100000, "CordbStepper - IsActive - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::Deactivate(void)
{
    if (!m_bIsActive)
        return S_OK;
    m_bIsActive = false;
    LOG((LF_CORDB, LL_INFO1000000, "CordbStepper - Deactivate - IMPLEMENTED\n"));
    MdbgProtBuffer sendbuf;
    int            buflen = 128;
    m_dbgprot_buffer_init(&sendbuf, buflen);
    m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_EVENT_KIND_STEP);
    m_dbgprot_buffer_add_int(&sendbuf, m_debuggerId);
    conn->SendEvent(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_CLEAR, &sendbuf);
    m_dbgprot_buffer_free(&sendbuf);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetInterceptMask(CorDebugIntercept mask)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbStepper - SetInterceptMask - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetUnmappedStopMask(CorDebugUnmappedStop mask)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbStepper - SetUnmappedStopMask - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::Step(BOOL bStepIn)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbStepper - Step - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbStepper::StepRange(BOOL bStepIn, COR_DEBUG_STEP_RANGE ranges[], ULONG32 cRangeCount)
{
    m_bIsActive = true;
    LOG((LF_CORDB, LL_INFO1000000, "CordbStepper - StepRange - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY 
    {    
        MdbgProtBuffer sendbuf;
        int            buflen = 128;
        m_dbgprot_buffer_init(&sendbuf, buflen);
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_EVENT_KIND_STEP);
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_SUSPEND_POLICY_ALL);
        m_dbgprot_buffer_add_byte(&sendbuf, 1); // modifiers
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_MOD_KIND_STEP);

        m_dbgprot_buffer_add_id(&sendbuf, m_pThread->GetThreadId());
        m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_SIZE_MIN);
        m_dbgprot_buffer_add_int(&sendbuf, bStepIn ? MDBGPROT_STEP_DEPTH_INTO : MDBGPROT_STEP_DEPTH_OVER);
        m_dbgprot_buffer_add_int(&sendbuf, conn->GetProcess()->GetJMCStatus() ? MDBGPROT_STEP_FILTER_DEBUGGER_NON_USER_CODE : MDBGPROT_STEP_FILTER_NONE);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
        m_dbgprot_buffer_free(&sendbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        m_debuggerId = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbStepper::StepOut(void)
{
    m_bIsActive = true;
    LOG((LF_CORDB, LL_INFO1000000, "CordbStepper - StepOut - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY 
    {    
        MdbgProtBuffer sendbuf;
        int            buflen = 128;
        m_dbgprot_buffer_init(&sendbuf, buflen);
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_EVENT_KIND_STEP);
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_SUSPEND_POLICY_ALL);
        m_dbgprot_buffer_add_byte(&sendbuf, 1); // modifiers
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_MOD_KIND_STEP);

        m_dbgprot_buffer_add_id(&sendbuf, m_pThread->GetThreadId());
        m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_SIZE_MIN);
        m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_DEPTH_OUT);
        m_dbgprot_buffer_add_int(&sendbuf, MDBGPROT_STEP_FILTER_NONE);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
        m_dbgprot_buffer_free(&sendbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetRangeIL(BOOL bIL)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbStepper - SetRangeIL - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugStepper)
        *pInterface = static_cast<ICorDebugStepper*>(this);
    else if (id == IID_ICorDebugStepper2)
        *pInterface = static_cast<ICorDebugStepper2*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugStepper*>(this));
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbStepper::SetJMC(BOOL fIsJMCStepper)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbStepper - SetJMC - NOT IMPLEMENTED\n"));
    return S_OK;
}
