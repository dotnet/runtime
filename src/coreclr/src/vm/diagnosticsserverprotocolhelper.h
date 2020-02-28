// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTICSSERVER_PROTOCOL_HELPER_H__
#define __DIAGNOSTICSSERVER_PROTOCOL_HELPER_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

class IpcStream;

/**
 * The Diagnostics Server command set is 0xFF
 * see diagnosticsipc.h and diagnosticserver.h for more details
 * enum class DiagnosticServerCommandId : uint8_t
 * {
 *     OK    = 0x00,
 *     Error = 0xFF,
 *     Advertise = 0x01,
 * };
 */


// Command = 0xFF01
struct DiagnosticsServerAdvertiseCommandPayload
{
    NewArrayHolder<BYTE> incomingBuffer;

    // The protocol buffer is defined as:
    // X, Y, Z means encode bytes for X followed by bytes for Y followed by bytes for Z
    //
    // PID = ulong
    // hash = CLSID (GUID)
    uint64_t pid;
    CLSID hash;
    static const DiagnosticsServerAdvertiseCommandPayload* TryParse(BYTE* lpBuffer, uint16_t& BufferSize);
};

class DiagnosticsServerProtocolHelper
{
public:
    // IPC event handlers.
    static void HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
};

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTICSSERVER_PROTOCOL_HELPER_H__
