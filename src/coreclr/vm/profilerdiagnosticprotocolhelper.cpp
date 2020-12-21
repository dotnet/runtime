// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "fastserializer.h"
#include "profilerdiagnosticprotocolhelper.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

#if defined(FEATURE_PERFTRACING) && defined(FEATURE_PROFAPI_ATTACH_DETACH) && !defined(DACCESS_COMPILE)
#include "profilinghelper.h"
#include "profilinghelper.inl"

void ProfilerDiagnosticProtocolHelper::HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream* pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    if (!g_fEEStarted)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORPROF_E_NOT_YET_AVAILABLE);
        delete pStream;
        return;
    }

    switch ((ProfilerCommandId)message.GetHeader().CommandId)
    {
    case ProfilerCommandId::AttachProfiler:
        ProfilerDiagnosticProtocolHelper::AttachProfiler(message, pStream);
        break;

    default:
        STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_WARNING, "Received unknown request type (%d)\n", message.GetHeader().CommandSet);
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_UNKNOWN_COMMAND);
        delete pStream;
        break;
    }
}

const AttachProfilerCommandPayload* AttachProfilerCommandPayload::TryParse(BYTE* lpBuffer, uint16_t& BufferSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
    }
    CONTRACTL_END;

    AttachProfilerCommandPayload* payload = new (nothrow) AttachProfilerCommandPayload;
    if (payload == nullptr)
    {
        // OOM
        return nullptr;
    }

    payload->incomingBuffer = lpBuffer;
    uint8_t* pBufferCursor = payload->incomingBuffer;
    uint32_t bufferLen = BufferSize;
    if (!::TryParse(pBufferCursor, bufferLen, payload->dwAttachTimeout) ||
        !::TryParse(pBufferCursor, bufferLen, payload->profilerGuid) ||
        !TryParseString(pBufferCursor, bufferLen, payload->pwszProfilerPath) ||
        !::TryParse(pBufferCursor, bufferLen, payload->cbClientData) ||
        !(bufferLen <= payload->cbClientData))
    {
        delete payload;
        return nullptr;
    }

    payload->pClientData = pBufferCursor;

    return payload;
}

void ProfilerDiagnosticProtocolHelper::AttachProfiler(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream)
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
    {
        return;
    }

    HRESULT hr = S_OK;
    NewHolder<const AttachProfilerCommandPayload> payload = message.TryParsePayload<AttachProfilerCommandPayload>();
    if (payload == nullptr)
    {
        hr = CORDIAGIPC_E_BAD_ENCODING;
        goto ErrExit;
    }

    if (!g_profControlBlock.fProfControlBlockInitialized)
    {
        hr = CORPROF_E_RUNTIME_UNINITIALIZED;
        goto ErrExit;
    }

    // Certain actions are only allowable during attach, and this flag is how we track it.
    ClrFlsSetThreadType(ThreadType_ProfAPI_Attach);

    EX_TRY
    {
        hr = ProfilingAPIUtility::LoadProfilerForAttach(&payload->profilerGuid,
                                                        payload->pwszProfilerPath,
                                                        payload->pClientData,
                                                        payload->cbClientData,
                                                        payload->dwAttachTimeout);
    }
    EX_CATCH_HRESULT(hr);

    // Clear the flag so this thread isn't permanently marked as the attach thread.
    ClrFlsClearThreadType(ThreadType_ProfAPI_Attach);

ErrExit:
    if (hr != S_OK)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, hr);
    }
    else
    {
        DiagnosticsIpc::IpcMessage profilerAttachResponse;
        if (profilerAttachResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, hr))
            profilerAttachResponse.Send(pStream);
    }
    delete pStream;
}

#endif // defined(FEATURE_PERFTRACING) && defined(FEATURE_PROFAPI_ATTACH_DETACH) && !defined(DACCESS_COMPILE)
