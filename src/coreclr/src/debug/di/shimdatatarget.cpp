// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
