// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_runtimeinformation.h"
#include "pal_types.h"
#include <stdio.h>
#include <string.h>
#include <sys/utsname.h>

extern "C" const char* SystemNative_GetUnixName()
{
    return PAL_UNIX_NAME;
}

extern "C" char* SystemNative_GetUnixRelease()
{
    struct utsname _utsname;
    return uname(&_utsname) != -1 ?
        strdup(_utsname.release) :
        nullptr;
}

extern "C" int32_t SystemNative_GetUnixVersion(char* version, int* capacity)
{
    struct utsname _utsname;
    if (uname(&_utsname) != -1)
    {
        int r = snprintf(version, static_cast<size_t>(*capacity), "%s %s %s", _utsname.sysname, _utsname.release, _utsname.version);
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
 3 - ARM64 */
extern "C" int32_t SystemNative_GetOSArchitecture()
{
#if defined(_ARM_)
    return ARCH_ARM;
#elif defined(_ARM64_)
    return ARCH_ARM64;
#elif defined(_AMD64_)
    return ARCH_X64;
#elif defined(_X86_)
    return ARCH_X86;
#else
#error Unidentified Architecture
#endif
}

/* Returns an int representing the OS Architecture:
0 - x86
1 - x64
2 - ARM
3 - ARM64 */
extern "C" int32_t SystemNative_GetProcessArchitecture()
{
#if defined(_ARM_)
    return ARCH_ARM;
#elif defined(_ARM64_)
    return ARCH_ARM64;
#elif defined(_AMD64_)
    return ARCH_X64;
#elif defined(_X86_)
    return ARCH_X86;
#else
#error Unidentified Architecture
#endif
}
