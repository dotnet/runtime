// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_PROTOCOL_HELPER_H__
#define __EVENTPIPE_PROTOCOL_HELPER_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "eventpipe.h"

class IpcStream;

class EventPipeProtocolHelper
{
public:
    // IPC event handlers.
    static void StopTracing(IpcStream *pStream);
    static void CollectTracing(IpcStream *pStream); // `dotnet-trace collect`

private:
    const static uint32_t DefaultCircularBufferMB = 1024; // 1 GB
    const static uint32_t DefaultProfilerSamplingRateInNanoseconds = 1000000; // 1 msec.
    const static uint32_t IpcStreamReadBufferSize = 8192;

    static bool TryParseProviderConfiguration(uint8_t *&bufferCursor, uint32_t &bufferLen, CQuickArray<EventPipeProviderConfiguration> &result);
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROTOCOL_HELPER_H__
