// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-THREAD.CPP
//

#include <cordb-appdomain.h>
#include <cordb-blocking-obj.h>
#include <cordb-chain.h>
#include <cordb-eval.h>
#include <cordb-frame.h>
#include <cordb-process.h>
#include <cordb-register.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb-stackwalk.h>
#include <cordb-function.h>
#include <cordb.h>


CordbStackWalk::CordbStackWalk(Connection* conn, CordbThread* ppThread): CordbBaseMono(conn)
{
    m_pThread = ppThread;
    m_pThread->InternalAddRef();
    m_nCurrentFrame = 0;
    m_nFrames = 0;
    m_ppFrames = NULL;
}

CordbStackWalk::~CordbStackWalk()
{
    if (m_pThread)
        m_pThread->InternalRelease();
    if (m_ppFrames != NULL)
    {
        for (int i = 0; i < m_nFrames; i++)
        {
            this->m_ppFrames[i]->InternalRelease();
        }
        m_nFrames = 0;
        free(m_ppFrames);
    }
}

void CordbStackWalk::Reset()
{
    m_nFrames = 0;
    m_nCurrentFrame = 0;
}

HRESULT STDMETHODCALLTYPE CordbStackWalk::GetContext(ULONG32 contextFlags, ULONG32 contextBufSize, ULONG32 *contextSize, BYTE contextBuf[  ])
{
    if (m_nFrames != 0 && m_nCurrentFrame >= m_nFrames)
        return S_OK;
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_pThread->GetThreadId());
        m_dbgprot_buffer_add_int(&localbuf, m_nCurrentFrame);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_THREAD, MDBGPROT_CMD_THREAD_GET_CONTEXT, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        int contextSizeReceived = 0;
        int64_t stack_pointer = m_dbgprot_decode_long(pReply->p, &pReply->p, pReply->end);
        *contextSize = contextSizeReceived;
        memcpy(contextBuf+POS_RSP, &stack_pointer, sizeof(int64_t));
        LOG((LF_CORDB, LL_INFO100000, "CordbStackWalk - GetContext - IMPLEMENTED - %d - %lld\n", m_nCurrentFrame, stack_pointer));
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbStackWalk::SetContext(CorDebugSetContextFlag flag, ULONG32 contextSize, BYTE context[  ])
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_pThread->GetThreadId());
        int64_t stack_pointer;
        memcpy(&stack_pointer, context+POS_RSP, sizeof(int64_t));
        m_dbgprot_buffer_add_long(&localbuf, stack_pointer);

        LOG((LF_CORDB, LL_INFO100000, "CordbStackWalk - SetContext - IMPLEMENTED - %d - %lld\n", m_nCurrentFrame, stack_pointer));

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_THREAD, MDBGPROT_CMD_THREAD_SET_CONTEXT, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        m_nCurrentFrame = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbStackWalk::Next(void)
{
    PopulateStackWalk();
    if (m_nCurrentFrame + 1 >= m_nFrames)
        return CORDBG_S_AT_END_OF_STACK;
    m_nCurrentFrame++;
    return S_OK;
}

HRESULT CordbStackWalk::PopulateStackWalk() {
    HRESULT hr = S_OK;
    if (m_nFrames != 0)
        return hr;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_pThread->GetThreadId());
        m_dbgprot_buffer_add_int(&localbuf, 0);
        m_dbgprot_buffer_add_int(&localbuf, -1);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_THREAD, MDBGPROT_CMD_THREAD_GET_FRAME_INFO, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        m_nFrames = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        m_ppFrames = (CordbNativeFrame**)malloc(sizeof(CordbNativeFrame*) * m_nFrames);

        for (int i = 0; i < m_nFrames; i++)
        {
            int frameid = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
            int methodId = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            int il_offset = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
            int flags = m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);

            CordbNativeFrame* frame = new CordbNativeFrame(conn, frameid, methodId, il_offset, flags, m_pThread, i);
            frame->InternalAddRef();
            m_ppFrames[i] = frame;
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbStackWalk::GetFrame(ICorDebugFrame **pFrame)
{
    PopulateStackWalk();
    if (m_nCurrentFrame >= m_nFrames)
        return CORDBG_E_PAST_END_OF_STACK;
    return this->m_ppFrames[m_nCurrentFrame]->QueryInterface(IID_ICorDebugFrame, (void**)pFrame);
}

// standard QI function
HRESULT CordbStackWalk::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugStackWalk)
    {
        *pInterface = static_cast<ICorDebugStackWalk*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugStackWalk*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}
