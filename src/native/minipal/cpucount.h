// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUCOUNT_H
#define HAVE_MINIPAL_CPUCOUNT_H

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

// Returns the maximum number of CPUs that could ever be available on this system,
// suitable for sizing cpu_set_t allocations via CPU_ALLOC.
//
// On Linux, this reads /sys/devices/system/cpu/possible to account for CPU hotplug.
// This may be larger than the number of online or present CPUs.
// Falls back to sysconf(_SC_NPROCESSORS_CONF) if the sysfs file is unavailable.
int minipal_get_cpu_max_possible_count(void);

// Returns the number of CPUs physically present in the system, regardless of
// whether they are currently online or offline or -1 if unavailable. This is the
// system core count and does not account for per-process affinity / cgroup limits.
//
// On Linux, this reads /sys/devices/system/cpu/present.
int minipal_get_cpu_present_count(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_CPUCOUNT_H
