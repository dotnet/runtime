// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//  This file was previously known as instrs.h
//
/*****************************************************************************
 *  x86 instructions for  the JIT compiler
 *
 *          id      -- the enum name for the instruction
 *          nm      -- textual name (for assembly dipslay)
 *          um      -- update mode, see IUM_xx enum (rd, wr, or rw)
 *          mr      -- base encoding for R/M[reg] addressing mode
 *          mi      -- base encoding for R/M,icon addressing mode
 *          rm      -- base encoding for reg,R/M  addressing mode
 *          a4      -- base encoding for eax,i32  addressing mode
 *          rr      -- base encoding for register addressing mode
 *          flags   -- flags, see INS_FLAGS_* enum
 *
******************************************************************************/

// clang-format off
#if !defined(TARGET_XARCH)
  #error Unexpected target type
#endif

#ifndef INST1
#error  At least INST1 must be defined before including this file.
#endif
/*****************************************************************************/
#ifndef INST0
#define INST0(id, nm, um, mr,                 flags)
#endif
#ifndef INST2
#define INST2(id, nm, um, mr, mi,             flags)
#endif
#ifndef INST3
#define INST3(id, nm, um, mr, mi, rm,         flags)
#endif
#ifndef INST4
#define INST4(id, nm, um, mr, mi, rm, a4,     flags)
#endif
#ifndef INST5
#define INST5(id, nm, um, mr, mi, rm, a4, rr, flags)
#endif

/*****************************************************************************/
/*               The following is x86-specific                               */
/*****************************************************************************/

//    id                nm                  um      mr            mi            rm            a4            rr           flags
INST5(invalid,          "INVALID",          IUM_RD, BAD_CODE,     BAD_CODE,     BAD_CODE,     BAD_CODE,     BAD_CODE,    INS_FLAGS_None)

INST5(push,             "push",             IUM_RD, 0x0030FE,     0x000068,     BAD_CODE,     BAD_CODE,     0x000050,    INS_FLAGS_None )
INST5(pop,              "pop",              IUM_WR, 0x00008E,     BAD_CODE,     BAD_CODE,     BAD_CODE,     0x000058,    INS_FLAGS_None )
// Does not affect the stack tracking in the emitter
INST5(push_hide,        "push",             IUM_RD, 0x0030FE,     0x000068,     BAD_CODE,     BAD_CODE,     0x000050,    INS_FLAGS_None )
INST5(pop_hide,         "pop",              IUM_WR, 0x00008E,     BAD_CODE,     BAD_CODE,     BAD_CODE,     0x000058,    INS_FLAGS_None )

INST5(inc,              "inc",              IUM_RW, 0x0000FE,     BAD_CODE,     BAD_CODE,     BAD_CODE,     0x000040,    Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF )
INST5(inc_l,            "inc",              IUM_RW, 0x0000FE,     BAD_CODE,     BAD_CODE,     BAD_CODE,     0x00C0FE,    Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF )
INST5(dec,              "dec",              IUM_RW, 0x0008FE,     BAD_CODE,     BAD_CODE,     BAD_CODE,     0x000048,    Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF )
INST5(dec_l,            "dec",              IUM_RW, 0x0008FE,     BAD_CODE,     BAD_CODE,     BAD_CODE,     0x00C8FE,    Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF )

// Multi-byte opcodes without modrm are represented in mixed endian fashion.
// See comment around quarter way through this file for more information.
INST5(bswap,            "bswap",            IUM_RW, 0x0F00C8,     BAD_CODE,     BAD_CODE,     BAD_CODE,     0x00C80F,    INS_FLAGS_None )

//    id                nm                  um      mr            mi            rm            a4                         flags
INST4(add,              "add",              IUM_RW, 0x000000,     0x000080,     0x000002,     0x000004,                  Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF     )
INST4(or,               "or",               IUM_RW, 0x000008,     0x000880,     0x00000A,     0x00000C,                  Resets_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Resets_CF     )
INST4(adc,              "adc",              IUM_RW, 0x000010,     0x001080,     0x000012,     0x000014,                  Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF
                                                                                                                                                                                                        | Reads_CF      )
INST4(sbb,              "sbb",              IUM_RW, 0x000018,     0x001880,     0x00001A,     0x00001C,                  Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF
                                                                                                                                                                                                        | Reads_CF      )
INST4(and,              "and",              IUM_RW, 0x000020,     0x002080,     0x000022,     0x000024,                  Resets_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Resets_CF     )
INST4(sub,              "sub",              IUM_RW, 0x000028,     0x002880,     0x00002A,     0x00002C,                  Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF     )
INST4(xor,              "xor",              IUM_RW, 0x000030,     0x003080,     0x000032,     0x000034,                  Resets_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Resets_CF     )
INST4(cmp,              "cmp",              IUM_RD, 0x000038,     0x003880,     0x00003A,     0x00003C,                  Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF     )
INST4(test,             "test",             IUM_RD, 0x000084,     0x0000F6,     0x000084,     0x0000A8,                  Resets_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Resets_CF     )
INST4(mov,              "mov",              IUM_WR, 0x000088,     0x0000C6,     0x00008A,     0x0000B0,                  INS_FLAGS_None )
INST4(mov_eliminated,   "mov_eliminated",   IUM_WR, 0x000088,     0x0000C6,     0x00008A,     0x0000B0,                  INS_FLAGS_None)

INST4(lea,              "lea",              IUM_WR, BAD_CODE,     BAD_CODE,     0x00008D,     BAD_CODE,                  INS_FLAGS_None )

//    id                nm                  um      mr            mi            rm                                       flags

// Note that emitter has only partial support for BT. It can only emit the reg,reg form
// and the registers need to be reversed to get the correct encoding.
INST3(bt,               "bt",               IUM_RD, 0x0F00A3,     BAD_CODE,     0x0F00A3,                                Undefined_OF   | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )

INST3(bsf,              "bsf",              IUM_WR, BAD_CODE,     BAD_CODE,     0x0F00BC,                                Undefined_OF   | Undefined_SF  | Writes_ZF     | Undefined_AF  | Undefined_PF  | Undefined_CF  )
INST3(bsr,              "bsr",              IUM_WR, BAD_CODE,     BAD_CODE,     0x0F00BD,                                Undefined_OF   | Undefined_SF  | Writes_ZF     | Undefined_AF  | Undefined_PF  | Undefined_CF  )

INST3(movsx,            "movsx",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F00BE,                                INS_FLAGS_None )
#ifdef TARGET_AMD64
INST3(movsxd,           "movsxd",           IUM_WR, BAD_CODE,     BAD_CODE,     0x4800000063,                            INS_FLAGS_None )
#endif
INST3(movzx,            "movzx",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F00B6,                                INS_FLAGS_None )

INST3(cmovo,            "cmovo",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0040,                                Reads_OF )
INST3(cmovno,           "cmovno",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0041,                                Reads_OF )
INST3(cmovb,            "cmovb",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0042,                                Reads_CF )
INST3(cmovae,           "cmovae",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0043,                                Reads_CF )
INST3(cmove,            "cmove",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0044,                                Reads_ZF )
INST3(cmovne,           "cmovne",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0045,                                Reads_ZF )
INST3(cmovbe,           "cmovbe",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0046,                                Reads_ZF | Reads_CF )
INST3(cmova,            "cmova",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0047,                                Reads_ZF | Reads_CF )
INST3(cmovs,            "cmovs",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0048,                                Reads_SF )
INST3(cmovns,           "cmovns",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F0049,                                Reads_SF )
INST3(cmovp,            "cmovp",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F004A,                                Reads_PF )
INST3(cmovnp,           "cmovnp",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F004B,                                Reads_PF )
INST3(cmovl,            "cmovl",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F004C,                                Reads_OF       | Reads_SF                                                                      )
INST3(cmovge,           "cmovge",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F004D,                                Reads_OF       | Reads_SF                                                                      )
INST3(cmovle,           "cmovle",           IUM_WR, BAD_CODE,     BAD_CODE,     0x0F004E,                                Reads_OF       | Reads_SF      | Reads_ZF                                                      )
INST3(cmovg,            "cmovg",            IUM_WR, BAD_CODE,     BAD_CODE,     0x0F004F,                                Reads_OF       | Reads_SF      | Reads_ZF                                                      )

INST3(xchg,             "xchg",             IUM_RW, 0x000086,     BAD_CODE,     0x000086,                                INS_FLAGS_None )
INST3(imul,             "imul",             IUM_RW, 0x0F00AC,     BAD_CODE,     0x0F00AF,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )

//    id                nm                  um      mr            mi            rm                                       flags

// Instead of encoding these as 3-operand instructions, we encode them
// as 2-operand instructions with the target register being implicit
// implicit_reg = op1*op2_icon
#define INSTMUL INST3
INSTMUL(imul_AX,        "imul",             IUM_RD, BAD_CODE,     0x000068,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_CX,        "imul",             IUM_RD, BAD_CODE,     0x000868,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_DX,        "imul",             IUM_RD, BAD_CODE,     0x001068,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_BX,        "imul",             IUM_RD, BAD_CODE,     0x001868,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_SP,        "imul",             IUM_RD, BAD_CODE,     BAD_CODE,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_BP,        "imul",             IUM_RD, BAD_CODE,     0x002868,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_SI,        "imul",             IUM_RD, BAD_CODE,     0x003068,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_DI,        "imul",             IUM_RD, BAD_CODE,     0x003868,     BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )

#ifdef TARGET_AMD64

INSTMUL(imul_08,        "imul",             IUM_RD, BAD_CODE,     0x4400000068, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_09,        "imul",             IUM_RD, BAD_CODE,     0x4400000868, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_10,        "imul",             IUM_RD, BAD_CODE,     0x4400001068, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_11,        "imul",             IUM_RD, BAD_CODE,     0x4400001868, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_12,        "imul",             IUM_RD, BAD_CODE,     0x4400002068, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_13,        "imul",             IUM_RD, BAD_CODE,     0x4400002868, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_14,        "imul",             IUM_RD, BAD_CODE,     0x4400003068, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INSTMUL(imul_15,        "imul",             IUM_RD, BAD_CODE,     0x4400003868, BAD_CODE,                                Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )

#endif // TARGET_AMD64

// the hex codes in this file represent the instruction encoding as follows:
// 0x0000ff00 - modrm byte position
// 0x000000ff - last byte of opcode (before modrm)
// 0x00ff0000 - first byte of opcode
// 0xff000000 - middle byte of opcode, if needed (after first, before last)
//
// So a 1-byte opcode is:      and with modrm:
//             0x00000011          0x0000RM11
//
// So a 2-byte opcode is:      and with modrm:
//             0x00002211          0x0011RM22
//
// So a 3-byte opcode is:      and with modrm:
//             0x00113322          0x2211RM33
//
// So a 4-byte opcode would be something like this:
//             0x22114433

#define PACK3(byte1,byte2,byte3) (((byte1) << 16) | ((byte2) << 24) | (byte3))
#define PACK2(byte1,byte2)                       (((byte1) << 16) | (byte2))
#define SSEFLT(c) PACK3(0xf3, 0x0f, c)
#define SSEDBL(c) PACK3(0xf2, 0x0f, c)
#define PCKDBL(c) PACK3(0x66, 0x0f, c)
#define PCKFLT(c) PACK2(0x0f,c)

// These macros encode extra byte that is implicit in the macro.
#define PACK4(byte1,byte2,byte3,byte4) (((byte1) << 16) | ((byte2) << 24) | (byte3) | ((byte4) << 8))
#define SSE38(c)   PACK4(0x66, 0x0f, 0x38, c)
#define SSE3A(c)   PACK4(0x66, 0x0f, 0x3A, c)

// VEX* encodes the implied leading opcode bytes in c1:
// 1: implied 0f, 2: implied 0f 38, 3: implied 0f 3a
#define VEX2INT(c1,c2)   PACK3(c1, 0xc5, c2)
#define VEX3INT(c1,c2)   PACK4(c1, 0xc5, 0x02, c2)
#define VEX3FLT(c1,c2)   PACK4(c1, 0xc5, 0x02, c2)

INST3(FIRST_SSE_INSTRUCTION, "FIRST_SSE_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)
// These are the SSE instructions used on x86
INST3(pmovmskb,         "pmovmskb",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xD7),                            INS_FLAGS_None)    // Move the MSB bits of all bytes in a xmm reg to an int reg
INST3(movmskpd,         "movmskpd",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x50),                            INS_FLAGS_None)    // Extract 2-bit sign mask from xmm and store in reg. The upper bits of r32 or r64 are filled with zeros.
INST3(movd,             "movd",             IUM_WR, PCKDBL(0x7E), BAD_CODE,     PCKDBL(0x6E),                            INS_FLAGS_None)    // Move Double/Quadword between mm regs <-> memory/r32/r64 regs, cleanup https://github.com/dotnet/runtime/issues/47943
INST3(movq,             "movq",             IUM_WR, PCKDBL(0xD6), BAD_CODE,     SSEFLT(0x7E),                            INS_FLAGS_None)    // Move Quadword between memory/mm <-> regs, cleanup https://github.com/dotnet/runtime/issues/47943
INST3(movsdsse2,        "movsd",            IUM_WR, SSEDBL(0x11), BAD_CODE,     SSEDBL(0x10),                            INS_Flags_IsDstSrcSrcAVXInstruction)

INST3(punpckldq,        "punpckldq",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x62),                            INS_Flags_IsDstDstSrcAVXInstruction)

INST3(xorps,            "xorps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x57),                            INS_Flags_IsDstDstSrcAVXInstruction)    // XOR packed singles

INST3(cvttsd2si,        "cvttsd2si",        IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x2C),                            INS_FLAGS_None)    // cvt with trunc scalar double to signed DWORDs

INST3(movntdq,          "movntdq",          IUM_WR, PCKDBL(0xE7), BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(movnti,           "movnti",           IUM_WR, PCKFLT(0xC3), BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(movntpd,          "movntpd",          IUM_WR, PCKDBL(0x2B), BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(movntps,          "movntps",          IUM_WR, PCKFLT(0x2B), BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(movdqu,           "movdqu",           IUM_WR, SSEFLT(0x7F), BAD_CODE,     SSEFLT(0x6F),                            INS_FLAGS_None)
INST3(movdqa,           "movdqa",           IUM_WR, PCKDBL(0x7F), BAD_CODE,     PCKDBL(0x6F),                            INS_FLAGS_None)
INST3(movlpd,           "movlpd",           IUM_WR, PCKDBL(0x13), BAD_CODE,     PCKDBL(0x12),                            INS_Flags_IsDstSrcSrcAVXInstruction)
INST3(movlps,           "movlps",           IUM_WR, PCKFLT(0x13), BAD_CODE,     PCKFLT(0x12),                            INS_Flags_IsDstSrcSrcAVXInstruction)
INST3(movhpd,           "movhpd",           IUM_WR, PCKDBL(0x17), BAD_CODE,     PCKDBL(0x16),                            INS_Flags_IsDstSrcSrcAVXInstruction)
INST3(movhps,           "movhps",           IUM_WR, PCKFLT(0x17), BAD_CODE,     PCKFLT(0x16),                            INS_Flags_IsDstSrcSrcAVXInstruction)
INST3(movss,            "movss",            IUM_WR, SSEFLT(0x11), BAD_CODE,     SSEFLT(0x10),                            INS_Flags_IsDstSrcSrcAVXInstruction)
INST3(movapd,           "movapd",           IUM_WR, PCKDBL(0x29), BAD_CODE,     PCKDBL(0x28),                            INS_FLAGS_None)
INST3(movaps,           "movaps",           IUM_WR, PCKFLT(0x29), BAD_CODE,     PCKFLT(0x28),                            INS_FLAGS_None)
INST3(movupd,           "movupd",           IUM_WR, PCKDBL(0x11), BAD_CODE,     PCKDBL(0x10),                            INS_FLAGS_None)
INST3(movups,           "movups",           IUM_WR, PCKFLT(0x11), BAD_CODE,     PCKFLT(0x10),                            INS_FLAGS_None)
INST3(movhlps,          "movhlps",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x12),                            INS_Flags_IsDstDstSrcAVXInstruction)
INST3(movlhps,          "movlhps",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x16),                            INS_Flags_IsDstDstSrcAVXInstruction)
INST3(movmskps,         "movmskps",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x50),                            INS_FLAGS_None)
INST3(unpckhps,         "unpckhps",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x15),                            INS_Flags_IsDstDstSrcAVXInstruction)
INST3(unpcklps,         "unpcklps",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x14),                            INS_Flags_IsDstDstSrcAVXInstruction)
INST3(maskmovdqu,       "maskmovdqu",       IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xF7),                            INS_FLAGS_None)

INST3(shufps,           "shufps",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0xC6),                            INS_Flags_IsDstDstSrcAVXInstruction)
INST3(shufpd,           "shufpd",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xC6),                            INS_Flags_IsDstDstSrcAVXInstruction)

INST3(punpckhdq,        "punpckhdq",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x6A),                            INS_Flags_IsDstDstSrcAVXInstruction)

INST3(lfence,           "lfence",           IUM_RD, 0x000FE8AE,   BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(mfence,           "mfence",           IUM_RD, 0x000FF0AE,   BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(prefetchnta,      "prefetchnta",      IUM_RD, 0x000F0018,   BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(prefetcht0,       "prefetcht0",       IUM_RD, 0x000F0818,   BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(prefetcht1,       "prefetcht1",       IUM_RD, 0x000F1018,   BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(prefetcht2,       "prefetcht2",       IUM_RD, 0x000F1818,   BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)
INST3(sfence,           "sfence",           IUM_RD, 0x000FF8AE,   BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)

// SSE 2 arith
INST3(addps,            "addps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x58),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed singles
INST3(addss,            "addss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x58),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add scalar singles
INST3(addpd,            "addpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x58),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed doubles
INST3(addsd,            "addsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x58),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add scalar doubles
INST3(mulps,            "mulps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x59),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply packed singles
INST3(mulss,            "mulss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x59),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply scalar single
INST3(mulpd,            "mulpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x59),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply packed doubles
INST3(mulsd,            "mulsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x59),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply scalar doubles
INST3(subps,            "subps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x5C),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed singles
INST3(subss,            "subss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x5C),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract scalar singles
INST3(subpd,            "subpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x5C),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed doubles
INST3(subsd,            "subsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x5C),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract scalar doubles
INST3(minps,            "minps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x5D),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Minimum packed singles
INST3(minss,            "minss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x5D),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Minimum scalar single
INST3(minpd,            "minpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x5D),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Minimum packed doubles
INST3(minsd,            "minsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x5D),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Minimum scalar double
INST3(divps,            "divps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x5E),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Divide packed singles
INST3(divss,            "divss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x5E),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Divide scalar singles
INST3(divpd,            "divpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x5E),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Divide packed doubles
INST3(divsd,            "divsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x5E),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Divide scalar doubles
INST3(maxps,            "maxps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x5F),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Maximum packed singles
INST3(maxss,            "maxss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x5F),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Maximum scalar single
INST3(maxpd,            "maxpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x5F),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Maximum packed doubles
INST3(maxsd,            "maxsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x5F),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Return Maximum scalar double
INST3(xorpd,            "xorpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x57),                            INS_Flags_IsDstDstSrcAVXInstruction)    // XOR packed doubles
INST3(andps,            "andps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x54),                            INS_Flags_IsDstDstSrcAVXInstruction)    // AND packed singles
INST3(andpd,            "andpd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x54),                            INS_Flags_IsDstDstSrcAVXInstruction)    // AND packed doubles
INST3(sqrtps,           "sqrtps",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x51),                            INS_FLAGS_None)    // Sqrt of packed singles
INST3(sqrtss,           "sqrtss",           IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x51),                            INS_Flags_IsDstSrcSrcAVXInstruction)    // Sqrt of scalar single
INST3(sqrtpd,           "sqrtpd",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x51),                            INS_FLAGS_None)    // Sqrt of packed doubles
INST3(sqrtsd,           "sqrtsd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x51),                            INS_Flags_IsDstSrcSrcAVXInstruction)    // Sqrt of scalar double
INST3(andnps,           "andnps",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x55),                            INS_Flags_IsDstDstSrcAVXInstruction)    // And-Not packed singles
INST3(andnpd,           "andnpd",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x55),                            INS_Flags_IsDstDstSrcAVXInstruction)    // And-Not packed doubles
INST3(orps,             "orps",             IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x56),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Or packed singles
INST3(orpd,             "orpd",             IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x56),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Or packed doubles
INST3(haddpd,           "haddpd",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x7C),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Horizontal add packed doubles
INST3(haddps,           "haddps",           IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x7C),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Horizontal add packed floats
INST3(hsubpd,           "hsubpd",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x7D),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Horizontal subtract packed doubles
INST3(hsubps,           "hsubps",           IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x7D),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Horizontal subtract packed floats
INST3(addsubps,         "addsubps",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0xD0),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add/Subtract packed singles
INST3(addsubpd,         "addsubpd",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xD0),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add/Subtract packed doubles

// SSE 2 approx arith
INST3(rcpps,            "rcpps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x53),                            INS_FLAGS_None)    // Reciprocal of packed singles
INST3(rcpss,            "rcpss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x53),                            INS_Flags_IsDstSrcSrcAVXInstruction)    // Reciprocal of scalar single
INST3(rsqrtps,          "rsqrtps",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x52),                            INS_FLAGS_None)    // Reciprocal Sqrt of packed singles
INST3(rsqrtss,          "rsqrtss",          IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x52),                            INS_Flags_IsDstSrcSrcAVXInstruction)    // Reciprocal Sqrt of scalar single

// SSE2 conversions
INST3(cvtpi2ps,         "cvtpi2ps",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x2A),                            INS_FLAGS_None)    // cvt packed DWORDs to singles
INST3(cvtsi2ss,         "cvtsi2ss",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x2A),                            INS_Flags_IsDstDstSrcAVXInstruction)    // cvt DWORD to scalar single
INST3(cvtpi2pd,         "cvtpi2pd",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x2A),                            INS_FLAGS_None)    // cvt packed DWORDs to doubles
INST3(cvtsi2sd,         "cvtsi2sd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x2A),                            INS_Flags_IsDstDstSrcAVXInstruction)    // cvt DWORD to scalar double
INST3(cvttps2pi,        "cvttps2pi",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x2C),                            INS_FLAGS_None)    // cvt with trunc packed singles to DWORDs
INST3(cvttss2si,        "cvttss2si",        IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x2C),                            INS_FLAGS_None)    // cvt with trunc scalar single to DWORD
INST3(cvttpd2pi,        "cvttpd2pi",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x2C),                            INS_FLAGS_None)    // cvt with trunc packed doubles to DWORDs
INST3(cvtps2pi,         "cvtps2pi",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x2D),                            INS_FLAGS_None)    // cvt packed singles to DWORDs
INST3(cvtss2si,         "cvtss2si",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x2D),                            INS_FLAGS_None)    // cvt scalar single to DWORD
INST3(cvtpd2pi,         "cvtpd2pi",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x2D),                            INS_FLAGS_None)    // cvt packed doubles to DWORDs
INST3(cvtsd2si,         "cvtsd2si",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x2D),                            INS_FLAGS_None)    // cvt scalar double to DWORD
INST3(cvtps2pd,         "cvtps2pd",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x5A),                            INS_FLAGS_None)    // cvt packed singles to doubles
INST3(cvtpd2ps,         "cvtpd2ps",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x5A),                            INS_FLAGS_None)    // cvt packed doubles to singles
INST3(cvtss2sd,         "cvtss2sd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x5A),                            INS_Flags_IsDstDstSrcAVXInstruction)    // cvt scalar single to scalar doubles
INST3(cvtsd2ss,         "cvtsd2ss",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x5A),                            INS_Flags_IsDstDstSrcAVXInstruction)    // cvt scalar double to scalar singles
INST3(cvtdq2ps,         "cvtdq2ps",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0x5B),                            INS_FLAGS_None)    // cvt packed DWORDs to singles
INST3(cvtps2dq,         "cvtps2dq",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x5B),                            INS_FLAGS_None)    // cvt packed singles to DWORDs
INST3(cvttps2dq,        "cvttps2dq",        IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x5B),                            INS_FLAGS_None)    // cvt with trunc packed singles to DWORDs
INST3(cvtpd2dq,         "cvtpd2dq",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0xE6),                            INS_FLAGS_None)    // cvt packed doubles to DWORDs
INST3(cvttpd2dq,        "cvttpd2dq",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xE6),                            INS_FLAGS_None)    // cvt with trunc packed doubles to DWORDs
INST3(cvtdq2pd,         "cvtdq2pd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0xE6),                            INS_FLAGS_None)    // cvt packed DWORDs to doubles

// SSE2 comparison instructions
INST3(comiss,           "comiss",           IUM_RD, BAD_CODE,     BAD_CODE,     PCKFLT(0x2F),                            Resets_OF  | Resets_SF     | Writes_ZF     | Resets_AF     | Writes_PF     | Writes_CF                     )  // ordered compare singles
INST3(comisd,           "comisd",           IUM_RD, BAD_CODE,     BAD_CODE,     PCKDBL(0x2F),                            Resets_OF  | Resets_SF     | Writes_ZF     | Resets_AF     | Writes_PF     | Writes_CF                     )  // ordered compare doubles
INST3(ucomiss,          "ucomiss",          IUM_RD, BAD_CODE,     BAD_CODE,     PCKFLT(0x2E),                            Resets_OF  | Resets_SF     | Writes_ZF     | Resets_AF     | Writes_PF     | Writes_CF                     )  // unordered compare singles
INST3(ucomisd,          "ucomisd",          IUM_RD, BAD_CODE,     BAD_CODE,     PCKDBL(0x2E),                            Resets_OF  | Resets_SF     | Writes_ZF     | Resets_AF     | Writes_PF     | Writes_CF                     )  // unordered compare doubles

// SSE2 packed single/double comparison operations.
// Note that these instructions not only compare but also overwrite the first source.
INST3(cmpps,            "cmpps",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKFLT(0xC2),                            INS_Flags_IsDstDstSrcAVXInstruction)    // compare packed singles
INST3(cmppd,            "cmppd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xC2),                            INS_Flags_IsDstDstSrcAVXInstruction)    // compare packed doubles
INST3(cmpss,            "cmpss",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0xC2),                            INS_Flags_IsDstDstSrcAVXInstruction)    // compare scalar singles
INST3(cmpsd,            "cmpsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0xC2),                            INS_Flags_IsDstDstSrcAVXInstruction)    // compare scalar doubles

//SSE2 packed integer operations
INST3(paddb,            "paddb",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xFC),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed byte integers
INST3(paddw,            "paddw",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xFD),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed word (16-bit) integers
INST3(paddd,            "paddd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xFE),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed double-word (32-bit) integers
INST3(paddq,            "paddq",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xD4),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed quad-word (64-bit) integers
INST3(paddsb,           "paddsb",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xEC),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed signed byte integers and saturate the results
INST3(paddsw,           "paddsw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xED),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed signed word integers and saturate the results
INST3(paddusb,          "paddusb",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xDC),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed unsigned byte integers and saturate the results
INST3(paddusw,          "paddusw",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xDD),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Add packed unsigned word integers and saturate the results
INST3(pavgb,            "pavgb",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xE0),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Average of packed byte integers
INST3(pavgw,            "pavgw",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xE3),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Average of packed word integers
INST3(psubb,            "psubb",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xF8),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed word (16-bit) integers
INST3(psubw,            "psubw",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xF9),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed word (16-bit) integers
INST3(psubd,            "psubd",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xFA),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed double-word (32-bit) integers
INST3(psubq,            "psubq",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xFB),                            INS_Flags_IsDstDstSrcAVXInstruction)    // subtract packed quad-word (64-bit) integers
INST3(pmaddwd,          "pmaddwd",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xF5),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply packed signed 16-bit integers in a and b, producing intermediate signed 32-bit integers. Horizontally add adjacent pairs of intermediate 32-bit integers, and pack the results in dst
INST3(pmulhw,           "pmulhw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xE5),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply high the packed 16-bit signed integers
INST3(pmulhuw,          "pmulhuw",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xE4),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply high the packed 16-bit unsigned integers
INST3(pmuludq,          "pmuludq",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xF4),                            INS_Flags_IsDstDstSrcAVXInstruction)    // packed multiply 32-bit unsigned integers and store 64-bit result
INST3(pmullw,           "pmullw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xD5),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed multiply 16 bit unsigned integers and store lower 16 bits of each result
INST3(pand,             "pand",             IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xDB),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed bit-wise AND of two xmm regs
INST3(pandn,            "pandn",            IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xDF),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed bit-wise AND NOT of two xmm regs
INST3(por,              "por",              IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xEB),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed bit-wise OR of two xmm regs
INST3(pxor,             "pxor",             IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xEF),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed bit-wise XOR of two xmm regs
INST3(psadbw,           "psadbw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xF6),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Compute the sum of absolute differences of packed unsigned 8-bit integers
INST3(psubsb,           "psubsb",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xE8),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed 8-bit integers in b from packed 8-bit integers in a using saturation
INST3(psubusb,          "psubusb",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xD8),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed unsigned 8-bit integers in b from packed unsigned 8-bit integers in a using saturation
INST3(psubsw,           "psubsw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xE9),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed 16-bit integers in b from packed 16-bit integers in a using saturation
INST3(psubusw,          "psubusw",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xD9),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Subtract packed unsigned 16-bit integers in b from packed unsigned 16-bit integers in a using saturation

// Note that the shift immediates share the same encoding between left and right-shift, and are distinguished by the Reg/Opcode,
// which is handled in emitxarch.cpp.
INST3(psrldq,           "psrldq",           IUM_WR, BAD_CODE,     PCKDBL(0x73), BAD_CODE,                                INS_Flags_IsDstDstSrcAVXInstruction)    // Shift right logical of xmm reg by given number of bytes
INST3(pslldq,           "pslldq",           IUM_WR, BAD_CODE,     PCKDBL(0x73), BAD_CODE,                                INS_Flags_IsDstDstSrcAVXInstruction)    // Shift left logical of xmm reg by given number of bytes
INST3(psllw,            "psllw",            IUM_WR, BAD_CODE,     PCKDBL(0x71), PCKDBL(0xF1),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift left logical of 16-bit integers
INST3(pslld,            "pslld",            IUM_WR, BAD_CODE,     PCKDBL(0x72), PCKDBL(0xF2),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift left logical of 32-bit integers
INST3(psllq,            "psllq",            IUM_WR, BAD_CODE,     PCKDBL(0x73), PCKDBL(0xF3),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift left logical of 64-bit integers
INST3(psrlw,            "psrlw",            IUM_WR, BAD_CODE,     PCKDBL(0x71), PCKDBL(0xD1),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift right logical of 16-bit integers
INST3(psrld,            "psrld",            IUM_WR, BAD_CODE,     PCKDBL(0x72), PCKDBL(0xD2),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift right logical of 32-bit integers
INST3(psrlq,            "psrlq",            IUM_WR, BAD_CODE,     PCKDBL(0x73), PCKDBL(0xD3),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift right logical of 64-bit integers
INST3(psraw,            "psraw",            IUM_WR, BAD_CODE,     PCKDBL(0x71), PCKDBL(0xE1),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift right arithmetic of 16-bit integers
INST3(psrad,            "psrad",            IUM_WR, BAD_CODE,     PCKDBL(0x72), PCKDBL(0xE2),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed shift right arithmetic of 32-bit integers

INST3(pmaxub,           "pmaxub",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xDE),                            INS_Flags_IsDstDstSrcAVXInstruction)    // packed maximum unsigned bytes
INST3(pminub,           "pminub",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xDA),                            INS_Flags_IsDstDstSrcAVXInstruction)    // packed minimum unsigned bytes
INST3(pmaxsw,           "pmaxsw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xEE),                            INS_Flags_IsDstDstSrcAVXInstruction)    // packed maximum signed words
INST3(pminsw,           "pminsw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xEA),                            INS_Flags_IsDstDstSrcAVXInstruction)    // packed minimum signed words
INST3(pcmpeqd,          "pcmpeqd",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x76),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 32-bit integers for equality
INST3(pcmpgtd,          "pcmpgtd",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x66),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 32-bit signed integers for greater than
INST3(pcmpeqw,          "pcmpeqw",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x75),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 16-bit integers for equality
INST3(pcmpgtw,          "pcmpgtw",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x65),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 16-bit signed integers for greater than
INST3(pcmpeqb,          "pcmpeqb",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x74),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 8-bit integers for equality
INST3(pcmpgtb,          "pcmpgtb",          IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x64),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 8-bit signed integers for greater than

INST3(pshufd,           "pshufd",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x70),                            INS_FLAGS_None)    // Packed shuffle of 32-bit integers
INST3(pshufhw,          "pshufhw",          IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x70),                            INS_FLAGS_None)    // Shuffle the high words in xmm2/m128 based on the encoding in imm8 and store the result in xmm1.
INST3(pshuflw,          "pshuflw",          IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x70),                            INS_FLAGS_None)    // Shuffle the low words in xmm2/m128 based on the encoding in imm8 and store the result in xmm1.
INST3(pextrw,           "pextrw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xC5),                            INS_FLAGS_None)    // Extract 16-bit value into a r32 with zero extended to 32-bits
INST3(pinsrw,           "pinsrw",           IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0xC4),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Insert word at index

INST3(punpckhbw,        "punpckhbw",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x68),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen ubyte to ushort (hi)
INST3(punpcklbw,        "punpcklbw",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x60),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen ubyte to ushort (lo)
INST3(punpckhqdq,       "punpckhqdq",       IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x6D),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen uint to ulong (hi)
INST3(punpcklqdq,       "punpcklqdq",       IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x6C),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen uint to ulong (lo)
INST3(punpckhwd,        "punpckhwd",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x69),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen ushort to uint (hi)
INST3(punpcklwd,        "punpcklwd",        IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x61),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen ushort to uint (lo)
INST3(unpckhpd,         "unpckhpd",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x15),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen ubyte to ushort (hi)
INST3(unpcklpd,         "unpcklpd",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x14),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Packed logical (unsigned) widen ubyte to ushort (hi)

INST3(packssdw,         "packssdw",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x6B),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Pack (narrow) int to short with saturation
INST3(packsswb,         "packsswb",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x63),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Pack (narrow) short to byte with saturation
INST3(packuswb,         "packuswb",         IUM_WR, BAD_CODE,     BAD_CODE,     PCKDBL(0x67),                            INS_Flags_IsDstDstSrcAVXInstruction)    // Pack (narrow) short to unsigned byte with saturation

//    id                nm                  um      mr            mi            rm                                       flags
INST3(dpps,             "dpps",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x40),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed dot product of two float vector regs
INST3(dppd,             "dppd",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x41),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed dot product of two double vector regs
INST3(insertps,         "insertps",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x21),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Insert packed single precision float value
INST3(pcmpeqq,          "pcmpeqq",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x29),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 64-bit integers for equality
INST3(pcmpgtq,          "pcmpgtq",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x37),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed compare 64-bit integers for equality
INST3(pmulld,           "pmulld",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x40),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed multiply 32 bit unsigned integers and store lower 32 bits of each result
INST3(ptest,            "ptest",            IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x17),                             INS_FLAGS_None)    // Packed logical compare
INST3(phaddd,           "phaddd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x02),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed horizontal add
INST3(pabsb,            "pabsb",            IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x1C),                             INS_FLAGS_None)    // Packed absolute value of bytes
INST3(pabsw,            "pabsw",            IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x1D),                             INS_FLAGS_None)    // Packed absolute value of 16-bit integers
INST3(pabsd,            "pabsd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x1E),                             INS_FLAGS_None)    // Packed absolute value of 32-bit integers
INST3(palignr,          "palignr",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x0F),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed Align Right
INST3(pmaddubsw,        "pmaddubsw",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x04),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply and Add Packed Signed and Unsigned Bytes
INST3(pmulhrsw,         "pmulhrsw",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x0B),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed Multiply High with Round and Scale
INST3(pshufb,           "pshufb",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x00),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed Shuffle Bytes
INST3(psignb,           "psignb",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x08),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed SIGN
INST3(psignw,           "psignw",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x09),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed SIGN
INST3(psignd,           "psignd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x0A),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed SIGN
INST3(pminsb,           "pminsb",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x38),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed minimum signed bytes
INST3(pminsd,           "pminsd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x39),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed minimum 32-bit signed integers
INST3(pminuw,           "pminuw",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x3A),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed minimum 16-bit unsigned integers
INST3(pminud,           "pminud",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x3B),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed minimum 32-bit unsigned integers
INST3(pmaxsb,           "pmaxsb",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x3C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed maximum signed bytes
INST3(pmaxsd,           "pmaxsd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x3D),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed maximum 32-bit signed integers
INST3(pmaxuw,           "pmaxuw",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x3E),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed maximum 16-bit unsigned integers
INST3(pmaxud,           "pmaxud",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x3F),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed maximum 32-bit unsigned integers
INST3(pmovsxbw,         "pmovsxbw",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x20),                             INS_FLAGS_None)    // Packed sign extend byte to short
INST3(pmovsxbd,         "pmovsxbd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x21),                             INS_FLAGS_None)    // Packed sign extend byte to int
INST3(pmovsxbq,         "pmovsxbq",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x22),                             INS_FLAGS_None)    // Packed sign extend byte to long
INST3(pmovsxwd,         "pmovsxwd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x23),                             INS_FLAGS_None)    // Packed sign extend short to int
INST3(pmovsxwq,         "pmovsxwq",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x24),                             INS_FLAGS_None)    // Packed sign extend short to long
INST3(pmovsxdq,         "pmovsxdq",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x25),                             INS_FLAGS_None)    // Packed sign extend int to long
INST3(pmovzxbw,         "pmovzxbw",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x30),                             INS_FLAGS_None)    // Packed zero extend byte to short
INST3(pmovzxbd,         "pmovzxbd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x31),                             INS_FLAGS_None)    // Packed zero extend byte to intg
INST3(pmovzxbq,         "pmovzxbq",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x32),                             INS_FLAGS_None)    // Packed zero extend byte to lon
INST3(pmovzxwd,         "pmovzxwd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x33),                             INS_FLAGS_None)    // Packed zero extend short to int
INST3(pmovzxwq,         "pmovzxwq",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x34),                             INS_FLAGS_None)    // Packed zero extend short to long
INST3(pmovzxdq,         "pmovzxdq",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x35),                             INS_FLAGS_None)    // Packed zero extend int to long
INST3(packusdw,         "packusdw",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x2B),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Pack (narrow) int to unsigned short with saturation
INST3(roundps,          "roundps",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x08),                             INS_FLAGS_None)    // Round packed single precision floating-point values
INST3(roundss,          "roundss",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x0A),                             INS_Flags_IsDstSrcSrcAVXInstruction)    // Round scalar single precision floating-point values
INST3(roundpd,          "roundpd",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x09),                             INS_FLAGS_None)    // Round packed double precision floating-point values
INST3(roundsd,          "roundsd",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x0B),                             INS_Flags_IsDstSrcSrcAVXInstruction)    // Round scalar double precision floating-point values
INST3(pmuldq,           "pmuldq",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x28),                             INS_Flags_IsDstDstSrcAVXInstruction)    // packed multiply 32-bit signed integers and store 64-bit result
INST3(blendps,          "blendps",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x0C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Blend Packed Single Precision Floating-Point Values
INST3(blendvps,         "blendvps",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x14),                             INS_FLAGS_None)    // Variable Blend Packed Singles
INST3(blendpd,          "blendpd",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x0D),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Blend Packed Double Precision Floating-Point Values
INST3(blendvpd,         "blendvpd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x15),                             INS_FLAGS_None)    // Variable Blend Packed Doubles
INST3(pblendw,          "pblendw",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x0E),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Blend Packed Words
INST3(pblendvb,         "pblendvb",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x10),                             INS_FLAGS_None)    // Variable Blend Packed Bytes
INST3(phaddw,           "phaddw",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x01),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed horizontal add of 16-bit integers
INST3(phsubw,           "phsubw",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x05),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed horizontal subtract of 16-bit integers
INST3(phsubd,           "phsubd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x06),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed horizontal subtract of 32-bit integers
INST3(phaddsw,          "phaddsw",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x03),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed horizontal add of 16-bit integers with saturation
INST3(phsubsw,          "phsubsw",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x07),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Packed horizontal subtract of 16-bit integers with saturation
INST3(lddqu,            "lddqu",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0xF0),                            INS_FLAGS_None)    // Load Unaligned integer
INST3(movntdqa,         "movntdqa",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x2A),                             INS_FLAGS_None)    // Load Double Quadword Non-Temporal Aligned Hint
INST3(movddup,          "movddup",          IUM_WR, BAD_CODE,     BAD_CODE,     SSEDBL(0x12),                            INS_FLAGS_None)    // Replicate Double FP Values
INST3(movsldup,         "movsldup",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x12),                            INS_FLAGS_None)    // Replicate even-indexed Single FP Values
INST3(movshdup,         "movshdup",         IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0x16),                            INS_FLAGS_None)    // Replicate odd-indexed Single FP Values
INST3(phminposuw,       "phminposuw",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x41),                             INS_FLAGS_None)    // Packed Horizontal Word Minimum
INST3(mpsadbw,          "mpsadbw",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x42),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Compute Multiple Packed Sums of Absolute Difference
INST3(pinsrb,           "pinsrb",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x20),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Insert Byte
INST3(pinsrd,           "pinsrd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x22),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Insert Dword
INST3(pinsrq,           "pinsrq",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x22),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Insert Qword
INST3(pextrb,           "pextrb",           IUM_WR, SSE3A(0x14),  BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Extract Byte
INST3(pextrd,           "pextrd",           IUM_WR, SSE3A(0x16),  BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Extract Dword
INST3(pextrq,           "pextrq",           IUM_WR, SSE3A(0x16),  BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Extract Qword
INST3(pextrw_sse41,     "pextrw",           IUM_WR, SSE3A(0x15),  BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Extract Word
INST3(extractps,        "extractps",        IUM_WR, SSE3A(0x17),  BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Extract Packed Floating-Point Values

//PCLMULQDQ instructions
INST3(pclmulqdq,        "pclmulqdq" ,       IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x44),                             INS_Flags_IsDstDstSrcAVXInstruction)   // Perform a carry-less multiplication of two quadwords

//AES instructions
INST3(aesdec,           "aesdec",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xDE),                             INS_Flags_IsDstDstSrcAVXInstruction)   // Perform one round of an AES decryption flow
INST3(aesdeclast,       "aesdeclast",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xDF),                             INS_Flags_IsDstDstSrcAVXInstruction)   // Perform last round of an AES decryption flow
INST3(aesenc,           "aesenc",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xDC),                             INS_Flags_IsDstDstSrcAVXInstruction)   // Perform one round of an AES encryption flow
INST3(aesenclast,       "aesenclast",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xDD),                             INS_Flags_IsDstDstSrcAVXInstruction)   // Perform last round of an AES encryption flow
INST3(aesimc,           "aesimc",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xDB),                             INS_FLAGS_None)   // Perform the AES InvMixColumn Transformation
INST3(aeskeygenassist,  "aeskeygenassist",  IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0xDF),                             INS_FLAGS_None)   // AES Round Key Generation Assist
INST3(LAST_SSE_INSTRUCTION, "LAST_SSE_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)

INST3(FIRST_AVX_INSTRUCTION, "FIRST_AVX_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)
// AVX only instructions
INST3(vbroadcastss,     "broadcastss",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x18),                             INS_FLAGS_None)    // Broadcast float value read from memory to entire ymm register
INST3(vbroadcastsd,     "broadcastsd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x19),                             INS_FLAGS_None)    // Broadcast float value read from memory to entire ymm register
INST3(vpbroadcastb,     "pbroadcastb",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x78),                             INS_FLAGS_None)    // Broadcast int8 value from reg/memory to entire ymm register
INST3(vpbroadcastw,     "pbroadcastw",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x79),                             INS_FLAGS_None)    // Broadcast int16 value from reg/memory to entire ymm register
INST3(vpbroadcastd,     "pbroadcastd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x58),                             INS_FLAGS_None)    // Broadcast int32 value from reg/memory to entire ymm register
INST3(vpbroadcastq,     "pbroadcastq",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x59),                             INS_FLAGS_None)    // Broadcast int64 value from reg/memory to entire ymm register
INST3(vextractf128,     "extractf128",      IUM_WR, SSE3A(0x19),  BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Extract 128-bit packed floating point values
INST3(vextracti128,     "extracti128",      IUM_WR, SSE3A(0x39),  BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Extract 128-bit packed integer values
INST3(vinsertf128,      "insertf128",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x18),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Insert 128-bit packed floating point values
INST3(vinserti128,      "inserti128",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x38),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Insert 128-bit packed integer values
INST3(vzeroupper,       "zeroupper",        IUM_WR, 0xC577F8,     BAD_CODE,     BAD_CODE,                                INS_FLAGS_None)    // Zero upper 128-bits of all YMM regs (includes 2-byte fixed VEX prefix)
INST3(vperm2i128,       "perm2i128",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x46),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Permute 128-bit halves of input register
INST3(vpermq,           "permq",            IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x00),                             INS_FLAGS_None)    // Permute 64-bit of input register
INST3(vpblendd,         "pblendd",          IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x02),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Blend Packed DWORDs
INST3(vblendvps,        "blendvps",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x4A),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Blend Packed Singles
INST3(vblendvpd,        "blendvpd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x4B),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Blend Packed Doubles
INST3(vpblendvb,        "pblendvb",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x4C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Blend Packed Bytes
INST3(vtestps,          "testps",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x0E),                             INS_FLAGS_None)    // Packed Bit Test
INST3(vtestpd,          "testpd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x0F),                             INS_FLAGS_None)    // Packed Bit Test
INST3(vpsrlvd,          "psrlvd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x45),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Bit Shift Right Logical
INST3(vpsrlvq,          "psrlvq",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x45),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Bit Shift Right Logical
INST3(vpsravd,          "psravd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x46),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Bit Shift Right Arithmetic
INST3(vpsllvd,          "psllvd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x47),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Bit Shift Left Logical
INST3(vpsllvq,          "psllvq",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x47),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Variable Bit Shift Left Logical
INST3(vpermilps,        "permilps",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x04),                             INS_FLAGS_None)    // Permute In-Lane of Quadruples of Single-Precision Floating-Point Values
INST3(vpermilpd,        "permilpd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x05),                             INS_FLAGS_None)    // Permute In-Lane of Quadruples of Double-Precision Floating-Point Values
INST3(vpermilpsvar,     "permilpsvar",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x0C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Permute In-Lane of Quadruples of Single-Precision Floating-Point Values
INST3(vpermilpdvar,     "permilpdvar",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x0D),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Permute In-Lane of Quadruples of Double-Precision Floating-Point Values
INST3(vperm2f128,       "perm2f128",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x06),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Permute Floating-Point Values
INST3(vpermpd,          "permpd",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0x01),                             INS_FLAGS_None)    // Permute Double-Precision Floating-Point Values
INST3(vpermd,           "permd",            IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x36),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Permute Packed Doublewords Elements
INST3(vpermps,          "permps",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x16),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Permute Single-Precision Floating-Point Elements
INST3(vbroadcastf128,   "broadcastf128",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x1A),                             INS_FLAGS_None)    // Broadcast packed float values read from memory to entire ymm register
INST3(vbroadcasti128,   "broadcasti128",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x5A),                             INS_FLAGS_None)    // Broadcast packed integer values read from memory to entire ymm register
INST3(vmaskmovps,       "maskmovps",        IUM_WR, SSE38(0x2E),  BAD_CODE,     SSE38(0x2C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Conditional SIMD Packed Single-Precision Floating-Point Loads and Stores
INST3(vmaskmovpd,       "maskmovpd",        IUM_WR, SSE38(0x2F),  BAD_CODE,     SSE38(0x2D),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Conditional SIMD Packed Double-Precision Floating-Point Loads and Stores
INST3(vpmaskmovd,       "pmaskmovd",        IUM_WR, SSE38(0x8E),  BAD_CODE,     SSE38(0x8C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Conditional SIMD Integer Packed Dword Loads and Stores
INST3(vpmaskmovq,       "pmaskmovq",        IUM_WR, SSE38(0x8E),  BAD_CODE,     SSE38(0x8C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Conditional SIMD Integer Packed Qword Loads and Stores
INST3(vpgatherdd,       "pgatherdd",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x90),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed Dword Values Using Signed Dword
INST3(vpgatherqd,       "pgatherqd",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x91),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed Dword Values Using Signed Qword
INST3(vpgatherdq,       "pgatherdq",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x90),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed Dword with Signed Dword Indices
INST3(vpgatherqq,       "pgatherqq",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x91),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed Qword with Signed Dword Indices
INST3(vgatherdps,       "gatherdps",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x92),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed SP FP values Using Signed Dword Indices
INST3(vgatherqps,       "gatherqps",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x93),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed SP FP values Using Signed Qword Indices
INST3(vgatherdpd,       "gatherdpd",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x92),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed DP FP Values Using Signed Dword Indices
INST3(vgatherqpd,       "gatherqpd",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x93),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Gather Packed DP FP Values Using Signed Qword Indices

INST3(FIRST_FMA_INSTRUCTION, "FIRST_FMA_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)
//    id                nm                  um      mr            mi            rm                                       flags
INST3(vfmadd132pd,      "fmadd132pd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x98),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Add of Packed Double-Precision Floating-Point Values
INST3(vfmadd213pd,      "fmadd213pd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA8),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmadd231pd,      "fmadd231pd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB8),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmadd132ps,      "fmadd132ps",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x98),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Add of Packed Single-Precision Floating-Point Values
INST3(vfmadd213ps,      "fmadd213ps",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA8),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmadd231ps,      "fmadd231ps",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB8),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmadd132sd,      "fmadd132sd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x99),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Add of Scalar Double-Precision Floating-Point Values
INST3(vfmadd213sd,      "fmadd213sd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA9),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmadd231sd,      "fmadd231sd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB9),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmadd132ss,      "fmadd132ss",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x99),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Add of Scalar Single-Precision Floating-Point Values
INST3(vfmadd213ss,      "fmadd213ss",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA9),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmadd231ss,      "fmadd231ss",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB9),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmaddsub132pd,   "fmaddsub132pd",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x96),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Alternating Add/Subtract of Packed Double-Precision Floating-Point Values
INST3(vfmaddsub213pd,   "fmaddsub213pd",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA6),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmaddsub231pd,   "fmaddsub231pd",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB6),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmaddsub132ps,   "fmaddsub132ps",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x96),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Alternating Add/Subtract of Packed Single-Precision Floating-Point Values
INST3(vfmaddsub213ps,   "fmaddsub213ps",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA6),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmaddsub231ps,   "fmaddsub231ps",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB6),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsubadd132pd,   "fmsubadd132pd",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x97),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Alternating Subtract/Add of Packed Double-Precision Floating-Point Values
INST3(vfmsubadd213pd,   "fmsubadd213pd",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA7),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsubadd231pd,   "fmsubadd231pd",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB7),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsubadd132ps,   "fmsubadd132ps",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x97),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Alternating Subtract/Add of Packed Single-Precision Floating-Point Values
INST3(vfmsubadd213ps,   "fmsubadd213ps",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xA7),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsubadd231ps,   "fmsubadd231ps",    IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xB7),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub132pd,      "fmsub132pd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9A),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Subtract of Packed Double-Precision Floating-Point Values
INST3(vfmsub213pd,      "fmsub213pd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAA),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub231pd,      "fmsub231pd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBA),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub132ps,      "fmsub132ps",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9A),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Subtract of Packed Single-Precision Floating-Point Values
INST3(vfmsub213ps,      "fmsub213ps",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAA),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub231ps,      "fmsub231ps",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBA),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub132sd,      "fmsub132sd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9B),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Subtract of Scalar Double-Precision Floating-Point Values
INST3(vfmsub213sd,      "fmsub213sd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAB),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub231sd,      "fmsub231sd",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBB),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub132ss,      "fmsub132ss",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9B),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Multiply-Subtract of Scalar Single-Precision Floating-Point Values
INST3(vfmsub213ss,      "fmsub213ss",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAB),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfmsub231ss,      "fmsub231ss",       IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBB),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd132pd,     "fnmadd132pd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Add of Packed Double-Precision Floating-Point Values
INST3(vfnmadd213pd,     "fnmadd213pd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAC),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd231pd,     "fnmadd231pd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBC),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd132ps,     "fnmadd132ps",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9C),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Add of Packed Single-Precision Floating-Point Values
INST3(vfnmadd213ps,     "fnmadd213ps",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAC),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd231ps,     "fnmadd231ps",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBC),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd132sd,     "fnmadd132sd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9D),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Add of Scalar Double-Precision Floating-Point Values
INST3(vfnmadd213sd,     "fnmadd213sd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAD),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd231sd,     "fnmadd231sd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBD),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd132ss,     "fnmadd132ss",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9D),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Add of Scalar Single-Precision Floating-Point Values
INST3(vfnmadd213ss,     "fnmadd213ss",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAD),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmadd231ss,     "fnmadd231ss",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBD),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub132pd,     "fnmsub132pd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9E),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Subtract of Packed Double-Precision Floating-Point Values
INST3(vfnmsub213pd,     "fnmsub213pd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAE),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub231pd,     "fnmsub231pd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBE),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub132ps,     "fnmsub132ps",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9E),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Subtract of Packed Single-Precision Floating-Point Values
INST3(vfnmsub213ps,     "fnmsub213ps",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAE),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub231ps,     "fnmsub231ps",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBE),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub132sd,     "fnmsub132sd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9F),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Subtract of Scalar Double-Precision Floating-Point Values
INST3(vfnmsub213sd,     "fnmsub213sd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAF),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub231sd,     "fnmsub231sd",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBF),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub132ss,     "fnmsub132ss",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x9F),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Fused Negative Multiply-Subtract of Scalar Single-Precision Floating-Point Values
INST3(vfnmsub213ss,     "fnmsub213ss",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xAF),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(vfnmsub231ss,     "fnmsub231ss",      IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xBF),                             INS_Flags_IsDstDstSrcAVXInstruction)    //
INST3(LAST_FMA_INSTRUCTION, "LAST_FMA_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)

INST3(FIRST_AVXVNNI_INSTRUCTION, "FIRST_AVXVNNI_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)
INST3(vpdpbusd,          "pdpbusd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x50),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply and Add Unsigned and Signed Bytes
INST3(vpdpwssd,          "pdpwssd",         IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x52),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply and Add Signed Word Integers
INST3(vpdpbusds,         "pdpbusds",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x51),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply and Add Unsigned and Signed Bytes with Saturation
INST3(vpdpwssds,         "pdpwssds",        IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0x53),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Multiply and Add Signed Word Integers with Saturation
INST3(LAST_AVXVNNI_INSTRUCTION, "LAST_AVXVNNI_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)

// BMI1
INST3(FIRST_BMI_INSTRUCTION, "FIRST_BMI_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)
INST3(andn,             "andn",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF2),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Logical AND NOT
INST3(blsi,             "blsi",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF3),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Extract Lowest Set Isolated Bit
INST3(blsmsk,           "blsmsk",           IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF3),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Get Mask Up to Lowest Set Bit
INST3(blsr,             "blsr",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF3),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Reset Lowest Set Bit
INST3(bextr,            "bextr",            IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF7),                             INS_Flags_IsDstDstSrcAVXInstruction)    // Bit Field Extract

// BMI2
INST3(rorx,             "rorx",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE3A(0xF0),                             INS_FLAGS_None)
INST3(pdep,             "pdep",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF5),                             INS_Flags_IsDstDstSrcAVXInstruction)                               // Parallel Bits Deposit
INST3(pext,             "pext",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF5),                             INS_Flags_IsDstDstSrcAVXInstruction)                               // Parallel Bits Extract
INST3(bzhi,             "bzhi",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF5),                             Resets_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Undefined_PF  | Writes_CF     | INS_Flags_IsDstDstSrcAVXInstruction)    // Zero High Bits Starting with Specified Bit Position
INST3(mulx,             "mulx",             IUM_WR, BAD_CODE,     BAD_CODE,     SSE38(0xF6),                             INS_Flags_IsDstDstSrcAVXInstruction)                               // Unsigned Multiply Without Affecting Flags

INST3(LAST_BMI_INSTRUCTION, "LAST_BMI_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)

INST3(LAST_AVX_INSTRUCTION, "LAST_AVX_INSTRUCTION", IUM_WR, BAD_CODE, BAD_CODE, BAD_CODE, INS_FLAGS_None)

// Scalar instructions in SSE4.2
INST3(crc32,            "crc32",            IUM_WR, BAD_CODE,     BAD_CODE,     PACK4(0xF2, 0x0F, 0x38, 0xF0),           INS_FLAGS_None)

// BMI1
INST3(tzcnt,            "tzcnt",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0xBC),                            Undefined_OF   | Undefined_SF  | Writes_ZF     | Undefined_AF  | Undefined_PF  | Writes_CF )    // Count the Number of Trailing Zero Bits

// LZCNT
INST3(lzcnt,            "lzcnt",            IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0xBD),                            Undefined_OF   | Undefined_SF  | Writes_ZF     | Undefined_AF  | Undefined_PF  | Writes_CF )

// POPCNT
INST3(popcnt,           "popcnt",           IUM_WR, BAD_CODE,     BAD_CODE,     SSEFLT(0xB8),                            Resets_OF      | Resets_SF     | Writes_ZF     | Resets_AF     | Resets_PF     | Resets_CF )

//    id                nm                  um      mr            mi                                                     flags
INST2(ret,              "ret",              IUM_RD, 0x0000C3,     0x0000C2,                                              INS_FLAGS_None )
INST2(loop,             "loop",             IUM_RD, BAD_CODE,     0x0000E2,                                              INS_FLAGS_None )
INST2(call,             "call",             IUM_RD, 0x0010FF,     0x0000E8,                                              INS_FLAGS_None )

INST2(rol,              "rol",              IUM_RW, 0x0000D2,     BAD_CODE,                                              Undefined_OF                                                                   | Writes_CF     )
INST2(rol_1,            "rol",              IUM_RW, 0x0000D0,     0x0000D0,                                              Writes_OF                                                                      | Writes_CF     )
INST2(rol_N,            "rol",              IUM_RW, 0x0000C0,     0x0000C0,                                              Undefined_OF                                                                   | Writes_CF     )
INST2(ror,              "ror",              IUM_RW, 0x0008D2,     BAD_CODE,                                              Undefined_OF                                                                   | Writes_CF     )
INST2(ror_1,            "ror",              IUM_RW, 0x0008D0,     0x0008D0,                                              Writes_OF                                                                      | Writes_CF     )
INST2(ror_N,            "ror",              IUM_RW, 0x0008C0,     0x0008C0,                                              Undefined_OF                                                                   | Writes_CF     )

INST2(rcl,              "rcl",              IUM_RW, 0x0010D2,     BAD_CODE,                                              Undefined_OF                                                                   | Writes_CF
                                                                                                                                                                                                        | Reads_CF      )
INST2(rcl_1,            "rcl",              IUM_RW, 0x0010D0,     0x0010D0,                                              Writes_OF                                                                      | Writes_CF
                                                                                                                                                                                                        | Reads_CF      )
INST2(rcl_N,            "rcl",              IUM_RW, 0x0010C0,     0x0010C0,                                              Undefined_OF                                                                   | Writes_CF
                                                                                                                                                                                                        | Reads_CF      ) 
INST2(rcr,              "rcr",              IUM_RW, 0x0018D2,     BAD_CODE,                                              Undefined_OF                                                                   | Writes_CF
                                                                                                                                                                                                        | Reads_CF      )
INST2(rcr_1,            "rcr",              IUM_RW, 0x0018D0,     0x0018D0,                                              Writes_OF                                                                      | Writes_CF
                                                                                                                                                                                                        | Reads_CF      )   
INST2(rcr_N,            "rcr",              IUM_RW, 0x0018C0,     0x0018C0,                                              Undefined_OF                                                                   | Writes_CF
                                                                                                                                                                                                        | Reads_CF      )
INST2(shl,              "shl",              IUM_RW, 0x0020D2,     BAD_CODE,                                              Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(shl_1,            "shl",              IUM_RW, 0x0020D0,     0x0020D0,                                              Writes_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(shl_N,            "shl",              IUM_RW, 0x0020C0,     0x0020C0,                                              Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(shr,              "shr",              IUM_RW, 0x0028D2,     BAD_CODE,                                              Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(shr_1,            "shr",              IUM_RW, 0x0028D0,     0x0028D0,                                              Writes_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(shr_N,            "shr",              IUM_RW, 0x0028C0,     0x0028C0,                                              Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(sar,              "sar",              IUM_RW, 0x0038D2,     BAD_CODE,                                              Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(sar_1,            "sar",              IUM_RW, 0x0038D0,     0x0038D0,                                              Writes_OF      | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST2(sar_N,            "sar",              IUM_RW, 0x0038C0,     0x0038C0,                                              Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )


//    id                nm                  um      mr                                                                   flags
INST1(r_movsb,          "rep movsb",        IUM_RD, 0x00A4F3,                                                            Reads_DF )
INST1(r_movsd,          "rep movsd",        IUM_RD, 0x00A5F3,                                                            Reads_DF )
#if defined(TARGET_AMD64)
INST1(r_movsq,          "rep movsq",        IUM_RD, 0xF3A548,                                                            Reads_DF )
#endif // defined(TARGET_AMD64)
INST1(movsb,            "movsb",            IUM_RD, 0x0000A4,                                                            Reads_DF   )
INST1(movsd,            "movsd",            IUM_RD, 0x0000A5,                                                            Reads_DF   )
#if defined(TARGET_AMD64)
INST1(movsq,            "movsq",            IUM_RD, 0x00A548,                                                            Reads_DF   )
#endif // defined(TARGET_AMD64)

INST1(r_stosb,          "rep stosb",        IUM_RD, 0x00AAF3,                                                            Reads_DF   )
INST1(r_stosd,          "rep stosd",        IUM_RD, 0x00ABF3,                                                            Reads_DF   )
#if defined(TARGET_AMD64)
INST1(r_stosq,          "rep stosq",        IUM_RD, 0xF3AB48,                                                            Reads_DF   )
#endif // defined(TARGET_AMD64)
INST1(stosb,            "stosb",            IUM_RD, 0x0000AA,                                                            Reads_DF   )
INST1(stosd,            "stosd",            IUM_RD, 0x0000AB,                                                            Reads_DF   )
#if defined(TARGET_AMD64)
INST1(stosq,            "stosq",            IUM_RD, 0x00AB48,                                                            Reads_DF   )
#endif // defined(TARGET_AMD64)

INST1(int3,             "int3",             IUM_RD, 0x0000CC,                                                            INS_FLAGS_None )
INST1(nop,              "nop",              IUM_RD, 0x000090,                                                            INS_FLAGS_None )
INST1(lock,             "lock",             IUM_RD, 0x0000F0,                                                            INS_FLAGS_None )
INST1(leave,            "leave",            IUM_RD, 0x0000C9,                                                            INS_FLAGS_None )


INST1(neg,              "neg",              IUM_RW, 0x0018F6,                                                            Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF     )
INST1(not,              "not",              IUM_RW, 0x0010F6,                                                            INS_FLAGS_None )

INST1(cdq,              "cdq",              IUM_RD, 0x000099,                                                            INS_FLAGS_None)
INST1(idiv,             "idiv",             IUM_RD, 0x0038F6,                                                            Undefined_OF   | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Undefined_CF  )
INST1(imulEAX,          "imul",             IUM_RD, 0x0028F6,                                                            Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )
INST1(div,              "div",              IUM_RD, 0x0030F6,                                                            Undefined_OF   | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Undefined_CF  )
INST1(mulEAX,           "mul",              IUM_RD, 0x0020F6,                                                            Writes_OF      | Undefined_SF  | Undefined_ZF  | Undefined_AF  | Undefined_PF  | Writes_CF     )

INST1(sahf,             "sahf",             IUM_RD, 0x00009E,                                                            Restore_SF_ZF_AF_PF_CF )

INST1(xadd,             "xadd",             IUM_RW, 0x0F00C0,                                                            Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF     )
INST1(cmpxchg,          "cmpxchg",          IUM_RW, 0x0F00B0,                                                            Writes_OF      | Writes_SF     | Writes_ZF     | Writes_AF     | Writes_PF     | Writes_CF     )

INST1(shld,             "shld",             IUM_RW, 0x0F00A4,                                                            Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )
INST1(shrd,             "shrd",             IUM_RW, 0x0F00AC,                                                            Undefined_OF   | Writes_SF     | Writes_ZF     | Undefined_AF  | Writes_PF     | Writes_CF     )

// For RyuJIT/x86, we follow the x86 calling convention that requires
// us to return floating point value on the x87 FP stack, so we need
// these instructions regardless of whether we're using full stack fp.
#ifdef TARGET_X86
INST1(fld,              "fld",              IUM_WR, 0x0000D9,                                                            INS_FLAGS_x87Instr)
INST1(fstp,             "fstp",             IUM_WR, 0x0018D9,                                                            INS_FLAGS_x87Instr)
#endif // TARGET_X86

INST1(seto,             "seto",             IUM_WR, 0x0F0090,                                                            Reads_OF )
INST1(setno,            "setno",            IUM_WR, 0x0F0091,                                                            Reads_OF )
INST1(setb,             "setb",             IUM_WR, 0x0F0092,                                                            Reads_CF )
INST1(setae,            "setae",            IUM_WR, 0x0F0093,                                                            Reads_CF )
INST1(sete,             "sete",             IUM_WR, 0x0F0094,                                                            Reads_ZF )
INST1(setne,            "setne",            IUM_WR, 0x0F0095,                                                            Reads_ZF )
INST1(setbe,            "setbe",            IUM_WR, 0x0F0096,                                                            Reads_ZF | Reads_CF )
INST1(seta,             "seta",             IUM_WR, 0x0F0097,                                                            Reads_ZF | Reads_CF )
INST1(sets,             "sets",             IUM_WR, 0x0F0098,                                                            Reads_SF )
INST1(setns,            "setns",            IUM_WR, 0x0F0099,                                                            Reads_SF )
INST1(setp,             "setp",             IUM_WR, 0x0F009A,                                                            Reads_PF )
INST1(setnp,            "setnp",            IUM_WR, 0x0F009B,                                                            Reads_PF )
INST1(setl,             "setl",             IUM_WR, 0x0F009C,                                                            Reads_OF       | Reads_SF  )
INST1(setge,            "setge",            IUM_WR, 0x0F009D,                                                            Reads_OF       | Reads_SF  )
INST1(setle,            "setle",            IUM_WR, 0x0F009E,                                                            Reads_OF       | Reads_SF      | Reads_ZF  )
INST1(setg,             "setg",             IUM_WR, 0x0F009F,                                                            Reads_OF       | Reads_SF      | Reads_ZF  )

#ifdef TARGET_AMD64
// A jump with rex prefix. This is used for register indirect
// tail calls.
INST1(rex_jmp,          "rex.jmp",          IUM_RD, 0x0020FE,                                                            INS_FLAGS_None)
#endif

INST1(i_jmp,            "jmp",              IUM_RD, 0x0020FE,                                                            INS_FLAGS_None )

INST0(jmp,              "jmp",              IUM_RD, 0x0000EB,                                                            INS_FLAGS_None )
INST0(jo,               "jo",               IUM_RD, 0x000070,                                                            Reads_OF )
INST0(jno,              "jno",              IUM_RD, 0x000071,                                                            Reads_OF )
INST0(jb,               "jb",               IUM_RD, 0x000072,                                                            Reads_CF )
INST0(jae,              "jae",              IUM_RD, 0x000073,                                                            Reads_CF )
INST0(je,               "je",               IUM_RD, 0x000074,                                                            Reads_ZF )
INST0(jne,              "jne",              IUM_RD, 0x000075,                                                            Reads_ZF )
INST0(jbe,              "jbe",              IUM_RD, 0x000076,                                                            Reads_ZF | Reads_CF )
INST0(ja,               "ja",               IUM_RD, 0x000077,                                                            Reads_ZF | Reads_CF )
INST0(js,               "js",               IUM_RD, 0x000078,                                                            Reads_SF )
INST0(jns,              "jns",              IUM_RD, 0x000079,                                                            Reads_SF )
INST0(jp,               "jp",               IUM_RD, 0x00007A,                                                            Reads_PF )
INST0(jnp,              "jnp",              IUM_RD, 0x00007B,                                                            Reads_PF )
INST0(jl,               "jl",               IUM_RD, 0x00007C,                                                            Reads_OF       | Reads_SF  )
INST0(jge,              "jge",              IUM_RD, 0x00007D,                                                            Reads_OF       | Reads_SF  )
INST0(jle,              "jle",              IUM_RD, 0x00007E,                                                            Reads_OF       | Reads_SF      | Reads_ZF  )
INST0(jg,               "jg",               IUM_RD, 0x00007F,                                                            Reads_OF       | Reads_SF      | Reads_ZF  )

INST0(l_jmp,            "jmp",              IUM_RD, 0x0000E9,                                                            INS_FLAGS_None )
INST0(l_jo,             "jo",               IUM_RD, 0x00800F,                                                            Reads_OF )
INST0(l_jno,            "jno",              IUM_RD, 0x00810F,                                                            Reads_OF )
INST0(l_jb,             "jb",               IUM_RD, 0x00820F,                                                            Reads_CF )
INST0(l_jae,            "jae",              IUM_RD, 0x00830F,                                                            Reads_CF )
INST0(l_je,             "je",               IUM_RD, 0x00840F,                                                            Reads_ZF )
INST0(l_jne,            "jne",              IUM_RD, 0x00850F,                                                            Reads_ZF )
INST0(l_jbe,            "jbe",              IUM_RD, 0x00860F,                                                            Reads_ZF | Reads_CF )
INST0(l_ja,             "ja",               IUM_RD, 0x00870F,                                                            Reads_ZF | Reads_CF )
INST0(l_js,             "js",               IUM_RD, 0x00880F,                                                            Reads_SF )
INST0(l_jns,            "jns",              IUM_RD, 0x00890F,                                                            Reads_SF )
INST0(l_jp,             "jp",               IUM_RD, 0x008A0F,                                                            Reads_PF )
INST0(l_jnp,            "jnp",              IUM_RD, 0x008B0F,                                                            Reads_PF )
INST0(l_jl,             "jl",               IUM_RD, 0x008C0F,                                                            Reads_OF       | Reads_SF  )
INST0(l_jge,            "jge",              IUM_RD, 0x008D0F,                                                            Reads_OF       | Reads_SF  )
INST0(l_jle,            "jle",              IUM_RD, 0x008E0F,                                                            Reads_OF       | Reads_SF      | Reads_ZF  )
INST0(l_jg,             "jg",               IUM_RD, 0x008F0F,                                                            Reads_OF       | Reads_SF      | Reads_ZF  )

INST0(align,            "align",            IUM_RD, BAD_CODE,                                                            INS_FLAGS_None)

/*****************************************************************************/
#undef  INST0
#undef  INST1
#undef  INST2
#undef  INST3
#undef  INST4
#undef  INST5
/*****************************************************************************/

// clang-format on
