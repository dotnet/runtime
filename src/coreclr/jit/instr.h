// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************/

#ifndef _INSTR_H_
#define _INSTR_H_
/*****************************************************************************/

#ifdef TARGET_LOONGARCH64
#define BAD_CODE 0XFFFFFFFF
#elif TARGET_RISCV64
#define BAD_CODE 0X00000000
#else
#define BAD_CODE 0x0BADC0DE // better not match a real encoding!
#endif

/*****************************************************************************/

// clang-format off
enum instruction : uint32_t
{
#if defined(TARGET_XARCH)
    #define INST0(id, nm, um, mr,                 tt, flags) INS_##id,
    #define INST1(id, nm, um, mr,                 tt, flags) INS_##id,
    #define INST2(id, nm, um, mr, mi,             tt, flags) INS_##id,
    #define INST3(id, nm, um, mr, mi, rm,         tt, flags) INS_##id,
    #define INST4(id, nm, um, mr, mi, rm, a4,     tt, flags) INS_##id,
    #define INST5(id, nm, um, mr, mi, rm, a4, rr, tt, flags) INS_##id,
    #include "instrs.h"

#elif defined(TARGET_ARM)
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

#elif defined(TARGET_ARM64)
    #define INST1(id, nm, ldst, fmt, e1                                ) INS_##id,
    #define INST2(id, nm, ldst, fmt, e1, e2                            ) INS_##id,
    #define INST3(id, nm, ldst, fmt, e1, e2, e3                        ) INS_##id,
    #define INST4(id, nm, ldst, fmt, e1, e2, e3, e4                    ) INS_##id,
    #define INST5(id, nm, ldst, fmt, e1, e2, e3, e4, e5                ) INS_##id,
    #define INST6(id, nm, ldst, fmt, e1, e2, e3, e4, e5, e6            ) INS_##id,
    #define INST9(id, nm, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) INS_##id,
    #include "instrs.h"

    #define INST1(id, nm, info, fmt, e1                                                     ) INS_sve_##id,
    #define INST2(id, nm, info, fmt, e1, e2                                                 ) INS_sve_##id,
    #define INST3(id, nm, info, fmt, e1, e2, e3                                             ) INS_sve_##id,
    #define INST4(id, nm, info, fmt, e1, e2, e3, e4                                         ) INS_sve_##id,
    #define INST5(id, nm, info, fmt, e1, e2, e3, e4, e5                                     ) INS_sve_##id,
    #define INST6(id, nm, info, fmt, e1, e2, e3, e4, e5, e6                                 ) INS_sve_##id,
    #define INST7(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7                             ) INS_sve_##id,
    #define INST8(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8                         ) INS_sve_##id,
    #define INST9(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9                     ) INS_sve_##id,
    #define INST11(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10,e11           ) INS_sve_##id,
    #define INST13(id, nm, info, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13) INS_sve_##id,
    #include "instrsarm64sve.h"

    INS_lea,   // Not a real instruction. It is used for load the address of stack locals

#elif defined(TARGET_LOONGARCH64)
    #define INST(id, nm, ldst, e1, msk, fmt) INS_##id,
    #include "instrs.h"

    INS_lea,   // Not a real instruction. It is used for load the address of stack locals
#elif defined(TARGET_RISCV64)
    #define INST(id, nm, ldst, e1) INS_##id,
    #include "instrs.h"

    INS_lea,   // Not a real instruction. It is used for load the address of stack locals
#else
#error Unsupported target architecture
#endif

    INS_none,
    INS_count = INS_none
};

//------------------------------------------------------------------------
// IsAvx512OrPriorInstruction: Is this an Avx512 or Avx or Sse or K (opmask) instruction.
// Technically, K instructions would be considered under the VEX encoding umbrella, but due to
// the instruction table encoding had to be pulled out with the rest of the `INST5` definitions.
//
// Arguments:
//    ins - The instruction to check.
//
// Returns:
//    `true` if it is a sse or avx or avx512 instruction.
//
inline bool IsAvx512OrPriorInstruction(instruction ins)
{
#if defined(TARGET_XARCH)
    return (ins >= INS_FIRST_SSE_INSTRUCTION) && (ins <= INS_LAST_AVX512_INSTRUCTION);
#else
    return false;
#endif // TARGET_XARCH
}

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

#if defined(TARGET_XARCH)

enum insFlags : uint64_t
{
    INS_FLAGS_None = 0ULL,

    // Reads
    Reads_OF = 1ULL << 0,
    Reads_SF = 1ULL << 1,
    Reads_ZF = 1ULL << 2,
    Reads_PF = 1ULL << 3,
    Reads_CF = 1ULL << 4,
    Reads_DF = 1ULL << 5,

    // Writes
    Writes_OF = 1ULL << 6,
    Writes_SF = 1ULL << 7,
    Writes_ZF = 1ULL << 8,
    Writes_AF = 1ULL << 9,
    Writes_PF = 1ULL << 10,
    Writes_CF = 1ULL << 11,

    // Resets
    Resets_OF = 1ULL << 12,
    Resets_SF = 1ULL << 13,
    Resets_ZF = 1ULL << 14,
    Resets_AF = 1ULL << 15,
    Resets_PF = 1ULL << 16,
    Resets_CF = 1ULL << 17,

    // Undefined
    Undefined_OF = 1ULL << 18,
    Undefined_SF = 1ULL << 19,
    Undefined_ZF = 1ULL << 20,
    Undefined_AF = 1ULL << 21,
    Undefined_PF = 1ULL << 22,
    Undefined_CF = 1ULL << 23,

    // Restore
    Restore_SF_ZF_AF_PF_CF = 1ULL << 24,

    // x87 instruction
    INS_FLAGS_x87Instr = 1ULL << 25,

    // Avx
    INS_Flags_IsDstDstSrcAVXInstruction = 1ULL << 26,
    INS_Flags_IsDstSrcSrcAVXInstruction = 1ULL << 27,
    INS_Flags_Is3OperandInstructionMask = (INS_Flags_IsDstDstSrcAVXInstruction | INS_Flags_IsDstSrcSrcAVXInstruction),

    // w and s bits
    INS_FLAGS_Has_Wbit = 1ULL << 29,
    INS_FLAGS_Has_Sbit = 1ULL << 30,

    // instruction input size
    // if not input size is set, instruction defaults to using
    // the emitAttr for size
    Input_8Bit  = 1ULL << 31,
    Input_16Bit = 1ULL << 32,
    Input_32Bit = 1ULL << 33,
    Input_64Bit = 1ULL << 34,
    Input_Mask = (0xFULL) << 31,

    // encoding of the REX.W-bit
    REX_W0  = 1ULL << 35,
    REX_W1  = 1ULL << 36,
    REX_WX  = 1ULL << 37,

    // encoding of the REX.W-bit is considered for EVEX only and W0 or WIG otherwise
    REX_W0_EVEX = REX_W0,
    REX_W1_EVEX = 1ULL << 38,

    // encoding of the REX.W-bit is ignored
    REX_WIG     = REX_W0,

    // whether VEX or EVEX encodings are directly supported
    Encoding_VEX   = 1ULL << 39,
    Encoding_EVEX  = 1ULL << 40,

    KInstruction = 1ULL << 41,

    // EVEX feature: embedded broadcast
    INS_Flags_EmbeddedBroadcastSupported = 1ULL << 42,

    //  TODO-Cleanup:  Remove this flag and its usage from TARGET_XARCH
    INS_FLAGS_DONT_CARE = 0x00ULL,
};

enum insOpts: unsigned
{
    INS_OPTS_NONE = 0,

    // Two-bits: 0b0000_0011
    INS_OPTS_EVEX_b_MASK = 0x03,         // mask for EVEX.b related features.

    INS_OPTS_EVEX_eb_er_rd = 1,     // Embedded Broadcast or Round down

    INS_OPTS_EVEX_er_ru = 2,        // Round up

    INS_OPTS_EVEX_er_rz = 3,        // Round towards zero

    // Two-bits: 0b0001_1100
    INS_OPTS_EVEX_aaa_MASK = 0x1C,  // mask for EVEX.aaa related features

    INS_OPTS_EVEX_em_k1 = 1 << 2,   // Embedded mask uses K1

    INS_OPTS_EVEX_em_k2 = 2 << 2,   // Embedded mask uses K2

    INS_OPTS_EVEX_em_k3 = 3 << 2,   // Embedded mask uses K3

    INS_OPTS_EVEX_em_k4 = 4 << 2,   // Embedded mask uses K4

    INS_OPTS_EVEX_em_k5 = 5 << 2,   // Embedded mask uses K5

    INS_OPTS_EVEX_em_k6 = 6 << 2,   // Embedded mask uses K6

    INS_OPTS_EVEX_em_k7 = 7 << 2,   // Embedded mask uses K7

    // One-bit:  0b0010_0000
    INS_OPTS_EVEX_z_MASK = 0x20,    // mask for EVEX.z related features

    INS_OPTS_EVEX_em_zero,          // Embedded mask merges with zero
};

#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
// TODO-Cleanup: Move 'insFlags' under TARGET_ARM
enum insFlags: unsigned
{
    INS_FLAGS_NOT_SET = 0x00,
    INS_FLAGS_SET = 0x01,
    INS_FLAGS_DONT_CARE = 0x02,
};
#else
#error Unsupported target architecture
#endif

#if defined(TARGET_ARM)
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
enum insBarrier : unsigned
{
    INS_BARRIER_SY = 15
};
#elif defined(TARGET_ARM64)
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

    INS_OPTS_SCALABLE_B,
    INS_OPTS_SCALABLE_H,
    INS_OPTS_SCALABLE_S,
    INS_OPTS_SCALABLE_D,
    INS_OPTS_SCALABLE_Q,

    INS_OPTS_SCALABLE_S_UXTW,
    INS_OPTS_SCALABLE_S_SXTW,
    INS_OPTS_SCALABLE_D_UXTW,
    INS_OPTS_SCALABLE_D_SXTW,

    INS_OPTS_MSL,         // Vector Immediate (shifting ones variant)

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

#if FEATURE_LOOP_ALIGN
    INS_OPTS_ALIGN        // Align instruction
#endif
};

// When a single instruction has different encodings variants, this is used
// to distinguish those that can't be determined solely by register usage.
enum insScalableOpts : unsigned
{
    INS_SCALABLE_OPTS_NONE,                // No Variants exist

    INS_SCALABLE_OPTS_WIDE,                // Variants with wide elements (eg asr)
    INS_SCALABLE_OPTS_WITH_SIMD_SCALAR,    // Variants with a NEON SIMD register (eg clasta)
    INS_SCALABLE_OPTS_PREDICATE_MERGE,     // Variants with a Pg/M predicate (eg brka)
    INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR, // Variants with {<Pd1>.<T>, <Pd2>.<T>} predicate pair (eg whilege)
    INS_SCALABLE_OPTS_VL_2X,               // Variants with a vector length specifier of 2x (eg whilege)
    INS_SCALABLE_OPTS_VL_4X,               // Variants with a vector length specifier of 4x (eg whilege)
    INS_SCALABLE_OPTS_SHIFT,               // Variants with an optional shift operation (eg dup)

    INS_SCALABLE_OPTS_LSL_N,               // Variants with a LSL #N (eg {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #2])
    INS_SCALABLE_OPTS_MOD_N,               // Variants with a <mod> #N (eg {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #2])

    INS_SCALABLE_OPTS_WITH_VECTOR_PAIR,    // Variants with {<Zn1>.<T>, <Zn2>.<T>} sve register pair (eg splice)

    // Removable once REG_V0 and REG_P0 are distinct
    INS_SCALABLE_OPTS_UNPREDICATED,      // Variants without a predicate (eg add)
    INS_SCALABLE_OPTS_UNPREDICATED_WIDE, // Variants without a predicate and wide elements (eg asr)
    INS_SCALABLE_OPTS_TO_PREDICATE,     // Variants moving to a predicate from a vector (e.g. pmov)
    INS_SCALABLE_OPTS_TO_VECTOR         // Variants moving to a vector from a predicate (e.g. pmov)
};

// Maps directly to the pattern used in SVE instructions such as cntb.
enum insSvePattern : unsigned
{
    SVE_PATTERN_POW2 = 0,   // The largest power of 2.
    SVE_PATTERN_VL1 = 1,    // 1 element.
    SVE_PATTERN_VL2 = 2,    // 2 elements.
    SVE_PATTERN_VL3 = 3,    // 3 elements.
    SVE_PATTERN_VL4 = 4,    // 4 elements.
    SVE_PATTERN_VL5 = 5,    // 5 elements.
    SVE_PATTERN_VL6 = 6,    // 6 elements.
    SVE_PATTERN_VL7 = 7,    // 7 elements.
    SVE_PATTERN_VL8 = 8,    // 8 elements.
    SVE_PATTERN_VL16 = 9,   // 16 elements.
    SVE_PATTERN_VL32 = 10,  // 32 elements.
    SVE_PATTERN_VL64 = 11,  // 64 elements.
    SVE_PATTERN_VL128 = 12, // 128 elements.
    SVE_PATTERN_VL256 = 13, // 256 elements.
    SVE_PATTERN_MUL4 = 29,  // The largest multiple of 3.
    SVE_PATTERN_MUL3 = 30,  // The largest multiple of 4.
    SVE_PATTERN_ALL = 31    // All available (implicitly a multiple of two).
};

// Prefetch operation specifier for SVE instructions such as prfb.
enum insSvePrfop : unsigned
{
    SVE_PRFOP_PLDL1KEEP = 0b0000,
    SVE_PRFOP_PLDL1STRM = 0b0001, 
    SVE_PRFOP_PLDL2KEEP = 0b0010, 
    SVE_PRFOP_PLDL2STRM = 0b0011, 
    SVE_PRFOP_PLDL3KEEP = 0b0100, 
    SVE_PRFOP_PLDL3STRM = 0b0101, 
    SVE_PRFOP_PSTL1KEEP = 0b1000, 
    SVE_PRFOP_PSTL1STRM = 0b1001, 
    SVE_PRFOP_PSTL2KEEP = 0b1010, 
    SVE_PRFOP_PSTL2STRM = 0b1011, 
    SVE_PRFOP_PSTL3KEEP = 0b1100, 
    SVE_PRFOP_PSTL3STRM = 0b1101,

    SVE_PRFOP_CONST6    = 0b0110,
    SVE_PRFOP_CONST7    = 0b0111,
    SVE_PRFOP_CONST14   = 0b1110,
    SVE_PRFOP_CONST15   = 0b1111
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
#elif defined(TARGET_LOONGARCH64)
enum insOpts : unsigned
{
    INS_OPTS_NONE,

    INS_OPTS_RC,     // see ::emitIns_R_C().
    INS_OPTS_RL,     // see ::emitIns_R_L().
    INS_OPTS_JIRL,   // see ::emitIns_J_R().
    INS_OPTS_J,      // see ::emitIns_J().
    INS_OPTS_J_cond, // see ::emitIns_J_cond_la().
    INS_OPTS_I,      // see ::emitIns_I_la().
    INS_OPTS_C,      // see ::emitIns_Call().
    INS_OPTS_RELOC,  // see ::emitIns_R_AI().
};

enum insBarrier : unsigned
{
    // TODO-LOONGARCH64-CQ: ALL there are the same value right now.
    // These are reserved for future extension.
    // Because the LoongArch64 doesn't support these right now.
    INS_BARRIER_FULL  =  0,
    INS_BARRIER_WMB   =  INS_BARRIER_FULL,//4,
    INS_BARRIER_MB    =  INS_BARRIER_FULL,//16,
    INS_BARRIER_ACQ   =  INS_BARRIER_FULL,//17,
    INS_BARRIER_REL   =  INS_BARRIER_FULL,//18,
    INS_BARRIER_RMB   =  INS_BARRIER_FULL,//19,
};
#elif defined(TARGET_RISCV64)
enum insOpts : unsigned
{
    INS_OPTS_NONE,

    INS_OPTS_RC,     // see ::emitIns_R_C().
    INS_OPTS_RL,     // see ::emitIns_R_L().
    INS_OPTS_JALR,   // see ::emitIns_J_R().
    INS_OPTS_J,      // see ::emitIns_J().
    INS_OPTS_J_cond, // see ::emitIns_J_cond_la().
    INS_OPTS_I,      // see ::emitLoadImmediate().
    INS_OPTS_C,      // see ::emitIns_Call().
    INS_OPTS_RELOC,  // see ::emitIns_R_AI().
};

enum insBarrier : unsigned
{
    INS_BARRIER_FULL  =  0x33,
};

#endif

#if defined(TARGET_XARCH)
// Represents tupletype attribute of instruction.
// This is used in determining factor N while calculating compressed displacement in EVEX encoding
// Reference: Section 2.6.5 in Intel 64 and ia-32 architectures software developer's manual volume 2.
enum insTupleType : uint16_t
{
    INS_TT_NONE             = 0x0000,
    INS_TT_FULL             = 0x0001,
    INS_TT_HALF             = 0x0002,
    INS_TT_IS_BROADCAST     = static_cast<uint16_t>(INS_TT_FULL | INS_TT_HALF),
    INS_TT_FULL_MEM         = 0x0010,
    INS_TT_TUPLE1_SCALAR    = 0x0020,
    INS_TT_TUPLE1_FIXED     = 0x0040,
    INS_TT_TUPLE2           = 0x0080,
    INS_TT_TUPLE4           = 0x0100,
    INS_TT_TUPLE8           = 0x0200,
    INS_TT_HALF_MEM         = 0x0400,
    INS_TT_QUARTER_MEM      = 0x0800,
    INS_TT_EIGHTH_MEM       = 0x1000,
    INS_TT_MEM128           = 0x2000,
    INS_TT_MOVDDUP          = 0x4000,
    INS_TT_IS_NON_BROADCAST = static_cast<uint16_t>(~INS_TT_IS_BROADCAST),
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
#if defined(TARGET_ARM64)
                EA_SCALABLE      = 0x020,
                EA_SIZE_MASK     = 0x03F,
#elif defined(TARGET_XARCH)
                EA_32BYTE        = 0x020,
                EA_64BYTE        = 0x040,
                EA_SIZE_MASK     = 0x07F,
#else
                EA_SIZE_MASK     = 0x01F,
#endif

#ifdef TARGET_64BIT
                EA_PTRSIZE       = EA_8BYTE,
#else
                EA_PTRSIZE       = EA_4BYTE,
#endif

                EA_OFFSET_FLG    = 0x080,
                EA_OFFSET        = EA_OFFSET_FLG | EA_PTRSIZE,       /* size ==  0 */
                EA_GCREF_FLG     = 0x100,
                EA_GCREF         = EA_GCREF_FLG |  EA_PTRSIZE,       /* size == -1 */
                EA_BYREF_FLG     = 0x200,
                EA_BYREF         = EA_BYREF_FLG |  EA_PTRSIZE,       /* size == -2 */
                EA_DSP_RELOC_FLG = 0x400, // Is the displacement of the instruction relocatable?
                EA_CNS_RELOC_FLG = 0x800, // Is the immediate of the instruction relocatable?
                EA_CNS_SEC_RELOC = 0x1000, // Is the offset immediate that should be relocatable
                EA_CNS_TLSGD_RELOC = 0x2000, // Is the tlsgd constant to pass to tls_get_addr(). Only on linux/x64/NativeAot
};

#define EA_ATTR(x)                  ((emitAttr)(x))
#define EA_SIZE(x)                  ((emitAttr)(((unsigned)(x)) &  EA_SIZE_MASK))
#define EA_SIZE_IN_BYTES(x)         ((UNATIVE_OFFSET)(EA_SIZE(x)))
#define EA_SET_FLG(x, flg)          ((emitAttr)(((unsigned)(x)) | (flg)))
#define EA_REMOVE_FLG(x, flg)       ((emitAttr)(((unsigned)(x)) & ~(flg)))
#define EA_4BYTE_DSP_RELOC          (EA_SET_FLG(EA_4BYTE, EA_DSP_RELOC_FLG))
#define EA_PTR_DSP_RELOC            (EA_SET_FLG(EA_PTRSIZE, EA_DSP_RELOC_FLG))
#define EA_HANDLE_CNS_RELOC         (EA_SET_FLG(EA_PTRSIZE, EA_CNS_RELOC_FLG))
#define EA_IS_OFFSET(x)             ((((unsigned)(x)) & ((unsigned)EA_OFFSET_FLG)) != 0)
#define EA_IS_GCREF(x)              ((((unsigned)(x)) & ((unsigned)EA_GCREF_FLG)) != 0)
#define EA_IS_BYREF(x)              ((((unsigned)(x)) & ((unsigned)EA_BYREF_FLG)) != 0)
#define EA_IS_GCREF_OR_BYREF(x)     ((((unsigned)(x)) & ((unsigned)(EA_BYREF_FLG | EA_GCREF_FLG))) != 0)
#define EA_IS_DSP_RELOC(x)          ((((unsigned)(x)) & ((unsigned)EA_DSP_RELOC_FLG)) != 0)
#define EA_IS_CNS_RELOC(x)          ((((unsigned)(x)) & ((unsigned)EA_CNS_RELOC_FLG)) != 0)
#define EA_IS_CNS_SEC_RELOC(x)      ((((unsigned)(x)) & ((unsigned)EA_CNS_SEC_RELOC)) != 0)
#define EA_IS_CNS_TLSGD_RELOC(x)      ((((unsigned)(x)) & ((unsigned)EA_CNS_TLSGD_RELOC)) != 0)
#define EA_IS_RELOC(x)              (EA_IS_DSP_RELOC(x) || EA_IS_CNS_RELOC(x))
#define EA_TYPE(x)                  ((emitAttr)(((unsigned)(x)) & ~(EA_OFFSET_FLG | EA_DSP_RELOC_FLG | EA_CNS_RELOC_FLG)))

// clang-format on

/*****************************************************************************/
#endif //_INSTR_H_
/*****************************************************************************/
