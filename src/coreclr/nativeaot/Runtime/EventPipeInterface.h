// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPE_INTERFACE_H__
#define __EVENTPIPE_INTERFACE_H__

// Initialize EventPipe
void EventPipeAdapter_Initialize();
bool EventPipeAdapter_Enabled();

// Initialize DS
bool DiagnosticServerAdapter_Initialize();
void DiagnosticServerAdapter_PauseForDiagnosticsMonitor();


void EventPipeAdapter_FinishInitialize();

void EventPipeAdapter_Shutdown();
bool DiagnosticServerAdapter_Shutdown();

#endif //__EVENTPIPE_INTERFACE_H__