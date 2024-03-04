// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    WasmOpcode, WasmSimdOpcode, JiterpSpecialOpcode
} from "./jiterpreter-opcodes";
import {
    MintOpcode, SimdIntrinsic2, SimdIntrinsic3, SimdIntrinsic4
} from "./mintops";

export const ldcTable: { [opcode: number]: [WasmOpcode, number] } = {
    [MintOpcode.MINT_LDC_I4_M1]: [WasmOpcode.i32_const, -1],
    [MintOpcode.MINT_LDC_I4_0]:  [WasmOpcode.i32_const,  0],
    [MintOpcode.MINT_LDC_I4_1]:  [WasmOpcode.i32_const,  1],
    [MintOpcode.MINT_LDC_I4_2]:  [WasmOpcode.i32_const,  2],
    [MintOpcode.MINT_LDC_I4_3]:  [WasmOpcode.i32_const,  3],
    [MintOpcode.MINT_LDC_I4_4]:  [WasmOpcode.i32_const,  4],
    [MintOpcode.MINT_LDC_I4_5]:  [WasmOpcode.i32_const,  5],
    [MintOpcode.MINT_LDC_I4_6]:  [WasmOpcode.i32_const,  6],
    [MintOpcode.MINT_LDC_I4_7]:  [WasmOpcode.i32_const,  7],
    [MintOpcode.MINT_LDC_I4_8]:  [WasmOpcode.i32_const,  8],
};

// operator, loadOperator, storeOperator
export type OpRec3 = [WasmOpcode, WasmOpcode, WasmOpcode];
// operator, lhsLoadOperator, rhsLoadOperator, storeOperator
export type OpRec4 = [WasmOpcode, WasmOpcode, WasmOpcode, WasmOpcode];

export const floatToIntTable: { [opcode: number]: WasmOpcode } = {
    [MintOpcode.MINT_CONV_I4_R4]: WasmOpcode.i32_trunc_s_f32,
    [MintOpcode.MINT_CONV_I8_R4]: WasmOpcode.i64_trunc_s_f32,
    [MintOpcode.MINT_CONV_I4_R8]: WasmOpcode.i32_trunc_s_f64,
    [MintOpcode.MINT_CONV_I8_R8]: WasmOpcode.i64_trunc_s_f64,
};

export const unopTable: { [opcode: number]: OpRec3 | undefined } = {
    [MintOpcode.MINT_CEQ0_I4]:       [WasmOpcode.i32_eqz, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD1_I4]:       [WasmOpcode.i32_add, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SUB1_I4]:       [WasmOpcode.i32_sub, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_NEG_I4]:        [WasmOpcode.i32_sub, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_NOT_I4]:        [WasmOpcode.i32_xor, WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_ADD1_I8]:       [WasmOpcode.i64_add, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SUB1_I8]:       [WasmOpcode.i64_sub, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_NEG_I8]:        [WasmOpcode.i64_sub, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_NOT_I8]:        [WasmOpcode.i64_xor, WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_ADD_I4_IMM]:    [WasmOpcode.i32_add, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_I4_IMM]:    [WasmOpcode.i32_mul, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD_I8_IMM]:    [WasmOpcode.i64_add, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_MUL_I8_IMM]:    [WasmOpcode.i64_mul, WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_NEG_R4]:        [WasmOpcode.f32_neg, WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_NEG_R8]:        [WasmOpcode.f64_neg, WasmOpcode.f64_load, WasmOpcode.f64_store],

    [MintOpcode.MINT_CONV_R4_I4]:    [WasmOpcode.f32_convert_s_i32, WasmOpcode.i32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_CONV_R8_I4]:    [WasmOpcode.f64_convert_s_i32, WasmOpcode.i32_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R_UN_I4]:  [WasmOpcode.f64_convert_u_i32, WasmOpcode.i32_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R4_I8]:    [WasmOpcode.f32_convert_s_i64, WasmOpcode.i64_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_CONV_R8_I8]:    [WasmOpcode.f64_convert_s_i64, WasmOpcode.i64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R_UN_I8]:  [WasmOpcode.f64_convert_u_i64, WasmOpcode.i64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R8_R4]:    [WasmOpcode.f64_promote_f32,   WasmOpcode.f32_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R4_R8]:    [WasmOpcode.f32_demote_f64,    WasmOpcode.f64_load, WasmOpcode.f32_store],

    [MintOpcode.MINT_CONV_I8_I4]:    [WasmOpcode.nop, WasmOpcode.i64_load32_s, WasmOpcode.i64_store],
    [MintOpcode.MINT_CONV_I8_U4]:    [WasmOpcode.nop, WasmOpcode.i64_load32_u, WasmOpcode.i64_store],

    [MintOpcode.MINT_CONV_U1_I4]:    [WasmOpcode.i32_and,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_U2_I4]:    [WasmOpcode.i32_and,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I1_I4]:    [WasmOpcode.i32_shr_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I2_I4]:    [WasmOpcode.i32_shr_s, WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CONV_U1_I8]:    [WasmOpcode.i32_and,   WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_U2_I8]:    [WasmOpcode.i32_and,   WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I1_I8]:    [WasmOpcode.i32_shr_s, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I2_I8]:    [WasmOpcode.i32_shr_s, WasmOpcode.i64_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_SHL_I4_IMM]:    [WasmOpcode.i32_shl,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHL_I8_IMM]:    [WasmOpcode.i64_shl,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_I4_IMM]:    [WasmOpcode.i32_shr_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_I8_IMM]:    [WasmOpcode.i64_shr_s, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_UN_I4_IMM]: [WasmOpcode.i32_shr_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_UN_I8_IMM]: [WasmOpcode.i64_shr_u, WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_ROL_I4_IMM]:    [WasmOpcode.i32_rotl, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ROL_I8_IMM]:    [WasmOpcode.i64_rotl, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_ROR_I4_IMM]:    [WasmOpcode.i32_rotr, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ROR_I8_IMM]:    [WasmOpcode.i64_rotr, WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_CLZ_I4]:        [WasmOpcode.i32_clz,    WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CTZ_I4]:        [WasmOpcode.i32_ctz,    WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_POPCNT_I4]:     [WasmOpcode.i32_popcnt, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLZ_I8]:        [WasmOpcode.i64_clz,    WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_CTZ_I8]:        [WasmOpcode.i64_ctz,    WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_POPCNT_I8]:     [WasmOpcode.i64_popcnt, WasmOpcode.i64_load, WasmOpcode.i64_store],
};

// HACK: Generating correct wasm for these is non-trivial so we hand them off to C.
// The opcode specifies whether the operands need to be promoted first.
export const intrinsicFpBinops: { [opcode: number]: WasmOpcode } = {
    [MintOpcode.MINT_CEQ_R4]:        WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CEQ_R8]:        WasmOpcode.nop,
    [MintOpcode.MINT_CNE_R4]:        WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CNE_R8]:        WasmOpcode.nop,
    [MintOpcode.MINT_CGT_R4]:        WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CGT_R8]:        WasmOpcode.nop,
    [MintOpcode.MINT_CGE_R4]:        WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CGE_R8]:        WasmOpcode.nop,
    [MintOpcode.MINT_CGT_UN_R4]:     WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CGT_UN_R8]:     WasmOpcode.nop,
    [MintOpcode.MINT_CLT_R4]:        WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CLT_R8]:        WasmOpcode.nop,
    [MintOpcode.MINT_CLT_UN_R4]:     WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CLT_UN_R8]:     WasmOpcode.nop,
    [MintOpcode.MINT_CLE_R4]:        WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CLE_R8]:        WasmOpcode.nop,
    [JiterpSpecialOpcode.CGE_UN_R4]: WasmOpcode.f64_promote_f32,
    [JiterpSpecialOpcode.CLE_UN_R4]: WasmOpcode.f64_promote_f32,
    [JiterpSpecialOpcode.CNE_UN_R4]: WasmOpcode.f64_promote_f32,
    [JiterpSpecialOpcode.CGE_UN_R8]: WasmOpcode.nop,
    [JiterpSpecialOpcode.CLE_UN_R8]: WasmOpcode.nop,
    [JiterpSpecialOpcode.CNE_UN_R8]: WasmOpcode.nop,
};

export const binopTable: { [opcode: number]: OpRec3 | OpRec4 | undefined } = {
    [MintOpcode.MINT_ADD_I4]: [WasmOpcode.i32_add, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD_OVF_I4]: [WasmOpcode.i32_add, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD_OVF_UN_I4]: [WasmOpcode.i32_add, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SUB_I4]: [WasmOpcode.i32_sub, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_I4]: [WasmOpcode.i32_mul, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_OVF_I4]: [WasmOpcode.i32_mul, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_OVF_UN_I4]: [WasmOpcode.i32_mul, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_DIV_I4]: [WasmOpcode.i32_div_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_DIV_UN_I4]: [WasmOpcode.i32_div_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_REM_I4]: [WasmOpcode.i32_rem_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_REM_UN_I4]: [WasmOpcode.i32_rem_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_AND_I4]: [WasmOpcode.i32_and, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_OR_I4]: [WasmOpcode.i32_or, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_XOR_I4]: [WasmOpcode.i32_xor, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHL_I4]: [WasmOpcode.i32_shl, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_I4]: [WasmOpcode.i32_shr_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_UN_I4]: [WasmOpcode.i32_shr_u, WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_ADD_I8]: [WasmOpcode.i64_add, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SUB_I8]: [WasmOpcode.i64_sub, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_MUL_I8]: [WasmOpcode.i64_mul, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_DIV_I8]: [WasmOpcode.i64_div_s, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_REM_I8]: [WasmOpcode.i64_rem_s, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_DIV_UN_I8]: [WasmOpcode.i64_div_u, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_REM_UN_I8]: [WasmOpcode.i64_rem_u, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_AND_I8]: [WasmOpcode.i64_and, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_OR_I8]: [WasmOpcode.i64_or, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_XOR_I8]: [WasmOpcode.i64_xor, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHL_I8]: [WasmOpcode.i64_shl, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_I8]: [WasmOpcode.i64_shr_s, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_UN_I8]: [WasmOpcode.i64_shr_u, WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_ADD_R4]: [WasmOpcode.f32_add, WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_SUB_R4]: [WasmOpcode.f32_sub, WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_MUL_R4]: [WasmOpcode.f32_mul, WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_DIV_R4]: [WasmOpcode.f32_div, WasmOpcode.f32_load, WasmOpcode.f32_store],

    [MintOpcode.MINT_ADD_R8]: [WasmOpcode.f64_add, WasmOpcode.f64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_SUB_R8]: [WasmOpcode.f64_sub, WasmOpcode.f64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_MUL_R8]: [WasmOpcode.f64_mul, WasmOpcode.f64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_DIV_R8]: [WasmOpcode.f64_div, WasmOpcode.f64_load, WasmOpcode.f64_store],

    [MintOpcode.MINT_CEQ_I4]: [WasmOpcode.i32_eq, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CNE_I4]: [WasmOpcode.i32_ne, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLT_I4]: [WasmOpcode.i32_lt_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_I4]: [WasmOpcode.i32_gt_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_I4]: [WasmOpcode.i32_le_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_I4]: [WasmOpcode.i32_ge_s, WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CLT_UN_I4]: [WasmOpcode.i32_lt_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_UN_I4]: [WasmOpcode.i32_gt_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_UN_I4]: [WasmOpcode.i32_le_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_UN_I4]: [WasmOpcode.i32_ge_u, WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CEQ_I8]: [WasmOpcode.i64_eq, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CNE_I8]: [WasmOpcode.i64_ne, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLT_I8]: [WasmOpcode.i64_lt_s, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_I8]: [WasmOpcode.i64_gt_s, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_I8]: [WasmOpcode.i64_le_s, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_I8]: [WasmOpcode.i64_ge_s, WasmOpcode.i64_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CLT_UN_I8]: [WasmOpcode.i64_lt_u, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_UN_I8]: [WasmOpcode.i64_gt_u, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_UN_I8]: [WasmOpcode.i64_le_u, WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_UN_I8]: [WasmOpcode.i64_ge_u, WasmOpcode.i64_load, WasmOpcode.i32_store],

};

export const relopbranchTable: { [opcode: number]: [comparisonOpcode: MintOpcode, immediateOpcode: WasmOpcode | false, isSafepoint: boolean] | MintOpcode | undefined } = {
    [MintOpcode.MINT_BEQ_I4_S]: MintOpcode.MINT_CEQ_I4,
    [MintOpcode.MINT_BNE_UN_I4_S]: MintOpcode.MINT_CNE_I4,
    [MintOpcode.MINT_BGT_I4_S]: MintOpcode.MINT_CGT_I4,
    [MintOpcode.MINT_BGT_UN_I4_S]: MintOpcode.MINT_CGT_UN_I4,
    [MintOpcode.MINT_BLT_I4_S]: MintOpcode.MINT_CLT_I4,
    [MintOpcode.MINT_BLT_UN_I4_S]: MintOpcode.MINT_CLT_UN_I4,
    [MintOpcode.MINT_BGE_I4_S]: MintOpcode.MINT_CGE_I4,
    [MintOpcode.MINT_BGE_UN_I4_S]: MintOpcode.MINT_CGE_UN_I4,
    [MintOpcode.MINT_BLE_I4_S]: MintOpcode.MINT_CLE_I4,
    [MintOpcode.MINT_BLE_UN_I4_S]: MintOpcode.MINT_CLE_UN_I4,

    [MintOpcode.MINT_BEQ_I4_SP]: [MintOpcode.MINT_CEQ_I4, false, true],
    [MintOpcode.MINT_BNE_UN_I4_SP]: [MintOpcode.MINT_CNE_I4, false, true],
    [MintOpcode.MINT_BGT_I4_SP]: [MintOpcode.MINT_CGT_I4, false, true],
    [MintOpcode.MINT_BGT_UN_I4_SP]: [MintOpcode.MINT_CGT_UN_I4, false, true],
    [MintOpcode.MINT_BLT_I4_SP]: [MintOpcode.MINT_CLT_I4, false, true],
    [MintOpcode.MINT_BLT_UN_I4_SP]: [MintOpcode.MINT_CLT_UN_I4, false, true],
    [MintOpcode.MINT_BGE_I4_SP]: [MintOpcode.MINT_CGE_I4, false, true],
    [MintOpcode.MINT_BGE_UN_I4_SP]: [MintOpcode.MINT_CGE_UN_I4, false, true],
    [MintOpcode.MINT_BLE_I4_SP]: [MintOpcode.MINT_CLE_I4, false, true],
    [MintOpcode.MINT_BLE_UN_I4_SP]: [MintOpcode.MINT_CLE_UN_I4, false, true],

    [MintOpcode.MINT_BEQ_I4_IMM_SP]: [MintOpcode.MINT_CEQ_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BNE_UN_I4_IMM_SP]: [MintOpcode.MINT_CNE_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGT_I4_IMM_SP]: [MintOpcode.MINT_CGT_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGT_UN_I4_IMM_SP]: [MintOpcode.MINT_CGT_UN_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLT_I4_IMM_SP]: [MintOpcode.MINT_CLT_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLT_UN_I4_IMM_SP]: [MintOpcode.MINT_CLT_UN_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGE_I4_IMM_SP]: [MintOpcode.MINT_CGE_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGE_UN_I4_IMM_SP]: [MintOpcode.MINT_CGE_UN_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLE_I4_IMM_SP]: [MintOpcode.MINT_CLE_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLE_UN_I4_IMM_SP]: [MintOpcode.MINT_CLE_UN_I4, WasmOpcode.i32_const, true],

    [MintOpcode.MINT_BEQ_I8_S]: MintOpcode.MINT_CEQ_I8,
    [MintOpcode.MINT_BNE_UN_I8_S]: MintOpcode.MINT_CNE_I8,
    [MintOpcode.MINT_BGT_I8_S]: MintOpcode.MINT_CGT_I8,
    [MintOpcode.MINT_BGT_UN_I8_S]: MintOpcode.MINT_CGT_UN_I8,
    [MintOpcode.MINT_BLT_I8_S]: MintOpcode.MINT_CLT_I8,
    [MintOpcode.MINT_BLT_UN_I8_S]: MintOpcode.MINT_CLT_UN_I8,
    [MintOpcode.MINT_BGE_I8_S]: MintOpcode.MINT_CGE_I8,
    [MintOpcode.MINT_BGE_UN_I8_S]: MintOpcode.MINT_CGE_UN_I8,
    [MintOpcode.MINT_BLE_I8_S]: MintOpcode.MINT_CLE_I8,
    [MintOpcode.MINT_BLE_UN_I8_S]: MintOpcode.MINT_CLE_UN_I8,

    [MintOpcode.MINT_BEQ_I8_IMM_SP]: [MintOpcode.MINT_CEQ_I8, WasmOpcode.i64_const, true],
    // FIXME: Missing compare opcode
    // [MintOpcode.MINT_BNE_UN_I8_IMM_SP]: [MintOpcode.MINT_CNE_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGT_I8_IMM_SP]: [MintOpcode.MINT_CGT_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGT_UN_I8_IMM_SP]: [MintOpcode.MINT_CGT_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLT_I8_IMM_SP]: [MintOpcode.MINT_CLT_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLT_UN_I8_IMM_SP]: [MintOpcode.MINT_CLT_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGE_I8_IMM_SP]: [MintOpcode.MINT_CGE_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGE_UN_I8_IMM_SP]: [MintOpcode.MINT_CGE_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLE_I8_IMM_SP]: [MintOpcode.MINT_CLE_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLE_UN_I8_IMM_SP]: [MintOpcode.MINT_CLE_UN_I8, WasmOpcode.i64_const, true],

    [MintOpcode.MINT_BEQ_R4_S]: MintOpcode.MINT_CEQ_R4,
    [MintOpcode.MINT_BNE_UN_R4_S]: <any>JiterpSpecialOpcode.CNE_UN_R4,
    [MintOpcode.MINT_BGT_R4_S]: MintOpcode.MINT_CGT_R4,
    [MintOpcode.MINT_BGT_UN_R4_S]: MintOpcode.MINT_CGT_UN_R4,
    [MintOpcode.MINT_BLT_R4_S]: MintOpcode.MINT_CLT_R4,
    [MintOpcode.MINT_BLT_UN_R4_S]: MintOpcode.MINT_CLT_UN_R4,
    [MintOpcode.MINT_BGE_R4_S]: MintOpcode.MINT_CGE_R4,
    [MintOpcode.MINT_BGE_UN_R4_S]: <any>JiterpSpecialOpcode.CGE_UN_R4,
    [MintOpcode.MINT_BLE_R4_S]: MintOpcode.MINT_CLE_R4,
    [MintOpcode.MINT_BLE_UN_R4_S]: <any>JiterpSpecialOpcode.CLE_UN_R4,

    [MintOpcode.MINT_BEQ_R8_S]: MintOpcode.MINT_CEQ_R8,
    [MintOpcode.MINT_BNE_UN_R8_S]: <any>JiterpSpecialOpcode.CNE_UN_R8,
    [MintOpcode.MINT_BGT_R8_S]: MintOpcode.MINT_CGT_R8,
    [MintOpcode.MINT_BGT_UN_R8_S]: MintOpcode.MINT_CGT_UN_R8,
    [MintOpcode.MINT_BLT_R8_S]: MintOpcode.MINT_CLT_R8,
    [MintOpcode.MINT_BLT_UN_R8_S]: MintOpcode.MINT_CLT_UN_R8,
    [MintOpcode.MINT_BGE_R8_S]: MintOpcode.MINT_CGE_R8,
    [MintOpcode.MINT_BGE_UN_R8_S]: <any>JiterpSpecialOpcode.CGE_UN_R8,
    [MintOpcode.MINT_BLE_R8_S]: MintOpcode.MINT_CLE_R8,
    [MintOpcode.MINT_BLE_UN_R8_S]: <any>JiterpSpecialOpcode.CLE_UN_R8,
};

export const mathIntrinsicTable: { [opcode: number]: [isUnary: boolean, isF32: boolean, opcodeOrFuncName: WasmOpcode | string] } = {
    [MintOpcode.MINT_SQRT]:     [true, false, WasmOpcode.f64_sqrt],
    [MintOpcode.MINT_SQRTF]:    [true, true, WasmOpcode.f32_sqrt],
    [MintOpcode.MINT_CEILING]:  [true, false, WasmOpcode.f64_ceil],
    [MintOpcode.MINT_CEILINGF]: [true, true, WasmOpcode.f32_ceil],
    [MintOpcode.MINT_FLOOR]:    [true, false, WasmOpcode.f64_floor],
    [MintOpcode.MINT_FLOORF]:   [true, true, WasmOpcode.f32_floor],
    [MintOpcode.MINT_ABS]:      [true, false, WasmOpcode.f64_abs],
    [MintOpcode.MINT_ABSF]:     [true, true, WasmOpcode.f32_abs],

    [MintOpcode.MINT_ACOS]:     [true, false, "acos"],
    [MintOpcode.MINT_ACOSF]:    [true, true, "acosf"],
    [MintOpcode.MINT_ACOSH]:    [true, false, "acosh"],
    [MintOpcode.MINT_ACOSHF]:   [true, true, "acoshf"],
    [MintOpcode.MINT_COS]:      [true, false, "cos"],
    [MintOpcode.MINT_COSF]:     [true, true, "cosf"],
    [MintOpcode.MINT_ASIN]:     [true, false, "asin"],
    [MintOpcode.MINT_ASINF]:    [true, true, "asinf"],
    [MintOpcode.MINT_ASINH]:    [true, false, "asinh"],
    [MintOpcode.MINT_ASINHF]:   [true, true, "asinhf"],
    [MintOpcode.MINT_SIN]:      [true, false, "sin"],
    [MintOpcode.MINT_SINF]:     [true, true, "sinf"],
    [MintOpcode.MINT_ATAN]:     [true, false, "atan"],
    [MintOpcode.MINT_ATANF]:    [true, true, "atanf"],
    [MintOpcode.MINT_ATANH]:    [true, false, "atanh"],
    [MintOpcode.MINT_ATANHF]:   [true, true, "atanhf"],
    [MintOpcode.MINT_TAN]:      [true, false, "tan"],
    [MintOpcode.MINT_TANF]:     [true, true, "tanf"],
    [MintOpcode.MINT_CBRT]:     [true, false, "cbrt"],
    [MintOpcode.MINT_CBRTF]:    [true, true, "cbrtf"],
    [MintOpcode.MINT_EXP]:      [true, false, "exp"],
    [MintOpcode.MINT_EXPF]:     [true, true, "expf"],
    [MintOpcode.MINT_LOG]:      [true, false, "log"],
    [MintOpcode.MINT_LOGF]:     [true, true, "logf"],
    [MintOpcode.MINT_LOG2]:     [true, false, "log2"],
    [MintOpcode.MINT_LOG2F]:    [true, true, "log2f"],
    [MintOpcode.MINT_LOG10]:    [true, false, "log10"],
    [MintOpcode.MINT_LOG10F]:   [true, true, "log10f"],

    [MintOpcode.MINT_MIN]:      [false, false, WasmOpcode.f64_min],
    [MintOpcode.MINT_MINF]:     [false, true, WasmOpcode.f32_min],
    [MintOpcode.MINT_MAX]:      [false, false, WasmOpcode.f64_max],
    [MintOpcode.MINT_MAXF]:     [false, true, WasmOpcode.f32_max],

    [MintOpcode.MINT_ATAN2]:    [false, false, "atan2"],
    [MintOpcode.MINT_ATAN2F]:   [false, true, "atan2f"],
    [MintOpcode.MINT_POW]:      [false, false, "pow"],
    [MintOpcode.MINT_POWF]:     [false, true, "powf"],
    [MintOpcode.MINT_REM_R8]:   [false, false, "fmod"],
    [MintOpcode.MINT_REM_R4]:   [false, true, "fmodf"],
};

export const simdCreateSizes = {
    [MintOpcode.MINT_SIMD_V128_I1_CREATE]: 1,
    [MintOpcode.MINT_SIMD_V128_I2_CREATE]: 2,
    [MintOpcode.MINT_SIMD_V128_I4_CREATE]: 4,
    [MintOpcode.MINT_SIMD_V128_I8_CREATE]: 8,
};

export const simdCreateLoadOps = {
    [MintOpcode.MINT_SIMD_V128_I1_CREATE]: WasmOpcode.i32_load8_s,
    [MintOpcode.MINT_SIMD_V128_I2_CREATE]: WasmOpcode.i32_load16_s,
    [MintOpcode.MINT_SIMD_V128_I4_CREATE]: WasmOpcode.i32_load,
    [MintOpcode.MINT_SIMD_V128_I8_CREATE]: WasmOpcode.i64_load,
};

export const simdCreateStoreOps = {
    [MintOpcode.MINT_SIMD_V128_I1_CREATE]: WasmOpcode.i32_store8,
    [MintOpcode.MINT_SIMD_V128_I2_CREATE]: WasmOpcode.i32_store16,
    [MintOpcode.MINT_SIMD_V128_I4_CREATE]: WasmOpcode.i32_store,
    [MintOpcode.MINT_SIMD_V128_I8_CREATE]: WasmOpcode.i64_store,
};

export const simdShiftTable = new Set<SimdIntrinsic3>([
    SimdIntrinsic3.V128_I1_LEFT_SHIFT,
    SimdIntrinsic3.V128_I2_LEFT_SHIFT,
    SimdIntrinsic3.V128_I4_LEFT_SHIFT,
    SimdIntrinsic3.V128_I8_LEFT_SHIFT,

    SimdIntrinsic3.V128_I1_RIGHT_SHIFT,
    SimdIntrinsic3.V128_I2_RIGHT_SHIFT,
    SimdIntrinsic3.V128_I4_RIGHT_SHIFT,

    SimdIntrinsic3.V128_I1_URIGHT_SHIFT,
    SimdIntrinsic3.V128_I2_URIGHT_SHIFT,
    SimdIntrinsic3.V128_I4_URIGHT_SHIFT,
    SimdIntrinsic3.V128_I8_URIGHT_SHIFT,
]);

export const simdExtractTable: { [intrinsic: number]: [laneCount: number, laneStoreOpcode: WasmOpcode] } = {
    [SimdIntrinsic3.ExtractScalarI1]: [16, WasmOpcode.i32_store],
    [SimdIntrinsic3.ExtractScalarU1]: [16, WasmOpcode.i32_store],
    [SimdIntrinsic3.ExtractScalarI2]: [8, WasmOpcode.i32_store],
    [SimdIntrinsic3.ExtractScalarU2]: [8, WasmOpcode.i32_store],
    [SimdIntrinsic3.ExtractScalarD4]: [4, WasmOpcode.i32_store],
    [SimdIntrinsic3.ExtractScalarR4]: [4, WasmOpcode.f32_store],
    [SimdIntrinsic3.ExtractScalarD8]: [2, WasmOpcode.i64_store],
    [SimdIntrinsic3.ExtractScalarR8]: [2, WasmOpcode.f64_store],
};

export const simdReplaceTable: { [intrinsic: number]: [laneCount: number, laneLoadOpcode: WasmOpcode] } = {
    [SimdIntrinsic4.ReplaceScalarD1]: [16, WasmOpcode.i32_load],
    [SimdIntrinsic4.ReplaceScalarD2]: [8, WasmOpcode.i32_load],
    [SimdIntrinsic4.ReplaceScalarD4]: [4, WasmOpcode.i32_load],
    [SimdIntrinsic4.ReplaceScalarR4]: [4, WasmOpcode.f32_load],
    [SimdIntrinsic4.ReplaceScalarD8]: [2, WasmOpcode.i64_load],
    [SimdIntrinsic4.ReplaceScalarR8]: [2, WasmOpcode.f64_load],
};

export const simdLoadTable = new Set<SimdIntrinsic2>([
    SimdIntrinsic2.LoadVector128ANY,
    SimdIntrinsic2.LoadScalarAndSplatVector128X1,
    SimdIntrinsic2.LoadScalarAndSplatVector128X2,
    SimdIntrinsic2.LoadScalarAndSplatVector128X4,
    SimdIntrinsic2.LoadScalarAndSplatVector128X8,
    SimdIntrinsic2.LoadScalarVector128X4,
    SimdIntrinsic2.LoadScalarVector128X8,
    SimdIntrinsic2.LoadWideningVector128I1,
    SimdIntrinsic2.LoadWideningVector128U1,
    SimdIntrinsic2.LoadWideningVector128I2,
    SimdIntrinsic2.LoadWideningVector128U2,
    SimdIntrinsic2.LoadWideningVector128I4,
    SimdIntrinsic2.LoadWideningVector128U4,
]);

export const simdStoreTable: { [intrinsic: number]: [laneCount: number] } = {
    [SimdIntrinsic4.StoreSelectedScalarX1]: [16],
    [SimdIntrinsic4.StoreSelectedScalarX2]: [8],
    [SimdIntrinsic4.StoreSelectedScalarX4]: [4],
    [SimdIntrinsic4.StoreSelectedScalarX8]: [2],
};

export const bitmaskTable: { [intrinsic: number]: WasmSimdOpcode } = {
    [SimdIntrinsic2.V128_I1_EXTRACT_MSB]: WasmSimdOpcode.i8x16_bitmask,
    [SimdIntrinsic2.V128_I2_EXTRACT_MSB]: WasmSimdOpcode.i16x8_bitmask,
    [SimdIntrinsic2.V128_I4_EXTRACT_MSB]: WasmSimdOpcode.i32x4_bitmask,
    [SimdIntrinsic2.V128_I8_EXTRACT_MSB]: WasmSimdOpcode.i64x2_bitmask,
};

export const createScalarTable: { [intrinsic: number]: [WasmOpcode, WasmSimdOpcode] } = {
    [SimdIntrinsic2.V128_I1_CREATE_SCALAR]: [WasmOpcode.i32_load8_s, WasmSimdOpcode.i8x16_replace_lane],
    [SimdIntrinsic2.V128_I2_CREATE_SCALAR]: [WasmOpcode.i32_load16_s, WasmSimdOpcode.i16x8_replace_lane],
    [SimdIntrinsic2.V128_I4_CREATE_SCALAR]: [WasmOpcode.i32_load, WasmSimdOpcode.i32x4_replace_lane],
    [SimdIntrinsic2.V128_I8_CREATE_SCALAR]: [WasmOpcode.i64_load, WasmSimdOpcode.i64x2_replace_lane],
};
