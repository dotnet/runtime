// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTIC_SERVER_H__
#define __DIAGNOSTIC_SERVER_H__

#include <stdint.h>

#ifdef FEATURE_PERFTRACING // This macro should change to something more generic than performance.

//! TODO: Temp class.
enum class DiagnosticMessageType : uint32_t
{
    ///////////////////////////////////////////////////////////////////////////
    // Debug = 0

    ///////////////////////////////////////////////////////////////////////////
    // EventPipe
    EnableEventPipe = 1024,
    DisableEventPipe,

    // TODO: Define what else is available on the out-of-proc interface?
    // GetSessionInfo,
    // CreateProvider,
    // DefineEvent,
    // GetProvider,
    // DeleteProvider,
    // EventActivityIdControl,
    // WriteEvent,
    // WriteEventData,
    // GetNextEvent,

    ///////////////////////////////////////////////////////////////////////////
    // Profiler = 2048
};

//! TODO: Temp class.
struct MessageHeader
{
    DiagnosticMessageType RequestType;
    uint32_t Pid;
};

//! Defines an implementation of a IPC handler that dispatches messages to the runtime.
class DiagnosticServer final
{
public:
    //! Initialize the event pipe (Creates the EventPipe IPC server).
    static bool Initialize();

    //! Shutdown the event pipe.
    static bool Shutdown();
};

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTIC_SERVER_H__
