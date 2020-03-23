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
    static CQuickArrayList<IpcStream::DiagnosticsIpc*> s_rgpIpcs;
    static CQuickArray<IpcStream*> s_rgpActiveConnectionsCache;

    static void ResizeCache(uint32_t size)
    {
        if (s_rgpActiveConnectionsCache != nullptr)
            ClearCache();

        // s_ppActiveConnectionsCache = new IpcStream*[size];
        // s_ActiveConnectionsCacheSize = size;
        // memset(s_ppActiveConnectionsCache, 0, size * sizeof(IpcStream*));
        s_rgpActiveConnectionsCache.ReSizeThrows(size);
    }

    static void RemoveFromCache(IpcStream *pStream)
    {
        for (uint32_t i = 0; i < (uint32_t)s_rgpActiveConnectionsCache.Size(); i++)
            if (s_rgpActiveConnectionsCache[i] == pStream)
                s_rgpActiveConnectionsCache[i] = nullptr;
    }

    static void ClearCache()
    {
        for (uint32_t i = 0; i < (uint32_t)s_rgpActiveConnectionsCache.Size(); i++)
        {
            if (s_rgpActiveConnectionsCache[i] != nullptr)
            {
                delete s_rgpActiveConnectionsCache[i];
                s_rgpActiveConnectionsCache[i] = nullptr;
            }
        }
    }
};

#endif // FEATURE_PERFTRACING

#endif // __IPC_STREAM_FACTORY_H__ 