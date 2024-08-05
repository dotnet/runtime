// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    sysinfo.c

Abstract:

    Implements GetSystemInfo.

Revision History:



--*/

#include "pal/palinternal.h"

#include <sched.h>
#include <errno.h>
#include <unistd.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <sys/types.h>

#if HAVE_SYSCONF
// <unistd.h> already included above
#elif HAVE_SYSCTL
#include <sys/sysctl.h>
#else
#error Either sysctl or sysconf is required for GetSystemInfo.
#endif

#if HAVE_SYSCTLBYNAME
#include <sys/sysctl.h>
#endif

#include <sys/param.h>

#if HAVE_SYS_VMPARAM_H
#include <sys/vmparam.h>
#endif  // HAVE_SYS_VMPARAM_H

#if HAVE_MACH_VM_TYPES_H
#include <mach/vm_types.h>
#endif // HAVE_MACH_VM_TYPES_H

#if HAVE_MACH_VM_PARAM_H
#include <mach/vm_param.h>
#endif  // HAVE_MACH_VM_PARAM_H

#if HAVE_MACHINE_VMPARAM_H
#include <machine/vmparam.h>
#endif  // HAVE_MACHINE_VMPARAM_H

#if defined(TARGET_OSX)
#include <mach/vm_statistics.h>
#include <mach/mach_types.h>
#include <mach/mach_init.h>
#include <mach/mach_host.h>
#endif // defined(TARGET_OSX)

// On some platforms sys/user.h ends up defining _DEBUG; if so
// remove the definition before including the header and put
// back our definition afterwards
#if USER_H_DEFINES_DEBUG
#define OLD_DEBUG _DEBUG
#undef _DEBUG
#endif
#include <sys/user.h>
#if USER_H_DEFINES_DEBUG
#undef _DEBUG
#define _DEBUG OLD_DEBUG
#undef OLD_DEBUG
#endif

#include "pal/dbgmsg.h"
#include "pal/process.h"

#include <algorithm>

SET_DEFAULT_DEBUG_CHANNEL(MISC);

#ifndef __APPLE__
#if HAVE_SYSCONF && HAVE__SC_AVPHYS_PAGES
#define SYSCONF_PAGES _SC_AVPHYS_PAGES
#elif HAVE_SYSCONF && HAVE__SC_PHYS_PAGES
#define SYSCONF_PAGES _SC_PHYS_PAGES
#else
#error Dont know how to get page-size on this architecture!
#endif
#endif // __APPLE__

DWORD
PALAPI
PAL_GetTotalCpuCount()
{
    int nrcpus = 0;

#if HAVE_SYSCONF

#if defined(HOST_ARM) || defined(HOST_ARM64)
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

    return nrcpus;
}

DWORD
PALAPI
PAL_GetLogicalCpuCountFromOS()
{
    static int nrcpus = -1;

    if (nrcpus == -1)
    {
#if HAVE_SCHED_GETAFFINITY

        cpu_set_t cpuSet;
        int st = sched_getaffinity(gPID, sizeof(cpu_set_t), &cpuSet);
        if (st != 0)
        {
            ASSERT("sched_getaffinity failed (%d)\n", errno);
        }

        nrcpus = CPU_COUNT(&cpuSet);
#else // HAVE_SCHED_GETAFFINITY
        nrcpus = PAL_GetTotalCpuCount();
#endif // HAVE_SCHED_GETAFFINITY
    }

    return nrcpus;
}

/*++
Function:
  GetSystemInfo

GetSystemInfo

The GetSystemInfo function returns information about the current system.

Parameters

lpSystemInfo
       [out] Pointer to a SYSTEM_INFO structure that receives the information.

Return Values

This function does not return a value.

Note:
  fields returned by this function are:
    dwNumberOfProcessors
    dwPageSize
Others are set to zero.

--*/
VOID
PALAPI
GetSystemInfo(
          OUT LPSYSTEM_INFO lpSystemInfo)
{
    int nrcpus = 0;
    long pagesize;

    PERF_ENTRY(GetSystemInfo);
    ENTRY("GetSystemInfo (lpSystemInfo=%p)\n", lpSystemInfo);

    pagesize = getpagesize();

    lpSystemInfo->wProcessorArchitecture_PAL_Undefined = 0;
    lpSystemInfo->wReserved_PAL_Undefined = 0;
    lpSystemInfo->dwPageSize = pagesize;
    lpSystemInfo->dwActiveProcessorMask_PAL_Undefined = 0;

    nrcpus = PAL_GetLogicalCpuCountFromOS();
    TRACE("dwNumberOfProcessors=%d\n", nrcpus);
    lpSystemInfo->dwNumberOfProcessors = nrcpus;

#ifdef VM_MAXUSER_ADDRESS
    lpSystemInfo->lpMaximumApplicationAddress = (PVOID) VM_MAXUSER_ADDRESS;
#elif defined(__linux__)
    lpSystemInfo->lpMaximumApplicationAddress = (PVOID) (1ull << 47);
#elif defined(__sun)
    lpSystemInfo->lpMaximumApplicationAddress = (PVOID) 0xfffffd7fffe00000ul;
#elif defined(USERLIMIT)
    lpSystemInfo->lpMaximumApplicationAddress = (PVOID) USERLIMIT;
#elif defined(HOST_64BIT)
#if defined(USRSTACK64)
    lpSystemInfo->lpMaximumApplicationAddress = (PVOID) USRSTACK64;
#else // !USRSTACK64
#error How come USRSTACK64 is not defined for 64bit?
#endif // USRSTACK64
#elif defined(USRSTACK)
    lpSystemInfo->lpMaximumApplicationAddress = (PVOID) USRSTACK;
#else
#error The maximum application address is not known on this platform.
#endif

    lpSystemInfo->lpMinimumApplicationAddress = (PVOID) pagesize;

    lpSystemInfo->dwProcessorType_PAL_Undefined = 0;

    lpSystemInfo->dwAllocationGranularity = pagesize;

    lpSystemInfo->wProcessorLevel_PAL_Undefined = 0;
    lpSystemInfo->wProcessorRevision_PAL_Undefined = 0;

    LOGEXIT("GetSystemInfo returns VOID\n");
    PERF_EXIT(GetSystemInfo);
}
