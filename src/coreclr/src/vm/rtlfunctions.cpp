// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// RtlFunctions.CPP
//

//
// Various functions for interacting with ntdll.
//
//

// Precompiled Header

#include "common.h"

#include "rtlfunctions.h"


#ifdef _TARGET_AMD64_

RtlVirtualUnwindFn*                 RtlVirtualUnwind_Unsafe         = NULL;

HRESULT EnsureRtlFunctions()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HMODULE hModuleNtDll = CLRLoadLibrary(W("ntdll"));

    if (hModuleNtDll == NULL)
        return E_FAIL;

#define ENSURE_FUNCTION_RENAME(clrname, ntname)   \
    if (NULL == clrname) { clrname = (ntname##Fn*)GetProcAddress(hModuleNtDll, #ntname); } \
    if (NULL == clrname) { return E_FAIL; } \
    { }

    ENSURE_FUNCTION_RENAME(RtlVirtualUnwind_Unsafe, RtlVirtualUnwind       );

    return S_OK;
}

#else // _TARGET_AMD64_

HRESULT EnsureRtlFunctions()
{
    LIMITED_METHOD_CONTRACT;
    return S_OK;
}

#endif // _TARGET_AMD64_

#if defined(WIN64EXCEPTIONS)

VOID InstallEEFunctionTable (
        PVOID pvTableID,
        PVOID pvStartRange,
        ULONG cbRange,
        PGET_RUNTIME_FUNCTION_CALLBACK pfnGetRuntimeFunctionCallback,
        PVOID pvContext,
        EEDynamicFunctionTableType TableType)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(cbRange <= DYNAMIC_FUNCTION_TABLE_MAX_RANGE);
    }
    CONTRACTL_END;

    static LPWSTR wszModuleName = NULL;
    static WCHAR  rgwModuleName[MAX_LONGPATH] = {0};

    if (wszModuleName == NULL)        
    {
        WCHAR rgwTempName[MAX_LONGPATH] = {0};
        DWORD dwTempNameSize = MAX_LONGPATH;

        // Leaves trailing backslash on path, producing something like "c:\windows\microsoft.net\framework\v4.0.x86dbg\"
        HRESULT hr = GetInternalSystemDirectory(rgwTempName, &dwTempNameSize);

        //finish creating complete path and copy to buffer if we can
        if (FAILED(hr) ||
            (wcscat_s(rgwTempName, MAX_LONGPATH, MAIN_DAC_MODULE_DLL_NAME_W) != 0) ||
            (wcscpy_s(rgwModuleName, MAX_LONGPATH, rgwTempName) != 0))
        {   // The CLR should become unavailable in this case.
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }

        // publish result
        InterlockedExchangeT(&wszModuleName, rgwModuleName);
    }

    if (!RtlInstallFunctionTableCallback(
            ((ULONG_PTR)pvTableID) | 3,  // the low 2 bits must be set so NT knows
                                         // it's not really a pointer.  See
                                         // DeleteEEFunctionTable.
            (ULONG_PTR)pvStartRange,
            cbRange,
            pfnGetRuntimeFunctionCallback,
            EncodeDynamicFunctionTableContext(pvContext, TableType),
            wszModuleName))
    {
        COMPlusThrowOM();
    }
}

#endif // WIN64EXCEPTIONS

