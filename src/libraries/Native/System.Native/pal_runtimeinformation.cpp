// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_runtimeinformation.h"
#include "pal_types.h"
#include <stdio.h>
#include <sys/utsname.h>

extern "C" int32_t GetUnixVersion(char* version, int* capacity)
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
 2 - ARM */
extern "C" int32_t GetUnixArchitecture()
{
#if defined(ARM)
    return ARCH_ARM;
#elif defined(X64)
    return ARCH_X64;
#elif defined(X86)
    return ARCH_X86;
#error Unidentified Architecture
#endif
}
