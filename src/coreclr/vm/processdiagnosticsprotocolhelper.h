// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    GetProcessInfo        = 0x00,
    ResumeRuntime         = 0x01,
    GetProcessEnvironment = 0x02
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

    // ProcessInfo = long pid, string cmdline, string OS, string arch, GUID runtimeCookie
    uint64_t ProcessId;
    LPCWSTR CommandLine;
    LPCWSTR OS;
    LPCWSTR Arch;
    GUID RuntimeCookie;
    uint16_t GetSize();
    bool Flatten(BYTE * &lpBuffer, uint16_t& cbSize);
};

struct EnvironmentHelper
{
    // The environemnt is sent back as an optional continuation stream of data.
    // It is encoded in the typical length-prefixed array format as defined in
    // the Diagnostics IPC Spec: https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md

    struct InitialPayload
    {
        uint32_t continuationSizeInBytes;
        uint16_t future;
    };

    // sent as: Array<Array<WCHAR>>
    NewArrayHolder<const WCHAR> Environment = nullptr;

    void PopulateEnvironment();
    uint32_t GetNumberOfElements() { PopulateEnvironment(); return _nEnvEntries; }

    // Write the environment block to the stream
    bool WriteToStream(IpcStream *pStream);

    // The size in bytes of the Diagnostic IPC Protocol encoded Environment Block
    // It is encoded as Array<Array<WCHAR>> so this will return at least sizeof(uint32_t)
    // if the env block is empty or failed to be snapshotted since the stream will
    // just contain 0 for the array length.
    uint32_t GetEnvironmentBlockSize();
private:
    uint32_t _nEnvEntries = 0;
    uint32_t _nWchars = 0;
};

class ProcessDiagnosticsProtocolHelper
{
public:
    // IPC event handlers.
    static void HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
    static void GetProcessInfo(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
    static void GetProcessEnvironment(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
    static void ResumeRuntimeStartup(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream);
};

#endif // FEATURE_PERFTRACING

#endif // __PROCESS_PROTOCOL_HELPER_H__
