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
INST3( movq,        "movq"        , 0, IUM_WR, 0, 0, PCKDBL(0xD6), BAD_CODE, SSEFLT(0x7E))
INST3( movsdsse2,   "movsd"       , 0, IUM_WR, 0, 0, SSEDBL(0x11), BAD_CODE, SSEDBL(0x10))

INST3( punpckldq,   "punpckldq"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0x62))

INST3( xorps,       "xorps"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0x57)) // XOR packed singles

INST3( cvttsd2si,   "cvttsd2si"   , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSEDBL(0x2C)) // cvt with trunc scalar double to signed DWORDs

#ifndef LEGACY_BACKEND
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

INST3( shufps,      "shufps"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKFLT(0xC6))
INST3( shufpd,      "shufpd"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, PCKDBL(0xC6))
       
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
INST3( sqrtsd, "sqrtsd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, SSEDBL(0x51))    // Sqrt of a scalar double
INST3( sqrtps, "sqrtps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x51))    // Sqrt of a packed float
INST3( sqrtpd, "sqrtpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x51))    // Sqrt of a packed double
INST3( andnps, "andnps", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x55))    // And-Not packed singles
INST3( andnpd, "andnpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x55))    // And-Not packed doubles
INST3( orps,   "orps",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x56))    // Or packed singles
INST3( orpd,   "orpd",   0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x56))    // Or packed doubles
INST3( haddpd, "haddpd", 0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x7C))    // Horizontal add packed doubles

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
INST3( ucomiss,   "ucomiss",    0, IUM_RD, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0x2E))    // unordered compare singles
INST3( ucomisd,   "ucomisd",    0, IUM_RD, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0x2E))    // unordered compare doubles

// SSE2 packed single/double comparison operations.
// Note that these instructions not only compare but also overwrite the first source.
INST3( cmpps,     "cmpps",      0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKFLT(0xC2))    // compare packed singles
INST3( cmppd,     "cmppd",      0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, PCKDBL(0xC2))    // compare packed doubles

//SSE2 packed integer operations
INST3( paddb,       "paddb"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFC))   // Add packed byte integers
INST3( paddw,       "paddw"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFD))   // Add packed word (16-bit) integers
INST3( paddd,       "paddd"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFE))   // Add packed double-word (32-bit) integers
INST3( paddq,       "paddq"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xD4))   // Add packed quad-word (64-bit) integers
INST3( psubb,       "psubb"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF8))   // Subtract packed word (16-bit) integers
INST3( psubw,       "psubw"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF9))   // Subtract packed word (16-bit) integers
INST3( psubd,       "psubd"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFA))   // Subtract packed double-word (32-bit) integers
INST3( psubq,       "psubq"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xFB))   // subtract packed quad-word (64-bit) integers
INST3( pmuludq,     "pmuludq"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xF4))   // packed multiply 32-bit unsigned integers and store 64-bit result
INST3( pmullw,      "pmullw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xD5))   // Packed multiply 16 bit unsigned integers and store lower 16 bits of each result
INST3( pand,        "pand"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDB))   // Packed bit-wise AND of two xmm regs
INST3( pandn,       "pandn"       , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xDF))   // Packed bit-wise AND NOT of two xmm regs
INST3( por,         "por"         , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xEB))   // Packed bit-wise OR of two xmm regs
INST3( pxor,        "pxor"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xEF))   // Packed bit-wise XOR of two xmm regs
INST3( psrldq,      "psrldq"      , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x73),  BAD_CODE    )   // Shift right logical of xmm reg by given number of bytes
INST3( pslldq,      "pslldq"      , 0, IUM_WR, 0, 0, BAD_CODE,     PCKDBL(0x73),  BAD_CODE    )   // Shift left logical of xmm reg by given number of bytes
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
INST3( pextrw,      "pextrw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xC5))   // Extract 16-bit value into a r32 with zero extended to 32-bits
INST3( pinsrw,      "pinsrw"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE,      PCKDBL(0xC4))   // packed insert word

#endif // !LEGACY_BACKEND
INST3(LAST_SSE2_INSTRUCTION, "LAST_SSE2_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)

#ifndef LEGACY_BACKEND
INST3(FIRST_SSE4_INSTRUCTION, "FIRST_SSE4_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)
// Most of the following instructions should be included in the method Is4ByteAVXInstruction()
//    enum           name           FP updmode rf wf    MR            MI        RM
INST3( dpps,         "dpps"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x40))   // Packed bit-wise AND NOT of two xmm regs
INST3( dppd,         "dppd"        , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x41))   // Packed bit-wise AND NOT of two xmm regs
INST3( insertps,     "insertps"    , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x21))   // Insert packed single precision float value
INST3( pcmpeqq,      "pcmpeqq"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x29))   // Packed compare 64-bit integers for equality
INST3( pcmpgtq,      "pcmpgtq"     , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x37))   // Packed compare 64-bit integers for equality
INST3( pmulld,       "pmulld"      , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE38(0x40))   // Packed multiply 32 bit unsigned integers and store lower 32 bits of each result
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
INST3( vinsertf128,  "insertf128"  , 0, IUM_WR, 0, 0, BAD_CODE,     BAD_CODE, SSE3A(0x18))   // Insert 128-bit packed floating point values
INST3( vzeroupper,   "zeroupper"   , 0, IUM_WR, 0, 0, 0xC577F8,     BAD_CODE, BAD_CODE)      // Zero upper 128-bits of all YMM regs (includes 2-byte fixed VEX prefix)

INST3(LAST_AVX_INSTRUCTION, "LAST_AVX_INSTRUCTION",  0, IUM_WR, 0, 0, BAD_CODE, BAD_CODE, BAD_CODE)
#endif // !LEGACY_BACKEND
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
#ifndef LEGACY_BACKEND
INST1(r_movsq, "rep movsq"    , 0, IUM_RD, 0, 0, 0xF3A548)
#endif // !LEGACY_BACKEND
INST1(movsb  , "movsb"        , 0, IUM_RD, 0, 0, 0x0000A4)
INST1(movsd  , "movsd"        , 0, IUM_RD, 0, 0, 0x0000A5)
#ifndef LEGACY_BACKEND
INST1(movsq, "movsq"          , 0, IUM_RD, 0, 0, 0x00A548)
#endif // !LEGACY_BACKEND

INST1(r_stosb, "rep stosb"    , 0, IUM_RD, 0, 0, 0x00AAF3)
INST1(r_stosd, "rep stosd"    , 0, IUM_RD, 0, 0, 0x00ABF3)
#ifndef LEGACY_BACKEND
INST1(r_stosq, "rep stosq"    , 0, IUM_RD, 0, 0, 0xF3AB48)
#endif // !LEGACY_BACKEND
INST1(stosb,   "stosb"        , 0, IUM_RD, 0, 0, 0x0000AA)
INST1(stosd,   "stosd"        , 0, IUM_RD, 0, 0, 0x0000AB)
#ifndef LEGACY_BACKEND
INST1(stosq,   "stosq"        , 0, IUM_RD, 0, 0, 0x00AB48)
#endif // !LEGACY_BACKEND

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

#if FEATURE_STACK_FP_X87
INST1(fnstsw , "fnstsw"       , 1, IUM_WR, 1, 0, 0x0020DF)
INST1(fcom   , "fcom"         , 1, IUM_RD, 0, 1, 0x0010D8)
INST1(fcomp  , "fcomp"        , 1, IUM_RD, 0, 1, 0x0018D8)
INST1(fcompp , "fcompp"       , 1, IUM_RD, 0, 1, 0x00D9DE)
INST1(fcomi  , "fcomi"        , 1, IUM_RD, 0, 1, 0x00F0DB)
INST1(fcomip , "fcomip"       , 1, IUM_RD, 0, 1, 0x00F0DF)

INST1(fchs   , "fchs"         , 1, IUM_RW, 0, 1, 0x00E0D9)
INST1(fabs   , "fabs"         , 1, IUM_RW, 0, 1, 0x00E1D9)
INST1(fsin   , "fsin"         , 1, IUM_RW, 0, 1, 0x00FED9)
INST1(fcos   , "fcos"         , 1, IUM_RW, 0, 1, 0x00FFD9)
INST1(fsqrt  , "fsqrt"        , 1, IUM_RW, 0, 1, 0x00FAD9)
INST1(fldl2e , "fldl2e"       , 1, IUM_RW, 0, 1, 0x00EAD9)
INST1(frndint, "frndint"      , 1, IUM_RW, 0, 1, 0x00FCD9)
INST1(f2xm1  , "f2xm1"        , 1, IUM_RW, 0, 1, 0x00F0D9)
INST1(fscale , "fscale"       , 1, IUM_RW, 0, 1, 0x00FDD9)

INST1(fld1   , "fld1"         , 1, IUM_WR, 0, 0, 0x00E8D9)
INST1(fldz   , "fldz"         , 1, IUM_WR, 0, 0, 0x00EED9)
INST1(fst    , "fst"          , 1, IUM_WR, 0, 0, 0x0010D9)

INST1(fadd   , "fadd"         , 1, IUM_RW, 0, 0, 0x0000D8)
INST1(faddp  , "faddp"        , 1, IUM_RW, 0, 0, 0x0000DA)
INST1(fsub   , "fsub"         , 1, IUM_RW, 0, 0, 0x0020D8)
INST1(fsubp  , "fsubp"        , 1, IUM_RW, 0, 0, 0x0028DA)
INST1(fsubr  , "fsubr"        , 1, IUM_RW, 0, 0, 0x0028D8)
INST1(fsubrp , "fsubrp"       , 1, IUM_RW, 0, 0, 0x0020DA)
INST1(fmul   , "fmul"         , 1, IUM_RW, 0, 0, 0x0008D8)
INST1(fmulp  , "fmulp"        , 1, IUM_RW, 0, 0, 0x0008DA)
INST1(fdiv   , "fdiv"         , 1, IUM_RW, 0, 0, 0x0030D8)
INST1(fdivp  , "fdivp"        , 1, IUM_RW, 0, 0, 0x0038DA)
INST1(fdivr  , "fdivr"        , 1, IUM_RW, 0, 0, 0x0038D8)
INST1(fdivrp , "fdivrp"       , 1, IUM_RW, 0, 0, 0x0030DA)

INST1(fxch   , "fxch"         , 1, IUM_RW, 0, 0, 0x00C8D9)
INST1(fprem  , "fprem"        , 0, IUM_RW, 0, 1, 0x00F8D9)

INST1(fild   , "fild"         , 1, IUM_RD, 0, 0, 0x0000DB)
INST1(fildl  , "fild"         , 1, IUM_RD, 0, 0, 0x0028DB)
INST1(fistp  , "fistp"        , 1, IUM_WR, 0, 0, 0x0018DB)
INST1(fistpl , "fistp"        , 1, IUM_WR, 0, 0, 0x0038DB)

INST1(fldcw  , "fldcw"        , 1, IUM_RD, 0, 0, 0x0028D9)
INST1(fnstcw , "fnstcw"       , 1, IUM_WR, 0, 0, 0x0038D9)
#endif // FEATURE_STACK_FP_X87

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
