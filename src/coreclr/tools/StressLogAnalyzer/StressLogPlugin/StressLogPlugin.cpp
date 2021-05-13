// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pch.h"

#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>
#include <math.h>

#include "strike.h"
#include "util.h"

#include "assert.h"

#define STRESS_LOG
#define STRESS_LOG_ANALYZER
#define MEMORY_MAPPED_STRESSLOG

class MapViewHolder
{
    void* whatever;
};

typedef unsigned char uint8_t;
typedef unsigned int uint32_t;
typedef long long int64_t;
typedef size_t uint64_t;

bool IsInCantAllocStressLogRegion()
{
    return true;
}

#include "../../../inc/stresslog.h"

BOOL g_bDacBroken;
WCHAR g_mdName[1];
SYMBOLS* g_ExtSymbols;
SOS* g_sos;

HRESULT OutputVaList(ULONG mask, PCSTR format, va_list args)
{
    return vprintf(format, args);
}

void ExtOut(PCSTR format, ...)
{
    va_list args;
    va_start(args, format);
    vprintf(format, args);
}

void GcHistClear()
{
}

void GcHistAddLog(LPCSTR msg, StressMsg* stressMsg)
{
}

// this is just to read string literals out of the coreclr and clrgc images
struct CorClrData : IDebugDataSpaces
{
    StressLog::StressLogHeader* hdr;
    CorClrData(StressLog::StressLogHeader* h) : hdr(h) { }

    virtual HRESULT ReadVirtual(void* src, void* dest, size_t size, int)
    {
        size_t cumSize = 0;
        for (int moduleIndex = 0; moduleIndex < StressLog::MAX_MODULES; moduleIndex++)
        {
            ptrdiff_t offs = (uint8_t*)src - hdr->modules[moduleIndex].baseAddress;
            if ((size_t)offs < hdr->modules[moduleIndex].size && (size_t)offs + size < hdr->modules[moduleIndex].size)
            {
                memcpy(dest, &hdr->moduleImage[offs + cumSize], size);
                return S_OK;
            }
            cumSize += hdr->modules[moduleIndex].size;
        }
        return E_FAIL;
    }
};

const int MAX_NUMBER_OF_HEAPS = 1024;
static volatile int64_t s_maxHeapNumberSeen = -1;
static volatile uint64_t s_threadIdOfHeap[MAX_NUMBER_OF_HEAPS][2];

enum GcThreadKind
{
    GC_THREAD_FG,
    GC_THREAD_BG,
};

struct GcThread
{
    GcThreadKind    kind;
    int             heapNumber;
};

bool LookupGcThread(uint64_t threadId, GcThread *gcThread)
{
    for (int i = 0; i <= s_maxHeapNumberSeen; i++)
    {
        if (s_threadIdOfHeap[i][GC_THREAD_FG] == threadId)
        {
            gcThread->heapNumber = i;
            gcThread->kind = GC_THREAD_FG;
            return true;
        }
        if (s_threadIdOfHeap[i][GC_THREAD_BG] == threadId)
        {
            gcThread->heapNumber = i;
            gcThread->kind = GC_THREAD_BG;
            return true;
        }
    }
    return false;
}

#define InterestingStrings \
d(IS_UNKNOWN,                   "")                                                                                         \
d(IS_THREAD_WAIT,               ThreadStressLog::gcServerThread0StartMsg())                                                 \
d(IS_THREAD_WAIT_DONE,          ThreadStressLog::gcServerThreadNStartMsg())                                                 \
d(IS_GCSTART,                   ThreadStressLog::gcDetailedStartMsg())                                                      \
d(IS_GCEND,                     ThreadStressLog::gcDetailedEndMsg())                                                        \
d(IS_MARK_START,                ThreadStressLog::gcStartMarkMsg())                                                          \
d(IS_PLAN_START,                ThreadStressLog::gcStartPlanMsg())                                                          \
d(IS_RELOCATE_START,            ThreadStressLog::gcStartRelocateMsg())                                                      \
d(IS_RELOCATE_END,              ThreadStressLog::gcEndRelocateMsg())                                                        \
d(IS_COMPACT_START,             ThreadStressLog::gcStartCompactMsg())                                                       \
d(IS_COMPACT_END,               ThreadStressLog::gcEndCompactMsg())                                                         \
d(IS_GCROOT,                    ThreadStressLog::gcRootMsg())                                                               \
d(IS_PLUG_MOVE,                 ThreadStressLog::gcPlugMoveMsg())                                                           \
d(IS_GCMEMCOPY,                 ThreadStressLog::gcMemCopyMsg())                                                            \
d(IS_GCROOT_PROMOTE,            ThreadStressLog::gcRootPromoteMsg())                                                        \
d(IS_PLAN_PLUG,                 ThreadStressLog::gcPlanPlugMsg())                                                           \
d(IS_PLAN_PINNED_PLUG,          ThreadStressLog::gcPlanPinnedPlugMsg())                                                     \
d(IS_DESIRED_NEW_ALLOCATION,    ThreadStressLog::gcDesiredNewAllocationMsg())                                               \
d(IS_MAKE_UNUSED_ARRAY,         ThreadStressLog::gcMakeUnusedArrayMsg())                                                    \
d(IS_START_BGC_THREAD,          ThreadStressLog::gcStartBgcThread())                                                        \
d(IS_UNINTERESTING,             "")

enum InterestingStringId : unsigned char
{
#define d(a,b)  a,
    InterestingStrings
    IS_INTERESTING
#undef d
};

const int MAX_INTERESTING_STRINGS = 1024;
int s_interestingStringCount = IS_INTERESTING;
const char* s_interestingStringTable[MAX_INTERESTING_STRINGS] =
{
#define d(a,b)  b,
    InterestingStrings
#undef d
};

bool s_interestingStringFilter[MAX_INTERESTING_STRINGS];

static void AddInterestingString(const char* s)
{
    for (int i = 0; i < s_interestingStringCount; i++)
    {
        if (strcmp(s_interestingStringTable[i], s) == 0)
        {
            s_interestingStringFilter[i] = true;
            return;
        }
    }
    int i = s_interestingStringCount++;
    s_interestingStringTable[i] = s;
    s_interestingStringFilter[IS_INTERESTING] = true;
}


InterestingStringId mapImageToStringId[sizeof(StressLog::StressLogHeader::moduleImage)];

InterestingStringId FindStringId(StressLog::StressLogHeader* hdr, char* format)
{
    size_t offset = format - (char*)hdr->moduleImage;
    assert(offset < sizeof(mapImageToStringId));
    InterestingStringId id = mapImageToStringId[offset];
    if (id != IS_UNKNOWN)
        return id;
    for (int i = 1; s_interestingStringTable[i] != nullptr; i++)
    {
        if (strcmp(format, s_interestingStringTable[i]) == 0)
        {
            id = (InterestingStringId)i;
            if (id > IS_INTERESTING)
                id = IS_INTERESTING;
            mapImageToStringId[offset] = id;
            return id;
        }
    }
    mapImageToStringId[offset] = IS_UNINTERESTING;
    return IS_UNINTERESTING;
}

const int MAX_LEVEL_FILTERS = 100;
static int s_levelFilterCount;
struct LevelFilter
{
    int minLevel;
    int maxLevel;
};

static LevelFilter s_levelFilter[MAX_LEVEL_FILTERS];

struct GcStartEnd
{
    double startTime;
    double endTime;
};

const int MAX_GC_INDEX = 1024 * 1024;
static GcStartEnd s_gcStartEnd[MAX_GC_INDEX];

static int s_gcFilterStart;
static int s_gcFilterEnd;

const int MAX_VALUE_FILTERS = 100;
static int s_valueFilterCount;

struct ValueFilter
{
    ULONGLONG start;
    ULONGLONG end;
};

static ValueFilter s_valueFilter[MAX_VALUE_FILTERS];

const int MAX_THREAD_FILTERS = 1024;
static int s_threadFilterCount;
static uint64_t s_threadFilter[MAX_THREAD_FILTERS];

static bool s_gcThreadFilter[MAX_NUMBER_OF_HEAPS][2];
static bool s_hadGcThreadFilters;

static bool s_printHexTidForGcThreads;

static uint32_t s_facilityIgnore;

static bool s_printEarliestMessages;
static int s_printEarliestMessageFromThreadCount;
static uint64_t s_printEarliestMessageFromThread[MAX_THREAD_FILTERS];
static bool s_printEarliestMessageFromGcThread[MAX_NUMBER_OF_HEAPS][2];

static bool FilterThread(ThreadStressLog* tsl)
{
    //    return tsl->threadId == 0x6ff8;

    if (s_gcFilterStart != 0)
    {
        // we have a filter based on a GC index
        // include all message for now so we don't miss any
        // GC start/end messages
        // we will throw away message for other threads later
        return true;
    }

    if (s_hadGcThreadFilters)
    {
        GcThread gcThread;
        if (!LookupGcThread(tsl->threadId, &gcThread))
        {
            // this may or may not be a GC thread - we don't know yet
            // include its messages to be conservative - we will have
            // a filter later to remove these messages
            return true;
        }
        return s_gcThreadFilter[gcThread.heapNumber][gcThread.kind];
    }
    else
    {
        if (s_threadFilterCount == 0)
            return true;
        // we can filter now
        for (int i = 0; i < s_threadFilterCount; i++)
        {
            if (s_threadFilter[i] == tsl->threadId)
                return true;
        }
        return false;
    }
}


int GcLogLevel(uint32_t facility)
{
    if ((facility & (LF_ALWAYS | 0xfffe | LF_GC)) == (LF_ALWAYS | LF_GC))
    {
        return (facility >> 16) & 0x7fff;
    }
    return 0;
}

static void RememberThreadForHeap(uint64_t threadId, int64_t heapNumber, GcThreadKind threadKind)
{
    if (s_maxHeapNumberSeen == -1 && heapNumber == 0)
    {
        // we don't want to remember these associations for WKS GC,
        // which can execute on any thread - as soon as we see
        // a heap number != 0, we assume SVR GC and remember it
        return;
    }

    if (heapNumber < MAX_NUMBER_OF_HEAPS)
    {
        s_threadIdOfHeap[heapNumber][threadKind] = threadId;
        int64_t maxHeapNumberSeen = s_maxHeapNumberSeen;
        while (maxHeapNumberSeen < heapNumber)
        {
            maxHeapNumberSeen = InterlockedCompareExchange64((volatile LONG64*)&s_maxHeapNumberSeen, heapNumber, maxHeapNumberSeen);
        }
    }
}

bool FilterMessage(StressLog::StressLogHeader* hdr, ThreadStressLog* tsl, uint32_t facility, char* format, double deltaTime, int argCount, void** args)
{
    bool fLevelFilter = false;
    if (s_levelFilterCount > 0)
    {
        int gcLogLevel = GcLogLevel(facility);
        for (int i = 0; i < s_levelFilterCount; i++)
        {
            if (s_levelFilter[i].minLevel <= gcLogLevel && gcLogLevel <= s_levelFilter[i].maxLevel)
            {
                fLevelFilter = true;
                break;
            }
        }
    }

    if (s_facilityIgnore != 0)
    {
        if ((facility & (LF_ALWAYS | 0xfffe | LF_GC)) == (LF_ALWAYS | LF_GC))
        {
            // specially encoded GC message including dprintf level
            if ((s_facilityIgnore & LF_GC) != 0)
            {
                return false;
            }
        }
        else if ((s_facilityIgnore & facility) != 0)
        {
            return false;
        }
    }

    InterestingStringId isd = FindStringId(hdr, format);
    switch (isd)
    {
    case    IS_THREAD_WAIT:
    case    IS_THREAD_WAIT_DONE:
        RememberThreadForHeap(tsl->threadId, (int64_t)args[0], GC_THREAD_FG);
        break;

    case    IS_DESIRED_NEW_ALLOCATION:
    {
        int genNumber = (int)(int64_t)args[1];
        if (genNumber <= 1)
        {
            // do this only for gen 0 and 1, because otherwise it
            // may be background GC
            RememberThreadForHeap(tsl->threadId, (int64_t)args[0], GC_THREAD_FG);
        }
        break;
    }

    case    IS_GCSTART:
    {
        int gcIndex = (int)(size_t)args[0];
        if (gcIndex < MAX_GC_INDEX)
        {
            s_gcStartEnd[gcIndex].startTime = deltaTime;
        }
        return true;
    }

    case    IS_GCEND:
    {
        int gcIndex = (int)(size_t)args[0];
        if (gcIndex < MAX_GC_INDEX)
        {
            s_gcStartEnd[gcIndex].endTime = deltaTime;
        }
        return true;
    }

    case    IS_MARK_START:
    case    IS_PLAN_START:
    case    IS_RELOCATE_START:
    case    IS_RELOCATE_END:
    case    IS_COMPACT_START:
    case    IS_COMPACT_END:
        RememberThreadForHeap(tsl->threadId, (int64_t)args[0], GC_THREAD_FG);
        return true;

    case    IS_PLAN_PLUG:
    case    IS_PLAN_PINNED_PLUG:
        if (s_valueFilterCount > 0)
        {
            // print this message if the plug or the gap before it contain (part of) the range we're looking for
            size_t gapSize = (size_t)args[0];
            size_t plugStart = (size_t)args[1];
            size_t gapStart = plugStart - gapSize;
            size_t plugEnd = (size_t)args[2];
            for (int i = 0; i < s_valueFilterCount; i++)
            {
                if (s_valueFilter[i].end < gapStart || plugEnd < s_valueFilter[i].start)
                {
                    // empty intersection with the gap+plug
                    continue;
                }
                return true;
            }
        }
        break;

    case    IS_GCMEMCOPY:
        if (s_valueFilterCount > 0)
        {
            // print this message if the source or destination range contain (part of) the range we're looking for
            size_t srcStart = (size_t)args[0];
            size_t dstStart = (size_t)args[1];
            size_t srcEnd = (size_t)args[2];
            size_t dstEnd = (size_t)args[3];
            for (int i = 0; i < s_valueFilterCount; i++)
            {
                if ((s_valueFilter[i].end < srcStart || srcEnd < s_valueFilter[i].start) &&
                    (s_valueFilter[i].end < dstStart || dstEnd < s_valueFilter[i].start))
                {
                    // empty intersection with both the source and the destination
                    continue;
                }
                return true;
            }
        }
        break;

    case    IS_MAKE_UNUSED_ARRAY:
        if (s_valueFilterCount > 0)
        {
            // print this message if the source or destination range contain (part of) the range we're looking for
            size_t start = (size_t)args[0];
            size_t end = (size_t)args[1];
            for (int i = 0; i < s_valueFilterCount; i++)
            {
                if ((s_valueFilter[i].end < start || end < s_valueFilter[i].start))
                {
                    // empty intersection with the unused array
                    continue;
                }
                return true;
            }
        }
        break;

    case    IS_GCROOT:
    case    IS_PLUG_MOVE:
    case    IS_GCROOT_PROMOTE:
    case    IS_INTERESTING:
        break;

    case    IS_START_BGC_THREAD:
        RememberThreadForHeap(tsl->threadId, (int64_t)args[0], GC_THREAD_BG);
        break;
    }
    return fLevelFilter || s_interestingStringFilter[isd];
}

struct StressThreadAndMsg
{
    uint64_t    threadId;
    StressMsg* msg;
    uint64_t    msgId;
};

int CmpMsg(const void* p1, const void* p2)
{
    const StressThreadAndMsg* msg1 = (const StressThreadAndMsg*)p1;
    const StressThreadAndMsg* msg2 = (const StressThreadAndMsg*)p2;

    if (msg1->msg->timeStamp < msg2->msg->timeStamp)
        return 1;
    if (msg1->msg->timeStamp > msg2->msg->timeStamp)
        return -11;

    if (msg1->threadId < msg2->threadId)
        return -1;
    if (msg1->threadId > msg2->threadId)
        return 1;

    if (msg1->msgId < msg2->msgId)
        return -1;
    if (msg1->msgId > msg2->msgId)
        return 1;

    assert(!"unreachable");
    return 0;
}

struct ThreadStressLogDesc
{
    volatile unsigned workStarted;
    volatile unsigned workFinished;
    ThreadStressLog* tsl;
    StressMsg* earliestMessage;

    ThreadStressLogDesc() : workStarted(0), workFinished(0), tsl(nullptr), earliestMessage(nullptr)
    {
    }
};

static const int MAX_THREADSTRESSLOGS = 64 * 1024;
static ThreadStressLogDesc s_threadStressLogDesc[MAX_THREADSTRESSLOGS];
static int s_threadStressLogCount;
static LONG s_wrappedWriteThreadCount;

static const LONG MAX_MESSAGE_COUNT = 1024 * 1024 * 1024;
static StressThreadAndMsg* s_threadMsgBuf;
static volatile LONG s_msgCount = 0;
static volatile LONG s_totalMsgCount = 0;
static double s_timeFilterStart = 0;
static double s_timeFilterEnd = 0;
static wchar_t* s_outputFileName = nullptr;

static StressLog::StressLogHeader* s_hdr;

static bool s_fPrintFormatStrings;

void Usage()
{
    printf("\n");
    printf("Usage:\n");
    printf("\n");
    printf(" -o:<outputfile.txt>: write output to a text file instead of the console\n");
    printf("\n");
    printf(" -v:<hexvalue>: look for a specific hex value (often used to look for addresses)\n");
    printf(" -v:<hexlower>-<hexupper>: look for values >= hexlower and <= hexupper\n");
    printf(" -v:<hexlower>+<hexsize>: look for values >= hexlower and <= hexlower+hexsize\n");
    printf("\n");
    printf(" -t:<start time>: don't consider messages before start time\n");
    printf(" -t:<start time>-<end time>: only consider messages >= start time and <= end time\n");
    printf(" -t:-<last seconds>: only consider messages in the last seconds\n");
    printf("\n");
    printf(" -l:<level1>,<level2>,... : print messages at dprint level1,level2,...\n");
    printf("\n");
    printf(" -g:<gc_index>: only print messages occuring during GC#gc_index\n");
    printf(" -g:<gc_index1>-<gc_index_2>: as above, for a range of GC indices\n");
    printf("\n");
    printf(" -f: print the raw format strings along with the message\n");
    printf("     (useful to search for the format string in the source code)\n");
    printf(" -f:<format string>: search for a specific format string\n");
    printf("    e.g. '-f:\"<%%Ix>:%%Ix\"'\n");
    printf("\n");
    printf(" -i:<hex facility code>: ignore messages from log facilities\n");
    printf("   e.g. '-i:7ffe' means ignore messages from anything but LF_GC\n");
    printf("\n");
    printf(" -tid: print hex thread ids, e.g. 2a08 instead of GC12\n");
    printf(" -tid:<thread id1>,<thread id2>,...: only print messages from the listed\n");
    printf("     threads. Thread ids are in hex, given as GC<decimal heap number>,\n");
    printf("     or BG<decimal heap number>\n");
    printf("     e.g. '-tid:2bc8,GC3,BG14' would print messages from thread 2bc8, the gc thread\n");
    printf("     associated with heap 3, and the background GC thread for heap 14\n");
    printf("\n");
    printf(" -e: printf earliest messages from all threads\n");
    printf(" -e:<thread id1>,<thread id2>,...: print earliest messages from the listed\n");
    printf("     threads. Thread ids are in hex, given as GC<decimal heap number>,\n");
    printf("     or BG<decimal heap number>\n");
    printf("     e.g. '-e:2bc8,GC3,BG14' would print the earliest messages from thread 2bc8,\n");
    printf("     the gc thread associated with heap 3, and the background GC thread for heap 14\n");
    printf("\n");
}

// Translate escape sequences like "\n" - only common ones are handled
static void InterpretEscapeSequences(char* s)
{
    char* d = s;
    char c = *s++;
    while (c != '\0')
    {
        if (c == '\\')
        {
            c = *s++;
            switch (c)
            {
            case    'n': *d++ = '\n'; break;
            case    't': *d++ = '\t'; break;
            case    'r': *d++ = '\r'; break;
            default:     *d++ = c;    break;
            }
        }
        else
        {
            *d++ = c;
        }
        c = *s++;
    }
    *d = '\0';
}

bool ParseOptions(int argc, wchar_t* argv[])
{
    int i = 0;
    while (i < argc)
    {
        wchar_t* arg = argv[i];
        if (arg[0] == '-')
        {
            switch (arg[1])
            {
            case 'v':
            case 'V':
                if (s_valueFilterCount >= MAX_VALUE_FILTERS)
                {
                    printf("too many value filters - max is %d\n", MAX_VALUE_FILTERS);
                    return false;
                }
                if (arg[2] == ':')
                {
                    int i = s_valueFilterCount++;
                    wchar_t* end = nullptr;
                    s_valueFilter[i].start = _wcstoui64(&arg[3], &end, 16);
                    if (*end == '-')
                    {
                        s_valueFilter[i].end = _wcstoui64(end + 1, &end, 16);
                    }
                    else if (*end == '+')
                    {
                        s_valueFilter[i].end = s_valueFilter[i].start + _wcstoui64(end + 1, &end, 16);
                    }
                    else if (*end != '\0')
                    {
                        printf("expected '-'<upper limit> or '+'<size of range>\n");
                        return false;
                    }
                    else
                    {
                        s_valueFilter[i].end = s_valueFilter[i].start;
                    }
                    if (*end != '\0')
                    {
                        printf("could not parse option %S\n", arg);
                        return false;
                    }
                }
                else
                {
                    printf("expected '-v:<hex value>'\n");
                    return false;
                }
                break;

            case 't':
            case 'T':
                if (arg[2] == ':')
                {
                    wchar_t* end = nullptr;
                    s_timeFilterStart = wcstod(&arg[3], &end);
                    if (*end == '-')
                    {
                        s_timeFilterEnd = wcstod(end + 1, &end);
                    }
                    else if (*end == '+')
                    {
                        s_timeFilterEnd = s_timeFilterStart + wcstod(end + 1, &end);
                    }
                    else
                    {
                        s_timeFilterEnd = INFINITY;
                    }
                    if (*end != '\0')
                    {
                        printf("could not parse option %S\n", arg);
                        return false;
                    }
                }
                else if (_wcsnicmp(arg, L"-tid:", 5) == 0)
                {
                    arg = arg + 5;
                    while (true)
                    {
                        if (s_threadFilterCount >= MAX_THREAD_FILTERS)
                        {
                            printf("too many thread filters - max is %d\n", MAX_THREAD_FILTERS);
                            return false;
                        }
                        wchar_t* end = nullptr;
                        if (_wcsnicmp(arg, L"gc", 2) == 0 || _wcsnicmp(arg, L"bg", 2) == 0)
                        {
                            int gcHeapNumber = wcstol(arg+2, &end, 10);
                            GcThreadKind kind = _wcsnicmp(arg, L"gc", 2) == 0 ? GC_THREAD_FG : GC_THREAD_BG;
                            if (gcHeapNumber < MAX_NUMBER_OF_HEAPS)
                            {
                                s_gcThreadFilter[gcHeapNumber][kind] = true;
                                s_hadGcThreadFilters = true;
                            }
                            else
                            {
                                printf("expected heap number < %d\n", MAX_NUMBER_OF_HEAPS);
                                return false;
                            }
                        }
                        else
                        {
                            int i = s_threadFilterCount++;
                            s_threadFilter[i] = _wcstoui64(arg, &end, 16);
                        }
                        if (*end == ',')
                        {
                            arg = end + 1;
                        }
                        else if (*end != '\0')
                        {
                            printf("could not parse %S\n", arg);
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else if (_wcsicmp(arg, L"-tid") == 0)
                {
                    s_printHexTidForGcThreads = true;
                }
                else
                {
                    printf("-t:<start> or -t:<-last seconds> or -t:<start>-<end> or\n");
                    printf("-tid:<hex thread id1>,<hex thread id2>,... expected\n");
                    return false;
                }
                break;

            case 'o':
            case 'O':
                if (arg[2] == ':')
                {
                    s_outputFileName = &arg[3];
                }
                else
                {
                    printf("expected '-o:<outputfile>'\n");
                    return false;
                }
                break;

            case 'l':
            case 'L':
                if (arg[2] == ':')
                {
                    arg = arg + 3;
                    while (true)
                    {
                        if (s_levelFilterCount >= MAX_LEVEL_FILTERS)
                        {
                            printf("too many level filters - max is %d\n", MAX_LEVEL_FILTERS);
                            return false;
                        }
                        int i = s_levelFilterCount++;
                        wchar_t* end = nullptr;
                        if (*arg == '*')
                        {
                            s_levelFilter[i].minLevel = 0;
                            s_levelFilter[i].maxLevel = 0x7fffffff;
                            end = arg + 1;
                        }
                        else
                        {
                            s_levelFilter[i].minLevel = wcstol(arg, &end, 10);
                            if (*end == '-')
                            {
                                s_levelFilter[i].maxLevel = wcstol(end + 1, &end, 10);
                            }
                            else
                            {
                                s_levelFilter[i].maxLevel = s_levelFilter[i].minLevel;
                            }
                        }
                        if (*end == ',')
                        {
                            arg = end + 1;
                        }
                        else if (*end != '\0')
                        {
                            printf("could not parse option %S\n", arg);
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    printf("expected '-l:<level>'\n");
                    return false;
                }
                break;

            case 'f':
            case 'F':
                if (arg[2] == '\0')
                {
                    s_fPrintFormatStrings = true;
                }
                else if (arg[2] == ':')
                {
                    if (s_interestingStringCount >= MAX_INTERESTING_STRINGS)
                    {
                        printf("too format string filters - max is %d\n", MAX_INTERESTING_STRINGS - IS_INTERESTING);
                        return false;
                    }
                    arg = &arg[3];
                    size_t requiredSize = 0;
                    if (wcstombs_s(&requiredSize, nullptr, 0, arg, 0) != 0)
                        return false;
                    char* buf = new char[requiredSize];
                    size_t actualSize = 0;
                    if (wcstombs_s(&actualSize, buf, requiredSize, arg, requiredSize) != 0)
                        return false;
                    if (actualSize <= 1)
                    {
                        printf("-f:<format string> expected\n");
                        return false;
                    }
                    assert(actualSize == requiredSize);
                    // remove double quotes around the string, if given
                    if (actualSize >= 2 && buf[0] == '"' && buf[actualSize - 2] == '"')
                    {
                        buf[actualSize - 2] = '\0';
                        buf++;
                    }
                    InterpretEscapeSequences(buf);
                    AddInterestingString(buf);
                }
                break;

            case 'g':
            case 'G':
                if (arg[2] == ':')
                {
                    wchar_t* end = nullptr;
                    s_gcFilterStart = wcstol(arg+3, &end, 10);
                    if (*end == '-')
                    {
                        s_gcFilterEnd = wcstol(end+1, &end, 10);
                    }
                    else
                    {
                        s_gcFilterEnd = s_gcFilterStart;
                    }
                    if (*end != '\0')
                    {
                        printf("could not parse option %S\n", arg);
                        return false;
                    }
                }
                else
                {
                    printf("-g:<gc index> or -g:<gc index start>-<gc index end> expected\n");
                    return false;
                }
                break;

            case 'i':
            case 'I':
                if (arg[2] == ':')
                {
                    wchar_t* end = nullptr;
                    s_facilityIgnore = wcstoul(arg + 3, &end, 16);
                    if (*end != '\0')
                    {
                        printf("could not parse option %S\n", arg);
                        return false;
                    }
                }
                else
                {
                    printf("-i:<hex facility code> expected\n");
                    return false;
                }
                break;

            case 'e':
            case 'E':
                if (arg[2] == '\0')
                {
                    s_printEarliestMessages = true;
                }
                else if (arg[2] == ':')
                {
                    arg = arg + 3;
                    while (true)
                    {
                        if (s_printEarliestMessageFromThreadCount >= MAX_THREAD_FILTERS)
                        {
                            printf("too many threads - max is %d\n", MAX_THREAD_FILTERS);
                            return false;
                        }
                        wchar_t* end = nullptr;
                        if (_wcsnicmp(arg, L"gc", 2) == 0 || _wcsnicmp(arg, L"bg", 2) == 0)
                        {
                            int gcHeapNumber = wcstol(arg + 2, &end, 10);
                            GcThreadKind kind = _wcsnicmp(arg, L"gc", 2) == 0 ? GC_THREAD_FG : GC_THREAD_BG;
                            if (gcHeapNumber < MAX_NUMBER_OF_HEAPS)
                            {
                                s_printEarliestMessageFromGcThread[gcHeapNumber][kind] = true;
                            }
                            else
                            {
                                printf("expected heap number < %d\n", MAX_NUMBER_OF_HEAPS);
                                return false;
                            }
                        }
                        else
                        {
                            int i = s_printEarliestMessageFromThreadCount++;
                            s_printEarliestMessageFromThread[i] = _wcstoui64(arg, &end, 16);
                        }
                        if (*end == ',')
                        {
                            arg = end + 1;
                        }
                        else if (*end != '\0')
                        {
                            printf("could not parse %S\n", arg);
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    printf("could not parse option %S\n", arg);
                    return false;
                }
                break;

            case '?':
                Usage();
                return false;

            default:
                printf("unrecognized option %S\n", arg);
                return false;
            }
        }
        i++;
    }
    return true;
}

static void IncludeMessage(uint64_t threadId, StressMsg* msg)
{
    LONG msgCount = _InterlockedIncrement(&s_msgCount) - 1;
    if (msgCount < MAX_MESSAGE_COUNT)
    {
        s_threadMsgBuf[msgCount].threadId = threadId;
        s_threadMsgBuf[msgCount].msg = msg;
        s_threadMsgBuf[msgCount].msgId = msgCount;
    }
}

DWORD WINAPI ProcessStresslogWorker(LPVOID)
{
    StressLog::StressLogHeader* hdr = s_hdr;
    LONG totalMsgCount = 0;
    int wrappedWriteThreadCount = 0;
    bool fTimeFilter = s_timeFilterStart != 0.0 || s_timeFilterEnd != 0.0;
    for (int threadStressLogIndex = 0; threadStressLogIndex < s_threadStressLogCount; threadStressLogIndex++)
    {
        // is another thread already working on this thread stress log?
        if (s_threadStressLogDesc[threadStressLogIndex].workStarted != 0 || InterlockedIncrement(&s_threadStressLogDesc[threadStressLogIndex].workStarted) != 1)
            continue;

        ThreadStressLog* tsl = s_threadStressLogDesc[threadStressLogIndex].tsl;
        if (!tsl->IsValid())
            continue;
        if (!FilterThread(tsl))
            continue;
        if (tsl->writeHasWrapped)
        {
            wrappedWriteThreadCount++;
        }
        //        printf("thread: %Ix\n", tsl->threadId);
        StressMsg* msg = tsl->curPtr;
        StressLogChunk* slc = tsl->curWriteChunk;
        int chunkCount = 0;
        StressMsg* prevMsg = nullptr;
        while (true)
        {
            //            printf("stress log chunk %Ix\n", (size_t)slc);
            if (!slc->IsValid())
            {
                printf("oops, invalid stress log chunk\n");
                slc = slc->next;
                if (slc == tsl->curWriteChunk)
                    break;
                chunkCount++;
                if (chunkCount >= tsl->chunkListLength)
                {
                    printf("oops, more chunks on list than expected\n");
                    break;
                }
                msg = nullptr;
            }
            assert(slc->next->prev == slc);
            assert(slc->prev->next == slc);
#ifdef _DEBUG
            int chunkCount1 = 0;
            for (StressLogChunk* slc1 = tsl->curWriteChunk; slc1 != slc; slc1 = slc1->next)
            {
                chunkCount1++;
            }
            if (chunkCount != chunkCount1)
            {
                printf("oops, we have a loop\n");
                break;
            }
#endif //_DEBUG

            size_t* p = (size_t*)slc->StartPtr();
            size_t* end = (size_t*)slc->EndPtr();
            if (p <= (size_t*)msg && (size_t*)msg < end)
            {
                ; // fine
            }
            else
            {
                while (p < end && *p == 0)
                    p++;
                msg = (StressMsg*)p;
            }
            StressMsg* endMsg = (StressMsg*)end;
            while (msg < endMsg)
            {
                totalMsgCount++;
                char* format = (char*)(hdr->moduleImage + msg->formatOffset);
                double deltaTime = ((double)(msg->timeStamp - hdr->startTimeStamp)) / hdr->tickFrequency;
                bool fIgnoreMessage = false;
                if (fTimeFilter)
                {
                    if (deltaTime < s_timeFilterStart)
                    {
                        // we know the times will only get smaller, so can stop here
                        break;
                    }
                    if (deltaTime > s_timeFilterEnd)
                    {
                        fIgnoreMessage = true;
                    }
                }
                int numberOfArgs = (msg->numberOfArgsX << 3) + msg->numberOfArgs;
                if (!fIgnoreMessage)
                {
                    bool fIncludeMessage = FilterMessage(hdr, tsl, msg->facility, format, deltaTime, numberOfArgs, msg->args);
                    if (!fIncludeMessage && s_valueFilterCount > 0)
                    {
                        for (int i = 0; i < numberOfArgs; i++)
                        {
                            for (int j = 0; j < s_valueFilterCount; j++)
                            {
                                if (s_valueFilter[j].start <= (size_t)msg->args[i] && (size_t)msg->args[i] <= s_valueFilter[j].end)
                                {
                                    fIncludeMessage = true;
                                    break;
                                }
                            }
                            if (fIncludeMessage)
                                break;
                        }
                    }
                    if (fIncludeMessage)
                    {
                        IncludeMessage(tsl->threadId, msg);
                    }
                }
                prevMsg = msg;
                msg = (StressMsg*)&msg->args[numberOfArgs];
            }
            if (slc == tsl->chunkListTail && !tsl->writeHasWrapped)
                break;
            slc = slc->next;
            if (slc == tsl->curWriteChunk)
                break;
            if (s_hadGcThreadFilters && !FilterThread(tsl))
                break;
            chunkCount++;
            if (chunkCount >= tsl->chunkListLength)
            {
                printf("oops, more chunks on list than expected\n");
                break;
            }
            msg = nullptr;
        }
        s_threadStressLogDesc[threadStressLogIndex].earliestMessage = prevMsg;
        s_threadStressLogDesc[threadStressLogIndex].workFinished = 1;
    }

    InterlockedAdd(&s_totalMsgCount, totalMsgCount);
    InterlockedAdd(&s_wrappedWriteThreadCount, wrappedWriteThreadCount);

    return 0;
}

static double FindLatestTime(StressLog::StressLogHeader* hdr)
{
    double latestTime = 0.0;
    for (ThreadStressLog* tsl = hdr->logs.t; tsl != nullptr; tsl = tsl->next)
    {
        StressMsg* msg = tsl->curPtr;
        double deltaTime = ((double)(msg->timeStamp - hdr->startTimeStamp)) / hdr->tickFrequency;
        latestTime = max(latestTime, deltaTime);
    }
    return latestTime;
}

static void PrintFriendlyNumber(int n)
{
    if (n < 1000)
        printf("%d", n);
    else if (n < 1000 * 1000)
        printf("%5.3f thousand", n / 1000.0);
    else if (n < 1000 * 1000 * 1000)
        printf("%8.6f million", n / 1000000.0);
    else
        printf("%11.9f billion", n / 1000000000.0);
}

static void PrintMessage(CorClrData& corClrData, FILE *outputFile, uint64_t threadId, StressMsg* msg)
{
    void* argBuffer[StressMsg::maxArgCnt];
    char* format = (char*)(s_hdr->moduleImage + msg->formatOffset);
    int numberOfArgs = (msg->numberOfArgsX << 3) + msg->numberOfArgs;
    for (int i = 0; i < numberOfArgs; i++)
    {
        argBuffer[i] = msg->args[i];
    }
    double deltaTime = ((double)(msg->timeStamp - s_hdr->startTimeStamp)) / s_hdr->tickFrequency;
    if (!s_printHexTidForGcThreads)
    {
        GcThread gcThread;
        if (LookupGcThread(threadId, &gcThread))
        {
            threadId = gcThread.heapNumber;
            if (gcThread.kind == GC_THREAD_FG)
                threadId |= 0x8000000000000000;
            else
                threadId |= 0x4000000000000000;
        }
    }
    formatOutput(&corClrData, outputFile, format, threadId, deltaTime, msg->facility, argBuffer, s_fPrintFormatStrings);
}

extern "C" int __declspec(dllexport) ProcessStresslog(void* baseAddress, int argc, wchar_t* argv[])
{
    if (!ParseOptions(argc, argv))
        return 1;

    StressLog::StressLogHeader* hdr = (StressLog::StressLogHeader*)baseAddress;
    if (hdr->headerSize != sizeof(*hdr) ||
        hdr->magic != 'STRL' ||
        hdr->version != 0x00010001)
    {
        printf("Unrecognized file format\n");
        return 1;
    }
    s_hdr = hdr;
    s_threadMsgBuf = new StressThreadAndMsg[MAX_MESSAGE_COUNT];
    int threadStressLogIndex = 0;
    double latestTime = FindLatestTime(hdr);
    if (s_timeFilterStart < 0)
    {
        s_timeFilterStart = max(latestTime + s_timeFilterStart, 0);
        s_timeFilterEnd = latestTime;
    }
    for (ThreadStressLog* tsl = hdr->logs.t; tsl != nullptr; tsl = tsl->next)
    {
        if (!tsl->IsValid())
            continue;
        if (!FilterThread(tsl))
            continue;
        if (threadStressLogIndex >= MAX_THREADSTRESSLOGS)
        {
            printf("too many threads\n");
            return 1;
        }
        s_threadStressLogDesc[threadStressLogIndex].tsl = tsl;
        threadStressLogIndex++;
    }
    s_threadStressLogCount = threadStressLogIndex;
    s_wrappedWriteThreadCount = 0;

    SYSTEM_INFO systemInfo;
    GetSystemInfo(&systemInfo);

    DWORD threadCount = min(systemInfo.dwNumberOfProcessors, MAXIMUM_WAIT_OBJECTS);
    HANDLE threadHandle[64];
    for (DWORD i = 0; i < threadCount; i++)
    {
        threadHandle[i] = CreateThread(NULL, 0, ProcessStresslogWorker, nullptr, 0, nullptr);
        if (threadHandle[i] == 0)
        {
            printf("CreateThread failed\n");
            return 1;
        }
    }
    WaitForMultipleObjects(threadCount, threadHandle, TRUE, INFINITE);

    // the interlocked increment may have increased s_msgCount beyond MAX_MESSAGE_COUNT -
    // make sure we don't go beyond the end of the buffer
    s_msgCount = min(s_msgCount, MAX_MESSAGE_COUNT);
    
    if (s_gcFilterStart != 0)
    {
        // find the time interval that includes the GCs in question
        double startTime = INFINITY;
        double endTime = 0.0;
        for (int i = s_gcFilterStart; i <= s_gcFilterEnd; i++)
        {
            startTime = min(startTime, s_gcStartEnd[i].startTime);
            if (s_gcStartEnd[i].endTime != 0.0)
            {
                endTime = max(endTime, s_gcStartEnd[i].endTime);
            }
            else
            {
                // haven't seen the end - assume it's still in progress
                endTime = latestTime;
            }
        }

        // remove all messages outside of this time interval
        int remMsgCount = 0;
        for (int msgIndex = 0; msgIndex < s_msgCount; msgIndex++)
        {
            StressMsg* msg = s_threadMsgBuf[msgIndex].msg;
            double deltaTime = ((double)(msg->timeStamp - hdr->startTimeStamp)) / hdr->tickFrequency;
            if (startTime <= deltaTime && deltaTime <= endTime)
            {
                s_threadMsgBuf[remMsgCount] = s_threadMsgBuf[msgIndex];
                remMsgCount++;
            }
        }
        s_msgCount = remMsgCount;
    }

    if (s_hadGcThreadFilters)
    {
        for (int k = GC_THREAD_FG; k <= GC_THREAD_BG; k++)
        {
            for (int heap = 0; heap <= s_maxHeapNumberSeen; heap++)
            {
                if (s_gcThreadFilter[heap][k])
                {
                    uint64_t threadId = s_threadIdOfHeap[heap][k];
                    if (threadId != 0)
                    {
                        if (s_threadFilterCount < MAX_THREAD_FILTERS)
                        {
                            int i = s_threadFilterCount++;
                            s_threadFilter[i] = threadId;
                        }
                        else
                        {
                            printf("too many thread filters, max = %d\n", MAX_THREAD_FILTERS);
                        }
                    }
                    else
                    {
                        printf("don't know thread id for GC%d, ignoring\n", heap);
                    }
                }
            }
        }
    }

    if (s_threadFilterCount > 0)
    {
        // remove all messages from other threads
        int remMsgCount = 0;
        for (int msgIndex = 0; msgIndex < s_msgCount; msgIndex++)
        {
            uint64_t threadId = s_threadMsgBuf[msgIndex].threadId;
            for (int i = 0; i < s_threadFilterCount; i++)
            {
                if (threadId == s_threadFilter[i])
                {
                    s_threadMsgBuf[remMsgCount] = s_threadMsgBuf[msgIndex];
                    remMsgCount++;
                    break;
                }
            }
        }
        s_msgCount = remMsgCount;
    }

    // if the sort becomes a bottleneck, we can do a bucket sort by time 
    // (say fractions of a second), then sort the individual buckets,
    // perhaps on multiple threads
    qsort(s_threadMsgBuf, s_msgCount, sizeof(s_threadMsgBuf[0]), CmpMsg);

    CorClrData corClrData(hdr);
    FILE* outputFile = stdout;
    if (s_outputFileName != nullptr)
    {
        if (_wfopen_s(&outputFile, s_outputFileName, L"w") != 0)
        {
            printf("could not create output file %S\n", s_outputFileName);
            outputFile = stdout;
        }
    }

    for (size_t i = 0; i < s_msgCount; i++)
    {
        uint64_t threadId = (unsigned)s_threadMsgBuf[i].threadId;
        StressMsg* msg = s_threadMsgBuf[i].msg;
        PrintMessage(corClrData, outputFile, threadId, msg);
    }

    for (int k = GC_THREAD_FG; k <= GC_THREAD_BG; k++)
    {
        for (int heap = 0; heap <= s_maxHeapNumberSeen; heap++)
        {
            uint64_t threadId = s_threadIdOfHeap[heap][k];
            if (threadId != 0)
            {
                if (s_printEarliestMessageFromGcThread[heap][k])
                {
                    if (s_printEarliestMessageFromThreadCount < MAX_THREAD_FILTERS)
                    {
                        int i = s_printEarliestMessageFromThreadCount++;
                        s_printEarliestMessageFromThread[i] = threadId;
                    }
                    else
                    {
                        printf("too many threads, max = %d\n", MAX_THREAD_FILTERS);
                    }
                }
            }
            else
            {
                printf("don't know thread id for GC%d, ignoring\n", heap);
            }
        }
    }

    if (s_printEarliestMessages || s_printEarliestMessageFromThreadCount > 0)
    {
        fprintf(outputFile, "\nEarliestMessages:\n");
        LONG earliestStartCount = s_msgCount;
        for (int threadStressLogIndex = 0; threadStressLogIndex < s_threadStressLogCount; threadStressLogIndex++)
        {
            StressMsg* msg = s_threadStressLogDesc[threadStressLogIndex].earliestMessage;
            if (msg == nullptr)
                continue;
            bool fIncludeMessage = s_printEarliestMessages;
            uint64_t threadId = s_threadStressLogDesc[threadStressLogIndex].tsl->threadId;
            if (!fIncludeMessage)
            {
                for (int i = 0; i < s_printEarliestMessageFromThreadCount; i++)
                {
                    if (threadId == s_printEarliestMessageFromThread[i])
                    {
                        fIncludeMessage = true;
                        break;
                    }
                }
            }
            if (fIncludeMessage)
            {
                IncludeMessage(threadId, msg);
            }
        }
        qsort(&s_threadMsgBuf[earliestStartCount], s_msgCount - earliestStartCount, sizeof(s_threadMsgBuf[0]), CmpMsg);
        for (size_t i = earliestStartCount; i < s_msgCount; i++)
        {
            uint64_t threadId = (unsigned)s_threadMsgBuf[i].threadId;
            StressMsg* msg = s_threadMsgBuf[i].msg;
            PrintMessage(corClrData, outputFile, threadId, msg);
        }
    }

    if (outputFile != stdout)
        fclose(outputFile);

    ptrdiff_t usedSize = hdr->memoryCur - hdr->memoryBase;
    ptrdiff_t availSize = hdr->memoryLimit - hdr->memoryCur;
    printf("Used file size: %6.3f GB, still available: %6.3f GB, %d threads total, %d overwrote earlier messages\n", 
        (double)usedSize / (1024 * 1024 * 1024), (double)availSize/ (1024 * 1024 * 1024),
        s_threadStressLogCount, s_wrappedWriteThreadCount);
    if (hdr->threadsWithNoLog != 0)
        printf("%Id threads did not get a log!\n", hdr->threadsWithNoLog);
    printf("Number of messages examined: "); PrintFriendlyNumber(s_totalMsgCount); printf(", printed: "); PrintFriendlyNumber(s_msgCount); printf("\n");

    delete[] s_threadMsgBuf;

    return 0;
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

