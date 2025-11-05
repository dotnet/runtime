// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "unwinder.h"

#ifdef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Given the target address of an UNWIND_INFO structure, this function retrieves all the memory used for
// the UNWIND_INFO, including the variable size array of UNWIND_CODE.  The function returns a host copy
// of the UNWIND_INFO.
//
// Arguments:
//    taUnwindInfo - the target address of an UNWIND_INFO
//
// Return Value:
//    Return a host copy of the UNWIND_INFO, including the array of UNWIND_CODE.
//
// Notes:
//    The host copy of UNWIND_INFO is created from DAC memory, which will be flushed when the DAC cache
//    is flushed (i.e. when the debugee is continued).  Thus, the caller doesn't need to worry about freeing
//    this memory.
//
UNWIND_INFO * DacGetUnwindInfo(TADDR taUnwindInfo)
{
    _ASSERTE("S390X: NYI");
    return NULL;
}


//---------------------------------------------------------------------------------------
//
// This function is just a wrapper over OOPStackUnwinder.  The runtime can call this function to
// virtually unwind a CONTEXT out-of-process.
//
// Arguments:
//    pContext - This is an in-out parameter.  On entry, this is the CONTEXT to be unwound.
//               On exit, this is the caller CONTEXT.
//
// Return Value:
//    TRUE if the unwinding is successful
//
// Notes:
//    This function overwrites the specified CONTEXT to store the caller CONTEXT.
//

BOOL DacUnwindStackFrame(CONTEXT * pContext, KNONVOLATILE_CONTEXT_POINTERS* pContextPointers)
{
    BOOL res = OOPStackUnwinderS390X::Unwind(pContext);

    if (res && pContextPointers)
    {
        for (int i = 0; i < 10; i++)
        {
            *(&pContextPointers->R6 + i) = &pContext->R6 + i;
        }
    }

    return res;
}

//---------------------------------------------------------------------------------------
//
// Unwind the given CONTEXT to the caller CONTEXT.  The given CONTEXT will be overwritten.
//
// Arguments:
//    pContext - in-out parameter storing the specified CONTEXT on entry and the unwound CONTEXT on exit
//
// Return Value:
//    TRUE if the unwinding is successful
//

BOOL OOPStackUnwinderS390X::Unwind(CONTEXT * pContext)
{
    _ASSERTE("S390X: NYI");
    return FALSE;
}

#else // DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// This function behaves like the RtlVirtualUnwind in Windows.
// It virtually unwinds the specified function by executing its
// prologue code backward or its epilogue code forward.
//
// If a context pointers record is specified, then the address where each
// nonvolatile registers is restored from is recorded in the appropriate
// element of the context pointers record.
//
// Arguments:
//
//     HandlerType - Supplies the handler type expected for the virtual unwind.
//         This may be either an exception or an unwind handler. A flag may
//         optionally be supplied to avoid epilogue detection if it is known
//         the specified control PC is not located inside a function epilogue.
//
//     ImageBase - Supplies the base address of the image that contains the
//         function being unwound.
//
//     ControlPc - Supplies the address where control left the specified
//         function.
//
//     FunctionEntry - Supplies the address of the function table entry for the
//         specified function.
//
//     ContextRecord - Supplies the address of a context record.
//
//     HandlerData - Supplies a pointer to a variable that receives a pointer
//         the language handler data.
//
//     EstablisherFrame - Supplies a pointer to a variable that receives the
//         the establisher frame pointer value.
//
//     ContextPointers - Supplies an optional pointer to a context pointers
//         record.
//
// Return value:
//
//     The handler routine address.  If control did not leave the specified
//     function in either the prologue or an epilogue and a handler of the
//     proper type is associated with the function, then the address of the
//     language specific exception handler is returned. Otherwise, NULL is
//     returned.
//
PEXCEPTION_ROUTINE RtlVirtualUnwind(
    _In_ ULONG HandlerType,
    _In_ ULONG64 ImageBase,
    _In_ ULONG64 ControlPc,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ OUT PCONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PULONG64 EstablisherFrame,
    __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers
    )
{
    _ASSERTE("S390X: NYI");
    return NULL;
}

#endif // DACCESS_COMPILE
