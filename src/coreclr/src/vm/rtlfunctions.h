//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



#ifndef __RTLFUNCTIONS_H__
#define __RTLFUNCTIONS_H__

#ifdef WIN64EXCEPTIONS

enum EEDynamicFunctionTableType
{
    DYNFNTABLE_JIT = 0,
    DYNFNTABLE_STUB = 1,
    DYNFNTABLE_INVALID = -1,

    DYNFNTABLE_FIRST = DYNFNTABLE_JIT,
    DYNFNTABLE_LAST = DYNFNTABLE_STUB,
};

// Used by OutOfProcessFunctionTableCallback in DLLS\mscordbg\DebugSupport.cpp
// to figure out how to parse a dunamic function table that was registered
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


#define DYNAMIC_FUNCTION_TABLE_MAX_RANGE LONG_MAX

#endif // WIN64EXCEPTIONS


#if defined(WIN64EXCEPTIONS) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(FEATURE_PAL)

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
    RtlDeleteFunctionTable((PRUNTIME_FUNCTION)((ULONG64)pvTableID | 3));
}

#else // WIN64EXCEPTIONS && !DACCESS_COMPILE && !CROSSGEN_COMPILE && !FEATURE_PAL

#define InstallEEFunctionTable(pvTableID, pvStartRange, cbRange, pfnGetRuntimeFunctionCallback, pvContext, TableType) do { } while (0)
#define DeleteEEFunctionTable(pvTableID) do { } while (0)

#endif // WIN64EXCEPTIONS && !DACCESS_COMPILE && !CROSSGEN_COMPILE && !FEATURE_PAL


#endif // !__RTLFUNCTIONS_H__
