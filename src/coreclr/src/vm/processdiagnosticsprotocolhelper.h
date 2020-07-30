// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __PROCESS_PROTOCOL_HELPER_H__
#define __PROCESS_PROTOCOL_HELPER_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "eventpipe.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

class IpcStream;

// The event pipe command set is 0x02
// see diagnosticsipc.h and diagnosticserver.h for more details
enum class ProcessCommandId : uint8_t
{
    GetProcessInfo = 0x00,
    ResumeRuntime  = 0x01,
    // future
};

// command = 0x0400
struct ProcessInfoPayload
{
    // The protocol buffer is defined as:
    // X, Y, Z means encode bytes for X followed by bytes for Y followed by bytes for Z
    // uint = 4 little endian bytes
    // long = 8 little endian bytes
    // GUID = 16 little endian bytes
    // wchar = 2 little endian bytes, UTF16 encoding
    // array<T> = uint length, length # of Ts
    // string = (array<char> where the last char must = 0) or (length = 0)

    // ProcessInfo = long pid, string cmdline, string OS, string arch, GUID runtimeCookie
    uint64_t ProcessId;
    LPCWSTR CommandLine;
    LPCWSTR OS;
    LPCWSTR Arch;
    GUID RuntimeCookie;
    uint16_t GetSize();
    bool Flatten(BYTE * &lpBuffer, uint16_t& cbSize);
};

class ProcessDiagnosticsProtocolHelper
{
public:
    // IPC event handlers.
    static void HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
    static void GetProcessInfo(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
    static void ResumeRuntimeStartup(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
};

#endif // FEATURE_PERFTRACING

#endif // __PROCESS_PROTOCOL_HELPER_H__
