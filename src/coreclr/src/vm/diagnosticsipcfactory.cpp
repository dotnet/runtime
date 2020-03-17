// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "diagnosticsprotocol.h"
#include "diagnosticsipcfactory.h"

#ifdef FEATURE_PERFTRACING

IpcStream **DiagnosticsIpcFactory::s_ppActiveConnections = nullptr;

IpcStream::DiagnosticsIpc *DiagnosticsIpcFactory::CreateServer(const char *const pIpcName, ErrorCallback callback)
{
    return IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::SERVER, callback);
}

IpcStream::DiagnosticsIpc *DiagnosticsIpcFactory::CreateClient(const char *const pIpcName, ErrorCallback callback)
{
    return IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT, callback);
}

// TODO: log info on looping counts
IpcStream *DiagnosticsIpcFactory::GetNextAvailableStream(IpcStream::DiagnosticsIpc *const *const ppIpcs, uint32_t nIpcs, ErrorCallback callback)
{
    // a static array that holds open client connections that haven't been used
    // Remove entries from this list that have been used, e.g., they are placed in pStream and returned
    // This will prevent the runtime from continually reestablishing connection when the server loop loops.
    // This does, however, introduce state to this method which is undesireable, but a justifiable cost to minimizing system calls.

    if (s_ppActiveConnections == nullptr)
    {
        s_ppActiveConnections = new IpcStream*[nIpcs];
        memset(s_ppActiveConnections, 0, nIpcs * sizeof(IpcStream*));
    }

    // when we get a connection, put it in this list.  If we use that connection, remove it.
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
                if (s_ppActiveConnections[i] != nullptr)
                {
                    // check if still usable and then push it
                    // s_ppActiveConnections[i]->IsConnected(); ????
                    pStreams.Push(s_ppActiveConnections[i]);
                    continue;
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
                        // TODO: Should we just fall through instead and ignore the client conn?
                        return nullptr;
                    }

                    // Add connection to list
                    s_ppActiveConnections[i] = pConnection;
                    pStreams.Push(pConnection);
                    pollTimeoutMs = pollTimeoutMaxMs;
                }
                else
                {
                    pollTimeoutMs = (pollTimeoutMs > pollTimeoutMaxMs) ?
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

        if (retval < 0)
        {
            if (pStream != nullptr)
            {
                // This stream was hung up
                for (uint32_t i = 0; i < nIpcs; i++)
                {
                    if (s_ppActiveConnections[i] == pStream)
                    {
                        s_ppActiveConnections[i] = nullptr;
                        delete pStream;
                        pStream = nullptr;
                    }
                }
                continue;
            }

            // TODO: error handle here?
        }
    }

    // Clean the Active Connection Cache of a used connection
    for (uint32_t i = 0; i < nIpcs; i++)
        if (s_ppActiveConnections[i] == pStream)
            s_ppActiveConnections[i] = nullptr;
    return pStream;
}

#endif // FEATURE_PERFTRACING