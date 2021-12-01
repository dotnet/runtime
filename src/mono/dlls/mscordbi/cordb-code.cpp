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

CordbCode::CordbCode(Connection* conn, CordbFunction* func) : CordbBaseMono(conn)
{
    this->m_pFunction = func;
    m_nSize = 0;
}

HRESULT CordbCode::IsIL(BOOL* pbIL)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbCode - IsIL - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbCode::GetFunction(ICorDebugFunction** ppFunction)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbCode - GetFunction - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbCode::GetAddress(CORDB_ADDRESS* pStart)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbCode - GetAddress - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

ULONG32 CordbCode::GetSize()
{
    if (m_nSize != 0)
        return m_nSize;

    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);

    m_dbgprot_buffer_add_id(&localbuf, this->GetFunction()->GetDebuggerId());
    int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_GET_BODY, &localbuf);
    m_dbgprot_buffer_free(&localbuf);

    ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
    if (received_reply_packet->Error() > 0 || received_reply_packet->Error2() > 0)
        return 0;
    MdbgProtBuffer* pReply = received_reply_packet->Buffer();

    m_nSize = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
    return m_nSize;
}

HRESULT CordbCode::GetSize(ULONG32* pcBytes)
{
    *pcBytes      = GetSize();
    LOG((LF_CORDB, LL_INFO1000000, "CordbCode - GetSize - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbCode::CreateBreakpoint(ULONG32 offset, ICorDebugFunctionBreakpoint** ppBreakpoint)
{
    // add it in a list to not recreate a already created breakpoint
    CordbFunctionBreakpoint* bp = new CordbFunctionBreakpoint(conn, this, offset);
    bp->QueryInterface(IID_ICorDebugFunctionBreakpoint, (void**)ppBreakpoint);
    LOG((LF_CORDB, LL_INFO1000000, "CordbCode - CreateBreakpoint - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbCode::GetCode(
    ULONG32 startOffset, ULONG32 endOffset, ULONG32 cBufferAlloc, BYTE buffer[], ULONG32* pcBufferSize)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbCode - GetCode - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);

        m_dbgprot_buffer_add_id(&localbuf, this->GetFunction()->GetDebuggerId());
        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_GET_BODY, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        ULONG32 totalSize = GetSize();

        if (cBufferAlloc < endOffset - startOffset)
            endOffset = startOffset + cBufferAlloc;

        if (endOffset > totalSize)
            endOffset = totalSize;

        if (startOffset > totalSize)
            startOffset = totalSize;

        uint8_t* m_rgbCode = m_dbgprot_decode_byte_array(pReply->p, &pReply->p, pReply->end, (int32_t*)pcBufferSize);
        memcpy(buffer,
                m_rgbCode+startOffset,
                endOffset - startOffset);
        free(m_rgbCode);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbCode::GetVersionNumber(ULONG32* nVersion)
{
    *nVersion = 1;
    LOG((LF_CORDB, LL_INFO100000, "CordbCode - GetVersionNumber - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbCode::GetILToNativeMapping(ULONG32 cMap, ULONG32* pcMap, COR_DEBUG_IL_TO_NATIVE_MAP map[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbCode - GetILToNativeMapping - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbCode::GetEnCRemapSequencePoints(ULONG32 cMap, ULONG32* pcMap, ULONG32 offsets[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbCode - GetEnCRemapSequencePoints - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbCode::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugCode)
    {
        *pInterface = static_cast<ICorDebugCode*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugCode*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}
