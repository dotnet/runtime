// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  RISCV64 instructions for JIT compiler
 *
 *          id          -- the enum name for the instruction
 *          nm          -- textual name (for assembly dipslay)
 *          ld/st/cmp   -- load/store/compare instruction
 *          encode      -- encoding 1
 *
 ******************************************************************************/

#if !defined(TARGET_S390X)
#error Unexpected target type
#endif

#ifndef INST
#error INST must be defined before including this file.
#endif

/*****************************************************************************/
/*               The following is RISCV64-specific                               */
/*****************************************************************************/

// If you're adding a new instruction:
// You need not only to fill in one of these macros describing the instruction, but also:
//   * If the instruction writes to more than one destination register, update the function
//     emitInsMayWriteMultipleRegs in emitriscv64.cpp.

// clang-format off

// RV32I & RV64I
INST(invalid,		"INVALID",		0,		BAD_CODE)
INST(nop,		"nop",			0,		0x00000013)
//// R
INST(br,		"br",			0,		0x07)
INST(ret,		"br",			0,		0x07)
//// R_R
INST(mov,		"lgfi",			0,		0xc01)
INST(break,		"break",		0,		0x00)

////R_I
INST(llgc,		"llgc",			0,		0xe390)
INST(lgb,		"lgb",			0,		0xe377)
INST(lgh,		"lgh",			0,		0xe315)
INST(llgh,		"lgh",			0,		0xe391)
INST(l,			"l",			0,		0x58)
//// R_R_I
INST(stc,		"stc",			0,		0x42)
INST(sth,		"sth",			0,		0x40)
INST(st,		"st",			0,		0x50)
INST(stg,		"stg",			0,		0xe324)
INST(std,		"std",			0,		0x60)
INST(lay,		"lay",			0,		0xe371)

//// R_R_R
INST(add,		"ark",			0,		0xb9f8)

//// R_R_R_I
INST(stmg,		"stmg",			0,		0xeb24)
INST(lmg,		"lmg",			0,		0xeb04)

//// R_I_R

//// R_I_I


//// R_R_I

//// R_I

//// R_R_I

// RV64I
//// R_R_I

//// R_R_R

//// R_R_I
// RV32M & RV64M
//// R_R_R
// RV64M
//// R_R_R
// RV32F & RV64D
//// R_R_R_R
//// R_R_R
//// R_R
//// R_R_R
//// R_R
//// R_R_R_R
//// R_R_R
//// R_R
//// R_R_R
//// R_R
INST(lgr,		"lgr",			0,		0xb904)
//// R_R_I
// RV64F
//// R_R
// RV64D
// RV32A + RV64A (R-type, R_R_R)

//s390xmarker:these are just added for the sake of passing the build, remove later
INST(j,             "j",              0,    0x0000006f)
INST(beq,           "beq",            0,    0x00000063)
INST(bne,           "bne",            0,    0x00001063)



// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
