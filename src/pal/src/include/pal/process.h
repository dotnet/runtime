//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

// The process ID of this process, so we can avoid excessive calls to getpid().
extern DWORD gPID;
extern LPWSTR pAppDir;

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
BOOL  PROCCreateInitialProcess(LPWSTR lpwstrCmdLine, LPWSTR lpwstrFullPath);

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
Function:
  PROCCleanupProcess
  
  Do all cleanup work for TerminateProcess, but don't terminate the process.
  If bTerminateUnconditionally is TRUE, we exit as quickly as possible.

(no return value)
--*/
void PROCCleanupProcess(BOOL bTerminateUnconditionally);

/*++
Function:
  InitializeFlushProcessWriteBuffers

Abstract
  This function initializes data structures needed for the FlushProcessWriteBuffers
Return
  TRUE if it succeeded, FALSE otherwise
--*/
BOOL InitializeFlushProcessWriteBuffers();

#if HAVE_MACH_EXCEPTIONS
/*++
Function:
  PROCThreadFromMachPort
  
  Given a Mach thread port, return the CPalThread associated with it.

Return
    CPalThread*
--*/
CorUnix::CPalThread *PROCThreadFromMachPort(mach_port_t hThread);
#endif // HAVE_MACH_EXCEPTIONS

#ifdef __cplusplus
}
#endif // __cplusplus

#endif //PAL_PROCESS_H_

