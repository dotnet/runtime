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
HARDWARE_INTRINSIC(Sve,           TrueMask,                                                         -1,      1,      false, {INS_invalid,        INS_sve_ptrue,      INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_EnumPattern,           HW_Flag_Scalable|HW_Flag_HasImmediateOperand|HW_Flag_ReturnsPerElementMask)

#endif // FEATURE_HW_INTRINSIC

#undef HARDWARE_INTRINSIC

// clang-format on
