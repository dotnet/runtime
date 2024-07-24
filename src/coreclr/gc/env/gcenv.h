// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCENV_H__
#define __GCENV_H__

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
#include "gcinterface.h"
#include "gcenv.ee.h"
#include "volatile.h"

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

#define STRESS_LOG
#ifdef STRESS_LOG
/*  STRESS_LOG_VA was added to allow sending GC trace output to the stress log. msg must be enclosed
    in ()'s and contain a format string followed by 0 to 12 arguments. The arguments must be numbers
     or string literals. This was done because GC Trace uses dprintf which doesn't contain info on
    how many arguments are getting passed in and using va_args would require parsing the format
    string during the GC
*/
#define STRESS_LOG_VA(level, msg) do {                                        \
            if (StressLog::LogOn(LF_GC, LL_ALWAYS))                           \
                StressLog::LogMsg(level, StressLogMsg msg);                   \
            LOGALWAYS(msg);                                                   \
            } while(0)

#define STRESS_LOG_WRITE(facility, level, msg, ...) do {                            \
            if (StressLog::LogOn(facility, level))                                  \
                StressLog::LogMsg(facility, level, StressLogMsg(msg, __VA_ARGS__)); \
            LOG((facility, level, msg, __VA_ARGS__));                               \
            } while(0)

#define STRESS_LOG0(facility, level, msg) do {                                \
            if (StressLog::LogOn(facility, level))                            \
                StressLog::LogMsg(level, facility, StressLogMsg(msg));        \
            LOG((facility, level, msg));                                      \
            } while(0)

#define STRESS_LOG1(facility, level, msg, data1) \
    STRESS_LOG_WRITE(facility, level, msg, data1)

#define STRESS_LOG2(facility, level, msg, data1, data2) \
    STRESS_LOG_WRITE(facility, level, msg, data1, data2)

#define STRESS_LOG3(facility, level, msg, data1, data2, data3) \
    STRESS_LOG_WRITE(facility, level, msg, data1, data2, data3)

#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4) \
    STRESS_LOG_WRITE(facility, level, msg, data1, data2, data3, data4)

#define STRESS_LOG5(facility, level, msg, data1, data2, data3, data4, data5) \
    STRESS_LOG_WRITE(facility, level, msg, data1, data2, data3, data4, data5)

#define STRESS_LOG6(facility, level, msg, data1, data2, data3, data4, data5, data6) \
    STRESS_LOG_WRITE(facility, level, msg, data1, data2, data3, data4, data5, data6)

#define STRESS_LOG7(facility, level, msg, data1, data2, data3, data4, data5, data6, data7)  \
    STRESS_LOG_WRITE(facility, level, msg, data1, data2, data3, data4, data5, data6, data7)

#define LOGALWAYS(msg)

#define static_assert_no_msg( cond ) static_assert( cond, #cond )

enum LogFacility
{
    LF_GC       = 0x00000001,
    LF_GCALLOC  = 0x00000100,
    LF_GCROOTS  = 0x00080000,
    LF_ALWAYS   = 0x80000000,
};

enum LogLevel
{
    LL_ALWAYS,
    LL_FATALERROR,
    LL_ERROR,
    LL_WARNING,
    LL_INFO10,       // can be expected to generate 10 logs per small but not trivial run
    LL_INFO100,      // can be expected to generate 100 logs per small but not trivial run
    LL_INFO1000,     // can be expected to generate 1,000 logs per small but not trivial run
    LL_INFO10000,    // can be expected to generate 10,000 logs per small but not trivial run
    LL_INFO100000,   // can be expected to generate 100,000 logs per small but not trivial run
    LL_INFO1000000,  // can be expected to generate 1,000,000 logs per small but not trivial run
    LL_EVERYTHING,
};

#define STRESS_LOG_PLUG_MOVE(plug_start, plug_end, plug_delta) do {                                                 \
            if (StressLog::LogOn(LF_GC, LL_INFO1000))                                                               \
                StressLog::LogMsg(LL_INFO1000, LF_GC,                                                               \
                    StressLogMsg(ThreadStressLog::gcPlugMoveMsg(),                                                  \
                        (void*)(size_t)(plug_start), (void*)(size_t)(plug_end), (void*)(size_t)(plug_delta)));      \
            LOG((LF_GC, LL_INFO10000, ThreadStressLog::gcPlugMoveMsg(), (plug_start), (plug_end), (plug_delta)));   \
            } while(0)

#define STRESS_LOG_ROOT_PROMOTE(root_addr, objPtr, methodTable) do {                                                            \
            if (StressLog::LogOn(LF_GC|LF_GCROOTS, LL_INFO1000))                                                                \
                StressLog::LogMsg(LL_INFO1000, LF_GC|LF_GCROOTS,                                                                \
                    StressLogMsg(ThreadStressLog::gcRootPromoteMsg(),                                                           \
                        (void*)(size_t)(root_addr), (void*)(size_t)(objPtr), (void*)(size_t)(methodTable)));                    \
            LOG((LF_GC|LF_GCROOTS, LL_INFO1000000, ThreadStressLog::gcRootPromoteMsg(), (root_addr), (objPtr), (methodTable))); \
            } while(0)

#define STRESS_LOG_ROOT_RELOCATE(root_addr, old_value, new_value, methodTable) do {                                                     \
            if (StressLog::LogOn(LF_GC|LF_GCROOTS, LL_INFO1000) && ((size_t)(old_value) != (size_t)(new_value)))                        \
                StressLog::LogMsg(LL_INFO1000, LF_GC|LF_GCROOTS,                                                                        \
                    StressLogMsg(ThreadStressLog::gcRootMsg(),                                                                          \
                    (void*)(size_t)(root_addr), (void*)(size_t)(old_value),                                                             \
                    (void*)(size_t)(new_value), (void*)(size_t)(methodTable)));                                                         \
            LOG((LF_GC|LF_GCROOTS, LL_INFO10000, ThreadStressLog::gcRootMsg(), (root_addr), (old_value), (new_value), (methodTable)));  \
            } while(0)

#define STRESS_LOG_GC_START(gcCount, Gen, collectClasses) do {                                                                  \
            if (StressLog::LogOn(LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10))                                                       \
                StressLog::LogMsg(LL_INFO10, LF_GCROOTS|LF_GC|LF_GCALLOC,                                                       \
                    StressLogMsg(ThreadStressLog::gcStartMsg(),                                                                 \
                        (void*)(size_t)(gcCount), (void*)(size_t)(Gen), (void*)(size_t)(collectClasses)));                      \
            LOG((LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10, ThreadStressLog::gcStartMsg(), (gcCount), (Gen), (collectClasses)));   \
            } while(0)

#define STRESS_LOG_GC_END(gcCount, Gen, collectClasses) do {                                                                    \
            if (StressLog::LogOn(LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10))                                                       \
                StressLog::LogMsg(LL_INFO10, LF_GCROOTS|LF_GC|LF_GCALLOC,                                                       \
                    StressLogMsg(ThreadStressLog::gcEndMsg(),                                                                   \
                        (void*)(size_t)(gcCount), (void*)(size_t)(Gen), (void*)(size_t)(collectClasses), 0));                   \
            LOG((LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10, ThreadStressLog::gcEndMsg(), (gcCount), (Gen), (collectClasses)));     \
            } while(0)

#define STRESS_LOG_OOM_STACK(size)
#define STRESS_LOG_GC_STACK

class ThreadStressLog
{
public:
    #include "../../inc/gcmsg.inl"
};

struct StressLogMsg
{
    int m_cArgs;
    const char* m_format;
    void* m_args[16];

    template<typename T>
    static void* ConvertArgument(T arg)
    {
        static_assert_no_msg(sizeof(T) <= sizeof(void*));
        return (void*)(size_t)arg;
    }

    template<>
    void* ConvertArgument(float arg) = delete;

#if TARGET_64BIT
    template<>
    void* ConvertArgument(double arg)
    {
        return (void*)(size_t)(*((uint64_t*)&arg));
    }
#else
    template<>
    void* ConvertArgument(double arg) = delete;
#endif

    StressLogMsg(const char* format) : m_cArgs(0), m_format(format)
    {
    }

    template<typename... Ts>
    StressLogMsg(const char* format, Ts... args)
        : m_cArgs(sizeof...(args))
        , m_format(format)
        , m_args{ ConvertArgument(args)... }
    {
        static_assert_no_msg(sizeof...(args) <= ARRAY_SIZE(m_args));
    }
};

class StressLog
{
public:
    static bool LogOn(unsigned, unsigned)
    {
        return true;
    }

    static BOOL StressLogOn(unsigned facility, unsigned level)
    {
        return true;
    }

    static void LogMsg(unsigned dprintfLevel, const StressLogMsg& msg)
    {
        GCToEEInterface::LogStressMsg(LL_ALWAYS, LF_ALWAYS|(dprintfLevel<<16)|LF_GC, msg);
    }

    static void LogMsg(unsigned level, unsigned facility, const StressLogMsg& msg)
    {
        GCToEEInterface::LogStressMsg(level, facility, msg);
    }

};
#else
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

#endif // USE_STRESS_LOG

#define LOG(x)

#define SVAL_IMPL_INIT(type, cls, var, init) \
    type cls::var = init

#include "etmdummy.h"
#define ETW_EVENT_ENABLED(e,f) false

#endif // __GCENV_H__
