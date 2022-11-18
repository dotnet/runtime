// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: WindowsPipeline.cpp
//

//
// Implements the native-pipeline on Windows OS.
//*****************************************************************************

#include "stdafx.h"
#include "nativepipeline.h"

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
    virtual HRESULT DebugActiveProcess(MachineInfo machineInfo, const ProcessDescriptor& processDescriptor);

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

protected:
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
    m_fKillOnExit = fKillOnExit;
    return TRUE;
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
    return S_OK;
}

// Attach the debugger to this process.
HRESULT WindowsNativePipeline::DebugActiveProcess(MachineInfo machineInfo, const ProcessDescriptor& processDescriptor)
{
    HRESULT hr = E_FAIL;
    BOOL ret = ::DebugActiveProcess(processDescriptor.m_Pid);

    if (ret)
    {
        hr = S_OK;
        m_dwProcessId = processDescriptor.m_Pid;
    }
    else
    {
        hr = HRESULT_FROM_GetLastError();
    }

    return hr;
}

// Detach
HRESULT WindowsNativePipeline::DebugActiveProcessStop(DWORD processId)
{
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
