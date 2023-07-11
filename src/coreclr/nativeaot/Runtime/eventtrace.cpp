// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace.cpp
// Abstract: This module implements Event Tracing support
//
// ============================================================================

#include "common.h"

#include "gcenv.h"
#include "gcheaputilities.h"

#include "daccess.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"

volatile LONGLONG ETW::GCLog::s_l64LastClientSequenceNumber = 0;

//---------------------------------------------------------------------------------------
//
// Helper to fire the GCStart event.  Figures out which version of GCStart to fire, and
// includes the client sequence number, if available.
//
// Arguments:
//      pGcInfo - ETW_GC_INFO containing details from GC about this collection
//

// static
void ETW::GCLog::FireGcStart(ETW_GC_INFO* pGcInfo)
{
    LIMITED_METHOD_CONTRACT;

    if (RUNTIME_PROVIDER_CATEGORY_ENABLED(TRACE_LEVEL_INFORMATION, CLR_GC_KEYWORD))
    {
        // If the controller specified a client sequence number for us to log with this
        // GCStart, then retrieve it
        LONGLONG l64ClientSequenceNumberToLog = 0;
        if ((s_l64LastClientSequenceNumber != 0) &&
            (pGcInfo->GCStart.Depth == GCHeapUtilities::GetGCHeap()->GetMaxGeneration()) &&
            (pGcInfo->GCStart.Reason == ETW_GC_INFO::GC_INDUCED))
        {
            // No InterlockedExchange64 on Redhawk (presumably b/c there is no compiler
            // intrinsic for this on x86, even though there is one for InterlockedCompareExchange64)
            l64ClientSequenceNumberToLog = PalInterlockedCompareExchange64(&s_l64LastClientSequenceNumber, 0, s_l64LastClientSequenceNumber);
        }

        FireEtwGCStart_V2(pGcInfo->GCStart.Count, pGcInfo->GCStart.Depth, pGcInfo->GCStart.Reason, pGcInfo->GCStart.Type, GetClrInstanceId(), l64ClientSequenceNumberToLog);
    }
}
