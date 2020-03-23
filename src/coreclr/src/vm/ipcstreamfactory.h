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
    static IpcStream::DiagnosticsIpc *CreateServer(const char *const pIpcName, ErrorCallback = nullptr);
    static IpcStream::DiagnosticsIpc *CreateClient(const char *const pIpcName, ErrorCallback = nullptr);
    static IpcStream *GetNextAvailableStream(IpcStream::DiagnosticsIpc *const *const ppIpcs, uint32_t nIpcs, ErrorCallback = nullptr);
private:
    static IpcStream **s_ppActiveConnectionsCache;
    static uint32_t s_ActiveConnectionsCacheSize;

    static void ResizeCache(uint32_t size)
    {
        if (s_ppActiveConnectionsCache != nullptr)
            delete[] s_ppActiveConnectionsCache;

        s_ppActiveConnectionsCache = new IpcStream*[size];
        s_ActiveConnectionsCacheSize = size;
        memset(s_ppActiveConnectionsCache, 0, size * sizeof(IpcStream*));
    }

    static void RemoveFromCache(IpcStream *pStream)
    {
        for (uint32_t i = 0; i < s_ActiveConnectionsCacheSize; i++)
            if (s_ppActiveConnectionsCache[i] == pStream)
                s_ppActiveConnectionsCache[i] = nullptr;
    }

    static void ClearCache()
    {
        for (uint32_t i = 0; i < s_ActiveConnectionsCacheSize; i++)
        {
            if (s_ppActiveConnectionsCache[i] != nullptr)
            {
                delete s_ppActiveConnectionsCache[i];
                s_ppActiveConnectionsCache[i] = nullptr;
            }
        }
    }
};

#endif // FEATURE_PERFTRACING

#endif // __IPC_STREAM_FACTORY_H__ 