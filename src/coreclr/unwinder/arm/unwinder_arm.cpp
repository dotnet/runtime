// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "utilcode.h"

#include "unwinder_arm.h"

#define DBS_EXTEND64(x) ((DWORD64)x)
#define MEMORY_READ_BYTE(params, addr)       (*dac_cast<PTR_BYTE>(addr))
#define MEMORY_READ_DWORD(params, addr)      (*dac_cast<PTR_DWORD>(addr))
#define MEMORY_READ_QWORD(params, addr)      (*dac_cast<PTR_UINT64>(addr))
#define MAX_PROLOG_SIZE                 16
#define MAX_EPILOG_SIZE                 16

#define STATUS_UNWIND_UNSUPPORTED_VERSION   STATUS_UNSUCCESSFUL


#define UPDATE_CONTEXT_POINTERS(Params, RegisterNumber, Address)                    \
do {                                                                                \
    if (ARGUMENT_PRESENT(Params)) {                                                 \
        PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
        if (ARGUMENT_PRESENT(ContextPointers)) {                                    \
            if (RegisterNumber >=  4 && RegisterNumber <= 11) {                     \
                (&ContextPointers->R4)[RegisterNumber - 4] = (PULONG)Address;       \
            } else if (RegisterNumber == 14) {                                      \
                ContextPointers->Lr = (PULONG)Address;                              \
            }                                                                       \
        }                                                                           \
    }                                                                               \
} while (0)

#define UPDATE_FP_CONTEXT_POINTERS(Params, RegisterNumber, Address)                 \
do {                                                                                \
    if (ARGUMENT_PRESENT(Params)) {                                                 \
        PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
        if (ARGUMENT_PRESENT(ContextPointers) &&                                    \
            (RegisterNumber >=  8) &&                                               \
            (RegisterNumber <= 15)) {                                               \
                                                                                    \
            (&ContextPointers->D8)[RegisterNumber - 8] = (PULONGLONG)Address;       \
        }                                                                           \
    }                                                                               \
} while (0)

#define VALIDATE_STACK_ADDRESS(Params, Context, DataSize, Alignment, OutStatus)
#define UNWIND_PARAMS_SET_TRAP_FRAME(Params, Address)


//
// Macro for accessing an integer register by index.
//

#define CONTEXT_REGISTER(ctx, idx)    ((&(ctx)->R0)[idx])

typedef struct _ARM_UNWIND_PARAMS
{
    PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers;
} ARM_UNWIND_PARAMS, *PARM_UNWIND_PARAMS;

//
// The ConditionTable is used to look up the state of a condition
// based on the CPSR flags N,Z,C,V, which reside in the upper 4
// bits. To use this table, take the condition you are interested
// in and use it as the index to look up the UINT16 from the table.
// Then right-shift that value by the upper 4 bits of the CPSR,
// and the low bit will be the result.
//
// The bits in the CPSR are ordered (MSB to LSB): N,Z,C,V. Taken
// together, this is called the CpsrFlags.
//
// The macros below are defined such that:
//
//    N = (NSET_MASK >> CpsrFlags) & 1
//    Z = (ZSET_MASK >> CpsrFlags) & 1
//    C = (CSET_MASK >> CpsrFlags) & 1
//    V = (VSET_MASK >> CpsrFlags) & 1
//
// Also:
//
//    (N == V) = (NEQUALV_MASK >> CpsrFlags) & 1
//

#define NSET_MASK        (0xff00)
#define ZSET_MASK        (0xf0f0)
#define CSET_MASK        (0xcccc)
#define VSET_MASK        (0xaaaa)

#define NEQUALV_MASK     ((NSET_MASK & VSET_MASK) | (~NSET_MASK & ~VSET_MASK))

static const UINT16 ConditionTable[16] =
{
    (UINT16)(ZSET_MASK),                     // EQ: Z
    (UINT16)(~ZSET_MASK),                    // NE: !Z
    (UINT16)(CSET_MASK),                     // CS: C
    (UINT16)(~CSET_MASK),                    // CC: !C
    (UINT16)(NSET_MASK),                     // MI: N
    (UINT16)(~NSET_MASK),                    // PL: !N
    (UINT16)(VSET_MASK),                     // VS: V
    (UINT16)(~VSET_MASK),                    // VC: !V
    (UINT16)(CSET_MASK & ~ZSET_MASK),        // HI: C & !Z
    (UINT16)(~CSET_MASK | ZSET_MASK),        // LO: !C | Z
    (UINT16)(NEQUALV_MASK),                  // GE: N == V
    (UINT16)(~NEQUALV_MASK),                 // LT: N != V
    (UINT16)(NEQUALV_MASK & ~ZSET_MASK),     // GT: (N == V) & !Z
    (UINT16)(~NEQUALV_MASK | ZSET_MASK),     // LE: (N != V) | Z
    (UINT16)(0xffff),                        // AL: always
    (UINT16)(0x0000)                         // NV: never
};


//
// This table describes the size of each unwind code, in bytes (lower nibble),
// along with the size of the corresponding machine code, in halfwords
// (upper nibble).
//

static const BYTE UnwindOpTable[256] =
{
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,

    0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,  0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,
    0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,  0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,
    0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,  0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,
    0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,  0x22,0x22,0x22,0x22,0x22,0x22,0x22,0x22,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
    0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,  0x21,0x21,0x21,0x21,0x21,0x21,0x21,0x21,
    0x21,0x21,0x21,0x21,0x21,0x21,0x21,0x21,  0x22,0x22,0x22,0x22,0x12,0x12,0x02,0x22,
    0x01,0x01,0x01,0x01,0x01,0x22,0x22,0x13,  0x14,0x23,0x24,0x11,0x21,0x10,0x20,0x00
};


typedef struct _ARM_CONTEXT_OFFSETS
{
    UINT16      Alignment;
    UINT16      TotalSize;
    UINT16      RegOffset[13];
    UINT16      FpRegOffset[32];
    UINT16      SpOffset;
    UINT16      LrOffset;
    UINT16      PcOffset;
    UINT16      CpsrOffset;
    UINT16      FpscrOffset;
} ARM_CONTEXT_OFFSETS, *PARM_CONTEXT_OFFSETS;

const UINT16 OFFSET_NONE = (UINT16)~0;

static const ARM_CONTEXT_OFFSETS TrapFrameOffsets =
{  8, 272, { 248,252,256,260, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, 72 },
   { 184, 192, 200, 208, 216, 224, 232, 240, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE,
     OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE}, 64, 68, 264,
   268, 176};

static const ARM_CONTEXT_OFFSETS MachineFrameOffsets =
{  8,   8, {  OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE },
   {OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE,
    OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE, OFFSET_NONE}, 0, OFFSET_NONE,  4, OFFSET_NONE , OFFSET_NONE};

static const ARM_CONTEXT_OFFSETS ContextOffsets =
{ 16, 416, {   4,  8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52 },
  { 80, 88, 96, 104, 112, 120, 128, 136, 144, 152, 160, 168, 176, 184, 192, 200,
    208, 216, 224, 232, 240, 248, 256, 264, 272, 280, 288, 296, 304, 312, 320,
    328}, 56, 60, 64, 68, 72};


//
// This table provides the register mask described by the given C/L/R/Reg bit
// combinations in the compact pdata format, along with the number of VFP
// registers to save in bits 16-19.
//

static const ULONG RegisterMaskLookup[1 << 6] =
{               // C L R Reg
    0x00010,     // 0 0 0 000
    0x00030,     // 0 0 0 001
    0x00070,     // 0 0 0 010
    0x000f0,     // 0 0 0 011
    0x001f0,     // 0 0 0 100
    0x003f0,     // 0 0 0 101
    0x007f0,     // 0 0 0 110
    0x00ff0,     // 0 0 0 111

    0x10000,     // 0 0 1 000
    0x20000,     // 0 0 1 001
    0x30000,     // 0 0 1 010
    0x40000,     // 0 0 1 011
    0x50000,     // 0 0 1 100
    0x60000,     // 0 0 1 101
    0x70000,     // 0 0 1 110
    0x00000,     // 0 0 1 111

    0x04010,     // 0 1 0 000
    0x04030,     // 0 1 0 001
    0x04070,     // 0 1 0 010
    0x040f0,     // 0 1 0 011
    0x041f0,     // 0 1 0 100
    0x043f0,     // 0 1 0 101
    0x047f0,     // 0 1 0 110
    0x04ff0,     // 0 1 0 111

    0x14000,     // 0 1 1 000
    0x24000,     // 0 1 1 001
    0x34000,     // 0 1 1 010
    0x44000,     // 0 1 1 011
    0x54000,     // 0 1 1 100
    0x64000,     // 0 1 1 101
    0x74000,     // 0 1 1 110
    0x04000,     // 0 1 1 111

    0x00810,     // 1 0 0 000
    0x00830,     // 1 0 0 001
    0x00870,     // 1 0 0 010
    0x008f0,     // 1 0 0 011
    0x009f0,     // 1 0 0 100
    0x00bf0,     // 1 0 0 101
    0x00ff0,     // 1 0 0 110
    0x0ffff,     // 1 0 0 111

    0x1ffff,     // 1 0 1 000
    0x2ffff,     // 1 0 1 001
    0x3ffff,     // 1 0 1 010
    0x4ffff,     // 1 0 1 011
    0x5ffff,     // 1 0 1 100
    0x6ffff,     // 1 0 1 101
    0x7ffff,     // 1 0 1 110
    0x0ffff,     // 1 0 1 111

    0x04810,     // 1 1 0 000
    0x04830,     // 1 1 0 001
    0x04870,     // 1 1 0 010
    0x048f0,     // 1 1 0 011
    0x049f0,     // 1 1 0 100
    0x04bf0,     // 1 1 0 101
    0x04ff0,     // 1 1 0 110
    0x0ffff,     // 1 1 0 111

    0x14800,     // 1 1 1 000
    0x24800,     // 1 1 1 001
    0x34800,     // 1 1 1 010
    0x44800,     // 1 1 1 011
    0x54800,     // 1 1 1 100
    0x64800,     // 1 1 1 101
    0x74800,     // 1 1 1 110
    0x04800      // 1 1 1 111
};



NTSTATUS
RtlpUnwindCustom(
    __inout PT_CONTEXT ContextRecord,
    _In_ BYTE Opcode,
    _In_ PARM_UNWIND_PARAMS UnwindParams
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
    const ARM_CONTEXT_OFFSETS *Offsets;
    ULONG RegIndex;
    ULONG SourceAddress;
    NTSTATUS Status;

    //
    // Determine which set of offsets to use
    //

    switch (Opcode)
    {
    case 0:
        Offsets = &TrapFrameOffsets;
        break;

    case 1:
        Offsets = &MachineFrameOffsets;
        break;

    case 2:
        Offsets = &ContextOffsets;
        break;

    default:
        return STATUS_UNSUCCESSFUL;
    }

    //
    // Handle general registers first
    //

    Status = STATUS_SUCCESS;
    VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, Offsets->TotalSize, Offsets->Alignment, &Status);
    if (!NT_SUCCESS(Status)) {
        return Status;
    }

    for (RegIndex = 0; RegIndex < 13; RegIndex++) {
        if (Offsets->RegOffset[RegIndex] != OFFSET_NONE) {
            SourceAddress = ContextRecord->Sp + Offsets->RegOffset[RegIndex];
            UPDATE_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
            CONTEXT_REGISTER(ContextRecord, RegIndex) =
                    MEMORY_READ_DWORD(UnwindParams, SourceAddress);
        }
    }

    for (RegIndex = 0; RegIndex < 32; RegIndex++) {
        if (Offsets->FpRegOffset[RegIndex] != OFFSET_NONE) {
            SourceAddress = ContextRecord->Sp + Offsets->FpRegOffset[RegIndex];
            UPDATE_FP_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
            ContextRecord->D[RegIndex] = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
        }
    }

    //
    // For the trap frame case, remember the trap frame at the current SP.
    //

    if (Opcode == 0) {
        UNWIND_PARAMS_SET_TRAP_FRAME(UnwindParams, ContextRecord->Sp);
    }

    //
    // Link register and PC next
    //

    if (Offsets->LrOffset != OFFSET_NONE) {
        SourceAddress = ContextRecord->Sp + Offsets->LrOffset;
        ContextRecord->Lr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);
    }
    if (Offsets->PcOffset != OFFSET_NONE) {
        SourceAddress = ContextRecord->Sp + Offsets->PcOffset;
        ContextRecord->Pc = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        //
        // If we pull the PC out of one of these, this means we are not
        // unwinding from a call, but rather from another frame.
        //

        ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
    }

    //
    // Finally the stack pointer
    //

    if (Offsets->SpOffset != OFFSET_NONE) {
        SourceAddress = ContextRecord->Sp + Offsets->SpOffset;
        ContextRecord->Sp = MEMORY_READ_DWORD(UnwindParams, SourceAddress);
    } else {
        ContextRecord->Sp += Offsets->TotalSize;
    }

    return STATUS_SUCCESS;
}


NTSTATUS
RtlpPopVfpRegisterRange(
    __inout PT_CONTEXT ContextRecord,
    _In_ ULONG RegStart,
    _In_ ULONG RegStop,
    _In_ PARM_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    Pops a series of floating-point registers in the provided inclusive range.

Arguments:

    ContextRecord - Supplies the address of a context record.

    RegStart - Specifies the index of the first register to pop.

    RegStop - Specifies the index of the final register to pop.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    An NTSTATUS indicating either STATUS_SUCCESS if everything went ok, or
    another status code if there were problems.

--*/

{
    ULONG RegCount;
    ULONG RegIndex;
    NTSTATUS Status;

    //
    // Count and validate the number of registers.
    //

    RegCount = RegStop + 1 - RegStart;
    Status = STATUS_SUCCESS;
    VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, 8 * RegCount, 8, &Status);
    if (Status != STATUS_SUCCESS) {
        return Status;
    }

    //
    // Then pop each register in sequence.
    //

    for (RegIndex = RegStart; RegIndex <= RegStop; RegIndex++) {
        UPDATE_FP_CONTEXT_POINTERS(UnwindParams, RegIndex, ContextRecord->Sp);
        ContextRecord->D[RegIndex] = MEMORY_READ_QWORD(UnwindParams, ContextRecord->Sp);
        ContextRecord->Sp += 8;
    }

    return STATUS_SUCCESS;
}

FORCEINLINE
WORD
RtlpRangeToMask(
    _In_ ULONG Start,
    _In_ ULONG Stop,
    _In_ ULONG Lr
    )

/*++

Routine Description:

    Generate a register mask from a start/stop range, plus a flag
    indicating whether or not to include LR in the list.

Arguments:

    Start - Supplies the index of the first register in the range.

    Stop - Supplies the index of the last register in the range.

    Lr - Supplies a value which, if non-zero, indicates that the LR
        register is to be included in the mask.

Return Value:

    A WORD value containing a bitmask of the registers.

--*/

{
    WORD Result;

    Result = 0;
    if (Start <= Stop) {
        Result |= ((1 << (Stop + 1)) - 1) - ((1 << Start) - 1);
    }
    return Result | ((Lr != 0) ? (1 << 14) : 0);
}

NTSTATUS
RtlpPopRegisterMask(
    __inout PT_CONTEXT ContextRecord,
    _In_ WORD RegMask,
    _In_ PARM_UNWIND_PARAMS UnwindParams
    )
/*++

Routine Description:

    Pops a series of integer registers based on a provided register mask.

Arguments:

    ContextRecord - Supplies the address of a context record.

    RegMask - Specifies a 16-bit mask of registers to pop.

    UnwindParams - Additional parameters shared with caller.

Return Value:

    An NTSTATUS indicating either STATUS_SUCCESS if everything went ok, or
    another status code if there were problems.

--*/

{
    ULONG RegCount;
    ULONG RegIndex;
    NTSTATUS Status;

    //
    // Count and validate the number of registers.
    //

    RegCount = 0;
    for (RegIndex = 0; RegIndex < 15; RegIndex++) {
        RegCount += (RegMask >> RegIndex) & 1;
    }

    Status = STATUS_SUCCESS;
    VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, 4 * RegCount, 4, &Status);
    if (Status != STATUS_SUCCESS) {
        return Status;
    }

    //
    // Then pop each register in sequence.
    //

    for (RegIndex = 0; RegIndex < 15; RegIndex++) {
        if ((RegMask & (1 << RegIndex)) != 0) {
            UPDATE_CONTEXT_POINTERS(UnwindParams, RegIndex, ContextRecord->Sp);
            CONTEXT_REGISTER(ContextRecord, RegIndex) =
                    MEMORY_READ_DWORD(UnwindParams, ContextRecord->Sp);
            ContextRecord->Sp += 4;
        }
    }

    //
    // If we popped LR, move it to the PC.
    //

    if ((RegMask & 0x4000) != 0) {
        ContextRecord->Pc = ContextRecord->Lr;
    }

    return STATUS_SUCCESS;
}

FORCEINLINE
BOOLEAN
RtlpCheckCondition(
    _In_ PT_CONTEXT ContextRecord,
    _In_ ULONG Condition
    )

/*++

Routine Description:

    Checks the condition codes against the provided condition, and determines
    whether or not the instruction will be executed.

Arguments:

    ContextRecord - Supplies the address of a context record.

    Condition - The condition to test (only low 4 bits matter).

Return Value:

    TRUE if the condition is met; FALSE otherwise.

--*/

{
    return (ConditionTable[Condition & 0xf] >> (ContextRecord->Cpsr >> 28)) & 1;
}

ULONG
RtlpComputeScopeSize(
    _In_ ULONG UnwindCodePtr,
    _In_ ULONG UnwindCodesEndPtr,
    _In_ BOOLEAN IsEpilog,
    _In_ PVOID UnwindParams
    )

/*++

Routine Description:

    Computes the size of an prolog or epilog

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
    BYTE TableValue;
    BYTE Opcode;

    //
    // Iterate through the unwind codes until we hit an end marker.
    // While iterating, accumulate the total scope size.
    //

    ScopeSize = 0;
    Opcode = 0;
    while (UnwindCodePtr < UnwindCodesEndPtr && (Opcode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr)) < 0xfd) {
        TableValue = UnwindOpTable[Opcode];
        ScopeSize += TableValue >> 4;
        UnwindCodePtr += TableValue & 0xf;
    }

    //
    // Handle the special epilog-only end codes.
    //

    if (Opcode >= 0xfd && Opcode <= 0xfe && IsEpilog) {
        ScopeSize += Opcode - 0xfc;
    }
    return ScopeSize;
}


HRESULT
RtlpUnwindFunctionCompact(
    _In_ ULONG ControlPcRva,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    __inout PT_CONTEXT ContextRecord,
    _Out_ PULONG EstablisherFrame,
    _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine,
    _Out_ PVOID *HandlerData,
    _In_ PARM_UNWIND_PARAMS UnwindParams
    )
{
    ULONG CBit;
    ULONG ComputeFramePointerLength;
    ULONG CurrentOffset;
    ULONG EpilogLength;
    ULONG FunctionLength;
    ULONG HBit;
    ULONG OffsetInFunction;
    ULONG OffsetInScope;
    ULONG PopMask;
    ULONG PrologLength;
    ULONG PushMask;
    ULONG PushPopParamsLength;
    ULONG PushPopFloatingPointLength;
    ULONG PushPopIntegerLength;
    ULONG RetBits;
    ULONG ReturnLength;
    ULONG ScopeStart;
    ULONG StackAdjustLength;
    ULONG StackAdjust;
    NTSTATUS Status;
    ULONG UnwindData;
    ULONG VfpSaveCount;

    UnwindData = FunctionEntry->UnwindData;
    Status = STATUS_SUCCESS;

    //
    // Compact records always describe an unwind to a call.
    //

    ContextRecord->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;

    //
    // Extract the basic information about how to do a full unwind.
    //

    FunctionLength = (UnwindData >> 2) & 0x7ff;
    RetBits = (UnwindData >> 13) & 3;
    HBit = (UnwindData >> 15) & 1;
    CBit = (UnwindData >> 21) & 1;
    StackAdjust = (UnwindData >> 22) & 0x3ff;

    //
    // Determine push/pop masks based on this information. This comes
    // from a mix of the C, L, R, and Reg fields.
    //

    VfpSaveCount = RegisterMaskLookup[(UnwindData >> 16) & 0x3f];
    PushMask = PopMask = VfpSaveCount & 0xffff;
    VfpSaveCount >>= 16;

    //
    // Move LR->PC for the pop case if the Ret field is 0. This must be
    // accurate so that the opcode size computation below is correct.
    //

    if (RetBits == 0) {
        _ASSERTE((PopMask & 0x4000) != 0);
        PopMask = (PopMask & ~0x4000) | 0x8000;
    }

    //
    // If the stack adjustment is folded into the push/pop, encode this
    // by setting one of the low 4 bits of the push/pop mask and recovering
    // the actual stack adjustment.
    //

    if (StackAdjust >= 0x3f4) {
        PushMask |= StackAdjust & 4;
        PopMask |= StackAdjust & 8;
        StackAdjust = (StackAdjust & 3) + 1;
    }

    //
    // If we're near the start of the function (within 9 halfwords),
    // see if we are within the prolog.
    //
    // N.B. If the low 2 bits of the UnwindData are 2, then we have
    // no prolog.
    //

    OffsetInFunction = (ControlPcRva - (FunctionEntry->BeginAddress & ~1)) / 2;
    OffsetInScope = 0;
    if (OffsetInFunction < 9 && (UnwindData & 3) != 2) {

        //
        // Compute sizes for each opcode in the prolog.
        //

        PushPopParamsLength = (HBit != 0) ? 1 : 0;
        PushPopIntegerLength = (PushMask == 0) ? 0 :
                               ((PushMask & 0xbf00) == 0) ? 1 : 2;
        ComputeFramePointerLength = (CBit == 0) ? 0 :
                                    ((PushMask & ~0x4800) == 0) ? 1 : 2;
        PushPopFloatingPointLength = (VfpSaveCount != 0) ? 2 : 0;
        StackAdjustLength = (StackAdjust == 0 || (PushMask & 4) != 0) ? 0 :
                            (StackAdjust < 0x80) ? 1 : 2;

        //
        // Compute the total prolog length and determine if we are within
        // its scope.
        //
        // N.B. We must execute prolog operations backwards to unwind, so
        // our final scope offset in this case is the distance from the end.
        //

        PrologLength = PushPopParamsLength +
                       PushPopIntegerLength +
                       ComputeFramePointerLength +
                       PushPopFloatingPointLength +
                       StackAdjustLength;

        if (OffsetInFunction < PrologLength) {
            OffsetInScope = PrologLength - OffsetInFunction;
        }
    } else {
        ComputeFramePointerLength = 0;
        PushPopParamsLength = 0;
        PushPopFloatingPointLength = 0;
        PushPopIntegerLength = 0;
        StackAdjustLength = 0;
    }

    //
    // If we're near the end of the function (within 8 halfwords), see if
    // we are within the epilog.
    //
    // N.B. If Ret == 3, then we have no epilog.
    //

    if (OffsetInScope == 0 && OffsetInFunction + 8 >= FunctionLength && RetBits != 3) {

        //
        // Compute sizes for each opcode in the epilog.
        //

        StackAdjustLength = (StackAdjust == 0 || (PopMask & 8) != 0) ? 0 :
                            (StackAdjust < 0x80) ? 1 : 2;
        PushPopFloatingPointLength = (VfpSaveCount != 0) ? 2 : 0;
        ComputeFramePointerLength = 0;
        PushPopIntegerLength = (PopMask == 0 || (HBit != 0 && RetBits == 0 && PopMask == 0x8000)) ? 0 :
                               ((PopMask & 0x7f00) == 0) ? 1 : 2;
        PushPopParamsLength = (HBit == 0) ? 0 : (RetBits == 0) ? 2 : 1;
        ReturnLength = RetBits;

        //
        // Compute the total epilog length and determine if we are within
        // its scope.
        //

        EpilogLength = StackAdjustLength +
                       PushPopFloatingPointLength +
                       PushPopIntegerLength +
                       PushPopParamsLength +
                       ReturnLength;

        ScopeStart = FunctionLength - EpilogLength;
        if (OffsetInFunction > ScopeStart) {
            OffsetInScope = OffsetInFunction - ScopeStart;
            PushMask = PopMask & 0x1fff;
            if (HBit == 0) {
                PushMask |= ((PopMask >> 1) & 0x4000);
            }
        }
    }

    //
    // Process operations backwards, in the order: stack deallocation,
    // VFP register popping, integer register popping, parameter home
    // area recovery.
    //
    // First case is simple: we process everything with no regard for
    // the current offset within the scope.
    //

    if (OffsetInScope == 0) {

        ContextRecord->Sp += 4 * StackAdjust;
        if (VfpSaveCount != 0) {
            Status = RtlpPopVfpRegisterRange(ContextRecord, 8, 8 + VfpSaveCount - 1, UnwindParams);
        }
        PushMask &= 0xfff0;
        if (PushMask != 0) {
            Status = RtlpPopRegisterMask(ContextRecord, (WORD)PushMask, UnwindParams);
        }
        if (HBit != 0) {
            ContextRecord->Sp += 4 * 4;
        }
    }

    //
    // Second case is more complex: we must step along each operation
    // to ensure it should be executed.
    //

    else {

        CurrentOffset = 0;
        if (CurrentOffset >= OffsetInScope && StackAdjustLength != 0) {
            ContextRecord->Sp += 4 * StackAdjust;
        }
        CurrentOffset += StackAdjustLength;

        if (CurrentOffset >= OffsetInScope && PushPopFloatingPointLength != 0) {
            Status = RtlpPopVfpRegisterRange(ContextRecord, 8, 8 + VfpSaveCount - 1, UnwindParams);
        }
        CurrentOffset += PushPopFloatingPointLength;

        //
        // N.B. We don't need to undo any side effects of frame pointer linkage
        //

        CurrentOffset += ComputeFramePointerLength;

        //
        // N.B. In the epilog case above, we copied PopMask to PushMask
        //

        if (CurrentOffset >= OffsetInScope && PushPopIntegerLength != 0) {
            PushMask &= 0xfff0;
            Status = RtlpPopRegisterMask(ContextRecord, (WORD)PushMask, UnwindParams);
            if (StackAdjustLength == 0) {
                ContextRecord->Sp += 4 * StackAdjust;
            }
        }
        CurrentOffset += PushPopIntegerLength;

        //
        // N.B. In the epilog case, we also need to pop the return address
        //

        if (CurrentOffset >= OffsetInScope && PushPopParamsLength != 0) {
            if (PushPopParamsLength == 2) {
                Status = RtlpPopRegisterMask(ContextRecord, 1 << 14, UnwindParams);
            }
            ContextRecord->Sp += 4 * 4;
        }
    }

    //
    // If we succeeded, post-process the results a bit
    //

    if (Status == STATUS_SUCCESS) {

        //
        // Since we always POP to the LR, recover the final PC from there.
        // Also set the establisher frame equal to the final stack pointer.
        //

        ContextRecord->Pc = ContextRecord->Lr;
        *EstablisherFrame = ContextRecord->Sp;

        if (ARGUMENT_PRESENT(HandlerRoutine)) {
            *HandlerRoutine = NULL;
        }
        *HandlerData = NULL;
    }

    return Status;
}



HRESULT
RtlpUnwindFunctionFull(
    _In_ ULONG ControlPcRva,
    _In_ ULONG ImageBase,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    __inout PT_CONTEXT ContextRecord,
    _Out_ PULONG EstablisherFrame,
    _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine,
    _Out_ PVOID *HandlerData,
    _In_ PARM_UNWIND_PARAMS UnwindParams
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
    ULONG CurCode;
    ULONG EpilogScopeCount;
    PEXCEPTION_ROUTINE ExceptionHandler;
    PVOID ExceptionHandlerData;
    ULONG FunctionLength;
    ULONG HeaderWord;
    ULONG OffsetInFunction;
    ULONG Param;
    ULONG ScopeNum;
    ULONG ScopeSize;
    ULONG ScopeStart;
    ULONG SkipHalfwords;
    HRESULT Status;
    BYTE TableValue;
    ULONG UnwindCodePtr;
    ULONG UnwindCodesEndPtr;
    ULONG UnwindDataPtr;
    ULONG UnwindIndex;
    ULONG UnwindWords;

    //
    // Unless we encounter a special frame, assume that any unwinding
    // will return us to the return address of a call and set the flag
    // appropriately (it will be cleared again if the special cases apply).
    //

    ContextRecord->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;

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
        return E_UNEXPECTED;
    }

    FunctionLength = HeaderWord & 0x3ffff;
    OffsetInFunction = (ControlPcRva - (FunctionEntry->BeginAddress & ~1)) / 2;

    if (OffsetInFunction >= FunctionLength) {
        return E_UNEXPECTED;
    }

    //
    // Determine the number of epilog scope records and the maximum number
    // of unwind codes.
    //

    UnwindWords = (HeaderWord >> 28) & 15;
    EpilogScopeCount = (HeaderWord >> 23) & 31;
    if (EpilogScopeCount == 0 && UnwindWords == 0) {
        EpilogScopeCount = MEMORY_READ_DWORD(UnwindParams, UnwindDataPtr);
        UnwindDataPtr += 4;
        UnwindWords = (EpilogScopeCount >> 16) & 0xff;
        EpilogScopeCount &= 0xffff;
    }
    if ((HeaderWord & (1 << 21)) != 0) {
        UnwindIndex = EpilogScopeCount;
        EpilogScopeCount = 0;
    } else {
        UnwindIndex = 0;
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
    SkipHalfwords = 0;

    //
    // If we're near the start of the function, and this function has a prolog,
    // compute the size of the prolog from the unwind codes. If we're in the
    // midst of it, we still execute starting at unwind code index 0, but we may
    // need to skip some to account for partial execution of the prolog.
    //

    if (OffsetInFunction < MAX_PROLOG_SIZE && ((HeaderWord & (1 << 22)) == 0)) {
        ScopeSize = RtlpComputeScopeSize(UnwindCodePtr, UnwindCodesEndPtr, FALSE, UnwindParams);

        if (OffsetInFunction < ScopeSize) {
            SkipHalfwords = ScopeSize - OffsetInFunction;
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

    if ((HeaderWord & (1 << 21)) != 0) {
        if (OffsetInFunction + MAX_EPILOG_SIZE >= FunctionLength) {
            ScopeSize = RtlpComputeScopeSize(UnwindCodePtr + UnwindIndex, UnwindCodesEndPtr, TRUE, UnwindParams);
            ScopeStart = FunctionLength - ScopeSize;

            if (OffsetInFunction >= ScopeStart) {
                UnwindCodePtr += UnwindIndex;
                SkipHalfwords = OffsetInFunction - ScopeStart;
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

            if (OffsetInFunction < ScopeStart + MAX_EPILOG_SIZE) {
                UnwindIndex = HeaderWord >> 24;
                ScopeSize = RtlpComputeScopeSize(UnwindCodePtr + UnwindIndex, UnwindCodesEndPtr, TRUE, UnwindParams);

                if (RtlpCheckCondition(ContextRecord, HeaderWord >> 20) &&
                    OffsetInFunction < ScopeStart + ScopeSize) {

                    UnwindCodePtr += UnwindIndex;
                    SkipHalfwords = OffsetInFunction - ScopeStart;
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

    while (UnwindCodePtr < UnwindCodesEndPtr && SkipHalfwords > 0) {
        CurCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
        if (CurCode >= 0xfd) {
            break;
        }
        TableValue = UnwindOpTable[CurCode];
        SkipHalfwords -= TableValue >> 4;
        UnwindCodePtr += TableValue & 0xf;
    }

    //
    // Now execute codes until we hit the end.
    //

    Status = STATUS_SUCCESS;
    while (UnwindCodePtr < UnwindCodesEndPtr && Status == STATUS_SUCCESS) {

        CurCode = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
        UnwindCodePtr++;

        //
        // 0x00-0x7f: 2-byte stack adjust ... add sp, sp, #0xval
        //

        if (CurCode < 0x80) {
            ContextRecord->Sp += (CurCode & 0x7f) * 4;
        }

        //
        // 0x80-0xbf: 4-byte bitmasked pop ... pop {r0-r12, lr}
        //

        else if (CurCode < 0xc0) {
            if (UnwindCodePtr >= UnwindCodesEndPtr) {
                Status = E_FAIL;
            } else {
                Param = ((CurCode & 0x20) << 9) |
                        ((CurCode & 0x1f) << 8) |
                        MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                UnwindCodePtr++;
                Status = RtlpPopRegisterMask(ContextRecord, (WORD)Param, UnwindParams);
            }
        }

        //
        // 0xc0-0xcf: 2-byte stack restore ... mov sp, rX
        //

        else if (CurCode < 0xd0) {
            ContextRecord->Sp = CONTEXT_REGISTER(ContextRecord, CurCode & 0x0f);
        }

        else {
            switch (CurCode) {

            //
            // 0xd0-0xd7: 2-byte range pop ... pop {r4-r7, lr}
            //

            case 0xd0:  case 0xd1:  case 0xd2:  case 0xd3:
            case 0xd4:  case 0xd5:  case 0xd6:  case 0xd7:
                Status = RtlpPopRegisterMask(ContextRecord,
                                             RtlpRangeToMask(4, 4 + (CurCode & 3), CurCode & 4),
                                             UnwindParams);
                break;

            //
            // 0xd8-0xdf: 4-byte range pop ... pop {r4-r11, lr}
            //

            case 0xd8:  case 0xd9:  case 0xda:  case 0xdb:
            case 0xdc:  case 0xdd:  case 0xde:  case 0xdf:
                Status = RtlpPopRegisterMask(ContextRecord,
                                             RtlpRangeToMask(4, 8 + (CurCode & 3), CurCode & 4),
                                             UnwindParams);
                break;

            //
            // 0xe0-0xe7: 4-byte range vpop ... vpop {d8-d15}
            //

            case 0xe0:  case 0xe1:  case 0xe2:  case 0xe3:
            case 0xe4:  case 0xe5:  case 0xe6:  case 0xe7:
                Status = RtlpPopVfpRegisterRange(ContextRecord,
                                                 8, 8 + (CurCode & 0x07),
                                                 UnwindParams);
                break;

            //
            // 0xe8-0xeb: 4-byte stack adjust ... addw sp, sp, #0xval
            //

            case 0xe8:  case 0xe9:  case 0xea:  case 0xeb:
                if (UnwindCodePtr >= UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                ContextRecord->Sp += 4 * 256 * (CurCode & 3);
                ContextRecord->Sp += 4 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                UnwindCodePtr++;
                break;

            //
            // 0xec-0xed: 2-byte bitmasked pop ... pop {r0-r7,lr}
            //

            case 0xec:  case 0xed:
                if (UnwindCodePtr >= UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                Status = RtlpPopRegisterMask(ContextRecord,
                                             MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr)
                                                    | ((CurCode << 14) & 0x4000),
                                             UnwindParams);
                UnwindCodePtr++;
                break;

            //
            // 0xee: 0-byte custom opcode
            //

            case 0xee:
                if (UnwindCodePtr >= UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                Param = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                UnwindCodePtr++;
                if ((Param & 0xf0) == 0x00) {
                    Status = RtlpUnwindCustom(ContextRecord,
                                              Param & 0x0f,
                                              UnwindParams);
                } else {
                    Status = E_FAIL;
                }
                break;

            //
            // 0xef: 4-byte stack restore with post-increment ... ldr pc, [sp], #X
            //

            case 0xef:
                if (UnwindCodePtr >= UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                Param = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                UnwindCodePtr++;
                if ((Param & 0xf0) == 0x00) {
                    Status = RtlpPopRegisterMask(ContextRecord,
                                                 0x4000,
                                                 UnwindParams);
                    ContextRecord->Sp += ((Param & 15) - 1) * 4;
                } else {
                    Status = E_FAIL;
                }
                break;

            //
            // 0xf5: 4-byte range vpop ... vpop {d0-d15}
            //

            case 0xf5:
                if (UnwindCodePtr >= UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                Param = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                UnwindCodePtr++;
                Status = RtlpPopVfpRegisterRange(ContextRecord,
                                                 Param >> 4, Param & 0x0f,
                                                 UnwindParams);
                break;

            //
            // 0xf6: 4-byte range vpop ... vpop {d16-d31}
            //

            case 0xf6:
                if (UnwindCodePtr >= UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                Param = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                UnwindCodePtr++;
                Status = RtlpPopVfpRegisterRange(ContextRecord,
                                                 16 + (Param >> 4), 16 + (Param & 0x0f),
                                                 UnwindParams);
                break;

            //
            // 0xf7: 2-byte stack adjust ... add sp, sp, <reg>
            // 0xf9: 4-byte stack adjust ... add sp, sp, <reg>
            //

            case 0xf7:
            case 0xf9:
                if (UnwindCodePtr + 2 > UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                ContextRecord->Sp += 4 * 256 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                ContextRecord->Sp += 4 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr + 1);
                UnwindCodePtr += 2;
                break;

            //
            // 0xf8: 2-byte stack adjust ... add sp, sp, <reg>
            // 0xfa: 4-byte stack adjust ... add sp, sp, <reg>
            //

            case 0xf8:
            case 0xfa:
                if (UnwindCodePtr + 3 > UnwindCodesEndPtr) {
                    Status = E_FAIL;
                    break;
                }
                ContextRecord->Sp += 4 * 256 * 256 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
                ContextRecord->Sp += 4 * 256 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr + 1);
                ContextRecord->Sp += 4 * MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr + 2);
                UnwindCodePtr += 3;
                break;

            //
            // 0xfb: 2-byte no-op/misc instruction
            // 0xfc: 4-byte no-op/misc instruction
            //

            case 0xfb:
            case 0xfc:
                break;

            //
            // 0xfd: 2-byte end (epilog)
            // 0xfe: 4-byte end (epilog)
            // 0xff: generic end
            //

            case 0xfd:
            case 0xfe:
            case 0xff:
                goto finished;

            default:
                Status = E_FAIL;
                break;
            }
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

        if ((ContextRecord->ContextFlags & CONTEXT_UNWOUND_TO_CALL) != 0) {
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


BOOL OOPStackUnwinderArm::Unwind(T_CONTEXT * pContext)
{
    DWORD64 ImageBase = 0;
    HRESULT hr = GetModuleBase(DBS_EXTEND64(pContext->Pc), &ImageBase);
    if (hr != S_OK)
        return FALSE;

    PEXCEPTION_ROUTINE DummyHandlerRoutine;
    PVOID DummyHandlerData;
    ULONG DummyEstablisherFrame;

    DWORD startingPc = pContext->Pc;
    DWORD startingSp = pContext->Sp;

    T_RUNTIME_FUNCTION Rfe;
    if (FAILED(GetFunctionEntry(DBS_EXTEND64(pContext->Pc), &Rfe, sizeof(Rfe))))
        return FALSE;

    if ((Rfe.UnwindData & 3) != 0)
    {
        hr = RtlpUnwindFunctionCompact(pContext->Pc - (ULONG)ImageBase,
                                        &Rfe,
                                        pContext,
                                        &DummyEstablisherFrame,
                                        &DummyHandlerRoutine,
                                        &DummyHandlerData,
                                        NULL);

    }
    else
    {
        hr = RtlpUnwindFunctionFull(pContext->Pc - (ULONG)ImageBase,
                                    (ULONG)ImageBase,
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
    OOPStackUnwinderArm unwinder;
    BOOL res = unwinder.Unwind(pContext);

    if (res && pContextPointers)
    {
        for (int i = 0; i < 8; i++)
        {
            *(&pContextPointers->R4 + i) = &pContext->R4 + i;
        }
    }

    return res;
}

#if defined(HOST_UNIX)
PEXCEPTION_ROUTINE RtlVirtualUnwind(
    _In_ ULONG HandlerType,
    _In_ ULONG ImageBase,
    _In_ ULONG ControlPc,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ OUT PCONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PULONG EstablisherFrame,
    __inout_opt PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers
    )
{
    PEXCEPTION_ROUTINE handlerRoutine;
    HRESULT res;

    ARM_UNWIND_PARAMS unwindParams;
    unwindParams.ContextPointers = ContextPointers;

    if ((FunctionEntry->UnwindData & 3) != 0)
    {
        res = RtlpUnwindFunctionCompact(ControlPc - ImageBase,
                                        FunctionEntry,
                                        ContextRecord,
                                        EstablisherFrame,
                                        &handlerRoutine,
                                        HandlerData,
                                        &unwindParams);

    }
    else
    {
        res = RtlpUnwindFunctionFull(ControlPc - ImageBase,
                                    ImageBase,
                                    FunctionEntry,
                                    ContextRecord,
                                    EstablisherFrame,
                                    &handlerRoutine,
                                    HandlerData,
                                    &unwindParams);
    }

    _ASSERTE(SUCCEEDED(res));

    return handlerRoutine;
}
#endif
