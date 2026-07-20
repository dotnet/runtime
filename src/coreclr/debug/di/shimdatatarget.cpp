// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// File: ShimDataTarget.cpp
//
//*****************************************************************************
#include "stdafx.h"
#include "safewrap.h"

#include "check.h"

#include <limits.h>

#include "shimpriv.h"


// Standard impl of IUnknown::QueryInterface
HRESULT STDMETHODCALLTYPE ShimDataTarget::QueryInterface(
    REFIID InterfaceId,
    PVOID* pInterface
    )
{
    if (InterfaceId == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugDataTarget *>(this));
    }
    else if (InterfaceId == IID_ICorDebugDataTarget)
    {
        *pInterface = static_cast<ICorDebugDataTarget *>(this);
    }
    else if (InterfaceId == IID_ICorDebugMutableDataTarget)
    {
        *pInterface = static_cast<ICorDebugMutableDataTarget *>(this);
    }
    else if (InterfaceId == IID_ICorDebugDataTarget4)
    {
        *pInterface = static_cast<ICorDebugDataTarget4 *>(this);
    }
    else if (InterfaceId == IID_ICorDebugDataTarget5)
    {
        *pInterface = static_cast<ICorDebugDataTarget5 *>(this);
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

// Standard impl of IUnknown::AddRef
ULONG STDMETHODCALLTYPE ShimDataTarget::AddRef()
{
    LONG ref = InterlockedIncrement(&m_ref);
    return ref;
}

// Standard impl of IUnknown::Release
ULONG STDMETHODCALLTYPE ShimDataTarget::Release()
{
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

//---------------------------------------------------------------------------------------
//
// Get the OS Process ID that this DataTarget is for.
//
// Return Value:
//     The OS PID of the process this data target is representing.
DWORD ShimDataTarget::GetPid()
{
    return m_processId;
}

//---------------------------------------------------------------------------------------
// Hook a custom function to handle ICorDebugMutableDataTarget::ContinueStatusChanged
//
// Arguments:
//   fpContinueStatusChanged - callback function to invoke.
//   pUserData - user data to pass to callback
//
void ShimDataTarget::HookContinueStatusChanged(FPContinueStatusChanged fpContinueStatusChanged, void * pUserData)
{
    m_fpContinueStatusChanged = fpContinueStatusChanged;
    m_pContinueStatusChangedUserData = pUserData;
}

//---------------------------------------------------------------------------------------
// ICorDebugDataTarget5::GetTargetInfo
//
// Reports the processor architecture and operating system of the target process.
//
HRESULT STDMETHODCALLTYPE ShimDataTarget::GetTargetInfo(CorDebugTargetInfo * pTargetInfo)
{
    if (pTargetInfo == NULL)
    {
        return E_INVALIDARG;
    }

#if defined(_M_IX86) || defined(__i386__)
    pTargetInfo->arch = CORDB_ARCH_X86;
#elif defined(_M_X64) || defined(__x86_64__)
    pTargetInfo->arch = CORDB_ARCH_AMD64;
#elif defined(_M_ARM) || defined(__arm__)
    pTargetInfo->arch = CORDB_ARCH_ARM;
#elif defined(_M_ARM64) || defined(__aarch64__)
    pTargetInfo->arch = CORDB_ARCH_ARM64;
#elif defined(__loongarch64)
    pTargetInfo->arch = CORDB_ARCH_LOONGARCH64;
#elif defined(__riscv) && (__riscv_xlen == 64)
    pTargetInfo->arch = CORDB_ARCH_RISCV64;
#else
    pTargetInfo->arch = CORDB_ARCH_UNKNOWN;
#endif

#if defined(__APPLE__)
    pTargetInfo->os = CORDB_OS_MACOS;
#elif defined(__linux__)
    pTargetInfo->os = CORDB_OS_LINUX;
#elif defined(_WIN32)
    pTargetInfo->os = CORDB_OS_WINDOWS;
#else
    pTargetInfo->os = CORDB_OS_UNKNOWN;
#endif

    return S_OK;
}
