// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

void EventPipeProtocolHelper::StopTracing(IpcStream *pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    uint32_t nNumberOfBytesRead = 0;
    EventPipeSessionID sessionId = (EventPipeSessionID) nullptr;
    bool fSuccess = pStream->Read(&sessionId, sizeof(sessionId), nNumberOfBytesRead);
    if (!fSuccess || nNumberOfBytesRead != sizeof(sessionId))
    {
        // TODO: Add error handling.
        delete pStream;
        return;
    }

    EventPipe::Disable(sessionId);
    uint32_t nBytesWritten = 0;
    fSuccess = pStream->Write(&sessionId, sizeof(sessionId), nBytesWritten);
    if (!fSuccess)
    {
        // TODO: Add error handling.
        delete pStream;
        return;
    }

    fSuccess = pStream->Flush();
    if (!fSuccess)
    {
        // TODO: Add error handling.
    }
    delete pStream;
}

static bool TryParseCircularBufferSize(uint8_t *&bufferCursor, uint32_t &bufferLen, uint32_t &circularBufferSizeInMB)
{
    const bool CanParse = TryParse(bufferCursor, bufferLen, circularBufferSizeInMB);
    return CanParse && (circularBufferSizeInMB > 0);
}

void EventPipeProtocolHelper::CollectTracing(IpcStream *pStream)
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

    // TODO: Read within a loop.
    uint8_t buffer[IpcStreamReadBufferSize]{};
    uint32_t nNumberOfBytesRead = 0;
    bool fSuccess = pStream->Read(buffer, sizeof(buffer), nNumberOfBytesRead);
    if (!fSuccess)
    {
        // TODO: Add error handling.
        delete pStream;
        return;
    }

    // The protocol buffer is defined as:
    // X, Y, Z means encode bytes for X followed by bytes for Y followed by bytes for Z
    // message = uint circularBufferMB, string outputPath, array<provider_config> providers
    // uint = 4 little endian bytes
    // wchar = 2 little endian bytes, UTF16 encoding
    // array<T> = uint length, length # of Ts
    // string = (array<char> where the last char must = 0) or (length = 0)
    // provider_config = ulong keywords, uint logLevel, string provider_name, string filter_data

    LPCWSTR strOutputPath;
    uint32_t circularBufferSizeInMB = EventPipeProtocolHelper::DefaultCircularBufferMB;
    CQuickArray<EventPipeProviderConfiguration> providerConfigs;

    uint8_t *pBufferCursor = buffer;
    uint32_t bufferLen = nNumberOfBytesRead;
    if (!TryParseCircularBufferSize(pBufferCursor, bufferLen, circularBufferSizeInMB) ||
        !TryParseString(pBufferCursor, bufferLen, strOutputPath) || // TODO: Remove. Currently ignored in this scenario.
        !TryParseProviderConfiguration(pBufferCursor, bufferLen, providerConfigs))
    {
        // TODO: error handling
        delete pStream;
        return;
    }

    auto sessionId = EventPipe::Enable(
        nullptr,                                        // strOutputPath (ignored in this scenario)
        circularBufferSizeInMB,                         // circularBufferSizeInMB
        DefaultProfilerSamplingRateInNanoseconds,       // ProfilerSamplingRateInNanoseconds
        providerConfigs.Ptr(),                          // pConfigs
        static_cast<uint32_t>(providerConfigs.Size()),  // numConfigs
        EventPipeSessionType::IpcStream,                // EventPipeSessionType
        pStream);                                       // IpcStream

    if (sessionId == 0)
        delete pStream;
}

#endif // FEATURE_PERFTRACING
