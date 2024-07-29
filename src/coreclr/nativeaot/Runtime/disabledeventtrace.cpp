// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace.cpp
// Abstract: This module implements Event Tracing support
//
// ============================================================================

#include "common.h"

#include "eventtrace.h"

void EventTracing_Initialize() { }

void ETW::GCLog::FireGcStart(ETW_GC_INFO * pGcInfo) { }

#ifdef FEATURE_ETW
BOOL ETW::GCLog::ShouldTrackMovementForEtw() { return FALSE; }
void ETW::GCLog::BeginMovedReferences(size_t * pProfilingContext) { }
void ETW::GCLog::EndMovedReferences(size_t profilingContext, BOOL fAllowProfApiNotification) { }
void ETW::GCLog::WalkHeap() { }
#endif // FEATURE_ETW
