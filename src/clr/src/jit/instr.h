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
enum instruction : unsigned
{
#if defined(_TARGET_XARCH_)
    #define INST0(id, nm, um, mr,                 flags) INS_##id,
    #define INST1(id, nm, um, mr,                 flags) INS_##id,
    #define INST2(id, nm, um, mr, mi,             flags) INS_##id,
    #define INST3(id, nm, um, mr, mi, rm,         flags) INS_##id,
    #define INST4(id, nm, um, mr, mi, rm, a4,     flags) INS_##id,
    #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) INS_##id,
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

    INS_lea,   // Not a real instruction. It is used for load the address of stack locals

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
};

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

enum GCtype : unsigned
{
    GCT_NONE,
    GCT_GCREF,
    GCT_BYREF
};

#if defined(_TARGET_XARCH_)
enum insFlags: uint8_t
{
    INS_FLAGS_None = 0x00,
    INS_FLAGS_ReadsFlags = 0x01,
    INS_FLAGS_WritesFlags = 0x02,
    INS_FLAGS_x87Instr = 0x04,
    INS_Flags_IsDstDstSrcAVXInstruction = 0x08,
    INS_Flags_IsDstSrcSrcAVXInstruction = 0x10,

    //  TODO-Cleanup:  Remove this flag and its usage from _TARGET_XARCH_
    INS_FLAGS_DONT_CARE = 0x00,
};
#elif defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
// TODO-Cleanup: Move 'insFlags' under _TARGET_ARM_
enum insFlags: unsigned
{
    INS_FLAGS_NOT_SET = 0x00,
    INS_FLAGS_SET = 0x01,
    INS_FLAGS_DONT_CARE = 0x02,
};
#else
#error Unsupported target architecture
#endif

#if defined(_TARGET_ARM_)
enum insOpts: unsigned
{
    INS_OPTS_NONE,
    INS_OPTS_LDST_PRE_DEC,
    INS_OPTS_LDST_POST_INC,

    INS_OPTS_RRX,
    INS_OPTS_LSL,
    INS_OPTS_LSR,
    INS_OPTS_ASR,
    INS_OPTS_ROR
};
#elif defined(_TARGET_ARM64_)
enum insOpts : unsigned
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
};

enum insCond : unsigned
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
};

enum insCflags : unsigned
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
};

enum insBarrier : unsigned
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
};
#endif

#undef EA_UNKNOWN
enum emitAttr : unsigned
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
};

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

enum InstructionSet
{
    InstructionSet_ILLEGAL = 0,
#ifdef _TARGET_XARCH_
    InstructionSet_Base,
    // Start linear order SIMD instruction sets
    // These ISAs have strictly generation to generation order.
    InstructionSet_SSE,
    InstructionSet_SSE2,
    InstructionSet_SSE3,
    InstructionSet_SSSE3,
    InstructionSet_SSE41,
    InstructionSet_SSE42,
    InstructionSet_AVX,
    InstructionSet_AVX2,
    // End linear order SIMD instruction sets.
    InstructionSet_AES,
    InstructionSet_BMI1,
    InstructionSet_BMI2,
    InstructionSet_FMA,
    InstructionSet_LZCNT,
    InstructionSet_PCLMULQDQ,
    InstructionSet_POPCNT,
    InstructionSet_BMI1_X64,
    InstructionSet_BMI2_X64,
    InstructionSet_LZCNT_X64,
    InstructionSet_POPCNT_X64,
    InstructionSet_SSE_X64,
    InstructionSet_SSE2_X64,
    InstructionSet_SSE41_X64,
    InstructionSet_SSE42_X64,
#elif defined(_TARGET_ARM_)
    InstructionSet_NEON,
#elif defined(_TARGET_ARM64_)
    InstructionSet_Base,      // Base instructions available on all Arm64 platforms
    InstructionSet_Aes,       // ID_AA64ISAR0_EL1.AES is 1 or better
    InstructionSet_Atomics,   // ID_AA64ISAR0_EL1.Atomic is 2 or better
    InstructionSet_Crc32,     // ID_AA64ISAR0_EL1.CRC32 is 1 or better
    InstructionSet_Dcpop,     // ID_AA64ISAR1_EL1.DPB is 1 or better
    InstructionSet_Dp,        // ID_AA64ISAR0_EL1.DP is 1 or better
    InstructionSet_Fcma,      // ID_AA64ISAR1_EL1.FCMA is 1 or better
    InstructionSet_Fp,        // ID_AA64PFR0_EL1.FP is 0 or better
    InstructionSet_Fp16,      // ID_AA64PFR0_EL1.FP is 1 or better
    InstructionSet_Jscvt,     // ID_AA64ISAR1_EL1.JSCVT is 1 or better
    InstructionSet_Lrcpc,     // ID_AA64ISAR1_EL1.LRCPC is 1 or better
    InstructionSet_Pmull,     // ID_AA64ISAR0_EL1.AES is 2 or better
    InstructionSet_Sha1,      // ID_AA64ISAR0_EL1.SHA1 is 1 or better
    InstructionSet_Sha256,    // ID_AA64ISAR0_EL1.SHA2 is 1 or better
    InstructionSet_Sha512,    // ID_AA64ISAR0_EL1.SHA2 is 2 or better
    InstructionSet_Sha3,      // ID_AA64ISAR0_EL1.SHA3 is 1 or better
    InstructionSet_Simd,      // ID_AA64PFR0_EL1.AdvSIMD is 0 or better
    InstructionSet_Simd_v81,  // ID_AA64ISAR0_EL1.RDM is 1 or better
    InstructionSet_Simd_fp16, // ID_AA64PFR0_EL1.AdvSIMD is 1 or better
    InstructionSet_Sm3,       // ID_AA64ISAR0_EL1.SM3 is 1 or better
    InstructionSet_Sm4,       // ID_AA64ISAR0_EL1.SM4 is 1 or better
    InstructionSet_Sve,       // ID_AA64PFR0_EL1.SVE is 1 or better
#endif
    InstructionSet_NONE       // No instruction set is available indicating an invalid value
};
// clang-format on

/*****************************************************************************/
#endif //_INSTR_H_
/*****************************************************************************/
