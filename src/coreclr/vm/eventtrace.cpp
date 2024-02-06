// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace.cpp
// Abstract: This module implements Event Tracing support
//

//

//
// ============================================================================

#include "common.h"

#ifdef FEATURE_NATIVEAOT

#include "commontypes.h"
#include "daccess.h"
#include "debugmacrosext.h"
#include "gcrhenv.h"
#define Win32EventWrite PalEtwEventWrite
#define InterlockedExchange64 PalInterlockedExchange64

#else // !FEATURE_NATIVEAOT

#include "eventtrace.h"
#include "winbase.h"
#include "contract.h"
#include "ex.h"
#include "dbginterface.h"
#include "finalizerthread.h"
#include "clrversion.h"
#include "typestring.h"

#define Win32EventWrite EventWrite

#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#include "runtimecallablewrapper.h"
#endif

#endif // FEATURE_NATIVEAOT

#include "eventtracepriv.h"

#ifndef HOST_UNIX
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context = { &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context = { &MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context = { &MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context, MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context = { &MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_Context, MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_EVENTPIPE_Context };
#else
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context = { MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context, &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_LTTNG_Context };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context = { MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context, &MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_LTTNG_Context };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context = { MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context, &MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_LTTNG_Context };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context = { MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_EVENTPIPE_Context, &MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_LTTNG_Context };
#endif // HOST_UNIX

#ifdef FEATURE_NATIVEAOT
volatile LONGLONG ETW::GCLog::s_l64LastClientSequenceNumber = 0;
#else // FEATURE_NATIVEAOT
Volatile<LONGLONG> ETW::GCLog::s_l64LastClientSequenceNumber = 0;
#endif // FEATURE_NATIVEAOT

#ifndef FEATURE_NATIVEAOT

//---------------------------------------------------------------------------------------
// Helper macros to determine which version of the Method events to use
//
// The V2 versions of these events include the NativeCodeId, the V1 versions do not.
// Historically, when we version events, we'd just stop sending the old version and only
// send the new one. However, now that we have xperf in heavy use internally and soon to be
// used externally, we need to be a bit careful. In particular, we'd like to allow
// current xperf to continue working without knowledge of NativeCodeIds, and allow future
// xperf to decode symbols in ReJITted functions. Thus,
//    * During a first-JIT, only issue the existing V1 MethodLoad, etc. events (NOT v0,
//        NOT v2). This event does not include a NativeCodeId, and can thus continue to be
//        parsed by older decoders.
//    * During a rejit, only issue the new V2 events (NOT v0 or v1), which will include a
//        nonzero NativeCodeId. Thus, your unique key for a method extent would be MethodID +
//        NativeCodeId + extent (hot/cold). These events will be ignored by older decoders
//        (including current xperf) because of the version number, but xperf will be
//        updated to decode these in the future.

#define FireEtwMethodLoadVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodLoadVerbose_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID); } \
    else \
        { FireEtwMethodLoadVerbose_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId); } \
}

#define FireEtwMethodLoad_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodLoad_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, clrInstanceID); } \
    else \
        { FireEtwMethodLoad_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulMethodFlags, clrInstanceID, nativeCodeId); } \
}

#define FireEtwMethodUnloadVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodUnloadVerbose_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID); } \
    else \
        { FireEtwMethodUnloadVerbose_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId); } \
}

#define FireEtwMethodUnload_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodUnload_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID); } \
    else \
        { FireEtwMethodUnload_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, nativeCodeId); } \
}

#define FireEtwMethodDCStartVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodDCStartVerbose_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID); } \
    else \
        { FireEtwMethodDCStartVerbose_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId); } \
}

#define FireEtwMethodDCStart_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodDCStart_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID); } \
    else \
        { FireEtwMethodDCStart_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, nativeCodeId); } \
}

#define FireEtwMethodDCEndVerbose_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodDCEndVerbose_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID);  } \
    else \
        { FireEtwMethodDCEndVerbose_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, szDtraceOutput1, szDtraceOutput2, szDtraceOutput3, clrInstanceID, nativeCodeId); } \
}

#define FireEtwMethodDCEnd_V1_or_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, nativeCodeId) \
{   \
    if (nativeCodeId == 0)   \
        { FireEtwMethodDCEnd_V1(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID);  } \
    else \
        { FireEtwMethodDCEnd_V2(ullMethodIdentifier, ullModuleID, ullMethodStartAddress, ulMethodSize, ulMethodToken, ulColdMethodFlags, clrInstanceID, nativeCodeId); } \
}

// Module load / unload events:

#define FireEtwModuleLoad_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleLoad_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)
#define FireEtwModuleUnload_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleUnload_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)
#define FireEtwModuleDCStart_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleDCStart_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)
#define FireEtwModuleDCEnd_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath) \
    FireEtwModuleDCEnd_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, clrInstanceId, ManagedPdbSignature, ManagedPdbAge, ManagedPdbPath, NativePdbSignature, NativePdbAge, NativePdbPath)



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
// to consume both <4.0 and 4.0 events would need to enable the expensive NGEN events to
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
            MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_NGEN_KEYWORD)
        && ! ( ETW_TRACING_CATEGORY_ENABLED(
                MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                TRACE_LEVEL_INFORMATION,
                CLR_OVERRIDEANDSUPPRESSNGENEVENTS_KEYWORD) )
    );
}

// Same as above, but for the rundown provider
BOOL IsRundownNgenKeywordEnabledAndNotSuppressed()
{
    LIMITED_METHOD_CONTRACT;

    return
#ifdef FEATURE_PERFTRACING
        EventPipeHelper::Enabled() ||
#endif // FEATURE_PERFTRACING
    (
        ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_RUNDOWNNGEN_KEYWORD)
        && ! ( ETW_TRACING_CATEGORY_ENABLED(
                MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                TRACE_LEVEL_INFORMATION,
                CLR_RUNDOWNOVERRIDEANDSUPPRESSNGENEVENTS_KEYWORD) )
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
        CallStackFrame *frame=NULL;
        __asm
        {
            mov frame, ebp
        }
        return frame;
    }
}
#endif //TARGET_X86

/*************************************/
/* Function to append a frame to an existing stack */
/*************************************/
#if  !defined(HOST_UNIX)
void ETW::SamplingLog::Append(SIZE_T currentFrame)
{
    LIMITED_METHOD_CONTRACT;
    if(m_FrameCount < (ETW::SamplingLog::s_MaxStackSize-1) &&
       currentFrame != 0)
    {
        m_EBPStack[m_FrameCount] = currentFrame;
        m_FrameCount++;
    }
};

/********************************************************/
/* Function to get the callstack on the current thread  */
/********************************************************/
ETW::SamplingLog::EtwStackWalkStatus ETW::SamplingLog::GetCurrentThreadsCallStack(UINT32 *frameCount, PVOID **Stack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
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
    for(int i=m_FrameCount; i<ETW::SamplingLog::s_MaxStackSize; i++)
    {
        m_EBPStack[i] = 0;
    }
    // This is for consumers to work correctly because the number of
    // frames in the manifest file is specified to be 2
    if(m_FrameCount < 2)
        m_FrameCount = 2;

    *frameCount = m_FrameCount;
    *Stack = (PVOID *)m_EBPStack;
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
#ifdef TARGET_AMD64
    if (RtlVirtualUnwind_Unsafe == NULL)
    {
        // We haven't even set up the RtlVirtualUnwind function pointer yet,
        // so it's too early to try stack walking.
        return ETW::SamplingLog::UnInitialized;
    }
#endif // TARGET_AMD64
    Thread *pThread = GetThreadNULLOk();
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
        CallStackFrame *currentEBP = GetEbp();
        CallStackFrame *lastEBP = NULL;

        // The EBP stack walk below is meant to be extremely fast. It does not attempt to protect
        // against cases of stack corruption. *BUT* it does need to validate a "sane" EBP chain.

        // Ensure the EBP in the starting frame is "reasonable" (i.e. above the address of a local)
        if ((SIZE_T) currentEBP > (SIZE_T)&currentEBP)
        {
            while(currentEBP)
            {
                lastEBP = currentEBP;
                currentEBP = currentEBP->m_Next;

                // Check for stack upper limit; we don't check the lower limit on each iteration
                // (we did it at the top) and each subsequent value in the loop is larger than
                // the previous (see the check "currentEBP < lastEBP" below)
                if((SIZE_T)currentEBP > (SIZE_T)Thread::GetStackUpperBound())
                {
                    break;
                }

                // If we have a too small address, we are probably bad
                if((SIZE_T)currentEBP < (SIZE_T)0x10000)
                    break;

                if((SIZE_T)currentEBP < (SIZE_T)lastEBP)
                {
                    break;
                }

                // Skip the top N frames
                if(skipTopNFrames) {
                    skipTopNFrames--;
                    continue;
                }

                // Save the Return Address for symbol decoding
                Append(lastEBP->m_ReturnAddress);
            }
        }
#else
        CONTEXT ctx;
        ClrCaptureContext(&ctx);
        UINT_PTR ControlPc = 0;
        UINT_PTR CurrentSP = 0, PrevSP = 0;

        while(1)
        {
            // Unwind to the caller
            ControlPc = Thread::VirtualUnwindCallFrame(&ctx);

            // This is to take care of recursion
            CurrentSP = (UINT_PTR)GetSP(&ctx);

            // when to break from this loop
            if ( ControlPc == 0 || ( PrevSP == CurrentSP ) )
            {
                break;
            }

            // Skip the top N frames
            if ( skipTopNFrames ) {
                skipTopNFrames--;
                continue;
            }

            // Add the stack frame to the list
            Append(ControlPc);

            PrevSP = CurrentSP;
        }
#endif //TARGET_X86
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
    pThread->MarkEtwStackWalkCompleted();
#endif //!DACCESS_COMPILE

    return ETW::SamplingLog::Completed;
}

#endif // !defined(HOST_UNIX)
#endif // !FEATURE_NATIVEAOT

/****************************************************************************/
/* Methods that are called from the runtime */
/****************************************************************************/

/****************************************************************************/
/* Methods for rundown events                                               */
/****************************************************************************/

/***************************************************************************/
/* This function should be called from the event tracing callback routine
   when the private CLR provider is enabled */
/***************************************************************************/

#ifndef FEATURE_NATIVEAOT

VOID ETW::GCLog::GCSettingsEvent()
{
    if (GCHeapUtilities::IsGCHeapInitialized())
    {
        if (ETW_TRACING_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context,
                                                 GCSettings))
        {
            ETW::GCLog::ETW_GC_INFO Info;

            Info.GCSettings.ServerGC = GCHeapUtilities::IsServerHeap ();
            Info.GCSettings.SegmentSize = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize (false);
            Info.GCSettings.LargeObjectSegmentSize = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize (true);
            FireEtwGCSettings_V1(Info.GCSettings.SegmentSize, Info.GCSettings.LargeObjectSegmentSize, Info.GCSettings.ServerGC, GetClrInstanceId());
        }
        GCHeapUtilities::GetGCHeap()->DiagTraceGCSegments();
    }
};

#endif // !FEATURE_NATIVEAOT



//---------------------------------------------------------------------------------------
//
// Helper to fire the GCStart event.  Figures out which version of GCStart to fire, and
// includes the client sequence number, if available.
//
// Arguments:
//      pGcInfo - ETW_GC_INFO containing details from GC about this collection
//

// static
VOID ETW::GCLog::FireGcStart(ETW_GC_INFO * pGcInfo)
{
    LIMITED_METHOD_CONTRACT;

    if (ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_GC_KEYWORD))
    {
        // If the controller specified a client sequence number for us to log with this
        // GCStart, then retrieve it
        LONGLONG l64ClientSequenceNumberToLog = 0;
        if ((s_l64LastClientSequenceNumber != 0) &&
            (pGcInfo->GCStart.Depth == GCHeapUtilities::GetGCHeap()->GetMaxGeneration()) &&
            (pGcInfo->GCStart.Reason == ETW_GC_INFO::GC_INDUCED))
        {
            l64ClientSequenceNumberToLog = InterlockedExchange64(&s_l64LastClientSequenceNumber, 0);
        }

        FireEtwGCStart_V2(pGcInfo->GCStart.Count, pGcInfo->GCStart.Depth, pGcInfo->GCStart.Reason, pGcInfo->GCStart.Type, GetClrInstanceId(), l64ClientSequenceNumberToLog);
    }
}




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
VOID ETW::GCLog::SendFinalizeObjectEvent(MethodTable * pMT, Object * pObj)
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
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, FinalizeObject))
    {
        FireEtwFinalizeObject(pMT, pObj, GetClrInstanceId());

        // This function checks if type events are enabled; if so, it sends event for
        // finalized object's type (and parameter types, if any)
        ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(
            NULL,       // Not batching this type with others
            (TADDR) pMT,

            // Don't spend the time entering the lock and checking the hash table to see
            // if we've already logged the type; just log it (if type events are enabled).
            ETW::TypeSystemLog::kTypeLogBehaviorAlwaysLog
            );
    }

    // Send private finalize object event, if it's enabled
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, PrvFinalizeObject))
    {
        EX_TRY
        {
            DefineFullyQualifiedNameForClassWOnStack();
            FireEtwPrvFinalizeObject(pMT, pObj, GetClrInstanceId(), GetFullyQualifiedNameForClassNestedAwareW(pMT));
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions);
    }
}


DWORD ETW::ThreadLog::GetEtwThreadFlags(Thread * pThread)
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
        (pThread == FinalizerThread::GetFinalizerThread()))
    {
        dwEtwThreadFlags |= kEtwThreadFlagFinalizer;
    }

    return dwEtwThreadFlags;
}

VOID ETW::ThreadLog::FireThreadCreated(Thread * pThread)
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

VOID ETW::ThreadLog::FireThreadDC(Thread * pThread)
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

#ifndef FEATURE_NATIVEAOT

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

    static key_t GetKey(const element_t &e)
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
        return (count_t) k.AsTAddr();
    }

    static bool IsNull(const element_t &e)
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
    Module * pModule;
    LoggedTypesFromModuleHash loggedTypesFromModuleHash;

    // These are used by the outer hash table (mapping Module*'s to instances of
    // LoggedTypesFromModule).
    static COUNT_T Hash(Module * pModule)
    {
        LIMITED_METHOD_CONTRACT;
        return (COUNT_T) (SIZE_T) pModule;
    }
    Module * GetKey()
    {
        LIMITED_METHOD_CONTRACT;
        return pModule;
    }

    LoggedTypesFromModule(Module * pModuleParam) : loggedTypesFromModuleHash()
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

class AllLoggedTypesTraits : public DefaultSHashTraits<ETW::LoggedTypesFromModule *>
{
public:

    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef DefaultSHashTraits<ETW::LoggedTypesFromModule *> PARENT;
    typedef PARENT::element_t element_t;
    typedef PARENT::count_t count_t;

    typedef Module * key_t;

    static key_t GetKey(const element_t &e)
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
        return (count_t) (size_t) k;
    }

    static bool IsNull(const element_t &e)
    {
        LIMITED_METHOD_CONTRACT;
        return (e == NULL);
    }

    static element_t Null()
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

    // A thread local copy of the global epoch.
    // This value is used by each thread to ensure that the thread local data structures
    // are in sync with the global state.
    unsigned int nEpoch;

    // The outer hash table (mapping Module*'s to instances of LoggedTypesFromModule)
    AllLoggedTypesHash allLoggedTypesHash;
};


CrstStatic AllLoggedTypes::s_cs;
AllLoggedTypes * ETW::TypeSystemLog::s_pAllLoggedTypes = NULL;
unsigned int ETW::TypeSystemLog::s_nEpoch = 0;
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
    s_fHeapAllocLowEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCLOW_KEYWORD);
    s_fHeapAllocHighEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCHIGH_KEYWORD);

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
        DWORD dwCustomObjectAllocationEventsPerTypePerSec = u16_strtoul(
            wszCustomObjectAllocationEventsPerTypePerSec,
            &endPtr,
            10          // Base 10 conversion
            );

        if (dwCustomObjectAllocationEventsPerTypePerSec == UINT32_MAX)
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
    s_fHeapAllocLowEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCLOW_KEYWORD);
    s_fHeapAllocHighEventEnabledNow = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TRACE_LEVEL_INFORMATION, CLR_GCHEAPALLOCHIGH_KEYWORD);

    // FUTURE: Would be nice here to log an error event if (s_fHeapAllocLowEventEnabledNow ||
    // s_fHeapAllocHighEventEnabledNow), but !s_fHeapAllocEventEnabledOnStartup

    // If the type events should be turned off, eliminate the hash tables that tracked
    // which types were logged. (If type events are turned back on later, we'll re-log
    // them all as we encounter them.) Note that all we can really test for is that the
    // Types keyword on the runtime provider is off. Not necessarily that it was on and
    // was just turned off with this request. But either way, TypeSystemLog can handle it
    // because it is extremely smart.
    if (!ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TRACE_LEVEL_INFORMATION, CLR_TYPE_KEYWORD))
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

// static
BOOL ETW::TypeSystemLog::AddOrReplaceTypeLoggingInfo(ETW::LoggedTypesFromModule * pLoggedTypesFromModule, const ETW::TypeLoggingInfo * pTypeLoggingInfo)
{
    LIMITED_METHOD_CONTRACT;

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
    EX_END_CATCH(RethrowTerminalExceptions);

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
void ETW::TypeSystemLog::SendObjectAllocatedEvent(Object * pObject)
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

    // Get stats for type
    TypeLoggingInfo typeLoggingInfo(NULL);
    LoggedTypesFromModule * pLoggedTypesFromModule = NULL;
    BOOL fCreatedNew = FALSE;
    typeLoggingInfo = LookupOrCreateTypeLoggingInfo(th, &fCreatedNew, &pLoggedTypesFromModule);
    if (typeLoggingInfo.th.IsNull())
        return;

    // Update stats with current allocation
    typeLoggingInfo.dwAllocsSkippedForSample++;
    typeLoggingInfo.cbIgnoredSizeForSample += size;

    // If both the high and low verbosity keywords are enabled, log all allocations.
    if (!(s_fHeapAllocHighEventEnabledNow && s_fHeapAllocLowEventEnabledNow))
    {
        // Get the number of threads so that we can scale the per-thread sampling data.
        // NOTE: We don't do this while holding the thread store lock, so this may not be perfect,
        // but it will be close enough.
        LONG numThreads = ThreadStore::s_pThreadStore->ThreadCountInEE();

        // This is our filter. If we should ignore this alloc, then record our updated
        // our stats, and bail without sending the event. Note that we always log objects
        // over 10K in size.
        if (size < 10000 && typeLoggingInfo.dwAllocsSkippedForSample < (typeLoggingInfo.dwAllocsToSkipPerSample * numThreads))
        {
            // Update hash table's copy of type logging info with these values.  It is not optimal that
            // we're doing another hash table lookup here.  Could instead have used LookupPtr()
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
                    kTypeLogBehaviorAlwaysLogTopLevelType);
            }
            return;
        }

        // Based on observed allocation stats, adjust our sampling rate for this type

        typeLoggingInfo.dwAllocCountInCurrentBucket += typeLoggingInfo.dwAllocsSkippedForSample;
        int delta = (dwTickNow - typeLoggingInfo.dwTickOfCurrentTimeBucket) & 0x7FFFFFFF;	// make wrap around work.

        int nMinAllocPerMSec = typeLoggingInfo.dwAllocCountInCurrentBucket / 16 / numThreads;		// This is an underestimation of the true rate.
        if (delta >= 16 || (nMinAllocPerMSec > 2 && nMinAllocPerMSec > typeLoggingInfo.flAllocPerMSec * 1.5F))
        {
            float flNewAllocPerMSec  = 0;
            if (delta >= 16)
            {
                // This is the normal case, our allocation rate is under control with the current throttling.
                flNewAllocPerMSec  = ((float) typeLoggingInfo.dwAllocCountInCurrentBucket) / delta;
                // Do a exponential decay window that is 5 * max(16, AllocationInterval)
                typeLoggingInfo.flAllocPerMSec = 0.8F *  typeLoggingInfo.flAllocPerMSec + 0.2F * flNewAllocPerMSec;
                typeLoggingInfo.dwTickOfCurrentTimeBucket = dwTickNow;
                typeLoggingInfo.dwAllocCountInCurrentBucket = 0;
            }
            else
            {
                flNewAllocPerMSec = (float) nMinAllocPerMSec;
                // This means the second clause above is true, which means our sampling rate is too low
                // so we need to throttle quickly.
                typeLoggingInfo.flAllocPerMSec = flNewAllocPerMSec;
            }


            // Obey the desired sampling rate, but don't ignore > 1000 allocations per second
            // per type
            int nDesiredMsBetweenEvents = (s_nCustomMsBetweenEvents == 0) ? GetDefaultMsBetweenEvents() : s_nCustomMsBetweenEvents;
            typeLoggingInfo.dwAllocsToSkipPerSample = min((int) (typeLoggingInfo.flAllocPerMSec * nDesiredMsBetweenEvents), 1000);
            if (typeLoggingInfo.dwAllocsToSkipPerSample == 1)
                typeLoggingInfo.dwAllocsToSkipPerSample = 0;
        }
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
            kTypeLogBehaviorAlwaysLogTopLevelType);
    }

    // Now log the allocation
    if (s_fHeapAllocHighEventEnabledNow)
    {
        FireEtwGCSampledObjectAllocationHigh(pObject, (LPVOID) th.AsTAddr(), dwObjectCountForTypeSample, nTotalSizeForTypeSample, GetClrInstanceId());
    }
    else
    {
        FireEtwGCSampledObjectAllocationLow(pObject, (LPVOID) th.AsTAddr(), dwObjectCountForTypeSample, nTotalSizeForTypeSample, GetClrInstanceId());
    }
}

//---------------------------------------------------------------------------------------
//
// Accessor for global hash table crst
//
// Return Value:
//      global hash table crst
//

// static
CrstBase * ETW::TypeSystemLog::GetHashCrst()
{
    LIMITED_METHOD_CONTRACT;
    return &AllLoggedTypes::s_cs;
}

// The number of type load operations
// NOTE: This isn't the count of types loaded, as some types may have multiple type loads
//       occur to them as they transition up type loader levels
LONG s_TypeLoadOps = 0;

UINT32 ETW::TypeSystemLog::TypeLoadBegin()
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    UINT32 typeLoad = (UINT32)InterlockedIncrement(&s_TypeLoadOps);

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TypeLoadStart))
    {
        FireEtwTypeLoadStart(
            typeLoad,
            GetClrInstanceId());
    }

    return typeLoad;
}

void ETW::TypeSystemLog::TypeLoadEnd(UINT32 typeLoad, TypeHandle th, UINT16 loadLevel)
{
    CONTRACTL{
        NOTHROW;
        THROWS;
    } CONTRACTL_END;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TypeLoadStop))
    {
        EX_TRY
        {
            StackSString typeName;
            const TypeString::FormatFlags formatFlags = static_cast<TypeString::FormatFlags>(
                TypeString::FormatNamespace |
                TypeString::FormatAngleBrackets);

            TypeString::AppendType(typeName, th, formatFlags);

            SCOUNT_T maxTypeNameLen = (cbMaxEtwEvent / 2) - 0x100;
            if (typeName.GetCount() > (unsigned)maxTypeNameLen)
            {
                typeName.Truncate(typeName.Begin() + maxTypeNameLen);
            }

            FireEtwTypeLoadStop(
                typeLoad,
                GetClrInstanceId(),
                loadLevel,
                (UINT64)th.AsPtr(),
                typeName
                );
        } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
    }
}

//---------------------------------------------------------------------------------------
//
// Outermost level of ETW-type-logging.  Clients outside eventtrace.cpp call this to log
// a TypeHandle and (recursively) its type parameters when present.  This method then calls
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
VOID ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(BulkTypeEventLogger * pLogger, ULONGLONG thAsAddr, TypeLogBehavior typeLogBehavior)
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
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        return;
    }

    TypeHandle th = TypeHandle::FromTAddr((TADDR) thAsAddr);

    // Check to see if we've already logged this type.  If so, bail immediately.
    // Otherwise, mark that it's getting logged (by adding it to the hash), and fall
    // through to the logging code below.  If caller doesn't care, then don't even
    // check; just log the type
    BOOL fShouldLogType = ((typeLogBehavior == kTypeLogBehaviorAlwaysLog) ||
                           (typeLogBehavior == kTypeLogBehaviorAlwaysLogTopLevelType)) ?
                           TRUE :
                               ShouldLogType(th);
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


    // Check to see if TypeLoggingInfo exists yet for th.  If not, creates one and
    // adds it to the hash.
    BOOL fCreatedNew = FALSE;

    // When we have a thread context, default to calling the API that requires one which
    // reduces the cost of locking.
    if (GetThreadNULLOk() != NULL)
    {
        LookupOrCreateTypeLoggingInfo(th, &fCreatedNew);
    }
    else
    {
        AddTypeToGlobalCacheIfNotExists(th, &fCreatedNew);
    }

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
//

// static
ETW::TypeLoggingInfo ETW::TypeSystemLog::LookupOrCreateTypeLoggingInfo(TypeHandle th, BOOL * pfCreatedNew, LoggedTypesFromModule ** ppLoggedTypesFromModule /* = NULL */)
{
    //LIMITED_METHOD_CONTRACT;
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pfCreatedNew != NULL);

    if (ppLoggedTypesFromModule != NULL)
    {
        *ppLoggedTypesFromModule = NULL;
    }

    BOOL fSucceeded = FALSE;

    Thread *pThread = GetThread();

    // Compare the thread local epoch value against the global epoch.
    // If the epoch has changed, dump the thread local state and start over.
    AllLoggedTypes * pThreadAllLoggedTypes = pThread->GetAllocationSamplingTable();
    if((pThreadAllLoggedTypes != NULL) && (pThreadAllLoggedTypes->nEpoch != s_nEpoch))
    {
        // Set the type hash pointer on the thread to NULL.
        pThread->SetAllocationSamplingTable(NULL);

        // DeleteTypeHashNoLock will set pThreadAllLoggedTypes to NULL
        DeleteTypeHashNoLock(&pThreadAllLoggedTypes);
    }

    // Create the thread local state if it doesn't exist.
    if (pThreadAllLoggedTypes == NULL)
    {
        pThreadAllLoggedTypes = new (nothrow) AllLoggedTypes;
        if (pThreadAllLoggedTypes == NULL)
        {
            // out of memory.  Bail on ETW stuff
            *pfCreatedNew = FALSE;
            return TypeLoggingInfo(NULL);
        }

        // Set the epoch so we know we can track when changes to global state occur.
        pThreadAllLoggedTypes->nEpoch = s_nEpoch;

        // Save the thread local state to the thread.
        pThread->SetAllocationSamplingTable(pThreadAllLoggedTypes);
    }

    BOOL addTypeToGlobalList = FALSE;

    // Step 1: go from LoaderModule to hash of types.

    Module * pLoaderModule = th.GetLoaderModule();
    _ASSERTE(pLoaderModule != NULL);
    LoggedTypesFromModule * pLoggedTypesFromModule = pThreadAllLoggedTypes->allLoggedTypesHash.Lookup(pLoaderModule);
    if (pLoggedTypesFromModule == NULL)
    {
        addTypeToGlobalList = TRUE;
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
            pThreadAllLoggedTypes->allLoggedTypesHash.Add(pLoggedTypesFromModule);
            fSucceeded = TRUE;
        }
        EX_CATCH
        {
            fSucceeded = FALSE;
        }
        EX_END_CATCH(RethrowTerminalExceptions);
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
    addTypeToGlobalList = TRUE;
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
    EX_END_CATCH(RethrowTerminalExceptions);
    if (!fSucceeded)
    {
        *pfCreatedNew = FALSE;
        return TypeLoggingInfo(NULL);
    }

    // This is the first time that we've seen this type on this thread, so we should attempt to
    // add it to the global list.
    if(!AddTypeToGlobalCacheIfNotExists(th, pfCreatedNew))
    {
        // out of memory or ETW has been disabled. Bail on ETW stuff
        *pfCreatedNew = FALSE;
        return TypeLoggingInfo(NULL);
    }

    return typeLoggingInfoNew;
}

//---------------------------------------------------------------------------------------
//
// Helper that creates a Type entry in the global type logging cache if one doesn't
// already exist.
//
// Arguments:
//      * th - Key to lookup or create
//
// Return Value:
//      TRUE if the type needed to be added to the cache.
//
//

// static
BOOL ETW::TypeSystemLog::AddTypeToGlobalCacheIfNotExists(TypeHandle th, BOOL * pfCreatedNew)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL fSucceeded = FALSE;

   {
        CrstHolder _crst(GetHashCrst());

        // Check if ETW is enabled, and if not, bail here.
        // We do this inside of the lock to ensure that we don't immediately
        // re-allocate the global type hash after it has been cleaned up.
        if (!ETW_TRACING_CATEGORY_ENABLED(
           MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_TYPE_KEYWORD))
        {
            *pfCreatedNew = FALSE;
            return fSucceeded;
        }

        if (s_pAllLoggedTypes == NULL)
        {
            s_pAllLoggedTypes = new (nothrow) AllLoggedTypes;
            if (s_pAllLoggedTypes == NULL)
            {
                // out of memory.  Bail on ETW stuff
                *pfCreatedNew = FALSE;
                return fSucceeded;
            }
        }
    }

    // Step 1: go from LoaderModule to hash of types.
    Module * pLoaderModule = th.GetLoaderModule();
    _ASSERTE(pLoaderModule != NULL);
    LoggedTypesFromModule * pLoggedTypesFromModule = nullptr;
    {
        CrstHolder _crst(GetHashCrst());
        pLoggedTypesFromModule = s_pAllLoggedTypes->allLoggedTypesHash.Lookup(pLoaderModule);
    }

    if (pLoggedTypesFromModule == NULL)
    {
        pLoggedTypesFromModule = new (nothrow) LoggedTypesFromModule(pLoaderModule);
        if (pLoggedTypesFromModule == NULL)
        {
            // out of memory.  Bail on ETW stuff
            *pfCreatedNew = FALSE;
            return fSucceeded;
        }
        {
            CrstHolder _crst(GetHashCrst());
            // recheck if the type has been added by another thread since we last checked above
            LoggedTypesFromModule * recheckLoggedTypesFromModule = s_pAllLoggedTypes->allLoggedTypesHash.Lookup(pLoaderModule);
            if (recheckLoggedTypesFromModule == NULL)
            {
                EX_TRY
                {
                    s_pAllLoggedTypes->allLoggedTypesHash.Add(pLoggedTypesFromModule);
                    fSucceeded = TRUE;
                }
                EX_CATCH
                {
                    fSucceeded = FALSE;
                }
                EX_END_CATCH(RethrowTerminalExceptions);
            }
            else
            {
                delete pLoggedTypesFromModule;
                pLoggedTypesFromModule = recheckLoggedTypesFromModule;
            }

            if (!fSucceeded)
            {
                *pfCreatedNew = FALSE;
                return fSucceeded;
            }
        }
    }

    // Step 2: From hash of types, see if our TypeHandle is there already
    TypeLoggingInfo typeLoggingInfoPreexisting;
    {
        CrstHolder _crst(GetHashCrst());
        typeLoggingInfoPreexisting = pLoggedTypesFromModule->loggedTypesFromModuleHash.Lookup(th);
        if (!typeLoggingInfoPreexisting.th.IsNull())
        {
            // Type is already hashed, so it's already logged, so we don't need to
            // log it again.
            *pfCreatedNew = FALSE;
            return fSucceeded;
        }
    }

    // We haven't logged this type, so we need to continue with this function to
    // log it below. Add it to the hash table first so any recursive calls will
    // see that this type is already being taken care of
    fSucceeded = FALSE;
    TypeLoggingInfo typeLoggingInfoNew(th);
    {
        CrstHolder _crst(GetHashCrst());
        // Like above, check if the type has been added from a different thread since we last looked it up.
        if (!pLoggedTypesFromModule->loggedTypesFromModuleHash.Lookup(th).th.IsNull())
        {
            *pfCreatedNew = FALSE;
            return fSucceeded;
        }

        EX_TRY
        {
            pLoggedTypesFromModule->loggedTypesFromModuleHash.Add(typeLoggingInfoNew);
            fSucceeded = TRUE;
        }
        EX_CATCH
        {
            fSucceeded = FALSE;
        }
        EX_END_CATCH(RethrowTerminalExceptions);
        if (!fSucceeded)
        {
            *pfCreatedNew = FALSE;
            return fSucceeded;
        }
    } // RELEASE: CrstHolder _crst(GetHashCrst());

    *pfCreatedNew = TRUE;
    return fSucceeded;
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
VOID ETW::TypeSystemLog::OnModuleUnload(Module * pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // We don't need to do anything if allocation sampling is disabled.
    if (!ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        return;
    }

    LoggedTypesFromModule * pLoggedTypesFromModule = NULL;

    {
        CrstHolder _crst(GetHashCrst());

        // We don't need to do anything if the global type hash doesn't contain any data.
        if (s_pAllLoggedTypes == NULL)
            return;

        // Is there a TypesHash for this module?
        pLoggedTypesFromModule = s_pAllLoggedTypes->allLoggedTypesHash.Lookup(pModule);
        if (pLoggedTypesFromModule == NULL)
            return;

        // Remove TypesHash from master hash mapping modules to their TypesHash
        s_pAllLoggedTypes->allLoggedTypesHash.Remove(pModule);

        // Increment the epoch to signal the change to all threads.
        s_nEpoch++;
    }

    // Destruct this TypesHash we just removed
    delete pLoggedTypesFromModule;
    pLoggedTypesFromModule = NULL;

}

//---------------------------------------------------------------------------------------
//
// Same semantics as DeleteTypeHash but assumes that the appropriate lock
// has already been acquired.
//

// static
VOID ETW::TypeSystemLog::DeleteTypeHashNoLock(AllLoggedTypes **ppAllLoggedTypes)
{
    LIMITED_METHOD_CONTRACT;

    if(ppAllLoggedTypes == NULL)
    {
        return;
    }

    AllLoggedTypes *pAllLoggedTypes = *ppAllLoggedTypes;

    if(pAllLoggedTypes == NULL)
    {
        return;
    }

    // Destruct each of the per-module TypesHashes
    AllLoggedTypesHash * pLoggedTypesHash = &pAllLoggedTypes->allLoggedTypesHash;
    for (AllLoggedTypesHash::Iterator iter = pLoggedTypesHash->Begin();
        iter != pLoggedTypesHash->End();
        ++iter)
    {
        LoggedTypesFromModule * pLoggedTypesFromModule = *iter;
        delete pLoggedTypesFromModule;
    }

    // This causes the default ~AllLoggedTypes() to be called, and thus
    // ~AllLoggedTypesHash() to be called
    delete pAllLoggedTypes;
    *ppAllLoggedTypes = NULL;
}

//---------------------------------------------------------------------------------------
//
// Called from shutdown to give us the opportunity to dump any sampled object allocation
// information before the process shuts down.
//

// static
VOID ETW::TypeSystemLog::FlushObjectAllocationEvents()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // If logging is not enabled, then we don't need to do any work.
    if (!(s_fHeapAllocLowEventEnabledNow || s_fHeapAllocHighEventEnabledNow))
    {
        return;
    }

    AllLoggedTypes * pThreadAllLoggedTypes = NULL;
    Thread * pThread = NULL;

    // Get the thread store lock.
    ThreadStoreLockHolder tsl;

    // Iterate over each thread and log any un-logged allocations.
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        pThreadAllLoggedTypes = pThread->GetAllocationSamplingTable();
        if (pThreadAllLoggedTypes == NULL)
        {
            continue;
        }

        DWORD dwAllocsSkippedForSample;
        SIZE_T cbIgnoredSizeForSample;

        // Iterate over each module.
        AllLoggedTypesHash * pLoggedTypesHash = &pThreadAllLoggedTypes->allLoggedTypesHash;
        for (AllLoggedTypesHash::Iterator iter = pLoggedTypesHash->Begin();
            iter != pLoggedTypesHash->End();
            ++iter)
        {
            // Iterate over each type in the module.
            LoggedTypesFromModule * pLoggedTypesFromModule = *iter;
            LoggedTypesFromModuleHash * pLoggedTypesFromModuleHash = &pLoggedTypesFromModule->loggedTypesFromModuleHash;
            for (LoggedTypesFromModuleHash::Iterator typeIter = pLoggedTypesFromModuleHash->Begin();
                typeIter != pLoggedTypesFromModuleHash->End();
                ++typeIter)
            {
                dwAllocsSkippedForSample = typeIter->dwAllocsSkippedForSample;
                cbIgnoredSizeForSample = typeIter->cbIgnoredSizeForSample;

                // Only write the event if there were allocations that have not been logged.
                if (dwAllocsSkippedForSample > 0 || cbIgnoredSizeForSample > 0)
                {
                    // Write the event based on which keyword was specified when ETW was configured.
                    if (s_fHeapAllocHighEventEnabledNow)
                    {
                        FireEtwGCSampledObjectAllocationHigh(NULL, (LPVOID) typeIter->th.AsTAddr(), dwAllocsSkippedForSample, cbIgnoredSizeForSample, GetClrInstanceId());
                    }
                    else
                    {
                        FireEtwGCSampledObjectAllocationLow(NULL, (LPVOID) typeIter->th.AsTAddr(), dwAllocsSkippedForSample, cbIgnoredSizeForSample, GetClrInstanceId());
                    }
                }
            }
        }
    }
}

//---------------------------------------------------------------------------------------
//
// Whenever we detect that the Types keyword is off, this gets called. This eliminates the
// global hash tables that tracked which types were logged (if the hash tables had been created
// previously). If type events are turned back on later, we'll re-log them all as we
// encounter them.  Thread local hash tables are destroyed in the Cleanup method, which is
// called during GC to ensure that there aren't any races.
//

// static
VOID ETW::TypeSystemLog::OnTypesKeywordTurnedOff()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Take the global cache lock.
    CrstHolder _crst(GetHashCrst());

    // Clean-up the global TypeHash if necessary.
    if (s_pAllLoggedTypes == NULL)
    {
        // Even if we don't increment the epoch, but we get into a situation where
        // some per thread data has been allocated, it will be cleaned up during the
        // next GC because we are guaranteed that s_nEpoch has been incremented at
        // least once (to shutdown allocation sampling).
        return;
    }

    // Destruct the global TypeHash
    DeleteTypeHashNoLock(&s_pAllLoggedTypes);

    // Increment the epoch to signal the change to all threads.
    s_nEpoch++;
}

//---------------------------------------------------------------------------------------
//
// Clean-up thread local type hashes.  This is called from within the GC to ensure that
// there are no races.  All threads are suspended when this is called.
//

// static
VOID ETW::TypeSystemLog::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If allocation sampling is enabled, bail here so that we don't delete
    // any of the thread local state.
    if (ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        return;
    }

    // If logging is disabled but the epoch has not been incremented,
    // we haven't ever turned on allocation sampling, so there is nothing
    // to clean-up.
    if(s_nEpoch <= 0)
    {
        return;
    }

    // Iterate over each thread and destruct the per thread caches
    AllLoggedTypes * pThreadAllLoggedTypes = NULL;
    Thread * pThread = NULL;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        pThreadAllLoggedTypes = pThread->GetAllocationSamplingTable();
        if(pThreadAllLoggedTypes == NULL)
        {
            continue;
        }

        // Destruct each of the thread local TypesHashes
        DeleteTypeHashNoLock(&pThreadAllLoggedTypes);

        // Set the thread type hash pointer to NULL
        pThread->SetAllocationSamplingTable(NULL);
    }
}


/****************************************************************************/
/* Called when ETW is turned ON on an existing process and ModuleRange events are to
     be fired */
/****************************************************************************/
VOID ETW::EnumerationLog::ModuleRangeRundown()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context,
                                         TRACE_LEVEL_INFORMATION,
                                         CLR_PERFTRACK_PRIVATE_KEYWORD))
        {
            ETW::EnumerationLog::EnumerationHelper(NULL, NULL, ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoadPrivate);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}


/****************************************************************************/
// Called when ETW is turned ON or OFF on an existing process, to send
// events that are only sent once per rundown
/****************************************************************************/
VOID ETW::EnumerationLog::SendOneTimeRundownEvents()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    // Fire the runtime information event
    ETW::InfoLog::RuntimeInformation(ETW::InfoLog::InfoStructs::Callback);

    if (ETW::CompilationLog::TieredCompilation::Rundown::IsEnabled() && g_pConfig->TieredCompilation())
    {
        ETW::CompilationLog::TieredCompilation::Rundown::SendSettings();
    }
}


/****************************************************************************/
/* Called when ETW is turned ON on an existing process */
/****************************************************************************/
VOID ETW::EnumerationLog::StartRundown()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        SendOneTimeRundownEvents();

        BOOL bIsPerfTrackRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                                                 TRACE_LEVEL_INFORMATION,
                                                                 CLR_RUNDOWNPERFTRACK_KEYWORD);
        BOOL bIsThreadingRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_RUNDOWNTHREADING_KEYWORD);

        BOOL bIsIlToNativeMapRundownEnabled =
            ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                         TRACE_LEVEL_INFORMATION,
                                         CLR_RUNDOWNJITTEDMETHODILTONATIVEMAP_KEYWORD);

        BOOL bIsRichDebugInfoEnabled =
            bIsIlToNativeMapRundownEnabled &&
            ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, JittedMethodRichDebugInfo);

        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNJIT_KEYWORD)
           ||
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNLOADER_KEYWORD)
           ||
           IsRundownNgenKeywordEnabledAndNotSuppressed()
           ||
           bIsIlToNativeMapRundownEnabled
           ||
           bIsPerfTrackRundownEnabled
           ||
           bIsThreadingRundownEnabled
           ||
           bIsRichDebugInfoEnabled)
        {
            // begin marker event will go to the rundown provider
            FireEtwDCStartInit_V1(GetClrInstanceId());

            // The rundown flag is expected to be checked in the caller, so no need to check here again
            DWORD enumerationOptions=ETW::EnumerationLog::EnumerationStructs::None;
            if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNLOADER_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart;
            }
            if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNJIT_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JitMethodDCStart;
            }
            if(IsRundownNgenKeywordEnabledAndNotSuppressed())
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart;
            }
            if(bIsIlToNativeMapRundownEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::MethodDCStartILToNativeMap;
            }
            if(bIsPerfTrackRundownEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCStart;
            }

            if (bIsRichDebugInfoEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JittedMethodRichDebugInfo;
            }

            ETW::EnumerationLog::EnumerationHelper(NULL, NULL, enumerationOptions);

            if (bIsThreadingRundownEnabled)
            {
                SendThreadRundownEvent();
            }

            // end marker event will go to the rundown provider
            FireEtwDCStartComplete_V1(GetClrInstanceId());
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
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

    DWORD enumerationOptions=ETW::EnumerationLog::EnumerationStructs::None;
    if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_LOADER_KEYWORD))
    {
        enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload;
    }
    if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_JIT_KEYWORD) &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_ENDENUMERATION_KEYWORD))
    {
        enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JitMethodUnload;
    }
    if(IsRuntimeNgenKeywordEnabledAndNotSuppressed() &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
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
VOID ETW::EnumerationLog::EnumerateForCaptureState()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TRACE_LEVEL_INFORMATION, KEYWORDZERO))
        {
            DWORD enumerationOptions = GetEnumerationOptionsFromRuntimeKeywords();

            // Send unload events for all remaining domains, including shared domain and
            // default domain.
            ETW::EnumerationLog::EnumerationHelper(NULL /* module filter */, NULL /* domain filter */, enumerationOptions);

            // Send thread created events for all currently active threads, if requested
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                                                 TRACE_LEVEL_INFORMATION,
                                                                 CLR_THREADING_KEYWORD))
            {
                SendThreadRundownEvent();
            }
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/**************************************************************************************/
/* Called when ETW is turned OFF on an existing process .Will be used by the controller for end rundown*/
/**************************************************************************************/
VOID ETW::EnumerationLog::EndRundown()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        SendOneTimeRundownEvents();

        BOOL bIsPerfTrackRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                                                 TRACE_LEVEL_INFORMATION,
                                                                 CLR_RUNDOWNPERFTRACK_KEYWORD);
        BOOL bIsThreadingRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_RUNDOWNTHREADING_KEYWORD);

        BOOL bIsGCRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_RUNDOWNGC_KEYWORD);

        BOOL bIsIlToNativeMapsRundownEnabled = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                                                            TRACE_LEVEL_INFORMATION,
                                                                            CLR_RUNDOWNJITTEDMETHODILTONATIVEMAP_KEYWORD);
        BOOL bIsRichDebugInfoEnabled =
            ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, JittedMethodRichDebugInfo);

        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNJIT_KEYWORD)
           ||
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_RUNDOWNLOADER_KEYWORD)
           ||
           IsRundownNgenKeywordEnabledAndNotSuppressed()
           ||
           bIsIlToNativeMapsRundownEnabled
           ||
           bIsPerfTrackRundownEnabled
           ||
           bIsThreadingRundownEnabled
           ||
           bIsGCRundownEnabled
           ||
           bIsRichDebugInfoEnabled
        )
        {
            // begin marker event will go to the rundown provider
            FireEtwDCEndInit_V1(GetClrInstanceId());

            // The rundown flag is expected to be checked in the caller, so no need to check here again
            DWORD enumerationOptions=ETW::EnumerationLog::EnumerationStructs::None;
            if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNLOADER_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd;
            }
            if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_RUNDOWNJIT_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JitMethodDCEnd;
            }
            if(IsRundownNgenKeywordEnabledAndNotSuppressed())
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd;
            }
            if(bIsIlToNativeMapsRundownEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::MethodDCEndILToNativeMap;
            }
            if(bIsPerfTrackRundownEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCEnd;
            }
            if (bIsRichDebugInfoEnabled)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::JittedMethodRichDebugInfo;
            }

            ETW::EnumerationLog::EnumerationHelper(NULL, NULL, enumerationOptions);

            if (bIsThreadingRundownEnabled)
            {
                SendThreadRundownEvent();
            }

            if (bIsGCRundownEnabled)
            {
                SendGCRundownEvent();
            }

            // end marker event will go to the rundown provider
            FireEtwDCEndComplete_V1(GetClrInstanceId());
        }
    } EX_CATCH {
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

#if !defined(HOST_UNIX)
    // Register CLR providers with the OS
    if (g_pEtwTracer == NULL)
    {
        NewHolder <ETW::CEtwTracer> tempEtwTracer (new (nothrow) ETW::CEtwTracer());
        if (tempEtwTracer != NULL && tempEtwTracer->Register () == ERROR_SUCCESS)
            g_pEtwTracer = tempEtwTracer.Extract ();
    }
#endif

    g_nClrInstanceId = GetRuntimeId() & 0x0000FFFF; // This will give us duplicate ClrInstanceId after UINT16_MAX

    // Any classes that need some initialization to happen after we've registered the
    // providers can do so now
    ETW::TypeSystemLog::PostRegistrationInit();

#if defined(HOST_UNIX) && defined (FEATURE_PERFTRACING)
    XplatEventLogger::InitializeLogger();
#endif // HOST_UNIX && FEATURE_PERFTRACING
}

// Plumbing to funnel event pipe callbacks and ETW callbacks together into a single common
// handler, for the purposes of informing the GC of changes to the event state.
//
// There is one callback for every EventPipe provider and one for all of ETW. The reason
// for this is that ETW passes the registration handle of the provider that was enabled
// as a field on the "CallbackContext" field of the callback, while EventPipe passes null
// unless another token is given to it when the provider is constructed. In the absence of
// a suitable token, this implementation has a different callback for every EventPipe provider
// that ultimately funnels them all into a common handler.

#if defined(HOST_UNIX)
// CLR_MANAGEDHEAPCOLLECT_KEYWORD is defined by the generated ETW manifest on Windows.
// On non-Windows, we need to make sure that this is defined.  Given that we can't change
// the value due to compatibility, we specify it here rather than generating defines based on the manifest.
#define CLR_MANAGEDHEAPCOLLECT_KEYWORD 0x800000
#endif // defined(HOST_UNIX)

// CallbackProviderIndex provides a quick identification of which provider triggered the
// ETW callback.
enum CallbackProviderIndex
{
    DotNETRuntime = 0,
    DotNETRuntimeRundown = 1,
    DotNETRuntimeStress = 2,
    DotNETRuntimePrivate = 3
};

// Common handler for all ETW or EventPipe event notifications. Based on the provider that
// was enabled/disabled, this implementation forwards the event state change onto GCHeapUtilities
// which will inform the GC to update its local state about what events are enabled.
VOID EtwCallbackCommon(
    CallbackProviderIndex ProviderIndex,
    ULONG ControlCode,
    UCHAR Level,
    ULONGLONG MatchAnyKeyword,
    PVOID pFilterData,
    BOOL isEventPipeCallback)
{
    LIMITED_METHOD_CONTRACT;

    bool bIsPublicTraceHandle = ProviderIndex == DotNETRuntime;
#if !defined(HOST_UNIX)
    static_assert(GCEventLevel_Fatal == TRACE_LEVEL_FATAL, "GCEventLevel_Fatal value mismatch");
    static_assert(GCEventLevel_Error == TRACE_LEVEL_ERROR, "GCEventLevel_Error value mismatch");
    static_assert(GCEventLevel_Warning == TRACE_LEVEL_WARNING, "GCEventLevel_Warning mismatch");
    static_assert(GCEventLevel_Information == TRACE_LEVEL_INFORMATION, "GCEventLevel_Information mismatch");
    static_assert(GCEventLevel_Verbose == TRACE_LEVEL_VERBOSE, "GCEventLevel_Verbose mismatch");
#endif // !defined(HOST_UNIX)

    DOTNET_TRACE_CONTEXT * ctxToUpdate;
    switch(ProviderIndex)
    {
    case DotNETRuntime:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
        break;
    case DotNETRuntimeRundown:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context;
        break;
    case DotNETRuntimePrivate:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context;
        break;
    case DotNETRuntimeStress:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context;
        break;
    default:
        _ASSERTE(!"EtwCallbackCommon was called with invalid context");
        return;
    }

    // This callback gets called on both ETW/EventPipe session enable/disable.
    // We need toupdate the EventPipe provider context if we are in a callback
    // from EventPipe, but not from ETW.
    if (isEventPipeCallback)
    {
        ctxToUpdate->EventPipeProvider.Level = Level;
        ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask = MatchAnyKeyword;
        ctxToUpdate->EventPipeProvider.IsEnabled = ControlCode;

        // For EventPipe, ControlCode can only be either 0 or 1.
        _ASSERTE(ControlCode == 0 || ControlCode == 1);
    }

    if (
#if !defined(HOST_UNIX)
        (ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER || ControlCode == EVENT_CONTROL_CODE_DISABLE_PROVIDER) &&
#endif
        (ProviderIndex == DotNETRuntime || ProviderIndex == DotNETRuntimePrivate))
    {
#if !defined(HOST_UNIX)
        // On Windows, consolidate level and keywords across event pipe and ETW contexts -
        // ETW may still want to see events that event pipe doesn't care about and vice versa
        GCEventKeyword keywords = static_cast<GCEventKeyword>(ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask |
                                                              ctxToUpdate->EtwProvider->MatchAnyKeyword);
        GCEventLevel level = static_cast<GCEventLevel>(max(ctxToUpdate->EventPipeProvider.Level,
                                                           ctxToUpdate->EtwProvider->Level));
#else
        GCEventKeyword keywords = static_cast<GCEventKeyword>(ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask);
        GCEventLevel level = static_cast<GCEventLevel>(ctxToUpdate->EventPipeProvider.Level);
#endif
        GCHeapUtilities::RecordEventStateChange(bIsPublicTraceHandle, keywords, level);
    }

    // Special check for the runtime provider's ManagedHeapCollectKeyword.  Profilers
    // flick this to force a full GC.
    if (g_fEEStarted && !g_fEEShutDown && bIsPublicTraceHandle &&
        ((MatchAnyKeyword & CLR_MANAGEDHEAPCOLLECT_KEYWORD) != 0))
    {
        // Profilers may (optionally) specify extra data in the filter parameter
        // to log with the GCStart event.
        LONGLONG l64ClientSequenceNumber = 0;
#if !defined(HOST_UNIX)
        PEVENT_FILTER_DESCRIPTOR FilterData = (PEVENT_FILTER_DESCRIPTOR)pFilterData;
        if ((FilterData != NULL) &&
           (FilterData->Type == 1) &&
           (FilterData->Size == sizeof(l64ClientSequenceNumber)))
        {
            l64ClientSequenceNumber = *(LONGLONG *) (FilterData->Ptr);
        }
#endif // !defined(HOST_UNIX)
        ETW::GCLog::ForceGC(l64ClientSequenceNumber);
    }
    // TypeSystemLog needs a notification when certain keywords are modified, so
    // give it a hook here.
    if (g_fEEStarted && !g_fEEShutDown && bIsPublicTraceHandle)
    {
        ETW::TypeSystemLog::OnKeywordsChanged();
    }

    if (g_fEEStarted && !g_fEEShutDown)
    {
        // Emit the YieldProcessor measured values at the beginning of the trace
        YieldProcessorNormalization::FireMeasurementEvents();
    }
}

// Individual callbacks for each EventPipe provider.

VOID EventPipeEtwCallbackDotNETRuntimeStress(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    LIMITED_METHOD_CONTRACT;

    EtwCallbackCommon(DotNETRuntimeStress, ControlCode, Level, MatchAnyKeyword, FilterData, true);
}

VOID EventPipeEtwCallbackDotNETRuntime(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    LIMITED_METHOD_CONTRACT;

    EtwCallbackCommon(DotNETRuntime, ControlCode, Level, MatchAnyKeyword, FilterData, true);
}

VOID EventPipeEtwCallbackDotNETRuntimeRundown(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    LIMITED_METHOD_CONTRACT;

    EtwCallbackCommon(DotNETRuntimeRundown, ControlCode, Level, MatchAnyKeyword, FilterData, true);
}

VOID EventPipeEtwCallbackDotNETRuntimePrivate(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    WRAPPER_NO_CONTRACT;

    EtwCallbackCommon(DotNETRuntimePrivate, ControlCode, Level, MatchAnyKeyword, FilterData, true);
}


#if !defined(HOST_UNIX)
HRESULT ETW::CEtwTracer::Register()
{
    WRAPPER_NO_CONTRACT;

    EventRegisterMicrosoft_Windows_DotNETRuntime();
    EventRegisterMicrosoft_Windows_DotNETRuntimePrivate();
    EventRegisterMicrosoft_Windows_DotNETRuntimeRundown();

    // Stress Log ETW events are available only on the desktop version of the runtime

    MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeHandle;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimePrivateHandle;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeRundownHandle;

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
    return S_OK;
}

extern "C"
{
    ETW_INLINE
    VOID EtwCallout(REGHANDLE RegHandle,
                    PCEVENT_DESCRIPTOR Descriptor,
                    ULONG ArgumentCount,
                    PEVENT_DATA_DESCRIPTOR EventData)
    {
        WRAPPER_NO_CONTRACT;
        UINT8 providerIndex = 0;
        if(RegHandle == Microsoft_Windows_DotNETRuntimeHandle) {
            providerIndex = 0;
        } else if(RegHandle == Microsoft_Windows_DotNETRuntimeRundownHandle) {
            providerIndex = 1;
        } else if(RegHandle == Microsoft_Windows_DotNETRuntimeStressHandle) {
            providerIndex = 2;
        } else if(RegHandle == Microsoft_Windows_DotNETRuntimePrivateHandle) {
            providerIndex = 3;
        } else {
            _ASSERTE(!"Provider not one of Runtime, Rundown, Private and Stress");
            return;
        }

        // stacks are supposed to be fired for only the events with a bit set in the etwStackSupportedEvents bitmap
        if(((etwStackSupportedEvents[providerIndex][Descriptor->Id/8]) &
            (1<<(Descriptor->Id%8))) != 0)
        {
            if(RegHandle == Microsoft_Windows_DotNETRuntimeHandle) {
                ETW::SamplingLog::SendStackTrace(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, &CLRStackWalk, &CLRStackId);
            } else if(RegHandle == Microsoft_Windows_DotNETRuntimeRundownHandle) {
                ETW::SamplingLog::SendStackTrace(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context, &CLRStackWalkDCStart, &CLRStackRundownId);
            } else if(RegHandle == Microsoft_Windows_DotNETRuntimePrivateHandle) {
                ETW::SamplingLog::SendStackTrace(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, &CLRStackWalkPrivate, &CLRStackPrivateId);
            } else if(RegHandle == Microsoft_Windows_DotNETRuntimeStressHandle) {
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
    VOID EtwCallback(
        _In_ LPCGUID SourceId,
        _In_ ULONG ControlCode,
        _In_ UCHAR Level,
        _In_ ULONGLONG MatchAnyKeyword,
        _In_ ULONGLONG MatchAllKeyword,
        _In_opt_ PEVENT_FILTER_DESCRIPTOR FilterData,
        _Inout_opt_ PVOID CallbackContext)
    {
        CONTRACTL {
            NOTHROW;
            if(g_fEEStarted) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);};
            MODE_ANY;
            CAN_TAKE_LOCK;
            STATIC_CONTRACT_FAULT;
        } CONTRACTL_END;

        // Mark that we are the special ETWRundown thread.  Currently all this does
        // is insure that AVs thrown in this thread are treated as normal exceptions.
        // This allows us to catch and swallow them.   We can do this because we have
        // a reasonably strong belief that doing ETW Rundown does not change runtime state
        // and thus if an AV happens it is better to simply give up logging ETW and
        // instead of terminating the process (which is what we would do normally)
        ClrFlsThreadTypeSwitch etwRundownThreadHolder(ThreadType_ETWRundownThread);
        PMCGEN_TRACE_CONTEXT context = (PMCGEN_TRACE_CONTEXT)CallbackContext;

        BOOLEAN bIsPublicTraceHandle = (context->RegistrationHandle==Microsoft_Windows_DotNETRuntimeHandle);

        BOOLEAN bIsPrivateTraceHandle = (context->RegistrationHandle==Microsoft_Windows_DotNETRuntimePrivateHandle);

        BOOLEAN bIsRundownTraceHandle = (context->RegistrationHandle==Microsoft_Windows_DotNETRuntimeRundownHandle);

        // EventPipeEtwCallback contains some GC eventing functionality shared between EventPipe and ETW.
        // Eventually, we'll want to merge these two codepaths whenever we can.
        CallbackProviderIndex providerIndex = DotNETRuntime;
        DOTNET_TRACE_CONTEXT providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
        if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeHandle) {
            providerIndex = DotNETRuntime;
            providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
        } else if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeRundownHandle) {
            providerIndex = DotNETRuntimeRundown;
            providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context;
        } else if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeStressHandle) {
            providerIndex = DotNETRuntimeStress;
            providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context;
        } else if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimePrivateHandle) {
            providerIndex = DotNETRuntimePrivate;
            providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context;
        } else {
            assert(!"unknown registration handle");
            return;
        }

        EtwCallbackCommon(providerIndex, ControlCode, Level, MatchAnyKeyword, FilterData, false);

        // A manifest based provider can be enabled to multiple event tracing sessions
        // As long as there is atleast 1 enabled session, IsEnabled will be TRUE
        // Since classic providers can be enabled to only a single session,
        // IsEnabled will be TRUE when it is enabled and FALSE when disabled
        BOOL bEnabled =
            ((ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER) ||
             (ControlCode == EVENT_CONTROL_CODE_CAPTURE_STATE));
        if(bEnabled)
        {
            if (bIsPrivateTraceHandle)
            {
                ETW::GCLog::GCSettingsEvent();
                if(g_fEEStarted && !g_fEEShutDown)
                {
                    ETW::EnumerationLog::ModuleRangeRundown();
                }
            }

#ifdef TARGET_AMD64
            // We only do this on amd64  (NOT ARM, because ARM uses frame based stack crawling)
            // If we have turned on the JIT keyword to the INFORMATION setting (needed to get JIT names) then
            // we assume that we also want good stack traces so we need to publish unwind information so
            // ETW can get at it
            if(bIsPublicTraceHandle && ETW_CATEGORY_ENABLED(providerContext, TRACE_LEVEL_INFORMATION, CLR_RUNDOWNJIT_KEYWORD))
                UnwindInfoTable::PublishUnwindInfo(g_fEEStarted != FALSE);
#endif

            if(g_fEEStarted && !g_fEEShutDown && bIsRundownTraceHandle)
            {
                // Start and End Method/Module Rundowns
                // Used to fire events that we missed since we started the controller after the process started
                // flags for immediate start rundown
                if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                                TRACE_LEVEL_INFORMATION,
                                                CLR_RUNDOWNSTART_KEYWORD))
                    ETW::EnumerationLog::StartRundown();

                // flags delayed end rundown
                if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
                                                TRACE_LEVEL_INFORMATION,
                                                CLR_RUNDOWNEND_KEYWORD))
                    ETW::EnumerationLog::EndRundown();
            }

            if (g_fEEStarted && !g_fEEShutDown && (ControlCode == EVENT_CONTROL_CODE_CAPTURE_STATE))
            {
                ETW::EnumerationLog::EnumerateForCaptureState();
            }
        }
#ifdef FEATURE_COMINTEROP
        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, CCWRefCountChange))
            g_pConfig->SetLogCCWRefCountChangeEnabled(bEnabled != 0);
#endif // FEATURE_COMINTEROP

    }
}
#endif // FEATURE_NATIVEAOT

#endif // HOST_UNIX
#ifndef FEATURE_NATIVEAOT

/****************************************************************************/
/* This is called by the runtime when an exception is thrown */
/****************************************************************************/
VOID ETW::ExceptionLog::ExceptionThrown(CrawlFrame  *pCf, BOOL bIsReThrownException, BOOL bIsNewException)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(GetThread()->GetThrowable() != NULL);
    } CONTRACTL_END;

    if(!(bIsReThrownException || bIsNewException))
    {
        return;
    }
    if(!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionThrown_V1))
    {
        return;
    }
    EX_TRY
    {
        SString exceptionType(W(""));
        LPWSTR exceptionMessage = NULL;
        BOOL bIsCLSCompliant=FALSE, bIsCSE=FALSE, bIsNestedException=FALSE, bHasInnerException=FALSE;
        UINT16 exceptionFlags=0;
        PVOID exceptionEIP=0;

        Thread *pThread = GetThread();

        struct
        {
            OBJECTREF exceptionObj;
            OBJECTREF innerExceptionObj;
            STRINGREF exceptionMessageRef;
        } gc;
        gc.exceptionObj = NULL;
        gc.innerExceptionObj = NULL;
        gc.exceptionMessageRef = NULL;
        GCPROTECT_BEGIN(gc);

        gc.exceptionObj = pThread->GetThrowable();
        gc.innerExceptionObj = ((EXCEPTIONREF)gc.exceptionObj)->GetInnerException();

        ThreadExceptionState *pExState = pThread->GetExceptionState();
#ifndef FEATURE_EH_FUNCLETS
        PTR_ExInfo pExInfo = NULL;
#else
        PTR_ExceptionTrackerBase pExInfo = NULL;
#endif //!FEATURE_EH_FUNCLETS
        pExInfo = pExState->GetCurrentExceptionTracker();
        _ASSERTE(pExInfo != NULL);
        bIsNestedException = (pExInfo->GetPreviousExceptionTracker() != NULL);
        bIsCLSCompliant = IsException((gc.exceptionObj)->GetMethodTable()) &&
                          ((gc.exceptionObj)->GetMethodTable() != CoreLibBinder::GetException(kRuntimeWrappedException));

        // A rethrown exception is also a nested exception
        // but since we have a separate flag for it, lets unset the nested flag
        if(bIsReThrownException)
        {
            bIsNestedException = FALSE;
        }
        bHasInnerException = (gc.innerExceptionObj) != NULL;

        exceptionFlags = ((bHasInnerException ? ETW::ExceptionLog::ExceptionStructs::HasInnerException : 0) |
                          (bIsNestedException ? ETW::ExceptionLog::ExceptionStructs::IsNestedException : 0) |
                          (bIsReThrownException ? ETW::ExceptionLog::ExceptionStructs::IsReThrownException : 0) |
                          (bIsCLSCompliant ? ETW::ExceptionLog::ExceptionStructs::IsCLSCompliant : 0));

        if (pCf->IsFrameless())
        {
#ifndef HOST_64BIT
            exceptionEIP = (PVOID)pCf->GetRegisterSet()->ControlPC;
#else
            exceptionEIP = (PVOID)GetIP(pCf->GetRegisterSet()->pContext);
#endif //!HOST_64BIT
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

        gc.exceptionMessageRef =  ((EXCEPTIONREF)gc.exceptionObj)->GetMessage();
        TypeHandle exceptionTypeHandle = (gc.exceptionObj)->GetTypeHandle();
        exceptionTypeHandle.GetName(exceptionType);
        WCHAR *exceptionTypeName = (WCHAR *)exceptionType.GetUnicode();

        if(gc.exceptionMessageRef != NULL)
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
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}


VOID ETW::ExceptionLog::ExceptionThrownEnd()
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionThrownStop))
    {
        return;
    }

    FireEtwExceptionThrownStop();
}

/****************************************************************************/
/* This is called by the runtime when an exception is handled by the runtime */
/****************************************************************************/
VOID ETW::ExceptionLog::ExceptionCatchBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionCatchStart))
    {
        return;
    }

    EX_TRY
    {
        SString methodName;
        pMethodDesc->GetFullMethodInfo(methodName);

        FireEtwExceptionCatchStart((uint64_t)pEntryEIP,
            (uint64_t)pMethodDesc,
            methodName.GetUnicode(),
            GetClrInstanceId());

    } EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

VOID ETW::ExceptionLog::ExceptionCatchEnd()
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionCatchStop))
    {
        return;
    }

    FireEtwExceptionCatchStop();
}

VOID ETW::ExceptionLog::ExceptionFinallyBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionFinallyStart))
    {
        return;
    }

    EX_TRY
    {
        SString methodName;
        pMethodDesc->GetFullMethodInfo(methodName);

        FireEtwExceptionFinallyStart((uint64_t)pEntryEIP,
            (uint64_t)pMethodDesc,
            methodName.GetUnicode(),
            GetClrInstanceId());

    } EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

VOID ETW::ExceptionLog::ExceptionFinallyEnd()
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionFinallyStop))
    {
        return;
    }

    FireEtwExceptionFinallyStop();
}

VOID ETW::ExceptionLog::ExceptionFilterBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionFilterStart))
    {
        return;
    }

    EX_TRY
    {
        SString methodName;
        pMethodDesc->GetFullMethodInfo(methodName);

        FireEtwExceptionFilterStart((uint64_t)pEntryEIP,
            (uint64_t)pMethodDesc,
            methodName.GetUnicode(),
            GetClrInstanceId());

    } EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

VOID ETW::ExceptionLog::ExceptionFilterEnd()
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ExceptionFilterStop))
    {
        return;
    }

    FireEtwExceptionFilterStop();
}

/****************************************************************************/
/* This is called by the runtime when a domain is loaded */
/****************************************************************************/
VOID ETW::LoaderLog::DomainLoadReal(BaseDomain *pDomain, _In_opt_ LPWSTR wszFriendlyName)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_LOADER_KEYWORD))
        {
            DWORD dwEventOptions = ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad;
            ETW::LoaderLog::SendDomainEvent(pDomain, dwEventOptions, wszFriendlyName);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when an AppDomain is unloaded */
/****************************************************************************/
VOID ETW::LoaderLog::DomainUnload(AppDomain *pDomain)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        KEYWORDZERO))
        {
            DWORD enumerationOptions = ETW::EnumerationLog::GetEnumerationOptionsFromRuntimeKeywords();

            // Domain unload also causes type unload events
            if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_TYPE_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::TypeUnload;
            }

            ETW::EnumerationLog::EnumerationHelper(NULL, pDomain, enumerationOptions);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when a LoaderAllocator is unloaded */
/****************************************************************************/
VOID ETW::LoaderLog::CollectibleLoaderAllocatorUnload(AssemblyLoaderAllocator *pLoaderAllocator)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        KEYWORDZERO))
        {
            DWORD enumerationOptions = ETW::EnumerationLog::GetEnumerationOptionsFromRuntimeKeywords();

            // Collectible Loader Allocator unload also causes type unload events
            if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                            TRACE_LEVEL_INFORMATION,
                                            CLR_TYPE_KEYWORD))
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::TypeUnload;
            }

            ETW::EnumerationLog::IterateCollectibleLoaderAllocator(pLoaderAllocator, enumerationOptions);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when the runtime is loaded
   Function gets called by both the Callback mechanism and regular ETW events.
   Type is used to differentiate whether its a callback or a normal call*/
/****************************************************************************/
VOID ETW::InfoLog::RuntimeInformation(INT32 type)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY {
        if((type == ETW::InfoLog::InfoStructs::Normal && ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, RuntimeInformationStart))
            ||
           (type == ETW::InfoLog::InfoStructs::Callback && ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context, RuntimeInformationDCStart))
          )
        {
            PCWSTR szDtraceOutput1=W(""),szDtraceOutput2=W("");
            UINT8 startupMode = 0;
            UINT startupFlags = CorHost2::GetStartupFlags();
            PathString dllPath;
            UINT8 Sku = ETW::InfoLog::InfoStructs::CoreCLR;

            //version info for coreclr.dll
            USHORT vmMajorVersion = RuntimeFileMajorVersion;
            USHORT vmMinorVersion = RuntimeFileMinorVersion;
            USHORT vmBuildVersion = RuntimeFileBuildVersion;
            USHORT vmRevisionVersion = RuntimeFileRevisionVersion;

            //version info for System.Private.CoreLib.dll
            USHORT bclMajorVersion = RuntimeProductMajorVersion;
            USHORT bclMinorVersion = RuntimeProductMinorVersion;
            USHORT bclBuildVersion = RuntimeProductPatchVersion;
            USHORT bclRevisionVersion = 0;

            LPCGUID comGUID=&IID_NULL;

            PCWSTR lpwszCommandLine = W("");


            // if GetClrModulePathName fails, we return an empty string
            if (!GetClrModulePathName(dllPath)) {
                dllPath.Set(W("\0"));
            }

            if(type == ETW::InfoLog::InfoStructs::Callback)
            {
                FireEtwRuntimeInformationDCStart( GetClrInstanceId(),
                                                  Sku,
                                                  bclMajorVersion,
                                                  bclMinorVersion,
                                                  bclBuildVersion,
                                                  bclRevisionVersion,
                                                  vmMajorVersion,
                                                  vmMinorVersion,
                                                  vmBuildVersion,
                                                  vmRevisionVersion,
                                                  startupFlags,
                                                  startupMode,
                                                  lpwszCommandLine,
                                                  comGUID,
                                                  dllPath );
            }
            else
            {
                FireEtwRuntimeInformationStart( GetClrInstanceId(),
                                                Sku,
                                                bclMajorVersion,
                                                bclMinorVersion,
                                                bclBuildVersion,
                                                bclRevisionVersion,
                                                vmMajorVersion,
                                                vmMinorVersion,
                                                vmBuildVersion,
                                                vmRevisionVersion,
                                                startupFlags,
                                                startupMode,
                                                lpwszCommandLine,
                                                comGUID,
                                                dllPath );
            }
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/* Fires ETW events every time a pdb is dynamically loaded.
*
* The ETW events correspond to sending parts of the pdb in roughly
* 64K sized chunks in order. Additional information sent is as follows:
* ModuleID, TotalChunks, Size of Current Chunk, Chunk Number, CLRInstanceID
*
* Note: The current implementation does not support reflection.emit.
* The method will silently return without firing an event.
*/

VOID ETW::CodeSymbolLog::EmitCodeSymbols(Module* pModule)
{
#if  !defined(HOST_UNIX) //UNIXTODO: Enable EmitCodeSymbols
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;


    EX_TRY {
        if (ETW_TRACING_CATEGORY_ENABLED(
                MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                TRACE_LEVEL_VERBOSE,
                CLR_CODESYMBOLS_KEYWORD))
        {
            if (pModule != NULL)
            {
                UINT16 clrInstanceID = GetClrInstanceId();
                UINT64 moduleID = (ModuleID)pModule;
                DWORD length = 0;
                // We silently exit if pdb is of length 0 instead of sending an event with no pdb bytes
                if (CodeSymbolLog::GetInMemorySymbolsLength(pModule, &length) == S_OK && length > 0)
                {
                    // The maximum data size allowed is 64K - (Size of the Event_Header)
                    // Since the actual size of user data can only be determined at runtime
                    // we simplify the header size value to be 1000 bytes as a conservative
                    // estmate.
                    static const DWORD maxDataSize = 63000;

                    ldiv_t qr = ldiv(length, maxDataSize);

                    // We do not allow pdbs of size greater than 2GB for now,
                    // so totalChunks should fit in 16 bits.
                    if (qr.quot < UINT16_MAX)
                    {
                        // If there are trailing bits in the last chunk, then increment totalChunks by 1
                        UINT16 totalChunks = (UINT16)(qr.quot + ((qr.rem != 0) ? 1 : 0));
                        NewArrayHolder<BYTE> chunk(new BYTE[maxDataSize]);
                        DWORD offset = 0;
                        for (UINT16 chunkNum = 0; offset < length; chunkNum++)
                        {
                            DWORD lengthRead = 0;
                            // We expect ReadInMemorySymbols to always return maxDataSize sized chunks
                            // Or it is the last chunk and it is less than maxDataSize.
                            CodeSymbolLog::ReadInMemorySymbols(pModule, offset, chunk, maxDataSize, &lengthRead);

                            _ASSERTE(lengthRead == maxDataSize || // Either we are in the first to (n-1)th chunk
                                (lengthRead < maxDataSize && chunkNum + 1 == totalChunks)); // Or we are in the last chunk

                            FireEtwCodeSymbols(moduleID, totalChunks, chunkNum, lengthRead, chunk, clrInstanceID);
                            offset += lengthRead;
                        }
                    }
                }
            }
        }
    } EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
#endif//  !defined(HOST_UNIX)
}

/* Returns the length of an in-memory symbol stream
*
* If the module has in-memory symbols the length of the stream will
* be placed in pCountSymbolBytes. If the module doesn't have in-memory
* symbols, *pCountSymbolBytes = 0
*
* Returns S_OK if the length could be determined (even if it is 0)
*
* Note: The current implementation does not support reflection.emit.
* CORPROF_E_MODULE_IS_DYNAMIC will be returned in that case.
*
* //IMPORTANT NOTE: The desktop code outside the Project K branch
* contains copies of this function in the clr\src\vm\proftoeeinterfaceimpl.cpp
* file of the desktop version corresponding to the profiler version
* of this feature. Anytime that feature/code is ported to Project K
* the code below should be appropriately merged so as to avoid
* duplication.
*/

HRESULT ETW::CodeSymbolLog::GetInMemorySymbolsLength(
    Module* pModule,
    DWORD* pCountSymbolBytes)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    if (pCountSymbolBytes == NULL)
    {
        return E_INVALIDARG;
    }
    *pCountSymbolBytes = 0;

    if (pModule == NULL)
    {
        return E_INVALIDARG;
    }
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //This method would work fine on reflection.emit, but there would be no way to know
    //if some other thread was changing the size of the symbols before this method returned.
    //Adding events or locks to detect/prevent changes would make the scenario workable
    if (pModule->IsReflection())
    {
        return COR_PRF_MODULE_DYNAMIC;
    }

    CGrowableStream* pStream = pModule->GetInMemorySymbolStream();
    if (pStream == NULL)
    {
        return S_OK;
    }

    STATSTG SizeData = { 0 };
    hr = pStream->Stat(&SizeData, STATFLAG_NONAME);
    if (FAILED(hr))
    {
        return hr;
    }
    if (SizeData.cbSize.u.HighPart > 0)
    {
        return COR_E_OVERFLOW;
    }
    *pCountSymbolBytes = SizeData.cbSize.u.LowPart;

    return S_OK;
}

/* Reads bytes from an in-memory symbol stream
*
* This function attempts to read countSymbolBytes of data starting at offset
* symbolsReadOffset within the in-memory stream. The data will be copied into
* pSymbolBytes which is expected to have countSymbolBytes of space available.
* pCountSymbolsBytesRead contains the actual number of bytes read which
* may be less than countSymbolBytes if the end of the stream is reached.
*
* Returns S_OK if a non-zero number of bytes were read.
*
* Note: The current implementation does not support reflection.emit.
* CORPROF_E_MODULE_IS_DYNAMIC will be returned in that case.
*
* //IMPORTANT NOTE: The desktop code outside the Project K branch
* contains copies of this function in the clr\src\vm\proftoeeinterfaceimpl.cpp
* file of the desktop version corresponding to the profiler version
* of this feature. Anytime that feature/code is ported to Project K
* the code below should be appropriately merged so as to avoid
* duplication.

*/

HRESULT ETW::CodeSymbolLog::ReadInMemorySymbols(
    Module* pModule,
    DWORD symbolsReadOffset,
    BYTE* pSymbolBytes,
    DWORD countSymbolBytes,
    DWORD* pCountSymbolBytesRead)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    if (pSymbolBytes == NULL)
    {
        return E_INVALIDARG;
    }
    if (pCountSymbolBytesRead == NULL)
    {
        return E_INVALIDARG;
    }
    *pCountSymbolBytesRead = 0;

    if (pModule == NULL)
    {
        return E_INVALIDARG;
    }
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //This method would work fine on reflection.emit, but there would be no way to know
    //if some other thread was changing the size of the symbols before this method returned.
    //Adding events or locks to detect/prevent changes would make the scenario workable
    if (pModule->IsReflection())
    {
        return COR_PRF_MODULE_DYNAMIC;
    }

    CGrowableStream* pStream = pModule->GetInMemorySymbolStream();
    if (pStream == NULL)
    {
        return E_INVALIDARG;
    }

    STATSTG SizeData = { 0 };
    hr = pStream->Stat(&SizeData, STATFLAG_NONAME);
    if (FAILED(hr))
    {
        return hr;
    }
    if (SizeData.cbSize.u.HighPart > 0)
    {
        return COR_E_OVERFLOW;
    }
    DWORD streamSize = SizeData.cbSize.u.LowPart;
    if (symbolsReadOffset >= streamSize)
    {
        return E_INVALIDARG;
    }

    *pCountSymbolBytesRead = min(streamSize - symbolsReadOffset, countSymbolBytes);
    memcpy_s(pSymbolBytes, countSymbolBytes, ((BYTE*)pStream->GetRawBuffer().StartAddress()) + symbolsReadOffset, *pCountSymbolBytesRead);

    return S_OK;
}

VOID ETW::MethodLog::GetR2RGetEntryPoint(MethodDesc *pMethodDesc, PCODE pEntryPoint)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, R2RGetEntryPoint))
    {
        EX_TRY
        {
                SendMethodDetailsEvent(pMethodDesc);

                SString tNamespace, tMethodName, tMethodSignature;
                pMethodDesc->GetMethodInfo(tNamespace, tMethodName, tMethodSignature);

                FireEtwR2RGetEntryPoint(
                    (UINT64)pMethodDesc,
                    (PCWSTR)tNamespace.GetUnicode(),
                    (PCWSTR)tMethodName.GetUnicode(),
                    (PCWSTR)tMethodSignature.GetUnicode(),
                    pEntryPoint,
                    GetClrInstanceId());

        } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
    }
}

VOID ETW::MethodLog::GetR2RGetEntryPointStart(MethodDesc *pMethodDesc)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, R2RGetEntryPointStart))
    {
        FireEtwR2RGetEntryPointStart(
            (UINT64)pMethodDesc,
            GetClrInstanceId());
    }
}

VOID ETW::MethodLog::LogMethodInstrumentationData(MethodDesc* method, uint32_t cbData, BYTE *data, TypeHandle* pTypeHandles, uint32_t numTypeHandles, MethodDesc** pMethods, uint32_t numMethods)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;
    const uint32_t chunkSize = 40000;
    const uint32_t maxDataSize = chunkSize * 0x1000;
    const uint32_t FinalChunkFlag = 0x80000000;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, JitInstrumentationDataVerbose))
    {
        EX_TRY
        {
            SendMethodDetailsEvent(method);

            // If there are any type handles, fire the BulkType events to describe them
            if (numTypeHandles != 0)
            {
                BulkTypeEventLogger typeLogger;

                for (uint32_t iTypeHandle = 0; iTypeHandle < numTypeHandles; iTypeHandle++)
                {
                    ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(&typeLogger, (ULONGLONG)pTypeHandles[iTypeHandle].AsPtr(), ETW::TypeSystemLog::kTypeLogBehaviorAlwaysLog);
                }
                typeLogger.FireBulkTypeEvent();
            }

            for (uint32_t iMethod = 0; iMethod < numMethods; iMethod++)
            {
                ETW::MethodLog::SendMethodDetailsEvent(pMethods[iMethod]);
            }

            ULONG ulMethodToken=0;
            auto pModule = method->GetModule();
            bool bIsDynamicMethod = method->IsDynamicMethod();
            BOOL bIsGenericMethod = FALSE;
            if(method->GetMethodTable())
                bIsGenericMethod = method->HasClassOrMethodInstantiation();

            // Use MethodDesc if Dynamic or Generic methods
            if( bIsDynamicMethod || bIsGenericMethod)
            {
                if(bIsGenericMethod)
                    ulMethodToken = (ULONG)method->GetMemberDef();
                if(bIsDynamicMethod) // if its a generic and a dynamic method, we would set the methodtoken to 0
                    ulMethodToken = (ULONG)0;
            }
            else
                ulMethodToken = (ULONG)method->GetMemberDef();

            SString tNamespace, tMethodName, tMethodSignature;
            method->GetMethodInfo(tNamespace, tMethodName, tMethodSignature);

            PCWSTR pNamespace = (PCWSTR)tNamespace.GetUnicode();
            PCWSTR pMethodName = (PCWSTR)tMethodName.GetUnicode();
            PCWSTR pMethodSignature = (PCWSTR)tMethodSignature.GetUnicode();

            // Send data in 40,000 byte chunks
            uint32_t chunkIndex = 0;
            for (; cbData > 0; chunkIndex++)
            {
                bool finalChunk = cbData <= chunkSize;
                uint32_t chunkSizeToEmit = finalChunk ? cbData : chunkSize;

                FireEtwJitInstrumentationDataVerbose(
                    GetClrInstanceId(),
                    chunkIndex | (finalChunk ? FinalChunkFlag : 0),
                    chunkSizeToEmit,
                    (ULONGLONG)(TADDR) method,
                    (ULONGLONG)(TADDR) pModule,
                    ulMethodToken,
                    pNamespace,
                    pMethodName,
                    pMethodSignature,
                    (BYTE*)data);
                data += chunkSizeToEmit;
                cbData -= chunkSizeToEmit;
            }
        } EX_CATCH{ } EX_END_CATCH(SwallowAllExceptions);
    }
}

/*******************************************************/
/* This is called by the runtime when a method is jitted completely */
/*******************************************************/
VOID ETW::MethodLog::MethodJitted(MethodDesc *pMethodDesc, SString *namespaceOrClassName, SString *methodName, SString *methodSignature, PCODE pNativeCodeStartAddress, PrepareCodeConfig *pConfig)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JIT_KEYWORD))
        {
            ETW::MethodLog::SendMethodEvent(pMethodDesc, ETW::EnumerationLog::EnumerationStructs::JitMethodLoad, TRUE, namespaceOrClassName, methodName, methodSignature, pNativeCodeStartAddress, pConfig);
        }

        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JITTEDMETHODILTONATIVEMAP_KEYWORD))
        {
            // The call to SendMethodILToNativeMapEvent assumes that the debugger's lazy
            // data has already been initialized.

            // g_pDebugInterface is initialized on startup on desktop CLR, regardless of whether a debugger
            // or profiler is loaded.  So it should always be available.
            _ASSERTE(g_pDebugInterface != NULL);
            g_pDebugInterface->InitializeLazyDataIfNecessary();

            ETW::MethodLog::SendMethodILToNativeMapEvent(pMethodDesc,
                                                         ETW::EnumerationLog::EnumerationStructs::JitMethodILToNativeMap,
                                                         pNativeCodeStartAddress,
                                                         pConfig->GetCodeVersion().GetVersionId(),
                                                         pConfig->GetCodeVersion().GetILCodeVersionId());
        }

        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, JittedMethodRichDebugInfo))
        {
            _ASSERTE(g_pDebugInterface != NULL);
            g_pDebugInterface->InitializeLazyDataIfNecessary();

            ETW::MethodLog::SendMethodRichDebugInfo(pMethodDesc, pNativeCodeStartAddress, pConfig->GetCodeVersion().GetVersionId(), pConfig->GetCodeVersion().GetILCodeVersionId(), NULL);
        }

    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/*************************************************/
/* This is called by the runtime when method jitting started */
/*************************************************/
VOID ETW::MethodLog::MethodJitting(MethodDesc *pMethodDesc, COR_ILMETHOD_DECODER* methodDecoder, SString *namespaceOrClassName, SString *methodName, SString *methodSignature)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(pMethodDesc != NULL);
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_VERBOSE,
                                        CLR_JIT_KEYWORD))
        {
            pMethodDesc->GetMethodInfo(*namespaceOrClassName, *methodName, *methodSignature);
            ETW::MethodLog::SendMethodJitStartEvent(pMethodDesc, methodDecoder, namespaceOrClassName, methodName, methodSignature);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/**********************************************************************/
/* This is called by the runtime when a single jit helper method with stub is initialized */
/**********************************************************************/
VOID ETW::MethodLog::StubInitialized(ULONGLONG ullHelperStartAddress, LPCWSTR pHelperName)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(ullHelperStartAddress != 0);
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JIT_KEYWORD))
        {
            DWORD dwHelperSize=0;
            Stub::RecoverStubAndSize((TADDR)ullHelperStartAddress, &dwHelperSize);
            ETW::MethodLog::SendHelperEvent(ullHelperStartAddress, dwHelperSize, pHelperName);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/**********************************************************/
/* This is called by the runtime when helpers with stubs are initialized */
/**********************************************************/
VOID ETW::MethodLog::StubsInitialized(PVOID *pHelperStartAddress, PVOID *pHelperNames, LONG lNoOfHelpers)
{
    WRAPPER_NO_CONTRACT;

    if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                    TRACE_LEVEL_INFORMATION,
                                    CLR_JIT_KEYWORD))
    {
        for(int i=0; i<lNoOfHelpers; i++)
        {
            if(pHelperStartAddress[i])
            {
                StubInitialized((ULONGLONG)pHelperStartAddress[i], (LPCWSTR)pHelperNames[i]);
            }
        }
    }
}

/****************************************************************************/
/* This is called by the runtime when a dynamic method is destroyed */
/****************************************************************************/
VOID ETW::MethodLog::DynamicMethodDestroyed(MethodDesc *pMethodDesc)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_JIT_KEYWORD))
            ETW::MethodLog::SendMethodEvent(pMethodDesc, ETW::EnumerationLog::EnumerationStructs::JitMethodUnload, TRUE);
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when a ngen method is restored */
/****************************************************************************/
VOID ETW::MethodLog::MethodRestored(MethodDesc *pMethodDesc)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if(IsRuntimeNgenKeywordEnabledAndNotSuppressed()
           &&
           ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_STARTENUMERATION_KEYWORD))
        {
            ETW::MethodLog::SendMethodEvent(pMethodDesc, ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad, FALSE);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when a method table is restored */
/****************************************************************************/
VOID ETW::MethodLog::MethodTableRestored(MethodTable *pMethodTable)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;
    EX_TRY
    {
        if(IsRuntimeNgenKeywordEnabledAndNotSuppressed()
            &&
            ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                         TRACE_LEVEL_INFORMATION,
                                         CLR_STARTENUMERATION_KEYWORD))
        {
            {
                MethodTable::MethodIterator iter(pMethodTable);
                for (; iter.IsValid(); iter.Next())
                {
                    MethodDesc *pMD = (MethodDesc *)(iter.GetMethodDesc());
                    if(pMD && pMD->GetMethodTable() == pMethodTable)
                        ETW::MethodLog::SendMethodEvent(pMD, ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad, FALSE);
                }
            }
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}


/****************************************************************************/
/* This is called by the runtime when a Strong Name Verification Starts */
/****************************************************************************/
VOID ETW::SecurityLog::StrongNameVerificationStart(DWORD dwInFlags, _In_ LPWSTR strFullyQualifiedAssemblyName)
{
    WRAPPER_NO_CONTRACT;
}


/****************************************************************************/
/* This is called by the runtime when a Strong Name Verification Ends */
/****************************************************************************/
VOID ETW::SecurityLog::StrongNameVerificationStop(DWORD dwInFlags,ULONG result, _In_ LPWSTR strFullyQualifiedAssemblyName)
{
    WRAPPER_NO_CONTRACT;
}

/****************************************************************************/
/* This is called by the runtime when field transparency calculations begin */
/****************************************************************************/
void ETW::SecurityLog::FireFieldTransparencyComputationStart(LPCWSTR wszFieldName,
                                                             LPCWSTR wszModuleName,
                                                             DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
    FireEtwFieldTransparencyComputationStart(wszFieldName, wszModuleName, dwAppDomain, GetClrInstanceId());
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
    FireEtwFieldTransparencyComputationEnd(wszFieldName, wszModuleName, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
}

/*****************************************************************************/
/* This is called by the runtime when method transparency calculations begin */
/*****************************************************************************/
void ETW::SecurityLog::FireMethodTransparencyComputationStart(LPCWSTR wszMethodName,
                                                              LPCWSTR wszModuleName,
                                                              DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
    FireEtwMethodTransparencyComputationStart(wszMethodName, wszModuleName, dwAppDomain, GetClrInstanceId());
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
    FireEtwMethodTransparencyComputationEnd(wszMethodName, wszModuleName, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
}

/*****************************************************************************/
/* This is called by the runtime when module transparency calculations begin */
/*****************************************************************************/
void ETW::SecurityLog::FireModuleTransparencyComputationStart(LPCWSTR wszModuleName,
                                                              DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
    FireEtwModuleTransparencyComputationStart(wszModuleName, dwAppDomain, GetClrInstanceId());
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
    FireEtwModuleTransparencyComputationEnd(wszModuleName, dwAppDomain, fIsAllCritical, fIsAllTransparent, fIsTreatAsSafe, fIsOpportunisticallyCritical, dwSecurityRuleSet, GetClrInstanceId());
}

/****************************************************************************/
/* This is called by the runtime when token transparency calculations begin */
/****************************************************************************/
void ETW::SecurityLog::FireTokenTransparencyComputationStart(DWORD dwToken,
                                                             LPCWSTR wszModuleName,
                                                             DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
    FireEtwTokenTransparencyComputationStart(dwToken, wszModuleName, dwAppDomain, GetClrInstanceId());
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
    FireEtwTokenTransparencyComputationEnd(dwToken, wszModuleName, dwAppDomain, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
}

/*****************************************************************************/
/* This is called by the runtime when type transparency calculations begin   */
/*****************************************************************************/
void ETW::SecurityLog::FireTypeTransparencyComputationStart(LPCWSTR wszTypeName,
                                                            LPCWSTR wszModuleName,
                                                            DWORD dwAppDomain)
{
    WRAPPER_NO_CONTRACT;
    FireEtwTypeTransparencyComputationStart(wszTypeName, wszModuleName, dwAppDomain, GetClrInstanceId());
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
    FireEtwTypeTransparencyComputationEnd(wszTypeName, wszModuleName, dwAppDomain, fIsAllCritical, fIsAllTransparent, fIsCritical, fIsTreatAsSafe, GetClrInstanceId());
}

/**********************************************************************************/
/* This is called by the runtime when a module is loaded */
/* liReportedSharedModule will be 0 when this module is reported for the 1st time */
/**********************************************************************************/
VOID ETW::LoaderLog::ModuleLoad(Module *pModule, LONG liReportedSharedModule)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        DWORD enumerationOptions = ETW::EnumerationLog::EnumerationStructs::None;
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        KEYWORDZERO))
        {
            BOOL bTraceFlagLoaderSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                                                    TRACE_LEVEL_INFORMATION,
                                                                    CLR_LOADER_KEYWORD);
            BOOL bTraceFlagNgenMethodSet = IsRuntimeNgenKeywordEnabledAndNotSuppressed();
            BOOL bTraceFlagStartRundownSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                                                          TRACE_LEVEL_INFORMATION,
                                                                          CLR_STARTENUMERATION_KEYWORD);
            BOOL bTraceFlagPerfTrackSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                                                          TRACE_LEVEL_INFORMATION,
                                                                          CLR_PERFTRACK_KEYWORD);

            if(liReportedSharedModule == 0)
            {

                if(bTraceFlagLoaderSet)
                    enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad;
                if (bTraceFlagPerfTrackSet)
                    enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoad;
                if(bTraceFlagNgenMethodSet && bTraceFlagStartRundownSet)
                    enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad;

                if(pModule->IsManifest() && bTraceFlagLoaderSet)
                    ETW::LoaderLog::SendAssemblyEvent(pModule->GetAssembly(), enumerationOptions);

                if(bTraceFlagLoaderSet || bTraceFlagPerfTrackSet)
                    ETW::LoaderLog::SendModuleEvent(pModule, ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad | ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoad);

                ETW::EnumerationLog::EnumerationHelper(pModule, NULL, enumerationOptions);
            }

            // we want to report domainmodule events whenever they are loaded in any AppDomain
            if(bTraceFlagLoaderSet)
                ETW::LoaderLog::SendModuleEvent(pModule, ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad, TRUE);
        }

        {
            BOOL bTraceFlagPerfTrackPrivateSet = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context,
                                                                                TRACE_LEVEL_INFORMATION,
                                                                                CLR_PERFTRACK_PRIVATE_KEYWORD);
            if (liReportedSharedModule == 0 && bTraceFlagPerfTrackPrivateSet)
            {
                enumerationOptions |= ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoadPrivate;
                ETW::LoaderLog::SendModuleRange(pModule, enumerationOptions);
            }
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/* This is called by the runtime when the process is being shutdown */
/****************************************************************************/
VOID ETW::EnumerationLog::ProcessShutdown()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TRACE_LEVEL_INFORMATION, KEYWORDZERO))
        {
            DWORD enumerationOptions = GetEnumerationOptionsFromRuntimeKeywords();

            // Send unload events for all remaining domains, including shared domain and
            // default domain.
            ETW::EnumerationLog::EnumerationHelper(NULL /* module filter */, NULL /* domain filter */, enumerationOptions);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/****************************************************************************/
/****************************************************************************/
/* beginning of helper functions */
/****************************************************************************/
/****************************************************************************/

/****************************************************************************/
/* This routine is used to send a domain load/unload or rundown event                              */
/****************************************************************************/
VOID ETW::LoaderLog::SendDomainEvent(BaseDomain *pBaseDomain, DWORD dwEventOptions, LPCWSTR wszFriendlyName)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if(!pBaseDomain)
        return;

    PCWSTR szDtraceOutput1=W("");
    BOOL bIsAppDomain = pBaseDomain->IsAppDomain();

    ULONGLONG ullDomainId = (ULONGLONG)pBaseDomain;
    ULONG ulDomainFlags = ETW::LoaderLog::LoaderStructs::DefaultDomain | ETW::LoaderLog::LoaderStructs::ExecutableDomain;

    LPCWSTR wsEmptyString = W("");

    LPWSTR lpswzDomainName = (LPWSTR)wsEmptyString;

    if(wszFriendlyName)
        lpswzDomainName = (PWCHAR)wszFriendlyName;
    else
        lpswzDomainName = (PWCHAR)pBaseDomain->AsAppDomain()->GetFriendlyName();

    /* prepare events args for ETW and ETM */
    szDtraceOutput1 = (PCWSTR)lpswzDomainName;

    if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad)
    {
        FireEtwAppDomainLoad_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, DefaultADID, GetClrInstanceId());
    }
    else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
    {
        FireEtwAppDomainUnload_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, DefaultADID, GetClrInstanceId());
    }
    else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
    {
        FireEtwAppDomainDCStart_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, DefaultADID, GetClrInstanceId());
    }
    else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd)
    {
        FireEtwAppDomainDCEnd_V1(ullDomainId, ulDomainFlags, szDtraceOutput1, DefaultADID, GetClrInstanceId());
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
VOID ETW::EnumerationLog::SendThreadRundownEvent()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

#ifndef DACCESS_COMPILE
    Thread *pThread = NULL;

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

/********************************************************/
/* This routine is used to send GC rundown events */
/********************************************************/
VOID ETW::EnumerationLog::SendGCRundownEvent()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (GCHeapUtilities::IsGCHeapInitialized())
    {
        EtwGCSettingsInfo gcSettingsInfo;
        GCHeapUtilities::GetGCHeap()->DiagGetGCSettings(&gcSettingsInfo);

        DWORD dwEtwGCSettingFlags = 0;
        if (gcSettingsInfo.concurrent_gc_p)
            dwEtwGCSettingFlags |= kEtwGCFlagConcurrent;

        if (gcSettingsInfo.use_large_pages_p)
            dwEtwGCSettingFlags |= kEtwGCFlagLargePages;

        if (gcSettingsInfo.use_frozen_segments_p)
            dwEtwGCSettingFlags |= kEtwGCFlagFrozenSegs;

        if (gcSettingsInfo.hard_limit_config_p)
            dwEtwGCSettingFlags |= kEtwGCFlagHardLimitConfig;

        if (gcSettingsInfo.no_affinitize_p)
            dwEtwGCSettingFlags |= kEtwGCFlagNoAffinitize;

        FireEtwGCSettingsRundown (
            gcSettingsInfo.heap_hard_limit,
            gcSettingsInfo.loh_threshold,
            gcSettingsInfo.physical_memory_from_config,
            gcSettingsInfo.gen0_min_budget_from_config,
            gcSettingsInfo.gen0_max_budget_from_config,
            gcSettingsInfo.high_mem_percent_from_config,
            dwEtwGCSettingFlags,
            GetClrInstanceId());
    }
}

/****************************************************************************/
/* This routine is used to send an assembly load/unload or rundown event ****/
/****************************************************************************/

VOID ETW::LoaderLog::SendAssemblyEvent(Assembly *pAssembly, DWORD dwEventOptions)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if(!pAssembly)
        return;

    PCWSTR szDtraceOutput1=W("");
    BOOL bIsDynamicAssembly = pAssembly->IsDynamic();
    BOOL bIsCollectibleAssembly = pAssembly->IsCollectible();
    BOOL bIsReadyToRun = pAssembly->GetPEAssembly()->IsReadyToRun();

    ULONGLONG ullAssemblyId = (ULONGLONG)pAssembly;
    ULONGLONG ullDomainId = (ULONGLONG)AppDomain::GetCurrentDomain();
    ULONGLONG ullBindingID = 0;
    ULONG ulAssemblyFlags = ((bIsDynamicAssembly ? ETW::LoaderLog::LoaderStructs::DynamicAssembly : 0) |
                             (bIsCollectibleAssembly ? ETW::LoaderLog::LoaderStructs::CollectibleAssembly : 0) |
                             (bIsReadyToRun ? ETW::LoaderLog::LoaderStructs::ReadyToRunAssembly : 0));

    SString sAssemblyPath;
    pAssembly->GetDisplayName(sAssemblyPath);
    LPWSTR lpszAssemblyPath = (LPWSTR)sAssemblyPath.GetUnicode();

/* prepare events args for ETW and ETM */
    szDtraceOutput1 = (PCWSTR)lpszAssemblyPath;

    if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad)
    {
        FireEtwAssemblyLoad_V1(ullAssemblyId, ullDomainId, ullBindingID, ulAssemblyFlags, szDtraceOutput1, GetClrInstanceId());
    }
    else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
    {
        FireEtwAssemblyUnload_V1(ullAssemblyId, ullDomainId, ullBindingID, ulAssemblyFlags, szDtraceOutput1, GetClrInstanceId());
    }
    else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
    {
        FireEtwAssemblyDCStart_V1(ullAssemblyId, ullDomainId, ullBindingID, ulAssemblyFlags, szDtraceOutput1, GetClrInstanceId());
    }
    else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd)
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

ETW_INLINE
    ULONG
    ETW::LoaderLog::SendModuleRange(
    _In_ Module *pModule,
    _In_ DWORD dwEventOptions)

{
    ULONG Result = ERROR_SUCCESS;

    return Result;
}

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

static void GetCodeViewInfo(Module * pModule, CV_INFO_PDB70 * pCvInfoIL, CV_INFO_PDB70 * pCvInfoNative)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE (pModule != NULL);
    _ASSERTE (pCvInfoIL != NULL);
    _ASSERTE (pCvInfoNative != NULL);

    ZeroMemory(pCvInfoIL, sizeof(*pCvInfoIL));
    ZeroMemory(pCvInfoNative, sizeof(*pCvInfoNative));

    PTR_PEAssembly pPEAssembly = pModule->GetPEAssembly();
    _ASSERTE(pPEAssembly != NULL);

    PTR_PEImageLayout pLayout = NULL;
    if (pPEAssembly->HasPEImage())
    {
        pLayout = pPEAssembly->GetLoadedLayout();
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
    IMAGE_DEBUG_DIRECTORY * rgDebugEntries =
        (IMAGE_DEBUG_DIRECTORY *) pLayout->GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_DEBUG, &cbDebugEntries);

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
        CV_INFO_PDB70 *     m_pPdb70;
        ULONG               m_cbPdb70;
    };

    // Iterate through all debug directory entries.  The very last one will be the
    // managed PDB entry.  The next to last one (if it exists) will be the (native) NGEN
    // PDB entry.  Treat raw bytes we read as untrusted.
    PdbInfo pdbInfoLast = {0};
    PdbInfo pdbInfoNextToLast = {0};
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
        CV_INFO_PDB70 * pPdb70 = (CV_INFO_PDB70 *) pLayout->GetRvaData(rvaOfRawData);
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
        // can be truncated to its actual data length (i.e., fewer than MAX_LONGPATH chars
        // may be present in the PE file). In some cases, though, cbDebugData will
        // include all MAX_LONGPATH chars even though path gets null-terminated well before
        // the MAX_LONGPATH limit.

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
        // The last item is the IL (managed) PDB info
        _ASSERTE(pdbInfoLast.m_cbPdb70 <= sizeof(*pCvInfoIL));      // Guaranteed by checks above
        memcpy(pCvInfoIL, pdbInfoLast.m_pPdb70, pdbInfoLast.m_cbPdb70);
    }

    if (pdbInfoNextToLast.m_pPdb70 != NULL)
    {
        // The next-to-last item is the NGEN (native) PDB info
        _ASSERTE(pdbInfoNextToLast.m_cbPdb70 <= sizeof(*pCvInfoNative));      // Guaranteed by checks above
        memcpy(pCvInfoNative, pdbInfoNextToLast.m_pPdb70, pdbInfoNextToLast.m_cbPdb70);
    }
}


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
VOID ETW::LoaderLog::SendModuleEvent(Module *pModule, DWORD dwEventOptions, BOOL bFireDomainModuleEvents)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if(!pModule)
        return;

    PCWSTR szDtraceOutput1=W(""),szDtraceOutput2=W("");
    BOOL bIsDynamicAssembly = pModule->GetAssembly()->IsDynamic();
    BOOL bIsManifestModule = pModule->IsManifest();
    ULONGLONG ullAppDomainId = 0; // This is used only with DomainModule events
    ULONGLONG ullModuleId = (ULONGLONG)(TADDR) pModule;
    ULONGLONG ullAssemblyId = (ULONGLONG)pModule->GetAssembly();
    BOOL bIsIbcOptimized = FALSE;
    BOOL bIsReadyToRun = pModule->IsReadyToRun();
    BOOL bIsPartialReadyToRun = FALSE;
    if (bIsReadyToRun)
    {
        bIsPartialReadyToRun = pModule->GetReadyToRunInfo()->IsPartial();
    }
    ULONG ulReservedFlags = 0;
    ULONG ulFlags = ((bIsDynamicAssembly ? ETW::LoaderLog::LoaderStructs::DynamicModule : 0) |
                     (bIsManifestModule ? ETW::LoaderLog::LoaderStructs::ManifestModule : 0) |
                     (bIsIbcOptimized ? ETW::LoaderLog::LoaderStructs::IbcOptimized : 0) |
                     (bIsReadyToRun ? ETW::LoaderLog::LoaderStructs::ReadyToRunModule : 0) |
                     (bIsPartialReadyToRun ? ETW::LoaderLog::LoaderStructs::PartialReadyToRunModule : 0));

    // Grab PDB path, guid, and age for managed PDB and native (NGEN) PDB when
    // available.  Any failures are not fatal.  The corresponding PDB info will remain
    // zeroed out, and that's what we'll include in the event.
    CV_INFO_PDB70 cvInfoIL = {0};
    CV_INFO_PDB70 cvInfoNative = {0};
    GetCodeViewInfo(pModule, &cvInfoIL, &cvInfoNative);

    PWCHAR ModuleILPath=(PWCHAR)W(""), ModuleNativePath=(PWCHAR)W("");

    if(bFireDomainModuleEvents)
    {
        ullAppDomainId = (ULONGLONG)pModule->GetDomainAssembly()->GetAppDomain();
    }

    LPCWSTR pEmptyString = W("");
    SString moduleName{ SString::Empty() };

    if(!bIsDynamicAssembly)
    {
        ModuleILPath = (PWCHAR)pModule->GetAssembly()->GetPEAssembly()->GetPEImage()->GetPath().GetUnicode();
        ModuleNativePath = (PWCHAR)pEmptyString;
    }

    // if we do not have a module path yet, we put the module name
    if(bIsDynamicAssembly || ModuleILPath==NULL || u16_strlen(ModuleILPath) <= 2)
    {
        moduleName.SetUTF8(pModule->GetSimpleName());
        ModuleILPath = (PWCHAR)moduleName.GetUnicode();
        ModuleNativePath = (PWCHAR)pEmptyString;
    }

    /* prepare events args for ETW and ETM */
    szDtraceOutput1 = (PCWSTR)ModuleILPath;
    szDtraceOutput2 = (PCWSTR)ModuleNativePath;

    // Convert PDB paths to UNICODE
    StackSString managedPdbPath(SString::Utf8, cvInfoIL.path);
    StackSString nativePdbPath(SString::Utf8, cvInfoNative.path);

    if(bFireDomainModuleEvents)
    {
        if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad)
        {
            FireEtwDomainModuleLoad_V1(ullModuleId, ullAssemblyId, ullAppDomainId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId());
        }
        else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
        {
            FireEtwDomainModuleDCStart_V1(ullModuleId, ullAssemblyId, ullAppDomainId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId());
        }
        else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd)
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
        if((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad) || (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoad))
        {
            FireEtwModuleLoad_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId(), &cvInfoIL.signature, cvInfoIL.age, managedPdbPath, &cvInfoNative.signature, cvInfoNative.age, nativePdbPath);
        }
        else if(dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
        {
            FireEtwModuleUnload_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId(), &cvInfoIL.signature, cvInfoIL.age, managedPdbPath, &cvInfoNative.signature, cvInfoNative.age, nativePdbPath);
        }
        else if((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) || (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCStart))
        {
            FireEtwModuleDCStart_V1_or_V2(ullModuleId, ullAssemblyId, ulFlags, ulReservedFlags, szDtraceOutput1, szDtraceOutput2, GetClrInstanceId(), &cvInfoIL.signature, cvInfoIL.age, managedPdbPath, &cvInfoNative.signature, cvInfoNative.age, nativePdbPath);
        }
        else if((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) || (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCEnd))
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

        if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeEnabledAny)
        {
            // Fire ModuleRangeLoad, ModuleRangeDCStart, ModuleRangeDCEnd or ModuleRangeLoadPrivate event for this Module
            SendModuleRange(pModule, dwEventOptions);
        }
    }
}

VOID ETW::MethodLog::SendMethodDetailsEvent(MethodDesc *pMethodDesc)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    EX_TRY
    {
        if(ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_METHODDIAGNOSTIC_KEYWORD))
        {
            if (pMethodDesc->IsDynamicMethod())
                goto done;

            Instantiation inst = pMethodDesc->GetMethodInstantiation();

            if (inst.GetNumArgs() > 1024) // ETW has a limit for maximum event size. Do not log overly large method type argument sets
                goto done;

            BulkTypeEventLogger typeLogger;

            ULONGLONG typeID = (ULONGLONG)pMethodDesc->GetMethodTable();
            ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(&typeLogger, typeID, ETW::TypeSystemLog::kTypeLogBehaviorAlwaysLog);
            ULONGLONG loaderModuleID = (ULONGLONG)pMethodDesc->GetLoaderModule();

            StackSArray<ULONGLONG> rgTypeParameters;
            DWORD cParams = inst.GetNumArgs();

            BOOL fSucceeded = FALSE;
            EX_TRY
            {
                for (COUNT_T i = 0; i < cParams; i++)
                {
                    rgTypeParameters.Append((ULONGLONG)inst[i].AsPtr());
                }
                fSucceeded = TRUE;
            }
            EX_CATCH
            {
                fSucceeded = FALSE;
            }
            EX_END_CATCH(RethrowTerminalExceptions);
            if (!fSucceeded)
                goto done;

            // Log any referenced parameter types
            for (COUNT_T i=0; i < cParams; i++)
            {
                ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(&typeLogger, rgTypeParameters[i], ETW::TypeSystemLog::kTypeLogBehaviorAlwaysLog);
            }

            typeLogger.FireBulkTypeEvent();
            // Send method event

            FireEtwMethodDetails((ULONGLONG)pMethodDesc, // MethodID
                                        typeID,  // MethodType
                                        pMethodDesc->GetMemberDef(), // MethodToken
                                        cParams,
                                        loaderModuleID,
                                        rgTypeParameters.OpenRawBuffer());

            rgTypeParameters.CloseRawBuffer();
        }
done:;
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

VOID ETW::MethodLog::SendNonDuplicateMethodDetailsEvent(MethodDesc* pMethodDesc, MethodDescSet* set)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (set == NULL || !set->Contains(pMethodDesc))
    {
        SendMethodDetailsEvent(pMethodDesc);

        if (set != NULL)
            set->Add(pMethodDesc);
    }
}

/*****************************************************************/
/* This routine is used to send an ETW event just before a method starts jitting*/
/*****************************************************************/
VOID ETW::MethodLog::SendMethodJitStartEvent(
    MethodDesc *pMethodDesc,
    COR_ILMETHOD_DECODER* methodDecoder,
    SString *namespaceOrClassName,
    SString *methodName,
    SString *methodSignature)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    Module *pModule = NULL;
    Module *pLoaderModule = NULL; // This must not be used except for getting the ModuleID

    ULONGLONG ullMethodIdentifier=0;
    ULONGLONG ullModuleID=0;
    ULONG ulMethodToken=0;
    ULONG ulMethodILSize=0;
    PCWSTR szDtraceOutput1=W(""),szDtraceOutput2=W(""),szDtraceOutput3=W("");

    if(pMethodDesc) {
        pModule = pMethodDesc->GetModule();

        SendMethodDetailsEvent(pMethodDesc);

        bool bIsDynamicMethod = pMethodDesc->IsDynamicMethod();
        BOOL bIsGenericMethod = FALSE;
        if(pMethodDesc->GetMethodTable())
            bIsGenericMethod = pMethodDesc->HasClassOrMethodInstantiation();

        ullModuleID = (ULONGLONG)(TADDR) pModule;
        ullMethodIdentifier = (ULONGLONG)pMethodDesc;

        // Use MethodDesc if Dynamic or Generic methods
        if( bIsDynamicMethod || bIsGenericMethod)
        {
            if(bIsGenericMethod)
                ulMethodToken = (ULONG)pMethodDesc->GetMemberDef();
            if(bIsDynamicMethod) // if its a generic and a dynamic method, we would set the methodtoken to 0
                ulMethodToken = (ULONG)0;
        }
        else
        {
            ulMethodToken = (ULONG)pMethodDesc->GetMemberDef();
        }

        if (methodDecoder != NULL)
            ulMethodILSize = methodDecoder->GetCodeSize();

        SString tNamespace, tMethodName, tMethodSignature;
        if(!namespaceOrClassName|| !methodName|| !methodSignature || (methodName->IsEmpty() && namespaceOrClassName->IsEmpty() && methodSignature->IsEmpty()))
        {
            pMethodDesc->GetMethodInfo(tNamespace, tMethodName, tMethodSignature);
            namespaceOrClassName = &tNamespace;
            methodName = &tMethodName;
            methodSignature = &tMethodSignature;
        }

        // fire method information
        /* prepare events args for ETW and ETM */
        szDtraceOutput1 = (PCWSTR)namespaceOrClassName->GetUnicode();
        szDtraceOutput2 = (PCWSTR)methodName->GetUnicode();
        szDtraceOutput3 = (PCWSTR)methodSignature->GetUnicode();

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
VOID ETW::MethodLog::SendMethodEvent(MethodDesc *pMethodDesc, DWORD dwEventOptions, BOOL bIsJit, SString *namespaceOrClassName, SString *methodName, SString *methodSignature, PCODE pNativeCodeStartAddress, PrepareCodeConfig *pConfig, MethodDescSet* sentMethodDetailsSet)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    Module *pModule = NULL;
    Module *pLoaderModule = NULL; // This must not be used except for getting the ModuleID
    ULONGLONG ullMethodStartAddress=0, ullModuleID=0, ullMethodIdentifier=0;
    ULONG ulMethodSize=0, ulMethodToken=0, ulMethodFlags=0, ulColdMethodFlags=0;
    PWCHAR pMethodName=NULL, pNamespaceName=NULL, pMethodSignature=NULL;
    BOOL bShowVerboseOutput = FALSE, bIsDynamicMethod = FALSE, bHasSharedGenericCode = FALSE, bIsGenericMethod = FALSE;
    PCWSTR szDtraceOutput1=W(""),szDtraceOutput2=W(""),szDtraceOutput3=W("");

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

    if(bIsRundownProvider)
    {
        bShowVerboseOutput = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_VERBOSE,
            KEYWORDZERO);
    }
    else if(bIsRuntimeProvider)
    {
        bShowVerboseOutput = ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_VERBOSE,
            KEYWORDZERO);
    }

    pModule = pMethodDesc->GetModule();
    bIsDynamicMethod = (BOOL)pMethodDesc->IsDynamicMethod();
    bHasSharedGenericCode = pMethodDesc->IsSharedByGenericInstantiations();

    if(pMethodDesc->GetMethodTable())
        bIsGenericMethod = pMethodDesc->HasClassOrMethodInstantiation();

    NativeCodeVersionId nativeCodeId = 0;
    ulMethodFlags = ulMethodFlags |
        (bHasSharedGenericCode ? ETW::MethodLog::MethodStructs::SharedGenericCode : 0) |
        (bIsGenericMethod ? ETW::MethodLog::MethodStructs::GenericMethod : 0) |
        (bIsDynamicMethod ? ETW::MethodLog::MethodStructs::DynamicMethod : 0) |
        (bIsJit ? ETW::MethodLog::MethodStructs::JittedMethod : 0);
    if (pConfig != nullptr)
    {
        if (pConfig->ProfilerRejectedPrecompiledCode())
        {
            ulMethodFlags |= ETW::MethodLog::MethodStructs::ProfilerRejectedPrecompiledCode;
        }
        if (pConfig->ReadyToRunRejectedPrecompiledCode())
        {
            ulMethodFlags |= ETW::MethodLog::MethodStructs::ReadyToRunRejectedPrecompiledCode;
        }

#ifdef FEATURE_CODE_VERSIONING
        nativeCodeId = pConfig->GetCodeVersion().GetVersionId();
#endif
    }

    unsigned int jitOptimizationTier = (unsigned int)PrepareCodeConfig::GetJitOptimizationTier(pConfig, pMethodDesc);
    static_assert_no_msg((unsigned int)PrepareCodeConfig::JitOptimizationTier::Count - 1 <= MethodFlagsJitOptimizationTierLowMask);
    _ASSERTE(jitOptimizationTier <= MethodFlagsJitOptimizationTierLowMask);
    _ASSERTE(((ulMethodFlags >> MethodFlagsJitOptimizationTierShift) & MethodFlagsJitOptimizationTierLowMask) == 0);
    ulMethodFlags |= jitOptimizationTier << MethodFlagsJitOptimizationTierShift;

    // Intentionally set the extent flags (cold vs. hot) only after all the other common
    // flags (above) have been set.
    ulColdMethodFlags = ulMethodFlags | ETW::MethodLog::MethodStructs::ColdSection; // Method Extent (bits 28, 29, 30, 31)
    ulMethodFlags = ulMethodFlags | ETW::MethodLog::MethodStructs::HotSection;         // Method Extent (bits 28, 29, 30, 31)

    // MethodDesc ==> Code Address ==>JitManager
    TADDR start = PCODEToPINSTR(pNativeCodeStartAddress ? pNativeCodeStartAddress : pMethodDesc->GetNativeCode());
    if(start == 0) {
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

    ullModuleID = (ULONGLONG)(TADDR) pModule;
    ullMethodIdentifier = (ULONGLONG)pMethodDesc;

    // Use MethodDesc if Dynamic or Generic methods
    if( bIsDynamicMethod || bIsGenericMethod)
    {
        bShowVerboseOutput = TRUE;
        if(bIsGenericMethod)
            ulMethodToken = (ULONG)pMethodDesc->GetMemberDef();
        if(bIsDynamicMethod) // if its a generic and a dynamic method, we would set the methodtoken to 0
            ulMethodToken = (ULONG)0;
    }
    else
        ulMethodToken = (ULONG)pMethodDesc->GetMemberDef();

    SString tNamespace, tMethodName, tMethodSignature;

    // if verbose method load info needed, only then
    // find method name and signature and fire verbose method load info
    if(bShowVerboseOutput)
    {
        if(!namespaceOrClassName|| !methodName|| !methodSignature || (methodName->IsEmpty() && namespaceOrClassName->IsEmpty() && methodSignature->IsEmpty()))
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

    /* prepare events args for ETW and ETM */
    szDtraceOutput1 = (PCWSTR)pNamespaceName;
    szDtraceOutput2 = (PCWSTR)pMethodName;
    szDtraceOutput3 = (PCWSTR)pMethodSignature;

    SendNonDuplicateMethodDetailsEvent(pMethodDesc, sentMethodDetailsSet);

    if((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoad) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad))
    {
        if(bShowVerboseOutput)
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
                nativeCodeId);
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
                nativeCodeId);
        }
    }
    else if((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnload) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodUnload))
    {
        if(bShowVerboseOutput)
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
                nativeCodeId);
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
                nativeCodeId);
        }
    }
    else if((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCStart) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart))
    {
        if(bShowVerboseOutput)
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
                nativeCodeId);
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
                nativeCodeId);
        }
    }
    else if((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodDCEnd) ||
        (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd))
    {
        if(bShowVerboseOutput)
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
                nativeCodeId);
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
                nativeCodeId);
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
VOID ETW::MethodLog::SendMethodILToNativeMapEvent(MethodDesc * pMethodDesc, DWORD dwEventOptions, PCODE pNativeCodeStartAddress, DWORD nativeCodeId, ReJITID ilCodeId)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
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
        pNativeCodeStartAddress,
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
        FireEtwMethodILToNativeMap_V1(
            ullMethodIdentifier,
            nativeCodeId,
            0,          // Extent:  This event is only sent for JITted (not NGENd) methods, and
            //          currently there is only one extent (hot) for JITted methods.
            cMap,
            rguiILOffset,
            rguiNativeOffset,
            GetClrInstanceId(),
            ilCodeId);
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
        FireEtwMethodDCStartILToNativeMap_V1(ullMethodIdentifier, nativeCodeId, 0, cMap, rguiILOffset, rguiNativeOffset, GetClrInstanceId(), ilCodeId);
    if ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::MethodDCEndILToNativeMap) != 0)
        FireEtwMethodDCEndILToNativeMap_V1(ullMethodIdentifier, nativeCodeId, 0, cMap, rguiILOffset, rguiNativeOffset, GetClrInstanceId(), ilCodeId);
}

template<typename T>
static void WriteToBuffer(BYTE** pBuffer, const T& val)
{
    memcpy(*pBuffer, &val, sizeof(T));
    *pBuffer += sizeof(T);
}

VOID ETW::MethodLog::SendMethodRichDebugInfo(MethodDesc* pMethodDesc, PCODE pNativeCodeStartAddress, DWORD nativeCodeId, ReJITID ilCodeId, MethodDescSet* sentMethodDetailsSet)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (pMethodDesc == NULL)
        return;

    if (pMethodDesc->HasClassOrMethodInstantiation() && pMethodDesc->IsTypicalMethodDefinition())
        return;

    DebugInfoRequest request;
    request.InitFromStartingAddr(pMethodDesc, pNativeCodeStartAddress);

    auto fpNew = [](void* data, size_t numBytes)
    {
        return new (nothrow) BYTE[numBytes];
    };

    ICorDebugInfo::InlineTreeNode* inlineTree = NULL;
    ULONG32 numInlineTree = 0;
    ICorDebugInfo::RichOffsetMapping* mappings = NULL;
    ULONG32 numMappings = 0;
    if (DebugInfoManager::GetRichDebugInfo(request, fpNew, NULL, &inlineTree, &numInlineTree, &mappings, &numMappings))
    {
        static_assert_no_msg((std::is_same<decltype(inlineTree->Method), CORINFO_METHOD_HANDLE>::value));
        static_assert_no_msg((std::is_same<decltype(inlineTree->ILOffset), uint32_t>::value));
        static_assert_no_msg((std::is_same<decltype(inlineTree->Child), uint32_t>::value));
        static_assert_no_msg((std::is_same<decltype(inlineTree->Sibling), uint32_t>::value));

        static_assert_no_msg((std::is_same<decltype(mappings->ILOffset), uint32_t>::value));
        static_assert_no_msg((std::is_same<decltype(mappings->Inlinee), uint32_t>::value));
        static_assert_no_msg((std::is_same<decltype(mappings->NativeOffset), uint32_t>::value));

        const uint32_t inlineTreeNodeDataSize =
            sizeof(CORINFO_METHOD_HANDLE) +
            sizeof(uint32_t) * 3;

        const uint32_t richOffsetMappingDataSize =
            sizeof(uint32_t) * 3 +
            1; // source type

        unsigned dataSize = 8 + numInlineTree * inlineTreeNodeDataSize + numMappings * richOffsetMappingDataSize;

        InlineSBuffer<1024> buffer;
        buffer.Preallocate(dataSize);

        BYTE* pBuffer = &buffer[0];
        WriteToBuffer(&pBuffer, numInlineTree);
        WriteToBuffer(&pBuffer, numMappings);
        for (uint32_t i = 0; i < numInlineTree; i++)
        {
            WriteToBuffer(&pBuffer, inlineTree[i].Method);
            WriteToBuffer(&pBuffer, inlineTree[i].ILOffset);
            WriteToBuffer(&pBuffer, inlineTree[i].Child);
            WriteToBuffer(&pBuffer, inlineTree[i].Sibling);
        }

        for (uint32_t i = 0; i < numMappings; i++)
        {
            WriteToBuffer(&pBuffer, mappings[i].ILOffset);
            WriteToBuffer(&pBuffer, mappings[i].Inlinee);
            WriteToBuffer(&pBuffer, mappings[i].NativeOffset);
            WriteToBuffer(&pBuffer, static_cast<uint8_t>(mappings[i].Source));
        }

        _ASSERTE(static_cast<size_t>(pBuffer - &buffer[0]) == static_cast<size_t>(dataSize));

        const uint32_t chunkSize = 40000;
        const uint32_t finalChunkFlag = 0x80000000;

        const BYTE* data = &buffer[0];
        unsigned dataLeft = dataSize;
        for (uint32_t chunkIndex = 0; dataLeft > 0; chunkIndex++)
        {
            bool finalChunk = dataLeft <= chunkSize;
            uint32_t chunkSizeToEmit = finalChunk ? dataLeft : chunkSize;

            FireEtwJittedMethodRichDebugInfo(
                GetClrInstanceId(),
                (ULONGLONG)pMethodDesc,
                nativeCodeId,
                ilCodeId,
                chunkIndex | (finalChunk ? finalChunkFlag  : 0),
                chunkSizeToEmit,
                data);

            data += chunkSizeToEmit;
            dataLeft -= chunkSizeToEmit;
        }

        // Send details about inlinees that may not be sent otherwise.
        for (unsigned i = 0; i < numInlineTree; i++)
        {
            MethodDesc* pInlineeMethodDesc = reinterpret_cast<MethodDesc*>(inlineTree[i].Method);
            if (pInlineeMethodDesc != pMethodDesc)
                SendNonDuplicateMethodDetailsEvent(pInlineeMethodDesc, sentMethodDetailsSet);
        }
    }

    delete[] (BYTE*)inlineTree;
    delete[] (BYTE*)mappings;
}

VOID ETW::MethodLog::SendHelperEvent(ULONGLONG ullHelperStartAddress, ULONG ulHelperSize, LPCWSTR pHelperName)
{
    WRAPPER_NO_CONTRACT;
    if(pHelperName)
    {
         PCWSTR szDtraceOutput1=W("");
         ULONG methodFlags = ETW::MethodLog::MethodStructs::JitHelperMethod; // helper flag set
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
    }
}


/****************************************************************************/
/* This routine sends back method events of type 'dwEventOptions', for all
   NGEN methods in pModule */
/****************************************************************************/
VOID ETW::MethodLog::SendEventsForNgenMethods(Module *pModule, DWORD dwEventOptions)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (!pModule)
        return;

#ifdef FEATURE_READYTORUN
    if (pModule->IsReadyToRun())
    {
        ReadyToRunInfo::MethodIterator mi(pModule->GetReadyToRunInfo());
        while (mi.Next())
        {
            // Call GetMethodDesc_NoRestore instead of GetMethodDesc to avoid restoring methods at shutdown.
            MethodDesc *hotDesc = (MethodDesc *)mi.GetMethodDesc_NoRestore();
            if (hotDesc != NULL)
            {
                ETW::MethodLog::SendMethodEvent(hotDesc, dwEventOptions, FALSE);
            }
        }

        return;
    }
#endif // FEATURE_READYTORUN
}

// Called be ETW::MethodLog::SendEventsForJitMethods
// Sends the ETW events once our caller determines whether or not rejit locks can be acquired
VOID ETW::MethodLog::SendEventsForJitMethodsHelper(LoaderAllocator *pLoaderAllocatorFilter,
                                                   DWORD dwEventOptions,
                                                   BOOL fLoadOrDCStart,
                                                   BOOL fUnloadOrDCEnd,
                                                   BOOL fSendMethodEvent,
                                                   BOOL fSendILToNativeMapEvent,
                                                   BOOL fSendRichDebugInfoEvent,
                                                   BOOL fGetCodeIds)
{
    CONTRACTL{
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(pLoaderAllocatorFilter == nullptr || pLoaderAllocatorFilter->IsCollectible());
    _ASSERTE(pLoaderAllocatorFilter == nullptr || !fGetCodeIds);

    // Set of methods for which we already have sent a MethodDetails event.
    // Only used when sending rich debug info that would otherwise send a lot
    // of duplicate events.
    MethodDescSet sentMethodDetailsSet;
    MethodDescSet* pSentMethodDetailsSet = fSendRichDebugInfoEvent ? &sentMethodDetailsSet : NULL;

    EEJitManager::CodeHeapIterator heapIterator(pLoaderAllocatorFilter);
    while (heapIterator.Next())
    {
        MethodDesc * pMD = heapIterator.GetMethod();
        if (pMD == NULL)
            continue;

        PCODE codeStart = PINSTRToPCODE(heapIterator.GetMethodCode());

        // Get info relevant to the native code version. In some cases, such as collectible loader
        // allocators, we don't support code versioning so we need to short circuit the call.
        // This also allows our caller to avoid having to pre-enter the relevant locks.
        // see code:#TableLockHolder
        DWORD nativeCodeVersionId = 0;
        ReJITID ilCodeId = 0;
        NativeCodeVersion nativeCodeVersion;
#ifdef FEATURE_CODE_VERSIONING
        if (fGetCodeIds && pMD->IsVersionable())
        {
            _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
            nativeCodeVersion = pMD->GetCodeVersionManager()->GetNativeCodeVersion(pMD, codeStart);
            if (nativeCodeVersion.IsNull())
            {
                // The code version manager hasn't been updated with the jitted code
                if (codeStart != pMD->GetNativeCode())
                {
                    continue;
                }
            }
            else
            {
                nativeCodeVersionId = nativeCodeVersion.GetVersionId();
                ilCodeId = nativeCodeVersion.GetILCodeVersionId();
            }
        }
        else
#endif
        if (codeStart != pMD->GetNativeCode())
        {
            continue;
        }

        PrepareCodeConfig config(!nativeCodeVersion.IsNull() ? nativeCodeVersion : NativeCodeVersion(pMD), FALSE, FALSE);

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
                    &config,
                    pSentMethodDetailsSet);
            }
        }

        // Send any supplemental events requested for this MethodID
        if (fSendILToNativeMapEvent)
            ETW::MethodLog::SendMethodILToNativeMapEvent(pMD, dwEventOptions, codeStart, nativeCodeVersionId, ilCodeId);

        if (fSendRichDebugInfoEvent)
            ETW::MethodLog::SendMethodRichDebugInfo(pMD, codeStart, nativeCodeVersion.GetVersionId(), ilCodeId, pSentMethodDetailsSet);

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
                    &config);
            }
        }
    }
}

/****************************************************************************/
/* This routine sends back method events of type 'dwEventOptions', for all
   JITed methods in either a given LoaderAllocator (if pLoaderAllocatorFilter is non NULL)
   or in a given Domain (if pDomainFilter is non NULL) or for
   all methods (if both filters are null) */
/****************************************************************************/
// Code review indicates this method is never called with both filters NULL. Ideally we would
// assert this and change the comment above, but given I am making a change late in the release I am being cautious
VOID ETW::MethodLog::SendEventsForJitMethods(BaseDomain *pDomainFilter, LoaderAllocator *pLoaderAllocatorFilter, DWORD dwEventOptions)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

#if !defined(DACCESS_COMPILE)
    EX_TRY
    {
        // This is only called for JITted methods loading xor unloading
        BOOL fLoadOrDCStart = ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoadOrDCStartAny) != 0);
        BOOL fUnloadOrDCEnd = ((dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnloadOrDCEndAny) != 0);
        _ASSERTE((fLoadOrDCStart || fUnloadOrDCEnd) && !(fLoadOrDCStart && fUnloadOrDCEnd));

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

        BOOL fSendRichDebugInfoEvent =
            (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::JittedMethodRichDebugInfo) != 0;

        if (fSendILToNativeMapEvent || fSendRichDebugInfoEvent)
        {
            // The call to SendMethodILToNativeMapEvent assumes that the debugger's lazy
            // data has already been initialized, to ensure we don't try to do the lazy init
            // while under the implicit, notrigger CodeHeapIterator lock below.

            // g_pDebugInterface is initialized on startup on desktop CLR, regardless of whether a debugger
            // or profiler is loaded.  So it should always be available.
            _ASSERTE(g_pDebugInterface != NULL);
            g_pDebugInterface->InitializeLazyDataIfNecessary();
        }

        // #TableLockHolder:
        //
        // A word about ReJitManager::TableLockHolder... As we enumerate through the functions,
        // we may need to grab their code IDs. The ReJitManager grabs its table Crst in order to
        // fetch these. However, several other kinds of locks are being taken during this
        // enumeration, such as the SystemDomain lock and the EEJitManager::CodeHeapIterator's
        // lock. In order to avoid lock-leveling issues, we grab the appropriate ReJitManager
        // table locks after SystemDomain and before CodeHeapIterator. In particular, we need to
        // grab the SharedDomain's ReJitManager table lock as well as the specific AppDomain's
        // ReJitManager table lock for the current AppDomain we're iterating. Why the SharedDomain's
        // ReJitManager lock? For any given AppDomain we're iterating over, the MethodDescs we
        // find may be managed by that AppDomain's ReJitManger OR the SharedDomain's ReJitManager.
        // (This is due to generics and whether given instantiations may be shared based on their
        // arguments.) Therefore, we proactively take the SharedDomain's ReJitManager's table
        // lock up front, and then individually take the appropriate AppDomain's ReJitManager's
        // table lock that corresponds to the domain or module we're currently iterating over.
        //

        // We only support getting rejit IDs when filtering by domain.
#ifdef FEATURE_CODE_VERSIONING
        if (pDomainFilter)
        {
            CodeVersionManager::LockHolder codeVersioningLockHolder;
            SendEventsForJitMethodsHelper(
                pLoaderAllocatorFilter,
                dwEventOptions,
                fLoadOrDCStart,
                fUnloadOrDCEnd,
                fSendMethodEvent,
                fSendILToNativeMapEvent,
                fSendRichDebugInfoEvent,
                TRUE);
        }
        else
#endif
        {
            SendEventsForJitMethodsHelper(
                pLoaderAllocatorFilter,
                dwEventOptions,
                fLoadOrDCStart,
                fUnloadOrDCEnd,
                fSendMethodEvent,
                fSendILToNativeMapEvent,
                fSendRichDebugInfoEvent,
                FALSE);
        }
    } EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
#endif // !DACCESS_COMPILE
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
VOID ETW::EnumerationLog::IterateAppDomain(AppDomain * pAppDomain, DWORD enumerationOptions)
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

    // Now it's safe to do the iteration
    IterateDomain(pAppDomain, enumerationOptions);
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
VOID ETW::EnumerationLog::IterateDomain(BaseDomain *pDomain, DWORD enumerationOptions)
{
    CONTRACTL {
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
        if(enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
        {
            ETW::LoaderLog::SendDomainEvent(pDomain, enumerationOptions);
        }

        // DC End or Unload Jit Method events
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnloadOrDCEndAny)
        {
            ETW::MethodLog::SendEventsForJitMethods(pDomain, NULL, enumerationOptions);
        }

        AppDomain::AssemblyIterator assemblyIterator = pDomain->AsAppDomain()->IterateAssembliesEx(
            (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        while (assemblyIterator.Next(pDomainAssembly.This()))
        {
            CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetAssembly();
            if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
            {
                ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
            }

            Module * pModule = pDomainAssembly->GetModule();
            ETW::EnumerationLog::IterateModule(pModule, enumerationOptions);

            if((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
                (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload))
            {
                ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
            }
        }

        // DC Start or Load Jit Method events
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoadOrDCStartAny)
        {
            ETW::MethodLog::SendEventsForJitMethods(pDomain, NULL, enumerationOptions);
        }

        // DC End or Unload events for Domain
        if((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload))
        {
            ETW::LoaderLog::SendDomainEvent(pDomain, enumerationOptions);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
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
VOID ETW::EnumerationLog::IterateCollectibleLoaderAllocator(AssemblyLoaderAllocator *pLoaderAllocator, DWORD enumerationOptions)
{
    CONTRACTL {
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

        // Iterate on all DomainAssembly loaded from the same AssemblyLoaderAllocator
        DomainAssemblyIterator domainAssemblyIt = pLoaderAllocator->Id()->GetDomainAssemblyIterator();
        while (!domainAssemblyIt.end())
        {
            Assembly *pAssembly = domainAssemblyIt->GetAssembly(); // TODO: handle iterator

            Module* pModule = domainAssemblyIt->GetModule();
            ETW::EnumerationLog::IterateModule(pModule, enumerationOptions);

            if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload)
            {
                ETW::EnumerationLog::IterateAssembly(pAssembly, enumerationOptions);
            }

            domainAssemblyIt++;
        }

        // Load Jit Method events
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoad)
        {
            ETW::MethodLog::SendEventsForJitMethods(NULL, pLoaderAllocator, enumerationOptions);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/********************************************************************************/
/* This routine fires ETW events for Assembly and the DomainModule's in them
   based on enumerationOptions.*/
/********************************************************************************/
VOID ETW::EnumerationLog::IterateAssembly(Assembly *pAssembly, DWORD enumerationOptions)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pAssembly != NULL);
    } CONTRACTL_END;

    EX_TRY
    {
        // DC Start events for Assembly
        if(enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart)
        {
            ETW::LoaderLog::SendAssemblyEvent(pAssembly, enumerationOptions);
        }

        // DC Start, DCEnd, events for DomainModule
        if((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart))
        {
            Module* pModule = pAssembly->GetDomainAssembly()->GetModule();
            ETW::LoaderLog::SendModuleEvent(pModule, enumerationOptions, TRUE);
        }

        // DC End or Unload events for Assembly
        if((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleUnload))
        {
            ETW::LoaderLog::SendAssemblyEvent(pAssembly, enumerationOptions);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
}

/********************************************************************************/
/* This routine fires ETW events for Module, their range information and the NGEN methods in them
   based on enumerationOptions.*/
/********************************************************************************/
VOID ETW::EnumerationLog::IterateModule(Module *pModule, DWORD enumerationOptions)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pModule != NULL);
    } CONTRACTL_END;

    EX_TRY
    {
        // DC Start events for Module
        if((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCStart) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeDCStart))
        {
            ETW::LoaderLog::SendModuleEvent(pModule, enumerationOptions);
        }

        // DC Start or Load or DC End or Unload Ngen Method events
        if((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodLoad) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCStart) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodUnload) ||
           (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::NgenMethodDCEnd))
        {
            ETW::MethodLog::SendEventsForNgenMethods(pModule, enumerationOptions);
        }

        // DC End or Unload events for Module
        if((enumerationOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd) ||
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
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context,
                                         TRACE_LEVEL_INFORMATION,
                                         CLR_PERFTRACK_PRIVATE_KEYWORD) &&
            (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::ModuleRangeLoadPrivate))
        {
            ETW::LoaderLog::SendModuleEvent(pModule, enumerationOptions);
        }
    } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);
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
//

// static
VOID ETW::EnumerationLog::EnumerationHelper(Module *moduleFilter, BaseDomain *domainFilter, DWORD enumerationOptions)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (moduleFilter)
    {
        // Iterate modules first because their number is usually smaller then the number of methods.
        // Thus hitting a timeout due to a large number of methods will not affect modules rundown.tf g
        ETW::EnumerationLog::IterateModule(moduleFilter, enumerationOptions);

        // As best I can tell from code review, these if statements below are never true. There is
        // only one caller to this method that specifies a moduleFilter, ETW::LoaderLog::ModuleLoad.
        // That method never specifies these flags. Because it is late in a release cycle I am not
        // making a change, but if you see this comment early in the next release cycle consider
        // deleting this apparently dead code.

        // DC End or Unload Jit Method events from all Domains
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodUnloadOrDCEndAny)
        {
            ETW::MethodLog::SendEventsForJitMethods(NULL, NULL, enumerationOptions);
        }

        // DC Start or Load Jit Method events from all Domains
        if (enumerationOptions & ETW::EnumerationLog::EnumerationStructs::JitMethodLoadOrDCStartAny)
        {
            ETW::MethodLog::SendEventsForJitMethods(NULL, NULL, enumerationOptions);
        }
    }
    else
    {
        if(domainFilter)
        {
            if(domainFilter->IsAppDomain())
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
            AppDomain *pDomain = AppDomain::GetCurrentDomain();
            if (pDomain != NULL)
            {
                ETW::EnumerationLog::IterateAppDomain(pDomain, enumerationOptions);
            }
        }
    }
}

void ETW::CompilationLog::TieredCompilation::GetSettings(UINT32 *flagsRef)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(g_pConfig->TieredCompilation());
    _ASSERTE(flagsRef != nullptr);

    enum class Flags : UINT32
    {
        None = 0x0,
        QuickJit = 0x1,
        QuickJitForLoops = 0x2,
        TieredPGO = 0x4,
        ReadyToRun = 0x8,
    };

    UINT32 flags = (UINT32)Flags::None;
    if (g_pConfig->TieredCompilation_QuickJit())
    {
        flags |= (UINT32)Flags::QuickJit;
        if (g_pConfig->TieredCompilation_QuickJitForLoops())
        {
            flags |= (UINT32)Flags::QuickJitForLoops;
        }
    }
#ifdef FEATURE_PGO
    if (g_pConfig->TieredPGO())
    {
        flags |= (UINT32)Flags::TieredPGO;
    }
#endif
#ifdef FEATURE_READYTORUN
    if (g_pConfig->ReadyToRun())
    {
        flags |= (UINT32)Flags::ReadyToRun;
    }
#endif
    *flagsRef = flags;
}

void ETW::CompilationLog::TieredCompilation::Runtime::SendSettings()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(g_pConfig->TieredCompilation());

    UINT32 flags;
    GetSettings(&flags);

    FireEtwTieredCompilationSettings(GetClrInstanceId(), flags);
}

void ETW::CompilationLog::TieredCompilation::Rundown::SendSettings()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(g_pConfig->TieredCompilation());

    UINT32 flags;
    GetSettings(&flags);

    FireEtwTieredCompilationSettingsDCStart(GetClrInstanceId(), flags);
}

void ETW::CompilationLog::TieredCompilation::Runtime::SendPause()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(g_pConfig->TieredCompilation());

    FireEtwTieredCompilationPause(GetClrInstanceId());
}

void ETW::CompilationLog::TieredCompilation::Runtime::SendResume(UINT32 newMethodCount)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(g_pConfig->TieredCompilation());

    FireEtwTieredCompilationResume(GetClrInstanceId(), newMethodCount);
}

void ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStart(UINT32 pendingMethodCount)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(g_pConfig->TieredCompilation());

    FireEtwTieredCompilationBackgroundJitStart(GetClrInstanceId(), pendingMethodCount);
}

void ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStop(UINT32 pendingMethodCount, UINT32 jittedMethodCount)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(g_pConfig->TieredCompilation());

    FireEtwTieredCompilationBackgroundJitStop(GetClrInstanceId(), pendingMethodCount, jittedMethodCount);
}

#endif // !FEATURE_NATIVEAOT

#ifdef FEATURE_PERFTRACING
#include "eventpipeadapter.h"
bool EventPipeHelper::Enabled()
{
    LIMITED_METHOD_CONTRACT;
    return EventPipeAdapter::Enabled();
}

bool EventPipeHelper::IsEnabled(DOTNET_TRACE_CONTEXT Context, UCHAR Level, ULONGLONG Keyword)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    if (!Context.EventPipeProvider.IsEnabled)
    {
        return false;
    }

    if ((Level <= Context.EventPipeProvider.Level) ||
        (Context.EventPipeProvider.Level == EP_EVENT_LEVEL_LOGALWAYS))
    {
        return (Keyword == (ULONGLONG)0) || (Keyword & Context.EventPipeProvider.EnabledKeywordsBitmask) != 0;
    }

    return false;
}
#endif // FEATURE_PERFTRACING

#if defined(HOST_UNIX)  && defined(FEATURE_PERFTRACING)
// This is a wrapper method for LTTng. See https://github.com/dotnet/coreclr/pull/27273 for details.
extern "C" bool XplatEventLoggerIsEnabled()
{
    return XplatEventLogger::IsEventLoggingEnabled();
}
#endif // HOST_UNIX && FEATURE_PERFTRACING
