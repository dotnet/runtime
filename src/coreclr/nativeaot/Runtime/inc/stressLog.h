// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// StressLog.h
//
// StressLog infrastructure
//
// The StressLog is a binary, memory based circular queue of logging messages.
//   It is intended to be used in retail builds during stress runs (activated
//   by registry key), to help find bugs that only turn up during stress runs.
//
// Differently from the desktop implementation the RH implementation of the
//   stress log will log all facilities, and only filter on logging level.
//
// The log has a very simple structure, and is meant to be dumped from an NTSD
//   extention (eg. strike).
//
// debug\rhsos\stresslogdump.cpp contains the dumper utility that parses this
//   log.
// ---------------------------------------------------------------------------

#ifndef StressLog_h
#define StressLog_h  1

#define SUPPRESS_WARNING_4127   \
    __pragma(warning(push))     \
    __pragma(warning(disable:4127)) /* conditional expression is constant*/

#define POP_WARNING_STATE       \
    __pragma(warning(pop))

#define WHILE_0             \
    SUPPRESS_WARNING_4127   \
    while(0)                \
    POP_WARNING_STATE       \


// let's keep STRESS_LOG defined always...
#if !defined(STRESS_LOG) && !defined(NO_STRESS_LOG)
#define STRESS_LOG
#endif

#if defined(STRESS_LOG)

//
// Logging levels and facilities
//
#define DEFINE_LOG_FACILITY(logname, value)  logname = value,

enum LogFacilitiesEnum: unsigned int {
#include "loglf.h"
    LF_ALWAYS        = 0x80000000u, // Log message irrepespective of LogFacility (if the level matches)
    LF_ALL           = 0xFFFFFFFFu, // Used only to mask bits. Never use as LOG((LF_ALL, ...))
};


#define LL_EVERYTHING  10
#define LL_INFO1000000  9       // can be expected to generate 1,000,000 logs per small but not trival run
#define LL_INFO100000   8       // can be expected to generate 100,000 logs per small but not trival run
#define LL_INFO10000    7       // can be expected to generate 10,000 logs per small but not trival run
#define LL_INFO1000     6       // can be expected to generate 1,000 logs per small but not trival run
#define LL_INFO100      5       // can be expected to generate 100 logs per small but not trival run
#define LL_INFO10       4       // can be expected to generate 10 logs per small but not trival run
#define LL_WARNING      3
#define LL_ERROR        2
#define LL_FATALERROR   1
#define LL_ALWAYS       0       // impossible to turn off (log level never negative)

//
//
//

#ifndef _ASSERTE
#define _ASSERTE(expr)
#endif


#ifndef DACCESS_COMPILE


//==========================================================================================
// The STRESS_LOG* macros
//
// The STRESS_LOG* macros work like printf.  In fact the use printf in their implementation
// so all printf format specifications work.  In addition the Stress log dumper knows
// about certain suffixes for the %p format specification (normally used to print a pointer)
//
//          %pM     // The pointer is a MethodInfo -- not supported yet (use %pK instead)
//          %pT     // The pointer is a type (MethodTable)
//          %pV     // The pointer is a C++ Vtable pointer
//          %pK     // The pointer is a code address (used for call stacks or method names)
//

// STRESS_LOG_VA was added to allow sending GC trace output to the stress log. msg must be enclosed
//   in ()'s and contain a format string followed by 0 - 4 arguments.  The arguments must be numbers or
//   string literals.  LogMsgOL is overloaded so that all of the possible sets of parameters are covered.
//   This was done becasue GC Trace uses dprintf which dosen't contain info on how many arguments are
//   getting passed in and using va_args would require parsing the format string during the GC
//

#define STRESS_LOG_VA(msg) do {                                                     \
            if (StressLog::StressLogOn(LF_GC, LL_ALWAYS))                           \
                StressLog::LogMsgOL msg;                                            \
            } WHILE_0

#define STRESS_LOG0(facility, level, msg) do {                                      \
            if (StressLog::StressLogOn(facility, level))                            \
                StressLog::LogMsg(facility, 0, msg);                                \
            } WHILE_0                                                              \

#define STRESS_LOG1(facility, level, msg, data1) do {                               \
            if (StressLog::StressLogOn(facility, level))                            \
                StressLog::LogMsg(facility, 1, msg, (void*)(size_t)(data1));        \
            } WHILE_0

#define STRESS_LOG2(facility, level, msg, data1, data2) do {                        \
            if (StressLog::StressLogOn(facility, level))                            \
                StressLog::LogMsg(facility, 2, msg,                                 \
                    (void*)(size_t)(data1), (void*)(size_t)(data2));                \
            } WHILE_0

#define STRESS_LOG3(facility, level, msg, data1, data2, data3) do {                           \
            if (StressLog::StressLogOn(facility, level))                                      \
                StressLog::LogMsg(facility, 3, msg,                                           \
                    (void*)(size_t)(data1),(void*)(size_t)(data2),(void*)(size_t)(data3));    \
            } WHILE_0

#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4) do {                    \
            if (StressLog::StressLogOn(facility, level))                                      \
                StressLog::LogMsg(facility, 4, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4));    \
            } WHILE_0

#define STRESS_LOG5(facility, level, msg, data1, data2, data3, data4, data5) do {             \
            if (StressLog::StressLogOn(facility, level))                                      \
                StressLog::LogMsg(facility, 5, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),     \
                    (void*)(size_t)(data5));                                                  \
            } WHILE_0

#define STRESS_LOG6(facility, level, msg, data1, data2, data3, data4, data5, data6) do {      \
            if (StressLog::StressLogOn(facility, level))                                      \
                StressLog::LogMsg(facility, 6, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),     \
                    (void*)(size_t)(data5), (void*)(size_t)(data6));                          \
            } WHILE_0

#define STRESS_LOG7(facility, level, msg, data1, data2, data3, data4, data5, data6, data7) do { \
            if (StressLog::StressLogOn(facility, level))                                      \
                StressLog::LogMsg(facility, 7, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),     \
                    (void*)(size_t)(data5), (void*)(size_t)(data6), (void*)(size_t)(data7));  \
            } WHILE_0

#define STRESS_LOG_COND0(facility, level, msg) do {                                 \
            if (StressLog::StressLogOn(facility, level) && (cond))                  \
                StressLog::LogMsg(facility, 0, msg);                                \
            } WHILE_0

#define STRESS_LOG_COND1(facility, level, cond, msg, data1) do {                    \
            if (StressLog::StressLogOn(facility, level) && (cond))                  \
                StressLog::LogMsg(facility, 1, msg, (void*)(size_t)(data1));        \
            } WHILE_0

#define STRESS_LOG_COND2(facility, level, cond, msg, data1, data2) do {             \
            if (StressLog::StressLogOn(facility, level) && (cond))                  \
                StressLog::LogMsg(facility, 2, msg,                                 \
                    (void*)(size_t)(data1), (void*)(size_t)(data2));                \
            } WHILE_0

#define STRESS_LOG_COND3(facility, level, cond, msg, data1, data2, data3) do {      \
            if (StressLog::StressLogOn(facility, level) && (cond))                  \
                StressLog::LogMsg(facility, 3, msg,                                 \
                    (void*)(size_t)(data1),(void*)(size_t)(data2),(void*)(size_t)(data3));    \
            } WHILE_0

#define STRESS_LOG_COND4(facility, level, cond, msg, data1, data2, data3, data4) do {         \
            if (StressLog::StressLogOn(facility, level) && (cond))                            \
                StressLog::LogMsg(facility, 4, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4));    \
            } WHILE_0

#define STRESS_LOG_COND5(facility, level, cond, msg, data1, data2, data3, data4, data5) do {  \
            if (StressLog::StressLogOn(facility, level) && (cond))                            \
                StressLog::LogMsg(facility, 5, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),     \
                    (void*)(size_t)(data5));                                                  \
            } WHILE_0

#define STRESS_LOG_COND6(facility, level, cond, msg, data1, data2, data3, data4, data5, data6) do {     \
            if (StressLog::StressLogOn(facility, level) && (cond))                            \
                StressLog::LogMsg(facility, 6, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),     \
                    (void*)(size_t)(data5), (void*)(size_t)(data6));                          \
            } WHILE_0

#define STRESS_LOG_COND7(facility, level, cond, msg, data1, data2, data3, data4, data5, data6, data7) do {  \
            if (StressLog::StressLogOn(facility, level) && (cond))                            \
                StressLog::LogMsg(facility, 7, msg, (void*)(size_t)(data1),                   \
                    (void*)(size_t)(data2),(void*)(size_t)(data3),(void*)(size_t)(data4),     \
                    (void*)(size_t)(data5), (void*)(size_t)(data6), (void*)(size_t)(data7));  \
            } WHILE_0

#define STRESS_LOG_RESERVE_MEM(numChunks) do {                                                \
            if (StressLog::StressLogOn(LF_ALL, LL_ALWAYS))                         \
                {StressLog::ReserveStressLogChunks (numChunks);}                              \
            } WHILE_0

// !!! WARNING !!!
// !!! DO NOT ADD STRESS_LOG8, as the stress log infrastructure supports a maximum of 7 arguments
// !!! WARNING !!!

#define STRESS_LOG_PLUG_MOVE(plug_start, plug_end, plug_delta) do {                           \
            if (StressLog::StressLogOn(LF_GC, LL_INFO1000))                                   \
                StressLog::LogMsg(LF_GC, 3, ThreadStressLog::gcPlugMoveMsg(),                 \
                (void*)(size_t)(plug_start), (void*)(size_t)(plug_end), (void*)(size_t)(plug_delta)); \
            } WHILE_0

#define STRESS_LOG_ROOT_PROMOTE(root_addr, objPtr, methodTable) do {                          \
            if (StressLog::StressLogOn(LF_GC|LF_GCROOTS, LL_INFO1000))                        \
                StressLog::LogMsg(LF_GC|LF_GCROOTS, 3, ThreadStressLog::gcRootPromoteMsg(),   \
                    (void*)(size_t)(root_addr), (void*)(size_t)(objPtr), (void*)(size_t)(methodTable)); \
            } WHILE_0

#define STRESS_LOG_ROOT_RELOCATE(root_addr, old_value, new_value, methodTable) do {           \
            if (StressLog::StressLogOn(LF_GC|LF_GCROOTS, LL_INFO1000) && ((size_t)(old_value) != (size_t)(new_value))) \
                StressLog::LogMsg(LF_GC|LF_GCROOTS, 4, ThreadStressLog::gcRootMsg(),          \
                    (void*)(size_t)(root_addr), (void*)(size_t)(old_value),                   \
                    (void*)(size_t)(new_value), (void*)(size_t)(methodTable));                \
            } WHILE_0

#define STRESS_LOG_GC_START(gcCount, Gen, collectClasses) do {                                \
            if (StressLog::StressLogOn(LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10))               \
                StressLog::LogMsg(LF_GCROOTS|LF_GC|LF_GCALLOC, 3, ThreadStressLog::gcStartMsg(),        \
                    (void*)(size_t)(gcCount), (void*)(size_t)(Gen), (void*)(size_t)(collectClasses));   \
            } WHILE_0

#define STRESS_LOG_GC_END(gcCount, Gen, collectClasses) do {                                  \
            if (StressLog::StressLogOn(LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10))               \
                StressLog::LogMsg(LF_GCROOTS|LF_GC|LF_GCALLOC, 3, ThreadStressLog::gcEndMsg(),\
                    (void*)(size_t)(gcCount), (void*)(size_t)(Gen), (void*)(size_t)(collectClasses), 0);\
            } WHILE_0

#if defined(_DEBUG)
#define MAX_CALL_STACK_TRACE          20
#define STRESS_LOG_OOM_STACK(size) do {                                                       \
                if (StressLog::StressLogOn(LF_ALWAYS, LL_ALWAYS))                              \
                {                                                                             \
                    StressLog::LogMsgOL("OOM on alloc of size %x \n", (void*)(size_t)(size)); \
                    StressLog::LogCallStack ("OOM");                                          \
                }                                                                             \
            } WHILE_0
#define STRESS_LOG_GC_STACK do {                                                              \
                if (StressLog::StressLogOn(LF_GC |LF_GCINFO, LL_ALWAYS))                      \
                {                                                                             \
                    StressLog::LogMsgOL("GC is triggered \n");                                \
                    StressLog::LogCallStack ("GC");                                           \
                }                                                                             \
            } WHILE_0
#else //_DEBUG
#define STRESS_LOG_OOM_STACK(size)
#define STRESS_LOG_GC_STACK
#endif //_DEBUG

#endif // DACCESS_COMPILE

//
// forward declarations:
//
class CrstStatic;
class Thread;
typedef DPTR(Thread) PTR_Thread;
class StressLog;
typedef DPTR(StressLog) PTR_StressLog;
class ThreadStressLog;
typedef DPTR(ThreadStressLog) PTR_ThreadStressLog;
struct StressLogChunk;
typedef DPTR(StressLogChunk) PTR_StressLogChunk;
struct DacpStressLogEnumCBArgs;
extern "C" void PopulateDebugHeaders();


//==========================================================================================
// StressLog - per-thread circular queue of stresslog messages
//
class StressLog {
    friend void PopulateDebugHeaders();
public:
// private:
    unsigned facilitiesToLog;               // Bitvector of facilities to log (see loglf.h)
    unsigned levelToLog;                    // log level
    unsigned MaxSizePerThread;              // maximum number of bytes each thread should have before wrapping
    unsigned MaxSizeTotal;                  // maximum memory allowed for stress log
    int32_t totalChunk;                       // current number of total chunks allocated
    PTR_ThreadStressLog logs;               // the list of logs for every thread.
    int32_t deadCount;                        // count of dead threads in the log
    CrstStatic *pLock;                      // lock
    unsigned __int64 tickFrequency;         // number of ticks per second
    unsigned __int64 startTimeStamp;        // start time from when tick counter started
    FILETIME startTime;                     // time the application started
    size_t   moduleOffset;                  // Used to compute format strings.

#ifndef DACCESS_COMPILE
public:
    static void Initialize(unsigned facilities, unsigned level, unsigned maxBytesPerThread,
                    unsigned maxBytesTotal, HANDLE hMod);
    // Called at DllMain THREAD_DETACH to recycle thread's logs
    static void ThreadDetach(ThreadStressLog *msgs);
    static long NewChunk ()     { return PalInterlockedIncrement (&theLog.totalChunk); }
    static long ChunkDeleted () { return PalInterlockedDecrement (&theLog.totalChunk); }

    //the result is not 100% accurate. If multiple threads call this funciton at the same time,
    //we could allow the total size be bigger than required. But the memory won't grow forever
    //and this is not critical so we don't try to fix the race
    static bool AllowNewChunk (long numChunksInCurThread);

    //preallocate Stress log chunks for current thread. The memory we could preallocate is still
    //bounded by per thread size limit and total size limit. If chunksToReserve is 0, we will try to
    //preallocate up to per thread size limit
    static bool ReserveStressLogChunks (unsigned int chunksToReserve);

// private:
    static ThreadStressLog* CreateThreadStressLog(Thread * pThread);
    static ThreadStressLog* CreateThreadStressLogHelper(Thread * pThread);

#else // DACCESS_COMPILE
public:
    bool Initialize();

    // Can't refer to the types in sospriv.h because it drags in windows.h
    void EnumerateStressMsgs(/*STRESSMSGCALLBACK*/ void* smcb, /*ENDTHREADLOGCALLBACK*/ void* etcb,
                                        void *token);
    void EnumStressLogMemRanges(/*STRESSLOGMEMRANGECALLBACK*/ void* slmrcb, void *token);

    // Called while dumping logs after operations are completed, to ensure DAC-caches
    // allow the stress logs to be dumped again
    void ResetForRead();

    ThreadStressLog* FindLatestThreadLog() const;

    friend class ClrDataAccess;

#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
public:
    FORCEINLINE static bool StressLogOn(unsigned /*facility*/, unsigned level)
    {
    #if defined(DACCESS_COMPILE)
        UNREFERENCED_PARAMETER(level);
        return FALSE;
    #else
        // In Redhawk we have rationalized facility codes and have much
        // fewer compared to desktop, as such we'll log all facilities and
        // limit the filtering to the log level...
        return
            // (theLog.facilitiesToLog & facility)
            //  &&
            (level <= theLog.levelToLog);
    #endif
    }

    static void LogMsg(unsigned facility, int cArgs, const char* format, ... );

    // Support functions for STRESS_LOG_VA
    // We disable the warning "conversion from 'type' to 'type' of greater size" since everything will
    // end up on the stack, and LogMsg will know the size of the variable based on the format string.
    #ifdef _MSC_VER
    #pragma warning( push )
    #pragma warning( disable : 4312 )
    #endif
    static void LogMsgOL(const char* format)
    { LogMsg(LF_GC, 0, format); }

    template < typename T1 >
    static void LogMsgOL(const char* format, T1 data1)
    {
        C_ASSERT(sizeof(T1) <= sizeof(void*));
        LogMsg(LF_GC, 1, format, (void*)(size_t)data1);
    }

    template < typename T1, typename T2 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2)
    {
        C_ASSERT(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*));
        LogMsg(LF_GC, 2, format, (void*)(size_t)data1, (void*)(size_t)data2);
    }

    template < typename T1, typename T2, typename T3 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3)
    {
        C_ASSERT(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*));
        LogMsg(LF_GC, 3, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3);
    }

    template < typename T1, typename T2, typename T3, typename T4 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4)
    {
        C_ASSERT(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*));
        LogMsg(LF_GC, 4, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5)
    {
        C_ASSERT(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*));
        LogMsg(LF_GC, 5, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6)
    {
        C_ASSERT(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*));
        LogMsg(LF_GC, 6, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6);
    }

    template < typename T1, typename T2, typename T3, typename T4, typename T5, typename T6, typename T7 >
    static void LogMsgOL(const char* format, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7)
    {
        C_ASSERT(sizeof(T1) <= sizeof(void*) && sizeof(T2) <= sizeof(void*) && sizeof(T3) <= sizeof(void*) && sizeof(T4) <= sizeof(void*) && sizeof(T5) <= sizeof(void*) && sizeof(T6) <= sizeof(void*) && sizeof(T7) <= sizeof(void*));
        LogMsg(LF_GC, 7, format, (void*)(size_t)data1, (void*)(size_t)data2, (void*)(size_t)data3, (void*)(size_t)data4, (void*)(size_t)data5, (void*)(size_t)data6, (void*)(size_t)data7);
    }

    #ifdef _MSC_VER
    #pragma warning( pop )
    #endif

// We can only log the stacktrace on DEBUG builds!
#ifdef _DEBUG
    static void LogCallStack(const char *const callTag);
#endif //_DEBUG

#endif // DACCESS_COMPILE

// private: // static variables
    static StressLog theLog;    // We only have one log, and this is it
};


//==========================================================================================
// Private classes
//

#if defined(_MSC_VER)
// don't warn about 0 sized array below or unnamed structures
#pragma warning(disable:4200 4201)
#endif

//==========================================================================================
// StressMsg
//
// The order of fields is important.  Keep the prefix length as the first field.
// And make sure the timeStamp field is naturally aligned, so we don't waste
// space on 32-bit platforms
//
struct StressMsg {
    union {
        struct {
            uint32_t numberOfArgs  : 3;   // at most 7 arguments
            uint32_t formatOffset  : 29;  // offset of string in mscorwks
        };
        uint32_t fmtOffsCArgs;            // for optimized access
    };
    uint32_t     facility;                // facility used to log the entry
    unsigned __int64 timeStamp;         // time when mssg was logged
    void*     args[0];                  // size given by numberOfArgs

    static const size_t maxArgCnt = 7;
    static const size_t maxOffset = 0x20000000;
    static size_t maxMsgSize ()
    { return sizeof(StressMsg) + maxArgCnt*sizeof(void*); }

    friend void PopulateDebugHeaders();
    friend class ThreadStressLog;
    friend class StressLog;
};

#ifdef _WIN64
#define STRESSLOG_CHUNK_SIZE (32 * 1024)
#else //_WIN64
#define STRESSLOG_CHUNK_SIZE (16 * 1024)
#endif //_WIN64
#define GC_STRESSLOG_MULTIPLY (5)

//==========================================================================================
// StressLogChunk
//
//  A chunk of contiguous memory containing instances of StressMsg
//
struct StressLogChunk
{
    PTR_StressLogChunk prev;
    PTR_StressLogChunk next;
    char buf[STRESSLOG_CHUNK_SIZE];
    uint32_t dwSig1;
    uint32_t dwSig2;

#ifndef DACCESS_COMPILE

    StressLogChunk (PTR_StressLogChunk p = NULL, PTR_StressLogChunk n = NULL)
        :prev (p), next (n), dwSig1 (0xCFCFCFCF), dwSig2 (0xCFCFCFCF)
    {}

#endif //!DACCESS_COMPILE

    char * StartPtr ()
    {
        return buf;
    }

    char * EndPtr ()
    {
        return buf + STRESSLOG_CHUNK_SIZE;
    }

    bool IsValid () const
    {
        return dwSig1 == 0xCFCFCFCF && dwSig2 == 0xCFCFCFCF;
    }
};

//==========================================================================================
// ThreadStressLog
//
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
    PTR_ThreadStressLog next;   // we keep a linked list of these
    uint64_t   threadId;        // the id for the thread using this buffer
    bool       isDead;          // Is this thread dead
    bool       readHasWrapped;      // set when read ptr has passed chunListTail
    bool       writeHasWrapped;     // set when write ptr has passed chunListHead
    StressMsg* curPtr;          // where packets are being put on the queue
    StressMsg* readPtr;         // where we are reading off the queue (used during dumping)
    PTR_StressLogChunk chunkListHead; //head of a list of stress log chunks
    PTR_StressLogChunk chunkListTail; //tail of a list of stress log chunks
    PTR_StressLogChunk curReadChunk;  //the stress log chunk we are currently reading
    PTR_StressLogChunk curWriteChunk; //the stress log chunk we are currently writing
    long chunkListLength;       // how many stress log chunks are in this stress log
    PTR_Thread pThread;         // thread associated with these stress logs
    StressMsg * origCurPtr;     // this holds the original curPtr before we start the dump

    friend void PopulateDebugHeaders();
    friend class StressLog;

#ifndef DACCESS_COMPILE
public:
    inline ThreadStressLog ();
    inline ~ThreadStressLog ();

    void LogMsg ( uint32_t facility, int cArgs, const char* format, ... )
    {
        va_list Args;
        va_start(Args, format);
        LogMsg (facility, cArgs, format, Args);
    }

    void LogMsg ( uint32_t facility, int cArgs, const char* format, va_list Args);

private:
    FORCEINLINE StressMsg* AdvanceWrite(int cArgs);
    inline StressMsg* AdvWritePastBoundary(int cArgs);
    FORCEINLINE bool GrowChunkList ();

#else // DACCESS_COMPILE
public:
    friend class ClrDataAccess;

    // Called while dumping.  Returns true after all messages in log were dumped
    FORCEINLINE bool CompletedDump ();

private:
    FORCEINLINE bool IsReadyForRead()       { return readPtr != NULL; }
    FORCEINLINE StressMsg* AdvanceRead();
    inline StressMsg* AdvReadPastBoundary();
#endif //!DACCESS_COMPILE

public:
    void Activate (Thread * pThread);

    bool IsValid () const
    {
        return chunkListHead != NULL && (!curWriteChunk || curWriteChunk->IsValid ());
    }

    static const char* gcStartMsg()
    {
        return "{ =========== BEGINGC %d, (requested generation = %lu, collect_classes = %lu) ==========\n";
    }

    static const char* gcEndMsg()
    {
        return "========== ENDGC %d (gen = %lu, collect_classes = %lu) ===========}\n";
    }

    static const char* gcRootMsg()
    {
        return "    GC Root %p RELOCATED %p -> %p  MT = %pT\n";
    }

    static const char* gcRootPromoteMsg()
    {
        return "    GCHeap::Promote: Promote GC Root *%p = %p MT = %pT\n";
    }

    static const char* gcPlugMoveMsg()
    {
        return "GC_HEAP RELOCATING Objects in heap within range [%p %p) by -0x%x bytes\n";
    }

};


//==========================================================================================
// Inline implementations:
//

#ifdef DACCESS_COMPILE

//------------------------------------------------------------------------------------------
// Called while dumping.  Returns true after all messages in log were dumped
FORCEINLINE bool ThreadStressLog::CompletedDump ()
{
    return readPtr->timeStamp == 0
            //if read has passed end of list but write has not passed head of list yet, we are done
            //if write has also wrapped, we are at the end if read pointer passed write pointer
            || (readHasWrapped &&
                    (!writeHasWrapped || (curReadChunk == curWriteChunk && readPtr >= curPtr)));
}

//------------------------------------------------------------------------------------------
// Called when dumping the log (by StressLog::Dump())
// Updates readPtr to point to next stress messaage to be dumped
inline StressMsg* ThreadStressLog::AdvanceRead() {
    // advance the marker
    readPtr = (StressMsg*)((char*)readPtr + sizeof(StressMsg) + readPtr->numberOfArgs*sizeof(void*));
    // wrap around if we need to
    if (readPtr >= (StressMsg *)curReadChunk->EndPtr ())
    {
        AdvReadPastBoundary();
    }
    return readPtr;
}

//------------------------------------------------------------------------------------------
// The factored-out slow codepath for AdvanceRead(), only called by AdvanceRead().
// Updates readPtr to and returns the first stress message >= startPtr
inline StressMsg* ThreadStressLog::AdvReadPastBoundary() {
    //if we pass boundary of tail list, we need to set has Wrapped
    if (curReadChunk == chunkListTail)
    {
        readHasWrapped = true;
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

#else // DACCESS_COMPILE

//------------------------------------------------------------------------------------------
// Initialize a ThreadStressLog
inline ThreadStressLog::ThreadStressLog()
{
    chunkListHead = chunkListTail = curWriteChunk = NULL;
    StressLogChunk * newChunk = new (nothrow) StressLogChunk;
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
    isDead = TRUE;
    curPtr = NULL;
    readPtr = NULL;
    writeHasWrapped = FALSE;
    curReadChunk = NULL;
    curWriteChunk = NULL;
    chunkListLength = 1;
    origCurPtr = NULL;
}

inline ThreadStressLog::~ThreadStressLog ()
{
    //no thing to do if the list is empty (failed to initialize)
    if (chunkListHead == NULL)
    {
        return;
    }

    StressLogChunk * chunk = chunkListHead;

    do
    {
        StressLogChunk * tmp = chunk;
        chunk = chunk->next;
        delete tmp;
        StressLog::ChunkDeleted ();
    } while (chunk != chunkListHead);
}

//------------------------------------------------------------------------------------------
// Called when logging, checks if we can increase the number of stress log chunks associated
// with the current thread
FORCEINLINE bool ThreadStressLog::GrowChunkList ()
{
    _ASSERTE (chunkListLength >= 1);
    if (!StressLog::AllowNewChunk (chunkListLength))
    {
        return FALSE;
    }
    StressLogChunk * newChunk = new (nothrow) StressLogChunk (chunkListTail, chunkListHead);
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

//------------------------------------------------------------------------------------------
// Called at runtime when writing the log (by StressLog::LogMsg())
// Updates curPtr to point to the next spot in the log where we can write
// a stress message with cArgs arguments
// For convenience it returns a pointer to the empty slot where we can
// write the next stress message.
// cArgs is the number of arguments in the message to be written.
inline StressMsg* ThreadStressLog::AdvanceWrite(int cArgs) {
    // _ASSERTE(cArgs <= StressMsg::maxArgCnt);
    // advance the marker
    StressMsg* p = (StressMsg*)((char*)curPtr - sizeof(StressMsg) - cArgs*sizeof(void*));

    //past start of current chunk
    //wrap around if we need to
    if (p < (StressMsg*)curWriteChunk->StartPtr ())
    {
       curPtr = AdvWritePastBoundary(cArgs);
    }
    else
    {
        curPtr = p;
    }

    return curPtr;
}

//------------------------------------------------------------------------------------------
// This is the factored-out slow codepath for AdvanceWrite() and is only called by
// AdvanceWrite().
// Returns the stress message flushed against endPtr
// In addition it writes NULLs b/w the startPtr and curPtr
inline StressMsg* ThreadStressLog::AdvWritePastBoundary(int cArgs) {
    //zeroed out remaining buffer
    memset (curWriteChunk->StartPtr (), 0, (char *)curPtr - (char *)curWriteChunk->StartPtr ());

    //if we are already at head of the list, try to grow the list
    if (curWriteChunk == chunkListHead)
    {
        GrowChunkList ();
    }

    curWriteChunk = curWriteChunk->prev;
    if (curWriteChunk == chunkListTail)
    {
        writeHasWrapped = TRUE;
    }
    curPtr = (StressMsg*)((char*)curWriteChunk->EndPtr () - sizeof(StressMsg) - cArgs * sizeof(void*));
    return curPtr;
}

#endif // DACCESS_COMPILE

#endif // STRESS_LOG

#ifndef __GCENV_BASE_INCLUDED__
#if !defined(STRESS_LOG) || defined(DACCESS_COMPILE)
#define STRESS_LOG_VA(msg)                                              do { } WHILE_0
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
#define STRESS_LOG_OOM_STACK(size)          do { } WHILE_0
#define STRESS_LOG_GC_STACK                 do { } WHILE_0
#define STRESS_LOG_RESERVE_MEM(numChunks)   do { } WHILE_0
#endif // !STRESS_LOG || DACCESS_COMPILE
#endif // !__GCENV_BASE_INCLUDED__

#endif // StressLog_h
