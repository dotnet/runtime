// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_dynamicload.h"
#include "pal_utilities.h"

#include <dlfcn.h>
#include <string.h>

void* SystemNative_LoadLibrary(const char* filename)
{
    // Check whether we have been requested to load 'libc'. If that's the case, then use the
    // correct file name based on the current platform.
    if (strcmp(filename, "libc") == 0)
    {
        filename = LIBC_FILENAME;
    }

    return dlopen(filename, RTLD_LAZY);
}

void* SystemNative_GetProcAddress(void* handle, const char* symbol)
{
    // We're not trying to disambiguate between "symbol was not found" and "symbol found, but
    // the value is null". .NET does not define a behavior for DllImports of null entrypoints,
    // so we might as well take the "not found" path on the managed side.
    return dlsym(handle, symbol);
}

void SystemNative_FreeLibrary(void* handle)
{
    dlclose(handle);
}
