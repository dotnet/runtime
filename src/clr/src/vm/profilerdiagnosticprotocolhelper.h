// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __PROFILER_DIAGNOSTIC_PROTOCOL_HELPER_H__
#define __PROFILER_DIAGNOSTIC_PROTOCOL_HELPER_H__

#if defined(FEATURE_PROFAPI_ATTACH_DETACH) && defined(FEATURE_PERFTRACING)

#include "common.h"
#include <diagnosticsprotocol.h>


class IpcStream;

// The Diagnostic command set is 0x01
enum class ProfilerCommandId : uint8_t
{
    // reserved      = 0x00,
    AttachProfiler = 0x01,
    // future
};

struct AttachProfilerCommandPayload
{
    NewArrayHolder<BYTE> incomingBuffer;

    // The protocol buffer is defined as:
    //   uint - attach timeout
    //   CLSID - profiler GUID
    //   string - profiler path
    //   array<char> - client data
    // returns
    //   ulong - status

    uint32_t dwAttachTimeout;
    CLSID profilerGuid;
    LPCWSTR pwszProfilerPath;
    uint32_t cbClientData;
    uint8_t* pClientData;

    static const AttachProfilerCommandPayload* TryParse(BYTE* lpBuffer, uint16_t& BufferSize);
};

class ProfilerDiagnosticProtocolHelper
{
public:
    // IPC event handlers.
    static void HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream* pStream);
    static void AttachProfiler(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);

private:
    const static uint32_t IpcStreamReadBufferSize = 8192;
};

#endif // defined(FEATURE_PROFAPI_ATTACH_DETACH) && defined(FEATURE_PERFTRACING)

#endif // __PROFILER_DIAGNOSTIC_PROTOCOL_HELPER_H__
