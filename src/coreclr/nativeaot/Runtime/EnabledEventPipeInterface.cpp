// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <eventpipe/ep.h>
#include <eventpipe/ep-rt-aot.h>
#include <eventpipe/ds-server.h>

void EventPipe_Initialize() { ep_init(); }

bool DiagnosticServer_Initialize() { return ds_server_init(); }
void DiagnosticServer_PauseForDiagnosticsMonitor() { ds_server_pause_for_diagnostics_monitor(); }

void EventPipe_FinishInitialize() { ep_finish_init(); }

void EventPipe_ThreadShutdown() { ep_rt_aot_thread_exited(); }

void EventPipe_Shutdown() { ep_shutdown(); }
bool DiagnosticServer_Shutdown() { return ds_server_shutdown(); }
