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
INST(mtctr,       "mtctr",        0,      XFX_FORM,     0x7C0903A6)  // Move to Count Register
INST(bctr,        "bctr",         0,      XL_FORM,      0x4E800420)  // Branch to Count Register
INST(bctrl,       "bctrl",        0,      XL_FORM,      0x4E800421)  // Branch to Count Register and Link
INST(bl,          "bl",           0,      I_FORM,       0x48000001)  // Branch and Link
INST(addi,        "addi",         0,      D_FORM,       0x38000000)
INST(li,    	  "li",    	  0, 	  D_FORM,   	0x38000000)  // addi with RA=0
INST(lis,   	  "lis",   	  0, 	  D_FORM,   	0x3C000000)  // addis with RA=0
INST(ori,   	  "ori",   	  0, 	  D_FORM,   	0x60000000)
INST(oris,  	  "oris",  	  0, 	  D_FORM,   	0x64000000)
INST(sldi,  	  "sldi",  	  0, 	  MD_FORM,  	0x78000000)  // rldicr
INST(srdi,        "srdi",         0,      MD_FORM,      0x78000002)  // rldicl - Shift Right Doubleword Immediate
INST(sld,         "sld",          0,      X_FORM,       0x7C000036)  // Shift Left Doubleword
INST(srd,         "srd",          0,      X_FORM,       0x7C000436)  // Shift Right Doubleword
INST(srad,        "srad",         0,      X_FORM,       0x7C000634)  // Shift Right Algebraic Doubleword
INST(slw,         "slw",          0,      X_FORM,       0x7C000030)  // Shift Left Word
INST(srw,         "srw",          0,      X_FORM,       0x7C000430)  // Shift Right Word
INST(sraw,        "sraw",         0,      X_FORM,       0x7C000630)  // Shift Right Algebraic Word
INST(sradi,       "sradi",        0,      MD_FORM,      0x7C000674)  // Shift Right Algebraic Doubleword Immediate
INST(slwi,        "slwi",         0,      M_FORM,       0x54000000)  // Shift Left Word Immediate (rlwinm)
INST(srwi,        "srwi",         0,      M_FORM,       0x54000000)  // Shift Right Word Immediate (rlwinm)
INST(srawi,       "srawi",        0,      X_FORM,       0x7C000670)  // Shift Right Algebraic Word Immediate
INST(andi,        "andi.",        0,      D_FORM,       0x70000000)  // AND Immediate (with record bit)
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
INST(lfs,       "lfs",          0,      D_FORM,       0xC0000000)
INST(lfd,       "lfd",          0,      D_FORM,       0xC8000000)
INST(stfs,      "stfs",         0,      D_FORM,       0xD0000000)
INST(stfd,      "stfd",         0,      D_FORM,       0xD8000000)
INST(stb,	  "stb",	  0,	  D_FORM,	0x98000000)
INST(sth,	  "sth",	  0,	  D_FORM,	0xB0000000)
INST(stw,	  "stw",	  0,	  D_FORM,	0x90000000)
INST(std,	  "std",	  0,	  DS_FORM,	0xF8000000)
INST(stdu,	  "stdu",	  0,	  DS_FORM,	0xF8000001)
INST(lbzx,        "lbzx",         0,      X_FORM,       0x7C0000AE)
INST(lhzx,        "lhzx",         0,      X_FORM,       0x7C00022E) 
INST(lhax,        "lhax",         0,      X_FORM,       0x7C0002AE)  
INST(lwzx,        "lwzx",         0,      X_FORM,       0x7C00002E)  // Load Word and Zero Indexed
INST(lwax,        "lwax",         0,      X_FORM,       0x7C0002AA)  // Load Word Algebraic Indexed
INST(ldx,         "ldx",          0,      X_FORM,       0x7C00002A)  // Load Doubleword Indexed
INST(lfsx,        "lfsx",         0,      X_FORM,       0x7C00042E)  // Load Floating Single Indexed
INST(lfdx,        "lfdx",         0,      X_FORM,       0x7C0004AE)  // Load Floating Double Indexed
INST(stbx,        "stbx",         0,      X_FORM,       0x7C0001AE)  // Store Byte Indexed
INST(sthx,        "sthx",         0,      X_FORM,       0x7C00032E)  // Store Halfword Indexed
INST(stwx,        "stwx",         0,      X_FORM,       0x7C00012E)  // Store Word Indexed
INST(stdx,        "stdx",         0,      X_FORM,       0x7C00012A)  // Store Doubleword Indexed
INST(stfsx,       "stfsx",        0,      X_FORM,       0x7C00052E)  // Store Floating Single Indexed
INST(stfdx,       "stfdx",        0,      X_FORM,       0x7C0005AE)  // Store Floating Double Indexed
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
INST(fmr,         "fmr",          0,      X_FORM,       0xFC000090)  // Floating Move Register
INST(fcmpu,       "fcmpu",        0,      X_FORM,       0xFC000000)  // Floating Compare Unordered
INST(fcmpo,       "fcmpo",        0,      X_FORM,       0xFC000020)  // Floating Compare Ordered
INST(frsp,        "frsp",         0,      X_FORM,       0xFC000018)  // Floating Round to Single Precision

// Memory barrier instructions
INST(hwsync,      "hwsync",       0,      X_FORM,       0x7C0004AC)  // Hardware Synchronize
INST(lwsync,      "lwsync",       0,      X_FORM,       0x7C2004AC)  // Lightweight Synchronize
INST(isync,       "isync",        0,      XL_FORM,      0x4C00012C)  // Instruction Synchronize

// Floating-point conversion instructions
INST(fctiwz,      "fctiwz",       0,      X_FORM,       0xFC00001E)  // Float Convert to Int Word, Round toward Zero
INST(fctidz,      "fctidz",       0,      X_FORM,       0xFC00065E)  // Float Convert to Int Doubleword, Round toward Zero
INST(fctiwuz,     "fctiwuz",      0,      X_FORM,       0xFC00011E)  // Float Convert to Unsigned Int Word, Round toward Zero
INST(fctiduz,     "fctiduz",      0,      X_FORM,       0xFC00075E)  // Float Convert to Unsigned Int Doubleword, Round toward Zero
INST(fcfid,       "fcfid",        0,      X_FORM,       0xFC00069C)  // Float Convert From Int Doubleword
INST(fcfids,      "fcfids",       0,      X_FORM,       0xEC00069C)  // Float Convert From Int Doubleword to Single
INST(fcfidu,      "fcfidu",       0,      X_FORM,       0xFC00079C)  // Float Convert From Unsigned Int Doubleword
INST(fcfidus,     "fcfidus",      0,      X_FORM,       0xEC00079C)  // Float Convert From Unsigned Int Doubleword to Single

// Sign extension instructions
INST(extsb,       "extsb",        0,      X_FORM,       0x7C000774)  // Extend Sign Byte
INST(extsh,       "extsh",        0,      X_FORM,       0x7C000734)  // Extend Sign Halfword
INST(extsw,       "extsw",        0,      X_FORM,       0x7C0007B4)  // Extend Sign Word
 
// Integer arithmetic instructions
INST(add,         "add",          0,      XO_FORM,      0x7C000214)  // Add
INST(subf,        "subf",         0,      XO_FORM,      0x7C000050)  // Subtract From
INST(mulld,       "mulld",        0,      XO_FORM,      0x7C0001D2)  // Multiply Low Doubleword
INST(mullw,       "mullw",        0,      XO_FORM,      0x7C0001D6)  // Multiply Low Word
INST(divd,        "divd",         0,      XO_FORM,      0x7C0003D2)  // Divide Doubleword
INST(divdu,       "divdu",        0,      XO_FORM,      0x7C000392)  // Divide Doubleword Unsigned
INST(divw,        "divw",         0,      XO_FORM,      0x7C0003D6)  // Divide Word
INST(divwu,       "divwu",        0,      XO_FORM,      0x7C000396)  // Divide Word Unsigned

// Logical/Bitwise instructions
INST(and_ins,     "and",          0,      X_FORM,       0x7C000038)  // AND (renamed to avoid C++ keyword conflict)
INST(or_ins,      "or",           0,      X_FORM,       0x7C000378)  // OR (renamed to avoid C++ keyword conflict)
INST(xor_ins,     "xor",          0,      X_FORM,       0x7C000278)  // XOR (renamed to avoid C++ keyword conflict)
INST(nor,         "nor",          0,      X_FORM,       0x7C0000F8)  // NOR
INST(nand,        "nand",         0,      X_FORM,       0x7C0003B8)  // NAND
INST(andc,        "andc",         0,      X_FORM,       0x7C000078)  // AND with Complement
INST(orc,         "orc",          0,      X_FORM,       0x7C000338)  // OR with Complement
INST(xori,        "xori",         0,      D_FORM,       0x68000000)  // XOR Immediate
INST(xoris,       "xoris",        0,      D_FORM,       0x6C000000)  // XOR Immediate Shifted

// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
