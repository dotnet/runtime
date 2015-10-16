//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
  PALShutdown

Utility function to force PAL to shutdown state

--*/
void PALShutdown( void );

/*++
Function:
  PALCommonCleanup

Utility function to free any resource used by the PAL. 

Parameters :
    full_cleanup:  TRUE: cleanup only what's needed and leave the rest 
                         to the OS process cleanup
                   FALSE: full cleanup 
--*/
void PALCommonCleanup( BOOL full_cleanup );

extern Volatile<INT> init_count;

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

Take the initializaiton critical section (init_critsec). necessary to serialize 
TerminateProcess along with PAL_Terminate and PAL_Initialize

(no parameters)

Return value :
    TRUE if critical section existed (and was acquired)
    FALSE if critical section doens't exist yet
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
