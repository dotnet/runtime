// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// NativePipeline.h
//

//
// defines native pipeline abstraction, which includes debug-support
// for event redirection.
//*****************************************************************************


#ifndef _NATIVE_PIPELINE_H
#define _NATIVE_PIPELINE_H

//-----------------------------------------------------------------------------
// Interface for native-debugging pipeline associated with a single process
// that is being debugged.
//
// On windows, this is a wrapper around the win32 debugging API
// (eg, kernel32!WaitForDebugEvent). On most Unix-like platforms,
// it has an alternative implementation. See code:IEventChannel and
// platformspecific.cpp for more information.
// @dbgtodo : All of the APIs that return BOOL should probably be changed to
// return HRESULTS so we don't have to rely on some implicit GetLastError protocol.
//-----------------------------------------------------------------------------
class INativeEventPipeline
{
public:
    // Call to delete the pipeline. This can only be called once.
    virtual void Delete() = 0;


    //
    // set whether to kill outstanding debuggees when the debugger exits.
    //
    // Arguments:
    //    fKillOnExit - When the debugger thread (this thread) exits, outstanding debuggees will be
    //         terminated (if true), else detached (if false)
    //
    // Returns:
    //    True on success, False on failure.
    //
    // Notes:
    //    This is a cross-platform wrapper around Kernel32!DebugSetProcessKillOnExit.
    //    This affects all debuggees handled by this thread.
    //    This is not supported or necessary for Mac debugging.  The only reason we need this on Windows is to
    //    ask the OS not to terminate the debuggee when the debugger exits.  The Mac debugging pipeline
    //    doesn't automatically kill the debuggee when the debugger exits.
    //

    virtual BOOL DebugSetProcessKillOnExit(bool fKillOnExit) = 0;

    // Create
    virtual HRESULT CreateProcessUnderDebugger(
        MachineInfo machineInfo,
        LPCWSTR lpApplicationName,
        LPCWSTR lpCommandLine,
        LPSECURITY_ATTRIBUTES lpProcessAttributes,
        LPSECURITY_ATTRIBUTES lpThreadAttributes,
        BOOL bInheritHandles,
        DWORD dwCreationFlags,
        LPVOID lpEnvironment,
        LPCWSTR lpCurrentDirectory,
        LPSTARTUPINFOW lpStartupInfo,
        LPPROCESS_INFORMATION lpProcessInformation) = 0;

    // Attach
    virtual HRESULT DebugActiveProcess(MachineInfo machineInfo, const ProcessDescriptor& processDescriptor) = 0;

    // Detach
    virtual HRESULT DebugActiveProcessStop(DWORD processId) =0;

    //
    // Block and wait for the next debug event from the debuggee process.
    //
    // Arguments:
    //    pEvent    - buffer for the debug event to be returned
    //    dwTimeout - number of milliseconds to wait before timing out
    //    pProcess  - the CordbProcess associated with this pipeline; used to look up a thread ID if necessary
    //
    // Return Value:
    //    TRUE if a debug event is available
    //
    // Notes:
    //    Once a debug event is returned, it is consumed from the pipeline and will not be accessible in the
    //    future.  Caller is responsible for saving the debug event if necessary.
    //

    virtual BOOL WaitForDebugEvent(DEBUG_EVENT * pEvent, DWORD dwTimeout, CordbProcess * pProcess) =0;

    //
    // This is specific to Windows.  When a debug event is sent to the debugger, the debuggee process is
    // suspended.  The debugger must call this function to resume the debuggee process.
    //
    // Arguments:
    //    dwProcessId      - process ID of the debuggee
    //    dwThreadId       - thread ID of the thread which has triggered a debug event before
    //    dwContinueStatus - whether to handle the exception (if any) reported on the specified thread
    //
    // Return Value:
    //    TRUE if successful
    //
    // Notes:
    //    For Mac debugging, the process isn't actually suspended when a debug event is raised.  As such,
    //    this function is a nop for Mac debugging.  See code:Debugger::SendRawEvent.
    //
    //    Of course, this is a semantic difference from Windows.  However, in most cases, the LS suspends
    //    all managed threads by calling code:Debugger::TrapAllRuntimeThreads immediately after raising a
    //    debug event.  The only case where this is not true is code:Debugger::SendCreateProcess, but that
    //    doesn't seem to be a problem at this point.
    //

    virtual BOOL ContinueDebugEvent(
      DWORD dwProcessId,
      DWORD dwThreadId,
      DWORD dwContinueStatus
    ) =0;

    //
    // Return a handle for the debuggee process.
    //
    // Return Value:
    //    handle for the debuggee process (see below)
    //
    // Notes:
    //    Handles are a Windows-specific concept.  For Mac debugging, the handle returned by this function is
    //    only valid for waiting on process termination.  This is ok for now because the only cases where a
    //    real process handle is needed are related to interop-debugging, which isn't supported on the Mac.
    //

    virtual HANDLE GetProcessHandle() = 0;

    //
    // Terminate the debuggee process.
    //
    // Arguments:
    //    exitCode - the exit code for the debuggee process
    //
    // Return Value:
    //    TRUE if successful
    //
    // Notes:
    //    The exit code is ignored for Mac debugging.
    //

    virtual BOOL TerminateProcess(UINT32 exitCode) = 0;

    //
    // Resume any suspended threads in the currend process.
    // This decreases the suspend count of each thread by at most 1.
    // Call multiple times until it returns S_FALSE if you want to really ensure
    // all threads are running.
    //
    // Notes:
    //    On Windows the OS may suspend threads when continuing a 2nd-chance exception.
    //    Call this to get them resumed again.  On other platforms this
    //    will typically be a no-op, so I provide a default implementation to avoid
    //    everyone having to override this.
    //
    // Return Value:
    //    S_OK if at least one thread was resumed from a suspended state
    //    S_FALSE if nothing was done
    //    An error code indicating why we were not able to attempt this

    virtual HRESULT EnsureThreadsRunning()
    {
        return S_FALSE;
    }

#ifdef TARGET_UNIX
    // Used by debugger side (RS) to cleanup the target (LS) named pipes
    // and semaphores when the debugger detects the debuggee process  exited.
    virtual void CleanupTargetProcess()
    {
    }
#endif
};

//
// Helper accessors for manipulating native pipeline.
// These also provide some platform abstractions for DEBUG_EVENT.
//

// Returns process ID that the debug event is on.
DWORD GetProcessId(const DEBUG_EVENT * pEvent);

// Returns Thread ID of the thread that fired the debug event.
DWORD GetThreadId(const DEBUG_EVENT * pEvent);

//
// Determines if this is an exception event.
//
// Arguments:
//    pEvent - [required, in]: debug event to inspect
//    pfFirstChance - [required, out]: set if this is an 1st-chance exception.
//    ppRecord - [required, out]: if this is an exception, pointer into to the exception record.
//        this pointer has the same lifetime semantics as the DEBUG_EVENT (it may
//        likely be a pointer into the debug-event).
//
// Returns:
//    True if this is an exception. Sets outparameters to exception values.
//    Else false.
//
// Notes:
//   Exceptions are spceial because they need to be sent to the CLR for filtering.
BOOL IsExceptionEvent(const DEBUG_EVENT * pEvent, BOOL * pfFirstChance, const EXCEPTION_RECORD ** ppRecord);


//-----------------------------------------------------------------------------
// Allocate and return a pipeline object for this platform
//
// Returns:
//    newly allocated pipeline object. Caller must call Dispose() on it.
INativeEventPipeline * NewPipelineForThisPlatform();

//-----------------------------------------------------------------------------
// Allocate and return a pipeline object for this platform
// Has debug checks (such as for event redirection)
//
// Returns:
//    newly allocated pipeline object. Caller must call Dispose() on it.
INativeEventPipeline * NewPipelineWithDebugChecks();



#endif // _NATIVE_PIPELINE_H

