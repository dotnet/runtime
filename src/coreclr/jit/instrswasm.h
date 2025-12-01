// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  WASM instructions
 *
 *          id          -- the enum name for the instruction
 *          nm          -- textual name (for assembly display)
 *          info        -- miscellaneous instruction info
 *          fmt         -- instruction format
 *          opcode      -- encoding (modulo operands)
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
INST(invalid,     "INVALID",     0, IF_NONE,    BAD_CODE)
INST(unreachable, "unreachable", 0, IF_OPCODE,  0x00)
INST(nop,         "nop",         0, IF_OPCODE,  0x01)
INST(br,          "br",          0, IF_ULEB128, 0x0C)
INST(local_get,   "local.get",   0, IF_ULEB128, 0x20)
INST(i32_load,    "i32.load",    0, IF_MEMARG,  0x28)
INST(i64_load,    "i64.load",    0, IF_MEMARG,  0x29)
INST(f32_load,    "f32.load",    0, IF_MEMARG,  0x2a)
INST(f64_load,    "f64.load",    0, IF_MEMARG,  0x2b)
INST(i32_add,     "i32.add",     0, IF_OPCODE,  0x6a)
INST(i64_add,     "i64.add",     0, IF_OPCODE,  0x7c)
INST(f32_add,     "f32.add",     0, IF_OPCODE,  0x92)
INST(f64_add,     "f64.add",     0, IF_OPCODE,  0xa0)
// clang-format on

#undef INST
