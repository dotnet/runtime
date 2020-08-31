// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_runtimeinformation.h"
#include "pal_types.h"
#include <stdio.h>
#include <string.h>
#include <sys/utsname.h>

const char* SystemNative_GetUnixName()
{
    return PAL_UNIX_NAME;
}

char* SystemNative_GetUnixRelease()
{
    struct utsname _utsname;
    return uname(&_utsname) != -1 ?
        strdup(_utsname.release) :
        NULL;
}

int32_t SystemNative_GetUnixVersion(char* version, int* capacity)
{
    struct utsname _utsname;
    if (uname(&_utsname) != -1)
    {
        int r = snprintf(version, (size_t)(*capacity), "%s %s %s", _utsname.sysname, _utsname.release, _utsname.version);
        if (r > *capacity)
        {
            *capacity = r + 1;
            return -1;
        }
    }

    return 0;
}

/* Returns an int representing the OS Architecture:
 0 - x86
 1 - x64
 2 - ARM
 3 - ARM64
 4 - WASM */
int32_t SystemNative_GetOSArchitecture()
{
#if defined(TARGET_ARM)
    return ARCH_ARM;
#elif defined(TARGET_ARM64)
    return ARCH_ARM64;
#elif defined(TARGET_AMD64)
    return ARCH_X64;
#elif defined(TARGET_X86)
    return ARCH_X86;
#elif defined(TARGET_WASM)
    return ARCH_WASM;
#else
#error Unidentified Architecture
#endif
}

/* Returns an int representing the OS Architecture:
0 - x86
1 - x64
2 - ARM
3 - ARM64
4 - WASM */
int32_t SystemNative_GetProcessArchitecture()
{
#if defined(TARGET_ARM)
    return ARCH_ARM;
#elif defined(TARGET_ARM64)
    return ARCH_ARM64;
#elif defined(TARGET_AMD64)
    return ARCH_X64;
#elif defined(TARGET_X86)
    return ARCH_X86;
#elif defined(TARGET_WASM)
    return ARCH_WASM;
#else
#error Unidentified Architecture
#endif
}
