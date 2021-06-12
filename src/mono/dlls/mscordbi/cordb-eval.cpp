// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-EVAL.CPP
//

#include <cordb-appdomain.h>
#include <cordb-eval.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-thread.h>
#include <cordb-value.h>
#include <cordb.h>

#include "corerror.h"
#include "metamodel.h"
#include "metamodelpub.h"
#include "rwutil.h"
#include "stdafx.h"

CordbEval::CordbEval(Connection* conn, CordbThread* thread) : CordbBaseMono(conn)
{
    this->m_pThread = thread;
    if (thread)
        thread->InternalAddRef();
    m_pValue    = NULL;
    m_commandId = -1;
}

CordbEval::~CordbEval()
{
    if (m_pThread)
        m_pThread->InternalRelease();
}
HRESULT STDMETHODCALLTYPE CordbEval::CallParameterizedFunction(ICorDebugFunction* pFunction,
                                                               ULONG32            nTypeArgs,
                                                               ICorDebugType*     ppTypeArgs[],
                                                               ULONG32            nArgs,
                                                               ICorDebugValue*    ppArgs[])
{
    conn->GetProcess()->Stop(false);
    LOG((LF_CORDB, LL_INFO1000000, "CordbEval - CallParameterizedFunction - IMPLEMENTED\n"));

    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, m_pThread->GetThreadId());
    m_dbgprot_buffer_add_int(&localbuf, 1);
    m_dbgprot_buffer_add_int(&localbuf, ((CordbFunction*)pFunction)->GetDebuggerId());
    m_dbgprot_buffer_add_int(&localbuf, nArgs);
    for (ULONG32 i = 0; i < nArgs; i++)
    {
        CorElementType ty;
        ppArgs[i]->GetType(&ty);
        CordbContent* cc;
        cc = ((CordbValue*)ppArgs[i])->GetValue();
        m_dbgprot_buffer_add_byte(&localbuf, ty);
        switch (ty)
        {
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
                m_dbgprot_buffer_add_int(&localbuf, cc->booleanValue);
                break;
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
                m_dbgprot_buffer_add_int(&localbuf, cc->charValue);
                break;
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_R4:
                m_dbgprot_buffer_add_int(&localbuf, cc->intValue);
                break;
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R8:
                m_dbgprot_buffer_add_long(&localbuf, cc->longValue);
                break;
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_STRING:
                m_dbgprot_buffer_add_id(&localbuf, cc->intValue);
                break;
            default:
                return E_NOTIMPL;
        }
    }
    m_commandId = conn->SendEvent(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_INVOKE_METHOD, &localbuf);
    m_dbgprot_buffer_free(&localbuf);
    conn->GetProcess()->AddPendingEval(this);
    return S_OK;
}

void CordbEval::EvalComplete(MdbgProtBuffer* pReply)
{
    m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);
    CordbObjectValue::CreateCordbValue(conn, pReply, &m_pValue);

    conn->GetCordb()->GetCallback()->EvalComplete(conn->GetProcess()->GetCurrentAppDomain(), m_pThread, this);
}

HRESULT STDMETHODCALLTYPE CordbEval::CreateValueForType(ICorDebugType* pType, ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - CreateValueForType - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewParameterizedObject(ICorDebugFunction* pConstructor,
                                                            ULONG32            nTypeArgs,
                                                            ICorDebugType*     ppTypeArgs[],
                                                            ULONG32            nArgs,
                                                            ICorDebugValue*    ppArgs[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewParameterizedObject - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewParameterizedObjectNoConstructor(ICorDebugClass* pClass,
                                                                         ULONG32         nTypeArgs,
                                                                         ICorDebugType*  ppTypeArgs[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewParameterizedObjectNoConstructor - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewParameterizedArray(ICorDebugType* pElementType,
                                                           ULONG32        rank,
                                                           ULONG32        dims[],
                                                           ULONG32        lowBounds[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewParameterizedArray - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewStringWithLength(LPCWSTR string, UINT uiLength)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        conn->GetProcess()->Stop(false);
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_pThread->GetThreadId());
        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_THREAD, MDBGPROT_CMD_THREAD_GET_APPDOMAIN, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        int domainId = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);

        LPSTR szString;
        UTF8STR(string, szString);

        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, domainId);
        m_dbgprot_buffer_add_string(&localbuf, szString);
        this->m_commandId = conn->SendEvent(MDBGPROT_CMD_SET_APPDOMAIN, MDBGPROT_CMD_APPDOMAIN_CREATE_STRING, &localbuf);
        m_dbgprot_buffer_free(&localbuf);
        conn->GetProcess()->AddPendingEval(this);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbEval::RudeAbort(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - RudeAbort - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT
CordbEval::QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface)
{
    if (id == IID_ICorDebugEval)
    {
        *pInterface = static_cast<ICorDebugEval*>(this);
    }
    else if (id == IID_ICorDebugEval2)
    {
        *pInterface = static_cast<ICorDebugEval2*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugEval*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbEval::CallFunction(ICorDebugFunction* pFunction, ULONG32 nArgs, ICorDebugValue* ppArgs[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - CallFunction - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewObject(ICorDebugFunction* pConstructor, ULONG32 nArgs, ICorDebugValue* ppArgs[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewObject - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewObjectNoConstructor(ICorDebugClass* pClass)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewObjectNoConstructor - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewString(LPCWSTR string)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewString - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::NewArray(
    CorElementType elementType, ICorDebugClass* pElementClass, ULONG32 rank, ULONG32 dims[], ULONG32 lowBounds[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - NewArray - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::IsActive(BOOL* pbActive)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - IsActive - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::Abort(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - Abort - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbEval::GetResult(ICorDebugValue** ppResult)
{
    *ppResult = m_pValue;
    LOG((LF_CORDB, LL_INFO1000000, "CordbEval - GetResult - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbEval::GetThread(ICorDebugThread** ppThread)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbEval - GetThread - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbEval::CreateValue(CorElementType   elementType,
                                                 ICorDebugClass*  pElementClass,
                                                 ICorDebugValue** ppValue)
{
    CordbContent content_value;
    content_value.booleanValue = 0;
    CordbValue* value = new CordbValue(conn, elementType, content_value, CordbObjectValue::GetTypeSize(elementType));
    LOG((LF_CORDB, LL_INFO1000000, "CordbEval - CreateValue - IMPLEMENTED\n"));
    value->QueryInterface(IID_ICorDebugValue, (void**)ppValue);
    return S_OK;
}
