// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "minipalconfig.h"

#if HAVE_RESOURCE_H
#include <sys/resource.h>
#endif

#include <limits.h>

#include "descriptorlimit.h"

bool minipal_increase_descriptor_limit(void)
{
#ifdef __wasm__
    // WebAssembly cannot set limits
#elif TARGET_LINUX_MUSL
    // Setting RLIMIT_NOFILE breaks debugging of coreclr on Alpine Linux for some reason
#elif HAVE_RESOURCE_H
    struct rlimit rlp;
    int result;

    result = getrlimit(RLIMIT_NOFILE, &rlp);
    if (result != 0)
    {
        return false;
    }
    // Set our soft limit for file descriptors to be the same
    // as the max limit.
    rlp.rlim_cur = rlp.rlim_max;
#ifdef __APPLE__
    // Based on compatibility note in setrlimit(2) manpage for OSX,
    // trim the limit to OPEN_MAX.
    if (rlp.rlim_cur > OPEN_MAX)
    {
        rlp.rlim_cur = OPEN_MAX;
    }
#endif
    result = setrlimit(RLIMIT_NOFILE, &rlp);
    if (result != 0)
    {
        return false;
    }
#endif
    return true;
}
