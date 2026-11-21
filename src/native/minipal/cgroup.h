// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CGROUP_H
#define HAVE_MINIPAL_CGROUP_H

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

// Finds the cgroups of the current process. The functions below call this when needed. It is not
// thread-safe, so the runtime calls it during startup. Does nothing if it has already been called.
void minipal_cgroup_initialize(void);

// Frees the state allocated by minipal_cgroup_initialize.
void minipal_cgroup_cleanup(void);

// Gets the CPU limit of the current process in CPUs, rounded up. Returns false if there is no limit.
bool minipal_cgroup_get_cpu_limit(uint32_t* val);

// Gets the memory limit of the current process in bytes. The value can be very large when the memory
// is not limited. Returns false if it is not available.
bool minipal_cgroup_get_memory_limit(uint64_t* val);

// Gets the memory usage of the cgroup of the current process in bytes, excluding inactive file pages,
// the same way as container tools. Returns false if it is not available.
bool minipal_cgroup_get_memory_usage(uint64_t* val);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_CGROUP_H
