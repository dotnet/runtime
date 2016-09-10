// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************/

#ifndef _INSTR_H_
#define _INSTR_H_
/*****************************************************************************/

#define BAD_CODE 0x0BADC0DE // better not match a real encoding!

/*****************************************************************************/

// clang-format off
DECLARE_TYPED_ENUM(instruction,unsigned)
{
#if defined(_TARGET_XARCH_)
    #define INST0(id, nm, fp, um, rf, wf, mr                ) INS_##id,
    #define INST1(id, nm, fp, um, rf, wf, mr                ) INS_##id,
    #define INST2(id, nm, fp, um, rf, wf, mr, mi            ) INS_##id,
    #define INST3(id, nm, fp, um, rf, wf, mr, mi, rm        ) INS_##id,
    #define INST4(id, nm, fp, um, rf, wf, mr, mi, rm, a4    ) INS_##id,
    #define INST5(id, nm, fp, um, rf, wf, mr, mi, rm, a4, rr) INS_##id,
    #include "instrs.h"

#elif defined(_TARGET_ARM_)
    #define INST1(id, nm, fp, ldst, fmt, e1                                ) INS_##id,
    #define INST2(id, nm, fp, ldst, fmt, e1, e2                            ) INS_##id,
    #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        ) INS_##id,
    #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) INS_##id,
    #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) INS_##id,
    #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) INS_##id,
    #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) INS_##id,
    #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) INS_##id,
    #include "instrs.h"
    #include "x86_instrs.h"

#elif defined(_TARGET_ARM64_)
    #define INST1(id, nm, fp, ldst, fmt, e1                                ) INS_##id,
    #define INST2(id, nm, fp, ldst, fmt, e1, e2                            ) INS_##id,
    #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        ) INS_##id,
    #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) INS_##id,
    #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) INS_##id,
    #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) INS_##id,
    #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) INS_##id,
    #include "instrs.h"

    INS_lea,   // Not a real instruction. It is used for load the address of stack locals

#else
#error Unsupported target architecture
#endif

    INS_none,
    INS_count = INS_none
}
END_DECLARE_TYPED_ENUM(instruction,unsigned)

/*****************************************************************************/

enum insUpdateModes
{
    IUM_RD,
    IUM_WR,
    IUM_RW,
};

/*****************************************************************************/

enum emitJumpKind
{
    EJ_NONE,

    #define JMP_SMALL(en, rev, ins)           EJ_##en,
    #include "emitjmps.h"

    EJ_COUNT
};

/*****************************************************************************/

DECLARE_TYPED_ENUM(GCtype,unsigned)
{
    GCT_NONE,
    GCT_GCREF,
    GCT_BYREF
}
END_DECLARE_TYPED_ENUM(GCtype,unsigned)

// TODO-Cleanup:  Move 'insFlags' under _TARGET_ARM_ 
DECLARE_TYPED_ENUM(insFlags,unsigned)
{
    INS_FLAGS_NOT_SET,
    INS_FLAGS_SET,
    INS_FLAGS_DONT_CARE
};
END_DECLARE_TYPED_ENUM(insFlags,unsigned)

#if defined(_TARGET_ARM_)
DECLARE_TYPED_ENUM(insOpts,unsigned)
{
    INS_OPTS_NONE,
    INS_OPTS_LDST_PRE_DEC,
    INS_OPTS_LDST_POST_INC,

    INS_OPTS_RRX,
    INS_OPTS_LSL,
    INS_OPTS_LSR,
    INS_OPTS_ASR,
    INS_OPTS_ROR
}
END_DECLARE_TYPED_ENUM(insOpts,unsigned)
#elif defined(_TARGET_ARM64_)
DECLARE_TYPED_ENUM(insOpts,unsigned)
{
    INS_OPTS_NONE,

    INS_OPTS_PRE_INDEX,
    INS_OPTS_POST_INDEX,

    INS_OPTS_LSL12,

    INS_OPTS_LSL = 4,
    INS_OPTS_LSR,
    INS_OPTS_ASR,
    INS_OPTS_ROR,

    INS_OPTS_UXTB = 8,
    INS_OPTS_UXTH,
    INS_OPTS_UXTW,
    INS_OPTS_UXTX,
    INS_OPTS_SXTB,
    INS_OPTS_SXTH,
    INS_OPTS_SXTW,
    INS_OPTS_SXTX,

    INS_OPTS_8B  = 16,
    INS_OPTS_16B,
    INS_OPTS_4H,
    INS_OPTS_8H,
    INS_OPTS_2S,
    INS_OPTS_4S,
    INS_OPTS_1D,
    INS_OPTS_2D,

    INS_OPTS_MSL,     // Vector Immediate (shifting ones variant)

    INS_OPTS_S_TO_4BYTE,  // Single to INT32
    INS_OPTS_D_TO_4BYTE,  // Double to INT32  

    INS_OPTS_S_TO_8BYTE,  // Single to INT64
    INS_OPTS_D_TO_8BYTE,  // Double to INT64

    INS_OPTS_4BYTE_TO_S,  // INT32 to Single
    INS_OPTS_4BYTE_TO_D,  // INT32 to Double  

    INS_OPTS_8BYTE_TO_S,  // INT64 to Single
    INS_OPTS_8BYTE_TO_D,  // INT64 to Double

    INS_OPTS_S_TO_D,      // Single to Double
    INS_OPTS_D_TO_S,      // Double to Single

    INS_OPTS_H_TO_S,      // Half to Single
    INS_OPTS_H_TO_D,      // Half to Double

    INS_OPTS_S_TO_H,      // Single to Half
    INS_OPTS_D_TO_H,      // Double to Half
}
END_DECLARE_TYPED_ENUM(insOpts,unsigned)

DECLARE_TYPED_ENUM(insCond,unsigned)
{
    INS_COND_EQ,
    INS_COND_NE,
    INS_COND_HS,
    INS_COND_LO,

    INS_COND_MI,
    INS_COND_PL,
    INS_COND_VS,
    INS_COND_VC,

    INS_COND_HI,
    INS_COND_LS,
    INS_COND_GE,
    INS_COND_LT,

    INS_COND_GT,
    INS_COND_LE,
}
END_DECLARE_TYPED_ENUM(insCond,unsigned)

DECLARE_TYPED_ENUM(insCflags,unsigned)
{
    INS_FLAGS_NONE,
    INS_FLAGS_V,
    INS_FLAGS_C,
    INS_FLAGS_CV,

    INS_FLAGS_Z,
    INS_FLAGS_ZV,
    INS_FLAGS_ZC,
    INS_FLAGS_ZCV,

    INS_FLAGS_N,
    INS_FLAGS_NV,
    INS_FLAGS_NC,
    INS_FLAGS_NCV,

    INS_FLAGS_NZ,
    INS_FLAGS_NZV,
    INS_FLAGS_NZC,
    INS_FLAGS_NZCV,
}
END_DECLARE_TYPED_ENUM(insCFlags,unsigned)

DECLARE_TYPED_ENUM(insBarrier,unsigned)
{
    INS_BARRIER_OSHLD =  1,
    INS_BARRIER_OSHST =  2,
    INS_BARRIER_OSH   =  3,

    INS_BARRIER_NSHLD =  5,
    INS_BARRIER_NSHST =  6,
    INS_BARRIER_NSH   =  7,

    INS_BARRIER_ISHLD =  9,
    INS_BARRIER_ISHST = 10,
    INS_BARRIER_ISH   = 11,

    INS_BARRIER_LD    = 13,
    INS_BARRIER_ST    = 14,
    INS_BARRIER_SY    = 15,
}
END_DECLARE_TYPED_ENUM(insBarrier,unsigned)
#endif

#undef EA_UNKNOWN
DECLARE_TYPED_ENUM(emitAttr,unsigned)
{
                EA_UNKNOWN       = 0x000,
                EA_1BYTE         = 0x001,
                EA_2BYTE         = 0x002,
                EA_4BYTE         = 0x004,
                EA_8BYTE         = 0x008,
                EA_16BYTE        = 0x010,
                EA_32BYTE        = 0x020,
                EA_SIZE_MASK     = 0x03F,

#ifdef _TARGET_64BIT_
                EA_PTRSIZE       = EA_8BYTE,
#else
                EA_PTRSIZE       = EA_4BYTE,
#endif

                EA_OFFSET_FLG    = 0x040,
                EA_OFFSET        = EA_OFFSET_FLG | EA_PTRSIZE,       /* size ==  0 */
                EA_GCREF_FLG     = 0x080,
                EA_GCREF         = EA_GCREF_FLG |  EA_PTRSIZE,       /* size == -1 */
                EA_BYREF_FLG     = 0x100,
                EA_BYREF         = EA_BYREF_FLG |  EA_PTRSIZE,       /* size == -2 */
                EA_DSP_RELOC_FLG = 0x200,
                EA_CNS_RELOC_FLG = 0x400,
}
END_DECLARE_TYPED_ENUM(emitAttr,unsigned)

#define EA_ATTR(x)                  ((emitAttr)(x))
#define EA_SIZE(x)                  ((emitAttr)(((unsigned)(x)) &  EA_SIZE_MASK))
#define EA_SIZE_IN_BYTES(x)         ((UNATIVE_OFFSET)(EA_SIZE(x)))
#define EA_SET_SIZE(x, sz)          ((emitAttr)((((unsigned)(x)) & ~EA_SIZE_MASK) | sz))
#define EA_SET_FLG(x, flg)          ((emitAttr)(((unsigned)(x)) | flg))
#define EA_4BYTE_DSP_RELOC          (EA_SET_FLG(EA_4BYTE, EA_DSP_RELOC_FLG))
#define EA_PTR_DSP_RELOC            (EA_SET_FLG(EA_PTRSIZE, EA_DSP_RELOC_FLG))
#define EA_HANDLE_CNS_RELOC         (EA_SET_FLG(EA_PTRSIZE, EA_CNS_RELOC_FLG))
#define EA_IS_OFFSET(x)             ((((unsigned)(x)) & ((unsigned)EA_OFFSET_FLG)) != 0)
#define EA_IS_GCREF(x)              ((((unsigned)(x)) & ((unsigned)EA_GCREF_FLG)) != 0)
#define EA_IS_BYREF(x)              ((((unsigned)(x)) & ((unsigned)EA_BYREF_FLG)) != 0)
#define EA_IS_GCREF_OR_BYREF(x)     ((((unsigned)(x)) & ((unsigned)(EA_BYREF_FLG | EA_GCREF_FLG))) != 0)
#define EA_IS_DSP_RELOC(x)          ((((unsigned)(x)) & ((unsigned)EA_DSP_RELOC_FLG)) != 0)
#define EA_IS_CNS_RELOC(x)          ((((unsigned)(x)) & ((unsigned)EA_CNS_RELOC_FLG)) != 0)
#define EA_IS_RELOC(x)              (EA_IS_DSP_RELOC(x) || EA_IS_CNS_RELOC(x))
#define EA_TYPE(x)                  ((emitAttr)(((unsigned)(x)) & ~(EA_OFFSET_FLG | EA_DSP_RELOC_FLG | EA_CNS_RELOC_FLG)))

#define EmitSize(x)                 (EA_ATTR(genTypeSize(TypeGet(x))))

// Enum specifying the instruction set for generating floating point or SIMD code.
enum InstructionSet
{
#ifdef _TARGET_XARCH_
    InstructionSet_SSE2,
    InstructionSet_AVX,
#elif defined(_TARGET_ARM_)
    InstructionSet_NEON,
#endif
    InstructionSet_NONE
};
// clang-format on

/*****************************************************************************/
#endif //_INSTR_H_
/*****************************************************************************/
