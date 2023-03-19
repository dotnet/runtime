// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#define DLL_EXPORT extern "C" __attribute((visibility("default")))
#endif
#include "stdio.h"

extern "C" void ReversePInvokeEntry();

DLL_EXPORT void PInvokeEntry()
{
    printf("Hello from PInvokeEntry\n");
    ReversePInvokeEntry();
}