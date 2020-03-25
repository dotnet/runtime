// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __IPC_STREAM_FACTORY_H__
#define __IPC_STREAM_FACTORY_H__

#ifdef FEATURE_PERFTRACING

#include "diagnosticsipc.h"

class IpcStreamFactory
{
public:
    static bool CreateServer(const char *const pIpcName, ErrorCallback = nullptr);
    static bool CreateClient(const char *const pIpcName, ErrorCallback = nullptr);
    static IpcStream *GetNextAvailableStream(ErrorCallback = nullptr);
    static bool HasActiveConnections();
    static void CloseConnections();
private:
    static CQuickArrayList<IpcStream::DiagnosticsIpc::IpcPollHandle> s_rgIpcPollHandles;
};

#endif // FEATURE_PERFTRACING

#endif // __IPC_STREAM_FACTORY_H__ 