//
// Copyright (c) Microsoft. All rights reserved.
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

//----------------------------------------------------------------------
// Exception Handling ABI Level I: Base ABI
//----------------------------------------------------------------------

typedef UINT_PTR _Unwind_Ptr;

struct dwarf_eh_bases
{
    _Unwind_Ptr dataRelBase, textRelBase;
};

extern "C" _Unwind_Ptr _Unwind_GetDataRelBase(_Unwind_Context *context);
extern "C" _Unwind_Ptr _Unwind_GetTextRelBase(_Unwind_Context *context);

typedef BYTE fde;
extern "C" const fde *_Unwind_Find_FDE(void *ip, dwarf_eh_bases *bases);

//----------------------------------------------------------------------
// Exception Handling ABI Level II: C++ ABI
//----------------------------------------------------------------------

struct __cxa_exception
{
    std::type_info *exceptionType;
    void (*exceptionDestructor)(void *);
    std::unexpected_handler unexpectedHandler;
    std::terminate_handler terminateHandler;
    __cxa_exception *nextException;

    int handlerCount;
    int handlerSwitchValue;
    const char *actionRecord;
    const char *languageSpecificData;
    void *catchTemp;
    void *adjustedPtr;

    _Unwind_Exception unwindHeader;
};

//----------------------------------------------------------------------
// Virtual Unwinding
//----------------------------------------------------------------------

static void UnwindContextToWinContext(_Unwind_Context *fromContext, CONTEXT *toContext)
{
#if defined(_PPC_)
    // TODO: what about FPR14-FPR31, V20-V31, VRSAVE, CR2-CR4?
    // Technically, these are all callee-saved, but the DWARF unwind info
    // does not seem to track them.
    toContext->Gpr1 = (ULONG) _Unwind_GetCFA(fromContext);
    toContext->Gpr11 = (ULONG) _Unwind_GetGR(fromContext, 0x46);
    for (int i = 13; i <= 31; i++)
    {
        (&toContext->Gpr0)[i] = (ULONG) _Unwind_GetGR(fromContext, i);
    }
    toContext->Lr = (ULONG) _Unwind_GetGR(fromContext, 0x41);
    toContext->Iar = (ULONG) _Unwind_GetIP(fromContext);
#elif defined(_X86_)
    toContext->Ebx = (ULONG) _Unwind_GetGR(fromContext, 3);
    toContext->Ebp = (ULONG) _Unwind_GetGR(fromContext, 4);
    toContext->Esp = (ULONG) _Unwind_GetCFA(fromContext);
    toContext->Esi = (ULONG) _Unwind_GetGR(fromContext, 6);
    toContext->Edi = (ULONG) _Unwind_GetGR(fromContext, 7);
    toContext->Eip = (ULONG) _Unwind_GetIP(fromContext);
#elif defined(_AMD64_)
    toContext->Rbx = (SIZE_T)_Unwind_GetGR(fromContext, 3);
    toContext->Rbp = (SIZE_T)_Unwind_GetGR(fromContext, 6);
    toContext->Rsp = (SIZE_T)_Unwind_GetCFA(fromContext);
    toContext->Rip = (SIZE_T)_Unwind_GetIP(fromContext);
    
    // No need to restore RDI/RSI since they are considered volatile per the GCC64 calling convention.
    //
    // NOTE: Attemptin to fetch these two registers will result in a crash on 10.5.8 but will "work" (i.e.
    // not crash) on 10.6.5. Since we dont need them, we dont bother.
    
    // Restore the extended non-volatile integer registers
    DWORD64 *pReg = &toContext->R12;
    for (int i = 12; i <= 15; i++, pReg++)
        *pReg = (SIZE_T)_Unwind_GetGR(fromContext, i);
        
    // TODO: neither x86 or AMD64 code current supports vector register contexts
#else
#error unsupported architecture
#endif
}

struct VirtualUnwindParam
{
    // Summarizes the unwinding work that still needs to be done.
    // If this is > 0, indicates the number of frames to unwind
    // after we find the starting context (identified by its CFA).
    // If this is < 0, we already found the starting context and
    // we're counting until we reach 0.
    int nFramesToUnwind;

    // CFA of the context from which to count the desired unwind depth.
    void *cfa;

    // Pointer to a region of memory into which we should store the
    // context of the target frame.
    CONTEXT *context;
};

static _Unwind_Reason_Code VirtualUnwindCallback(_Unwind_Context *context, void *pvParam)
{
    VirtualUnwindParam *param = (VirtualUnwindParam *) pvParam;
    if (param->nFramesToUnwind > 0)
    {
        // Stil looking for the starting context.
        if (_Unwind_GetCFA(context) == param->cfa)
        {
            // This is the starting context.
            param->nFramesToUnwind = -param->nFramesToUnwind;
        }
    }
    else
    {
        // We've found the starting context; unwind as many frames as requested.
        param->nFramesToUnwind++;
        if (param->nFramesToUnwind == 0)
        {
            // We've unwound all frames requested.
            UnwindContextToWinContext(context, param->context);
            return _URC_NORMAL_STOP;
        }
    }
    return _URC_NO_REASON;
}

static BOOL VirtualUnwind(CONTEXT *context, int nFramesToUnwind)
{
    VirtualUnwindParam param;
    param.nFramesToUnwind = nFramesToUnwind;
#if defined(_PPC_)
    param.cfa = (void *) context->Gpr1;
#elif defined(_X86_)
    param.cfa = (void *) context->Esp;
#elif defined(_AMD64_)
    param.cfa = (void *) context->Rsp;
#else
#error unsupported architecture
#endif
    param.context = context;
    _Unwind_Backtrace(VirtualUnwindCallback, &param);
    return param.nFramesToUnwind == 0;
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
    fprintf(stderr, "  ip =0x%p", _Unwind_GetIP(context));
    fprintf(stderr, "  cfa=0x%p", _Unwind_GetCFA(context));
#if defined(_X86_)
    // TODO: display more registers
#elif defined(_PPC_)
    fprintf(stderr, "  ra =0x%p", _Unwind_GetGR(context, 0x41));
    fprintf(stderr, "  r11=0x%p\n", _Unwind_GetGR(context, 0x46));
    for (int i = 13; i < 32; i++)
    {
        fprintf(stderr, "  r%02d=0x%p", i, _Unwind_GetGR(context, i));
        if ((i - 13) % 4 == 3)
            fprintf(stderr, "\n");
    }
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
            name = dl_info.dli_sname - 1;
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
            strcmp(dl_info.dli_sname - 1, "start") &&
            strcmp(dl_info.dli_sname - 1, "_thread_create_running")))))
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

//----------------------------------------------------------------------
// Representation of an SEH Exception as a C++ object
//----------------------------------------------------------------------

struct PAL_SEHException
{
public:
    // Note that the following two are actually embedded in this heap-allocated
    // instance - in contrast to Win32, where the exception record would usually
    // be allocated on the stack.  This is needed because foreign cleanup handlers
    // partially unwind the stack on the second pass.
    EXCEPTION_POINTERS ExceptionPointers;
    EXCEPTION_RECORD ExceptionRecord;
    CONTEXT ContextRecord;

    PAL_SEHException(EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pContextRecord)
    {
        ExceptionPointers.ExceptionRecord = &ExceptionRecord;
        ExceptionPointers.ContextRecord = &ContextRecord;
        ExceptionRecord = *pExceptionRecord;
        ContextRecord = *pContextRecord;
        nestedExceptionEstablisherFrame = NULL;
    }

    static PAL_SEHException *FromExceptionObject(_Unwind_Exception *exceptionObject)
    {
        if (exceptionObject->exception_class == Class)
        {
            if (*((__cxa_exception *) (exceptionObject + 1) - 1)->exceptionType == typeid(PAL_SEHException))
            {
                return (PAL_SEHException *) (exceptionObject + 1);
            }
        }
        return NULL;
    }

    void NestedIn(_Unwind_Context *context)
    {
        nestedExceptionEstablisherFrame = _Unwind_GetCFA(context);
        ExceptionRecord.ExceptionFlags |= EXCEPTION_NESTED_CALL;
    }

    void RanThroughFilter(_Unwind_Context *context)
    {
        if (nestedExceptionEstablisherFrame == _Unwind_GetCFA(context))
        {
            // We were processing a nested exception, and we have repeated
            // the search phase past the filter that threw the nested exception.
            // Thus, clear the corresponding flag now.
            ExceptionRecord.ExceptionFlags &= ~EXCEPTION_NESTED_CALL;
        }
    }

private:
#ifdef __GNUC__
    static const __uint64_t Class = 0x474e5543432b2b00ULL; // vendor = GNUC, language = C++\0
#else
#error Vendor code not defined for this platform.
#endif

    // If we encountered a nested exception, the EXCEPTION_NESTED_CALL flag must
    // remain set on the exception while unwinding to the frame with the CFA
    // stored below.  Note that this will always be a PAL_TryExcept frame.
    void *nestedExceptionEstablisherFrame;
};

EXCEPTION_POINTERS *
PALAPI
PAL_GetExceptionPointers(_Unwind_Exception *exceptionObject)
{
    PAL_SEHException *pSEHException = PAL_SEHException::FromExceptionObject(exceptionObject);
    if (pSEHException)
    {
        return &pSEHException->ExceptionPointers;
    }
    return NULL;
}

//----------------------------------------------------------------------
// Raising Exceptions
//----------------------------------------------------------------------

static void DeleteThrownException(_Unwind_Exception *exceptionObject)
{
    // The argument exception has been thrown.
    _ASSERTE(std::uncaught_exception());

    // Deleting it in this way will adjust the uncaught exceptions count.
    __cxa_begin_catch(exceptionObject);
    __cxa_end_catch();
}

static void RunVectoredHandler(EXCEPTION_POINTERS *pExceptionPointers, _Unwind_Exception *exceptionObject, PVECTORED_EXCEPTION_HANDLER pHandler)
{
    if (pHandler != NULL)
    {
        EXCEPTION_DISPOSITION disposition = pHandler(pExceptionPointers);
        switch (disposition)
        {
        case EXCEPTION_CONTINUE_EXECUTION:
            {
                BOOL fNonContinuable = pExceptionPointers->ExceptionRecord->ExceptionFlags & EXCEPTION_NONCONTINUABLE;
                CONTEXT *context = pExceptionPointers->ContextRecord;
                if (fNonContinuable)
                {
                    RaiseException(EXCEPTION_NONCONTINUABLE_EXCEPTION,
                                   EXCEPTION_NONCONTINUABLE, 0, NULL);
                }
                else
                {
                    SetThreadContext(PAL_GetCurrentThread(), context);
                }
            }
            abort(); // should never reach here
        case EXCEPTION_CONTINUE_SEARCH:
            break;
        default:
            DeleteThrownException(exceptionObject);
            RaiseException(EXCEPTION_INVALID_DISPOSITION,
                           EXCEPTION_NONCONTINUABLE, 0, NULL);
            abort(); // should never reach here
        }
    }
}

PAL_NORETURN
static void RtlpRaiseException(EXCEPTION_RECORD *ExceptionRecord) 
{
    // Capture the context of RtlpRaiseException.
    CONTEXT ContextRecord;
    ZeroMemory(&ContextRecord, sizeof(CONTEXT));
    ContextRecord.ContextFlags = CONTEXT_FULL;
    CONTEXT_CaptureContext(&ContextRecord);

    // Find the caller of RtlpRaiseException.  This provides the exact context
    // that handlers expect to see, which is the one they would want to fix up
    // to resume after a continuable exception.
    VirtualUnwind(&ContextRecord, 1);

    // The frame we're looking at now is either RaiseException or PAL_TryExcept.
    // If it's RaiseException, we have to unwind one level further to get the
    // actual context user code could be resumed at.
#if defined(_PPC_)
    void *pc = (void *) ContextRecord.Iar;
#elif defined(_X86_)
    void *pc = (void *) ContextRecord.Eip;
#elif defined(_AMD64_)
    void *pc = (void *) ContextRecord.Rip;
#else
#error unsupported architecture
#endif
    if ((SIZE_T) pc - (SIZE_T) RaiseException < (SIZE_T) pc - (SIZE_T) PAL_TryExcept)
    {
        VirtualUnwind(&ContextRecord, 1);
#if defined(_PPC_)
        pc = (void *) ContextRecord.Iar;
#elif defined(_X86_)
        pc = (void *) ContextRecord.Eip;
#elif defined(_AMD64_)
        pc = (void *) ContextRecord.Rip;
#else
#error unsupported architecture
#endif
    }
    ExceptionRecord->ExceptionAddress = pc;

    EXCEPTION_POINTERS pointers;
    pointers.ExceptionRecord = ExceptionRecord;
    pointers.ContextRecord = &ContextRecord;

    SEHRaiseException(InternalGetCurrentThread(), &pointers, 0);
}

PAL_NORETURN
static void RaiseExceptionObject(_Unwind_Exception *exceptionObject)
{
    // Dummy exception record for use by foreign exceptions.
    EXCEPTION_RECORD ForeignExceptionRecord = { EXCEPTION_FOREIGN, EXCEPTION_NONCONTINUABLE, NULL, NULL, 0 };
    EXCEPTION_POINTERS ForeignExceptionPointers = { &ForeignExceptionRecord, NULL };

    bool fSkipVEH = false;
    
    EXCEPTION_POINTERS *pExceptionPointers = PAL_GetExceptionPointers(exceptionObject);
    if (!pExceptionPointers)
    {
        pExceptionPointers = &ForeignExceptionPointers;
    }
    else
    {
        fSkipVEH =(pExceptionPointers->ExceptionRecord->ExceptionFlags & EXCEPTION_SKIP_VEH)?true:false;
        if (!fSkipVEH)
        {
            // For exceptions that we know about, keep a reference in the corresponding
            // EXCEPTION_RECORD incase this turns out to be an async exception in managed code.
            pExceptionPointers->ExceptionRecord->ExceptionInformation[NATIVE_EXCEPTION_ASYNC_SLOT] = (ULONG_PTR)exceptionObject;
        }
    }
    
    if (!fSkipVEH)
    {
        RunVectoredHandler(pExceptionPointers, exceptionObject, VectoredExceptionHandler);
    }

    _Unwind_Reason_Code urc;
    PAL_Leave(PAL_BoundaryEH);
    urc = _Unwind_RaiseException(exceptionObject);

    // _Unwind_RaiseException is supposed to return an _Unwind_Reason_Code.
    // However, it's not implemented according to spec.  So we always have
    // to assume that we got _URC_END_OF_STACK.
    if (urc == (_Unwind_Reason_Code) (UINT_PTR) exceptionObject)
    {
        urc = _URC_END_OF_STACK;
    }
    _ASSERTE(urc == _URC_END_OF_STACK);

    PAL_Reenter(PAL_BoundaryEH);
    RunVectoredHandler(pExceptionPointers, exceptionObject, VectoredContinueHandler);

    DeleteThrownException(exceptionObject);

    WARN("unhandled exception; terminating self\n");

    abort();
    // unreached
}

static void ThrowHelper(void *pvParam)
{
    PEXCEPTION_POINTERS lpExceptionPointers = (PEXCEPTION_POINTERS) pvParam;
    PAL_Leave(PAL_BoundaryEH);
    throw PAL_SEHException(lpExceptionPointers->ExceptionRecord, lpExceptionPointers->ContextRecord);
}

static void RethrowHelper(void *pvParam)
{
    PAL_Leave(PAL_BoundaryEH);
    throw;
}

static EXCEPTION_DISPOSITION ThrowFilter(
    EXCEPTION_POINTERS *ExceptionPointers,
    PAL_DISPATCHER_CONTEXT *DispatcherContext,
    void *pvParam)
{
    return EXCEPTION_EXECUTE_HANDLER;
}

PAL_NORETURN
VOID
PALAPI
PAL_CppRethrow()
{
    // Throw the exception using C++ throw, but intercept the exception so that
    // we can raise it using our runtime, which supported vectored handlers.
    BOOL fExecuteHandler;
    _Unwind_Exception *exceptionObject =
        PAL_TryExcept(RethrowHelper, ThrowFilter, NULL, &fExecuteHandler);
    _ASSERTE(exceptionObject != NULL);
    PAL_CheckVirtualUnwind();
    RaiseExceptionObject(exceptionObject);
}

PAL_NORETURN
void SEHRaiseException(CPalThread *pthrCurrent, 
                       PEXCEPTION_POINTERS lpExceptionPointers, 
                       int signal_code)
{
    _Unwind_Exception *exceptionObject = NULL;
    
    if (lpExceptionPointers->ExceptionRecord->ExceptionFlags & EXCEPTION_SKIP_VEH)
    {
        // If we are going to skip VEH, then it implies we are going to dispatch
        // an async exception in managed code. For this case, extract the
        // native exception object from the ExceptionInformation array that was saved there
        // when PAL_DispatchException had initially raised the exception.
        exceptionObject = (_Unwind_Exception *)lpExceptionPointers->ExceptionRecord->ExceptionInformation[NATIVE_EXCEPTION_ASYNC_SLOT];
        lpExceptionPointers->ExceptionRecord->ExceptionInformation[NATIVE_EXCEPTION_ASYNC_SLOT] = (ULONG_PTR)NULL;
    }
    
    if (exceptionObject == NULL)
    {
        // Throw the exception using C++ throw, but intercept the exception so that
        // we can raise it using our runtime, which supported vectored handlers.
        BOOL fExecuteHandler;
        exceptionObject = PAL_TryExcept(ThrowHelper, ThrowFilter, lpExceptionPointers, &fExecuteHandler);
    }

    _ASSERTE(exceptionObject != NULL);
    
    // Note: We cannot call PAL_CheckVirtualUnwind here, since we this function
    // may be called for a fault in dynamically-generated code.

#if defined(_AMD64_)
    // When PAL_TryExcept returns, it has executed the second pass for handling the exception
    // that was raised by ThrowHelper. As part of processing the second pass, PAL_RunHandler
    // would have set the EXCEPTION_UNWINDING flag (and possibly, even the EXCEPTION_TARGET_UNWIND flag)
    // in the ExceptionRecord contained inside the exception object. 
    //
    // When this exception object is passed to the RunVectoredExceptionHandler, the exception pointers
    // passed to the vectored handler would be the one extracted from the exception object. Since that
    // would have the unwinding flag set, any code that checks for the flag to conditional process the
    // exception would go awry.
    //
    // To fix this problem, we will fetch the ExceptionPointers from the exception object and clear off
    // any such flags.
    //
    // Note 1: This problem is also present when the exception object is seen by the various native
    //         personality routines (e.g. UnwindThunkPersonality). However, stack frame based handling
    //         of exceptions is not affected by this problem since all of our native personality routines
    //         clear off the existing flags and reset them again based upon the active phase (first or second)
    //         of exception dispatch before passing the exception record to CLR's managed personality routines.
    //
    // Note 2: This problem is also applicable to Mac X86. TODO: Fix this problem for MacX86.
    EXCEPTION_POINTERS *pExceptionPointers = PAL_GetExceptionPointers(exceptionObject);
    _ASSERTE(pExceptionPointers != NULL);
    
    // Clear of the EXCEPTION_UNWINDING/EXCEPTION_TARGET_UNWIND flags
    pExceptionPointers->ExceptionRecord->ExceptionFlags =
    (pExceptionPointers->ExceptionRecord->ExceptionFlags & ~(EXCEPTION_UNWINDING|EXCEPTION_TARGET_UNWIND));
#endif // defined(_AMD64_)

    RaiseExceptionObject(exceptionObject);
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

PAL_NORETURN
VOID
PALAPI
PAL_RaiseException(
           IN PEXCEPTION_POINTERS ExceptionPointers)
{
    PAL_CheckVirtualUnwind();
    ExceptionPointers->ExceptionRecord->ExceptionFlags |= EXCEPTION_NONCONTINUABLE;
    SEHRaiseException(InternalGetCurrentThread(), ExceptionPointers, 0);
}

//----------------------------------------------------------------------
// SEH Personality
//----------------------------------------------------------------------

#if defined(__LINUX__) || defined(__APPLE__)
// TODO: Enable these routines for Linux.
EXCEPTION_DISPOSITION
PAL_RunFilter(
    PEXCEPTION_POINTERS ExceptionPointers,
    PAL_DISPATCHER_CONTEXT *DispatcherContext,
    void *pvParam,
    PFN_PAL_EXCEPTION_FILTER Filter)
{
    _ASSERT(FALSE);
    return 0;
}

void PAL_CallRunHandler()
{
    _ASSERT(FALSE);
}

struct _Unwind_Exception *PAL_TryExcept(
    PFN_PAL_BODY pfnBody,
    PFN_PAL_EXCEPTION_FILTER pfnFilter,
    void *pvParam,
    BOOL *pfExecuteHandler)
{
    // UNIXTODO: Exception handling
    pfnBody(pvParam);
    *pfExecuteHandler = FALSE; 
    return NULL;
}
#else
// from runfilter.s
extern "C"
EXCEPTION_DISPOSITION
PAL_RunFilter(PEXCEPTION_POINTERS ExceptionPointers,
              PAL_DISPATCHER_CONTEXT *DispatcherContext,
              void *pvParam,
              PFN_PAL_EXCEPTION_FILTER Filter);
extern "C" void PAL_CallRunHandler();
#endif // __LINUX__

extern "C"
_Unwind_Reason_Code PAL_SEHPersonalityRoutine(
    int version,
    _Unwind_Action actions,
    __uint64_t exceptionClass,
    _Unwind_Exception *exceptionObject,
    _Unwind_Context *context)
{
    _Unwind_Reason_Code urc = _URC_NO_REASON;
    PAL_Reenter(PAL_BoundaryEH);

    TRACE("actions=%x\n", actions);
    _ASSERTE(version == 1);

    // Determine what state the frame is in for which this personality routine
    // was invoked.  The only function this personality routine is affiliated
    // with is PAL_TryExcept, which is assembly code.  That function contains
    // two calls: one to run the try block, one to run a handler.  The
    // PAL_CallRunHandler label sits in-between these two, thus we can
    // use it to make the distinction which call the exception escaped from.
    if (_Unwind_GetIP(context) > (void *) PAL_CallRunHandler)
    {
        // This personality routine had invoked the handler associated
        // with this frame, and the handler raised an exception.
        // This is called a "collided unwind".
        TRACE("collided unwind\n");
        PAL_SEHException *pSEHException = PAL_SEHException::FromExceptionObject(exceptionObject);
        if (pSEHException)
        {
            pSEHException->ExceptionRecord.ExceptionFlags |= EXCEPTION_COLLIDED_UNWIND;
        }
        urc = _URC_CONTINUE_UNWIND;
        goto exit;
    }

    if ((actions & _UA_PHASE_MASK) == _UA_SEARCH_PHASE)
    {
        // Determine whether this is an SEH exception or a foreign exception.
        PAL_SEHException *pSEHException = PAL_SEHException::FromExceptionObject(exceptionObject);
        if (pSEHException)
        {
            pSEHException->ExceptionRecord.ExceptionFlags &= ~EXCEPTION_UNWIND;
        }

        // Obtain the filter and parameter from the original frame.
        PFN_PAL_EXCEPTION_FILTER pfnFilter;
        void *pvParam;
#if defined(_PPC_)
        pfnFilter = (PFN_PAL_EXCEPTION_FILTER) _Unwind_GetGR(context, 28);
        pvParam = _Unwind_GetGR(context, 29);
#elif defined(_X86_)
        pfnFilter = ((PFN_PAL_EXCEPTION_FILTER *) _Unwind_GetGR(context, 4))[3]; // [ebp+12]
        pvParam = ((void **) _Unwind_GetGR(context, 4))[4]; // [ebp+16]
#elif defined(_AMD64_)
        // Filter address is stored at RSP+8
        // pvParam is stored at RSP+16
        // Refer to PAL_TryExcept implementation for details.
        pfnFilter = ((PFN_PAL_EXCEPTION_FILTER *) _Unwind_GetCFA(context))[1]; 
        pvParam = ((void **) _Unwind_GetCFA(context))[2]; 
#else
#error unsupported architecture
#endif

        // Make some of our state available to the filter.
        PAL_DISPATCHER_CONTEXT dispatcherContext;
        dispatcherContext.actions = actions;
        dispatcherContext.exception_object = exceptionObject;
        dispatcherContext.context = context;

        // Dummy exception record for use by foreign exceptions.
        EXCEPTION_RECORD ForeignExceptionRecord = { EXCEPTION_FOREIGN, EXCEPTION_NONCONTINUABLE, NULL, NULL, 0 };
        EXCEPTION_POINTERS ForeignExceptionPointers = { &ForeignExceptionRecord, NULL };

        // Run the filter.  If this throws, the PAL_SEHFilterPersonalityRoutine
        // will be invoked.
        EXCEPTION_DISPOSITION disposition =
            PAL_RunFilter(pSEHException ? &pSEHException->ExceptionPointers : &ForeignExceptionPointers,
                          &dispatcherContext,
                          pvParam, pfnFilter);
        TRACE("filter returned %d\n", disposition);

        if (pSEHException)
        {
            pSEHException->RanThroughFilter(context);
        }

        switch (disposition)
        {
        case EXCEPTION_CONTINUE_EXECUTION:
            if (!pSEHException || // foreign exceptions are never continuable
                (pSEHException->ExceptionRecord.ExceptionFlags & EXCEPTION_NONCONTINUABLE))
            {
                DeleteThrownException(exceptionObject);
                RaiseException(EXCEPTION_NONCONTINUABLE_EXCEPTION,
                               EXCEPTION_NONCONTINUABLE, 0, NULL);
            }
            else
            {
                CONTEXT *newContext = pSEHException->ExceptionPointers.ContextRecord;
                DeleteThrownException(exceptionObject);
                SetThreadContext(PAL_GetCurrentThread(), newContext);
            }
            abort(); // should never reach here
        case EXCEPTION_EXECUTE_HANDLER:
            urc = _URC_HANDLER_FOUND;
            goto exit;
        case EXCEPTION_CONTINUE_SEARCH:
            urc = _URC_CONTINUE_UNWIND;
            goto exit;
        default:
            DeleteThrownException(exceptionObject);
            RaiseException(EXCEPTION_INVALID_DISPOSITION,
                           EXCEPTION_NONCONTINUABLE, 0, NULL);
            // The above call should never return, but the compiler doesn't know that.
            urc = _URC_FATAL_PHASE1_ERROR;
            goto exit;
        }
    }
    else
    {
        _ASSERTE((actions & _UA_PHASE_MASK)  == _UA_CLEANUP_PHASE);

        // There is a cleanup to run.  Here we behave differently from
        // Windows SEH:  We unwind the stack up to the frame that installed
        // this personality routine, and run the handler from there.
        // The advantage is that this is consistent with C++ on this
        // platform, wich unwinds the stack also to run destructors for
        // objects allocated on the stack.  By behaving in the same way,
        // we can deal with C++ exceptions colliding with SEH unwinds.
#if defined(_PPC_)
        _Unwind_SetGR(context, 3, (void *) actions);
        _Unwind_SetGR(context, 4, exceptionObject);
#elif defined(_X86_) 
        void **args = (void **) _Unwind_GetCFA(context);
        args[0] = (void *) actions;
        args[1] = exceptionObject;
#elif defined(_AMD64_)
        // We have allocated two slots on the stack for preserving these two.
        // They are beyond the locations where we spilled the args to PAL_TryExcept
        // and hence, are at RSP+32 and RSP+40 respectively.
        void **args = (void **) _Unwind_GetCFA(context);
        args[4] = (void *) actions;
        args[5] = exceptionObject;
#else
#error unsupported architecture
#endif
        TRACE("unwinding to ip=%p, cfa=%p\n", _Unwind_GetIP(context), _Unwind_GetCFA(context));

        // Fix the context to invoke PAL_CallRunHandler
        _Unwind_SetIP(context, (void *) PAL_CallRunHandler);
        urc = _URC_INSTALL_CONTEXT;
        goto exit;
    }

exit:
    _ASSERTE(urc != _URC_NO_REASON);
    if (urc != _URC_INSTALL_CONTEXT)
    {
        // If we're installing a context, it's because we want to run
        // code that depends on the PAL.  Otherwise, bye-bye.
        PAL_Leave(PAL_BoundaryEH);
    }
    return urc;
}

extern "C"
_Unwind_Exception *PAL_RunHandler(
    _Unwind_Action actions,
    _Unwind_Exception *exceptionObject,
    PFN_PAL_EXCEPTION_FILTER pfnFilter,
    void *pvParam,
    BOOL *pfExecuteHandler)
{
    _ASSERTE((actions & _UA_PHASE_MASK) == _UA_CLEANUP_PHASE);

    PAL_SEHException *pSEHException = PAL_SEHException::FromExceptionObject(exceptionObject);

    PAL_DISPATCHER_CONTEXT dispatcherContext;
    dispatcherContext.actions = actions;
    dispatcherContext.exception_object = exceptionObject;
    dispatcherContext.context = NULL;

    // Dummy exception record for use by foreign exceptions.
    EXCEPTION_RECORD ForeignExceptionRecord = { EXCEPTION_FOREIGN, EXCEPTION_NONCONTINUABLE, NULL, NULL, 0 };
    EXCEPTION_POINTERS ForeignExceptionPointers = { &ForeignExceptionRecord, NULL };
    EXCEPTION_RECORD *pExceptionRecord = &ForeignExceptionRecord;
    if (pSEHException)
    {
        pExceptionRecord = &pSEHException->ExceptionRecord;
    }

    pExceptionRecord->ExceptionFlags =
            (pExceptionRecord->ExceptionFlags & ~(EXCEPTION_UNWINDING|EXCEPTION_TARGET_UNWIND)) |
            EXCEPTION_UNWINDING | ((actions & _UA_HANDLER_FRAME) ? EXCEPTION_TARGET_UNWIND : 0);

    EXCEPTION_DISPOSITION disposition;
    PAL_CPP_TRY
    {
        // MACTODO: Why do we invoke the PAL_EXCEPT macro's filter in the 
        // unwind pass when it simply returns with the original disposition that
        // was provided in the first pass?
        disposition = pfnFilter(pSEHException ? &pSEHException->ExceptionPointers : &ForeignExceptionPointers,
                                &dispatcherContext, pvParam);
    }
    PAL_CPP_CATCH_ALL
    {
        // In case of a collided unwind, delete the original exception
        // and propagate the new one instead.
        _Unwind_DeleteException(exceptionObject);
        PAL_CPP_RETHROW;
    }
    PAL_CPP_ENDTRY

    if ((actions & _UA_HANDLER_FRAME) && disposition == EXCEPTION_EXECUTE_HANDLER)
    {
        // Return to PAL_TryExcept, which will return the exception that
        // occurred, so that the PAL_EXCEPT/PAL_FINALLY body will be run.
        *pfExecuteHandler = TRUE;
        return exceptionObject;
    }
    else if (~(actions & _UA_HANDLER_FRAME) && disposition == EXCEPTION_CONTINUE_SEARCH)
    {
        // Cleanups have been run for this frame; continue running cleanups
        // up the stack.
        *pfExecuteHandler = FALSE;
        return exceptionObject;
    }
    else
    {
        // This filter misbehaved; it claimed it would handle/not handle
        // the exception, but it didn't/did.
        DeleteThrownException(exceptionObject);
        RaiseException(EXCEPTION_INVALID_DISPOSITION,
                       EXCEPTION_NONCONTINUABLE, 0, NULL);
        // The above call should never return, but the compiler doesn't know that.
        return NULL;
    }
}

//----------------------------------------------------------------------
// SEH Filter Personality
//----------------------------------------------------------------------

extern "C"
_Unwind_Reason_Code PAL_SEHFilterPersonalityRoutine(
    int version,
    _Unwind_Action actions,
    __uint64_t exceptionClass,
    _Unwind_Exception *exceptionObject,
    _Unwind_Context *context)
{
    PAL_Reenter(PAL_BoundaryEH);

    _ASSERTE(version == 1);

    // When this personality routine runs, we have two activations
    // of _Unwind_RaiseException: The outer _Unwind_RaiseException
    // was running a filter in the search phase, and that filter
    // threw an exception.  This is called a "nested exception".
    // The inner _Unwind_RaiseException activation is dispatching
    // the nested exception, and called this routine.
    TRACE("actions=%x\n", actions);

    // Retrieve the dispatcher context of the outer _Unwind_RaiseException.
    PAL_DISPATCHER_CONTEXT *outerDispatcherContext;
#if defined(_PPC_)
    outerDispatcherContext = (PAL_DISPATCHER_CONTEXT *) _Unwind_GetGR(context, 29);
#elif defined(_X86_)
    outerDispatcherContext = (PAL_DISPATCHER_CONTEXT *) ((void **) _Unwind_GetGR(context, 4))[3]; // [ebp+12]
#elif defined(_AMD64_)
    // Filter address is stored at RSP+8
    // Refer to PAL_RunFilter implementation for details.
    outerDispatcherContext = (PAL_DISPATCHER_CONTEXT *)((void **)_Unwind_GetCFA(context))[1]; 
#else
#error unsupported architecture
#endif
    _ASSERTE(outerDispatcherContext->actions & _UA_SEARCH_PHASE);

    if ((actions & _UA_PHASE_MASK) == _UA_SEARCH_PHASE)
    {
        TRACE("nested exception\n");
        PAL_SEHException *pSEHException = PAL_SEHException::FromExceptionObject(exceptionObject);
        if (pSEHException)
        {
            pSEHException->NestedIn(outerDispatcherContext->context);
        }
        else
        {
            // A foreign exception escaped from a filter.  This is not a
            // supported action for a filter to take (neither in our SEH
            // model, nor in the unwind library's model).  Let the system
            // execute its (unspecified) behavior.
        }
        PAL_Leave(PAL_BoundaryEH);
        return _URC_CONTINUE_UNWIND;
    }
    else
    {
        _ASSERTE((actions & _UA_PHASE_MASK) == _UA_CLEANUP_PHASE);

        // Rethrowing the exception itself from a filter is not supported.
        // (That would mess up reference counting of the exception object
        // in standard C++.)
        _ASSERT(outerDispatcherContext->exception_object != exceptionObject);

        // We're in the cleanup phase of a nested exception.  This means
        // that nobody fixed up the context of that exception and resumed
        // execution.  In other words, we'll never resume the outer unwind
        // and we have to clean up its state.
        _Unwind_DeleteException(outerDispatcherContext->exception_object);

        // No cleanup to run for a nested exception.
        PAL_Leave(PAL_BoundaryEH);
        return _URC_CONTINUE_UNWIND;
    }
}

#ifdef _PPC_
// This function does not do anything.  It ist just here to be called by
// PAL_TRY, so we can avoid the body of PAL_TRY being translated by the
// compiler into a leaf function (i.e., one that does not set up its
// own frame), because on a hardware fault in a leaf function, we would
// get a stack that would not be unwindable.
EXTERN_C VOID PALAPI PAL_DummyCall()
{
}
#endif // _PPC_
