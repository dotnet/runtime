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

// control flow
//
INST(invalid,     "INVALID",     0, IF_NONE,    BAD_CODE)
INST(unreachable, "unreachable", 0, IF_OPCODE,  0x00)
INST(label,       "label",       0, IF_LABEL,   0x00)
INST(nop,         "nop",         0, IF_OPCODE,  0x01)
INST(block,       "block",       0, IF_TYPE,    0x02)
INST(loop,        "loop",        0, IF_TYPE,    0x03)
INST(if,          "if",          0, IF_TYPE,    0x04)
INST(else,        "else",        0, IF_OPCODE,  0x05)
INST(end,         "end",         0, IF_OPCODE,  0x0B)
INST(br,          "br",          0, IF_ULEB128, 0x0C)
INST(br_if,       "br_if",       0, IF_ULEB128, 0x0D)
INST(br_table,    "br_table",    0, IF_ULEB128, 0x0E)
INST(return,      "return",      0, IF_OPCODE,  0x0F)

// constants
//
INST(i32_const,   "i32.const",   0, IF_ULEB128, 0x41)
INST(i64_const,   "i64.const",   0, IF_ULEB128, 0x42)
INST(f32_const,   "f32.const",   0, IF_ULEB128, 0x43)
INST(f64_const,   "f64.const",   0, IF_ULEB128, 0x44)

// relops
//
INST(i32_eqz,     "i32.eqz",     0, IF_OPCODE,  0x45)
INST(i32_eq,      "i32.eq",      0, IF_OPCODE,  0x46)
INST(i32_ne,      "i32.ne",      0, IF_OPCODE,  0x47)
INST(i32_lt_s,    "i32.lt_s",    0, IF_OPCODE,  0x48)
INST(i32_lt_u,    "i32.lt_u",    0, IF_OPCODE,  0x49)
INST(i32_gt_s,    "i32.gt_s",    0, IF_OPCODE,  0x4A)
INST(i32_gt_u,    "i32.gt_u",    0, IF_OPCODE,  0x4B)
INST(i32_le_s,    "i32.le_s",    0, IF_OPCODE,  0x4C)
INST(i32_le_u,    "i32.le_u",    0, IF_OPCODE,  0x4D)
INST(i32_ge_s,    "i32.ge_s",    0, IF_OPCODE,  0x4E)
INST(i32_ge_u,    "i32.ge_u",    0, IF_OPCODE,  0x4F)

INST(i64_eqz,     "i64.eqz",     0, IF_OPCODE,  0x50)
INST(i64_eq,      "i64.eq",      0, IF_OPCODE,  0x51)
INST(i64_ne,      "i64.ne",      0, IF_OPCODE,  0x52)
INST(i64_lt_s,    "i64.lt_s",    0, IF_OPCODE,  0x53)
INST(i64_lt_u,    "i64.lt_u",    0, IF_OPCODE,  0x54)
INST(i64_gt_s,    "i64.gt_s",    0, IF_OPCODE,  0x55)
INST(i64_gt_u,    "i64.gt_u",    0, IF_OPCODE,  0x56)
INST(i64_le_s,    "i64.le_s",    0, IF_OPCODE,  0x57)
INST(i64_le_u,    "i64.le_u",    0, IF_OPCODE,  0x58)
INST(i64_ge_s,    "i64.ge_s",    0, IF_OPCODE,  0x59)
INST(i64_ge_u,    "i64.ge_u",    0, IF_OPCODE,  0x5A)

INST(f32_eq,      "f32.eq",      0, IF_OPCODE,  0x5B)
INST(f32_ne,      "f32.ne",      0, IF_OPCODE,  0x5C)
INST(f32_lt,      "f32.lt",      0, IF_OPCODE,  0x5D)
INST(f32_gt,      "f32.gt",      0, IF_OPCODE,  0x5E)
INST(f32_le,      "f32.le",      0, IF_OPCODE,  0x5F)
INST(f32_ge,      "f32.ge",      0, IF_OPCODE,  0x60)

INST(f64_eq,      "f64.eq",      0, IF_OPCODE,  0x61)
INST(f64_ne,      "f64.ne",      0, IF_OPCODE,  0x62)
INST(f64_lt,      "f64.lt",      0, IF_OPCODE,  0x63)
INST(f64_gt,      "f64.gt",      0, IF_OPCODE,  0x64)
INST(f64_le,      "f64.le",      0, IF_OPCODE,  0x65)
INST(f64_ge,      "f64.ge",      0, IF_OPCODE,  0x66)

// other

INST(local_get,   "local.get",   0, IF_ULEB128, 0x20)
INST(i32_load,    "i32.load",    0, IF_MEMARG,  0x28)
INST(i32_add,     "i32.add",     0, IF_OPCODE,  0x6a)
// clang-format on

#undef INST
