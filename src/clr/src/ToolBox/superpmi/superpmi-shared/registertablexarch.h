//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// RegisterTableXarch.h - X macro table for x86/AMD64 registers, for use with DISX86
//----------------------------------------------------------

#ifndef REGDEF
#error Must define REGDEF macro before including this file
#endif

/*
REGDEF(msdisID,                  name)
*/

// 32 bit general purpose registers
REGDEF(DISX86::REGA::regaEax, "eax")
REGDEF(DISX86::REGA::regaEcx, "ecx")
REGDEF(DISX86::REGA::regaEdx, "edx")
REGDEF(DISX86::REGA::regaEbx, "ebx")
REGDEF(DISX86::REGA::regaEsp, "esp")
REGDEF(DISX86::REGA::regaEbp, "ebp")
REGDEF(DISX86::REGA::regaEsi, "esi")
REGDEF(DISX86::REGA::regaEdi, "edi")
REGDEF(DISX86::REGA::regaR8d, "r8d")
REGDEF(DISX86::REGA::regaR9d, "r9d")
REGDEF(DISX86::REGA::regaR10d, "r10d")
REGDEF(DISX86::REGA::regaR11d, "r11d")
REGDEF(DISX86::REGA::regaR12d, "r12d")
REGDEF(DISX86::REGA::regaR13d, "r13d")
REGDEF(DISX86::REGA::regaR14d, "r14d")
REGDEF(DISX86::REGA::regaR15d, "r15d")

// 64 bit general purpose registers
REGDEF(DISX86::REGA::regaRax, "rax")
REGDEF(DISX86::REGA::regaRcx, "rcx")
REGDEF(DISX86::REGA::regaRdx, "rdx")
REGDEF(DISX86::REGA::regaRbx, "rbx")
REGDEF(DISX86::REGA::regaRsp, "rsp")
REGDEF(DISX86::REGA::regaRbp, "rbp")
REGDEF(DISX86::REGA::regaRsi, "rsi")
REGDEF(DISX86::REGA::regaRdi, "rdi")
REGDEF(DISX86::REGA::regaR8, "r8")
REGDEF(DISX86::REGA::regaR9, "r9")
REGDEF(DISX86::REGA::regaR10, "r10")
REGDEF(DISX86::REGA::regaR11, "r11")
REGDEF(DISX86::REGA::regaR12, "r12")
REGDEF(DISX86::REGA::regaR13, "r13")
REGDEF(DISX86::REGA::regaR14, "r14")
REGDEF(DISX86::REGA::regaR15, "r15")

// 16 bit general purpose registers
REGDEF(DISX86::REGA::regaAx, "ax")
REGDEF(DISX86::REGA::regaCx, "cx")
REGDEF(DISX86::REGA::regaDx, "dx")
REGDEF(DISX86::REGA::regaBx, "bx")
REGDEF(DISX86::REGA::regaSp, "sp")
REGDEF(DISX86::REGA::regaBp, "bp")
REGDEF(DISX86::REGA::regaSi, "si")
REGDEF(DISX86::REGA::regaDi, "di")
REGDEF(DISX86::REGA::regaR8w, "r8w")
REGDEF(DISX86::REGA::regaR9w, "r9w")
REGDEF(DISX86::REGA::regaR10w, "r10w")
REGDEF(DISX86::REGA::regaR11w, "r11w")
REGDEF(DISX86::REGA::regaR12w, "r12w")
REGDEF(DISX86::REGA::regaR13w, "r13w")
REGDEF(DISX86::REGA::regaR14w, "r14w")
REGDEF(DISX86::REGA::regaR15w, "r15w")

// 8 bit general purpose registers
REGDEF(DISX86::REGA::regaAl, "al")
REGDEF(DISX86::REGA::regaCl, "cl")
REGDEF(DISX86::REGA::regaDl, "dl")
REGDEF(DISX86::REGA::regaBl, "bl")
REGDEF(DISX86::REGA::regaSpl, "spl")
REGDEF(DISX86::REGA::regaBpl, "bpl")
REGDEF(DISX86::REGA::regaSil, "sil")
REGDEF(DISX86::REGA::regaDil, "dil")
REGDEF(DISX86::REGA::regaR8b, "r8b")
REGDEF(DISX86::REGA::regaR9b, "r9b")
REGDEF(DISX86::REGA::regaR10b, "r10b")
REGDEF(DISX86::REGA::regaR11b, "r11b")
REGDEF(DISX86::REGA::regaR12b, "r12b")
REGDEF(DISX86::REGA::regaR13b, "r13b")
REGDEF(DISX86::REGA::regaR14b, "r14b")
REGDEF(DISX86::REGA::regaR15b, "r15b")

// 8 bit general purpose registers
REGDEF(DISX86::REGA::regaAh, "ah")
REGDEF(DISX86::REGA::regaCh, "ch")
REGDEF(DISX86::REGA::regaDh, "dh")
REGDEF(DISX86::REGA::regaBh, "bh")

// x87 floating point stack
REGDEF(DISX86::REGA::regaSt0, "st0")
REGDEF(DISX86::REGA::regaSt1, "st1")
REGDEF(DISX86::REGA::regaSt2, "st2")
REGDEF(DISX86::REGA::regaSt3, "st3")
REGDEF(DISX86::REGA::regaSt4, "st4")
REGDEF(DISX86::REGA::regaSt5, "st5")
REGDEF(DISX86::REGA::regaSt6, "st6")
REGDEF(DISX86::REGA::regaSt7, "st7")

// XMM registers
REGDEF(DISX86::REGA::regaXmm0, "xmm0")
REGDEF(DISX86::REGA::regaXmm1, "xmm1")
REGDEF(DISX86::REGA::regaXmm2, "xmm2")
REGDEF(DISX86::REGA::regaXmm3, "xmm3")
REGDEF(DISX86::REGA::regaXmm4, "xmm4")
REGDEF(DISX86::REGA::regaXmm5, "xmm5")
REGDEF(DISX86::REGA::regaXmm6, "xmm6")
REGDEF(DISX86::REGA::regaXmm7, "xmm7")
REGDEF(DISX86::REGA::regaXmm8, "xmm8")
REGDEF(DISX86::REGA::regaXmm9, "xmm9")
REGDEF(DISX86::REGA::regaXmm10, "xmm10")
REGDEF(DISX86::REGA::regaXmm11, "xmm11")
REGDEF(DISX86::REGA::regaXmm12, "xmm12")
REGDEF(DISX86::REGA::regaXmm13, "xmm13")
REGDEF(DISX86::REGA::regaXmm14, "xmm14")
REGDEF(DISX86::REGA::regaXmm15, "xmm15")

#undef REGDEF
