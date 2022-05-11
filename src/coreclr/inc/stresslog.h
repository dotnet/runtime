// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*************************************************************************************/
/*                                   StressLog.h                                     */
/*************************************************************************************/

/* StressLog is a binary, memory based circular queue of logging messages.  It is
   intended to be used in retail builds during stress runs (activated
   by registry key), so to help find bugs that only turn up during stress runs.

   It is meant to have very low overhead and can not cause deadlocks, etc.  It is
   however thread safe */

/* The log has a very simple structure, and it meant to be dumped from a NTSD
   extention (eg. strike). There is no memory allocation system calls etc to purtub things */

// ******************************************************************************
// WARNING!!!: These classes are used by SOS in the diagnostics repo. Values should
// added or removed in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/master/src/inc/stresslog.h
// Parser: https://github.com/dotnet/diagnostics/blob/master/src/SOS/Strike/stressLogDump.cpp
// ******************************************************************************

/*************************************************************************************/

#ifndef StressLog_h
#define StressLog_h  1

#include "log.h"

#if defined(STRESS_LOG) && !defined(FEATURE_NO_STRESSLOG)
#ifndef STRESS_LOG_ANALYZER
#include "holder.h"
#include "staticcontract.h"
#include "mscoree.h"
#include "clrinternal.h"
#ifdef STRESS_LOG_READONLY
#include <stddef.h> // offsetof
#else //STRESS_LOG_READONLY
#include "clrhost.h"
#endif //STRESS_LOG_READONLY

#ifndef _ASSERTE
#define _ASSERTE(expr)
#endif
#else
#include <stddef.h> // offsetof
#endif // STRESS_LOG_ANALYZER

/* The STRESS_LOG* macros work like printf.  In fact the use printf in their implementation
   so all printf format specifications work.  In addition the Stress log dumper knows
   about certain suffixes for the %p format specification (normally used to print a pointer)

            %pM     // The pointer is a MethodDesc
            %pT     // The pointer is a type (MethodTable)
            %pV     // The pointer is a C++ Vtable pointer (useful for distinguishing different types of frames
            %pK     // The pointer is a code address (used for stack track)
*/

/*  STRESS_LOG_VA was added to allow sendign GC trace output to the stress log. msg must be enclosed
      in ()'s and contain a format string followed by 0 - 4 arguments.  The arguments must be numbers or
      string literals.  LogMsgOL is overloaded so that all of the possible sets of parameters are covered.
      This was done becasue GC Trace uses dprintf which dosen't contain info on how many arguments are
      getting passed in and using va_args would require parsing the format string during the GC
*/
#define STRESS_LOG_VA(dprintfLevel,msg) do {                                                        \
            if (StressLog::LogOn(LF_GC, LL_ALWAYS))                                                 \
                StressLog::LogMsg(LL_ALWAYS, LF_ALWAYS|(dprintfLevel<<16)|LF_GC, StressLogMsg msg); \
            LOGALWAYS(msg);                                                                         \
            } while(0)

#define STRESS_LOG0(facility, level, msg) do {                                \
            if (StressLog::LogOn(facility, level))                            \
                StressLog::LogMsg(level, facility, 0, msg);                   \
            LOG((facility, level, msg));                                      \
            } while(0)

#define STRESS_LOG1(facility, level, msg, data1) do {                              \
            if (StressLog::LogOn(facility, level))                                 \
                StressLog::LogMsg(level, facility, 1, msg, (void*)(size_t)(data1));\
            LOG((facility, level, msg, data1));                                    \
            } while(0)

#define STRESS_LOG2(facility, level, msg, data1, data2) do {                       \
            if (StressLog::LogOn(facility, level))                                 \
                StressLog::LogMsg(level, facility, 2, msg,                         \
                    (void*)(size_t)(data1), (void*)(size_t)(data2));               \
            LOG((facility, level, msg, data1, data2));                             \
            } while(0)

#define STRESS_LOG2_CHECK_EE_STARTED(facility, level, msg, data1, data2) do { \
            if (g_fEEStarted)                                                 \
                STRESS_LOG2(facility, level, msg, data1, data2);              \
            else                                                              \
                LOG((facility, level, msg, data1, data2));                    \
            } while(0)

#define STRESS_LOG3(facility, level, msg, data1, data2, data3) do {                           \
            if (StressLog::LogOn(facility, level))                                            \
                StressLog::LogMsg(level, facility, 3, msg,                                    \
                    (void*)(size_t)(data1),(void*)(size_t)(data2),(void*)(size_t)(data3));    \
            LOG((facility, level, msg, data1, data2, data3));                                 \
            } while(0)

#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4) do {                              \
            if (StressLog::LogOn(facility, level))                                                      \
                StressLog::LogMsg(level, facility, 4, msg, (void*)(size_t)(data1),                      \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4));              \
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

#define STRESS_LOG_COND0(facility, level, cond, msg) do {                     \
            if (StressLog::LogOn(facility, level) && (cond))                  \
                StressLog::LogMsg(level, facility, 0, msg);                   \
            LOG((facility, level, msg));                                      \
            } while(0)

#define STRESS_LOG_COND1(facility, level, cond, msg, data1) do {                    \
            if (StressLog::LogOn(facility, level) && (cond))                        \
                StressLog::LogMsg(level, facility, 1, msg, (void*)(size_t)(data1)); \
            LOG((facility, level, msg, data1));                                     \
            } while(0)

#define STRESS_LOG_COND2(facility, level, cond, msg, data1, data2) do {            \
            if (StressLog::LogOn(facility, level) && (cond))                       \
                StressLog::LogMsg(level, facility, 2, msg,                         \
                    (void*)(size_t)(data1), (void*)(size_t)(data2));               \
            LOG((facility, level, msg, data1, data2));                             \
            } while(0)

#define STRESS_LOG_COND3(facility, level, cond, msg, data1, data2, data3) do {                \
            if (StressLog::LogOn(facility, level) && (cond))                                  \
                StressLog::LogMsg(level, facility, 3, msg,                                    \
                    (void*)(size_t)(data1),(void*)(size_t)(data2),(void*)(size_t)(data3));    \
            LOG((facility, level, msg, data1, data2, data3));                                 \
            } while(0)

#define STRESS_LOG_COND4(facility, level, cond, msg, data1, data2, data3, data4) do {                   \
            if (StressLog::LogOn(facility, level) && (cond))                                            \
                StressLog::LogMsg(level, facility, 4, msg, (void*)(size_t)(data1),                      \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4));              \
            LOG((facility, level, msg, data1, data2, data3, data4));                                    \
            } while(0)

#define STRESS_LOG_COND5(facility, level, cond, msg, data1, data2, data3, data4, data5) do {            \
            if (StressLog::LogOn(facility, level) && (cond))                                            \
                StressLog::LogMsg(level, facility, 5, msg, (void*)(size_t)(data1),                      \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),               \
                    (void*)(size_t)(data5));                                                            \
            LOG((facility, level, msg, data1, data2, data3, data4, data5));                             \
            } while(0)

#define STRESS_LOG_COND6(facility, level, cond, msg, data1, data2, data3, data4, data5, data6) do {     \
            if (StressLog::LogOn(facility, level) && (cond))                                            \
                StressLog::LogMsg(level, facility, 6, msg, (void*)(size_t)(data1),                      \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),               \
                    (void*)(size_t)(data5), (void*)(size_t)(data6));                                    \
            LOG((facility, level, msg, data1, data2, data3, data4, data5, data6));                      \
            } while(0)

#define STRESS_LOG_COND7(facility, level, cond, msg, data1, data2, data3, data4, data5, data6, data7) do { \
            if (StressLog::LogOn(facility, level) && (cond))                                               \
                StressLog::LogMsg(level, facility, 7, msg, (void*)(size_t)(data1),                         \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),                  \
                    (void*)(size_t)(data5), (void*)(size_t)(data6), (void*)(size_t)(data7));               \
            LOG((facility, level, msg, data1, data2, data3, data4, data5, data6, data7));                  \
            } while(0)

#define STRESS_LOG_RESERVE_MEM(numChunks) do {                              \
            if (StressLog::StressLogOn(LF_ALL, LL_ALWAYS))                  \
                {StressLog::ReserveStressLogChunks (numChunks);}            \
            } while(0)
// !!! WARNING !!!
// !!! DO NOT ADD STRESS_LOG8, as the stress log infrastructure supports a maximum of 7 arguments
// !!! WARNING !!!

#define STRESS_LOG_PLUG_MOVE(plug_start, plug_end, plug_delta) do {                                                 \
            if (StressLog::LogOn(LF_GC, LL_INFO1000))                                                               \
                StressLog::LogMsg(LL_INFO1000, LF_GC, 3, ThreadStressLog::gcPlugMoveMsg(),                          \
                (void*)(size_t)(plug_start), (void*)(size_t)(plug_end), (void*)(size_t)(plug_delta));               \
            LOG((LF_GC, LL_INFO10000, ThreadStressLog::gcPlugMoveMsg(), (plug_start), (plug_end), (plug_delta)));   \
            } while(0)

#define STRESS_LOG_ROOT_PROMOTE(root_addr, objPtr, methodTable) do {                                                            \
            if (StressLog::LogOn(LF_GC|LF_GCROOTS, LL_INFO1000))                                                                \
                StressLog::LogMsg(LL_INFO1000, LF_GC|LF_GCROOTS, 3, ThreadStressLog::gcRootPromoteMsg(),                        \
                    (void*)(size_t)(root_addr), (void*)(size_t)(objPtr), (void*)(size_t)(methodTable));                         \
            LOG((LF_GC|LF_GCROOTS, LL_INFO1000000, ThreadStressLog::gcRootPromoteMsg(), (root_addr), (objPtr), (methodTable))); \
            } while(0)

#define STRESS_LOG_ROOT_RELOCATE(root_addr, old_value, new_value, methodTable) do {                                                     \
            if (StressLog::LogOn(LF_GC|LF_GCROOTS, LL_INFO1000) && ((size_t)(old_value) != (size_t)(new_value)))                        \
                StressLog::LogMsg(LL_INFO1000, LF_GC|LF_GCROOTS, 4, ThreadStressLog::gcRootMsg(),                                       \
                    (void*)(size_t)(root_addr), (void*)(size_t)(old_value),                                                             \
                    (void*)(size_t)(new_value), (void*)(size_t)(methodTable));                                                          \
            LOG((LF_GC|LF_GCROOTS, LL_INFO10000, ThreadStressLog::gcRootMsg(), (root_addr), (old_value), (new_value), (methodTable)));  \
            } while(0)

#define STRESS_LOG_GC_START(gcCount, Gen, collectClasses) do {                                                                  \
            if (StressLog::LogOn(LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10))                                                       \
                StressLog::LogMsg(LL_INFO10, LF_GCROOTS|LF_GC|LF_GCALLOC, 3, ThreadStressLog::gcStartMsg(),                     \
                    (void*)(size_t)(gcCount), (void*)(size_t)(Gen), (void*)(size_t)(collectClasses));                           \
            LOG((LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10, ThreadStressLog::gcStartMsg(), (gcCount), (Gen), (collectClasses)));   \
            } while(0)

#define STRESS_LOG_GC_END(gcCount, Gen, collectClasses) do {                                                                    \
            if (StressLog::LogOn(LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10))                                                       \
                StressLog::LogMsg(LL_INFO10, LF_GCROOTS|LF_GC|LF_GCALLOC, 3, ThreadStressLog::gcEndMsg(),                       \
                    (void*)(size_t)(gcCount), (void*)(size_t)(Gen), (void*)(size_t)(collectClasses), 0);                        \
            LOG((LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10, ThreadStressLog::gcEndMsg(), (gcCount), (Gen), (collectClasses)));     \
            } while(0)

#if defined(_DEBUG)
#define MAX_CALL_STACK_TRACE          20
#define STRESS_LOG_OOM_STACK(size) do {                                                           \
                CantAllocHolder caHolder;                                                         \
                if (StressLog::LogOn(LF_EEMEM, LL_ALWAYS))                                        \
                {                                                                                 \
                    StressLog::LogMsgOL("OOM on alloc of size %x \n", (void*)(size_t)(size));     \
                    StressLog::LogCallStack ("OOM");                                              \
                }                                                                                 \
            } while(0)
#define STRESS_LOG_GC_STACK do {                                                                  \
                if (StressLog::LogOn(LF_GC |LF_GCINFO, LL_ALWAYS))                                \
                {                                                                                 \
                    StressLog::LogMsgOL("GC is triggered \n");                                    \
                    StressLog::LogCallStack ("GC");                                               \
                }                                                                                 \
            } while(0)

#else //!_DEBUG
#define STRESS_LOG_OOM_STACK(size)
#define STRESS_LOG_GC_STACK
#endif //_DEBUG

class ThreadStressLog;

struct StressLogMsg;

/*************************************************************************************/
/* a log is a circular queue of messages */

class StressLog {
public:
    static void Initialize(unsigned facilities, unsigned level, unsigned maxBytesPerThread,
        unsigned maxBytesTotal, void* moduleBase, LPWSTR logFilename = nullptr);
    static void Terminate(BOOL fProcessDetach=FALSE);
    static void ThreadDetach();         // call at DllMain  THREAD_DETACH if you want to recycle thread logs
#ifndef STRESS_LOG_ANALYZER
    static int NewChunk ()
    {
        return InterlockedIncrement (&theLog.totalChunk);
    }
    static int ChunkDeleted ()
    {
        return InterlockedDecrement (&theLog.totalChunk);
    }
#endif //STRESS_LOG_ANALYZER

    //the result is not 100% accurate. If multiple threads call this function at the same time,
    //we could allow the total size be bigger than required. But the memory won't grow forever
    //and this is not critical so we don't try to fix the race
    static BOOL AllowNewChunk (LONG numChunksInCurThread);

    //preallocate Stress log chunks for current thread. The memory we could preallocate is still
    //bounded by per thread size limit and total size limit. If chunksToReserve is 0, we will try to
    //preallocate up to per thread size limit
    static BOOL ReserveStressLogChunks (unsigned chunksToReserve);

    // used by out of process debugger to dump the stress log to 'fileName'
    // IDebugDataSpaces is the NTSD execution callback for getting process memory.
    // This function is defined in the tools\strike\stressLogDump.cpp file
    static HRESULT Dump(ULONG64 logAddr, const char* fileName, struct IDebugDataSpaces* memCallBack);

    static BOOL StressLogOn(unsigned facility, unsigned level);
    static BOOL ETWLogOn(unsigned facility, unsigned level);
    static BOOL LogOn(unsigned facility, unsigned level);

// private:
    unsigned facilitiesToLog;               // Bitvector of facilities to log (see loglf.h)
    unsigned levelToLog;                    // log level (see log.h)
    unsigned MaxSizePerThread;              // maximum number of bytes each thread should have before wrapping
    unsigned MaxSizeTotal;                  // maximum memory allowed for stress log
    Volatile<LONG> totalChunk;              // current number of total chunks allocated
    Volatile<ThreadStressLog*> logs;        // the list of logs for every thread.
    unsigned padding;                       // Preserve the layout for SOS
    Volatile<LONG> deadCount;               // count of dead threads in the log
    CRITSEC_COOKIE lock;                    // lock
    unsigned __int64 tickFrequency;         // number of ticks per second
    unsigned __int64 startTimeStamp;        // start time from when tick counter started
    FILETIME startTime;                     // time the application started
    SIZE_T   moduleOffset;                  // Used to compute format strings.
    struct ModuleDesc
    {
        uint8_t* baseAddress;
        size_t        size;
    };
    static const size_t MAX_MODULES = 5;
    ModuleDesc    modules[MAX_MODULES];     // descriptor of the modules images

#if defined(HOST_64BIT)
#define MEMORY_MAPPED_STRESSLOG
#ifdef HOST_WINDOWS
#define MEMORY_MAPPED_STRESSLOG_BASE_ADDRESS (void*)0x400000000000
#else
#define MEMORY_MAPPED_STRESSLOG_BASE_ADDRESS nullptr
#endif
#endif

#ifdef STRESS_LOG_ANALYZER
    static size_t writing_base_address;
    static size_t reading_base_address;

    template<typename T>
    static T* TranslateMemoryMappedPointer(T* input)
    {
        if (input == nullptr)
        {
            return nullptr;
        }

        return ((T*)(((uint8_t*)input) - writing_base_address + reading_base_address));
    }
#else
    template<typename T>
    static T* TranslateMemoryMappedPointer(T* input)
    {
        return input;
    }
#endif

#ifdef MEMORY_MAPPED_STRESSLOG

    // 
    // Intentionally avoid unmapping the file during destructor to avoid a race 
    // condition between additional logging in other thread and the destructor.
    // 
    // The operating system will make sure the file get unmapped during process shutdown
    // 
    LPVOID hMapView; 
    static void* AllocMemoryMapped(size_t n);

    struct StressLogHeader
    {
        size_t        headerSize;               // size of this header including size field and moduleImage
        uint32_t      magic;                    // must be 'STRL'
        uint32_t      version;                  // must be 0x00010001
        uint8_t*      memoryBase;               // base address of the memory mapped file
        uint8_t*      memoryCur;                // highest address currently used
        uint8_t*      memoryLimit;              // limit that can be used
        Volatile<ThreadStressLog*>  logs;       // the list of logs for every thread.
        uint64_t      tickFrequency;            // number of ticks per second
        uint64_t      startTimeStamp;           // start time from when tick counter started
        uint64_t      threadsWithNoLog;         // threads that didn't get a log
        uint64_t      reserved[15];             // for future expansion
        ModuleDesc    modules[MAX_MODULES];     // descriptor of the modules images
        uint8_t       moduleImage[64*1024*1024];// copy of the module images described by modules field
    };

    StressLogHeader* stressLogHeader;       // header to find things in the memory mapped file
#endif // MEMORY_MAPPED_STRESSLOG

    static thread_local ThreadStressLog* t_pCurrentThreadLog;

// private:
    static void Enter(CRITSEC_COOKIE dummy = NULL);
    static void Leave(CRITSEC_COOKIE dummy = NULL);
    static ThreadStressLog* CreateThreadStressLog();
    static ThreadStressLog* CreateThreadStressLogHelper();

    static BOOL InlinedStressLogOn(unsigned facility, unsigned level);
    static BOOL InlinedETWLogOn(unsigned facility, unsigned level);

    static void LogMsg(unsigned level, unsigned facility, int cArgs, const char* format, ... );

    static void LogMsg(unsigned level, unsigned facility, const StressLogMsg& msg);

    static void AddModule(uint8_t* moduleBase);

// Support functions for STRESS_LOG_VA
// We disable the warning "conversion from 'type' to 'type' of greater size" since everything will
// end up on the stack, and LogMsg will know the size of the variable based on the format string.
#ifdef _MSC_VER
#pragma warning( push )
#pragma warning( disable : 4312 )
#endif
    static void LogMsgOL(const char* format)
    { LogMsg(LL_ALWAYS, LF_GC, 0, format); }

    template < typename T1 >
    static void LogMsgOL(const char* format, T1 data1)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 1, format, (void*)(size_t)data1);
    }

    template < typename T1, typename T2 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 2, format, (void*)(size_t)data1, (void*)(size_t)data2);
    }

    template < typename T1, typename T2, typename T3 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 3, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3);
    }

    template < typename T1, typename T2, typename T3, typename T4 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 4, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 5, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 6, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 7, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6, (void*)(size_t)data7);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 8, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6, (void*)(size_t)data7, (void*)(size_t)data8);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 9, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6, (void*)(size_t)data7, (void*)(size_t)data8, (void*)(size_t)data9);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 10, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6, (void*)(size_t)data7, (void*)(size_t)data8, (void*)(size_t)data9, (void*)(size_t)data10);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10, typename T11 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10, T11 data11)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*) && sizeof(T11) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 11, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6, (void*)(size_t)data7, (void*)(size_t)data8, (void*)(size_t)data9, (void*)(size_t)data10, (void*)(size_t)data11);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7, typename T8, typename T9, typename T10, typename T11, typename T12 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9, T10 data10, T11 data11, T12 data12)
    {
        static_assert_no_msg(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*) && sizeof(T8) <= sizeof(void*) && sizeof(T9) <= sizeof(void*) && sizeof(T10) <= sizeof(void*) && sizeof(T11) <= sizeof(void*) && sizeof(T12) <= sizeof(void*));
        LogMsg(LL_ALWAYS, LF_GC, 12, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6, (void*)(size_t)data7, (void*)(size_t)data8, (void*)(size_t)data9, (void*)(size_t)data10, (void*)(size_t)data11, (void*)(size_t)data12);
    }

#ifdef _MSC_VER
#pragma warning( pop )
#endif

// We can only log the stacktrace on DEBUG builds!
#ifdef _DEBUG
typedef USHORT
(__stdcall *PFNRtlCaptureStackBackTrace)(
    IN ULONG FramesToSkip,
    IN ULONG FramesToCapture,
    OUT PVOID * BackTrace,
    OUT PULONG BackTraceHash);

    PFNRtlCaptureStackBackTrace RtlCaptureStackBackTrace;

    static void LogCallStack(const char *const callTag);
#endif //_DEBUG

// private: // static variables
    static StressLog theLog;    // We only have one log, and this is it
};

#ifndef STRESS_LOG_ANALYZER
typedef Holder<CRITSEC_COOKIE, StressLog::Enter, StressLog::Leave, NULL, CompareDefault<CRITSEC_COOKIE>> StressLogLockHolder;
#endif //!STRESS_LOG_ANALYZER

#if defined(DACCESS_COMPILE)
inline BOOL StressLog::LogOn(unsigned facility, unsigned level)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

    // StressLog isn't dacized, and besides we don't want to log to it in DAC builds.
    return FALSE;
}
#endif

/*************************************************************************************/
/* private classes */

#if defined(_MSC_VER)
#pragma warning(disable:4200 4201)					// don't warn about 0 sized array below or unnamed structures
#endif

// The order of fields is important.  Keep the prefix length as the first field.
// And make sure the timeStamp field is naturally alligned, so we don't waste
// space on 32-bit platforms
struct StressMsg {
    static const size_t formatOffsetBits = 26;
    union {
        struct {
            uint32_t numberOfArgs  : 3;                   // at most 7 arguments here
            uint32_t formatOffset  : formatOffsetBits;    // offset of string in mscorwks
            uint32_t numberOfArgsX : 3;                   // extend number of args in a backward compat way
        };
        uint32_t fmtOffsCArgs;    // for optimized access
    };
    uint32_t facility;                      // facility used to log the entry
    uint64_t timeStamp;                     // time when mssg was logged
    void*     args[0];                      // size given by numberOfArgs

    static const size_t maxArgCnt = 63;
    static const size_t maxOffset = 1 << formatOffsetBits;
    static size_t maxMsgSize ()
    { return sizeof(StressMsg) + maxArgCnt*sizeof(void*); }

    friend class ThreadStressLog;
    friend class StressLog;
};
#ifdef HOST_64BIT
#define STRESSLOG_CHUNK_SIZE (32 * 1024)
#else //HOST_64BIT
#define STRESSLOG_CHUNK_SIZE (16 * 1024)
#endif //HOST_64BIT
#define GC_STRESSLOG_MULTIPLY 5

// a chunk of memory for stress log
struct StressLogChunk
{
    StressLogChunk * prev;
    StressLogChunk * next;
    char buf[STRESSLOG_CHUNK_SIZE];
    DWORD dwSig1;
    DWORD dwSig2;

#if !defined(STRESS_LOG_READONLY)

#ifdef MEMORY_MAPPED_STRESSLOG
    static bool s_memoryMapped;
#endif //MEMORY_MAPPED_STRESSLOG

#ifdef HOST_WINDOWS
    static HANDLE s_LogChunkHeap;
#endif //HOST_WINDOWS

    void * operator new (size_t size) throw()
    {
        if (IsInCantAllocStressLogRegion ())
        {
            return NULL;
        }
#ifdef MEMORY_MAPPED_STRESSLOG
        if (s_memoryMapped)
            return StressLog::AllocMemoryMapped(size);
#endif //MEMORY_MAPPED_STRESSLOG
#ifdef HOST_WINDOWS
        _ASSERTE(s_LogChunkHeap);
        return HeapAlloc(s_LogChunkHeap, 0, size);
#else
        return malloc(size);
#endif //HOST_WINDOWS
    }

    void operator delete (void * chunk)
    {
#ifdef MEMORY_MAPPED_STRESSLOG
        if (s_memoryMapped)
            return;
#endif //MEMORY_MAPPED_STRESSLOG
#ifdef HOST_WINDOWS
        _ASSERTE(s_LogChunkHeap);
        HeapFree (s_LogChunkHeap, 0, chunk);
#else
        free(chunk);
#endif //HOST_WINDOWS
    }

#endif //!STRESS_LOG_READONLY

    StressLogChunk (StressLogChunk * p = NULL, StressLogChunk * n = NULL)
        :prev (p), next (n), dwSig1 (0xCFCFCFCF), dwSig2 (0xCFCFCFCF)
    {}

    char * StartPtr ()
    {
        return buf;
    }

    char * EndPtr ()
    {
        return buf + STRESSLOG_CHUNK_SIZE;
    }

    BOOL IsValid () const
    {
        return dwSig1 == 0xCFCFCFCF && dwSig2 == 0xCFCFCFCF;
    }
};

// This class implements a circular stack of variable sized elements
//    .The buffer between startPtr-endPtr is used in a circular manner
//     to store instances of the variable-sized struct StressMsg.
//     The StressMsg are always aligned to endPtr, while the space
//     left between startPtr and the last element is 0-padded.
//    .curPtr points to the most recently written log message
//    .readPtr points to the next log message to be dumped
//    .hasWrapped is TRUE while dumping the log, if we had wrapped
//     past the endPtr marker, back to startPtr
// The AdvanceRead/AdvanceWrite operations simply update the
//     readPtr / curPtr fields. thecaller is responsible for reading/writing
//     to the corresponding field
class ThreadStressLog {
#ifdef STRESS_LOG_ANALYZER
public:
#endif
    ThreadStressLog* next;      // we keep a linked list of these
    uint64_t   threadId;        // the id for the thread using this buffer
    uint8_t    isDead;          // Is this thread dead
    uint8_t    readHasWrapped;  // set when read ptr has passed chunkListTail
    uint8_t    writeHasWrapped; // set when write ptr has passed chunkListHead
    StressMsg* curPtr;          // where packets are being put on the queue
    StressMsg* readPtr;         // where we are reading off the queue (used during dumping)
    StressLogChunk * chunkListHead; //head of a list of stress log chunks
    StressLogChunk * chunkListTail; //tail of a list of stress log chunks
    StressLogChunk * curReadChunk; //the stress log chunk we are currently reading
    StressLogChunk * curWriteChunk; //the stress log chunk we are currently writing
    long       chunkListLength; // how many stress log chunks are in this stress log

#ifdef STRESS_LOG_READONLY
    FORCEINLINE StressMsg* AdvanceRead();
#endif //STRESS_LOG_READONLY
    FORCEINLINE StressMsg* AdvanceWrite(int cArgs);

#ifdef STRESS_LOG_READONLY
    inline StressMsg* AdvReadPastBoundary();
#endif //STRESS_LOG_READONLY
    inline StressMsg* AdvWritePastBoundary(int cArgs);

#ifdef STRESS_LOG_READONLY
    ThreadStressLog* FindLatestThreadLog() const;
#endif //STRESS_LOG_READONLY
    friend class StressLog;

#if !defined(STRESS_LOG_READONLY) && !defined(STRESS_LOG_ANALYZER)
    FORCEINLINE BOOL GrowChunkList ()
    {
        _ASSERTE (chunkListLength >= 1);
        if (!StressLog::AllowNewChunk (chunkListLength))
        {
            return FALSE;
        }
        StressLogChunk * newChunk = new StressLogChunk (chunkListTail, chunkListHead);
        if (newChunk == NULL)
        {
            return FALSE;
        }
        StressLog::NewChunk ();
        chunkListLength++;
        chunkListHead->prev = newChunk;
        chunkListTail->next = newChunk;
        chunkListHead = newChunk;

        return TRUE;
    }
#endif //!STRESS_LOG_READONLY && !STRESS_LOG_ANALYZER

public:
#if !defined(STRESS_LOG_READONLY) && !defined(STRESS_LOG_ANALYZER)
    ThreadStressLog ()
    {
        chunkListHead = chunkListTail = curWriteChunk = NULL;
        StressLogChunk * newChunk = new StressLogChunk;
        //OOM or in cantalloc region
        if (newChunk == NULL)
        {
            return;
        }
        StressLog::NewChunk ();

        newChunk->prev = newChunk;
        newChunk->next = newChunk;

        chunkListHead = chunkListTail = newChunk;

        next = NULL;
        threadId = 0;
        isDead = TRUE;
        curPtr = NULL;
        readPtr = NULL;
        writeHasWrapped = FALSE;
        curReadChunk = NULL;
        curWriteChunk = NULL;
        chunkListLength = 1;
    }

#endif //!STRESS_LOG_READONLY && !STRESS_LOG_ANALYZER

#if defined(MEMORY_MAPPED_STRESSLOG) && !defined(STRESS_LOG_ANALYZER)
    void* __cdecl operator new(size_t n, const NoThrow&) NOEXCEPT;
    void __cdecl operator delete (void * chunk);
#endif

    ~ThreadStressLog ()
    {
        //no thing to do if the list is empty (failed to initialize)
        if (chunkListHead == NULL)
        {
            return;
        }
#if !defined(STRESS_LOG_READONLY) && !defined(STRESS_LOG_ANALYZER)
        _ASSERTE (chunkListLength >= 1 && chunkListLength <= StressLog::theLog.totalChunk);
#endif //!STRESS_LOG_READONLY && !STRESS_LOG_ANALYZER
        StressLogChunk * chunk = chunkListHead;

        do
        {
            StressLogChunk * tmp = chunk;
            chunk = chunk->next;
            delete tmp;
#if !defined(STRESS_LOG_READONLY) && !defined(STRESS_LOG_ANALYZER)
            StressLog::ChunkDeleted ();
#endif //!STRESS_LOG_READONLY && !STRESS_LOG_ANALYZER
        } while (chunk != chunkListHead);
    }

    void Activate ()
    {
#ifndef STRESS_LOG_READONLY
        //there is no need to zero buffers because we could handle garbage contents
        threadId = GetCurrentThreadId ();
        isDead = FALSE;
        curWriteChunk = chunkListTail;
        curPtr = (StressMsg *)curWriteChunk->EndPtr ();
        writeHasWrapped = FALSE;
#else //STRESS_LOG_READONLY
        curReadChunk = curWriteChunk;
        readPtr = curPtr;
        readHasWrapped = FALSE;
        // the last written log, if it wrapped around may have partially overwritten
        // a previous record.  Update curPtr to reflect the last safe beginning of a record,
        // but curPtr shouldn't wrap around, otherwise it'll break our assumptions about stress
        // log
        curPtr = (StressMsg*)((char*)curPtr - StressMsg::maxMsgSize());
        if (curPtr < (StressMsg*)curWriteChunk->StartPtr())
        {
            curPtr = (StressMsg *)curWriteChunk->StartPtr();
        }
        //corner case: the log is empty
        if (readPtr == (StressMsg *)curReadChunk->EndPtr ())
        {
            AdvReadPastBoundary();
        }
#endif //!STRESS_LOG_READONLY
    }

    BOOL IsValid () const
    {
        return chunkListHead != NULL && (!curWriteChunk || StressLog::TranslateMemoryMappedPointer(curWriteChunk)->IsValid ());
    }

#ifdef STRESS_LOG_READONLY
    // Called while dumping.  Returns true after all messages in log were dumped
    FORCEINLINE BOOL CompletedDump ()
    {
        return readPtr->timeStamp == 0
                //if read has passed end of list but write has not passed head of list yet, we are done
                //if write has also wrapped, we are at the end if read pointer passed write pointer
                || (readHasWrapped &&
                        (!writeHasWrapped || (curReadChunk == curWriteChunk && readPtr >= curPtr)));
    }
#endif //STRESS_LOG_READONLY

    #include "gcmsg.inl"

    static const char* TaskSwitchMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "StressLog TaskSwitch Marker\n";
    }

    void LogMsg(unsigned facility, int cArgs, const char* format, ...);

    FORCEINLINE void LogMsg (unsigned facility, int cArgs, const char* format, va_list Args);
#ifdef STRESS_LOG_READONLY
    static size_t OffsetOfNext () {return offsetof (ThreadStressLog, next);}
    static size_t OffsetOfListHead () {return offsetof (ThreadStressLog, chunkListHead);}
#endif //STRESS_LOG_READONLY
};

#ifdef STRESS_LOG_READONLY
/*********************************************************************************/
// Called when dumping the log (by StressLog::Dump())
// Updates readPtr to point to next stress messaage to be dumped
// For convenience it returns the new value of readPtr
inline StressMsg* ThreadStressLog::AdvanceRead() {
    STATIC_CONTRACT_LEAF;
    // advance the marker
    readPtr = (StressMsg*)((char*)readPtr + sizeof(StressMsg) + readPtr->numberOfArgs*sizeof(void*));
    // wrap around if we need to
    if (readPtr >= (StressMsg *)curReadChunk->EndPtr ())
    {
        AdvReadPastBoundary();
    }
    return readPtr;
}

// It's the factored-out slow codepath for AdvanceRead() and
// is only called by AdvanceRead().
// Updates readPtr to and returns the first stress message >= startPtr
inline StressMsg* ThreadStressLog::AdvReadPastBoundary() {
    STATIC_CONTRACT_LEAF;
    //if we pass boundary of tail list, we need to set has Wrapped
    if (curReadChunk == chunkListTail)
    {
        readHasWrapped = TRUE;
        //If write has not wrapped, we know the contents from list head to
        //cur pointer is garbage, we don't need to read them
        if (!writeHasWrapped)
        {
            return readPtr;
        }
    }
    curReadChunk = curReadChunk->next;
    void** p = (void**)curReadChunk->StartPtr();
    while (*p == NULL && (size_t)(p-(void**)curReadChunk->StartPtr ()) < (StressMsg::maxMsgSize()/sizeof(void*)))
    {
        ++p;
    }
    // if we failed to find a valid start of a StressMsg fallback to startPtr (since timeStamp==0)
    if (*p == NULL)
    {
        p = (void**) curReadChunk->StartPtr ();
    }
    readPtr = (StressMsg*)p;

    return readPtr;
}
#endif //STRESS_LOG_READONLY
/*********************************************************************************/
// Called at runtime when writing the log (by StressLog::LogMsg())
// Updates curPtr to point to the next spot in the log where we can write
// a stress message with cArgs arguments
// For convenience it returns a pointer to the empty slot where we can
// write the next stress message.
// cArgs is the number of arguments in the message to be written.
inline StressMsg* ThreadStressLog::AdvanceWrite(int cArgs) {
    STATIC_CONTRACT_LEAF;
    // _ASSERTE(cArgs <= StressMsg::maxArgCnt);
    // advance the marker
    StressMsg* p = (StressMsg*)((char*)curPtr - sizeof(StressMsg) - cArgs*sizeof(void*));

    //past start of current chunk
    //wrap around if we need to
    if (p < (StressMsg*)curWriteChunk->StartPtr ())
    {
        return AdvWritePastBoundary(cArgs);
    }
    else
    {
        return p;
    }
}

// It's the factored-out slow codepath for AdvanceWrite() and
// is only called by AdvanceWrite().
// Returns the stress message flushed against endPtr
// In addition it writes NULLs b/w the startPtr and curPtr
inline StressMsg* ThreadStressLog::AdvWritePastBoundary(int cArgs) {
    STATIC_CONTRACT_WRAPPER;
#if !defined(STRESS_LOG_READONLY) && !defined(STRESS_LOG_ANALYZER)
    //zeroed out remaining buffer
    memset (curWriteChunk->StartPtr (), 0, (BYTE *)curPtr - (BYTE *)curWriteChunk->StartPtr ());

    //if we are already at head of the list, try to grow the list
    if (curWriteChunk == chunkListHead)
    {
        GrowChunkList ();
    }
#endif //!STRESS_LOG_READONLY && !STRESS_LOG_ANALYZER

    curWriteChunk = curWriteChunk->prev;
#ifndef STRESS_LOG_READONLY
    if (curWriteChunk == chunkListTail)
    {
        writeHasWrapped = TRUE;
    }
#endif //STRESS_LOG_READONLY
    return (StressMsg*)((char*)curWriteChunk->EndPtr () - sizeof(StressMsg) - cArgs * sizeof(void*));
}

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
};

#else   // STRESS_LOG

#define STRESS_LOG_VA(level,msg)                                        do { } while(0)
#define STRESS_LOG0(facility, level, msg)                               do { } while(0)
#define STRESS_LOG1(facility, level, msg, data1)                        do { } while(0)
#define STRESS_LOG2(facility, level, msg, data1, data2)                 do { } while(0)
#define STRESS_LOG2_CHECK_EE_STARTED(facility, level, msg, data1, data2)do { } while(0)
#define STRESS_LOG3(facility, level, msg, data1, data2, data3)          do { } while(0)
#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4)   do { } while(0)
#define STRESS_LOG5(facility, level, msg, data1, data2, data3, data4, data5)   do { } while(0)
#define STRESS_LOG6(facility, level, msg, data1, data2, data3, data4, data5, data6)   do { } while(0)
#define STRESS_LOG7(facility, level, msg, data1, data2, data3, data4, data5, data6, data7)   do { } while(0)
#define STRESS_LOG_PLUG_MOVE(plug_start, plug_end, plug_delta)          do { } while(0)
#define STRESS_LOG_ROOT_PROMOTE(root_addr, objPtr, methodTable)         do { } while(0)
#define STRESS_LOG_ROOT_RELOCATE(root_addr, old_value, new_value, methodTable) do { } while(0)
#define STRESS_LOG_GC_START(gcCount, Gen, collectClasses)               do { } while(0)
#define STRESS_LOG_GC_END(gcCount, Gen, collectClasses)                 do { } while(0)
#define STRESS_LOG_OOM_STACK(size)   do { } while(0)
#define STRESS_LOG_GC_STACK(size)   do { } while(0)
#define STRESS_LOG_RESERVE_MEM(numChunks) do {} while (0)
#endif // STRESS_LOG

#endif // StressLog_h
