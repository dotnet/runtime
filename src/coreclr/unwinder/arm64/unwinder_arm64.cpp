// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "utilcode.h"
#include "crosscomp.h"

#include "unwinder_arm64.h"

typedef struct _ARM64_KTRAP_FRAME {

//
// Exception active indicator.
//
//    0 - interrupt frame.
//    1 - exception frame.
//    2 - service frame.
//

    /* +0x000 */ UCHAR ExceptionActive;              // always valid
    /* +0x001 */ UCHAR ContextFromKFramesUnwound;    // set if KeContextFromKFrames created this frame
    /* +0x002 */ UCHAR DebugRegistersValid;          // always valid
    /* +0x003 */ union {
                     UCHAR PreviousMode;   // system services only
                     UCHAR PreviousIrql;             // interrupts only
                 };

//
// Page fault information (page faults only)
// Previous trap frame address (system services only)
//
// Organized this way to allow first couple words to be used
// for scratch space in the general case
//

    /* +0x004 */ ULONG FaultStatus;                      // page faults only
    /* +0x008 */ union {
                     ULONG64 FaultAddress;             // page faults only
                     ULONG64 TrapFrame;                // system services only
                 };

//
// The ARM architecture does not have an architectural trap frame.  On
// an exception or interrupt, the processor switches to an
// exception-specific processor mode in which at least the LR and SP
// registers are banked.  Software is responsible for preserving
// registers which reflect the processor state in which the
// exception occurred rather than any intermediate processor modes.
//

//
// Volatile floating point state is dynamically allocated; this
// pointer may be NULL if the FPU was not enabled at the time the
// trap was taken.
//

    /* +0x010 */ PVOID VfpState;

//
// Debug registers
//

    /* +0x018 */ ULONG Bcr[ARM64_MAX_BREAKPOINTS];
    /* +0x038 */ ULONG64 Bvr[ARM64_MAX_BREAKPOINTS];
    /* +0x078 */ ULONG Wcr[ARM64_MAX_WATCHPOINTS];
    /* +0x080 */ ULONG64 Wvr[ARM64_MAX_WATCHPOINTS];

//
// Volatile registers X0-X17, and the FP, SP, LR
//

    /* +0x090 */ ULONG Spsr;
    /* +0x094 */ ULONG Esr;
    /* +0x098 */ ULONG64 Sp;
    /* +0x0A0 */ ULONG64 X[19];
    /* +0x138 */ ULONG64 Lr;
    /* +0x140 */ ULONG64 Fp;
    /* +0x148 */ ULONG64 Pc;
    /* +0x150 */

} ARM64_KTRAP_FRAME, *PARM64_KTRAP_FRAME;

typedef struct _ARM64_VFP_STATE
{
    struct _ARM64_VFP_STATE *Link;          // link to next state entry
    ULONG Fpcr;                             // FPCR register
    ULONG Fpsr;                             // FPSR register
    NEON128 V[32];                          // All V registers (0-31)
} ARM64_VFP_STATE, *PARM64_VFP_STATE, KARM64_VFP_STATE, *PKARM64_VFP_STATE;

//
// Parameters describing the unwind codes.
//

#define STATUS_UNWIND_UNSUPPORTED_VERSION   STATUS_UNSUCCESSFUL
#define STATUS_UNWIND_NOT_IN_FUNCTION       STATUS_UNSUCCESSFUL
#define STATUS_UNWIND_INVALID_SEQUENCE      STATUS_UNSUCCESSFUL

//
// Macros for accessing memory. These can be overridden if other code
// (in particular the debugger) needs to use them.

#define MEMORY_READ_BYTE(params, addr)       (*dac_cast<PTR_BYTE>(addr))
#define MEMORY_READ_DWORD(params, addr)      (*dac_cast<PTR_DWORD>(addr))
#define MEMORY_READ_QWORD(params, addr)      (*dac_cast<PTR_UINT64>(addr))

typedef struct _ARM64_UNWIND_PARAMS
{
    PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers;
} ARM64_UNWIND_PARAMS, *PARM64_UNWIND_PARAMS;

#define UNWIND_PARAMS_SET_TRAP_FRAME(Params, Address, Size)

#define UPDATE_CONTEXT_POINTERS(Params, RegisterNumber, Address)                      \
do {                                                                                  \
    if (ARGUMENT_PRESENT(Params)) {                                                   \
        PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
        if (ARGUMENT_PRESENT(ContextPointers)) {                                      \
            if (RegisterNumber >=  19 && RegisterNumber <= 30) {                      \
                (&ContextPointers->X19)[RegisterNumber - 19] = (PDWORD64)Address;     \
            }                                                                         \
        }                                                                             \
    }                                                                                 \
} while (0)


#define UPDATE_FP_CONTEXT_POINTERS(Params, RegisterNumber, Address)                   \
do {                                                                                  \
    if (ARGUMENT_PRESENT(Params)) {                                                   \
        PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
        if (ARGUMENT_PRESENT(ContextPointers) &&                                      \
            (RegisterNumber >=  8) &&                                                 \
            (RegisterNumber <= 15)) {                                                 \
                                                                                      \
            (&ContextPointers->D8)[RegisterNumber - 8] = (PDWORD64)Address;           \
        }                                                                             \
    }                                                                                 \
} while (0)

#define VALIDATE_STACK_ADDRESS_EX(Params, Context, Address, DataSize, Alignment, OutStatus)
#define VALIDATE_STACK_ADDRESS(Params, Context, DataSize, Alignment, OutStatus)

//
// Macros to clarify opcode parsing
//

#define OPCODE_IS_END(Op) (((Op) & 0xfe) == 0xe4)

//
// This table describes the size of each unwind code, in bytes
//

static const BYTE UnwindCodeSizeTable[256] =
{
    1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
    1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
    1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
    1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
    1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
    1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
    2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2,
    4,1,2,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1
};

NTSTATUS
RtlpUnwindCustom(
    __inout PT_CONTEXT ContextRecord,
    _In_ BYTE Opcode,
    _In_ PARM64_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    Handles custom unwinding operations involving machine-specific
    frames.

Arguments:

    ContextRecord - Supplies the address of a context record.

    Opcode - The opcode to decode.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    An NTSTATUS indicating either STATUS_SUCCESS if everything went ok, or
    another status code if there were problems.

--*/

{
    ULONG Fpcr;
    ULONG Fpsr;
    ULONG RegIndex;
    ULONG_PTR SourceAddress;
    ULONG_PTR StartingSp;
    NTSTATUS Status;
    ULONG_PTR VfpStateAddress;

    StartingSp = ContextRecord->Sp;
    Status = STATUS_SUCCESS;

    //
    // The opcode describes the special-case stack
    //

    switch (Opcode)
    {

    //
    // Trap frame case
    //

    case 0xe8:  // MSFT_OP_TRAP_FRAME:

        //
        // Ensure there is enough valid space for the trap frame
        //

        VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, sizeof(ARM64_KTRAP_FRAME), 16, &Status);
        if (!NT_SUCCESS(Status)) {
            return Status;
        }

        //
        // Restore X0-X17, and D0-D7
        //

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, X);
        for (RegIndex = 0; RegIndex < 18; RegIndex++) {
            UPDATE_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
#ifdef __GNUC__
            *(&ContextRecord->X0 + RegIndex) = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
#else
            ContextRecord->X[RegIndex] = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
#endif
            SourceAddress += sizeof(ULONG_PTR);
        }

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, VfpState);
        VfpStateAddress = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
        if (VfpStateAddress != 0) {

            SourceAddress = VfpStateAddress + FIELD_OFFSET(KARM64_VFP_STATE, Fpcr);
            Fpcr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);
            SourceAddress = VfpStateAddress + FIELD_OFFSET(KARM64_VFP_STATE, Fpsr);
            Fpsr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);
            if (Fpcr != (ULONG)-1 && Fpsr != (ULONG)-1) {

                ContextRecord->Fpcr = Fpcr;
                ContextRecord->Fpsr = Fpsr;

                SourceAddress = VfpStateAddress + FIELD_OFFSET(KARM64_VFP_STATE, V);
                for (RegIndex = 0; RegIndex < 32; RegIndex++) {
                    UPDATE_FP_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
                    ContextRecord->V[RegIndex].Low = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
                    ContextRecord->V[RegIndex].High = MEMORY_READ_QWORD(UnwindParams, SourceAddress + 8);
                    SourceAddress += 2 * sizeof(ULONGLONG);
                }
            }
        }

        //
        // Restore R11, R12, SP, LR, PC, and the status registers
        //

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, Spsr);
        ContextRecord->Cpsr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, Sp);
        ContextRecord->Sp = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, Lr);
        ContextRecord->Lr = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, Fp);
        ContextRecord->Fp = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, Pc);
        ContextRecord->Pc = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        //
        // Set the trap frame and clear the unwound-to-call flag
        //

        UNWIND_PARAMS_SET_TRAP_FRAME(UnwindParams, StartingSp, sizeof(ARM64_KTRAP_FRAME));
        ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
        break;

    //
    // Context case
    //

    case 0xea:  // MSFT_OP_CONTEXT:

        //
        // Ensure there is enough valid space for the full CONTEXT structure
        //

        VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, sizeof(CONTEXT), 16, &Status);
        if (!NT_SUCCESS(Status)) {
            return Status;
        }

        //
        // Restore X0-X28, and D0-D31
        //

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, X0);
        for (RegIndex = 0; RegIndex < 29; RegIndex++) {
            UPDATE_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
#ifdef __GNUC__
            *(&ContextRecord->X0 + RegIndex) = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
#else
            ContextRecord->X[RegIndex] = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
#endif
            SourceAddress += sizeof(ULONG_PTR);
        }

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, V);
        for (RegIndex = 0; RegIndex < 32; RegIndex++) {
            UPDATE_FP_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
            ContextRecord->V[RegIndex].Low = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
            ContextRecord->V[RegIndex].High = MEMORY_READ_QWORD(UnwindParams, SourceAddress + 8);
            SourceAddress += 2 * sizeof(ULONGLONG);
        }

        //
        // Restore SP, LR, PC, and the status registers
        //

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, Cpsr);
        ContextRecord->Cpsr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, Fp);
        ContextRecord->Fp = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, Lr);
        ContextRecord->Lr = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, Sp);
        ContextRecord->Sp = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, Pc);
        ContextRecord->Pc = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, Fpcr);
        ContextRecord->Fpcr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, Fpsr);
        ContextRecord->Fpsr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        //
        // Inherit the unwound-to-call flag from this context
        //

        SourceAddress = StartingSp + FIELD_OFFSET(T_CONTEXT, ContextFlags);
        ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
        ContextRecord->ContextFlags |=
                        MEMORY_READ_DWORD(UnwindParams, SourceAddress) & CONTEXT_UNWOUND_TO_CALL;
        break;

    default:
        return STATUS_UNSUCCESSFUL;
    }

    return STATUS_SUCCESS;
}

ULONG
RtlpComputeScopeSize(
    _In_ ULONG_PTR UnwindCodePtr,
    _In_ ULONG_PTR UnwindCodesEndPtr,
    _In_ BOOLEAN IsEpilog,
    _In_ PARM64_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    Computes the size of an prolog or epilog, in words.

Arguments:

    UnwindCodePtr - Supplies a pointer to the start of the unwind
        code sequence.

    UnwindCodesEndPtr - Supplies a pointer to the byte immediately
        following the unwind code table, as described by the header.

    IsEpilog - Specifies TRUE if the scope describes an epilog,
        or FALSE if it describes a prolog.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    The size of the scope described by the unwind codes, in halfword units.

--*/

{
    ULONG ScopeSize;
    BYTE Opcode;

    //
    // Iterate through the unwind codes until we hit an end marker.
    // While iterating, accumulate the total scope size.
    //

    ScopeSize = 0;
    Opcode = 0;
    while (UnwindCodePtr < UnwindCodesEndPtr) {
        Opcode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
        if (OPCODE_IS_END(Opcode)) {
            break;
        }

        UnwindCodePtr += UnwindCodeSizeTable[Opcode];
        ScopeSize++;
    }

    //
    // Epilogs have one extra instruction at the end that needs to be
    // accounted for.
    //

    if (IsEpilog) {
        ScopeSize++;
    }

    return ScopeSize;
}

NTSTATUS
RtlpUnwindRestoreRegisterRange(
    __inout PT_CONTEXT ContextRecord,
    _In_ LONG SpOffset,
    _In_ ULONG FirstRegister,
    _In_ ULONG RegisterCount,
    _In_ PARM64_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    Restores a series of integer registers from the stack.

Arguments:

    ContextRecord - Supplies the address of a context record.

    SpOffset - Specifies a stack offset. Positive values are simply used
        as a base offset. Negative values assume a predecrement behavior:
        a 0 offset is used for restoration, but the absolute value of the
        offset is added to the final Sp.

    FirstRegister - Specifies the index of the first register to restore.

    RegisterCount - Specifies the number of registers to restore.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    None.

--*/

{
    ULONG_PTR CurAddress;
    ULONG RegIndex;
    NTSTATUS Status;

    //
    // Compute the source address and validate it.
    //

    CurAddress = ContextRecord->Sp;
    if (SpOffset >= 0) {
        CurAddress += SpOffset;
    }

    Status = STATUS_SUCCESS;
    VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, 8 * RegisterCount, 8, &Status);
    if (Status != STATUS_SUCCESS) {
        return Status;
    }

    //
    // Restore the registers
    //

    for (RegIndex = 0; RegIndex < RegisterCount; RegIndex++) {
        UPDATE_CONTEXT_POINTERS(UnwindParams, FirstRegister + RegIndex, CurAddress);
#ifdef __GNUC__
        *(&ContextRecord->X0 + FirstRegister + RegIndex) = MEMORY_READ_QWORD(UnwindParams, CurAddress);
#else
        ContextRecord->X[FirstRegister + RegIndex] = MEMORY_READ_QWORD(UnwindParams, CurAddress);
#endif
        CurAddress += 8;
    }
    if (SpOffset < 0) {
        ContextRecord->Sp -= SpOffset;
    }

    return STATUS_SUCCESS;
}

NTSTATUS
RtlpUnwindRestoreFpRegisterRange(
    __inout PT_CONTEXT ContextRecord,
    _In_ LONG SpOffset,
    _In_ ULONG FirstRegister,
    _In_ ULONG RegisterCount,
    _In_ PARM64_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    Restores a series of floating-point registers from the stack.

Arguments:

    ContextRecord - Supplies the address of a context record.

    SpOffset - Specifies a stack offset. Positive values are simply used
        as a base offset. Negative values assume a predecrement behavior:
        a 0 offset is used for restoration, but the absolute value of the
        offset is added to the final Sp.

    FirstRegister - Specifies the index of the first register to restore.

    RegisterCount - Specifies the number of registers to restore.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    None.

--*/

{
    ULONG_PTR CurAddress;
    ULONG RegIndex;
    NTSTATUS Status;

    //
    // Compute the source address and validate it.
    //

    CurAddress = ContextRecord->Sp;
    if (SpOffset >= 0) {
        CurAddress += SpOffset;
    }

    Status = STATUS_SUCCESS;
    VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, 8 * RegisterCount, 8, &Status);
    if (Status != STATUS_SUCCESS) {
        return Status;
    }

    //
    // Restore the registers
    //

    for (RegIndex = 0; RegIndex < RegisterCount; RegIndex++) {
        UPDATE_FP_CONTEXT_POINTERS(UnwindParams, FirstRegister + RegIndex, CurAddress);
        ContextRecord->V[FirstRegister + RegIndex].Low = MEMORY_READ_QWORD(UnwindParams, CurAddress);
        CurAddress += 8;
    }
    if (SpOffset < 0) {
        ContextRecord->Sp -= SpOffset;
    }

    return STATUS_SUCCESS;
}

NTSTATUS
RtlpUnwindFunctionFull(
    _In_ DWORD64 ControlPcRva,
    _In_ ULONG_PTR ImageBase,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    __inout T_CONTEXT *ContextRecord,
    _Out_ PDWORD64 EstablisherFrame,
    _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine,
    _Out_ PVOID *HandlerData,
    _In_ PARM64_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    This function virtually unwinds the specified function by parsing the
    .xdata record to determine where in the function the provided ControlPc
    is, and then executing unwind codes that map to the function's prolog
    or epilog behavior.

    If a context pointers record is specified (in the UnwindParams), then
    the address where each nonvolatile register is restored from is recorded
    in the appropriate element of the context pointers record.

Arguments:

    ControlPcRva - Supplies the address where control left the specified
        function, as an offset relative to the IamgeBase.

    ImageBase - Supplies the base address of the image that contains the
        function being unwound.

    FunctionEntry - Supplies the address of the function table entry for the
        specified function. If appropriate, this should have already been
        probed.

    ContextRecord - Supplies the address of a context record.

    EstablisherFrame - Supplies a pointer to a variable that receives the
        the establisher frame pointer value.

    HandlerRoutine - Supplies an optional pointer to a variable that receives
        the handler routine address.  If control did not leave the specified
        function in either the prolog or an epilog and a handler of the
        proper type is associated with the function, then the address of the
        language specific exception handler is returned. Otherwise, NULL is
        returned.

    HandlerData - Supplies a pointer to a variable that receives a pointer
        the the language handler data.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    STATUS_SUCCESS if the unwind could be completed, a failure status otherwise.
    Unwind can only fail when validation bounds are specified.

--*/

{
    ULONG AccumulatedSaveNexts;
    ULONG CurCode;
    ULONG EpilogScopeCount;
    PEXCEPTION_ROUTINE ExceptionHandler;
    PVOID ExceptionHandlerData;
    BOOLEAN FinalPcFromLr;
    ULONG FunctionLength;
    ULONG HeaderWord;
    ULONG NextCode;
    DWORD64 OffsetInFunction;
    ULONG ScopeNum;
    ULONG ScopeSize;
    ULONG ScopeStart;
    DWORD64 SkipWords;
    NTSTATUS Status;
    ULONG_PTR UnwindCodePtr;
    ULONG_PTR UnwindCodesEndPtr;
    ULONG_PTR UnwindDataPtr;
    ULONG UnwindIndex;
    ULONG UnwindWords;

    //
    // Unless a special frame is enountered, assume that any unwinding
    // will return us to the return address of a call and set the flag
    // appropriately (it will be cleared again if the special cases apply).
    //

    ContextRecord->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;

    //
    // By default, unwinding is done by popping to the LR, then copying
    // that LR to the PC. However, some special opcodes require different
    // behavior.
    //

    FinalPcFromLr = TRUE;

    //
    // Fetch the header word from the .xdata blob
    //

    UnwindDataPtr = ImageBase + FunctionEntry->UnwindData;
    HeaderWord = MEMORY_READ_DWORD(UnwindParams, UnwindDataPtr);
    UnwindDataPtr += 4;

    //
    // Verify the version before we do anything else
    //

    if (((HeaderWord >> 18) & 3) != 0) {
        return STATUS_UNWIND_UNSUPPORTED_VERSION;
    }

    FunctionLength = HeaderWord & 0x3ffff;
    OffsetInFunction = (ControlPcRva - FunctionEntry->BeginAddress) / 4;

    //
    // Determine the number of epilog scope records and the maximum number
    // of unwind codes.
    //

    UnwindWords = (HeaderWord >> 27) & 31;
    EpilogScopeCount = (HeaderWord >> 22) & 31;
    if (EpilogScopeCount == 0 && UnwindWords == 0) {
        EpilogScopeCount = MEMORY_READ_DWORD(UnwindParams, UnwindDataPtr);
        UnwindDataPtr += 4;
        UnwindWords = (EpilogScopeCount >> 16) & 0xff;
        EpilogScopeCount &= 0xffff;
    }
    if ((HeaderWord & (1 << 21)) != 0) {
        UnwindIndex = EpilogScopeCount;
        EpilogScopeCount = 0;
    }

    //
    // If exception data is present, extract it now.
    //

    ExceptionHandler = NULL;
    ExceptionHandlerData = NULL;
    if ((HeaderWord & (1 << 20)) != 0) {
        ExceptionHandler = (PEXCEPTION_ROUTINE)(ImageBase +
                        MEMORY_READ_DWORD(UnwindParams, UnwindDataPtr + 4 * (EpilogScopeCount + UnwindWords)));
        ExceptionHandlerData = (PVOID)(UnwindDataPtr + 4 * (EpilogScopeCount + UnwindWords + 1));
    }

    //
    // Unless we are in a prolog/epilog, we execute the unwind codes
    // that immediately follow the epilog scope list.
    //

    UnwindCodePtr = UnwindDataPtr + 4 * EpilogScopeCount;
    UnwindCodesEndPtr = UnwindCodePtr + 4 * UnwindWords;
    SkipWords = 0;

    //
    // If we're near the start of the function, and this function has a prolog,
    // compute the size of the prolog from the unwind codes. If we're in the
    // midst of it, we still execute starting at unwind code index 0, but we may
    // need to skip some to account for partial execution of the prolog.
    //
    // N.B. As an optimization here, note that each byte of unwind codes can
    //      describe at most one 32-bit instruction. Thus, the largest prologue
    //      that could possibly be described by UnwindWords (which is 4 * the
    //      number of unwind code bytes) is 4 * UnwindWords words. If
    //      OffsetInFunction is larger than this value, it is guaranteed to be
    //      in the body of the function.
    //

    if (OffsetInFunction < 4 * UnwindWords) {
        ScopeSize = RtlpComputeScopeSize(UnwindCodePtr, UnwindCodesEndPtr, FALSE, UnwindParams);

        if (OffsetInFunction < ScopeSize) {
            SkipWords = ScopeSize - OffsetInFunction;
            ExceptionHandler = NULL;
            ExceptionHandlerData = NULL;
            goto ExecuteCodes;
        }
    }

    //
    // We're not in the prolog, now check to see if we are in the epilog.
    // In the simple case, the 'E' bit is set indicating there is a single
    // epilog that lives at the end of the function. If we're near the end
    // of the function, compute the actual size of the epilog from the
    // unwind codes. If we're in the midst of it, adjust the unwind code
    // pointer to the start of the codes and determine how many we need to skip.
    //
    // N.B. Similar to the prolog case above, the maximum number of halfwords
    //      that an epilog can cover is limited by UnwindWords. In the epilog
    //      case, however, the starting index within the unwind code table is
    //      non-zero, and so the maximum number of unwind codes that can pertain
    //      to an epilog is (UnwindWords * 4 - UnwindIndex), thus further
    //      constraining the bounds of the epilog.
    //

    if ((HeaderWord & (1 << 21)) != 0) {
        if (OffsetInFunction + (4 * UnwindWords - UnwindIndex) >= FunctionLength) {
            ScopeSize = RtlpComputeScopeSize(UnwindCodePtr + UnwindIndex, UnwindCodesEndPtr, TRUE, UnwindParams);
            ScopeStart = FunctionLength - ScopeSize;

            if (OffsetInFunction >= ScopeStart) {
                UnwindCodePtr += UnwindIndex;
                SkipWords = OffsetInFunction - ScopeStart;
                ExceptionHandler = NULL;
                ExceptionHandlerData = NULL;
            }
        }
    }

    //
    // In the multiple-epilog case, we scan forward to see if we are within
    // shooting distance of any of the epilogs. If we are, we compute the
    // actual size of the epilog from the unwind codes and proceed like the
    // simple case above.
    //

    else {
        for (ScopeNum = 0; ScopeNum < EpilogScopeCount; ScopeNum++) {
            HeaderWord = MEMORY_READ_DWORD(UnwindParams, UnwindDataPtr);
            UnwindDataPtr += 4;

            //
            // The scope records are stored in order. If we hit a record that
            // starts after our current position, we must not be in an epilog.
            //

            ScopeStart = HeaderWord & 0x3ffff;
            if (OffsetInFunction < ScopeStart) {
                break;
            }

            UnwindIndex = HeaderWord >> 22;
            if (OffsetInFunction < ScopeStart + (4 * UnwindWords - UnwindIndex)) {
                ScopeSize = RtlpComputeScopeSize(UnwindCodePtr + UnwindIndex, UnwindCodesEndPtr, TRUE, UnwindParams);

                if (OffsetInFunction < ScopeStart + ScopeSize) {

                    UnwindCodePtr += UnwindIndex;
                    SkipWords = OffsetInFunction - ScopeStart;
                    ExceptionHandler = NULL;
                    ExceptionHandlerData = NULL;
                    break;
                }
            }
        }
    }

ExecuteCodes:

    //
    // Skip over unwind codes until we account for the number of halfwords
    // to skip.
    //

    while (UnwindCodePtr < UnwindCodesEndPtr && SkipWords > 0) {
        CurCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
        if (OPCODE_IS_END(CurCode)) {
            break;
        }
        UnwindCodePtr += UnwindCodeSizeTable[CurCode];
        SkipWords--;
    }

    //
    // Now execute codes until we hit the end.
    //

    Status = STATUS_SUCCESS;
    AccumulatedSaveNexts = 0;
    while (UnwindCodePtr < UnwindCodesEndPtr && Status == STATUS_SUCCESS) {

        CurCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
        UnwindCodePtr += 1;

        //
        // alloc_s (000xxxxx): allocate small stack with size < 1024 (2^5 * 16)
        //

        if (CurCode <= 0x1f) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            ContextRecord->Sp += 16 * (CurCode & 0x1f);
        }

        //
        // save_r19r20_x (001zzzzz): save <r19,r20> pair at [sp-#Z*8]!, pre-indexed offset >= -248
        //

        else if (CurCode <= 0x3f) {
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        -8 * (CurCode & 0x1f),
                        19,
                        2 + 2 * AccumulatedSaveNexts,
                        UnwindParams);
            AccumulatedSaveNexts = 0;
        }

        //
        // save_fplr (01zzzzzz): save <r29,lr> pair at [sp+#Z*8], offset <= 504
        //

        else if (CurCode <= 0x7f) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        8 * (CurCode & 0x3f),
                        29,
                        2,
                        UnwindParams);
        }

        //
        // save_fplr_x (10zzzzzz): save <r29,lr> pair at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
        //

        else if (CurCode <= 0xbf) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        -8 * ((CurCode & 0x3f) + 1),
                        29,
                        2,
                        UnwindParams);
        }

        //
        // alloc_m (11000xxx|xxxxxxxx): allocate large stack with size < 32k (2^11 * 16).
        //

        else if (CurCode <= 0xc7) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            ContextRecord->Sp += 16 * ((CurCode & 7) << 8);
            ContextRecord->Sp += 16 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
        }

        //
        // save_regp (110010xx|xxzzzzzz): save r(19+#X) pair at [sp+#Z*8], offset <= 504
        //

        else if (CurCode <= 0xcb) {
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        8 * (NextCode & 0x3f),
                        19 + ((CurCode & 3) << 2) + (NextCode >> 6),
                        2 + 2 * AccumulatedSaveNexts,
                        UnwindParams);
            AccumulatedSaveNexts = 0;
        }

        //
        // save_regp_x (110011xx|xxzzzzzz): save pair r(19+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
        //

        else if (CurCode <= 0xcf) {
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        -8 * ((NextCode & 0x3f) + 1),
                        19 + ((CurCode & 3) << 2) + (NextCode >> 6),
                        2 + 2 * AccumulatedSaveNexts,
                        UnwindParams);
            AccumulatedSaveNexts = 0;
        }

        //
        // save_reg (110100xx|xxzzzzzz): save reg r(19+#X) at [sp+#Z*8], offset <= 504
        //

        else if (CurCode <= 0xd3) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        8 * (NextCode & 0x3f),
                        19 + ((CurCode & 3) << 2) + (NextCode >> 6),
                        1,
                        UnwindParams);
        }

        //
        // save_reg_x (1101010x|xxxzzzzz): save reg r(19+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -256
        //

        else if (CurCode <= 0xd5) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        -8 * ((NextCode & 0x1f) + 1),
                        19 + ((CurCode & 1) << 3) + (NextCode >> 5),
                        1,
                        UnwindParams);
        }

        //
        // save_lrpair (1101011x|xxzzzzzz): save pair <r19+2*#X,lr> at [sp+#Z*8], offset <= 504
        //

        else if (CurCode <= 0xd7) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        8 * (NextCode & 0x3f),
                        19 + 2 * (((CurCode & 1) << 2) + (NextCode >> 6)),
                        1,
                        UnwindParams);
            if (Status == STATUS_SUCCESS) {
                RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        8 * (NextCode & 0x3f) + 8,
                        30,
                        1,
                        UnwindParams);
            }
        }

        //
        // save_fregp (1101100x|xxzzzzzz): save pair d(8+#X) at [sp+#Z*8], offset <= 504
        //

        else if (CurCode <= 0xd9) {
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreFpRegisterRange(
                        ContextRecord,
                        8 * (NextCode & 0x3f),
                        8 + ((CurCode & 1) << 2) + (NextCode >> 6),
                        2 + AccumulatedSaveNexts,
                        UnwindParams);
            AccumulatedSaveNexts = 0;
        }

        //
        // save_fregp_x (1101101x|xxzzzzzz): save pair d(8+#X), at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
        //

        else if (CurCode <= 0xdb) {
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreFpRegisterRange(
                        ContextRecord,
                        -8 * ((NextCode & 0x3f) + 1),
                        8 + ((CurCode & 1) << 2) + (NextCode >> 6),
                        2 + AccumulatedSaveNexts,
                        UnwindParams);
            AccumulatedSaveNexts = 0;
        }

        //
        // save_freg (1101110x|xxzzzzzz): save reg d(9+#X) at [sp+#Z*8], offset <= 504
        //

        else if (CurCode <= 0xdd) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreFpRegisterRange(
                        ContextRecord,
                        8 * (NextCode & 0x3f),
                        8 + ((CurCode & 1) << 2) + (NextCode >> 6),
                        1,
                        UnwindParams);
        }

        //
        // save_freg_x (11011110|xxxzzzzz): save reg d(8+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -256
        //

        else if (CurCode == 0xde) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreFpRegisterRange(
                        ContextRecord,
                        -8 * ((NextCode & 0x1f) + 1),
                        8 + (NextCode >> 5),
                        1,
                        UnwindParams);
        }

        //
        // alloc_l (11100000|xxxxxxxx|xxxxxxxx|xxxxxxxx): allocate large stack with size < 256M
        //

        else if (CurCode == 0xe0) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            ContextRecord->Sp += 16 * (MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr) << 16);
            UnwindCodePtr++;
            ContextRecord->Sp += 16 * (MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr) << 8);
            UnwindCodePtr++;
            ContextRecord->Sp += 16 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
        }

        //
        // set_fp (11100001): set up r29: with: mov r29,sp
        //

        else if (CurCode == 0xe1) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            ContextRecord->Sp = ContextRecord->Fp;
        }

        //
        // add_fp (11100010|xxxxxxxx): set up r29 with: add r29,sp,#x*8
        //

        else if (CurCode == 0xe2) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            ContextRecord->Sp = ContextRecord->Fp - 8 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
        }

        //
        // nop (11100011): no unwind operation is required
        //

        else if (CurCode == 0xe3) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
        }

        //
        // end (11100100): end of unwind code
        //

        else if (CurCode == 0xe4) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            goto finished;
        }

        //
        // end_c (11100101): end of unwind code in current chained scope
        //

        else if (CurCode == 0xe5) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            goto finished;
        }

        //
        // save_next (11100110): save next non-volatile Int or FP register pair.
        //

        else if (CurCode == 0xe6) {
            AccumulatedSaveNexts++;
        }

        //
        // custom_0 (111010xx): restore custom structure
        //

        else if (CurCode >= 0xe8 && CurCode <= 0xeb) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            Status = RtlpUnwindCustom(ContextRecord, (BYTE) CurCode, UnwindParams);
            FinalPcFromLr = FALSE;
        }

        //
        // Anything else is invalid
        //

        else {
            return STATUS_UNWIND_INVALID_SEQUENCE;
        }
    }

    //
    // If we succeeded, post-process the results a bit
    //
finished:
    if (Status == STATUS_SUCCESS) {

        //
        // Since we always POP to the LR, recover the final PC from there, unless
        // it was overwritten due to a special case custom unwinding operation.
        // Also set the establisher frame equal to the final stack pointer.
        //

        if (FinalPcFromLr) {
            ContextRecord->Pc = ContextRecord->Lr;
        }
        *EstablisherFrame = ContextRecord->Sp;

        if (ARGUMENT_PRESENT(HandlerRoutine)) {
            *HandlerRoutine = ExceptionHandler;
        }
        *HandlerData = ExceptionHandlerData;
    }

    return Status;
}

NTSTATUS
RtlpUnwindFunctionCompact(
    _In_ DWORD64 ControlPcRva,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    __inout T_CONTEXT *ContextRecord,
    _Out_ PDWORD64 EstablisherFrame,
    _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine,
    _Out_ PVOID *HandlerData,
    _In_ PARM64_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    This function virtually unwinds the specified function by parsing the
    compact .pdata record to determine where in the function the provided
    ControlPc is, and then executing a standard, well-defined set of
    operations.

    If a context pointers record is specified (in the UnwindParams), then
    the address where each nonvolatile register is restored from is recorded
    in the appropriate element of the context pointers record.

Arguments:

    ControlPcRva - Supplies the address where control left the specified
        function, as an offset relative to the IamgeBase.

    FunctionEntry - Supplies the address of the function table entry for the
        specified function. If appropriate, this should have already been
        probed.

    ContextRecord - Supplies the address of a context record.

    EstablisherFrame - Supplies a pointer to a variable that receives the
        the establisher frame pointer value.

    HandlerRoutine - Supplies an optional pointer to a variable that receives
        the handler routine address.  If control did not leave the specified
        function in either the prolog or an epilog and a handler of the
        proper type is associated with the function, then the address of the
        language specific exception handler is returned. Otherwise, NULL is
        returned.

    HandlerData - Supplies a pointer to a variable that receives a pointer
        the the language handler data.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    STATUS_SUCCESS if the unwind could be completed, a failure status otherwise.
    Unwind can only fail when validation bounds are specified.

--*/

{
    ULONG Count;
    ULONG Cr;
    ULONG CurrentOffset;
    ULONG EpilogLength;
    ULONG Flag;
    ULONG FloatSize;
    ULONG FrameSize;
    ULONG FRegOpcodes;
    ULONG FunctionLength;
    ULONG HBit;
    ULONG HOpcodes;
    ULONG IRegOpcodes;
    ULONG IntSize;
    ULONG LocalSize;
    DWORD64 OffsetInFunction;
    DWORD64 OffsetInScope;
    ULONG PrologLength;
    ULONG RegF;
    ULONG RegI;
    ULONG RegSize;
    ULONG ScopeStart;
    ULONG StackAdjustOpcodes;
    NTSTATUS Status;
    ULONG UnwindData;

    UnwindData = FunctionEntry->UnwindData;
    Status = STATUS_SUCCESS;

    //
    // Compact records always describe an unwind to a call.
    //

    ContextRecord->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;

    //
    // Extract the basic information about how to do a full unwind.
    //

    Flag = UnwindData & 3;
    FunctionLength = (UnwindData >> 2) & 0x7ff;
    RegF = (UnwindData >> 13) & 7;
    RegI = (UnwindData >> 16) & 0xf;
    HBit = (UnwindData >> 20) & 1;
    Cr = (UnwindData >> 21) & 3;
    FrameSize = (UnwindData >> 23) & 0x1ff;

    if (Flag == 3) {
        return STATUS_UNWIND_INVALID_SEQUENCE;
    }
    if (Cr == 2) {
        return STATUS_UNWIND_INVALID_SEQUENCE;
    }

    //
    // Determine the size of the locals
    //

    IntSize = RegI * 8;
    if (Cr == 1) {
        IntSize += 8;
    }
    FloatSize = (RegF == 0) ? 0 : (RegF + 1) * 8;
    RegSize = (IntSize + FloatSize + 8*8 * HBit + 0xf) & ~0xf;
    if (RegSize > 16 * FrameSize) {
        return STATUS_UNWIND_INVALID_SEQUENCE;
    }
    LocalSize = 16 * FrameSize - RegSize;

    //
    // If we're near the start of the function (within 17 words),
    // see if we are within the prolog.
    //
    // N.B. If the low 2 bits of the UnwindData are 2, then we have
    // no prolog.
    //

    OffsetInFunction = (ControlPcRva - FunctionEntry->BeginAddress) / 4;
    OffsetInScope = 0;
    if (OffsetInFunction < 17 && Flag != 2) {

        //
        // Compute sizes for each opcode in the prolog.
        //

        IRegOpcodes = (IntSize + 8) / 16;
        FRegOpcodes = (FloatSize + 8) / 16;
        HOpcodes = 4 * HBit;
        StackAdjustOpcodes = (Cr == 3) ? 1 : 0;
        if (Cr != 3 || LocalSize > 512) {
            StackAdjustOpcodes += (LocalSize > 4088) ? 2 : (LocalSize > 0) ? 1 : 0;
        }

        //
        // Compute the total prolog length and determine if we are within
        // its scope.
        //
        // N.B. We must execute prolog operations backwards to unwind, so
        // our final scope offset in this case is the distance from the end.
        //

        PrologLength = IRegOpcodes + FRegOpcodes + HOpcodes + StackAdjustOpcodes;

        if (OffsetInFunction < PrologLength) {
            OffsetInScope = PrologLength - OffsetInFunction;
        }
    }

    //
    // If we're near the end of the function (within 15 words), see if
    // we are within the epilog.
    //
    // N.B. If the low 2 bits of the UnwindData are 2, then we have
    // no epilog.
    //

    if (OffsetInScope == 0 && OffsetInFunction + 15 >= FunctionLength && Flag != 2) {

        //
        // Compute sizes for each opcode in the epilog.
        //

        IRegOpcodes = (IntSize + 8) / 16;
        FRegOpcodes = (FloatSize + 8) / 16;
        HOpcodes = HBit;
        StackAdjustOpcodes = (Cr == 3) ? 1 : 0;
        if (Cr != 3 || LocalSize > 512) {
            StackAdjustOpcodes += (LocalSize > 4088) ? 2 : (LocalSize > 0) ? 1 : 0;
        }

        //
        // Compute the total epilog length and determine if we are within
        // its scope.
        //

        EpilogLength = IRegOpcodes + FRegOpcodes + HOpcodes + StackAdjustOpcodes + 1;

        ScopeStart = FunctionLength - EpilogLength;
        if (OffsetInFunction > ScopeStart) {
            OffsetInScope = OffsetInFunction - ScopeStart;
        }
    }

    //
    // Process operations backwards, in the order: stack/frame deallocation,
    // VFP register popping, integer register popping, parameter home
    // area recovery.
    //
    // First case is simple: we process everything with no regard for
    // the current offset within the scope.
    //

    Status = STATUS_SUCCESS;
    if (OffsetInScope == 0) {

        if (Cr == 3) {
            Status = RtlpUnwindRestoreRegisterRange(ContextRecord, 0, 29, 2, UnwindParams);
        }
        ContextRecord->Sp += LocalSize;

        if (RegF != 0 && Status == STATUS_SUCCESS) {
            Status = RtlpUnwindRestoreFpRegisterRange(ContextRecord, IntSize, 8, RegF + 1, UnwindParams);
        }

        if (Cr == 1 && Status == STATUS_SUCCESS) {
            Status = RtlpUnwindRestoreRegisterRange(ContextRecord, IntSize - 8, 30, 1, UnwindParams);
        }
        if (RegI > 0 && Status == STATUS_SUCCESS) {
            Status = RtlpUnwindRestoreRegisterRange(ContextRecord, 0, 19, RegI, UnwindParams);
        }
        ContextRecord->Sp += RegSize;
    }

    //
    // Second case is more complex: we must step along each operation
    // to ensure it should be executed.
    //

    else {

        CurrentOffset = 0;
        if (Cr == 3) {
            if (LocalSize <= 512) {
                if (CurrentOffset++ >= OffsetInScope) {
                    Status = RtlpUnwindRestoreRegisterRange(ContextRecord, -(LONG)LocalSize, 29, 2, UnwindParams);
                }
                LocalSize = 0;
            }
        }
        while (LocalSize != 0) {
            Count = (LocalSize + 4087) % 4088 + 1;
            if (CurrentOffset++ >= OffsetInScope) {
                ContextRecord->Sp += Count;
            }
            LocalSize -= Count;
        }

        if (HBit != 0) {
            CurrentOffset += 4;
        }

        if (RegF != 0 && Status == STATUS_SUCCESS) {
            RegF++;
            while (RegF != 0) {
                Count = 2 - (RegF & 1);
                RegF -= Count;
                if (CurrentOffset++ >= OffsetInScope) {
                    Status = RtlpUnwindRestoreFpRegisterRange(
                               ContextRecord,
                               (RegF == 0 && RegI == 0) ? (-(LONG)RegSize) : (IntSize + 8 * RegF),
                               8 + RegF,
                               Count,
                               UnwindParams);
                }
            }
        }

        if (Cr == 1 && Status == STATUS_SUCCESS) {
            if (RegI % 2 == 0) {
                if (CurrentOffset++ >= OffsetInScope) {
                    Status = RtlpUnwindRestoreRegisterRange(ContextRecord, IntSize - 8, 30, 1, UnwindParams);
                }
            } else {
                if (CurrentOffset++ >= OffsetInScope) {
                    RegI--;
                    Status = RtlpUnwindRestoreRegisterRange(ContextRecord, IntSize - 8, 30, 1, UnwindParams);
                    if (Status == STATUS_SUCCESS) {
                        Status = RtlpUnwindRestoreRegisterRange(ContextRecord, IntSize - 16, 19 + RegI, 1, UnwindParams);
                    }
                }
            }
        }

        while (RegI != 0 && Status == STATUS_SUCCESS) {
            Count = 2 - (RegI & 1);
            RegI -= Count;
            if (CurrentOffset++ >= OffsetInScope) {
                Status = RtlpUnwindRestoreRegisterRange(
                            ContextRecord,
                            (RegI == 0) ? (-(LONG)RegSize) : (8 * RegI),
                            19 + RegI,
                            Count,
                            UnwindParams);
            }
        }
    }

    //
    // If we succeeded, post-process the results a bit
    //

    if (Status == STATUS_SUCCESS) {

        ContextRecord->Pc = ContextRecord->Lr;
        *EstablisherFrame = ContextRecord->Sp;

        if (ARGUMENT_PRESENT(HandlerRoutine)) {
            *HandlerRoutine = NULL;
        }
        *HandlerData = NULL;
    }

    return Status;
}

BOOL OOPStackUnwinderArm64::Unwind(T_CONTEXT * pContext)
{
    DWORD64 ImageBase = 0;
    HRESULT hr = GetModuleBase(pContext->Pc, &ImageBase);
    if (hr != S_OK)
        return FALSE;

    PEXCEPTION_ROUTINE DummyHandlerRoutine;
    PVOID DummyHandlerData;
    DWORD64 DummyEstablisherFrame;

    DWORD64 startingPc = pContext->Pc;
    DWORD64 startingSp = pContext->Sp;

    T_RUNTIME_FUNCTION Rfe;
    if (FAILED(GetFunctionEntry(pContext->Pc, &Rfe, sizeof(Rfe))))
        return FALSE;

    if ((Rfe.UnwindData & 3) != 0)
    {
        hr = RtlpUnwindFunctionCompact(pContext->Pc - ImageBase,
                                        &Rfe,
                                        pContext,
                                        &DummyEstablisherFrame,
                                        &DummyHandlerRoutine,
                                        &DummyHandlerData,
                                        NULL);

    }
    else
    {
        hr = RtlpUnwindFunctionFull(pContext->Pc - ImageBase,
                                    ImageBase,
                                    &Rfe,
                                    pContext,
                                    &DummyEstablisherFrame,
                                    &DummyHandlerRoutine,
                                    &DummyHandlerData,
                                    NULL);
    }

    // PC == 0 means unwinding is finished.
    // Same if no forward progress is made
    if (pContext->Pc == 0 || (startingPc == pContext->Pc && startingSp == pContext->Sp))
        return FALSE;

    return TRUE;
}

BOOL DacUnwindStackFrame(T_CONTEXT *pContext, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers)
{
    OOPStackUnwinderArm64 unwinder;
    BOOL res = unwinder.Unwind(pContext);

    if (res && pContextPointers)
    {
        for (int i = 0; i < 12; i++)
        {
            *(&pContextPointers->X19 + i) = &pContext->X19 + i;
        }
    }

    return res;
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
    PEXCEPTION_ROUTINE handlerRoutine;
    HRESULT hr;

    DWORD64 startingPc = ControlPc;
    DWORD64 startingSp = ContextRecord->Sp;

    T_RUNTIME_FUNCTION rfe;

    rfe.BeginAddress = FunctionEntry->BeginAddress;
    rfe.UnwindData = FunctionEntry->UnwindData;

    ARM64_UNWIND_PARAMS unwindParams;
    unwindParams.ContextPointers = ContextPointers;

    if ((rfe.UnwindData & 3) != 0)
    {
        hr = RtlpUnwindFunctionCompact(ControlPc - ImageBase,
                                        &rfe,
                                        ContextRecord,
                                        EstablisherFrame,
                                        &handlerRoutine,
                                        HandlerData,
                                        &unwindParams);

    }
    else
    {
        hr = RtlpUnwindFunctionFull(ControlPc - ImageBase,
                                    ImageBase,
                                    &rfe,
                                    ContextRecord,
                                    EstablisherFrame,
                                    &handlerRoutine,
                                    HandlerData,
                                    &unwindParams);
    }

    _ASSERTE(SUCCEEDED(hr));

    return handlerRoutine;
}
#endif
