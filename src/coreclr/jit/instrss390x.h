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
INST(align,             "align",                0,              BAD_CODE)
INST(nop,               "nop",                  0,              0x07)
//// R
INST(br,		"br",			0,		0x07)
INST(ret,		"ret",			0,		0x07)
//// R_R
INST(lgfi,		"lgfi",			0,		0xc01)
INST(iihf,              "iihf",                 0,              0xc08)
INST(iilf,              "iilf",                 0,              0xc09)
INST(break,		"break",		0,		0x00)
INST(brk_unix,          "brk_unix",             0,              0xD4200000)
INST(aebr,      "aebr",         0,      0xb30a)
INST(adbr,      "adbr",         0,      0xb31a)
INST(sebr,      "sebr",         0,      0xb30b)
INST(sdbr,      "sdbr",         0,      0xb31b)
INST(meebr,     "meebr",        0,      0xb317)
INST(mdbr,      "mdbr",         0,      0xb31c)
INST(debr,      "debr",         0,      0xb30d)
INST(ddbr,      "ddbr",         0,      0xb31d)
INST(cfebr,     "cfebr",        0,      0xb398)
INST(cfdbr,     "cfdbr",        0,      0xb399)
INST(cgebr,     "cgebr",        0,      0xb3a8)
INST(cgdbr,     "cgdbr",        0,      0xb3a9)
INST(clfebr,    "clfebr",       0,      0xb39c)
INST(clfdbr,    "clfdbr",       0,      0xb39d)
INST(clgebr,    "clgebr",       0,      0xb3ac)
INST(clgdbr,    "clgdbr",       0,      0xb3ad)
INST(ldebr,     "ldebr",        0,      0xb304)
INST(ledbr,     "ledbr",        0,      0xb344)
INST(cebr,      "cebr",         0,      0xb309)
INST(cdbr,      "cdbr",         0,      0xb319)
INST(dr,        "dr",           0,      0x1d)
INST(dsgr,      "dsgr",         0,      0xb90d)
INST(dlr,       "dlr",          0,      0xb997)
INST(dlgr,      "dlgr",         0,      0xb987)
INST(llgfr,     "llgfr",        0,      0xb916)
INST(lgfr,      "lgfr",         0,      0xb914)
INST(lcdbr,     "lcdbr",        0,      0xb313)
INST(lcebr,     "lcebr",        0,      0xb303)

////R_I
INST(llgc,		"llgc",			0,		0xe390)
INST(lgb,		"lgb",			0,		0xe377)
INST(lgh,		"lgh",			0,		0xe315)
INST(llgh,		"lgh",			0,		0xe391)
INST(l,			"l",			0,		0x58)
INST(lg,		"lg",			0,		0xe304)
INST(afi,       "afi",          0,      0xc29)
INST(agfi,      "agfi",         0,      0xc28)
INST(msfi,      "msfi",         0,      0xc21)
INST(msgfi,     "msgfi",        0,      0xc20)
INST(oill,      "oill",         0,      0xa5b)
INST(ley,          "ley",                      0,              0xed64)
INST(ldy,          "ldy",                      0,              0xed65)
INST(stey,      "stey",         0,      0xed66)
INST(stdy,      "stdy",         0,      0xed67)
INST(ldgr,      "ldgr",         0,      0xb3c1)
INST(xihf,      "xifh",         0,      0xc06)
INST(nihf,      "nihf",         0,      0xc0a)
INST(srlg,      "srlg",         0,      0xeb0c)
INST(rllg,      "rllg",         0,      0xeb1c)
INST(srlk,      "srlk",         0,      0xebde)
INST(rll,       "rll",          0,      0xeb1d)
INST(lcr,       "lcr",          0,      0x13)
INST(lcgr,      "lcgr",         0,      0xb903)
INST(nork,      "nork",         0,      0xb976)
INST(nogrk,     "nogrk",        0,      0xb966)

//// R_R_I
INST(stc,		"stc",			0,		0x42)
INST(sth,		"sth",			0,		0x40)
INST(st,		"st",			0,		0x50)
INST(stg,		"stg",			0,		0xe324)
INST(std,		"std",			0,		0x60)
INST(lay,		"lay",			0,		0xe371)
INST(srda,      "srda",         0,      0x8e)
INST(srdl,      "srdl",         0,      0x8c)
INST(sllk,      "sllk",         0,      0xebdf)
INST(srak,      "srak",         0,      0xebdc)
//// R_R_R
INST(ark,		"ark",			0,		0xb9f8)
INST(agrk,		"agrk",			0,		0xb9e8)
INST(srk,       "srk",          0,      0xb9f9)
INST(sgrk,      "sgrk",         0,      0xb9e9)
INST(nrk,       "nrk",          0,      0xb9f4)
INST(ngrk,      "ngrk",         0,      0xb9e4)
INST(ncrk,      "ncrk",         0,      0xb9f5)
INST(ork,       "ork",          0,      0xb9f6)
INST(ogrk,      "ogrk",         0,      0xb9e6)
INST(xrk,       "xrk",          0,      0xb9f7)
INST(xgrk,      "xgrk",         0,      0xb9e7)
INST(mul,		"msrkc",		0,		0xb9fd)
INST(msgrkc,	"msgrkc",		0,		0xb9ed)

//// R_R_R_I
INST(stmg,		"stmg",			0,		0xeb24)
INST(lmg,		"lmg",			0,		0xeb04)
INST(srag,      "srag",         0,      0xeb0a)
INST(sllg,      "sllg",         0,      0xeb0d)
//// R_I_R

//// R_I_I
INST(ni,        "ni",           0,      0x94)
INST(xi,        "xi",           0,      0x97)

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

//// Compare (RR format - 2 bytes, sets condition code)
INST(cr,            "cr",             0,    0x19)
INST(cgr,       "cgr",          0,      0xB920)
INST(clr,       "clr",          0,      0x15)
INST(clgr,      "clgr",         0,      0xB921)

INST(chi,       "chi",          0,      0xa7e)
INST(cfi,       "cfi",          0,      0xc2d)
INST(cgfi,      "cgfi",         0,      0xc2c)
INST(clfi,      "clfi",         0,      0xc2f)
INST(clgfi,     "clgfi",        0,      0xc2e)

//// Conditional Branch (all use BRCL opcode 0xC04, RIL format, 6 bytes)
INST(j,             "brcl",           0,    0xC04)
INST(beq,           "brcl",           0,    0xC04)
INST(bne,           "brcl",           0,    0xC04)
INST(bgt,           "brcl",           0,    0xC04)
INST(ble,           "brcl",           0,    0xC04)
INST(blt,           "brcl",           0,    0xC04)
INST(bge,           "brcl",           0,    0xC04)

// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
