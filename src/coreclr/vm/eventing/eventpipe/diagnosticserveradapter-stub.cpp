// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "diagnosticserveradapter.h"

bool DiagnosticServerAdapter::Initialize()
{
    return false;
}

bool DiagnosticServerAdapter::Shutdown()
{
    return false;
}

void DiagnosticServerAdapter::PauseForDiagnosticsMonitor()
{
}

void DiagnosticServerAdapter::ResumeRuntimeStartup()
{
}

bool DiagnosticServerAdapter::IsPausedInRuntimeStartup()
{
    return false;
}
