// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdio.h>
#include <platformdefines.h>
#ifndef TARGET_WINDOWS
#include <dlfcn.h>
#endif

extern "C" DLL_EXPORT int NativeSum(int a, int b)
{
    return a + b;
}
