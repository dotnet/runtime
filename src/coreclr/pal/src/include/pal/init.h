// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/init.h

Abstract:
    Header file for PAL init utility functions. Those functions
    are only use by the PAL itself.

Revision History:



--*/

#ifndef _PAL_INIT_H_
#define _PAL_INIT_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*++
Function:
  PALCommonCleanup

Utility function to prepare for shutdown.

--*/
void PALCommonCleanup();

extern Volatile<INT> init_count;

extern SIZE_T g_defaultStackSize;

extern BOOL g_useDefaultBaseAddr;

/*++
MACRO:
  PALIsInitialized

Returns TRUE if the PAL is in an initialized state
(#calls to PAL_Initialize > #calls to PAL_Terminate)

Warning : this will only report the PAL's state at the moment it is called.
If it is necessary to ensure the PAL remains initialized (or not) while doing
some work, the Initialization lock (PALInitLock()) should be held.
--*/
#define PALIsInitialized() (0 < init_count)

/*++
Function:
  PALIsThreadDataInitialized

Returns TRUE if startup has reached a point where thread data is available
--*/
BOOL
PALIsThreadDataInitialized();

/*++
Function:
  PALIsShuttingDown

Returns TRUE if the some thread has declared intent to shutdown
--*/
BOOL
PALIsShuttingDown();

/*++
Function:
  PALSetShutdownIntent

Delcares intent to shutdown
--*/
void
PALSetShutdownIntent();

/*++
Function:
  PALInitLock

Take the initialization critical section (init_critsec). necessary to serialize
TerminateProcess along with PAL_Terminate and PAL_Initialize

(no parameters)

Return value :
    TRUE if critical section existed (and was acquired)
    FALSE if critical section doesn't exist yet
--*/
BOOL PALInitLock(void);

/*++
Function:
  PALInitUnlock

Release the initialization critical section (init_critsec).

(no parameters, no return value)
--*/
void PALInitUnlock(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_INIT_H_ */
