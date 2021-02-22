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
}

CordbFunctionBreakpoint::~CordbFunctionBreakpoint() {}

HRESULT __stdcall CordbFunctionBreakpoint::GetFunction(ICorDebugFunction** ppFunction)
{
    GetCode()->GetFunction()->QueryInterface(IID_ICorDebugFunction, (void**)ppFunction);
    LOG((LF_CORDB, LL_INFO1000000, "CordbFunctionBreakpoint - GetFunction - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT __stdcall CordbFunctionBreakpoint::GetOffset(ULONG32* pnOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunctionBreakpoint - GetOffset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT __stdcall CordbFunctionBreakpoint::Activate(BOOL bActive)
{
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
        conn->SendEvent(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
        m_dbgprot_buffer_free(&sendbuf);
        LOG((LF_CORDB, LL_INFO1000000, "CordbFunctionBreakpoint - Activate - IMPLEMENTED\n"));
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "CordbFunctionBreakpoint - Activate - FALSE - NOT IMPLEMENTED\n"));
    }
    return S_OK;
}

HRESULT __stdcall CordbFunctionBreakpoint::IsActive(BOOL* pbActive)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunctionBreakpoint - IsActive - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT __stdcall CordbFunctionBreakpoint::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugFunctionBreakpoint)
    {
        *pInterface = static_cast<ICorDebugFunctionBreakpoint*>(this);
    }
    else
    {
        // Not looking for a function breakpoint? See if the base class handles
        // this interface. (issue 143976)
        // return CordbBreakpoint::QueryInterface(id, pInterface);
    }
    AddRef();
    return S_OK;
}
