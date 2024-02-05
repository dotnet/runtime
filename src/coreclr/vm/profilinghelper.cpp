// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
//        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
//        g_profControlBlock.pProfInterface->AppDomainCreationStarted(MyAppDomainID);
//        END_PROFILER_CALLBACK();
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
// evacuation counters work, see code:ProfilingAPIUtility::IsProfilerEvacuated.
//

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


#ifdef FEATURE_PROFAPI_ATTACH_DETACH
#include "profdetach.h"
#endif // FEATURE_PROFAPI_ATTACH_DETACH

#include "utilcode.h"

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
            _ASSERTE((newProfStatus == kProfStatusPreInitialize) ||
                (newProfStatus == kProfStatusInitializingForStartupLoad) ||
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

        case kProfStatusPreInitialize:
            _ASSERTE((newProfStatus == kProfStatusNone) ||
                (newProfStatus == kProfStatusInitializingForStartupLoad) ||
                (newProfStatus == kProfStatusInitializingForAttachLoad));
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

    StackSString supplementaryInformationUtf8;
    supplementaryInformation.ConvertToUTF8(supplementaryInformationUtf8);

    pString->AppendUTF8("  ");
    pString->AppendPrintf(
        supplementaryInformationUtf8.GetUTF8(),
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

    StackSString messageFromResource;
    if (!messageFromResource.LoadResource(
        CCompRC::Debugging,
        iStringResourceID
        ))
    {
        // Resource not found; should never happen.
        return;
    }

    StackSString messageFromResourceUtf8;
    messageFromResource.ConvertToUTF8(messageFromResourceUtf8);

    StackSString messageToLog;
    messageToLog.VPrintf(messageFromResourceUtf8.GetUTF8(), insertionArgs);

    AppendSupplementaryInformation(iStringResourceID, &messageToLog);

    if (EventEnabledProfilerMessage())
    {
        StackSString messageToLogUtf16;
        messageToLog.ConvertToUnicode(messageToLogUtf16);

        // Write to ETW and EventPipe with the message
        FireEtwProfilerMessage(GetClrInstanceId(), messageToLogUtf16.GetUnicode());
    }

    // Output debug strings for diagnostic messages.
    OutputDebugStringUtf8(messageToLog.GetUTF8());
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

    va_list insertionArgs;
    va_start(insertionArgs, iStringResourceID);
    LogProfEventVA(
        iStringResourceID,
        EVENTLOG_INFORMATION_TYPE,
        insertionArgs);
    va_end(insertionArgs);
}

#ifdef PROF_TEST_ONLY_FORCE_ELT
// Special forward-declarations of the profiling API's slow-path enter/leave/tailcall
// hooks. These need to be forward-declared here so that they may be referenced in
// InitializeProfiling() below solely for the debug-only, test-only code to allow
// enter/leave/tailcall to be turned on at startup without a profiler. See
// code:ProfControlBlock#TestOnlyELT
EXTERN_C void STDMETHODCALLTYPE ProfileEnterNaked(UINT_PTR clientData);
EXTERN_C void STDMETHODCALLTYPE ProfileLeaveNaked(UINT_PTR clientData);
EXTERN_C void STDMETHODCALLTYPE ProfileTailcallNaked(UINT_PTR clientData);
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

    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableDiagnostics) == 0)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling disabled via EnableDiagnostics config.\n"));

        return S_OK;
    }
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableDiagnostics_Profiler) == 0)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling disabled via EnableDiagnostics_Profiler config.\n"));

        return S_OK;
    }


    AttemptLoadProfilerForStartup();
    AttemptLoadDelayedStartupProfilers();
    AttemptLoadProfilerList();

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
    // Test-only, debug-only code to allow attaching profilers to call ICorProfilerInfo interface,
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
        hr = LPCWSTRToGuid(wszClsid, pClsid) ? S_OK : E_FAIL;
    }
    else
    {
#ifndef TARGET_UNIX
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

#else // !TARGET_UNIX
        // ProgID not supported on TARGET_UNIX
        hr = E_INVALIDARG;
#endif // !TARGET_UNIX
    }

    if (FAILED(hr))
    {
        MAKE_UTF8PTR_FROMWIDE(badClsid, wszClsid);
        LOG((
            LF_CORPROF,
            LL_INFO10,
            "**PROF: Invalid CLSID or ProgID (%s).  hr=0x%x.\n",
            badClsid,
            hr));

        ProfilingAPIUtility::LogProfError(IDS_E_PROF_BAD_CLSID, badClsid, hr);
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

    fProfEnabled = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_ENABLE_PROFILING);

    NewArrayHolder<WCHAR> wszClsid(NULL);
    NewArrayHolder<WCHAR> wszProfilerDLL(NULL);
    CLSID clsid;

    if (fProfEnabled == 0)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling not enabled.\n"));
        return S_FALSE;
    }

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Initializing Profiling Services.\n"));

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER, &wszClsid));

#if defined(TARGET_ARM64)
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH_ARM64, &wszProfilerDLL));
#elif defined(TARGET_ARM)
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH_ARM32, &wszProfilerDLL));
#endif
    if(wszProfilerDLL == NULL)
    {
#ifdef TARGET_64BIT
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH_64, &wszProfilerDLL));
#else
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH_32, &wszProfilerDLL));
#endif
        if(wszProfilerDLL == NULL)
        {
            IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_PROFILER_PATH, &wszProfilerDLL));
        }
    }

    // If the environment variable doesn't exist, profiling is not enabled.
    if (wszClsid == NULL)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling flag set, but required "
             "environment variable does not exist.\n"));

        LogProfError(IDS_E_PROF_NO_CLSID);

        return S_FALSE;
    }

    if ((wszProfilerDLL != NULL) && (u16_strlen(wszProfilerDLL) >= MAX_LONGPATH))
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling flag set, but CORECLR_PROFILER_PATH was not set properly.\n"));

        LogProfError(IDS_E_PROF_BAD_PATH);

        return S_FALSE;
    }

#ifdef TARGET_UNIX
    // If the environment variable doesn't exist, profiling is not enabled.
    if (wszProfilerDLL == NULL)
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling flag set, but required "
             "environment variable does not exist.\n"));

        LogProfError(IDS_E_PROF_BAD_PATH);

        return S_FALSE;
    }
#endif // TARGET_UNIX

    hr = ProfilingAPIUtility::ProfilerCLSIDFromString(wszClsid, &clsid);
    if (FAILED(hr))
    {
        // ProfilerCLSIDFromString already logged an event if there was a failure
        return hr;
    }

    char clsidUtf8[GUID_STR_BUFFER_LEN];
    GuidToLPSTR(clsid, clsidUtf8);
    hr = LoadProfiler(
        kStartupLoad,
        &clsid,
        (LPCSTR)clsidUtf8,
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

//static
HRESULT ProfilingAPIUtility::AttemptLoadDelayedStartupProfilers()
{
    if (g_profControlBlock.storedProfilers.IsEmpty())
    {
        return S_OK;
    }

    HRESULT storedHr = S_OK;
    STOREDPROFILERLIST *profilers = &g_profControlBlock.storedProfilers;
    for (StoredProfilerNode* item = profilers->GetHead(); item != NULL; item = STOREDPROFILERLIST::GetNext(item))
    {
        LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiler loading from GUID/Path stored from the IPC channel."));
        CLSID *pClsid = &(item->guid);

        char clsidUtf8[GUID_STR_BUFFER_LEN];
        GuidToLPSTR(*pClsid, clsidUtf8);
        HRESULT hr = LoadProfiler(
            kStartupLoad,
            pClsid,
            (LPCSTR)clsidUtf8,
            item->path.GetUnicode(),
            NULL,               // No client data for startup load
            0);                 // No client data for startup load
        if (FAILED(hr))
        {
            // LoadProfiler logs if there is an error
            storedHr = hr;
        }
    }

    return storedHr;
}

// static
HRESULT ProfilingAPIUtility::AttemptLoadProfilerList()
{
    HRESULT hr = S_OK;
    CLRConfigStringHolder wszProfilerList(NULL);

#if defined(TARGET_ARM64)
    CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_ARM64, &wszProfilerList);
#elif defined(TARGET_ARM)
    CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_ARM32, &wszProfilerList);
#endif
    if (wszProfilerList == NULL)
    {
#ifdef TARGET_64BIT
        CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_64, &wszProfilerList);
#else
        CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_32, &wszProfilerList);
#endif
        if (wszProfilerList == NULL)
        {
            CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_NOTIFICATION_PROFILERS, &wszProfilerList);
            if (wszProfilerList == NULL)
            {
                // No profiler list specified, bail
                return S_OK;
            }
        }
    }

    DWORD dwEnabled = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CORECLR_ENABLE_NOTIFICATION_PROFILERS);
    if (dwEnabled == 0)
    {
        // Profiler list explicitly disabled, bail
        LogProfInfo(IDS_E_PROF_NOTIFICATION_DISABLED);
        return S_OK;
    }

    SString profilerList{wszProfilerList};

    HRESULT storedHr = S_OK;
    for (SString::Iterator sectionStart = profilerList.Begin(), sectionEnd = profilerList.Begin();
        profilerList.Find(sectionEnd, W(';'));
        sectionStart = ++sectionEnd)
    {
        SString::Iterator pathEnd = sectionStart;
        if (!profilerList.Find(pathEnd, W('=')) || pathEnd > sectionEnd)
        {
            ProfilingAPIUtility::LogProfError(IDS_E_PROF_BAD_PATH);
            storedHr = E_FAIL;
            continue;
        }
        SString::Iterator clsidStart = pathEnd + 1;
        profilerList.Replace(pathEnd, W('\0'));
        profilerList.Replace(sectionEnd, W('\0'));
        
        SString clsidString{profilerList.GetUnicode(clsidStart)};
        NewArrayHolder<WCHAR> clsidStringRaw = clsidString.GetCopyOfUnicodeString();
        CLSID clsid;
        hr = ProfilingAPIUtility::ProfilerCLSIDFromString(clsidStringRaw, &clsid);
        if (FAILED(hr))
        {
            // ProfilerCLSIDFromString already logged an event if there was a failure
            storedHr = hr;
            continue;
        }

        char clsidUtf8[GUID_STR_BUFFER_LEN];
        GuidToLPSTR(clsid, clsidUtf8);
        hr = LoadProfiler(
            kStartupLoad,
            &clsid,
            (LPCSTR)clsidUtf8,
            profilerList.GetUnicode(sectionStart),
            NULL,               // No client data for startup load
            0);                 // No client data for startup load
        if (FAILED(hr))
        {
            // LoadProfiler already logged if there was an error
            storedHr = hr;
            continue;
        }
    }

    return storedHr;
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

// static
HRESULT ProfilingAPIUtility::DoPreInitialization(
        EEToProfInterfaceImpl *pEEProf,
        const CLSID *pClsid,
        LPCSTR szClsid,
        LPCWSTR wszProfilerDLL,
        LoadType loadType,
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
        PRECONDITION(pEEProf != NULL);
        PRECONDITION(pClsid != NULL);
        PRECONDITION(szClsid != NULL);
    }
    CONTRACTL_END;

    _ASSERTE(s_csStatus != NULL);

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
            LOG((LF_CORPROF, LL_INFO10, "**PROF: DOTNET_ProfAPI_ProfilerCompatibilitySetting is set to PreventLoad. "
                 "Profiler will not be loaded.\n"));

            MAKE_UTF8PTR_FROMWIDE(szEnvVarName, CLRConfig::EXTERNAL_ProfAPI_ProfilerCompatibilitySetting.name);
            MAKE_UTF8PTR_FROMWIDE(szEnvVarValue, wszProfilerCompatibilitySetting.GetValue());
            LogProfInfo(IDS_PROF_PROFILER_DISABLED,
                        szEnvVarName,
                        szEnvVarValue,
                        szClsid);

            return S_OK;
        }
    }

    HRESULT hr = S_OK;

    NewHolder<ProfToEEInterfaceImpl> pProfEE(new (nothrow) ProfToEEInterfaceImpl());
    if (pProfEE == NULL)
    {
        LOG((LF_CORPROF, LL_ERROR, "**PROF: Unable to allocate ProfToEEInterfaceImpl.\n"));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, szClsid, E_OUTOFMEMORY);
        return E_OUTOFMEMORY;
    }

    // Initialize the interface
    hr = pProfEE->Init();
    if (FAILED(hr))
    {
        LOG((LF_CORPROF, LL_ERROR, "**PROF: ProfToEEInterface::Init failed.\n"));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, szClsid, hr);
        return hr;
    }

    // Provide the newly created and inited interface
    LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiling code being provided with EE interface.\n"));

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
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_INTERNAL_INIT, szClsid, hr);
        return hr;
    }
#endif // FEATURE_PROFAPI_ATTACH_DETACH

    // Initialize internal state of our EEToProfInterfaceImpl.  This also loads the
    // profiler itself, but does not yet call its Initialize() callback
    hr = pEEProf->Init(pProfEE, pClsid, szClsid, wszProfilerDLL, (loadType == kAttachLoad), dwConcurrentGCWaitTimeoutInMs);
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
            LogProfError(IDS_E_PROF_NOT_ATTACHABLE, szClsid);
            return CORPROF_E_PROFILER_NOT_ATTACHABLE;
        }
    }
    else if (!pEEProf->IsCallback3Supported()) // V2 profiler from startup
    {
        if (profilerCompatibilityFlag == kDisableV2Profiler)
        {
            LOG((LF_CORPROF, LL_INFO10, "**PROF: DOTNET_ProfAPI_ProfilerCompatibilitySetting is set to DisableV2Profiler (the default). "
                 "V2 profilers are not allowed, so that the configured V2 profiler is going to be unloaded.\n"));

            LogProfInfo(IDS_PROF_V2PROFILER_DISABLED, szClsid);
            return S_OK;
        }

        _ASSERTE(profilerCompatibilityFlag == kEnableV2Profiler);

        LOG((LF_CORPROF, LL_INFO10, "**PROF: DOTNET_ProfAPI_ProfilerCompatibilitySetting is set to EnableV2Profiler. "
             "The configured V2 profiler is going to be initialized.\n"));

        MAKE_UTF8PTR_FROMWIDE(szEnvVarName, CLRConfig::EXTERNAL_ProfAPI_ProfilerCompatibilitySetting.name);
        MAKE_UTF8PTR_FROMWIDE(szEnvVarValue, wszProfilerCompatibilitySetting.GetValue());
        LogProfInfo(IDS_PROF_V2PROFILER_ENABLED,
                    szEnvVarName,
                    szEnvVarValue,
                    szClsid);
    }

    return hr;
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
//    * szClsid - Profiler's CLSID (or progid) in string form, for event log messages
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
        LPCSTR szClsid,
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

    // Valid loadType?
    _ASSERTE((loadType == kStartupLoad) || (loadType == kAttachLoad));

    // If a nonzero client data size is reported, there'd better be client data!
    _ASSERTE((cbClientData == 0) || (pvClientData != NULL));

    // Client data is currently only specified on attach
    _ASSERTE((pvClientData == NULL) || (loadType == kAttachLoad));

    ProfilerInfo profilerInfo;
    profilerInfo.Init();
    profilerInfo.inUse = TRUE;

    HRESULT hr = PerformDeferredInit();
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF,
            LL_ERROR,
            "**PROF: ProfilingAPIUtility::PerformDeferredInit failed. hr=0x%x.\n",
            hr));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, szClsid, hr);
        return hr;
    }

    {
        // Usually we need to take the lock when modifying profiler status, but at this
        // point no one else could have a pointer to this ProfilerInfo so we don't
        // need to synchronize. Once we store it in g_profControlBlock we need to.
        profilerInfo.curProfStatus.Set(kProfStatusPreInitialize);
    }

    NewHolder<EEToProfInterfaceImpl> pEEProf(new (nothrow) EEToProfInterfaceImpl());
    if (pEEProf == NULL)
    {
        LOG((LF_CORPROF, LL_ERROR, "**PROF: Unable to allocate EEToProfInterfaceImpl.\n"));
        LogProfError(IDS_E_PROF_INTERNAL_INIT, szClsid, E_OUTOFMEMORY);
        return E_OUTOFMEMORY;
    }

    // Create the ProfToEE interface to provide to the profiling services
    hr = DoPreInitialization(pEEProf, pClsid, szClsid, wszProfilerDLL, loadType, dwConcurrentGCWaitTimeoutInMs);
    if (FAILED(hr))
    {
        return hr;
    }

    {
        // Usually we need to take the lock when modifying profiler status, but at this
        // point no one else could have a pointer to this ProfilerInfo so we don't
        // need to synchronize. Once we store it in g_profControlBlock we need to.

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
        profilerInfo.pProfInterface = pEEProf.GetValue();
        pEEProf.SuppressRelease();
        pEEProf = NULL;

        // Set global status to reflect the proper type of Init we're doing (attach vs
        // startup)
        profilerInfo.curProfStatus.Set(
            (loadType == kStartupLoad) ?
                kProfStatusInitializingForStartupLoad :
                kProfStatusInitializingForAttachLoad);
    }

    ProfilerInfo *pProfilerInfo = NULL;
    {
        // Now we register the profiler, from this point on we need to worry about
        // synchronization
        CRITSEC_Holder csh(s_csStatus);

        // Check if this profiler is notification only and load as appropriate
        BOOL notificationOnly = FALSE;
        {
            HRESULT callHr = profilerInfo.pProfInterface->LoadAsNotificationOnly(&notificationOnly);
            if (FAILED(callHr))
            {
                notificationOnly = FALSE;
            }
        }

        if (notificationOnly)
        {
            pProfilerInfo = g_profControlBlock.FindNextFreeProfilerInfoSlot();
            if (pProfilerInfo == NULL)
            {
                LogProfError(IDS_E_PROF_NOTIFICATION_LIMIT_EXCEEDED);
                return CORPROF_E_PROFILER_ALREADY_ACTIVE;
            }
        }
        else
        {
            // "main" profiler, there can only be one
            if (g_profControlBlock.mainProfilerInfo.curProfStatus.Get() != kProfStatusNone)
            {
                LogProfError(IDS_PROF_ALREADY_LOADED);
                return CORPROF_E_PROFILER_ALREADY_ACTIVE;
            }

            // This profiler cannot be a notification only profiler
            pProfilerInfo = &(g_profControlBlock.mainProfilerInfo);
        }

        pProfilerInfo->curProfStatus.Set(profilerInfo.curProfStatus.Get());
        pProfilerInfo->pProfInterface = profilerInfo.pProfInterface;
        pProfilerInfo->pProfInterface->SetProfilerInfo(pProfilerInfo);
        pProfilerInfo->inUse = TRUE;
    }

    // Now that the profiler is officially loaded and in Init status, call into the
    // profiler's appropriate Initialize() callback. Note that if the profiler fails this
    // call, we should abort the rest of the profiler loading, and reset our state so we
    // appear as if we never attempted to load the profiler.

    if (loadType == kStartupLoad)
    {
        // This EvacuationCounterHolder is just to make asserts in EEToProfInterfaceImpl happy.
        // Using it like this without the dirty read/evac counter increment/clean read pattern
        // is not safe generally, but in this specific case we can skip all that since we haven't
        // published it yet, so we are the only thread that can access it.
        EvacuationCounterHolder holder(pProfilerInfo);
        hr = pProfilerInfo->pProfInterface->Initialize();
    }
    else
    {
        // This EvacuationCounterHolder is just to make asserts in EEToProfInterfaceImpl happy.
        // Using it like this without the dirty read/evac counter increment/clean read pattern
        // is not safe generally, but in this specific case we can skip all that since we haven't
        // published it yet, so we are the only thread that can access it.
        EvacuationCounterHolder holder(pProfilerInfo);

        _ASSERTE(loadType == kAttachLoad);
        _ASSERTE(pProfilerInfo->pProfInterface->IsCallback3Supported());

        hr = pProfilerInfo->pProfInterface->InitializeForAttach(pvClientData, cbClientData);
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
        if (pProfilerInfo->pProfInterface->HasTimedOutWaitingForConcurrentGC())
        {
            ProfilingAPIUtility::LogProfError(IDS_E_PROF_TIMEOUT_WAITING_FOR_CONCURRENT_GC, dwConcurrentGCWaitTimeoutInMs, szClsid);
        }

        // Check for known failure types, to customize the event we log
        if ((loadType == kAttachLoad) &&
            ((hr == CORPROF_E_PROFILER_NOT_ATTACHABLE) || (hr == E_NOTIMPL)))
        {
            _ASSERTE(pProfilerInfo->pProfInterface->IsCallback3Supported());

            // Profiler supports ICorProfilerCallback3, but explicitly doesn't support
            // Attach loading.  So log specialized event
            LogProfError(IDS_E_PROF_NOT_ATTACHABLE, szClsid);

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
            LogProfInfo(IDS_PROF_CANCEL_ACTIVATION, szClsid);
        }
        else
        {
            LogProfError(IDS_E_PROF_INIT_CALLBACK_FAILED, szClsid, hr);
        }

        // Profiler failed; reset everything. This will automatically reset
        // g_profControlBlock and will unload the profiler's DLL.
        TerminateProfiling(pProfilerInfo);
        return hr;
    }

#ifdef FEATURE_MULTICOREJIT

    // Disable multicore JIT when profiling is enabled
    if (pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_JIT_COMPILATION))
    {
        MulticoreJitManager::DisableMulticoreJit();
    }

#endif

    // Indicate that profiling is properly initialized.  On an attach-load, this will
    // force a FlushStoreBuffers(), which is important for catch-up synchronization (see
    // code:#ProfCatchUpSynchronization)
    pProfilerInfo->curProfStatus.Set(kProfStatusActive);

    LOG((
        LF_CORPROF,
        LL_INFO10,
        "**PROF: Profiler successfully loaded and initialized.\n"));

    LogProfInfo(IDS_PROF_LOAD_COMPLETE, szClsid);

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Profiler created and enabled.\n"));

    if (loadType == kStartupLoad)
    {
        // For startup profilers only: If the profiler is interested in tracking GC
        // events, then we must disable concurrent GC since concurrent GC can allocate
        // and kill objects without relocating and thus not doing a heap walk.
        if (pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_GC))
        {
            LOG((LF_CORPROF, LL_INFO10, "**PROF: Turning off concurrent GC at startup.\n"));
            // Previously we would use SetGCConcurrent(0) to indicate to the GC that it shouldn't even
            // attempt to use concurrent GC. The standalone GC feature create a cycle during startup,
            // where the profiler couldn't set startup flags for the GC. To overcome this, we call
            // TempraryDisableConcurrentGC and never enable it again. This has a perf cost, since the
            // GC will create concurrent GC data structures, but it is acceptable in the context of
            // this kind of profiling.
            GCHeapUtilities::GetGCHeap()->TemporaryDisableConcurrentGC();
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
            // This EvacuationCounterHolder is just to make asserts in EEToProfInterfaceImpl happy.
            // Using it like this without the dirty read/evac counter increment/clean read pattern
            // is not safe generally, but in this specific case we can skip all that since we haven't
            // published it yet, so we are the only thread that can access it.
            EvacuationCounterHolder holder(pProfilerInfo);
            pProfilerInfo->pProfInterface->ProfilerAttachComplete();
        }
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Performs the evacuation checks by grabbing the thread store lock, iterating through
// all EE Threads, and querying each one's evacuation counter.  If they're all 0, the
// profiler is ready to be unloaded.
//
// Return Value:
//    Nonzero iff the profiler is fully evacuated and ready to be unloaded.
//

// static
BOOL ProfilingAPIUtility::IsProfilerEvacuated(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(pProfilerInfo->curProfStatus.Get() == kProfStatusDetaching);

    // Note that threads are still in motion as we check its evacuation counter.
    // This is ok, because we've already changed the profiler status to
    // kProfStatusDetaching and flushed CPU buffers. So at this point the counter
    // will typically only go down to 0 (and not increment anymore), with one
    // small exception (below). So if we get a read of 0 below, the counter will
    // typically stay there. Specifically:
    //     * Profiler is most likely not about to increment its evacuation counter
    //         from 0 to 1 because pThread sees that the status is
    //         kProfStatusDetaching.
    //     * Note that there is a small race where pThread might actually
    //         increment its evac counter from 0 to 1 (if it dirty-read the
    //         profiler status a tad too early), but that implies that when
    //         pThread rechecks the profiler status (clean read) then pThread
    //         will immediately decrement the evac counter back to 0 and avoid
    //         calling into the EEToProfInterfaceImpl pointer.
    //
    // (see
    // code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
    // for details)Doing this under the thread store lock not only ensures we can
    // iterate through the Thread objects safely, but also forces us to serialize with
    // the GC. The latter is important, as server GC enters the profiler on non-EE
    // Threads, and so no evacuation counters might be incremented during server GC even
    // though control could be entering the profiler.
    {
        ThreadStoreLockHolder TSLockHolder;

        Thread * pThread = ThreadStore::GetAllThreadList(
            NULL,   // cursor thread; always NULL to begin with
            0,      // mask to AND with Thread::m_State to filter returned threads
            0);     // bits to match the result of the above AND.  (m_State & 0 == 0,
                    // so we won't filter out any threads)

        // Note that, by not filtering out any of the threads, we're intentionally including
        // stuff like TS_Dead or TS_Unstarted.  But that keeps us on the safe
        // side.  If an EE Thread object exists, we want to check its counters to be
        // absolutely certain it isn't executing in a profiler.

        while (pThread != NULL)
        {
            // Note that pThread is still in motion as we check its evacuation counter.
            // This is ok, because we've already changed the profiler status to
            // kProfStatusDetaching and flushed CPU buffers. So at this point the counter
            // will typically only go down to 0 (and not increment anymore), with one
            // small exception (below). So if we get a read of 0 below, the counter will
            // typically stay there. Specifically:
            //     * pThread is most likely not about to increment its evacuation counter
            //         from 0 to 1 because pThread sees that the status is
            //         kProfStatusDetaching.
            //     * Note that there is a small race where pThread might actually
            //         increment its evac counter from 0 to 1 (if it dirty-read the
            //         profiler status a tad too early), but that implies that when
            //         pThread rechecks the profiler status (clean read) then pThread
            //         will immediately decrement the evac counter back to 0 and avoid
            //         calling into the EEToProfInterfaceImpl pointer.
            //
            // (see
            // code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
            // for details)
            DWORD dwEvacCounter = pThread->GetProfilerEvacuationCounter(pProfilerInfo->slot);
            if (dwEvacCounter != 0)
            {
                LOG((
                    LF_CORPROF,
                    LL_INFO100,
                    "**PROF: Profiler not yet evacuated because OS Thread ID 0x%x has evac counter of %d (decimal).\n",
                    pThread->GetOSThreadId(),
                    dwEvacCounter));
                return FALSE;
            }

            pThread = ThreadStore::GetAllThreadList(pThread, 0, 0);
        }
    }

    // FUTURE: When rejit feature crew complete, add code to verify all rejitted
    // functions are fully reverted and off of all stacks.  If this is very easy to
    // verify (e.g., checking a single value), consider putting it above the loop
    // above so we can early-out quicker if rejitted code is still around.

    // We got this far without returning, so the profiler is fully evacuated
    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// This is the top-most level of profiling API teardown, and is called directly by
// EEShutDownHelper() (in ceemain.cpp). This cleans up internal structures relating to
// the Profiling API. If we're not in process teardown, then this also releases the
// profiler COM object and frees the profiler DLL
//

// static
void ProfilingAPIUtility::TerminateProfiling(ProfilerInfo *pProfilerInfo)
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
        if (pProfilerInfo->curProfStatus.Get() == kProfStatusDetaching && pProfilerInfo->pProfInterface.Load() != NULL)
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
#endif // FEATURE_PROFAPI_ATTACH_DETACH

        if (pProfilerInfo->curProfStatus.Get() == kProfStatusActive)
        {
            pProfilerInfo->curProfStatus.Set(kProfStatusDetaching);

            // Profiler was active when TerminateProfiling() was called, so we're unloading
            // it due to shutdown. But other threads may still be trying to enter profiler
            // callbacks (e.g., ClassUnloadStarted() can get called during shutdown). Now
            // that the status has been changed to kProfStatusDetaching, no new threads will
            // attempt to enter the profiler. But use the detach evacuation counters to see
            // if other threads already began to enter the profiler.
            if (!ProfilingAPIUtility::IsProfilerEvacuated(pProfilerInfo))
            {
                // Other threads might be entering the profiler, so just skip cleanup
                return;
            }
        }


        // If we have a profiler callback wrapper and / or info implementation
        // active, then terminate them.

        if (pProfilerInfo->pProfInterface.Load() != NULL)
        {
            // This destructor takes care of releasing the profiler's ICorProfilerCallback*
            // interface, and unloading the DLL when we're not in process teardown.
            delete pProfilerInfo->pProfInterface;
            pProfilerInfo->pProfInterface.Store(NULL);
        }

        // NOTE: Intentionally not destroying / NULLing s_csStatus. If
        // s_csStatus is already initialized, we can reuse it each time we do another
        // attach / detach, so no need to destroy it.

        // Attach/Load/Detach are all synchronized with the Status Crst, don't need to worry about races
        // If we disabled concurrent GC and somehow failed later during the initialization
        if (g_profControlBlock.fConcurrentGCDisabledForAttach.Load() && g_profControlBlock.IsMainProfiler(pProfilerInfo->pProfInterface))
        {
            g_profControlBlock.fConcurrentGCDisabledForAttach = FALSE;

            // We know for sure GC has been fully initialized as we've turned off concurrent GC before
            _ASSERTE(IsGarbageCollectorFullyInitialized());
            GCHeapUtilities::GetGCHeap()->TemporaryEnableConcurrentGC();
        }

        // #ProfileResetSessionStatus Reset all the status variables that are for the current
        // profiling attach session.
        // When you are adding new status in g_profControlBlock, you need to think about whether
        // your new status is per-session, or consistent across sessions
        pProfilerInfo->ResetPerSessionStatus();

        pProfilerInfo->curProfStatus.Set(kProfStatusNone);

        g_profControlBlock.DeRegisterProfilerInfo(pProfilerInfo);

        g_profControlBlock.UpdateGlobalEventMask();
    }
}

#endif // PROFILING_SUPPORTED
