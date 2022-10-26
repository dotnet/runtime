// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "utilcode.h"
#include "crosscomp.h"

#include "unwinder.h"

// #error "TODO-RISCV64: missing implementation"
//
#if 0
NTSTATUS
RtlpUnwindCustom(
    __inout PT_CONTEXT ContextRecord,
    _In_ BYTE Opcode,
    _In_ PLOONGARCH64_UNWIND_PARAMS UnwindParams
    )
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return STATUS_SUCCESS;
}

ULONG
RtlpComputeScopeSize(
    _In_ ULONG_PTR UnwindCodePtr,
    _In_ ULONG_PTR UnwindCodesEndPtr,
    _In_ BOOLEAN IsEpilog,
    _In_ PLOONGARCH64_UNWIND_PARAMS UnwindParams
    )

{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return 0;
}

NTSTATUS
RtlpUnwindRestoreRegisterRange(
    __inout PT_CONTEXT ContextRecord,
    _In_ LONG SpOffset,
    _In_ ULONG FirstRegister,
    _In_ ULONG RegisterCount,
    _In_ PLOONGARCH64_UNWIND_PARAMS UnwindParams
    )
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return STATUS_SUCCESS;
}

NTSTATUS
RtlpUnwindRestoreFpRegisterRange(
    __inout PT_CONTEXT ContextRecord,
    _In_ LONG SpOffset,
    _In_ ULONG FirstRegister,
    _In_ ULONG RegisterCount,
    _In_ PLOONGARCH64_UNWIND_PARAMS UnwindParams
    )
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return STATUS_SUCCESS;
}

NTSTATUS
RtlpUnwindFunctionFull(
    _In_ DWORD64 ControlPcRva,
    _In_ ULONG_PTR ImageBase,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    __inout T_CONTEXT *ContextRecord,
    _Out_ PDWORD64 EstablisherFrame,
    __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine,
    _Out_ PVOID *HandlerData,
    _In_ PLOONGARCH64_UNWIND_PARAMS UnwindParams
    )
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return STATUS_SUCCESS;
}

NTSTATUS
RtlpUnwindFunctionCompact(
    _In_ DWORD64 ControlPcRva,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    __inout T_CONTEXT *ContextRecord,
    _Out_ PDWORD64 EstablisherFrame,
    __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine,
    _Out_ PVOID *HandlerData,
    _In_ PLOONGARCH64_UNWIND_PARAMS UnwindParams
    )
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return STATUS_SUCCESS;
}

BOOL OOPStackUnwinderRiscv64::Unwind(T_CONTEXT * pContext)
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return false;
}
#endif

BOOL DacUnwindStackFrame(T_CONTEXT *pContext, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers)
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return false;
}

#if defined(HOST_UNIX)
PEXCEPTION_ROUTINE
RtlVirtualUnwind(
    IN ULONG HandlerType,
    IN ULONG64 ImageBase,
    IN ULONG64 ControlPc,
    IN PT_RUNTIME_FUNCTION FunctionEntry,
    IN OUT PCONTEXT ContextRecord,
    OUT PVOID *HandlerData,
    OUT PULONG64 EstablisherFrame,
    IN OUT PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
    )
{
    _ASSERTE(!"TODO RISCV64 NUYI");
    return NULL;

}
#endif
