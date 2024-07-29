// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>
#include <stdint.h>
#ifdef _WIN32
#include <memoryapi.h>
#else
#include <sys/mman.h>
#endif

#ifndef _WIN32
extern "C" DLL_EXPORT int GetMapAnonymousFlag()
{
    return MAP_ANONYMOUS;
}
#endif