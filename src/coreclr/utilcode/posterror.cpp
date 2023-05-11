// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// PostErrors.cpp
//
// This module contains the error handling/posting code for the engine.  It
// is assumed that all methods may be called by a dispatch client, and therefore
// errors are always posted using IErrorInfo.
//

//*****************************************************************************
#include "stdafx.h"                     // Standard header.

#ifndef FEATURE_UTILCODE_NO_DEPENDENCIES

#include <utilcode.h>                   // Utility helpers.
#include <corerror.h>
#include "../dlls/mscorrc/resource.h"
#include "ex.h"

#include <posterror.h>

// Local prototypes.
HRESULT FillErrorInfo(LPCWSTR szMsg, DWORD dwHelpContext);

void GetResourceCultureCallbacks(
        FPGETTHREADUICULTURENAMES* fpGetThreadUICultureNames,
        FPGETTHREADUICULTUREID* fpGetThreadUICultureId)
{
    WRAPPER_NO_CONTRACT;
    CCompRC::GetDefaultCallbacks(
        fpGetThreadUICultureNames,
        fpGetThreadUICultureId
    );
}
//*****************************************************************************
// Set callbacks to get culture info
//*****************************************************************************
void SetResourceCultureCallbacks(
    FPGETTHREADUICULTURENAMES fpGetThreadUICultureNames,
    FPGETTHREADUICULTUREID fpGetThreadUICultureId       // TODO: Don't rely on the LCID, only the name
)
{
    WRAPPER_NO_CONTRACT;
    CCompRC::SetDefaultCallbacks(
        fpGetThreadUICultureNames,
        fpGetThreadUICultureId
    );

}

//*****************************************************************************
// Public function to load a resource string
//*****************************************************************************
STDAPI UtilLoadStringRC(
    UINT iResourceID,
    _Out_writes_(iMax) LPWSTR szBuffer,
    int iMax,
    int bQuiet
)
{
    WRAPPER_NO_CONTRACT;
    return UtilLoadResourceString(bQuiet? CCompRC::Optional : CCompRC::Required,iResourceID, szBuffer, iMax);
}

HRESULT UtilLoadResourceString(CCompRC::ResourceCategory eCategory, UINT iResourceID, _Out_writes_ (iMax) LPWSTR szBuffer, int iMax)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT retVal = E_OUTOFMEMORY;

    SString::Startup();
    EX_TRY
    {
        CCompRC *pResourceDLL = CCompRC::GetDefaultResourceDll();

        if (pResourceDLL != NULL)
        {
            retVal = pResourceDLL->LoadString(eCategory, iResourceID, szBuffer, iMax);
        }
    }
    EX_CATCH
    {
        // Catch any errors and return E_OUTOFMEMORY;
        retVal = E_OUTOFMEMORY;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return retVal;
}

//*****************************************************************************
// Format a Runtime Error message.
//*****************************************************************************
static HRESULT FormatRuntimeErrorVA(
    _Inout_updates_(cchMsg) WCHAR       *rcMsg,                 // Buffer into which to format.
    ULONG       cchMsg,                 // Size of buffer, characters.
    HRESULT     hrRpt,                  // The HR to report.
    va_list     marker)                 // Optional args.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    WCHAR       rcBuf[512]; // Resource string.
    char        msgBufUtf8[2048];
    HRESULT     hr;

    // Ensure nul termination.
    *rcMsg = W('\0');

    // If this is one of our errors or if it is simply a resource ID, then grab the error from the rc file.
    if ((HRESULT_FACILITY(hrRpt) == FACILITY_URT) || (HIWORD(hrRpt) == 0))
    {
        hr = UtilLoadStringRC(LOWORD(hrRpt), rcBuf, ARRAY_SIZE(rcBuf), true);
        if (hr == S_OK)
        {
            hr = E_OUTOFMEMORY; // Out of memory is possible

            MAKE_UTF8PTR_FROMWIDE_NOTHROW(rcUtf8, rcBuf);
            if (rcUtf8 != NULL)
            {
                _vsnprintf_s(msgBufUtf8, ARRAY_SIZE(msgBufUtf8), _TRUNCATE, rcUtf8, marker);
                MAKE_WIDEPTR_FROMUTF8_NOTHROW(msgBuf, msgBufUtf8);
                if (msgBuf != NULL)
                {
                    hr = S_OK; // Performed all formatting allocations.
                    wcscpy_s(rcMsg, cchMsg, msgBuf);
                }
            }
        }
    }
    // Otherwise it isn't one of ours, so we need to see if the system can
    // find the text for it.
    else
    {
        if (WszFormatMessage(FORMAT_MESSAGE_FROM_SYSTEM,
                0, hrRpt, 0,
                rcMsg, cchMsg, 0/*<TODO>@todo: marker</TODO>*/))
        {
            hr = S_OK;

            // System messages contain a trailing \r\n, which we don't want normally.
            size_t iLen = u16_strlen(rcMsg);
            if (iLen > 3 && rcMsg[iLen - 2] == '\r' && rcMsg[iLen - 1] == '\n')
                rcMsg[iLen - 2] = '\0';
        }
        else
            hr = HRESULT_FROM_GetLastError();
    }

    _ASSERTE(SUCCEEDED(hr));
    return hrRpt;
} // FormatRuntimeErrorVA

#ifdef FEATURE_COMINTEROP
//*****************************************************************************
// Create, fill out and set an error info object.  Note that this does not fill
// out the IID for the error object; that is done elsewhere.
//*****************************************************************************
HRESULT FillErrorInfo(                  // Return status.
    LPCWSTR     szMsg,                  // Error message.
    DWORD       dwHelpContext)          // Help context.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ICreateErrorInfo *pICreateErr = NULL;      // Error info creation Iface pointer.
    IErrorInfo *pIErrInfo = NULL;       // The IErrorInfo interface.
    HRESULT     hr;                     // Return status.

    // Get the ICreateErrorInfo pointer.
    hr = S_OK;
    EX_TRY
    {
        hr = CreateErrorInfo(&pICreateErr);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (FAILED(hr))
        return (hr);

    // Set message text description.
    if (FAILED(hr = pICreateErr->SetDescription((LPWSTR) szMsg)))
        goto Exit1;

    // suppress PreFast warning about passing literal string to non-const API.
    // This API (ICreateErrorInfo::SetHelpFile) is documented to take a const argument, but
    // we can't put const in the signature because it would break existing implementors of
    // the API.
#ifdef _PREFAST_
#pragma prefast(push)
#pragma warning(disable:6298)
#endif

    // Set the help file and help context.
    //<TODO>@todo: we don't have a help file yet.</TODO>
    if (FAILED(hr = pICreateErr->SetHelpFile(const_cast<WCHAR*>(W("complib.hlp")))) ||
        FAILED(hr = pICreateErr->SetHelpContext(dwHelpContext)))
        goto Exit1;

#ifdef _PREFAST_
#pragma prefast(pop)
#endif

    // Get the IErrorInfo pointer.
    if (FAILED(hr = pICreateErr->QueryInterface(IID_IErrorInfo, (PVOID *) &pIErrInfo)))
        goto Exit1;

    // Save the error and release our local pointers.
    {
        // If we get here, we have loaded oleaut32.dll.
        CONTRACT_VIOLATION(ThrowsViolation);
        SetErrorInfo(0L, pIErrInfo);
    }

Exit1:
    pICreateErr->Release();
    if (pIErrInfo) {
        pIErrInfo->Release();
    }
    return hr;
}
#endif // FEATURE_COMINTEROP

//*****************************************************************************
// This function will post an error for the client.  If the LOWORD(hrRpt) can
// be found as a valid error message, then it is loaded and formatted with
// the arguments passed in.  If it cannot be found, then the error is checked
// against FormatMessage to see if it is a system error.  System errors are
// not formatted so no add'l parameters are required.  If any errors in this
// process occur, hrRpt is returned for the client with no error posted.
//*****************************************************************************
static HRESULT PostErrorVA(                      // Returned error.
    HRESULT     hrRpt,                  // Reported error.
    va_list     marker)                  // Error arguments.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP

    const DWORD cchMsg = 4096;
    WCHAR      *rcMsg = (WCHAR*)alloca(cchMsg * sizeof(WCHAR));             // Error message.
    HRESULT     hr;

    // Return warnings without text.
    if (!FAILED(hrRpt))
        goto ErrExit;

    // If we are already out of memory or out of stack or the thread is in some bad state,
    // we don't want throw gasoline on the fire by calling ErrorInfo stuff below (which can
    // trigger a delayload of oleaut32.dll). We don't need to embellish transient errors
    // so just return this without text.
    if (Exception::IsTransient(hrRpt))
    {
        goto ErrExit;
    }

    // Format the error.
    FormatRuntimeErrorVA(rcMsg, cchMsg, hrRpt, marker);

    // Turn the error into a posted error message.  If this fails, we still
    // return the original error message since a message caused by our error
    // handling system isn't going to give you a clue about the original error.
    hr = FillErrorInfo(rcMsg, LOWORD(hrRpt));
    _ASSERTE(hr == S_OK);

ErrExit:

#endif // FEATURE_COMINTEROP

    return (hrRpt);
} // PostErrorVA

#endif //!FEATURE_UTILCODE_NO_DEPENDENCIES

//*****************************************************************************
// This function will post an error for the client.  If the LOWORD(hrRpt) can
// be found as a valid error message, then it is loaded and formatted with
// the arguments passed in.  If it cannot be found, then the error is checked
// against FormatMessage to see if it is a system error.  System errors are
// not formatted so no add'l parameters are required.  If any errors in this
// process occur, hrRpt is returned for the client with no error posted.
//*****************************************************************************
HRESULT PostError(
    HRESULT hrRpt,      // Reported error.
    ...)                // Error arguments.
{
#ifndef FEATURE_UTILCODE_NO_DEPENDENCIES
    WRAPPER_NO_CONTRACT;
    va_list     marker;                 // User text.
    va_start(marker, hrRpt);
    hrRpt = PostErrorVA(hrRpt, marker);
    va_end(marker);
#endif //!FEATURE_UTILCODE_NO_DEPENDENCIES
    return hrRpt;
}
