// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <platformdefines.h>

typedef void (*PFNACTION1)();
extern "C" DLL_EXPORT void InvokeCallbackCatchAndRethrow(PFNACTION1 callback1, PFNACTION1 callback2)
{
    try
    {
        callback1();
    }
    catch (int ex)
    {
        callback2();
        printf("Caught exception %d in native code, rethrowing\n", ex);
        throw;
    }
}

typedef void (*PFNACTION1)();
extern "C" DLL_EXPORT void NativeThrow(PFNACTION1 callback)
{
    throw 1;
}
