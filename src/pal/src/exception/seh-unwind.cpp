//
// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) Geoff Norton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*++



Module Name:

    seh-unwind.cpp

Abstract:

    Implementation of exception API functions based on
    the Unwind API.



--*/

#ifndef FEATURE_PAL_SXS
#error FEATURE_PAL_SXS needs to be defined for this file.
#endif // !FEATURE_PAL_SXS

#include "pal/context.h"
#include <dlfcn.h>
#include <exception>
#if HAVE_LIBUNWIND_H
#define UNW_LOCAL_ONLY
#include <libunwind.h>
#endif

//----------------------------------------------------------------------
// Virtual Unwinding
//----------------------------------------------------------------------

#if HAVE_LIBUNWIND_H
#if UNWIND_CONTEXT_IS_UCONTEXT_T

#if defined(_AMD64_)
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Rip)        \
    ASSIGN_REG(Rsp)        \
    ASSIGN_REG(Rbp)        \
    ASSIGN_REG(Rbx)        \
    ASSIGN_REG(R12)        \
    ASSIGN_REG(R13)        \
    ASSIGN_REG(R14)        \
    ASSIGN_REG(R15)     
#else // _AMD64_
#error unsupported architecture
#endif // _AMD64_

static void WinContextToUnwindContext(CONTEXT *winContext, unw_context_t *unwContext)
{
#define ASSIGN_REG(reg) MCREG_##reg(unwContext->uc_mcontext) = winContext->reg;
    ASSIGN_UNWIND_REGS
#undef ASSIGN_REG
}
#else
static void WinContextToUnwindCursor(CONTEXT *winContext, unw_cursor_t *cursor)
{
#if defined(_AMD64_)
    unw_set_reg(cursor, UNW_REG_IP, winContext->Rip);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Rsp);
    unw_set_reg(cursor, UNW_X86_64_RBP, winContext->Rbp);
    unw_set_reg(cursor, UNW_X86_64_RBX, winContext->Rbx);
    unw_set_reg(cursor, UNW_X86_64_R12, winContext->R12);
    unw_set_reg(cursor, UNW_X86_64_R13, winContext->R13);
    unw_set_reg(cursor, UNW_X86_64_R14, winContext->R14);
    unw_set_reg(cursor, UNW_X86_64_R15, winContext->R15);
#else
#error unsupported architecture
#endif
}
#endif

static void UnwindContextToWinContext(unw_cursor_t *cursor, CONTEXT *winContext)
{
#if defined(_AMD64_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Rip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Rsp);
    unw_get_reg(cursor, UNW_X86_64_RBP, (unw_word_t *) &winContext->Rbp);
    unw_get_reg(cursor, UNW_X86_64_RBX, (unw_word_t *) &winContext->Rbx);
    unw_get_reg(cursor, UNW_X86_64_R12, (unw_word_t *) &winContext->R12);
    unw_get_reg(cursor, UNW_X86_64_R13, (unw_word_t *) &winContext->R13);
    unw_get_reg(cursor, UNW_X86_64_R14, (unw_word_t *) &winContext->R14);
    unw_get_reg(cursor, UNW_X86_64_R15, (unw_word_t *) &winContext->R15);
#else
#error unsupported architecture
#endif
}

static void GetContextPointer(unw_cursor_t *cursor, unw_context_t *unwContext, int reg, PDWORD64 *contextPointer)
{
#if defined(__APPLE__)
    // Returning NULL indicates that we don't have context pointers available
    *contextPointer = NULL;
#else
    unw_save_loc_t saveLoc;
    unw_get_save_loc(cursor, reg, &saveLoc);
    if (saveLoc.type == UNW_SLT_MEMORY)
    {
        PDWORD64 pLoc = (PDWORD64)saveLoc.u.addr;
        // Filter out fake save locations that point to unwContext 
        if ((pLoc < (PDWORD64)unwContext) || ((PDWORD64)(unwContext + 1) <= pLoc))
            *contextPointer = (PDWORD64)saveLoc.u.addr;
    }
#endif
}

static void GetContextPointers(unw_cursor_t *cursor, unw_context_t *unwContext, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
#if defined(_AMD64_)
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBP, &contextPointers->Rbp);
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBX, &contextPointers->Rbx);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R12, &contextPointers->R12);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R13, &contextPointers->R13);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R14, &contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R15, &contextPointers->R15);
#else
#error unsupported architecture
#endif
}

BOOL PAL_VirtualUnwind(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
    int st;
    unw_context_t unwContext;
    unw_cursor_t cursor;
#if defined(__APPLE__) || defined(__FreeBSD__)
    DWORD64 curPc;
#endif

#if UNWIND_CONTEXT_IS_UCONTEXT_T
    WinContextToUnwindContext(context, &unwContext);
#else
    st = unw_getcontext(&unwContext);
    if (st < 0)
    {
        return FALSE;
    }
#endif

    st = unw_init_local(&cursor, &unwContext);
    if (st < 0)
    {
        return FALSE;
    }

#if !UNWIND_CONTEXT_IS_UCONTEXT_T
    // Set the unwind context to the specified windows context
    WinContextToUnwindCursor(context, &cursor);
#endif

#if defined(__APPLE__) || defined(__FreeBSD__)
    // OSX and FreeBSD appear to do two different things when unwinding
    // 1: If it reaches where it cannot unwind anymore, say a 
    // managed frame.  It wil return 0, but also update the $pc
    // 2: If it unwinds all the way to _start it will return
    // 0 from the step, but $pc will stay the same.
    // The behaviour of libunwind from nongnu.org is to null the PC
    // So we bank the original PC here, so we can compare it after
    // the step
    curPc = context->Rip;
#endif

    st = unw_step(&cursor);
    if (st < 0)
    {
        return FALSE;
    }

    // Update the passed in windows context to reflect the unwind
    //
    UnwindContextToWinContext(&cursor, context);
#if defined(__APPLE__) || defined(__FreeBSD__)
    if (st == 0 && context->Rip == curPc)
    {
        context->Rip = 0;
    }
#endif

    if (contextPointers != NULL)
    {
        GetContextPointers(&cursor, &unwContext, contextPointers);
    }

    return TRUE;
}
#else
#error don't know how to unwind on this platform
#endif

/*++
Function:
    RtlpRaiseException

Parameters:
    ExceptionRecord - the Windows exception record to throw

Note:
    The name of this function and the name of the ExceptionRecord 
    parameter is used in the sos lldb plugin code to read the exception
    record. See coreclr\src\ToolBox\SOS\lldbplugin\debugclient.cpp.
--*/
PAL_NORETURN
static void RtlpRaiseException(EXCEPTION_RECORD *ExceptionRecord)
{
    // Capture the context of RtlpRaiseException.
    CONTEXT ContextRecord;
    ZeroMemory(&ContextRecord, sizeof(CONTEXT));
    ContextRecord.ContextFlags = CONTEXT_FULL;
    CONTEXT_CaptureContext(&ContextRecord);

    // Find the caller of RtlpRaiseException.  
    PAL_VirtualUnwind(&ContextRecord, NULL);

    // The frame we're looking at now is RaiseException. We have to unwind one 
    // level further to get the actual context user code could be resumed at.
    PAL_VirtualUnwind(&ContextRecord, NULL);
#if defined(_X86_)
    ExceptionRecord->ExceptionAddress = (void *) ContextRecord.Eip;
#elif defined(_AMD64_)
    ExceptionRecord->ExceptionAddress = (void *) ContextRecord.Rip;
#else
#error unsupported architecture
#endif

    EXCEPTION_POINTERS pointers;
    pointers.ExceptionRecord = ExceptionRecord;
    pointers.ContextRecord = &ContextRecord;

    SEHRaiseException(InternalGetCurrentThread(), &pointers, 0);
}

PAL_NORETURN
void SEHRaiseException(CPalThread *pthrCurrent,
                       PEXCEPTION_POINTERS lpExceptionPointers,
                       int signal_code)
{
    throw PAL_SEHException(lpExceptionPointers->ExceptionRecord, lpExceptionPointers->ContextRecord);
}

/*++
Function:
  RaiseException

See MSDN doc.
--*/
// no PAL_NORETURN, as callers must assume this can return for continuable exceptions.
VOID
PALAPI
RaiseException(IN DWORD dwExceptionCode,
               IN DWORD dwExceptionFlags,
               IN DWORD nNumberOfArguments,
               IN CONST ULONG_PTR *lpArguments)
{
    // PERF_ENTRY_ONLY is used here because RaiseException may or may not
    // return. We can not get latency data without PERF_EXIT. For this reason,
    // PERF_ENTRY_ONLY is used to profile frequency only.
    PERF_ENTRY_ONLY(RaiseException);
    ENTRY("RaiseException(dwCode=%#x, dwFlags=%#x, nArgs=%u, lpArguments=%p)\n",
          dwExceptionCode, dwExceptionFlags, nNumberOfArguments, lpArguments);

    /* Validate parameters */
    if (dwExceptionCode & RESERVED_SEH_BIT)
    {
        WARN("Exception code %08x has bit 28 set; clearing it.\n", dwExceptionCode);
        dwExceptionCode ^= RESERVED_SEH_BIT;
    }

    if (nNumberOfArguments > EXCEPTION_MAXIMUM_PARAMETERS)
    {
        WARN("Number of arguments (%d) exceeds the limit "
            "EXCEPTION_MAXIMUM_PARAMETERS (%d); ignoring extra parameters.\n",
            nNumberOfArguments, EXCEPTION_MAXIMUM_PARAMETERS);
        nNumberOfArguments = EXCEPTION_MAXIMUM_PARAMETERS;
    }

    EXCEPTION_RECORD exceptionRecord;
    ZeroMemory(&exceptionRecord, sizeof(EXCEPTION_RECORD));

    exceptionRecord.ExceptionCode = dwExceptionCode;
    exceptionRecord.ExceptionFlags = dwExceptionFlags;
    exceptionRecord.ExceptionRecord = NULL;
    exceptionRecord.ExceptionAddress = NULL; // will be set by RtlpRaiseException
    exceptionRecord.NumberParameters = nNumberOfArguments;
    if (nNumberOfArguments)
    {
        CopyMemory(exceptionRecord.ExceptionInformation, lpArguments,
                   nNumberOfArguments * sizeof(ULONG_PTR));
    }
    RtlpRaiseException(&exceptionRecord);

    LOGEXIT("RaiseException returns\n");
}
