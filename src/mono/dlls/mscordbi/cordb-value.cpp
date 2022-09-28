// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-VALUE.CPP
//

#include <cordb-breakpoint.h>
#include <cordb-class.h>
#include <cordb-type.h>
#include <cordb-value.h>
#include <cordb-process.h>
#include <cordb.h>

using namespace std;

CordbValue::CordbValue(Connection* conn, CorElementType type, CordbContent value, int size) : CordbBaseMono(conn)
{
    this->m_type  = type;
    this->m_value = value;
    this->m_size  = size;
    this->conn    = conn;
    m_pType       = NULL;
}

CordbValue::~CordbValue()
{
    if (m_pType)
        m_pType->InternalRelease();
}

HRESULT STDMETHODCALLTYPE CordbValue::GetType(CorElementType* pType)
{
    *pType = m_type;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetSize(ULONG32* pSize)
{
    *pSize = m_size;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetAddress(CORDB_ADDRESS* pAddress)
{
    *pAddress = (CORDB_ADDRESS)m_value.pointerValue;
    LOG((LF_CORDB, LL_INFO1000000, "CordbValue - GetAddress - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbValue - CreateBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValue::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugGenericValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugGenericValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetExactType(ICorDebugType** ppType)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbValue - GetExactType - IMPLEMENTED\n"));
    if (m_pType == NULL)
    {
        m_pType = conn->GetProcess()->FindOrAddPrimitiveType(m_type);
        m_pType->InternalAddRef();
    }
    m_pType->QueryInterface(IID_ICorDebugType, (void**)ppType);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetSize64(ULONG64* pSize)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbValue - GetSize64 - IMPLEMENTED\n"));
    *pSize = m_size;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetValue(void* pTo)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbValue - GetValue - IMPLEMENTED\n"));
    SIZE_T nSizeRead;
    conn->GetProcess()->ReadMemory(m_value.pointerValue, m_size, (BYTE*)pTo, &nSizeRead);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::SetValue(void* pFrom)
{
    memcpy(&m_value, pFrom, m_size);
    LOG((LF_CORDB, LL_INFO1000000, "CordbValue - SetValue - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetType(CorElementType* pType)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbReferenceValue - GetType - IMPLEMENTED\n"));
    *pType = m_type;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetSize(ULONG32* pSize)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbReferenceValue - GetSize - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetAddress(CORDB_ADDRESS* pAddress)
{
    *pAddress = (CORDB_ADDRESS)m_pAddress;
    LOG((LF_CORDB, LL_INFO1000000, "CordbReferenceValue - GetAddress - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbReferenceValue - CreateBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugReferenceValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugReferenceValue)
    {
        *pInterface = static_cast<ICorDebugReferenceValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugReferenceValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetExactType(ICorDebugType** ppType)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbReferenceValue - GetExactType - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (m_pCordbType)
        {
            m_pCordbType->QueryInterface(IID_ICorDebugType, (void**)ppType);
            goto __Exit;
        }
        if (m_pClass != NULL)
        {
            m_pCordbType = conn->GetProcess()->FindOrAddClassType(m_type, m_pClass);
            m_pCordbType->InternalAddRef();
            m_pCordbType->QueryInterface(IID_ICorDebugType, (void**)ppType);
            goto __Exit;
        }
        if (m_type == ELEMENT_TYPE_CLASS && m_debuggerId != -1)
        {
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);

            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_OBJECT_REF, MDBGPROT_CMD_OBJECT_REF_GET_TYPE, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            int type_id = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);

            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, type_id);

            cmdId = conn->SendEvent(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            pReply = received_reply_packet->Buffer();

            char* namespace_str      = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
            char* class_name_str     = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
            char* class_fullname_str = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
            int   assembly_id        = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            int   module_id          = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            type_id                  = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            int type_id2             = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            int token                = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
            m_pClass                 = conn->GetProcess()->FindOrAddClass(token, module_id);
            m_pClass->InternalAddRef();
            m_pCordbType = conn->GetProcess()->FindOrAddClassType(m_type, m_pClass);
            m_pCordbType->InternalAddRef();
            m_pCordbType->QueryInterface(IID_ICorDebugType, (void**)ppType);
            free(namespace_str);
            free(class_name_str);
            free(class_fullname_str);
            goto __Exit;
        }
        if (m_type == ELEMENT_TYPE_SZARRAY && m_debuggerId != -1)
        {
            m_pClass = NULL;
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);

            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_TYPE, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            int type_id = m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);
            int rank    = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
            if (type_id == ELEMENT_TYPE_CLASS)
            {
                int klass_id = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);

                m_dbgprot_buffer_init(&localbuf, 128);
                m_dbgprot_buffer_add_id(&localbuf, klass_id);

                cmdId = conn->SendEvent(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
                m_dbgprot_buffer_free(&localbuf);

                received_reply_packet = conn->GetReplyWithError(cmdId);
                CHECK_ERROR_RETURN_FALSE(received_reply_packet);
                pReply = received_reply_packet->Buffer();

                char* namespace_str      = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                char* class_name_str     = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                char* class_fullname_str = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                int   assembly_id        = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int   module_id          = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int   type_id3           = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int   type_id2           = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int   token              = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
                m_pClass                 = conn->GetProcess()->FindOrAddClass(token, module_id);
                m_pClass->InternalAddRef();
                free(namespace_str);
                free(class_name_str);
                free(class_fullname_str);
            }

            m_pCordbType = conn->GetProcess()->FindOrAddArrayType(m_type, conn->GetProcess()->FindOrAddClassType((CorElementType)type_id, m_pClass));
            m_pCordbType->InternalAddRef();
            m_pCordbType->QueryInterface(IID_ICorDebugType, (void**)ppType);
            goto __Exit;
        }
        m_pCordbType = conn->GetProcess()->FindOrAddPrimitiveType(m_type);
        m_pCordbType->InternalAddRef();
        m_pCordbType->QueryInterface(IID_ICorDebugType, (void**)ppType);
    }
    EX_CATCH_HRESULT(hr);
__Exit:
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetSize64(ULONG64* pSize)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbReferenceValue - GetSize64 - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetValue(void* pTo)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbReferenceValue - GetValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::SetValue(void* pFrom)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbReferenceValue - SetValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::IsNull(BOOL* pbNull)
{
    if (m_debuggerId == -1)
        *pbNull = true;
    LOG((LF_CORDB, LL_INFO1000000, "CordbReferenceValue - IsNull - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetValue(CORDB_ADDRESS* pValue)
{
    if (m_debuggerId == -1)
        *pValue = NULL;
    else
        *pValue = (CORDB_ADDRESS)&m_debuggerId;
    LOG((LF_CORDB, LL_INFO1000000, "CordbReferenceValue - GetValue - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::SetValue(CORDB_ADDRESS value)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbReferenceValue - SetValue CORDB_ADDRESS - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::Dereference(ICorDebugValue** ppValue)
{
    if (m_debuggerId == -1)
        return CORDBG_E_BAD_REFERENCE_VALUE;
    if (!m_pClass) {
        ICorDebugType* cordbType;
        GetExactType(&cordbType);
        cordbType->Release();
    }
    if (m_type == ELEMENT_TYPE_SZARRAY || m_type == ELEMENT_TYPE_ARRAY)
    {
        CordbArrayValue* objectValue = new CordbArrayValue(conn, m_pCordbType, m_debuggerId, m_pClass);
        objectValue->QueryInterface(IID_ICorDebugValue, (void**)ppValue);
    }
    else
    {
        CordbObjectValue* objectValue = new CordbObjectValue(conn, m_type, m_debuggerId, m_pClass);
        objectValue->QueryInterface(IID_ICorDebugValue, (void**)ppValue);
    }
    LOG((LF_CORDB, LL_INFO1000000, "CordbReferenceValue - Dereference - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::DereferenceStrong(ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbReferenceValue - DereferenceStrong - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

CordbReferenceValue::CordbReferenceValue(Connection* conn, CorElementType type, int object_id, CordbClass* klass, CordbType* cordbType, CORDB_ADDRESS cordbAddress)
    : CordbBaseMono(conn)
{
    this->m_type       = type;
    this->m_debuggerId = object_id;
    this->conn         = conn;
    this->m_pClass     = klass;
    this->m_pCordbType = cordbType;
    this->m_pAddress   = cordbAddress;
    if (cordbType)
        cordbType->InternalAddRef();
    if (klass)
        klass->InternalAddRef();
}

CordbReferenceValue::~CordbReferenceValue()
{
    if (m_pCordbType)
        m_pCordbType->InternalRelease();
    if (m_pClass)
        m_pClass->InternalRelease();
}

CordbObjectValue::CordbObjectValue(Connection* conn, CorElementType type, int object_id, CordbClass* klass)
    : CordbBaseMono(conn)
{
    this->m_type       = type;
    this->m_debuggerId = object_id;
    this->m_pClass     = klass;
    if (klass)
        klass->InternalAddRef();
    m_pCordbType = NULL;
}

CordbObjectValue::~CordbObjectValue()
{
    if (m_pClass)
        m_pClass->InternalRelease();
    if (m_pCordbType)
        m_pCordbType->InternalRelease();
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetType(CorElementType* pType)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbObjectValue - GetType - IMPLEMENTED\n"));
    *pType = m_type;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetSize(ULONG32* pSize)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetSize - NOT IMPLEMENTED\n"));
    *pSize = 10;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetAddress(CORDB_ADDRESS* pAddress)
{
    *pAddress = (CORDB_ADDRESS)&m_debuggerId;
    LOG((LF_CORDB, LL_INFO1000000, "CordbObjectValue - GetAddress - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - CreateBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetExactType(ICorDebugType** ppType)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbObjectValue - GetExactType - IMPLEMENTED\n"));
    if (m_pCordbType == NULL)
    {
        m_pCordbType = conn->GetProcess()->FindOrAddClassType(m_type, m_pClass);
        m_pCordbType->InternalAddRef();
    }
    m_pCordbType->QueryInterface(IID_ICorDebugType, (void**)ppType);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetSize64(ULONG64* pSize)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetSize64 - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetValue(void* pTo)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::SetValue(void* pFrom)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - SetValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetVirtualMethodAndType(mdMemberRef         memberRef,
                                                                    ICorDebugFunction** ppFunction,
                                                                    ICorDebugType**     ppType)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetVirtualMethodAndType - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetLength(ULONG32* pcchString)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (m_debuggerId == -1)
            hr = S_FALSE;
        else if (m_type == ELEMENT_TYPE_STRING)
        {
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);

            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_STRING_REF, MDBGPROT_CMD_STRING_REF_GET_LENGTH, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            *pcchString = (ULONG32)m_dbgprot_decode_long(pReply->p, &pReply->p, pReply->end);
        }
        else
            hr = E_NOTIMPL;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetString(ULONG32 cchString, ULONG32* pcchString, WCHAR szString[])
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (m_debuggerId == -1)
            hr = S_FALSE;
        else if (m_type == ELEMENT_TYPE_STRING)
        {
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);

            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_STRING_REF, MDBGPROT_CMD_STRING_REF_GET_VALUE, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            *pcchString   = cchString;
            int use_utf16 = m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);
            if (use_utf16)
            {
                LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetString - NOT IMPLEMENTED - use_utf16\n"));
            }
            else
            {
                char* value = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                LOG((LF_CORDB, LL_INFO1000000, "CordbObjectValue - GetString - IMPLEMENTED\n"));
                if (cchString >= strlen(value))
                {
                    MultiByteToWideChar(CP_UTF8, 0, value, -1, szString, cchString);
                    *pcchString = cchString;
                }
                free(value);
            }
            hr =  S_OK;
        }
        else
            hr = E_NOTIMPL;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::CreateHandle(CorDebugHandleType type, ICorDebugHandleValue** ppHandle)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - CreateHandle - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetThreadOwningMonitorLock(ICorDebugThread** ppThread,
                                                                       DWORD*            pAcquisitionCount)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetThreadOwningMonitorLock - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetMonitorEventWaitList(ICorDebugThreadEnum** ppThreadEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetMonitorEventWaitList - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - EnumerateExceptionCallStack - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetCachedInterfaceTypes(BOOL                bIInspectableOnly,
                                                                    ICorDebugTypeEnum** ppInterfacesEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetCachedInterfaceTypes - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetCachedInterfacePointers(BOOL           bIInspectableOnly,
                                                                       ULONG32        celt,
                                                                       ULONG32*       pcEltFetched,
                                                                       CORDB_ADDRESS* ptrs)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetCachedInterfacePointers - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetTarget(ICorDebugReferenceValue** ppObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetTarget - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetFunction(ICorDebugFunction** ppFunction)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetFunction - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetClass(ICorDebugClass** ppClass)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetClass - IMPLEMENTED\n"));
    if (m_pClass) {
        m_pClass->QueryInterface(IID_ICorDebugClass, (void**)ppClass);
        return S_OK;
    }
    return S_FALSE;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetFieldValue(ICorDebugClass*  pClass,
                                                          mdFieldDef       fieldDef,
                                                          ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbObjectValue - GetFieldValue - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (m_debuggerId == -1)
            hr = S_FALSE;
        else {
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);
            m_dbgprot_buffer_add_int(&localbuf, fieldDef);

            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_OBJECT_REF, MDBGPROT_CMD_OBJECT_REF_GET_VALUES_ICORDBG, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            hr = CreateCordbValue(conn, pReply, ppValue);
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

int CordbObjectValue::GetTypeSize(int type)
{
    switch (type)
    {
        case ELEMENT_TYPE_VOID:
            return 0;
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
            return 1;
            break;
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
            return 2;
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
            return 4;
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
            return 8;
    }
    return 0;
}

HRESULT CordbObjectValue::CreateCordbValue(Connection* conn, MdbgProtBuffer* pReply, ICorDebugValue** ppValue)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        CorElementType type = (CorElementType)m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);
        CordbContent   value;

        if ((MdbgProtValueTypeId)type == MDBGPROT_VALUE_TYPE_ID_NULL)
        {
            CorElementType type = (CorElementType)m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);
            if (type == ELEMENT_TYPE_CLASS || type == ELEMENT_TYPE_STRING)
            {
                int klass_id = (CorElementType)m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);

                MdbgProtBuffer localbuf;
                m_dbgprot_buffer_init(&localbuf, 128);
                m_dbgprot_buffer_add_id(&localbuf, klass_id);
                int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
                m_dbgprot_buffer_free(&localbuf);
                ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
                CHECK_ERROR_RETURN_FALSE(received_reply_packet);
                MdbgProtBuffer* pReply             = received_reply_packet->Buffer();
                char*           namespace_str      = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                char*           class_name_str     = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                char*           class_fullname_str = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                int             assembly_id        = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int             module_id          = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int             type_id            = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int             type_id2           = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                int             token              = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);

                CordbClass*          klass    =  conn->GetProcess()->FindOrAddClass(token, assembly_id);
                CordbReferenceValue* refValue = new CordbReferenceValue(conn, type, -1, klass);
                refValue->QueryInterface(IID_ICorDebugValue, (void**)ppValue);
                free(namespace_str);
                free(class_name_str);
                free(class_fullname_str);
            }
            if (type == ELEMENT_TYPE_SZARRAY)
            {
                CordbClass* klass   = NULL;
                int         type_id = m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);
                if (type_id == ELEMENT_TYPE_CLASS)
                {
                    int klass_id = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);

                    MdbgProtBuffer localbuf;
                    m_dbgprot_buffer_init(&localbuf, 128);
                    m_dbgprot_buffer_add_id(&localbuf, klass_id);
                    int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
                    m_dbgprot_buffer_free(&localbuf);
                    ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
                    CHECK_ERROR_RETURN_FALSE(received_reply_packet);
                    MdbgProtBuffer* pReply             = received_reply_packet->Buffer();
                    char*           namespace_str      = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                    char*           class_name_str     = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                    char*           class_fullname_str = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
                    int             assembly_id        = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                    int             module_id          = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                    int             type_id3           = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                    int             type_id2           = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                    int             token              = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
                    klass                              =  conn->GetProcess()->FindOrAddClass(token, module_id);
                    free(namespace_str);
                    free(class_name_str);
                    free(class_fullname_str);
                }
                CordbType* cordbtype = conn->GetProcess()->FindOrAddArrayType(type, conn->GetProcess()->FindOrAddClassType((CorElementType)type_id, klass));
                CordbReferenceValue* refValue = new CordbReferenceValue(conn, type, -1, klass, cordbtype);
                refValue->QueryInterface(IID_ICorDebugValue, (void**)ppValue);
            }
            goto __Exit;
        }

        switch (type)
        {
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R8:
                value.pointerValue = m_dbgprot_decode_long(pReply->p, &pReply->p, pReply->end);
                break;
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_STRING:
            {
                int object_id = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
                CORDB_ADDRESS address = m_dbgprot_decode_long(pReply->p, &pReply->p, pReply->end);
                CordbReferenceValue* refValue  = new CordbReferenceValue(conn, type, object_id, NULL, NULL, address);
                refValue->QueryInterface(IID_ICorDebugValue, (void**)ppValue);
                goto __Exit;
            }
            default:
                LOG((LF_CORDB, LL_INFO100000, "default value - %d", type));
                hr = E_FAIL;
                goto __Exit;
        }
        *ppValue = new CordbValue(conn, type, value, GetTypeSize(type));
        (*ppValue)->AddRef();
    }
    EX_CATCH_HRESULT(hr);
__Exit:
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetVirtualMethod(mdMemberRef memberRef, ICorDebugFunction** ppFunction)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetVirtualMethod - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetContext(ICorDebugContext** ppContext)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetContext - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::IsValueClass(BOOL* pbIsValueClass)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - IsValueClass - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetManagedCopy(IUnknown** ppObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - GetManagedCopy - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::SetFromManagedCopy(IUnknown* pObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - SetFromManagedCopy - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::IsValid(BOOL* pbValid)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - IsValid - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::CreateRelocBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbObjectValue - CreateRelocBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugObjectValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugObjectValue)
    {
        *pInterface = static_cast<ICorDebugObjectValue*>(this);
    }
    else if (id == IID_ICorDebugObjectValue2)
    {
        *pInterface = static_cast<ICorDebugObjectValue2*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue)
    {
        *pInterface = static_cast<ICorDebugHeapValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue2)
    {
        *pInterface = static_cast<ICorDebugHeapValue2*>(this);
    }
    else if (id == IID_ICorDebugHeapValue3)
    {
        *pInterface = static_cast<ICorDebugHeapValue3*>(this);
    }
    else if ((id == IID_ICorDebugStringValue) && (m_type == ELEMENT_TYPE_STRING))
    {
        *pInterface = static_cast<ICorDebugStringValue*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugObjectValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

CordbArrayValue::CordbArrayValue(Connection* conn, CordbType* type, int object_id, CordbClass* klass)
    : CordbBaseMono(conn)
{
    this->m_pCordbType = type;
    this->m_debuggerId = object_id;
    this->m_pClass     = klass;
    if (klass)
        klass->InternalAddRef();
    if (m_pCordbType)
        m_pCordbType->InternalAddRef();
}

CordbArrayValue::~CordbArrayValue()
{
    if (m_pClass)
        m_pClass->InternalRelease();
    if (m_pCordbType)
        m_pCordbType->InternalRelease();
}
HRESULT STDMETHODCALLTYPE CordbArrayValue::GetClass(ICorDebugClass** ppClass)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetClass - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetFieldValue(ICorDebugClass*  pClass,
                                                         mdFieldDef       fieldDef,
                                                         ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetFieldValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetVirtualMethod(mdMemberRef memberRef, ICorDebugFunction** ppFunction)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetVirtualMethod - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetContext(ICorDebugContext** ppContext)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetContext - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::IsValueClass(BOOL* pbIsValueClass)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - IsValueClass - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetManagedCopy(IUnknown** ppObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetManagedCopy - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::SetFromManagedCopy(IUnknown* pObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - SetFromManagedCopy - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetType(CorElementType* pType)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetType - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetSize(ULONG32* pSize)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetSize - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetAddress(CORDB_ADDRESS* pAddress)
{
    *pAddress = (CORDB_ADDRESS)&m_debuggerId;
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetAddress - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - CreateBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugValue)
    {
        *pInterface = static_cast<ICorDebugValue*>(static_cast<ICorDebugArrayValue*>(this));
    }
    else if (id == IID_ICorDebugValue2)
    {
        *pInterface = static_cast<ICorDebugValue2*>(this);
    }
    else if (id == IID_ICorDebugValue3)
    {
        *pInterface = static_cast<ICorDebugValue3*>(this);
    }
    else if (id == IID_ICorDebugArrayValue)
    {
        *pInterface = static_cast<ICorDebugArrayValue*>(this);
    }
    else if (id == IID_ICorDebugGenericValue)
    {
        *pInterface = static_cast<ICorDebugGenericValue*>(this);
    }
    else if (id == IID_ICorDebugHeapValue2)
    {
        *pInterface = static_cast<ICorDebugHeapValue2*>(this);
    }
    else if (id == IID_ICorDebugHeapValue3)
    {
        *pInterface = static_cast<ICorDebugHeapValue3*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugArrayValue*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetVirtualMethodAndType(mdMemberRef         memberRef,
                                                                   ICorDebugFunction** ppFunction,
                                                                   ICorDebugType**     ppType)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetVirtualMethodAndType - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetValue(void* pTo)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::SetValue(void* pFrom)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - SetValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetLength(ULONG32* pcchString)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetLength - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetString(ULONG32 cchString, ULONG32* pcchString, WCHAR szString[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetString - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::IsValid(BOOL* pbValid)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - IsValid - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::CreateRelocBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - CreateRelocBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetExactType(ICorDebugType** ppType)
{
    m_pCordbType->QueryInterface(IID_ICorDebugType, (void**)ppType);
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetExactType - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetSize64(ULONG64* pSize)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetSize64 - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::CreateHandle(CorDebugHandleType type, ICorDebugHandleValue** ppHandle)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - CreateHandle - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetThreadOwningMonitorLock(ICorDebugThread** ppThread,
                                                                      DWORD*            pAcquisitionCount)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetThreadOwningMonitorLock - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetMonitorEventWaitList(ICorDebugThreadEnum** ppThreadEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetMonitorEventWaitList - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - EnumerateExceptionCallStack - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetCachedInterfaceTypes(BOOL                bIInspectableOnly,
                                                                   ICorDebugTypeEnum** ppInterfacesEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetCachedInterfaceTypes - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetCachedInterfacePointers(BOOL           bIInspectableOnly,
                                                                      ULONG32        celt,
                                                                      ULONG32*       pcEltFetched,
                                                                      CORDB_ADDRESS* ptrs)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetCachedInterfacePointers - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetTarget(ICorDebugReferenceValue** ppObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetTarget - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetFunction(ICorDebugFunction** ppFunction)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetFunction - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetElementType(CorElementType* pType)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetElementType - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetRank(ULONG32* pnRank)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbArrayValue - GetRank - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_LENGTH, &localbuf);
        m_dbgprot_buffer_free(&localbuf);
        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();
        int             rank   = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        *pnRank = rank;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetCount(ULONG32* pnCount)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_LENGTH, &localbuf);
        m_dbgprot_buffer_free(&localbuf);
        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();
        int             rank   = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        m_nCount               = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        LOG((LF_CORDB, LL_INFO1000000, "CordbArrayValue - GetCount - IMPLEMENTED\n"));
        *pnCount = m_nCount;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetDimensions(ULONG32 cdim, ULONG32 dims[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetDimensions - IMPLEMENTED\n"));
    dims[0] = m_nCount;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::HasBaseIndices(BOOL* pbHasBaseIndices)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - HasBaseIndices - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetBaseIndices(ULONG32 cdim, ULONG32 indices[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetBaseIndices - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetElement(ULONG32 cdim, ULONG32 indices[], ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbArrayValue - GetElement - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);
        m_dbgprot_buffer_add_int(&localbuf, indices[cdim - 1]);
        m_dbgprot_buffer_add_int(&localbuf, 1);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_VALUES, &localbuf);
        m_dbgprot_buffer_free(&localbuf);
        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();
        hr = CordbObjectValue::CreateCordbValue(conn, pReply, ppValue);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetElementAtPosition(ULONG32 nPosition, ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbArrayValue - GetElementAtPosition - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}


CordbValueEnum::CordbValueEnum(Connection* conn, long nThreadDebuggerId, long nFrameDebuggerId, bool bIsArgument, ILCodeKind nFlags):CordbBaseMono(conn)
{
    m_nThreadDebuggerId = nThreadDebuggerId;
    m_nFrameDebuggerId = nFrameDebuggerId;
    m_nCurrentValuePos = 0;
    m_nCount = 0;
    m_nFlags = nFlags;
    m_bIsArgument = bIsArgument;
    m_pValues = NULL;
}

HRESULT STDMETHODCALLTYPE CordbValueEnum::Next(ULONG celt, ICorDebugValue* values[], ULONG* pceltFetched)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbValueEnum - Next - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValueEnum::Skip(ULONG celt)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbValueEnum - Skip - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValueEnum::Reset(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbValueEnum - Reset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValueEnum::Clone(ICorDebugEnum** ppEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbValueEnum - Clone - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValueEnum::GetCount(ULONG* pcelt)
{
    if (m_nFlags == ILCODE_REJIT_IL)
    {
        *pcelt = 0;
        return S_OK;
    }
    if (m_bIsArgument) {
        LOG((LF_CORDB, LL_INFO1000000, "CordbFrame - GetArgument - IMPLEMENTED\n"));
        HRESULT hr = S_OK;
        EX_TRY
        {
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_nThreadDebuggerId);
            m_dbgprot_buffer_add_id(&localbuf, m_nFrameDebuggerId);
            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_STACK_FRAME, MDBGPROT_CMD_STACK_FRAME_GET_ARGUMENTS, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();
            m_nCount = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
            *pcelt = m_nCount;
            m_pValues = new ICorDebugValue*[m_nCount];
            for (int i = 0; i < m_nCount; i++)
            {
                hr = CordbObjectValue::CreateCordbValue(conn, pReply, &m_pValues[i]);
            }
        }
        EX_CATCH_HRESULT(hr);
        return hr;
    }
    LOG((LF_CORDB, LL_INFO100000, "CordbValueEnum - GetCount - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValueEnum::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugValueEnum)
    {
        *pInterface = static_cast<ICorDebugValueEnum*>(this);
    }
    else if (id == IID_ICorDebugEnum)
    {
        *pInterface = static_cast<ICorDebugEnum*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(this);
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}
