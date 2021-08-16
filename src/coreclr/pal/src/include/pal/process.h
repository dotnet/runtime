// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/process.h

Abstract:

    Miscellaneous process related functions.

Revision History:



--*/

#ifndef _PAL_PROCESS_H_
#define _PAL_PROCESS_H_

#include "pal/palinternal.h"
#include "pal/stackstring.hpp"

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/* thread ID of thread that has initiated an ExitProcess (or TerminateProcess).
   this is to make sure only one thread cleans up the PAL, and also to prevent
   calls to CreateThread from succeeding once shutdown has started
   [defined in process.c]
*/
extern Volatile<LONG> terminator;

// The process and session ID of this process, so we can avoid excessive calls to getpid() and getsid().
extern DWORD gPID;
extern DWORD gSID;

extern LPWSTR pAppDir;

// The Mac sandbox application group ID (if exists) and container (shared) path
#ifdef __APPLE__
extern LPCSTR gApplicationGroupId;
extern int gApplicationGroupIdLength;
#endif // __APPLE__
extern PathCharString *gSharedFilesPath;

/*++
Function:
  PROCGetProcessIDFromHandle

Abstract
  Return the process ID from a process handle
--*/
DWORD PROCGetProcessIDFromHandle(HANDLE hProcess);

/*++
Function:
  PROCCreateInitialProcess

Abstract
  Initialize all the structures for the initial process.

Parameter
  lpwstrCmdLine:   Command line.
  lpwstrFullPath : Full path to executable

Return
  TRUE: if successful
  FALSE: otherwise

Notes :
    This function takes ownership of lpwstrCmdLine, but not of lpwstrFullPath
--*/
BOOL PROCCreateInitialProcess(LPWSTR lpwstrCmdLine, LPWSTR lpwstrFullPath);

/*++
Function:
  PROCCleanupInitialProcess

Abstract
  Cleanup all the structures for the initial process.

Parameter
  VOID

Return
  VOID

--*/
VOID PROCCleanupInitialProcess(VOID);

#if USE_SYSV_SEMAPHORES
/*++
Function:
  PROCCleanupThreadSemIds(VOID);

Abstract
  Cleanup SysV semaphore ids for all threads.

(no parameters, no return value)
--*/
VOID PROCCleanupThreadSemIds(VOID);
#endif

/*++
Function:
  PROCProcessLock

Abstract
  Enter the critical section associated to the current process
--*/
VOID PROCProcessLock(VOID);


/*++
Function:
  PROCProcessUnlock

Abstract
  Leave the critical section associated to the current process
--*/
VOID PROCProcessUnlock(VOID);

/*++
Function
  PROCAbortInitialize()

Abstract
  Initialize the process abort crash dump program file path and
  name. Doing all of this ahead of time so nothing is allocated
  or copied in PROCAbort/signal handler.

Return
  TRUE - succeeds, FALSE - fails

--*/
BOOL PROCAbortInitialize();

/*++
Function:
  PROCAbort()

  Aborts the process after calling the shutdown cleanup handler. This function
  should be called instead of calling abort() directly.

Parameters:
  signal - POSIX signal number

  Does not return
--*/
PAL_NORETURN
VOID PROCAbort(int signal = SIGABRT);

/*++
Function:
  PROCNotifyProcessShutdown

  Calls the abort handler to do any shutdown cleanup. Call be
  called from the unhandled native exception handler.

(no return value)
--*/
VOID PROCNotifyProcessShutdown(bool isExecutingOnAltStack = false);

/*++
Function:
  PROCCreateCrashDumpIfEnabled

  Creates crash dump of the process (if enabled). Can be
  called from the unhandled native exception handler.

Parameters:
  signal - POSIX signal number

(no return value)
--*/
VOID PROCCreateCrashDumpIfEnabled(int signal);

/*++
Function:
  InitializeFlushProcessWriteBuffers

Abstract
  This function initializes data structures needed for the FlushProcessWriteBuffers
Return
  TRUE if it succeeded, FALSE otherwise
--*/
BOOL InitializeFlushProcessWriteBuffers();

#ifdef __cplusplus
}
#endif // __cplusplus

#endif //PAL_PROCESS_H_

