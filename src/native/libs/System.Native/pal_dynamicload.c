// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_dynamicload.h"

#include <dlfcn.h>
#include <string.h>

#if HAVE_GNU_LIBNAMES_H
#include <gnu/lib-names.h>
#endif

void* SystemNative_LoadLibrary(const char* filename)
{
    // Check whether we have been requested to load 'libc'. If that's the case, then:
    // * For Linux, use the full name of the library that is defined in <gnu/lib-names.h> by the
    //   LIBC_SO constant. The problem is that calling dlopen("libc.so") will fail for libc even
    //   though it works for other libraries. The reason is that libc.so is just linker script
    //   (i.e. a test file).
    //   As a result, we have to use the full name (i.e. lib.so.6) that is defined by LIBC_SO.
    // * For macOS, use constant value absolute path "/usr/lib/libc.dylib".
    // * For FreeBSD, use constant value "libc.so.7".
    // * For rest of Unices, use constant value "libc.so".
    if (strcmp(filename, "libc") == 0)
    {
#if defined(__APPLE__)
        filename = "/usr/lib/libc.dylib";
#elif defined(__FreeBSD__)
        filename = "libc.so.7";
#elif defined(LIBC_SO)
        filename = LIBC_SO;
#else
        filename = "libc.so";
#endif
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
