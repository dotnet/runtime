//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#ifndef __GCENV_STRUCTS_INCLUDED__
#define __GCENV_STRUCTS_INCLUDED__
//
// Structs shared between the GC and the environment
//

struct GCSystemInfo
{
    uint32_t dwNumberOfProcessors;
    uint32_t dwPageSize;
    uint32_t dwAllocationGranularity;
};

// An 'abstract' definition of Windows MEMORYSTATUSEX.  In practice, the only difference is the missing struct size
// field and one field that Windows documents to always be 0.  If additional information is available on other OSes,
// this information should be surfaced through this structure as additional fields that the GC may optionally depend on.
struct GCMemoryStatus
{
    uint32_t dwMemoryLoad;
    uint64_t ullTotalPhys;
    uint64_t ullAvailPhys;
    uint64_t ullTotalPageFile;
    uint64_t ullAvailPageFile;
    uint64_t ullTotalVirtual;
    uint64_t ullAvailVirtual;
};

typedef void * HANDLE;

#ifndef _INC_WINDOWS

typedef union _LARGE_INTEGER {
    struct {
#if BIGENDIAN
        int32_t HighPart;
        uint32_t LowPart;
#else
        uint32_t LowPart;
        int32_t HighPart;
#endif
    } u;
    int64_t QuadPart;
} LARGE_INTEGER, *PLARGE_INTEGER;

#ifdef WIN32

#pragma pack(push, 8)

typedef struct _RTL_CRITICAL_SECTION {
    void* DebugInfo;

    //
    //  The following three fields control entering and exiting the critical
    //  section for the resource
    //

    int32_t LockCount;
    int32_t RecursionCount;
    HANDLE OwningThread;        // from the thread's ClientId->UniqueThread
    HANDLE LockSemaphore;
    uintptr_t SpinCount;        // force size on 64-bit systems when packed
} CRITICAL_SECTION, RTL_CRITICAL_SECTION, *PRTL_CRITICAL_SECTION;

#pragma pack(pop)

#else

typedef struct _RTL_CRITICAL_SECTION {
    pthread_mutex_t mutex;
} CRITICAL_SECTION, RTL_CRITICAL_SECTION, *PRTL_CRITICAL_SECTION;

#endif

#endif // _INC_WINDOWS

#endif // __GCENV_STRUCTS_INCLUDED__
