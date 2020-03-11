// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "diagnosticsprotocol.h"
#include "diagnosticsipcfactory.h"

#ifdef FEATURE_PERFTRACING

IpcStream::DiagnosticsIpc *DiagnosticsIpcFactory::CreateServer(const char *const pIpcName, ErrorCallback callback)
{
    return IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::SERVER, callback);
}

IpcStream::DiagnosticsIpc *DiagnosticsIpcFactory::CreateClient(const char *const pIpcName, ErrorCallback callback)
{
    return IpcStream::DiagnosticsIpc::Create(pIpcName, IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT, callback);
}

IpcStream *DiagnosticsIpcFactory::GetNextConnectedStream(IpcStream::DiagnosticsIpc **pIpcs, uint32_t nIpcs, ErrorCallback callback)
{
    CQuickArrayList<IpcStream*> pStreams;
    for (uint64_t i = 0; i < nIpcs; i++)
    {
        if (pIpcs[i]->mode == IpcStream::DiagnosticsIpc::ConnectionMode::CLIENT)
        {
            // TODO: Should we loop here to ensure connection?
            pStreams.Push(pIpcs[i]->Connect(callback));
            if (pStreams[i] != nullptr)
            {
                uint8_t advertiseBuffer[18];
                if (!DiagnosticsIpc::PopulateIpcAdvertisePayload_V1(advertiseBuffer))
                {
                    if (callback != nullptr)
                        callback("Unable to generate Advertise Buffer", -1);
                    return nullptr;
                }

                uint32_t nBytesWritten = 0;
                if (!pStreams[i]->Write(advertiseBuffer, sizeof(advertiseBuffer), nBytesWritten))
                {
                    if (callback != nullptr)
                        callback("Unable to send Advertise message", -1);
                    return nullptr;
                }
                _ASSERTE(nBytesWritten == sizeof(advertiseBuffer));
            }
        }
        else
        {
            pStreams.Push(pIpcs[i]->Accept(false, callback));
        }

        if (pStreams[i] == nullptr)
        {
            if (callback != nullptr)
                callback("Unable to establish stream", -1);
            return nullptr;
        }
    }

    return IpcStream::Select(pStreams.Ptr(), nIpcs, callback);
}

#endif // FEATURE_PERFTRACING