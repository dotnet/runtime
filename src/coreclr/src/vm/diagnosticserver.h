// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTIC_SERVER_H__
#define __DIAGNOSTIC_SERVER_H__

#ifdef FEATURE_PERFTRACING // This macro should change to something more generic than performance.

#include <stdint.h>
#include "diagnosticsipc.h"

//! TODO: Temp class.
enum class DiagnosticMessageType : uint32_t
{
    ///////////////////////////////////////////////////////////////////////////
    // Debug = 0
    GenerateCoreDump = 1,           // Initiates core dump generation

    ///////////////////////////////////////////////////////////////////////////
    // EventPipe = 1024
    StartEventPipeTracing = 1024,   // To file
    StopEventPipeTracing,
    CollectEventPipeTracing,        // To IPC

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

private:
    static IpcStream::DiagnosticsIpc *s_pIpc;
};

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTIC_SERVER_H__
