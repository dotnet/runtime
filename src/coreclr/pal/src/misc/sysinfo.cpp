// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    sysinfo.cpp

Abstract:

    Implements the CPU count queries.

--*/

#include "pal/palinternal.h"

#include <sched.h>
#include <errno.h>
#include <unistd.h>
#include <minipal/cpucount.h>

#if HAVE_SYSCONF
// <unistd.h> already included above
#elif HAVE_SYSCTL
#include <sys/sysctl.h>
#else
#error Either sysctl or sysconf is required for the CPU count.
#endif

#include "pal/dbgmsg.h"
#include "pal/process.h"

#include <algorithm>

SET_DEFAULT_DEBUG_CHANNEL(MISC);

DWORD
PALAPI
PAL_GetTotalCpuCount()
{
    int nrcpus = 0;

#if HAVE_SYSCONF

#if defined(HOST_ARM) || defined(HOST_ARM64) || defined(HOST_LOONGARCH64) || defined(HOST_RISCV64)
#define SYSCONF_GET_NUMPROCS       _SC_NPROCESSORS_CONF
#define SYSCONF_GET_NUMPROCS_NAME "_SC_NPROCESSORS_CONF"
#else
#define SYSCONF_GET_NUMPROCS       _SC_NPROCESSORS_ONLN
#define SYSCONF_GET_NUMPROCS_NAME "_SC_NPROCESSORS_ONLN"
#endif
    nrcpus = sysconf(SYSCONF_GET_NUMPROCS);
    if (nrcpus < 1)
    {
        ASSERT("sysconf failed for %s (%d)\n", SYSCONF_GET_NUMPROCS_NAME, errno);
    }
#elif HAVE_SYSCTL
    int rc;
    size_t sz;
    int mib[] = { CTL_HW, HW_NCPU };
    sz = sizeof(nrcpus);
    rc = sysctl(mib, 2, &nrcpus, &sz, NULL, 0);
    if (rc != 0)
    {
        ASSERT("sysctl failed for HW_NCPU (%d)\n", errno);
    }
#else // HAVE_SYSCONF
#error "Don't know how to get total CPU count on this platform"
#endif // HAVE_SYSCONF

    if (nrcpus < 1)
    {
        // Default to 1 if we failed to get the number of CPUs or if the value is invalid.
        nrcpus = 1;
    }

    return nrcpus;
}

DWORD
PALAPI
PAL_GetLogicalCpuCountFromOS()
{
    static int nrcpus = -1;

    // Android tries really hard to save power by powering off CPUs on SMP phones which
    // means the normal way to query cpu count can underestimate the number of available CPUs.
#if defined(HOST_ANDROID)
    if (nrcpus <= 0)
    {
        nrcpus = minipal_get_cpu_present_count();
    }
#endif

    if (nrcpus <= 0)
    {
#if HAVE_SCHED_GETAFFINITY

        int configuredCpuCount = minipal_get_cpu_max_possible_count();
        if (configuredCpuCount == -1)
        {
            // In the unlikely event that minipal_get_cpu_max_possible_count() fails, just assume a reasonable default maximum number of CPUs to avoid failing.
            configuredCpuCount = CPU_SETSIZE;
        }

        int cpusToAllocate = std::max(configuredCpuCount, CPU_SETSIZE);

        cpu_set_t* pCpuSet = CPU_ALLOC(cpusToAllocate);
        if (pCpuSet != nullptr)
        {
            size_t cpuSetSize = CPU_ALLOC_SIZE(cpusToAllocate);
            CPU_ZERO_S(cpuSetSize, pCpuSet);

            int st = sched_getaffinity(gPID, cpuSetSize, pCpuSet);
            if (st == 0)
            {
                nrcpus = CPU_COUNT_S(cpuSetSize, pCpuSet);
            }
            else
            {
                ASSERT("sched_getaffinity failed (%d)\n", errno);
            }

            CPU_FREE(pCpuSet);
        }
        else
        {
            ASSERT("CPU_ALLOC failed!\n");
        }

        if (nrcpus < 1)
        {
            // If we failed to get the number of CPUs from sched_getaffinity, fall back to getting the total number of CPUs in the system.
            nrcpus = PAL_GetTotalCpuCount();
        }
#else // HAVE_SCHED_GETAFFINITY
        nrcpus = PAL_GetTotalCpuCount();
#endif // HAVE_SCHED_GETAFFINITY
    }

    return nrcpus;
}
