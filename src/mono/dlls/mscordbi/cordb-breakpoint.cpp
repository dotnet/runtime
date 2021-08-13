// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BREAKPOINT.CPP
//

#include <cordb-breakpoint.h>
#include <cordb-code.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb.h>

using namespace std;

CordbFunctionBreakpoint::CordbFunctionBreakpoint(Connection* conn, CordbCode* code, ULONG32 offset)
    : CordbBaseMono(conn)
{
    this->m_pCode  = code;
    this->m_offset = offset;
    conn->GetProcess()->AddBreakpoint(this);
    m_debuggerId = -1;
    m_bActive = false;
}

CordbFunctionBreakpoint::~CordbFunctionBreakpoint() {}

HRESULT CordbFunctionBreakpoint::GetFunction(ICorDebugFunction** ppFunction)
{
    GetCode()->GetFunction()->QueryInterface(IID_ICorDebugFunction, (void**)ppFunction);
    LOG((LF_CORDB, LL_INFO1000000, "CordbFunctionBreakpoint - GetFunction - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbFunctionBreakpoint::GetOffset(ULONG32* pnOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunctionBreakpoint - GetOffset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbFunctionBreakpoint::Activate(BOOL bActive)
{
    m_bActive = bActive;
    if (bActive)
    {
        MdbgProtBuffer sendbuf;
        int            buflen = 128;
        m_dbgprot_buffer_init(&sendbuf, buflen);
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_EVENT_KIND_BREAKPOINT);
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_SUSPEND_POLICY_ALL);
        m_dbgprot_buffer_add_byte(&sendbuf, 1); // modifiers
        m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_MOD_KIND_LOCATION_ONLY);
        m_dbgprot_buffer_add_id(&sendbuf, this->GetCode()->GetFunction()->GetDebuggerId());
        m_dbgprot_buffer_add_long(&sendbuf, m_offset);
        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
        m_dbgprot_buffer_free(&sendbuf);
        LOG((LF_CORDB, LL_INFO1000000, "CordbFunctionBreakpoint - Activate - IMPLEMENTED\n"));

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        m_debuggerId = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "CordbFunctionBreakpoint - Activate - FALSE - NOT IMPLEMENTED\n"));
    }
    return S_OK;
}

HRESULT CordbFunctionBreakpoint::IsActive(BOOL* pbActive)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunctionBreakpoint - IsActive - IMPLEMENTED\n"));
    *pbActive = m_bActive;
    return S_OK;
}

HRESULT CordbFunctionBreakpoint::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugFunctionBreakpoint)
    {
        *pInterface = static_cast<ICorDebugFunctionBreakpoint*>(this);
    }
    else if (id == IID_ICorDebugBreakpoint)
    {
        *pInterface = static_cast<ICorDebugBreakpoint*>(this);
    }
    else if (id == IID_IUnknown)
        *pInterface = static_cast<ICorDebugFunctionBreakpoint*>(this);
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}
