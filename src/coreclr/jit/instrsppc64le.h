// Licensed to the .NET Foundation under one or more agreements.
// // The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  ppc64le instructions for JIT compiler
 *  TODO POWERPC64
******************************************************************************/

#if !defined(TARGET_POWERPC64)
#error Unexpected target type
#endif

#ifndef INST
#error INST must be defined before including this file.
#endif

/*****************************************************************************/
/*               The following is PPC64LE-specific                             */
/*****************************************************************************/

// If you're adding a new instruction:
// You need not only to fill in one of these macros describing the instruction, but also:
//   * If the instruction writes to more than one destination register, update the function
//     emitInsMayWriteMultipleRegs in emitPpc64le.cpp.

// clang-format off
INST(invalid,     "INVALID",      0,      IF_NONE,   	BAD_CODE)
INST(trap,        "trap",         0,      X_FORM,	0x7FE00008)
INST(mov,         "mr",           0,      X_FORM,	0x7C000378)
INST(movi,        "movi",         0,      IF_DV_1B,  	0x0F000400)
INST(nop,         "nop",          0,      IF_SN_0A,  	0xD503201F)
INST(push,        "push",         0,      IUM_RD, 	0x0030FE)
INST(pop,         "pop",          0,      IUM_WR, 	0x00008E)
INST(blr,         "blr",          0,      IF_SN_0A,     0x4E800020)
INST(mflr,        "mflr",         0,      XFX_FORM,     0x7C0802A6)
INST(mtlr,        "mtlr",         0,      XFX_FORM,     0x7C0803A6)
INST(bctr,        "bctr",         0,      XL_FORM,      0x4E800420)  // Branch to Count Register
INST(bctrl,       "bctrl",        0,      XL_FORM,      0x4E800421)  // Branch to Count Register and Link
INST(bl,          "bl",           0,      I_FORM,       0x48000001)  // Branch and Link
INST(addi,        "addi",         0,      D_FORM,       0x38000000)
INST(li,    	  "li",    	  0, 	  D_FORM,   	0x38000000)  // addi with RA=0
INST(lis,   	  "lis",   	  0, 	  D_FORM,   	0x3C000000)  // addis with RA=0
INST(ori,   	  "ori",   	  0, 	  D_FORM,   	0x60000000)
INST(oris,  	  "oris",  	  0, 	  D_FORM,   	0x64000000)
INST(sldi,  	  "sldi",  	  0, 	  MD_FORM,  	0x78000000)  // rldicr
INST(cmpw,	  "cmpw",	  0,	  X_FORM,	0x7C000000) // cmp with L=0
INST(cmpd,        "cmpd",          0,      X_FORM,       0x7C200000) // cmp with L=1
INST(cmpwi,	  "cmpwi",	  0,	  D_FORM,	0x2C000000) // cmpi with L=0
INST(cmpdi,  	  "cmpdi",  	  0, 	  D_FORM,   	0x2C200000) // cmpi with L=1
INST(lbz,	  "lbz",	  0,	  D_FORM,	0x88000000)
INST(lhz,	  "lhz",	  0,	  D_FORM,	0xA0000000)
INST(lha,	  "lha",	  0,	  D_FORM,	0xA8000000)
INST(lwz,	  "lwz",	  0,	  D_FORM,	0x80000000)
INST(lwa,	  "lwa",	  0,	  DS_FORM,	0xE8000000)
INST(ld,	  "ld",		  0,	  DS_FORM,	0xE8000000)
INST(lfd,       "lfd",          0,      D_FORM,       0xC8000000)
INST(stfd,      "stfd",         0,      D_FORM,       0xD8000000)
INST(stb,	  "stb",	  0,	  D_FORM,	0x98000000)
INST(sth,	  "sth",	  0,	  D_FORM,	0xB0000000)
INST(stw,	  "stw",	  0,	  D_FORM,	0x90000000)
INST(std,	  "std",	  0,	  DS_FORM,	0xF8000000)
INST(stdu,	  "stdu",	  0,	  DS_FORM,	0xF8000001)
INST(b,		  "b",		  0,	  I_FORM,	0x48000000)
INST(beq,	  "beq",	  0,	  B_FORM,	0x41820000)
INST(bne,	  "bne",	  0,	  B_FORM,	0x40820000)
INST(blt,         "blt",          0,      B_FORM,       0x41800000)
INST(bge,         "bge",          0,      B_FORM,       0x40800000)
INST(bgt,         "bgt",          0,      B_FORM,       0x41810000)
INST(ble,         "ble",          0,      B_FORM,       0x40810000)

// Floating-point arithmetic instructions
INST(fadds,       "fadds",        0,      A_FORM,       0xEC00002A)  // Floating Add Single
INST(fadd,        "fadd",         0,      A_FORM,       0xFC00002A)  // Floating Add Double
INST(fsubs,       "fsubs",        0,      A_FORM,       0xEC000028)  // Floating Subtract Single
INST(fsub,        "fsub",         0,      A_FORM,       0xFC000028)  // Floating Subtract Double
INST(fmuls,       "fmuls",        0,      A_FORM,       0xEC000032)  // Floating Multiply Single
INST(fmul,        "fmul",         0,      A_FORM,       0xFC000032)  // Floating Multiply Double
INST(fdivs,       "fdivs",        0,      A_FORM,       0xEC000024)  // Floating Divide Single
INST(fdiv,        "fdiv",         0,      A_FORM,       0xFC000024)  // Floating Divide Double

// Integer arithmetic instructions
INST(add,         "add",          0,      XO_FORM,      0x7C000214)  // Add
INST(subf,        "subf",         0,      XO_FORM,      0x7C000050)  // Subtract From
INST(mulld,       "mulld",        0,      XO_FORM,      0x7C0001D2)  // Multiply Low Doubleword
INST(mullw,       "mullw",        0,      XO_FORM,      0x7C0001D6)  // Multiply Low Word
INST(divd,        "divd",         0,      XO_FORM,      0x7C0003D2)  // Divide Doubleword
INST(divdu,       "divdu",        0,      XO_FORM,      0x7C000392)  // Divide Doubleword Unsigned
INST(divw,        "divw",         0,      XO_FORM,      0x7C0003D6)  // Divide Word
INST(divwu,       "divwu",        0,      XO_FORM,      0x7C000396)  // Divide Word Unsigned

// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
