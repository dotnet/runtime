// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "diagnosticsprotocol.h"
#include "ipcstreamfactory.h"

#ifdef FEATURE_PERFTRACING

CQuickArrayList<IpcStream::DiagnosticsIpc*> IpcStreamFactory::s_rgpIpcs = CQuickArrayList<IpcStream::DiagnosticsIpc*>();
CQuickArray<IpcStream*> IpcStreamFactory::s_rgpActiveConnectionsCache = CQuickArray<IpcStream*>();

bool IpcStreamFactory::CreateServer(const char *const pIpcName, ErrorCallback callback)
{
    IpcStream::DiagnosticsIpc *pIpc = IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::SERVER, callback);
    if (pIpc != nullptr)
    {
        s_rgpIpcs.Push(pIpc);
        return true;
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
        s_rgpIpcs.Push(pIpc);
        return true;
    }
    else
    {
        return false;
    }
}

bool IpcStreamFactory::HasActiveConnections()
{
    return s_rgpIpcs.Size() > 0;
}

void IpcStreamFactory::CloseConnections()
{
    for (uint32_t i = 0; i < (uint32_t)s_rgpIpcs.Size(); i++)
    {
        IpcStream::DiagnosticsIpc *pIpc = s_rgpIpcs.Pop();
        if (pIpc != nullptr)
            delete pIpc;

        if (s_rgpActiveConnectionsCache[i] != nullptr)
            delete s_rgpActiveConnectionsCache[i];
    }
}

IpcStream *IpcStreamFactory::GetNextAvailableStream(ErrorCallback callback)
{
    // a static array that holds open client connections that haven't been used
    // Remove entries from this list that have been used, e.g., they are placed in pStream and returned
    // This will prevent the runtime from continually reestablishing connection when the server loop loops.
    // This does, however, introduce state to this method which is undesireable, but a justifiable cost to minimizing system calls.

    if (s_rgpActiveConnectionsCache == nullptr)
    {
        ResizeCache((uint32_t)s_rgpIpcs.Size());
    }

    if (s_rgpActiveConnectionsCache.Size() != s_rgpIpcs.Size())
    {
        // number of connections has changed
        // (3/2020 - This isn't possible, but should be here for future proofing)
        ClearCache();
        ResizeCache((uint32_t)s_rgpIpcs.Size());
    }

    IpcStream *pStream = nullptr;
    
    // Polling timeout semantics
    // If client connection is opted in
    //   and connection succeeds => set timeout to max
    //   and connection fails => set timeout to minimum and scale by falloff factor
    // else => set timeout to -1 (infinite)
    //
    // If an agent closes its socket while we're still connected,
    // the max timeout is the amount of time it will take for us to notice
    int32_t pollTimeoutFalloffFactor = 2;
    int32_t pollTimeoutMinMs = 250;
    int32_t pollTimeoutMs = -1;
    int32_t pollTimeoutMaxMs = 30000; // 30s
    uint32_t nPollAttempts = 0;

    while (pStream == nullptr)
    {
        CQuickArrayList<IpcStream*> pStreams;
        for (uint32_t i = 0; i < (uint32_t)s_rgpIpcs.Size(); i++)
        {
            if (s_rgpIpcs[i]->mode == IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT)
            {
                pollTimeoutMs = (pollTimeoutMs == -1) ? pollTimeoutMinMs : pollTimeoutMs;
                if (s_rgpActiveConnectionsCache[i] != nullptr)
                {
                    // Check if the connection is still open by doing a 0 length read
                    // this should fail if the connection has been closed
                    // N.B.: this can race (connection closes between here and Poll)
                    //       but retry semantics means it shouldn't matter cause we'll
                    //       self-correct
                    uint32_t nBytesRead;
                    uint8_t buf[1];
                    if (s_rgpActiveConnectionsCache[i]->Read(buf, 0, nBytesRead))
                    {
                        pStreams.Push(s_rgpActiveConnectionsCache[i]);
                        continue;
                    }
                    else
                    {
                        delete s_rgpActiveConnectionsCache[i];
                        s_rgpActiveConnectionsCache[i] = nullptr;
                        pollTimeoutMs = pollTimeoutMinMs;
                    }
                }

                // loop here
                IpcStream *pConnection = nullptr;
                pConnection = s_rgpIpcs[i]->Connect(callback);

                if (pConnection != nullptr)
                {
                    if (!DiagnosticsIpc::SendIpcAdvertise_V1(pConnection))
                    {
                        if (callback != nullptr)
                            callback("Failed to send advertise message", -1);
                        return nullptr;
                    }

                    // Add connection to cache
                    s_rgpActiveConnectionsCache[i] = pConnection;
                    pStreams.Push(pConnection);
                    pollTimeoutMs = pollTimeoutMaxMs;
                }
                else
                {
                    pollTimeoutMs = (pollTimeoutMs >= pollTimeoutMaxMs) ?
                        pollTimeoutMaxMs :
                        pollTimeoutMs * pollTimeoutFalloffFactor;
                }
            }
            else
            {
                IpcStream *pServer = s_rgpIpcs[i]->Accept(false, callback);
                if (pServer == nullptr)
                {
                    if (callback != nullptr)
                        callback("DiagnosticsServer failed to accept", -1);
                    return nullptr;
                }
                pStreams.Push(pServer);
            }
        }

        int32_t retval = IpcStream::Poll(pStreams.Ptr(), (uint32_t)pStreams.Size(), pollTimeoutMs, &pStream, callback);
        nPollAttempts++;
        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - Poll attempt: %d, timeout: %dms.\n", nPollAttempts, pollTimeoutMs);

        if (retval < 0)
        {
            if (pStream != nullptr)
            {
                STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::GetNextAvailableStream - Poll attempt: %d, connection hung up.\n", nPollAttempts);
                // This stream was hung up
                RemoveFromCache(pStream);
                delete pStream;
                pStream = nullptr;
                pollTimeoutMs = pollTimeoutMinMs;
                continue;
            }
        }
    }

    // Clean the Active Connection Cache of a used connection
    RemoveFromCache(pStream);
    return pStream;
}

#endif // FEATURE_PERFTRACING