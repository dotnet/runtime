// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
