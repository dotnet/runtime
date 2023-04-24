// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "pal/signal.hpp"
#include "pal/virtual.h"

#if HAVE_MACH_EXCEPTIONS
#include "machexception.h"
#else
#include <signal.h>
#endif

#include <string.h>
#include <unistd.h>
#include <pthread.h>
#include <stdlib.h>
#include <utility>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT);

/* Constant and type definitions **********************************************/

/* Bit 28 of exception codes is reserved. */
const UINT RESERVED_SEH_BIT = 0x800000;

/* Internal variables definitions **********************************************/

PHARDWARE_EXCEPTION_HANDLER g_hardwareExceptionHandler = NULL;
// Function to check if an activation can be safely injected at a specified context
PHARDWARE_EXCEPTION_SAFETY_CHECK_FUNCTION g_safeExceptionCheckFunction = NULL;

PGET_GCMARKER_EXCEPTION_CODE g_getGcMarkerExceptionCode = NULL;

// Return address of the SEHProcessException, which is used to enable walking over
// the signal handler trampoline on some Unixes where the libunwind cannot do that.
void* g_SEHProcessExceptionReturnAddress = NULL;
void* g_InvokeActivationHandlerReturnAddress = NULL;

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
    if (!SEHInitializeSignals(pthrCurrent, flags))
    {
        ERROR("SEHInitializeSignals failed!\n");
        SEHCleanup();
        return FALSE;
    }

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

    SEHCleanupSignals();
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
    IN PHARDWARE_EXCEPTION_HANDLER exceptionHandler,
    IN PHARDWARE_EXCEPTION_SAFETY_CHECK_FUNCTION exceptionCheckFunction)
{
    g_hardwareExceptionHandler = exceptionHandler;
    g_safeExceptionCheckFunction = exceptionCheckFunction;
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

EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);

/*++
Function:
    PAL_ThrowExceptionFromContext

    This function creates a stack frame right below the target frame, restores all callee
    saved registers from the passed in context, sets the RSP to that frame and sets the
    return address to the target frame's RIP.
    Then it uses the ThrowExceptionHelper to throw the passed in exception from that context.

Parameters:
    CONTEXT* context - context from which the exception will be thrown
    PAL_SEHException* ex - the exception to throw.
--*/
VOID
PALAPI
PAL_ThrowExceptionFromContext(CONTEXT* context, PAL_SEHException* ex)
{
    // We need to make a copy of the exception off stack, since the "ex" is located in one of the stack
    // frames that will become obsolete by the ThrowExceptionFromContextInternal and the ThrowExceptionHelper
    // could overwrite the "ex" object by stack e.g. when allocating the low level exception object for "throw".
    static __thread BYTE threadLocalExceptionStorage[sizeof(PAL_SEHException)];
    ThrowExceptionFromContextInternal(context, new (threadLocalExceptionStorage) PAL_SEHException(std::move(*ex)));
}

/*++
Function:
    ThrowExceptionHelper

    Helper function to throw the passed in exception.
    It is called from the assembler function ThrowExceptionFromContextInternal

Parameters:
    PAL_SEHException* ex - the exception to throw.
--*/
extern "C"
#ifdef HOST_X86
void __fastcall ThrowExceptionHelper(PAL_SEHException* ex)
#else // HOST_X86
void ThrowExceptionHelper(PAL_SEHException* ex)
#endif // !HOST_X86
{
    throw std::move(*ex);
}

/*++
Function:
    EnsureExceptionRecordsOnHeap

    Helper function to move records from stack to heap.

Parameters:
    PAL_SEHException* exception
--*/
static void EnsureExceptionRecordsOnHeap(PAL_SEHException* exception)
{
    if( !exception->RecordsOnStack ||
        exception->ExceptionPointers.ExceptionRecord == NULL )
    {
        return;
    }

    CONTEXT* contextRecord = exception->ExceptionPointers.ContextRecord;
    EXCEPTION_RECORD* exceptionRecord = exception->ExceptionPointers.ExceptionRecord;

    CONTEXT* contextRecordCopy;
    EXCEPTION_RECORD* exceptionRecordCopy;
    AllocateExceptionRecords(&exceptionRecordCopy, &contextRecordCopy);

    *exceptionRecordCopy = *exceptionRecord;
    *contextRecordCopy = *contextRecord;

    exception->ExceptionPointers.ExceptionRecord = exceptionRecordCopy;
    exception->ExceptionPointers.ContextRecord = contextRecordCopy;
    exception->RecordsOnStack = false;
}

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
SEHProcessException(PAL_SEHException* exception)
{
    g_SEHProcessExceptionReturnAddress = __builtin_return_address(0);

    CONTEXT* contextRecord = exception->GetContextRecord();
    EXCEPTION_RECORD* exceptionRecord = exception->GetExceptionRecord();

    if (!IsInDebugBreak(exceptionRecord->ExceptionAddress))
    {
        if (g_hardwareExceptionHandler != NULL)
        {
            _ASSERTE(g_safeExceptionCheckFunction != NULL);
            // Check if it is safe to handle the hardware exception (the exception happened in managed code
            // or in a jitter helper or it is a debugger breakpoint)
            if (g_safeExceptionCheckFunction(contextRecord, exceptionRecord))
            {
                EnsureExceptionRecordsOnHeap(exception);
                if (g_hardwareExceptionHandler(exception))
                {
                    // The exception happened in managed code and the execution should continue.
                    return TRUE;
                }

                // The exception was a single step or a breakpoint and it was not handled by the debugger.
            }
        }

        if (CatchHardwareExceptionHolder::IsEnabled())
        {
            EnsureExceptionRecordsOnHeap(exception);
            exception->IsExternal = true;
            PAL_ThrowExceptionFromContext(exception->GetContextRecord(), exception);
        }
    }

    // Unhandled hardware exception pointers->ExceptionRecord->ExceptionCode at pointers->ExceptionRecord->ExceptionAddress
    return FALSE;
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
#elif defined(__linux__) || defined(__FreeBSD__) || defined(__NetBSD__) || defined(__sun)
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
#elif defined(__linux__) || defined(__FreeBSD__) || defined(__NetBSD__) || defined(__sun)
    return NO_ERROR;
#else // HAVE_MACH_EXCEPTIONS
#error not yet implemented
#endif // HAVE_MACH_EXCEPTIONS
}

/*++

  CatchHardwareExceptionHolder implementation

--*/

extern "C"
void
PALAPI
PAL_CatchHardwareExceptionHolderEnter()
{
    CPalThread *pThread = InternalGetCurrentThread();
    pThread->IncrementHardwareExceptionHolderCount();
}

extern "C"
void
PALAPI
PAL_CatchHardwareExceptionHolderExit()
{
    CPalThread *pThread = InternalGetCurrentThread();
    pThread->DecrementHardwareExceptionHolderCount();
}

bool CatchHardwareExceptionHolder::IsEnabled()
{
    CPalThread *pThread = GetCurrentPalThread();
    return pThread ? pThread->IsHardwareExceptionsEnabled() : false;
}

/*++

  NativeExceptionHolderBase implementation

--*/

#if defined(__GNUC__)
static __thread
#else // __GNUC__
__declspec(thread) static
#endif // !__GNUC__
NativeExceptionHolderBase *t_nativeExceptionHolderHead = nullptr;

extern "C"
NativeExceptionHolderBase **
PAL_GetNativeExceptionHolderHead()
{
    return &t_nativeExceptionHolderHead;
}

NativeExceptionHolderBase *
NativeExceptionHolderBase::FindNextHolder(NativeExceptionHolderBase *currentHolder, PVOID stackLowAddress, PVOID stackHighAddress)
{
    NativeExceptionHolderBase *holder = (currentHolder == nullptr) ? t_nativeExceptionHolderHead : currentHolder->m_next;

    while (holder != nullptr)
    {
        if (((void *)holder >= stackLowAddress) && ((void *)holder < stackHighAddress))
        {
            return holder;
        }
        // Get next holder
        holder = holder->m_next;
    }

    return nullptr;
}

#include "seh-unwind.cpp"
