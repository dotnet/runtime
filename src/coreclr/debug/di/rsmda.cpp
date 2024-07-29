// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: RsMda.cpp
//

// Manage Debug Assistant support in the Right-Side
//
//*****************************************************************************
#include "stdafx.h"

#include "winbase.h"
#include "corpriv.h"

//-----------------------------------------------------------------------------
// Cordb MDA notification
//-----------------------------------------------------------------------------
CordbMDA::CordbMDA(CordbProcess * pProc, DebuggerMDANotification * pData)
: CordbBase(pProc, 0, enumCordbMDA)
{
    _ASSERTE(pData != NULL);

    // Owning Parent process should add us to process'es neuter list.

    // Pick up ownership of strings
    m_szName         = pData->szName.TransferStringData();
    m_szDescription  = pData->szDescription.TransferStringData();
    m_szXml          = pData->szXml.TransferStringData();

    m_dwOSTID = pData->dwOSThreadId;
    m_flags   = pData->flags;
}

//-----------------------------------------------------------------------------
// Destructor for CordbMDA object. Not much to do here since neutering should
// have taken care of it all.
//-----------------------------------------------------------------------------
CordbMDA::~CordbMDA()
{
    // Strings protected w/ holders that will automatically free them.
    _ASSERTE(IsNeutered());
}

//-----------------------------------------------------------------------------
// Neuter the CordbMDA object.
//-----------------------------------------------------------------------------
void CordbMDA::Neuter()
{
    // Release buffers. Once we're neutered, these can no longer be accessed anyways,
    // so may as well free them now.
    // This is being done under the process-lock, and our accessors are also done
    // under that lock, so we don't have to worry about any races here. :)
    m_szName.Clear();
    m_szDescription.Clear();
    m_szXml.Clear();

    CordbBase::Neuter();
};

//-----------------------------------------------------------------------------
// Implement IUnknown::QueryInterface.
//-----------------------------------------------------------------------------
HRESULT CordbMDA::QueryInterface(REFIID riid, void **ppInterface)
{
    if (riid == IID_ICorDebugMDA)
        *ppInterface = static_cast<ICorDebugMDA*>(this);
    else if (riid == IID_IUnknown)
        *ppInterface = static_cast<IUnknown*>(static_cast<ICorDebugMDA*>(this));
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// Helper to marshal a string object out through the ICorDebug interfaces
//  *GetName() functions using the common triple design pattern.
//
// parameters:
//   pInputString - the string that we want to marshal out via the triple
//   cchName, pcchName, szName - triple used to marshal out a string.
//        Same usage as CordbModule::GetName and other string getters on the API.
//
// *pcchName is always set to the length of pInputString (including NULL). This lets
//   callers know the full size of buffer they'd need to allocate to get the full string.
//
// if (cchName == 0) then we're in "query" mode:
//     szName must be null. pcchName must be non-null and this function will just set
//     *pcchName to let the caller know how large of a buffer to allocate.

// if (cchName != 0) then we copy as much as can fit into szName. We will always
//     null terminate szName.
//     pcchName can be null. If it's non-null, we set it.
//
//
// Expected usage is that caller calls us twice, once in query mode to allocate
// buffer, then a 2nd time to fill the buffer.
//
// Returns: S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CopyOutString(LPCWSTR pInputString, ULONG32 cchName, ULONG32 * pcchName, _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    _ASSERTE(pInputString != NULL);
    ULONG32 len = (ULONG32) u16_strlen(pInputString) + 1;

    if (cchName == 0)
    {
        // Query length
        if ((szName != NULL) || (pcchName == NULL))
        {
            return E_INVALIDARG;
        }
        *pcchName = len;
        return S_OK;
    }
    else
    {
        // Get data
        if (szName == NULL)
        {
            return E_INVALIDARG;
        }

        // Just copy whatever we can fit into the buffer. If we truncate, that's ok.
        // This will also guarantee that we null terminate.
        wcsncpy_s(szName, cchName, pInputString, _TRUNCATE);

        if (pcchName != 0)
        {
            *pcchName = len;
        }

        return S_OK;
    }
}

//-----------------------------------------------------------------------------
// Get the string for the type of the MDA. Never empty.
// This is a convenient performant alternative to getting the XML stream and extracting
// the type from that based off the schema.
// See CopyOutString for parameter details.
//-----------------------------------------------------------------------------
HRESULT CordbMDA::GetName(ULONG32 cchName, ULONG32 * pcchName, _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        hr = E_NOTIMPL;
    }
    PUBLIC_API_END(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Get a string description of the MDA. This may be empty (0-length).
// See CopyOutString for parameter details.
//-----------------------------------------------------------------------------
HRESULT CordbMDA::GetDescription(ULONG32 cchName, ULONG32 * pcchName, _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        hr = E_NOTIMPL;
    }
    PUBLIC_API_END(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Get the full associated XML for the MDA. This may be empty.
// This could be a potentially expensive operation if the xml stream is large.
// See the MDA documentation for the schema for this XML stream.
// See CopyOutString for parameter details.
//-----------------------------------------------------------------------------
HRESULT CordbMDA::GetXML(ULONG32 cchName, ULONG32 * pcchName, _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        hr = E_NOTIMPL;
    }
    PUBLIC_API_END(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Get flags for this MDA object.
//-----------------------------------------------------------------------------
HRESULT CordbMDA::GetFlags(CorDebugMDAFlags * pFlags)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        hr = E_NOTIMPL;
    }
    PUBLIC_API_END(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Thread that the MDA is fired on. We use the os tid instead of an ICDThread in case an MDA is fired on a
// native thread (or a managed thread that hasn't yet entered managed code and so we don't have a ICDThread
// object for it yet)
//-----------------------------------------------------------------------------
HRESULT CordbMDA::GetOSThreadId(DWORD * pOsTid)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        hr = E_NOTIMPL;
    }
    PUBLIC_API_END(hr);
    return hr;
}

