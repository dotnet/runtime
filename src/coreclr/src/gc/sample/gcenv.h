//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#if defined(_DEBUG)
#ifndef _DEBUG_IMPL
#define _DEBUG_IMPL 1
#endif
#define ASSERT(_expr) assert(_expr)
#else
#define ASSERT(_expr)
#endif

#ifndef _ASSERTE
#define _ASSERTE(_expr) ASSERT(_expr)
#endif

typedef wchar_t WCHAR;
#define W(s) L##s

#include "gcenv.structs.h"
#include "gcenv.base.h"
#include "gcenv.ee.h"
#include "gcenv.os.h"
#include "gcenv.interlocked.h"
#include "gcenv.interlocked.inl"
#include "gcenv.object.h"
#include "gcenv.sync.h"

#define MAX_LONGPATH 1024

//
// Thread
//

struct alloc_context;

class Thread
{
    uint32_t m_fPreemptiveGCDisabled;
    uintptr_t m_alloc_context[16]; // Reserve enough space to fix allocation context

    friend class ThreadStore;
    Thread * m_pNext;

public:
    Thread()
    {
    }

    bool PreemptiveGCDisabled()
    {
        return !!m_fPreemptiveGCDisabled;
    }

    void EnablePreemptiveGC()
    {
        m_fPreemptiveGCDisabled = false;
    }

    void DisablePreemptiveGC()
    {
        m_fPreemptiveGCDisabled = true;
    }

    alloc_context* GetAllocContext()
    {
        return (alloc_context *)&m_alloc_context;
    }

    void SetGCSpecial(bool fGCSpecial)
    {
    }

    bool CatchAtSafePoint()
    {
        // This is only called by the GC on a background GC worker thread that's explicitly interested in letting
        // a foreground GC proceed at that point. So it's always safe to return true.
        return true;
    }
};

Thread * GetThread();

class ThreadStore
{
public:
    static Thread * GetThreadList(Thread * pThread);

    static void AttachCurrentThread();
};

// -----------------------------------------------------------------------------------------------------------
// Config file enumulation
//

class EEConfig
{
public:
    enum HeapVerifyFlags {
        HEAPVERIFY_NONE = 0,
        HEAPVERIFY_GC = 1,   // Verify the heap at beginning and end of GC
        HEAPVERIFY_BARRIERCHECK = 2,   // Verify the brick table
        HEAPVERIFY_SYNCBLK = 4,   // Verify sync block scanning

                                  // the following options can be used to mitigate some of the overhead introduced
                                  // by heap verification.  some options might cause heap verifiction to be less
                                  // effective depending on the scenario.

        HEAPVERIFY_NO_RANGE_CHECKS = 0x10,   // Excludes checking if an OBJECTREF is within the bounds of the managed heap
        HEAPVERIFY_NO_MEM_FILL = 0x20,   // Excludes filling unused segment portions with fill pattern
        HEAPVERIFY_POST_GC_ONLY = 0x40,   // Performs heap verification post-GCs only (instead of before and after each GC)
        HEAPVERIFY_DEEP_ON_COMPACT = 0x80    // Performs deep object verfication only on compacting GCs.
    };

    enum  GCStressFlags {
        GCSTRESS_NONE = 0,
        GCSTRESS_ALLOC = 1,    // GC on all allocs and 'easy' places
        GCSTRESS_TRANSITION = 2,    // GC on transitions to preemtive GC
        GCSTRESS_INSTR_JIT = 4,    // GC on every allowable JITed instr
        GCSTRESS_INSTR_NGEN = 8,    // GC on every allowable NGEN instr
        GCSTRESS_UNIQUE = 16,   // GC only on a unique stack trace
    };

    int     GetHeapVerifyLevel() { return 0; }
    bool    IsHeapVerifyEnabled() { return GetHeapVerifyLevel() != 0; }

    GCStressFlags GetGCStressLevel()        const { return GCSTRESS_NONE; }
    bool    IsGCStressMix()                 const { return false; }

    int     GetGCtraceStart()               const { return 0; }
    int     GetGCtraceEnd()               const { return 0; }//1000000000; }
    int     GetGCtraceFac()               const { return 0; }
    int     GetGCprnLvl()               const { return 0; }
    bool    IsGCBreakOnOOMEnabled()         const { return false; }
    int     GetGCgen0size()               const { return 0; }
    int     GetSegmentSize()               const { return 0; }
    int     GetGCconcurrent()               const { return 1; }
    int     GetGCLatencyMode()              const { return 1; }
    int     GetGCForceCompact()             const { return 0; }
    int     GetGCRetainVM()                const { return 0; }
    int     GetGCTrimCommit()               const { return 0; }
    int     GetGCLOHCompactionMode()        const { return 0; }

    bool    GetGCAllowVeryLargeObjects()   const { return false; }

    bool    GetGCConservative()             const { return true; }
};

extern EEConfig * g_pConfig;

#include "etmdummy.h"
#define ETW_EVENT_ENABLED(e,f) false
