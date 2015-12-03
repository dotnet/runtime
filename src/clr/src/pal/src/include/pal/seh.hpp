//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    include/pal/seh.hpp

Abstract:
    Header file for public Structured Exception Handling stuff



--*/

#ifndef _PAL_SEH_HPP_
#define _PAL_SEH_HPP_

#include "config.h"
#include "pal/palinternal.h"
#include "pal/corunix.hpp"

// Uncomment this define to turn off the signal handling thread.
// #define DO_NOT_USE_SIGNAL_HANDLING_THREAD

/*++
Function :
    SEHInitialize

    Initialize all SEH-related stuff (signals, etc)

Parameters:
    CPalThread * pthrCurrent : reference to the current thread.
    flags : PAL initialize flags

Return value:
    TRUE  if SEH support initialization succeeded,
    FALSE otherwise

--*/
BOOL 
SEHInitialize(CorUnix::CPalThread *pthrCurrent, DWORD flags);

/*++
Function :
    SEHCleanup

    Clean up SEH-related stuff(signals, etc)

Parameters:
    None

    (no return value)
--*/
VOID 
SEHCleanup();

/*++
Function:
    SEHProcessException

    Build the PAL exception and sent it to any handler registered.

Parameters:
    None

Return value:
    Does not return
--*/
VOID 
SEHProcessException(PEXCEPTION_POINTERS pointers);

#if !HAVE_MACH_EXCEPTIONS
// TODO: Implement for Mach exceptions.  Not in CoreCLR surface area.
/*++
Function :
    SEHHandleControlEvent

    handle Control-C and Control-Break events (call handler routines, 
    notify debugger)

Parameters :
    DWORD event : event that occurred
    LPVOID eip  : instruction pointer when exception occurred                                 

(no return value)

Notes :
    Handlers are called on a last-installed, first called basis, until a 
    handler returns TRUE. If no handler returns TRUE (or no hanlder is 
    installed), the default behavior is to call ExitProcess
--*/
void SEHHandleControlEvent(DWORD event, LPVOID eip);
#endif // !HAVE_MACH_EXCEPTIONS

#if !HAVE_MACH_EXCEPTIONS
/*++
Function :
    SEHSetSafeState

    specify whether the current thread is in a state where exception handling 
    of signals can be done safely

Parameters:
    CPalThread * pthrCurrent : reference to the current thread.
    BOOL state : TRUE if the thread is safe, FALSE otherwise

(no return value)
--*/
void SEHSetSafeState(CorUnix::CPalThread *pthrCurrent, BOOL state);

/*++
Function :
    SEHGetSafeState

    determine whether the current thread is in a state where exception handling 
    of signals can be done safely

Parameters:
    CPalThread * pthrCurrent : reference to the current thread.

Return value :
    TRUE if the thread is in a safe state, FALSE otherwise
--*/
BOOL SEHGetSafeState(CorUnix::CPalThread *pthrCurrent);
#endif // !HAVE_MACH_EXCEPTIONS

extern "C"
{

#ifdef FEATURE_PAL_SXS
/*++
Function :
    SEHEnable

    Enable SEH-related stuff on this thread

Parameters:
    CPalThread * pthrCurrent : reference to the current thread.

Return value :
    ERROR_SUCCESS, if enabling succeeded
    an error code, otherwise
--*/
CorUnix::PAL_ERROR SEHEnable(CorUnix::CPalThread *pthrCurrent);

/*++
Function :
    SEHDisable

    Disable SEH-related stuff on this thread

Parameters:
    CPalThread * pthrCurrent : reference to the current thread.

Return value :
    ERROR_SUCCESS, if enabling succeeded
    an error code, otherwise
--*/
CorUnix::PAL_ERROR SEHDisable(CorUnix::CPalThread *pthrCurrent);

#endif // FEATURE_PAL_SXS

}

#endif /* _PAL_SEH_HPP_ */

