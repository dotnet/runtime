// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DIAGNOSTIC_SERVER_H__
#define __DIAGNOSTIC_SERVER_H__

#ifdef FEATURE_PERFTRACING // This macro should change to something more generic than performance.

#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

//! Defines an implementation of a IPC handler that dispatches messages to the runtime.
class DiagnosticServer final
{
public:
    // Initialize the event pipe (Creates the EventPipe IPC server).
    static bool Initialize();

    // Shutdown the event pipe.
    static bool Shutdown();

    // Diagnostics server thread.
    static DWORD WINAPI DiagnosticsServerThread(LPVOID lpThreadParameter);

    // Pauses EEStartup after the Diagnostics Server has been started
    // allowing a Diagnostics Monitor to attach perform tasks before
    // Startup is completed
    NOINLINE static void PauseForDiagnosticsMonitor();

    // Sets CLREvent to resume startup in EEStartupHelper (see: ceemain.cpp after DiagnosticServer::Initialize() for pausing point)
    // This is a no-op if not configured to pause or runtime has already resumed
    static void ResumeRuntimeStartup();

private:
    static Volatile<bool> s_shuttingDown;
    static CLREventStatic *s_ResumeRuntimeStartupEvent;
};

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTIC_SERVER_H__
