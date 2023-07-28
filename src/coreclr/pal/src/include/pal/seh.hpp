// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    Send the PAL exception to any handler registered.

Parameters:
    PAL_SEHException* exception

Return value:
    Returns TRUE if the exception happened in managed code and the execution should
    continue (with possibly modified context).
    Returns FALSE if the exception happened in managed code and it was not handled.
    In case the exception was handled by calling a catch handler, it doesn't return at all.
--*/
BOOL
SEHProcessException(PAL_SEHException* exception);

/*++
Function:
    AllocateExceptionRecords

Parameters:
    exceptionRecord - output pointer to the allocated Windows exception record
    contextRecord - output pointer to the allocated Windows context record
--*/
VOID
AllocateExceptionRecords(EXCEPTION_RECORD** exceptionRecord, CONTEXT** contextRecord);

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

extern "C"
{

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

}

// Offset of the local variable containing pointer to windows style context in the common_signal_handler / PAL_DispatchException function.
// This offset is relative to the frame pointer.
extern int g_hardware_exception_context_locvar_offset;
// Offset of the local variable containing pointer to windows style context in the inject_activation_handler.
// This offset is relative to the frame pointer.
extern int g_inject_activation_context_locvar_offset;

#endif /* _PAL_SEH_HPP_ */

