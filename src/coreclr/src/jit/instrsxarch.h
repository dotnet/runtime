// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  This file was previously known as instrs.h
//
/*****************************************************************************
 *  x86 instructions for  the JIT compiler
 *
 *          id      -- the enum name for the instruction
 *          nm      -- textual name (for assembly dipslay)
 *          fp      -- 1 = floating point instruction, 0 = not floating point instruction
 *          um      -- update mode, see IUM_xx enum (rd, wr, or rw)
 *          rf      -- 1 = reads flags, 0 = doesn't read flags
 *          wf      -- 1 = writes flags, 0 = doesn't write flags
 *          mr      -- base encoding for R/M[reg] addressing mode
 *          mi      -- base encoding for R/M,icon addressing mode
 *          rm      -- base encoding for reg,R/M  addressing mode
 *          a4      -- base encoding for eax,i32  addressing mode
 *          rr      -- base encoding for register addressing mode
 *
******************************************************************************/

// clang-format off
#if !defined(_TARGET_XARCH_)
  #error Unexpected target type
#endif

#ifndef INST1
#error  At least INST1 must be defined before including this file.
#endif
/*****************************************************************************/
#ifndef INST0
#define INST0(id, nm, fp, um, rf, wf, mr                )
#endif
#ifndef INST2
#define INST2(id, nm, fp, um, rf, wf, mr, mi            )
#endif
#ifndef INST3
#define INST3(id, nm, fp, um, rf, wf, mr, mi, rm        )
#endif
#ifndef INST4
#define INST4(id, nm, fp, um, rf, wf, mr, mi, rm, a4    )
#endif
#ifndef INST5
#define INST5(id, nm, fp, um, rf, wf, mr, mi, rm, a4, rr)
#endif

/*****************************************************************************/
/*               The following is x86-specific                               */
/*****************************************************************************/

//    enum     name            FP  updmode rf wf R/M[reg]  R/M,icon  reg,R/M   eax,i32   register
INST5(invalid, "INVALID"      , 0, IUM_RD, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE, BAD_CODE, BAD_CODE)

INST5(push   , "push"         , 0, IUM_RD, 0, 0, 0x0030FE, 0x000068, BAD_CODE, BAD_CODE, 0x000050)
INST5(pop    , "pop"          , 0, IUM_WR, 0, 0, 0x00008E, BAD_CODE, BAD_CODE, BAD_CODE, 0x000058)
// Does not affect the stack tracking in the emitter
INST5(push_hide, "push"       , 0, IUM_RD, 0, 0, 0x0030FE, 0x000068, BAD_CODE, BAD_CODE, 0x000050)
INST5(pop_hide,  "pop"        , 0, IUM_WR, 0, 0, 0x00008E, BAD_CODE, BAD_CODE, BAD_CODE, 0x000058)

INST5(inc    , "inc"          , 0, IUM_RW, 0, 1, 0x0000FE, BAD_CODE, BAD_CODE, BAD_CODE, 0x000040)
INST5(inc_l  , "inc"          , 0, IUM_RW, 0, 1, 0x0000FE, BAD_CODE, BAD_CODE, BAD_CODE, 0x00C0FE)
INST5(dec    , "dec"          , 0, IUM_RW, 0, 1, 0x0008FE, BAD_CODE, BAD_CODE, BAD_CODE, 0x000048)
INST5(dec_l  , "dec"          , 0, IUM_RW, 0, 1, 0x0008FE, BAD_CODE, BAD_CODE, BAD_CODE, 0x00C8FE)

//    enum     name            FP  updmode rf wf R/M,R/M[reg] R/M,icon  reg,R/M   eax,i32

INST4(add    , "add"          , 0, IUM_RW, 0, 1, 0x000000, 0x000080, 0x000002, 0x000004)
INST4(or     , "or"           , 0, IUM_RW, 0, 1, 0x000008, 0x000880, 0x00000A, 0x00000C)
INST4(adc    , "adc"          , 0, IUM_RW, 1, 1, 0x000010, 0x001080, 0x000012, 0x000014)
INST4(sbb    , "sbb"          , 0, IUM_RW, 1, 1, 0x000018, 0x001880, 0x00001A, 0x00001C)
INST4(and    , "and"          , 0, IUM_RW, 0, 1, 0x000020, 0x002080, 0x000022, 0x000024)
INST4(sub    , "sub"          , 0, IUM_RW, 0, 1, 0x000028, 0x002880, 0x00002A, 0x00002C)
INST4(xor    , "xor"          , 0, IUM_RW, 0, 1, 0x000030, 0x003080, 0x000032, 0x000034)
INST4(cmp    , "cmp"          , 0, IUM_RD, 0, 1, 0x000038, 0x003880, 0x00003A, 0x00003C)
INST4(test   , "test"         , 0, IUM_RD, 0, 1, 0x000084, 0x0000F6, 0x000084, 0x0000A8)
INST4(mov    , "mov"          , 0, IUM_WR, 0, 0, 0x000088, 0x0000C6, 0x00008A, 0x0000B0)

INST4(lea    , "lea"          , 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, 0x00008D, BAD_CODE)

//    enum     name            FP  updmode rf wf R/M,R/M[reg]  R/M,icon  reg,R/M

// Note that emitter has only partial support for BT. It can only emit the reg,reg form
// and the registers need to be reversed to get the correct encoding.
INST3(bt     , "bt"           , 0, IUM_RD, 0, 1, 0x0F00A3, BAD_CODE, 0x0F00A3)

INST3(movsx  , "movsx"        , 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, 0x0F00BE)
#ifdef _TARGET_AMD64_
INST3(movsxd , "movsxd"       , 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, 0x4800000063LL )
#endif
INST3(movzx  , "movzx"        , 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, 0x0F00B6)

INST3(cmovo  , "cmovo"        , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0040)
INST3(cmovno , "cmovno"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0041)
INST3(cmovb  , "cmovb"        , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0042)
INST3(cmovae , "cmovae"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0043)
INST3(cmove  , "cmove"        , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0044)
INST3(cmovne , "cmovne"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0045)
INST3(cmovbe , "cmovbe"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0046)
INST3(cmova  , "cmova"        , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0047)
INST3(cmovs  , "cmovs"        , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0048)
INST3(cmovns , "cmovns"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F0049)
INST3(cmovpe , "cmovpe"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F004A)
INST3(cmovpo , "cmovpo"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F004B)
INST3(cmovl  , "cmovl"        , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F004C)
INST3(cmovge , "cmovge"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F004D)
INST3(cmovle , "cmovle"       , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F004E)
INST3(cmovg  , "cmovg"        , 0, IUM_WR, 1, 0, BAD_CODE, BAD_CODE, 0x0F004F)

INST3(xchg   , "xchg"         , 0, IUM_RW, 0, 0, 0x000086, BAD_CODE, 0x000086)
INST3(imul   , "imul"         , 0, IUM_RW, 0, 1, 0x0F00AC, BAD_CODE, 0x0F00AF) // op1 *= op2

//    enum     name            FP  updmode rf wf R/M,R/M[reg]  R/M,icon  reg,R/M

// Instead of encoding these as 3-operand instructions, we encode them
// as 2-operand instructions with the target register being implicit
// implicit_reg = op1*op2_icon
#define INSTMUL INST3
INSTMUL(imul_AX, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x000068, BAD_CODE)
INSTMUL(imul_CX, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x000868, BAD_CODE)
INSTMUL(imul_DX, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x001068, BAD_CODE)
INSTMUL(imul_BX, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x001868, BAD_CODE)
INSTMUL(imul_SP, "imul", 0, IUM_RD, 0, 1, BAD_CODE, BAD_CODE, BAD_CODE)
INSTMUL(imul_BP, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x002868, BAD_CODE)
INSTMUL(imul_SI, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x003068, BAD_CODE)
INSTMUL(imul_DI, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x003868, BAD_CODE)

#ifdef _TARGET_AMD64_

INSTMUL(imul_08, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400000068, BAD_CODE)
INSTMUL(imul_09, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400000868, BAD_CODE)
INSTMUL(imul_10, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400001068, BAD_CODE)
INSTMUL(imul_11, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400001868, BAD_CODE)
INSTMUL(imul_12, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400002068, BAD_CODE)
INSTMUL(imul_13, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400002868, BAD_CODE)
INSTMUL(imul_14, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400003068, BAD_CODE)
INSTMUL(imul_15, "imul", 0, IUM_RD, 0, 1, BAD_CODE, 0x4400003868, BAD_CODE)

#endif // _TARGET_AMD64_

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

#define PACK3(byte1,byte2,byte3) ((byte1 << 16) | (byte2 << 24) | byte3)
#define PACK2(byte1,byte2)                       ((byte1 << 16) | byte2)
#define SSEFLT(c) PACK3(0xf3, 0x0f, c)
#define SSEDBL(c) PACK3(0xf2, 0x0f, c)
#define PCKDBL(c) PACK3(0x66, 0x0f, c)
#define PCKFLT(c) PACK2(0x0f,c)

// These macros encode extra byte that is implicit in the macro.
#define PACK4(byte1,byte2,byte3,byte4) ((byte1 << 16) | (byte2 << 24) | byte3 | (byte4 << 8))
#define SSE38(c)   PACK4(0x66, 0x0f, 0x38, c)
#define SSE3A(c)   PACK4(0x66, 0x0f, 0x3A, c)

// VEX* encodes the implied leading opcode bytes in c1:
// 1: implied 0f, 2: implied 0f 38, 3: implied 0f 3a
#define VEX2INT(c1,c2)   PACK3(c1, 0xc5, c2)
#define VEX3INT(c1,c2)   PACK4(c1, 0xc5, 0x02, c2)
#define VEX3FLT(c1,c2)   PACK4(c1, 0xc5, 0x02, c2)

//  Please insert any SSE2 instructions between FIRST_SSE2_INSTRUCTION and LAST_SSE2_INSTRUCTION
INST3(FIRST_SSE2_INSTRUCTION, "FIRST_SSE2_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

// These are the SSE instructions used on x86
INST3( mov_i2xmm,   "movd"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0x6E)) // Move int reg to a xmm reg. reg1=xmm reg, reg2=int reg
INST3( mov_xmm2i,   "movd"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0x7E)) // Move xmm reg to an int reg. reg1=xmm reg, reg2=int reg
INST3( pmovmskb,    "pmovmskb"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0xD7)) // Move the MSB bits of all bytes in a xmm reg to an int reg
INST3( movmskpd,    "movmskpd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0x50)) // Extract 2-bit sign mask from xmm and store in reg. The upper bits of r32 or r64 are filled with zeros.
INST3( movd,        "movd"        , 0, IUM_WR, 0, 0, PCKDBL(0x7E), BAD_CODE, PCKDBL(0x6E))
INST3( movq,        "movq"        , 0, IUM_WR, 0, 0, PCKDBL(0xD6), BAD_CODE, SSEFLT(0x7E))
INST3( movsdsse2,   "movsd"       , 0, IUM_WR, 0, 0, SSEDBL(0x11), BAD_CODE, SSEDBL(0x10))

INST3( punpckldq,   "punpckldq"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0x62))

INST3( xorps,       "xorps"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0x57)) // XOR packed singles

INST3( cvttsd2si,   "cvttsd2si"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEDBL(0x2C)) // cvt with trunc scalar double to signed DWORDs

INST3( movntdq,     "movntdq"     , 0, IUM_WR, 0, 0, PCKDBL(0xE7), BAD_CODE, BAD_CODE)
INST3( movnti,      "movnti"      , 0, IUM_WR, 0, 0, PCKFLT(0xC3), BAD_CODE, BAD_CODE)
INST3( movntpd,     "movntpd"     , 0, IUM_WR, 0, 0, PCKDBL(0x2B), BAD_CODE, BAD_CODE)
INST3( movntps,     "movntps"     , 0, IUM_WR, 0, 0, PCKFLT(0x2B), BAD_CODE, BAD_CODE)
INST3( movdqu,      "movdqu"      , 0, IUM_WR, 0, 0, SSEFLT(0x7F), BAD_CODE, SSEFLT(0x6F))
INST3( movdqa,      "movdqa"      , 0, IUM_WR, 0, 0, PCKDBL(0x7F), BAD_CODE, PCKDBL(0x6F))
INST3( movlpd,      "movlpd"      , 0, IUM_WR, 0, 0, PCKDBL(0x13), BAD_CODE, PCKDBL(0x12))
INST3( movlps,      "movlps"      , 0, IUM_WR, 0, 0, PCKFLT(0x13), BAD_CODE, PCKFLT(0x12))
INST3( movhpd,      "movhpd"      , 0, IUM_WR, 0, 0, PCKDBL(0x17), BAD_CODE, PCKDBL(0x16))
INST3( movhps,      "movhps"      , 0, IUM_WR, 0, 0, PCKFLT(0x17), BAD_CODE, PCKFLT(0x16))
INST3( movss,       "movss"       , 0, IUM_WR, 0, 0, SSEFLT(0x11), BAD_CODE, SSEFLT(0x10))
INST3( movapd,      "movapd"      , 0, IUM_WR, 0, 0, PCKDBL(0x29), BAD_CODE, PCKDBL(0x28))
INST3( movaps,      "movaps"      , 0, IUM_WR, 0, 0, PCKFLT(0x29), BAD_CODE, PCKFLT(0x28))
INST3( movupd,      "movupd"      , 0, IUM_WR, 0, 0, PCKDBL(0x11), BAD_CODE, PCKDBL(0x10))
INST3( movups,      "movups"      , 0, IUM_WR, 0, 0, PCKFLT(0x11), BAD_CODE, PCKFLT(0x10))
INST3( movhlps,     "movhlps"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0x12))
INST3( movlhps,     "movlhps"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0x16))
INST3( movmskps,    "movmskps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0x50))
INST3( unpckhps,    "unpckhps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0x15))
INST3( unpcklps,    "unpcklps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0x14))
INST3( maskmovdqu,  "maskmovdqu"  , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0xF7))

INST3( shufps,      "shufps"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0xC6))
INST3( shufpd,      "shufpd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0xC6))

INST3( punpckhdq,   "punpckhdq"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0x6A))

INST3( lfence,      "lfence"      , 0, IUM_RD, 0, 0, 0x000FE8AE,   BAD_CODE, BAD_CODE)
INST3( mfence,      "mfence"      , 0, IUM_RD, 0, 0, 0x000FF0AE,   BAD_CODE, BAD_CODE)
INST3( prefetchnta, "prefetchnta" , 0, IUM_RD, 0, 0, 0x000F0018,   BAD_CODE, BAD_CODE)
INST3( prefetcht0,  "prefetcht0"  , 0, IUM_RD, 0, 0, 0x000F0818,   BAD_CODE, BAD_CODE)
INST3( prefetcht1,  "prefetcht1"  , 0, IUM_RD, 0, 0, 0x000F1018,   BAD_CODE, BAD_CODE)
INST3( prefetcht2,  "prefetcht2"  , 0, IUM_RD, 0, 0, 0x000F1818,   BAD_CODE, BAD_CODE)
INST3( sfence,      "sfence"      , 0, IUM_RD, 0, 0, 0x000FF8AE,   BAD_CODE, BAD_CODE)

// SSE 2 arith
INST3( addps,  "addps",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x58))    // Add packed singles
INST3( addss,  "addss",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x58))    // Add scalar singles
INST3( addpd,  "addpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x58))    // Add packed doubles
INST3( addsd,  "addsd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x58))    // Add scalar doubles
INST3( mulps,  "mulps",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x59))    // Multiply packed singles
INST3( mulss,  "mulss",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x59))    // Multiply scalar single
INST3( mulpd,  "mulpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x59))    // Multiply packed doubles
INST3( mulsd,  "mulsd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x59))    // Multiply scalar doubles
INST3( subps,  "subps",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x5C))    // Subtract packed singles
INST3( subss,  "subss",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x5C))    // Subtract scalar singles
INST3( subpd,  "subpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x5C))    // Subtract packed doubles
INST3( subsd,  "subsd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x5C))    // Subtract scalar doubles
INST3( minps,  "minps",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x5D))    // Return Minimum packed singles
INST3( minss,  "minss",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x5D))    // Return Minimum scalar single
INST3( minpd,  "minpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x5D))    // Return Minimum packed doubles
INST3( minsd,  "minsd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x5D))    // Return Minimum scalar double
INST3( divps,  "divps",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x5E))    // Divide packed singles
INST3( divss,  "divss",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x5E))    // Divide scalar singles
INST3( divpd,  "divpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x5E))    // Divide packed doubles
INST3( divsd,  "divsd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x5E))    // Divide scalar doubles
INST3( maxps,  "maxps",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x5F))    // Return Maximum packed singles
INST3( maxss,  "maxss",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x5F))    // Return Maximum scalar single
INST3( maxpd,  "maxpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x5F))    // Return Maximum packed doubles
INST3( maxsd,  "maxsd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x5F))    // Return Maximum scalar double
INST3( xorpd,  "xorpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x57))    // XOR packed doubles
INST3( andps,  "andps",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x54))    // AND packed singles
INST3( andpd,  "andpd",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x54))    // AND packed doubles
INST3( sqrtps, "sqrtps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x51))    // Sqrt of packed singles
INST3( sqrtss, "sqrtss", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x51))    // Sqrt of scalar single
INST3( sqrtpd, "sqrtpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x51))    // Sqrt of packed doubles
INST3( sqrtsd, "sqrtsd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x51))    // Sqrt of scalar double
INST3( andnps, "andnps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x55))    // And-Not packed singles
INST3( andnpd, "andnpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x55))    // And-Not packed doubles
INST3( orps,   "orps",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x56))    // Or packed singles
INST3( orpd,   "orpd",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x56))    // Or packed doubles
INST3( haddpd, "haddpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x7C))    // Horizontal add packed doubles
INST3( haddps, "haddps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x7C))    // Horizontal add packed floats
INST3( hsubpd, "hsubpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x7D))    // Horizontal subtract packed doubles
INST3( hsubps, "hsubps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x7D))    // Horizontal subtract packed floats
INST3( addsubps, "addsubps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0xD0))    // Add/Subtract packed singles
INST3( addsubpd, "addsubpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0xD0))    // Add/Subtract packed doubles

// SSE 2 approx arith
INST3( rcpps,   "rcpps",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x53))    // Reciprocal of packed singles
INST3( rcpss,   "rcpss",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x53))    // Reciprocal of scalar single
INST3( rsqrtps, "rsqrtps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x52))    // Reciprocal Sqrt of packed singles
INST3( rsqrtss, "rsqrtss", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x52))    // Reciprocal Sqrt of scalar single

// SSE2 conversions
INST3( cvtpi2ps,  "cvtpi2ps",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x2A))   // cvt packed DWORDs to singles
INST3( cvtsi2ss,  "cvtsi2ss",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x2A))   // cvt DWORD to scalar single
INST3( cvtpi2pd,  "cvtpi2pd",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x2A))   // cvt packed DWORDs to doubles
INST3( cvtsi2sd,  "cvtsi2sd",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x2A))   // cvt DWORD to scalar double
INST3( cvttps2pi, "cvttps2pi",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x2C))   // cvt with trunc packed singles to DWORDs
INST3( cvttss2si, "cvttss2si",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x2C))   // cvt with trunc scalar single to DWORD
INST3( cvttpd2pi, "cvttpd2pi",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x2C))   // cvt with trunc packed doubles to DWORDs
INST3( cvtps2pi,  "cvtps2pi",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x2D))   // cvt packed singles to DWORDs
INST3( cvtss2si,  "cvtss2si",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x2D))   // cvt scalar single to DWORD
INST3( cvtpd2pi,  "cvtpd2pi",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x2D))   // cvt packed doubles to DWORDs
INST3( cvtsd2si,  "cvtsd2si",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x2D))   // cvt scalar double to DWORD
INST3( cvtps2pd,  "cvtps2pd",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x5A))   // cvt packed singles to doubles
INST3( cvtpd2ps,  "cvtpd2ps",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x5A))   // cvt packed doubles to singles
INST3( cvtss2sd,  "cvtss2sd",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x5A))   // cvt scalar single to scalar doubles
INST3( cvtsd2ss,  "cvtsd2ss",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x5A))   // cvt scalar double to scalar singles
INST3( cvtdq2ps,  "cvtdq2ps",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x5B))   // cvt packed DWORDs to singles
INST3( cvtps2dq,  "cvtps2dq",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x5B))   // cvt packed singles to DWORDs
INST3( cvttps2dq, "cvttps2dq",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0x5B))   // cvt with trunc packed singles to DWORDs
INST3( cvtpd2dq,  "cvtpd2dq",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0xE6))   // cvt packed doubles to DWORDs
INST3( cvttpd2dq, "cvttpd2dq",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0xE6))   // cvt with trunc packed doubles to DWORDs
INST3( cvtdq2pd,  "cvtdq2pd",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0xE6))   // cvt packed DWORDs to doubles

// SSE2 comparison instructions
INST3( comiss,    "comiss",     0, IUM_RD, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x2F))    // ordered compare singles
INST3( comisd,    "comisd",     0, IUM_RD, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x2F))    // ordered compare doubles
INST3( ucomiss,   "ucomiss",    0, IUM_RD, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x2E))    // unordered compare singles
INST3( ucomisd,   "ucomisd",    0, IUM_RD, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x2E))    // unordered compare doubles

// SSE2 packed single/double comparison operations.
// Note that these instructions not only compare but also overwrite the first source.
INST3( cmpps,     "cmpps",      0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0xC2))    // compare packed singles
INST3( cmppd,     "cmppd",      0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0xC2))    // compare packed doubles
INST3( cmpss,     "cmpss",      0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEFLT(0xC2))    // compare scalar singles
INST3( cmpsd,     "cmpsd",      0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0xC2))    // compare scalar doubles

//SSE2 packed integer operations
INST3( paddb,       "paddb"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFC))   // Add packed byte integers
INST3( paddw,       "paddw"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFD))   // Add packed word (16-bit) integers
INST3( paddd,       "paddd"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFE))   // Add packed double-word (32-bit) integers
INST3( paddq,       "paddq"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xD4))   // Add packed quad-word (64-bit) integers
INST3( paddsb,      "paddsb"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xEC))   // Add packed signed byte integers and saturate the results
INST3( paddsw,      "paddsw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xED))   // Add packed signed word integers and saturate the results
INST3( paddusb,     "paddusb"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDC))   // Add packed unsigned byte integers and saturate the results
INST3( paddusw,     "paddusw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDD))   // Add packed unsigned word integers and saturate the results
INST3( pavgb,       "pavgb"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xE0))   // Average of packed byte integers
INST3( pavgw,       "pavgw"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xE3))   // Average of packed word integers
INST3( psubb,       "psubb"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF8))   // Subtract packed word (16-bit) integers
INST3( psubw,       "psubw"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF9))   // Subtract packed word (16-bit) integers
INST3( psubd,       "psubd"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFA))   // Subtract packed double-word (32-bit) integers
INST3( psubq,       "psubq"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFB))   // subtract packed quad-word (64-bit) integers
INST3( pmaddwd,     "pmaddwd"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF5))   // Multiply packed signed 16-bit integers in a and b, producing intermediate signed 32-bit integers. Horizontally add adjacent pairs of intermediate 32-bit integers, and pack the results in dst
INST3( pmulhw,      "pmulhw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xE5))   // Multiply high the packed 16-bit signed integers
INST3( pmulhuw,     "pmulhuw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xE4))   // Multiply high the packed 16-bit unsigned integers
INST3( pmuludq,     "pmuludq"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF4))   // packed multiply 32-bit unsigned integers and store 64-bit result
INST3( pmullw,      "pmullw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xD5))   // Packed multiply 16 bit unsigned integers and store lower 16 bits of each result
INST3( pand,        "pand"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDB))   // Packed bit-wise AND of two xmm regs
INST3( pandn,       "pandn"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDF))   // Packed bit-wise AND NOT of two xmm regs
INST3( por,         "por"         , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xEB))   // Packed bit-wise OR of two xmm regs
INST3( pxor,        "pxor"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xEF))   // Packed bit-wise XOR of two xmm regs
INST3( psadbw,      "psadbw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF6))   // Compute the sum of absolute differences of packed unsigned 8-bit integers
INST3( psubsb,      "psubsb"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xE8))   // Subtract packed 8-bit integers in b from packed 8-bit integers in a using saturation
INST3( psubusb,     "psubusb"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xD8))   // Subtract packed unsigned 8-bit integers in b from packed unsigned 8-bit integers in a using saturation
INST3( psubsw,      "psubsw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xE9))   // Subtract packed 16-bit integers in b from packed 16-bit integers in a using saturation
INST3( psubusw,     "psubusw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xD9))   // Subtract packed unsigned 16-bit integers in b from packed unsigned 16-bit integers in a using saturation

// Note that the shift immediates share the same encoding between left and right-shift, and are distinguished by the Reg/Opcode,
// which is handled in emitxarch.cpp.
INST3( psrldq,      "psrldq"      , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x73),  BAD_CODE    )   // Shift right logical of xmm reg by given number of bytes
INST3( pslldq,      "pslldq"      , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x73),  BAD_CODE    )   // Shift left logical of xmm reg by given number of bytes
INST3( psllw,       "psllw"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x71),  PCKDBL(0xF1))   // Packed shift left logical of 16-bit integers
INST3( pslld,       "pslld"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x72),  PCKDBL(0xF2))   // Packed shift left logical of 32-bit integers
INST3( psllq,       "psllq"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x73),  PCKDBL(0xF3))   // Packed shift left logical of 64-bit integers
INST3( psrlw,       "psrlw"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x71),  PCKDBL(0xD1))   // Packed shift right logical of 16-bit integers
INST3( psrld,       "psrld"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x72),  PCKDBL(0xD2))   // Packed shift right logical of 32-bit integers
INST3( psrlq,       "psrlq"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x73),  PCKDBL(0xD3))   // Packed shift right logical of 64-bit integers
INST3( psraw,       "psraw"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x71),  PCKDBL(0xE1))   // Packed shift right arithmetic of 16-bit integers
INST3( psrad,       "psrad"       , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x72),  PCKDBL(0xE2))   // Packed shift right arithmetic of 32-bit integers

INST3( pmaxub,      "pmaxub"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDE))   // packed maximum unsigned bytes
INST3( pminub,      "pminub"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDA))   // packed minimum unsigned bytes
INST3( pmaxsw,      "pmaxsw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xEE))   // packed maximum signed words
INST3( pminsw,      "pminsw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xEA))   // packed minimum signed words
INST3( pcmpeqd,     "pcmpeqd"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x76))   // Packed compare 32-bit integers for equality
INST3( pcmpgtd,     "pcmpgtd"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x66))   // Packed compare 32-bit signed integers for greater than
INST3( pcmpeqw,     "pcmpeqw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x75))   // Packed compare 16-bit integers for equality
INST3( pcmpgtw,     "pcmpgtw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x65))   // Packed compare 16-bit signed integers for greater than
INST3( pcmpeqb,     "pcmpeqb"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x74))   // Packed compare 8-bit integers for equality
INST3( pcmpgtb,     "pcmpgtb"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x64))   // Packed compare 8-bit signed integers for greater than

INST3( pshufd,      "pshufd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x70))   // Packed shuffle of 32-bit integers
INST3( pshufhw,     "pshufhw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      SSEFLT(0x70))   // Shuffle the high words in xmm2/m128 based on the encoding in imm8 and store the result in xmm1.
INST3( pshuflw,     "pshuflw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      SSEDBL(0x70))   // Shuffle the low words in xmm2/m128 based on the encoding in imm8 and store the result in xmm1.
INST3( pextrw,      "pextrw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xC5))   // Extract 16-bit value into a r32 with zero extended to 32-bits
INST3( pinsrw,      "pinsrw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xC4))   // Insert word at index

INST3( punpckhbw,   "punpckhbw"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x68))   // Packed logical (unsigned) widen ubyte to ushort (hi)
INST3( punpcklbw,   "punpcklbw"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x60))   // Packed logical (unsigned) widen ubyte to ushort (lo)
INST3( punpckhqdq,  "punpckhqdq"  , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x6D))   // Packed logical (unsigned) widen uint to ulong (hi)
INST3( punpcklqdq,  "punpcklqdq"  , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x6C))   // Packed logical (unsigned) widen uint to ulong (lo)
INST3( punpckhwd,   "punpckhwd"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x69))   // Packed logical (unsigned) widen ushort to uint (hi)
INST3( punpcklwd,   "punpcklwd"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x61))   // Packed logical (unsigned) widen ushort to uint (lo)
INST3( unpckhpd,    "unpckhpd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x15))   // Packed logical (unsigned) widen ubyte to ushort (hi)
INST3( unpcklpd,    "unpcklpd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x14))   // Packed logical (unsigned) widen ubyte to ushort (hi)

INST3( packssdw,    "packssdw"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x6B))   // Pack (narrow) int to short with saturation
INST3( packsswb,    "packsswb"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x63))   // Pack (narrow) short to byte with saturation
INST3( packuswb,    "packuswb"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0x67))   // Pack (narrow) short to unsigned byte with saturation

INST3(LAST_SSE2_INSTRUCTION, "LAST_SSE2_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

INST3(FIRST_SSE4_INSTRUCTION, "FIRST_SSE4_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

//    enum           name           FP updmode rf wf    MR            MI        RM
INST3( dpps,         "dpps"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x40))   // Packed dot product of two float vector regs
INST3( dppd,         "dppd"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x41))   // Packed dot product of two double vector regs
INST3( insertps,     "insertps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x21))   // Insert packed single precision float value
INST3( pcmpeqq,      "pcmpeqq"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x29))   // Packed compare 64-bit integers for equality
INST3( pcmpgtq,      "pcmpgtq"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x37))   // Packed compare 64-bit integers for equality
INST3( pmulld,       "pmulld"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x40))   // Packed multiply 32 bit unsigned integers and store lower 32 bits of each result
INST3( ptest,        "ptest"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x17))   // Packed logical compare
INST3( phaddd,       "phaddd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x02))   // Packed horizontal add
INST3( pabsb,        "pabsb"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x1C))   // Packed absolute value of bytes
INST3( pabsw,        "pabsw"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x1D))   // Packed absolute value of 16-bit integers
INST3( pabsd,        "pabsd"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x1E))   // Packed absolute value of 32-bit integers
INST3( palignr,      "palignr"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x0F))   // Packed Align Right
INST3( pmaddubsw,    "pmaddubsw"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x04))   // Multiply and Add Packed Signed and Unsigned Bytes
INST3( pmulhrsw,     "pmulhrsw"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x0B))   // Packed Multiply High with Round and Scale
INST3( pshufb,       "pshufb"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x00))   // Packed Shuffle Bytes
INST3( psignb,       "psignb"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x08))   // Packed SIGN
INST3( psignw,       "psignw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x09))   // Packed SIGN
INST3( psignd,       "psignd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x0A))   // Packed SIGN
INST3( pminsb,       "pminsb"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x38))   // packed minimum signed bytes
INST3( pminsd,       "pminsd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x39))   // packed minimum 32-bit signed integers
INST3( pminuw,       "pminuw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x3A))   // packed minimum 16-bit unsigned integers
INST3( pminud,       "pminud"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x3B))   // packed minimum 32-bit unsigned integers
INST3( pmaxsb,       "pmaxsb"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x3C))   // packed maximum signed bytes
INST3( pmaxsd,       "pmaxsd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x3D))   // packed maximum 32-bit signed integers
INST3( pmaxuw,       "pmaxuw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x3E))   // packed maximum 16-bit unsigned integers
INST3( pmaxud,       "pmaxud"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x3F))   // packed maximum 32-bit unsigned integers
INST3( pmovsxbw,     "pmovsxbw"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x20))   // Packed sign extend byte to short
INST3( pmovsxbd,     "pmovsxbd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x21))   // Packed sign extend byte to int
INST3( pmovsxbq,     "pmovsxbq"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x22))   // Packed sign extend byte to long
INST3( pmovsxwd,     "pmovsxwd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x23))   // Packed sign extend short to int
INST3( pmovsxwq,     "pmovsxwq"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x24))   // Packed sign extend short to long
INST3( pmovsxdq,     "pmovsxdq"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x25))   // Packed sign extend int to long
INST3( pmovzxbw,     "pmovzxbw"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x30))   // Packed zero extend byte to short
INST3( pmovzxbd,     "pmovzxbd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x31))   // Packed zero extend byte to intg
INST3( pmovzxbq,     "pmovzxbq"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x32))   // Packed zero extend byte to lon
INST3( pmovzxwd,     "pmovzxwd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x33))   // Packed zero extend short to int
INST3( pmovzxwq,     "pmovzxwq"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x34))   // Packed zero extend short to long
INST3( pmovzxdq,     "pmovzxdq"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x35))   // Packed zero extend int to long
INST3( packusdw,     "packusdw"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x2B))   // Pack (narrow) int to unsigned short with saturation
INST3( roundps,      "roundps"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x08))   // Round packed single precision floating-point values
INST3( roundss,      "roundss"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x0A))   // Round scalar single precision floating-point values
INST3( roundpd,      "roundpd"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x09))   // Round packed double precision floating-point values
INST3( roundsd,      "roundsd"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x0B))   // Round scalar double precision floating-point values
INST3( pmuldq,       "pmuldq"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x28))   // packed multiply 32-bit signed integers and store 64-bit result
INST3( blendps,      "blendps"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x0C))   // Blend Packed Single Precision Floating-Point Values
INST3( blendvps,     "blendvps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x14))   // Variable Blend Packed Singles
INST3( blendpd,      "blendpd"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x0D))   // Blend Packed Double Precision Floating-Point Values
INST3( blendvpd,     "blendvpd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x15))   // Variable Blend Packed Doubles
INST3( pblendw,      "pblendw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x0E))   // Blend Packed Words
INST3( pblendvb,     "pblendvb"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x10))   // Variable Blend Packed Bytes
INST3( phaddw,       "phaddw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x01))   // Packed horizontal add of 16-bit integers
INST3( phsubw,       "phsubw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x05))   // Packed horizontal subtract of 16-bit integers
INST3( phsubd,       "phsubd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x06))   // Packed horizontal subtract of 32-bit integers
INST3( phaddsw,      "phaddsw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x03))   // Packed horizontal add of 16-bit integers with saturation
INST3( phsubsw,      "phsubsw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x07))   // Packed horizontal subtract of 16-bit integers with saturation
INST3( lddqu,        "lddqu"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEDBL(0xF0))  // Load Unaligned integer
INST3( movntdqa,     "movntdqa"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x2A))   // Load Double Quadword Non-Temporal Aligned Hint
INST3( movddup,      "movddup"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEDBL(0x12))  // Replicate Double FP Values
INST3( movsldup,     "movsldup"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEFLT(0x12))  // Replicate even-indexed Single FP Values
INST3( movshdup,     "movshdup"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEFLT(0x16))  // Replicate odd-indexed Single FP Values
INST3( phminposuw,   "phminposuw"  , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x41))   // Packed Horizontal Word Minimum
INST3( mpsadbw,      "mpsadbw"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x42))   // Compute Multiple Packed Sums of Absolute Difference
INST3( pinsrb,       "pinsrb"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x20))   // Insert Byte
INST3( pinsrd,       "pinsrd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x22))   // Insert Dword
INST3( pinsrq,       "pinsrq"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x22))   // Insert Qword
INST3( pextrb,       "pextrb"      , 0, IUM_WR, 0, 0, SSE3A(0x14),  BAD_CODE, BAD_CODE)      // Extract Byte
INST3( pextrd,       "pextrd"      , 0, IUM_WR, 0, 0, SSE3A(0x16),  BAD_CODE, BAD_CODE)      // Extract Dword
INST3( pextrq,       "pextrq"      , 0, IUM_WR, 0, 0, SSE3A(0x16),  BAD_CODE, BAD_CODE)      // Extract Qword
INST3( pextrw_sse41, "pextrw"      , 0, IUM_WR, 0, 0, SSE3A(0x15),  BAD_CODE, BAD_CODE)      // Extract Word
INST3( extractps,    "extractps"   , 0, IUM_WR, 0, 0, SSE3A(0x17),  BAD_CODE, BAD_CODE)      // Extract Packed Floating-Point Values

INST3(LAST_SSE4_INSTRUCTION, "LAST_SSE4_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

INST3(FIRST_AVX_INSTRUCTION, "FIRST_AVX_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)
// AVX only instructions
INST3( vbroadcastss, "broadcastss" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x18))   // Broadcast float value read from memory to entire ymm register
INST3( vbroadcastsd, "broadcastsd" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x19))   // Broadcast float value read from memory to entire ymm register
INST3( vpbroadcastb, "pbroadcastb" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x78))   // Broadcast int8 value from reg/memory to entire ymm register
INST3( vpbroadcastw, "pbroadcastw" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x79))   // Broadcast int16 value from reg/memory to entire ymm register
INST3( vpbroadcastd, "pbroadcastd" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x58))   // Broadcast int32 value from reg/memory to entire ymm register
INST3( vpbroadcastq, "pbroadcastq" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x59))   // Broadcast int64 value from reg/memory to entire ymm register
INST3( vextractf128, "extractf128" , 0, IUM_WR, 0, 0, SSE3A(0x19),  BAD_CODE, BAD_CODE)      // Extract 128-bit packed floating point values
INST3( vextracti128, "extracti128" , 0, IUM_WR, 0, 0, SSE3A(0x39),  BAD_CODE, BAD_CODE)      // Extract 128-bit packed integer values
INST3( vinsertf128,  "insertf128"  , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x18))   // Insert 128-bit packed floating point values
INST3( vinserti128,  "inserti128"  , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x38))   // Insert 128-bit packed integer values
INST3( vzeroupper,   "zeroupper"   , 0, IUM_WR, 0, 0, 0xC577F8,     BAD_CODE, BAD_CODE)      // Zero upper 128-bits of all YMM regs (includes 2-byte fixed VEX prefix)
INST3( vperm2i128,   "perm2i128"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x46))   // Permute 128-bit halves of input register
INST3( vpermq,       "permq"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x00))   // Permute 64-bit of input register
INST3( vpblendd,     "pblendd"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x02))   // Blend Packed DWORDs
INST3( vblendvps,    "blendvps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x4A))   // Variable Blend Packed Singles
INST3( vblendvpd,    "blendvpd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x4B))   // Variable Blend Packed Doubles
INST3( vpblendvb,    "pblendvb"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x4C))   // Variable Blend Packed Bytes
INST3( vtestps,      "testps"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x0E))   // Packed Bit Test
INST3( vtestpd,      "testpd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x0F))   // Packed Bit Test
INST3( vpsrlvd,      "psrlvd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x45))   // Variable Bit Shift Right Logical
INST3( vpsrlvq,      "psrlvq"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x45))   // Variable Bit Shift Right Logical
INST3( vpsravd,      "psravd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x46))   // Variable Bit Shift Right Arithmetic
INST3( vpsllvd,      "psllvd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x47))   // Variable Bit Shift Left Logical
INST3( vpsllvq,      "psllvq"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x47))   // Variable Bit Shift Left Logical
INST3( vpermilps,    "permilps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x04))   // Permute In-Lane of Quadruples of Single-Precision Floating-Point Values
INST3( vpermilpd,    "permilpd"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x05))   // Permute In-Lane of Quadruples of Double-Precision Floating-Point Values
INST3( vpermilpsvar, "permilpsvar" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x0C))   // Permute In-Lane of Quadruples of Single-Precision Floating-Point Values
INST3( vpermilpdvar, "permilpdvar" , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x0D))   // Permute In-Lane of Quadruples of Double-Precision Floating-Point Values
INST3( vperm2f128,   "perm2f128"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x06))   // Permute Floating-Point Values
INST3(vbroadcastf128,"broadcastf128",0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x1A))   // Broadcast packed float values read from memory to entire ymm register
INST3(vbroadcasti128,"broadcasti128",0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x5A))   // Broadcast packed integer values read from memory to entire ymm register
INST3(vmaskmovps,    "maskmovps"    ,0, IUM_WR, 0, 0, SSE38(0x2E),  BAD_CODE, SSE38(0x2C))   // Conditional SIMD Packed Single-Precision Floating-Point Loads and Stores
INST3(vmaskmovpd,    "maskmovpd"    ,0, IUM_WR, 0, 0, SSE38(0x2F),  BAD_CODE, SSE38(0x2D))   // Conditional SIMD Packed Double-Precision Floating-Point Loads and Stores

INST3(FIRST_FMA_INSTRUCTION, "FIRST_FMA_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)
//    enum            name             FP updmode rf wf MR            MI        RM
INST3(vfmadd132pd,    "fmadd132pd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x98))   // Fused Multiply-Add of Packed Double-Precision Floating-Point Values
INST3(vfmadd213pd,    "fmadd213pd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA8))   //
INST3(vfmadd231pd,    "fmadd231pd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB8))   //
INST3(vfmadd132ps,    "fmadd132ps",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x98))   // Fused Multiply-Add of Packed Single-Precision Floating-Point Values
INST3(vfmadd213ps,    "fmadd213ps",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA8))   //
INST3(vfmadd231ps,    "fmadd231ps",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB8))   //
INST3(vfmadd132sd,    "fmadd132sd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x99))   // Fused Multiply-Add of Scalar Double-Precision Floating-Point Values
INST3(vfmadd213sd,    "fmadd213sd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA9))   //
INST3(vfmadd231sd,    "fmadd231sd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB9))   //
INST3(vfmadd132ss,    "fmadd132ss",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x99))   // Fused Multiply-Add of Scalar Single-Precision Floating-Point Values
INST3(vfmadd213ss,    "fmadd213ss",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA9))   //
INST3(vfmadd231ss,    "fmadd231ss",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB9))   //
INST3(vfmaddsub132pd, "fmaddsub132pd", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x96))   // Fused Multiply-Alternating Add/Subtract of Packed Double-Precision Floating-Point Values
INST3(vfmaddsub213pd, "fmaddsub213pd", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA6))   //
INST3(vfmaddsub231pd, "fmaddsub231pd", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB6))   //
INST3(vfmaddsub132ps, "fmaddsub132ps", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x96))   // Fused Multiply-Alternating Add/Subtract of Packed Single-Precision Floating-Point Values
INST3(vfmaddsub213ps, "fmaddsub213ps", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA6))   //
INST3(vfmaddsub231ps, "fmaddsub231ps", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB6))   //
INST3(vfmsubadd132pd, "fmsubadd132pd", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x97))   // Fused Multiply-Alternating Subtract/Add of Packed Double-Precision Floating-Point Values
INST3(vfmsubadd213pd, "fmsubadd213pd", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA7))   //
INST3(vfmsubadd231pd, "fmsubadd231pd", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB7))   //
INST3(vfmsubadd132ps, "fmsubadd132ps", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x97))   // Fused Multiply-Alternating Subtract/Add of Packed Single-Precision Floating-Point Values
INST3(vfmsubadd213ps, "fmsubadd213ps", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xA7))   //
INST3(vfmsubadd231ps, "fmsubadd231ps", 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xB7))   //
INST3(vfmsub132pd,    "fmsub132pd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9A))   // Fused Multiply-Subtract of Packed Double-Precision Floating-Point Values
INST3(vfmsub213pd,    "fmsub213pd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAA))   //
INST3(vfmsub231pd,    "fmsub231pd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBA))   //
INST3(vfmsub132ps,    "fmsub132ps",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9A))   // Fused Multiply-Subtract of Packed Single-Precision Floating-Point Values
INST3(vfmsub213ps,    "fmsub213ps",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAA))   //
INST3(vfmsub231ps,    "fmsub231ps",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBA))   //
INST3(vfmsub132sd,    "fmsub132sd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9B))   // Fused Multiply-Subtract of Scalar Double-Precision Floating-Point Values
INST3(vfmsub213sd,    "fmsub213sd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAB))   //
INST3(vfmsub231sd,    "fmsub231sd",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBB))   //
INST3(vfmsub132ss,    "fmsub132ss",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9B))   // Fused Multiply-Subtract of Scalar Single-Precision Floating-Point Values
INST3(vfmsub213ss,    "fmsub213ss",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAB))   //
INST3(vfmsub231ss,    "fmsub231ss",    0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBB))   //
INST3(vfnmadd132pd,   "fmnadd132pd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9C))   // Fused Negative Multiply-Add of Packed Double-Precision Floating-Point Values
INST3(vfnmadd213pd,   "fmnadd213pd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAC))   //
INST3(vfnmadd231pd,   "fmnadd231pd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBC))   //
INST3(vfnmadd132ps,   "fmnadd132ps",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9C))   // Fused Negative Multiply-Add of Packed Single-Precision Floating-Point Values
INST3(vfnmadd213ps,   "fmnadd213ps",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAC))   //
INST3(vfnmadd231ps,   "fmnadd231ps",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBC))   //
INST3(vfnmadd132sd,   "fmnadd132sd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9D))   // Fused Negative Multiply-Add of Scalar Double-Precision Floating-Point Values
INST3(vfnmadd213sd,   "fmnadd213sd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAD))   //
INST3(vfnmadd231sd,   "fmnadd231sd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBD))   //
INST3(vfnmadd132ss,   "fmnadd132ss",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9D))   // Fused Negative Multiply-Add of Scalar Single-Precision Floating-Point Values
INST3(vfnmadd213ss,   "fmnadd213ss",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAD))   //
INST3(vfnmadd231ss,   "fmnadd231ss",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBD))   //
INST3(vfnmsub132pd,   "fmnsub132pd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9E))   // Fused Negative Multiply-Subtract of Packed Double-Precision Floating-Point Values
INST3(vfnmsub213pd,   "fmnsub213pd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAE))   //
INST3(vfnmsub231pd,   "fmnsub231pd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBE))   //
INST3(vfnmsub132ps,   "fmnsub132ps",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9E))   // Fused Negative Multiply-Subtract of Packed Single-Precision Floating-Point Values
INST3(vfnmsub213ps,   "fmnsub213ps",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAE))   //
INST3(vfnmsub231ps,   "fmnsub231ps",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBE))   //
INST3(vfnmsub132sd,   "fmnsub132sd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9F))   // Fused Negative Multiply-Subtract of Scalar Double-Precision Floating-Point Values
INST3(vfnmsub213sd,   "fmnsub213sd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAF))   //
INST3(vfnmsub231sd,   "fmnsub231sd",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBF))   //
INST3(vfnmsub132ss,   "fmnsub132ss",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x9F))   // Fused Negative Multiply-Subtract of Scalar Single-Precision Floating-Point Values
INST3(vfnmsub213ss,   "fmnsub213ss",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xAF))   //
INST3(vfnmsub231ss,   "fmnsub231ss",   0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xBF))   //
INST3(LAST_FMA_INSTRUCTION, "LAST_FMA_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

// BMI1
INST3(FIRST_BMI_INSTRUCTION, "FIRST_BMI_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)
INST3(andn,           "andn",          0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xF2))   // Logical AND NOT
INST3(blsi,           "blsi",          0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xF3))   // Extract Lowest Set Isolated Bit
INST3(blsmsk,         "blsmsk",        0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xF3))   // Get Mask Up to Lowest Set Bit
INST3(blsr,           "blsr",          0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xF3))   // Reset Lowest Set Bit

// BMI2
INST3(pdep,           "pdep",          0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xF5))   // Parallel Bits Deposit
INST3(pext,           "pext",          0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0xF5))   // Parallel Bits Extract
INST3(LAST_BMI_INSTRUCTION, "LAST_BMI_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

INST3(LAST_AVX_INSTRUCTION, "LAST_AVX_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

// Scalar instructions in SSE4.2
INST3( crc32,        "crc32"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PACK4(0xF2, 0x0F, 0x38, 0xF0))

// BMI1
INST3( tzcnt,        "tzcnt"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEFLT(0xBC))    // Count the Number of Trailing Zero Bits

// LZCNT
INST3( lzcnt,        "lzcnt"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEFLT(0xBD))

// POPCNT
INST3( popcnt,       "popcnt"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEFLT(0xB8))

//    enum     name            FP  updmode rf wf R/M,R/M[reg]  R/M,icon

INST2(ret    , "ret"          , 0, IUM_RD, 0, 0, 0x0000C3, 0x0000C2)
INST2(loop   , "loop"         , 0, IUM_RD, 0, 0, BAD_CODE, 0x0000E2)
INST2(call   , "call"         , 0, IUM_RD, 0, 1, 0x0010FF, 0x0000E8)

INST2(rol    , "rol"          , 0, IUM_RW, 0, 1, 0x0000D2, BAD_CODE)
INST2(rol_1  , "rol"          , 0, IUM_RW, 0, 1, 0x0000D0, 0x0000D0)
INST2(rol_N  , "rol"          , 0, IUM_RW, 0, 1, 0x0000C0, 0x0000C0)
INST2(ror    , "ror"          , 0, IUM_RW, 0, 1, 0x0008D2, BAD_CODE)
INST2(ror_1  , "ror"          , 0, IUM_RW, 0, 1, 0x0008D0, 0x0008D0)
INST2(ror_N  , "ror"          , 0, IUM_RW, 0, 1, 0x0008C0, 0x0008C0)

INST2(rcl    , "rcl"          , 0, IUM_RW, 1, 1, 0x0010D2, BAD_CODE)
INST2(rcl_1  , "rcl"          , 0, IUM_RW, 1, 1, 0x0010D0, 0x0010D0)
INST2(rcl_N  , "rcl"          , 0, IUM_RW, 1, 1, 0x0010C0, 0x0010C0)
INST2(rcr    , "rcr"          , 0, IUM_RW, 1, 1, 0x0018D2, BAD_CODE)
INST2(rcr_1  , "rcr"          , 0, IUM_RW, 1, 1, 0x0018D0, 0x0018D0)
INST2(rcr_N  , "rcr"          , 0, IUM_RW, 1, 1, 0x0018C0, 0x0018C0)
INST2(shl    , "shl"          , 0, IUM_RW, 0, 1, 0x0020D2, BAD_CODE)
INST2(shl_1  , "shl"          , 0, IUM_RW, 0, 1, 0x0020D0, 0x0020D0)
INST2(shl_N  , "shl"          , 0, IUM_RW, 0, 1, 0x0020C0, 0x0020C0)
INST2(shr    , "shr"          , 0, IUM_RW, 0, 1, 0x0028D2, BAD_CODE)
INST2(shr_1  , "shr"          , 0, IUM_RW, 0, 1, 0x0028D0, 0x0028D0)
INST2(shr_N  , "shr"          , 0, IUM_RW, 0, 1, 0x0028C0, 0x0028C0)
INST2(sar    , "sar"          , 0, IUM_RW, 0, 1, 0x0038D2, BAD_CODE)
INST2(sar_1  , "sar"          , 0, IUM_RW, 0, 1, 0x0038D0, 0x0038D0)
INST2(sar_N  , "sar"          , 0, IUM_RW, 0, 1, 0x0038C0, 0x0038C0)


//    enum     name            FP  updmode rf wf R/M,R/M[reg]

INST1(r_movsb, "rep movsb"    , 0, IUM_RD, 0, 0, 0x00A4F3)
INST1(r_movsd, "rep movsd"    , 0, IUM_RD, 0, 0, 0x00A5F3)
#if defined(_TARGET_AMD64_)
INST1(r_movsq, "rep movsq"    , 0, IUM_RD, 0, 0, 0xF3A548)
#endif // defined(_TARGET_AMD64_)
INST1(movsb  , "movsb"        , 0, IUM_RD, 0, 0, 0x0000A4)
INST1(movsd  , "movsd"        , 0, IUM_RD, 0, 0, 0x0000A5)
#if defined(_TARGET_AMD64_)
INST1(movsq, "movsq"          , 0, IUM_RD, 0, 0, 0x00A548)
#endif // defined(_TARGET_AMD64_)

INST1(r_stosb, "rep stosb"    , 0, IUM_RD, 0, 0, 0x00AAF3)
INST1(r_stosd, "rep stosd"    , 0, IUM_RD, 0, 0, 0x00ABF3)
#if defined(_TARGET_AMD64_)
INST1(r_stosq, "rep stosq"    , 0, IUM_RD, 0, 0, 0xF3AB48)
#endif // defined(_TARGET_AMD64_)
INST1(stosb,   "stosb"        , 0, IUM_RD, 0, 0, 0x0000AA)
INST1(stosd,   "stosd"        , 0, IUM_RD, 0, 0, 0x0000AB)
#if defined(_TARGET_AMD64_)
INST1(stosq,   "stosq"        , 0, IUM_RD, 0, 0, 0x00AB48)
#endif // defined(_TARGET_AMD64_)

INST1(int3   , "int3"         , 0, IUM_RD, 0, 0, 0x0000CC)
INST1(nop    , "nop"          , 0, IUM_RD, 0, 0, 0x000090)
INST1(lock   , "lock"         , 0, IUM_RD, 0, 0, 0x0000F0)
INST1(leave  , "leave"        , 0, IUM_RD, 0, 0, 0x0000C9)


INST1(neg    , "neg"          , 0, IUM_RW, 0, 1, 0x0018F6)
INST1(not    , "not"          , 0, IUM_RW, 0, 1, 0x0010F6)

INST1(cdq    , "cdq"          , 0, IUM_RD, 0, 1, 0x000099)
INST1(idiv   , "idiv"         , 0, IUM_RD, 0, 1, 0x0038F6)
INST1(imulEAX, "imul"         , 0, IUM_RD, 0, 1, 0x0028F6) // edx:eax = eax*op1
INST1(div    , "div"          , 0, IUM_RD, 0, 1, 0x0030F6)
INST1(mulEAX , "mul"          , 0, IUM_RD, 0, 1, 0x0020F6)

INST1(sahf   , "sahf"         , 0, IUM_RD, 0, 1, 0x00009E)

INST1(xadd   , "xadd"         , 0, IUM_RW, 0, 1, 0x0F00C0)
INST1(cmpxchg, "cmpxchg"      , 0, IUM_RW, 0, 1, 0x0F00B0)

INST1(shld   , "shld"         , 0, IUM_RW, 0, 1, 0x0F00A4)
INST1(shrd   , "shrd"         , 0, IUM_RW, 0, 1, 0x0F00AC)

// For RyuJIT/x86, we follow the x86 calling convention that requires
// us to return floating point value on the x87 FP stack, so we need
// these instructions regardless of whether we're using full stack fp.
#ifdef _TARGET_X86_
INST1(fld    , "fld"          , 1, IUM_WR, 0, 0, 0x0000D9)
INST1(fstp   , "fstp"         , 1, IUM_WR, 0, 0, 0x0018D9)
#endif // _TARGET_X86

INST1(seto   , "seto"         , 0, IUM_WR, 1, 0, 0x0F0090)
INST1(setno  , "setno"        , 0, IUM_WR, 1, 0, 0x0F0091)
INST1(setb   , "setb"         , 0, IUM_WR, 1, 0, 0x0F0092)
INST1(setae  , "setae"        , 0, IUM_WR, 1, 0, 0x0F0093)
INST1(sete   , "sete"         , 0, IUM_WR, 1, 0, 0x0F0094)
INST1(setne  , "setne"        , 0, IUM_WR, 1, 0, 0x0F0095)
INST1(setbe  , "setbe"        , 0, IUM_WR, 1, 0, 0x0F0096)
INST1(seta   , "seta"         , 0, IUM_WR, 1, 0, 0x0F0097)
INST1(sets   , "sets"         , 0, IUM_WR, 1, 0, 0x0F0098)
INST1(setns  , "setns"        , 0, IUM_WR, 1, 0, 0x0F0099)
INST1(setpe  , "setpe"        , 0, IUM_WR, 1, 0, 0x0F009A)
INST1(setpo  , "setpo"        , 0, IUM_WR, 1, 0, 0x0F009B)
INST1(setl   , "setl"         , 0, IUM_WR, 1, 0, 0x0F009C)
INST1(setge  , "setge"        , 0, IUM_WR, 1, 0, 0x0F009D)
INST1(setle  , "setle"        , 0, IUM_WR, 1, 0, 0x0F009E)
INST1(setg   , "setg"         , 0, IUM_WR, 1, 0, 0x0F009F)

#ifdef _TARGET_AMD64_
// A jump with rex prefix. This is used for register indirect
// tail calls.
INST1(rex_jmp, "rex.jmp"      , 0, IUM_RD, 0, 0, 0x0020FE)
#endif

INST1(i_jmp  , "jmp"          , 0, IUM_RD, 0, 0, 0x0020FE)

INST0(jmp    , "jmp"          , 0, IUM_RD, 0, 0, 0x0000EB)
INST0(jo     , "jo"           , 0, IUM_RD, 1, 0, 0x000070)
INST0(jno    , "jno"          , 0, IUM_RD, 1, 0, 0x000071)
INST0(jb     , "jb"           , 0, IUM_RD, 1, 0, 0x000072)
INST0(jae    , "jae"          , 0, IUM_RD, 1, 0, 0x000073)
INST0(je     , "je"           , 0, IUM_RD, 1, 0, 0x000074)
INST0(jne    , "jne"          , 0, IUM_RD, 1, 0, 0x000075)
INST0(jbe    , "jbe"          , 0, IUM_RD, 1, 0, 0x000076)
INST0(ja     , "ja"           , 0, IUM_RD, 1, 0, 0x000077)
INST0(js     , "js"           , 0, IUM_RD, 1, 0, 0x000078)
INST0(jns    , "jns"          , 0, IUM_RD, 1, 0, 0x000079)
INST0(jpe    , "jpe"          , 0, IUM_RD, 1, 0, 0x00007A)
INST0(jpo    , "jpo"          , 0, IUM_RD, 1, 0, 0x00007B)
INST0(jl     , "jl"           , 0, IUM_RD, 1, 0, 0x00007C)
INST0(jge    , "jge"          , 0, IUM_RD, 1, 0, 0x00007D)
INST0(jle    , "jle"          , 0, IUM_RD, 1, 0, 0x00007E)
INST0(jg     , "jg"           , 0, IUM_RD, 1, 0, 0x00007F)

INST0(l_jmp  , "jmp"          , 0, IUM_RD, 0, 0, 0x0000E9)
INST0(l_jo   , "jo"           , 0, IUM_RD, 1, 0, 0x00800F)
INST0(l_jno  , "jno"          , 0, IUM_RD, 1, 0, 0x00810F)
INST0(l_jb   , "jb"           , 0, IUM_RD, 1, 0, 0x00820F)
INST0(l_jae  , "jae"          , 0, IUM_RD, 1, 0, 0x00830F)
INST0(l_je   , "je"           , 0, IUM_RD, 1, 0, 0x00840F)
INST0(l_jne  , "jne"          , 0, IUM_RD, 1, 0, 0x00850F)
INST0(l_jbe  , "jbe"          , 0, IUM_RD, 1, 0, 0x00860F)
INST0(l_ja   , "ja"           , 0, IUM_RD, 1, 0, 0x00870F)
INST0(l_js   , "js"           , 0, IUM_RD, 1, 0, 0x00880F)
INST0(l_jns  , "jns"          , 0, IUM_RD, 1, 0, 0x00890F)
INST0(l_jpe  , "jpe"          , 0, IUM_RD, 1, 0, 0x008A0F)
INST0(l_jpo  , "jpo"          , 0, IUM_RD, 1, 0, 0x008B0F)
INST0(l_jl   , "jl"           , 0, IUM_RD, 1, 0, 0x008C0F)
INST0(l_jge  , "jge"          , 0, IUM_RD, 1, 0, 0x008D0F)
INST0(l_jle  , "jle"          , 0, IUM_RD, 1, 0, 0x008E0F)
INST0(l_jg   , "jg"           , 0, IUM_RD, 1, 0, 0x008F0F)

INST0(align  , "align"        , 0, IUM_RD, 0, 0, BAD_CODE)

/*****************************************************************************/
#undef  INST0
#undef  INST1
#undef  INST2
#undef  INST3
#undef  INST4
#undef  INST5
/*****************************************************************************/

// clang-format on
