//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

    ~ShimRemoteDataTarget();

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

private:
    DbgTransportTarget  * m_pProxy;
    DbgTransportSession * m_pTransport;
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
    if (m_pTransport != NULL)
    {
        m_pProxy->ReleaseTransport(m_pTransport);
    }

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
                                        DWORD processId, 
                                        ShimDataTarget ** ppDataTarget)
{
    HandleHolder hDummy;
    HRESULT hr = E_FAIL;

    ShimRemoteDataTarget * pRemoteDataTarget = NULL;
    DbgTransportTarget *   pProxy = g_pDbgTransportTarget;
    DbgTransportSession *  pTransport = NULL;

    hr = pProxy->GetTransportForProcess(processId, &pTransport, &hDummy);
    if (FAILED(hr))
    {
        goto Label_Exit;
    }

    if (!pTransport->WaitForSessionToOpen(10000))
    {
        hr = CORDBG_E_TIMEOUT;
        goto Label_Exit;
    }

    pRemoteDataTarget = new (nothrow) ShimRemoteDataTarget(processId, pProxy, pTransport);
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
         *pPlatform = CORDB_PLATFORM_MAC_X86;
     #elif defined(DBG_TARGET_AMD64)
         *pPlatform = CORDB_PLATFORM_MAC_AMD64;
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

    HRESULT hr = E_FAIL;
    hr = m_pTransport->ReadMemory(reinterpret_cast<BYTE *>(CORDB_ADDRESS_TO_PTR(address)), 
                                  pBuffer, 
                                  cbRequestSize);
    if (pcbRead != NULL)
    {
        *pcbRead = (SUCCEEDED(hr) ? cbRequestSize : 0);
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

    // ICorDebugDataTarget::GetThreadContext() and ICorDebugDataTarget::SetThreadContext() are currently only 
    // required for interop-debugging and inspection of floating point registers, both of which are not 
    // implemented on Mac.
    _ASSERTE(!"The remote data target doesn't know how to get a thread's CONTEXT.");
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
