//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    seh.cpp

Abstract:

    Implementation of exception API functions.



--*/

#include "pal/thread.hpp"
#include "pal/handleapi.hpp"
#include "pal/seh.hpp"
#include "pal/dbgmsg.h"
#include "pal/critsect.h"
#include "pal/debug.h"
#include "pal/init.h"
#include "pal/process.h"
#include "pal/malloc.hpp"
#include "signal.hpp"

#if HAVE_ALLOCA_H
#include "alloca.h"
#endif

#if HAVE_MACH_EXCEPTIONS
#include "machexception.h"
#else
#include <signal.h>
#endif

#include <string.h>
#include <unistd.h>
#include <pthread.h>
#include <stdlib.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT);

/* Constant and type definitions **********************************************/

/* Bit 28 of exception codes is reserved. */
const UINT RESERVED_SEH_BIT = 0x800000;

/* Internal variables definitions **********************************************/

PHARDWARE_EXCEPTION_HANDLER g_hardwareExceptionHandler = NULL;

/* Internal function declarations *********************************************/

BOOL SEHInitializeConsole();

#if !HAVE_MACH_EXCEPTIONS
PAL_ERROR
StartExternalSignalHandlerThread(
    CPalThread *pthr);
#endif // !HAVE_MACH_EXCEPTIONS

/* Internal function definitions **********************************************/

/*++
Function :
    SEHInitialize

    Initialize all SEH-related stuff (signals, etc)

Parameters :
    CPalThread * pthrCurrent : reference to the current thread.
    PAL initialize flags

Return value :
    TRUE  if SEH support initialization succeeded
    FALSE otherwise
--*/
BOOL 
SEHInitialize (CPalThread *pthrCurrent, DWORD flags)
{
    BOOL bRet = FALSE;

    if (!SEHInitializeConsole())
    {
        ERROR("SEHInitializeConsole failed!\n");
        SEHCleanup();
        goto SEHInitializeExit;
    }

#if !HAVE_MACH_EXCEPTIONS
    if (!SEHInitializeSignals())
    {
        ERROR("SEHInitializeSignals failed!\n");
        SEHCleanup();
        goto SEHInitializeExit;
    }

    if (flags & PAL_INITIALIZE_SIGNAL_THREAD)
    {
        PAL_ERROR palError = StartExternalSignalHandlerThread(pthrCurrent);
        if (NO_ERROR != palError)
        {
            ERROR("StartExternalSignalHandlerThread returned %d\n", palError);
            SEHCleanup();
            goto SEHInitializeExit;
        }
    }
#endif
    bRet = TRUE;

SEHInitializeExit:
    return bRet;
}

/*++
Function :
    SEHCleanup

    Undo work done by SEHInitialize

Parameters :
    None

    (no return value)
    
--*/
VOID 
SEHCleanup()
{
    TRACE("Cleaning up SEH\n");

#if HAVE_MACH_EXCEPTIONS
    SEHCleanupExceptionPort();
#else
    SEHCleanupSignals();
#endif
}

/*++
Function:
    PAL_SetHardwareExceptionHandler

    Register a hardware exception handler.

Parameters:
    handler - exception handler

Return value:
    None
--*/
VOID
PALAPI 
PAL_SetHardwareExceptionHandler(
    IN PHARDWARE_EXCEPTION_HANDLER exceptionHandler)

{
    //TRACE("Hardware exception installed: %p\n", exceptionHandler);
    g_hardwareExceptionHandler = exceptionHandler;
}

/*++
Function:
    SEHProcessException

    Build the PAL exception and sent it to any handler registered.

Parameters:
    PEXCEPTION_POINTERS pointers

Return value:
    Returns only if the exception is unhandled
--*/
VOID
SEHProcessException(PEXCEPTION_POINTERS pointers)
{
    PAL_SEHException exception(pointers->ExceptionRecord, pointers->ContextRecord);

    if (g_hardwareExceptionHandler != NULL)
    {
        g_hardwareExceptionHandler(&exception);
    }

    if (CatchHardwareExceptionHolder::IsEnabled())
    {
        throw exception;
    }

    TRACE("Unhandled hardware exception %08x at %p\n", 
        pointers->ExceptionRecord->ExceptionCode, pointers->ExceptionRecord->ExceptionAddress);
}

/*++
Function :
    SEHEnable

    Enable SEH-related stuff on this thread

Parameters:
    CPalThread * pthrCurrent : reference to the current thread.

Return value :
    TRUE  if enabling succeeded
    FALSE otherwise
--*/
extern "C"
PAL_ERROR SEHEnable(CPalThread *pthrCurrent)
{
#if HAVE_MACH_EXCEPTIONS
    return pthrCurrent->EnableMachExceptions();
#elif __LINUX__ || defined(__FreeBSD__)
    // TODO: This needs to be implemented. Cannot put an ASSERT here
    // because it will make other parts of PAL fail.
    return NO_ERROR;
#else// HAVE_MACH_EXCEPTIONS
#error not yet implemented
#endif // HAVE_MACH_EXCEPTIONS
}

/*++
Function :
    SEHDisable

    Disable SEH-related stuff on this thread

Parameters:
    CPalThread * pthrCurrent : reference to the current thread.

Return value :
    TRUE  if enabling succeeded
    FALSE otherwise
--*/
extern "C"
PAL_ERROR SEHDisable(CPalThread *pthrCurrent)
{
#if HAVE_MACH_EXCEPTIONS
    return pthrCurrent->DisableMachExceptions();
    // TODO: This needs to be implemented. Cannot put an ASSERT here
    // because it will make other parts of PAL fail.
#elif __LINUX__ || defined(__FreeBSD__)
    return NO_ERROR;
#else // HAVE_MACH_EXCEPTIONS
#error not yet implemented
#endif // HAVE_MACH_EXCEPTIONS
}

/*++

  CatchHardwareExceptionHolder implementation

--*/

#ifdef __llvm__
__thread 
#else // __llvm__
__declspec(thread)
#endif // !__llvm__
int t_holderCount = 0;

CatchHardwareExceptionHolder::CatchHardwareExceptionHolder()
{
    ++t_holderCount;
}

CatchHardwareExceptionHolder::~CatchHardwareExceptionHolder()
{
    --t_holderCount;
}

bool CatchHardwareExceptionHolder::IsEnabled()
{
    return t_holderCount > 0;
}

/*++

  NativeExceptionHolderBase implementation

--*/

#ifdef __llvm__
__thread 
#else // __llvm__
__declspec(thread)
#endif // !__llvm__
static NativeExceptionHolderBase *t_nativeExceptionHolderHead = nullptr;

NativeExceptionHolderBase::NativeExceptionHolderBase()
    : CatchHardwareExceptionHolder()
{
    m_head = nullptr;
    m_next = nullptr;
}

NativeExceptionHolderBase::~NativeExceptionHolderBase()
{
    // Only destroy if Push was called
    if (m_head != nullptr)
    {
        *m_head = m_next;
        m_head = nullptr;
        m_next = nullptr;
    }
}

void 
NativeExceptionHolderBase::Push()
{
    NativeExceptionHolderBase **head = &t_nativeExceptionHolderHead;
    m_head = head;
    m_next = *head;
    *head = this;
}

NativeExceptionHolderBase *
NativeExceptionHolderBase::FindNextHolder(void *stackLowAddress, void *stackHighAddress)
{
    NativeExceptionHolderBase *holder = this;

    while (holder != nullptr)
    {
        if (((void *)holder > stackLowAddress) && ((void *)holder < stackHighAddress))
        { 
            return holder;
        }
        // Get next holder
        holder = holder->m_next;
    }

    return nullptr;
}

NativeExceptionHolderBase *
NativeExceptionHolderBase::FindHolder(void *stackLowAddress, void *stackHighAddress)
{
    NativeExceptionHolderBase *head = t_nativeExceptionHolderHead;
    if (head == nullptr)
    {
        return nullptr;
    }
    return head->FindNextHolder(stackLowAddress, stackHighAddress);
}

#include "seh-unwind.cpp"
