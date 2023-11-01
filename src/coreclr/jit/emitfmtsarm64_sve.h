// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//////////////////////////////////////////////////////////////////////////////

// clang-format off
#if !defined(TARGET_ARM64)
#error Unexpected target type
#endif

#ifndef IF_DEF
#error Must define IF_DEF macro before including this file
#endif

//////////////////////////////////////////////////////////////////////////////
//
// enum insFormat   instruction            enum ID_OPS
//                  scheduling
//                  (unused)
//////////////////////////////////////////////////////////////////////////////
IF_DEF(SVE_13A, IS_NONE, NONE) // Instruction has 13 possible encoding types, type A
IF_DEF(SVE_11A, IS_NONE, NONE) // Instruction has 11 possible encoding types, type A
IF_DEF(SVE_9A, IS_NONE, NONE) // Instruction has 9  possible encoding types, type A
IF_DEF(SVE_9B, IS_NONE, NONE) // Instruction has 9  possible encoding types, type B
IF_DEF(SVE_9C, IS_NONE, NONE) // Instruction has 9  possible encoding types, type C
IF_DEF(SVE_9D, IS_NONE, NONE) // Instruction has 9  possible encoding types, type D
IF_DEF(SVE_9E, IS_NONE, NONE) // Instruction has 9  possible encoding types, type E
IF_DEF(SVE_9F, IS_NONE, NONE) // Instruction has 9  possible encoding types, type F
IF_DEF(SVE_8A, IS_NONE, NONE) // Instruction has 8  possible encoding types, type A
IF_DEF(SVE_8B, IS_NONE, NONE) // Instruction has 8  possible encoding types, type B
IF_DEF(SVE_8C, IS_NONE, NONE) // Instruction has 8  possible encoding types, type C
IF_DEF(SVE_7A, IS_NONE, NONE) // Instruction has 7  possible encoding types, type A
IF_DEF(SVE_6A, IS_NONE, NONE) // Instruction has 6  possible encoding types, type A
IF_DEF(SVE_6B, IS_NONE, NONE) // Instruction has 6  possible encoding types, type B
IF_DEF(SVE_6C, IS_NONE, NONE) // Instruction has 6  possible encoding types, type C
IF_DEF(SVE_6D, IS_NONE, NONE) // Instruction has 6  possible encoding types, type D
IF_DEF(SVE_6E, IS_NONE, NONE) // Instruction has 6  possible encoding types, type E
IF_DEF(SVE_6F, IS_NONE, NONE) // Instruction has 6  possible encoding types, type F
IF_DEF(SVE_6G, IS_NONE, NONE) // Instruction has 6  possible encoding types, type G
IF_DEF(SVE_5A, IS_NONE, NONE) // Instruction has 5  possible encoding types, type A
IF_DEF(SVE_5B, IS_NONE, NONE) // Instruction has 5  possible encoding types, type B
IF_DEF(SVE_5C, IS_NONE, NONE) // Instruction has 5  possible encoding types, type C
IF_DEF(SVE_5D, IS_NONE, NONE) // Instruction has 5  possible encoding types, type D
IF_DEF(SVE_5E, IS_NONE, NONE) // Instruction has 5  possible encoding types, type E
IF_DEF(SVE_4A, IS_NONE, NONE) // Instruction has 4  possible encoding types, type A
IF_DEF(SVE_4B, IS_NONE, NONE) // Instruction has 4  possible encoding types, type B
IF_DEF(SVE_4C, IS_NONE, NONE) // Instruction has 4  possible encoding types, type C
IF_DEF(SVE_4D, IS_NONE, NONE) // Instruction has 4  possible encoding types, type D
IF_DEF(SVE_4E, IS_NONE, NONE) // Instruction has 4  possible encoding types, type E
IF_DEF(SVE_4F, IS_NONE, NONE) // Instruction has 4  possible encoding types, type F
IF_DEF(SVE_4G, IS_NONE, NONE) // Instruction has 4  possible encoding types, type G
IF_DEF(SVE_4H, IS_NONE, NONE) // Instruction has 4  possible encoding types, type H
IF_DEF(SVE_4I, IS_NONE, NONE) // Instruction has 4  possible encoding types, type I
IF_DEF(SVE_4J, IS_NONE, NONE) // Instruction has 4  possible encoding types, type J
IF_DEF(SVE_4K, IS_NONE, NONE) // Instruction has 4  possible encoding types, type K
IF_DEF(SVE_4L, IS_NONE, NONE) // Instruction has 4  possible encoding types, type L
IF_DEF(SVE_3A, IS_NONE, NONE) // Instruction has 3  possible encoding types, type A
IF_DEF(SVE_3B, IS_NONE, NONE) // Instruction has 3  possible encoding types, type B
IF_DEF(SVE_3C, IS_NONE, NONE) // Instruction has 3  possible encoding types, type C
IF_DEF(SVE_3D, IS_NONE, NONE) // Instruction has 3  possible encoding types, type D
IF_DEF(SVE_3E, IS_NONE, NONE) // Instruction has 3  possible encoding types, type E
IF_DEF(SVE_3F, IS_NONE, NONE) // Instruction has 3  possible encoding types, type F
IF_DEF(SVE_3G, IS_NONE, NONE) // Instruction has 3  possible encoding types, type G
IF_DEF(SVE_3H, IS_NONE, NONE) // Instruction has 3  possible encoding types, type H
IF_DEF(SVE_3I, IS_NONE, NONE) // Instruction has 3  possible encoding types, type I
IF_DEF(SVE_3J, IS_NONE, NONE) // Instruction has 3  possible encoding types, type J
IF_DEF(SVE_3K, IS_NONE, NONE) // Instruction has 3  possible encoding types, type K
IF_DEF(SVE_3L, IS_NONE, NONE) // Instruction has 3  possible encoding types, type L
IF_DEF(SVE_3M, IS_NONE, NONE) // Instruction has 3  possible encoding types, type M
IF_DEF(SVE_3N, IS_NONE, NONE) // Instruction has 3  possible encoding types, type N
IF_DEF(SVE_3O, IS_NONE, NONE) // Instruction has 3  possible encoding types, type O
IF_DEF(SVE_3P, IS_NONE, NONE) // Instruction has 3  possible encoding types, type P
IF_DEF(SVE_3Q, IS_NONE, NONE) // Instruction has 3  possible encoding types, type Q
IF_DEF(SVE_3R, IS_NONE, NONE) // Instruction has 3  possible encoding types, type R
IF_DEF(SVE_3S, IS_NONE, NONE) // Instruction has 3  possible encoding types, type S
IF_DEF(SVE_3T, IS_NONE, NONE) // Instruction has 3  possible encoding types, type T
IF_DEF(SVE_3U, IS_NONE, NONE) // Instruction has 3  possible encoding types, type U
IF_DEF(SVE_3V, IS_NONE, NONE) // Instruction has 3  possible encoding types, type V
IF_DEF(SVE_2AA, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AA
IF_DEF(SVE_2AB, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AB
IF_DEF(SVE_2AC, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AC
IF_DEF(SVE_2AD, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AD
IF_DEF(SVE_2AE, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AE
IF_DEF(SVE_2AF, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AF
IF_DEF(SVE_2AG, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AG
IF_DEF(SVE_2AH, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AH
IF_DEF(SVE_2AI, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AI
IF_DEF(SVE_2AJ, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AJ
IF_DEF(SVE_2AK, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AK
IF_DEF(SVE_2AL, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AL
IF_DEF(SVE_2AM, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AM
IF_DEF(SVE_2AN, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AN
IF_DEF(SVE_2AO, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AO
IF_DEF(SVE_2AP, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AP
IF_DEF(SVE_2AQ, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AQ
IF_DEF(SVE_2AR, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AR
IF_DEF(SVE_2AS, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AS
IF_DEF(SVE_2AT, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AT
IF_DEF(SVE_2AU, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AU
IF_DEF(SVE_2AV, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AV
IF_DEF(SVE_2AW, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AW
IF_DEF(SVE_2AX, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AX
IF_DEF(SVE_2AY, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AY
IF_DEF(SVE_2AZ, IS_NONE, NONE) // Instruction has 2  possible encoding types, type AZ
IF_DEF(SVE_2BA, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BA
IF_DEF(SVE_2BB, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BB
IF_DEF(SVE_2BC, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BC
IF_DEF(SVE_2BD, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BD
IF_DEF(SVE_2BE, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BE
IF_DEF(SVE_2BF, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BF
IF_DEF(SVE_2BG, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BG
IF_DEF(SVE_2BH, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BH
IF_DEF(SVE_2BI, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BI
IF_DEF(SVE_2BJ, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BJ
IF_DEF(SVE_2BK, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BK
IF_DEF(SVE_2BL, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BL
IF_DEF(SVE_2BM, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BM
IF_DEF(SVE_2BN, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BN
IF_DEF(SVE_2BO, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BO
IF_DEF(SVE_2BP, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BP
IF_DEF(SVE_2BQ, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BQ
IF_DEF(SVE_2BR, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BR
IF_DEF(SVE_2BS, IS_NONE, NONE) // Instruction has 2  possible encoding types, type BS

/*****************************************************************************
*           SVE_WW_XY
*           WW      -- code
*           X       -- register count
*           Y       -- unique id

*****************************************************************************/

IF_DEF(SVE_AA_3A,   IS_NONE, NONE) // SVE_AA_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_bin_pred_log
IF_DEF(SVE_AB_3A,   IS_NONE, NONE) // SVE_AB_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_bin_pred_arit_0
IF_DEF(SVE_AB_3B,   IS_NONE, NONE) // SVE_AB_3B  ................ ...gggmmmmmddddd  -- sve_int_bin_pred_arit_0
IF_DEF(SVE_AC_3A,   IS_NONE, NONE) // SVE_AC_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_bin_pred_div
IF_DEF(SVE_AD_3A,   IS_NONE, NONE) // SVE_AD_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_bin_pred_arit_1
IF_DEF(SVE_AE_3A,   IS_NONE, NONE) // SVE_AE_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_bin_pred_arit_2
IF_DEF(SVE_AF_3A,   IS_NONE, NONE) // SVE_AF_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_reduce_2
IF_DEF(SVE_AG_3A,   IS_NONE, NONE) // SVE_AG_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_reduce_2q
IF_DEF(SVE_AH_3A,   IS_NONE, NONE) // SVE_AH_3A  ........xx.....M ...gggnnnnnddddd  -- sve_int_movprfx_pred
IF_DEF(SVE_AI_3A,   IS_NONE, NONE) // SVE_AI_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_reduce_0
IF_DEF(SVE_AJ_3A,   IS_NONE, NONE) // SVE_AJ_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_reduce_0q
IF_DEF(SVE_AK_3A,   IS_NONE, NONE) // SVE_AK_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_reduce_1
IF_DEF(SVE_AL_3A,   IS_NONE, NONE) // SVE_AL_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_reduce_1q
IF_DEF(SVE_AM_2A,   IS_NONE, NONE) // SVE_AM_2A  ........xx...... ...gggxxiiiddddd  -- sve_int_bin_pred_shift_0
IF_DEF(SVE_AN_3A,   IS_NONE, NONE) // SVE_AN_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_bin_pred_shift_1
IF_DEF(SVE_AO_3A,   IS_NONE, NONE) // SVE_AO_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_bin_pred_shift_2
IF_DEF(SVE_AP_3A,   IS_NONE, NONE) // SVE_AP_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_un_pred_arit_1
IF_DEF(SVE_AQ_3A,   IS_NONE, NONE) // SVE_AQ_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_un_pred_arit_0
IF_DEF(SVE_AR_4A,   IS_NONE, NONE) // SVE_AR_4A  ........xx.mmmmm ...gggnnnnnddddd  -- sve_int_mlas_vvv_pred
IF_DEF(SVE_AS_4A,   IS_NONE, NONE) // SVE_AS_4A  ........xx.mmmmm ...gggaaaaaddddd  -- sve_int_mladdsub_vvv_pred
IF_DEF(SVE_AT_3A,   IS_NONE, NONE) // SVE_AT_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_bin_cons_arit_0
IF_DEF(SVE_AT_3B,   IS_NONE, NONE) // SVE_AT_3B  ...........mmmmm ......nnnnnddddd  -- sve_int_bin_cons_arit_0
IF_DEF(SVE_AU_3A,   IS_NONE, NONE) // SVE_AU_3A  ...........mmmmm ......nnnnnddddd  -- sve_int_bin_cons_log
IF_DEF(SVE_AV_3A,   IS_NONE, NONE) // SVE_AV_3A  ...........mmmmm ......kkkkkddddd  -- sve_int_tern_log
IF_DEF(SVE_AW_2A,   IS_NONE, NONE) // SVE_AW_2A  ........xx.xxiii ......mmmmmddddd  -- sve_int_rotate_imm
IF_DEF(SVE_AX_1A,   IS_NONE, NONE) // SVE_AX_1A  ........xx.iiiii ......iiiiiddddd  -- sve_int_index_ii
IF_DEF(SVE_AY_2A,   IS_NONE, NONE) // SVE_AY_2A  ........xx.mmmmm ......iiiiiddddd  -- sve_int_index_ir
IF_DEF(SVE_AZ_2A,   IS_NONE, NONE) // SVE_AZ_2A  ........xx.iiiii ......nnnnnddddd  -- sve_int_index_ri
IF_DEF(SVE_BA_3A,   IS_NONE, NONE) // SVE_BA_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_index_rr
IF_DEF(SVE_BB_2A,   IS_NONE, NONE) // SVE_BB_2A  ...........nnnnn .....iiiiiiddddd  -- sve_int_arith_vl
IF_DEF(SVE_BC_1A,   IS_NONE, NONE) // SVE_BC_1A  ................ .....iiiiiiddddd  -- sve_int_read_vl_a
IF_DEF(SVE_BD_3A,   IS_NONE, NONE) // SVE_BD_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_mul_b
IF_DEF(SVE_BD_3B,   IS_NONE, NONE) // SVE_BD_3B  ...........mmmmm ......nnnnnddddd  -- sve_int_mul_b
IF_DEF(SVE_BE_3A,   IS_NONE, NONE) // SVE_BE_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_sqdmulh
IF_DEF(SVE_BF_2A,   IS_NONE, NONE) // SVE_BF_2A  ........xx.xxiii ......nnnnnddddd  -- sve_int_bin_cons_shift_b
IF_DEF(SVE_BG_3A,   IS_NONE, NONE) // SVE_BG_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_bin_cons_shift_a
IF_DEF(SVE_BH_3A,   IS_NONE, NONE) // SVE_BH_3A  .........x.mmmmm ....hhnnnnnddddd  -- sve_int_bin_cons_misc_0_a
IF_DEF(SVE_BH_3B,   IS_NONE, NONE) // SVE_BH_3B  ...........mmmmm ....hhnnnnnddddd  -- sve_int_bin_cons_misc_0_a
IF_DEF(SVE_BH_3B_A, IS_NONE, NONE) // SVE_BH_3B_A  ...........mmmmm ....hhnnnnnddddd  -- 
IF_DEF(SVE_BI_2A,   IS_NONE, NONE) // SVE_BI_2A  ................ ......nnnnnddddd  -- sve_int_bin_cons_misc_0_d
IF_DEF(SVE_BJ_2A,   IS_NONE, NONE) // SVE_BJ_2A  ........xx...... ......nnnnnddddd  -- sve_int_bin_cons_misc_0_c
IF_DEF(SVE_BK_3A,   IS_NONE, NONE) // SVE_BK_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_bin_cons_misc_0_b
IF_DEF(SVE_BL_1A,   IS_NONE, NONE) // SVE_BL_1A  ............iiii ......pppppddddd  -- sve_int_count
IF_DEF(SVE_BM_1A,   IS_NONE, NONE) // SVE_BM_1A  ............iiii ......pppppddddd  -- sve_int_pred_pattern_a
IF_DEF(SVE_BN_1A,   IS_NONE, NONE) // SVE_BN_1A  ............iiii ......pppppddddd  -- sve_int_countvlv1
IF_DEF(SVE_BO_1A,   IS_NONE, NONE) // SVE_BO_1A  ...........Xiiii ......pppppddddd  -- sve_int_pred_pattern_b
IF_DEF(SVE_BO_1A_A, IS_NONE, NONE) // SVE_BO_1A_A  ...........Xiiii ......pppppddddd  -- 
IF_DEF(SVE_BP_1A,   IS_NONE, NONE) // SVE_BP_1A  ............iiii ......pppppddddd  -- sve_int_countvlv0
IF_DEF(SVE_BQ_2A,   IS_NONE, NONE) // SVE_BQ_2A  ...........iiiii ...iiinnnnnddddd  -- sve_int_perm_extract_i
IF_DEF(SVE_BQ_2B,   IS_NONE, NONE) // SVE_BQ_2B  ...........iiiii ...iiimmmmmddddd  -- sve_int_perm_extract_i
IF_DEF(SVE_BR_3A,   IS_NONE, NONE) // SVE_BR_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_perm_bin_long_perm_zz
IF_DEF(SVE_BR_3B,   IS_NONE, NONE) // SVE_BR_3B  ...........mmmmm ......nnnnnddddd  -- sve_int_perm_bin_long_perm_zz
IF_DEF(SVE_BS_1A,   IS_NONE, NONE) // SVE_BS_1A  ..............ii iiiiiiiiiiiddddd  -- sve_int_log_imm
IF_DEF(SVE_BT_1A,   IS_NONE, NONE) // SVE_BT_1A  ..............ii iiiiiiiiiiiddddd  -- sve_int_dup_mask_imm
IF_DEF(SVE_BU_2A,   IS_NONE, NONE) // SVE_BU_2A  ........xx..gggg ...iiiiiiiiddddd  -- sve_int_dup_fpimm_pred
IF_DEF(SVE_BV_2A,   IS_NONE, NONE) // SVE_BV_2A  ........xx..gggg ..hiiiiiiiiddddd  -- sve_int_dup_imm_pred
IF_DEF(SVE_BV_2A_A, IS_NONE, NONE) // SVE_BV_2A_A  ........xx..gggg ..hiiiiiiiiddddd  -- 
IF_DEF(SVE_BV_2B,   IS_NONE, NONE) // SVE_BV_2B  ........xx..gggg ...........ddddd  -- sve_int_dup_imm_pred
IF_DEF(SVE_BV_2A_J, IS_NONE, NONE) // SVE_BV_2A_J  ........xx..gggg ..hiiiiiiiiddddd  -- 
IF_DEF(SVE_BW_2A,   IS_NONE, NONE) // SVE_BW_2A  ........ii.xxxxx ......nnnnnddddd  -- sve_int_perm_dup_i
IF_DEF(SVE_BX_2A,   IS_NONE, NONE) // SVE_BX_2A  ...........ixxxx ......nnnnnddddd  -- sve_int_perm_dupq_i
IF_DEF(SVE_BY_2A,   IS_NONE, NONE) // SVE_BY_2A  ............iiii ......mmmmmddddd  -- sve_int_perm_extq
IF_DEF(SVE_BZ_3A,   IS_NONE, NONE) // SVE_BZ_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_perm_tbl_3src
IF_DEF(SVE_BZ_3A_A, IS_NONE, NONE) // SVE_BZ_3A_A  ........xx.mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_CA_3A,   IS_NONE, NONE) // SVE_CA_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_perm_tbxquads
IF_DEF(SVE_CB_2A,   IS_NONE, NONE) // SVE_CB_2A  ........xx...... ......nnnnnddddd  -- sve_int_perm_dup_r
IF_DEF(SVE_CC_2A,   IS_NONE, NONE) // SVE_CC_2A  ........xx...... ......mmmmmddddd  -- sve_int_perm_insrv
IF_DEF(SVE_CD_2A,   IS_NONE, NONE) // SVE_CD_2A  ........xx...... ......mmmmmddddd  -- sve_int_perm_insrs
IF_DEF(SVE_CE_2A,   IS_NONE, NONE) // SVE_CE_2A  ................ ......nnnnn.DDDD  -- sve_int_mov_v2p
IF_DEF(SVE_CE_2B,   IS_NONE, NONE) // SVE_CE_2B  .........i...ii. ......nnnnn.DDDD  -- sve_int_mov_v2p
IF_DEF(SVE_CE_2C,   IS_NONE, NONE) // SVE_CE_2C  ..............i. ......nnnnn.DDDD  -- sve_int_mov_v2p
IF_DEF(SVE_CE_2D,   IS_NONE, NONE) // SVE_CE_2D  .............ii. ......nnnnn.DDDD  -- sve_int_mov_v2p
IF_DEF(SVE_CF_2A,   IS_NONE, NONE) // SVE_CF_2A  ................ .......NNNNddddd  -- sve_int_mov_p2v
IF_DEF(SVE_CF_2B,   IS_NONE, NONE) // SVE_CF_2B  .........i...ii. .......NNNNddddd  -- sve_int_mov_p2v
IF_DEF(SVE_CF_2C,   IS_NONE, NONE) // SVE_CF_2C  ..............i. .......NNNNddddd  -- sve_int_mov_p2v
IF_DEF(SVE_CF_2D,   IS_NONE, NONE) // SVE_CF_2D  .............ii. .......NNNNddddd  -- sve_int_mov_p2v
IF_DEF(SVE_CG_2A,   IS_NONE, NONE) // SVE_CG_2A  ........xx...... ......nnnnnddddd  -- sve_int_perm_reverse_z
IF_DEF(SVE_CH_2A,   IS_NONE, NONE) // SVE_CH_2A  ........xx...... ......nnnnnddddd  -- sve_int_perm_unpk
IF_DEF(SVE_CI_3A,   IS_NONE, NONE) // SVE_CI_3A  ........xx..MMMM .......NNNN.DDDD  -- sve_int_perm_bin_perm_pp
IF_DEF(SVE_CJ_2A,   IS_NONE, NONE) // SVE_CJ_2A  ........xx...... .......NNNN.DDDD  -- sve_int_perm_reverse_p
IF_DEF(SVE_CK_2A,   IS_NONE, NONE) // SVE_CK_2A  ................ .......NNNN.DDDD  -- sve_int_perm_punpk
IF_DEF(SVE_CL_3A,   IS_NONE, NONE) // SVE_CL_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_perm_compact
IF_DEF(SVE_CM_3A,   IS_NONE, NONE) // SVE_CM_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_perm_clast_zz
IF_DEF(SVE_CN_3A,   IS_NONE, NONE) // SVE_CN_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_perm_clast_vz
IF_DEF(SVE_CO_3A,   IS_NONE, NONE) // SVE_CO_3A  ........xx...... ...gggmmmmmddddd  -- sve_int_perm_clast_rz
IF_DEF(SVE_CP_3A,   IS_NONE, NONE) // SVE_CP_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_perm_cpy_v
IF_DEF(SVE_CQ_3A,   IS_NONE, NONE) // SVE_CQ_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_perm_cpy_r
IF_DEF(SVE_CR_3A,   IS_NONE, NONE) // SVE_CR_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_perm_last_v
IF_DEF(SVE_CS_3A,   IS_NONE, NONE) // SVE_CS_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_perm_last_r
IF_DEF(SVE_CT_3A,   IS_NONE, NONE) // SVE_CT_3A  ................ ...gggnnnnnddddd  -- sve_int_perm_revd
IF_DEF(SVE_CU_3A,   IS_NONE, NONE) // SVE_CU_3A  ........xx...... ...gggnnnnnddddd  -- sve_int_perm_rev
IF_DEF(SVE_CV_3A,   IS_NONE, NONE) // SVE_CV_3A  ........xx...... ...VVVnnnnnddddd  -- sve_int_perm_splice
IF_DEF(SVE_CV_3B,   IS_NONE, NONE) // SVE_CV_3B  ........xx...... ...VVVmmmmmddddd  -- sve_int_perm_splice
IF_DEF(SVE_CW_4A,   IS_NONE, NONE) // SVE_CW_4A  ........xx.mmmmm ..VVVVnnnnnddddd  -- sve_int_sel_vvv
IF_DEF(SVE_CX_4A,   IS_NONE, NONE) // SVE_CX_4A  ........xx.mmmmm ...gggnnnnn.DDDD  -- sve_int_cmp_0
IF_DEF(SVE_CX_4A_A, IS_NONE, NONE) // SVE_CX_4A_A  ........xx.mmmmm ...gggnnnnn.DDDD  -- 
IF_DEF(SVE_CY_3A,   IS_NONE, NONE) // SVE_CY_3A  ........xx.iiiii ...gggnnnnn.DDDD  -- sve_int_ucmp_vi
IF_DEF(SVE_CY_3B,   IS_NONE, NONE) // SVE_CY_3B  ........xx.iiiii ii.gggnnnnn.DDDD  -- sve_int_ucmp_vi
IF_DEF(SVE_CZ_4A,   IS_NONE, NONE) // SVE_CZ_4A  ............MMMM ..gggg.NNNN.DDDD  -- sve_int_pred_log
IF_DEF(SVE_CZ_4A_K, IS_NONE, NONE) // SVE_CZ_4A_K  ............MMMM ..gggg.NNNN.DDDD  -- 
IF_DEF(SVE_CZ_4A_L, IS_NONE, NONE) // SVE_CZ_4A_L  ............MMMM ..gggg.NNNN.DDDD  -- 
IF_DEF(SVE_CZ_4A_A, IS_NONE, NONE) // SVE_CZ_4A_A  ............MMMM ..gggg.NNNN.DDDD  -- 
IF_DEF(SVE_DA_4A,   IS_NONE, NONE) // SVE_DA_4A  ............MMMM ..gggg.NNNN.DDDD  -- sve_int_brkp
IF_DEF(SVE_DB_3A,   IS_NONE, NONE) // SVE_DB_3A  ................ ..gggg.NNNNMDDDD  -- sve_int_break
IF_DEF(SVE_DB_3B,   IS_NONE, NONE) // SVE_DB_3B  ................ ..gggg.NNNN.DDDD  -- sve_int_break
IF_DEF(SVE_DC_3A,   IS_NONE, NONE) // SVE_DC_3A  ................ ..gggg.NNNN.MMMM  -- sve_int_brkn
IF_DEF(SVE_DD_2A,   IS_NONE, NONE) // SVE_DD_2A  ................ .......gggg.DDDD  -- sve_int_pfirst
IF_DEF(SVE_DE_1A,   IS_NONE, NONE) // SVE_DE_1A  ........xx...... ......ppppp.DDDD  -- sve_int_ptrue
IF_DEF(SVE_DF_2A,   IS_NONE, NONE) // SVE_DF_2A  ........xx...... .......VVVV.DDDD  -- sve_int_pnext
IF_DEF(SVE_DG_2A,   IS_NONE, NONE) // SVE_DG_2A  ................ .......gggg.DDDD  -- sve_int_rdffr
IF_DEF(SVE_DH_1A,   IS_NONE, NONE) // SVE_DH_1A  ................ ............DDDD  -- sve_int_rdffr_2
IF_DEF(SVE_DI_2A,   IS_NONE, NONE) // SVE_DI_2A  ................ ..gggg.NNNN.....  -- sve_int_ptest
IF_DEF(SVE_DJ_1A,   IS_NONE, NONE) // SVE_DJ_1A  ................ ............DDDD  -- sve_int_pfalse
IF_DEF(SVE_DK_3A,   IS_NONE, NONE) // SVE_DK_3A  ........xx...... ..gggg.NNNNddddd  -- sve_int_pcount_pred
IF_DEF(SVE_DL_2A,   IS_NONE, NONE) // SVE_DL_2A  ........xx...... .....l.NNNNddddd  -- sve_int_pcount_pn
IF_DEF(SVE_DM_2A,   IS_NONE, NONE) // SVE_DM_2A  ........xx...... .......MMMMddddd  -- sve_int_count_r
IF_DEF(SVE_DN_2A,   IS_NONE, NONE) // SVE_DN_2A  ........xx...... .......MMMMddddd  -- sve_int_count_v
IF_DEF(SVE_DO_2A,   IS_NONE, NONE) // SVE_DO_2A  ........xx...... .....X.MMMMddddd  -- sve_int_count_r_sat
IF_DEF(SVE_DO_2A_A, IS_NONE, NONE) // SVE_DO_2A_A  ........xx...... .....X.MMMMddddd  -- 
IF_DEF(SVE_DP_2A,   IS_NONE, NONE) // SVE_DP_2A  ........xx...... .......MMMMddddd  -- sve_int_count_v_sat
IF_DEF(SVE_DQ_0A,   IS_NONE, NONE) // SVE_DQ_0A  ................ ................  -- sve_int_setffr
IF_DEF(SVE_DR_1A,   IS_NONE, NONE) // SVE_DR_1A  ................ .......NNNN.....  -- sve_int_wrffr
IF_DEF(SVE_DS_2A,   IS_NONE, NONE) // SVE_DS_2A  .........x.mmmmm ......nnnnn.....  -- sve_int_cterm
IF_DEF(SVE_DT_3A,   IS_NONE, NONE) // SVE_DT_3A  ........xx.mmmmm ...X..nnnnn.DDDD  -- sve_int_while_rr
IF_DEF(SVE_DU_3A,   IS_NONE, NONE) // SVE_DU_3A  ........xx.mmmmm ......nnnnn.DDDD  -- sve_int_whilenc
IF_DEF(SVE_DV_4A,   IS_NONE, NONE) // SVE_DV_4A  ........ix.xxxvv ..NNNN.MMMM.DDDD  -- sve_int_pred_dup
IF_DEF(SVE_DW_2A,   IS_NONE, NONE) // SVE_DW_2A  ........xx...... ......iiNNN.DDDD  -- sve_int_ctr_to_mask
IF_DEF(SVE_DW_2B,   IS_NONE, NONE) // SVE_DW_2B  ........xx...... .......iNNN.DDDD  -- sve_int_ctr_to_mask
IF_DEF(SVE_DX_3A,   IS_NONE, NONE) // SVE_DX_3A  ........xx.mmmmm ......nnnnn.DDD.  -- sve_int_while_rr_pair
IF_DEF(SVE_DY_3A,   IS_NONE, NONE) // SVE_DY_3A  ........xx.mmmmm ..l...nnnnn..DDD  -- sve_int_while_rr_pn
IF_DEF(SVE_DZ_1A,   IS_NONE, NONE) // SVE_DZ_1A  ........xx...... .............DDD  -- sve_int_pn_ptrue
IF_DEF(SVE_EA_1A,   IS_NONE, NONE) // SVE_EA_1A  ........xx...... ...iiiiiiiiddddd  -- sve_int_dup_fpimm
IF_DEF(SVE_EB_1A,   IS_NONE, NONE) // SVE_EB_1A  ........xx...... ..hiiiiiiiiddddd  -- sve_int_dup_imm
IF_DEF(SVE_EB_1B,   IS_NONE, NONE) // SVE_EB_1B  ........xx...... ...........ddddd  -- sve_int_dup_imm
IF_DEF(SVE_EC_1A,   IS_NONE, NONE) // SVE_EC_1A  ........xx...... ..hiiiiiiiiddddd  -- sve_int_arith_imm0
IF_DEF(SVE_ED_1A,   IS_NONE, NONE) // SVE_ED_1A  ........xx...... ...iiiiiiiiddddd  -- sve_int_arith_imm1
IF_DEF(SVE_EE_1A,   IS_NONE, NONE) // SVE_EE_1A  ........xx...... ...iiiiiiiiddddd  -- sve_int_arith_imm2
IF_DEF(SVE_EF_3A,   IS_NONE, NONE) // SVE_EF_3A  ...........mmmmm ......nnnnnddddd  -- sve_intx_dot2
IF_DEF(SVE_EG_3A,   IS_NONE, NONE) // SVE_EG_3A  ...........iimmm ......nnnnnddddd  -- sve_intx_dot2_by_indexed_elem
IF_DEF(SVE_EH_3A,   IS_NONE, NONE) // SVE_EH_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_dot
IF_DEF(SVE_EI_3A,   IS_NONE, NONE) // SVE_EI_3A  ...........mmmmm ......nnnnnddddd  -- sve_intx_mixed_dot
IF_DEF(SVE_EJ_3A,   IS_NONE, NONE) // SVE_EJ_3A  ........xx.mmmmm ....rrnnnnnddddd  -- sve_intx_cdot
IF_DEF(SVE_EK_3A,   IS_NONE, NONE) // SVE_EK_3A  ........xx.mmmmm ....rrnnnnnddddd  -- sve_intx_cmla
IF_DEF(SVE_EL_3A,   IS_NONE, NONE) // SVE_EL_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_mlal_long
IF_DEF(SVE_EM_3A,   IS_NONE, NONE) // SVE_EM_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_qrdmlah
IF_DEF(SVE_EN_3A,   IS_NONE, NONE) // SVE_EN_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_qdmlalbt
IF_DEF(SVE_EO_3A,   IS_NONE, NONE) // SVE_EO_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_qdmlal_long
IF_DEF(SVE_EP_3A,   IS_NONE, NONE) // SVE_EP_3A  ........xx...... ...gggmmmmmddddd  -- sve_intx_pred_arith_binary
IF_DEF(SVE_EQ_3A,   IS_NONE, NONE) // SVE_EQ_3A  ........xx...... ...gggnnnnnddddd  -- sve_intx_accumulate_long_pairs
IF_DEF(SVE_ER_3A,   IS_NONE, NONE) // SVE_ER_3A  ........xx...... ...gggmmmmmddddd  -- sve_intx_arith_binary_pairs
IF_DEF(SVE_ES_3A,   IS_NONE, NONE) // SVE_ES_3A  ........xx...... ...gggnnnnnddddd  -- sve_intx_pred_arith_unary
IF_DEF(SVE_ET_3A,   IS_NONE, NONE) // SVE_ET_3A  ........xx...... ...gggmmmmmddddd  -- sve_intx_pred_arith_binary_sat
IF_DEF(SVE_EU_3A,   IS_NONE, NONE) // SVE_EU_3A  ........xx...... ...gggmmmmmddddd  -- sve_intx_bin_pred_shift_sat_round
IF_DEF(SVE_EV_3A,   IS_NONE, NONE) // SVE_EV_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_clamp
IF_DEF(SVE_EW_3A,   IS_NONE, NONE) // SVE_EW_3A  ...........mmmmm ......nnnnnddddd  -- sve_ptr_muladd_unpred
IF_DEF(SVE_EW_3B,   IS_NONE, NONE) // SVE_EW_3B  ...........mmmmm ......aaaaaddddd  -- sve_ptr_muladd_unpred
IF_DEF(SVE_EX_3A,   IS_NONE, NONE) // SVE_EX_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_perm_binquads
IF_DEF(SVE_EY_3A,   IS_NONE, NONE) // SVE_EY_3A  ...........iimmm ......nnnnnddddd  -- sve_intx_dot_by_indexed_elem
IF_DEF(SVE_EY_3B,   IS_NONE, NONE) // SVE_EY_3B  ...........immmm ......nnnnnddddd  -- sve_intx_dot_by_indexed_elem
IF_DEF(SVE_EZ_3A,   IS_NONE, NONE) // SVE_EZ_3A  ...........iimmm ......nnnnnddddd  -- sve_intx_mixed_dot_by_indexed_elem
IF_DEF(SVE_FA_3A,   IS_NONE, NONE) // SVE_FA_3A  ...........iimmm ....rrnnnnnddddd  -- sve_intx_cdot_by_indexed_elem
IF_DEF(SVE_FA_3B,   IS_NONE, NONE) // SVE_FA_3B  ...........immmm ....rrnnnnnddddd  -- sve_intx_cdot_by_indexed_elem
IF_DEF(SVE_FB_3A,   IS_NONE, NONE) // SVE_FB_3A  ...........iimmm ....rrnnnnnddddd  -- sve_intx_cmla_by_indexed_elem
IF_DEF(SVE_FB_3B,   IS_NONE, NONE) // SVE_FB_3B  ...........immmm ....rrnnnnnddddd  -- sve_intx_cmla_by_indexed_elem
IF_DEF(SVE_FC_3A,   IS_NONE, NONE) // SVE_FC_3A  ...........iimmm ....rrnnnnnddddd  -- sve_intx_qrdcmla_by_indexed_elem
IF_DEF(SVE_FC_3B,   IS_NONE, NONE) // SVE_FC_3B  ...........immmm ....rrnnnnnddddd  -- sve_intx_qrdcmla_by_indexed_elem
IF_DEF(SVE_FD_3A,   IS_NONE, NONE) // SVE_FD_3A  .........i.iimmm ......nnnnnddddd  -- sve_intx_mul_by_indexed_elem
IF_DEF(SVE_FD_3B,   IS_NONE, NONE) // SVE_FD_3B  ...........iimmm ......nnnnnddddd  -- sve_intx_mul_by_indexed_elem
IF_DEF(SVE_FD_3C,   IS_NONE, NONE) // SVE_FD_3C  ...........immmm ......nnnnnddddd  -- sve_intx_mul_by_indexed_elem
IF_DEF(SVE_FE_3A,   IS_NONE, NONE) // SVE_FE_3A  ...........iimmm ....i.nnnnnddddd  -- sve_intx_mul_long_by_indexed_elem
IF_DEF(SVE_FE_3B,   IS_NONE, NONE) // SVE_FE_3B  ...........immmm ....i.nnnnnddddd  -- sve_intx_mul_long_by_indexed_elem
IF_DEF(SVE_FF_3A,   IS_NONE, NONE) // SVE_FF_3A  .........i.iimmm ......nnnnnddddd  -- sve_intx_mla_by_indexed_elem
IF_DEF(SVE_FF_3B,   IS_NONE, NONE) // SVE_FF_3B  ...........iimmm ......nnnnnddddd  -- sve_intx_mla_by_indexed_elem
IF_DEF(SVE_FF_3C,   IS_NONE, NONE) // SVE_FF_3C  ...........immmm ......nnnnnddddd  -- sve_intx_mla_by_indexed_elem
IF_DEF(SVE_FG_3A,   IS_NONE, NONE) // SVE_FG_3A  ...........iimmm ....i.nnnnnddddd  -- sve_intx_mla_long_by_indexed_elem
IF_DEF(SVE_FG_3B,   IS_NONE, NONE) // SVE_FG_3B  ...........immmm ....i.nnnnnddddd  -- sve_intx_mla_long_by_indexed_elem
IF_DEF(SVE_FH_3A,   IS_NONE, NONE) // SVE_FH_3A  ...........iimmm ....i.nnnnnddddd  -- sve_intx_qdmul_long_by_indexed_elem
IF_DEF(SVE_FH_3B,   IS_NONE, NONE) // SVE_FH_3B  ...........immmm ....i.nnnnnddddd  -- sve_intx_qdmul_long_by_indexed_elem
IF_DEF(SVE_FI_3A,   IS_NONE, NONE) // SVE_FI_3A  .........i.iimmm ......nnnnnddddd  -- sve_intx_qdmulh_by_indexed_elem
IF_DEF(SVE_FI_3B,   IS_NONE, NONE) // SVE_FI_3B  ...........iimmm ......nnnnnddddd  -- sve_intx_qdmulh_by_indexed_elem
IF_DEF(SVE_FI_3C,   IS_NONE, NONE) // SVE_FI_3C  ...........immmm ......nnnnnddddd  -- sve_intx_qdmulh_by_indexed_elem
IF_DEF(SVE_FJ_3A,   IS_NONE, NONE) // SVE_FJ_3A  ...........iimmm ....i.nnnnnddddd  -- sve_intx_qdmla_long_by_indexed_elem
IF_DEF(SVE_FJ_3B,   IS_NONE, NONE) // SVE_FJ_3B  ...........immmm ....i.nnnnnddddd  -- sve_intx_qdmla_long_by_indexed_elem
IF_DEF(SVE_FK_3A,   IS_NONE, NONE) // SVE_FK_3A  .........i.iimmm ......nnnnnddddd  -- sve_intx_qrdmlah_by_indexed_elem
IF_DEF(SVE_FK_3B,   IS_NONE, NONE) // SVE_FK_3B  ...........iimmm ......nnnnnddddd  -- sve_intx_qrdmlah_by_indexed_elem
IF_DEF(SVE_FK_3C,   IS_NONE, NONE) // SVE_FK_3C  ...........immmm ......nnnnnddddd  -- sve_intx_qrdmlah_by_indexed_elem
IF_DEF(SVE_FL_3A,   IS_NONE, NONE) // SVE_FL_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_cons_arith_long
IF_DEF(SVE_FM_3A,   IS_NONE, NONE) // SVE_FM_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_cons_arith_wide
IF_DEF(SVE_FN_3A,   IS_NONE, NONE) // SVE_FN_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_cons_mul_long
IF_DEF(SVE_FN_3B,   IS_NONE, NONE) // SVE_FN_3B  ...........mmmmm ......nnnnnddddd  -- sve_intx_cons_mul_long
IF_DEF(SVE_FO_3A,   IS_NONE, NONE) // SVE_FO_3A  ...........mmmmm ......nnnnnddddd  -- sve_intx_mmla
IF_DEF(SVE_FP_3A,   IS_NONE, NONE) // SVE_FP_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_eorx
IF_DEF(SVE_FQ_3A,   IS_NONE, NONE) // SVE_FQ_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_perm_bit
IF_DEF(SVE_FR_2A,   IS_NONE, NONE) // SVE_FR_2A  .........x.xxiii ......nnnnnddddd  -- sve_intx_shift_long
IF_DEF(SVE_FS_3A,   IS_NONE, NONE) // SVE_FS_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_clong
IF_DEF(SVE_FT_2A,   IS_NONE, NONE) // SVE_FT_2A  ........xx.xxiii ......nnnnnddddd  -- sve_intx_shift_insert
IF_DEF(SVE_FU_2A,   IS_NONE, NONE) // SVE_FU_2A  ........xx.xxiii ......nnnnnddddd  -- sve_intx_sra
IF_DEF(SVE_FV_2A,   IS_NONE, NONE) // SVE_FV_2A  ........xx...... .....rmmmmmddddd  -- sve_intx_cadd
IF_DEF(SVE_FW_3A,   IS_NONE, NONE) // SVE_FW_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_aba
IF_DEF(SVE_FX_3A,   IS_NONE, NONE) // SVE_FX_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_aba_long
IF_DEF(SVE_FY_3A,   IS_NONE, NONE) // SVE_FY_3A  .........x.mmmmm ......nnnnnddddd  -- sve_intx_adc_long
IF_DEF(SVE_FZ_2A,   IS_NONE, NONE) // SVE_FZ_2A  ................ ......nnnn.ddddd  -- sve_intx_multi_extract_narrow
IF_DEF(SVE_GA_2A,   IS_NONE, NONE) // SVE_GA_2A  ............iiii ......nnnn.ddddd  -- sve_intx_multi_shift_narrow
IF_DEF(SVE_GB_2A,   IS_NONE, NONE) // SVE_GB_2A  .........x.xxiii ......nnnnnddddd  -- sve_intx_shift_narrow
IF_DEF(SVE_GC_3A,   IS_NONE, NONE) // SVE_GC_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_arith_narrow
IF_DEF(SVE_GD_2A,   IS_NONE, NONE) // SVE_GD_2A  .........x.xx... ......nnnnnddddd  -- sve_intx_extract_narrow
IF_DEF(SVE_GE_4A,   IS_NONE, NONE) // SVE_GE_4A  ........xx.mmmmm ...gggnnnnn.DDDD  -- sve_intx_match
IF_DEF(SVE_GF_3A,   IS_NONE, NONE) // SVE_GF_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_intx_histseg
IF_DEF(SVE_GG_3A,   IS_NONE, NONE) // SVE_GG_3A  ........ii.mmmmm ......nnnnnddddd  -- sve_intx_lut2_16
IF_DEF(SVE_GG_3B,   IS_NONE, NONE) // SVE_GG_3B  ........ii.mmmmm ...i..nnnnnddddd  -- sve_intx_lut2_16
IF_DEF(SVE_GH_3A,   IS_NONE, NONE) // SVE_GH_3A  ........i..mmmmm ......nnnnnddddd  -- sve_intx_lut4_16
IF_DEF(SVE_GH_3B,   IS_NONE, NONE) // SVE_GH_3B  ........ii.mmmmm ......nnnnnddddd  -- sve_intx_lut4_16
IF_DEF(SVE_GH_3B_B, IS_NONE, NONE) // SVE_GH_3B_B  ........ii.mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_GI_4A,   IS_NONE, NONE) // SVE_GI_4A  ........xx.mmmmm ...gggnnnnnddddd  -- sve_intx_histcnt
IF_DEF(SVE_GJ_3A,   IS_NONE, NONE) // SVE_GJ_3A  ...........mmmmm ......nnnnnddddd  -- sve_crypto_binary_const
IF_DEF(SVE_GK_2A,   IS_NONE, NONE) // SVE_GK_2A  ................ ......mmmmmddddd  -- sve_crypto_binary_dest
IF_DEF(SVE_GL_1A,   IS_NONE, NONE) // SVE_GL_1A  ................ ...........ddddd  -- sve_crypto_unary
IF_DEF(SVE_GM_3A,   IS_NONE, NONE) // SVE_GM_3A  ...........iimmm ....iinnnnnddddd  -- sve_fp8_fma_long_by_indexed_elem
IF_DEF(SVE_GN_3A,   IS_NONE, NONE) // SVE_GN_3A  ...........mmmmm ......nnnnnddddd  -- sve_fp8_fma_long
IF_DEF(SVE_GO_3A,   IS_NONE, NONE) // SVE_GO_3A  ...........mmmmm ......nnnnnddddd  -- sve_fp8_fma_long_long
IF_DEF(SVE_GP_3A,   IS_NONE, NONE) // SVE_GP_3A  ........xx.....r ...gggmmmmmddddd  -- sve_fp_fcadd
IF_DEF(SVE_GQ_3A,   IS_NONE, NONE) // SVE_GQ_3A  ................ ...gggnnnnnddddd  -- sve_fp_fcvt2
IF_DEF(SVE_GR_3A,   IS_NONE, NONE) // SVE_GR_3A  ........xx...... ...gggmmmmmddddd  -- sve_fp_pairwise
IF_DEF(SVE_GS_3A,   IS_NONE, NONE) // SVE_GS_3A  ........xx...... ...gggnnnnnddddd  -- sve_fp_fast_redq
IF_DEF(SVE_GT_4A,   IS_NONE, NONE) // SVE_GT_4A  ........xx.mmmmm .rrgggnnnnnddddd  -- sve_fp_fcmla
IF_DEF(SVE_GU_3A,   IS_NONE, NONE) // SVE_GU_3A  ...........iimmm ......nnnnnddddd  -- sve_fp_fma_by_indexed_elem
IF_DEF(SVE_GU_3B,   IS_NONE, NONE) // SVE_GU_3B  ...........immmm ......nnnnnddddd  -- sve_fp_fma_by_indexed_elem
IF_DEF(SVE_GU_3C,   IS_NONE, NONE) // SVE_GU_3C  .........i.iimmm ......nnnnnddddd  -- sve_fp_fma_by_indexed_elem
IF_DEF(SVE_GV_3A,   IS_NONE, NONE) // SVE_GV_3A  ...........immmm ....rrnnnnnddddd  -- sve_fp_fcmla_by_indexed_elem
IF_DEF(SVE_GW_3A,   IS_NONE, NONE) // SVE_GW_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_fp_clamp
IF_DEF(SVE_GW_3B,   IS_NONE, NONE) // SVE_GW_3B  ...........mmmmm ......nnnnnddddd  -- sve_fp_clamp
IF_DEF(SVE_GX_3A,   IS_NONE, NONE) // SVE_GX_3A  ...........iimmm ......nnnnnddddd  -- sve_fp_fmul_by_indexed_elem
IF_DEF(SVE_GX_3B,   IS_NONE, NONE) // SVE_GX_3B  ...........immmm ......nnnnnddddd  -- sve_fp_fmul_by_indexed_elem
IF_DEF(SVE_GX_3C,   IS_NONE, NONE) // SVE_GX_3C  .........i.iimmm ......nnnnnddddd  -- sve_fp_fmul_by_indexed_elem
IF_DEF(SVE_GY_3A,   IS_NONE, NONE) // SVE_GY_3A  ...........iimmm ....i.nnnnnddddd  -- sve_fp_fdot_by_indexed_elem
IF_DEF(SVE_GY_3B,   IS_NONE, NONE) // SVE_GY_3B  ...........iimmm ......nnnnnddddd  -- sve_fp_fdot_by_indexed_elem
IF_DEF(SVE_GY_3B_D, IS_NONE, NONE) // SVE_GY_3B_D  ...........iimmm ......nnnnnddddd  -- 
IF_DEF(SVE_GZ_3A,   IS_NONE, NONE) // SVE_GZ_3A  ...........iimmm ....i.nnnnnddddd  -- sve_fp_fma_long_by_indexed_elem
IF_DEF(SVE_HA_3A,   IS_NONE, NONE) // SVE_HA_3A  ...........mmmmm ......nnnnnddddd  -- sve_fp_fdot
IF_DEF(SVE_HA_3A_E, IS_NONE, NONE) // SVE_HA_3A_E  ...........mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_HA_3A_F, IS_NONE, NONE) // SVE_HA_3A_F  ...........mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_HB_3A,   IS_NONE, NONE) // SVE_HB_3A  ...........mmmmm ......nnnnnddddd  -- sve_fp_fma_long
IF_DEF(SVE_HC_3A,   IS_NONE, NONE) // SVE_HC_3A  ...........iimmm ....iinnnnnddddd  -- sve_fp8_fma_long_long_by_indexed_elem
IF_DEF(SVE_HD_3A,   IS_NONE, NONE) // SVE_HD_3A  ...........mmmmm ......nnnnnddddd  -- sve_fp_fmmla
IF_DEF(SVE_HD_3A_A, IS_NONE, NONE) // SVE_HD_3A_A  ...........mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_HE_3A,   IS_NONE, NONE) // SVE_HE_3A  ........xx...... ...gggnnnnnddddd  -- sve_fp_fast_red
IF_DEF(SVE_HF_2A,   IS_NONE, NONE) // SVE_HF_2A  ........xx...... ......nnnnnddddd  -- sve_fp_2op_u_zd
IF_DEF(SVE_HG_2A,   IS_NONE, NONE) // SVE_HG_2A  ................ ......nnnn.ddddd  -- sve_fp8_fcvt_narrow
IF_DEF(SVE_HH_2A,   IS_NONE, NONE) // SVE_HH_2A  ................ ......nnnnnddddd  -- sve_fp8_fcvt_wide
IF_DEF(SVE_HI_3A,   IS_NONE, NONE) // SVE_HI_3A  ........xx...... ...gggnnnnn.DDDD  -- sve_fp_2op_p_pd
IF_DEF(SVE_HJ_3A,   IS_NONE, NONE) // SVE_HJ_3A  ........xx...... ...gggmmmmmddddd  -- sve_fp_2op_p_vd
IF_DEF(SVE_HK_3A,   IS_NONE, NONE) // SVE_HK_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_fp_3op_u_zd
IF_DEF(SVE_HK_3B,   IS_NONE, NONE) // SVE_HK_3B  ...........mmmmm ......nnnnnddddd  -- sve_fp_3op_u_zd
IF_DEF(SVE_HL_3A,   IS_NONE, NONE) // SVE_HL_3A  ........xx...... ...gggmmmmmddddd  -- sve_fp_2op_p_zds
IF_DEF(SVE_HL_3B,   IS_NONE, NONE) // SVE_HL_3B  ................ ...gggmmmmmddddd  -- sve_fp_2op_p_zds
IF_DEF(SVE_HM_2A,   IS_NONE, NONE) // SVE_HM_2A  ........xx...... ...ggg....iddddd  -- sve_fp_2op_i_p_zds
IF_DEF(SVE_HN_2A,   IS_NONE, NONE) // SVE_HN_2A  ........xx...iii ......mmmmmddddd  -- sve_fp_ftmad
IF_DEF(SVE_HO_3A,   IS_NONE, NONE) // SVE_HO_3A  ................ ...gggnnnnnddddd  -- sve_fp_2op_p_zd_b_0
IF_DEF(SVE_HO_3A_B, IS_NONE, NONE) // SVE_HO_3A_B  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HP_3A,   IS_NONE, NONE) // SVE_HP_3A  .............xx. ...gggnnnnnddddd  -- sve_fp_2op_p_zd_d
IF_DEF(SVE_HP_3B,   IS_NONE, NONE) // SVE_HP_3B  ................ ...gggnnnnnddddd  -- sve_fp_2op_p_zd_d
IF_DEF(SVE_HP_3B_H, IS_NONE, NONE) // SVE_HP_3B_H  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HP_3B_I, IS_NONE, NONE) // SVE_HP_3B_I  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HP_3B_J, IS_NONE, NONE) // SVE_HP_3B_J  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HQ_3A,   IS_NONE, NONE) // SVE_HQ_3A  ........xx...... ...gggnnnnnddddd  -- sve_fp_2op_p_zd_a
IF_DEF(SVE_HR_3A,   IS_NONE, NONE) // SVE_HR_3A  ........xx...... ...gggnnnnnddddd  -- sve_fp_2op_p_zd_b_1
IF_DEF(SVE_HS_3A,   IS_NONE, NONE) // SVE_HS_3A  ................ ...gggnnnnnddddd  -- sve_fp_2op_p_zd_c
IF_DEF(SVE_HS_3A_H, IS_NONE, NONE) // SVE_HS_3A_H  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HS_3A_I, IS_NONE, NONE) // SVE_HS_3A_I  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HS_3A_J, IS_NONE, NONE) // SVE_HS_3A_J  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HT_4A,   IS_NONE, NONE) // SVE_HT_4A  ........xx.mmmmm ...gggnnnnn.DDDD  -- sve_fp_3op_p_pd
IF_DEF(SVE_HU_4A,   IS_NONE, NONE) // SVE_HU_4A  ........xx.mmmmm ...gggnnnnnddddd  -- sve_fp_3op_p_zds_a
IF_DEF(SVE_HU_4B,   IS_NONE, NONE) // SVE_HU_4B  ...........mmmmm ...gggnnnnnddddd  -- sve_fp_3op_p_zds_a
IF_DEF(SVE_HV_4A,   IS_NONE, NONE) // SVE_HV_4A  ........xx.aaaaa ...gggmmmmmddddd  -- sve_fp_3op_p_zds_b
IF_DEF(SVE_HW_4A,   IS_NONE, NONE) // SVE_HW_4A  .........h.mmmmm ...gggnnnnnttttt  -- sve_mem_32b_gld_vs
IF_DEF(SVE_HW_4A_A, IS_NONE, NONE) // SVE_HW_4A_A  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HW_4B,   IS_NONE, NONE) // SVE_HW_4B  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_32b_gld_vs
IF_DEF(SVE_HW_4A_B, IS_NONE, NONE) // SVE_HW_4A_B  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HW_4A_C, IS_NONE, NONE) // SVE_HW_4A_C  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HW_4B_D, IS_NONE, NONE) // SVE_HW_4B_D  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HX_3A,   IS_NONE, NONE) // SVE_HX_3A  ...........iiiii ...gggnnnnnttttt  -- sve_mem_32b_gld_vi
IF_DEF(SVE_HX_3A_B, IS_NONE, NONE) // SVE_HX_3A_B  ...........iiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_HX_3A_E, IS_NONE, NONE) // SVE_HX_3A_E  ...........iiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_HY_3A,   IS_NONE, NONE) // SVE_HY_3A  .........h.mmmmm ...gggnnnnn.oooo  -- sve_mem_32b_prfm_sv
IF_DEF(SVE_HY_3A_A, IS_NONE, NONE) // SVE_HY_3A_A  .........h.mmmmm ...gggnnnnn.oooo  -- 
IF_DEF(SVE_HY_3B,   IS_NONE, NONE) // SVE_HY_3B  ...........mmmmm ...gggnnnnn.oooo  -- sve_mem_32b_prfm_sv
IF_DEF(SVE_HZ_2A,   IS_NONE, NONE) // SVE_HZ_2A  ...........iiiii ...gggnnnnn.oooo  -- sve_mem_32b_prfm_vi
IF_DEF(SVE_HZ_2A_B, IS_NONE, NONE) // SVE_HZ_2A_B  ...........iiiii ...gggnnnnn.oooo  -- 
IF_DEF(SVE_IA_2A,   IS_NONE, NONE) // SVE_IA_2A  ..........iiiiii ...gggnnnnn.oooo  -- sve_mem_prfm_si
IF_DEF(SVE_IB_3A,   IS_NONE, NONE) // SVE_IB_3A  ...........mmmmm ...gggnnnnn.oooo  -- sve_mem_prfm_ss
IF_DEF(SVE_IC_3A,   IS_NONE, NONE) // SVE_IC_3A  ..........iiiiii ...gggnnnnnttttt  -- sve_mem_ld_dup
IF_DEF(SVE_IC_3A_A, IS_NONE, NONE) // SVE_IC_3A_A  ..........iiiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IC_3A_B, IS_NONE, NONE) // SVE_IC_3A_B  ..........iiiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IC_3A_C, IS_NONE, NONE) // SVE_IC_3A_C  ..........iiiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_ID_2A,   IS_NONE, NONE) // SVE_ID_2A  ..........iiiiii ...iiinnnnn.TTTT  -- sve_mem_32b_pfill
IF_DEF(SVE_IE_2A,   IS_NONE, NONE) // SVE_IE_2A  ..........iiiiii ...iiinnnnnttttt  -- sve_mem_32b_fill
IF_DEF(SVE_IF_4A,   IS_NONE, NONE) // SVE_IF_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_32b_gldnt_vs
IF_DEF(SVE_IF_4A_A, IS_NONE, NONE) // SVE_IF_4A_A  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A,   IS_NONE, NONE) // SVE_IG_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_cldff_ss
IF_DEF(SVE_IG_4A_C, IS_NONE, NONE) // SVE_IG_4A_C  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_D, IS_NONE, NONE) // SVE_IG_4A_D  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_E, IS_NONE, NONE) // SVE_IG_4A_E  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_F, IS_NONE, NONE) // SVE_IG_4A_F  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_G, IS_NONE, NONE) // SVE_IG_4A_G  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IH_3A,   IS_NONE, NONE) // SVE_IH_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_cld_si_q
IF_DEF(SVE_IH_3A_F, IS_NONE, NONE) // SVE_IH_3A_F  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IH_3A_G, IS_NONE, NONE) // SVE_IH_3A_G  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IH_3A_A, IS_NONE, NONE) // SVE_IH_3A_A  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_II_4A,   IS_NONE, NONE) // SVE_II_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_cld_ss_q
IF_DEF(SVE_II_4A_H, IS_NONE, NONE) // SVE_II_4A_H  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_II_4A_I, IS_NONE, NONE) // SVE_II_4A_I  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_II_4A_B, IS_NONE, NONE) // SVE_II_4A_B  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A,   IS_NONE, NONE) // SVE_IJ_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_cld_si
IF_DEF(SVE_IJ_3A_C, IS_NONE, NONE) // SVE_IJ_3A_C  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_D, IS_NONE, NONE) // SVE_IJ_3A_D  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_E, IS_NONE, NONE) // SVE_IJ_3A_E  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_F, IS_NONE, NONE) // SVE_IJ_3A_F  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_G, IS_NONE, NONE) // SVE_IJ_3A_G  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A,   IS_NONE, NONE) // SVE_IK_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_cld_ss
IF_DEF(SVE_IK_4A_F, IS_NONE, NONE) // SVE_IK_4A_F  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_G, IS_NONE, NONE) // SVE_IK_4A_G  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_H, IS_NONE, NONE) // SVE_IK_4A_H  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_I, IS_NONE, NONE) // SVE_IK_4A_I  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_E, IS_NONE, NONE) // SVE_IK_4A_E  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IL_3A,   IS_NONE, NONE) // SVE_IL_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_cldnf_si
IF_DEF(SVE_IL_3A_A, IS_NONE, NONE) // SVE_IL_3A_A  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IL_3A_B, IS_NONE, NONE) // SVE_IL_3A_B  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IL_3A_C, IS_NONE, NONE) // SVE_IL_3A_C  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IM_3A,   IS_NONE, NONE) // SVE_IM_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_cldnt_si
IF_DEF(SVE_IN_4A,   IS_NONE, NONE) // SVE_IN_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_cldnt_ss
IF_DEF(SVE_IO_3A,   IS_NONE, NONE) // SVE_IO_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_ldqr_si
IF_DEF(SVE_IP_4A,   IS_NONE, NONE) // SVE_IP_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_ldqr_ss
IF_DEF(SVE_IQ_3A,   IS_NONE, NONE) // SVE_IQ_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_eldq_si
IF_DEF(SVE_IR_4A,   IS_NONE, NONE) // SVE_IR_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_eldq_ss
IF_DEF(SVE_IS_3A,   IS_NONE, NONE) // SVE_IS_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_eld_si
IF_DEF(SVE_IT_4A,   IS_NONE, NONE) // SVE_IT_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_eld_ss
IF_DEF(SVE_IU_4A,   IS_NONE, NONE) // SVE_IU_4A  .........h.mmmmm ...gggnnnnnttttt  -- sve_mem_64b_gld_sv
IF_DEF(SVE_IU_4A_A, IS_NONE, NONE) // SVE_IU_4A_A  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IU_4B,   IS_NONE, NONE) // SVE_IU_4B  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_64b_gld_sv
IF_DEF(SVE_IU_4B_B, IS_NONE, NONE) // SVE_IU_4B_B  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IU_4A_C, IS_NONE, NONE) // SVE_IU_4A_C  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IU_4B_D, IS_NONE, NONE) // SVE_IU_4B_D  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IV_3A,   IS_NONE, NONE) // SVE_IV_3A  ...........iiiii ...gggnnnnnttttt  -- sve_mem_64b_gld_vi
IF_DEF(SVE_IW_4A,   IS_NONE, NONE) // SVE_IW_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_64b_gldq_vs
IF_DEF(SVE_IX_4A,   IS_NONE, NONE) // SVE_IX_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_64b_gldnt_vs
IF_DEF(SVE_IY_4A,   IS_NONE, NONE) // SVE_IY_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_sstq_64b_vs
IF_DEF(SVE_IZ_4A,   IS_NONE, NONE) // SVE_IZ_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_sstnt_32b_vs
IF_DEF(SVE_IZ_4A_A, IS_NONE, NONE) // SVE_IZ_4A_A  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JA_4A,   IS_NONE, NONE) // SVE_JA_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_sstnt_64b_vs
IF_DEF(SVE_JB_4A,   IS_NONE, NONE) // SVE_JB_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_cstnt_ss
IF_DEF(SVE_JC_4A,   IS_NONE, NONE) // SVE_JC_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_est_ss
IF_DEF(SVE_JD_4A,   IS_NONE, NONE) // SVE_JD_4A  .........xxmmmmm ...gggnnnnnttttt  -- sve_mem_cst_ss
IF_DEF(SVE_JD_4B,   IS_NONE, NONE) // SVE_JD_4B  ..........xmmmmm ...gggnnnnnttttt  -- sve_mem_cst_ss
IF_DEF(SVE_JD_4C,   IS_NONE, NONE) // SVE_JD_4C  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_cst_ss
IF_DEF(SVE_JD_4C_A, IS_NONE, NONE) // SVE_JD_4C_A  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JE_3A,   IS_NONE, NONE) // SVE_JE_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_estq_si
IF_DEF(SVE_JF_4A,   IS_NONE, NONE) // SVE_JF_4A  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_estq_ss
IF_DEF(SVE_JG_2A,   IS_NONE, NONE) // SVE_JG_2A  ..........iiiiii ...iiinnnnn.TTTT  -- sve_mem_pspill
IF_DEF(SVE_JH_2A,   IS_NONE, NONE) // SVE_JH_2A  ..........iiiiii ...iiinnnnnttttt  -- sve_mem_spill
IF_DEF(SVE_JI_3A,   IS_NONE, NONE) // SVE_JI_3A  ...........iiiii ...gggnnnnnttttt  -- sve_mem_sst_vi_b
IF_DEF(SVE_JI_3A_A, IS_NONE, NONE) // SVE_JI_3A_A  ...........iiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4A,   IS_NONE, NONE) // SVE_JJ_4A  ...........mmmmm .h.gggnnnnnttttt  -- sve_mem_sst_sv2
IF_DEF(SVE_JJ_4A_B, IS_NONE, NONE) // SVE_JJ_4A_B  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4A_C, IS_NONE, NONE) // SVE_JJ_4A_C  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4A_D, IS_NONE, NONE) // SVE_JJ_4A_D  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4B,   IS_NONE, NONE) // SVE_JJ_4B  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_sst_sv2
IF_DEF(SVE_JJ_4B_E, IS_NONE, NONE) // SVE_JJ_4B_E  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4B_C, IS_NONE, NONE) // SVE_JJ_4B_C  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JK_4A,   IS_NONE, NONE) // SVE_JK_4A  ...........mmmmm .h.gggnnnnnttttt  -- sve_mem_sst_vs2
IF_DEF(SVE_JK_4A_B, IS_NONE, NONE) // SVE_JK_4A_B  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JK_4B,   IS_NONE, NONE) // SVE_JK_4B  ...........mmmmm ...gggnnnnnttttt  -- sve_mem_sst_vs2
IF_DEF(SVE_JL_3A,   IS_NONE, NONE) // SVE_JL_3A  ...........iiiii ...gggnnnnnttttt  -- sve_mem_sst_vi_a
IF_DEF(SVE_JM_3A,   IS_NONE, NONE) // SVE_JM_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_cstnt_si
IF_DEF(SVE_JN_3A,   IS_NONE, NONE) // SVE_JN_3A  .........xx.iiii ...gggnnnnnttttt  -- sve_mem_cst_si
IF_DEF(SVE_JN_3B,   IS_NONE, NONE) // SVE_JN_3B  ..........x.iiii ...gggnnnnnttttt  -- sve_mem_cst_si
IF_DEF(SVE_JN_3C,   IS_NONE, NONE) // SVE_JN_3C  ............iiii ...gggnnnnnttttt  -- sve_mem_cst_si
IF_DEF(SVE_JN_3C_D, IS_NONE, NONE) // SVE_JN_3C_D  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_JO_3A,   IS_NONE, NONE) // SVE_JO_3A  ............iiii ...gggnnnnnttttt  -- sve_mem_est_si

//////////////////////////////////////////////////////////////////////////////
#undef IF_DEF
//////////////////////////////////////////////////////////////////////////////
// clang-format on
