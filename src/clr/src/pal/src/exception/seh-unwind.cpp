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
#elif defined(_ARM64_)
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Pc)         \
    ASSIGN_REG(Sp)         \
    ASSIGN_REG(Fp)         \
    ASSIGN_REG(Lr)         \
    ASSIGN_REG(X19)        \
    ASSIGN_REG(X20)        \
    ASSIGN_REG(X21)        \
    ASSIGN_REG(X22)        \
    ASSIGN_REG(X23)        \
    ASSIGN_REG(X24)        \
    ASSIGN_REG(X25)        \
    ASSIGN_REG(X26)        \
    ASSIGN_REG(X27)        \
    ASSIGN_REG(X28)
#else
#error unsupported architecture
#endif

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
#elif defined(_ARM_)
    unw_set_reg(cursor, UNW_REG_IP, winContext->Pc);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Sp);
    unw_set_reg(cursor, UNW_ARM_R14, winContext->Lr);
    unw_set_reg(cursor, UNW_ARM_R4, winContext->R4);
    unw_set_reg(cursor, UNW_ARM_R5, winContext->R5);
    unw_set_reg(cursor, UNW_ARM_R6, winContext->R6);
    unw_set_reg(cursor, UNW_ARM_R7, winContext->R7);
    unw_set_reg(cursor, UNW_ARM_R8, winContext->R8);
    unw_set_reg(cursor, UNW_ARM_R9, winContext->R9);
    unw_set_reg(cursor, UNW_ARM_R10, winContext->R10);
    unw_set_reg(cursor, UNW_ARM_R11, winContext->R11);
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
#elif defined(_ARM_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_ARM_R14, (unw_word_t *) &winContext->Lr);
    unw_get_reg(cursor, UNW_ARM_R4, (unw_word_t *) &winContext->R4);
    unw_get_reg(cursor, UNW_ARM_R5, (unw_word_t *) &winContext->R5);
    unw_get_reg(cursor, UNW_ARM_R6, (unw_word_t *) &winContext->R6);
    unw_get_reg(cursor, UNW_ARM_R7, (unw_word_t *) &winContext->R7);
    unw_get_reg(cursor, UNW_ARM_R8, (unw_word_t *) &winContext->R8);
    unw_get_reg(cursor, UNW_ARM_R9, (unw_word_t *) &winContext->R9);
    unw_get_reg(cursor, UNW_ARM_R10, (unw_word_t *) &winContext->R10);
    unw_get_reg(cursor, UNW_ARM_R11, (unw_word_t *) &winContext->R11);
#elif defined(_ARM64_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_AARCH64_X29, (unw_word_t *) &winContext->Fp);
    unw_get_reg(cursor, UNW_AARCH64_X30, (unw_word_t *) &winContext->Lr);
    unw_get_reg(cursor, UNW_AARCH64_X19, (unw_word_t *) &winContext->X19);
    unw_get_reg(cursor, UNW_AARCH64_X20, (unw_word_t *) &winContext->X20);
    unw_get_reg(cursor, UNW_AARCH64_X21, (unw_word_t *) &winContext->X21);
    unw_get_reg(cursor, UNW_AARCH64_X22, (unw_word_t *) &winContext->X22);
    unw_get_reg(cursor, UNW_AARCH64_X23, (unw_word_t *) &winContext->X23);
    unw_get_reg(cursor, UNW_AARCH64_X24, (unw_word_t *) &winContext->X24);
    unw_get_reg(cursor, UNW_AARCH64_X25, (unw_word_t *) &winContext->X25);
    unw_get_reg(cursor, UNW_AARCH64_X26, (unw_word_t *) &winContext->X26);
    unw_get_reg(cursor, UNW_AARCH64_X27, (unw_word_t *) &winContext->X27);
    unw_get_reg(cursor, UNW_AARCH64_X28, (unw_word_t *) &winContext->X28);
#else
#error unsupported architecture
#endif
}

static void GetContextPointer(unw_cursor_t *cursor, unw_context_t *unwContext, int reg, SIZE_T **contextPointer)
{
#if defined(__APPLE__)
    // Returning NULL indicates that we don't have context pointers available
    *contextPointer = NULL;
#else
    unw_save_loc_t saveLoc;
    unw_get_save_loc(cursor, reg, &saveLoc);
    if (saveLoc.type == UNW_SLT_MEMORY)
    {
        SIZE_T *pLoc = (SIZE_T *)saveLoc.u.addr;
        // Filter out fake save locations that point to unwContext 
        if ((pLoc < (SIZE_T *)unwContext) || ((SIZE_T *)(unwContext + 1) <= pLoc))
            *contextPointer = (SIZE_T *)saveLoc.u.addr;
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
#elif defined(_ARM_)
    GetContextPointer(cursor, unwContext, UNW_ARM_R4, &contextPointers->R4);
    GetContextPointer(cursor, unwContext, UNW_ARM_R5, &contextPointers->R5);
    GetContextPointer(cursor, unwContext, UNW_ARM_R6, &contextPointers->R6);
    GetContextPointer(cursor, unwContext, UNW_ARM_R7, &contextPointers->R7);
    GetContextPointer(cursor, unwContext, UNW_ARM_R8, &contextPointers->R8);
    GetContextPointer(cursor, unwContext, UNW_ARM_R9, &contextPointers->R9);
    GetContextPointer(cursor, unwContext, UNW_ARM_R10, &contextPointers->R10);
    GetContextPointer(cursor, unwContext, UNW_ARM_R11, &contextPointers->R11);
#elif defined(_ARM64_)
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X19, &contextPointers->X19);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X20, &contextPointers->X20);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X21, &contextPointers->X21);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X22, &contextPointers->X22);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X23, &contextPointers->X23);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X24, &contextPointers->X24);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X25, &contextPointers->X25);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X26, &contextPointers->X26);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X27, &contextPointers->X27);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X28, &contextPointers->X28);
#else
#error unsupported architecture
#endif
}

static DWORD64 GetPc(CONTEXT *context)
{
#if defined(_AMD64_)
    return context->Rip;
#elif defined(_ARM64_)
    return context->Pc;
#else
#error don't know how to get the program counter for this architecture
#endif
}

static void SetPc(CONTEXT *context, DWORD64 pc)
{
#if defined(_AMD64_)
    context->Rip = pc;
#elif defined(_ARM64_)
    context->Pc = pc;
#else
#error don't know how to set the program counter for this architecture
#endif
}

BOOL PAL_VirtualUnwind(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
    int st;
    unw_context_t unwContext;
    unw_cursor_t cursor;
#if defined(__APPLE__) || defined(__FreeBSD__) || defined(_ARM64_)
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

#if defined(__APPLE__) || defined(__FreeBSD__) || defined(_ARM64_)
    // OSX and FreeBSD appear to do two different things when unwinding
    // 1: If it reaches where it cannot unwind anymore, say a 
    // managed frame.  It wil return 0, but also update the $pc
    // 2: If it unwinds all the way to _start it will return
    // 0 from the step, but $pc will stay the same.
    // The behaviour of libunwind from nongnu.org is to null the PC
    // So we bank the original PC here, so we can compare it after
    // the step
    curPc = GetPc(context);
#endif

    st = unw_step(&cursor);
    if (st < 0)
    {
        return FALSE;
    }

    // Update the passed in windows context to reflect the unwind
    //
    UnwindContextToWinContext(&cursor, context);
#if defined(__APPLE__) || defined(__FreeBSD__) || defined(_ARM64_)
    if (st == 0 && GetPc(context) == curPc)
    {
        SetPc(context, 0);
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
#elif defined(_ARM_) || defined(_ARM64_)
    ExceptionRecord->ExceptionAddress = (void *) ContextRecord.Pc;
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
