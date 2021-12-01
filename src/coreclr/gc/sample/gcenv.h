// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __GCENV_H__
#define __GCENV_H__

// The sample is to be kept simple, so building the sample
// in tandem with a standalone GC is currently not supported.
#ifdef BUILD_AS_STANDALONE
#undef BUILD_AS_STANDALONE
#endif // BUILD_AS_STANDALONE

#define FEATURE_REDHAWK

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

#include "gcenv.structs.h"
#include "gcenv.base.h"
#include "gcenv.os.h"
#include "gcenv.interlocked.h"
#include "gcenv.interlocked.inl"
#include "gcenv.object.h"
#include "gcenv.sync.h"
#include "gcenv.ee.h"
#include "volatile.h"

#ifdef TARGET_UNIX
#include "gcenv.unix.inl"
#else
#include "gcenv.windows.inl"
#endif

#define MAX_LONGPATH 1024

#ifdef _MSC_VER
#define SUPPRESS_WARNING_4127   \
    __pragma(warning(push))     \
    __pragma(warning(disable:4127)) /* conditional expression is constant*/
#define POP_WARNING_STATE       \
    __pragma(warning(pop))
#else // _MSC_VER
#define SUPPRESS_WARNING_4127
#define POP_WARNING_STATE
#endif // _MSC_VER

#define WHILE_0             \
    SUPPRESS_WARNING_4127   \
    while(0)                \
    POP_WARNING_STATE       \

#define LL_INFO10 4

#define STRESS_LOG_VA(level,msg)                                        do { } WHILE_0
#define STRESS_LOG0(facility, level, msg)                               do { } WHILE_0
#define STRESS_LOG1(facility, level, msg, data1)                        do { } WHILE_0
#define STRESS_LOG2(facility, level, msg, data1, data2)                 do { } WHILE_0
#define STRESS_LOG3(facility, level, msg, data1, data2, data3)          do { } WHILE_0
#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4)   do { } WHILE_0
#define STRESS_LOG5(facility, level, msg, data1, data2, data3, data4, data5)   do { } WHILE_0
#define STRESS_LOG6(facility, level, msg, data1, data2, data3, data4, data5, data6)   do { } WHILE_0
#define STRESS_LOG7(facility, level, msg, data1, data2, data3, data4, data5, data6, data7)   do { } WHILE_0
#define STRESS_LOG_PLUG_MOVE(plug_start, plug_end, plug_delta)          do { } WHILE_0
#define STRESS_LOG_ROOT_PROMOTE(root_addr, objPtr, methodTable)         do { } WHILE_0
#define STRESS_LOG_ROOT_RELOCATE(root_addr, old_value, new_value, methodTable) do { } WHILE_0
#define STRESS_LOG_GC_START(gcCount, Gen, collectClasses)               do { } WHILE_0
#define STRESS_LOG_GC_END(gcCount, Gen, collectClasses)                 do { } WHILE_0
#define STRESS_LOG_OOM_STACK(size)   do { } while(0)
#define STRESS_LOG_RESERVE_MEM(numChunks) do {} while (0)
#define STRESS_LOG_GC_STACK

#define LOG(x)

#define SVAL_IMPL_INIT(type, cls, var, init) \
    type cls::var = init

//
// Thread
//

struct alloc_context;

class Thread
{
    bool m_fPreemptiveGCDisabled;
    uintptr_t m_alloc_context[16]; // Reserve enough space to fix allocation context

    friend class ThreadStore;
    Thread * m_pNext;

public:
    Thread()
    {
    }

    bool PreemptiveGCDisabled()
    {
        return m_fPreemptiveGCDisabled;
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
        GCSTRESS_TRANSITION = 2,    // GC on transitions to preemptive GC
        GCSTRESS_INSTR_JIT = 4,    // GC on every allowable JITed instr
        GCSTRESS_INSTR_NGEN = 8,    // GC on every allowable NGEN instr
        GCSTRESS_UNIQUE = 16,   // GC only on a unique stack trace
    };
};

#include "etmdummy.h"
#define ETW_EVENT_ENABLED(e,f) false

#endif // __GCENV_H__
