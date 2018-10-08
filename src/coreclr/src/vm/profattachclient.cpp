// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ProfAttachClient.cpp
// 

// 
// Implementation of the AttachProfiler() API, used by CLRProfilingImpl::AttachProfiler.
// 
// CLRProfilingImpl::AttachProfiler (in ndp\clr\src\DLLS\shim\shimapi.cpp) just thunks down
// to mscorwks!AttachProfiler (below), which calls other functions in this file, all of
// which are in mscorwks.dll. The AttachProfiler() API is consumed by trigger processes
// in order to force the runtime of a target process to load a profiler. The prime
// portion of this implementation lives in ProfilingAPIAttachClient, which handles
// opening a client connection to the pipe created by the target profilee, and sending
// requests across that pipe to force the target profilee (which acts as the pipe server)
// to attach a profiler.

// 
// Since these functions are executed by the trigger process, they intentionally seek the
// event and pipe objects by names based on the PID of the target app to profile (which
// is NOT the PID of the current process, as the current process is just the trigger
// process). This implies, for example, that the variable
// ProfilingAPIAttachDetach::s_hAttachEvent is of no use to the current process, as
// s_hAttachEvent is only applicable to the target profilee app's process.
// 
// Most of the contracts in this file follow the lead of default contracts throughout the
// CLR (triggers, throws, etc.). Since AttachProfiler() is called by native code either
// on a native thread created by the trigger process, or via a P/Invoke, these functions
// will all run on threads in MODE_PREEMPTIVE.
//     * MODE_PREEMPTIVE also allows for GetThread() == NULL, which will be the case for
//         a native-only thread calling AttachProfiler()
//         

// ======================================================================================

#include "common.h"

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 
#include "tlhelp32.h"       // For CreateToolhelp32Snapshot, etc. in MightProcessExist()
#include "profilinghelper.h"
#include "profattach.h"
#include "profattach.inl"
#include "profattachclient.h"

// CLRProfilingImpl::AttachProfiler calls this, which itself is just a simple wrapper around
// code:ProfilingAPIAttachClient::AttachProfiler.  See public documentation for a
// description of the parameters, return value, etc.
extern "C" HRESULT STDMETHODCALLTYPE AttachProfiler(
    DWORD dwProfileeProcessID,
    DWORD dwMillisecondsMax,
    const CLSID * pClsidProfiler,
    LPCWSTR wszProfilerPath,
    void * pvClientData,
    UINT cbClientData,
    LPCWSTR wszRuntimeVersion)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;

        // This is the entrypoint into the EE by a trigger process.  As such, this
        // is profiling-specific and not considered mainline EE code.
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;

    EX_TRY
    {
        ProfilingAPIAttachClient attachClient;
        hr = attachClient.AttachProfiler(
            dwProfileeProcessID,
            dwMillisecondsMax,
            pClsidProfiler,
            wszProfilerPath,
            pvClientData,
            cbClientData,
            wszRuntimeVersion);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR(); 
        _ASSERTE(!"Unhandled exception executing AttachProfiler API");
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    // For ease-of-use by profilers, normalize similar HRESULTs down.
    if ((hr == HRESULT_FROM_WIN32(ERROR_BROKEN_PIPE)) ||
        (hr == HRESULT_FROM_WIN32(ERROR_PIPE_NOT_CONNECTED)) ||
        (hr == HRESULT_FROM_WIN32(ERROR_BAD_PIPE)))
    {
        hr = CORPROF_E_IPC_FAILED;
    }

    return hr;
}


// ----------------------------------------------------------------------------
// AdjustRemainingMs
// 
// Description:
//    Simple helper to do timeout arithmetic.  Timeout arithmetic is based on
//    CLRGetTickCount64, which returns an unsigned 64-bit int representing the number of
//    milliseconds transpired since the machine has been up. Since a machine is unlikely
//    to be up for > 500 million years, wraparound issues may be ignored.
//    
//    Caller repeatedly calls this function (usually once before a lenghty operation
//    with a timeout) to check on its remaining time allotment and get alerted when time
//    runs out.
//    
// Arguments:
//    * ui64StartTimeMs - [in] When did caller begin, in tick counts (ms)?
//    * dwMillisecondsMax - [in] How much time does caller have, total?
//    * pdwMillisecondsRemaining - [out] Remaining ms caller has before exceeding its
//        timeout.
//
// Return Value:
//    HRESULT_FROM_WIN32(ERROR_TIMEOUT) if caller is out of time; else S_OK
//    

static HRESULT AdjustRemainingMs(
    ULONGLONG ui64StartTimeMs, 
    DWORD dwMillisecondsMax,
    DWORD * pdwMillisecondsRemaining)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(pdwMillisecondsRemaining != NULL);

    ULONGLONG ui64NowMs = CLRGetTickCount64();

    if (ui64NowMs - ui64StartTimeMs > dwMillisecondsMax)
    {
        // Out of time!
        return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
    }

    // How much of dwMillisecondsMax remain to be used?
    *pdwMillisecondsRemaining = dwMillisecondsMax - static_cast<DWORD>(ui64NowMs - ui64StartTimeMs);
    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachClient::AttachProfiler
// 
// Description:
//    Main worker for AttachProfiler API. Trigger process calls mscoree!AttachProfiler
//    which just defers to this function to do all the work.
//    
//    ** See public API docs for description of params / return value. **
//    
//    Note that, in the trigger process, the dwMillisecondsMax timeouts are cumulative:
//    the caller specifies a single timeout value for the entire AttachProfiler API call.
//    So we must constantly adjust the timeouts we use so they're based on the time
//    remaining from the original dwMillisecondsMax specified by the AttachProfiler API
//    client.
//    

HRESULT ProfilingAPIAttachClient::AttachProfiler(
    DWORD dwProfileeProcessID,
    DWORD dwMillisecondsMax,
    const CLSID * pClsidProfiler,
    LPCWSTR wszProfilerPath,
    void * pvClientData,
    UINT cbClientData,
    LPCWSTR wszRuntimeVersion)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    InitializeLogging();

    HRESULT hr;

    // Is cbClientData just crazy-sick-overflow big?
    if (cbClientData >= 0xFFFFffffUL - sizeof(AttachRequestMessage))
    {
        return E_OUTOFMEMORY;
    }

    if ((pvClientData == NULL) && (cbClientData != 0))
    {
        return E_INVALIDARG;
    }

    if (pClsidProfiler == NULL)
    {
        return E_INVALIDARG;
    }

    if ((wszProfilerPath != NULL) && (wcslen(wszProfilerPath) >= MAX_LONGPATH))
    {
        return E_INVALIDARG;
    }

    // See if we can early-out due to the profilee process ID not existing.
    // MightProcessExist() only returns FALSE if it has positively verified the process
    // ID didn't exist when MightProcessExist() was called. So it might incorrectly
    // return TRUE (if it hit an error trying to determine whether the process exists).
    // But that's ok, as we'll catch a nonexistent process later on when we try to fiddle
    // with its event & pipe. MightProcessExist() is used strictly as an optional
    // optimization to early-out before waiting for the event to appear.
    if (!MightProcessExist(dwProfileeProcessID))
    {
        return CORPROF_E_PROFILEE_PROCESS_NOT_FOUND;
    }

    // Adjust time out value according to env var COMPlus_ProfAPI_AttachProfilerTimeoutInMs
    // The default is 10 seconds as we want to avoid client (trigger process) time out too early 
    // due to wait operation for concurrent GC in the server (profilee side)
    DWORD dwMillisecondsMinFromEnv = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ProfAPI_AttachProfilerMinTimeoutInMs);

    if (dwMillisecondsMax < dwMillisecondsMinFromEnv)
        dwMillisecondsMax = dwMillisecondsMinFromEnv;
    
#ifdef _DEBUG
    {
        WCHAR wszClsidProfiler[40];
        if (!StringFromGUID2(*pClsidProfiler, wszClsidProfiler, _countof(wszClsidProfiler)))
        {
            wcscpy_s(&wszClsidProfiler[0], _countof(wszClsidProfiler), W("(error)"));
        }
        LOG((
            LF_CORPROF, 
            LL_INFO10,
            "**PROF TRIGGER: mscorwks!AttachProfiler invoked with Trigger Process ID: '%d', "
                "Target Profilee Process ID: '%d', dwMillisecondsMax: '%d', pClsidProfiler: '%S',"
                "wszProfilerPath: '%S'\n",
            GetProcessId(GetCurrentProcess()),
            dwProfileeProcessID,
            dwMillisecondsMax,
            wszClsidProfiler,
            wszProfilerPath == NULL ? W("") : wszProfilerPath));
    }
#endif // _DEBUG

    // See code:AdjustRemainingMs
    ULONGLONG ui64StartTimeMs = CLRGetTickCount64();
    DWORD dwMillisecondsRemaining = dwMillisecondsMax;

    HandleHolder hProfileeProcess = ::OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, dwProfileeProcessID);
    if (!hProfileeProcess)
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF TRIGGER: OpenProcess failed. LastError=0x%x.\n",
            ::GetLastError()));
        return HRESULT_FROM_GetLastError();
    }
    
    StackSString attachPipeName;
    ProfilingAPIAttachDetach::GetAttachPipeNameForPidAndVersion(hProfileeProcess, wszRuntimeVersion, &attachPipeName);

    // Try to open pipe with 0ms timeout in case the pipe is still around from
    // a previous attach request
    hr = OpenPipeClient(attachPipeName.GetUnicode(), 0);
    if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
    {
        // Pipe doesn't exist, so signal attach event and retry.  Note that any other
        // failure from the above OpenPipeClient call will NOT cause us to signal
        // the attach event, as signaling the attach event can only help with making
        // sure the pipe gets created, and nothing else.
        StackSString attachEventName;
        ProfilingAPIAttachDetach::GetAttachEventNameForPidAndVersion(hProfileeProcess, wszRuntimeVersion, &attachEventName);
        hr = SignalAttachEvent(attachEventName.GetUnicode());
        if (FAILED(hr))
        {
            LOG((
                LF_CORPROF, 
                LL_ERROR, 
                "**PROF TRIGGER: Unable to signal the global attach event.  hr=0x%x.\n",
                hr));

            // It's reasonable for SignalAttachEvent to err out if the event
            // simply doesn't exist. This happens on server apps that just circumvent
            // using an event. They just create the AttachThread and attach pipe on
            // startup, and are always listening on the pipe. So if event signaling
            // failed due to nonexistent event, keep on going and try connecting to the
            // pipe again. But if event signaling failed for any other reason, that's
            // unexpected so give up.
            if (hr != HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
            {
                return hr;
            }
        }

        hr = AdjustRemainingMs(ui64StartTimeMs, dwMillisecondsMax, &dwMillisecondsRemaining);
        if (FAILED(hr))
        {
            return hr;
        }

        hr = OpenPipeClient(attachPipeName.GetUnicode(), dwMillisecondsRemaining);
    }

    // hr now holds the result of either the original OpenPipeClient call (if it
    // failed for a reason other than ERROR_FILE_NOT_FOUND) or the 2nd
    // OpenPipeClient call (if the first call yielded ERROR_FILE_NOT_FOUND and we
    // signaled the event and retried).
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF TRIGGER: Unable to open a client connection to the pipe.  hr=0x%x.\n",
            hr));
        return hr;
    }

    // At this point the pipe is definitely open
    _ASSERTE(IsValidHandle(m_hPipeClient));

    hr = AdjustRemainingMs(ui64StartTimeMs, dwMillisecondsMax, &dwMillisecondsRemaining);
    if (FAILED(hr))
    {
        return hr;
    }

    // Send the GetVersion message and verify we're talking the same language
    hr = VerifyVersionIsCompatible(dwMillisecondsRemaining);
    if (FAILED(hr))
    {
        return hr;
    }

    hr = AdjustRemainingMs(ui64StartTimeMs, dwMillisecondsMax, &dwMillisecondsRemaining);
    if (FAILED(hr))
    {
        return hr;
    }

    // Send the attach message!
    HRESULT hrAttach;
    hr = SendAttachRequest(
        dwMillisecondsRemaining, 
        pClsidProfiler,
        wszProfilerPath,
        pvClientData,
        cbClientData,
        &hrAttach);
    if (FAILED(hr))
    {
        return hr;
    }

    LOG((
        LF_CORPROF, 
        LL_INFO10,
        "**PROF TRIGGER: AttachProfiler succeeded sending attach request.  Trigger Process ID: '%d', "
        "Target Profilee Process ID: '%d', Attach HRESULT: '0x%x'\n",
        GetProcessId(GetCurrentProcess()),
        dwProfileeProcessID,
        hrAttach));

    return hrAttach;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachClient::MightProcessExist
// 
// Description:
//    Returns BOOL indicating whether a process with the specified process ID might exist
//    on the local computer.
//    
// Arguments:
//    * dwProcessID - Process ID to look up
//        
// Return Value:
//    nonzero if process might possibly exist; FALSE if not
//    
// Notes:
//    * Since processes come and go while this function executes, this should only be
//        used on a process ID that is supposed to exist both before and after this
//        function returns. A return of FALSE reliably tells you that supposition is
//        wrong. A return of TRUE, however, only means the process ID existed when this
//        function did its search. It's still possible the process has exited by the time
//        this function returns.
//    * If this function is unsure of a process's existence (e.g., if it encounters an
//        error while trying to find out), it errs on the side of optimism and returns
//        TRUE.
//        

BOOL ProfilingAPIAttachClient::MightProcessExist(DWORD dwProcessID)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // There are a few ways to check whether a process exists. Some dismissed
    // alternatives:
    // 
    //     * OpenProcess() with a "limited" access right.
    //         * Even relatively limited access rights such as SYNCHRONIZE and
    //             PROCESS_QUERY_INFORMATION often fail with ERROR_ACCESS_DENIED, even if
    //             the caller is running as administrator.
    //             
    //    * EnumProcesses() + search through returned PIDs
    //         * EnumProcesses() requires psychic powers to know how big to allocate the
    //             array of PIDs to receive (EnumProcesses() won't give you a hint if
    //             you're wrong).
    //             
    // Method of choice is CreateToolhelp32Snapshot, which gives an enumerator to iterate
    // through all processes.

    // Take a snapshot of all processes in the system.
    HandleHolder hProcessSnap = CreateToolhelp32Snapshot(
        TH32CS_SNAPPROCESS, 
        0                      // Unused when snap type is TH32CS_SNAPPROCESS
        );
    if (hProcessSnap == INVALID_HANDLE_VALUE)
    {
        // Dunno if process exists.  Err on the side of optimism
        return TRUE;
    }

    // Set the size of the structure before using it.
    PROCESSENTRY32 entry;
    ZeroMemory(&entry, sizeof(entry));
    entry.dwSize = sizeof(PROCESSENTRY32);

    // Start enumeration with Process32First.  It will set dwSize to tell us how many
    // members of PROCESSENTRY32 we can trust.  We only need th32ProcessID
    if (!Process32First(hProcessSnap, &entry) ||
        (offsetof(PROCESSENTRY32, th32ProcessID) + sizeof(entry.th32ProcessID) > entry.dwSize))
    {
        // Can't tell if process exists, so assume it might
        return TRUE;
    }

    do
    {
        if (entry.th32ProcessID == dwProcessID)
        {
            // Definitely exists
            return TRUE;
        }
    } while (Process32Next(hProcessSnap, &entry));

    // Process32Next() failed.  Return FALSE only if we exhausted our search
    return (GetLastError() != ERROR_NO_MORE_FILES);
}



// ----------------------------------------------------------------------------
// ProfilingAPIAttachClient::OpenPipeClient
//
// Description: 
//    Attempts to create a client connection to the remote server pipe
//
// Arguments:
//    * wszPipeName - Name of pipe to connect to.
//    * dwMillisecondsMax - Total ms to spend trying to connect to the pipe.
//
// Return Value:
//    HRESULT indicating success / failure
//

HRESULT ProfilingAPIAttachClient::OpenPipeClient(
    LPCWSTR wszPipeName,
    DWORD dwMillisecondsMax)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    const DWORD kSleepMsUntilRetryCreateFile = 100;
    HRESULT hr;
    DWORD dwErr;

    // See code:AdjustRemainingMs
    ULONGLONG ui64StartTimeMs = CLRGetTickCount64();
    DWORD dwMillisecondsRemaining = dwMillisecondsMax;

    HandleHolder hPipeClient;

    // We need to wait until the pipe is both CREATED (i.e., target profilee app has
    // created the server end of the pipe) and AVAILABLE (i.e., no other trigger has opened
    // the client end to the pipe). There is no Win32 API to wait until the pipe is
    // CREATED, so we must make our own retry loop that calls CreateFileW. Once the pipe
    // is known to be CREATED, we can use WaitNamedPipe to wait until the pipe is
    // AVAILABLE. (Note: It would have been nice if we could use WaitNamedPipe to wait
    // until the pipe is both CREATED and AVAILABLE. But WaitNamedPipe just returns an
    // error immediately if the pipe is not yet CREATED, regardless of the timeout value
    // specified.)
    while (TRUE)
    {
        // This CreateFile call doesn't create the pipe. The pipe must be created by the
        // target profilee. This CreateFile call attempts to open a client connection to
        // the pipe. If CreateFile succeeds, that implies the pipe had already been
        // successfully CREATED by the target profilee, and is AVAILABLE, and we now have
        // a client connection to the pipe ready to go.
        hPipeClient = CreateFileW(
            wszPipeName,
            GENERIC_READ | GENERIC_WRITE,
            0,                      // dwShareMode (i.e., no sharing)
            NULL,                   // lpSecurityAttributes (i.e., handle not inheritable and
                                    //     only current user may access this handle)
            OPEN_EXISTING,          // Only open (don't create) the pipe
            FILE_FLAG_OVERLAPPED,   // Using overlapped I/O allows async ops w/ timeout
            NULL);                  // hTemplateFile

        if (hPipeClient != INVALID_HANDLE_VALUE)
        {
            // CreateFile succeeded!  Pipe is CREATED (by target profilee)
            // and AVAILABLE and we're connected
            break;
        }

        // Opening the pipe failed.  Why?
        dwErr = GetLastError();
        switch(dwErr)
        {
        default:
            // Any error other than the ones specifically brought out below isn't
            // retry-able (e.g., security failure)
            return HRESULT_FROM_WIN32(dwErr);

        case ERROR_FILE_NOT_FOUND:
            // Pipe not CREATED yet.  Can we retry?
            if (dwMillisecondsRemaining <= kSleepMsUntilRetryCreateFile)
            {
                // No time left, gotta bail!
                return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
            }

            // Sleep and retry
            // (bAlertable=FALSE: don't wake up due to overlapped I/O)
            ClrSleepEx(kSleepMsUntilRetryCreateFile, FALSE);
            dwMillisecondsRemaining -= kSleepMsUntilRetryCreateFile;
            break;

        case ERROR_PIPE_BUSY:
            // Pipe CREATED, but it's not AVAILABLE.  Wait until it's AVAILABLE
            
            LOG((
                LF_CORPROF, 
                LL_INFO10, 
                "**PROF TRIGGER: Found pipe, but pipe is busy.  Waiting until pipe is available.\n"));

            hr = AdjustRemainingMs(ui64StartTimeMs, dwMillisecondsMax, &dwMillisecondsRemaining);
            if (FAILED(hr))
            {
                return HRESULT_FROM_WIN32(ERROR_PIPE_BUSY);
            }

            if (!WaitNamedPipeW(wszPipeName, dwMillisecondsRemaining))
            {
                // If we timeout here, convert the error into something more useful
                dwErr = GetLastError();
                if ((dwErr == ERROR_TIMEOUT) || (dwErr == ERROR_SEM_TIMEOUT))
                {
                    return HRESULT_FROM_WIN32(ERROR_PIPE_BUSY);
                }

                // Failed for a reason other timeout.  Send that reason back to caller
                LOG((
                    LF_CORPROF, 
                    LL_ERROR, 
                    "**PROF TRIGGER: WaitNamedPipe failed for a reason other timeout.  hr=0x%x.\n",
                    HRESULT_FROM_WIN32(dwErr)));
                return HRESULT_FROM_WIN32(dwErr);
            }

            // Pipe should be ready to open now, so retry.  Note that it's still
            // possible that another client sneaks in and connects before we get a
            // chance to.  If that happens, CreateFile will fail again, and we'll end up
            // here waiting again (until we timeout).
            break;
        }
    }

    // Only way to exit loop above is if pipe is CREATED and AVAILABLE.
    _ASSERTE(IsValidHandle(hPipeClient));

    // We now have a valid handle on the pipe, which means we're connected
    // to the pipe, and no one else is

    // change to message-read mode. 
    DWORD dwMode = PIPE_READMODE_MESSAGE; 
    if (!SetNamedPipeHandleState( 
        hPipeClient, // pipe handle 
        &dwMode,     // new pipe mode (PIPE_READMODE_MESSAGE)
        NULL,        // lpMaxCollectionCount, must be NULL when client & server on same box
        NULL))       // lpCollectDataTimeout,  must be NULL when client & server on same box
    {
        hr = HRESULT_FROM_GetLastError();
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF TRIGGER: SetNamedPipeHandleState failed.  hr=0x%x.\n",
            hr));
        return hr;
    }

    // Pipe's client handle is now ready for use by this class
    m_hPipeClient = (HANDLE) hPipeClient;

    // Ownership transferred to this class, so this function shouldn't call CloseHandle()
    hPipeClient.SuppressRelease();

    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachClient::SignalAttachEvent
// 
// Description:
//    Trigger process calls this (indirectly via AttachProfiler()) to find, open, and
//    signal the Globally Named Attach Event.
//    
// Arguments:
//    * wszEventName - Name of event to signal
//        
// Return Value:
//    HRESULT indicating success or failure.
//    

HRESULT ProfilingAPIAttachClient::SignalAttachEvent(LPCWSTR wszEventName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HandleHolder hAttachEvent;

    hAttachEvent = OpenEventW(
        EVENT_MODIFY_STATE,     // dwDesiredAccess
        FALSE,                  // bInheritHandle
        wszEventName);
    if (hAttachEvent == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }

    // Dealing directly with Windows event objects, not CLR event cookies, so
    // using Win32 API directly.  Note that none of this code executes on Unix,
    // so the CLR wrapper is of no use to us anyway.
#pragma push_macro("SetEvent")
#undef SetEvent
    if (!SetEvent(hAttachEvent))
#pragma pop_macro("SetEvent")
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachClient::VerifyVersionIsCompatible
//
// Description: 
//    Sends a GetVersion request message across the pipe to the target profilee, reads
//    the response, and determines if the response allows for compatible communication.
//
// Arguments:
//    * dwMillisecondsMax - How much time do we have left to wait for the response?
//
// Return Value:
//    HRESULT indicating success or failure.  If pipe communication succeeds, but we
//    determine that the response doesn't allow for compatible communication, return
//    CORPROF_E_PROFILEE_INCOMPATIBLE_WITH_TRIGGER.
//
// Assumptions:
//    * Client connection should be established before calling this function (or a
//        callee will assert).
//

HRESULT ProfilingAPIAttachClient::VerifyVersionIsCompatible(
    DWORD dwMillisecondsMax)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr;
    DWORD cbReceived;
    GetVersionRequestMessage requestMsg;
    GetVersionResponseMessage responseMsg;

    hr = SendAndReceive(
        dwMillisecondsMax,
        reinterpret_cast<LPVOID>(&requestMsg),
        static_cast<DWORD>(sizeof(requestMsg)),
        reinterpret_cast<LPVOID>(&responseMsg),
        static_cast<DWORD>(sizeof(responseMsg)),
        &cbReceived);
    if (FAILED(hr))
    {
        return hr;
    }

    // Did profilee successfully carry out the GetVersion request?
    if (FAILED(responseMsg.m_hr))
    {
        return responseMsg.m_hr;
    }

    // We should have valid version info for the target profilee.  Now do the
    // comparisons to determine if we're compatible.
    if (
        // Am I too old (i.e., profilee requires a newer trigger)?
        (ProfilingAPIAttachDetach::kCurrentProcessVersion < 
            responseMsg.m_minimumAllowableTriggerVersion) ||

        // Is the profilee too old (i.e., this trigger requires a newer profilee)?
        (responseMsg.m_profileeVersion <
            ProfilingAPIAttachDetach::kMinimumAllowableProfileeVersion))
    {
        return CORPROF_E_PROFILEE_INCOMPATIBLE_WITH_TRIGGER;
    }

    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachClient::SendAttachRequest
// 
// Description:
//    Sends an Attach request message across the pipe to the target profilee, and returns
//    the response.
//    
// Arguments:
//    * dwMillisecondsMax - [in] How much time is left to wait for response?
//    * pClsidProfiler - [in] CLSID of profiler to attach
//    * pvClientData - [in] Client data to pass to profiler's InitializeForAttach
//        callback
//    * cbClientData - [in] Size of client data
//    * phrAttach - [out] Response HRESULT sent back by target profilee
//        
// Return Value:
//    HRESULT indicating success / failure with sending request & receiving response. If
//    S_OK is returned, consult phrAttach to determine success / failure of the actual
//    attach operation.
//    
// Assumptions:
//    * Client connection should be established before calling this function (or a callee
//        will assert).
//        

HRESULT ProfilingAPIAttachClient::SendAttachRequest(
        DWORD dwMillisecondsMax, 
        const CLSID * pClsidProfiler,
        LPCWSTR wszProfilerPath,
        void * pvClientData,
        UINT cbClientData,
        HRESULT * phrAttach)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(phrAttach != NULL);

    // These were already verified early on
    _ASSERTE(cbClientData < 0xFFFFffffUL - sizeof(AttachRequestMessageV2));
    _ASSERTE((pvClientData != NULL) || (cbClientData == 0));

    // Allocate enough space for the message, including the variable-length client data.
    DWORD cbMessage = sizeof(AttachRequestMessageV2) + cbClientData;
    _ASSERTE(cbMessage >= sizeof(AttachRequestMessageV2));
    NewHolder<BYTE> pbMessageStart(new (nothrow) BYTE[cbMessage]);
    if (pbMessageStart == NULL)
    {
        return E_OUTOFMEMORY;
    }

    // Initialize the message.  First the client data at the tail end...
    memcpy(pbMessageStart + sizeof(AttachRequestMessageV2), pvClientData, cbClientData);

    // ...then the message struct fields (use constructor in-place)
    new ((void *) pbMessageStart) AttachRequestMessageV2(
            cbMessage,
            ProfilingAPIAttachDetach::kCurrentProcessVersion,   // Version of the trigger process
            pClsidProfiler,
            wszProfilerPath,
            sizeof(AttachRequestMessageV2),                       // dwClientDataStartOffset
            cbClientData,
            dwMillisecondsMax
            );

    HRESULT hr;
    DWORD cbReceived;
    AttachResponseMessage attachResponseMessage(E_UNEXPECTED);

    hr = SendAndReceive(
        dwMillisecondsMax,
        (LPVOID) pbMessageStart,
        cbMessage,
        reinterpret_cast<LPVOID>(&attachResponseMessage),
        static_cast<DWORD>(sizeof(attachResponseMessage)),
        &cbReceived);
    if (FAILED(hr))
    {
        return hr;
    }

    // Successfully got a response from target.  The response contained the HRESULT
    // indicating whether the attach was successful, so return that HRESULT in the [out]
    // param.
    *phrAttach = attachResponseMessage.m_hr;
    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachClient::SendAndReceive
//
// Description: 
//    Used in trigger process to send a request and receive the response.
//
// Arguments:
//    * dwMillisecondsMax - [in] Timeout for entire send/receive operation
//    * pvInBuffer - [in] Buffer contaning the request message
//    * cbInBuffer - [in] Number of bytes in the request message
//    * pvOutBuffer - [in/out] Buffer to write the response into
//    * cbOutBuffer - [in] Size of the response buffer
//    * pcbReceived - [out] Number of bytes actually written into response buffer
//
// Return Value:
//    HRESULT indicating success or failure
//
// Notes:
//    * The [out] parameters may be written to even if this function fails.  But their
//        contents should be ignored by the caller in this case.
//

HRESULT ProfilingAPIAttachClient::SendAndReceive(
    DWORD dwMillisecondsMax,
    LPVOID pvInBuffer,
    DWORD cbInBuffer,
    LPVOID pvOutBuffer,
    DWORD cbOutBuffer,
    DWORD * pcbReceived)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(m_hPipeClient));
    _ASSERTE(pvInBuffer != NULL);
    _ASSERTE(pvOutBuffer != NULL);
    _ASSERTE(pcbReceived != NULL);

    HRESULT hr;
    DWORD dwErr;
    ProfilingAPIAttachDetach::OverlappedResultHolder overlapped;
    hr = overlapped.Initialize();
    if (FAILED(hr))
    {
        return hr;
    }

    if (TransactNamedPipe(
        m_hPipeClient,
        pvInBuffer,
        cbInBuffer,
        pvOutBuffer,
        cbOutBuffer,
        pcbReceived,
        overlapped))
    {
        // Hot dog!  Send and receive succeeded immediately!  All done.
        return S_OK;
    }

    dwErr = GetLastError();
    if (dwErr != ERROR_IO_PENDING)
    {
        // An unexpected error.  Caller has to deal with it
        hr = HRESULT_FROM_WIN32(dwErr);
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF TRIGGER: TransactNamedPipe failed.  hr=0x%x.\n",
            hr));
        return hr;
    }

    // Typical case=ERROR_IO_PENDING: TransactNamedPipe has begun the transaction, and
    // it's still in progress. Wait until it's done (or timeout expires).
    hr = overlapped.Wait(
        dwMillisecondsMax,
        m_hPipeClient,
        pcbReceived);
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF TRIGGER: Waiting for overlapped result for TransactNamedPipe failed.  hr=0x%x.\n",
            hr));
        return hr;
    }

    return S_OK;
}

#endif // FEATURE_PROFAPI_ATTACH_DETACH 
