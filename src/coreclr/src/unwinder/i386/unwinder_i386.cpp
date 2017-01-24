// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

#include "stdafx.h"
#include "unwinder_i386.h"

#ifdef WIN64EXCEPTIONS
/*++

Routine Description:

    This function virtually unwinds the specified function by executing its
    prologue code backward or its epilogue code forward.

    If a context pointers record is specified, then the address where each
    nonvolatile registers is restored from is recorded in the appropriate
    element of the context pointers record.

Arguments:

    HandlerType - Supplies the handler type expected for the virtual unwind.
        This may be either an exception or an unwind handler. A flag may
        optionally be supplied to avoid epilogue detection if it is known
        the specified control PC is not located inside a function epilogue.

    ImageBase - Supplies the base address of the image that contains the
        function being unwound.

    ControlPc - Supplies the address where control left the specified
        function.

    FunctionEntry - Supplies the address of the function table entry for the
        specified function.

    ContextRecord - Supplies the address of a context record.


    HandlerData - Supplies a pointer to a variable that receives a pointer
        the the language handler data.

    EstablisherFrame - Supplies a pointer to a variable that receives the
        the establisher frame pointer value.

    ContextPointers - Supplies an optional pointer to a context pointers
        record.

    HandlerRoutine - Supplies an optional pointer to a variable that receives
        the handler routine address.  If control did not leave the specified
        function in either the prologue or an epilogue and a handler of the
        proper type is associated with the function, then the address of the
        language specific exception handler is returned. Otherwise, NULL is
        returned.
--*/
HRESULT
OOPStackUnwinderX86::VirtualUnwind(
    __in DWORD HandlerType,
    __in DWORD ImageBase,
    __in DWORD ControlPc,
    __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PCONTEXT ContextRecord,
    __out PVOID *HandlerData,
    __out PDWORD EstablisherFrame,
    __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
    __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine
    )
{
    *EstablisherFrame = ContextRecord->Esp;
    if (HandlerRoutine != NULL)
    {
        *HandlerRoutine = NULL;
    }

    REGDISPLAY rd;

    if (ContextPointers != NULL)
    {
#define CALLEE_SAVED_REGISTER(reg) rd.p##reg = ContextPointers->reg;
        ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
    }
    else
    {
#define CALLEE_SAVED_REGISTER(reg) rd.p##reg = NULL;
        ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
    }

    if (rd.pEbp == NULL)
    {
        rd.pEbp = &(ContextRecord->Ebp);
    }
    rd.SP = ContextRecord->Esp;
    rd.ControlPC = (PCODE)(ContextRecord->Eip);
    rd.PCTAddr = (UINT_PTR)&(ContextRecord->Eip);

    CodeManState codeManState;
    codeManState.dwIsSet = 0;

    EECodeInfo codeInfo;
    codeInfo.Init((PCODE) ControlPc);

    if (!UnwindStackFrame(&rd, &codeInfo, UpdateAllRegs, &codeManState, NULL))
    {
        return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
    }

#define CALLEE_SAVED_REGISTER(reg) if (rd.p##reg != NULL) { ContextRecord->reg = *rd.p##reg; }
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
    
    if (ContextPointers != NULL) 
    {
#define CALLEE_SAVED_REGISTER(reg) if (rd.p##reg != &(ContextRecord->reg)) { ContextPointers->reg = rd.p##reg; }
        ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
    }

    ContextRecord->Esp = rd.SP;
    ContextRecord->Eip = rd.ControlPC;
    ContextRecord->Ebp = *rd.pEbp;

    return S_OK;
}

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
//         the the language handler data.
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
EXTERN_C
NTSYSAPI
PEXCEPTION_ROUTINE
NTAPI
RtlVirtualUnwind (
    __in DWORD HandlerType,
    __in DWORD ImageBase,
    __in DWORD ControlPc,
    __in PRUNTIME_FUNCTION FunctionEntry,
    __inout PT_CONTEXT ContextRecord,
    __out PVOID *HandlerData,
    __out PDWORD EstablisherFrame,
    __inout_opt PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers
    )
{
    PEXCEPTION_ROUTINE handlerRoutine;

    HRESULT res = OOPStackUnwinderX86::VirtualUnwind(
        HandlerType,
        ImageBase,
        ControlPc,
        (_PIMAGE_RUNTIME_FUNCTION_ENTRY)FunctionEntry,
        ContextRecord,
        HandlerData,
        EstablisherFrame,
        ContextPointers,
        &handlerRoutine);

    _ASSERTE(SUCCEEDED(res));

    return handlerRoutine;
}
#endif // WIN64EXCEPTIONS
