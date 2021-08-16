// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: EventRedirectionPipeline.cpp
//

//
// Implement a native pipeline that redirects events.
//*****************************************************************************

#include "stdafx.h"
#include "nativepipeline.h"
#include "sstring.h"

#if defined(ENABLE_EVENT_REDIRECTION_PIPELINE)
#include "eventredirection.h"
#include "eventredirectionpipeline.h"


// Constructor
EventRedirectionPipeline::EventRedirectionPipeline()
{
    m_pBlock = NULL;
    m_dwProcessId = 0;

    InitConfiguration();
}

// Dtor
EventRedirectionPipeline::~EventRedirectionPipeline()
{
    CloseBlock();
}

// Call to free up the pipeline.
void EventRedirectionPipeline::Delete()
{
    delete this;
}

//---------------------------------------------------------------------------------------
//
// Returns true if the Redirection is enabled.
//
// Arguments:
//    szOptions - specific Create/attach options to include in the overal format string
//    pidTarget - pid of real debuggeee.
//
// Return Value:
//    S_OK on success.
//
//
// Notes:
//    This will spin up an auxillary debugger (windbg) and attach it to the existing
//    process. If this is a create case, then we're attaching to a create-suspended process.
//
//---------------------------------------------------------------------------------------
void EventRedirectionPipeline::InitConfiguration()
{
    // We need some config strings. See header for possible values.
    m_DebuggerCmd = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DbgRedirectApplication);
    m_AttachParams = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DbgRedirectAttachCmd);
    m_CreateParams = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DbgRedirectCreateCmd);
    m_CommonParams = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DbgRedirectCommonCmd);
}


// Implement INativeEventPipeline::DebugSetProcessKillOnExit
BOOL EventRedirectionPipeline::DebugSetProcessKillOnExit(bool fKillOnExit)
{
    // Not implemented for redirection pipeline. That's ok. Redirection pipeline doesn't care.
    // Return success so caller can assert for other pipeline types.
    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// Attach a real debugger to the target.
//
// Arguments:
//    szOptions - specific Create/attach options to include in the overal format string
//    pidTarget - pid of real debuggeee.
//
// Return Value:
//    S_OK on success.
//
//
// Notes:
//    This will spin up an auxillary debugger (windbg) and attach it to the existing
//    process. If this is a create case, then we're attaching to a create-suspended process.
//
//---------------------------------------------------------------------------------------
HRESULT EventRedirectionPipeline::AttachDebuggerToTarget(LPCWSTR szOptions, DWORD pidTarget)
{
    SString s;

    BOOL fRemap = false;

    LPCWSTR lpApplicationName = NULL;
    LPCWSTR lpCommandLine = NULL;

    EX_TRY
    {
        m_pBlock = new (nothrow) RedirectionBlock(); // $$ make throwing

        ZeroMemory(m_pBlock, sizeof(RedirectionBlock));

        // Initialize
        m_pBlock->m_versionCookie = EVENT_REDIRECTION_CURRENT_VERSION;

        s.Printf(m_CommonParams, GetCurrentProcessId(), m_pBlock, szOptions, pidTarget);
        lpCommandLine = s.GetUnicode();


        lpApplicationName = m_DebuggerCmd; // eg, something like L"c:\\debuggers_amd64\\windbg.exe";

        // Initialize events.
        const BOOL kManualResetEvent = TRUE;
        const BOOL kAutoResetEvent = FALSE;

        m_pBlock->m_hEventAvailable = WszCreateEvent(NULL, kAutoResetEvent, FALSE, NULL);
        m_pBlock->m_hEventConsumed  = WszCreateEvent(NULL, kAutoResetEvent, FALSE, NULL);

        m_pBlock->m_hDetachEvent = WszCreateEvent(NULL, kManualResetEvent, FALSE, NULL);

        fRemap = true;
    }EX_CATCH {}
    EX_END_CATCH(SwallowAllExceptions)

    if (!fRemap)
    {
        return HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
    }

    STARTUPINFO startupInfo = {0};
    startupInfo.cb = sizeof (STARTUPINFOW);

    PROCESS_INFORMATION procInfo = {0};

    // Now create the debugger
    BOOL fStatus = WszCreateProcess(
        lpApplicationName,
        lpCommandLine,
        NULL,
        NULL,
        FALSE,
        0, // flags
        NULL,
        NULL,
        &startupInfo,
        &procInfo);

    if (!fStatus)
    {
        return HRESULT_FROM_GetLastError();
    }

    CloseHandle(procInfo.hProcess);
    CloseHandle(procInfo.hThread);

    return S_OK;

}

//---------------------------------------------------------------------------------------
//
// Close the event block
//
// Notes:
//     This can be called multiple times.
//---------------------------------------------------------------------------------------
void EventRedirectionPipeline::CloseBlock()
{
    if (m_pBlock == NULL)
    {
        return;
    }

    // Close our handle to the IPC events. When server closes its handles, OS will free the events.

    // Setting the detach event signals the Server that this block is closing.
    if (m_pBlock->m_hDetachEvent != NULL)
    {
        SetEvent(m_pBlock->m_hDetachEvent);
        CloseHandle(m_pBlock->m_hDetachEvent);
    }

    if (m_pBlock->m_hEventAvailable != NULL)
    {
        CloseHandle(m_pBlock->m_hEventAvailable);
    }

    if (m_pBlock->m_hEventConsumed != NULL)
    {
        CloseHandle(m_pBlock->m_hEventConsumed);
    }

    delete m_pBlock;
    m_pBlock = NULL;
}


// Wait for a debug event
BOOL EventRedirectionPipeline::WaitForDebugEvent(DEBUG_EVENT * pEvent, DWORD dwTimeout, CordbProcess * pProcess)
{
    // Get debug event via Redirection from control block
    DWORD res = WaitForSingleObject(m_pBlock->m_hEventAvailable, dwTimeout);
    if (res == WAIT_TIMEOUT)
    {
        // No event is available.
        return FALSE;
    }


    pEvent->dwDebugEventCode = EXCEPTION_DEBUG_EVENT;
    pEvent->dwProcessId  = m_pBlock->m_dwProcessId;
    pEvent->dwThreadId = m_pBlock->m_dwThreadId;
    pEvent->u.Exception.dwFirstChance = m_pBlock->m_dwFirstChance;

    _ASSERTE(sizeof(m_pBlock->m_record) == sizeof(pEvent->u.Exception.ExceptionRecord));
    memcpy(&pEvent->u.Exception.ExceptionRecord, &m_pBlock->m_record, sizeof(m_pBlock->m_record));

    // We've got an event!
    return TRUE;
}

// Continue a debug event
BOOL EventRedirectionPipeline::ContinueDebugEvent(
  DWORD dwProcessId,
  DWORD dwThreadId,
  DWORD dwContinueStatus
)
{
    m_pBlock->m_ContinuationStatus = dwContinueStatus;
    m_pBlock->m_counterConsumed++;

    // Sanity check the block. If these checks fail, then the block is corrupted (perhaps a issue in the
    // extension dll feeding us the events?).


    _ASSERTE(dwProcessId == m_pBlock->m_dwProcessId);
    _ASSERTE(dwThreadId  == m_pBlock->m_dwThreadId);
    _ASSERTE(m_pBlock->m_counterAvailable == m_pBlock->m_counterConsumed);

    SetEvent(m_pBlock->m_hEventConsumed);

    return TRUE;
}

// Create
HRESULT EventRedirectionPipeline::CreateProcessUnderDebugger(
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
    DWORD dwRealCreationFlags = dwCreationFlags;
    dwRealCreationFlags |= CREATE_SUSPENDED;
    dwRealCreationFlags &= ~(DEBUG_ONLY_THIS_PROCESS | DEBUG_PROCESS);

    // We must create the real process so that startup info and process information are correct.
    BOOL fStatus = WszCreateProcess(
            lpApplicationName,
            lpCommandLine,
            lpProcessAttributes,
            lpThreadAttributes,
            bInheritHandles,
            dwRealCreationFlags,
            lpEnvironment,
            lpCurrentDirectory,
            lpStartupInfo,
            lpProcessInformation);
    if (!fStatus)
    {
        return HRESULT_FROM_GetLastError();
    }

    // Attach the real debugger.
    AttachDebuggerToTarget(m_CreateParams, lpProcessInformation->dwProcessId);

    m_dwProcessId = lpProcessInformation->dwProcessId;

    return S_OK;
}


// Attach
HRESULT EventRedirectionPipeline::DebugActiveProcess(MachineInfo machineInfo, const ProcessDescriptor& processDescriptor)
{
    m_dwProcessId = processDescriptor.m_Pid;

    // Use redirected pipeline
    // Spin up debugger to attach to target.
    return AttachDebuggerToTarget(m_AttachParams, processDescriptor.m_Pid);
}

// Detach
HRESULT EventRedirectionPipeline::DebugActiveProcessStop(DWORD processId)
{
    // Use redirected pipeline
    SetEvent(m_pBlock->m_hDetachEvent);
    CloseBlock();

    // Assume detach can't fail (true on WinXP and above)
    return S_OK;
}

// Return a handle for the debuggee process.
HANDLE EventRedirectionPipeline::GetProcessHandle()
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
BOOL EventRedirectionPipeline::TerminateProcess(UINT32 exitCode)
{
    _ASSERTE(m_dwProcessId != 0);

    // Get a process handle for the process ID.
    HANDLE hProc = OpenProcess(PROCESS_TERMINATE, FALSE, m_dwProcessId);

    if (hProc == NULL)
    {
        return FALSE;
    }

    return ::TerminateProcess(hProc, exitCode);
}

#endif // ENABLE_EVENT_REDIRECTION_PIPELINE


