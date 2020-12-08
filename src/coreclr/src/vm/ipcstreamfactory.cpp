// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "diagnosticsprotocol.h"
#include "ipcstreamfactory.h"

#ifdef FEATURE_PERFTRACING

CQuickArrayList<IpcStreamFactory::DiagnosticPort*> IpcStreamFactory::s_rgpDiagnosticPorts = CQuickArrayList<IpcStreamFactory::DiagnosticPort*>();
Volatile<bool> IpcStreamFactory::s_isShutdown = false;
IpcStreamFactory::DiagnosticPort *IpcStreamFactory::s_currentPort = nullptr;

CQuickArrayList<LPSTR> split(LPSTR string, LPCSTR delimiters)
{
    CQuickArrayList<LPSTR> parts;
    char *context;
    char *part = nullptr;
    for (char *cursor = string; ; cursor = nullptr)
    {
        if ((part = strtok_s(cursor, delimiters, &context)) != nullptr)
            parts.Push(part);
        else
            break;
    }

    return parts;
}

bool IsWhitespace(char c)
{
    return (c == ' ' || c == '\r' || c == '\t' || c == '\n');
}

bool IsEmpty(LPCSTR string)
{
    uint32_t len = static_cast<uint32_t>(strlen(string));
    for (uint32_t i = 0; i < len; i++)
        if (!IsWhitespace(string[i]))
            return false;
    return true;
}

bool IpcStreamFactory::ConnectDiagnosticPort::GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback)
{
    STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO1000, "IpcStreamFactory::ClientConnectionState::GetIpcPollHandle - ENTER.\n");
    if (_pStream == nullptr)
    {
        STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::ClientConnectionState::GetIpcPollHandle - cache was empty!\n");
        // cache is empty, reconnect, e.g., there was a disconnect
        IpcStream *pConnection = _pIpc->Connect(callback);
        if (pConnection == nullptr)
        {
            if (callback != nullptr)
                callback("Failed to connect to client connection", -1);
            return false;
        }
#ifdef TARGET_UNIX
        STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::ClientConnectionState::GetIpcPollHandle - returned connection { _clientSocket = %d }\n", pConnection->_clientSocket);
#else
        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::ClientConnectionState::GetIpcPollHandle - returned connection { _hPipe = %d, _oOverlap.hEvent = %d }\n", pConnection->_hPipe, pConnection->_oOverlap.hEvent);
#endif
        if (!DiagnosticsIpc::SendIpcAdvertise_V1(pConnection))
        {
            if (callback != nullptr)
                callback("Failed to send advertise message", -1);
            delete pConnection;
            return false;
        }

        _pStream = pConnection;
    }
    *pIpcPollHandle = { nullptr, _pStream, 0, this };
    STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::ClientConnectionState::GetIpcPollHandle - EXIT.\n");
    return true;
}

IpcStream *IpcStreamFactory::ConnectDiagnosticPort::GetConnectedStream(ErrorCallback callback)
{
    IpcStream *pStream = _pStream;
    _pStream = nullptr;
    return pStream;
}

void IpcStreamFactory::ConnectDiagnosticPort::Reset(ErrorCallback callback)
{
    delete _pStream;
    _pStream = nullptr;
}

bool IpcStreamFactory::ListenDiagnosticPort::GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback)
{
    *pIpcPollHandle = { _pIpc, nullptr, 0, this };
    return true;
}

IpcStream *IpcStreamFactory::ListenDiagnosticPort::GetConnectedStream(ErrorCallback callback)
{
    return _pIpc->Accept(callback);
}

// noop for server
void IpcStreamFactory::ListenDiagnosticPort::Reset(ErrorCallback)
{
    return;
}

bool IpcStreamFactory::Configure(ErrorCallback callback)
{
    bool fSuccess = true;

    NewArrayHolder<char> dotnetDiagnosticPorts = nullptr;
    CLRConfigStringHolder dotnetDiagnosticPortsW = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DOTNET_DiagnosticPorts);
    int nCharactersWritten = 0;
    if (dotnetDiagnosticPortsW != nullptr)
    {
        nCharactersWritten = WideCharToMultiByte(CP_UTF8, 0, dotnetDiagnosticPortsW, -1, NULL, 0, NULL, NULL);
        dotnetDiagnosticPorts = new char[nCharactersWritten];
        nCharactersWritten = WideCharToMultiByte(CP_UTF8, 0, dotnetDiagnosticPortsW, -1, dotnetDiagnosticPorts, nCharactersWritten, NULL, NULL);
        ASSERT(nCharactersWritten != 0);

        CQuickArrayList<LPSTR> portConfigs = split(dotnetDiagnosticPorts, ";");
        while (portConfigs.Size() > 0)
        {
            LPSTR portConfig = portConfigs.Pop();
            STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::Configure - Attempted to create Diagnostic Port from \"%s\".\n", portConfig);
            CQuickArrayList<LPSTR> portConfigParts = split(portConfig, ",");
            DiagnosticPortBuilder builder;

            if (portConfigParts.Size() == 0)
            {
                fSuccess &= false;
                continue;
            }

            while (portConfigParts.Size() > 1)
                builder.WithTag(portConfigParts.Pop());
            builder.WithPath(portConfigParts.Pop());

            if (IsEmpty(builder.Path))
            {
                STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::Configure - Ignoring port configuration with empty address\n");
                continue;
            }

            // Ignore listen type (see conversation in https://github.com/dotnet/runtime/pull/40499 for details)
            if (builder.Type == DiagnosticPortType::LISTEN)
            {
                STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::Configure - Ignoring LISTEN port configuration \n");
                continue;
            }

            const bool fBuildSuccess = BuildAndAddPort(builder, callback);
            STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::Configure - Diagnostic Port creation succeeded? %d \n", fBuildSuccess);
            fSuccess &= fBuildSuccess;
        }
    }

    // create the default listen port
    DWORD dotnetDiagnosticPortSuspend = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DOTNET_DefaultDiagnosticPortSuspend);
    DiagnosticPortBuilder defaultListenPortBuilder = DiagnosticPortBuilder{}
        .WithPath(nullptr)
        .WithSuspendMode(dotnetDiagnosticPortSuspend > 0 ? DiagnosticPortSuspendMode::SUSPEND : DiagnosticPortSuspendMode::NOSUSPEND)
        .WithType(DiagnosticPortType::LISTEN);
    

    fSuccess &= BuildAndAddPort(defaultListenPortBuilder, callback);
    return fSuccess;
}

bool IpcStreamFactory::BuildAndAddPort(IpcStreamFactory::DiagnosticPortBuilder builder, ErrorCallback callback)
{
    if (builder.Type == DiagnosticPortType::LISTEN)
    {
        IpcStream::DiagnosticsIpc *pIpc = IpcStream::DiagnosticsIpc::Create(builder.Path, IpcStream::DiagnosticsIpc::ConnectionMode::LISTEN, callback);
        if (pIpc != nullptr)
        {
            if (pIpc->Listen(callback))
            {
                s_rgpDiagnosticPorts.Push(new ListenDiagnosticPort(pIpc, builder));
                return true;
            }
            else
            {
                delete pIpc;
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    else if (builder.Type == DiagnosticPortType::CONNECT)
    {
        IpcStream::DiagnosticsIpc *pIpc = IpcStream::DiagnosticsIpc::Create(builder.Path, IpcStream::DiagnosticsIpc::ConnectionMode::CONNECT, callback);
        if (pIpc != nullptr)
        {
            s_rgpDiagnosticPorts.Push(new ConnectDiagnosticPort(pIpc, builder));
            return true;
        }
        else
        {
            return false;
        }
    }
    return false;
}

bool IpcStreamFactory::HasActivePorts()
{
    return !s_isShutdown && s_rgpDiagnosticPorts.Size() > 0;
}

void IpcStreamFactory::ClosePorts(ErrorCallback callback)
{
    for (uint32_t i = 0; i < (uint32_t)s_rgpDiagnosticPorts.Size(); i++)
        s_rgpDiagnosticPorts[i]->Close(callback);
}

void IpcStreamFactory::Shutdown(ErrorCallback callback)
{
    if (s_isShutdown)
        return;
    s_isShutdown = true;
    for (uint32_t i = 0; i < (uint32_t)s_rgpDiagnosticPorts.Size(); i++)
        s_rgpDiagnosticPorts[i]->Close(true, callback);
}

bool IpcStreamFactory::AnySuspendedPorts()
{
    bool fAnySuspendedPorts = false;
    for (uint32_t i = 0; i < (uint32_t)s_rgpDiagnosticPorts.Size(); i++)
        fAnySuspendedPorts |= !(s_rgpDiagnosticPorts[i]->SuspendMode == DiagnosticPortSuspendMode::NOSUSPEND || s_rgpDiagnosticPorts[i]->HasResumedRuntime);
    return fAnySuspendedPorts;
}

void IpcStreamFactory::ResumeCurrentPort()
{
    if (s_currentPort != nullptr)
        s_currentPort->HasResumedRuntime = true;
}

// helper function for getting timeout
int32_t IpcStreamFactory::GetNextTimeout(int32_t currentTimeoutMs)
{
    if (currentTimeoutMs == s_pollTimeoutInfinite)
    {
        return s_pollTimeoutMinMs;
    }
    else
    {
        return (currentTimeoutMs >= s_pollTimeoutMaxMs) ?
                    s_pollTimeoutMaxMs :
                    (int32_t)((float)currentTimeoutMs * s_pollTimeoutFalloffFactor);
    }
}

IpcStream *IpcStreamFactory::GetNextAvailableStream(ErrorCallback callback)
{
    STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - ENTER");
    IpcStream *pStream = nullptr;
    CQuickArrayList<IpcStream::DiagnosticsIpc::IpcPollHandle> rgIpcPollHandles;

    int32_t pollTimeoutMs = s_pollTimeoutInfinite;
    bool fConnectSuccess = true;
    uint32_t nPollAttempts = 0;

    while (pStream == nullptr)
    {
        fConnectSuccess = true;
        for (uint32_t i = 0; i < (uint32_t)s_rgpDiagnosticPorts.Size(); i++)
        {
            IpcStream::DiagnosticsIpc::IpcPollHandle pollHandle = {};
            if (s_rgpDiagnosticPorts[i]->GetIpcPollHandle(&pollHandle, callback))
            {
                rgIpcPollHandles.Push(pollHandle);
            }
            else
            {
                fConnectSuccess = false;
            }
        }

        pollTimeoutMs = fConnectSuccess ?
            s_pollTimeoutInfinite :
            GetNextTimeout(pollTimeoutMs);

        nPollAttempts++;
        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - Poll attempt: %d, timeout: %dms.\n", nPollAttempts, pollTimeoutMs);
        for (uint32_t i = 0; i < rgIpcPollHandles.Size(); i++)
        {
            if (rgIpcPollHandles[i].pIpc != nullptr)
            {
#ifdef TARGET_UNIX
                STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "\tSERVER IpcPollHandle[%d] = { _serverSocket = %d }\n", i, rgIpcPollHandles[i].pIpc->_serverSocket);
#else
                STRESS_LOG3(LF_DIAGNOSTICS_PORT, LL_INFO10, "\tSERVER IpcPollHandle[%d] = { _hPipe = %d, _oOverlap.hEvent = %d }\n", i, rgIpcPollHandles[i].pIpc->_hPipe, rgIpcPollHandles[i].pIpc->_oOverlap.hEvent);
#endif
            }
            else
            {
#ifdef TARGET_UNIX
                STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "\tCLIENT IpcPollHandle[%d] = { _clientSocket = %d }\n", i, rgIpcPollHandles[i].pStream->_clientSocket);
#else
                STRESS_LOG3(LF_DIAGNOSTICS_PORT, LL_INFO10, "\tCLIENT IpcPollHandle[%d] = { _hPipe = %d, _oOverlap.hEvent = %d }\n", i, rgIpcPollHandles[i].pStream->_hPipe, rgIpcPollHandles[i].pStream->_oOverlap.hEvent);
#endif
            }
        }
        int32_t retval = IpcStream::DiagnosticsIpc::Poll(rgIpcPollHandles.Ptr(), (uint32_t)rgIpcPollHandles.Size(), pollTimeoutMs, callback);
        bool fSawError = false;

        if (retval != 0)
        {
            for (uint32_t i = 0; i < (uint32_t)rgIpcPollHandles.Size(); i++)
            {
                switch ((IpcStream::DiagnosticsIpc::PollEvents)rgIpcPollHandles[i].revents)
                {
                    case IpcStream::DiagnosticsIpc::PollEvents::HANGUP:
                        ((DiagnosticPort*)(rgIpcPollHandles[i].pUserData))->Reset(callback);
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - HUP :: Poll attempt: %d, connection %d hung up. Connect is reset.\n", nPollAttempts, i);
                        pollTimeoutMs = s_pollTimeoutMinMs;
                        break;
                    case IpcStream::DiagnosticsIpc::PollEvents::SIGNALED:
                        if (pStream == nullptr) // only use first signaled stream; will get others on subsequent calls
                        {
                            pStream = ((DiagnosticPort*)(rgIpcPollHandles[i].pUserData))->GetConnectedStream(callback);
                            if (pStream == nullptr)
                            {
                                fSawError = true;
                            }
                            s_currentPort = (DiagnosticPort*)(rgIpcPollHandles[i].pUserData);
                        }
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - SIG :: Poll attempt: %d, connection %d signalled.\n", nPollAttempts, i);
                        break;
                    case IpcStream::DiagnosticsIpc::PollEvents::ERR:
                        ((DiagnosticPort*)(rgIpcPollHandles[i].pUserData))->Reset(callback);
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - ERR :: Poll attempt: %d, connection %d errored. Connection is reset.\n", nPollAttempts, i);
                        fSawError = true;
                        break;
                    case IpcStream::DiagnosticsIpc::PollEvents::NONE:
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - NON :: Poll attempt: %d, connection %d had no events.\n", nPollAttempts, i);
                        break;
                    default:
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - UNK :: Poll attempt: %d, connection %d had invalid PollEvent.\n", nPollAttempts, i);
                        fSawError = true;
                        break;
                }
            }
        }


        if (pStream == nullptr && fSawError)
        {
            s_currentPort = nullptr;
            return nullptr;
        }

        // clear the view
        while (rgIpcPollHandles.Size() > 0)
            rgIpcPollHandles.Pop();
    }

#ifdef TARGET_UNIX
    STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - EXIT :: Poll attempt: %d, stream using handle %d.\n", nPollAttempts, pStream->_clientSocket);
#else
    STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - EXIT :: Poll attempt: %d, stream using handle %d.\n", nPollAttempts, pStream->_hPipe);
#endif
    return pStream;
}

#endif // FEATURE_PERFTRACING
