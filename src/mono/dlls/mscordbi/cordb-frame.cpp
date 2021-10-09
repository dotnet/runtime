// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-FRAME.CPP
//

#include <cordb-code.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-register.h>
#include <cordb-thread.h>
#include <cordb-value.h>

using namespace std;

CordbFrameEnum::CordbFrameEnum(Connection* conn, CordbThread* thread) : CordbBaseMono(conn)
{
    this->m_pThread = thread;
    m_nFrames       = 0;
    m_ppFrames      = NULL;
}

CordbFrameEnum::~CordbFrameEnum()
{
    Reset();
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Next(ULONG celt, ICorDebugFrame* frames[], ULONG* pceltFetched)
{
    GetCount();
    for (int i = 0; i < m_nFrames; i++)
    {
        this->m_ppFrames[i]->QueryInterface(IID_ICorDebugFrame, (void**)&frames[i]);
    }
    LOG((LF_CORDB, LL_INFO1000000, "CordbFrameEnum - Next - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Skip(ULONG celt)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrameEnum - Skip - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Reset(void)
{
    for (int i = 0; i < m_nFrames; i++)
    {
        this->m_ppFrames[i]->InternalRelease();
    }
    m_nFrames = 0;
    if (m_ppFrames)
        free(m_ppFrames);
    m_ppFrames = NULL;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::Clone(ICorDebugEnum** ppEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrameEnum - Clone - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbFrameEnum::GetCount() {
    HRESULT hr = S_OK;
    if (m_nFrames != 0)
        return hr;
    EX_TRY 
    {
        Reset();
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

        m_nFrames  = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        m_ppFrames = (CordbNativeFrame**)malloc(sizeof(CordbNativeFrame*) * m_nFrames);

        for (int i = 0; i < m_nFrames; i++)
        {
            int frameid   = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
            int methodId  = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            int il_offset = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
            int flags     = m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);

            CordbNativeFrame* frame = new CordbNativeFrame(conn, frameid, methodId, il_offset, flags, m_pThread, i);
            frame->InternalAddRef();
            m_ppFrames[i] = frame;
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::GetCount(ULONG* pcelt)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbFrameEnum - GetCount - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    
    hr = GetCount();
    *pcelt = m_nFrames;

    return hr;
}

HRESULT STDMETHODCALLTYPE CordbFrameEnum::QueryInterface(REFIID riid, void** ppvObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrameEnum - QueryInterface - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

CordbJITILFrame::CordbJITILFrame(
    Connection* conn, int frameid, int methodId, int il_offset, int flags, CordbThread* thread)
    : CordbBaseMono(conn)
{
    this->m_debuggerFrameId  = frameid;
    this->m_debuggerMethodId = methodId;
    this->m_ilOffset         = il_offset;
    this->m_flags            = flags;
    this->m_pThread          = thread;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetChain(ICorDebugChain** ppChain)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetChain - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCode(ICorDebugCode** ppCode)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetCode - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetFunction(ICorDebugFunction** ppFunction)
{
    CordbFunction* func = conn->GetProcess()->FindFunction(m_debuggerMethodId);
    if (!func)
    {
        func = new CordbFunction(conn, 0, m_debuggerMethodId, NULL);
    }
    func->QueryInterface(IID_ICorDebugFunction, (void**)ppFunction);
    LOG((LF_CORDB, LL_INFO1000000, "CordbFrame - GetFunction - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetFunctionToken(mdMethodDef* pToken)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetFunctionToken - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerMethodId);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_METHOD, MDBGPROT_CMD_METHOD_GET_INFO, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();
        int flags = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        int iflags = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        *pToken = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd)
{
    *pStart = 0; //forced value, only to make vs work, I'm not sure which value should returned from mono runtime
    *pEnd   = 1; //forced value, only to make vs work, I'm not sure which value should returned from mono runtime
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetStackRange - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCaller(ICorDebugFrame** ppFrame)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetCaller - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCallee(ICorDebugFrame** ppFrame)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetCallee - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::CreateStepper(ICorDebugStepper** ppStepper)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbJITILFrame - CreateStepper - IMPLEMENTED\n"));
    return m_pThread->CreateStepper(ppStepper);
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface)
{
    if (id == IID_ICorDebugILFrame)
    {
        *pInterface = static_cast<ICorDebugILFrame*>(this);
    }
    else if (id == IID_ICorDebugILFrame2)
    {
        *pInterface = static_cast<ICorDebugILFrame2*>(this);
    }
    else if (id == IID_ICorDebugILFrame3)
    {
        *pInterface = static_cast<ICorDebugILFrame3*>(this);
    }
    else if (id == IID_ICorDebugILFrame4)
    {
        *pInterface = static_cast<ICorDebugILFrame4*>(this);
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::RemapFunction(ULONG32 newILOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - RemapFunction - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::EnumerateTypeParameters(ICorDebugTypeEnum** ppTyParEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - EnumerateTypeParameters - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetReturnValueForILOffset(ULONG32 ILoffset, ICorDebugValue** ppReturnValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetReturnValueForILOffset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::EnumerateLocalVariablesEx(ILCodeKind flags, ICorDebugValueEnum** ppValueEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - EnumerateLocalVariablesEx - IMPLEMENTED\n"));
    CordbValueEnum* pCordbValueEnum = new CordbValueEnum(conn, m_pThread->GetThreadId(), m_debuggerFrameId, false, flags);
    return pCordbValueEnum->QueryInterface(IID_ICorDebugValueEnum, (void**)ppValueEnum);
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetLocalVariableEx(ILCodeKind flags, DWORD dwIndex, ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetLocalVariableEx - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetCodeEx(ILCodeKind flags, ICorDebugCode** ppCode)
{
    if (flags == ILCODE_REJIT_IL)
        *ppCode = NULL;
    else
    {
        ICorDebugFunction* ppFunction;
        GetFunction(&ppFunction);
        ppFunction->GetILCode(ppCode);
        ppFunction->Release();
    }
    LOG((LF_CORDB, LL_INFO1000000, "CordbJITILFrame - GetCodeEx - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetIP(ULONG32* pnOffset, CorDebugMappingResult* pMappingResult)
{
    *pnOffset       = m_ilOffset;
    *pMappingResult = MAPPING_EXACT;
    LOG((LF_CORDB, LL_INFO1000000, "CordbFrame - GetIP - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::SetIP(ULONG32 nOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - SetIP - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::EnumerateLocalVariables(ICorDebugValueEnum** ppValueEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - EnumerateLocalVariables - IMPLEMENTED\n"));
    CordbValueEnum *pCordbValueEnum = new CordbValueEnum(conn, m_pThread->GetThreadId(), false, m_debuggerFrameId);
    return pCordbValueEnum->QueryInterface(IID_ICorDebugValueEnum, (void**)ppValueEnum);
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetLocalVariable(DWORD dwIndex, ICorDebugValue** ppValue)
{
    HRESULT hr = S_OK;
    EX_TRY 
    {    
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_pThread->GetThreadId());
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerFrameId);
        m_dbgprot_buffer_add_int(&localbuf, 1);
        m_dbgprot_buffer_add_int(&localbuf, dwIndex);

        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_STACK_FRAME, MDBGPROT_CMD_STACK_FRAME_GET_VALUES, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();
        hr = CordbObjectValue::CreateCordbValue(conn, pReply, ppValue);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::EnumerateArguments(ICorDebugValueEnum** ppValueEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - EnumerateArguments - IMPLEMENTED\n"));
    CordbValueEnum* pCordbValueEnum = new CordbValueEnum(conn, m_pThread->GetThreadId(), m_debuggerFrameId, true);
    return pCordbValueEnum->QueryInterface(IID_ICorDebugValueEnum, (void**)ppValueEnum);
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetArgument(DWORD dwIndex, ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbFrame - GetArgument - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY 
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_pThread->GetThreadId());
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerFrameId);

        m_dbgprot_buffer_add_int(&localbuf, dwIndex);
        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_STACK_FRAME, MDBGPROT_CMD_STACK_FRAME_GET_ARGUMENT, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        hr = CordbObjectValue::CreateCordbValue(conn, pReply, ppValue);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetStackDepth(ULONG32* pDepth)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetStackDepth - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::GetStackValue(DWORD dwIndex, ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - GetStackValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbJITILFrame::CanSetIP(ULONG32 nOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbFrame - CanSetIP - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

CordbNativeFrame::~CordbNativeFrame()
{
    m_JITILFrame->InternalRelease();
}

CordbNativeFrame::CordbNativeFrame(
    Connection* conn, int frameid, int methodId, int il_offset, int flags, CordbThread* thread, int posFrame)
    : CordbBaseMono(conn)
{
    m_JITILFrame = new CordbJITILFrame(conn, frameid, methodId, il_offset, flags, thread);
    m_JITILFrame->InternalAddRef();
    this->m_pThread = thread;
    this->m_nPosFrame = posFrame;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetIP(ULONG32* pnOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetIP - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::SetIP(ULONG32 nOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - SetIP - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetRegisterSet(ICorDebugRegisterSet** ppRegisters)
{
    return m_pThread->GetRegisterSet(ppRegisters);
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalRegisterValue(CorDebugRegister reg,
                                                                  ULONG            cbSigBlob,
                                                                  PCCOR_SIGNATURE  pvSigBlob,
                                                                  ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetLocalRegisterValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalDoubleRegisterValue(CorDebugRegister highWordReg,
                                                                        CorDebugRegister lowWordReg,
                                                                        ULONG            cbSigBlob,
                                                                        PCCOR_SIGNATURE  pvSigBlob,
                                                                        ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetLocalDoubleRegisterValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalMemoryValue(CORDB_ADDRESS    address,
                                                                ULONG            cbSigBlob,
                                                                PCCOR_SIGNATURE  pvSigBlob,
                                                                ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetLocalMemoryValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalRegisterMemoryValue(CorDebugRegister highWordReg,
                                                                        CORDB_ADDRESS    lowWordAddress,
                                                                        ULONG            cbSigBlob,
                                                                        PCCOR_SIGNATURE  pvSigBlob,
                                                                        ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetLocalRegisterMemoryValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetLocalMemoryRegisterValue(CORDB_ADDRESS    highWordAddress,
                                                                        CorDebugRegister lowWordRegister,
                                                                        ULONG            cbSigBlob,
                                                                        PCCOR_SIGNATURE  pvSigBlob,
                                                                        ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetLocalMemoryRegisterValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::CanSetIP(ULONG32 nOffset)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - CanSetIP - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetChain(ICorDebugChain** ppChain)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetChain - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetCode(ICorDebugCode** ppCode)
{
    ICorDebugFunction* ppFunction;
    m_JITILFrame->GetFunction(&ppFunction);
    ppFunction->GetILCode(ppCode);
    ppFunction->Release();
    LOG((LF_CORDB, LL_INFO100000, "CordbJITILFrame - GetCodeEx - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetFunction(ICorDebugFunction** ppFunction)
{
    return m_JITILFrame->GetFunction(ppFunction);
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetFunctionToken(mdMethodDef* pToken)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetFunctionToken - IMPLEMENTED\n"));
    return m_JITILFrame->GetFunctionToken(pToken);
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd)
{
    return m_JITILFrame->GetStackRange(pStart, pEnd);
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetCaller(ICorDebugFrame** ppFrame)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetCaller - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetCallee(ICorDebugFrame** ppFrame)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetCallee - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::CreateStepper(ICorDebugStepper** ppStepper)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - CreateStepper - IMPLEMENTED\n"));
    return m_pThread->CreateStepper(ppStepper);
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugFrame)
    {
        *pInterface = static_cast<ICorDebugFrame*>(static_cast<ICorDebugNativeFrame*>(this));
    }
    else if (id == IID_ICorDebugNativeFrame)
    {
        *pInterface = static_cast<ICorDebugNativeFrame*>(this);
    }
    else if (id == IID_ICorDebugNativeFrame2)
    {
        *pInterface = static_cast<ICorDebugNativeFrame2*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugNativeFrame*>(this));
    }
    else
    {
        // might be searching for an IL Frame. delegate that search to the
        // JITILFrame
        if (m_JITILFrame != NULL)
        {
            return m_JITILFrame->QueryInterface(id, pInterface);
        }
        else
        {
            *pInterface = NULL;
            return E_NOINTERFACE;
        }
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::IsChild(BOOL* pIsChild)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - IsChild - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::IsMatchingParentFrame(ICorDebugNativeFrame2* pPotentialParentFrame,
                                                                  BOOL*                  pIsParent)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - IsMatchingParentFrame - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbNativeFrame::GetStackParameterSize(ULONG32* pSize)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbNativeFrame - GetStackParameterSize - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}
