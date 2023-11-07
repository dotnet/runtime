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
*
*           ggg, DDD, TTT, MMM, NNN, VVV  --  various predicate register specifications
*           mmmmm   --  Xm, Vm or Zm registers
*           nnnnn   --  Xn, Vn or Zn registers
*           ttttt   --  Xt, Vt or Zt registers
*           ddddd   --  Xd, Xdn, Vd, Vdn or Zd, Zdn, Zda registers
*           i*      --  immediates
*           X       -- vector size
*           x       -- element size
*****************************************************************************/

IF_DEF(SVE_AA_3A,   IS_NONE, NONE) // SVE_AA_3A  ........xx...... ...gggmmmmmddddd  -- SVE bitwise logical operations (predicated)
IF_DEF(SVE_AB_3A,   IS_NONE, NONE) // SVE_AB_3A  ........xx...... ...gggmmmmmddddd  -- SVE integer add/subtract vectors (predicated)
IF_DEF(SVE_AB_3B,   IS_NONE, NONE) // SVE_AB_3B  ................ ...gggmmmmmddddd  -- SVE integer add/subtract vectors (predicated)
IF_DEF(SVE_AC_3A,   IS_NONE, NONE) // SVE_AC_3A  ........xx...... ...gggmmmmmddddd  -- SVE integer divide vectors (predicated)
IF_DEF(SVE_AD_3A,   IS_NONE, NONE) // SVE_AD_3A  ........xx...... ...gggmmmmmddddd  -- SVE integer min/max/difference (predicated)
IF_DEF(SVE_AE_3A,   IS_NONE, NONE) // SVE_AE_3A  ........xx...... ...gggmmmmmddddd  -- SVE integer multiply vectors (predicated)
IF_DEF(SVE_AF_3A,   IS_NONE, NONE) // SVE_AF_3A  ........xx...... ...gggnnnnnddddd  -- SVE bitwise logical reduction (predicated)
IF_DEF(SVE_AG_3A,   IS_NONE, NONE) // SVE_AG_3A  ........xx...... ...gggnnnnnddddd  -- SVE bitwise logical reduction (quadwords)
IF_DEF(SVE_AH_3A,   IS_NONE, NONE) // SVE_AH_3A  ........xx.....M ...gggnnnnnddddd  -- SVE constructive prefix (predicated)
IF_DEF(SVE_AI_3A,   IS_NONE, NONE) // SVE_AI_3A  ........xx...... ...gggnnnnnddddd  -- SVE integer add reduction (predicated)
IF_DEF(SVE_AJ_3A,   IS_NONE, NONE) // SVE_AJ_3A  ........xx...... ...gggnnnnnddddd  -- SVE integer add reduction (quadwords)
IF_DEF(SVE_AK_3A,   IS_NONE, NONE) // SVE_AK_3A  ........xx...... ...gggnnnnnddddd  -- SVE integer min/max reduction (predicated)
IF_DEF(SVE_AL_3A,   IS_NONE, NONE) // SVE_AL_3A  ........xx...... ...gggnnnnnddddd  -- SVE integer min/max reduction (quadwords)
IF_DEF(SVE_AM_2A,   IS_NONE, NONE) // SVE_AM_2A  ........xx...... ...gggxxiiiddddd  -- SVE bitwise shift by immediate (predicated)
IF_DEF(SVE_AN_3A,   IS_NONE, NONE) // SVE_AN_3A  ........xx...... ...gggmmmmmddddd  -- SVE bitwise shift by vector (predicated)
IF_DEF(SVE_AO_3A,   IS_NONE, NONE) // SVE_AO_3A  ........xx...... ...gggmmmmmddddd  -- SVE bitwise shift by wide elements (predicated)
IF_DEF(SVE_AP_3A,   IS_NONE, NONE) // SVE_AP_3A  ........xx...... ...gggnnnnnddddd  -- SVE bitwise unary operations (predicated)
IF_DEF(SVE_AQ_3A,   IS_NONE, NONE) // SVE_AQ_3A  ........xx...... ...gggnnnnnddddd  -- SVE integer unary operations (predicated)
IF_DEF(SVE_AR_4A,   IS_NONE, NONE) // SVE_AR_4A  ........xx.mmmmm ...gggnnnnnddddd  -- SVE integer multiply-accumulate writing addend (predicated)
IF_DEF(SVE_AS_4A,   IS_NONE, NONE) // SVE_AS_4A  ........xx.mmmmm ...gggaaaaaddddd  -- SVE integer multiply-add writing multiplicand (predicated)
IF_DEF(SVE_AT_3A,   IS_NONE, NONE) // SVE_AT_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE integer add/subtract vectors (unpredicated)
IF_DEF(SVE_AT_3B,   IS_NONE, NONE) // SVE_AT_3B  ...........mmmmm ......nnnnnddddd  -- SVE integer add/subtract vectors (unpredicated)
IF_DEF(SVE_AU_3A,   IS_NONE, NONE) // SVE_AU_3A  ...........mmmmm ......nnnnnddddd  -- SVE bitwise logical operations (unpredicated)
IF_DEF(SVE_AV_3A,   IS_NONE, NONE) // SVE_AV_3A  ...........mmmmm ......kkkkkddddd  -- SVE2 bitwise ternary operations
IF_DEF(SVE_AW_2A,   IS_NONE, NONE) // SVE_AW_2A  ........xx.xxiii ......mmmmmddddd  -- sve_int_rotate_imm
IF_DEF(SVE_AX_1A,   IS_NONE, NONE) // SVE_AX_1A  ........xx.iiiii ......iiiiiddddd  -- SVE index generation (immediate start, immediate increment)
IF_DEF(SVE_AY_2A,   IS_NONE, NONE) // SVE_AY_2A  ........xx.mmmmm ......iiiiiddddd  -- SVE index generation (immediate start, register increment)
IF_DEF(SVE_AZ_2A,   IS_NONE, NONE) // SVE_AZ_2A  ........xx.iiiii ......nnnnnddddd  -- SVE index generation (register start, immediate increment)
IF_DEF(SVE_BA_3A,   IS_NONE, NONE) // SVE_BA_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE index generation (register start, register increment)
IF_DEF(SVE_BB_2A,   IS_NONE, NONE) // SVE_BB_2A  ...........nnnnn .....iiiiiiddddd  -- SVE stack frame adjustment
IF_DEF(SVE_BC_1A,   IS_NONE, NONE) // SVE_BC_1A  ................ .....iiiiiiddddd  -- SVE stack frame size
IF_DEF(SVE_BD_3A,   IS_NONE, NONE) // SVE_BD_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer multiply vectors (unpredicated)
IF_DEF(SVE_BD_3B,   IS_NONE, NONE) // SVE_BD_3B  ...........mmmmm ......nnnnnddddd  -- SVE2 integer multiply vectors (unpredicated)
IF_DEF(SVE_BE_3A,   IS_NONE, NONE) // SVE_BE_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 signed saturating doubling multiply high (unpredicated)
IF_DEF(SVE_BF_2A,   IS_NONE, NONE) // SVE_BF_2A  ........xx.xxiii ......nnnnnddddd  -- SVE bitwise shift by immediate (unpredicated)
IF_DEF(SVE_BG_3A,   IS_NONE, NONE) // SVE_BG_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE bitwise shift by wide elements (unpredicated)
IF_DEF(SVE_BH_3A,   IS_NONE, NONE) // SVE_BH_3A  .........x.mmmmm ....hhnnnnnddddd  -- SVE address generation
IF_DEF(SVE_BH_3B,   IS_NONE, NONE) // SVE_BH_3B  ...........mmmmm ....hhnnnnnddddd  -- SVE address generation
IF_DEF(SVE_BH_3B_A, IS_NONE, NONE) // SVE_BH_3B_A  ...........mmmmm ....hhnnnnnddddd  -- 
IF_DEF(SVE_BI_2A,   IS_NONE, NONE) // SVE_BI_2A  ................ ......nnnnnddddd  -- SVE constructive prefix (unpredicated)
IF_DEF(SVE_BJ_2A,   IS_NONE, NONE) // SVE_BJ_2A  ........xx...... ......nnnnnddddd  -- SVE floating-point exponential accelerator
IF_DEF(SVE_BK_3A,   IS_NONE, NONE) // SVE_BK_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE floating-point trig select coefficient
IF_DEF(SVE_BL_1A,   IS_NONE, NONE) // SVE_BL_1A  ............iiii ......pppppddddd  -- SVE element count
IF_DEF(SVE_BM_1A,   IS_NONE, NONE) // SVE_BM_1A  ............iiii ......pppppddddd  -- SVE inc/dec register by element count
IF_DEF(SVE_BN_1A,   IS_NONE, NONE) // SVE_BN_1A  ............iiii ......pppppddddd  -- SVE inc/dec vector by element count
IF_DEF(SVE_BO_1A,   IS_NONE, NONE) // SVE_BO_1A  ...........Xiiii ......pppppddddd  -- SVE saturating inc/dec register by element count
IF_DEF(SVE_BO_1A_A, IS_NONE, NONE) // SVE_BO_1A_A  ...........Xiiii ......pppppddddd  -- 
IF_DEF(SVE_BP_1A,   IS_NONE, NONE) // SVE_BP_1A  ............iiii ......pppppddddd  -- SVE saturating inc/dec vector by element count
IF_DEF(SVE_BQ_2A,   IS_NONE, NONE) // SVE_BQ_2A  ...........iiiii ...iiinnnnnddddd  -- SVE extract vector (immediate offset, destructive)
IF_DEF(SVE_BQ_2B,   IS_NONE, NONE) // SVE_BQ_2B  ...........iiiii ...iiimmmmmddddd  -- SVE extract vector (immediate offset, destructive)
IF_DEF(SVE_BR_3A,   IS_NONE, NONE) // SVE_BR_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE permute vector segments
IF_DEF(SVE_BR_3B,   IS_NONE, NONE) // SVE_BR_3B  ...........mmmmm ......nnnnnddddd  -- SVE permute vector segments
IF_DEF(SVE_BS_1A,   IS_NONE, NONE) // SVE_BS_1A  ..............ii iiiiiiiiiiiddddd  -- SVE bitwise logical with immediate (unpredicated)
IF_DEF(SVE_BT_1A,   IS_NONE, NONE) // SVE_BT_1A  ..............ii iiiiiiiiiiiddddd  -- SVE broadcast bitmask immediate
IF_DEF(SVE_BU_2A,   IS_NONE, NONE) // SVE_BU_2A  ........xx..gggg ...iiiiiiiiddddd  -- SVE copy floating-point immediate (predicated)
IF_DEF(SVE_BV_2A,   IS_NONE, NONE) // SVE_BV_2A  ........xx..gggg ..hiiiiiiiiddddd  -- SVE copy integer immediate (predicated)
IF_DEF(SVE_BV_2A_A, IS_NONE, NONE) // SVE_BV_2A_A  ........xx..gggg ..hiiiiiiiiddddd  -- 
IF_DEF(SVE_BV_2B,   IS_NONE, NONE) // SVE_BV_2B  ........xx..gggg ...........ddddd  -- SVE copy integer immediate (predicated)
IF_DEF(SVE_BV_2A_J, IS_NONE, NONE) // SVE_BV_2A_J  ........xx..gggg ..hiiiiiiiiddddd  -- 
IF_DEF(SVE_BW_2A,   IS_NONE, NONE) // SVE_BW_2A  ........ii.xxxxx ......nnnnnddddd  -- SVE broadcast indexed element
IF_DEF(SVE_BX_2A,   IS_NONE, NONE) // SVE_BX_2A  ...........ixxxx ......nnnnnddddd  -- sve_int_perm_dupq_i
IF_DEF(SVE_BY_2A,   IS_NONE, NONE) // SVE_BY_2A  ............iiii ......mmmmmddddd  -- sve_int_perm_extq
IF_DEF(SVE_BZ_3A,   IS_NONE, NONE) // SVE_BZ_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE table lookup (three sources)
IF_DEF(SVE_BZ_3A_A, IS_NONE, NONE) // SVE_BZ_3A_A  ........xx.mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_CA_3A,   IS_NONE, NONE) // SVE_CA_3A  ........xx.mmmmm ......nnnnnddddd  -- sve_int_perm_tbxquads
IF_DEF(SVE_CB_2A,   IS_NONE, NONE) // SVE_CB_2A  ........xx...... ......nnnnnddddd  -- SVE broadcast general register
IF_DEF(SVE_CC_2A,   IS_NONE, NONE) // SVE_CC_2A  ........xx...... ......mmmmmddddd  -- SVE insert SIMD&FP scalar register
IF_DEF(SVE_CD_2A,   IS_NONE, NONE) // SVE_CD_2A  ........xx...... ......mmmmmddddd  -- SVE insert general register
IF_DEF(SVE_CE_2A,   IS_NONE, NONE) // SVE_CE_2A  ................ ......nnnnn.DDDD  -- SVE move predicate from vector
IF_DEF(SVE_CE_2B,   IS_NONE, NONE) // SVE_CE_2B  .........i...ii. ......nnnnn.DDDD  -- SVE move predicate from vector
IF_DEF(SVE_CE_2C,   IS_NONE, NONE) // SVE_CE_2C  ..............i. ......nnnnn.DDDD  -- SVE move predicate from vector
IF_DEF(SVE_CE_2D,   IS_NONE, NONE) // SVE_CE_2D  .............ii. ......nnnnn.DDDD  -- SVE move predicate from vector
IF_DEF(SVE_CF_2A,   IS_NONE, NONE) // SVE_CF_2A  ................ .......NNNNddddd  -- SVE move predicate into vector
IF_DEF(SVE_CF_2B,   IS_NONE, NONE) // SVE_CF_2B  .........i...ii. .......NNNNddddd  -- SVE move predicate into vector
IF_DEF(SVE_CF_2C,   IS_NONE, NONE) // SVE_CF_2C  ..............i. .......NNNNddddd  -- SVE move predicate into vector
IF_DEF(SVE_CF_2D,   IS_NONE, NONE) // SVE_CF_2D  .............ii. .......NNNNddddd  -- SVE move predicate into vector
IF_DEF(SVE_CG_2A,   IS_NONE, NONE) // SVE_CG_2A  ........xx...... ......nnnnnddddd  -- SVE reverse vector elements
IF_DEF(SVE_CH_2A,   IS_NONE, NONE) // SVE_CH_2A  ........xx...... ......nnnnnddddd  -- SVE unpack vector elements
IF_DEF(SVE_CI_3A,   IS_NONE, NONE) // SVE_CI_3A  ........xx..MMMM .......NNNN.DDDD  -- SVE permute predicate elements
IF_DEF(SVE_CJ_2A,   IS_NONE, NONE) // SVE_CJ_2A  ........xx...... .......NNNN.DDDD  -- SVE reverse predicate elements
IF_DEF(SVE_CK_2A,   IS_NONE, NONE) // SVE_CK_2A  ................ .......NNNN.DDDD  -- SVE unpack predicate elements
IF_DEF(SVE_CL_3A,   IS_NONE, NONE) // SVE_CL_3A  ........xx...... ...gggnnnnnddddd  -- SVE compress active elements
IF_DEF(SVE_CM_3A,   IS_NONE, NONE) // SVE_CM_3A  ........xx...... ...gggmmmmmddddd  -- SVE conditionally broadcast element to vector
IF_DEF(SVE_CN_3A,   IS_NONE, NONE) // SVE_CN_3A  ........xx...... ...gggmmmmmddddd  -- SVE conditionally extract element to SIMD&FP scalar
IF_DEF(SVE_CO_3A,   IS_NONE, NONE) // SVE_CO_3A  ........xx...... ...gggmmmmmddddd  -- SVE conditionally extract element to general register
IF_DEF(SVE_CP_3A,   IS_NONE, NONE) // SVE_CP_3A  ........xx...... ...gggnnnnnddddd  -- SVE copy SIMD&FP scalar register to vector (predicated)
IF_DEF(SVE_CQ_3A,   IS_NONE, NONE) // SVE_CQ_3A  ........xx...... ...gggnnnnnddddd  -- SVE copy general register to vector (predicated)
IF_DEF(SVE_CR_3A,   IS_NONE, NONE) // SVE_CR_3A  ........xx...... ...gggnnnnnddddd  -- SVE extract element to SIMD&FP scalar register
IF_DEF(SVE_CS_3A,   IS_NONE, NONE) // SVE_CS_3A  ........xx...... ...gggnnnnnddddd  -- SVE extract element to general register
IF_DEF(SVE_CT_3A,   IS_NONE, NONE) // SVE_CT_3A  ................ ...gggnnnnnddddd  -- SVE reverse doublewords
IF_DEF(SVE_CU_3A,   IS_NONE, NONE) // SVE_CU_3A  ........xx...... ...gggnnnnnddddd  -- SVE reverse within elements
IF_DEF(SVE_CV_3A,   IS_NONE, NONE) // SVE_CV_3A  ........xx...... ...VVVnnnnnddddd  -- SVE vector splice (destructive)
IF_DEF(SVE_CV_3B,   IS_NONE, NONE) // SVE_CV_3B  ........xx...... ...VVVmmmmmddddd  -- SVE vector splice (destructive)
IF_DEF(SVE_CW_4A,   IS_NONE, NONE) // SVE_CW_4A  ........xx.mmmmm ..VVVVnnnnnddddd  -- SVE select vector elements (predicated)
IF_DEF(SVE_CX_4A,   IS_NONE, NONE) // SVE_CX_4A  ........xx.mmmmm ...gggnnnnn.DDDD  -- SVE integer compare vectors
IF_DEF(SVE_CX_4A_A, IS_NONE, NONE) // SVE_CX_4A_A  ........xx.mmmmm ...gggnnnnn.DDDD  -- 
IF_DEF(SVE_CY_3A,   IS_NONE, NONE) // SVE_CY_3A  ........xx.iiiii ...gggnnnnn.DDDD  -- SVE integer compare with unsigned immediate
IF_DEF(SVE_CY_3B,   IS_NONE, NONE) // SVE_CY_3B  ........xx.iiiii ii.gggnnnnn.DDDD  -- SVE integer compare with unsigned immediate
IF_DEF(SVE_CZ_4A,   IS_NONE, NONE) // SVE_CZ_4A  ............MMMM ..gggg.NNNN.DDDD  -- SVE predicate logical operations
IF_DEF(SVE_CZ_4A_K, IS_NONE, NONE) // SVE_CZ_4A_K  ............MMMM ..gggg.NNNN.DDDD  -- 
IF_DEF(SVE_CZ_4A_L, IS_NONE, NONE) // SVE_CZ_4A_L  ............MMMM ..gggg.NNNN.DDDD  -- 
IF_DEF(SVE_CZ_4A_A, IS_NONE, NONE) // SVE_CZ_4A_A  ............MMMM ..gggg.NNNN.DDDD  -- 
IF_DEF(SVE_DA_4A,   IS_NONE, NONE) // SVE_DA_4A  ............MMMM ..gggg.NNNN.DDDD  -- SVE propagate break from previous partition
IF_DEF(SVE_DB_3A,   IS_NONE, NONE) // SVE_DB_3A  ................ ..gggg.NNNNMDDDD  -- SVE partition break condition
IF_DEF(SVE_DB_3B,   IS_NONE, NONE) // SVE_DB_3B  ................ ..gggg.NNNN.DDDD  -- SVE partition break condition
IF_DEF(SVE_DC_3A,   IS_NONE, NONE) // SVE_DC_3A  ................ ..gggg.NNNN.MMMM  -- SVE propagate break to next partition
IF_DEF(SVE_DD_2A,   IS_NONE, NONE) // SVE_DD_2A  ................ .......gggg.DDDD  -- SVE predicate first active
IF_DEF(SVE_DE_1A,   IS_NONE, NONE) // SVE_DE_1A  ........xx...... ......ppppp.DDDD  -- SVE predicate initialize
IF_DEF(SVE_DF_2A,   IS_NONE, NONE) // SVE_DF_2A  ........xx...... .......VVVV.DDDD  -- SVE predicate next active
IF_DEF(SVE_DG_2A,   IS_NONE, NONE) // SVE_DG_2A  ................ .......gggg.DDDD  -- SVE predicate read from FFR (predicated)
IF_DEF(SVE_DH_1A,   IS_NONE, NONE) // SVE_DH_1A  ................ ............DDDD  -- SVE predicate read from FFR (unpredicated)
IF_DEF(SVE_DI_2A,   IS_NONE, NONE) // SVE_DI_2A  ................ ..gggg.NNNN.....  -- SVE predicate test
IF_DEF(SVE_DJ_1A,   IS_NONE, NONE) // SVE_DJ_1A  ................ ............DDDD  -- SVE predicate zero
IF_DEF(SVE_DK_3A,   IS_NONE, NONE) // SVE_DK_3A  ........xx...... ..gggg.NNNNddddd  -- SVE predicate count
IF_DEF(SVE_DL_2A,   IS_NONE, NONE) // SVE_DL_2A  ........xx...... .....l.NNNNddddd  -- SVE predicate count (predicate-as-counter)
IF_DEF(SVE_DM_2A,   IS_NONE, NONE) // SVE_DM_2A  ........xx...... .......MMMMddddd  -- SVE inc/dec register by predicate count
IF_DEF(SVE_DN_2A,   IS_NONE, NONE) // SVE_DN_2A  ........xx...... .......MMMMddddd  -- SVE inc/dec vector by predicate count
IF_DEF(SVE_DO_2A,   IS_NONE, NONE) // SVE_DO_2A  ........xx...... .....X.MMMMddddd  -- SVE saturating inc/dec register by predicate count
IF_DEF(SVE_DO_2A_A, IS_NONE, NONE) // SVE_DO_2A_A  ........xx...... .....X.MMMMddddd  -- 
IF_DEF(SVE_DP_2A,   IS_NONE, NONE) // SVE_DP_2A  ........xx...... .......MMMMddddd  -- SVE saturating inc/dec vector by predicate count
IF_DEF(SVE_DQ_0A,   IS_NONE, NONE) // SVE_DQ_0A  ................ ................  -- SVE FFR initialise
IF_DEF(SVE_DR_1A,   IS_NONE, NONE) // SVE_DR_1A  ................ .......NNNN.....  -- SVE FFR write from predicate
IF_DEF(SVE_DS_2A,   IS_NONE, NONE) // SVE_DS_2A  .........x.mmmmm ......nnnnn.....  -- SVE conditionally terminate scalars
IF_DEF(SVE_DT_3A,   IS_NONE, NONE) // SVE_DT_3A  ........xx.mmmmm ...X..nnnnn.DDDD  -- SVE integer compare scalar count and limit
IF_DEF(SVE_DU_3A,   IS_NONE, NONE) // SVE_DU_3A  ........xx.mmmmm ......nnnnn.DDDD  -- SVE pointer conflict compare
IF_DEF(SVE_DV_4A,   IS_NONE, NONE) // SVE_DV_4A  ........ix.xxxvv ..NNNN.MMMM.DDDD  -- SVE broadcast predicate element
IF_DEF(SVE_DW_2A,   IS_NONE, NONE) // SVE_DW_2A  ........xx...... ......iiNNN.DDDD  -- SVE extract mask predicate from predicate-as-counter
IF_DEF(SVE_DW_2B,   IS_NONE, NONE) // SVE_DW_2B  ........xx...... .......iNNN.DDDD  -- SVE extract mask predicate from predicate-as-counter
IF_DEF(SVE_DX_3A,   IS_NONE, NONE) // SVE_DX_3A  ........xx.mmmmm ......nnnnn.DDD.  -- SVE integer compare scalar count and limit (predicate pair)
IF_DEF(SVE_DY_3A,   IS_NONE, NONE) // SVE_DY_3A  ........xx.mmmmm ..l...nnnnn..DDD  -- SVE integer compare scalar count and limit (predicate-as-counter)
IF_DEF(SVE_DZ_1A,   IS_NONE, NONE) // SVE_DZ_1A  ........xx...... .............DDD  -- sve_int_pn_ptrue
IF_DEF(SVE_EA_1A,   IS_NONE, NONE) // SVE_EA_1A  ........xx...... ...iiiiiiiiddddd  -- SVE broadcast floating-point immediate (unpredicated)
IF_DEF(SVE_EB_1A,   IS_NONE, NONE) // SVE_EB_1A  ........xx...... ..hiiiiiiiiddddd  -- SVE broadcast integer immediate (unpredicated)
IF_DEF(SVE_EB_1B,   IS_NONE, NONE) // SVE_EB_1B  ........xx...... ...........ddddd  -- SVE broadcast integer immediate (unpredicated)
IF_DEF(SVE_EC_1A,   IS_NONE, NONE) // SVE_EC_1A  ........xx...... ..hiiiiiiiiddddd  -- SVE integer add/subtract immediate (unpredicated)
IF_DEF(SVE_ED_1A,   IS_NONE, NONE) // SVE_ED_1A  ........xx...... ...iiiiiiiiddddd  -- SVE integer min/max immediate (unpredicated)
IF_DEF(SVE_EE_1A,   IS_NONE, NONE) // SVE_EE_1A  ........xx...... ...iiiiiiiiddddd  -- SVE integer multiply immediate (unpredicated)
IF_DEF(SVE_EF_3A,   IS_NONE, NONE) // SVE_EF_3A  ...........mmmmm ......nnnnnddddd  -- SVE two-way dot product
IF_DEF(SVE_EG_3A,   IS_NONE, NONE) // SVE_EG_3A  ...........iimmm ......nnnnnddddd  -- SVE two-way dot product (indexed)
IF_DEF(SVE_EH_3A,   IS_NONE, NONE) // SVE_EH_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE integer dot product (unpredicated)
IF_DEF(SVE_EI_3A,   IS_NONE, NONE) // SVE_EI_3A  ...........mmmmm ......nnnnnddddd  -- SVE mixed sign dot product
IF_DEF(SVE_EJ_3A,   IS_NONE, NONE) // SVE_EJ_3A  ........xx.mmmmm ....rrnnnnnddddd  -- SVE2 complex integer dot product
IF_DEF(SVE_EK_3A,   IS_NONE, NONE) // SVE_EK_3A  ........xx.mmmmm ....rrnnnnnddddd  -- SVE2 complex integer multiply-add
IF_DEF(SVE_EL_3A,   IS_NONE, NONE) // SVE_EL_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer multiply-add long
IF_DEF(SVE_EM_3A,   IS_NONE, NONE) // SVE_EM_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 saturating multiply-add high
IF_DEF(SVE_EN_3A,   IS_NONE, NONE) // SVE_EN_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 saturating multiply-add interleaved long
IF_DEF(SVE_EO_3A,   IS_NONE, NONE) // SVE_EO_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 saturating multiply-add long
IF_DEF(SVE_EP_3A,   IS_NONE, NONE) // SVE_EP_3A  ........xx...... ...gggmmmmmddddd  -- SVE2 integer halving add/subtract (predicated)
IF_DEF(SVE_EQ_3A,   IS_NONE, NONE) // SVE_EQ_3A  ........xx...... ...gggnnnnnddddd  -- SVE2 integer pairwise add and accumulate long
IF_DEF(SVE_ER_3A,   IS_NONE, NONE) // SVE_ER_3A  ........xx...... ...gggmmmmmddddd  -- SVE2 integer pairwise arithmetic
IF_DEF(SVE_ES_3A,   IS_NONE, NONE) // SVE_ES_3A  ........xx...... ...gggnnnnnddddd  -- SVE2 integer unary operations (predicated)
IF_DEF(SVE_ET_3A,   IS_NONE, NONE) // SVE_ET_3A  ........xx...... ...gggmmmmmddddd  -- SVE2 saturating add/subtract
IF_DEF(SVE_EU_3A,   IS_NONE, NONE) // SVE_EU_3A  ........xx...... ...gggmmmmmddddd  -- SVE2 saturating/rounding bitwise shift left (predicated)
IF_DEF(SVE_EV_3A,   IS_NONE, NONE) // SVE_EV_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE integer clamp
IF_DEF(SVE_EW_3A,   IS_NONE, NONE) // SVE_EW_3A  ...........mmmmm ......nnnnnddddd  -- SVE2 multiply-add (checked pointer)
IF_DEF(SVE_EW_3B,   IS_NONE, NONE) // SVE_EW_3B  ...........mmmmm ......aaaaaddddd  -- SVE2 multiply-add (checked pointer)
IF_DEF(SVE_EX_3A,   IS_NONE, NONE) // SVE_EX_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE permute vector elements (quadwords)
IF_DEF(SVE_EY_3A,   IS_NONE, NONE) // SVE_EY_3A  ...........iimmm ......nnnnnddddd  -- SVE integer dot product (indexed)
IF_DEF(SVE_EY_3B,   IS_NONE, NONE) // SVE_EY_3B  ...........immmm ......nnnnnddddd  -- SVE integer dot product (indexed)
IF_DEF(SVE_EZ_3A,   IS_NONE, NONE) // SVE_EZ_3A  ...........iimmm ......nnnnnddddd  -- SVE mixed sign dot product (indexed)
IF_DEF(SVE_FA_3A,   IS_NONE, NONE) // SVE_FA_3A  ...........iimmm ....rrnnnnnddddd  -- SVE2 complex integer dot product (indexed)
IF_DEF(SVE_FA_3B,   IS_NONE, NONE) // SVE_FA_3B  ...........immmm ....rrnnnnnddddd  -- SVE2 complex integer dot product (indexed)
IF_DEF(SVE_FB_3A,   IS_NONE, NONE) // SVE_FB_3A  ...........iimmm ....rrnnnnnddddd  -- SVE2 complex integer multiply-add (indexed)
IF_DEF(SVE_FB_3B,   IS_NONE, NONE) // SVE_FB_3B  ...........immmm ....rrnnnnnddddd  -- SVE2 complex integer multiply-add (indexed)
IF_DEF(SVE_FC_3A,   IS_NONE, NONE) // SVE_FC_3A  ...........iimmm ....rrnnnnnddddd  -- SVE2 complex saturating multiply-add (indexed)
IF_DEF(SVE_FC_3B,   IS_NONE, NONE) // SVE_FC_3B  ...........immmm ....rrnnnnnddddd  -- SVE2 complex saturating multiply-add (indexed)
IF_DEF(SVE_FD_3A,   IS_NONE, NONE) // SVE_FD_3A  .........i.iimmm ......nnnnnddddd  -- SVE2 integer multiply (indexed)
IF_DEF(SVE_FD_3B,   IS_NONE, NONE) // SVE_FD_3B  ...........iimmm ......nnnnnddddd  -- SVE2 integer multiply (indexed)
IF_DEF(SVE_FD_3C,   IS_NONE, NONE) // SVE_FD_3C  ...........immmm ......nnnnnddddd  -- SVE2 integer multiply (indexed)
IF_DEF(SVE_FE_3A,   IS_NONE, NONE) // SVE_FE_3A  ...........iimmm ....i.nnnnnddddd  -- SVE2 integer multiply long (indexed)
IF_DEF(SVE_FE_3B,   IS_NONE, NONE) // SVE_FE_3B  ...........immmm ....i.nnnnnddddd  -- SVE2 integer multiply long (indexed)
IF_DEF(SVE_FF_3A,   IS_NONE, NONE) // SVE_FF_3A  .........i.iimmm ......nnnnnddddd  -- SVE2 integer multiply-add (indexed)
IF_DEF(SVE_FF_3B,   IS_NONE, NONE) // SVE_FF_3B  ...........iimmm ......nnnnnddddd  -- SVE2 integer multiply-add (indexed)
IF_DEF(SVE_FF_3C,   IS_NONE, NONE) // SVE_FF_3C  ...........immmm ......nnnnnddddd  -- SVE2 integer multiply-add (indexed)
IF_DEF(SVE_FG_3A,   IS_NONE, NONE) // SVE_FG_3A  ...........iimmm ....i.nnnnnddddd  -- SVE2 integer multiply-add long (indexed)
IF_DEF(SVE_FG_3B,   IS_NONE, NONE) // SVE_FG_3B  ...........immmm ....i.nnnnnddddd  -- SVE2 integer multiply-add long (indexed)
IF_DEF(SVE_FH_3A,   IS_NONE, NONE) // SVE_FH_3A  ...........iimmm ....i.nnnnnddddd  -- SVE2 saturating multiply (indexed)
IF_DEF(SVE_FH_3B,   IS_NONE, NONE) // SVE_FH_3B  ...........immmm ....i.nnnnnddddd  -- SVE2 saturating multiply (indexed)
IF_DEF(SVE_FI_3A,   IS_NONE, NONE) // SVE_FI_3A  .........i.iimmm ......nnnnnddddd  -- SVE2 saturating multiply high (indexed)
IF_DEF(SVE_FI_3B,   IS_NONE, NONE) // SVE_FI_3B  ...........iimmm ......nnnnnddddd  -- SVE2 saturating multiply high (indexed)
IF_DEF(SVE_FI_3C,   IS_NONE, NONE) // SVE_FI_3C  ...........immmm ......nnnnnddddd  -- SVE2 saturating multiply high (indexed)
IF_DEF(SVE_FJ_3A,   IS_NONE, NONE) // SVE_FJ_3A  ...........iimmm ....i.nnnnnddddd  -- SVE2 saturating multiply-add (indexed)
IF_DEF(SVE_FJ_3B,   IS_NONE, NONE) // SVE_FJ_3B  ...........immmm ....i.nnnnnddddd  -- SVE2 saturating multiply-add (indexed)
IF_DEF(SVE_FK_3A,   IS_NONE, NONE) // SVE_FK_3A  .........i.iimmm ......nnnnnddddd  -- SVE2 saturating multiply-add high (indexed)
IF_DEF(SVE_FK_3B,   IS_NONE, NONE) // SVE_FK_3B  ...........iimmm ......nnnnnddddd  -- SVE2 saturating multiply-add high (indexed)
IF_DEF(SVE_FK_3C,   IS_NONE, NONE) // SVE_FK_3C  ...........immmm ......nnnnnddddd  -- SVE2 saturating multiply-add high (indexed)
IF_DEF(SVE_FL_3A,   IS_NONE, NONE) // SVE_FL_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer add/subtract long
IF_DEF(SVE_FM_3A,   IS_NONE, NONE) // SVE_FM_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer add/subtract wide
IF_DEF(SVE_FN_3A,   IS_NONE, NONE) // SVE_FN_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer multiply long
IF_DEF(SVE_FN_3B,   IS_NONE, NONE) // SVE_FN_3B  ...........mmmmm ......nnnnnddddd  -- SVE2 integer multiply long
IF_DEF(SVE_FO_3A,   IS_NONE, NONE) // SVE_FO_3A  ...........mmmmm ......nnnnnddddd  -- SVE integer matrix multiply accumulate
IF_DEF(SVE_FP_3A,   IS_NONE, NONE) // SVE_FP_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 bitwise exclusive-or interleaved
IF_DEF(SVE_FQ_3A,   IS_NONE, NONE) // SVE_FQ_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 bitwise permute
IF_DEF(SVE_FR_2A,   IS_NONE, NONE) // SVE_FR_2A  .........x.xxiii ......nnnnnddddd  -- SVE2 bitwise shift left long
IF_DEF(SVE_FS_3A,   IS_NONE, NONE) // SVE_FS_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer add/subtract interleaved long
IF_DEF(SVE_FT_2A,   IS_NONE, NONE) // SVE_FT_2A  ........xx.xxiii ......nnnnnddddd  -- SVE2 bitwise shift and insert
IF_DEF(SVE_FU_2A,   IS_NONE, NONE) // SVE_FU_2A  ........xx.xxiii ......nnnnnddddd  -- SVE2 bitwise shift right and accumulate
IF_DEF(SVE_FV_2A,   IS_NONE, NONE) // SVE_FV_2A  ........xx...... .....rmmmmmddddd  -- SVE2 complex integer add
IF_DEF(SVE_FW_3A,   IS_NONE, NONE) // SVE_FW_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer absolute difference and accumulate
IF_DEF(SVE_FX_3A,   IS_NONE, NONE) // SVE_FX_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer absolute difference and accumulate long
IF_DEF(SVE_FY_3A,   IS_NONE, NONE) // SVE_FY_3A  .........x.mmmmm ......nnnnnddddd  -- SVE2 integer add/subtract long with carry
IF_DEF(SVE_FZ_2A,   IS_NONE, NONE) // SVE_FZ_2A  ................ ......nnnn.ddddd  -- SME2 multi-vec extract narrow
IF_DEF(SVE_GA_2A,   IS_NONE, NONE) // SVE_GA_2A  ............iiii ......nnnn.ddddd  -- SME2 multi-vec shift narrow
IF_DEF(SVE_GB_2A,   IS_NONE, NONE) // SVE_GB_2A  .........x.xxiii ......nnnnnddddd  -- SVE2 bitwise shift right narrow
IF_DEF(SVE_GC_3A,   IS_NONE, NONE) // SVE_GC_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 integer add/subtract narrow high part
IF_DEF(SVE_GD_2A,   IS_NONE, NONE) // SVE_GD_2A  .........x.xx... ......nnnnnddddd  -- SVE2 saturating extract narrow
IF_DEF(SVE_GE_4A,   IS_NONE, NONE) // SVE_GE_4A  ........xx.mmmmm ...gggnnnnn.DDDD  -- SVE2 character match
IF_DEF(SVE_GF_3A,   IS_NONE, NONE) // SVE_GF_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE2 histogram generation (segment)
IF_DEF(SVE_GG_3A,   IS_NONE, NONE) // SVE_GG_3A  ........ii.mmmmm ......nnnnnddddd  -- SVE2 lookup table with 2-bit indices and 16-bit element size
IF_DEF(SVE_GG_3B,   IS_NONE, NONE) // SVE_GG_3B  ........ii.mmmmm ...i..nnnnnddddd  -- SVE2 lookup table with 2-bit indices and 16-bit element size
IF_DEF(SVE_GH_3A,   IS_NONE, NONE) // SVE_GH_3A  ........i..mmmmm ......nnnnnddddd  -- SVE2 lookup table with 4-bit indices and 16-bit element size
IF_DEF(SVE_GH_3B,   IS_NONE, NONE) // SVE_GH_3B  ........ii.mmmmm ......nnnnnddddd  -- SVE2 lookup table with 4-bit indices and 16-bit element size
IF_DEF(SVE_GH_3B_B, IS_NONE, NONE) // SVE_GH_3B_B  ........ii.mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_GI_4A,   IS_NONE, NONE) // SVE_GI_4A  ........xx.mmmmm ...gggnnnnnddddd  -- SVE2 histogram generation (vector)
IF_DEF(SVE_GJ_3A,   IS_NONE, NONE) // SVE_GJ_3A  ...........mmmmm ......nnnnnddddd  -- SVE2 crypto constructive binary operations
IF_DEF(SVE_GK_2A,   IS_NONE, NONE) // SVE_GK_2A  ................ ......mmmmmddddd  -- SVE2 crypto destructive binary operations
IF_DEF(SVE_GL_1A,   IS_NONE, NONE) // SVE_GL_1A  ................ ...........ddddd  -- SVE2 crypto unary operations
IF_DEF(SVE_GM_3A,   IS_NONE, NONE) // SVE_GM_3A  ...........iimmm ....iinnnnnddddd  -- SVE2 FP8 multiply-add long (indexed)
IF_DEF(SVE_GN_3A,   IS_NONE, NONE) // SVE_GN_3A  ...........mmmmm ......nnnnnddddd  -- SVE2 FP8 multiply-add long
IF_DEF(SVE_GO_3A,   IS_NONE, NONE) // SVE_GO_3A  ...........mmmmm ......nnnnnddddd  -- SVE2 FP8 multiply-add long long
IF_DEF(SVE_GP_3A,   IS_NONE, NONE) // SVE_GP_3A  ........xx.....r ...gggmmmmmddddd  -- SVE floating-point complex add (predicated)
IF_DEF(SVE_GQ_3A,   IS_NONE, NONE) // SVE_GQ_3A  ................ ...gggnnnnnddddd  -- SVE floating-point convert precision odd elements
IF_DEF(SVE_GR_3A,   IS_NONE, NONE) // SVE_GR_3A  ........xx...... ...gggmmmmmddddd  -- SVE2 floating-point pairwise operations
IF_DEF(SVE_GS_3A,   IS_NONE, NONE) // SVE_GS_3A  ........xx...... ...gggnnnnnddddd  -- SVE floating-point recursive reduction (quadwords)
IF_DEF(SVE_GT_4A,   IS_NONE, NONE) // SVE_GT_4A  ........xx.mmmmm .rrgggnnnnnddddd  -- SVE floating-point complex multiply-add (predicated)
IF_DEF(SVE_GU_3A,   IS_NONE, NONE) // SVE_GU_3A  ...........iimmm ......nnnnnddddd  -- SVE floating-point multiply-add (indexed)
IF_DEF(SVE_GU_3B,   IS_NONE, NONE) // SVE_GU_3B  ...........immmm ......nnnnnddddd  -- SVE floating-point multiply-add (indexed)
IF_DEF(SVE_GU_3C,   IS_NONE, NONE) // SVE_GU_3C  .........i.iimmm ......nnnnnddddd  -- SVE floating-point multiply-add (indexed)
IF_DEF(SVE_GV_3A,   IS_NONE, NONE) // SVE_GV_3A  ...........immmm ....rrnnnnnddddd  -- SVE floating-point complex multiply-add (indexed)
IF_DEF(SVE_GW_3A,   IS_NONE, NONE) // SVE_GW_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE FP clamp
IF_DEF(SVE_GW_3B,   IS_NONE, NONE) // SVE_GW_3B  ...........mmmmm ......nnnnnddddd  -- SVE FP clamp
IF_DEF(SVE_GX_3A,   IS_NONE, NONE) // SVE_GX_3A  ...........iimmm ......nnnnnddddd  -- SVE floating-point multiply (indexed)
IF_DEF(SVE_GX_3B,   IS_NONE, NONE) // SVE_GX_3B  ...........immmm ......nnnnnddddd  -- SVE floating-point multiply (indexed)
IF_DEF(SVE_GX_3C,   IS_NONE, NONE) // SVE_GX_3C  .........i.iimmm ......nnnnnddddd  -- SVE floating-point multiply (indexed)
IF_DEF(SVE_GY_3A,   IS_NONE, NONE) // SVE_GY_3A  ...........iimmm ....i.nnnnnddddd  -- SVE BFloat16 floating-point dot product (indexed)
IF_DEF(SVE_GY_3B,   IS_NONE, NONE) // SVE_GY_3B  ...........iimmm ......nnnnnddddd  -- SVE BFloat16 floating-point dot product (indexed)
IF_DEF(SVE_GY_3B_D, IS_NONE, NONE) // SVE_GY_3B_D  ...........iimmm ......nnnnnddddd  -- 
IF_DEF(SVE_GZ_3A,   IS_NONE, NONE) // SVE_GZ_3A  ...........iimmm ....i.nnnnnddddd  -- SVE floating-point multiply-add long (indexed)
IF_DEF(SVE_HA_3A,   IS_NONE, NONE) // SVE_HA_3A  ...........mmmmm ......nnnnnddddd  -- SVE BFloat16 floating-point dot product
IF_DEF(SVE_HA_3A_E, IS_NONE, NONE) // SVE_HA_3A_E  ...........mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_HA_3A_F, IS_NONE, NONE) // SVE_HA_3A_F  ...........mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_HB_3A,   IS_NONE, NONE) // SVE_HB_3A  ...........mmmmm ......nnnnnddddd  -- SVE floating-point multiply-add long
IF_DEF(SVE_HC_3A,   IS_NONE, NONE) // SVE_HC_3A  ...........iimmm ....iinnnnnddddd  -- SVE2 FP8 multiply-add long long (indexed)
IF_DEF(SVE_HD_3A,   IS_NONE, NONE) // SVE_HD_3A  ...........mmmmm ......nnnnnddddd  -- SVE floating point matrix multiply accumulate
IF_DEF(SVE_HD_3A_A, IS_NONE, NONE) // SVE_HD_3A_A  ...........mmmmm ......nnnnnddddd  -- 
IF_DEF(SVE_HE_3A,   IS_NONE, NONE) // SVE_HE_3A  ........xx...... ...gggnnnnnddddd  -- SVE floating-point recursive reduction
IF_DEF(SVE_HF_2A,   IS_NONE, NONE) // SVE_HF_2A  ........xx...... ......nnnnnddddd  -- SVE floating-point reciprocal estimate (unpredicated)
IF_DEF(SVE_HG_2A,   IS_NONE, NONE) // SVE_HG_2A  ................ ......nnnn.ddddd  -- SVE2 FP8 downconverts
IF_DEF(SVE_HH_2A,   IS_NONE, NONE) // SVE_HH_2A  ................ ......nnnnnddddd  -- SVE2 FP8 upconverts
IF_DEF(SVE_HI_3A,   IS_NONE, NONE) // SVE_HI_3A  ........xx...... ...gggnnnnn.DDDD  -- SVE floating-point compare with zero
IF_DEF(SVE_HJ_3A,   IS_NONE, NONE) // SVE_HJ_3A  ........xx...... ...gggmmmmmddddd  -- SVE floating-point serial reduction (predicated)
IF_DEF(SVE_HK_3A,   IS_NONE, NONE) // SVE_HK_3A  ........xx.mmmmm ......nnnnnddddd  -- SVE floating-point arithmetic (unpredicated)
IF_DEF(SVE_HK_3B,   IS_NONE, NONE) // SVE_HK_3B  ...........mmmmm ......nnnnnddddd  -- SVE floating-point arithmetic (unpredicated)
IF_DEF(SVE_HL_3A,   IS_NONE, NONE) // SVE_HL_3A  ........xx...... ...gggmmmmmddddd  -- SVE floating-point arithmetic (predicated)
IF_DEF(SVE_HL_3B,   IS_NONE, NONE) // SVE_HL_3B  ................ ...gggmmmmmddddd  -- SVE floating-point arithmetic (predicated)
IF_DEF(SVE_HM_2A,   IS_NONE, NONE) // SVE_HM_2A  ........xx...... ...ggg....iddddd  -- SVE floating-point arithmetic with immediate (predicated)
IF_DEF(SVE_HN_2A,   IS_NONE, NONE) // SVE_HN_2A  ........xx...iii ......mmmmmddddd  -- SVE floating-point trig multiply-add coefficient
IF_DEF(SVE_HO_3A,   IS_NONE, NONE) // SVE_HO_3A  ................ ...gggnnnnnddddd  -- SVE floating-point convert precision
IF_DEF(SVE_HO_3A_B, IS_NONE, NONE) // SVE_HO_3A_B  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HP_3A,   IS_NONE, NONE) // SVE_HP_3A  .............xx. ...gggnnnnnddddd  -- SVE floating-point convert to integer
IF_DEF(SVE_HP_3B,   IS_NONE, NONE) // SVE_HP_3B  ................ ...gggnnnnnddddd  -- SVE floating-point convert to integer
IF_DEF(SVE_HP_3B_H, IS_NONE, NONE) // SVE_HP_3B_H  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HP_3B_I, IS_NONE, NONE) // SVE_HP_3B_I  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HP_3B_J, IS_NONE, NONE) // SVE_HP_3B_J  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HQ_3A,   IS_NONE, NONE) // SVE_HQ_3A  ........xx...... ...gggnnnnnddddd  -- SVE floating-point round to integral value
IF_DEF(SVE_HR_3A,   IS_NONE, NONE) // SVE_HR_3A  ........xx...... ...gggnnnnnddddd  -- SVE floating-point unary operations
IF_DEF(SVE_HS_3A,   IS_NONE, NONE) // SVE_HS_3A  ................ ...gggnnnnnddddd  -- SVE integer convert to floating-point
IF_DEF(SVE_HS_3A_H, IS_NONE, NONE) // SVE_HS_3A_H  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HS_3A_I, IS_NONE, NONE) // SVE_HS_3A_I  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HS_3A_J, IS_NONE, NONE) // SVE_HS_3A_J  ................ ...gggnnnnnddddd  -- 
IF_DEF(SVE_HT_4A,   IS_NONE, NONE) // SVE_HT_4A  ........xx.mmmmm ...gggnnnnn.DDDD  -- SVE floating-point compare vectors
IF_DEF(SVE_HU_4A,   IS_NONE, NONE) // SVE_HU_4A  ........xx.mmmmm ...gggnnnnnddddd  -- SVE floating-point multiply-accumulate writing addend
IF_DEF(SVE_HU_4B,   IS_NONE, NONE) // SVE_HU_4B  ...........mmmmm ...gggnnnnnddddd  -- SVE floating-point multiply-accumulate writing addend
IF_DEF(SVE_HV_4A,   IS_NONE, NONE) // SVE_HV_4A  ........xx.aaaaa ...gggmmmmmddddd  -- SVE floating-point multiply-accumulate writing multiplicand
IF_DEF(SVE_HW_4A,   IS_NONE, NONE) // SVE_HW_4A  .........h.mmmmm ...gggnnnnnttttt  -- SVE 32-bit gather load (scalar plus 32-bit unscaled offsets)
IF_DEF(SVE_HW_4A_A, IS_NONE, NONE) // SVE_HW_4A_A  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HW_4B,   IS_NONE, NONE) // SVE_HW_4B  ...........mmmmm ...gggnnnnnttttt  -- SVE 32-bit gather load (scalar plus 32-bit unscaled offsets)
IF_DEF(SVE_HW_4A_B, IS_NONE, NONE) // SVE_HW_4A_B  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HW_4A_C, IS_NONE, NONE) // SVE_HW_4A_C  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HW_4B_D, IS_NONE, NONE) // SVE_HW_4B_D  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_HX_3A,   IS_NONE, NONE) // SVE_HX_3A  ...........iiiii ...gggnnnnnttttt  -- SVE 32-bit gather load (vector plus immediate)
IF_DEF(SVE_HX_3A_B, IS_NONE, NONE) // SVE_HX_3A_B  ...........iiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_HX_3A_E, IS_NONE, NONE) // SVE_HX_3A_E  ...........iiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_HY_3A,   IS_NONE, NONE) // SVE_HY_3A  .........h.mmmmm ...gggnnnnn.oooo  -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled offsets)
IF_DEF(SVE_HY_3A_A, IS_NONE, NONE) // SVE_HY_3A_A  .........h.mmmmm ...gggnnnnn.oooo  -- 
IF_DEF(SVE_HY_3B,   IS_NONE, NONE) // SVE_HY_3B  ...........mmmmm ...gggnnnnn.oooo  -- SVE 32-bit gather prefetch (scalar plus 32-bit scaled offsets)
IF_DEF(SVE_HZ_2A,   IS_NONE, NONE) // SVE_HZ_2A  ...........iiiii ...gggnnnnn.oooo  -- SVE 32-bit gather prefetch (vector plus immediate)
IF_DEF(SVE_HZ_2A_B, IS_NONE, NONE) // SVE_HZ_2A_B  ...........iiiii ...gggnnnnn.oooo  -- 
IF_DEF(SVE_IA_2A,   IS_NONE, NONE) // SVE_IA_2A  ..........iiiiii ...gggnnnnn.oooo  -- SVE contiguous prefetch (scalar plus immediate)
IF_DEF(SVE_IB_3A,   IS_NONE, NONE) // SVE_IB_3A  ...........mmmmm ...gggnnnnn.oooo  -- SVE contiguous prefetch (scalar plus scalar)
IF_DEF(SVE_IC_3A,   IS_NONE, NONE) // SVE_IC_3A  ..........iiiiii ...gggnnnnnttttt  -- SVE load and broadcast element
IF_DEF(SVE_IC_3A_A, IS_NONE, NONE) // SVE_IC_3A_A  ..........iiiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IC_3A_B, IS_NONE, NONE) // SVE_IC_3A_B  ..........iiiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IC_3A_C, IS_NONE, NONE) // SVE_IC_3A_C  ..........iiiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_ID_2A,   IS_NONE, NONE) // SVE_ID_2A  ..........iiiiii ...iiinnnnn.TTTT  -- SVE load predicate register
IF_DEF(SVE_IE_2A,   IS_NONE, NONE) // SVE_IE_2A  ..........iiiiii ...iiinnnnnttttt  -- SVE load vector register
IF_DEF(SVE_IF_4A,   IS_NONE, NONE) // SVE_IF_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE2 32-bit gather non-temporal load (vector plus scalar)
IF_DEF(SVE_IF_4A_A, IS_NONE, NONE) // SVE_IF_4A_A  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A,   IS_NONE, NONE) // SVE_IG_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE contiguous first-fault load (scalar plus scalar)
IF_DEF(SVE_IG_4A_C, IS_NONE, NONE) // SVE_IG_4A_C  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_D, IS_NONE, NONE) // SVE_IG_4A_D  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_E, IS_NONE, NONE) // SVE_IG_4A_E  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_F, IS_NONE, NONE) // SVE_IG_4A_F  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IG_4A_G, IS_NONE, NONE) // SVE_IG_4A_G  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IH_3A,   IS_NONE, NONE) // SVE_IH_3A  ............iiii ...gggnnnnnttttt  -- SVE contiguous load (quadwords, scalar plus immediate)
IF_DEF(SVE_IH_3A_F, IS_NONE, NONE) // SVE_IH_3A_F  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IH_3A_G, IS_NONE, NONE) // SVE_IH_3A_G  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IH_3A_A, IS_NONE, NONE) // SVE_IH_3A_A  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_II_4A,   IS_NONE, NONE) // SVE_II_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE contiguous load (quadwords, scalar plus scalar)
IF_DEF(SVE_II_4A_H, IS_NONE, NONE) // SVE_II_4A_H  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_II_4A_I, IS_NONE, NONE) // SVE_II_4A_I  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_II_4A_B, IS_NONE, NONE) // SVE_II_4A_B  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A,   IS_NONE, NONE) // SVE_IJ_3A  ............iiii ...gggnnnnnttttt  -- SVE contiguous load (scalar plus immediate)
IF_DEF(SVE_IJ_3A_C, IS_NONE, NONE) // SVE_IJ_3A_C  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_D, IS_NONE, NONE) // SVE_IJ_3A_D  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_E, IS_NONE, NONE) // SVE_IJ_3A_E  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_F, IS_NONE, NONE) // SVE_IJ_3A_F  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IJ_3A_G, IS_NONE, NONE) // SVE_IJ_3A_G  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A,   IS_NONE, NONE) // SVE_IK_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE contiguous load (scalar plus scalar)
IF_DEF(SVE_IK_4A_F, IS_NONE, NONE) // SVE_IK_4A_F  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_G, IS_NONE, NONE) // SVE_IK_4A_G  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_H, IS_NONE, NONE) // SVE_IK_4A_H  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_I, IS_NONE, NONE) // SVE_IK_4A_I  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IK_4A_E, IS_NONE, NONE) // SVE_IK_4A_E  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IL_3A,   IS_NONE, NONE) // SVE_IL_3A  ............iiii ...gggnnnnnttttt  -- SVE contiguous non-fault load (scalar plus immediate)
IF_DEF(SVE_IL_3A_A, IS_NONE, NONE) // SVE_IL_3A_A  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IL_3A_B, IS_NONE, NONE) // SVE_IL_3A_B  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IL_3A_C, IS_NONE, NONE) // SVE_IL_3A_C  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_IM_3A,   IS_NONE, NONE) // SVE_IM_3A  ............iiii ...gggnnnnnttttt  -- SVE contiguous non-temporal load (scalar plus immediate)
IF_DEF(SVE_IN_4A,   IS_NONE, NONE) // SVE_IN_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE contiguous non-temporal load (scalar plus scalar)
IF_DEF(SVE_IO_3A,   IS_NONE, NONE) // SVE_IO_3A  ............iiii ...gggnnnnnttttt  -- SVE load and broadcast quadword (scalar plus immediate)
IF_DEF(SVE_IP_4A,   IS_NONE, NONE) // SVE_IP_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE load and broadcast quadword (scalar plus scalar)
IF_DEF(SVE_IQ_3A,   IS_NONE, NONE) // SVE_IQ_3A  ............iiii ...gggnnnnnttttt  -- SVE load multiple structures (quadwords, scalar plus immediate)
IF_DEF(SVE_IR_4A,   IS_NONE, NONE) // SVE_IR_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE load multiple structures (quadwords, scalar plus scalar)
IF_DEF(SVE_IS_3A,   IS_NONE, NONE) // SVE_IS_3A  ............iiii ...gggnnnnnttttt  -- SVE load multiple structures (scalar plus immediate)
IF_DEF(SVE_IT_4A,   IS_NONE, NONE) // SVE_IT_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE load multiple structures (scalar plus scalar)
IF_DEF(SVE_IU_4A,   IS_NONE, NONE) // SVE_IU_4A  .........h.mmmmm ...gggnnnnnttttt  -- SVE 64-bit gather load (scalar plus 32-bit unpacked scaled offsets)
IF_DEF(SVE_IU_4A_A, IS_NONE, NONE) // SVE_IU_4A_A  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IU_4B,   IS_NONE, NONE) // SVE_IU_4B  ...........mmmmm ...gggnnnnnttttt  -- SVE 64-bit gather load (scalar plus 32-bit unpacked scaled offsets)
IF_DEF(SVE_IU_4B_B, IS_NONE, NONE) // SVE_IU_4B_B  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IU_4A_C, IS_NONE, NONE) // SVE_IU_4A_C  .........h.mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IU_4B_D, IS_NONE, NONE) // SVE_IU_4B_D  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_IV_3A,   IS_NONE, NONE) // SVE_IV_3A  ...........iiiii ...gggnnnnnttttt  -- SVE 64-bit gather load (vector plus immediate)
IF_DEF(SVE_IW_4A,   IS_NONE, NONE) // SVE_IW_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE2 128-bit gather load (vector plus scalar)
IF_DEF(SVE_IX_4A,   IS_NONE, NONE) // SVE_IX_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE2 64-bit gather non-temporal load (vector plus scalar)
IF_DEF(SVE_IY_4A,   IS_NONE, NONE) // SVE_IY_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE2 128-bit scatter store (vector plus scalar)
IF_DEF(SVE_IZ_4A,   IS_NONE, NONE) // SVE_IZ_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE2 32-bit scatter non-temporal store (vector plus scalar)
IF_DEF(SVE_IZ_4A_A, IS_NONE, NONE) // SVE_IZ_4A_A  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JA_4A,   IS_NONE, NONE) // SVE_JA_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE2 64-bit scatter non-temporal store (vector plus scalar)
IF_DEF(SVE_JB_4A,   IS_NONE, NONE) // SVE_JB_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE contiguous non-temporal store (scalar plus scalar)
IF_DEF(SVE_JC_4A,   IS_NONE, NONE) // SVE_JC_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE store multiple structures (scalar plus scalar)
IF_DEF(SVE_JD_4A,   IS_NONE, NONE) // SVE_JD_4A  .........xxmmmmm ...gggnnnnnttttt  -- SVE contiguous store (scalar plus scalar)
IF_DEF(SVE_JD_4B,   IS_NONE, NONE) // SVE_JD_4B  ..........xmmmmm ...gggnnnnnttttt  -- SVE contiguous store (scalar plus scalar)
IF_DEF(SVE_JD_4C,   IS_NONE, NONE) // SVE_JD_4C  ...........mmmmm ...gggnnnnnttttt  -- SVE contiguous store (scalar plus scalar)
IF_DEF(SVE_JD_4C_A, IS_NONE, NONE) // SVE_JD_4C_A  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JE_3A,   IS_NONE, NONE) // SVE_JE_3A  ............iiii ...gggnnnnnttttt  -- SVE store multiple structures (quadwords, scalar plus immediate)
IF_DEF(SVE_JF_4A,   IS_NONE, NONE) // SVE_JF_4A  ...........mmmmm ...gggnnnnnttttt  -- SVE store multiple structures (quadwords, scalar plus scalar)
IF_DEF(SVE_JG_2A,   IS_NONE, NONE) // SVE_JG_2A  ..........iiiiii ...iiinnnnn.TTTT  -- SVE store predicate register
IF_DEF(SVE_JH_2A,   IS_NONE, NONE) // SVE_JH_2A  ..........iiiiii ...iiinnnnnttttt  -- SVE store vector register
IF_DEF(SVE_JI_3A,   IS_NONE, NONE) // SVE_JI_3A  ...........iiiii ...gggnnnnnttttt  -- SVE 32-bit scatter store (vector plus immediate)
IF_DEF(SVE_JI_3A_A, IS_NONE, NONE) // SVE_JI_3A_A  ...........iiiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4A,   IS_NONE, NONE) // SVE_JJ_4A  ...........mmmmm .h.gggnnnnnttttt  -- SVE 64-bit scatter store (scalar plus 64-bit scaled offsets)
IF_DEF(SVE_JJ_4A_B, IS_NONE, NONE) // SVE_JJ_4A_B  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4A_C, IS_NONE, NONE) // SVE_JJ_4A_C  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4A_D, IS_NONE, NONE) // SVE_JJ_4A_D  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4B,   IS_NONE, NONE) // SVE_JJ_4B  ...........mmmmm ...gggnnnnnttttt  -- SVE 64-bit scatter store (scalar plus 64-bit scaled offsets)
IF_DEF(SVE_JJ_4B_E, IS_NONE, NONE) // SVE_JJ_4B_E  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JJ_4B_C, IS_NONE, NONE) // SVE_JJ_4B_C  ...........mmmmm ...gggnnnnnttttt  -- 
IF_DEF(SVE_JK_4A,   IS_NONE, NONE) // SVE_JK_4A  ...........mmmmm .h.gggnnnnnttttt  -- SVE 64-bit scatter store (scalar plus 64-bit unscaled offsets)
IF_DEF(SVE_JK_4A_B, IS_NONE, NONE) // SVE_JK_4A_B  ...........mmmmm .h.gggnnnnnttttt  -- 
IF_DEF(SVE_JK_4B,   IS_NONE, NONE) // SVE_JK_4B  ...........mmmmm ...gggnnnnnttttt  -- SVE 64-bit scatter store (scalar plus 64-bit unscaled offsets)
IF_DEF(SVE_JL_3A,   IS_NONE, NONE) // SVE_JL_3A  ...........iiiii ...gggnnnnnttttt  -- SVE 64-bit scatter store (vector plus immediate)
IF_DEF(SVE_JM_3A,   IS_NONE, NONE) // SVE_JM_3A  ............iiii ...gggnnnnnttttt  -- SVE contiguous non-temporal store (scalar plus immediate)
IF_DEF(SVE_JN_3A,   IS_NONE, NONE) // SVE_JN_3A  .........xx.iiii ...gggnnnnnttttt  -- SVE contiguous store (scalar plus immediate)
IF_DEF(SVE_JN_3B,   IS_NONE, NONE) // SVE_JN_3B  ..........x.iiii ...gggnnnnnttttt  -- SVE contiguous store (scalar plus immediate)
IF_DEF(SVE_JN_3C,   IS_NONE, NONE) // SVE_JN_3C  ............iiii ...gggnnnnnttttt  -- SVE contiguous store (scalar plus immediate)
IF_DEF(SVE_JN_3C_D, IS_NONE, NONE) // SVE_JN_3C_D  ............iiii ...gggnnnnnttttt  -- 
IF_DEF(SVE_JO_3A,   IS_NONE, NONE) // SVE_JO_3A  ............iiii ...gggnnnnnttttt  -- SVE store multiple structures (scalar plus immediate)

//////////////////////////////////////////////////////////////////////////////
#undef IF_DEF
//////////////////////////////////////////////////////////////////////////////
// clang-format on
