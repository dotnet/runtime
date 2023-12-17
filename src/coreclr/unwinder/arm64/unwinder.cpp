// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "utilcode.h"
#include "crosscomp.h"

#include "unwinder.h"

#define NOTHING

#define ARM64_CONTEXT T_CONTEXT

#ifndef HOST_ARM64
#define CONTEXT T_CONTEXT
#define PCONTEXT PT_CONTEXT
#define KNONVOLATILE_CONTEXT_POINTERS T_KNONVOLATILE_CONTEXT_POINTERS
#define PKNONVOLATILE_CONTEXT_POINTERS PT_KNONVOLATILE_CONTEXT_POINTERS
#define RUNTIME_FUNCTION T_RUNTIME_FUNCTION
#define PRUNTIME_FUNCTION PT_RUNTIME_FUNCTION
#endif

#ifndef __in
#define __in _In_
#define __out _Out_
#endif

#ifndef FIELD_OFFSET
#define FIELD_OFFSET(type, field)    ((LONG)__builtin_offsetof(type, field))
#endif

#ifdef HOST_UNIX
#define RtlZeroMemory ZeroMemory

typedef enum ARM64_FNPDATA_FLAGS {
    PdataRefToFullXdata = 0,
    PdataPackedUnwindFunction = 1,
    PdataPackedUnwindFragment = 2,
} ARM64_FNPDATA_FLAGS;

typedef enum ARM64_FNPDATA_CR {
    PdataCrUnchained = 0,
    PdataCrUnchainedSavedLr = 1,
    PdataCrChainedWithPac = 2,
    PdataCrChained = 3,
} ARM64_FNPDATA_CR;

#endif // HOST_UNIX

//
// MessageId: STATUS_BAD_FUNCTION_TABLE
//
// MessageText:
//
// A malformed function table was encountered during an unwind operation.
//
#define STATUS_BAD_FUNCTION_TABLE        ((NTSTATUS)0xC00000FFL)

//
// Flags for RtlVirtualUnwind2.
//

#define RTL_VIRTUAL_UNWIND2_VALIDATE_PAC        0x00000001UL

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

#define RTL_VIRTUAL_UNWIND_VALID_FLAGS_ARM64 (RTL_VIRTUAL_UNWIND2_VALIDATE_PAC)

//
// Parameters describing the unwind codes.
//

#define STATUS_UNWIND_UNSUPPORTED_VERSION   STATUS_UNSUCCESSFUL
#define STATUS_UNWIND_NOT_IN_FUNCTION       STATUS_UNSUCCESSFUL
#define STATUS_UNWIND_INVALID_SEQUENCE      STATUS_UNSUCCESSFUL

//
// Macros for accessing memory. These can be overridden if other code
// (in particular the debugger) needs to use them.
//

//
// Macros for accessing memory. These can be overridden if other code
// (in particular the debugger) needs to use them.

#if !defined(DEBUGGER_UNWIND)

#define MEMORY_READ_BYTE(params, addr)       (*dac_cast<PTR_BYTE>(addr))
#define MEMORY_READ_WORD(params, addr)      (*dac_cast<PTR_WORD>(addr))
#define MEMORY_READ_DWORD(params, addr)      (*dac_cast<PTR_DWORD>(addr))
#define MEMORY_READ_QWORD(params, addr)      (*dac_cast<PTR_UINT64>(addr))

#endif

//
// ARM64_UNWIND_PARAMS definition. This is the kernel-specific definition,
// and contains information on the original PC, the stack bounds, and
// a pointer to the non-volatile context pointer array. Any usage of
// these fields must be wrapped in a macro so that the debugger can take
// a direct drop of this code and use it.
//

#if !defined(DEBUGGER_UNWIND)

typedef struct _ARM64_UNWIND_PARAMS
{
    ULONG_PTR       ControlPc;
    PULONG_PTR      LowLimit;
    PULONG_PTR      HighLimit;
    PKNONVOLATILE_CONTEXT_POINTERS ContextPointers;
} ARM64_UNWIND_PARAMS, *PARM64_UNWIND_PARAMS;

#define UNWIND_PARAMS_SET_TRAP_FRAME(Params, Address, Size)

#if !defined(UPDATE_CONTEXT_POINTERS)
#define UPDATE_CONTEXT_POINTERS(Params, RegisterNumber, Address)                \
do {                                                                            \
    PKNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
    if (ARGUMENT_PRESENT(ContextPointers)) {                                    \
        if (RegisterNumber >= 19 && RegisterNumber <= 28) {                     \
            (&ContextPointers->X19)[RegisterNumber - 19] = (PULONG64)Address;   \
        } else if (RegisterNumber == 29) {                                      \
            ContextPointers->Fp = (PULONG64)Address;                            \
        } else if (RegisterNumber == 30) {                                      \
            ContextPointers->Lr = (PULONG64)Address;                            \
        }                                                                       \
    }                                                                           \
} while (0)
#endif // !defined(UPDATE_CONTEXT_POINTERS)

#if !defined(UPDATE_FP_CONTEXT_POINTERS)
#define UPDATE_FP_CONTEXT_POINTERS(Params, RegisterNumber, Address)             \
do {                                                                            \
    PKNONVOLATILE_CONTEXT_POINTERS ContextPointers = (Params)->ContextPointers; \
    if (ARGUMENT_PRESENT(ContextPointers) &&                                    \
        (RegisterNumber >=  8) &&                                               \
        (RegisterNumber <= 15)) {                                               \
                                                                                \
        (&ContextPointers->D8)[RegisterNumber - 8] = (PULONG64)Address;         \
    }                                                                           \
} while (0)
#endif // !defined(UPDATE_FP_CONTEXT_POINTERS)

#if !defined(VALIDATE_STACK_ADDRESS_EX)
#define VALIDATE_STACK_ADDRESS_EX(Params, Context, Address, DataSize, Alignment, OutStatus)
#endif // !defined(VALIDATE_STACK_ADDRESS_EX)

#if !defined(VALIDATE_STACK_ADDRESS)
#define VALIDATE_STACK_ADDRESS(Params, Context, DataSize, Alignment, OutStatus) \
    VALIDATE_STACK_ADDRESS_EX(Params, Context, (Context)->Sp, DataSize, Alignment, OutStatus)
#endif // !defined(VALIDATE_STACK_ADDRESS)

#else // !defined(DEBUGGER_UNWIND)

#if !defined(UPDATE_CONTEXT_POINTERS)
#define UPDATE_CONTEXT_POINTERS(Params, RegisterNumber, Address)
#endif // !defined(UPDATE_CONTEXT_POINTERS)

#if !defined(UPDATE_FP_CONTEXT_POINTERS)
#define UPDATE_FP_CONTEXT_POINTERS(Params, RegisterNumber, Address)
#endif // !defined(UPDATE_FP_CONTEXT_POINTERS)

#if !defined(VALIDATE_STACK_ADDRESS_EX)
#define VALIDATE_STACK_ADDRESS_EX(Params, Context, Address, DataSize, Alignment, OutStatus)
#endif // !defined(VALIDATE_STACK_ADDRESS_EX)

#if !defined(VALIDATE_STACK_ADDRESS)
#define VALIDATE_STACK_ADDRESS(Params, Context, DataSize, Alignment, OutStatus)
#endif // !defined(VALIDATE_STACK_ADDRESS)

#endif // !defined(DEBUGGER_UNWIND)

//
// Macros for stripping pointer authentication (PAC) bits.
//

#if !defined(DEBUGGER_STRIP_PAC)

// NOTE: Pointer authentication is not used by .NET, so the implementation does nothing
#define STRIP_PAC(Params, pointer)

#endif

//
// Macros to clarify opcode parsing
//

#define OPCODE_IS_END(Op) (((Op) & 0xfe) == 0xe4)

//
// This table describes the size of each unwind code, in bytes, for unwind codes
// in the range 0xE0-0xFF.
//

static const BYTE UnwindCodeSizeTable[32] =
{
    4,1,2,1,1,1,1,3, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 2,3,4,5,1,1,1,1
};

//
// This table describes the number of instructions represented by each unwind
// code in the range 0xE0-0xFF.
//

static const BYTE UnwindCodeInstructionCountTable[32] =
{
    1,1,1,1,1,1,1,1,    // 0xE0-0xE7
    0,                  // 0xE8 - MSFT_OP_TRAP_FRAME
    0,                  // 0xE9 - MSFT_OP_MACHINE_FRAME
    0,                  // 0xEA - MSFT_OP_CONTEXT
    0,                  // 0xEB - MSFT_OP_EC_CONTEXT / MSFT_OP_RET_TO_GUEST (unused)
    0,                  // 0xEC - MSFT_OP_CLEAR_UNWOUND_TO_CALL
    0,                  // 0XED - MSFT_OP_RET_TO_GUEST_LEAF (unused)
    0,0,                // 0xEE-0xEF
    0,0,0,0,0,0,0,0,    // 0xF0-0xF7
    1,1,1,1,1,1,1,1     // 0xF8-0xFF
};

#if !defined(ALIGN_DOWN_BY)

#define ALIGN_DOWN_BY(length, alignment) \
    ((ULONG_PTR)(length) & ~((ULONG_PTR)(alignment) - 1))

#endif

#if !defined(ALIGN_UP_BY)

#define ALIGN_UP_BY(length, alignment) \
    (ALIGN_DOWN_BY(((ULONG_PTR)(length) + (alignment) - 1), alignment))

#endif

#define OP_BUFFER_PRE_ADJUST(_sav_slot, _slots) {}
#define OP_BUFFER_POST_ADJUST(_sav_slot, _slots) {(_sav_slot) += (_slots);}

#define DBG_OP(...)

#pragma warning(push)
#pragma warning(disable:4214)   // bit field types other than int
#pragma warning(disable:4201)   // nameless struct/union
#pragma warning(disable:4309)   // truncation of constant value

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wbitfield-constant-conversion"
#endif

void emit_save_fplr(char** buff, LONG offset) {
    union uop {
        char val;
        struct {
            char z : 6;            // pair at[sp + #Z * 8], offset <= 504
            char fixed : 2;
        };
    };

    union uop *op;

    OP_BUFFER_PRE_ADJUST(*buff, 1);

    offset = ((offset)/8);
    op = (union uop*)(*buff);
    op->fixed = 1;
    op->z = (char)offset;

    OP_BUFFER_POST_ADJUST(*buff, 1);
}

void emit_save_fplr_x(char** buff, LONG offset) {
    union uop {
        char val;
        struct {
            char z : 6;            // pair at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
            char fixed : 2;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 1);

    offset = ((-offset)/8)-1;
    op = (union uop*)(*buff);
    op->fixed = 2;
    op->z = (char)offset;

    OP_BUFFER_POST_ADJUST(*buff, 1);
}

void emit_save_regp(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 6;
            short x : 4;        // save r(19 + #X) pair at[sp + #Z * 8], offset <= 504
            short fixed : 6;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((offset)/8);
    op = (union uop*)(*buff);
    op->fixed = 0x32;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_regp_x(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 6;
            short x : 4;        // save pair r(19+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
            short fixed : 6;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((-offset)/8)-1;
    op = (union uop*)(*buff);
    op->fixed = 0x33;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_reg(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 6;
            short x : 4;        // save reg r(19+#X) at [sp+#Z*8], offset <= 504
            short fixed : 6;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((offset)/8);
    op = (union uop*)(*buff);
    op->fixed = 0x34;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_reg_x(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 5;
            short x : 4;        // save reg r(19+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -256
            short fixed : 7;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((-offset)/8)-1;
    op = (union uop*)(*buff);
    op->fixed = 0x6A;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_lrpair(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 6;
            short x : 3;        // save pair <r(19+2*#X),lr> at [sp+#Z*8], offset <= 504
            short fixed : 7;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((offset)/8);
    op = (union uop*)(*buff);
    op->fixed = 0x6B;
    op->x = (short)(reg / 2);
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_fregp(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 6;
            short x : 3;        // save pair d(8+#X) at [sp+#Z*8], offset <= 504
            short fixed : 7;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((offset)/8);
    op = (union uop*)(*buff);
    op->fixed = 0x6C;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_fregp_x(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 6;
            short x : 3;        // save pair d(8 + #X), at[sp - (#Z + 1) * 8]!, pre - indexed offset >= -512
            short fixed : 7;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((-offset)/8)-1;
    op = (union uop*)(*buff);
    op->fixed = 0x6D;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_freg(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 6;
            short x : 3;        // save reg d(8+#X) at [sp+#Z*8], offset <= 504
            short fixed : 7;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((offset)/8);
    op = (union uop*)(*buff);
    op->fixed = 0x6E;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_save_freg_x(char** buff, LONG reg, LONG offset) {
    union uop {
        short val;
        struct {
            short z : 5;
            short x : 3;        // save reg d(8+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -256
            short fixed : 8;
        };
    };

    union uop* op;

    OP_BUFFER_PRE_ADJUST(*buff, 2);

    offset = ((-offset)/8)-1;
    op = (union uop*)(*buff);
    op->fixed = 0xDE;
    op->x = (short)reg;
    op->z = (short)offset;

    OP_BUFFER_POST_ADJUST(*buff, 2);
}

void emit_alloc(char** buff, LONG size) {

    union uop_alloc_l {
        long val;
        struct {
            long x : 24;        // allocate large stack with size < 256M (2^24 *16)
            long fixed : 8;
        };
    };

    union uop_alloc_m {
        short val;
        struct {
            short x : 11;        // allocate large stack with size < 32K (2^11 * 16)
            short fixed : 5;
        };
    };

    union uop_alloc_s {
        char val;
        struct {
            char x : 5;            // allocate small stack with size < 512 (2^5 * 16)
            char fixed : 3;
        };
    };

    if (size >= 16384) {
        union uop_alloc_l* op;

        OP_BUFFER_PRE_ADJUST(*buff, 4);

        op = (union uop_alloc_l*)(*buff);
        op->fixed = 0xE0;
        op->x = size / 16;

        OP_BUFFER_POST_ADJUST(*buff, 4);
    }
    else if (size >= 512) {
        union uop_alloc_m* op;

        OP_BUFFER_PRE_ADJUST(*buff, 2);

        op = (union uop_alloc_m*)(*buff);
        op->fixed = 0x18;
        op->x = (short)(size / 16);

        OP_BUFFER_POST_ADJUST(*buff, 2);
    }
    else {
        union uop_alloc_s* op;

        OP_BUFFER_PRE_ADJUST(*buff, 1);

        op = (union uop_alloc_s*)(*buff);
        op->fixed = 0x0;
        op->x = (char)(size / 16);

        OP_BUFFER_POST_ADJUST(*buff, 1);
    }
}

void emit_end(char** buff) {
    char* op;

    OP_BUFFER_PRE_ADJUST(*buff, 1);

    op = (char*)(*buff);
    *op = 0xE4;

    OP_BUFFER_POST_ADJUST(*buff, 1);
}

void emit_end_c(char** buff) {
    char* op;

    OP_BUFFER_PRE_ADJUST(*buff, 1);

    op = (char*)(*buff);
    *op = 0xE5;

    OP_BUFFER_POST_ADJUST(*buff, 1);
}

void emit_set_fp(char** buff) {
    char* op;

    OP_BUFFER_PRE_ADJUST(*buff, 1);

    op = (char*)(*buff);
    *op = 0xE1;

    OP_BUFFER_POST_ADJUST(*buff, 1);
}

void emit_nop(char** buff) {
    char* op;

    OP_BUFFER_PRE_ADJUST(*buff, 1);

    op = (char*)(*buff);
    *op = 0xE3;

    OP_BUFFER_POST_ADJUST(*buff, 1);
}

void emit_pac(char** buff) {
    char* op;

    OP_BUFFER_PRE_ADJUST(*buff, 1);

    op = (char*)(*buff);
    *op = 0xFC;

    OP_BUFFER_POST_ADJUST(*buff, 1);
}

#ifdef __clang__
#pragma clang diagnostic pop
#endif

#pragma warning(pop)

#define NO_HOME_NOPS ((size_t)-1)

VOID
RtlpExpandCompactToFull (
    _In_ IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY* fnent_pdata,
    _Inout_ IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA* fnent_xdata
)
{

    LONG intsz;
    LONG fpsz;
    LONG savsz;
    LONG locsz;
    LONG famsz;
    BOOLEAN sav_predec_done = FALSE;
    BOOLEAN fp_set = FALSE;
    LONG sav_slot = 0;
    char* op_buffer;
    char* op_buffer_start;
    char* op_buffer_end;
    size_t op_buffer_used;
    size_t ops_before_nops = NO_HOME_NOPS;

    //
    // Calculate sizes.
    //

    famsz = fnent_pdata->FrameSize * 2;
    intsz = fnent_pdata->RegI;
    if (fnent_pdata->CR == PdataCrUnchainedSavedLr) {
        intsz += 1; // lr
    }

    fpsz = fnent_pdata->RegF;
    if (fnent_pdata->RegF != 0) {
        fpsz += 1;
    }

    savsz = intsz + fpsz;

    //
    // Usually Homes are saved as part of the savesz area.
    // In other words, they are saved in the space allocated
    // by the pre-decrement operation performed by a non-volatile
    // register save. If there are no non-volatile register saves,
    // then Homes are saved in the localsz area.
    //

    if (savsz > 0) {
        savsz += (fnent_pdata->H * 8);
    }

    savsz = ALIGN_UP_BY(savsz, 2);
    locsz = famsz - savsz;

    //
    // Initialize xdata main header.
    //

    fnent_xdata->FunctionLength = fnent_pdata->FunctionLength;
    fnent_xdata->Version = 0;
    fnent_xdata->ExceptionDataPresent = 0;
    op_buffer_start = (char*)(fnent_xdata + 1);
    op_buffer_end = op_buffer_start + ((fnent_xdata->CodeWords) * 4);
    op_buffer = op_buffer_start;

    DBG_OP("end\n");
    emit_end(&op_buffer);

    if (fnent_pdata->CR == PdataCrChainedWithPac) {
        DBG_OP("pac\n");
        emit_pac(&op_buffer);
    }

    //
    // Save the integer registers.
    //

    if (intsz != 0) {
        ULONG intreg;

        //
        // Special case for only x19 + LR, for which an _x option is not
        // available, so do the SP decrement by itself first.
        //

        if ((fnent_pdata->RegI == 1) && (fnent_pdata->CR == PdataCrUnchainedSavedLr)) {
            DBG_OP("alloc_s (%i)\n", savsz * 8);
            emit_alloc(&op_buffer, savsz * 8);
            sav_predec_done = TRUE;
        }

        //
        // Issue save-pair instructions as long as there are even number
        // or registers to lave left.
        //

        for (intreg = 0; intreg < ((fnent_pdata->RegI / 2) * 2); intreg += 2) {
            if (!sav_predec_done) {
                DBG_OP("save_regp_x\t(%s, %s, %i)\n", int_reg_names[intreg], int_reg_names[intreg + 1], -savsz * 8);
                emit_save_regp_x(&op_buffer, intreg, -savsz * 8);
                sav_slot += 2;
                sav_predec_done = TRUE;
            }
            else {
                DBG_OP("save_regp\t(%s, %s, %i)\n", int_reg_names[intreg], int_reg_names[intreg + 1], sav_slot * 8);
                emit_save_regp(&op_buffer, intreg, sav_slot * 8);
                sav_slot += 2;
            }
        }

        //
        // Address the remaining possible cases:
        //    - Last remaining odd register
        //    - LR, when CR=1 (saving LR needed but no FP chain)
        //    - Both, as a pair
        //

        if ((fnent_pdata->RegI % 2) == 1) {
            if (fnent_pdata->CR == PdataCrUnchainedSavedLr) {

                //
                // special case at the top of the function makes sure
                // !sav_predec_done can't even happen.
                //

                _ASSERTE(sav_predec_done);

                DBG_OP("save_lrpair\t(%s, %i)\n", int_reg_names[intreg], sav_slot * 8);
                emit_save_lrpair(&op_buffer, intreg, sav_slot * 8);
                sav_slot += 2;
            }
            else {
                if (!sav_predec_done) {
                    DBG_OP("save_reg_x\t(%s, %i)\n", int_reg_names[intreg], -savsz * 8);
                    emit_save_reg_x(&op_buffer, intreg, -savsz * 8);
                    sav_slot += 1;
                    sav_predec_done = TRUE;
                }
                else {
                    DBG_OP("save_reg\t(%s, %i)\n", int_reg_names[intreg], sav_slot * 8);
                    emit_save_reg(&op_buffer, intreg, sav_slot * 8);
                    sav_slot += 1;
                }
            }
        }
        else {
            if (fnent_pdata->CR == PdataCrUnchainedSavedLr) {
                if (!sav_predec_done) {
                    DBG_OP("save_reg_x\t(%s, %i)\n", int_reg_names[11], -savsz * 8);
                    emit_save_reg_x(&op_buffer, 11, -savsz * 8);
                    sav_slot += 1;
                    sav_predec_done = TRUE;
                }
                else {
                    DBG_OP("save_reg\t(%s, %i)\n", int_reg_names[11], sav_slot * 8);
                    emit_save_reg(&op_buffer, 11, sav_slot * 8);
                    sav_slot += 1;
                }
            }
        }
    }

    //
    // Save the floating point registers.
    //

    if (fpsz != 0) {
        LONG fpreg;

        for (fpreg = 0; fpreg < ((fpsz / 2) * 2); fpreg += 2) {
            if (!sav_predec_done) {
                DBG_OP("save_fregp_x\t(%s, %s, %i)\n", fp_reg_names[fpreg], fp_reg_names[fpreg + 1], -savsz * 8);
                emit_save_fregp_x(&op_buffer, fpreg, -savsz * 8);
                sav_slot += 2;
                sav_predec_done = TRUE;
            }
            else {
                DBG_OP("save_fregp\t(%s, %s, %i)\n", fp_reg_names[fpreg], fp_reg_names[fpreg + 1], sav_slot * 8);
                emit_save_fregp(&op_buffer, fpreg, sav_slot * 8);
                sav_slot += 2;
            }
        }

        if ((fpsz % 2) == 1) {
            if (!sav_predec_done) {
                DBG_OP("save_freg_x\t(%s, %i)\n", fp_reg_names[fpreg], -savsz * 8);
                emit_save_freg_x(&op_buffer, fpreg, -savsz * 8);
                sav_slot += 1;
                sav_predec_done = TRUE;
            }
            else {
                DBG_OP("save_freg\t(%s, %i)\n", fp_reg_names[fpreg], sav_slot * 8);
                emit_save_freg(&op_buffer, fpreg, sav_slot * 8);
                sav_slot += 1;
            }
        }
    }

    //
    // Save parameter registers. Record the instructions
    // that save them, if Homes are being saved into the
    // savesz area. If they are being saved into the localsz
    // area, then they don't realy need to be indicated since
    // they are no-ops and there is nothing following them.
    // In that case, the Homes save instructions will just
    // be considered part of the body.
    //

    if ((fnent_pdata->H != 0) && sav_predec_done) {
        ops_before_nops = op_buffer - op_buffer_start;
        DBG_OP("nop\nnop\nnop\nnop\n");
        emit_nop(&op_buffer);
        emit_nop(&op_buffer);
        emit_nop(&op_buffer);
        emit_nop(&op_buffer);
    }

    //
    // Reserve space for locals and fp,lr chain.
    //

    if (locsz > 0) {
        if ((fnent_pdata->CR == PdataCrChained) ||
            (fnent_pdata->CR == PdataCrChainedWithPac)) {

            if (locsz <= (512 / 8)) {
                DBG_OP("save_fplr_x\t(%i)\n", -locsz * 8);
                emit_save_fplr_x(&op_buffer, -locsz * 8);
            }
            else {
                DBG_OP("alloc\t\t(%i)\n", locsz * 8);
                emit_alloc(&op_buffer, locsz * 8);
                DBG_OP("save_fplr\t(%i)\n", 0);
                emit_save_fplr(&op_buffer, 0);
            }

            DBG_OP("set_fp\n");
            emit_set_fp(&op_buffer);
            fp_set = TRUE;
        }
        else {
            DBG_OP("alloc\t\t(%i)\n", locsz * 8);
            emit_alloc(&op_buffer, locsz * 8);
        }
    }

    if (fnent_pdata->Flag == PdataPackedUnwindFragment) {
        DBG_OP("end_c\n");
        emit_end_c(&op_buffer);
    }

    //
    // Adjust epilog information in the header
    //

    if (fnent_pdata->Flag == PdataPackedUnwindFragment) {

        //
        // Fragment case: no epilog
        //

        fnent_xdata->EpilogInHeader = 0;
        fnent_xdata->EpilogCount = 0;
    }
    else {

        //
        // With EpilogInHeader true, EpilogCount represents
        // the op index to the start of the epilog. If the
        // set_fp is present in the prolog, set this field
        // to 1 so that this op is skipped for the epilog.
        //

        fnent_xdata->EpilogInHeader = 1;
        if (fp_set) {
            fnent_xdata->EpilogCount = 1;
        }
        else {
            fnent_xdata->EpilogCount = 0;
        }
    }

    //
    // Flip the buffer around. This will acomplish two
    // needed things:
    //   - Opcodes closer to the body show first;
    //   - Opcodes become big-endian, as they should.
    //

    op_buffer_used = op_buffer - op_buffer_start;
    if (op_buffer_used > 1) {
        char* lo = op_buffer_start;
        char* hi = op_buffer - 1;
        char swap;
        while (lo < hi) {
            swap = *lo;
            *lo++ = *hi;
            *hi-- = swap;
        }
    }

    //
    // On functions with homed parameters, generate the
    // epilog by copying the prolog minus the param
    // saving NOPs.
    //

    if ((ops_before_nops != NO_HOME_NOPS) && (fnent_xdata->EpilogInHeader != 0)) {
        char* src = op_buffer - 1;
        char* dst = src + op_buffer_used -4;
        char* skip = src - ops_before_nops;
        while (src >= op_buffer_start) {
            if (src == skip) {
                src -= 4;
                continue;
            }

            *dst-- = *src--;
        }

        fnent_xdata->EpilogCount += (ULONG)op_buffer_used;
        op_buffer_used = (op_buffer_used * 2) - 4;
    }

    //
    // Adjust the CodeWords count.
    //

    op_buffer_used = ALIGN_UP_BY(op_buffer_used, 4);
    op_buffer_used /= 4;
    fnent_xdata->CodeWords = (ULONG)op_buffer_used;

    return;
}


static
ULONG_PTR
RtlpGetUnwindCodeSize (
    _In_ ULONG UnwindCode,
    _In_opt_ PULONG ScopeSize
    )

/*++

Routine Description:

    This function determines the number of bytes in an unwind code based on the
    first byte of that unwind code.

Argument:

    UnwindCode - Supplies the first byte of the unwind code.

    ScopeSize - Supplies a pointer to a variable that is incremented by the
        number of instructions represented by the specified unwind code.

Return Value:

    The number of bytes in the specified unwind code is returned as the
    function value.

--*/

{
    _ASSERTE(UnwindCode <= 0xFF);

    if (UnwindCode < 0xC0) {
        if (ARGUMENT_PRESENT(ScopeSize)) {
            *ScopeSize += 1;
        }

        return 1;

    } else if (UnwindCode < 0xE0) {
        if (ARGUMENT_PRESENT(ScopeSize)) {
            *ScopeSize += 1;
        }

        return 2;

    } else {
        if (ARGUMENT_PRESENT(ScopeSize)) {
            *ScopeSize += UnwindCodeInstructionCountTable[UnwindCode - 0xE0];
        }

        return UnwindCodeSizeTable[UnwindCode - 0xE0];
    }
}

static
ULONG
RtlpComputeScopeSize (
    __in ULONG_PTR UnwindCodePtr,
    __in ULONG_PTR UnwindCodesEndPtr,
    __in BOOLEAN IsEpilog,
    __in PARM64_UNWIND_PARAMS UnwindParams
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

    UNREFERENCED_PARAMETER(UnwindParams);

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

        UnwindCodePtr += RtlpGetUnwindCodeSize(Opcode, &ScopeSize);
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

static
NTSTATUS
RtlpUnwindRestoreRegisterRange (
    _Inout_ PCONTEXT ContextRecord,
    _In_ LONG SpOffset,
    _In_range_(0, 30) ULONG FirstRegister,
    _In_range_(1, 31-FirstRegister) ULONG RegisterCount,
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
    // Validate non-overflowing register count.
    //

    if ((FirstRegister + RegisterCount) > 31) {
        return STATUS_UNWIND_INVALID_SEQUENCE;
    }

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
        ContextRecord->X[FirstRegister + RegIndex] = MEMORY_READ_QWORD(UnwindParams, CurAddress);
        CurAddress += 8;
    }
    if (SpOffset < 0) {
        ContextRecord->Sp -= SpOffset;
    }

    return STATUS_SUCCESS;
}

static
NTSTATUS
RtlpUnwindRestoreFpRegisterRange (
    __inout PCONTEXT ContextRecord,
    __in LONG SpOffset,
    __in ULONG FirstRegister,
    __in ULONG RegisterCount,
    __in PARM64_UNWIND_PARAMS UnwindParams
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
    // Validate non-overflowing register count.
    //

    if ((FirstRegister + RegisterCount) > 32) {
        return STATUS_UNWIND_INVALID_SEQUENCE;
    }

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

static
NTSTATUS
RtlpUnwindRestoreSimdRegisterRange (
    __inout PCONTEXT ContextRecord,
    __in LONG SpOffset,
    __in ULONG FirstRegister,
    __in ULONG RegisterCount,
    __in PARM64_UNWIND_PARAMS UnwindParams
    )

/*++

Routine Description:

    Restores a series of full SIMD (Q) registers from the stack.

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
    // Validate non-overflowing register count.
    //

    if ((FirstRegister + RegisterCount) > 32) {
        return STATUS_UNWIND_INVALID_SEQUENCE;
    }

    //
    // Compute the source address and validate it.
    //

    CurAddress = ContextRecord->Sp;
    if (SpOffset >= 0) {
        CurAddress += SpOffset;
    }

    Status = STATUS_SUCCESS;
    VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, 16 * RegisterCount, 16, &Status);
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
        ContextRecord->V[FirstRegister + RegIndex].High = MEMORY_READ_QWORD(UnwindParams, CurAddress);
        CurAddress += 8;
    }
    if (SpOffset < 0) {
        ContextRecord->Sp -= SpOffset;
    }

    return STATUS_SUCCESS;
}

static
NTSTATUS
RtlpUnwindCustom (
    __inout PCONTEXT ContextRecord,
    __in BYTE Opcode,
    __in PARM64_UNWIND_PARAMS UnwindParams
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
        // Restore X0-X18, and D0-D7
        //

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, X);
        for (RegIndex = 0; RegIndex < 19; RegIndex++) {
            UPDATE_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
            ContextRecord->X[RegIndex] = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
            SourceAddress += sizeof(ULONG_PTR);
        }

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_KTRAP_FRAME, VfpState);
        VfpStateAddress = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
        if (VfpStateAddress != 0) {

            SourceAddress = VfpStateAddress + FIELD_OFFSET(KARM64_VFP_STATE, Fpcr);
            Fpcr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);
            SourceAddress = VfpStateAddress + FIELD_OFFSET(KARM64_VFP_STATE, Fpsr);
            Fpsr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);
            if (Fpcr != -1 && Fpsr != -1) {

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
    // Machine frame case
    //

    case 0xe9:  // MSFT_OP_MACHINE_FRAME:

        //
        // Ensure there is enough valid space for the machine frame
        //

        VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, 16, 16, &Status);
        if (!NT_SUCCESS(Status)) {
            return Status;
        }

        //
        // Restore the SP and PC, and clear the unwound-to-call flag
        //

        ContextRecord->Sp = MEMORY_READ_QWORD(UnwindParams, StartingSp + 0);
        ContextRecord->Pc = MEMORY_READ_QWORD(UnwindParams, StartingSp + 8);
        ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
        break;

    //
    // Context case
    //

    case 0xea:  // MSFT_OP_CONTEXT:

        //
        // Ensure there is enough valid space for the full CONTEXT structure
        //

        VALIDATE_STACK_ADDRESS(UnwindParams, ContextRecord, sizeof(ARM64_CONTEXT), 16, &Status);
        if (!NT_SUCCESS(Status)) {
            return Status;
        }

        //
        // Restore X0-X28, and D0-D31
        //

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, X);
        for (RegIndex = 0; RegIndex < 29; RegIndex++) {
            UPDATE_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
            ContextRecord->X[RegIndex] = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
            SourceAddress += sizeof(ULONG_PTR);
        }

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, V);
        for (RegIndex = 0; RegIndex < 32; RegIndex++) {
            UPDATE_FP_CONTEXT_POINTERS(UnwindParams, RegIndex, SourceAddress);
            ContextRecord->V[RegIndex].Low = MEMORY_READ_QWORD(UnwindParams, SourceAddress);
            ContextRecord->V[RegIndex].High = MEMORY_READ_QWORD(UnwindParams, SourceAddress + 8);
            SourceAddress += 2 * sizeof(ULONGLONG);
        }

        //
        // Restore SP, LR, PC, and the status registers
        //

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, Cpsr);
        ContextRecord->Cpsr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, Fp);
        ContextRecord->Fp = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, Lr);
        ContextRecord->Lr = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, Sp);
        ContextRecord->Sp = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, Pc);
        ContextRecord->Pc = MEMORY_READ_QWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, Fpcr);
        ContextRecord->Fpcr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, Fpsr);
        ContextRecord->Fpsr = MEMORY_READ_DWORD(UnwindParams, SourceAddress);

        //
        // Inherit the unwound-to-call flag from this context
        //

        SourceAddress = StartingSp + FIELD_OFFSET(ARM64_CONTEXT, ContextFlags);
        ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
        ContextRecord->ContextFlags |=
                        MEMORY_READ_DWORD(UnwindParams, SourceAddress) & CONTEXT_UNWOUND_TO_CALL;
        break;

    case 0xeb:  // MSFT_OP_EC_CONTEXT:
        // NOTE: for .NET, the arm64ec context restoring is not implemented
        _ASSERTE(FALSE);
        return STATUS_UNSUCCESSFUL;

    case 0xec: // MSFT_OP_CLEAR_UNWOUND_TO_CALL
        ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
        ContextRecord->Pc = ContextRecord->Lr;
        break;

    default:
        return STATUS_UNSUCCESSFUL;
    }

    return STATUS_SUCCESS;
}

NTSTATUS
RtlpUnwindFunctionFull (
    __in ULONG ControlPcRva,
    __in ULONG_PTR ImageBase,
    __in PRUNTIME_FUNCTION FunctionEntry,
    __in IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA *FunctionEntryExtended,
    __inout PCONTEXT ContextRecord,
    __out PULONG_PTR EstablisherFrame,
    __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine,
    __out PVOID *HandlerData,
    __in PARM64_UNWIND_PARAMS UnwindParams,
    __in ULONG UnwindFlags
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
        the the language handler data.

    UnwindParams - Additional parameters shared with caller.

    UnwindFlags - Supplies additional flags for the unwind operation.

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
    ULONG OffsetInFunction;
    ULONG ScopeNum;
    ULONG ScopeSize;
    ULONG ScopeStart;
    ULONG SkipWords;
    NTSTATUS Status;
    ULONG_PTR UnwindCodePtr;
    ULONG_PTR UnwindCodesEndPtr;
    ULONG_PTR UnwindDataPtr;
    ULONG UnwindIndex;
    ULONG UnwindWords;

    UNREFERENCED_PARAMETER(UnwindFlags);

    //
    // Unless a special frame is encountered, assume that any unwinding
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

    UnwindDataPtr = (FunctionEntryExtended != NULL) ?
                    ((ULONG_PTR)FunctionEntryExtended) :
                    (ImageBase + FunctionEntry->UnwindData);

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

    UnwindIndex = 0;
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

            //
            // N.B. This code assumes that no handleable exceptions can occur in
            //      the prolog or in a chained shrink-wrapping prolog region.
            //

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
        UnwindCodePtr += RtlpGetUnwindCodeSize(CurCode, NULL);
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
                        2 + (2 * AccumulatedSaveNexts),
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
                        2 + (2 * AccumulatedSaveNexts),
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
                        2 + (2 * AccumulatedSaveNexts),
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
                        2 + (2 * AccumulatedSaveNexts),
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
                        2 + (2 * AccumulatedSaveNexts),
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
        // end_c (11100101): end of unwind code in current chained scope.
        //          Continue unwinding parent scope.
        //

        else if (CurCode == 0xe5) {
            NOTHING;
        }

        //
        // save_next_pair (11100110): save next non-volatile Int or FP register pair.
        //

        else if (CurCode == 0xe6) {
            AccumulatedSaveNexts += 1;
        }

        //
        //      11100111 ' 0pxrrrrr ' ffoooooo
        //      p: 0/1 - single/pair
        //      x: 0/1 - positive offset / negative offset with writeback
        //      r: register number
        //      f: 00/01/10 - X / D / Q
        //      o: offset * 16 for x=1 or p=1 or f=Q / else offset * 8
        //

        else if (CurCode == 0xe7) {
            LONG SpOffset;
            ULONG RegCount;
            union uop {
                unsigned short val;
                struct {
                    unsigned char val1;
                    unsigned char val2;
                };
                struct {
                    unsigned short o : 6;
                    unsigned short f : 2;
                    unsigned short r : 5;
                    unsigned short x : 1;
                    unsigned short p : 1;
                    unsigned short fixed : 1;
                };
            } op;

            op.val2 = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr += 1;
            op.val1 = MEMORY_READ_BYTE(UnwindParams, UnwindCodePtr);
            UnwindCodePtr += 1;

            //
            // save_next_pair only permited for pairs.
            //

            if ((op.p == 0) && (AccumulatedSaveNexts != 0)) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }

            if (op.fixed != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }

            SpOffset = op.o + op.x;
            SpOffset *= ((op.x == 1) || (op.f == 2) || (op.p == 1)) ? (16) : (8);
            SpOffset *= (op.x == 1) ? (-1) : (1);
            RegCount = 1 + op.p + (2 * AccumulatedSaveNexts);
            switch (op.f) {
            case 0:
               Status = RtlpUnwindRestoreRegisterRange(
                            ContextRecord,
                            SpOffset,
                            op.r,
                            RegCount,
                            UnwindParams);
                break;

            case 1:
                Status = RtlpUnwindRestoreFpRegisterRange(
                            ContextRecord,
                            SpOffset,
                            op.r,
                            RegCount,
                            UnwindParams);
                break;

            case 2:
                Status = RtlpUnwindRestoreSimdRegisterRange(
                            ContextRecord,
                            SpOffset,
                            op.r,
                            RegCount,
                            UnwindParams);
                break;

            default:
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }

            AccumulatedSaveNexts = 0;
        }

        //
        // custom_0 (111010xx): restore custom structure
        //

        else if (CurCode >= 0xe8 && CurCode <= 0xec) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }
            Status = RtlpUnwindCustom(ContextRecord, (BYTE) CurCode, UnwindParams);
            FinalPcFromLr = FALSE;
        }

        //
        // pac (11111100): function has pointer authentication 
        //

        else if (CurCode == 0xfc) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }

            STRIP_PAC(UnwindParams, &ContextRecord->Lr);

            //
            // TODO: Implement support for UnwindFlags RTL_VIRTUAL_UNWIND2_VALIDATE_PAC.
            //
        }

        //
        // future/nop: the following ranges represent encodings reserved for
        //      future extension. They are treated as a nop and, therefore, no
        //      unwind action is taken.
        //
        //      11111000|yyyyyyyy
        //      11111001|yyyyyyyy|yyyyyyyy
        //      11111010|yyyyyyyy|yyyyyyyy|yyyyyyyy
        //      11111011|yyyyyyyy|yyyyyyyy|yyyyyyyy|yyyyyyyy
        //      111111xx
        //

        else if (CurCode >= 0xf8) {
            if (AccumulatedSaveNexts != 0) {
                return STATUS_UNWIND_INVALID_SEQUENCE;
            }

            if (CurCode <= 0xfb) {
                UnwindCodePtr += 1 + (CurCode & 0x3);
            }
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
RtlpUnwindFunctionCompact (
    __in ULONG ControlPcRva,
    __in ULONG_PTR ImageBase,
    __in PRUNTIME_FUNCTION FunctionEntry,
    __inout PCONTEXT ContextRecord,
    __out PULONG_PTR EstablisherFrame,
    __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine,
    __out PVOID *HandlerData,
    __in PARM64_UNWIND_PARAMS UnwindParams,
    __in ULONG UnwindFlags
    )
{

    NTSTATUS Status;

    //
    // The longest possible array of unwind opcodes that a compressed format can generate is
    // 28 + 24 bytes. Rounding it up to a multiple of 4, that results in an array of 52 bytes.
    // Note that the following example isn't even fully legal as any allocation above 4KiB would
    // require a call to __chkstk and, thus, rule-out compressed encoding. But since it can be
    // encoded, it is considered here.
    //
    // Compressed:
    //
    // Flag = PdataPackedUnwindFunction
    // RegF = 7
    // RegI = 10
    // H = 1
    // CR = PdataCrChainedWithPac
    // FrameSize = 8000/16;
    //
    // Full Prolog:
    // e1 40 c1 e7 e3 e3 e3 e3 d9 90 d9 0e d8 8c d8 0a ca 08 c9 86 c9 04 c8 82 cc 19 fc e4
    //
    // Full Epilog (same as prolog minus the 4 x NOP for param home spill):
    // e1 40 c1 e7             d9 90 d9 0e d8 8c d8 0a ca 08 c9 86 c9 04 c8 82 cc 19 fc e4
    //
    // E4       end
    // FC       pac
    // CC 19    save_regp_x     (x19, x20, -208)
    // C8 82    save_regp       (x21, x22, 16)
    // C9 04    save_regp       (x23, x24, 32)
    // C9 86    save_regp       (x25, x26, 48)
    // CA 08    save_regp       (x27, x28, 64)
    // D8 0A    save_fregp      (d8, d9, 80)
    // D8 8C    save_fregp      (d10, d11, 96)
    // D9 0E    save_fregp      (d12, d13, 112)
    // D9 90    save_fregp      (d14, d15, 128)
    // E3       nop
    // E3       nop
    // E3       nop
    // E3       nop
    // C1 E7    alloc           (7792)
    // 40       save_fplr       (0)
    // E1       set_fp
    //

    struct LOCAL_XDATA {
        IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA xdata;
        char ops[60];
    } fnent_xdata = {};

    fnent_xdata.xdata.CodeWords = sizeof(fnent_xdata.ops) / 4;
    RtlpExpandCompactToFull(FunctionEntry, &fnent_xdata.xdata);
    Status = RtlpUnwindFunctionFull(ControlPcRva,
                                    ImageBase,
                                    FunctionEntry,
                                    &fnent_xdata.xdata,
                                    ContextRecord,
                                    EstablisherFrame,
                                    HandlerRoutine,
                                    HandlerData,
                                    UnwindParams,
                                    UnwindFlags);

    return Status;
}

#if !defined(DEBUGGER_UNWIND)

NTSTATUS
RtlpxVirtualUnwind (
    _In_ ULONG HandlerType,
    _In_ ULONG_PTR ImageBase,
    _In_ ULONG_PTR ControlPc,
    _In_opt_ PRUNTIME_FUNCTION FunctionEntry,
    _Inout_ PCONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PULONG_PTR EstablisherFrame,
    _Inout_opt_ PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
    _In_opt_ PULONG_PTR LowLimit,
    _In_opt_ PULONG_PTR HighLimit,
    _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine,
    _In_ ULONG UnwindFlags
    )

/*++

Routine Description:

    This function virtually unwinds the specified function by executing its
    prolog code backward or its epilog code forward.

    If a context pointers record is specified, then the address where each
    nonvolatile registers is restored from is recorded in the appropriate
    element of the context pointers record.

Arguments:

    HandlerType - Supplies the handler type expected for the virtual unwind.
        This may be either an exception or an unwind handler. A flag may
        optionally be supplied to indicate that the unwind should assume
        that the instruction at the PC is the one we are interested in
        (versus the PC being a return address).

    ImageBase - Supplies the base address of the image that contains the
        function being unwound.

    ControlPc - Supplies the address where control left the specified
        function.

    FunctionEntry - Supplies the address of the function table entry for the
        specified function. If appropriate, this should have already been
        probed.

    ContextRecord - Supplies the address of a context record.

    HandlerData - Supplies a pointer to a variable that receives a pointer
        the the language handler data.

    EstablisherFrame - Supplies a pointer to a variable that receives the
        the establisher frame pointer value.

    ContextPointers - Supplies an optional pointer to a context pointers
        record.

    LowLimit - Supplies an optional low limit used to bound the establisher
        frame. This must be supplied in conjunction with a high limit.

    HighLimit - Supplies an optional high limit used to bound the establisher
        frame. This must be supplied in conjunction with a low limit.

    HandlerRoutine - Supplies an optional pointer to a variable that receives
        the handler routine address.  If control did not leave the specified
        function in either the prolog or an epilog and a handler of the
        proper type is associated with the function, then the address of the
        language specific exception handler is returned. Otherwise, NULL is
        returned.

    UnwindFlags - Supplies additional flags for the unwind operation.

Return Value:

    STATUS_SUCCESS if the unwind could be completed, a failure status otherwise.
    Unwind can only fail when validation bounds are specified.

--*/

{
    ULONG ControlPcRva;
    NTSTATUS Status;
    ARM64_UNWIND_PARAMS UnwindParams;
    ULONG UnwindType;

    UNREFERENCED_PARAMETER(HandlerType);

    _ASSERTE((UnwindFlags & ~RTL_VIRTUAL_UNWIND_VALID_FLAGS_ARM64) == 0);

    if (FunctionEntry == NULL) {

        //
        // If the function does not have a function entry, then it is
        // a pure leaf/trivial function. This means the stack pointer
        // does not move, and LR is never overwritten, from the time
        // it was called to the time it returns. To unwind such function,
        // assign the value in LR to PC, simulating a simple ret instruction.
        //

        //
        // If the old control PC is the same as the return address,
        // then no progress is being made and the stack is most
        // likely malformed.
        //

        if (ControlPc == ContextRecord->Lr) {
            return STATUS_BAD_FUNCTION_TABLE;
        }

        //
        // Set the point where control left the current function by
        // obtaining the return address from the current context.
        // Also indicate that we unwound from a call so that the
        // language-specific handler can differentiate neighboring
        // exception scopes.
        //

        ContextRecord->Pc = ContextRecord->Lr;
        ContextRecord->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;

        //
        // Set remaining output data and return. All work done.
        //

        *EstablisherFrame = ContextRecord->Sp;
        *HandlerData = NULL;
        if (ARGUMENT_PRESENT(HandlerRoutine)) {
            *HandlerRoutine = NULL;
        }

        return STATUS_SUCCESS;
    }

    //
    // Make sure out-of-bound stack accesses don't send us into an infinite
    // unwinding loop.
    //
#if 0
    __try {
#endif
        //
        // Build an UnwindParams structure containing the starting PC, stack
        // limits, and context pointers.
        //

        UnwindParams.ControlPc = ControlPc;
        UnwindParams.LowLimit = LowLimit;
        UnwindParams.HighLimit = HighLimit;
        UnwindParams.ContextPointers = ContextPointers;
        UnwindType = (FunctionEntry->UnwindData & 3);

        //
        // Unwind type 3 refers to a chained record. The top 30 bits of the
        // unwind data contains the RVA of the parent pdata record.
        //

        if (UnwindType == 3) {
            if ((FunctionEntry->UnwindData & 4) == 0) {
                FunctionEntry = (PRUNTIME_FUNCTION)(ImageBase + FunctionEntry->UnwindData - 3);
                UnwindType = (FunctionEntry->UnwindData & 3);

                _ASSERTE(UnwindType != 3);

                ControlPcRva = FunctionEntry->BeginAddress;

            } else {
                return STATUS_UNWIND_UNSUPPORTED_VERSION;
            }

        } else {
            ControlPcRva = (ULONG)(ControlPc - ImageBase);
        }

        //
        // Identify the compact .pdata format versus the full .pdata+.xdata format.
        //

        if (UnwindType != 0) {
            Status = RtlpUnwindFunctionCompact(ControlPcRva,
                                               ImageBase,
                                               FunctionEntry,
                                               ContextRecord,
                                               EstablisherFrame,
                                               HandlerRoutine,
                                               HandlerData,
                                               &UnwindParams,
                                               UnwindFlags);

        } else {

            Status = RtlpUnwindFunctionFull(ControlPcRva,
                                            ImageBase,
                                            FunctionEntry,
                                            NULL,
                                            ContextRecord,
                                            EstablisherFrame,
                                            HandlerRoutine,
                                            HandlerData,
                                            &UnwindParams,
                                            UnwindFlags);
        }
 #if 0
    }

    //
    // If we do take an exception here, fetch the exception code as the status
    // and do not propagate the exception. Since the exception handler also
    // uses this function, propagating it will most likely generate the same
    // exception at the same point in the unwind, and continuing will typically
    // overflow the kernel stack.
    //

    __except (EXCEPTION_EXECUTE_HANDLER) {
        Status = GetExceptionCode();
    }
#endif // HOST_WINDOWS
    return Status;
}

#endif // !defined(DEBUGGER_UNWIND)

BOOL OOPStackUnwinderArm64::Unwind(T_CONTEXT * pContext)
{
    DWORD64 ImageBase = 0;
    HRESULT hr = GetModuleBase(pContext->Pc, &ImageBase);
    if (hr != S_OK)
        return FALSE;

    PEXCEPTION_ROUTINE DummyHandlerRoutine = NULL;
    PVOID DummyHandlerData;
    DWORD64 DummyEstablisherFrame;

    DWORD64 startingPc = pContext->Pc;
    DWORD64 startingSp = pContext->Sp;

    T_RUNTIME_FUNCTION Rfe;
    if (FAILED(GetFunctionEntry(pContext->Pc, &Rfe, sizeof(Rfe))))
        return FALSE;

    NTSTATUS Status;

    Status = RtlpxVirtualUnwind(0 /* HandlerType */,
                                ImageBase,
                                pContext->Pc,
                                &Rfe,
                                pContext,
                                &DummyHandlerData,
                                &DummyEstablisherFrame,
                                NULL,
                                NULL,
                                NULL,
                                &DummyHandlerRoutine,
                                0);

    //
    // If we fail the unwind, clear the PC to 0. This is recognized by
    // many callers as a failure, given that RtlVirtualUnwind does not
    // return a status code.
    //

    if (!NT_SUCCESS(Status)) {
        pContext->Pc = 0;
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

#undef PRUNTIME_FUNCTION

PEXCEPTION_ROUTINE
RtlVirtualUnwind(
    IN ULONG HandlerType,
    IN ULONG64 ImageBase,
    IN ULONG64 ControlPc,
    IN PRUNTIME_FUNCTION FunctionEntry,
    IN OUT PCONTEXT ContextRecord,
    OUT PVOID *HandlerData,
    OUT PULONG64 EstablisherFrame,
    IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
    )
{
    PEXCEPTION_ROUTINE HandlerRoutine;
    NTSTATUS Status;

    HandlerRoutine = NULL;
    Status = RtlpxVirtualUnwind(HandlerType,
                                ImageBase,
                                ControlPc,
                                (PIMAGE_ARM64_RUNTIME_FUNCTION_ENTRY)FunctionEntry,
                                ContextRecord,
                                HandlerData,
                                EstablisherFrame,
                                ContextPointers,
                                NULL,
                                NULL,
                                &HandlerRoutine,
                                0);

    //
    // If we fail the unwind, clear the PC to 0. This is recognized by
    // many callers as a failure, given that RtlVirtualUnwind does not
    // return a status code.
    //

    if (!NT_SUCCESS(Status)) {
        ContextRecord->Pc = 0;
    }

    return HandlerRoutine;
}
#endif
