// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loads the LTTng tracepoint provider (libcoreclrtraceptprovider.so) from the directory of the
// module that contains this code (libcoreclr.so or the single-file host).

#include <dlfcn.h>
#include <errno.h>
#include <limits.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

static const char tpLibName[] = "libcoreclrtraceptprovider.so";

// DOTNET_LTTng=0 disables loading the provider. This env var is formally declared in clrconfigvalues.h,
// but this code runs too early to use the mechanics that come with that definition.
static bool ShouldLoadTracepointProvider()
{
    const char* value = getenv("DOTNET_LTTng");
    if (value == nullptr)
    {
        value = getenv("COMPlus_LTTng");
    }

    if (value == nullptr)
    {
        return true;
    }

    errno = 0;
    char* end;
    unsigned long result = strtoul(value, &end, 10);
    if (end == value || errno == ERANGE || (uint32_t)result != result)
    {
        return true;
    }

    return result != 0;
}

/*++

NOTE: InitializeTracepointProvider MUST NOT depend on the PAL as it runs prior to PAL initialization.

Constructor priority is set to 200, which allows for constructors to
guarantee that they run before or after this constructor by setting
their priority appropriately.

Priority values must be greater than 100.  The lower the value,
the higher the priority.

--*/
__attribute__((__unused__))
__attribute__((constructor (200)))
static void
InitializeTracepointProvider(void)
{
    if (!ShouldLoadTracepointProvider())
    {
        return;
    }

    // Get the path to the currently executing shared object.
    Dl_info info;
    if (!dladdr((void *)InitializeTracepointProvider, &info) || info.dli_fname == nullptr)
    {
        return;
    }

    const char* lastSlash = strrchr(info.dli_fname, '/');
    if (lastSlash == nullptr)
    {
        return;
    }

    // Replace the shared object name with the tracepoint provider's.
    char tpProvPath[PATH_MAX];
    size_t directoryLength = (size_t)(lastSlash - info.dli_fname) + 1;
    if (directoryLength + sizeof(tpLibName) > sizeof(tpProvPath))
    {
        return;
    }

    memcpy(tpProvPath, info.dli_fname, directoryLength);
    memcpy(tpProvPath + directoryLength, tpLibName, sizeof(tpLibName));

    // It's OK if this fails - that just means that tracing dependencies aren't available.
    dlopen(tpProvPath, RTLD_NOW | RTLD_GLOBAL);
}
