// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "diagnosticsprotocol.h"
#include "ipcstreamfactory.h"

#ifdef FEATURE_PERFTRACING

CQuickArrayList<IpcStream::IpcPollHandle> IpcStreamFactory::s_rgIpcPollHandles = CQuickArrayList<IpcStream::IpcPollHandle>();

bool IpcStreamFactory::CreateServer(const char *const pIpcName, ErrorCallback callback)
{
    IpcStream::DiagnosticsIpc *pIpc = IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::SERVER, callback);
    if (pIpc != nullptr)
    {
        if (pIpc->Listen(callback))
        {
            IpcStream::IpcPollHandle ipcPollHandle { .pIpc = pIpc, .pStream = nullptr, .revents = 0 };
            s_rgIpcPollHandles.Push(ipcPollHandle);
            // s_rgpIpcs.Push(pIpc);
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
        IpcStream::IpcPollHandle ipcPollHandle { .pIpc = pIpc, .pStream = nullptr, .revents = 0 };
        s_rgIpcPollHandles.Push(ipcPollHandle);
        // s_rgpIpcs.Push(pIpc);
        return true;
    }
    else
    {
        return false;
    }
}

bool IpcStreamFactory::HasActiveConnections()
{
    return s_rgIpcPollHandles.Size() > 0;
}

void IpcStreamFactory::CloseConnections()
{
    while (s_rgIpcPollHandles.Size() > 0)
    {
        IpcStream::IpcPollHandle ipcPollHandle = s_rgIpcPollHandles.Pop();
        if (ipcPollHandle.pStream != nullptr)
            delete ipcPollHandle.pStream;
        if (ipcPollHandle.pIpc != nullptr)
            delete ipcPollHandle.pIpc;
    }
}

IpcStream *IpcStreamFactory::GetNextAvailableStream(ErrorCallback callback)
{
    IpcStream *pStream = nullptr;
    // View of s_rgIpcPollhandles
    CQuickArrayList<IpcStream::IpcPollHandle*> pIpcPollHandles;
    
    // Polling timeout semantics
    // If client connection is opted in
    //   and connection succeeds => set timeout to infinite
    //   and connection fails => set timeout to minimum and scale by falloff factor
    // else => set timeout to -1 (infinite)
    //
    // If an agent closes its socket while we're still connected,
    // Poll will return and let us know which connection hung up
    int32_t pollTimeoutFalloffFactor = 2;
    int32_t pollTimeoutInfinite = -1;
    int32_t pollTimeoutMinMs = 250;
    int32_t pollTimeoutMs = pollTimeoutInfinite;
    int32_t pollTimeoutMaxMs = 30000; // 30s
    uint32_t nPollAttempts = 0;

    while (pStream == nullptr)
    {
        for (uint32_t i = 0; i < (uint32_t)s_rgIpcPollHandles.Size(); i++)
        {
            if (s_rgIpcPollHandles[i].pIpc->mode == IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT)
            {
                pollTimeoutMs = (pollTimeoutMs == pollTimeoutInfinite) ? pollTimeoutMinMs : pollTimeoutMs;
                if (s_rgIpcPollHandles[i].pStream == nullptr)
                {
                    // cache is empty, reconnect
                    IpcStream *pConnection = nullptr;
                    pConnection = s_rgIpcPollHandles[i].pIpc->Connect(callback);

                    if (pConnection != nullptr)
                    {
                        if (!DiagnosticsIpc::SendIpcAdvertise_V1(pConnection))
                        {
                            if (callback != nullptr)
                                callback("Failed to send advertise message", -1);
                            delete pConnection;
                            return nullptr;
                        }

                        // Add connection to cache
                        s_rgIpcPollHandles[i].pStream = pConnection;
                        pollTimeoutMs = pollTimeoutInfinite;
                        pIpcPollHandles.Push(&s_rgIpcPollHandles[i]);
                    }
                    else
                    {
                        pollTimeoutMs = (pollTimeoutMs >= pollTimeoutMaxMs) ?
                            pollTimeoutMaxMs :
                            pollTimeoutMs * pollTimeoutFalloffFactor;
                    }
                }
            }
            else
            {
                bool fSuccess = s_rgIpcPollHandles[i].pIpc->Listen();
                if (!fSuccess)
                {
                    // TODO: error check the server failing to listen
                }
                pIpcPollHandles.Push(&s_rgIpcPollHandles[i]);
            }
        }

        int32_t retval = IpcStream::DiagnosticsIpc::Poll(pIpcPollHandles.Ptr(), (uint32_t)pIpcPollHandles.Size(), pollTimeoutMs, callback);
        nPollAttempts++;
        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - Poll attempt: %d, timeout: %dms.\n", nPollAttempts, pollTimeoutMs);

        if (retval != 0)
        {
            for (uint32_t i = 0; i < (uint32_t)pIpcPollHandles.Size(); i++)
            {
                switch ((IpcStream::PollEvents)pIpcPollHandles[i]->revents)
                {
                    case IpcStream::PollEvents::HANGUP:
                        delete pIpcPollHandles[i]->pStream;
                        pIpcPollHandles[i]->pStream = nullptr; // clear the cache of the hung up connection; will trigger a reconnect poll
                        STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - Poll attempt: %d, connection hung up.\n", nPollAttempts);
                        pollTimeoutMs = pollTimeoutMinMs;
                        break;
                    case IpcStream::PollEvents::SIGNALED:
                        if (pStream == nullptr) // only use first signaled stream; will get others on subsequent calls
                        {
                            pStream = pIpcPollHandles[i]->pStream;
                            pIpcPollHandles[i]->pStream = nullptr; // pass ownership to caller so we aren't caching the connection anymore
                        }
                        break;
                    case IpcStream::PollEvents::ERR:
                    default:
                        // TODO: Error handling
                        break;
                }
            }
        }

        // clear the view
        while (pIpcPollHandles.Size() > 0)
            pIpcPollHandles.Pop();
    }

    // Clean the Active Connection Cache of a used connection
    return pStream;
}

#endif // FEATURE_PERFTRACING