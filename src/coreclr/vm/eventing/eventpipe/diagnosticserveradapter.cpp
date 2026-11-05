// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "diagnosticserveradapter.h"

bool DiagnosticServerAdapter::Initialize()
{
    return ds_server_init();
}

bool DiagnosticServerAdapter::Shutdown()
{
    return ds_server_shutdown();
}

void DiagnosticServerAdapter::PauseForDiagnosticsMonitor()
{
    ds_server_pause_for_diagnostics_monitor();
}

void DiagnosticServerAdapter::ResumeRuntimeStartup()
{
    ds_server_resume_runtime_startup();
}

bool DiagnosticServerAdapter::IsPausedInRuntimeStartup()
{
    return ds_server_is_paused_in_startup();
}
