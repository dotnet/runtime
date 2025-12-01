// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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


#ifdef HOST_AMD64

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

#else // HOST_AMD64

HRESULT EnsureRtlFunctions()
{
    LIMITED_METHOD_CONTRACT;
    return S_OK;
}

#endif // HOST_AMD64

#ifndef HOST_X86

#define DYNAMIC_FUNCTION_TABLE_MAX_RANGE INT32_MAX

VOID InstallEEFunctionTable (
        PVOID pvTableID,
        PVOID pvStartRange,
        ULONG cbRange,
        PGET_RUNTIME_FUNCTION_CALLBACK pfnGetRuntimeFunctionCallback,
        PVOID pvContext)
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

    if (wszModuleName == NULL)
    {
        StackSString ssTempName;

        IfFailThrow(GetClrModuleDirectory(ssTempName));

        ssTempName.Append(MAIN_DAC_MODULE_DLL_NAME_W);

        NewArrayHolder<WCHAR> wzTempName(ssTempName.GetCopyOfUnicodeString());

        // publish result
        if (InterlockedCompareExchangeT(&wszModuleName, (LPWSTR)wzTempName, nullptr) == nullptr)
        {
            wzTempName.SuppressRelease();
        }
    }

    if (!RtlInstallFunctionTableCallback(
            ((ULONG_PTR)pvTableID) | 3,  // the low 2 bits must be set so NT knows
                                         // it's not really a pointer.  See
                                         // DeleteEEFunctionTable.
            (ULONG_PTR)pvStartRange,
            cbRange,
            pfnGetRuntimeFunctionCallback,
            pvContext,
            wszModuleName))
    {
        COMPlusThrowOM();
    }
}

#endif // HOST_X86
