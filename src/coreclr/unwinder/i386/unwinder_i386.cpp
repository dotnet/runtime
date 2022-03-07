// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "unwinder_i386.h"

#ifdef FEATURE_EH_FUNCLETS
BOOL OOPStackUnwinderX86::Unwind(T_CONTEXT* pContextRecord, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers)
{
    REGDISPLAY rd;

    FillRegDisplay(&rd, pContextRecord);

    rd.SP = pContextRecord->Esp;
    rd.PCTAddr = (UINT_PTR)&(pContextRecord->Eip);

    if (pContextPointers)
    {
        rd.pCurrentContextPointers = pContextPointers;
    }

    CodeManState codeManState;
    codeManState.dwIsSet = 0;

    DWORD ControlPc = pContextRecord->Eip;

    EECodeInfo codeInfo;
    codeInfo.Init((PCODE) ControlPc);

    if (!UnwindStackFrame(&rd, &codeInfo, UpdateAllRegs, &codeManState, NULL))
    {
        return FALSE;
    }

    pContextRecord->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;

#define ARGUMENT_AND_SCRATCH_REGISTER(reg) if (rd.pCurrentContextPointers->reg) pContextRecord->reg = *rd.pCurrentContextPointers->reg;
    ENUM_ARGUMENT_AND_SCRATCH_REGISTERS();
#undef ARGUMENT_AND_SCRATCH_REGISTER

#define CALLEE_SAVED_REGISTER(reg) if (rd.pCurrentContextPointers->reg) pContextRecord->reg = *rd.pCurrentContextPointers->reg;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    pContextRecord->Esp = rd.SP - codeInfo.GetCodeManager()->GetStackParameterSize(&codeInfo);
    pContextRecord->Eip = rd.ControlPC;

    return TRUE;
}

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
    _In_ DWORD HandlerType,
    _In_ DWORD ImageBase,
    _In_ DWORD ControlPc,
    _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
    __inout PCONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PDWORD EstablisherFrame,
    __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
    _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine
    )
{
    if (HandlerRoutine != NULL)
    {
        *HandlerRoutine = NULL;
    }

    _ASSERTE(ContextRecord->Eip == ControlPc);

    if (!OOPStackUnwinderX86::Unwind(ContextRecord, ContextPointers))
    {
        return HRESULT_FROM_WIN32(ERROR_READ_FAULT);
    }

    // For x86, the value of Establisher Frame Pointer is Caller SP
    //
    // (Please refers to CLR ABI for details)
    *EstablisherFrame = ContextRecord->Esp;
    return S_OK;
}

BOOL DacUnwindStackFrame(T_CONTEXT* pContextRecord, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers)
{
    BOOL res = OOPStackUnwinderX86::Unwind(pContextRecord, NULL);

    if (res && pContextPointers)
    {
        FillContextPointers(pContextPointers, pContextRecord);
    }

    return res;
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
    _In_ DWORD HandlerType,
    _In_ DWORD ImageBase,
    _In_ DWORD ControlPc,
    _In_ PRUNTIME_FUNCTION FunctionEntry,
    __inout PT_CONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PDWORD EstablisherFrame,
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
#endif // FEATURE_EH_FUNCLETS
