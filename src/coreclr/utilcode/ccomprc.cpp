// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"                     // Standard header.
#include <utilcode.h>                   // Utility helpers.
#include <corerror.h>

#include "../dlls/mscorrc/resource.h"
#ifdef HOST_UNIX
#include "resourcestring.h"
#define NATIVE_STRING_RESOURCE_NAME mscorrc
__attribute__((visibility("default"))) DECLARE_NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME);
#endif

#include <stdlib.h>

// External prototypes.
extern void* GetClrModuleBase();

HRESULT CCompRC::LoadString(UINT iResourceID, _Out_writes_(iMax) LPWSTR szBuffer, int iMax,  int *pcwchUsed)
{
#ifdef DACCESS_COMPILE
    return E_NOTIMPL;
#else
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

#ifdef HOST_WINDOWS
    HRESULT         hr;
    int length;

    length = ::LoadString((HINSTANCE)GetClrModuleBase(), iResourceID, szBuffer, iMax);
    if(length > 0)
    {
        if(pcwchUsed)
        {
            *pcwchUsed = length;
        }
        return (S_OK);
    }
    if(SUCCEEDED(GetLastError()))
        hr=HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
    else
        hr=HRESULT_FROM_GetLastError();

    // Return an empty string to save the people with a bad error handling
    if (szBuffer && iMax)
        *szBuffer = W('\0');

    return hr;
#else // HOST_WINDOWS
    return LoadNativeStringResource(NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME), iResourceID,
      szBuffer, iMax, pcwchUsed);
#endif // HOST_WINDOWS
#endif
}
