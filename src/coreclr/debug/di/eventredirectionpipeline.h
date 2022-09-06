// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// EventRedirectionPipeline.h
//

//
// defines native pipeline abstraction for debug-support
// for event redirection.
//*****************************************************************************

#ifndef _EVENTREDIRECTION_PIPELINE_
#define _EVENTREDIRECTION_PIPELINE_

#include "nativepipeline.h"

struct RedirectionBlock;
//-----------------------------------------------------------------------------
// For debugging purposes, helper class to allow native debug events to get
// redirected through StrikeRS debugger extension. Only 1 OS debugger can be
// attached to a process. This allows a debugger (such as Windbg) to attach directly
// to the Left-side (and thus be used to debug the left-side). ICorDebug then does a
// "virtual attach" through this pipeline.
//
// If this is a raw native attach, all calls go right through to the native pipeline.
//-----------------------------------------------------------------------------
class EventRedirectionPipeline :
    public INativeEventPipeline
{
public:
    EventRedirectionPipeline();
    ~EventRedirectionPipeline();

    // Returns null if redirection is not enabled, else returns a new redirection pipeline.

    //
    // Implementation of INativeEventPipeline
    //

    // Call to free up the pipeline.
    virtual void Delete();

    // Mark what to do with outstanding debuggees when event thread is killed.
    virtual BOOL DebugSetProcessKillOnExit(bool fKillOnExit);

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
        LPPROCESS_INFORMATION lpProcessInformation);

    // Attach
    virtual HRESULT DebugActiveProcess(MachineInfo machineInfo, const ProcessDescriptor& processDescriptor);

    // Detach
    virtual HRESULT DebugActiveProcessStop(DWORD processId);

    // GEt a debug event
    virtual BOOL WaitForDebugEvent(DEBUG_EVENT * pEvent, DWORD dwTimeout, CordbProcess * pProcess);

    // Continue a debug event received from WaitForDebugEvent
    virtual BOOL ContinueDebugEvent(
      DWORD dwProcessId,
      DWORD dwThreadId,
      DWORD dwContinueStatus
    );

    // Return a handle for the debuggee process.
    virtual HANDLE GetProcessHandle();

    // Terminate the debuggee process.
    virtual BOOL TerminateProcess(UINT32 exitCode);

protected:

    //
    // Following us support for event-redirection.
    //


    // Rediretion block, or NULL if we're using the native pipeline.
    RedirectionBlock * m_pBlock;

    // Initialize configuration values.
    void InitConfiguration();

    HRESULT AttachDebuggerToTarget(LPCWSTR szOptions, DWORD pid);
    void CloseBlock();

    //
    // Configuration information to launch the debugger.
    // These are retrieved via the standard Config helpers.
    //

    // The debugger application to launch. eg:
    //    c:\debuggers_amd64\windbg.exe
    CLRConfigStringHolder m_DebuggerCmd;

    // The common format string for the command line.
    // This will get the following printf args:
    //    int (%d or %x): this process's pid (the ICD Client)
    //    pointer (%p): the address of the control block (m_pBlock). The launched debugger will
    //      then use this to communicate with this process.
    //    extra format string (%s): args specific for either launch or attach
    //    target debuggee (%d or %x): pid of the debuggee.
    // eg (for windbg):
    //   -c ".load C:\vbl\ClrDbg\ndp\clr\src\Tools\strikeRS\objc\amd64\strikeRS.dll; !watch %x %p" %s -p %d
    CLRConfigStringHolder m_CommonParams;

    // Command parameters for create case.
    // Note that we must always physically call CreateProcess on the debuggee so that we get the proper out-parameters
    // from create-process (eg, target's handle, startup info, etc). So we always attach the auxiliary debugger
    // even in the create case. Use "-pr -pb" in Windbg to attach to a create-suspended process.
    //
    // Common Windbg options:
    // -WX disable automatic workspace loading. This guarantees the newly created windbg has a clean
    // environment and is not tainted with settings that will break the extension dll.
    // -pr option will tell real Debugger to resume main thread. This goes with the CREATE_SUSPENDED flag we passed to CreateProcess.
    // -pb option is required when attaching to newly created suspended process. It tells the debugger
    // to not create the break-in thread (which it can't do on a pre-initialized process).
    // eg:
    //  "-WX -pb -pr"
    CLRConfigStringHolder m_CreateParams;

    // command parameters for attach. The WFDE server will send a loader breakpoint.
    // eg:
    //   "-WX"
    CLRConfigStringHolder m_AttachParams;

    DWORD              m_dwProcessId;
};



#endif // _EVENTREDIRECTION_PIPELINE_

