// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace.cpp
// Abstract: This module implements Event Tracing support
//
// ============================================================================

#include "common.h"

#ifdef FEATURE_NATIVEAOT
#include "gcenv.h"
#include "gcheaputilities.h"

#include "daccess.h"

#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "stackframeiterator.h"
#include "thread.h"
#include "rwlock.h"
#include "threadstore.h"
#include "threadstore.inl"
//#include "PalRedhawk.h"

#define Win32EventWrite PalEventWrite
#else // !FEATURE_NATIVEAOT

#include "eventtrace.h"
#include "winbase.h"
#include "contract.h"
#include "ex.h"
#include "dbginterface.h"
#define Win32EventWrite EventWrite

// Flags used to store some runtime information for Event Tracing
BOOL g_fEEOtherStartup = FALSE;
BOOL g_fEEComActivatedStartup = FALSE;
LPCGUID g_fEEComObjectGuid = &GUID_NULL;

BOOL g_fEEHostedStartup = FALSE;

#endif // FEATURE_NATIVEAOT

#include "eventtracepriv.h"

#ifdef FEATURE_NATIVEAOT
volatile LONGLONG ETW::GCLog::s_l64LastClientSequenceNumber = 0;
#else // FEATURE_NATIVEAOT
Volatile<LONGLONG> ETW::GCLog::s_l64LastClientSequenceNumber = 0;
#endif // FEATURE_NATIVEAOT

#ifndef FEATURE_NATIVEAOT

//---------------------------------------------------------------------------------------
// Helper macros to determine which version of the Method events to use
//
// The V2 versions of these events include the ReJITID, the V1 versions do not.
// Historically, when we version events, we'd just stop sending the old version and only
// send the new one. However, now that we have xperf in heavy use internally and soon to be
// used externally, we need to be a bit careful. In particular, we'd like to allow
// current xperf to continue working without knowledge of ReJITIDs, and allow future
// xperf to decode symbols in ReJITted functions. Thus,
//    * During a first-JIT, only issue the existing V1 MethodLoad, etc. events (NOT v0,
//        NOT v2). This event does not include a ReJITID, and can thus continue to be
//        parsed by older decoders.
//    * During a rejit, only issue the new V2 events (NOT v0 or v1), which will include a
//        nonzero ReJITID. Thus, your unique key for a method extent would be MethodID +
//        ReJITID + extent (hot/cold). These events will be ignored by older decoders
//        (including current xperf) because of the version number, but xperf will be
//        updated to decode these in the future.

#define FireEtwMethodLoadVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodLoadVerbose_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID); } \
    else \
        { FireEtwMethodLoadVerbose_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID); } \
}

#define FireEtwMethodLoad_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodLoad_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, clrInstanceID); } \
    else \
        { FireEtwMethodLoad_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, clrInstanceID, rejitID); } \
}

#define FireEtwMethodUnloadVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodUnloadVerbose_V1(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID); } \
    else \
        { FireEtwMethodUnloadVerbose_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID); } \
}

#define FireEtwMethodUnload_V1_or_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodUnload_V1(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID); } \
    else \
        { FireEtwMethodUnload_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, rejitID); } \
}

#define FireEtwMethodDCStartVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodDCStartVerbose_V1(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID); } \
    else \
        { FireEtwMethodDCStartVerbose_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID); } \
}

#define FireEtwMethodDCStart_V1_or_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodDCStart_V1(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID); } \
    else \
        { FireEtwMethodDCStart_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, rejitID); } \
}

#define FireEtwMethodDCEndVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodDCEndVerbose_V1(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID);  } \
    else \
        { FireEtwMethodDCEndVerbose_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, rejitID); } \
}

#define FireEtwMethodDCEnd_V1_or_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, rejitID) \
{   \
    if (rejitID == 0)   \
        { FireEtwMethodDCEnd_V1(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID);  } \
    else \
        { FireEtwMethodDCEnd_V2(ullMethodIdentifier, ullModuleID, ullColdMethodStartAddress, ulColdMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, rejitID); } \
}

// Module load / unload events:
//     There is no precedent here for using GUIDs in Mac events, and it's doubtful any
//     of the new PDB fields for the V2 Module events are at all useful on the Mac anyway.  So
//     stick with V1 module events on the Mac.

#ifdef FEATURE_DTRACE
#define FireEtwModuleLoad_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleLoad_V1(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId)
#define FireEtwModuleUnload_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleUnload_V1(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId)
#define FireEtwModuleDCStart_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleDCStart_V1(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId)
#define FireEtwModuleDCEnd_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleDCEnd_V1(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId)
#else   // FEATURE_DTRACE
#define FireEtwModuleLoad_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleLoad_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)
#define FireEtwModuleUnload_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleUnload_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)
#define FireEtwModuleDCStart_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleDCStart_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)
#define FireEtwModuleDCEnd_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleDCEnd_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)
#endif  // FEATURE_DTRACE


//---------------------------------------------------------------------------------------
//
// Rather than checking the NGEN keyword on the runtime provider directly, use this
// helper that checks that the NGEN runtime provider keyword is enabled AND the
// OverrideAndSuppressNGenEvents keyword on the runtime provider is NOT enabled.
//
// OverrideAndSuppressNGenEvents allows controllers to set the expensive NGEN keyword for
// older runtimes (< 4.0) where NGEN PDB info is NOT available, while suppressing those
// expensive events on newer runtimes (>= 4.5) where NGEN PDB info IS available. Note
// that 4.0 has NGEN PDBS but unfortunately not the OverrideAndSuppressNGenEvents
// keyword, b/c NGEN PDBs were made publicly only after 4.0 shipped. So tools that need
// to consume both <4.0 and 4.0 events would neeed to enable the expensive NGEN events to
// deal properly with 3.5, even though those events aren't necessary on 4.0.
//
// On CoreCLR, this keyword is a no-op, because coregen PDBs don't exist (and thus we'll
// need the NGEN rundown to still work on Silverligth).
//
// Return Value:
//      nonzero iff NGenKeyword is enabled on the runtime provider and
//      OverrideAndSuppressNGenEventsKeyword is not enabled on the runtime provider.
//

BOOL IsRuntimeNgenKeywordEnabledAndNotSuppressed()
{
    LIMITED_METHOD_CONTRACT;

    return
        (
            ETW_TRACING_CATEGORY_ENABLED(
                MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                TRACE_LEVEL_INFORMATION,
                CLR_NGEN_KEYWORD)
            && !(ETW_TRACING_CATEGORY_ENABLED(
                MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                TRACE_LEVEL_INFORMATION,
                CLR_OVERRIDEANDSUPPRESSNGENEVENTS_KEYWORD))
            );
}

// Same as above, but for the rundown provider
BOOL IsRundownNgenKeywordEnabledAndNotSuppressed()
{
    LIMITED_METHOD_CONTRACT;

    return
        (
            ETW_TRACING_CATEGORY_ENABLED(
                MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                TRACE_LEVEL_INFORMATION,
                CLR_RUNDOWNNGEN_KEYWORD)
            && !(ETW_TRACING_CATEGORY_ENABLED(
                MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                TRACE_LEVEL_INFORMATION,
                CLR_RUNDOWNOVERRIDEANDSUPPRESSNGENEVENTS_KEYWORD))
            );
}

/*******************************************************/
/* Fast assembly function to get the topmost EBP frame */
/*******************************************************/
#if defined(TARGET_X86)
extern "C"
{
    CallStackFrame* GetEbp()
    {
        CallStackFrame* frame = NULL;
        __asm
        {
            mov frame, ebp
        }
        return frame;
    }
}
#endif //TARGET_X86

#ifndef FEATURE_PAL

/*************************************/
/* Function to append a frame to an existing stack */
/*************************************/
void ETW::SamplingLog::Append(SIZE_T currentFrame)
{
    LIMITED_METHOD_CONTRACT;
    if (m_FrameCount < (ETW::SamplingLog::s_MaxStackSize - 1) &&
        currentFrame != 0)
    {
        m_EBPStack[m_FrameCount] = currentFrame;
        m_FrameCount++;
    }
};

/********************************************************/
/* Function to get the callstack on the current thread  */
/********************************************************/
ETW::SamplingLog::EtwStackWalkStatus ETW::SamplingLog::GetCurrentThreadsCallStack(UINT32* frameCount, PVOID** Stack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // The stack walk performed below can cause allocations (thus entering the host). But
    // this is acceptable, since we're not supporting the use of SQL/F1 profiling and
    // full-blown ETW CLR stacks (which would be redundant).
    PERMANENT_CONTRACT_VIOLATION(HostViolation, ReasonUnsupportedForSQLF1Profiling);

    m_FrameCount = 0;
    ETW::SamplingLog::EtwStackWalkStatus stackwalkStatus = SaveCurrentStack();

    _ASSERTE(m_FrameCount < ETW::SamplingLog::s_MaxStackSize);

    // this not really needed, but let's do it
    // because we use the framecount while dumping the stack event
    for (int i = m_FrameCount; i < ETW::SamplingLog::s_MaxStackSize; i++)
    {
        m_EBPStack[i] = 0;
    }
    // This is for consumers to work correctly because the number of
    // frames in the manifest file is specified to be 2
    if (m_FrameCount < 2)
        m_FrameCount = 2;

    *frameCount = m_FrameCount;
    *Stack = (PVOID*)m_EBPStack;
    return stackwalkStatus;
};

/*************************************/
/* Function to save the stack on the current thread */
/*************************************/
ETW::SamplingLog::EtwStackWalkStatus ETW::SamplingLog::SaveCurrentStack(int skipTopNFrames)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    if (!IsGarbageCollectorFullyInitialized())
    {
        // If the GC isn't ready yet, then there won't be any interesting
        // managed code on the stack to walk. Plus, the stack walk itself may
        // hit problems (e.g., when calling into the code manager) if it's run
        // too early during startup.
        return ETW::SamplingLog::UnInitialized;
    }
#ifndef DACCESS_COMPILE
#if !defined(TARGET_X86) && !defined(TARGET_ARM)
    if (RtlVirtualUnwind_Unsafe == NULL)
    {
        // We haven't even set up the RtlVirtualUnwind function pointer yet,
        // so it's too early to try stack walking.
        return ETW::SamplingLog::UnInitialized;
    }
#endif // !TARGET_X86 && !TARGET_ARM
    Thread* pThread = GetThread();
    if (pThread == NULL)
    {
        return ETW::SamplingLog::UnInitialized;
    }
    // The thread should not have a hijack set up or we can't walk the stack.
    if (pThread->m_State & Thread::TS_Hijacked) {
        return ETW::SamplingLog::UnInitialized;
    }

    if (pThread->IsEtwStackWalkInProgress())
    {
        return ETW::SamplingLog::InProgress;
    }
    pThread->MarkEtwStackWalkInProgress();
    EX_TRY
    {
#ifdef TARGET_X86
        CallStackFrame * currentEBP = GetEbp();
        CallStackFrame* lastEBP = NULL;
        while (currentEBP)
        {
            lastEBP = currentEBP;
            currentEBP = currentEBP->m_Next;

            // Skip the top N frames
            if (skipTopNFrames) {
                skipTopNFrames--;
                continue;
            }

            // Save the Return Address for symbol decoding
            Append(lastEBP->m_ReturnAddress);

            // Check for stack limits
            if ((SIZE_T)currentEBP < (SIZE_T)Thread::GetStackLowerBound() || (SIZE_T)currentEBP >(SIZE_T)Thread::GetStackUpperBound())
            {
                break;
            }

            // If we have a too small address, we are probably bad
            if ((SIZE_T)currentEBP < (SIZE_T)0x10000)
                break;

            if ((SIZE_T)currentEBP < (SIZE_T)lastEBP)
            {
                break;
            }
        }
#else
        CONTEXT ctx;
        ClrCaptureContext(&ctx);
        UINT_PTR ControlPc = 0;
        UINT_PTR CurrentSP = 0, PrevSP = 0;

        while (1)
        {
            // Unwind to the caller
            ControlPc = Thread::VirtualUnwindCallFrame(&ctx);

            // This is to take care of recursion
            CurrentSP = (UINT_PTR)GetSP(&ctx);

            // when to break from this loop
            if (ControlPc == 0 || (PrevSP == CurrentSP))
            {
                break;
            }

            // Skip the top N frames
            if (skipTopNFrames) {
                skipTopNFrames--;
                continue;
            }

            // Add the stack frame to the list
            Append(ControlPc);

            PrevSP = CurrentSP;
        }
#endif //TARGET_X86
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
    pThread->MarkEtwStackWalkCompleted();
#endif //!DACCESS_COMPILE

    return ETW::SamplingLog::Completed;
}
#endif //!FEATURE_PAL

#endif // !FEATURE_NATIVEAOT


#if defined(FEATURE_NATIVEAOT) || !defined(FEATURE_PAL) || defined(FEATURE_DTRACE)

/****************************************************************************/
/* Methods that are called from the runtime */
/****************************************************************************/

#ifndef FEATURE_DTRACE
/****************************************************************************/
/* Methods for rundown events                                               */
/* Since DTRACe does not support passing a method pointer as a callback when*/
/* enable a events, rundown events are not supported on Mac                 */
/****************************************************************************/

/***************************************************************************/
/* This function should be called from the event tracing callback routine
   when the private CLR provider is enabled */
   /***************************************************************************/

#ifndef FEATURE_NATIVEAOT

void ETW::GCLog::GCSettingsEvent()
{
    if (GCHeapUtilities::IsGCHeapInitialized())
    {
        if (ETW_TRACING_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context,
            GCSettings))
        {
            ETW::GCLog::ETW_GC_INFO Info;

            Info.GCSettings.ServerGC = GCHeapUtilities::IsServerHeap();
            Info.GCSettings.SegmentSize = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(FALSE);
            Info.GCSettings.LargeObjectSegmentSize = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(TRUE);
            FireEtwGCSettings_V1(Info.GCSettings.SegmentSize, Info.GCSettings.LargeObjectSegmentSize, Info.GCSettings.ServerGC, GetClrInstanceId());
        }
        GCHeapUtilities::GetGCHeap()->TraceGCSegments();
    }
};

#endif // !FEATURE_NATIVEAOT


//---------------------------------------------------------------------------------------
// Code for sending GC heap object events is generally the same for both FEATURE_NATIVEAOT
// and !FEATURE_NATIVEAOT builds
//---------------------------------------------------------------------------------------


// Simple helpers called by the GC to decide whether it needs to do a walk of heap
// objects and / or roots.

BOOL ETW::GCLog::ShouldWalkHeapObjectsForEtw()
{
    LIMITED_METHOD_CONTRACT;
    return ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_GCHEAPDUMP_KEYWORD);
}

BOOL ETW::GCLog::ShouldWalkHeapRootsForEtw()
{
    LIMITED_METHOD_CONTRACT;
    return ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_GCHEAPDUMP_KEYWORD);
}

BOOL ETW::GCLog::ShouldTrackMovementForEtw()
{
    LIMITED_METHOD_CONTRACT;
    return ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_GCHEAPSURVIVALANDMOVEMENT_KEYWORD);
}

BOOL ETW::GCLog::ShouldWalkStaticsAndCOMForEtw()
{
    // @TODO:
    return FALSE;
}

void ETW::GCLog::WalkStaticsAndCOMForETW()
{
    // @TODO:
}


// Batches the list of moved/surviving references for the GCBulkMovedObjectRanges /
// GCBulkSurvivingObjectRanges events
struct EtwGcMovementContext
{
public:
    // An instance of EtwGcMovementContext is dynamically allocated and stored
    // inside of MovedReferenceContextForEtwAndProfapi, which in turn is dynamically
    // allocated and pointed to by a profiling_context pointer created by the GC on the stack.
    // This is used to batch and send GCBulkSurvivingObjectRanges events and
    // GCBulkMovedObjectRanges events. This method is passed a pointer to
    // MovedReferenceContextForEtwAndProfapi::pctxEtw; if non-NULL it gets returned;
    // else, a new EtwGcMovementContext is allocated, stored in that pointer, and
    // then returned. Callers should test for NULL, which can be returned if out of
    // memory
    static EtwGcMovementContext* GetOrCreateInGCContext(EtwGcMovementContext** ppContext)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ppContext != NULL);

        EtwGcMovementContext* pContext = *ppContext;
        if (pContext == NULL)
        {
            pContext = new (nothrow) EtwGcMovementContext;
            *ppContext = pContext;
        }
        return pContext;
    }

    EtwGcMovementContext() :
        iCurBulkSurvivingObjectRanges(0),
        iCurBulkMovedObjectRanges(0)
    {
        LIMITED_METHOD_CONTRACT;
        Clear();
    }

    // Resets structure for reuse on construction, and after each flush.
    // (Intentionally leave iCurBulk* as is, since they persist across flushes within a GC.)
    void Clear()
    {
        LIMITED_METHOD_CONTRACT;
        cBulkSurvivingObjectRanges = 0;
        cBulkMovedObjectRanges = 0;
        ZeroMemory(rgGCBulkSurvivingObjectRanges, sizeof(rgGCBulkSurvivingObjectRanges));
        ZeroMemory(rgGCBulkMovedObjectRanges, sizeof(rgGCBulkMovedObjectRanges));
    }

    //---------------------------------------------------------------------------------------
    // GCBulkSurvivingObjectRanges
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkSurvivingObjectRanges event
    UINT iCurBulkSurvivingObjectRanges;

    // Number of surviving object ranges currently filled out in rgGCBulkSurvivingObjectRanges array
    UINT cBulkSurvivingObjectRanges;

    // Struct array containing the primary data for each GCBulkSurvivingObjectRanges
    // event. Fix the size so the total event stays well below the 64K limit (leaving
    // lots of room for non-struct fields that come before the values data)
    EventStructGCBulkSurvivingObjectRangesValue rgGCBulkSurvivingObjectRanges[
        (cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkSurvivingObjectRangesValue)];

    //---------------------------------------------------------------------------------------
    // GCBulkMovedObjectRanges
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkMovedObjectRanges event
    UINT iCurBulkMovedObjectRanges;

    // Number of Moved object ranges currently filled out in rgGCBulkMovedObjectRanges array
    UINT cBulkMovedObjectRanges;

    // Struct array containing the primary data for each GCBulkMovedObjectRanges
    // event. Fix the size so the total event stays well below the 64K limit (leaving
    // lots of room for non-struct fields that come before the values data)
    EventStructGCBulkMovedObjectRangesValue rgGCBulkMovedObjectRanges[
        (cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkMovedObjectRangesValue)];
};

// Contains above struct for ETW, plus extra info (opaque to us) used by the profiling
// API to track its own information.
struct MovedReferenceContextForEtwAndProfapi
{
    // An instance of MovedReferenceContextForEtwAndProfapi is dynamically allocated and
    // pointed to by a profiling_context pointer created by the GC on the stack. This is used to
    // batch and send GCBulkSurvivingObjectRanges events and GCBulkMovedObjectRanges
    // events and the corresponding callbacks for profapi profilers. This method is
    // passed a pointer to a MovedReferenceContextForEtwAndProfapi; if non-NULL it gets
    // returned; else, a new MovedReferenceContextForEtwAndProfapi is allocated, stored
    // in that pointer, and then returned. Callers should test for NULL, which can be
    // returned if out of memory
    static MovedReferenceContextForEtwAndProfapi* CreateInGCContext(LPVOID pvContext)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(pvContext != NULL);

        MovedReferenceContextForEtwAndProfapi* pContext = *(MovedReferenceContextForEtwAndProfapi**)pvContext;

        // Shouldn't be called if the context was already created.  Perhaps someone made
        // one too many BeginMovedReferences calls, or didn't have an EndMovedReferences
        // in between?
        _ASSERTE(pContext == NULL);

        pContext = new (nothrow) MovedReferenceContextForEtwAndProfapi;
        *(MovedReferenceContextForEtwAndProfapi**)pvContext = pContext;

        return pContext;
    }


    MovedReferenceContextForEtwAndProfapi() :
        pctxProfAPI(NULL),
        pctxEtw(NULL)

    {
        LIMITED_METHOD_CONTRACT;
    }

    LPVOID pctxProfAPI;
    EtwGcMovementContext* pctxEtw;
};


//---------------------------------------------------------------------------------------
//
// Called by the GC for each moved or surviving reference that it encounters. This
// batches the info into our context's buffer, and flushes that buffer to ETW as it fills
// up.
//
// Arguments:
//      * pbMemBlockStart - Start of moved/surviving block
//      * pbMemBlockEnd - Next pointer after end of moved/surviving block
//      * cbRelocDistance - How far did the block move? (0 for non-compacted / surviving
//          references; negative if moved to earlier addresses)
//      * profilingContext - Where our context is stored
//      * fCompacting - Is this a compacting GC? Used to decide whether to send the moved
//          or surviving event
//

// static
void ETW::GCLog::MovedReference(
    BYTE* pbMemBlockStart,
    BYTE* pbMemBlockEnd,
    ptrdiff_t cbRelocDistance,
    size_t profilingContext,
    BOOL fCompacting,
    BOOL /*fAllowProfApiNotification*/) // @TODO: unused param from newer implementation
{
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;  // EEToProfInterfaceImpl::AllocateMovedReferencesData takes lock
    }
    CONTRACTL_END;

    MovedReferenceContextForEtwAndProfapi* pCtxForEtwAndProfapi =
        (MovedReferenceContextForEtwAndProfapi*)profilingContext;
    if (pCtxForEtwAndProfapi == NULL)
    {
        _ASSERTE(!"MovedReference() encountered a NULL profilingContext");
        return;
    }

#ifdef PROFILING_SUPPORTED
    // ProfAPI
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        g_profControlBlock.pProfInterface->MovedReference(pbMemBlockStart,
            pbMemBlockEnd,
            cbRelocDistance,
            &(pCtxForEtwAndProfapi->pctxProfAPI),
            fCompacting);
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED

    // ETW

    if (!ShouldTrackMovementForEtw())
        return;

    EtwGcMovementContext* pContext =
        EtwGcMovementContext::GetOrCreateInGCContext(&pCtxForEtwAndProfapi->pctxEtw);
    if (pContext == NULL)
        return;

    if (fCompacting)
    {
        // Moved references

        _ASSERTE(pContext->cBulkMovedObjectRanges < _countof(pContext->rgGCBulkMovedObjectRanges));
        EventStructGCBulkMovedObjectRangesValue* pValue =
            &pContext->rgGCBulkMovedObjectRanges[pContext->cBulkMovedObjectRanges];
        pValue->OldRangeBase = pbMemBlockStart;
        pValue->NewRangeBase = pbMemBlockStart + cbRelocDistance;
        pValue->RangeLength = pbMemBlockEnd - pbMemBlockStart;
        pContext->cBulkMovedObjectRanges++;

        // If buffer is now full, empty it into ETW
        if (pContext->cBulkMovedObjectRanges == _countof(pContext->rgGCBulkMovedObjectRanges))
        {
            FireEtwGCBulkMovedObjectRanges(
                pContext->iCurBulkMovedObjectRanges,
                pContext->cBulkMovedObjectRanges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkMovedObjectRanges[0]),
                &pContext->rgGCBulkMovedObjectRanges[0]);

            pContext->iCurBulkMovedObjectRanges++;
            pContext->Clear();
        }
    }
    else
    {
        // Surviving references

        _ASSERTE(pContext->cBulkSurvivingObjectRanges < _countof(pContext->rgGCBulkSurvivingObjectRanges));
        EventStructGCBulkSurvivingObjectRangesValue* pValue =
            &pContext->rgGCBulkSurvivingObjectRanges[pContext->cBulkSurvivingObjectRanges];
        pValue->RangeBase = pbMemBlockStart;
        pValue->RangeLength = pbMemBlockEnd - pbMemBlockStart;
        pContext->cBulkSurvivingObjectRanges++;

        // If buffer is now full, empty it into ETW
        if (pContext->cBulkSurvivingObjectRanges == _countof(pContext->rgGCBulkSurvivingObjectRanges))
        {
            FireEtwGCBulkSurvivingObjectRanges(
                pContext->iCurBulkSurvivingObjectRanges,
                pContext->cBulkSurvivingObjectRanges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkSurvivingObjectRanges[0]),
                &pContext->rgGCBulkSurvivingObjectRanges[0]);

            pContext->iCurBulkSurvivingObjectRanges++;
            pContext->Clear();
        }
    }
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT
}


//---------------------------------------------------------------------------------------
//
// Called by the GC just before it begins enumerating plugs.  Gives us a chance to
// allocate our context structure, to allow us to batch plugs before firing events
// for them
//
// Arguments:
//      * pProfilingContext - Points to location on stack (in GC function) where we can
//         store a pointer to the context we allocate
//

// static
void ETW::GCLog::BeginMovedReferences(size_t* pProfilingContext)
{
    LIMITED_METHOD_CONTRACT;

    MovedReferenceContextForEtwAndProfapi::CreateInGCContext(LPVOID(pProfilingContext));
}


//---------------------------------------------------------------------------------------
//
// Called by the GC at the end of a heap walk to give us a place to flush any remaining
// buffers of data to ETW or the profapi profiler
//
// Arguments:
//      profilingContext - Our context we built up during the heap walk
//

// static
void ETW::GCLog::EndMovedReferences(size_t profilingContext,
    BOOL /*fAllowProfApiNotification*/) // @TODO: unused param from newer implementation
{
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    MovedReferenceContextForEtwAndProfapi* pCtxForEtwAndProfapi = (MovedReferenceContextForEtwAndProfapi*)profilingContext;
    if (pCtxForEtwAndProfapi == NULL)
    {
        _ASSERTE(!"EndMovedReferences() encountered a NULL profilingContext");
        return;
    }

#ifdef PROFILING_SUPPORTED
    // ProfAPI
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        g_profControlBlock.pProfInterface->EndMovedReferences(&(pCtxForEtwAndProfapi->pctxProfAPI));
        END_PIN_PROFILER();
    }
#endif //PROFILING_SUPPORTED

    // ETW

    if (!ShouldTrackMovementForEtw())
        return;

    // If context isn't already set up for us, then we haven't been collecting any data
    // for ETW events.
    EtwGcMovementContext* pContext = pCtxForEtwAndProfapi->pctxEtw;
    if (pContext == NULL)
        return;

    // Flush any remaining moved or surviving range data

    if (pContext->cBulkMovedObjectRanges > 0)
    {
        FireEtwGCBulkMovedObjectRanges(
            pContext->iCurBulkMovedObjectRanges,
            pContext->cBulkMovedObjectRanges,
            GetClrInstanceId(),
            sizeof(pContext->rgGCBulkMovedObjectRanges[0]),
            &pContext->rgGCBulkMovedObjectRanges[0]);
    }

    if (pContext->cBulkSurvivingObjectRanges > 0)
    {
        FireEtwGCBulkSurvivingObjectRanges(
            pContext->iCurBulkSurvivingObjectRanges,
            pContext->cBulkSurvivingObjectRanges,
            GetClrInstanceId(),
            sizeof(pContext->rgGCBulkSurvivingObjectRanges[0]),
            &pContext->rgGCBulkSurvivingObjectRanges[0]);
    }

    pCtxForEtwAndProfapi->pctxEtw = NULL;
    delete pContext;
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT
}

/***************************************************************************/
/* This implements the public runtime provider's GCHeapCollectKeyword.  It
   performs a full, gen-2, blocking GC.
/***************************************************************************/
void ETW::GCLog::ForceGC(LONGLONG l64ClientSequenceNumber)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_NATIVEAOT
    if (!GCHeapUtilities::IsGCHeapInitialized())
        return;

    // No InterlockedExchange64 on Redhawk, even though there is one for
    // InterlockedCompareExchange64. Technically, there's a race here by using
    // InterlockedCompareExchange64, but it's not worth addressing. The race would be
    // between two ETW controllers trying to trigger GCs simultaneously, in which case
    // one will win and get its sequence number to appear in the GCStart event, while the
    // other will lose. Rare, uninteresting, and low-impact.
    PalInterlockedCompareExchange64(&s_l64LastClientSequenceNumber, l64ClientSequenceNumber, s_l64LastClientSequenceNumber);
#else // !FEATURE_NATIVEAOT
    if (!IsGarbageCollectorFullyInitialized())
        return;

    InterlockedExchange64(&s_l64LastClientSequenceNumber, l64ClientSequenceNumber);
#endif // FEATURE_NATIVEAOT

    ForceGCForDiagnostics();
}

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

#if !defined(FEATURE_PAL) || defined(FEATURE_DTRACE)

    if (ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_GC_KEYWORD))
    {
#if !defined(FEATURE_PAL)
        // If the controller specified a client sequence number for us to log with this
        // GCStart, then retrieve it
        LONGLONG l64ClientSequenceNumberToLog = 0;
        if ((s_l64LastClientSequenceNumber != 0) &&
            (pGcInfo->GCStart.Depth == GCHeapUtilities::GetGCHeap()->GetMaxGeneration()) &&
            (pGcInfo->GCStart.Reason == ETW_GC_INFO::GC_INDUCED))
        {
#ifdef FEATURE_NATIVEAOT
            // No InterlockedExchange64 on Redhawk (presumably b/c there is no compiler
            // intrinsic for this on x86, even though there is one for InterlockedCompareExchange64)
            l64ClientSequenceNumberToLog = PalInterlockedCompareExchange64(&s_l64LastClientSequenceNumber, 0, s_l64LastClientSequenceNumber);
#else
            l64ClientSequenceNumberToLog = InterlockedExchange64(&s_l64LastClientSequenceNumber, 0);
#endif
        }

        FireEtwGCStart_V2(pGcInfo->GCStart.Count, pGcInfo->GCStart.Depth, pGcInfo->GCStart.Reason, pGcInfo->GCStart.Type, GetClrInstanceId(), l64ClientSequenceNumberToLog);

#elif defined(FEATURE_DTRACE)
        FireEtwGCStart(pGcInfo->GCStart.Count, pGcInfo->GCStart.Reason);
#endif
    }

#endif // defined(FEATURE_PAL) || defined(FEATURE_DTRACE)
}

//---------------------------------------------------------------------------------------
//
// Contains code common to profapi and ETW scenarios where the profiler wants to force
// the CLR to perform a GC.  The important work here is to create a managed thread for
// the current thread BEFORE the GC begins.  On both ETW and profapi threads, there may
// not yet be a managed thread object.  But some scenarios require a managed thread
// object be present (notably if we need to call into Jupiter during the GC).
//
// Return Value:
//      HRESULT indicating success or failure
//
// Assumptions:
//      Caller should ensure that the EE has fully started up and that the GC heap is
//      initialized enough to actually perform a GC
//

// static
HRESULT ETW::GCLog::ForceGCForDiagnostics()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

#ifndef FEATURE_NATIVEAOT
    // Caller should ensure we're past startup.
    _ASSERTE(IsGarbageCollectorFullyInitialized());

    // In immersive apps the GarbageCollect() call below will call into Jupiter,
    // which will call back into the runtime to track references. This call
    // chain would cause a Thread object to be created for this thread while code
    // higher on the stack owns the ThreadStoreLock. This will lead to asserts
    // since the ThreadStoreLock is non-reentrant. To avoid this we'll create
    // the Thread object here instead.
    if (GetThreadNULLOk() == NULL)
    {
        HRESULT hr = E_FAIL;
        SetupThreadNoThrow(&hr);
        if (FAILED(hr))
            return hr;
    }

    ASSERT_NO_EE_LOCKS_HELD();

    EX_TRY
    {
        // Need to switch to cooperative mode as the thread will access managed
        // references (through Jupiter callbacks).
        GCX_COOP();

#else       // FEATURE_NATIVEAOT
        _ASSERTE(GCHeapUtilities::IsGCHeapInitialized());

        ThreadStore::AttachCurrentThread();
        Thread* pThread = ThreadStore::GetCurrentThread();

        // Doing this prevents the GC from trying to walk this thread's stack for roots.
        pThread->SetGCSpecial(true);

        // While doing the GC, much code assumes & asserts the thread doing the GC is in
        // cooperative mode.
        pThread->DisablePreemptiveMode();
#endif // FEATURE_NATIVEAOT

        hr = GCHeapUtilities::GetGCHeap()->GarbageCollect(
            -1,     // all generations should be collected
            FALSE,  // low_memory_p
            collection_blocking);

#ifdef FEATURE_NATIVEAOT
        // In case this thread (generated by the ETW OS APIs) hangs around a while,
        // better stick it back into preemptive mode, so it doesn't block any other GCs
        pThread->EnablePreemptiveMode();
#else   // !FEATURE_NATIVEAOT
    }
EX_CATCH{ }
EX_END_CATCH(RethrowCorruptingExceptions);
#endif // FEATURE_NATIVEAOT

return hr;
}


//---------------------------------------------------------------------------------------
// BulkTypeValue / BulkTypeEventLogger: These take care of batching up types so they can
// be logged via ETW in bulk
//---------------------------------------------------------------------------------------

BulkTypeValue::BulkTypeValue() : cTypeParameters(0), rgTypeParameters()
#ifdef FEATURE_NATIVEAOT
, ullSingleTypeParameter(0)
#else // FEATURE_NATIVEAOT
, sName()
#endif // FEATURE_NATIVEAOT
{
    LIMITED_METHOD_CONTRACT;
    ZeroMemory(&fixedSizedData, sizeof(fixedSizedData));
}

//---------------------------------------------------------------------------------------
//
// Clears a BulkTypeValue so it can be reused after the buffer is flushed to ETW
//

void BulkTypeValue::Clear()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ZeroMemory(&fixedSizedData, sizeof(fixedSizedData));
    cTypeParameters = 0;
#ifdef FEATURE_NATIVEAOT
    ullSingleTypeParameter = 0;
    rgTypeParameters.Release();
#else // FEATURE_NATIVEAOT
    sName.Clear();
    rgTypeParameters.Clear();
#endif // FEATURE_NATIVEAOT
}

//---------------------------------------------------------------------------------------
//
// Fire an ETW event for all the types we batched so far, and then reset our state
// so we can start batching new types at the beginning of the array.
//
//

void BulkTypeEventLogger::FireBulkTypeEvent()
{
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
    LIMITED_METHOD_CONTRACT;

    if (m_nBulkTypeValueCount == 0)
    {
        // No types were batched up, so nothing to send
        return;
    }

    // Normally, we'd use the MC-generated FireEtwBulkType for all this gunk, but
    // it's insufficient as the bulk type event is too complex (arrays of structs of
    // varying size). So we directly log the event via EventDataDescCreate and
    // EventWrite

    // We use one descriptor for the count + one for the ClrInstanceID + 4
    // per batched type (to include fixed-size data + name + param count + param
    // array).  But the system limit of 128 descriptors per event kicks in way
    // before the 64K event size limit, and we already limit our batch size
    // (m_nBulkTypeValueCount) to stay within the 128 descriptor limit.
    EVENT_DATA_DESCRIPTOR EventData[128];
    UINT16 nClrInstanceID = GetClrInstanceId();

    UINT iDesc = 0;

    _ASSERTE(iDesc < _countof(EventData));
    EventDataDescCreate(&EventData[iDesc++], &m_nBulkTypeValueCount, sizeof(m_nBulkTypeValueCount));

    _ASSERTE(iDesc < _countof(EventData));
    EventDataDescCreate(&EventData[iDesc++], &nClrInstanceID, sizeof(nClrInstanceID));

    for (int iTypeData = 0; iTypeData < m_nBulkTypeValueCount; iTypeData++)
    {
        // Do fixed-size data as one bulk copy
        _ASSERTE(iDesc < _countof(EventData));
        EventDataDescCreate(
            &EventData[iDesc++],
            &(m_rgBulkTypeValues[iTypeData].fixedSizedData),
            sizeof(m_rgBulkTypeValues[iTypeData].fixedSizedData));

        // Do var-sized data individually per field

        // Type name (nonexistent and thus empty on FEATURE_NATIVEAOT)
        _ASSERTE(iDesc < _countof(EventData));
#ifdef FEATURE_NATIVEAOT
        EventDataDescCreate(&EventData[iDesc++], L"", sizeof(WCHAR));
#else   // FEATURE_NATIVEAOT
        LPCWSTR wszName = m_rgBulkTypeValues[iTypeData].sName.GetUnicode();
        EventDataDescCreate(
            &EventData[iDesc++],
            (wszName == NULL) ? L"" : wszName,
            (wszName == NULL) ? sizeof(WCHAR) : (m_rgBulkTypeValues[iTypeData].sName.GetCount() + 1) * sizeof(WCHAR));
#endif // FEATURE_NATIVEAOT

        // Type parameter count
#ifndef FEATURE_NATIVEAOT
        m_rgBulkTypeValues[iTypeData].cTypeParameters = m_rgBulkTypeValues[iTypeData].rgTypeParameters.GetCount();
#endif // FEATURE_NATIVEAOT
        _ASSERTE(iDesc < _countof(EventData));
        EventDataDescCreate(
            &EventData[iDesc++],
            &(m_rgBulkTypeValues[iTypeData].cTypeParameters),
            sizeof(m_rgBulkTypeValues[iTypeData].cTypeParameters));

        // Type parameter array
        if (m_rgBulkTypeValues[iTypeData].cTypeParameters > 0)
        {
            _ASSERTE(iDesc < _countof(EventData));
            EventDataDescCreate(
                &EventData[iDesc++],
#ifdef FEATURE_NATIVEAOT
                ((m_rgBulkTypeValues[iTypeData].cTypeParameters == 1) ?
                    &(m_rgBulkTypeValues[iTypeData].ullSingleTypeParameter) :
                    (ULONGLONG*)(m_rgBulkTypeValues[iTypeData].rgTypeParameters)),
#else
                m_rgBulkTypeValues[iTypeData].rgTypeParameters.GetElements(),
#endif
                sizeof(ULONGLONG) * m_rgBulkTypeValues[iTypeData].cTypeParameters);
        }
    }

    Win32EventWrite(Microsoft_Windows_DotNETRuntimeHandle, &BulkType, iDesc, EventData);

    // Reset state
    m_nBulkTypeValueCount = 0;
    m_nBulkTypeValueByteCount = 0;
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT
}

#ifndef FEATURE_NATIVEAOT

//---------------------------------------------------------------------------------------
//
// Batches a single type into the array, flushing the array to ETW if it fills up. Most
// interaction with the type system (to analyze the type) is done here. This does not
// recursively batch up any parameter types (for arrays or generics), but does add their
// TypeHandles to the rgTypeParameters array. LogTypeAndParameters is responsible for
// initiating any recursive calls to deal with type parameters.
//
// Arguments:
//      th - TypeHandle to batch
//
// Return Value:
//      Index into array of where this type got batched. -1 if there was a failure.
//

int BulkTypeEventLogger::LogSingleType(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;  // some of the type system stuff can take locks
    }
    CONTRACTL_END;

    // If there's no room for another type, flush what we've got
    if (m_nBulkTypeValueCount == _countof(m_rgBulkTypeValues))
    {
        FireBulkTypeEvent();
    }

    _ASSERTE(m_nBulkTypeValueCount < _countof(m_rgBulkTypeValues));

    if (!th.IsTypeDesc() && th.GetMethodTable()->IsArray())
    {
        _ASSERTE(!"BulkTypeEventLogger::LogSingleType called with MethodTable array");
        return -1;
    }

    BulkTypeValue* pVal = &m_rgBulkTypeValues[m_nBulkTypeValueCount];

    // Clear out pVal before filling it out (array elements can get reused if there
    // are enough types that we need to flush to multiple events).  Clearing the
    // contained SBuffer can throw, so deal with exceptions
    BOOL fSucceeded = FALSE;
    EX_TRY
    {
        pVal->Clear();
        fSucceeded = TRUE;
    }
        EX_CATCH
    {
        fSucceeded = FALSE;
    }
    EX_END_CATCH(RethrowCorruptingExceptions);
    if (!fSucceeded)
        return -1;

    pVal->fixedSizedData.TypeID = (ULONGLONG)th.AsTAddr();
    pVal->fixedSizedData.ModuleID = (ULONGLONG)(TADDR)th.GetModule();
    pVal->fixedSizedData.TypeNameID = (th.GetMethodTable() == NULL) ? 0 : th.GetCl();
    pVal->fixedSizedData.Flags = 0;
    pVal->fixedSizedData.CorElementType = (BYTE)th.GetInternalCorElementType();

    if (th.IsArray())
    {
        // Normal typedesc array
        pVal->fixedSizedData.Flags |= kEtwTypeFlagsArray;

        // Fetch TypeHandle of array elements
        fSucceeded = FALSE;
        EX_TRY
        {
            pVal->rgTypeParameters.Append((ULONGLONG)th.AsArray()->GetArrayElementTypeHandle().AsTAddr());
            fSucceeded = TRUE;
        }
            EX_CATCH
        {
            fSucceeded = FALSE;
        }
        EX_END_CATCH(RethrowCorruptingExceptions);
        if (!fSucceeded)
            return -1;
    }
    else if (th.IsTypeDesc())
    {
        // Non-array Typedescs
        PTR_TypeDesc pTypeDesc = th.AsTypeDesc();
        if (pTypeDesc->HasTypeParam())
        {
            fSucceeded = FALSE;
            EX_TRY
            {
                pVal->rgTypeParameters.Append((ULONGLONG)pTypeDesc->GetTypeParam().AsTAddr());
                fSucceeded = TRUE;
            }
                EX_CATCH
            {
                fSucceeded = FALSE;
            }
            EX_END_CATCH(RethrowCorruptingExceptions);
            if (!fSucceeded)
                return -1;
        }
    }
    else
    {
        // Non-array MethodTable

        PTR_MethodTable pMT = th.AsMethodTable();

        // Make CorElementType more specific if this is a string MT
        if (pMT->IsString())
        {
            pVal->fixedSizedData.CorElementType = ELEMENT_TYPE_STRING;
        }
        else if (pMT->IsObjectClass())
        {
            pVal->fixedSizedData.CorElementType = ELEMENT_TYPE_OBJECT;
        }

        // Generic arguments
        DWORD cTypeParameters = pMT->GetNumGenericArgs();
        if (cTypeParameters > 0)
        {
            Instantiation inst = pMT->GetInstantiation();
            fSucceeded = FALSE;
            EX_TRY
            {
                for (DWORD i = 0; i < cTypeParameters; i++)
                {
                    pVal->rgTypeParameters.Append((ULONGLONG)inst[i].AsTAddr());
                }
                fSucceeded = TRUE;
            }
                EX_CATCH
            {
                fSucceeded = FALSE;
            }
            EX_END_CATCH(RethrowCorruptingExceptions);
            if (!fSucceeded)
                return -1;
        }

        if (pMT->HasFinalizer())
        {
            pVal->fixedSizedData.Flags |= kEtwTypeFlagsFinalizable;
        }
        if (pMT->IsDelegate())
        {
            pVal->fixedSizedData.Flags |= kEtwTypeFlagsDelegate;
        }
        if (pMT->IsComObjectType())
        {
            pVal->fixedSizedData.Flags |= kEtwTypeFlagsExternallyImplementedCOMObject;
        }
    }

    // If the profiler wants it, construct a name.  Always normalize the string (even if
    // type names are not requested) so that calls to sName.GetCount() can't throw
    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_GCHEAPANDTYPENAMES_KEYWORD))
        {
            th.GetName(pVal->sName);
        }
        pVal->sName.Normalize();
    }
        EX_CATCH
    {
        // If this failed, the name remains empty, which is ok; the event just
        // won't have a name in it.
        pVal->sName.Clear();
    }
    EX_END_CATCH(RethrowCorruptingExceptions);

    // Now that we know the full size of this type's data, see if it fits in our
    // batch or whether we need to flush

    int cbVal = pVal->GetByteCountInEvent();
    if (cbVal > kMaxBytesTypeValues)
    {
        // This type is apparently so huge, it's too big to squeeze into an event, even
        // if it were the only type batched in the whole event.  Bail
        _ASSERTE(!"Type too big to log via ETW");
        return -1;
    }

    if (m_nBulkTypeValueByteCount + cbVal > kMaxBytesTypeValues)
    {
        // Although this type fits into the array, its size is so big that the entire
        // array can't be logged via ETW. So flush the array, and start over by
        // calling ourselves--this refetches the type info and puts it at the
        // beginning of the array.  Since we know this type is small enough to be
        // batched into an event on its own, this recursive call will not try to
        // call itself again.
        FireBulkTypeEvent();
        return LogSingleType(th);
    }

    // The type fits into the batch, so update our state
    m_nBulkTypeValueCount++;
    m_nBulkTypeValueByteCount += cbVal;
    return m_nBulkTypeValueCount - 1;       // Index of type we just added
}

void BulkTypeEventLogger::Cleanup() {}

//---------------------------------------------------------------------------------------
//
// High-level method to batch a type and (recursively) its type parameters, flushing to
// ETW as needed.  This is called by (static)
// ETW::TypeSystemLog::LogTypeAndParametersIfNecessary, which is what clients use to log
// type events
//
// Arguments:
//      * thAsAddr - Type to batch
//      * typeLogBehavior - Reminder of whether the type system log lock is held
//          (useful if we need to recursively call back into TypeSystemLog), and whether
//          we even care to check if the type was already logged
//

void BulkTypeEventLogger::LogTypeAndParameters(ULONGLONG thAsAddr, ETW::TypeSystemLog::TypeLogBehavior typeLogBehavior)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;  // LogSingleType can take locks
    }
    CONTRACTL_END;

    TypeHandle th = TypeHandle::FromTAddr((TADDR)thAsAddr);

    // Batch up this type.  This grabs useful info about the type, including any
    // type parameters it may have, and sticks it in m_rgBulkTypeValues
    int iBulkTypeEventData = LogSingleType(th);
    if (iBulkTypeEventData == -1)
    {
        // There was a failure trying to log the type, so don't bother with its type
        // parameters
        return;
    }

    // Look at the type info we just batched, so we can get the type parameters
    BulkTypeValue* pVal = &m_rgBulkTypeValues[iBulkTypeEventData];

    // We're about to recursively call ourselves for the type parameters, so make a
    // local copy of their type handles first (else, as we log them we could flush
    // and clear out m_rgBulkTypeValues, thus trashing pVal)

    StackSArray<ULONGLONG> rgTypeParameters;
    DWORD cParams = pVal->rgTypeParameters.GetCount();

    BOOL fSucceeded = FALSE;
    EX_TRY
    {
        for (COUNT_T i = 0; i < cParams; i++)
        {
            rgTypeParameters.Append(pVal->rgTypeParameters[i]);
        }
        fSucceeded = TRUE;
    }
        EX_CATCH
    {
        fSucceeded = FALSE;
    }
    EX_END_CATCH(RethrowCorruptingExceptions);
    if (!fSucceeded)
        return;

    // Before we recurse, adjust the special-cased type-log behavior that allows a
    // top-level type to be logged without lookup, but still requires lookups to avoid
    // dupes of type parameters
    if (typeLogBehavior == ETW::TypeSystemLog::kTypeLogBehaviorAssumeLockAndAlwaysLogTopLevelType)
        typeLogBehavior = ETW::TypeSystemLog::kTypeLogBehaviorAssumeLockAndLogIfFirstTime;

    // Recursively log any referenced parameter types
    for (COUNT_T i = 0; i < cParams; i++)
    {
        ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(this, rgTypeParameters[i], typeLogBehavior);
    }
}

#endif // FEATURE_NATIVEAOT

// Holds state that batches of roots, nodes, edges, and types as the GC walks the heap
// at the end of a collection.
class EtwGcHeapDumpContext
{
public:
    // An instance of EtwGcHeapDumpContext is dynamically allocated and stored inside of
    // ProfilingScanContext and ProfilerWalkHeapContext, which are context structures
    // that the GC heap walker sends back to the callbacks. This method is passed a
    // pointer to ProfilingScanContext::pvEtwContext or
    // ProfilerWalkHeapContext::pvEtwContext; if non-NULL it gets returned; else, a new
    // EtwGcHeapDumpContext is allocated, stored in that pointer, and then returned.
    // Callers should test for NULL, which can be returned if out of memory
    static EtwGcHeapDumpContext* GetOrCreateInGCContext(LPVOID* ppvEtwContext)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ppvEtwContext != NULL);

        EtwGcHeapDumpContext* pContext = (EtwGcHeapDumpContext*)*ppvEtwContext;
        if (pContext == NULL)
        {
            pContext = new (nothrow) EtwGcHeapDumpContext;
            *ppvEtwContext = pContext;
        }
        return pContext;
    }

    EtwGcHeapDumpContext() :
        iCurBulkRootEdge(0),
        iCurBulkRootConditionalWeakTableElementEdge(0),
        iCurBulkNodeEvent(0),
        iCurBulkEdgeEvent(0),
        bulkTypeEventLogger()
    {
        LIMITED_METHOD_CONTRACT;
        ClearRootEdges();
        ClearRootConditionalWeakTableElementEdges();
        ClearNodes();
        ClearEdges();
    }

    // These helpers clear the individual buffers, for use after a flush and on
    // construction.  They intentionally leave the indices (iCur*) alone, since they
    // persist across flushes within a GC

    void ClearRootEdges()
    {
        LIMITED_METHOD_CONTRACT;
        cGcBulkRootEdges = 0;
        ZeroMemory(rgGcBulkRootEdges, sizeof(rgGcBulkRootEdges));
    }

    void ClearRootConditionalWeakTableElementEdges()
    {
        LIMITED_METHOD_CONTRACT;
        cGCBulkRootConditionalWeakTableElementEdges = 0;
        ZeroMemory(rgGCBulkRootConditionalWeakTableElementEdges, sizeof(rgGCBulkRootConditionalWeakTableElementEdges));
    }

    void ClearNodes()
    {
        LIMITED_METHOD_CONTRACT;
        cGcBulkNodeValues = 0;
        ZeroMemory(rgGcBulkNodeValues, sizeof(rgGcBulkNodeValues));
    }

    void ClearEdges()
    {
        LIMITED_METHOD_CONTRACT;
        cGcBulkEdgeValues = 0;
        ZeroMemory(rgGcBulkEdgeValues, sizeof(rgGcBulkEdgeValues));
    }

    //---------------------------------------------------------------------------------------
    // GCBulkRootEdge
    //
    // A "root edge" is the relationship between a source "GCRootID" (i.e., stack
    // variable, handle, static, etc.) and the target "RootedNodeAddress" (the managed
    // object that gets rooted).
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkRootEdge event
    UINT iCurBulkRootEdge;

    // Number of root edges currently filled out in rgGcBulkRootEdges array
    UINT cGcBulkRootEdges;

    // Struct array containing the primary data for each GCBulkRootEdge event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the root edge data)
    EventStructGCBulkRootEdgeValue rgGcBulkRootEdges[(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkRootEdgeValue)];


    //---------------------------------------------------------------------------------------
    // GCBulkRootConditionalWeakTableElementEdge
    //
    // These describe dependent handles, which simulate an edge connecting a key NodeID
    // to a value NodeID.
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkRootConditionalWeakTableElementEdge event
    UINT iCurBulkRootConditionalWeakTableElementEdge;

    // Number of root edges currently filled out in rgGCBulkRootConditionalWeakTableElementEdges array
    UINT cGCBulkRootConditionalWeakTableElementEdges;

    // Struct array containing the primary data for each GCBulkRootConditionalWeakTableElementEdge event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the root edge data)
    EventStructGCBulkRootConditionalWeakTableElementEdgeValue rgGCBulkRootConditionalWeakTableElementEdges
        [(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkRootConditionalWeakTableElementEdgeValue)];

    //---------------------------------------------------------------------------------------
    // GCBulkNode
    //
    // A "node" is ANY managed object sitting on the heap, including RootedNodeAddresses
    // as well as leaf nodes.
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkNode event
    UINT iCurBulkNodeEvent;

    // Number of nodes currently filled out in rgGcBulkNodeValues array
    UINT cGcBulkNodeValues;

    // Struct array containing the primary data for each GCBulkNode event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the node data)
    EventStructGCBulkNodeValue rgGcBulkNodeValues[(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkNodeValue)];

    //---------------------------------------------------------------------------------------
    // GCBulkEdge
    //
    // An "edge" is the relationship between a source node and its referenced target
    // node. Edges are reported in bulk, separately from Nodes, but it is expected that
    // the consumer read the Node and Edge streams together. One takes the first node
    // from the Node stream, and then reads EdgeCount entries in the Edge stream, telling
    // you all of that Node's targets. Then, one takes the next node in the Node stream,
    // and reads the next entries in the Edge stream (using this Node's EdgeCount to
    // determine how many) to find all of its targets. This continues on until the Node
    // and Edge streams have been fully read.
    //
    // GCBulkRootEdges are not duplicated in the GCBulkEdge events. GCBulkEdge events
    // begin at the GCBulkRootEdge.RootedNodeAddress and move forward.
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkEdge event
    UINT iCurBulkEdgeEvent;

    // Number of nodes currently filled out in rgGcBulkEdgeValues array
    UINT cGcBulkEdgeValues;

    // Struct array containing the primary data for each GCBulkEdge event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the edge data)
    EventStructGCBulkEdgeValue rgGcBulkEdgeValues[(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkEdgeValue)];


    //---------------------------------------------------------------------------------------
    // BulkType
    //
    // Types are a bit more complicated to batch up, since their data is of varying
    // size.  BulkTypeEventLogger takes care of the pesky details for us
    //---------------------------------------------------------------------------------------

    BulkTypeEventLogger bulkTypeEventLogger;
};



//---------------------------------------------------------------------------------------
//
// Called during a heap walk for each root reference encountered.  Batches up the root in
// the ETW context
//
// Arguments:
//      * pvHandle - If the root is a handle, this points to the handle
//      * pRootedNode - Points to object that is rooted
//      * pSecondaryNodeForDependentHandle - For dependent handles, this is the
//          secondary object
//      * fDependentHandle - nonzero iff this is for a dependent handle
//      * profilingScanContext - The shared profapi/etw context built up during the heap walk.
//      * dwGCFlags - Bitmask of "GC_"-style flags set by GC
//      * rootFlags - Bitmask of EtwGCRootFlags describing the root
//

// static
void ETW::GCLog::RootReference(
    LPVOID pvHandle,
    Object* pRootedNode,
    Object* pSecondaryNodeForDependentHandle,
    BOOL fDependentHandle,
    ProfilingScanContext* profilingScanContext,
    DWORD dwGCFlags,
    DWORD rootFlags)
{
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
    LIMITED_METHOD_CONTRACT;

    if (pRootedNode == NULL)
        return;

    EtwGcHeapDumpContext* pContext =
        EtwGcHeapDumpContext::GetOrCreateInGCContext(&profilingScanContext->pvEtwContext);
    if (pContext == NULL)
        return;

    // Determine root kind, root ID, and handle-specific flags
    LPVOID pvRootID = NULL;
    BYTE nRootKind = (BYTE)profilingScanContext->dwEtwRootKind;
    switch (nRootKind)
    {
    case kEtwGCRootKindStack:
#ifndef FEATURE_NATIVEAOT
        pvRootID = profilingScanContext->pMD;
#endif // !FEATURE_NATIVEAOT
        break;

    case kEtwGCRootKindHandle:
        pvRootID = pvHandle;
        break;

    case kEtwGCRootKindFinalizer:
        _ASSERTE(pvRootID == NULL);
        break;

    case kEtwGCRootKindOther:
    default:
        _ASSERTE(nRootKind == kEtwGCRootKindOther);
        _ASSERTE(pvRootID == NULL);
        break;
    }

    // Convert GC root flags to ETW root flags
    if (dwGCFlags & GC_CALL_INTERIOR)
        rootFlags |= kEtwGCRootFlagsInterior;
    if (dwGCFlags & GC_CALL_PINNED)
        rootFlags |= kEtwGCRootFlagsPinning;

    // Add root edge to appropriate buffer
    if (fDependentHandle)
    {
        _ASSERTE(pContext->cGCBulkRootConditionalWeakTableElementEdges <
            _countof(pContext->rgGCBulkRootConditionalWeakTableElementEdges));
        EventStructGCBulkRootConditionalWeakTableElementEdgeValue* pRCWTEEdgeValue =
            &pContext->rgGCBulkRootConditionalWeakTableElementEdges[pContext->cGCBulkRootConditionalWeakTableElementEdges];
        pRCWTEEdgeValue->GCKeyNodeID = pRootedNode;
        pRCWTEEdgeValue->GCValueNodeID = pSecondaryNodeForDependentHandle;
        pRCWTEEdgeValue->GCRootID = pvRootID;
        pContext->cGCBulkRootConditionalWeakTableElementEdges++;

        // If RCWTE edge buffer is now full, empty it into ETW
        if (pContext->cGCBulkRootConditionalWeakTableElementEdges ==
            _countof(pContext->rgGCBulkRootConditionalWeakTableElementEdges))
        {
            FireEtwGCBulkRootConditionalWeakTableElementEdge(
                pContext->iCurBulkRootConditionalWeakTableElementEdge,
                pContext->cGCBulkRootConditionalWeakTableElementEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]),
                &pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]);

            pContext->iCurBulkRootConditionalWeakTableElementEdge++;
            pContext->ClearRootConditionalWeakTableElementEdges();
        }
    }
    else
    {
        _ASSERTE(pContext->cGcBulkRootEdges < _countof(pContext->rgGcBulkRootEdges));
        EventStructGCBulkRootEdgeValue* pBulkRootEdgeValue = &pContext->rgGcBulkRootEdges[pContext->cGcBulkRootEdges];
        pBulkRootEdgeValue->RootedNodeAddress = pRootedNode;
        pBulkRootEdgeValue->GCRootKind = nRootKind;
        pBulkRootEdgeValue->GCRootFlag = rootFlags;
        pBulkRootEdgeValue->GCRootID = pvRootID;
        pContext->cGcBulkRootEdges++;

        // If root edge buffer is now full, empty it into ETW
        if (pContext->cGcBulkRootEdges == _countof(pContext->rgGcBulkRootEdges))
        {
            FireEtwGCBulkRootEdge(
                pContext->iCurBulkRootEdge,
                pContext->cGcBulkRootEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkRootEdges[0]),
                &pContext->rgGcBulkRootEdges[0]);

            pContext->iCurBulkRootEdge++;
            pContext->ClearRootEdges();
        }
    }
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT
}


//---------------------------------------------------------------------------------------
//
// Called during a heap walk for each object reference encountered.  Batches up the
// corresponding node, edges, and type data for the ETW events.
//
// Arguments:
//      * profilerWalkHeapContext - The shared profapi/etw context built up during the heap walk.
//      * pObjReferenceSource - Object doing the pointing
//      * typeID - Type of pObjReferenceSource
//      * fDependentHandle - nonzero iff this is for a dependent handle
//      * cRefs - Count of objects being pointed to
//      * rgObjReferenceTargets - Array of objects being pointed to
//

// static
void ETW::GCLog::ObjectReference(
    ProfilerWalkHeapContext* profilerWalkHeapContext,
    Object* pObjReferenceSource,
    ULONGLONG typeID,
    ULONGLONG cRefs,
    Object** rgObjReferenceTargets)
{
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        // LogTypeAndParametersIfNecessary can take a lock
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    EtwGcHeapDumpContext* pContext =
        EtwGcHeapDumpContext::GetOrCreateInGCContext(&profilerWalkHeapContext->pvEtwContext);
    if (pContext == NULL)
        return;

    //---------------------------------------------------------------------------------------
    //    GCBulkNode events
    //---------------------------------------------------------------------------------------

    // Add Node (pObjReferenceSource) to buffer
    _ASSERTE(pContext->cGcBulkNodeValues < _countof(pContext->rgGcBulkNodeValues));
    EventStructGCBulkNodeValue* pBulkNodeValue = &pContext->rgGcBulkNodeValues[pContext->cGcBulkNodeValues];
    pBulkNodeValue->Address = pObjReferenceSource;
    pBulkNodeValue->Size = pObjReferenceSource->GetSize();
    pBulkNodeValue->TypeID = typeID;
    pBulkNodeValue->EdgeCount = cRefs;
    pContext->cGcBulkNodeValues++;

    // If Node buffer is now full, empty it into ETW
    if (pContext->cGcBulkNodeValues == _countof(pContext->rgGcBulkNodeValues))
    {
        FireEtwGCBulkNode(
            pContext->iCurBulkNodeEvent,
            pContext->cGcBulkNodeValues,
            GetClrInstanceId(),
            sizeof(pContext->rgGcBulkNodeValues[0]),
            &pContext->rgGcBulkNodeValues[0]);

        pContext->iCurBulkNodeEvent++;
        pContext->ClearNodes();
    }

    //---------------------------------------------------------------------------------------
    //    BulkType events
    //---------------------------------------------------------------------------------------

    // We send type information as necessary--only for nodes, and only for nodes that we
    // haven't already sent type info for
    if (typeID != 0)
    {
        ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(
            &pContext->bulkTypeEventLogger,     // Batch up this type with others to minimize events
            typeID,

            // During heap walk, GC holds the lock for us, so we can directly enter the
            // hash to see if the type has already been logged
            ETW::TypeSystemLog::kTypeLogBehaviorAssumeLockAndLogIfFirstTime
        );
    }

    //---------------------------------------------------------------------------------------
    //    GCBulkEdge events
    //---------------------------------------------------------------------------------------

    // Add Edges (rgObjReferenceTargets) to buffer. Buffer could fill up before all edges
    // are added (it could even fill up multiple times during this one call if there are
    // a lot of edges), so empty Edge buffer into ETW as we go along, as many times as we
    // need.

    for (ULONGLONG i = 0; i < cRefs; i++)
    {
        _ASSERTE(pContext->cGcBulkEdgeValues < _countof(pContext->rgGcBulkEdgeValues));
        EventStructGCBulkEdgeValue* pBulkEdgeValue = &pContext->rgGcBulkEdgeValues[pContext->cGcBulkEdgeValues];
        pBulkEdgeValue->Value = rgObjReferenceTargets[i];
        // FUTURE: ReferencingFieldID
        pBulkEdgeValue->ReferencingFieldID = 0;
        pContext->cGcBulkEdgeValues++;

        // If Edge buffer is now full, empty it into ETW
        if (pContext->cGcBulkEdgeValues == _countof(pContext->rgGcBulkEdgeValues))
        {
            FireEtwGCBulkEdge(
                pContext->iCurBulkEdgeEvent,
                pContext->cGcBulkEdgeValues,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkEdgeValues[0]),
                &pContext->rgGcBulkEdgeValues[0]);

            pContext->iCurBulkEdgeEvent++;
            pContext->ClearEdges();
        }
    }
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT
}

//---------------------------------------------------------------------------------------
//
// Called by GC at end of heap dump to give us a convenient time to flush any remaining
// buffers of data to ETW
//
// Arguments:
//      profilerWalkHeapContext - Context containing data we've batched up
//

// static
void ETW::GCLog::EndHeapDump(ProfilerWalkHeapContext* profilerWalkHeapContext)
{
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
    LIMITED_METHOD_CONTRACT;

    // If context isn't already set up for us, then we haven't been collecting any data
    // for ETW events.
    EtwGcHeapDumpContext* pContext = (EtwGcHeapDumpContext*)profilerWalkHeapContext->pvEtwContext;
    if (pContext == NULL)
        return;

    // If the GC events are enabled, flush any remaining root, node, and / or edge data
    if (ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_GCHEAPDUMP_KEYWORD))
    {
        if (pContext->cGcBulkRootEdges > 0)
        {
            FireEtwGCBulkRootEdge(
                pContext->iCurBulkRootEdge,
                pContext->cGcBulkRootEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkRootEdges[0]),
                &pContext->rgGcBulkRootEdges[0]);
        }

        if (pContext->cGCBulkRootConditionalWeakTableElementEdges > 0)
        {
            FireEtwGCBulkRootConditionalWeakTableElementEdge(
                pContext->iCurBulkRootConditionalWeakTableElementEdge,
                pContext->cGCBulkRootConditionalWeakTableElementEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]),
                &pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]);
        }

        if (pContext->cGcBulkNodeValues > 0)
        {
            FireEtwGCBulkNode(
                pContext->iCurBulkNodeEvent,
                pContext->cGcBulkNodeValues,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkNodeValues[0]),
                &pContext->rgGcBulkNodeValues[0]);
        }

        if (pContext->cGcBulkEdgeValues > 0)
        {
            FireEtwGCBulkEdge(
                pContext->iCurBulkEdgeEvent,
                pContext->cGcBulkEdgeValues,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkEdgeValues[0]),
                &pContext->rgGcBulkEdgeValues[0]);
        }
    }

    // Ditto for type events
    if (ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        pContext->bulkTypeEventLogger.FireBulkTypeEvent();
        pContext->bulkTypeEventLogger.Cleanup();
    }

    // Delete any GC state built up in the context
    profilerWalkHeapContext->pvEtwContext = NULL;
    delete pContext;
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT
}


#ifndef FEATURE_NATIVEAOT

//---------------------------------------------------------------------------------------
//
// Helper to send public finalize object & type events, and private finalize object
// event.  If Type events are enabled, this will send the Type event for the finalized
// objects.  It will not be batched with other types (except type parameters, if any),
// and will not check if the Type has already been logged (may thus result in dupe
// logging of the Type).
//
// Arguments:
//      pMT - MT of object getting finalized
//      pObj - object getting finalized
//

// static
void ETW::GCLog::SendFinalizeObjectEvent(MethodTable* pMT, Object* pObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        // LogTypeAndParameters locks, and we take our own lock if typeLogBehavior says to
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Send public finalize object event, if it's enabled
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, FinalizeObject))
    {
        FireEtwFinalizeObject(pMT, pObj, GetClrInstanceId());

        // This function checks if type events are enabled; if so, it sends event for
        // finalized object's type (and parameter types, if any)
        ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(
            NULL,       // Not batching this type with others
            (TADDR)pMT,

            // Don't spend the time entering the lock and checking the hash table to see
            // if we've already logged the type; just log it (if type events are enabled).
            ETW::TypeSystemLog::kTypeLogBehaviorAlwaysLog
        );
    }

    // Send private finalize object event, if it's enabled
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, PrvFinalizeObject))
    {
        EX_TRY
        {
            DefineFullyQualifiedNameForClassWOnStack();
            FireEtwPrvFinalizeObject(pMT, pObj, GetClrInstanceId(), GetFullyQualifiedNameForClassNestedAwareW(pMT));
        }
            EX_CATCH
        {
        }
        EX_END_CATCH(RethrowCorruptingExceptions);
    }
}

DWORD ETW::ThreadLog::GetEtwThreadFlags(Thread* pThread)
{
    LIMITED_METHOD_CONTRACT;

    DWORD dwEtwThreadFlags = 0;

    if (pThread->IsThreadPoolThread())
    {
        dwEtwThreadFlags |= kEtwThreadFlagThreadPoolWorker;
    }
    if (pThread->IsGCSpecial())
    {
        dwEtwThreadFlags |= kEtwThreadFlagGCSpecial;
    }
    if (IsGarbageCollectorFullyInitialized() &&
        (pThread == GCHeapUtilities::GetGCHeap()->GetFinalizerThread()))
    {
        dwEtwThreadFlags |= kEtwThreadFlagFinalizer;
    }

    return dwEtwThreadFlags;
}

void ETW::ThreadLog::FireThreadCreated(Thread* pThread)
{
    LIMITED_METHOD_CONTRACT;

    FireEtwThreadCreated(
        (ULONGLONG)pThread,
        (ULONGLONG)pThread->GetDomain(),
        GetEtwThreadFlags(pThread),
        pThread->GetThreadId(),
        pThread->GetOSThreadId(),
        GetClrInstanceId());
}

void ETW::ThreadLog::FireThreadDC(Thread* pThread)
{
    LIMITED_METHOD_CONTRACT;

    FireEtwThreadDC(
        (ULONGLONG)pThread,
        (ULONGLONG)pThread->GetDomain(),
        GetEtwThreadFlags(pThread),
        pThread->GetThreadId(),
        pThread->GetOSThreadId(),
        GetClrInstanceId());
}



// TypeSystemLog implementation
//
// We keep track of which TypeHandles have been logged, and stats on instances of these
// TypeHandles that have been allocated, by a hash table of hash tables. The outer hash
// table maps Module*'s to an inner hash table that contains all the TypeLoggingInfos for that
// Module*. Arranging things this way makes it easy to deal with Module unloads, as we
// can simply remove the corresponding inner hash table from the outer hash table.

// The following help define the "inner" hash table: a hash table of TypeLoggingInfos
// from a particular Module (key = TypeHandle, value = TypeLoggingInfo.

class LoggedTypesFromModuleTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ETW::TypeLoggingInfo> >
{
public:

    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef NoRemoveSHashTraits< DefaultSHashTraits<ETW::TypeLoggingInfo> > PARENT;
    typedef PARENT::element_t element_t;
    typedef PARENT::count_t count_t;

    typedef TypeHandle key_t;

    static key_t GetKey(const element_t& e)
    {
        LIMITED_METHOD_CONTRACT;
        return e.th;
    }

    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return (k1 == k2);
    }

    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)k.AsTAddr();
    }

    static bool IsNull(const element_t& e)
    {
        LIMITED_METHOD_CONTRACT;
        return (e.th.AsTAddr() == NULL);
    }

    static const element_t Null()
    {
        LIMITED_METHOD_CONTRACT;
        return ETW::TypeLoggingInfo(NULL);
    }
};
typedef SHash<LoggedTypesFromModuleTraits> LoggedTypesFromModuleHash;

// The inner hash table is housed inside this class, which acts as an entry in the outer
// hash table.
class ETW::LoggedTypesFromModule
{
public:
    Module* pModule;
    LoggedTypesFromModuleHash loggedTypesFromModuleHash;

    // These are used by the outer hash table (mapping Module*'s to instances of
    // LoggedTypesFromModule).
    static COUNT_T Hash(Module* pModule)
    {
        LIMITED_METHOD_CONTRACT;
        return (COUNT_T)(SIZE_T)pModule;
    }
    Module* GetKey()
    {
        LIMITED_METHOD_CONTRACT;
        return pModule;
    }

    LoggedTypesFromModule(Module* pModuleParam) : loggedTypesFromModuleHash()
    {
        LIMITED_METHOD_CONTRACT;
        pModule = pModuleParam;
    }

    ~LoggedTypesFromModule()
    {
        LIMITED_METHOD_CONTRACT;
    }
};

// The following define the outer hash table (mapping Module*'s to instances of
// LoggedTypesFromModule).

class AllLoggedTypesTraits : public DefaultSHashTraits<ETW::LoggedTypesFromModule*>
{
public:

    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef DefaultSHashTraits<ETW::LoggedTypesFromModule*> PARENT;
    typedef PARENT::element_t element_t;
    typedef PARENT::count_t count_t;

    typedef Module* key_t;

    static key_t GetKey(const element_t& e)
    {
        LIMITED_METHOD_CONTRACT;
        return e->pModule;
    }

    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return (k1 == k2);
    }

    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)(size_t)k;
    }

    static bool IsNull(const element_t& e)
    {
        LIMITED_METHOD_CONTRACT;
        return (e == NULL);
    }

    static const element_t Null()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }
};

typedef SHash<AllLoggedTypesTraits> AllLoggedTypesHash;

// The outer hash table (mapping Module*'s to instances of LoggedTypesFromModule) is
// housed in this struct, which is dynamically allocated the first time we decide we need
// it.
struct AllLoggedTypes
{
public:
    // This Crst protects the entire outer & inner hash tables.  On a GC heap walk, it
    // is entered once for the duration of the walk, so that we can freely access the
    // hash tables during the walk.  On each object allocation, this Crst must be
    // entered individually each time.
    static CrstStatic s_cs;

    // The outer hash table (mapping Module*'s to instances of LoggedTypesFromModule)
    AllLoggedTypesHash allLoggedTypesHash;
};


CrstStatic AllLoggedTypes::s_cs;
AllLoggedTypes* ETW::TypeSystemLog::s_pAllLoggedTypes = NULL;
BOOL ETW::TypeSystemLog::s_fHeapAllocEventEnabledOnStartup = FALSE;
BOOL ETW::TypeSystemLog::s_fHeapAllocHighEventEnabledNow = FALSE;
BOOL ETW::TypeSystemLog::s_fHeapAllocLowEventEnabledNow = FALSE;
int ETW::TypeSystemLog::s_nCustomMsBetweenEvents = 0;


//---------------------------------------------------------------------------------------
//
// Initializes TypeSystemLog (specifically its crst).  Called just before ETW providers
// are registered with the OS
//
// Return Value:
//     HRESULT indicating success or failure
//

// static
HRESULT ETW::TypeSystemLog::PreRegistrationInit()
{
    LIMITED_METHOD_CONTRACT;

    if (!AllLoggedTypes::s_cs.InitNoThrow(
        CrstEtwTypeLogHash,
        CRST_UNSAFE_ANYMODE))       // This lock is taken during a GC while walking the heap
    {
        return E_FAIL;
    }

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Initializes TypeSystemLog (specifically its crst).  Called just after ETW providers
// are registered with the OS
//
// Return Value:
//     HRESULT indicating success or failure
//

// static
void ETW::TypeSystemLog::PostRegistrationInit()
{
    LIMITED_METHOD_CONTRACT;

    // Initialize our "current state" BOOLs that remember if low or high allocation
    // sampling is turned on
    BOOL s_fHeapAllocLowEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCLOW_KEYWORD);
    BOOL s_fHeapAllocHighEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCHIGH_KEYWORD);

    // Snapshot the current state of the object allocated keyword (on startup), and rely
    // on this snapshot for the rest of the process run. Since these events require the
    // slow alloc JIT helper to be enabled, and that can only be done on startup, we
    // remember in this BOOL that we did so, so that we can prevent the object allocated
    // event from being fired if the fast allocation helper were enabled but had to
    // degrade down to the slow helper (e.g., thread ran over its allocation limit). This
    // keeps things consistent.
    s_fHeapAllocEventEnabledOnStartup = (s_fHeapAllocLowEventEnabledNow || s_fHeapAllocHighEventEnabledNow);

    if (s_fHeapAllocEventEnabledOnStartup)
    {
        // Determine if a COMPLUS env var is overriding the frequency for the sampled
        // object allocated events

        // Config value intentionally typed as string, b/c DWORD interpretation is hard-coded
        // to hex, which is not what the user would expect.  This way I can force the
        // conversion to use decimal.
        NewArrayHolder<WCHAR> wszCustomObjectAllocationEventsPerTypePerSec(NULL);
        if (FAILED(CLRConfig::GetConfigValue(
            CLRConfig::UNSUPPORTED_ETW_ObjectAllocationEventsPerTypePerSec,
            &wszCustomObjectAllocationEventsPerTypePerSec)) ||
            (wszCustomObjectAllocationEventsPerTypePerSec == NULL))
        {
            return;
        }
        LPWSTR endPtr;
        DWORD dwCustomObjectAllocationEventsPerTypePerSec = wcstoul(
            wszCustomObjectAllocationEventsPerTypePerSec,
            &endPtr,
            10          // Base 10 conversion
        );

        if (dwCustomObjectAllocationEventsPerTypePerSec == ULONG_MAX)
            dwCustomObjectAllocationEventsPerTypePerSec = 0;
        if (dwCustomObjectAllocationEventsPerTypePerSec != 0)
        {
            // MsBetweenEvents = (1000 ms/sec) / (custom desired events/sec)
            s_nCustomMsBetweenEvents = 1000 / dwCustomObjectAllocationEventsPerTypePerSec;
        }
    }
}


//---------------------------------------------------------------------------------------
//
// Update object allocation sampling frequency and / or Type hash table contents based
// on what keywords were changed.
//

// static
void ETW::TypeSystemLog::OnKeywordsChanged()
{
    LIMITED_METHOD_CONTRACT;

    // If the desired frequency for the GCSampledObjectAllocation events has changed,
    // update our state.
    s_fHeapAllocLowEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCLOW_KEYWORD);
    s_fHeapAllocHighEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCHIGH_KEYWORD);

    // FUTURE: Would be nice here to log an error event if (s_fHeapAllocLowEventEnabledNow ||
    // s_fHeapAllocHighEventEnabledNow), but !s_fHeapAllocEventEnabledOnStartup

    // If the type events should be turned off, eliminate the hash tables that tracked
    // which types were logged. (If type events are turned back on later, we'll re-log
    // them all as we encounter them.) Note that all we can really test for is that the
    // Types keyword on the runtime provider is off. Not necessarily that it was on and
    // was just turned off with this request. But either way, TypeSystemLog can handle it
    // because it is extremely smart.
    if (!ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_TYPE_KEYWORD))
        OnTypesKeywordTurnedOff();
}


//---------------------------------------------------------------------------------------
//
// Based on keywords alone, determine the what the default sampling rate should be for
// object allocation events.  (This function does not consider any COMPLUS overrides for
// the sampling rate.)
//

// static
int ETW::TypeSystemLog::GetDefaultMsBetweenEvents()
{
    LIMITED_METHOD_CONTRACT;

    // We should only get here if the allocation event is enabled. In spirit, this assert
    // is correct, but a race could cause the assert to fire (if someone toggled the
    // event off after we decided that the event was on and we started down the path of
    // calculating statistics to fire the event). In such a case we'll end up returning
    // k_nDefaultMsBetweenEventsLow below, but next time we won't get here as we'll know
    // early enough not to fire the event.
    //_ASSERTE(IsHeapAllocEventEnabled());

    // MsBetweenEvents = (1000 ms/sec) / (desired events/sec)
    const int k_nDefaultMsBetweenEventsHigh = 1000 / 100;   // 100 events per type per sec
    const int k_nDefaultMsBetweenEventsLow = 1000 / 5;      // 5 events per type per sec

    // If both are set, High takes precedence
    if (s_fHeapAllocHighEventEnabledNow)
    {
        return k_nDefaultMsBetweenEventsHigh;
    }
    return k_nDefaultMsBetweenEventsLow;
}

//---------------------------------------------------------------------------------------
//
// Use this to decide whether to fire the object allocation event
//
// Return Value:
//      nonzero iff we should fire the event.
//

// static
BOOL ETW::TypeSystemLog::IsHeapAllocEventEnabled()
{
    LIMITED_METHOD_CONTRACT;

    return
        // Only fire the event if it was enabled at startup (and thus the slow-JIT new
        // helper is used in all cases)
        s_fHeapAllocEventEnabledOnStartup &&

        // AND a keyword is still enabled.  (Thus people can turn off the event
        // whenever they want; but they cannot turn it on unless it was also on at startup.)
        (s_fHeapAllocHighEventEnabledNow || s_fHeapAllocLowEventEnabledNow);
}

//---------------------------------------------------------------------------------------
//
// Helper that adds (or updates) the TypeLoggingInfo inside the inner hash table passed
// in.
//
// Arguments:
//      * pLoggedTypesFromModule - Inner hash table to update
//      * pTypeLoggingInfo - TypeLoggingInfo to store
//
// Return Value:
//      nonzero iff the add/replace was successful.
//
// Assumptions:
//     Caller must be holding the hash crst
//

// static
BOOL ETW::TypeSystemLog::AddOrReplaceTypeLoggingInfo(ETW::LoggedTypesFromModule* pLoggedTypesFromModule, const ETW::TypeLoggingInfo* pTypeLoggingInfo)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(GetHashCrst()->OwnedByCurrentThread());
    _ASSERTE(pLoggedTypesFromModule != NULL);

    BOOL fSucceeded = FALSE;
    EX_TRY
    {
        pLoggedTypesFromModule->loggedTypesFromModuleHash.AddOrReplace(*pTypeLoggingInfo);
        fSucceeded = TRUE;
    }
        EX_CATCH
    {
        fSucceeded = FALSE;
    }
    EX_END_CATCH(RethrowCorruptingExceptions);

    return fSucceeded;
}

//---------------------------------------------------------------------------------------
//
// Records stats about the object's allocation, and determines based on those stats whether
// to fires the high / low frequency GCSampledObjectAllocation ETW event
//
// Arguments:
//      * pObject - Allocated object to log
//      * th - TypeHandle for the object
//

// static
void ETW::TypeSystemLog::SendObjectAllocatedEvent(Object* pObject)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // No-op if the appropriate keywords were not enabled on startup (or we're not yet
    // started up)
    if (!s_fHeapAllocEventEnabledOnStartup || !g_fEEStarted)
        return;

    TypeHandle th = pObject->GetTypeHandle();

    SIZE_T size = pObject->GetSize();
    if (size < MIN_OBJECT_SIZE)
    {
        size = PtrAlign(size);
    }

    SIZE_T nTotalSizeForTypeSample = size;
    DWORD dwTickNow = GetTickCount();
    DWORD dwObjectCountForTypeSample = 0;

    // BLOCK: Hold the crst around the type stats hash table while we read and update
    // the type's stats
    {
        CrstHolder _crst(GetHashCrst());

        // Get stats for type
        TypeLoggingInfo typeLoggingInfo(NULL);
        LoggedTypesFromModule* pLoggedTypesFromModule = NULL;
        BOOL fCreatedNew = FALSE;
        typeLoggingInfo = LookupOrCreateTypeLoggingInfo(th, &fCreatedNew, &pLoggedTypesFromModule);
        if (typeLoggingInfo.th.IsNull())
            return;

        // Update stats with current allocation
        typeLoggingInfo.dwAllocsSkippedForSample++;
        typeLoggingInfo.cbIgnoredSizeForSample += size;

        // This is our filter. If we should ignore this alloc, then record our updated
        // our stats, and bail without sending the event. Note that we always log objects
        // over 10K in size.
        if (size < 10000 && typeLoggingInfo.dwAllocsSkippedForSample < typeLoggingInfo.dwAllocsToSkipPerSample)
        {
            // Update hash table's copy of type logging info with these values. Unfortunate that
            // we're doing another hash table lookup here. Could instead have used LookupPtr()
            // if it gave us back a non-const pointer, and then we could have updated in-place
            AddOrReplaceTypeLoggingInfo(pLoggedTypesFromModule, &typeLoggingInfo);
            if (fCreatedNew)
            {
                // Although we're skipping logging the allocation, we still need to log
                // the type (so it's available for resolving future allocation events to
                // their types).
                //
                // (See other call to LogTypeAndParametersIfNecessary further down for
                // more comments.)
                LogTypeAndParametersIfNecessary(
                    NULL,
                    th.AsTAddr(),
                    kTypeLogBehaviorAssumeLockAndAlwaysLogTopLevelType);
            }
            return;
        }

        // Based on observed allocation stats, adjust our sampling rate for this type

        typeLoggingInfo.dwAllocCountInCurrentBucket += typeLoggingInfo.dwAllocsSkippedForSample;
        int delta = (dwTickNow - typeLoggingInfo.dwTickOfCurrentTimeBucket) & 0x7FFFFFFF;	// make wrap around work.

        int nMinAllocPerMSec = typeLoggingInfo.dwAllocCountInCurrentBucket / 16;		// This is an underestimation of the true rate.
        if (delta >= 16 || (nMinAllocPerMSec > 2 && nMinAllocPerMSec > typeLoggingInfo.flAllocPerMSec * 1.5F))
        {
            float flNewAllocPerMSec = 0;
            if (delta >= 16)
            {
                // This is the normal case, our allocation rate is under control with the current throttling.
                flNewAllocPerMSec = ((float)typeLoggingInfo.dwAllocCountInCurrentBucket) / delta;
                // Do a exponential decay window that is 5 * max(16, AllocationInterval)
                typeLoggingInfo.flAllocPerMSec = 0.8F * typeLoggingInfo.flAllocPerMSec + 0.2F * flNewAllocPerMSec;
                typeLoggingInfo.dwTickOfCurrentTimeBucket = dwTickNow;
                typeLoggingInfo.dwAllocCountInCurrentBucket = 0;
            }
            else
            {
                flNewAllocPerMSec = (float)nMinAllocPerMSec;
                // This means the second clause above is true, which means our sampling rate is too low
                // so we need to throttle quickly.
                typeLoggingInfo.flAllocPerMSec = flNewAllocPerMSec;
            }


            // Obey the desired sampling rate, but don't ignore > 1000 allocations per second
            // per type
            int nDesiredMsBetweenEvents = (s_nCustomMsBetweenEvents == 0) ? GetDefaultMsBetweenEvents() : s_nCustomMsBetweenEvents;
            typeLoggingInfo.dwAllocsToSkipPerSample = min((int)(typeLoggingInfo.flAllocPerMSec * nDesiredMsBetweenEvents), 1000);
            if (typeLoggingInfo.dwAllocsToSkipPerSample == 1)
                typeLoggingInfo.dwAllocsToSkipPerSample = 0;
        }

        // We're logging this sample, so save the values we need into locals, and reset
        // our counts for the next sample.
        nTotalSizeForTypeSample = typeLoggingInfo.cbIgnoredSizeForSample;
        dwObjectCountForTypeSample = typeLoggingInfo.dwAllocsSkippedForSample;
        typeLoggingInfo.cbIgnoredSizeForSample = 0;
        typeLoggingInfo.dwAllocsSkippedForSample = 0;

        // Save updated stats into hash table
        if (!AddOrReplaceTypeLoggingInfo(pLoggedTypesFromModule, &typeLoggingInfo))
        {
            return;
        }

        // While we're still holding the crst, optionally log any relevant Types now (we may need
        // to reconsult the hash in here if there are any type parameters, though we can
        // optimize and NOT consult the hash for th itself).
        if (fCreatedNew)
        {
            // We were the ones to add the Type to the hash.  So it wasn't there before,
            // which means it hasn't been logged yet.
            LogTypeAndParametersIfNecessary(

                // No BulkTypeEventLogger, as we're not batching during a GC heap walk
                NULL,

                th.AsTAddr(),

                // We've determined the type is not yet logged, so no need to check
                kTypeLogBehaviorAssumeLockAndAlwaysLogTopLevelType);
        }
    }       // RELEASE: CrstHolder _crst(GetHashCrst());

    // Now log the allocation
    if (s_fHeapAllocHighEventEnabledNow)
    {
        FireEtwGCSampledObjectAllocationHigh(pObject, (LPVOID)th.AsTAddr(), dwObjectCountForTypeSample, nTotalSizeForTypeSample, GetClrInstanceId());
    }
    else
    {
        FireEtwGCSampledObjectAllocationLow(pObject, (LPVOID)th.AsTAddr(), dwObjectCountForTypeSample, nTotalSizeForTypeSample, GetClrInstanceId());
    }
}

//---------------------------------------------------------------------------------------
//
// Accessor for hash table crst
//
// Return Value:
//      hash table crst
//

// static
CrstBase* ETW::TypeSystemLog::GetHashCrst()
{
    LIMITED_METHOD_CONTRACT;
    return &AllLoggedTypes::s_cs;
}

//---------------------------------------------------------------------------------------
//
// Outermost level of ETW-type-logging.  Clients outside eventtrace.cpp call this to log
// a TypeHandle and (recursively) its type parameters when present.  This guy then calls
// into the appropriate BulkTypeEventLogger to do the batching and logging
//
// Arguments:
//      * pBulkTypeEventLogger - If our caller is keeping track of batched types, it
//          passes this to us so we can use it to batch the current type (GC heap walk
//          does this).  If this is NULL, no batching is going on (e.g., we're called on
//          object allocation, not a GC heal walk), in which case we create our own
//          temporary BulkTypeEventLogger.
//      * thAsAddr - TypeHandle to batch
//      * typeLogBehavior - Optimization to tell us we don't need to enter the
//          TypeSystemLog's crst, as the TypeSystemLog's hash table is already protected
//          by a prior acquisition of the crst by our caller.  (Or that we don't even
//          need to check the hash in the first place.)
//

// static
void ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(BulkTypeEventLogger* pLogger, ULONGLONG thAsAddr, TypeLogBehavior typeLogBehavior)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        // LogTypeAndParameters locks, and we take our own lock if typeLogBehavior says to
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (!ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        return;
    }

    TypeHandle th = TypeHandle::FromTAddr((TADDR)thAsAddr);
    if (!th.IsRestored())
    {
        return;
    }

    // Check to see if we've already logged this type.  If so, bail immediately.
    // Otherwise, mark that it's getting logged (by adding it to the hash), and fall
    // through to the logging code below.  If caller doesn't care, then don't even
    // check; just log the type
    BOOL fShouldLogType = ((typeLogBehavior == kTypeLogBehaviorAlwaysLog) ||
        (typeLogBehavior == kTypeLogBehaviorAssumeLockAndAlwaysLogTopLevelType)) ?
        TRUE :
        ((typeLogBehavior == kTypeLogBehaviorTakeLockAndLogIfFirstTime) ?
            ShouldLogType(th) :
            ShouldLogTypeNoLock(th));
    if (!fShouldLogType)
        return;

    if (pLogger == NULL)
    {
        // We're not batching this type against previous types (e.g., we're being called
        // on object allocate instead of a GC heap walk).  So create a temporary logger
        // on the stack.  If there are generic parameters that need to be logged, then
        // at least they'll get batched together with the type
        BulkTypeEventLogger logger;
        logger.LogTypeAndParameters(thAsAddr, typeLogBehavior);

        // Since this logger isn't being used to batch anything else, flush what we have
        logger.FireBulkTypeEvent();
    }
    else
    {
        // We are batching this type with others (e.g., we're being called at the end of
        // a GC on a heap walk).  So use the logger our caller set up for us.
        pLogger->LogTypeAndParameters(thAsAddr, typeLogBehavior);
    }
}


//---------------------------------------------------------------------------------------
//
// Same as code:ETW::TypeSystemLog::ShouldLogTypeNoLock but acquires the lock first.

// static
BOOL ETW::TypeSystemLog::ShouldLogType(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CrstHolder _crst(GetHashCrst());
    return ShouldLogTypeNoLock(th);
}


//---------------------------------------------------------------------------------------
//
// Ask hash table if we've already logged the type, without first acquiring the lock
// (our caller already did this).  As a side-effect, a TypeLoggingInfo will be created
// for this type (so future calls to this function will return FALSE to avoid dupe type
// logging).
//
// Arguments:
//      pth - TypeHandle to query
//
// Return Value:
//      nonzero iff type should be logged (i.e., not previously logged)
//
// Assumptions:
//      Caller must own the hash table's crst
//

// static
BOOL ETW::TypeSystemLog::ShouldLogTypeNoLock(TypeHandle th)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(GetHashCrst()->OwnedByCurrentThread());

    // Check to see if TypeLoggingInfo exists yet for *pth.  If not, creates one and
    // adds it to the hash.
    BOOL fCreatedNew = FALSE;
    LookupOrCreateTypeLoggingInfo(th, &fCreatedNew);

    // Return whether we had to create the TypeLoggingInfo (indicating it was not yet in
    // the hash, and thus that we hadn't yet logged the type).
    return fCreatedNew;
}


//---------------------------------------------------------------------------------------
//
// Helper that returns (creating if necessary) the TypeLoggingInfo in the hash table
// corresponding with the specified TypeHandle
//
// Arguments:
//      * th - Key to lookup the TypeLoggingInfo
//      * pfCreatedNew - [out] Points to nonzero iff a new TypeLoggingInfo was created
//          (i.e., none existed yet in the hash for th).
//      * ppLoggedTypesFromModule - [out] Points to the inner hash that was used to do
//          the lookup.  (An otpimization so the caller doesn't have to find this again,
//          if it needs to do further operations on it.)
//
// Return Value:
//      TypeLoggingInfo found or created.
//
// Assumptions:
//      Hash crst must be held by caller
//

// static
ETW::TypeLoggingInfo ETW::TypeSystemLog::LookupOrCreateTypeLoggingInfo(TypeHandle th, BOOL* pfCreatedNew, LoggedTypesFromModule** ppLoggedTypesFromModule /* = NULL */)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pfCreatedNew != NULL);
    _ASSERTE(GetHashCrst()->OwnedByCurrentThread());

    if (ppLoggedTypesFromModule != NULL)
    {
        *ppLoggedTypesFromModule = NULL;
    }

    BOOL fSucceeded = FALSE;

    if (s_pAllLoggedTypes == NULL)
    {
        s_pAllLoggedTypes = new (nothrow) AllLoggedTypes;
        if (s_pAllLoggedTypes == NULL)
        {
            // out of memory.  Bail on ETW stuff
            *pfCreatedNew = FALSE;
            return TypeLoggingInfo(NULL);
        }
    }

    // Step 1: go from LoaderModule to hash of types.

    Module* pLoaderModule = th.GetLoaderModule();
    _ASSERTE(pLoaderModule != NULL);
    LoggedTypesFromModule* pLoggedTypesFromModule = s_pAllLoggedTypes->allLoggedTypesHash.Lookup(pLoaderModule);
    if (pLoggedTypesFromModule == NULL)
    {
        pLoggedTypesFromModule = new (nothrow) LoggedTypesFromModule(pLoaderModule);
        if (pLoggedTypesFromModule == NULL)
        {
            // out of memory.  Bail on ETW stuff
            *pfCreatedNew = FALSE;
            return TypeLoggingInfo(NULL);
        }

        fSucceeded = FALSE;
        EX_TRY
        {
            s_pAllLoggedTypes->allLoggedTypesHash.Add(pLoggedTypesFromModule);
            fSucceeded = TRUE;
        }
            EX_CATCH
        {
            fSucceeded = FALSE;
        }
        EX_END_CATCH(RethrowCorruptingExceptions);
        if (!fSucceeded)
        {
            *pfCreatedNew = FALSE;
            return TypeLoggingInfo(NULL);
        }
    }

    if (ppLoggedTypesFromModule != NULL)
    {
        *ppLoggedTypesFromModule = pLoggedTypesFromModule;
    }

    // Step 2: From hash of types, see if our TypeHandle is there already
    TypeLoggingInfo typeLoggingInfoPreexisting = pLoggedTypesFromModule->loggedTypesFromModuleHash.Lookup(th);
    if (!typeLoggingInfoPreexisting.th.IsNull())
    {
        // Type is already hashed, so it's already logged, so we don't need to
        // log it again.
        *pfCreatedNew = FALSE;
        return typeLoggingInfoPreexisting;
    }

    // We haven't logged this type, so we need to continue with this function to
    // log it below. Add it to the hash table first so any recursive calls will
    // see that this type is already being taken care of
    fSucceeded = FALSE;
    TypeLoggingInfo typeLoggingInfoNew(th);
    EX_TRY
    {
        pLoggedTypesFromModule->loggedTypesFromModuleHash.Add(typeLoggingInfoNew);
        fSucceeded = TRUE;
    }
        EX_CATCH
    {
        fSucceeded = FALSE;
    }
    EX_END_CATCH(RethrowCorruptingExceptions);
    if (!fSucceeded)
    {
        *pfCreatedNew = FALSE;
        return TypeLoggingInfo(NULL);
    }

    *pfCreatedNew = TRUE;
    return typeLoggingInfoNew;
}


//---------------------------------------------------------------------------------------
//
// Called when we determine if a module was unloaded, so we can clear out that module's
// set of types from our hash table
//
// Arguments:
//      pModule - Module getting unloaded
//

// static
void ETW::TypeSystemLog::OnModuleUnload(Module* pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (!ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        return;
    }

    LoggedTypesFromModule* pLoggedTypesFromModule = NULL;

    {
        CrstHolder _crst(GetHashCrst());

        if (s_pAllLoggedTypes == NULL)
            return;

        // Is there a TypesHash for this module?
        pLoggedTypesFromModule = s_pAllLoggedTypes->allLoggedTypesHash.Lookup(pModule);
        if (pLoggedTypesFromModule == NULL)
            return;

        // Remove TypesHash from master hash mapping modules to their TypesHash
        s_pAllLoggedTypes->allLoggedTypesHash.Remove(pModule);
    }

    // Destruct this TypesHash we just removed
    delete pLoggedTypesFromModule;
    pLoggedTypesFromModule = NULL;
}

//---------------------------------------------------------------------------------------
//
// Whenever we detect that the Types keyword is off, this gets called. This eliminates the
// hash tables that tracked which types were logged (if the hash tables had been created
// previously). If type events are turned back on later, we'll re-log them all as we
// encounter them.
//

// static
void ETW::TypeSystemLog::OnTypesKeywordTurnedOff()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CrstHolder _crst(GetHashCrst());

    if (s_pAllLoggedTypes == NULL)
        return;

    // Destruct each of the per-module TypesHashes
    AllLoggedTypesHash* pLoggedTypesHash = &s_pAllLoggedTypes->allLoggedTypesHash;
    for (AllLoggedTypesHash::Iterator iter = pLoggedTypesHash->Begin();
        iter != pLoggedTypesHash->End();
        ++iter)
    {
        LoggedTypesFromModule* pLoggedTypesFromModule = *iter;
        delete pLoggedTypesFromModule;
    }

    // This causes the default ~AllLoggedTypes() to be called, and thus
    // ~AllLoggedTypesHash() to be called
    delete s_pAllLoggedTypes;
    s_pAllLoggedTypes = NULL;
}


/****************************************************************************/
/* Called when ETW is turned ON on an existing process and ModuleRange events are to
     be fired */
     /****************************************************************************/
void ETW::EnumerationLog::ModuleRangeRundown()
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context,
                                         TRACE_LEVEL_INFORMATION,
                                         CLR_PERFTRACK_PRIVATE_KEYWORD))
        {
            ETW::EnumerationLog::EnumerationHelper(NULL, NULL, ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoadPrivate);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* Called when ETW is turned ON on an existing process */
/****************************************************************************/
void ETW::EnumerationLog::StartRundown()
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        BOOL bIsArmRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                                                 TRACE_LEVEL_INFORMATION,
                                                                 CLR_RUNDOWNAPPDOMAINRESOURCEMANAGEMENT_KEYWORD);
        BOOL bIsPerfTrackRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                                                 TRACE_LEVEL_INFORMATION,
                                                                 CLR_RUNDOWNPERFTRACK_KEYWORD);
        BOOL bIsThreadingRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_RUNDOWNTHREADING_KEYWORD);

        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNJIT_KEYWORD)
           ||
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNLOADER_KEYWORD)
           ||
           IsRundownNgenKeywordEnabledAndNotSuppressed()
           ||
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNJITTEDMETHODILTONATIVEMAP_KEYWORD)
           ||
           bIsArmRundownEnabled
           ||
           bIsPerfTrackRundownEnabled
           ||
           bIsThreadingRundownEnabled)
        {
            // begin marker event will go to the rundown provider
            FireEtwDCStartInit_V1(GetClrInstanceId());

            // The rundown flag is expected to be checked in the caller, so no need to check here again
            DWORD enumerationOptions = ETW::EnumerationLog::EnumerationStructs::None;
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNLOADER_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart;
            }
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNJIT_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JitMethodDCStart;
            }
            if (IsRundownNgenKeywordEnabledAndNotSuppressed())
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart;
            }
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNJITTEDMETHODILTONATIVEMAP_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::MethodDCStartILToNativeMap;
            }
            if (bIsPerfTrackRundownEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCStart;
            }

            ETW::EnumerationLog::EnumerationHelper(NULL, NULL, enumerationOptions);

            if (bIsArmRundownEnabled)
            {
                // When an ETW event consumer asks for ARM rundown, that not only enables
                // the ETW events, but also causes some minor behavioral changes in the
                // CLR, such as gathering CPU usage baselines for each thread right now,
                // and also gathering resource usage information later on (keyed off of
                // g_fEnableARM, which we'll set right now).
                EnableARM();
            }

            if (bIsArmRundownEnabled || bIsThreadingRundownEnabled)
            {
                SendThreadRundownEvent();
            }

            // end marker event will go to the rundown provider
            FireEtwDCStartComplete_V1(GetClrInstanceId());
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

//---------------------------------------------------------------------------------------
//
// Simple helper to convert the currently active keywords on the runtime provider into a
// bitmask of enumeration options as defined in ETW::EnumerationLog::EnumerationStructs
//
// Return Value:
//      ETW::EnumerationLog::EnumerationStructs bitmask corresponding to the currently
//      active keywords on the runtime provider
//

// static
DWORD ETW::EnumerationLog::GetEnumerationOptionsFromRuntimeKeywords()
{
    LIMITED_METHOD_CONTRACT;

    DWORD enumerationOptions = ETW::EnumerationLog::EnumerationStructs::None;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_LOADER_KEYWORD))
    {
        enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload;
    }
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_JIT_KEYWORD) &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_ENDENUMERATION_KEYWORD))
    {
        enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JitMethodUnload;
    }
    if (IsRuntimeNgenKeywordEnabledAndNotSuppressed() &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_ENDENUMERATION_KEYWORD))
    {
        enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::NgenMethodUnload;
    }

    return enumerationOptions;
}

//---------------------------------------------------------------------------------------
//
// Executes a flavor of rundown initiated by a CAPTURE_STATE request to
// code:#EtwCallback.  CAPTURE_STATE is the "ETW-sanctioned" way of performing a
// rundown, whereas the CLR's rundown provider was *our* version of this, implemented
// before CAPTURE_STATE was standardized.
//
// When doing a CAPTURE_STATE, the CLR rundown provider is completely unused.  Instead,
// we pay attention to the runtime keywords active at the time the CAPTURE_STATE was
// requested, and enumerate through the appropriate objects (AppDomains, assemblies,
// modules, types, methods, threads) and send runtime events for each of them.
//
// CAPTURE_STATE is intended to be used primarily by PerfTrack.  Implementing this form
// of rundown allows PerfTrack to be blissfully unaware of the CLR's rundown provider.
//

// static
void ETW::EnumerationLog::EnumerateForCaptureState()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, KEYWORDZERO))
        {
            DWORD enumerationOptions = GetEnumerationOptionsFromRuntimeKeywords();

            // Send unload events for all remaining domains, including shared domain and
            // default domain.
            ETW::EnumerationLog::EnumerationHelper(NULL /* module filter */, NULL /* domain filter */, enumerationOptions);

            // Send thread created events for all currently active threads, if requested
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                                                 TRACE_LEVEL_INFORMATION,
                                                                 CLR_THREADING_KEYWORD))
            {
                SendThreadRundownEvent();
            }
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/**************************************************************************************/
/* Called when ETW is turned OFF on an existing process .Will be used by the controller for end rundown*/
/**************************************************************************************/
void ETW::EnumerationLog::EndRundown()
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        BOOL bIsPerfTrackRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                                                 TRACE_LEVEL_INFORMATION,
                                                                 CLR_RUNDOWNPERFTRACK_KEYWORD);
        BOOL bIsThreadingRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_RUNDOWNTHREADING_KEYWORD);
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNJIT_KEYWORD)
           ||
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNLOADER_KEYWORD)
           ||
           IsRundownNgenKeywordEnabledAndNotSuppressed()
           ||
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNJITTEDMETHODILTONATIVEMAP_KEYWORD)
           ||
           bIsPerfTrackRundownEnabled
           ||
           bIsThreadingRundownEnabled
        )
        {
            // begin marker event will go to the rundown provider
            FireEtwDCEndInit_V1(GetClrInstanceId());

            // The rundown flag is expected to be checked in the caller, so no need to check here again
            DWORD enumerationOptions = ETW::EnumerationLog::EnumerationStructs::None;
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNLOADER_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd;
            }
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNJIT_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JitMethodDCEnd;
            }
            if (IsRundownNgenKeywordEnabledAndNotSuppressed())
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd;
            }
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNJITTEDMETHODILTONATIVEMAP_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::MethodDCEndILToNativeMap;
            }
            if (bIsPerfTrackRundownEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCEnd;
            }

            ETW::EnumerationLog::EnumerationHelper(NULL, NULL, enumerationOptions);

            if (bIsThreadingRundownEnabled)
            {
                SendThreadRundownEvent();
            }

            // end marker event will go to the rundown provider
            FireEtwDCEndComplete_V1(GetClrInstanceId());
        }
    } EX_CATCH{
        STRESS_LOG1(LF_ALWAYS, LL_ERROR, "Exception during Rundown Enumeration, EIP of last AV = %p", g_LastAccessViolationEIP);
    } EX_END_CATCH(SwallowAllExceptions);
}

// #Registration
/*++

Routine Description:

    Registers provider with ETW tracing framework.
    This function should not be called more than once, on
    Dll Process attach only.
    Not thread safe.

Arguments:
    none

Return Value:
    Returns the return value from RegisterTraceGuids or EventRegister.

--*/

void InitializeEventTracing()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Do startup-only initialization of any state required by the ETW classes before
    // events can be fired
    HRESULT hr = ETW::TypeSystemLog::PreRegistrationInit();
    if (FAILED(hr))
        return;

    // Register CLR providers with the OS
    if (g_pEtwTracer == NULL)
    {
        NewHolder <ETW::CEtwTracer> tempEtwTracer(new (nothrow) ETW::CEtwTracer());
        if (tempEtwTracer != NULL && tempEtwTracer->Register() == ERROR_SUCCESS)
            g_pEtwTracer = tempEtwTracer.Extract();
    }

    g_nClrInstanceId = GetRuntimeId() & 0x0000FFFF; // This will give us duplicate ClrInstanceId after UINT16_MAX

    // Any classes that need some initialization to happen after we've registered the
    // providers can do so now
    ETW::TypeSystemLog::PostRegistrationInit();
}

HRESULT ETW::CEtwTracer::Register()
{
    WRAPPER_NO_CONTRACT;

    OSVERSIONINFO osVer;
    osVer.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    if (GetOSVersion(&osVer) == FALSE) {
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }
    else if (osVer.dwMajorVersion < ETW_SUPPORTED_MAJORVER) {
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    // if running on OS < Longhorn, skip registration unless reg key is set
    // since ETW reg is expensive (in both time and working set) on older OSes
    if (osVer.dwMajorVersion < ETW_ENABLED_MAJORVER && !g_fEnableETW && !CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PreVistaETWEnabled))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    // If running on OS >= Longhorn, skip registration if ETW is not enabled
    if (osVer.dwMajorVersion >= ETW_ENABLED_MAJORVER && !g_fEnableETW && !CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_VistaAndAboveETWEnabled))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    EventRegisterMicrosoft_Windows_DotNETRuntime();
    EventRegisterMicrosoft_Windows_DotNETRuntimePrivate();
    EventRegisterMicrosoft_Windows_DotNETRuntimeRundown();

    // Stress Log ETW events are available only on the desktop version of the runtime
#ifndef FEATURE_CORECLR
    EventRegisterMicrosoft_Windows_DotNETRuntimeStress();
#endif // !FEATURE_CORECLR

    MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeHandle;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimePrivateHandle;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeRundownHandle;
#ifndef FEATURE_CORECLR
    MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeStressHandle;
#endif // !FEATURE_CORECLR

    return S_OK;
}

// #Unregistration
/*++

Routine Description:
        Unregisters the provider from ETW. This function
        should only be called once from DllMain Detach process.
        Not thread safe.

Arguments:
       none

Return Value:
       Returns ERROR_SUCCESS

--*/
HRESULT ETW::CEtwTracer::UnRegister()
{
    LIMITED_METHOD_CONTRACT;

    EventUnregisterMicrosoft_Windows_DotNETRuntime();
    EventUnregisterMicrosoft_Windows_DotNETRuntimePrivate();
    EventUnregisterMicrosoft_Windows_DotNETRuntimeRundown();
#ifndef FEATURE_CORECLR
    EventUnregisterMicrosoft_Windows_DotNETRuntimeStress();
#endif // !FEATURE_CORECLR
    return S_OK;
}

extern "C"
{
    ETW_INLINE
        void EtwCallout(REGHANDLE RegHandle,
            PCEVENT_DESCRIPTOR Descriptor,
            ULONG ArgumentCount,
            PEVENT_DATA_DESCRIPTOR EventData)
    {
        WRAPPER_NO_CONTRACT;
        UINT8 providerIndex = 0;
        if (RegHandle == Microsoft_Windows_DotNETRuntimeHandle) {
            providerIndex = 0;
        }
        else if (RegHandle == Microsoft_Windows_DotNETRuntimeRundownHandle) {
            providerIndex = 1;
        }
        else if (RegHandle == Microsoft_Windows_DotNETRuntimeStressHandle) {
            providerIndex = 2;
        }
        else if (RegHandle == Microsoft_Windows_DotNETRuntimePrivateHandle) {
            providerIndex = 3;
        }
        else {
            _ASSERTE(!"Provider not one of Runtime, Rundown, Private and Stress");
            return;
        }

        // stacks are supposed to be fired for only the events with a bit set in the etwStackSupportedEvents bitmap
        if (((etwStackSupportedEvents[providerIndex][Descriptor->Id / 8]) &
            (1 << (Descriptor->Id % 8))) != 0)
        {
            if (RegHandle == Microsoft_Windows_DotNETRuntimeHandle) {
                ETW::SamplingLog::SendStackTrace(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, &CLRStackWalk, &CLRStackId);
            }
            else if (RegHandle == Microsoft_Windows_DotNETRuntimeRundownHandle) {
                ETW::SamplingLog::SendStackTrace(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context, &CLRStackWalkDCStart, &CLRStackRundownId);
            }
            else if (RegHandle == Microsoft_Windows_DotNETRuntimePrivateHandle) {
                ETW::SamplingLog::SendStackTrace(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, &CLRStackWalkPrivate, &CLRStackPrivateId);
            }
            else if (RegHandle == Microsoft_Windows_DotNETRuntimeStressHandle) {
                ETW::SamplingLog::SendStackTrace(MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_Context, &CLRStackWalkStress, &CLRStackStressId);
            }
        }
    }
}

extern "C"
{

    // #EtwCallback:
    // During the build, MC generates the code to register our provider, and to register
    // our ETW callback. (This is buried under Intermediates, in a path like
    // Intermediate\clr\corguids.nativeproj_1723354836\obj1c\x86\ClrEtwAll.h.) The ETW
    // callback is also generated for us by MC. But we can hook into this generated
    // callback by #defining MCGEN_PRIVATE_ENABLE_CALLBACK_V2 to be a call to this
    // function (EtwCallback), thus causing EtwCallback to get called after the
    // MC-generated code executes.
    //
    // This callback function is called whenever an ETW session is enabled or disabled. A
    // callback function needs to be specified when the provider is registered. C style
    // callback wrappers are needed during event registration. To handle the callback
    // action in this class, we pass "this" during provider registration and modify the
    // context to the relevant context in the C callback later.
    ETW_INLINE
        void EtwCallback(
            _In_ LPCGUID SourceId,
            _In_ ULONG ControlCode,
            _In_ UCHAR Level,
            _In_ ULONGLONG MatchAnyKeyword,
            _In_ ULONGLONG MatchAllKeyword,
            _In_opt_ PEVENT_FILTER_DESCRIPTOR FilterData,
            _Inout_opt_ PVOID CallbackContext)
    {
        CONTRACTL{
            NOTHROW;
            if (g_fEEStarted) { GC_TRIGGERS; }
 else { DISABLED(GC_NOTRIGGER); };
MODE_ANY;
CAN_TAKE_LOCK;
STATIC_CONTRACT_FAULT;
SO_NOT_MAINLINE;
        } CONTRACTL_END;

        // Mark that we are the special ETWRundown thread.  Currently all this does
        // is insure that AVs thrown in this thread are treated as normal exceptions.
        // This allows us to catch and swallow them.   We can do this because we have
        // a reasonably strong belief that doing ETW Rundown does not change runtime state
        // and thus if an AV happens it is better to simply give up logging ETW and
        // instead of terminating the process (which is what we would do normally)
        ClrFlsThreadTypeSwitch etwRundownThreadHolder(ThreadType_ETWRundownThread);
        PMCGEN_TRACE_CONTEXT context = (PMCGEN_TRACE_CONTEXT)CallbackContext;

        BOOLEAN bIsPublicTraceHandle =
#ifdef WINXP_AND_WIN2K3_BUILD_SUPPORT
            McGenPreVista ? ((ULONGLONG)Microsoft_Windows_DotNETRuntimeHandle == (ULONGLONG)context) :
#endif
            (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeHandle);

        BOOLEAN bIsPrivateTraceHandle =
#ifdef WINXP_AND_WIN2K3_BUILD_SUPPORT
            McGenPreVista ? ((ULONGLONG)Microsoft_Windows_DotNETRuntimePrivateHandle == (ULONGLONG)context) :
#endif
            (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimePrivateHandle);

        BOOLEAN bIsRundownTraceHandle =
#ifdef WINXP_AND_WIN2K3_BUILD_SUPPORT
            McGenPreVista ? ((ULONGLONG)Microsoft_Windows_DotNETRuntimeRundownHandle == (ULONGLONG)context) :
#endif
            (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeRundownHandle);


        // A manifest based provider can be enabled to multiple event tracing sessions
        // As long as there is atleast 1 enabled session, IsEnabled will be TRUE
        // Since classic providers can be enabled to only a single session,
        // IsEnabled will be TRUE when it is enabled and FALSE when disabled
        BOOL bEnabled =
            ((ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER) ||
                (ControlCode == EVENT_CONTROL_CODE_CAPTURE_STATE));
        if (bEnabled)
        {
            // TypeSystemLog needs a notification when certain keywords are modified, so
            // give it a hook here.
            if (g_fEEStarted && !g_fEEShutDown && bIsPublicTraceHandle)
            {
                ETW::TypeSystemLog::OnKeywordsChanged();
            }

            if (bIsPrivateTraceHandle)
            {
                ETW::GCLog::GCSettingsEvent();
                if (g_fEEStarted && !g_fEEShutDown)
                {
                    ETW::EnumerationLog::ModuleRangeRundown();
                }
            }

#ifdef _WIN64   // We only do this on 64 bit (NOT ARM, because ARM uses frame based stack crawling)
            // If we have turned on the JIT keyword to the VERBOSE setting (needed to get JIT names) then
            // we assume that we also want good stack traces so we need to publish unwind information so
            // ETW can get at it
            if (bIsPublicTraceHandle && ETW_CATEGORY_ENABLED((*context), TRACE_LEVEL_VERBOSE, CLR_RUNDOWNJIT_KEYWORD))
                UnwindInfoTable::PublishUnwindInfo(g_fEEStarted != FALSE);
#endif
            if (g_fEEStarted && !g_fEEShutDown && bIsRundownTraceHandle)
            {
                // Fire the runtime information event
                ETW::InfoLog::RuntimeInformation(ETW::InfoLog::InfoStructs::Callback);

                // Start and End Method/Module Rundowns
                // Used to fire events that we missed since we started the controller after the process started
                // flags for immediate start rundown
                if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                    TRACE_LEVEL_INFORMATION,
                    CLR_RUNDOWNSTART_KEYWORD))
                    ETW::EnumerationLog::StartRundown();

                // flags delayed end rundown
                if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
                    TRACE_LEVEL_INFORMATION,
                    CLR_RUNDOWNEND_KEYWORD))
                    ETW::EnumerationLog::EndRundown();
            }

            if (g_fEEStarted && !g_fEEShutDown && (ControlCode == EVENT_CONTROL_CODE_CAPTURE_STATE))
            {
                ETW::EnumerationLog::EnumerateForCaptureState();
            }

            // Special check for the runtime provider's GCHeapCollectKeyword.  Profilers
            // flick this to force a full GC.
            if (g_fEEStarted && !g_fEEShutDown && bIsPublicTraceHandle &&
                ((MatchAnyKeyword & CLR_GCHEAPCOLLECT_KEYWORD) != 0))
            {
                // Profilers may (optionally) specify extra data in the filter parameter
                // to log with the GCStart event.
                LONGLONG l64ClientSequenceNumber = 0;
                if ((FilterData != NULL) &&
                    (FilterData->Type == 1) &&
                    (FilterData->Size == sizeof(l64ClientSequenceNumber)))
                {
                    l64ClientSequenceNumber = *(LONGLONG*)(FilterData->Ptr);
                }
                ETW::GCLog::ForceGC(l64ClientSequenceNumber);
            }
        }
#ifdef FEATURE_COMINTEROP
        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, CCWRefCountChange))
            g_pConfig->SetLogCCWRefCountChangeEnabled(bEnabled != 0);
#endif // FEATURE_COMINTEROP

    }
}

#endif // FEATURE_NATIVEAOT
#else // !FEATURE_DTRACE

/**************************************************************************************/
/* Helper data structure for supporting string in Dtrace probes. Since Dtrace does not support Unicode    */
/* in its printf API, we cast the unicode string to UFT8 string and then output them.                                */
/**************************************************************************************/
#define DTRACE_OUTPUT_STRING_LEN 512
const CHAR szDtraceOutputNULL[] = "NULL";
INT32 WideCharToMultiByte(LPCWSTR wszSrcStr, LPSTR szDstStr);

#include <rotor_pal.h>

// The possible value of COMPlus_ETWEnabled should be '0' or '1'
#define SIZE_ETWEnabled 2
// The possible value of COMPlus_EventInfo should be a string in the following format:
// GUID:HexNumfer:Level
// GUID: For example e13c0d23-ccbc-4e12-931b-d9cc2eee27e4 (36 bytes)
// HewNumber: 0xffffffff (10 bytes)
// Level: 0~9 (1 bytes)
// Therefore the length of it should be 36 + 1 + 10 + 1 + 1 + 1 = 50
#define SIZE_EventInfo 50

ULONG ETW::CEtwTracer::Register()
{
    // Get Env Var COMPlus_ETWEnabled
    char szETWEnabled[SIZE_ETWEnabled];
    DWORD newLen = GetEnvironmentVariableA("COMPlus_ETWEnabled", szETWEnabled, SIZE_ETWEnabled);
    if (newLen == 0 || newLen >= SIZE_ETWEnabled || strcmp(szETWEnabled, "1") != 0)
        return 0;

    // Get Env Var COMPlus_EventInfo
    char szEventInfo[SIZE_EventInfo];
    newLen = GetEnvironmentVariableA("COMPlus_EventInfo", szEventInfo, SIZE_EventInfo);
    if (newLen == 0 || newLen >= SIZE_EventInfo || strchr(szEventInfo, ' ') != NULL)
        return 0;

    // Get Env Var COMPlus_EventLogFileName
    char szEventLogFN[_MAX_FNAME];
    newLen = GetEnvironmentVariableA("COMPlus_EventLogFileName", szEventLogFN, _MAX_FNAME);
    if (newLen == 0 || newLen >= _MAX_FNAME || strchr(szEventLogFN, '|') != NULL)
        return 0;
    char szEventLogFullPath[_MAX_PATH];
    newLen = GetFullPathNameA(szEventLogFN, _MAX_PATH, szEventLogFullPath, NULL);
    if (newLen == 0 || newLen > _MAX_PATH || strchr(szEventLogFN, '|') != NULL)
        return 0;

    // Get the process id which is ued in dtrace to fire the probes of the process
    int nProcessId = GetCurrentProcessId();

    // Start the log (By calling an PAL API to connect to a Unix Domain Server)
    PAL_StartLog(szEventInfo, szEventLogFullPath, nProcessId);

    return 0;
}

INT32  WideCharToMultiByte(LPCWSTR wszSrcStr, LPSTR szDstStr)
{
    INT32 nSize = WideCharToMultiByte(CP_UTF8, 0, wszSrcStr, -1, NULL, 0, NULL, NULL);
    if (0 == nSize)
    {
        return 0;
    }
    if (nSize > DTRACE_OUTPUT_STRING_LEN - 1)
    {
        nSize = DTRACE_OUTPUT_STRING_LEN - 1;
    }
    INT32 nSize2 = WideCharToMultiByte(CP_UTF8, 0, wszSrcStr, -1, szDstStr, nSize, NULL, NULL);
    if (nSize2 != nSize || nSize2 <= 0)
    {
        return 0;
    }
    return nSize;
}

void EEConfigSetup_V1()
{
    FireEtwEEConfigSetup_V1(GetClrInstanceId());
}

void EEConfigSetupEnd_V1()
{
    FireEtwEEConfigSetupEnd_V1(GetClrInstanceId());
}

void LdSysBases_V1()
{
    FireEtwLdSysBases_V1(GetClrInstanceId());
}

void LdSysBasesEnd_V1()
{
    FireEtwLdSysBasesEnd_V1(GetClrInstanceId());
}

void ExecExe_V1()
{
    FireEtwExecExe_V1(GetClrInstanceId());
}

void ExecExeEnd_V1()
{
    FireEtwExecExeEnd_V1(GetClrInstanceId());
}

void Main_V1()
{
    FireEtwMain_V1(GetClrInstanceId());
}

void MainEnd_V1()
{
    FireEtwMainEnd_V1(GetClrInstanceId());
}


void ApplyPolicyStart_V1()
{
    FireEtwApplyPolicyStart_V1(GetClrInstanceId());
}

void ApplyPolicyEnd_V1()
{
    FireEtwApplyPolicyEnd_V1(GetClrInstanceId());
}

void PrestubWorker_V1()
{
    FireEtwPrestubWorker_V1(GetClrInstanceId());
}

void PrestubWorkerEnd_V1()
{
    FireEtwPrestubWorkerEnd_V1(GetClrInstanceId());
}

void ExplicitBindStart_V1()
{
    FireEtwExplicitBindStart_V1(GetClrInstanceId());
}

void ExplicitBindEnd_V1()
{
    FireEtwExplicitBindEnd_V1(GetClrInstanceId());
}

void ParseXml_V1()
{
    FireEtwParseXml_V1(GetClrInstanceId());
}

void ParseXmlEnd_V1()
{
    FireEtwParseXmlEnd_V1(GetClrInstanceId());
}

void InitDefaultDomain_V1()
{
    FireEtwInitDefaultDomain_V1(GetClrInstanceId());
}

void InitDefaultDomainEnd_V1()
{
    FireEtwInitDefaultDomainEnd_V1(GetClrInstanceId());
}
void AllowBindingRedirs_V1()
{
    FireEtwAllowBindingRedirs_V1(GetClrInstanceId());
}

void AllowBindingRedirsEnd_V1()
{
    FireEtwAllowBindingRedirsEnd_V1(GetClrInstanceId());
}

void EEConfigSync_V1()
{
    FireEtwEEConfigSync_V1(GetClrInstanceId());
}

void EEConfigSyncEnd_V1()
{
    FireEtwEEConfigSyncEnd_V1(GetClrInstanceId());
}

void FusionBinding_V1()
{
    FireEtwFusionBinding_V1(GetClrInstanceId());
}

void FusionBindingEnd_V1()
{
    FireEtwFusionBindingEnd_V1(GetClrInstanceId());
}

void LoaderCatchCall_V1()
{
    FireEtwLoaderCatchCall_V1(GetClrInstanceId());
}

void LoaderCatchCallEnd_V1()
{
    FireEtwLoaderCatchCallEnd_V1(GetClrInstanceId());
}

void FusionInit_V1()
{
    FireEtwFusionInit_V1(GetClrInstanceId());
}

void FusionInitEnd_V1()
{
    FireEtwFusionInitEnd_V1(GetClrInstanceId());
}

void FusionAppCtx_V1()
{
    FireEtwFusionAppCtx_V1(GetClrInstanceId());
}

void FusionAppCtxEnd_V1()
{
    FireEtwFusionAppCtxEnd_V1(GetClrInstanceId());
}

void SecurityCatchCall_V1()
{
    FireEtwSecurityCatchCall_V1(GetClrInstanceId());
}

void SecurityCatchCallEnd_V1()
{
    FireEtwSecurityCatchCallEnd_V1(GetClrInstanceId());
}


#endif // !FEATURE_DTRACE

#ifndef FEATURE_NATIVEAOT

/****************************************************************************/
/* This is called by the runtime when an exception is thrown */
/****************************************************************************/
void ETW::ExceptionLog::ExceptionThrown(CrawlFrame* pCf, BOOL bIsReThrownException, BOOL bIsNewException)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(GetThread() != NULL);
        PRECONDITION(GetThread()->GetThrowable() != NULL);
    } CONTRACTL_END;

    if (!(bIsReThrownException || bIsNewException))
    {
        return;
    }
    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, ExceptionThrown_V1))
    {
        return;
    }
    EX_TRY
    {
        SString exceptionType(L"");
        LPWSTR exceptionMessage = NULL;
        BOOL bIsCLSCompliant = FALSE, bIsCSE = FALSE, bIsNestedException = FALSE, bHasInnerException = FALSE;
        UINT16 exceptionFlags = 0;
        PVOID exceptionEIP = 0;

        Thread* pThread = GetThread();

        struct
        {
            OBJECTREF exceptionObj;
            OBJECTREF innerExceptionObj;
            STRINGREF exceptionMessageRef;
        } gc;
        ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.exceptionObj = pThread->GetThrowable();
        gc.innerExceptionObj = ((EXCEPTIONREF)gc.exceptionObj)->GetInnerException();

        ThreadExceptionState* pExState = pThread->GetExceptionState();
#ifndef WIN64EXCEPTIONS
        PTR_ExInfo pExInfo = NULL;
#else
        PTR_ExceptionTracker pExInfo = NULL;
#endif //!WIN64EXCEPTIONS
        pExInfo = pExState->GetCurrentExceptionTracker();
        _ASSERTE(pExInfo != NULL);
        bIsNestedException = (pExInfo->GetPreviousExceptionTracker() != NULL);
        bIsCSE = (pExInfo->GetCorruptionSeverity() == ProcessCorrupting);
        bIsCLSCompliant = IsException((gc.exceptionObj)->GetMethodTable()) &&
                          ((gc.exceptionObj)->GetMethodTable() != MscorlibBinder::GetException(kRuntimeWrappedException));

        // A rethrown exception is also a nested exception
        // but since we have a separate flag for it, lets unset the nested flag
        if (bIsReThrownException)
        {
            bIsNestedException = FALSE;
        }
        bHasInnerException = (gc.innerExceptionObj) != NULL;

        exceptionFlags = ((bHasInnerException ? ETW::ExceptionLog::ExceptionStructs::HasInnerException : 0) |
                          (bIsNestedException ? ETW::ExceptionLog::ExceptionStructs::IsNestedException : 0) |
                          (bIsReThrownException ? ETW::ExceptionLog::ExceptionStructs::IsReThrownException : 0) |
                          (bIsCSE ? ETW::ExceptionLog::ExceptionStructs::IsCSE : 0) |
                          (bIsCLSCompliant ? ETW::ExceptionLog::ExceptionStructs::IsCLSCompliant : 0));

        if (pCf->IsFrameless())
        {
#ifndef _WIN64
            exceptionEIP = (PVOID)pCf->GetRegisterSet()->ControlPC;
#else
            exceptionEIP = (PVOID)GetIP(pCf->GetRegisterSet()->pContext);
#endif //!_WIN64
        }
        else
        {
            exceptionEIP = (PVOID)(pCf->GetFrame()->GetIP());
        }

        // On platforms other than IA64, we are at the instruction after the faulting instruction
        // This check has been copied from StackTraceInfo::AppendElement
        if (!(pCf->HasFaulted() || pCf->IsIPadjusted()) && exceptionEIP != 0)
        {
            exceptionEIP = (PVOID)((UINT_PTR)exceptionEIP - 1);
        }

        gc.exceptionMessageRef = ((EXCEPTIONREF)gc.exceptionObj)->GetMessage();
        TypeHandle exceptionTypeHandle = (gc.exceptionObj)->GetTypeHandle();
        exceptionTypeHandle.GetName(exceptionType);
        WCHAR* exceptionTypeName = (WCHAR*)exceptionType.GetUnicode();

        if (gc.exceptionMessageRef != NULL)
        {
            exceptionMessage = (gc.exceptionMessageRef)->GetBuffer();
        }

        HRESULT exceptionHRESULT = ((EXCEPTIONREF)gc.exceptionObj)->GetHResult();

        FireEtwExceptionThrown_V1(exceptionTypeName,
                                  exceptionMessage,
                                  exceptionEIP,
                                  exceptionHRESULT,
                                  exceptionFlags,
                                  GetClrInstanceId());
        GCPROTECT_END();
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when a domain is loaded */
/****************************************************************************/
void ETW::LoaderLog::DomainLoadReal(BaseDomain* pDomain, _In_opt_ LPWSTR wszFriendlyName)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_LOADER_KEYWORD))
        {
            DWORD dwEventOptions = ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad;
            ETW::LoaderLog::SendDomainEvent(pDomain, dwEventOptions, wszFriendlyName);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when an AppDomain is unloaded */
/****************************************************************************/
void ETW::LoaderLog::DomainUnload(AppDomain* pDomain)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        KEYWORDZERO))
        {
            if (!pDomain->NoAccessToHandleTable())
            {
                DWORD enumerationOptions = ETW::EnumerationLog::GetEnumerationOptionsFromRuntimeKeywords();

                // Domain unload also causes type unload events
                if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                                TRACE_LEVEL_INFORMATION,
                                                CLR_TYPE_KEYWORD))
                {
                    enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::TypeUnload;
                }

                ETW::EnumerationLog::EnumerationHelper(NULL, pDomain, enumerationOptions);
            }
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when a LoaderAllocator is unloaded */
/****************************************************************************/
void ETW::LoaderLog::CollectibleLoaderAllocatorUnload(AssemblyLoaderAllocator* pLoaderAllocator)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        KEYWORDZERO))
        {
            DWORD enumerationOptions = ETW::EnumerationLog::GetEnumerationOptionsFromRuntimeKeywords();

            // Collectible Loader Allocator unload also causes type unload events
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_TYPE_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::TypeUnload;
            }

            ETW::EnumerationLog::IterateCollectibleLoaderAllocator(pLoaderAllocator, enumerationOptions);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when the runtime is loaded
   Function gets called by both the Callback mechanism and regular ETW events.
   Type is used to differentiate whether its a callback or a normal call*/
   /****************************************************************************/
void ETW::InfoLog::RuntimeInformation(INT32 type)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY{
        if ((type == ETW::InfoLog::InfoStructs::Normal && ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, RuntimeInformationStart))
#ifndef FEATURE_PAL
            ||
           (type == ETW::InfoLog::InfoStructs::Callback && ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context, RuntimeInformationDCStart))
#endif //!FEATURE_PAL
          )
        {
#ifndef FEATURE_DTRACE
            PCWSTR szDtraceOutput1 = L"",szDtraceOutput2 = L"";
#else
            CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
            CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
#endif // !FEATURE_DTRACE
            UINT8 startupMode = 0;
            UINT startupFlags = 0;
            WCHAR dllPath[MAX_PATH + 1] = {0};
            UINT8 Sku = 0;
            _ASSERTE(g_fEEManagedEXEStartup ||   //CLR started due to a managed exe
                g_fEEIJWStartup ||               //CLR started as a mixed mode Assembly
                CLRHosted() || g_fEEHostedStartup || //CLR started through one of the Hosting API CLRHosted() returns true if CLR started through the V2 Interface while
                                                    // g_fEEHostedStartup is true if CLR is hosted through the V1 API.
                g_fEEComActivatedStartup ||      //CLR started as a COM object
                g_fEEOtherStartup);            //In case none of the 4 above mentioned cases are true for example ngen, ildasm then we assume its a "other" startup

#ifdef FEATURE_CORECLR
            Sku = ETW::InfoLog::InfoStructs::CoreCLR;
#else
            Sku = ETW::InfoLog::InfoStructs::DesktopCLR;
#endif //FEATURE_CORECLR

            //version info for clr.dll
            USHORT vmMajorVersion = VER_MAJORVERSION;
            USHORT vmMinorVersion = VER_MINORVERSION;
            USHORT vmBuildVersion = VER_PRODUCTBUILD;
            USHORT vmQfeVersion = VER_PRODUCTBUILD_QFE;

            //version info for mscorlib.dll
            USHORT bclMajorVersion = VER_ASSEMBLYMAJORVERSION;
            USHORT bclMinorVersion = VER_ASSEMBLYMINORVERSION;
            USHORT bclBuildVersion = VER_ASSEMBLYBUILD;
            USHORT bclQfeVersion = VER_ASSEMBLYBUILD_QFE;

#ifndef FEATURE_PAL
            LPCGUID comGUID = g_fEEComObjectGuid;
#else
            unsigned int comGUID = 0;
#endif //!FEATURE_PAL

#ifndef FEATURE_DTRACE
            LPWSTR lpwszCommandLine = L"";
            LPWSTR lpwszRuntimeDllPath = (LPWSTR)dllPath;
#else
            SIZE_T lpwszCommandLine = (SIZE_T)szDtraceOutput1;
            SIZE_T lpwszRuntimeDllPath = (SIZE_T)szDtraceOutput2;
#endif //!FEATURE_DTRACE

#ifndef FEATURE_CORECLR
            startupFlags = CorHost2::GetStartupFlags();
#endif //!FEATURE_CORECLR

            // Determine the startupmode
            if (g_fEEIJWStartup)
            {
                //IJW Mode
                startupMode = ETW::InfoLog::InfoStructs::IJW;
            }
            else if (g_fEEManagedEXEStartup)
            {
                //managed exe
                startupMode = ETW::InfoLog::InfoStructs::ManagedExe;
#ifndef FEATURE_DTRACE
                lpwszCommandLine = WszGetCommandLine();
#else
                INT32 nSize = WideCharToMultiByte(WszGetCommandLine(), szDtraceOutput1);
                if (nSize > 0) {
                    lpwszCommandLine = (SIZE_T)szDtraceOutput1;
                }
#endif //!FEATURE_DTRACE
            }
            else if (CLRHosted() || g_fEEHostedStartup)
            {
                //Hosted CLR
                startupMode = ETW::InfoLog::InfoStructs::HostedCLR;
            }
            else if (g_fEEComActivatedStartup)
            {
                //com activated
                startupMode = ETW::InfoLog::InfoStructs::COMActivated;
            }
            else if (g_fEEOtherStartup)
            {
                //startup type is other
                startupMode = ETW::InfoLog::InfoStructs::Other;
            }

            _ASSERTE(NumItems(dllPath) > MAX_PATH);
            // if WszGetModuleFileName fails, we return an empty string
            if (!WszGetModuleFileName(GetCLRModule(), dllPath, MAX_PATH)) {
                dllPath[0] = 0;
            }
            dllPath[MAX_PATH] = 0;
#ifdef FEATURE_DTRACE
            _ASSERTE(NumItems(szDtraceOutput2) >= NumItems(dllPath));
            INT32 nSize = WideCharToMultiByte(dllPath, szDtraceOutput2);
            if (nSize > 0) {
                lpwszRuntimeDllPath = (SIZE_T)szDtraceOutput2;
            }
#endif // FEATURE_DTRACE

            if (type == ETW::InfoLog::InfoStructs::Callback)
            {
                FireEtwRuntimeInformationDCStart(GetClrInstanceId(),
                                                  Sku,
                                                  bclMajorVersion,
                                                  bclMinorVersion,
                                                  bclBuildVersion,
                                                  bclQfeVersion,
                                                  vmMajorVersion,
                                                  vmMinorVersion,
                                                  vmBuildVersion,
                                                  vmQfeVersion,
                                                  startupFlags,
                                                  startupMode,
                                                  lpwszCommandLine,
                                                  comGUID,
                                                  lpwszRuntimeDllPath);
            }
            else
            {
                FireEtwRuntimeInformationStart(GetClrInstanceId(),
                                                Sku,
                                                bclMajorVersion,
                                                bclMinorVersion,
                                                bclBuildVersion,
                                                bclQfeVersion,
                                                vmMajorVersion,
                                                vmMinorVersion,
                                                vmBuildVersion,
                                                vmQfeVersion,
                                                startupFlags,
                                                startupMode,
                                                lpwszCommandLine,
                                                comGUID,
                                                lpwszRuntimeDllPath);
            }
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/*******************************************************/
/* This is called by the runtime when a method is jitted completely */
/*******************************************************/
void ETW::MethodLog::MethodJitted(MethodDesc* pMethodDesc, SString* namespaceOrClassName, SString* methodName, SString* methodSignature, SIZE_T pCode, ReJITID rejitID)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JIT_KEYWORD))
        {
            ETW::MethodLog::SendMethodEvent(pMethodDesc, ETW::EnumerationLog::EnumerationStructs::JitMethodLoad, TRUE, namespaceOrClassName, methodName, methodSignature, pCode, rejitID);
        }
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JITTEDMETHODILTONATIVEMAP_KEYWORD))
        {
            // The call to SendMethodILToNativeMapEvent assumes that the debugger's lazy
            // data has already been initialized.

            // g_pDebugInterface is initialized on startup on desktop CLR, regardless of whether a debugger
            // or profiler is loaded.  So it should always be available.
            _ASSERTE(g_pDebugInterface != NULL);
            g_pDebugInterface->InitializeLazyDataIfNecessary();

            ETW::MethodLog::SendMethodILToNativeMapEvent(pMethodDesc, ETW::EnumerationLog::EnumerationStructs::JitMethodILToNativeMap, rejitID);
        }
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/*************************************************/
/* This is called by the runtime when method jitting started */
/*************************************************/
void ETW::MethodLog::MethodJitting(MethodDesc* pMethodDesc, SString* namespaceOrClassName, SString* methodName, SString* methodSignature)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(pMethodDesc != NULL);
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_VERBOSE,
                                        CLR_JIT_KEYWORD))
        {
            pMethodDesc->GetMethodInfo(*namespaceOrClassName, *methodName, *methodSignature);
            ETW::MethodLog::SendMethodJitStartEvent(pMethodDesc, namespaceOrClassName, methodName, methodSignature);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/**********************************************************************/
/* This is called by the runtime when a single jit helper method with stub is initialized */
/**********************************************************************/
void ETW::MethodLog::StubInitialized(ULONGLONG ullHelperStartAddress, LPCWSTR pHelperName)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(ullHelperStartAddress != 0);
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JIT_KEYWORD))
        {
            DWORD dwHelperSize = 0;
            Stub::RecoverStubAndSize((TADDR)ullHelperStartAddress, &dwHelperSize);
            ETW::MethodLog::SendHelperEvent(ullHelperStartAddress, dwHelperSize, pHelperName);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/**********************************************************/
/* This is called by the runtime when helpers with stubs are initialized */
/**********************************************************/
void ETW::MethodLog::StubsInitialized(PVOID* pHelperStartAddress, PVOID* pHelperNames, LONG lNoOfHelpers)
{
    WRAPPER_NO_CONTRACT;

    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_JIT_KEYWORD))
    {
        for (int i = 0; i < lNoOfHelpers; i++)
        {
            if (pHelperStartAddress[i])
            {
                StubInitialized((ULONGLONG)pHelperStartAddress[i], (LPCWSTR)pHelperNames[i]);
            }
        }
    }
}

/****************************************************************************/
/* This is called by the runtime when a dynamic method is destroyed */
/****************************************************************************/
void ETW::MethodLog::DynamicMethodDestroyed(MethodDesc* pMethodDesc)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JIT_KEYWORD))
            ETW::MethodLog::SendMethodEvent(pMethodDesc, ETW::EnumerationLog::EnumerationStructs::JitMethodUnload, TRUE);
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when a ngen method is restored */
/****************************************************************************/
void ETW::MethodLog::MethodRestored(MethodDesc* pMethodDesc)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (IsRuntimeNgenKeywordEnabledAndNotSuppressed()
           &&
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_STARTENUMERATION_KEYWORD))
        {
            ETW::MethodLog::SendMethodEvent(pMethodDesc, ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad, FALSE);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when a method table is restored */
/****************************************************************************/
void ETW::MethodLog::MethodTableRestored(MethodTable* pMethodTable)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;
    EX_TRY
    {
        if (IsRuntimeNgenKeywordEnabledAndNotSuppressed()
            &&
            ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                         TRACE_LEVEL_INFORMATION,
                                         CLR_STARTENUMERATION_KEYWORD))
        {
#ifdef FEATURE_REMOTING
            if (!pMethodTable->IsThunking())
#endif
            {
                MethodTable::MethodIterator iter(pMethodTable);
                for (; iter.IsValid(); iter.Next())
                {
                    MethodDesc* pMD = (MethodDesc*)(iter.GetMethodDesc());
                    if (pMD && pMD->IsRestored() && pMD->GetMethodTable_NoLogging() == pMethodTable)
                        ETW::MethodLog::SendMethodEvent(pMD, ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad, FALSE);
                }
            }
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}


/****************************************************************************/
/* This is called by the runtime when a Strong Name Verification Starts */
/****************************************************************************/
void ETW::SecurityLog::StrongNameVerificationStart(DWORD dwInFlags, _In_ LPWSTR strFullyQualifiedAssemblyName)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_CORECLR
#ifndef FEATURE_DTRACE
    FireEtwStrongNameVerificationStart_V1(dwInFlags, 0, strFullyQualifiedAssemblyName, GetClrInstanceId());
#else
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace do not support UNICODE string, they need to be converted to ANSI string
    INT32 nSize = WideCharToMultiByte(strFullyQualifiedAssemblyName, szDtraceOutput1);
    if (nSize != 0)
        FireEtwStrongNameVerificationStart_V1(dwInFlags, 0, szDtraceOutput1, GetClrInstanceId());
#endif
#endif // !FEATURE_CORECLR
}


/****************************************************************************/
/* This is called by the runtime when a Strong Name Verification Ends */
/****************************************************************************/
void ETW::SecurityLog::StrongNameVerificationStop(DWORD dwInFlags, ULONG result, _In_ LPWSTR strFullyQualifiedAssemblyName)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_CORECLR
#ifndef FEATURE_DTRACE
    FireEtwStrongNameVerificationStop_V1(dwInFlags, result, strFullyQualifiedAssemblyName, GetClrInstanceId());
#else
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace do not support UNICODE string, they need to be converted to ANSI string
    INT32 nSize = WideCharToMultiByte(strFullyQualifiedAssemblyName, szDtraceOutput1);
    if (nSize != 0)
        FireEtwStrongNameVerificationStop_V1(dwInFlags, result, szDtraceOutput1, GetClrInstanceId());
#endif
#endif // !FEATURE_CORECLR
}

/****************************************************************************/
/* This is called by the runtime when field transparency calculations begin */
/****************************************************************************/
void ETW::SecurityLog::FireFieldTransparencyComputationStart(LPCWSTR wszFieldName,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwFieldTransparencyComputationStart(wszFieldName, wszModuleName, dwAppDomain, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeField = WideCharToMultiByte(wszFieldName, szDtraceOutput1);
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput2);

    if (nSizeField != 0 && nSizeModule != 0)
        FireEtwFieldTransparencyComputationStart(szDtraceOutput1, szDtraceOutput2, dwAppDomain, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/****************************************************************************/
/* This is called by the runtime when field transparency calculations end   */
/****************************************************************************/
void ETW::SecurityLog::FireFieldTransparencyComputationEnd(LPCWSTR wszFieldName,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain,
    BOOL fIsCritical,
    BOOL fIsTreatAsSafe)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwFieldTransparencyComputationEnd(wszFieldName, wszModuleName, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeField = WideCharToMultiByte(wszFieldName, szDtraceOutput1);
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput2);

    if (nSizeField != 0 && nSizeModule != 0)
        FireEtwFieldTransparencyComputationEnd(szDtraceOutput1, szDtraceOutput2, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/*****************************************************************************/
/* This is called by the runtime when method transparency calculations begin */
/*****************************************************************************/
void ETW::SecurityLog::FireMethodTransparencyComputationStart(LPCWSTR wszMethodName,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwMethodTransparencyComputationStart(wszMethodName, wszModuleName, dwAppDomain, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeMethod = WideCharToMultiByte(wszMethodName, szDtraceOutput1);
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput2);

    if (nSizeMethod != 0 && nSizeModule != 0)
        FireEtwMethodTransparencyComputationStart(szDtraceOutput1, szDtraceOutput2, dwAppDomain, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/*****************************************************************************/
/* This is called by the runtime when method transparency calculations end   */
/********************************************(********************************/
void ETW::SecurityLog::FireMethodTransparencyComputationEnd(LPCWSTR wszMethodName,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain,
    BOOL fIsCritical,
    BOOL fIsTreatAsSafe)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwMethodTransparencyComputationEnd(wszMethodName, wszModuleName, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeMethod = WideCharToMultiByte(wszMethodName, szDtraceOutput1);
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput2);

    if (nSizeMethod != 0 && nSizeModule != 0)
        FireEtwMethodTransparencyComputationEnd(szDtraceOutput1, szDtraceOutput2, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/*****************************************************************************/
/* This is called by the runtime when module transparency calculations begin */
/*****************************************************************************/
void ETW::SecurityLog::FireModuleTransparencyComputationStart(LPCWSTR wszModuleName,
    DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwModuleTransparencyComputationStart(wszModuleName, dwAppDomain, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput1);

    if (nSizeModule != 0)
        FireEtwModuleTransparencyComputationStart(szDtraceOutput1, dwAppDomain, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/****************************************************************************/
/* This is called by the runtime when module transparency calculations end  */
/****************************************************************************/
void ETW::SecurityLog::FireModuleTransparencyComputationEnd(LPCWSTR wszModuleName,
    DWORD dwAppDomain,
    BOOL fIsAllCritical,
    BOOL fIsAllTransparent,
    BOOL fIsTreatAsSafe,
    BOOL fIsOpportunisticallyCritical,
    DWORD dwSecurityRuleSet)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwModuleTransparencyComputationEnd(wszModuleName, dwAppDomain, fIsAllCritical, fIsAllTransparent, fIsTreatAsSafe, fIsOpportunisticallyCritical, dwSecurityRuleSet, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput1);

    if (nSizeModule != 0)
        FireEtwModuleTransparencyComputationEnd(szDtraceOutput1, dwAppDomain, fIsAllCritical, fIsAllTransparent, fIsTreatAsSafe, fIsOpportunisticallyCritical, dwSecurityRuleSet, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/****************************************************************************/
/* This is called by the runtime when token transparency calculations begin */
/****************************************************************************/
void ETW::SecurityLog::FireTokenTransparencyComputationStart(DWORD dwToken,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwTokenTransparencyComputationStart(dwToken, wszModuleName, dwAppDomain, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput1);

    if (nSizeModule != 0)
        FireEtwTokenTransparencyComputationStart(dwToken, szDtraceOutput1, dwAppDomain, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/****************************************************************************/
/* This is called by the runtime when token transparency calculations end   */
/****************************************************************************/
void ETW::SecurityLog::FireTokenTransparencyComputationEnd(DWORD dwToken,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain,
    BOOL fIsCritical,
    BOOL fIsTreatAsSafe)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwTokenTransparencyComputationEnd(dwToken, wszModuleName, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput1);

    if (nSizeModule != 0)
        FireEtwTokenTransparencyComputationEnd(dwToken, szDtraceOutput1, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/*****************************************************************************/
/* This is called by the runtime when type transparency calculations begin   */
/*****************************************************************************/
void ETW::SecurityLog::FireTypeTransparencyComputationStart(LPCWSTR wszTypeName,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwTypeTransparencyComputationStart(wszTypeName, wszModuleName, dwAppDomain, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeType = WideCharToMultiByte(wszTypeName, szDtraceOutput1);
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput2);

    if (nSizeType != 0 && nSizeModule != 0)
        FireEtwTypeTransparencyComputationStart(szDtraceOutput1, szDtraceOutput2, dwAppDomain, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/****************************************************************************/
/* This is called by the runtime when type transparency calculations end    */
/****************************************************************************/
void ETW::SecurityLog::FireTypeTransparencyComputationEnd(LPCWSTR wszTypeName,
    LPCWSTR wszModuleName,
    DWORD dwAppDomain,
    BOOL fIsAllCritical,
    BOOL fIsAllTransparent,
    BOOL fIsCritical,
    BOOL fIsTreatAsSafe)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_DTRACE
    FireEtwTypeTransparencyComputationEnd(wszTypeName, wszModuleName, dwAppDomain, fIsAllCritical, fIsAllTransparent, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#else // FEATURE_DTRACE
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    // since DTrace does not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeType = WideCharToMultiByte(wszTypeName, szDtraceOutput1);
    INT32 nSizeModule = WideCharToMultiByte(wszModuleName, szDtraceOutput2);

    if (nSizeType != 0 && nSizeModule != 0)
        FireEtwTypeTransparencyComputationEnd(szDtraceOutput1, szDtraceOutput2, dwAppDomain, fIsAllCritical, fIsAllTransparent, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
#endif // !FEATURE_DTRACE
}

/**********************************************************************************/
/* This is called by the runtime when a module is loaded */
/* liReportedSharedModule will be 0 when this module is reported for the 1st time */
/**********************************************************************************/
void ETW::LoaderLog::ModuleLoad(Module* pModule, LONG liReportedSharedModule)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        DWORD enumerationOptions = ETW::EnumerationLog::EnumerationStructs::None;
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        KEYWORDZERO))
        {
            BOOL bTraceFlagLoaderSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                                                    TRACE_LEVEL_INFORMATION,
                                                                    CLR_LOADER_KEYWORD);
            BOOL bTraceFlagNgenMethodSet = IsRuntimeNgenKeywordEnabledAndNotSuppressed();
            BOOL bTraceFlagStartRundownSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                                                          TRACE_LEVEL_INFORMATION,
                                                                          CLR_STARTENUMERATION_KEYWORD);
            BOOL bTraceFlagPerfTrackSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
                                                                          TRACE_LEVEL_INFORMATION,
                                                                          CLR_PERFTRACK_KEYWORD);

            if (liReportedSharedModule == 0)
            {

                if (bTraceFlagLoaderSet)
                    enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad;
                if (bTraceFlagPerfTrackSet)
                    enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoad;
                if (bTraceFlagNgenMethodSet && bTraceFlagStartRundownSet)
                    enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad;

                if (pModule->IsManifest() && bTraceFlagLoaderSet)
                    ETW::LoaderLog::SendAssemblyEvent(pModule->GetAssembly(), enumerationOptions);

                if (bTraceFlagLoaderSet || bTraceFlagPerfTrackSet)
                    ETW::LoaderLog::SendModuleEvent(pModule, ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad | ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoad);

                ETW::EnumerationLog::EnumerationHelper(pModule, NULL, enumerationOptions);
            }

            // we want to report domainmodule events whenever they are loaded in any AppDomain
            if (bTraceFlagLoaderSet)
                ETW::LoaderLog::SendModuleEvent(pModule, ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad, TRUE);
        }

#if !defined(FEATURE_PAL)
        {
            BOOL bTraceFlagPerfTrackPrivateSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context,
                                                                                TRACE_LEVEL_INFORMATION,
                                                                                CLR_PERFTRACK_PRIVATE_KEYWORD);
            if (liReportedSharedModule == 0 && bTraceFlagPerfTrackPrivateSet)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoadPrivate;
                ETW::LoaderLog::SendModuleRange(pModule, enumerationOptions);
            }
        }
#endif
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when the process is being shutdown */
/****************************************************************************/
void ETW::EnumerationLog::ProcessShutdown()
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, KEYWORDZERO))
        {
            DWORD enumerationOptions = GetEnumerationOptionsFromRuntimeKeywords();

            // Send unload events for all remaining domains, including shared domain and
            // default domain.
            ETW::EnumerationLog::EnumerationHelper(NULL /* module filter */, NULL /* domain filter */, enumerationOptions);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/****************************************************************************/
/* Beginning of helper functions */
/****************************************************************************/
/****************************************************************************/

/****************************************************************************/
/* This routine is used to send a domain load/unload or rundown event                              */
/****************************************************************************/
void ETW::LoaderLog::SendDomainEvent(BaseDomain* pBaseDomain, DWORD dwEventOptions, LPCWSTR wszFriendlyName)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (!pBaseDomain)
        return;

#ifndef FEATURE_DTRACE
    PCWSTR szDtraceOutput1 = L"";
#else
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
#endif // !FEATURE_DTRACE
    BOOL bIsDefaultDomain = pBaseDomain->IsDefaultDomain();
    BOOL bIsAppDomain = pBaseDomain->IsAppDomain();
    BOOL bIsExecutable = bIsAppDomain ? !(pBaseDomain->AsAppDomain()->IsPassiveDomain()) : FALSE;
    BOOL bIsSharedDomain = pBaseDomain->IsSharedDomain();
    UINT32 uSharingPolicy = bIsAppDomain ? (pBaseDomain->AsAppDomain()->GetSharePolicy()) : 0;

    ULONGLONG ullDomainId = (ULONGLONG)pBaseDomain;
    ULONG ulDomainFlags = ((bIsDefaultDomain ? ETW::LoaderLog::LoaderStructs::DefaultDomain : 0) |
        (bIsExecutable ? ETW::LoaderLog::LoaderStructs::ExecutableDomain : 0) |
        (bIsSharedDomain ? ETW::LoaderLog::LoaderStructs::SharedDomain : 0) |
        (uSharingPolicy << 28));

    LPCWSTR wsEmptyString = L"";
    LPCWSTR wsSharedString = L"SharedDomain";

    LPWSTR lpswzDomainName = (LPWSTR)wsEmptyString;

    if (bIsAppDomain)
    {
        if (wszFriendlyName)
            lpswzDomainName = (PWCHAR)wszFriendlyName;
        else
            lpswzDomainName = (PWCHAR)pBaseDomain->AsAppDomain()->GetFriendlyName();
    }
    else
        lpswzDomainName = (LPWSTR)wsSharedString;

    /* prepare events args for ETW and ETM */
#ifndef FEATURE_DTRACE
    szDtraceOutput1 = (PCWSTR)lpswzDomainName;
#else // !FEATURE_DTRACE
    // since DTrace do not support UNICODE string, they need to be converted to ANSI string
    INT32 nSize = WideCharToMultiByte(lpswzDomainName, szDtraceOutput1);
    if (nSize == 0)
        return;
#endif // !FEATURE_DTRACE

    if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad)
    {
        FireEtwAppDomainLoad_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, pBaseDomain->GetId().m_dwId, GetClrInstanceId());
    }
    else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
    {
        FireEtwAppDomainUnload_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, pBaseDomain->GetId().m_dwId, GetClrInstanceId());
    }
    else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
    {
        FireEtwAppDomainDCStart_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, pBaseDomain->GetId().m_dwId, GetClrInstanceId());
    }
    else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd)
    {
        FireEtwAppDomainDCEnd_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, pBaseDomain->GetId().m_dwId, GetClrInstanceId());
    }
    else
    {
        _ASSERTE((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd));
    }
}

/********************************************************/
/* This routine is used to send thread rundown events when ARM is enabled */
/********************************************************/
void ETW::EnumerationLog::SendThreadRundownEvent()
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

#ifndef DACCESS_COMPILE
    Thread* pThread = NULL;

    // Take the thread store lock while we enumerate threads.
    ThreadStoreLockHolder tsl;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        if (pThread->IsUnstarted() || pThread->IsDead())
            continue;

        // Send thread rundown provider events and thread created runtime provider
        // events (depending on which are enabled)
        ThreadLog::FireThreadDC(pThread);
        ThreadLog::FireThreadCreated(pThread);
    }
#endif // !DACCESS_COMPILE
}

/****************************************************************************/
/* This routine is used to send an assembly load/unload or rundown event ****/
/****************************************************************************/
void ETW::LoaderLog::SendAssemblyEvent(Assembly* pAssembly, DWORD dwEventOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (!pAssembly)
        return;

#ifndef FEATURE_DTRACE
    PCWSTR szDtraceOutput1 = L"";
#else
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
#endif // !FEATURE_DTRACE
    BOOL bIsDynamicAssembly = pAssembly->IsDynamic();
    BOOL bIsCollectibleAssembly = pAssembly->IsCollectible();
    BOOL bIsDomainNeutral = pAssembly->IsDomainNeutral();
    BOOL bHasNativeImage = pAssembly->GetManifestFile()->HasNativeImage();

    ULONGLONG ullAssemblyId = (ULONGLONG)pAssembly;
    ULONGLONG ullDomainId = (ULONGLONG)pAssembly->GetDomain();
    ULONGLONG ullBindingID = 0;
#if (defined FEATURE_PREJIT) && (defined FEATURE_FUSION_DEPRECATE)
    ullBindingID = pAssembly->GetManifestFile()->GetBindingID();
#endif
    ULONG ulAssemblyFlags = ((bIsDomainNeutral ? ETW::LoaderLog::LoaderStructs::DomainNeutralAssembly : 0) |
        (bIsDynamicAssembly ? ETW::LoaderLog::LoaderStructs::DynamicAssembly : 0) |
        (bHasNativeImage ? ETW::LoaderLog::LoaderStructs::NativeAssembly : 0) |
        (bIsCollectibleAssembly ? ETW::LoaderLog::LoaderStructs::CollectibleAssembly : 0));

    SString sAssemblyPath;
    pAssembly->GetDisplayName(sAssemblyPath);
    LPWSTR lpszAssemblyPath = (LPWSTR)sAssemblyPath.GetUnicode();

    /* prepare events args for ETW and ETM */
#ifndef FEATURE_DTRACE
    szDtraceOutput1 = (PCWSTR)lpszAssemblyPath;
#else // !FEATURE_DTRACE
    // since DTrace do not support UNICODE string, they need to be converted to ANSI string
    INT32 nSize = WideCharToMultiByte(lpszAssemblyPath, szDtraceOutput1);
    if (nSize == 0)
        return;
#endif // !FEATURE_DTRACE

    if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad)
    {
        FireEtwAssemblyLoad_V1(ullAssemblyId, ullDomainId, ullBindingID, ulAssemblyFlags, szDtraceOutput1, GetClrInstanceId());
    }
    else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
    {
        FireEtwAssemblyUnload_V1(ullAssemblyId, ullDomainId, ullBindingID, ulAssemblyFlags, szDtraceOutput1, GetClrInstanceId());
    }
    else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
    {
        FireEtwAssemblyDCStart_V1(ullAssemblyId, ullDomainId, ullBindingID, ulAssemblyFlags, szDtraceOutput1, GetClrInstanceId());
    }
    else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd)
    {
        FireEtwAssemblyDCEnd_V1(ullAssemblyId, ullDomainId, ullBindingID, ulAssemblyFlags, szDtraceOutput1, GetClrInstanceId());
    }
    else
    {
        _ASSERTE((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd));
    }
}

#if !defined(FEATURE_PAL)
ETW_INLINE
ULONG
ETW::LoaderLog::SendModuleRange(
    _In_ Module* pModule,
    _In_ DWORD dwEventOptions)

{
    ULONG Result = ERROR_SUCCESS;


    // do not fire the ETW event when:
    // 1. We did not load the native image
    // 2. We do not have IBC data for the native image
    if (!pModule || !pModule->HasNativeImage() || !pModule->IsIbcOptimized())
    {
        return Result;
    }

    // get information about the hot sections from the native image that has been loaded
    COUNT_T cbSizeOfSectionTable;
    CORCOMPILE_VIRTUAL_SECTION_INFO* pVirtualSectionsTable = (CORCOMPILE_VIRTUAL_SECTION_INFO*)pModule->GetNativeImage()->GetVirtualSectionsTable(&cbSizeOfSectionTable);

    COUNT_T RangeCount = cbSizeOfSectionTable / sizeof(CORCOMPILE_VIRTUAL_SECTION_INFO);

    // if we do not have any hot ranges, we do not fire the ETW event

    // Figure out the rest of the event data
    UINT16 ClrInstanceId = GetClrInstanceId();
    UINT64 ModuleID = (ULONGLONG)(TADDR)pModule;

    for (COUNT_T i = 0; i < RangeCount; ++i)
    {
        DWORD rangeBegin = pVirtualSectionsTable[i].VirtualAddress;
        DWORD rangeSize = pVirtualSectionsTable[i].Size;
        DWORD sectionType = pVirtualSectionsTable[i].SectionType;

        UINT8 ibcType = VirtualSectionData::IBCType(sectionType);
        UINT8 rangeType = VirtualSectionData::RangeType(sectionType);
        UINT16 virtualSectionType = VirtualSectionData::VirtualSectionType(sectionType);
        BOOL isIBCProfiledColdSection = VirtualSectionData::IsIBCProfiledColdSection(sectionType);
        if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoad)
        {
            if (isIBCProfiledColdSection)
                Result &= FireEtwModuleRangeLoad(ClrInstanceId, ModuleID, rangeBegin, rangeSize, rangeType);
        }
        else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCStart)
        {
            if (isIBCProfiledColdSection)
                Result &= FireEtwModuleRangeDCStart(ClrInstanceId, ModuleID, rangeBegin, rangeSize, rangeType);
        }
        else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCEnd)
        {
            if (isIBCProfiledColdSection)
                Result &= FireEtwModuleRangeDCEnd(ClrInstanceId, ModuleID, rangeBegin, rangeSize, rangeType);
        }
        // Fire private events if they are requested.
        if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoadPrivate)
        {
            Result &= FireEtwModuleRangeLoadPrivate(ClrInstanceId, ModuleID, rangeBegin, rangeSize, rangeType, ibcType, virtualSectionType);
        }
    }
    return Result;
}
#endif // !FEATURE_PAL

#ifndef FEATURE_DTRACE
//---------------------------------------------------------------------------------------
//
// Helper that takes a module, and returns the managed and native PDB information
// corresponding to that module. Used by the routine that fires the module load / unload
// events.
//
// Arguments:
//      * pModule - Module to examine
//      * pCvInfoIL - [out] CV_INFO_PDB70 corresponding to managed PDB for this module
//          (the last debug directory entry in the PE File), if it exists. If it doesn't
//          exist, this is zeroed out.
//      * pCvInfoNative - [out] CV_INFO_PDB70 corresponding to native NGEN PDB for this
//          module (the next-to-last debug directory entry in the PE File), if it exists.
//          If it doesn't exist, this is zeroed out.
//
// Notes:
//     * This method only understands the CV_INFO_PDB70 / RSDS format. If the format
//         changes, this function will act as if there are no debug directory entries.
//         Module load / unload events will still be fired, but all PDB info will be
//         zeroed out.
//     * The raw data in the PE file's debug directory entries are assumed to be
//         untrusted, and reported sizes of buffers are verified against their data.
//

static void GetCodeViewInfo(Module* pModule, CV_INFO_PDB70* pCvInfoIL, CV_INFO_PDB70* pCvInfoNative)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pModule != NULL);
    _ASSERTE(pCvInfoIL != NULL);
    _ASSERTE(pCvInfoNative != NULL);

    ZeroMemory(pCvInfoIL, sizeof(*pCvInfoIL));
    ZeroMemory(pCvInfoNative, sizeof(*pCvInfoNative));

    PTR_PEFile pPEFile = pModule->GetFile();
    _ASSERTE(pPEFile != NULL);

    PTR_PEImageLayout pLayout = NULL;
    if (pPEFile->HasNativeImage())
    {
        pLayout = pPEFile->GetLoadedNative();
    }
    else if (pPEFile->HasOpenedILimage())
    {
        pLayout = pPEFile->GetLoadedIL();
    }

    if (pLayout == NULL)
    {
        // This can happen for reflection-loaded modules
        return;
    }

    if (!pLayout->HasNTHeaders())
    {
        // Without NT headers, we'll have a tough time finding the debug directory
        // entries. This can happen for nlp files.
        return;
    }

    if (!pLayout->HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG))
        return;

    COUNT_T cbDebugEntries;
    IMAGE_DEBUG_DIRECTORY* rgDebugEntries =
        (IMAGE_DEBUG_DIRECTORY*)pLayout->GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_DEBUG, &cbDebugEntries);

    if (cbDebugEntries < sizeof(IMAGE_DEBUG_DIRECTORY))
        return;

    // Since rgDebugEntries is an array of IMAGE_DEBUG_DIRECTORYs, cbDebugEntries
    // should be a multiple of sizeof(IMAGE_DEBUG_DIRECTORY).
    if (cbDebugEntries % sizeof(IMAGE_DEBUG_DIRECTORY) != 0)
        return;

    // Temporary storage for a CV_INFO_PDB70 and its size (which could be less than
    // sizeof(CV_INFO_PDB70); see below).
    struct PdbInfo
    {
        CV_INFO_PDB70* m_pPdb70;
        ULONG               m_cbPdb70;
    };

    // Iterate through all debug directory entries.  The very last one will be the
    // managed PDB entry.  The next to last one (if it exists) will be the (native) NGEN
    // PDB entry.  Treat raw bytes we read as untrusted.
    PdbInfo pdbInfoLast = { 0 };
    PdbInfo pdbInfoNextToLast = { 0 };
    int cEntries = cbDebugEntries / sizeof(IMAGE_DEBUG_DIRECTORY);
    for (int i = 0; i < cEntries; i++)
    {
        if (rgDebugEntries[i].Type != IMAGE_DEBUG_TYPE_CODEVIEW)
            continue;

        // Get raw data pointed to by this IMAGE_DEBUG_DIRECTORY

        // Some compilers set PointerToRawData but not AddressOfRawData as they put the
        // data at the end of the file in an unmapped part of the file
        RVA rvaOfRawData = (rgDebugEntries[i].AddressOfRawData != NULL) ?
            rgDebugEntries[i].AddressOfRawData :
            pLayout->OffsetToRva(rgDebugEntries[i].PointerToRawData);

        ULONG cbDebugData = rgDebugEntries[i].SizeOfData;
        if (cbDebugData < (offsetof(CV_INFO_PDB70, magic) + sizeof(((CV_INFO_PDB70*)0)->magic)))
        {
            // raw data too small to contain magic number at expected spot, so its format
            // is not recognizable. Skip
            continue;
        }

        if (!pLayout->CheckRva(rvaOfRawData, cbDebugData))
        {
            // Memory claimed to belong to the raw data does not fit.
            // IMAGE_DEBUG_DIRECTORY is outright corrupt. Do not include PDB info in
            // event at all.
            return;
        }

        // Verify the magic number is as expected
        CV_INFO_PDB70* pPdb70 = (CV_INFO_PDB70*)pLayout->GetRvaData(rvaOfRawData);
        if (pPdb70->magic != CV_SIGNATURE_RSDS)
        {
            // Unrecognized magic number.  Skip
            continue;
        }

        // From this point forward, the format should adhere to the expected layout of
        // CV_INFO_PDB70. If we find otherwise, then assume the IMAGE_DEBUG_DIRECTORY is
        // outright corrupt, and do not include PDB info in event at all. The caller will
        // still fire the module event, but have zeroed-out / empty PDB fields.

        // Verify sane size of raw data
        if (cbDebugData > sizeof(CV_INFO_PDB70))
            return;

        // cbDebugData actually can be < sizeof(CV_INFO_PDB70), since the "path" field
        // can be truncated to its actual data length (i.e., fewer than MAX_PATH chars
        // may be present in the PE file). In some cases, though, cbDebugData will
        // include all MAX_PATH chars even though path gets null-terminated well before
        // the MAX_PATH limit.

        // Gotta have at least one byte of the path
        if (cbDebugData < offsetof(CV_INFO_PDB70, path) + sizeof(char))
            return;

        // How much space is available for the path?
        size_t cchPathMaxIncludingNullTerminator = (cbDebugData - offsetof(CV_INFO_PDB70, path)) / sizeof(char);
        _ASSERTE(cchPathMaxIncludingNullTerminator >= 1);   // Guaranteed above

        // Verify path string fits inside the declared size
        size_t cchPathActualExcludingNullTerminator = strnlen(pPdb70->path, cchPathMaxIncludingNullTerminator);
        if (cchPathActualExcludingNullTerminator == cchPathMaxIncludingNullTerminator)
        {
            // This is how strnlen indicates failure--it couldn't find the null
            // terminator within the buffer size specified
            return;
        }

        // Looks valid.  Remember it.
        pdbInfoNextToLast = pdbInfoLast;
        pdbInfoLast.m_pPdb70 = pPdb70;
        pdbInfoLast.m_cbPdb70 = cbDebugData;
    }

    // Return whatever we found

    if (pdbInfoLast.m_pPdb70 != NULL)
    {
        // The last guy is the IL (managed) PDB info
        _ASSERTE(pdbInfoLast.m_cbPdb70 <= sizeof(*pCvInfoIL));      // Guaranteed by checks above
        memcpy(pCvInfoIL, pdbInfoLast.m_pPdb70, pdbInfoLast.m_cbPdb70);
    }

    if (pdbInfoNextToLast.m_pPdb70 != NULL)
    {
        // The next-to-last guy is the NGEN (native) PDB info
        _ASSERTE(pdbInfoNextToLast.m_cbPdb70 <= sizeof(*pCvInfoNative));      // Guaranteed by checks above
        memcpy(pCvInfoNative, pdbInfoNextToLast.m_pPdb70, pdbInfoNextToLast.m_cbPdb70);
    }
}
#endif // FEATURE_DTRACE



//---------------------------------------------------------------------------------------
//
// send a module load/unload or rundown event and domainmodule load and rundown event
//
// Arguments:
//      * pModule - Module loading or unloading
//      * dwEventOptions - Bitmask of which events to fire
//      * bFireDomainModuleEvents - nonzero if we are to fire DomainModule events; zero
//          if we are to fire Module events
//
void ETW::LoaderLog::SendModuleEvent(Module* pModule, DWORD dwEventOptions, BOOL bFireDomainModuleEvents)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (!pModule)
        return;

#ifndef FEATURE_DTRACE
    PCWSTR szDtraceOutput1 = L"", szDtraceOutput2 = L"";
#else
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
#endif // !FEATURE_DTRACE
    BOOL bIsDynamicAssembly = pModule->GetAssembly()->IsDynamic();
    BOOL bHasNativeImage = FALSE;
#ifdef FEATURE_PREJIT
    bHasNativeImage = pModule->HasNativeImage();
#endif // FEATURE_PREJIT
    BOOL bIsManifestModule = pModule->IsManifest();
    ULONGLONG ullAppDomainId = 0; // This is used only with DomainModule events
    ULONGLONG ullModuleId = (ULONGLONG)(TADDR)pModule;
    ULONGLONG ullAssemblyId = (ULONGLONG)pModule->GetAssembly();
    BOOL bIsDomainNeutral = pModule->GetAssembly()->IsDomainNeutral();
    BOOL bIsIbcOptimized = FALSE;
    if (bHasNativeImage)
    {
        bIsIbcOptimized = pModule->IsIbcOptimized();
    }
    ULONG ulReservedFlags = 0;
    ULONG ulFlags = ((bIsDomainNeutral ? ETW::LoaderLog::LoaderStructs::DomainNeutralModule : 0) |
        (bHasNativeImage ? ETW::LoaderLog::LoaderStructs::NativeModule : 0) |
        (bIsDynamicAssembly ? ETW::LoaderLog::LoaderStructs::DynamicModule : 0) |
        (bIsManifestModule ? ETW::LoaderLog::LoaderStructs::ManifestModule : 0) |
        (bIsIbcOptimized ? ETW::LoaderLog::LoaderStructs::IbcOptimized : 0));

#ifndef FEATURE_DTRACE
    // Grab PDB path, guid, and age for managed PDB and native (NGEN) PDB when
    // available.  Any failures are not fatal.  The corresponding PDB info will remain
    // zeroed out, and that's what we'll include in the event.
    CV_INFO_PDB70 cvInfoIL = { 0 };
    CV_INFO_PDB70 cvInfoNative = { 0 };
    GetCodeViewInfo(pModule, &cvInfoIL, &cvInfoNative);
#endif // FEATURE_DTRACE

    PWCHAR ModuleILPath = L"", ModuleNativePath = L"";

    if (bFireDomainModuleEvents)
    {
        if (pModule->GetDomain()->IsSharedDomain()) // for shared domains, we do not fire domainmodule event
            return;
        ullAppDomainId = (ULONGLONG)pModule->FindDomainAssembly(pModule->GetDomain()->AsAppDomain())->GetAppDomain();
    }

    LPCWSTR pEmptyString = L"";
#ifndef FEATURE_PAL
    SString moduleName = L"";
#else // !FEATURE_PAL
    SString moduleName;
#endif // !FEATURE_PAL
    if (!bIsDynamicAssembly)
    {
        ModuleILPath = (PWCHAR)pModule->GetAssembly()->GetManifestFile()->GetILimage()->GetPath().GetUnicode();
        ModuleNativePath = (PWCHAR)pEmptyString;

#ifdef FEATURE_PREJIT
        if (bHasNativeImage)
            ModuleNativePath = (PWCHAR)pModule->GetNativeImage()->GetPath().GetUnicode();
#endif // FEATURE_PREJIT
    }

    // if we do not have a module path yet, we put the module name
    if (bIsDynamicAssembly || ModuleILPath == NULL || wcslen(ModuleILPath) <= 2)
    {
        moduleName.SetUTF8(pModule->GetSimpleName());
        ModuleILPath = (PWCHAR)moduleName.GetUnicode();
        ModuleNativePath = (PWCHAR)pEmptyString;
    }

    /* prepare events args for ETW and ETM */
#ifndef FEATURE_DTRACE
    szDtraceOutput1 = (PCWSTR)ModuleILPath;
    szDtraceOutput2 = (PCWSTR)ModuleNativePath;

    // Convert PDB paths to UNICODE
    StackSString managedPdbPath(SString::Utf8, cvInfoIL.path);
    StackSString nativePdbPath(SString::Utf8, cvInfoNative.path);
#else // !FEATURE_DTRACE
    // since DTrace do not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeOfILPath = WideCharToMultiByte(ModuleILPath, szDtraceOutput1);
    if (nSizeOfILPath == 0)
        return;
    INT32 nSizeOfNativePath = WideCharToMultiByte(ModuleNativePath, szDtraceOutput2);
    if (nSizeOfNativePath == 0)
        return;
#endif // !FEATURE_DTRACE

    if (bFireDomainModuleEvents)
    {
        if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad)
        {
            FireEtwDomainModuleLoad_V1(ullModuleId, ullAssemblyId, ullAppDomainId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId());
        }
        else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
        {
            FireEtwDomainModuleDCStart_V1(ullModuleId, ullAssemblyId, ullAppDomainId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId());
        }
        else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd)
        {
            FireEtwDomainModuleDCEnd_V1(ullModuleId, ullAssemblyId, ullAppDomainId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId());
        }
        else
        {
            _ASSERTE((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad) ||
                (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) ||
                (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd));
        }
    }
    else
    {
        if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad) || (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoad))
        {
            FireEtwModuleLoad_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId(), &cvInfoIL.signature, cvInfoIL.age, managedPdbPath, &cvInfoNative.signature, cvInfoNative.age, nativePdbPath);
        }
        else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
        {
            FireEtwModuleUnload_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId(), &cvInfoIL.signature, cvInfoIL.age, managedPdbPath, &cvInfoNative.signature, cvInfoNative.age, nativePdbPath);
        }
        else if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) || (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCStart))
        {
            FireEtwModuleDCStart_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId(), &cvInfoIL.signature, cvInfoIL.age, managedPdbPath, &cvInfoNative.signature, cvInfoNative.age, nativePdbPath);
        }
        else if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) || (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCEnd))
        {
            FireEtwModuleDCEnd_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId(), &cvInfoIL.signature, cvInfoIL.age, managedPdbPath, &cvInfoNative.signature, cvInfoNative.age, nativePdbPath);
        }
        else
        {
            _ASSERTE((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad) ||
                (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload) ||
                (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) ||
                (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
                (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeEnabledAny));

        }
#if !defined(FEATURE_PAL)
        if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeEnabledAny)
        {
            // Fire ModuleRangeLoad, ModuleRangeDCStart, ModuleRangeDCEnd or ModuleRangeLoadPrivate event for this Module
            SendModuleRange(pModule, dwEventOptions);
        }
#endif
    }
}

/*****************************************************************/
/* This routine is used to send an ETW event just before a method starts jitting*/
/*****************************************************************/
void ETW::MethodLog::SendMethodJitStartEvent(MethodDesc* pMethodDesc, SString* namespaceOrClassName, SString* methodName, SString* methodSignature)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    Module* pModule = NULL;
    Module* pLoaderModule = NULL; // This must not be used except for getting the ModuleID

    ULONGLONG ullMethodIdentifier = 0;
    ULONGLONG ullModuleID = 0;
    ULONG ulMethodToken = 0;
    ULONG ulMethodILSize = 0;
#ifndef FEATURE_DTRACE
    PCWSTR szDtraceOutput1 = L"", szDtraceOutput2 = L"", szDtraceOutput3 = L"";
#else
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput3[DTRACE_OUTPUT_STRING_LEN];
#endif // !FEATURE_DTRACE

    if (pMethodDesc) {
        pModule = pMethodDesc->GetModule_NoLogging();

        if (!pMethodDesc->IsRestored()) {
            return;
        }

        bool bIsDynamicMethod = pMethodDesc->IsDynamicMethod();
        BOOL bIsGenericMethod = FALSE;
        if (pMethodDesc->GetMethodTable_NoLogging())
            bIsGenericMethod = pMethodDesc->HasClassOrMethodInstantiation_NoLogging();

        ullModuleID = (ULONGLONG)(TADDR)pModule;
        ullMethodIdentifier = (ULONGLONG)pMethodDesc;

        // Use MethodDesc if Dynamic or Generic methods
        if (bIsDynamicMethod || bIsGenericMethod)
        {
            if (bIsGenericMethod)
                ulMethodToken = (ULONG)pMethodDesc->GetMemberDef_NoLogging();
            if (bIsDynamicMethod) // if its a generic and a dynamic method, we would set the methodtoken to 0
                ulMethodToken = (ULONG)0;
        }
        else
            ulMethodToken = (ULONG)pMethodDesc->GetMemberDef_NoLogging();

        if (pMethodDesc->IsIL())
        {
            COR_ILMETHOD_DECODER::DecoderStatus decoderstatus = COR_ILMETHOD_DECODER::FORMAT_ERROR;
            COR_ILMETHOD_DECODER ILHeader(pMethodDesc->GetILHeader(), pMethodDesc->GetMDImport(), &decoderstatus);
            ulMethodILSize = (ULONG)ILHeader.GetCodeSize();
        }

        SString tNamespace, tMethodName, tMethodSignature;
        if (!namespaceOrClassName || !methodName || !methodSignature || (methodName->IsEmpty() && namespaceOrClassName->IsEmpty() && methodSignature->IsEmpty()))
        {
            pMethodDesc->GetMethodInfo(tNamespace, tMethodName, tMethodSignature);
            namespaceOrClassName = &tNamespace;
            methodName = &tMethodName;
            methodSignature = &tMethodSignature;
        }

        // fire method information
        /* prepare events args for ETW and ETM */
#ifndef FEATURE_DTRACE
        szDtraceOutput1 = (PCWSTR)namespaceOrClassName->GetUnicode();
        szDtraceOutput2 = (PCWSTR)methodName->GetUnicode();
        szDtraceOutput3 = (PCWSTR)methodSignature->GetUnicode();
#else // !FEATURE_DTRACE
        // since DTrace do not support UNICODE string, they need to be converted to ANSI string
        INT32 nSizeOfNamespaceOrClassName = WideCharToMultiByte((PCWSTR)namespaceOrClassName->GetUnicode(), szDtraceOutput1);
        if (nSizeOfNamespaceOrClassName == 0)
            return;
        INT32 nSizeOfMethodName = WideCharToMultiByte((PCWSTR)methodName->GetUnicode(), szDtraceOutput2);
        if (nSizeOfMethodName == 0)
            return;
        INT32 nSizeMethodsignature = WideCharToMultiByte((PCWSTR)methodSignature->GetUnicode(), szDtraceOutput3);
        if (nSizeMethodsignature == 0)
            return;
#endif // !FEATURE_DTRACE

        FireEtwMethodJittingStarted_V1(ullMethodIdentifier,
            ullModuleID,
            ulMethodToken,
            ulMethodILSize,
            szDtraceOutput1,
            szDtraceOutput2,
            szDtraceOutput3,
            GetClrInstanceId());
    }
}

/****************************************************************************/
/* This routine is used to send a method load/unload or rundown event                              */
/****************************************************************************/
void ETW::MethodLog::SendMethodEvent(MethodDesc* pMethodDesc, DWORD dwEventOptions, BOOL bIsJit, SString* namespaceOrClassName, SString* methodName, SString* methodSignature, SIZE_T pCode, ReJITID rejitID)
{
    CONTRACTL{
        THROWS;
        GC_NOTRIGGER;
        SO_NOT_MAINLINE;
    } CONTRACTL_END;

    Module* pModule = NULL;
    Module* pLoaderModule = NULL; // This must not be used except for getting the ModuleID
    ULONGLONG ullMethodStartAddress = 0, ullColdMethodStartAddress = 0, ullModuleID = 0, ullMethodIdentifier = 0;
    ULONG ulMethodSize = 0, ulColdMethodSize = 0, ulMethodToken = 0, ulMethodFlags = 0, ulColdMethodFlags = 0;
    PWCHAR pMethodName = NULL, pNamespaceName = NULL, pMethodSignature = NULL;
    BOOL bHasNativeImage = FALSE, bShowVerboseOutput = FALSE, bIsDynamicMethod = FALSE, bHasSharedGenericCode = FALSE, bIsGenericMethod = FALSE;
#ifndef FEATURE_DTRACE
    PCWSTR szDtraceOutput1 = L"", szDtraceOutput2 = L"", szDtraceOutput3 = L"";
#else
    CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput2[DTRACE_OUTPUT_STRING_LEN];
    CHAR szDtraceOutput3[DTRACE_OUTPUT_STRING_LEN];
#endif // !FEATURE_DTRACE

    BOOL bIsRundownProvider = ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCStart) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCEnd) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd));

    BOOL bIsRuntimeProvider = ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoad) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnload) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodUnload));

    if (pMethodDesc == NULL)
        return;

    if (!pMethodDesc->IsRestored())
    {
        // Forcibly restoring ngen methods can cause all sorts of deadlocks and contract violations
        // These events are therefore put under the private provider
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_PRIVATENGENFORCERESTORE_KEYWORD))
        {
            PERMANENT_CONTRACT_VIOLATION(GCViolation, ReasonNonShippingCode);
            pMethodDesc->CheckRestore();
        }
        else
        {
            return;
        }
    }


    if (bIsRundownProvider)
    {
        bShowVerboseOutput = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
            TRACE_LEVEL_VERBOSE,
            KEYWORDZERO);
    }
    else if (bIsRuntimeProvider)
    {
        bShowVerboseOutput = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
            TRACE_LEVEL_VERBOSE,
            KEYWORDZERO);
    }

    pModule = pMethodDesc->GetModule_NoLogging();
#ifdef FEATURE_PREJIT
    bHasNativeImage = pModule->HasNativeImage();
#endif // FEATURE_PREJIT
    bIsDynamicMethod = (BOOL)pMethodDesc->IsDynamicMethod();
    bHasSharedGenericCode = pMethodDesc->IsSharedByGenericInstantiations();

    if (pMethodDesc->GetMethodTable_NoLogging())
        bIsGenericMethod = pMethodDesc->HasClassOrMethodInstantiation_NoLogging();

    ulMethodFlags = ((ulMethodFlags |
        (bHasSharedGenericCode ? ETW::MethodLog::MethodStructs::SharedGenericCode : 0) |
        (bIsGenericMethod ? ETW::MethodLog::MethodStructs::GenericMethod : 0) |
        (bIsDynamicMethod ? ETW::MethodLog::MethodStructs::DynamicMethod : 0) |
        (bIsJit ? ETW::MethodLog::MethodStructs::JittedMethod : 0)));

    // Intentionally set the extent flags (cold vs. hot) only after all the other common
    // flags (above) have been set.
    ulColdMethodFlags = ulMethodFlags | ETW::MethodLog::MethodStructs::ColdSection; // Method Extent (bits 28, 29, 30, 31)
    ulMethodFlags = ulMethodFlags | ETW::MethodLog::MethodStructs::HotSection;         // Method Extent (bits 28, 29, 30, 31)

    // MethodDesc ==> Code Address ==>JitManager
    TADDR start = pCode ? pCode : PCODEToPINSTR(pMethodDesc->GetNativeCode());
    if (start == 0) {
        // this method hasn't been jitted
        return;
    }

    // EECodeInfo is technically initialized by a "PCODE", but it can also be initialized
    // by a TADDR (i.e., w/out thumb bit set on ARM)
    EECodeInfo codeInfo(start);

    // MethodToken ==> MethodRegionInfo
    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    ullMethodStartAddress = (ULONGLONG)methodRegionInfo.hotStartAddress;
    ulMethodSize = (ULONG)methodRegionInfo.hotSize;

    ullModuleID = (ULONGLONG)(TADDR)pModule;
    ullMethodIdentifier = (ULONGLONG)pMethodDesc;

    // Use MethodDesc if Dynamic or Generic methods
    if (bIsDynamicMethod || bIsGenericMethod)
    {
        bShowVerboseOutput = TRUE;
        if (bIsGenericMethod)
            ulMethodToken = (ULONG)pMethodDesc->GetMemberDef_NoLogging();
        if (bIsDynamicMethod) // if its a generic and a dynamic method, we would set the methodtoken to 0
            ulMethodToken = (ULONG)0;
    }
    else
        ulMethodToken = (ULONG)pMethodDesc->GetMemberDef_NoLogging();

    if (bHasNativeImage)
    {
        ullColdMethodStartAddress = (ULONGLONG)methodRegionInfo.coldStartAddress;
        ulColdMethodSize = (ULONG)methodRegionInfo.coldSize; // methodRegionInfo.coldSize is size_t and info.MethodLoadInfo.MethodSize is 32 bit; will give incorrect values on a 64-bit machine
    }

    SString tNamespace, tMethodName, tMethodSignature;

    // if verbose method load info needed, only then
    // find method name and signature and fire verbose method load info
    if (bShowVerboseOutput)
    {
        if (!namespaceOrClassName || !methodName || !methodSignature || (methodName->IsEmpty() && namespaceOrClassName->IsEmpty() && methodSignature->IsEmpty()))
        {
            pMethodDesc->GetMethodInfo(tNamespace, tMethodName, tMethodSignature);
            namespaceOrClassName = &tNamespace;
            methodName = &tMethodName;
            methodSignature = &tMethodSignature;
        }
        pNamespaceName = (PWCHAR)namespaceOrClassName->GetUnicode();
        pMethodName = (PWCHAR)methodName->GetUnicode();
        pMethodSignature = (PWCHAR)methodSignature->GetUnicode();
    }

    BOOL bFireEventForColdSection = (bHasNativeImage && ullColdMethodStartAddress && ulColdMethodSize);

    /* prepare events args for ETW and ETM */
#ifndef FEATURE_DTRACE
    szDtraceOutput1 = (PCWSTR)pNamespaceName;
    szDtraceOutput2 = (PCWSTR)pMethodName;
    szDtraceOutput3 = (PCWSTR)pMethodSignature;
#else // !FEATURE_DTRACE
    // since DTrace do not support UNICODE string, they need to be converted to ANSI string
    INT32 nSizeTempNamespaceName = WideCharToMultiByte(pNamespaceName, szDtraceOutput1);
    if (nSizeTempNamespaceName == 0)
        return;
    INT32 nSizeTempMethodName = WideCharToMultiByte(pMethodName, szDtraceOutput2);
    if (nSizeTempMethodName == 0)
        return;
    INT32 nSizeMothodSignature = WideCharToMultiByte(pMethodSignature, szDtraceOutput3);
    if (nSizeMothodSignature == 0)
        return;
#endif // !FEATURE_DTRACE

    if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoad) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad))
    {
        if (bShowVerboseOutput)
        {
            FireEtwMethodLoadVerbose_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                szDtraceOutput1,
                szDtraceOutput2,
                szDtraceOutput3,
                GetClrInstanceId(),
                rejitID);
        }
        else
        {
            FireEtwMethodLoad_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                GetClrInstanceId(),
                rejitID);
        }
        if (bFireEventForColdSection)
        {
            if (bShowVerboseOutput)
            {
                FireEtwMethodLoadVerbose_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    szDtraceOutput1,
                    szDtraceOutput2,
                    szDtraceOutput3,
                    GetClrInstanceId(),
                    rejitID);
            }
            else
            {
                FireEtwMethodLoad_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    GetClrInstanceId(),
                    rejitID);
            }
        }
    }
    else if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnload) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodUnload))
    {
        if (bShowVerboseOutput)
        {
            FireEtwMethodUnloadVerbose_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                szDtraceOutput1,
                szDtraceOutput2,
                szDtraceOutput3,
                GetClrInstanceId(),
                rejitID);
        }
        else
        {
            FireEtwMethodUnload_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                GetClrInstanceId(),
                rejitID);
        }
        if (bFireEventForColdSection)
        {
            if (bShowVerboseOutput)
            {
                FireEtwMethodUnloadVerbose_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    szDtraceOutput1,
                    szDtraceOutput2,
                    szDtraceOutput3,
                    GetClrInstanceId(),
                    rejitID);
            }
            else
            {
                FireEtwMethodUnload_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    GetClrInstanceId(),
                    rejitID);
            }
        }
    }
    else if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCStart) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart))
    {
        if (bShowVerboseOutput)
        {
            FireEtwMethodDCStartVerbose_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                szDtraceOutput1,
                szDtraceOutput2,
                szDtraceOutput3,
                GetClrInstanceId(),
                rejitID);
        }
        else
        {
            FireEtwMethodDCStart_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                GetClrInstanceId(),
                rejitID);
        }
        if (bFireEventForColdSection)
        {
            if (bShowVerboseOutput)
            {
                FireEtwMethodDCStartVerbose_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    szDtraceOutput1,
                    szDtraceOutput2,
                    szDtraceOutput3,
                    GetClrInstanceId(),
                    rejitID);
            }
            else
            {
                FireEtwMethodDCStart_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    GetClrInstanceId(),
                    rejitID);
            }
        }
    }
    else if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCEnd) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd))
    {
        if (bShowVerboseOutput)
        {
            FireEtwMethodDCEndVerbose_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                szDtraceOutput1,
                szDtraceOutput2,
                szDtraceOutput3,
                GetClrInstanceId(),
                rejitID);
        }
        else
        {
            FireEtwMethodDCEnd_V1_or_V2(ullMethodIdentifier,
                ullModuleID,
                ullMethodStartAddress,
                ulMethodSize,
                ulMethodToken,
                ulMethodFlags,
                GetClrInstanceId(),
                rejitID);
        }
        if (bFireEventForColdSection)
        {
            if (bShowVerboseOutput)
            {
                FireEtwMethodDCEndVerbose_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    szDtraceOutput1,
                    szDtraceOutput2,
                    szDtraceOutput3,
                    GetClrInstanceId(),
                    rejitID);
            }
            else
            {
                FireEtwMethodDCEnd_V1_or_V2(ullMethodIdentifier,
                    ullModuleID,
                    ullColdMethodStartAddress,
                    ulColdMethodSize,
                    ulMethodToken,
                    ulColdMethodFlags,
                    GetClrInstanceId(),
                    rejitID);
            }
        }
    }
    else
    {
        _ASSERTE((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoad) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnload) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCStart) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCEnd) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodUnload) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart) ||
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd));
    }
}

// This event cannot be supported yet on coreclr, since Silverlight needs to support
// XP, and this event uses a format (dynamic-sized arrays) only supported by the
// Vista+ Crimson event format. So stub out the whole function to a no-op on pre-Vista
// platforms.
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
//---------------------------------------------------------------------------------------
//
// Fires the IL-to-native map event for JITted methods.  This is used for the runtime,
// rundown start, and rundown end events that include the il-to-native map information
//
// Arguments:
//      pMethodDesc - MethodDesc for which we'll fire the map event
//      dwEventOptions - Options that tells us, in the rundown case, whether we're
//                       supposed to fire the start or end rundown events.
//

// static
void ETW::MethodLog::SendMethodILToNativeMapEvent(MethodDesc* pMethodDesc, DWORD dwEventOptions, ReJITID rejitID)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    // This is the limit on how big the il-to-native map can get, as measured by number
    // of entries in each parallel array (IL offset array and native offset array).
    // This number was chosen to ensure the overall event stays under the Windows limit
    // of 64K
    const USHORT kMapEntriesMax = 7000;

    if (pMethodDesc == NULL)
        return;

    if (pMethodDesc->HasClassOrMethodInstantiation() && pMethodDesc->IsTypicalMethodDefinition())
        return;

    // g_pDebugInterface is initialized on startup on desktop CLR, regardless of whether a debugger
    // or profiler is loaded.  So it should always be available.
    _ASSERTE(g_pDebugInterface != NULL);

    ULONGLONG ullMethodIdentifier = (ULONGLONG)pMethodDesc;

    USHORT cMap;
    NewArrayHolder<UINT> rguiILOffset;
    NewArrayHolder<UINT> rguiNativeOffset;

    HRESULT hr = g_pDebugInterface->GetILToNativeMappingIntoArrays(
        pMethodDesc,
        kMapEntriesMax,
        &cMap,
        &rguiILOffset,
        &rguiNativeOffset);
    if (FAILED(hr))
        return;

    // Runtime provider.
    //
    // This macro already checks for the JittedMethodILToNativeMapKeyword before
    // choosing to fire the event
    if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodILToNativeMap) != 0)
    {
        FireEtwMethodILToNativeMap(
            ullMethodIdentifier,
            rejitID,
            0,          // Extent:  This event is only sent for JITted (not NGENd) methods, and
            //          currently there is only one extent (hot) for JITted methods.
            cMap,
            rguiILOffset,
            rguiNativeOffset,
            GetClrInstanceId());
    }

    // Rundown provider
    //
    // These macros already check for the JittedMethodILToNativeMapRundownKeyword
    // before choosing to fire the event--we further check our options to see if we
    // should fire the Start and / or End flavor of the event (since the keyword alone
    // is insufficient to distinguish these).
    //
    // (for an explanation of the parameters see the FireEtwMethodILToNativeMap call above)
    if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::MethodDCStartILToNativeMap) != 0)
        FireEtwMethodDCStartILToNativeMap(ullMethodIdentifier, 0, 0, cMap, rguiILOffset, rguiNativeOffset, GetClrInstanceId());
    if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::MethodDCEndILToNativeMap) != 0)
        FireEtwMethodDCEndILToNativeMap(ullMethodIdentifier, 0, 0, cMap, rguiILOffset, rguiNativeOffset, GetClrInstanceId());
}
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT


void ETW::MethodLog::SendHelperEvent(ULONGLONG ullHelperStartAddress, ULONG ulHelperSize, LPCWSTR pHelperName)
{
    WRAPPER_NO_CONTRACT;
    if (pHelperName)
    {
#ifndef FEATURE_DTRACE
        PCWSTR szDtraceOutput1 = L"";
#else
        CHAR szDtraceOutput1[DTRACE_OUTPUT_STRING_LEN];
#endif // !FEATURE_DTRACE
        ULONG methodFlags = ETW::MethodLog::MethodStructs::JitHelperMethod; // helper flag set
#ifndef FEATURE_DTRACE
        FireEtwMethodLoadVerbose_V1(ullHelperStartAddress,
            0,
            ullHelperStartAddress,
            ulHelperSize,
            0,
            methodFlags,
            NULL,
            pHelperName,
            NULL,
            GetClrInstanceId());
#else // !FEATURE_DTRACE
         // since DTrace do not support UNICODE string, they need to be converted to ANSI string
        INT32 nTempHelperName = WideCharToMultiByte(pHelperName, szDtraceOutput1);
        if (nTempHelperName == 0)
            return;
        // in the action, printf, of DTtrace, it cannot print an arg with value NULL when the format is set %s.
        // Dtrace does not provide the condition statement so that we give a string "NULL" to it.
        FireEtwMethodLoadVerbose_V1(ullHelperStartAddress,
            0,
            ullHelperStartAddress,
            ulHelperSize,
            0,
            methodFlags,
            szDtraceOutputNULL,
            szDtraceOutput1,
            szDtraceOutputNULL,
            GetClrInstanceId());
#endif // !FEATURE_DTRACE
    }
}


/****************************************************************************/
/* This routine sends back method events of type 'dwEventOptions', for all
   NGEN methods in pModule */
   /****************************************************************************/
void ETW::MethodLog::SendEventsForNgenMethods(Module* pModule, DWORD dwEventOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

#ifdef FEATURE_PREJIT
    if (!pModule || !pModule->HasNativeImage())
        return;

    MethodIterator mi(pModule);

    while (mi.Next())
    {
        MethodDesc* hotDesc = (MethodDesc*)mi.GetMethodDesc();
        ETW::MethodLog::SendMethodEvent(hotDesc, dwEventOptions, FALSE);
    }
#endif // FEATURE_PREJIT
}

/****************************************************************************/
/* This routine sends back method events of type 'dwEventOptions', for all
   JITed methods in either a given LoaderAllocator (if pLoaderAllocatorFilter is non NULL)
   or in a given Domain (if pDomainFilter is non NULL) or for
   all methods (if both filters are null) */
   /****************************************************************************/
void ETW::MethodLog::SendEventsForJitMethods(BaseDomain* pDomainFilter, LoaderAllocator* pLoaderAllocatorFilter, DWORD dwEventOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

#if !defined(FEATURE_PAL) && !defined(DACCESS_COMPILE)

    // This is only called for JITted methods loading xor unloading
    BOOL fLoadOrDCStart = ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoadOrDCStartAny) != 0);
    BOOL fUnloadOrDCEnd = ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnloadOrDCEndAny) != 0);
    _ASSERTE((fLoadOrDCStart || fUnloadOrDCEnd) && !(fLoadOrDCStart&& fUnloadOrDCEnd));

    BOOL fSendMethodEvent =
        (dwEventOptions &
            (ETW::EnumerationLog::EnumerationStructs::JitMethodLoad |
                ETW::EnumerationLog::EnumerationStructs::JitMethodDCStart |
                ETW::EnumerationLog::EnumerationStructs::JitMethodUnload |
                ETW::EnumerationLog::EnumerationStructs::JitMethodDCEnd)) != 0;

    BOOL fSendILToNativeMapEvent =
        (dwEventOptions &
            (ETW::EnumerationLog::EnumerationStructs::MethodDCStartILToNativeMap |
                ETW::EnumerationLog::EnumerationStructs::MethodDCEndILToNativeMap)) != 0;

    BOOL fCollectibleLoaderAllocatorFilter =
        ((pLoaderAllocatorFilter != NULL) && (pLoaderAllocatorFilter->IsCollectible()));
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
    if (fSendILToNativeMapEvent)
    {
        // The call to SendMethodILToNativeMapEvent assumes that the debugger's lazy
        // data has already been initialized, to ensure we don't try to do the lazy init
        // while under the implicit, notrigger CodeHeapIterator lock below.

        // g_pDebugInterface is initialized on startup on desktop CLR, regardless of whether a debugger
        // or profiler is loaded.  So it should always be available.
        _ASSERTE(g_pDebugInterface != NULL);
        g_pDebugInterface->InitializeLazyDataIfNecessary();
    }
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT

    EEJitManager::CodeHeapIterator heapIterator(pDomainFilter, pLoaderAllocatorFilter);
    while (heapIterator.Next())
    {
        MethodDesc* pMD = heapIterator.GetMethod();
        if (pMD == NULL)
            continue;

        TADDR codeStart = heapIterator.GetMethodCode();

        // Grab rejitID from the rejit manager. Short-circuit the call if we're filtering
        // by a collectible loader allocator, since rejit is not supported on RefEmit
        // assemblies.  This also allows us to avoid having to pre-enter the rejit
        // manager locks (which we have to do when filtering by domain; see
        // code:#TableLockHolder).
        ReJITID rejitID =
            fCollectibleLoaderAllocatorFilter ?
            0 :
            pMD->GetReJitManager()->GetReJitIdNoLock(pMD, codeStart);

        // There are small windows of time where the heap iterator may come across a
        // codeStart that is not yet published to the MethodDesc. This may happen if
        // we're JITting the method right now on another thread, and have not completed
        // yet. Detect the race, and skip the method if appropriate. (If rejitID is
        // nonzero, there is no race, as GetReJitIdNoLock will not return a nonzero
        // rejitID if the codeStart has not yet been published for that rejitted version
        // of the method.) This check also catches recompilations due to EnC, which we do
        // not want to issue events for, in order to ensure xperf's assumption that
        // MethodDesc* + ReJITID + extent (hot vs. cold) form a unique key for code
        // ranges of methods
        if ((rejitID == 0) && (codeStart != PCODEToPINSTR(pMD->GetNativeCode())))
            continue;

        // When we're called to announce loads, then the methodload event itself must
        // precede any supplemental events, so that the method load or method jitting
        // event is the first event the profiler sees for that MethodID (and not, say,
        // the MethodILToNativeMap event.)
        if (fLoadOrDCStart)
        {
            if (fSendMethodEvent)
            {
                ETW::MethodLog::SendMethodEvent(
                    pMD,
                    dwEventOptions,
                    TRUE,           // bIsJit
                    NULL,           // namespaceOrClassName
                    NULL,           // methodName
                    NULL,           // methodSignature
                    codeStart,
                    rejitID);
            }
        }

        // Send any supplemental events requested for this MethodID
#ifndef WINXP_AND_WIN2K3_BUILD_SUPPORT
        if (fSendILToNativeMapEvent)
            ETW::MethodLog::SendMethodILToNativeMapEvent(pMD, dwEventOptions, rejitID);
#endif // WINXP_AND_WIN2K3_BUILD_SUPPORT

        // When we're called to announce unloads, then the methodunload event itself must
        // come after any supplemental events, so that the method unload event is the
        // last event the profiler sees for this MethodID
        if (fUnloadOrDCEnd)
        {
            if (fSendMethodEvent)
            {
                ETW::MethodLog::SendMethodEvent(
                    pMD,
                    dwEventOptions,
                    TRUE,           // bIsJit
                    NULL,           // namespaceOrClassName
                    NULL,           // methodName
                    NULL,           // methodSignature
                    codeStart,
                    rejitID);
            }
        }
    }
#endif // !FEATURE_PAL && !DACCESS_COMPILE
}

//---------------------------------------------------------------------------------------
//
// Wrapper around IterateDomain, which locks the AppDomain to be <
// STAGE_FINALIZED until the iteration is complete.
//
// Arguments:
//      pAppDomain - AppDomain to iterate
//      enumerationOptions - Flags indicating what to enumerate.  Just passed
//         straight through to IterateDomain
//
void ETW::EnumerationLog::IterateAppDomain(AppDomain* pAppDomain, DWORD enumerationOptions)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pAppDomain != NULL);
    }
    CONTRACTL_END;

    // Hold the system domain lock during the entire iteration, so we can
    // ensure the App Domain does not get finalized until we're all done
    SystemDomain::LockHolder lh;

    if (pAppDomain->IsFinalized())
    {
        return;
    }

    // Since we're not FINALIZED yet, the handle table should remain intact,
    // as should all type information in this AppDomain
    _ASSERTE(!pAppDomain->NoAccessToHandleTable());

    // Now it's safe to do the iteration
    IterateDomain(pAppDomain, enumerationOptions);

    // Since we're holding the system domain lock, the AD type info should be
    // there throughout the entire iteration we just did
    _ASSERTE(!pAppDomain->NoAccessToHandleTable());
}

/********************************************************************************/
/* This routine fires ETW events for
   Domain,
   Assemblies in them,
   DomainModule's in them,
   Modules in them,
   JIT methods in them,
   and the NGEN methods in them
   based on enumerationOptions.*/
   /********************************************************************************/
void ETW::EnumerationLog::IterateDomain(BaseDomain* pDomain, DWORD enumerationOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pDomain != NULL);
    } CONTRACTL_END;

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    // Do not call IterateDomain() directly with an AppDomain.  Use
    // IterateAppDomain(), which wraps this function with a hold on the
    // SystemDomain lock, which ensures pDomain's type data doesn't disappear
    // on us.
    if (pDomain->IsAppDomain())
    {
        _ASSERTE(SystemDomain::IsUnderDomainLock());
    }
#endif // defined(_DEBUG) && !defined(DACCESS_COMPILE)

    EX_TRY
    {
        // DC Start events for Domain
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
        {
            ETW::LoaderLog::SendDomainEvent(pDomain, enumerationOptions);
        }

    // DC End or Unload Jit Method events
    if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnloadOrDCEndAny)
    {
        ETW::MethodLog::SendEventsForJitMethods(pDomain, NULL, enumerationOptions);
    }

    if (pDomain->IsAppDomain())
    {
        AppDomain::AssemblyIterator assemblyIterator = pDomain->AsAppDomain()->IterateAssembliesEx(
            (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly*> pDomainAssembly;
        while (assemblyIterator.Next(pDomainAssembly.This()))
        {
            CollectibleAssemblyHolder<Assembly*> pAssembly = pDomainAssembly->GetLoadedAssembly();
            BOOL bIsDomainNeutral = pAssembly->IsDomainNeutral();
            if (bIsDomainNeutral)
                continue;
            if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
            {
                ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
            }

            DomainModuleIterator domainModuleIterator = pDomainAssembly->IterateModules(kModIterIncludeLoaded);
            while (domainModuleIterator.Next())
            {
                Module* pModule = domainModuleIterator.GetModule();
                ETW::EnumerationLog::IterateModule(pModule, enumerationOptions);
            }

            if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
               (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload))
            {
                ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
            }
        }
    }
    else
    {
        SharedDomain::SharedAssemblyIterator sharedDomainIterator;
        while (sharedDomainIterator.Next())
        {
            Assembly* pAssembly = sharedDomainIterator.GetAssembly();
            if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
            {
                ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
            }

            ModuleIterator domainModuleIterator = pAssembly->IterateModules();
            while (domainModuleIterator.Next())
            {
                Module* pModule = domainModuleIterator.GetModule();
                ETW::EnumerationLog::IterateModule(pModule, enumerationOptions);
            }

            if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
                (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload))
            {
                ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
            }
        }
    }

    // DC Start or Load Jit Method events
    if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoadOrDCStartAny)
    {
        ETW::MethodLog::SendEventsForJitMethods(pDomain, NULL, enumerationOptions);
    }

    // DC End or Unload events for Domain
    if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload))
    {
        ETW::LoaderLog::SendDomainEvent(pDomain, enumerationOptions);
    }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}


/********************************************************************************/
/* This routine fires ETW events for
   Assembly in LoaderAllocator,
   DomainModule's in them,
   Modules in them,
   JIT methods in them,
   and the NGEN methods in them
   based on enumerationOptions.*/
   /********************************************************************************/
void ETW::EnumerationLog::IterateCollectibleLoaderAllocator(AssemblyLoaderAllocator* pLoaderAllocator, DWORD enumerationOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pLoaderAllocator != NULL);
    } CONTRACTL_END;

    EX_TRY
    {
        // Unload Jit Method events
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnload)
        {
            ETW::MethodLog::SendEventsForJitMethods(NULL, pLoaderAllocator, enumerationOptions);
        }

        Assembly* pAssembly = pLoaderAllocator->Id()->GetDomainAssembly()->GetAssembly();
        _ASSERTE(!pAssembly->IsDomainNeutral()); // Collectible Assemblies are not domain neutral.

        DomainModuleIterator domainModuleIterator = pLoaderAllocator->Id()->GetDomainAssembly()->IterateModules(kModIterIncludeLoaded);
        while (domainModuleIterator.Next())
        {
            Module* pModule = domainModuleIterator.GetModule();
            ETW::EnumerationLog::IterateModule(pModule, enumerationOptions);
        }

        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
        {
            ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
        }

        // Load Jit Method events
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoad)
        {
            ETW::MethodLog::SendEventsForJitMethods(NULL, pLoaderAllocator, enumerationOptions);
        }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/********************************************************************************/
/* This routine fires ETW events for Assembly and the DomainModule's in them
   based on enumerationOptions.*/
   /********************************************************************************/
void ETW::EnumerationLog::IterateAssembly(Assembly* pAssembly, DWORD enumerationOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pAssembly != NULL);
    } CONTRACTL_END;

    EX_TRY
    {
        // DC Start events for Assembly
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
        {
            ETW::LoaderLog::SendAssemblyEvent(pAssembly, enumerationOptions);
        }

    // DC Start, DCEnd, events for DomainModule
    if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart))
    {
        if (pAssembly->GetDomain()->IsAppDomain())
        {
            DomainModuleIterator dmIterator = pAssembly->FindDomainAssembly(pAssembly->GetDomain()->AsAppDomain())->IterateModules(kModIterIncludeLoaded);
            while (dmIterator.Next())
            {
                ETW::LoaderLog::SendModuleEvent(dmIterator.GetModule(), enumerationOptions, TRUE);
            }
        }
    }

    // DC End or Unload events for Assembly
    if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload))
    {
        ETW::LoaderLog::SendAssemblyEvent(pAssembly, enumerationOptions);
    }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

/********************************************************************************/
/* This routine fires ETW events for Module, their range information and the NGEN methods in them
   based on enumerationOptions.*/
   /********************************************************************************/
void ETW::EnumerationLog::IterateModule(Module* pModule, DWORD enumerationOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pModule != NULL);
    } CONTRACTL_END;

    EX_TRY
    {
        // DC Start events for Module
        if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCStart))
        {
            ETW::LoaderLog::SendModuleEvent(pModule, enumerationOptions);
        }

    // DC Start or Load or DC End or Unload Ngen Method events
    if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodUnload) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd))
    {
        ETW::MethodLog::SendEventsForNgenMethods(pModule, enumerationOptions);
    }

    // DC End or Unload events for Module
    if ((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload) ||
       (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCEnd))
    {
        ETW::LoaderLog::SendModuleEvent(pModule, enumerationOptions);
    }

    // If we're logging types, then update the internal Type hash table to account
    // for the module's unloading
    if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::TypeUnload)
    {
        ETW::TypeSystemLog::OnModuleUnload(pModule);
    }

    // ModuleRangeLoadPrivate events for module range information from attach/detach scenarios
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     CLR_PERFTRACK_PRIVATE_KEYWORD) &&
        (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoadPrivate))
    {
        ETW::LoaderLog::SendModuleEvent(pModule, enumerationOptions);
    }
    } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
}

//---------------------------------------------------------------------------------------
//
// This routine sends back domain, assembly, module and method events based on
// enumerationOptions.
//
// Arguments:
//      * moduleFilter - if non-NULL, events from only moduleFilter module are reported
//      * domainFilter - if non-NULL, events from only domainFilter domain are reported
//      * enumerationOptions - Flags from ETW::EnumerationLog::EnumerationStructs which
//          describe which events should be sent.
//
// Notes:
//     * if all filter args are NULL, events from all domains are reported
//
// #TableLockHolder:
//
// A word about ReJitManager::TableLockHolder... As we enumerate through the functions,
// we may need to grab their ReJITIDs. The ReJitManager grabs its table Crst in order to
// fetch these. However, several other kinds of locks are being taken during this
// enumeration, such as the SystemDomain lock and the EEJitManager::CodeHeapIterator's
// lock. In order to avoid lock-leveling issues, we grab the appropriate ReJitManager
// table locks up front. In particular, we need to grab the SharedDomain's ReJitManager
// table lock as well as the specific AppDomain's ReJitManager table lock for the current
// AppDomain we're iterating. Why the SharedDomain's ReJitManager lock? For any given
// AppDomain we're iterating over, the MethodDescs we find may be managed by that
// AppDomain's ReJitManger OR the SharedDomain's ReJitManager. (This is due to generics
// and whether given instantiations may be shared based on their arguments.) Therefore,
// we proactively take the SharedDomain's ReJitManager's table lock up front, and then
// individually take the appropriate AppDomain's ReJitManager's table lock that
// corresponds to the domain or module we're currently iterating over.
//

// static
void ETW::EnumerationLog::EnumerationHelper(Module* moduleFilter, BaseDomain* domainFilter, DWORD enumerationOptions)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    // See code:#TableLockHolder
    ReJitManager::TableLockHolder lkRejitMgrSharedDomain(SharedDomain::GetDomain()->GetReJitManager());

    if (moduleFilter)
    {
        // See code:#TableLockHolder
        ReJitManager::TableLockHolder lkRejitMgrModule(moduleFilter->GetReJitManager());


        // DC End or Unload Jit Method events from all Domains
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnloadOrDCEndAny)
        {
            ETW::MethodLog::SendEventsForJitMethods(NULL, NULL, enumerationOptions);
        }

        ETW::EnumerationLog::IterateModule(moduleFilter, enumerationOptions);

        // DC Start or Load Jit Method events from all Domains
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoadOrDCStartAny)
        {
            ETW::MethodLog::SendEventsForJitMethods(NULL, NULL, enumerationOptions);
        }
    }
    else
    {
        if (domainFilter)
        {
            // See code:#TableLockHolder
            ReJitManager::TableLockHolder lkRejitMgrAD(domainFilter->GetReJitManager());

            if (domainFilter->IsAppDomain())
            {
                ETW::EnumerationLog::IterateAppDomain(domainFilter->AsAppDomain(), enumerationOptions);
            }
            else
            {
                ETW::EnumerationLog::IterateDomain(domainFilter, enumerationOptions);
            }
        }
        else
        {
            AppDomainIterator appDomainIterator(FALSE);
            while (appDomainIterator.Next())
            {
                AppDomain* pDomain = appDomainIterator.GetDomain();
                if (pDomain != NULL)
                {
                    // See code:#TableLockHolder
                    ReJitManager::TableLockHolder lkRejitMgrAD(pDomain->GetReJitManager());

                    ETW::EnumerationLog::IterateAppDomain(pDomain, enumerationOptions);
                }
            }
            ETW::EnumerationLog::IterateDomain(SharedDomain::GetDomain(), enumerationOptions);
        }
    }
}

#endif // !FEATURE_NATIVEAOT
#endif  // defined(FEATURE_NATIVEAOT) || !defined(FEATURE_PAL) || defined(FEATURE_DTRACE)
