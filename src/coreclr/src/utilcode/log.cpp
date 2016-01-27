// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Simple Logging Facility
//
#include "stdafx.h"

//
// Define LOGGING by default in a checked build. If you want to log in a free
// build, define logging independent of _DEBUG here and each place you want
// to use it.
//
#ifdef _DEBUG
#define LOGGING
#endif

#include "log.h"
#include "utilcode.h"

#ifdef LOGGING

#define DEFAULT_LOGFILE_NAME    "COMPLUS.LOG"

#define LOG_ENABLE_FILE_LOGGING         0x0001
#define LOG_ENABLE_FLUSH_FILE           0x0002
#define LOG_ENABLE_CONSOLE_LOGGING      0x0004
#define LOG_ENABLE_APPEND_FILE          0x0010
#define LOG_ENABLE_DEBUGGER_LOGGING     0x0020
#define LOG_ENABLE                      0x0040


static DWORD    LogFlags                    = 0;
static char     szLogFileName[MAX_LONGPATH+1]   = DEFAULT_LOGFILE_NAME;
static HANDLE   LogFileHandle               = INVALID_HANDLE_VALUE;
static MUTEX_COOKIE   LogFileMutex                = 0;
static DWORD    LogFacilityMask             = LF_ALL;
static DWORD    LogFacilityMask2            = 0;
static DWORD    LogVMLevel                  = LL_INFO100;
        // <TODO>@todo FIX should probably only display warnings and above by default</TODO>


VOID InitLogging()
{
    STATIC_CONTRACT_NOTHROW;
    
        // <TODO>FIX bit of a workaround for now, check for the log file in the
        // registry and if there, turn on file logging VPM</TODO>
    
    LogFlags |= REGUTIL::GetConfigFlag_DontUse_(CLRConfig::INTERNAL_LogEnable, LOG_ENABLE);    
    LogFacilityMask = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_LogFacility, LogFacilityMask) | LF_ALWAYS;
    LogVMLevel = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_LogLevel, LogVMLevel);
    LogFlags |= REGUTIL::GetConfigFlag_DontUse_(CLRConfig::INTERNAL_LogFileAppend, LOG_ENABLE_APPEND_FILE);
    LogFlags |= REGUTIL::GetConfigFlag_DontUse_(CLRConfig::INTERNAL_LogFlushFile,  LOG_ENABLE_FLUSH_FILE);
    LogFlags |= REGUTIL::GetConfigFlag_DontUse_(CLRConfig::INTERNAL_LogToDebugger, LOG_ENABLE_DEBUGGER_LOGGING);
    LogFlags |= REGUTIL::GetConfigFlag_DontUse_(CLRConfig::INTERNAL_LogToFile,     LOG_ENABLE_FILE_LOGGING);
    LogFlags |= REGUTIL::GetConfigFlag_DontUse_(CLRConfig::INTERNAL_LogToConsole,  LOG_ENABLE_CONSOLE_LOGGING);
    
    LogFacilityMask2 = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_LogFacility2, LogFacilityMask2) | LF_ALWAYS;

    LPWSTR fileName = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_LogFile);
    if (fileName != 0)
    {
        int ret;
        ret = WszWideCharToMultiByte(CP_ACP, 0, fileName, -1, szLogFileName, sizeof(szLogFileName)-1, NULL, NULL);
        _ASSERTE(ret != 0);
        delete fileName;
    }

    if (REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_LogWithPid, FALSE))
    {
        char szPid[20];
        sprintf_s(szPid, COUNTOF(szPid), ".%d", GetCurrentProcessId());
        strcat_s(szLogFileName, _countof(szLogFileName), szPid);
    }

    if ((LogFlags & LOG_ENABLE) &&
        (LogFlags & LOG_ENABLE_FILE_LOGGING) &&
        (LogFileHandle == INVALID_HANDLE_VALUE))
    {
        DWORD fdwCreate = (LogFlags & LOG_ENABLE_APPEND_FILE) ? OPEN_ALWAYS : CREATE_ALWAYS;
        LogFileHandle = CreateFileA(
            szLogFileName,
            GENERIC_WRITE,
            FILE_SHARE_READ,
            NULL,
            fdwCreate,
            FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN |  ((LogFlags & LOG_ENABLE_FLUSH_FILE) ? FILE_FLAG_WRITE_THROUGH : 0),
            NULL);

        if(0 == LogFileMutex)
        {
            LogFileMutex = ClrCreateMutex(
                NULL,
                FALSE,
                NULL);
            _ASSERTE(LogFileMutex != 0);
        }

            // Some other logging may be going on, try again with another file name
        if (LogFileHandle == INVALID_HANDLE_VALUE)
        {
            char* ptr = szLogFileName + strlen(szLogFileName) + 1;
            ptr[-1] = '.';
            ptr[0] = '0';
            ptr[1] = 0;

            for(int i = 0; i < 10; i++)
            {
                LogFileHandle = CreateFileA(
                    szLogFileName,
                    GENERIC_WRITE,
                    FILE_SHARE_READ,
                    NULL,
                    fdwCreate,
                    FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN |  ((LogFlags & LOG_ENABLE_FLUSH_FILE) ? FILE_FLAG_WRITE_THROUGH : 0),
                    NULL);
                if (LogFileHandle != INVALID_HANDLE_VALUE)
                    break;
                *ptr = *ptr + 1;
            }
            if (LogFileHandle == INVALID_HANDLE_VALUE) {
                DWORD       written;
                char buff[MAX_LONGPATH+60];
                strcpy(buff, "Could not open log file, logging to ");
                strcat_s(buff, _countof(buff), szLogFileName);
                // ARULM--Changed WriteConsoleA to WriteFile to be CE compat
                WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), buff, (DWORD)strlen(buff), &written, 0);
                }
        }
        if (LogFileHandle == INVALID_HANDLE_VALUE)
            UtilMessageBoxNonLocalized(NULL, W("Could not open log file"), W("CLR logging"), MB_OK | MB_ICONINFORMATION, FALSE, TRUE);
        if (LogFileHandle != INVALID_HANDLE_VALUE)
        {
            if (LogFlags & LOG_ENABLE_APPEND_FILE)
                SetFilePointer(LogFileHandle, 0, NULL, FILE_END);
            LogSpew( LF_ALWAYS, FATALERROR, "************************ New Output *****************\n" );
        }
    }
}

VOID EnterLogLock()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    // We don't care about violating CANNOT_TAKE_LOCK in debug-only builds, and it's
    // rather hard to care about this, as we LOG all over the place.
    CONTRACT_VIOLATION(TakesLockViolation);

    if(LogFileMutex != 0)
    {
        DWORD status;
        status = ClrWaitForMutex(LogFileMutex, INFINITE, FALSE);
        _ASSERTE(WAIT_OBJECT_0 == status);
    }
}

VOID LeaveLogLock()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if(LogFileMutex != 0)
    {
        BOOL success;
        success = ClrReleaseMutex(LogFileMutex);
        _ASSERTE(success);
    }
}

static bool bLoggingInitialized = false;
VOID InitializeLogging()
{
    STATIC_CONTRACT_NOTHROW;

    if (bLoggingInitialized)
        return;
    bLoggingInitialized = true;

    InitLogging();      // You can call this in the debugger to fetch new settings
}

VOID FlushLogging() {
    STATIC_CONTRACT_NOTHROW;

    if (LogFileHandle != INVALID_HANDLE_VALUE)
    {
        // We must take the lock, as an OS deadlock can occur between
        // FlushFileBuffers and WriteFile.
        EnterLogLock();        
        FlushFileBuffers( LogFileHandle );
        LeaveLogLock();        
    }
}

VOID ShutdownLogging()
{
    STATIC_CONTRACT_NOTHROW;

    if (LogFileHandle != INVALID_HANDLE_VALUE) {
        LogSpew( LF_ALWAYS, FATALERROR, "Logging shutting down\n");
        CloseHandle( LogFileHandle );
    }
    LogFileHandle = INVALID_HANDLE_VALUE;
    bLoggingInitialized = false;
}


bool LoggingEnabled()
{
    STATIC_CONTRACT_LEAF;

    return ((LogFlags & LOG_ENABLE) != 0);
}


bool LoggingOn(DWORD facility, DWORD level) {
    STATIC_CONTRACT_LEAF;

    _ASSERTE(LogFacilityMask & LF_ALWAYS); // LF_ALWAYS should always be enabled

    return((LogFlags & LOG_ENABLE) &&
           level <= LogVMLevel &&
           (facility & LogFacilityMask));
}

bool Logging2On(DWORD facility2, DWORD level) {
    STATIC_CONTRACT_LEAF;

    _ASSERTE(LogFacilityMask2 & LF_ALWAYS); // LF_ALWAYS should always be enabled

    return((LogFlags & LOG_ENABLE) &&
           level <= LogVMLevel &&
           (facility2 & LogFacilityMask2));
}

//
// Don't use me directly, use the macros in log.h
//
VOID LogSpewValist(DWORD facility, DWORD level, const char *fmt, va_list args)
{
    SCAN_IGNORE_FAULT;  // calls to new (nothrow) in logging code are OK
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    
    if (!LoggingOn(facility, level))
        return;

    DEBUG_ONLY_FUNCTION;

    LogSpewAlwaysValist(fmt, args);
}


VOID LogSpew2Valist(DWORD facility2, DWORD level, const char *fmt, va_list args)
{
    SCAN_IGNORE_FAULT;  // calls to new (nothrow) in logging code are OK
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    
    if (!Logging2On(facility2, level))
        return;

    DEBUG_ONLY_FUNCTION;

    LogSpewAlwaysValist(fmt, args);
}


VOID LogSpewAlwaysValist(const char *fmt, va_list args)
{
    SCAN_IGNORE_FAULT;  // calls to new (nothrow) in logging code are OK
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    
    DEBUG_ONLY_FUNCTION;

    // We can't do heap allocations at all.  The current thread may have
    // suspended another thread, and the suspended thread may be inside of the
    // heap lock.
    //
    // (Some historical comments:)
    //
    // We must operate with a very small stack (in case we're logging durring
    // a stack overflow)
    //
    // We're going to bypass our debug memory allocator and just allocate memory from
    // the process heap. Why? Because our debug memory allocator will log out of memory
    // conditions. If we're low on memory, and we try to log an out of memory condition, and we try
    // and allocate memory again using the debug allocator, we could (and probably will) hit
    // another low memory condition, try to log it, and we spin indefinately until we hit a stack overflow.

    const int BUFFERSIZE = 1000;
    static char rgchBuffer[BUFFERSIZE];

    EnterLogLock();

    char *  pBuffer      = &rgchBuffer[0];
    DWORD       buflen       = 0;
    DWORD       written;

    static bool needsPrefix = true;

    if (needsPrefix)
        buflen = sprintf_s(pBuffer, COUNTOF(rgchBuffer), "TID %04x: ", GetCurrentThreadId());

    needsPrefix = (fmt[strlen(fmt)-1] == '\n');

    int cCountWritten = _vsnprintf(&pBuffer[buflen], BUFFERSIZE-buflen, fmt, args );
    pBuffer[BUFFERSIZE-1] = 0;
    if (cCountWritten < 0) {
        buflen = BUFFERSIZE - 1;
    } else {
        buflen += cCountWritten;
    }

    // Its a little late for this, but at least you wont continue
    // trashing your program...
    _ASSERTE((buflen < (DWORD) BUFFERSIZE) && "Log text is too long!") ;

#if !PLATFORM_UNIX
    //convert NL's to CR NL to fixup notepad
    const int BUFFERSIZE2 = BUFFERSIZE + 500;
    char rgchBuffer2[BUFFERSIZE2];
    char * pBuffer2 = &rgchBuffer2[0];

    char *d = pBuffer2;
    for (char *p = pBuffer; *p != '\0'; p++)
    {
        if (*p == '\n') {
            _ASSERTE(d < pBuffer2 + BUFFERSIZE2);
            *(d++) = '\r';
        }

        _ASSERTE(d < pBuffer2 + BUFFERSIZE2);
        *(d++) = *p;
    }
    *d = 0;

    buflen = (DWORD)(d - pBuffer2);
    pBuffer = pBuffer2;
#endif // PLATFORM_UNIX

    if (LogFlags & LOG_ENABLE_FILE_LOGGING && LogFileHandle != INVALID_HANDLE_VALUE)
    {
        WriteFile(LogFileHandle, pBuffer, buflen, &written, NULL);
        if (LogFlags & LOG_ENABLE_FLUSH_FILE) {
            FlushFileBuffers( LogFileHandle );
        }
    }

    if (LogFlags & LOG_ENABLE_CONSOLE_LOGGING)
    {
        WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), pBuffer, buflen, &written, 0);
        //<TODO>@TODO ...Unnecessary to flush console?</TODO>
        if (LogFlags & LOG_ENABLE_FLUSH_FILE)
            FlushFileBuffers( GetStdHandle(STD_OUTPUT_HANDLE) );
    }

    if (LogFlags & LOG_ENABLE_DEBUGGER_LOGGING)
    {
        OutputDebugStringA(pBuffer);
    }

    LeaveLogLock();
}

VOID LogSpew(DWORD facility, DWORD level, const char *fmt, ... )
{
    STATIC_CONTRACT_WRAPPER;
    
    ENTER_SO_NOT_MAINLINE_CODE;
    
#ifdef SELF_NO_HOST
    if (TRUE)
#else //!SELF_NO_HOST
    if (CanThisThreadCallIntoHost())
#endif //!SELF_NO_HOST
    {
        va_list     args;
        va_start(args, fmt);
        LogSpewValist (facility, level, fmt, args);
        va_end(args);
    }
    else
    {
        // Cannot acquire the required lock, as this would call back
        // into the host.  Eat the log message.
    }
    
    LEAVE_SO_NOT_MAINLINE_CODE;
}

VOID LogSpew2(DWORD facility2, DWORD level, const char *fmt, ... )
{
    STATIC_CONTRACT_WRAPPER;
    
    ENTER_SO_NOT_MAINLINE_CODE;
    
#ifdef SELF_NO_HOST
    if (TRUE)
#else //!SELF_NO_HOST
    if (CanThisThreadCallIntoHost())
#endif //!SELF_NO_HOST
    {
        va_list     args;
        va_start(args, fmt);
        LogSpew2Valist(facility2, level, fmt, args);
        va_end(args);
    }
    else
    {
        // Cannot acquire the required lock, as this would call back
        // into the host.  Eat the log message.
    }
    
    LEAVE_SO_NOT_MAINLINE_CODE;
}

VOID LogSpewAlways (const char *fmt, ... )
{
    STATIC_CONTRACT_WRAPPER;
    
    ENTER_SO_NOT_MAINLINE_CODE;
    
#ifdef SELF_NO_HOST
    if (TRUE)
#else //!SELF_NO_HOST
    if (CanThisThreadCallIntoHost())
#endif //!SELF_NO_HOST
    {
        va_list     args;
        va_start(args, fmt);
        LogSpewValist (LF_ALWAYS, LL_ALWAYS, fmt, args);
        va_end(args);
    }
    else
    {
        // Cannot acquire the required lock, as this would call back
        // into the host.  Eat the log message.
    }
    
    LEAVE_SO_NOT_MAINLINE_CODE;
}

#endif // LOGGING

