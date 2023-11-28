// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

void EventPipe_Initialize() {}

bool DiagnosticServer_Initialize() { return false; }
void DiagnosticServer_PauseForDiagnosticsMonitor() {}

void EventPipe_FinishInitialize() {}

void EventPipe_ThreadShutdown() { }

void EventPipe_Shutdown() {}
bool DiagnosticServer_Shutdown() { return false; }
