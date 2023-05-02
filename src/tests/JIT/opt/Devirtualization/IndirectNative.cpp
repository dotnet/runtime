// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

#ifdef TARGET_WINDOWS
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#define DLL_EXPORT extern "C" __attribute((visibility("default")))
#endif

#ifndef TARGET_WINDOWS
#define __stdcall
#endif

DLL_EXPORT int32_t __stdcall E()
{
    return 4;
}

DLL_EXPORT int32_t __stdcall EParams(int32_t a, int32_t b)
{
    return a + b + 4;
}

DLL_EXPORT int32_t __stdcall EPtrs(int32_t* a, int32_t* b)
{
    return *a + *b + 4;
}
