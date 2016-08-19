// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// clang-format off

/*****************************************************************************/

#ifndef GTSTRUCT_0
#error  Define GTSTRUCT_0 before including this file.
#endif

#ifndef GTSTRUCT_1
#error  Define GTSTRUCT_1 before including this file.
#endif

#ifndef GTSTRUCT_2
#error  Define GTSTRUCT_2 before including this file.
#endif

#ifndef GTSTRUCT_3
#error  Define GTSTRUCT_3 before including this file.
#endif

#ifndef GTSTRUCT_4
#error  Define GTSTRUCT_4 before including this file.
#endif

#ifndef GTSTRUCT_N
#error  Define GTSTRUCT_N before including this file.
#endif

/*****************************************************************************/

//
//       Field name    , Allowed node enum(s)
//                                  

GTSTRUCT_0(UnOp        , GT_OP)
GTSTRUCT_0(Op          , GT_OP)
#if !FEATURE_EH_FUNCLETS
GTSTRUCT_2(Val         , GT_END_LFIN, GT_JMP)
#else
GTSTRUCT_1(Val         , GT_JMP)
#endif
#ifndef LEGACY_BACKEND
GTSTRUCT_3(IntConCommon, GT_CNS_INT, GT_CNS_LNG, GT_JMPTABLE)
GTSTRUCT_1(JumpTable   , GT_JMPTABLE)
#else // LEGACY_BACKEND
GTSTRUCT_2(IntConCommon, GT_CNS_INT, GT_CNS_LNG)
#endif// LEGACY_BACKEND
GTSTRUCT_1(IntCon      , GT_CNS_INT)
GTSTRUCT_1(LngCon      , GT_CNS_LNG)
GTSTRUCT_1(DblCon      , GT_CNS_DBL) 
GTSTRUCT_1(StrCon      , GT_CNS_STR) 
GTSTRUCT_N(LclVarCommon, GT_LCL_VAR, GT_LCL_FLD, GT_REG_VAR, GT_PHI_ARG, GT_STORE_LCL_VAR, GT_STORE_LCL_FLD, GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR) 
GTSTRUCT_3(LclVar      , GT_LCL_VAR, GT_LCL_VAR_ADDR, GT_STORE_LCL_VAR) 
#ifndef LEGACY_BACKEND
GTSTRUCT_3(LclFld      , GT_LCL_FLD, GT_STORE_LCL_FLD, GT_LCL_FLD_ADDR)
#else // LEGACY_BACKEND
GTSTRUCT_1(LclFld      , GT_LCL_FLD)
#endif // LEGACY_BACKEND
GTSTRUCT_1(RegVar      , GT_REG_VAR)
GTSTRUCT_1(Cast        , GT_CAST)
GTSTRUCT_1(Box         , GT_BOX)
GTSTRUCT_1(Field       , GT_FIELD) 
GTSTRUCT_1(Call        , GT_CALL) 
GTSTRUCT_1(ArgList     , GT_LIST)
GTSTRUCT_1(Colon       , GT_COLON)
GTSTRUCT_1(FptrVal     , GT_FTN_ADDR)
GTSTRUCT_1(Intrinsic   , GT_INTRINSIC) 
GTSTRUCT_1(Index       , GT_INDEX)
#ifdef FEATURE_SIMD
GTSTRUCT_2(BoundsChk   , GT_ARR_BOUNDS_CHECK, GT_SIMD_CHK)
#else // !FEATURE_SIMD
GTSTRUCT_1(BoundsChk   , GT_ARR_BOUNDS_CHECK)
#endif  // !FEATURE_SIMD
GTSTRUCT_1(ArrLen      , GT_ARR_LENGTH)
GTSTRUCT_1(ArrElem     , GT_ARR_ELEM) 
GTSTRUCT_1(ArrOffs     , GT_ARR_OFFSET)
GTSTRUCT_1(ArrIndex    , GT_ARR_INDEX) 
GTSTRUCT_1(RetExpr     , GT_RET_EXPR) 
GTSTRUCT_2(Stmt        , GT_STMT, GT_IL_OFFSET) 
GTSTRUCT_1(Obj         , GT_OBJ)
GTSTRUCT_2(CopyOrReload, GT_COPY, GT_RELOAD)
GTSTRUCT_2(ClsVar      , GT_CLS_VAR, GT_CLS_VAR_ADDR) 
GTSTRUCT_1(ArgPlace    , GT_ARGPLACE) 
GTSTRUCT_1(Label       , GT_LABEL) 
GTSTRUCT_1(CmpXchg     , GT_CMPXCHG)
GTSTRUCT_1(AddrMode    , GT_LEA)
GTSTRUCT_1(Qmark       , GT_QMARK)
GTSTRUCT_1(PhiArg      , GT_PHI_ARG)
GTSTRUCT_1(StoreInd    , GT_STOREIND)
GTSTRUCT_N(Indir       , GT_STOREIND, GT_IND, GT_NULLCHECK, GT_OBJ)
GTSTRUCT_1(PutArgStk   , GT_PUTARG_STK)
GTSTRUCT_1(PhysReg     , GT_PHYSREG)
GTSTRUCT_3(BlkOp       , GT_COPYBLK, GT_INITBLK, GT_COPYOBJ)
GTSTRUCT_1(CpObj       , GT_COPYOBJ)
GTSTRUCT_1(InitBlk     , GT_INITBLK)
GTSTRUCT_1(CpBlk       , GT_COPYBLK)
#ifdef FEATURE_SIMD
GTSTRUCT_1(SIMD        , GT_SIMD) 
#endif // FEATURE_SIMD
GTSTRUCT_1(AllocObj    , GT_ALLOCOBJ)
/*****************************************************************************/
#undef  GTSTRUCT_0
#undef  GTSTRUCT_1
#undef  GTSTRUCT_2
#undef  GTSTRUCT_3
#undef  GTSTRUCT_4
#undef  GTSTRUCT_N
/*****************************************************************************/

// clang-format on
