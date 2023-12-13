// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __GCENV_STRUCTS_INCLUDED__
#define __GCENV_STRUCTS_INCLUDED__
//
// Structs shared between the GC and the environment
//

struct GCSystemInfo
{
    uint32_t        dwNumberOfProcessors;
    uint32_t        dwPageSize;
    uint32_t        dwAllocationGranularity;
};

typedef void * HANDLE;

// Across all platforms, NativeAOT Thread::GetPalThreadIdForLogging assumes that the thread
// ID is stored in the first 8 bytes of this structure.

class EEThreadId
{
    uint64_t m_uiId;
public:
    bool IsCurrentThread();

    void SetToCurrentThread();

    void Clear()
    {
        m_uiId = 0;
    }
};

#ifndef _INC_WINDOWS

#ifdef TARGET_UNIX

typedef struct _RTL_CRITICAL_SECTION {
    pthread_mutex_t mutex;
} CRITICAL_SECTION, RTL_CRITICAL_SECTION, *PRTL_CRITICAL_SECTION;

#else

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

#endif

#endif // _INC_WINDOWS

#endif // __GCENV_STRUCTS_INCLUDED__
