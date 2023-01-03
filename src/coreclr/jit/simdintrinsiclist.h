// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef SIMD_INTRINSIC
#error Define SIMD_INTRINSIC before including this file
#endif
/*****************************************************************************/

// clang-format off
#ifdef FEATURE_SIMD

    /*
         Notes:
            a) TYP_UNKNOWN means 'baseType' of SIMD vector which is not known apriori
            b) Each method maps to a unique intrinsic Id
            c) To facilitate argument types to be used as an array initializer, args are listed within "{}" braces.
            d) Since comma is used as actual param separator in a macro, TYP_UNDEF entries are added to keep param count constant.
            e) TODO-Cleanup: when we plumb TYP_SIMD through front-end, replace TYP_STRUCT with TYP_SIMD.
     */

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)

// Max number of parameters that we model in the table for SIMD intrinsic methods.
#define SIMD_INTRINSIC_MAX_MODELED_PARAM_COUNT       3

// Actual maximum number of parameters for any SIMD intrinsic method.
// Constructors that take either N values, or a smaller Vector plus additional element values,
// actually have more arguments than the "modeled" count.
#define SIMD_INTRINSIC_MAX_PARAM_COUNT               5

// Max number of base types supported by an intrinsic
#define SIMD_INTRINSIC_MAX_BASETYPE_COUNT    10

/***************************************************************************************************************************************************************************************************************************
              Method Name,              Is Instance    Intrinsic Id,             Display Name,             return type,   Arg count,    Individual argument types           Supported base types
                                           Method                                                                                      (including implicit "this")
 ***************************************************************************************************************************************************************************************************************************/
SIMD_INTRINSIC(nullptr,                     false,       None,                     "None",                   TYP_UNDEF,      0,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// Internal intrinsics for saving & restoring the upper half of a vector register
SIMD_INTRINSIC("UpperSave",                 false,       UpperSave,                "UpperSave Internal",     TYP_STRUCT,     2,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("UpperRestore",              false,       UpperRestore,             "UpperRestore Internal",  TYP_STRUCT,     2,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

SIMD_INTRINSIC(nullptr,                     false,       Invalid,                   "Invalid",               TYP_UNDEF,      0,      {TYP_UNDEF,  TYP_UNDEF,  TYP_UNDEF},   {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
#undef SIMD_INTRINSIC
#else // !defined(TARGET_XARCH) && !defined(TARGET_ARM64)
#error SIMD intrinsics not defined for target arch
#endif // !defined(TARGET_XARCH) && !defined(TARGET_ARM64)


#endif //FEATURE_SIMD
// clang-format on
