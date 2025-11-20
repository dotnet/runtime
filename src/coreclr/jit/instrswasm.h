// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  WASM instructions
 *
 *          id          -- the enum name for the instruction
 *          nm          -- textual name (for assembly display)
 *          info        -- miscellaneous instruction info
 *          encode      -- encoding (modulo operands)
 *
 ******************************************************************************/

#ifndef TARGET_WASM
#error Unexpected target type
#endif

#ifndef INST
#error INST must be defined before including this file.
#endif

// TODO-WASM: fill out with more instructions (and everything else needed).
//
// clang-format off
INST(invalid,     "INVALID",     0, BAD_CODE)
INST(unreachable, "unreachable", 0, 0x00)
INST(nop,         "nop",         0, 0x01)
INST(i32_add,     "i32.add",     0, 0x6a)
// clang-format on

#undef INST
