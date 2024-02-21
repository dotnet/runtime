// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef HARDWARE_INTRINSIC
#error Define HARDWARE_INTRINSIC before including this file
#endif
/*****************************************************************************/

// clang-format off

#ifdef FEATURE_HW_INTRINSICS
// ***************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************
//                 ISA            Function name                                              SIMD size  NumArg  EncodesExtraTypeArg                                                                                            Instructions                                                                                        Category                           Flags
//                                                                                                                          {TYP_BYTE,           TYP_UBYTE,          TYP_SHORT,          TYP_USHORT,         TYP_INT,            TYP_UINT,           TYP_LONG,           TYP_ULONG,          TYP_FLOAT,          TYP_DOUBLE}
// ***************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************
//  SVE Intrinsics

// Sve
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskByte,                                               -1,      1,      false, {INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskDouble,                                             -1,      1,      false, {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_sve_ptrue},   HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskInt16,                                              -1,      1,      false, {INS_invalid,        INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskInt32,                                              -1,      1,      false, {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskInt64,                                              -1,      1,      false, {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskSByte,                                              -1,      1,      false, {INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskSingle,                                             -1,      1,      false, {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_sve_ptrue,      INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskUInt16,                                             -1,      1,      false, {INS_invalid,        INS_invalid,        INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskUInt32,                                             -1,      1,      false, {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)
HARDWARE_INTRINSIC(Sve,           CreateTrueMaskUInt64,                                             -1,      1,      false, {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)

HARDWARE_INTRINSIC(Sve,           LoadVector,                                                       -1,      2,      true,  {INS_sve_ld1b,       INS_sve_ld1b,       INS_sve_ld1h,       INS_sve_ld1h,       INS_sve_ld1w,       INS_sve_ld1w,       INS_sve_ld1d,       INS_sve_ld1d,       INS_sve_ld1w,       INS_sve_ld1d},    HW_Category_MemoryLoad,            HW_Flag_Scalable|HW_Flag_MaskedOperation)

#endif // FEATURE_HW_INTRINSIC

#undef HARDWARE_INTRINSIC

// clang-format on
