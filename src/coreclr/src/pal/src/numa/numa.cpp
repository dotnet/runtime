// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    numa.cpp

Abstract:

    Implementation of NUMA related APIs

--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(NUMA);

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/numa.h"
#include "pal/corunix.hpp"
#include "pal/thread.hpp"

#if HAVE_PTHREAD_NP_H
#include <pthread_np.h>
#endif

#include <pthread.h>
#include <dlfcn.h>
#ifdef __FreeBSD__
#include <stdlib.h>
#else
#include <alloca.h>
#endif

#include <algorithm>

#include "numashim.h"

using namespace CorUnix;

#if HAVE_CPUSET_T
typedef cpuset_t cpu_set_t;
#endif

// CPU affinity descriptor
struct CpuAffinity
{
    // NUMA node
    BYTE Node;
    // CPU number relative to the group the CPU is in
    BYTE Number;
    // CPU group
    WORD Group;
};

// Array mapping global CPU index to its affinity
CpuAffinity *g_cpuToAffinity = NULL;

// Array mapping CPU group and index in the group to the global CPU index
short *g_groupAndIndexToCpu = NULL;
// Array mapping CPU group to the corresponding affinity mask of the CPUs in the group
KAFFINITY *g_groupToCpuMask = NULL;
// Array mapping CPU group to the number of processors in the group
BYTE *g_groupToCpuCount = NULL;

// Total number of processors in the system
int g_cpuCount = 0;
// Total number of possible processors in the system
int g_possibleCpuCount = 0;
// Total number of CPU groups
int g_groupCount = 0;
// The highest NUMA node available
int g_highestNumaNode = 0;
// Is numa available
bool g_numaAvailable = false;

void* numaHandle = nullptr;

#if HAVE_NUMA_H
#define PER_FUNCTION_BLOCK(fn) decltype(fn)* fn##_ptr;
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK
#endif // HAVE_NUMA_H

static const int MaxCpusPerGroup = 8 * sizeof(KAFFINITY);
static const WORD NO_GROUP = 0xffff;

/*++
Function:
  FreeLookupArrays

Free CPU and group lookup arrays
--*/
VOID
FreeLookupArrays()
{
    free(g_groupAndIndexToCpu);
    free(g_cpuToAffinity);
    free(g_groupToCpuMask);
    free(g_groupToCpuCount);

    g_groupAndIndexToCpu = NULL;
    g_cpuToAffinity = NULL;
    g_groupToCpuMask = NULL;
    g_groupToCpuCount = NULL;
}

/*++
Function:
  AllocateLookupArrays

Allocate CPU and group lookup arrays
Return TRUE if the allocation succeeded
--*/
BOOL
AllocateLookupArrays()
{
    g_groupAndIndexToCpu = (short*)malloc(g_groupCount * MaxCpusPerGroup * sizeof(short));
    if (g_groupAndIndexToCpu == NULL)
    {
        goto FAILED;
    }

    g_cpuToAffinity = (CpuAffinity*)malloc(g_possibleCpuCount * sizeof(CpuAffinity));
    if (g_cpuToAffinity == NULL)
    {
        goto FAILED;
    }

    g_groupToCpuMask = (KAFFINITY*)malloc(g_groupCount * sizeof(KAFFINITY));
    if (g_groupToCpuMask == NULL)
    {
        goto FAILED;
    }

    g_groupToCpuCount = (BYTE*)malloc(g_groupCount * sizeof(BYTE));
    if (g_groupToCpuCount == NULL)
    {
        goto FAILED;
    }

    memset(g_groupAndIndexToCpu, 0xff, g_groupCount * MaxCpusPerGroup * sizeof(short));
    memset(g_cpuToAffinity, 0xff, g_possibleCpuCount * sizeof(CpuAffinity));
    memset(g_groupToCpuMask, 0, g_groupCount * sizeof(KAFFINITY));
    memset(g_groupToCpuCount, 0, g_groupCount * sizeof(BYTE));

    return TRUE;

FAILED:
    FreeLookupArrays();

    return FALSE;
}

/*++
Function:
  GetFullAffinityMask

Get affinity mask for the specified number of processors with all
the processors enabled.
--*/
KAFFINITY GetFullAffinityMask(int cpuCount)
{
    return ((KAFFINITY)1 << (cpuCount)) - 1;
}

/*++
Function:
  NUMASupportInitialize

Initialize data structures for getting and setting thread affinities to processors and
querying NUMA related processor information.
On systems with no NUMA support, it behaves as if there was a single NUMA node with
a single group of processors.
--*/
BOOL
NUMASupportInitialize()
{
#if HAVE_NUMA_H
    numaHandle = dlopen("libnuma.so", RTLD_LAZY);
    if (numaHandle == 0)
    {
        numaHandle = dlopen("libnuma.so.1", RTLD_LAZY);
    }
    if (numaHandle != 0)
    {
        dlsym(numaHandle, "numa_allocate_cpumask");
#define PER_FUNCTION_BLOCK(fn) \
    fn##_ptr = (decltype(fn)*)dlsym(numaHandle, #fn); \
    if (fn##_ptr == NULL) { fprintf(stderr, "Cannot get symbol " #fn " from libnuma\n"); abort(); }
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

        if (numa_available() == -1)
        {
            dlclose(numaHandle);
        }
        else
        {
            g_numaAvailable = true;

            struct bitmask *mask = numa_allocate_cpumask();
            int numaNodesCount = numa_max_node() + 1;

            g_possibleCpuCount = numa_num_possible_cpus();
            g_cpuCount = 0;
            g_groupCount = 0;

            for (int i = 0; i < numaNodesCount; i++)
            {
                int st = numa_node_to_cpus(i, mask);
                // The only failure that can happen is that the mask is not large enough
                // but that cannot happen since the mask was allocated by numa_allocate_cpumask
                _ASSERTE(st == 0);
                unsigned int nodeCpuCount = numa_bitmask_weight(mask);
                g_cpuCount += nodeCpuCount;
                unsigned int nodeGroupCount = (nodeCpuCount + MaxCpusPerGroup - 1) / MaxCpusPerGroup;
                g_groupCount += nodeGroupCount;
            }

            if (!AllocateLookupArrays())
            {
                dlclose(numaHandle);
                return FALSE;
            }

            WORD currentGroup = 0;
            int currentGroupCpus = 0;

            for (int i = 0; i < numaNodesCount; i++)
            {
                int st = numa_node_to_cpus(i, mask);
                // The only failure that can happen is that the mask is not large enough
                // but that cannot happen since the mask was allocated by numa_allocate_cpumask
                _ASSERTE(st == 0);
                unsigned int nodeCpuCount = numa_bitmask_weight(mask);
                unsigned int nodeGroupCount = (nodeCpuCount + MaxCpusPerGroup - 1) / MaxCpusPerGroup;
                for (int j = 0; j < g_possibleCpuCount; j++)
                {
                    if (numa_bitmask_isbitset(mask, j))
                    {
                        if (currentGroupCpus == MaxCpusPerGroup)
                        {
                            g_groupToCpuCount[currentGroup] = MaxCpusPerGroup;
                            g_groupToCpuMask[currentGroup] = GetFullAffinityMask(MaxCpusPerGroup);
                            currentGroupCpus = 0;
                            currentGroup++;
                        }
                        g_cpuToAffinity[j].Node = i;
                        g_cpuToAffinity[j].Group = currentGroup;
                        g_cpuToAffinity[j].Number = currentGroupCpus;
                        g_groupAndIndexToCpu[currentGroup * MaxCpusPerGroup + currentGroupCpus] = j;
                        currentGroupCpus++;
                    }
                }

                if (currentGroupCpus != 0)
                {
                    g_groupToCpuCount[currentGroup] = currentGroupCpus;
                    g_groupToCpuMask[currentGroup] = GetFullAffinityMask(currentGroupCpus);
                    currentGroupCpus = 0;
                    currentGroup++;
                }
            }

            numa_free_cpumask(mask);

            g_highestNumaNode = numa_max_node();
        }
    }
#endif // HAVE_NUMA_H
    if (!g_numaAvailable)
    {
        // No NUMA
        g_possibleCpuCount = PAL_GetLogicalCpuCountFromOS();
        g_cpuCount = PAL_GetLogicalCpuCountFromOS();
        g_groupCount = 1;
        g_highestNumaNode = 0;

        if (!AllocateLookupArrays())
        {
            return FALSE;
        }

        for (int i = 0; i < g_possibleCpuCount; i++)
        {
            g_cpuToAffinity[i].Number = i;
            g_cpuToAffinity[i].Group = 0;
        }
    }

    return TRUE;
}

/*++
Function:
  NUMASupportCleanup

Cleanup of the NUMA support data structures
--*/
VOID
NUMASupportCleanup()
{
    FreeLookupArrays();
#if HAVE_NUMA_H
    if (g_numaAvailable)
    {
        dlclose(numaHandle);
    }
#endif // HAVE_NUMA_H
}

/*++
Function:
  GetNumaHighestNodeNumber

See MSDN doc.
--*/
BOOL
PALAPI
GetNumaHighestNodeNumber(
  OUT PULONG HighestNodeNumber
)
{
    PERF_ENTRY(GetNumaHighestNodeNumber);
    ENTRY("GetNumaHighestNodeNumber(HighestNodeNumber=%p)\n", HighestNodeNumber);
    *HighestNodeNumber = (ULONG)g_highestNumaNode;

    BOOL success = TRUE;

    LOGEXIT("GetNumaHighestNodeNumber returns BOOL %d\n", success);
    PERF_EXIT(GetNumaHighestNodeNumber);

    return success;
}

/*++
Function:
  GetNumaProcessorNodeEx

See MSDN doc.
--*/
BOOL
PALAPI
GetNumaProcessorNodeEx(
  IN  PPROCESSOR_NUMBER Processor,
  OUT PUSHORT NodeNumber
)
{
    PERF_ENTRY(GetNumaProcessorNodeEx);
    ENTRY("GetNumaProcessorNodeEx(Processor=%p, NodeNumber=%p)\n", Processor, NodeNumber);

    BOOL success = FALSE;

    if ((Processor->Group < g_groupCount) && 
        (Processor->Number < MaxCpusPerGroup) && 
        (Processor->Reserved == 0))
    {  
        short cpu = g_groupAndIndexToCpu[Processor->Group * MaxCpusPerGroup + Processor->Number];
        if (cpu != -1)
        {
            *NodeNumber = g_cpuToAffinity[cpu].Node;
            success = TRUE;
        }
    }

    if (!success)
    {
        *NodeNumber = 0xffff;
        SetLastError(ERROR_INVALID_PARAMETER);  
    }

    LOGEXIT("GetNumaProcessorNodeEx returns BOOL %d\n", success);
    PERF_EXIT(GetNumaProcessorNodeEx);

    return success;
}

/*++
Function:
  GetLogicalProcessorInformationEx

See MSDN doc.
--*/
BOOL
PALAPI
GetLogicalProcessorInformationEx(
  IN LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
  OUT OPTIONAL PSYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX Buffer,
  IN OUT PDWORD ReturnedLength
)
{
    PERF_ENTRY(GetLogicalProcessorInformationEx);
    ENTRY("GetLogicalProcessorInformationEx(RelationshipType=%d, Buffer=%p, ReturnedLength=%p)\n", RelationshipType, Buffer, ReturnedLength);

    BOOL success = FALSE;

    if (RelationshipType == RelationGroup)
    {
        size_t requiredSize = __builtin_offsetof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX, Group);
        requiredSize += __builtin_offsetof(GROUP_RELATIONSHIP, GroupInfo);
        requiredSize += g_groupCount * sizeof(PROCESSOR_GROUP_INFO);

        if (*ReturnedLength >= requiredSize)
        {
            Buffer->Relationship = RelationGroup;
            Buffer->Size = requiredSize;
            Buffer->Group.MaximumGroupCount = g_groupCount;
            Buffer->Group.ActiveGroupCount = g_groupCount;
            for (int i = 0; i < g_groupCount; i++)
            {
                Buffer->Group.GroupInfo[i].MaximumProcessorCount = MaxCpusPerGroup;
                Buffer->Group.GroupInfo[i].ActiveProcessorCount = g_groupToCpuCount[i];
                Buffer->Group.GroupInfo[i].ActiveProcessorMask = g_groupToCpuMask[i];
            }

            success = TRUE;
        }
        else
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
        }

        *ReturnedLength = requiredSize;
    }
    else
    {
        // We only support the group relationship
        SetLastError(ERROR_INVALID_PARAMETER);  
    }

    LOGEXIT("GetLogicalProcessorInformationEx returns BOOL %d\n", success);
    PERF_EXIT(GetLogicalProcessorInformationEx);

    return success;
}

/*++
Function:
  GetThreadGroupAffinityInternal

Get the group affinity for the specified pthread
--*/
BOOL
GetThreadGroupAffinityInternal(
  IN pthread_t thread,
  OUT PGROUP_AFFINITY GroupAffinity
)
{
    BOOL success = FALSE;

#if HAVE_PTHREAD_GETAFFINITY_NP
    cpu_set_t cpuSet;

    int st = pthread_getaffinity_np(thread, sizeof(cpu_set_t), &cpuSet);

    if (st == 0)
    {
        WORD group = NO_GROUP;
        KAFFINITY mask = 0;

        for (int i = 0; i < g_possibleCpuCount; i++)
        {
            if (CPU_ISSET(i, &cpuSet))
            {
                WORD g = g_cpuToAffinity[i].Group;
                // Unless the thread affinity was already set by SetThreadGroupAffinity, it is possible that
                // the current thread has affinity with processors from multiple groups. So we report just the
                // first group we find.
                if (group == NO_GROUP || g == group)
                {
                    group = g;
                    mask |= ((KAFFINITY)1) << g_cpuToAffinity[i].Number;
                }
            }
        }

        GroupAffinity->Group = group;
        GroupAffinity->Mask = mask;
        success = TRUE;
    }
    else
    {
        SetLastError(ERROR_GEN_FAILURE);
    }
#else // HAVE_PTHREAD_GETAFFINITY_NP
    // There is no API to manage thread affinity, so let's return a group affinity
    // with all the CPUs on the system.
    GroupAffinity->Group = 0;
    GroupAffinity->Mask = GetFullAffinityMask(g_possibleCpuCount);
    success = TRUE;
#endif // HAVE_PTHREAD_GETAFFINITY_NP

    return success;
}

/*++
Function:
  GetThreadGroupAffinity

See MSDN doc.
--*/
BOOL
PALAPI
GetThreadGroupAffinity(
  IN HANDLE hThread,
  OUT PGROUP_AFFINITY GroupAffinity
)
{
    PERF_ENTRY(GetThreadGroupAffinity);
    ENTRY("GetThreadGroupAffinity(hThread=%p, GroupAffinity=%p)\n", hThread, GroupAffinity);
    CPalThread *pCurrentThread = InternalGetCurrentThread();
    CPalThread *pTargetThread = NULL;
    IPalObject *pTargetThreadObject = NULL;

    PAL_ERROR palErr =
        InternalGetThreadDataFromHandle(pCurrentThread, hThread,
                                        0, // THREAD_SET_CONTEXT
                                        &pTargetThread, &pTargetThreadObject);

    if (NO_ERROR != palErr)
    {
        ERROR("Unable to obtain thread data for handle %p (error %x)!\n", hThread,
              palErr);
        return FALSE;
    }

    BOOL success = GetThreadGroupAffinityInternal(
        pTargetThread->GetPThreadSelf(), GroupAffinity);
    LOGEXIT("GetThreadGroupAffinity returns BOOL %d\n", success);
    PERF_EXIT(GetThreadGroupAffinity);

    return success;
}


/*++
Function:
  SetThreadGroupAffinity

See MSDN doc.
--*/
BOOL
PALAPI
SetThreadGroupAffinity(
  IN HANDLE hThread,
  IN const GROUP_AFFINITY *GroupAffinity,
  OUT OPTIONAL PGROUP_AFFINITY PreviousGroupAffinity
)
{
    PERF_ENTRY(SetThreadGroupAffinity);
    ENTRY("SetThreadGroupAffinity(hThread=%p, GroupAffinity=%p, PreviousGroupAffinity=%p)\n", hThread, GroupAffinity, PreviousGroupAffinity);

    CPalThread *pCurrentThread = InternalGetCurrentThread();
    CPalThread *pTargetThread = NULL;
    IPalObject *pTargetThreadObject = NULL;

    PAL_ERROR palErr =
        InternalGetThreadDataFromHandle(pCurrentThread, hThread,
                                        0, // THREAD_SET_CONTEXT
                                        &pTargetThread, &pTargetThreadObject);

    if (NO_ERROR != palErr)
    {
        ERROR("Unable to obtain thread data for handle %p (error %x)!\n", hThread,
              palErr);
        return FALSE;
    }

    pthread_t thread = pTargetThread->GetPThreadSelf();

    if (PreviousGroupAffinity != NULL)
    {
        GetThreadGroupAffinityInternal(thread, PreviousGroupAffinity);
    }

#if HAVE_PTHREAD_GETAFFINITY_NP
    int groupStartIndex = GroupAffinity->Group * MaxCpusPerGroup;
    KAFFINITY mask = 1;
    cpu_set_t cpuSet;
    CPU_ZERO(&cpuSet);

    for (int i = 0; i < MaxCpusPerGroup; i++, mask <<= 1)
    {
        if (GroupAffinity->Mask & mask)
        {
            int cpu = g_groupAndIndexToCpu[groupStartIndex + i];
            if (cpu != -1)
            {
                CPU_SET(cpu, &cpuSet);
            }
        }
    }

    int st = pthread_setaffinity_np(thread, sizeof(cpu_set_t), &cpuSet);

    if (st != 0)
    {
        switch (st)
        {
        case EINVAL:
            // There is no processor in the mask that is allowed to execute the process
            SetLastError(ERROR_INVALID_PARAMETER);
            break;
        case ESRCH:
            SetLastError(ERROR_INVALID_HANDLE);
            break;
        default:
            SetLastError(ERROR_GEN_FAILURE);
            break;
        }
    }

    BOOL success = (st == 0);
#else // HAVE_PTHREAD_GETAFFINITY_NP
    // There is no API to manage thread affinity, so let's ignore the request
    BOOL success = TRUE;
#endif // HAVE_PTHREAD_GETAFFINITY_NP

    LOGEXIT("SetThreadGroupAffinity returns BOOL %d\n", success);
    PERF_EXIT(SetThreadGroupAffinity);

    return success;
}

/*++
Function:
  SetThreadAffinityMask

See MSDN doc.
--*/
DWORD_PTR
PALAPI
SetThreadAffinityMask(
  IN HANDLE hThread,
  IN DWORD_PTR dwThreadAffinityMask
)
{
    PERF_ENTRY(SetThreadAffinityMask);
    ENTRY("SetThreadAffinityMask(hThread=%p, dwThreadAffinityMask=%p)\n", hThread, dwThreadAffinityMask);

    CPalThread *pCurrentThread = InternalGetCurrentThread();
    CPalThread *pTargetThread = NULL;
    IPalObject *pTargetThreadObject = NULL;

    PAL_ERROR palErr =
        InternalGetThreadDataFromHandle(pCurrentThread, hThread,
                                        0, // THREAD_SET_CONTEXT
                                        &pTargetThread, &pTargetThreadObject);

    if (NO_ERROR != palErr)
    {
        ERROR("Unable to obtain thread data for handle %p (error %x)!\n", hThread,
              palErr);
        return 0;
    }

    pthread_t thread = pTargetThread->GetPThreadSelf();

#if HAVE_PTHREAD_GETAFFINITY_NP
    cpu_set_t prevCpuSet;
    CPU_ZERO(&prevCpuSet);
    KAFFINITY prevMask = 0;

    int st = pthread_getaffinity_np(thread, sizeof(cpu_set_t), &prevCpuSet);

    if (st == 0)
    {
        for (int i = 0; i < std::min(8 * (int)sizeof(KAFFINITY), g_possibleCpuCount); i++)
        {
            if (CPU_ISSET(i, &prevCpuSet))
            {
                prevMask |= ((KAFFINITY)1) << i;
            }
        }
    }

    cpu_set_t cpuSet;
    CPU_ZERO(&cpuSet);

    int cpu = 0;
    while (dwThreadAffinityMask)
    {
        if (dwThreadAffinityMask & 1)
        {
            CPU_SET(cpu, &cpuSet);
        }
        cpu++;
        dwThreadAffinityMask >>= 1;
    }

    st = pthread_setaffinity_np(thread, sizeof(cpu_set_t), &cpuSet);

    if (st != 0)
    {
        switch (st)
        {
        case EINVAL:
            // There is no processor in the mask that is allowed to execute the
            // process
            SetLastError(ERROR_INVALID_PARAMETER);
            break;
        case ESRCH:
            SetLastError(ERROR_INVALID_HANDLE);
            break;
        default:
            SetLastError(ERROR_GEN_FAILURE);
            break;
        }
    }

    DWORD_PTR ret = (st == 0) ? prevMask : 0;
#else  // HAVE_PTHREAD_GETAFFINITY_NP
    // There is no API to manage thread affinity, so let's ignore the request
    DWORD_PTR ret = 0;
#endif // HAVE_PTHREAD_GETAFFINITY_NP
    LOGEXIT("SetThreadAffinityMask returns  %lu\n", ret);
    PERF_EXIT(SetThreadAffinityMask);

    return ret;
}

/*++
Function:
  GetCurrentProcessorNumberEx

See MSDN doc.
--*/
VOID
PALAPI
GetCurrentProcessorNumberEx(
  OUT PPROCESSOR_NUMBER ProcNumber
)
{
    PERF_ENTRY(GetCurrentProcessorNumberEx);
    ENTRY("GetCurrentProcessorNumberEx(ProcNumber=%p\n", ProcNumber);

    DWORD cpu = GetCurrentProcessorNumber();
    _ASSERTE(cpu < g_possibleCpuCount);
    ProcNumber->Group = g_cpuToAffinity[cpu].Group;
    ProcNumber->Number = g_cpuToAffinity[cpu].Number;

    LOGEXIT("GetCurrentProcessorNumberEx\n");
    PERF_EXIT(GetCurrentProcessorNumberEx);
}

/*++
Function:
  GetProcessAffinityMask

See MSDN doc.
--*/
BOOL
PALAPI
GetProcessAffinityMask(
  IN HANDLE hProcess,
  OUT PDWORD_PTR lpProcessAffinityMask,
  OUT PDWORD_PTR lpSystemAffinityMask
)
{
    PERF_ENTRY(GetProcessAffinityMask);
    ENTRY("GetProcessAffinityMask(hProcess=%p, lpProcessAffinityMask=%p, lpSystemAffinityMask=%p\n", hProcess, lpProcessAffinityMask, lpSystemAffinityMask);

    BOOL success = FALSE;

    if (hProcess == GetCurrentProcess())
    {
        DWORD_PTR systemMask = GetFullAffinityMask(g_cpuCount);

#if HAVE_SCHED_GETAFFINITY
        int pid = getpid();
        cpu_set_t cpuSet;
        int st = sched_getaffinity(pid, sizeof(cpu_set_t), &cpuSet);
        if (st == 0)
        {
            WORD group = NO_GROUP;
            DWORD_PTR processMask = 0;

            for (int i = 0; i < g_possibleCpuCount; i++)
            {
                if (CPU_ISSET(i, &cpuSet))
                {
                    WORD g = g_cpuToAffinity[i].Group;
                    if (group == NO_GROUP || g == group)
                    {
                        group = g;
                        processMask |= ((DWORD_PTR)1) << g_cpuToAffinity[i].Number;
                    }
                    else
                    {
                        // The process has affinity in more than one group, in such case
                        // the function needs to return zero in both masks.
                        processMask = 0;
                        systemMask = 0;
                        group = NO_GROUP;
                        break;
                    }
                }
            }

            success = TRUE;

            *lpProcessAffinityMask = processMask;
            *lpSystemAffinityMask = systemMask;
        }
        else if (errno == EINVAL)
        {
            // There are more processors than can fit in a cpu_set_t
            // return zero in both masks.
            *lpProcessAffinityMask = 0;
            *lpSystemAffinityMask = 0;
            success = TRUE;
        }
        else
        {
            // We should not get any of the errors that the sched_getaffinity can return since none
            // of them applies for the current thread, so this is an unexpected kind of failure.
            SetLastError(ERROR_GEN_FAILURE);
        }
#else // HAVE_SCHED_GETAFFINITY
        // There is no API to manage thread affinity, so let's return both affinity masks
        // with all the CPUs on the system set.
        *lpSystemAffinityMask = systemMask;
        *lpProcessAffinityMask = systemMask;

        success = TRUE;
#endif // HAVE_SCHED_GETAFFINITY
    }
    else
    {
        // PAL supports getting affinity mask for the current process only
        SetLastError(ERROR_INVALID_PARAMETER);
    }

    LOGEXIT("GetProcessAffinityMask returns BOOL %d\n", success);
    PERF_EXIT(GetProcessAffinityMask);

    return success;
}

/*++
Function:
  VirtualAllocExNuma

See MSDN doc.
--*/
LPVOID
PALAPI
VirtualAllocExNuma(
  IN HANDLE hProcess,
  IN OPTIONAL LPVOID lpAddress,
  IN SIZE_T dwSize,
  IN DWORD flAllocationType,
  IN DWORD flProtect,
  IN DWORD nndPreferred
)
{
    PERF_ENTRY(VirtualAllocExNuma);
    ENTRY("VirtualAllocExNuma(hProcess=%p, lpAddress=%p, dwSize=%u, flAllocationType=%#x, flProtect=%#x, nndPreferred=%d\n", 
        hProcess, lpAddress, dwSize, flAllocationType, flProtect, nndPreferred);

    LPVOID result = NULL;

    if (hProcess == GetCurrentProcess())
    {
        if (nndPreferred <= g_highestNumaNode)
        {
            result = VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
#if HAVE_NUMA_H
            if (result != NULL && g_numaAvailable)
            {
                int nodeMaskLength = (g_highestNumaNode + 1 + sizeof(unsigned long) - 1) / sizeof(unsigned long);
                unsigned long *nodeMask = (unsigned long*)alloca(nodeMaskLength * sizeof(unsigned long));
                memset(nodeMask, 0, nodeMaskLength);

                int index = nndPreferred / sizeof(unsigned long);
                int mask = ((unsigned long)1) << (nndPreferred & (sizeof(unsigned long) - 1));
                nodeMask[index] = mask;

                int st = mbind(result, dwSize, MPOL_PREFERRED, nodeMask, g_highestNumaNode, 0);

                _ASSERTE(st == 0);
                // If the mbind fails, we still return the allocated memory since the nndPreferred is just a hint
            }
#endif // HAVE_NUMA_H
        }
        else
        {
            // The specified node number is larger than the maximum available one
            SetLastError(ERROR_INVALID_PARAMETER);
        }
    }    
    else
    {
        // PAL supports allocating from the current process virtual space only
        SetLastError(ERROR_INVALID_PARAMETER);
    }

    LOGEXIT("VirtualAllocExNuma returns %p\n", result);
    PERF_EXIT(VirtualAllocExNuma);

    return result;
}

/*++
Function:
  SetThreadIdealProcessorEx

See MSDN doc.
--*/
BOOL
PALAPI
SetThreadIdealProcessorEx(
  IN HANDLE hThread,
  IN PPROCESSOR_NUMBER lpIdealProcessor,
  OUT PPROCESSOR_NUMBER lpPreviousIdealProcessor)
{
    PERF_ENTRY(SetThreadIdealProcessorEx);
    ENTRY("SetThreadIdealProcessorEx(hThread=%p, lpIdealProcessor=%p)\n", hThread, lpIdealProcessor);

    CPalThread *pCurrentThread = InternalGetCurrentThread();
    CPalThread *pTargetThread = NULL;
    IPalObject *pTargetThreadObject = NULL;

    PAL_ERROR palErr =
        InternalGetThreadDataFromHandle(pCurrentThread, hThread,
                                        0, // THREAD_SET_CONTEXT
                                        &pTargetThread, &pTargetThreadObject);

    if (NO_ERROR != palErr)
    {
        ERROR("Unable to obtain thread data for handle %p (error %x)!\n", hThread,
              palErr);
        return 0;
    }

    pthread_t thread = pTargetThread->GetPThreadSelf();

#if HAVE_PTHREAD_GETAFFINITY_NP
    int cpu = -1;
    if ((lpIdealProcessor->Group < g_groupCount) &&
        (lpIdealProcessor->Number < MaxCpusPerGroup) &&
        (lpIdealProcessor->Reserved == 0))
    {
        cpu = g_groupAndIndexToCpu[lpIdealProcessor->Group * MaxCpusPerGroup + lpIdealProcessor->Number];
    }

    if (cpu == -1)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    if (lpPreviousIdealProcessor != NULL)
    {
        cpu_set_t prevCpuSet;
        CPU_ZERO(&prevCpuSet);
        DWORD prevCpu = GetCurrentProcessorNumber();

        int st = pthread_getaffinity_np(thread, sizeof(cpu_set_t), &prevCpuSet);

        if (st == 0)
        {
            for (int i = 0; i < g_possibleCpuCount; i++)
            {
                if (CPU_ISSET(i, &prevCpuSet))
                {
                    prevCpu = i;
                    break;
                }
            }
        }

        _ASSERTE(prevCpu < g_possibleCpuCount);
        lpPreviousIdealProcessor->Group = g_cpuToAffinity[prevCpu].Group;
        lpPreviousIdealProcessor->Number = g_cpuToAffinity[prevCpu].Number;
        lpPreviousIdealProcessor->Reserved = 0;
    }

    cpu_set_t cpuSet;
    CPU_ZERO(&cpuSet);
    CPU_SET(cpu, &cpuSet);

    int st = pthread_setaffinity_np(thread, sizeof(cpu_set_t), &cpuSet);

    if (st != 0)
    {
        switch (st)
        {
        case EINVAL:
            // There is no processor in the mask that is allowed to execute the
            // process
            SetLastError(ERROR_INVALID_PARAMETER);
            break;
        case ESRCH:
            SetLastError(ERROR_INVALID_HANDLE);
            break;
        default:
            SetLastError(ERROR_GEN_FAILURE);
            break;
        }
    }

    BOOL success = (st == 0);

#else  // HAVE_PTHREAD_GETAFFINITY_NP
    // There is no API to manage thread affinity, so let's ignore the request
    BOOL success = FALSE;
#endif // HAVE_PTHREAD_GETAFFINITY_NP

    LOGEXIT("SetThreadIdealProcessorEx returns BOOL %d\n", success);
    PERF_EXIT(SetThreadIdealProcessorEx);

    return success;
}
