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
//                 ISA            Function name                                              SIMD size  NumArg                                                                                                                 Instructions                                                                                        Category                           Flags
//                                                                                                                  {TYP_BYTE,           TYP_UBYTE,          TYP_SHORT,          TYP_USHORT,         TYP_INT,            TYP_UINT,           TYP_LONG,           TYP_ULONG,          TYP_FLOAT,          TYP_DOUBLE}
// ***************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************************
//  Wasm Intrinsics
#define FIRST_NI_Vector128        NI_Vector128_Create
HARDWARE_INTRINSIC(Vector128,      Create,                                                            8,     -1,    {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_Helper,                HW_Flag_SpecialImport|HW_Flag_NoCodeGen)
HARDWARE_INTRINSIC(Vector128,      CreateScalar,                                                      8,      1,    {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_Helper,                HW_Flag_SpecialImport|HW_Flag_NoCodeGen)
HARDWARE_INTRINSIC(Vector128,      CreateScalarUnsafe,                                                8,      1,    {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_Helper,                HW_Flag_SpecialImport|HW_Flag_NoCodeGen)
HARDWARE_INTRINSIC(Vector128,      GetElement,                                                        8,      1,    {INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid,        INS_invalid},     HW_Category_IMM,                   HW_Flag_SpecialImport)
#define LAST_NI_Vector128         NI_Vector128_CreateScalarUnsafe
#endif // FEATURE_HW_INTRINSICS