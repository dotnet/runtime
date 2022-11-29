// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_dynamicload.h"

#if HAVE_DLFCN_H
#include <dlfcn.h>
#endif
#include <string.h>
#include <stdio.h>

#if HAVE_GNU_LIBNAMES_H
#include <gnu/lib-names.h>
#endif

void* SystemNative_LoadLibrary(const char* filename)
{
#if !defined(TARGET_WASI)
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
#else /* TARGET_WASI */
    return NULL;
#endif /* TARGET_WASI */
}

void* SystemNative_GetLoadLibraryError(void)
{
#if !defined(TARGET_WASI)
    return dlerror();
#else /* TARGET_WASI */
    return NULL;
#endif /* TARGET_WASI */
}

void* SystemNative_GetProcAddress(void* handle, const char* symbol)
{
#if !defined(TARGET_WASI)
    // We're not trying to disambiguate between "symbol was not found" and "symbol found, but
    // the value is null". .NET does not define a behavior for DllImports of null entrypoints,
    // so we might as well take the "not found" path on the managed side.
    return dlsym(handle, symbol);
#else /* TARGET_WASI */
    return NULL;
#endif /* TARGET_WASI */
}

void SystemNative_FreeLibrary(void* handle)
{
#if !defined(TARGET_WASI)
    dlclose(handle);
#endif /* TARGET_WASI */
}

#if defined TARGET_ANDROID || TARGET_WASI
void* SystemNative_GetDefaultSearchOrderPseudoHandle(void)
{
    return NULL;
}
#else
static void* volatile g_defaultSearchOrderPseudoHandle = NULL;
void* SystemNative_GetDefaultSearchOrderPseudoHandle(void)
{
    // Read the value once from the volatile static to avoid reading from memory twice.
    void* defaultSearchOrderPseudoHandle = (void*)g_defaultSearchOrderPseudoHandle;
    if (defaultSearchOrderPseudoHandle == NULL)
    {
#ifdef TARGET_ANDROID
        int flag = RTLD_NOW;
#else
        int flag = RTLD_LAZY;
#endif

        // Assign back to the static as well as the local here.
        // We don't need to check for a race between two threads as the value returned by
        // dlopen here will always be the same in a given environment.
        g_defaultSearchOrderPseudoHandle = defaultSearchOrderPseudoHandle = dlopen(NULL, flag);
    }
    return defaultSearchOrderPseudoHandle;
}
