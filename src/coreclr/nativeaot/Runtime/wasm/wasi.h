// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef	_WASI_H
#define	_WASI_H

#include <sys/resource.h>

typedef unsigned long long rlim_t;

struct rlimit {
	rlim_t rlim_cur;
	rlim_t rlim_max;
};

#define RLIM_INFINITY (~0ULL)
#define RLIMIT_AS      9

inline int getrlimit (int resource_id, struct rlimit * ret_rlimit)
{
    // TODO-LLVM: ifdef out the callers.
    ret_rlimit->rlim_cur = RLIM_INFINITY;
    ret_rlimit->rlim_max = RLIM_INFINITY;
    return 0;
}
#endif // _WASI_H
