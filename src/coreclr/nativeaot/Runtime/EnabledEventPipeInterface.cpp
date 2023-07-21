// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "eventpipeadapter.h"
#include "diagnosticserveradapter.h"

void EventPipeAdapter_Initialize() { EventPipeAdapter::Initialize(); }

bool DiagnosticServerAdapter_Initialize() { return DiagnosticServerAdapter::Initialize(); }
void DiagnosticServerAdapter_PauseForDiagnosticsMonitor() { DiagnosticServerAdapter::PauseForDiagnosticsMonitor();}

void EventPipeAdapter_FinishInitialize() { EventPipeAdapter::FinishInitialize(); }

void EventPipe_ThreadShutdown() { ep_rt_aot_thread_exited(); }

void EventPipeAdapter_Shutdown() { EventPipeAdapter::Shutdown(); }
bool DiagnosticServerAdapter_Shutdown() { return DiagnosticServerAdapter::Shutdown(); }
