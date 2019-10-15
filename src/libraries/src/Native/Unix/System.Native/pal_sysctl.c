// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"

// These functions are only used for platforms which support
// using sysctl to gather system information.

#if HAVE_SYS_SYSCTL_H

#include "pal_utilities.h"
#include "pal_sysctl.h"
#include "pal_errno.h"
#include "pal_safecrt.h"

#include <errno.h>

#include <sys/types.h>
#include <sys/sysctl.h>

int32_t SystemNative_Sysctl(int* name, unsigned int namelen, void* value, size_t* len)
{
    void* newp = NULL;
    size_t newlen = 0;

#if defined(__linux__) || defined(_WASM_)
    return sysctl(name, (int)(namelen), value, len, newp, newlen);
#else
    return sysctl(name, namelen, value, len, newp, newlen);
#endif
}

#endif // HAVE_SYSCTL_H
