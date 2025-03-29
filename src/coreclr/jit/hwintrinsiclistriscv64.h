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
#define FIRST_NI_RiscV64Base          NI_RiscV64Base_AbsScalar
//  Base Intrinsics
HARDWARE_INTRINSIC(RiscV64Base,       AbsScalar,                                                         0,      1,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fsgnjx_s,           INS_fsgnjx_d},        HW_Category_Scalar,                 HW_Flag_NoFlag)
HARDWARE_INTRINSIC(RiscV64Base,       FusedMultiplyAddScalar,                                            0,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fmadd_s,          INS_fmadd_d},       HW_Category_Scalar,                  HW_Flag_NoFlag)
HARDWARE_INTRINSIC(RiscV64Base,       FusedMultiplySubtractScalar,                                       0,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fmsub_s,          INS_fmsub_d},       HW_Category_Scalar,                  HW_Flag_NoFlag)
HARDWARE_INTRINSIC(RiscV64Base,       FusedNegatedMultiplyAddScalar,                                     0,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fnmadd_s,         INS_fnmadd_d},      HW_Category_Scalar,                  HW_Flag_NoFlag)
HARDWARE_INTRINSIC(RiscV64Base,       FusedNegatedMultiplySubtractScalar,                                0,      3,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_fnmsub_s,         INS_fnmsub_d},      HW_Category_Scalar,                  HW_Flag_NoFlag)
// HARDWARE_INTRINSIC(RiscV64Base, MultiplyHigh,                                                      0,      2,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_smulh,          INS_umulh,          INS_invalid,        INS_invalid},     HW_Category_Scalar,                HW_Flag_NoFloatingPointUsed)
#define LAST_NI_RiscV64Base NI_RiscV64Base_FusedNegatedMultiplySubtractScalar

// #define FIRST_NI_Zbb               NI_Zbb_LeadingZeroCount
// HARDWARE_INTRINSIC(RiscV64Base, LeadingZeroCount,                                                  0,      1,     {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_clzw,        INS_clzw,        INS_clz,            INS_clz,            INS_invalid,        INS_invalid},     HW_Category_Scalar,                HW_Flag_BaseTypeFromFirstArg|HW_Flag_NoFloatingPointUsed)
// #define LAST_NI_Zbb               NI_Zbb_LeadingZeroCount

#endif // FEATURE_HW_INTRINSIC

#undef HARDWARE_INTRINSIC

// clang-format on
