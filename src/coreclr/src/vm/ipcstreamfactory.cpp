// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "diagnosticsprotocol.h"
#include "ipcstreamfactory.h"

#ifdef FEATURE_PERFTRACING

CQuickArrayList<IpcStreamFactory::ConnectionState*> IpcStreamFactory::s_rgpConnectionStates = CQuickArrayList<IpcStreamFactory::ConnectionState*>();
Volatile<bool> IpcStreamFactory::s_isShutdown = false;

bool IpcStreamFactory::ClientConnectionState::GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback)
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

IpcStream *IpcStreamFactory::ClientConnectionState::GetConnectedStream(ErrorCallback callback)
{
    IpcStream *pStream = _pStream;
    _pStream = nullptr;
    return pStream;
}

void IpcStreamFactory::ClientConnectionState::Reset(ErrorCallback callback)
{
    delete _pStream;
    _pStream = nullptr;
}

bool IpcStreamFactory::ServerConnectionState::GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback)
{
    *pIpcPollHandle = { _pIpc, nullptr, 0, this };
    return true;
}

IpcStream *IpcStreamFactory::ServerConnectionState::GetConnectedStream(ErrorCallback callback)
{
    return _pIpc->Accept(callback);
}

// noop for server
void IpcStreamFactory::ServerConnectionState::Reset(ErrorCallback)
{
    return;
}

bool IpcStreamFactory::CreateServer(const char *const pIpcName, ErrorCallback callback)
{
    IpcStream::DiagnosticsIpc *pIpc = IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::SERVER, callback);
    if (pIpc != nullptr)
    {
        if (pIpc->Listen(callback))
        {
            s_rgpConnectionStates.Push(new ServerConnectionState(pIpc));
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

bool IpcStreamFactory::CreateClient(const char *const pIpcName, ErrorCallback callback)
{
    IpcStream::DiagnosticsIpc *pIpc = IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT, callback);
    if (pIpc != nullptr)
    {
        s_rgpConnectionStates.Push(new ClientConnectionState(pIpc));
        return true;
    }
    else
    {
        return false;
    }
}

bool IpcStreamFactory::HasActiveConnections()
{
    return !s_isShutdown && s_rgpConnectionStates.Size() > 0;
}

void IpcStreamFactory::CloseConnections(ErrorCallback callback)
{
    for (uint32_t i = 0; i < (uint32_t)s_rgpConnectionStates.Size(); i++)
        s_rgpConnectionStates[i]->Close(callback);
}

void IpcStreamFactory::Shutdown(ErrorCallback callback)
{
    if (s_isShutdown)
        return;
    s_isShutdown = true;
    for (uint32_t i = 0; i < (uint32_t)s_rgpConnectionStates.Size(); i++)
        s_rgpConnectionStates[i]->Close(true, callback);
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
        for (uint32_t i = 0; i < (uint32_t)s_rgpConnectionStates.Size(); i++)
        {
            IpcStream::DiagnosticsIpc::IpcPollHandle pollHandle = {};
            if (s_rgpConnectionStates[i]->GetIpcPollHandle(&pollHandle, callback))
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
                        ((ConnectionState*)(rgIpcPollHandles[i].pUserData))->Reset(callback);
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - HUP :: Poll attempt: %d, connection %d hung up.\n", nPollAttempts, i);
                        pollTimeoutMs = s_pollTimeoutMinMs;
                        break;
                    case IpcStream::DiagnosticsIpc::PollEvents::SIGNALED:
                        if (pStream == nullptr) // only use first signaled stream; will get others on subsequent calls
                            pStream = ((ConnectionState*)(rgIpcPollHandles[i].pUserData))->GetConnectedStream(callback);
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - SIG :: Poll attempt: %d, connection %d signalled.\n", nPollAttempts, i);
                        break;
                    case IpcStream::DiagnosticsIpc::PollEvents::ERR:
                        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - ERR :: Poll attempt: %d, connection %d errored.\n", nPollAttempts, i);
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
            return nullptr;

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
