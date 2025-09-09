// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpreter.h"
#include "intrinsics.h"

#define HAS_PREFIX(haystack, needle) \
    (strncmp(haystack, needle, strlen(needle)) == 0)

NamedIntrinsic GetNamedIntrinsic(COMP_HANDLE compHnd, CORINFO_METHOD_HANDLE compMethod, CORINFO_METHOD_HANDLE method)
{
    const char* className = NULL;
    const char* namespaceName = NULL;
    const char* enclosingClassNames[2] = {nullptr};
    const char* methodName = compHnd->getMethodNameFromMetadata(method, &className, &namespaceName, enclosingClassNames, ArrLen(enclosingClassNames));

    // Array methods don't have metadata
    if (!namespaceName)
        return NI_Illegal;

    if (!HAS_PREFIX(namespaceName, "System"))
        return NI_Illegal;

    if (!strcmp(namespaceName, "System"))
    {
        if (!strcmp(className, "Double") || !strcmp(className, "Single"))
        {
            if (!strcmp(methodName, "ConvertToIntegerNative"))
                return NI_PRIMITIVE_ConvertToIntegerNative;
            else if (!strcmp(methodName, "MultiplyAddEstimate"))
                return NI_System_Math_MultiplyAddEstimate;
        }
        else if (!strcmp(className, "Math") || !strcmp(className, "MathF"))
        {
            if (!strcmp(methodName, "ReciprocalEstimate"))
                return NI_System_Math_ReciprocalEstimate;
            else if (!strcmp(methodName, "ReciprocalSqrtEstimate"))
                return NI_System_Math_ReciprocalSqrtEstimate;
        }
    }
    else if (!strcmp(namespaceName, "System.StubHelpers"))
    {
        if (!strcmp(className, "StubHelpers"))
        {
            if (!strcmp(methodName, "NextCallReturnAddress"))
                return NI_System_StubHelpers_NextCallReturnAddress;
            else if (!strcmp(methodName, "GetStubContext"))
                return NI_System_StubHelpers_GetStubContext;
        }
    }
    else if (!strcmp(namespaceName, "System.Numerics"))
    {
        if (!strcmp(className, "Vector") && !strcmp(methodName, "get_IsHardwareAccelerated"))
            return NI_IsSupported_False;

        // Fall back to managed implementation for everything else.
        return NI_Illegal;
    }
    else if (!strcmp(namespaceName, "System.Runtime.Intrinsics"))
    {
        // Vector128<T> etc
        if (HAS_PREFIX(className, "Vector") && !strcmp(methodName, "get_IsHardwareAccelerated"))
            return NI_IsSupported_False;

        // Fall back to managed implementation for everything else.
        return NI_Illegal;
    }
    else if (HAS_PREFIX(namespaceName, "System.Runtime.Intrinsics"))
    {
        // Architecture-specific intrinsics.
        if (!strcmp(methodName, "get_IsSupported"))
            return NI_IsSupported_False;

        // Every intrinsic except IsSupported is PNSE in interpreted-only mode.
        return NI_Throw_PlatformNotSupportedException;
    }
    else if (HAS_PREFIX(namespaceName, "System.Runtime"))
    {
        if (!strcmp(namespaceName, "System.Runtime.CompilerServices"))
        {
            if (!strcmp(className, "StaticsHelpers"))
            {
                if (!strcmp(methodName, "VolatileReadAsByref"))
                    return NI_System_Runtime_CompilerServices_StaticsHelpers_VolatileReadAsByref;
            }
            else if (!strcmp(className, "RuntimeHelpers"))
            {
                if (!strcmp(methodName, "IsReferenceOrContainsReferences"))
                    return NI_System_Runtime_CompilerServices_RuntimeHelpers_IsReferenceOrContainsReferences;
                else if (!strcmp(methodName, "GetMethodTable"))
                    return NI_System_Runtime_CompilerServices_RuntimeHelpers_GetMethodTable;
            }
        }
        else if (!strcmp(namespaceName, "System.Runtime.InteropServices"))
        {
            if (!strcmp(className, "MemoryMarshal"))
            {
                if (!strcmp(methodName, "GetArrayDataReference"))
                    return NI_System_Runtime_InteropService_MemoryMarshal_GetArrayDataReference;
            }
        }
    }
    else if (!strcmp(namespaceName, "System.Threading"))
    {
        if (!strcmp(className, "Interlocked"))
        {
            if (!strcmp(methodName, "CompareExchange"))
                return NI_System_Threading_Interlocked_CompareExchange;
            else if (!strcmp(methodName, "Exchange"))
                return NI_System_Threading_Interlocked_Exchange;
            else if (!strcmp(methodName, "MemoryBarrier"))
                return NI_System_Threading_Interlocked_MemoryBarrier;
        }
        else if (!strcmp(className, "Thread"))
        {
            if (!strcmp(methodName, "FastPollGC"))
                return NI_System_Threading_Thread_FastPollGC;
        }
        else if (!strcmp(className, "Volatile"))
        {
            if (!strcmp(methodName, "ReadBarrier"))
                return NI_System_Threading_Volatile_ReadBarrier;
            else if (!strcmp(methodName, "WriteBarrier"))
                return NI_System_Threading_Volatile_WriteBarrier;
        }
    }

    return NI_Illegal;
}
