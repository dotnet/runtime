// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#ifndef GTSTRUCT_2_SPECIAL
#error  Define GTSTRUCT_2_SPECIAL before including this file.
#endif

#ifndef GTSTRUCT_3_SPECIAL
#error  Define GTSTRUCT_3_SPECIAL before including this file.
#endif

/*****************************************************************************/

//
//       Field name    , Allowed node enum(s)
//
// The "SPECIAL" variants indicate that some or all of the allowed opers exist elsewhere. This is
// used in the DEBUGGABLE_GENTREE implementation when determining which vtable pointer to use for
// a given oper. For example, IntConCommon (for the GenTreeIntConCommon type) allows opers
// for all its subtypes. The "SPECIAL" version is attached to the supertypes. "N" is always
// considered "special".

GTSTRUCT_0(UnOp        , GT_OP)
GTSTRUCT_0(Op          , GT_OP)
#if !defined(FEATURE_EH_FUNCLETS)
GTSTRUCT_2(Val         , GT_END_LFIN, GT_JMP)
#else
GTSTRUCT_1(Val         , GT_JMP)
#endif
GTSTRUCT_2_SPECIAL(IntConCommon, GT_CNS_INT, GT_CNS_LNG)
GTSTRUCT_1(IntCon      , GT_CNS_INT)
GTSTRUCT_1(LngCon      , GT_CNS_LNG)
GTSTRUCT_1(DblCon      , GT_CNS_DBL)
GTSTRUCT_1(StrCon      , GT_CNS_STR)
GTSTRUCT_N(LclVarCommon, GT_LCL_VAR, GT_LCL_FLD, GT_PHI_ARG, GT_STORE_LCL_VAR, GT_STORE_LCL_FLD, GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR)
GTSTRUCT_3(LclVar      , GT_LCL_VAR, GT_LCL_VAR_ADDR, GT_STORE_LCL_VAR)
GTSTRUCT_3(LclFld      , GT_LCL_FLD, GT_STORE_LCL_FLD, GT_LCL_FLD_ADDR)
GTSTRUCT_1(Cast        , GT_CAST)
GTSTRUCT_1(Box         , GT_BOX)
GTSTRUCT_1(Field       , GT_FIELD)
GTSTRUCT_1(Call        , GT_CALL)
GTSTRUCT_1(ArgList     , GT_LIST)
GTSTRUCT_1(FieldList   , GT_FIELD_LIST)
GTSTRUCT_1(Colon       , GT_COLON)
GTSTRUCT_1(FptrVal     , GT_FTN_ADDR)
GTSTRUCT_1(Intrinsic   , GT_INTRINSIC)
GTSTRUCT_1(Index       , GT_INDEX)
GTSTRUCT_1(IndexAddr   , GT_INDEX_ADDR)
#if defined(FEATURE_HW_INTRINSICS) && defined(FEATURE_SIMD)
GTSTRUCT_3(BoundsChk   , GT_ARR_BOUNDS_CHECK, GT_SIMD_CHK, GT_HW_INTRINSIC_CHK)
#elif defined(FEATURE_SIMD)
GTSTRUCT_2(BoundsChk   , GT_ARR_BOUNDS_CHECK, GT_SIMD_CHK)
#elif defined(FEATURE_HW_INTRINSICS)
GTSTRUCT_2(BoundsChk   , GT_ARR_BOUNDS_CHECK, GT_HW_INTRINSIC_CHK)
#else // !FEATURE_SIMD && !FEATURE_HW_INTRINSICS
GTSTRUCT_1(BoundsChk   , GT_ARR_BOUNDS_CHECK)
#endif  // !FEATURE_SIMD && !FEATURE_HW_INTRINSICS
GTSTRUCT_1(ArrLen      , GT_ARR_LENGTH)
GTSTRUCT_1(ArrElem     , GT_ARR_ELEM)
GTSTRUCT_1(ArrOffs     , GT_ARR_OFFSET)
GTSTRUCT_1(ArrIndex    , GT_ARR_INDEX)
GTSTRUCT_1(RetExpr     , GT_RET_EXPR)
GTSTRUCT_1(ILOffset    , GT_IL_OFFSET)
GTSTRUCT_2(CopyOrReload, GT_COPY, GT_RELOAD)
GTSTRUCT_2(ClsVar      , GT_CLS_VAR, GT_CLS_VAR_ADDR)
GTSTRUCT_1(ArgPlace    , GT_ARGPLACE)
GTSTRUCT_1(CmpXchg     , GT_CMPXCHG)
GTSTRUCT_1(AddrMode    , GT_LEA)
GTSTRUCT_N(Blk         , GT_BLK, GT_STORE_BLK, GT_OBJ, GT_STORE_OBJ, GT_DYN_BLK, GT_STORE_DYN_BLK)
GTSTRUCT_2(Obj         , GT_OBJ, GT_STORE_OBJ)
GTSTRUCT_2(DynBlk      , GT_DYN_BLK, GT_STORE_DYN_BLK)
GTSTRUCT_1(Qmark       , GT_QMARK)
GTSTRUCT_1(PhiArg      , GT_PHI_ARG)
GTSTRUCT_1(Phi         , GT_PHI)
GTSTRUCT_1(StoreInd    , GT_STOREIND)
GTSTRUCT_N(Indir       , GT_STOREIND, GT_IND, GT_NULLCHECK, GT_BLK, GT_STORE_BLK, GT_OBJ, GT_STORE_OBJ, GT_DYN_BLK, GT_STORE_DYN_BLK)
#if FEATURE_ARG_SPLIT
GTSTRUCT_2_SPECIAL(PutArgStk, GT_PUTARG_STK, GT_PUTARG_SPLIT)
GTSTRUCT_1(PutArgSplit , GT_PUTARG_SPLIT)
#else // !FEATURE_ARG_SPLIT
GTSTRUCT_1(PutArgStk   , GT_PUTARG_STK)
#endif // !FEATURE_ARG_SPLIT
GTSTRUCT_1(PhysReg     , GT_PHYSREG)
#ifdef FEATURE_SIMD
GTSTRUCT_1(SIMD        , GT_SIMD)
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
GTSTRUCT_1(HWIntrinsic , GT_HWINTRINSIC)
#endif // FEATURE_HW_INTRINSICS
GTSTRUCT_1(AllocObj    , GT_ALLOCOBJ)
GTSTRUCT_1(RuntimeLookup, GT_RUNTIMELOOKUP)
GTSTRUCT_2(CC          , GT_JCC, GT_SETCC)
#if defined(TARGET_X86)
GTSTRUCT_1(MultiRegOp  , GT_MUL_LONG)
#elif defined (TARGET_ARM)
GTSTRUCT_3(MultiRegOp  , GT_MUL_LONG, GT_PUTARG_REG, GT_BITCAST)
#endif
/*****************************************************************************/
#undef  GTSTRUCT_0
#undef  GTSTRUCT_1
#undef  GTSTRUCT_2
#undef  GTSTRUCT_3
#undef  GTSTRUCT_4
#undef  GTSTRUCT_N
#undef  GTSTRUCT_2_SPECIAL
#undef  GTSTRUCT_3_SPECIAL
/*****************************************************************************/

// clang-format on
