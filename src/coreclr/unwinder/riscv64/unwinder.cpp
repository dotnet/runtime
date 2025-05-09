// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "utilcode.h"
#include "crosscomp.h"

#include "unwinder.h"

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

typedef struct _RISCV64_UNWIND_PARAMS
{
    PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers;
} RISCV64_UNWIND_PARAMS, *PRISCV64_UNWIND_PARAMS;


#define UNWIND_PARAMS_SET_TRAP_FRAME(Params, Address, Size)
#define UPDATE_CONTEXT_POINTERS(Params, RegisterNumber, Address)                      \
do {                                                                                  \
    if (ARGUMENT_PRESENT(Params)) {                                                   \
        PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
        if (ARGUMENT_PRESENT(ContextPointers)) {                                      \
            if (RegisterNumber == 1)                                                  \
                ContextPointers->Ra = (PDWORD64)Address;                              \
            else if (RegisterNumber == 8)                                             \
                ContextPointers->Fp = (PDWORD64)Address;                              \
            else if (RegisterNumber == 9)                                             \
                ContextPointers->S1 = (PDWORD64)Address;                              \
            else if (RegisterNumber >= 18 && RegisterNumber <= 27)                    \
                (&ContextPointers->S2)[RegisterNumber - 18] = (PDWORD64)Address;      \
        }                                                                             \
    }                                                                                 \
} while (0)


#define UPDATE_FP_CONTEXT_POINTERS(Params, RegisterNumber, Address)                   \
do {                                                                                  \
    if (ARGUMENT_PRESENT(Params)) {                                                   \
        PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
        if (ARGUMENT_PRESENT(ContextPointers)) {                                       \
            if (RegisterNumber == 8)                                                  \
                ContextPointers->F8 = (PDWORD64)Address;                              \
            else if (RegisterNumber == 9)                                             \
                ContextPointers->F9 = (PDWORD64)Address;                              \
            else if (RegisterNumber >= 18 && RegisterNumber <= 27)                    \
                (&ContextPointers->F18)[RegisterNumber - 18] = (PDWORD64)Address;     \
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
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 00-0F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 10-1F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 20-2F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 30-3F
    2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 40-4F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 50-5F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 60-6F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 70-7F
    2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 80-8F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 90-9F
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // A0-AF
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // B0-BF
    2, 2, 2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 3, 2, 2, 2, // C0-CF
    3, 2, 2, 2, 2, 2, 3, 2, 3, 2, 3, 2, 3, 3, 2, 1, // D0-DF
    4, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // E0-EF
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1  // F0-FF
};

ULONG
RtlpComputeScopeSize(
    _In_ ULONG_PTR UnwindCodePtr,
    _In_ ULONG_PTR UnwindCodesEndPtr,
    _In_ BOOLEAN IsEpilog,
    _In_ PRISCV64_UNWIND_PARAMS UnwindParams
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
    _In_ PRISCV64_UNWIND_PARAMS UnwindParams
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
        *(&ContextRecord->R0 + FirstRegister + RegIndex) = MEMORY_READ_QWORD(UnwindParams, CurAddress);
#else
        ContextRecord->R[FirstRegister + RegIndex] = MEMORY_READ_QWORD(UnwindParams, CurAddress);
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
    _In_ PRISCV64_UNWIND_PARAMS UnwindParams
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
        ContextRecord->F[FirstRegister + RegIndex] = MEMORY_READ_QWORD(UnwindParams, CurAddress);
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
    __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine,
    _Out_ PVOID *HandlerData,
    _In_ PRISCV64_UNWIND_PARAMS UnwindParams
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
        function, as an offset relative to the ImageBase.

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
        the language handler data.

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
    BOOLEAN FinalPcFromRa;
    ULONG FunctionLength;
    ULONG HeaderWord;
    ULONG NextCode, NextCode1, NextCode2;
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
    // Unless a special frame is encountered, assume that any unwinding
    // will return us to the return address of a call and set the flag
    // appropriately (it will be cleared again if the special cases apply).
    //

    ContextRecord->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;

    //
    // By default, unwinding is done by popping to the RA, then copying
    // that RA to the PC. However, some special opcodes require different
    // behavior.
    //

    FinalPcFromRa = TRUE;

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
        assert(!"ShouldNotReachHere");
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
        // alloc_m (11000xxx|xxxxxxxx): allocate large stack with size < 32k (2^11 * 16).
        //

        else if ((CurCode & 0xf8) == 0xc0) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            ContextRecord->Sp += 16 * ((CurCode & 7) << 8);
            ContextRecord->Sp += 16 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
        }

        //
        // save_reg (11010000|000xxxxx|zzzzzzzz): save reg r(1+#X) at [sp+#Z*8], offset <= 2047
        //

        else if (CurCode == 0xd0) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            NextCode1 = (uint8_t)MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreRegisterRange(
                        ContextRecord,
                        8 * NextCode1,
                        1 + NextCode,
                        1,
                        UnwindParams);
        }

        //
        // save_freg (1101110x|xxxxzzzz|zzzzzzzz): save reg f(8+#X) at [sp+#Z*8], offset <= 32767
        //

        else if ((CurCode & 0xfe) == 0xdc) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            NextCode1 = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            Status = RtlpUnwindRestoreFpRegisterRange(
                        ContextRecord,
                        8 * (((NextCode & 0xf) << 8) + NextCode1),
                        8 + (NextCode >> 4) + ((CurCode & 0x1) << 4),
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
        // set_fp (11100001): set up fp: with: ori fp,sp,0
        //

        else if (CurCode == 0xe1) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            ContextRecord->Sp = ContextRecord->Fp;
        }

        //
        // add_fp (11100010|000xxxxx|xxxxxxxx): set up fp with: addi.d fp,sp,#x*8
        //

        else if (CurCode == 0xe2) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            NextCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            NextCode1 = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr++;
            ContextRecord->Sp = ContextRecord->Fp - 8 * ((NextCode << 8) | NextCode1);
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
        // Since we always POP to the RA, recover the final PC from there, unless
        // it was overwritten due to a special case custom unwinding operation.
        // Also set the establisher frame equal to the final stack pointer.
        //

        if (FinalPcFromRa) {
            ContextRecord->Pc = ContextRecord->Ra;
        }

        *EstablisherFrame = ContextRecord->Sp;

        if (ARGUMENT_PRESENT(HandlerRoutine)) {
            *HandlerRoutine = ExceptionHandler;
        }
        *HandlerData = ExceptionHandlerData;
    }

    return Status;
}

BOOL OOPStackUnwinderRISCV64::Unwind(T_CONTEXT * pContext)
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

    assert((Rfe.UnwindData & 3) == 0);
    hr = RtlpUnwindFunctionFull(pContext->Pc - ImageBase,
                                ImageBase,
                                &Rfe,
                                pContext,
                                &DummyEstablisherFrame,
                                &DummyHandlerRoutine,
                                &DummyHandlerData,
                                NULL);

    // PC == 0 means unwinding is finished.
    // Same if no forward progress is made
    if (pContext->Pc == 0 || (startingPc == pContext->Pc && startingSp == pContext->Sp))
        return FALSE;

    return TRUE;
}

BOOL DacUnwindStackFrame(T_CONTEXT *pContext, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers)
{
    OOPStackUnwinderRISCV64 unwinder;
    BOOL res = unwinder.Unwind(pContext);

    if (res && pContextPointers)
    {
        pContextPointers->S1 = &pContext->S1;
        pContextPointers->S2 = &pContext->S2;
        pContextPointers->S3 = &pContext->S3;
        pContextPointers->S4 = &pContext->S4;
        pContextPointers->S5 = &pContext->S5;
        pContextPointers->S6 = &pContext->S6;
        pContextPointers->S7 = &pContext->S7;
        pContextPointers->S8 = &pContext->S8;
        pContextPointers->S9 = &pContext->S9;
        pContextPointers->S10 = &pContext->S10;
        pContextPointers->S11 = &pContext->S11;
        pContextPointers->Fp = &pContext->Fp;
        pContextPointers->Ra = &pContext->Ra;
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

    RISCV64_UNWIND_PARAMS unwindParams;
    unwindParams.ContextPointers = ContextPointers;

    assert((rfe.UnwindData & 3) == 0);
    hr = RtlpUnwindFunctionFull(ControlPc - ImageBase,
                                ImageBase,
                                &rfe,
                                ContextRecord,
                                EstablisherFrame,
                                &handlerRoutine,
                                HandlerData,
                                &unwindParams);

    return handlerRoutine;
}
#endif
