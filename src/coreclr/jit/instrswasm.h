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
INST(block,       "block",       0, IF_BLOCK,   0x02)
INST(loop,        "loop",        0, IF_BLOCK,   0x03)
INST(if,          "if",          0, IF_BLOCK,   0x04)
INST(else,        "else",        0, IF_OPCODE,  0x05)
INST(end,         "end",         0, IF_OPCODE,  0x0B)
INST(br,          "br",          0, IF_ULEB128, 0x0C)
INST(br_if,       "br_if",       0, IF_ULEB128, 0x0D)
INST(br_table,    "br_table",    0, IF_ULEB128, 0x0E)
INST(return,      "return",      0, IF_OPCODE,  0x0F)

INST(local_get,   "local.get",   0, IF_ULEB128, 0x20)
INST(i32_load,    "i32.load",    0, IF_MEMARG,  0x28)
INST(i64_load,    "i64.load",    0, IF_MEMARG,  0x29)
INST(f32_load,    "f32.load",    0, IF_MEMARG,  0x2A)
INST(f64_load,    "f64.load",    0, IF_MEMARG,  0x2B)
// 5.4.7 Numeric Instructions
// TODO-WASM: Constants
// INST(i32_const,   "i32.const",   0, IF_LEB128, 0x41)
// INST(i64_const,   "i64.const",   0, IF_LEB128, 0x42)
// INST(f32_const,   "f32.const",   0, IF_F32, 0x43)
// INST(f64_const,   "f64.const",   0, IF_F64, 0x44)
// Integer comparisons
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
// Floating-point comparisons
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
// Integer arithmetic and logic operations
INST(i32_clz,     "i32.clz",     0, IF_OPCODE,  0x67)
INST(i32_ctz,     "i32.ctz",     0, IF_OPCODE,  0x68)
INST(i32_popcnt,  "i32.popcnt",  0, IF_OPCODE,  0x69)
INST(i32_add,     "i32.add",     0, IF_OPCODE,  0x6A)
INST(i32_sub,     "i32.sub",     0, IF_OPCODE,  0x6B)
INST(i32_mul,     "i32.mul",     0, IF_OPCODE,  0x6C)
INST(i32_div_s,   "i32.div_s",   0, IF_OPCODE,  0x6D)
INST(i32_div_u,   "i32.div_u",   0, IF_OPCODE,  0x6E)
INST(i32_rem_s,   "i32.rem_s",   0, IF_OPCODE,  0x6F)
INST(i32_rem_u,   "i32.rem_u",   0, IF_OPCODE,  0x70)
INST(i32_and,     "i32.and",     0, IF_OPCODE,  0x71)
INST(i32_or,      "i32.or",      0, IF_OPCODE,  0x72)
INST(i32_xor,     "i32.xor",     0, IF_OPCODE,  0x73)
INST(i32_shl,     "i32.shl",     0, IF_OPCODE,  0x74)
INST(i32_shr_s,   "i32.shr_s",   0, IF_OPCODE,  0x75)
INST(i32_shr_u,   "i32.shr_u",   0, IF_OPCODE,  0x76)
INST(i32_rotl,    "i32.rotl",    0, IF_OPCODE,  0x77)
INST(i32_rotr,    "i32.rotr",    0, IF_OPCODE,  0x78)
INST(i64_clz,     "i64.clz",     0, IF_OPCODE,  0x79)
INST(i64_ctz,     "i64.ctz",     0, IF_OPCODE,  0x7A)
INST(i64_popcnt,  "i64.popcnt",  0, IF_OPCODE,  0x7B)
INST(i64_add,     "i64.add",     0, IF_OPCODE,  0x7C)
INST(i64_sub,     "i64.sub",     0, IF_OPCODE,  0x7D)
INST(i64_mul,     "i64.mul",     0, IF_OPCODE,  0x7E)
INST(i64_div_s,   "i64.div_s",   0, IF_OPCODE,  0x7F)
INST(i64_div_u,   "i64.div_u",   0, IF_OPCODE,  0x80)
INST(i64_rem_s,   "i64.rem_s",   0, IF_OPCODE,  0x81)
INST(i64_rem_u,   "i64.rem_u",   0, IF_OPCODE,  0x82)
INST(i64_and,     "i64.and",     0, IF_OPCODE,  0x83)
INST(i64_or,      "i64.or",      0, IF_OPCODE,  0x84)
INST(i64_xor,     "i64.xor",     0, IF_OPCODE,  0x85)
INST(i64_shl,     "i64.shl",     0, IF_OPCODE,  0x86)
INST(i64_shr_s,   "i64.shr_s",   0, IF_OPCODE,  0x87)
INST(i64_shr_u,   "i64.shr_u",   0, IF_OPCODE,  0x88)
INST(i64_rotl,    "i64.rotl",    0, IF_OPCODE,  0x89)
INST(i64_rotr,    "i64.rotr",    0, IF_OPCODE,  0x8A)
// Floating point arithmetic operations
INST(f32_abs,     "f32.abs",     0, IF_OPCODE,  0x8B)
INST(f32_neg,     "f32.neg",     0, IF_OPCODE,  0x8C)
INST(f32_ceil,    "f32.ceil",    0, IF_OPCODE,  0x8D)
INST(f32_floor,   "f32.floor",   0, IF_OPCODE,  0x8E)
INST(f32_trunc,   "f32.trunc",   0, IF_OPCODE,  0x8F)
INST(f32_nearest, "f32.nearest", 0, IF_OPCODE,  0x90)
INST(f32_sqrt,    "f32.sqrt",    0, IF_OPCODE,  0x91)
INST(f32_add,     "f32.add",     0, IF_OPCODE,  0x92)
INST(f32_sub,     "f32.sub",     0, IF_OPCODE,  0x93)
INST(f32_mul,     "f32.mul",     0, IF_OPCODE,  0x94)
INST(f32_div,     "f32.div",     0, IF_OPCODE,  0x95)
INST(f32_min,     "f32.min",     0, IF_OPCODE,  0x96)
INST(f32_max,     "f32.max",     0, IF_OPCODE,  0x97)
INST(f32_copysign,"f32.copysign",0, IF_OPCODE,  0x98)
INST(f64_abs,     "f64.abs",     0, IF_OPCODE,  0x99)
INST(f64_neg,     "f64.neg",     0, IF_OPCODE,  0x9A)
INST(f64_ceil,    "f64.ceil",    0, IF_OPCODE,  0x9B)
INST(f64_floor,   "f64.floor",   0, IF_OPCODE,  0x9C)
INST(f64_trunc,   "f64.trunc",   0, IF_OPCODE,  0x9D)
INST(f64_nearest, "f64.nearest", 0, IF_OPCODE,  0x9E)
INST(f64_sqrt,    "f64.sqrt",    0, IF_OPCODE,  0x9F)
INST(f64_add,     "f64.add",     0, IF_OPCODE,  0xA0)
INST(f64_sub,     "f64.sub",     0, IF_OPCODE,  0xA1)
INST(f64_mul,     "f64.mul",     0, IF_OPCODE,  0xA2)
INST(f64_div,     "f64.div",     0, IF_OPCODE,  0xA3)
INST(f64_min,     "f64.min",     0, IF_OPCODE,  0xA4)
INST(f64_max,     "f64.max",     0, IF_OPCODE,  0xA5)
INST(f64_copysign,"f64.copysign",0, IF_OPCODE,  0xA6)
// clang-format on

#undef INST
