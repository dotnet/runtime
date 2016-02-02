// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ProfilingHelper.cpp
// 

//
// Implementation of helper classes used for miscellaneous purposes within the profiling
// API
// 
// ======================================================================================

// 
// #LoadUnloadCallbackSynchronization
// 
// There is synchronization around loading profilers, unloading profilers, and issuing
// callbacks to profilers, to ensure that we know when it's safe to detach profilers or
// to call into profilers. The synchronization scheme is intentionally lockless on the
// mainline path (issuing callbacks into the profiler), with heavy locking on the
// non-mainline path (loading / unloading profilers).
// 
// PROTECTED DATA
// 
// The synchronization protects the following data:
// 
//     * ProfilingAPIDetach::s_profilerDetachInfo
//     * (volatile) g_profControlBlock.curProfStatus.m_profStatus
//     * (volatile) g_profControlBlock.pProfInterface
//         * latter implies the profiler DLL's load status is protected as well, as
//             pProfInterface changes between non-NULL and NULL as a profiler DLL is
//             loaded and unloaded, respectively.
//         
// SYNCHRONIZATION COMPONENTS
// 
// * Simple Crst: code:ProfilingAPIUtility::s_csStatus
// * Lockless, volatile per-thread counters: code:EvacuationCounterHolder
// * Profiler status transition invariants and CPU buffer flushing:
//     code:CurrentProfilerStatus::Set
//         
// WRITERS
// 
// The above data is considered to be "written to" when a profiler is loaded or unloaded,
// or the status changes (see code:ProfilerStatus), or a request to detach the profiler
// is received (see code:ProfilingAPIDetach::RequestProfilerDetach), or the DetachThread
// consumes or modifies the contents of code:ProfilingAPIDetach::s_profilerDetachInfo.
// All these cases are serialized with each other by the simple Crst:
// code:ProfilingAPIUtility::s_csStatus
// 
// READERS
// 
// Readers are the mainline case and are lockless. A "reader" is anyone who wants to
// issue a profiler callback. Readers are scattered throughout the runtime, and have the
// following format:
//    {
//        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
//        g_profControlBlock.pProfInterface->AppDomainCreationStarted(MyAppDomainID);
//        END_PIN_PROFILER();
//    }
// The BEGIN / END macros do the following:
// * Evaluate the expression argument (e.g., CORProfilerTrackAppDomainLoads()). This is a
//     "dirty read" as the profiler could be detached at any moment during or after that
//     evaluation.
// * If true, push a code:EvacuationCounterHolder on the stack, which increments the
//     per-thread evacuation counter (not interlocked).
// * Re-evaluate the expression argument. This time, it's a "clean read" (see below for
//     why).
// * If still true, execute the statements inside the BEGIN/END block. Inside that block,
//     the profiler is guaranteed to remain loaded, because the evacuation counter
//     remains nonzero (again, see below).
// * Once the BEGIN/END block is exited, the evacuation counter is decremented, and the
//     profiler is unpinned and allowed to detach.
//     
// READER / WRITER COORDINATION
// 
// The above ensures that a reader never touches g_profControlBlock.pProfInterface and
// all it embodies (including the profiler DLL code and callback implementations) unless
// the reader was able to increment its thread's evacuation counter AND re-verify that
// the profiler's status is still active (the status check is included in the macro's
// expression argument, such as CORProfilerTrackAppDomainLoads()).
// 
// At the same time, a profiler DLL is never unloaded (nor
// g_profControlBlock.pProfInterface deleted and NULLed out) UNLESS the writer performs
// these actions:
// * (a) Set the profiler's status to a non-active state like kProfStatusDetaching or
//     kProfStatusNone
// * (b) Call FlushProcessWriteBuffers()
// * (c) Grab thread store lock, iterate through all threads, and verify each per-thread
//     evacuation counter is zero.
//     
// The above steps are why it's considered a "clean read" if a reader first increments
// its evacuation counter and then checks the profiler status. Once the writer flushes
// the CPU buffers (b), the reader will see the updated status (from a) and know not to
// use g_profControlBlock.pProfInterface. And if the reader clean-reads the status before
// the buffers were flushed, then the reader will have incremented its evacuation counter
// first, which the writer will be sure to see in (c).  For more details about how the
// evacuation counters work, see code:ProfilingAPIDetach::IsProfilerEvacuated.
// 
// WHEN ARE BEGIN/END_PIN_PROFILER REQUIRED?
// 
// In general, any time you access g_profControlBlock.pProfInterface, you must be inside
// a BEGIN/END_PIN_PROFILER block. This is pretty much always true throughout the EE, but
// there are some exceptions inside the profiling API code itself, where the BEGIN / END
// macros are unnecessary:
//     * If you are inside a public ICorProfilerInfo function's implementation, the
//         profiler is already pinned. This is because the profiler called the Info
//         function from either:
//         * a callback implemented inside of g_profControlBlock.pProfInterface, in which
//             case the BEGIN/END macros are already in place around the call to that
//             callback, OR
//         * a hijacked thread or a thread of the profiler's own creation. In either
//             case, it's the profiler's responsibility to end hijacking and end its own
//             threads before requesting a detach. So the profiler DLL is guaranteed not
//             to disappear while hijacking or profiler-created threads are in action.
//    * If you're executing while code:ProfilingAPIUtility::s_csStatus is held, then
//        you're explicitly serialized against all code that might unload the profiler's
//        DLL and delete g_profControlBlock.pProfInterface. So the profiler is therefore
//        still guaranteed not to disappear.
//    * If slow ELT helpers, fast ELT hooks, or profiler-instrumented code is on the
//        stack, then the profiler cannot be detached yet anyway. Today, we outright
//        refuse a detach request from a profiler that instrumented code or enabled ELT.
//        Once rejit / revert is implemented, the evacuation checks will ensure all
//        instrumented code (including ELT) are reverted and off all stacks before
//        attempting to unload the profielr.


#include "common.h"

#ifdef PROFILING_SUPPORTED

#include "eeprofinterfaces.h"
#include "eetoprofinterfaceimpl.h"
#include "eetoprofinterfaceimpl.inl"
#include "corprof.h"
#include "proftoeeinterfaceimpl.h"
#include "proftoeeinterfaceimpl.inl"
#include "profilinghelper.h"
#include "profilinghelper.inl"
#include "eemessagebox.h"

#if defined(FEATURE_PROFAPI_EVENT_LOGGING) && !defined(FEATURE_CORECLR)
#include <eventmsg.h>
#endif // FEATURE_PROFAPI_EVENT_LOGGING) && !FEATURE_CORECLR

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 
#include "profattach.h"
#include "profdetach.h"
#endif // FEATURE_PROFAPI_ATTACH_DETACH 

#include "utilcode.h"

#ifndef FEATURE_PAL
#include "securitywrapper.h"
#endif // !FEATURE_PAL

//---------------------------------------------------------------------------------------
// Normally, this would go in profilepriv.inl, but it's not easily inlineable because of
// the use of BEGIN/END_PIN_PROFILER
//
// Return Value:
//      TRUE iff security transparency checks in full trust assemblies should be disabled
//      due to the profiler.
//
BOOL CORProfilerBypassSecurityChecks()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    {
        BEGIN_PIN_PROFILER(CORProfilerPresent());

        // V2 profiler binaries, for compatibility purposes, should bypass transparency
        // checks in full trust assemblies.
        if (!(&g_profControlBlock)->pProfInterface->IsCallback3Supported())
            return TRUE;
        
        // V4 profiler binaries must opt in to bypasssing transparency checks in full trust
        // assemblies.
        if (((&g_profControlBlock)->dwEventMask & COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST) != 0)
            return TRUE;

        END_PIN_PROFILER();
    }

    // All other cases, including no profiler loaded at all: Don't bypass
    return FALSE;
}

// ----------------------------------------------------------------------------
// CurrentProfilerStatus methods


//---------------------------------------------------------------------------------------
//
// Updates the value indicating the profiler's current status
//
// Arguments:
//      profStatus - New value (from enum ProfilerStatus) to set.
//
// Notes:
//    Sets the status under a lock, and performs a debug-only check to verify that the
//    status transition is a legal one.  Also performs a FlushStoreBuffers() after
//    changing the status when necessary.
//

void CurrentProfilerStatus::Set(ProfilerStatus newProfStatus)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(ProfilingAPIUtility::GetStatusCrst() != NULL);

    {
        // Need to serialize attempts to transition the profiler status.  For example, a
        // profiler in one thread could request a detach, while the CLR in another
        // thread is transitioning the profiler from kProfStatusInitializing* to
        // kProfStatusActive
        CRITSEC_Holder csh(ProfilingAPIUtility::GetStatusCrst());

        // Based on what the old status is, verify the new status is a legal transition.
        switch(m_profStatus)
        {
        default:
            _ASSERTE(!"Unknown ProfilerStatus");
            break;

        case kProfStatusNone:
            _ASSERTE((newProfStatus == kProfStatusInitializingForStartupLoad) ||
                (newProfStatus == kProfStatusInitializingForAttachLoad));
            break;

        case kProfStatusDetaching:
            _ASSERTE(newProfStatus == kProfStatusNone);
            break;

        case kProfStatusInitializingForStartupLoad:
        case kProfStatusInitializingForAttachLoad:
            _ASSERTE((newProfStatus == kProfStatusActive) ||
                (newProfStatus == kProfStatusNone));
            break;

        case kProfStatusActive:
            _ASSERTE((newProfStatus == kProfStatusNone) ||
                (newProfStatus == kProfStatusDetaching));
            break;
        }

        m_profStatus = newProfStatus;
    }

#if !defined(DACCESS_COMPILE)
    if (((newProfStatus == kProfStatusNone) ||
         (newProfStatus == kProfStatusDetaching) ||
         (newProfStatus == kProfStatusActive)))
    {
        // Flush the store buffers on all CPUs, to ensure other threads see that
        // g_profControlBlock.curProfStatus has changed. The important status changes to
        // flush are:
        //     * to kProfStatusNone or kProfStatusDetaching so other threads know to stop
        //         making calls into the profiler
        //     * to kProfStatusActive, to ensure callbacks can be issued by the time an
        //         attaching profiler receives ProfilerAttachComplete(), so the profiler
        //         can safely perform catchup at that time (see
        //         code:#ProfCatchUpSynchronization).
        //         
        ::FlushProcessWriteBuffers();
    }
#endif // !defined(DACCESS_COMPILE)
}


//---------------------------------------------------------------------------------------
// ProfilingAPIUtility members


// See code:#LoadUnloadCallbackSynchronization.
CRITSEC_COOKIE ProfilingAPIUtility::s_csStatus = NULL;


SidBuffer * ProfilingAPIUtility::s_pSidBuffer = NULL;

// ----------------------------------------------------------------------------
// ProfilingAPIUtility::AppendSupplementaryInformation
// 
// Description:
//    Helper to the event logging functions to append the process ID and string
//    resource ID to the end of the message.
//    
// Arguments:
//    * iStringResource - [in] String resource ID to append to message.
//    * pString - [in/out] On input, the string to log so far. On output, the original
//        string with the process ID info appended.
//        

// static
void ProfilingAPIUtility::AppendSupplementaryInformation(int iStringResource, SString * pString)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This loads resource strings, which takes locks.
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    StackSString supplementaryInformation;

    if (!supplementaryInformation.LoadResource(
        CCompRC::Debugging,
        IDS_PROF_SUPPLEMENTARY_INFO 
        ))
    {
        // Resource not found; should never happen.
        return;
    }

    pString->Append(W("  "));
    pString->AppendPrintf(
        supplementaryInformation, 
        GetCurrentProcessId(),
        iStringResource);
}

//---------------------------------------------------------------------------------------
//
// Helper function to log publicly-viewable errors about profiler loading and
// initialization.
// 
//
// Arguments:
//      * iStringResourceID - resource ID of string containing message to log
//      * wEventType - same constant used in win32 to specify the type of event:
//                   usually EVENTLOG_ERROR_TYPE, EVENTLOG_WARNING_TYPE, or
//                   EVENTLOG_INFORMATION_TYPE
//      * insertionArgs - 0 or more values to be inserted into the string to be logged
//          (>0 only if iStringResourceID contains format arguments (%)).
//

// static
void ProfilingAPIUtility::LogProfEventVA(
    int iStringResourceID, 
    WORD wEventType,
    va_list insertionArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This loads resource strings, which takes locks.
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifndef FEATURE_PROFAPI_EVENT_LOGGING


    // Rotor messages go to message boxes

    EEMessageBoxCatastrophic(
        iStringResourceID,              // Text message to display
        IDS_EE_PROFILING_FAILURE,       // Titlebar of message box
        insertionArgs);                 // Insertion strings for text message

#else // FEATURE_PROFAPI_EVENT_LOGGING
    
    // Non-rotor messages go to the event log

    StackSString messageFromResource;
    StackSString messageToLog;

    if (!messageFromResource.LoadResource(
        CCompRC::Debugging,
        iStringResourceID 
        ))
    {
        // Resource not found; should never happen.
        return;
    }

    messageToLog.VPrintf(messageFromResource, insertionArgs);

    AppendSupplementaryInformation(iStringResourceID, &messageToLog);

#if defined(FEATURE_CORECLR)
    // CoreCLR on Windows ouputs debug strings for diagnostic messages.
    WszOutputDebugString(messageToLog);
#else
    // Get the user SID for the current process, so it can be provided to the event
    // logging API, which will then fill out the "User" field in the event log entry. If
    // this fails, that's not fatal. We can just pass NULL for the PSID, and the "User"
    // field will be left blank.
    PSID psid = NULL;
    HRESULT hr = GetCurrentProcessUserSid(&psid);
    if (FAILED(hr))
    {
        // No biggie.  Just pass in a NULL psid, and the User field will be empty
        _ASSERTE(psid == NULL);
    }

    // On desktop CLR builds, the profiling API uses the event log for end-user-friendly
    // diagnostic messages.
    ReportEventCLR(wEventType,             // wType
                   0,                      // wCategory
                   COR_Profiler,           // dwEventID
                   psid,                   // lpUserSid
                   &messageToLog);         // uh duh
#endif // FEATURE_CORECLR

#endif // FEATURE_PROFAPI_EVENT_LOGGING
}

// See code:ProfilingAPIUtility.LogProfEventVA for description of arguments.
// static
void ProfilingAPIUtility::LogProfError(int iStringResourceID, ...)
{
    CONTRACTL
{
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This loads resource strings, which takes locks.
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    va_list insertionArgs;
    va_start(insertionArgs, iStringResourceID);
    LogProfEventVA(
        iStringResourceID, 
        EVENTLOG_ERROR_TYPE, 
        insertionArgs);
    va_end(insertionArgs);
}

// See code:ProfilingAPIUtility.LogProfEventVA for description of arguments.
// static
void ProfilingAPIUtility::LogProfInfo(int iStringResourceID, ...)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This loads resource strings, which takes locks.
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

// Rotor uses message boxes instead of event log, and it would be disruptive to
// pop a messagebox in the user's face every time an app runs with a profiler
// configured to load.  So only log this only when we don't do a pop-up.
#ifdef FEATURE_PROFAPI_EVENT_LOGGING
    va_list insertionArgs;
    va_start(insertionArgs, iStringResourceID);
    LogProfEventVA(
        iStringResourceID, 
        EVENTLOG_INFORMATION_TYPE, 
        insertionArgs);
    va_end(insertionArgs);
#endif //FEATURE_PROFAPI_EVENT_LOGGING
}

#ifdef PROF_TEST_ONLY_FORCE_ELT
// Special forward-declarations of the profiling API's slow-path enter/leave/tailcall
// hooks. These need to be forward-declared here so that they may be referenced in
// InitializeProfiling() below solely for the debug-only, test-only code to allow
// enter/leave/tailcall to be turned on at startup without a profiler. See
// code:ProfControlBlock#TestOnlyELT
EXTERN_C void __stdcall ProfileEnterNaked(UINT_PTR clientData);
EXTERN_C void __stdcall ProfileLeaveNaked(UINT_PTR clientData);
EXTERN_C void __stdcall ProfileTailcallNaked(UINT_PTR clientData);
#endif //PROF_TEST_ONLY_FORCE_ELT

// ----------------------------------------------------------------------------
// ProfilingAPIUtility::InitializeProfiling
//
// This is the top-most level of profiling API initialization, and is called directly by
// EEStartupHelper() (in ceemain.cpp).  This initializes internal structures relating to the
// Profiling API.  This also orchestrates loading the profiler and initializing it (if
// its GUID is specified in the environment).
//
// Return Value:
//    HRESULT indicating success or failure.  This is generally very lenient about internal
//    failures, as we don't want them to prevent the startup of the app:
//    S_OK = Environment didn't request a profiler, or
//           Environment did request a profiler, and it was loaded successfully
//    S_FALSE = There was a problem loading the profiler, but that shouldn't prevent the app
//              from starting up
//    else (failure) = There was a serious problem that should be dealt with by the caller
//
// Notes:
//     This function (or one of its callees) will log an error to the event log
//     if there is a failure
//
// Assumptions:
//    InitializeProfiling is called during startup, AFTER the host has initialized its
//    settings and the config variables have been read, but BEFORE the finalizer thread
//    has entered its first wait state.  ASSERTs are placed in
//    code:ProfilingAPIAttachDetach::Initialize (which is called by this function, and
//    which depends on these assumptions) to verify.

// static
HRESULT ProfilingAPIUtility::InitializeProfiling()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    } 
    CONTRACTL_END;

    InitializeLogging();

    // NULL out / initialize members of the global profapi structure
    g_profControlBlock.Init();

    if (IsCompilationProcess())
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling disabled for ngen process.\n"));
        return S_OK;
    }

    AttemptLoadProfilerForStartup();
    // For now, the return value from AttemptLoadProfilerForStartup is of no use to us. 
    // Any event has been logged already by AttemptLoadProfilerForStartup, and
    // regardless of whether a profiler got loaded, we still need to continue.


#ifdef PROF_TEST_ONLY_FORCE_ELT
    // Test-only, debug-only code to enable ELT on startup regardless of whether a
    // startup profiler is loaded.  See code:ProfControlBlock#TestOnlyELT.
    DWORD dwEnableSlowELTHooks = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TestOnlyEnableSlowELTHooks);
    if (dwEnableSlowELTHooks != 0)
    {
        (&g_profControlBlock)->fTestOnlyForceEnterLeave = TRUE;
        SetJitHelperFunction(CORINFO_HELP_PROF_FCN_ENTER, (void *) ProfileEnterNaked);
        SetJitHelperFunction(CORINFO_HELP_PROF_FCN_LEAVE, (void *) ProfileLeaveNaked);
        SetJitHelperFunction(CORINFO_HELP_PROF_FCN_TAILCALL, (void *) ProfileTailcallNaked);
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Enabled test-only slow ELT hooks.\n"));
    }
#endif //PROF_TEST_ONLY_FORCE_ELT

#ifdef PROF_TEST_ONLY_FORCE_OBJECT_ALLOCATED
    // Test-only, debug-only code to enable ObjectAllocated callbacks on startup regardless of whether a
    // startup profiler is loaded.  See code:ProfControlBlock#TestOnlyObjectAllocated.
    DWORD dwEnableObjectAllocated = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TestOnlyEnableObjectAllocatedHook);
    if (dwEnableObjectAllocated != 0)
    {
        (&g_profControlBlock)->fTestOnlyForceObjectAllocated = TRUE;
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Enabled test-only object ObjectAllocated hooks.\n"));
    }
#endif //PROF_TEST_ONLY_FORCE_ELT


#ifdef _DEBUG
    // Test-only, debug-only code to allow attaching profilers to call ICorProfilerInfo inteface, 
    // which would otherwise be disallowed for attaching profilers
    DWORD dwTestOnlyEnableICorProfilerInfo = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TestOnlyEnableICorProfilerInfo);
    if (dwTestOnlyEnableICorProfilerInfo != 0)
    {
        (&g_profControlBlock)->fTestOnlyEnableICorProfilerInfo = TRUE;
    }
#endif // _DEBUG

    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIUtility::ProfilerCLSIDFromString
// 
// Description:
//    Takes a string form of a CLSID (or progid, believe it or not), and returns the
//    corresponding CLSID structure.
//    
// Arguments:
//    * wszClsid - [in / out] CLSID string to convert. This may also be a progid. This
//        ensures our behavior is backward-compatible with previous CLR versions. I don't
//        know why previous versions allowed the user to set a progid in the environment,
//        but well whatever. On [out], this string is normalized in-place (e.g.,
//        double-quotes around progid are removed).
//    * pClsid - [out] CLSID structure corresponding to wszClsid
//
// Return Value:
//    HRESULT indicating success or failure.
//    
// Notes:
//    * An event is logged if there is a failure.
//        

// static
HRESULT ProfilingAPIUtility::ProfilerCLSIDFromString(
    __inout_z LPWSTR wszClsid, 
    CLSID * pClsid)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    } 
    CONTRACTL_END;

    _ASSERTE(wszClsid != NULL);
    _ASSERTE(pClsid != NULL);

    HRESULT hr;

    // Translate the string into a CLSID
    if (*wszClsid == W('{'))
    {
        hr = IIDFromString(wszClsid, pClsid);
    }
    else
    {
#ifndef FEATURE_PAL
        WCHAR *szFrom, *szTo;

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26000) // "espX thinks there is an overflow here, but there isn't any"
#endif
        for (szFrom=szTo=wszClsid;  *szFrom;  )
        {
            if (*szFrom == W('"'))
            {
                ++szFrom;
                continue;
            }
            *szTo++ = *szFrom++;
        }
        *szTo = 0;
        hr = CLSIDFromProgID(wszClsid, pClsid);
#ifdef _PREFAST_
#pragma warning(pop)
#endif /*_PREFAST_*/

#else // !FEATURE_PAL
        // ProgID not supported on FEATURE_PAL
        hr = E_INVALIDARG;
#endif // !FEATURE_PAL
    }

    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_INFO10, 
            "**PROF: Invalid CLSID or ProgID (%S).  hr=0x%x.\n",
            wszClsid,
            hr));
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_BAD_CLSID, wszClsid, hr);
        return hr;
    }

    return S_OK;
}

// ----------------------------------------------------------------------------
// ProfilingAPIUtility::AttemptLoadProfilerForStartup
// 
// Description:
//    Checks environment or registry to see if the app is configured to run with a
//    profiler loaded on startup. If so, this calls LoadProfiler() to load it up.
//    
// Arguments:
//
// Return Value:
//    * S_OK: Startup-profiler has been loaded
//    * S_FALSE: No profiler is configured for startup load
//    * else, HRESULT indicating failure that occurred
//        
// Assumptions:
//    * This should be called on startup, after g_profControlBlock is initialized, but
//        before any attach infrastructure is initialized. This ensures we don't receive
//        an attach request while startup-loading a profiler.
//        
// Notes:
//    * This or its callees will ensure an event is logged on failure (though will be
//        silent if no profiler is configured for startup load (which causes S_FALSE to
//        be returned)
//        

// static
HRESULT ProfilingAPIUtility::AttemptLoadProfilerForStartup()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    } 
    CONTRACTL_END;

    HRESULT hr;

    // Find out if profiling is enabled
    DWORD fProfEnabled = 0;

#ifdef FEATURE_CORECLR
    fProfEnabled = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_ENABLE_PROFILING);
#else //FEATURE_CORECLR
    fProfEnabled = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_COR_ENABLE_PROFILING);
#endif //FEATURE_CORECLR

    // If profiling is not enabled, return.
    if (fProfEnabled == 0)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling not enabled.\n"));
        return S_FALSE;
    }

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Initializing Profiling Services.\n"));

    // Get the CLSID of the profiler to CoCreate
    NewArrayHolder<WCHAR> wszClsid(NULL);
    NewArrayHolder<WCHAR> wszProfilerDLL(NULL);

#ifdef FEATURE_CORECLR
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER, &wszClsid));

#if defined(_TARGET_X86_)
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH_32, &wszProfilerDLL));
#elif defined(_TARGET_AMD64_)
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH_64, &wszProfilerDLL));
#endif
    if(wszProfilerDLL == NULL)
    {
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH, &wszProfilerDLL));
    }
#else // FEATURE_CORECLR
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_COR_PROFILER, &wszClsid));
    
#if defined(_TARGET_X86_)
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_COR_PROFILER_PATH_32, &wszProfilerDLL));
#elif defined(_TARGET_AMD64_)
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_COR_PROFILER_PATH_64, &wszProfilerDLL));
#endif 
    if(wszProfilerDLL == NULL)
    {    
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_COR_PROFILER_PATH, &wszProfilerDLL));
    }
#endif // FEATURE_CORECLR
    
    // If the environment variable doesn't exist, profiling is not enabled.
    if (wszClsid == NULL)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling flag set, but required "
             "environment variable does not exist.\n"));

        LogProfError(IDS_E_PROF_NO_CLSID);

        return S_FALSE;
    }

    if ((wszProfilerDLL != NULL) && (wcslen(wszProfilerDLL) >= MAX_LONGPATH))
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling flag set, but COR_PROFILER_PATH was not set properly.\n"));

        LogProfError(IDS_E_PROF_BAD_PATH);

        return S_FALSE;
    }
    
#ifdef FEATURE_PAL
    // If the environment variable doesn't exist, profiling is not enabled.
    if (wszProfilerDLL == NULL)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling flag set, but required "
             "environment variable does not exist.\n"));

        LogProfError(IDS_E_PROF_BAD_PATH);

        return S_FALSE;
    }
#endif // FEATURE_PAL
    
    CLSID clsid;
    hr = ProfilingAPIUtility::ProfilerCLSIDFromString(wszClsid, &clsid);
    if (FAILED(hr))
    {
        // ProfilerCLSIDFromString already logged an event if there was a failure
        return hr;
    }

    hr = LoadProfiler(
        kStartupLoad,
        &clsid,
        wszClsid,
        wszProfilerDLL,
        NULL,               // No client data for startup load
        0);                 // No client data for startup load
    if (FAILED(hr))
    {
        // A failure in either the CLR or the profiler prevented it from
        // loading.  Event has been logged.  Propagate hr
        return hr;
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Performs lazy initialization that need not occur on startup, but does need to occur
// before trying to load a profiler.
//
// Return Value:
//    HRESULT indicating success or failure.
//

HRESULT ProfilingAPIUtility::PerformDeferredInit()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    // Initialize internal resources for detaching
    HRESULT hr = ProfilingAPIDetach::Initialize();
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Unable to initialize resources for detaching. hr=0x%x.\n",
            hr));
        return hr;
    }
#endif // FEATURE_PROFAPI_ATTACH_DETACH

    if (s_csStatus == NULL)
    {
        s_csStatus = ClrCreateCriticalSection(
            CrstProfilingAPIStatus, 
            (CrstFlags) (CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN));
        if (s_csStatus == NULL)
        {
            return E_OUTOFMEMORY;
        }
    }

    return S_OK;
}

// ----------------------------------------------------------------------------
// ProfilingAPIUtility::LoadProfiler
//
// Description: 
//    Outermost common code for loading the profiler DLL.  Both startup and attach code
//    paths use this.
//
// Arguments:
//    * loadType - Startup load or attach load?
//    * pClsid - Profiler's CLSID
//    * wszClsid - Profiler's CLSID (or progid) in string form, for event log messages
//    * wszProfilerDLL - Profiler's DLL path
//    * pvClientData - For attach loads, this is the client data the trigger wants to
//        pass to the profiler DLL
//    * cbClientData - For attach loads, size of client data in bytes
//    * dwConcurrentGCWaitTimeoutInMs - Time out for wait operation on concurrent GC. Attach scenario only
//
// Return Value:
//    HRESULT indicating success or failure of the load
//
// Notes:
//    * On failure, this function or a callee will have logged an event
//

// static
HRESULT ProfilingAPIUtility::LoadProfiler(
        LoadType loadType,
        const CLSID * pClsid,
        LPCWSTR wszClsid, 
        LPCWSTR wszProfilerDLL,
        LPVOID pvClientData,
        UINT cbClientData,
        DWORD dwConcurrentGCWaitTimeoutInMs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_ANY;
    }
    CONTRACTL_END;

    if (g_fEEShutDown)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;            
    }
    
    enum ProfilerCompatibilityFlag
    {
        // Default: disable V2 profiler
        kDisableV2Profiler = 0x0,

        // Enable V2 profilers
        kEnableV2Profiler  = 0x1,

        // Disable Profiling
        kPreventLoad       = 0x2,
    };

    ProfilerCompatibilityFlag profilerCompatibilityFlag = kDisableV2Profiler;
    NewArrayHolder<WCHAR> wszProfilerCompatibilitySetting(NULL);

    if (loadType == kStartupLoad)
    {
        CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ProfAPI_ProfilerCompatibilitySetting, &wszProfilerCompatibilitySetting);
        if (wszProfilerCompatibilitySetting != NULL)
        {
            if (SString::_wcsicmp(wszProfilerCompatibilitySetting, W("EnableV2Profiler")) == 0)
            {
                profilerCompatibilityFlag = kEnableV2Profiler;
            }
            else if (SString::_wcsicmp(wszProfilerCompatibilitySetting, W("PreventLoad")) == 0)
            {
                profilerCompatibilityFlag = kPreventLoad;
            }
        }

        if (profilerCompatibilityFlag == kPreventLoad)
        {
            LOG((LF_CORPROF, LL_INFO10, "**PROF: COMPLUS_ProfAPI_ProfilerCompatibilitySetting is set to PreventLoad. "
                 "Profiler will not be loaded.\n"));

            LogProfInfo(IDS_PROF_PROFILER_DISABLED, 
                        CLRConfig::EXTERNAL_ProfAPI_ProfilerCompatibilitySetting.name,
                        wszProfilerCompatibilitySetting.GetValue(), 
                        wszClsid);

            return S_OK;
        }
    }

    HRESULT hr;

    hr = PerformDeferredInit();
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: ProfilingAPIUtility::PerformDeferredInit failed. hr=0x%x.\n",
            hr));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, wszClsid, hr);
        return hr;
    }

    // Valid loadType?
    _ASSERTE((loadType == kStartupLoad) || (loadType == kAttachLoad));

    // If a nonzero client data size is reported, there'd better be client data!
    _ASSERTE((cbClientData == 0) || (pvClientData != NULL));

    // Client data is currently only specified on attach
    _ASSERTE((pvClientData == NULL) || (loadType == kAttachLoad));

    // Don't be telling me to load a profiler if there already is one.
    _ASSERTE(g_profControlBlock.curProfStatus.Get() == kProfStatusNone);

    // Create the ProfToEE interface to provide to the profiling services
    NewHolder<ProfToEEInterfaceImpl> pProfEE(new (nothrow) ProfToEEInterfaceImpl());
    if (pProfEE == NULL)
    {
        LOG((LF_CORPROF, LL_ERROR, "**PROF: Unable to allocate ProfToEEInterfaceImpl.\n"));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, wszClsid, E_OUTOFMEMORY);
        return E_OUTOFMEMORY;
    }

    // Initialize the interface
    hr = pProfEE->Init();
    if (FAILED(hr))
    {
        LOG((LF_CORPROF, LL_ERROR, "**PROF: ProfToEEInterface::Init failed.\n"));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, wszClsid, hr);
        return hr;
    }

    // Provide the newly created and inited interface
    LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling code being provided with EE interface.\n"));

    // Create a new EEToProf object
    NewHolder<EEToProfInterfaceImpl> pEEProf(new (nothrow) EEToProfInterfaceImpl());
    if (pEEProf == NULL)
    {
        LOG((LF_CORPROF, LL_ERROR, "**PROF: Unable to allocate EEToProfInterfaceImpl.\n"));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, wszClsid, E_OUTOFMEMORY);
        return E_OUTOFMEMORY;
    }

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    // We're about to load the profiler, so first make sure we successfully create the
    // DetachThread and abort the load of the profiler if we can't. This ensures we don't
    // load a profiler unless we're prepared to detach it later.
    hr = ProfilingAPIDetach::CreateDetachThread();
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Unable to create DetachThread. hr=0x%x.\n",
            hr));
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_INTERNAL_INIT, wszClsid, hr);
        return hr;
    }
#endif // FEATURE_PROFAPI_ATTACH_DETACH

    // Initialize internal state of our EEToProfInterfaceImpl.  This also loads the
    // profiler itself, but does not yet call its Initalize() callback
    hr = pEEProf->Init(pProfEE, pClsid, wszClsid, wszProfilerDLL, (loadType == kAttachLoad), dwConcurrentGCWaitTimeoutInMs);
    if (FAILED(hr))
    {
        LOG((LF_CORPROF, LL_ERROR, "**PROF: EEToProfInterfaceImpl::Init failed.\n"));
        // EEToProfInterfaceImpl::Init logs an event log error on failure
        return hr;
    }

    // EEToProfInterfaceImpl::Init takes over the ownership of pProfEE when Init succeeds, and 
    // EEToProfInterfaceImpl::~EEToProfInterfaceImpl is responsible for releasing the resource pointed
    // by pProfEE.  Calling SuppressRelease here is necessary to avoid double release that 
    // the resource pointed by pProfEE are released by both pProfEE and pEEProf's destructor.
    pProfEE.SuppressRelease();
    pProfEE = NULL;

    if (loadType == kAttachLoad)  // V4 profiler from attach
    {
        // Profiler must support ICorProfilerCallback3 to be attachable
        if (!pEEProf->IsCallback3Supported())
        {
            LogProfError(IDS_E_PROF_NOT_ATTACHABLE, wszClsid);
            return CORPROF_E_PROFILER_NOT_ATTACHABLE;
        }
    }
    else if (!pEEProf->IsCallback3Supported()) // V2 profiler from startup 
    {
        if (profilerCompatibilityFlag == kDisableV2Profiler)
        {
            LOG((LF_CORPROF, LL_INFO10, "**PROF: COMPLUS_ProfAPI_ProfilerCompatibilitySetting is set to DisableV2Profiler (the default). "
                 "V2 profilers are not allowed, so that the configured V2 profiler is going to be unloaded.\n"));

            LogProfInfo(IDS_PROF_V2PROFILER_DISABLED, wszClsid);
            return S_OK;
        }

        _ASSERTE(profilerCompatibilityFlag == kEnableV2Profiler);

        // To prevent V2 profilers from AV, once a V2 profiler is already loaded by a V2 rutnime in the process, 
        // V4 runtime will not try to load the V2 profiler again.
        if (IsV2RuntimeLoaded())
        {
            LogProfInfo(IDS_PROF_V2PROFILER_ALREADY_LOADED, wszClsid);
            return S_OK;
        }

        LOG((LF_CORPROF, LL_INFO10, "**PROF: COMPLUS_ProfAPI_ProfilerCompatibilitySetting is set to EnableV2Profiler. "
             "The configured V2 profiler is going to be initialized.\n"));

        LogProfInfo(IDS_PROF_V2PROFILER_ENABLED,
                    CLRConfig::EXTERNAL_ProfAPI_ProfilerCompatibilitySetting.name,
                    wszProfilerCompatibilitySetting.GetValue(), 
                    wszClsid);
    }

    _ASSERTE(s_csStatus != NULL);
    {
        // All modification of the profiler's status and
        // g_profControlBlock.pProfInterface need to be serialized against each other,
        // in particular, this code should be serialized against detach and unloading
        // code.
        CRITSEC_Holder csh(s_csStatus);

        // We've successfully allocated and initialized the callback wrapper object and the
        // Info interface implementation objects.  The profiler DLL is therefore also
        // successfully loaded (but not yet Initialized).  Transfer ownership of the 
        // callback wrapper object to globals (thus suppress a release when the local 
        // vars go out of scope).
        //
        // Setting this state now enables us to call into the profiler's Initialize()
        // callback (which we do immediately below), and have it successfully call
        // back into us via the Info interface (ProfToEEInterfaceImpl) to perform its
        // initialization.
        g_profControlBlock.pProfInterface = pEEProf.GetValue();
        pEEProf.SuppressRelease();
        pEEProf = NULL;

        // Set global status to reflect the proper type of Init we're doing (attach vs
        // startup)
        g_profControlBlock.curProfStatus.Set(
            (loadType == kStartupLoad) ?
                kProfStatusInitializingForStartupLoad :
                kProfStatusInitializingForAttachLoad);
    }

    // Now that the profiler is officially loaded and in Init status, call into the
    // profiler's appropriate Initialize() callback. Note that if the profiler fails this
    // call, we should abort the rest of the profiler loading, and reset our state so we
    // appear as if we never attempted to load the profiler.

    if (loadType == kStartupLoad) 
    {
        hr = g_profControlBlock.pProfInterface->Initialize();
    }
    else
    {
        _ASSERTE(loadType == kAttachLoad);
        _ASSERTE(g_profControlBlock.pProfInterface->IsCallback3Supported());
        hr = g_profControlBlock.pProfInterface->InitializeForAttach(pvClientData, cbClientData);
    }

    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_INFO10, 
            "**PROF: Profiler failed its Initialize callback.  hr=0x%x.\n",
            hr));

        // If we timed out due to waiting on concurrent GC to finish, it is very likely this is 
        // the reason InitializeForAttach callback failed even though we cannot be sure and we cannot 
        // cannot assume hr is going to be CORPROF_E_TIMEOUT_WAITING_FOR_CONCURRENT_GC. 
        // The best we can do in this case is to report this failure anyway.
        if (g_profControlBlock.pProfInterface->HasTimedOutWaitingForConcurrentGC())
        {            
            ProfilingAPIUtility::LogProfError(IDS_E_PROF_TIMEOUT_WAITING_FOR_CONCURRENT_GC, dwConcurrentGCWaitTimeoutInMs, wszClsid);
        }
        
        // Check for known failure types, to customize the event we log
        if ((loadType == kAttachLoad) && 
            ((hr == CORPROF_E_PROFILER_NOT_ATTACHABLE) || (hr == E_NOTIMPL)))
        {
            _ASSERTE(g_profControlBlock.pProfInterface->IsCallback3Supported());

            // Profiler supports ICorProfilerCallback3, but explicitly doesn't support
            // Attach loading.  So log specialized event
            LogProfError(IDS_E_PROF_NOT_ATTACHABLE, wszClsid);

            // Normalize (CORPROF_E_PROFILER_NOT_ATTACHABLE || E_NOTIMPL) down to
            // CORPROF_E_PROFILER_NOT_ATTACHABLE
            hr = CORPROF_E_PROFILER_NOT_ATTACHABLE;
        }
        else if (hr == CORPROF_E_PROFILER_CANCEL_ACTIVATION)
        {
            // Profiler didn't encounter a bad error, but is voluntarily choosing not to
            // profile this runtime.  Profilers that need to set system environment
            // variables to be able to profile services may use this HRESULT to avoid
            // profiling all the other managed apps on the box.
            LogProfInfo(IDS_PROF_CANCEL_ACTIVATION, wszClsid);
        }
        else
        {
            LogProfError(IDS_E_PROF_INIT_CALLBACK_FAILED, wszClsid, hr);
        }

        // Profiler failed; reset everything. This will automatically reset
        // g_profControlBlock and will unload the profiler's DLL.
        TerminateProfiling();
        return hr;
    }

#ifdef FEATURE_MULTICOREJIT

    // Disable multicore JIT when profiling is enabled
    if (g_profControlBlock.dwEventMask & COR_PRF_MONITOR_JIT_COMPILATION)
    {
        MulticoreJitManager::DisableMulticoreJit();
    }

#endif

    // Indicate that profiling is properly initialized.  On an attach-load, this will
    // force a FlushStoreBuffers(), which is important for catch-up synchronization (see
    // code:#ProfCatchUpSynchronization)
    g_profControlBlock.curProfStatus.Set(kProfStatusActive);

    LOG((
        LF_CORPROF, 
        LL_INFO10, 
        "**PROF: Profiler successfully loaded and initialized.\n"));

    LogProfInfo(IDS_PROF_LOAD_COMPLETE, wszClsid);

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiler created and enabled.\n"));

    if (loadType == kStartupLoad)
    {
        // For startup profilers only: If the profiler is interested in tracking GC
        // events, then we must disable concurrent GC since concurrent GC can allocate
        // and kill objects without relocating and thus not doing a heap walk.
        if (CORProfilerTrackGC())
        {
            LOG((LF_CORPROF, LL_INFO10, "**PROF: Turning off concurrent GC at startup.\n"));       
            g_pConfig->SetGCconcurrent(0);
            LOG((LF_CORPROF, LL_INFO10, "**PROF: Concurrent GC has been turned off at startup.\n"));       
        }
    }

    if (loadType == kAttachLoad)
    {
        // #ProfCatchUpSynchronization
        // 
        // Now that callbacks are enabled (and all threads are aware), tell an attaching
        // profiler that it's safe to request catchup information.
        //
        // There's a race we're preventing that's worthwhile to spell out. An attaching
        // profiler should be able to get a COMPLETE set of data through the use of
        // callbacks unioned with the use of catch-up enumeration Info functions. To
        // achieve this, we must ensure that there is no "hole"--any new data the
        // profiler seeks must be available from a callback or a catch-up info function
        // (or both, as dupes are ok). That means that:
        // 
        // * callbacks must be enabled on other threads NO LATER THAN the profiler begins
        //     requesting catch-up information on this thread
        //     * Abbreviate: callbacks <= catch-up.
        //         
        // Otherwise, if catch-up < callbacks, then it would be possible to have this:
        // 
        // * catch-up < new data arrives < callbacks.
        //     
        // In this nightmare scenario, the new data would not be accessible from the
        // catch-up calls made by the profiler (cuz the profiler made the calls too
        // early) or the callbacks made into the profiler (cuz the callbacks were enabled
        // too late). That's a hole, and that's bad. So we ensure callbacks <= catch-up
        // by the following order of operations:
        // 
        // * This thread:
        //     * a: Set (volatile) currentProfStatus = kProfStatusActive (done above) and
        //         event mask bits (profiler did this in Initialize() callback above,
        //         when it called SetEventMask)
        //     * b: Flush CPU buffers (done automatically when we set status to
        //         kProfStatusActive)
        //     * c: CLR->Profiler call: ProfilerAttachComplete() (below). Inside this
        //         call:
        //         * Profiler->CLR calls: Catch-up Info functions
        // * Other threads:
        //     * a: New data (thread, JIT info, etc.) is created
        //     * b: This new data is now available to a catch-up Info call
        //     * c: currentProfStatus & event mask bits are accurately visible to thread
        //         in determining whether to make a callback
        //     * d: Read currentProfStatus & event mask bits and make callback
        //         (CLR->Profiler) if necessary
        //         
        // So as long as OtherThreads.c <= ThisThread.c we're ok. This means other
        // threads must be able to get a clean read of the (volatile) currentProfStatus &
        // event mask bits BEFORE this thread calls ProfilerAttachComplete(). Use of the
        // "volatile" keyword ensures that compiler optimizations and (w/ VC2005+
        // compilers) the CPU's instruction reordering optimizations at runtime are
        // disabled enough such that they do not hinder the order above. Use of
        // FlushStoreBuffers() ensures that multiple caches on multiple CPUs do not
        // hinder the order above (by causing other threads to get stale reads of the
        // volatiles).
        // 
        // For more information about catch-up enumerations and exactly which entities,
        // and which stage of loading, are permitted to appear in the enumerations, see
        // code:ProfilerFunctionEnum::Init#ProfilerEnumGeneral

        {
            BEGIN_PIN_PROFILER(CORProfilerPresent());
            g_profControlBlock.pProfInterface->ProfilerAttachComplete();
            END_PIN_PROFILER();
        }
    }
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// This is the top-most level of profiling API teardown, and is called directly by
// EEShutDownHelper() (in ceemain.cpp). This cleans up internal structures relating to
// the Profiling API. If we're not in process teardown, then this also releases the
// profiler COM object and frees the profiler DLL
//

// static
void ProfilingAPIUtility::TerminateProfiling()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (IsAtProcessExit())
    {
        // We're tearing down the process so don't bother trying to clean everything up.
        // There's no reliable way to verify other threads won't be trying to re-enter
        // the profiler anyway, so cleaning up here could cause AVs.
        return;
    }

    _ASSERTE(s_csStatus != NULL);
    {
        // We're modifying status and possibly unloading the profiler DLL below, so
        // serialize this code with any other loading / unloading / detaching code.
        CRITSEC_Holder csh(s_csStatus);


#ifdef FEATURE_PROFAPI_ATTACH_DETACH
        if (ProfilingAPIDetach::GetEEToProfPtr() != NULL)
        {
            // The profiler is still being referenced by
            // ProfilingAPIDetach::s_profilerDetachInfo, so don't try to release and
            // unload it. This can happen if Shutdown and Detach race, and Shutdown wins.
            // For example, we could be called as part of Shutdown, but the profiler
            // called RequestProfilerDetach near shutdown time as well (or even earlier
            // but remains un-evacuated as shutdown begins). Whatever the cause, just
            // don't unload the profiler here (as part of shutdown), and let the Detach
            // Thread deal with it (if it gets the chance).
            // 
            // Note: Since this check occurs inside s_csStatus, we don't have to worry
            // that ProfilingAPIDetach::GetEEToProfPtr() will suddenly change during the
            // code below.
            // 
            // FUTURE: For reattach-with-neutered-profilers feature crew, change the
            // above to scan through list of detaching profilers to make sure none of
            // them give a GetEEToProfPtr() equal to g_profControlBlock.pProfInterface.
            return;
        }

        if (g_profControlBlock.curProfStatus.Get() == kProfStatusActive)
        {
            g_profControlBlock.curProfStatus.Set(kProfStatusDetaching);

            // Profiler was active when TerminateProfiling() was called, so we're unloading
            // it due to shutdown. But other threads may still be trying to enter profiler
            // callbacks (e.g., ClassUnloadStarted() can get called during shutdown). Now
            // that the status has been changed to kProfStatusDetaching, no new threads will
            // attempt to enter the profiler. But use the detach evacuation counters to see
            // if other threads already began to enter the profiler.
            if (!ProfilingAPIDetach::IsProfilerEvacuated())
            {
                // Other threads might be entering the profiler, so just skip cleanup
                return;
            }
        }
#endif // FEATURE_PROFAPI_ATTACH_DETACH

        // If we have a profiler callback wrapper and / or info implementation
        // active, then terminate them.

        if (g_profControlBlock.pProfInterface.Load() != NULL)
        {
            // This destructor takes care of releasing the profiler's ICorProfilerCallback*
            // interface, and unloading the DLL when we're not in process teardown.
            delete g_profControlBlock.pProfInterface;
            g_profControlBlock.pProfInterface.Store(NULL);
        }

        // NOTE: Intentionally not deleting s_pSidBuffer. Doing so can cause annoying races
        // with other threads that lazily create and initialize it when needed. (Example:
        // it's used to fill out the "User" field of profiler event log entries.) Keeping
        // s_pSidBuffer around after a profiler detaches and before a new one attaches
        // consumes a bit more memory unnecessarily, but it'll get paged out if another
        // profiler doesn't attach.

        // NOTE: Similarly, intentionally not destroying / NULLing s_csStatus. If
        // s_csStatus is already initialized, we can reuse it each time we do another
        // attach / detach, so no need to destroy it.

        // If we disabled concurrent GC and somehow failed later during the initialization
        if (g_profControlBlock.fConcurrentGCDisabledForAttach)
        {
            // We know for sure GC has been fully initialized as we've turned off concurrent GC before
            _ASSERTE(IsGarbageCollectorFullyInitialized());
            GCHeap::GetGCHeap()->TemporaryEnableConcurrentGC();
            g_profControlBlock.fConcurrentGCDisabledForAttach = FALSE;
        }

        // #ProfileResetSessionStatus Reset all the status variables that are for the current 
        // profiling attach session.
        // When you are adding new status in g_profControlBlock, you need to think about whether
        // your new status is per-session, or consistent across sessions
        g_profControlBlock.ResetPerSessionStatus();

        g_profControlBlock.curProfStatus.Set(kProfStatusNone);
    }
}

#ifndef FEATURE_PAL

// ----------------------------------------------------------------------------
// ProfilingAPIUtility::GetCurrentProcessUserSid
// 
// Description:
//    Generates a SID of the current user from the current process's token. SID is
//    returned in an [out] param, and is also cached for future use. The SID is used for
//    two purposes: event log entries (for filling out the User field) and the ACL used
//    on the globally named pipe object for attaching profilers.
//    
// Arguments:
//    * ppsid - [out] Generated (or cached) SID
//        
// Return Value:
//    HRESULT indicating success or failure.
//    

// static
HRESULT ProfilingAPIUtility::GetCurrentProcessUserSid(PSID * ppsid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (s_pSidBuffer == NULL)
    {
        HRESULT hr;
        NewHolder<SidBuffer> pSidBuffer(new (nothrow) SidBuffer);
        if (pSidBuffer == NULL)
        {
            return E_OUTOFMEMORY;
        }

        // This gets the SID of the user from the process token
        hr = pSidBuffer->InitFromProcessUserNoThrow(GetCurrentProcessId());
        if (FAILED(hr))
        {
            return hr;
        }

        if (FastInterlockCompareExchangePointer(
            &s_pSidBuffer, 
            pSidBuffer.GetValue(), 
            NULL) == NULL)
        {
            // Lifetime successfully transferred to s_pSidBuffer, so don't delete it here
            pSidBuffer.SuppressRelease();
        }
    }

    _ASSERTE(s_pSidBuffer != NULL);
    _ASSERTE(s_pSidBuffer->GetSid().RawSid() != NULL);
    *ppsid = s_pSidBuffer->GetSid().RawSid();
    return S_OK;
}

#endif // !FEATURE_PAL

#endif // PROFILING_SUPPORTED
