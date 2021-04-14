// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-FUNCTION.CPP
//

#include <cordb-assembly.h>
#include <cordb-code.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-breakpoint.h>
#include <cordb-class.h>
#include <cordb.h>

using namespace std;

CordbFunction::CordbFunction(Connection* conn, mdToken token, int id, CordbModule* module) : CordbBaseMono(conn)
{
    this->m_metadataToken = token;
    this->m_debuggerId    = id;
    m_pCode               = NULL;
    this->m_pModule       = module;
    if (module)
        module->InternalAddRef();
    conn->GetProcess()->AddFunction(this);
}

CordbFunction::~CordbFunction()
{
    if (m_pCode)
        m_pCode->InternalRelease();
    if (m_pModule)
        m_pModule->InternalRelease();
}

HRESULT CordbFunction::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugFunction)
    {
        *pInterface = static_cast<ICorDebugFunction*>(this);
    }
    else if (id == IID_ICorDebugFunction2)
    {
        *pInterface = static_cast<ICorDebugFunction2*>(this);
    }
    else if (id == IID_ICorDebugFunction3)
    {
        *pInterface = static_cast<ICorDebugFunction3*>(this);
    }
    else if (id == IID_ICorDebugFunction4)
    {
        *pInterface = static_cast<ICorDebugFunction4*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugFunction*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT CordbFunction::GetModule(ICorDebugModule** ppModule)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - GetModule - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY 
    {    
        MdbgProtBuffer localbuf;
        if (!m_pModule)
        {
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);
            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_ASSEMBLY, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            int module_id = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            m_pModule     = conn->GetProcess()->GetModule(module_id);
            if (m_pModule)
                m_pModule->InternalAddRef();
        }

        if (!m_pModule)
            hr = S_FALSE;
        else {
            m_pModule->AddRef();
            *ppModule = static_cast<ICorDebugModule*>(m_pModule);
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbFunction::GetClass(ICorDebugClass** ppClass)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);
        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_GET_CLASS_TOKEN, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        int m_type = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        CordbClass* m_pClass = conn->GetProcess()->FindOrAddClass(m_type, m_pModule->GetDebuggerId());
        hr = m_pClass->QueryInterface(IID_ICorDebugClass, (void**)ppClass);
    }
    EX_CATCH_HRESULT(hr);
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - GetClass - IMPLEMENTED\n"));
    return hr;
}

HRESULT CordbFunction::GetToken(mdMethodDef* pMethodDef)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - GetToken - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY 
    {     
        if (this->GetMetadataToken() == 0)
        {
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);
            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_TOKEN, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            this->m_metadataToken = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        }
        *pMethodDef = this->GetMetadataToken();        
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbFunction::GetILCode(ICorDebugCode** ppCode)
{
    if (m_pCode == NULL)
    {
        m_pCode = new CordbCode(conn, this);
        m_pCode->InternalAddRef();
    }
    m_pCode->QueryInterface(IID_ICorDebugCode, (void**)ppCode);
    LOG((LF_CORDB, LL_INFO1000000, "CordbFunction - GetILCode - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbFunction::GetNativeCode(ICorDebugCode** ppCode)
{
    if (m_pCode == NULL)
    {
        m_pCode = new CordbCode(conn, this);
        m_pCode->InternalAddRef();
    }
    m_pCode->QueryInterface(IID_ICorDebugCode, (void**)ppCode);
    LOG((LF_CORDB, LL_INFO1000000, "CordbFunction - GetNativeCode - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbFunction::CreateBreakpoint(ICorDebugFunctionBreakpoint** ppBreakpoint)
{
    if (m_pCode == NULL)
    {
        m_pCode = new CordbCode(conn, this);
        m_pCode->InternalAddRef();
    }
    CordbFunctionBreakpoint* bp = new CordbFunctionBreakpoint(conn, m_pCode, 0);
    bp->QueryInterface(IID_ICorDebugFunctionBreakpoint, (void**)ppBreakpoint);
    LOG((LF_CORDB, LL_INFO1000000, "CordbFunction - CreateBreakpoint - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbFunction::GetLocalVarSigToken(mdSignature* pmdSig)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - GetLocalVarSigToken - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbFunction::GetCurrentVersionNumber(ULONG32* pnCurrentVersion)
{
    *pnCurrentVersion = 1;
    LOG((LF_CORDB, LL_INFO1000000, "CordbFunction - GetCurrentVersionNumber - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbFunction::SetJMCStatus(BOOL bIsJustMyCode)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - SetJMCStatus - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbFunction::GetJMCStatus(BOOL* pbIsJustMyCode)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - GetJMCStatus - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbFunction::EnumerateNativeCode(ICorDebugCodeEnum** ppCodeEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - EnumerateNativeCode - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbFunction::GetVersionNumber(ULONG32* pnVersion)
{
    *pnVersion = 1;
    LOG((LF_CORDB, LL_INFO1000000, "CordbFunction - GetVersionNumber - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbFunction::GetActiveReJitRequestILCode(ICorDebugILCode** ppReJitedILCode)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - GetActiveReJitRequestILCode - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbFunction::CreateNativeBreakpoint(ICorDebugFunctionBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFunction - CreateNativeBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}
