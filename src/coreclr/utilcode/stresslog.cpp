// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*************************************************************************************/
/*                                   StressLog.cpp                                   */
/*************************************************************************************/

/*************************************************************************************/

#include "stdafx.h"			// precompiled headers

#include "switches.h"
#include "stresslog.h"
#include "clrhost.h"
#define DONOT_DEFINE_ETW_CALLBACK
#include "eventtracebase.h"
#include "ex.h"

 #if !defined(STRESS_LOG_READONLY)
#ifdef HOST_WINDOWS
HANDLE StressLogChunk::s_LogChunkHeap = NULL;
#endif
thread_local ThreadStressLog* StressLog::t_pCurrentThreadLog;
thread_local bool t_triedToCreateThreadStressLog;
#endif // !STRESS_LOG_READONLY

/*********************************************************************************/
#if defined(HOST_X86)

/* This is like QueryPerformanceCounter but a lot faster.  On machines with
   variable-speed CPUs (for power management), this is not accurate, but may
   be good enough.
*/
__forceinline __declspec(naked) unsigned __int64 getTimeStamp() {
    STATIC_CONTRACT_LEAF;

   __asm {
        RDTSC   // read time stamp counter
        ret
    };
}

#else // HOST_X86
unsigned __int64 getTimeStamp() {
    STATIC_CONTRACT_LEAF;

    LARGE_INTEGER ret;
    ZeroMemory(&ret, sizeof(LARGE_INTEGER));

    QueryPerformanceCounter(&ret);

    return ret.QuadPart;
}

#endif // HOST_X86

#if defined(HOST_X86) && !defined(HOST_UNIX)

/*********************************************************************************/
/* Get the the frequency cooresponding to 'getTimeStamp'.  For x86, this is the
   frequency of the RDTSC instruction, which is just the clock rate of the CPU.
   This can vary due to power management, so this is at best a rough approximation.
*/
unsigned __int64 getTickFrequency()
{
    //
    // At startup, the OS calculates the CPU clock frequency and makes it available
    // at HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0
    //

    unsigned __int64 hz = 0;

    HKEY hKey;
    if (ERROR_SUCCESS == RegOpenKeyExW(
        HKEY_LOCAL_MACHINE,
        W("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0"),
        0,
        KEY_QUERY_VALUE,
        &hKey))
    {
        DWORD mhz;
        DWORD mhzType;
        DWORD cbMhz = (DWORD)sizeof(mhz);
        if (ERROR_SUCCESS == RegQueryValueExW(
            hKey,
            W("~MHz"),
            NULL,
            &mhzType,
            (LPBYTE)&mhz,
            &cbMhz))
        {
            _ASSERTE(REG_DWORD == mhzType);
            _ASSERTE((DWORD)sizeof(mhz) == cbMhz);

            hz = (unsigned __int64)mhz * 1000000;
        }

        RegCloseKey(hKey);
    }

    return hz;
}

#else // HOST_X86


/*********************************************************************************/
/* Get the the frequency cooresponding to 'getTimeStamp'.  For non-x86
   architectures, this is just the performance counter frequency.
*/
unsigned __int64 getTickFrequency()
{
    LARGE_INTEGER ret;
    ZeroMemory(&ret, sizeof(LARGE_INTEGER));
    QueryPerformanceFrequency(&ret);
    return ret.QuadPart;
}

#endif // HOST_X86

#ifdef STRESS_LOG

StressLog StressLog::theLog = { 0, 0, 0, 0, 0, 0, TLS_OUT_OF_INDEXES, 0, 0, 0 };
const static unsigned __int64 RECYCLE_AGE = 0x40000000L;        // after a billion cycles, we can discard old threads

/*********************************************************************************/
void StressLog::Enter(CRITSEC_COOKIE) {
    STATIC_CONTRACT_LEAF;

    IncCantAllocCount();
    ClrEnterCriticalSection(theLog.lock);
    DecCantAllocCount();
}

void StressLog::Leave(CRITSEC_COOKIE) {
    STATIC_CONTRACT_LEAF;

    IncCantAllocCount();
    ClrLeaveCriticalSection(theLog.lock);
    DecCantAllocCount();
}

/*********************************************************************************/
void StressLog::Initialize(unsigned facilities, unsigned level, unsigned maxBytesPerThread,
    unsigned maxBytesTotal, void* moduleBase, LPWSTR logFilename)
{
    STATIC_CONTRACT_LEAF;

    if (theLog.MaxSizePerThread != 0)
    {
        // guard ourself against multiple initialization. First init wins.
        return;
    }

    theLog.lock = ClrCreateCriticalSection(CrstStressLog, (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));
    // StressLog::Terminate is going to free memory.
    if (maxBytesPerThread < STRESSLOG_CHUNK_SIZE)
    {
        maxBytesPerThread = STRESSLOG_CHUNK_SIZE;
    }
    theLog.MaxSizePerThread = maxBytesPerThread;

    if (maxBytesTotal < STRESSLOG_CHUNK_SIZE * 256)
    {
        maxBytesTotal = STRESSLOG_CHUNK_SIZE * 256;
    }
    theLog.MaxSizeTotal = (unsigned)min(maxBytesTotal, 0xffffffff);;
    theLog.totalChunk = 0;
    theLog.facilitiesToLog = facilities | LF_ALWAYS;
    theLog.levelToLog = level;
    theLog.deadCount = 0;

    theLog.tickFrequency = getTickFrequency();

    GetSystemTimeAsFileTime(&theLog.startTime);
    theLog.startTimeStamp = getTimeStamp();
    theLog.moduleOffset = (SIZE_T)moduleBase;

#ifndef HOST_UNIX
#ifdef _DEBUG
    HMODULE hModNtdll = GetModuleHandleA("ntdll.dll");
    theLog.RtlCaptureStackBackTrace = reinterpret_cast<PFNRtlCaptureStackBackTrace>(
        GetProcAddress(hModNtdll, "RtlCaptureStackBackTrace"));
#endif // _DEBUG
#endif // !HOST_UNIX

#if !defined (STRESS_LOG_READONLY) && defined(HOST_WINDOWS)
    if (logFilename == nullptr)
    {
        StressLogChunk::s_LogChunkHeap = HeapCreate(0, STRESSLOG_CHUNK_SIZE * 128, 0);
        if (StressLogChunk::s_LogChunkHeap == NULL)
        {
            StressLogChunk::s_LogChunkHeap = GetProcessHeap();
        }
        _ASSERTE(StressLogChunk::s_LogChunkHeap);
    }
#endif //!STRESS_LOG_READONLY

#ifdef MEMORY_MAPPED_STRESSLOG
    if (logFilename != nullptr)
    {
        if (maxBytesTotal < sizeof(StressLogHeader))
        {
            return;
        }
        HandleHolder hFile = WszCreateFile(logFilename,
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ,
            NULL,                 // default security descriptor
            CREATE_ALWAYS,
            FILE_ATTRIBUTE_NORMAL,
            NULL);

        if (hFile == INVALID_HANDLE_VALUE)
        {
            return;
        }

        size_t fileSize = maxBytesTotal;
        HandleHolder hMap = WszCreateFileMapping(hFile, NULL, PAGE_READWRITE, (DWORD)(fileSize >> 32), (DWORD)fileSize, NULL);
        if (hMap == NULL)
        {
            return;
        }

        theLog.hMapView = MapViewOfFileEx(hMap, FILE_MAP_ALL_ACCESS, 0, 0, fileSize, (void*)0x400000000000);
        if (theLog.hMapView == NULL)
        {
            return;
        }

        StressLogHeader* hdr = (StressLogHeader*)(uint8_t*)(void*)theLog.hMapView;
        hdr->headerSize = sizeof(StressLogHeader);
        hdr->magic = 'STRL';
        hdr->version = 0x00010001;
        hdr->memoryBase = (uint8_t*)hdr;
        hdr->memoryCur = hdr->memoryBase + sizeof(StressLogHeader);
        hdr->memoryLimit = hdr->memoryBase + fileSize;
        hdr->logs = nullptr;
        hdr->tickFrequency = theLog.tickFrequency;
        hdr->startTimeStamp = theLog.startTimeStamp;
        theLog.stressLogHeader = hdr;

        // copy coreclr image - just for the string literals
        AddModule((uint8_t*)moduleBase);
    }
#endif
}

void StressLog::AddModule(uint8_t* moduleBase)
{
#ifdef MEMORY_MAPPED_STRESSLOG
    int moduleIndex = 0;
    StressLogHeader* hdr = theLog.stressLogHeader;
    if (hdr == nullptr)
    {
        // nothing to do for the non-memory mapped stress log
        return;
    }
    size_t cumSize = 0;
    while (moduleIndex < MAX_MODULES && hdr->modules[moduleIndex].baseAddress != nullptr)
    {
        if (hdr->modules[moduleIndex].baseAddress == moduleBase)
            return;
        cumSize += hdr->modules[moduleIndex].size;
        moduleIndex++;
    }
    if (moduleIndex >= MAX_MODULES)
    {
        DebugBreak();
        return;
    }
    hdr->modules[moduleIndex].baseAddress = moduleBase;
    uint8_t* addr = moduleBase;
    while (true)
    {
        MEMORY_BASIC_INFORMATION mbi;
        size_t size = VirtualQuery(addr, &mbi, sizeof(mbi));
        if (size == 0)
            break;
        // copy the region containing string literals to the memory mapped file
        if (mbi.AllocationBase != moduleBase)
            break;
        ptrdiff_t offs = (uint8_t*)mbi.BaseAddress - (uint8_t*)mbi.AllocationBase + cumSize;
        memcpy(&hdr->moduleImage[offs], mbi.BaseAddress, mbi.RegionSize);
        addr += mbi.RegionSize;
        hdr->modules[moduleIndex].size = (size_t)(addr - (uint8_t*)moduleBase);
    }
#endif //MEMORY_MAPPED_STRESSLOG
}

/*********************************************************************************/
void StressLog::Terminate(BOOL fProcessDetach) {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    theLog.facilitiesToLog = 0;

    StressLogLockHolder lockh(theLog.lock, FALSE);
    if (!fProcessDetach) {
        lockh.Acquire(); lockh.Release();       // The Enter() Leave() forces a memory barrier on weak memory model systems
                                // we want all the other threads to notice that facilitiesToLog is now zero

                // This is not strictly threadsafe, since there is no way of insuring when all the
                // threads are out of logMsg.  In practice, since they can no longer enter logMsg
                // and there are no blocking operations in logMsg, simply sleeping will insure
                // that everyone gets out.
        ClrSleepEx(2, FALSE);
        lockh.Acquire();
    }

    // Free the log memory
    ThreadStressLog* ptr = theLog.logs;
    theLog.logs = 0;
    while(ptr != 0) {
        ThreadStressLog* tmp = ptr;
        ptr = ptr->next;
        delete tmp;
    }

    if (!fProcessDetach) {
        lockh.Release();
    }

#if !defined (STRESS_LOG_READONLY) && defined(HOST_WINDOWS)
    if (StressLogChunk::s_LogChunkHeap != NULL && StressLogChunk::s_LogChunkHeap != GetProcessHeap ())
    {
        HeapDestroy (StressLogChunk::s_LogChunkHeap);
    }
#endif //!STRESS_LOG_READONLY
}

/*********************************************************************************/
/* create a new thread stress log buffer associated with Thread local slot, for the Stress log */

ThreadStressLog* StressLog::CreateThreadStressLog() {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    static PVOID callerID = NULL;

    ThreadStressLog* msgs = t_pCurrentThreadLog;
    if (msgs != NULL)
    {
        return msgs;
    }

    if (callerID == ClrTeb::GetFiberPtrId())
    {
        return NULL;
    }

#if defined(HOST_WINDOWS) && !defined(MEMORY_MAPPED_STRESSLOG)
    if (!StressLogChunk::s_LogChunkHeap)
    {
        return NULL;
    }
#endif

    //if we are not allowed to allocate stress log, we should not even try to take the lock
    if (IsInCantAllocStressLogRegion ())
    {
        return NULL;
    }

    // if it looks like we won't be allowed to allocate a new chunk, exit early
    if (theLog.deadCount == 0 && !AllowNewChunk (0))
    {
        return NULL;
    }

    StressLogLockHolder lockh(theLog.lock, FALSE);

    class NestedCaller
    {
    public:
        NestedCaller()
        {
        }
        ~NestedCaller()
        {
            callerID = NULL;
        }
        void Mark()
        {
            callerID = ClrTeb::GetFiberPtrId();
        }
    };

    NestedCaller nested;

    BOOL noFLSNow = FALSE;

    PAL_CPP_TRY
    {
        // Acquiring the lack can throw an OOM exception the first time its called on a thread. We go
        // ahead and try to provoke that now, before we've altered the list of available stress logs, and bail if
        // we fail.
        lockh.Acquire();
        nested.Mark();

        // ClrFlsSetValue can throw an OOM exception the first time its called on a thread for a given slot. We go
        // ahead and try to provoke that now, before we've altered the list of available stress logs, and bail if
        // we fail.
        t_pCurrentThreadLog = NULL;
    }
#pragma warning(suppress: 4101)
    PAL_CPP_CATCH_DERIVED(OutOfMemoryException, obj)
    {
        // Just leave on any exception. Note: can't goto or return from within EX_CATCH...
        noFLSNow = TRUE;
    }
    PAL_CPP_ENDTRY;

    if (noFLSNow == FALSE && theLog.facilitiesToLog != 0)
        msgs = CreateThreadStressLogHelper();

    return msgs;
}

ThreadStressLog* StressLog::CreateThreadStressLogHelper() {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    BOOL skipInsert = FALSE;
    ThreadStressLog* msgs = NULL;

    // See if we can recycle a dead thread
    if (theLog.deadCount > 0)
    {
        unsigned __int64 recycleStamp = getTimeStamp() - RECYCLE_AGE;
        msgs = theLog.logs;
        //find out oldest dead ThreadStressLog in case we can't find one within
        //recycle age but can't create a new chunk
        ThreadStressLog * oldestDeadMsg = NULL;

        while(msgs != 0)
        {
            if (msgs->isDead)
            {
                BOOL hasTimeStamp = msgs->curPtr != (StressMsg *)msgs->chunkListTail->EndPtr();
                if (hasTimeStamp && msgs->curPtr->timeStamp < recycleStamp)
                {
                    skipInsert = TRUE;
                    InterlockedDecrement(&theLog.deadCount);
                    break;
                }

                if (!oldestDeadMsg)
                {
                    oldestDeadMsg = msgs;
                }
                else if (hasTimeStamp && oldestDeadMsg->curPtr->timeStamp > msgs->curPtr->timeStamp)
                {
                    oldestDeadMsg = msgs;
                }
            }

            msgs = msgs->next;
        }

        //if the total stress log size limit is already passed and we can't add new chunk,
        //always reuse the oldest dead msg
        if (!AllowNewChunk (0) && !msgs)
        {
            msgs = oldestDeadMsg;
            skipInsert = TRUE;
            InterlockedDecrement(&theLog.deadCount);
        }
    }

    if (msgs == 0)  {
    	FAULT_NOT_FATAL(); // We don't mind if we can't allocate here, we'll try again later.
    	if (IsInCantAllocStressLogRegion ())
    	{
            goto LEAVE;
    	}

    	msgs = new (nothrow) ThreadStressLog;

        if (msgs == 0 ||!msgs->IsValid ())
        {
            delete msgs;
            msgs = 0;
#ifdef MEMORY_MAPPED_STRESSLOG
            if (!t_triedToCreateThreadStressLog && theLog.stressLogHeader != nullptr)
            {
                theLog.stressLogHeader->threadsWithNoLog++;
                t_triedToCreateThreadStressLog = true;
            }
#endif //MEMORY_MAPPED_STRESSLOG
            goto LEAVE;
        }
    }
    else
    {
        // recycle old thread msg
        msgs->threadId = GetCurrentThreadId();
        StressLogChunk* slc = msgs->chunkListHead;
        while (true)
        {
            if (slc == msgs->chunkListTail)
                break;
            slc = slc->next;
        }
    }

    msgs->Activate ();

    t_pCurrentThreadLog = msgs;

    if (!skipInsert) {
#ifdef _DEBUG
        ThreadStressLog* walk = theLog.logs;
        while (walk)
        {
            _ASSERTE (walk != msgs);
            walk = walk->next;
        }
#endif
        // Put it into the stress log
        msgs->next = theLog.logs;
        theLog.logs = msgs;
#ifdef MEMORY_MAPPED_STRESSLOG
        if (theLog.stressLogHeader != nullptr)
        theLog.stressLogHeader->logs = msgs;
#endif // MEMORY_MAPPED_STRESSLOG
    }

LEAVE:
    ;
    return msgs;
}

/*********************************************************************************/
/* static */
void StressLog::ThreadDetach() {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    ThreadStressLog* msgs = t_pCurrentThreadLog;

#ifndef DACCESS_COMPILE
    if (msgs == 0)
    {
        return;
    }

    t_pCurrentThreadLog = NULL;

    // We are deleting a fiber.  The thread is running a different fiber now.
    // We should write this message to the StressLog for deleted fiber.
    msgs->LogMsg (LF_STARTUP, 0, "******* DllMain THREAD_DETACH called Thread dying *******\n");
#endif

    msgs->isDead = TRUE;
    InterlockedIncrement(&theLog.deadCount);
}

BOOL StressLog::AllowNewChunk (LONG numChunksInCurThread)
{
    _ASSERTE (numChunksInCurThread <= theLog.totalChunk);
    DWORD perThreadLimit = theLog.MaxSizePerThread;

#ifndef DACCESS_COMPILE
    if (numChunksInCurThread == 0 && IsSuspendEEThread())
        return TRUE;

    if (IsGCSpecialThread())
    {
        perThreadLimit *= GC_STRESSLOG_MULTIPLY;
    }
#endif

    if ((DWORD)numChunksInCurThread * STRESSLOG_CHUNK_SIZE >= perThreadLimit)
    {
        return FALSE;
    }

    return theLog.MaxSizeTotal == 0xffffffff || (DWORD)theLog.totalChunk * STRESSLOG_CHUNK_SIZE < theLog.MaxSizeTotal;
}

BOOL StressLog::ReserveStressLogChunks (unsigned chunksToReserve)
{
    ThreadStressLog* msgs = t_pCurrentThreadLog;

    if (msgs == 0)
    {
        msgs = CreateThreadStressLog();

        if (msgs == 0)
            return FALSE;
    }

    if (chunksToReserve == 0)
    {
        chunksToReserve = (theLog.MaxSizePerThread + STRESSLOG_CHUNK_SIZE - 1)  / STRESSLOG_CHUNK_SIZE;
    }

    LONG numTries = (LONG)chunksToReserve - msgs->chunkListLength;
    for (LONG i = 0; i < numTries; i++)
    {
        msgs->GrowChunkList ();
    }

    return msgs->chunkListLength >= (LONG)chunksToReserve;
}

void (*FSwitchToSOTolerant)();
void (*FSwitchToSOIntolerant)();
void TrackSO(BOOL tolerance)
{
    if (tolerance)
    {
        if (FSwitchToSOTolerant)
        {
            FSwitchToSOTolerant();
        }
    }
    else
    {
        if (FSwitchToSOIntolerant)
        {
            FSwitchToSOIntolerant();
        }
    }
}

/*********************************************************************************/
/* fetch a buffer that can be used to write a stress message, it is thread safe */
FORCEINLINE void ThreadStressLog::LogMsg(unsigned facility, int cArgs, const char* format, va_list Args)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    // Asserts in this function cause infinite loops in the asserting mechanism.
    // Just use debug breaks instead.

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
    // _ASSERTE ( cArgs >= 0 && cArgs <= 63 );
    if (cArgs < 0 || cArgs > 63) DebugBreak();
#endif //

    size_t offs = 0;
#ifdef MEMORY_MAPPED_STRESSLOG
    StressLog::StressLogHeader* hdr = StressLog::theLog.stressLogHeader;
    if (hdr != nullptr)
    {
        int moduleIndex = 0;
        size_t cumSize = 0;
        offs = 0;
        while (moduleIndex < StressLog::MAX_MODULES)
        {
            offs = (uint8_t*)format - hdr->modules[moduleIndex].baseAddress;
            if (offs < hdr->modules[moduleIndex].size)
            {
                offs += cumSize;
                break;
            }
            cumSize += hdr->modules[moduleIndex].size;
            moduleIndex++;
        }
    }
    else
#endif // MEMORY_MAPPED_STRESSLOG
    {
#ifdef _DEBUG
        // currently SOS stress log only supports a maximum of 7 arguments
        if (cArgs > 7)
            DebugBreak();
#endif //_DEBUG
        offs = ((size_t)format - StressLog::theLog.moduleOffset);
    }

    // _ASSERTE ( offs < StressMsg::maxOffset );
    if (offs >= StressMsg::maxOffset)
    {
#ifdef _DEBUG
        DebugBreak(); // in lieu of the above _ASSERTE
#endif // _DEBUG

        // Set it to this string instead.
        offs =
#ifdef _DEBUG
            (size_t)"<BUG: StressLog format string beyond maxOffset>";
#else // _DEBUG
            0; // a 0 offset is ignored by StressLog::Dump
#endif // _DEBUG else
    }

    // Get next available slot
    StressMsg* msg = AdvanceWrite(cArgs);

    msg->timeStamp = getTimeStamp();
    msg->facility = facility;
    msg->formatOffset = offs;
    msg->numberOfArgs = cArgs & 0x7;
    msg->numberOfArgsX = cArgs >> 3;

    for ( int i = 0; i < cArgs; ++i )
    {
        void* data = va_arg(Args, void*);
        msg->args[i] = data;
    }

    // only store curPtr once the msg is complete
    curPtr = msg;

#ifdef _DEBUG
    if (!IsValid () || threadId != GetCurrentThreadId ())
        DebugBreak();
#endif // _DEBUG
#endif //DACCESS_COMPILE
}

void ThreadStressLog::LogMsg(unsigned facility, int cArgs, const char* format, ...)
{
    va_list Args;
    va_start(Args, format);
    LogMsg(facility, cArgs, format, Args);
    va_end(Args);
}

FORCEINLINE BOOL StressLog::InlinedStressLogOn(unsigned facility, unsigned level)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

#if defined(DACCESS_COMPILE)
    return FALSE;
#else
    return ((theLog.facilitiesToLog & facility) && (level <= theLog.levelToLog));
#endif
}

BOOL StressLog::StressLogOn(unsigned facility, unsigned level)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

    return InlinedStressLogOn(facility, level);
}

FORCEINLINE BOOL StressLog::InlinedETWLogOn(unsigned facility, unsigned level)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

    return FALSE;
}

BOOL StressLog::ETWLogOn(unsigned facility, unsigned level)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

    return InlinedETWLogOn(facility, level);
}

#if !defined(DACCESS_COMPILE)
BOOL StressLog::LogOn(unsigned facility, unsigned level)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

    return InlinedStressLogOn(facility, level) || InlinedETWLogOn(facility, level);
}
#endif

/* static */
void StressLog::LogMsg(unsigned level, unsigned facility, int cArgs, const char* format, ...)
{
    STATIC_CONTRACT_SUPPORTS_DAC;
#ifndef DACCESS_COMPILE
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    // Any stresslog LogMsg could theoretically create a new stress log and thus
    // enter a critical section.  But we don't want these to cause violations in
    // CANNOT_TAKE_LOCK callers, since the callers would otherwise be fine in runs that don't
    // set the stress log config parameter.
    CONTRACT_VIOLATION(TakesLockViolation);

    _ASSERTE(cArgs >= 0 && cArgs <= 63);

    va_list Args;

    if (InlinedStressLogOn(facility, level))
    {
        ThreadStressLog* msgs = t_pCurrentThreadLog;

        if (msgs == 0)
        {
            msgs = CreateThreadStressLog();

            if (msgs == 0)
                return;
        }
        va_start(Args, format);
        msgs->LogMsg(facility, cArgs, format, Args);
        va_end(Args);
    }

    // Stress Log ETW feature available only on the desktop versions of the runtime
#endif //!DACCESS_COMPILE
}

/* static */
void StressLog::LogMsg(unsigned level, unsigned facility, const StressLogMsg &msg)
{
    STATIC_CONTRACT_SUPPORTS_DAC;
#ifndef DACCESS_COMPILE
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    // Any stresslog LogMsg could theoretically create a new stress log and thus
    // enter a critical section.  But we don't want these to cause violations in
    // CANNOT_TAKE_LOCK callers, since the callers would otherwise be fine in runs that don't
    // set the stress log config parameter.
    CONTRACT_VIOLATION(TakesLockViolation);

    _ASSERTE(msg.m_cArgs >= 0 && msg.m_cArgs <= 63);

    if (InlinedStressLogOn(facility, level))
    {
#ifdef HOST_WINDOWS // On Linux, this cast: (va_list)msg.m_args gives a compile error
       ThreadStressLog* msgs = t_pCurrentThreadLog;

        if (msgs == 0)
        {
            msgs = CreateThreadStressLog();

            if (msgs == 0)
                return;
        }
        msgs->LogMsg(facility, msg.m_cArgs, msg.m_format, (va_list)msg.m_args);
#endif //HOST_WINDOWS
    }

    // Stress Log ETW feature available only on the desktop versions of the runtime
#endif //!DACCESS_COMPILE
}

#ifdef _DEBUG
/* static */
void  StressLog::LogCallStack(const char *const callTag){
   if (theLog.RtlCaptureStackBackTrace)
   {
        size_t  CallStackTrace[MAX_CALL_STACK_TRACE];
        ULONG hash;
        USHORT stackTraceCount = theLog.RtlCaptureStackBackTrace (2, MAX_CALL_STACK_TRACE, (PVOID *)CallStackTrace, &hash);
        if (stackTraceCount > MAX_CALL_STACK_TRACE)
            stackTraceCount = MAX_CALL_STACK_TRACE;
        LogMsgOL("Start of %s stack \n", callTag);
        USHORT i = 0;
        for (;i < stackTraceCount; i++)
        {
            LogMsgOL("(%s stack)%pK\n", callTag, CallStackTrace[i]);
        }
        LogMsgOL("End of %s stack\n", callTag);
        }
}
#endif //_DEBUG

#ifdef MEMORY_MAPPED_STRESSLOG
void* StressLog::AllocMemoryMapped(size_t n)
{
    if ((ptrdiff_t)n > 0)
    {
        StressLogHeader* hdr = theLog.stressLogHeader;
        assert(hdr != nullptr);
        uint8_t* newMemValue = (uint8_t*)InterlockedAdd64((LONG64*)&hdr->memoryCur, n);
        if (newMemValue < hdr->memoryLimit)
        {
            return newMemValue - n;
        }
        // when we run out, we just can't allocate anymore
        hdr->memoryCur = hdr->memoryLimit;
    }
    return nullptr;
}

void* __cdecl ThreadStressLog::operator new(size_t n, const NoThrow&) NOEXCEPT
{
    if (StressLogChunk::s_LogChunkHeap != NULL)
    {
        //no need to zero memory because we could handle garbage contents
        return HeapAlloc(StressLogChunk::s_LogChunkHeap, 0, n);
    }
    else
    {
        return StressLog::AllocMemoryMapped(n);
    }
}
#endif //MEMORY_MAPPED_STRESSLOG

#endif // STRESS_LOG

