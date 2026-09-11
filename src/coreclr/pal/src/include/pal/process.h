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

#include <signal.h>
#if defined(TARGET_WASI)
#include "pal/wasi/pal_wasi_missing.h"
#endif

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

// The Mac sandbox application group ID (if exists) and container (shared) path
#ifdef __APPLE__
extern LPCSTR gApplicationGroupId;
extern int gApplicationGroupIdLength;
#endif // __APPLE__
extern PathCharString *gSharedFilesPath;

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
  siginfo - POSIX signal info
  context - signal context or nullptr

  Does not return
--*/
#if !defined(HOST_ARM)  // PAL_NORETURN produces broken unwinding information for this method
                        // making crash dumps impossible to analyze
PAL_NORETURN
#endif
VOID PROCAbort(int signal = SIGABRT, siginfo_t* siginfo = nullptr, void* context = nullptr);

/*++
Function:
  PROCNotifyProcessShutdown

  Calls the abort handler to do any shutdown cleanup. Call be
  called from the unhandled native exception handler.

(no return value)
--*/
VOID PROCNotifyProcessShutdown(bool isExecutingOnAltStack = false);

// Controls how concurrent crash diagnostics -- both the out-of-proc
// crash dump and the in-proc crash report -- are serialized across threads.
// Except for CrashDumpSerialize_None, only one thread (the "winner") ever
// generates diagnostics at a time; the mode names the action a contending
// thread takes when it finds the gate already held.
enum CrashDumpSerializeMode
{
    // Don't serialize at all: no gate, every thread generates crash diagnostics
    // concurrently.
    CrashDumpSerialize_None,

    // On contention, don't wait: if another thread is already generating crash
    // diagnostics, return immediately without generating a (duplicate) dump or
    // report. Otherwise generate it and continue. The gate is re-armed
    // afterwards so a later crash can generate diagnostics again.
    CrashDumpSerialize_NoWait,

    // On contention, wait indefinitely: the winner generates the crash dump or
    // report and then expects to terminate the process; a contending thread waits
    // indefinitely until that happens.
    CrashDumpSerialize_WaitInfinite
};

/*++
Function:
  PROCCreateCrashDumpIfEnabled

  Creates crash dump of the process (if enabled). Can be
  called from the unhandled native exception handler.

Parameters:
  signal - POSIX signal number
  siginfo - POSIX signal info or nullptr
  context - signal context or nullptr
  serializeMode - how to serialize concurrent crash diagnostics

(no return value)
--*/
VOID PROCCreateCrashDumpIfEnabled(int signal, siginfo_t* siginfo, void* context, CrashDumpSerializeMode serializeMode);

/*++
Function:
  PROCLogManagedCallstackForSignal

  Invokes the registered callback to log the managed callstack for a signal.
  Used by Android since CreateDump is not supported there.

Parameters:
  signal - POSIX signal number

(no return value)
--*/
VOID PROCLogManagedCallstackForSignal(int signal);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif //PAL_PROCESS_H_
