// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef HARDWARE_INTRINSIC
#error Define HARDWARE_INTRINSIC before including this file
#endif
/*****************************************************************************/

// clang-format off

#ifdef FEATURE_HW_INTRINSICS
//                 ISA            Function name                                              SIMD size  NumArg                                                                                                                  Instructions                                                                                        Category                           Flags
//                                                                                                                  {TYP_BYTE,           TYP_UBYTE,          TYP_SHORT,          TYP_USHORT,         TYP_INT,            TYP_UINT,           TYP_LONG,           TYP_ULONG,          TYP_FLOAT,          TYP_DOUBLE}
// ***************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************
// TODO: s/NONE/RiscVBase
#define FIRST_NI_NONE          NI_NONE_AbsScalar
//  Base Intrinsics
HARDWARE_INTRINSIC(NONE,       AbsScalar,                                                         8,      1,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fsgnjx_s,           INS_fsgnjx_d},        HW_Category_Scalar,                 HW_Flag_NoFlag)
HARDWARE_INTRINSIC(NONE,       FusedMultiplyAddNegatedScalar,                                     8,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fnmadd_s,         INS_fnmadd_d},      HW_Category_Scalar,                  HW_Flag_SpecialCodeGen)
HARDWARE_INTRINSIC(NONE,       FusedMultiplyAddScalar,                                            8,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fmadd_s,          INS_fmadd_d},       HW_Category_Scalar,                  HW_Flag_SpecialCodeGen)
HARDWARE_INTRINSIC(NONE,       FusedMultiplySubtractNegatedScalar,                                8,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fnmsub_s,         INS_fnmsub_d},      HW_Category_Scalar,                  HW_Flag_SpecialCodeGen)
HARDWARE_INTRINSIC(NONE,       FusedMultiplySubtractScalar,                                       8,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fmsub_s,          INS_fmsub_d},       HW_Category_Scalar,                  HW_Flag_SpecialCodeGen)

// HARDWARE_INTRINSIC(NONE, LeadingSignCount,                                                  0,      1,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_cls,            INS_invalid,        INS_cls,            INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_Scalar,                HW_Flag_BaseTypeFromFirstArg|HW_Flag_NoFloatingPointUsed)
// HARDWARE_INTRINSIC(NONE, LeadingZeroCount,                                                  0,      1,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_clz,            INS_clz,            INS_invalid,        INS_invalid},     HW_Category_Scalar,                HW_Flag_BaseTypeFromFirstArg|HW_Flag_NoFloatingPointUsed)

// HARDWARE_INTRINSIC(NONE, MultiplyHigh,                                                      0,      2,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_smulh,          INS_umulh,          INS_invalid,        INS_invalid},     HW_Category_Scalar,                HW_Flag_NoFloatingPointUsed)
#define LAST_NI_NONE NI_NONE_FusedMultiplySubtractScalar

#endif // FEATURE_HW_INTRINSIC

#undef HARDWARE_INTRINSIC

// clang-format on
