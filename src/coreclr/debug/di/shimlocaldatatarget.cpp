// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// File: ShimLocalDataTarget.cpp
//
//*****************************************************************************
#include "stdafx.h"
#include "safewrap.h"

#include "check.h"

#include <limits.h>

#include "shimpriv.h"
#include "shimdatatarget.h"


// The Shim's Live data-target is allowed to call OS APIs directly.
// see code:RSDebuggingInfo#UseDataTarget.
#undef ReadProcessMemory
#undef WriteProcessMemory


class ShimLocalDataTarget : public ShimDataTarget
{
public:
    ShimLocalDataTarget(DWORD processId, HANDLE hProcess);

    ~ShimLocalDataTarget();

    virtual void Dispose();

    //
    // ICorDebugMutableDataTarget.
    //

    virtual HRESULT STDMETHODCALLTYPE GetPlatform(
        CorDebugPlatform *pPlatform);

    virtual HRESULT STDMETHODCALLTYPE ReadVirtual(
        CORDB_ADDRESS address,
        BYTE * pBuffer,
        ULONG32 request,
        ULONG32 *pcbRead);

    virtual HRESULT STDMETHODCALLTYPE WriteVirtual(
        CORDB_ADDRESS address,
        const BYTE * pBuffer,
        ULONG32 request);

    virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextFlags,
        ULONG32 contextSize,
        BYTE * context);

    virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextSize,
        const BYTE * context);

    virtual HRESULT STDMETHODCALLTYPE ContinueStatusChanged(
        DWORD dwThreadId,
        CORDB_CONTINUE_STATUS dwContinueStatus);

    virtual HRESULT STDMETHODCALLTYPE VirtualUnwind(
        DWORD threadId, ULONG32 contextSize, PBYTE context);

private:
    // Handle to the process. We own this.
    HANDLE m_hProcess;
};


// Determines whether the target and host are running on compatible platforms.
// Arguments:
//     input: hTargetProcess - handle for the target process
// Return Value: TRUE iff both target and host are both Wow64 or neither is.
// Note: throws
BOOL CompatibleHostAndTargetPlatforms(HANDLE hTargetProcess)
{
#if defined(TARGET_UNIX)
    return TRUE;
#else
    // get the platform for the host process
    BOOL fHostProcessIsWow64 = FALSE;
    BOOL fSuccess = FALSE;
    HANDLE hHostProcess = GetCurrentProcess();

    fSuccess = IsWow64Process(hHostProcess, &fHostProcessIsWow64);
    CloseHandle(hHostProcess);
    hHostProcess = NULL;

    if (!fSuccess)
    {
        ThrowHR(HRESULT_FROM_GetLastError());
    }

    //  get the platform for the target process
    if (hTargetProcess == NULL)
    {
        ThrowHR(HRESULT_FROM_GetLastError());
    }

    BOOL fTargetProcessIsWow64 = FALSE;
    fSuccess = IsWow64Process(hTargetProcess, &fTargetProcessIsWow64);

    if (!fSuccess)
    {
        ThrowHR(HRESULT_FROM_GetLastError());
    }

    // We don't want to expose the IPC block if one process is x86 and
    // the other is ia64 or amd64
    if (fTargetProcessIsWow64 != fHostProcessIsWow64)
    {
        return FALSE;
    }
    else
    {
        return TRUE;
    }
#endif
} // CompatibleHostAndTargetPlatforms

// Helper macro to check for failure conditions at the start of data-target methods.
#define ReturnFailureIfStateNotOk() \
    if (m_hr != S_OK) \
    { \
        return m_hr; \
    }

//---------------------------------------------------------------------------------------
//
// ctor for ShimLocalDataTarget.
//
// Arguments:
//      processId - pid of live process.
//      hProcess - handle to kernel process object.
//
// Assumptions:
//    Shim takes ownership of handle hProcess.
//

ShimLocalDataTarget::ShimLocalDataTarget(DWORD processId, HANDLE hProcess)
{
    m_ref = 0;

    m_processId = processId;
    m_hProcess = hProcess;

    m_hr = S_OK;

    m_fpContinueStatusChanged = NULL;
    m_pContinueStatusChangedUserData = NULL;
}

//---------------------------------------------------------------------------------------
//
// dctor for ShimLocalDataTarget.
//
ShimLocalDataTarget::~ShimLocalDataTarget()
{
    Dispose();
}


//---------------------------------------------------------------------------------------
//
// Dispose all resources and neuter the object.
//
//
//
// Notes:
//    Release all resources (such as the handle to the process we got in the ctor).
//    May be called multiple times.
//    All other non-trivial APIs (eg, not IUnknown) will fail after this.
//

void ShimLocalDataTarget::Dispose()
{
    if (m_hProcess != NULL)
    {
        CloseHandle(m_hProcess);
        m_hProcess = NULL;
    }
    m_hr = CORDBG_E_OBJECT_NEUTERED;
}

//---------------------------------------------------------------------------------------
//
// Construction method for data-target
//
// Arguments:
//      processId - (input) live OS process ID to build a data-target for.
//      ppDataTarget - (output) new data-target instance. This gets addreffed.
//
// Return Value:
//    S_OK on success.
//
// Assumptions:
//    pid must be for local, same architecture, process.
//    Caller must have security permissions for OpenProcess()
//    Caller must release *ppDataTarget.
//

HRESULT BuildPlatformSpecificDataTarget(MachineInfo machineInfo,
                                        const ProcessDescriptor * pProcessDescriptor,
                                        ShimDataTarget ** ppDataTarget)
{
    HRESULT hr = S_OK;
    HANDLE hProcess = NULL;
    ShimLocalDataTarget * pLocalDataTarget = NULL;

    *ppDataTarget = NULL;

    hProcess = OpenProcess(
        PROCESS_DUP_HANDLE        |
        PROCESS_QUERY_INFORMATION |
        PROCESS_TERMINATE         |
        PROCESS_VM_OPERATION      |
        PROCESS_VM_READ           |
        PROCESS_VM_WRITE          |
        SYNCHRONIZE,
        FALSE,
        pProcessDescriptor->m_Pid);

    if (hProcess == NULL)
    {
        hr = HRESULT_FROM_GetLastError();
        goto Label_Exit;
    }

    EX_TRY
    {
        if (!CompatibleHostAndTargetPlatforms(hProcess))
        {
            hr = CORDBG_E_INCOMPATIBLE_PLATFORMS;
            goto Label_Exit;
        }
    }
    EX_CATCH_HRESULT(hr);
    if (FAILED(hr))
    {
        goto Label_Exit;
    }
    pLocalDataTarget = new (nothrow) ShimLocalDataTarget(pProcessDescriptor->m_Pid, hProcess);
    if (pLocalDataTarget == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Label_Exit;
    }

    // ShimLocalDataTarget now has ownership of Handle.
    hProcess = NULL;

    _ASSERTE(SUCCEEDED(hr));
    *ppDataTarget = pLocalDataTarget;
    pLocalDataTarget->AddRef(); // must addref out-parameters

Label_Exit:
    if (FAILED(hr))
    {
        if (hProcess != NULL)
        {
            CloseHandle(hProcess);
        }
        delete pLocalDataTarget;
    }

    return hr;
}

// impl of interface method ICorDebugDataTarget::GetPlatform
HRESULT STDMETHODCALLTYPE
ShimLocalDataTarget::GetPlatform(
        CorDebugPlatform *pPlatform)
{
#ifdef TARGET_UNIX
#error ShimLocalDataTarget is not implemented on PAL systems yet
#endif
    // Assume that we're running on Windows for now.
#if defined(TARGET_X86)
    *pPlatform = CORDB_PLATFORM_WINDOWS_X86;
#elif defined(TARGET_AMD64)
    *pPlatform = CORDB_PLATFORM_WINDOWS_AMD64;
#elif defined(TARGET_ARM)
    *pPlatform = CORDB_PLATFORM_WINDOWS_ARM;
#elif defined(TARGET_ARM64)
    *pPlatform = CORDB_PLATFORM_WINDOWS_ARM64;
#else
#error Unknown Processor.
#endif
    return S_OK;
}

// impl of interface method ICorDebugDataTarget::ReadVirtual
HRESULT STDMETHODCALLTYPE
ShimLocalDataTarget::ReadVirtual(
    CORDB_ADDRESS address,
    PBYTE pBuffer,
    ULONG32 cbRequestSize,
    ULONG32 *pcbRead)
{
    ReturnFailureIfStateNotOk();


    // ReadProcessMemory will fail if any part of the
    // region to read does not have read access.  This
    // routine attempts to read the largest valid prefix
    // so it has to break up reads on page boundaries.

    HRESULT hrStatus = S_OK;
    ULONG32 totalDone = 0;
    SIZE_T read;
    ULONG32 readSize;

    while (cbRequestSize > 0)
    {
        // Calculate bytes to read and don't let read cross
        // a page boundary.
        readSize = GetOsPageSize() - (ULONG32)(address & (GetOsPageSize() - 1));
        readSize = min(cbRequestSize, readSize);

        if (!ReadProcessMemory(m_hProcess, (PVOID)(ULONG_PTR)address,
                               pBuffer, readSize, &read))
        {
            if (totalDone == 0)
            {
                // If we haven't read anything indicate failure.
                hrStatus = HRESULT_FROM_GetLastError();
            }
            break;
        }

        totalDone += (ULONG32)read;
        address += read;
        pBuffer += read;
        cbRequestSize -= (ULONG32)read;
    }

    *pcbRead = totalDone;
    return hrStatus;
}

// impl of interface method ICorDebugMutableDataTarget::WriteVirtual
HRESULT STDMETHODCALLTYPE
ShimLocalDataTarget::WriteVirtual(
    CORDB_ADDRESS pAddress,
    const BYTE * pBuffer,
    ULONG32 cbRequestSize)
{
    ReturnFailureIfStateNotOk();

    SIZE_T cbWritten;
    BOOL fWriteOk = WriteProcessMemory(m_hProcess, CORDB_ADDRESS_TO_PTR(pAddress), pBuffer, cbRequestSize, &cbWritten);
    if (fWriteOk)
    {
        _ASSERTE(cbWritten == cbRequestSize);  // MSDN docs say this must always be true
        return S_OK;
    }
    else
    {
        return HRESULT_FROM_GetLastError();
    }
}

HRESULT STDMETHODCALLTYPE
ShimLocalDataTarget::GetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextFlags,
    ULONG32 contextSize,
    BYTE * pContext)
{
    ReturnFailureIfStateNotOk();
    // @dbgtodo - Ideally we should cache the thread handles so that we don't need to
    // open and close the thread handles every time.

    HRESULT hr = E_FAIL;

    if (!CheckContextSizeForBuffer(contextSize, pContext))
    {
        return E_INVALIDARG;
    }

    HandleHolder hThread = OpenThread(
        THREAD_GET_CONTEXT | THREAD_SET_CONTEXT | THREAD_QUERY_INFORMATION ,
        FALSE, // thread handle is not inheritable.
        dwThreadID);

    if (hThread != NULL)
    {
        DT_CONTEXT * pCtx = reinterpret_cast<DT_CONTEXT *>(pContext);
        pCtx->ContextFlags = contextFlags;

        if (DbiGetThreadContext(hThread, pCtx))
        {
            hr = S_OK;
        }
    }

    // hThread destructed automatically
    return hr;
}

// impl of interface method ICorDebugMutableDataTarget::SetThreadContext
HRESULT STDMETHODCALLTYPE
ShimLocalDataTarget::SetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextSize,
    const BYTE * pContext)
{
    ReturnFailureIfStateNotOk();
    HRESULT hr = E_FAIL;

    if (!CheckContextSizeForBuffer(contextSize, pContext))
    {
        return E_INVALIDARG;
    }


    HandleHolder hThread = OpenThread(
        THREAD_GET_CONTEXT | THREAD_SET_CONTEXT | THREAD_QUERY_INFORMATION,
        FALSE, // thread handle is not inheritable.
        dwThreadID);

    if (hThread != NULL)
    {
        if (DbiSetThreadContext(hThread, reinterpret_cast<const DT_CONTEXT *>(pContext)))
        {
            hr = S_OK;
        }
    }

    // hThread destructed automatically
    return hr;
}

// Public implementation of ICorDebugMutableDataTarget::ContinueStatusChanged
HRESULT STDMETHODCALLTYPE
ShimLocalDataTarget::ContinueStatusChanged(
    DWORD dwThreadId,
    CORDB_CONTINUE_STATUS dwContinueStatus)
{
    ReturnFailureIfStateNotOk();
    if (m_fpContinueStatusChanged != NULL)
    {
        return m_fpContinueStatusChanged(m_pContinueStatusChangedUserData, dwThreadId, dwContinueStatus);
    }
    return E_NOTIMPL;
}

//---------------------------------------------------------------------------------------
//
// Unwind the stack to the next frame.
//
// Return Value:
//     context filled in with the next frame
//
HRESULT STDMETHODCALLTYPE
ShimLocalDataTarget::VirtualUnwind(DWORD threadId, ULONG32 contextSize, PBYTE context)
{
#ifndef TARGET_UNIX
    _ASSERTE(!"ShimLocalDataTarget::VirtualUnwind NOT IMPLEMENTED");
#endif
    return E_NOTIMPL;
}

