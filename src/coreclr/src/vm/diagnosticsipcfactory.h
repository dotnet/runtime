// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTICS_IPC_FACTORY_H__
#define __DIAGNOSTICS_IPC_FACTORY_H__

#ifdef FEATURE_PERFTRACING

#include "diagnosticsipc.h"

class DiagnosticsIpcFactory
{
public:
    static IpcStream::DiagnosticsIpc *CreateServer(const char *const pIpcName, ErrorCallback = nullptr);
    static IpcStream::DiagnosticsIpc *CreateClient(const char *const pIpcName, ErrorCallback = nullptr);
    static IpcStream *GetNextAvailableStream(IpcStream::DiagnosticsIpc **ppIpcs, uint32_t nIpcs, ErrorCallback = nullptr);
private:
    static IpcStream **s_ppActiveConnections;
};

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTICS_IPC_FACTORY_H__ 