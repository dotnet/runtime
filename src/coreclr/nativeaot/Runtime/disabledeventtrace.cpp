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
BOOL ETW::GCLog::ShouldWalkHeapObjectsForEtw() { return FALSE; }
BOOL ETW::GCLog::ShouldWalkHeapRootsForEtw() { return FALSE; }
BOOL ETW::GCLog::ShouldTrackMovementForEtw() { return FALSE; }
BOOL ETW::GCLog::ShouldWalkStaticsAndCOMForEtw() { return FALSE; }
void ETW::GCLog::ForceGC(LONGLONG l64ClientSequenceNumber) { }
void ETW::GCLog::EndHeapDump(ProfilerWalkHeapContext * profilerWalkHeapContext) { }
void ETW::GCLog::BeginMovedReferences(size_t * pProfilingContext) { }
void ETW::GCLog::MovedReference(BYTE * pbMemBlockStart, BYTE * pbMemBlockEnd, ptrdiff_t cbRelocDistance, size_t profilingContext, BOOL fCompacting, BOOL fAllowProfApiNotification) { }
void ETW::GCLog::EndMovedReferences(size_t profilingContext, BOOL fAllowProfApiNotification) { }
void ETW::GCLog::WalkStaticsAndCOMForETW() { }
void ETW::GCLog::ObjectReference(
    ProfilerWalkHeapContext* profilerWalkHeapContext,
    Object* pObjReferenceSource,
    ULONGLONG typeID,
    ULONGLONG cRefs,
    Object** rgObjReferenceTargets) { }
void ETW::GCLog::RootReference(
    LPVOID pvHandle,
    Object * pRootedNode,
    Object * pSecondaryNodeForDependentHandle,
    BOOL fDependentHandle,
    ProfilingScanContext * profilingScanContext,
    DWORD dwGCFlags,
    DWORD rootFlags) { }
#endif // FEATURE_ETW
