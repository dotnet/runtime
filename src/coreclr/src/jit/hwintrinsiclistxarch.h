// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
#ifndef HARDWARE_INTRINSIC
#error Define HARDWARE_INTRINSIC before including this file
#endif
/*****************************************************************************/

// clang-format off

#if FEATURE_HW_INTRINSICS
//                 Intrinsic ID                                     Function name                                   ISA
//  SSE Intrinsics
HARDWARE_INTRINSIC(SSE_IsSupported,                                 "get_IsSupported",                              SSE)
HARDWARE_INTRINSIC(SSE_Add,                                         "Add",                                          SSE)
HARDWARE_INTRINSIC(SSE_AddScalar,                                   "AddScalar",                                    SSE)
HARDWARE_INTRINSIC(SSE_And,                                         "And",                                          SSE)
HARDWARE_INTRINSIC(SSE_AndNot,                                      "AndNot",                                       SSE)
HARDWARE_INTRINSIC(SSE_CompareEqual,                                "CompareEqual",                                 SSE)
HARDWARE_INTRINSIC(SSE_CompareEqualOrderedScalar,                   "CompareEqualOrderedScalar",                    SSE)
HARDWARE_INTRINSIC(SSE_CompareEqualScalar,                          "CompareEqualScalar",                           SSE)
HARDWARE_INTRINSIC(SSE_CompareEqualUnorderedScalar,                 "CompareEqualUnorderedScalar",                  SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThan,                          "CompareGreaterThan",                           SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThanOrderedScalar,             "CompareGreaterThanOrderedScalar",              SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThanScalar,                    "CompareGreaterThanScalar",                     SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThanUnorderedScalar,           "CompareGreaterThanUnorderedScalar",            SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThanOrEqual,                   "CompareGreaterThanOrEqual",                    SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThanOrEqualOrderedScalar,      "CompareGreaterThanOrEqualOrderedScalar",       SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThanOrEqualScalar,             "CompareGreaterThanOrEqualScalar",              SSE)
HARDWARE_INTRINSIC(SSE_CompareGreaterThanOrEqualUnorderedScalar,    "CompareGreaterThanOrEqualUnorderedScalar",     SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThan,                             "CompareLessThan",                              SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThanOrderedScalar,                "CompareLessThanOrderedScalar",                 SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThanScalar,                       "CompareLessThanScalar",                        SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThanUnorderedScalar,              "CompareLessThanUnorderedScalar",               SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThanOrEqual,                      "CompareLessThanOrEqual",                       SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThanOrEqualOrderedScalar,         "CompareLessThanOrEqualOrderedScalar",          SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThanOrEqualScalar,                "CompareLessThanOrEqualScalar",                 SSE)
HARDWARE_INTRINSIC(SSE_CompareLessThanOrEqualUnorderedScalar,       "CompareLessThanOrEqualUnorderedScalar",        SSE)
HARDWARE_INTRINSIC(SSE_CompareNotEqual,                             "CompareNotEqual",                              SSE)
HARDWARE_INTRINSIC(SSE_CompareNotEqualOrderedScalar,                "CompareNotEqualOrderedScalar",                 SSE)
HARDWARE_INTRINSIC(SSE_CompareNotEqualScalar,                       "CompareNotEqualScalar",                        SSE)
HARDWARE_INTRINSIC(SSE_CompareNotEqualUnorderedScalar,              "CompareNotEqualUnorderedScalar",               SSE)
HARDWARE_INTRINSIC(SSE_CompareNotGreaterThan,                       "CompareNotGreaterThan",                        SSE)
HARDWARE_INTRINSIC(SSE_CompareNotGreaterThanScalar,                 "CompareNotGreaterThanScalar",                  SSE)
HARDWARE_INTRINSIC(SSE_CompareNotGreaterThanOrEqual,                "CompareNotGreaterThanOrEqual",                 SSE)
HARDWARE_INTRINSIC(SSE_CompareNotGreaterThanOrEqualScalar,          "CompareNotGreaterThanOrEqualScalar",           SSE)
HARDWARE_INTRINSIC(SSE_CompareNotLessThan,                          "CompareNotLessThan",                           SSE)
HARDWARE_INTRINSIC(SSE_CompareNotLessThanScalar,                    "CompareNotLessThanScalar",                     SSE)
HARDWARE_INTRINSIC(SSE_CompareNotLessThanOrEqual,                   "CompareNotLessThanOrEqual",                    SSE)
HARDWARE_INTRINSIC(SSE_CompareNotLessThanOrEqualScalar,             "CompareNotLessThanOrEqualScalar",              SSE)
HARDWARE_INTRINSIC(SSE_CompareOrdered,                              "CompareOrdered",                               SSE)
HARDWARE_INTRINSIC(SSE_CompareOrderedScalar,                        "CompareOrderedScalar",                         SSE)
HARDWARE_INTRINSIC(SSE_CompareUnordered,                            "CompareUnordered",                             SSE)
HARDWARE_INTRINSIC(SSE_CompareUnorderedScalar,                      "CompareUnorderedScalar",                       SSE)
HARDWARE_INTRINSIC(SSE_ConvertToInt32,                              "ConvertToInt32",                               SSE)
HARDWARE_INTRINSIC(SSE_ConvertToInt64,                              "ConvertToInt64",                               SSE)
HARDWARE_INTRINSIC(SSE_ConvertToSingle,                             "ConvertToSingle",                              SSE)
HARDWARE_INTRINSIC(SSE_ConvertToVector128SingleScalar,              "ConvertToVector128SingleScalar",               SSE)
HARDWARE_INTRINSIC(SSE_ConvertToInt32WithTruncation,                "ConvertToInt32WithTruncation",                 SSE)
HARDWARE_INTRINSIC(SSE_ConvertToInt64WithTruncation,                "ConvertToInt64WithTruncation",                 SSE)
HARDWARE_INTRINSIC(SSE_Divide,                                      "Divide",                                       SSE)
HARDWARE_INTRINSIC(SSE_DivideScalar,                                "DivideScalar",                                 SSE)
HARDWARE_INTRINSIC(SSE_LoadAlignedVector128,                        "LoadAlignedVector128",                         SSE)
HARDWARE_INTRINSIC(SSE_LoadHigh,                                    "LoadHigh",                                     SSE)
HARDWARE_INTRINSIC(SSE_LoadLow,                                     "LoadLow",                                      SSE)
HARDWARE_INTRINSIC(SSE_LoadScalar,                                  "LoadScalar",                                   SSE)
HARDWARE_INTRINSIC(SSE_LoadVector128,                               "LoadVector128",                                SSE)
HARDWARE_INTRINSIC(SSE_Max,                                         "Max",                                          SSE)
HARDWARE_INTRINSIC(SSE_MaxScalar,                                   "MaxScalar",                                    SSE)
HARDWARE_INTRINSIC(SSE_Min,                                         "Min",                                          SSE)
HARDWARE_INTRINSIC(SSE_MinScalar,                                   "MinScalar",                                    SSE)
HARDWARE_INTRINSIC(SSE_MoveHighToLow,                               "MoveHighToLow",                                SSE)
HARDWARE_INTRINSIC(SSE_MoveLowToHigh,                               "MoveLowToHigh",                                SSE)
HARDWARE_INTRINSIC(SSE_MoveMask,                                    "MoveMask",                                     SSE)
HARDWARE_INTRINSIC(SSE_MoveScalar,                                  "MoveScalar",                                   SSE)
HARDWARE_INTRINSIC(SSE_Multiply,                                    "Multiply",                                     SSE)
HARDWARE_INTRINSIC(SSE_MultiplyScalar,                              "MultiplyScalar",                               SSE)
HARDWARE_INTRINSIC(SSE_Or,                                          "Or",                                           SSE)
HARDWARE_INTRINSIC(SSE_Reciprocal,                                  "Reciprocal",                                   SSE)
HARDWARE_INTRINSIC(SSE_ReciprocalScalar,                            "ReciprocalScalar",                             SSE)
HARDWARE_INTRINSIC(SSE_ReciprocalSqrt,                              "ReciprocalSqrt",                               SSE)
HARDWARE_INTRINSIC(SSE_ReciprocalSqrtScalar,                        "ReciprocalSqrtScalar",                         SSE)
HARDWARE_INTRINSIC(SSE_SetAllVector128,                             "SetAllVector128",                              SSE)
HARDWARE_INTRINSIC(SSE_SetScalar,                                   "SetScalar",                                    SSE)
HARDWARE_INTRINSIC(SSE_SetVector128,                                "SetVector128",                                 SSE)
HARDWARE_INTRINSIC(SSE_SetZeroVector128,                            "SetZeroVector128",                             SSE)
HARDWARE_INTRINSIC(SSE_Shuffle,                                     "Shuffle",                                      SSE)
HARDWARE_INTRINSIC(SSE_Sqrt,                                        "Sqrt",                                         SSE)
HARDWARE_INTRINSIC(SSE_SqrtScalar,                                  "SqrtScalar",                                   SSE)
HARDWARE_INTRINSIC(SSE_StaticCast,                                  "StaticCast",                                   SSE)
HARDWARE_INTRINSIC(SSE_Store,                                       "Store",                                        SSE)
HARDWARE_INTRINSIC(SSE_StoreAligned,                                "StoreAligned",                                 SSE)
HARDWARE_INTRINSIC(SSE_StoreAlignedNonTemporal,                     "StoreAlignedNonTemporal",                      SSE)
HARDWARE_INTRINSIC(SSE_StoreHigh,                                   "StoreHigh",                                    SSE)
HARDWARE_INTRINSIC(SSE_StoreLow,                                    "StoreLow",                                     SSE)
HARDWARE_INTRINSIC(SSE_StoreScalar,                                 "StoreScalar",                                  SSE)
HARDWARE_INTRINSIC(SSE_Subtract,                                    "Subtract",                                     SSE)
HARDWARE_INTRINSIC(SSE_SubtractScalar,                              "SubtractScalar",                               SSE)
HARDWARE_INTRINSIC(SSE_UnpackHigh,                                  "UnpackHigh",                                   SSE)
HARDWARE_INTRINSIC(SSE_UnpackLow,                                   "UnpackLow",                                    SSE)
HARDWARE_INTRINSIC(SSE_Xor,                                         "Xor",                                          SSE)

//  SSE2 Intrinsics
HARDWARE_INTRINSIC(SSE2_IsSupported,                                "get_IsSupported",                              SSE2)
HARDWARE_INTRINSIC(SSE2_Add,                                        "Add",                                          SSE2)

//  SSE3 Intrinsics
HARDWARE_INTRINSIC(SSE3_IsSupported,                                "get_IsSupported",                              SSE3)

//  SSSE3 Intrinsics
HARDWARE_INTRINSIC(SSSE3_IsSupported,                               "get_IsSupported",                              SSSE3)

//  SSE41 Intrinsics
HARDWARE_INTRINSIC(SSE41_IsSupported,                               "get_IsSupported",                              SSE41)

//  SSE42 Intrinsics
HARDWARE_INTRINSIC(SSE42_IsSupported,                               "get_IsSupported",                              SSE42)
HARDWARE_INTRINSIC(SSE42_Crc32,                                     "Crc32",                                        SSE42)

//  AVX Intrinsics
HARDWARE_INTRINSIC(AVX_IsSupported,                                 "get_IsSupported",                              AVX)
HARDWARE_INTRINSIC(AVX_Add,                                         "Add",                                          AVX)

//  AVX2 Intrinsics
HARDWARE_INTRINSIC(AVX2_IsSupported,                                "get_IsSupported",                              AVX2)
HARDWARE_INTRINSIC(AVX2_Add,                                        "Add",                                          AVX2)

//  AES Intrinsics
HARDWARE_INTRINSIC(AES_IsSupported,                                 "get_IsSupported",                              AES)

//  BMI1 Intrinsics
HARDWARE_INTRINSIC(BMI1_IsSupported,                                "get_IsSupported",                              BMI1)

//  BMI2 Intrinsics
HARDWARE_INTRINSIC(BMI2_IsSupported,                                "get_IsSupported",                              BMI2)

//  FMA Intrinsics
HARDWARE_INTRINSIC(FMA_IsSupported,                                 "get_IsSupported",                              FMA)

//  LZCNT Intrinsics
HARDWARE_INTRINSIC(LZCNT_IsSupported,                               "get_IsSupported",                              LZCNT)
HARDWARE_INTRINSIC(LZCNT_LeadingZeroCount,                          "LeadingZeroCount",                             LZCNT)

//  PCLMULQDQ Intrinsics
HARDWARE_INTRINSIC(PCLMULQDQ_IsSupported,                           "get_IsSupported",                              PCLMULQDQ)

//  POPCNT Intrinsics
HARDWARE_INTRINSIC(POPCNT_IsSupported,                              "get_IsSupported",                              POPCNT)
HARDWARE_INTRINSIC(POPCNT_PopCount,                                 "PopCount",                                     POPCNT)
#endif // FEATURE_HW_INTRINSICS

#undef HARDWARE_INTRINSIC

// clang-format on
