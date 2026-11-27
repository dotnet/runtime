// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUCOUNT_H
#define HAVE_MINIPAL_CPUCOUNT_H

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Get the maximum number of CPUs that could ever be available on this system,
 * suitable for sizing cpu_set_t allocations via CPU_ALLOC.
 *
 * On Linux, this reads /sys/devices/system/cpu/possible to account for CPU hotplug.
 * This may be larger than the number of online or present CPUs.
 * Falls back to sysconf(_SC_NPROCESSORS_CONF) if the sysfs file is unavailable.
 *
 * @return The maximum number of possible CPUs.
 */
int minipal_get_cpu_max_possible_count(void);

/**
 * Get the number of CPUs physically present in the system, regardless of
 * whether they are currently online or offline. This is the
 * system core count and does not account for per-process affinity / cgroup limits.
 *
 * On Linux, this reads /sys/devices/system/cpu/present.
 *
 * @return The number of present CPUs, or -1 if unavailable.
 */
int minipal_get_cpu_present_count(void);

/**
 * Get the number of CPUs in the system. This is the online count, except on
 * architectures where cores are commonly taken offline to save power, where it is the configured count.
 *
 * @return The number of CPUs, at least 1.
 */
int minipal_get_cpu_total_count(void);

/**
 * Get the number of CPUs in the affinity mask of the current process.
 * On Android this is the number of present CPUs, since cores there are commonly offline.
 * Falls back to minipal_get_cpu_total_count if the affinity mask cannot be queried.
 *
 * @return The number of CPUs, at least 1.
 */
int minipal_get_cpu_affinity_count(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_CPUCOUNT_H
