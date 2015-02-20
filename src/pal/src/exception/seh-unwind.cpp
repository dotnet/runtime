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
#define UNW_LOCAL_ONLY
#include <libunwind.h>

//----------------------------------------------------------------------
// Exception Handling ABI Level I: Base ABI
//----------------------------------------------------------------------

typedef UINT_PTR _Unwind_Ptr;

struct dwarf_eh_bases
{
    _Unwind_Ptr dataRelBase, textRelBase;
};

typedef BYTE fde;
extern "C" const fde *_Unwind_Find_FDE(void *ip, dwarf_eh_bases *bases);

//----------------------------------------------------------------------
// Virtual Unwinding
//----------------------------------------------------------------------

#if UNWIND_CONTEXT_IS_UCONTEXT_T
static void WinContextToUnwindContext(CONTEXT *winContext, unw_context_t *unwContext)
{
#if defined(_AMD64_)
    unwContext->uc_mcontext.gregs[REG_RIP] = winContext->Rip;
    unwContext->uc_mcontext.gregs[REG_RSP] = winContext->Rsp;
    unwContext->uc_mcontext.gregs[REG_RBP] = winContext->Rbp;
    unwContext->uc_mcontext.gregs[REG_RBX] = winContext->Rbx;
    unwContext->uc_mcontext.gregs[REG_R12] = winContext->R12;
    unwContext->uc_mcontext.gregs[REG_R13] = winContext->R13;
    unwContext->uc_mcontext.gregs[REG_R14] = winContext->R14;
    unwContext->uc_mcontext.gregs[REG_R15] = winContext->R15;
#else
#error unsupported architecture
#endif
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

static void GetContextPointer(unw_cursor_t *cursor, int reg, PDWORD64 *contextPointer)
{
#if defined(__APPLE__)
    //OSXTODO
#else
    unw_save_loc_t saveLoc;
    unw_get_save_loc(cursor, reg, &saveLoc);
    if (saveLoc.type == UNW_SLT_MEMORY)
    {
        *contextPointer = (PDWORD64)saveLoc.u.addr;
    }
#endif
}

static void GetContextPointers(unw_cursor_t *cursor, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
#if defined(_AMD64_)
    GetContextPointer(cursor, UNW_X86_64_RBP, &contextPointers->Rbp);
    GetContextPointer(cursor, UNW_X86_64_RBX, &contextPointers->Rbx);
    GetContextPointer(cursor, UNW_X86_64_R12, &contextPointers->R12);
    GetContextPointer(cursor, UNW_X86_64_R13, &contextPointers->R13);
    GetContextPointer(cursor, UNW_X86_64_R14, &contextPointers->R14);
    GetContextPointer(cursor, UNW_X86_64_R15, &contextPointers->R15);
#else
#error unsupported architecture
#endif
}

BOOL PAL_VirtualUnwind(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
    int st;
    unw_context_t unwContext;
    unw_cursor_t cursor;

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

    st = unw_step(&cursor);
    if (st < 0)
    {
        return FALSE;
    }

    // Update the passed in windows context to reflect the unwind
    UnwindContextToWinContext(&cursor, context);

    if (contextPointers != NULL)
    {
        GetContextPointers(&cursor, contextPointers);
    }

    return TRUE;
}

#if _DEBUG
//----------------------------------------------------------------------
// Virtual Unwinding Debugging Assertions
//----------------------------------------------------------------------

// Print a trace of virtually unwinding the stack.
// This helps us diagnose non-unwindable stacks.
// Unfortunately, calling this function from gdb will not be very useful,
// since gdb makes it look like it had been called directly from "start"
// (whose unwind info says we reached the end of the stack).
// You can still call it from, say, RaiseTheExceptionInternalOnly.

// (non-static to ease debugging)
void DisplayContext(_Unwind_Context *context)
{
    fprintf(stderr, "  ip =%p", _Unwind_GetIP(context));
    fprintf(stderr, "  cfa=%p", _Unwind_GetCFA(context));
#if defined(_X86_)
    // TODO: display more registers
#endif
    fprintf(stderr, "\n");
}

static _Unwind_Reason_Code PrintVirtualUnwindCallback(_Unwind_Context *context, void *pvParam)
{
    int *pFrameNumber = (int *) pvParam;

    void *ip = _Unwind_GetIP(context);
    const char *module = NULL;
    const char *name = 0;
    int offset = 0;

    Dl_info dl_info;
    if (dladdr(ip, &dl_info))
    {
        module = dl_info.dli_fname;
        if (dl_info.dli_sname)
        {
            name = dl_info.dli_sname;
            offset = (char *) ip - (char *) dl_info.dli_saddr;
        }
        else
        {
            name = "<img-base>";
            offset = (char *) ip - (char *) dl_info.dli_fbase;
        }
    }

    if (module)
        fprintf(stderr, "#%-3d  %s!%s+%d\n", *pFrameNumber, module, name, offset);
    else
        fprintf(stderr, "#%-3d  ??\n", *pFrameNumber);
    DisplayContext(context);
    (*pFrameNumber)++;
    return _URC_NO_REASON;
}

extern "C" void PAL_PrintVirtualUnwind()
{
    BOOL fEntered = PAL_ReenterForEH();

    fprintf(stderr, "\nVirtual unwind of PAL thread %p\n", InternalGetCurrentThread());
    int frameNumber = 0;
    _Unwind_Reason_Code urc = _Unwind_Backtrace(PrintVirtualUnwindCallback, &frameNumber);
    fprintf(stderr, "End of stack (return code=%d).\n", urc);

    if (fEntered)
    {
        PAL_Leave(PAL_BoundaryEH);
    }
}

static const char *PAL_CHECK_UNWINDABLE_STACKS = "PAL_CheckUnwindableStacks";

enum CheckUnwindableStacksMode
{
    // special value to indicate we've not initialized yet
    CheckUnwindableStacks_Uninitialized = -1,

    CheckUnwindableStacks_Off           = 0,
    CheckUnwindableStacks_On            = 1,
    CheckUnwindableStacks_Thorough      = 2,

    CheckUnwindableStacks_Default       = CheckUnwindableStacks_On
};

static CheckUnwindableStacksMode s_mode = CheckUnwindableStacks_Uninitialized;

// A variant of the above.  This one's not meant for CLR developers to use for tracing,
// but implements debug checks to assert stack consistency.
static _Unwind_Reason_Code CheckVirtualUnwindCallback(_Unwind_Context *context, void *pvParam)
{
    void *ip = _Unwind_GetIP(context);

    // If we reach an IP that we cannot find a module for,
    // then we ended up in dynamically-generated code and
    // the stack will not be unwindable past this point.
    Dl_info dl_info;
    if (dladdr(ip, &dl_info) == 0 ||
        ((s_mode == CheckUnwindableStacks_Thorough) &&
        ( dl_info.dli_sname == NULL ||
          ( _Unwind_Find_FDE(ip, NULL) == NULL &&
            strcmp(dl_info.dli_sname, "start") &&
            strcmp(dl_info.dli_sname, "_thread_create_running")))))
    {
        *(BOOL *) pvParam = FALSE;
    }

    return _URC_NO_REASON;
}

extern "C" void PAL_CheckVirtualUnwind()
{
    if (s_mode == CheckUnwindableStacks_Uninitialized)
    {
        const char *checkUnwindableStacks = getenv(PAL_CHECK_UNWINDABLE_STACKS);
        s_mode = checkUnwindableStacks ?
            (CheckUnwindableStacksMode) atoi(checkUnwindableStacks) : CheckUnwindableStacks_Default;
    }

    if (s_mode != CheckUnwindableStacks_Off)
    {
        BOOL fUnwindable = TRUE;
        _ASSERTE(_Unwind_Backtrace(CheckVirtualUnwindCallback, &fUnwindable) == _URC_END_OF_STACK);
        if (!fUnwindable)
        {
            PAL_PrintVirtualUnwind();
            ASSERT("Stack not unwindable.  Throwing may terminate the process.\n");
        }
    }
}
#else // _DEBUG

#define PAL_CheckVirtualUnwind()

#endif // _DEBUG

//----------------------------------------------------------------------
// Registering Vectored Handlers
//----------------------------------------------------------------------

static PVECTORED_EXCEPTION_HANDLER VectoredExceptionHandler = NULL;
static PVECTORED_EXCEPTION_HANDLER VectoredContinueHandler = NULL;

void SetVectoredExceptionHandler(PVECTORED_EXCEPTION_HANDLER pHandler)
{
    PERF_ENTRY(SetVectoredExceptionHandler);
    ENTRY("SetVectoredExceptionHandler(pHandler=%p)\n", pHandler);

    _ASSERTE(VectoredExceptionHandler == NULL);
    VectoredExceptionHandler = pHandler;

    LOGEXIT("SetVectoredExceptionHandler returns\n");
    PERF_EXIT(SetVectoredExceptionHandler);

}

void SetVectoredContinueHandler(PVECTORED_EXCEPTION_HANDLER pHandler)
{
    PERF_ENTRY(SetVectoredContinueHandler);
    ENTRY("SetVectoredContinueHandler(pHandler=%p)\n", pHandler);

    _ASSERTE(VectoredContinueHandler == NULL);
    VectoredContinueHandler = pHandler;

    LOGEXIT("SetVectoredContinueHandler returns\n");
    PERF_EXIT(SetVectoredContinueHandler);
}

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

    PAL_CheckVirtualUnwind();

    EXCEPTION_RECORD exceptionRecord;
    ZeroMemory(&exceptionRecord, sizeof(EXCEPTION_RECORD));

    exceptionRecord.ExceptionCode = dwExceptionCode;
    exceptionRecord.ExceptionFlags = dwExceptionFlags;
    exceptionRecord.ExceptionRecord = NULL;
    exceptionRecord.ExceptionAddress = NULL; // will be set by RtlpRaiseException
    exceptionRecord.NumberParameters = nNumberOfArguments;
    if (nNumberOfArguments)
    {
        if (nNumberOfArguments > EXCEPTION_MAXIMUM_PARAMETERS)
        {
            WARN("Number of arguments (%d) exceeds the limit "
                 "EXCEPTION_MAXIMUM_PARAMETERS (%d); ignoring extra parameters.\n",
                  nNumberOfArguments, EXCEPTION_MAXIMUM_PARAMETERS);
            nNumberOfArguments = EXCEPTION_MAXIMUM_PARAMETERS;
        }
        CopyMemory(exceptionRecord.ExceptionInformation, lpArguments,
                   nNumberOfArguments * sizeof(ULONG_PTR));
    }
    RtlpRaiseException(&exceptionRecord);

    LOGEXIT("RaiseException returns\n");
}
