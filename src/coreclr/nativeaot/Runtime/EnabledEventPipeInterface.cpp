// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "eventpipeadapter.h"
#include "diagnosticserveradapter.h"

#include "gcenv.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "SpinLock.h"

void EventPipeAdapter_Initialize() { EventPipeAdapter::Initialize(); }
bool EventPipeAdapter_Enabled() { return EventPipeAdapter::Enabled(); }

bool DiagnosticServerAdapter_Initialize() { return DiagnosticServerAdapter::Initialize(); }
void DiagnosticServerAdapter_PauseForDiagnosticsMonitor() { DiagnosticServerAdapter::PauseForDiagnosticsMonitor();}


void EventPipeAdapter_FinishInitialize() { EventPipeAdapter::FinishInitialize(); }

void EventPipeAdapter_Shutdown() { EventPipeAdapter::Shutdown(); }
bool DiagnosticServerAdapter_Shutdown() { return DiagnosticServerAdapter::Shutdown(); }
