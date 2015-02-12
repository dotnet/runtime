//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

// ==++==
// 
 
// 
// ==--==

#include "strike.h"
#include "util.h"
#include <stdio.h>      
#include <ctype.h>

#ifndef STRESS_LOG
#define STRESS_LOG
#endif // STRESS_LOG
#define STRESS_LOG_READONLY
#include "stresslog.h"


void GcHistClear();
void GcHistAddLog(LPCSTR msg, StressMsg* stressMsg);


/*********************************************************************************/
static const WCHAR* getTime(const FILETIME* time, __out_ecount (buffLen) WCHAR* buff, int buffLen) 
{
    SYSTEMTIME systemTime;
    static const WCHAR badTime[] = L"BAD TIME";
    static const WCHAR format[] = L"HH:mm:ss";
    
    if (!FileTimeToSystemTime(time, &systemTime))
        return badTime;

#ifdef FEATURE_PAL
    if (0 == PAL_FormatDateW(NULL, format, FALSE, FALSE, 0, &systemTime, buff, buffLen))
        return badTime;
#else // FEATURE_PAL
    SYSTEMTIME localTime;
    SystemTimeToTzSpecificLocalTime(NULL, &systemTime, &localTime);

    // we want a non null buff for the following
    int ret = GetTimeFormatW(LOCALE_USER_DEFAULT, 0, &localTime, format, buff, buffLen);
    if (ret == 0)
        return badTime;
#endif // FEATURE_PAL else
    
    return buff;
}

/*********************************************************************************/
static inline __int64& toInt64(FILETIME& t) 
{
    return *((__int64 *) &t);
}

/*********************************************************************************/
ThreadStressLog* ThreadStressLog::FindLatestThreadLog() const 
{
    const ThreadStressLog* latestLog = 0;
    for (const ThreadStressLog* ptr = this; ptr != NULL; ptr = ptr->next) 
    {
        if (ptr->readPtr != NULL)
            if (latestLog == 0 || ptr->readPtr->timeStamp > latestLog->readPtr->timeStamp)
                latestLog = ptr;
    }
    return const_cast<ThreadStressLog*>(latestLog);
}

const char *getFacilityName(DWORD_PTR lf)
{
    struct FacilityName_t { size_t lf; const char* lfName; };
    #define DEFINE_LOG_FACILITY(logname, value) {logname, #logname},
    static FacilityName_t facilities[] =
    {
        #include <loglf.h>
        { LF_ALWAYS, "LF_ALWAYS" }
    };
    static char buff[1024] = "`";
    if ( lf == LF_ALL )
    {
        return "`ALL`";
    }
    else 
    {
        buff[1] = '\0';
        for ( int i = 0; i < 32; ++i )
        {
            if ( lf & 0x1 )
            {
                strcat_s ( buff, _countof(buff), &(facilities[i].lfName[3]) );
                strcat_s ( buff, _countof(buff), "`" );
            }
            lf >>= 1;
        }
        return buff;
    }
}

/***********************************************************************************/
/* recognize special pretty printing instructions in the format string             */
/* Note that this function might have side effect such that args array value might */
/* be altered if format string contains %s                                         */
// TODO: This function assumes the pointer size of the target equals the pointer size of the host
// TODO: replace uses of void* with appropriate TADDR or CLRDATA_ADDRESS
void formatOutput(struct IDebugDataSpaces* memCallBack, __in FILE* file, __inout __inout_z char* format, unsigned threadId, double timeStamp, DWORD_PTR facility, __in void** args)
{
    fprintf(file, "%4x %13.9f : ", threadId, timeStamp);
    fprintf(file, "%-20s ", getFacilityName ( facility ));

    CQuickBytes fullname;
    char* ptr = format;
    void** argsPtr = args;
    const SIZE_T capacity_buff = 2048;
    LPWSTR buff = (LPWSTR)alloca(capacity_buff * sizeof(WCHAR));
    static char formatCopy[256];
    
    int iArgCount = 0;
    
    strcpy_s(formatCopy, _countof(formatCopy), format);
    for(;;) 
    {
        char c = *ptr++;
        if (c == 0)
            break;
        if (c == '{')           // Reverse the '{' 's because the log is displayed backwards
            ptr[-1] = '}';
        else if (c == '}')
            ptr[-1] = '{';
        else if (c == '%') 
        {
            argsPtr++;          // This format will consume one of the args
            if (*ptr == '%') 
            {
                ptr++;          // skip the whole %%
                --argsPtr;      // except for a %% 
            }
            else if (*ptr == 'p') 
            {   // It is a %p
                ptr++;
                if (isalpha(*ptr)) 
                {   // It is a special %p formatter
                        // Print the string up to that point
                    c = *ptr;
                    *ptr = 0;       // Terminate the string temporarily
                    fprintf(file, format, args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                    *ptr = c;       // Put it back  

                        // move the argument pointers past the part the was printed
                    format = ptr + 1;
                    args = argsPtr;
                    iArgCount = -1;
                    DWORD_PTR arg = DWORD_PTR(argsPtr[-1]);

                    switch (c) 
                    {
                        case 'M':   // format as a method Desc
                            if (g_bDacBroken)
                            {
                                fprintf(file," (MethodDesc: %p)",arg);
                            }
                            else
                            {
                                if (!IsMethodDesc(arg)) 
                                {
                                    if (arg != 0) 
                                        fprintf(file, " (BAD Method)");
                                }
                                else 
                                {
                                    DacpMethodDescData MethodDescData;
                                    MethodDescData.Request(g_sos,(CLRDATA_ADDRESS)arg);

                                    static WCHAR wszNameBuffer[1024]; // should be large enough
                                    if (g_sos->GetMethodDescName(arg, 1024, wszNameBuffer, NULL) != S_OK)
                                    {
                                        wcscpy_s(wszNameBuffer, _countof(wszNameBuffer),L"UNKNOWN METHODDESC");
                                    }

                                    wcscpy_s(buff, capacity_buff, wszNameBuffer);
                                    fprintf(file, " (%S)", wszNameBuffer);
                                }
                            }
                            break;

                            // fall through
                        case 'T':       // format as a MethodTable
                            if (g_bDacBroken)
                            {
                                fprintf(file, "(MethodTable: %p)",arg);
                            }
                            else
                            {
                                if (arg & 3) 
                                {
                                    arg &= ~3;      // GC steals the lower bits for its own use during GC.  
                                    fprintf(file, " Low Bit(s) Set");
                                }
                                if (!IsMethodTable(arg))
                                {
                                    fprintf(file, " (BAD MethodTable)");
                                }
                                else 
                                {
                                    NameForMT_s (arg, g_mdName, mdNameLen);
                                    fprintf(file, " (%S)", g_mdName);
                                }
                            }
                            break;

                        case 'V': 
                            {   // format as a C vtable pointer 
                            char Symbol[1024];
                            ULONG64 Displacement;
                                HRESULT hr = g_ExtSymbols->GetNameByOffset(TO_CDADDR(arg), Symbol, 1024, NULL, &Displacement);
                            if (SUCCEEDED(hr) && Symbol[0] != '\0' && Displacement == 0) 
                                fprintf(file, " (%s)", Symbol);
                            else 
                                fprintf(file, " (Unknown VTable)");
                            }
                            break;
                        case 'K':
                            {   // format a frame in stack trace
                                char Symbol[1024];
                                ULONG64 Displacement;
                                HRESULT hr = g_ExtSymbols->GetNameByOffset (TO_CDADDR(arg), Symbol, 1024, NULL, &Displacement);
                                if (SUCCEEDED (hr) && Symbol[0] != '\0') 
                                {
                                    fprintf (file, " (%s", Symbol);
                                    if (Displacement)
                                    {
                                        fprintf (file, "+%#x", Displacement);
                                    }
                                    fprintf (file, ")");
                                }
                                else 
                                    fprintf (file, " (Unknown function)");    
                            }
                            break;
                        default:
                            format = ptr;   // Just print the character. 
                    }
                }
            }
            else if (*ptr == 's' || *ptr == 'h' && *(ptr+1) == 's' && ++ptr) 
            {
                HRESULT     hr;

                // need to _alloca, instead of declaring a local buffer
                // since we may have more than one %s in the format
                ULONG cbStrBuf = 256;
                char* strBuf = (char *)_alloca(cbStrBuf);
                
                hr = memCallBack->ReadVirtual(TO_CDADDR((char* )args[iArgCount]), strBuf, cbStrBuf, 0);
                if (hr != S_OK) 
                {
                    strcpy_s(strBuf, cbStrBuf, "(#Could not read address of string#)");           
                }

                args[iArgCount] = strBuf;                    
            }
            else if (*ptr == 'S' || *ptr == 'l' && *(ptr+1) == 's' && ++ptr) 
            {
                HRESULT     hr;

                // need to _alloca, instead of declaring a local buffer
                // since we may have more than one %s in the format
                ULONG cbWstrBuf = 256 * sizeof(WCHAR);
                WCHAR* wstrBuf = (WCHAR *)_alloca(cbWstrBuf);
                
                hr = memCallBack->ReadVirtual(TO_CDADDR((char* )args[iArgCount]), wstrBuf, cbWstrBuf, 0);
                if (hr != S_OK)
                {
                    wcscpy_s(wstrBuf, cbWstrBuf/sizeof(WCHAR), L"(#Could not read address of string#)");
                }

                args[iArgCount] = wstrBuf;
            }
            iArgCount++;
        }
    }
        // Print anything after the last special format instruction.
    fprintf(file, format, args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
    fprintf(file, "\n");
}

void __cdecl
vDoOut(BOOL bToConsole, FILE* file, PCSTR Format, ...)
{
    va_list Args;
    
    va_start(Args, Format);

    if (bToConsole)
    {
        g_ExtControl->OutputVaList(DEBUG_OUTPUT_NORMAL, Format, Args);
    }
    else
    {
        vfprintf(file, Format, Args);
    }

    va_end(Args);
}


/*********************************************************************************/
HRESULT StressLog::Dump(ULONG64 outProcLog, const char* fileName, struct IDebugDataSpaces* memCallBack) 
{
    ULONG64 g_hThisInst;
    BOOL    bDoGcHist = (fileName == NULL);

    // Fetch the circular buffer bookeeping data 
    StressLog inProcLog;
    HRESULT hr = memCallBack->ReadVirtual(UL64_TO_CDA(outProcLog), &inProcLog, sizeof(StressLog), 0);
    if (hr != S_OK) 
    {
        return hr;
    }
    if (inProcLog.logs.Load() == NULL || inProcLog.moduleOffset == 0) 
    {
        ExtOut ( "----- No thread logs in the image: The stress log was probably not initialized correctly. -----\n");
        return S_FALSE;
    }

    g_hThisInst = (ULONG64) inProcLog.moduleOffset;

    if (bDoGcHist)
    {
        GcHistClear();
    }
    else
    {
    ExtOut("Writing to file: %s\n", fileName);
    ExtOut("Stress log in module 0x%p\n", SOS_PTR(g_hThisInst));
    ExtOut("Stress log address = 0x%p\n", SOS_PTR(outProcLog));
    }
    // Fetch the circular buffers for each thread into the 'logs' list
    ThreadStressLog* logs = 0;

    CLRDATA_ADDRESS outProcPtr = TO_CDADDR(inProcLog.logs.Load());
    ThreadStressLog* inProcPtr;
    ThreadStressLog** logsPtr = &logs;
    int threadCtr = 0;
    unsigned __int64 lastTimeStamp = 0;// timestamp of last log entry
    while(outProcPtr != 0) {
        inProcPtr = new ThreadStressLog;
        hr = memCallBack->ReadVirtual(outProcPtr, inProcPtr, sizeof (*inProcPtr), 0);
        if (hr != S_OK || inProcPtr->chunkListHead == NULL)
        {
            delete inProcPtr;
            goto FREE_MEM;
        }

        CLRDATA_ADDRESS outProcListHead = TO_CDADDR(inProcPtr->chunkListHead);
        CLRDATA_ADDRESS outProcChunkPtr = outProcListHead;
        StressLogChunk ** chunksPtr = &inProcPtr->chunkListHead;
        StressLogChunk * inProcPrevChunkPtr = NULL;
        BOOL curPtrInitialized = FALSE;
        do
        {
            StressLogChunk * inProcChunkPtr = new StressLogChunk;
            hr = memCallBack->ReadVirtual (outProcChunkPtr, inProcChunkPtr, sizeof (*inProcChunkPtr), NULL);
            if (hr != S_OK || !inProcChunkPtr->IsValid ())
            {
                if (hr != S_OK)
                    ExtOut ("ReadVirtual failed with code hr = %x.\n", hr );
                else
                    ExtOut ("Invalid stress log chunk: %p", SOS_PTR(outProcChunkPtr));

                // Now cleanup
                delete inProcChunkPtr;
                // if this is the first time through, inProcPtr->chunkListHead may still contain
                // the out-of-process value for the chunk pointer.  NULL it to avoid AVs
                if (TO_CDADDR(inProcPtr->chunkListHead) == outProcListHead)
                   inProcPtr->chunkListHead = NULL; 
                delete inProcPtr;
                goto FREE_MEM;
            }

            if (!curPtrInitialized && outProcChunkPtr == TO_CDADDR(inProcPtr->curWriteChunk))
            {
                inProcPtr->curPtr = (StressMsg *)((BYTE *)inProcChunkPtr + ((BYTE *)inProcPtr->curPtr - (BYTE *)inProcPtr->curWriteChunk));
                inProcPtr->curWriteChunk = inProcChunkPtr;
                curPtrInitialized = TRUE;
            }
            
            outProcChunkPtr = TO_CDADDR(inProcChunkPtr->next);
            *chunksPtr = inProcChunkPtr;
            chunksPtr = &inProcChunkPtr->next;
            inProcChunkPtr->prev = inProcPrevChunkPtr;
            inProcPrevChunkPtr = inProcChunkPtr;

            if (outProcChunkPtr == outProcListHead)
            {
                inProcChunkPtr->next = inProcPtr->chunkListHead;
                inProcPtr->chunkListHead->prev = inProcChunkPtr;
                inProcPtr->chunkListTail = inProcChunkPtr;
            }           
        } while (outProcChunkPtr != outProcListHead);
        
        if (!curPtrInitialized)
        {
            delete inProcPtr;
            goto FREE_MEM;
        }

        // TODO: fix on 64 bit
        inProcPtr->Activate ();
        if (inProcPtr->readPtr->timeStamp > lastTimeStamp)
        {
            lastTimeStamp = inProcPtr->readPtr->timeStamp;
        }

        outProcPtr = TO_CDADDR(inProcPtr->next);
        *logsPtr = inProcPtr;
        logsPtr = &inProcPtr->next;
        threadCtr++;
    }

    FILE* file;
    file = NULL;
    if (!bDoGcHist && (fopen_s(&file, fileName, "w") != 0))
    {
        hr = GetLastError();
        goto FREE_MEM;
    }
    hr = S_FALSE;       // return false if there are no message to print to the log

    vDoOut(bDoGcHist, file, "STRESS LOG:\n"
              "    facilitiesToLog  = 0x%x\n"
              "    levelToLog       = %d\n"
              "    MaxLogSizePerThread = 0x%x (%d)\n"
              "    MaxTotalLogSize = 0x%x (%d)\n"
              "    CurrentTotalLogChunk = %d\n"
              "    ThreadsWithLogs  = %d\n",
        inProcLog.facilitiesToLog, inProcLog.levelToLog, inProcLog.MaxSizePerThread, inProcLog.MaxSizePerThread,
        inProcLog.MaxSizeTotal, inProcLog.MaxSizeTotal, inProcLog.totalChunk.Load(), threadCtr);

    FILETIME endTime;
    double totalSecs;
    totalSecs = ((double) (lastTimeStamp - inProcLog.startTimeStamp)) / inProcLog.tickFrequency;
    toInt64(endTime) = toInt64(inProcLog.startTime) + ((__int64) (totalSecs * 1.0E7));

    WCHAR timeBuff[64];
    vDoOut(bDoGcHist, file, "    Clock frequency  = %5.3f GHz\n", inProcLog.tickFrequency / 1.0E9);
    vDoOut(bDoGcHist, file, "    Start time         %S\n", getTime(&inProcLog.startTime, timeBuff, 64));
    vDoOut(bDoGcHist, file, "    Last message time  %S\n", getTime(&endTime, timeBuff, 64));
    vDoOut(bDoGcHist, file, "    Total elapsed time %5.3f sec\n", totalSecs);

    if (!bDoGcHist)
    {
    fprintf(file, "\nTHREAD  TIMESTAMP     FACILITY                              MESSAGE\n");
    fprintf(file, "  ID  (sec from start)\n");
    fprintf(file, "--------------------------------------------------------------------------------------\n");
    }
    char format[257];
    format[256] = format[0] = 0;
    void** args;
    unsigned msgCtr;
    msgCtr = 0;
    for (;;) 
    {
        ThreadStressLog* latestLog = logs->FindLatestThreadLog();

        if (IsInterrupt()) 
        {
            vDoOut(bDoGcHist, file, "----- Interrupted by user -----\n");
            break;
        }

        if (latestLog == 0) {
            break;
        }
        StressMsg* latestMsg = latestLog->readPtr;
        if (latestMsg->formatOffset != 0 && !latestLog->CompletedDump()) 
        {
            TADDR taFmt = (latestMsg->formatOffset) + TO_TADDR(g_hThisInst);
            hr = memCallBack->ReadVirtual(TO_CDADDR(taFmt), format, 256, 0);
            if (hr != S_OK) 
                strcpy_s(format, _countof(format), "Could not read address of format string");

            double deltaTime = ((double) (latestMsg->timeStamp - inProcLog.startTimeStamp)) / inProcLog.tickFrequency;
            if (bDoGcHist)
            {
                if (strcmp(format, ThreadStressLog::TaskSwitchMsg()) == 0)
                {
                    latestLog->threadId = (unsigned)(size_t)latestMsg->args[0];
                }
                GcHistAddLog(format, latestMsg);                                
            }
            else
            {
            if (strcmp(format, ThreadStressLog::TaskSwitchMsg()) == 0)
            {
                fprintf (file, "Task was switched from %x\n", (unsigned)(size_t)latestMsg->args[0]);
                latestLog->threadId = (unsigned)(size_t)latestMsg->args[0];
            }
                else 
                {
                args = latestMsg->args;
                formatOutput(memCallBack, file, format, latestLog->threadId, deltaTime, latestMsg->facility, args);
            }
            }
            msgCtr++;
        }

        latestLog->readPtr = latestLog->AdvanceRead();
        if (latestLog->CompletedDump())
        {
            latestLog->readPtr = NULL;
            if (!bDoGcHist)
            {
            fprintf(file, "------------ Last message from thread %x -----------\n", latestLog->threadId);
        }
        }

        if (msgCtr % 64 == 0) 
        {
            ExtOut(".");        // to indicate progress
            if (msgCtr % (64*64) == 0) 
                ExtOut("\n");   
        }
    }
    ExtOut("\n");

    vDoOut(bDoGcHist, file, "---------------------------- %d total entries ------------------------------------\n", msgCtr);
    if (!bDoGcHist)
    {
    fclose(file);
    }

FREE_MEM:
    // clean up the 'logs' list
    while (logs) {
        ThreadStressLog* temp = logs;
        logs = logs->next;
        delete temp;
    }

    return hr;
}

