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
#if !defined(FEATURE_CORESYSTEM)
    // Late bind to DebugSetProcessKillOnExit - WinXP and above only
    HModuleHolder hKernel32;
    hKernel32 = WszLoadLibrary(W("kernel32"));
    SIMPLIFYING_ASSUMPTION(hKernel32 != NULL);
    if (hKernel32 == NULL)
        return;

    typedef BOOL (*DebugSetProcessKillOnExitSig) (BOOL);
    DebugSetProcessKillOnExitSig pDebugSetProcessKillOnExit =
        reinterpret_cast<DebugSetProcessKillOnExitSig>(GetProcAddress(hKernel32, "DebugSetProcessKillOnExit"));

    // If the API doesn't exist (eg. Win2k) - there isn't anything we can do, just
    // silently ignore the request.
    if (pDebugSetProcessKillOnExit == NULL)
        return;

    BOOL ret = pDebugSetProcessKillOnExit(m_fKillOnExit);

    // Not a good failure path here.
    // 1) This shouldn't fail.
    // 2) Even if it does, this is likely called after the debuggee
    // has already been created, and if this API fails, most scenarios will
    // be unaffected, so we don't want to fail the overall debugging session.
    SIMPLIFYING_ASSUMPTION(ret);

#else
	// The API doesn't exit on CoreSystem, just return
	return;
#endif
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
HRESULT WindowsNativePipeline::DebugActiveProcess(MachineInfo machineInfo, const ProcessDescriptor& processDescriptor)
{
    HRESULT hr = E_FAIL;
    BOOL ret = ::DebugActiveProcess(processDescriptor.m_Pid);

    if (ret)
    {
        hr = S_OK;
        m_dwProcessId = processDescriptor.m_Pid;
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
            if (SUCCEEDED(IsRemoteDebuggerPresent(processDescriptor.m_Pid, &fIsDebuggerPresent)))
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
#if !defined(FEATURE_CORESYSTEM)

    // Get a process handle for the process ID.
    HandleHolder hProc = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId);
    if (hProc == NULL)
        return HRESULT_FROM_GetLastError();

    // Delay-bind to CheckRemoteDebuggerPresent - WinXP SP1 and above only
    HModuleHolder hKernel32;
    hKernel32 = WszLoadLibrary(W("kernel32"));
    if (hKernel32 == NULL)
        return HRESULT_FROM_GetLastError();

    typedef BOOL (*CheckRemoteDebuggerPresentSig) (HANDLE, PBOOL);
    CheckRemoteDebuggerPresentSig pCheckRemoteDebuggerPresent =
        reinterpret_cast<CheckRemoteDebuggerPresentSig>(GetProcAddress(hKernel32, "CheckRemoteDebuggerPresent"));
    if (pCheckRemoteDebuggerPresent == NULL)
        return HRESULT_FROM_GetLastError();

    // API exists - call it
    if (!pCheckRemoteDebuggerPresent(hProc, pfDebuggerPresent))
        return HRESULT_FROM_GetLastError();

    return S_OK;
#else

	//CoreSystem doesn't have this API
	return E_FAIL;
#endif
}

// Detach
HRESULT WindowsNativePipeline::DebugActiveProcessStop(DWORD processId)
{
#if !defined(FEATURE_CORESYSTEM)
    // Late-bind to DebugActiveProcessStop since it's WinXP and above only
    HModuleHolder hKernel32;
    hKernel32 = WszLoadLibrary(W("kernel32"));
    if (hKernel32 == NULL)
        return HRESULT_FROM_GetLastError();

    typedef BOOL (*DebugActiveProcessStopSig) (DWORD);
    DebugActiveProcessStopSig pDebugActiveProcessStop =
        reinterpret_cast<DebugActiveProcessStopSig>(GetProcAddress(hKernel32, "DebugActiveProcessStop"));

    // Win2K will fail here - can't find DebugActiveProcessStop
    if (pDebugActiveProcessStop == NULL)
        return HRESULT_FROM_GetLastError();

    // Ok, the API exists, call it
    if (!pDebugActiveProcessStop(processId))
    {
        // Detach itself failed
        return HRESULT_FROM_GetLastError();
    }
#else
	// The API exists, call it
    if (!::DebugActiveProcessStop(processId))
    {
        // Detach itself failed
        return HRESULT_FROM_GetLastError();
    }
#endif
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
#ifdef FEATURE_CORESYSTEM
	_ASSERTE("NYI");
	return E_FAIL;
#else
    _ASSERTE(m_dwProcessId != 0);

    // Take a snapshot of all running threads (similar to ShimProcess::QueueFakeThreadAttachEventsNativeOrder)
    // Alternately we could return thread creation/exit in WaitForDebugEvent.  But we expect this to be used
    // very rarely, so no need to complicate more common codepaths.
    HANDLE hThreadSnap = INVALID_HANDLE_VALUE;
    THREADENTRY32 te32;

    hThreadSnap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (hThreadSnap == INVALID_HANDLE_VALUE)
        return HRESULT_FROM_GetLastError();

    // HandleHolder doesn't deal with INVALID_HANDLE_VALUE, so we only assign if we have a legal value.
    HandleHolder hSnapshotHolder(hThreadSnap);

    // Fill in the size of the structure before using it.
    te32.dwSize = sizeof(THREADENTRY32);

    // Retrieve information about the first thread, and exit if unsuccessful
    if (!Thread32First(hThreadSnap, &te32))
        return HRESULT_FROM_GetLastError();

    // Now walk the thread list of the system and attempt to resume any that are part of this process
    // Ignore errors - this is a best effort (but ASSERT in CHK builds since we don't expect errors
    // in practice - we expect the process to be frozen at a debug event, so no races etc.)

    HRESULT hr = S_FALSE;   // no thread was resumed
    do
    {
        if (te32.th32OwnerProcessID == m_dwProcessId)
        {
            HandleHolder hThread = ::OpenThread(THREAD_SUSPEND_RESUME, FALSE, te32.th32ThreadID);
            _ASSERTE(hThread != NULL);
            if (hThread != NULL)
            {
                // Resume each thread exactly once (if they were suspended multiple times,
                // then EnsureThreadsRunning would need to be called multiple times until it
                // returned S_FALSE.
                DWORD prevCount = ::ResumeThread(hThread);
                _ASSERTE(prevCount >= 0);
                if (prevCount >= 1)
                    hr = S_OK;      // some thread was resumed
            }
        }
    } while(Thread32Next(hThreadSnap, &te32));

    return hr;
#endif
}
