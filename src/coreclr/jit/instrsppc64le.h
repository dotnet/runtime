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
INST(mov,         "mr",           0,      X_FORM,	0x7FE00008)
INST(movi,        "movi",         0,      IF_DV_1B,  	0x0F000400)
INST(nop,         "nop",          0,      IF_SN_0A,  	0xD503201F)
INST(push,        "push",         0,      IUM_RD, 	0x0030FE)
INST(pop,         "pop",          0,      IUM_WR, 	0x00008E)
INST(blr,         "blr",          0,      IF_SN_0A,     0x4E800020)
INST(li,    	  "li",    	  0, 	  D_FORM,   	0x38000000)  // addi with RA=0
INST(lis,   	  "lis",   	  0, 	  D_FORM,   	0x3C000000)  // addis with RA=0
INST(ori,   	  "ori",   	  0, 	  D_FORM,   	0x60000000)
INST(oris,  	  "oris",  	  0, 	  D_FORM,   	0x64000000)
INST(sldi,  	  "sldi",  	  0, 	  MD_FORM,  	0x78000000)  // rldicr
INST(cmpdi,  	  "cmpdi",  	  0, 	  D_FORM,   	0x2C200000)  // cmpi with L=1
INST(lbz,	  "lbz",	  0,	  D_FORM,	0x88000000)
INST(lhz,	  "lhz",	  0,	  D_FORM,	0xA0000000)
INST(lha,	  "lha",	  0,	  D_FORM,	0xA8000000)
INST(lwz,	  "lwz",	  0,	  D_FORM,	0x80000000)
INST(lwa,	  "lwa",	  0,	  DS_FORM,	0xE8000000)
INST(ld,	  "ld",		  0,	  DS_FORM,	0xE8000000)

// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
