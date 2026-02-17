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
INST(invalid,       "INVALID",        0,    BAD_CODE)
