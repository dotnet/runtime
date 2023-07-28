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

#include "eventtrace_etw.h"

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

void EventTracing_Initialize()
{
#ifdef FEATURE_ETW
    MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_Context.IsEnabled = FALSE;
    MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_Context.IsEnabled = FALSE;

    // Register the Redhawk event provider with the system.
    RH_ETW_REGISTER_Microsoft_Windows_Redhawk_GC_Private();
    RH_ETW_REGISTER_Microsoft_Windows_Redhawk_GC_Public();

    MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_Redhawk_GC_PrivateHandle;
    MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_Redhawk_GC_PublicHandle;
#endif // FEATURE_ETW
}

#ifdef FEATURE_ETW
//
// -----------------------------------------------------------------------------------------------------------
//
// The automatically generated part of the Redhawk ETW infrastructure (EtwEvents.h) calls the following
// function whenever the system enables or disables tracing for this provider.
//

uint32_t EtwCallback(uint32_t IsEnabled, RH_ETW_CONTEXT * pContext)
{
    GCHeapUtilities::RecordEventStateChange(!!(pContext->RegistrationHandle == Microsoft_Windows_Redhawk_GC_PublicHandle),
                                            static_cast<GCEventKeyword>(pContext->MatchAnyKeyword),
                                            static_cast<GCEventLevel>(pContext->Level));

    if (IsEnabled &&
        (pContext->RegistrationHandle == Microsoft_Windows_Redhawk_GC_PrivateHandle) &&
        GCHeapUtilities::IsGCHeapInitialized())
    {
        FireEtwGCSettings(GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(FALSE),
                          GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(TRUE),
                          GCHeapUtilities::IsServerHeap());
        GCHeapUtilities::GetGCHeap()->DiagTraceGCSegments();
    }

    // Special check for the runtime provider's ManagedHeapCollectKeyword.  Profilers
    // flick this to force a full GC.
    if (IsEnabled &&
        (pContext->RegistrationHandle == Microsoft_Windows_Redhawk_GC_PublicHandle) &&
        GCHeapUtilities::IsGCHeapInitialized() &&
        ((pContext->MatchAnyKeyword & CLR_MANAGEDHEAPCOLLECT_KEYWORD) != 0))
    {
        // Profilers may (optionally) specify extra data in the filter parameter
        // to log with the GCStart event.
        LONGLONG l64ClientSequenceNumber = 0;
        if ((pContext->FilterData != NULL) &&
            (pContext->FilterData->Type == 1) &&
            (pContext->FilterData->Size == sizeof(l64ClientSequenceNumber)))
        {
            l64ClientSequenceNumber = *(LONGLONG *) (pContext->FilterData->Ptr);
        }
        ETW::GCLog::ForceGC(l64ClientSequenceNumber);
    }

    return 0;
}
#endif // FEATURE_ETW