// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: WindowsPipeline.cpp
// 

//
// Implements the native-pipeline on Windows OS.
//*****************************************************************************

#include "stdafx.h"
#include "nativepipeline.h"

#include <Tlhelp32.h>

#include "holder.h"


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
// Class serves as a connector to win32 native-debugging API.
class WindowsNativePipeline : 
    public INativeEventPipeline
{
public:
    WindowsNativePipeline()
    {
        // Default value for Win32.
        m_fKillOnExit = true;
        m_dwProcessId = 0;
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
    virtual HRESULT DebugActiveProcess(MachineInfo machineInfo, DWORD processId);

    // Detach
    virtual HRESULT DebugActiveProcessStop(DWORD processId);

    virtual BOOL WaitForDebugEvent(DEBUG_EVENT * pEvent, DWORD dwTimeout, CordbProcess * pProcess);

    virtual BOOL ContinueDebugEvent(
      DWORD dwProcessId,
      DWORD dwThreadId,
      DWORD dwContinueStatus
    );

    // Return a handle for the debuggee process.
    virtual HANDLE GetProcessHandle();

    // Terminate the debuggee process.
    virtual BOOL TerminateProcess(UINT32 exitCode);

    // Resume any suspended threads
    virtual HRESULT EnsureThreadsRunning();

protected:
    void UpdateDebugSetProcessKillOnExit();

    HRESULT IsRemoteDebuggerPresent(DWORD processId, BOOL* pfDebuggerPresent);

    // Cached value from DebugSetProcessKillOnExit.
    // This is thread-local, and impacts all debuggees on the thread.
    bool m_fKillOnExit;

    DWORD m_dwProcessId;
};

// Allocate and return a pipeline object for this platform
INativeEventPipeline * NewPipelineForThisPlatform()
{
    return new (nothrow) WindowsNativePipeline();
}

// Call to free up the pipeline.
void WindowsNativePipeline::Delete()
{
    delete this;
}


// set whether to kill outstanding debuggees when the debugger exits.
BOOL WindowsNativePipeline::DebugSetProcessKillOnExit(bool fKillOnExit)
{
    // Can't call kernel32!DebugSetProcessKillOnExit until after the event thread
    // has spawned a debuggee. So cache the value now and call it later.
    // This bit is enforced in code:WindowsNativePipeline::UpdateDebugSetProcessKillOnExit
    m_fKillOnExit = fKillOnExit;
    return TRUE;
}

// Enforces the bit set in code:WindowsNativePipeline::DebugSetProcessKillOnExit
void WindowsNativePipeline::UpdateDebugSetProcessKillOnExit()
{
	// The API doesn't exit on CoreSystem, just return
	return;
}

// Create an process under the debugger.
HRESULT WindowsNativePipeline::CreateProcessUnderDebugger(
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
    // This is always doing Native-debugging at the OS-level.
    dwCreationFlags |= (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS);

    BOOL ret = ::WszCreateProcess(
          lpApplicationName,
          lpCommandLine,
          lpProcessAttributes,
          lpThreadAttributes,
          bInheritHandles,
          dwCreationFlags,
          lpEnvironment,
          lpCurrentDirectory,
          lpStartupInfo,
          lpProcessInformation);
    if (!ret)
    {
        return HRESULT_FROM_GetLastError();
    }

    m_dwProcessId = lpProcessInformation->dwProcessId;
    UpdateDebugSetProcessKillOnExit();
    return S_OK;    
}

// Attach the debugger to this process.
HRESULT WindowsNativePipeline::DebugActiveProcess(MachineInfo machineInfo, DWORD processId)
{
    HRESULT hr = E_FAIL;
    BOOL ret = ::DebugActiveProcess(processId);

    if (ret)
    {
        hr = S_OK;
        m_dwProcessId = processId;
        UpdateDebugSetProcessKillOnExit();
    }
    else
    {
        hr = HRESULT_FROM_GetLastError();

        // There are at least two scenarios in which DebugActiveProcess() returns E_INVALIDARG: 
        //     1) if the specified process doesn't exist, or
        //     2) if the specified process already has a debugger atttached
        // We need to distinguish these two cases in order to return the correct HR.
        if (hr == E_INVALIDARG)
        {
            // Check whether a debugger is known to be already attached.
            // Note that this API won't work on some OSes, in which case we err on the side of returning E_INVALIDARG
            // even though a debugger may be attached.  Another approach could be to assume that if
            // OpenProcess succeeded, then DebugActiveProcess must only have failed because a debugger is
            // attached.  But I think it's better to only return the specific error code if we know for sure
            // the case is true.
            BOOL fIsDebuggerPresent = FALSE;
            if (SUCCEEDED(IsRemoteDebuggerPresent(processId, &fIsDebuggerPresent)))
            {
                if (fIsDebuggerPresent)
                {
                    hr = CORDBG_E_DEBUGGER_ALREADY_ATTACHED;
                }
            }
        }
    }

    return hr;
}

// Determine (if possible) whether a debugger is attached to the target process
HRESULT WindowsNativePipeline::IsRemoteDebuggerPresent(DWORD processId, BOOL* pfDebuggerPresent)
{

	//CoreSystem doesn't have this API
	return E_FAIL;
}

// Detach
HRESULT WindowsNativePipeline::DebugActiveProcessStop(DWORD processId)
{
	// The API exists, call it
    if (!::DebugActiveProcessStop(processId))
    {
        // Detach itself failed
        return HRESULT_FROM_GetLastError();
    }
    return S_OK;
}

BOOL WindowsNativePipeline::WaitForDebugEvent(DEBUG_EVENT * pEvent, DWORD dwTimeout, CordbProcess * pProcess)
{
    return ::WaitForDebugEvent(pEvent, dwTimeout);
}

BOOL WindowsNativePipeline::ContinueDebugEvent(
  DWORD dwProcessId,
  DWORD dwThreadId,
  DWORD dwContinueStatus
)
{
    return ::ContinueDebugEvent(dwProcessId, dwThreadId, dwContinueStatus);
}

// Return a handle for the debuggee process.
HANDLE WindowsNativePipeline::GetProcessHandle()
{
    _ASSERTE(m_dwProcessId != 0);

    return ::OpenProcess(PROCESS_DUP_HANDLE        |
                         PROCESS_QUERY_INFORMATION |
                         PROCESS_TERMINATE         |
                         PROCESS_VM_OPERATION      |
                         PROCESS_VM_READ           |
                         PROCESS_VM_WRITE          |
                         SYNCHRONIZE,
                         FALSE,
                         m_dwProcessId);
}

// Terminate the debuggee process.
BOOL WindowsNativePipeline::TerminateProcess(UINT32 exitCode)
{
    _ASSERTE(m_dwProcessId != 0);

    // Get a process handle for the process ID.
    HandleHolder hProc = OpenProcess(PROCESS_TERMINATE, FALSE, m_dwProcessId);

    if (hProc == NULL)
    {
        return FALSE;
    }

    return ::TerminateProcess(hProc, exitCode);
}

// Resume any suspended threads (but just once)
HRESULT WindowsNativePipeline::EnsureThreadsRunning()
{
	_ASSERTE("NYI");
	return E_FAIL;
}
