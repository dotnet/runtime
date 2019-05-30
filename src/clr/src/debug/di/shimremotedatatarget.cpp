// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************

// 
// File: ShimRemoteDataTarget.cpp
//
//*****************************************************************************
#include "stdafx.h"
#include "safewrap.h"

#include "check.h" 

#include <limits.h>

#include "shimpriv.h"
#include "shimdatatarget.h"

#include "dbgtransportsession.h"
#include "dbgtransportmanager.h"

class ShimRemoteDataTarget : public ShimDataTarget
{
public:
    ShimRemoteDataTarget(DWORD processId, DbgTransportTarget * pProxy, DbgTransportSession * pTransport);

    virtual ~ShimRemoteDataTarget();

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
    DbgTransportTarget  * m_pProxy;
    DbgTransportSession * m_pTransport;
    int m_fd;                           // /proc/<pid>/mem handle
};


// Helper macro to check for failure conditions at the start of data-target methods.
#define ReturnFailureIfStateNotOk() \
    if (m_hr != S_OK) \
    { \
        return m_hr; \
    }

//---------------------------------------------------------------------------------------
//
// This is the ctor for ShimRemoteDataTarget. 
//
// Arguments:
//      processId  - pid of live process on the remote machine
//      pProxy     - connection to the debugger proxy
//      pTransport - connection to the debuggee process
//

ShimRemoteDataTarget::ShimRemoteDataTarget(DWORD processId, 
                                           DbgTransportTarget * pProxy, 
                                           DbgTransportSession * pTransport)
{
    m_ref = 0;

    m_processId = processId;
    m_pProxy = pProxy;
    m_pTransport = pTransport;

    m_hr = S_OK;

    m_fpContinueStatusChanged = NULL;
    m_pContinueStatusChangedUserData = NULL;

    char memPath[128];
    _snprintf_s(memPath, sizeof(memPath), sizeof(memPath), "/proc/%lu/mem", m_processId);
    m_fd = _open(memPath, 0); // O_RDONLY
}

//---------------------------------------------------------------------------------------
//
// dtor for ShimRemoteDataTarget
//

ShimRemoteDataTarget::~ShimRemoteDataTarget()
{
    Dispose();
}

//---------------------------------------------------------------------------------------
//
// Dispose all resources and neuter the object.
//
// Notes:
//    Release all resources (such as the connections to the debugger proxy and the debuggee process).
//    May be called multiple times.
//    All other non-trivial APIs (eg, not IUnknown) will fail after this.
//

void ShimRemoteDataTarget::Dispose()
{
    if (m_fd != -1)
    {
        _close(m_fd);
        m_fd = -1;
    }
    if (m_pTransport != NULL)
    {
        m_pProxy->ReleaseTransport(m_pTransport);
    }
    m_pTransport = NULL;
    m_hr = CORDBG_E_OBJECT_NEUTERED;
}

//---------------------------------------------------------------------------------------
//
// Construction method for data-target
//
// Arguments:
//      machineInfo  - (input) the IP address of the remote machine and the port number of the debugger proxy
//      processId    - (input) live OS process ID to build a data-target for.
//      ppDataTarget - (output) new data-target instance. This gets addreffed.
//
// Return Value:
//    S_OK on success.
//
// Assumptions:
//    pid is for a process on the remote machine specified by the IP address in machineInfo
//    Caller must release *ppDataTarget.
//

HRESULT BuildPlatformSpecificDataTarget(MachineInfo machineInfo,
                                        const ProcessDescriptor * pProcessDescriptor,
                                        ShimDataTarget ** ppDataTarget)
{
    HandleHolder hDummy;
    HRESULT hr = E_FAIL;

    ShimRemoteDataTarget * pRemoteDataTarget = NULL;
    DbgTransportTarget *   pProxy = g_pDbgTransportTarget;
    DbgTransportSession *  pTransport = NULL;

    hr = pProxy->GetTransportForProcess(pProcessDescriptor, &pTransport, &hDummy);
    if (FAILED(hr))
    {
        goto Label_Exit;
    }

    if (!pTransport->WaitForSessionToOpen(10000))
    {
        hr = CORDBG_E_TIMEOUT;
        goto Label_Exit;
    }

    pRemoteDataTarget = new (nothrow) ShimRemoteDataTarget(pProcessDescriptor->m_Pid, pProxy, pTransport);
    if (pRemoteDataTarget == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Label_Exit;
    }

    _ASSERTE(SUCCEEDED(hr));
    *ppDataTarget = pRemoteDataTarget;
    pRemoteDataTarget->AddRef(); // must addref out-parameters

Label_Exit:
    if (FAILED(hr))
    {
        if (pRemoteDataTarget != NULL)
        {
            // The ShimRemoteDataTarget has ownership of the proxy and the transport, 
            // so we don't need to clean them up here.
            delete pRemoteDataTarget;
        }
        else
        {
            if (pTransport != NULL)
            {
                pProxy->ReleaseTransport(pTransport);
            }
        }
    }

    return hr;
}

// impl of interface method ICorDebugDataTarget::GetPlatform
HRESULT STDMETHODCALLTYPE
ShimRemoteDataTarget::GetPlatform( 
        CorDebugPlatform *pPlatform)
{
#ifdef FEATURE_PAL
     #if defined(DBG_TARGET_X86)
         *pPlatform = CORDB_PLATFORM_POSIX_X86;
     #elif defined(DBG_TARGET_AMD64)
         *pPlatform = CORDB_PLATFORM_POSIX_AMD64;
     #elif defined(DBG_TARGET_ARM)
         *pPlatform = CORDB_PLATFORM_POSIX_ARM;
     #elif defined(DBG_TARGET_ARM64)
         *pPlatform = CORDB_PLATFORM_POSIX_ARM64;
     #else
         #error Unknown Processor.
     #endif
#else
    #if defined(DBG_TARGET_X86)
        *pPlatform = CORDB_PLATFORM_WINDOWS_X86;
    #elif defined(DBG_TARGET_AMD64)
        *pPlatform = CORDB_PLATFORM_WINDOWS_AMD64;
    #elif defined(DBG_TARGET_ARM)
        *pPlatform = CORDB_PLATFORM_WINDOWS_ARM;
    #elif defined(DBG_TARGET_ARM64)
        *pPlatform = CORDB_PLATFORM_WINDOWS_ARM64;
    #else
        #error Unknown Processor.
    #endif
#endif

    return S_OK;
}

// impl of interface method ICorDebugDataTarget::ReadVirtual
HRESULT STDMETHODCALLTYPE
ShimRemoteDataTarget::ReadVirtual(
    CORDB_ADDRESS address,
    PBYTE pBuffer,
    ULONG32 cbRequestSize,
    ULONG32 *pcbRead)
{
    ReturnFailureIfStateNotOk();

    size_t read = cbRequestSize;
    HRESULT hr = S_OK;

    if (m_fd != -1)
    {
        read = _pread(m_fd, pBuffer, cbRequestSize, (ULONG64)address);
        if (read == -1)
        {
            hr = E_FAIL;
        }
    }
    else
    {
        hr = m_pTransport->ReadMemory(reinterpret_cast<BYTE *>(CORDB_ADDRESS_TO_PTR(address)), pBuffer, cbRequestSize);
    }
    if (pcbRead != NULL)
    {
        *pcbRead = (SUCCEEDED(hr) ? read : 0);
    }
    return hr;
}

// impl of interface method ICorDebugMutableDataTarget::WriteVirtual
HRESULT STDMETHODCALLTYPE
ShimRemoteDataTarget::WriteVirtual( 
    CORDB_ADDRESS pAddress,
    const BYTE * pBuffer,
    ULONG32 cbRequestSize)
{
    ReturnFailureIfStateNotOk();

    HRESULT hr = E_FAIL;
    hr = m_pTransport->WriteMemory(reinterpret_cast<BYTE *>(CORDB_ADDRESS_TO_PTR(pAddress)), 
                                   const_cast<BYTE *>(pBuffer), 
                                   cbRequestSize);
    return hr;
}

// impl of interface method ICorDebugMutableDataTarget::GetThreadContext
HRESULT STDMETHODCALLTYPE
ShimRemoteDataTarget::GetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextFlags,
    ULONG32 contextSize,
    BYTE * pContext)
{
    ReturnFailureIfStateNotOk();
        
    // GetThreadContext() is currently not implemented in ShimRemoteDataTarget, which is used with our pipe transport 
    // (FEATURE_DBGIPC_TRANSPORT_DI). Pipe transport is used on POSIX system, but occasionally we can turn it on for Windows for testing,
    // and then we'd like to have same behavior as on POSIX system (zero context).
    //
    // We don't have a good way to implement GetThreadContext() in ShimRemoteDataTarget yet, because we have no way to convert a thread ID to a 
    // thread handle.  The function to do the conversion is OpenThread(), which is not implemented in PAL. Even if we had a handle, PAL implementation 
    // of GetThreadContext() is very limited and doesn't work when we're not attached with ptrace. 
    // Instead, we just zero out the seed CONTEXT for the stackwalk.  This tells the stackwalker to
    // start the stackwalk with the first explicit frame.  This won't work when we do native debugging, 
    // but that won't happen on the POSIX systems since they don't support native debugging.
    ZeroMemory(pContext, contextSize);
    return E_NOTIMPL;
}

// impl of interface method ICorDebugMutableDataTarget::SetThreadContext
HRESULT STDMETHODCALLTYPE
ShimRemoteDataTarget::SetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextSize,
    const BYTE * pContext)
{
    ReturnFailureIfStateNotOk();

    // ICorDebugDataTarget::GetThreadContext() and ICorDebugDataTarget::SetThreadContext() are currently only 
    // required for interop-debugging and inspection of floating point registers, both of which are not 
    // implemented on Mac.
    _ASSERTE(!"The remote data target doesn't know how to set a thread's CONTEXT.");
    return E_NOTIMPL;
}

// Public implementation of ICorDebugMutableDataTarget::ContinueStatusChanged
HRESULT STDMETHODCALLTYPE
ShimRemoteDataTarget::ContinueStatusChanged(
    DWORD dwThreadId,
    CORDB_CONTINUE_STATUS dwContinueStatus)
{
    ReturnFailureIfStateNotOk();

    _ASSERTE(!"ShimRemoteDataTarget::ContinueStatusChanged() is called unexpectedly");
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
ShimRemoteDataTarget::VirtualUnwind(DWORD threadId, ULONG32 contextSize, PBYTE context)
{
    return m_pTransport->VirtualUnwind(threadId, contextSize, context);
}
