// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __RTLFUNCTIONS_H__
#define __RTLFUNCTIONS_H__


#if !defined(DACCESS_COMPILE) && defined(HOST_WINDOWS) && !defined(HOST_X86)

// Wrapper for RtlInstallFunctionTableCallback.
VOID InstallEEFunctionTable(
        PVOID pvTableID,
        PVOID pvStartRange,
        ULONG cbRange,
        PGET_RUNTIME_FUNCTION_CALLBACK pfnGetRuntimeFunctionCallback,
        PVOID pvContext);

inline
VOID DeleteEEFunctionTable(
        PVOID pvTableID)
{
    RtlDeleteFunctionTable((PT_RUNTIME_FUNCTION)((ULONG64)pvTableID | 3));
}

#else

#define InstallEEFunctionTable(pvTableID, pvStartRange, cbRange, pfnGetRuntimeFunctionCallback, pvContext) do { } while (0)
#define DeleteEEFunctionTable(pvTableID) do { } while (0)

#endif


#endif // !__RTLFUNCTIONS_H__
