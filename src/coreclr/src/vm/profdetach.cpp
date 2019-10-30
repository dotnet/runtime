// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ProfDetach.cpp
// 

//
// Implementation of helper classes and structures used for Profiling API Detaching
//
// ======================================================================================

#include "common.h"

#ifdef FEATURE_PROFAPI_ATTACH_DETACH

#include "profdetach.h"
#include "profilinghelper.h"
#include "profilinghelper.inl"
#include "eetoprofinterfaceimpl.inl"

// Class static member variables
ProfilerDetachInfo ProfilingAPIDetach::s_profilerDetachInfo;
CLREvent           ProfilingAPIDetach::s_eventDetachWorkAvailable;


// ---------------------------------------------------------------------------------------
// ProfilerDetachInfo constructor
//
// Description:
//    Set every member variable to NULL or 0.  They'll get initialized to real values
//    in ProfilingAPIDetach::RequestProfilerDetach.
//    

ProfilerDetachInfo::ProfilerDetachInfo()
{
    // Executed during construction of a global object, therefore we cannot
    // use real contracts, as this requires that utilcode has been initialized.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    Init();
}

void ProfilerDetachInfo::Init()
{
    // Executed during construction of a global object, therefore we cannot
    // use real contracts, as this requires that utilcode has been initialized.
    STATIC_CONTRACT_LEAF;
    
    m_pEEToProf = NULL;
    m_ui64DetachStartTime = 0;
    m_dwExpectedCompletionMilliseconds = 0;
}


// ----------------------------------------------------------------------------
// Implementation of ProfilingAPIAttachDetach statics


// ----------------------------------------------------------------------------
// ProfilingAPIDetach::Initialize
// 
// Description:
//    Initialize static event

// static 
HRESULT ProfilingAPIDetach::Initialize()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (!s_eventDetachWorkAvailable.IsValid())
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            s_eventDetachWorkAvailable.CreateAutoEvent(FALSE);
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
            if (SUCCEEDED(hr))
            {
                // For exceptions that give us useless hr's, just use E_FAIL
                hr = E_FAIL;
            }
        }
        EX_END_CATCH(RethrowTerminalExceptions)

        if (FAILED(hr))
        {
            return hr;
        }
    }

    return S_OK;
}



// ----------------------------------------------------------------------------
// ProfilingAPIDetach::RequestProfilerDetach
// 
// Description:
//    Initialize ProfilerDetachInfo structures with parameters passed from
//    ICorProfilerInfo3::RequestProfilerDetach
//    
// Arguments:
//    * dwExpectedCompletionMilliseconds - A hint to the CLR as to how long it should
//        wait before checking to see if execution has evacuated the profiler and all
//        profiler-instrumented code. If this is 0, the CLR will select a default.
//      
// Notes:
// 
//    Invariants maintained by profiler:
//    * Before calling RequestProfilerDetach, the profiler must turn off all hijacking.
//    * If RequestProfilerDetach is called from a thread created by the CLR (i.e., from
//        within a callback), the profiler must first have exited all threads of its own
//        creation
//    * If RequestProfilerDetach is called from a thread of the profiler's own creation,
//        then
//        * The profiler must first have exited all OTHER threads of its own creation,
//            AND
//        * The profiler must immediately call FreeLibraryAndExitThread() after
//            RequestProfilerDetach returns.
//
//    The above invariants result in the following possiblities:
//        * RequestProfilerDetach() may be called multi-threaded, but only from within
//            profiler callbacks. As such, evacuation counters will have been incremented
//            before entry into RequestProfilerDetach(), so the DetachThread will be
//            blocked until all such threads have returned from RequestProfilerDetach and
//            the callback from which RequestProfilerDetach was called. OR
//        * RequestProfilerDetach() is called single-threaded, from a thread of the
//            profiler's creation, which promises not to make any more calls into the CLR
//            afterward. In this case, the DetachThread will be blocked until
//            RequestProfilerDetach signals s_eventDetachWorkAvailable at the end.
//            

// static
HRESULT ProfilingAPIDetach::RequestProfilerDetach(DWORD dwExpectedCompletionMilliseconds)
{
    CONTRACTL
    {
        NOTHROW;
        // Crst is used so GC may be triggered
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        // Crst is used to synchronize the initialization of ProfilingAPIDetach internal structure
        CAN_TAKE_LOCK;
        PRECONDITION(ProfilingAPIUtility::GetStatusCrst() != NULL);
        PRECONDITION(s_eventDetachWorkAvailable.IsValid());
    }
    CONTRACTL_END;

    // Runtime must be fully started, or else CpuStoreBufferControl used below may not
    // be initialized yet.
    if (!g_fEEStarted)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;
    }

    if (dwExpectedCompletionMilliseconds == 0)
    {
        // Pick suitable default if the profiler just leaves this at 0.  5 seconds is
        // reasonable.
        dwExpectedCompletionMilliseconds = 5000;
    }

    {
        CRITSEC_Holder csh(ProfilingAPIUtility::GetStatusCrst());

        // return immediately if detach is in progress

        if (s_profilerDetachInfo.m_pEEToProf != NULL)
        {
            return CORPROF_E_PROFILER_DETACHING;
        }

        ProfilerStatus curProfStatus = g_profControlBlock.curProfStatus.Get();

        if ((curProfStatus == kProfStatusInitializingForStartupLoad) ||
            (curProfStatus == kProfStatusInitializingForAttachLoad))
        {
            return CORPROF_E_PROFILER_NOT_YET_INITIALIZED;
        }

        if (curProfStatus != kProfStatusActive)
        {
            // Before we acquired the lock, someone else must have unloaded the profiler
            // for us (e.g., shutdown or the DetachThread in response to a prior
            // RequestProfilerDetach call).
            return CORPROF_E_PROFILER_DETACHING;
        }

        EEToProfInterfaceImpl * pEEToProf = g_profControlBlock.pProfInterface;

        // Since prof status was active after entering the lock, the profiler must not
        // have unloaded out from under us.
        _ASSERTE(pEEToProf != NULL);

        if (!pEEToProf->IsCallback3Supported())
        {
            return CORPROF_E_CALLBACK3_REQUIRED;
        }

        // Did the profiler do anything immutable?  That will prevent us from allowing it to
        // detach.
        HRESULT hr = pEEToProf->EnsureProfilerDetachable();
        if (FAILED(hr))
        {
            return hr;
        }
        s_profilerDetachInfo.m_pEEToProf = pEEToProf;
        s_profilerDetachInfo.m_ui64DetachStartTime = CLRGetTickCount64();
        s_profilerDetachInfo.m_dwExpectedCompletionMilliseconds = dwExpectedCompletionMilliseconds;

        // Ok, time to seal the profiler from receiving or making calls with the CLR.
        // (This will force a FlushStoreBuffers().)
        g_profControlBlock.curProfStatus.Set(kProfStatusDetaching);
    }

    // Sealing done. Wake up the DetachThread so it can loop until the profiler code is
    // fully evacuated off of all stacks.
    if (!s_eventDetachWorkAvailable.Set())
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // FUTURE: Currently, kProfStatusDetaching prevents callbacks from being sent to the
    // profiler AND prevents another profiler from attaching. In the future, when
    // implementing the reattach-with-neutered-profilers feature crew, we may want to add
    // another block here to call ProfilingAPIUtility::SetProfStatus(kProfStatusNone), so callbacks are
    // prevented but a new profiler may attempt to attach.

    EX_TRY
    {
        ProfilingAPIUtility::LogProfInfo(IDS_PROF_DETACH_INITIATED);
    }
    EX_CATCH
    {
        // Oh well, rest of detach succeeded, so we should still return success to the
        // profiler.
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// This is where the DetachThread spends its life.  This waits until there's a profiler
// to detach, then loops until the profiler code is completely evacuated off all stacks.
// This will then unload the profiler.
//

// static
void ProfilingAPIDetach::ExecuteEvacuationLoop()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Wait until there's a profiler to detach (or until this thread should "wake up"
    // for some other reason, such as exiting due to an unsuccessful startup-load of a
    // profiler).
    DWORD dwRet = s_eventDetachWorkAvailable.Wait(INFINITE, FALSE /* alertable */);
    if (dwRet != WAIT_OBJECT_0)
    {
        // The wait ended due to a failure or a reason other than the event getting
        // signaled (e.g., WAIT_ABANDONED)
        DWORD dwErr;
        if (dwRet == WAIT_FAILED)
        {
            dwErr = GetLastError();
            LOG((
                LF_CORPROF, 
                LL_ERROR, 
                "**PROF: DetachThread wait for s_eventDetachWorkAvailable failed with GetLastError = %d.\n",
                dwErr));
        }
        else
        {
            dwErr = dwRet;      // No extra error info available beyond the return code
            LOG((
                LF_CORPROF, 
                LL_ERROR, 
                "**PROF: DetachThread wait for s_eventDetachWorkAvailable terminated with %d.\n",
                dwErr));
        }

        ProfilingAPIUtility::LogProfError(IDS_PROF_DETACH_THREAD_ERROR, dwErr);
        return;
    }

    // Peek to make sure there's actually a profiler to detach
    {
        CRITSEC_Holder csh(ProfilingAPIUtility::GetStatusCrst());

        if (s_profilerDetachInfo.m_pEEToProf == NULL)
        {
            // Nothing to detach.  This can happen if the DetachThread (i.e., current
            // thread) was created but then the profiler failed to load.
            return;
        }
    }

    do
    {
        // Give profiler a chance to return from its procs
        SleepWhileProfilerEvacuates();
    }
    while (!ProfilingAPIUtility::IsProfilerEvacuated());

    UnloadProfiler();
}

//---------------------------------------------------------------------------------------
//
// This is called in between evacuation counter checks.  This calculates how long to
// sleep, and then sleeps.
//

// static
void ProfilingAPIDetach::SleepWhileProfilerEvacuates()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Don't want to check evacuation any more frequently than every 300ms
    const DWORD kdwDefaultMinSleepMs = 300;

    // The default "steady state" max sleep is how long we'll wait if, after a couple
    // tries the profiler still hasn't evacuated. Default to every 10 minutes
    const DWORD kdwDefaultMaxSleepMs = 600000;

    static DWORD s_dwMinSleepMs = 0;
    static DWORD s_dwMaxSleepMs = 0;

    // First time through, initialize the static min / max sleep times.  Normally, we'll
    // just use the constants above, but the user may customize these (within reason).
    
    // They should either both be uninitialized or both initialized
    _ASSERTE(
        ((s_dwMinSleepMs == 0) && (s_dwMaxSleepMs == 0)) ||
        ((s_dwMinSleepMs != 0) && (s_dwMaxSleepMs != 0)));
    
    if (s_dwMaxSleepMs == 0)
    {
        // No race here, since only the DetachThread runs this code
        
        s_dwMinSleepMs = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ProfAPI_DetachMinSleepMs);
        s_dwMaxSleepMs = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ProfAPI_DetachMaxSleepMs);

        // Here's the "within reason" part:  the user may not customize these values to
        // be more "extreme" than the constants, or to be 0 (which would confuse the
        // issue of whether these statics were intialized yet).
        if ((s_dwMinSleepMs < kdwDefaultMinSleepMs) || (s_dwMinSleepMs > kdwDefaultMaxSleepMs))
        {
            // Sleeping less than 300ms between evac checks could negatively affect the
            // app by having the DetachThread execute too often.  And a min sleep time
            // that's too high could result in a profiler hanging around way too long
            // when it's actually ready to be unloaded.
            s_dwMinSleepMs = kdwDefaultMinSleepMs;
        }
        if ((s_dwMaxSleepMs < kdwDefaultMinSleepMs) || (s_dwMaxSleepMs > kdwDefaultMaxSleepMs))
        {
            // A steady state that's too small would retry the evac checks too often on
            // an ongoing basis.  A steady state that's too high could result in a
            // profiler hanging around way too long when it's actually ready to be
            // unloaded.
            s_dwMaxSleepMs = kdwDefaultMaxSleepMs;
        }
    }

    // Take note of when the detach was requested and how long to sleep for
    ULONGLONG ui64ExpectedCompletionMilliseconds;
    ULONGLONG ui64DetachStartTime;
    {
        CRITSEC_Holder csh(ProfilingAPIUtility::GetStatusCrst());

        _ASSERTE(s_profilerDetachInfo.m_pEEToProf != NULL);
        ui64ExpectedCompletionMilliseconds = s_profilerDetachInfo.m_dwExpectedCompletionMilliseconds;
        ui64DetachStartTime = s_profilerDetachInfo.m_ui64DetachStartTime;
    }

    // ui64SleepMilliseconds is calculated to ensure that CLR checks evacuation status roughly:
    //     * After profiler's ui64ExpectedCompletionMilliseconds hint has elapsed (but not
    //         too soon)
    //     * At least once more after 2*ui64ExpectedCompletionMilliseconds have elapsed
    //         (but not too soon)
    //     * Occasionally thereafter (steady state)

    ULONGLONG ui64ElapsedMilliseconds = CLRGetTickCount64() - ui64DetachStartTime;
    ULONGLONG ui64SleepMilliseconds;
    if (ui64ExpectedCompletionMilliseconds > ui64ElapsedMilliseconds)
    {
        // Haven't hit ui64ExpectedCompletionMilliseconds yet, so sleep for remainder
        ui64SleepMilliseconds = ui64ExpectedCompletionMilliseconds - ui64ElapsedMilliseconds;
    }
    else if ((2*ui64ExpectedCompletionMilliseconds) > ui64ElapsedMilliseconds)
    {
        // We're between ui64ExpectedCompletionMilliseconds &
        // 2*ui64ExpectedCompletionMilliseconds, so sleep until
        // 2*ui64ExpectedCompletionMilliseconds have transpired
        ui64SleepMilliseconds = (2*ui64ExpectedCompletionMilliseconds) - ui64ElapsedMilliseconds;
    }
    else
    {
        // Steady state
        ui64SleepMilliseconds = s_dwMaxSleepMs;
    }

    // ...but keep it in bounds!
    ui64SleepMilliseconds = min(
        max(ui64SleepMilliseconds, s_dwMinSleepMs), 
        s_dwMaxSleepMs);

    // At this point it's safe to cast ui64SleepMilliseconds down to a DWORD since we
    // know it's between s_dwMinSleepMs & s_dwMaxSleepMs
    _ASSERTE(ui64SleepMilliseconds <= 0xFFFFffff);
    ClrSleepEx((DWORD) ui64SleepMilliseconds, FALSE /* alertable */);
}



// ---------------------------------------------------------------------------------------
// After we've verified a detaching profiler has fully evacuated, call this to unload the
// profiler and clean up state.
//
// Assumptions:
//     Since this is called well after the profiler called RequestProfilerDetach, the
//     profiler must not have any other threads in use. Also, now that the profiler has
//     been evacuated, no CLR threads will be calling into the profiler (thus the
//     profiler will not gain control via CLR threads either). That means the profiler
//     may not call back into the CLR on any other threads.
//

// static
void ProfilingAPIDetach::UnloadProfiler()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(g_profControlBlock.curProfStatus.Get() == kProfStatusDetaching);

    {
        CRITSEC_Holder csh(ProfilingAPIUtility::GetStatusCrst());

        // Notify profiler it's about to be unloaded
        _ASSERTE(s_profilerDetachInfo.m_pEEToProf != NULL);
        s_profilerDetachInfo.m_pEEToProf->ProfilerDetachSucceeded();

        // Reset detach state.
        s_profilerDetachInfo.Init();

        // This deletes the EEToProfInterfaceImpl object managing the detaching profiler,
        // releases the profiler's callback interfaces, unloads the profiler DLL, sets
        // the status to kProfStatusNone, and resets g_profControlBlock for use next time
        // a profiler tries to attach.
        // 
        // Note that s_profilerDetachInfo.Init() has already NULL'd out
        // s_profilerDetachInfo.m_pEEToProf, so we won't have a dangling pointer to the
        // EEToProfInterfaceImpl that's about to be destroyed.
        ProfilingAPIUtility::TerminateProfiling();
    }

    ProfilingAPIUtility::LogProfInfo(IDS_PROF_DETACH_COMPLETE);
}

// ----------------------------------------------------------------------------
// ProfilingAPIDetach::ProfilingAPIDetachThreadStart
//
// Description:
//    Thread proc for DetachThread. Serves as a simple try/catch wrapper around a call to
//    ProfilingAPIDetach::ExecuteEvacuationLoop.  This thread proc is specified by
//    code:ProfilingAPIDetach::CreateDetachThread when it spins up the new DetachThread.
//    This occurs when a profiler is either startup-loaded or attach-loaded.
//
// Arguments:
//    * LPVOID thread proc param is ignored
//
// Return Value:
//    Just returns 0 always.
//

// static
DWORD WINAPI ProfilingAPIDetach::ProfilingAPIDetachThreadStart(LPVOID)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // At start of this thread, set its type so SOS !threads and anyone else knows who we
    // are.
    ClrFlsSetThreadType(ThreadType_ProfAPI_Detach);

    LOG((
        LF_CORPROF, 
        LL_INFO10, 
        "**PROF: DetachThread created and executing.\n"));

    // This try block is a last-ditch stop-gap to prevent an unhandled exception on the
    // DetachThread from bringing down the process.  Note that if the unhandled
    // exception is a terminal one, then hey, sure, let's tear everything down.  Also
    // note that any naughtiness in the profiler (e.g., throwing an exception from its
    // Initialize callback) should already be handled before we pop back to here, so this
    // is just being super paranoid.
    EX_TRY
    {
        // Don't care about return value, thread proc will just return 0 regardless
        ExecuteEvacuationLoop();
    }
    EX_CATCH
    {
        _ASSERTE(!"Unhandled exception on profiling API detach thread");
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    LOG((
        LF_CORPROF, 
        LL_INFO10, 
        "**PROF: DetachThread exiting.\n"));

    return 0;
}

// ---------------------------------------------------------------------------------------
// Called during startup or attach load of a profiler to create a new thread to fill the role of
// the DetachThread.
//

// static
HRESULT ProfilingAPIDetach::CreateDetachThread()
{
    // This function is practically a leaf (though not quite), so keeping the contract
    // strict to allow for maximum flexibility on when this may called.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // FUTURE: When reattach with neutered profilers is implemented, this
    // function should check if a DetachThread already exists (use synchronization
    // to prevent race), and just return if so.

    HandleHolder hDetachThread;

    // The DetachThread is intentionally not an EE Thread-object thread (it won't
    // execute managed code).
    hDetachThread = ::CreateThread(
        NULL,       // lpThreadAttributes; don't want child processes inheriting this handle
        0,          // dwStackSize (0 = use default)
        ProfilingAPIDetachThreadStart,
        NULL,       // lpParameter (none to pass)
        0,          // dwCreationFlags (0 = use default flags, start thread immediately)
        NULL        // lpThreadId (don't need therad ID)
        );
    if (hDetachThread == NULL)
    {
        DWORD dwErr = GetLastError();
        
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Failed to create DetachThread.  GetLastError=%d.\n",
            dwErr));

        return HRESULT_FROM_WIN32(dwErr);
    }

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Accessor for ProfilingAPIDetach::s_profilerDetachInfo.m_pEEToProf, which is the
// profiler being detached (or NULL if no profiler is being detached).
//
// Return Value:
//      EEToProfInterfaceImpl * for the profiler being detached.
//

// static
EEToProfInterfaceImpl * ProfilingAPIDetach::GetEEToProfPtr()
{
    LIMITED_METHOD_CONTRACT;
    return s_profilerDetachInfo.m_pEEToProf;
}

#endif // FEATURE_PROFAPI_ATTACH_DETACH
