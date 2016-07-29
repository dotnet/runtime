// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_runtimeextensions.h"
#include "pal_types.h"
#include <stdio.h>
#include <sys/utsname.h>

extern "C" int32_t SystemNative_GetNodeName(char* version, int* capacity)
{
    struct utsname _utsname;
    if (uname(&_utsname) != -1)
    {
        int r = snprintf(version, static_cast<size_t>(*capacity), "%s", _utsname.nodename);
        if (r > *capacity)
        {
            *capacity = r + 1;
            return -1;
        }
    }

    return 0;
}
