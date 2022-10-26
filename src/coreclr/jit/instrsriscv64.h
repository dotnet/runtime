// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  LoongArch64 instructions for JIT compiler
 *
 *          id          -- the enum name for the instruction
 *          nm          -- textual name (for assembly dipslay)
 *          ld/st/cmp   -- load/store/compare instruction
 *          encode      -- encoding 1
 *
******************************************************************************/

#if !defined(TARGET_RISCV64)
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
//     emitInsMayWriteMultipleRegs in emitLoongarch64.cpp.

// clang-format off
INST(invalid,       "INVALID",        0,    BAD_CODE)
INST(nop,           "addi",           0,    0x00000003)
INST(mov,           "addi",           0,    0x00000003)
INST(j,             "j",              0,    0x0000006f)
// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
