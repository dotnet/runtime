// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "fastserializer.h"
#include "dumpdiagnosticprotocolhelper.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

#ifdef FEATURE_PERFTRACING

#ifdef FEATURE_PAL

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

    MAKE_UTF8PTR_FROMWIDE_NOTHROW(szDumpName, payload->dumpName);
    if (szDumpName != nullptr)
    {
        if (!PAL_GenerateCoreDump(szDumpName, payload->dumpType, payload->diagnostics))
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

    DiagnosticsIpc::IpcMessage successResponse;
    if (successResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, S_OK))
        successResponse.Send(pStream);
    delete pStream;
}

#endif // FEATURE_PAL

#endif // FEATURE_PERFTRACING
