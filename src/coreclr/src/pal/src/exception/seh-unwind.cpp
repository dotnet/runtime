// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    seh-unwind.cpp

Abstract:

    Implementation of exception API functions based on
    the Unwind API.



--*/

#ifdef HOST_UNIX
#include "pal/context.h"
#include "pal.h"
#include <dlfcn.h>

#define UNW_LOCAL_ONLY
// Sub-headers included from the libunwind.h contain an empty struct
// and clang issues a warning. Until the libunwind is fixed, disable
// the warning.
#ifdef __llvm__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wextern-c-compat"
#endif
#include <libunwind.h>
#ifdef __llvm__
#pragma clang diagnostic pop
#endif
#else // HOST_UNIX

#include <windows.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <libunwind.h>
#include "debugmacros.h"
#include "crosscomp.h"

#define KNONVOLATILE_CONTEXT_POINTERS T_KNONVOLATILE_CONTEXT_POINTERS
#define CONTEXT T_CONTEXT

#define ASSERT(x, ...)
#define TRACE(x, ...)

#define PALAPI

#endif // HOST_UNIX

//----------------------------------------------------------------------
// Virtual Unwinding
//----------------------------------------------------------------------

#if UNWIND_CONTEXT_IS_UCONTEXT_T

#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Rip)        \
    ASSIGN_REG(Rsp)        \
    ASSIGN_REG(Rbp)        \
    ASSIGN_REG(Rbx)        \
    ASSIGN_REG(R12)        \
    ASSIGN_REG(R13)        \
    ASSIGN_REG(R14)        \
    ASSIGN_REG(R15)
#elif (defined(HOST_UNIX) && defined(HOST_ARM64)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM64))
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
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Eip)        \
    ASSIGN_REG(Esp)        \
    ASSIGN_REG(Ebp)        \
    ASSIGN_REG(Ebx)        \
    ASSIGN_REG(Esi)        \
    ASSIGN_REG(Edi)
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
static void WinContextToUnwindContext(CONTEXT *winContext, unw_context_t *unwContext)
{
#if (defined(HOST_UNIX) && defined(HOST_ARM)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM))
    // Assuming that unw_set_reg() on cursor will point the cursor to the
    // supposed stack frame is dangerous for libunwind-arm in Linux.
    // It is because libunwind's unw_cursor_t has other data structure
    // initialized by unw_init_local(), which are not updated by
    // unw_set_reg().
    unwContext->regs[0] = 0;
    unwContext->regs[1] = 0;
    unwContext->regs[2] = 0;
    unwContext->regs[3] = 0;
    unwContext->regs[4] = winContext->R4;
    unwContext->regs[5] = winContext->R5;
    unwContext->regs[6] = winContext->R6;
    unwContext->regs[7] = winContext->R7;
    unwContext->regs[8] = winContext->R8;
    unwContext->regs[9] = winContext->R9;
    unwContext->regs[10] = winContext->R10;
    unwContext->regs[11] = winContext->R11;
    unwContext->regs[12] = 0;
    unwContext->regs[13] = winContext->Sp;
    unwContext->regs[14] = winContext->Lr;
    unwContext->regs[15] = winContext->Pc;
#elif defined(HOST_ARM64)
    unwContext->uc_mcontext.pc       = winContext->Pc;
    unwContext->uc_mcontext.sp       = winContext->Sp;
    unwContext->uc_mcontext.regs[29] = winContext->Fp;
    unwContext->uc_mcontext.regs[30] = winContext->Lr;

    unwContext->uc_mcontext.regs[19] = winContext->X19;
    unwContext->uc_mcontext.regs[20] = winContext->X20;
    unwContext->uc_mcontext.regs[21] = winContext->X21;
    unwContext->uc_mcontext.regs[22] = winContext->X22;
    unwContext->uc_mcontext.regs[23] = winContext->X23;
    unwContext->uc_mcontext.regs[24] = winContext->X24;
    unwContext->uc_mcontext.regs[25] = winContext->X25;
    unwContext->uc_mcontext.regs[26] = winContext->X26;
    unwContext->uc_mcontext.regs[27] = winContext->X27;
    unwContext->uc_mcontext.regs[28] = winContext->X28;
#endif
}

static void WinContextToUnwindCursor(CONTEXT *winContext, unw_cursor_t *cursor)
{
#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
    unw_set_reg(cursor, UNW_REG_IP, winContext->Rip);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Rsp);
    unw_set_reg(cursor, UNW_X86_64_RBP, winContext->Rbp);
    unw_set_reg(cursor, UNW_X86_64_RBX, winContext->Rbx);
    unw_set_reg(cursor, UNW_X86_64_R12, winContext->R12);
    unw_set_reg(cursor, UNW_X86_64_R13, winContext->R13);
    unw_set_reg(cursor, UNW_X86_64_R14, winContext->R14);
    unw_set_reg(cursor, UNW_X86_64_R15, winContext->R15);
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
    unw_set_reg(cursor, UNW_REG_IP, winContext->Eip);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Esp);
    unw_set_reg(cursor, UNW_X86_EBP, winContext->Ebp);
    unw_set_reg(cursor, UNW_X86_EBX, winContext->Ebx);
    unw_set_reg(cursor, UNW_X86_ESI, winContext->Esi);
    unw_set_reg(cursor, UNW_X86_EDI, winContext->Edi);
#endif
}
#endif

void UnwindContextToWinContext(unw_cursor_t *cursor, CONTEXT *winContext)
{
#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Rip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Rsp);
    unw_get_reg(cursor, UNW_X86_64_RBP, (unw_word_t *) &winContext->Rbp);
    unw_get_reg(cursor, UNW_X86_64_RBX, (unw_word_t *) &winContext->Rbx);
    unw_get_reg(cursor, UNW_X86_64_R12, (unw_word_t *) &winContext->R12);
    unw_get_reg(cursor, UNW_X86_64_R13, (unw_word_t *) &winContext->R13);
    unw_get_reg(cursor, UNW_X86_64_R14, (unw_word_t *) &winContext->R14);
    unw_get_reg(cursor, UNW_X86_64_R15, (unw_word_t *) &winContext->R15);
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Eip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Esp);
    unw_get_reg(cursor, UNW_X86_EBP, (unw_word_t *) &winContext->Ebp);
    unw_get_reg(cursor, UNW_X86_EBX, (unw_word_t *) &winContext->Ebx);
    unw_get_reg(cursor, UNW_X86_ESI, (unw_word_t *) &winContext->Esi);
    unw_get_reg(cursor, UNW_X86_EDI, (unw_word_t *) &winContext->Edi);
#elif (defined(HOST_UNIX) && defined(HOST_ARM)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM))
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_ARM_R14, (unw_word_t *) &winContext->Lr);
    unw_get_reg(cursor, UNW_ARM_R4, (unw_word_t *) &winContext->R4);
    unw_get_reg(cursor, UNW_ARM_R5, (unw_word_t *) &winContext->R5);
    unw_get_reg(cursor, UNW_ARM_R6, (unw_word_t *) &winContext->R6);
    unw_get_reg(cursor, UNW_ARM_R7, (unw_word_t *) &winContext->R7);
    unw_get_reg(cursor, UNW_ARM_R8, (unw_word_t *) &winContext->R8);
    unw_get_reg(cursor, UNW_ARM_R9, (unw_word_t *) &winContext->R9);
    unw_get_reg(cursor, UNW_ARM_R10, (unw_word_t *) &winContext->R10);
    unw_get_reg(cursor, UNW_ARM_R11, (unw_word_t *) &winContext->R11);
#elif (defined(HOST_UNIX) && defined(HOST_ARM64)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM64))
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
#if defined(HAVE_UNW_GET_SAVE_LOC)
    unw_save_loc_t saveLoc;
    unw_get_save_loc(cursor, reg, &saveLoc);
    if (saveLoc.type == UNW_SLT_MEMORY)
    {
        SIZE_T *pLoc = (SIZE_T *)saveLoc.u.addr;
        // Filter out fake save locations that point to unwContext
        if (unwContext == NULL || (pLoc < (SIZE_T *)unwContext) || ((SIZE_T *)(unwContext + 1) <= pLoc))
            *contextPointer = (SIZE_T *)saveLoc.u.addr;
    }
#else
    // Returning NULL indicates that we don't have context pointers available
    *contextPointer = NULL;
#endif
}

void GetContextPointers(unw_cursor_t *cursor, unw_context_t *unwContext, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBP, &contextPointers->Rbp);
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBX, &contextPointers->Rbx);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R12, &contextPointers->R12);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R13, &contextPointers->R13);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R14, &contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R15, &contextPointers->R15);
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
    GetContextPointer(cursor, unwContext, UNW_X86_EBX, &contextPointers->Ebx);
    GetContextPointer(cursor, unwContext, UNW_X86_EBP, &contextPointers->Ebp);
    GetContextPointer(cursor, unwContext, UNW_X86_ESI, &contextPointers->Esi);
    GetContextPointer(cursor, unwContext, UNW_X86_EDI, &contextPointers->Edi);
#elif (defined(HOST_UNIX) && defined(HOST_ARM)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM))
    GetContextPointer(cursor, unwContext, UNW_ARM_R4, &contextPointers->R4);
    GetContextPointer(cursor, unwContext, UNW_ARM_R5, &contextPointers->R5);
    GetContextPointer(cursor, unwContext, UNW_ARM_R6, &contextPointers->R6);
    GetContextPointer(cursor, unwContext, UNW_ARM_R7, &contextPointers->R7);
    GetContextPointer(cursor, unwContext, UNW_ARM_R8, &contextPointers->R8);
    GetContextPointer(cursor, unwContext, UNW_ARM_R9, &contextPointers->R9);
    GetContextPointer(cursor, unwContext, UNW_ARM_R10, &contextPointers->R10);
    GetContextPointer(cursor, unwContext, UNW_ARM_R11, &contextPointers->R11);
#elif (defined(HOST_UNIX) && defined(HOST_ARM64)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM64))
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
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X29, &contextPointers->Fp);
#else
#error unsupported architecture
#endif
}

#ifndef HOST_WINDOWS

extern int g_common_signal_handler_context_locvar_offset;

BOOL PAL_VirtualUnwind(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
    int st;
    unw_context_t unwContext;
    unw_cursor_t cursor;

    DWORD64 curPc = CONTEXTGetPC(context);

#ifndef __APPLE__
    // Check if the PC is the return address from the SEHProcessException in the common_signal_handler.
    // If that's the case, extract its local variable containing the windows style context of the hardware
    // exception and return that. This skips the hardware signal handler trampoline that the libunwind
    // cannot cross on some systems.
    if ((void*)curPc == g_SEHProcessExceptionReturnAddress)
    {
        CONTEXT* signalContext = (CONTEXT*)(CONTEXTGetFP(context) + g_common_signal_handler_context_locvar_offset);
        memcpy_s(context, sizeof(CONTEXT), signalContext, sizeof(CONTEXT));

        return TRUE;
    }
#endif

    if ((context->ContextFlags & CONTEXT_EXCEPTION_ACTIVE) != 0)
    {
        // The current frame is a source of hardware exception. Due to the fact that
        // we use the low level unwinder to unwind just one frame a time, the
        // unwinder doesn't have the signal_frame flag set. So it doesn't
        // know that it should not decrement the PC before looking up the unwind info.
        // So we compensate it by incrementing the PC before passing it to the unwinder.
        // Without it, the unwinder would not find unwind info if the hardware exception
        // happened in the first instruction of a function.
        CONTEXTSetPC(context, curPc + 1);
    }

#if !UNWIND_CONTEXT_IS_UCONTEXT_T
// The unw_getcontext is defined in the libunwind headers for ARM as inline assembly with
// stmia instruction storing SP and PC, which clang complains about as deprecated.
// However, it is required for atomic restoration of the context, so disable that warning.
#if defined(__llvm__) && defined(TARGET_ARM)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winline-asm"
#endif
    st = unw_getcontext(&unwContext);
#if defined(__llvm__) && defined(TARGET_ARM)
#pragma clang diagnostic pop
#endif

    if (st < 0)
    {
        return FALSE;
    }
#endif

    WinContextToUnwindContext(context, &unwContext);

    st = unw_init_local(&cursor, &unwContext);
    if (st < 0)
    {
        return FALSE;
    }

#if !UNWIND_CONTEXT_IS_UCONTEXT_T
    // Set the unwind context to the specified windows context
    WinContextToUnwindCursor(context, &cursor);
#endif

    st = unw_step(&cursor);
    if (st < 0)
    {
        return FALSE;
    }

    // Check if the frame we have unwound to is a frame that caused
    // synchronous signal, like a hardware exception and record it
    // in the context flags.
    if (unw_is_signal_frame(&cursor) > 0)
    {
        context->ContextFlags |= CONTEXT_EXCEPTION_ACTIVE;
#if defined(CONTEXT_UNWOUND_TO_CALL)
        context->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
#endif // CONTEXT_UNWOUND_TO_CALL
    }
    else
    {
        context->ContextFlags &= ~CONTEXT_EXCEPTION_ACTIVE;
#if defined(CONTEXT_UNWOUND_TO_CALL)
        context->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;
#endif // CONTEXT_UNWOUND_TO_CALL
    }

    // Update the passed in windows context to reflect the unwind
    //
    UnwindContextToWinContext(&cursor, context);

    // On some OSes / architectures if it unwound all the way to _start
    // (__libc_start_main on arm64 Linux with glibc older than 2.27).
    // >= 0 is returned from the step, but $pc will stay the same.
    // So we detect that here and set the $pc to NULL in that case.
    // This is the default behavior of the libunwind on x64 Linux.
    //
    if (st >= 0 && CONTEXTGetPC(context) == curPc)
    {
        CONTEXTSetPC(context, 0);
    }

    if (contextPointers != NULL)
    {
        GetContextPointers(&cursor, &unwContext, contextPointers);
    }
    return TRUE;
}

struct ExceptionRecords
{
    CONTEXT ContextRecord;
    EXCEPTION_RECORD ExceptionRecord;
};

// Max number of fallback contexts that are used when malloc fails to allocate ExceptionRecords structure
static const int MaxFallbackContexts = sizeof(size_t) * 8;
// Array of fallback contexts
static ExceptionRecords s_fallbackContexts[MaxFallbackContexts];
// Bitmap used for allocating fallback contexts - bits set to 1 represent already allocated context.
static volatile size_t s_allocatedContextsBitmap = 0;

/*++
Function:
    AllocateExceptionRecords

    Allocate EXCEPTION_RECORD and CONTEXT structures for an exception.
Parameters:
    exceptionRecord - output pointer to the allocated exception record
    contextRecord - output pointer to the allocated context record
--*/
VOID
AllocateExceptionRecords(EXCEPTION_RECORD** exceptionRecord, CONTEXT** contextRecord)
{
    ExceptionRecords* records;
    if (posix_memalign((void**)&records, alignof(ExceptionRecords), sizeof(ExceptionRecords)) != 0)
    {
        size_t bitmap;
        size_t newBitmap;
        int index;

        do
        {
            bitmap = s_allocatedContextsBitmap;
            index = __builtin_ffsl(~bitmap) - 1;
            if (index < 0)
            {
                PROCAbort();
            }

            newBitmap = bitmap | ((size_t)1 << index);
        }
        while (__sync_val_compare_and_swap(&s_allocatedContextsBitmap, bitmap, newBitmap) != bitmap);

        records = &s_fallbackContexts[index];
    }

    *contextRecord = &records->ContextRecord;
    *exceptionRecord = &records->ExceptionRecord;
}

/*++
Function:
    PAL_FreeExceptionRecords

    Free EXCEPTION_RECORD and CONTEXT structures of an exception that were allocated by the
    AllocateExceptionRecords.
Parameters:
    exceptionRecord - exception record
    contextRecord - context record
--*/
VOID
PALAPI
PAL_FreeExceptionRecords(IN EXCEPTION_RECORD *exceptionRecord, IN CONTEXT *contextRecord)
{
    // Both records are allocated at once and the allocated memory starts at the contextRecord
    ExceptionRecords* records = (ExceptionRecords*)contextRecord;
    if ((records >= &s_fallbackContexts[0]) && (records < &s_fallbackContexts[MaxFallbackContexts]))
    {
        int index = records - &s_fallbackContexts[0];
        __sync_fetch_and_and(&s_allocatedContextsBitmap, ~((size_t)1 << index));
    }
    else
    {
        free(contextRecord);
    }
}

/*++
Function:
    RtlpRaiseException

Parameters:
    ExceptionRecord - the Windows exception record to throw

Note:
    The name of this function and the name of the ExceptionRecord
    parameter is used in the sos lldb plugin code to read the exception
    record. See coreclr\src\ToolBox\SOS\lldbplugin\services.cpp.

    This function must not be inlined or optimized so the below PAL_VirtualUnwind
    calls end up with RaiseException caller's context and so the above debugger
    code finds the function and ExceptionRecord parameter.
--*/
PAL_NORETURN
__attribute__((noinline))
__attribute__((NOOPT_ATTRIBUTE))
static void
RtlpRaiseException(EXCEPTION_RECORD *ExceptionRecord, CONTEXT *ContextRecord)
{
    throw PAL_SEHException(ExceptionRecord, ContextRecord);
}

/*++
Function:
  RaiseException

See MSDN doc.
--*/
// no PAL_NORETURN, as callers must assume this can return for continuable exceptions.
__attribute__((noinline))
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

    CONTEXT *contextRecord;
    EXCEPTION_RECORD *exceptionRecord;
    AllocateExceptionRecords(&exceptionRecord, &contextRecord);

    ZeroMemory(exceptionRecord, sizeof(EXCEPTION_RECORD));

    exceptionRecord->ExceptionCode = dwExceptionCode;
    exceptionRecord->ExceptionFlags = dwExceptionFlags;
    exceptionRecord->ExceptionRecord = NULL;
    exceptionRecord->ExceptionAddress = NULL; // will be set by RtlpRaiseException
    exceptionRecord->NumberParameters = nNumberOfArguments;
    if (nNumberOfArguments)
    {
        CopyMemory(exceptionRecord->ExceptionInformation, lpArguments,
                   nNumberOfArguments * sizeof(ULONG_PTR));
    }

    // Capture the context of RaiseException.
    ZeroMemory(contextRecord, sizeof(CONTEXT));
    contextRecord->ContextFlags = CONTEXT_FULL;
    CONTEXT_CaptureContext(contextRecord);

    // We have to unwind one level to get the actual context user code could be resumed at.
    PAL_VirtualUnwind(contextRecord, NULL);

    exceptionRecord->ExceptionAddress = (void *)CONTEXTGetPC(contextRecord);

    RtlpRaiseException(exceptionRecord, contextRecord);

    LOGEXIT("RaiseException returns\n");
}

#endif // !HOST_WINDOWS
