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

#ifdef PLATFORM_UNIX

class EEThreadId
{
    pthread_t m_id;
    // Indicates whether the m_id is valid or not. pthread_t doesn't have any
    // portable "invalid" value.
    bool m_isValid;

public:
    bool IsCurrentThread()
    {
        return m_isValid && pthread_equal(m_id, pthread_self());
    }

    void SetToCurrentThread()
    {
        m_id = pthread_self();
        m_isValid = true;
    }

    void Clear()
    {
        m_isValid = false;
    }
};

#else // PLATFORM_UNIX

#ifndef _INC_WINDOWS
extern "C" uint32_t __stdcall GetCurrentThreadId();
#endif

class EEThreadId
{
    uint32_t m_uiId;
public:

    bool IsCurrentThread()
    {
        return m_uiId == ::GetCurrentThreadId();
    }

    void SetToCurrentThread()
    {
        m_uiId = ::GetCurrentThreadId();        
    }

    void Clear()
    {
        m_uiId = 0;
    }
};

#endif // PLATFORM_UNIX

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

#ifdef PLATFORM_UNIX

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
