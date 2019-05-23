// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTIC_PROTOCOL_HELPER_H__
#define __DIAGNOSTIC_PROTOCOL_HELPER_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"

class IpcStream;

class DiagnosticProtocolHelper
{
public:
    // IPC event handlers.
#ifdef FEATURE_PAL
    static void GenerateCoreDump(IpcStream *pStream); // `dotnet-dump collect`
#endif

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    static void AttachProfiler(IpcStream *pStream);
#endif // FEATURE_PROFAPI_ATTACH_DETACH

private:
    const static uint32_t IpcStreamReadBufferSize = 8192;
};

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTIC_PROTOCOL_HELPER_H__
