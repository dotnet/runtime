// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Logging Facility
//


// Logging Subsystems


#ifndef __LOG_H__
#define __LOG_H__


#define DEFINE_LOG_FACILITY(logname, value)  logname = value,

enum {
#include "loglf.h"
    LF_ALWAYS        = 0x80000000, // Log message irrepespective of LogFacility (if the level matches)
    LF_ALL           = 0xFFFFFFFF, // Used only to mask bits. Never use as LOG((LF_ALL, ...))

    // LogFacility2: all 32-bit of LogFacility are used, need a 2nd DWORD for more facilities
    LF2_MULTICOREJIT = 0x00000001  // Multicore JIT
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
#define LL_ALWAYS   	0		// impossible to turn off (log level never negative)


#define INFO5       LL_INFO10
#define INFO4       LL_INFO100
#define INFO3       LL_INFO1000
#define INFO2       LL_INFO10000
#define INFO1       LL_INFO100000
#define WARNING     0
#define ERROR       0
#define FATALERROR  0

#ifndef LOGGING

#define LOG(x)
#define LOG2(x)

#define InitializeLogging()
#define InitLogging()
#define ShutdownLogging()
#define FlushLogging()
#define LoggingOn(facility, level) 0
#define Logging2On(facility, level) 0
#define EnterLogLock()
#define LeaveLogLock()

#else

extern VOID InitializeLogging();
extern VOID InitLogging();
extern VOID ShutdownLogging();
extern VOID FlushLogging();

extern VOID LogSpew(DWORD facility, DWORD level, const char *fmt, ... );
extern VOID LogSpewValist(DWORD facility, DWORD level, const char *fmt, va_list args);

extern VOID LogSpew2(DWORD facility2, DWORD level, const char *fmt, ... );
extern VOID LogSpew2Valist(DWORD facility2, DWORD level, const char *fmt, va_list args);

extern VOID LogSpewAlwaysValist(const char *fmt, va_list args);
extern VOID LogSpewAlways (const char *fmt, ... );
extern VOID EnterLogLock();
extern VOID LeaveLogLock();

VOID AddLoggingFacility( DWORD facility );
VOID SetLoggingLevel( DWORD level );
bool LoggingEnabled();
bool LoggingOn(DWORD facility, DWORD level);
bool Logging2On(DWORD facility, DWORD level);

#define LOG(x)      do { if (LoggingEnabled()) { LogSpew x; } } while (0)

#define LOG2(x)     do { if (LoggingEnabled()) { LogSpew2 x; } } while (0)

#endif

#ifdef __cplusplus
#include "stresslog.h"		// special logging for retail code
#endif

#endif //__LOG_H__
