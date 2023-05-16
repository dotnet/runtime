// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

void EventPipeAdapter_Initialize() {}
bool EventPipeAdapter_Enabled() { return false; }

bool DiagnosticServerAdapter_Initialize() { return false; }
void DiagnosticServerAdapter_PauseForDiagnosticsMonitor() {}

void EventPipeAdapter_FinishInitialize() {}

void EventPipeAdapter_Shutdown() {}
bool DiagnosticServerAdapter_Shutdown() { return false; }
