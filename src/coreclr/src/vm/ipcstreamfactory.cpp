// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "diagnosticsprotocol.h"
#include "ipcstreamfactory.h"

#ifdef FEATURE_PERFTRACING

IpcStream **IpcStreamFactory::s_ppActiveConnectionsCache = nullptr;
uint32_t IpcStreamFactory::s_ActiveConnectionsCacheSize = 0;

IpcStream::DiagnosticsIpc *IpcStreamFactory::CreateServer(const char *const pIpcName, ErrorCallback callback)
{
    return IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::SERVER, callback);
}

IpcStream::DiagnosticsIpc *IpcStreamFactory::CreateClient(const char *const pIpcName, ErrorCallback callback)
{
    return IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT, callback);
}

IpcStream *IpcStreamFactory::GetNextAvailableStream(IpcStream::DiagnosticsIpc *const *const ppIpcs, uint32_t nIpcs, ErrorCallback callback)
{
    // a static array that holds open client connections that haven't been used
    // Remove entries from this list that have been used, e.g., they are placed in pStream and returned
    // This will prevent the runtime from continually reestablishing connection when the server loop loops.
    // This does, however, introduce state to this method which is undesireable, but a justifiable cost to minimizing system calls.

    if (s_ppActiveConnectionsCache == nullptr)
    {
        ResizeCache(nIpcs);
    }

    if (s_ActiveConnectionsCacheSize != nIpcs)
    {
        // number of connections has changed
        // (3/2020 - This isn't possible, but should be here for future proofing)
        ClearCache();
        ResizeCache(nIpcs);
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
        for (uint32_t i = 0; i < nIpcs; i++)
        {
            if (ppIpcs[i]->mode == IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT)
            {
                pollTimeoutMs = (pollTimeoutMs == -1) ? pollTimeoutMinMs : pollTimeoutMs;
                if (s_ppActiveConnectionsCache[i] != nullptr)
                {
                    // Check if the connection is still open by doing a 0 length read
                    // this should fail if the connection has been closed
                    // N.B.: this can race (connection closes between here and Poll)
                    //       but retry semantics means it shouldn't matter cause we'll
                    //       self-correct
                    uint32_t nBytesRead;
                    uint8_t buf[1];
                    if (s_ppActiveConnectionsCache[i]->Read(buf, 0, nBytesRead))
                    {
                        pStreams.Push(s_ppActiveConnectionsCache[i]);
                        continue;
                    }
                    else
                    {
                        delete s_ppActiveConnectionsCache[i];
                        s_ppActiveConnectionsCache[i] = nullptr;
                        pollTimeoutMs = pollTimeoutMinMs;
                    }
                }

                // loop here
                IpcStream *pConnection = nullptr;
                pConnection = ppIpcs[i]->Connect(callback);

                if (pConnection != nullptr)
                {
                    if (!DiagnosticsIpc::SendIpcAdvertise_V1(pConnection))
                    {
                        if (callback != nullptr)
                            callback("Failed to send advertise message", -1);
                        return nullptr;
                    }

                    // Add connection to cache
                    s_ppActiveConnectionsCache[i] = pConnection;
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
                IpcStream *pServer = ppIpcs[i]->Accept(false, callback);
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