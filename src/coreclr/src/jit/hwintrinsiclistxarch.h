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
//                  Intrinsic ID                   Function name                ISA   
//  SSE Intrinsics          
HARDWARE_INTRINSIC(SSE_IsSupported,             "get_IsSupported",              SSE)
HARDWARE_INTRINSIC(SSE_Add,                     "Add",                          SSE)

//  SSE2 Intrinsics 
HARDWARE_INTRINSIC(SSE2_IsSupported,            "get_IsSupported",              SSE2)
HARDWARE_INTRINSIC(SSE2_Add,                    "Add",                          SSE2)

//  SSE3 Intrinsics 
HARDWARE_INTRINSIC(SSE3_IsSupported,            "get_IsSupported",              SSE3)

//  SSSE3 Intrinsics 
HARDWARE_INTRINSIC(SSSE3_IsSupported,           "get_IsSupported",              SSSE3)

//  SSE41 Intrinsics 
HARDWARE_INTRINSIC(SSE41_IsSupported,           "get_IsSupported",              SSE41)

//  SSE42 Intrinsics 
HARDWARE_INTRINSIC(SSE42_IsSupported,           "get_IsSupported",              SSE42)
HARDWARE_INTRINSIC(SSE42_Crc32,                 "Crc32",                        SSE42)

//  AVX Intrinsics 
HARDWARE_INTRINSIC(AVX_IsSupported,             "get_IsSupported",              AVX)
HARDWARE_INTRINSIC(AVX_Add,                     "Add",                          AVX)

//  AVX2 Intrinsics 
HARDWARE_INTRINSIC(AVX2_IsSupported,            "get_IsSupported",              AVX2)
HARDWARE_INTRINSIC(AVX2_Add,                    "Add",                          AVX2)

//  AES Intrinsics 
HARDWARE_INTRINSIC(AES_IsSupported,             "get_IsSupported",              AES)

//  BMI1 Intrinsics 
HARDWARE_INTRINSIC(BMI1_IsSupported,            "get_IsSupported",              BMI1)

//  BMI2 Intrinsics 
HARDWARE_INTRINSIC(BMI2_IsSupported,            "get_IsSupported",              BMI2)

//  FMA Intrinsics 
HARDWARE_INTRINSIC(FMA_IsSupported,             "get_IsSupported",              FMA)

//  LZCNT Intrinsics 
HARDWARE_INTRINSIC(LZCNT_IsSupported,           "get_IsSupported",              LZCNT)
HARDWARE_INTRINSIC(LZCNT_LeadingZeroCount,      "LeadingZeroCount",             LZCNT)

//  PCLMULQDQ Intrinsics 
HARDWARE_INTRINSIC(PCLMULQDQ_IsSupported,       "get_IsSupported",              PCLMULQDQ)

//  POPCNT Intrinsics 
HARDWARE_INTRINSIC(POPCNT_IsSupported,          "get_IsSupported",              POPCNT)
HARDWARE_INTRINSIC(POPCNT_PopCount,              "PopCount",                    POPCNT)
#endif // FEATURE_HW_INTRINSICS

#undef HARDWARE_INTRINSIC

// clang-format on
