// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Stub sys/resource.h wrapper for WASI — provides rlimit types.
// The WASI SDK has sys/resource.h but rlimit is hidden behind
// __wasilibc_unmodified_upstream. We provide the types directly.

#ifndef _WASI_SYS_RESOURCE_H
#define _WASI_SYS_RESOURCE_H

#include_next <sys/resource.h>

#ifndef RLIMIT_AS

typedef unsigned long long rlim_t;

struct rlimit {
    rlim_t rlim_cur;
    rlim_t rlim_max;
};

#define RLIM_INFINITY (~0ULL)
#define RLIMIT_AS      9
#define RLIMIT_FSIZE   1

static inline int getrlimit(int resource, struct rlimit *rlim) {
    rlim->rlim_cur = RLIM_INFINITY;
    rlim->rlim_max = RLIM_INFINITY;
    return 0;
}

#endif // RLIMIT_AS

#endif // _WASI_SYS_RESOURCE_H
