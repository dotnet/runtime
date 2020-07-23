// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "fastserializer.h"
#include "dumpdiagnosticprotocolhelper.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"
#include "diagnosticstriggermanager.h"

#ifdef FEATURE_PERFTRACING

void DumpDiagnosticProtocolHelper::HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream* pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    switch ((DumpCommandId)message.GetHeader().CommandId)
    {
    case DumpCommandId::GenerateCoreDump:
        DumpDiagnosticProtocolHelper::GenerateCoreDump(message, pStream);
        break;
    case DumpCommandId::GenerateCoreDumpV2:
        DumpDiagnosticProtocolHelper::GenerateCoreDumpV2(message, pStream);
        break;

    default:
        STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_WARNING, "Received unknown request type (%d)\n", message.GetHeader().CommandSet);
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_UNKNOWN_COMMAND);
        delete pStream;
        break;
    }
}

const GenerateCoreDumpCommandPayload* GenerateCoreDumpCommandPayload::TryParse(BYTE* lpBuffer, uint16_t& BufferSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
    }
    CONTRACTL_END;

    GenerateCoreDumpCommandPayload* payload = new (nothrow) GenerateCoreDumpCommandPayload;
    if (payload == nullptr)
    {
        // OOM
        return nullptr;
    }

    payload->incomingBuffer = lpBuffer;
    uint8_t* pBufferCursor = payload->incomingBuffer;
    uint32_t bufferLen = BufferSize;
    if (!TryParseString(pBufferCursor, bufferLen, payload->dumpName) ||
        !::TryParse(pBufferCursor, bufferLen, payload->dumpType) ||
        !::TryParse(pBufferCursor, bufferLen, payload->diagnostics))
    {
        delete payload;
        return nullptr;
    }

    return payload;
}

const GenerateCoreDumpCommandV2Payload* GenerateCoreDumpCommandV2Payload::TryParse(BYTE* lpBuffer, uint16_t& BufferSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
    }
    CONTRACTL_END;

    GenerateCoreDumpCommandV2Payload* payload = new (nothrow) GenerateCoreDumpCommandV2Payload;
    if (payload == nullptr)
    {
        // OOM
        return nullptr;
    }

    payload->incomingBuffer = lpBuffer;
    uint8_t* pBufferCursor = payload->incomingBuffer;
    uint32_t bufferLen = BufferSize;
    if (!TryParseString(pBufferCursor, bufferLen, payload->dumpName) ||
        !::TryParse(pBufferCursor, bufferLen, payload->dumpType) ||
        !::TryParse(pBufferCursor, bufferLen, payload->diagnostics) ||
        !TryParseString(pBufferCursor, bufferLen, payload->condition) ||
        !TryParseString(pBufferCursor, bufferLen, payload->identity)
        )
    {
        delete payload;
        return nullptr;
    }

    return payload;
}

void DumpDiagnosticProtocolHelper::GenerateCoreDump(DiagnosticsIpc::IpcMessage& message, IpcStream* pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    if (pStream == nullptr)
        return;

    NewHolder<const GenerateCoreDumpCommandPayload> payload = message.TryParsePayload<GenerateCoreDumpCommandPayload>();
    if (payload == nullptr)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
        delete pStream;
        return;
    }
    GenerateCoreDumpImpl(payload->dumpName, payload->dumpType, payload->diagnostics, pStream);
}

class DumpAction : public IDiagnosticsTriggerAction
{
public:
    DumpAction(LPCWSTR dumpName, uint32_t dumpType, uint32_t diagnostics, LPCWSTR identity, IpcStream* pStream)
    {
        this->dumpName = new WCHAR[wcslen(dumpName) + 1]; 
        wcscpy(this->dumpName, dumpName);
        this->identity = new WCHAR[wcslen(identity) + 1]; 
        wcscpy(this->identity, identity);
        this->dumpType = dumpType;
        this->diagnostics = diagnostics;
        this->pStream = pStream;
    }
    
    ~DumpAction()
    {
        delete[] dumpName;
        delete[] identity;
    }

    virtual void Run()
    {
        DumpDiagnosticProtocolHelper::GenerateCoreDumpImpl(this->dumpName, this->dumpType, this->diagnostics, this->pStream);
        this->pStream = nullptr;
        DiagnosticsTriggerManager::GetInstance().UnregisterTrigger(this->identity);
    }

    virtual void Cancel()
    {
        // TODO, andrewau, handle cancel on request
        if (this->pStream != nullptr)
        {
            // TODO, andrewau, the correct error code is operation canceled
            DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
            delete pStream;
            this->pStream = nullptr;
        }
    }
private:
    WCHAR* dumpName;
    uint32_t dumpType;
    uint32_t diagnostics;
    WCHAR* identity;
    IpcStream* pStream;
};

void DumpDiagnosticProtocolHelper::GenerateCoreDumpV2(DiagnosticsIpc::IpcMessage& message, IpcStream* pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    if (pStream == nullptr)
        return;

    NewHolder<const GenerateCoreDumpCommandV2Payload> payload = message.TryParsePayload<GenerateCoreDumpCommandV2Payload>();
    if (payload == nullptr)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
        delete pStream;
        return;
    }
    if (payload->condition != nullptr)
    {
        DumpAction* dumpAction = new (nothrow) DumpAction(payload->dumpName, payload->dumpType, payload->diagnostics, payload->identity, pStream);
        if (dumpAction == nullptr)
        {
            // TODO, andrewau, the correct error code is OOM
            DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
            delete pStream;
            return;
        }
        DiagnosticsTriggerManager& diagnosticsTriggerManager = DiagnosticsTriggerManager::GetInstance();
        if (!diagnosticsTriggerManager.RegisterTrigger(payload->condition, payload->identity, dumpAction))
        {
            // TODO, andrewau, the correct error code is unrecognized condition
            DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
            delete pStream;
            return;
        }
    }
    else
    {
        GenerateCoreDumpImpl(payload->dumpName, payload->dumpType, payload->diagnostics, pStream);
    }
}

void DumpDiagnosticProtocolHelper::GenerateCoreDumpImpl(LPCWSTR dumpName, uint32_t dumpType, uint32_t diagnostics, IpcStream* pStream)
{
#ifdef HOST_UNIX
    MAKE_UTF8PTR_FROMWIDE_NOTHROW(szDumpName, dumpName);
    if (szDumpName != nullptr)
    {
        if (!PAL_GenerateCoreDump(szDumpName, dumpType, diagnostics))
        {
            DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, E_FAIL);
            delete pStream;
            return;
        }
    }
    else
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, E_OUTOFMEMORY);
        delete pStream;
        return;
    }
#else
    if (!GenerateCrashDump(dumpName, dumpType, diagnostics))
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, E_FAIL);
        delete pStream;
        return;
    }
#endif

    DiagnosticsIpc::IpcMessage successResponse;
    HRESULT success = S_OK;
    if (successResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, success))
        successResponse.Send(pStream);
    delete pStream;
}

#endif // FEATURE_PERFTRACING
