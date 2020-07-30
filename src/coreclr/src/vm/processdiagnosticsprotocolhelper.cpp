// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "fastserializer.h"
#include "processdiagnosticsprotocolhelper.h"
#include "eventpipeeventsource.h"
#include "diagnosticsprotocol.h"
#include "diagnosticserver.h"

#ifdef FEATURE_PERFTRACING

static inline uint32_t GetStringLength(const WCHAR *&value)
{
    return static_cast<uint32_t>(wcslen(value) + 1);
}

static bool TryWriteString(uint8_t * &bufferCursor, uint16_t &bufferLen, const WCHAR *value)
{
    uint32_t stringLen = GetStringLength(value);
    S_UINT16 totalStringSizeInBytes(stringLen * sizeof(WCHAR) + sizeof(uint32_t));
    ASSERT(!totalStringSizeInBytes.IsOverflow());
    ASSERT(bufferLen >= totalStringSizeInBytes.Value());
    if (bufferLen < totalStringSizeInBytes.Value() || totalStringSizeInBytes.IsOverflow())
        return false;

    memcpy(bufferCursor, &stringLen, sizeof(stringLen));
    bufferCursor += sizeof(stringLen);

    memcpy(bufferCursor, value, stringLen * sizeof(WCHAR));
    bufferCursor += stringLen * sizeof(WCHAR);

    bufferLen -= totalStringSizeInBytes.Value();
    return true;
}

uint16_t ProcessInfoPayload::GetSize()
{
    LIMITED_METHOD_CONTRACT;

    // see IPC spec @ https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md
    // for definition of serialization format

    // uint64_t ProcessId;  -> 8 bytes
    // GUID RuntimeCookie;  -> 16 bytes
    // LPCWSTR CommandLine; -> 4 bytes + strlen * sizeof(WCHAR)
    // LPCWSTR OS;          -> 4 bytes + strlen * sizeof(WCHAR)
    // LPCWSTR Arch;        -> 4 bytes + strlen * sizeof(WCHAR)

    S_UINT16 size = S_UINT16(0);
    size += sizeof(ProcessId);
    size += sizeof(RuntimeCookie);

    size += sizeof(uint32_t);
    size += CommandLine != nullptr ?
        S_UINT16(GetStringLength(CommandLine) * sizeof(WCHAR)) :
        S_UINT16(0);

    size += sizeof(uint32_t);
    size += OS != nullptr ?
        S_UINT16(GetStringLength(OS) * sizeof(WCHAR)) :
        S_UINT16(0);

    size += sizeof(uint32_t);
    size += Arch != nullptr ?
        S_UINT16(GetStringLength(Arch) * sizeof(WCHAR)) :
        S_UINT16(0);

    ASSERT(!size.IsOverflow());
    return size.Value();
}

bool ProcessInfoPayload::Flatten(BYTE * &lpBuffer, uint16_t &cbSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(cbSize == GetSize());
        PRECONDITION(lpBuffer != nullptr);
    }
    CONTRACTL_END;

    // see IPC spec @ https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md
    // for definition of serialization format

    bool fSuccess = true;
    // uint64_t ProcessId;
    memcpy(lpBuffer, &ProcessId, sizeof(ProcessId));
    lpBuffer += sizeof(ProcessId);
    cbSize -= sizeof(ProcessId);

    // GUID RuntimeCookie;
    memcpy(lpBuffer, &RuntimeCookie, sizeof(RuntimeCookie));
    lpBuffer += sizeof(RuntimeCookie);
    cbSize -= sizeof(RuntimeCookie);

    // LPCWSTR CommandLine;
    fSuccess &= TryWriteString(lpBuffer, cbSize, CommandLine);

    // LPCWSTR OS;
    if (fSuccess)
        fSuccess &= TryWriteString(lpBuffer, cbSize, OS);

    // LPCWSTR Arch;
    if (fSuccess)
        fSuccess &= TryWriteString(lpBuffer, cbSize, Arch);

    // Assert we've used the whole buffer we were given
    ASSERT(cbSize == 0);

    return fSuccess;
}

void ProcessDiagnosticsProtocolHelper::GetProcessInfo(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    struct ProcessInfoPayload payload = {};

    // Get cmdline
    payload.CommandLine = GetManagedCommandLine();

    // Checkout https://github.com/dotnet/coreclr/pull/24433 for more information about this fall back.
    if (payload.CommandLine == nullptr)
    {
        // Use the result from GetCommandLineW() instead
        payload.CommandLine = GetCommandLineW();
    }

    // get OS + Arch info
    payload.OS = EventPipeEventSource::s_pOSInformation;
    payload.Arch = EventPipeEventSource::s_pArchInformation;

    // Get the PID
    payload.ProcessId = GetCurrentProcessId();

    // Get the cookie
    payload.RuntimeCookie = DiagnosticsIpc::GetAdvertiseCookie_V1();

    DiagnosticsIpc::IpcMessage ProcessInfoResponse;
    const bool fSuccess = ProcessInfoResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, payload) ?
        ProcessInfoResponse.Send(pStream) :
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, E_FAIL);

    if (!fSuccess)
        STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_WARNING, "Failed to send DiagnosticsIPC response");

    delete pStream;
}

void ProcessDiagnosticsProtocolHelper::ResumeRuntimeStartup(DiagnosticsIpc::IpcMessage& message, IpcStream *pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    // no payload
    DiagnosticServer::ResumeRuntimeStartup();
    HRESULT res = S_OK;

    DiagnosticsIpc::IpcMessage successResponse;
    const bool fSuccess = successResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, res) ?
        successResponse.Send(pStream) :
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, E_FAIL);
    if (!fSuccess)
        STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_WARNING, "Failed to send DiagnosticsIPC response");

    delete pStream;
}

void ProcessDiagnosticsProtocolHelper::HandleIpcMessage(DiagnosticsIpc::IpcMessage& message, IpcStream* pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    switch ((ProcessCommandId)message.GetHeader().CommandId)
    {
    case ProcessCommandId::GetProcessInfo:
        ProcessDiagnosticsProtocolHelper::GetProcessInfo(message, pStream);
        break;
    case ProcessCommandId::ResumeRuntime:
        ProcessDiagnosticsProtocolHelper::ResumeRuntimeStartup(message, pStream);
        break;

    default:
        STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_WARNING, "Received unknown request type (%d)\n", message.GetHeader().CommandSet);
        DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_UNKNOWN_COMMAND);
        delete pStream;
        break;
    }
}

#endif // FEATURE_PERFTRACING
