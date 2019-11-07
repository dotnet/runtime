// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: DbgTransportPipeline.cpp
//

//
// Implements the native pipeline for Mac debugging.
//*****************************************************************************

#include "stdafx.h"
#include "nativepipeline.h"
#include "dbgtransportsession.h"
#include "dbgtransportmanager.h"


DWORD GetProcessId(const DEBUG_EVENT * pEvent)
{
    return pEvent->dwProcessId;
}
DWORD GetThreadId(const DEBUG_EVENT * pEvent)
{
    return pEvent->dwThreadId;
}

// Get exception event
BOOL IsExceptionEvent(const DEBUG_EVENT * pEvent, BOOL * pfFirstChance, const EXCEPTION_RECORD ** ppRecord)
{
    if (pEvent->dwDebugEventCode != EXCEPTION_DEBUG_EVENT)
    {
        *pfFirstChance = FALSE;
        *ppRecord = NULL;
        return FALSE;
    }
    *pfFirstChance = pEvent->u.Exception.dwFirstChance;
    *ppRecord = &(pEvent->u.Exception.ExceptionRecord);
    return TRUE;
}


//---------------------------------------------------------------------------------------
//
// INativeEventPipeline is an abstraction over the Windows native debugging pipeline.  This class is an
// implementation which works over an SSL connection for debugging a target process on a Mac remotely.
// It builds on top of code:DbgTransportTarget (which is a connection to the debugger proxy on the Mac) and
// code:DbgTransportSession (which is a connection to the target process on the Mac).  See
// code:IEventChannel for more information.
//
// Assumptions:
//    This class is NOT thread-safe.  Caller is assumed to have taken the appropriate measures for
//    synchronization.
//

class DbgTransportPipeline :
    public INativeEventPipeline
{
public:
    DbgTransportPipeline()
    {
        m_fRunning   = FALSE;
        m_hProcess   = NULL;
        m_pIPCEvent  = reinterpret_cast<DebuggerIPCEvent * >(m_rgbIPCEventBuffer);
        m_pProxy     = NULL;
        m_pTransport = NULL;
        _ASSERTE(!IsTransportRunning());
    }

    virtual ~DbgTransportPipeline()
    {
        Dispose();
    }

    // Call to free up the pipeline.
    virtual void Delete();

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

    // Block and wait for the next debug event from the debuggee process.
    virtual BOOL WaitForDebugEvent(DEBUG_EVENT * pEvent, DWORD dwTimeout, CordbProcess * pProcess);

    virtual BOOL ContinueDebugEvent(
      DWORD dwProcessId,
      DWORD dwThreadId,
      DWORD dwContinueStatus
    );

    // Return a handle which will be signaled when the debuggee process terminates.
    virtual HANDLE GetProcessHandle();

    // Terminate the debuggee process.
    virtual BOOL TerminateProcess(UINT32 exitCode);

#ifdef FEATURE_PAL
    virtual void CleanupTargetProcess()
    {
        m_pTransport->CleanupTargetProcess();
    }
#endif

private:
    // Return TRUE if the transport is up and runnning
    BOOL IsTransportRunning()
    {
        return m_fRunning;
    };

    // clean up all resources
    void Dispose()
    {
        if (m_hProcess != NULL)
        {
            CloseHandle(m_hProcess);
        }
        m_hProcess = NULL;

        if (m_pTransport)
        {
            if (m_ticket.IsValid())
            {
                m_pTransport->StopUsingAsDebugger(&m_ticket);
            }
            m_pProxy->ReleaseTransport(m_pTransport);
        }
        m_pTransport = NULL;
        m_pProxy = NULL;
    }

    BOOL                  m_fRunning;

    DWORD                 m_dwProcessId;
    // This is actually a handle to an event.  This is only valid for waiting on process termination.
    HANDLE                m_hProcess;

    DbgTransportTarget *  m_pProxy;
    DbgTransportSession * m_pTransport;

    // Any buffer for storing a DebuggerIPCEvent must be at least CorDBIPC_BUFFER_SIZE big.  For simplicity
    // sake I have added an extra field member which points to the buffer.
    DebuggerIPCEvent *    m_pIPCEvent;
    BYTE                  m_rgbIPCEventBuffer[CorDBIPC_BUFFER_SIZE];
    DebugTicket           m_ticket;
};

// Allocate and return a pipeline object for this platform
INativeEventPipeline * NewPipelineForThisPlatform()
{
    return new (nothrow) DbgTransportPipeline();
}

// Call to free up the lpProcessInformationpeline.
void DbgTransportPipeline::Delete()
{
    delete this;
}

// set whether to kill outstanding debuggees when the debugger exits.
BOOL DbgTransportPipeline::DebugSetProcessKillOnExit(bool fKillOnExit)
{
    // This is not supported or necessary for Mac debugging.  The only reason we need this on Windows is to
    // ask the OS not to terminate the debuggee when the debugger exits.  The Mac debugging pipeline doesn't
    // automatically kill the debuggee when the debugger exits.
    return TRUE;
}

// Create an process under the debugger.
HRESULT DbgTransportPipeline::CreateProcessUnderDebugger(
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
    LPPROCESS_INFORMATION lpProcessInformation)
{
    // INativeEventPipeline has a 1:1 relationship with CordbProcess.
    _ASSERTE(!IsTransportRunning());

    // We don't support interop-debugging on the Mac.
    _ASSERTE(!(dwCreationFlags & (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS)));

    // When we're using a transport we can't deal with creating a suspended process (we need the process to
    // startup in order that it can start up a transport thread and reply to our messages).
    _ASSERTE(!(dwCreationFlags & CREATE_SUSPENDED));

    // Connect to the debugger proxy on the remote machine and ask it to create a process for us.
    HRESULT hr  = E_FAIL;

    m_pProxy = g_pDbgTransportTarget;
    hr = m_pProxy->CreateProcess(lpApplicationName,
                                 lpCommandLine,
                                 lpProcessAttributes,
                                 lpThreadAttributes,
                                 bInheritHandles,
                                 dwCreationFlags,
                                 lpEnvironment,
                                 lpCurrentDirectory,
                                 lpStartupInfo,
                                 lpProcessInformation);

    if (SUCCEEDED(hr))
    {
        ProcessDescriptor processDescriptor = ProcessDescriptor::Create(lpProcessInformation->dwProcessId, NULL);

        // Establish a connection to the actual runtime to be debugged.
        hr = m_pProxy->GetTransportForProcess(&processDescriptor,
                                              &m_pTransport,
                                              &m_hProcess);
        if (SUCCEEDED(hr))
        {
            // Wait for the connection to become useable (or time out).
            if (!m_pTransport->WaitForSessionToOpen(10000))
            {
                hr = CORDBG_E_TIMEOUT;
            }
            else
            {
                if (!m_pTransport->UseAsDebugger(&m_ticket))
                {
                    hr = CORDBG_E_DEBUGGER_ALREADY_ATTACHED;
                }
            }
        }
    }

    if (SUCCEEDED(hr))
    {
        _ASSERTE((m_hProcess != NULL) && (m_hProcess != INVALID_HANDLE_VALUE));

        m_dwProcessId = lpProcessInformation->dwProcessId;

        // For Mac remote debugging, we don't actually have a process handle to hand back to the debugger.
        // Instead, we return a handle to an event as the "process handle".  The Win32 event thread also waits
        // on this event handle, and the event will be signaled when the proxy notifies us that the process
        // on the remote machine is terminated.  However, normally the debugger calls CloseHandle() immediately
        // on the "process handle" after CreateProcess() returns.  Doing so causes the Win32 event thread to
        // continue waiting on a closed event handle, and so it will never wake up.
        // (In fact, in Whidbey, we also duplicate the process handle in code:CordbProcess::Init.)
        if (!DuplicateHandle(GetCurrentProcess(),
                             m_hProcess,
                             GetCurrentProcess(),
                             &(lpProcessInformation->hProcess),
                             0,      // ignored since we are going to pass DUPLICATE_SAME_ACCESS
                             FALSE,
                             DUPLICATE_SAME_ACCESS))
        {
            hr = HRESULT_FROM_GetLastError();
        }
    }

    if (SUCCEEDED(hr))
    {
        m_fRunning = TRUE;
    }
    else
    {
        Dispose();
    }

    return hr;
}

// Attach the debugger to this process.
HRESULT DbgTransportPipeline::DebugActiveProcess(MachineInfo machineInfo, const ProcessDescriptor& processDescriptor)
{
    // INativeEventPipeline has a 1:1 relationship with CordbProcess.
    _ASSERTE(!IsTransportRunning());

    HRESULT hr = E_FAIL;

    m_pProxy = g_pDbgTransportTarget;

    // Establish a connection to the actual runtime to be debugged.
    hr = m_pProxy->GetTransportForProcess(&processDescriptor, &m_pTransport, &m_hProcess);
    if (SUCCEEDED(hr))
    {
        // TODO: Pass this timeout as a parameter all the way from debugger
        // Wait for the connection to become useable (or time out).
        if (!m_pTransport->WaitForSessionToOpen(10000))
        {
            hr = CORDBG_E_TIMEOUT;
        }
        else
        {
            if (!m_pTransport->UseAsDebugger(&m_ticket))
            {
                hr = CORDBG_E_DEBUGGER_ALREADY_ATTACHED;
            }
        }
    }

    if (SUCCEEDED(hr))
    {
        m_dwProcessId = processDescriptor.m_Pid;
        m_fRunning = TRUE;
    }
    else
    {
        Dispose();
    }

    return hr;
}

// Detach
HRESULT DbgTransportPipeline::DebugActiveProcessStop(DWORD processId)
{
    // The only way to tell the transport to detach from a process is by shutting it down.
    // That will happen when we neuter the CordbProcess object.
    return E_NOTIMPL;
}

// Block and wait for the next debug event from the debuggee process.
BOOL DbgTransportPipeline::WaitForDebugEvent(DEBUG_EVENT * pEvent, DWORD dwTimeout, CordbProcess * pProcess)
{
    if (!IsTransportRunning())
    {
        return FALSE;
    }

    // We need to wait for a debug event from the transport and the process termination event.
    // On Windows, process termination is communicated via a debug event as well, but that's not true for
    // the Mac debugging transport.
    DWORD cWaitSet = 2;
    HANDLE rghWaitSet[2];
    rghWaitSet[0] = m_pTransport->GetDebugEventReadyEvent();
    rghWaitSet[1] = m_hProcess;

    DWORD dwRet = ::WaitForMultipleObjectsEx(cWaitSet, rghWaitSet, FALSE, dwTimeout, FALSE);

    if (dwRet == WAIT_OBJECT_0)
    {
        // The Mac debugging transport actually transmits IPC events and not debug events.
        // We need to convert the IPC event to a debug event and pass it back to the caller.
        m_pTransport->GetNextEvent(m_pIPCEvent, CorDBIPC_BUFFER_SIZE);

        pEvent->dwProcessId = m_pIPCEvent->processId;
        pEvent->dwThreadId = m_pIPCEvent->threadId;
        _ASSERTE(m_dwProcessId == m_pIPCEvent->processId);

        // The Windows implementation stores the target address of the IPC event in the debug event.
        // We can do that for Mac debugging, but that would require the caller to do another cross-machine
        // ReadProcessMemory(). Since we have all the data in-proc already, we just store a local address.
        //
        // @dbgtodo  Mac - We are using -1 as a dummy base address right now.
        // Currently Mac remote debugging doesn't really support multi-instance.
        InitEventForDebuggerNotification(pEvent, PTR_TO_CORDB_ADDRESS(reinterpret_cast<LPVOID>(-1)), m_pIPCEvent);

        return TRUE;
    }
    else if (dwRet == (WAIT_OBJECT_0 + 1))
    {
        // The process has been terminated.

        // We don't have a lot of information here.
        pEvent->dwDebugEventCode = EXIT_PROCESS_DEBUG_EVENT;
        pEvent->dwProcessId = m_dwProcessId;
        pEvent->dwThreadId = 0;                 // On Windows this is the first thread created in the process.
        pEvent->u.ExitProcess.dwExitCode = 0;   // This is not passed back to us by the transport.

        // Once the process termination event is signaled, we cannot send or receive any events.
        // So we mark the transport as not running anymore.
        m_fRunning = FALSE;
        return TRUE;
    }
    else
    {
        // We may have timed out, or the actual wait operation may have failed.
        // Either way, we don't have an event.
        return FALSE;
    }
}

BOOL DbgTransportPipeline::ContinueDebugEvent(
  DWORD dwProcessId,
  DWORD dwThreadId,
  DWORD dwContinueStatus
)
{
    if (!IsTransportRunning())
    {
        return FALSE;
    }

    // See code:INativeEventPipeline::ContinueDebugEvent.
    return TRUE;
}

// Return a handle which will be signaled when the debuggee process terminates.
HANDLE DbgTransportPipeline::GetProcessHandle()
{
    HANDLE hProcessTerminated;

    if (!DuplicateHandle(GetCurrentProcess(),
                         m_hProcess,
                         GetCurrentProcess(),
                         &hProcessTerminated,
                         0,      // ignored since we are going to pass DUPLICATE_SAME_ACCESS
                         FALSE,
                         DUPLICATE_SAME_ACCESS))
    {
        return NULL;
    }

    // The handle returned here is only valid for waiting on process termination.
    // See code:INativeEventPipeline::GetProcessHandle.
    return hProcessTerminated;
}

// Terminate the debuggee process.
BOOL DbgTransportPipeline::TerminateProcess(UINT32 exitCode)
{
    _ASSERTE(IsTransportRunning());

    // The transport will still be running until the process termination handle is signaled.
    m_pProxy->KillProcess(m_dwProcessId);
    return TRUE;
}
