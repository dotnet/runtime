// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __RTLFUNCTIONS_H__
#define __RTLFUNCTIONS_H__

#ifdef FEATURE_EH_FUNCLETS

enum EEDynamicFunctionTableType
{
    DYNFNTABLE_JIT = 0,
    DYNFNTABLE_STUB = 1,
    DYNFNTABLE_INVALID = -1,

    DYNFNTABLE_FIRST = DYNFNTABLE_JIT,
    DYNFNTABLE_LAST = DYNFNTABLE_STUB,
};

// Used by OutOfProcessFunctionTableCallback in DLLS\mscordbg\DebugSupport.cpp
// to figure out how to parse a dynamic function table that was registered
// with a callback.
inline
EEDynamicFunctionTableType IdentifyDynamicFunctionTableTypeFromContext (PVOID pvContext)
{
    EEDynamicFunctionTableType type = (EEDynamicFunctionTableType)((SIZE_T)pvContext & 3);
    if (type < DYNFNTABLE_FIRST || type > DYNFNTABLE_LAST)
        type = DYNFNTABLE_INVALID;
    return type;
}

inline
PVOID EncodeDynamicFunctionTableContext (PVOID pvContext, EEDynamicFunctionTableType type)
{
    _ASSERTE(type >= DYNFNTABLE_FIRST && type <= DYNFNTABLE_LAST);
    return (PVOID)((SIZE_T)pvContext | type);
}

inline
PVOID DecodeDynamicFunctionTableContext (PVOID pvContext)
{
    return (PVOID)((SIZE_T)pvContext & ~3);
}


#define DYNAMIC_FUNCTION_TABLE_MAX_RANGE INT32_MAX

#endif // FEATURE_EH_FUNCLETS


#if defined(FEATURE_EH_FUNCLETS) && !defined(DACCESS_COMPILE) && !defined(TARGET_UNIX)

// Wrapper for RtlInstallFunctionTableCallback.
VOID InstallEEFunctionTable(
        PVOID pvTableID,
        PVOID pvStartRange,
        ULONG cbRange,
        PGET_RUNTIME_FUNCTION_CALLBACK pfnGetRuntimeFunctionCallback,
        PVOID pvContext,
        EEDynamicFunctionTableType TableType);

inline
VOID DeleteEEFunctionTable(
        PVOID pvTableID)
{
    RtlDeleteFunctionTable((PT_RUNTIME_FUNCTION)((ULONG64)pvTableID | 3));
}

#else // FEATURE_EH_FUNCLETS && !DACCESS_COMPILE && !TARGET_UNIX

#define InstallEEFunctionTable(pvTableID, pvStartRange, cbRange, pfnGetRuntimeFunctionCallback, pvContext, TableType) do { } while (0)
#define DeleteEEFunctionTable(pvTableID) do { } while (0)

#endif // FEATURE_EH_FUNCLETS && !DACCESS_COMPILE && !TARGET_UNIX


#endif // !__RTLFUNCTIONS_H__
