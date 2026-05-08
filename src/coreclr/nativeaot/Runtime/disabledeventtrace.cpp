// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace.cpp
// Abstract: This module implements Event Tracing support
//
// ============================================================================

#include "common.h"

#include "eventtrace.h"
#include "eventtracebase.h"

void EventTracing_Initialize() { }

bool IsRuntimeProviderEnabled(uint8_t level, uint64_t keyword)
{
    return false;
}

void ETW::GCLog::FireGcStart(ETW_GC_INFO * pGcInfo) { }
void ETW::LoaderLog::ModuleLoad(HANDLE pModule) { }
BOOL ETW::GCLog::ShouldTrackMovementForEtw() { return FALSE; }
void ETW::GCLog::BeginMovedReferences(size_t * pProfilingContext) { }
void ETW::GCLog::EndMovedReferences(size_t profilingContext, BOOL fAllowProfApiNotification) { }
void ETW::GCLog::WalkHeap() { }
