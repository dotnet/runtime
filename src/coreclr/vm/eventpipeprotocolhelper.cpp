// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "fastserializer.h"
#include "eventpipefile.h"
#include "eventpipeprotocolhelper.h"
#include "eventpipesession.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

#ifdef FEATURE_PERFTRACING

static bool IsNullOrWhiteSpace(LPCWSTR value)
{
    if (value == nullptr)
        return true;

    while (*value)
    {
        if (!iswspace(*value))
            return false;
        ++value;
    }
    return true;
}

static bool TryParseCircularBufferSize(uint8_t*& bufferCursor, uint32_t& bufferLen, uint32_t& circularBufferSizeInMB)
{
    const bool CanParse = TryParse(bufferCursor, bufferLen, circularBufferSizeInMB);
    return CanParse && (circularBufferSizeInMB > 0);
}

static bool TryParseSerializationFormat(uint8_t*& bufferCursor, uint32_t& bufferLen, EventPipeSerializationFormat& serializationFormat)
{
    const bool CanParse = TryParse(bufferCursor, bufferLen, (uint32_t&)serializationFormat);
    return CanParse && (0 <= (int)serializationFormat) && ((int)serializationFormat < (int)EventPipeSerializationFormat::Count);
}

static bool TryParseRundownRequested(uint8_t*& bufferCursor, uint32_t& bufferLen, bool& rundownRequested)
{
    return TryParse(bufferCursor, bufferLen, rundownRequested);
}

const EventPipeCollectTracing2CommandPayload* EventPipeCollectTracing2CommandPayload::TryParse(BYTE* lpBuffer, uint16_t& BufferSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
    }
    CONTRACTL_END;

    EventPipeCollectTracing2CommandPayload *payload = new (nothrow) EventPipeCollectTracing2CommandPayload;
    if (payload == nullptr)
    {
        // OOM
        return nullptr;
    }

    payload->incomingBuffer = lpBuffer;
    uint8_t* pBufferCursor = payload->incomingBuffer;
    uint32_t bufferLen = BufferSize;
    if (!TryParseCircularBufferSize(pBufferCursor, bufferLen, payload->circularBufferSizeInMB) ||
        !TryParseSerializationFormat(pBufferCursor, bufferLen, payload->serializationFormat) ||
        !TryParseRundownRequested(pBufferCursor, bufferLen, payload->rundownRequested) ||
        !EventPipeProtocolHelper::TryParseProviderConfiguration(pBufferCursor, bufferLen, payload->providerConfigs))
    {
        delete payload;
        return nullptr;
    }

    return payload;
}

const EventPipeCollectTracingCommandPayload* EventPipeCollectTracingCommandPayload::TryParse(BYTE* lpBuffer, uint16_t& BufferSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
    }
    CONTRACTL_END;

    EventPipeCollectTracingCommandPayload *payload = new (nothrow) EventPipeCollectTracingCommandPayload;
    if (payload == nullptr)
    {
        // OOM
        return nullptr;
    }

    payload->incomingBuffer = lpBuffer;
    uint8_t* pBufferCursor = payload->incomingBuffer;
    uint32_t bufferLen = BufferSize;
    if (!TryParseCircularBufferSize(pBufferCursor, bufferLen, payload->circularBufferSizeInMB) ||
        !TryParseSerializationFormat(pBufferCursor, bufferLen, payload->serializationFormat) ||
        !EventPipeProtocolHelper::TryParseProviderConfiguration(pBufferCursor, bufferLen, payload->providerConfigs))
    {
        delete payload;
        return nullptr;
    }

    return payload;
}

void EventPipeProtocolHelper::HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream* pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    switch ((EventPipeCommandId)message.GetHeader().CommandId)
    {
    case EventPipeCommandId::CollectTracing:
        EventPipeProtocolHelper::CollectTracing(message, pStream);
        break;
    case EventPipeCommandId::CollectTracing2:
        EventPipeProtocolHelper::CollectTracing2(message, pStream);
        break;
    case EventPipeCommandId::StopTracing:
        EventPipeProtocolHelper::StopTracing(message, pStream);
        break;

    default:
        STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_WARNING, "Received unknown request type (%d)\n", message.GetHeader().CommandSet);
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_UNKNOWN_COMMAND);
        delete pStream;
        break;
    }
}

bool EventPipeProtocolHelper::TryParseProviderConfiguration(uint8_t *&bufferCursor, uint32_t &bufferLen, CQuickArray<EventPipeProviderConfiguration> &result)
{
    // Picking an arbitrary upper bound,
    // This should be larger than any reasonable client request.
    const uint32_t MaxCountConfigs = 1000; // TODO: This might be too large.

    uint32_t countConfigs = 0;
    if (!TryParse(bufferCursor, bufferLen, countConfigs))
        return false;
    if (countConfigs > MaxCountConfigs)
        return false;
    EventPipeProviderConfiguration *pConfigs = result.AllocNoThrow(countConfigs);
    if (pConfigs == nullptr)
        return false;

    for (uint32_t i = 0; i < countConfigs; i++)
    {
        uint64_t keywords = 0;
        if (!TryParse(bufferCursor, bufferLen, keywords))
            return false;

        uint32_t logLevel = 0;
        if (!TryParse(bufferCursor, bufferLen, logLevel))
            return false;
        if (logLevel > 5) // (logLevel > EventPipeEventLevel::Verbose)
            return false;

        LPCWSTR pProviderName = nullptr;
        if (!TryParseString(bufferCursor, bufferLen, pProviderName))
            return false;
        if (IsNullOrWhiteSpace(pProviderName))
            return false;

        LPCWSTR pFilterData = nullptr; // This parameter is optional.
        TryParseString(bufferCursor, bufferLen, pFilterData);

        pConfigs[i] = EventPipeProviderConfiguration(pProviderName, keywords, logLevel, pFilterData);
    }
    return (countConfigs > 0);
}

void EventPipeProtocolHelper::StopTracing(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    NewHolder<const EventPipeStopTracingCommandPayload> payload = message.TryParsePayload<EventPipeStopTracingCommandPayload>();
    if (payload == nullptr)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
        delete pStream;
        return;
    }

    EventPipe::Disable(payload->sessionId);

    DiagnosticsIpc::IpcMessage stopTracingResponse;
    if (stopTracingResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, payload->sessionId))
        stopTracingResponse.Send(pStream);

    bool fSuccess = pStream->Flush();
    if (!fSuccess)
    {
        // TODO: Add error handling.
    }
    delete pStream;
}

void EventPipeProtocolHelper::CollectTracing(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    NewHolder<const EventPipeCollectTracingCommandPayload> payload = message.TryParsePayload<EventPipeCollectTracingCommandPayload>();
    if (payload == nullptr)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
        delete pStream;
        return;
    }

    auto sessionId = EventPipe::Enable(
        nullptr,                                        // strOutputPath (ignored in this scenario)
        payload->circularBufferSizeInMB,                         // circularBufferSizeInMB
        payload->providerConfigs.Ptr(),                          // pConfigs
        static_cast<uint32_t>(payload->providerConfigs.Size()),  // numConfigs
        EventPipeSessionType::IpcStream,                // EventPipeSessionType
        payload->serializationFormat,                   // EventPipeSerializationFormat
        true,                                           // rundownRequested
        pStream);                                       // IpcStream

    if (sessionId == 0)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, E_FAIL);
        delete pStream;
    }
    else
    {
        DiagnosticsIpc::IpcMessage successResponse;
        if (successResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, sessionId))
            successResponse.Send(pStream);
        EventPipe::StartStreaming(sessionId);
    }
}

void EventPipeProtocolHelper::CollectTracing2(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    const EventPipeCollectTracing2CommandPayload* payload = message.TryParsePayload<EventPipeCollectTracing2CommandPayload>();
    if (payload == nullptr)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
        delete payload;
        delete pStream;
        return;
    }

    auto sessionId = EventPipe::Enable(
        nullptr,                                        // strOutputPath (ignored in this scenario)
        payload->circularBufferSizeInMB,                         // circularBufferSizeInMB
        payload->providerConfigs.Ptr(),                          // pConfigs
        static_cast<uint32_t>(payload->providerConfigs.Size()),  // numConfigs
        EventPipeSessionType::IpcStream,                // EventPipeSessionType
        payload->serializationFormat,                   // EventPipeSerializationFormat
        payload->rundownRequested,                      // rundownRequested
        pStream);                                       // IpcStream

    if (sessionId == 0)
    {
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, E_FAIL);
        delete payload;
        delete pStream;
    }
    else
    {
        DiagnosticsIpc::IpcMessage successResponse;
        if (successResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, sessionId))
            successResponse.Send(pStream);
        EventPipe::StartStreaming(sessionId);
    }
}

#endif // FEATURE_PERFTRACING
