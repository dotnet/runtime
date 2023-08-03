// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EVENTPIPE_INTERFACE_H
#define EVENTPIPE_INTERFACE_H

// Initialize EventPipe
void EventPipeAdapter_Initialize();

// Initialize DS
bool DiagnosticServerAdapter_Initialize();
void DiagnosticServerAdapter_PauseForDiagnosticsMonitor();


void EventPipeAdapter_FinishInitialize();

void EventPipe_ThreadShutdown();

void EventPipeAdapter_Shutdown();
bool DiagnosticServerAdapter_Shutdown();

void EventTracing_Initialize();
#endif //EVENTPIPE_INTERFACE_H