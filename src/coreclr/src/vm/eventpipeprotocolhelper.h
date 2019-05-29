// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_PROTOCOL_HELPER_H__
#define __EVENTPIPE_PROTOCOL_HELPER_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "eventpipe.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

class IpcStream;

// The event pipe command set is 0x02
// see diagnosticsipc.h and diagnosticserver.h for more details
enum class EventPipeCommandId : uint8_t
{
    StopTracing    = 0x01,
    CollectTracing = 0x02,
    // future
};

// Command = 0x0202
struct EventPipeCollectTracingCommandPayload
{
    NewArrayHolder<BYTE> incomingBuffer;

    // The protocol buffer is defined as:
    // X, Y, Z means encode bytes for X followed by bytes for Y followed by bytes for Z
    // message = uint circularBufferMB, string outputPath, array<provider_config> providers
    // uint = 4 little endian bytes
    // wchar = 2 little endian bytes, UTF16 encoding
    // array<T> = uint length, length # of Ts
    // string = (array<char> where the last char must = 0) or (length = 0)
    // provider_config = ulong keywords, uint logLevel, string provider_name, string filter_data
    uint32_t circularBufferSizeInMB;
    LPCWSTR outputPath;
    CQuickArray<EventPipeProviderConfiguration> providerConfigs;
    static const EventPipeCollectTracingCommandPayload* TryParse(BYTE* lpBuffer, uint16_t& BufferSize);
};

// Command = 0x0201
struct EventPipeStopTracingCommandPayload
{
    EventPipeSessionID sessionId;
};

class EventPipeProtocolHelper
{
public:
    // IPC event handlers.
    static void HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
    static void StopTracing(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
    static void CollectTracing(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream); // `dotnet-trace collect`
    static bool TryParseProviderConfiguration(uint8_t *&bufferCursor, uint32_t &bufferLen, CQuickArray<EventPipeProviderConfiguration> &result);

private:
    const static uint32_t DefaultCircularBufferMB = 1024; // 1 GB
    const static uint32_t IpcStreamReadBufferSize = 8192;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROTOCOL_HELPER_H__
