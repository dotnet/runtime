// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
HANDLE StressLogChunk::s_LogChunkHeap = NULL;
#endif // !STRESS_LOG_READONLY

/*********************************************************************************/
#if defined(_TARGET_X86_)

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

#else // _TARGET_X86_ 
unsigned __int64 getTimeStamp() {
    STATIC_CONTRACT_LEAF;
    
    LARGE_INTEGER ret;
    ZeroMemory(&ret, sizeof(LARGE_INTEGER));
    
    QueryPerformanceCounter(&ret);

    return ret.QuadPart;
}

#endif // _TARGET_X86_ 

#if defined(_TARGET_X86_)

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

#else // _TARGET_X86_ 


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

#endif // _TARGET_X86_ 

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
void StressLog::Initialize(unsigned facilities,  unsigned level, unsigned maxBytesPerThread, 
            unsigned maxBytesTotal, HMODULE hMod) 
{
    STATIC_CONTRACT_LEAF;

    if (theLog.MaxSizePerThread != 0)
    {
        // guard ourself against multiple initialization. First init wins.
        return;
    }
    
    _ASSERTE (theLog.TLSslot == (unsigned int)TLS_OUT_OF_INDEXES);
    theLog.lock = ClrCreateCriticalSection(CrstStressLog,(CrstFlags)(CRST_UNSAFE_ANYMODE|CRST_DEBUGGER_THREAD));
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
    theLog.MaxSizeTotal = maxBytesTotal;
    theLog.totalChunk = 0;
    theLog.facilitiesToLog = facilities | LF_ALWAYS;
    theLog.levelToLog = level;
    theLog.deadCount = 0;
    theLog.TLSslot = TlsIdx_StressLog;
	
    theLog.tickFrequency = getTickFrequency();
	
    GetSystemTimeAsFileTime (&theLog.startTime);
    theLog.startTimeStamp = getTimeStamp();

#ifndef FEATURE_PAL
    theLog.moduleOffset = (SIZE_T)hMod; // HMODULES are base addresses.

#ifdef _DEBUG
    HMODULE hModNtdll = GetModuleHandleA("ntdll.dll");
    theLog.RtlCaptureStackBackTrace = reinterpret_cast<PFNRtlCaptureStackBackTrace>(
            GetProcAddress(hModNtdll, "RtlCaptureStackBackTrace"));
#endif // _DEBUG

#else // !FEATURE_PAL
    theLog.moduleOffset = (SIZE_T)PAL_GetSymbolModuleBase((void *)StressLog::Initialize);
#endif // !FEATURE_PAL

#if !defined (STRESS_LOG_READONLY)    
    StressLogChunk::s_LogChunkHeap = ClrHeapCreate (0, STRESSLOG_CHUNK_SIZE * 128, 0);
    if (StressLogChunk::s_LogChunkHeap == NULL)
    {
        StressLogChunk::s_LogChunkHeap = ClrGetProcessHeap ();
    }
    _ASSERTE (StressLogChunk::s_LogChunkHeap);
#endif //!STRESS_LOG_READONLY
}

/*********************************************************************************/
void StressLog::Terminate(BOOL fProcessDetach) {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    
    if (theLog.TLSslot != (unsigned int)TLS_OUT_OF_INDEXES) {
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

        theLog.TLSslot = TLS_OUT_OF_INDEXES;
        if (!fProcessDetach) {
            lockh.Release();
        }
    }
#if !defined (STRESS_LOG_READONLY)    
    if (StressLogChunk::s_LogChunkHeap != NULL && StressLogChunk::s_LogChunkHeap != ClrGetProcessHeap ())
    {
        ClrHeapDestroy (StressLogChunk::s_LogChunkHeap);
    }
#endif //!STRESS_LOG_READONLY
}

/*********************************************************************************/
/* create a new thread stress log buffer associated with Thread local slot TLSslot, for the Stress log */

ThreadStressLog* StressLog::CreateThreadStressLog() {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    static PVOID callerID = NULL;

    ThreadStressLog* msgs = (ThreadStressLog*) ClrFlsGetValue(theLog.TLSslot);
    if (msgs != NULL)
    {
        return msgs;
    }

    if (callerID == ClrTeb::GetFiberPtrId())
    {
        return NULL;
    }

    //if we are not allowed to allocate stress log, we should not even try to take the lock
    if (!StressLogChunk::s_LogChunkHeap || !CanThisThreadCallIntoHost() || IsInCantAllocStressLogRegion ())
    {
        return NULL;
    }

    // if it looks like we won't be allowed to allocate a new chunk, exit early
    if (theLog.deadCount == 0 && !AllowNewChunk (0))
    {
        return NULL;
    }

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return NULL);
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
        ClrFlsSetValue(theLog.TLSslot, NULL);
    }
    PAL_CPP_CATCH_DERIVED(OutOfMemoryException, obj)
    {
        // Just leave on any exception. Note: can't goto or return from within EX_CATCH...
        noFLSNow = TRUE;
    }
    PAL_CPP_ENDTRY;

    if (noFLSNow == FALSE && theLog.facilitiesToLog != 0)
        msgs = CreateThreadStressLogHelper();

    END_SO_INTOLERANT_CODE;

    return msgs;
}

ThreadStressLog* StressLog::CreateThreadStressLogHelper() {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_INTOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(theLog.TLSslot != (unsigned int)TLS_OUT_OF_INDEXES); // because facilitiesToLog is != 0


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
            goto LEAVE;
        }
    }

    msgs->Activate ();

    // We know this isn't going to throw an exception now because the call to ClrFlsSetValue above succeeded for
    // this thread.
    {
        CONTRACT_VIOLATION(ThrowsViolation);
        ClrFlsSetValue(theLog.TLSslot, msgs);
    }

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
    }

LEAVE:
    ;
    return msgs;
}
    
/*********************************************************************************/
/* static */
void StressLog::ThreadDetach(ThreadStressLog *msgs) {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

#ifndef DACCESS_COMPILE
    if (msgs == 0)
    {
        return;
    }

    _ASSERTE(theLog.TLSslot != (unsigned int)TLS_OUT_OF_INDEXES); // because facilitiesToLog is != 0
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

    return (DWORD)theLog.totalChunk * STRESSLOG_CHUNK_SIZE < theLog.MaxSizeTotal;
}

BOOL StressLog::ReserveStressLogChunks (unsigned chunksToReserve)
{
    ThreadStressLog* msgs = (ThreadStressLog*) ClrFlsGetValue(theLog.TLSslot);

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
void ThreadStressLog::LogMsg ( DWORD_PTR facility, int cArgs, const char* format, va_list Args)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

	// Asserts in this function cause infinite loops in the asserting mechanism.
	// Just use debug breaks instead.
	
#ifndef DACCESS_COMPILE
#ifdef _DEBUG
    // _ASSERTE ( cArgs >= 0 && cArgs <= 7 );
	if (cArgs < 0 || cArgs > 7) DebugBreak();
#endif // 
    
    size_t offs = ((size_t)format - StressLog::theLog.moduleOffset);

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
	msg->numberOfArgs = cArgs;

    for ( int i = 0; i < cArgs; ++i )
    {
        void* data = va_arg(Args, void*);
        msg->args[i] = data;
    }

#ifdef _DEBUG
    if (!IsValid () || threadId != GetCurrentThreadId ())
        DebugBreak();
#endif // _DEBUG
#endif //DACCESS_COMPILE
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

#if defined(FEATURE_EVENT_TRACE) && !defined(FEATURE_CORECLR) && !defined(DACCESS_COMPILE)
    return ((Microsoft_Windows_DotNETRuntimeStressHandle != 0) && 
        (ETW_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_Context, (UCHAR)level, facility) != 0));
#else
    return FALSE;
#endif
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
void StressLog::LogMsg (unsigned level, unsigned facility, int cArgs, const char* format, ... )
{
    STATIC_CONTRACT_SUPPORTS_DAC;
#ifndef DACCESS_COMPILE
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    // Any stresslog LogMsg could theoretically create a new stress log and thus
    // enter a critical section.  But we don't want these to cause violations in
    // CANNOT_TAKE_LOCK callers, since the callers would otherwise be fine in runs that don't
    // set the stress log config parameter.
    CONTRACT_VIOLATION(TakesLockViolation);

    _ASSERTE ( cArgs >= 0 && cArgs <= 7 );

    va_list Args;
    
    if(InlinedStressLogOn(facility, level))
    {
        ThreadStressLog* msgs = (ThreadStressLog*) ClrFlsGetValue(theLog.TLSslot);

        if (msgs == 0) {
            msgs = CreateThreadStressLog();

            if (msgs == 0)
                return;
        }
        va_start(Args, format);
        msgs->LogMsg (facility, cArgs, format, Args);
        va_end(Args);
    }

// Stress Log ETW feature available only on the desktop versions of the runtime
#if !defined(FEATURE_CORECLR)
    // The previous LogMsg call could have modified the Args, so we need to reset it
    if(InlinedETWLogOn(facility, level))
    {
#define MAX_STRESSLOG_DATA_ETW_LENGTH 256
        CHAR logMessage[MAX_STRESSLOG_DATA_ETW_LENGTH];

        va_start(Args, format);
        ULONG messageLength = (USHORT)_vsnprintf_s(logMessage, COUNTOF(logMessage), MAX_STRESSLOG_DATA_ETW_LENGTH-1, format, Args);
        va_end(Args);
        
        if(messageLength >= 0 && 
           messageLength < MAX_STRESSLOG_DATA_ETW_LENGTH) // this condition has been added to make prefast happy
        {
            logMessage[messageLength] = 0;
        }
        messageLength++;
        logMessage[MAX_STRESSLOG_DATA_ETW_LENGTH-1] = 0;
        FireEtwStressLogEvent_V1((UINT32)facility, (UCHAR)level, logMessage, GetClrInstanceId());
#undef MAX_STRESSLOG_DATA_ETW_LENGTH
    }
#endif // !FEATURE_CORECLR
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

#endif // STRESS_LOG

