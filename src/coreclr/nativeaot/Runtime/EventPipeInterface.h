// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EVENTPIPE_INTERFACE_H
#define EVENTPIPE_INTERFACE_H

// Initialize EventPipe
void EventPipe_Initialize();

// Initialize DS
bool DiagnosticServer_Initialize();
void DiagnosticServer_PauseForDiagnosticsMonitor();

void EventPipe_FinishInitialize();

void EventPipe_ThreadShutdown();

void EventPipe_Shutdown();
bool DiagnosticServer_Shutdown();

void EventTracing_Initialize();
#endif //EVENTPIPE_INTERFACE_H