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

#define STRESS_LOG0(facility, level, msg) do {                                \
            if (StressLog::LogOn(facility, level))                            \
                StressLog::LogMsg(level, facility, StressLogMsg(msg));        \
            LOG((facility, level, msg));                                      \
            } while(0)

#define STRESS_LOG1(facility, level, msg, data1) do {                              \
            if (StressLog::LogOn(facility, level))                                 \
                StressLog::LogMsg(level, facility,                                 \
                    StressLogMsg(msg, (void*)(size_t)(data1)));                    \
            LOG((facility, level, msg, data1));                                    \
            } while(0)

#define STRESS_LOG2(facility, level, msg, data1, data2) do {                            \
            if (StressLog::LogOn(facility, level))                                      \
                StressLog::LogMsg(level, facility,                                      \
                    StressLogMsg(msg, (void*)(size_t)(data1), (void*)(size_t)(data2))); \
            LOG((facility, level, msg, data1, data2));                                  \
            } while(0)

#define STRESS_LOG2_CHECK_EE_STARTED(facility, level, msg, data1, data2) do { \
            if (g_fEEStarted)                                                 \
                STRESS_LOG2(facility, level, msg, data1, data2);              \
            else                                                              \
                LOG((facility, level, msg, data1, data2));                    \
            } while(0)

#define STRESS_LOG3(facility, level, msg, data1, data2, data3) do {                             \
            if (StressLog::LogOn(facility, level))                                              \
                StressLog::LogMsg(level, facility,                                              \
                    StressLogMsg(msg,                                                           \
                        (void*)(size_t)(data1),(void*)(size_t)(data2),(void*)(size_t)(data3))); \
            LOG((facility, level, msg, data1, data2, data3));                                   \
            } while(0)

#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4) do {                              \
            if (StressLog::LogOn(facility, level))                                                      \
                StressLog::LogMsg(level, facility,                                                      \
                    StressLogMsg(msg, (void*)(size_t)(data1),(void*)(size_t)(data2),                    \
                        (void*)(size_t)(data3),(void*)(size_t)(data4)));                                \
            LOG((facility, level, msg, data1, data2, data3, data4));                                    \
            } while(0)

#define STRESS_LOG5(facility, level, msg, data1, data2, data3, data4, data5) do {                       \
            if (StressLog::LogOn(facility, level))                                                      \
                StressLog::LogMsg(level, facility, 5, msg, (void*)(size_t)(data1),                      \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),               \
                    (void*)(size_t)(data5));                                                            \
            LOG((facility, level, msg, data1, data2, data3, data4, data5));                             \
            } while(0)

#define STRESS_LOG6(facility, level, msg, data1, data2, data3, data4, data5, data6) do {                \
            if (StressLog::LogOn(facility, level))                                                      \
                StressLog::LogMsg(level, facility, 6, msg, (void*)(size_t)(data1),                      \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),               \
                    (void*)(size_t)(data5), (void*)(size_t)(data6));                                    \
            LOG((facility, level, msg, data1, data2, data3, data4, data5, data6));                      \
            } while(0)

#define STRESS_LOG7(facility, level, msg, data1, data2, data3, data4, data5, data6, data7) do {         \
            if (StressLog::LogOn(facility, level))                                                      \
                StressLog::LogMsg(level, facility, 7, msg, (void*)(size_t)(data1),                      \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),               \
                    (void*)(size_t)(data5), (void*)(size_t)(data6), (void*)(size_t)(data7));            \
            LOG((facility, level, msg, data1, data2, data3, data4, data5, data6, data7));               \
            } while(0)

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

    StressLogMsg(const char* format) : m_cArgs(0), m_format(format)
    {
    }

    template < typename T1 >
    StressLogMsg(const char* format, T1 data1) : m_cArgs(1), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
    }

    template < typename T1, typename T2 >
    StressLogMsg(const char* format, T1 data1, T2 data2) : m_cArgs(2), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
    }

    template < typename T1, typename T2, typename T3 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3) : m_cArgs(3), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
    }

    template < typename T1, typename T2, typename T3, typename T4 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4) : m_cArgs(4), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5) : m_cArgs(5), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6) : m_cArgs(6), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7) : m_cArgs(7), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8) : m_cArgs(8), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
        m_args[7] = (void*)(size_t)data8;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9) : m_cArgs(9), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
        m_args[7] = (void*)(size_t)data8;
        m_args[8] = (void*)(size_t)data9;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10) : m_cArgs(10), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
        m_args[7] = (void*)(size_t)data8;
        m_args[8] = (void*)(size_t)data9;
        m_args[9] = (void*)(size_t)data10;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10, typename T11 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10, T11 data11) : m_cArgs(11), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*) && sizeof(T11) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
        m_args[7] = (void*)(size_t)data8;
        m_args[8] = (void*)(size_t)data9;
        m_args[9] = (void*)(size_t)data10;
        m_args[10] = (void*)(size_t)data11;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10, typename T11, typename T12 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10, T11 data11, T12 data12) : m_cArgs(12), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*) && sizeof(T11) <= sizeof(void*) && sizeof(T12) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
        m_args[7] = (void*)(size_t)data8;
        m_args[8] = (void*)(size_t)data9;
        m_args[9] = (void*)(size_t)data10;
        m_args[10] = (void*)(size_t)data11;
        m_args[11] = (void*)(size_t)data12;
    }


    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10, typename T11, typename T12, typename T13 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10, T11 data11, T12 data12, T13 data13) : m_cArgs(13), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*) && sizeof(T11) <= sizeof(void*) && sizeof(T12) <= sizeof(void*) && sizeof(T13) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
        m_args[7] = (void*)(size_t)data8;
        m_args[8] = (void*)(size_t)data9;
        m_args[9] = (void*)(size_t)data10;
        m_args[10] = (void*)(size_t)data11;
        m_args[11] = (void*)(size_t)data12;
        m_args[12] = (void*)(size_t)data13;
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10, typename T11, typename T12, typename T13, typename T14 >
    StressLogMsg(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10, T11 data11, T12 data12, T13 data13, T14 data14) : m_cArgs(14), m_format(format)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*) && sizeof(T11) <= sizeof(void*) && sizeof(T12) <= sizeof(void*) && sizeof(T13) <= sizeof(void*) && sizeof(T14) <= sizeof(void*));
        m_args[0] = (void*)(size_t)data1;
        m_args[1] = (void*)(size_t)data2;
        m_args[2] = (void*)(size_t)data3;
        m_args[3] = (void*)(size_t)data4;
        m_args[4] = (void*)(size_t)data5;
        m_args[5] = (void*)(size_t)data6;
        m_args[6] = (void*)(size_t)data7;
        m_args[7] = (void*)(size_t)data8;
        m_args[8] = (void*)(size_t)data9;
        m_args[9] = (void*)(size_t)data10;
        m_args[10] = (void*)(size_t)data11;
        m_args[11] = (void*)(size_t)data12;
        m_args[12] = (void*)(size_t)data13;
        m_args[13] = (void*)(size_t)data14;
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
