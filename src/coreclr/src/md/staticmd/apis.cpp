// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"

#include <utilcode.h>                   // Utility helpers.
#include <posterror.h>                  // Error handlers
#define INIT_GUIDS
#include <corpriv.h>
#include <winwrap.h>
#include <mscoree.h>
#include "shimload.h"
#include "metadataexports.h"
#include "ex.h"
#include "product_version.h"

// ---------------------------------------------------------------------------
// %%Function: MetaDataGetDispenser
// This function gets the Dispenser interface given the CLSID and REFIID.
// ---------------------------------------------------------------------------
STDAPI DLLEXPORT MetaDataGetDispenser(  // Return HRESULT
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv)                   // Return interface pointer here.
{

    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;

    NonVMComHolder<IClassFactory> pcf(NULL);
    HRESULT hr;
    BEGIN_ENTRYPOINT_NOTHROW;

    IfFailGo(MetaDataDllGetClassObject(rclsid, IID_IClassFactory, (void **) &pcf));
    hr = pcf->CreateInstance(NULL, riid, ppv);

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return (hr);
}

// ---------------------------------------------------------------------------
// %%Function: GetMetaDataInternalInterface
// This function gets the IMDInternalImport given the metadata on memory.
// ---------------------------------------------------------------------------
STDAPI DLLEXPORT GetMetaDataInternalInterface(
    LPVOID      pData,                  // [IN] in memory metadata section
    ULONG       cbData,                 // [IN] size of the metadata section
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [IN] desired interface
    void        **ppv)                  // [OUT] returned interface
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pData));
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = GetMDInternalInterface(pData, cbData, flags, riid, ppv);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// %%Function: GetMetaDataInternalInterfaceFromPublic
// This function gets the internal scopeless interface given the public
// scopeless interface.
// ---------------------------------------------------------------------------
STDAPI DLLEXPORT GetMetaDataInternalInterfaceFromPublic(
    IUnknown    *pv,                    // [IN] Given interface.
    REFIID      riid,                   // [IN] desired interface
    void        **ppv)                  // [OUT] returned interface
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pv));
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = GetMDInternalInterfaceFromPublic(pv, riid, ppv);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// %%Function: GetMetaDataPublicInterfaceFromInternal
// This function gets the public scopeless interface given the internal
// scopeless interface.
// ---------------------------------------------------------------------------
STDAPI DLLEXPORT GetMetaDataPublicInterfaceFromInternal(
    void        *pv,                    // [IN] Given interface.
    REFIID      riid,                   // [IN] desired interface.
    void        **ppv)                  // [OUT] returned interface
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pv));
        PRECONDITION(CheckPointer(ppv));
        ENTRY_POINT;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = GetMDPublicInterfaceFromInternal(pv, riid, ppv);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

//
// Returns version of the runtime (null-terminated).
//
// Arguments:
//    pBuffer - [out] Output buffer allocated by caller of size cchBuffer.
//    cchBuffer - Size of pBuffer in characters.
//    pdwLength - [out] Size of the version string in characters (incl. null-terminator). Will be filled
//                even if ERROR_INSUFFICIENT_BUFFER is returned.
//
// Return Value:
//    S_OK - Output buffer contains the version string.
//    HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) - *pdwLength contains required size of the buffer in
//                                                    characters.

STDAPI GetCORVersionInternal(
__out_ecount_z_opt(cchBuffer) LPWSTR pBuffer,
                              DWORD cchBuffer,
                        __out DWORD *pdwLength)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pBuffer, NULL_OK));
        PRECONDITION(CheckPointer(pdwLength));
    } CONTRACTL_END;

    HRESULT hr;
    BEGIN_ENTRYPOINT_NOTHROW;

    if ((pBuffer != NULL) && (cchBuffer > 0))
    {   // Initialize the output for case the function fails
        *pBuffer = W('\0');
    }

#define VERSION_NUMBER_NOSHIM W("v") QUOTE_MACRO_L(CLR_MAJOR_VERSION.CLR_MINOR_VERSION.CLR_BUILD_VERSION)

    DWORD length = (DWORD)(wcslen(VERSION_NUMBER_NOSHIM) + 1);
    if (length > cchBuffer)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }
    else
    {
        if (pBuffer == NULL)
        {
            hr = E_POINTER;
        }
        else
        {
            CopyMemory(pBuffer, VERSION_NUMBER_NOSHIM, length * sizeof(WCHAR));
            hr = S_OK;
        }
    }
    *pdwLength = length;

    END_ENTRYPOINT_NOTHROW;
    return hr;

}

// Replacement for legacy shim API GetCORRequiredVersion(...) used in linked libraries.
// Used in code:TiggerStorage::GetDefaultVersion#CallTo_CLRRuntimeHostInternal_GetImageVersionString.
HRESULT
CLRRuntimeHostInternal_GetImageVersionString(
    __out_ecount_opt(*pcchBuffer) LPWSTR wszBuffer,
    __inout                       DWORD *pcchBuffer)
{
    // Simply forward the call to the ICLRRuntimeHostInternal implementation.
    STATIC_CONTRACT_WRAPPER;

    HRESULT hr = GetCORVersionInternal(wszBuffer, *pcchBuffer, pcchBuffer);

    return hr;
} // CLRRuntimeHostInternal_GetImageVersionString
