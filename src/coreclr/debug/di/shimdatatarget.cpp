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

extern "C" bool TryGetSymbol(
    ICorDebugDataTarget* dataTarget,
    uint64_t baseAddress,
    const char* symbolName,
    uint64_t* symbolAddress);

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
    else if (InterfaceId == IID_ICLRContractLocator)
    {
        *pInterface = static_cast<ICLRContractLocator *>(this);
    }
    else if (InterfaceId == IID_ICLRRuntimeLocator)
    {
        *pInterface = static_cast<ICLRRuntimeLocator *>(this);
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

void ShimDataTarget::SetRuntimeBase(CORDB_ADDRESS runtimeBase)
{
    m_runtimeBase = runtimeBase;
}

HRESULT STDMETHODCALLTYPE ShimDataTarget::GetContractDescriptor(CLRDATA_ADDRESS * contractAddress)
{
    if (contractAddress == NULL || m_runtimeBase == 0)
    {
        return E_INVALIDARG;
    }

    uint64_t address = 0;
    if (!TryGetSymbol(
            static_cast<ICorDebugDataTarget *>(this),
            m_runtimeBase,
            "DotNetRuntimeContractDescriptor",
            &address))
    {
        return E_FAIL;
    }

    *contractAddress = address;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE ShimDataTarget::GetRuntimeBase(CLRDATA_ADDRESS * baseAddress)
{
    if (baseAddress == NULL || m_runtimeBase == 0)
    {
        return E_INVALIDARG;
    }

    *baseAddress = m_runtimeBase;
    return S_OK;
}
