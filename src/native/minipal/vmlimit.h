// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_VMLIMIT_H
#define HAVE_MINIPAL_VMLIMIT_H

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

// Returns the effective virtual address space limit for the current process
// by taking the minimum of RLIMIT_AS (if set and finite) and the VmallocTotal
// value from /proc/meminfo (on Linux).
// Returns SIZE_MAX if neither limit is available or applicable.
size_t minipal_get_virtual_address_space_limit(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_VMLIMIT_H
