// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
PGET_GCMARKER_EXCEPTION_CODE g_getGcMarkerExceptionCode = NULL;

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
#if !HAVE_MACH_EXCEPTIONS
    if (!SEHInitializeSignals())
    {
        ERROR("SEHInitializeSignals failed!\n");
        SEHCleanup();
        return FALSE;
    }
#endif

    return TRUE;
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
    g_hardwareExceptionHandler = exceptionHandler;
}

/*++
Function:
    PAL_SetGetGcMarkerExceptionCode

    Register a function that determines if the specified IP has code that is a GC marker for GCCover.

Parameters:
    getGcMarkerExceptionCode - the function to register

Return value:
    None
--*/
VOID
PALAPI 
PAL_SetGetGcMarkerExceptionCode(
    IN PGET_GCMARKER_EXCEPTION_CODE getGcMarkerExceptionCode)
{
    g_getGcMarkerExceptionCode = getGcMarkerExceptionCode;
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
    if (!IsInDebugBreak(pointers->ExceptionRecord->ExceptionAddress))
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
#elif __linux__ || defined(__FreeBSD__) || defined(__NetBSD__)
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
#elif __linux__ || defined(__FreeBSD__) || defined(__NetBSD__)
    return NO_ERROR;
#else // HAVE_MACH_EXCEPTIONS
#error not yet implemented
#endif // HAVE_MACH_EXCEPTIONS
}

/*++

  CatchHardwareExceptionHolder implementation

--*/

CatchHardwareExceptionHolder::CatchHardwareExceptionHolder()
{
    CPalThread *pThread = InternalGetCurrentThread();
    ++pThread->m_hardwareExceptionHolderCount;
}

CatchHardwareExceptionHolder::~CatchHardwareExceptionHolder()
{
    CPalThread *pThread = InternalGetCurrentThread();
    --pThread->m_hardwareExceptionHolderCount;
}

bool CatchHardwareExceptionHolder::IsEnabled()
{
    CPalThread *pThread = InternalGetCurrentThread();
    return pThread->IsHardwareExceptionsEnabled();
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
NativeExceptionHolderBase::FindNextHolder(NativeExceptionHolderBase *currentHolder, void *stackLowAddress, void *stackHighAddress)
{
    NativeExceptionHolderBase *holder = (currentHolder == nullptr) ? t_nativeExceptionHolderHead : currentHolder->m_next;

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

#include "seh-unwind.cpp"
